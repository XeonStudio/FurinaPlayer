using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using FurinaPlayer.UI.Controls;
using FurinaPlayer.UI.ViewModels;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Markup;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using SkiaSharp.Views.Windows;
using FurinaPlayer.Audio.Output;
using FurinaPlayer.Core.Models;
using FurinaPlayer.Core.Services;
using WinRT;
using WinRT.Interop;
using Windows.Foundation;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.System;
using Windows.UI;

namespace FurinaPlayer.UI.Pages;

[WinRTRuntimeClassName("Microsoft.UI.Xaml.IUIElementOverrides")]
// [WinRTExposedType(typeof(SettingsPageWinRTTypeDetails))]
public sealed partial class SettingsPage : Page, IComponentConnector
{
	private bool _suppressMaterialChange;

	private bool _handlersAttached;

	private SKXamlCanvas? _ambientCausticCanvas;

	private Grid? _rootStageGrid;

	private ScrollViewer? _settingsScrollViewer;

	private Button? _fetchAlbumArtButton;
	private Button? _scanIntegrityButton;
	private ProgressBar? _integrityProgressBar;
	private TextBlock? _integrityStatusText;
	private StackPanel? _integrityContainer;

	private Button? _openCrashFolderButton;

	private Button? _addFolderButton;

	private Button? _removeFolderButton;

	private Button? _scanNowButton;

	private Button? _importLyricFontButton;

	private Button? _resetLyricFontButton;

	private Button? _pickWallpaperButton;

	private Button? _saveButton;

	private Button? _restoreDefaultsButton;

	private Button? _refreshDevicesButton;
	private bool _suppressOutputUiEvents;
	private bool _initialLoadedSetupDone;
	private bool _liquidGlassApplied;
	private bool _lastAppliedGlassIsDark;
	private string? _lastLoadedLanguage;

	private static readonly string[] SystemFontOptions = new string[35]
	{
		"系统默认", "微软雅黑", "微软雅黑 Light", "宋体", "黑体", "楷体", "仿宋", "隶书", "幼圆", "等线",
		"华文细黑", "华文楷体", "华文宋体", "华文仿宋", "华文琥珀", "华文彩云", "Arial", "Segoe UI", "Segoe UI Light", "Consolas",
		"Times New Roman", "Georgia", "Verdana", "Tahoma", "Courier New", "Comic Sans MS", "Impact", "Lucida Console", "Trebuchet MS", "Arial Black",
		"Garamond", "Palatino Linotype", "Book Antiqua", "Bodoni MT", "Century Gothic"
	};

	private static readonly (string Name, string Url)[] OssLibraries = new (string, string)[19]
	{
		(".NET 8", "https://github.com/dotnet/dotnet"),
		("Windows App SDK / WinUI 3", "https://github.com/microsoft/WindowsAppSDK"),
		("CommunityToolkit.Mvvm", "https://github.com/CommunityToolkit/dotnet"),
		("CommunityToolkit.WinUI", "https://github.com/CommunityToolkit/Windows"),
		("NAudio", "https://github.com/naudio/NAudio"),
		("LibVLC", "https://github.com/videolan/vlc"),
		("LibVLCSharp", "https://github.com/videolan/libvlcsharp"),
		("ATL (Audio Tools Library)", "https://github.com/mabdelouahab/atldotnet"),
		("MathNet.Numerics", "https://github.com/mathnet/mathnet-numerics"),
		("SkiaSharp", "https://github.com/mono/SkiaSharp"),
		("LiveChartsCore", "https://github.com/beto-rodriguez/LiveCharts2"),
		("EF Core / Microsoft.Data.Sqlite", "https://github.com/dotnet/efcore"),
		("FFmpeg（无损音频格式转换）", "https://ffmpeg.org/"),
		("LRCLIB 在线歌词 API", "https://lrclib.net/"),
		("网易云音乐歌词接口（非官方）", "https://music.163.com/"),
		("QQ 音乐元数据与封面接口（非官方）", "https://y.qq.com/"),
		("Apple Music / iTunes Search API", "https://itunes.apple.com/"),
		("VST3 SDK（Steinberg，接口规范参考）", "https://steinbergmedia.github.io/vst3_dev_portal/"),
		("VST3 插件模拟宿主（自研，尚未加载真实插件）", "https://github.com/steinbergmedia/vst3_public_sdk")
	};

	[GeneratedCode("Microsoft.UI.Xaml.Markup.Compiler", " 3.0.0.2409")]
	private TextBlock VersionText;

	[GeneratedCode("Microsoft.UI.Xaml.Markup.Compiler", " 3.0.0.2409")]
	private StackPanel OssList;

	[GeneratedCode("Microsoft.UI.Xaml.Markup.Compiler", " 3.0.0.2409")]
	private ComboBox ThemeCombo;

	[GeneratedCode("Microsoft.UI.Xaml.Markup.Compiler", " 3.0.0.2409")]
	private ComboBox MaterialCombo;

	[GeneratedCode("Microsoft.UI.Xaml.Markup.Compiler", " 3.0.0.2409")]
	private ComboBox WallpaperCombo;

	[GeneratedCode("Microsoft.UI.Xaml.Markup.Compiler", " 3.0.0.2409")]
	private StackPanel WallpaperLocalPanel;

	[GeneratedCode("Microsoft.UI.Xaml.Markup.Compiler", " 3.0.0.2409")]
	private Button LyricFontButton;

	[GeneratedCode("Microsoft.UI.Xaml.Markup.Compiler", " 3.0.0.2409")]
	private TextBlock LyricFontInfoText;

	[GeneratedCode("Microsoft.UI.Xaml.Markup.Compiler", " 3.0.0.2409")]
	private Flyout LyricFontFlyout;

	[GeneratedCode("Microsoft.UI.Xaml.Markup.Compiler", " 3.0.0.2409")]
	private ListView LyricFontList;

	[GeneratedCode("Microsoft.UI.Xaml.Markup.Compiler", " 3.0.0.2409")]
	private TextBlock LyricFontButtonText;

	[GeneratedCode("Microsoft.UI.Xaml.Markup.Compiler", " 3.0.0.2409")]
	private TextBlock WallpaperPathText;

	[GeneratedCode("Microsoft.UI.Xaml.Markup.Compiler", " 3.0.0.2409")]
	private ComboBox OutputModeCombo;

	[GeneratedCode("Microsoft.UI.Xaml.Markup.Compiler", " 3.0.0.2409")]
	private ComboBox WasapiDeviceCombo;

	[GeneratedCode("Microsoft.UI.Xaml.Markup.Compiler", " 3.0.0.2409")]
	private ComboBox AsioDeviceCombo;

	[GeneratedCode("Microsoft.UI.Xaml.Markup.Compiler", " 3.0.0.2409")]
	private bool _contentLoaded;

	public SettingsViewModel ViewModel { get; set; }

	public event Action<string[]>? ScanRequested;

	public event Action? SettingsSaved;

	public SettingsPage()
	{
		InitializeComponent();
		NavigationCacheMode = NavigationCacheMode.Required;
		Loaded += (_, _) => EnsureLibraryIntegrityUi();
		Loaded += OnLoaded;
		ActualThemeChanged += (s, e) => ApplySettingsLiquidGlass(force: true);
		SizeChanged += OnSizeChanged;
	}

	public SettingsPage(SettingsViewModel viewModel)
		: this()
	{
		ViewModel = viewModel;
		DataContext = viewModel;
	}

	private void OnLoaded(object sender, RoutedEventArgs e)
	{
		string curLang = Loc.CurrentLanguage;
		if (!_initialLoadedSetupDone || !string.Equals(_lastLoadedLanguage, curLang, StringComparison.Ordinal))
		{
			if (LyricFontButton != null)
			{
				LyricFontButton.Tag = "__fluent_upgraded";
				if (LyricFontButton.Content is DependencyObject dobj)
				{
					FluentIconFactory.UpgradeButtonsInVisualTree(dobj);
				}
			}
			FluentIconFactory.UpgradeButtonsInVisualTree(this);
			FurinaPlayer.UI.Helpers.UiLocalizer.LocalizeTree(this);
			_initialLoadedSetupDone = true;
			_lastLoadedLanguage = curLang;
		}
		ApplySettingsLiquidGlass(force: !_sidebarLayoutBuilt);
		UpdateResponsiveLayout(ActualWidth);
	}

	private void OnSizeChanged(object sender, SizeChangedEventArgs e)
	{
		_ambientCausticCanvas?.Invalidate();
		UpdateResponsiveLayout(e.NewSize.Width);
	}

	private ScrollViewer? ResolveSettingsScrollViewer()
	{
		if (_settingsScrollViewer != null)
		{
			return _settingsScrollViewer;
		}
		if (Content is ScrollViewer sv)
		{
			_settingsScrollViewer = sv;
			return sv;
		}
		if (Content is Grid rootGrid)
		{
			foreach (UIElement child in rootGrid.Children)
			{
				if (child is ScrollViewer innerSv)
				{
					_settingsScrollViewer = innerSv;
					return innerSv;
				}
			}
		}
		return null;
	}

	private Grid? ResolveSettingsGrid()
	{
		if (_innerSettingsGrid != null)
		{
			return _innerSettingsGrid;
		}
		ScrollViewer? sv = ResolveSettingsScrollViewer();
		if (sv?.Content is Grid g)
		{
			_innerSettingsGrid = g;
			return g;
		}
		return null;
	}

	private void ApplySettingsLiquidGlass(bool force = false)
	{
		try
		{
			bool isDark = LiquidGlassEngine.IsElementDarkTheme(this);
			if (!force && _liquidGlassApplied && _sidebarLayoutBuilt && _lastAppliedGlassIsDark == isDark)
			{
				return;
			}
			_lastAppliedGlassIsDark = isDark;
			Background = new SolidColorBrush(Colors.Transparent);

			if (_ambientCausticCanvas != null)
			{
				_ambientCausticCanvas.Visibility = Visibility.Collapsed;
			}

			ScrollViewer? targetSv = ResolveSettingsScrollViewer();
			if (targetSv != null)
			{
				targetSv.Background = new SolidColorBrush(Colors.Transparent);
			}

			Grid? settingsGrid = ResolveSettingsGrid();
			if (targetSv != null && settingsGrid != null && _outerSettingsContainer == null)
			{
				Thickness gridPad = settingsGrid.Padding;
				Thickness gridMargin = settingsGrid.Margin;
				double gridMaxW = settingsGrid.MaxWidth;
				settingsGrid.Padding = new Thickness(0);
				settingsGrid.Margin = new Thickness(0);
				settingsGrid.MaxWidth = double.PositiveInfinity;
				settingsGrid.HorizontalAlignment = HorizontalAlignment.Stretch;

				_outerSettingsContainer = new StackPanel
				{
					Spacing = 18,
					Padding = gridPad.Left > 0 ? gridPad : new Thickness(24, 16, 24, 28),
					Margin = gridMargin,
					MaxWidth = (!double.IsNaN(gridMaxW) && !double.IsInfinity(gridMaxW) && gridMaxW > 0) ? gridMaxW : 1280.0,
					HorizontalAlignment = HorizontalAlignment.Stretch,
					Background = new SolidColorBrush(Colors.Transparent)
				};

				targetSv.Content = null;
				_outerSettingsContainer.Children.Add(settingsGrid);
				targetSv.Content = _outerSettingsContainer;
			}

			if (_furinaPlayerAppearanceCard == null)
			{
				LiquidGlassEngine.UpgradeVisualTreeToLiquidGlass(this, isDark);
			}
			else
			{
				if (settingsGrid != null)
				{
					foreach (UIElement child in settingsGrid.Children)
					{
						if (child is Panel p)
						{
							foreach (UIElement sub in p.Children)
							{
								if (!ReferenceEquals(sub, _furinaPlayerAppearanceCard))
								{
									LiquidGlassEngine.UpgradeVisualTreeToLiquidGlass(sub, isDark);
								}
							}
						}
						else if (!ReferenceEquals(child, _furinaPlayerAppearanceCard))
						{
							LiquidGlassEngine.UpgradeVisualTreeToLiquidGlass(child, isDark);
						}
					}
				}
				if (_aboutCardBorder != null)
				{
					LiquidGlassEngine.UpgradeVisualTreeToLiquidGlass(_aboutCardBorder, isDark);
				}
			}

			if (settingsGrid != null)
			{
				settingsGrid.Background = new SolidColorBrush(Colors.Transparent);
				var panels = settingsGrid.Children.OfType<FrameworkElement>().Where(c => c is not SKXamlCanvas).ToList();
				if (panels.Count >= 2 && panels[1] is StackPanel col1Panel)
				{
					try
					{
						if (_systemSettingsCard == null)
						{
							_systemSettingsCard = CreateSystemAndWindowSettingsCard(isDark);
							col1Panel.Children.Insert(0, _systemSettingsCard);
						}
						else
						{
							LiquidGlassEngine.AttachInteractiveLiquidGlass(_systemSettingsCard, LiquidGlassVariant.Regular, isDark, enablePointerTracking: true);
						}
					}
					catch (Exception ex)
					{
						CrashReporter.LogInfo($"SettingsPage layout step failed (_systemSettingsCard): {ex.Message}");
					}

					// 隐藏原本旧的“外观与主题”卡片（插入系统设置后位于 index 1）
					if (col1Panel.Children.Count >= 2 && col1Panel.Children[1] is Border legacyAppearanceCard && legacyAppearanceCard != _furinaPlayerAppearanceCard)
					{
						legacyAppearanceCard.Visibility = Visibility.Collapsed;
					}

					try
					{
						if (_furinaPlayerAppearanceCard == null)
						{
							_furinaPlayerAppearanceCard = CreateFurinaPlayerAppearanceWorkbenchCard(isDark);
							// 插入到系统与窗口设置之后、音乐库之前
							int insertIdx = Math.Min(2, col1Panel.Children.Count);
							col1Panel.Children.Insert(insertIdx, _furinaPlayerAppearanceCard);
						}
						else
						{
							RefreshFurinaPlayerAppearanceWorkbenchVisuals(isDark);
						}
					}
					catch (Exception ex)
					{
						CrashReporter.LogInfo($"SettingsPage layout step failed (_furinaPlayerAppearanceCard): {ex.Message}");
					}

					try
					{
						if (_desktopPetCard == null)
						{
							_desktopPetCard = CreateDesktopPetCard(isDark);
							int insertIdx = Math.Min(3, col1Panel.Children.Count);
							col1Panel.Children.Insert(insertIdx, _desktopPetCard);
						}
						else
						{
							LiquidGlassEngine.AttachInteractiveLiquidGlass(_desktopPetCard, LiquidGlassVariant.Regular, isDark, enablePointerTracking: true);
							SyncDesktopPetUi();
						}
					}
					catch (Exception ex)
					{
						CrashReporter.LogInfo($"SettingsPage layout step failed (_desktopPetCard): {ex.Message}");
					}
				}
				if (panels.Count >= 1)
				{
					try
					{
						if (_audioCheckRepairCard == null)
						{
							_audioCheckRepairCard = CreateAudioCheckAndRepairCard(isDark);
							if (panels[0] is Panel col0Panel)
							{
								col0Panel.Children.Add(_audioCheckRepairCard);
							}
							else if (panels.Count >= 2 && panels[1] is Panel col1Fallback)
							{
								col1Fallback.Children.Insert(Math.Min(4, col1Fallback.Children.Count), _audioCheckRepairCard);
							}
						}
						else
						{
							LiquidGlassEngine.AttachInteractiveLiquidGlass(_audioCheckRepairCard, LiquidGlassVariant.Regular, isDark, enablePointerTracking: true);
							RefreshCheckRepairTheme(isDark);
						}
					}
					catch (Exception ex)
					{
						CrashReporter.LogInfo($"SettingsPage layout step failed (_audioCheckRepairCard): {ex.Message}");
					}
				}
				foreach (UIElement child in settingsGrid.Children)
				{
					if (child is Border cardBorder)
					{
						if (cardBorder.CornerRadius == default)
						{
							cardBorder.CornerRadius = new CornerRadius(16);
						}
						LiquidGlassEngine.AttachInteractiveLiquidGlass(cardBorder, LiquidGlassVariant.Regular, isDark, enablePointerTracking: true);
					}
					else if (child is LiquidGlassBorder lgBorder)
					{
						lgBorder.Variant = LiquidGlassVariant.Regular;
						lgBorder.GlassOpacity = 0.84;
						lgBorder.HighlightOpacity = 0.72;
					}
					else if (child is Panel columnPanel)
					{
						foreach (UIElement subChild in columnPanel.Children)
						{
							if (subChild is Border subCardBorder)
							{
								if (subCardBorder.CornerRadius == default)
								{
									subCardBorder.CornerRadius = new CornerRadius(16);
								}
								LiquidGlassEngine.AttachInteractiveLiquidGlass(subCardBorder, LiquidGlassVariant.Regular, isDark, enablePointerTracking: true);
							}
							else if (subChild is LiquidGlassBorder subLgBorder)
							{
								subLgBorder.Variant = LiquidGlassVariant.Regular;
								subLgBorder.GlassOpacity = 0.84;
								subLgBorder.HighlightOpacity = 0.72;
							}
						}
					}
				}
			}

			try
			{
				EnsureAboutCardAtEnd(settingsGrid, isDark);
			}
			catch (Exception ex)
			{
				CrashReporter.LogInfo($"SettingsPage layout step failed (EnsureAboutCardAtEnd): {ex.Message}");
			}
			try
			{
				EnsureSidebarQuickJumpLayout(targetSv, settingsGrid, isDark);
			}
			catch (Exception ex)
			{
				CrashReporter.LogInfo($"SettingsPage layout step failed (EnsureSidebarQuickJumpLayout): {ex.Message}");
			}
			_liquidGlassApplied = _sidebarLayoutBuilt;

			ComboBox?[] combos = new ComboBox?[]
			{
				ThemeCombo,
				MaterialCombo,
				WallpaperCombo,
				OutputModeCombo,
				WasapiDeviceCombo,
				AsioDeviceCombo
			};
			foreach (ComboBox? combo in combos)
			{
				if (combo != null)
				{
					if (combo.CornerRadius == default)
					{
						combo.CornerRadius = new CornerRadius(10);
					}
					LiquidGlassEngine.AttachInteractiveLiquidGlass(combo, LiquidGlassVariant.Clear, isDark, enablePointerTracking: true);
				}
			}

			Button?[] primaryButtons = new Button?[]
			{
				_saveButton,
				_scanNowButton,
				_fetchAlbumArtButton
			};
			foreach (Button? btn in primaryButtons)
			{
				if (btn != null)
				{
					if (btn.CornerRadius == default)
					{
						btn.CornerRadius = new CornerRadius(10);
					}
					LiquidGlassEngine.AttachInteractiveLiquidGlass(btn, LiquidGlassVariant.AccentLensed, isDark, enablePointerTracking: true);
				}
			}

			Button?[] secondaryButtons = new Button?[]
			{
				LyricFontButton,
				_openCrashFolderButton,
				_addFolderButton,
				_removeFolderButton,
				_importLyricFontButton,
				_resetLyricFontButton,
				_pickWallpaperButton,
				_restoreDefaultsButton,
				_refreshDevicesButton
			};
			foreach (Button? btn in secondaryButtons)
			{
				if (btn != null)
				{
					if (btn.CornerRadius == default)
					{
						btn.CornerRadius = new CornerRadius(10);
					}
					LiquidGlassEngine.AttachInteractiveLiquidGlass(btn, LiquidGlassVariant.Clear, isDark, enablePointerTracking: true);
				}
			}

			UpgradeSettingsVisualTreeNodes(this, isDark);
		}
		catch (Exception ex)
		{
			CrashReporter.LogInfo($"ApplySettingsLiquidGlass failed: {ex.Message}");
		}
	}

	private void UpgradeSettingsVisualTreeNodes(DependencyObject root, bool isDark)
	{
		if (root == null || ReferenceEquals(root, _furinaPlayerAppearanceCard)) return;

		int count = VisualTreeHelper.GetChildrenCount(root);
		for (int i = 0; i < count; i++)
		{
			DependencyObject child = VisualTreeHelper.GetChild(root, i);
			if (ReferenceEquals(child, _furinaPlayerAppearanceCard)) continue;
			if (child is LiquidGlassBorder lgBorder)
			{
				lgBorder.Variant = LiquidGlassVariant.Regular;
				lgBorder.GlassOpacity = 0.84;
				lgBorder.HighlightOpacity = 0.72;
			}
			else if (child is ComboBox combo)
			{
				if (combo.CornerRadius == default)
				{
					combo.CornerRadius = new CornerRadius(10);
				}
				LiquidGlassEngine.AttachInteractiveLiquidGlass(combo, LiquidGlassVariant.Clear, isDark, enablePointerTracking: true);
			}
			else if (child is ToggleSwitch toggle)
			{
				LiquidGlassEngine.AttachInteractiveLiquidGlass(toggle, LiquidGlassVariant.Clear, isDark, enablePointerTracking: true);
			}
			else if (child is Button btn && btn != _saveButton && btn != _scanNowButton && btn != _fetchAlbumArtButton)
			{
				if (btn.CornerRadius == default)
				{
					btn.CornerRadius = new CornerRadius(10);
				}
				LiquidGlassEngine.AttachInteractiveLiquidGlass(btn, LiquidGlassVariant.Clear, isDark, enablePointerTracking: true);
			}
			else if (child is Border border && border.CornerRadius.TopLeft >= 10 && border.ActualWidth > 60 && border.ActualHeight > 32)
			{
				var variant = border.CornerRadius.TopLeft >= 14 ? LiquidGlassVariant.Regular : LiquidGlassVariant.Clear;
				LiquidGlassEngine.AttachInteractiveLiquidGlass(border, variant, isDark, enablePointerTracking: true);
			}

			if (child is Button or ComboBox or ToggleSwitch or Slider or ProgressBar or TextBox or PasswordBox or CheckBox or RadioButton or ScrollBar)
			{
				continue;
			}

			UpgradeSettingsVisualTreeNodes(child, isDark);
		}
	}

