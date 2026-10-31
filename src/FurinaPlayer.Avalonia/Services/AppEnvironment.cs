using System;
using System.IO;

namespace FurinaPlayer.Avalonia.Services;

public static class AppEnvironment
{
    public static string DataFolder { get; }
    public static string SettingsFile { get; }
    public static string DatabaseFile { get; }
    public static string WallpapersFolder { get; }
    public static string CoversFolder { get; }

    static AppEnvironment()
    {
        string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        string preferred = Path.Combine(localAppData, "FurinaPlayer");

        try
        {
            if (!Directory.Exists(preferred))
            {
                Directory.CreateDirectory(preferred);
            }
            DataFolder = preferred;
        }
        catch
        {
            DataFolder = Path.Combine(AppContext.BaseDirectory, "Data");
            Directory.CreateDirectory(DataFolder);
        }

        SettingsFile = Path.Combine(DataFolder, "settings.json");
        DatabaseFile = Path.Combine(DataFolder, "library.db");
        WallpapersFolder = Path.Combine(DataFolder, "wallpapers");
        CoversFolder = Path.Combine(DataFolder, "covers");

        Directory.CreateDirectory(WallpapersFolder);
        Directory.CreateDirectory(CoversFolder);
    }
}
