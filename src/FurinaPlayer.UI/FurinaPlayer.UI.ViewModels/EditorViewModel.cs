using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel.__Internals;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Dispatching;
using SonicWave.Audio;
using SonicWave.Audio.Dsp;
using SonicWave.Audio.Renderers;
using SonicWave.Audio.Vst3;
using SonicWave.Core.Models;
using SonicWave.Core.Services;
using WinRT;

namespace FurinaPlayer.UI.ViewModels;

[WinRTRuntimeClassName("Microsoft.UI.Xaml.Data.INotifyPropertyChanged")]
[WinRTExposedType(typeof(EditorViewModelWinRTTypeDetails))]
public class EditorViewModel : ObservableObject
{
	private sealed record EditSnapshot(float[] Samples, int Channels, int SampleRate, long SelStart, long SelEnd);

	public sealed record PreviewAnalysisResult(SpectrumPoint[] Spectrum, PhaseAnalyzer.LissajousPoint[] Phase, double PhaseDifference, double StereoCorrelation);

	private readonly AudioEngine? _engine;

	private float[] _samples = Array.Empty<float>();

	private int _channels = 1;

	private int _sampleRate = 44100;

	private long _selectionStart;

	private long _selectionEnd;

	private readonly List<EditSnapshot> _undoStack = new List<EditSnapshot>();

	private readonly List<EditSnapshot> _redoStack = new List<EditSnapshot>();

	private const int MaxUndoSteps = 15;

	private readonly DispatcherQueue? _uiDispatcher;

	[ObservableProperty]
	private ObservableCollection<Vst3PluginState> vst3Plugins = new ObservableCollection<Vst3PluginState>();

	[ObservableProperty]
	private string vst3StatusText = "效果组：启用插件后其效果应用到预览播放与导出（与 DSP 页共用同一份插件配置）";

	[ObservableProperty]
	private string filePath = string.Empty;

	[ObservableProperty]
	private string fileName = string.Empty;

	[ObservableProperty]
	private string statusText = "未加载音频文件，请先打开文件";

	[ObservableProperty]
	private ObservableCollection<WaveformRenderer.PeakPair> waveformPeaks = new ObservableCollection<WaveformRenderer.PeakPair>();

	[ObservableProperty]
	private ObservableCollection<SpectrumPoint> spectrumPoints = new ObservableCollection<SpectrumPoint>();

	[ObservableProperty]
	private ObservableCollection<PhaseAnalyzer.LissajousPoint> phasePoints = new ObservableCollection<PhaseAnalyzer.LissajousPoint>();

	[ObservableProperty]
	private double selectionStartSeconds;

	[ObservableProperty]
	private double selectionEndSeconds;

	[ObservableProperty]
	[NotifyPropertyChangedFor("DurationText")]
	private double durationSeconds;

	[ObservableProperty]
	[NotifyPropertyChangedFor("PhaseDifferenceText")]
	private double phaseDifference;

	[ObservableProperty]
	[NotifyPropertyChangedFor("StereoCorrelationText")]
	private double stereoCorrelation;

	[ObservableProperty]
	private double reverbMix = 0.25;

	[ObservableProperty]
	private string reverbPreset = "房间";

	[ObservableProperty]
	private bool reverbEnabled;

	[ObservableProperty]
	private double spatialWidth = 0.6;

	[ObservableProperty]
	private double spatialModulation = 0.25;

	[ObservableProperty]
	private double spatialPredelayMs = 25.0;

	[ObservableProperty]
	private double pitchSemitones;

	[ObservableProperty]
	private double speedRatio = 1.0;

	[ObservableProperty]
	private double fadeInSeconds = 0.5;

	[ObservableProperty]
	private double fadeOutSeconds = 0.5;

	[ObservableProperty]
	private string fadeCurve = "linear";

	[ObservableProperty]
	private double normalizeTargetDbfs = -1.0;

	[ObservableProperty]
	[NotifyCanExecuteChangedFor("UndoCommand")]
	private bool canUndo;

