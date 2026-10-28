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
[WinRTExposedType(typeof(DspPageWinRTTypeDetails))]
public sealed class DspPage : Page, IComponentConnector
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
	private string _currentDspCategory = "rack";
	private readonly List<Button> _categoryTabButtons = new();

	[GeneratedCode("Microsoft.UI.Xaml.Markup.Compiler", " 3.0.0.2409")]
	private IDspPage_Bindings Bindings;

	public DspViewModel ViewModel { get; set; }

	public DspPage()
	{
		InitializeComponent();
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
		if (_currentDspCategory != "free")
		{
			SetDspViewCategory(_currentDspCategory);
		}
	}

	private void RestoreLayouts()
	{
		Dictionary<string, double[]> layouts = ViewModel.LoadPanelLayouts();
		RestorePanel(EqPanel, "dsp.eq", layouts, 20.0, 16.0, 600.0, 380.0);
		RestorePanel(EffectsPanel, "dsp.effects", layouts, 640.0, 16.0, 560.0, 380.0);
		RestorePanel(TimbrePanel, "dsp.timbre", layouts, 1820.0, 416.0, 560.0, 350.0);
		RestorePanel(Vst3Panel, "dsp.vst3", layouts, 20.0, 416.0, 600.0, 360.0);
		if (_compressorPanel != null) RestorePanel(_compressorPanel, "dsp.compressor", layouts, 20.0, 796.0, 600.0, 370.0);
		if (_spatialCrossfeedPanel != null) RestorePanel(_spatialCrossfeedPanel, "dsp.spatial", layouts, 640.0, 416.0, 560.0, 360.0);
		if (_convolutionPanel != null) RestorePanel(_convolutionPanel, "dsp.convolution", layouts, 1220.0, 16.0, 580.0, 380.0);
		if (_advancedPanel != null) RestorePanel(_advancedPanel, "dsp.advanced", layouts, 1220.0, 416.0, 580.0, 360.0);
		if (_replayGainHeadroomPanel != null) RestorePanel(_replayGainHeadroomPanel, "dsp.replaygain", layouts, 1820.0, 16.0, 560.0, 380.0);
		if (_rackOrderPanel != null) RestorePanel(_rackOrderPanel, "dsp.rack", layouts, 640.0, 796.0, 1160.0, 370.0);
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
		ResetPanelLayout(EqPanel, 20.0, 16.0, 600.0, 370.0);
		ResetPanelLayout(EffectsPanel, 640.0, 16.0, 560.0, 370.0);
		ResetPanelLayout(TimbrePanel, 1820.0, 416.0, 560.0, 350.0);
		ResetPanelLayout(Vst3Panel, 20.0, 416.0, 600.0, 360.0);
		if (_compressorPanel != null) ResetPanelLayout(_compressorPanel, 20.0, 796.0, 600.0, 370.0);
		if (_spatialCrossfeedPanel != null) ResetPanelLayout(_spatialCrossfeedPanel, 640.0, 416.0, 560.0, 360.0);
		if (_convolutionPanel != null) ResetPanelLayout(_convolutionPanel, 1220.0, 16.0, 580.0, 380.0);
		if (_advancedPanel != null) ResetPanelLayout(_advancedPanel, 1220.0, 416.0, 580.0, 360.0);
		if (_replayGainHeadroomPanel != null) ResetPanelLayout(_replayGainHeadroomPanel, 1820.0, 16.0, 560.0, 380.0);
		if (_rackOrderPanel != null) ResetPanelLayout(_rackOrderPanel, 640.0, 796.0, 1160.0, 370.0);
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
				SetupQuickNavPills(rootGrid);
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

		headerGrid.Children.Add(new FontIcon
		{
			Glyph = glyph,
			FontSize = 15,
			Opacity = 0.85,
			VerticalAlignment = VerticalAlignment.Center,
			Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 0, 229, 255))
		});

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

		return CreatePanelCard("母带动态压缩器 (Echo Compressor)", "\uE9D9", sp, out var h, out var r, 600, 370);
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
			Content = new StackPanel
			{
				Orientation = Orientation.Horizontal,
				Spacing = 6,
				Children =
				{
					new FontIcon { Glyph = "\uE8B7", FontSize = 13 },
					new TextBlock { Text = "载入脉冲响应 IR (.wav)", FontSize = 12 }
				}
			},
			Padding = new Thickness(12, 6, 12, 6)
		};
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

		var modules = new (string Name, string Glyph, string Desc)[]
		{
			("余量管理 (Headroom)", "\uE994", "数字余量下压防削波"),
			("响度均衡 (ReplayGain)", "\uE895", "曲目/专辑感知响度自动对齐"),
			("10段图形EQ & AutoEq", "\uE995", "多段高精度参数化均衡校准"),
			("母带动态压缩器 (Compressor)", "\uE9D9", "瞬态与动态范围塑造"),
			("双耳互馈 (Crossfeed)", "\uE950", "消除耳机听觉疲劳与头中效应"),
			("声场宽度与声道矩阵", "\uE74C", "0~200% 声场宽度与极性控制"),
			("FIR 卷积房间校正", "\uE8B7", "脉冲响应逆滤波器修正"),
			("多相升频重采样 (Resampler)", "\uE774", "最高 384kHz 高解析升频"),
			("PCM 转 DSD (SDM 调制器)", "\uE8B8", "DSD64/128/256 高阶噪声整形"),
			("VST3 插件宿主", "\uE71D", "外接第三方专业音频效果器")
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

			var chk = new CheckBox { IsChecked = true, Content = $"{i + 1}. {m.Name}", FontSize = 12 };
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
					Content = new StackPanel
					{
						Orientation = Orientation.Horizontal,
						Spacing = 6,
						Children =
						{
							new FontIcon { Glyph = "\uE896", FontSize = 12, Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 0, 229, 255)) },
							new TextBlock { Text = "导入 AutoEq 耳机预设", FontSize = 12 }
						}
					},
					Padding = new Thickness(10, 4, 10, 4),
					CornerRadius = new CornerRadius(6)
				};
				btnAutoEq.Click += OnAutoEqImportClick;
				presetStack.Children.Add(btnAutoEq);
			}
		}
		catch (Exception ex)
		{
			Debug.WriteLine($"SetupAutoEqButton failed: {ex.Message}");
		}
	}

	private void SetupQuickNavPills(Grid rootGrid)
	{
		try
		{
			_categoryTabButtons.Clear();
			var pillsStack = new StackPanel
			{
				Orientation = Orientation.Horizontal,
				Spacing = 8,
				Margin = new Thickness(0, 4, 0, 4)
			};

			var items = new (string Key, string Name, string Glyph)[]
			{
				("rack", "全景机架", "\uE8B8"),
				("eq", "均衡与校正", "\uE995"),
				("spatial", "空间声场", "\uE950"),
				("dynamics", "动态母带", "\uE9D9"),
				("hires", "高解析DSD", "\uE774"),
				("vst3", "扩展与VST3", "\uE71D"),
				("free", "自由画布", "\uE74C")
			};

			foreach (var item in items)
			{
				var btn = new Button
				{
					Tag = item.Key,
					Padding = new Thickness(12, 5, 12, 5),
					CornerRadius = new CornerRadius(14),
					BorderThickness = new Thickness(1),
					Content = new StackPanel
					{
						Orientation = Orientation.Horizontal,
						Spacing = 6,
						Children =
						{
							new FontIcon { Glyph = item.Glyph, FontSize = 12 },
							new TextBlock { Text = item.Name, FontSize = 12 }
						}
					}
				};
				btn.Click += (s, e) =>
				{
					if (s is Button b && b.Tag is string cat)
					{
						SetDspViewCategory(cat);
					}
				};
				_categoryTabButtons.Add(btn);
				pillsStack.Children.Add(btn);
			}

			if (rootGrid.Children.Count > 0 && rootGrid.Children[0] is FrameworkElement topHeader)
			{
				int idx = rootGrid.Children.IndexOf(topHeader);
				rootGrid.Children.RemoveAt(idx);
				var headerContainer = new StackPanel { Orientation = Orientation.Vertical, Spacing = 8 };
				headerContainer.Children.Add(topHeader);
				headerContainer.Children.Add(pillsStack);
				Grid.SetRow(headerContainer, 0);
				rootGrid.Children.Insert(idx, headerContainer);
			}

			SetDspViewCategory("rack");
		}
		catch (Exception ex)
		{
			Debug.WriteLine($"SetupQuickNavPills failed: {ex.Message}");
		}
	}

	private IEnumerable<Border?> GetAllPanels()
	{
		yield return _rackOrderPanel;
		yield return EqPanel;
		yield return _convolutionPanel;
		yield return _spatialCrossfeedPanel;
		yield return _compressorPanel;
		yield return _replayGainHeadroomPanel;
		yield return _advancedPanel;
		yield return Vst3Panel;
		yield return EffectsPanel;
		yield return TimbrePanel;
	}

	private void SetDspViewCategory(string category)
	{
		_currentDspCategory = category;
		UpdateCategoryTabButtons();

		if (category == "free")
		{
			RestoreLayouts();
			foreach (var p in GetAllPanels())
			{
				if (p != null) p.Visibility = Visibility.Visible;
			}
			return;
		}

		foreach (var p in GetAllPanels())
		{
			if (p != null) p.Visibility = Visibility.Collapsed;
		}

		switch (category)
		{
			case "rack":
				ShowAndArrangePanel(_rackOrderPanel, 20, 20, 1160, 480);
				break;
			case "eq":
				ShowAndArrangePanel(EqPanel, 20, 20, 570, 480);
				ShowAndArrangePanel(_convolutionPanel, 610, 20, 570, 480);
				break;
			case "spatial":
				ShowAndArrangePanel(_spatialCrossfeedPanel, 20, 20, 1160, 480);
				break;
			case "dynamics":
				ShowAndArrangePanel(_compressorPanel, 20, 20, 570, 480);
				ShowAndArrangePanel(_replayGainHeadroomPanel, 610, 20, 570, 480);
				break;
			case "hires":
				ShowAndArrangePanel(_advancedPanel, 20, 20, 1160, 480);
				break;
			case "vst3":
				ShowAndArrangePanel(Vst3Panel, 20, 20, 570, 480);
				ShowAndArrangePanel(EffectsPanel, 610, 20, 570, 480);
				break;
		}

		if (PanelCanvas?.Parent is ScrollViewer sv)
		{
			sv.ChangeView(0, 0, null, disableAnimation: false);
		}
	}

	private static void ShowAndArrangePanel(Border? panel, double left, double top, double width, double height)
	{
		if (panel == null) return;
		panel.Visibility = Visibility.Visible;
		Canvas.SetLeft(panel, left);
		Canvas.SetTop(panel, top);
		panel.Width = width;
		panel.Height = height;
	}

	private void UpdateCategoryTabButtons()
	{
		foreach (var btn in _categoryTabButtons)
		{
			if (btn.Tag is string key)
			{
				bool isSelected = key == _currentDspCategory;
				btn.Background = isSelected
					? new SolidColorBrush(Windows.UI.Color.FromArgb(240, 0, 180, 255))
					: new SolidColorBrush(Windows.UI.Color.FromArgb(160, 24, 28, 36));
				btn.BorderBrush = isSelected
					? new SolidColorBrush(Windows.UI.Color.FromArgb(255, 0, 229, 255))
					: new SolidColorBrush(Windows.UI.Color.FromArgb(60, 255, 255, 255));
				if (btn.Content is StackPanel sp && sp.Children.Count >= 2)
				{
					if (sp.Children[0] is FontIcon icon)
					{
						icon.Foreground = isSelected
							? new SolidColorBrush(Windows.UI.Color.FromArgb(255, 10, 15, 25))
							: new SolidColorBrush(Windows.UI.Color.FromArgb(200, 0, 229, 255));
					}
					if (sp.Children[1] is TextBlock tb)
					{
						tb.Foreground = isSelected
							? new SolidColorBrush(Windows.UI.Color.FromArgb(255, 10, 15, 25))
							: new SolidColorBrush(Windows.UI.Color.FromArgb(220, 255, 255, 255));
						tb.FontWeight = isSelected ? Microsoft.UI.Text.FontWeights.SemiBold : Microsoft.UI.Text.FontWeights.Normal;
					}
				}
			}
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
