using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
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
using SkiaSharp;
using SonicWave.Audio.Dsp;
using SonicWave.Core.Models;
using SonicWave.Core.Services;
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
	private ParametricEqWorkbenchControl? _eqWorkbenchControl;
	private DispatcherQueueTimer? _eqSpectrumTimer;
	private DispatcherQueueTimer? _mixAssistantLearnTimer;
	private bool _isAnalyzingTrackForMixAssistant;
	private Grid? _workbenchHost;
	private Border? _sidebarBorder;
	private TextBlock? _sidebarDspBadgeTb;
	private TextBlock? _sidebarBrandTitleTb;
	private TextBlock? _sidebarSubtitleTb;
	private readonly List<Button> _sidebarNavButtons = new();
	private readonly List<TextBlock> _sidebarSectionHeaders = new();
	private TextBlock? _sidebarAdvTitle;
	private FontIcon? _sidebarAdvChevron;
	private Border? _headroomWorkbench;
	private TextBlock? _headroomNavSubTb;
	private static string? _cachedWallpaperPath;
	private static DateTime _cachedWallpaperWriteUtc;
	private static bool _cachedWallpaperIsDark;

	// Headroom Real-time Budget & Monitoring Elements
	private TextBlock? _headroomStreamInfoTb;
	private Border? _headroomInMeterFill;
	private TextBlock? _headroomInMeterValTb;
	private Border? _headroomOutMeterFill;
	private TextBlock? _headroomOutMeterValTb;
	private Border? _headroomTpMeterFill;
	private TextBlock? _headroomTpMeterValTb;
	private TextBlock? _headroomProtectStatusTb;
	private TextBlock? _headroomClipCountTb;
	private TextBlock? _headroomLastClipTb;
	private TextBlock? _headroomTruePeakTb;
	private TextBlock? _headroomMarginTb;
	private int _headroomClipCount = 0;
	private DateTime? _headroomLastClipTime = null;
	private double _headroomAvgMarginDb = 0.0;
	private ToggleSwitch? _headroomPowerSwitch;
	private TextBlock? _headroomGainBigTb;
	private TextBlock? _headroomAmpRatioTb;
	private Slider? _headroomGainSlider;
	private CheckBox? _headroomDynamicLoudnessCb;
	private Button? _headroomAuditionBtn;
	private readonly List<Button> _headroomPresetButtons = new();

	public Brush CreateWhiteAcrylicBrush(double tintOpacity = 0.75, byte fallbackAlpha = 210)
	{
		return LiquidGlassEngine.CreateSurfaceBrush(
			LiquidGlassVariant.Clear,
			isDark: IsDspDark,
			opacityScale: Math.Clamp(tintOpacity / 0.85, 0.55, 1.0));
	}

	public Brush CreateAcrylicCardBrush(double tintOpacity = 0.88, byte fallbackAlpha = 235)
	{
		return LiquidGlassEngine.CreateSurfaceBrush(
			LiquidGlassVariant.Regular,
			isDark: IsDspDark,
			opacityScale: Math.Clamp(tintOpacity / 0.90, 0.72, 1.05));
	}

	public Brush CreateLiquidGlassRimBrush(bool isHovered = false, bool isSelected = false)
	{
		return LiquidGlassEngine.CreateSpecularRimBrush(
			LiquidGlassVariant.Regular,
			isDark: IsDspDark,
			isHovered: isHovered,
			isSelected: isSelected);
	}

	public bool IsDspDark => ResolveIsDspDark();

	private bool ResolveIsDspDark()
	{
		try
		{
			AppSettings? settings = null;
			try
			{
				settings = (ViewModel?.SettingsService ?? new SettingsService()).Load();
			}
			catch
			{
			}

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
			if (!System.IO.File.Exists(path)) return false;
			DateTime writeUtc = System.IO.File.GetLastWriteTimeUtc(path);
			if (string.Equals(_cachedWallpaperPath, path, StringComparison.OrdinalIgnoreCase)
				&& _cachedWallpaperWriteUtc == writeUtc)
			{
				return _cachedWallpaperIsDark;
			}

			using var codec = SKCodec.Create(path);
			if (codec == null) return false;

			int maxDim = Math.Max(1, Math.Max(codec.Info.Width, codec.Info.Height));
			var scaled = codec.GetScaledDimensions(Math.Min(1.0f, 64.0f / maxDim));
			var info = new SKImageInfo(Math.Max(1, scaled.Width), Math.Max(1, scaled.Height), SKColorType.Bgra8888, SKAlphaType.Premul);
			using var bmp = new SKBitmap(info);
			var result = codec.GetPixels(info, bmp.GetPixels());
			if (result != SKCodecResult.Success && result != SKCodecResult.IncompleteInput)
			{
				return false;
			}

			double totalLum = 0;
			int count = 0;
			for (int y = 0; y < bmp.Height; y++)
			{
				for (int x = 0; x < bmp.Width; x++)
				{
					var c = bmp.GetPixel(x, y);
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

	public Brush DspTextPrimaryBrush => new SolidColorBrush(IsDspDark
		? Colors.White                                     // #FFFFFF
		: Windows.UI.Color.FromArgb(255, 15, 23, 42));     // #0F172A

	public Brush DspTextSecondaryBrush => new SolidColorBrush(IsDspDark
		? Windows.UI.Color.FromArgb(255, 248, 250, 252)    // #F8FAFC
		: Windows.UI.Color.FromArgb(255, 51, 65, 85));     // #334155

	public Brush DspTextSubHeaderBrush => new SolidColorBrush(IsDspDark
		? Windows.UI.Color.FromArgb(255, 226, 232, 240)    // #E2E8F0
		: Windows.UI.Color.FromArgb(255, 71, 85, 105));    // #475569

	public Brush DspTextMutedBrush => new SolidColorBrush(IsDspDark
		? Windows.UI.Color.FromArgb(255, 203, 213, 225)    // #CBD5E1
		: Windows.UI.Color.FromArgb(255, 100, 116, 139));  // #64748B

	public Brush DspCardBgBrush => CreateAcrylicCardBrush(0.88, 235);
	public Brush DspCardBorderBrush => CreateLiquidGlassRimBrush();
	public Brush DspSoftSurfaceBrush => IsDspDark
		? CreateWhiteAcrylicBrush(0.14, 42)
		: new SolidColorBrush(Windows.UI.Color.FromArgb(220, 241, 245, 249));
	public Brush DspAccentSoftBgBrush => new SolidColorBrush(IsDspDark
		? Windows.UI.Color.FromArgb(85, 99, 102, 241)
		: Windows.UI.Color.FromArgb(255, 238, 242, 255));
	public Brush DspAccentSoftBorderBrush => new SolidColorBrush(IsDspDark
		? Windows.UI.Color.FromArgb(180, 129, 140, 248)
		: Windows.UI.Color.FromArgb(255, 199, 210, 254));
	public Brush DspAccentSoftTextBrush => new SolidColorBrush(IsDspDark
		? Windows.UI.Color.FromArgb(255, 199, 210, 254)
		: Windows.UI.Color.FromArgb(255, 79, 70, 229));

	private void RefreshInitializedWorkbenchesTheme(bool isDark)
	{
		if (_eqWorkbenchControl != null) _eqWorkbenchControl.UpdateTheme(isDark);
		if (_roomCorrectionWorkbench != null) RefreshRoomCorrectionTheme(isDark);
		if (_headphoneCorrectionWorkbench != null) RefreshHeadphoneCorrectionTheme(isDark);
		if (_channelToolWorkbench != null) RefreshChannelToolTheme(isDark);
		if (_compressorWorkbench != null) RefreshCompressorTheme(isDark);
		if (_multibandCompressorWorkbench != null) RefreshMultibandTheme(isDark);
		if (_deEsserWorkbench != null) RefreshDeEsserTheme(isDark);
		if (_crossfeedWorkbench != null) RefreshCrossfeedTheme(isDark);
		if (_stereoFieldWorkbench != null) RefreshStereoFieldTheme(isDark);
	}

	private void RefreshWorkbenchThemeByView(FrameworkElement view, bool isDark)
	{
		if (ReferenceEquals(view, _parametricEqWorkbench)) _eqWorkbenchControl?.UpdateTheme(isDark);
		else if (ReferenceEquals(view, _roomCorrectionWorkbench)) RefreshRoomCorrectionTheme(isDark);
		else if (ReferenceEquals(view, _headphoneCorrectionWorkbench)) RefreshHeadphoneCorrectionTheme(isDark);
		else if (ReferenceEquals(view, _channelToolWorkbench)) RefreshChannelToolTheme(isDark);
		else if (ReferenceEquals(view, _compressorWorkbench)) RefreshCompressorTheme(isDark);
		else if (ReferenceEquals(view, _multibandCompressorWorkbench)) RefreshMultibandTheme(isDark);
		else if (ReferenceEquals(view, _deEsserWorkbench)) RefreshDeEsserTheme(isDark);
		else if (ReferenceEquals(view, _crossfeedWorkbench)) RefreshCrossfeedTheme(isDark);
		else if (ReferenceEquals(view, _stereoFieldWorkbench)) RefreshStereoFieldTheme(isDark);
	}

	public void ApplyDspLiquidGlass()
	{
		bool isDark = IsDspDark;
		Background = CreateWhiteAcrylicBrush(0.65, 185);
		RefreshSidebarVisuals();
		RefreshInitializedWorkbenchesTheme(isDark);
		if (_workbenchHost != null)
		{
			if (!ReferenceEquals(_workbenchHost.Children.FirstOrDefault(), _parametricEqWorkbench))
			{
				LiquidGlassEngine.UpgradeVisualTreeToLiquidGlass(_workbenchHost, isDark);
			}
			RefreshWorkbenchTextContrast(_workbenchHost, isDark);
		}
		else
		{
			LiquidGlassEngine.UpgradeVisualTreeToLiquidGlass(this, isDark);
			RefreshWorkbenchTextContrast(this, isDark);
		}
	}

	private void UpgradeAndRefreshWorkbenchVisuals(FrameworkElement view, bool isDark)
	{
		if (!ReferenceEquals(view, _parametricEqWorkbench))
		{
			LiquidGlassEngine.UpgradeVisualTreeToLiquidGlass(view, isDark: isDark);
			RefreshWorkbenchTextContrast(view, isDark);
		}

		if (view.IsLoaded) return;

		void OnViewLoaded(object s, RoutedEventArgs e)
		{
			view.Loaded -= OnViewLoaded;
			bool currentDark = IsDspDark;
			RefreshWorkbenchThemeByView(view, currentDark);
			if (!ReferenceEquals(view, _parametricEqWorkbench))
			{
				LiquidGlassEngine.UpgradeVisualTreeToLiquidGlass(view, isDark: currentDark);
				RefreshWorkbenchTextContrast(view, currentDark);
			}
		}

		view.Loaded += OnViewLoaded;
	}

	private void RefreshWorkbenchTextContrast(DependencyObject? root, bool isDark, bool parentHasLightSolidBg = false)
	{
		if (root == null || root is ParametricEqWorkbenchControl) return;
		int count = VisualTreeHelper.GetChildrenCount(root);
		for (int i = 0; i < count; i++)
		{
			var child = VisualTreeHelper.GetChild(root, i);
			if (child is ParametricEqWorkbenchControl)
			{
				continue;
			}
			bool childHasLightBg = parentHasLightSolidBg;
			if (child is Border b && b.Background is SolidColorBrush bScb && bScb.Color.A >= 160)
			{
				double lum = 0.2126 * bScb.Color.R + 0.7152 * bScb.Color.G + 0.0722 * bScb.Color.B;
				childHasLightBg = lum > 165.0;
			}
			else if (child is Border bClear && bClear.Background is LinearGradientBrush)
			{
				// LiquidGlass gradient surface follows isDark
				childHasLightBg = !isDark;
			}

			bool effectiveDark = isDark && !childHasLightBg;

			if (child is TextBlock tb)
			{
				tb.Foreground = RemapNeutralBrush(tb.Foreground, effectiveDark, tb.FontSize >= 14 || tb.FontWeight.Weight >= 600);
			}
			else if (child is FontIcon fi)
			{
				fi.Foreground = RemapNeutralBrush(fi.Foreground, effectiveDark, isPrimary: false);
			}
			else if (child is Control ctrl && child is not TextBox)
			{
				if (ctrl.Background is SolidColorBrush bgScb && bgScb.Color.A >= 160)
				{
					double lum = 0.2126 * bgScb.Color.R + 0.7152 * bgScb.Color.G + 0.0722 * bgScb.Color.B;
					childHasLightBg = lum > 165.0;
					bool isColoredAccent = Math.Max(bgScb.Color.R, Math.Max(bgScb.Color.G, bgScb.Color.B)) - Math.Min(bgScb.Color.R, Math.Min(bgScb.Color.G, bgScb.Color.B)) > 35;
					if (!isColoredAccent)
					{
						ctrl.Foreground = RemapNeutralBrush(ctrl.Foreground, isDark && !childHasLightBg, isPrimary: false);
					}
					else if (!childHasLightBg)
					{
						// Preserve white/light text inside dark colored accent buttons (e.g. indigo/green primary actions).
						continue;
					}
				}
				else
				{
					if (ctrl.Background is LinearGradientBrush)
					{
						childHasLightBg = !isDark;
					}
					ctrl.Foreground = RemapNeutralBrush(ctrl.Foreground, isDark && !childHasLightBg, isPrimary: false);
				}
			}
			RefreshWorkbenchTextContrast(child, isDark, childHasLightBg);
		}
	}

	private Brush RemapNeutralBrush(Brush? brush, bool isDark, bool isPrimary)
	{
		if (brush is not SolidColorBrush scb) return brush!;
		var c = scb.Color;
		if (c.A < 200) return brush;

		if (isDark)
		{
			// Remap dark slate or dim grey neutral colors to crisp high-contrast dark-mode brushes
			if (c.R == 15 && c.G == 23 && c.B == 42) return DspTextPrimaryBrush;
			if (c.R == 30 && c.G == 41 && c.B == 59) return isPrimary ? DspTextPrimaryBrush : DspTextSecondaryBrush;
			if (c.R == 51 && c.G == 65 && c.B == 85) return DspTextSecondaryBrush;
			if (c.R == 71 && c.G == 85 && c.B == 105) return DspTextSubHeaderBrush;
			if (c.R == 100 && c.G == 116 && c.B == 139) return DspTextMutedBrush;
			if (c.R == 148 && c.G == 163 && c.B == 184) return DspTextMutedBrush;
			// Any other dark neutral grey/black text on dark glass
			if (c.R <= 90 && c.G <= 105 && c.B <= 125 && Math.Max(c.R, Math.Max(c.G, c.B)) - Math.Min(c.R, Math.Min(c.G, c.B)) <= 45)
			{
				return isPrimary ? DspTextPrimaryBrush : DspTextSubHeaderBrush;
			}
		}
		else
		{
			if (c.R == 255 && c.G == 255 && c.B == 255) return DspTextPrimaryBrush;
			if (c.R == 248 && c.G == 250 && c.B == 252) return DspTextPrimaryBrush;
			if (c.R == 241 && c.G == 245 && c.B == 249) return DspTextSecondaryBrush;
			if (c.R == 226 && c.G == 232 && c.B == 240) return DspTextSubHeaderBrush;
			if (c.R == 203 && c.G == 213 && c.B == 225) return DspTextMutedBrush;
		}
		return brush;
	}

	private void RefreshSidebarVisuals()
	{
		bool isDark = IsDspDark;
		if (_sidebarBorder != null)
		{
			LiquidGlassEngine.AttachInteractiveLiquidGlass(_sidebarBorder, LiquidGlassVariant.FloatingBar, isDark: isDark, enablePointerTracking: true);
		}
		if (_sidebarDspBadgeTb != null)
		{
			_sidebarDspBadgeTb.Foreground = new SolidColorBrush(isDark
				? Windows.UI.Color.FromArgb(255, 165, 180, 252)
				: Windows.UI.Color.FromArgb(255, 99, 102, 241));
		}
		if (_sidebarBrandTitleTb != null)
		{
			_sidebarBrandTitleTb.Foreground = DspTextPrimaryBrush;
		}
		if (_sidebarSubtitleTb != null)
		{
			_sidebarSubtitleTb.Foreground = DspTextSubHeaderBrush;
		}
		Brush sectionBrush = DspTextSubHeaderBrush;
		foreach (var header in _sidebarSectionHeaders)
		{
			header.Foreground = sectionBrush;
		}
		if (_sidebarAdvTitle != null)
		{
			_sidebarAdvTitle.Foreground = sectionBrush;
		}
		if (_sidebarAdvChevron != null)
		{
			_sidebarAdvChevron.Foreground = DspTextMutedBrush;
		}
		UpdateSidebarNavButtonColors();
	}

	private void UpdateSidebarNavButtonColors()
	{
		bool isDark = IsDspDark;
		foreach (var btn in _sidebarNavButtons)
		{
			bool isSelected = (btn.Tag as string) == _currentDspCategory;
			if (isSelected)
			{
				btn.Background = LiquidGlassEngine.CreateSurfaceBrush(LiquidGlassVariant.AccentLensed, isDark: isDark, 0.95);
				btn.BorderBrush = LiquidGlassEngine.CreateSpecularRimBrush(LiquidGlassVariant.AccentLensed, isDark: isDark, isHovered: true, isSelected: true);
				btn.BorderThickness = new Thickness(1);
			}
			else
			{
				btn.Background = new SolidColorBrush(Colors.Transparent);
				btn.BorderBrush = new SolidColorBrush(Colors.Transparent);
				btn.BorderThickness = new Thickness(0);
			}

			if (btn.Content is Grid g)
			{
				if (g.Children.Count > 0 && g.Children[0] is FontIcon ic)
				{
					ic.Foreground = isSelected
						? new SolidColorBrush(isDark ? Windows.UI.Color.FromArgb(255, 224, 231, 255) : Windows.UI.Color.FromArgb(255, 79, 70, 229))
						: DspTextMutedBrush;
				}
				if (g.Children.Count > 1 && g.Children[1] is StackPanel sp)
				{
					if (sp.Children.Count > 0 && sp.Children[0] is TextBlock t1)
					{
						t1.Foreground = isSelected
							? new SolidColorBrush(isDark ? Colors.White : Windows.UI.Color.FromArgb(255, 67, 56, 202))
							: (isDark ? DspTextPrimaryBrush : new SolidColorBrush(Windows.UI.Color.FromArgb(255, 30, 41, 59)));
					}
					if (sp.Children.Count > 1 && sp.Children[1] is TextBlock t2)
					{
						t2.Foreground = isSelected
							? new SolidColorBrush(isDark ? Windows.UI.Color.FromArgb(255, 224, 231, 255) : Windows.UI.Color.FromArgb(255, 79, 70, 229))
							: DspTextMutedBrush;
					}
				}
				if (g.Children.Count > 2 && g.Children[2] is FontIcon checkIc)
				{
					checkIc.Foreground = new SolidColorBrush(isDark
						? Windows.UI.Color.FromArgb(255, 74, 222, 128)
						: Windows.UI.Color.FromArgb(255, 22, 163, 74));
				}
			}
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

		PanelLayoutController layout = _layout;
		layout.LayoutChanged = (Action)Delegate.Combine(layout.LayoutChanged, new Action(SaveLayouts));
		ActualThemeChanged += (s, e) => ApplyDspLiquidGlass();
		Loaded += (object _, RoutedEventArgs _) =>
		{
			StartDspMeterTimer();
			if (_currentDspCategory == "param_eq")
			{
				StartEqSpectrumTimer();
			}
			ApplyDspLiquidGlass();
		};
		Unloaded += (object _, RoutedEventArgs _) =>
		{
			StopDspMeterTimer();
			StopEqSpectrumTimer();
			StopMixAssistantLearnTimer(finalizeIfLearning: true);
			PanelLayoutController layout2 = _layout;
			layout2.LayoutChanged = (Action)Delegate.Remove(layout2.LayoutChanged, new Action(SaveLayouts));
		};
	}

	public DspPage(DspViewModel viewModel)
		: this()
	{
		ViewModel = viewModel;
		DataContext = viewModel;
		SyncParametricEqWorkbenchFromViewModel();
	}

	protected override void OnNavigatedTo(NavigationEventArgs e)
	{
		base.OnNavigatedTo(e);
		if (e.Parameter is DspViewModel dspViewModel)
		{
			ViewModel = dspViewModel;
			DataContext = dspViewModel;
		}
		StartDspMeterTimer();
		DspEq.Gains = ViewModel.EqGains;
		EqPresetCombo.SelectedIndex = Math.Max(0, Array.IndexOf(PresetNames, ViewModel.EqPresetName));
		ReverbPresetCombo.SelectedIndex = Math.Max(0, Array.IndexOf(ReverbNames, ViewModel.ReverbPreset));
		DrawEqCurve(ViewModel.EqGains);
		SyncParametricEqWorkbenchFromViewModel();
		RestoreLayouts();
		UpdateVst3SidebarSubtitle();
		if (_currentDspCategory != "free")
		{
			SetDspViewCategory(_currentDspCategory);
		}
		if (_currentDspCategory == "param_eq")
		{
			StartEqSpectrumTimer();
		}
		ApplyDspLiquidGlass();
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
		StopDspMeterTimer();
		StopEqSpectrumTimer();
		StopMixAssistantLearnTimer(finalizeIfLearning: true);
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
				// Keep plugin audio processing active in rack when GUI window is closed without interrupting playback
				ViewModel.Vst3StatusText = "已关闭《" + vst3PluginState.Name + "》原生窗口：插件仍在机架后台实时处理音频（可随时重新打开界面或停用）";
				RefreshVst3RackUi();
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
			Background = CreateAcrylicCardBrush(),
			BorderBrush = CreateLiquidGlassRimBrush(),
			BorderThickness = new Thickness(1)
		};
		LiquidGlassEngine.AttachInteractiveLiquidGlass(panel, LiquidGlassVariant.Regular, isDark: IsDspDark, enablePointerTracking: true);

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
			rootGrid.Background = new SolidColorBrush(Colors.Transparent);

			// 2-column layout: Col 0 = Left Sidebar (240px), Col 1 = Main Workbench (*)
			rootGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(240, GridUnitType.Pixel) });
			rootGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.0, GridUnitType.Star) });

			var causticBackdrop = LiquidGlassEngine.CreateAmbientCausticCanvas(() => IsDspDark, enablePointLights: false);
			Grid.SetColumnSpan(causticBackdrop, 2);
			rootGrid.Children.Add(causticBackdrop);

			var sidebar = CreateEchoSidebar();
			Grid.SetColumn(sidebar, 0);
			rootGrid.Children.Add(sidebar);

			_workbenchHost = new Grid
			{
				Background = new SolidColorBrush(Colors.Transparent)
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
			BorderThickness = new Thickness(0, 0, 1.2, 0),
			Padding = new Thickness(12, 16, 12, 12)
		};
		_sidebarBorder = border;
		LiquidGlassEngine.AttachInteractiveLiquidGlass(border, LiquidGlassVariant.FloatingBar, isDark: IsDspDark, enablePointerTracking: true);

		var sidebarGrid = new Grid();
		sidebarGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Brand Header
		sidebarGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1.0, GridUnitType.Star) }); // Scrollable items
		sidebarGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Footer Output

		// 1. Logo Header
		var headerStack = new StackPanel { Spacing = 2, Margin = new Thickness(6, 0, 0, 14) };
		_sidebarDspBadgeTb = new TextBlock
		{
			Text = "DSP",
			FontSize = 10,
			FontWeight = Microsoft.UI.Text.FontWeights.Bold,
			Foreground = new SolidColorBrush(IsDspDark
				? Windows.UI.Color.FromArgb(255, 165, 180, 252)
				: Windows.UI.Color.FromArgb(255, 99, 102, 241))
		};
		headerStack.Children.Add(_sidebarDspBadgeTb);
		_sidebarBrandTitleTb = new TextBlock
		{
			Text = "FurinaPlayer",
			FontSize = 20,
			FontWeight = Microsoft.UI.Text.FontWeights.ExtraBold,
			Foreground = DspTextPrimaryBrush
		};
		headerStack.Children.Add(_sidebarBrandTitleTb);
		_sidebarSubtitleTb = new TextBlock
		{
			Text = "信号控制",
			FontSize = 11,
			Foreground = DspTextSubHeaderBrush
		};
		headerStack.Children.Add(_sidebarSubtitleTb);
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

		_sidebarSectionHeaders.Clear();
		void AddNavSectionHeader(string title)
		{
			var headerTb = new TextBlock
			{
				Text = title,
				FontSize = 11,
				FontWeight = Microsoft.UI.Text.FontWeights.Bold,
				Foreground = DspTextSubHeaderBrush,
				Margin = new Thickness(8, 12, 0, 4)
			};
			_sidebarSectionHeaders.Add(headerTb);
			itemsStack.Children.Add(headerTb);
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
				Foreground = DspTextMutedBrush,
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
				Foreground = IsDspDark ? DspTextPrimaryBrush : DspTextSecondaryBrush
			};
			var subTb = new TextBlock
			{
				Text = subtitle,
				FontSize = 10,
				Foreground = DspTextMutedBrush
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
			else if (key == "vst3")
			{
				_vst3NavSubTb = subTb;
				UpdateVst3SidebarSubtitle();
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
					Foreground = new SolidColorBrush(IsDspDark
						? Windows.UI.Color.FromArgb(255, 74, 222, 128)
						: Windows.UI.Color.FromArgb(255, 22, 163, 74)),
					VerticalAlignment = VerticalAlignment.Center
				};
				Grid.SetColumn(checkIcon, 2);
				rowGrid.Children.Add(checkIcon);
			}

			btn.Content = rowGrid;
			btn.PointerEntered += (s, e) =>
			{
				if (s is Button b && b.Tag is string cat && cat != _currentDspCategory)
				{
					b.Background = LiquidGlassEngine.CreateSurfaceBrush(LiquidGlassVariant.Clear, isDark: IsDspDark, 0.72);
					b.BorderBrush = LiquidGlassEngine.CreateSpecularRimBrush(LiquidGlassVariant.Clear, isDark: IsDspDark, isHovered: true, isSelected: false);
					b.BorderThickness = new Thickness(1);
				}
			};
			btn.PointerExited += (s, e) =>
			{
				if (s is Button b && b.Tag is string cat && cat != _currentDspCategory)
				{
					b.Background = new SolidColorBrush(Colors.Transparent);
					b.BorderBrush = new SolidColorBrush(Colors.Transparent);
					b.BorderThickness = new Thickness(0);
				}
			};
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
		AddNavSectionHeader("插件宿主");
		itemsStack.Children.Add(CreateNavButton("vst3", "\uE8B8", "VST3 机架", "原生插件宿主"));
		AddNavSectionHeader("空间与输出");
		itemsStack.Children.Add(CreateNavButton("room", "\uE7BE", "房间校正", "无 IR"));
		itemsStack.Children.Add(CreateNavButton("channel", "\uE768", "声道工具", "立体声直通"));
		itemsStack.Children.Add(CreateNavButton("safety", "\uE72E", "输出安全", "待命"));

		// 高级设置 (含折叠/展开指示与状态)
		var advHeaderGrid = new Grid { Margin = new Thickness(8, 12, 8, 4) };
		advHeaderGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.0, GridUnitType.Star) });
		advHeaderGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

		var advTitle = new TextBlock
		{
			Text = "高级设置",
			FontSize = 11,
			FontWeight = Microsoft.UI.Text.FontWeights.Bold,
			Foreground = DspTextSubHeaderBrush
		};
		_sidebarAdvTitle = advTitle;
		Grid.SetColumn(advTitle, 0);
		advHeaderGrid.Children.Add(advTitle);

		var advChevron = new FontIcon
		{
			Glyph = "\uE70D",
			FontSize = 10,
			Foreground = DspTextMutedBrush
		};
		_sidebarAdvChevron = advChevron;
		Grid.SetColumn(advChevron, 1);
		advHeaderGrid.Children.Add(advChevron);

		var advPanel = new StackPanel { Spacing = 2, Visibility = Visibility.Visible };
		advHeaderGrid.Tapped += (s, e) =>
		{
			if (advPanel.Visibility == Visibility.Visible)
			{
				advPanel.Visibility = Visibility.Collapsed;
				advChevron.Glyph = "\uE70E";
			}
			else
			{
				advPanel.Visibility = Visibility.Visible;
				advChevron.Glyph = "\uE70D";
			}
		};
		itemsStack.Children.Add(advHeaderGrid);

		advPanel.Children.Add(CreateNavButton("compressor", "\uE9D9", "压缩器", "已旁路"));
		advPanel.Children.Add(CreateNavButton("multiband", "\uE994", "三频段压缩", "已旁路"));
		advPanel.Children.Add(CreateNavButton("deesser", "\uE74F", "去齿音", "已旁路"));
		advPanel.Children.Add(CreateNavButton("limiter", "\uE71A", "限幅", "自动上限", hasCheck: true));
		advPanel.Children.Add(CreateNavButton("crossfeed", "\uE7F5", "交叉馈送", "已旁路"));
		advPanel.Children.Add(CreateNavButton("virtual_stage", "\uE995", "虚拟声场", "已旁路"));
		advPanel.Children.Add(CreateNavButton("stereo_field", "\uE7BE", "立体声场", "132%", hasCheck: true));
		advPanel.Children.Add(CreateNavButton("channel_matrix", "\uE8AB", "声道矩阵", "已旁路"));
		// VST3 promoted to top-level section "插件宿主"
		itemsStack.Children.Add(advPanel);
		sv.Content = itemsStack;
		Grid.SetRow(sv, 1);
		sidebarGrid.Children.Add(sv);

		// 3. Footer device
		var footerBtn = CreateNavButton("device", "\uE774", "系统输出", "自动 · 共享模式");
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
			Foreground = DspTextPrimaryBrush,
			VerticalAlignment = VerticalAlignment.Center
		});
		Grid.SetColumn(titleSp, 0);
		headerGrid.Children.Add(titleSp);
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
		safetyTitleSp.Children.Add(new TextBlock { Text = "增益与峰值安全", FontSize = 14, FontWeight = Microsoft.UI.Text.FontWeights.SemiBold, Foreground = DspTextPrimaryBrush });
		safetyTitleSp.Children.Add(new TextBlock { Text = "等待当前宿主读数", FontSize = 12, Foreground = DspTextMutedBrush, VerticalAlignment = VerticalAlignment.Center });
		safetyStack.Children.Add(safetyTitleSp);

		var indGrid3 = new Grid { ColumnSpacing = 16 };
		indGrid3.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.0, GridUnitType.Star) });
		indGrid3.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.0, GridUnitType.Star) });
		indGrid3.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.0, GridUnitType.Star) });

		var ind1 = new TextBlock { Text = "配置风险估计: 未报告", FontSize = 12, Foreground = DspTextSubHeaderBrush };
		var ind2 = new TextBlock { Text = "实测峰值超限: 未知", FontSize = 12, Foreground = DspTextSubHeaderBrush };
		var ind3 = new TextBlock { Text = "当前限幅: 未知", FontSize = 12, Foreground = DspTextSubHeaderBrush };
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
			box.Children.Add(new TextBlock { Text = label, FontSize = 11, Foreground = DspTextMutedBrush });
			box.Children.Add(new TextBlock { Text = val, FontSize = 13, FontWeight = Microsoft.UI.Text.FontWeights.SemiBold, Foreground = DspTextPrimaryBrush });
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
			Foreground = DspTextMutedBrush,
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
			Background = IsDspDark ? new SolidColorBrush(Windows.UI.Color.FromArgb(255, 30, 41, 59)) : new SolidColorBrush(Windows.UI.Color.FromArgb(255, 241, 245, 249)),
			BorderBrush = DspCardBorderBrush,
			BorderThickness = new Thickness(1),
			CornerRadius = new CornerRadius(8),
			Padding = new Thickness(12, 10, 12, 10),
			Child = new TextBlock
			{
				Text = "在 32 位浮点架构下，DSP 信号链内部可保留大于 0 dBFS 的电平余量，不会发生数字硬削波截断。Headroom 控制位于主输出级，可在进入声卡 DAC 之前预留安全的采样间真实峰值 (True Peak) 余量，结合限幅器确保无失真高保真还原。",
				FontSize = 11,
				Foreground = DspTextSecondaryBrush,
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
			Foreground = DspTextPrimaryBrush,
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
			Foreground = DspTextPrimaryBrush,
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
			Foreground = DspTextPrimaryBrush,
			VerticalAlignment = VerticalAlignment.Bottom
		};
		var unitTb = new TextBlock
		{
			Text = "dB",
			FontSize = 26,
			FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
			Foreground = DspTextSubHeaderBrush,
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
			Foreground = DspTextMutedBrush,
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
			Background = IsDspDark ? new SolidColorBrush(Windows.UI.Color.FromArgb(120, 30, 41, 59)) : new SolidColorBrush(Windows.UI.Color.FromArgb(210, 255, 255, 255)),
			BorderBrush = IsDspDark ? new SolidColorBrush(Windows.UI.Color.FromArgb(70, 255, 255, 255)) : new SolidColorBrush(Windows.UI.Color.FromArgb(180, 203, 213, 225)),
			BorderThickness = new Thickness(1),
			Foreground = DspTextPrimaryBrush
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

		scaleGrid.Children.Add(new TextBlock { Text = "-12 dB", FontSize = 10, Foreground = DspTextMutedBrush, HorizontalAlignment = HorizontalAlignment.Left });
		var sc2 = new TextBlock { Text = "-6 dB", FontSize = 10, Foreground = DspTextMutedBrush, HorizontalAlignment = HorizontalAlignment.Center };
		Grid.SetColumn(sc2, 1); scaleGrid.Children.Add(sc2);
		var sc3 = new TextBlock { Text = "0 dB", FontSize = 11, FontWeight = Microsoft.UI.Text.FontWeights.Bold, Foreground = DspTextPrimaryBrush, HorizontalAlignment = HorizontalAlignment.Center };
		Grid.SetColumn(sc3, 2); scaleGrid.Children.Add(sc3);
		var sc4 = new TextBlock { Text = "+6 dB", FontSize = 10, Foreground = DspTextMutedBrush, HorizontalAlignment = HorizontalAlignment.Center };
		Grid.SetColumn(sc4, 3); scaleGrid.Children.Add(sc4);
		var sc5 = new TextBlock { Text = "+12 dB", FontSize = 10, Foreground = DspTextMutedBrush, HorizontalAlignment = HorizontalAlignment.Right };
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
			Background = IsDspDark ? new SolidColorBrush(Windows.UI.Color.FromArgb(120, 30, 41, 59)) : new SolidColorBrush(Windows.UI.Color.FromArgb(210, 255, 255, 255)),
			BorderBrush = IsDspDark ? new SolidColorBrush(Windows.UI.Color.FromArgb(70, 255, 255, 255)) : new SolidColorBrush(Windows.UI.Color.FromArgb(180, 203, 213, 225)),
			BorderThickness = new Thickness(1),
			Foreground = DspTextPrimaryBrush
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
				Background = IsDspDark ? new SolidColorBrush(Windows.UI.Color.FromArgb(120, 30, 41, 59)) : new SolidColorBrush(Windows.UI.Color.FromArgb(210, 255, 255, 255)),
				BorderBrush = IsDspDark ? new SolidColorBrush(Windows.UI.Color.FromArgb(70, 255, 255, 255)) : new SolidColorBrush(Windows.UI.Color.FromArgb(180, 203, 213, 225))
			};
			Microsoft.UI.Xaml.Automation.AutomationProperties.SetName(pillBtn, $"快速应用 {label} {targetDb:+0;-0;0} dB");
			ToolTipService.SetToolTip(pillBtn, $"快速切换至 {label} {targetDb:+0;-0;0} dB");
			var contentSp = new StackPanel { Spacing = 1, HorizontalAlignment = HorizontalAlignment.Center };
			contentSp.Children.Add(new TextBlock { Text = label, FontSize = 11, Foreground = DspTextMutedBrush, HorizontalAlignment = HorizontalAlignment.Center });
			contentSp.Children.Add(new TextBlock { Text = $"{targetDb:+0;-0;0} dB", FontSize = 12, FontWeight = Microsoft.UI.Text.FontWeights.SemiBold, Foreground = DspTextPrimaryBrush, HorizontalAlignment = HorizontalAlignment.Center });
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
					new FontIcon { Glyph = "\uE7F6", FontSize = 13, Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 99, 102, 241)) },
					new TextBlock { Text = "按住听直通", FontSize = 12, FontWeight = Microsoft.UI.Text.FontWeights.Medium, Foreground = DspTextSecondaryBrush }
				}
			},
			Padding = new Thickness(14, 8, 14, 8),
			CornerRadius = new CornerRadius(8),
			Background = IsDspDark ? new SolidColorBrush(Windows.UI.Color.FromArgb(120, 30, 41, 59)) : new SolidColorBrush(Windows.UI.Color.FromArgb(210, 255, 255, 255)),
			BorderBrush = IsDspDark ? new SolidColorBrush(Windows.UI.Color.FromArgb(70, 255, 255, 255)) : new SolidColorBrush(Windows.UI.Color.FromArgb(180, 203, 213, 225)),
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
			Foreground = DspTextMutedBrush,
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
			Foreground = DspTextPrimaryBrush
		});

		rightStack.Children.Add(new TextBlock
		{
			Text = "-- --",
			FontSize = 38,
			FontWeight = Microsoft.UI.Text.FontWeights.Bold,
			Foreground = DspTextPrimaryBrush,
			Margin = new Thickness(0, 4, 0, 2)
		});

		var lampSp = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8, VerticalAlignment = VerticalAlignment.Center };
		var lampDot = new Ellipse { Width = 8, Height = 8, Fill = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 34, 197, 94)) };
		lampSp.Children.Add(lampDot);
		lampSp.Children.Add(new TextBlock { Text = "等待当前宿主读数", FontSize = 12, Foreground = DspTextMutedBrush });
		rightStack.Children.Add(lampSp);

		var recoCard = new Border
		{
			Background = IsDspDark ? new SolidColorBrush(Windows.UI.Color.FromArgb(140, 30, 41, 59)) : new SolidColorBrush(Windows.UI.Color.FromArgb(160, 248, 250, 252)),
			BorderBrush = DspCardBorderBrush,
			BorderThickness = new Thickness(1),
			CornerRadius = new CornerRadius(10),
			Padding = new Thickness(16, 14, 16, 14),
			Margin = new Thickness(0, 10, 0, 0)
		};
		var recoStack = new StackPanel { Spacing = 8 };

		var recoHeadSp = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 6 };
		recoHeadSp.Children.Add(new FontIcon { Glyph = "\uEA18", FontSize = 12, Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 99, 102, 241)) });
		recoHeadSp.Children.Add(new TextBlock { Text = "建议 · 安全", FontSize = 11, Foreground = DspTextSubHeaderBrush });
		recoStack.Children.Add(recoHeadSp);

		recoStack.Children.Add(new TextBlock
		{
			Text = "保持待命",
			FontSize = 18,
			FontWeight = Microsoft.UI.Text.FontWeights.Bold,
			Foreground = DspTextPrimaryBrush
		});
		recoStack.Children.Add(new TextBlock
		{
			Text = "有 DSP 模块开启，但暂未检测到风险。",
			FontSize = 12,
			Foreground = DspTextMutedBrush,
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
			Background = IsDspDark ? new SolidColorBrush(Windows.UI.Color.FromArgb(120, 30, 41, 59)) : new SolidColorBrush(Windows.UI.Color.FromArgb(210, 255, 255, 255)),
			BorderBrush = IsDspDark ? new SolidColorBrush(Windows.UI.Color.FromArgb(70, 255, 255, 255)) : new SolidColorBrush(Windows.UI.Color.FromArgb(180, 203, 213, 225)),
			BorderThickness = new Thickness(1),
			Foreground = DspTextSecondaryBrush,
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
		budgetHeadSp.Children.Add(new TextBlock { Text = "Headroom 预算", FontSize = 15, FontWeight = Microsoft.UI.Text.FontWeights.Bold, Foreground = DspTextPrimaryBrush });
		_headroomStreamInfoTb = new TextBlock { Text = "等待当前音乐流数据", FontSize = 12, Foreground = DspTextMutedBrush, VerticalAlignment = VerticalAlignment.Center };
		budgetHeadSp.Children.Add(_headroomStreamInfoTb);
		Grid.SetColumn(budgetHeadSp, 0);
		budgetHeadGrid.Children.Add(budgetHeadSp);

		var btnRefreshBudget = new Button
		{
			Content = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 5, Children = { new FontIcon { Glyph = "\uE72C", FontSize = 12, Foreground = DspTextMutedBrush }, new TextBlock { Text = "刷新状态", FontSize = 12, Foreground = DspTextSecondaryBrush } } },
			Padding = new Thickness(10, 4, 10, 4),
			CornerRadius = new CornerRadius(6),
			Background = IsDspDark ? new SolidColorBrush(Windows.UI.Color.FromArgb(120, 30, 41, 59)) : new SolidColorBrush(Windows.UI.Color.FromArgb(210, 255, 255, 255)),
			BorderBrush = IsDspDark ? new SolidColorBrush(Windows.UI.Color.FromArgb(70, 255, 255, 255)) : new SolidColorBrush(Windows.UI.Color.FromArgb(140, 226, 232, 240)),
			BorderThickness = new Thickness(1)
		};
		void UpdateHeadroomTelemetry()
		{
			if (_headroomClipCountTb != null)
			{
				_headroomClipCountTb.Text = $"{_headroomClipCount} 次";
				_headroomClipCountTb.Foreground = new SolidColorBrush(_headroomClipCount == 0
					? Windows.UI.Color.FromArgb(255, 34, 197, 94)
					: Windows.UI.Color.FromArgb(255, 239, 68, 68));
			}
			if (_headroomLastClipTb != null)
			{
				_headroomLastClipTb.Text = _headroomLastClipTime.HasValue ? _headroomLastClipTime.Value.ToString("HH:mm:ss") : "无记录";
				_headroomLastClipTb.Foreground = DspTextSecondaryBrush;
			}
			if (_headroomMarginTb != null)
			{
				_headroomMarginTb.Text = _headroomAvgMarginDb != 0.0 ? $"{_headroomAvgMarginDb:+0.0;-0.0;0.0} dB" : "--";
				_headroomMarginTb.Foreground = DspTextSecondaryBrush;
			}
			if (_headroomProtectStatusTb != null)
			{
				_headroomProtectStatusTb.Text = _headroomClipCount == 0 ? "待命" : "曾削波";
				_headroomProtectStatusTb.Foreground = new SolidColorBrush(_headroomClipCount == 0
					? Windows.UI.Color.FromArgb(255, 34, 197, 94)
					: Windows.UI.Color.FromArgb(255, 239, 68, 68));
			}
		}

		btnRefreshBudget.Click += (s, e) =>
		{
			_headroomClipCount = 0;
			_headroomLastClipTime = null;
			_headroomAvgMarginDb = 0.0;
			UpdateHeadroomTelemetry();
		};
		Grid.SetColumn(btnRefreshBudget, 1);
		budgetHeadGrid.Children.Add(btnRefreshBudget);
		budgetStack.Children.Add(budgetHeadGrid);

		var meterScaleGrid = new Grid { Margin = new Thickness(80, 0, 60, 0) };
		meterScaleGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.0, GridUnitType.Star) });
		meterScaleGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.0, GridUnitType.Star) });
		meterScaleGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.0, GridUnitType.Star) });
		meterScaleGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.0, GridUnitType.Star) });
		meterScaleGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.0, GridUnitType.Star) });
		meterScaleGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

		meterScaleGrid.Children.Add(new TextBlock { Text = "-24", FontSize = 10, Foreground = DspTextMutedBrush, HorizontalAlignment = HorizontalAlignment.Left });
		var ms1 = new TextBlock { Text = "-18", FontSize = 10, Foreground = DspTextMutedBrush, HorizontalAlignment = HorizontalAlignment.Center };
		Grid.SetColumn(ms1, 1); meterScaleGrid.Children.Add(ms1);
		var ms2 = new TextBlock { Text = "-12", FontSize = 10, Foreground = DspTextMutedBrush, HorizontalAlignment = HorizontalAlignment.Center };
		Grid.SetColumn(ms2, 2); meterScaleGrid.Children.Add(ms2);
		var ms3 = new TextBlock { Text = "-6", FontSize = 10, Foreground = DspTextMutedBrush, HorizontalAlignment = HorizontalAlignment.Center };
		Grid.SetColumn(ms3, 3); meterScaleGrid.Children.Add(ms3);
		var ms4 = new TextBlock { Text = "0", FontSize = 10, Foreground = DspTextMutedBrush, HorizontalAlignment = HorizontalAlignment.Center };
		Grid.SetColumn(ms4, 4); meterScaleGrid.Children.Add(ms4);
		var msUnit = new TextBlock { Text = "dBFS", FontSize = 10, Foreground = DspTextMutedBrush, Margin = new Thickness(8, 0, 0, 0) };
		Grid.SetColumn(msUnit, 5); meterScaleGrid.Children.Add(msUnit);
		budgetStack.Children.Add(meterScaleGrid);

		Grid CreateLedMeterRow(string label, out Border fillBar, out TextBlock valTb)
		{
			var row = new Grid { ColumnSpacing = 12, Margin = new Thickness(0, 3, 0, 3) };
			row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(80, GridUnitType.Pixel) });
			row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.0, GridUnitType.Star) });
			row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(60, GridUnitType.Pixel) });

			var lblTb = new TextBlock { Text = label, FontSize = 12, Foreground = DspTextSecondaryBrush, VerticalAlignment = VerticalAlignment.Center };
			Grid.SetColumn(lblTb, 0); row.Children.Add(lblTb);

			var trackContainer = new Grid
			{
				Height = 12,
				HorizontalAlignment = HorizontalAlignment.Stretch,
				VerticalAlignment = VerticalAlignment.Center
			};
			var trackBg = new Border
			{
				Height = 10,
				CornerRadius = new CornerRadius(5),
				Background = IsDspDark ? new SolidColorBrush(Windows.UI.Color.FromArgb(40, 255, 255, 255)) : new SolidColorBrush(Windows.UI.Color.FromArgb(255, 226, 232, 240)),
				HorizontalAlignment = HorizontalAlignment.Stretch
			};
			trackContainer.Children.Add(trackBg);

			var fill = new Border
			{
				Height = 10,
				CornerRadius = new CornerRadius(5),
				HorizontalAlignment = HorizontalAlignment.Left,
				Width = 0,
				Background = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 79, 70, 229))
			};
			trackContainer.Children.Add(fill);
			fillBar = fill;

			Grid.SetColumn(trackContainer, 1); row.Children.Add(trackContainer);

			var val = new TextBlock
			{
				Text = "--",
				FontSize = 12,
				FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
				Foreground = DspTextSecondaryBrush,
				HorizontalAlignment = HorizontalAlignment.Right,
				VerticalAlignment = VerticalAlignment.Center
			};
			Grid.SetColumn(val, 2); row.Children.Add(val);
			valTb = val;

			return row;
		}

		budgetStack.Children.Add(CreateLedMeterRow("输入峰值", out _headroomInMeterFill, out _headroomInMeterValTb));
		budgetStack.Children.Add(CreateLedMeterRow("输出估算", out _headroomOutMeterFill, out _headroomOutMeterValTb));
		budgetStack.Children.Add(CreateLedMeterRow("True Peak", out _headroomTpMeterFill, out _headroomTpMeterValTb));

		var statusFoldBtn = new Button
		{
			Content = "▼ 实时状态",
			Background = new SolidColorBrush(Colors.Transparent),
			BorderThickness = new Thickness(0),
			Padding = new Thickness(0, 6, 0, 2),
			FontSize = 12,
			Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 99, 102, 241)),
			FontWeight = Microsoft.UI.Text.FontWeights.Medium
		};
		var statusFoldContent = new Grid { Margin = new Thickness(0, 4, 0, 0), ColumnSpacing = 16 };
		statusFoldContent.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.0, GridUnitType.Star) });
		statusFoldContent.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.0, GridUnitType.Star) });
		statusFoldContent.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.0, GridUnitType.Star) });
		statusFoldContent.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.0, GridUnitType.Star) });
		statusFoldContent.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.0, GridUnitType.Star) });

		StackPanel MakeStatusStat(string statTitle, string statVal, out TextBlock valOutTb, Windows.UI.Color? col = null)
		{
			var sp = new StackPanel { Spacing = 2 };
			sp.Children.Add(new TextBlock { Text = statTitle, FontSize = 11, Foreground = DspTextMutedBrush });
			var valBlock = new TextBlock { Text = statVal, FontSize = 13, FontWeight = Microsoft.UI.Text.FontWeights.SemiBold, Foreground = col.HasValue ? new SolidColorBrush(col.Value) : DspTextPrimaryBrush };
			sp.Children.Add(valBlock);
			valOutTb = valBlock;
			return sp;
		}
		var st1 = MakeStatusStat("保护状态", "待命", out _headroomProtectStatusTb, Windows.UI.Color.FromArgb(255, 34, 197, 94));
		var st2 = MakeStatusStat("削波次数", "0 次", out _headroomClipCountTb, Windows.UI.Color.FromArgb(255, 34, 197, 94));
		var st3 = MakeStatusStat("最近削波", "无记录", out _headroomLastClipTb);
		var st4 = MakeStatusStat("True Peak", "--", out _headroomTruePeakTb);
		var st5 = MakeStatusStat("实时余量", "--", out _headroomMarginTb);
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
						? (IsDspDark ? new SolidColorBrush(Windows.UI.Color.FromArgb(50, 79, 70, 229)) : new SolidColorBrush(Windows.UI.Color.FromArgb(255, 238, 242, 255)))
						: (IsDspDark ? new SolidColorBrush(Windows.UI.Color.FromArgb(40, 255, 255, 255)) : new SolidColorBrush(Windows.UI.Color.FromArgb(255, 255, 255, 255)));
					btn.BorderBrush = isMatch
						? new SolidColorBrush(Windows.UI.Color.FromArgb(255, 79, 70, 229))
						: (IsDspDark ? new SolidColorBrush(Windows.UI.Color.FromArgb(60, 255, 255, 255)) : new SolidColorBrush(Windows.UI.Color.FromArgb(255, 203, 213, 225)));
					if (btn.Content is StackPanel sp && sp.Children.Count >= 2)
					{
						var activeMatchBrush = new SolidColorBrush(IsDspDark
							? Windows.UI.Color.FromArgb(255, 199, 210, 254)
							: Windows.UI.Color.FromArgb(255, 79, 70, 229));
						if (sp.Children[0] is TextBlock t1) t1.Foreground = isMatch ? activeMatchBrush : DspTextMutedBrush;
						if (sp.Children[1] is TextBlock t2) t2.Foreground = isMatch ? activeMatchBrush : DspTextPrimaryBrush;
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
		_eqWorkbenchControl = new ParametricEqWorkbenchControl
		{
			ShowApplyOfflineDspButton = false,
			ShowAnalyzeTrackButton = true
		};
		_eqWorkbenchControl.UpdateTheme(IsDspDark);

		SyncParametricEqWorkbenchFromViewModel();

		_eqWorkbenchControl.BandsChanged += (IReadOnlyList<EchoBiquadFilter> bands) =>
		{
			if (ViewModel == null || _eqWorkbenchControl == null) return;
			ViewModel.ApplyParametricEqState(bands, _eqWorkbenchControl.PreampDb, _eqWorkbenchControl.IsEqEnabled, save: true);
			SyncLegacyFreeEqUiFromViewModel();
		};

		_eqWorkbenchControl.BandChanged += (int idx, EchoBiquadFilter band) =>
		{
			if (ViewModel == null) return;
			ViewModel.Engine?.SetEchoEqBand(idx, band.FilterType, band.FrequencyHz, band.GainDb, band.Q, band.Enabled);
			ViewModel.SyncEchoEqBand(idx, band, save: true);
			SyncLegacyFreeEqUiFromViewModel();
		};

		_eqWorkbenchControl.PreampChanged += (double db) =>
		{
			if (ViewModel == null) return;
			ViewModel.EchoEqPreampDb = db;
			ViewModel.Engine?.SetEchoEqPreamp(db);
			ViewModel.SaveEchoSettings();
		};

		_eqWorkbenchControl.EnabledChanged += (bool en) =>
		{
			if (ViewModel == null) return;
			ViewModel.EchoEqEnabled = en;
			ViewModel.Engine?.SetEchoEqEnabled(en);
			ViewModel.SaveEchoSettings();
		};

		_eqWorkbenchControl.ImportAutoEqRequested += () => _ = ImportAutoEqFileAsync();
		_eqWorkbenchControl.MixAssistantLearnRequested += OnMixAssistantLearnRequested;
		_eqWorkbenchControl.MixAssistantAnalyzeAudioRequested += () => _ = OnMixAssistantAnalyzeAudioRequestedAsync();
		_eqWorkbenchControl.SaveUserPresetRequested += (string name, IReadOnlyList<EchoBiquadFilter> bands, double preampDb) =>
		{
			if (ViewModel == null || _eqWorkbenchControl == null) return;
			ViewModel.ApplyParametricEqState(bands, preampDb, _eqWorkbenchControl.IsEqEnabled, save: true);
			ViewModel.EqPresetName = name;
			ViewModel.StatusText = $"已保存自定义 EQ 预设：{name}";
		};

		_parametricEqWorkbench = new Border
		{
			Child = _eqWorkbenchControl
		};
		return _parametricEqWorkbench;
	}

	private void SyncParametricEqWorkbenchFromViewModel()
	{
		if (ViewModel == null || _eqWorkbenchControl == null) return;
		ViewModel.InitializeEchoEqBands();
		_eqWorkbenchControl.SetBands(ViewModel.EchoEqBands, ViewModel.EchoEqPreampDb, ViewModel.EchoEqEnabled);
	}

	private void SyncLegacyFreeEqUiFromViewModel()
	{
		if (ViewModel == null) return;
		if (DspEq != null)
		{
			DspEq.Gains = ViewModel.EqGains;
		}
		DrawEqCurve(ViewModel.EqGains);
	}

	private float[]? GetLiveSpectrumBands()
	{
		try
		{
			return ViewModel?.Engine?.Session?.GetLiveAudioMeters().Bands;
		}
		catch
		{
			return null;
		}
	}

	private void StartEqSpectrumTimer()
	{
		if (_currentDspCategory != "param_eq" || _eqWorkbenchControl == null || DispatcherQueue == null)
		{
			return;
		}
		if (_eqSpectrumTimer == null)
		{
			_eqSpectrumTimer = DispatcherQueue.CreateTimer();
			_eqSpectrumTimer.Interval = TimeSpan.FromMilliseconds(50);
			_eqSpectrumTimer.Tick += OnEqSpectrumTick;
		}
		if (!_eqSpectrumTimer.IsRunning)
		{
			_eqSpectrumTimer.Start();
		}
	}

	private void StopEqSpectrumTimer()
	{
		if (_eqSpectrumTimer != null && _eqSpectrumTimer.IsRunning)
		{
			_eqSpectrumTimer.Stop();
		}
	}

	private void OnEqSpectrumTick(DispatcherQueueTimer sender, object args)
	{
		if (_currentDspCategory != "param_eq" || _eqWorkbenchControl == null)
		{
			StopEqSpectrumTimer();
			return;
		}
		if (_mixAssistantLearnTimer?.IsRunning == true)
		{
			return;
		}
		float[]? bands = GetLiveSpectrumBands();
		if (bands != null && bands.Length > 0)
		{
			_eqWorkbenchControl.UpdateSpectrum(bands);
		}
	}

	private void OnMixAssistantLearnRequested(bool isLearning)
	{
		if (_eqWorkbenchControl == null) return;

		if (isLearning)
		{
			if (!_eqWorkbenchControl.MixAssistant.IsLearning)
			{
				_eqWorkbenchControl.MixAssistant.BeginLearning();
			}
			if (_mixAssistantLearnTimer == null && DispatcherQueue != null)
			{
				_mixAssistantLearnTimer = DispatcherQueue.CreateTimer();
				_mixAssistantLearnTimer.Interval = TimeSpan.FromMilliseconds(60);
				_mixAssistantLearnTimer.Tick += OnMixAssistantLearnTick;
			}
			if (_mixAssistantLearnTimer != null && !_mixAssistantLearnTimer.IsRunning)
			{
				_mixAssistantLearnTimer.Start();
			}
		}
		else
		{
			StopMixAssistantLearnTimer(finalizeIfLearning: true);
		}
	}

	private void OnMixAssistantLearnTick(DispatcherQueueTimer sender, object args)
	{
		if (_eqWorkbenchControl == null || !_eqWorkbenchControl.MixAssistant.IsLearning)
		{
			StopMixAssistantLearnTimer(finalizeIfLearning: false);
			return;
		}
		float[]? bands = GetLiveSpectrumBands();
		if (bands != null && bands.Length > 0)
		{
			_eqWorkbenchControl.UpdateSpectrum(bands);
			_eqWorkbenchControl.SetMixAssistantProgress(
				isLearning: true,
				progress01: _eqWorkbenchControl.MixAssistant.LearningProgress01,
				statusText: $"正在学习音频频谱平衡... ({_eqWorkbenchControl.MixAssistant.LearnedFrames} 帧)");
		}
	}

	private void StopMixAssistantLearnTimer(bool finalizeIfLearning)
	{
		if (_mixAssistantLearnTimer != null && _mixAssistantLearnTimer.IsRunning)
		{
			_mixAssistantLearnTimer.Stop();
		}
		if (finalizeIfLearning && _eqWorkbenchControl != null && _eqWorkbenchControl.MixAssistant.IsLearning)
		{
			MixAssistantProfile profile = _eqWorkbenchControl.MixAssistant.EndLearning();
			_eqWorkbenchControl.SetMixAssistantProfile(profile, autoApply: true);
		}
	}

	private async Task OnMixAssistantAnalyzeAudioRequestedAsync()
	{
		if (_eqWorkbenchControl == null || _isAnalyzingTrackForMixAssistant)
		{
			return;
		}

		string? filePath = ViewModel?.Engine?.CurrentTrack?.FilePath;
		if (string.IsNullOrWhiteSpace(filePath) || !System.IO.File.Exists(filePath))
		{
			_eqWorkbenchControl.SetMixAssistantProgress(false, 0.0, "请先加载或播放本地音轨后再进行分析");
			return;
		}

		StopMixAssistantLearnTimer(finalizeIfLearning: false);
		_isAnalyzingTrackForMixAssistant = true;
		_eqWorkbenchControl.SetMixAssistantProgress(false, 0.12, "正在后台解码并分析当前音轨频谱...");

		try
		{
			MixAssistantEngine assistant = _eqWorkbenchControl.MixAssistant;
			MixAssistantProfile? profile = await Task.Run(() =>
			{
				using var reader = new SonicWave.Audio.Decoders.SafeAudioFileReader(filePath);
				int sampleRate = Math.Max(8000, reader.WaveFormat.SampleRate);
				int channels = Math.Max(1, reader.WaveFormat.Channels);
				int maxFrames = Math.Min(sampleRate * 45, 2_000_000);
				int maxSamples = maxFrames * channels;
				float[] buffer = new float[maxSamples];
				int totalRead = 0;
				while (totalRead < maxSamples)
				{
					int read = reader.Read(buffer, totalRead, maxSamples - totalRead);
					if (read <= 0) break;
					totalRead += read;
				}

				int alignedSamples = totalRead - (totalRead % channels);
				if (alignedSamples <= 0)
				{
					return (MixAssistantProfile?)null;
				}

				float[] analyzedSamples = alignedSamples == buffer.Length ? buffer : buffer[..alignedSamples];
				return assistant.AnalyzeSamples(analyzedSamples, channels, sampleRate);
			});

			if (_eqWorkbenchControl == null) return;

			if (profile != null)
			{
				_eqWorkbenchControl.SetMixAssistantProfile(profile, autoApply: true);
			}
			else
			{
				_eqWorkbenchControl.SetMixAssistantProgress(false, 0.0, "未能从当前音轨读取有效的音频采样数据");
			}
		}
		catch (Exception ex)
		{
			_eqWorkbenchControl?.SetMixAssistantProgress(false, 0.0, $"音轨分析失败：{ex.Message}");
		}
		finally
		{
			_isAnalyzingTrackForMixAssistant = false;
		}
	}

	private async Task ImportAutoEqFileAsync()
	{
		try
		{
			if (ViewModel == null) return;

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
					_eqWorkbenchControl?.SetBands(ViewModel.EchoEqBands, ViewModel.EchoEqPreampDb, ViewModel.EchoEqEnabled);
					SyncLegacyFreeEqUiFromViewModel();
					await new ContentDialog
					{
						Title = "AutoEq 耳机校准曲线载入成功",
						Content = $"已从 {file.Name} 成功导入并解析 {ViewModel.EchoEqBands.Count} 段 ParametricEQ 滤波器（前级增益 {ViewModel.EchoEqPreampDb:F1} dB）。",
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

	private void ApplyEqBuiltinPreset(int presetIdx)
	{
		if (ViewModel == null) return;
		ViewModel.InitializeEchoEqBands();

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

		var updatedBands = new List<EchoBiquadFilter>(ViewModel.EchoEqBands.Count);
		for (int i = 0; i < ViewModel.EchoEqBands.Count; i++)
		{
			EchoBiquadFilter src = ViewModel.EchoEqBands[i];
			updatedBands.Add(new EchoBiquadFilter
			{
				FilterType = src.FilterType,
				FrequencyHz = src.FrequencyHz,
				GainDb = i < gains.Length ? gains[i] : src.GainDb,
				Q = src.Q,
				Enabled = true
			});
		}

		ViewModel.ApplyParametricEqState(updatedBands, ViewModel.EchoEqPreampDb, ViewModel.EchoEqEnabled, save: true);
		_eqWorkbenchControl?.SetBands(ViewModel.EchoEqBands, ViewModel.EchoEqPreampDb, ViewModel.EchoEqEnabled);
		SyncLegacyFreeEqUiFromViewModel();
	}
	public void SelectDspCategory(string category)
	{
		_currentDspCategory = category;

		// Highlight matching button in sidebar
		UpdateSidebarNavButtonColors();

		if (_workbenchHost == null) return;
		_workbenchHost.Children.Clear();
		FrameworkElement activeView = category switch
		{
			"listening_plan" or "listeningPlan" => _listeningPlanWorkbench ?? CreateListeningPlanWorkbench(),
			"param_eq" => _parametricEqWorkbench ?? CreateParametricEqWorkbench(),
			"headphone" => _headphoneCorrectionWorkbench ?? CreateHeadphoneCorrectionWorkbench(),
			"level_gain" => _volumeEqualizationWorkbench ?? CreateVolumeEqualizationWorkbench(),
			"headroom" => _headroomWorkbench ?? CreateHeadroomWorkbench(),
			"channel" => _channelToolWorkbench ?? CreateChannelToolWorkbench(),
			"room" or "convolution" => _roomCorrectionWorkbench ?? CreateRoomCorrectionWorkbench(),
			"crossfeed" => _crossfeedWorkbench ?? CreateCrossfeedWorkbench(),
			"virtual_stage" or "virtualStage" => _virtualStageWorkbench ?? CreateVirtualStageWorkbench(),
			"stereo_field" or "stereoField" => _stereoFieldWorkbench ?? CreateStereoFieldWorkbench(),
			"channel_matrix" or "channelMatrix" => _channelMatrixWorkbench ?? CreateChannelMatrixWorkbench(),
			"deesser" or "de_esser" => _deEsserWorkbench ?? CreateDeEsserWorkbench(),
			"limiter" => _limiterWorkbench ?? CreateLimiterWorkbench(),
			"spatial" => _virtualStageWorkbench ?? CreateVirtualStageWorkbench(),
			"compressor" or "dynamics" => _compressorWorkbench ?? CreateCompressorWorkbench(),
			"multiband" => _multibandCompressorWorkbench ?? CreateMultibandCompressorWorkbench(),
			"resampler" or "src" => _srcUpsamplingWorkbench ?? CreateSrcUpsamplingWorkbench(),
			"dsd" or "sdm" => _sdmDsdWorkbench ?? CreateSdmDsdWorkbench(),
			"safety" => _outputSafetyWorkbench ?? CreateOutputSafetyWorkbench(),
			"device" => _audioDeviceWorkbench ?? CreateAudioDeviceWorkbench(),
			"vst3" => _vst3RackWorkbench ?? CreateVst3RackWorkbench(),
			_ => _parametricEqWorkbench ?? CreateParametricEqWorkbench()
		};

		Grid.SetRow(activeView, 0);
		Grid.SetColumn(activeView, 0);
		_workbenchHost.Children.Add(activeView);

		bool isDark = IsDspDark;
		RefreshWorkbenchThemeByView(activeView, isDark);
		UpgradeAndRefreshWorkbenchVisuals(activeView, isDark);
		if (ReferenceEquals(activeView, _parametricEqWorkbench))
		{
			SyncParametricEqWorkbenchFromViewModel();
			StartEqSpectrumTimer();
		}
		else
		{
			StopEqSpectrumTimer();
			StopMixAssistantLearnTimer(finalizeIfLearning: true);
		}
		if (category == "vst3")
		{
			RefreshVst3RackUi();
		}
	}

	public void SetDspViewCategory(string category)
	{
		SelectDspCategory(category);
	}

	private FrameworkElement? _vst3WrappedWorkbench;

	private FrameworkElement CreateWrappedCard(string title, string glyph, Border? innerCard)
	{
		if (innerCard == null) return new Grid();
		if (ReferenceEquals(innerCard, Vst3Panel) && _vst3WrappedWorkbench != null)
		{
			return _vst3WrappedWorkbench;
		}
		try { PanelCanvas?.Children.Remove(innerCard); } catch { }
		DependencyObject? parent = innerCard.Parent ?? VisualTreeHelper.GetParent(innerCard);
		if (parent is Border parentBorder)
		{
			parentBorder.Child = null;
		}
		else if (parent is Panel parentPanel)
		{
			parentPanel.Children.Remove(innerCard);
		}
		innerCard.HorizontalAlignment = HorizontalAlignment.Stretch;
		innerCard.VerticalAlignment = VerticalAlignment.Stretch;
		innerCard.Width = double.NaN;
		innerCard.Height = double.NaN;
		innerCard.Background = CreateAcrylicCardBrush(0.88, 235);
		innerCard.BorderBrush = CreateLiquidGlassRimBrush();
		innerCard.BorderThickness = new Thickness(1);
		innerCard.CornerRadius = new CornerRadius(12);
		LiquidGlassEngine.AttachInteractiveLiquidGlass(innerCard, LiquidGlassVariant.Regular, isDark: IsDspDark, enablePointerTracking: true);

		var card = new Border
		{
			Background = new SolidColorBrush(Colors.Transparent),
			Padding = new Thickness(24, 20, 24, 20),
			Child = innerCard
		};
		if (ReferenceEquals(innerCard, Vst3Panel)) _vst3WrappedWorkbench = card;
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
				Vst3PluginsList.Height = double.NaN;
				Vst3PluginsList.MaxHeight = double.PositiveInfinity;
				Vst3PluginsList.MinHeight = 240;
				Vst3PluginsList.VerticalAlignment = VerticalAlignment.Stretch;
				Vst3PluginsList.Padding = new Thickness(0, 0, 16, 0);
				if (VisualTreeHelper.GetParent(Vst3PluginsList) is Grid parentGrid && parentGrid.RowDefinitions.Count > 0)
				{
					int row = Grid.GetRow(Vst3PluginsList);
					if (row >= 0 && row < parentGrid.RowDefinitions.Count)
					{
						parentGrid.RowDefinitions[row].Height = new GridLength(1.0, GridUnitType.Star);
					}
				}
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

		UpdateHeadroomTelemetry();
	}

	private void UpdateHeadroomTelemetry()
	{
		try
		{
			var session = SonicWave.Audio.AudioSession.Instance;
			var engine = ViewModel?.AudioEngine ?? ViewModel?.Engine;
			var track = session?.CurrentTrack ?? engine?.CurrentTrack;

			// 1. Current Track Path & Title
			if (track != null && !string.IsNullOrWhiteSpace(track.FilePath))
			{
				string fileName = System.IO.Path.GetFileName(track.FilePath);
				string title = !string.IsNullOrWhiteSpace(track.DisplayTitle)
					? track.DisplayTitle
					: (!string.IsNullOrWhiteSpace(track.Title) ? track.Title : fileName);

				if (_headroomStreamInfoTb != null)
				{
					_headroomStreamInfoTb.Text = $"{title}  ·  {fileName}";
				}
			}
			else
			{
				if (_headroomStreamInfoTb != null)
				{
					_headroomStreamInfoTb.Text = "等待当前音乐流数据";
				}
			}

			// 2. Playback state
			bool isPlaying = (session?.State == SonicWave.Audio.PlaybackState.Playing) ||
			                 (engine?.State == SonicWave.Audio.PlaybackState.Playing);

			if (!isPlaying)
			{
				UpdateHeadroomMeterRow(_headroomInMeterFill, _headroomInMeterValTb, -90.0);
				UpdateHeadroomMeterRow(_headroomOutMeterFill, _headroomOutMeterValTb, -90.0);
				UpdateHeadroomMeterRow(_headroomTpMeterFill, _headroomTpMeterValTb, -90.0);
				if (_headroomTruePeakTb != null) _headroomTruePeakTb.Text = "--";
				if (_headroomMarginTb != null) _headroomMarginTb.Text = "--";
				if (_headroomProtectStatusTb != null)
				{
					_headroomProtectStatusTb.Text = "待命";
					_headroomProtectStatusTb.Foreground = DspTextMutedBrush;
				}
				return;
			}

			// 3. Audio Meters from AudioSession.Instance or Engine
			var meters = session?.GetLiveAudioMeters() ?? engine?.GetLiveAudioMeters() ?? (-90.0, -90.0, Array.Empty<float>(), 0f, 1f, Array.Empty<float>());

			double inPeak = session?.LatestInputPeakDb ?? engine?.LatestInputPeakDb ?? meters.PeakDb;
			double outPeak = meters.PeakDb;
			double truePeak = session?.LatestTruePeakDb ?? engine?.LatestTruePeakDb ?? (outPeak > -60.0 ? outPeak + 0.3 : -90.0);

			if (inPeak <= -60.0 && outPeak > -60.0) inPeak = outPeak;

			// Update 3 dB Progress Bars (Fill width & text)
			UpdateHeadroomMeterRow(_headroomInMeterFill, _headroomInMeterValTb, inPeak);
			UpdateHeadroomMeterRow(_headroomOutMeterFill, _headroomOutMeterValTb, outPeak);
			UpdateHeadroomMeterRow(_headroomTpMeterFill, _headroomTpMeterValTb, truePeak);

			// 4. Overload / Clipping detection
			if (outPeak >= -0.05 || truePeak >= -0.05)
			{
				_headroomClipCount++;
				_headroomLastClipTime = DateTime.Now;
			}

			// 5. Margin & Average Margin
			double peakForMargin = Math.Max(outPeak, truePeak);
			double margin = peakForMargin > -60.0 ? -peakForMargin : 0.0;
			if (margin < 0.0) margin = 0.0;
			_headroomAvgMarginDb = _headroomAvgMarginDb <= 0.0 ? margin : (_headroomAvgMarginDb * 0.9 + margin * 0.1);

			bool headroomEnabled = ViewModel?.EchoHeadroomEnabled ?? true;

			// 6. Update Status Foldout Stats
			if (_headroomClipCountTb != null)
			{
				_headroomClipCountTb.Text = $"{_headroomClipCount} 次";
				_headroomClipCountTb.Foreground = _headroomClipCount > 0
					? new SolidColorBrush(Windows.UI.Color.FromArgb(255, 239, 68, 68))
					: new SolidColorBrush(Windows.UI.Color.FromArgb(255, 22, 163, 74));
			}

			if (_headroomLastClipTb != null)
			{
				_headroomLastClipTb.Text = _headroomLastClipTime.HasValue
					? _headroomLastClipTime.Value.ToString("HH:mm:ss")
					: "无记录";
			}

			if (_headroomTruePeakTb != null)
			{
				_headroomTruePeakTb.Text = truePeak > -60.0 ? $"{truePeak:F1} dBTP" : "--";
				_headroomTruePeakTb.Foreground = truePeak >= 0.0
					? new SolidColorBrush(Windows.UI.Color.FromArgb(255, 239, 68, 68))
					: (truePeak >= -1.0 ? new SolidColorBrush(Windows.UI.Color.FromArgb(255, 245, 158, 11)) : DspTextPrimaryBrush);
			}

			if (_headroomMarginTb != null)
			{
				_headroomMarginTb.Text = outPeak > -60.0 ? $"{_headroomAvgMarginDb:F1} dB" : "--";
				_headroomMarginTb.Foreground = _headroomAvgMarginDb < 1.0
					? new SolidColorBrush(Windows.UI.Color.FromArgb(255, 245, 158, 11))
					: DspTextPrimaryBrush;
			}

			if (_headroomProtectStatusTb != null)
			{
				if (!headroomEnabled)
				{
					_headroomProtectStatusTb.Text = "已旁路";
					_headroomProtectStatusTb.Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 245, 158, 11));
				}
				else if (_headroomClipCount > 0)
				{
					_headroomProtectStatusTb.Text = "检测到过载";
					_headroomProtectStatusTb.Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 239, 68, 68));
				}
				else
				{
					_headroomProtectStatusTb.Text = "运行正常";
					_headroomProtectStatusTb.Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 34, 197, 94));
				}
			}
		}
		catch
		{
		}
	}

	private void UpdateHeadroomMeterRow(Border? fillBar, TextBlock? valTb, double peakDb)
	{
		if (fillBar == null || valTb == null) return;

		if (peakDb <= -60.0)
		{
			fillBar.Width = 0;
			valTb.Text = "--";
			valTb.Foreground = DspTextMutedBrush;
			return;
		}

		valTb.Text = $"{peakDb:+0.0;-0.0;0.0} dB";

		// Scale from -24.0 dBFS to 0.0 dBFS
		double ratio = Math.Clamp((peakDb - (-24.0)) / 24.0, 0.0, 1.0);
		double containerW = (fillBar.Parent is FrameworkElement parent && parent.ActualWidth > 0)
			? parent.ActualWidth
			: 360.0;

		fillBar.Width = Math.Max(0, ratio * containerW);

		if (peakDb >= 0.0)
		{
			fillBar.Background = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 239, 68, 68)); // Red (Clip)
			valTb.Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 220, 38, 38));
		}
		else if (peakDb >= -3.0)
		{
			fillBar.Background = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 245, 158, 11)); // Amber (Hot)
			valTb.Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 217, 119, 6));
		}
		else
		{
			fillBar.Background = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 79, 70, 229)); // Indigo (Normal)
			valTb.Foreground = DspTextPrimaryBrush;
		}
	}

	private async void OnAutoEqImportClick(object sender, RoutedEventArgs e)
	{
		await ImportAutoEqFileAsync();
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
