using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text.Json;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel.__Internals;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Dispatching;
using SonicWave.Audio;
using SonicWave.Audio.Dsp;
using SonicWave.Audio.Vst3;
using SonicWave.Core.Models;
using SonicWave.Core.Services;
using WinRT;

namespace FurinaPlayer.UI.ViewModels;

[WinRTRuntimeClassName("Microsoft.UI.Xaml.Data.INotifyPropertyChanged")]
[WinRTExposedType(typeof(DspViewModelWinRTTypeDetails))]
public class DspViewModel : ObservableObject
{
	private readonly AudioEngine _engine;

	public AudioEngine AudioEngine => _engine;

	private readonly SettingsService _settingsService;

	private readonly DispatcherQueue? _uiDispatcher;

	private string _lastSavedVst3Signature = string.Empty;

	[ObservableProperty]
	private double[] eqGains = new double[10];

	[ObservableProperty]
	private bool reverbEnabled;

	[ObservableProperty]
	private double reverbMix = 0.2;

	[ObservableProperty]
	private string reverbPreset = "大厅";

	[ObservableProperty]
	private double spatialWidth = 0.6;

	[ObservableProperty]
	private double spatialModulation = 0.25;

	[ObservableProperty]
	private double spatialPredelayMs = 25.0;

	[ObservableProperty]
	private double speed = 1.0;

	[ObservableProperty]
	private string eqPresetName = "自定义";

	[ObservableProperty]
	private string statusText = "调节立即生效并随设置保存";

	[ObservableProperty]
	private bool isRendering;

	[ObservableProperty]
	private string renderStatusText = "效果播放前先预渲染，渲染完成后开始播放";

	[ObservableProperty]
	private double renderProgressValue;

	[ObservableProperty]
	private ObservableCollection<string> vst3Folders = new ObservableCollection<string>();

	[ObservableProperty]
	private ObservableCollection<Vst3PluginState> vst3Plugins = new ObservableCollection<Vst3PluginState>();

	[ObservableProperty]
	private string vst3StatusText = "Steinberg VST3 64位原生宿主就绪 · 扫描目录或加载 .vst3 插件即可实时渲染与交互";

	[ObservableProperty]
	private double timbreBass;

	[ObservableProperty]
	private double timbreMidrange;

	[ObservableProperty]
	private double timbreTreble;

	[ObservableProperty]
	private double timbreThickness;

	[ObservableProperty]
	private double timbreClarity;

	[ObservableProperty]
	private double timbreSoundstage;

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.2.0.0")]
	private RelayCommand<string?>? addVst3FolderCommand;

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.2.0.0")]
	private RelayCommand<string?>? removeVst3FolderCommand;

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.2.0.0")]
	private RelayCommand? scanVst3Command;

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.2.0.0")]
	private RelayCommand? applyVst3Command;

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.2.0.0")]
	private RelayCommand<string>? applyPresetCommand;

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.2.0.0")]
	private RelayCommand? resetAllCommand;

	public SettingsService SettingsService => _settingsService;

	public AudioEngine Engine => _engine;

