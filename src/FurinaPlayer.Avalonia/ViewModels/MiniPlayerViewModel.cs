using System;
using System.IO;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using FurinaPlayer.Avalonia.Controls;
using SonicWave.Audio;
using SonicWave.Core.Models;
using SonicWave.Core.Services;

namespace FurinaPlayer.Avalonia.ViewModels;

public sealed class MiniPlayerViewModel : ViewModelBase
{
    private readonly AudioEngine _engine;
    private readonly SettingsService? _settingsService;
    private readonly DispatcherTimer _posTimer;

    private Track? _currentTrack;
    private string _title = "等待播放歌曲";
    private string _artist = "Furina Player";
    private string _album = string.Empty;
    private string? _coverPath;
    private Bitmap? _coverImage;
    private bool _isPlaying;
    private string _positionText = "00:00";
    private string _durationText = "00:00";
    private double _positionMs;
    private double _durationMs = 100.0;
    private int _volume = 80;
    private int _prevVolume = 80;
    private bool _isMuted;
    private PlaybackMode _mode = PlaybackMode.Sequential;
    private string _formatBadge = "FLAC / 96 kHz";
    private bool _isUserDragging;

    public Track? CurrentTrack
    {
        get => _currentTrack;
        private set => SetProperty(ref _currentTrack, value);
    }

    public string Title
    {
        get => _title;
        private set => SetProperty(ref _title, value);
    }

    public string Artist
    {
        get => _artist;
        private set => SetProperty(ref _artist, value);
    }

    public string Album
    {
        get => _album;
        private set => SetProperty(ref _album, value);
    }

    public string? CoverPath
    {
        get => _coverPath;
        private set => SetProperty(ref _coverPath, value);
    }

    public Bitmap? CoverImage
    {
        get => _coverImage;
        private set => SetProperty(ref _coverImage, value);
    }

    public bool IsPlaying
    {
        get => _isPlaying;
        private set => SetProperty(ref _isPlaying, value);
    }

    public string PositionText
    {
        get => _positionText;
        private set => SetProperty(ref _positionText, value);
    }

    public string DurationText
    {
        get => _durationText;
        private set => SetProperty(ref _durationText, value);
    }

    public double PositionMs
    {
        get => _positionMs;
        set
        {
            if (SetProperty(ref _positionMs, value))
            {
                PositionText = FormatTime(TimeSpan.FromMilliseconds(value));
            }
        }
    }

    public double DurationMs
    {
        get => _durationMs;
        private set => SetProperty(ref _durationMs, Math.Max(1.0, value));
    }

    public int Volume
    {
        get => _volume;
        set
        {
            int clamped = Math.Clamp(value, 0, 100);
            if (SetProperty(ref _volume, clamped))
            {
                _engine.Volume = clamped;
                IsMuted = clamped == 0;
            }
        }
    }

    public bool IsMuted
    {
        get => _isMuted;
        set => SetProperty(ref _isMuted, value);
    }

    public PlaybackMode Mode
    {
        get => _mode;
        set => SetProperty(ref _mode, value);
    }

    public FluentIconKind ModeIconKind => Mode switch
    {
        PlaybackMode.RepeatOne => FluentIconKind.ModeLoopOne,
        PlaybackMode.RepeatAll => FluentIconKind.ModeLoopAll,
        PlaybackMode.Shuffle => FluentIconKind.ModeShuffle,
        _ => FluentIconKind.ModeSequential
    };

    public string ModeTooltip => Mode switch
    {
        PlaybackMode.RepeatOne => "单曲循环",
        PlaybackMode.RepeatAll => "列表循环",
        PlaybackMode.Shuffle => "随机播放",
        _ => "顺序播放"
    };

    public string FormatBadge
    {
        get => _formatBadge;
        private set => SetProperty(ref _formatBadge, value);
    }

    public MiniPlayerViewModel(AudioEngine engine, SettingsService? settingsService)
    {
        _engine = engine;
        _settingsService = settingsService;

        _volume = _engine.Volume;
        _mode = _engine.Mode;

        _engine.TrackChanged += OnTrackChanged;
        _engine.StateChanged += OnStateChanged;
        _engine.VolumeChanged += v =>
        {
            Dispatcher.UIThread.Post(() =>
            {
                _volume = v;
                OnPropertyChanged(nameof(Volume));
                IsMuted = v == 0;
            });
        };

        // 100ms real-time progress clock
        _posTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(100)
        };
        _posTimer.Tick += OnTimerTick;
        _posTimer.Start();

