using System;
using System.CodeDom.Compiler;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using FurinaPlayer.UI.Controls;
using FurinaPlayer.UI.Pages;
using FurinaPlayer.UI.ViewModels;
using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Markup;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI.Xaml.Navigation;
using SkiaSharp;
using SkiaSharp.Views.Windows;
using SonicWave.Audio;
using SonicWave.Audio.Vst3;
using SonicWave.Core.Models;
using WinRT;
using WinRT.Interop;
using Windows.Graphics;
using Windows.UI;

namespace SonicWave.App;

[WinRTRuntimeClassName("Microsoft.UI.Xaml.Markup.IComponentConnector")]
[WinRTExposedType(typeof(MainWindowWinRTTypeDetails))]
public sealed class MainWindow : Window, IComponentConnector
{
	private readonly AudioEngine _engine;

	private readonly HomeViewModel _homeVm;

	private readonly NowPlayingViewModel _nowPlayingVm;

	private readonly EditorViewModel _editorVm;

	private readonly SettingsViewModel _settingsVm;

	private readonly DspViewModel _dspVm;

	private bool _placementRestored;

	private BackgroundMaterial _currentMaterial;

	private bool _isDarkTheme;

	private float _pointerNormX = 0.5f;

	private float _pointerNormY = 0.25f;

	private string _currentNavTag = "home";

	[GeneratedCode("Microsoft.UI.Xaml.Markup.Compiler", " 3.0.0.2409")]
	private Grid RootGrid;

	[GeneratedCode("Microsoft.UI.Xaml.Markup.Compiler", " 3.0.0.2409")]
	private Grid LiquidGlassOverlay;

	[GeneratedCode("Microsoft.UI.Xaml.Markup.Compiler", " 3.0.0.2409")]
	private SKXamlCanvas GlassSkiaCanvas;

	[GeneratedCode("Microsoft.UI.Xaml.Markup.Compiler", " 3.0.0.2409")]
	private Grid TitleBar;

	[GeneratedCode("Microsoft.UI.Xaml.Markup.Compiler", " 3.0.0.2409")]
	private NavigationView Nav;

	[GeneratedCode("Microsoft.UI.Xaml.Markup.Compiler", " 3.0.0.2409")]
	private MiniPlayer MiniPlayer;

	[GeneratedCode("Microsoft.UI.Xaml.Markup.Compiler", " 3.0.0.2409")]
	private Frame ContentFrame;

	[GeneratedCode("Microsoft.UI.Xaml.Markup.Compiler", " 3.0.0.2409")]
	private Button MinButton;

	[GeneratedCode("Microsoft.UI.Xaml.Markup.Compiler", " 3.0.0.2409")]
	private Button MaxButton;

	[GeneratedCode("Microsoft.UI.Xaml.Markup.Compiler", " 3.0.0.2409")]
	private Button CloseButton;

	[GeneratedCode("Microsoft.UI.Xaml.Markup.Compiler", " 3.0.0.2409")]
	private bool _contentLoaded;

	public event Func<Task>? WallpaperRefreshRequested;

	public event Action? SessionClosing;

	public Frame MainContentFrame => ContentFrame;

