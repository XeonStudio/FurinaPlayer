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

	public bool UseAuditionDawMode { get; set; } = false;

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

	// ECHO Audio DSP Core Settings
	public bool EchoHeadroomEnabled { get; set; } = true;
	public double EchoHeadroomDb { get; set; } = -3.0;
	public bool EchoDynamicLoudnessCompensation { get; set; } = false;
	public bool EchoCompressorEnabled { get; set; } = false;
	public double EchoCompressorThresholdDb { get; set; } = -18.0;
	public double EchoCompressorRatio { get; set; } = 4.0;
	public double EchoCompressorAttackMs { get; set; } = 10.0;
	public double EchoCompressorReleaseMs { get; set; } = 120.0;
	public double EchoCompressorKneeDb { get; set; } = 6.0;
	public double EchoCompressorMakeupDb { get; set; } = 0.0;
	public double EchoCompressorMix { get; set; } = 1.0;
	public string EchoCompressorDetectorMode { get; set; } = "Peak";
	public bool EchoCompressorSidechainHighpassEnabled { get; set; } = false;
	public double EchoCompressorSidechainHighpassHz { get; set; } = 80.0;
	public bool EchoCompressorAutoRelease { get; set; } = false;
	public double EchoCompressorRangeDb { get; set; } = 60.0;
	public double EchoCompressorStereoLink { get; set; } = 1.0;

	// FurinaPlayer Multiband Compressor Settings
	public bool FurinaMultibandEnabled { get; set; } = false;
	public double FurinaMultibandLowCrossoverHz { get; set; } = 150.0;
	public double FurinaMultibandHighCrossoverHz { get; set; } = 2500.0;
	public double FurinaMultibandMix { get; set; } = 1.0;
	public double FurinaMultibandStereoLink { get; set; } = 1.0;

	// Band 0: Low
	public bool FurinaMultibandLowEnabled { get; set; } = true;
	public double FurinaMultibandLowThresholdDb { get; set; } = -18.0;
	public double FurinaMultibandLowRatio { get; set; } = 2.0;
	public double FurinaMultibandLowAttackMs { get; set; } = 10.0;
	public double FurinaMultibandLowReleaseMs { get; set; } = 120.0;
	public double FurinaMultibandLowKneeDb { get; set; } = 6.0;
	public double FurinaMultibandLowMakeupDb { get; set; } = 0.0;

	// Band 1: Mid
	public bool FurinaMultibandMidEnabled { get; set; } = true;
	public double FurinaMultibandMidThresholdDb { get; set; } = -18.0;
	public double FurinaMultibandMidRatio { get; set; } = 2.0;
	public double FurinaMultibandMidAttackMs { get; set; } = 10.0;
	public double FurinaMultibandMidReleaseMs { get; set; } = 120.0;
	public double FurinaMultibandMidKneeDb { get; set; } = 6.0;
	public double FurinaMultibandMidMakeupDb { get; set; } = 0.0;

	// Band 2: High
	public bool FurinaMultibandHighEnabled { get; set; } = true;
	public double FurinaMultibandHighThresholdDb { get; set; } = -18.0;
	public double FurinaMultibandHighRatio { get; set; } = 2.0;
	public double FurinaMultibandHighAttackMs { get; set; } = 10.0;
	public double FurinaMultibandHighReleaseMs { get; set; } = 120.0;
	public double FurinaMultibandHighKneeDb { get; set; } = 6.0;
	public double FurinaMultibandHighMakeupDb { get; set; } = 0.0;
	public bool EchoCrossfeedEnabled { get; set; } = false;
	public double EchoCrossfeedAmount { get; set; } = 0.35;
	public double EchoCrossfeedCutoffHz { get; set; } = 700.0;
	public bool EchoStereoFieldEnabled { get; set; } = false;
	public double EchoStereoWidth { get; set; } = 1.0;
	public double EchoStereoCenterGainDb { get; set; } = 0.0;
	public double EchoStereoSideGainDb { get; set; } = 0.0;
	public int EchoChannelMatrixPreset { get; set; } = 0;

	// FurinaPlayer Channel Tool Settings (声道工具 / 偏音补偿 / 声像平衡)
	public bool FurinaChannelToolEnabled { get; set; } = false;
	public double FurinaChannelBalance { get; set; } = 0.0;
	public double FurinaChannelLeftGainDb { get; set; } = 0.0;
	public double FurinaChannelRightGainDb { get; set; } = 0.0;
	public double FurinaChannelTrimStepDb { get; set; } = 0.25;
	public bool FurinaChannelConstantPower { get; set; } = false;
	public double FurinaChannelLeftDelayMs { get; set; } = 0.0;
	public double FurinaChannelRightDelayMs { get; set; } = 0.0;
	public double FurinaChannelLowLeftGainDb { get; set; } = 0.0;
	public double FurinaChannelLowRightGainDb { get; set; } = 0.0;
	public double FurinaChannelMidLeftGainDb { get; set; } = 0.0;
	public double FurinaChannelMidRightGainDb { get; set; } = 0.0;
	public double FurinaChannelHighLeftGainDb { get; set; } = 0.0;
	public double FurinaChannelHighRightGainDb { get; set; } = 0.0;
	public string FurinaChannelMonoMode { get; set; } = "off";
	public bool FurinaChannelSwapLeftRight { get; set; } = false;
	public bool FurinaChannelInvertLeft { get; set; } = false;
	public bool FurinaChannelInvertRight { get; set; } = false;
	public string FurinaChannelProMode { get; set; } = "simple";
	public string? FurinaChannelPresetsJson { get; set; }

	// ECHO 向前兼容属性映射
	public bool EchoChannelToolEnabled { get => FurinaChannelToolEnabled; set => FurinaChannelToolEnabled = value; }
	public double EchoChannelBalance { get => FurinaChannelBalance; set => FurinaChannelBalance = value; }
	public double EchoChannelLeftGainDb { get => FurinaChannelLeftGainDb; set => FurinaChannelLeftGainDb = value; }
	public double EchoChannelRightGainDb { get => FurinaChannelRightGainDb; set => FurinaChannelRightGainDb = value; }

	public bool FurinaReplayGainEnabled { get; set; } = false;
	public double FurinaReplayGainTargetLufs { get; set; } = -14.0;
	public int FurinaReplayGainMode { get; set; } = 1;
	public double FurinaReplayGainPreampDb { get; set; } = 0.0;
	public double FurinaReplayGainUnknownDb { get; set; } = -5.0;
	public bool FurinaReplayGainPreventClipping { get; set; } = true;
	public bool FurinaReplayGainTruePeak { get; set; } = true;
	public bool FurinaReplayGainAnalyzeOnScan { get; set; } = false;

	// 向前兼容属性映射
	public bool EchoReplayGainEnabled { get => FurinaReplayGainEnabled; set => FurinaReplayGainEnabled = value; }
	public double EchoReplayGainTargetLufs { get => FurinaReplayGainTargetLufs; set => FurinaReplayGainTargetLufs = value; }
	public int EchoReplayGainMode { get => FurinaReplayGainMode; set => FurinaReplayGainMode = value; }
	public double EchoReplayGainPreampDb { get => FurinaReplayGainPreampDb; set => FurinaReplayGainPreampDb = value; }
	public double EchoReplayGainUnknownDb { get => FurinaReplayGainUnknownDb; set => FurinaReplayGainUnknownDb = value; }
	public bool EchoReplayGainPreventClipping { get => FurinaReplayGainPreventClipping; set => FurinaReplayGainPreventClipping = value; }
	public bool EchoReplayGainTruePeak { get => FurinaReplayGainTruePeak; set => FurinaReplayGainTruePeak = value; }
	public bool EchoReplayGainAnalyzeOnScan { get => FurinaReplayGainAnalyzeOnScan; set => FurinaReplayGainAnalyzeOnScan = value; }
	public bool EchoConvolutionEnabled { get; set; } = false;
	public string? EchoConvolutionFilePath { get; set; }
	public double EchoConvolutionTrimDb { get; set; } = 0.0;
	public bool EchoResamplerEnabled { get; set; } = false;
	public int EchoResamplerTargetRate { get; set; } = 192000;
	public string FurinaSrcMode { get; set; } = "off";
	public string FurinaSrcQuality { get; set; } = "transparent";
	public string FurinaSrcFilter1x { get; set; } = "furina-gauss-long";
	public string FurinaSrcFilterNx { get; set; } = "furina-hb";
	public string FurinaSrcCompute { get; set; } = "simd";
	public string FurinaSrcDither { get; set; } = "off";
	public bool FurinaSrcAdvanced { get; set; } = false;
	public bool FurinaSrcAbBenchmark { get; set; } = false;
	public bool EchoSdmEnabled { get; set; } = false;
	public int EchoSdmOrder { get; set; } = 5;
	public int EchoSdmRate { get; set; } = 64;

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