	[ObservableProperty]
	[NotifyCanExecuteChangedFor("RedoCommand")]
	private bool canRedo;

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.2.0.0")]
	private RelayCommand? applyVst3Command;

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.2.0.0")]
	private AsyncRelayCommand? trimSelectionCommand;

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.2.0.0")]
	private AsyncRelayCommand? cutSelectionCommand;

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.2.0.0")]
	private AsyncRelayCommand? applyFadeCommand;

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.2.0.0")]
	private AsyncRelayCommand? normalizeCommand;

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.2.0.0")]
	private AsyncRelayCommand? applyDspCommand;

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.2.0.0")]
	private AsyncRelayCommand<string>? exportCommand;

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.2.0.0")]
	private AsyncRelayCommand? undoCommand;

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.2.0.0")]
	private AsyncRelayCommand? redoCommand;

	public SettingsService? SettingsService { get; }

	public PreviewPlayer Preview { get; }

	public Equalizer Equalizer { get; }

	public FftAnalyzer Fft { get; }

	public IReadOnlyList<Vst3PluginState> ActiveVst3Plugins => Vst3Plugins.Where((Vst3PluginState p) => p?.IsActive ?? false).ToList();

	public int EditorSampleRate
	{
		get
		{
			if (_sampleRate <= 0)
			{
				return 44100;
			}
			return _sampleRate;
		}
	}

	public string DurationText => $"时长: {DurationSeconds:F1} 秒";

	public string PhaseDifferenceText => $"相位差: {PhaseDifference:F1}°";

	public string StereoCorrelationText => $"相关度: {StereoCorrelation:F2}";

	public double[] EqGains
	{
		get
		{
			return Equalizer.Gains;
		}
		set
		{
			Equalizer.Gains = value;
		}
	}

	public string SampleRateText => _sampleRate.ToString();

	public string ChannelsText => _channels.ToString();

	public long SelectionStartSample => _selectionStart;

	public long SelectionEndSample => _selectionEnd;

