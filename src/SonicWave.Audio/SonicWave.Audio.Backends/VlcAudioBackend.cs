using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using SonicWave.Audio.Core;
using SonicWave.Audio.Decoders;
using SonicWave.Audio.Dsp;
using SonicWave.Core.Models;

namespace SonicWave.Audio.Backends;

internal sealed class VlcAudioBackend : IAudioBackend, ISwitchStoppableBackend, IDisposable
{
	private readonly LibVLCDecoder _vlc = new LibVLCDecoder();

	private readonly object _opLock = new object();

	private int _gen;

	private int _acceptEvents;

	private int _stoppedSinceOpen = 1;

	private int _disposed;

	private long _openedTimestamp;

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
		_vlc.EndReached += OnVlcEndReached;
		_vlc.EncounteredError += OnVlcEncounteredError;
	}

	private void OnVlcEndReached()
	{
		if (Volatile.Read(ref _disposed) != 0 || Volatile.Read(ref _acceptEvents) == 0)
		{
			return;
		}
		long openedTs = Volatile.Read(ref _openedTimestamp);
		if (openedTs > 0 && (Stopwatch.GetTimestamp() - openedTs) * 1000.0 / Stopwatch.Frequency < 250.0)
		{
			TimeSpan pos = _vlc.Position;
			TimeSpan dur = _vlc.Duration;
			if (pos < TimeSpan.FromMilliseconds(500) && dur > TimeSpan.FromSeconds(2))
			{
				return;
			}
		}
		Ended?.Invoke();
	}

	private void OnVlcEncounteredError()
	{
		if (Volatile.Read(ref _disposed) != 0 || Volatile.Read(ref _acceptEvents) == 0)
		{
			return;
		}
		Failed?.Invoke();
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
		if (Volatile.Read(ref _disposed) != 0)
		{
			return false;
		}
		ActiveBackendCoordinator.Activate(this);
		bool opened = false;
		lock (_opLock)
		{
			if (Volatile.Read(ref _disposed) != 0 || !ActiveBackendCoordinator.IsActive(this))
			{
				return false;
			}
			Volatile.Write(ref _acceptEvents, 0);
			Interlocked.Increment(ref _gen);
			Interlocked.Exchange(ref _stoppedSinceOpen, 1);
			_vlc.Stop();
			if (!_vlc.IsAvailable)
			{
				_vlc.Initialize();
			}
			if (_vlc.IsAvailable)
			{
				Volatile.Write(ref _openedTimestamp, Stopwatch.GetTimestamp());
				Volatile.Write(ref _acceptEvents, 1);
				if (_vlc.Open(filePath, startPaused))
				{
					Interlocked.Exchange(ref _stoppedSinceOpen, 0);
					opened = true;
				}
				else
				{
					Volatile.Write(ref _acceptEvents, 0);
				}
			}
		}
		if (!opened)
		{
			ActiveBackendCoordinator.Deactivate(this);
		}
		return opened;
	}

	public bool WaitUntilReady(int timeoutMs)
	{
		return _vlc.WaitUntilPlaying(timeoutMs, CancellationToken.None);
	}

	public bool WaitUntilReady(int timeoutMs, CancellationToken cancellationToken)
	{
		return _vlc.WaitUntilPlaying(timeoutMs, cancellationToken);
	}

	public void Play()
	{
		if (Volatile.Read(ref _disposed) != 0 || Volatile.Read(ref _stoppedSinceOpen) != 0)
		{
			return;
		}
		ActiveBackendCoordinator.Activate(this);
		lock (_opLock)
		{
			if (Volatile.Read(ref _disposed) != 0 || Volatile.Read(ref _stoppedSinceOpen) != 0 || !ActiveBackendCoordinator.IsActive(this))
			{
				return;
			}
			_vlc.Play();
		}
	}

	public void Pause()
	{
		lock (_opLock)
		{
			if (Volatile.Read(ref _disposed) != 0)
			{
				return;
			}
			_vlc.Pause();
		}
	}

	public void Stop()
	{
		lock (_opLock)
		{
			Volatile.Write(ref _acceptEvents, 0);
			Interlocked.Increment(ref _gen);
			Interlocked.Exchange(ref _stoppedSinceOpen, 1);
			_vlc.Stop();
		}
		ActiveBackendCoordinator.Deactivate(this);
	}

	public void ForceStopAndClose()
	{
		Stop();
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
		if (Interlocked.Exchange(ref _disposed, 1) != 0)
		{
			return;
		}
		ForceStopAndClose();
		lock (_opLock)
		{
			_vlc.EndReached -= OnVlcEndReached;
			_vlc.EncounteredError -= OnVlcEncounteredError;
			_vlc.Dispose();
		}
	}
}