	public MainWindow(HomeViewModel homeVm, NowPlayingViewModel nowPlayingVm, EditorViewModel editorVm, SettingsViewModel settingsVm, DspViewModel dspVm, AudioEngine engine)
	{
		InitializeComponent();
		_engine = engine;
		_homeVm = homeVm;
		_nowPlayingVm = nowPlayingVm;
		_editorVm = editorVm;
		_settingsVm = settingsVm;
		_dspVm = dspVm;
		MiniPlayer.DataContext = nowPlayingVm;
		this.MiniPlayer.NavigateToPlayingRequested += () => NavigateTo("playing");
		FurinaPlayer.UI.Controls.MiniPlayer.OpenNowPlayingRequested += () => NavigateTo("playing");
		Title = "Furina player · 全格式无损音乐播放器";
		ApplyMaterial(_settingsVm.Material);
		Activated += OnFirstActivated;
		ExtendsContentIntoTitleBar = true;
		SetTitleBar(TitleBar);
		nint hwnd = WindowNative.GetWindowHandle(this);
		DwmGlass.RemoveBorderColor(hwnd);
		if (AppWindowTitleBar.IsCustomizationSupported())
		{
			var tb = AppWindow.TitleBar;
			tb.ExtendsContentIntoTitleBar = true;
			tb.ButtonBackgroundColor = Windows.UI.Color.FromArgb(0, 0, 0, 0);
			tb.ButtonInactiveBackgroundColor = Windows.UI.Color.FromArgb(0, 0, 0, 0);
			tb.ButtonHoverBackgroundColor = Windows.UI.Color.FromArgb(40, 128, 128, 128);
			tb.ButtonPressedBackgroundColor = Windows.UI.Color.FromArgb(70, 128, 128, 128);
			UpdateTitleBarButtonColors(ElementTheme.Default);
		}
		if (AppWindow.Presenter is OverlappedPresenter overlappedPresenter)
		{
			overlappedPresenter.SetBorderAndTitleBar(hasBorder: true, hasTitleBar: true);
		}
		if (TitleBar != null)
		{
			TitleBar.Margin = new Thickness(0);
			TitleBar.Padding = new Thickness(0);
		}
		if (RootGrid != null)
		{
			RootGrid.Margin = new Thickness(0);
			RootGrid.Padding = new Thickness(0);
			RootGrid.PointerMoved -= OnRootGridPointerMoved;
			RootGrid.PointerMoved += OnRootGridPointerMoved;
			RootGrid.Loaded += (object _, RoutedEventArgs _) =>
			{
				ApplyWindowBackdrop();
			};
		}
		if (ContentFrame != null)
		{
			ContentFrame.Navigated -= OnContentFrameNavigated;
			ContentFrame.Navigated += OnContentFrameNavigated;
		}
		if (MinButton != null) MinButton.Visibility = Visibility.Collapsed;
		if (MaxButton != null) MaxButton.Visibility = Visibility.Collapsed;
		if (CloseButton != null) CloseButton.Visibility = Visibility.Collapsed;
		Closed += (object _, WindowEventArgs _) =>
		{
			SaveWindowPlacement();
			SessionClosing?.Invoke();
			_engine.Dispose();
			Vst3NativeHost.Instance.Unload();
		};
		ApplyFluentWindowIcons();
		ApplyWindowBackdrop();
		Nav.SelectedItem = Nav.MenuItems[0];
		UpdateMaxIcon();
		// Auto capture sequence is managed by App.CapturePagesAsync to avoid dual-runner conflict
	}

	private void ApplyFluentWindowIcons()
	{
		try
		{
			if (TitleBar != null)
			{
				FluentIconFactory.UpgradeButtonsInVisualTree(TitleBar);
			}
			if (MinButton != null) MinButton.Visibility = Visibility.Collapsed;
			if (MaxButton != null) MaxButton.Visibility = Visibility.Collapsed;
			if (CloseButton != null) CloseButton.Visibility = Visibility.Collapsed;

			if (Nav != null)
			{
				foreach (var obj in Nav.MenuItems)
				{
					if (obj is NavigationViewItem item && item.Tag is string tag)
					{
						UpgradeNavItemIcon(item, tag);
					}
				}
				foreach (var obj in Nav.FooterMenuItems)
				{
					if (obj is NavigationViewItem item && item.Tag is string tag)
					{
						UpgradeNavItemIcon(item, tag);
					}
				}
			}
		}
		catch (Exception ex)
		{
			Debug.WriteLine($"ApplyFluentWindowIcons error: {ex.Message}");
		}
	}

	private static void UpgradeNavItemIcon(NavigationViewItem item, string tag)
	{
		FluentIconKind kind = tag switch
		{
			"home" => FluentIconKind.Home,
			"playing" => FluentIconKind.NowPlaying,
			"editor" => FluentIconKind.AudioEditor,
			"dsp" => FluentIconKind.DspEffects,
			"settings" => FluentIconKind.Settings,
			_ => FluentIconKind.AppLogo
		};
		string label = item.Content as string ?? tag;
		item.Icon = null;
		var sp = new StackPanel
		{
			Orientation = Orientation.Horizontal,
			Spacing = 12,
			VerticalAlignment = VerticalAlignment.Center
		};
		sp.Children.Add(FluentIconFactory.Create(kind, 20));
		sp.Children.Add(new TextBlock
		{
			Text = label,
			VerticalAlignment = VerticalAlignment.Center,
			FontWeight = Microsoft.UI.Text.FontWeights.Medium
		});
		item.Content = sp;
	}

