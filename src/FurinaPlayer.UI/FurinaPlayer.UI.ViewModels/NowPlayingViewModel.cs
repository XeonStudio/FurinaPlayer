using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel.__Internals;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using SonicWave.Audio;
using SonicWave.Audio.Dsp;
using SonicWave.Audio.Output;
using SonicWave.Core.Models;
using SonicWave.Core.Services;
using WinRT;

namespace FurinaPlayer.UI.ViewModels;

[WinRTRuntimeClassName("Microsoft.UI.Xaml.Data.INotifyPropertyChanged")]
[WinRTExposedType(typeof(NowPlayingViewModelWinRTTypeDetails))]
public class NowPlayingViewModel : ObservableObject
{
	private readonly AudioEngine _engine;

	private readonly LyricService _lyricService;

	private readonly LibraryService _library;

	private readonly SettingsService? _settingsService;

	private readonly DispatcherQueue? _uiDispatcher;

	private bool _positionSliderDragging;

	private long _lastRecordedTrackId = -1L;

	[ObservableProperty]
	private Track? currentTrack;

	[ObservableProperty]
	private string coverPath = string.Empty;

	[ObservableProperty]
	private string title = string.Empty;

	[ObservableProperty]
	private string artist = string.Empty;

	[ObservableProperty]
	private string album = string.Empty;

	[ObservableProperty]
	private TimeSpan position;

	[ObservableProperty]
	private TimeSpan duration;

	[ObservableProperty]
	private double positionPercent;

	[ObservableProperty]
	private int volume = 80;

	[ObservableProperty]
	[NotifyPropertyChangedFor("PlayButtonVisibility")]
	[NotifyPropertyChangedFor("PauseButtonVisibility")]
	private bool isPlaying;

	[ObservableProperty]
	private string modeText = "顺序播放";

	[ObservableProperty]
	private double speed = 1.0;

	[ObservableProperty]
	private ObservableCollection<LyricLine> lyricLines = new ObservableCollection<LyricLine>();

	[ObservableProperty]
	private int currentLyricIndex = -1;

	[ObservableProperty]
	private string lyricSource = string.Empty;

	[ObservableProperty]
	private ObservableCollection<Track> recentTracks = new ObservableCollection<Track>();

	[ObservableProperty]
	private double[] eqGains = new double[10];

	[ObservableProperty]
	[NotifyPropertyChangedFor("EqualizerVisibility")]
	private bool equalizerVisible;

	[ObservableProperty]
	private bool isVlcAvailable;

	[ObservableProperty]
	[NotifyPropertyChangedFor("StatusVisibility")]
	[NotifyPropertyChangedFor("HasStatusMessage")]
	private string statusMessage = string.Empty;

	private DispatcherQueueTimer? _statusTimer;

	private static Task<bool>? _deviceCheckTask;

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.2.0.0")]
	private RelayCommand? playCommand;

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.2.0.0")]
	private RelayCommand? pauseCommand;

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.2.0.0")]
	private RelayCommand? togglePlayPauseCommand;

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.2.0.0")]
	private RelayCommand? nextCommand;

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.2.0.0")]
	private RelayCommand? previousCommand;

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.2.0.0")]
	private RelayCommand? stopCommand;

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.2.0.0")]
	private RelayCommand? cycleModeCommand;

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.2.0.0")]
	private RelayCommand? toggleEqualizerCommand;

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.2.0.0")]
	private RelayCommand? applyEqualizerCommand;

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.2.0.0")]
	private RelayCommand<string>? applyEqPresetCommand;

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.2.0.0")]
	private RelayCommand<Track?>? playRecentCommand;

	public Visibility PlayButtonVisibility
	{
		get
		{
			if (!IsPlaying)
			{
				return Visibility.Visible;
			}
			return Visibility.Collapsed;
		}
	}

	public Visibility PauseButtonVisibility
	{
		get
		{
			if (!IsPlaying)
			{
				return Visibility.Collapsed;
			}
			return Visibility.Visible;
		}
	}

	public Visibility EqualizerVisibility
	{
		get
		{
			if (!EqualizerVisible)
			{
				return Visibility.Collapsed;
			}
			return Visibility.Visible;
		}
	}

	public Visibility StatusVisibility
	{
		get
		{
			if (!string.IsNullOrEmpty(StatusMessage))
			{
				return Visibility.Visible;
			}
			return Visibility.Collapsed;
		}
	}

	public bool HasStatusMessage => !string.IsNullOrEmpty(StatusMessage);

