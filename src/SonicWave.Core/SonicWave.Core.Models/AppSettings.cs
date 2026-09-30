using System.Collections.Generic;

namespace SonicWave.Core.Models;

public sealed class AppSettings
{
	public OutputMode OutputMode { get; set; }

	public string? OutputDeviceId { get; set; }

	public int SampleRate { get; set; }

	public int BitDepth { get; set; }

	public int BufferMilliseconds { get; set; } = 200;

	public DsdOutputMode DsdMode { get; set; } = DsdOutputMode.Dop;

	public bool VolumeNormalization { get; set; }

	public int Volume { get; set; } = 5;

	public bool GaplessPlayback { get; set; } = true;

	public bool CrossfadeEnabled { get; set; }

	public int CrossfadeSeconds { get; set; } = 3;

	public PlaybackMode PlaybackMode { get; set; }

	public double PlaybackSpeed { get; set; } = 1.0;

	public bool KeepPitchOnSpeed { get; set; } = true;

	public ThemeMode Theme { get; set; }

	public WallpaperSource WallpaperSource { get; set; }

	public string? LocalWallpaperPath { get; set; }

	public BackgroundMaterial BackgroundMaterial { get; set; }

	public string? AccentColorHex { get; set; }

	public int FontScalePercent { get; set; } = 100;

	public bool ShowMiniPlayer { get; set; } = true;

	public bool LyricsPanelExpanded { get; set; } = true;

	public int EqPresetIndex { get; set; }

	public double[] EqGains { get; set; } = new double[10];

	public bool ReverbEnabled { get; set; }

	public double ReverbMix { get; set; } = 0.2;

	public string ReverbPreset { get; set; } = "大厅";

	public double SpatialWidth { get; set; } = 0.6;

	public double SpatialModulation { get; set; } = 0.25;

	public double SpatialPredelayMs { get; set; } = 25.0;

	public double PitchShiftSemitones { get; set; }

	public double TimbreBass { get; set; }

	public double TimbreMidrange { get; set; }

	public double TimbreTreble { get; set; }

	public double TimbreThickness { get; set; }

	public double TimbreClarity { get; set; }

	public double TimbreSoundstage { get; set; }

	public Dictionary<string, double[]> PanelLayouts { get; set; } = new Dictionary<string, double[]>();

	public string? LastPlayedTrackPath { get; set; }

	public long LastPlayedPositionMs { get; set; }

	public List<string> LastQueuePaths { get; set; } = new List<string>();

	public List<string> Vst3ScanFolders { get; set; } = new List<string>();

	public List<Vst3PluginState> Vst3Plugins { get; set; } = new List<Vst3PluginState>();

	public List<string> MusicFolders { get; set; } = new List<string>();

	public string? CacheDirectory { get; set; }

	public string? ExportDirectory { get; set; }

	public bool AutoImportNewFiles { get; set; } = true;

	public bool EnableGlobalHotkeys { get; set; } = true;

	public string HotkeyPlayPause { get; set; } = "MediaPlayPause";

	public string HotkeyNext { get; set; } = "MediaNextTrack";

	public string HotkeyPrevious { get; set; } = "MediaPreviousTrack";

	public string Language { get; set; } = "zh-CN";

	public bool CheckUpdatesOnStartup { get; set; } = true;

	public string LyricFontFamily { get; set; } = string.Empty;

	public string? LyricFontFilePath { get; set; }

	public double AlbumColumnWidth { get; set; } = 140.0;

	public int? WindowLeft { get; set; }

	public int? WindowTop { get; set; }

	public int? WindowWidth { get; set; }

	public int? WindowHeight { get; set; }

	public bool WindowMaximized { get; set; }
}