	private void OnFirstActivated(object sender, WindowActivatedEventArgs args)
	{
		if (!_placementRestored)
		{
			_placementRestored = true;
			try
			{
				Activated -= OnFirstActivated;
			}
			catch
			{
			}
			RestoreWindowPlacement();
		}
	}

	private void RestoreWindowPlacement()
	{
		try
		{
			AppSettings settings = _settingsVm.Settings;
			int? windowWidth = settings.WindowWidth;
			if (windowWidth.HasValue)
			{
				int valueOrDefault = windowWidth.GetValueOrDefault();
				if (valueOrDefault >= 800)
				{
					windowWidth = settings.WindowHeight;
					if (windowWidth.HasValue)
					{
						int valueOrDefault2 = windowWidth.GetValueOrDefault();
						if (valueOrDefault2 >= 500)
						{
							windowWidth = settings.WindowLeft;
							if (windowWidth.HasValue)
							{
								int valueOrDefault3 = windowWidth.GetValueOrDefault();
								windowWidth = settings.WindowTop;
								if (windowWidth.HasValue)
								{
									int valueOrDefault4 = windowWidth.GetValueOrDefault();
									if (valueOrDefault3 >= -20000 && valueOrDefault3 <= 20000 && valueOrDefault4 >= -20000 && valueOrDefault4 <= 20000 && valueOrDefault <= 4000 && valueOrDefault2 <= 4000)
									{
										RECT lprc = new RECT
										{
											Left = valueOrDefault3,
											Top = valueOrDefault4,
											Right = valueOrDefault3 + valueOrDefault,
											Bottom = valueOrDefault4 + valueOrDefault2
										};
										if (MonitorFromRect(ref lprc, 0u) != IntPtr.Zero)
										{
											AppWindow.MoveAndResize(new RectInt32(valueOrDefault3, valueOrDefault4, valueOrDefault, valueOrDefault2));
											if (settings.WindowMaximized && AppWindow.Presenter is OverlappedPresenter overlappedPresenter)
											{
												overlappedPresenter.Maximize();
											}
											return;
										}
									}
								}
							}
						}
					}
				}
			}
		}
		catch
		{
		}
		AppWindow.Resize(new SizeInt32(1180, 760));
		try
		{
			RectInt32 workArea = DisplayArea.GetFromWindowId(AppWindow.Id, DisplayAreaFallback.Nearest).WorkArea;
			AppWindow.Move(new PointInt32(workArea.X + Math.Max(0, (workArea.Width - 1180) / 2), workArea.Y + Math.Max(0, (workArea.Height - 760) / 2)));
		}
		catch
		{
		}
	}

	private void SaveWindowPlacement()
	{
		try
		{
			WINDOWPLACEMENT lpwndpl = new WINDOWPLACEMENT
			{
				length = Marshal.SizeOf<WINDOWPLACEMENT>()
			};
			if (GetWindowPlacement(WindowNative.GetWindowHandle(this), ref lpwndpl))
			{
				RECT r = lpwndpl.rcNormalPosition;
				bool maximized = AppWindow.Presenter is OverlappedPresenter overlappedPresenter && overlappedPresenter.State == OverlappedPresenterState.Maximized;
				_settingsVm.UpdateSettings((AppSettings s) =>
				{
					s.WindowLeft = r.Left;
					s.WindowTop = r.Top;
					s.WindowWidth = Math.Max(800, r.Right - r.Left);
					s.WindowHeight = Math.Max(500, r.Bottom - r.Top);
					s.WindowMaximized = maximized;
				});
			}
		}
		catch
		{
		}
	}

	[DllImport("user32.dll", SetLastError = true)]
	private static extern bool GetWindowPlacement(nint hWnd, ref WINDOWPLACEMENT lpwndpl);

