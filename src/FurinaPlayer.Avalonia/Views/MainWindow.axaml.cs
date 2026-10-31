using System;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using FurinaPlayer.Avalonia.ViewModels;

namespace FurinaPlayer.Avalonia.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    private MainViewModel? Vm => DataContext as MainViewModel;

    private void OnTitleBarPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            BeginMoveDrag(e);
        }
    }

    private void OnMinimizeClick(object? sender, RoutedEventArgs e)
    {
        WindowState = WindowState.Minimized;
    }

    private void OnMaximizeClick(object? sender, RoutedEventArgs e)
    {
        WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
    }

    private void OnCloseClick(object? sender, RoutedEventArgs e)
    {
        Close();
    }

    private void UpdateNavButtons(Button activeButton)
    {
        BtnNavHome.Classes.Clear();
        BtnNavNowPlaying.Classes.Clear();
        BtnNavDsp.Classes.Clear();
        BtnNavEditor.Classes.Clear();
        BtnNavSettings.Classes.Clear();

        BtnNavHome.Classes.Add("nav-item");
        BtnNavNowPlaying.Classes.Add("nav-item");
        BtnNavDsp.Classes.Add("nav-item");
        BtnNavEditor.Classes.Add("nav-item");
        BtnNavSettings.Classes.Add("nav-item");

        activeButton.Classes.Clear();
        activeButton.Classes.Add("nav-active");
    }

    private void OnNavHomeClick(object? sender, RoutedEventArgs e)
    {
        Vm?.NavigateTo(PageKind.Home);
        UpdateNavButtons(BtnNavHome);
    }

    private void OnNavNowPlayingClick(object? sender, RoutedEventArgs e)
    {
        Vm?.NavigateTo(PageKind.NowPlaying);
        UpdateNavButtons(BtnNavNowPlaying);
    }

    private void OnNavDspClick(object? sender, RoutedEventArgs e)
    {
        Vm?.NavigateTo(PageKind.DspEffects);
        UpdateNavButtons(BtnNavDsp);
    }

    private void OnNavEditorClick(object? sender, RoutedEventArgs e)
    {
        Vm?.NavigateTo(PageKind.AudioEditor);
        UpdateNavButtons(BtnNavEditor);
    }

    private void OnNavSettingsClick(object? sender, RoutedEventArgs e)
    {
        Vm?.NavigateTo(PageKind.Settings);
        UpdateNavButtons(BtnNavSettings);
    }
}
