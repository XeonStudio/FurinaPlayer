using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using SonicWave.Audio.Dsp;
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

		public long PositionFrames
		{
			get
			{
				return _positionFrames;
			}
			set
			{
				_positionFrames = value;
			}
		}

		public FloatSampleProvider(float[] samples, int channels, int sampleRate, object sync)
		{
			_samples = samples;
			_channels = channels;
			_sampleRate = sampleRate;
			_sync = sync;
			WaveFormat = NAudio.Wave.WaveFormat.CreateIeeeFloatWaveFormat(sampleRate, channels);
		}

		public int Read(float[] buffer, int offset, int count)
		{
			lock (_sync)
			{
				long num = _samples.Length / _channels;
				if (_positionFrames >= num)
				{
					return 0;
				}
				long num2 = Math.Min(count / _channels, num - _positionFrames);
				Array.Copy(_samples, _positionFrames * _channels, buffer, offset, num2 * _channels);
				_positionFrames += num2;
				return (int)(num2 * _channels);
			}
		}
	}

	private readonly object _lock = new object();

	private WaveOutEvent? _output;

	private FloatSampleProvider? _source;

	private float[] _samples = Array.Empty<float>();

	private int _channels = 1;

	private int _sampleRate = 44100;

	private bool _playing;

	private bool _paused;

	private RenderSettings _renderSettings = RenderSettings.Flat;

	private CancellationTokenSource? _renderCts;

	private bool _rendering;

	private Timer? _restartTimer;

	private bool _restartScheduled;

	private TimeSpan _resumePosition;

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
					_source.PositionFrames = Math.Clamp((long)(value.TotalSeconds * (double)_sampleRate), 0L, Math.Max(0L, num - 1));
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
			_source = new FloatSampleProvider(_samples, _channels, _sampleRate, _lock);
		}
	}

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
		}
		ScheduleRestart();
	}

	public void SetReverb(bool enabled, double mix, string preset)
	{
		SpatialParameters spatialParameters = SpatialAudioEffect.PresetDefaults(string.IsNullOrEmpty(preset) ? "大厅" : preset);
		SetSpatialAudio(enabled, mix, preset, spatialParameters.Width, spatialParameters.Modulation, spatialParameters.PredelayMs);
	}

	public void SetVst3(IReadOnlyList<Vst3PluginState> plugins)
	{
		lock (_lock)
		{
			_renderSettings = _renderSettings with
			{
				Vst3 = (plugins ?? Array.Empty<Vst3PluginState>())
			};
		}
		ScheduleRestart();
	}

	public void Play()
	{
		bool flag = false;
		lock (_lock)
		{
			if (_source == null || _playing || (_renderCts != null && _rendering))
			{
				return;
			}
			flag = _renderSettings.IsActive;
			if (!flag)
			{
				StartOutputLocked(_samples);
			}
		}
		if (flag)
		{
			StartRenderAsync();
		}
	}

	public void Pause()
	{
		lock (_lock)
		{
			if (_output != null && _playing)
			{
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
			if (!_playing || !_renderSettings.IsActive)
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
		bool flag = false;
		lock (_lock)
		{
			if (!_restartScheduled)
			{
				return;
			}
			_restartScheduled = false;
			if (!_playing || !_renderSettings.IsActive)
			{
				return;
			}
			_resumePosition = Position;
			flag = true;
		}
		if (flag)
		{
			Stop();
			Play();
		}
	}

	private async void StartRenderAsync()
	{
		CancellationTokenSource cts;
		float[] source;
		int sr;
		int ch;
		RenderSettings settings;
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
		}
		RenderingChanged?.Invoke(obj: true);
		RenderProgress?.Invoke(new RenderProgress(0.0, TimeSpan.Zero));
		try
		{
			Progress<RenderProgress> progress = new Progress<RenderProgress>((RenderProgress p) =>
			{
				RenderProgress?.Invoke(p);
			});
			RenderResult renderResult = await Task.Run(() => OfflineEffectRenderer.RenderSamplesAsync(source, sr, ch, settings, progress, cts.Token));
			lock (_lock)
			{
				if (cts.IsCancellationRequested)
				{
					return;
				}
				_renderCts = null;
				_rendering = false;
				if (!_playing && _output == null)
				{
					StartOutputLocked(renderResult.Samples);
					if (_resumePosition > TimeSpan.Zero)
					{
						_source.PositionFrames = Math.Clamp((long)(_resumePosition.TotalSeconds * (double)_sampleRate), 0L, Math.Max(0, _samples.Length / _channels - 1));
						_resumePosition = TimeSpan.Zero;
					}
				}
			}
		}
		catch (OperationCanceledException)
		{
			lock (_lock)
			{
				_rendering = false;
			}
		}
		catch (Exception)
		{
			lock (_lock)
			{
				_rendering = false;
				_renderCts = null;
				if (!_playing && _output == null)
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
		_source = new FloatSampleProvider(samples, _channels, _sampleRate, _lock);
		if (_output == null)
		{
			_output = new WaveOutEvent
			{
				DesiredLatency = 200
			};
			_output.PlaybackStopped += OnPlaybackStopped;
			_output.Init(new SampleToWaveProvider16(_source));
		}
		if (_paused)
		{
			_paused = false;
		}
		else if (_source.PositionFrames >= _samples.Length / _channels)
		{
			_source.PositionFrames = 0L;
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

	private void StopLocked()
	{
		_playing = false;
		_paused = false;
		if (_output != null)
		{
			_output.PlaybackStopped -= OnPlaybackStopped;
			_output.Stop();
			_output.Dispose();
			_output = null;
		}
		if (_source != null)
		{
			_source.PositionFrames = 0L;
		}
	}

	private void OnPlaybackStopped(object? sender, StoppedEventArgs e)
	{
		lock (_lock)
		{
			_playing = false;
			_paused = false;
		}
		PlaybackStopped?.Invoke();
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