	[DllImport("user32.dll")]
	private static extern nint MonitorFromRect(ref RECT lprc, uint dwFlags);

	private void OnMinimizeClick(object sender, RoutedEventArgs e)
	{
		if (AppWindow.Presenter is OverlappedPresenter overlappedPresenter)
		{
			overlappedPresenter.Minimize();
		}
	}

	private void OnMaximizeClick(object sender, RoutedEventArgs e)
	{
		if (AppWindow.Presenter is OverlappedPresenter overlappedPresenter)
		{
			if (overlappedPresenter.State == OverlappedPresenterState.Maximized)
			{
				overlappedPresenter.Restore();
			}
			else
			{
				overlappedPresenter.Maximize();
			}
		}
		UpdateMaxIcon();
	}

	private void UpdateMaxIcon()
	{
		if (MaxButton != null)
		{
			MaxButton.Visibility = Visibility.Collapsed;
		}
	}

	private void OnCloseClick(object sender, RoutedEventArgs e)
	{
		Close();
	}

	public void ApplyTheme(ThemeMode mode)
	{
		Grid rootGrid = RootGrid;
		rootGrid.RequestedTheme = mode switch
		{
			ThemeMode.Dark => ElementTheme.Dark, 
			ThemeMode.Light => ElementTheme.Light, 
			_ => ElementTheme.Default, 
		};
		_isDarkTheme = mode switch
		{
			ThemeMode.Dark => true,
			ThemeMode.Light => false,
			_ => LiquidGlassEngine.IsElementDarkTheme(rootGrid)
		};
		UpdateTitleBarButtonColors(rootGrid.RequestedTheme);
		ApplyMaterial(_currentMaterial);
	}

	private void UpdateTitleBarButtonColors(ElementTheme theme)
	{
		if (!AppWindowTitleBar.IsCustomizationSupported()) return;
		try
		{
			var tb = AppWindow.TitleBar;
			bool isDark = theme == ElementTheme.Dark || (theme == ElementTheme.Default && Application.Current.RequestedTheme == ApplicationTheme.Dark);
			_isDarkTheme = isDark;
			if (isDark)
			{
				tb.ButtonForegroundColor = Windows.UI.Color.FromArgb(255, 230, 235, 245);
				tb.ButtonHoverForegroundColor = Windows.UI.Color.FromArgb(255, 255, 255, 255);
				tb.ButtonInactiveForegroundColor = Windows.UI.Color.FromArgb(140, 180, 190, 205);
			}
			else
			{
				tb.ButtonForegroundColor = Windows.UI.Color.FromArgb(255, 30, 36, 46);
				tb.ButtonHoverForegroundColor = Windows.UI.Color.FromArgb(255, 0, 0, 0);
				tb.ButtonInactiveForegroundColor = Windows.UI.Color.FromArgb(140, 100, 110, 125);
			}
		}
		catch
		{
		}
	}

	private void ApplyWindowBackdrop()
	{
		try
		{
			if (RootGrid != null && RootGrid.RequestedTheme != ElementTheme.Default)
			{
				_isDarkTheme = RootGrid.RequestedTheme == ElementTheme.Dark;
			}
			else
			{
				_isDarkTheme = LiquidGlassEngine.IsElementDarkTheme(RootGrid);
			}

			if (TitleBar != null)
			{
				TitleBar.Background = LiquidGlassEngine.CreateSurfaceBrush(LiquidGlassVariant.FloatingBar, _isDarkTheme, 0.45);
			}
			if (Nav != null)
			{
				Nav.Background = LiquidGlassEngine.CreateSurfaceBrush(LiquidGlassVariant.FloatingBar, _isDarkTheme, 0.45);
			}
		}
		catch
		{
		}
	}

