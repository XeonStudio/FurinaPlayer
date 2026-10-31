using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using FurinaPlayer.Avalonia.ViewModels;
using SonicWave.Core.Models;

namespace FurinaPlayer.Avalonia.Views;

public partial class HomePageView : UserControl
{
    public HomePageView()
    {
        InitializeComponent();
    }

    private HomeViewModel? Vm => DataContext as HomeViewModel;

    private async void OnAddFilesClick(object? sender, RoutedEventArgs e)
    {
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel == null) return;

        var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "选择要导入的音频文件",
            AllowMultiple = true,
            FileTypeFilter = new List<FilePickerFileType>
            {
                new("音频文件 (*.flac, *.mp3, *.wav, *.dsf, *.dff, *.m4a, *.ogg)")
                {
                    Patterns = new[] { "*.flac", "*.mp3", "*.wav", "*.dsf", "*.dff", "*.m4a", "*.aac", "*.ogg", "*.ape" }
                },
                new("所有文件 (*.*)")
                {
                    Patterns = new[] { "*.*" }
                }
            }
        });

        if (files != null && files.Count > 0)
        {
            var paths = files.Select(f => f.Path.LocalPath).ToList();
            if (Vm != null)
            {
                await Vm.ImportFilesAsync(paths);
            }
        }
    }

    private async void OnScanFolderClick(object? sender, RoutedEventArgs e)
    {
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel == null) return;

        var folders = await topLevel.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            Title = "选择包含音频的音乐文件夹",
            AllowMultiple = false
        });

        if (folders != null && folders.Count > 0)
        {
            string path = folders[0].Path.LocalPath;
            if (Vm != null)
            {
                await Vm.ScanFolderAsync(path);
            }
        }
    }

    private void OnToggleViewClick(object? sender, RoutedEventArgs e)
    {
        Vm?.ToggleView();
    }

    private async void OnRefreshClick(object? sender, RoutedEventArgs e)
    {
        if (Vm != null)
        {
            await Vm.RefreshLibraryAsync();
        }
    }

    private void OnPlayTrackClick(object? sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: Track track })
        {
            Vm?.PlayTrack(track);
        }
    }

    private async void OnDeleteTrackClick(object? sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: Track track })
        {
            if (Vm != null)
            {
                await Vm.RemoveTrackAsync(track);
            }
        }
    }

    private void OnTrackDoubleTapped(object? sender, TappedEventArgs e)
    {
        if (Vm?.SelectedTrack != null)
        {
            Vm.PlayTrack(Vm.SelectedTrack);
        }
    }
}