	public string LyricFontFamily => _settingsService?.Load().LyricFontFamily ?? string.Empty;

	public string LyricFontFilePath => _settingsService?.Load().LyricFontFilePath ?? string.Empty;

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.2.0.0")]
	[ExcludeFromCodeCoverage]
	public Track? CurrentTrack
	{
		get
		{
			return currentTrack;
		}
		set
		{
			if (!EqualityComparer<Track>.Default.Equals(currentTrack, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.CurrentTrack);
				currentTrack = value;
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.CurrentTrack);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.2.0.0")]
	[ExcludeFromCodeCoverage]
	public string CoverPath
	{
		get
		{
			return coverPath;
		}
		[MemberNotNull("coverPath")]
		set
		{
			if (!EqualityComparer<string>.Default.Equals(coverPath, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.CoverPath);
				coverPath = value;
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.CoverPath);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.2.0.0")]
	[ExcludeFromCodeCoverage]
	public string Title
	{
		get
		{
			return title;
		}
		[MemberNotNull("title")]
		set
		{
			if (!EqualityComparer<string>.Default.Equals(title, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.Title);
				title = value;
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.Title);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.2.0.0")]
	[ExcludeFromCodeCoverage]
	public string Artist
	{
		get
		{
			return artist;
		}
		[MemberNotNull("artist")]
		set
		{
			if (!EqualityComparer<string>.Default.Equals(artist, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.Artist);
				artist = value;
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.Artist);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.2.0.0")]
	[ExcludeFromCodeCoverage]
	public string Album
	{
		get
		{
			return album;
		}
		[MemberNotNull("album")]
		set
		{
			if (!EqualityComparer<string>.Default.Equals(album, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.Album);
				album = value;
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.Album);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.2.0.0")]
	[ExcludeFromCodeCoverage]
	public TimeSpan Position
	{
		get
		{
			return position;
		}
		set
		{
			if (!EqualityComparer<TimeSpan>.Default.Equals(position, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.Position);
				position = value;
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.Position);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.2.0.0")]
	[ExcludeFromCodeCoverage]
	public TimeSpan Duration
	{
		get
		{
			return duration;
		}
		set
		{
			if (!EqualityComparer<TimeSpan>.Default.Equals(duration, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.Duration);
				duration = value;
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.Duration);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.2.0.0")]
	[ExcludeFromCodeCoverage]
	public double PositionPercent
	{
		get
		{
			return positionPercent;
		}
		set
		{
			if (!EqualityComparer<double>.Default.Equals(positionPercent, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.PositionPercent);
				positionPercent = value;
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.PositionPercent);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.2.0.0")]
	[ExcludeFromCodeCoverage]
	public int Volume
	{
		get
		{
			return volume;
		}
		set
		{
			if (!EqualityComparer<int>.Default.Equals(volume, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.Volume);
				volume = value;
				OnVolumeChanged(value);
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.Volume);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.2.0.0")]
	[ExcludeFromCodeCoverage]
	public bool IsPlaying
	{
		get
		{
			return isPlaying;
		}
		set
		{
			if (!EqualityComparer<bool>.Default.Equals(isPlaying, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.IsPlaying);
				isPlaying = value;
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.IsPlaying);
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.PlayButtonVisibility);
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.PauseButtonVisibility);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.2.0.0")]
	[ExcludeFromCodeCoverage]
	public string ModeText
	{
		get
		{
			return modeText;
		}
		[MemberNotNull("modeText")]
		set
		{
			if (!EqualityComparer<string>.Default.Equals(modeText, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.ModeText);
				modeText = value;
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.ModeText);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.2.0.0")]
	[ExcludeFromCodeCoverage]
	public double Speed
	{
		get
		{
			return speed;
		}
		set
		{
			if (!EqualityComparer<double>.Default.Equals(speed, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.Speed);
				speed = value;
				OnSpeedChanged(value);
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.Speed);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.2.0.0")]
	[ExcludeFromCodeCoverage]
	public ObservableCollection<LyricLine> LyricLines
	{
		get
		{
			return lyricLines;
		}
		[MemberNotNull("lyricLines")]
		set
		{
			if (!EqualityComparer<ObservableCollection<LyricLine>>.Default.Equals(lyricLines, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.LyricLines);
				lyricLines = value;
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.LyricLines);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.2.0.0")]
	[ExcludeFromCodeCoverage]
	public int CurrentLyricIndex
	{
		get
		{
			return currentLyricIndex;
		}
		set
		{
			if (!EqualityComparer<int>.Default.Equals(currentLyricIndex, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.CurrentLyricIndex);
				currentLyricIndex = value;
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.CurrentLyricIndex);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.2.0.0")]
	[ExcludeFromCodeCoverage]
	public string LyricSource
	{
		get
		{
			return lyricSource;
		}
		[MemberNotNull("lyricSource")]
		set
		{
			if (!EqualityComparer<string>.Default.Equals(lyricSource, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.LyricSource);
				lyricSource = value;
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.LyricSource);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.2.0.0")]
	[ExcludeFromCodeCoverage]
	public ObservableCollection<Track> RecentTracks
	{
		get
		{
			return recentTracks;
		}
		[MemberNotNull("recentTracks")]
		set
		{
			if (!EqualityComparer<ObservableCollection<Track>>.Default.Equals(recentTracks, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.RecentTracks);
				recentTracks = value;
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.RecentTracks);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.2.0.0")]
	[ExcludeFromCodeCoverage]
	public double[] EqGains
	{
		get
		{
			return eqGains;
		}
		[MemberNotNull("eqGains")]
		set
		{
			if (!EqualityComparer<double[]>.Default.Equals(eqGains, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.EqGains);
				eqGains = value;
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.EqGains);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.2.0.0")]
	[ExcludeFromCodeCoverage]
	public bool EqualizerVisible
	{
		get
		{
			return equalizerVisible;
		}
		set
		{
			if (!EqualityComparer<bool>.Default.Equals(equalizerVisible, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.EqualizerVisible);
				equalizerVisible = value;
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.EqualizerVisible);
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.EqualizerVisibility);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.2.0.0")]
	[ExcludeFromCodeCoverage]
	public bool IsVlcAvailable
	{
		get
		{
			return isVlcAvailable;
		}
		set
		{
			if (!EqualityComparer<bool>.Default.Equals(isVlcAvailable, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.IsVlcAvailable);
				isVlcAvailable = value;
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.IsVlcAvailable);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.2.0.0")]
	[ExcludeFromCodeCoverage]
	public string StatusMessage
	{
		get
		{
			return statusMessage;
		}
		[MemberNotNull("statusMessage")]
		set
		{
			if (!EqualityComparer<string>.Default.Equals(statusMessage, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.StatusMessage);
				statusMessage = value;
				OnStatusMessageChanged(value);
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.StatusMessage);
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.StatusVisibility);
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.HasStatusMessage);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.2.0.0")]
	[ExcludeFromCodeCoverage]
	public IRelayCommand PlayCommand => playCommand ?? (playCommand = new RelayCommand(Play));

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.2.0.0")]
	[ExcludeFromCodeCoverage]
	public IRelayCommand PauseCommand => pauseCommand ?? (pauseCommand = new RelayCommand(Pause));

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.2.0.0")]
	[ExcludeFromCodeCoverage]
	public IRelayCommand TogglePlayPauseCommand => togglePlayPauseCommand ?? (togglePlayPauseCommand = new RelayCommand(TogglePlayPause));

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.2.0.0")]
	[ExcludeFromCodeCoverage]
	public IRelayCommand NextCommand => nextCommand ?? (nextCommand = new RelayCommand(Next));

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.2.0.0")]
	[ExcludeFromCodeCoverage]
	public IRelayCommand PreviousCommand => previousCommand ?? (previousCommand = new RelayCommand(Previous));

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.2.0.0")]
	[ExcludeFromCodeCoverage]
	public IRelayCommand StopCommand => stopCommand ?? (stopCommand = new RelayCommand(Stop));

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.2.0.0")]
	[ExcludeFromCodeCoverage]
	public IRelayCommand CycleModeCommand => cycleModeCommand ?? (cycleModeCommand = new RelayCommand(CycleMode));

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.2.0.0")]
	[ExcludeFromCodeCoverage]
	public IRelayCommand ToggleEqualizerCommand => toggleEqualizerCommand ?? (toggleEqualizerCommand = new RelayCommand(ToggleEqualizer));

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.2.0.0")]
	[ExcludeFromCodeCoverage]
	public IRelayCommand ApplyEqualizerCommand => applyEqualizerCommand ?? (applyEqualizerCommand = new RelayCommand(ApplyEqualizer));

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.2.0.0")]
	[ExcludeFromCodeCoverage]
	public IRelayCommand<string> ApplyEqPresetCommand => applyEqPresetCommand ?? (applyEqPresetCommand = new RelayCommand<string>(ApplyEqPreset));

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.2.0.0")]
	[ExcludeFromCodeCoverage]
	public IRelayCommand<Track?> PlayRecentCommand => playRecentCommand ?? (playRecentCommand = new RelayCommand<Track>(PlayRecent));

	public NowPlayingViewModel(AudioEngine engine, LyricService lyricService, LibraryService library, SettingsService? settingsService = null)
	{
		_uiDispatcher = DispatcherQueue.GetForCurrentThread();
		_engine = engine;
		_lyricService = lyricService;
		_library = library;
		_settingsService = settingsService;
		int initialVol = _engine.Volume > 0 ? _engine.Volume : (_settingsService?.Load().Volume > 0 ? _settingsService.Load().Volume : 80);
		volume = initialVol;
		_engine.Volume = initialVol;
		_engine.VolumeChanged += (int v) =>
		{
			RunOnUiThread(() =>
			{
				if (volume != v && v > 0)
				{
					volume = v;
					OnPropertyChanged(__KnownINotifyPropertyChangedArgs.Volume);
				}
			});
		};
		_engine.TrackChanged += OnTrackChanged;
		_engine.StateChanged += OnStateChanged;
		_engine.PositionChanged += HandlePositionChanged;
		_engine.TrackEnded += (Track _) =>
		{
			Task.Run((Func<Task?>)LoadLyricsAsync);
		};
		_engine.PlaybackError += OnPlaybackError;
		_engine.RenderingChanged += OnRenderingChanged;
		_engine.RenderProgress += OnRenderProgress;
	}

	private void RunOnUiThread(Action action)
	{
		if (_uiDispatcher == null || _uiDispatcher.HasThreadAccess)
		{
			action();
			return;
		}
		_uiDispatcher.TryEnqueue(() =>
		{
			action();
		});
	}

	private void OnRenderingChanged(bool rendering)
	{
		RunOnUiThread(() =>
		{
			if (rendering)
			{
				StatusMessage = "正在预渲染效果…完成后自动播放";
			}
			else if (StatusMessage.StartsWith("正在预渲染"))
			{
				StatusMessage = string.Empty;
			}
		});
	}

	private void OnRenderProgress(RenderProgress p)
	{
		RunOnUiThread(() =>
		{
			if (p.Fraction < 1.0)
			{
				StatusMessage = $"正在预渲染效果… {p.Fraction:P0}，预计剩余 {Math.Max(0, (int)p.Remaining.TotalSeconds)} 秒";
			}
			else
			{
				StatusMessage = "预渲染完成，开始播放";
			}
		});
	}

	private void OnStatusTimerTick(object? sender, object e)
	{
		try
		{
			_statusTimer?.Stop();
			StatusMessage = string.Empty;
		}
		catch
		{
		}
	}

	private void OnPlaybackError(Track track, string message)
	{
		RunOnUiThread(() =>
		{
			StatusMessage = message;
		});
	}

	public void Initialize()
	{
		IsVlcAvailable = _engine.IsVlcAvailable;
		Volume = _engine.Volume;
		IsPlaying = _engine.State == PlaybackState.Playing;
		ModeText = _engine.Mode switch
		{
			PlaybackMode.Sequential => "顺序播放", 
			PlaybackMode.RepeatOne => "单曲循环", 
			PlaybackMode.RepeatAll => "列表循环", 
			PlaybackMode.Shuffle => "随机播放", 
			_ => "顺序播放", 
		};
		if (CurrentTrack == null && _engine.CurrentTrack != null)
		{
			OnTrackChanged(_engine.CurrentTrack);
			Position = _engine.Position;
			if (Duration.TotalSeconds > 0.0)
			{
				PositionPercent = Position.TotalSeconds / Duration.TotalSeconds * 100.0;
			}
		}
		if (!string.IsNullOrEmpty(StatusMessage))
		{
			return;
		}
		if (_deviceCheckTask == null)
		{
			_deviceCheckTask = Task.Run(() =>
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
			});
		}
		if (_deviceCheckTask.IsCompleted && !_deviceCheckTask.Result)
		{
			StatusMessage = "未检测到音频输出设备：播放可能无声，请检查系统声音设置后重试。";
		}
	}

	private void OnTrackChanged(Track? track)
	{
		RunOnUiThread(() =>
		{
			bool isSameTrack = track != null && CurrentTrack != null && CurrentTrack.Id == track.Id;
			bool wasReferenceEqual = ReferenceEquals(currentTrack, track);
			if (track != null && track.DurationMilliseconds <= 0 && _engine.Duration.TotalMilliseconds > 0)
			{
				track.DurationMilliseconds = (long)_engine.Duration.TotalMilliseconds;
			}
			CurrentTrack = track;
			if (wasReferenceEqual)
			{
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.CurrentTrack);
			}
			CoverPath = track?.CoverPath ?? string.Empty;
			Title = track?.DisplayTitle ?? string.Empty;
			Artist = track?.Artist ?? string.Empty;
			Album = track?.Album ?? string.Empty;
			NowPlayingViewModel nowPlayingViewModel = this;
			TimeSpan timeSpan = ((track != null && track.DurationMilliseconds > 0) ? TimeSpan.FromMilliseconds(track.DurationMilliseconds) : ((_engine.Duration > TimeSpan.Zero) ? _engine.Duration : TimeSpan.Zero));
			nowPlayingViewModel.Duration = timeSpan;
			if (!isSameTrack)
			{
				Position = TimeSpan.Zero;
				PositionPercent = 0.0;
				LyricLines = new ObservableCollection<LyricLine>();
				CurrentLyricIndex = -1;
				LyricSource = track != null ? "正在加载歌词…" : string.Empty;
			}
			Task.Run((Func<Task?>)LoadLyricsAsync);

			ResolveStreamingCoverIfNeeded(track);
		});
	}

	private void ResolveStreamingCoverIfNeeded(Track? track)
	{
		if (track == null) return;
		bool isNetwork = track.Id < 0 ||
			string.Equals(track.Genre, "Online", StringComparison.OrdinalIgnoreCase) ||
			(!string.IsNullOrEmpty(track.FilePath) &&
			 (track.FilePath.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
			  track.FilePath.StartsWith("https://", StringComparison.OrdinalIgnoreCase)));

		if (!isNetwork) return;

		string coverDir = _library?.CoverDirectory ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "FurinaPlayer", "covers");
		try { Directory.CreateDirectory(coverDir); } catch { }

		long absId = Math.Abs(track.Id);
		string streamCacheFile = Path.Combine(coverDir, $"stream_{absId}.jpg");
		string netCacheFile = Path.Combine(coverDir, $"net_{absId}.jpg");

		// 1. 如果本地缓存文件已存在，立即采用本地缓存
		if (File.Exists(streamCacheFile))
		{
			track.CoverPath = streamCacheFile;
			CoverPath = streamCacheFile;
			return;
		}
		if (File.Exists(netCacheFile))
		{
			track.CoverPath = netCacheFile;
			CoverPath = netCacheFile;
			return;
		}

		// 2. 如果 track.CoverPath 自身就是本地已存在的文件，无需额外处理
		if (!string.IsNullOrEmpty(track.CoverPath) && File.Exists(track.CoverPath))
		{
			CoverPath = track.CoverPath;
			return;
		}

		// 3. 如果 track.CoverPath 包含网络图片 URL，先赋值给 CoverPath 并后台下载到本地缓存
		if (!string.IsNullOrEmpty(track.CoverPath) &&
			(track.CoverPath.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
			 track.CoverPath.StartsWith("https://", StringComparison.OrdinalIgnoreCase)))
		{
			string onlineUrl = track.CoverPath;
			_ = Task.Run(async () =>
			{
				try
				{
					using var hc = new System.Net.Http.HttpClient { Timeout = TimeSpan.FromSeconds(10) };
					var bytes = await hc.GetByteArrayAsync(onlineUrl);
					if (bytes != null && bytes.Length > 0)
					{
						await File.WriteAllBytesAsync(streamCacheFile, bytes);
						track.CoverPath = streamCacheFile;
						RunOnUiThread(() =>
						{
							if (CurrentTrack?.Id == track.Id)
							{
								CoverPath = streamCacheFile;
							}
						});
					}
				}
				catch { }
			});
			return;
		}

		// 4. 如果 CoverPath 为空，且为网易云流媒体歌曲 (absId > 0)，通过云端歌曲详情接口补充封面并缓存
		if (absId > 0)
		{
			_ = Task.Run(async () =>
			{
				try
				{
					using var hc = new System.Net.Http.HttpClient { Timeout = TimeSpan.FromSeconds(8) };
					hc.DefaultRequestHeaders.Referrer = new Uri("http://music.163.com/");
					string detailJson = await hc.GetStringAsync($"http://music.163.com/api/song/detail/?id={absId}&ids=[{absId}]");
					using var doc = System.Text.Json.JsonDocument.Parse(detailJson);
					if (doc.RootElement.TryGetProperty("songs", out var songsArr) && songsArr.GetArrayLength() > 0)
					{
						var s0 = songsArr[0];
						string? fetchedPic = null;
						if (s0.TryGetProperty("album", out var alb) && alb.TryGetProperty("picUrl", out var pProp))
						{
							fetchedPic = pProp.GetString();
						}
						else if (s0.TryGetProperty("al", out var al) && al.TryGetProperty("picUrl", out var alProp))
						{
							fetchedPic = alProp.GetString();
						}

						if (!string.IsNullOrWhiteSpace(fetchedPic))
						{
							if (fetchedPic.StartsWith("http://", StringComparison.OrdinalIgnoreCase))
							{
								fetchedPic = "https://" + fetchedPic[7..];
							}
							var imgBytes = await hc.GetByteArrayAsync(fetchedPic);
							if (imgBytes != null && imgBytes.Length > 0)
							{
								await File.WriteAllBytesAsync(streamCacheFile, imgBytes);
								track.CoverPath = streamCacheFile;
								RunOnUiThread(() =>
								{
									if (CurrentTrack?.Id == track.Id)
									{
										CoverPath = streamCacheFile;
									}
								});
							}
						}
					}
				}
				catch { }
			});
		}
	}

	public async Task RefreshCurrentCoverAsync()
	{
		long? num = CurrentTrack?.Id;
		if (!num.HasValue)
		{
			return;
		}
		try
		{
			Track track = await _library.GetTrackByIdAsync(num.Value);
			if (track != null && CurrentTrack != null)
			{
				CurrentTrack.CoverPath = track.CoverPath;
				string path = track.CoverPath ?? string.Empty;
				RunOnUiThread(() =>
				{
					CoverPath = path;
				});
			}
		}
		catch
		{
		}
	}

	private void OnStateChanged(PlaybackState state)
	{
		RunOnUiThread(() =>
		{
			IsPlaying = state == PlaybackState.Playing;
			if (CurrentTrack != null && _engine.Duration.TotalMilliseconds > 0)
			{
				if (CurrentTrack.DurationMilliseconds <= 0)
				{
					CurrentTrack.DurationMilliseconds = (long)_engine.Duration.TotalMilliseconds;
				}
				if (Duration <= TimeSpan.Zero)
				{
					Duration = _engine.Duration;
				}
			}
			if (state == PlaybackState.Playing && CurrentTrack != null)
			{
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.CurrentTrack);
				if (CurrentTrack.Id != _lastRecordedTrackId)
				{
					_lastRecordedTrackId = CurrentTrack.Id;
					long id = CurrentTrack.Id;
					Task.Run(async () =>
					{
						try
						{
							await _library.UpdatePlayStateAsync(id);
						}
						catch
						{
						}
					});
				}
			}
		});
	}

	private void HandlePositionChanged(TimeSpan pos)
	{
		RunOnUiThread(() =>
		{
			if (Duration <= TimeSpan.Zero && _engine.Duration > TimeSpan.Zero)
			{
				Duration = _engine.Duration;
				if (CurrentTrack != null && CurrentTrack.DurationMilliseconds <= 0)
				{
					CurrentTrack.DurationMilliseconds = (long)_engine.Duration.TotalMilliseconds;
				}
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.CurrentTrack);
			}
			if (!_positionSliderDragging)
			{
				Position = pos;
				PositionPercent = ((Duration.TotalSeconds > 0.0) ? (pos.TotalSeconds / Duration.TotalSeconds * 100.0) : 0.0);
				UpdateLyricHighlight(pos);
			}
		});
	}

	[RelayCommand]
	private void Play()
	{
		if (_engine.CurrentTrack == null && _engine.Queue.Count == 0)
		{
			StatusMessage = "请先在“首页”选择一首歌曲开始播放";
		}
		else
		{
			_engine.Play();
		}
	}

	[RelayCommand]
	private void Pause()
	{
		_engine.Pause();
	}

	[RelayCommand]
	private void TogglePlayPause()
	{
		if (_engine.CurrentTrack == null && _engine.Queue.Count == 0)
		{
			StatusMessage = "请先在“首页”选择一首歌曲开始播放";
		}
		else
		{
			_engine.TogglePlayPause();
		}
	}

	[RelayCommand]
	private void Next()
	{
		_engine.Next();
	}

	[RelayCommand]
	private void Previous()
	{
		_engine.Previous();
	}

	[RelayCommand]
	private void Stop()
	{
		_engine.Stop();
	}

	[RelayCommand]
	private void CycleMode()
	{
		_engine.Mode = (PlaybackMode)((int)(_engine.Mode + 1) % 4);
		ModeText = _engine.Mode switch
		{
			PlaybackMode.Sequential => "顺序播放", 
			PlaybackMode.RepeatOne => "单曲循环", 
			PlaybackMode.RepeatAll => "列表循环", 
			PlaybackMode.Shuffle => "随机播放", 
			_ => "顺序播放", 
		};
	}

	public void BeginSeek()
	{
		_positionSliderDragging = true;
	}

	public void CommitSeek(double percent)
	{
		_positionSliderDragging = false;
		if (!(Duration.TotalSeconds <= 0.0))
		{
			_engine.Seek(TimeSpan.FromSeconds(Duration.TotalSeconds * percent / 100.0));
		}
	}

	public AudioEngine Engine => _engine;

	public (double PeakDb, double RmsDb, float[] Bands, float StereoBalance, float PhaseCorrelation, float[] VectorPoints) GetLiveAudioMeters() => _engine.GetLiveAudioMeters();

	public (string Codec, int SampleRate, int BitDepth, int Channels, string OutputDevice, string OutputEngine, string OutputMode) GetAudioFormatDetails() => _engine.GetAudioFormatDetails();

	public void SeekSeconds(double seconds)
	{
		if (seconds < 0) seconds = 0;
		if (Duration.TotalSeconds > 0 && seconds > Duration.TotalSeconds) seconds = Duration.TotalSeconds;
		_engine.Seek(TimeSpan.FromSeconds(seconds));
	}

	public void SetVolumePercent(int vol)
	{
		Volume = Math.Clamp(vol, 0, 100);
	}

	public void SeekToPercent(double percent)
	{
		CommitSeek(percent);
	}

	[RelayCommand]
	private void ToggleEqualizer()
	{
		EqualizerVisible = !EqualizerVisible;
	}

	[RelayCommand]
	public void ApplyEqualizer()
	{
		_engine.SetEqGains(EqGains);
	}

	[RelayCommand]
	private void ApplyEqPreset(string presetName)
	{
		if (EqPresets.Presets.TryGetValue(presetName, out double[] value))
		{
			EqGains = (double[])value.Clone();
			_engine.SetEqGains(EqGains);
		}
	}

	public void ApplyDefaults()
	{
		Volume = 80;
		Speed = 1.0;
		EqGains = new double[10];
		_engine.SetEqGains(EqGains);
	}

	public async Task LoadRecentAsync(int count = 30)
	{
		try
		{
			List<Track> items = await _library.GetRecentAsync(count);
			RunOnUiThread(() =>
			{
				RecentTracks = new ObservableCollection<Track>(items);
			});
		}
		catch
		{
			RunOnUiThread(() =>
			{
				RecentTracks = new ObservableCollection<Track>();
			});
		}
	}

	[RelayCommand]
	private void PlayRecent(Track? track)
	{
		if (track != null)
		{
			if (_engine.CurrentTrack != null && _engine.CurrentTrack.Id == track.Id)
			{
				if (_engine.State == PlaybackState.Playing)
				{
					_engine.Seek(TimeSpan.Zero);
				}
				else
				{
					_engine.Play();
				}
				return;
			}
			List<Track> list = RecentTracks.ToList();
			int num = list.FindIndex((Track t) => t.Id == track.Id);
			if (num < 0)
			{
				list.Add(track);
			}
			num = list.FindIndex((Track t) => t.Id == track.Id);
			_engine.SetQueue(list, Math.Max(0, num));
			_engine.Play();
		}
	}

	public async Task<string> ImportLyricsBatchAsync(string[] lrcPaths)
	{
		if (lrcPaths.Length == 0)
		{
			return "未选择歌词文件";
		}
		List<Track> tracks = await _library.GetAllTracksAsync();
		int matched = 0;
		List<string> failed = new List<string>();
		foreach (string text in lrcPaths)
		{
			try
			{
				Track track = _lyricService.MatchLyricToTrack(text, tracks);
				if (track == null || string.IsNullOrEmpty(track.FilePath))
				{
					failed.Add(Path.GetFileName(text));
					continue;
				}
				string directoryName = Path.GetDirectoryName(track.FilePath);
				if (string.IsNullOrEmpty(directoryName))
				{
					failed.Add(Path.GetFileName(text));
					continue;
				}
				string destFileName = Path.Combine(directoryName, Path.GetFileNameWithoutExtension(track.FilePath) + ".lrc");
				File.Copy(text, destFileName, overwrite: true);
				matched++;
			}
			catch
			{
				failed.Add(Path.GetFileName(text));
			}
		}
		if (matched > 0)
		{
			await LoadLyricsAsync();
		}
		return (matched > 0) ? ($"批量导入完成：成功 {matched} 首" + ((failed.Count > 0) ? $"，未匹配 {failed.Count} 个文件" : "")) : ("未匹配到对应歌曲的歌词文件" + ((failed.Count > 0) ? $"（{failed.Count} 个文件）" : ""));
	}

	public async Task LoadLyricsAsync()
	{
		Track? track = CurrentTrack;
		if (track == null)
		{
			RunOnUiThread(() =>
			{
				LyricLines = new ObservableCollection<LyricLine>();
				CurrentLyricIndex = -1;
				LyricSource = string.Empty;
			});
			return;
		}

		long targetTrackId = track.Id;
		LyricDocument? doc = null;

		// 1. 如果是流媒体曲目（track.Id < 0），优先使用网易云原生精准 ID 直接提取歌词
		long absId = Math.Abs(track.Id);
		if (track.Id < 0 && absId > 0)
		{
			doc = await _lyricService.GetNeteaseLyricByIdAsync(absId);
		}

		// 2. 本地歌曲或内嵌歌词
		if (doc == null && !string.IsNullOrEmpty(track.FilePath) && !track.FilePath.StartsWith("http", StringComparison.OrdinalIgnoreCase))
		{
			doc = _lyricService.LoadLocalLyrics(track.FilePath, track.EmbeddedLyrics);
		}

		// 3. 在线检索 LRCLIB
		if (doc == null && !string.IsNullOrWhiteSpace(track.DisplayTitle))
		{
			doc = await _lyricService.SearchOnlineAsync(track.DisplayTitle, track.Artist);
		}

		// 4. 网易云云端检索
		if (doc == null && !string.IsNullOrWhiteSpace(track.DisplayTitle))
		{
			doc = await _lyricService.SearchNeteaseAsync(track.DisplayTitle, track.Artist);
		}

		// 竞态校验：若已切歌，丢弃旧结果
		if (CurrentTrack?.Id != targetTrackId)
		{
			return;
		}

		RunOnUiThread(() =>
		{
			if (CurrentTrack?.Id != targetTrackId) return;

			if (doc != null && doc.Lines.Count > 0)
			{
				LyricLines = new ObservableCollection<LyricLine>(doc.Lines);
				LyricSource = doc.Source switch
				{
					"local" => "本地歌词",
					"embedded" => "内嵌歌词",
					"lrclib" => "LRCLIB 在线",
					"netease" => "网易云在线",
					_ => "在线歌词"
				};
				UpdateLyricHighlight(Position);
			}
			else
			{
				LyricLines = new ObservableCollection<LyricLine>();
				CurrentLyricIndex = -1;
				LyricSource = "未找到歌词";
			}
		});
	}

	private void UpdateLyricHighlight(TimeSpan pos)
	{
		if (LyricLines.Count != 0)
		{
			int item = LyricIndices(pos).Cur;
			CurrentLyricIndex = item;
		}
	}

	private (int Cur, int Next) LyricIndices(TimeSpan pos)
	{
		int item = -1;
		for (int i = 0; i < LyricLines.Count; i++)
		{
			if (LyricLines[i].Time <= pos)
			{
				item = i;
				continue;
			}
			return (Cur: item, Next: i);
		}
		return (Cur: item, Next: -1);
	}

	public void Dispose()
	{
		_engine.TrackChanged -= OnTrackChanged;
		_engine.StateChanged -= OnStateChanged;
		_engine.PositionChanged -= HandlePositionChanged;
		_engine.PlaybackError -= OnPlaybackError;
		_engine.RenderingChanged -= OnRenderingChanged;
		_engine.RenderProgress -= OnRenderProgress;
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.2.0.0")]
	private void OnVolumeChanged(int value)
	{
		_engine.Volume = value;
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.2.0.0")]
	private void OnSpeedChanged(double value)
	{
		_engine.Speed = value;
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.2.0.0")]
	private void OnStatusMessageChanged(string value)
	{
		try
		{
			if (_statusTimer == null && _uiDispatcher != null)
			{
				_statusTimer = _uiDispatcher.CreateTimer();
				_statusTimer.Interval = TimeSpan.FromMilliseconds(3000.0);
				_statusTimer.Tick += OnStatusTimerTick;
			}
			_statusTimer?.Stop();
			if (!string.IsNullOrEmpty(value))
			{
				_statusTimer?.Start();
			}
		}
		catch
		{
		}
	}
}
