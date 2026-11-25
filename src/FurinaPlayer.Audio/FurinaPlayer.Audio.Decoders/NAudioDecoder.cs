using System;
using System.Collections.Generic;
using System.Threading;
using NAudio.CoreAudioApi;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using FurinaPlayer.Audio.Dsp;
using FurinaPlayer.Audio.Output;
using FurinaPlayer.Core.Models;

namespace FurinaPlayer.Audio.Decoders;

public sealed class NAudioDecoder : IDisposable
{
	private IWavePlayer? _player;

	private SafeAudioFileReader? _reader;

	private DspSampleProvider? _dsp;

	private FurinaSrcSampleProvider? _srcProvider;

	private FurinaSrcRuntimeConfig _srcConfig = new();

	private EventHandler<StoppedEventArgs>? _stoppedHandler;

	private readonly object _lock = new object();

	public int Generation { get; set; }

	public bool IsPlaying { get; private set; }

	public string? LastInitError { get; private set; }

	public string? LastPlaybackExceptionMessage { get; private set; }

	public string ActiveOutputKind { get; private set; } = "WASAPI Shared";

	public DspSampleProvider? Dsp => _dsp;

	public FurinaSrcSampleProvider? SrcProvider => _srcProvider;

	public int BitsPerSample => _reader?.WaveFormat.BitsPerSample ?? 16;

	public int Channels => _reader?.WaveFormat.Channels ?? 2;

	public int SampleRate => _reader?.WaveFormat.SampleRate ?? 0;

	public int OutputSampleRate => _srcProvider?.TargetSampleRate ?? SampleRate;

	public TimeSpan Position
	{
		get
		{
			lock (_lock)
			{
				return _reader?.CurrentTime ?? TimeSpan.Zero;
			}
		}
	}

	public TimeSpan Duration
	{
		get
		{
			lock (_lock)
			{
				return _reader?.TotalTime ?? TimeSpan.Zero;
			}
		}
	}

	public float Volume
	{
		get
		{
			lock (_lock)
			{
				return _dsp?.OutputVolume ?? 1f;
			}
		}
		set
		{
			lock (_lock)
			{
				float volume = Math.Clamp(value, 0f, 1f);
				if (_reader != null)
				{
					_reader.Volume = 1f;
				}
				if (_dsp != null)
				{
					_dsp.OutputVolume = volume;
				}
			}
		}
	}

	public event Action<int, bool>? PlaybackStopped;

	public bool Open(
		string filePath,
		string? deviceId,
		string? asioDriver,
		int bufferMs = 200,
		FurinaSrcRuntimeConfig? srcConfig = null,
		OutputMode outputMode = OutputMode.WasapiShared)
	{
		lock (_lock)
		{
			Close();
			try
			{
				LastInitError = null;
				LastPlaybackExceptionMessage = null;
				_srcConfig = srcConfig ?? new FurinaSrcRuntimeConfig();
				_reader = new SafeAudioFileReader(filePath);
				int sourceRate = _reader.WaveFormat.SampleRate;
				int channels = _reader.WaveFormat.Channels;
				_dsp = new DspSampleProvider(_reader, sourceRate, channels);

				int desiredRate = FurinaSrcCatalog.ResolveTargetSampleRate(sourceRate, _srcConfig.Mode, _srcConfig.CustomTargetRate);
				List<int> candidates = BuildRateCandidateLadder(sourceRate, desiredRate);

				Exception? lastEx = null;
				foreach (int candidateRate in candidates)
				{
					bool isFallbackRate = candidateRate != desiredRate;
					var srcProvider = new FurinaSrcSampleProvider(
						_dsp,
						candidateRate,
						_srcConfig,
						isFallbackRate: isFallbackRate,
						sharedResampler: _dsp.DspChain.Resampler);
					var waveProvider = new SampleToWaveProvider(srcProvider);

					if (TryInitOutputWithResilience(waveProvider, outputMode, deviceId, asioDriver, bufferMs, out IWavePlayer? player, out string outputKind, out Exception? initEx))
					{
						_srcProvider = srcProvider;
						_player = player;
						ActiveOutputKind = outputKind;
						break;
					}
					lastEx = initEx;
				}

				if (_player == null)
				{
					throw lastEx ?? new InvalidOperationException("Unable to initialize audio output device.");
				}

				int gen = Generation;
				_stoppedHandler = (object? _, StoppedEventArgs e) =>
				{
					OnPlaybackStopped(gen, e);
				};
				_player.PlaybackStopped += _stoppedHandler;
				return true;
			}
			catch (Exception ex)
			{
				LastInitError = $"{ex.GetType().Name}: {ex.Message}";
				Close();
				return false;
			}
		}
	}