	private void OnRootGridPointerMoved(object sender, PointerRoutedEventArgs e)
	{
		if (RootGrid == null || RootGrid.ActualWidth <= 0 || RootGrid.ActualHeight <= 0)
		{
			return;
		}
		if (_currentNavTag == "playing")
		{
			return;
		}
		var pt = e.GetCurrentPoint(RootGrid).Position;
		float nx = (float)Math.Clamp(pt.X / RootGrid.ActualWidth, 0.0, 1.0);
		float ny = (float)Math.Clamp(pt.Y / RootGrid.ActualHeight, 0.0, 1.0);
		if (Math.Abs(nx - _pointerNormX) > 0.008f || Math.Abs(ny - _pointerNormY) > 0.008f)
		{
			_pointerNormX = nx;
			_pointerNormY = ny;
			if (GlassSkiaCanvas != null && GlassSkiaCanvas.Visibility == Visibility.Visible)
			{
				GlassSkiaCanvas.Invalidate();
			}
		}
	}

	public void ApplyMaterial(BackgroundMaterial material)
	{
		_currentMaterial = material;
		nint windowHandle = WindowNative.GetWindowHandle(this);
		DwmGlass.RemoveBorderColor(windowHandle);
		DwmGlass.SetImmersiveDarkMode(windowHandle, _isDarkTheme);
		try
		{
			if (material == BackgroundMaterial.LiquidGlassDwm)
			{
				SystemBackdrop = null;
				DwmGlass.ApplyTransientBackdrop(windowHandle);
			}
			else
			{
				DwmGlass.ClearBackdrop(windowHandle);
				SystemBackdrop = ((material == BackgroundMaterial.LiquidGlass) ? ((SystemBackdrop)new DesktopAcrylicBackdrop()) : ((SystemBackdrop)new MicaBackdrop()));
			}
		}
		catch
		{
			try
			{
				SystemBackdrop = new MicaBackdrop();
			}
			catch
			{
			}
		}
		ApplyWindowBackdrop();
		if (RootGrid != null)
		{
			RootGrid.Background = new SolidColorBrush(Colors.Transparent);
		}
		if (_currentNavTag == "playing")
		{
			if (LiquidGlassOverlay != null)
			{
				LiquidGlassOverlay.Visibility = Visibility.Collapsed;
			}
			if (GlassSkiaCanvas != null)
			{
				GlassSkiaCanvas.Visibility = Visibility.Collapsed;
			}
		}
		else
		{
			if (LiquidGlassOverlay != null)
			{
				LiquidGlassOverlay.Visibility = Visibility.Visible;
			}
			if (GlassSkiaCanvas != null)
			{
				GlassSkiaCanvas.Visibility = Visibility.Visible;
				GlassSkiaCanvas.Invalidate();
			}
		}
	}

	private void OnGlassSkiaPaint(object? sender, SKPaintSurfaceEventArgs e)
	{
		LiquidGlassEngine.RenderAmbientCausticStage(
			e.Surface.Canvas,
			e.Info.Width,
			e.Info.Height,
			isDark: _isDarkTheme,
			_pointerNormX,
			_pointerNormY);
	}

	public void ApplyWallpaper(string? imagePath, bool solid)
	{
		try
		{
			if (_currentNavTag == "playing" || solid)
			{
				if (RootGrid != null)
				{
					RootGrid.Background = new SolidColorBrush(Colors.Transparent);
				}
				return;
			}
			if (!string.IsNullOrWhiteSpace(imagePath) && File.Exists(imagePath))
			{
				BitmapImage imageSource = new BitmapImage(new Uri(imagePath));
				RootGrid.Background = new ImageBrush
				{
					ImageSource = imageSource,
					Stretch = Stretch.UniformToFill,
					AlignmentX = AlignmentX.Center,
					AlignmentY = AlignmentY.Center
				};
				return;
			}
		}
		catch
		{
		}
		if (RootGrid != null)
		{
			RootGrid.Background = new SolidColorBrush(Colors.Transparent);
		}
	}

	private void OnContentFrameNavigated(object sender, NavigationEventArgs e)
	{
		string tag = e.SourcePageType == typeof(NowPlayingPage) ? "playing"
			: e.SourcePageType == typeof(EditorPage) ? "editor"
			: e.SourcePageType == typeof(DspPage) ? "dsp"
			: e.SourcePageType == typeof(SettingsPage) ? "settings"
			: "home";
		ApplyStageGlassByTag(tag);
	}