        SyncTrackInfo(_engine.CurrentTrack);
    }

    private void OnStateChanged(PlaybackState state)
    {
        Dispatcher.UIThread.Post(() =>
        {
            IsPlaying = (state == PlaybackState.Playing);
        });
    }

    private void OnTrackChanged(Track? track)
    {
        Dispatcher.UIThread.Post(() =>
        {
            SyncTrackInfo(track);
        });
    }

    private void SyncTrackInfo(Track? track)
    {
        CurrentTrack = track;
        if (track == null)
        {
            Title = "等待播放歌曲";
            Artist = "Furina Player";
            Album = string.Empty;
            DurationText = "00:00";
            DurationMs = 100.0;
            PositionText = "00:00";
            PositionMs = 0;
            FormatBadge = "READY";
            LoadCover(null);
            return;
        }

        Title = track.DisplayTitle;
        Artist = string.IsNullOrWhiteSpace(track.Artist) ? "未知艺术家" : track.Artist;
        Album = string.IsNullOrWhiteSpace(track.Album) ? "未知专辑" : track.Album;
        DurationMs = Math.Max(1.0, track.DurationMilliseconds);
        DurationText = FormatTime(TimeSpan.FromMilliseconds(track.DurationMilliseconds));

        // Format info
        string fmt = track.Format.ToString().ToUpperInvariant();
        if (track.SampleRate > 0)
        {
            FormatBadge = $"{fmt} / {track.SampleRate / 1000.0:0.#} kHz / {track.BitDepth}bit";
        }
        else
        {
            FormatBadge = fmt;
        }

        LoadCover(track.CoverPath);
    }

    private void LoadCover(string? path)
    {
        CoverPath = path;
        CoverImage = null;

        if (!string.IsNullOrEmpty(path) && File.Exists(path))
        {
            try
            {
                CoverImage = new Bitmap(path);
            }
            catch
            {
                CoverImage = null;
            }
        }
    }

    private void OnTimerTick(object? sender, EventArgs e)
    {
        if (_isUserDragging) return;

        if (_engine.CurrentTrack != null)
        {
            var pos = _engine.Position;
            var dur = _engine.Duration;

            if (dur.TotalMilliseconds > 0)
            {
                DurationMs = dur.TotalMilliseconds;
                DurationText = FormatTime(dur);
            }

            _positionMs = pos.TotalMilliseconds;
            OnPropertyChanged(nameof(PositionMs));
            PositionText = FormatTime(pos);
        }
    }

    public void TogglePlayPause()
    {
        _engine.TogglePlayPause();
    }

    public void Next()
    {
        _engine.Next();
    }

    public void Previous()
    {
        _engine.Previous();
    }

    public void ToggleMode()
    {
        Mode = Mode switch
        {
            PlaybackMode.Sequential => PlaybackMode.RepeatAll,
            PlaybackMode.RepeatAll => PlaybackMode.RepeatOne,
            PlaybackMode.RepeatOne => PlaybackMode.Shuffle,
            _ => PlaybackMode.Sequential
        };

        _engine.Mode = Mode;
        OnPropertyChanged(nameof(ModeIconKind));
        OnPropertyChanged(nameof(ModeTooltip));

        _settingsService?.Update(s => s.PlaybackMode = Mode);
    }

    public void ToggleMute()
    {
        if (IsMuted)
        {
            Volume = _prevVolume > 0 ? _prevVolume : 50;
        }
        else
        {
            _prevVolume = Volume;
            Volume = 0;
        }
    }

    public void StartSeek()
    {
        _isUserDragging = true;
    }

    public void EndSeek(double targetMs)
    {
        _isUserDragging = false;
        _engine.Seek(TimeSpan.FromMilliseconds(targetMs));
    }

    private static string FormatTime(TimeSpan t)
    {
        if (t.TotalHours >= 1.0)
            return t.ToString(@"h\:mm\:ss");
        return t.ToString(@"m\:ss");
    }
}
