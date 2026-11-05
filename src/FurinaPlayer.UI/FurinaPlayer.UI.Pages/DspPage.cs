using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using FurinaPlayer.UI.Controls;
using FurinaPlayer.UI.Helpers;
using FurinaPlayer.UI.ViewModels;
using Microsoft.UI;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Markup;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.UI.Xaml.Shapes;
using SonicWave.Audio.Dsp;
using SonicWave.Core.Models;
using WinRT;
using WinRT.Interop;
using Windows.Foundation;
using Windows.Storage;
using Windows.Storage.Pickers;

namespace FurinaPlayer.UI.Pages;

[WinRTRuntimeClassName("Microsoft.UI.Xaml.IUIElementOverrides")]
// [WinRTExposedType(typeof(DspPageWinRTTypeDetails))]
public sealed partial class DspPage : Page, IComponentConnector
{
	[GeneratedCode("Microsoft.UI.Xaml.Markup.Compiler", " 3.0.0.2409")]
	private interface IDspPage_Bindings
	{
		void Initialize();

		void Update();

		void StopTracking();

		void DisconnectUnloadedObject(int connectionId);
	}

	private interface IDspPage_BindingsScopeConnector
	{
		WeakReference Parent { get; set; }

		bool ContainsElement(int connectionId);

		void RegisterForElementConnection(int connectionId, IComponentConnector connector);
	}

	[GeneratedCode("Microsoft.UI.Xaml.Markup.Compiler", " 3.0.0.2409")]
	[DebuggerNonUserCode]
	private static class XamlBindingSetters
	{
		public static void Set_Microsoft_UI_Xaml_UIElement_Visibility(UIElement obj, Visibility value)
		{
			obj.Visibility = value;
		}

		public static void Set_Microsoft_UI_Xaml_Controls_Primitives_RangeBase_Value(RangeBase obj, double value)
		{
			obj.Value = value;
		}

		public static void Set_Microsoft_UI_Xaml_Controls_TextBlock_Text(TextBlock obj, string value, string targetNullValue)
		{
			if (value == null && targetNullValue != null)
			{
				value = targetNullValue;
			}
			obj.Text = value ?? string.Empty;
		}
	}

	[GeneratedCode("Microsoft.UI.Xaml.Markup.Compiler", " 3.0.0.2409")]
	[DebuggerNonUserCode]
	[WinRTRuntimeClassName("Microsoft.UI.Xaml.Markup.IComponentConnector")]
	[WinRTExposedType(typeof(DspPage_DspPage_obj1_BindingsWinRTTypeDetails))]
	private class DspPage_obj1_Bindings : IComponentConnector, IDspPage_Bindings
	{
		[GeneratedCode("Microsoft.UI.Xaml.Markup.Compiler", " 3.0.0.2409")]
		[DebuggerNonUserCode]
		private class DspPage_obj1_BindingsTracking
		{
			private WeakReference<DspPage_obj1_Bindings> weakRefToBindingObj;

			private DspViewModel cache_ViewModel;

			public DspPage_obj1_BindingsTracking(DspPage_obj1_Bindings obj)
			{
				weakRefToBindingObj = new WeakReference<DspPage_obj1_Bindings>(obj);
			}

			public DspPage_obj1_Bindings TryGetBindingObject()
			{
				DspPage_obj1_Bindings target = null;
				if (weakRefToBindingObj != null)
				{
					weakRefToBindingObj.TryGetTarget(out target);
					if (target == null)
					{
						weakRefToBindingObj = null;
						ReleaseAllListeners();
					}
				}
				return target;
			}

			public void ReleaseAllListeners()
			{
				UpdateChildListeners_ViewModel(null);
			}

			public void PropertyChanged_ViewModel(object sender, PropertyChangedEventArgs e)
			{
				DspPage_obj1_Bindings dspPage_obj1_Bindings = TryGetBindingObject();
				if (dspPage_obj1_Bindings == null)
				{
					return;
				}
				string propertyName = e.PropertyName;
				DspViewModel dspViewModel = sender as DspViewModel;
				if (string.IsNullOrEmpty(propertyName))
				{
					if (dspViewModel != null)
					{
						dspPage_obj1_Bindings.Update_ViewModel_IsRendering(dspViewModel.IsRendering, 1073741824);
						dspPage_obj1_Bindings.Update_ViewModel_RenderProgressValue(dspViewModel.RenderProgressValue, 1073741824);
						dspPage_obj1_Bindings.Update_ViewModel_RenderStatusText(dspViewModel.RenderStatusText, 1073741824);
					}
					return;
				}
				switch (propertyName)
				{
				case "IsRendering":
					if (dspViewModel != null)
					{
						dspPage_obj1_Bindings.Update_ViewModel_IsRendering(dspViewModel.IsRendering, 1073741824);
					}
					break;
				case "RenderProgressValue":
					if (dspViewModel != null)
					{
						dspPage_obj1_Bindings.Update_ViewModel_RenderProgressValue(dspViewModel.RenderProgressValue, 1073741824);
					}
					break;
				case "RenderStatusText":
					if (dspViewModel != null)
					{
						dspPage_obj1_Bindings.Update_ViewModel_RenderStatusText(dspViewModel.RenderStatusText, 1073741824);
					}
					break;
				}
			}

			public void UpdateChildListeners_ViewModel(DspViewModel obj)
			{
				if (obj != cache_ViewModel)
				{
					if (cache_ViewModel != null)
					{
						((INotifyPropertyChanged)cache_ViewModel).PropertyChanged -= PropertyChanged_ViewModel;
						cache_ViewModel = null;
					}
					if (obj != null)
					{
						cache_ViewModel = obj;
						((INotifyPropertyChanged)obj).PropertyChanged += PropertyChanged_ViewModel;
					}
				}
			}
		}

		private DspPage dataRoot;

		private bool initialized;

		private const int NOT_PHASED = int.MinValue;

		private const int DATA_CHANGED = 1073741824;

		private Border obj2;

		private ProgressBar obj3;

		private TextBlock obj4;

		private DspPage_obj1_BindingsTracking bindingsTracking;

		public DspPage_obj1_Bindings()
		{
			bindingsTracking = new DspPage_obj1_BindingsTracking(this);
		}

		public void Connect(int connectionId, object target)
		{
			switch (connectionId)
			{
			case 2:
				obj2 = target.As<Border>();
				break;
			case 3:
				obj3 = target.As<ProgressBar>();
				break;
			case 4:
				obj4 = target.As<TextBlock>();
				break;
			}
		}

		[GeneratedCode("Microsoft.UI.Xaml.Markup.Compiler", " 3.0.0.2409")]
		[DebuggerNonUserCode]
		public IComponentConnector GetBindingConnector(int connectionId, object target)
		{
			return null;
		}

		public void Initialize()
		{
			if (!initialized)
			{
				Update();
			}
		}

		public void Update()
		{
			Update_(dataRoot, int.MinValue);
			initialized = true;
		}

		public void StopTracking()
		{
			bindingsTracking.ReleaseAllListeners();
			initialized = false;
		}

		public void DisconnectUnloadedObject(int connectionId)
		{
			throw new ArgumentException("No unloadable elements to disconnect.");
		}

		public bool SetDataRoot(object newDataRoot)
		{
			bindingsTracking.ReleaseAllListeners();
			if (newDataRoot != null)
			{
				dataRoot = newDataRoot.As<DspPage>();
				return true;
			}
			return false;
		}

		public void Activated(object obj, WindowActivatedEventArgs data)
		{
			Initialize();
		}

		public void Loading(FrameworkElement src, object data)
		{
			Initialize();
		}

		private void Update_(DspPage obj, int phase)
		{
			if (obj != null && (phase & -1073741823) != 0)
			{
				Update_ViewModel(obj.ViewModel, phase);
			}
		}

		private void Update_ViewModel(DspViewModel obj, int phase)
		{
			bindingsTracking.UpdateChildListeners_ViewModel(obj);
			if (obj != null && (phase & -1073741823) != 0)
			{
				Update_ViewModel_IsRendering(obj.IsRendering, phase);
				Update_ViewModel_RenderProgressValue(obj.RenderProgressValue, phase);
				Update_ViewModel_RenderStatusText(obj.RenderStatusText, phase);
			}
		}

		private void Update_ViewModel_IsRendering(bool obj, int phase)
		{
			if ((phase & -1073741823) != 0)
			{
				Update_ViewModel_IsRendering_Cast_IsRendering_To_Visibility((!obj) ? Visibility.Collapsed : Visibility.Visible, phase);
			}
		}

		private void Update_ViewModel_IsRendering_Cast_IsRendering_To_Visibility(Visibility obj, int phase)
		{
			if ((phase & -1073741823) != 0)
			{
				XamlBindingSetters.Set_Microsoft_UI_Xaml_UIElement_Visibility(obj2, obj);
			}
		}

		private void Update_ViewModel_RenderProgressValue(double obj, int phase)
		{
			if ((phase & -1073741823) != 0)
			{
				XamlBindingSetters.Set_Microsoft_UI_Xaml_Controls_Primitives_RangeBase_Value(obj3, obj);
			}
		}

		private void Update_ViewModel_RenderStatusText(string obj, int phase)
		{
			if ((phase & -1073741823) != 0)
			{
				XamlBindingSetters.Set_Microsoft_UI_Xaml_Controls_TextBlock_Text(obj4, obj, null);
			}
		}
	}

	private static readonly string[] PresetNames = new string[9] { "平坦", "流行", "摇滚", "爵士", "古典", "电子", "乡村", "低音增强", "高音增强" };

	private static readonly string[] ReverbNames = new string[5] { "房间", "大厅", "板式", "录音室", "浴室" };

	private readonly PanelLayoutController _layout;

	private Vst3EditorWindow? _vst3EditorWindow;

	[GeneratedCode("Microsoft.UI.Xaml.Markup.Compiler", " 3.0.0.2409")]
	private TextBlock RenderRemainingText;

	[GeneratedCode("Microsoft.UI.Xaml.Markup.Compiler", " 3.0.0.2409")]
	private Canvas PanelCanvas;

	[GeneratedCode("Microsoft.UI.Xaml.Markup.Compiler", " 3.0.0.2409")]
	private Border EqPanel;

	[GeneratedCode("Microsoft.UI.Xaml.Markup.Compiler", " 3.0.0.2409")]
	private Border EffectsPanel;

	[GeneratedCode("Microsoft.UI.Xaml.Markup.Compiler", " 3.0.0.2409")]
	private Border TimbrePanel;

	[GeneratedCode("Microsoft.UI.Xaml.Markup.Compiler", " 3.0.0.2409")]
	private Border Vst3Panel;

	[GeneratedCode("Microsoft.UI.Xaml.Markup.Compiler", " 3.0.0.2409")]
	private Grid Vst3Header;

	[GeneratedCode("Microsoft.UI.Xaml.Markup.Compiler", " 3.0.0.2409")]
	private Grid Vst3ResizeHandle;

	[GeneratedCode("Microsoft.UI.Xaml.Markup.Compiler", " 3.0.0.2409")]
	private ListView Vst3PluginsList;

	[GeneratedCode("Microsoft.UI.Xaml.Markup.Compiler", " 3.0.0.2409")]
	private ListView Vst3FoldersList;

	[GeneratedCode("Microsoft.UI.Xaml.Markup.Compiler", " 3.0.0.2409")]
	private Grid TimbreHeader;

	[GeneratedCode("Microsoft.UI.Xaml.Markup.Compiler", " 3.0.0.2409")]
	private Grid TimbreResizeHandle;

	[GeneratedCode("Microsoft.UI.Xaml.Markup.Compiler", " 3.0.0.2409")]
	private Grid EffectsHeader;

	[GeneratedCode("Microsoft.UI.Xaml.Markup.Compiler", " 3.0.0.2409")]
	private Grid EffectsResizeHandle;

	[GeneratedCode("Microsoft.UI.Xaml.Markup.Compiler", " 3.0.0.2409")]
	private ComboBox ReverbPresetCombo;

	[GeneratedCode("Microsoft.UI.Xaml.Markup.Compiler", " 3.0.0.2409")]
	private Grid EqHeader;

	[GeneratedCode("Microsoft.UI.Xaml.Markup.Compiler", " 3.0.0.2409")]
	private Grid EqResizeHandle;

	[GeneratedCode("Microsoft.UI.Xaml.Markup.Compiler", " 3.0.0.2409")]
	private EqualizerControl DspEq;

	[GeneratedCode("Microsoft.UI.Xaml.Markup.Compiler", " 3.0.0.2409")]
	private ComboBox EqPresetCombo;

	[GeneratedCode("Microsoft.UI.Xaml.Markup.Compiler", " 3.0.0.2409")]
	private Canvas EqCurveCanvas;

	[GeneratedCode("Microsoft.UI.Xaml.Markup.Compiler", " 3.0.0.2409")]
	private bool _contentLoaded;

	private Border? _compressorPanel;
	private Border? _spatialCrossfeedPanel;
	private Border? _convolutionPanel;
	private Border? _advancedPanel;
	private Border? _replayGainHeadroomPanel;
	private Border? _rackOrderPanel;
	private ProgressBar? _grProgressBar;
	private TextBlock? _grValueText;
	private TextBlock? _irInfoText;
	private DispatcherQueueTimer? _dspMeterTimer;
	private bool _echoPanelsInitialized;
	private string _currentDspCategory = "param_eq";
	private readonly List<Button> _categoryTabButtons = new();
	private Border? _parametricEqWorkbench;
	private ParametricEqCurveControl? _eqCurveControl;
	private StackPanel? _eqBandPillsStack;
	private ComboBox? _filterTypeCombo;
	private Slider? _freqSlider;
	private TextBlock? _freqValueTb;
	private Slider? _gainSlider;
	private TextBlock? _gainValueTb;
	private Slider? _qSlider;
	private TextBlock? _qValueTb;
	private Slider? _preampSlider;
	private TextBlock? _preampValueTb;
	private CheckBox? _bandEnableCb;
	private TextBlock? _bandIndexBadge;
	private Grid? _workbenchHost;
	private readonly List<Button> _sidebarNavButtons = new();
	private Border? _headroomWorkbench;
	private TextBlock? _headroomNavSubTb;
	private ToggleSwitch? _headroomPowerSwitch;
	private TextBlock? _headroomGainBigTb;
	private TextBlock? _headroomAmpRatioTb;
	private Slider? _headroomGainSlider;
	private CheckBox? _headroomDynamicLoudnessCb;
	private Button? _headroomAuditionBtn;
	private readonly List<Button> _headroomPresetButtons = new();

	public static Brush CreateWhiteAcrylicBrush(double tintOpacity = 0.75, byte fallbackAlpha = 210)
	{
		try
		{
			return new AcrylicBrush
			{
				TintColor = Windows.UI.Color.FromArgb(255, 255, 255, 255),
				TintOpacity = tintOpacity,
				TintLuminosityOpacity = 0.85,
				FallbackColor = Windows.UI.Color.FromArgb(fallbackAlpha, 250, 252, 255)
			};
		}
		catch
		{
			return new SolidColorBrush(Windows.UI.Color.FromArgb(fallbackAlpha, 250, 252, 255));
		}
	}

	public static Brush CreateAcrylicCardBrush(double tintOpacity = 0.88, byte fallbackAlpha = 235)
	{
		try
		{
			return new AcrylicBrush
			{
				TintColor = Windows.UI.Color.FromArgb(255, 255, 255, 255),
				TintOpacity = tintOpacity,
				TintLuminosityOpacity = 0.90,
				FallbackColor = Windows.UI.Color.FromArgb(fallbackAlpha, 255, 255, 255)
			};
		}
		catch
		{
			return new SolidColorBrush(Windows.UI.Color.FromArgb(fallbackAlpha, 255, 255, 255));
		}
	}

	[GeneratedCode("Microsoft.UI.Xaml.Markup.Compiler", " 3.0.0.2409")]
	private IDspPage_Bindings Bindings;

	public DspViewModel ViewModel { get; set; }

	public DspPage()
	{
		InitializeComponent();
		Background = CreateWhiteAcrylicBrush(0.65, 185);
		EqCurveCanvas.Height = 70.0;
		if (PanelCanvas != null)
		{
			PanelCanvas.Width = 2460;
			PanelCanvas.Height = 1200;
		}
		_layout = new PanelLayoutController(PanelCanvas, 420.0, 240.0);
		_layout.Register(EqPanel, EqHeader);
		_layout.Register(EffectsPanel, EffectsHeader);
		_layout.Register(TimbrePanel, TimbreHeader);
		_layout.Register(Vst3Panel, Vst3Header);
		_layout.RegisterResizeHandle(EqResizeHandle, EqPanel);
		_layout.RegisterResizeHandle(EffectsResizeHandle, EffectsPanel);
		_layout.RegisterResizeHandle(TimbreResizeHandle, TimbrePanel);
		_layout.RegisterResizeHandle(Vst3ResizeHandle, Vst3Panel);

		InitializeEchoDspPanels();
		Loaded += (s, e) => FluentIconFactory.UpgradeButtonsInVisualTree(this);

		PanelLayoutController layout = _layout;
		layout.LayoutChanged = (Action)Delegate.Combine(layout.LayoutChanged, new Action(SaveLayouts));
		Unloaded += (object _, RoutedEventArgs _) =>
		{
			StopDspMeterTimer();
			PanelLayoutController layout2 = _layout;
			layout2.LayoutChanged = (Action)Delegate.Remove(layout2.LayoutChanged, new Action(SaveLayouts));
		};
	}

	public DspPage(DspViewModel viewModel)
		: this()
	{
		ViewModel = viewModel;
		DataContext = viewModel;
	}

	protected override void OnNavigatedTo(NavigationEventArgs e)
	{
		base.OnNavigatedTo(e);
		if (e.Parameter is DspViewModel dspViewModel)
		{
			ViewModel = dspViewModel;
			DataContext = dspViewModel;
		}
		DspEq.Gains = ViewModel.EqGains;
		EqPresetCombo.SelectedIndex = Math.Max(0, Array.IndexOf(PresetNames, ViewModel.EqPresetName));
		ReverbPresetCombo.SelectedIndex = Math.Max(0, Array.IndexOf(ReverbNames, ViewModel.ReverbPreset));
		DrawEqCurve(ViewModel.EqGains);
		RestoreLayouts();
		OptimizeVst3PanelLayout();
		FluentIconFactory.UpgradeButtonsInVisualTree(this);
		if (_currentDspCategory != "free")
		{
			SetDspViewCategory(_currentDspCategory);
		}
	}

	private void RestoreLayouts(bool forceReset = false)
	{
		Dictionary<string, double[]> layouts = forceReset ? new Dictionary<string, double[]>() : (ViewModel?.LoadPanelLayouts() ?? new Dictionary<string, double[]>());
		// 检测是否存在病态重叠堆叠（例如两个或多个面板 X、Y 坐标极其接近 <= 30px）
		if (!forceReset && layouts.Count > 1)
		{
			int overlapCount = 0;
			var posList = new List<(double X, double Y)>();
			foreach (var kv in layouts)
			{
				if (kv.Value != null && kv.Value.Length >= 2)
				{
					double x = kv.Value[0];
					double y = kv.Value[1];
					if (posList.Any(p => Math.Abs(p.X - x) < 30.0 && Math.Abs(p.Y - y) < 30.0))
					{
						overlapCount++;
					}
					posList.Add((x, y));
				}
			}
			if (overlapCount >= 2)
			{
				layouts.Clear();
			}
		}

		if (PanelCanvas != null)
		{
			PanelCanvas.Width = 3050;
			PanelCanvas.Height = 1100;
		}

		// 第一排（顶部，Y = 16，标准高度 440）
		RestorePanel(_rackOrderPanel, "dsp.rack", layouts, 20.0, 16.0, 620.0, 440.0);
		RestorePanel(EqPanel, "dsp.eq", layouts, 660.0, 16.0, 580.0, 440.0);
		RestorePanel(_convolutionPanel, "dsp.convolution", layouts, 1260.0, 16.0, 570.0, 440.0);
		RestorePanel(_replayGainHeadroomPanel, "dsp.replaygain", layouts, 1850.0, 16.0, 570.0, 440.0);

		// 第二排（底部，Y = 476，标准高度 480）
		RestorePanel(Vst3Panel, "dsp.vst3", layouts, 20.0, 476.0, 620.0, 480.0);
		RestorePanel(_spatialCrossfeedPanel, "dsp.spatial", layouts, 660.0, 476.0, 580.0, 480.0);
		RestorePanel(_compressorPanel, "dsp.compressor", layouts, 1260.0, 476.0, 570.0, 480.0);
		RestorePanel(_advancedPanel, "dsp.advanced", layouts, 1850.0, 476.0, 570.0, 480.0);

		RestorePanel(EffectsPanel, "dsp.effects", layouts, 2440.0, 16.0, 560.0, 440.0);
		RestorePanel(TimbrePanel, "dsp.timbre", layouts, 2440.0, 476.0, 560.0, 480.0);
	}