	private void ApplyStageGlassByTag(string tag)
	{
		_currentNavTag = tag;
		ApplyWindowBackdrop();
		if (tag == "playing")
		{
			if (LiquidGlassOverlay != null)
			{
				LiquidGlassOverlay.Visibility = Visibility.Collapsed;
			}
			if (GlassSkiaCanvas != null)
			{
				GlassSkiaCanvas.Visibility = Visibility.Collapsed;
			}
			if (RootGrid != null)
			{
				RootGrid.Background = new SolidColorBrush(Colors.Transparent);
			}
		}
		else
		{
			if (LiquidGlassOverlay != null)
			{
				LiquidGlassOverlay.Visibility = Visibility.Visible;
			}
			if (GlassSkiaCanvas != null)
			{
				GlassSkiaCanvas.Visibility = Visibility.Visible;
				GlassSkiaCanvas.Invalidate();
			}
			if (RootGrid != null)
			{
				RootGrid.Background = new SolidColorBrush(Colors.Transparent);
			}
		}
	}

	private void OnNavSelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
	{
		try
		{
			if (!(args.SelectedItem is NavigationViewItem { Tag: string tag }))
			{
				return;
			}
			Log("navigate: " + tag);
			ApplyStageGlassByTag(tag);
			switch (tag)
			{
			case "home":
				ContentFrame.Navigate(typeof(HomePage), _homeVm, new SuppressNavigationTransitionInfo());
				break;
			case "playing":
				ContentFrame.Navigate(typeof(NowPlayingPage), _nowPlayingVm, new SuppressNavigationTransitionInfo());
				break;
			case "editor":
				ContentFrame.Navigate(typeof(EditorPage), _editorVm, new SuppressNavigationTransitionInfo());
				break;
			case "dsp":
				ContentFrame.Navigate(typeof(DspPage), _dspVm, new SuppressNavigationTransitionInfo());
				break;
			case "settings":
				ContentFrame.Navigate(typeof(SettingsPage), _settingsVm, new SuppressNavigationTransitionInfo());
				if (!(ContentFrame.Content is SettingsPage settingsPage))
				{
					break;
				}
				settingsPage.ScanRequested += (string[] folders) =>
				{
					foreach (string parameter in folders)
					{
						_homeVm.ScanFolderCommand.ExecuteAsync(parameter);
					}
				};
				settingsPage.SettingsSaved += async () =>
				{
					if (WallpaperRefreshRequested != null)
					{
						await WallpaperRefreshRequested();
					}
				};
				break;
			}
		}
		catch (Exception ex)
		{
			StringBuilder stringBuilder = new StringBuilder();
			for (Exception ex2 = ex; ex2 != null; ex2 = ex2.InnerException)
			{
				stringBuilder.Append(" [" + ex2.GetType().Name + " hr=0x" + ex2.HResult.ToString("X8") + " msg=" + ex2.Message + "]");
			}
			Log("navigate failed:" + stringBuilder?.ToString() + Environment.NewLine + ex);
		}
	}

	public void NavigateTo(string tag)
	{
		try
		{
			if (tag == "playing")
			{
				if (LiquidGlassOverlay != null)
				{
					LiquidGlassOverlay.Visibility = Visibility.Collapsed;
				}
				if (GlassSkiaCanvas != null)
				{
					GlassSkiaCanvas.Visibility = Visibility.Collapsed;
				}
				if (RootGrid != null)
				{
					RootGrid.Background = new SolidColorBrush(Colors.Transparent);
				}
			}
			else
			{
				if (LiquidGlassOverlay != null)
				{
					LiquidGlassOverlay.Visibility = Visibility.Visible;
				}
				if (GlassSkiaCanvas != null)
				{
					GlassSkiaCanvas.Visibility = Visibility.Visible;
					GlassSkiaCanvas.Invalidate();
				}
				if (RootGrid != null)
				{
					RootGrid.Background = new SolidColorBrush(Colors.Transparent);
				}
			}
			ApplyStageGlassByTag(tag);

			if (Nav != null && Nav.MenuItems != null)
			{
				foreach (var item in Nav.MenuItems)
				{
					if (item is NavigationViewItem nvi && (string?)nvi.Tag == tag)
					{
						if (Nav.SelectedItem != nvi)
						{
							Nav.SelectedItem = nvi;
							return;
						}
						break;
					}
				}
			}

			switch (tag)
			{
			case "home":
				ContentFrame.Navigate(typeof(HomePage), _homeVm, new SuppressNavigationTransitionInfo());
				break;
			case "playing":
				ContentFrame.Navigate(typeof(NowPlayingPage), _nowPlayingVm, new SuppressNavigationTransitionInfo());
				break;
			case "editor":
				ContentFrame.Navigate(typeof(EditorPage), _editorVm, new SuppressNavigationTransitionInfo());
				break;
			case "dsp":
				ContentFrame.Navigate(typeof(DspPage), _dspVm, new SuppressNavigationTransitionInfo());
				break;
			case "settings":
				ContentFrame.Navigate(typeof(SettingsPage), _settingsVm, new SuppressNavigationTransitionInfo());
				break;
			}
		}
		catch (Exception ex)
		{
			Log("NavigateTo failed: " + ex);
		}
	}

