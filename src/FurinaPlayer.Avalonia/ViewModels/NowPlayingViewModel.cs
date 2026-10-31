using System;
using System.Collections.ObjectModel;
using System.IO;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using SonicWave.Audio;
using SonicWave.Audio.Dsp;
using SonicWave.Core.Models;
using SonicWave.Core.Services;

namespace FurinaPlayer.Avalonia.ViewModels;

public enum NowPlayingThemeMode
{
    Classic,
    AuditionDaw,
    PowerPoint
}

public class LyricDisplayItem : ViewModelBase
{
    private bool _isActive;
    public TimeSpan Time { get; set; }
    public string Text { get; set; } = string.Empty;
    public string Subtitle { get; set; } = string.Empty;

    public bool IsActive
    {
        get => _isActive;
        set => SetProperty(ref _isActive, value);
    }
}

public sealed class NowPlayingViewModel : ViewModelBase
{
    private readonly AudioEngine _engine;
    private readonly SettingsService? _settings;
    private readonly DispatcherTimer _updateTimer;

    private NowPlayingThemeMode _currentTheme = NowPlayingThemeMode.Classic;
    private ObservableCollection<LyricDisplayItem> _lyrics = new();
    private int _activeLyricIndex = -1;
    private string _activeLyricText = "未匹配到歌词";

    // Audition DAW & Insight 2 Metrics
    private string _dawTimecode = "00:00.000";
    private float _momentaryLufs = -24.0f;
    private float _shortTermLufs = -23.5f;
    private float _integratedLufs = -23.0f;
    private float _loudnessRange = 4.2f;
    private float _correlation = 0.88f;
    private float _truePeakDb = -1.2f;

    // PowerPoint Theme Metrics
    private string _pptSlideInfo = "幻灯片 6 共 8 张";
    private string _pptWordInfo = "歌词: 已播 239 字 / 共 860 字";
    private string _pptMemoNote = "单击此处添加演示文档备注 (当前曲目信息与备忘录)";

    public MiniPlayerViewModel MiniPlayerVm { get; }

    public NowPlayingThemeMode CurrentTheme
    {
        get => _currentTheme;
        set
        {
            if (SetProperty(ref _currentTheme, value))
            {
                OnPropertyChanged(nameof(IsClassicActive));
                OnPropertyChanged(nameof(IsAuditionActive));
                OnPropertyChanged(nameof(IsPowerPointActive));
                OnPropertyChanged(nameof(ThemeButtonLabel));
            }
        }
    }

    public bool IsClassicActive => CurrentTheme == NowPlayingThemeMode.Classic;
    public bool IsAuditionActive => CurrentTheme == NowPlayingThemeMode.AuditionDaw;
    public bool IsPowerPointActive => CurrentTheme == NowPlayingThemeMode.PowerPoint;

    public string ThemeButtonLabel => CurrentTheme switch
    {
        NowPlayingThemeMode.Classic => "经典歌词 模式",
        NowPlayingThemeMode.AuditionDaw => "Audition DAW 模式",
        NowPlayingThemeMode.PowerPoint => "PowerPoint 主题",
        _ => "切换主题"
    };

    public ObservableCollection<LyricDisplayItem> Lyrics
    {
        get => _lyrics;
        private set => SetProperty(ref _lyrics, value);
    }

    public int ActiveLyricIndex
    {
        get => _activeLyricIndex;
        private set => SetProperty(ref _activeLyricIndex, value);
    }

    public string ActiveLyricText
    {
        get => _activeLyricText;
        private set => SetProperty(ref _activeLyricText, value);
    }

    // DAW Insight 2 Pro properties
    public string DawTimecode
    {
        get => _dawTimecode;
        private set => SetProperty(ref _dawTimecode, value);
    }

    public float MomentaryLufs
    {
        get => _momentaryLufs;
        private set => SetProperty(ref _momentaryLufs, value);
    }

    public float ShortTermLufs
    {
        get => _shortTermLufs;
        private set => SetProperty(ref _shortTermLufs, value);
    }

    public float IntegratedLufs
    {
        get => _integratedLufs;
        private set => SetProperty(ref _integratedLufs, value);
    }

    public float LoudnessRange
    {
        get => _loudnessRange;
        private set => SetProperty(ref _loudnessRange, value);
    }

    public float Correlation
    {
        get => _correlation;
        private set => SetProperty(ref _correlation, value);
    }

    public float TruePeakDb
    {
        get => _truePeakDb;
        private set => SetProperty(ref _truePeakDb, value);
    }

    public string PptSlideInfo
    {
        get => _pptSlideInfo;
        private set => SetProperty(ref _pptSlideInfo, value);
    }

    public string PptWordInfo
    {
        get => _pptWordInfo;
        private set => SetProperty(ref _pptWordInfo, value);
    }

