using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SonicWave.Audio.Backends;
using SonicWave.Audio.Core;
using SonicWave.Audio.Decoders;
using SonicWave.Audio.Dsp;
using SonicWave.Core.Models;

namespace SonicWave.Audio;

public sealed class AudioEngine : IDisposable
{
	public static AudioEngine? Instance { get; internal set; }

	private readonly AudioSession _session;

	private readonly object _queueLock = new object();

	private Task _workerTail = Task.CompletedTask;

	private long _playbackGeneration;

	private long _latestSeekSeq;

	private int _preparingTrack;

	private CancellationTokenSource? _switchCts;

	private bool _disposed;

	public AudioSession Session => _session;

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
		Instance = this;
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
		_ = EnqueueSerial(() =>
		{
			if (!_disposed)
			{
				_session.ConfigureOutput(mode, deviceId, asioDriver, bufferMs);
			}
		});
	}

	public (double PeakDb, double RmsDb, float[] Bands, float StereoBalance, float PhaseCorrelation, float[] VectorPoints) GetLiveAudioMeters() => _session.GetLiveAudioMeters();

	public double LatestInputPeakDb => _session.LatestInputPeakDb;

	public double LatestOutputPeakDb => _session.LatestOutputPeakDb;

	public double LatestTruePeakDb => _session.LatestTruePeakDb;

	public (string Codec, int SampleRate, int BitDepth, int Channels, string OutputDevice, string OutputEngine, string OutputMode) GetAudioFormatDetails() => _session.GetAudioFormatDetails();

	public void SetQueue(IEnumerable<Track> tracks, int startIndex = 0)
	{
		if (tracks == null)
		{
			return;
		}
		List<Track> snapshot = tracks.ToList();
		_ = EnqueueSerial(() =>
		{
			if (!_disposed)
			{
				_session.SetQueue(snapshot, startIndex);
			}
		});
	}

	public void RemoveTrackFromQueue(long trackId)
	{
		_ = EnqueueSerial(() =>
		{
			if (_disposed)
			{
				return;
			}
			_session.RemoveTrackFromQueue(trackId);
			if (_session.Queue.Count == 0)
			{
				lock (_queueLock)
				{
					BeginSwitchGenerationLocked();
				}
				ActiveBackendCoordinator.StopAll();
				_session.Stop();
			}
		});
	}

	public void PlayTrack(Track track)
	{
		_ = PlayTrackAsync(track, CancellationToken.None);
	}

	public Task PlayTrackAsync(Track track, CancellationToken cancellationToken = default)
	{
		if (track == null || cancellationToken.IsCancellationRequested)
		{
			return Task.CompletedTask;
		}

		long myGen;
		CancellationToken ct;
		lock (_queueLock)
		{
			if (_disposed)
			{
				return Task.CompletedTask;
			}
			BeginSwitchGenerationLocked(cancellationToken, out myGen, out ct);
		}

		return EnqueueSerialAsync(async () =>
		{
			if (_disposed || ct.IsCancellationRequested || myGen != Volatile.Read(ref _playbackGeneration))
			{
				return;
			}
			bool resumingPausedSameTrack = _session.CurrentTrack != null
				&& _session.CurrentTrack.Id == track.Id
				&& _session.State == PlaybackState.Paused;
			if (!resumingPausedSameTrack)
			{
				StopBackendsAndSessionOnWorker();
				await PrepareStreamingTrackAsync(track, myGen, ct).ConfigureAwait(false);
			}
			if (_disposed || ct.IsCancellationRequested || myGen != Volatile.Read(ref _playbackGeneration))
			{
				return;
			}
			_session.PlayTrack(track, ct);
		});
	}

	public bool PrepareRestore(TimeSpan position)
	{
		long myGen;
		CancellationToken ct;
		lock (_queueLock)
		{
			if (_disposed)
			{
				return false;
			}
			BeginSwitchGenerationLocked(CancellationToken.None, out myGen, out ct);
		}

		_ = EnqueueSerialAsync(async () =>
		{
			if (_disposed || ct.IsCancellationRequested || myGen != Volatile.Read(ref _playbackGeneration))
			{
				return;
			}
			StopBackendsAndSessionOnWorker();
			Track? target = _session.CurrentTrack;
			if (target != null)
			{
				await PrepareStreamingTrackAsync(target, myGen, ct).ConfigureAwait(false);
			}
			if (_disposed || ct.IsCancellationRequested || myGen != Volatile.Read(ref _playbackGeneration))
			{
				return;
			}
			_session.PrepareRestore(position, ct);
		});
		return true;
	}

	public void Play()
	{
		long myGen;
		CancellationToken ct;
		lock (_queueLock)
		{
			if (_disposed)
			{
				return;
			}
			if (_session.State != PlaybackState.Paused || Volatile.Read(ref _preparingTrack) != 0)
			{
				BeginSwitchGenerationLocked(CancellationToken.None, out myGen, out ct);
			}
			else
			{
				myGen = Volatile.Read(ref _playbackGeneration);
				ct = _switchCts?.Token ?? CancellationToken.None;
			}
		}

		_ = EnqueueSerialAsync(async () =>
		{
			if (_disposed || ct.IsCancellationRequested || myGen != Volatile.Read(ref _playbackGeneration))
			{
				return;
			}
			Track? current = _session.CurrentTrack;
			if (current == null && _session.Queue.Count > 0)
			{
				int idx = Math.Clamp(_session.QueueIndex, 0, _session.Queue.Count - 1);
				current = _session.Queue[idx];
			}
			if (current != null && _session.State != PlaybackState.Paused)
			{
				await PrepareStreamingTrackAsync(current, myGen, ct).ConfigureAwait(false);
			}
			if (_disposed || ct.IsCancellationRequested || myGen != Volatile.Read(ref _playbackGeneration))
			{
				return;
			}
			_session.Play(ct);
		});
	}

	public void Pause()
	{
		bool cancelPendingOpen = Volatile.Read(ref _preparingTrack) != 0 && _session.State != PlaybackState.Playing;
		lock (_queueLock)
		{
			if (_disposed)
			{
				return;
			}
			if (cancelPendingOpen)
			{
				BeginSwitchGenerationLocked();
			}
		}

		_ = EnqueueSerial(() =>
		{
			if (_disposed)
			{
				return;
			}
			if (cancelPendingOpen && _session.State != PlaybackState.Playing)
			{
				StopBackendsAndSessionOnWorker();
				return;
			}
			_session.Pause();
		});
	}

	public void TogglePlayPause()
	{
		bool cancelPendingOpen = Volatile.Read(ref _preparingTrack) != 0 && _session.State != PlaybackState.Playing;
		long myGen;
		CancellationToken ct;
		lock (_queueLock)
		{
			if (_disposed)
			{
				return;
			}
			if (cancelPendingOpen)
			{
				BeginSwitchGenerationLocked(CancellationToken.None, out myGen, out ct);
			}
			else if (_session.State != PlaybackState.Playing && _session.State != PlaybackState.Paused)
			{
				BeginSwitchGenerationLocked(CancellationToken.None, out myGen, out ct);
			}
			else
			{
				myGen = Volatile.Read(ref _playbackGeneration);
				ct = _switchCts?.Token ?? CancellationToken.None;
			}
		}

		_ = EnqueueSerialAsync(async () =>
		{
			if (_disposed || ct.IsCancellationRequested || myGen != Volatile.Read(ref _playbackGeneration))
			{
				return;
			}
			if (cancelPendingOpen && _session.State != PlaybackState.Playing)
			{
				StopBackendsAndSessionOnWorker();
				return;
			}
			if (_session.State != PlaybackState.Playing && _session.State != PlaybackState.Paused)
			{
				Track? current = _session.CurrentTrack;
				if (current != null)
				{
					await PrepareStreamingTrackAsync(current, myGen, ct).ConfigureAwait(false);
				}
				if (_disposed || ct.IsCancellationRequested || myGen != Volatile.Read(ref _playbackGeneration))
				{
					return;
				}
				_session.Play(ct);
			}
			else
			{
				_session.TogglePlayPause();
			}
		});
	}

	public void Stop()
	{
		lock (_queueLock)
		{
			if (_disposed)
			{
				return;
			}
			BeginSwitchGenerationLocked();
		}

		_ = EnqueueSerial(() =>
		{
			if (!_disposed)
			{
				StopBackendsAndSessionOnWorker();
			}
		});
	}

	public bool Next()
	{
		long myGen;
		CancellationToken ct;
		lock (_queueLock)
		{
			if (_disposed || _session.Queue.Count == 0)
			{
				return false;
			}
			BeginSwitchGenerationLocked(CancellationToken.None, out myGen, out ct);
		}

		_ = EnqueueSerialAsync(async () =>
		{
			if (_disposed || ct.IsCancellationRequested || myGen != Volatile.Read(ref _playbackGeneration))
			{
				return;
			}
			StopBackendsAndSessionOnWorker();
			_session.Next(ct);
			await Task.CompletedTask;
		});
		return true;
	}

	public bool Previous()
	{
		long myGen;
		CancellationToken ct;
		lock (_queueLock)
		{
			if (_disposed || _session.Queue.Count == 0)
			{
				return false;
			}
			if (_session.Position > TimeSpan.FromSeconds(3.0))
			{
				_ = EnqueueSerial(() =>
				{
					if (!_disposed)
					{
						_session.Seek(TimeSpan.Zero);
					}
				});
				return true;
			}
			BeginSwitchGenerationLocked(CancellationToken.None, out myGen, out ct);
		}

		_ = EnqueueSerialAsync(async () =>
		{
			if (_disposed || ct.IsCancellationRequested || myGen != Volatile.Read(ref _playbackGeneration))
			{
				return;
			}
			StopBackendsAndSessionOnWorker();
			_session.Previous(ct);
			await Task.CompletedTask;
		});
		return true;
	}

	public void Seek(TimeSpan position)
	{
		long seekSeq = Interlocked.Increment(ref _latestSeekSeq);
		_ = EnqueueSerial(() =>
		{
			if (!_disposed && seekSeq == Interlocked.Read(ref _latestSeekSeq))
			{
				_session.Seek(position);
			}
		});
	}

	private async Task PrepareStreamingTrackAsync(Track track, long gen, CancellationToken ct)
	{
		if (track == null || string.IsNullOrWhiteSpace(track.FilePath))
		{
			return;
		}
		if (!StreamAudioCache.IsNetworkUrl(track.FilePath) || StreamAudioCache.IsNonPlayableWebUrl(track.FilePath))
		{
			return;
		}
		if (StreamAudioCache.TryGetCachedFilePathIfReady(track.FilePath, out string readyFile) && File.Exists(readyFile))
		{
			track.FilePath = readyFile;
			return;
		}

		Interlocked.Exchange(ref _preparingTrack, 1);
		try
		{
			var (ok, cachedPath) = await StreamAudioCache
				.TryGetOrDownloadCachedFileAsync(track.FilePath, timeoutMs: 8000, ct)
				.ConfigureAwait(false);
			if (!ct.IsCancellationRequested && gen == Volatile.Read(ref _playbackGeneration)
				&& ok && !string.IsNullOrEmpty(cachedPath) && File.Exists(cachedPath))
			{
				track.FilePath = cachedPath;
			}
		}
		catch
		{
		}
		finally
		{
			Interlocked.Exchange(ref _preparingTrack, 0);
		}
	}

	private Task EnqueueSerial(Action action)
	{
		return EnqueueSerialAsync(() =>
		{
			action();
			return Task.CompletedTask;
		});
	}

	private Task EnqueueSerialAsync(Func<Task> work)
	{
		lock (_queueLock)
		{
			if (_disposed)
			{
				return Task.CompletedTask;
			}
			Task next = _workerTail.ContinueWith(async _ =>
			{
				try
				{
					await work().ConfigureAwait(false);
				}
				catch (OperationCanceledException)
				{
				}
				catch (Exception ex)
				{
					System.Diagnostics.Debug.WriteLine($"AudioEngine worker error: {ex.Message}");
				}
			}, CancellationToken.None, TaskContinuationOptions.None, TaskScheduler.Default).Unwrap();
			_workerTail = next;
			return next;
		}
	}

	private void BeginSwitchGenerationLocked()
	{
		BeginSwitchGenerationLocked(CancellationToken.None, out _, out _);
	}

	private void BeginSwitchGenerationLocked(CancellationToken externalToken, out long gen, out CancellationToken token)
	{
		gen = Interlocked.Increment(ref _playbackGeneration);
		CancellationTokenSource? previous = _switchCts;
		CancellationTokenSource nextCts = externalToken.CanBeCanceled
			? CancellationTokenSource.CreateLinkedTokenSource(externalToken)
			: new CancellationTokenSource();
		_switchCts = nextCts;
		token = nextCts.Token;
		if (previous != null)
		{
			try
			{
				previous.Cancel();
			}
			catch
			{
			}
			try
			{
				previous.Dispose();
			}
			catch
			{
			}
		}
	}

	private void StopBackendsAndSessionOnWorker()
	{
		ActiveBackendCoordinator.StopAll();
		_session.Stop();
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

	public EchoDspChain? DspChain => _session.DspChain;

	public void SetEchoHeadroom(double headroomDb) => _session.SetEchoHeadroom(headroomDb);
	public void SetEchoHeadroom(bool enabled, double headroomDb) => _session.SetEchoHeadroom(enabled, headroomDb);

	public void SetEchoCompressor(bool enabled, float thresholdDb, float ratio, float attackMs, float releaseMs, float kneeDb, float makeupDb, float mix)
		=> _session.SetEchoCompressor(enabled, thresholdDb, ratio, attackMs, releaseMs, kneeDb, makeupDb, mix);

	public void SetFurinaMultiband(bool enabled, float lowCrossoverHz, float highCrossoverHz, float mix, float stereoLink)
		=> _session.SetFurinaMultiband(enabled, lowCrossoverHz, highCrossoverHz, mix, stereoLink);

	public void SetFurinaMultibandBand(int bandIndex, bool enabled, float thresholdDb, float ratio, float attackMs, float releaseMs, float kneeDb, float makeupDb)
		=> _session.SetFurinaMultibandBand(bandIndex, enabled, thresholdDb, ratio, attackMs, releaseMs, kneeDb, makeupDb);

	public void SetCompressor(bool enabled, float thresholdDb, float ratio, float attackMs, float releaseMs, float kneeDb, float makeupDb, float mix, CompressorDetectorMode detectorMode = CompressorDetectorMode.Peak, bool sidechainHp = false, float sidechainHpHz = 80f, bool autoRelease = false, float rangeDb = 60f, float stereoLink = 1f)
		=> _session.SetCompressor(enabled, thresholdDb, ratio, attackMs, releaseMs, kneeDb, makeupDb, mix, detectorMode, sidechainHp, sidechainHpHz, autoRelease, rangeDb, stereoLink);

	public void SetEchoCrossfeed(bool enabled, float amount, float cutoffHz)
		=> _session.SetEchoCrossfeed(enabled, amount, cutoffHz);

	public void SetFurinaCrossfeed(bool enabled, float amount, float cutoffHz)
		=> _session.SetFurinaCrossfeed(enabled, amount, cutoffHz);

	public void SetEchoStereoField(bool enabled, float width, float centerGainDb, float sideGainDb)
		=> _session.SetEchoStereoField(enabled, width, centerGainDb, sideGainDb);

	public void SetFurinaStereoField(bool enabled, float width, float midGainDb, float sideGainDb)
		=> _session.SetFurinaStereoField(enabled, width, midGainDb, sideGainDb);

	public void SetEchoChannelMatrix(ChannelMatrixPreset preset)
		=> _session.SetEchoChannelMatrix(preset);

	public void SetFurinaChannelMatrixDirect(bool enabled, float l2l, float r2l, float l2r, float r2r)
		=> _session.SetFurinaChannelMatrixDirect(enabled, l2l, r2l, l2r, r2r);

	public void SetFurinaVirtualStage(bool enabled, float strength, float azimuth, float reflection, bool hrtf, string hrtfPreset)
		=> _session.SetFurinaVirtualStage(enabled, strength, azimuth, reflection, hrtf, hrtfPreset);

	public void SetFurinaDeEsser(bool enabled, float freqHz, float q, float threshDb, float ratio, float kneeDb, float maxRedDb, float attackMs, float releaseMs, float mix, bool auditionBand)
		=> _session.SetFurinaDeEsser(enabled, freqHz, q, threshDb, ratio, kneeDb, maxRedDb, attackMs, releaseMs, mix, auditionBand);

	public void SetFurinaLimiter(bool enabled, bool autoCeiling, float manualCeilingDb, float releaseMs)
		=> _session.SetFurinaLimiter(enabled, autoCeiling, manualCeilingDb, releaseMs);

	public void SetFurinaChannelTool(
		bool enabled,
		double balance,
		double leftGainDb,
		double rightGainDb,
		bool constantPower = false,
		double leftDelayMs = 0.0,
		double rightDelayMs = 0.0,
		double lowLeftGainDb = 0.0,
		double lowRightGainDb = 0.0,
		double midLeftGainDb = 0.0,
		double midRightGainDb = 0.0,
		double highLeftGainDb = 0.0,
		double highRightGainDb = 0.0,
		ChannelMonoMode monoMode = ChannelMonoMode.Off,
		bool swapLeftRight = false,
		bool invertLeft = false,
		bool invertRight = false,
		bool compareBypassed = false)
		=> _session.SetFurinaChannelTool(
			enabled, balance, leftGainDb, rightGainDb, constantPower,
			leftDelayMs, rightDelayMs, lowLeftGainDb, lowRightGainDb,
			midLeftGainDb, midRightGainDb, highLeftGainDb, highRightGainDb,
			monoMode, swapLeftRight, invertLeft, invertRight, compareBypassed);

	public void SetFurinaReplayGain(ReplayGainMode mode, float targetLufs, float preampDb, float unknownDb, bool preventClipping)
		=> _session.SetFurinaReplayGain(mode, targetLufs, preampDb, unknownDb, preventClipping);

	public void SetEchoReplayGain(ReplayGainMode mode, float preampDb, bool preventClipping)
		=> _session.SetEchoReplayGain(mode, preampDb, preventClipping);

	public bool LoadEchoConvolution(string filePath, float trimDb)
		=> _session.LoadEchoConvolution(filePath, trimDb);

	public bool LoadFurinaPlayerConvolution(string filePath, float trimDb)
		=> _session.LoadFurinaPlayerConvolution(filePath, trimDb);

	public void ClearEchoConvolution()
		=> _session.ClearEchoConvolution();

	public void ClearFurinaPlayerConvolution()
		=> _session.ClearFurinaPlayerConvolution();

	public void SetEchoEqBand(int index, EchoFilterType type, double freqHz, double gainDb, double q, bool enabled)
	{
		var eq = DspChain?.Eq;
		if (eq != null && index >= 0 && index < eq.UserBands.Count)
		{
			var band = eq.UserBands[index];
			band.FilterType = type;
			band.FrequencyHz = freqHz;
			band.GainDb = gainDb;
			band.Q = q;
			band.Enabled = enabled;
			band.UpdateCoefficients(48000.0);
		}
	}

	public void SetEchoEqPreamp(double preampDb)
	{
		var eq = DspChain?.Eq;
		if (eq != null)
		{
			eq.PreampDb = preampDb;
		}
	}

	public void SetEchoEqEnabled(bool enabled)
	{
		var eq = DspChain?.Eq;
		if (eq != null)
		{
			eq.Enabled = enabled;
		}
	}

	public void Dispose()
	{
		Task? pendingTail = null;
		lock (_queueLock)
		{
			if (_disposed)
			{
				return;
			}
			_disposed = true;
			BeginSwitchGenerationLocked();
			_switchCts?.Dispose();
			_switchCts = null;
			pendingTail = _workerTail;
		}
		try
		{
			pendingTail?.Wait(1500);
		}
		catch
		{
		}
		ActiveBackendCoordinator.StopAll();
		_session.Dispose();
	}
}
