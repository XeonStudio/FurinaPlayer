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
using SonicWave.Audio.Output;
using SonicWave.Audio.Vst3;
using SonicWave.Core.Models;

namespace SonicWave.Audio;

public sealed class AudioSession : IDisposable
{
	public static AudioSession? Instance { get; internal set; }
	private readonly Timer _positionTimer;

	private readonly Timer _eqApplyTimer;

	private readonly Timer _renderRestartTimer;

	private readonly Random _random = new Random();

	private readonly IAudioBackend _vlcBackend;

	private readonly IAudioBackend _naudioBackend;

	private OutputMode _outputMode;

	private string? _deviceId;

	private string? _asioDriver;

	private bool _useVlc;

	private bool _vlcFallbackActive;

	private PlaybackState _state;

	private int _runToken;

	private long _sessionSwitchSeq;

	private int _isSwitchingBackend;

	private int _positionTickActive;

	private bool _restorePending;

	private long _activeTrackId = -1L;

	private TimeSpan _restorePosition;

	private TimeSpan _abStart = TimeSpan.MinValue;

	private TimeSpan _abEnd = TimeSpan.MinValue;

	private int _volume = 80;

	private double[] _eqGains = new double[10];

	private double[] _pendingEq = new double[10];

	private bool _eqPending;

	private bool _reverbEnabled;

	private double _reverbMix = 0.2;

	private string _reverbPreset = "大厅";

	private double _spatialWidth = 0.6;

	private double _spatialModulation = 0.25;

	private double _spatialPredelayMs = 25.0;

	private TimbreSettings _timbre = new TimbreSettings();

	private double _speed = 1.0;

	private IReadOnlyList<Vst3PluginState> _vst3 = Array.Empty<Vst3PluginState>();

	private CancellationTokenSource? _renderCts;

	private bool _rendering;

	private string? _renderedWavPath;

	private TimeSpan _renderResumeAt;

	private bool _renderRestartScheduled;

	private readonly VlcEndDetector _vlcEnd = new VlcEndDetector();

	private readonly PlaybackClock _clock = new PlaybackClock();

	private readonly PlaybackWatchdog _watchdog = new PlaybackWatchdog(TimeSpan.FromSeconds(5.0), TimeSpan.FromSeconds(3.0));

	private int _recoveryCount;

	private DateTime _lastEndedUtc = DateTime.MinValue;

	private long _lastEndedTrackId = -1L;

	private bool _inPositionGetter;

	private long _cachedVlcPositionTicks;

	private long _cachedVlcDurationTicks;

	private List<Track> _queue = new List<Track>();

	private int _index = -1;

	public bool IsVlcAvailable => _vlcBackend.IsAvailable;

	public PlaybackState State => _state;

	public int CurrentSampleRate => _naudioBackend.SampleRate;

	public Track? CurrentTrack
	{
		get
		{
			if (_index < 0 || _index >= _queue.Count)
			{
				return null;
			}
			return _queue[_index];
		}
	}

	public IReadOnlyList<Track> Queue => _queue;

	public int QueueIndex => _index;

	public PlaybackMode Mode { get; set; }

	public TimeSpan Position
	{
		get
		{
			if (_inPositionGetter)
			{
				return TimeSpan.FromTicks(Interlocked.Read(ref _cachedVlcPositionTicks));
			}
			if (_useVlc && Volatile.Read(ref _isSwitchingBackend) > 0)
			{
				return TimeSpan.FromTicks(Interlocked.Read(ref _cachedVlcPositionTicks));
			}
			_inPositionGetter = true;
			try
			{
				TimeSpan rawPos = CurrentBackend.Position;
				if (_useVlc)
				{
					Interlocked.Exchange(ref _cachedVlcPositionTicks, rawPos.Ticks);
				}
				TimeSpan timeSpan = _clock.Guard(rawPos, seekPending: false);
				if (State != PlaybackState.Stopped && _abEnd > TimeSpan.Zero && timeSpan >= _abEnd)
				{
					Seek(_abStart);
					return _abStart;
				}
				return timeSpan;
			}
			finally
			{
				_inPositionGetter = false;
			}
		}
	}

	public TimeSpan Duration
	{
		get
		{
			if (CurrentTrack != null && CurrentTrack.DurationMilliseconds > 0)
			{
				return TimeSpan.FromMilliseconds(CurrentTrack.DurationMilliseconds);
			}
			if (_useVlc && Volatile.Read(ref _isSwitchingBackend) > 0)
			{
				return TimeSpan.FromTicks(Interlocked.Read(ref _cachedVlcDurationTicks));
			}
			TimeSpan dur = CurrentBackend.Duration;
			if (_useVlc && dur > TimeSpan.Zero)
			{
				Interlocked.Exchange(ref _cachedVlcDurationTicks, dur.Ticks);
			}
			return dur;
		}
	}

	public int Volume
	{
		get
		{
			return _volume;
		}
		set
		{
			int v = Math.Clamp(value, 0, 100);
			if (v != _volume)
			{
				_volume = v;
				ApplyVolume();
				SafeRaise(() =>
				{
					VolumeChanged?.Invoke(v);
				}, "VolumeChanged");
			}
		}
	}

	public double Speed
	{
		get
		{
			return _speed;
		}
		set
		{
			_speed = Math.Clamp(value, 0.5, 3.0);
			try
			{
				if (_useVlc && _vlcBackend.IsAvailable)
				{
					_vlcBackend.Speed = _speed;
				}
			}
			catch
			{
			}
		}
	}

	private IAudioBackend CurrentBackend
	{
		get
		{
			if (!_useVlc)
			{
				return _naudioBackend;
			}
			return _vlcBackend;
		}
	}

	private bool RequiresNaudioPath
	{
		get
		{
			// Always route through NAudio by default so that DspSampleProvider
			// processes audio with real-time 1024 FFT spectrum, EQ, VST3, and stereo phase/balance meters.
			return true;
		}
	}

	private bool Vst3Active
	{
		get
		{
			if (_vst3 != null)
			{
				return _vst3.Any((Vst3PluginState p) => p?.IsActive ?? false);
			}
			return false;
		}
	}

	private bool Vst3NativeActive
	{
		get
		{
			if (Vst3NativeHost.Instance.IsLoaded && _vst3 != null)
			{
				return _vst3.Any((Vst3PluginState p) => p != null && p.IsActive && Vst3NativeHost.Instance.IsLoadedFor(p.Path));
			}
			return false;
		}
	}

	private RenderSettings CurrentRenderSettings => new RenderSettings(_eqGains, _reverbEnabled, _reverbMix, _reverbPreset, _timbre, new SpatialParameters(_spatialWidth, _spatialModulation, _spatialPredelayMs), _vst3);

	private bool RequiresPrerender => false;

	public event Action<Track?>? TrackChanged;

	public event Action<PlaybackState>? StateChanged;

	public event Action<TimeSpan>? PositionChanged;

	public event Action<Track>? TrackEnded;

	public event Action<Track, PlaybackErrorKind, string>? PlaybackError;

	public event Action<bool>? RenderingChanged;

	public event Action<RenderProgress>? RenderProgress;

	public event Action<int>? VolumeChanged;

	public event Action<PlaybackStatus>? StatusChanged;

	internal AudioSession(Func<IAudioBackend>? vlcFactory = null, Func<IAudioBackend>? naudioFactory = null)
	{
		Instance = this;
		_vlcBackend = vlcFactory?.Invoke() ?? new VlcAudioBackend();
		_naudioBackend = naudioFactory?.Invoke() ?? new NaudioAudioBackend(null, null, 200);
		_vlcBackend.Ended += OnBackendEnded;
		_vlcBackend.Failed += OnVlcFailed;
		_naudioBackend.Ended += OnBackendEnded;
		_naudioBackend.Failed += OnNaudioFailed;
		_positionTimer = new Timer((object? _) =>
		{
			RaisePositionChanged();
		}, null, -1, -1);
		_eqApplyTimer = new Timer((object? _) =>
		{
			ApplyPendingEq();
		}, null, -1, -1);
		_renderRestartTimer = new Timer((object? _) =>
		{
			RestartRenderedPlaybackIfNeeded();
		}, null, -1, -1);
		Trace("session created");
	}

	private static void Trace(string message)
	{
		try
		{
			File.AppendAllText(Path.Combine(AppContext.BaseDirectory, "sw_engine.log"), DateTime.Now.ToString("HH:mm:ss.fff") + " " + message + Environment.NewLine);
		}
		catch
		{
		}
	}

	private static void SafeRaise(Action? handler, string name)
	{
		try
		{
			handler?.Invoke();
		}
		catch (Exception ex)
		{
			Trace(name + " subscriber error: " + ex);
		}
	}

	public void ConfigureOutput(OutputMode mode, string? deviceId = null, string? asioDriver = null, int bufferMs = 200)
	{
		_outputMode = mode;
		_deviceId = deviceId;
		_asioDriver = asioDriver;
		_naudioBackend.Configure(deviceId, asioDriver, bufferMs);
		_useVlc = mode == OutputMode.SoftwareDecode && !RequiresNaudioPath;
		_vlcFallbackActive = false;
	}