	private void UpdateResponsiveLayout(double width)
	{
		try
		{
			if (_sidebarLayoutBuilt && _settingsTwoColGrid != null && _settingsTwoColGrid.ColumnDefinitions.Count >= 2)
			{
				_settingsTwoColGrid.ColumnDefinitions[0].Width = width < 820.0
					? new GridLength(190.0, GridUnitType.Pixel)
					: new GridLength(244.0, GridUnitType.Pixel);
				UpdatePresetGridColumns(Math.Max(420.0, width - 280.0));
				return;
			}
			Grid? settingsGrid = ResolveSettingsGrid();
			if (settingsGrid != null)
			{
				List<FrameworkElement> allPanels = settingsGrid.Children
					.OfType<FrameworkElement>()
					.Where(c => c is not SKXamlCanvas)
					.ToList();
				foreach (FrameworkElement p in allPanels)
				{
					if (p is Panel pnl)
					{
						pnl.Visibility = pnl.Children.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
					}
				}
				List<FrameworkElement> panels = allPanels
					.Where(c => c is not Panel pnl || pnl.Children.Count > 0)
					.ToList();

				if (panels.Count == 2)
				{
					FrameworkElement panel0 = panels[0];
					FrameworkElement panel1 = panels[1];
					if (width < 980.0)
					{
						settingsGrid.ColumnDefinitions.Clear();
						settingsGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.0, GridUnitType.Star) });
						settingsGrid.RowDefinitions.Clear();
						settingsGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
						settingsGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
						Grid.SetColumn(panel0, 0); Grid.SetRow(panel0, 0);
						Grid.SetColumn(panel1, 0); Grid.SetRow(panel1, 1);
					}
					else
					{
						settingsGrid.ColumnDefinitions.Clear();
						settingsGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(width >= 1320.0 ? 420.0 : 380.0, GridUnitType.Pixel) });
						settingsGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.0, GridUnitType.Star), MinWidth = 380.0 });
						settingsGrid.RowDefinitions.Clear();
						settingsGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
						Grid.SetColumn(panel0, 0); Grid.SetRow(panel0, 0);
						Grid.SetColumn(panel1, 1); Grid.SetRow(panel1, 0);
					}
				}
				else if (panels.Count >= 3)
				{
					FrameworkElement panel0 = panels[0];
					FrameworkElement panel1 = panels[1];
					FrameworkElement panel2 = panels[2];

					if (width < 980.0)
					{
						settingsGrid.ColumnDefinitions.Clear();
						settingsGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.0, GridUnitType.Star), MaxWidth = 720.0 });

						settingsGrid.RowDefinitions.Clear();
						settingsGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
						settingsGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
						settingsGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

						Grid.SetColumn(panel0, 0); Grid.SetRow(panel0, 0);
						Grid.SetColumn(panel1, 0); Grid.SetRow(panel1, 1);
						Grid.SetColumn(panel2, 0); Grid.SetRow(panel2, 2);
					}
					else if (width < 1320.0)
					{
						settingsGrid.ColumnDefinitions.Clear();
						settingsGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.0, GridUnitType.Star), MinWidth = 380.0 });
						settingsGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.0, GridUnitType.Star), MinWidth = 380.0 });

						settingsGrid.RowDefinitions.Clear();
						settingsGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
						settingsGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

						Grid.SetColumn(panel0, 0); Grid.SetRow(panel0, 0);
						Grid.SetColumn(panel1, 1); Grid.SetRow(panel1, 0);
						Grid.SetColumn(panel2, 0); Grid.SetRow(panel2, 1);
					}
					else
					{
						settingsGrid.ColumnDefinitions.Clear();
						settingsGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(400.0, GridUnitType.Pixel) });
						settingsGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.0, GridUnitType.Star), MinWidth = 360.0 });
						settingsGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.0, GridUnitType.Star), MinWidth = 360.0 });

						settingsGrid.RowDefinitions.Clear();
						settingsGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

						Grid.SetColumn(panel0, 0); Grid.SetRow(panel0, 0);
						Grid.SetColumn(panel1, 1); Grid.SetRow(panel1, 0);
						Grid.SetColumn(panel2, 2); Grid.SetRow(panel2, 0);
					}
				}
			}
			UpdatePresetGridColumns(width);
		}
		catch (Exception)
		{
		}
	}

	private void EnsureSidebarQuickJumpLayout(ScrollViewer? targetSv, Grid? settingsGrid, bool isDark)
	{
		if (targetSv == null || settingsGrid == null) return;

		if (_sidebarLayoutBuilt && _settingsSidebarBorder != null)
		{
			LiquidGlassEngine.AttachInteractiveLiquidGlass(_settingsSidebarBorder, LiquidGlassVariant.Regular, isDark, enablePointerTracking: true);
			foreach (var item in _sidebarNavItems)
			{
				if (item.Card is Border b &&
					!item.Title.Contains("音频", StringComparison.OrdinalIgnoreCase) &&
					!item.Title.Contains("Audio", StringComparison.OrdinalIgnoreCase))
				{
					LiquidGlassEngine.AttachInteractiveLiquidGlass(b, LiquidGlassVariant.Regular, isDark, enablePointerTracking: true);
				}
			}
			UpdateSidebarNavHighlight(_activeSidebarNavIndex, isDark);
			return;
		}

		var collectedCards = new List<FrameworkElement>();
		Panel? saveResetPanel = _saveButton?.Parent as Panel;

		var panels = settingsGrid.Children.OfType<FrameworkElement>().Where(c => c is not SKXamlCanvas).ToList();
		foreach (var panelElem in panels)
		{
			if (panelElem is Border directBorder)
			{
				if (directBorder.Visibility != Visibility.Collapsed)
				{
					collectedCards.Add(directBorder);
				}
			}
			else if (panelElem is Panel colPanel)
			{
				var children = colPanel.Children.ToList();
				foreach (var child in children)
				{
					if (child is Border card && card.Visibility != Visibility.Collapsed)
					{
						colPanel.Children.Remove(card);
						collectedCards.Add(card);
					}
				}
			}
		}

		foreach (var c in collectedCards)
		{
			if (settingsGrid.Children.Contains(c))
			{
				settingsGrid.Children.Remove(c);
			}
		}

		if (_aboutCardBorder != null)
		{
			_outerSettingsContainer?.Children.Remove(_aboutCardBorder);
			collectedCards.Remove(_aboutCardBorder);
		}
		_backupResetCard ??= CreateBackupAndResetCard(isDark);
		collectedCards.Remove(_backupResetCard);
		collectedCards.Add(_backupResetCard);
		if (_aboutCardBorder != null)
		{
			collectedCards.Add(_aboutCardBorder);
		}

		if (saveResetPanel != null && saveResetPanel.Parent is Panel oldParent)
		{
			oldParent.Children.Remove(saveResetPanel);
		}

		_cardsStackPanel = new StackPanel
		{
			Spacing = 18,
			Padding = new Thickness(4, 4, 20, 32),
			MaxWidth = 960,
			HorizontalAlignment = HorizontalAlignment.Stretch,
			Background = new SolidColorBrush(Colors.Transparent)
		};

		_sidebarNavItems.Clear();
		foreach (var card in collectedCards)
		{
			string cardTitle = ExtractCardSectionTitle(card);
			bool isAudioCard = cardTitle.Contains("音频", StringComparison.OrdinalIgnoreCase) || cardTitle.Contains("Audio", StringComparison.OrdinalIgnoreCase);
			if (card is Border b)
			{
				b.CornerRadius = new CornerRadius(16);
				b.Margin = new Thickness(0);
				b.HorizontalAlignment = HorizontalAlignment.Stretch;
				b.MaxWidth = double.PositiveInfinity;
				if (!isAudioCard)
				{
					if (b.Padding.Left < 16)
					{
						b.Padding = new Thickness(22, 18, 22, 18);
					}
					LiquidGlassEngine.AttachInteractiveLiquidGlass(b, LiquidGlassVariant.Regular, isDark, enablePointerTracking: true);
				}
			}
			_cardsStackPanel.Children.Add(card);
		}

		_rightCardsScrollViewer = new ScrollViewer
		{
			VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
			HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
			Padding = new Thickness(6, 12, 12, 16),
			Background = new SolidColorBrush(Colors.Transparent),
			Content = _cardsStackPanel
		};

		var sidebarGrid = new Grid { RowSpacing = 10 };
		sidebarGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
		sidebarGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
		sidebarGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

		var headerStack = new StackPanel { Spacing = 10 };
		headerStack.Children.Add(new TextBlock
		{
			Text = Loc.T("设置"),
			FontSize = 20,
			FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
			Margin = new Thickness(4, 0, 0, 0)
		});

		_sidebarSearchBox = new TextBox
		{
			PlaceholderText = Loc.T("搜索设置..."),
			CornerRadius = new CornerRadius(10),
			FontSize = 13
		};
		_sidebarSearchBox.TextChanged += (_, _) => FilterSettingsCardsByQuery(_sidebarSearchBox.Text);
		headerStack.Children.Add(_sidebarSearchBox);
		Grid.SetRow(headerStack, 0);
		sidebarGrid.Children.Add(headerStack);

		_sidebarNavStack = new StackPanel { Spacing = 4 };
		for (int i = 0; i < collectedCards.Count; i++)
		{
			var card = collectedCards[i];
			string title = ExtractCardSectionTitle(card);
			if (string.IsNullOrWhiteSpace(title)) continue;

			int capturedIdx = _sidebarNavItems.Count;
			if (title.Contains("音频", StringComparison.OrdinalIgnoreCase) || title.Contains("Audio", StringComparison.OrdinalIgnoreCase))
			{
				EnhanceAudioOutputCard(card);
			}

			var navBtn = new Button
			{
				HorizontalAlignment = HorizontalAlignment.Stretch,
				HorizontalContentAlignment = HorizontalAlignment.Left,
				CornerRadius = new CornerRadius(10),
				Padding = new Thickness(12, 10, 12, 10),
				BorderThickness = new Thickness(0),
				Content = new StackPanel
				{
					Orientation = Orientation.Horizontal,
					Spacing = 10,
					Children =
					{
						CreateSectionIcon(title, 18),
						new TextBlock
						{
							Text = title,
							FontSize = 15.0,
							FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
							VerticalAlignment = VerticalAlignment.Center,
							TextTrimming = TextTrimming.CharacterEllipsis
						}
					}
				}
			};
			navBtn.Click += (_, _) =>
			{
				UpdateSidebarNavHighlight(capturedIdx, LiquidGlassEngine.IsElementDarkTheme(this));
				if (card.Visibility != Visibility.Visible) card.Visibility = Visibility.Visible;
				card.StartBringIntoView(new BringIntoViewOptions
				{
					AnimationDesired = true,
					VerticalAlignmentRatio = 0.03
				});
			};
			_sidebarNavItems.Add((navBtn, card, title));
			_sidebarNavStack.Children.Add(navBtn);
		}

		var navScroll = new ScrollViewer
		{
			VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
			HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
			Content = _sidebarNavStack
		};
		Grid.SetRow(navScroll, 1);
		sidebarGrid.Children.Add(navScroll);

		if (saveResetPanel != null)
		{
			if (saveResetPanel is StackPanel sp)
			{
				sp.Orientation = Orientation.Vertical;
				sp.Spacing = 8;
				sp.HorizontalAlignment = HorizontalAlignment.Stretch;
			}
			foreach (var child in saveResetPanel.Children)
			{
				if (child is Button b)
				{
					b.HorizontalAlignment = HorizontalAlignment.Stretch;
					b.CornerRadius = new CornerRadius(10);
					LiquidGlassEngine.AttachInteractiveLiquidGlass(
						b,
						ReferenceEquals(b, _saveButton) ? LiquidGlassVariant.AccentLensed : LiquidGlassVariant.Regular,
						isDark,
						enablePointerTracking: true);
				}
			}
			saveResetPanel.Margin = new Thickness(0, 6, 0, 0);
			Grid.SetRow(saveResetPanel, 2);
			sidebarGrid.Children.Add(saveResetPanel);
		}

		_settingsSidebarBorder = new Border
		{
			CornerRadius = new CornerRadius(16),
			Padding = new Thickness(12, 14, 12, 14),
			Margin = new Thickness(20, 14, 10, 20),
			VerticalAlignment = VerticalAlignment.Stretch,
			Child = sidebarGrid
		};
		LiquidGlassEngine.AttachInteractiveLiquidGlass(_settingsSidebarBorder, LiquidGlassVariant.Regular, isDark, enablePointerTracking: true);

		_settingsTwoColGrid = new Grid
		{
			Background = new SolidColorBrush(Colors.Transparent)
		};
		_settingsTwoColGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(244.0, GridUnitType.Pixel) });
		_settingsTwoColGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.0, GridUnitType.Star) });

		Grid.SetColumn(_settingsSidebarBorder, 0);
		Grid.SetColumn(_rightCardsScrollViewer, 1);
		_settingsTwoColGrid.Children.Add(_settingsSidebarBorder);
		_settingsTwoColGrid.Children.Add(_rightCardsScrollViewer);

		if (ReferenceEquals(Content, targetSv))
		{
			Content = _settingsTwoColGrid;
		}
		else if (targetSv.Parent is Grid parentGrid)
		{
			int row = Grid.GetRow(targetSv);
			int col = Grid.GetColumn(targetSv);
			int rowSpan = Grid.GetRowSpan(targetSv);
			int colSpan = Grid.GetColumnSpan(targetSv);
			parentGrid.Children.Remove(targetSv);
			Grid.SetRow(_settingsTwoColGrid, row);
			Grid.SetColumn(_settingsTwoColGrid, col);
			Grid.SetRowSpan(_settingsTwoColGrid, rowSpan);
			Grid.SetColumnSpan(_settingsTwoColGrid, colSpan);
			parentGrid.Children.Add(_settingsTwoColGrid);
		}

		_sidebarLayoutBuilt = true;
		UpdateSidebarNavHighlight(0, isDark);
	}

	private void UpdateSidebarNavHighlight(int activeIndex, bool isDark)
	{
		_activeSidebarNavIndex = activeIndex;
		for (int i = 0; i < _sidebarNavItems.Count; i++)
		{
			var btn = _sidebarNavItems[i].NavBtn;
			bool isActive = i == activeIndex;
			btn.Background = isActive
				? new SolidColorBrush(isDark ? Color.FromArgb(62, 56, 189, 248) : Color.FromArgb(42, 37, 99, 235))
				: new SolidColorBrush(Colors.Transparent);
		}
	}

	private void FilterSettingsCardsByQuery(string? query)
	{
		string q = (query ?? string.Empty).Trim();
		bool hasQuery = q.Length > 0;
		foreach (var item in _sidebarNavItems)
		{
			bool match = !hasQuery || ElementContainsText(item.Card, q);
			item.Card.Visibility = match ? Visibility.Visible : Visibility.Collapsed;
			item.NavBtn.Visibility = match ? Visibility.Visible : Visibility.Collapsed;
		}
	}

	private static bool ElementContainsText(DependencyObject root, string query)
	{
		if (root is TextBlock tb && !string.IsNullOrEmpty(tb.Text) &&
			tb.Text.Contains(query, StringComparison.OrdinalIgnoreCase))
		{
			return true;
		}
		int count = VisualTreeHelper.GetChildrenCount(root);
		for (int i = 0; i < count; i++)
		{
			if (ElementContainsText(VisualTreeHelper.GetChild(root, i), query))
				return true;
		}
		return false;
	}

	private static string ExtractCardSectionTitle(DependencyObject root)
	{
		if (root is TextBlock tb && !string.IsNullOrWhiteSpace(tb.Text))
		{
			return tb.Text.Trim();
		}
		int count = VisualTreeHelper.GetChildrenCount(root);
		for (int i = 0; i < count; i++)
		{
			string found = ExtractCardSectionTitle(VisualTreeHelper.GetChild(root, i));
			if (!string.IsNullOrEmpty(found))
				return found;
		}
		return string.Empty;
	}

	private static UIElement CreateSectionIcon(string title, double size)
	{
		if (title.Contains("音频", StringComparison.OrdinalIgnoreCase) || title.Contains("Audio", StringComparison.OrdinalIgnoreCase))
		{
			string iconPath = Path.Combine(AppContext.BaseDirectory, "Assets", "Icons", "audio_output.png");
			if (File.Exists(iconPath))
			{
				return new Image
				{
					Width = size,
					Height = size,
					Stretch = Stretch.Uniform,
					VerticalAlignment = VerticalAlignment.Center,
					Source = new Microsoft.UI.Xaml.Media.Imaging.BitmapImage
					{
						DecodePixelType = Microsoft.UI.Xaml.Media.Imaging.DecodePixelType.Logical,
						DecodePixelWidth = Math.Max(32, (int)Math.Ceiling(size * 2)),
						UriSource = new Uri(iconPath)
					}
				};
			}
		}
		return FluentIconFactory.Create(GuessSectionIcon(title), size);
	}

	private void EnhanceAudioOutputCard(FrameworkElement card)
	{
		if (card is Border cardBorder)
		{
			cardBorder.BorderThickness = new Thickness(0);
			cardBorder.BorderBrush = new SolidColorBrush(Colors.Transparent);
			cardBorder.Background = new SolidColorBrush(Colors.Transparent);
			// 系统与窗口设置卡片外层 Padding=22 + 内层 StackPanel Padding=16 -> 总左缩进 38px
			cardBorder.Padding = new Thickness(38, 18, 22, 18);
			cardBorder.Margin = new Thickness(0);

			if (cardBorder.Child is Panel rootPanel)
			{
				foreach (var child in rootPanel.Children)
				{
					if (child is Border innerBorder && innerBorder.Child is Panel)
					{
						innerBorder.BorderThickness = new Thickness(0);
						innerBorder.BorderBrush = new SolidColorBrush(Colors.Transparent);
						innerBorder.Background = new SolidColorBrush(Colors.Transparent);
						innerBorder.Padding = new Thickness(0);
						innerBorder.Margin = new Thickness(0);
					}
				}
			}
		}

		TextBlock? headerTb = FindFirstTextBlock(card);
		if (headerTb != null && headerTb.Tag as string != "AudioHeaderEnhanced")
		{
			DependencyObject? rawParent = (headerTb.Parent as DependencyObject) ?? VisualTreeHelper.GetParent(headerTb);
			headerTb.Tag = "AudioHeaderEnhanced";
			headerTb.FontSize = 16;
			headerTb.FontWeight = Microsoft.UI.Text.FontWeights.SemiBold;
			headerTb.Margin = new Thickness(0);
			headerTb.VerticalAlignment = VerticalAlignment.Center;

			var headerRow = new StackPanel
			{
				Orientation = Orientation.Horizontal,
				Spacing = 8,
				Margin = new Thickness(0, 0, 0, 6)
			};

			if (rawParent is Panel parentPanel)
			{
				int idx = parentPanel.Children.IndexOf(headerTb);
				if (idx >= 0)
				{
					parentPanel.Children.RemoveAt(idx);
					headerRow.Children.Add(CreateSectionIcon(headerTb.Text ?? "音频输出", 20));
					headerRow.Children.Add(headerTb);
					parentPanel.Children.Insert(idx, headerRow);
				}
			}
			else if (rawParent is Border parentBorder)
			{
				parentBorder.Child = null;
				headerRow.Children.Add(CreateSectionIcon(headerTb.Text ?? "音频输出", 20));
				headerRow.Children.Add(headerTb);
				parentBorder.Child = headerRow;
			}
		}

		// 将音频输出相关偏好开关（独占回退共享、切歌淡入淡出）归类至“音频输出”栏目下
		if (card is Border audioCardBorder && audioCardBorder.Child is Panel audioRootPanel)
		{
			Panel? targetSp = audioRootPanel;
			if (audioRootPanel.Children.FirstOrDefault() is Border innerB && innerB.Child is Panel innerSp)
			{
				targetSp = innerSp;
			}
			if (targetSp != null && targetSp.Tag as string != "AudioExtraTogglesAdded")
			{
				targetSp.Tag = "AudioExtraTogglesAdded";
				var autoFallbackToggle = new ToggleSwitch
				{
					Header = Loc.T("音频独占播放不稳定自动切共享"),
					IsOn = ViewModel?.AutoFallbackExclusiveToShared ?? true,
					OnContent = Loc.T("开启"),
					OffContent = Loc.T("关闭"),
					Margin = new Thickness(0, 6, 0, 0)
				};
				autoFallbackToggle.Toggled += (_, _) => ViewModel?.SetAutoFallbackExclusiveToShared(autoFallbackToggle.IsOn);
				var autoFallbackDesc = new TextBlock
				{
					Text = Loc.T("当 WASAPI 独占模式初始化失败或设备异常中断时，自动回退至共享模式保证连续出声。"),
					FontSize = 11,
					Opacity = 0.6,
					Margin = new Thickness(0, -4, 0, 4),
					TextWrapping = TextWrapping.Wrap
				};

				var crossfadeToggle = new ToggleSwitch
				{
					Header = Loc.T("切换歌曲时淡入淡出"),
					IsOn = ViewModel?.CrossfadeEnabled ?? false,
					OnContent = Loc.T("开启"),
					OffContent = Loc.T("关闭")
				};
				crossfadeToggle.Toggled += (_, _) => ViewModel?.SetCrossfadeEnabled(crossfadeToggle.IsOn);
				var crossfadeDesc = new TextBlock
				{
					Text = Loc.T("切歌时进行平滑交叉淡入淡出过渡，消除曲目衔接突兀感。"),
					FontSize = 11,
					Opacity = 0.6,
					Margin = new Thickness(0, -4, 0, 4),
					TextWrapping = TextWrapping.Wrap
				};

				targetSp.Children.Add(autoFallbackToggle);
				targetSp.Children.Add(autoFallbackDesc);
				targetSp.Children.Add(crossfadeToggle);
				targetSp.Children.Add(crossfadeDesc);
			}
		}
	}
	private static TextBlock? FindFirstTextBlock(DependencyObject root)
	{
		if (root is TextBlock tb && !string.IsNullOrWhiteSpace(tb.Text))
			return tb;
		int count = VisualTreeHelper.GetChildrenCount(root);
		for (int i = 0; i < count; i++)
		{
			var found = FindFirstTextBlock(VisualTreeHelper.GetChild(root, i));
			if (found != null) return found;
		}
		return null;
	}
	private static FluentIconKind GuessSectionIcon(string title)
	{
		if (title.Contains("检查与修复", StringComparison.OrdinalIgnoreCase) || title.Contains("Check & Repair", StringComparison.OrdinalIgnoreCase) || title.Contains("Repair", StringComparison.OrdinalIgnoreCase))
			return FluentIconKind.DiagnosticLog;
		if (title.Contains("备份", StringComparison.OrdinalIgnoreCase) || title.Contains("Backup", StringComparison.OrdinalIgnoreCase))
			return FluentIconKind.RefreshReset;
		if (title.Contains("音频", StringComparison.OrdinalIgnoreCase) || title.Contains("Audio", StringComparison.OrdinalIgnoreCase))
			return FluentIconKind.Volume;
		if (title.Contains("系统", StringComparison.OrdinalIgnoreCase) || title.Contains("System", StringComparison.OrdinalIgnoreCase))
			return FluentIconKind.Settings;
		if (title.Contains("外观", StringComparison.OrdinalIgnoreCase) || title.Contains("Appearance", StringComparison.OrdinalIgnoreCase))
			return FluentIconKind.ModeClassic;
		if (title.Contains("宠物", StringComparison.OrdinalIgnoreCase) || title.Contains("看板娘", StringComparison.OrdinalIgnoreCase) || title.Contains("Pet", StringComparison.OrdinalIgnoreCase) || title.Contains("Mascot", StringComparison.OrdinalIgnoreCase))
			return FluentIconKind.MusicHero;
		if (title.Contains("音乐库", StringComparison.OrdinalIgnoreCase) || title.Contains("Library", StringComparison.OrdinalIgnoreCase))
			return FluentIconKind.FolderScan;
		if (title.Contains("歌词", StringComparison.OrdinalIgnoreCase) || title.Contains("Lyric", StringComparison.OrdinalIgnoreCase))
			return FluentIconKind.LyricsImport;
		if (title.Contains("关于", StringComparison.OrdinalIgnoreCase) || title.Contains("About", StringComparison.OrdinalIgnoreCase))
			return FluentIconKind.AppLogo;
		return FluentIconKind.Settings;
	}

	private void EnsureAboutCardAtEnd(Grid? settingsGrid, bool isDark)
	{
		if (settingsGrid == null) return;
		try
		{
			if (_outerSettingsContainer == null || settingsGrid == null || VersionText == null)
			{
				return;
			}
			if (_aboutCardBorder == null)
			{
				DependencyObject? cur = VersionText;
				while (cur != null)
				{
					DependencyObject? parent = VisualTreeHelper.GetParent(cur) ?? (cur as FrameworkElement)?.Parent;
					if (cur is Border candidateBorder && parent is Panel parentPanel &&
					    (ReferenceEquals(parentPanel, settingsGrid) || settingsGrid.Children.Contains(parentPanel)))
					{
						_aboutCardBorder = candidateBorder;
						parentPanel.Children.Remove(candidateBorder);
						if (!ReferenceEquals(parentPanel, settingsGrid) && parentPanel.Children.Count == 0)
						{
							parentPanel.Visibility = Visibility.Collapsed;
						}
						Grid.SetColumn(candidateBorder, 0);
						Grid.SetRow(candidateBorder, 0);
						Grid.SetColumnSpan(candidateBorder, 1);
						candidateBorder.HorizontalAlignment = HorizontalAlignment.Stretch;
						candidateBorder.Margin = new Thickness(0);
						_outerSettingsContainer.Children.Add(candidateBorder);
						UpdateResponsiveLayout(ActualWidth > 0 ? ActualWidth : 1200.0);
						break;
					}
					cur = parent;
				}
			}
			if (_aboutCardBorder != null)
			{
				if (_aboutCardBorder.CornerRadius == default)
				{
					_aboutCardBorder.CornerRadius = new CornerRadius(16);
				}
				LiquidGlassEngine.AttachInteractiveLiquidGlass(_aboutCardBorder, LiquidGlassVariant.Regular, isDark, enablePointerTracking: true);
			}
		}
		catch
		{
		}
	}

	private static BackgroundMaterial MaterialFromIndex(int idx)
	{
		if (idx >= 1)
		{
			if (idx == 2)
			{
				return BackgroundMaterial.LiquidGlassDwm;
			}
			return BackgroundMaterial.LiquidGlass;
		}
		return BackgroundMaterial.Mica;
	}

	private static int IndexFromMaterial(BackgroundMaterial m)
	{
		return m switch
		{
			BackgroundMaterial.LiquidGlassDwm => 2, 
			BackgroundMaterial.LiquidGlass => 1, 
			_ => 0, 
		};
	}

	private static string GetAppVersion()
	{
		try
		{
			Assembly? entryAsm = Assembly.GetEntryAssembly();
			string? buildMetaVersion = entryAsm?
				.GetCustomAttributes<AssemblyMetadataAttribute>()
				.FirstOrDefault(a => string.Equals(a.Key, "FurinaBuildVersion", StringComparison.Ordinal))?
				.Value;
			string rawVersion = !string.IsNullOrWhiteSpace(buildMetaVersion)
				? buildMetaVersion
				: (entryAsm?.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? "1.0.0.0.0000");
			if (string.IsNullOrWhiteSpace(rawVersion))
			{
				return "1.0.0.0.0000";
			}
			var match = System.Text.RegularExpressions.Regex.Match(
				rawVersion,
				@"^(.+?)[（(]第\s*(\d+)\s*次迭代\s*·\s*(\d+)\s*项新增功能\s*·\s*(\d+)\s*项修复\s*·\s*([\d-]+)\s*编译[）)]$");
			if (match.Success)
			{
				return Loc.F(
					"{0}（第 {1} 次迭代 · {2} 项新增功能 · {3} 项修复 · {4} 编译）",
					match.Groups[1].Value.Trim(),
					match.Groups[2].Value,
					match.Groups[3].Value,
					match.Groups[4].Value,
					match.Groups[5].Value);
			}
			return rawVersion;
		}
		catch
		{
			return "1.0.0.0.0000";
		}
	}

	protected override void OnNavigatedTo(NavigationEventArgs e)
	{
		base.OnNavigatedTo(e);
		if (e.Parameter is SettingsViewModel settingsViewModel)
		{
			ViewModel = settingsViewModel;
			DataContext = settingsViewModel;
			settingsViewModel.PropertyChanged -= OnSettingsViewModelPropertyChanged;
			settingsViewModel.PropertyChanged += OnSettingsViewModelPropertyChanged;
		}
		if (!_handlersAttached)
		{
			_handlersAttached = true;
			OutputModeCombo.SelectionChanged += (object _, SelectionChangedEventArgs _) =>
			{
				if (!_suppressOutputUiEvents) ViewModel.SetOutputMode((OutputMode)Math.Max(0, OutputModeCombo.SelectedIndex));
			};
			ThemeCombo.SelectionChanged += (object _, SelectionChangedEventArgs _) =>
			{
				ViewModel.SetTheme((ThemeMode)Math.Max(0, ThemeCombo.SelectedIndex));
			};
			MaterialCombo.SelectionChanged += (object _, SelectionChangedEventArgs _) =>
			{
				if (!_suppressMaterialChange)
				{
					ViewModel.SetMaterial(MaterialFromIndex(MaterialCombo.SelectedIndex));
				}
			};
			WallpaperCombo.SelectionChanged += (object _, SelectionChangedEventArgs _) =>
			{
				ViewModel.SetWallpaperSource((WallpaperSource)Math.Max(0, WallpaperCombo.SelectedIndex));
				WallpaperLocalPanel.Visibility = ((WallpaperCombo.SelectedIndex != 1) ? Visibility.Collapsed : Visibility.Visible);
			};
			WasapiDeviceCombo.SelectionChanged += (object _, SelectionChangedEventArgs _) =>
			{
				if (!_suppressOutputUiEvents && WasapiDeviceCombo.SelectedItem is ComboBoxItem { Tag: string tag })
				{
					ViewModel.SelectedWasapiDeviceId = tag;
				}
			};
			AsioDeviceCombo.SelectionChanged += (object _, SelectionChangedEventArgs _) =>
			{
				if (!_suppressOutputUiEvents && AsioDeviceCombo.SelectedItem is ComboBoxItem { Tag: string tag })
				{
					ViewModel.SelectedAsioDriver = tag;
				}
			};
		}
		ViewModel.Load();
		VersionText.Text = Loc.F("软件版本：{0}", GetAppVersion());
		UpdateAboutSection();
		_suppressOutputUiEvents = true;
		try { OutputModeCombo.SelectedIndex = (int)ViewModel.Settings.OutputMode; } finally { _suppressOutputUiEvents = false; }
		ThemeCombo.SelectedIndex = (int)ViewModel.Settings.Theme;
		_suppressMaterialChange = true;
		MaterialCombo.SelectedIndex = IndexFromMaterial(ViewModel.Settings.BackgroundMaterial);
		_suppressMaterialChange = false;
		WallpaperCombo.SelectedIndex = (int)ViewModel.Settings.WallpaperSource;
		WallpaperLocalPanel.Visibility = ((ViewModel.Settings.WallpaperSource != WallpaperSource.LocalImage) ? Visibility.Collapsed : Visibility.Visible);
		WallpaperPathText.Text = ViewModel.LocalWallpaperPath;
		InitLyricFontUi();
		PopulateOssList();
		RefreshDevicesAsync();
		if (_fetchAlbumArtButton != null && ViewModel != null)
		{
			_fetchAlbumArtButton.IsEnabled = ViewModel.CanFetchAlbumArt;
		}
		EnsureLibraryIntegrityUi();
		ApplySettingsLiquidGlass();
		ResumeCheckRepairOnNavigatedTo();
	}

	protected override void OnNavigatedFrom(NavigationEventArgs e)
	{
		base.OnNavigatedFrom(e);
		StopCheckRepairLiveTimer();
		if (ViewModel != null)
		{
			ViewModel.PropertyChanged -= OnSettingsViewModelPropertyChanged;
		}
	}

	private void OnSettingsViewModelPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
	{
		if (e.PropertyName == "CanFetchAlbumArt" || e.PropertyName == "IsFetchingAlbumArt" || e.PropertyName == "AlbumArtCooldownSeconds")
		{
			if (_fetchAlbumArtButton != null && ViewModel != null)
			{
				_fetchAlbumArtButton.IsEnabled = ViewModel.CanFetchAlbumArt;
			}
		}
		else if (e.PropertyName is nameof(SettingsViewModel.IsCheckingLibraryIntegrity)
		         or nameof(SettingsViewModel.LibraryIntegrityProgress)
		         or nameof(SettingsViewModel.LibraryIntegrityStatus))
		{
			SyncLibraryIntegrityUi();
		}
		else if (e.PropertyName is nameof(SettingsViewModel.PetEnabled)
		         or nameof(SettingsViewModel.PetScalePercent)
		         or nameof(SettingsViewModel.PetStatusText)
		         or nameof(SettingsViewModel.PetCustomIdleGifPath)
		         or nameof(SettingsViewModel.PetCustomGrooveGifPath)
		         or nameof(SettingsViewModel.PetCustomClickGifPath)
		         or nameof(SettingsViewModel.PetBubbleScalePercent)
		         or nameof(SettingsViewModel.PetBubbleStyle))
		{
			SyncDesktopPetUi();
		}
	}

	private void InitLyricFontUi()
	{
		if (!(LyricFontButton == null) && !(LyricFontList == null) && ViewModel != null)
		{
			List<string> list = new List<string>(SystemFontOptions);
			if (list.Count > 0)
			{
				list[0] = Loc.T("系统默认");
			}
			string lyricFontFilePath = ViewModel.LyricFontFilePath;
			string lyricFontFamily = ViewModel.LyricFontFamily;
			if (!string.IsNullOrWhiteSpace(lyricFontFilePath))
			{
				list.Insert(0, Loc.T("已导入自定义字体"));
				LyricFontInfoText.Text = Loc.F("已导入字体：{0}（优先生效）", Path.GetFileName(lyricFontFilePath));
			}
			else
			{
				LyricFontInfoText.Text = Loc.T("正在播放页歌词使用所选字体；导入的 .ttf/.otf 字体文件优先生效并复制到应用数据目录。");
			}
			LyricFontList.ItemsSource = list;
			TextBlock lyricFontButtonText = LyricFontButtonText;
			string text;
			if (string.IsNullOrWhiteSpace(lyricFontFilePath))
			{
				text = (string.IsNullOrWhiteSpace(lyricFontFamily) ? Loc.T("系统默认") : lyricFontFamily);
			}
			else
			{
				text = Loc.F("已导入：{0}", Path.GetFileName(lyricFontFilePath));
			}
			lyricFontButtonText.Text = text;
		}
	}

	private void OnLyricFontButtonClick(object sender, RoutedEventArgs e)
	{
		if (LyricFontFlyout != null)
		{
			LyricFontFlyout.ShowAt(LyricFontButton);
		}
	}

	private void OnLyricFontItemClick(object sender, ItemClickEventArgs e)
	{
		if (ViewModel != null && e.ClickedItem is string text)
		{
			if (string.Equals(text, "系统默认", StringComparison.Ordinal) || string.Equals(text, Loc.T("系统默认"), StringComparison.Ordinal) || string.Equals(text, "已导入自定义字体", StringComparison.Ordinal) || string.Equals(text, Loc.T("已导入自定义字体"), StringComparison.Ordinal))
			{
				ViewModel.SetLyricFont("", ViewModel.LyricFontFilePath);
			}
			else
			{
				ViewModel.SetLyricFont(text, ViewModel.LyricFontFilePath);
			}
			LyricFontFlyout?.Hide();
			InitLyricFontUi();
		}
	}

	private async void OnImportLyricFontClick(object sender, RoutedEventArgs e)
	{
		try
		{
			FileOpenPicker fileOpenPicker = new FileOpenPicker
			{
				FileTypeFilter = { ".ttf", ".otf" }
			};
			nint windowHandle = WindowNative.GetWindowHandle(WindowManager.CurrentWindow);
			InitializeWithWindow.Initialize(fileOpenPicker, windowHandle);
			StorageFile storageFile = await fileOpenPicker.PickSingleFileAsync();
			if (!(storageFile == null))
			{
				string text = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "FurinaPlayer", "fonts");
				Directory.CreateDirectory(text);
				string text2 = Path.Combine(text, Path.GetFileName(storageFile.Path));
				File.Copy(storageFile.Path, text2, overwrite: true);
				ViewModel.SetLyricFont(ViewModel.LyricFontFamily, text2);
				InitLyricFontUi();
			}
		}
		catch (Exception ex)
		{
			if (LyricFontInfoText != null)
			{
				LyricFontInfoText.Text = Loc.F("导入字体失败：{0}", ex.Message);
			}
		}
	}

	private void OnResetLyricFontClick(object sender, RoutedEventArgs e)
	{
		ViewModel.ResetLyricFont();
		InitLyricFontUi();
	}

	private void UpdateAboutSection()
	{
		if (VersionText?.Parent is not Panel parentPanel) return;
		foreach (UIElement child in parentPanel.Children)
		{
			if (child is not TextBlock tb || ReferenceEquals(tb, VersionText)) continue;
			string t = tb.Text ?? string.Empty;
			if (t.StartsWith("WinUI 3", StringComparison.Ordinal))
			{
				tb.Text = Loc.T("WinUI 3 · .NET 8 · LibVLC（原生解码）· NAudio（WASAPI/ASIO 实时 DSP）· ATL · SkiaSharp · FFmpeg · OPRA 600");
			}
			else if (t.Contains("LibVLC", StringComparison.Ordinal) && t.Contains("DSP", StringComparison.Ordinal))
			{
				tb.Text = Loc.T("均衡器与实时 DSP 采用独立实现的公开技术路线：含 OPRA 600 耳机知识库与 Mix Assistant V2 智能混音平衡、WSOLA 无失真变速变调、空间声场工作台、8192 点 Insight Pro 频谱分析、32 种实时音效、纯音听力检测与 EQ 补偿联动、音频检查与修复助手、ECHO 风格首页/工作台、动态壁纸及支持全系统与自定义字体的芙宁娜桌面宠物。");
			}
		}
	}

	private void PopulateOssList()
	{
		if (OssList.Children.Count <= 0)
		{
			(string, string)[] ossLibraries = OssLibraries;
			for (int i = 0; i < ossLibraries.Length; i++)
			{
				(string, string) tuple = ossLibraries[i];
				string item = tuple.Item1;
				string item2 = tuple.Item2;
				HyperlinkButton hyperlinkButton = new HyperlinkButton
				{
					Content = Loc.T(item),
					Tag = item2,
					FontSize = 12.0
				};
				hyperlinkButton.Click += OnOpenLinkClick;
				OssList.Children.Add(hyperlinkButton);
			}
		}
	}

	private async void OnOpenLinkClick(object sender, RoutedEventArgs e)
	{
		if (sender is HyperlinkButton { Tag: string tag })
		{
			try
			{
				await Launcher.LaunchUriAsync(new Uri(tag));
			}
			catch
			{
			}
		}
	}

	private async void OnPickWallpaperClick(object sender, RoutedEventArgs e)
	{
		try
		{
			FileOpenPicker fileOpenPicker = new FileOpenPicker();
			string[] array = new string[6] { ".jpg", ".jpeg", ".png", ".bmp", ".webp", ".gif" };
			foreach (string item in array)
			{
				fileOpenPicker.FileTypeFilter.Add(item);
			}
			nint windowHandle = WindowNative.GetWindowHandle(WindowManager.CurrentWindow);
			InitializeWithWindow.Initialize(fileOpenPicker, windowHandle);
			StorageFile storageFile = await fileOpenPicker.PickSingleFileAsync();
			if (storageFile != null)
			{
				ViewModel.SetLocalWallpaperPath(storageFile.Path);
				WallpaperPathText.Text = storageFile.Path;
				SettingsSaved?.Invoke();
			}
		}
		catch (Exception ex)
		{
			WallpaperPathText.Text = Loc.F("选择失败：{0}", ex.Message);
		}
	}

	private void RefreshDevices()
	{
		ViewModel.RefreshDevicesCommand.Execute(null);
		SyncDeviceComboBoxesFromViewModel();
	}

	private async void RefreshDevicesAsync()
	{
		try
		{
			if (ViewModel == null) return;
			await ViewModel.RefreshDevicesAsync();
			SyncDeviceComboBoxesFromViewModel();
		}
		catch
		{
		}
	}

	private void SyncDeviceComboBoxesFromViewModel()
	{
		_suppressOutputUiEvents = true;
		try
		{
		WasapiDeviceCombo.Items.Clear();
		foreach (AudioDeviceInfo wasapiDevice in ViewModel.WasapiDevices)
		{
			ComboBoxItem item = new ComboBoxItem
			{
				Content = (wasapiDevice.IsDefault ? Loc.F("{0}（默认）", wasapiDevice.Name) : wasapiDevice.Name),
				Tag = wasapiDevice.Id
			};
			WasapiDeviceCombo.Items.Add(item);
		}
		AsioDeviceCombo.Items.Clear();
		foreach (AudioDeviceInfo asioDevice in ViewModel.AsioDevices)
		{
			ComboBoxItem item2 = new ComboBoxItem
			{
				Content = asioDevice.Name,
				Tag = asioDevice.Id
			};
			AsioDeviceCombo.Items.Add(item2);
		}
		if (WasapiDeviceCombo.Items.Count > 0)
		{
			WasapiDeviceCombo.SelectedIndex = Math.Max(0, ViewModel.WasapiDevices.ToList().FindIndex((AudioDeviceInfo x) => x.Id == ViewModel.SelectedWasapiDeviceId));
		}
		if (AsioDeviceCombo.Items.Count > 0)
		{
			AsioDeviceCombo.SelectedIndex = Math.Max(0, ViewModel.AsioDevices.ToList().FindIndex((AudioDeviceInfo x) => x.Id == ViewModel.SelectedAsioDriver));
		}
		}
		finally { _suppressOutputUiEvents = false; }
	}

	private void OnRefreshDevicesClick(object sender, RoutedEventArgs e)
	{
		RefreshDevices();
	}

	private async void OnAddFolderClick(object sender, RoutedEventArgs e)
	{
		FolderPicker folderPicker = new FolderPicker
		{
			FileTypeFilter = { "*" }
		};
		nint windowHandle = WindowNative.GetWindowHandle(WindowManager.CurrentWindow);
		InitializeWithWindow.Initialize(folderPicker, windowHandle);
		StorageFolder storageFolder = await folderPicker.PickSingleFolderAsync();
		if (storageFolder != null)
		{
			ViewModel.AddMusicFolder(storageFolder.Path);
		}
	}

	private void OnRemoveFolderClick(object sender, RoutedEventArgs e)
	{
		string text = ViewModel.MusicFoldersText.Split('\n').FirstOrDefault((string f) => !string.IsNullOrWhiteSpace(f));
		if (text != null)
		{
			ViewModel.RemoveMusicFolder(text.Trim());
		}
	}

	private void OnScanNowClick(object sender, RoutedEventArgs e)
	{
		ScanRequested?.Invoke(ViewModel.Settings.MusicFolders.ToArray());
	}

	private async void OnRestoreDefaultsClick(object sender, RoutedEventArgs e)
	{
		if (await new ContentDialog
		{
			Title = Loc.T("恢复默认设置"),
			Content = Loc.T("将重置音量、均衡器、空间音频、音色、主题、材质、壁纸、输出设备、歌词字体、VST3 插件配置等所有自定义参数为出厂默认值（类似清洁安装）。\n此操作保留已导入的音乐与专辑，不会删除音乐库与本地文件，是否继续？"),
			PrimaryButtonText = Loc.T("恢复"),
			CloseButtonText = Loc.T("取消"),
			DefaultButton = ContentDialogButton.Close,
			XamlRoot = XamlRoot
		}.ShowAsync() == ContentDialogResult.Primary)
		{
			ViewModel.RestoreDefaultsCommand.Execute(null);
			SettingsSaved?.Invoke();
			_suppressOutputUiEvents = true;
		try { OutputModeCombo.SelectedIndex = (int)ViewModel.Settings.OutputMode; } finally { _suppressOutputUiEvents = false; }
			ThemeCombo.SelectedIndex = (int)ViewModel.Settings.Theme;
			_suppressMaterialChange = true;
			MaterialCombo.SelectedIndex = IndexFromMaterial(ViewModel.Settings.BackgroundMaterial);
			_suppressMaterialChange = false;
			WallpaperCombo.SelectedIndex = (int)ViewModel.Settings.WallpaperSource;
			WallpaperLocalPanel.Visibility = ((ViewModel.Settings.WallpaperSource != WallpaperSource.LocalImage) ? Visibility.Collapsed : Visibility.Visible);
			WallpaperPathText.Text = ViewModel.LocalWallpaperPath;
			RefreshDevices();
			RefreshFurinaPlayerAppearanceWorkbenchVisuals(LiquidGlassEngine.IsElementDarkTheme(this));
		}
	}

	private void OnSaveClick(object sender, RoutedEventArgs e)
	{
		ViewModel.SaveCommand.Execute(null);
		SettingsSaved?.Invoke();
	}

	private void EnsureLibraryIntegrityUi()
	{
		if (_integrityContainer != null || _fetchAlbumArtButton == null)
		{
			SyncLibraryIntegrityUi();
			return;
		}

		// 沿视觉树向上查找包含 _fetchAlbumArtButton 的宿主 Panel（兼容 Border / ContentPresenter 包装或 Loaded 前状态）
		Panel? hostPanel = null;
		DependencyObject? cur = _fetchAlbumArtButton;
		DependencyObject? directChildInPanel = _fetchAlbumArtButton;
		while (cur != null)
		{
			var p = VisualTreeHelper.GetParent(cur) ?? (cur as FrameworkElement)?.Parent;
			if (p is StackPanel sp && sp.Orientation == Orientation.Vertical)
			{
				hostPanel = sp;
				directChildInPanel = cur;
				break;
			}
			if (p is Panel fallbackPanel && hostPanel == null)
			{
				hostPanel = fallbackPanel;
				directChildInPanel = cur;
			}
			cur = p;
		}

		if (hostPanel == null)
		{
			return;
		}

		bool isDark = LiquidGlassEngine.IsElementDarkTheme(this);

		_scanIntegrityButton = new Button
		{
			CornerRadius = new CornerRadius(10),
			Padding = new Thickness(12, 7, 12, 7),
			Content = BuildIntegrityButtonContent(false)
		};
		_scanIntegrityButton.Click += OnScanLibraryIntegrityClick;
		LiquidGlassEngine.AttachInteractiveLiquidGlass(_scanIntegrityButton, LiquidGlassVariant.AccentLensed, isDark, enablePointerTracking: true);

		_integrityProgressBar = new ProgressBar
		{
			Minimum = 0,
			Maximum = 100,
			Value = 0,
			Height = 5,
			CornerRadius = new CornerRadius(3),
			Visibility = Visibility.Collapsed
		};

		_integrityStatusText = new TextBlock
		{
			Text = ViewModel?.LibraryIntegrityStatus ?? Loc.T("尚未扫描（多线程并行检查元数据、内嵌歌词与专辑封面）"),
			FontSize = 12,
			Opacity = 0.78,
			TextWrapping = TextWrapping.Wrap
		};

		_integrityContainer = new StackPanel
		{
			Spacing = 8,
			Margin = new Thickness(0, 8, 0, 0)
		};
		var libActionRow = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
		libActionRow.Children.Add(_scanIntegrityButton);

		var clearCacheBtn = new Button
		{
			CornerRadius = new CornerRadius(10),
			Padding = new Thickness(12, 7, 12, 7),
			Content = Loc.T("清空曲库缓存")
		};
		clearCacheBtn.Click += OnClearLibraryCacheClick;
		LiquidGlassEngine.AttachInteractiveLiquidGlass(clearCacheBtn, LiquidGlassVariant.Regular, isDark, enablePointerTracking: true);
		libActionRow.Children.Add(clearCacheBtn);

		_integrityContainer.Children.Add(libActionRow);
		_integrityContainer.Children.Add(_integrityProgressBar);
		_integrityContainer.Children.Add(_integrityStatusText);

		int insertIdx = directChildInPanel is UIElement uie ? hostPanel.Children.IndexOf(uie) : -1;
		if (insertIdx >= 0 && insertIdx + 2 <= hostPanel.Children.Count)
		{
			hostPanel.Children.Insert(insertIdx + 2, _integrityContainer);
		}
		else
		{
			hostPanel.Children.Add(_integrityContainer);
		}

		SyncLibraryIntegrityUi();
	}

	private static StackPanel BuildIntegrityButtonContent(bool isRunning)
	{
		var sp = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
		sp.Children.Add(FluentIconFactory.Create(isRunning ? FluentIconKind.Stop : FluentIconKind.Search, 16));
		sp.Children.Add(new TextBlock
		{
			Text = isRunning ? Loc.T("停止完整性扫描") : Loc.T("扫描曲库文件完整性并补齐"),
			VerticalAlignment = VerticalAlignment.Center
		});
		return sp;
	}

	private void SyncLibraryIntegrityUi()
	{
		if (ViewModel == null) return;
		if (_scanIntegrityButton != null)
		{
			_scanIntegrityButton.Content = BuildIntegrityButtonContent(ViewModel.IsCheckingLibraryIntegrity);
		}
		if (_integrityProgressBar != null)
		{
			_integrityProgressBar.Visibility = (ViewModel.IsCheckingLibraryIntegrity || ViewModel.LibraryIntegrityProgress > 0)
				? Visibility.Visible
				: Visibility.Collapsed;
			_integrityProgressBar.Value = ViewModel.LibraryIntegrityProgress;
		}
		if (_integrityStatusText != null)
		{
			_integrityStatusText.Text = ViewModel.LibraryIntegrityStatus;
		}
	}

	private async void OnScanLibraryIntegrityClick(object sender, RoutedEventArgs e)
	{
		if (ViewModel == null) return;
		if (ViewModel.IsCheckingLibraryIntegrity)
		{
			ViewModel.CancelLibraryIntegrityScan();
			return;
		}
		await ViewModel.ScanLibraryIntegrityAsync();
	}
	private async void OnFetchAlbumArtClick(object sender, RoutedEventArgs e)
	{
		if (ViewModel == null || !ViewModel.CanFetchAlbumArt)
		{
			return;
		}
		if (_fetchAlbumArtButton != null)
		{
			_fetchAlbumArtButton.IsEnabled = false;
		}
		try
		{
			await ViewModel.FetchMissingAlbumCoverAsync();
		}
		catch (Exception ex)
		{
			ViewModel.AlbumArtStatus = Loc.F("操作异常：{0}", ex.Message);
		}
		finally
		{
			if (_fetchAlbumArtButton != null && ViewModel != null)
			{
				_fetchAlbumArtButton.IsEnabled = ViewModel.CanFetchAlbumArt;
			}
		}
	}

	private void OnOpenCrashFolderClick(object sender, RoutedEventArgs e)
	{
		try
		{
			string text = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "FurinaPlayer", "CrashReports");
			Directory.CreateDirectory(text);
			Process.Start(new ProcessStartInfo("explorer.exe", text)
			{
				UseShellExecute = true
			});
		}
		catch
		{
		}
	}

	[GeneratedCode("Microsoft.UI.Xaml.Markup.Compiler", " 3.0.0.2409")]
	[DebuggerNonUserCode]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("ms-appx:///FurinaPlayer.UI/Pages/SettingsPage.xaml");
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
			VersionText = target.As<TextBlock>();
			break;
		case 3:
			_openCrashFolderButton = target.As<Button>();
			_openCrashFolderButton.Click += OnOpenCrashFolderClick;
			break;
		case 4:
			OssList = target.As<StackPanel>();
			break;
		case 5:
			_fetchAlbumArtButton = target.As<Button>();
			_fetchAlbumArtButton.Click += OnFetchAlbumArtClick;
			break;
		case 6:
		{
			Button b6 = target.As<Button>();
			_addFolderButton = b6;
			b6.Padding = new Thickness(8, 5, 8, 5);
			b6.Click += OnAddFolderClick;
			break;
		}
		case 7:
		{
			Button b7 = target.As<Button>();
			_removeFolderButton = b7;
			b7.Padding = new Thickness(8, 5, 8, 5);
			b7.Click += OnRemoveFolderClick;
			break;
		}
		case 8:
		{
			Button b8 = target.As<Button>();
			_scanNowButton = b8;
			b8.Padding = new Thickness(8, 5, 8, 5);
			b8.Click += OnScanNowClick;
			break;
		}
		case 9:
			ThemeCombo = target.As<ComboBox>();
			break;
		case 10:
			MaterialCombo = target.As<ComboBox>();
			break;
		case 11:
			WallpaperCombo = target.As<ComboBox>();
			break;
		case 12:
			WallpaperLocalPanel = target.As<StackPanel>();
			break;
		case 13:
			LyricFontButton = target.As<Button>();
			break;
		case 14:
			LyricFontInfoText = target.As<TextBlock>();
			break;
		case 15:
			_importLyricFontButton = target.As<Button>();
			_importLyricFontButton.Click += OnImportLyricFontClick;
			break;
		case 16:
			_resetLyricFontButton = target.As<Button>();
			_resetLyricFontButton.Click += OnResetLyricFontClick;
			break;
		case 17:
			LyricFontFlyout = target.As<Flyout>();
			break;
		case 18:
			LyricFontList = target.As<ListView>();
			LyricFontList.ItemClick += OnLyricFontItemClick;
			break;
		case 20:
			LyricFontButtonText = target.As<TextBlock>();
			break;
		case 21:
			_pickWallpaperButton = target.As<Button>();
			_pickWallpaperButton.Click += OnPickWallpaperClick;
			break;
		case 22:
			WallpaperPathText = target.As<TextBlock>();
			break;
		case 23:
			_saveButton = target.As<Button>();
			_saveButton.Click += OnSaveClick;
			break;
		case 24:
			_restoreDefaultsButton = target.As<Button>();
			_restoreDefaultsButton.Click += OnRestoreDefaultsClick;
			break;
		case 25:
			OutputModeCombo = target.As<ComboBox>();
			break;
		case 26:
			WasapiDeviceCombo = target.As<ComboBox>();
			break;
		case 27:
			AsioDeviceCombo = target.As<ComboBox>();
			break;
		case 28:
			_refreshDevicesButton = target.As<Button>();
			_refreshDevicesButton.Click += OnRefreshDevicesClick;
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

	private Border? _systemSettingsCard;

	private Border CreateSystemAndWindowSettingsCard(bool isDark)
	{
		var card = new Border
		{
			Name = "SystemAndWindowSettingsCard",
			CornerRadius = new CornerRadius(16),
			Margin = new Thickness(0, 0, 0, 16)
		};

		var sp = new StackPanel { Spacing = 12, Padding = new Thickness(16) };

		// Header
		var headerPanel = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
		headerPanel.Children.Add(FluentIconFactory.Create(FluentIconKind.Settings, 20));
		headerPanel.Children.Add(new TextBlock
		{
			Text = Loc.T("系统与窗口设置"),
			FontSize = 16,
			FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
			VerticalAlignment = VerticalAlignment.Center
		});
		sp.Children.Add(headerPanel);

		// 0. 界面语言 (Language)
		var langPanel = new Grid { ColumnSpacing = 12 };
		langPanel.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
		langPanel.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
		var langLabelPanel = new StackPanel { Spacing = 2, VerticalAlignment = VerticalAlignment.Center };
		langLabelPanel.Children.Add(new TextBlock { Text = Loc.T("界面语言"), FontWeight = Microsoft.UI.Text.FontWeights.Medium, FontSize = 14 });
		langLabelPanel.Children.Add(new TextBlock { Text = Loc.T("切换播放器所有界面与主题插件的显示语言"), FontSize = 12, Opacity = 0.72 });
		Grid.SetColumn(langLabelPanel, 0);
		langPanel.Children.Add(langLabelPanel);

		var langCombo = new ComboBox
		{
			MinWidth = 170,
			CornerRadius = new CornerRadius(10),
			HorizontalAlignment = HorizontalAlignment.Right,
			VerticalAlignment = VerticalAlignment.Center
		};
		string curLang = Loc.NormalizeLanguage(ViewModel?.Language ?? Loc.CurrentLanguage);
		int selectedLangIdx = 0;
		for (int li = 0; li < Loc.SupportedLanguages.Count; li++)
		{
			var opt = Loc.SupportedLanguages[li];
			langCombo.Items.Add(new ComboBoxItem { Content = opt.NativeName, Tag = opt.Code });
			if (string.Equals(opt.Code, curLang, StringComparison.OrdinalIgnoreCase))
				selectedLangIdx = li;
		}
		langCombo.SelectedIndex = selectedLangIdx;
		langCombo.SelectionChanged += (_, _) =>
		{
			if (langCombo.SelectedItem is ComboBoxItem cbi && cbi.Tag is string code)
			{
				if (!string.Equals(code, Loc.CurrentLanguage, StringComparison.OrdinalIgnoreCase))
				{
					ViewModel?.SetLanguage(code);
				}
			}
		};
		Grid.SetColumn(langCombo, 1);
		langPanel.Children.Add(langCombo);
		sp.Children.Add(langPanel);

		// 1. 界面缩放 (UI Scale)
		var scalePanel = new StackPanel { Spacing = 6 };
		var scaleHeader = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 10 };
		scaleHeader.Children.Add(new TextBlock { Text = Loc.T("界面缩放 (UI Scale)"), FontWeight = Microsoft.UI.Text.FontWeights.Medium, FontSize = 14 });
		var scaleValueText = new TextBlock
		{
			Text = $"{((int)Math.Round((ViewModel?.UiScale ?? 1.0) * 100))}%",
			FontSize = 13,
			FontWeight = Microsoft.UI.Text.FontWeights.Bold,
			Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 99, 102, 241)),
			VerticalAlignment = VerticalAlignment.Center
		};
		scaleHeader.Children.Add(scaleValueText);
		scalePanel.Children.Add(scaleHeader);

		var scaleControls = new Grid { ColumnSpacing = 12 };
		scaleControls.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
		scaleControls.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(140, GridUnitType.Pixel) });

		var scaleSlider = new Slider
		{
			Minimum = 80,
			Maximum = 200,
			StepFrequency = 5,
			Value = Math.Round((ViewModel?.UiScale ?? 1.0) * 100),
			Margin = new Thickness(0, -6, 0, 0)
		};

		var scaleCombo = new ComboBox
		{
			MinWidth = 130,
			CornerRadius = new CornerRadius(10),
			HorizontalAlignment = HorizontalAlignment.Right
		};
		var scalePresets = new[] { 80, 90, 100, 110, 125, 150, 175, 200 };
		int selectedComboIdx = 2;
		for (int i = 0; i < scalePresets.Length; i++)
		{
			int p = scalePresets[i];
			scaleCombo.Items.Add(new ComboBoxItem { Content = $"{p}%" + (p == 100 ? Loc.T("（标准）") : ""), Tag = p });
			if (Math.Abs(scaleSlider.Value - p) < 3) selectedComboIdx = i;
		}
		scaleCombo.SelectedIndex = selectedComboIdx;

		bool updatingScale = false;
		scaleSlider.ValueChanged += (s, e) =>
		{
			if (updatingScale || ViewModel == null) return;
			updatingScale = true;
			double sVal = e.NewValue / 100.0;
			scaleValueText.Text = $"{((int)e.NewValue)}%";
			ViewModel.SetUiScale(sVal);
			for (int i = 0; i < scaleCombo.Items.Count; i++)
			{
				if (scaleCombo.Items[i] is ComboBoxItem cbi && cbi.Tag is int tag && Math.Abs(tag - e.NewValue) < 3)
				{
					scaleCombo.SelectedIndex = i;
					break;
				}
			}
			updatingScale = false;
		};

		scaleCombo.SelectionChanged += (s, e) =>
		{
			if (updatingScale || ViewModel == null) return;
			if (scaleCombo.SelectedItem is ComboBoxItem cbi && cbi.Tag is int tag)
			{
				updatingScale = true;
				scaleSlider.Value = tag;
				scaleValueText.Text = $"{tag}%";
				ViewModel.SetUiScale(tag / 100.0);
				updatingScale = false;
			}
		};

		Grid.SetColumn(scaleSlider, 0);
		Grid.SetColumn(scaleCombo, 1);
		scaleControls.Children.Add(scaleSlider);
		scaleControls.Children.Add(scaleCombo);
		scalePanel.Children.Add(scaleControls);
		sp.Children.Add(scalePanel);

		// 2. 窗口关闭时隐藏到托盘
		var trayToggle = new ToggleSwitch
		{
			Header = Loc.T("窗口关闭时隐藏到托盘"),
			IsOn = ViewModel?.CloseToTray ?? true,
			OnContent = Loc.T("开启"),
			OffContent = Loc.T("关闭")
		};
		trayToggle.Toggled += (s, e) => ViewModel?.SetCloseToTray(trayToggle.IsOn);
		var trayDesc = new TextBlock
		{
			Text = Loc.T("点击右上角关闭按钮时，保持应用在后台运行，双击托盘图标恢复窗口。"),
			FontSize = 11,
			Opacity = 0.6,
			Margin = new Thickness(0, -6, 0, 4)
		};
		sp.Children.Add(trayToggle);
		sp.Children.Add(trayDesc);

		// 3. 记住窗口位置与尺寸
		var rememberToggle = new ToggleSwitch
		{
			Header = Loc.T("记住窗口大小与位置"),
			IsOn = ViewModel?.RememberWindowState ?? true,
			OnContent = Loc.T("开启"),
			OffContent = Loc.T("关闭")
		};
		rememberToggle.Toggled += (s, e) => ViewModel?.SetRememberWindowState(rememberToggle.IsOn);
		var rememberDesc = new TextBlock
		{
			Text = Loc.T("退出时记录窗口尺寸与屏幕位置，并在下次启动时精准恢复。"),
			FontSize = 11,
			Opacity = 0.6,
			Margin = new Thickness(0, -6, 0, 4)
		};
		sp.Children.Add(rememberToggle);
		sp.Children.Add(rememberDesc);

		// 4. 播放歌曲时保持设备唤醒
		var wakeToggle = new ToggleSwitch
		{
			Header = Loc.T("播放歌曲时保持设备唤醒"),
			IsOn = ViewModel?.PreventSystemSleep ?? true,
			OnContent = Loc.T("开启"),
			OffContent = Loc.T("关闭")
		};
		wakeToggle.Toggled += (s, e) => ViewModel?.SetPreventSystemSleep(wakeToggle.IsOn);
		var wakeDesc = new TextBlock
		{
			Text = Loc.T("在音频高保真播放期间保持系统唤醒，避免 Windows 进入睡眠导致播放中断。"),
			FontSize = 11,
			Opacity = 0.6,
			Margin = new Thickness(0, -6, 0, 4)
		};
		sp.Children.Add(wakeToggle);
		sp.Children.Add(wakeDesc);

		// 5. 记忆播放进度
		var rememberProgressToggle = new ToggleSwitch
		{
			Header = Loc.T("记忆播放进度"),
			IsOn = ViewModel?.RememberPlaybackProgress ?? true,
			OnContent = Loc.T("开启"),
			OffContent = Loc.T("关闭")
		};
		rememberProgressToggle.Toggled += (s, e) => ViewModel?.SetRememberPlaybackProgress(rememberProgressToggle.IsOn);
		var rememberProgressDesc = new TextBlock
		{
			Text = Loc.T("退出时自动保存当前曲目、播放队列与时间位置，下次启动时恢复。"),
			FontSize = 11,
			Opacity = 0.6,
			Margin = new Thickness(0, -6, 0, 4)
		};
		sp.Children.Add(rememberProgressToggle);
		sp.Children.Add(rememberProgressDesc);

		// 6. 启动软件自动播放歌曲
		var autoPlayStartupToggle = new ToggleSwitch
		{
			Header = Loc.T("启动软件自动播放歌曲"),
			IsOn = ViewModel?.AutoPlayOnStartup ?? false,
			OnContent = Loc.T("开启"),
			OffContent = Loc.T("关闭")
		};
		autoPlayStartupToggle.Toggled += (s, e) => ViewModel?.SetAutoPlayOnStartup(autoPlayStartupToggle.IsOn);
		var autoPlayStartupDesc = new TextBlock
		{
			Text = Loc.T("打开 FurinaPlayer 后自动继续播放上次曲目或曲库首曲。"),
			FontSize = 11,
			Opacity = 0.6,
			Margin = new Thickness(0, -6, 0, 4)
		};
		sp.Children.Add(autoPlayStartupToggle);
		sp.Children.Add(autoPlayStartupDesc);

		card.Child = sp;
		LiquidGlassEngine.AttachInteractiveLiquidGlass(card, LiquidGlassVariant.Regular, isDark, enablePointerTracking: true);
		return card;
	}

	private Grid? _innerSettingsGrid;
	private StackPanel? _outerSettingsContainer;
	private bool _sidebarLayoutBuilt;
	private Grid? _settingsTwoColGrid;
	private Border? _settingsSidebarBorder;
	private StackPanel? _sidebarNavStack;
	private ScrollViewer? _rightCardsScrollViewer;
	private StackPanel? _cardsStackPanel;
	private TextBox? _sidebarSearchBox;
	private readonly List<(Button NavBtn, FrameworkElement Card, string Title)> _sidebarNavItems = new();
	private int _activeSidebarNavIndex;
	private Border? _furinaPlayerAppearanceCard;
	private Border? _aboutCardBorder;
	private Border? _backupResetCard;
	private readonly Dictionary<string, Button> _furinaPlayerThemeModeButtons = new();
	private Border? _acrylicFeatureCard;
	private Border? _acrylicLiveBadgeBorder;
	private TextBlock? _acrylicLiveBadgeText;
	private Border? _unfocusedSubCard;
	private ToggleSwitch? _unfocusedToggle;
	private Border? _opacitySubCard;
	private TextBlock? _opacityValueText;
	private Slider? _opacitySlider;
	private Button? _scheduleToggleBtn;
	private TextBox? _scheduleDarkStartBox;
	private TextBox? _scheduleDarkEndBox;
	// (removed)
	// (removed)
	private Button? _presetExpanderBtn;
	private Border? _presetExpanderSwatch;
	private TextBlock? _presetExpanderNameText;
	private TextBlock? _presetExpanderArrowText;
	private Border? _presetWorkbenchContainer;
	private readonly Dictionary<string, Button> _categoryFilterButtons = new();
	private Grid? _presetsGridContainer;
	private int _presetGridCols = 2;
	private bool _suppressFurinaPlayerUiEvents;

	private static bool TryDetachFromTree(DependencyObject? root, FrameworkElement target)
	{
		if (root == null || ReferenceEquals(root, target)) return false;

		if (root is Panel panel)
		{
			if (panel.Children.Contains(target))
			{
				panel.Children.Remove(target);
				return true;
			}
			foreach (UIElement child in panel.Children)
			{
				if (TryDetachFromTree(child, target)) return true;
			}
		}
		else if (root is Border border && border.Child != null)
		{
			if (ReferenceEquals(border.Child, target))
			{
				border.Child = null;
				return true;
			}
			if (TryDetachFromTree(border.Child, target)) return true;
		}
		else if (root is ContentControl cc && cc.Content is DependencyObject contentObj)
		{
			if (ReferenceEquals(contentObj, target))
			{
				cc.Content = null;
				return true;
			}
			if (TryDetachFromTree(contentObj, target)) return true;
		}
		else if (root is ScrollViewer sv && sv.Content is DependencyObject svObj)
		{
			if (ReferenceEquals(svObj, target))
			{
				sv.Content = null;
				return true;
			}
			if (TryDetachFromTree(svObj, target)) return true;
		}

		return false;
	}

	private static void DetachFromParent(FrameworkElement? el, DependencyObject? fallbackRoot = null)
	{
		if (el == null) return;
		DependencyObject? parent = el.Parent ?? VisualTreeHelper.GetParent(el);
		if (parent is Panel p)
		{
			p.Children.Remove(el);
			return;
		}
		if (parent is ContentControl cc)
		{
			cc.Content = null;
			return;
		}
		if (parent is Border b)
		{
			b.Child = null;
			return;
		}
		if (parent is ContentPresenter cp)
		{
			cp.Content = null;
			return;
		}
		if (fallbackRoot != null)
		{
			TryDetachFromTree(fallbackRoot, el);
		}
	}

	private void SafeMoveToPanel(Panel target, FrameworkElement? el)
	{
		if (el == null) return;
		try
		{
			DetachFromParent(el, _innerSettingsGrid ?? (DependencyObject?)Content);
			if (!target.Children.Contains(el))
			{
				target.Children.Add(el);
			}
		}
		catch (Exception ex)
		{
			CrashReporter.LogInfo($"SafeMoveToPanel failed ({el.Name}): {ex.Message}");
		}
	}

	private Border CreateFurinaPlayerAppearanceWorkbenchCard(bool isDark)
	{
		var outerCard = new Border
		{
			Name = "ModernAppearanceWorkbenchCard",
			CornerRadius = new CornerRadius(16),
			Padding = new Thickness(18, 16, 18, 18),
			Margin = new Thickness(0, 0, 0, 16)
		};

		var rootStack = new StackPanel { Spacing = 14 };

		// 1. Header "外观与个性化"
		var headerSection = new StackPanel { Spacing = 6 };
		var titleRow = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
		titleRow.Children.Add(FluentIconFactory.Create(FluentIconKind.Wallpaper, 20));
		titleRow.Children.Add(new TextBlock
		{
			Text = Loc.T("外观与个性化"),
			FontSize = 16,
			FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
			VerticalAlignment = VerticalAlignment.Center
		});
		headerSection.Children.Add(titleRow);
		rootStack.Children.Add(headerSection);

		// 2. 主题模式切换 (浅色 | 深色 | 跟随系统 | 亚克力 | 液态玻璃)
		var themeModeSection = new StackPanel { Spacing = 8, Margin = new Thickness(0, 2, 0, 2) };
		themeModeSection.Children.Add(new TextBlock
		{
			Text = Loc.T("主题模式"),
			FontSize = 14,
			FontWeight = Microsoft.UI.Text.FontWeights.Medium
		});

		var modeButtonsPanel = new SimpleWrapPanel
		{
			HorizontalAlignment = HorizontalAlignment.Stretch
		};
		(string Key, string Icon, string Label)[] modeItems = new[]
		{
			("light", "☀", Loc.T("浅色")),
			("dark", "🌙", Loc.T("深色")),
			("system", "🖥", Loc.T("跟随系统")),
			("acrylic", "🪟", Loc.T("亚克力")),
			("liquid-glass", "💧", Loc.T("液态玻璃"))
		};
		_furinaPlayerThemeModeButtons.Clear();
		foreach (var m in modeItems)
		{
			var btnContent = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 4 };
			btnContent.Children.Add(new TextBlock { Text = m.Icon, FontSize = 12, VerticalAlignment = VerticalAlignment.Center });
			btnContent.Children.Add(new TextBlock { Text = m.Label, FontSize = 12, FontWeight = Microsoft.UI.Text.FontWeights.SemiBold, VerticalAlignment = VerticalAlignment.Center });

			var mBtn = new Button
			{
				Content = btnContent,
				Tag = m.Key,
				CornerRadius = new CornerRadius(8),
				Padding = new Thickness(10, 6, 10, 6),
				Margin = new Thickness(0, 0, 6, 6),
				BorderThickness = new Thickness(1.2)
			};
			mBtn.Click += (s, e) =>
			{
				if (_suppressFurinaPlayerUiEvents || ViewModel == null) return;
				ViewModel.SetFurinaPlayerThemeMode(m.Key);
				if (m.Key == "acrylic") ViewModel.SetMaterial(BackgroundMaterial.Acrylic);
				else if (m.Key == "liquid-glass") ViewModel.SetMaterial(BackgroundMaterial.LiquidGlass);
				else if (m.Key == "light") ViewModel.SetTheme(ThemeMode.Light);
				else if (m.Key == "dark") ViewModel.SetTheme(ThemeMode.Dark);
				else if (m.Key == "system") ViewModel.SetTheme(ThemeMode.System);
				SyncLegacyThemeMaterialCombos();
				RefreshFurinaPlayerAppearanceWorkbenchVisuals(LiquidGlassEngine.IsElementDarkTheme(this));
			};
			_furinaPlayerThemeModeButtons[m.Key] = mBtn;
			modeButtonsPanel.Children.Add(mBtn);
		}
		themeModeSection.Children.Add(modeButtonsPanel);
		rootStack.Children.Add(themeModeSection);

		// 3. "窗口亚克力主题" Feature Card (with miniature glass preview, live % badge, 失焦时保持通透 & 透明度 0-100% slider)
		_acrylicFeatureCard = new Border
		{
			CornerRadius = new CornerRadius(16),
			Padding = new Thickness(18, 16, 18, 16),
			BorderThickness = new Thickness(1)
		};
		var acrylicStack = new StackPanel { Spacing = 14 };

		var acrylicTopGrid = new Grid { ColumnSpacing = 14 };
		acrylicTopGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
		acrylicTopGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
		acrylicTopGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

		// Miniature glass window preview icon
		var miniGlassPreview = new Border
		{
			Width = 48,
			Height = 48,
			CornerRadius = new CornerRadius(12),
			BorderThickness = new Thickness(1),
			BorderBrush = new SolidColorBrush(Color.FromArgb(140, 148, 163, 184)),
			Background = new LinearGradientBrush
			{
				StartPoint = new Point(0, 0),
				EndPoint = new Point(1, 1),
				GradientStops =
				{
					new GradientStop { Color = Color.FromArgb(220, 224, 242, 254), Offset = 0.0 },
					new GradientStop { Color = Color.FromArgb(190, 224, 231, 255), Offset = 0.5 },
					new GradientStop { Color = Color.FromArgb(210, 254, 243, 199), Offset = 1.0 }
				}
			}
		};
		var miniInnerGrid = new Grid { Padding = new Thickness(7) };
		miniInnerGrid.Children.Add(new Border
		{
			Width = 8,
			Height = 8,
			CornerRadius = new CornerRadius(4),
			HorizontalAlignment = HorizontalAlignment.Left,
			VerticalAlignment = VerticalAlignment.Top,
			Background = new SolidColorBrush(Color.FromArgb(230, 56, 189, 248))
		});
		miniInnerGrid.Children.Add(new Border
		{
			Height = 14,
			CornerRadius = new CornerRadius(5),
			VerticalAlignment = VerticalAlignment.Bottom,
			Background = new SolidColorBrush(Color.FromArgb(195, 255, 255, 255)),
			BorderBrush = new SolidColorBrush(Color.FromArgb(140, 255, 255, 255)),
			BorderThickness = new Thickness(1)
		});
		miniGlassPreview.Child = miniInnerGrid;
		Grid.SetColumn(miniGlassPreview, 0);
		acrylicTopGrid.Children.Add(miniGlassPreview);

		var acrylicTextStack = new StackPanel { Spacing = 4, VerticalAlignment = VerticalAlignment.Center };
		acrylicTextStack.Children.Add(new TextBlock
		{
			Text = Loc.T("窗口亚克力主题"),
			FontSize = 15,
			FontWeight = Microsoft.UI.Text.FontWeights.Bold
		});
		acrylicTextStack.Children.Add(new TextBlock
		{
			Text = Loc.T("开启后会立即使用系统亚克力材质，让桌面背景从窗口后方透出；界面会保留可读遮罩，Windows 11 22H2 及以上效果最佳。"),
			FontSize = 12.5,
			Opacity = 0.78,
			TextWrapping = TextWrapping.Wrap
		});
		Grid.SetColumn(acrylicTextStack, 1);
		acrylicTopGrid.Children.Add(acrylicTextStack);

		int initOpacity = ViewModel?.AcrylicOpacityPercent ?? 100;
		_acrylicLiveBadgeBorder = new Border
		{
			CornerRadius = new CornerRadius(999),
			Padding = new Thickness(12, 5, 12, 5),
			VerticalAlignment = VerticalAlignment.Center,
			BorderThickness = new Thickness(1)
		};
		_acrylicLiveBadgeText = new TextBlock
		{
			Text = $"{initOpacity}%",
			FontSize = 12.5,
			FontWeight = Microsoft.UI.Text.FontWeights.Bold
		};
		_acrylicLiveBadgeBorder.Child = _acrylicLiveBadgeText;
		Grid.SetColumn(_acrylicLiveBadgeBorder, 2);
		acrylicTopGrid.Children.Add(_acrylicLiveBadgeBorder);
		acrylicStack.Children.Add(acrylicTopGrid);

		// Bottom two sub-cards: 失焦时保持通透 | 透明度
		var acrylicSubCardsGrid = new Grid { ColumnSpacing = 14 };
		acrylicSubCardsGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
		acrylicSubCardsGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

		// Left sub-card: 失焦时保持通透
		_unfocusedSubCard = new Border
		{
			CornerRadius = new CornerRadius(12),
			Padding = new Thickness(16, 12, 16, 12),
			BorderThickness = new Thickness(1)
		};
		var unfocusedGrid = new Grid { ColumnSpacing = 10 };
		unfocusedGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
		unfocusedGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

		var unfocusedLabels = new StackPanel { Spacing = 4, VerticalAlignment = VerticalAlignment.Center };
		unfocusedLabels.Children.Add(new TextBlock
		{
			Text = Loc.T("失焦时保持通透"),
			FontSize = 13.5,
			FontWeight = Microsoft.UI.Text.FontWeights.SemiBold
		});
		unfocusedLabels.Children.Add(new TextBlock
		{
			Text = Loc.T("切到别的软件时，不收回窗口亚克力透明效果"),
			FontSize = 11.5,
			Opacity = 0.72,
			TextWrapping = TextWrapping.Wrap
		});
		Grid.SetColumn(unfocusedLabels, 0);
		unfocusedGrid.Children.Add(unfocusedLabels);

		_unfocusedToggle = new ToggleSwitch
		{
			IsOn = ViewModel?.AcrylicKeepTranslucentWhenUnfocused ?? true,
			OnContent = null,
			OffContent = null,
			MinWidth = 0,
			VerticalAlignment = VerticalAlignment.Center
		};
		_unfocusedToggle.Toggled += (s, e) =>
		{
			if (_suppressFurinaPlayerUiEvents || ViewModel == null) return;
			ViewModel.SetAcrylicKeepTranslucentWhenUnfocused(_unfocusedToggle.IsOn);
		};
		Grid.SetColumn(_unfocusedToggle, 1);
		unfocusedGrid.Children.Add(_unfocusedToggle);
		_unfocusedSubCard.Child = unfocusedGrid;
		Grid.SetColumn(_unfocusedSubCard, 0);
		acrylicSubCardsGrid.Children.Add(_unfocusedSubCard);

		// Right sub-card: 透明度 0% - 100%
		_opacitySubCard = new Border
		{
			CornerRadius = new CornerRadius(12),
			Padding = new Thickness(16, 12, 16, 12),
			BorderThickness = new Thickness(1)
		};
		var opacityStack = new StackPanel { Spacing = 4 };
		var opacityHeaderGrid = new Grid();
		opacityHeaderGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
		opacityHeaderGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
		opacityHeaderGrid.Children.Add(new TextBlock
		{
			Text = Loc.T("透明度"),
			FontSize = 13.5,
			FontWeight = Microsoft.UI.Text.FontWeights.SemiBold
		});
		_opacityValueText = new TextBlock
		{
			Text = $"{initOpacity}%",
			FontSize = 13,
			FontWeight = Microsoft.UI.Text.FontWeights.Bold
		};
		Grid.SetColumn(_opacityValueText, 1);
		opacityHeaderGrid.Children.Add(_opacityValueText);
		opacityStack.Children.Add(opacityHeaderGrid);

		_opacitySlider = new Slider
		{
			Minimum = 0,
			Maximum = 100,
			StepFrequency = 1,
			Value = initOpacity,
			Margin = new Thickness(0, -4, 0, -4)
		};
		_opacitySlider.ValueChanged += (s, e) =>
		{
			if (_suppressFurinaPlayerUiEvents || ViewModel == null) return;
			int pct = (int)Math.Round(e.NewValue);
			if (_opacityValueText != null) _opacityValueText.Text = $"{pct}%";
			if (_acrylicLiveBadgeText != null) _acrylicLiveBadgeText.Text = $"{pct}%";
			LiquidGlassEngine.AcrylicOpacityScale = pct / 100.0;
			ViewModel.SetAcrylicOpacityPercent(pct);
			WindowManager.InvalidateStageGlass();
			LiquidGlassEngine.RefreshAllAttachedElements(LiquidGlassEngine.IsElementDarkTheme(this));
		};
		opacityStack.Children.Add(_opacitySlider);
		opacityStack.Children.Add(new TextBlock
		{
			Text = Loc.T("0% 为不透明，100% 为最通透"),
			FontSize = 11.5,
			Opacity = 0.72
		});
		_opacitySubCard.Child = opacityStack;
		Grid.SetColumn(_opacitySubCard, 1);
		acrylicSubCardsGrid.Children.Add(_opacitySubCard);

		acrylicStack.Children.Add(acrylicSubCardsGrid);
		_acrylicFeatureCard.Child = acrylicStack;
		rootStack.Children.Add(_acrylicFeatureCard);

		// 4. 材质与壁纸设置
		SafeMoveToPanel(rootStack, MaterialCombo);
		SafeMoveToPanel(rootStack, WallpaperCombo);
		SafeMoveToPanel(rootStack, WallpaperLocalPanel);

		// 4b. 动态壁纸（测试性功能）
		var dynWallBox = new Border
		{
			CornerRadius = new CornerRadius(12),
			Padding = new Thickness(14, 12, 14, 12),
			Margin = new Thickness(0, 4, 0, 2),
			Background = new SolidColorBrush(Color.FromArgb(18, 99, 102, 241)),
			BorderBrush = new SolidColorBrush(Color.FromArgb(44, 99, 102, 241)),
			BorderThickness = new Thickness(1)
		};
		var dynStack = new StackPanel { Spacing = 10 };
		var dynHeader = new Grid();
		dynHeader.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
		dynHeader.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
		var dynTitlePanel = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8, VerticalAlignment = VerticalAlignment.Center };
		dynTitlePanel.Children.Add(new TextBlock { Text = Loc.T("动态壁纸"), FontSize = 14, FontWeight = Microsoft.UI.Text.FontWeights.SemiBold, VerticalAlignment = VerticalAlignment.Center });
		dynTitlePanel.Children.Add(new Border
		{
			CornerRadius = new CornerRadius(5),
			Padding = new Thickness(6, 2, 6, 2),
			Background = new SolidColorBrush(Color.FromArgb(26, 245, 158, 11)),
			Child = new TextBlock { Text = Loc.T("测试性功能"), FontSize = 10.5, FontWeight = Microsoft.UI.Text.FontWeights.SemiBold, Foreground = new SolidColorBrush(Color.FromArgb(255, 217, 119, 6)) }
		});
		dynHeader.Children.Add(dynTitlePanel);

		var dynEnableToggle = new ToggleSwitch
		{
			IsOn = ViewModel?.DynamicWallpaperEnabled ?? false,
			OnContent = Loc.T("开启"),
			OffContent = Loc.T("关闭"),
			MinWidth = 0
		};
		dynEnableToggle.Toggled += (_, _) =>
		{
			if (ViewModel != null) ViewModel.DynamicWallpaperEnabled = dynEnableToggle.IsOn;
		};
		Grid.SetColumn(dynEnableToggle, 1);
		dynHeader.Children.Add(dynEnableToggle);
		dynStack.Children.Add(dynHeader);

		var dynPathTb = new TextBlock
		{
			Text = string.IsNullOrWhiteSpace(ViewModel?.DynamicWallpaperPath) ? Loc.T("未选择动态壁纸 (.gif / .mp4 / .webm / .mkv / .mov)") : ViewModel.DynamicWallpaperPath,
			FontSize = 12,
			Opacity = 0.72,
			TextTrimming = TextTrimming.CharacterEllipsis
		};
		dynStack.Children.Add(dynPathTb);

		var dynBtnsRow = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
		var importDynBtn = new Button { Content = Loc.T("导入动态壁纸 (.gif / 视频)"), CornerRadius = new CornerRadius(8), Padding = new Thickness(12, 7, 12, 7) };
		importDynBtn.Click += async (_, _) =>
		{
			try
			{
				var picker = new Windows.Storage.Pickers.FileOpenPicker();
				var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(WindowManager.CurrentWindow);
				WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);
				foreach (var ext in new[] { ".gif", ".mp4", ".webm", ".mkv", ".mov", ".wmv" }) picker.FileTypeFilter.Add(ext);
				var f = await picker.PickSingleFileAsync();
				if (f != null && ViewModel != null)
				{
					ViewModel.DynamicWallpaperPath = f.Path;
					ViewModel.DynamicWallpaperEnabled = true;
					dynEnableToggle.IsOn = true;
					dynPathTb.Text = f.Path;
				}
			}
			catch { }
		};
		var clearDynBtn = new Button { Content = Loc.T("清除"), CornerRadius = new CornerRadius(8), Padding = new Thickness(10, 7, 10, 7) };
		clearDynBtn.Click += (_, _) =>
		{
			if (ViewModel == null) return;
			ViewModel.DynamicWallpaperEnabled = false;
			ViewModel.DynamicWallpaperPath = string.Empty;
			dynEnableToggle.IsOn = false;
			dynPathTb.Text = Loc.T("未选择动态壁纸 (.gif / .mp4 / .webm / .mkv / .mov)");
		};
		dynBtnsRow.Children.Add(importDynBtn);
		dynBtnsRow.Children.Add(clearDynBtn);
		dynStack.Children.Add(dynBtnsRow);

		var dynParamsRow = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 14 };
		var fpsCombo = new ComboBox { Header = Loc.T("播放帧率 (FPS)"), Width = 140, CornerRadius = new CornerRadius(8) };
		foreach (int fps in new[] { 15, 24, 30, 60 }) fpsCombo.Items.Add(new ComboBoxItem { Content = $"{fps} FPS", Tag = fps });
		int curFps = ViewModel?.DynamicWallpaperFps ?? 30;
		fpsCombo.SelectedIndex = curFps switch { 15 => 0, 24 => 1, 60 => 3, _ => 2 };
		fpsCombo.SelectionChanged += (_, _) =>
		{
			if (ViewModel != null && fpsCombo.SelectedItem is ComboBoxItem { Tag: int f }) ViewModel.DynamicWallpaperFps = f;
		};

		var resCombo = new ComboBox { Header = Loc.T("目标分辨率 (自适应窗口缩放)"), Width = 210, CornerRadius = new CornerRadius(8) };
		resCombo.Items.Add(new ComboBoxItem { Content = Loc.T("720p (性能优先)"), Tag = "720p" });
		resCombo.Items.Add(new ComboBoxItem { Content = Loc.T("1080p (均衡推荐)"), Tag = "1080p" });
		resCombo.Items.Add(new ComboBoxItem { Content = Loc.T("原始分辨率"), Tag = "original" });
		string curRes = ViewModel?.DynamicWallpaperResolution ?? "1080p";
		resCombo.SelectedIndex = curRes switch { "720p" => 0, "original" => 2, _ => 1 };
		resCombo.SelectionChanged += (_, _) =>
		{
			if (ViewModel != null && resCombo.SelectedItem is ComboBoxItem { Tag: string r }) ViewModel.DynamicWallpaperResolution = r;
		};
		dynParamsRow.Children.Add(fpsCombo);
		dynParamsRow.Children.Add(resCombo);
		dynStack.Children.Add(dynParamsRow);

		dynWallBox.Child = dynStack;
		rootStack.Children.Add(dynWallBox);

		// 5. 歌词字体与排版
		var lyricFontHeader = new TextBlock
		{
			Text = Loc.T("歌词字体"),
			FontSize = 14,
			FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
			Margin = new Thickness(0, 4, 0, 0)
		};
		rootStack.Children.Add(lyricFontHeader);

		SafeMoveToPanel(rootStack, LyricFontButton);

		var fontButtonsSp = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
		SafeMoveToPanel(fontButtonsSp, _importLyricFontButton);
		SafeMoveToPanel(fontButtonsSp, _resetLyricFontButton);
		if (fontButtonsSp.Children.Count > 0)
		{
			rootStack.Children.Add(fontButtonsSp);
		}

		SafeMoveToPanel(rootStack, LyricFontInfoText);

		// 4. "定时切换深色模式" Section
		var scheduleSection = new StackPanel { Spacing = 8, Margin = new Thickness(0, 2, 0, 2) };
		var scheduleHeaderRow = new Grid { ColumnSpacing = 12 };
		scheduleHeaderRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
		scheduleHeaderRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

		var scheduleLabel = new TextBlock
		{
			Text = Loc.T("定时切换深色模式"),
			FontSize = 14.5,
			FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
			VerticalAlignment = VerticalAlignment.Center
		};
		Grid.SetColumn(scheduleLabel, 0);
		scheduleHeaderRow.Children.Add(scheduleLabel);

		_scheduleToggleBtn = new Button
		{
			Content = (ViewModel?.ScheduledDarkModeEnabled ?? false) ? Loc.T("已启用") : Loc.T("未启用"),
			CornerRadius = new CornerRadius(8),
			Padding = new Thickness(14, 6, 14, 6),
			FontSize = 12.5,
			FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
			VerticalAlignment = VerticalAlignment.Center
		};
		_scheduleToggleBtn.Click += (s, e) =>
		{
			if (_suppressFurinaPlayerUiEvents || ViewModel == null) return;
			bool next = !ViewModel.ScheduledDarkModeEnabled;
			ViewModel.SetScheduledDarkMode(next, _scheduleDarkStartBox?.Text, _scheduleDarkEndBox?.Text);
			RefreshFurinaPlayerAppearanceWorkbenchVisuals(LiquidGlassEngine.IsElementDarkTheme(this));
		};
		Grid.SetColumn(_scheduleToggleBtn, 1);
		scheduleHeaderRow.Children.Add(_scheduleToggleBtn);
		scheduleSection.Children.Add(scheduleHeaderRow);

		var scheduleTimesWrap = new SimpleWrapPanel
		{
			HorizontalAlignment = HorizontalAlignment.Stretch
		};

		var darkStartPair = new StackPanel
		{
			Orientation = Orientation.Horizontal,
			Spacing = 8,
			Margin = new Thickness(0, 0, 16, 4),
			VerticalAlignment = VerticalAlignment.Center
		};
		darkStartPair.Children.Add(new TextBlock
		{
			Text = Loc.T("切到深色"),
			FontSize = 12.5,
			Opacity = 0.78,
			VerticalAlignment = VerticalAlignment.Center
		});
		_scheduleDarkStartBox = new TextBox
		{
			Text = ViewModel?.ScheduledDarkStartTime ?? "19:00",
			Width = 88,
			MinWidth = 0,
			CornerRadius = new CornerRadius(8),
			FontSize = 12.5,
			Padding = new Thickness(10, 5, 10, 5),
			VerticalAlignment = VerticalAlignment.Center
		};
		_scheduleDarkStartBox.LostFocus += (s, e) =>
		{
			if (_suppressFurinaPlayerUiEvents || ViewModel == null) return;
			ViewModel.SetScheduledDarkMode(ViewModel.ScheduledDarkModeEnabled, _scheduleDarkStartBox.Text, _scheduleDarkEndBox?.Text);
		};
		darkStartPair.Children.Add(_scheduleDarkStartBox);
		scheduleTimesWrap.Children.Add(darkStartPair);

		var darkEndPair = new StackPanel
		{
			Orientation = Orientation.Horizontal,
			Spacing = 8,
			Margin = new Thickness(0, 0, 0, 4),
			VerticalAlignment = VerticalAlignment.Center
		};
		darkEndPair.Children.Add(new TextBlock
		{
			Text = Loc.T("切回浅色"),
			FontSize = 12.5,
			Opacity = 0.78,
			VerticalAlignment = VerticalAlignment.Center
		});
		_scheduleDarkEndBox = new TextBox
		{
			Text = ViewModel?.ScheduledDarkEndTime ?? "07:00",
			Width = 88,
			MinWidth = 0,
			CornerRadius = new CornerRadius(8),
			FontSize = 12.5,
			Padding = new Thickness(10, 5, 10, 5),
			VerticalAlignment = VerticalAlignment.Center
		};
		_scheduleDarkEndBox.LostFocus += (s, e) =>
		{
			if (_suppressFurinaPlayerUiEvents || ViewModel == null) return;
			ViewModel.SetScheduledDarkMode(ViewModel.ScheduledDarkModeEnabled, _scheduleDarkStartBox?.Text, _scheduleDarkEndBox.Text);
		};
		darkEndPair.Children.Add(_scheduleDarkEndBox);
		scheduleTimesWrap.Children.Add(darkEndPair);

		scheduleSection.Children.Add(scheduleTimesWrap);
		rootStack.Children.Add(scheduleSection);

		// 7. "主题预设" Header + Expander Button
		var presetHeaderRow = new Grid { ColumnSpacing = 12, Margin = new Thickness(0, 4, 0, 0) };
		presetHeaderRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
		presetHeaderRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

		presetHeaderRow.Children.Add(new TextBlock
		{
			Text = Loc.T("主题预设"),
			FontSize = 15,
			FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
			VerticalAlignment = VerticalAlignment.Center
		});

		var expanderContent = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
		_presetExpanderSwatch = new Border
		{
			Width = 14,
			Height = 14,
			CornerRadius = new CornerRadius(7),
			Background = new SolidColorBrush(LiquidGlassEngine.ActivePreset.AccentPrimary),
			BorderBrush = new SolidColorBrush(Color.FromArgb(90, 255, 255, 255)),
			BorderThickness = new Thickness(1),
			VerticalAlignment = VerticalAlignment.Center
		};
		_presetExpanderNameText = new TextBlock
		{
			Text = LiquidGlassEngine.ActivePreset.Name,
			FontSize = 12.5,
			FontWeight = Microsoft.UI.Text.FontWeights.Bold,
			VerticalAlignment = VerticalAlignment.Center
		};
		_presetExpanderArrowText = new TextBlock
		{
			Text = (ViewModel?.ThemePresetsExpanded ?? true) ? Loc.T("收起主题预设 ∧") : Loc.T("展开主题预设 ∨"),
			FontSize = 12.5,
			Opacity = 0.78,
			VerticalAlignment = VerticalAlignment.Center
		};
		expanderContent.Children.Add(_presetExpanderSwatch);
		expanderContent.Children.Add(_presetExpanderNameText);
		expanderContent.Children.Add(_presetExpanderArrowText);

		_presetExpanderBtn = new Button
		{
			Content = expanderContent,
			CornerRadius = new CornerRadius(8),
			Padding = new Thickness(12, 6, 12, 6),
			BorderThickness = new Thickness(1)
		};
		_presetExpanderBtn.Click += (s, e) =>
		{
			if (ViewModel == null) return;
			bool next = !ViewModel.ThemePresetsExpanded;
			ViewModel.SetThemePresetsExpanded(next);
			if (_presetWorkbenchContainer != null)
			{
				_presetWorkbenchContainer.Visibility = next ? Visibility.Visible : Visibility.Collapsed;
			}
			if (_presetExpanderArrowText != null)
			{
				_presetExpanderArrowText.Text = next ? Loc.T("收起主题预设 ∧") : Loc.T("展开主题预设 ∨");
			}
		};
		Grid.SetColumn(_presetExpanderBtn, 1);
		presetHeaderRow.Children.Add(_presetExpanderBtn);
		rootStack.Children.Add(presetHeaderRow);

		// 8. Theme Presets & Custom Theme Expanded Workbench Container
		_presetWorkbenchContainer = new Border
		{
			CornerRadius = new CornerRadius(16),
			Padding = new Thickness(18, 16, 18, 18),
			BorderThickness = new Thickness(1),
			Visibility = (ViewModel?.ThemePresetsExpanded ?? true) ? Visibility.Visible : Visibility.Collapsed
		};
		var workbenchStack = new StackPanel { Spacing = 14 };

		// 8a. Custom Theme Header Bar ("自定义主题" stacked cleanly above action pills so title never squishes)
		var customHeaderStack = new StackPanel { Spacing = 10 };

		var customTitleStack = new StackPanel
		{
			Orientation = Orientation.Vertical,
			Spacing = 4
		};
		customTitleStack.Children.Add(new TextBlock
		{
			Text = Loc.T("自定义主题"),
			FontSize = 14,
			FontWeight = Microsoft.UI.Text.FontWeights.Bold
		});
		customTitleStack.Children.Add(new TextBlock
		{
			Text = Loc.T("新主题会自动收入「我的」，支持配色编辑和 JSON 参数导入"),
			FontSize = 12,
			Opacity = 0.78,
			TextWrapping = TextWrapping.Wrap
		});
		customHeaderStack.Children.Add(customTitleStack);

		var customActionsBar = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
		var editThemeBtn = new Button
		{
			Content = Loc.T("✎ 进入编辑"),
			CornerRadius = new CornerRadius(999),
			Padding = new Thickness(11, 5, 11, 5),
			FontSize = 12,
			FontWeight = Microsoft.UI.Text.FontWeights.SemiBold
		};
		editThemeBtn.Click += async (s, e) => await ShowCustomThemeEditorDialogAsync(editCurrent: true);

		var newThemeBtn = new Button
		{
			Content = Loc.T("+ 新建我的主题"),
			CornerRadius = new CornerRadius(999),
			Padding = new Thickness(11, 5, 11, 5),
			FontSize = 12,
			FontWeight = Microsoft.UI.Text.FontWeights.SemiBold
		};
		newThemeBtn.Click += async (s, e) => await ShowCustomThemeEditorDialogAsync(editCurrent: false);

		var importThemeBtn = new Button
		{
			Content = Loc.T("📁 导入参数"),
			CornerRadius = new CornerRadius(999),
			Padding = new Thickness(11, 5, 11, 5),
			FontSize = 12,
			FontWeight = Microsoft.UI.Text.FontWeights.SemiBold
		};
		importThemeBtn.Click += async (s, e) => await ShowImportThemeParamsDialogAsync();

		customActionsBar.Children.Add(editThemeBtn);
		customActionsBar.Children.Add(newThemeBtn);
		customActionsBar.Children.Add(importThemeBtn);
		customHeaderStack.Children.Add(customActionsBar);
		workbenchStack.Children.Add(customHeaderStack);

		// 8b. Category Filter Bar (4x2 responsive grid so all 8 categories stay visible without horizontal clipping)
		var categoryGrid = new Grid { ColumnSpacing = 8, RowSpacing = 8 };
		for (int c = 0; c < 4; c++)
		{
			categoryGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
		}
		categoryGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
		categoryGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

		_categoryFilterButtons.Clear();
		for (int catIdx = 0; catIdx < LiquidGlassEngine.ThemeCategories.Length; catIdx++)
		{
			string currentCat = LiquidGlassEngine.ThemeCategories[catIdx];
			var catBtn = new Button
			{
				Content = Loc.T(currentCat),
				Tag = currentCat,
				HorizontalAlignment = HorizontalAlignment.Stretch,
				HorizontalContentAlignment = HorizontalAlignment.Center,
				CornerRadius = new CornerRadius(999),
				Padding = new Thickness(8, 5, 8, 5),
				FontSize = 12,
				FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
				BorderThickness = new Thickness(1)
			};
			catBtn.Click += (s, e) =>
			{
				ViewModel?.SetSelectedThemeCategory(currentCat);
				RefreshFurinaPlayerAppearanceWorkbenchVisuals(LiquidGlassEngine.IsElementDarkTheme(this));
			};
			_categoryFilterButtons[currentCat] = catBtn;
			Grid.SetRow(catBtn, catIdx / 4);
			Grid.SetColumn(catBtn, catIdx % 4);
			categoryGrid.Children.Add(catBtn);
		}
		workbenchStack.Children.Add(categoryGrid);

		// 8c. Preset Cards Grid
		_presetsGridContainer = new Grid
		{
			ColumnSpacing = 10,
			RowSpacing = 10,
			Margin = new Thickness(0, 4, 0, 0)
		};
		_presetsGridContainer.SizeChanged += (s, e) => UpdatePresetGridColumnsFromContainer(e.NewSize.Width);
		workbenchStack.Children.Add(_presetsGridContainer);

		_presetWorkbenchContainer.Child = workbenchStack;
		rootStack.Children.Add(_presetWorkbenchContainer);

		outerCard.Child = rootStack;
		LiquidGlassEngine.AttachInteractiveLiquidGlass(outerCard, LiquidGlassVariant.Regular, isDark, enablePointerTracking: true);
		RefreshFurinaPlayerAppearanceWorkbenchVisuals(isDark);
		return outerCard;
	}

	private void SyncLegacyThemeMaterialCombos()
	{
		if (ViewModel == null) return;
		try
		{
			_suppressMaterialChange = true;
			if (ThemeCombo != null)
			{
				ThemeCombo.SelectedIndex = (int)ViewModel.Settings.Theme;
			}
			if (MaterialCombo != null)
			{
				MaterialCombo.SelectedIndex = IndexFromMaterial(ViewModel.Settings.BackgroundMaterial);
			}
		}
		catch
		{
		}
		finally
		{
			_suppressMaterialChange = false;
		}
	}

	private void UpdatePresetGridColumns(double pageWidth)
	{
		double containerWidth = (_presetsGridContainer != null && _presetsGridContainer.ActualWidth > 60.0)
			? _presetsGridContainer.ActualWidth
			: EstimatePresetGridWidth(pageWidth);
		UpdatePresetGridColumnsFromContainer(containerWidth);
	}

	private static double EstimatePresetGridWidth(double pageWidth)
	{
		double effectiveWidth = Math.Min(1280.0, pageWidth);
		if (effectiveWidth < 980.0)
		{
			return Math.Max(260.0, effectiveWidth - 48.0 - 72.0);
		}
		double col0Width = effectiveWidth >= 1320.0 ? 420.0 : 380.0;
		return Math.Max(300.0, effectiveWidth - col0Width - 68.0 - 72.0);
	}

	private void UpdatePresetGridColumnsFromContainer(double containerWidth)
	{
		if (containerWidth <= 40.0) return;
		int targetCols = containerWidth >= 760.0 ? 4 : (containerWidth >= 520.0 ? 3 : 2);
		if (targetCols != _presetGridCols)
		{
			_presetGridCols = targetCols;
			PopulateFurinaPlayerPresetCardsGrid(LiquidGlassEngine.IsElementDarkTheme(this));
		}
	}

	private void RefreshFurinaPlayerAppearanceWorkbenchVisuals(bool isDark)
	{
		if (_furinaPlayerAppearanceCard == null) return;
		_suppressFurinaPlayerUiEvents = true;
		try
		{
			LiquidGlassEngine.AttachInteractiveLiquidGlass(_furinaPlayerAppearanceCard, LiquidGlassVariant.Regular, isDark, enablePointerTracking: true);

			var activePreset = LiquidGlassEngine.ActivePreset;
			Color accent = activePreset.AccentPrimary;
			Color cardSurface = isDark
				? Color.FromArgb(145, 30, 41, 59)
				: Color.FromArgb(185, 255, 255, 255);
			Color subCardSurface = isDark
				? Color.FromArgb(165, 22, 30, 46)
				: Color.FromArgb(215, 255, 255, 255);
			Color subtleBorder = isDark
				? Color.FromArgb(60, 255, 255, 255)
				: Color.FromArgb(35, 15, 23, 42);

			// Update Theme Mode Buttons
			string currentMode = ViewModel?.FurinaPlayerThemeMode ?? LiquidGlassEngine.ActiveThemeMode;
			foreach (var kv in _furinaPlayerThemeModeButtons)
			{
				bool isSelected = string.Equals(kv.Key, currentMode, StringComparison.OrdinalIgnoreCase);
				if (isSelected)
				{
					kv.Value.Background = new SolidColorBrush(isDark
						? Color.FromArgb(95, accent.R, accent.G, accent.B)
						: Color.FromArgb(38, accent.R, accent.G, accent.B));
					kv.Value.BorderBrush = new SolidColorBrush(accent);
				}
				else
				{
					kv.Value.Background = new SolidColorBrush(isDark
						? Color.FromArgb(110, 30, 41, 59)
						: Color.FromArgb(175, 255, 255, 255));
					kv.Value.BorderBrush = new SolidColorBrush(subtleBorder);
				}
			}

			// Update Acrylic Feature Card & Subcards
			if (_acrylicFeatureCard != null)
			{
				_acrylicFeatureCard.Background = new SolidColorBrush(cardSurface);
				_acrylicFeatureCard.BorderBrush = new SolidColorBrush(subtleBorder);
			}
			if (_unfocusedSubCard != null)
			{
				_unfocusedSubCard.Background = new SolidColorBrush(subCardSurface);
				_unfocusedSubCard.BorderBrush = new SolidColorBrush(subtleBorder);
			}
			if (_opacitySubCard != null)
			{
				_opacitySubCard.Background = new SolidColorBrush(subCardSurface);
				_opacitySubCard.BorderBrush = new SolidColorBrush(subtleBorder);
			}

			int pct = ViewModel?.AcrylicOpacityPercent ?? 100;
			if (_acrylicLiveBadgeBorder != null)
			{
				_acrylicLiveBadgeBorder.Background = new SolidColorBrush(subCardSurface);
				_acrylicLiveBadgeBorder.BorderBrush = new SolidColorBrush(subtleBorder);
			}
			if (_acrylicLiveBadgeText != null) _acrylicLiveBadgeText.Text = $"{pct}%";
			if (_opacityValueText != null) _opacityValueText.Text = $"{pct}%";
			if (_opacitySlider != null && Math.Abs(_opacitySlider.Value - pct) > 0.5)
			{
				_opacitySlider.Value = pct;
			}
			if (_unfocusedToggle != null)
			{
				_unfocusedToggle.IsOn = ViewModel?.AcrylicKeepTranslucentWhenUnfocused ?? true;
			}

			// Update Scheduled Dark Mode controls
			bool schedOn = ViewModel?.ScheduledDarkModeEnabled ?? false;
			if (_scheduleToggleBtn != null)
			{
				_scheduleToggleBtn.Content = schedOn ? Loc.T("已启用") : Loc.T("未启用");
				_scheduleToggleBtn.Background = new SolidColorBrush(schedOn
					? Color.FromArgb(isDark ? (byte)90 : (byte)36, accent.R, accent.G, accent.B)
					: subCardSurface);
				_scheduleToggleBtn.BorderBrush = new SolidColorBrush(schedOn ? accent : subtleBorder);
				_scheduleToggleBtn.BorderThickness = new Thickness(1);
			}
			if (_scheduleDarkStartBox != null && !_scheduleDarkStartBox.FocusState.HasFlag(FocusState.Keyboard))
			{
				_scheduleDarkStartBox.Text = ViewModel?.ScheduledDarkStartTime ?? "19:00";
			}
			if (_scheduleDarkEndBox != null && !_scheduleDarkEndBox.FocusState.HasFlag(FocusState.Keyboard))
			{
				_scheduleDarkEndBox.Text = ViewModel?.ScheduledDarkEndTime ?? "07:00";
			}

			// Rainbow & Invert modes removed
			// (removed invert toggle)

			// Update Preset Expander Button
			if (_presetExpanderSwatch != null)
			{
				_presetExpanderSwatch.Background = new SolidColorBrush(activePreset.AccentPrimary);
			}
			if (_presetExpanderNameText != null)
			{
				_presetExpanderNameText.Text = activePreset.Name;
			}
			bool expanded = ViewModel?.ThemePresetsExpanded ?? true;
			if (_presetExpanderArrowText != null)
			{
				_presetExpanderArrowText.Text = expanded ? Loc.T("收起主题预设 ∧") : Loc.T("展开主题预设 ∨");
			}
			if (_presetExpanderBtn != null)
			{
				var expanderBg = new SolidColorBrush(subCardSurface);
				var expanderBorder = new SolidColorBrush(subtleBorder);
				var expanderHoverBorder = new SolidColorBrush(accent);
				_presetExpanderBtn.CornerRadius = new CornerRadius(8);
				_presetExpanderBtn.BorderThickness = new Thickness(1);
				_presetExpanderBtn.Background = expanderBg;
				_presetExpanderBtn.BorderBrush = expanderBorder;
				_presetExpanderBtn.Resources["ButtonBackground"] = expanderBg;
				_presetExpanderBtn.Resources["ButtonBorderBrush"] = expanderBorder;
				_presetExpanderBtn.Resources["ButtonBorderBrushPointerOver"] = expanderHoverBorder;
				_presetExpanderBtn.Resources["ButtonBorderBrushPressed"] = expanderHoverBorder;
			}
			if (_presetWorkbenchContainer != null)
			{
				_presetWorkbenchContainer.Visibility = expanded ? Visibility.Visible : Visibility.Collapsed;
				_presetWorkbenchContainer.Background = new SolidColorBrush(cardSurface);
				_presetWorkbenchContainer.BorderBrush = new SolidColorBrush(subtleBorder);
			}

			// Update Category Filter Buttons
			string selectedCat = ViewModel?.SelectedThemeCategory ?? "全部";
			foreach (var kv in _categoryFilterButtons)
			{
				bool isCatSelected = string.Equals(kv.Key, selectedCat, StringComparison.OrdinalIgnoreCase);
				kv.Value.Background = new SolidColorBrush(isCatSelected
					? Color.FromArgb(isDark ? (byte)95 : (byte)38, accent.R, accent.G, accent.B)
					: subCardSurface);
				kv.Value.BorderBrush = new SolidColorBrush(isCatSelected ? accent : subtleBorder);
			}

			PopulateFurinaPlayerPresetCardsGrid(isDark);
		}
		finally
		{
			_suppressFurinaPlayerUiEvents = false;
		}
	}

	private void PopulateFurinaPlayerPresetCardsGrid(bool isDark)
	{
		if (_presetsGridContainer == null) return;
		_presetsGridContainer.Children.Clear();
		_presetsGridContainer.ColumnDefinitions.Clear();
		_presetsGridContainer.RowDefinitions.Clear();

		int cols = Math.Max(2, _presetGridCols);
		for (int c = 0; c < cols; c++)
		{
			_presetsGridContainer.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
		}

		string selectedCat = ViewModel?.SelectedThemeCategory ?? "全部";
		var allPresets = LiquidGlassEngine.GetAllPresets().ToList();
		List<FurinaPlayerThemePreset> filtered;
		if (selectedCat == "全部")
		{
			filtered = allPresets;
		}
		else if (selectedCat == "我的")
		{
			filtered = allPresets.Where(p => p.IsCustom || p.Category == "我的").ToList();
		}
		else
		{
			filtered = allPresets.Where(p => string.Equals(p.Category, selectedCat, StringComparison.OrdinalIgnoreCase)).ToList();
		}

		var cardElements = new List<FrameworkElement>();
		if (selectedCat == "全部" || selectedCat == "个性")
		{
			cardElements.Add(CreateRandomPresetCard(isDark));
		}
		foreach (var preset in filtered)
		{
			cardElements.Add(CreatePresetCard(preset, isDark));
		}

		int totalRows = (cardElements.Count + cols - 1) / cols;
		for (int r = 0; r < totalRows; r++)
		{
			_presetsGridContainer.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
		}

		for (int i = 0; i < cardElements.Count; i++)
		{
			int r = i / cols;
			int c = i % cols;
			Grid.SetRow(cardElements[i], r);
			Grid.SetColumn(cardElements[i], c);
			_presetsGridContainer.Children.Add(cardElements[i]);
		}
	}

	private FrameworkElement CreateRandomPresetCard(bool isDark)
	{
		var activePreset = LiquidGlassEngine.ActivePreset;
		bool isRandomActive = activePreset.Id.StartsWith("random-", StringComparison.OrdinalIgnoreCase);

		Color cardBg = isDark
			? Color.FromArgb(175, 24, 32, 50)
			: Color.FromArgb(228, 255, 255, 255);
		Color cardBorder = isRandomActive
			? activePreset.AccentPrimary
			: (isDark ? Color.FromArgb(55, 255, 255, 255) : Color.FromArgb(35, 15, 23, 42));

		var card = new Border
		{
			CornerRadius = new CornerRadius(14),
			Padding = new Thickness(12, 10, 12, 10),
			Background = new SolidColorBrush(cardBg),
			BorderBrush = new SolidColorBrush(cardBorder),
			BorderThickness = new Thickness(isRandomActive ? 2.0 : 1.0)
		};

		var stack = new StackPanel { Spacing = 6 };

		// Top gradient banner + refresh button
		var banner = new Border
		{
			Height = 36,
			CornerRadius = new CornerRadius(10),
			Padding = new Thickness(10, 0, 8, 0),
			Background = new LinearGradientBrush
			{
				StartPoint = new Point(0, 0),
				EndPoint = new Point(1, 1),
				GradientStops =
				{
					new GradientStop { Color = isRandomActive ? activePreset.StageTopColor : Color.FromArgb(255, 224, 242, 254), Offset = 0.0 },
					new GradientStop { Color = isRandomActive ? activePreset.StageMidColor : Color.FromArgb(255, 237, 233, 254), Offset = 0.55 },
					new GradientStop { Color = isRandomActive ? activePreset.AccentPrimary : Color.FromArgb(255, 244, 114, 182), Offset = 1.0 }
				}
			}
		};
		var bannerGrid = new Grid();
		var refreshBtn = new Button
		{
			Content = "↻",
			Width = 26,
			Height = 26,
			Padding = new Thickness(0),
			CornerRadius = new CornerRadius(13),
			FontSize = 13,
			FontWeight = Microsoft.UI.Text.FontWeights.Bold,
			HorizontalAlignment = HorizontalAlignment.Right,
			VerticalAlignment = VerticalAlignment.Center,
			Background = new SolidColorBrush(Color.FromArgb(215, 255, 255, 255)),
			Foreground = new SolidColorBrush(Color.FromArgb(255, 30, 41, 59)),
			BorderThickness = new Thickness(0)
		};
		refreshBtn.Click += (s, e) =>
		{
			ViewModel?.GenerateAndApplyRandomPreset();
			RefreshFurinaPlayerAppearanceWorkbenchVisuals(LiquidGlassEngine.IsElementDarkTheme(this));
		};
		bannerGrid.Children.Add(refreshBtn);
		banner.Child = bannerGrid;
		stack.Children.Add(banner);

		stack.Children.Add(new TextBlock
		{
			Text = isRandomActive ? Loc.F("随机主题 · {0}", activePreset.Name) : Loc.T("随机主题"),
			FontSize = 14,
			FontWeight = Microsoft.UI.Text.FontWeights.Bold
		});
		stack.Children.Add(new TextBlock
		{
			Text = Loc.T("随机生成一套颜色并自动调整对比度，保证文字和按钮可读。"),
			FontSize = 11.5,
			Opacity = 0.74,
			TextWrapping = TextWrapping.Wrap,
			MinHeight = 32
		});

		// Swatch circles
		var swatchesRow = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 6, Margin = new Thickness(0, 2, 0, 0) };
		Color s1 = isRandomActive ? activePreset.Swatch1 : Color.FromArgb(255, 224, 242, 254);
		Color s2 = isRandomActive ? activePreset.Swatch2 : Color.FromArgb(255, 167, 243, 208);
		Color s3 = isRandomActive ? activePreset.Swatch3 : Color.FromArgb(255, 99, 102, 241);
		swatchesRow.Children.Add(CreateSwatchDot(s1));
		swatchesRow.Children.Add(CreateSwatchDot(s2));
		swatchesRow.Children.Add(CreateSwatchDot(s3));
		stack.Children.Add(swatchesRow);

		card.Child = stack;
		card.PointerPressed += (s, e) =>
		{
			ViewModel?.GenerateAndApplyRandomPreset();
			RefreshFurinaPlayerAppearanceWorkbenchVisuals(LiquidGlassEngine.IsElementDarkTheme(this));
		};
		return card;
	}

	private FrameworkElement CreatePresetCard(FurinaPlayerThemePreset preset, bool isDark)
	{
		bool isSelected = string.Equals(LiquidGlassEngine.ActivePreset.Id, preset.Id, StringComparison.OrdinalIgnoreCase);

		Color cardBg = isDark
			? Color.FromArgb(180, 24, 32, 50)
			: Color.FromArgb(232, 255, 255, 255);
		Color borderColor = isSelected
			? preset.AccentPrimary
			: (isDark ? Color.FromArgb(55, 255, 255, 255) : Color.FromArgb(35, 15, 23, 42));

		var card = new Border
		{
			CornerRadius = new CornerRadius(14),
			Padding = new Thickness(12, 10, 12, 10),
			Background = new SolidColorBrush(cardBg),
			BorderBrush = new SolidColorBrush(borderColor),
			BorderThickness = new Thickness(isSelected ? 2.0 : 1.0)
		};

		var stack = new StackPanel { Spacing = 6 };

		// Top miniature gradient preview banner + checkmark badge when selected
		var banner = new Border
		{
			Height = 36,
			CornerRadius = new CornerRadius(10),
			Padding = new Thickness(10, 0, 8, 0),
			BorderBrush = new SolidColorBrush(Color.FromArgb(45, preset.AccentPrimary.R, preset.AccentPrimary.G, preset.AccentPrimary.B)),
			BorderThickness = new Thickness(1),
			Background = new LinearGradientBrush
			{
				StartPoint = new Point(0, 0),
				EndPoint = new Point(1, 1),
				GradientStops =
				{
					new GradientStop { Color = preset.StageTopColor, Offset = 0.0 },
					new GradientStop { Color = preset.StageMidColor, Offset = 0.62 },
					new GradientStop { Color = Color.FromArgb(195, preset.AccentPrimary.R, preset.AccentPrimary.G, preset.AccentPrimary.B), Offset = 1.0 }
				}
			}
		};

		if (isSelected)
		{
			var checkGrid = new Grid();
			checkGrid.Children.Add(new Border
			{
				Width = 22,
				Height = 22,
				CornerRadius = new CornerRadius(11),
				HorizontalAlignment = HorizontalAlignment.Right,
				VerticalAlignment = VerticalAlignment.Center,
				Background = new SolidColorBrush(Color.FromArgb(230, 255, 255, 255)),
				BorderBrush = new SolidColorBrush(preset.AccentPrimary),
				BorderThickness = new Thickness(1.2),
				Child = new TextBlock
				{
					Text = "✓",
					FontSize = 12,
					FontWeight = Microsoft.UI.Text.FontWeights.Bold,
					Foreground = new SolidColorBrush(preset.AccentPrimary),
					HorizontalAlignment = HorizontalAlignment.Center,
					VerticalAlignment = VerticalAlignment.Center
				}
			});
			banner.Child = checkGrid;
		}
		stack.Children.Add(banner);

		stack.Children.Add(new TextBlock
		{
			Text = preset.Name,
			FontSize = 14,
			FontWeight = Microsoft.UI.Text.FontWeights.Bold
		});
		stack.Children.Add(new TextBlock
		{
			Text = preset.Description,
			FontSize = 11.5,
			Opacity = 0.74,
			TextWrapping = TextWrapping.Wrap,
			MinHeight = 32
		});

		var swatchesRow = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 6, Margin = new Thickness(0, 2, 0, 0) };
		swatchesRow.Children.Add(CreateSwatchDot(preset.Swatch1));
		swatchesRow.Children.Add(CreateSwatchDot(preset.Swatch2));
		swatchesRow.Children.Add(CreateSwatchDot(preset.Swatch3));
		stack.Children.Add(swatchesRow);

		card.Child = stack;
		card.PointerPressed += (s, e) =>
		{
			if (ViewModel == null) return;
			ViewModel.SelectThemePreset(preset.Id);
			SyncLegacyThemeMaterialCombos();
			RefreshFurinaPlayerAppearanceWorkbenchVisuals(LiquidGlassEngine.IsElementDarkTheme(this));
		};
		return card;
	}

	private static Border CreateSwatchDot(Color c)
	{
		return new Border
		{
			Width = 16,
			Height = 16,
			CornerRadius = new CornerRadius(8),
			Background = new SolidColorBrush(c),
			BorderBrush = new SolidColorBrush(Color.FromArgb(75, 100, 116, 139)),
			BorderThickness = new Thickness(1)
		};
	}

	private async System.Threading.Tasks.Task ShowCustomThemeEditorDialogAsync(bool editCurrent)
	{
		if (XamlRoot == null || ViewModel == null) return;
		var current = LiquidGlassEngine.ActivePreset;

		var nameBox = new TextBox
		{
			Header = Loc.T("主题名称"),
			Text = editCurrent ? Loc.F("{0} · 定制", current.Name) : Loc.T("我的水神主题"),
			CornerRadius = new CornerRadius(8)
		};
		var topHexBox = new TextBox
		{
			Header = Loc.T("背景主色 (Hex)"),
			Text = LiquidGlassEngine.ToHexColor(current.StageTopColor),
			CornerRadius = new CornerRadius(8)
		};
		var midHexBox = new TextBox
		{
			Header = Loc.T("面板渐变色 (Hex)"),
			Text = LiquidGlassEngine.ToHexColor(current.StageMidColor),
			CornerRadius = new CornerRadius(8)
		};
		var accentHexBox = new TextBox
		{
			Header = Loc.T("强调色 (Hex)"),
			Text = LiquidGlassEngine.ToHexColor(current.AccentPrimary),
			CornerRadius = new CornerRadius(8)
		};
		var darkSwitch = new ToggleSwitch
		{
			Header = Loc.T("深色基底主题"),
			IsOn = current.IsDark,
			OnContent = Loc.T("深色"),
			OffContent = Loc.T("浅色")
		};

		var sp = new StackPanel { Spacing = 10, MinWidth = 340 };
		sp.Children.Add(nameBox);
		sp.Children.Add(topHexBox);
		sp.Children.Add(midHexBox);
		sp.Children.Add(accentHexBox);
		sp.Children.Add(darkSwitch);

		var dialog = new ContentDialog
		{
			Title = editCurrent ? Loc.T("编辑并保存为我的主题") : Loc.T("新建我的主题"),
			Content = sp,
			PrimaryButtonText = Loc.T("保存并应用"),
			CloseButtonText = Loc.T("取消"),
			DefaultButton = ContentDialogButton.Primary,
			XamlRoot = XamlRoot
		};

		if (await dialog.ShowAsync() == ContentDialogResult.Primary)
		{
			ViewModel.CreateCustomThemePreset(
				nameBox.Text,
				topHexBox.Text,
				midHexBox.Text,
				accentHexBox.Text,
				darkSwitch.IsOn);
			SyncLegacyThemeMaterialCombos();
			RefreshFurinaPlayerAppearanceWorkbenchVisuals(LiquidGlassEngine.IsElementDarkTheme(this));
		}
	}

	private async System.Threading.Tasks.Task ShowImportThemeParamsDialogAsync()
	{
		if (XamlRoot == null || ViewModel == null) return;
		string currentJson = LiquidGlassEngine.SerializeCustomPresets();
		if (string.IsNullOrWhiteSpace(currentJson) || currentJson == "[]")
		{
			var p = LiquidGlassEngine.ActivePreset;
			currentJson = $"[{{\"id\":\"custom-import\",\"name\":\"{p.Name} ({Loc.T("导入")})\",\"category\":\"我的\",\"desc\":\"{Loc.T("JSON 导入主题预设")}\",\"isDark\":\"{(p.IsDark ? "1" : "0")}\",\"top\":\"{LiquidGlassEngine.ToHexColor(p.StageTopColor)}\",\"mid\":\"{LiquidGlassEngine.ToHexColor(p.StageMidColor)}\",\"pri\":\"{LiquidGlassEngine.ToHexColor(p.AccentPrimary)}\"}}]";
		}

		var jsonBox = new TextBox
		{
			Header = Loc.T("主题预设 JSON 参数（支持复制导出或粘贴导入）"),
			Text = currentJson,
			AcceptsReturn = true,
			TextWrapping = TextWrapping.Wrap,
			Height = 160,
			CornerRadius = new CornerRadius(8)
		};

		var dialog = new ContentDialog
		{
			Title = Loc.T("导入 / 导出自定义主题参数"),
			Content = jsonBox,
			PrimaryButtonText = Loc.T("导入并应用"),
			CloseButtonText = Loc.T("取消"),
			DefaultButton = ContentDialogButton.Primary,
			XamlRoot = XamlRoot
		};

		if (await dialog.ShowAsync() == ContentDialogResult.Primary)
		{
			if (ViewModel.ImportCustomThemePresetJson(jsonBox.Text))
			{
				SyncLegacyThemeMaterialCombos();
				RefreshFurinaPlayerAppearanceWorkbenchVisuals(LiquidGlassEngine.IsElementDarkTheme(this));
			}
		}
	}

	private Border? _desktopPetCard;
	private Border? _petStatusBadge;
	private TextBlock? _petStatusText;
	private Button? _petToggleButton;
	private TextBlock? _petToggleButtonText;
	private Button? _petLyricsButton;
	private TextBlock? _petLyricsButtonText;
	private Slider? _petGifSpeedSlider;
	private TextBlock? _petGifSpeedValueText;
	private Slider? _petBubbleScaleSlider;
	private TextBlock? _petBubbleScaleValueText;
	private ComboBox? _petBubbleStyleCombo;
	private bool _suppressPetExtraSliderEvents;
	private Slider? _petScaleSlider;
	private TextBlock? _petScaleValueText;
	private TextBlock? _petIdlePathText;
	private TextBlock? _petGroovePathText;
	private TextBlock? _petClickPathText;
	private bool _suppressPetScaleEvent;

	private Border CreateDesktopPetCard(bool isDark)
	{
		var card = new Border
		{
			Name = "DesktopPetSettingsCard",
			CornerRadius = new CornerRadius(16),
			Margin = new Thickness(0, 0, 0, 16)
		};

		var sp = new StackPanel { Spacing = 12, Padding = new Thickness(16) };

		// Header + Status Badge
		var headerGrid = new Grid { ColumnSpacing = 10 };
		headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
		headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

		var titlePanel = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8, VerticalAlignment = VerticalAlignment.Center };
		titlePanel.Children.Add(FluentIconFactory.Create(FluentIconKind.AppLogo, 20));
		titlePanel.Children.Add(new TextBlock
		{
			Text = Loc.T("桌面宠物（看板娘）"),
			FontSize = 16,
			FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
			VerticalAlignment = VerticalAlignment.Center
		});
		Grid.SetColumn(titlePanel, 0);
		headerGrid.Children.Add(titlePanel);

		_petStatusText = new TextBlock
		{
			FontSize = 12,
			FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
			VerticalAlignment = VerticalAlignment.Center
		};
		_petStatusBadge = new Border
		{
			CornerRadius = new CornerRadius(10),
			Padding = new Thickness(10, 4, 10, 4),
			VerticalAlignment = VerticalAlignment.Center,
			Child = _petStatusText
		};
		Grid.SetColumn(_petStatusBadge, 1);
		headerGrid.Children.Add(_petStatusBadge);
		sp.Children.Add(headerGrid);

		sp.Children.Add(new TextBlock
		{
			Text = Loc.T("在桌面悬浮展示芙宁娜动态看板娘，支持律动同步、拖拽定位与快捷播放控制。"),
			FontSize = 12,
			Opacity = 0.74,
			TextWrapping = TextWrapping.Wrap
		});

		// Action buttons row
		var btnRow = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 10 };
		_petToggleButtonText = new TextBlock { VerticalAlignment = VerticalAlignment.Center };
		_petToggleButton = new Button
		{
			CornerRadius = new CornerRadius(10),
			Padding = new Thickness(14, 7, 14, 7),
			Content = _petToggleButtonText
		};
		_petToggleButton.Click += (_, _) => ViewModel?.TogglePetVisibility();
		LiquidGlassEngine.AttachInteractiveLiquidGlass(_petToggleButton, LiquidGlassVariant.AccentLensed, isDark, enablePointerTracking: true);
		btnRow.Children.Add(_petToggleButton);

		var resetPosBtn = new Button
		{
			CornerRadius = new CornerRadius(10),
			Padding = new Thickness(14, 7, 14, 7),
			Content = new StackPanel
			{
				Orientation = Orientation.Horizontal,
				Spacing = 6,
				Children =
				{
					FluentIconFactory.Create(FluentIconKind.RefreshReset, 14),
					new TextBlock { Text = Loc.T("重置位置"), VerticalAlignment = VerticalAlignment.Center }
				}
			}
		};
		resetPosBtn.Click += (_, _) => ViewModel?.ResetPetPosition();
		LiquidGlassEngine.AttachInteractiveLiquidGlass(resetPosBtn, LiquidGlassVariant.Regular, isDark, enablePointerTracking: true);
		btnRow.Children.Add(resetPosBtn);

		_petLyricsButtonText = new TextBlock { VerticalAlignment = VerticalAlignment.Center };
		_petLyricsButton = new Button
		{
			CornerRadius = new CornerRadius(10),
			Padding = new Thickness(14, 7, 14, 7),
			Content = new StackPanel
			{
				Orientation = Orientation.Horizontal,
				Spacing = 6,
				Children =
				{
					FluentIconFactory.Create(FluentIconKind.LyricsImport, 14),
					_petLyricsButtonText
				}
			}
		};
		_petLyricsButton.Click += (_, _) =>
		{
			ViewModel?.TogglePetLyrics();
			SyncDesktopPetUi();
		};
		LiquidGlassEngine.AttachInteractiveLiquidGlass(_petLyricsButton, LiquidGlassVariant.Regular, isDark, enablePointerTracking: true);
		btnRow.Children.Add(_petLyricsButton);
		sp.Children.Add(btnRow);

		// Scale slider row (60 - 180, step 10)
		var scalePanel = new StackPanel { Spacing = 6 };
		var scaleHeader = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 10 };
		scaleHeader.Children.Add(new TextBlock
		{
			Text = Loc.T("宠物大小"),
			FontWeight = Microsoft.UI.Text.FontWeights.Medium,
			FontSize = 14,
			VerticalAlignment = VerticalAlignment.Center
		});
		_petScaleValueText = new TextBlock
		{
			FontSize = 13,
			FontWeight = Microsoft.UI.Text.FontWeights.Bold,
			Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 99, 102, 241)),
			VerticalAlignment = VerticalAlignment.Center
		};
		scaleHeader.Children.Add(_petScaleValueText);
		scalePanel.Children.Add(scaleHeader);

		_petScaleSlider = new Slider
		{
			Minimum = 60,
			Maximum = 180,
			StepFrequency = 10,
			TickFrequency = 20,
			SnapsTo = Microsoft.UI.Xaml.Controls.Primitives.SliderSnapsTo.StepValues,
			Value = ViewModel?.PetScalePercent ?? 100
		};
		_petScaleSlider.ValueChanged += (_, args) =>
		{
			if (_suppressPetScaleEvent || ViewModel == null) return;
			int pct = (int)Math.Round(args.NewValue);
			ViewModel.PetScalePercent = pct;
			if (_petScaleValueText != null)
				_petScaleValueText.Text = $"{pct}%";
		};
		scalePanel.Children.Add(_petScaleSlider);
		sp.Children.Add(scalePanel);

		// GIF speed slider row (50 - 200, step 10)
		var speedPanel = new StackPanel { Spacing = 6 };
		var speedHeader = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 10 };
		speedHeader.Children.Add(new TextBlock
		{
			Text = Loc.T("GIF 播放速度"),
			FontWeight = Microsoft.UI.Text.FontWeights.Medium,
			FontSize = 14,
			VerticalAlignment = VerticalAlignment.Center
		});
		_petGifSpeedValueText = new TextBlock
		{
			FontSize = 13,
			FontWeight = Microsoft.UI.Text.FontWeights.Bold,
			Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 99, 102, 241)),
			VerticalAlignment = VerticalAlignment.Center
		};
		speedHeader.Children.Add(_petGifSpeedValueText);
		speedPanel.Children.Add(speedHeader);

		_petGifSpeedSlider = new Slider
		{
			Minimum = 50,
			Maximum = 200,
			StepFrequency = 10,
			TickFrequency = 25,
			SnapsTo = Microsoft.UI.Xaml.Controls.Primitives.SliderSnapsTo.StepValues,
			Value = ViewModel?.PetGifSpeedPercent ?? 100
		};
		_petGifSpeedSlider.ValueChanged += (_, args) =>
		{
			if (_suppressPetExtraSliderEvents || ViewModel == null) return;
			int pct = (int)Math.Round(args.NewValue);
			ViewModel.PetGifSpeedPercent = pct;
			if (_petGifSpeedValueText != null)
				_petGifSpeedValueText.Text = $"{pct}% ({pct / 100.0:F1}x)";
		};
		speedPanel.Children.Add(_petGifSpeedSlider);
		sp.Children.Add(speedPanel);

		// Lyric bubble scale slider row (60 - 180, step 10)
		var bubbleScalePanel = new StackPanel { Spacing = 6 };
		var bubbleScaleHeader = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 10 };
		bubbleScaleHeader.Children.Add(new TextBlock
		{
			Text = Loc.T("歌词气泡大小"),
			FontWeight = Microsoft.UI.Text.FontWeights.Medium,
			FontSize = 14,
			VerticalAlignment = VerticalAlignment.Center
		});
		_petBubbleScaleValueText = new TextBlock
		{
			FontSize = 13,
			FontWeight = Microsoft.UI.Text.FontWeights.Bold,
			Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 99, 102, 241)),
			VerticalAlignment = VerticalAlignment.Center
		};
		bubbleScaleHeader.Children.Add(_petBubbleScaleValueText);
		bubbleScalePanel.Children.Add(bubbleScaleHeader);

		_petBubbleScaleSlider = new Slider
		{
			Minimum = 60,
			Maximum = 180,
			StepFrequency = 10,
			TickFrequency = 20,
			SnapsTo = Microsoft.UI.Xaml.Controls.Primitives.SliderSnapsTo.StepValues,
			Value = ViewModel?.PetBubbleScalePercent ?? 100
		};
		_petBubbleScaleSlider.ValueChanged += (_, args) =>
		{
			if (_suppressPetExtraSliderEvents || ViewModel == null) return;
			int pct = (int)Math.Round(args.NewValue);
			ViewModel.PetBubbleScalePercent = pct;
			if (_petBubbleScaleValueText != null)
				_petBubbleScaleValueText.Text = $"{pct}%";
		};
		bubbleScalePanel.Children.Add(_petBubbleScaleSlider);
		sp.Children.Add(bubbleScalePanel);

		// Lyric bubble theme style selector
		var bubbleStylePanel = new StackPanel { Spacing = 6 };
		bubbleStylePanel.Children.Add(new TextBlock
		{
			Text = Loc.T("歌词气泡样式"),
			FontWeight = Microsoft.UI.Text.FontWeights.Medium,
			FontSize = 14
		});
		_petBubbleStyleCombo = new ComboBox
		{
			MinWidth = 240,
			CornerRadius = new CornerRadius(8)
		};
		_petBubbleStyleCombo.Items.Add(new ComboBoxItem { Content = Loc.T("经典芙宁娜（深蓝细框）"), Tag = "classic" });
		_petBubbleStyleCombo.Items.Add(new ComboBoxItem { Content = Loc.T("皇冠小鱼寝（柔蓝圆框）"), Tag = "crown" });
		_petBubbleStyleCombo.Items.Add(new ComboBoxItem { Content = Loc.T("冰雪晶语（漫威晶冰）"), Tag = "ice" });
		_petBubbleStyleCombo.Items.Add(new ComboBoxItem { Content = Loc.T("阳伞水母礼帽（圆润蓝边）"), Tag = "umbrella" });
		_petBubbleStyleCombo.Items.Add(new ComboBoxItem { Content = Loc.T("天使小猫互动（双层蓝框）"), Tag = "angel" });
		_petBubbleStyleCombo.SelectionChanged += (_, _) =>
		{
			if (_suppressPetExtraSliderEvents || ViewModel == null) return;
			if (_petBubbleStyleCombo.SelectedItem is ComboBoxItem { Tag: string styleTag })
			{
				ViewModel.PetBubbleStyle = styleTag;
			}
		};
		bubbleStylePanel.Children.Add(_petBubbleStyleCombo);
		sp.Children.Add(bubbleStylePanel);

		// Desktop Pet Lyric Appearance (Font, Opacity, Color/Gradient, Active Highlight)
		var lyricStyleBorder = new Border
		{
			CornerRadius = new CornerRadius(12),
			Padding = new Thickness(12, 10, 12, 10),
			Background = new SolidColorBrush(Color.FromArgb(14, 37, 99, 235)),
			BorderBrush = new SolidColorBrush(Color.FromArgb(32, 37, 99, 235)),
			BorderThickness = new Thickness(1)
		};
		var lsStack = new StackPanel { Spacing = 10 };
		lsStack.Children.Add(new TextBlock { Text = Loc.T("桌宠歌词外观样式"), FontSize = 14, FontWeight = Microsoft.UI.Text.FontWeights.SemiBold });

		var fontCombo = new ComboBox { Header = Loc.T("系统歌词字体"), MinWidth = 260, IsEditable = true, CornerRadius = new CornerRadius(8) };
		var sysFonts = SkiaSharp.SKFontManager.Default.FontFamilies
			.Where(f => !string.IsNullOrWhiteSpace(f))
			.Distinct(StringComparer.OrdinalIgnoreCase)
			.OrderBy(f => f, StringComparer.OrdinalIgnoreCase)
			.ToList();
		if (sysFonts.Remove("Microsoft YaHei UI"))
		{
			sysFonts.Insert(0, "Microsoft YaHei UI");
		}
		else if (sysFonts.Count == 0)
		{
			sysFonts.Add("Microsoft YaHei UI");
		}
		foreach (string ff in sysFonts)
		{
			fontCombo.Items.Add(ff);
		}
		string curFont = ViewModel?.PetLyricFontFamily ?? "Microsoft YaHei UI";
		if (!sysFonts.Contains(curFont, StringComparer.OrdinalIgnoreCase))
		{
			fontCombo.Items.Insert(0, curFont);
		}
		fontCombo.SelectedItem = curFont;
		if (fontCombo.SelectedIndex < 0) fontCombo.SelectedIndex = 0;
		fontCombo.SelectionChanged += (_, _) =>
		{
			if (ViewModel != null && fontCombo.SelectedItem is string ff) ViewModel.PetLyricFontFamily = ff;
		};
		lsStack.Children.Add(fontCombo);

		var customFontPathTb = new TextBlock
		{
			Text = string.IsNullOrWhiteSpace(ViewModel?.PetLyricFontFilePath)
				? Loc.T("当前使用系统字库字体")
				: $"自定义字体：{Path.GetFileName(ViewModel.PetLyricFontFilePath)}",
			FontSize = 11.5,
			Opacity = 0.72,
			TextTrimming = TextTrimming.CharacterEllipsis
		};
		var customFontBtns = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
		var importPetFontBtn = new Button { Content = Loc.T("导入自定义字体 (.ttf / .otf)"), CornerRadius = new CornerRadius(8), Padding = new Thickness(10, 6, 10, 6) };
		importPetFontBtn.Click += async (_, _) =>
		{
			try
			{
				var picker = new Windows.Storage.Pickers.FileOpenPicker();
				var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(WindowManager.CurrentWindow);
				WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);
				picker.FileTypeFilter.Add(".ttf");
				picker.FileTypeFilter.Add(".otf");
				picker.FileTypeFilter.Add(".ttc");
				var f = await picker.PickSingleFileAsync();
				if (f != null && ViewModel != null)
				{
					ViewModel.PetLyricFontFilePath = f.Path;
					customFontPathTb.Text = $"自定义字体：{f.Name}";
				}
			}
			catch { }
		};
		var resetPetFontBtn = new Button { Content = Loc.T("恢复系统字体"), CornerRadius = new CornerRadius(8), Padding = new Thickness(10, 6, 10, 6) };
		resetPetFontBtn.Click += (_, _) =>
		{
			if (ViewModel == null) return;
			ViewModel.PetLyricFontFilePath = string.Empty;
			customFontPathTb.Text = Loc.T("当前使用系统字库字体");
		};
		customFontBtns.Children.Add(importPetFontBtn);
		customFontBtns.Children.Add(resetPetFontBtn);
		lsStack.Children.Add(customFontBtns);
		lsStack.Children.Add(customFontPathTb);

		var opHeader = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
		opHeader.Children.Add(new TextBlock { Text = Loc.T("歌词透明度"), FontSize = 13, FontWeight = Microsoft.UI.Text.FontWeights.Medium });
		var opValTb = new TextBlock { Text = $"{ViewModel?.PetLyricOpacityPercent ?? 100}%", FontSize = 13, FontWeight = Microsoft.UI.Text.FontWeights.Bold, Foreground = new SolidColorBrush(Color.FromArgb(255, 37, 99, 235)) };
		opHeader.Children.Add(opValTb);
		lsStack.Children.Add(opHeader);

		var opSlider = new Slider { Minimum = 20, Maximum = 100, StepFrequency = 5, Value = ViewModel?.PetLyricOpacityPercent ?? 100 };
		opSlider.ValueChanged += (_, args) =>
		{
			int v = (int)Math.Round(args.NewValue);
			opValTb.Text = $"{v}%";
			if (ViewModel != null) ViewModel.PetLyricOpacityPercent = v;
		};
		lsStack.Children.Add(opSlider);

		var colorPresets = new (string Name, string Hex)[]
		{
			("深海蓝", "#232B5C"),
			("亮海蓝", "#2563EB"),
			("极光紫", "#7C3AED"),
			("樱花粉", "#EC4899"),
			("芙宁娜金", "#D97706"),
			("翡翠绿", "#10B981"),
			("纯净白", "#FFFFFF")
		};

		var colorRow = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 10 };
		var baseColorCombo = new ComboBox { Header = Loc.T("歌词文字颜色"), Width = 150, CornerRadius = new CornerRadius(8) };
		foreach (var cp in colorPresets) baseColorCombo.Items.Add(new ComboBoxItem { Content = Loc.T(cp.Name), Tag = cp.Hex });
		baseColorCombo.SelectedIndex = Math.Max(0, Array.FindIndex(colorPresets, c => string.Equals(c.Hex, ViewModel?.PetLyricColorHex, StringComparison.OrdinalIgnoreCase)));
		baseColorCombo.SelectionChanged += (_, _) =>
		{
			if (ViewModel != null && baseColorCombo.SelectedItem is ComboBoxItem { Tag: string hex }) ViewModel.PetLyricColorHex = hex;
		};

		var gradEndCombo = new ComboBox { Header = Loc.T("渐变副色"), Width = 150, CornerRadius = new CornerRadius(8) };
		foreach (var cp in colorPresets) gradEndCombo.Items.Add(new ComboBoxItem { Content = Loc.T(cp.Name), Tag = cp.Hex });
		gradEndCombo.SelectedIndex = Math.Max(0, Array.FindIndex(colorPresets, c => string.Equals(c.Hex, ViewModel?.PetLyricGradientEndColorHex, StringComparison.OrdinalIgnoreCase)));
		gradEndCombo.SelectionChanged += (_, _) =>
		{
			if (ViewModel != null && gradEndCombo.SelectedItem is ComboBoxItem { Tag: string hex }) ViewModel.PetLyricGradientEndColorHex = hex;
		};
		colorRow.Children.Add(baseColorCombo);
		colorRow.Children.Add(gradEndCombo);
		lsStack.Children.Add(colorRow);

		var gradToggle = new ToggleSwitch { Header = Loc.T("启用歌词渐变色"), IsOn = ViewModel?.PetLyricGradientEnabled ?? false, OnContent = Loc.T("开启"), OffContent = Loc.T("关闭") };
		gradToggle.Toggled += (_, _) => { if (ViewModel != null) ViewModel.PetLyricGradientEnabled = gradToggle.IsOn; };
		lsStack.Children.Add(gradToggle);

		var hlRow = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 12 };
		var hlToggle = new ToggleSwitch { Header = Loc.T("当前播放歌词高亮"), IsOn = ViewModel?.PetLyricHighlightCurrent ?? true, OnContent = Loc.T("开启"), OffContent = Loc.T("关闭") };
		hlToggle.Toggled += (_, _) => { if (ViewModel != null) ViewModel.PetLyricHighlightCurrent = hlToggle.IsOn; };
		var hlColorCombo = new ComboBox { Header = Loc.T("高亮颜色"), Width = 150, CornerRadius = new CornerRadius(8) };
		foreach (var cp in colorPresets) hlColorCombo.Items.Add(new ComboBoxItem { Content = Loc.T(cp.Name), Tag = cp.Hex });
		hlColorCombo.SelectedIndex = Math.Max(0, Array.FindIndex(colorPresets, c => string.Equals(c.Hex, ViewModel?.PetLyricHighlightColorHex, StringComparison.OrdinalIgnoreCase)));
		hlColorCombo.SelectionChanged += (_, _) =>
		{
			if (ViewModel != null && hlColorCombo.SelectedItem is ComboBoxItem { Tag: string hex }) ViewModel.PetLyricHighlightColorHex = hex;
		};
		hlRow.Children.Add(hlToggle);
		hlRow.Children.Add(hlColorCombo);
		lsStack.Children.Add(hlRow);

		lyricStyleBorder.Child = lsStack;
		sp.Children.Add(lyricStyleBorder);

		// Custom GIF section
		sp.Children.Add(new TextBlock
		{
			Text = Loc.T("自定义动画 (GIF)"),
			FontWeight = Microsoft.UI.Text.FontWeights.Medium,
			FontSize = 14,
			Margin = new Thickness(0, 4, 0, 0)
		});

		_petIdlePathText = new TextBlock { FontSize = 12, Opacity = 0.72, TextTrimming = TextTrimming.CharacterEllipsis, VerticalAlignment = VerticalAlignment.Center };
		_petGroovePathText = new TextBlock { FontSize = 12, Opacity = 0.72, TextTrimming = TextTrimming.CharacterEllipsis, VerticalAlignment = VerticalAlignment.Center };
		_petClickPathText = new TextBlock { FontSize = 12, Opacity = 0.72, TextTrimming = TextTrimming.CharacterEllipsis, VerticalAlignment = VerticalAlignment.Center };

		sp.Children.Add(BuildCustomGifRow(Loc.T("待机 GIF"), "idle", _petIdlePathText, isDark));
		sp.Children.Add(BuildCustomGifRow(Loc.T("律动 GIF"), "groove", _petGroovePathText, isDark));
		sp.Children.Add(BuildCustomGifRow(Loc.T("点击 GIF"), "click", _petClickPathText, isDark));

		card.Child = sp;
		LiquidGlassEngine.AttachInteractiveLiquidGlass(card, LiquidGlassVariant.Regular, isDark, enablePointerTracking: true);
		SyncDesktopPetUi();
		return card;
	}

	private async void OnClearLibraryCacheClick(object sender, RoutedEventArgs e)
	{
		if (ViewModel == null) return;
		var dlg = new ContentDialog
		{
			Title = Loc.T("清空曲库缓存"),
			Content = Loc.T("将清理本地封面缓存文件、移除路径已失效的曲目索引并压缩 SQLite 曲库数据库（不会删除磁盘上的原始音乐文件）。是否继续？"),
			PrimaryButtonText = Loc.T("立即清理"),
			CloseButtonText = Loc.T("取消"),
			DefaultButton = ContentDialogButton.Close,
			XamlRoot = XamlRoot
		};
		if (await dlg.ShowAsync() != ContentDialogResult.Primary) return;

		if (_integrityStatusText != null) _integrityStatusText.Text = Loc.T("正在清理曲库缓存与压缩数据库...");
		var res = await ViewModel.ClearLibraryCacheAsync();
		if (_integrityStatusText != null)
		{
			double mb = res.FreedBytes / (1024.0 * 1024.0);
			_integrityStatusText.Text = $"曲库缓存已清空：清理封面缓存 {res.ClearedCoverFiles} 个（释放 {mb:0.##} MB），移除失效曲目索引 {res.RemovedMissingTracks} 条。";
		}
	}

	private Border CreateBackupAndResetCard(bool isDark)
	{
		var card = new Border
		{
			CornerRadius = new CornerRadius(16),
			Padding = new Thickness(22, 18, 22, 18),
			Margin = new Thickness(0)
		};
		var sp = new StackPanel { Spacing = 12 };

		var headerRow = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8, Margin = new Thickness(0, 0, 0, 4) };
		headerRow.Children.Add(FluentIconFactory.Create(FluentIconKind.RefreshReset, 20));
		headerRow.Children.Add(new TextBlock
		{
			Text = Loc.T("备份与重置"),
			FontSize = 16,
			FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
			VerticalAlignment = VerticalAlignment.Center
		});
		sp.Children.Add(headerRow);

		var statusTb = new TextBlock
		{
			Text = Loc.T("支持导出全部设置与偏好至备份文件、迁移导入至新设备，或按周期/启动频率自动备份。"),
			FontSize = 12,
			Opacity = 0.72,
			TextWrapping = TextWrapping.Wrap
		};

		var btnRow = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
		var exportBtn = new Button { Content = Loc.T("导出设置备份"), CornerRadius = new CornerRadius(8), Padding = new Thickness(12, 8, 12, 8) };
		exportBtn.Click += async (_, _) =>
		{
			if (ViewModel == null) return;
			try
			{
				var picker = new Windows.Storage.Pickers.FileSavePicker { SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.DocumentsLibrary, SuggestedFileName = $"FurinaPlayer_Settings_{DateTime.Now:yyyyMMdd}" };
				picker.FileTypeChoices.Add("JSON 备份文件", new List<string> { ".json" });
				var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(WindowManager.CurrentWindow);
				WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);
				var f = await picker.PickSaveFileAsync();
				if (f != null)
				{
					ViewModel.ExportSettingsBackup(f.Path);
					statusTb.Text = $"已导出设置备份至：{f.Path}";
				}
			}
			catch (Exception ex) { statusTb.Text = $"导出失败：{ex.Message}"; }
		};

		var importBtn = new Button { Content = Loc.T("导入设置备份（新设备迁移）"), CornerRadius = new CornerRadius(8), Padding = new Thickness(12, 8, 12, 8) };
		importBtn.Click += async (_, _) =>
		{
			if (ViewModel == null) return;
			try
			{
				var picker = new Windows.Storage.Pickers.FileOpenPicker { SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.DocumentsLibrary };
				picker.FileTypeFilter.Add(".json");
				var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(WindowManager.CurrentWindow);
				WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);
				var f = await picker.PickSingleFileAsync();
				if (f != null)
				{
					ViewModel.ImportSettingsBackup(f.Path, sanitizeForNewDevice: true);
					SettingsSaved?.Invoke();
					RefreshDevices();
					RefreshFurinaPlayerAppearanceWorkbenchVisuals(LiquidGlassEngine.IsElementDarkTheme(this));
					SyncDesktopPetUi();
					statusTb.Text = $"已导入备份（已自动重置本机声卡与失效路径适配新设备）：{f.Name}";
				}
			}
			catch (Exception ex) { statusTb.Text = $"导入失败：{ex.Message}"; }
		};

		var openBackupDirBtn = new Button { Content = Loc.T("打开自动备份目录"), CornerRadius = new CornerRadius(8), Padding = new Thickness(12, 8, 12, 8) };
		openBackupDirBtn.Click += (_, _) =>
		{
			if (ViewModel == null) return;
			string dir = ViewModel.SettingsServiceInstance.BackupDirectory;
			Directory.CreateDirectory(dir);
			Process.Start(new ProcessStartInfo("explorer.exe", $"\"{dir}\"") { UseShellExecute = true });
		};

		btnRow.Children.Add(exportBtn);
		btnRow.Children.Add(importBtn);
		btnRow.Children.Add(openBackupDirBtn);
		sp.Children.Add(btnRow);

		// Auto-backup settings
		var autoBackupToggle = new ToggleSwitch
		{
			Header = Loc.T("自动备份软件设置与首选项"),
			IsOn = ViewModel?.AutoBackupEnabled ?? false,
			OnContent = Loc.T("开启"),
			OffContent = Loc.T("关闭")
		};
		autoBackupToggle.Toggled += (_, _) => { if (ViewModel != null) ViewModel.AutoBackupEnabled = autoBackupToggle.IsOn; };
		sp.Children.Add(autoBackupToggle);

		var autoRow = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 12 };
		var modeCombo = new ComboBox { Header = Loc.T("自动备份策略"), Width = 170, CornerRadius = new CornerRadius(8) };
		modeCombo.Items.Add(new ComboBoxItem { Content = Loc.T("按时间周期 (天)"), Tag = "periodic" });
		modeCombo.Items.Add(new ComboBoxItem { Content = Loc.T("按软件启动频率 (次)"), Tag = "launch_count" });
		modeCombo.SelectedIndex = string.Equals(ViewModel?.AutoBackupMode, "launch_count", StringComparison.OrdinalIgnoreCase) ? 1 : 0;

		var intervalCombo = new ComboBox { Header = Loc.T("备份间隔"), Width = 140, CornerRadius = new CornerRadius(8) };
		void PopulateIntervals()
		{
			intervalCombo.Items.Clear();
			bool byLaunch = modeCombo.SelectedIndex == 1;
			if (byLaunch)
			{
				foreach (int c in new[] { 1, 3, 5, 10, 20 }) intervalCombo.Items.Add(new ComboBoxItem { Content = $"每 {c} 次启动", Tag = c });
				int cur = ViewModel?.AutoBackupLaunchInterval ?? 5;
				intervalCombo.SelectedIndex = cur switch { 1 => 0, 3 => 1, 10 => 3, 20 => 4, _ => 2 };
			}
			else
			{
				foreach (int d in new[] { 1, 3, 7, 14, 30 }) intervalCombo.Items.Add(new ComboBoxItem { Content = $"每 {d} 天", Tag = d });
				int cur = ViewModel?.AutoBackupPeriodDays ?? 7;
				intervalCombo.SelectedIndex = cur switch { 1 => 0, 3 => 1, 14 => 3, 30 => 4, _ => 2 };
			}
		}
		PopulateIntervals();
		modeCombo.SelectionChanged += (_, _) =>
		{
			if (ViewModel != null && modeCombo.SelectedItem is ComboBoxItem { Tag: string m })
			{
				ViewModel.AutoBackupMode = m;
				PopulateIntervals();
			}
		};
		intervalCombo.SelectionChanged += (_, _) =>
		{
			if (ViewModel == null || intervalCombo.SelectedItem is not ComboBoxItem { Tag: int val }) return;
			if (modeCombo.SelectedIndex == 1) ViewModel.AutoBackupLaunchInterval = val;
			else ViewModel.AutoBackupPeriodDays = val;
		};
		autoRow.Children.Add(modeCombo);
		autoRow.Children.Add(intervalCombo);
		sp.Children.Add(autoRow);

		// Restore Defaults button in dedicated Backup & Reset section
		var restoreBtn = new Button
		{
			Content = Loc.T("恢复默认设置"),
			CornerRadius = new CornerRadius(8),
			Padding = new Thickness(14, 8, 14, 8),
			Background = new SolidColorBrush(Color.FromArgb(24, 239, 68, 68)),
			Foreground = new SolidColorBrush(Color.FromArgb(255, 220, 38, 38)),
			FontWeight = Microsoft.UI.Text.FontWeights.SemiBold
		};
		restoreBtn.Click += OnRestoreDefaultsClick;
		sp.Children.Add(restoreBtn);
		sp.Children.Add(statusTb);

		card.Child = sp;
		LiquidGlassEngine.AttachInteractiveLiquidGlass(card, LiquidGlassVariant.Regular, isDark, enablePointerTracking: true);
		return card;
	}

	private Grid BuildCustomGifRow(string label, string kind, TextBlock pathText, bool isDark)
	{
		var row = new Grid { ColumnSpacing = 8 };
		row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(86, GridUnitType.Pixel) });
		row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
		row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
		row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

		var lbl = new TextBlock
		{
			Text = label,
			FontSize = 13,
			VerticalAlignment = VerticalAlignment.Center
		};
		Grid.SetColumn(lbl, 0);
		row.Children.Add(lbl);

		Grid.SetColumn(pathText, 1);
		row.Children.Add(pathText);

		var pickBtn = new Button
		{
			Content = Loc.T("选择 GIF…"),
			CornerRadius = new CornerRadius(8),
			Padding = new Thickness(10, 4, 10, 4),
			FontSize = 12
		};
		pickBtn.Click += async (_, _) => await PickCustomPetGifAsync(kind);
		LiquidGlassEngine.AttachInteractiveLiquidGlass(pickBtn, LiquidGlassVariant.Regular, isDark, enablePointerTracking: false);
		Grid.SetColumn(pickBtn, 2);
		row.Children.Add(pickBtn);

		var resetBtn = new Button
		{
			Content = Loc.T("恢复默认"),
			CornerRadius = new CornerRadius(8),
			Padding = new Thickness(10, 4, 10, 4),
			FontSize = 12
		};
		resetBtn.Click += (_, _) => ViewModel?.SetCustomPetGif(kind, null);
		LiquidGlassEngine.AttachInteractiveLiquidGlass(resetBtn, LiquidGlassVariant.Regular, isDark, enablePointerTracking: false);
		Grid.SetColumn(resetBtn, 3);
		row.Children.Add(resetBtn);

		return row;
	}

	private async System.Threading.Tasks.Task PickCustomPetGifAsync(string kind)
	{
		if (ViewModel == null) return;
		try
		{
			var picker = new FileOpenPicker
			{
				FileTypeFilter = { ".gif" }
			};
			nint windowHandle = WindowNative.GetWindowHandle(WindowManager.CurrentWindow);
			InitializeWithWindow.Initialize(picker, windowHandle);
			StorageFile file = await picker.PickSingleFileAsync();
			if (file != null && !string.IsNullOrWhiteSpace(file.Path))
			{
				ViewModel.SetCustomPetGif(kind, file.Path);
			}
		}
		catch
		{
		}
	}

	private void SyncDesktopPetUi()
	{
		if (ViewModel == null) return;
		bool enabled = ViewModel.PetEnabled;

		if (_petStatusText != null)
		{
			_petStatusText.Text = ViewModel.PetStatusText;
			_petStatusText.Foreground = enabled
				? new SolidColorBrush(Windows.UI.Color.FromArgb(255, 56, 189, 248))
				: new SolidColorBrush(Windows.UI.Color.FromArgb(255, 148, 163, 184));
		}
		if (_petStatusBadge != null)
		{
			_petStatusBadge.Background = enabled
				? new SolidColorBrush(Windows.UI.Color.FromArgb(38, 56, 189, 248))
				: new SolidColorBrush(Windows.UI.Color.FromArgb(28, 148, 163, 184));
		}
		if (_petToggleButtonText != null)
		{
			_petToggleButtonText.Text = enabled ? Loc.T("隐藏看板娘") : Loc.T("显示看板娘");
		}
		if (_petLyricsButtonText != null)
		{
			_petLyricsButtonText.Text = $"{Loc.T("看板娘歌词气泡")}: {(ViewModel.PetLyricsEnabled ? Loc.T("开") : Loc.T("关"))}";
		}
		if (_petScaleSlider != null)
		{
			_suppressPetScaleEvent = true;
			_petScaleSlider.Value = ViewModel.PetScalePercent;
			_suppressPetScaleEvent = false;
		}
		if (_petScaleValueText != null)
		{
			_petScaleValueText.Text = $"{ViewModel.PetScalePercent}%";
		}
		if (_petGifSpeedSlider != null)
		{
			_suppressPetExtraSliderEvents = true;
			_petGifSpeedSlider.Value = ViewModel.PetGifSpeedPercent;
			_suppressPetExtraSliderEvents = false;
		}
		if (_petGifSpeedValueText != null)
		{
			_petGifSpeedValueText.Text = $"{ViewModel.PetGifSpeedPercent}% ({ViewModel.PetGifSpeedPercent / 100.0:F1}x)";
		}
		if (_petBubbleScaleSlider != null)
		{
			_suppressPetExtraSliderEvents = true;
			_petBubbleScaleSlider.Value = ViewModel.PetBubbleScalePercent;
			_suppressPetExtraSliderEvents = false;
		}
		if (_petBubbleScaleValueText != null)
		{
			_petBubbleScaleValueText.Text = $"{ViewModel.PetBubbleScalePercent}%";
		}
		if (_petBubbleStyleCombo != null)
		{
			_suppressPetExtraSliderEvents = true;
			string curStyle = ViewModel.PetBubbleStyle;
			for (int i = 0; i < _petBubbleStyleCombo.Items.Count; i++)
			{
				if (_petBubbleStyleCombo.Items[i] is ComboBoxItem item && string.Equals(item.Tag?.ToString(), curStyle, StringComparison.OrdinalIgnoreCase))
				{
					_petBubbleStyleCombo.SelectedIndex = i;
					break;
				}
			}
			_suppressPetExtraSliderEvents = false;
		}
		if (_petIdlePathText != null)
		{
			_petIdlePathText.Text = string.IsNullOrWhiteSpace(ViewModel.PetCustomIdleGifPath)
				? Loc.T("默认内置")
				: Path.GetFileName(ViewModel.PetCustomIdleGifPath);
		}
		if (_petGroovePathText != null)
		{
			_petGroovePathText.Text = string.IsNullOrWhiteSpace(ViewModel.PetCustomGrooveGifPath)
				? Loc.T("默认内置")
				: Path.GetFileName(ViewModel.PetCustomGrooveGifPath);
		}
		if (_petClickPathText != null)
		{
			_petClickPathText.Text = string.IsNullOrWhiteSpace(ViewModel.PetCustomClickGifPath)
				? Loc.T("默认内置")
				: Path.GetFileName(ViewModel.PetCustomClickGifPath);
		}
	}
}
