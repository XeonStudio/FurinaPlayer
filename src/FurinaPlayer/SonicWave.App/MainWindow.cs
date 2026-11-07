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

	private readonly StreamingViewModel _streamingVm;

	private StreamingPage? _streamingPage;

	private SystemTrayManager? _trayManager;

	private bool _isExplicitExit;

	private ScaleTransform? _uiScaleTransform;

	private bool _placementRestored;

	private BackgroundMaterial _currentMaterial;

	private bool _isDarkTheme;

	private float _pointerNormX = 0.5f;

	private float _pointerNormY = 0.25f;

	private string _currentNavTag = "home";

	private Border? _wallpaperBorder;

	private string? _lastWallpaperPath;

	private bool _lastWallpaperSolid = true;

	private DispatcherTimer? _rainbowTimer;

	private DispatcherTimer? _scheduledDarkTimer;

	private int _rainbowTickCounter;

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

	public MainWindow(HomeViewModel homeVm, NowPlayingViewModel nowPlayingVm, EditorViewModel editorVm, SettingsViewModel settingsVm, DspViewModel dspVm, StreamingViewModel streamingVm, AudioEngine engine)
	{
		InitializeComponent();
		_engine = engine;
		_homeVm = homeVm;
		_nowPlayingVm = nowPlayingVm;
		_editorVm = editorVm;
		_settingsVm = settingsVm;
		_dspVm = dspVm;
		_streamingVm = streamingVm;
		MiniPlayer.DataContext = nowPlayingVm;
		this.MiniPlayer.NavigateToPlayingRequested += () => NavigateTo("playing");
		FurinaPlayer.UI.Controls.MiniPlayer.OpenNowPlayingRequested += () => NavigateTo("playing");
		Title = "Furina player · 全格式无损音乐播放器";
		_settingsVm.SyncLiquidGlassEngineFromSettings();
		EnsureWallpaperLayer();
		ApplyMaterial(_settingsVm.Material);
		Activated += OnFirstActivated;
		Activated += OnWindowFocusChanged;
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

		try
		{
			_trayManager = new SystemTrayManager();
			_trayManager.Initialize();
			_trayManager.RestoreRequested += () => DispatcherQueue.TryEnqueue(RestoreMainWindow);
			_trayManager.PlayPauseRequested += () => DispatcherQueue.TryEnqueue(() => _engine.TogglePlayPause());
			_trayManager.NextRequested += () => DispatcherQueue.TryEnqueue(() => _engine.Next());
			_trayManager.PrevRequested += () => DispatcherQueue.TryEnqueue(() => _engine.Previous());
			_trayManager.ExitRequested += () => DispatcherQueue.TryEnqueue(ExitApplication);
		}
		catch { }

		AppWindow.Closing += (sender, args) =>
		{
			if (_settingsVm.Settings.CloseToTray && !_isExplicitExit)
			{
				args.Cancel = true;
				AppWindow.Hide();
				_trayManager?.ShowNotification("FurinaPlayer", "已最小化到系统托盘，后台持续播放中");
			}
		};

		AppWindow.Changed += (s, args) =>
		{
			if (args.DidSizeChange)
			{
				UpdateScaledRootSize(_settingsVm.Settings.UiScale);
			}
		};

		_settingsVm.UiScaleChanged += s => DispatcherQueue.TryEnqueue(() => ApplyUiScale(s));
		_settingsVm.AppearanceChanged += () => DispatcherQueue.TryEnqueue(ApplyEchoAppearanceSettings);
		ApplyUiScale(_settingsVm.Settings.UiScale);
		InitializeAppearanceTimers();

		_engine.StateChanged += s => UpdateKeepAwake();
		_settingsVm.PreventSystemSleepChanged += b => UpdateKeepAwake();
		UpdateKeepAwake();

		Closed += (object _, WindowEventArgs _) =>
		{
			_trayManager?.Dispose();
			PowerWakeManager.SetKeepAwake(false);
			SaveWindowPlacement();
			SessionClosing?.Invoke();
			_engine.Dispose();
			Vst3NativeHost.Instance.Unload();
		};

		if (Nav != null && Nav.MenuItems != null)
		{
			var streamingItem = new NavigationViewItem
			{
				Content = "流媒体音乐",
				Tag = "streaming"
			};
			Nav.MenuItems.Insert(1, streamingItem);
		}

		ApplyFluentWindowIcons();
		ApplyEchoAppearanceSettings();
		Nav.SelectedItem = Nav.MenuItems[0];
		UpdateMaxIcon();
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
			"streaming" => FluentIconKind.MusicHero,
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
			if (!settings.RememberWindowState)
			{
				AppWindow.Resize(new SizeInt32(1180, 760));
				try
				{
					RectInt32 workArea = DisplayArea.GetFromWindowId(AppWindow.Id, DisplayAreaFallback.Nearest).WorkArea;
					AppWindow.Move(new PointInt32(workArea.X + Math.Max(0, (workArea.Width - 1180) / 2), workArea.Y + Math.Max(0, (workArea.Height - 760) / 2)));
				}
				catch { }
				return;
			}
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
			if (!_settingsVm.Settings.RememberWindowState)
			{
				return;
			}
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
		if (RootGrid == null) return;
		bool effectiveDark = ResolveEffectiveDarkTheme(mode);
		RootGrid.RequestedTheme = effectiveDark ? ElementTheme.Dark : ElementTheme.Light;
		_isDarkTheme = effectiveDark;
		UpdateTitleBarButtonColors(RootGrid.RequestedTheme);
		ApplyMaterial(_currentMaterial);
		LiquidGlassEngine.RefreshAllAttachedElements(_isDarkTheme);
		if (ContentFrame?.Content is DependencyObject pageRoot && _currentNavTag != "playing")
		{
			LiquidGlassEngine.UpgradeVisualTreeToLiquidGlass(pageRoot, _isDarkTheme);
		}
		GlassSkiaCanvas?.Invalidate();
	}

	private bool ResolveEffectiveDarkTheme(ThemeMode? overrideMode = null)
	{
		try
		{
			if (_settingsVm != null && _settingsVm.ScheduledDarkModeEnabled)
			{
				if (IsCurrentlyInScheduledDarkWindow(_settingsVm.ScheduledDarkStartTime, _settingsVm.ScheduledDarkEndTime))
				{
					return !LiquidGlassEngine.InvertModeEnabled;
				}
				return LiquidGlassEngine.InvertModeEnabled;
			}

			string echoMode = _settingsVm?.EchoThemeMode ?? LiquidGlassEngine.ActiveThemeMode;
			bool baseDark;
			if (echoMode == "dark")
			{
				baseDark = true;
			}
			else if (echoMode == "light")
			{
				baseDark = false;
			}
			else if (echoMode == "ambient" || echoMode == "acrylic" || echoMode == "liquid-glass")
			{
				baseDark = LiquidGlassEngine.ActivePreset.IsDark;
			}
			else
			{
				ThemeMode tm = overrideMode ?? (_settingsVm?.Settings.Theme ?? ThemeMode.System);
				baseDark = tm switch
				{
					ThemeMode.Dark => true,
					ThemeMode.Light => false,
					_ => Application.Current != null && Application.Current.RequestedTheme == ApplicationTheme.Dark
				};
			}

			return LiquidGlassEngine.InvertModeEnabled ? !baseDark : baseDark;
		}
		catch
		{
			return false;
		}
	}

	private static bool IsCurrentlyInScheduledDarkWindow(string startStr, string endStr)
	{
		if (!TimeSpan.TryParse(startStr, out var start)) start = new TimeSpan(19, 0, 0);
		if (!TimeSpan.TryParse(endStr, out var end)) end = new TimeSpan(7, 0, 0);
		var now = DateTime.Now.TimeOfDay;
		if (start <= end)
		{
			return now >= start && now < end;
		}
		return now >= start || now < end;
	}

	private void InitializeAppearanceTimers()
	{
		try
		{
			_rainbowTimer = new DispatcherTimer
			{
				Interval = TimeSpan.FromMilliseconds(55)
			};
			_rainbowTimer.Tick += (s, e) =>
			{
				if (!LiquidGlassEngine.RainbowModeEnabled)
				{
					_rainbowTimer.Stop();
					return;
				}
				LiquidGlassEngine.RainbowHueDegrees = (LiquidGlassEngine.RainbowHueDegrees + 3.6f) % 360f;
				GlassSkiaCanvas?.Invalidate();
				_rainbowTickCounter++;
				if (_rainbowTickCounter % 4 == 0)
				{
					ApplyWindowBackdrop();
					LiquidGlassEngine.RefreshAllAttachedElements(_isDarkTheme);
				}
			};

			_scheduledDarkTimer = new DispatcherTimer
			{
				Interval = TimeSpan.FromSeconds(30)
			};
			_scheduledDarkTimer.Tick += (s, e) =>
			{
				if (_settingsVm != null && _settingsVm.ScheduledDarkModeEnabled)
				{
					bool targetDark = ResolveEffectiveDarkTheme();
					if (targetDark != _isDarkTheme)
					{
						ApplyEchoAppearanceSettings();
					}
				}
			};
			_scheduledDarkTimer.Start();
		}
		catch
		{
		}
	}

	private void OnWindowFocusChanged(object sender, WindowActivatedEventArgs args)
	{
		bool focused = args.WindowActivationState != WindowActivationState.Deactivated;
		if (LiquidGlassEngine.IsWindowFocused != focused)
		{
			LiquidGlassEngine.IsWindowFocused = focused;
			if (!LiquidGlassEngine.KeepTranslucentWhenUnfocused)
			{
				ApplyWindowBackdrop();
				LiquidGlassEngine.RefreshAllAttachedElements(_isDarkTheme);
				GlassSkiaCanvas?.Invalidate();
			}
		}
	}

	private void EnsureWallpaperLayer()
	{
		if (LiquidGlassOverlay == null) return;
		if (_wallpaperBorder == null)
		{
			_wallpaperBorder = new Border
			{
				IsHitTestVisible = false,
				HorizontalAlignment = HorizontalAlignment.Stretch,
				VerticalAlignment = VerticalAlignment.Stretch,
				Visibility = Visibility.Collapsed
			};
		}
		if (!LiquidGlassOverlay.Children.Contains(_wallpaperBorder))
		{
			LiquidGlassOverlay.Children.Insert(0, _wallpaperBorder);
		}
		else if (LiquidGlassOverlay.Children.IndexOf(_wallpaperBorder) != 0)
		{
			LiquidGlassOverlay.Children.Remove(_wallpaperBorder);
			LiquidGlassOverlay.Children.Insert(0, _wallpaperBorder);
		}
	}

	public void ApplyEchoAppearanceSettings()
	{
		try
		{
			_settingsVm?.SyncLiquidGlassEngineFromSettings();
			bool effectiveDark = ResolveEffectiveDarkTheme();
			_isDarkTheme = effectiveDark;
			if (RootGrid != null)
			{
				RootGrid.RequestedTheme = effectiveDark ? ElementTheme.Dark : ElementTheme.Light;
			}
			UpdateTitleBarButtonColors(effectiveDark ? ElementTheme.Dark : ElementTheme.Light);
			ApplyMaterial(_settingsVm?.Material ?? _currentMaterial);
			UpdateWallpaperVisuals();
			ApplyWindowBackdrop();
			LiquidGlassEngine.RefreshAllAttachedElements(_isDarkTheme);
			if (ContentFrame?.Content is DependencyObject pageRoot && _currentNavTag != "playing")
			{
				LiquidGlassEngine.UpgradeVisualTreeToLiquidGlass(pageRoot, _isDarkTheme);
			}
			if (LiquidGlassEngine.RainbowModeEnabled)
			{
				if (_rainbowTimer != null && !_rainbowTimer.IsEnabled)
				{
					_rainbowTimer.Start();
				}
			}
			else
			{
				_rainbowTimer?.Stop();
			}
			GlassSkiaCanvas?.Invalidate();
		}
		catch (Exception ex)
		{
			Log("ApplyEchoAppearanceSettings error: " + ex.Message);
		}
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
			string echoMode = _settingsVm?.EchoThemeMode ?? LiquidGlassEngine.ActiveThemeMode;
			bool useAcrylic = material == BackgroundMaterial.LiquidGlass
				|| material == BackgroundMaterial.LiquidGlassDwm
				|| material == BackgroundMaterial.Acrylic
				|| echoMode == "acrylic"
				|| echoMode == "liquid-glass"
				|| echoMode == "ambient";

			DwmGlass.ClearBackdrop(windowHandle);
			if (useAcrylic)
			{
				if (SystemBackdrop is not DesktopAcrylicBackdrop)
				{
					SystemBackdrop = new DesktopAcrylicBackdrop();
				}
			}
			else
			{
				if (SystemBackdrop is not MicaBackdrop)
				{
					SystemBackdrop = new MicaBackdrop();
				}
			}
		}
		catch
		{
			try
			{
				if (SystemBackdrop == null)
				{
					SystemBackdrop = new DesktopAcrylicBackdrop();
				}
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
		UpdateWallpaperVisuals();
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
		_lastWallpaperPath = imagePath;
		_lastWallpaperSolid = solid;
		UpdateWallpaperVisuals();
	}

	private void UpdateWallpaperVisuals()
	{
		try
		{
			EnsureWallpaperLayer();
			if (RootGrid != null)
			{
				RootGrid.Background = new SolidColorBrush(Colors.Transparent);
			}
			if (_wallpaperBorder == null)
			{
				return;
			}
			if (_currentNavTag == "playing" || _lastWallpaperSolid || string.IsNullOrWhiteSpace(_lastWallpaperPath) || !File.Exists(_lastWallpaperPath))
			{
				_wallpaperBorder.Visibility = Visibility.Collapsed;
				return;
			}

			string echoMode = _settingsVm?.EchoThemeMode ?? LiquidGlassEngine.ActiveThemeMode;
			double opacityScale = Math.Clamp(LiquidGlassEngine.AcrylicOpacityScale, 0.0, 1.0);

			// 在纯亚克力模式下，让系统桌面从窗口后方透出；在氛围/液态玻璃模式下，壁纸与焦散光场柔美交融
			double targetOpacity = echoMode switch
			{
				"acrylic" => Math.Clamp((1.0 - opacityScale) * 0.35, 0.0, 0.35),
				"ambient" => 0.38 + opacityScale * 0.45,
				"liquid-glass" => 0.25 + opacityScale * 0.38,
				_ => 0.20
			};

			if (targetOpacity <= 0.02)
			{
				_wallpaperBorder.Visibility = Visibility.Collapsed;
				return;
			}

			if (!(_wallpaperBorder.Background is ImageBrush existingBrush) || (_wallpaperBorder.Tag as string) != _lastWallpaperPath)
			{
				BitmapImage imageSource = new BitmapImage(new Uri(_lastWallpaperPath));
				_wallpaperBorder.Background = new ImageBrush
				{
					ImageSource = imageSource,
					Stretch = Stretch.UniformToFill,
					AlignmentX = AlignmentX.Center,
					AlignmentY = AlignmentY.Center
				};
				_wallpaperBorder.Tag = _lastWallpaperPath;
			}
			_wallpaperBorder.Opacity = targetOpacity;
			_wallpaperBorder.Visibility = Visibility.Visible;
		}
		catch
		{
			if (_wallpaperBorder != null)
			{
				_wallpaperBorder.Visibility = Visibility.Collapsed;
			}
		}
	}

	private void OnContentFrameNavigated(object sender, NavigationEventArgs e)
	{
		string tag = e.SourcePageType == typeof(NowPlayingPage) ? "playing"
			: e.SourcePageType == typeof(EditorPage) ? "editor"
			: e.SourcePageType == typeof(DspPage) ? "dsp"
			: e.SourcePageType == typeof(StreamingPage) ? "streaming"
			: e.SourcePageType == typeof(SettingsPage) ? "settings"
			: "home";
		ApplyStageGlassByTag(tag);
	}

	private void ApplyStageGlassByTag(string tag)
	{
		_currentNavTag = tag;
		ApplyWindowBackdrop();
		UpdateWallpaperVisuals();
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
			case "streaming":
				try
				{
					_streamingPage ??= new StreamingPage(_streamingVm);
					_streamingPage.ViewModel = _streamingVm;
					_streamingPage.DataContext = _streamingVm;
					ContentFrame.Content = _streamingPage;
				}
				catch (Exception exStream)
				{
					Log("navigate streaming exception: " + exStream);
				}
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
			case "streaming":
				try
				{
					_streamingPage ??= new StreamingPage(_streamingVm);
					_streamingPage.ViewModel = _streamingVm;
					_streamingPage.DataContext = _streamingVm;
					ContentFrame.Content = _streamingPage;
				}
				catch (Exception exStream)
				{
					Log("navigate streaming exception: " + exStream);
				}
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

	[DllImport("user32.dll")]
	private static extern bool SetForegroundWindow(nint hWnd);

	public void RestoreMainWindow()
	{
		try
		{
			AppWindow.Show();
			if (AppWindow.Presenter is OverlappedPresenter op)
			{
				if (op.State == OverlappedPresenterState.Minimized)
				{
					op.Restore();
				}
			}
			Activate();
			nint hwnd = WindowNative.GetWindowHandle(this);
			SetForegroundWindow(hwnd);
		}
		catch { }
	}

	public void ExitApplication()
	{
		_isExplicitExit = true;
		_trayManager?.Dispose();
		PowerWakeManager.SetKeepAwake(false);
		SaveWindowPlacement();
		Close();
	}

	public void ApplyUiScale(double scale)
	{
		try
		{
			if (scale < 0.5 || scale > 3.0) scale = 1.0;
			if (RootGrid == null) return;

			if (_uiScaleTransform == null)
			{
				_uiScaleTransform = new ScaleTransform();
				RootGrid.RenderTransform = _uiScaleTransform;
				RootGrid.RenderTransformOrigin = new Windows.Foundation.Point(0, 0);
			}
			_uiScaleTransform.ScaleX = scale;
			_uiScaleTransform.ScaleY = scale;
			UpdateScaledRootSize(scale);
		}
		catch (Exception ex)
		{
			Log("ApplyUiScale error: " + ex);
		}
	}

	private void UpdateScaledRootSize(double scale)
	{
		try
		{
			if (RootGrid == null) return;
			if (Math.Abs(scale - 1.0) < 0.01)
			{
				RootGrid.ClearValue(FrameworkElement.WidthProperty);
				RootGrid.ClearValue(FrameworkElement.HeightProperty);
				return;
			}
			var appSize = AppWindow.Size;
			if (appSize.Width > 0 && appSize.Height > 0)
			{
				RootGrid.Width = Math.Max(100, appSize.Width / scale);
				RootGrid.Height = Math.Max(100, appSize.Height / scale);
			}
		}
		catch { }
	}

	private void UpdateKeepAwake()
	{
		try
		{
			bool preventSleep = _settingsVm.Settings.PreventSystemSleep;
			bool isPlaying = _engine.State == PlaybackState.Playing;
			PowerWakeManager.SetKeepAwake(preventSleep && isPlaying);
		}
		catch { }
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