	public (double PeakDb, double RmsDb, float[] Bands, float StereoBalance, float PhaseCorrelation, float[] VectorPoints) GetLiveAudioMeters()
	{
		if (!_useVlc && _naudioBackend is NaudioAudioBackend naudio)
		{
			var dsp = naudio.Decoder.Dsp;
			if (dsp != null && State == PlaybackState.Playing)
			{
				double peakDb = SanitizeDb(dsp.LatestPeakDb);
				double rmsDb = SanitizeDb(dsp.LatestRmsDb);
				float[] bands = SanitizeFloatArray(dsp.LatestBands, 0f, 1f, 0f);
				float balance = SanitizeUnitSigned(dsp.LatestStereoBalance, 0f);
				float phase = SanitizeUnitSigned(dsp.LatestPhaseCorrelation, 1f);
				float[] vectorPoints = SanitizeFloatArray(dsp.LatestVectorPoints, -1f, 1f, 0f);
				return (peakDb, rmsDb, bands, balance, phase, vectorPoints);
			}
		}
		return (-90.0, -90.0, Array.Empty<float>(), 0f, 1f, Array.Empty<float>());
	}

	public double LatestInputPeakDb
	{
		get
		{
			if (!_useVlc && _naudioBackend is NaudioAudioBackend naudio)
			{
				var dsp = naudio.Decoder.Dsp;
				if (dsp != null && State == PlaybackState.Playing)
				{
					return SanitizeDb(dsp.LatestInputPeakDb);
				}
			}
			return -90.0;
		}
	}

	public double LatestOutputPeakDb => GetLiveAudioMeters().PeakDb;

	public double LatestTruePeakDb
	{
		get
		{
			if (!_useVlc && _naudioBackend is NaudioAudioBackend naudio)
			{
				var dsp = naudio.Decoder.Dsp;
				if (dsp != null && State == PlaybackState.Playing)
				{
					return SanitizeDb(dsp.LatestTruePeakDb);
				}
			}
			return -90.0;
		}
	}

	private static double SanitizeDb(double value) => double.IsFinite(value) ? value : -90.0;

	private static float SanitizeUnitSigned(float value, float fallback = 0f) =>
		float.IsFinite(value) ? Math.Clamp(value, -1f, 1f) : fallback;

	private static float[] SanitizeFloatArray(float[]? source, float min, float max, float fallback = 0f)
	{
		if (source == null || source.Length == 0)
		{
			return Array.Empty<float>();
		}
		for (int i = 0; i < source.Length; i++)
		{
			float v = source[i];
			if (!float.IsFinite(v) || v < min || v > max)
			{
				float[] copy = new float[source.Length];
				for (int j = 0; j < source.Length; j++)
				{
					float s = source[j];
					copy[j] = float.IsFinite(s) ? Math.Clamp(s, min, max) : fallback;
				}
				return copy;
			}
		}
		return source;
	}

	public (string Codec, int SampleRate, int BitDepth, int Channels, string OutputDevice, string OutputEngine, string OutputMode) GetAudioFormatDetails()
	{
		var track = CurrentTrack;
		string codec = "PCM";
		if (!string.IsNullOrEmpty(track?.FilePath))
		{
			try
			{
				string cleanPath = track.FilePath;
				int qIdx = cleanPath.IndexOf('?');
				if (qIdx >= 0)
				{
					cleanPath = cleanPath[..qIdx];
				}
				if (Uri.TryCreate(cleanPath, UriKind.Absolute, out Uri? uri) && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps))
				{
					cleanPath = uri.AbsolutePath;
				}
				string ext = System.IO.Path.GetExtension(cleanPath).TrimStart('.').ToUpperInvariant();
				codec = string.IsNullOrEmpty(ext) ? (IsNetworkUrl(track.FilePath) ? "MP3" : "PCM") : ext;
			}
			catch
			{
				codec = "PCM";
			}
		}

		int sr = 0;
		int bits = 0;
		int ch = 2;

		if (!_useVlc && _naudioBackend is NaudioAudioBackend naudio)
		{
			sr = naudio.Decoder.SampleRate;
			bits = naudio.Decoder.BitsPerSample;
			ch = naudio.Decoder.Channels;
		}

		if (sr <= 0 && track?.SampleRate > 0) sr = track.SampleRate;
		if (sr <= 0) sr = 48000;

		if (bits <= 0 && track?.BitDepth > 0) bits = track.BitDepth;
		if (bits <= 0) bits = 24;

		if (ch <= 0) ch = 2;

		string device = !string.IsNullOrEmpty(_asioDriver) ? _asioDriver : (!string.IsNullOrEmpty(_deviceId) ? _deviceId : "Default WASAPI Device");
		string engine = _useVlc ? "VLC Direct" : (!string.IsNullOrEmpty(_asioDriver) ? "ASIO Exclusive" : "NAudio WASAPI");
		string mode = _outputMode.ToString();

