using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using FurinaPlayer.UI.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Markup;
using Microsoft.UI.Xaml.Navigation;
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
	}

	public SettingsPage(SettingsViewModel viewModel)
		: this()
	{
		ViewModel = viewModel;
		DataContext = viewModel;
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
		try
		{
			await ViewModel.FetchMissingAlbumCoverAsync();
		}
		catch (Exception ex)
		{
			ViewModel.AlbumArtStatus = "操作异常：" + ex.Message;
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
			target.As<Button>().Click += OnOpenCrashFolderClick;
			break;
		case 4:
			OssList = target.As<StackPanel>();
			break;
		case 5:
			target.As<Button>().Click += OnFetchAlbumArtClick;
			break;
		case 6:
			target.As<Button>().Click += OnAddFolderClick;
			break;
		case 7:
			target.As<Button>().Click += OnRemoveFolderClick;
			break;
		case 8:
			target.As<Button>().Click += OnScanNowClick;
			break;
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
			target.As<Button>().Click += OnImportLyricFontClick;
			break;
		case 16:
			target.As<Button>().Click += OnResetLyricFontClick;
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
			target.As<Button>().Click += OnPickWallpaperClick;
			break;
		case 22:
			WallpaperPathText = target.As<TextBlock>();
			break;
		case 23:
			target.As<Button>().Click += OnSaveClick;
			break;
		case 24:
			target.As<Button>().Click += OnRestoreDefaultsClick;
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
			target.As<Button>().Click += OnRefreshDevicesClick;
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
