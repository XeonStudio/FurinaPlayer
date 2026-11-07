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
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.System;

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

	private void ApplySettingsLiquidGlass()
	{
		try
		{
			bool isDark = LiquidGlassEngine.IsElementDarkTheme(this);
			Background = new SolidColorBrush(Colors.Transparent);

			if (_ambientCausticCanvas == null)
			{
				_ambientCausticCanvas = LiquidGlassEngine.CreateAmbientCausticCanvas(() => LiquidGlassEngine.IsElementDarkTheme(this));
				Grid.SetRow(_ambientCausticCanvas, 0);
				Grid.SetColumn(_ambientCausticCanvas, 0);
				Grid.SetRowSpan(_ambientCausticCanvas, 12);
				Grid.SetColumnSpan(_ambientCausticCanvas, 12);
				Canvas.SetZIndex(_ambientCausticCanvas, -10);
			}

			if (Content is ScrollViewer sv)
			{
				_settingsScrollViewer = sv;
				sv.Background = new SolidColorBrush(Colors.Transparent);
				_rootStageGrid = new Grid
				{
					Background = new SolidColorBrush(Colors.Transparent)
				};
				Content = null;
				_rootStageGrid.Children.Insert(0, _ambientCausticCanvas);
				_rootStageGrid.Children.Add(sv);
				Content = _rootStageGrid;
			}
			else if (Content is Grid existingRootGrid)
			{
				_rootStageGrid = existingRootGrid;
				existingRootGrid.Background = new SolidColorBrush(Colors.Transparent);
				if (!existingRootGrid.Children.Contains(_ambientCausticCanvas))
				{
					existingRootGrid.Children.Insert(0, _ambientCausticCanvas);
				}
				else if (existingRootGrid.Children.IndexOf(_ambientCausticCanvas) != 0)
				{
					existingRootGrid.Children.Remove(_ambientCausticCanvas);
					existingRootGrid.Children.Insert(0, _ambientCausticCanvas);
				}
			}

			_ambientCausticCanvas.Invalidate();

			LiquidGlassEngine.UpgradeVisualTreeToLiquidGlass(this, isDark);

			ScrollViewer? targetSv = ResolveSettingsScrollViewer();
			if (targetSv?.Content is Grid settingsGrid)
			{
				settingsGrid.Background = new SolidColorBrush(Colors.Transparent);
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
		if (root == null) return;

		int count = VisualTreeHelper.GetChildrenCount(root);
		for (int i = 0; i < count; i++)
		{
			DependencyObject child = VisualTreeHelper.GetChild(root, i);
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
			ScrollViewer? sv = ResolveSettingsScrollViewer();
			if (sv != null && sv.Content is Grid settingsGrid)
			{
				List<FrameworkElement> panels = settingsGrid.Children
					.OfType<FrameworkElement>()
					.Where(c => c is not SKXamlCanvas)
					.ToList();

				if (panels.Count >= 3)
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
		}
		catch (Exception)
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
			AssemblyInformationalVersionAttribute customAttribute = Assembly.GetEntryAssembly().GetCustomAttribute<AssemblyInformationalVersionAttribute>();
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
}
