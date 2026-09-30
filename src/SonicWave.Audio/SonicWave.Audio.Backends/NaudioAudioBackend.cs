using System;
using System.Collections.Generic;
using SonicWave.Audio.Core;
using SonicWave.Audio.Decoders;
using SonicWave.Audio.Dsp;
using SonicWave.Core.Models;

namespace SonicWave.Audio.Backends;

internal sealed class NaudioAudioBackend : IAudioBackend, IDisposable
{
	private readonly NAudioDecoder _naudio = new NAudioDecoder();

	private string? _deviceId;

	private string? _asioDriver;

	private int _bufferMs = 200;

	private int _gen;

	private int _volume = 5;

	private bool _applyDsp = true;

	private double[] _eqGains = new double[10];

	private bool _reverbEnabled;

	private double _reverbMix = 0.2;

	private string _reverbPreset = "大厅";

	private double _spatialWidth = 0.6;

	private double _spatialModulation = 0.25;

	private double _spatialPredelayMs = 25.0;

	private TimbreSettings _timbre = new TimbreSettings();

	private IReadOnlyList<Vst3PluginState> _vst3 = Array.Empty<Vst3PluginState>();

	public string Name => "naudio";

	public bool IsAvailable => true;

	public NAudioDecoder Decoder => _naudio;

	public int SampleRate => _naudio.SampleRate;

	public TimeSpan Position => _naudio.Position;

	public TimeSpan Duration => _naudio.Duration;

	public bool IsActuallyPlaying => _naudio.IsPlaying;

	public int Volume
	{
		get
		{
			return _volume;
		}
		set
		{
			_volume = Math.Clamp(value, 0, 100);
			_naudio.Volume = (float)_volume / 100f;
		}
	}

	public double Speed { get; set; } = 1.0;

	public event Action? Ended;

	public event Action? Failed;

	public NaudioAudioBackend(string? deviceId, string? asioDriver, int bufferMs)
	{
		_deviceId = deviceId;
		_asioDriver = asioDriver;
		_bufferMs = ((bufferMs > 0) ? bufferMs : 200);
		_naudio.PlaybackStopped += OnPlaybackStopped;
	}

	public void Configure(string? deviceId, string? asioDriver, int bufferMs)
	{
		_deviceId = deviceId;
		_asioDriver = asioDriver;
		_bufferMs = ((bufferMs > 0) ? bufferMs : 200);
	}

	public void SetApplyDspOnOpen(bool apply)
	{
		_applyDsp = apply;
	}

	public void SetDspState(double[] eqGains, bool reverbEnabled, double reverbMix, string reverbPreset, double spatialWidth, double spatialModulation, double spatialPredelayMs, TimbreSettings timbre, IReadOnlyList<Vst3PluginState> vst3)
	{
		_eqGains = eqGains ?? new double[10];
		_reverbEnabled = reverbEnabled;
		_reverbMix = reverbMix;
		_reverbPreset = (string.IsNullOrEmpty(reverbPreset) ? "大厅" : reverbPreset);
		_spatialWidth = spatialWidth;
		_spatialModulation = spatialModulation;
		_spatialPredelayMs = spatialPredelayMs;
		_timbre = timbre ?? new TimbreSettings();
		_vst3 = vst3 ?? Array.Empty<Vst3PluginState>();
	}

	public bool Open(string filePath, bool startPaused)
	{
		_gen++;
		_naudio.Generation = _gen;
		if (!_naudio.Open(filePath, _deviceId, _asioDriver, _bufferMs))
		{
			return false;
		}
		ApplyDspLocked();
		_naudio.Volume = (float)_volume / 100f;
		return true;
	}

	public bool WaitUntilReady(int timeoutMs)
	{
		return true;
	}

	public void Play()
	{
		_naudio.Play();
	}

	public void Pause()
	{
		_naudio.Pause();
	}

	public void Stop()
	{
		_naudio.Stop();
	}

	public bool Seek(TimeSpan position)
	{
		return _naudio.Seek(position);
	}

	public void SetEqGains(double[] gains)
	{
		_eqGains = gains ?? new double[10];
		_naudio.SetEqGains(_eqGains);
	}

	public void SetSpatialAudio(bool enabled, double mix, string preset, double width, double modulation, double predelayMs)
	{
		_reverbEnabled = enabled;
		_reverbMix = mix;
		_reverbPreset = (string.IsNullOrEmpty(preset) ? "大厅" : preset);
		_spatialWidth = width;
		_spatialModulation = modulation;
		_spatialPredelayMs = predelayMs;
		_naudio.SetSpatialAudio(enabled, mix, preset, width, modulation, predelayMs);
	}

	public void SetTimbre(TimbreSettings timbre)
	{
		_timbre = timbre ?? new TimbreSettings();
		_naudio.SetTimbre(_timbre);
	}

	public void SetVst3(IReadOnlyList<Vst3PluginState> plugins)
	{
		_vst3 = plugins ?? Array.Empty<Vst3PluginState>();
		_naudio.SetVst3(_vst3);
	}

	private void ApplyDspLocked()
	{
		if (_applyDsp)
		{
			_naudio.SetEqGains(_eqGains);
			_naudio.SetSpatialAudio(_reverbEnabled, _reverbMix, _reverbPreset, _spatialWidth, _spatialModulation, _spatialPredelayMs);
			_naudio.SetTimbre(_timbre);
			_naudio.SetVst3(_vst3);
		}
		else
		{
			_naudio.SetEqGains(new double[10]);
			_naudio.SetSpatialAudio(enabled: false, 0.0, "大厅", 0.6, 0.25, 25.0);
			_naudio.SetTimbre(new TimbreSettings());
		}
	}

	private void OnPlaybackStopped(int gen, bool error)
	{
		if (gen == _gen)
		{
			if (error)
			{
				Failed?.Invoke();
			}
			else
			{
				Ended?.Invoke();
			}
		}
	}

	public void Dispose()
	{
		_naudio.Dispose();
	}
}
