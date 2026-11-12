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
using SonicWave.Audio.Output;
using SonicWave.Core.Models;
using WinRT;
using WinRT.Interop;
using Windows.Foundation;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.System;
using Windows.UI;

namespace FurinaPlayer.UI.Pages;

[WinRTRuntimeClassName("Microsoft.UI.Xaml.IUIElementOverrides")]
[WinRTExposedType(typeof(SettingsPageWinRTTypeDetails))]
public sealed class SettingsPage : Page, IComponentConnector
{
	private bool _suppressMaterialChange;

	private bool _handlersAttached;

	private SKXamlCanvas? _ambientCausticCanvas;

	private Grid? _rootStageGrid;

	private ScrollViewer? _settingsScrollViewer;

	private Button? _fetchAlbumArtButton;

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

	private static readonly string[] SystemFontOptions = new string[35]
	{
		"系统默认", "微软雅黑", "微软雅黑 Light", "宋体", "黑体", "楷体", "仿宋", "隶书", "幼圆", "等线",
		"华文细黑", "华文楷体", "华文宋体", "华文仿宋", "华文琥珀", "华文彩云", "Arial", "Segoe UI", "Segoe UI Light", "Consolas",
		"Times New Roman", "Georgia", "Verdana", "Tahoma", "Courier New", "Comic Sans MS", "Impact", "Lucida Console", "Trebuchet MS", "Arial Black",
		"Garamond", "Palatino Linotype", "Book Antiqua", "Bodoni MT", "Century Gothic"
	};

