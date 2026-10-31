using Avalonia.Controls;
using Avalonia.Interactivity;
using FurinaPlayer.Avalonia.ViewModels;

namespace FurinaPlayer.Avalonia.Views;

public partial class DspPageView : UserControl
{
    public DspPageView()
    {
        InitializeComponent();
    }

    private DspViewModel? Vm => DataContext as DspViewModel;

    private void OnResetEqClick(object? sender, RoutedEventArgs e)
    {
        Vm?.ResetEq();
    }

    private void OnBassBoostClick(object? sender, RoutedEventArgs e)
    {
        Vm?.ApplyBassBoost();
    }

    private void OnVocalBoostClick(object? sender, RoutedEventArgs e)
    {
        Vm?.ApplyVocalBoost();
    }
}
