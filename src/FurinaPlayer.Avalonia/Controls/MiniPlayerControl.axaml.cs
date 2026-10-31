using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using FurinaPlayer.Avalonia.ViewModels;

namespace FurinaPlayer.Avalonia.Controls;

public partial class MiniPlayerControl : UserControl
{
    public MiniPlayerControl()
    {
        InitializeComponent();
    }

    private MiniPlayerViewModel? Vm => DataContext as MiniPlayerViewModel;

    private void OnPlayPauseClick(object? sender, RoutedEventArgs e)
    {
        Vm?.TogglePlayPause();
    }

    private void OnPreviousClick(object? sender, RoutedEventArgs e)
    {
        Vm?.Previous();
    }

    private void OnNextClick(object? sender, RoutedEventArgs e)
    {
        Vm?.Next();
    }

    private void OnToggleModeClick(object? sender, RoutedEventArgs e)
    {
        Vm?.ToggleMode();
    }

    private void OnMuteClick(object? sender, RoutedEventArgs e)
    {
        Vm?.ToggleMute();
    }

    private void OnSliderPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        Vm?.StartSeek();
    }

    private void OnSliderPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (SeekSlider != null)
        {
            Vm?.EndSeek(SeekSlider.Value);
        }
    }
}