	private static readonly (string Name, string Url)[] OssLibraries = new (string, string)[16]
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
		("LRCLIB 在线歌词 API", "https://lrclib.net/"),
		("网易云音乐歌词接口（非官方）", "https://music.163.com/"),
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
		Loaded += OnLoaded;
		ActualThemeChanged += (s, e) => ApplySettingsLiquidGlass();
		SizeChanged += OnSizeChanged;
		ApplySettingsLiquidGlass();
	}

	public SettingsPage(SettingsViewModel viewModel)
		: this()
	{
		ViewModel = viewModel;
		DataContext = viewModel;
	}

	private void OnLoaded(object sender, RoutedEventArgs e)
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
		ApplySettingsLiquidGlass();
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

	private void ApplySettingsLiquidGlass()
	{
		try
		{
			bool isDark = LiquidGlassEngine.IsElementDarkTheme(this);
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

			if (_echoAppearanceCard == null)
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
								if (!ReferenceEquals(sub, _echoAppearanceCard))
								{
									LiquidGlassEngine.UpgradeVisualTreeToLiquidGlass(sub, isDark);
								}
							}
						}
						else if (!ReferenceEquals(child, _echoAppearanceCard))
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
					if (_systemSettingsCard == null)
					{
						_systemSettingsCard = CreateSystemAndWindowSettingsCard(isDark);
						col1Panel.Children.Insert(0, _systemSettingsCard);
					}
					else
					{
						LiquidGlassEngine.AttachInteractiveLiquidGlass(_systemSettingsCard, LiquidGlassVariant.Regular, isDark, enablePointerTracking: true);
					}

					// 隐藏原本旧的“外观与主题”卡片（插入系统设置后位于 index 1）
					if (col1Panel.Children.Count >= 2 && col1Panel.Children[1] is Border legacyAppearanceCard && legacyAppearanceCard != _echoAppearanceCard)
					{
						legacyAppearanceCard.Visibility = Visibility.Collapsed;
					}

					if (_echoAppearanceCard == null)
					{
						_echoAppearanceCard = CreateEchoAppearanceWorkbenchCard(isDark);
						// 插入到系统与窗口设置之后、音乐库之前
						col1Panel.Children.Insert(2, _echoAppearanceCard);
					}
					else
					{
						RefreshEchoAppearanceWorkbenchVisuals(isDark);
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

			EnsureAboutCardAtEnd(settingsGrid, isDark);

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
		catch
		{
		}
	}

	private void UpgradeSettingsVisualTreeNodes(DependencyObject root, bool isDark)
	{
		if (root == null || ReferenceEquals(root, _echoAppearanceCard)) return;

		int count = VisualTreeHelper.GetChildrenCount(root);
		for (int i = 0; i < count; i++)
		{
			DependencyObject child = VisualTreeHelper.GetChild(root, i);
			if (ReferenceEquals(child, _echoAppearanceCard)) continue;
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

			UpgradeSettingsVisualTreeNodes(child, isDark);
		}
	}

	private void UpdateResponsiveLayout(double width)
	{
		try
		{
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

	private void EnsureAboutCardAtEnd(Grid? settingsGrid, bool isDark)
	{
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
			if (!string.IsNullOrWhiteSpace(buildMetaVersion))
			{
				return buildMetaVersion;
			}
			AssemblyInformationalVersionAttribute? customAttribute = entryAsm?.GetCustomAttribute<AssemblyInformationalVersionAttribute>();
			return string.IsNullOrWhiteSpace(customAttribute?.InformationalVersion) ? "1.0.0.0.0000" : customAttribute.InformationalVersion;
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
			settingsViewModel.PropertyChanged += OnSettingsViewModelPropertyChanged;
		}
		if (!_handlersAttached)
		{
			_handlersAttached = true;
			OutputModeCombo.SelectionChanged += (object _, SelectionChangedEventArgs _) =>
			{
				ViewModel.SetOutputMode((OutputMode)Math.Max(0, OutputModeCombo.SelectedIndex));
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
				if (WasapiDeviceCombo.SelectedItem is ComboBoxItem { Tag: string tag })
				{
					ViewModel.SelectedWasapiDeviceId = tag;
				}
			};
			AsioDeviceCombo.SelectionChanged += (object _, SelectionChangedEventArgs _) =>
			{
				if (AsioDeviceCombo.SelectedItem is ComboBoxItem { Tag: string tag })
				{
					ViewModel.SelectedAsioDriver = tag;
				}
			};
		}
		ViewModel.Load();
		VersionText.Text = "软件版本：" + GetAppVersion();
		OutputModeCombo.SelectedIndex = (int)ViewModel.Settings.OutputMode;
		ThemeCombo.SelectedIndex = (int)ViewModel.Settings.Theme;
		_suppressMaterialChange = true;
		MaterialCombo.SelectedIndex = IndexFromMaterial(ViewModel.Settings.BackgroundMaterial);
		_suppressMaterialChange = false;
		WallpaperCombo.SelectedIndex = (int)ViewModel.Settings.WallpaperSource;
		WallpaperLocalPanel.Visibility = ((ViewModel.Settings.WallpaperSource != WallpaperSource.LocalImage) ? Visibility.Collapsed : Visibility.Visible);
		WallpaperPathText.Text = ViewModel.LocalWallpaperPath;
		InitLyricFontUi();
		PopulateOssList();
		RefreshDevices();
		if (_fetchAlbumArtButton != null && ViewModel != null)
		{
			_fetchAlbumArtButton.IsEnabled = ViewModel.CanFetchAlbumArt;
		}
		ApplySettingsLiquidGlass();
	}

	protected override void OnNavigatedFrom(NavigationEventArgs e)
	{
		base.OnNavigatedFrom(e);
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
	}

	private void InitLyricFontUi()
	{
		if (!(LyricFontButton == null) && !(LyricFontList == null) && ViewModel != null)
		{
			List<string> list = new List<string>(SystemFontOptions);
			string lyricFontFilePath = ViewModel.LyricFontFilePath;
			string lyricFontFamily = ViewModel.LyricFontFamily;
			if (!string.IsNullOrWhiteSpace(lyricFontFilePath))
			{
				list.Insert(0, "已导入自定义字体");
				LyricFontInfoText.Text = "已导入字体：" + Path.GetFileName(lyricFontFilePath) + "（优先生效）";
			}
			else
			{
				LyricFontInfoText.Text = "正在播放页歌词使用所选字体；导入的 .ttf/.otf 字体文件优先生效并复制到应用数据目录。";
			}
			LyricFontList.ItemsSource = list;
			TextBlock lyricFontButtonText = LyricFontButtonText;
			string text;
			if (string.IsNullOrWhiteSpace(lyricFontFilePath))
			{
				text = (string.IsNullOrWhiteSpace(lyricFontFamily) ? "系统默认" : lyricFontFamily);
			}
			else
			{
				text = "已导入：" + Path.GetFileName(lyricFontFilePath);
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
			if (string.Equals(text, "系统默认", StringComparison.Ordinal) || string.Equals(text, "已导入自定义字体", StringComparison.Ordinal))
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
				LyricFontInfoText.Text = "导入字体失败：" + ex.Message;
			}
		}
	}

	private void OnResetLyricFontClick(object sender, RoutedEventArgs e)
	{
		ViewModel.ResetLyricFont();
		InitLyricFontUi();
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
					Content = item,
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
			WallpaperPathText.Text = "选择失败：" + ex.Message;
		}
	}

	private void RefreshDevices()
	{
		ViewModel.RefreshDevicesCommand.Execute(null);
		WasapiDeviceCombo.Items.Clear();
		foreach (AudioDeviceInfo wasapiDevice in ViewModel.WasapiDevices)
		{
			ComboBoxItem item = new ComboBoxItem
			{
				Content = (wasapiDevice.IsDefault ? (wasapiDevice.Name + "（默认）") : wasapiDevice.Name),
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
			Title = "恢复默认设置",
			Content = "将重置音量、均衡器、空间音频、音色、主题、材质、壁纸、输出设备、歌词字体、VST3 插件配置等所有自定义参数为出厂默认值（类似清洁安装）。\n此操作保留已导入的音乐与专辑，不会删除音乐库与本地文件，是否继续？",
			PrimaryButtonText = "恢复",
			CloseButtonText = "取消",
			DefaultButton = ContentDialogButton.Close,
			XamlRoot = XamlRoot
		}.ShowAsync() == ContentDialogResult.Primary)
		{
			ViewModel.RestoreDefaultsCommand.Execute(null);
			SettingsSaved?.Invoke();
			OutputModeCombo.SelectedIndex = (int)ViewModel.Settings.OutputMode;
			ThemeCombo.SelectedIndex = (int)ViewModel.Settings.Theme;
			_suppressMaterialChange = true;
			MaterialCombo.SelectedIndex = IndexFromMaterial(ViewModel.Settings.BackgroundMaterial);
			_suppressMaterialChange = false;
			WallpaperCombo.SelectedIndex = (int)ViewModel.Settings.WallpaperSource;
			WallpaperLocalPanel.Visibility = ((ViewModel.Settings.WallpaperSource != WallpaperSource.LocalImage) ? Visibility.Collapsed : Visibility.Visible);
			WallpaperPathText.Text = ViewModel.LocalWallpaperPath;
			RefreshDevices();
			RefreshEchoAppearanceWorkbenchVisuals(LiquidGlassEngine.IsElementDarkTheme(this));
		}
	}

	private void OnSaveClick(object sender, RoutedEventArgs e)
	{
		ViewModel.SaveCommand.Execute(null);
		SettingsSaved?.Invoke();
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
			ViewModel.AlbumArtStatus = "操作异常：" + ex.Message;
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
			Text = "系统与窗口设置",
			FontSize = 16,
			FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
			VerticalAlignment = VerticalAlignment.Center
		});
		sp.Children.Add(headerPanel);

		// 1. 界面缩放 (UI Scale)
		var scalePanel = new StackPanel { Spacing = 6 };
		var scaleHeader = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 10 };
		scaleHeader.Children.Add(new TextBlock { Text = "界面缩放 (UI Scale)", FontWeight = Microsoft.UI.Text.FontWeights.Medium, FontSize = 14 });
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
			scaleCombo.Items.Add(new ComboBoxItem { Content = $"{p}%" + (p == 100 ? "（标准）" : ""), Tag = p });
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
			Header = "窗口关闭时隐藏到托盘",
			IsOn = ViewModel?.CloseToTray ?? true,
			OnContent = "开启",
			OffContent = "关闭"
		};
		trayToggle.Toggled += (s, e) => ViewModel?.SetCloseToTray(trayToggle.IsOn);
		var trayDesc = new TextBlock
		{
			Text = "点击右上角关闭按钮时，保持应用在后台运行，双击托盘图标恢复窗口。",
			FontSize = 11,
			Opacity = 0.6,
			Margin = new Thickness(0, -6, 0, 4)
		};
		sp.Children.Add(trayToggle);
		sp.Children.Add(trayDesc);

		// 3. 记住窗口位置与尺寸
		var rememberToggle = new ToggleSwitch
		{
			Header = "记住窗口大小与位置",
			IsOn = ViewModel?.RememberWindowState ?? true,
			OnContent = "开启",
			OffContent = "关闭"
		};
		rememberToggle.Toggled += (s, e) => ViewModel?.SetRememberWindowState(rememberToggle.IsOn);
		var rememberDesc = new TextBlock
		{
			Text = "退出时记录窗口尺寸与屏幕位置，并在下次启动时精准恢复。",
			FontSize = 11,
			Opacity = 0.6,
			Margin = new Thickness(0, -6, 0, 4)
		};
		sp.Children.Add(rememberToggle);
		sp.Children.Add(rememberDesc);

		// 4. 音乐播放时设备保持唤醒
		var wakeToggle = new ToggleSwitch
		{
			Header = "播放音乐时阻止系统休眠",
			IsOn = ViewModel?.PreventSystemSleep ?? true,
			OnContent = "开启",
			OffContent = "关闭"
		};
		wakeToggle.Toggled += (s, e) => ViewModel?.SetPreventSystemSleep(wakeToggle.IsOn);
		var wakeDesc = new TextBlock
		{
			Text = "在音频高保真播放期间保持系统唤醒，避免 Windows 进入睡眠导致播放中断。",
			FontSize = 11,
			Opacity = 0.6,
			Margin = new Thickness(0, -6, 0, 4)
		};
		sp.Children.Add(wakeToggle);
		sp.Children.Add(wakeDesc);

		card.Child = sp;
		LiquidGlassEngine.AttachInteractiveLiquidGlass(card, LiquidGlassVariant.Regular, isDark, enablePointerTracking: true);
		return card;
	}

	private Grid? _innerSettingsGrid;
	private StackPanel? _outerSettingsContainer;
	private Border? _echoAppearanceCard;
	private Border? _aboutCardBorder;
	private readonly Dictionary<string, Button> _echoThemeModeButtons = new();
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
	private ToggleSwitch? _rainbowToggle;
	private ToggleSwitch? _invertToggle;
	private Button? _presetExpanderBtn;
	private Border? _presetExpanderSwatch;
	private TextBlock? _presetExpanderNameText;
	private TextBlock? _presetExpanderArrowText;
	private Border? _presetWorkbenchContainer;
	private readonly Dictionary<string, Button> _categoryFilterButtons = new();
	private Grid? _presetsGridContainer;
	private int _presetGridCols = 2;
	private bool _suppressEchoUiEvents;

	private static void DetachFromParent(FrameworkElement? el)
	{
		if (el == null) return;
		if (el.Parent is Panel p)
		{
			p.Children.Remove(el);
		}
		else if (el.Parent is ContentControl cc)
		{
			cc.Content = null;
		}
		else if (el.Parent is Border b)
		{
			b.Child = null;
		}
	}

	private Border CreateEchoAppearanceWorkbenchCard(bool isDark)
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
			Text = "外观与个性化",
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
			Text = "主题模式",
			FontSize = 14,
			FontWeight = Microsoft.UI.Text.FontWeights.Medium
		});

		var modeButtonsPanel = new SimpleWrapPanel
		{
			HorizontalAlignment = HorizontalAlignment.Stretch
		};
		(string Key, string Icon, string Label)[] modeItems = new[]
		{
			("light", "☀", "浅色"),
			("dark", "🌙", "深色"),
			("system", "🖥", "跟随系统"),
			("acrylic", "🪟", "亚克力"),
			("liquid-glass", "💧", "液态玻璃")
		};
		_echoThemeModeButtons.Clear();
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
				if (_suppressEchoUiEvents || ViewModel == null) return;
				ViewModel.SetEchoThemeMode(m.Key);
				if (m.Key == "acrylic") ViewModel.SetMaterial(BackgroundMaterial.Acrylic);
				else if (m.Key == "liquid-glass") ViewModel.SetMaterial(BackgroundMaterial.LiquidGlass);
				else if (m.Key == "light") ViewModel.SetTheme(ThemeMode.Light);
				else if (m.Key == "dark") ViewModel.SetTheme(ThemeMode.Dark);
				else if (m.Key == "system") ViewModel.SetTheme(ThemeMode.System);
				SyncLegacyThemeMaterialCombos();
				RefreshEchoAppearanceWorkbenchVisuals(LiquidGlassEngine.IsElementDarkTheme(this));
			};
			_echoThemeModeButtons[m.Key] = mBtn;
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
			Text = "窗口亚克力主题",
			FontSize = 15,
			FontWeight = Microsoft.UI.Text.FontWeights.Bold
		});
		acrylicTextStack.Children.Add(new TextBlock
		{
			Text = "开启后会立即使用系统亚克力材质，让桌面背景从窗口后方透出；界面会保留可读遮罩，Windows 11 22H2 及以上效果最佳。",
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
			Text = "失焦时保持通透",
			FontSize = 13.5,
			FontWeight = Microsoft.UI.Text.FontWeights.SemiBold
		});
		unfocusedLabels.Children.Add(new TextBlock
		{
			Text = "切到别的软件时，不收回窗口亚克力透明效果",
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
			if (_suppressEchoUiEvents || ViewModel == null) return;
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
			Text = "透明度",
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
			if (_suppressEchoUiEvents || ViewModel == null) return;
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
			Text = "0% 为不透明，100% 为最通透",
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
		if (MaterialCombo != null)
		{
			DetachFromParent(MaterialCombo);
			rootStack.Children.Add(MaterialCombo);
		}
		if (WallpaperCombo != null)
		{
			DetachFromParent(WallpaperCombo);
			rootStack.Children.Add(WallpaperCombo);
		}
		if (WallpaperLocalPanel != null)
		{
			DetachFromParent(WallpaperLocalPanel);
			rootStack.Children.Add(WallpaperLocalPanel);
		}

		// 5. 歌词字体与排版
		var lyricFontHeader = new TextBlock
		{
			Text = "歌词字体",
			FontSize = 14,
			FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
			Margin = new Thickness(0, 4, 0, 0)
		};
		rootStack.Children.Add(lyricFontHeader);

		if (LyricFontButton != null)
		{
			DetachFromParent(LyricFontButton);
			rootStack.Children.Add(LyricFontButton);
		}

		var fontButtonsSp = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
		if (_importLyricFontButton != null)
		{
			DetachFromParent(_importLyricFontButton);
			fontButtonsSp.Children.Add(_importLyricFontButton);
		}
		if (_resetLyricFontButton != null)
		{
			DetachFromParent(_resetLyricFontButton);
			fontButtonsSp.Children.Add(_resetLyricFontButton);
		}
		if (fontButtonsSp.Children.Count > 0)
		{
			rootStack.Children.Add(fontButtonsSp);
		}

		if (LyricFontInfoText != null)
		{
			DetachFromParent(LyricFontInfoText);
			rootStack.Children.Add(LyricFontInfoText);
		}

		// 4. "定时切换深色模式" Section
		var scheduleSection = new StackPanel { Spacing = 8, Margin = new Thickness(0, 2, 0, 2) };
		var scheduleHeaderRow = new Grid { ColumnSpacing = 12 };
		scheduleHeaderRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
		scheduleHeaderRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

		var scheduleLabel = new TextBlock
		{
			Text = "定时切换深色模式",
			FontSize = 14.5,
			FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
			VerticalAlignment = VerticalAlignment.Center
		};
		Grid.SetColumn(scheduleLabel, 0);
		scheduleHeaderRow.Children.Add(scheduleLabel);

		_scheduleToggleBtn = new Button
		{
			Content = (ViewModel?.ScheduledDarkModeEnabled ?? false) ? "已启用" : "未启用",
			CornerRadius = new CornerRadius(8),
			Padding = new Thickness(14, 6, 14, 6),
			FontSize = 12.5,
			FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
			VerticalAlignment = VerticalAlignment.Center
		};
		_scheduleToggleBtn.Click += (s, e) =>
		{
			if (_suppressEchoUiEvents || ViewModel == null) return;
			bool next = !ViewModel.ScheduledDarkModeEnabled;
			ViewModel.SetScheduledDarkMode(next, _scheduleDarkStartBox?.Text, _scheduleDarkEndBox?.Text);
			RefreshEchoAppearanceWorkbenchVisuals(LiquidGlassEngine.IsElementDarkTheme(this));
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
			Text = "切到深色",
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
			if (_suppressEchoUiEvents || ViewModel == null) return;
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
			Text = "切回浅色",
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
			if (_suppressEchoUiEvents || ViewModel == null) return;
			ViewModel.SetScheduledDarkMode(ViewModel.ScheduledDarkModeEnabled, _scheduleDarkStartBox?.Text, _scheduleDarkEndBox.Text);
		};
		darkEndPair.Children.Add(_scheduleDarkEndBox);
		scheduleTimesWrap.Children.Add(darkEndPair);

		scheduleSection.Children.Add(scheduleTimesWrap);
		rootStack.Children.Add(scheduleSection);

		// 5. "Rainbow 模式" PRO Row
		var rainbowRow = new Grid { Margin = new Thickness(0, 2, 0, 2) };
		rainbowRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
		rainbowRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

		var rainbowLeft = new StackPanel
		{
			Orientation = Orientation.Horizontal,
			Spacing = 10,
			VerticalAlignment = VerticalAlignment.Center
		};
		rainbowLeft.Children.Add(new TextBlock
		{
			Text = "Rainbow 模式",
			FontSize = 14.5,
			FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
			VerticalAlignment = VerticalAlignment.Center
		});
		rainbowLeft.Children.Add(new Border
		{
			CornerRadius = new CornerRadius(6),
			Padding = new Thickness(7, 2, 7, 2),
			Background = new SolidColorBrush(Color.FromArgb(38, 124, 58, 237)),
			BorderBrush = new SolidColorBrush(Color.FromArgb(110, 124, 58, 237)),
			BorderThickness = new Thickness(1),
			VerticalAlignment = VerticalAlignment.Center,
			Child = new TextBlock
			{
				Text = "PRO",
				FontSize = 10.5,
				FontWeight = Microsoft.UI.Text.FontWeights.Bold,
				Foreground = new SolidColorBrush(Color.FromArgb(255, 139, 92, 246))
			}
		});
		rainbowLeft.Children.Add(new Border
		{
			Width = 38,
			Height = 16,
			CornerRadius = new CornerRadius(8),
			VerticalAlignment = VerticalAlignment.Center,
			Background = new LinearGradientBrush
			{
				StartPoint = new Point(0, 0.5),
				EndPoint = new Point(1, 0.5),
				GradientStops =
				{
					new GradientStop { Color = Color.FromArgb(255, 244, 63, 94), Offset = 0.0 },
					new GradientStop { Color = Color.FromArgb(255, 245, 158, 11), Offset = 0.25 },
					new GradientStop { Color = Color.FromArgb(255, 16, 185, 129), Offset = 0.5 },
					new GradientStop { Color = Color.FromArgb(255, 56, 189, 248), Offset = 0.75 },
					new GradientStop { Color = Color.FromArgb(255, 139, 92, 246), Offset = 1.0 }
				}
			}
		});
		Grid.SetColumn(rainbowLeft, 0);
		rainbowRow.Children.Add(rainbowLeft);

		_rainbowToggle = new ToggleSwitch
		{
			IsOn = ViewModel?.RainbowModeEnabled ?? false,
			OnContent = null,
			OffContent = null,
			MinWidth = 0,
			VerticalAlignment = VerticalAlignment.Center
		};
		_rainbowToggle.Toggled += (s, e) =>
		{
			if (_suppressEchoUiEvents || ViewModel == null) return;
			ViewModel.SetRainbowMode(_rainbowToggle.IsOn);
		};
		Grid.SetColumn(_rainbowToggle, 1);
		rainbowRow.Children.Add(_rainbowToggle);
		rootStack.Children.Add(rainbowRow);

		// 6. "反色模式" Row
		var invertRow = new Grid { Margin = new Thickness(0, 2, 0, 2) };
		invertRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
		invertRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

		invertRow.Children.Add(new TextBlock
		{
			Text = "反色模式",
			FontSize = 14.5,
			FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
			VerticalAlignment = VerticalAlignment.Center
		});
		_invertToggle = new ToggleSwitch
		{
			IsOn = ViewModel?.InvertModeEnabled ?? false,
			OnContent = null,
			OffContent = null,
			MinWidth = 0,
			VerticalAlignment = VerticalAlignment.Center
		};
		_invertToggle.Toggled += (s, e) =>
		{
			if (_suppressEchoUiEvents || ViewModel == null) return;
			ViewModel.SetInvertMode(_invertToggle.IsOn);
			RefreshEchoAppearanceWorkbenchVisuals(LiquidGlassEngine.IsElementDarkTheme(this));
		};
		Grid.SetColumn(_invertToggle, 1);
		invertRow.Children.Add(_invertToggle);
		rootStack.Children.Add(invertRow);

		// 7. "主题预设" Header + Expander Button
		var presetHeaderRow = new Grid { ColumnSpacing = 12, Margin = new Thickness(0, 4, 0, 0) };
		presetHeaderRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
		presetHeaderRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

		presetHeaderRow.Children.Add(new TextBlock
		{
			Text = "主题预设",
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
			Text = (ViewModel?.ThemePresetsExpanded ?? true) ? "收起主题预设 ∧" : "展开主题预设 ∨",
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
				_presetExpanderArrowText.Text = next ? "收起主题预设 ∧" : "展开主题预设 ∨";
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
			Text = "自定义主题",
			FontSize = 14,
			FontWeight = Microsoft.UI.Text.FontWeights.Bold
		});
		customTitleStack.Children.Add(new TextBlock
		{
			Text = "新主题会自动收入「我的」，支持配色编辑和 JSON 参数导入",
			FontSize = 12,
			Opacity = 0.78,
			TextWrapping = TextWrapping.Wrap
		});
		customHeaderStack.Children.Add(customTitleStack);

		var customActionsBar = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
		var editThemeBtn = new Button
		{
			Content = "✎ 进入编辑",
			CornerRadius = new CornerRadius(999),
			Padding = new Thickness(11, 5, 11, 5),
			FontSize = 12,
			FontWeight = Microsoft.UI.Text.FontWeights.SemiBold
		};
		editThemeBtn.Click += async (s, e) => await ShowCustomThemeEditorDialogAsync(editCurrent: true);

		var newThemeBtn = new Button
		{
			Content = "+ 新建我的主题",
			CornerRadius = new CornerRadius(999),
			Padding = new Thickness(11, 5, 11, 5),
			FontSize = 12,
			FontWeight = Microsoft.UI.Text.FontWeights.SemiBold
		};
		newThemeBtn.Click += async (s, e) => await ShowCustomThemeEditorDialogAsync(editCurrent: false);

		var importThemeBtn = new Button
		{
			Content = "📁 导入参数",
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
				Content = currentCat,
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
				RefreshEchoAppearanceWorkbenchVisuals(LiquidGlassEngine.IsElementDarkTheme(this));
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
		RefreshEchoAppearanceWorkbenchVisuals(isDark);
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
			PopulateEchoPresetCardsGrid(LiquidGlassEngine.IsElementDarkTheme(this));
		}
	}

	private void RefreshEchoAppearanceWorkbenchVisuals(bool isDark)
	{
		if (_echoAppearanceCard == null) return;
		_suppressEchoUiEvents = true;
		try
		{
			LiquidGlassEngine.AttachInteractiveLiquidGlass(_echoAppearanceCard, LiquidGlassVariant.Regular, isDark, enablePointerTracking: true);

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
			string currentMode = ViewModel?.EchoThemeMode ?? LiquidGlassEngine.ActiveThemeMode;
			foreach (var kv in _echoThemeModeButtons)
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
				_scheduleToggleBtn.Content = schedOn ? "已启用" : "未启用";
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

			if (_rainbowToggle != null) _rainbowToggle.IsOn = ViewModel?.RainbowModeEnabled ?? false;
			if (_invertToggle != null) _invertToggle.IsOn = ViewModel?.InvertModeEnabled ?? false;

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
				_presetExpanderArrowText.Text = expanded ? "收起主题预设 ∧" : "展开主题预设 ∨";
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

			PopulateEchoPresetCardsGrid(isDark);
		}
		finally
		{
			_suppressEchoUiEvents = false;
		}
	}

	private void PopulateEchoPresetCardsGrid(bool isDark)
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
		List<EchoThemePreset> filtered;
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
			RefreshEchoAppearanceWorkbenchVisuals(LiquidGlassEngine.IsElementDarkTheme(this));
		};
		bannerGrid.Children.Add(refreshBtn);
		banner.Child = bannerGrid;
		stack.Children.Add(banner);

		stack.Children.Add(new TextBlock
		{
			Text = isRandomActive ? $"随机主题 · {activePreset.Name}" : "随机主题",
			FontSize = 14,
			FontWeight = Microsoft.UI.Text.FontWeights.Bold
		});
		stack.Children.Add(new TextBlock
		{
			Text = "随机生成一套颜色并自动调整对比度，保证文字和按钮可读。",
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
			RefreshEchoAppearanceWorkbenchVisuals(LiquidGlassEngine.IsElementDarkTheme(this));
		};
		return card;
	}

	private FrameworkElement CreatePresetCard(EchoThemePreset preset, bool isDark)
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
			RefreshEchoAppearanceWorkbenchVisuals(LiquidGlassEngine.IsElementDarkTheme(this));
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
			Header = "主题名称",
			Text = editCurrent ? (current.Name + " · 定制") : "我的水神主题",
			CornerRadius = new CornerRadius(8)
		};
		var topHexBox = new TextBox
		{
			Header = "背景主色 (Hex)",
			Text = LiquidGlassEngine.ToHexColor(current.StageTopColor),
			CornerRadius = new CornerRadius(8)
		};
		var midHexBox = new TextBox
		{
			Header = "面板渐变色 (Hex)",
			Text = LiquidGlassEngine.ToHexColor(current.StageMidColor),
			CornerRadius = new CornerRadius(8)
		};
		var accentHexBox = new TextBox
		{
			Header = "强调色 (Hex)",
			Text = LiquidGlassEngine.ToHexColor(current.AccentPrimary),
			CornerRadius = new CornerRadius(8)
		};
		var darkSwitch = new ToggleSwitch
		{
			Header = "深色基底主题",
			IsOn = current.IsDark,
			OnContent = "深色",
			OffContent = "浅色"
		};

		var sp = new StackPanel { Spacing = 10, MinWidth = 340 };
		sp.Children.Add(nameBox);
		sp.Children.Add(topHexBox);
		sp.Children.Add(midHexBox);
		sp.Children.Add(accentHexBox);
		sp.Children.Add(darkSwitch);

		var dialog = new ContentDialog
		{
			Title = editCurrent ? "编辑并保存为我的主题" : "新建我的主题",
			Content = sp,
			PrimaryButtonText = "保存并应用",
			CloseButtonText = "取消",
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
			RefreshEchoAppearanceWorkbenchVisuals(LiquidGlassEngine.IsElementDarkTheme(this));
		}
	}

	private async System.Threading.Tasks.Task ShowImportThemeParamsDialogAsync()
	{
		if (XamlRoot == null || ViewModel == null) return;
		string currentJson = LiquidGlassEngine.SerializeCustomPresets();
		if (string.IsNullOrWhiteSpace(currentJson) || currentJson == "[]")
		{
			var p = LiquidGlassEngine.ActivePreset;
			currentJson = $"[{{\"id\":\"custom-import\",\"name\":\"{p.Name} (导入)\",\"category\":\"我的\",\"desc\":\"JSON 导入主题预设\",\"isDark\":\"{(p.IsDark ? "1" : "0")}\",\"top\":\"{LiquidGlassEngine.ToHexColor(p.StageTopColor)}\",\"mid\":\"{LiquidGlassEngine.ToHexColor(p.StageMidColor)}\",\"pri\":\"{LiquidGlassEngine.ToHexColor(p.AccentPrimary)}\"}}]";
		}

		var jsonBox = new TextBox
		{
			Header = "主题预设 JSON 参数（支持复制导出或粘贴导入）",
			Text = currentJson,
			AcceptsReturn = true,
			TextWrapping = TextWrapping.Wrap,
			Height = 160,
			CornerRadius = new CornerRadius(8)
		};

		var dialog = new ContentDialog
		{
			Title = "导入 / 导出自定义主题参数",
			Content = jsonBox,
			PrimaryButtonText = "导入并应用",
			CloseButtonText = "取消",
			DefaultButton = ContentDialogButton.Primary,
			XamlRoot = XamlRoot
		};

		if (await dialog.ShowAsync() == ContentDialogResult.Primary)
		{
			if (ViewModel.ImportCustomThemePresetJson(jsonBox.Text))
			{
				SyncLegacyThemeMaterialCombos();
				RefreshEchoAppearanceWorkbenchVisuals(LiquidGlassEngine.IsElementDarkTheme(this));
			}
		}
	}
}