	private static bool TryInitOutputWithResilience(
		IWaveProvider waveProvider,
		OutputMode outputMode,
		string? deviceId,
		string? asioDriver,
		int bufferMs,
		out IWavePlayer? initializedPlayer,
		out string activeKind,
		out Exception? lastError)
	{
		initializedPlayer = null;
		activeKind = "WASAPI Shared";
		lastError = null;

		var attempts = new List<(bool UseAsio, bool Exclusive, bool ForceDefaultDevice, bool UseWaveOut, int DelayBeforeMs)>();
		if (outputMode == OutputMode.Asio && !string.IsNullOrWhiteSpace(asioDriver))
		{
			attempts.Add((true, false, false, false, 0));
		}

		bool wantExclusive = outputMode == OutputMode.WasapiExclusive;
		if (!string.IsNullOrWhiteSpace(deviceId))
		{
			if (wantExclusive)
			{
				attempts.Add((false, true, false, false, 0));
			}
			attempts.Add((false, false, false, false, 0));
		}

		if (wantExclusive)
		{
			attempts.Add((false, true, true, false, 0));
		}
		attempts.Add((false, false, true, false, 0));
		// Retry default WASAPI once after short settling delay (critical for hot-plug / Bluetooth switches)
		attempts.Add((false, false, true, false, 160));
		attempts.Add((false, false, true, true, 0));

		foreach (var attempt in attempts)
		{
			if (attempt.DelayBeforeMs > 0)
			{
				Thread.Sleep(attempt.DelayBeforeMs);
			}
			IWavePlayer? player = null;
			try
			{
				player = CreateOutputPlayer(
					useAsio: attempt.UseAsio,
					asioDriver: asioDriver,
					deviceId: deviceId,
					exclusive: attempt.Exclusive,
					forceDefaultDevice: attempt.ForceDefaultDevice,
					useWaveOut: attempt.UseWaveOut,
					bufferMs: bufferMs);
				player.Init(waveProvider);
				initializedPlayer = player;
				activeKind = attempt.UseAsio ? "ASIO"
					: attempt.UseWaveOut ? "WaveOut Fallback"
					: (attempt.Exclusive ? "WASAPI Exclusive" : (attempt.ForceDefaultDevice && !string.IsNullOrWhiteSpace(deviceId) ? "WASAPI Shared (Fallback Default)" : "WASAPI Shared"));
				return true;
			}
			catch (Exception ex)
			{
				lastError = ex;
				try
				{
					player?.Dispose();
				}
				catch
				{
				}
			}
		}
		return false;
	}

