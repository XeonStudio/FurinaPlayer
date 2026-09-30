using System;
using System.CodeDom.Compiler;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using FurinaPlayer.UI.Controls;
using FurinaPlayer.UI.Pages;
using FurinaPlayer.UI.ViewModels;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Markup;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Media.Imaging;
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
		Title = "Furina player · 全格式无损音乐播放器";
		ApplyMaterial(BackgroundMaterial.Mica);
		Activated += OnFirstActivated;
		ExtendsContentIntoTitleBar = true;
		SetTitleBar(TitleBar);
		if (AppWindow.Presenter is OverlappedPresenter overlappedPresenter)
		{
			overlappedPresenter.SetBorderAndTitleBar(hasBorder: false, hasTitleBar: false);
		}
		Closed += (object _, WindowEventArgs _) =>
		{
			SaveWindowPlacement();
			SessionClosing?.Invoke();
			_engine.Dispose();
			Vst3NativeHost.Instance.Unload();
		};
		Nav.SelectedItem = Nav.MenuItems[0];
		UpdateMaxIcon();
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
		if (MaxButton?.Content is FontIcon fontIcon)
		{
			bool flag = AppWindow.Presenter is OverlappedPresenter overlappedPresenter && overlappedPresenter.State == OverlappedPresenterState.Maximized;
			fontIcon.Glyph = (flag ? "\ue923" : "\ue922");
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
		ApplyMaterial(_currentMaterial);
	}

	public void ApplyMaterial(BackgroundMaterial material)
	{
		_currentMaterial = material;
		nint windowHandle = WindowNative.GetWindowHandle(this);
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
		if (LiquidGlassOverlay != null)
		{
			LiquidGlassOverlay.Visibility = ((material != BackgroundMaterial.LiquidGlass) ? Visibility.Collapsed : Visibility.Visible);
		}
		if (GlassSkiaCanvas != null)
		{
			GlassSkiaCanvas.Visibility = ((material != BackgroundMaterial.LiquidGlassDwm) ? Visibility.Collapsed : Visibility.Visible);
			if (GlassSkiaCanvas.Visibility == Visibility.Visible)
			{
				GlassSkiaCanvas.Invalidate();
			}
		}
	}

	private void OnGlassSkiaPaint(object? sender, SKPaintSurfaceEventArgs e)
	{
		SKCanvas canvas = e.Surface.Canvas;
		canvas.Clear(SKColors.Transparent);
		float num = e.Info.Width;
		float num2 = e.Info.Height;
		if (num <= 0f || num2 <= 0f)
		{
			return;
		}
		using (SKPaint sKPaint = new SKPaint
		{
			IsAntialias = true,
			Style = SKPaintStyle.Stroke,
			StrokeWidth = 1.5f
		})
		{
			sKPaint.Shader = SKShader.CreateLinearGradient(new SKPoint(0f, 0f), new SKPoint(num, 0f), new SKColor[3]
			{
				new SKColor(16777215u),
				new SKColor(3019898879u),
				new SKColor(16777215u)
			}, null, SKShaderTileMode.Clamp);
			canvas.DrawLine(0f, 1f, num, 1f, sKPaint);
		}
		using (SKPaint sKPaint2 = new SKPaint())
		{
			float num3 = Math.Min(140f, num2 * 0.18f);
			sKPaint2.Shader = SKShader.CreateLinearGradient(new SKPoint(0f, 0f), new SKPoint(0f, num3), new SKColor[3]
			{
				new SKColor(1560281087u),
				new SKColor(452984831u),
				new SKColor(16777215u)
			}, null, SKShaderTileMode.Clamp);
			canvas.DrawRect(new SKRect(0f, 0f, num, num3), sKPaint2);
		}
		using SKPaint sKPaint3 = new SKPaint
		{
			IsAntialias = true,
			Style = SKPaintStyle.Stroke,
			StrokeWidth = 1f
		};
		sKPaint3.Shader = SKShader.CreateLinearGradient(new SKPoint(0f, 0f), new SKPoint(num, 0f), new SKColor[3]
		{
			new SKColor(16777215u),
			new SKColor(1308622847u),
			new SKColor(16777215u)
		}, null, SKShaderTileMode.Clamp);
		canvas.DrawLine(0f, num2 - 1f, num, num2 - 1f, sKPaint3);
	}

	public void ApplyWallpaper(string? imagePath, bool solid)
	{
		try
		{
			if (solid)
			{
				RootGrid.Background = new SolidColorBrush(Color.FromArgb(byte.MaxValue, 28, 30, 38));
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
		RootGrid.Background = null;
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
			break;
		case 10:
			MaxButton = target.As<Button>();
			MaxButton.Click += OnMaximizeClick;
			break;
		case 11:
			CloseButton = target.As<Button>();
			CloseButton.Click += OnCloseClick;
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