	public bool HasSelection => _selectionEnd > _selectionStart;

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
	public string FilePath
	{
		get
		{
			return filePath;
		}
		[MemberNotNull("filePath")]
		set
		{
			if (!EqualityComparer<string>.Default.Equals(filePath, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.FilePath);
				filePath = value;
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.FilePath);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.2.0.0")]
	[ExcludeFromCodeCoverage]
	public string FileName
	{
		get
		{
			return fileName;
		}
		[MemberNotNull("fileName")]
		set
		{
			if (!EqualityComparer<string>.Default.Equals(fileName, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.FileName);
				fileName = value;
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.FileName);
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
	public ObservableCollection<WaveformRenderer.PeakPair> WaveformPeaks
	{
		get
		{
			return waveformPeaks;
		}
		[MemberNotNull("waveformPeaks")]
		set
		{
			if (!EqualityComparer<ObservableCollection<WaveformRenderer.PeakPair>>.Default.Equals(waveformPeaks, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.WaveformPeaks);
				waveformPeaks = value;
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.WaveformPeaks);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.2.0.0")]
	[ExcludeFromCodeCoverage]
	public ObservableCollection<SpectrumPoint> SpectrumPoints
	{
		get
		{
			return spectrumPoints;
		}
		[MemberNotNull("spectrumPoints")]
		set
		{
			if (!EqualityComparer<ObservableCollection<SpectrumPoint>>.Default.Equals(spectrumPoints, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.SpectrumPoints);
				spectrumPoints = value;
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.SpectrumPoints);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.2.0.0")]
	[ExcludeFromCodeCoverage]
	public ObservableCollection<PhaseAnalyzer.LissajousPoint> PhasePoints
	{
		get
		{
			return phasePoints;
		}
		[MemberNotNull("phasePoints")]
		set
		{
			if (!EqualityComparer<ObservableCollection<PhaseAnalyzer.LissajousPoint>>.Default.Equals(phasePoints, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.PhasePoints);
				phasePoints = value;
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.PhasePoints);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.2.0.0")]
	[ExcludeFromCodeCoverage]
	public double SelectionStartSeconds
	{
		get
		{
			return selectionStartSeconds;
		}
		set
		{
			if (!EqualityComparer<double>.Default.Equals(selectionStartSeconds, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.SelectionStartSeconds);
				selectionStartSeconds = value;
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.SelectionStartSeconds);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.2.0.0")]
	[ExcludeFromCodeCoverage]
	public double SelectionEndSeconds
	{
		get
		{
			return selectionEndSeconds;
		}
		set
		{
			if (!EqualityComparer<double>.Default.Equals(selectionEndSeconds, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.SelectionEndSeconds);
				selectionEndSeconds = value;
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.SelectionEndSeconds);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.2.0.0")]
	[ExcludeFromCodeCoverage]
	public double DurationSeconds
	{
		get
		{
			return durationSeconds;
		}
		set
		{
			if (!EqualityComparer<double>.Default.Equals(durationSeconds, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.DurationSeconds);
				durationSeconds = value;
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.DurationSeconds);
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.DurationText);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.2.0.0")]
	[ExcludeFromCodeCoverage]
	public double PhaseDifference
	{
		get
		{
			return phaseDifference;
		}
		set
		{
			if (!EqualityComparer<double>.Default.Equals(phaseDifference, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.PhaseDifference);
				phaseDifference = value;
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.PhaseDifference);
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.PhaseDifferenceText);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.2.0.0")]
	[ExcludeFromCodeCoverage]
	public double StereoCorrelation
	{
		get
		{
			return stereoCorrelation;
		}
		set
		{
			if (!EqualityComparer<double>.Default.Equals(stereoCorrelation, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.StereoCorrelation);
				stereoCorrelation = value;
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.StereoCorrelation);
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.StereoCorrelationText);
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
	public double PitchSemitones
	{
		get
		{
			return pitchSemitones;
		}
		set
		{
			if (!EqualityComparer<double>.Default.Equals(pitchSemitones, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.PitchSemitones);
				pitchSemitones = value;
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.PitchSemitones);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.2.0.0")]
	[ExcludeFromCodeCoverage]
	public double SpeedRatio
	{
		get
		{
			return speedRatio;
		}
		set
		{
			if (!EqualityComparer<double>.Default.Equals(speedRatio, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.SpeedRatio);
				speedRatio = value;
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.SpeedRatio);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.2.0.0")]
	[ExcludeFromCodeCoverage]
	public double FadeInSeconds
	{
		get
		{
			return fadeInSeconds;
		}
		set
		{
			if (!EqualityComparer<double>.Default.Equals(fadeInSeconds, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.FadeInSeconds);
				fadeInSeconds = value;
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.FadeInSeconds);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.2.0.0")]
	[ExcludeFromCodeCoverage]
	public double FadeOutSeconds
	{
		get
		{
			return fadeOutSeconds;
		}
		set
		{
			if (!EqualityComparer<double>.Default.Equals(fadeOutSeconds, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.FadeOutSeconds);
				fadeOutSeconds = value;
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.FadeOutSeconds);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.2.0.0")]
	[ExcludeFromCodeCoverage]
	public string FadeCurve
	{
		get
		{
			return fadeCurve;
		}
		[MemberNotNull("fadeCurve")]
		set
		{
			if (!EqualityComparer<string>.Default.Equals(fadeCurve, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.FadeCurve);
				fadeCurve = value;
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.FadeCurve);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.2.0.0")]
	[ExcludeFromCodeCoverage]
	public double NormalizeTargetDbfs
	{
		get
		{
			return normalizeTargetDbfs;
		}
		set
		{
			if (!EqualityComparer<double>.Default.Equals(normalizeTargetDbfs, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.NormalizeTargetDbfs);
				normalizeTargetDbfs = value;
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.NormalizeTargetDbfs);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.2.0.0")]
	[ExcludeFromCodeCoverage]
	public bool CanUndo
	{
		get
		{
			return canUndo;
		}
		set
		{
			if (!EqualityComparer<bool>.Default.Equals(canUndo, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.CanUndo);
				canUndo = value;
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.CanUndo);
				UndoCommand.NotifyCanExecuteChanged();
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.2.0.0")]
	[ExcludeFromCodeCoverage]
	public bool CanRedo
	{
		get
		{
			return canRedo;
		}
		set
		{
			if (!EqualityComparer<bool>.Default.Equals(canRedo, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.CanRedo);
				canRedo = value;
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.CanRedo);
				RedoCommand.NotifyCanExecuteChanged();
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.2.0.0")]
	[ExcludeFromCodeCoverage]
	public IRelayCommand ApplyVst3Command => applyVst3Command ?? (applyVst3Command = new RelayCommand(ApplyVst3));

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.2.0.0")]
	[ExcludeFromCodeCoverage]
	public IAsyncRelayCommand TrimSelectionCommand => trimSelectionCommand ?? (trimSelectionCommand = new AsyncRelayCommand(TrimSelectionAsync));

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.2.0.0")]
	[ExcludeFromCodeCoverage]
	public IAsyncRelayCommand CutSelectionCommand => cutSelectionCommand ?? (cutSelectionCommand = new AsyncRelayCommand(CutSelectionAsync));

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.2.0.0")]
	[ExcludeFromCodeCoverage]
	public IAsyncRelayCommand ApplyFadeCommand => applyFadeCommand ?? (applyFadeCommand = new AsyncRelayCommand(ApplyFadeAsync));

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.2.0.0")]
	[ExcludeFromCodeCoverage]
	public IAsyncRelayCommand NormalizeCommand => normalizeCommand ?? (normalizeCommand = new AsyncRelayCommand(NormalizeAsync));

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.2.0.0")]
	[ExcludeFromCodeCoverage]
	public IAsyncRelayCommand ApplyDspCommand => applyDspCommand ?? (applyDspCommand = new AsyncRelayCommand(ApplyDspAsync));

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.2.0.0")]
	[ExcludeFromCodeCoverage]
	public IAsyncRelayCommand<string> ExportCommand => exportCommand ?? (exportCommand = new AsyncRelayCommand<string>(ExportAsync));

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.2.0.0")]
	[ExcludeFromCodeCoverage]
	public IAsyncRelayCommand UndoCommand => undoCommand ?? (undoCommand = new AsyncRelayCommand(UndoAsync, () => CanUndo));

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.2.0.0")]
	[ExcludeFromCodeCoverage]
	public IAsyncRelayCommand RedoCommand => redoCommand ?? (redoCommand = new AsyncRelayCommand(RedoAsync, () => CanRedo));

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

	public EditorViewModel(AudioEngine? engine = null, SettingsService? settingsService = null)
	{
		_engine = engine;
		SettingsService = settingsService;
		_uiDispatcher = DispatcherQueue.GetForCurrentThread();
		Equalizer = new Equalizer();
		Fft = new FftAnalyzer
		{
			FftSize = 8192
		};
		Preview = new PreviewPlayer();
		Preview.RenderingChanged += OnPreviewRenderingChanged;
		Preview.RenderProgress += OnPreviewRenderProgress;
		AppSettings appSettings = settingsService?.Load();
		if (appSettings != null && appSettings.Vst3Plugins != null)
		{
			Vst3Plugins = new ObservableCollection<Vst3PluginState>(appSettings.Vst3Plugins);
		}
		Preview.SetVst3(ActiveVst3Plugins);
	}

	private void OnPreviewRenderingChanged(bool rendering)
	{
		RunOnUiThread(() =>
		{
			StatusText = (rendering ? "正在预渲染预览效果…" : "预渲染完成，开始播放预览");
		});
	}

	private void OnPreviewRenderProgress(RenderProgress p)
	{
		RunOnUiThread(() =>
		{
			if (p.Fraction < 1.0)
			{
				StatusText = $"预渲染中… {p.Fraction:P0}，预计剩余 {Math.Max(0, (int)p.Remaining.TotalSeconds)} 秒";
			}
			else
			{
				StatusText = "预渲染完成，开始播放预览";
			}
		});
	}

	public void SavePanelLayouts(Dictionary<string, double[]> layouts)
	{
		SettingsService?.Update((AppSettings s) =>
		{
			s.PanelLayouts = layouts;
		});
	}

	public Dictionary<string, double[]> LoadPanelLayouts()
	{
		return SettingsService?.Load().PanelLayouts ?? new Dictionary<string, double[]>();
	}

	[RelayCommand]
	private void ApplyVst3()
	{
		Preview.SetVst3(ActiveVst3Plugins);
		SaveVst3State();
		Vst3StatusText = "效果组已更新：启用 " + ActiveVst3Plugins.Count + " 个插件，下次预览播放时生效";
	}

	private void SaveVst3State()
	{
		try
		{
			SettingsService?.Update((AppSettings s) =>
			{
				s.Vst3Plugins = Vst3Plugins.ToList();
			});
		}
		catch
		{
		}
	}

	public void ResetDefaults()
	{
		foreach (Vst3PluginState vst3Plugin in Vst3Plugins)
		{
			vst3Plugin.Enabled = false;
			vst3Plugin.Bypass = false;
		}
		Preview.SetVst3(ActiveVst3Plugins);
		SaveVst3State();
		Vst3StatusText = "效果组已恢复默认（插件配置清空，音乐库不受影响）";
	}

	public void ApplyPreviewEq(double[] gains)
	{
		Equalizer.Gains = gains;
		Preview.SetEqGains(gains);
	}

	public void ApplyPreviewReverb()
	{
		Preview.SetSpatialAudio(ReverbEnabled, ReverbMix, ReverbPreset, SpatialWidth, SpatialModulation, SpatialPredelayMs);
	}

	public async Task LoadFileAsync(string path)
	{
		FilePath = path;
		FileName = Path.GetFileName(path);
		StatusText = "正在读取…";
		try
		{
			await Task.Run(() =>
			{
				var (array, waveFormat) = AudioEditService.LoadWav(path);
				_samples = array;
				_channels = ((waveFormat.Channels <= 0) ? 1 : waveFormat.Channels);
				_sampleRate = ((waveFormat.SampleRate > 0) ? waveFormat.SampleRate : 44100);
				_selectionStart = 0L;
				_selectionEnd = array.Length / _channels;
			});
		}
		catch (Exception ex)
		{
			_samples = Array.Empty<float>();
			_channels = 1;
			_sampleRate = 44100;
			DurationSeconds = 0.0;
			WaveformPeaks = new ObservableCollection<WaveformRenderer.PeakPair>();
			SpectrumPoints = new ObservableCollection<SpectrumPoint>();
			PhasePoints = new ObservableCollection<PhaseAnalyzer.LissajousPoint>();
			PhaseDifference = 0.0;
			StereoCorrelation = 0.0;
			StatusText = "无法读取该音频文件：" + ex.Message;
			return;
		}
		DurationSeconds = (double)_samples.Length / (double)_channels / (double)_sampleRate;
		SelectionStartSeconds = 0.0;
		SelectionEndSeconds = DurationSeconds;
		_undoStack.Clear();
		_redoStack.Clear();
		UpdateHistoryState();
		Preview.Load(_samples, _sampleRate, _channels);
		Preview.SetEqGains(Equalizer.Gains);
		Preview.SetSpatialAudio(ReverbEnabled, ReverbMix, ReverbPreset, SpatialWidth, SpatialModulation, SpatialPredelayMs);
		Preview.SetVst3(ActiveVst3Plugins);
		await AnalyzeAllAsync();
		StatusText = $"{FileName} · {_sampleRate}Hz · {_channels}声道 · {DurationSeconds:F1}s";
	}

	public async Task AnalyzeAllAsync()
	{
		if (_samples.Length == 0 || _channels <= 0)
		{
			WaveformPeaks = new ObservableCollection<WaveformRenderer.PeakPair>();
			SpectrumPoints = new ObservableCollection<SpectrumPoint>();
			PhasePoints = new ObservableCollection<PhaseAnalyzer.LissajousPoint>();
			PhaseDifference = 0.0;
			StereoCorrelation = 0.0;
			return;
		}
		float[] samples = _samples;
		int channels = _channels;
		int sampleRate = _sampleRate;
		(List<WaveformRenderer.PeakPair>, List<SpectrumPoint>, List<PhaseAnalyzer.LissajousPoint>, double, double) tuple = await Task.Run(() =>
		{
			List<WaveformRenderer.PeakPair> item = WaveformRenderer.ComputePeaks(samples, channels, 1200);
			List<SpectrumPoint> item2 = Fft.Compute(samples, sampleRate, logarithmic: true, 96);
			float[] left = ExtractChannel(samples, channels, 0);
			float[] right = ExtractChannel(samples, channels, Math.Min(1, channels - 1));
			List<PhaseAnalyzer.LissajousPoint> item3 = PhaseAnalyzer.ComputeLissajous(left, right, 1500);
			return (peaks: item, spectrum: item2, phase: item3, PhaseAnalyzer.PhaseDifferenceDegrees(left, right), PhaseAnalyzer.StereoCorrelation(left, right));
		});
		WaveformPeaks = new ObservableCollection<WaveformRenderer.PeakPair>(tuple.Item1);
		SpectrumPoints = new ObservableCollection<SpectrumPoint>(tuple.Item2);
		PhasePoints = new ObservableCollection<PhaseAnalyzer.LissajousPoint>(tuple.Item3);
		PhaseDifference = tuple.Item4;
		StereoCorrelation = tuple.Item5;
	}

	public void SetSelection(double startSeconds, double endSeconds)
	{
		SelectionStartSeconds = startSeconds;
		SelectionEndSeconds = endSeconds;
		_selectionStart = (long)(startSeconds * (double)_sampleRate);
		_selectionEnd = (long)(endSeconds * (double)_sampleRate);
		OnPropertyChanged("HasSelection");
	}

	public void SelectAll()
	{
		if (_channels != 0)
		{
			_selectionStart = 0L;
			_selectionEnd = _samples.Length / _channels;
			SelectionStartSeconds = 0.0;
			SelectionEndSeconds = DurationSeconds;
			OnPropertyChanged("HasSelection");
		}
	}

	[RelayCommand]
	private async Task TrimSelectionAsync()
	{
		if (HasSelection)
		{
			await ApplyEditAsync((float[] s) => AudioEditService.Trim(s, _channels, _selectionStart, _selectionEnd));
			StatusText = "已裁剪选区";
		}
	}

	[RelayCommand]
	private async Task CutSelectionAsync()
	{
		if (HasSelection)
		{
			await ApplyEditAsync((float[] s) => AudioEditService.CutSelection(s, _channels, _selectionStart, _selectionEnd));
			StatusText = "已删除选区";
		}
	}

	[RelayCommand]
	private async Task ApplyFadeAsync()
	{
		await ApplyEditAsync((float[] s) =>
		{
			AudioEditService.ApplyFade(s, _channels, FadeInSeconds, FadeOutSeconds, _sampleRate, FadeCurve);
			return s;
		});
		StatusText = "已应用淡入淡出";
	}

	[RelayCommand]
	private async Task NormalizeAsync()
	{
		await ApplyEditAsync((float[] s) =>
		{
			AudioEditService.NormalizePeak(s, NormalizeTargetDbfs);
			return s;
		});
		StatusText = $"已标准化至 {NormalizeTargetDbfs} dBFS";
	}

	[RelayCommand]
	private async Task ApplyDspAsync()
	{
		await ApplyEditAsync((float[] s) =>
		{
			Equalizer.ProcessInterleaved(s, _channels);
			if (ReverbEnabled)
			{
				SpatialAudioEffect spatialAudioEffect = new SpatialAudioEffect(_sampleRate);
				spatialAudioEffect.SetPreset(ReverbPreset);
				spatialAudioEffect.WetMix = ReverbMix;
				spatialAudioEffect.Width = SpatialWidth;
				spatialAudioEffect.Modulation = SpatialModulation;
				spatialAudioEffect.PredelayMs = SpatialPredelayMs;
				spatialAudioEffect.ProcessInterleaved(s, _channels);
			}
			if (Math.Abs(PitchSemitones) > 0.01)
			{
				PitchShifter shifter = new PitchShifter();
				s = ApplyPerChannel(s, _channels, (float[] ch) => shifter.PitchShift(ch, PitchSemitones));
			}
			if (Math.Abs(SpeedRatio - 1.0) > 0.01)
			{
				PitchShifter shifter2 = new PitchShifter();
				s = ApplyPerChannel(s, _channels, (float[] ch) => shifter2.TimeStretch(ch, SpeedRatio));
			}
			foreach (Vst3PluginState vst3Plugin in Vst3Plugins)
			{
				if (vst3Plugin != null && vst3Plugin.IsActive)
				{
					Vst3SimulatedProcessor.Process(s, _channels, new Vst3Parameters(vst3Plugin.Drive, vst3Plugin.Tone, vst3Plugin.Mix));
				}
			}
			return s;
		});
		StatusText = "已应用 DSP 效果（含效果组 VST3 插件）";
	}

	[RelayCommand]
	private async Task ExportAsync(string exportPath)
	{
		try
		{
			await Task.Run(() =>
			{
				AudioEditService.ExportWav(exportPath, _samples, _sampleRate, _channels);
			});
			StatusText = "已导出: " + exportPath;
		}
		catch (Exception ex)
		{
			StatusText = "导出失败：" + ex.Message;
		}
	}

	private async Task ApplyEditAsync(Func<float[], float[]> edit)
	{
		try
		{
			await Task.Run(() =>
			{
				EditSnapshot snapshot = CaptureSnapshot();
				float[] samples = edit(_samples);
				PushUndo(snapshot);
				_samples = samples;
			});
		}
		catch (Exception ex)
		{
			StatusText = "编辑失败：" + ex.Message;
			return;
		}
		_selectionStart = 0L;
		_selectionEnd = _samples.Length / _channels;
		SelectionStartSeconds = 0.0;
		SelectionEndSeconds = (double)_samples.Length / (double)_channels / (double)_sampleRate;
		DurationSeconds = SelectionEndSeconds;
		Preview.Load(_samples, _sampleRate, _channels);
		Preview.SetEqGains(Equalizer.Gains);
		Preview.SetSpatialAudio(ReverbEnabled, ReverbMix, ReverbPreset, SpatialWidth, SpatialModulation, SpatialPredelayMs);
		Preview.SetVst3(ActiveVst3Plugins);
		await AnalyzeAllAsync();
	}

	public async Task<bool> TryAutoLoadCurrentTrackAsync()
	{
		Track track = _engine?.CurrentTrack;
		if (track == null || string.IsNullOrEmpty(track.FilePath) || !File.Exists(track.FilePath))
		{
			return false;
		}
		await LoadFileAsync(track.FilePath);
		return true;
	}

	[RelayCommand(CanExecute = "CanUndo")]
	private async Task UndoAsync()
	{
		if (_undoStack.Count != 0)
		{
			_redoStack.Add(CaptureSnapshot());
			List<EditSnapshot> undoStack = _undoStack;
			EditSnapshot snap = undoStack[undoStack.Count - 1];
			_undoStack.RemoveAt(_undoStack.Count - 1);
			await RestoreSnapshotAsync(snap);
			StatusText = "已撤销上一步";
		}
	}

	[RelayCommand(CanExecute = "CanRedo")]
	private async Task RedoAsync()
	{
		if (_redoStack.Count != 0)
		{
			_undoStack.Add(CaptureSnapshot());
			List<EditSnapshot> redoStack = _redoStack;
			EditSnapshot snap = redoStack[redoStack.Count - 1];
			_redoStack.RemoveAt(_redoStack.Count - 1);
			await RestoreSnapshotAsync(snap);
			StatusText = "已重做下一步";
		}
	}

	public PreviewAnalysisResult ComputePreviewAnalysisAt(TimeSpan pos)
	{
		if (_samples.Length == 0 || _channels <= 0 || _sampleRate <= 0)
		{
			return new PreviewAnalysisResult(Array.Empty<SpectrumPoint>(), Array.Empty<PhaseAnalyzer.LissajousPoint>(), 0.0, 0.0);
		}
		long num = _samples.Length / _channels;
		int num2 = Math.Min(Fft.FftSize, (int)num);
		if (num2 < 256)
		{
			return new PreviewAnalysisResult(Array.Empty<SpectrumPoint>(), Array.Empty<PhaseAnalyzer.LissajousPoint>(), 0.0, 0.0);
		}
		long num3 = Math.Clamp((long)(pos.TotalSeconds * (double)_sampleRate) - num2 / 2, 0L, Math.Max(0L, num - num2));
		float[] array = new float[num2 * _channels];
		Array.Copy(_samples, num3 * _channels, array, 0L, array.Length);
		List<SpectrumPoint> list = Fft.Compute(array, _sampleRate, logarithmic: true, 64);
		float[] left = ExtractChannel(array, _channels, 0);
		float[] right = ExtractChannel(array, _channels, Math.Min(1, _channels - 1));
		return new PreviewAnalysisResult(Phase: PhaseAnalyzer.ComputeLissajous(left, right, 400).ToArray(), Spectrum: list.ToArray(), PhaseDifference: PhaseAnalyzer.PhaseDifferenceDegrees(left, right), StereoCorrelation: PhaseAnalyzer.StereoCorrelation(left, right));
	}

	public void UpdateAnalysisScalars(double phaseDifference, double correlation)
	{
		PhaseDifference = phaseDifference;
		StereoCorrelation = correlation;
	}

	private EditSnapshot CaptureSnapshot()
	{
		return new EditSnapshot((float[])_samples.Clone(), _channels, _sampleRate, _selectionStart, _selectionEnd);
	}

	private void PushUndo(EditSnapshot snapshot)
	{
		_undoStack.Add(snapshot);
		if (_undoStack.Count > 15)
		{
			_undoStack.RemoveAt(0);
		}
		_redoStack.Clear();
		UpdateHistoryState();
	}

	private async Task RestoreSnapshotAsync(EditSnapshot snap)
	{
		_samples = snap.Samples;
		_channels = ((snap.Channels <= 0) ? 1 : snap.Channels);
		_sampleRate = ((snap.SampleRate > 0) ? snap.SampleRate : 44100);
		_selectionStart = snap.SelStart;
		_selectionEnd = snap.SelEnd;
		SelectionStartSeconds = ((_samples.Length / _channels > 0) ? ((double)_selectionStart / (double)_sampleRate) : 0.0);
		SelectionEndSeconds = (double)_selectionEnd / (double)_sampleRate;
		DurationSeconds = (double)_samples.Length / (double)_channels / (double)_sampleRate;
		UpdateHistoryState();
		Preview.Load(_samples, _sampleRate, _channels);
		Preview.SetEqGains(Equalizer.Gains);
		Preview.SetSpatialAudio(ReverbEnabled, ReverbMix, ReverbPreset, SpatialWidth, SpatialModulation, SpatialPredelayMs);
		Preview.SetVst3(ActiveVst3Plugins);
		await AnalyzeAllAsync();
	}

	private void UpdateHistoryState()
	{
		CanUndo = _undoStack.Count > 0;
		CanRedo = _redoStack.Count > 0;
	}

	public void Dispose()
	{
		Preview.Dispose();
	}

	private static float[] ExtractChannel(float[] samples, int channels, int channel)
	{
		int num = samples.Length / channels;
		float[] array = new float[num];
		for (int i = 0; i < num; i++)
		{
			array[i] = samples[i * channels + channel];
		}
		return array;
	}

	private static float[] ApplyPerChannel(float[] samples, int channels, Func<float[], float[]> fn)
	{
		float[] array = new float[samples.Length];
		for (int i = 0; i < channels; i++)
		{
			float[] arg = ExtractChannel(samples, channels, i);
			float[] array2 = fn(arg);
			for (int j = 0; j < array2.Length && i + j * channels < array.Length; j++)
			{
				array[i + j * channels] = array2[j];
			}
		}
		return array;
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.2.0.0")]
	private void OnReverbMixChanged(double value)
	{
		ApplyPreviewReverb();
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.2.0.0")]
	private void OnReverbPresetChanged(string value)
	{
		ApplyPreviewReverb();
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.2.0.0")]
	private void OnReverbEnabledChanged(bool value)
	{
		ApplyPreviewReverb();
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.2.0.0")]
	private void OnSpatialWidthChanged(double value)
	{
		ApplyPreviewReverb();
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.2.0.0")]
	private void OnSpatialModulationChanged(double value)
	{
		ApplyPreviewReverb();
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.2.0.0")]
	private void OnSpatialPredelayMsChanged(double value)
	{
		ApplyPreviewReverb();
	}
}