	public int EngineSampleRate => _engine.CurrentSampleRate;

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.2.0.0")]
	[ExcludeFromCodeCoverage]
	public double[] EqGains
	{
		get
		{
			return eqGains;
		}
		[MemberNotNull("eqGains")]
		set
		{
			if (!EqualityComparer<double[]>.Default.Equals(eqGains, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.EqGains);
				eqGains = value;
				OnEqGainsChanged(value);
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.EqGains);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.2.0.0")]
	[ExcludeFromCodeCoverage]
	public bool ReverbEnabled
	{
		get
		{
			return reverbEnabled;
		}
		set
		{
			if (!EqualityComparer<bool>.Default.Equals(reverbEnabled, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.ReverbEnabled);
				reverbEnabled = value;
				OnReverbEnabledChanged(value);
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.ReverbEnabled);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.2.0.0")]
	[ExcludeFromCodeCoverage]
	public double ReverbMix
	{
		get
		{
			return reverbMix;
		}
		set
		{
			if (!EqualityComparer<double>.Default.Equals(reverbMix, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.ReverbMix);
				reverbMix = value;
				OnReverbMixChanged(value);
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.ReverbMix);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.2.0.0")]
	[ExcludeFromCodeCoverage]
	public string ReverbPreset
	{
		get
		{
			return reverbPreset;
		}
		[MemberNotNull("reverbPreset")]
		set
		{
			if (!EqualityComparer<string>.Default.Equals(reverbPreset, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.ReverbPreset);
				reverbPreset = value;
				OnReverbPresetChanged(value);
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.ReverbPreset);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.2.0.0")]
	[ExcludeFromCodeCoverage]
	public double SpatialWidth
	{
		get
		{
			return spatialWidth;
		}
		set
		{
			if (!EqualityComparer<double>.Default.Equals(spatialWidth, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.SpatialWidth);
				spatialWidth = value;
				OnSpatialWidthChanged(value);
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.SpatialWidth);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.2.0.0")]
	[ExcludeFromCodeCoverage]
	public double SpatialModulation
	{
		get
		{
			return spatialModulation;
		}
		set
		{
			if (!EqualityComparer<double>.Default.Equals(spatialModulation, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.SpatialModulation);
				spatialModulation = value;
				OnSpatialModulationChanged(value);
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.SpatialModulation);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.2.0.0")]
	[ExcludeFromCodeCoverage]
	public double SpatialPredelayMs
	{
		get
		{
			return spatialPredelayMs;
		}
		set
		{
			if (!EqualityComparer<double>.Default.Equals(spatialPredelayMs, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.SpatialPredelayMs);
				spatialPredelayMs = value;
				OnSpatialPredelayMsChanged(value);
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.SpatialPredelayMs);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.2.0.0")]
	[ExcludeFromCodeCoverage]
	public double Speed
	{
		get
		{
			return speed;
		}
		set
		{
			if (!EqualityComparer<double>.Default.Equals(speed, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.Speed);
				speed = value;
				OnSpeedChanged(value);
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.Speed);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.2.0.0")]
	[ExcludeFromCodeCoverage]
	public string EqPresetName
	{
		get
		{
			return eqPresetName;
		}
		[MemberNotNull("eqPresetName")]
		set
		{
			if (!EqualityComparer<string>.Default.Equals(eqPresetName, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.EqPresetName);
				eqPresetName = value;
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.EqPresetName);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.2.0.0")]
	[ExcludeFromCodeCoverage]
	public string StatusText
	{
		get
		{
			return statusText;
		}
		[MemberNotNull("statusText")]
		set
		{
			if (!EqualityComparer<string>.Default.Equals(statusText, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.StatusText);
				statusText = value;
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.StatusText);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.2.0.0")]
	[ExcludeFromCodeCoverage]
	public bool IsRendering
	{
		get
		{
			return isRendering;
		}
		set
		{
			if (!EqualityComparer<bool>.Default.Equals(isRendering, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.IsRendering);
				isRendering = value;
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.IsRendering);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.2.0.0")]
	[ExcludeFromCodeCoverage]
	public string RenderStatusText
	{
		get
		{
			return renderStatusText;
		}
		[MemberNotNull("renderStatusText")]
		set
		{
			if (!EqualityComparer<string>.Default.Equals(renderStatusText, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.RenderStatusText);
				renderStatusText = value;
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.RenderStatusText);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.2.0.0")]
	[ExcludeFromCodeCoverage]
	public double RenderProgressValue
	{
		get
		{
			return renderProgressValue;
		}
		set
		{
			if (!EqualityComparer<double>.Default.Equals(renderProgressValue, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.RenderProgressValue);
				renderProgressValue = value;
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.RenderProgressValue);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.2.0.0")]
	[ExcludeFromCodeCoverage]
	public ObservableCollection<string> Vst3Folders
	{
		get
		{
			return vst3Folders;
		}
		[MemberNotNull("vst3Folders")]
		set
		{
			if (!EqualityComparer<ObservableCollection<string>>.Default.Equals(vst3Folders, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.Vst3Folders);
				vst3Folders = value;
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.Vst3Folders);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.2.0.0")]
	[ExcludeFromCodeCoverage]
	public ObservableCollection<Vst3PluginState> Vst3Plugins
	{
		get
		{
			return vst3Plugins;
		}
		[MemberNotNull("vst3Plugins")]
		set
		{
			if (!EqualityComparer<ObservableCollection<Vst3PluginState>>.Default.Equals(vst3Plugins, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.Vst3Plugins);
				vst3Plugins = value;
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.Vst3Plugins);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.2.0.0")]
	[ExcludeFromCodeCoverage]
	public string Vst3StatusText
	{
		get
		{
			return vst3StatusText;
		}
		[MemberNotNull("vst3StatusText")]
		set
		{
			if (!EqualityComparer<string>.Default.Equals(vst3StatusText, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.Vst3StatusText);
				vst3StatusText = value;
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.Vst3StatusText);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.2.0.0")]
	[ExcludeFromCodeCoverage]
	public double TimbreBass
	{
		get
		{
			return timbreBass;
		}
		set
		{
			if (!EqualityComparer<double>.Default.Equals(timbreBass, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.TimbreBass);
				timbreBass = value;
				OnTimbreBassChanged(value);
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.TimbreBass);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.2.0.0")]
	[ExcludeFromCodeCoverage]
	public double TimbreMidrange
	{
		get
		{
			return timbreMidrange;
		}
		set
		{
			if (!EqualityComparer<double>.Default.Equals(timbreMidrange, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.TimbreMidrange);
				timbreMidrange = value;
				OnTimbreMidrangeChanged(value);
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.TimbreMidrange);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.2.0.0")]
	[ExcludeFromCodeCoverage]
	public double TimbreTreble
	{
		get
		{
			return timbreTreble;
		}
		set
		{
			if (!EqualityComparer<double>.Default.Equals(timbreTreble, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.TimbreTreble);
				timbreTreble = value;
				OnTimbreTrebleChanged(value);
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.TimbreTreble);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.2.0.0")]
	[ExcludeFromCodeCoverage]
	public double TimbreThickness
	{
		get
		{
			return timbreThickness;
		}
		set
		{
			if (!EqualityComparer<double>.Default.Equals(timbreThickness, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.TimbreThickness);
				timbreThickness = value;
				OnTimbreThicknessChanged(value);
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.TimbreThickness);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.2.0.0")]
	[ExcludeFromCodeCoverage]
	public double TimbreClarity
	{
		get
		{
			return timbreClarity;
		}
		set
		{
			if (!EqualityComparer<double>.Default.Equals(timbreClarity, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.TimbreClarity);
				timbreClarity = value;
				OnTimbreClarityChanged(value);
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.TimbreClarity);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.2.0.0")]
	[ExcludeFromCodeCoverage]
	public double TimbreSoundstage
	{
		get
		{
			return timbreSoundstage;
		}
		set
		{
			if (!EqualityComparer<double>.Default.Equals(timbreSoundstage, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.TimbreSoundstage);
				timbreSoundstage = value;
				OnTimbreSoundstageChanged(value);
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.TimbreSoundstage);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.2.0.0")]
	[ExcludeFromCodeCoverage]
	public IRelayCommand<string?> AddVst3FolderCommand => addVst3FolderCommand ?? (addVst3FolderCommand = new RelayCommand<string>(AddVst3Folder));

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.2.0.0")]
	[ExcludeFromCodeCoverage]
	public IRelayCommand<string?> RemoveVst3FolderCommand => removeVst3FolderCommand ?? (removeVst3FolderCommand = new RelayCommand<string>(RemoveVst3Folder));

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.2.0.0")]
	[ExcludeFromCodeCoverage]
	public IRelayCommand ScanVst3Command => scanVst3Command ?? (scanVst3Command = new RelayCommand(ScanVst3));

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.2.0.0")]
	[ExcludeFromCodeCoverage]
	public IRelayCommand ApplyVst3Command => applyVst3Command ?? (applyVst3Command = new RelayCommand(ApplyVst3));

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.2.0.0")]
	[ExcludeFromCodeCoverage]
	public IRelayCommand<string> ApplyPresetCommand => applyPresetCommand ?? (applyPresetCommand = new RelayCommand<string>(ApplyPreset));

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.2.0.0")]
	[ExcludeFromCodeCoverage]
	public IRelayCommand ResetAllCommand => resetAllCommand ?? (resetAllCommand = new RelayCommand(ResetAll));

	public event Action<double[]>? EqGainsChanged;

	public DspViewModel(AudioEngine engine, SettingsService settingsService)
	{
		_uiDispatcher = DispatcherQueue.GetForCurrentThread();
		_engine = engine;
		_settingsService = settingsService;
		_engine.RenderingChanged += OnRenderingChanged;
		_engine.RenderProgress += OnRenderProgress;
		AppSettings appSettings = _settingsService.Load();
		EqGains = (double[])appSettings.EqGains.Clone();
		ReverbEnabled = appSettings.ReverbEnabled;
		ReverbMix = appSettings.ReverbMix;
		ReverbPreset = (string.IsNullOrEmpty(appSettings.ReverbPreset) ? "大厅" : appSettings.ReverbPreset);
		SpatialWidth = appSettings.SpatialWidth;
		SpatialModulation = appSettings.SpatialModulation;
		SpatialPredelayMs = appSettings.SpatialPredelayMs;
		Speed = ((appSettings.PlaybackSpeed > 0.0) ? appSettings.PlaybackSpeed : 1.0);
		TimbreBass = appSettings.TimbreBass;
		TimbreMidrange = appSettings.TimbreMidrange;
		TimbreTreble = appSettings.TimbreTreble;
		TimbreThickness = appSettings.TimbreThickness;
		TimbreClarity = appSettings.TimbreClarity;
		TimbreSoundstage = appSettings.TimbreSoundstage;
		EqPresetName = "自定义";
		Vst3Folders = new ObservableCollection<string>((appSettings.Vst3ScanFolders != null && appSettings.Vst3ScanFolders.Count > 0) ? appSettings.Vst3ScanFolders : Vst3Manager.DefaultScanFolders.ToList());
		List<Vst3PluginState> initialPlugins = Vst3Manager.Merge(appSettings.Vst3Plugins ?? new List<Vst3PluginState>(), Enumerable.Empty<string>());
		Vst3Plugins = new ObservableCollection<Vst3PluginState>(initialPlugins);
		_lastSavedVst3Signature = Vst3SyncHub.BuildFullSignature(Vst3Plugins);
		Vst3SyncHub.PluginsSynced += OnExternalVst3Synced;
		ApplyVst3ToEngine();
		Vst3SyncHub.Publish(this, Vst3Plugins);

		// Load ECHO Audio DSP settings
		EchoHeadroomEnabled = appSettings.EchoHeadroomEnabled;
		EchoHeadroomDb = appSettings.EchoHeadroomDb;
		EchoDynamicLoudnessCompensation = appSettings.EchoDynamicLoudnessCompensation;
		EchoCompressorEnabled = appSettings.EchoCompressorEnabled;
		EchoCompressorThresholdDb = appSettings.EchoCompressorThresholdDb;
		EchoCompressorRatio = appSettings.EchoCompressorRatio;
		EchoCompressorAttackMs = appSettings.EchoCompressorAttackMs;
		EchoCompressorReleaseMs = appSettings.EchoCompressorReleaseMs;
		EchoCompressorKneeDb = appSettings.EchoCompressorKneeDb;
		EchoCompressorMakeupDb = appSettings.EchoCompressorMakeupDb;
		EchoCompressorMix = appSettings.EchoCompressorMix;
		EchoCompressorDetectorMode = appSettings.EchoCompressorDetectorMode ?? "Peak";
		EchoCompressorSidechainHighpassEnabled = appSettings.EchoCompressorSidechainHighpassEnabled;
		EchoCompressorSidechainHighpassHz = appSettings.EchoCompressorSidechainHighpassHz;
		EchoCompressorAutoRelease = appSettings.EchoCompressorAutoRelease;
		EchoCompressorRangeDb = appSettings.EchoCompressorRangeDb;
		EchoCompressorStereoLink = appSettings.EchoCompressorStereoLink;
		FurinaMultibandEnabled = appSettings.FurinaMultibandEnabled;
		FurinaMultibandLowCrossoverHz = appSettings.FurinaMultibandLowCrossoverHz;
		FurinaMultibandHighCrossoverHz = appSettings.FurinaMultibandHighCrossoverHz;
		FurinaMultibandMix = appSettings.FurinaMultibandMix;
		FurinaMultibandStereoLink = appSettings.FurinaMultibandStereoLink;

		FurinaMultibandLowEnabled = appSettings.FurinaMultibandLowEnabled;
		FurinaMultibandLowThresholdDb = appSettings.FurinaMultibandLowThresholdDb;
		FurinaMultibandLowRatio = appSettings.FurinaMultibandLowRatio;
		FurinaMultibandLowAttackMs = appSettings.FurinaMultibandLowAttackMs;
		FurinaMultibandLowReleaseMs = appSettings.FurinaMultibandLowReleaseMs;
		FurinaMultibandLowKneeDb = appSettings.FurinaMultibandLowKneeDb;
		FurinaMultibandLowMakeupDb = appSettings.FurinaMultibandLowMakeupDb;

		FurinaMultibandMidEnabled = appSettings.FurinaMultibandMidEnabled;
		FurinaMultibandMidThresholdDb = appSettings.FurinaMultibandMidThresholdDb;
		FurinaMultibandMidRatio = appSettings.FurinaMultibandMidRatio;
		FurinaMultibandMidAttackMs = appSettings.FurinaMultibandMidAttackMs;
		FurinaMultibandMidReleaseMs = appSettings.FurinaMultibandMidReleaseMs;
		FurinaMultibandMidKneeDb = appSettings.FurinaMultibandMidKneeDb;
		FurinaMultibandMidMakeupDb = appSettings.FurinaMultibandMidMakeupDb;

		FurinaMultibandHighEnabled = appSettings.FurinaMultibandHighEnabled;
		FurinaMultibandHighThresholdDb = appSettings.FurinaMultibandHighThresholdDb;
		FurinaMultibandHighRatio = appSettings.FurinaMultibandHighRatio;
		FurinaMultibandHighAttackMs = appSettings.FurinaMultibandHighAttackMs;
		FurinaMultibandHighReleaseMs = appSettings.FurinaMultibandHighReleaseMs;
		FurinaMultibandHighKneeDb = appSettings.FurinaMultibandHighKneeDb;
		FurinaMultibandHighMakeupDb = appSettings.FurinaMultibandHighMakeupDb;
		EchoCrossfeedEnabled = appSettings.EchoCrossfeedEnabled;
		EchoCrossfeedAmount = appSettings.EchoCrossfeedAmount;
		EchoCrossfeedCutoffHz = appSettings.EchoCrossfeedCutoffHz;
		EchoStereoFieldEnabled = appSettings.EchoStereoFieldEnabled;
		EchoStereoWidth = appSettings.EchoStereoWidth;
		EchoStereoCenterGainDb = appSettings.EchoStereoCenterGainDb;
		EchoStereoSideGainDb = appSettings.EchoStereoSideGainDb;
		EchoChannelMatrixPreset = appSettings.EchoChannelMatrixPreset;
		FurinaReplayGainEnabled = appSettings.FurinaReplayGainEnabled;
		FurinaReplayGainTargetLufs = appSettings.FurinaReplayGainTargetLufs;
		FurinaReplayGainMode = appSettings.FurinaReplayGainMode;
		FurinaReplayGainPreampDb = appSettings.FurinaReplayGainPreampDb;
		FurinaReplayGainUnknownDb = appSettings.FurinaReplayGainUnknownDb;
		FurinaReplayGainPreventClipping = appSettings.FurinaReplayGainPreventClipping;
		FurinaReplayGainTruePeak = appSettings.FurinaReplayGainTruePeak;
		FurinaReplayGainAnalyzeOnScan = appSettings.FurinaReplayGainAnalyzeOnScan;
		EchoConvolutionEnabled = appSettings.EchoConvolutionEnabled;
		EchoConvolutionFilePath = appSettings.EchoConvolutionFilePath;
		EchoConvolutionTrimDb = appSettings.EchoConvolutionTrimDb;
		EchoResamplerEnabled = appSettings.EchoResamplerEnabled;
		EchoResamplerTargetRate = appSettings.EchoResamplerTargetRate;
		FurinaSrcMode = appSettings.FurinaSrcMode ?? "off";
		FurinaSrcQuality = appSettings.FurinaSrcQuality ?? "transparent";
		FurinaSrcFilter1x = appSettings.FurinaSrcFilter1x ?? "furina-gauss-long";
		FurinaSrcFilterNx = appSettings.FurinaSrcFilterNx ?? "furina-hb";
		FurinaSrcCompute = appSettings.FurinaSrcCompute ?? "simd";
		FurinaSrcDither = appSettings.FurinaSrcDither ?? "off";
		FurinaSrcAdvanced = appSettings.FurinaSrcAdvanced;
		FurinaSrcAbBenchmark = appSettings.FurinaSrcAbBenchmark;
		EchoSdmEnabled = appSettings.EchoSdmEnabled;
		EchoSdmOrder = appSettings.EchoSdmOrder;
		EchoSdmRate = appSettings.EchoSdmRate;

		// Load FurinaPlayer Channel Tool Settings
		FurinaChannelToolEnabled = appSettings.FurinaChannelToolEnabled;
		FurinaChannelBalance = appSettings.FurinaChannelBalance;
		FurinaChannelLeftGainDb = appSettings.FurinaChannelLeftGainDb;
		FurinaChannelRightGainDb = appSettings.FurinaChannelRightGainDb;
		FurinaChannelTrimStepDb = appSettings.FurinaChannelTrimStepDb > 0 ? appSettings.FurinaChannelTrimStepDb : 0.25;
		FurinaChannelConstantPower = appSettings.FurinaChannelConstantPower;
		FurinaChannelLeftDelayMs = appSettings.FurinaChannelLeftDelayMs;
		FurinaChannelRightDelayMs = appSettings.FurinaChannelRightDelayMs;
		FurinaChannelLowLeftGainDb = appSettings.FurinaChannelLowLeftGainDb;
		FurinaChannelLowRightGainDb = appSettings.FurinaChannelLowRightGainDb;
		FurinaChannelMidLeftGainDb = appSettings.FurinaChannelMidLeftGainDb;
		FurinaChannelMidRightGainDb = appSettings.FurinaChannelMidRightGainDb;
		FurinaChannelHighLeftGainDb = appSettings.FurinaChannelHighLeftGainDb;
		FurinaChannelHighRightGainDb = appSettings.FurinaChannelHighRightGainDb;
		FurinaChannelMonoMode = appSettings.FurinaChannelMonoMode ?? "off";
		FurinaChannelSwapLeftRight = appSettings.FurinaChannelSwapLeftRight;
		FurinaChannelInvertLeft = appSettings.FurinaChannelInvertLeft;
		FurinaChannelInvertRight = appSettings.FurinaChannelInvertRight;
		FurinaChannelProMode = appSettings.FurinaChannelProMode ?? "simple";
		LoadChannelPresets(appSettings.FurinaChannelPresetsJson);
		ApplyChannelToolSettings(false);

		InitializeEchoEqBands();
	}

	public void SavePanelLayouts(Dictionary<string, double[]> layouts)
	{
		_settingsService.Update((AppSettings s) =>
		{
			s.PanelLayouts = layouts;
		});
	}

	public Dictionary<string, double[]> LoadPanelLayouts()
	{
		return _settingsService.Load().PanelLayouts ?? new Dictionary<string, double[]>();
	}

	[RelayCommand]
	private void AddVst3Folder(string? folder)
	{
		if (!string.IsNullOrWhiteSpace(folder) && Directory.Exists(folder) && !Vst3Folders.Any((string f) => string.Equals(f, folder, StringComparison.OrdinalIgnoreCase)))
		{
			Vst3Folders.Add(folder);
			SaveVst3State();
			Vst3StatusText = "已添加目录：" + folder + "，点击“扫描”发现插件";
		}
	}

	[RelayCommand]
	private void RemoveVst3Folder(string? folder)
	{
		if (!string.IsNullOrWhiteSpace(folder))
		{
			string text = Vst3Folders.FirstOrDefault((string f) => string.Equals(f, folder, StringComparison.OrdinalIgnoreCase));
			if (text != null)
			{
				Vst3Folders.Remove(text);
				SaveVst3State();
				Vst3StatusText = "已移除目录：" + folder;
			}
		}
	}

	[RelayCommand]
	private void ScanVst3()
	{
		List<string> list = new List<string>();
		foreach (string vst3Folder in Vst3Folders)
		{
			try
			{
				list.AddRange(Vst3Manager.ScanFolder(vst3Folder));
			}
			catch
			{
			}
		}
		List<Vst3PluginState> list2 = Vst3Manager.Merge(Vst3Plugins.ToList(), list);
		Vst3Plugins = new ObservableCollection<Vst3PluginState>(list2);
		ApplyVst3ToEngine();
		Vst3StatusText = ((list.Count > 0) ? $"扫描完成：发现 {list.Count} 个 VST3 插件" : "扫描完成：未在目录中发现 .vst3 插件（可添加自定义目录）");
		SaveVst3State();
		Vst3SyncHub.Publish(this, Vst3Plugins);
	}

	[RelayCommand]
	private void ApplyVst3()
	{
		ApplyVst3ToEngine();
		string currentSig = Vst3SyncHub.BuildFullSignature(Vst3Plugins);
		if (!string.Equals(_lastSavedVst3Signature, currentSig, StringComparison.Ordinal))
		{
			SaveVst3State();
		}
		Vst3SyncHub.Publish(this, Vst3Plugins);
	}

	public Vst3PluginState? AddOrSelectVst3PluginFile(string? filePath)
	{
		if (string.IsNullOrWhiteSpace(filePath) || !Vst3Manager.IsValidVst3Path(filePath))
		{
			Vst3StatusText = "无效的 VST3 插件路径：" + (filePath ?? string.Empty);
			return null;
		}
		Vst3PluginState created = Vst3Manager.CreateState(filePath);
		Vst3PluginState? existing = Vst3Plugins.FirstOrDefault((Vst3PluginState p) => p != null && string.Equals(p.Path, created.Path, StringComparison.OrdinalIgnoreCase));
		if (existing != null)
		{
			if (string.IsNullOrWhiteSpace(existing.Vendor) || existing.Vendor.Contains("未知"))
			{
				existing.Vendor = created.Vendor;
			}
			Vst3StatusText = $"已选中插件：{existing.Name} ({existing.Vendor})";
			return existing;
		}
		Vst3Plugins.Insert(0, created);
		SaveVst3State();
		Vst3SyncHub.Publish(this, Vst3Plugins);
		Vst3StatusText = $"已加载插件文件：{created.Name} ({created.Vendor})";
		return created;
	}

	public int RestoreDefaultVst3Folders()
	{
		int added = 0;
		foreach (string defFolder in Vst3Manager.DefaultScanFolders)
		{
			if (!Vst3Folders.Any((string f) => string.Equals(f, defFolder, StringComparison.OrdinalIgnoreCase)))
			{
				Vst3Folders.Add(defFolder);
				added++;
			}
		}
		if (added > 0)
		{
			SaveVst3State();
			Vst3StatusText = $"已恢复 {added} 个默认系统 VST3 扫描目录";
		}
		else
		{
			Vst3StatusText = "默认系统 VST3 扫描目录均已存在";
		}
		return added;
	}

	private void ApplyVst3ToEngine()
	{
		Vst3SyncHub.PushToEngine(_engine, Vst3Plugins.ToList());
	}

	private void OnExternalVst3Synced(object sender, IReadOnlyList<Vst3PluginState> plugins)
	{
		if (ReferenceEquals(sender, this))
		{
			return;
		}
		RunOnUiThread(() =>
		{
			string incomingSig = Vst3SyncHub.BuildFullSignature(plugins);
			string localSig = Vst3SyncHub.BuildFullSignature(Vst3Plugins);
			if (!string.Equals(localSig, incomingSig, StringComparison.Ordinal))
			{
				Vst3Plugins = new ObservableCollection<Vst3PluginState>(Vst3SyncHub.CloneList(plugins));
				_lastSavedVst3Signature = incomingSig;
			}
			Vst3PluginState? active = Vst3Plugins.FirstOrDefault((Vst3PluginState p) => p != null && p.IsActive);
			Vst3StatusText = active != null
				? $"已同步 VST3 启用状态：{active.Name}（实时处理中）"
				: "已同步 VST3 状态：当前未启用插件（直通）";
		});
	}

	private void SaveVst3State()
	{
		try
		{
			List<Vst3PluginState> snapshot = Vst3SyncHub.CloneList(Vst3Plugins);
			_lastSavedVst3Signature = Vst3SyncHub.BuildFullSignature(snapshot);
			_settingsService.Update((AppSettings s) =>
			{
				s.Vst3ScanFolders = Vst3Folders.ToList();
				s.Vst3Plugins = snapshot;
			});
		}
		catch
		{
		}
	}

	private void RunOnUiThread(Action action)
	{
		if (_uiDispatcher == null || _uiDispatcher.HasThreadAccess)
		{
			action();
			return;
		}
		_uiDispatcher.TryEnqueue(() =>
		{
			action();
		});
	}

	private void OnRenderingChanged(bool rendering)
	{
		RunOnUiThread(() =>
		{
			IsRendering = rendering;
			if (rendering)
			{
				RenderProgressValue = 0.0;
				RenderStatusText = "正在预渲染效果，完成后自动开始播放…";
			}
			else
			{
				RenderStatusText = ((RenderProgressValue >= 1.0) ? "预渲染完成，已开始播放" : "预渲染已取消");
			}
		});
	}

	private void OnRenderProgress(RenderProgress p)
	{
		RunOnUiThread(() =>
		{
			RenderProgressValue = p.Fraction;
			RenderStatusText = $"预渲染中… {p.Fraction:P0}，预计剩余 {Math.Max(0, (int)p.Remaining.TotalSeconds)} 秒";
		});
	}

	private void ApplyTimbre()
	{
		TimbreSettings timbre = new TimbreSettings
		{
			Bass = TimbreBass,
			Midrange = TimbreMidrange,
			Treble = TimbreTreble,
			Thickness = TimbreThickness,
			Clarity = TimbreClarity,
			Soundstage = TimbreSoundstage
		};
		_engine.SetTimbre(timbre);
		Save();
	}

	public void ApplyEqGains(double[] gains)
	{
		EqGains = (double[])gains.Clone();
		_engine.SetEqGains(EqGains);
		Save();
	}

	private void ApplySpatialParams()
	{
		_engine.SetSpatialAudio(ReverbEnabled, ReverbMix, ReverbPreset, SpatialWidth, SpatialModulation, SpatialPredelayMs);
		Save();
	}

	[RelayCommand]
	private void ApplyPreset(string presetName)
	{
		if (EqPresets.Presets.TryGetValue(presetName, out double[] value))
		{
			EqGains = (double[])value.Clone();
			EqPresetName = presetName;
			StatusText = "已应用预设：" + presetName;
		}
	}

	[RelayCommand]
	private void ResetAll()
	{
		EqGains = new double[10];
		ReverbEnabled = false;
		ReverbMix = 0.2;
		ReverbPreset = "大厅";
		SpatialWidth = 0.6;
		SpatialModulation = 0.25;
		SpatialPredelayMs = 25.0;
		Speed = 1.0;
		TimbreBass = 0.0;
		TimbreMidrange = 0.0;
		TimbreTreble = 0.0;
		TimbreThickness = 0.0;
		TimbreClarity = 0.0;
		TimbreSoundstage = 0.0;
		EqPresetName = "平坦";
		_engine.Speed = 1.0;
		_engine.SetSpatialAudio(enabled: false, ReverbMix, ReverbPreset, SpatialWidth, SpatialModulation, SpatialPredelayMs);
		_engine.SetEqGains(EqGains);
		_engine.SetTimbre(new TimbreSettings());
		foreach (Vst3PluginState vst3Plugin in Vst3Plugins)
		{
			vst3Plugin.Enabled = false;
			vst3Plugin.Bypass = false;
		}
		ApplyVst3ToEngine();
		Save();
		SaveVst3State();
		Vst3SyncHub.Publish(this, Vst3Plugins);
		StatusText = "已重置全部 DSP 效果";
	}

	public void ReloadDefaults()
	{
		EqGains = new double[10];
		ReverbEnabled = false;
		ReverbMix = 0.2;
		ReverbPreset = "大厅";
		SpatialWidth = 0.6;
		SpatialModulation = 0.25;
		SpatialPredelayMs = 25.0;
		Speed = 1.0;
		TimbreBass = 0.0;
		TimbreMidrange = 0.0;
		TimbreTreble = 0.0;
		TimbreThickness = 0.0;
		TimbreClarity = 0.0;
		TimbreSoundstage = 0.0;
		EqPresetName = "平坦";
		foreach (Vst3PluginState vst3Plugin in Vst3Plugins)
		{
			vst3Plugin.Enabled = false;
			vst3Plugin.Bypass = false;
		}
		ApplyVst3ToEngine();
		StatusText = "已恢复默认 DSP 设置";
		Save();
		SaveVst3State();
		Vst3SyncHub.Publish(this, Vst3Plugins);
	}

	private void Save()
	{
		try
		{
			SaveCore();
		}
		catch
		{
		}
	}

	private void SaveCore()
	{
		_settingsService.Update((AppSettings s) =>
		{
			s.EqGains = (double[])EqGains.Clone();
			s.ReverbEnabled = ReverbEnabled;
			s.ReverbMix = ReverbMix;
			s.ReverbPreset = ReverbPreset;
			s.SpatialWidth = SpatialWidth;
			s.SpatialModulation = SpatialModulation;
			s.SpatialPredelayMs = SpatialPredelayMs;
			s.PlaybackSpeed = Speed;
			s.TimbreBass = TimbreBass;
			s.TimbreMidrange = TimbreMidrange;
			s.TimbreTreble = TimbreTreble;
			s.TimbreThickness = TimbreThickness;
			s.TimbreClarity = TimbreClarity;
			s.TimbreSoundstage = TimbreSoundstage;
		});
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.2.0.0")]
	private void OnEqGainsChanged(double[] value)
	{
		_engine.SetEqGains(value);
		EqGainsChanged?.Invoke(value);
		Save();
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.2.0.0")]
	private void OnReverbEnabledChanged(bool value)
	{
		_engine.SetSpatialAudio(value, ReverbMix, ReverbPreset, SpatialWidth, SpatialModulation, SpatialPredelayMs);
		Save();
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.2.0.0")]
	private void OnReverbMixChanged(double value)
	{
		_engine.SetSpatialAudio(ReverbEnabled, value, ReverbPreset, SpatialWidth, SpatialModulation, SpatialPredelayMs);
		Save();
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.2.0.0")]
	private void OnReverbPresetChanged(string value)
	{
		_engine.SetSpatialAudio(ReverbEnabled, ReverbMix, value, SpatialWidth, SpatialModulation, SpatialPredelayMs);
		Save();
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.2.0.0")]
	private void OnSpatialWidthChanged(double value)
	{
		ApplySpatialParams();
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.2.0.0")]
	private void OnSpatialModulationChanged(double value)
	{
		ApplySpatialParams();
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.2.0.0")]
	private void OnSpatialPredelayMsChanged(double value)
	{
		ApplySpatialParams();
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.2.0.0")]
	private void OnSpeedChanged(double value)
	{
		_engine.Speed = value;
		Save();
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.2.0.0")]
	private void OnTimbreBassChanged(double value)
	{
		ApplyTimbre();
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.2.0.0")]
	private void OnTimbreMidrangeChanged(double value)
	{
		ApplyTimbre();
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.2.0.0")]
	private void OnTimbreTrebleChanged(double value)
	{
		ApplyTimbre();
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.2.0.0")]
	private void OnTimbreThicknessChanged(double value)
	{
		ApplyTimbre();
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.2.0.0")]
	private void OnTimbreClarityChanged(double value)
	{
		ApplyTimbre();
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.2.0.0")]
	private void OnTimbreSoundstageChanged(double value)
	{
		ApplyTimbre();
	}

	// ==========================================
	// ECHO Audio Core DSP Suite Controls
	// ==========================================
	public bool EchoEqEnabled { get; set; } = true;
	public double EchoEqPreampDb { get; set; } = 0.0;
	public List<EchoBiquadFilter> EchoEqBands { get; } = new();

	public void InitializeEchoEqBands()
	{
		if (EchoEqBands.Count == 0)
		{
			double[] freqs = { 31.25, 62.5, 125.0, 250.0, 500.0, 1000.0, 2000.0, 4000.0, 8000.0, 16000.0 };
			for (int i = 0; i < freqs.Length; i++)
			{
				var filter = new EchoBiquadFilter
				{
					FilterType = (i == 0) ? EchoFilterType.LowShelf : ((i == freqs.Length - 1) ? EchoFilterType.HighShelf : EchoFilterType.Peaking),
					FrequencyHz = freqs[i],
					GainDb = 0.0,
					Q = 1.414,
					Enabled = true
				};
				filter.UpdateCoefficients(48000.0);
				EchoEqBands.Add(filter);
			}
		}
	}

	public void ApplyParametricEqState(IReadOnlyList<EchoBiquadFilter> bands, double preampDb, bool enabled, bool save = true)
	{
		if (bands == null) return;

		double sampleRate = EngineSampleRate > 0 ? EngineSampleRate : 48000.0;
		EchoEqBands.Clear();
		for (int i = 0; i < bands.Count; i++)
		{
			EchoBiquadFilter src = bands[i];
			var clone = new EchoBiquadFilter
			{
				FilterType = src.FilterType,
				FrequencyHz = src.FrequencyHz,
				GainDb = src.GainDb,
				Q = src.Q,
				Enabled = src.Enabled
			};
			clone.UpdateCoefficients(sampleRate);
			EchoEqBands.Add(clone);
		}

		EchoEqPreampDb = Math.Clamp(preampDb, -24.0, 24.0);
		EchoEqEnabled = enabled;

		// Zero the legacy 10-band equalizer first so it doesn't double-process alongside EchoEqProcessor.
		// Note SetEqGains also writes zeros into DspChain.Eq.UserBands, so push EchoEqBands right after.
		_engine.SetEqGains(new double[10]);
		_engine.SetEchoEqEnabled(EchoEqEnabled);
		_engine.SetEchoEqPreamp(EchoEqPreampDb);

		for (int i = 0; i < EchoEqBands.Count; i++)
		{
			EchoBiquadFilter b = EchoEqBands[i];
			_engine.SetEchoEqBand(i, b.FilterType, b.FrequencyHz, b.GainDb, b.Q, b.Enabled);
		}

		int engineBandCount = _engine.DspChain?.Eq?.UserBands?.Count ?? 10;
		for (int i = EchoEqBands.Count; i < engineBandCount; i++)
		{
			_engine.SetEchoEqBand(i, EchoFilterType.Peaking, 1000.0, 0.0, 1.414, false);
		}

		UpdateLegacyEqGainsSummary(BuildSummaryEqGains(EchoEqBands));
		if (save)
		{
			SaveEchoSettings();
		}
	}

	public void SyncEchoEqBand(int index, EchoBiquadFilter filter, bool save = true)
	{
		if (filter == null || index < 0) return;
		double sampleRate = EngineSampleRate > 0 ? EngineSampleRate : 48000.0;
		var clone = new EchoBiquadFilter
		{
			FilterType = filter.FilterType,
			FrequencyHz = filter.FrequencyHz,
			GainDb = filter.GainDb,
			Q = filter.Q,
			Enabled = filter.Enabled
		};
		clone.UpdateCoefficients(sampleRate);

		while (EchoEqBands.Count <= index)
		{
			var pad = new EchoBiquadFilter
			{
				FilterType = EchoFilterType.Peaking,
				FrequencyHz = 1000.0,
				GainDb = 0.0,
				Q = 1.414,
				Enabled = false
			};
			pad.UpdateCoefficients(sampleRate);
			EchoEqBands.Add(pad);
		}
		EchoEqBands[index] = clone;

		_engine.SetEchoEqBand(index, clone.FilterType, clone.FrequencyHz, clone.GainDb, clone.Q, clone.Enabled);
		UpdateLegacyEqGainsSummary(BuildSummaryEqGains(EchoEqBands));
		if (save)
		{
			SaveEchoSettings();
		}
	}

	private static double[] BuildSummaryEqGains(IReadOnlyList<EchoBiquadFilter> bands)
	{
		double[] targetFrequencies = { 31.25, 62.5, 125.0, 250.0, 500.0, 1000.0, 2000.0, 4000.0, 8000.0, 16000.0 };
		double[] summary = new double[10];
		if (bands == null || bands.Count == 0)
		{
			return summary;
		}

		for (int i = 0; i < targetFrequencies.Length; i++)
		{
			double targetFreq = targetFrequencies[i];
			EchoBiquadFilter? best = null;
			double bestDist = double.MaxValue;
			for (int j = 0; j < bands.Count; j++)
			{
				EchoBiquadFilter candidate = bands[j];
				if (!candidate.Enabled || candidate.FrequencyHz <= 0.0) continue;
				double dist = Math.Abs(Math.Log10(candidate.FrequencyHz) - Math.Log10(targetFreq));
				if (dist < bestDist)
				{
					bestDist = dist;
					best = candidate;
				}
			}

			if (best != null && bestDist < 0.25)
			{
				summary[i] = Math.Clamp(best.GainDb, -12.0, 12.0);
			}
		}

		return summary;
	}

	private void UpdateLegacyEqGainsSummary(double[] summary)
	{
		if (EqualityComparer<double[]>.Default.Equals(eqGains, summary))
		{
			return;
		}
		OnPropertyChanging(__KnownINotifyPropertyChangingArgs.EqGains);
		eqGains = summary;
		EqGainsChanged?.Invoke(summary);
		OnPropertyChanged(__KnownINotifyPropertyChangedArgs.EqGains);
	}

	public bool EchoHeadroomEnabled { get; set; } = true;
	public double EchoHeadroomDb { get; set; } = -3.0;
	public bool EchoDynamicLoudnessCompensation { get; set; } = false;
	public bool EchoHeadroomDirectBypassActive { get; set; } = false;

	public void ApplyHeadroomSettings()
	{
		if (Engine != null)
		{
			bool effectiveEnabled = EchoHeadroomEnabled && !EchoHeadroomDirectBypassActive;
			double effectiveDb = EchoHeadroomDirectBypassActive ? 0.0 : EchoHeadroomDb;
			Engine.SetEchoHeadroom(effectiveEnabled, effectiveDb);
		}
		SaveEchoSettings();
	}
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

	public bool FurinaMultibandEnabled { get; set; } = false;
	public double FurinaMultibandLowCrossoverHz { get; set; } = 150.0;
	public double FurinaMultibandHighCrossoverHz { get; set; } = 2500.0;
	public double FurinaMultibandMix { get; set; } = 1.0;
	public double FurinaMultibandStereoLink { get; set; } = 1.0;

	public bool FurinaMultibandLowEnabled { get; set; } = true;
	public double FurinaMultibandLowThresholdDb { get; set; } = -18.0;
	public double FurinaMultibandLowRatio { get; set; } = 2.0;
	public double FurinaMultibandLowAttackMs { get; set; } = 10.0;
	public double FurinaMultibandLowReleaseMs { get; set; } = 120.0;
	public double FurinaMultibandLowKneeDb { get; set; } = 6.0;
	public double FurinaMultibandLowMakeupDb { get; set; } = 0.0;

	public bool FurinaMultibandMidEnabled { get; set; } = true;
	public double FurinaMultibandMidThresholdDb { get; set; } = -18.0;
	public double FurinaMultibandMidRatio { get; set; } = 2.0;
	public double FurinaMultibandMidAttackMs { get; set; } = 10.0;
	public double FurinaMultibandMidReleaseMs { get; set; } = 120.0;
	public double FurinaMultibandMidKneeDb { get; set; } = 6.0;
	public double FurinaMultibandMidMakeupDb { get; set; } = 0.0;

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

	public bool FurinaReplayGainEnabled { get; set; } = false;
	public double FurinaReplayGainTargetLufs { get; set; } = -14.0;
	public int FurinaReplayGainMode { get; set; } = 1; // 0: Auto, 1: Track, 2: Album
	public double FurinaReplayGainPreampDb { get; set; } = 0.0;
	public double FurinaReplayGainUnknownDb { get; set; } = -5.0;
	public bool FurinaReplayGainPreventClipping { get; set; } = true;
	public bool FurinaReplayGainTruePeak { get; set; } = true;
	public bool FurinaReplayGainAnalyzeOnScan { get; set; } = false;

	public double? FurinaReplayGainCurrentAppliedDb { get; set; } = null;
	public double? FurinaReplayGainIntegratedLufs { get; set; } = null;
	public double? FurinaReplayGainTrackGainDb { get; set; } = null;
	public double? FurinaReplayGainAlbumGainDb { get; set; } = null;
	public double? FurinaReplayGainPeak { get; set; } = null;
	public string FurinaReplayGainSource { get; set; } = "无数据";
	public string FurinaReplayGainEffectiveMode { get; set; } = "单曲";
	public bool FurinaReplayGainPreventedClipping { get; set; } = false;
	public bool FurinaReplayGainIsScanning { get; set; } = false;
	public double FurinaReplayGainScanProgress { get; set; } = 0.0;
	public string FurinaReplayGainScanText { get; set; } = "就绪";

	// 向前兼容属性映射
	public bool EchoReplayGainEnabled { get => FurinaReplayGainEnabled; set => FurinaReplayGainEnabled = value; }
	public double EchoReplayGainTargetLufs { get => FurinaReplayGainTargetLufs; set => FurinaReplayGainTargetLufs = value; }
	public int EchoReplayGainMode { get => FurinaReplayGainMode; set => FurinaReplayGainMode = value; }
	public double EchoReplayGainPreampDb { get => FurinaReplayGainPreampDb; set => FurinaReplayGainPreampDb = value; }
	public double EchoReplayGainUnknownDb { get => FurinaReplayGainUnknownDb; set => FurinaReplayGainUnknownDb = value; }
	public bool EchoReplayGainPreventClipping { get => FurinaReplayGainPreventClipping; set => FurinaReplayGainPreventClipping = value; }
	public bool EchoReplayGainTruePeak { get => FurinaReplayGainTruePeak; set => FurinaReplayGainTruePeak = value; }
	public bool EchoReplayGainAnalyzeOnScan { get => FurinaReplayGainAnalyzeOnScan; set => FurinaReplayGainAnalyzeOnScan = value; }
	public double? EchoReplayGainCurrentAppliedDb { get => FurinaReplayGainCurrentAppliedDb; set => FurinaReplayGainCurrentAppliedDb = value; }
	public double? EchoReplayGainIntegratedLufs { get => FurinaReplayGainIntegratedLufs; set => FurinaReplayGainIntegratedLufs = value; }
	public bool EchoReplayGainIsScanning { get => FurinaReplayGainIsScanning; set => FurinaReplayGainIsScanning = value; }
	public double EchoReplayGainScanProgress { get => FurinaReplayGainScanProgress; set => FurinaReplayGainScanProgress = value; }
	public string EchoReplayGainScanText { get => FurinaReplayGainScanText; set => FurinaReplayGainScanText = value; }

	public void ApplyReplayGainSettings()
	{
		var mode = !FurinaReplayGainEnabled ? ReplayGainMode.Off : (FurinaReplayGainMode switch
		{
			0 => ReplayGainMode.Auto,
			1 => ReplayGainMode.Track,
			2 => ReplayGainMode.Album,
			_ => ReplayGainMode.Track
		});

		if (Engine != null)
		{
			Engine.SetFurinaReplayGain(mode, (float)FurinaReplayGainTargetLufs, (float)FurinaReplayGainPreampDb, (float)FurinaReplayGainUnknownDb, FurinaReplayGainPreventClipping);
		}

		SaveEchoSettings();
	}

	public bool EchoConvolutionEnabled { get; set; } = false;
	public string? EchoConvolutionFilePath { get; set; }
	public double EchoConvolutionTrimDb { get; set; } = 0.0;

	public bool EchoResamplerEnabled { get; set; } = false;
	public bool EchoSrcUpsamplingEnabled { get => EchoResamplerEnabled; set => EchoResamplerEnabled = value; }
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

	public void ApplyEchoDsp()
	{
		_engine.SetEchoHeadroom(EchoHeadroomEnabled, EchoHeadroomDb);
		var compDetMode = string.Equals(EchoCompressorDetectorMode, "RMS", StringComparison.OrdinalIgnoreCase)
			? CompressorDetectorMode.Rms : CompressorDetectorMode.Peak;
		_engine.SetCompressor(
			EchoCompressorEnabled,
			(float)EchoCompressorThresholdDb,
			(float)EchoCompressorRatio,
			(float)EchoCompressorAttackMs,
			(float)EchoCompressorReleaseMs,
			(float)EchoCompressorKneeDb,
			(float)EchoCompressorMakeupDb,
			(float)EchoCompressorMix,
			compDetMode,
			EchoCompressorSidechainHighpassEnabled,
			(float)EchoCompressorSidechainHighpassHz,
			EchoCompressorAutoRelease,
			(float)EchoCompressorRangeDb,
			(float)EchoCompressorStereoLink);
		_engine.SetFurinaMultiband(FurinaMultibandEnabled, (float)FurinaMultibandLowCrossoverHz, (float)FurinaMultibandHighCrossoverHz, (float)FurinaMultibandMix, (float)FurinaMultibandStereoLink);
		_engine.SetFurinaMultibandBand(0, FurinaMultibandLowEnabled, (float)FurinaMultibandLowThresholdDb, (float)FurinaMultibandLowRatio, (float)FurinaMultibandLowAttackMs, (float)FurinaMultibandLowReleaseMs, (float)FurinaMultibandLowKneeDb, (float)FurinaMultibandLowMakeupDb);
		_engine.SetFurinaMultibandBand(1, FurinaMultibandMidEnabled, (float)FurinaMultibandMidThresholdDb, (float)FurinaMultibandMidRatio, (float)FurinaMultibandMidAttackMs, (float)FurinaMultibandMidReleaseMs, (float)FurinaMultibandMidKneeDb, (float)FurinaMultibandMidMakeupDb);
		_engine.SetFurinaMultibandBand(2, FurinaMultibandHighEnabled, (float)FurinaMultibandHighThresholdDb, (float)FurinaMultibandHighRatio, (float)FurinaMultibandHighAttackMs, (float)FurinaMultibandHighReleaseMs, (float)FurinaMultibandHighKneeDb, (float)FurinaMultibandHighMakeupDb);
		_engine.SetEchoCrossfeed(EchoCrossfeedEnabled, (float)EchoCrossfeedAmount, (float)EchoCrossfeedCutoffHz);
		_engine.SetEchoStereoField(EchoStereoFieldEnabled, (float)EchoStereoWidth, (float)EchoStereoCenterGainDb, (float)EchoStereoSideGainDb);
		_engine.SetEchoChannelMatrix((SonicWave.Audio.Dsp.ChannelMatrixPreset)EchoChannelMatrixPreset);
		_engine.SetEchoReplayGain((SonicWave.Audio.Dsp.ReplayGainMode)EchoReplayGainMode, (float)EchoReplayGainPreampDb, EchoReplayGainPreventClipping);
		if (EchoConvolutionEnabled && !string.IsNullOrEmpty(EchoConvolutionFilePath))
		{
			_engine.LoadEchoConvolution(EchoConvolutionFilePath, (float)EchoConvolutionTrimDb);
		}
		SaveEchoSettings();
	}

	public void SaveEchoSettings()
	{
		_settingsService.Update((AppSettings s) =>
		{
			s.EchoHeadroomEnabled = EchoHeadroomEnabled;
			s.EchoHeadroomDb = EchoHeadroomDb;
			s.EchoDynamicLoudnessCompensation = EchoDynamicLoudnessCompensation;
			s.EchoCompressorEnabled = EchoCompressorEnabled;
			s.EchoCompressorThresholdDb = EchoCompressorThresholdDb;
			s.EchoCompressorRatio = EchoCompressorRatio;
			s.EchoCompressorAttackMs = EchoCompressorAttackMs;
			s.EchoCompressorReleaseMs = EchoCompressorReleaseMs;
			s.EchoCompressorKneeDb = EchoCompressorKneeDb;
			s.EchoCompressorMakeupDb = EchoCompressorMakeupDb;
			s.EchoCompressorMix = EchoCompressorMix;
			s.EchoCompressorDetectorMode = EchoCompressorDetectorMode;
			s.EchoCompressorSidechainHighpassEnabled = EchoCompressorSidechainHighpassEnabled;
			s.EchoCompressorSidechainHighpassHz = EchoCompressorSidechainHighpassHz;
			s.EchoCompressorAutoRelease = EchoCompressorAutoRelease;
			s.EchoCompressorRangeDb = EchoCompressorRangeDb;
			s.EchoCompressorStereoLink = EchoCompressorStereoLink;
			s.FurinaMultibandEnabled = FurinaMultibandEnabled;
			s.FurinaMultibandLowCrossoverHz = FurinaMultibandLowCrossoverHz;
			s.FurinaMultibandHighCrossoverHz = FurinaMultibandHighCrossoverHz;
			s.FurinaMultibandMix = FurinaMultibandMix;
			s.FurinaMultibandStereoLink = FurinaMultibandStereoLink;

			s.FurinaMultibandLowEnabled = FurinaMultibandLowEnabled;
			s.FurinaMultibandLowThresholdDb = FurinaMultibandLowThresholdDb;
			s.FurinaMultibandLowRatio = FurinaMultibandLowRatio;
			s.FurinaMultibandLowAttackMs = FurinaMultibandLowAttackMs;
			s.FurinaMultibandLowReleaseMs = FurinaMultibandLowReleaseMs;
			s.FurinaMultibandLowKneeDb = FurinaMultibandLowKneeDb;
			s.FurinaMultibandLowMakeupDb = FurinaMultibandLowMakeupDb;

			s.FurinaMultibandMidEnabled = FurinaMultibandMidEnabled;
			s.FurinaMultibandMidThresholdDb = FurinaMultibandMidThresholdDb;
			s.FurinaMultibandMidRatio = FurinaMultibandMidRatio;
			s.FurinaMultibandMidAttackMs = FurinaMultibandMidAttackMs;
			s.FurinaMultibandMidReleaseMs = FurinaMultibandMidReleaseMs;
			s.FurinaMultibandMidKneeDb = FurinaMultibandMidKneeDb;
			s.FurinaMultibandMidMakeupDb = FurinaMultibandMidMakeupDb;

			s.FurinaMultibandHighEnabled = FurinaMultibandHighEnabled;
			s.FurinaMultibandHighThresholdDb = FurinaMultibandHighThresholdDb;
			s.FurinaMultibandHighRatio = FurinaMultibandHighRatio;
			s.FurinaMultibandHighAttackMs = FurinaMultibandHighAttackMs;
			s.FurinaMultibandHighReleaseMs = FurinaMultibandHighReleaseMs;
			s.FurinaMultibandHighKneeDb = FurinaMultibandHighKneeDb;
			s.FurinaMultibandHighMakeupDb = FurinaMultibandHighMakeupDb;
			s.EchoCrossfeedEnabled = EchoCrossfeedEnabled;
			s.EchoCrossfeedAmount = EchoCrossfeedAmount;
			s.EchoCrossfeedCutoffHz = EchoCrossfeedCutoffHz;
			s.EchoStereoFieldEnabled = EchoStereoFieldEnabled;
			s.EchoStereoWidth = EchoStereoWidth;
			s.EchoStereoCenterGainDb = EchoStereoCenterGainDb;
			s.EchoStereoSideGainDb = EchoStereoSideGainDb;
			s.EchoChannelMatrixPreset = EchoChannelMatrixPreset;
			s.FurinaReplayGainEnabled = FurinaReplayGainEnabled;
			s.FurinaReplayGainTargetLufs = FurinaReplayGainTargetLufs;
			s.FurinaReplayGainMode = FurinaReplayGainMode;
			s.FurinaReplayGainPreampDb = FurinaReplayGainPreampDb;
			s.FurinaReplayGainUnknownDb = FurinaReplayGainUnknownDb;
			s.FurinaReplayGainPreventClipping = FurinaReplayGainPreventClipping;
			s.FurinaReplayGainTruePeak = FurinaReplayGainTruePeak;
			s.FurinaReplayGainAnalyzeOnScan = FurinaReplayGainAnalyzeOnScan;
			s.EchoConvolutionEnabled = EchoConvolutionEnabled;
			s.EchoConvolutionFilePath = EchoConvolutionFilePath;
			s.EchoConvolutionTrimDb = EchoConvolutionTrimDb;
			s.EchoResamplerEnabled = EchoResamplerEnabled;
			s.EchoResamplerTargetRate = EchoResamplerTargetRate;
			s.FurinaSrcMode = FurinaSrcMode;
			s.FurinaSrcQuality = FurinaSrcQuality;
			s.FurinaSrcFilter1x = FurinaSrcFilter1x;
			s.FurinaSrcFilterNx = FurinaSrcFilterNx;
			s.FurinaSrcCompute = FurinaSrcCompute;
			s.FurinaSrcDither = FurinaSrcDither;
			s.FurinaSrcAdvanced = FurinaSrcAdvanced;
			s.FurinaSrcAbBenchmark = FurinaSrcAbBenchmark;
			s.EchoSdmEnabled = EchoSdmEnabled;
			s.EchoSdmOrder = EchoSdmOrder;
			s.EchoSdmRate = EchoSdmRate;

			// Save FurinaPlayer Channel Tool Settings
			s.FurinaChannelToolEnabled = FurinaChannelToolEnabled;
			s.FurinaChannelBalance = FurinaChannelBalance;
			s.FurinaChannelLeftGainDb = FurinaChannelLeftGainDb;
			s.FurinaChannelRightGainDb = FurinaChannelRightGainDb;
			s.FurinaChannelTrimStepDb = FurinaChannelTrimStepDb;
			s.FurinaChannelConstantPower = FurinaChannelConstantPower;
			s.FurinaChannelLeftDelayMs = FurinaChannelLeftDelayMs;
			s.FurinaChannelRightDelayMs = FurinaChannelRightDelayMs;
			s.FurinaChannelLowLeftGainDb = FurinaChannelLowLeftGainDb;
			s.FurinaChannelLowRightGainDb = FurinaChannelLowRightGainDb;
			s.FurinaChannelMidLeftGainDb = FurinaChannelMidLeftGainDb;
			s.FurinaChannelMidRightGainDb = FurinaChannelMidRightGainDb;
			s.FurinaChannelHighLeftGainDb = FurinaChannelHighLeftGainDb;
			s.FurinaChannelHighRightGainDb = FurinaChannelHighRightGainDb;
			s.FurinaChannelMonoMode = FurinaChannelMonoMode;
			s.FurinaChannelSwapLeftRight = FurinaChannelSwapLeftRight;
			s.FurinaChannelInvertLeft = FurinaChannelInvertLeft;
			s.FurinaChannelInvertRight = FurinaChannelInvertRight;
			s.FurinaChannelProMode = FurinaChannelProMode;
			try
			{
				s.FurinaChannelPresetsJson = JsonSerializer.Serialize(FurinaChannelPresets);
			}
			catch { }
		});
	}

	public bool ImportAutoEqPreset(string text)
	{
		try
		{
			var preset = SonicWave.Audio.Dsp.AutoEqParser.Parse(text);
			if (preset.Bands.Count > 0)
			{
				var chain = _engine.DspChain;
				if (chain != null)
				{
					SonicWave.Audio.Dsp.AutoEqParser.ApplyTo(preset, chain.Eq);
				}
				StatusText = $"已导入 AutoEq 预设：{preset.Bands.Count} 段滤波，前级增益 {preset.PreampDb:F1} dB";
				return true;
			}
		}
		catch (Exception ex)
		{
			StatusText = "AutoEq 预设导入失败：" + ex.Message;
		}
		return false;
	}

	public bool LoadConvolutionIr(string filePath)
	{
		try
		{
			EchoConvolutionFilePath = filePath;
			EchoConvolutionEnabled = true;
			bool ok = _engine.LoadEchoConvolution(filePath, (float)EchoConvolutionTrimDb);
			if (ok)
			{
				StatusText = "已加载房间校正/耳机脉冲响应：" + Path.GetFileName(filePath);
				SaveEchoSettings();
				OnPropertyChanged(nameof(EchoConvolutionFilePath));
				OnPropertyChanged(nameof(EchoConvolutionEnabled));
				return true;
			}
			StatusText = "脉冲响应加载失败，请检查文件格式";
		}
		catch (Exception ex)
		{
			StatusText = "加载脉冲响应出错: " + ex.Message;
		}
		return false;
	}

	public void ClearConvolutionIr()
	{
		try
		{
			EchoConvolutionFilePath = null;
			EchoConvolutionEnabled = false;
			_engine.ClearEchoConvolution();
			StatusText = "已清除脉冲响应";
			SaveEchoSettings();
			OnPropertyChanged(nameof(EchoConvolutionFilePath));
			OnPropertyChanged(nameof(EchoConvolutionEnabled));
		}
		catch (Exception ex)
		{
			StatusText = "清除脉冲响应出错: " + ex.Message;
		}
	}

	public void ApplyConvolutionSettings()
	{
		try
		{
			if (_engine.DspChain?.Convolution != null)
			{
				_engine.DspChain.Convolution.Enabled = EchoConvolutionEnabled;
				_engine.DspChain.Convolution.TrimDb = (float)EchoConvolutionTrimDb;
			}
			SaveEchoSettings();
			OnPropertyChanged(nameof(EchoConvolutionEnabled));
			OnPropertyChanged(nameof(EchoConvolutionTrimDb));
		}
		catch (Exception ex)
		{
			System.Diagnostics.Debug.WriteLine("ApplyConvolutionSettings error: " + ex.Message);
		}
	}

	public bool ImportAutoEqText(string text)
	{
		try
		{
			var preset = SonicWave.Audio.Dsp.AutoEqParser.Parse(text);
			if (preset.Bands.Count > 0)
			{
				ApplyParametricEqState(preset.Bands, preset.PreampDb, enabled: true, save: true);
				EqPresetName = "AutoEq 校准";
				StatusText = $"已导入 AutoEq 耳机预设 ({preset.Bands.Count} 个频点，前级 {preset.PreampDb:F1} dB)";
				return true;
			}
		}
		catch (Exception ex)
		{
			StatusText = "导入 AutoEq 出错: " + ex.Message;
		}
		return false;
	}

	#region FurinaPlayer Channel Tool State & Presets

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
	public bool FurinaChannelCompareBypassed { get; set; } = false;

	public ObservableCollection<ChannelToolPreset> FurinaChannelPresets { get; } = new();

	public void ApplyChannelToolSettings(bool save = true)
	{
		var monoMode = FurinaChannelMonoMode switch
		{
			"sum" => ChannelMonoMode.Sum,
			"left" => ChannelMonoMode.LeftOnly,
			"right" => ChannelMonoMode.RightOnly,
			_ => ChannelMonoMode.Off
		};

		_engine.SetFurinaChannelTool(
			FurinaChannelToolEnabled,
			FurinaChannelBalance,
			FurinaChannelLeftGainDb,
			FurinaChannelRightGainDb,
			FurinaChannelConstantPower,
			FurinaChannelLeftDelayMs,
			FurinaChannelRightDelayMs,
			FurinaChannelLowLeftGainDb,
			FurinaChannelLowRightGainDb,
			FurinaChannelMidLeftGainDb,
			FurinaChannelMidRightGainDb,
			FurinaChannelHighLeftGainDb,
			FurinaChannelHighRightGainDb,
			monoMode,
			FurinaChannelSwapLeftRight,
			FurinaChannelInvertLeft,
			FurinaChannelInvertRight,
			FurinaChannelCompareBypassed);

		if (save)
		{
			SaveEchoSettings();
		}
	}

	public void LoadChannelPresets(string? json)
	{
		FurinaChannelPresets.Clear();
		if (!string.IsNullOrWhiteSpace(json))
		{
			try
			{
				var list = JsonSerializer.Deserialize<List<ChannelToolPreset>>(json);
				if (list != null)
				{
					foreach (var p in list)
					{
						FurinaChannelPresets.Add(p);
					}
				}
			}
			catch { }
		}
	}

	public ChannelToolPreset SaveChannelPreset(string name)
	{
		var preset = new ChannelToolPreset
		{
			Id = Guid.NewGuid().ToString("N"),
			Name = string.IsNullOrWhiteSpace(name) ? "耳机偏音补偿" : name.Trim(),
			Balance = FurinaChannelBalance,
			LeftGainDb = FurinaChannelLeftGainDb,
			RightGainDb = FurinaChannelRightGainDb,
			TrimStepDb = FurinaChannelTrimStepDb,
			ConstantPower = FurinaChannelConstantPower,
			LeftDelayMs = FurinaChannelLeftDelayMs,
			RightDelayMs = FurinaChannelRightDelayMs,
			LowLeftGainDb = FurinaChannelLowLeftGainDb,
			LowRightGainDb = FurinaChannelLowRightGainDb,
			MidLeftGainDb = FurinaChannelMidLeftGainDb,
			MidRightGainDb = FurinaChannelMidRightGainDb,
			HighLeftGainDb = FurinaChannelHighLeftGainDb,
			HighRightGainDb = FurinaChannelHighRightGainDb,
			MonoMode = FurinaChannelMonoMode,
			SwapLeftRight = FurinaChannelSwapLeftRight,
			InvertLeft = FurinaChannelInvertLeft,
			InvertRight = FurinaChannelInvertRight,
			CreatedAt = DateTime.Now
		};

		FurinaChannelPresets.Insert(0, preset);
		SaveEchoSettings();
		return preset;
	}

	public void ApplyChannelPreset(ChannelToolPreset preset)
	{
		if (preset == null) return;
		FurinaChannelBalance = preset.Balance;
		FurinaChannelLeftGainDb = preset.LeftGainDb;
		FurinaChannelRightGainDb = preset.RightGainDb;
		FurinaChannelTrimStepDb = preset.TrimStepDb;
		FurinaChannelConstantPower = preset.ConstantPower;
		FurinaChannelLeftDelayMs = preset.LeftDelayMs;
		FurinaChannelRightDelayMs = preset.RightDelayMs;
		FurinaChannelLowLeftGainDb = preset.LowLeftGainDb;
		FurinaChannelLowRightGainDb = preset.LowRightGainDb;
		FurinaChannelMidLeftGainDb = preset.MidLeftGainDb;
		FurinaChannelMidRightGainDb = preset.MidRightGainDb;
		FurinaChannelHighLeftGainDb = preset.HighLeftGainDb;
		FurinaChannelHighRightGainDb = preset.HighRightGainDb;
		FurinaChannelMonoMode = preset.MonoMode;
		FurinaChannelSwapLeftRight = preset.SwapLeftRight;
		FurinaChannelInvertLeft = preset.InvertLeft;
		FurinaChannelInvertRight = preset.InvertRight;
		FurinaChannelToolEnabled = true;
		FurinaChannelCompareBypassed = false;
		ApplyChannelToolSettings(true);
	}

	public void DeleteChannelPreset(string id)
	{
		var found = FurinaChannelPresets.FirstOrDefault(p => p.Id == id);
		if (found != null)
		{
			FurinaChannelPresets.Remove(found);
			SaveEchoSettings();
		}
	}

	public void RenameChannelPreset(string id, string newName)
	{
		var found = FurinaChannelPresets.FirstOrDefault(p => p.Id == id);
		if (found != null && !string.IsNullOrWhiteSpace(newName))
		{
			found.Name = newName.Trim();
			SaveEchoSettings();
		}
	}

	#endregion
}

internal static class Vst3SyncHub
{
	private static readonly object s_gate = new object();
	private static List<Vst3PluginState>? s_latestPlugins;
	private static string s_lastEngineSignature = string.Empty;
	private static bool s_hasEngineSynced;

	public static event Action<object, IReadOnlyList<Vst3PluginState>>? PluginsSynced;

	public static Vst3PluginState ClonePlugin(Vst3PluginState p)
	{
		return new Vst3PluginState
		{
			Path = p.Path ?? string.Empty,
			Name = p.Name ?? string.Empty,
			Vendor = p.Vendor ?? "未知（模拟宿主）",
			Version = p.Version ?? "1.0.0",
			Enabled = p.Enabled,
			Bypass = p.Bypass,
			Drive = p.Drive,
			Tone = p.Tone,
			Mix = p.Mix
		};
	}

	public static List<Vst3PluginState> CloneList(IEnumerable<Vst3PluginState>? plugins)
	{
		if (plugins == null)
		{
			return new List<Vst3PluginState>();
		}
		var result = new List<Vst3PluginState>();
		foreach (Vst3PluginState p in plugins)
		{
			if (p != null)
			{
				result.Add(ClonePlugin(p));
			}
		}
		return result;
	}

	public static string BuildActiveSignature(IEnumerable<Vst3PluginState>? plugins)
	{
		if (plugins == null)
		{
			return string.Empty;
		}
		return string.Join(";", plugins
			.Where(p => p != null && p.IsActive && !string.IsNullOrWhiteSpace(p.Path))
			.Select(p => $"{p.Path.Trim().ToLowerInvariant()}|{p.Drive:R}|{p.Tone:R}|{p.Mix:R}"));
	}

	public static string BuildFullSignature(IEnumerable<Vst3PluginState>? plugins)
	{
		if (plugins == null)
		{
			return string.Empty;
		}
		return string.Join(";", plugins
			.Where(p => p != null && !string.IsNullOrWhiteSpace(p.Path))
			.Select(p => $"{p.Path.Trim().ToLowerInvariant()}|{(p.Enabled ? 1 : 0)}|{(p.Bypass ? 1 : 0)}|{p.Drive:R}|{p.Tone:R}|{p.Mix:R}"));
	}

	public static bool TryGetLatest(out List<Vst3PluginState> plugins)
	{
		lock (s_gate)
		{
			if (s_latestPlugins != null)
			{
				plugins = CloneList(s_latestPlugins);
				return true;
			}
		}
		plugins = new List<Vst3PluginState>();
		return false;
	}

	public static void Publish(object sender, IEnumerable<Vst3PluginState>? plugins)
	{
		List<Vst3PluginState> snapshot = CloneList(plugins);
		string nextFullSig = BuildFullSignature(snapshot);
		lock (s_gate)
		{
			if (s_latestPlugins != null && string.Equals(BuildFullSignature(s_latestPlugins), nextFullSig, StringComparison.Ordinal))
			{
				return;
			}
			s_latestPlugins = snapshot;
		}
		PluginsSynced?.Invoke(sender, CloneList(snapshot));
	}

	public static void PushToEngine(AudioEngine? engine, IEnumerable<Vst3PluginState>? plugins, bool force = false)
	{
		if (engine == null)
		{
			return;
		}
		List<Vst3PluginState> snapshot = CloneList(plugins);
		string sig = BuildActiveSignature(snapshot);
		Vst3PluginState? firstActive = snapshot.FirstOrDefault(p => p.IsActive);
		bool hostMatches = firstActive != null
			? Vst3NativeHost.Instance.IsLoadedFor(firstActive.Path)
			: !Vst3NativeHost.Instance.IsLoaded;
		lock (s_gate)
		{
			if (!force && s_hasEngineSynced && string.Equals(s_lastEngineSignature, sig, StringComparison.Ordinal) && hostMatches)
			{
				return;
			}
			s_lastEngineSignature = sig;
			s_hasEngineSynced = true;
		}
		bool wasPaused = engine.State == PlaybackState.Paused;
		engine.SetVst3(snapshot);
		if (wasPaused && engine.State == PlaybackState.Playing)
		{
			engine.Pause();
		}
	}
}