	private static void Log(string s)
	{
		try
		{
			File.AppendAllText(Path.Combine(AppContext.BaseDirectory, "sw_crash.log"), DateTime.Now.ToString("HH:mm:ss.fff") + " " + s + Environment.NewLine);
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
			Uri resourceLocator = new Uri("ms-appx:///MainWindow.xaml");
			Application.LoadComponent(this, resourceLocator, ComponentResourceLocation.Application);
		}
	}

	[GeneratedCode("Microsoft.UI.Xaml.Markup.Compiler", " 3.0.0.2409")]
	[DebuggerNonUserCode]
	public void Connect(int connectionId, object target)
	{
		switch (connectionId)
		{
		case 2:
			RootGrid = target.As<Grid>();
			break;
		case 3:
			LiquidGlassOverlay = target.As<Grid>();
			break;
		case 4:
			GlassSkiaCanvas = target.As<SKXamlCanvas>();
			GlassSkiaCanvas.PaintSurface += OnGlassSkiaPaint;
			break;
		case 5:
			TitleBar = target.As<Grid>();
			break;
		case 6:
			Nav = target.As<NavigationView>();
			Nav.SelectionChanged += OnNavSelectionChanged;
			break;
		case 7:
			MiniPlayer = target.As<MiniPlayer>();
			break;
		case 8:
			ContentFrame = target.As<Frame>();
			break;
		case 9:
			MinButton = target.As<Button>();
			MinButton.Click += OnMinimizeClick;
			MinButton.Visibility = Visibility.Collapsed;
			break;
		case 10:
			MaxButton = target.As<Button>();
			MaxButton.Click += OnMaximizeClick;
			MaxButton.Visibility = Visibility.Collapsed;
			break;
		case 11:
			CloseButton = target.As<Button>();
			CloseButton.Click += OnCloseClick;
			CloseButton.Visibility = Visibility.Collapsed;
			break;
		}
		_contentLoaded = true;
	}

	private void CheckCommandLineAndRunAutoCapture()
	{
		string[] args = Environment.GetCommandLineArgs();
		if (args.Any(a => a.Equals("--capture-pages", StringComparison.OrdinalIgnoreCase)))
		{
			DispatcherQueue.TryEnqueue(async () =>
			{
				await RunAutoCaptureSequenceAsync();
			});
		}
	}

