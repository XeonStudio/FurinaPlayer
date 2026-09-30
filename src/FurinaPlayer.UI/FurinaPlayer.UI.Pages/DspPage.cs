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

	[GeneratedCode("Microsoft.UI.Xaml.Markup.Compiler", " 3.0.0.2409")]
	private IDspPage_Bindings Bindings;

	public DspViewModel ViewModel { get; set; }

	public DspPage()
	{
		InitializeComponent();
		_layout = new PanelLayoutController(PanelCanvas, 420.0, 240.0);
		_layout.Register(EqPanel, EqHeader);
		_layout.Register(EffectsPanel, EffectsHeader);
		_layout.Register(TimbrePanel, TimbreHeader);
		_layout.Register(Vst3Panel, Vst3Header);
		_layout.RegisterResizeHandle(EqResizeHandle, EqPanel);
		_layout.RegisterResizeHandle(EffectsResizeHandle, EffectsPanel);
		_layout.RegisterResizeHandle(TimbreResizeHandle, TimbrePanel);
		_layout.RegisterResizeHandle(Vst3ResizeHandle, Vst3Panel);
		PanelLayoutController layout = _layout;
		layout.LayoutChanged = (Action)Delegate.Combine(layout.LayoutChanged, new Action(SaveLayouts));
		Unloaded += (object _, RoutedEventArgs _) =>
		{
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
	}

	private void RestoreLayouts()
	{
		Dictionary<string, double[]> layouts = ViewModel.LoadPanelLayouts();
		RestorePanel(EqPanel, "dsp.eq", layouts, 20.0, 16.0, 660.0, 460.0);
		RestorePanel(EffectsPanel, "dsp.effects", layouts, 700.0, 16.0, 640.0, 400.0);
		RestorePanel(TimbrePanel, "dsp.timbre", layouts, 700.0, 432.0, 640.0, 320.0);
		RestorePanel(Vst3Panel, "dsp.vst3", layouts, 20.0, 492.0, 1320.0, 252.0);
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
				["dsp.eq"] = PanelState(EqPanel),
				["dsp.effects"] = PanelState(EffectsPanel),
				["dsp.timbre"] = PanelState(TimbrePanel),
				["dsp.vst3"] = PanelState(Vst3Panel)
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
		ResetPanelLayout(EqPanel, 20.0, 16.0, 660.0, 460.0);
		ResetPanelLayout(EffectsPanel, 700.0, 16.0, 640.0, 400.0);
		ResetPanelLayout(TimbrePanel, 700.0, 432.0, 640.0, 320.0);
		ResetPanelLayout(Vst3Panel, 20.0, 492.0, 1320.0, 252.0);
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
			_vst3EditorWindow = Vst3EditorWindow.Open(tag.Path, sampleRate, 2, out string error);
			if (_vst3EditorWindow == null)
			{
				ViewModel.Vst3StatusText = "打开插件界面失败：" + error;
				return;
			}
			_vst3EditorWindow.Closed += OnVst3EditorClosed;
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
			ViewModel.Vst3StatusText = "已打开插件原生界面：" + tag.Name + "，实时独占渲染与播放当前歌曲";
		}
		catch (Exception ex)
		{
			ViewModel.Vst3StatusText = "打开插件界面异常：" + ex.Message;
		}
	}

	private void OnVst3EditorClosed()
	{
		Vst3EditorWindow w = _vst3EditorWindow;
		_vst3EditorWindow = null;
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
