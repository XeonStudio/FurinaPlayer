using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using SonicWave.Audio.Dsp;
using SonicWave.Audio.Vst3;
using SonicWave.Core.Models;

namespace SonicWave.Audio;

public sealed class PreviewPlayer : IDisposable
{
	private sealed class FloatSampleProvider : ISampleProvider
	{
		private readonly float[] _samples;
		private readonly int _channels;
		private readonly int _sampleRate;
		private readonly object _sync;
		private long _positionFrames;

		public WaveFormat WaveFormat { get; }

		public float Volume { get; set; } = 1.0f;

		public long PositionFrames
		{
			get
			{
				lock (_sync)
				{
					return _positionFrames;
				}
			}
			set
			{
				lock (_sync)
				{
					_positionFrames = value;
				}
			}
		}

		public FloatSampleProvider(float[] samples, int channels, int sampleRate, float volume, object sync)
		{
			_samples = samples ?? Array.Empty<float>();
			_channels = ((channels <= 0) ? 1 : channels);
			_sampleRate = ((sampleRate > 0) ? sampleRate : 44100);
			Volume = volume;
			_sync = sync;
			WaveFormat = WaveFormat.CreateIeeeFloatWaveFormat(_sampleRate, _channels);
		}

		public int Read(float[] buffer, int offset, int count)
		{
			lock (_sync)
			{
				long totalFrames = _samples.Length / _channels;
				if (_positionFrames >= totalFrames)
				{
					return 0;
				}
				long framesToRead = Math.Min(count / _channels, totalFrames - _positionFrames);
				int samplesToCopy = (int)(framesToRead * _channels);
				int srcIdx = (int)(_positionFrames * _channels);

				float vol = Volume;
				for (int i = 0; i < samplesToCopy; i++)
				{
					float sample = _samples[srcIdx + i] * vol;
					// Transparent soft limiter to avoid harsh digital clipping / wrapping
					if (sample > 1.0f)
					{
						sample = 1.0f + (sample - 1.0f) * 0.1f;
					}
					else if (sample < -1.0f)
					{
						sample = -1.0f + (sample + 1.0f) * 0.1f;
					}
					buffer[offset + i] = Math.Clamp(sample, -1.05f, 1.05f);
				}

				_positionFrames += framesToRead;
				return samplesToCopy;
			}
		}
	}

	private sealed class SafeClampedWaveProvider16 : IWaveProvider
	{
		private readonly ISampleProvider _source;
		private float[] _floatBuffer = new float[4096];

		public WaveFormat WaveFormat { get; }

		public SafeClampedWaveProvider16(ISampleProvider source)
		{
			_source = source;
			WaveFormat = new WaveFormat(source.WaveFormat.SampleRate, 16, source.WaveFormat.Channels);
		}

		public int Read(byte[] buffer, int offset, int count)
		{
			int samplesRequired = count / 2;
			if (_floatBuffer.Length < samplesRequired)
			{
				Array.Resize(ref _floatBuffer, samplesRequired);
			}
			int samplesRead = _source.Read(_floatBuffer, 0, samplesRequired);
			int outIdx = offset;
			for (int i = 0; i < samplesRead; i++)
			{
				float s = Math.Clamp(_floatBuffer[i], -1.0f, 1.0f);
				short val = (short)Math.Clamp((int)(s * 32767f), -32768, 32767);
				buffer[outIdx++] = (byte)(val & 0xFF);
				buffer[outIdx++] = (byte)((val >> 8) & 0xFF);
			}
			return samplesRead * 2;
		}
	}

