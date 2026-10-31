using System;
using System.IO;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using FurinaPlayer.Avalonia.Services;
using FurinaPlayer.Avalonia.ViewModels;
using FurinaPlayer.Avalonia.Views;
using SonicWave.Audio;
using SonicWave.Core.Models;
using SonicWave.Core.Services;

namespace FurinaPlayer.Avalonia;

public partial class App : Application
{
    public static AudioEngine? AudioEngine { get; private set; }
    public static LibraryService? LibraryService { get; private set; }
    public static SettingsService? SettingsService { get; private set; }
    public static AlbumArtService? AlbumArtService { get; private set; }
    public static LyricService? LyricService { get; private set; }
    public static MainViewModel? MainVm { get; private set; }

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override async void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            try
            {
                // Initialize core services
                SettingsService = new SettingsService(AppEnvironment.SettingsFile);
                var settings = SettingsService.Load();

                LibraryService = new LibraryService(AppEnvironment.DatabaseFile);
                await LibraryService.InitializeAsync();

                AlbumArtService = new AlbumArtService(LibraryService, AppEnvironment.CoversFolder);
                LyricService = new LyricService();

                AudioEngine = new AudioEngine();
                AudioEngine.ConfigureOutput(settings.OutputMode, settings.OutputDeviceId, settings.OutputDeviceId, settings.BufferMilliseconds);
                AudioEngine.Volume = settings.Volume;
                AudioEngine.Mode = settings.PlaybackMode;

                // Sync volume changes to settings
                AudioEngine.VolumeChanged += v =>
                {
                    try { SettingsService?.Update(s => s.Volume = v); } catch { }
                };

                MainVm = new MainViewModel(LibraryService, AudioEngine, SettingsService);

                var mainWindow = new MainWindow
                {
                    DataContext = MainVm
                };
                desktop.MainWindow = mainWindow;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[FurinaPlayer.Avalonia Startup Error] {ex}");
            }
        }

        base.OnFrameworkInitializationCompleted();
    }
}
