using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
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

	private readonly SettingsService _settingsService;

	private readonly DispatcherQueue? _uiDispatcher;

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
	private string vst3StatusText = "扫描 VST3 目录后启用插件，效果经预渲染链路生效（模拟宿主）";

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
		Vst3Plugins = new ObservableCollection<Vst3PluginState>(appSettings.Vst3Plugins ?? new List<Vst3PluginState>());
		_engine.SetVst3(Vst3Plugins.ToList());

		// Load ECHO Audio DSP settings
		EchoHeadroomDb = appSettings.EchoHeadroomDb;
		EchoCompressorEnabled = appSettings.EchoCompressorEnabled;
		EchoCompressorThresholdDb = appSettings.EchoCompressorThresholdDb;
		EchoCompressorRatio = appSettings.EchoCompressorRatio;
		EchoCompressorAttackMs = appSettings.EchoCompressorAttackMs;
		EchoCompressorReleaseMs = appSettings.EchoCompressorReleaseMs;
		EchoCompressorKneeDb = appSettings.EchoCompressorKneeDb;
		EchoCompressorMakeupDb = appSettings.EchoCompressorMakeupDb;
		EchoCrossfeedEnabled = appSettings.EchoCrossfeedEnabled;
		EchoCrossfeedAmount = appSettings.EchoCrossfeedAmount;
		EchoCrossfeedCutoffHz = appSettings.EchoCrossfeedCutoffHz;
		EchoStereoFieldEnabled = appSettings.EchoStereoFieldEnabled;
		EchoStereoWidth = appSettings.EchoStereoWidth;
		EchoStereoCenterGainDb = appSettings.EchoStereoCenterGainDb;
		EchoStereoSideGainDb = appSettings.EchoStereoSideGainDb;
		EchoChannelMatrixPreset = appSettings.EchoChannelMatrixPreset;
		EchoReplayGainMode = appSettings.EchoReplayGainMode;
		EchoReplayGainPreampDb = appSettings.EchoReplayGainPreampDb;
		EchoReplayGainPreventClipping = appSettings.EchoReplayGainPreventClipping;
		EchoConvolutionEnabled = appSettings.EchoConvolutionEnabled;
		EchoConvolutionFilePath = appSettings.EchoConvolutionFilePath;
		EchoConvolutionTrimDb = appSettings.EchoConvolutionTrimDb;
		EchoResamplerEnabled = appSettings.EchoResamplerEnabled;
		EchoResamplerTargetRate = appSettings.EchoResamplerTargetRate;
		EchoSdmEnabled = appSettings.EchoSdmEnabled;
		EchoSdmOrder = appSettings.EchoSdmOrder;
		EchoSdmRate = appSettings.EchoSdmRate;
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
	}

	[RelayCommand]
	private void ApplyVst3()
	{
		ApplyVst3ToEngine();
		SaveVst3State();
	}

	private void ApplyVst3ToEngine()
	{
		_engine.SetVst3(Vst3Plugins.ToList());
	}

	private void SaveVst3State()
	{
		try
		{
			_settingsService.Update((AppSettings s) =>
			{
				s.Vst3ScanFolders = Vst3Folders.ToList();
				s.Vst3Plugins = Vst3Plugins.ToList();
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
	public double EchoHeadroomDb { get; set; } = 0.0;
	public bool EchoCompressorEnabled { get; set; } = false;
	public double EchoCompressorThresholdDb { get; set; } = -18.0;
	public double EchoCompressorRatio { get; set; } = 4.0;
	public double EchoCompressorAttackMs { get; set; } = 10.0;
	public double EchoCompressorReleaseMs { get; set; } = 120.0;
	public double EchoCompressorKneeDb { get; set; } = 6.0;
	public double EchoCompressorMakeupDb { get; set; } = 0.0;

	public bool EchoCrossfeedEnabled { get; set; } = false;
	public double EchoCrossfeedAmount { get; set; } = 0.35;
	public double EchoCrossfeedCutoffHz { get; set; } = 700.0;

	public bool EchoStereoFieldEnabled { get; set; } = false;
	public double EchoStereoWidth { get; set; } = 1.0;
	public double EchoStereoCenterGainDb { get; set; } = 0.0;
	public double EchoStereoSideGainDb { get; set; } = 0.0;

	public int EchoChannelMatrixPreset { get; set; } = 0;
	public int EchoReplayGainMode { get; set; } = 1;
	public double EchoReplayGainPreampDb { get; set; } = 0.0;
	public bool EchoReplayGainPreventClipping { get; set; } = true;

	public bool EchoConvolutionEnabled { get; set; } = false;
	public string? EchoConvolutionFilePath { get; set; }
	public double EchoConvolutionTrimDb { get; set; } = 0.0;

	public bool EchoResamplerEnabled { get; set; } = false;
	public int EchoResamplerTargetRate { get; set; } = 192000;

	public bool EchoSdmEnabled { get; set; } = false;
	public int EchoSdmOrder { get; set; } = 5;
	public int EchoSdmRate { get; set; } = 64;

	public void ApplyEchoDsp()
	{
		_engine.SetEchoHeadroom(EchoHeadroomDb);
		_engine.SetEchoCompressor(EchoCompressorEnabled, (float)EchoCompressorThresholdDb, (float)EchoCompressorRatio, (float)EchoCompressorAttackMs, (float)EchoCompressorReleaseMs, (float)EchoCompressorKneeDb, (float)EchoCompressorMakeupDb, 1.0f);
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
			s.EchoHeadroomDb = EchoHeadroomDb;
			s.EchoCompressorEnabled = EchoCompressorEnabled;
			s.EchoCompressorThresholdDb = EchoCompressorThresholdDb;
			s.EchoCompressorRatio = EchoCompressorRatio;
			s.EchoCompressorAttackMs = EchoCompressorAttackMs;
			s.EchoCompressorReleaseMs = EchoCompressorReleaseMs;
			s.EchoCompressorKneeDb = EchoCompressorKneeDb;
			s.EchoCompressorMakeupDb = EchoCompressorMakeupDb;
			s.EchoCrossfeedEnabled = EchoCrossfeedEnabled;
			s.EchoCrossfeedAmount = EchoCrossfeedAmount;
			s.EchoCrossfeedCutoffHz = EchoCrossfeedCutoffHz;
			s.EchoStereoFieldEnabled = EchoStereoFieldEnabled;
			s.EchoStereoWidth = EchoStereoWidth;
			s.EchoStereoCenterGainDb = EchoStereoCenterGainDb;
			s.EchoStereoSideGainDb = EchoStereoSideGainDb;
			s.EchoChannelMatrixPreset = EchoChannelMatrixPreset;
			s.EchoReplayGainMode = EchoReplayGainMode;
			s.EchoReplayGainPreampDb = EchoReplayGainPreampDb;
			s.EchoReplayGainPreventClipping = EchoReplayGainPreventClipping;
			s.EchoConvolutionEnabled = EchoConvolutionEnabled;
			s.EchoConvolutionFilePath = EchoConvolutionFilePath;
			s.EchoConvolutionTrimDb = EchoConvolutionTrimDb;
			s.EchoResamplerEnabled = EchoResamplerEnabled;
			s.EchoResamplerTargetRate = EchoResamplerTargetRate;
			s.EchoSdmEnabled = EchoSdmEnabled;
			s.EchoSdmOrder = EchoSdmOrder;
			s.EchoSdmRate = EchoSdmRate;
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

	public bool ImportAutoEqText(string text)
	{
		try
		{
			var preset = SonicWave.Audio.Dsp.AutoEqParser.Parse(text);
			if (preset.Bands.Count > 0)
			{
				double[] targetFrequencies = new double[10] { 31.0, 62.0, 125.0, 250.0, 500.0, 1000.0, 2000.0, 4000.0, 8000.0, 16000.0 };
				double[] newGains = new double[10];

				for (int i = 0; i < 10; i++)
				{
					double targetFreq = targetFrequencies[i];
					var best = preset.Bands.OrderBy(b => Math.Abs(Math.Log10(b.FrequencyHz) - Math.Log10(targetFreq))).FirstOrDefault();
					if (best != null && Math.Abs(Math.Log10(best.FrequencyHz) - Math.Log10(targetFreq)) < 0.25)
					{
						newGains[i] = Math.Clamp(best.GainDb, -12.0, 12.0);
					}
				}

				EqGains = newGains;
				ApplyEqGains(newGains);
				EqPresetName = "AutoEq 校准";
				StatusText = $"已导入 AutoEq 耳机预设 ({preset.Bands.Count} 个频点)";
				SaveEchoSettings();
				return true;
			}
		}
		catch (Exception ex)
		{
			StatusText = "导入 AutoEq 出错: " + ex.Message;
		}
		return false;
	}
}