	private readonly object _lock = new object();
	private IWavePlayer? _output;
	private FloatSampleProvider? _source;
	private float[] _samples = Array.Empty<float>();
	private int _channels = 1;
	private int _sampleRate = 44100;
	private float _volume = 1.0f;
	private bool _playing;
	private bool _paused;
	private RenderSettings _renderSettings = RenderSettings.Flat;
	private EchoBiquadFilter[]? _parametricBands;
	private double _parametricPreampDb;
	private CancellationTokenSource? _renderCts;
	private bool _rendering;
	private Timer? _restartTimer;
	private bool _restartScheduled;
	private TimeSpan _resumePosition;
	private bool _settingsDirty;
	private string _lastVst3Signature = string.Empty;
	private long _lastRenderedVst3StateVersion;

	public float Volume
	{
		get
		{
			lock (_lock)
			{
				return _volume;
			}
		}
		set
		{
			lock (_lock)
			{
				_volume = Math.Clamp(value, 0f, 1f);
				if (_source != null)
				{
					_source.Volume = _volume;
				}
			}
		}
	}

	public bool IsPlaying
	{
		get
		{
			lock (_lock)
			{
				return _playing;
			}
		}
	}

	public bool IsRendering
	{
		get
		{
			lock (_lock)
			{
				return _rendering;
			}
		}
	}

	public TimeSpan Duration
	{
		get
		{
			if (_channels <= 0 || _sampleRate <= 0)
			{
				return TimeSpan.Zero;
			}
			return TimeSpan.FromSeconds((double)_samples.Length / (double)_channels / (double)_sampleRate);
		}
	}

	public TimeSpan Position
	{
		get
		{
			lock (_lock)
			{
				return (_source == null) ? TimeSpan.Zero : TimeSpan.FromSeconds((double)_source.PositionFrames / (double)_sampleRate);
			}
		}
		set
		{
			lock (_lock)
			{
				if (_source != null)
				{
					long num = _samples.Length / _channels;
					long clamped = Math.Clamp((long)(value.TotalSeconds * (double)_sampleRate), 0L, Math.Max(0L, num - 1));
					_source.PositionFrames = clamped;
					if (_paused || !_playing)
					{
						_resumePosition = TimeSpan.FromSeconds((double)clamped / (double)_sampleRate);
					}
				}
			}
		}
	}

	public event Action<bool>? RenderingChanged;
	public event Action<RenderProgress>? RenderProgress;
	public event Action? PlaybackStopped;

	public void Load(float[] samples, int sampleRate, int channels)
	{
		lock (_lock)
		{
			StopLocked();
			_resumePosition = TimeSpan.Zero;
			_restartScheduled = false;
			_samples = samples ?? Array.Empty<float>();
			_channels = ((channels <= 0) ? 1 : channels);
			_sampleRate = ((sampleRate > 0) ? sampleRate : 44100);
			_settingsDirty = HasActiveEffectsLocked();
			_source = new FloatSampleProvider(_samples, _channels, _sampleRate, _volume, _lock);
		}
	}

	private bool HasActiveParametricEqLocked()
	{
		if (Math.Abs(_parametricPreampDb) > 0.05)
		{
			return true;
		}
		if (_parametricBands == null || _parametricBands.Length == 0)
		{
			return false;
		}
		for (int i = 0; i < _parametricBands.Length; i++)
		{
			EchoBiquadFilter b = _parametricBands[i];
			if (b != null && b.Enabled &&
			    (Math.Abs(b.GainDb) > 0.05 ||
			     b.FilterType is EchoFilterType.LowPass or EchoFilterType.HighPass or EchoFilterType.BandPass or EchoFilterType.Notch))
			{
				return true;
			}
		}
		return false;
	}

	private bool HasActiveEffectsLocked() => _renderSettings.IsActive || HasActiveParametricEqLocked();

	public void SetEqGains(double[] gains)
	{
		lock (_lock)
		{
			if (gains == null || gains.Length != 10)
			{
				return;
			}
			_renderSettings = _renderSettings with
			{
				EqGains = (double[])gains.Clone()
			};
			_settingsDirty = true;
		}
		ScheduleRestart();
	}

