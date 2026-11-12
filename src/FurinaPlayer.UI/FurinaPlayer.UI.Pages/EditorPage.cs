using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using FurinaPlayer.UI.Controls;
using FurinaPlayer.UI.Helpers;
using FurinaPlayer.UI.ViewModels;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Markup;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.UI.Xaml.Shapes;
using SkiaSharp;
using SkiaSharp.Views.Windows;
using SonicWave.Audio.Dsp;
using SonicWave.Core.Models;
using SonicWave.Core.Services;
using WinRT;
using WinRT.Interop;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.UI;

namespace FurinaPlayer.UI.Pages;

[WinRTRuntimeClassName("Microsoft.UI.Xaml.IUIElementOverrides")]
[WinRTExposedType(typeof(EditorPageWinRTTypeDetails))]
public sealed class EditorPage : Page, IComponentConnector
{
	private readonly DispatcherTimer _previewTimer;

	private readonly PanelLayoutController _layout;

	private bool _previewDragging;

	private bool _analysisBusy;

	private long _lastAnalysisMs = -1L;

	private Vst3EditorWindow? _editorVstWindow;

	private SKXamlCanvas? _ambientCausticCanvas;

	private static string? _cachedWallpaperPath;

	private static DateTime _cachedWallpaperWriteUtc;

	private static bool _cachedWallpaperIsDark;

	private static readonly ConditionalWeakTable<DependencyObject, string> _elementContrastTiers = new();

	private bool _unifiedEditorLayoutInitialized;

	private string _currentEditorCategory = "insight";

	private Border? _editorSidebarBorder;

	private Grid? _workbenchShell;

	private ScrollViewer? _editorWorkbenchHost;

	private InsightProWorkbenchControl? _insightProControl;

	private Grid? _insightProHost;

	private ParametricEqWorkbenchControl? _editorParamEqControl;

	private Grid? _paramEqHost;

	private bool _suppressParamEqEvents;

	private Grid? _waveformSectionGrid;

	private Border? _waveformCardBorder;

	private readonly List<Button> _editorSidebarNavButtons = new();

	private readonly List<TextBlock> _editorSidebarSectionHeaders = new();

	private TextBlock? _editorSidebarBadgeTb;

	private TextBlock? _editorSidebarTitleTb;

	private TextBlock? _editorSidebarSubtitleTb;

	private Border? _editorSidebarDivider;

	private bool? _lastPreviewButtonPlaying;

	private sealed class VstSwitchTracker
	{
		public Vst3PluginState? Plugin;
		public bool LastEnabled;
		public bool SuppressNext;
	}

	private readonly ConditionalWeakTable<ToggleSwitch, VstSwitchTracker> _vstSwitchTrackers = new();

	private bool _suppressVstContainerEvents;

	private readonly Dictionary<string, FrameworkElement> _editorCategoryViews = new(StringComparer.OrdinalIgnoreCase);

	[GeneratedCode("Microsoft.UI.Xaml.Markup.Compiler", " 3.0.0.2409")]
	private Canvas PanelCanvas;

	[GeneratedCode("Microsoft.UI.Xaml.Markup.Compiler", " 3.0.0.2409")]
	private Border SpectrumPanel;

	[GeneratedCode("Microsoft.UI.Xaml.Markup.Compiler", " 3.0.0.2409")]
	private Border PhasePanel;

	[GeneratedCode("Microsoft.UI.Xaml.Markup.Compiler", " 3.0.0.2409")]
	private Border InfoPanel;

	[GeneratedCode("Microsoft.UI.Xaml.Markup.Compiler", " 3.0.0.2409")]
	private Border EqPanel;

	[GeneratedCode("Microsoft.UI.Xaml.Markup.Compiler", " 3.0.0.2409")]
	private Border EffectsPanel;

	[GeneratedCode("Microsoft.UI.Xaml.Markup.Compiler", " 3.0.0.2409")]
	private Border FadePanel;

	[GeneratedCode("Microsoft.UI.Xaml.Markup.Compiler", " 3.0.0.2409")]
	private Border VstPanel;

	[GeneratedCode("Microsoft.UI.Xaml.Markup.Compiler", " 3.0.0.2409")]
	private Grid VstHeader;

	[GeneratedCode("Microsoft.UI.Xaml.Markup.Compiler", " 3.0.0.2409")]
	private Grid VstResizeHandle;

	[GeneratedCode("Microsoft.UI.Xaml.Markup.Compiler", " 3.0.0.2409")]
	private ListView EditorVstList;

	[GeneratedCode("Microsoft.UI.Xaml.Markup.Compiler", " 3.0.0.2409")]
	private Grid FadeHeader;

	[GeneratedCode("Microsoft.UI.Xaml.Markup.Compiler", " 3.0.0.2409")]
	private Grid FadeResizeHandle;

	[GeneratedCode("Microsoft.UI.Xaml.Markup.Compiler", " 3.0.0.2409")]
	private Grid EffectsHeader;

	[GeneratedCode("Microsoft.UI.Xaml.Markup.Compiler", " 3.0.0.2409")]
	private Grid EffectsResizeHandle;

	[GeneratedCode("Microsoft.UI.Xaml.Markup.Compiler", " 3.0.0.2409")]
	private ComboBox ReverbPresetCombo;

	[GeneratedCode("Microsoft.UI.Xaml.Markup.Compiler", " 3.0.0.2409")]
	private Grid EqHeader;

	[GeneratedCode("Microsoft.UI.Xaml.Markup.Compiler", " 3.0.0.2409")]
	private EqualizerControl EditorEq;

	[GeneratedCode("Microsoft.UI.Xaml.Markup.Compiler", " 3.0.0.2409")]
	private Grid EqResizeHandle;

	[GeneratedCode("Microsoft.UI.Xaml.Markup.Compiler", " 3.0.0.2409")]
	private Grid InfoHeader;

	[GeneratedCode("Microsoft.UI.Xaml.Markup.Compiler", " 3.0.0.2409")]
	private Grid InfoResizeHandle;

	[GeneratedCode("Microsoft.UI.Xaml.Markup.Compiler", " 3.0.0.2409")]
	private TextBlock SampleInfoText;

	[GeneratedCode("Microsoft.UI.Xaml.Markup.Compiler", " 3.0.0.2409")]
	private Grid PhaseHeader;

	[GeneratedCode("Microsoft.UI.Xaml.Markup.Compiler", " 3.0.0.2409")]
	private PhaseAnalyzerControl Phase;

	[GeneratedCode("Microsoft.UI.Xaml.Markup.Compiler", " 3.0.0.2409")]
	private Grid PhaseResizeHandle;

	[GeneratedCode("Microsoft.UI.Xaml.Markup.Compiler", " 3.0.0.2409")]
	private Grid SpectrumHeader;

	[GeneratedCode("Microsoft.UI.Xaml.Markup.Compiler", " 3.0.0.2409")]
	private SpectrumAnalyzer Spectrum;

	[GeneratedCode("Microsoft.UI.Xaml.Markup.Compiler", " 3.0.0.2409")]
	private Grid SpectrumResizeHandle;

	[GeneratedCode("Microsoft.UI.Xaml.Markup.Compiler", " 3.0.0.2409")]
	private Slider SelEndSlider;

	[GeneratedCode("Microsoft.UI.Xaml.Markup.Compiler", " 3.0.0.2409")]
	private TextBlock SelEndText;

	[GeneratedCode("Microsoft.UI.Xaml.Markup.Compiler", " 3.0.0.2409")]
	private Slider SelStartSlider;

	[GeneratedCode("Microsoft.UI.Xaml.Markup.Compiler", " 3.0.0.2409")]
	private TextBlock SelStartText;

	[GeneratedCode("Microsoft.UI.Xaml.Markup.Compiler", " 3.0.0.2409")]
	private WaveformControl Waveform;

	[GeneratedCode("Microsoft.UI.Xaml.Markup.Compiler", " 3.0.0.2409")]
	private Slider PreviewSlider;

	[GeneratedCode("Microsoft.UI.Xaml.Markup.Compiler", " 3.0.0.2409")]
	private TextBlock PreviewTimeText;

	[GeneratedCode("Microsoft.UI.Xaml.Markup.Compiler", " 3.0.0.2409")]
	private Button PreviewPlayButton;

	[GeneratedCode("Microsoft.UI.Xaml.Markup.Compiler", " 3.0.0.2409")]
	private bool _contentLoaded;

	public EditorViewModel ViewModel { get; set; }

	public EditorPage()
	{
		InitializeComponent();
		SelEndSlider.Value = 100.0;
		_previewTimer = new DispatcherTimer
		{
			Interval = TimeSpan.FromMilliseconds(33.0)
		};
		_previewTimer.Tick += OnPreviewTick;
		_layout = new PanelLayoutController(PanelCanvas, 260.0, 150.0, 1500.0);
		SetupUnifiedEditorLayout();
		ApplyEditorLiquidGlass();
		Loaded += (s, e) =>
		{
			UpgradeEditorChromeIcons();
			ApplyEditorLiquidGlass();
		};
		ActualThemeChanged += (s, e) => ApplyEditorLiquidGlass();
		Unloaded += (object _, RoutedEventArgs _) =>
		{
			_previewTimer.Stop();
			ViewModel?.Preview.Stop();
		};
	}

	public EditorPage(EditorViewModel viewModel)
		: this()
	{
		ViewModel = viewModel;
		DataContext = viewModel;
	}

	protected override async void OnNavigatedTo(NavigationEventArgs e)
	{
		base.OnNavigatedTo(e);
		if (e.Parameter is EditorViewModel editorViewModel)
		{
			ViewModel = editorViewModel;
			DataContext = editorViewModel;
		}
		if (ViewModel != null)
		{
			if (ViewModel.EchoEqBands.Count == 0)
			{
				ViewModel.InitializeEchoEqBands();
			}
			EditorEq.Gains = ViewModel.Equalizer.Gains;
			if (_editorParamEqControl != null)
			{
				_suppressParamEqEvents = true;
				try
				{
					_editorParamEqControl.SetBands(ViewModel.EchoEqBands, ViewModel.EchoEqPreampDb, ViewModel.EchoEqEnabled);
				}
				finally
				{
					_suppressParamEqEvents = false;
				}
			}
		}
		SetupEditorVstListStyling();
		SetupUnifiedEditorLayout();
		UpdateInsightUi();
		UpgradeEditorChromeIcons();
		ApplyEditorLiquidGlass();
		ReverbPresetCombo.SelectionChanged += (object _, SelectionChangedEventArgs _) =>
		{
			if (ReverbPresetCombo.SelectedItem is ComboBoxItem { Content: string content })
			{
				ViewModel.ReverbPreset = content;
			}
		};
		if (string.IsNullOrEmpty(ViewModel.FilePath))
		{
			await ViewModel.TryAutoLoadCurrentTrackAsync();
			if (!string.IsNullOrEmpty(ViewModel.FilePath))
			{
				RefreshVisuals();
				SampleInfoText.Text = "采样率 " + ViewModel.SampleRateText + " · 声道: " + ViewModel.ChannelsText;
			}
		}
	}

	private void OnPanelEditClick(object sender, RoutedEventArgs e)
	{
	}

	private void OnResetLayoutClick(object sender, RoutedEventArgs e)
	{
		SelectEditorCategory("insight");
	}

	private void RestoreLayouts()
	{
		Dictionary<string, double[]> layouts = ViewModel.LoadPanelLayouts();
		RestorePanel(SpectrumPanel, "editor.spectrum", layouts, 16.0, 16.0, 720.0, 250.0);
		RestorePanel(PhasePanel, "editor.phase", layouts, 752.0, 16.0, 440.0, 250.0);
		RestorePanel(InfoPanel, "editor.info", layouts, 1208.0, 16.0, 292.0, 250.0);
		RestorePanel(EqPanel, "editor.eq", layouts, 16.0, 282.0, 720.0, 280.0);
		RestorePanel(EffectsPanel, "editor.effects", layouts, 752.0, 282.0, 440.0, 280.0);
		RestorePanel(FadePanel, "editor.fade", layouts, 1208.0, 282.0, 292.0, 280.0);
		RestorePanel(VstPanel, "editor.vst", layouts, 16.0, 578.0, 720.0, 300.0);
	}