    public string PptMemoNote
    {
        get => _pptMemoNote;
        set => SetProperty(ref _pptMemoNote, value);
    }

    public NowPlayingViewModel(AudioEngine engine, SettingsService? settings, MiniPlayerViewModel miniPlayerVm)
    {
        _engine = engine;
        _settings = settings;
        MiniPlayerVm = miniPlayerVm;

        LoadDefaultSampleLyrics();

        _updateTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(50)
        };
        _updateTimer.Tick += OnUpdateTick;
        _updateTimer.Start();
    }

    private void OnUpdateTick(object? sender, EventArgs e)
    {
        var pos = _engine.Position;
        DawTimecode = $"{pos.Minutes:00}:{pos.Seconds:00}.{pos.Milliseconds:000}";

        // Read real loudness from LockFreeAudioVisualBuffer
        var buf = LockFreeAudioVisualBuffer.Instance;
        buf.GetLoudnessInfo(out float mom, out float sh, out float tp, out float cor);

        if (_engine.CurrentTrack != null)
        {
            MomentaryLufs = Math.Clamp(mom, -70.0f, 6.0f);
            ShortTermLufs = Math.Clamp(sh, -70.0f, 6.0f);
            TruePeakDb = Math.Clamp(tp, -60.0f, 3.0f);
            Correlation = Math.Clamp(cor, -1.0f, 1.0f);
            IntegratedLufs = -23.0f;
            LoudnessRange = 4.2f;
        }

        // Synchronize lyrics
        UpdateActiveLyric(pos);
    }

    private void UpdateActiveLyric(TimeSpan pos)
    {
        if (Lyrics.Count == 0) return;

        int newIdx = -1;
        for (int i = 0; i < Lyrics.Count; i++)
        {
            if (Lyrics[i].Time <= pos)
            {
                newIdx = i;
            }
            else
            {
                break;
            }
        }

        if (newIdx != ActiveLyricIndex)
        {
            ActiveLyricIndex = newIdx;
            for (int i = 0; i < Lyrics.Count; i++)
            {
                Lyrics[i].IsActive = (i == newIdx);
            }

            if (newIdx >= 0 && newIdx < Lyrics.Count)
            {
                ActiveLyricText = Lyrics[newIdx].Text;
                PptSlideInfo = $"幻灯片 {newIdx + 1} 共 {Lyrics.Count} 张";
                PptWordInfo = $"歌词: 已播 {ActiveLyricText.Length * (newIdx + 1)} 字 / 幽静封映之月";
            }
        }
    }

    public void CycleTheme()
    {
        CurrentTheme = CurrentTheme switch
        {
            NowPlayingThemeMode.Classic => NowPlayingThemeMode.AuditionDaw,
            NowPlayingThemeMode.AuditionDaw => NowPlayingThemeMode.PowerPoint,
            NowPlayingThemeMode.PowerPoint => NowPlayingThemeMode.Classic,
            _ => NowPlayingThemeMode.Classic
        };
    }

    public void SetTheme(NowPlayingThemeMode mode)
    {
        CurrentTheme = mode;
    }

    private void LoadDefaultSampleLyrics()
    {
        Lyrics.Clear();
        Lyrics.Add(new LyricDisplayItem { Time = TimeSpan.FromSeconds(0), Text = "飘曳的眠梦 (The Swinging Motion of the Hulls)", Subtitle = "Outside It Is Growing Dark" });
        Lyrics.Add(new LyricDisplayItem { Time = TimeSpan.FromSeconds(12), Text = "海风轻抚着古老月港的桅杆", Subtitle = "Gentle sea breeze embraces the ancient harbor" });
        Lyrics.Add(new LyricDisplayItem { Time = TimeSpan.FromSeconds(25), Text = "月影漫过幽蓝如镜的渊泉之底", Subtitle = "Moonlight cascades across abyssal crystal depths" });
        Lyrics.Add(new LyricDisplayItem { Time = TimeSpan.FromSeconds(38), Text = "纯水流转着未曾被遗忘的颂歌", Subtitle = "Pure waters carry forward the forgotten hymn" });
        Lyrics.Add(new LyricDisplayItem { Time = TimeSpan.FromSeconds(52), Text = "于静谧终焉之际，星辰指引归途", Subtitle = "Stars guide our voyage at tranquility's eve" });
        Lyrics.Add(new LyricDisplayItem { Time = TimeSpan.FromSeconds(68), Text = "拂去往昔潮汐沉淀的尘埃与叹息", Subtitle = "Brushing away tides of memory and time" });
        Lyrics.Add(new LyricDisplayItem { Time = TimeSpan.FromSeconds(85), Text = "愿静穆之梦常驻波澜不惊的夜晚", Subtitle = "May quiet dreams abide in serene night" });
    }
}