	public void SetParametricEq(IReadOnlyList<EchoBiquadFilter>? bands, double preampDb = 0.0)
	{
		lock (_lock)
		{
			if (bands == null || bands.Count == 0)
			{
				_parametricBands = null;
			}
			else
			{
				var snapshot = new EchoBiquadFilter[bands.Count];
				int safeRate = _sampleRate > 0 ? _sampleRate : 44100;
				for (int i = 0; i < bands.Count; i++)
				{
					EchoBiquadFilter src = bands[i];
					var copy = new EchoBiquadFilter
					{
						FilterType = src.FilterType,
						FrequencyHz = src.FrequencyHz,
						GainDb = src.GainDb,
						Q = src.Q,
						Enabled = src.Enabled
					};
					copy.UpdateCoefficients(safeRate);
					snapshot[i] = copy;
				}
				_parametricBands = snapshot;
			}
			_parametricPreampDb = Math.Clamp(preampDb, -30.0, 30.0);
			_settingsDirty = true;
		}
		ScheduleRestart();
	}

	public void SetSpatialAudio(bool enabled, double mix, string preset, double width, double modulation, double predelayMs)
	{
		lock (_lock)
		{
			_renderSettings = _renderSettings with
			{
				ReverbEnabled = enabled,
				ReverbMix = mix,
				ReverbPreset = (string.IsNullOrEmpty(preset) ? "大厅" : preset),
				Spatial = new SpatialParameters(width, modulation, predelayMs)
			};
			_settingsDirty = true;
		}
		ScheduleRestart();
	}

	public void SetReverb(bool enabled, double mix, string preset)
	{
		SpatialParameters spatialParameters = SpatialAudioEffect.PresetDefaults(string.IsNullOrEmpty(preset) ? "大厅" : preset);
		SetSpatialAudio(enabled, mix, preset, spatialParameters.Width, spatialParameters.Modulation, spatialParameters.PredelayMs);
	}

	private static string ComputeVst3Signature(IReadOnlyList<Vst3PluginState>? plugins)
	{
		if (plugins == null || plugins.Count == 0)
		{
			return string.Empty;
		}
		StringBuilder sb = new StringBuilder();
		for (int i = 0; i < plugins.Count; i++)
		{
			Vst3PluginState? item = plugins[i];
			if (item != null && item.IsActive)
			{
				string key = string.IsNullOrWhiteSpace(item.Path) ? item.Name : Vst3NativeHost.ResolveCacheKey(item.Path);
				sb.Append(key)
				  .Append('|').Append(item.Drive.ToString("R"))
				  .Append('|').Append(item.Tone.ToString("R"))
				  .Append('|').Append(item.Mix.ToString("R"))
				  .Append(';');
			}
		}
		return sb.ToString();
	}

	public void SetVst3(IReadOnlyList<Vst3PluginState> plugins)
	{
		Vst3PluginState[] snapshot;
		if (plugins == null || plugins.Count == 0)
		{
			snapshot = Array.Empty<Vst3PluginState>();
		}
		else
		{
			snapshot = new Vst3PluginState[plugins.Count];
			for (int i = 0; i < plugins.Count; i++)
			{
				snapshot[i] = plugins[i];
			}
		}

		bool changed;
		lock (_lock)
		{
			_renderSettings = _renderSettings with
			{
				Vst3 = snapshot
			};
			string sig = ComputeVst3Signature(snapshot);
			long ver = Vst3NativeHost.Instance.StateVersion;
			changed = !string.Equals(sig, _lastVst3Signature, StringComparison.Ordinal) ||
			          (sig.Length > 0 && ver != _lastRenderedVst3StateVersion);
			if (changed)
			{
				_lastVst3Signature = sig;
				_settingsDirty = true;
			}
		}
		if (changed)
		{
			ScheduleRestart();
		}
	}