	private void RestorePanel(Border panel, string key, Dictionary<string, double[]> layouts, double defLeft, double defTop, double defWidth, double defHeight)
	{
		if (panel == null) return;
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
		if (_currentDspCategory != "free") return;
		if (ViewModel != null)
		{
			var dict = new Dictionary<string, double[]>
			{
				["dsp.eq"] = PanelState(EqPanel),
				["dsp.effects"] = PanelState(EffectsPanel),
				["dsp.timbre"] = PanelState(TimbrePanel),
				["dsp.vst3"] = PanelState(Vst3Panel)
			};
			if (_compressorPanel != null) dict["dsp.compressor"] = PanelState(_compressorPanel);
			if (_spatialCrossfeedPanel != null) dict["dsp.spatial"] = PanelState(_spatialCrossfeedPanel);
			if (_convolutionPanel != null) dict["dsp.convolution"] = PanelState(_convolutionPanel);
			if (_advancedPanel != null) dict["dsp.advanced"] = PanelState(_advancedPanel);
			if (_replayGainHeadroomPanel != null) dict["dsp.replaygain"] = PanelState(_replayGainHeadroomPanel);
			if (_rackOrderPanel != null) dict["dsp.rack"] = PanelState(_rackOrderPanel);
			ViewModel.SavePanelLayouts(dict);
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

	private void OnDspEqGainsChanged(double[] gains)
	{
		if (ViewModel != null)
		{
			ViewModel.ApplyEqGains(gains);
			DrawEqCurve(gains);
		}
	}

	private void OnEqPresetChanged(object sender, SelectionChangedEventArgs e)
	{
		if (ViewModel != null && !(EqPresetCombo == null) && EqPresetCombo.SelectedItem is ComboBoxItem { Content: string content })
		{
			ViewModel.ApplyPresetCommand.Execute(content);
			DspEq.Gains = ViewModel.EqGains;
			DrawEqCurve(ViewModel.EqGains);
		}
	}

	private void OnReverbPresetChanged(object sender, SelectionChangedEventArgs e)
	{
		if (ViewModel != null && !(ReverbPresetCombo == null) && ReverbPresetCombo.SelectedItem is ComboBoxItem { Content: string content })
		{
			ViewModel.ReverbPreset = content;
		}
	}

	private void OnResetClick(object sender, RoutedEventArgs e)
	{
		if (ViewModel != null)
		{
			ViewModel.ResetAllCommand.Execute(null);
			DspEq.Gains = new double[10];
			DrawEqCurve(new double[10]);
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
				SaveLayouts();
			}
		}
	}

	private void OnResetLayoutClick(object sender, RoutedEventArgs e)
	{
		RestoreLayouts(forceReset: true);
		SaveLayouts();
	}

	private static void ResetPanelLayout(Border panel, double left, double top, double width, double height)
	{
		Canvas.SetLeft(panel, left);
		Canvas.SetTop(panel, top);
		panel.Width = width;
		panel.Height = height;
	}

	private void OnEqCanvasSizeChanged(object sender, SizeChangedEventArgs e)
	{
		if (ViewModel != null)
		{
			DrawEqCurve(ViewModel.EqGains);
		}
	}

	private async void OnAddVst3FolderClick(object sender, RoutedEventArgs e)
	{
		try
		{
			FolderPicker folderPicker = new FolderPicker
			{
				FileTypeFilter = { "*" },
				SuggestedStartLocation = PickerLocationId.ComputerFolder
			};
			nint windowHandle = WindowNative.GetWindowHandle(WindowManager.CurrentWindow);
			InitializeWithWindow.Initialize(folderPicker, windowHandle);
			StorageFolder storageFolder = await folderPicker.PickSingleFolderAsync();
			if (storageFolder != null)
			{
				ViewModel.AddVst3FolderCommand.Execute(storageFolder.Path);
			}
		}
		catch (Exception ex)
		{
			ViewModel.Vst3StatusText = "添加目录失败：" + ex.Message;
		}
	}

	private void OnRemoveVst3FolderClick(object sender, RoutedEventArgs e)
	{
		if (Vst3FoldersList.SelectedItem is string parameter)
		{
			ViewModel.RemoveVst3FolderCommand.Execute(parameter);
		}
	}

	private void OnScanVst3Click(object sender, RoutedEventArgs e)
	{
		ViewModel.ScanVst3Command.Execute(null);
	}

	private void OnVst3Toggled(object sender, RoutedEventArgs e)
	{
		ViewModel.ApplyVst3Command.Execute(null);
		if (_vst3EditorWindow != null)
		{
			DspViewModel viewModel = ViewModel;
			if (viewModel == null || !viewModel.Vst3Plugins.Any((Vst3PluginState p) => p?.IsActive ?? false))
			{
				CloseVst3Editor();
			}
		}
	}

	protected override void OnNavigatedFrom(NavigationEventArgs e)
	{
		base.OnNavigatedFrom(e);
		CloseVst3Editor();
	}

	private void OnVst3ParamsClick(object sender, RoutedEventArgs e)
	{
		if (!(sender is Button { Tag: Vst3PluginState tag }))
		{
			return;
		}
		try
		{
			if (_vst3EditorWindow != null && string.Equals(_vst3EditorWindow.PluginPath, tag.Path, StringComparison.OrdinalIgnoreCase))
			{
				_vst3EditorWindow.BringToFront();
				return;
			}
			CloseVst3Editor();
			int sampleRate = ((ViewModel.EngineSampleRate > 0) ? ViewModel.EngineSampleRate : 44100);
			Vst3EditorWindow? editor = Vst3EditorWindow.Open(tag.Path, sampleRate, 2, out string error);
			if (editor == null)
			{
				ViewModel.Vst3StatusText = "打开插件界面失败：" + error;
				return;
			}
			_vst3EditorWindow = editor;
			editor.Closed += () => OnVst3EditorClosed(editor);
			foreach (Vst3PluginState vst3Plugin in ViewModel.Vst3Plugins)
			{
				if (vst3Plugin != null && vst3Plugin != tag)
				{
					vst3Plugin.Enabled = false;
					vst3Plugin.Bypass = false;
				}
			}
			tag.Enabled = true;
			tag.Bypass = false;
			ViewModel.ApplyVst3Command.Execute(null);
			ViewModel.Vst3StatusText = "已打开插件原生界面：" + tag.Name + "，实时处理与播放当前歌曲";
		}
		catch (Exception ex)
		{
			ViewModel.Vst3StatusText = "打开插件界面异常：" + ex.Message;
		}
	}

	private void OnVst3EditorClosed(Vst3EditorWindow? w)
	{
		if (w == null)
		{
			w = _vst3EditorWindow;
		}
		if (_vst3EditorWindow == w)
		{
			_vst3EditorWindow = null;
		}
		if (w != null && ViewModel != null)
		{
			Vst3PluginState vst3PluginState = ViewModel.Vst3Plugins.FirstOrDefault((Vst3PluginState p) => p != null && string.Equals(p.Path, w.PluginPath, StringComparison.OrdinalIgnoreCase));
			if (vst3PluginState != null && vst3PluginState.IsActive)
			{
				vst3PluginState.Enabled = false;
				vst3PluginState.Bypass = false;
				ViewModel.ApplyVst3Command.Execute(null);
				ViewModel.Vst3StatusText = "已关闭插件界面并停用《" + vst3PluginState.Name + "》：歌曲继续正常播放（可在列表中重新启用）";
			}
		}
	}

	private void CloseVst3Editor()
	{
		if (_vst3EditorWindow == null)
		{
			return;
		}
		Vst3EditorWindow vst3EditorWindow = _vst3EditorWindow;
		_vst3EditorWindow = null;
		try
		{
			vst3EditorWindow.Close();
		}
		catch
		{
		}
		OnVst3EditorClosed(vst3EditorWindow);
	}

	private void DrawEqCurve(double[] gains)
	{
		EqCurveCanvas.Children.Clear();
		double num = ((EqCurveCanvas.ActualWidth > 0.0) ? EqCurveCanvas.ActualWidth : 520.0);
		double num2 = ((EqCurveCanvas.ActualHeight > 0.0) ? EqCurveCanvas.ActualHeight : 150.0);
		if (num <= 20.0 || num2 <= 20.0)
		{
			return;
		}
		double[] bandFrequencies = Equalizer.BandFrequencies;
		double d = 20000.0;
		double num3 = Math.Log10(20.0);
		double num4 = Math.Log10(d) - num3;
		double num5 = 20.0;
		double num6 = 14.0;
		double num7 = num2 / 2.0;
		Line item = new Line
		{
			X1 = num5,
			Y1 = num7,
			X2 = num - num5,
			Y2 = num7,
			Stroke = new SolidColorBrush(Colors.Gray),
			Opacity = 0.35,
			StrokeThickness = 1.0
		};
		EqCurveCanvas.Children.Add(item);
		List<Point> list = new List<Point>();
		for (int i = 0; i < bandFrequencies.Length; i++)
		{
			double x = num5 + (Math.Log10(bandFrequencies[i]) - num3) / num4 * (num - num5 * 2.0);
			double num8 = ((i < gains.Length) ? gains[i] : 0.0);
			double y = num7 - num8 / 12.0 * (num7 - num6);
			list.Add(new Point(x, y));
		}
		Polyline polyline = new Polyline
		{
			Stroke = new SolidColorBrush(Colors.DeepSkyBlue),
			StrokeThickness = 2.5,
			StrokeLineJoin = PenLineJoin.Round
		};
		foreach (Point item2 in list)
		{
			polyline.Points.Add(item2);
		}
		EqCurveCanvas.Children.Add(polyline);
	}

	private void InitializeEchoDspPanels()
	{
		if (_echoPanelsInitialized || PanelCanvas == null) return;
		_echoPanelsInitialized = true;

		try
		{
			_compressorPanel = CreateCompressorPanel();
			_spatialCrossfeedPanel = CreateSpatialCrossfeedPanel();
			_convolutionPanel = CreateConvolutionPanel();
			_advancedPanel = CreateAdvancedDspPanel();
			_replayGainHeadroomPanel = CreateReplayGainHeadroomPanel();
			_rackOrderPanel = CreateDspRackOrderPanel();

			PanelCanvas.Children.Add(_compressorPanel);
			PanelCanvas.Children.Add(_spatialCrossfeedPanel);
			PanelCanvas.Children.Add(_convolutionPanel);
			PanelCanvas.Children.Add(_advancedPanel);
			PanelCanvas.Children.Add(_replayGainHeadroomPanel);
			PanelCanvas.Children.Add(_rackOrderPanel);

			SetupAutoEqButton();

			if (Content is Grid rootGrid)
			{
				SetupUnifiedEchoDspLayout(rootGrid);
			}
		}
		catch (Exception ex)
		{
			Debug.WriteLine($"InitializeEchoDspPanels failed: {ex.Message}");
		}
	}

	private Border CreatePanelCard(string title, string glyph, FrameworkElement content, out Grid headerGrid, out Grid resizeGrid, double width, double height)
	{
		var panel = new Border
		{
			Width = width,
			Height = height,
			CornerRadius = new CornerRadius(12),
			Padding = new Thickness(12),
			Background = (Brush)Application.Current.Resources["CardBackgroundFillColorDefaultBrush"],
			BorderBrush = (Brush)Application.Current.Resources["CardStrokeColorDefaultBrush"],
			BorderThickness = new Thickness(1)
		};

		var mainGrid = new Grid { RowSpacing = 10 };
		mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
		mainGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1.0, GridUnitType.Star) });

		headerGrid = new Grid
		{
			Background = new SolidColorBrush(Colors.Transparent),
			ColumnSpacing = 6
		};
		headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
		headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.0, GridUnitType.Star) });

		FluentIconKind headerIconKind = glyph switch
		{
			"\uE9D9" or "\uE994" => FluentIconKind.Dynamics,
			"\uE950" => FluentIconKind.Spatial,
			"\uE8B7" => FluentIconKind.FolderScan,
			"\uE774" => FluentIconKind.HiRes,
			"\uE8B8" => FluentIconKind.Rack,
			_ => FluentIconKind.Eq
		};
		headerGrid.Children.Add(FluentIconFactory.Create(headerIconKind, 20));

		var titleTb = new TextBlock
		{
			Text = title,
			FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
			FontSize = 15,
			VerticalAlignment = VerticalAlignment.Center
		};
		Grid.SetColumn(titleTb, 1);
		headerGrid.Children.Add(titleTb);

		var scroll = new ScrollViewer
		{
			VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
			HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
			Content = content
		};
		Grid.SetRow(scroll, 1);

		resizeGrid = new Grid
		{
			Width = 20,
			Height = 20,
			HorizontalAlignment = HorizontalAlignment.Right,
			VerticalAlignment = VerticalAlignment.Bottom,
			Background = new SolidColorBrush(Colors.Transparent)
		};
		ToolTipService.SetToolTip(resizeGrid, "拖拽调整大小");
		var resizePath = new Path
		{
			Data = (Geometry)XamlReader.Load("<Geometry xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'>M0,20 L20,0 L20,20 Z</Geometry>"),
			Fill = (Brush)Application.Current.Resources["TextFillColorSecondaryBrush"],
			Opacity = 0.45
		};
		resizeGrid.Children.Add(resizePath);
		Grid.SetRow(resizeGrid, 1);

		mainGrid.Children.Add(headerGrid);
		mainGrid.Children.Add(scroll);
		mainGrid.Children.Add(resizeGrid);
		panel.Child = mainGrid;

		_layout?.Register(panel, headerGrid);
		_layout?.RegisterResizeHandle(resizeGrid, panel);

		return panel;
	}

	private Border CreateCompressorPanel()
	{
		var sp = new StackPanel { Spacing = 10 };

		var toggle = new ToggleSwitch
		{
			Header = "启用母带动态压缩器",
			IsOn = ViewModel?.EchoCompressorEnabled ?? false
		};
		toggle.Toggled += (s, e) => { if (ViewModel != null) ViewModel.EchoCompressorEnabled = toggle.IsOn; };
		sp.Children.Add(toggle);

		var sThresh = new Slider { Header = "阈值 Threshold (-60 ~ 0 dB)", Minimum = -60, Maximum = 0, StepFrequency = 0.5, Value = ViewModel?.EchoCompressorThresholdDb ?? -18.0 };
		sThresh.ValueChanged += (s, e) => { if (ViewModel != null) ViewModel.EchoCompressorThresholdDb = e.NewValue; };
		sp.Children.Add(sThresh);

		var sRatio = new Slider { Header = "压缩比 Ratio (1:1 ~ 20:1)", Minimum = 1.0, Maximum = 20.0, StepFrequency = 0.1, Value = ViewModel?.EchoCompressorRatio ?? 3.0 };
		sRatio.ValueChanged += (s, e) => { if (ViewModel != null) ViewModel.EchoCompressorRatio = e.NewValue; };
		sp.Children.Add(sRatio);

		var gridTime = new Grid { ColumnSpacing = 12 };
		gridTime.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.0, GridUnitType.Star) });
		gridTime.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.0, GridUnitType.Star) });
		var sAttack = new Slider { Header = "起音 Attack (ms)", Minimum = 0.1, Maximum = 100.0, StepFrequency = 0.5, Value = ViewModel?.EchoCompressorAttackMs ?? 15.0 };
		sAttack.ValueChanged += (s, e) => { if (ViewModel != null) ViewModel.EchoCompressorAttackMs = e.NewValue; };
		var sRelease = new Slider { Header = "释放 Release (ms)", Minimum = 10, Maximum = 1000, StepFrequency = 5, Value = ViewModel?.EchoCompressorReleaseMs ?? 120.0 };
		sRelease.ValueChanged += (s, e) => { if (ViewModel != null) ViewModel.EchoCompressorReleaseMs = e.NewValue; };
		Grid.SetColumn(sAttack, 0);
		Grid.SetColumn(sRelease, 1);
		gridTime.Children.Add(sAttack);
		gridTime.Children.Add(sRelease);
		sp.Children.Add(gridTime);

		var gridKnee = new Grid { ColumnSpacing = 12 };
		gridKnee.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.0, GridUnitType.Star) });
		gridKnee.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.0, GridUnitType.Star) });
		var sKnee = new Slider { Header = "软拐点 Knee (dB)", Minimum = 0, Maximum = 24, StepFrequency = 0.5, Value = ViewModel?.EchoCompressorKneeDb ?? 6.0 };
		sKnee.ValueChanged += (s, e) => { if (ViewModel != null) ViewModel.EchoCompressorKneeDb = e.NewValue; };
		var sMakeup = new Slider { Header = "增益补偿 Makeup (dB)", Minimum = 0, Maximum = 24, StepFrequency = 0.5, Value = ViewModel?.EchoCompressorMakeupDb ?? 0.0 };
		sMakeup.ValueChanged += (s, e) => { if (ViewModel != null) ViewModel.EchoCompressorMakeupDb = e.NewValue; };
		Grid.SetColumn(sKnee, 0);
		Grid.SetColumn(sMakeup, 1);
		gridKnee.Children.Add(sKnee);
		gridKnee.Children.Add(sMakeup);
		sp.Children.Add(gridKnee);

		var grStack = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 10, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 4, 0, 0) };
		grStack.Children.Add(new TextBlock { Text = "实时减益 GR:", FontSize = 12, Opacity = 0.8, VerticalAlignment = VerticalAlignment.Center });
		_grProgressBar = new ProgressBar { Minimum = 0, Maximum = 24, Height = 10, Width = 220, VerticalAlignment = VerticalAlignment.Center, Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 255, 90, 95)) };
		_grValueText = new TextBlock { Text = "0.0 dB", FontSize = 12, FontWeight = Microsoft.UI.Text.FontWeights.SemiBold, VerticalAlignment = VerticalAlignment.Center };
		grStack.Children.Add(_grProgressBar);
		grStack.Children.Add(_grValueText);
		sp.Children.Add(grStack);

		return CreatePanelCard("母带动态压缩器 (FurinaPlayer Compressor)", "\uE9D9", sp, out var h, out var r, 600, 370);
	}

	private Border CreateSpatialCrossfeedPanel()
	{
		var sp = new StackPanel { Spacing = 10 };

		var toggle = new ToggleSwitch
		{
			Header = "启用双耳互馈 (Crossfeed)",
			IsOn = ViewModel?.EchoCrossfeedEnabled ?? false
		};
		toggle.Toggled += (s, e) => { if (ViewModel != null) ViewModel.EchoCrossfeedEnabled = toggle.IsOn; };
		sp.Children.Add(toggle);

		var comboMode = new ComboBox { Header = "互馈滤波曲线", MinWidth = 200, SelectedIndex = 0 };
		comboMode.Items.Add(new ComboBoxItem { Content = "Chu Moy (经典自然)" });
		comboMode.Items.Add(new ComboBoxItem { Content = "Jan Meier (宽深舒适)" });
		comboMode.Items.Add(new ComboBoxItem { Content = "Linkwitz (温暖声场)" });
		sp.Children.Add(comboMode);

		var sAmount = new Slider { Header = "互馈深度 Amount (0% ~ 100%)", Minimum = 0.0, Maximum = 1.0, StepFrequency = 0.05, Value = ViewModel?.EchoCrossfeedAmount ?? 0.35 };
		sAmount.ValueChanged += (s, e) => { if (ViewModel != null) ViewModel.EchoCrossfeedAmount = e.NewValue; };
		sp.Children.Add(sAmount);

		var sCutoff = new Slider { Header = "低通截止 Cutoff (400 ~ 1000 Hz)", Minimum = 400, Maximum = 1000, StepFrequency = 10, Value = ViewModel?.EchoCrossfeedCutoffHz ?? 650.0 };
		sCutoff.ValueChanged += (s, e) => { if (ViewModel != null) ViewModel.EchoCrossfeedCutoffHz = e.NewValue; };
		sp.Children.Add(sCutoff);

		var sWidth = new Slider { Header = "立体声宽度 Stereo Width (0% 单声道 ~ 200% 超宽)", Minimum = 0.0, Maximum = 2.0, StepFrequency = 0.05, Value = ViewModel?.EchoStereoWidth ?? 1.0 };
		sWidth.ValueChanged += (s, e) => { if (ViewModel != null) ViewModel.EchoStereoWidth = e.NewValue; };
		sp.Children.Add(sWidth);

		var comboMatrix = new ComboBox { Header = "声道矩阵模式 (Channel Matrix)", MinWidth = 200 };
		comboMatrix.Items.Add(new ComboBoxItem { Content = "正常立体声 (Normal)" });
		comboMatrix.Items.Add(new ComboBoxItem { Content = "左右声道对调 (Swap L/R)" });
		comboMatrix.Items.Add(new ComboBoxItem { Content = "单声道混合 (Mono Mix)" });
		comboMatrix.Items.Add(new ComboBoxItem { Content = "反相 (Invert Phase)" });
		comboMatrix.SelectedIndex = Math.Clamp(ViewModel?.EchoChannelMatrixPreset ?? 0, 0, 3);
		comboMatrix.SelectionChanged += (s, e) => { if (ViewModel != null && comboMatrix.SelectedIndex >= 0) ViewModel.EchoChannelMatrixPreset = comboMatrix.SelectedIndex; };
		sp.Children.Add(comboMatrix);

		return CreatePanelCard("空间声场与双耳互馈 (Crossfeed & Spatial)", "\uE950", sp, out var h, out var r, 560, 370);
	}

	private Border CreateConvolutionPanel()
	{
		var sp = new StackPanel { Spacing = 12 };

		var toggle = new ToggleSwitch
		{
			Header = "启用 FIR 卷积耳机/房间校正",
			IsOn = ViewModel?.EchoConvolutionEnabled ?? false
		};
		toggle.Toggled += (s, e) => { if (ViewModel != null) ViewModel.EchoConvolutionEnabled = toggle.IsOn; };
		sp.Children.Add(toggle);

		var btnLoad = new Button
		{
			Padding = new Thickness(12, 6, 12, 6),
			Tag = "__fluent_upgraded"
		};
		FluentIconFactory.SetButtonFluentIcon(btnLoad, FluentIconKind.FolderScan, "载入脉冲响应 IR (.wav)", 18);
		btnLoad.Click += OnConvolutionImportClick;
		sp.Children.Add(btnLoad);

		_irInfoText = new TextBlock
		{
			Text = string.IsNullOrEmpty(ViewModel?.EchoConvolutionFilePath) ? "当前未载入脉冲响应文件（支持 44.1k/48k/96k 双声道 IR）" : $"已载入 IR: {System.IO.Path.GetFileName(ViewModel.EchoConvolutionFilePath)}",
			FontSize = 11,
			Opacity = 0.7,
			TextWrapping = TextWrapping.Wrap
		};
		sp.Children.Add(_irInfoText);

		var sTrim = new Slider { Header = "输出电平衰减 Trim (-24 ~ +6 dB)", Minimum = -24.0, Maximum = 6.0, StepFrequency = 0.5, Value = ViewModel?.EchoConvolutionTrimDb ?? 0.0 };
		sTrim.ValueChanged += (s, e) => { if (ViewModel != null) ViewModel.EchoConvolutionTrimDb = e.NewValue; };
		sp.Children.Add(sTrim);

		sp.Children.Add(new TextBlock
		{
			Text = "FIR 卷积引擎支持高精度直接卷积与耳机逆滤波器，实时修正频响曲线与房间混响失真。",
			FontSize = 11,
			Opacity = 0.55,
			TextWrapping = TextWrapping.Wrap
		});

		return CreatePanelCard("FIR 卷积房间/耳机校正 (Convolution)", "\uE8B7", sp, out var h, out var r, 580, 380);
	}

	private Border CreateAdvancedDspPanel()
	{
		var sp = new StackPanel { Spacing = 12 };

		var toggleResample = new ToggleSwitch
		{
			Header = "启用实时多相高阶升频重采样 (Resampler)",
			IsOn = ViewModel?.EchoResamplerEnabled ?? false
		};
		toggleResample.Toggled += (s, e) => { if (ViewModel != null) ViewModel.EchoResamplerEnabled = toggleResample.IsOn; };
		sp.Children.Add(toggleResample);

		var comboRate = new ComboBox { Header = "目标采样率 Target SampleRate", MinWidth = 200 };
		comboRate.Items.Add(new ComboBoxItem { Content = "自动保持原始 (Auto)" });
		comboRate.Items.Add(new ComboBoxItem { Content = "48.0 kHz" });
		comboRate.Items.Add(new ComboBoxItem { Content = "96.0 kHz (高解析)" });
		comboRate.Items.Add(new ComboBoxItem { Content = "192.0 kHz (母带级)" });
		comboRate.Items.Add(new ComboBoxItem { Content = "384.0 kHz (极限高保真)" });
		int curRate = ViewModel?.EchoResamplerTargetRate ?? 0;
		comboRate.SelectedIndex = curRate switch { 48000 => 1, 96000 => 2, 192000 => 3, 384000 => 4, _ => 0 };
		comboRate.SelectionChanged += (s, e) =>
		{
			if (ViewModel != null)
			{
				ViewModel.EchoResamplerTargetRate = comboRate.SelectedIndex switch
				{
					1 => 48000,
					2 => 96000,
					3 => 192000,
					4 => 384000,
					_ => 0
				};
			}
		};
		sp.Children.Add(comboRate);

		var toggleSdm = new ToggleSwitch
		{
			Header = "启用 PCM 转 DSD 调制器 (SDM Modulator)",
			IsOn = ViewModel?.EchoSdmEnabled ?? false
		};
		toggleSdm.Toggled += (s, e) => { if (ViewModel != null) ViewModel.EchoSdmEnabled = toggleSdm.IsOn; };
		sp.Children.Add(toggleSdm);

		var comboDsd = new ComboBox { Header = "DSD 阶数模式", MinWidth = 200 };
		comboDsd.Items.Add(new ComboBoxItem { Content = "DSD64 (2.822 MHz / 1-bit)" });
		comboDsd.Items.Add(new ComboBoxItem { Content = "DSD128 (5.644 MHz / 1-bit)" });
		comboDsd.Items.Add(new ComboBoxItem { Content = "DSD256 (11.289 MHz / 1-bit)" });
		int curSdmRate = ViewModel?.EchoSdmRate ?? 2822400;
		comboDsd.SelectedIndex = curSdmRate switch { 5644800 => 1, 11289600 => 2, _ => 0 };
		comboDsd.SelectionChanged += (s, e) =>
		{
			if (ViewModel != null)
			{
				ViewModel.EchoSdmRate = comboDsd.SelectedIndex switch
				{
					1 => 5644800,
					2 => 11289600,
					_ => 2822400
				};
			}
		};
		sp.Children.Add(comboDsd);

		sp.Children.Add(new TextBlock
		{
			Text = "5阶五极点噪声整形滤波器持续运行，将量化噪声推向 50kHz 以上超声频段，保留纯净模拟质感。",
			FontSize = 11,
			Opacity = 0.55,
			TextWrapping = TextWrapping.Wrap
		});

		return CreatePanelCard("实时升频与 PCM 转 DSD (SDM Modulator)", "\uE774", sp, out var h, out var r, 580, 360);
	}

	private Border CreateReplayGainHeadroomPanel()
	{
		var sp = new StackPanel { Spacing = 12 };

		var comboRg = new ComboBox { Header = "ReplayGain 响度均衡模式", MinWidth = 200 };
		comboRg.Items.Add(new ComboBoxItem { Content = "关闭 (Off)" });
		comboRg.Items.Add(new ComboBoxItem { Content = "按曲目 (Track Gain)" });
		comboRg.Items.Add(new ComboBoxItem { Content = "按专辑 (Album Gain)" });
		comboRg.SelectedIndex = Math.Clamp(ViewModel?.EchoReplayGainMode ?? 0, 0, 2);
		comboRg.SelectionChanged += (s, e) => { if (ViewModel != null && comboRg.SelectedIndex >= 0) ViewModel.EchoReplayGainMode = comboRg.SelectedIndex; };
		sp.Children.Add(comboRg);

		var sPreamp = new Slider { Header = "PreAmp 前置增益 (-12 ~ +12 dB)", Minimum = -12.0, Maximum = 12.0, StepFrequency = 0.5, Value = ViewModel?.EchoReplayGainPreampDb ?? 0.0 };
		sPreamp.ValueChanged += (s, e) => { if (ViewModel != null) ViewModel.EchoReplayGainPreampDb = e.NewValue; };
		sp.Children.Add(sPreamp);

		var togglePreventClip = new ToggleSwitch
		{
			Header = "削波溢出防护 (Prevent Clipping)",
			IsOn = ViewModel?.EchoReplayGainPreventClipping ?? true
		};
		togglePreventClip.Toggled += (s, e) => { if (ViewModel != null) ViewModel.EchoReplayGainPreventClipping = togglePreventClip.IsOn; };
		sp.Children.Add(togglePreventClip);

		var sHeadroom = new Slider { Header = "数字余量衰减 Headroom (0 ~ -12 dB)", Minimum = -12.0, Maximum = 0.0, StepFrequency = 0.5, Value = ViewModel?.EchoHeadroomDb ?? -1.0 };
		sHeadroom.ValueChanged += (s, e) => { if (ViewModel != null) ViewModel.EchoHeadroomDb = e.NewValue; };
		sp.Children.Add(sHeadroom);

		sp.Children.Add(new TextBlock
		{
			Text = "在进入 DSP 链路前留出充足计算余量，配合峰值削波限制器杜绝数字破音。",
			FontSize = 11,
			Opacity = 0.55,
			TextWrapping = TextWrapping.Wrap
		});

		return CreatePanelCard("响度均衡与数字余量 (ReplayGain & Headroom)", "\uE994", sp, out var h, out var r, 560, 380);
	}

	private Border CreateDspRackOrderPanel()
	{
		var sp = new StackPanel { Spacing = 8 };

		var modules = new (string Name, FluentIconKind Kind, string Desc)[]
		{
			("余量管理 (Headroom)", FluentIconKind.Dynamics, "数字余量下压防削波"),
			("响度均衡 (ReplayGain)", FluentIconKind.Volume, "曲目/专辑感知响度自动对齐"),
			("10段图形EQ & AutoEq", FluentIconKind.Eq, "多段高精度参数化均衡校准"),
			("母带动态压缩器 (Compressor)", FluentIconKind.Dynamics, "瞬态与动态范围塑造"),
			("双耳互馈 (Crossfeed)", FluentIconKind.Spatial, "消除耳机听觉疲劳与头中效应"),
			("声场宽度与声道矩阵", FluentIconKind.Spatial, "0~200% 声场宽度与极性控制"),
			("FIR 卷积房间校正", FluentIconKind.FolderScan, "脉冲响应逆滤波器修正"),
			("多相升频重采样 (Resampler)", FluentIconKind.HiRes, "最高 384kHz 高解析升频"),
			("PCM 转 DSD (SDM 调制器)", FluentIconKind.HiRes, "DSD64/128/256 高阶噪声整形"),
			("VST3 插件宿主", FluentIconKind.VstPlugin, "外接第三方专业音频效果器")
		};

		var listGrid = new Grid { RowSpacing = 6 };
		for (int i = 0; i < modules.Length; i++)
		{
			listGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
			var m = modules[i];

			var row = new Border
			{
				Padding = new Thickness(8, 4, 8, 4),
				CornerRadius = new CornerRadius(6),
				Background = new SolidColorBrush(Windows.UI.Color.FromArgb(100, 28, 32, 40)),
				BorderBrush = new SolidColorBrush(Windows.UI.Color.FromArgb(50, 255, 255, 255)),
				BorderThickness = new Thickness(1)
			};
			var rowGrid = new Grid { ColumnSpacing = 10 };
			rowGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
			rowGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.0, GridUnitType.Star) });
			rowGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

			var chkContent = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8, VerticalAlignment = VerticalAlignment.Center };
			chkContent.Children.Add(FluentIconFactory.Create(m.Kind, 18));
			chkContent.Children.Add(new TextBlock { Text = $"{i + 1}. {m.Name}", FontSize = 12, VerticalAlignment = VerticalAlignment.Center });
			var chk = new CheckBox { IsChecked = true, Content = chkContent, FontSize = 12 };
			var descTb = new TextBlock { Text = m.Desc, FontSize = 11, Opacity = 0.6, VerticalAlignment = VerticalAlignment.Center };
			Grid.SetColumn(descTb, 1);

			var indicator = new Border
			{
				Width = 8,
				Height = 8,
				CornerRadius = new CornerRadius(4),
				Background = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 0, 229, 255)),
				VerticalAlignment = VerticalAlignment.Center
			};
			ToolTipService.SetToolTip(indicator, "处理链路活动中");
			Grid.SetColumn(indicator, 2);

			rowGrid.Children.Add(chk);
			rowGrid.Children.Add(descTb);
			rowGrid.Children.Add(indicator);
			row.Child = rowGrid;

			Grid.SetRow(row, i);
			listGrid.Children.Add(row);
		}

		sp.Children.Add(listGrid);

		return CreatePanelCard("全链路 DSP Rack 机架编排 (DSP Order & Bypass)", "\uE8B8", sp, out var h, out var r, 1160, 370);
	}

	private void SetupAutoEqButton()
	{
		try
		{
			if (EqPresetCombo?.Parent is StackPanel presetStack)
			{
				var btnAutoEq = new Button
				{
					Padding = new Thickness(10, 4, 10, 4),
					CornerRadius = new CornerRadius(6),
					Tag = "__fluent_upgraded"
				};
				FluentIconFactory.SetButtonFluentIcon(btnAutoEq, FluentIconKind.Eq, "导入 AutoEq 耳机预设", 18);
				btnAutoEq.Click += OnAutoEqImportClick;
				presetStack.Children.Add(btnAutoEq);
			}
		}
		catch (Exception ex)
		{
			Debug.WriteLine($"SetupAutoEqButton failed: {ex.Message}");
		}
	}

	private void SetupUnifiedEchoDspLayout(Grid rootGrid)
	{
		try
		{
			rootGrid.Children.Clear();
			rootGrid.RowDefinitions.Clear();
			rootGrid.ColumnDefinitions.Clear();
			rootGrid.Padding = new Thickness(0);
			rootGrid.Background = CreateWhiteAcrylicBrush(0.70, 190);

			// 2-column layout: Col 0 = Left Sidebar (240px), Col 1 = Main Workbench (*)
			rootGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(240, GridUnitType.Pixel) });
			rootGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.0, GridUnitType.Star) });

			var sidebar = CreateEchoSidebar();
			Grid.SetColumn(sidebar, 0);
			rootGrid.Children.Add(sidebar);

			_workbenchHost = new Grid
			{
				Background = CreateWhiteAcrylicBrush(0.72, 195)
			};
			Grid.SetColumn(_workbenchHost, 1);
			rootGrid.Children.Add(_workbenchHost);

			// Initialize and display Parametric EQ as default
			CreateParametricEqWorkbench();
			SelectDspCategory("param_eq");
		}
		catch (Exception ex)
		{
			Debug.WriteLine($"SetupUnifiedEchoDspLayout failed: {ex.Message}");
		}
	}

	private FrameworkElement CreateEchoSidebar()
	{
		var border = new Border
		{
			Width = 240,
			Background = CreateWhiteAcrylicBrush(0.82, 225),
			BorderBrush = new SolidColorBrush(Windows.UI.Color.FromArgb(120, 226, 232, 240)),
			BorderThickness = new Thickness(0, 0, 1, 0),
			Padding = new Thickness(12, 16, 12, 12)
		};

		var sidebarGrid = new Grid();
		sidebarGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Brand Header
		sidebarGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1.0, GridUnitType.Star) }); // Scrollable items
		sidebarGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Footer Output

		// 1. Logo Header
		var headerStack = new StackPanel { Spacing = 2, Margin = new Thickness(6, 0, 0, 14) };
		headerStack.Children.Add(new TextBlock
		{
			Text = "DSP",
			FontSize = 10,
			FontWeight = Microsoft.UI.Text.FontWeights.Bold,
			Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 99, 102, 241))
		});
		headerStack.Children.Add(new TextBlock
		{
			Text = "FurinaPlayer",
			FontSize = 20,
			FontWeight = Microsoft.UI.Text.FontWeights.ExtraBold,
			Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 15, 23, 42))
		});
		headerStack.Children.Add(new TextBlock
		{
			Text = "信号控制",
			FontSize = 11,
			Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 100, 116, 139))
		});
		Grid.SetRow(headerStack, 0);
		sidebarGrid.Children.Add(headerStack);

		// 2. Scrollable Navigation List
		var sv = new ScrollViewer
		{
			VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
			HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
			Padding = new Thickness(0, 0, 4, 0)
		};
		var itemsStack = new StackPanel { Spacing = 4 };
		_sidebarNavButtons.Clear();

		void AddNavSectionHeader(string title)
		{
			itemsStack.Children.Add(new TextBlock
			{
				Text = title,
				FontSize = 11,
				FontWeight = Microsoft.UI.Text.FontWeights.Bold,
				Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 71, 85, 105)),
				Margin = new Thickness(8, 12, 0, 4)
			});
		}

		Button CreateNavButton(string key, string iconGlyph, string title, string subtitle, bool hasCheck = false)
		{
			var btn = new Button
			{
				Tag = key,
				HorizontalAlignment = HorizontalAlignment.Stretch,
				HorizontalContentAlignment = HorizontalAlignment.Stretch,
				Padding = new Thickness(10, 8, 10, 8),
				Margin = new Thickness(0, 2, 0, 2),
				CornerRadius = new CornerRadius(8),
				Background = new SolidColorBrush(Colors.Transparent),
				BorderThickness = new Thickness(0)
			};

			var rowGrid = new Grid { ColumnSpacing = 10 };
			rowGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto }); // Icon
			rowGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.0, GridUnitType.Star) }); // Texts
			if (hasCheck) rowGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto }); // Checkmark

			var icon = new FontIcon
			{
				Glyph = iconGlyph,
				FontSize = 16,
				Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 100, 116, 139)),
				VerticalAlignment = VerticalAlignment.Center
			};
			Grid.SetColumn(icon, 0);
			rowGrid.Children.Add(icon);

			var textSp = new StackPanel { Spacing = 2, VerticalAlignment = VerticalAlignment.Center };
			var titleTb = new TextBlock
			{
				Text = title,
				FontSize = 13,
				FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
				Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 30, 41, 59))
			};
			var subTb = new TextBlock
			{
				Text = subtitle,
				FontSize = 10,
				Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 100, 116, 139))
			};
			if (key == "headroom")
			{
				_headroomNavSubTb = subTb;
				double initialDb = ViewModel?.EchoHeadroomDb ?? -3.0;
				bool enabled = ViewModel?.EchoHeadroomEnabled ?? true;
				subTb.Text = !enabled ? "已旁路" : $"{initialDb:+0.0;-0.0;0.0} dB";
			}
			else if (key == "headphone")
			{
				_headphoneNavSubTb = subTb;
			}
			else if (key == "room")
			{
				_roomNavSubTb = subTb;
				bool enabled = ViewModel?.EchoConvolutionEnabled ?? false;
				string? irPath = ViewModel?.EchoConvolutionFilePath;
				subTb.Text = string.IsNullOrEmpty(irPath) ? "无 IR" : (enabled ? "已启用" : "已载入");
			}
			else if (key == "channel")
			{
				_channelNavSubTb = subTb;
				bool enabled = ViewModel?.FurinaChannelToolEnabled ?? false;
				subTb.Text = !enabled ? "立体声直通" : "已启用";
			}
			textSp.Children.Add(titleTb);
			textSp.Children.Add(subTb);
			Grid.SetColumn(textSp, 1);
			rowGrid.Children.Add(textSp);

			if (hasCheck)
			{
				var checkIcon = new FontIcon
				{
					Glyph = "\uE73E",
					FontSize = 12,
					Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 22, 163, 74)),
					VerticalAlignment = VerticalAlignment.Center
				};
				Grid.SetColumn(checkIcon, 2);
				rowGrid.Children.Add(checkIcon);
			}

			btn.Content = rowGrid;
			btn.Click += (s, e) =>
			{
				if (s is Button b && b.Tag is string cat)
				{
					SelectDspCategory(cat);
				}
			};
			_sidebarNavButtons.Add(btn);
			return btn;
		}

		// Items:
		itemsStack.Children.Add(CreateNavButton("listening_plan", "\uE7F6", "听音方案", "Moondrop / Custom"));

		AddNavSectionHeader("电平");
		itemsStack.Children.Add(CreateNavButton("level_gain", "\uE767", "音量均衡", "已旁路"));
		itemsStack.Children.Add(CreateNavButton("headroom", "\uE916", "Headroom", "-3 dB", hasCheck: true));

		AddNavSectionHeader("采样转换");
		itemsStack.Children.Add(CreateNavButton("resampler", "\uE774", "升频", "已旁路"));
		itemsStack.Children.Add(CreateNavButton("dsd", "\uE8B7", "DSD 转换 测试版", "已旁路"));

		AddNavSectionHeader("音色");
		itemsStack.Children.Add(CreateNavButton("param_eq", "\uE995", "参数 EQ", "已启用 · 10频段", hasCheck: true));
		itemsStack.Children.Add(CreateNavButton("headphone", "\uE7F5", "耳机校正", "已旁路"));

		AddNavSectionHeader("空间与输出");
		itemsStack.Children.Add(CreateNavButton("room", "\uE7BE", "房间校正", "无 IR"));
		itemsStack.Children.Add(CreateNavButton("channel", "\uE768", "声道工具", "立体声直通"));
		itemsStack.Children.Add(CreateNavButton("safety", "\uE72E", "输出安全", "待命"));

		AddNavSectionHeader("高级设置");
		itemsStack.Children.Add(CreateNavButton("compressor", "\uE9D9", "压缩器", "已旁路"));
		itemsStack.Children.Add(CreateNavButton("multiband", "\uE994", "三频段压缩", "已旁路"));
		itemsStack.Children.Add(CreateNavButton("deesser", "\uE74F", "去齿音", "已旁路"));
		itemsStack.Children.Add(CreateNavButton("limiter", "\uE71A", "限幅", "自动上限", hasCheck: true));
		itemsStack.Children.Add(CreateNavButton("crossfeed", "\uE895", "交叉馈送", "双耳互馈"));
		itemsStack.Children.Add(CreateNavButton("vst3", "\uE8B8", "VST3 机架", "扩展机架"));

		sv.Content = itemsStack;
		Grid.SetRow(sv, 1);
		sidebarGrid.Children.Add(sv);

		// 3. Footer device
		var footerBtn = CreateNavButton("device", "\uE7F6", "音频输出设备", "WASAPI · 独占/共享");
		footerBtn.Margin = new Thickness(0, 10, 0, 0);
		Grid.SetRow(footerBtn, 2);
		sidebarGrid.Children.Add(footerBtn);

		border.Child = sidebarGrid;
		return border;
	}

	private FrameworkElement CreateHeadroomWorkbench()
	{
		var sv = new ScrollViewer
		{
			VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
			HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled
		};

		var root = new Grid
		{
			Background = CreateWhiteAcrylicBrush(0.72, 195),
			Padding = new Thickness(24, 20, 24, 20),
			RowSpacing = 16
		};
		root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // 0: Top Header
		root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // 1: Gain & Peak Safety Card
		root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // 2: Dynamic Loudness Compensation Card
		root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // 3: Main Dual Column Area
		root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // 4: Headroom Budget & Realtime Status Card

		// --- 0. Top Header ---
		var headerGrid = new Grid();
		headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.0, GridUnitType.Star) });
		headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

		var titleSp = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 12, VerticalAlignment = VerticalAlignment.Center };
		var iconBorder = new Border
		{
			Width = 36,
			Height = 36,
			CornerRadius = new CornerRadius(18),
			Background = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 238, 242, 255)),
			Child = new FontIcon { Glyph = "\uE916", FontSize = 18, Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 99, 102, 241)), HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center }
		};
		titleSp.Children.Add(iconBorder);
		titleSp.Children.Add(new TextBlock
		{
			Text = "Headroom",
			FontSize = 22,
			FontWeight = Microsoft.UI.Text.FontWeights.Bold,
			Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 15, 23, 42)),
			VerticalAlignment = VerticalAlignment.Center
		});
		Grid.SetColumn(titleSp, 0);
		headerGrid.Children.Add(titleSp);

		var topActionsSp = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 10, VerticalAlignment = VerticalAlignment.Center };
		var modulePill = new Border
		{
			Background = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 238, 242, 255)),
			BorderBrush = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 199, 210, 254)),
			BorderThickness = new Thickness(1),
			CornerRadius = new CornerRadius(14),
			Padding = new Thickness(10, 4, 10, 4),
			Child = new StackPanel
			{
				Orientation = Orientation.Horizontal,
				Spacing = 5,
				Children =
				{
					new FontIcon { Glyph = "\uE945", FontSize = 12, Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 79, 70, 229)) },
					new TextBlock { Text = "4 个模块启用", FontSize = 12, Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 79, 70, 229)), FontWeight = Microsoft.UI.Text.FontWeights.SemiBold }
				}
			}
		};
		var readyPill = new Border
		{
			Background = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 240, 253, 244)),
			BorderBrush = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 187, 247, 208)),
			BorderThickness = new Thickness(1),
			CornerRadius = new CornerRadius(14),
			Padding = new Thickness(10, 4, 10, 4),
			Child = new StackPanel
			{
				Orientation = Orientation.Horizontal,
				Spacing = 5,
				Children =
				{
					new FontIcon { Glyph = "\uE9D9", FontSize = 12, Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 22, 163, 74)) },
					new TextBlock { Text = "就绪", FontSize = 12, Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 22, 163, 74)), FontWeight = Microsoft.UI.Text.FontWeights.SemiBold }
				}
			}
		};
		var btnRefreshTop = new Button
		{
			Content = new StackPanel
			{
				Orientation = Orientation.Horizontal,
				Spacing = 6,
				Children =
				{
					new FontIcon { Glyph = "\uE72C", FontSize = 12, Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 71, 85, 105)) },
					new TextBlock { Text = "刷新状态", FontSize = 12, Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 51, 65, 85)) }
				}
			},
			Padding = new Thickness(10, 5, 10, 5),
			CornerRadius = new CornerRadius(8),
			Background = new SolidColorBrush(Windows.UI.Color.FromArgb(210, 255, 255, 255)),
			BorderBrush = new SolidColorBrush(Windows.UI.Color.FromArgb(140, 226, 232, 240)),
			BorderThickness = new Thickness(1)
		};
		topActionsSp.Children.Add(modulePill);
		topActionsSp.Children.Add(readyPill);
		topActionsSp.Children.Add(btnRefreshTop);
		Grid.SetColumn(topActionsSp, 1);
		headerGrid.Children.Add(topActionsSp);
		Grid.SetRow(headerGrid, 0);
		root.Children.Add(headerGrid);

		// --- 1. 增益与峰值安全 卡片 ---
		var safetyCard = new Border
		{
			Background = CreateAcrylicCardBrush(0.88, 235),
			BorderBrush = new SolidColorBrush(Windows.UI.Color.FromArgb(140, 226, 232, 240)),
			BorderThickness = new Thickness(1),
			CornerRadius = new CornerRadius(12),
			Padding = new Thickness(18, 14, 18, 14)
		};
		var safetyStack = new StackPanel { Spacing = 10 };

		var safetyTitleSp = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 12 };
		safetyTitleSp.Children.Add(new TextBlock { Text = "增益与峰值安全", FontSize = 14, FontWeight = Microsoft.UI.Text.FontWeights.SemiBold, Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 15, 23, 42)) });
		safetyTitleSp.Children.Add(new TextBlock { Text = "等待当前宿主读数", FontSize = 12, Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 100, 116, 139)), VerticalAlignment = VerticalAlignment.Center });
		safetyStack.Children.Add(safetyTitleSp);

		var indGrid3 = new Grid { ColumnSpacing = 16 };
		indGrid3.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.0, GridUnitType.Star) });
		indGrid3.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.0, GridUnitType.Star) });
		indGrid3.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.0, GridUnitType.Star) });

		var ind1 = new TextBlock { Text = "配置风险估计: 未报告", FontSize = 12, Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 71, 85, 105)) };
		var ind2 = new TextBlock { Text = "实测峰值超限: 未知", FontSize = 12, Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 71, 85, 105)) };
		var ind3 = new TextBlock { Text = "当前限幅: 未知", FontSize = 12, Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 71, 85, 105)) };
		Grid.SetColumn(ind1, 0); indGrid3.Children.Add(ind1);
		Grid.SetColumn(ind2, 1); indGrid3.Children.Add(ind2);
		Grid.SetColumn(ind3, 2); indGrid3.Children.Add(ind3);
		safetyStack.Children.Add(indGrid3);

		var metricsGrid = new Grid { ColumnSpacing = 16, Margin = new Thickness(0, 4, 0, 4) };
		metricsGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.0, GridUnitType.Star) });
		metricsGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.0, GridUnitType.Star) });
		metricsGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.0, GridUnitType.Star) });
		metricsGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.0, GridUnitType.Star) });

		StackPanel MakeMetricBox(string label, string val)
		{
			var box = new StackPanel { Spacing = 2 };
			box.Children.Add(new TextBlock { Text = label, FontSize = 11, Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 100, 116, 139)) });
			box.Children.Add(new TextBlock { Text = val, FontSize = 13, FontWeight = Microsoft.UI.Text.FontWeights.SemiBold, Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 15, 23, 42)) });
			return box;
		}
		var m1 = MakeMetricBox("限幅前", "未知");
		var m2 = MakeMetricBox("DSP 后", "未知");
		var m3 = MakeMetricBox("Limiter 衰减", "未知");
		var m4 = MakeMetricBox("历史输出超限次数", "未知");
		Grid.SetColumn(m1, 0); metricsGrid.Children.Add(m1);
		Grid.SetColumn(m2, 1); metricsGrid.Children.Add(m2);
		Grid.SetColumn(m3, 2); metricsGrid.Children.Add(m3);
		Grid.SetColumn(m4, 3); metricsGrid.Children.Add(m4);
		safetyStack.Children.Add(metricsGrid);

		safetyStack.Children.Add(new TextBlock
		{
			Text = "限幅前超过 0 dBTP 不等于输出已被削波。历史次数不代表当前仍在削波；读数只需覆盖所报告的阶段。",
			FontSize = 11,
			Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 100, 116, 139)),
			TextWrapping = TextWrapping.Wrap
		});

		var foldBtn = new Button
		{
			Content = "▶ 处理位置与可见边界",
			Background = new SolidColorBrush(Colors.Transparent),
			BorderThickness = new Thickness(0),
			Padding = new Thickness(0, 4, 0, 4),
			FontSize = 12,
			Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 79, 70, 229)),
			FontWeight = Microsoft.UI.Text.FontWeights.Medium
		};
		var foldContent = new Border
		{
			Visibility = Visibility.Collapsed,
			Background = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 241, 245, 249)),
			BorderBrush = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 226, 232, 240)),
			BorderThickness = new Thickness(1),
			CornerRadius = new CornerRadius(8),
			Padding = new Thickness(12, 10, 12, 10),
			Child = new TextBlock
			{
				Text = "在 32 位浮点架构下，DSP 信号链内部可保留大于 0 dBFS 的电平余量，不会发生数字硬削波截断。Headroom 控制位于主输出级，可在进入声卡 DAC 之前预留安全的采样间真实峰值 (True Peak) 余量，结合限幅器确保无失真高保真还原。",
				FontSize = 11,
				Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 51, 65, 85)),
				TextWrapping = TextWrapping.Wrap
			}
		};
		foldBtn.Click += (s, e) =>
		{
			if (foldContent.Visibility == Visibility.Visible)
			{
				foldContent.Visibility = Visibility.Collapsed;
				foldBtn.Content = "▶ 处理位置与可见边界";
			}
			else
			{
				foldContent.Visibility = Visibility.Visible;
				foldBtn.Content = "▼ 处理位置与可见边界";
			}
		};
		safetyStack.Children.Add(foldBtn);
		safetyStack.Children.Add(foldContent);

		safetyCard.Child = safetyStack;
		Grid.SetRow(safetyCard, 1);
		root.Children.Add(safetyCard);

		// --- 2. 动态等响补偿 ---
		var dynLoudCard = new Border
		{
			Background = CreateAcrylicCardBrush(0.88, 235),
			BorderBrush = new SolidColorBrush(Windows.UI.Color.FromArgb(140, 226, 232, 240)),
			BorderThickness = new Thickness(1),
			CornerRadius = new CornerRadius(10),
			Padding = new Thickness(18, 12, 18, 12)
		};
		var dynLoudGrid = new Grid();
		dynLoudGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.0, GridUnitType.Star) });
		dynLoudGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

		var dynLoudTitleTb = new TextBlock
		{
			Text = "动态等响补偿",
			FontSize = 14,
			FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
			Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 15, 23, 42)),
			VerticalAlignment = VerticalAlignment.Center
		};
		Grid.SetColumn(dynLoudTitleTb, 0);
		dynLoudGrid.Children.Add(dynLoudTitleTb);

		_headroomDynamicLoudnessCb = new CheckBox
		{
			IsChecked = ViewModel?.EchoDynamicLoudnessCompensation ?? false,
			VerticalAlignment = VerticalAlignment.Center
		};
		_headroomDynamicLoudnessCb.Checked += (s, e) =>
		{
			if (ViewModel != null)
			{
				ViewModel.EchoDynamicLoudnessCompensation = true;
				ViewModel.SaveEchoSettings();
			}
		};
		_headroomDynamicLoudnessCb.Unchecked += (s, e) =>
		{
			if (ViewModel != null)
			{
				ViewModel.EchoDynamicLoudnessCompensation = false;
				ViewModel.SaveEchoSettings();
			}
		};
		Grid.SetColumn(_headroomDynamicLoudnessCb, 1);
		dynLoudGrid.Children.Add(_headroomDynamicLoudnessCb);
		dynLoudCard.Child = dynLoudGrid;

		Grid.SetRow(dynLoudCard, 2);
		root.Children.Add(dynLoudCard);

		// --- 3. 主控制区双列布局 ---
		var dualColGrid = new Grid { ColumnSpacing = 16 };
		dualColGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.0, GridUnitType.Star) });
		dualColGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(340, GridUnitType.Pixel) });

		// 左列：主增益卡片
		var gainCard = new Border
		{
			Background = CreateAcrylicCardBrush(0.88, 235),
			BorderBrush = new SolidColorBrush(Windows.UI.Color.FromArgb(140, 226, 232, 240)),
			BorderThickness = new Thickness(1),
			CornerRadius = new CornerRadius(12),
			Padding = new Thickness(22, 18, 22, 18)
		};
		var gainStack = new StackPanel { Spacing = 12 };

		var gainTopRow = new Grid();
		gainTopRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.0, GridUnitType.Star) });
		gainTopRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

		gainTopRow.Children.Add(new TextBlock
		{
			Text = "增益",
			FontSize = 15,
			FontWeight = Microsoft.UI.Text.FontWeights.Bold,
			Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 15, 23, 42)),
			VerticalAlignment = VerticalAlignment.Center
		});

		_headroomPowerSwitch = new ToggleSwitch
		{
			IsOn = ViewModel?.EchoHeadroomEnabled ?? true,
			OnContent = "已启用",
			OffContent = "已旁路",
			VerticalAlignment = VerticalAlignment.Center
		};

		Grid.SetColumn(_headroomPowerSwitch, 1);
		gainTopRow.Children.Add(_headroomPowerSwitch);
		gainStack.Children.Add(gainTopRow);

		var numSp = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 6, Margin = new Thickness(0, 4, 0, 0) };
		_headroomGainBigTb = new TextBlock
		{
			Text = $"{ViewModel?.EchoHeadroomDb ?? -3.0:0.#}",
			FontSize = 54,
			FontWeight = Microsoft.UI.Text.FontWeights.ExtraBold,
			Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 15, 23, 42)),
			VerticalAlignment = VerticalAlignment.Bottom
		};
		var unitTb = new TextBlock
		{
			Text = "dB",
			FontSize = 26,
			FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
			Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 71, 85, 105)),
			VerticalAlignment = VerticalAlignment.Bottom,
			Margin = new Thickness(0, 0, 0, 8)
		};
		numSp.Children.Add(_headroomGainBigTb);
		numSp.Children.Add(unitTb);
		gainStack.Children.Add(numSp);

		double initDb = ViewModel?.EchoHeadroomDb ?? -3.0;
		double initRatio = Math.Pow(10.0, initDb / 20.0);
		_headroomAmpRatioTb = new TextBlock
		{
			Text = $"×{initRatio:F3}  {initRatio * 100.0:F1}% amplitude",
			FontSize = 12,
			Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 100, 116, 139)),
			Margin = new Thickness(2, 0, 0, 10)
		};
		gainStack.Children.Add(_headroomAmpRatioTb);

		var sliderRow = new Grid { ColumnSpacing = 12 };
		sliderRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto }); // [-]
		sliderRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.0, GridUnitType.Star) }); // Slider + scale
		sliderRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto }); // [+]

		var btnMinus = new Button
		{
			Content = "－",
			FontSize = 16,
			FontWeight = Microsoft.UI.Text.FontWeights.Bold,
			Width = 38,
			Height = 38,
			CornerRadius = new CornerRadius(19),
			Padding = new Thickness(0),
			HorizontalContentAlignment = HorizontalAlignment.Center,
			VerticalContentAlignment = VerticalAlignment.Center,
			Background = new SolidColorBrush(Windows.UI.Color.FromArgb(210, 255, 255, 255)),
			BorderBrush = new SolidColorBrush(Windows.UI.Color.FromArgb(180, 203, 213, 225)),
			BorderThickness = new Thickness(1),
			Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 15, 23, 42))
		};
		Microsoft.UI.Xaml.Automation.AutomationProperties.SetName(btnMinus, "减少 Headroom 增益 0.5 dB");
		ToolTipService.SetToolTip(btnMinus, "步进减少 0.5 dB (下限 -12 dB)");
		Grid.SetColumn(btnMinus, 0);
		sliderRow.Children.Add(btnMinus);

		var sliderColSp = new StackPanel { Spacing = 2 };
		_headroomGainSlider = new Slider
		{
			Minimum = -12.0,
			Maximum = 12.0,
			StepFrequency = 0.5,
			Value = ViewModel?.EchoHeadroomDb ?? -3.0,
			HorizontalAlignment = HorizontalAlignment.Stretch
		};
		Microsoft.UI.Xaml.Automation.AutomationProperties.SetName(_headroomGainSlider, "Headroom 增益控制滑块");
		ToolTipService.SetToolTip(_headroomGainSlider, "调节输出 Headroom 增益 (-12 dB ~ +12 dB)");
		sliderColSp.Children.Add(_headroomGainSlider);

		var scaleGrid = new Grid();
		scaleGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.0, GridUnitType.Star) });
		scaleGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.0, GridUnitType.Star) });
		scaleGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.0, GridUnitType.Star) });
		scaleGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.0, GridUnitType.Star) });
		scaleGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.0, GridUnitType.Star) });

		scaleGrid.Children.Add(new TextBlock { Text = "-12 dB", FontSize = 10, Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 100, 116, 139)), HorizontalAlignment = HorizontalAlignment.Left });
		var sc2 = new TextBlock { Text = "-6 dB", FontSize = 10, Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 100, 116, 139)), HorizontalAlignment = HorizontalAlignment.Center };
		Grid.SetColumn(sc2, 1); scaleGrid.Children.Add(sc2);
		var sc3 = new TextBlock { Text = "0 dB", FontSize = 11, FontWeight = Microsoft.UI.Text.FontWeights.Bold, Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 15, 23, 42)), HorizontalAlignment = HorizontalAlignment.Center };
		Grid.SetColumn(sc3, 2); scaleGrid.Children.Add(sc3);
		var sc4 = new TextBlock { Text = "+6 dB", FontSize = 10, Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 100, 116, 139)), HorizontalAlignment = HorizontalAlignment.Center };
		Grid.SetColumn(sc4, 3); scaleGrid.Children.Add(sc4);
		var sc5 = new TextBlock { Text = "+12 dB", FontSize = 10, Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 100, 116, 139)), HorizontalAlignment = HorizontalAlignment.Right };
		Grid.SetColumn(sc5, 4); scaleGrid.Children.Add(sc5);
		sliderColSp.Children.Add(scaleGrid);

		Grid.SetColumn(sliderColSp, 1);
		sliderRow.Children.Add(sliderColSp);

		var btnPlus = new Button
		{
			Content = "＋",
			FontSize = 16,
			FontWeight = Microsoft.UI.Text.FontWeights.Bold,
			Width = 38,
			Height = 38,
			CornerRadius = new CornerRadius(19),
			Padding = new Thickness(0),
			HorizontalContentAlignment = HorizontalAlignment.Center,
			VerticalContentAlignment = VerticalAlignment.Center,
			Background = new SolidColorBrush(Windows.UI.Color.FromArgb(210, 255, 255, 255)),
			BorderBrush = new SolidColorBrush(Windows.UI.Color.FromArgb(180, 203, 213, 225)),
			BorderThickness = new Thickness(1),
			Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 15, 23, 42))
		};
		Microsoft.UI.Xaml.Automation.AutomationProperties.SetName(btnPlus, "增加 Headroom 增益 0.5 dB");
		ToolTipService.SetToolTip(btnPlus, "步进增加 0.5 dB (上限 +12 dB)");
		Grid.SetColumn(btnPlus, 2);
		sliderRow.Children.Add(btnPlus);

		gainStack.Children.Add(sliderRow);

		// Presets Row + Audition Button
		var presetsSp = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8, Margin = new Thickness(0, 10, 0, 0) };
		_headroomPresetButtons.Clear();

		Button CreatePresetPill(string label, double targetDb)
		{
			var pillBtn = new Button
			{
				Tag = targetDb,
				Padding = new Thickness(14, 6, 14, 6),
				CornerRadius = new CornerRadius(8),
				BorderThickness = new Thickness(1),
				Background = new SolidColorBrush(Windows.UI.Color.FromArgb(210, 255, 255, 255)),
				BorderBrush = new SolidColorBrush(Windows.UI.Color.FromArgb(180, 203, 213, 225))
			};
			Microsoft.UI.Xaml.Automation.AutomationProperties.SetName(pillBtn, $"快速应用 {label} {targetDb:+0;-0;0} dB");
			ToolTipService.SetToolTip(pillBtn, $"快速切换至 {label} {targetDb:+0;-0;0} dB");
			var contentSp = new StackPanel { Spacing = 1, HorizontalAlignment = HorizontalAlignment.Center };
			contentSp.Children.Add(new TextBlock { Text = label, FontSize = 11, Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 100, 116, 139)), HorizontalAlignment = HorizontalAlignment.Center });
			contentSp.Children.Add(new TextBlock { Text = $"{targetDb:+0;-0;0} dB", FontSize = 12, FontWeight = Microsoft.UI.Text.FontWeights.SemiBold, Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 15, 23, 42)), HorizontalAlignment = HorizontalAlignment.Center });
			pillBtn.Content = contentSp;
			pillBtn.Click += (s, e) =>
			{
				if (_headroomGainSlider != null) _headroomGainSlider.Value = targetDb;
			};
			_headroomPresetButtons.Add(pillBtn);
			return pillBtn;
		}

		presetsSp.Children.Add(CreatePresetPill("增益", 6.0));
		presetsSp.Children.Add(CreatePresetPill("直通", 0.0));
		presetsSp.Children.Add(CreatePresetPill("日常", -3.0));
		presetsSp.Children.Add(CreatePresetPill("DSP", -6.0));

		_headroomAuditionBtn = new Button
		{
			Content = new StackPanel
			{
				Orientation = Orientation.Horizontal,
				Spacing = 6,
				Children =
				{
					new FontIcon { Glyph = "\uE7F6", FontSize = 13, Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 79, 70, 229)) },
					new TextBlock { Text = "按住听直通", FontSize = 12, FontWeight = Microsoft.UI.Text.FontWeights.Medium, Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 30, 41, 59)) }
				}
			},
			Padding = new Thickness(14, 8, 14, 8),
			CornerRadius = new CornerRadius(8),
			Background = new SolidColorBrush(Windows.UI.Color.FromArgb(210, 255, 255, 255)),
			BorderBrush = new SolidColorBrush(Windows.UI.Color.FromArgb(180, 203, 213, 225)),
			BorderThickness = new Thickness(1)
		};
		Microsoft.UI.Xaml.Automation.AutomationProperties.SetName(_headroomAuditionBtn, "按住临时直通试听");
		ToolTipService.SetToolTip(_headroomAuditionBtn, "按住此按钮可临时旁路 Headroom 增益进行原声 A/B 试听，松开恢复预设");
		_headroomAuditionBtn.PointerPressed += (s, e) =>
		{
			if (ViewModel != null)
			{
				ViewModel.EchoHeadroomDirectBypassActive = true;
				ViewModel.ApplyHeadroomSettings();
				if (_headroomGainBigTb != null) _headroomGainBigTb.Text = "0";
				if (_headroomAmpRatioTb != null) _headroomAmpRatioTb.Text = "×1.000  100.0% amplitude (直通试听中)";
			}
		};
		void ReleaseAudition()
		{
			if (ViewModel != null && ViewModel.EchoHeadroomDirectBypassActive)
			{
				ViewModel.EchoHeadroomDirectBypassActive = false;
				ViewModel.ApplyHeadroomSettings();
				if (ViewModel != null)
				{
					if (_headroomGainBigTb != null) _headroomGainBigTb.Text = $"{ViewModel.EchoHeadroomDb:+0.0;-0.0;0.0}".TrimEnd('0').TrimEnd('.');
					double amp = Math.Pow(10.0, ViewModel.EchoHeadroomDb / 20.0);
					if (_headroomAmpRatioTb != null) _headroomAmpRatioTb.Text = $"×{amp:F3}  {amp * 100.0:F1}% amplitude";
				}
			}
		}
		_headroomAuditionBtn.PointerReleased += (s, e) => ReleaseAudition();
		_headroomAuditionBtn.PointerCaptureLost += (s, e) => ReleaseAudition();
		_headroomAuditionBtn.PointerCanceled += (s, e) => ReleaseAudition();
		_headroomAuditionBtn.PointerExited += (s, e) => ReleaseAudition();
		presetsSp.Children.Add(_headroomAuditionBtn);

		gainStack.Children.Add(presetsSp);

		gainStack.Children.Add(new TextBlock
		{
			Text = "向右提高增益会直接加大音量。还没有峰值读数，安全范围先按不超过 0 dB。",
			FontSize = 11,
			Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 100, 116, 139)),
			Margin = new Thickness(0, 10, 0, 0)
		});

		gainCard.Child = gainStack;
		Grid.SetColumn(gainCard, 0);
		dualColGrid.Children.Add(gainCard);

		// 右列：340px 实时余量卡片
		var rightCard = new Border
		{
			Background = CreateAcrylicCardBrush(0.88, 235),
			BorderBrush = new SolidColorBrush(Windows.UI.Color.FromArgb(140, 226, 232, 240)),
			BorderThickness = new Thickness(1),
			CornerRadius = new CornerRadius(12),
			Padding = new Thickness(20, 18, 20, 18)
		};
		var rightStack = new StackPanel { Spacing = 12 };

		rightStack.Children.Add(new TextBlock
		{
			Text = "实时余量",
			FontSize = 15,
			FontWeight = Microsoft.UI.Text.FontWeights.Bold,
			Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 15, 23, 42))
		});

		rightStack.Children.Add(new TextBlock
		{
			Text = "-- --",
			FontSize = 38,
			FontWeight = Microsoft.UI.Text.FontWeights.Bold,
			Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 15, 23, 42)),
			Margin = new Thickness(0, 4, 0, 2)
		});

		var lampSp = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8, VerticalAlignment = VerticalAlignment.Center };
		var lampDot = new Ellipse { Width = 8, Height = 8, Fill = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 22, 163, 74)) };
		lampSp.Children.Add(lampDot);
		lampSp.Children.Add(new TextBlock { Text = "等待当前宿主读数", FontSize = 12, Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 100, 116, 139)) });
		rightStack.Children.Add(lampSp);

		var recoCard = new Border
		{
			Background = new SolidColorBrush(Windows.UI.Color.FromArgb(160, 248, 250, 252)),
			BorderBrush = new SolidColorBrush(Windows.UI.Color.FromArgb(140, 226, 232, 240)),
			BorderThickness = new Thickness(1),
			CornerRadius = new CornerRadius(10),
			Padding = new Thickness(16, 14, 16, 14),
			Margin = new Thickness(0, 10, 0, 0)
		};
		var recoStack = new StackPanel { Spacing = 8 };

		var recoHeadSp = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 6 };
		recoHeadSp.Children.Add(new FontIcon { Glyph = "\uEA18", FontSize = 12, Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 79, 70, 229)) });
		recoHeadSp.Children.Add(new TextBlock { Text = "建议 · 安全", FontSize = 11, Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 71, 85, 105)) });
		recoStack.Children.Add(recoHeadSp);

		recoStack.Children.Add(new TextBlock
		{
			Text = "保持待命",
			FontSize = 18,
			FontWeight = Microsoft.UI.Text.FontWeights.Bold,
			Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 15, 23, 42))
		});
		recoStack.Children.Add(new TextBlock
		{
			Text = "有 DSP 模块开启，但暂未检测到风险。",
			FontSize = 12,
			Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 100, 116, 139)),
			TextWrapping = TextWrapping.Wrap
		});

		var recoBtnsSp = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 10, Margin = new Thickness(0, 4, 0, 0) };
		var btnApplyNeg3 = new Button
		{
			Content = "应用 -3 dB",
			Background = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 79, 70, 229)),
			Foreground = new SolidColorBrush(Colors.White),
			FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
			Padding = new Thickness(14, 7, 14, 7),
			CornerRadius = new CornerRadius(8)
		};
		btnApplyNeg3.Click += (s, e) =>
		{
			if (_headroomGainSlider != null) _headroomGainSlider.Value = -3.0;
		};
		var btnSetNeg6 = new Button
		{
			Content = "设为 -6 dB",
			Background = new SolidColorBrush(Windows.UI.Color.FromArgb(210, 255, 255, 255)),
			BorderBrush = new SolidColorBrush(Windows.UI.Color.FromArgb(180, 203, 213, 225)),
			BorderThickness = new Thickness(1),
			Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 30, 41, 59)),
			Padding = new Thickness(14, 7, 14, 7),
			CornerRadius = new CornerRadius(8)
		};
		btnSetNeg6.Click += (s, e) =>
		{
			if (_headroomGainSlider != null) _headroomGainSlider.Value = -6.0;
		};
		recoBtnsSp.Children.Add(btnApplyNeg3);
		recoBtnsSp.Children.Add(btnSetNeg6);
		recoStack.Children.Add(recoBtnsSp);

		recoCard.Child = recoStack;
		rightStack.Children.Add(recoCard);

		rightCard.Child = rightStack;
		Grid.SetColumn(rightCard, 1);
		dualColGrid.Children.Add(rightCard);

		Grid.SetRow(dualColGrid, 3);
		root.Children.Add(dualColGrid);

		// --- 4. 底部 Headroom 预算卡片 ---
		var budgetCard = new Border
		{
			Background = CreateAcrylicCardBrush(0.88, 235),
			BorderBrush = new SolidColorBrush(Windows.UI.Color.FromArgb(140, 226, 232, 240)),
			BorderThickness = new Thickness(1),
			CornerRadius = new CornerRadius(12),
			Padding = new Thickness(20, 16, 20, 16)
		};
		var budgetStack = new StackPanel { Spacing = 12 };

		var budgetHeadGrid = new Grid();
		budgetHeadGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.0, GridUnitType.Star) });
		budgetHeadGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

		var budgetHeadSp = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 10, VerticalAlignment = VerticalAlignment.Center };
		budgetHeadSp.Children.Add(new TextBlock { Text = "Headroom 预算", FontSize = 15, FontWeight = Microsoft.UI.Text.FontWeights.Bold, Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 15, 23, 42)) });
		budgetHeadSp.Children.Add(new TextBlock { Text = "等待当前宿主读数", FontSize = 12, Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 100, 116, 139)), VerticalAlignment = VerticalAlignment.Center });
		Grid.SetColumn(budgetHeadSp, 0);
		budgetHeadGrid.Children.Add(budgetHeadSp);

		var btnRefreshBudget = new Button
		{
			Content = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 5, Children = { new FontIcon { Glyph = "\uE72C", FontSize = 12, Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 100, 116, 139)) }, new TextBlock { Text = "刷新状态", FontSize = 12, Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 51, 65, 85)) } } },
			Padding = new Thickness(10, 4, 10, 4),
			CornerRadius = new CornerRadius(6),
			Background = new SolidColorBrush(Windows.UI.Color.FromArgb(210, 255, 255, 255)),
			BorderBrush = new SolidColorBrush(Windows.UI.Color.FromArgb(140, 226, 232, 240)),
			BorderThickness = new Thickness(1)
		};
		Grid.SetColumn(btnRefreshBudget, 1);
		budgetHeadGrid.Children.Add(btnRefreshBudget);
		budgetStack.Children.Add(budgetHeadGrid);

		var meterScaleGrid = new Grid { Margin = new Thickness(80, 0, 50, 0) };
		meterScaleGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.0, GridUnitType.Star) });
		meterScaleGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.0, GridUnitType.Star) });
		meterScaleGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.0, GridUnitType.Star) });
		meterScaleGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.0, GridUnitType.Star) });
		meterScaleGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.0, GridUnitType.Star) });
		meterScaleGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

		meterScaleGrid.Children.Add(new TextBlock { Text = "-24", FontSize = 10, Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 100, 116, 139)), HorizontalAlignment = HorizontalAlignment.Left });
		var ms1 = new TextBlock { Text = "-18", FontSize = 10, Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 100, 116, 139)), HorizontalAlignment = HorizontalAlignment.Center };
		Grid.SetColumn(ms1, 1); meterScaleGrid.Children.Add(ms1);
		var ms2 = new TextBlock { Text = "-12", FontSize = 10, Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 100, 116, 139)), HorizontalAlignment = HorizontalAlignment.Center };
		Grid.SetColumn(ms2, 2); meterScaleGrid.Children.Add(ms2);
		var ms3 = new TextBlock { Text = "-6", FontSize = 10, Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 100, 116, 139)), HorizontalAlignment = HorizontalAlignment.Center };
		Grid.SetColumn(ms3, 3); meterScaleGrid.Children.Add(ms3);
		var ms4 = new TextBlock { Text = "0", FontSize = 10, Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 100, 116, 139)), HorizontalAlignment = HorizontalAlignment.Center };
		Grid.SetColumn(ms4, 4); meterScaleGrid.Children.Add(ms4);
		var msUnit = new TextBlock { Text = "dBFS", FontSize = 10, Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 100, 116, 139)), Margin = new Thickness(8, 0, 0, 0) };
		Grid.SetColumn(msUnit, 5); meterScaleGrid.Children.Add(msUnit);
		budgetStack.Children.Add(meterScaleGrid);

		Grid CreateLedMeterRow(string label)
		{
			var row = new Grid { ColumnSpacing = 12, Margin = new Thickness(0, 2, 0, 2) };
			row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(80, GridUnitType.Pixel) });
			row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.0, GridUnitType.Star) });
			row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(50, GridUnitType.Pixel) });

			var lblTb = new TextBlock { Text = label, FontSize = 12, Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 51, 65, 85)), VerticalAlignment = VerticalAlignment.Center };
			Grid.SetColumn(lblTb, 0); row.Children.Add(lblTb);

			var segSp = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 2, VerticalAlignment = VerticalAlignment.Center };
			for (int i = 0; i < 36; i++)
			{
				var seg = new Border
				{
					Width = 8,
					Height = 14,
					CornerRadius = new CornerRadius(1),
					Background = (i == 0)
						? new SolidColorBrush(Windows.UI.Color.FromArgb(255, 79, 70, 229))
						: new SolidColorBrush(Windows.UI.Color.FromArgb(255, 226, 232, 240))
				};
				segSp.Children.Add(seg);
			}
			Grid.SetColumn(segSp, 1); row.Children.Add(segSp);

			var valTb = new TextBlock { Text = "--", FontSize = 12, FontWeight = Microsoft.UI.Text.FontWeights.SemiBold, Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 15, 23, 42)), HorizontalAlignment = HorizontalAlignment.Right, VerticalAlignment = VerticalAlignment.Center };
			Grid.SetColumn(valTb, 2); row.Children.Add(valTb);
			return row;
		}

		budgetStack.Children.Add(CreateLedMeterRow("输入峰值"));
		budgetStack.Children.Add(CreateLedMeterRow("输出估算"));
		budgetStack.Children.Add(CreateLedMeterRow("True Peak"));

		var statusFoldBtn = new Button
		{
			Content = "▼ 实时状态",
			Background = new SolidColorBrush(Colors.Transparent),
			BorderThickness = new Thickness(0),
			Padding = new Thickness(0, 6, 0, 2),
			FontSize = 12,
			Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 79, 70, 229)),
			FontWeight = Microsoft.UI.Text.FontWeights.Medium
		};
		var statusFoldContent = new Grid { Margin = new Thickness(0, 4, 0, 0), ColumnSpacing = 16 };
		statusFoldContent.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.0, GridUnitType.Star) });
		statusFoldContent.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.0, GridUnitType.Star) });
		statusFoldContent.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.0, GridUnitType.Star) });
		statusFoldContent.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.0, GridUnitType.Star) });
		statusFoldContent.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.0, GridUnitType.Star) });

		StackPanel MakeStatusStat(string statTitle, string statVal, Windows.UI.Color? col = null)
		{
			var sp = new StackPanel { Spacing = 2 };
			sp.Children.Add(new TextBlock { Text = statTitle, FontSize = 11, Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 100, 116, 139)) });
			sp.Children.Add(new TextBlock { Text = statVal, FontSize = 13, FontWeight = Microsoft.UI.Text.FontWeights.SemiBold, Foreground = new SolidColorBrush(col ?? Windows.UI.Color.FromArgb(255, 15, 23, 42)) });
			return sp;
		}
		var st1 = MakeStatusStat("保护状态", "待命", Windows.UI.Color.FromArgb(255, 22, 163, 74));
		var st2 = MakeStatusStat("削波次数", "0 次", Windows.UI.Color.FromArgb(255, 22, 163, 74));
		var st3 = MakeStatusStat("最近削波", "无记录");
		var st4 = MakeStatusStat("True Peak", "--");
		var st5 = MakeStatusStat("实时余量", "--");
		Grid.SetColumn(st1, 0); statusFoldContent.Children.Add(st1);
		Grid.SetColumn(st2, 1); statusFoldContent.Children.Add(st2);
		Grid.SetColumn(st3, 2); statusFoldContent.Children.Add(st3);
		Grid.SetColumn(st4, 3); statusFoldContent.Children.Add(st4);
		Grid.SetColumn(st5, 4); statusFoldContent.Children.Add(st5);

		statusFoldBtn.Click += (s, e) =>
		{
			if (statusFoldContent.Visibility == Visibility.Visible)
			{
				statusFoldContent.Visibility = Visibility.Collapsed;
				statusFoldBtn.Content = "▶ 实时状态";
			}
			else
			{
				statusFoldContent.Visibility = Visibility.Visible;
				statusFoldBtn.Content = "▼ 实时状态";
			}
		};
		budgetStack.Children.Add(statusFoldBtn);
		budgetStack.Children.Add(statusFoldContent);

		budgetCard.Child = budgetStack;
		Grid.SetRow(budgetCard, 4);
		root.Children.Add(budgetCard);

		// --- 联动更新 ---
		void UpdateHeadroomDisplay(double db)
		{
			if (_headroomGainBigTb != null) _headroomGainBigTb.Text = $"{db:+0.0;-0.0;0.0}".TrimEnd('0').TrimEnd('.');
			double amp = Math.Pow(10.0, db / 20.0);
			if (_headroomAmpRatioTb != null) _headroomAmpRatioTb.Text = $"×{amp:F3}  {amp * 100.0:F1}% amplitude";
			if (_headroomNavSubTb != null)
			{
				bool enabled = ViewModel?.EchoHeadroomEnabled ?? true;
				_headroomNavSubTb.Text = !enabled ? "已旁路" : $"{db:+0.0;-0.0;0.0} dB";
			}
			foreach (var btn in _headroomPresetButtons)
			{
				if (btn.Tag is double targetDb)
				{
					bool isMatch = Math.Abs(targetDb - db) < 0.1;
					btn.Background = isMatch
						? new SolidColorBrush(Windows.UI.Color.FromArgb(255, 238, 242, 255))
						: new SolidColorBrush(Windows.UI.Color.FromArgb(255, 255, 255, 255));
					btn.BorderBrush = isMatch
						? new SolidColorBrush(Windows.UI.Color.FromArgb(255, 79, 70, 229))
						: new SolidColorBrush(Windows.UI.Color.FromArgb(255, 203, 213, 225));
					if (btn.Content is StackPanel sp && sp.Children.Count >= 2)
					{
						if (sp.Children[0] is TextBlock t1) t1.Foreground = new SolidColorBrush(isMatch ? Windows.UI.Color.FromArgb(255, 79, 70, 229) : Windows.UI.Color.FromArgb(255, 100, 116, 139));
						if (sp.Children[1] is TextBlock t2) t2.Foreground = new SolidColorBrush(isMatch ? Windows.UI.Color.FromArgb(255, 79, 70, 229) : Windows.UI.Color.FromArgb(255, 15, 23, 42));
					}
				}
			}
		}

		_headroomGainSlider.ValueChanged += (s, e) =>
		{
			if (ViewModel != null)
			{
				ViewModel.EchoHeadroomDb = e.NewValue;
				ViewModel.ApplyHeadroomSettings();
				UpdateHeadroomDisplay(e.NewValue);
			}
		};
		_headroomPowerSwitch.Toggled += (s, e) =>
		{
			if (ViewModel != null)
			{
				ViewModel.EchoHeadroomEnabled = _headroomPowerSwitch.IsOn;
				ViewModel.ApplyHeadroomSettings();
				UpdateHeadroomDisplay(ViewModel.EchoHeadroomDb);
			}
		};
		btnMinus.Click += (s, e) =>
		{
			if (_headroomGainSlider != null && _headroomGainSlider.Value > _headroomGainSlider.Minimum)
			{
				_headroomGainSlider.Value = Math.Max(_headroomGainSlider.Minimum, _headroomGainSlider.Value - 0.5);
			}
		};
		btnPlus.Click += (s, e) =>
		{
			if (_headroomGainSlider != null && _headroomGainSlider.Value < _headroomGainSlider.Maximum)
			{
				_headroomGainSlider.Value = Math.Min(_headroomGainSlider.Maximum, _headroomGainSlider.Value + 0.5);
			}
		};

		UpdateHeadroomDisplay(initDb);

		sv.Content = root;
		_headroomWorkbench = new Border { Child = sv };
		return _headroomWorkbench;
	}

	private FrameworkElement CreateParametricEqWorkbench()
	{
		var root = new Grid
		{
			Background = CreateWhiteAcrylicBrush(0.72, 195),
			Padding = new Thickness(24, 20, 24, 20),
			RowSpacing = 14
		};
		root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // 0: Top title & controls
		root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1.0, GridUnitType.Star) }); // 1: Scrollable workbench body

		// --- 0. Top Title & Quick Actions Row (matching Image 3) ---
		var topBar = new Grid { ColumnSpacing = 16 };
		topBar.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto }); // Title + Power
		topBar.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.0, GridUnitType.Star) }); // Middle spacer / options
		topBar.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto }); // Preset & Tools & Scale

		var leftTitleSp = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 14, VerticalAlignment = VerticalAlignment.Center };
		var titleTb = new TextBlock
		{
			Text = "参数 EQ",
			FontSize = 22,
			FontWeight = Microsoft.UI.Text.FontWeights.Bold,
			Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 15, 23, 42)), // Slate 900
			VerticalAlignment = VerticalAlignment.Center
		};
		leftTitleSp.Children.Add(titleTb);

		var powerSwitch = new ToggleSwitch
		{
			IsOn = ViewModel?.EchoEqEnabled ?? true,
			OnContent = "",
			OffContent = "",
			VerticalAlignment = VerticalAlignment.Center
		};
		powerSwitch.Toggled += (s, e) =>
		{
			if (ViewModel != null)
			{
				ViewModel.EchoEqEnabled = powerSwitch.IsOn;
				ViewModel.Engine?.SetEchoEqEnabled(powerSwitch.IsOn);
			}
		};
		leftTitleSp.Children.Add(powerSwitch);
		Grid.SetColumn(leftTitleSp, 0);
		topBar.Children.Add(leftTitleSp);

		var rightSp = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 10, VerticalAlignment = VerticalAlignment.Center };

		// Simple / Pro segmented button (White Acrylic styled)
		var modeBorder = new Border
		{
			Background = new SolidColorBrush(Windows.UI.Color.FromArgb(180, 241, 245, 249)),
			BorderBrush = new SolidColorBrush(Windows.UI.Color.FromArgb(140, 226, 232, 240)),
			BorderThickness = new Thickness(1),
			CornerRadius = new CornerRadius(14),
			Padding = new Thickness(2)
		};
		var modeSp = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 2 };
		var btnSimple = new Button
		{
			Content = "Simple",
			Padding = new Thickness(12, 3, 12, 3),
			CornerRadius = new CornerRadius(12),
			FontSize = 12,
			Background = new SolidColorBrush(Colors.Transparent),
			Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 100, 116, 139)), // Slate 500
			BorderThickness = new Thickness(0)
		};
		var btnPro = new Button
		{
			Content = "Pro",
			Padding = new Thickness(12, 3, 12, 3),
			CornerRadius = new CornerRadius(12),
			FontSize = 12,
			Background = new SolidColorBrush(Windows.UI.Color.FromArgb(220, 255, 255, 255)),
			Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 15, 23, 42)), // Slate 900
			FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
			BorderThickness = new Thickness(0)
		};
		btnSimple.Click += (s, e) =>
		{
			btnSimple.Background = new SolidColorBrush(Windows.UI.Color.FromArgb(220, 255, 255, 255));
			btnSimple.Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 15, 23, 42));
			btnSimple.FontWeight = Microsoft.UI.Text.FontWeights.SemiBold;
			btnPro.Background = new SolidColorBrush(Colors.Transparent);
			btnPro.Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 100, 116, 139));
			btnPro.FontWeight = Microsoft.UI.Text.FontWeights.Normal;
		};
		btnPro.Click += (s, e) =>
		{
			btnPro.Background = new SolidColorBrush(Windows.UI.Color.FromArgb(220, 255, 255, 255));
			btnPro.Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 15, 23, 42));
			btnPro.FontWeight = Microsoft.UI.Text.FontWeights.SemiBold;
			btnSimple.Background = new SolidColorBrush(Colors.Transparent);
			btnSimple.Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 100, 116, 139));
			btnSimple.FontWeight = Microsoft.UI.Text.FontWeights.Normal;
		};
		modeSp.Children.Add(btnSimple);
		modeSp.Children.Add(btnPro);
		modeBorder.Child = modeSp;
		rightSp.Children.Add(modeBorder);

		// Preset Dropdown (Translucent White Acrylic)
		var presetCombo = new ComboBox
		{
			MinWidth = 240,
			CornerRadius = new CornerRadius(8),
			Background = new SolidColorBrush(Windows.UI.Color.FromArgb(220, 255, 255, 255)),
			BorderBrush = new SolidColorBrush(Windows.UI.Color.FromArgb(160, 203, 213, 225)),
			BorderThickness = new Thickness(1),
			Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 15, 23, 42))
		};
		string[] presets = {
			"平坦 (Flat)",
			"耳机校正 - Moondrop / Parameter (哈曼目标)",
			"耳机校正 - Sennheiser HD600 (弥散场优化)",
			"耳机校正 - Sony WH-1000XM5 (下潜饱满)",
			"哈曼目标曲线 Harman Target 2019",
			"低频重低音增强 (Sub-Bass Boost +6dB)",
			"高频通透空气感 (High Air Brilliance)",
			"暖声人声突出 (Warm Vocal Presence)",
			"开阔立体声场 (Open Soundstage)"
		};
		foreach (var p in presets) presetCombo.Items.Add(new ComboBoxItem { Content = p });
		presetCombo.SelectedIndex = 1;
		presetCombo.SelectionChanged += (s, e) =>
		{
			if (presetCombo.SelectedIndex >= 0)
			{
				ApplyEqBuiltinPreset(presetCombo.SelectedIndex);
			}
		};
		rightSp.Children.Add(presetCombo);

		// Spectrum button (White Acrylic)
		var btnSpectrum = new Button
		{
			Content = new StackPanel
			{
				Orientation = Orientation.Horizontal,
				Spacing = 5,
				Children = { new FontIcon { Glyph = "\uE9D9", FontSize = 14 }, new TextBlock { Text = "频谱", FontSize = 12, VerticalAlignment = VerticalAlignment.Center } }
			},
			Padding = new Thickness(10, 4, 10, 4),
			CornerRadius = new CornerRadius(8),
			Background = new SolidColorBrush(Windows.UI.Color.FromArgb(210, 255, 255, 255)),
			BorderBrush = new SolidColorBrush(Windows.UI.Color.FromArgb(140, 226, 232, 240)),
			BorderThickness = new Thickness(1),
			Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 51, 65, 85))
		};
		btnSpectrum.Click += (s, e) =>
		{
			if (_eqCurveControl != null)
			{
				_eqCurveControl.ShowSpectrum = !_eqCurveControl.ShowSpectrum;
				btnSpectrum.Background = _eqCurveControl.ShowSpectrum
					? new SolidColorBrush(Windows.UI.Color.FromArgb(235, 238, 242, 255))
					: new SolidColorBrush(Windows.UI.Color.FromArgb(210, 255, 255, 255));
				btnSpectrum.BorderBrush = _eqCurveControl.ShowSpectrum
					? new SolidColorBrush(Windows.UI.Color.FromArgb(255, 99, 102, 241))
					: new SolidColorBrush(Windows.UI.Color.FromArgb(140, 226, 232, 240));
				btnSpectrum.Foreground = _eqCurveControl.ShowSpectrum
					? new SolidColorBrush(Windows.UI.Color.FromArgb(255, 79, 70, 229))
					: new SolidColorBrush(Windows.UI.Color.FromArgb(255, 51, 65, 85));
			}
		};
		rightSp.Children.Add(btnSpectrum);

		// Undo / Redo / Reset buttons
		var btnUndo = new Button
		{
			Content = new FontIcon { Glyph = "\uE7A7", FontSize = 13 },
			Padding = new Thickness(8, 4, 8, 4),
			CornerRadius = new CornerRadius(8),
			Background = new SolidColorBrush(Windows.UI.Color.FromArgb(210, 255, 255, 255)),
			BorderBrush = new SolidColorBrush(Windows.UI.Color.FromArgb(140, 226, 232, 240)),
			BorderThickness = new Thickness(1),
			Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 71, 85, 105))
		};
		var btnRedo = new Button
		{
			Content = new FontIcon { Glyph = "\uE7A6", FontSize = 13 },
			Padding = new Thickness(8, 4, 8, 4),
			CornerRadius = new CornerRadius(8),
			Background = new SolidColorBrush(Windows.UI.Color.FromArgb(210, 255, 255, 255)),
			BorderBrush = new SolidColorBrush(Windows.UI.Color.FromArgb(140, 226, 232, 240)),
			BorderThickness = new Thickness(1),
			Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 71, 85, 105))
		};
		var btnResetCurve = new Button
		{
			Content = new FontIcon { Glyph = "\uE72C", FontSize = 13 },
			Padding = new Thickness(8, 4, 8, 4),
			CornerRadius = new CornerRadius(8),
			Background = new SolidColorBrush(Windows.UI.Color.FromArgb(210, 255, 255, 255)),
			BorderBrush = new SolidColorBrush(Windows.UI.Color.FromArgb(140, 226, 232, 240)),
			BorderThickness = new Thickness(1),
			Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 71, 85, 105))
		};
		btnResetCurve.Click += (s, e) => ResetEqBandsToFlat();
		rightSp.Children.Add(btnUndo);
		rightSp.Children.Add(btnRedo);
		rightSp.Children.Add(btnResetCurve);

		// Scale Controls: + ±12 dB - | 自动
		var scaleSp = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 3, VerticalAlignment = VerticalAlignment.Center };
		var btnZoomIn = new Button
		{
			Content = "+",
			Padding = new Thickness(8, 2, 8, 2),
			FontSize = 12,
			CornerRadius = new CornerRadius(6),
			Background = new SolidColorBrush(Windows.UI.Color.FromArgb(210, 255, 255, 255)),
			BorderBrush = new SolidColorBrush(Windows.UI.Color.FromArgb(140, 226, 232, 240)),
			BorderThickness = new Thickness(1),
			Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 51, 65, 85))
		};
		var tbScale = new TextBlock
		{
			Text = "±12 dB",
			FontSize = 12,
			FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
			VerticalAlignment = VerticalAlignment.Center,
			Margin = new Thickness(6, 0, 6, 0),
			Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 15, 23, 42)) // Slate 900
		};
		var btnZoomOut = new Button
		{
			Content = "-",
			Padding = new Thickness(8, 2, 8, 2),
			FontSize = 12,
			CornerRadius = new CornerRadius(6),
			Background = new SolidColorBrush(Windows.UI.Color.FromArgb(210, 255, 255, 255)),
			BorderBrush = new SolidColorBrush(Windows.UI.Color.FromArgb(140, 226, 232, 240)),
			BorderThickness = new Thickness(1),
			Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 51, 65, 85))
		};
		var btnAuto = new Button
		{
			Content = "自动",
			Padding = new Thickness(10, 2, 10, 2),
			FontSize = 12,
			CornerRadius = new CornerRadius(6),
			Background = new SolidColorBrush(Windows.UI.Color.FromArgb(210, 255, 255, 255)),
			BorderBrush = new SolidColorBrush(Windows.UI.Color.FromArgb(140, 226, 232, 240)),
			BorderThickness = new Thickness(1),
			Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 51, 65, 85))
		};
		btnZoomIn.Click += (s, e) =>
		{
			if (_eqCurveControl != null && _eqCurveControl.DbRange > 6.0)
			{
				_eqCurveControl.DbRange -= 6.0;
				tbScale.Text = $"±{_eqCurveControl.DbRange:F0} dB";
			}
		};
		btnZoomOut.Click += (s, e) =>
		{
			if (_eqCurveControl != null && _eqCurveControl.DbRange < 30.0)
			{
				_eqCurveControl.DbRange += 6.0;
				tbScale.Text = $"±{_eqCurveControl.DbRange:F0} dB";
			}
		};
		scaleSp.Children.Add(btnZoomIn);
		scaleSp.Children.Add(tbScale);
		scaleSp.Children.Add(btnZoomOut);
		scaleSp.Children.Add(btnAuto);
		rightSp.Children.Add(scaleSp);

		Grid.SetColumn(rightSp, 2);
		topBar.Children.Add(rightSp);
		Grid.SetRow(topBar, 0);
		root.Children.Add(topBar);

		// --- 1. Main Workbench Scrollable Content ---
		var svBody = new ScrollViewer
		{
			VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
			HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled
		};
		var bodyStack = new StackPanel { Spacing = 14 };

		// A. Large Interactive EQ Curve Display (ParametricEqCurveControl) wrapped in White Acrylic Border
		_eqCurveControl = new ParametricEqCurveControl
		{
			Height = 330,
			HorizontalAlignment = HorizontalAlignment.Stretch
		};
		var curveCard = new Border
		{
			Background = CreateAcrylicCardBrush(0.88, 235),
			BorderBrush = new SolidColorBrush(Windows.UI.Color.FromArgb(140, 226, 232, 240)),
			BorderThickness = new Thickness(1),
			CornerRadius = new CornerRadius(12),
			Padding = new Thickness(2),
			Child = _eqCurveControl
		};
		if (ViewModel != null)
		{
			ViewModel.InitializeEchoEqBands();
			_eqCurveControl.SetBands(ViewModel.EchoEqBands, 0);
		}
		_eqCurveControl.BandSelected += (idx) => SelectEqBand(idx);
		_eqCurveControl.BandChanged += (idx, freq, gain) => OnCurveBandMoved(idx, freq, gain);
		bodyStack.Children.Add(curveCard);

		// B. Band Carousel / Horizontal Pill List Row (White Acrylic)
		var carouselCard = new Border
		{
			Background = CreateAcrylicCardBrush(0.88, 235),
			BorderBrush = new SolidColorBrush(Windows.UI.Color.FromArgb(140, 226, 232, 240)),
			BorderThickness = new Thickness(1),
			CornerRadius = new CornerRadius(12),
			Padding = new Thickness(16, 12, 16, 12)
		};
		var carouselGrid = new Grid();
		carouselGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Status text & compare
		carouselGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Horizontal band pills

		var statusRow = new Grid { Margin = new Thickness(0, 0, 0, 8) };
		statusRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.0, GridUnitType.Star) });
		statusRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

		var statusLeftSp = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 12 };
		statusLeftSp.Children.Add(new TextBlock
		{
			Text = "● 待确认 余量 -0.0 dB",
			FontSize = 12,
			Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 22, 163, 74)), // Green 600
			FontWeight = Microsoft.UI.Text.FontWeights.SemiBold
		});
		statusLeftSp.Children.Add(new TextBlock
		{
			Text = "待改前",
			FontSize = 12,
			Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 100, 116, 139)) // Slate 500
		});
		statusLeftSp.Children.Add(new TextBlock
		{
			Text = "待改后",
			FontSize = 12,
			Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 100, 116, 139)) // Slate 500
		});
		Grid.SetColumn(statusLeftSp, 0);
		statusRow.Children.Add(statusLeftSp);

		var compareRightSp = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 10 };
		var btnAb = new Button
		{
			Content = "⇄ A/B 对比",
			Padding = new Thickness(10, 4, 10, 4),
			FontSize = 11,
			CornerRadius = new CornerRadius(6),
			Background = new SolidColorBrush(Windows.UI.Color.FromArgb(210, 255, 255, 255)),
			BorderBrush = new SolidColorBrush(Windows.UI.Color.FromArgb(140, 203, 213, 225)),
			BorderThickness = new Thickness(1),
			Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 51, 65, 85))
		};
		var btnLoudness = new Button
		{
			Content = "实时等响对比",
			Padding = new Thickness(10, 4, 10, 4),
			FontSize = 11,
			CornerRadius = new CornerRadius(6),
			Background = new SolidColorBrush(Windows.UI.Color.FromArgb(210, 255, 255, 255)),
			BorderBrush = new SolidColorBrush(Windows.UI.Color.FromArgb(140, 203, 213, 225)),
			BorderThickness = new Thickness(1),
			Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 51, 65, 85))
		};
		compareRightSp.Children.Add(btnAb);
		compareRightSp.Children.Add(btnLoudness);
		Grid.SetColumn(compareRightSp, 1);
		statusRow.Children.Add(compareRightSp);
		Grid.SetRow(statusRow, 0);
		carouselGrid.Children.Add(statusRow);

		// Band Pills
		var pillsSv = new ScrollViewer { HorizontalScrollBarVisibility = ScrollBarVisibility.Auto, VerticalScrollBarVisibility = ScrollBarVisibility.Disabled };
		_eqBandPillsStack = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
		RefreshBandPills();
		pillsSv.Content = _eqBandPillsStack;
		Grid.SetRow(pillsSv, 1);
		carouselGrid.Children.Add(pillsSv);

		carouselCard.Child = carouselGrid;
		bodyStack.Children.Add(carouselCard);

		// C. Selected Band Inspector Panel
		bodyStack.Children.Add(CreateBandInspectorCard());

		// D. Bottom Action Toolbar
		bodyStack.Children.Add(CreateEqBottomToolbar());

		svBody.Content = bodyStack;
		Grid.SetRow(svBody, 1);
		root.Children.Add(svBody);

		_parametricEqWorkbench = new Border { Child = root };
		return _parametricEqWorkbench;
	}

	private FrameworkElement CreateBandInspectorCard()
	{
		var card = new Border
		{
			Background = CreateAcrylicCardBrush(0.90, 240),
			BorderBrush = new SolidColorBrush(Windows.UI.Color.FromArgb(140, 226, 232, 240)), // Slate 200 (#E2E8F0)
			BorderThickness = new Thickness(1),
			CornerRadius = new CornerRadius(12),
			Padding = new Thickness(18, 16, 18, 16)
		};

		var stack = new StackPanel { Spacing = 14 };

		// Row 1: Band badge, Filter Type ComboBox, Enable checkbox, Reset, Delete
		var row1 = new Grid { ColumnSpacing = 12 };
		row1.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto }); // Badge
		row1.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto }); // Type combo
		row1.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.0, GridUnitType.Star) }); // Spacer
		row1.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto }); // Enable & Buttons

		_bandIndexBadge = new TextBlock
		{
			Text = "01",
			FontSize = 13,
			FontWeight = Microsoft.UI.Text.FontWeights.Bold,
			Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 79, 70, 229)), // Indigo 600
			VerticalAlignment = VerticalAlignment.Center,
			HorizontalAlignment = HorizontalAlignment.Center
		};
		var badgeBorder = new Border
		{
			Background = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 238, 242, 255)), // Indigo 50 (#EEF2FF)
			BorderBrush = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 99, 102, 241)), // Indigo 500 (#6366F1)
			BorderThickness = new Thickness(1.2),
			CornerRadius = new CornerRadius(14),
			Width = 32,
			Height = 32,
			Child = _bandIndexBadge
		};
		Grid.SetColumn(badgeBorder, 0);
		row1.Children.Add(badgeBorder);

		_filterTypeCombo = new ComboBox
		{
			MinWidth = 150,
			CornerRadius = new CornerRadius(8),
			Background = new SolidColorBrush(Windows.UI.Color.FromArgb(210, 255, 255, 255)),
			BorderBrush = new SolidColorBrush(Windows.UI.Color.FromArgb(180, 203, 213, 225)), // Slate 300
			BorderThickness = new Thickness(1),
			Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 15, 23, 42))
		};
		string[] typeNames = { "低架 (Low Shelf)", "峰值 (Peaking)", "高架 (High Shelf)", "低通 (Low Pass)", "高通 (High Pass)", "带通 (Band Pass)", "陷波 (Notch)" };
		foreach (var t in typeNames) _filterTypeCombo.Items.Add(new ComboBoxItem { Content = t });
		_filterTypeCombo.SelectedIndex = 1;
		_filterTypeCombo.SelectionChanged += OnInspectorFilterTypeChanged;
		Grid.SetColumn(_filterTypeCombo, 1);
		row1.Children.Add(_filterTypeCombo);

		var row1RightSp = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 10, VerticalAlignment = VerticalAlignment.Center };
		_bandEnableCb = new CheckBox
		{
			Content = "启用此段",
			IsChecked = true,
			FontSize = 13,
			FontWeight = Microsoft.UI.Text.FontWeights.Medium,
			Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 15, 23, 42))
		};
		_bandEnableCb.Checked += (s, e) => OnInspectorBandEnabledChanged(true);
		_bandEnableCb.Unchecked += (s, e) => OnInspectorBandEnabledChanged(false);
		row1RightSp.Children.Add(_bandEnableCb);

		var btnResetBand = new Button
		{
			Content = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 4, Children = { new FontIcon { Glyph = "\uE72C", FontSize = 12 }, new TextBlock { Text = "重置此段", FontSize = 12 } } },
			Padding = new Thickness(10, 5, 10, 5),
			CornerRadius = new CornerRadius(6),
			Background = new SolidColorBrush(Windows.UI.Color.FromArgb(210, 255, 255, 255)),
			BorderBrush = new SolidColorBrush(Windows.UI.Color.FromArgb(180, 203, 213, 225)),
			BorderThickness = new Thickness(1),
			Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 51, 65, 85))
		};
		btnResetBand.Click += (s, e) => ResetCurrentBand();
		row1RightSp.Children.Add(btnResetBand);

		var btnDeleteBand = new Button
		{
			Content = new FontIcon { Glyph = "\uE74D", FontSize = 13 },
			Padding = new Thickness(8, 5, 8, 5),
			CornerRadius = new CornerRadius(6),
			Background = new SolidColorBrush(Windows.UI.Color.FromArgb(210, 255, 255, 255)),
			BorderBrush = new SolidColorBrush(Windows.UI.Color.FromArgb(180, 254, 202, 202)),
			BorderThickness = new Thickness(1),
			Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 220, 38, 38))
		};
		btnDeleteBand.Click += (s, e) => ZeroCurrentBandGain();
		row1RightSp.Children.Add(btnDeleteBand);

		Grid.SetColumn(row1RightSp, 3);
		row1.Children.Add(row1RightSp);
		stack.Children.Add(row1);

		// Row 2: Frequency, Gain, Q Sliders in 3 columns
		var slidersGrid = new Grid { ColumnSpacing = 24 };
		slidersGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.0, GridUnitType.Star) }); // Freq
		slidersGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.0, GridUnitType.Star) }); // Gain
		slidersGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.0, GridUnitType.Star) }); // Q

		// Freq Column
		var freqCol = new StackPanel { Spacing = 4 };
		var freqHead = new Grid();
		freqHead.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.0, GridUnitType.Star) });
		freqHead.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
		freqHead.Children.Add(new TextBlock { Text = "频率 Frequency", FontSize = 12, FontWeight = Microsoft.UI.Text.FontWeights.Medium, Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 71, 85, 105)) });
		_freqValueTb = new TextBlock { Text = "1000 Hz", FontSize = 14, FontWeight = Microsoft.UI.Text.FontWeights.Bold, Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 2, 132, 199)) }; // Sky 600
		Grid.SetColumn(_freqValueTb, 1);
		freqHead.Children.Add(_freqValueTb);
		freqCol.Children.Add(freqHead);

		_freqSlider = new Slider { Minimum = 0, Maximum = 1000, StepFrequency = 1, Value = 500 };
		_freqSlider.ValueChanged += OnInspectorFreqSliderChanged;
		freqCol.Children.Add(_freqSlider);
		Grid.SetColumn(freqCol, 0);
		slidersGrid.Children.Add(freqCol);

		// Gain Column
		var gainCol = new StackPanel { Spacing = 4 };
		var gainHead = new Grid();
		gainHead.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.0, GridUnitType.Star) });
		gainHead.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
		gainHead.Children.Add(new TextBlock { Text = "增益 Gain", FontSize = 12, FontWeight = Microsoft.UI.Text.FontWeights.Medium, Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 71, 85, 105)) });
		_gainValueTb = new TextBlock { Text = "0.0 dB", FontSize = 14, FontWeight = Microsoft.UI.Text.FontWeights.Bold, Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 234, 88, 12)) }; // Orange 600
		Grid.SetColumn(_gainValueTb, 1);
		gainHead.Children.Add(_gainValueTb);
		gainCol.Children.Add(gainHead);

		_gainSlider = new Slider { Minimum = -18.0, Maximum = 18.0, StepFrequency = 0.1, Value = 0.0 };
		_gainSlider.ValueChanged += OnInspectorGainSliderChanged;
		gainCol.Children.Add(_gainSlider);
		Grid.SetColumn(gainCol, 1);
		slidersGrid.Children.Add(gainCol);

		// Q Column
		var qCol = new StackPanel { Spacing = 4 };
		var qHead = new Grid();
		qHead.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.0, GridUnitType.Star) });
		qHead.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
		qHead.Children.Add(new TextBlock { Text = "品质因数 Q", FontSize = 12, FontWeight = Microsoft.UI.Text.FontWeights.Medium, Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 71, 85, 105)) });
		_qValueTb = new TextBlock { Text = "1.41", FontSize = 14, FontWeight = Microsoft.UI.Text.FontWeights.Bold, Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 124, 58, 237)) }; // Violet 600
		Grid.SetColumn(_qValueTb, 1);
		qHead.Children.Add(_qValueTb);
		qCol.Children.Add(qHead);

		_qSlider = new Slider { Minimum = 0.1, Maximum = 10.0, StepFrequency = 0.05, Value = 1.41 };
		_qSlider.ValueChanged += OnInspectorQSliderChanged;
		qCol.Children.Add(_qSlider);

		var qTagsSp = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 6, HorizontalAlignment = HorizontalAlignment.Right };
		var btnWide = new Button
		{
			Content = "宽 (0.5)",
			Padding = new Thickness(8, 2, 8, 2),
			FontSize = 10,
			CornerRadius = new CornerRadius(4),
			Background = new SolidColorBrush(Windows.UI.Color.FromArgb(210, 255, 255, 255)),
			BorderBrush = new SolidColorBrush(Windows.UI.Color.FromArgb(180, 203, 213, 225)),
			BorderThickness = new Thickness(1),
			Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 51, 65, 85))
		};
		btnWide.Click += (s, e) => { if (_qSlider != null) _qSlider.Value = 0.5; };
		var btnStd = new Button
		{
			Content = "标准 (1.4)",
			Padding = new Thickness(8, 2, 8, 2),
			FontSize = 10,
			CornerRadius = new CornerRadius(4),
			Background = new SolidColorBrush(Windows.UI.Color.FromArgb(210, 255, 255, 255)),
			BorderBrush = new SolidColorBrush(Windows.UI.Color.FromArgb(180, 203, 213, 225)),
			BorderThickness = new Thickness(1),
			Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 51, 65, 85))
		};
		btnStd.Click += (s, e) => { if (_qSlider != null) _qSlider.Value = 1.41; };
		var btnNarrow = new Button
		{
			Content = "窄 (3.0)",
			Padding = new Thickness(8, 2, 8, 2),
			FontSize = 10,
			CornerRadius = new CornerRadius(4),
			Background = new SolidColorBrush(Windows.UI.Color.FromArgb(210, 255, 255, 255)),
			BorderBrush = new SolidColorBrush(Windows.UI.Color.FromArgb(180, 203, 213, 225)),
			BorderThickness = new Thickness(1),
			Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 51, 65, 85))
		};
		btnNarrow.Click += (s, e) => { if (_qSlider != null) _qSlider.Value = 3.0; };
		qTagsSp.Children.Add(btnWide);
		qTagsSp.Children.Add(btnStd);
		qTagsSp.Children.Add(btnNarrow);
		qCol.Children.Add(qTagsSp);

		Grid.SetColumn(qCol, 2);
		slidersGrid.Children.Add(qCol);
		stack.Children.Add(slidersGrid);

		// Row 3: Preamp, Scope, Snap
		var row3 = new Grid { ColumnSpacing = 16 };
		row3.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto }); // Snap & Scope
		row3.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.0, GridUnitType.Star) }); // Preamp slider

		var scopeSp = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 12, VerticalAlignment = VerticalAlignment.Center };
		var cbSnap = new CheckBox
		{
			Content = "吸附到标准频点",
			FontSize = 12,
			Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 51, 65, 85))
		};
		scopeSp.Children.Add(cbSnap);

		var scopeBorder = new Border
		{
			Background = new SolidColorBrush(Windows.UI.Color.FromArgb(180, 241, 245, 249)),
			BorderBrush = new SolidColorBrush(Windows.UI.Color.FromArgb(140, 226, 232, 240)),
			BorderThickness = new Thickness(1),
			CornerRadius = new CornerRadius(8),
			Padding = new Thickness(4, 2, 4, 2)
		};
		var scopeInnerSp = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 6 };
		scopeInnerSp.Children.Add(new TextBlock { Text = "作用域:", FontSize = 11, Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 71, 85, 105)), VerticalAlignment = VerticalAlignment.Center });
		scopeInnerSp.Children.Add(new Button
		{
			Content = "立体声",
			Padding = new Thickness(8, 2, 8, 2),
			FontSize = 11,
			CornerRadius = new CornerRadius(4),
			Background = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 79, 70, 229)), // Indigo 600
			Foreground = new SolidColorBrush(Colors.White),
			FontWeight = Microsoft.UI.Text.FontWeights.SemiBold
		});
		scopeInnerSp.Children.Add(new Button { Content = "左", Padding = new Thickness(8, 2, 8, 2), FontSize = 11, CornerRadius = new CornerRadius(4), Background = new SolidColorBrush(Windows.UI.Color.FromArgb(210, 255, 255, 255)), BorderBrush = new SolidColorBrush(Windows.UI.Color.FromArgb(180, 203, 213, 225)), BorderThickness = new Thickness(1), Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 51, 65, 85)) });
		scopeInnerSp.Children.Add(new Button { Content = "右", Padding = new Thickness(8, 2, 8, 2), FontSize = 11, CornerRadius = new CornerRadius(4), Background = new SolidColorBrush(Windows.UI.Color.FromArgb(210, 255, 255, 255)), BorderBrush = new SolidColorBrush(Windows.UI.Color.FromArgb(180, 203, 213, 225)), BorderThickness = new Thickness(1), Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 51, 65, 85)) });
		scopeInnerSp.Children.Add(new Button { Content = "中 (M)", Padding = new Thickness(8, 2, 8, 2), FontSize = 11, CornerRadius = new CornerRadius(4), Background = new SolidColorBrush(Windows.UI.Color.FromArgb(210, 255, 255, 255)), BorderBrush = new SolidColorBrush(Windows.UI.Color.FromArgb(180, 203, 213, 225)), BorderThickness = new Thickness(1), Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 51, 65, 85)) });
		scopeInnerSp.Children.Add(new Button { Content = "侧 (S)", Padding = new Thickness(8, 2, 8, 2), FontSize = 11, CornerRadius = new CornerRadius(4), Background = new SolidColorBrush(Windows.UI.Color.FromArgb(210, 255, 255, 255)), BorderBrush = new SolidColorBrush(Windows.UI.Color.FromArgb(180, 203, 213, 225)), BorderThickness = new Thickness(1), Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 51, 65, 85)) });
		scopeBorder.Child = scopeInnerSp;
		scopeSp.Children.Add(scopeBorder);

		Grid.SetColumn(scopeSp, 0);
		row3.Children.Add(scopeSp);

		// Preamp controls
		var preampSp = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 10, HorizontalAlignment = HorizontalAlignment.Right, VerticalAlignment = VerticalAlignment.Center };
		preampSp.Children.Add(new TextBlock { Text = "EQ 前级增益:", FontSize = 12, FontWeight = Microsoft.UI.Text.FontWeights.Medium, Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 71, 85, 105)), VerticalAlignment = VerticalAlignment.Center });
		_preampSlider = new Slider { Minimum = -12.0, Maximum = 12.0, StepFrequency = 0.5, Value = ViewModel?.EchoEqPreampDb ?? 0.0, Width = 140, VerticalAlignment = VerticalAlignment.Center };
		_preampSlider.ValueChanged += (s, e) =>
		{
			if (ViewModel != null)
			{
				ViewModel.EchoEqPreampDb = e.NewValue;
				ViewModel.Engine?.SetEchoEqPreamp(e.NewValue);
				if (_preampValueTb != null) _preampValueTb.Text = $"{e.NewValue:+0.0;-0.0;0.0} dB";
				if (_eqCurveControl != null) _eqCurveControl.PreampDb = e.NewValue;
			}
		};
		preampSp.Children.Add(_preampSlider);
		_preampValueTb = new TextBlock { Text = $"{_preampSlider.Value:+0.0;-0.0;0.0} dB", FontSize = 12, FontWeight = Microsoft.UI.Text.FontWeights.Bold, Width = 55, Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 15, 23, 42)), VerticalAlignment = VerticalAlignment.Center };
		preampSp.Children.Add(_preampValueTb);
		preampSp.Children.Add(new TextBlock { Text = "合成响应 -0.0 dB (自动补偿提升)", FontSize = 11, Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 100, 116, 139)), VerticalAlignment = VerticalAlignment.Center });

		Grid.SetColumn(preampSp, 1);
		row3.Children.Add(preampSp);
		stack.Children.Add(row3);

		card.Child = stack;
		return card;
	}

	private FrameworkElement CreateEqBottomToolbar()
	{
		var bar = new Grid { Margin = new Thickness(0, 4, 0, 0) };
		bar.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.0, GridUnitType.Star) }); // Left preset actions
		bar.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto }); // Right utilities

		var leftSp = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 10, VerticalAlignment = VerticalAlignment.Center };
		var btnSaveMod = new Button
		{
			Content = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 6, Children = { new FontIcon { Glyph = "\uE74E", FontSize = 13 }, new TextBlock { Text = "保存修改", FontWeight = Microsoft.UI.Text.FontWeights.SemiBold } } },
			Background = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 79, 70, 229)), // Indigo 600 (#4F46E5)
			Foreground = new SolidColorBrush(Colors.White),
			Padding = new Thickness(16, 6, 16, 6),
			CornerRadius = new CornerRadius(8)
		};
		leftSp.Children.Add(btnSaveMod);

		var tbPresetName = new TextBox
		{
			PlaceholderText = "保存为用户预设名称...",
			Width = 220,
			CornerRadius = new CornerRadius(8),
			Background = new SolidColorBrush(Windows.UI.Color.FromArgb(210, 255, 255, 255)),
			BorderBrush = new SolidColorBrush(Windows.UI.Color.FromArgb(180, 203, 213, 225)),
			Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 15, 23, 42))
		};
		leftSp.Children.Add(tbPresetName);

		var btnSavePreset = new Button
		{
			Content = "保存到预设",
			Padding = new Thickness(14, 6, 14, 6),
			CornerRadius = new CornerRadius(8),
			Background = new SolidColorBrush(Windows.UI.Color.FromArgb(210, 255, 255, 255)),
			BorderBrush = new SolidColorBrush(Windows.UI.Color.FromArgb(180, 203, 213, 225)),
			BorderThickness = new Thickness(1),
			Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 51, 65, 85))
		};
		leftSp.Children.Add(btnSavePreset);
		Grid.SetColumn(leftSp, 0);
		bar.Children.Add(leftSp);

		var rightSp = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8, VerticalAlignment = VerticalAlignment.Center };
		Button CreateToolBtn(string text) => new Button
		{
			Content = text,
			Padding = new Thickness(10, 6, 10, 6),
			CornerRadius = new CornerRadius(8),
			Background = new SolidColorBrush(Windows.UI.Color.FromArgb(210, 255, 255, 255)),
			BorderBrush = new SolidColorBrush(Windows.UI.Color.FromArgb(140, 226, 232, 240)),
			BorderThickness = new Thickness(1),
			Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 51, 65, 85))
		};

		rightSp.Children.Add(CreateToolBtn("🎧 输出配置"));
		rightSp.Children.Add(CreateToolBtn("❓"));
		rightSp.Children.Add(CreateToolBtn("🔗 分享曲线"));
		rightSp.Children.Add(CreateToolBtn("📥 导入分享码"));
		var btnResetAll = CreateToolBtn("🔄 重置曲线");
		btnResetAll.Click += (s, e) => ResetEqBandsToFlat();
		rightSp.Children.Add(btnResetAll);

		var btnImportApo = CreateToolBtn("📁 导入预设 / APO");
		btnImportApo.Click += OnAutoEqImportClick;
		rightSp.Children.Add(btnImportApo);

		rightSp.Children.Add(CreateToolBtn("更多工具 ▾"));
		Grid.SetColumn(rightSp, 1);
		bar.Children.Add(rightSp);

		return bar;
	}

	private void RefreshBandPills()
	{
		if (_eqBandPillsStack == null || ViewModel == null) return;
		_eqBandPillsStack.Children.Clear();

		var bands = ViewModel.EchoEqBands;
		int activeIdx = _eqCurveControl?.SelectedBandIndex ?? 0;

		Windows.UI.Color[] pillColors = {
			Windows.UI.Color.FromArgb(255, 14, 165, 233), // 01 sky
			Windows.UI.Color.FromArgb(255, 22, 163, 74),  // 02 green
			Windows.UI.Color.FromArgb(255, 147, 51, 234), // 03 purple
			Windows.UI.Color.FromArgb(255, 225, 29, 72),  // 04 rose
			Windows.UI.Color.FromArgb(255, 234, 88, 12),  // 05 orange
			Windows.UI.Color.FromArgb(255, 202, 138, 4),  // 06 amber
			Windows.UI.Color.FromArgb(255, 13, 148, 136), // 07 teal
			Windows.UI.Color.FromArgb(255, 37, 99, 235),  // 08 blue
			Windows.UI.Color.FromArgb(255, 192, 38, 211), // 09 fuchsia
			Windows.UI.Color.FromArgb(255, 220, 38, 38)   // 10 red
		};

		for (int i = 0; i < bands.Count; i++)
		{
			int bandIndex = i;
			var band = bands[i];
			bool isSelected = (bandIndex == activeIdx);
			var color = pillColors[bandIndex % pillColors.Length];

			var pill = new Button
			{
				Tag = bandIndex,
				Padding = new Thickness(8, 4, 10, 4),
				CornerRadius = new CornerRadius(16),
				Background = isSelected
					? new SolidColorBrush(Windows.UI.Color.FromArgb(45, color.R, color.G, color.B))
					: new SolidColorBrush(Windows.UI.Color.FromArgb(210, 255, 255, 255)),
				BorderBrush = isSelected
					? new SolidColorBrush(color)
					: new SolidColorBrush(Windows.UI.Color.FromArgb(140, 226, 232, 240)),
				BorderThickness = new Thickness(isSelected ? 1.5 : 1)
			};

			var pillSp = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 6, VerticalAlignment = VerticalAlignment.Center };
			// Number pill
			var numBorder = new Border
			{
				Background = new SolidColorBrush(color),
				CornerRadius = new CornerRadius(10),
				Width = 20,
				Height = 20,
				Child = new TextBlock
				{
					Text = $"{bandIndex + 1:D2}",
					FontSize = 10,
					FontWeight = Microsoft.UI.Text.FontWeights.Bold,
					Foreground = new SolidColorBrush(Colors.White),
					HorizontalAlignment = HorizontalAlignment.Center,
					VerticalAlignment = VerticalAlignment.Center
				}
			};
			pillSp.Children.Add(numBorder);

			string freqText = band.FrequencyHz >= 1000 ? $"{band.FrequencyHz / 1000.0:F1}k" : $"{band.FrequencyHz:F0}";
			string gainText = $"{band.GainDb:+0.0;-0.0;0.0}";
			pillSp.Children.Add(new TextBlock
			{
				Text = $"{freqText} Hz  {gainText} dB",
				FontSize = 11,
				FontWeight = isSelected ? Microsoft.UI.Text.FontWeights.Bold : Microsoft.UI.Text.FontWeights.Normal,
				Foreground = new SolidColorBrush(isSelected ? Windows.UI.Color.FromArgb(255, 15, 23, 42) : Windows.UI.Color.FromArgb(255, 51, 65, 85)),
				VerticalAlignment = VerticalAlignment.Center
			});

			pill.Content = pillSp;
			pill.Click += (s, e) => SelectEqBand(bandIndex);
			_eqBandPillsStack.Children.Add(pill);
		}

		// Add "+" button
		var btnAddBand = new Button
		{
			Content = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 4, Children = { new FontIcon { Glyph = "\uE710", FontSize = 11 }, new TextBlock { Text = "添加频段", FontSize = 11 } } },
			Padding = new Thickness(8, 4, 10, 4),
			CornerRadius = new CornerRadius(16),
			Background = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 241, 245, 249)),
			BorderBrush = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 226, 232, 240)),
			BorderThickness = new Thickness(1),
			Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 51, 65, 85))
		};
		btnAddBand.Click += (s, e) => AddNewEqBand();
		_eqBandPillsStack.Children.Add(btnAddBand);
	}

	private void SelectEqBand(int idx)
	{
		if (ViewModel == null || idx < 0 || idx >= ViewModel.EchoEqBands.Count) return;
		if (_eqCurveControl != null)
		{
			_eqCurveControl.SelectedBandIndex = idx;
		}

		UpdateSelectedBandInspector(idx);
		RefreshBandPills();
	}

	private void UpdateSelectedBandInspector(int idx)
	{
		if (ViewModel == null || idx < 0 || idx >= ViewModel.EchoEqBands.Count) return;
		var band = ViewModel.EchoEqBands[idx];

		if (_bandIndexBadge != null) _bandIndexBadge.Text = $"{idx + 1:D2}";
		if (_bandEnableCb != null) _bandEnableCb.IsChecked = band.Enabled;
		if (_filterTypeCombo != null) _filterTypeCombo.SelectedIndex = Math.Clamp((int)band.FilterType, 0, 6);

		// Frequency mapping (logarithmic 20 ~ 20000 Hz)
		if (_freqSlider != null)
		{
			double logMin = Math.Log10(20.0);
			double logMax = Math.Log10(20000.0);
			double logCur = Math.Log10(Math.Clamp(band.FrequencyHz, 20.0, 20000.0));
			double sliderVal = ((logCur - logMin) / (logMax - logMin)) * 1000.0;
			_freqSlider.Value = sliderVal;
		}
		if (_freqValueTb != null)
		{
			_freqValueTb.Text = band.FrequencyHz >= 1000 ? $"{band.FrequencyHz / 1000.0:F2} kHz" : $"{band.FrequencyHz:F0} Hz";
		}

		if (_gainSlider != null) _gainSlider.Value = band.GainDb;
		if (_gainValueTb != null) _gainValueTb.Text = $"{band.GainDb:+0.0;-0.0;0.0} dB";

		if (_qSlider != null) _qSlider.Value = band.Q;
		if (_qValueTb != null) _qValueTb.Text = $"{band.Q:F2}";
	}

	private void OnInspectorFreqSliderChanged(object sender, RangeBaseValueChangedEventArgs e)
	{
		if (ViewModel == null || _eqCurveControl == null) return;
		int idx = _eqCurveControl.SelectedBandIndex;
		if (idx < 0 || idx >= ViewModel.EchoEqBands.Count) return;

		double logMin = Math.Log10(20.0);
		double logMax = Math.Log10(20000.0);
		double freq = Math.Pow(10.0, logMin + (e.NewValue / 1000.0) * (logMax - logMin));
		freq = Math.Clamp(freq, 20.0, 20000.0);

		var band = ViewModel.EchoEqBands[idx];
		band.FrequencyHz = freq;
		band.UpdateCoefficients(48000.0);

		if (_freqValueTb != null)
		{
			_freqValueTb.Text = freq >= 1000 ? $"{freq / 1000.0:F2} kHz" : $"{freq:F0} Hz";
		}
		_eqCurveControl.SetBands(ViewModel.EchoEqBands, idx);
		ViewModel.Engine?.SetEchoEqBand(idx, band.FilterType, band.FrequencyHz, band.GainDb, band.Q, band.Enabled);
		RefreshBandPills();
	}

	private void OnInspectorGainSliderChanged(object sender, RangeBaseValueChangedEventArgs e)
	{
		if (ViewModel == null || _eqCurveControl == null) return;
		int idx = _eqCurveControl.SelectedBandIndex;
		if (idx < 0 || idx >= ViewModel.EchoEqBands.Count) return;

		var band = ViewModel.EchoEqBands[idx];
		band.GainDb = e.NewValue;
		band.UpdateCoefficients(48000.0);

		if (_gainValueTb != null) _gainValueTb.Text = $"{band.GainDb:+0.0;-0.0;0.0} dB";
		_eqCurveControl.SetBands(ViewModel.EchoEqBands, idx);
		ViewModel.Engine?.SetEchoEqBand(idx, band.FilterType, band.FrequencyHz, band.GainDb, band.Q, band.Enabled);
		RefreshBandPills();
	}

	private void OnInspectorQSliderChanged(object sender, RangeBaseValueChangedEventArgs e)
	{
		if (ViewModel == null || _eqCurveControl == null) return;
		int idx = _eqCurveControl.SelectedBandIndex;
		if (idx < 0 || idx >= ViewModel.EchoEqBands.Count) return;

		var band = ViewModel.EchoEqBands[idx];
		band.Q = e.NewValue;
		band.UpdateCoefficients(48000.0);

		if (_qValueTb != null) _qValueTb.Text = $"{band.Q:F2}";
		_eqCurveControl.SetBands(ViewModel.EchoEqBands, idx);
		ViewModel.Engine?.SetEchoEqBand(idx, band.FilterType, band.FrequencyHz, band.GainDb, band.Q, band.Enabled);
	}

	private void OnInspectorFilterTypeChanged(object sender, SelectionChangedEventArgs e)
	{
		if (ViewModel == null || _eqCurveControl == null || _filterTypeCombo == null) return;
		int idx = _eqCurveControl.SelectedBandIndex;
		if (idx < 0 || idx >= ViewModel.EchoEqBands.Count) return;

		int sel = _filterTypeCombo.SelectedIndex;
		if (sel >= 0 && sel <= 6)
		{
			var band = ViewModel.EchoEqBands[idx];
			band.FilterType = (EchoFilterType)sel;
			band.UpdateCoefficients(48000.0);
			_eqCurveControl.SetBands(ViewModel.EchoEqBands, idx);
			ViewModel.Engine?.SetEchoEqBand(idx, band.FilterType, band.FrequencyHz, band.GainDb, band.Q, band.Enabled);
		}
	}

	private void OnInspectorBandEnabledChanged(bool enabled)
	{
		if (ViewModel == null || _eqCurveControl == null) return;
		int idx = _eqCurveControl.SelectedBandIndex;
		if (idx < 0 || idx >= ViewModel.EchoEqBands.Count) return;

		var band = ViewModel.EchoEqBands[idx];
		band.Enabled = enabled;
		band.UpdateCoefficients(48000.0);
		_eqCurveControl.SetBands(ViewModel.EchoEqBands, idx);
		ViewModel.Engine?.SetEchoEqBand(idx, band.FilterType, band.FrequencyHz, band.GainDb, band.Q, band.Enabled);
		RefreshBandPills();
	}

	private void ResetCurrentBand()
	{
		if (ViewModel == null || _eqCurveControl == null) return;
		int idx = _eqCurveControl.SelectedBandIndex;
		if (idx < 0 || idx >= ViewModel.EchoEqBands.Count) return;

		var band = ViewModel.EchoEqBands[idx];
		band.GainDb = 0.0;
		band.Q = 1.414;
		band.Enabled = true;
		band.UpdateCoefficients(48000.0);
		_eqCurveControl.SetBands(ViewModel.EchoEqBands, idx);
		UpdateSelectedBandInspector(idx);
		ViewModel.Engine?.SetEchoEqBand(idx, band.FilterType, band.FrequencyHz, band.GainDb, band.Q, band.Enabled);
		RefreshBandPills();
	}

	private void ZeroCurrentBandGain()
	{
		if (ViewModel == null || _eqCurveControl == null) return;
		int idx = _eqCurveControl.SelectedBandIndex;
		if (idx < 0 || idx >= ViewModel.EchoEqBands.Count) return;

		var band = ViewModel.EchoEqBands[idx];
		band.GainDb = 0.0;
		band.UpdateCoefficients(48000.0);
		_eqCurveControl.SetBands(ViewModel.EchoEqBands, idx);
		UpdateSelectedBandInspector(idx);
		ViewModel.Engine?.SetEchoEqBand(idx, band.FilterType, band.FrequencyHz, band.GainDb, band.Q, band.Enabled);
		RefreshBandPills();
	}

	private void AddNewEqBand()
	{
		if (ViewModel == null || _eqCurveControl == null) return;
		double nextFreq = 1000.0;
		if (ViewModel.EchoEqBands.Count > 0)
		{
			var last = ViewModel.EchoEqBands[^1];
			nextFreq = Math.Clamp(last.FrequencyHz * 1.5, 20.0, 20000.0);
		}
		var newBand = new EchoBiquadFilter
		{
			FilterType = EchoFilterType.Peaking,
			FrequencyHz = nextFreq,
			GainDb = 0.0,
			Q = 1.414,
			Enabled = true
		};
		newBand.UpdateCoefficients(48000.0);
		ViewModel.EchoEqBands.Add(newBand);

		int newIdx = ViewModel.EchoEqBands.Count - 1;
		_eqCurveControl.SetBands(ViewModel.EchoEqBands, newIdx);
		SelectEqBand(newIdx);
	}

	private void ResetEqBandsToFlat()
	{
		if (ViewModel == null || _eqCurveControl == null) return;
		foreach (var b in ViewModel.EchoEqBands)
		{
			b.GainDb = 0.0;
			b.Enabled = true;
			b.UpdateCoefficients(48000.0);
		}
		if (ViewModel.Engine != null)
		{
			for (int i = 0; i < ViewModel.EchoEqBands.Count; i++)
			{
				var b = ViewModel.EchoEqBands[i];
				ViewModel.Engine.SetEchoEqBand(i, b.FilterType, b.FrequencyHz, b.GainDb, b.Q, b.Enabled);
			}
		}
		_eqCurveControl.SetBands(ViewModel.EchoEqBands, _eqCurveControl.SelectedBandIndex);
		UpdateSelectedBandInspector(_eqCurveControl.SelectedBandIndex);
		RefreshBandPills();
	}

	private void ApplyEqBuiltinPreset(int presetIdx)
	{
		if (ViewModel == null || _eqCurveControl == null) return;
		ViewModel.InitializeEchoEqBands();
		var bands = ViewModel.EchoEqBands;

		// Standard 10 band gains for presets
		double[] gains = presetIdx switch
		{
			1 => new double[] { 5.5, 4.8, 1.5, -0.5, 0.0, 0.5, 2.5, 4.0, 1.0, -2.0 }, // Moondrop / Harman Target
			2 => new double[] { 6.5, 4.0, 1.2, 0.0, 0.0, 0.0, 1.0, 1.5, -1.0, 2.0 },  // HD600 Diffuse Field
			3 => new double[] { 3.0, 2.0, -1.5, -2.5, 0.0, 1.0, 2.0, 3.0, 2.5, 1.5 }, // Sony WH-1000XM5
			4 => new double[] { 6.0, 5.0, 2.5, 0.0, 0.0, 0.5, 3.0, 4.5, 1.0, -1.5 },  // Harman 2019
			5 => new double[] { 6.0, 5.0, 3.5, 1.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0 },   // Sub-Bass Boost
			6 => new double[] { 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.5, 2.0, 3.5, 5.0 },   // High Air
			7 => new double[] { 0.0, 0.5, 1.5, 0.5, 0.0, 2.5, 3.0, 1.5, 0.0, 0.0 },   // Warm Vocal
			8 => new double[] { 2.0, 1.0, 0.0, -1.5, 0.0, 0.5, 2.0, 3.0, 4.0, 3.0 },  // Open Soundstage
			_ => new double[10] // Flat
		};

		for (int i = 0; i < Math.Min(bands.Count, gains.Length); i++)
		{
			bands[i].GainDb = gains[i];
			bands[i].Enabled = true;
			bands[i].UpdateCoefficients(48000.0);
			ViewModel.Engine?.SetEchoEqBand(i, bands[i].FilterType, bands[i].FrequencyHz, bands[i].GainDb, bands[i].Q, bands[i].Enabled);
		}

		_eqCurveControl.SetBands(ViewModel.EchoEqBands, _eqCurveControl.SelectedBandIndex);
		UpdateSelectedBandInspector(_eqCurveControl.SelectedBandIndex);
		RefreshBandPills();
	}

	private void OnCurveBandMoved(int idx, double freq, double gain)
	{
		if (ViewModel == null || idx < 0 || idx >= ViewModel.EchoEqBands.Count) return;
		var band = ViewModel.EchoEqBands[idx];
		band.FrequencyHz = freq;
		band.GainDb = gain;
		band.UpdateCoefficients(48000.0);

		UpdateSelectedBandInspector(idx);
		ViewModel.Engine?.SetEchoEqBand(idx, band.FilterType, band.FrequencyHz, band.GainDb, band.Q, band.Enabled);
		RefreshBandPills();
	}

	public void SelectDspCategory(string category)
	{
		_currentDspCategory = category;

		// Highlight matching button in sidebar
		foreach (var btn in _sidebarNavButtons)
		{
			if (btn.Tag is string key)
			{
				bool isSelected = (key == category);
				btn.Background = isSelected
					? new SolidColorBrush(Windows.UI.Color.FromArgb(255, 238, 242, 255)) // Indigo 50 (#EEF2FF)
					: new SolidColorBrush(Colors.Transparent);
				btn.BorderBrush = isSelected
					? new SolidColorBrush(Windows.UI.Color.FromArgb(255, 99, 102, 241)) // Indigo 500 (#6366F1)
					: new SolidColorBrush(Colors.Transparent);
				btn.BorderThickness = new Thickness(isSelected ? 1 : 0);

				if (btn.Content is Grid g)
				{
					if (g.Children.Count > 0 && g.Children[0] is FontIcon ic)
					{
						ic.Foreground = isSelected
							? new SolidColorBrush(Windows.UI.Color.FromArgb(255, 79, 70, 229)) // Indigo 600
							: new SolidColorBrush(Windows.UI.Color.FromArgb(255, 100, 116, 139)); // Slate 500
					}
					if (g.Children.Count > 1 && g.Children[1] is StackPanel sp)
					{
						if (sp.Children.Count > 0 && sp.Children[0] is TextBlock t1)
						{
							t1.Foreground = isSelected
								? new SolidColorBrush(Windows.UI.Color.FromArgb(255, 79, 70, 229))
								: new SolidColorBrush(Windows.UI.Color.FromArgb(255, 30, 41, 59)); // Slate 800
						}
						if (sp.Children.Count > 1 && sp.Children[1] is TextBlock t2)
						{
							t2.Foreground = isSelected
								? new SolidColorBrush(Windows.UI.Color.FromArgb(255, 99, 102, 241))
								: new SolidColorBrush(Windows.UI.Color.FromArgb(255, 100, 116, 139));
						}
					}
				}
			}
		}

		if (_workbenchHost == null) return;
		_workbenchHost.Children.Clear();

		FrameworkElement activeView = category switch
		{
			"param_eq" => _parametricEqWorkbench ?? CreateParametricEqWorkbench(),
			"headphone" => _headphoneCorrectionWorkbench ?? CreateHeadphoneCorrectionWorkbench(),
			"level_gain" => _volumeEqualizationWorkbench ?? CreateVolumeEqualizationWorkbench(),
			"headroom" => _headroomWorkbench ?? CreateHeadroomWorkbench(),
			"channel" => _channelToolWorkbench ?? CreateChannelToolWorkbench(),
			"room" or "convolution" => _roomCorrectionWorkbench ?? CreateRoomCorrectionWorkbench(),
			"crossfeed" or "spatial" => CreateWrappedCard("交叉馈送与空间声场", "\uE895", _spatialCrossfeedPanel ?? CreateSpatialCrossfeedPanel()),
			"compressor" or "dynamics" => _compressorWorkbench ?? CreateCompressorWorkbench(),
			"multiband" => _multibandCompressorWorkbench ?? CreateMultibandCompressorWorkbench(),
			"resampler" or "src" => _srcUpsamplingWorkbench ?? CreateSrcUpsamplingWorkbench(),
			"dsd" or "sdm" => _sdmDsdWorkbench ?? CreateSdmDsdWorkbench(),
			"vst3" => CreateWrappedCard("VST3 扩展机架", "\uE8B8", Vst3Panel),
			_ => _parametricEqWorkbench ?? CreateParametricEqWorkbench()
		};

		Grid.SetRow(activeView, 0);
		Grid.SetColumn(activeView, 0);
		_workbenchHost.Children.Add(activeView);
	}

	public void SetDspViewCategory(string category)
	{
		SelectDspCategory(category);
	}

	private FrameworkElement CreateWrappedCard(string title, string glyph, Border? innerCard)
	{
		if (innerCard == null) return new Grid();
		if (innerCard.Parent is Border parentBorder)
		{
			parentBorder.Child = null;
		}
		else if (innerCard.Parent is Panel parentPanel)
		{
			parentPanel.Children.Remove(innerCard);
		}
		innerCard.HorizontalAlignment = HorizontalAlignment.Stretch;
		innerCard.VerticalAlignment = VerticalAlignment.Stretch;
		innerCard.Width = double.NaN;
		innerCard.Height = double.NaN;
		innerCard.Background = CreateAcrylicCardBrush(0.88, 235);
		innerCard.BorderBrush = new SolidColorBrush(Windows.UI.Color.FromArgb(140, 226, 232, 240));
		innerCard.BorderThickness = new Thickness(1);
		innerCard.CornerRadius = new CornerRadius(12);

		var card = new Border
		{
			Background = CreateWhiteAcrylicBrush(0.72, 195),
			Padding = new Thickness(24, 20, 24, 20),
			Child = innerCard
		};
		return card;
	}

	private void OptimizeVst3PanelLayout()
	{
		if (Vst3Panel == null) return;
		try
		{
			Vst3Panel.MinWidth = 1000;
			if (Vst3FoldersList != null)
			{
				Vst3FoldersList.MinWidth = 340;
			}
			if (Vst3PluginsList != null)
			{
				Vst3PluginsList.MinWidth = 650;
				Vst3PluginsList.Padding = new Thickness(0, 0, 16, 0);
				Vst3PluginsList.ContainerContentChanging -= OnVst3PluginsContainerContentChanging;
				Vst3PluginsList.ContainerContentChanging += OnVst3PluginsContainerContentChanging;
			}
			AdjustVst3VisualTree(Vst3Panel);
		}
		catch {}
	}

	private void OnVst3PluginsContainerContentChanging(ListViewBase sender, ContainerContentChangingEventArgs args)
	{
		if (args.ItemContainer != null)
		{
			FormatVst3PluginItem(args.ItemContainer);
		}
	}

	private static void FormatVst3PluginItem(DependencyObject root)
	{
		int count = VisualTreeHelper.GetChildrenCount(root);
		for (int i = 0; i < count; i++)
		{
			var child = VisualTreeHelper.GetChild(root, i);
			if (child is TextBlock tb)
			{
				if (tb.MaxWidth < 400 && tb.MaxWidth > 0)
				{
					tb.MaxWidth = 450;
				}
				tb.TextTrimming = TextTrimming.CharacterEllipsis;
			}
			else if (child is Button btn)
			{
				btn.Height = 28;
				btn.Padding = new Thickness(8, 2, 8, 2);
				btn.Margin = new Thickness(6, 0, 8, 0);
			}
			else if (child is ToggleButton tog)
			{
				tog.Height = 28;
				tog.Padding = new Thickness(8, 2, 8, 2);
			}
			FormatVst3PluginItem(child);
		}
	}

	private static void AdjustVst3VisualTree(DependencyObject node)
	{
		if (node is Grid grid)
		{
			if (grid.ColumnDefinitions.Count >= 2)
			{
				grid.ColumnDefinitions[0].Width = new GridLength(380);
				grid.ColumnDefinitions[1].Width = new GridLength(1.0, GridUnitType.Star);
			}
		}
		else if (node is StackPanel sp && sp.Orientation == Orientation.Horizontal)
		{
			sp.Spacing = 8;
		}

		int c = VisualTreeHelper.GetChildrenCount(node);
		for (int i = 0; i < c; i++)
		{
			AdjustVst3VisualTree(VisualTreeHelper.GetChild(node, i));
		}
	}

	private void ScrollToPanel(Border panel)
	{
		try
		{
			double left = Canvas.GetLeft(panel);
			double top = Canvas.GetTop(panel);
			if (PanelCanvas?.Parent is ScrollViewer sv)
			{
				sv.ChangeView(Math.Max(0, left - 40), Math.Max(0, top - 40), null, disableAnimation: false);
			}
		}
		catch {}
	}

	private void StartDspMeterTimer()
	{
		if (_dspMeterTimer == null)
		{
			_dspMeterTimer = DispatcherQueue.GetForCurrentThread().CreateTimer();
			_dspMeterTimer.Interval = TimeSpan.FromMilliseconds(50);
			_dspMeterTimer.Tick += OnDspMeterTick;
		}
		if (!_dspMeterTimer.IsRunning)
		{
			_dspMeterTimer.Start();
		}
	}

	private void StopDspMeterTimer()
	{
		if (_dspMeterTimer != null && _dspMeterTimer.IsRunning)
		{
			_dspMeterTimer.Stop();
		}
	}

	private void OnDspMeterTick(DispatcherQueueTimer sender, object args)
	{
		try
		{
			if (ViewModel?.Engine?.DspChain != null)
			{
				var comp = ViewModel.Engine.DspChain.Compressor;
				if (comp != null && _grProgressBar != null && _grValueText != null)
				{
					float gr = comp.GainReductionDb;
					double absGr = Math.Abs(Math.Clamp((double)gr, -24.0, 0.0));
					_grProgressBar.Value = absGr;
					_grValueText.Text = $"{gr:F1} dB";
				}
			}
		}
		catch {}
	}

	private async void OnAutoEqImportClick(object sender, RoutedEventArgs e)
	{
		try
		{
			var picker = new FileOpenPicker
			{
				FileTypeFilter = { ".txt", ".csv" },
				SuggestedStartLocation = PickerLocationId.ComputerFolder
			};
			nint windowHandle = WindowNative.GetWindowHandle(WindowManager.CurrentWindow);
			InitializeWithWindow.Initialize(picker, windowHandle);
			var file = await picker.PickSingleFileAsync();
			if (file != null)
			{
				string text = await FileIO.ReadTextAsync(file);
				bool ok = ViewModel.ImportAutoEqText(text);
				if (ok)
				{
					DspEq.Gains = ViewModel.EqGains;
					DrawEqCurve(ViewModel.EqGains);
					await new ContentDialog
					{
						Title = "AutoEq 耳机校准曲线载入成功",
						Content = $"已从 {file.Name} 成功导入并解析 ParametricEQ 参数，10段专业均衡器已完成耳机频响修正！",
						CloseButtonText = "好",
						XamlRoot = XamlRoot
					}.ShowAsync();
				}
				else
				{
					await new ContentDialog
					{
						Title = "导入未完全匹配",
						Content = "文件未包含标准 AutoEq 参数，请确认是否为包含 Preamp / Filter 的 ParametricEQ 文本。",
						CloseButtonText = "知道了",
						XamlRoot = XamlRoot
					}.ShowAsync();
				}
			}
		}
		catch (Exception ex)
		{
			Debug.WriteLine("AutoEq import error: " + ex.Message);
		}
	}

	private async void OnConvolutionImportClick(object sender, RoutedEventArgs e)
	{
		try
		{
			var picker = new FileOpenPicker
			{
				FileTypeFilter = { ".wav" },
				SuggestedStartLocation = PickerLocationId.ComputerFolder
			};
			nint windowHandle = WindowNative.GetWindowHandle(WindowManager.CurrentWindow);
			InitializeWithWindow.Initialize(picker, windowHandle);
			var file = await picker.PickSingleFileAsync();
			if (file != null)
			{
				bool ok = ViewModel.LoadConvolutionIr(file.Path);
				if (ok && _irInfoText != null)
				{
					_irInfoText.Text = $"已载入 IR: {file.Name}";
					_irInfoText.Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 0, 229, 255));
				}
			}
		}
		catch (Exception ex)
		{
			Debug.WriteLine("Convolution import error: " + ex.Message);
		}
	}

	[GeneratedCode("Microsoft.UI.Xaml.Markup.Compiler", " 3.0.0.2409")]
	[DebuggerNonUserCode]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("ms-appx:///FurinaPlayer.UI/Pages/DspPage.xaml");
			Application.LoadComponent(this, resourceLocator, ComponentResourceLocation.Nested);
		}
	}

	[GeneratedCode("Microsoft.UI.Xaml.Markup.Compiler", " 3.0.0.2409")]
	[DebuggerNonUserCode]
	public void Connect(int connectionId, object target)
	{
		switch (connectionId)
		{
		case 5:
			RenderRemainingText = target.As<TextBlock>();
			break;
		case 6:
			PanelCanvas = target.As<Canvas>();
			break;
		case 7:
			EqPanel = target.As<Border>();
			break;
		case 8:
			EffectsPanel = target.As<Border>();
			break;
		case 9:
			TimbrePanel = target.As<Border>();
			break;
		case 10:
			Vst3Panel = target.As<Border>();
			break;
		case 11:
			Vst3Header = target.As<Grid>();
			break;
		case 12:
			Vst3ResizeHandle = target.As<Grid>();
			break;
		case 13:
			Vst3PluginsList = target.As<ListView>();
			break;
		case 16:
			target.As<ToggleSwitch>().Toggled += OnVst3Toggled;
			break;
		case 17:
			target.As<ToggleButton>().Click += OnVst3Toggled;
			break;
		case 18:
			target.As<Button>().Click += OnVst3ParamsClick;
			break;
		case 19:
			Vst3FoldersList = target.As<ListView>();
			break;
		case 20:
			target.As<Button>().Click += OnAddVst3FolderClick;
			break;
		case 21:
			target.As<Button>().Click += OnRemoveVst3FolderClick;
			break;
		case 22:
			target.As<Button>().Click += OnScanVst3Click;
			break;
		case 23:
			target.As<Button>().Click += OnPanelEditClick;
			break;
		case 24:
			TimbreHeader = target.As<Grid>();
			break;
		case 25:
			TimbreResizeHandle = target.As<Grid>();
			break;
		case 26:
			target.As<Button>().Click += OnPanelEditClick;
			break;
		case 27:
			EffectsHeader = target.As<Grid>();
			break;
		case 28:
			EffectsResizeHandle = target.As<Grid>();
			break;
		case 29:
			ReverbPresetCombo = target.As<ComboBox>();
			ReverbPresetCombo.SelectionChanged += OnReverbPresetChanged;
			break;
		case 30:
			target.As<Button>().Click += OnPanelEditClick;
			break;
		case 31:
			EqHeader = target.As<Grid>();
			break;
		case 32:
			EqResizeHandle = target.As<Grid>();
			break;
		case 33:
			DspEq = target.As<EqualizerControl>();
			DspEq.GainsChanged += OnDspEqGainsChanged;
			break;
		case 34:
			EqPresetCombo = target.As<ComboBox>();
			EqPresetCombo.SelectionChanged += OnEqPresetChanged;
			break;
		case 35:
			target.As<Button>().Click += OnResetClick;
			break;
		case 36:
			EqCurveCanvas = target.As<Canvas>();
			EqCurveCanvas.SizeChanged += OnEqCanvasSizeChanged;
			break;
		case 37:
			target.As<Button>().Click += OnPanelEditClick;
			break;
		case 38:
			target.As<Button>().Click += OnResetLayoutClick;
			break;
		}
		_contentLoaded = true;
	}

	[GeneratedCode("Microsoft.UI.Xaml.Markup.Compiler", " 3.0.0.2409")]
	[DebuggerNonUserCode]
	public IComponentConnector GetBindingConnector(int connectionId, object target)
	{
		IComponentConnector result = null;
		if (connectionId == 1)
		{
			Page page = (Page)target;
			DspPage_obj1_Bindings dspPage_obj1_Bindings = new DspPage_obj1_Bindings();
			result = dspPage_obj1_Bindings;
			dspPage_obj1_Bindings.SetDataRoot(this);
			Bindings = dspPage_obj1_Bindings;
			page.Loading += dspPage_obj1_Bindings.Loading;
		}
		return result;
	}
}