		return (codec, sr, bits, ch, device, engine, mode);
	}

	public void SetQueue(IEnumerable<Track> tracks, int startIndex = 0)
	{
		_queue = tracks.ToList();
		_index = Math.Clamp(startIndex, 0, Math.Max(0, _queue.Count - 1));
	}

	public void RemoveTrackFromQueue(long trackId)
	{
		int num = _queue.FindIndex((Track t) => t.Id == trackId);
		if (num < 0)
		{
			return;
		}
		bool flag = num == _index;
		_queue.RemoveAt(num);
		if (_queue.Count == 0)
		{
			Stop();
			_index = -1;
		}
		else if (flag)
		{
			if (State != PlaybackState.Stopped)
			{
				_index = Math.Min(num, _queue.Count - 1);
				PlayTrack(_queue[_index]);
			}
			else
			{
				_index = Math.Min(num, _queue.Count - 1);
			}
		}
		else if (num < _index)
		{
			_index--;
		}
	}

	public void PlayTrack(Track track) => PlayTrack(track, CancellationToken.None);

	public void PlayTrack(Track track, CancellationToken cancellationToken)
	{
		if (cancellationToken.IsCancellationRequested)
		{
			return;
		}
		Trace("PlayTrack: " + track.FilePath);
		int num = _queue.FindIndex((Track t) => t.Id == track.Id);
		if (num < 0)
		{
			_queue.Add(track);
			num = _queue.Count - 1;
		}
		_index = num;
		_restorePending = false;
		CancelRender();

		// 如果当前暂停的曲目正是所选曲目，直接恢复播放，避免重置进度到开头
		if (CurrentTrack != null && CurrentTrack.Id == track.Id && State == PlaybackState.Paused && _activeTrackId == track.Id)
		{
			Play(cancellationToken);
			return;
		}

		Interlocked.Increment(ref _sessionSwitchSeq);
		Interlocked.Exchange(ref _cachedVlcPositionTicks, 0);
		Interlocked.Exchange(ref _cachedVlcDurationTicks, 0);
		_positionTimer.Change(-1, -1);
		_activeTrackId = -1L;
		Play(cancellationToken);
	}

	public bool PrepareRestore(TimeSpan position) => PrepareRestore(position, CancellationToken.None);

	public bool PrepareRestore(TimeSpan position, CancellationToken cancellationToken)
	{
		if (cancellationToken.IsCancellationRequested)
		{
			return false;
		}
		Track track = CurrentTrack;
		if (track == null || (!File.Exists(track.FilePath) && !IsNetworkUrl(track.FilePath)))
		{
			return false;
		}
		if (IsNetworkUrl(track.FilePath))
		{
			if (StreamAudioCache.IsNonPlayableWebUrl(track.FilePath))
			{
				Trace("PrepareRestore rejected non-playable web URL: " + track.FilePath);
				return false;
			}
			try
			{
				if (StreamAudioCache.TryGetCachedFilePathIfReady(track.FilePath, out string readyFile) && File.Exists(readyFile))
				{
					track.FilePath = readyFile;
				}
				else if (StreamAudioCache.TryGetOrDownloadCachedFile(track.FilePath, out string cachedFile, 5000, cancellationToken) && File.Exists(cachedFile))
				{
					track.FilePath = cachedFile;
				}
			}
			catch (Exception ex)
			{
				Trace("PrepareRestore cache failed: " + ex.Message);
			}
		}
		if (cancellationToken.IsCancellationRequested)
		{
			return false;
		}
		Interlocked.Increment(ref _sessionSwitchSeq);
		_restorePending = false;
		_positionTimer.Change(-1, -1);
		StopBackendsForSwitch();
		_restorePosition = position;
		if (RequiresNaudioPath)
		{
			_useVlc = false;
			_vlcFallbackActive = false;
		}
		bool flag = OpenWithNaudio(track.FilePath);
		if (!flag && !cancellationToken.IsCancellationRequested && IsVlcAvailable && !StreamAudioCache.IsNonPlayableWebUrl(track.FilePath))
		{
			Trace("NAudio failed to open file, falling back to VLC: " + track.FilePath);
			flag = _vlcBackend.Open(track.FilePath, startPaused: false);
			if (flag)
			{
				flag = _vlcBackend is VlcAudioBackend vlc
					? vlc.WaitUntilReady(2000, cancellationToken)
					: _vlcBackend.WaitUntilReady(2000);
				if (flag)
				{
					_useVlc = true;
					_vlcFallbackActive = true;
					_vlcBackend.Speed = _speed;
					_vlcBackend.Seek(position);
					_vlcBackend.Pause();
				}
				else
				{
					_vlcBackend.Stop();
				}
			}
		}
		if (cancellationToken.IsCancellationRequested)
		{
			StopBackendsForSwitch();
			return false;
		}
		if (flag && track.DurationMilliseconds <= 0 && CurrentBackend.Duration.TotalMilliseconds > 0)
		{
			track.DurationMilliseconds = (long)CurrentBackend.Duration.TotalMilliseconds;
		}
		SafeRaise(() =>
		{
			TrackChanged?.Invoke(track);
		}, "TrackChanged");
		if (flag)
		{
			if (!_useVlc)
			{
				_naudioBackend.Seek(position);
			}
			if (_activeTrackId != -1)
			{
				Stop();
				return false;
			}
			_restorePending = true;
			SetState(PlaybackState.Stopped);
			SafeRaise(() =>
			{
				PositionChanged?.Invoke(position);
			}, "PositionChanged");
			_clock.Reset(position);
			_watchdog.Reset(position);
			Trace("PrepareRestore OK @" + position);
		}
		return flag;
	}

	public void Play() => Play(CancellationToken.None);

	public void Play(CancellationToken cancellationToken)
	{
		if (cancellationToken.IsCancellationRequested)
		{
			return;
		}
		_recoveryCount = 0;
		if (CurrentTrack == null)
		{
			if (_queue.Count > 0)
			{
				_index = 0;
			}
			else
			{
				Trace("Play: no current track and queue is empty");
				return;
			}
		}
		bool flag = CurrentTrack.Id == _activeTrackId;
		if (!flag)
		{
			TryDelete(_renderedWavPath);
			_renderedWavPath = null;
		}
		if (_rendering)
		{
			if (flag)
			{
				Trace("Play: rendering in progress, ignored");
				return;
			}
			CancelRender();
			TryDelete(_renderedWavPath);
			_renderedWavPath = null;
			_restorePending = false;
			Trace("Play: cancel render, switching track");
		}
		if (State == PlaybackState.Paused)
		{
			if (flag)
			{
				CurrentBackend.Play();
				SetState(PlaybackState.Playing);
				_positionTimer.Change(0, 50);
				_watchdog.Reset(Position);
				Trace("Play: resumed from pause (" + (_useVlc ? "vlc" : "naudio") + ")");
				return;
			}
			else
			{
				Trace("Play: paused but different track requested, switching track");
			}
		}
		else if ((State == PlaybackState.Playing && IsDecoderActuallyPlaying()) & flag)
		{
			Trace("Play: already playing same track, ignored");
			return;
		}
		if (_restorePending)
		{
			_restorePending = false;
			if (!_useVlc && RequiresPrerender)
			{
				StartRenderedPlayback(Position);
				return;
			}
			if (_useVlc)
			{
				_vlcBackend.Play();
				_vlcBackend.Speed = _speed;
				_vlcBackend.Seek(_restorePosition);
			}
			else
			{
				_naudioBackend.Play();
			}
			_activeTrackId = CurrentTrack.Id;
			SetState(PlaybackState.Playing);
			_positionTimer.Change(0, 50);
			_watchdog.Reset(Position);
			Trace("Play: resume restored session");
		}
		else if (RequiresPrerender)
		{
			StartRenderedPlayback(TimeSpan.Zero);
		}
		else if (!OpenCurrent(cancellationToken))
		{
			_activeTrackId = -1L;
			_positionTimer.Change(-1, -1);
			_restorePending = false;
			SetState(PlaybackState.Stopped);
			Trace("Play: open failed, stopped");
		}
		else if (cancellationToken.IsCancellationRequested)
		{
			StopBackendsForSwitch();
			_activeTrackId = -1L;
			_positionTimer.Change(-1, -1);
			SetState(PlaybackState.Stopped);
		}
		else
		{
			_activeTrackId = CurrentTrack.Id;
			if (_useVlc)
			{
				_vlcBackend.Speed = _speed;
				_vlcBackend.Play();
			}
			else
			{
				_naudioBackend.Play();
			}
			SetState(PlaybackState.Playing);
			_positionTimer.Change(0, 50);
			_watchdog.Reset(Position);
			Trace("Play: playing (" + (_useVlc ? "vlc" : "naudio") + ")");
		}
	}

	public void Pause()
	{
		_vlcEnd.Reset();
		_watchdog.Reset(Position);
		_positionTimer.Change(-1, -1);
		CurrentBackend.Pause();
		SetState(PlaybackState.Paused);
	}

	public void TogglePlayPause()
	{
		if (State == PlaybackState.Playing)
		{
			if (!IsDecoderActuallyPlaying())
			{
				Play();
			}
			else
			{
				Pause();
			}
		}
		else if (State == PlaybackState.Paused)
		{
			Play();
		}
		else
		{
			Play();
		}
	}

	private bool IsDecoderActuallyPlaying()
	{
		try
		{
			return CurrentBackend.IsActuallyPlaying;
		}
		catch
		{
			return false;
		}
	}

	private void StopBackendsForSwitch()
	{
		Interlocked.Increment(ref _sessionSwitchSeq);
		Interlocked.Increment(ref _isSwitchingBackend);
		try
		{
			try { _naudioBackend.Stop(); } catch { }
			try { _vlcBackend.Stop(); } catch { }
		}
		finally
		{
			Interlocked.Decrement(ref _isSwitchingBackend);
			Interlocked.Increment(ref _sessionSwitchSeq);
		}
	}

	public void Stop()
	{
		Interlocked.Increment(ref _sessionSwitchSeq);
		_recoveryCount = 0;
		_runToken++;
		_vlcEnd.Reset();
		_watchdog.Reset(TimeSpan.Zero);
		_restorePending = false;
		_activeTrackId = -1L;
		_renderRestartScheduled = false;
		try
		{
			_renderRestartTimer.Change(-1, -1);
		}
		catch
		{
		}
		CancelRender();
		_positionTimer.Change(-1, -1);
		// Always stop both backends cleanly so neither NAudio nor LibVLC lingers after switching
		StopBackendsForSwitch();
		_useVlc = _outputMode == OutputMode.SoftwareDecode && !RequiresNaudioPath;
		_vlcFallbackActive = false;
		TryDelete(_renderedWavPath);
		_renderedWavPath = null;
		SetState(PlaybackState.Stopped);
	}

	public bool Next() => Next(CancellationToken.None);

	public bool Next(CancellationToken cancellationToken)
	{
		if (_queue.Count == 0 || cancellationToken.IsCancellationRequested)
		{
			return false;
		}
		Interlocked.Increment(ref _sessionSwitchSeq);
		_positionTimer.Change(-1, -1);
		_index = Mode switch
		{
			PlaybackMode.Shuffle => (_queue.Count > 1) ? GetRandomIndexExcept(_index) : 0, 
			PlaybackMode.Sequential => (_index + 1 < _queue.Count) ? (_index + 1) : -1, 
			_ => (_index + 1) % _queue.Count, 
		};
		if (_index < 0)
		{
			Stop();
			return false;
		}
		_activeTrackId = -1L;
		PlayTrack(_queue[_index], cancellationToken);
		return true;
	}

	public bool Previous() => Previous(CancellationToken.None);

	public bool Previous(CancellationToken cancellationToken)
	{
		if (_queue.Count == 0 || cancellationToken.IsCancellationRequested)
		{
			return false;
		}
		if (Position > TimeSpan.FromSeconds(3.0))
		{
			Seek(TimeSpan.Zero);
			return true;
		}
		Interlocked.Increment(ref _sessionSwitchSeq);
		_positionTimer.Change(-1, -1);
		_index = Mode switch
		{
			PlaybackMode.Shuffle => (_queue.Count > 1) ? GetRandomIndexExcept(_index) : 0, 
			PlaybackMode.Sequential => (_index - 1 >= 0) ? (_index - 1) : 0, 
			_ => (_index - 1 + _queue.Count) % _queue.Count, 
		};
		_activeTrackId = -1L;
		PlayTrack(_queue[_index], cancellationToken);
		return true;
	}

	private int GetRandomIndexExcept(int current)
	{
		if (_queue.Count <= 1)
		{
			return 0;
		}
		int next;
		do
		{
			next = _random.Next(_queue.Count);
		} while (next == current);
		return next;
	}

	public void Seek(TimeSpan position)
	{
		_vlcEnd.Reset();
		_clock.Rebase(position);
		_watchdog.Reset(position);
		if (_useVlc)
		{
			_vlcBackend.Seek(position);
		}
		else
		{
			_naudioBackend.Seek(position);
		}
		if (_rendering)
		{
			_renderResumeAt = position;
		}
		if (_abStart != TimeSpan.MinValue && position < _abStart)
		{
			_abStart = position;
		}
		RaisePositionChanged();
	}

	private void ApplyVolume()
	{
		try
		{
			int effectiveVol = _volume > 0 ? _volume : 80;
			CurrentBackend.Volume = effectiveVol;
		}
		catch
		{
		}
	}

	public void SetAbLoop(TimeSpan? a, TimeSpan? b)
	{
		if (a.HasValue && b.HasValue && b > a)
		{
			_abStart = a.Value;
			_abEnd = b.Value;
		}
		else
		{
			_abStart = TimeSpan.MinValue;
			_abEnd = TimeSpan.MinValue;
		}
	}

	public void SetEqGains(double[] gains)
	{
		_eqGains = ((gains != null && gains.Length == 10) ? ((double[])gains.Clone()) : new double[10]);
		_naudioBackend.SetEqGains(_eqGains);
		ScheduleVlcEqApply();
		if (_renderedWavPath != null)
		{
			ScheduleRenderedRestart("eq");
		}
	}

	private void ScheduleVlcEqApply()
	{
		_pendingEq = (double[])_eqGains.Clone();
		_eqPending = true;
		try
		{
			_eqApplyTimer.Change(150, -1);
		}
		catch
		{
			ApplyPendingEq();
		}
	}

	private void ApplyPendingEq()
	{
		if (!_eqPending)
		{
			return;
		}
		_eqPending = false;
		try
		{
			if (_useVlc)
			{
				_vlcBackend.SetEqGains(CombinedEqGains());
			}
		}
		catch (Exception ex)
		{
			Trace("VLC SetEqualizer error: " + ex.Message);
		}
	}

	public void SetSpatialAudio(bool enabled, double mix, string preset, double width, double modulation, double predelayMs)
	{
		_reverbEnabled = enabled;
		_reverbMix = mix;
		_reverbPreset = (string.IsNullOrEmpty(preset) ? "大厅" : preset);
		_spatialWidth = Math.Clamp(width, 0.0, 1.0);
		_spatialModulation = Math.Clamp(modulation, 0.0, 1.0);
		_spatialPredelayMs = Math.Clamp(predelayMs, 0.0, 120.0);
		_naudioBackend.SetSpatialAudio(_reverbEnabled, _reverbMix, _reverbPreset, _spatialWidth, _spatialModulation, _spatialPredelayMs);
		RefreshPlaybackPath("spatial");
		ScheduleRenderedRestart("spatial");
	}

	public void SetReverb(bool enabled, double mix, string preset)
	{
		SetSpatialAudio(enabled, mix, preset, _spatialWidth, _spatialModulation, _spatialPredelayMs);
	}

	public void SetTimbre(TimbreSettings timbre)
	{
		_timbre = timbre ?? new TimbreSettings();
		_naudioBackend.SetTimbre(_timbre);
		ScheduleVlcEqApply();
		RefreshPlaybackPath("timbre");
		ScheduleRenderedRestart("timbre");
	}

	public EchoDspChain? DspChain => (_naudioBackend as NaudioAudioBackend)?.Decoder.Dsp?.DspChain;

	public void SetEchoHeadroom(double headroomDb) => SetEchoHeadroom(true, headroomDb);

	public void SetEchoHeadroom(bool enabled, double headroomDb)
	{
		var chain = DspChain;
		if (chain != null)
		{
			chain.Headroom.Enabled = enabled;
			chain.Headroom.HeadroomDb = headroomDb;
		}
	}

	public void SetEchoCompressor(bool enabled, float thresholdDb, float ratio, float attackMs, float releaseMs, float kneeDb, float makeupDb, float mix)
	{
		SetCompressor(enabled, thresholdDb, ratio, attackMs, releaseMs, kneeDb, makeupDb, mix);
	}

	public void SetCompressor(bool enabled, float thresholdDb, float ratio, float attackMs, float releaseMs, float kneeDb, float makeupDb, float mix, CompressorDetectorMode detectorMode = CompressorDetectorMode.Peak, bool sidechainHp = false, float sidechainHpHz = 80f, bool autoRelease = false, float rangeDb = 60f, float stereoLink = 1f)
	{
		var chain = DspChain;
		if (chain != null)
		{
			chain.Compressor.Enabled = enabled;
			chain.Compressor.ThresholdDb = thresholdDb;
			chain.Compressor.Ratio = ratio;
			chain.Compressor.AttackMs = attackMs;
			chain.Compressor.ReleaseMs = releaseMs;
			chain.Compressor.KneeDb = kneeDb;
			chain.Compressor.MakeupDb = makeupDb;
			chain.Compressor.Mix = mix;
			chain.Compressor.DetectorMode = detectorMode;
			chain.Compressor.SidechainHighpassEnabled = sidechainHp;
			chain.Compressor.SidechainHighpassHz = sidechainHpHz;
			chain.Compressor.AutoRelease = autoRelease;
			chain.Compressor.RangeDb = rangeDb;
			chain.Compressor.StereoLink = stereoLink;
		}
	}

	public void SetFurinaMultiband(bool enabled, float lowCrossoverHz, float highCrossoverHz, float mix, float stereoLink)
	{
		var chain = DspChain;
		if (chain != null)
		{
			chain.Multiband.Enabled = enabled;
			chain.Multiband.LowCrossoverHz = lowCrossoverHz;
			chain.Multiband.HighCrossoverHz = highCrossoverHz;
			chain.Multiband.Mix = mix;
			chain.Multiband.StereoLink = stereoLink;
		}
	}

	public void SetFurinaMultibandBand(int bandIndex, bool enabled, float thresholdDb, float ratio, float attackMs, float releaseMs, float kneeDb, float makeupDb)
	{
		var chain = DspChain;
		if (chain != null && bandIndex >= 0 && bandIndex < chain.Multiband.Bands.Length)
		{
			var band = chain.Multiband.Bands[bandIndex];
			band.Enabled = enabled;
			band.ThresholdDb = thresholdDb;
			band.Ratio = ratio;
			band.AttackMs = attackMs;
			band.ReleaseMs = releaseMs;
			band.KneeDb = kneeDb;
			band.MakeupDb = makeupDb;
		}
	}

	public void SetFurinaCrossfeed(bool enabled, float amount, float cutoffHz)
	{
		SetEchoCrossfeed(enabled, amount, cutoffHz);
	}

	public void SetEchoCrossfeed(bool enabled, float amount, float cutoffHz)
	{
		var chain = DspChain;
		if (chain != null)
		{
			chain.Spatial.CrossfeedEnabled = enabled;
			chain.Spatial.CrossfeedAmount = amount;
			chain.Spatial.CrossfeedCutoffHz = cutoffHz;
		}
	}

	public void SetEchoStereoField(bool enabled, float width, float centerGainDb, float sideGainDb)
	{
		var chain = DspChain;
		if (chain != null)
		{
			chain.Spatial.StereoFieldEnabled = enabled;
			chain.Spatial.StereoWidth = width;
			chain.Spatial.CenterGainDb = centerGainDb;
			chain.Spatial.SideGainDb = sideGainDb;
		}
	}

	public void SetFurinaStereoField(bool enabled, float width, float midGainDb, float sideGainDb)
		=> SetEchoStereoField(enabled, width, midGainDb, sideGainDb);

	public void SetEchoChannelMatrix(ChannelMatrixPreset preset)
	{
		var chain = DspChain;
		if (chain != null)
		{
			chain.Spatial.SetMatrixPreset(preset);
		}
	}

	public void SetFurinaChannelMatrixDirect(bool enabled, float l2l, float r2l, float l2r, float r2r)
	{
		var chain = DspChain;
		if (chain != null)
		{
			chain.Spatial.ChannelMatrixEnabled = enabled;
			chain.Spatial.MatrixLeftToLeft = l2l;
			chain.Spatial.MatrixRightToLeft = r2l;
			chain.Spatial.MatrixLeftToRight = l2r;
			chain.Spatial.MatrixRightToRight = r2r;
		}
	}

	public void SetFurinaVirtualStage(bool enabled, float strength, float azimuth, float reflection, bool hrtf, string hrtfPreset)
	{
		var chain = DspChain;
		if (chain != null)
		{
			chain.Spatial.VirtualStageEnabled = enabled;
			chain.Spatial.VirtualStageStrength = strength;
			chain.Spatial.VirtualStageAzimuth = azimuth;
			chain.Spatial.VirtualStageReflection = reflection;
			chain.Spatial.VirtualStageHrtf = hrtf;
			chain.Spatial.VirtualStageHrtfPreset = hrtfPreset;
		}
	}

	public void SetFurinaDeEsser(bool enabled, float freqHz, float q, float threshDb, float ratio, float kneeDb, float maxRedDb, float attackMs, float releaseMs, float mix, bool auditionBand)
	{
		var chain = DspChain;
		if (chain != null)
		{
			chain.DeEsser.Enabled = enabled;
			chain.DeEsser.FrequencyHz = freqHz;
			chain.DeEsser.Q = q;
			chain.DeEsser.ThresholdDb = threshDb;
			chain.DeEsser.Ratio = ratio;
			chain.DeEsser.KneeDb = kneeDb;
			chain.DeEsser.MaxReductionDb = maxRedDb;
			chain.DeEsser.AttackMs = attackMs;
			chain.DeEsser.ReleaseMs = releaseMs;
			chain.DeEsser.Mix = mix;
			chain.DeEsser.AuditionBand = auditionBand;
		}
	}

	public void SetFurinaLimiter(bool enabled, bool autoCeiling, float manualCeilingDb, float releaseMs)
	{
		var chain = DspChain;
		if (chain != null)
		{
			chain.Limiter.Enabled = enabled;
			chain.Limiter.AutoCeiling = autoCeiling;
			chain.Limiter.ManualCeilingDb = manualCeilingDb;
			chain.Limiter.ReleaseMs = releaseMs;
		}
	}

	public void SetFurinaChannelTool(
		bool enabled,
		double balance,
		double leftGainDb,
		double rightGainDb,
		bool constantPower,
		double leftDelayMs,
		double rightDelayMs,
		double lowLeftGainDb,
		double lowRightGainDb,
		double midLeftGainDb,
		double midRightGainDb,
		double highLeftGainDb,
		double highRightGainDb,
		ChannelMonoMode monoMode,
		bool swapLeftRight,
		bool invertLeft,
		bool invertRight,
		bool compareBypassed = false)
	{
		var chain = DspChain;
		if (chain != null)
		{
			chain.Spatial.ChannelToolEnabled = enabled;
			chain.Spatial.Balance = balance;
			chain.Spatial.LeftGainDb = leftGainDb;
			chain.Spatial.RightGainDb = rightGainDb;
			chain.Spatial.ConstantPower = constantPower;
			chain.Spatial.LeftDelayMs = leftDelayMs;
			chain.Spatial.RightDelayMs = rightDelayMs;
			chain.Spatial.LowLeftGainDb = lowLeftGainDb;
			chain.Spatial.LowRightGainDb = lowRightGainDb;
			chain.Spatial.MidLeftGainDb = midLeftGainDb;
			chain.Spatial.MidRightGainDb = midRightGainDb;
			chain.Spatial.HighLeftGainDb = highLeftGainDb;
			chain.Spatial.HighRightGainDb = highRightGainDb;
			chain.Spatial.MonoMode = monoMode;
			chain.Spatial.SwapLeftRight = swapLeftRight;
			chain.Spatial.InvertPhaseLeft = invertLeft;
			chain.Spatial.InvertPhaseRight = invertRight;
			chain.Spatial.CompareBypassed = compareBypassed;
		}
	}

	public void SetFurinaReplayGain(ReplayGainMode mode, float targetLufs, float preampDb, float unknownDb, bool preventClipping)
	{
		var chain = DspChain;
		if (chain != null)
		{
			chain.ReplayGain.Mode = mode;
			chain.ReplayGain.TargetLufs = targetLufs;
			chain.ReplayGain.PreampDb = preampDb;
			chain.ReplayGain.UnknownLoudnessDb = unknownDb;
			chain.ReplayGain.PreventClipping = preventClipping;
		}
	}

	public void SetEchoReplayGain(ReplayGainMode mode, float preampDb, bool preventClipping)
	{
		SetFurinaReplayGain(mode, -14.0f, preampDb, -5.0f, preventClipping);
	}

	public bool LoadEchoConvolution(string filePath, float trimDb)
	{
		var chain = DspChain;
		if (chain != null)
		{
			chain.Convolution.TrimDb = trimDb;
			bool ok = chain.Convolution.LoadFile(filePath);
			chain.Convolution.Enabled = ok;
			return ok;
		}
		return false;
	}

	public bool LoadFurinaPlayerConvolution(string filePath, float trimDb) => LoadEchoConvolution(filePath, trimDb);

	public void ClearEchoConvolution()
	{
		var chain = DspChain;
		if (chain != null)
		{
			chain.Convolution.ClearImpulseResponse();
			chain.Convolution.Enabled = false;
		}
	}

	public void ClearFurinaPlayerConvolution() => ClearEchoConvolution();

	public void SetVst3(IReadOnlyList<Vst3PluginState> plugins)
	{
		bool vst3Active = Vst3Active;
		bool hadRenderedWav = _renderedWavPath != null;
		Vst3PluginState[] snapshot = plugins != null ? plugins.ToArray() : Array.Empty<Vst3PluginState>();
		_vst3 = snapshot;

		try
		{
			double sampleRate = _naudioBackend.SampleRate > 0
				? _naudioBackend.SampleRate
				: (Vst3NativeHost.Instance.SampleRate > 0 ? Vst3NativeHost.Instance.SampleRate : 44100.0);
			bool hasAnyEnabled = snapshot.Any(p => p != null && p.Enabled && !string.IsNullOrWhiteSpace(p.Path) && Vst3Manager.IsValidVst3Path(p.Path));
			Vst3NativeHost.Instance.EnsureChainLoaded(snapshot, sampleRate, 2, unloadPrimaryWhenEmpty: !hasAnyEnabled);
		}
		catch
		{
		}

		_naudioBackend.SetVst3(snapshot);
		Vst3NativeHost.Instance.SetTransportPlaying(State == PlaybackState.Playing);

		bool vst3Active2 = Vst3Active;
		if (hadRenderedWav)
		{
			CancelRender();
			RestartCleanNaudioChain(vst3Active && !vst3Active2 ? "vst3-disable" : "vst3-enable");
		}
		RefreshPlaybackPath("vst3");
	}

	private void RestartCleanNaudioChain(string reason)
	{
		Track currentTrack = CurrentTrack;
		if (currentTrack == null)
		{
			return;
		}
		bool wasPlaying = State == PlaybackState.Playing;
		bool wasPaused = State == PlaybackState.Paused;
		bool restorePending = _restorePending;
		TimeSpan pos = Position;
		Trace("restart clean naudio chain: " + reason + " @" + pos.ToString() + " " + currentTrack.FilePath);
		_runToken++;
		try
		{
			_positionTimer.Change(-1, -1);
			StopBackendsForSwitch();
			TryDelete(_renderedWavPath);
			_renderedWavPath = null;
			if (RequiresNaudioPath)
			{
				_useVlc = false;
				_vlcFallbackActive = false;
			}
			if (OpenWithNaudio(currentTrack.FilePath))
			{
				_naudioBackend.Seek(pos);
				if (wasPlaying && !restorePending)
				{
					_naudioBackend.Play();
					SetState(PlaybackState.Playing);
					_positionTimer.Change(0, 50);
				}
				else if (wasPaused && !restorePending)
				{
					SetState(PlaybackState.Paused);
				}
				else if (restorePending)
				{
					_restorePending = true;
					SetState(PlaybackState.Stopped);
				}
				else
				{
					SetState(PlaybackState.Stopped);
				}
				SafeRaise(() =>
				{
					PositionChanged?.Invoke(pos);
				}, "PositionChanged");
				Trace("restart clean naudio chain OK @" + pos);
			}
			else
			{
				_restorePending = false;
				SetState(PlaybackState.Stopped);
				Trace("restart clean naudio chain FAILED");
			}
		}
		finally
		{
			_watchdog.Reset(pos);
		}
	}

	private void ScheduleRenderedRestart(string reason)
	{
		if (_renderedWavPath == null || State == PlaybackState.Stopped || !CurrentRenderSettings.IsActive || Vst3NativeActive)
		{
			return;
		}
		Trace("schedule rendered restart: " + reason);
		_renderRestartScheduled = true;
		try
		{
			_renderRestartTimer.Change(600, -1);
		}
		catch
		{
			RestartRenderedPlaybackIfNeeded();
		}
	}

	private void RestartRenderedPlaybackIfNeeded()
	{
		if (_renderRestartScheduled)
		{
			_renderRestartScheduled = false;
			if (_renderedWavPath != null && State != PlaybackState.Stopped && !_useVlc)
			{
				TimeSpan position = Position;
				Trace("restart rendered playback @" + position);
				StartRenderedPlayback(position, State == PlaybackState.Playing);
			}
		}
	}

	private double[] CombinedEqGains()
	{
		double[] array = _timbre?.BuildEqGains() ?? new double[10];
		double[] array2 = new double[10];
		for (int i = 0; i < 10; i++)
		{
			array2[i] = Math.Clamp(((i < _eqGains.Length) ? _eqGains[i] : 0.0) + array[i], -12.0, 12.0);
		}
		return array2;
	}

	private void RefreshPlaybackPath(string reason)
	{
		if (_useVlc && _vlcFallbackActive)
		{
			return;
		}
		bool flag = _outputMode == OutputMode.SoftwareDecode && !RequiresNaudioPath;
		if (flag == _useVlc)
		{
			return;
		}
		if (flag && !_useVlc && (State == PlaybackState.Playing || State == PlaybackState.Paused))
		{
			return;
		}
		bool wasPlaying = State == PlaybackState.Playing;
		bool wasPaused = State == PlaybackState.Paused;
		bool flag2 = wasPlaying || wasPaused;
		bool restorePending = _restorePending;
		TimeSpan timeSpan = ((flag2 | restorePending) ? Position : TimeSpan.Zero);
		bool useVlc = _useVlc;
		_useVlc = flag;
		Trace("RefreshPlaybackPath: " + reason + " -> " + (_useVlc ? "vlc" : "naudio"));
		if (CurrentTrack == null)
		{
			return;
		}
		if (!flag)
		{
			if (!(flag2 | restorePending))
			{
				return;
			}
			_runToken++;
			try
			{
				_positionTimer.Change(-1, -1);
				StopBackendsForSwitch();
				TryDelete(_renderedWavPath);
				_renderedWavPath = null;
				if (OpenCurrent())
				{
					if (_useVlc)
					{
						_vlcBackend.Seek(timeSpan);
						if (restorePending || wasPaused)
						{
							_vlcBackend.Pause();
						}
						else if (wasPlaying)
						{
							_vlcBackend.Speed = _speed;
							_vlcBackend.Play();
						}
					}
					else
					{
						_naudioBackend.Seek(timeSpan);
						if (wasPlaying && !restorePending)
						{
							_naudioBackend.Play();
						}
					}
					if (restorePending)
					{
						_restorePending = true;
						SetState(PlaybackState.Stopped);
					}
					else if (wasPaused)
					{
						SetState(PlaybackState.Paused);
					}
					else if (wasPlaying)
					{
						SetState(PlaybackState.Playing);
						_positionTimer.Change(0, 50);
					}
					else
					{
						SetState(PlaybackState.Stopped);
					}
				}
				else
				{
					_restorePending = false;
					SetState(PlaybackState.Stopped);
				}
				return;
			}
			finally
			{
				_watchdog.Reset(timeSpan);
			}
		}
		else
		{
			if (!flag2 && !restorePending)
			{
				return;
			}
			_runToken++;
			try
			{
				_positionTimer.Change(-1, -1);
				StopBackendsForSwitch();
				TryDelete(_renderedWavPath);
				_renderedWavPath = null;
				_index = Math.Max(0, _index);
				if (OpenCurrent())
				{
					if (_useVlc)
					{
						bool flag3 = _vlcBackend.WaitUntilReady(1500);
						_vlcBackend.Speed = _speed;
						if (!flag3 && !restorePending)
						{
							Trace("RefreshPlaybackPath: vlc not ready, fallback to naudio");
							_vlcBackend.Stop();
							_useVlc = false;
							_vlcFallbackActive = false;
							string text = CurrentTrack?.FilePath ?? string.Empty;
							if (string.IsNullOrEmpty(text) || !OpenWithNaudio(text))
							{
								_useVlc = true;
								_vlcFallbackActive = true;
								if (OpenCurrent())
								{
									if (wasPlaying)
									{
										_vlcBackend.Play();
									}
									else if (wasPaused)
									{
										_vlcBackend.Pause();
									}
								}
							}
							else
							{
								_naudioBackend.Seek(timeSpan);
								if (wasPlaying)
								{
									_naudioBackend.Play();
								}
							}
						}
						else
						{
							_vlcBackend.Seek(timeSpan);
							if (restorePending || wasPaused)
							{
								_vlcBackend.Pause();
							}
							else if (wasPlaying)
							{
								_vlcBackend.Play();
							}
						}
					}
					else
					{
						_naudioBackend.Seek(timeSpan);
						if (wasPlaying && !restorePending)
						{
							_naudioBackend.Play();
						}
					}
					if (restorePending)
					{
						_restorePending = true;
						SetState(PlaybackState.Stopped);
					}
					else if (wasPaused)
					{
						SetState(PlaybackState.Paused);
					}
					else if (wasPlaying)
					{
						SetState(PlaybackState.Playing);
						_positionTimer.Change(0, 50);
					}
					else
					{
						SetState(PlaybackState.Stopped);
					}
				}
				else
				{
					_restorePending = false;
					SetState(PlaybackState.Stopped);
				}
			}
			finally
			{
				_watchdog.Reset(timeSpan);
			}
		}
	}

	private void StartRenderedPlayback(TimeSpan resumeAt, bool autoPlay = true)
	{
		Track track = CurrentTrack;
		if (track == null)
		{
			return;
		}
		SafeRaise(() =>
		{
			TrackChanged?.Invoke(track);
		}, "TrackChanged");
		_renderResumeAt = resumeAt;
		int gen = ++_runToken;
		CancelRender();
		CancellationTokenSource cts = new CancellationTokenSource();
		_renderCts = cts;
		_rendering = true;
		_positionTimer.Change(-1, -1);
		SetState(PlaybackState.Loading);
		SafeRaise(() =>
		{
			RenderingChanged?.Invoke(obj: true);
		}, "RenderingChanged");
		Trace("StartRenderedPlayback: " + track.FilePath);
		Task.Run(async () =>
		{
			string wav = null;
			try
			{
				wav = Path.Combine(EnsureRenderDir(), "render_" + track.Id + "_" + Guid.NewGuid().ToString("N").Substring(0, 8) + ".wav");
				RenderSettings currentRenderSettings = CurrentRenderSettings;
				Progress<RenderProgress> progress = new Progress<RenderProgress>((RenderProgress p) =>
				{
					if (!cts.IsCancellationRequested)
					{
						SafeRaise(() =>
						{
							RenderProgress?.Invoke(p);
						}, "RenderProgress");
					}
				});
				await OfflineEffectRenderer.RenderFileToWavAsync(track.FilePath, wav, currentRenderSettings, progress, cts.Token);
				if (cts.IsCancellationRequested || gen != _runToken)
				{
					TryDelete(wav);
				}
				else
				{
					TryDelete(_renderedWavPath);
					_renderedWavPath = wav;
					_useVlc = false;
					_vlcFallbackActive = false;
					bool flag = OpenRenderedWav(track, wav);
					if (!flag)
					{
						_renderedWavPath = null;
						flag = OpenWithNaudio(track.FilePath);
					}
					if (!flag)
					{
						SafeRaise(() =>
						{
							PlaybackError?.Invoke(track, PlaybackErrorKind.Decode, BuildOpenErrorMessage(track));
						}, "PlaybackError");
					}
					else
					{
						if (_renderResumeAt > TimeSpan.Zero)
						{
							_naudioBackend.Seek(_renderResumeAt);
						}
						_activeTrackId = track.Id;
						if (autoPlay)
						{
							_naudioBackend.Play();
							SetState(PlaybackState.Playing);
							_positionTimer.Change(0, 50);
							_watchdog.Reset(_renderResumeAt);
							Trace("Rendered playback started: " + wav);
						}
						else
						{
							_restorePending = true;
							SetState(PlaybackState.Stopped);
							_watchdog.Reset(_renderResumeAt);
							Trace("Rendered playback ready (paused): " + wav);
						}
					}
				}
			}
			catch (OperationCanceledException)
			{
				TryDelete(wav);
			}
			catch (Exception ex2)
			{
				Exception ex3 = ex2;
				Exception ex4 = ex3;
				Trace("Prerender failed: " + ex4.Message);
				TryDelete(wav);
				if (gen == _runToken)
				{
					_renderedWavPath = null;
					_useVlc = false;
					_vlcFallbackActive = false;
					if (OpenWithNaudio(track.FilePath))
					{
						_activeTrackId = track.Id;
						_naudioBackend.Play();
						SetState(PlaybackState.Playing);
						_positionTimer.Change(0, 50);
					}
					else
					{
						SafeRaise(() =>
						{
							PlaybackError?.Invoke(track, PlaybackErrorKind.Decode, "效果渲染失败：" + ex4.Message);
						}, "PlaybackError");
					}
				}
			}
			finally
			{
				if (_renderCts == cts)
				{
					_renderCts = null;
					_rendering = false;
					SafeRaise(() =>
					{
						RenderingChanged?.Invoke(obj: false);
					}, "RenderingChanged");
				}
			}
		});
	}

	private bool OpenRenderedWav(Track track, string wav)
	{
		_naudioBackend.SetApplyDspOnOpen(apply: false);
		return _naudioBackend.Open(wav, startPaused: false);
	}

	private void CancelRender()
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

	private static string EnsureRenderDir()
	{
		try
		{
			string text = Path.Combine(Path.GetTempPath(), "FurinaPlayer_Render");
			Directory.CreateDirectory(text);
			return text;
		}
		catch
		{
			string text2 = Path.Combine(AppContext.BaseDirectory, "render_cache");
			Directory.CreateDirectory(text2);
			return text2;
		}
	}

	private static void TryDelete(string? path)
	{
		try
		{
			if (path != null && File.Exists(path))
			{
				File.Delete(path);
			}
		}
		catch
		{
		}
	}

	private bool OpenCurrent() => OpenCurrent(CancellationToken.None);

	private bool OpenCurrent(CancellationToken cancellationToken)
	{
		if (cancellationToken.IsCancellationRequested)
		{
			return false;
		}
		_recoveryCount = 0;
		_positionTimer.Change(-1, -1);
		Track track = CurrentTrack;
		if (track == null)
		{
			return false;
		}
		StopBackendsForSwitch();
		_vlcEnd.Reset();
		_clock.Reset(TimeSpan.Zero);
		if (RequiresNaudioPath)
		{
			_useVlc = false;
			_vlcFallbackActive = false;
		}
		string filePath = _renderedWavPath ?? track.FilePath;
		if (IsNetworkUrl(filePath))
		{
			// 杜绝送入解码器：拦截非音频流的纯网页 URL (如专辑/艺人/歌单)
			if (StreamAudioCache.IsNonPlayableWebUrl(filePath))
			{
				Trace("OpenCurrent REJECTED non-playable web URL: " + filePath);
				SafeRaise(() =>
				{
					PlaybackError?.Invoke(track, PlaybackErrorKind.Decode, "无法作为单曲播放网页地址: " + filePath);
				}, "PlaybackError");
				return false;
			}

			try
			{
				if (StreamAudioCache.TryGetCachedFilePathIfReady(filePath, out string readyFile) && File.Exists(readyFile))
				{
					track.FilePath = readyFile;
					filePath = readyFile;
				}
				else if (StreamAudioCache.TryGetOrDownloadCachedFile(filePath, out string cachedFile, 6000, cancellationToken) && File.Exists(cachedFile))
				{
					track.FilePath = cachedFile;
					filePath = cachedFile;
				}
			}
			catch (Exception ex)
			{
				Trace("OpenCurrent cache retrieval failed: " + ex.Message);
			}

			if (cancellationToken.IsCancellationRequested)
			{
				return false;
			}
		}

		bool flag = false;
		try
		{
			flag = OpenWithNaudio(filePath, _renderedWavPath == null);
		}
		catch (Exception ex)
		{
			Trace("OpenWithNaudio threw exception: " + ex.Message);
			flag = false;
		}

		if (cancellationToken.IsCancellationRequested)
		{
			StopBackendsForSwitch();
			return false;
		}

		if (!flag && IsVlcAvailable)
		{
			// 严防死锁：若为网页链接或已确认不可播放的 URL，绝不回退至 VLC 尝试解码网页
			if (IsNetworkUrl(filePath) && StreamAudioCache.IsNonPlayableWebUrl(filePath))
			{
				Trace("Skipping VLC fallback for non-playable web URL: " + filePath);
			}
			else
			{
				try
				{
					Trace("NAudio failed to open file in OpenCurrent, falling back to VLC: " + filePath);
					flag = _vlcBackend.Open(filePath, startPaused: false);
					if (flag)
					{
						_useVlc = true;
						_vlcFallbackActive = true;
						_vlcBackend.Speed = _speed;
						_vlcBackend.Volume = _volume > 0 ? _volume : 80;
					}
					else
					{
						_vlcBackend.Stop();
					}
				}
				catch (Exception ex)
				{
					Trace("VLC Open threw exception: " + ex.Message);
					flag = false;
				}
			}
		}
		if (cancellationToken.IsCancellationRequested)
		{
			StopBackendsForSwitch();
			return false;
		}
		if (flag && track.DurationMilliseconds <= 0 && CurrentBackend.Duration.TotalMilliseconds > 0)
		{
			track.DurationMilliseconds = (long)CurrentBackend.Duration.TotalMilliseconds;
		}
		SafeRaise(() =>
		{
			TrackChanged?.Invoke(track);
		}, "TrackChanged");
		if (!flag)
		{
			var (kind, message) = BuildOpenError(track);
			Trace("OpenCurrent FAILED: " + track.FilePath);
			SafeRaise(() =>
			{
				PlaybackError?.Invoke(track, kind, message);
			}, "PlaybackError");
		}
		else
		{
			Trace("OpenCurrent OK (" + (_useVlc ? "vlc" : "naudio") + "): " + track.FilePath);
		}
		return flag;
	}

	private bool OpenWithNaudio(string filePath, bool applyDsp = true)
	{
		try
		{
			if (IsNetworkUrl(filePath))
			{
				if (StreamAudioCache.IsNonPlayableWebUrl(filePath))
				{
					Trace("OpenWithNaudio: rejecting non-playable web URL: " + filePath);
					return false;
				}
				if (StreamAudioCache.TryGetCachedFilePathIfReady(filePath, out string cachedFile) && File.Exists(cachedFile))
				{
					filePath = cachedFile;
				}
				else
				{
					// 未缓存到本地的网络直链不再通过 NAudio MediaFoundation 同步阻塞打开，避免 Win32 假死
					Trace("OpenWithNaudio: stream not cached locally, deferring to VLC streaming: " + filePath);
					return false;
				}
			}
			_naudioBackend.SetApplyDspOnOpen(applyDsp);
			_naudioBackend.SetDspState(_eqGains, _reverbEnabled, _reverbMix, _reverbPreset, _spatialWidth, _spatialModulation, _spatialPredelayMs, _timbre, _vst3);
			bool flag = _naudioBackend.Open(filePath, startPaused: false);
			if (flag)
			{
				_useVlc = false;
				_vlcFallbackActive = false;
				ApplyVolume();
			}
			return flag;
		}
		catch (Exception ex)
		{
			Trace("OpenWithNaudio exception: " + ex.Message);
			return false;
		}
	}

	private static bool IsNetworkUrl(string? path)
	{
		if (string.IsNullOrWhiteSpace(path)) return false;
		return Uri.TryCreate(path, UriKind.Absolute, out Uri? uri) 
			&& (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps || uri.Scheme == "rtsp" || uri.Scheme == "rtmp");
	}
	private (PlaybackErrorKind Kind, string Message) BuildOpenError(Track track)
	{
		if (!File.Exists(track.FilePath) && !IsNetworkUrl(track.FilePath))
		{
			return (Kind: PlaybackErrorKind.FileNotFound, Message: "文件不存在或已被移动：" + track.FilePath);
		}
		if (!HasAudioOutputDevice())
		{
			return (Kind: PlaybackErrorKind.Output, Message: "未检测到可用的音频输出设备：播放无法出声。请检查系统声音设置、连接音箱/耳机后重试。");
		}
		return (Kind: PlaybackErrorKind.Decode, Message: "无法播放该音频文件：" + track.FilePath + "。解码器不支持此格式或文件已损坏，请更换文件。");
	}

	private static string BuildOpenErrorMessage(Track track)
	{
		if (!HasAudioOutputDevice())
		{
			return "未检测到可用的音频输出设备：播放无法出声。请检查系统声音设置、连接音箱/耳机后重试。";
		}
		return "无法播放该音频文件：" + track.FilePath + "。解码器不支持此格式或文件已损坏，请更换文件。";
	}

	private static bool HasAudioOutputDevice()
	{
		try
		{
			using DeviceManager deviceManager = new DeviceManager();
			return deviceManager.EnumerateWasapiDevices().Count > 0 || deviceManager.EnumerateAsioDevices().Count > 0;
		}
		catch
		{
			return true;
		}
	}

	private void OnBackendEnded()
	{
		if (Volatile.Read(ref _isSwitchingBackend) > 0)
		{
			return;
		}
		long endedSeq = Interlocked.Read(ref _sessionSwitchSeq);
		Task.Run(() => OnTrackEnded(endedSeq));
	}

	private void OnVlcFailed()
	{
		Trace("VLC error for: " + (CurrentTrack?.FilePath ?? "?"));
		Track track = CurrentTrack;
		if (track != null)
		{
			SafeRaise(() =>
			{
				PlaybackError?.Invoke(track, PlaybackErrorKind.Output, "播放器输出声音失败：请检查音频输出设备，或到设置中切换输出模式后重试。");
			}, "PlaybackError");
		}
		Stop();
	}

	private void OnNaudioFailed()
	{
		long failedSeq = Interlocked.Read(ref _sessionSwitchSeq);
		Track track = CurrentTrack;
		if (track != null)
		{
			SafeRaise(() =>
			{
				PlaybackError?.Invoke(track, PlaybackErrorKind.System, "播放中途出错：音频输出可能被系统中断，请检查设备后重试。");
			}, "PlaybackError");
		}
		Task.Run(() => OnTrackEnded(failedSeq));
	}

	private readonly object _trackEndedLock = new object();

	private void OnTrackEnded(long? expectedSwitchSeq = null)
	{
		lock (_trackEndedLock)
		{
			if (Volatile.Read(ref _isSwitchingBackend) > 0)
			{
				Trace("OnTrackEnded ignored during backend switch");
				return;
			}
			if (expectedSwitchSeq.HasValue && expectedSwitchSeq.Value != Interlocked.Read(ref _sessionSwitchSeq))
			{
				Trace("OnTrackEnded stale generation ignored");
				return;
			}
			if (State == PlaybackState.Stopped || State == PlaybackState.Loading || State == PlaybackState.Error)
			{
				return;
			}
			Track ended = CurrentTrack;
			if (ended == null)
			{
				return;
			}
			DateTime utcNow = DateTime.UtcNow;
			if (_lastEndedTrackId == ended.Id && (utcNow - _lastEndedUtc).TotalMilliseconds < 1500.0)
			{
				Trace("OnTrackEnded duplicate ignored: " + ended.Id);
				return;
			}
			_lastEndedUtc = utcNow;
			_lastEndedTrackId = ended.Id;
			SafeRaise(() =>
			{
				TrackEnded?.Invoke(ended);
			}, "TrackEnded");
			if (Mode == PlaybackMode.RepeatOne)
			{
				_activeTrackId = -1L;
				PlayTrack(ended);
			}
			else if (Mode == PlaybackMode.Sequential)
			{
				if (_index + 1 < _queue.Count)
				{
					_index++;
					_activeTrackId = -1L;
					PlayTrack(_queue[_index]);
				}
				else
				{
					Stop();
				}
			}
			else if (Mode == PlaybackMode.Shuffle)
			{
				_index = (_queue.Count > 1) ? GetRandomIndexExcept(_index) : 0;
				_activeTrackId = -1L;
				PlayTrack(_queue[_index]);
			}
			else // RepeatAll
			{
				if (_queue.Count > 0)
				{
					_index = (_index + 1) % _queue.Count;
					_activeTrackId = -1L;
					PlayTrack(_queue[_index]);
				}
				else
				{
					Stop();
				}
			}
		}
	}

	private void SetState(PlaybackState state)
	{
		_state = state;
		try
		{
			SonicWave.Audio.Vst3.Vst3NativeHost.Instance.SetTransportPlaying(state == PlaybackState.Playing);
		}
		catch
		{
		}
		SafeRaise(() =>
		{
			StateChanged?.Invoke(state);
		}, "StateChanged");
		SafeRaise(() =>
		{
			StatusChanged?.Invoke(BuildStatus());
		}, "StatusChanged");
	}

	private PlaybackStatus BuildStatus()
	{
		return new PlaybackStatus(_state, CurrentTrack, Position, Duration, _useVlc ? "vlc" : "naudio", CurrentRenderSettings.IsActive, PlaybackErrorKind.None, null, Array.Empty<string>(), _recoveryCount);
	}

	private void RaisePositionChanged()
	{
		if (Interlocked.CompareExchange(ref _positionTickActive, 1, 0) != 0)
		{
			return;
		}
		try
		{
			long tickSeq = Interlocked.Read(ref _sessionSwitchSeq);
			if (Volatile.Read(ref _isSwitchingBackend) > 0)
			{
				return;
			}
			if (State == PlaybackState.Playing || State == PlaybackState.Paused)
			{
				SafeRaise(() =>
				{
					PositionChanged?.Invoke(Position);
				}, "PositionChanged");
			}
			if (State != PlaybackState.Playing || _rendering)
			{
				_vlcEnd.Reset();
				return;
			}
			if (tickSeq != Interlocked.Read(ref _sessionSwitchSeq) || Volatile.Read(ref _isSwitchingBackend) > 0)
			{
				return;
			}
			if (_useVlc)
			{
				try
				{
					bool isActuallyPlaying = _vlcBackend.IsActuallyPlaying;
					TimeSpan position = _vlcBackend.Position;
					TimeSpan duration = _vlcBackend.Duration;
					Interlocked.Exchange(ref _cachedVlcPositionTicks, position.Ticks);
					if (duration > TimeSpan.Zero)
					{
						Interlocked.Exchange(ref _cachedVlcDurationTicks, duration.Ticks);
					}
					if (_vlcEnd.Report(isActuallyPlaying, position, duration))
					{
						Trace("VLC natural end detected by position poll");
						Task.Run(() => OnTrackEnded(tickSeq));
						return;
					}
				}
				catch
				{
				}
			}
			else
			{
				_vlcEnd.Reset();
			}
			try
			{
				bool isActuallyPlaying2 = CurrentBackend.IsActuallyPlaying;
				TimeSpan position2 = CurrentBackend.Position;
				TimeSpan duration2 = CurrentBackend.Duration;
				if (isActuallyPlaying2 && duration2 > TimeSpan.FromSeconds(6.0) && position2 >= TimeSpan.FromSeconds(3.0) && position2 < duration2 - TimeSpan.FromSeconds(3.0))
				{
					if (_watchdog.CheckStalled(position2) && tickSeq == Interlocked.Read(ref _sessionSwitchSeq))
					{
						Trace("watchdog stall detected @" + position2);
						RecoverFromStall(position2);
					}
					else if (_recoveryCount > 0)
					{
						_recoveryCount = 0;
					}
				}
				else
				{
					_watchdog.Reset(position2);
				}
			}
			catch
			{
			}
		}
		finally
		{
			Volatile.Write(ref _positionTickActive, 0);
		}
	}

	private void RecoverFromStall(TimeSpan pos)
	{
		if (_recoveryCount >= 3)
		{
			FailCurrent(PlaybackErrorKind.System, "播放停滞且自动恢复失败，请重新播放或检查音频输出设备。");
			return;
		}
		_recoveryCount++;
		Track currentTrack = CurrentTrack;
		if (currentTrack == null)
		{
			return;
		}
		Trace("watchdog recovery #" + _recoveryCount + " @" + pos.ToString() + " " + currentTrack.FilePath);
		long recoverySeq = Interlocked.Read(ref _sessionSwitchSeq);
		_runToken++;
		_positionTimer.Change(-1, -1);
		if (IsNetworkUrl(currentTrack.FilePath))
		{
			if (StreamAudioCache.IsNonPlayableWebUrl(currentTrack.FilePath))
			{
				Trace("watchdog recovery rejected non-playable web URL: " + currentTrack.FilePath);
				return;
			}
			try
			{
				if (StreamAudioCache.TryGetCachedFilePathIfReady(currentTrack.FilePath, out string readyFile) && File.Exists(readyFile))
				{
					currentTrack.FilePath = readyFile;
				}
				else if (StreamAudioCache.TryGetOrDownloadCachedFile(currentTrack.FilePath, out string cachedFile, 5000) && File.Exists(cachedFile))
				{
					currentTrack.FilePath = cachedFile;
				}
			}
			catch (Exception ex)
			{
				Trace("watchdog recovery cache download failed: " + ex.Message);
			}
		}
		if (recoverySeq != Interlocked.Read(ref _sessionSwitchSeq) || CurrentTrack?.Id != currentTrack.Id)
		{
			Trace("watchdog recovery aborted due to track switch");
			return;
		}
		StopBackendsForSwitch();
		if (RequiresNaudioPath)
		{
			_useVlc = false;
			_vlcFallbackActive = false;
		}
		if (OpenWithNaudio(currentTrack.FilePath))
		{
			_naudioBackend.Seek(pos);
			_naudioBackend.Play();
			_activeTrackId = currentTrack.Id;
			SetState(PlaybackState.Playing);
			_positionTimer.Change(0, 50);
			_watchdog.Reset(pos);
			Trace("watchdog recovered via naudio @" + pos);
			return;
		}
		if (IsVlcAvailable && !StreamAudioCache.IsNonPlayableWebUrl(currentTrack.FilePath))
		{
			try
			{
				if (_vlcBackend.Open(currentTrack.FilePath, startPaused: false))
				{
					_useVlc = true;
					_vlcFallbackActive = true;
					_vlcBackend.Seek(pos);
					_vlcBackend.Play();
					_activeTrackId = currentTrack.Id;
					SetState(PlaybackState.Playing);
					_positionTimer.Change(0, 50);
					_watchdog.Reset(pos);
					Trace("watchdog recovered via vlc @" + pos);
					return;
				}
			}
			catch (Exception ex)
			{
				Trace("watchdog recovery vlc open failed: " + ex.Message);
			}
		}
		FailCurrent(PlaybackErrorKind.System, "播放停滞且自动恢复失败：无法重新打开音频文件。");
	}

	private void FailCurrent(PlaybackErrorKind kind, string message)
	{
		_runToken++;
		Track track = CurrentTrack;
		_restorePending = false;
		_activeTrackId = -1L;
		CancelRender();
		_positionTimer.Change(-1, -1);
		try
		{
			_renderRestartTimer.Change(-1, -1);
		}
		catch
		{
		}
		StopBackendsForSwitch();
		_useVlc = _outputMode == OutputMode.SoftwareDecode && !RequiresNaudioPath;
		_vlcFallbackActive = false;
		TryDelete(_renderedWavPath);
		_renderedWavPath = null;
		SetState(PlaybackState.Error);
		if (track != null)
		{
			SafeRaise(() =>
			{
				PlaybackError?.Invoke(track, kind, message);
			}, "PlaybackError");
		}
		Trace("playback error: " + kind.ToString() + " " + message);
	}

	public void Dispose()
	{
		_runToken++;
		CancelRender();
		TryDelete(_renderedWavPath);
		_renderedWavPath = null;
		_positionTimer.Dispose();
		_eqApplyTimer.Dispose();
		_renderRestartTimer.Dispose();
		_naudioBackend.Dispose();
		_vlcBackend.Dispose();
	}
}
