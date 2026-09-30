using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SonicWave.Audio.Backends;
using SonicWave.Audio.Core;
using SonicWave.Audio.Dsp;
using SonicWave.Audio.Output;
using SonicWave.Audio.Vst3;
using SonicWave.Core.Models;

namespace SonicWave.Audio;

internal sealed class AudioSession : IDisposable
{
	private readonly Timer _positionTimer;

	private readonly Timer _eqApplyTimer;

	private readonly Timer _renderRestartTimer;

	private readonly Random _random = new Random();

	private readonly IAudioBackend _vlcBackend;

	private readonly IAudioBackend _naudioBackend;

	private OutputMode _outputMode;

	private bool _useVlc;

	private PlaybackState _state;

	private int _runToken;

	private bool _restorePending;

	private long _activeTrackId = -1L;

	private TimeSpan _restorePosition;

	private TimeSpan _abStart = TimeSpan.MinValue;

	private TimeSpan _abEnd = TimeSpan.MinValue;

	private int _volume = 5;

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

	private readonly PlaybackWatchdog _watchdog = new PlaybackWatchdog(TimeSpan.FromMilliseconds(250.0), 6);

	private int _recoveryCount;

	private DateTime _lastEndedUtc = DateTime.MinValue;

	private long _lastEndedTrackId = -1L;