	public void Play()
	{
		bool needRender = false;
		lock (_lock)
		{
			if (_source == null || _playing || (_renderCts != null && _rendering))
			{
				return;
			}
			bool vst3ParamChanged = _lastVst3Signature.Length > 0 && _lastRenderedVst3StateVersion != Vst3NativeHost.Instance.StateVersion;
			if (_paused && _output != null && !_settingsDirty && !vst3ParamChanged)
			{
				_paused = false;
				_playing = true;
				_output.Play();
				return;
			}
			if (HasActiveEffectsLocked())
			{
				needRender = true;
			}
			else
			{
				_settingsDirty = false;
				StartOutputLocked(_samples);
			}
		}
		if (needRender)
		{
			StartRenderAsync();
		}
	}

	public void Pause()
	{
		lock (_lock)
		{
			if (_rendering)
			{
				CancelRenderLocked();
				_paused = true;
				return;
			}
			if (_output != null && _playing)
			{
				if (_source != null)
				{
					_resumePosition = TimeSpan.FromSeconds((double)_source.PositionFrames / (double)_sampleRate);
				}
				_output.Pause();
				_playing = false;
				_paused = true;
			}
		}
	}

	public void Stop()
	{
		lock (_lock)
		{
			CancelRenderLocked();
			StopLocked();
		}
		PlaybackStopped?.Invoke();
	}

	private void ScheduleRestart()
	{
		bool flag = false;
		lock (_lock)
		{
			if (!_playing && !_rendering)
			{
				return;
			}
			_restartScheduled = true;
			flag = true;
		}
		if (!flag)
		{
			return;
		}
		try
		{
			if (_restartTimer == null)
			{
				_restartTimer = new Timer((object? _) =>
				{
					RestartDue();
				}, null, -1, -1);
			}
			_restartTimer.Change(400, -1);
		}
		catch
		{
			RestartDue();
		}
	}

	private void RestartDue()
	{
		bool needRender = false;
		lock (_lock)
		{
			if (!_restartScheduled)
			{
				return;
			}
			_restartScheduled = false;
			if (!_playing && !_rendering)
			{
				return;
			}
			if (_source != null && _playing)
			{
				_resumePosition = TimeSpan.FromSeconds((double)_source.PositionFrames / (double)_sampleRate);
			}
			CancelRenderLocked();
			StopLocked(resetPosition: false);
			if (HasActiveEffectsLocked())
			{
				needRender = true;
			}
			else
			{
				_settingsDirty = false;
				StartOutputLocked(_samples);
			}
		}
		if (needRender)
		{
			StartRenderAsync();
		}
	}

	private async void StartRenderAsync()
	{
		CancellationTokenSource cts;
		float[] source;
		int sr;
		int ch;
		RenderSettings settings;
		EchoBiquadFilter[]? parametricBands;
		double parametricPreampDb;
		long startVst3Ver;
		lock (_lock)
		{
			if (_playing || _rendering)
			{
				return;
			}
			_renderCts?.Cancel();
			_renderCts?.Dispose();
			cts = new CancellationTokenSource();
			_renderCts = cts;
			_rendering = true;
			source = _samples;
			sr = _sampleRate;
			ch = _channels;
			settings = _renderSettings;
			parametricBands = _parametricBands;
			parametricPreampDb = _parametricPreampDb;
			startVst3Ver = Vst3NativeHost.Instance.StateVersion;
		}
		RenderingChanged?.Invoke(obj: true);
		RenderProgress?.Invoke(new RenderProgress(0.0, TimeSpan.Zero));
		try
		{
			Progress<RenderProgress> progress = new Progress<RenderProgress>((RenderProgress p) =>
			{
				RenderProgress?.Invoke(p);
			});
			RenderResult renderResult = await Task.Run(() =>
				parametricBands != null || Math.Abs(parametricPreampDb) > 0.05
					? OfflineEffectRenderer.RenderSamplesWithParametricEqAsync(source, sr, ch, settings, parametricBands, parametricPreampDb, progress, cts.Token)
					: OfflineEffectRenderer.RenderSamplesAsync(source, sr, ch, settings, progress, cts.Token));
			lock (_lock)
			{
				if (cts.IsCancellationRequested)
				{
					return;
				}
				_renderCts = null;
				_rendering = false;
				_settingsDirty = false;
				_lastRenderedVst3StateVersion = startVst3Ver;
				if (!_playing)
				{
					StartOutputLocked(renderResult.Samples);
				}
			}
		}
		catch (OperationCanceledException)
		{
			lock (_lock)
			{
				if (ReferenceEquals(_renderCts, cts))
				{
					_renderCts = null;
					_rendering = false;
				}
			}
		}
		catch (Exception)
		{
			lock (_lock)
			{
				if (ReferenceEquals(_renderCts, cts))
				{
					_rendering = false;
					_renderCts = null;
				}
				if (!_playing)
				{
					StartOutputLocked(_samples);
				}
			}
		}
		finally
		{
			RenderingChanged?.Invoke(obj: false);
		}
	}