	private void RestorePanel(Border panel, string key, Dictionary<string, double[]> layouts, double defLeft, double defTop, double defWidth, double defHeight)
	{
		if (layouts != null && layouts.TryGetValue(key, out double[] value) && value.Length == 4)
		{
			Canvas.SetLeft(panel, value[0]);
			Canvas.SetTop(panel, value[1]);
			panel.Width = value[2];
			panel.Height = value[3];
		}
		else
		{
			ResetPanelLayout(panel, defLeft, defTop, defWidth, defHeight);
		}
	}

	private void SaveLayouts()
	{
		if (ViewModel != null)
		{
			ViewModel.SavePanelLayouts(new Dictionary<string, double[]>
			{
				["editor.spectrum"] = PanelState(SpectrumPanel),
				["editor.phase"] = PanelState(PhasePanel),
				["editor.info"] = PanelState(InfoPanel),
				["editor.eq"] = PanelState(EqPanel),
				["editor.effects"] = PanelState(EffectsPanel),
				["editor.fade"] = PanelState(FadePanel),
				["editor.vst"] = PanelState(VstPanel)
			});
		}
	}

	private static double[] PanelState(Border panel)
	{
		return new double[4]
		{
			Canvas.GetLeft(panel),
			Canvas.GetTop(panel),
			panel.Width,
			panel.Height
		};
	}

	private static void ResetPanelLayout(Border panel, double left, double top, double width, double height)
	{
		Canvas.SetLeft(panel, left);
		Canvas.SetTop(panel, top);
		panel.Width = width;
		panel.Height = height;
	}

	private async void OnUndoClick(object sender, RoutedEventArgs e)
	{
		await ViewModel.UndoCommand.ExecuteAsync(null);
		RefreshVisuals();
	}

	private async void OnRedoClick(object sender, RoutedEventArgs e)
	{
		await ViewModel.RedoCommand.ExecuteAsync(null);
		RefreshVisuals();
	}

