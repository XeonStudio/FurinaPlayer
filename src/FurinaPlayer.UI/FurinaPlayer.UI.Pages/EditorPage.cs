using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using FurinaPlayer.UI.Controls;
using FurinaPlayer.UI.Helpers;
using FurinaPlayer.UI.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Markup;
using Microsoft.UI.Xaml.Navigation;
using SonicWave.Core.Models;
using WinRT;
using WinRT.Interop;
using Windows.Storage;
using Windows.Storage.Pickers;

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
		Unloaded += (object _, RoutedEventArgs _) =>
		{
			_previewTimer.Stop();
			ViewModel?.Preview.Stop();
		};
		_layout = new PanelLayoutController(PanelCanvas, 260.0, 150.0, 1500.0);
		_layout.Register(SpectrumPanel, SpectrumHeader);
		_layout.Register(PhasePanel, PhaseHeader);
		_layout.Register(InfoPanel, InfoHeader);
		_layout.Register(EqPanel, EqHeader);
		_layout.Register(EffectsPanel, EffectsHeader);
		_layout.Register(FadePanel, FadeHeader);
		_layout.Register(VstPanel, VstHeader);
		_layout.RegisterResizeHandle(SpectrumResizeHandle, SpectrumPanel);
		_layout.RegisterResizeHandle(PhaseResizeHandle, PhasePanel);
		_layout.RegisterResizeHandle(InfoResizeHandle, InfoPanel);
		_layout.RegisterResizeHandle(EqResizeHandle, EqPanel);
		_layout.RegisterResizeHandle(EffectsResizeHandle, EffectsPanel);
		_layout.RegisterResizeHandle(FadeResizeHandle, FadePanel);
		_layout.RegisterResizeHandle(VstResizeHandle, VstPanel);
		PanelLayoutController layout = _layout;
		layout.LayoutChanged = (Action)Delegate.Combine(layout.LayoutChanged, new Action(SaveLayouts));
		Unloaded += (object _, RoutedEventArgs _) =>
		{
			PanelLayoutController layout2 = _layout;
			layout2.LayoutChanged = (Action)Delegate.Remove(layout2.LayoutChanged, new Action(SaveLayouts));
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
		EditorEq.Gains = ViewModel.Equalizer.Gains;
		RestoreLayouts();
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

	private async void OnPanelEditClick(object sender, RoutedEventArgs e)
	{
		if (sender is FrameworkElement { Tag: string tag })
		{
			Border border = (Border)FindName(tag + "Panel");
			if (!(border == null))
			{
				await _layout.ShowResizeDialogAsync(XamlRoot, border, "调整面板大小");
			}
		}
	}

	private void OnResetLayoutClick(object sender, RoutedEventArgs e)
	{
		ResetPanelLayout(SpectrumPanel, 16.0, 16.0, 720.0, 250.0);
		ResetPanelLayout(PhasePanel, 752.0, 16.0, 440.0, 250.0);
		ResetPanelLayout(InfoPanel, 1208.0, 16.0, 292.0, 250.0);
		ResetPanelLayout(EqPanel, 16.0, 282.0, 720.0, 280.0);
		ResetPanelLayout(EffectsPanel, 752.0, 282.0, 440.0, 280.0);
		ResetPanelLayout(FadePanel, 1208.0, 282.0, 292.0, 280.0);
		ResetPanelLayout(VstPanel, 16.0, 578.0, 720.0, 300.0);
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

	private void RefreshVisuals()
	{
		Waveform.SetPeaks(ViewModel.WaveformPeaks, ViewModel.SelectionStartSeconds / Math.Max(0.001, ViewModel.DurationSeconds), ViewModel.SelectionEndSeconds / Math.Max(0.001, ViewModel.DurationSeconds), 0.0, "#4CC2FF");
		Spectrum.SetPoints(ViewModel.SpectrumPoints, "#4CC2FF");
		Phase.SetPoints(ViewModel.PhasePoints, "#4CC2FF");
		SelStartSlider.Value = ((ViewModel.DurationSeconds > 0.0) ? (ViewModel.SelectionStartSeconds / ViewModel.DurationSeconds * 100.0) : 0.0);
		SelEndSlider.Value = ((ViewModel.DurationSeconds > 0.0) ? (ViewModel.SelectionEndSeconds / ViewModel.DurationSeconds * 100.0) : 100.0);
		UpdateSelectionTexts();
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
		ViewModel.EqGains = EditorEq.Gains;
		await ViewModel.ApplyDspCommand.ExecuteAsync(null);
		EditorEq.Gains = new double[10];
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
		Task.Run(() => ViewModel.ComputePreviewAnalysisAt(TimeSpan.FromMilliseconds(posMs))).ContinueWith((Task<EditorViewModel.PreviewAnalysisResult> t) =>
		{
			_analysisBusy = false;
			if (t.IsCompletedSuccessfully && ViewModel != null && ViewModel.Preview.IsPlaying)
			{
				EditorViewModel.PreviewAnalysisResult result = t.Result;
				Spectrum.SetPoints(result.Spectrum, "#4CC2FF");
				Phase.SetPoints(result.Phase, "#4CC2FF");
				ViewModel.UpdateAnalysisScalars(result.PhaseDifference, result.StereoCorrelation);
			}
		}, TaskScheduler.FromCurrentSynchronizationContext());
	}

	private void UpdatePreviewButton()
	{
		if (PreviewPlayButton?.Content is FontIcon fontIcon)
		{
			fontIcon.Glyph = ((ViewModel != null && ViewModel.Preview.IsPlaying) ? "\ue769" : "\ue768");
		}
	}

	private void OnEditorVstToggled(object sender, RoutedEventArgs e)
	{
		if (ViewModel != null)
		{
			ViewModel.ApplyVst3Command.Execute(null);
		}
	}

	private void OnEditorVstEditorClick(object sender, RoutedEventArgs e)
	{
		if (!(sender is FrameworkElement { Tag: Vst3PluginState tag }))
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
			_editorVstWindow = Vst3EditorWindow.Open(tag.Path, ViewModel.EditorSampleRate, 2, out string error);
			if (_editorVstWindow == null)
			{
				ViewModel.Vst3StatusText = "打开插件界面失败：" + error;
				return;
			}
			_editorVstWindow.Closed += OnEditorVstWindowClosed;
			tag.Enabled = true;
			tag.Bypass = false;
			ViewModel.ApplyVst3Command.Execute(null);
			ViewModel.Vst3StatusText = "已打开插件原生界面：" + tag.Name + "（效果组，参数实时生效）";
		}
		catch (Exception ex)
		{
			ViewModel.Vst3StatusText = "打开插件界面异常：" + ex.Message;
		}
	}

	private void OnEditorVstWindowClosed()
	{
		Vst3EditorWindow w = _editorVstWindow;
		_editorVstWindow = null;
		if (w != null && ViewModel != null)
		{
			Vst3PluginState vst3PluginState = ViewModel.Vst3Plugins.FirstOrDefault((Vst3PluginState p) => p != null && string.Equals(p.Path, w.PluginPath, StringComparison.OrdinalIgnoreCase));
			if (vst3PluginState != null && vst3PluginState.IsActive)
			{
				vst3PluginState.Enabled = false;
				vst3PluginState.Bypass = false;
				ViewModel.ApplyVst3Command.Execute(null);
				ViewModel.Vst3StatusText = "已关闭插件界面并停用《" + vst3PluginState.Name + "》（效果组）";
			}
		}
	}

	private void CloseEditorVstWindow()
	{
		if (_editorVstWindow == null)
		{
			return;
		}
		Vst3EditorWindow editorVstWindow = _editorVstWindow;
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
			target.As<ToggleSwitch>().Toggled += OnEditorVstToggled;
			break;
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