	private void StartOutputLocked(float[] samples)
	{
		// Clean up previous output session
		if (_output != null)
		{
			_output.PlaybackStopped -= OnPlaybackStopped;
			try { _output.Stop(); } catch { }
			try { _output.Dispose(); } catch { }
			_output = null;
		}

		_source = new FloatSampleProvider(samples, _channels, _sampleRate, _volume, _lock);

		if (_resumePosition > TimeSpan.Zero)
		{
			long totalFrames = samples.Length / Math.Max(1, _channels);
			_source.PositionFrames = Math.Clamp((long)(_resumePosition.TotalSeconds * (double)_sampleRate), 0L, Math.Max(0L, totalFrames - 1));
			_resumePosition = TimeSpan.Zero;
		}
		else if (_paused)
		{
			_source.PositionFrames = 0L;
		}
		_paused = false;

		// Use modern low-latency WASAPI with IEEE float 32-bit output; fallback safely to clamped WaveOutEvent
		try
		{
			var wasapi = new WasapiOut(NAudio.CoreAudioApi.AudioClientShareMode.Shared, 100);
			wasapi.PlaybackStopped += OnPlaybackStopped;
			wasapi.Init(_source.ToWaveProvider());
			_output = wasapi;
		}
		catch
		{
			var waveOut = new WaveOutEvent
			{
				DesiredLatency = 150
			};
			waveOut.PlaybackStopped += OnPlaybackStopped;
			waveOut.Init(new SafeClampedWaveProvider16(_source));
			_output = waveOut;
		}

		_output.Play();
		_playing = true;
	}

	private void CancelRenderLocked()
	{
		try
		{
			_renderCts?.Cancel();
		}
		catch
		{
		}
		_renderCts = null;
		_rendering = false;
	}

	private void StopLocked(bool resetPosition = true)
	{
		_playing = false;
		_paused = false;
		if (_output != null)
		{
			_output.PlaybackStopped -= OnPlaybackStopped;
			try { _output.Stop(); } catch { }
			try { _output.Dispose(); } catch { }
			_output = null;
		}
		if (resetPosition)
		{
			_resumePosition = TimeSpan.Zero;
			if (_source != null)
			{
				_source.PositionFrames = 0L;
			}
		}
	}

	private void OnPlaybackStopped(object? sender, StoppedEventArgs e)
	{
		bool raised = false;
		lock (_lock)
		{
			if (!ReferenceEquals(sender, _output))
			{
				return;
			}
			_playing = false;
			_paused = false;
			raised = true;
		}
		if (raised)
		{
			PlaybackStopped?.Invoke();
		}
	}

	public void Dispose()
	{
		lock (_lock)
		{
			CancelRenderLocked();
			StopLocked();
			_restartTimer?.Dispose();
			_restartTimer = null;
		}
	}
}