	private bool _inPositionGetter;

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
				return TimeSpan.Zero;
			}
			_inPositionGetter = true;
			try
			{
				TimeSpan timeSpan = _clock.Guard(CurrentBackend.Position, seekPending: false);
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
			return CurrentBackend.Duration;
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
			if (!_reverbEnabled)
			{
				TimbreSettings timbre = _timbre;
				if (timbre == null || !timbre.IsActive)
				{
					return Vst3Active;
				}
			}
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
				return _vst3.Any((Vst3PluginState p) => p != null && p.IsActive && string.Equals(p.Path, Vst3NativeHost.Instance.LoadedPath, StringComparison.OrdinalIgnoreCase));
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
		_naudioBackend.Configure(deviceId, asioDriver, bufferMs);
		_useVlc = mode == OutputMode.SoftwareDecode && !RequiresNaudioPath;
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

	public void PlayTrack(Track track)
	{
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
		_activeTrackId = -1L;
		Play();
	}

	public bool PrepareRestore(TimeSpan position)
	{
		Track track = CurrentTrack;
		if (track == null || !File.Exists(track.FilePath))
		{
			return false;
		}
		_restorePending = false;
		_positionTimer.Change(-1, -1);
		_restorePosition = position;
		bool flag;
		if (_useVlc)
		{
			flag = _vlcBackend.Open(track.FilePath, startPaused: false);
			if (flag)
			{
				flag = _vlcBackend.WaitUntilReady(2000);
				if (flag)
				{
					_vlcBackend.Speed = _speed;
					_vlcBackend.Seek(position);
					_vlcBackend.Pause();
				}
				else
				{
					_vlcBackend.Stop();
				}
			}
			if (!flag)
			{
				_useVlc = false;
				_vlcBackend.Stop();
				flag = OpenWithNaudio(track.FilePath);
			}
		}
		else
		{
			flag = OpenWithNaudio(track.FilePath);
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

	public void Play()
	{
		if (CurrentTrack == null)
		{
			Trace("Play: no current track");
			return;
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
				_watchdog.Reset(Position);
				Trace("Play: resumed from pause");
				return;
			}
			Trace("Play: paused but different track requested, switching track");
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
			_positionTimer.Change(0, 500);
			_watchdog.Reset(Position);
			Trace("Play: resume restored session");
		}
		else if (RequiresPrerender)
		{
			StartRenderedPlayback(TimeSpan.Zero);
		}
		else if (!OpenCurrent())
		{
			_activeTrackId = -1L;
			_positionTimer.Change(-1, -1);
			_restorePending = false;
			SetState(PlaybackState.Stopped);
			Trace("Play: open failed, stopped");
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
			_positionTimer.Change(0, 500);
			_watchdog.Reset(Position);
			Trace("Play: playing (" + (_useVlc ? "vlc" : "naudio") + ")");
		}
	}

	public void Pause()
	{
		_vlcEnd.Reset();
		_watchdog.Reset(Position);
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
		else if (CurrentTrack != null)
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

	public void Stop()
	{
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
		if (_useVlc)
		{
			_vlcBackend.Stop();
		}
		else
		{
			_naudioBackend.Stop();
		}
		TryDelete(_renderedWavPath);
		_renderedWavPath = null;
		SetState(PlaybackState.Stopped);
	}

	public bool Next()
	{
		if (_queue.Count == 0)
		{
			return false;
		}
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
		PlayTrack(_queue[_index]);
		return true;
	}

	public bool Previous()
	{
		if (_queue.Count == 0)
		{
			return false;
		}
		if (Position > TimeSpan.FromSeconds(3.0))
		{
			Seek(TimeSpan.Zero);
			return true;
		}
		_index = Mode switch
		{
			PlaybackMode.Shuffle => (_queue.Count > 1) ? GetRandomIndexExcept(_index) : 0, 
			PlaybackMode.Sequential => (_index - 1 >= 0) ? (_index - 1) : 0, 
			_ => (_index - 1 + _queue.Count) % _queue.Count, 
		};
		_activeTrackId = -1L;
		PlayTrack(_queue[_index]);
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
			CurrentBackend.Volume = _volume;
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

	public void SetVst3(IReadOnlyList<Vst3PluginState> plugins)
	{
		bool vst3Active = Vst3Active;
		bool flag = _renderedWavPath != null;
		_vst3 = plugins ?? Array.Empty<Vst3PluginState>();
		_naudioBackend.SetVst3(_vst3);

		Vst3PluginState? activePlugin = _vst3.FirstOrDefault(p => p != null && p.IsActive);
		if (activePlugin != null && !string.IsNullOrWhiteSpace(activePlugin.Path))
		{
			try
			{
				int sampleRate = _naudioBackend.SampleRate > 0 ? _naudioBackend.SampleRate : 44100;
				Vst3NativeHost.Instance.EnsureLoaded(activePlugin.Path, sampleRate, 2, out _);
				Vst3NativeHost.Instance.UpdateSampleRate(sampleRate);
			}
			catch
			{
			}
		}
		else if (vst3Active && !Vst3Active)
		{
			try
			{
				Vst3NativeHost.Instance.Unload();
			}
			catch
			{
			}
		}

		bool vst3Active2 = Vst3Active;
		if (flag)
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
		bool flag = State == PlaybackState.Playing;
		bool restorePending = _restorePending;
		TimeSpan pos = Position;
		Trace("restart clean naudio chain: " + reason + " @" + pos.ToString() + " " + currentTrack.FilePath);
		_runToken++;
		try
		{
			_positionTimer.Change(-1, -1);
			_naudioBackend.Stop();
			TryDelete(_renderedWavPath);
			_renderedWavPath = null;
			if (OpenWithNaudio(currentTrack.FilePath))
			{
				_naudioBackend.Seek(pos);
				if (flag)
				{
					_naudioBackend.Play();
					SetState(PlaybackState.Playing);
					_positionTimer.Change(0, 500);
				}
				else if (restorePending)
				{
					_restorePending = true;
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
		bool flag = _outputMode == OutputMode.SoftwareDecode && !RequiresNaudioPath;
		if (flag == _useVlc)
		{
			return;
		}
		if (flag && !_useVlc && (State == PlaybackState.Playing || State == PlaybackState.Paused))
		{
			return;
		}
		bool flag2 = State == PlaybackState.Playing || State == PlaybackState.Paused;
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
				if (useVlc)
				{
					_vlcBackend.Stop();
				}
				else
				{
					_naudioBackend.Stop();
				}
				TryDelete(_renderedWavPath);
				_renderedWavPath = null;
				if (OpenCurrent())
				{
					_naudioBackend.Seek(timeSpan);
					if (!restorePending)
					{
						_naudioBackend.Play();
					}
					SetState(PlaybackState.Playing);
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
				if (useVlc)
				{
					_vlcBackend.Stop();
				}
				else
				{
					_naudioBackend.Stop();
				}
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
							string text = CurrentTrack?.FilePath ?? string.Empty;
							if (string.IsNullOrEmpty(text) || !OpenWithNaudio(text))
							{
								_useVlc = true;
								if (OpenCurrent())
								{
									_vlcBackend.Play();
								}
							}
							else
							{
								_naudioBackend.Seek(timeSpan);
								_naudioBackend.Play();
							}
						}
						else
						{
							_vlcBackend.Seek(timeSpan);
							if (restorePending)
							{
								_vlcBackend.Pause();
							}
							else
							{
								_vlcBackend.Play();
							}
						}
					}
					else
					{
						_naudioBackend.Seek(timeSpan);
						if (!restorePending)
						{
							_naudioBackend.Play();
						}
					}
					if (restorePending)
					{
						_restorePending = true;
						SetState(PlaybackState.Stopped);
					}
					else
					{
						SetState(PlaybackState.Playing);
						_positionTimer.Change(0, 500);
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
							_positionTimer.Change(0, 500);
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
					if (OpenWithNaudio(track.FilePath))
					{
						_activeTrackId = track.Id;
						_naudioBackend.Play();
						SetState(PlaybackState.Playing);
						_positionTimer.Change(0, 500);
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

	private bool OpenCurrent()
	{
		Track track = CurrentTrack;
		if (track == null)
		{
			return false;
		}
		_vlcEnd.Reset();
		_clock.Reset(TimeSpan.Zero);
		string filePath = _renderedWavPath ?? track.FilePath;
		bool flag;
		if (_useVlc)
		{
			flag = _vlcBackend.Open(filePath, startPaused: false);
			if (flag)
			{
				_vlcBackend.Speed = _speed;
				_vlcBackend.Volume = _volume;
			}
			if (!flag)
			{
				_useVlc = false;
				_vlcBackend.Stop();
				flag = OpenWithNaudio(filePath, _renderedWavPath == null);
			}
		}
		else
		{
			flag = OpenWithNaudio(filePath, _renderedWavPath == null);
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
		_naudioBackend.SetApplyDspOnOpen(applyDsp);
		_naudioBackend.SetDspState(_eqGains, _reverbEnabled, _reverbMix, _reverbPreset, _spatialWidth, _spatialModulation, _spatialPredelayMs, _timbre, _vst3);
		bool flag = _naudioBackend.Open(filePath, startPaused: false);
		if (flag)
		{
			ApplyVolume();
		}
		return flag;
	}

	private (PlaybackErrorKind Kind, string Message) BuildOpenError(Track track)
	{
		if (!File.Exists(track.FilePath))
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
		Task.Run(() => OnTrackEnded());
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
		Track track = CurrentTrack;
		if (track != null)
		{
			SafeRaise(() =>
			{
				PlaybackError?.Invoke(track, PlaybackErrorKind.System, "播放中途出错：音频输出可能被系统中断，请检查设备后重试。");
			}, "PlaybackError");
		}
		Task.Run(() => OnTrackEnded());
	}

	private readonly object _trackEndedLock = new object();

	private void OnTrackEnded()
	{
		lock (_trackEndedLock)
		{
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
		if (_useVlc)
		{
			try
			{
				bool isActuallyPlaying = _vlcBackend.IsActuallyPlaying;
				TimeSpan position = _vlcBackend.Position;
				TimeSpan duration = _vlcBackend.Duration;
				if (_vlcEnd.Report(isActuallyPlaying, position, duration))
				{
					Trace("VLC natural end detected by position poll");
					OnTrackEnded();
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
			if (isActuallyPlaying2 && duration2 > TimeSpan.FromSeconds(3.0) && position2 < duration2 - TimeSpan.FromSeconds(3.0))
			{
				if (_watchdog.CheckStalled(position2))
				{
					Trace("watchdog stall detected @" + position2);
					RecoverFromStall(position2);
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

	private void RecoverFromStall(TimeSpan pos)
	{
		if (_recoveryCount >= 2)
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
		_runToken++;
		_positionTimer.Change(-1, -1);
		if (_useVlc)
		{
			_useVlc = false;
			_vlcBackend.Stop();
			if (OpenWithNaudio(currentTrack.FilePath))
			{
				_naudioBackend.Seek(pos);
				_naudioBackend.Play();
				_activeTrackId = currentTrack.Id;
				SetState(PlaybackState.Playing);
				_positionTimer.Change(0, 500);
				_watchdog.Reset(pos);
				Trace("watchdog recovered via naudio @" + pos);
				return;
			}
		}
		else
		{
			_naudioBackend.Stop();
			if (OpenWithNaudio(currentTrack.FilePath))
			{
				_naudioBackend.Seek(pos);
				_naudioBackend.Play();
				_activeTrackId = currentTrack.Id;
				SetState(PlaybackState.Playing);
				_positionTimer.Change(0, 500);
				_watchdog.Reset(pos);
				Trace("watchdog recovered via reopen @" + pos);
				return;
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
		if (_useVlc)
		{
			_vlcBackend.Stop();
		}
		else
		{
			_naudioBackend.Stop();
		}
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
