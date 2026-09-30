using System;
using System.Collections.Generic;
using SonicWave.Audio.Core;
using SonicWave.Audio.Dsp;
using SonicWave.Core.Models;

namespace SonicWave.Audio;

public sealed class AudioEngine : IDisposable
{
	private readonly AudioSession _session;

	public bool IsVlcAvailable => _session.IsVlcAvailable;

	public PlaybackState State => _session.State;

	public int CurrentSampleRate => _session.CurrentSampleRate;

	public Track? CurrentTrack => _session.CurrentTrack;

	public IReadOnlyList<Track> Queue => _session.Queue;

	public int QueueIndex => _session.QueueIndex;

	public PlaybackMode Mode
	{
		get
		{
			return _session.Mode;
		}
		set
		{
			_session.Mode = value;
		}
	}

	public TimeSpan Position => _session.Position;

	public TimeSpan Duration => _session.Duration;

	public int Volume
	{
		get
		{
			return _session.Volume;
		}
		set
		{
			_session.Volume = value;
		}
	}

	public double Speed
	{
		get
		{
			return _session.Speed;
		}
		set
		{
			_session.Speed = value;
		}
	}

	public event Action<Track?>? TrackChanged;

	public event Action<PlaybackState>? StateChanged;

	public event Action<TimeSpan>? PositionChanged;

	public event Action<Track>? TrackEnded;

	public event Action<Track, string>? PlaybackError;

	public event Action<bool>? RenderingChanged;

	public event Action<RenderProgress>? RenderProgress;

	public event Action<int>? VolumeChanged;

	public event Action<PlaybackStatus>? StatusChanged;

	public AudioEngine()
	{
		_session = new AudioSession();
		_session.TrackChanged += (Track? t) =>
		{
			TrackChanged?.Invoke(t);
		};
		_session.StateChanged += (PlaybackState s) =>
		{
			StateChanged?.Invoke(s);
		};
		_session.PositionChanged += (TimeSpan p) =>
		{
			PositionChanged?.Invoke(p);
		};
		_session.TrackEnded += (Track t) =>
		{
			TrackEnded?.Invoke(t);
		};
		_session.PlaybackError += (Track t, PlaybackErrorKind _, string msg) =>
		{
			PlaybackError?.Invoke(t, msg);
		};
		_session.RenderingChanged += (bool v) =>
		{
			RenderingChanged?.Invoke(v);
		};
		_session.RenderProgress += (RenderProgress p) =>
		{
			RenderProgress?.Invoke(p);
		};
		_session.VolumeChanged += (int v) =>
		{
			VolumeChanged?.Invoke(v);
		};
		_session.StatusChanged += (PlaybackStatus s) =>
		{
			StatusChanged?.Invoke(s);
		};
	}

	public void ConfigureOutput(OutputMode mode, string? deviceId = null, string? asioDriver = null, int bufferMs = 200)
	{
		_session.ConfigureOutput(mode, deviceId, asioDriver, bufferMs);
	}

	public void SetQueue(IEnumerable<Track> tracks, int startIndex = 0)
	{
		_session.SetQueue(tracks, startIndex);
	}

	public void RemoveTrackFromQueue(long trackId)
	{
		_session.RemoveTrackFromQueue(trackId);
	}

	public void PlayTrack(Track track)
	{
		_session.PlayTrack(track);
	}

	public bool PrepareRestore(TimeSpan position)
	{
		return _session.PrepareRestore(position);
	}

	public void Play()
	{
		_session.Play();
	}

	public void Pause()
	{
		_session.Pause();
	}

	public void TogglePlayPause()
	{
		_session.TogglePlayPause();
	}

	public void Stop()
	{
		_session.Stop();
	}

	public bool Next()
	{
		return _session.Next();
	}

	public bool Previous()
	{
		return _session.Previous();
	}

	public void Seek(TimeSpan position)
	{
		_session.Seek(position);
	}

	public void SetAbLoop(TimeSpan? a, TimeSpan? b)
	{
		_session.SetAbLoop(a, b);
	}

	public void SetEqGains(double[] gains)
	{
		_session.SetEqGains(gains);
	}

	public void SetSpatialAudio(bool enabled, double mix, string preset, double width, double modulation, double predelayMs)
	{
		_session.SetSpatialAudio(enabled, mix, preset, width, modulation, predelayMs);
	}

	public void SetReverb(bool enabled, double mix, string preset)
	{
		_session.SetReverb(enabled, mix, preset);
	}

	public void SetTimbre(TimbreSettings timbre)
	{
		_session.SetTimbre(timbre);
	}

	public void SetVst3(IReadOnlyList<Vst3PluginState> plugins)
	{
		_session.SetVst3(plugins);
	}

	public void Dispose()
	{
		_session.Dispose();
	}
}