	private static IWavePlayer CreateOutputPlayer(
		bool useAsio,
		string? asioDriver,
		string? deviceId,
		bool exclusive,
		bool forceDefaultDevice,
		bool useWaveOut,
		int bufferMs)
	{
		int safeBufferMs = Math.Clamp(bufferMs, 40, 1000);
		if (useAsio && !string.IsNullOrWhiteSpace(asioDriver))
		{
			return new AsioOut(asioDriver);
		}
		if (useWaveOut)
		{
			return new WaveOutEvent { DesiredLatency = safeBufferMs };
		}

		var shareMode = exclusive ? AudioClientShareMode.Exclusive : AudioClientShareMode.Shared;
		using MMDeviceEnumerator enumerator = new MMDeviceEnumerator();
		if (!forceDefaultDevice && !string.IsNullOrWhiteSpace(deviceId))
		{
			using MMDevice targetDevice = enumerator.GetDevice(deviceId);
			if (targetDevice.State != DeviceState.Active)
			{
				throw new InvalidOperationException("Target WASAPI endpoint is no longer active.");
			}
			return new WasapiOut(targetDevice, shareMode, useEventSync: false, safeBufferMs);
		}

		using MMDevice defaultDevice = enumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);
		return new WasapiOut(defaultDevice, shareMode, useEventSync: false, safeBufferMs);
	}

	private static List<int> BuildRateCandidateLadder(int sourceRate, int desiredRate)
	{
		var list = new List<int> { desiredRate };
		if (desiredRate != sourceRate)
		{
			int familyBase = FurinaSrcCatalog.DetectRateFamilyBase(sourceRate);
			int[] ladder = familyBase == 44100
				? new[] { 176400, 88200, 192000, 96000, 48000 }
				: new[] { 192000, 96000, 48000 };
			foreach (int r in ladder)
			{
				if (r < desiredRate && r >= sourceRate && !list.Contains(r))
				{
					list.Add(r);
				}
			}
			if (!list.Contains(sourceRate))
			{
				list.Add(sourceRate);
			}
		}
		return list;
	}

	public void SetSrcConfig(FurinaSrcRuntimeConfig config)
	{
		_srcConfig = config ?? new FurinaSrcRuntimeConfig();
		_srcProvider?.ApplyConfig(_srcConfig);
	}

	private void OnPlaybackStopped(int gen, StoppedEventArgs e)
	{
		IsPlaying = false;
		if (e.Exception != null)
		{
			LastPlaybackExceptionMessage = $"{e.Exception.GetType().Name}: {e.Exception.Message}";
		}
		PlaybackStopped?.Invoke(gen, e.Exception != null);
	}

	public bool OpenForReading(string filePath)
	{
		lock (_lock)
		{
			Close();
			try
			{
				_reader = new SafeAudioFileReader(filePath);
				_dsp = new DspSampleProvider(_reader, _reader.WaveFormat.SampleRate, _reader.WaveFormat.Channels);
				return true;
			}
			catch (Exception ex)
			{
				LastInitError = $"{ex.GetType().Name}: {ex.Message}";
				Close();
				return false;
			}
		}
	}

	public void Play()
	{
		lock (_lock)
		{
			if (_player != null && _reader != null)
			{
				_player.Play();
				IsPlaying = true;
			}
		}
	}

	public void Pause()
	{
		lock (_lock)
		{
			_player?.Pause();
			IsPlaying = false;
		}
	}

	public void Stop()
	{
		lock (_lock)
		{
			_player?.Stop();
			IsPlaying = false;
		}
	}

	public bool Seek(TimeSpan position)
	{
		lock (_lock)
		{
			if (_reader == null)
			{
				return false;
			}
			try
			{
				_reader.CurrentTime = position;
				_srcProvider?.RequestReset();
				return true;
			}
			catch
			{
				return false;
			}
		}
	}

	public void SetEqGains(double[] gains)
	{
		_dsp?.SetEqGains(gains);
	}

	public void SetSpatialAudio(bool enabled, double mix, string preset, double width, double modulation, double predelayMs)
	{
		_dsp?.SetSpatialAudio(enabled, mix, preset, width, modulation, predelayMs);
	}

	public void SetReverb(bool enabled, double mix, string preset)
	{
		SetSpatialAudio(enabled, mix, preset, SpatialParameters.Default.Width, SpatialParameters.Default.Modulation, SpatialParameters.Default.PredelayMs);
	}

	public void SetTimbre(TimbreSettings timbre)
	{
		_dsp?.SetTimbre(timbre);
	}

	public void SetVst3(IReadOnlyList<Vst3PluginState> plugins)
	{
		_dsp?.SetVst3(plugins);
	}

	public int ReadSamples(float[] buffer, int offset, int count)
	{
		lock (_lock)
		{
			return _reader?.Read(buffer, offset, count) ?? 0;
		}
	}

	public void Close()
	{
		lock (_lock)
		{
			if (_player != null)
			{
				if (_stoppedHandler != null)
				{
					_player.PlaybackStopped -= _stoppedHandler;
				}
				_stoppedHandler = null;
				try { _player.Stop(); } catch { }
				try { _player.Dispose(); } catch { }
				_player = null;
			}
			_reader?.Dispose();
			_reader = null;
			_dsp = null;
			_srcProvider = null;
			IsPlaying = false;
		}
	}

	public void Dispose()
	{
		Close();
	}
}