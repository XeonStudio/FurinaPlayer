using Avalonia.Controls;
using Avalonia.Interactivity;
using FurinaPlayer.Avalonia.ViewModels;

namespace FurinaPlayer.Avalonia.Views;

public partial class EditorPageView : UserControl
{
    public EditorPageView()
    {
        InitializeComponent();
    }

    private EditorViewModel? Vm => DataContext as EditorViewModel;

    private void OnSetMarkerAClick(object? sender, RoutedEventArgs e)
    {
        Vm?.SetMarkerAAtCurrentPosition();
    }

    private void OnSetMarkerBClick(object? sender, RoutedEventArgs e)
    {
        Vm?.SetMarkerBAtCurrentPosition();
    }

    private void OnClearLoopClick(object? sender, RoutedEventArgs e)
    {
        Vm?.ClearLoop();
    }

    private void OnExportSelectionClick(object? sender, RoutedEventArgs e)
    {
        Vm?.ExportSelection();
    }

    private void OnSpeed075Click(object? sender, RoutedEventArgs e)
    {
        if (Vm != null) Vm.PlaybackSpeed = 0.75;
    }

    private void OnSpeed100Click(object? sender, RoutedEventArgs e)
    {
        if (Vm != null) Vm.PlaybackSpeed = 1.0;
    }

    private void OnSpeed125Click(object? sender, RoutedEventArgs e)
    {
        if (Vm != null) Vm.PlaybackSpeed = 1.25;
    }

    private void OnSpeed150Click(object? sender, RoutedEventArgs e)
    {
        if (Vm != null) Vm.PlaybackSpeed = 1.5;
    }
}
