using Avalonia.Controls;
using Avalonia.Interactivity;
using FurinaPlayer.Avalonia.ViewModels;

namespace FurinaPlayer.Avalonia.Views;

public partial class NowPlayingPageView : UserControl
{
    public NowPlayingPageView()
    {
        InitializeComponent();
    }

    private NowPlayingViewModel? Vm => DataContext as NowPlayingViewModel;

    private void OnSelectClassicClick(object? sender, RoutedEventArgs e)
    {
        Vm?.SetTheme(NowPlayingThemeMode.Classic);
    }

    private void OnSelectAuditionClick(object? sender, RoutedEventArgs e)
    {
        Vm?.SetTheme(NowPlayingThemeMode.AuditionDaw);
    }

    private void OnSelectPowerPointClick(object? sender, RoutedEventArgs e)
    {
        Vm?.SetTheme(NowPlayingThemeMode.PowerPoint);
    }

    private void OnCycleModeClick(object? sender, RoutedEventArgs e)
    {
        Vm?.MiniPlayerVm.ToggleMode();
    }

    private void OnPreviousClick(object? sender, RoutedEventArgs e)
    {
        Vm?.MiniPlayerVm.Previous();
    }

    private void OnPlayPauseClick(object? sender, RoutedEventArgs e)
    {
        Vm?.MiniPlayerVm.TogglePlayPause();
    }

    private void OnNextClick(object? sender, RoutedEventArgs e)
    {
        Vm?.MiniPlayerVm.Next();
    }

    private void OnSpeedClick(object? sender, RoutedEventArgs e)
    {
        // Toggle speeds 1.0x -> 1.25x -> 1.5x -> 0.75x -> 1.0x
        if (Vm != null)
        {
            var btn = sender as Button;
            var tb = btn?.Content as TextBlock;
            if (tb != null)
            {
                if (tb.Text == "1.0x") tb.Text = "1.25x";
                else if (tb.Text == "1.25x") tb.Text = "1.5x";
                else if (tb.Text == "1.5x") tb.Text = "0.75x";
                else tb.Text = "1.0x";
            }
        }
    }

    private void OnEqQuickClick(object? sender, RoutedEventArgs e)
    {
        if (VisualRoot is MainWindow mw && mw.DataContext is MainViewModel mainVm)
        {
            mainVm.NavigateTo(PageKind.DspEffects);
        }
    }
}