	private void OnUndoAccelerator(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
	{
		args.Handled = true;
		OnUndoClick(sender, new RoutedEventArgs());
	}

	private void OnRedoAccelerator(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
	{
		args.Handled = true;
		OnRedoClick(sender, new RoutedEventArgs());
	}

	private async void OnLoadCurrentClick(object sender, RoutedEventArgs e)
	{
		await ViewModel.TryAutoLoadCurrentTrackAsync();
		if (string.IsNullOrEmpty(ViewModel.FilePath))
		{
			ViewModel.StatusText = "当前没有正在播放的曲目";
			return;
		}
		RefreshVisuals();
		SampleInfoText.Text = "采样率 " + ViewModel.SampleRateText + " · 声道: " + ViewModel.ChannelsText;
	}

	private async void OnOpenClick(object sender, RoutedEventArgs e)
	{
		_ = 1;
		try
		{
			FileOpenPicker fileOpenPicker = new FileOpenPicker
			{
				FileTypeFilter = { ".wav", ".flac", ".mp3", ".aiff", ".m4a" }
			};
			nint windowHandle = WindowNative.GetWindowHandle(WindowManager.CurrentWindow);
			InitializeWithWindow.Initialize(fileOpenPicker, windowHandle);
			StorageFile storageFile = await fileOpenPicker.PickSingleFileAsync();
			if (storageFile != null)
			{
				await LoadFileAsync(storageFile.Path);
			}
		}
		catch (Exception ex)
		{
			if (ViewModel != null)
			{
				ViewModel.StatusText = "打开文件失败：" + ex.Message;
			}
		}
	}

	public async Task LoadFileAsync(string path)
	{
		await ViewModel.LoadFileAsync(path);
		RefreshVisuals();
		SampleInfoText.Text = "采样率 " + ViewModel.SampleRateText + " · 声道: " + ViewModel.ChannelsText;
	}

	private void UpdateInsightUi()
	{
		if (ViewModel == null || _insightProControl == null)
		{
			return;
		}
		_insightProControl.UpdateSnapshot(ViewModel.GetCurrentInsightSnapshot());
	}

	private void RefreshVisuals()
	{
		Waveform.SetAuditionData(
			ViewModel.LeftWaveformPeaks,
			ViewModel.RightWaveformPeaks,
			ViewModel.LeftRms,
			ViewModel.RightRms,
			ViewModel.SelectionStartSeconds / Math.Max(0.001, ViewModel.DurationSeconds),
			ViewModel.SelectionEndSeconds / Math.Max(0.001, ViewModel.DurationSeconds),
			0.0,
			"#00e5ff");
		UpdateInsightUi();
		if (_editorParamEqControl != null && !_editorParamEqControl.MixAssistant.IsLearning)
		{
			float[] bands64 = new float[64];
			if (ViewModel.TryGetCurrentSpectrum64(bands64))
			{
				_editorParamEqControl.UpdateSpectrum(bands64);
			}
		}
		SelStartSlider.Value = ((ViewModel.DurationSeconds > 0.0) ? (ViewModel.SelectionStartSeconds / ViewModel.DurationSeconds * 100.0) : 0.0);
		SelEndSlider.Value = ((ViewModel.DurationSeconds > 0.0) ? (ViewModel.SelectionEndSeconds / ViewModel.DurationSeconds * 100.0) : 100.0);
		UpdateSelectionTexts();
		ApplyEditorTextContrast(ResolveIsEditorDark());
	}

	private void OnSelectionChanged(object sender, RangeBaseValueChangedEventArgs e)
	{
		if (ViewModel != null && !(ViewModel.DurationSeconds <= 0.0))
		{
			double num = SelStartSlider.Value / 100.0 * ViewModel.DurationSeconds;
			double num2 = SelEndSlider.Value / 100.0 * ViewModel.DurationSeconds;
			if (num2 < num)
			{
				num2 = num;
			}
			ViewModel.SetSelection(num, num2);
			Waveform.SetSelection(num / ViewModel.DurationSeconds, num2 / ViewModel.DurationSeconds);
			UpdateSelectionTexts();
		}
	}

	private void UpdateSelectionTexts()
	{
		SelStartText.Text = $"{ViewModel.SelectionStartSeconds:F1}s";
		SelEndText.Text = $"{ViewModel.SelectionEndSeconds:F1}s";
	}

	private async void OnTrimClick(object sender, RoutedEventArgs e)
	{
		await ViewModel.TrimSelectionCommand.ExecuteAsync(null);
		RefreshVisuals();
	}

	private async void OnCutClick(object sender, RoutedEventArgs e)
	{
		await ViewModel.CutSelectionCommand.ExecuteAsync(null);
		RefreshVisuals();
	}

	private async void OnFadeClick(object sender, RoutedEventArgs e)
	{
		await ViewModel.ApplyFadeCommand.ExecuteAsync(null);
		RefreshVisuals();
	}

	private async void OnNormalizeClick(object sender, RoutedEventArgs e)
	{
		await ViewModel.NormalizeCommand.ExecuteAsync(null);
		RefreshVisuals();
	}

	private void OnEditorEqGainsChanged(double[] gains)
	{
		if (ViewModel != null)
		{
			ViewModel.ApplyPreviewEq(gains);
		}
	}

	private async void OnDspClick(object sender, RoutedEventArgs e)
	{
		await ApplyOfflineDspAsync();
	}

	private async Task ApplyOfflineDspAsync()
	{
		if (ViewModel == null)
		{
			return;
		}
		if (_editorParamEqControl != null)
		{
			ViewModel.ApplyPreviewParametricEq(
				_editorParamEqControl.Bands,
				_editorParamEqControl.PreampDb,
				ViewModel.EchoEqEnabled);
		}
		await ViewModel.ApplyDspCommand.ExecuteAsync(null);
		if (_editorParamEqControl != null)
		{
			_editorParamEqControl.ResetToFlat();
		}
		else
		{
			ViewModel.InitializeEchoEqBands();
			ViewModel.ApplyPreviewParametricEq(ViewModel.EchoEqBands, 0.0, ViewModel.EchoEqEnabled);
		}
		RefreshVisuals();
	}

	private async void OnExportClick(object sender, RoutedEventArgs e)
	{
		_ = 1;
		try
		{
			FileSavePicker fileSavePicker = new FileSavePicker
			{
				FileTypeChoices = { 
				{
					"WAV 音频",
					(IList<string>)new List<string> { ".wav" }
				} },
				SuggestedFileName = "edited.wav"
			};
			nint windowHandle = WindowNative.GetWindowHandle(WindowManager.CurrentWindow);
			InitializeWithWindow.Initialize(fileSavePicker, windowHandle);
			StorageFile storageFile = await fileSavePicker.PickSaveFileAsync();
			if (storageFile != null)
			{
				await ViewModel.ExportCommand.ExecuteAsync(storageFile.Path);
			}
		}
		catch (Exception ex)
		{
			if (ViewModel != null)
			{
				ViewModel.StatusText = "导出失败：" + ex.Message;
			}
		}
	}

	private void OnPreviewPlayClick(object sender, RoutedEventArgs e)
	{
		if (ViewModel != null && !(ViewModel.DurationSeconds <= 0.0))
		{
			if (ViewModel.Preview.IsPlaying)
			{
				ViewModel.Preview.Pause();
			}
			else
			{
				ViewModel.Preview.Play();
			}
			UpdatePreviewButton();
			_previewTimer.Start();
		}
	}

	private void OnPreviewStopClick(object sender, RoutedEventArgs e)
	{
		ViewModel?.Preview.Stop();
		UpdatePreviewButton();
		OnPreviewTick(null, null);
	}

	private void OnPreviewSliderChanged(object sender, RangeBaseValueChangedEventArgs e)
	{
		if (_previewDragging && ViewModel != null && !(ViewModel.Preview.Duration.TotalSeconds <= 0.0))
		{
			ViewModel.Preview.Position = TimeSpan.FromSeconds(e.NewValue / 1000.0 * ViewModel.Preview.Duration.TotalSeconds);
		}
	}

	private void OnPreviewPointerPressed(object sender, PointerRoutedEventArgs e)
	{
		_previewDragging = true;
	}

	private void OnPreviewPointerReleased(object sender, PointerRoutedEventArgs e)
	{
		_previewDragging = false;
	}

	private void OnPreviewPointerMoved(object sender, PointerRoutedEventArgs e)
	{
		if (ViewModel != null && !(ViewModel.Preview.Duration.TotalSeconds <= 0.0) && sender is Slider { ActualWidth: not (<=0.0) } slider)
		{
			TimeSpan t = TimeSpan.FromSeconds(Math.Clamp(e.GetCurrentPoint(slider).Position.X / slider.ActualWidth, 0.0, 1.0) * ViewModel.Preview.Duration.TotalSeconds);
			ToolTipService.SetToolTip(slider, NowPlayingPage.FormatTime(t) + " / " + NowPlayingPage.FormatTime(ViewModel.Preview.Duration));
		}
	}

	private void OnPreviewPointerExited(object sender, PointerRoutedEventArgs e)
	{
		if (sender is Slider element)
		{
			ToolTipService.SetToolTip(element, null);
		}
	}

	private void OnPreviewTick(object? sender, object e)
	{
		if (ViewModel == null || ViewModel.DurationSeconds <= 0.0)
		{
			return;
		}
		TimeSpan position = ViewModel.Preview.Position;
		TimeSpan duration = ViewModel.Preview.Duration;
		double num = ((duration.TotalSeconds > 0.0) ? (position.TotalSeconds / duration.TotalSeconds) : 0.0);
		if (!_previewDragging)
		{
			PreviewSlider.Value = Math.Clamp(num * 1000.0, 0.0, 1000.0);
		}
		PreviewTimeText.Text = $"{position:mm\\:ss} / {duration:mm\\:ss}";
		Waveform.SetPlayhead(Math.Clamp(num, 0.0, 1.0));
		UpdatePreviewButton();
		if (!ViewModel.Preview.IsPlaying || _analysisBusy)
		{
			return;
		}
		_analysisBusy = true;
		long posMs = (long)position.TotalMilliseconds;
		if (posMs == _lastAnalysisMs)
		{
			_analysisBusy = false;
			return;
		}
		_lastAnalysisMs = posMs;
		Task.Run(() =>
		{
			float[] bands64 = new float[64];
			InsightProSnapshot snapshot = ViewModel.AdvancePreviewInsight(TimeSpan.FromMilliseconds(posMs), duration, bands64);
			return (Snapshot: snapshot, Bands64: bands64);
		}).ContinueWith((Task<(InsightProSnapshot Snapshot, float[] Bands64)> t) =>
		{
			_analysisBusy = false;
			if (t.IsCompletedSuccessfully && ViewModel != null && ViewModel.Preview.IsPlaying)
			{
				(InsightProSnapshot snapshot, float[] bands64) = t.Result;
				_insightProControl?.UpdateSnapshot(snapshot);
				_editorParamEqControl?.UpdateSpectrum(bands64);
				ViewModel.UpdateAnalysisScalars(snapshot.PhaseDifferenceDeg, snapshot.StereoCorrelation);
			}
		}, TaskScheduler.FromCurrentSynchronizationContext());
	}

	private void UpdatePreviewButton()
	{
		if (PreviewPlayButton == null)
		{
			return;
		}
		bool isPlaying = ViewModel != null && ViewModel.Preview.IsPlaying;
		if (PreviewPlayButton.Content is FontIcon fontIcon)
		{
			fontIcon.Glyph = (isPlaying ? "\ue769" : "\ue768");
			_lastPreviewButtonPlaying = isPlaying;
		}
		else if (_lastPreviewButtonPlaying != isPlaying)
		{
			_lastPreviewButtonPlaying = isPlaying;
			FluentIconFactory.SetButtonFluentIcon(
				PreviewPlayButton,
				isPlaying ? FluentIconKind.Pause : FluentIconKind.Play,
				null,
				20);
		}
	}

	private void SetupEditorVstListStyling()
	{
		if (EditorVstList == null) return;
		EditorVstList.Height = double.NaN;
		EditorVstList.MaxHeight = double.PositiveInfinity;
		EditorVstList.VerticalAlignment = VerticalAlignment.Stretch;
		EditorVstList.HorizontalAlignment = HorizontalAlignment.Stretch;
		if (VisualTreeHelper.GetParent(EditorVstList) is Grid parentGrid)
		{
			if (parentGrid.RowDefinitions.Count > 0)
			{
				int row = Grid.GetRow(EditorVstList);
				if (row >= 0 && row < parentGrid.RowDefinitions.Count)
				{
					parentGrid.RowDefinitions[row].Height = new GridLength(1.0, GridUnitType.Star);
				}
			}
			if (parentGrid.ColumnDefinitions.Count > 1)
			{
				foreach (UIElement child in parentGrid.Children)
				{
					if (child is FrameworkElement fe && !ReferenceEquals(fe, EditorVstList) && Grid.GetColumn(fe) == 1)
					{
						fe.Visibility = Visibility.Collapsed;
					}
				}
				Grid.SetColumnSpan(EditorVstList, 2);
			}
		}
		EditorVstList.ContainerContentChanging -= OnEditorVstContainerChanging;
		EditorVstList.ContainerContentChanging += OnEditorVstContainerChanging;
	}

	private sealed class ContrastPalette
	{
		public bool IsDark { get; }
		public Brush Primary { get; }
		public Brush Secondary { get; }
		public Brush Muted { get; }
		public Brush PhaseOverlay { get; }
		public Brush AccentWhite { get; }
		public Brush ResizeFill { get; }
		public Brush Divider { get; }

		public ContrastPalette(bool isDark)
		{
			IsDark = isDark;
			Primary = new SolidColorBrush(isDark
				? Color.FromArgb(255, 248, 250, 252)   // #F8FAFC
				: Color.FromArgb(255, 15, 23, 42));    // #0F172A
			Secondary = new SolidColorBrush(isDark
				? Color.FromArgb(255, 226, 232, 240)   // #E2E8F0
				: Color.FromArgb(255, 51, 65, 85));    // #334155
			Muted = new SolidColorBrush(isDark
				? Color.FromArgb(255, 203, 213, 225)   // #CBD5E1
				: Color.FromArgb(255, 71, 85, 105));   // #475569
			PhaseOverlay = new SolidColorBrush(Color.FromArgb(255, 226, 232, 240)); // #E2E8F0 over dark #16191d canvas
			AccentWhite = new SolidColorBrush(Colors.White);
			ResizeFill = new SolidColorBrush(isDark
				? Color.FromArgb(255, 203, 213, 225)   // #CBD5E1
				: Color.FromArgb(255, 100, 116, 139)); // #64748B
			Divider = new SolidColorBrush(isDark
				? Color.FromArgb(95, 203, 213, 225)
				: Color.FromArgb(65, 15, 23, 42));
		}
	}

	private AppSettings? GetCurrentSettings()
	{
		try
		{
			return (ViewModel?.SettingsService ?? new SettingsService()).Load();
		}
		catch
		{
			return null;
		}
	}

	private bool ResolveIsEditorDark()
	{
		try
		{
			AppSettings? settings = GetCurrentSettings();
			if (settings?.ScheduledDarkModeEnabled == true && IsLoaded && ActualTheme != ElementTheme.Default)
			{
				return ActualTheme == ElementTheme.Dark;
			}

			bool invert = LiquidGlassEngine.InvertModeEnabled;
			string mode = settings?.EchoThemeMode ?? LiquidGlassEngine.ActiveThemeMode;

			if (string.Equals(mode, "dark", StringComparison.OrdinalIgnoreCase))
			{
				return !invert;
			}
			if (string.Equals(mode, "light", StringComparison.OrdinalIgnoreCase))
			{
				return invert;
			}

			if (mode == "ambient" || mode == "acrylic" || mode == "liquid-glass")
			{
				bool baseDark = LiquidGlassEngine.ActivePreset?.IsDark ?? false;
				if (!baseDark && settings != null)
				{
					if (settings.Theme == ThemeMode.Dark)
					{
						baseDark = true;
					}
					else if (settings.WallpaperSource == WallpaperSource.LocalImage
						&& !string.IsNullOrWhiteSpace(settings.LocalWallpaperPath)
						&& LiquidGlassEngine.AcrylicOpacityScale >= 0.35
						&& IsWallpaperDarkCached(settings.LocalWallpaperPath))
					{
						baseDark = true;
					}
				}
				return invert ? !baseDark : baseDark;
			}

			if (IsLoaded && ActualTheme != ElementTheme.Default)
			{
				return ActualTheme == ElementTheme.Dark;
			}

			if (settings != null && settings.Theme != ThemeMode.System)
			{
				bool baseDark = settings.Theme == ThemeMode.Dark;
				return invert ? !baseDark : baseDark;
			}

			return LiquidGlassEngine.IsElementDarkTheme(null);
		}
		catch
		{
			return IsLoaded && ActualTheme == ElementTheme.Dark;
		}
	}

	private static bool IsWallpaperDarkCached(string path)
	{
		try
		{
			if (!File.Exists(path)) return false;
			DateTime writeUtc = File.GetLastWriteTimeUtc(path);
			if (string.Equals(_cachedWallpaperPath, path, StringComparison.OrdinalIgnoreCase)
				&& _cachedWallpaperWriteUtc == writeUtc)
			{
				return _cachedWallpaperIsDark;
			}

			using SKCodec? codec = SKCodec.Create(path);
			if (codec == null) return false;

			int maxDim = Math.Max(1, Math.Max(codec.Info.Width, codec.Info.Height));
			SKSizeI scaled = codec.GetScaledDimensions(Math.Min(1.0f, 64.0f / maxDim));
			SKImageInfo info = new SKImageInfo(Math.Max(1, scaled.Width), Math.Max(1, scaled.Height), SKColorType.Bgra8888, SKAlphaType.Premul);
			using SKBitmap bmp = new SKBitmap(info);
			SKCodecResult result = codec.GetPixels(info, bmp.GetPixels());
			if (result != SKCodecResult.Success && result != SKCodecResult.IncompleteInput)
			{
				return false;
			}

			double totalLum = 0.0;
			int count = 0;
			for (int y = 0; y < bmp.Height; y++)
			{
				for (int x = 0; x < bmp.Width; x++)
				{
					SKColor c = bmp.GetPixel(x, y);
					totalLum += (0.2126 * c.Red + 0.7152 * c.Green + 0.0722 * c.Blue) / 255.0;
					count++;
				}
			}

			bool isDark = count > 0 && (totalLum / count) < 0.45;
			_cachedWallpaperPath = path;
			_cachedWallpaperWriteUtc = writeUtc;
			_cachedWallpaperIsDark = isDark;
			return isDark;
		}
		catch
		{
			return false;
		}
	}

	private void SetupUnifiedEditorLayout()
	{
		if (_unifiedEditorLayoutInitialized)
		{
			return;
		}
		if (Content is not Grid rootGrid)
		{
			return;
		}
		_unifiedEditorLayoutInitialized = true;

		// ContentFrame already sits above MainWindow's floating player bar; reclaim vertical room for the workbench
		rootGrid.Padding = new Thickness(16.0, 6.0, 16.0, 8.0);
		rootGrid.RowSpacing = 5.0;

		// Compact Row 0 toolbar & wrap into a sleek horizontal ScrollViewer so buttons never hard-clip mid-text
		StackPanel? row0Toolbar = null;
		Border? row1PreviewBorder = null;
		Grid? row2WaveformGrid = null;
		foreach (UIElement rootChild in rootGrid.Children)
		{
			if (rootChild is StackPanel toolbar && Grid.GetRow(toolbar) == 0)
			{
				row0Toolbar = toolbar;
			}
			else if (rootChild is Border b && Grid.GetRow(b) == 1)
			{
				row1PreviewBorder = b;
			}
			else if (rootChild is Grid wfGrid && Grid.GetRow(wfGrid) == 2)
			{
				row2WaveformGrid = wfGrid;
			}
		}

		if (row0Toolbar != null)
		{
			for (int i = 0; i < row0Toolbar.Children.Count; i++)
			{
				if (row0Toolbar.Children[i] is Button btn)
				{
					bool isResetBtn = (ToolTipService.GetToolTip(btn) is string tip && tip.Contains("恢复默认位置"))
						|| (btn.Content is Panel p && p.Children.OfType<TextBlock>().Any(tb => tb.Text == "重置布局"));
					if (isResetBtn)
					{
						btn.Visibility = Visibility.Collapsed;
						if (i > 0 && row0Toolbar.Children[i - 1] is Border div && div.Width <= 2.0)
						{
							div.Visibility = Visibility.Collapsed;
						}
					}
					else
					{
						btn.Padding = new Thickness(7.0, 3.0, 7.0, 3.0);
						btn.FontSize = 11.5;
					}
				}
			}
			row0Toolbar.Spacing = 4.0;
			row0Toolbar.Padding = new Thickness(0.0, 0.0, 0.0, 0.0);
			rootGrid.Children.Remove(row0Toolbar);
			var toolbarScroll = new ScrollViewer
			{
				HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
				VerticalScrollBarVisibility = ScrollBarVisibility.Disabled,
				VerticalScrollMode = ScrollMode.Disabled,
				Content = row0Toolbar
			};
			// Only reserve bottom padding when the horizontal scrollbar is actually visible
			toolbarScroll.RegisterPropertyChangedCallback(ScrollViewer.ComputedHorizontalScrollBarVisibilityProperty, (senderObj, _) =>
			{
				if (senderObj is ScrollViewer sv)
				{
					bool barVisible = sv.ComputedHorizontalScrollBarVisibility == Visibility.Visible;
					row0Toolbar.Padding = new Thickness(0.0, 0.0, 0.0, barVisible ? 12.0 : 0.0);
				}
			});
			Grid.SetRow(toolbarScroll, 0);
			rootGrid.Children.Add(toolbarScroll);
		}

		// Fix Row 1 Preview Bar so PreviewSlider stretches flexibly instead of squishing label & time text
		if (row1PreviewBorder?.Child is Grid previewGrid && previewGrid.ColumnDefinitions.Count >= 5)
		{
			previewGrid.ColumnDefinitions[1].Width = GridLength.Auto;
			previewGrid.ColumnDefinitions[2].Width = new GridLength(1.0, GridUnitType.Star);
			foreach (UIElement child in previewGrid.Children)
			{
				if (child is TextBlock tb && Grid.GetColumn(tb) == 1)
				{
					tb.Text = "独立预览（不干扰主播放）";
					tb.TextTrimming = TextTrimming.CharacterEllipsis;
				}
			}
			if (PreviewSlider != null)
			{
				PreviewSlider.Width = double.NaN;
				PreviewSlider.MinWidth = 100.0;
				PreviewSlider.HorizontalAlignment = HorizontalAlignment.Stretch;
			}
			if (PreviewTimeText != null)
			{
				PreviewTimeText.MinWidth = 84.0;
			}
		}

		// Move Row 2 (Waveform + Selection) inside the default "waveform" sidebar workbench view so Row 3 gets full vertical space!
		if (row2WaveformGrid != null)
		{
			rootGrid.Children.Remove(row2WaveformGrid);
			if (rootGrid.RowDefinitions.Count > 2)
			{
				rootGrid.RowDefinitions[2].Height = new GridLength(0.0);
			}
			_waveformSectionGrid = row2WaveformGrid;
			foreach (UIElement wfChild in row2WaveformGrid.Children)
			{
				if (wfChild is Border wfBorder && Grid.GetRow(wfBorder) == 0)
				{
					_waveformCardBorder = wfBorder;
					wfBorder.Height = double.NaN;
					wfBorder.MinHeight = 130.0;
					wfBorder.VerticalAlignment = VerticalAlignment.Stretch;
				}
				else if (wfChild is Grid selGrid && Grid.GetRow(selGrid) == 1)
				{
					foreach (UIElement selColChild in selGrid.Children)
					{
						if (selColChild is StackPanel sp)
						{
							foreach (UIElement spChild in sp.Children)
							{
								if (spChild is Slider s)
								{
									s.Width = 160.0;
								}
							}
						}
					}
				}
			}
			if (row2WaveformGrid.RowDefinitions.Count >= 2)
			{
				row2WaveformGrid.RowDefinitions[0].Height = new GridLength(1.0, GridUnitType.Star);
				row2WaveformGrid.RowDefinitions[1].Height = GridLength.Auto;
			}
		}

		// Collapse legacy Row 3 ScrollViewer containing PanelCanvas
		ScrollViewer? legacyScrollViewer = (PanelCanvas?.Parent as ScrollViewer)
			?? rootGrid.Children.OfType<ScrollViewer>().FirstOrDefault(sv => Grid.GetRow(sv) == 3);
		if (legacyScrollViewer != null)
		{
			legacyScrollViewer.Visibility = Visibility.Collapsed;
		}

		// Detach all 7 panels from PanelCanvas and strip floating canvas chrome
		Border?[] allPanels = new Border?[]
		{
			SpectrumPanel,
			PhasePanel,
			InfoPanel,
			EqPanel,
			EffectsPanel,
			FadePanel,
			VstPanel
		};
		foreach (Border? panel in allPanels)
		{
			if (panel == null) continue;
			PanelCanvas?.Children.Remove(panel);
			panel.ClearValue(Canvas.LeftProperty);
			panel.ClearValue(Canvas.TopProperty);
			panel.Width = double.NaN;
			panel.Height = double.NaN;
			panel.HorizontalAlignment = HorizontalAlignment.Stretch;
			panel.VerticalAlignment = VerticalAlignment.Stretch;
		}

		// Hide pencil resize buttons in headers and clear drag tooltips
		Grid?[] allHeaders = new Grid?[]
		{
			SpectrumHeader,
			PhaseHeader,
			InfoHeader,
			EqHeader,
			EffectsHeader,
			FadeHeader,
			VstHeader
		};
		foreach (Grid? header in allHeaders)
		{
			if (header == null) continue;
			ToolTipService.SetToolTip(header, null);
			foreach (UIElement child in header.Children)
			{
				if (child is Button btn && btn.Tag is string)
				{
					btn.Visibility = Visibility.Collapsed;
				}
			}
		}

		// Collapse all 7 corner resize handles
		Grid?[] resizeHandles = new Grid?[]
		{
			SpectrumResizeHandle,
			PhaseResizeHandle,
			InfoResizeHandle,
			EqResizeHandle,
			EffectsResizeHandle,
			FadeResizeHandle,
			VstResizeHandle
		};
		foreach (Grid? handle in resizeHandles)
		{
			if (handle != null)
			{
				handle.Visibility = Visibility.Collapsed;
			}
		}

		// Inset PhasePanel bottom-right stats StackPanel cleanly inside the card corner
		if (PhasePanel?.Child is Grid phaseGrid)
		{
			foreach (UIElement child in phaseGrid.Children)
			{
				if (child is StackPanel statsStack
					&& Grid.GetRow(statsStack) == 1
					&& statsStack.HorizontalAlignment == HorizontalAlignment.Right
					&& statsStack.VerticalAlignment == VerticalAlignment.Bottom)
				{
					statsStack.Margin = new Thickness(0.0, 0.0, 10.0, 8.0);
					break;
				}
			}
		}

		// Prevent nested ScrollViewer wheel-trapping inside EffectsPanel and FadePanel
		DisableInnerPanelScroll(EffectsPanel);
		DisableInnerPanelScroll(FadePanel);

		// Build 2-column unified workbench in Row 3: 228px left sidebar + * right workbench
		Grid workbenchShell = new Grid
		{
			ColumnSpacing = 14.0
		};
		_workbenchShell = workbenchShell;
		workbenchShell.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(228.0, GridUnitType.Pixel) });
		workbenchShell.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.0, GridUnitType.Star) });
		Grid.SetRow(workbenchShell, 3);

		FrameworkElement sidebar = CreateEditorSidebar();
		Grid.SetColumn(sidebar, 0);
		workbenchShell.Children.Add(sidebar);

		_editorWorkbenchHost = new ScrollViewer
		{
			VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
			HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
			HorizontalScrollMode = ScrollMode.Disabled,
			Padding = new Thickness(2.0, 0.0, 4.0, 4.0),
			Background = new SolidColorBrush(Colors.Transparent)
		};
		Grid.SetColumn(_editorWorkbenchHost, 1);
		workbenchShell.Children.Add(_editorWorkbenchHost);

		rootGrid.Children.Add(workbenchShell);

		SelectEditorCategory("insight");
	}

	private static void DisableInnerPanelScroll(Border? panel)
	{
		if (panel?.Child is Grid innerGrid)
		{
			foreach (UIElement child in innerGrid.Children)
			{
				if (child is ScrollViewer sv)
				{
					sv.VerticalScrollBarVisibility = ScrollBarVisibility.Disabled;
					sv.VerticalScrollMode = ScrollMode.Disabled;
				}
			}
		}
	}

	private FrameworkElement CreateEditorSidebar()
	{
		bool isDark = ResolveIsEditorDark();
		var border = new Border
		{
			Width = 228.0,
			HorizontalAlignment = HorizontalAlignment.Stretch,
			CornerRadius = new CornerRadius(14.0),
			BorderThickness = new Thickness(1.2),
			Padding = new Thickness(10.0, 12.0, 10.0, 10.0)
		};
		_editorSidebarBorder = border;
		LiquidGlassEngine.AttachInteractiveLiquidGlass(border, LiquidGlassVariant.FloatingBar, isDark, enablePointerTracking: true);

		var sidebarGrid = new Grid();
		sidebarGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
		sidebarGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1.0, GridUnitType.Star) });

		var headerStack = new StackPanel
		{
			Spacing = 2.0,
			Margin = new Thickness(6.0, 0.0, 6.0, 8.0)
		};
		_editorSidebarBadgeTb = new TextBlock
		{
			Text = "EDITOR WORKBENCH",
			FontSize = 9.5,
			FontWeight = Microsoft.UI.Text.FontWeights.Bold,
			CharacterSpacing = 80
		};
		headerStack.Children.Add(_editorSidebarBadgeTb);

		_editorSidebarTitleTb = new TextBlock
		{
			Text = "音频工作台",
			FontSize = 15.5,
			FontWeight = Microsoft.UI.Text.FontWeights.ExtraBold
		};
		headerStack.Children.Add(_editorSidebarTitleTb);

		_editorSidebarSubtitleTb = new TextBlock
		{
			Text = "分析 · 调音 · 插件",
			FontSize = 10.5
		};
		headerStack.Children.Add(_editorSidebarSubtitleTb);

		_editorSidebarDivider = new Border
		{
			Height = 1.0,
			Margin = new Thickness(0.0, 6.0, 0.0, 0.0)
		};
		headerStack.Children.Add(_editorSidebarDivider);

		Grid.SetRow(headerStack, 0);
		sidebarGrid.Children.Add(headerStack);

		var sv = new ScrollViewer
		{
			VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
			HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
			Padding = new Thickness(0.0, 0.0, 2.0, 0.0)
		};
		var itemsStack = new StackPanel { Spacing = 3.0 };
		_editorSidebarNavButtons.Clear();
		_editorSidebarSectionHeaders.Clear();

		void AddNavSectionHeader(string title, bool isFirst = false)
		{
			var headerTb = new TextBlock
			{
				Text = title,
				FontSize = 10.5,
				FontWeight = Microsoft.UI.Text.FontWeights.Bold,
				Margin = new Thickness(8.0, isFirst ? 2.0 : 8.0, 0.0, 2.0)
			};
			_editorSidebarSectionHeaders.Add(headerTb);
			itemsStack.Children.Add(headerTb);
		}

		Button CreateNavButton(string key, string iconGlyph, string title, string subtitle)
		{
			var btn = new Button
			{
				Tag = key,
				HorizontalAlignment = HorizontalAlignment.Stretch,
				HorizontalContentAlignment = HorizontalAlignment.Stretch,
				MinHeight = 0.0,
				Padding = new Thickness(10.0, 6.5, 10.0, 6.5),
				Margin = new Thickness(0.0, 0.5, 0.0, 0.5),
				CornerRadius = new CornerRadius(9.0),
				Background = new SolidColorBrush(Colors.Transparent),
				BorderThickness = new Thickness(0.0)
			};
			ToolTipService.SetToolTip(btn, $"{title} · {subtitle}");

			var rowGrid = new Grid { ColumnSpacing = 10.0 };
			rowGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
			rowGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.0, GridUnitType.Star) });

			var icon = new FontIcon
			{
				Glyph = iconGlyph,
				FontSize = 15.0,
				VerticalAlignment = VerticalAlignment.Center
			};
			Grid.SetColumn(icon, 0);
			rowGrid.Children.Add(icon);

			var textSp = new StackPanel
			{
				Spacing = 1.5,
				VerticalAlignment = VerticalAlignment.Center
			};
			var titleTb = new TextBlock
			{
				Text = title,
				FontSize = 12.5,
				FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
				TextTrimming = TextTrimming.CharacterEllipsis
			};
			var subTb = new TextBlock
			{
				Text = subtitle,
				FontSize = 10.0,
				TextTrimming = TextTrimming.CharacterEllipsis
			};
			textSp.Children.Add(titleTb);
			textSp.Children.Add(subTb);
			Grid.SetColumn(textSp, 1);
			rowGrid.Children.Add(textSp);

			btn.Content = rowGrid;
			btn.PointerEntered += (s, e) =>
			{
				if (s is Button b && b.Tag is string cat && !string.Equals(cat, _currentEditorCategory, StringComparison.OrdinalIgnoreCase))
				{
					bool dark = ResolveIsEditorDark();
					b.Background = LiquidGlassEngine.CreateSurfaceBrush(LiquidGlassVariant.Clear, dark, 0.72);
					b.BorderBrush = LiquidGlassEngine.CreateSpecularRimBrush(LiquidGlassVariant.Clear, dark, isHovered: true, isSelected: false);
					b.BorderThickness = new Thickness(1.0);
				}
			};
			btn.PointerExited += (s, e) =>
			{
				if (s is Button b && b.Tag is string cat && !string.Equals(cat, _currentEditorCategory, StringComparison.OrdinalIgnoreCase))
				{
					b.Background = new SolidColorBrush(Colors.Transparent);
					b.BorderBrush = new SolidColorBrush(Colors.Transparent);
					b.BorderThickness = new Thickness(0.0);
				}
			};
			btn.Click += (s, e) =>
			{
				if (s is Button b && b.Tag is string cat)
				{
					SelectEditorCategory(cat);
				}
			};
			_editorSidebarNavButtons.Add(btn);
			return btn;
		}

		AddNavSectionHeader("编辑与信息", isFirst: true);
		itemsStack.Children.Add(CreateNavButton("waveform", "\uE8C9", "波形与选区", "采样信息 & 区间剪辑"));

		AddNavSectionHeader("监测与分析");
		itemsStack.Children.Add(CreateNavButton("insight", "\uE9D2", "Insight Pro 监测分析", "响度 / 电平 / 声场 / 频谱 / 语图"));

		AddNavSectionHeader("调音与处理");
		itemsStack.Children.Add(CreateNavButton("eq", "\uE995", "参数均衡 & Mix Assistant", "10段参数EQ · 智能混音 · AutoEq"));
		itemsStack.Children.Add(CreateNavButton("effects", "\uE768", "空间与变速", "混响、立体声宽与变速"));
		itemsStack.Children.Add(CreateNavButton("fade", "\uE9D5", "淡入淡出 / 标准化", "响度目标与包络曲线"));

		AddNavSectionHeader("效果插件");
		itemsStack.Children.Add(CreateNavButton("vst", "\uE950", "VST3 效果架", "Audition 式插件链"));

		sv.Content = itemsStack;
		Grid.SetRow(sv, 1);
		sidebarGrid.Children.Add(sv);

		border.Child = sidebarGrid;
		RefreshEditorSidebarVisuals(isDark);
		return border;
	}

	private static string NormalizeCategoryKey(string? key) => key?.ToLowerInvariant() switch
	{
		"insight" or "spectrum" or "phase" => "insight",
		"waveform" or "eq" or "effects" or "fade" or "vst" => key.ToLowerInvariant(),
		_ => "insight"
	};

	private bool IsIsolatedWorkbenchView(FrameworkElement? view)
	{
		return view != null && (ReferenceEquals(view, _insightProHost) || ReferenceEquals(view, _paramEqHost));
	}

	private void WireEditorParametricEqControl(ParametricEqWorkbenchControl control)
	{
		control.BandsChanged += bands =>
		{
			if (ViewModel == null || _suppressParamEqEvents)
			{
				return;
			}
			ViewModel.ApplyPreviewParametricEq(bands, control.PreampDb, ViewModel.EchoEqEnabled);
		};
		control.PreampChanged += preampDb =>
		{
			if (ViewModel == null || _suppressParamEqEvents)
			{
				return;
			}
			ViewModel.ApplyPreviewParametricEq(control.Bands, preampDb, ViewModel.EchoEqEnabled);
		};
		control.EnabledChanged += enabled =>
		{
			if (ViewModel == null || _suppressParamEqEvents)
			{
				return;
			}
			ViewModel.ApplyPreviewParametricEq(control.Bands, control.PreampDb, enabled);
		};
		control.ApplyOfflineDspRequested += async () =>
		{
			await ApplyOfflineDspAsync();
		};
		control.ImportAutoEqRequested += async () =>
		{
			if (ViewModel == null)
			{
				return;
			}
			try
			{
				FileOpenPicker picker = new FileOpenPicker
				{
					FileTypeFilter = { ".txt", ".csv" }
				};
				nint windowHandle = WindowNative.GetWindowHandle(WindowManager.CurrentWindow);
				InitializeWithWindow.Initialize(picker, windowHandle);
				StorageFile storageFile = await picker.PickSingleFileAsync();
				if (storageFile == null)
				{
					return;
				}
				string text = await File.ReadAllTextAsync(storageFile.Path);
				if (ViewModel.ImportAutoEqToParametricBands(text))
				{
					_suppressParamEqEvents = true;
					try
					{
						control.SetBands(ViewModel.EchoEqBands, ViewModel.EchoEqPreampDb, ViewModel.EchoEqEnabled);
					}
					finally
					{
						_suppressParamEqEvents = false;
					}
					ViewModel.ApplyPreviewParametricEq(control.Bands, control.PreampDb, ViewModel.EchoEqEnabled);
				}
			}
			catch (Exception ex)
			{
				ViewModel.StatusText = "导入 AutoEq 失败：" + ex.Message;
			}
		};
		control.MixAssistantAnalyzeAudioRequested += async () =>
		{
			if (ViewModel == null)
			{
				return;
			}
			control.SetMixAssistantProgress(isLearning: false, progress01: 0.15, statusText: "正在分析音轨全曲频响平衡...");
			MixAssistantProfile profile = await ViewModel.AnalyzeMixAssistantOnLoadedSamples();
			if (profile.AnalyzedFrames > 0)
			{
				control.SetMixAssistantProfile(profile, autoApply: true);
			}
			else
			{
				control.SetMixAssistantProgress(isLearning: false, progress01: 0.0, statusText: "请先打开或加载音频文件后再分析音轨");
			}
		};
		control.MixAssistantLearnRequested += isLearning =>
		{
			if (ViewModel == null)
			{
				return;
			}
			if (isLearning)
			{
				if (ViewModel.DurationSeconds > 0.0)
				{
					if (!ViewModel.Preview.IsPlaying)
					{
						ViewModel.Preview.Play();
						UpdatePreviewButton();
					}
					_previewTimer.Start();
				}
				else
				{
					control.SetMixAssistantProgress(isLearning: false, progress01: 0.0, statusText: "请先加载音频文件后再开启学习监听");
				}
			}
		};
	}

	private FrameworkElement GetOrCreateEditorCategoryView(string key)
	{
		key = NormalizeCategoryKey(key);
		if (_editorCategoryViews.TryGetValue(key, out FrameworkElement? cached))
		{
			return cached;
		}

		FrameworkElement view;
		switch (key)
		{
			case "insight":
			{
				if (_insightProControl == null)
				{
					_insightProControl = new InsightProWorkbenchControl
					{
						HorizontalAlignment = HorizontalAlignment.Stretch,
						VerticalAlignment = VerticalAlignment.Stretch
					};
					_insightProControl.ResetRequested += () =>
					{
						if (ViewModel == null) return;
						ViewModel.InsightAnalyzer.ResetIntegrated();
						UpdateInsightUi();
					};
					_insightProControl.TargetChanged += preset =>
					{
						if (ViewModel == null) return;
						ViewModel.InsightAnalyzer.TargetPreset = preset;
						UpdateInsightUi();
					};
				}
				_insightProControl.UpdateTheme(ResolveIsEditorDark());
				UpdateInsightUi();

				_insightProHost = new Grid
				{
					HorizontalAlignment = HorizontalAlignment.Stretch,
					VerticalAlignment = VerticalAlignment.Stretch
				};
				_insightProHost.Children.Add(_insightProControl);
				view = _insightProHost;
				break;
			}
			case "eq":
			{
				if (_editorParamEqControl == null)
				{
					ViewModel?.InitializeEchoEqBands();
					_editorParamEqControl = new ParametricEqWorkbenchControl
					{
						HorizontalAlignment = HorizontalAlignment.Stretch,
						VerticalAlignment = VerticalAlignment.Stretch,
						ShowApplyOfflineDspButton = true,
						ApplyOfflineDspButtonText = "✨ 应用均衡器与DSP到音轨",
						ShowAnalyzeTrackButton = true
					};
					WireEditorParametricEqControl(_editorParamEqControl);
				}
				_editorParamEqControl.UpdateTheme(ResolveIsEditorDark());
				if (ViewModel != null)
				{
					_suppressParamEqEvents = true;
					try
					{
						_editorParamEqControl.SetBands(ViewModel.EchoEqBands, ViewModel.EchoEqPreampDb, ViewModel.EchoEqEnabled);
					}
					finally
					{
						_suppressParamEqEvents = false;
					}
					float[] bands64 = new float[64];
					if (ViewModel.TryGetCurrentSpectrum64(bands64))
					{
						_editorParamEqControl.UpdateSpectrum(bands64);
					}
				}

				_paramEqHost = new Grid
				{
					HorizontalAlignment = HorizontalAlignment.Stretch,
					VerticalAlignment = VerticalAlignment.Stretch
				};
				_paramEqHost.Children.Add(_editorParamEqControl);
				view = _paramEqHost;
				break;
			}
			case "effects":
			{
				var grid = new Grid();
				if (EffectsPanel != null)
				{
					EffectsPanel.MaxWidth = 880.0;
					EffectsPanel.HorizontalAlignment = HorizontalAlignment.Stretch;
					EffectsPanel.VerticalAlignment = VerticalAlignment.Stretch;
					grid.Children.Add(EffectsPanel);
				}
				view = grid;
				break;
			}
			case "fade":
			{
				var grid = new Grid();
				if (FadePanel != null)
				{
					FadePanel.MaxWidth = 880.0;
					FadePanel.HorizontalAlignment = HorizontalAlignment.Stretch;
					FadePanel.VerticalAlignment = VerticalAlignment.Stretch;
					grid.Children.Add(FadePanel);
				}
				view = grid;
				break;
			}
			case "vst":
			{
				var grid = new Grid();
				if (VstPanel != null)
				{
					VstPanel.MinHeight = 240.0;
					VstPanel.VerticalAlignment = VerticalAlignment.Stretch;
					grid.Children.Add(VstPanel);
				}
				view = grid;
				break;
			}
			default: // "waveform"
			{
				var grid = new Grid { RowSpacing = 10.0 };
				grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
				grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1.0, GridUnitType.Star) });
				if (InfoPanel != null)
				{
					InfoPanel.VerticalAlignment = VerticalAlignment.Stretch;
					Grid.SetRow(InfoPanel, 0);
					grid.Children.Add(InfoPanel);
				}
				if (_waveformSectionGrid != null)
				{
					_waveformSectionGrid.VerticalAlignment = VerticalAlignment.Stretch;
					Grid.SetRow(_waveformSectionGrid, 1);
					grid.Children.Add(_waveformSectionGrid);
				}
				view = grid;
				break;
			}
		}

		_editorCategoryViews[key] = view;
		return view;
	}

	public void SelectEditorCategory(string key)
	{
		string normalized = NormalizeCategoryKey(key);

		_currentEditorCategory = normalized;
		bool isDark = ResolveIsEditorDark();
		RefreshEditorSidebarVisuals(isDark);

		if (_editorWorkbenchHost == null)
		{
			return;
		}

		bool manageOwnScroll = string.Equals(normalized, "insight", StringComparison.OrdinalIgnoreCase)
			|| string.Equals(normalized, "eq", StringComparison.OrdinalIgnoreCase);
		_editorWorkbenchHost.VerticalScrollBarVisibility = manageOwnScroll ? ScrollBarVisibility.Disabled : ScrollBarVisibility.Auto;
		_editorWorkbenchHost.VerticalScrollMode = manageOwnScroll ? ScrollMode.Disabled : ScrollMode.Auto;

		FrameworkElement view = GetOrCreateEditorCategoryView(normalized);
		_editorWorkbenchHost.Content = view;
		_editorWorkbenchHost.ChangeView(null, 0.0, null, disableAnimation: true);

		ApplyEditorLiquidGlassOnPanels(isDark);
		if (ReferenceEquals(view, _insightProHost))
		{
			_insightProControl?.UpdateTheme(isDark);
			UpdateInsightUi();
			return;
		}
		if (ReferenceEquals(view, _paramEqHost))
		{
			_editorParamEqControl?.UpdateTheme(isDark);
			return;
		}

		LiquidGlassEngine.UpgradeVisualTreeToLiquidGlass(view, isDark);
		FluentIconFactory.UpgradeButtonsInVisualTree(view);
		ApplyEditorContrastToSubtree(view, new ContrastPalette(isDark), insideHeader: false, insidePhaseOverlay: false, insideAccentButton: false);

		DispatcherQueue?.TryEnqueue(() =>
		{
			if (string.Equals(_currentEditorCategory, normalized, StringComparison.OrdinalIgnoreCase))
			{
				bool currentDark = ResolveIsEditorDark();
				ApplyEditorContrastToSubtree(view, new ContrastPalette(currentDark), insideHeader: false, insidePhaseOverlay: false, insideAccentButton: false);
			}
		});
	}

	private void RefreshEditorSidebarVisuals(bool isDark)
	{
		if (_editorSidebarBorder != null)
		{
			LiquidGlassEngine.AttachInteractiveLiquidGlass(_editorSidebarBorder, LiquidGlassVariant.FloatingBar, isDark, enablePointerTracking: true);
		}
		if (_editorSidebarBadgeTb != null)
		{
			_editorSidebarBadgeTb.Foreground = new SolidColorBrush(isDark
				? Color.FromArgb(255, 165, 180, 252)
				: Color.FromArgb(255, 99, 102, 241));
		}
		if (_editorSidebarTitleTb != null)
		{
			_editorSidebarTitleTb.Foreground = new SolidColorBrush(isDark
				? Colors.White
				: Color.FromArgb(255, 15, 23, 42));
		}
		if (_editorSidebarSubtitleTb != null)
		{
			_editorSidebarSubtitleTb.Foreground = new SolidColorBrush(isDark
				? Color.FromArgb(255, 203, 213, 225)
				: Color.FromArgb(255, 71, 85, 105));
		}
		if (_editorSidebarDivider != null)
		{
			_editorSidebarDivider.Background = new SolidColorBrush(isDark
				? Color.FromArgb(42, 255, 255, 255)
				: Color.FromArgb(32, 15, 23, 42));
		}

		Brush sectionBrush = new SolidColorBrush(isDark
			? Color.FromArgb(255, 186, 198, 214)
			: Color.FromArgb(255, 71, 85, 105));
		foreach (TextBlock header in _editorSidebarSectionHeaders)
		{
			header.Foreground = sectionBrush;
		}

		UpdateEditorSidebarNavVisuals(isDark);
	}

	private void UpdateEditorSidebarNavVisuals(bool isDark)
	{
		Brush primaryText = new SolidColorBrush(isDark
			? Color.FromArgb(255, 248, 250, 252)
			: Color.FromArgb(255, 15, 23, 42));
		Brush mutedText = new SolidColorBrush(isDark
			? Color.FromArgb(255, 203, 213, 225)
			: Color.FromArgb(255, 71, 85, 105));
		Brush mutedIcon = new SolidColorBrush(isDark
			? Color.FromArgb(255, 203, 213, 225)
			: Color.FromArgb(255, 100, 116, 139));
		Brush selectedTitleBrush = new SolidColorBrush(isDark
			? Colors.White
			: Color.FromArgb(255, 67, 56, 202));
		Brush selectedAccentBrush = new SolidColorBrush(isDark
			? Color.FromArgb(255, 224, 231, 255)
			: Color.FromArgb(255, 79, 70, 229));

		foreach (Button btn in _editorSidebarNavButtons)
		{
			bool isSelected = string.Equals(btn.Tag as string, _currentEditorCategory, StringComparison.OrdinalIgnoreCase);
			if (isSelected)
			{
				btn.Background = LiquidGlassEngine.CreateSurfaceBrush(LiquidGlassVariant.AccentLensed, isDark, 0.95);
				btn.BorderBrush = LiquidGlassEngine.CreateSpecularRimBrush(LiquidGlassVariant.AccentLensed, isDark, isHovered: true, isSelected: true);
				btn.BorderThickness = new Thickness(1.0);
			}
			else
			{
				btn.Background = new SolidColorBrush(Colors.Transparent);
				btn.BorderBrush = new SolidColorBrush(Colors.Transparent);
				btn.BorderThickness = new Thickness(0.0);
			}

			if (btn.Content is Grid g)
			{
				if (g.Children.Count > 0 && g.Children[0] is FontIcon ic)
				{
					ic.Foreground = isSelected ? selectedAccentBrush : mutedIcon;
				}
				if (g.Children.Count > 1 && g.Children[1] is StackPanel sp)
				{
					if (sp.Children.Count > 0 && sp.Children[0] is TextBlock t1)
					{
						t1.Foreground = isSelected ? selectedTitleBrush : primaryText;
					}
					if (sp.Children.Count > 1 && sp.Children[1] is TextBlock t2)
					{
						t2.Foreground = isSelected ? selectedAccentBrush : mutedText;
					}
				}
				else if (g.Children.Count > 1 && g.Children[1] is TextBlock fallbackTb)
				{
					fallbackTb.Foreground = isSelected ? selectedTitleBrush : primaryText;
				}
			}
		}
	}

	private void ApplyEditorLiquidGlassOnPanels(bool isDark)
	{
		Border?[] floatingPanels = new Border?[]
		{
			_waveformCardBorder,
			SpectrumPanel,
			PhasePanel,
			InfoPanel,
			EqPanel,
			EffectsPanel,
			FadePanel,
			VstPanel
		};
		foreach (Border? panel in floatingPanels)
		{
			if (panel != null)
			{
				panel.CornerRadius = new CornerRadius(16.0);
				panel.BorderThickness = new Thickness(1.2);
				LiquidGlassEngine.AttachInteractiveLiquidGlass(panel, LiquidGlassVariant.FloatingBar, isDark, enablePointerTracking: true);
			}
		}

		Grid?[] panelHeaders = new Grid?[]
		{
			SpectrumHeader,
			PhaseHeader,
			InfoHeader,
			EqHeader,
			EffectsHeader,
			FadeHeader,
			VstHeader
		};
		foreach (Grid? header in panelHeaders)
		{
			if (header != null)
			{
				header.CornerRadius = new CornerRadius(15.0, 15.0, 0.0, 0.0);
				header.Background = LiquidGlassEngine.CreateSurfaceBrush(LiquidGlassVariant.Clear, isDark, 0.55);
				header.BorderBrush = LiquidGlassEngine.CreateSpecularRimBrush(LiquidGlassVariant.Clear, isDark);
				header.BorderThickness = new Thickness(0.0, 0.0, 0.0, 1.0);
			}
		}

		if (PreviewPlayButton != null)
		{
			PreviewPlayButton.CornerRadius = new CornerRadius(14.0);
			LiquidGlassEngine.AttachInteractiveLiquidGlass(PreviewPlayButton, LiquidGlassVariant.AccentLensed, isDark, enablePointerTracking: true);
		}
	}

	private void UpgradeEditorChromeIcons()
	{
		if (Content is not Grid rootGrid)
		{
			FluentIconFactory.UpgradeButtonsInVisualTree(this);
			return;
		}
		foreach (UIElement child in rootGrid.Children)
		{
			if (ReferenceEquals(child, _workbenchShell))
			{
				if (_editorSidebarBorder != null)
				{
					FluentIconFactory.UpgradeButtonsInVisualTree(_editorSidebarBorder);
				}
				if (_editorWorkbenchHost?.Content is FrameworkElement activeView && !IsIsolatedWorkbenchView(activeView))
				{
					FluentIconFactory.UpgradeButtonsInVisualTree(activeView);
				}
			}
			else
			{
				FluentIconFactory.UpgradeButtonsInVisualTree(child);
			}
		}
	}

	private void ApplyEditorLiquidGlass()
	{
		bool isDark = ResolveIsEditorDark();

		if (Content is Grid rootGrid)
		{
			rootGrid.Background = new SolidColorBrush(Colors.Transparent);
			if (_ambientCausticCanvas != null)
			{
				_ambientCausticCanvas.Visibility = Visibility.Collapsed;
			}
			foreach (UIElement child in rootGrid.Children)
			{
				if (ReferenceEquals(child, _workbenchShell))
				{
					if (_editorWorkbenchHost?.Content is FrameworkElement activeView && !IsIsolatedWorkbenchView(activeView))
					{
						LiquidGlassEngine.UpgradeVisualTreeToLiquidGlass(activeView, isDark);
					}
				}
				else
				{
					LiquidGlassEngine.UpgradeVisualTreeToLiquidGlass(child, isDark);
				}
			}
		}
		else
		{
			LiquidGlassEngine.UpgradeVisualTreeToLiquidGlass(this, isDark);
		}

		ApplyEditorLiquidGlassOnPanels(isDark);
		RefreshEditorSidebarVisuals(isDark);
		ApplyEditorTextContrast(isDark);
	}

	private void ApplyEditorTextContrast(bool isDark)
	{
		ContrastPalette palette = new ContrastPalette(isDark);
		ApplyEditorContrastToSubtree(this, palette, insideHeader: false, insidePhaseOverlay: false, insideAccentButton: false);
		RefreshEditorSidebarVisuals(isDark);
		_insightProControl?.UpdateTheme(isDark);
		_editorParamEqControl?.UpdateTheme(isDark);
	}

	private bool IsPanelHeader(DependencyObject obj)
	{
		return ReferenceEquals(obj, SpectrumHeader)
			|| ReferenceEquals(obj, PhaseHeader)
			|| ReferenceEquals(obj, InfoHeader)
			|| ReferenceEquals(obj, EqHeader)
			|| ReferenceEquals(obj, EffectsHeader)
			|| ReferenceEquals(obj, FadeHeader)
			|| ReferenceEquals(obj, VstHeader);
	}

	private bool IsResizeHandle(DependencyObject obj)
	{
		return ReferenceEquals(obj, SpectrumResizeHandle)
			|| ReferenceEquals(obj, PhaseResizeHandle)
			|| ReferenceEquals(obj, InfoResizeHandle)
			|| ReferenceEquals(obj, EqResizeHandle)
			|| ReferenceEquals(obj, EffectsResizeHandle)
			|| ReferenceEquals(obj, FadeResizeHandle)
			|| ReferenceEquals(obj, VstResizeHandle);
	}

	private static bool IsNeutralTextColor(Color c)
	{
		int max = Math.Max(c.R, Math.Max(c.G, c.B));
		int min = Math.Min(c.R, Math.Min(c.G, c.B));
		return (max - min) <= 45;
	}

	private static bool IsAccentStyledButton(ButtonBase btn)
	{
		try
		{
			if (Application.Current?.Resources != null
				&& Application.Current.Resources.TryGetValue("AccentButtonStyle", out object? accentStyle)
				&& ReferenceEquals(btn.Style, accentStyle))
			{
				return true;
			}
		}
		catch
		{
		}

		if (btn.Background is SolidColorBrush scb && scb.Color.A > 0 && !IsNeutralTextColor(scb.Color))
		{
			return true;
		}
		return false;
	}

	private void ApplyEditorContrastToSubtree(
		DependencyObject root,
		ContrastPalette palette,
		bool insideHeader,
		bool insidePhaseOverlay,
		bool insideAccentButton)
	{
		if (root == null
			|| root is InsightProWorkbenchControl
			|| root is ParametricEqWorkbenchControl
			|| root is ParametricEqCurveControl)
		{
			return;
		}

		int count = VisualTreeHelper.GetChildrenCount(root);
		for (int i = 0; i < count; i++)
		{
			DependencyObject child = VisualTreeHelper.GetChild(root, i);

			// Sidebar colors and active/hover visuals are managed by RefreshEditorSidebarVisuals
			if (_editorSidebarBorder != null && ReferenceEquals(child, _editorSidebarBorder))
			{
				continue;
			}

			// InsightProWorkbenchControl, ParametricEqWorkbenchControl, and ParametricEqCurveControl manage their own internal DAW / EQ themes
			if (child is InsightProWorkbenchControl
				|| child is ParametricEqWorkbenchControl
				|| child is ParametricEqCurveControl)
			{
				continue;
			}

			// Skip gradient-filled FluentIconFactory badge tiles so icon badge text is never overwritten
			if (child is Border tileBorder && tileBorder.Background is LinearGradientBrush && tileBorder.Child is TextBlock)
			{
				continue;
			}

			bool childHeader = insideHeader || IsPanelHeader(child);
			bool childPhaseOverlay = (insidePhaseOverlay || ReferenceEquals(child, PhasePanel)) && !childHeader;
			bool childAccentBtn = insideAccentButton;

			if (child is Button btn)
			{
				if (IsAccentStyledButton(btn))
				{
					btn.Foreground = palette.AccentWhite;
					childAccentBtn = true;
				}
				else
				{
					btn.Foreground = palette.Primary;
				}
				ApplyEditorContrastToSubtree(child, palette, childHeader, childPhaseOverlay, childAccentBtn);
				continue;
			}

			if (child is ToggleButton toggleBtn)
			{
				toggleBtn.Foreground = palette.Primary;
				ApplyEditorContrastToSubtree(child, palette, childHeader, childPhaseOverlay, childAccentBtn);
				continue;
			}

			if (child is ToggleSwitch toggleSwitch)
			{
				toggleSwitch.Foreground = palette.Secondary;
				StyleTemplateTextPresenters(toggleSwitch, palette, includeOnOffPresenters: true);
				continue;
			}

			if (child is Slider slider)
			{
				slider.Foreground = palette.Secondary;
				StyleTemplateTextPresenters(slider, palette, includeOnOffPresenters: false);
				continue;
			}

			if (child is ComboBox combo)
			{
				combo.Foreground = palette.Primary;
				StyleComboTemplateElements(combo, palette);
				continue;
			}

			if (child is Border border)
			{
				if (border.Width > 0 && border.Width <= 2.0 && border.Height >= 12.0 && border.Height <= 32.0)
				{
					border.Background = palette.Divider;
				}
			}
			else if (child is Microsoft.UI.Xaml.Shapes.Path shapePath && IsResizeHandle(root))
			{
				shapePath.Fill = palette.ResizeFill;
				shapePath.Opacity = palette.IsDark ? 0.55 : 0.45;
			}
			else if (child is TextBlock tb)
			{
				ApplyTextBlockContrast(tb, palette, childHeader, childPhaseOverlay, childAccentBtn);
			}
			else if (child is FontIcon fi)
			{
				ApplyFontIconContrast(fi, palette, childHeader, childAccentBtn);
			}

			ApplyEditorContrastToSubtree(child, palette, childHeader, childPhaseOverlay, childAccentBtn);
		}
	}

	private void ApplyTextBlockContrast(
		TextBlock tb,
		ContrastPalette palette,
		bool insideHeader,
		bool insidePhaseOverlay,
		bool insideAccentButton)
	{
		if (!_elementContrastTiers.TryGetValue(tb, out string? tier))
		{
			if (insidePhaseOverlay)
			{
				tier = "phaseOverlay";
			}
			else if (insideAccentButton)
			{
				tier = "accentButton";
			}
			else if (insideHeader || ReferenceEquals(tb, SelStartText) || ReferenceEquals(tb, SelEndText) || tb.FontSize >= 13.0 || tb.FontWeight.Weight >= 600)
			{
				tier = "primary";
			}
			else if (tb.Opacity < 0.72)
			{
				tier = "muted";
			}
			else if (tb.Opacity < 0.99)
			{
				tier = "secondary";
			}
			else
			{
				tier = "primary";
			}
			_elementContrastTiers.AddOrUpdate(tb, tier);
		}

		if (tb.Foreground is SolidColorBrush existingScb && !IsNeutralTextColor(existingScb.Color))
		{
			return;
		}

		switch (tier)
		{
			case "phaseOverlay":
				tb.Foreground = palette.PhaseOverlay;
				tb.Opacity = 1.0;
				break;
			case "accentButton":
				tb.Foreground = palette.AccentWhite;
				tb.Opacity = 1.0;
				break;
			case "muted":
				tb.Foreground = palette.Muted;
				tb.Opacity = 1.0;
				break;
			case "secondary":
				tb.Foreground = palette.Secondary;
				tb.Opacity = 1.0;
				break;
			default:
				tb.Foreground = palette.Primary;
				tb.Opacity = 1.0;
				break;
		}
	}

	private static void ApplyFontIconContrast(
		FontIcon fi,
		ContrastPalette palette,
		bool insideHeader,
		bool insideAccentButton)
	{
		if (fi.Foreground is SolidColorBrush existingScb && !IsNeutralTextColor(existingScb.Color))
		{
			return;
		}

		fi.Foreground = insideAccentButton
			? palette.AccentWhite
			: (insideHeader ? palette.Secondary : palette.Primary);
		fi.Opacity = 1.0;
	}

	private static void StyleTemplateTextPresenters(DependencyObject root, ContrastPalette palette, bool includeOnOffPresenters)
	{
		int count = VisualTreeHelper.GetChildrenCount(root);
		for (int i = 0; i < count; i++)
		{
			DependencyObject child = VisualTreeHelper.GetChild(root, i);
			if (child is ContentPresenter cp)
			{
				if (cp.Name == "HeaderContentPresenter")
				{
					cp.Foreground = palette.Secondary;
					cp.Opacity = 1.0;
					StylePresenterInnerTextBlocks(cp, palette.Secondary, setOpaque: true);
					continue;
				}
				if (includeOnOffPresenters && (cp.Name == "OffContentPresenter" || cp.Name == "OnContentPresenter"))
				{
					// Only update Foreground; never force Opacity = 1.0 so ToggleSwitch VisualStateManager controls On/Off visibility
					cp.Foreground = palette.Secondary;
					StylePresenterInnerTextBlocks(cp, palette.Secondary, setOpaque: false);
					continue;
				}
			}
			StyleTemplateTextPresenters(child, palette, includeOnOffPresenters);
		}
	}

	private static void StyleComboTemplateElements(DependencyObject root, ContrastPalette palette)
	{
		int count = VisualTreeHelper.GetChildrenCount(root);
		for (int i = 0; i < count; i++)
		{
			DependencyObject child = VisualTreeHelper.GetChild(root, i);
			if (child is ContentPresenter cp)
			{
				if (cp.Name == "HeaderContentPresenter")
				{
					cp.Foreground = palette.Secondary;
					cp.Opacity = 1.0;
					StylePresenterInnerTextBlocks(cp, palette.Secondary);
					continue;
				}
				if (cp.Name == "ContentPresenter")
				{
					cp.Foreground = palette.Primary;
					cp.Opacity = 1.0;
					StylePresenterInnerTextBlocks(cp, palette.Primary);
					continue;
				}
			}
			else if (child is FontIcon fi && fi.Name == "DropDownGlyph")
			{
				fi.Foreground = palette.Muted;
				fi.Opacity = 1.0;
				continue;
			}
			StyleComboTemplateElements(child, palette);
		}
	}

	private static void StylePresenterInnerTextBlocks(DependencyObject presenter, Brush brush, bool setOpaque = true)
	{
		int count = VisualTreeHelper.GetChildrenCount(presenter);
		for (int i = 0; i < count; i++)
		{
			DependencyObject child = VisualTreeHelper.GetChild(presenter, i);
			if (child is TextBlock tb)
			{
				tb.Foreground = brush;
				if (setOpaque)
				{
					tb.Opacity = 1.0;
				}
			}
			StylePresenterInnerTextBlocks(child, brush, setOpaque);
		}
	}

	private void OnEditorVstContainerChanging(ListViewBase sender, ContainerContentChangingEventArgs args)
	{
		if (args.ItemContainer != null && !args.InRecycleQueue)
		{
			bool isDark = ResolveIsEditorDark();
			_suppressVstContainerEvents = true;
			try
			{
				FormatVstItemVisuals(args.ItemContainer);
				LiquidGlassEngine.UpgradeVisualTreeToLiquidGlass(args.ItemContainer, isDark);
				ApplyEditorContrastToSubtree(args.ItemContainer, new ContrastPalette(isDark), insideHeader: false, insidePhaseOverlay: false, insideAccentButton: false);
			}
			finally
			{
				_suppressVstContainerEvents = false;
			}
		}
	}

	private void FormatVstItemVisuals(DependencyObject root)
	{
		if (root is ListViewItem lvi)
		{
			lvi.Padding = new Thickness(8, 2, 16, 2);
			lvi.HorizontalContentAlignment = HorizontalAlignment.Stretch;
		}
		int count = VisualTreeHelper.GetChildrenCount(root);
		for (int i = 0; i < count; i++)
		{
			var child = VisualTreeHelper.GetChild(root, i);
			if (child is ToggleSwitch ts)
			{
				ts.OnContent = "";
				ts.OffContent = "";
				ts.MinWidth = 46;
				ts.Width = 46;
				ts.Margin = new Thickness(4, 0, 4, 0);
				ts.VerticalAlignment = VerticalAlignment.Center;
				VstSwitchTracker tracker = _vstSwitchTrackers.GetOrCreateValue(ts);
				Vst3PluginState? plugin = ts.DataContext as Vst3PluginState;
				tracker.Plugin = plugin;
				tracker.LastEnabled = plugin?.Enabled ?? ts.IsOn;
				continue;
			}
			if (child is Button btn)
			{
				string text = btn.Content as string ?? "";
				bool isUiBtn = text.Contains("界面");
				bool alreadyUpgraded = false;
				if (btn.Content is StackPanel spCheck)
				{
					isUiBtn = spCheck.Children.Any(c => c is TextBlock tb && tb.Text.Contains("界面"));
					alreadyUpgraded = isUiBtn
						&& spCheck.Children.Count >= 2
						&& spCheck.Children[0] is not FontIcon
						&& spCheck.Children[0] is not TextBlock;
				}
				if (isUiBtn)
				{
					btn.Height = 28;
					btn.Padding = new Thickness(8, 2, 8, 2);
					btn.Margin = new Thickness(6, 0, 8, 0);
					// Never overwrite btn.Tag here — it carries the {Binding} Vst3PluginState!
					if (!alreadyUpgraded)
					{
						var icon = FluentIconFactory.Create(FluentIconKind.VstPlugin, 14);
						var panel = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 5, VerticalAlignment = VerticalAlignment.Center };
						panel.Children.Add(icon);
						panel.Children.Add(new TextBlock { Text = "界面", FontSize = 11, FontWeight = Microsoft.UI.Text.FontWeights.Medium, VerticalAlignment = VerticalAlignment.Center });
						btn.Content = panel;
					}
				}
				continue;
			}
			else if (child is ToggleButton tb)
			{
				tb.Height = 28;
				tb.Padding = new Thickness(8, 2, 8, 2);
				tb.FontSize = 11;
			}
			else if (child is TextBlock txt)
			{
				if (txt.FontSize > 13) txt.FontSize = 12;
			}
			FormatVstItemVisuals(child);
		}
	}

	private void OnEditorVstSwitchDataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
	{
		if (sender is ToggleSwitch ts)
		{
			VstSwitchTracker tracker = _vstSwitchTrackers.GetOrCreateValue(ts);
			Vst3PluginState? next = args.NewValue as Vst3PluginState;
			tracker.Plugin = next;
			tracker.LastEnabled = next?.Enabled ?? false;
			tracker.SuppressNext = true;
			DispatcherQueue?.TryEnqueue(() => tracker.SuppressNext = false);
		}
	}

	private void SyncVstListItemControls(Vst3PluginState plugin, ToggleSwitch? excludeSwitch = null, ToggleButton? excludeBypass = null)
	{
		if (EditorVstList == null)
		{
			return;
		}
		if (EditorVstList.ContainerFromItem(plugin) is not DependencyObject container)
		{
			return;
		}
		_suppressVstContainerEvents = true;
		try
		{
			SyncVstContainerRecursive(container, plugin, excludeSwitch, excludeBypass);
		}
		finally
		{
			_suppressVstContainerEvents = false;
		}
	}

	private void SyncVstContainerRecursive(DependencyObject root, Vst3PluginState plugin, ToggleSwitch? excludeSwitch, ToggleButton? excludeBypass)
	{
		int count = VisualTreeHelper.GetChildrenCount(root);
		for (int i = 0; i < count; i++)
		{
			DependencyObject child = VisualTreeHelper.GetChild(root, i);
			if (child is ToggleSwitch ts)
			{
				VstSwitchTracker tracker = _vstSwitchTrackers.GetOrCreateValue(ts);
				tracker.Plugin = plugin;
				tracker.LastEnabled = plugin.Enabled;
				if (!ReferenceEquals(ts, excludeSwitch) && ts.IsOn != plugin.Enabled)
				{
					ts.IsOn = plugin.Enabled;
				}
			}
			else if (child is ToggleButton tb && !ReferenceEquals(tb, excludeBypass))
			{
				if ((tb.IsChecked == true) != plugin.Bypass)
				{
					tb.IsChecked = plugin.Bypass;
				}
			}
			else
			{
				SyncVstContainerRecursive(child, plugin, excludeSwitch, excludeBypass);
			}
		}
	}

	private void ApplyVst3ToEditorPreview()
	{
		if (ViewModel == null)
		{
			return;
		}
		bool wasPlaying = ViewModel.Preview.IsPlaying;
		TimeSpan resumePos = wasPlaying ? ViewModel.Preview.Position : TimeSpan.Zero;
		ViewModel.ApplyVst3Command.Execute(null);
		// PreviewPlayer.SetVst3 skips restarting when all effects become inactive;
		// restart dry playback at the same timestamp so disabling the last VST3 takes effect immediately.
		if (wasPlaying
			&& ViewModel.ActiveVst3Plugins.Count == 0
			&& !ViewModel.ReverbEnabled
			&& !ViewModel.HasActiveParametricEq
			&& !ViewModel.Equalizer.Gains.Any(g => Math.Abs(g) > 0.01))
		{
			ViewModel.Preview.Stop();
			ViewModel.Preview.Play();
			ViewModel.Preview.Position = resumePos;
			UpdatePreviewButton();
		}
	}

	private void OnEditorVstToggled(object sender, RoutedEventArgs e)
	{
		if (_suppressVstContainerEvents || ViewModel == null)
		{
			return;
		}
		if (sender is FrameworkElement fe && !fe.IsLoaded)
		{
			return;
		}
		if (sender is ToggleSwitch ts)
		{
			VstSwitchTracker tracker = _vstSwitchTrackers.GetOrCreateValue(ts);
			Vst3PluginState? plugin = ts.DataContext as Vst3PluginState;
			if (tracker.SuppressNext || !ReferenceEquals(tracker.Plugin, plugin))
			{
				tracker.Plugin = plugin;
				tracker.LastEnabled = plugin?.Enabled ?? ts.IsOn;
				return;
			}
			if (plugin == null)
			{
				return;
			}
			if (ts.IsOn == tracker.LastEnabled && plugin.Enabled == ts.IsOn)
			{
				return;
			}
			plugin.Enabled = ts.IsOn;
			plugin.Bypass = false;
			tracker.LastEnabled = plugin.Enabled;
			SyncVstListItemControls(plugin, excludeSwitch: ts);
			ApplyVst3ToEditorPreview();
		}
		else if (sender is ToggleButton tb)
		{
			if (tb.DataContext is Vst3PluginState plugin)
			{
				plugin.Bypass = tb.IsChecked == true;
			}
			ApplyVst3ToEditorPreview();
		}
	}

	private void OnEditorVstEditorClick(object sender, RoutedEventArgs e)
	{
		Vst3PluginState? tag = (sender as FrameworkElement)?.Tag as Vst3PluginState
			?? (sender as FrameworkElement)?.DataContext as Vst3PluginState;
		if (tag == null || ViewModel == null)
		{
			return;
		}
		try
		{
			if (_editorVstWindow != null && string.Equals(_editorVstWindow.PluginPath, tag.Path, StringComparison.OrdinalIgnoreCase))
			{
				_editorVstWindow.BringToFront();
				return;
			}
			CloseEditorVstWindow();
			int sampleRate = (ViewModel.EditorSampleRate > 0) ? ViewModel.EditorSampleRate : 44100;
			Vst3EditorWindow? opened = Vst3EditorWindow.Open(tag.Path, sampleRate, 2, out string error);
			if (opened == null)
			{
				ViewModel.Vst3StatusText = "打开插件界面失败：" + error;
				return;
			}
			_editorVstWindow = opened;
			opened.Closed += () => OnEditorVstWindowClosed(opened);
			bool stateChanged = !tag.Enabled || tag.Bypass;
			if (stateChanged)
			{
				tag.Enabled = true;
				tag.Bypass = false;
				SyncVstListItemControls(tag);
				ApplyVst3ToEditorPreview();
			}
			ViewModel.Vst3StatusText = "已打开插件原生界面：" + tag.Name + "（效果组，参数实时生效）";
		}
		catch (Exception ex)
		{
			ViewModel.Vst3StatusText = "打开插件界面异常：" + ex.Message;
		}
	}

	private void OnEditorVstWindowClosed(Vst3EditorWindow? w)
	{
		if (w == null)
		{
			w = _editorVstWindow;
		}
		if (ReferenceEquals(_editorVstWindow, w))
		{
			_editorVstWindow = null;
		}
		if (w != null && ViewModel != null)
		{
			Vst3PluginState? vst3PluginState = ViewModel.Vst3Plugins.FirstOrDefault((Vst3PluginState p) => p != null && string.Equals(p.Path, w.PluginPath, StringComparison.OrdinalIgnoreCase));
			if (vst3PluginState != null)
			{
				ViewModel.Vst3StatusText = vst3PluginState.IsActive
					? "已关闭《" + vst3PluginState.Name + "》插件界面（插件保持启用，预览与导出继续生效）"
					: "已关闭《" + vst3PluginState.Name + "》插件界面";
			}
		}
	}

	private void CloseEditorVstWindow()
	{
		Vst3EditorWindow? editorVstWindow = _editorVstWindow;
		if (editorVstWindow == null)
		{
			return;
		}
		_editorVstWindow = null;
		try
		{
			editorVstWindow.Close();
		}
		catch
		{
		}
	}

	protected override void OnNavigatedFrom(NavigationEventArgs e)
	{
		base.OnNavigatedFrom(e);
		_previewTimer.Stop();
		ViewModel?.Preview.Stop();
		CloseEditorVstWindow();
	}

	[GeneratedCode("Microsoft.UI.Xaml.Markup.Compiler", " 3.0.0.2409")]
	[DebuggerNonUserCode]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("ms-appx:///FurinaPlayer.UI/Pages/EditorPage.xaml");
			Application.LoadComponent(this, resourceLocator, ComponentResourceLocation.Nested);
		}
	}

	[GeneratedCode("Microsoft.UI.Xaml.Markup.Compiler", " 3.0.0.2409")]
	[DebuggerNonUserCode]
	public void Connect(int connectionId, object target)
	{
		switch (connectionId)
		{
		case 2:
			target.As<KeyboardAccelerator>().Invoked += OnUndoAccelerator;
			break;
		case 3:
			target.As<KeyboardAccelerator>().Invoked += OnRedoAccelerator;
			break;
		case 4:
			PanelCanvas = target.As<Canvas>();
			break;
		case 5:
			SpectrumPanel = target.As<Border>();
			break;
		case 6:
			PhasePanel = target.As<Border>();
			break;
		case 7:
			InfoPanel = target.As<Border>();
			break;
		case 8:
			EqPanel = target.As<Border>();
			break;
		case 9:
			EffectsPanel = target.As<Border>();
			break;
		case 10:
			FadePanel = target.As<Border>();
			break;
		case 11:
			VstPanel = target.As<Border>();
			break;
		case 12:
			VstHeader = target.As<Grid>();
			break;
		case 13:
			VstResizeHandle = target.As<Grid>();
			break;
		case 14:
			EditorVstList = target.As<ListView>();
			break;
		case 17:
		{
			ToggleSwitch vstToggle = target.As<ToggleSwitch>();
			vstToggle.DataContextChanged += OnEditorVstSwitchDataContextChanged;
			vstToggle.Toggled += OnEditorVstToggled;
			break;
		}
		case 18:
			target.As<ToggleButton>().Click += OnEditorVstToggled;
			break;
		case 19:
			target.As<Button>().Click += OnEditorVstEditorClick;
			break;
		case 20:
			target.As<Button>().Click += OnPanelEditClick;
			break;
		case 21:
			FadeHeader = target.As<Grid>();
			break;
		case 22:
			FadeResizeHandle = target.As<Grid>();
			break;
		case 23:
			target.As<Button>().Click += OnPanelEditClick;
			break;
		case 24:
			EffectsHeader = target.As<Grid>();
			break;
		case 25:
			EffectsResizeHandle = target.As<Grid>();
			break;
		case 26:
			ReverbPresetCombo = target.As<ComboBox>();
			break;
		case 27:
			target.As<Button>().Click += OnPanelEditClick;
			break;
		case 28:
			EqHeader = target.As<Grid>();
			break;
		case 29:
			EditorEq = target.As<EqualizerControl>();
			EditorEq.GainsChanged += OnEditorEqGainsChanged;
			break;
		case 30:
			EqResizeHandle = target.As<Grid>();
			break;
		case 31:
			target.As<Button>().Click += OnPanelEditClick;
			break;
		case 32:
			InfoHeader = target.As<Grid>();
			break;
		case 33:
			InfoResizeHandle = target.As<Grid>();
			break;
		case 34:
			SampleInfoText = target.As<TextBlock>();
			break;
		case 35:
			target.As<Button>().Click += OnPanelEditClick;
			break;
		case 36:
			PhaseHeader = target.As<Grid>();
			break;
		case 37:
			Phase = target.As<PhaseAnalyzerControl>();
			break;
		case 38:
			PhaseResizeHandle = target.As<Grid>();
			break;
		case 39:
			target.As<Button>().Click += OnPanelEditClick;
			break;
		case 40:
			SpectrumHeader = target.As<Grid>();
			break;
		case 41:
			Spectrum = target.As<SpectrumAnalyzer>();
			break;
		case 42:
			SpectrumResizeHandle = target.As<Grid>();
			break;
		case 43:
			target.As<Button>().Click += OnPanelEditClick;
			break;
		case 44:
			SelEndSlider = target.As<Slider>();
			SelEndSlider.ValueChanged += OnSelectionChanged;
			break;
		case 45:
			SelEndText = target.As<TextBlock>();
			break;
		case 46:
			SelStartSlider = target.As<Slider>();
			SelStartSlider.ValueChanged += OnSelectionChanged;
			break;
		case 47:
			SelStartText = target.As<TextBlock>();
			break;
		case 48:
			Waveform = target.As<WaveformControl>();
			break;
		case 49:
			PreviewSlider = target.As<Slider>();
			PreviewSlider.ValueChanged += OnPreviewSliderChanged;
			PreviewSlider.PointerPressed += OnPreviewPointerPressed;
			PreviewSlider.PointerReleased += OnPreviewPointerReleased;
			PreviewSlider.PointerCaptureLost += OnPreviewPointerReleased;
			PreviewSlider.PointerMoved += OnPreviewPointerMoved;
			PreviewSlider.PointerExited += OnPreviewPointerExited;
			break;
		case 50:
			PreviewTimeText = target.As<TextBlock>();
			break;
		case 51:
			PreviewPlayButton = target.As<Button>();
			PreviewPlayButton.Click += OnPreviewPlayClick;
			break;
		case 52:
			target.As<Button>().Click += OnPreviewStopClick;
			break;
		case 53:
			target.As<Button>().Click += OnOpenClick;
			break;
		case 54:
			target.As<Button>().Click += OnLoadCurrentClick;
			break;
		case 55:
			target.As<Button>().Click += OnUndoClick;
			break;
		case 56:
			target.As<Button>().Click += OnRedoClick;
			break;
		case 57:
			target.As<Button>().Click += OnTrimClick;
			break;
		case 58:
			target.As<Button>().Click += OnCutClick;
			break;
		case 59:
			target.As<Button>().Click += OnFadeClick;
			break;
		case 60:
			target.As<Button>().Click += OnNormalizeClick;
			break;
		case 61:
			target.As<Button>().Click += OnDspClick;
			break;
		case 62:
			target.As<Button>().Click += OnExportClick;
			break;
		case 63:
			target.As<Button>().Click += OnResetLayoutClick;
			break;
		}
		_contentLoaded = true;
	}

	[GeneratedCode("Microsoft.UI.Xaml.Markup.Compiler", " 3.0.0.2409")]
	[DebuggerNonUserCode]
	public IComponentConnector GetBindingConnector(int connectionId, object target)
	{
		return null;
	}
}
