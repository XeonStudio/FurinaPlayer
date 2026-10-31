using System;
using SonicWave.Audio;
using SonicWave.Core.Services;

namespace FurinaPlayer.Avalonia.ViewModels;

public enum PageKind
{
    Home,
    NowPlaying,
    AudioEditor,
    DspEffects,
    Settings
}

public sealed class MainViewModel : ViewModelBase
{
    private PageKind _currentPage = PageKind.Home;
    private string _pageTitle = "音乐库";

    public HomeViewModel HomeVm { get; }
    public MiniPlayerViewModel MiniPlayerVm { get; }
    public NowPlayingViewModel NowPlayingVm { get; }
    public DspViewModel DspVm { get; }
    public EditorViewModel EditorVm { get; }

    public PageKind CurrentPage
    {
        get => _currentPage;
        set
        {
            if (SetProperty(ref _currentPage, value))
            {
                OnPropertyChanged(nameof(IsHomeActive));
                OnPropertyChanged(nameof(IsNowPlayingActive));
                OnPropertyChanged(nameof(IsAudioEditorActive));
                OnPropertyChanged(nameof(IsDspActive));
                OnPropertyChanged(nameof(IsSettingsActive));

                PageTitle = value switch
                {
                    PageKind.Home => "音乐库",
                    PageKind.NowPlaying => "正在播放",
                    PageKind.AudioEditor => "音频编辑器",
                    PageKind.DspEffects => "效果器与 DSP",
                    PageKind.Settings => "设置与个性化",
                    _ => "Furina Player"
                };
            }
        }
    }

    public string PageTitle
    {
        get => _pageTitle;
        private set => SetProperty(ref _pageTitle, value);
    }

    public bool IsHomeActive => CurrentPage == PageKind.Home;
    public bool IsNowPlayingActive => CurrentPage == PageKind.NowPlaying;
    public bool IsAudioEditorActive => CurrentPage == PageKind.AudioEditor;
    public bool IsDspActive => CurrentPage == PageKind.DspEffects;
    public bool IsSettingsActive => CurrentPage == PageKind.Settings;

    public MainViewModel(LibraryService library, AudioEngine engine, SettingsService? settings)
    {
        HomeVm = new HomeViewModel(library, engine, settings);
        MiniPlayerVm = new MiniPlayerViewModel(engine, settings);
        NowPlayingVm = new NowPlayingViewModel(engine, settings, MiniPlayerVm);
        DspVm = new DspViewModel(engine, settings);
        EditorVm = new EditorViewModel(engine, settings);
    }

    public void NavigateTo(PageKind page)
    {
        CurrentPage = page;
    }
}