	private async Task RunAutoCaptureSequenceAsync()
	{
		try
		{
			string outDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Screenshots");
			string artifactDir = @"C:\Users\Xeon\.gemini\antigravity\brain\083a6d72-f694-4402-b683-fad78cc28269";
			if (!Directory.Exists(outDir)) Directory.CreateDirectory(outDir);

			await Task.Delay(1500);

			// 1. Home Page
			await CaptureCurrentWindowAsync(Path.Combine(outDir, "home.png"), Path.Combine(artifactDir, "home.png"));

			// 2. NowPlaying Page - Audition DAW
			ContentFrame.Navigate(typeof(NowPlayingPage), _nowPlayingVm, new SuppressNavigationTransitionInfo());
			await Task.Delay(1500);
			if (ContentFrame.Content is NowPlayingPage npPage)
			{
				npPage.SetLyricsMode(NowPlayingPage.LyricsPageMode.AuditionDaw);
				await Task.Delay(1500);
				try
				{
					await Task.WhenAny(npPage.CaptureDawPreviewAsync(Path.Combine(outDir, "playing_audition.png")), Task.Delay(1500));
				}
				catch {}
				await CaptureCurrentWindowAsync(Path.Combine(outDir, "playing_audition_full.png"), Path.Combine(artifactDir, "playing_audition.png"));

				// 3. NowPlaying Page - PowerPoint Theme
				npPage.SetLyricsMode(NowPlayingPage.LyricsPageMode.PowerPoint);
				await Task.Delay(1500);
				try
				{
					await Task.WhenAny(npPage.CaptureDawPreviewAsync(Path.Combine(outDir, "playing_powerpoint.png")), Task.Delay(1500));
				}
				catch {}
				await CaptureCurrentWindowAsync(Path.Combine(outDir, "playing_powerpoint_full.png"), Path.Combine(artifactDir, "playing_powerpoint.png"));

				// 4. NowPlaying Page - Classic Centered Mode
				npPage.SetLyricsMode(NowPlayingPage.LyricsPageMode.Classic);
				await Task.Delay(1500);
				await CaptureCurrentWindowAsync(Path.Combine(outDir, "playing_classic.png"), Path.Combine(artifactDir, "playing_classic.png"));
			}

			// 5. DSP Page
			ContentFrame.Navigate(typeof(DspPage), _dspVm, new SuppressNavigationTransitionInfo());
			await Task.Delay(1500);
			await CaptureCurrentWindowAsync(Path.Combine(outDir, "dsp.png"), Path.Combine(artifactDir, "dsp.png"));

			// 6. Settings Page
			ContentFrame.Navigate(typeof(SettingsPage), _settingsVm, new SuppressNavigationTransitionInfo());
			await Task.Delay(1000);
			await CaptureCurrentWindowAsync(Path.Combine(outDir, "settings.png"), Path.Combine(artifactDir, "settings.png"));

			await Task.Delay(500);
			Close();
		}
		catch (Exception ex)
		{
			Debug.WriteLine($"Auto capture failed: {ex.Message}");
			Close();
		}
	}

	private async Task CaptureCurrentWindowAsync(string filePath, string? artifactCopyPath = null)
	{
		try
		{
			var rtb = new RenderTargetBitmap();
			await rtb.RenderAsync(RootGrid);
			var buffer = await rtb.GetPixelsAsync();
			using (var stream = File.Create(filePath))
			{
				var encoder = await Windows.Graphics.Imaging.BitmapEncoder.CreateAsync(
					Windows.Graphics.Imaging.BitmapEncoder.PngEncoderId,
					System.IO.WindowsRuntimeStreamExtensions.AsRandomAccessStream(stream));
				encoder.SetPixelData(
					Windows.Graphics.Imaging.BitmapPixelFormat.Bgra8,
					Windows.Graphics.Imaging.BitmapAlphaMode.Premultiplied,
					(uint)rtb.PixelWidth,
					(uint)rtb.PixelHeight,
					96, 96,
					System.Runtime.InteropServices.WindowsRuntime.WindowsRuntimeBufferExtensions.ToArray(buffer));
				await encoder.FlushAsync();
			}

			if (!string.IsNullOrEmpty(artifactCopyPath) && File.Exists(filePath))
			{
				try
				{
					string? dir = Path.GetDirectoryName(artifactCopyPath);
					if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir)) Directory.CreateDirectory(dir);
					File.Copy(filePath, artifactCopyPath, true);
				}
				catch {}
			}
		}
		catch (Exception ex)
		{
			Debug.WriteLine($"CaptureCurrentWindowAsync failed: {ex.Message}");
		}
	}

	[GeneratedCode("Microsoft.UI.Xaml.Markup.Compiler", " 3.0.0.2409")]
	[DebuggerNonUserCode]
	public IComponentConnector GetBindingConnector(int connectionId, object target)
	{
		return null;
	}
}
