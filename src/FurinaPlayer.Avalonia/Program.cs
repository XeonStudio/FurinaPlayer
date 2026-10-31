using System;
using System.IO;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using FurinaPlayer.Avalonia.ViewModels;
using FurinaPlayer.Avalonia.Views;

namespace FurinaPlayer.Avalonia;

public static class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        var appBuilder = BuildAvaloniaApp();

        if (args.Length >= 2 && args[0].Equals("--capture-pages", StringComparison.OrdinalIgnoreCase))
        {
            string outDir = args[1];
            Directory.CreateDirectory(outDir);

            appBuilder.AfterSetup(builder =>
            {
                Dispatcher.UIThread.Post(async () =>
                {
                    try
                    {
                        if (App.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop &&
                            desktop.MainWindow is MainWindow win &&
                            win.DataContext is MainViewModel mainVm)
                        {
                            // Wait for UI layout & render
                            await Task.Delay(1000);

                            // Capture Home Page
                            mainVm.NavigateTo(PageKind.Home);
                            await Task.Delay(500);
                            CaptureWindow(win, Path.Combine(outDir, "avalonia_home.png"));

                            // Capture Now Playing Page - Classic Theme
                            mainVm.NavigateTo(PageKind.NowPlaying);
                            mainVm.NowPlayingVm.SetTheme(NowPlayingThemeMode.Classic);
                            await Task.Delay(500);
                            CaptureWindow(win, Path.Combine(outDir, "avalonia_playing_classic.png"));

                            // Capture Now Playing Page - Audition DAW Theme
                            mainVm.NowPlayingVm.SetTheme(NowPlayingThemeMode.AuditionDaw);
                            await Task.Delay(500);
                            CaptureWindow(win, Path.Combine(outDir, "avalonia_playing_audition.png"));

                            // Capture Now Playing Page - PowerPoint Theme
                            mainVm.NowPlayingVm.SetTheme(NowPlayingThemeMode.PowerPoint);
                            await Task.Delay(500);
                            CaptureWindow(win, Path.Combine(outDir, "avalonia_playing_powerpoint.png"));

                            // Reset back to Classic for default playing view
                            mainVm.NowPlayingVm.SetTheme(NowPlayingThemeMode.Classic);
                            await Task.Delay(300);

                            // Capture DSP Page
                            mainVm.NavigateTo(PageKind.DspEffects);
                            await Task.Delay(500);
                            CaptureWindow(win, Path.Combine(outDir, "avalonia_dsp.png"));

                            // Capture Audio Editor Page
                            mainVm.NavigateTo(PageKind.AudioEditor);
                            await Task.Delay(500);
                            CaptureWindow(win, Path.Combine(outDir, "avalonia_editor.png"));

                            // Capture Settings Page
                            mainVm.NavigateTo(PageKind.Settings);
                            await Task.Delay(500);
                            CaptureWindow(win, Path.Combine(outDir, "avalonia_settings.png"));

                            // End-to-End Playback Test
                            if (mainVm.HomeVm.VisibleTracks.Count > 0)
                            {
                                var track = mainVm.HomeVm.VisibleTracks[0];
                                Console.WriteLine($"[TEST PLAYBACK] Starting playback for: {track.DisplayTitle} ({track.FilePath})");
                                mainVm.HomeVm.PlayTrack(track);

                                // Navigate to Now Playing to observe live visual state
                                mainVm.NavigateTo(PageKind.NowPlaying);
                                await Task.Delay(1500);

                                Console.WriteLine($"[TEST PLAYBACK] Current Title: {mainVm.MiniPlayerVm.Title}, IsPlaying: {mainVm.MiniPlayerVm.IsPlaying}, Pos: {mainVm.MiniPlayerVm.PositionText} ({mainVm.MiniPlayerVm.PositionMs}ms)");
                                CaptureWindow(win, Path.Combine(outDir, "avalonia_live_playing.png"));

                                // Test Pause
                                mainVm.MiniPlayerVm.TogglePlayPause();
                                await Task.Delay(300);
                                Console.WriteLine($"[TEST PLAYBACK] After Pause IsPlaying: {mainVm.MiniPlayerVm.IsPlaying}");

                                // Test Resume
                                mainVm.MiniPlayerVm.TogglePlayPause();
                                await Task.Delay(300);
                                Console.WriteLine($"[TEST PLAYBACK] After Resume IsPlaying: {mainVm.MiniPlayerVm.IsPlaying}");

                                // Test Seek
                                mainVm.MiniPlayerVm.StartSeek();
                                mainVm.MiniPlayerVm.EndSeek(5000);
                                await Task.Delay(500);
                                Console.WriteLine($"[TEST PLAYBACK] After Seek Pos: {mainVm.MiniPlayerVm.PositionText} ({mainVm.MiniPlayerVm.PositionMs}ms)");
                            }

                            Console.WriteLine("[AVALONIA TEST CAPTURE SUCCESS]");
                            desktop.Shutdown(0);
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[AVALONIA TEST CAPTURE ERROR] {ex}");
                        Environment.Exit(1);
                    }
                });
            });
        }

        appBuilder.StartWithClassicDesktopLifetime(args);
    }

    private static void CaptureWindow(MainWindow window, string destinationPath)
    {
        int w = (int)Math.Max(800, window.Bounds.Width > 0 ? window.Bounds.Width : window.Width);
        int h = (int)Math.Max(600, window.Bounds.Height > 0 ? window.Bounds.Height : window.Height);

        using var rtb = new RenderTargetBitmap(new PixelSize(w, h), new Vector(96, 96));
        rtb.Render(window);
        rtb.Save(destinationPath);
        Console.WriteLine($"Saved capture: {destinationPath}");
    }

    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();
}
