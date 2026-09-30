using System;
using System.Collections.Generic;
using SonicWave.Audio.Core;
using SonicWave.Audio.Decoders;
using SonicWave.Audio.Dsp;
using SonicWave.Core.Models;

namespace SonicWave.Audio.Backends;

internal sealed class VlcAudioBackend : IAudioBackend, IDisposable
{
	private readonly LibVLCDecoder _vlc = new LibVLCDecoder();

	public string Name => "vlc";

	public bool IsAvailable => _vlc.IsAvailable;

	public int SampleRate => 0;

	public TimeSpan Position => _vlc.Position;

	public TimeSpan Duration => _vlc.Duration;

	public bool IsActuallyPlaying => _vlc.IsPlaying;

	public int Volume
	{
		get
		{
			return _vlc.Volume;
		}
		set
		{
			_vlc.Volume = value;
		}
	}

	public double Speed
	{
		get
		{
			return _vlc.Speed;
		}
		set
		{
			_vlc.Speed = value;
		}
	}

	public event Action? Ended;

	public event Action? Failed;

	public VlcAudioBackend()
	{
		_vlc.EndReached += () =>
		{
			Ended?.Invoke();
		};
		_vlc.EncounteredError += () =>
		{
			Failed?.Invoke();
		};
	}

	public void Configure(string? deviceId, string? asioDriver, int bufferMs)
	{
	}

	public void SetApplyDspOnOpen(bool apply)
	{
	}

	public void SetDspState(double[] eqGains, bool reverbEnabled, double reverbMix, string reverbPreset, double spatialWidth, double spatialModulation, double spatialPredelayMs, TimbreSettings timbre, IReadOnlyList<Vst3PluginState> vst3)
	{
	}

	public bool Open(string filePath, bool startPaused)
	{
		if (!_vlc.IsAvailable)
		{
			_vlc.Initialize();
		}
		if (!_vlc.IsAvailable)
		{
			return false;
		}
		return _vlc.Open(filePath, startPaused);
	}

	public bool WaitUntilReady(int timeoutMs)
	{
		return _vlc.WaitUntilPlaying(timeoutMs);
	}

	public void Play()
	{
		_vlc.Play();
	}

	public void Pause()
	{
		_vlc.Pause();
	}

	public void Stop()
	{
		_vlc.Stop();
	}

	public bool Seek(TimeSpan position)
	{
		return _vlc.Seek(position);
	}

	public void SetEqGains(double[] gains)
	{
		_vlc.SetEqGains(gains);
	}

	public void SetSpatialAudio(bool enabled, double mix, string preset, double width, double modulation, double predelayMs)
	{
	}

	public void SetTimbre(TimbreSettings timbre)
	{
	}

	public void SetVst3(IReadOnlyList<Vst3PluginState> plugins)
	{
	}

	public void Dispose()
	{
		_vlc.Dispose();
	}
}
