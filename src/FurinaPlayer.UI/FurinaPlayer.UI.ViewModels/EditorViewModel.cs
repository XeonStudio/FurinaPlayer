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
using FurinaPlayer.UI.Helpers;
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

	private string _lastCommittedVst3FullSignature = string.Empty;

	private string _lastPreviewVst3Signature = string.Empty;

	private HashSet<string> _lastCommittedEnabledPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

	private readonly object _echoEqLock = new object();

	private readonly object _insightEqLock = new object();

	private double _echoEqPreampDb;

	private bool _echoEqEnabled = true;

	private int _echoEqVersion;

	private int _insightEqVersion = -1;

	private int _insightEqSampleRate;

	private double _lastInsightPosSec = -1.0;

	private EchoEqProcessor? _previewInsightEq;

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

	public IReadOnlyList<WaveformRenderer.PeakPair> LeftWaveformPeaks { get; private set; } = Array.Empty<WaveformRenderer.PeakPair>();
	public IReadOnlyList<WaveformRenderer.PeakPair> RightWaveformPeaks { get; private set; } = Array.Empty<WaveformRenderer.PeakPair>();
	public IReadOnlyList<float> LeftRms { get; private set; } = Array.Empty<float>();
	public IReadOnlyList<float> RightRms { get; private set; } = Array.Empty<float>();

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

	public InsightProAnalyzer InsightAnalyzer { get; } = new InsightProAnalyzer();

	public List<EchoBiquadFilter> EchoEqBands { get; } = new List<EchoBiquadFilter>();

	public double EchoEqPreampDb
	{
		get => _echoEqPreampDb;
		set => SetProperty(ref _echoEqPreampDb, Math.Clamp(value, -18.0, 18.0));
	}

	public bool EchoEqEnabled
	{
		get => _echoEqEnabled;
		set => SetProperty(ref _echoEqEnabled, value);
	}

	public bool HasActiveParametricEq
	{
		get
		{
			lock (_echoEqLock)
			{
				return _echoEqEnabled && HasActiveParametricEqLocked();
			}
		}
	}

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
		InitializeEchoEqBands();
		Fft = new FftAnalyzer
		{
			FftSize = 8192
		};
		Preview = new PreviewPlayer();
		Preview.RenderingChanged += OnPreviewRenderingChanged;
		Preview.RenderProgress += OnPreviewRenderProgress;
		Vst3SyncHub.PluginsSynced += OnExternalVst3Synced;
		if (Vst3SyncHub.TryGetLatest(out List<Vst3PluginState> syncedPlugins))
		{
			Vst3Plugins = new ObservableCollection<Vst3PluginState>(syncedPlugins);
		}
		else
		{
			AppSettings appSettings = settingsService?.Load();
			if (appSettings != null && appSettings.Vst3Plugins != null)
			{
				Vst3Plugins = new ObservableCollection<Vst3PluginState>(Vst3SyncHub.CloneList(appSettings.Vst3Plugins));
			}
		}
		_lastCommittedVst3FullSignature = Vst3SyncHub.BuildFullSignature(Vst3Plugins);
		_lastCommittedEnabledPaths = new HashSet<string>(
			Vst3Plugins.Where((Vst3PluginState p) => p != null && p.Enabled && !string.IsNullOrWhiteSpace(p.Path)).Select((Vst3PluginState p) => p.Path),
			StringComparer.OrdinalIgnoreCase);
		ApplyPreviewVst3IfChanged(force: true);
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

	private void ApplyPreviewVst3IfChanged(bool force = false)
	{
		IReadOnlyList<Vst3PluginState> active = ActiveVst3Plugins;
		string sig = Vst3SyncHub.BuildActiveSignature(active);
		if (!force && string.Equals(_lastPreviewVst3Signature, sig, StringComparison.Ordinal))
		{
			return;
		}
		_lastPreviewVst3Signature = sig;
		Preview.SetVst3(active);
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
			if (!string.Equals(_lastCommittedVst3FullSignature, incomingSig, StringComparison.Ordinal))
			{
				List<Vst3PluginState> cloned = Vst3SyncHub.CloneList(plugins);
				_lastCommittedVst3FullSignature = incomingSig;
				_lastCommittedEnabledPaths = new HashSet<string>(
					cloned.Where((Vst3PluginState p) => p.Enabled && !string.IsNullOrWhiteSpace(p.Path)).Select((Vst3PluginState p) => p.Path),
					StringComparer.OrdinalIgnoreCase);
				Vst3Plugins = new ObservableCollection<Vst3PluginState>(cloned);
			}
			ApplyPreviewVst3IfChanged(force: false);
			Vst3PluginState? active = Vst3Plugins.FirstOrDefault((Vst3PluginState p) => p != null && p.IsActive);
			Vst3StatusText = active != null
				? $"效果组已同步启用：{active.Name}（预览与主播放引擎实时生效）"
				: "效果组已同步：当前未启用插件（直通）";
		});
	}

	[RelayCommand]
	private void ApplyVst3()
	{
		List<Vst3PluginState> enabledPlugins = Vst3Plugins.Where((Vst3PluginState p) => p != null && p.Enabled).ToList();
		bool normalizedChanged = false;
		if (enabledPlugins.Count > 1)
		{
			string? activeEditorPath = Vst3EditorWindow.ActivePluginPath;
			Vst3PluginState? winner = (!string.IsNullOrWhiteSpace(activeEditorPath))
				? enabledPlugins.FirstOrDefault((Vst3PluginState p) => string.Equals(p.Path, activeEditorPath, StringComparison.OrdinalIgnoreCase))
				: null;
			winner ??= enabledPlugins.FirstOrDefault((Vst3PluginState p) => !string.IsNullOrWhiteSpace(p.Path) && !_lastCommittedEnabledPaths.Contains(p.Path));
			winner ??= enabledPlugins.FirstOrDefault((Vst3PluginState p) => p.IsActive) ?? enabledPlugins[0];
			foreach (Vst3PluginState p in enabledPlugins)
			{
				if (!ReferenceEquals(p, winner))
				{
					p.Enabled = false;
					p.Bypass = false;
					normalizedChanged = true;
				}
			}
		}

		string currentFullSig = Vst3SyncHub.BuildFullSignature(Vst3Plugins);
		Vst3PluginState? firstActive = Vst3Plugins.FirstOrDefault((Vst3PluginState p) => p != null && p.IsActive);
		bool hostMatches = _engine == null || (firstActive != null
			? Vst3NativeHost.Instance.IsLoadedFor(firstActive.Path)
			: !Vst3NativeHost.Instance.IsLoaded);

		if (!normalizedChanged && string.Equals(_lastCommittedVst3FullSignature, currentFullSig, StringComparison.Ordinal) && hostMatches)
		{
			ApplyPreviewVst3IfChanged(force: false);
			return;
		}

		_lastCommittedVst3FullSignature = currentFullSig;
		_lastCommittedEnabledPaths = new HashSet<string>(
			Vst3Plugins.Where((Vst3PluginState p) => p != null && p.Enabled && !string.IsNullOrWhiteSpace(p.Path)).Select((Vst3PluginState p) => p.Path),
			StringComparer.OrdinalIgnoreCase);

		if (normalizedChanged)
		{
			// Refresh items so EditorPage ToggleSwitch reflects single-slot exclusivity;
			// rebinding fires Toggled which will hit the signature guard above and no-op.
			Vst3Plugins = new ObservableCollection<Vst3PluginState>(Vst3SyncHub.CloneList(Vst3Plugins));
		}

		ApplyPreviewVst3IfChanged(force: false);
		Vst3SyncHub.PushToEngine(_engine, Vst3Plugins.ToList());
		SaveVst3State();
		Vst3SyncHub.Publish(this, Vst3Plugins);
		Vst3StatusText = ActiveVst3Plugins.Count > 0
			? $"效果组已更新：启用 {ActiveVst3Plugins[0].Name}（预览与主播放引擎同步生效）"
			: "效果组已更新：当前未启用 VST3 插件（直通）";
	}

	private void SaveVst3State()
	{
		try
		{
			List<Vst3PluginState> snapshot = Vst3SyncHub.CloneList(Vst3Plugins);
			SettingsService?.Update((AppSettings s) =>
			{
				s.Vst3Plugins = snapshot;
			});
		}
		catch
		{
		}
	}

	public void ResetDefaults()
	{
		bool hadActiveOrEnabled = false;
		foreach (Vst3PluginState vst3Plugin in Vst3Plugins)
		{
			if (vst3Plugin != null && (vst3Plugin.Enabled || vst3Plugin.Bypass))
			{
				hadActiveOrEnabled = true;
			}
			if (vst3Plugin != null)
			{
				vst3Plugin.Enabled = false;
				vst3Plugin.Bypass = false;
			}
		}
		_lastCommittedVst3FullSignature = Vst3SyncHub.BuildFullSignature(Vst3Plugins);
		_lastCommittedEnabledPaths.Clear();
		if (hadActiveOrEnabled)
		{
			Vst3Plugins = new ObservableCollection<Vst3PluginState>(Vst3SyncHub.CloneList(Vst3Plugins));
		}
		ApplyPreviewVst3IfChanged(force: false);
		Vst3SyncHub.PushToEngine(_engine, Vst3Plugins.ToList());
		SaveVst3State();
		Vst3SyncHub.Publish(this, Vst3Plugins);
		Vst3StatusText = "效果组已恢复默认（插件配置清空，音乐库不受影响）";
	}

	public void InitializeEchoEqBands()
	{
		lock (_echoEqLock)
		{
			PopulateDefaultEchoEqBandsLocked(_sampleRate > 0 ? _sampleRate : 48000);
			_echoEqPreampDb = 0.0;
			_echoEqEnabled = true;
			_echoEqVersion++;
		}
		OnPropertyChanged(nameof(EchoEqPreampDb));
		OnPropertyChanged(nameof(EchoEqEnabled));
		Equalizer.Gains = new double[10];
	}

	private void PopulateDefaultEchoEqBandsLocked(double sampleRate)
	{
		EchoEqBands.Clear();
		double[] freqs = MixAssistantEngine.GraphicFrequencies;
		for (int i = 0; i < freqs.Length; i++)
		{
			var band = new EchoBiquadFilter
			{
				FilterType = i == 0
					? EchoFilterType.LowShelf
					: (i == freqs.Length - 1 ? EchoFilterType.HighShelf : EchoFilterType.Peaking),
				FrequencyHz = freqs[i],
				GainDb = 0.0,
				Q = 1.414,
				Enabled = true
			};
			band.UpdateCoefficients(sampleRate);
			EchoEqBands.Add(band);
		}
	}

	public void ApplyPreviewParametricEq(IReadOnlyList<EchoBiquadFilter>? bands, double preampDb, bool enabled)
	{
		List<EchoBiquadFilter>? activeSnapshot = null;
		double[] summaryGains = new double[10];
		double clampedPreamp = Math.Clamp(preampDb, -18.0, 18.0);

		lock (_echoEqLock)
		{
			int rate = _sampleRate > 0 ? _sampleRate : 48000;
			EchoEqBands.Clear();
			if (bands != null && bands.Count > 0)
			{
				for (int i = 0; i < bands.Count; i++)
				{
					EchoBiquadFilter? src = bands[i];
					if (src == null) continue;
					var copy = new EchoBiquadFilter
					{
						FilterType = src.FilterType,
						FrequencyHz = src.FrequencyHz,
						GainDb = src.GainDb,
						Q = src.Q,
						Enabled = src.Enabled
					};
					copy.UpdateCoefficients(rate);
					EchoEqBands.Add(copy);
				}
			}
			if (EchoEqBands.Count == 0)
			{
				PopulateDefaultEchoEqBandsLocked(rate);
			}

			_echoEqPreampDb = clampedPreamp;
			_echoEqEnabled = enabled;
			_echoEqVersion++;

			double[] isoFreqs = MixAssistantEngine.GraphicFrequencies;
			for (int i = 0; i < 10; i++)
			{
				if (!enabled)
				{
					summaryGains[i] = 0.0;
					continue;
				}
				if (i < EchoEqBands.Count && Math.Abs(EchoEqBands[i].FrequencyHz - isoFreqs[i]) < isoFreqs[i] * 0.2)
				{
					summaryGains[i] = EchoEqBands[i].Enabled ? Math.Clamp(EchoEqBands[i].GainDb, -12.0, 12.0) : 0.0;
				}
				else
				{
					double resp = 0.0;
					for (int b = 0; b < EchoEqBands.Count; b++)
					{
						if (EchoEqBands[b].Enabled)
						{
							resp += EchoEqBands[b].CalculateResponseDb(isoFreqs[i], 48000.0);
						}
					}
					summaryGains[i] = Math.Clamp(resp, -12.0, 12.0);
				}
			}

			if (enabled)
			{
				activeSnapshot = CloneEchoBands(EchoEqBands, rate);
			}
		}

		OnPropertyChanged(nameof(EchoEqPreampDb));
		OnPropertyChanged(nameof(EchoEqEnabled));
		Equalizer.Gains = summaryGains;
		Preview.SetParametricEq(activeSnapshot, enabled ? clampedPreamp : 0.0);
	}

	public bool ImportAutoEqToParametricBands(string text)
	{
		if (string.IsNullOrWhiteSpace(text))
		{
			return false;
		}
		try
		{
			AutoEqPreset preset = AutoEqParser.Parse(text);
			if (preset.Bands.Count == 0)
			{
				StatusText = "AutoEq 文件中未检测到有效滤波频段";
				return false;
			}
			ApplyPreviewParametricEq(preset.Bands, preset.PreampDb, enabled: true);
			StatusText = $"已导入 AutoEq 参数均衡（{preset.Bands.Count} 段，前级 {preset.PreampDb:+0.0;-0.0;0.0} dB）";
			return true;
		}
		catch (Exception ex)
		{
			StatusText = "AutoEq 导入失败：" + ex.Message;
			return false;
		}
	}

	public Task<MixAssistantProfile> AnalyzeMixAssistantOnLoadedSamples()
	{
		float[] samples = _samples;
		int channels = _channels;
		int sampleRate = _sampleRate;
		return Task.Run(() =>
		{
			var engine = new MixAssistantEngine();
			return engine.AnalyzeSamples(samples, channels, sampleRate);
		});
	}

	public InsightProSnapshot GetCurrentInsightSnapshot() => InsightAnalyzer.GetSnapshot();

	public bool TryGetCurrentSpectrum64(float[] spectrumOut64)
	{
		if (spectrumOut64 == null || spectrumOut64.Length == 0 || _samples.Length == 0)
		{
			return false;
		}
		PopulateSpectrum64FromSnapshot(InsightAnalyzer.GetSnapshot(), spectrumOut64);
		return true;
	}

	public static void PopulateSpectrum64FromSnapshot(InsightProSnapshot snapshot, float[] spectrumOut64)
	{
		if (spectrumOut64 == null || spectrumOut64.Length == 0)
		{
			return;
		}
		float[] src = snapshot.SpectrumDb;
		if (src == null || src.Length == 0)
		{
			Array.Clear(spectrumOut64, 0, spectrumOut64.Length);
			return;
		}

		int outLen = spectrumOut64.Length;
		int srcLen = src.Length;
		for (int i = 0; i < outLen; i++)
		{
			double f0 = (double)i * srcLen / outLen;
			double f1 = (double)(i + 1) * srcLen / outLen;
			int idx0 = Math.Clamp((int)Math.Floor(f0), 0, srcLen - 1);
			int idx1 = Math.Clamp((int)Math.Ceiling(f1) - 1, idx0, srcLen - 1);
			float sum = 0f;
			int count = 0;
			for (int k = idx0; k <= idx1; k++)
			{
				sum += src[k];
				count++;
			}
			float avgDb = count > 0 ? sum / count : -110f;
			spectrumOut64[i] = Math.Clamp((avgDb + 80.0f) / 80.0f, 0.0f, 1.0f);
		}
	}

	private bool HasActiveParametricEqLocked()
	{
		if (Math.Abs(_echoEqPreampDb) > 0.01)
		{
			return true;
		}
		for (int i = 0; i < EchoEqBands.Count; i++)
		{
			EchoBiquadFilter? b = EchoEqBands[i];
			if (b != null && b.Enabled &&
				(Math.Abs(b.GainDb) > 0.01 ||
				 b.FilterType is EchoFilterType.LowPass or EchoFilterType.HighPass or EchoFilterType.BandPass or EchoFilterType.Notch))
			{
				return true;
			}
		}
		return false;
	}

	private static List<EchoBiquadFilter> CloneEchoBands(IReadOnlyList<EchoBiquadFilter> source, double sampleRate)
	{
		var cloned = new List<EchoBiquadFilter>(source.Count);
		for (int i = 0; i < source.Count; i++)
		{
			EchoBiquadFilter? src = source[i];
			if (src == null) continue;
			var copy = new EchoBiquadFilter
			{
				FilterType = src.FilterType,
				FrequencyHz = src.FrequencyHz,
				GainDb = src.GainDb,
				Q = src.Q,
				Enabled = src.Enabled
			};
			copy.UpdateCoefficients(sampleRate);
			cloned.Add(copy);
		}
		return cloned;
	}

	private EchoEqProcessor? CreateActiveEchoEqProcessor(int sampleRate)
	{
		int safeRate = sampleRate > 0 ? sampleRate : 44100;
		List<EchoBiquadFilter> snapshot;
		double preampDb;
		lock (_echoEqLock)
		{
			if (!_echoEqEnabled || !HasActiveParametricEqLocked())
			{
				return null;
			}
			snapshot = CloneEchoBands(EchoEqBands, safeRate);
			preampDb = _echoEqPreampDb;
		}

		var eq = new EchoEqProcessor(safeRate)
		{
			Enabled = true,
			CalibrationEnabled = true,
			PreampDb = preampDb
		};
		eq.Prepare(safeRate);
		eq.SetCalibrationBands(snapshot, 0.0);
		return eq;
	}

	private void ApplyActivePreviewEqForInsight(float[] interleaved, int channels, int sampleRate, double posSec)
	{
		lock (_insightEqLock)
		{
			int currentVersion;
			bool eqEnabled;
			lock (_echoEqLock)
			{
				currentVersion = _echoEqVersion;
				eqEnabled = _echoEqEnabled;
			}

			if (!eqEnabled)
			{
				_previewInsightEq = null;
				_insightEqVersion = currentVersion;
				_lastInsightPosSec = posSec;
				return;
			}

			if (currentVersion != _insightEqVersion || _insightEqSampleRate != sampleRate)
			{
				_insightEqVersion = currentVersion;
				_insightEqSampleRate = sampleRate;
				_previewInsightEq = CreateActiveEchoEqProcessor(sampleRate);
			}

			if (_previewInsightEq != null)
			{
				if (posSec < _lastInsightPosSec - 0.05 || posSec - _lastInsightPosSec > 0.5)
				{
					_previewInsightEq.Reset();
				}
				_previewInsightEq.ProcessInterleaved(interleaved, 0, interleaved.Length, channels);
			}
			_lastInsightPosSec = posSec;
		}
	}

	private void SeedInsightAnalyzer(float[] samples, int channels, int sampleRate)
	{
		if (samples == null || samples.Length == 0 || channels <= 0 || sampleRate <= 0)
		{
			InsightAnalyzer.Reset();
			return;
		}

		int totalFrames = samples.Length / channels;
		if (totalFrames <= 0)
		{
			InsightAnalyzer.Reset();
			return;
		}

		int seedFrames = Math.Min(totalFrames, sampleRate * 4);
		float[] seedBuffer = new float[seedFrames * channels];
		Array.Copy(samples, 0, seedBuffer, 0, seedBuffer.Length);

		EchoEqProcessor? eq = CreateActiveEchoEqProcessor(sampleRate);
		eq?.ProcessInterleaved(seedBuffer, 0, seedBuffer.Length, channels);

		InsightAnalyzer.Reset();
		InsightAnalyzer.AnalyzeWindow(seedBuffer, channels, sampleRate, 0, seedFrames);
	}

	public InsightProSnapshot AdvancePreviewInsight(TimeSpan position, TimeSpan duration, float[]? spectrumOut64 = null)
	{
		float[] samples = _samples;
		int channels = _channels;
		int sampleRate = _sampleRate;
		if (samples.Length == 0 || channels <= 0 || sampleRate <= 0)
		{
			if (spectrumOut64 != null)
			{
				Array.Clear(spectrumOut64, 0, spectrumOut64.Length);
			}
			return InsightAnalyzer.GetSnapshot();
		}

		long totalFrames = samples.Length / channels;
		int chunkFrames = Math.Min(2048, (int)totalFrames);
		if (chunkFrames <= 0)
		{
			if (spectrumOut64 != null)
			{
				Array.Clear(spectrumOut64, 0, spectrumOut64.Length);
			}
			return InsightAnalyzer.GetSnapshot();
		}

		double posSec = Math.Max(0.0, position.TotalSeconds);
		if (duration.TotalSeconds > 0.0)
		{
			posSec = Math.Min(posSec, duration.TotalSeconds);
		}

		long centerFrame = (long)(posSec * sampleRate);
		long startFrame = Math.Clamp(centerFrame - chunkFrames / 2, 0L, Math.Max(0L, totalFrames - chunkFrames));

		float[] chunk = new float[chunkFrames * channels];
		Array.Copy(samples, startFrame * channels, chunk, 0L, chunk.Length);

		ApplyActivePreviewEqForInsight(chunk, channels, sampleRate, posSec);

		InsightAnalyzer.PushSamples(chunk, channels, sampleRate);
		InsightProSnapshot snapshot = InsightAnalyzer.GetSnapshot();

		if (spectrumOut64 != null)
		{
			PopulateSpectrum64FromSnapshot(snapshot, spectrumOut64);
		}

		return snapshot;
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
			InsightAnalyzer.Reset();
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
		Preview.SetEqGains(new double[10]);
		SyncPreviewParametricEq();
		Preview.SetSpatialAudio(ReverbEnabled, ReverbMix, ReverbPreset, SpatialWidth, SpatialModulation, SpatialPredelayMs);
		ApplyPreviewVst3IfChanged(force: true);
		await AnalyzeAllAsync();
		StatusText = $"{FileName} · {_sampleRate}Hz · {_channels}声道 · {DurationSeconds:F1}s";
	}

	public async Task AnalyzeAllAsync()
	{
		if (_samples.Length == 0 || _channels <= 0)
		{
			WaveformPeaks = new ObservableCollection<WaveformRenderer.PeakPair>();
			LeftWaveformPeaks = Array.Empty<WaveformRenderer.PeakPair>();
			RightWaveformPeaks = Array.Empty<WaveformRenderer.PeakPair>();
			LeftRms = Array.Empty<float>();
			RightRms = Array.Empty<float>();
			SpectrumPoints = new ObservableCollection<SpectrumPoint>();
			PhasePoints = new ObservableCollection<PhaseAnalyzer.LissajousPoint>();
			PhaseDifference = 0.0;
			StereoCorrelation = 0.0;
			InsightAnalyzer.Reset();
			return;
		}
		float[] samples = _samples;
		int channels = _channels;
		int sampleRate = _sampleRate;
		(List<WaveformRenderer.PeakPair> peaks,
		 List<WaveformRenderer.PeakPair> leftPeaks,
		 List<WaveformRenderer.PeakPair> rightPeaks,
		 List<float> leftRms,
		 List<float> rightRms,
		 List<SpectrumPoint> spectrum,
		 List<PhaseAnalyzer.LissajousPoint> phase,
		 double phaseDiff,
		 double correlation) tuple = await Task.Run(() =>
		{
			SeedInsightAnalyzer(samples, channels, sampleRate);
			var (leftP, rightP, leftR, rightR) = WaveformRenderer.ComputeStereoPeaksAndRms(samples, channels, 1200);
			List<WaveformRenderer.PeakPair> item = leftP;
			List<SpectrumPoint> item2 = Fft.Compute(samples, sampleRate, logarithmic: true, 96);
			float[] left = ExtractChannel(samples, channels, 0);
			float[] right = ExtractChannel(samples, channels, Math.Min(1, channels - 1));
			List<PhaseAnalyzer.LissajousPoint> item3 = PhaseAnalyzer.ComputeLissajous(left, right, 1500);
			return (peaks: item, leftPeaks: leftP, rightPeaks: rightP, leftRms: leftR, rightRms: rightR, spectrum: item2, phase: item3, phaseDiff: PhaseAnalyzer.PhaseDifferenceDegrees(left, right), correlation: PhaseAnalyzer.StereoCorrelation(left, right));
		});
		WaveformPeaks = new ObservableCollection<WaveformRenderer.PeakPair>(tuple.peaks);
		LeftWaveformPeaks = tuple.leftPeaks;
		RightWaveformPeaks = tuple.rightPeaks;
		LeftRms = tuple.leftRms;
		RightRms = tuple.rightRms;
		SpectrumPoints = new ObservableCollection<SpectrumPoint>(tuple.spectrum);
		PhasePoints = new ObservableCollection<PhaseAnalyzer.LissajousPoint>(tuple.phase);
		PhaseDifference = tuple.phaseDiff;
		StereoCorrelation = tuple.correlation;
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

	private void SyncPreviewParametricEq()
	{
		List<EchoBiquadFilter>? activeSnapshot = null;
		double preampDb = 0.0;
		lock (_echoEqLock)
		{
			if (_echoEqEnabled)
			{
				int rate = _sampleRate > 0 ? _sampleRate : 48000;
				activeSnapshot = CloneEchoBands(EchoEqBands, rate);
				preampDb = _echoEqPreampDb;
			}
		}
		Preview.SetParametricEq(activeSnapshot, preampDb);
	}

	[RelayCommand]
	private async Task ApplyDspAsync()
	{
		await ApplyEditAsync((float[] s) =>
		{
			float[] working = (float[])s.Clone();
			if (EchoEqEnabled)
			{
				EchoEqProcessor? parametricEq = CreateActiveEchoEqProcessor(_sampleRate);
				parametricEq?.ProcessInterleaved(working, 0, working.Length, _channels);
			}
			if (ReverbEnabled)
			{
				SpatialAudioEffect spatialAudioEffect = new SpatialAudioEffect(_sampleRate);
				spatialAudioEffect.SetPreset(ReverbPreset);
				spatialAudioEffect.WetMix = ReverbMix;
				spatialAudioEffect.Width = SpatialWidth;
				spatialAudioEffect.Modulation = SpatialModulation;
				spatialAudioEffect.PredelayMs = SpatialPredelayMs;
				spatialAudioEffect.ProcessInterleaved(working, _channels);
			}
			if (Math.Abs(PitchSemitones) > 0.01)
			{
				PitchShifter shifter = new PitchShifter();
				working = ApplyPerChannel(working, _channels, (float[] ch) => shifter.PitchShift(ch, PitchSemitones));
			}
			if (Math.Abs(SpeedRatio - 1.0) > 0.01)
			{
				PitchShifter shifter2 = new PitchShifter();
				working = ApplyPerChannel(working, _channels, (float[] ch) => shifter2.TimeStretch(ch, SpeedRatio));
			}
			foreach (Vst3PluginState vst3Plugin in Vst3Plugins)
			{
				if (vst3Plugin != null && vst3Plugin.IsActive)
				{
					Vst3SimulatedProcessor.Process(working, _channels, new Vst3Parameters(vst3Plugin.Drive, vst3Plugin.Tone, vst3Plugin.Mix));
				}
			}
			return working;
		});
		StatusText = "已应用 DSP 效果（含参数均衡与 VST3 插件）";
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
		Preview.SetEqGains(new double[10]);
		SyncPreviewParametricEq();
		Preview.SetSpatialAudio(ReverbEnabled, ReverbMix, ReverbPreset, SpatialWidth, SpatialModulation, SpatialPredelayMs);
		ApplyPreviewVst3IfChanged(force: true);
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
		Preview.SetEqGains(new double[10]);
		SyncPreviewParametricEq();
		Preview.SetSpatialAudio(ReverbEnabled, ReverbMix, ReverbPreset, SpatialWidth, SpatialModulation, SpatialPredelayMs);
		ApplyPreviewVst3IfChanged(force: true);
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
