using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using FurinaPlayer.UI.Controls;
using FurinaPlayer.UI.ViewModels;
using Microsoft.UI;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Markup;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.Web.WebView2.Core;
using SonicWave.Audio.Decoders;
using SonicWave.Audio.Renderers;
using SonicWave.Core.Models;
using WinRT;
using WinRT.Interop;
using Windows.Storage;
using Windows.Storage.Pickers;

namespace FurinaPlayer.UI.Pages;

[WinRTRuntimeClassName("Microsoft.UI.Xaml.IUIElementOverrides")]
[WinRTExposedType(typeof(NowPlayingPageWinRTTypeDetails))]
public sealed class NowPlayingPage : Page, IComponentConnector
{
	private static readonly double[] SpeedOptions = new double[7] { 0.5, 0.75, 1.0, 1.25, 1.5, 2.0, 3.0 };

	[GeneratedCode("Microsoft.UI.Xaml.Markup.Compiler", " 3.0.0.2409")]
	private LyricsPanel Lyrics;

	[GeneratedCode("Microsoft.UI.Xaml.Markup.Compiler", " 3.0.0.2409")]
	private EqualizerControl Equalizer;

	[GeneratedCode("Microsoft.UI.Xaml.Markup.Compiler", " 3.0.0.2409")]
	private ComboBox EqPresetCombo;

	[GeneratedCode("Microsoft.UI.Xaml.Markup.Compiler", " 3.0.0.2409")]
	private ComboBox SpeedCombo;

	[GeneratedCode("Microsoft.UI.Xaml.Markup.Compiler", " 3.0.0.2409")]
	private Slider ProgressSlider;

	[GeneratedCode("Microsoft.UI.Xaml.Markup.Compiler", " 3.0.0.2409")]
	private bool _contentLoaded;

	public NowPlayingViewModel ViewModel { get; set; }

	public NowPlayingPage()
	{
		InitializeComponent();
		Loaded += OnLoaded;
		Unloaded += OnUnloaded;
	}

	public NowPlayingPage(NowPlayingViewModel viewModel)
		: this()
	{
		ViewModel = viewModel;
		DataContext = viewModel;
	}

	protected override void OnNavigatedTo(NavigationEventArgs e)
	{
		base.OnNavigatedTo(e);
		_isUnloaded = false;
		if (e.Parameter is NowPlayingViewModel nowPlayingViewModel && !ReferenceEquals(ViewModel, nowPlayingViewModel))
		{
			if (ViewModel != null)
			{
				ViewModel.PropertyChanged -= OnViewModelPropertyChanged;
			}
			ViewModel = nowPlayingViewModel;
			DataContext = nowPlayingViewModel;
		}
		if (ViewModel != null)
		{
			ViewModel.SyncWithEngine();
			ViewModel.PropertyChanged -= OnViewModelPropertyChanged;
			ViewModel.PropertyChanged += OnViewModelPropertyChanged;
		}
		if (_modeSwitchBtn != null) _modeSwitchBtn.Visibility = Visibility.Visible;
		if (s_currentMode == LyricsPageMode.Classic)
		{
			s_currentMode = LyricsPageMode.AuditionDaw;
		}
		if (_classicContainer != null) _classicContainer.Visibility = Visibility.Collapsed;
		if (_dawContainer != null) _dawContainer.Visibility = Visibility.Visible;
		StartMeterTimer();
		if (_isWrapped)
		{
			_ = EnsureDawWebViewAsync(s_currentMode);
		}
	}

	protected override void OnNavigatedFrom(NavigationEventArgs e)
	{
		base.OnNavigatedFrom(e);
		StopMeterTimer();
		CancelPendingEnvelopeWork();
		if (_dawContainer != null) _dawContainer.Visibility = Visibility.Collapsed;
		if (_modeSwitchBtn != null) _modeSwitchBtn.Visibility = Visibility.Collapsed;
		GC.Collect(1, GCCollectionMode.Optimized);
	}

	private bool _layoutAdjusted;
	private TextBlock? _positionText;
	private TextBlock? _durationText;

	// DAW & PowerPoint Mode Integration
	public enum LyricsPageMode
	{
		AuditionDaw,
		PowerPoint,
		// Kept for caller compatibility; falls back to AuditionDaw.
		Classic
	}

	private static LyricsPageMode s_currentMode = LyricsPageMode.AuditionDaw;
	private static bool s_useDawMode => true;
	private bool _isWrapped;
	private Grid? _rootGrid;
	private Grid? _classicContainer;
	private Grid? _dawContainer;
	private WebView2? _webView;
	private Button? _modeSwitchBtn;
	private DispatcherQueueTimer? _meterTimer;
	private bool _isDawMode => true;
	private bool _dawReady;
	private bool _isUnloaded;
	private bool _isInitializingWebView;
	private bool _fullStateSyncPending;
	private int _webNavigationGeneration;
	private CancellationTokenSource? _envelopeCts;
	private int _envelopeGeneration;
	private string? _activeEnvelopeKey;
	private string? _completedEnvelopeKey;
	private string? _envelopeRetryKey;
	private int _envelopeRetryCount;
	private const int MaxEnvelopeRetries = 4;
	private string? _lastMeterTrackKey;
	private bool? _lastMeterIsPlaying;

	private void OnLoaded(object sender, RoutedEventArgs e)
	{
		_isUnloaded = false;
		ApplyResponsiveLayout();
		SetupPageContainers();
		FluentIconFactory.UpgradeButtonsInVisualTree(_classicContainer ?? (DependencyObject)this);
		UpdateClassicModeButtonIcon();
		if (ViewModel != null)
		{
			ViewModel.Initialize();
			ViewModel.PropertyChanged -= OnViewModelPropertyChanged;
			ViewModel.PropertyChanged += OnViewModelPropertyChanged;
			Lyrics.Attach(ViewModel);
			Lyrics.ApplyLyricFont(ViewModel.LyricFontFamily, ViewModel.LyricFontFilePath);
			Equalizer.Gains = ViewModel.EqGains;
			int num = Array.IndexOf(SpeedOptions, ViewModel.Speed);
			if (num >= 0)
			{
				SpeedCombo.SelectedIndex = num;
			}
			UpdateTimeTexts();
			UpdateClassicModeButtonIcon();
			if (_isDawMode && _dawReady)
			{
				ScheduleDawFullStateSync();
			}
		}
		StartMeterTimer();
	}

	private bool _isDraggingSlider;

	private void OnViewModelPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
	{
		if (e.PropertyName == nameof(NowPlayingViewModel.Position) ||
		    e.PropertyName == nameof(NowPlayingViewModel.Duration) ||
		    e.PropertyName == nameof(NowPlayingViewModel.PositionPercent))
		{
			UpdateTimeTexts();
			if (e.PropertyName == nameof(NowPlayingViewModel.Duration) && _isDawMode)
			{
				ScheduleDawFullStateSync();
			}
		}
		else if (e.PropertyName == nameof(NowPlayingViewModel.CurrentTrack) ||
		         e.PropertyName == nameof(NowPlayingViewModel.CoverPath) ||
		         e.PropertyName == nameof(NowPlayingViewModel.Title))
		{
			UpdateLyricsPlaceholderState();
			if (_isDawMode)
			{
				ScheduleDawFullStateSync();
			}
		}
		else if (e.PropertyName == nameof(NowPlayingViewModel.IsPlaying))
		{
			if (_isDawMode)
			{
				SendDawPlaybackState();
				if (ViewModel?.IsPlaying == true)
				{
					SendDawAudioFormat();
					SendDawEnvelope();
				}
			}
		}
		else if (e.PropertyName == nameof(NowPlayingViewModel.LyricLines) ||
		         e.PropertyName == nameof(NowPlayingViewModel.LyricSource))
		{
			UpdateLyricsPlaceholderState();
			if (_isDawMode)
			{
				SendDawLyrics();
			}
		}
		else if (e.PropertyName == nameof(NowPlayingViewModel.ModeText))
		{
			UpdateClassicModeButtonIcon();
		}
	}

	private void UpdateClassicModeButtonIcon()
	{
		try
		{
			FindAndUpdateClassicModeButton(_classicContainer, ViewModel?.ModeText ?? "顺序播放");
		}
		catch
		{
		}
	}

	private static void FindAndUpdateClassicModeButton(DependencyObject? root, string modeText)
	{
		if (root == null) return;
		int count = VisualTreeHelper.GetChildrenCount(root);
		for (int i = 0; i < count; i++)
		{
			var child = VisualTreeHelper.GetChild(root, i);
			if (child is Button btn && ToolTipService.GetToolTip(btn) is string tt && tt.Contains("循环模式"))
			{
				FluentIconFactory.SetButtonFluentIcon(btn, FluentIconFactory.ResolvePlayModeKind(modeText), modeText, 18);
				return;
			}
			FindAndUpdateClassicModeButton(child, modeText);
		}
	}

	private void UpdateTimeTexts()
	{
		if (ViewModel == null) return;
		if (_positionText != null)
		{
			_positionText.Text = FormatTime(ViewModel.Position);
		}
		if (_durationText != null)
		{
			_durationText.Text = FormatTime(ViewModel.Duration);
		}
		if (!_isDraggingSlider && ProgressSlider != null)
		{
			ProgressSlider.Value = ViewModel.PositionPercent;
		}
	}

	private void ApplyResponsiveLayout()
	{
		if (_layoutAdjusted) return;
		_layoutAdjusted = true;
		try
		{
			if (Content is Grid rootGrid && rootGrid.ColumnDefinitions.Count >= 2)
			{
				rootGrid.ColumnDefinitions[0].Width = new GridLength(1.0, GridUnitType.Star);
				rootGrid.ColumnDefinitions[0].MinWidth = 320.0;
				rootGrid.ColumnDefinitions[1].Width = new GridLength(1.0, GridUnitType.Star);
				rootGrid.ColumnDefinitions[1].MinWidth = 300.0;
			}
			SetupTimeTexts(Content);
		}
		catch (Exception)
		{
		}
	}

	private void SetupTimeTexts(DependencyObject? root)
	{
		if (root == null) return;
		int count = VisualTreeHelper.GetChildrenCount(root);
		for (int i = 0; i < count; i++)
		{
			var child = VisualTreeHelper.GetChild(root, i);
			if (child is Slider s && s.Name == "ProgressSlider" && root is Grid parentGrid)
			{
				foreach (var sibling in parentGrid.Children)
				{
					if (sibling is Grid timeGrid && Grid.GetRow(timeGrid) == 1 && timeGrid.Children.Count >= 2)
					{
						if (timeGrid.Children[0] is TextBlock pos && timeGrid.Children[1] is TextBlock dur)
						{
							_positionText = pos;
							_durationText = dur;
							_positionText.ClearValue(TextBlock.TextProperty);
							_durationText.ClearValue(TextBlock.TextProperty);
							UpdateTimeTexts();
							return;
						}
					}
				}
			}
			SetupTimeTexts(child);
		}
	}

	private void OnUnloaded(object sender, RoutedEventArgs e)
	{
		_isUnloaded = true;
		StopMeterTimer();
		CancelPendingEnvelopeWork();
		if (ViewModel != null)
		{
			ViewModel.PropertyChanged -= OnViewModelPropertyChanged;
		}
		Lyrics.Detach();
	}

	private StackPanel? _noLyricsPlaceholder;

	private void OptimizeClassicLayout(Grid? originalGrid)
	{
		if (originalGrid == null) return;
		try
		{
			originalGrid.HorizontalAlignment = HorizontalAlignment.Center;
			originalGrid.VerticalAlignment = VerticalAlignment.Center;
			originalGrid.MaxWidth = 1180;
			originalGrid.Margin = new Thickness(24, 16, 24, 16);

			if (originalGrid.ColumnDefinitions.Count >= 2)
			{
				originalGrid.ColumnDefinitions[0].Width = new GridLength(1.0, GridUnitType.Star);
				originalGrid.ColumnDefinitions[0].MinWidth = 360.0;
				originalGrid.ColumnDefinitions[1].Width = new GridLength(1.0, GridUnitType.Star);
				originalGrid.ColumnDefinitions[1].MinWidth = 360.0;
			}

			EnlargeAlbumCover(originalGrid);

			foreach (var child in originalGrid.Children)
			{
				if (child is FrameworkElement fe && Grid.GetColumn(fe) == 1 && fe is Grid rightGrid)
				{
					SetupNoLyricsPlaceholder(rightGrid);
					break;
				}
			}
		}
		catch (Exception ex)
		{
			Debug.WriteLine($"OptimizeClassicLayout failed: {ex.Message}");
		}
	}

	private void EnlargeAlbumCover(DependencyObject root)
	{
		int count = VisualTreeHelper.GetChildrenCount(root);
		for (int i = 0; i < count; i++)
		{
			var child = VisualTreeHelper.GetChild(root, i);
			if (child is Border b && (b.Width == 220 || (b.Child is Grid g && g.Children.Any(c => c is Image))))
			{
				b.Width = 280;
				b.Height = 280;
				b.CornerRadius = new CornerRadius(18);
				return;
			}
			EnlargeAlbumCover(child);
		}
	}

	private void SetupNoLyricsPlaceholder(Grid? lyricsParentGrid)
	{
		if (lyricsParentGrid == null || _noLyricsPlaceholder != null) return;

		_noLyricsPlaceholder = new StackPanel
		{
			HorizontalAlignment = HorizontalAlignment.Center,
			VerticalAlignment = VerticalAlignment.Center,
			Spacing = 16,
			Visibility = Visibility.Collapsed
		};

		var iconCircle = new Border
		{
			Width = 88,
			Height = 88,
			CornerRadius = new CornerRadius(44),
			HorizontalAlignment = HorizontalAlignment.Center,
			Background = new SolidColorBrush(Windows.UI.Color.FromArgb(45, 56, 189, 248)),
			BorderBrush = new SolidColorBrush(Windows.UI.Color.FromArgb(110, 56, 189, 248)),
			BorderThickness = new Thickness(1.5),
			Child = FluentIconFactory.Create(FluentIconKind.MusicHero, 54)
		};

		var textStack = new StackPanel { Spacing = 6, HorizontalAlignment = HorizontalAlignment.Center };
		textStack.Children.Add(new TextBlock
		{
			Text = "纯音乐 · 静享悠扬旋律",
			FontSize = 18,
			FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
			HorizontalAlignment = HorizontalAlignment.Center,
			Opacity = 0.95
		});
		textStack.Children.Add(new TextBlock
		{
			Text = "当前音轨暂未匹配到歌词，静心聆听高品质原声音效",
			FontSize = 12,
			HorizontalAlignment = HorizontalAlignment.Center,
			Opacity = 0.6
		});

		var importBtn = new Button
		{
			HorizontalAlignment = HorizontalAlignment.Center,
			Margin = new Thickness(0, 6, 0, 0),
			Padding = new Thickness(16, 7, 16, 7),
			CornerRadius = new CornerRadius(16)
		};
		FluentIconFactory.SetButtonFluentIcon(importBtn, FluentIconKind.LyricsImport, "导入本地 LRC 歌词", 18);
		importBtn.Click += OnBatchImportLyricsClick;

		_noLyricsPlaceholder.Children.Add(iconCircle);
		_noLyricsPlaceholder.Children.Add(textStack);
		_noLyricsPlaceholder.Children.Add(importBtn);

		Grid.SetRow(_noLyricsPlaceholder, 1);
		lyricsParentGrid.Children.Add(_noLyricsPlaceholder);
		UpdateLyricsPlaceholderState();
	}

	private void UpdateLyricsPlaceholderState()
	{
		if (ViewModel == null) return;
		bool hasLyrics = ViewModel.LyricLines != null && ViewModel.LyricLines.Count > 0;
		if (_noLyricsPlaceholder != null)
		{
			_noLyricsPlaceholder.Visibility = hasLyrics ? Visibility.Collapsed : Visibility.Visible;
		}
		if (Lyrics != null)
		{
			Lyrics.Visibility = hasLyrics ? Visibility.Visible : Visibility.Collapsed;
		}
	}

	private void SetupPageContainers()
	{
		if (_isWrapped) return;
		_isWrapped = true;

		try
		{
			if (Content is Grid originalGrid)
			{
				Content = null;
				_classicContainer = originalGrid;
				OptimizeClassicLayout(_classicContainer);

				_dawContainer = new Grid
				{
					Visibility = Visibility.Collapsed,
					Background = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 18, 20, 22)),
					HorizontalAlignment = HorizontalAlignment.Stretch,
					VerticalAlignment = VerticalAlignment.Stretch
				};

				_modeSwitchBtn = new Button
				{
					HorizontalAlignment = HorizontalAlignment.Right,
					VerticalAlignment = VerticalAlignment.Top,
					Margin = new Thickness(0, 8, 20, 0),
					Padding = new Thickness(12, 6, 12, 6),
					CornerRadius = new CornerRadius(16),
					Background = new SolidColorBrush(Windows.UI.Color.FromArgb(235, 30, 34, 40)),
					BorderBrush = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 0, 229, 255)),
					BorderThickness = new Thickness(1)
				};
				_modeSwitchBtn.Click += OnModeSwitchClick;

				_rootGrid = new Grid();
				_rootGrid.Children.Add(_classicContainer);
				_rootGrid.Children.Add(_dawContainer);
				_rootGrid.Children.Add(_modeSwitchBtn);

				Content = _rootGrid;

				SetDawMode(s_useDawMode);
			}
		}
		catch (Exception ex)
		{
			Debug.WriteLine($"Error wrapping containers: {ex.Message}");
		}
	}

	private void OnModeSwitchClick(object sender, RoutedEventArgs e)
	{
		LyricsPageMode next = s_currentMode == LyricsPageMode.AuditionDaw
			? LyricsPageMode.PowerPoint
			: LyricsPageMode.AuditionDaw;
		SetLyricsMode(next);
	}

	public void SetLyricsMode(LyricsPageMode mode)
	{
		if (mode == LyricsPageMode.Classic)
		{
			mode = LyricsPageMode.AuditionDaw;
		}

		s_currentMode = mode;
		UpdateModeSwitchButtonUI();

		if (_classicContainer != null) _classicContainer.Visibility = Visibility.Collapsed;
		if (_dawContainer != null) _dawContainer.Visibility = Visibility.Visible;
		_ = EnsureDawWebViewAsync(mode);
		StartMeterTimer();
	}

	public async Task<bool> CaptureDawPreviewAsync(string filePath)
	{
		if (_webView == null || _webView.CoreWebView2 == null)
		{
			return false;
		}

		try
		{
			var track = GetAuthoritativeTrack();
			if (track != null && !string.IsNullOrEmpty(track.FilePath) && File.Exists(track.FilePath))
			{
				var env = await Task.Run(() => WaveformRenderer.ComputeAuditionEnvelopeFromFile(track.FilePath, 4096));
				if (env != null && env.Peak != null && env.Peak.Length > 0)
				{
					PostDawMessage(new
					{
						protocolVersion = 1,
						type = "echo:audition:envelopeData",
						trackId = FormatDawTrackId(track),
						p = SanitizeFiniteArray(env.Peak),
						r = SanitizeFiniteArray(env.Rms),
						lMin = SanitizeFiniteArray(env.LeftMin),
						lMax = SanitizeFiniteArray(env.LeftMax),
						rMin = SanitizeFiniteArray(env.RightMin),
						rMax = SanitizeFiniteArray(env.RightMax),
						specCols = env.SpecCols,
						specBins = env.SpecBins,
						sampleRate = env.SampleRate,
						specL = env.SpecLeftBase64,
						specR = env.SpecRightBase64
					});
					await Task.Delay(250);
				}
			}

			string? dir = Path.GetDirectoryName(filePath);
			if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
			{
				Directory.CreateDirectory(dir);
			}

			using (var mem = new MemoryStream())
			{
				await _webView.CoreWebView2.CapturePreviewAsync(
					CoreWebView2CapturePreviewImageFormat.Png,
					System.IO.WindowsRuntimeStreamExtensions.AsRandomAccessStream(mem));
				if (mem.Length > 0)
				{
					File.WriteAllBytes(filePath, mem.ToArray());
					return true;
				}
			}
			return false;
		}
		catch (Exception ex)
		{
			Debug.WriteLine($"CaptureDawPreviewAsync failed: {ex.Message}");
			return false;
		}
	}

	private void SetDawMode(bool enableDaw)
	{
		SetLyricsMode(s_currentMode == LyricsPageMode.PowerPoint ? LyricsPageMode.PowerPoint : LyricsPageMode.AuditionDaw);
	}

	private void UpdateModeSwitchButtonUI()
	{
		if (_modeSwitchBtn == null) return;
		var sp = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8, VerticalAlignment = VerticalAlignment.Center };
		if (s_currentMode == LyricsPageMode.PowerPoint)
		{
			_modeSwitchBtn.Margin = new Thickness(0, 4, 12, 0);
			_modeSwitchBtn.BorderBrush = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 234, 91, 45));
			sp.Children.Add(FluentIconFactory.Create(FluentIconKind.ModePowerPoint, 18));
			sp.Children.Add(new TextBlock { Text = "PowerPoint 演示模式 ▾", FontSize = 12, FontWeight = Microsoft.UI.Text.FontWeights.SemiBold, Foreground = new SolidColorBrush(Colors.White), VerticalAlignment = VerticalAlignment.Center });
			ToolTipService.SetToolTip(_modeSwitchBtn, "当前：PowerPoint 幻灯片演示文稿（点击切换到 Audition DAW，右键选择）");
		}
		else
		{
			_modeSwitchBtn.Margin = new Thickness(0, 4, 12, 0);
			_modeSwitchBtn.BorderBrush = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 0, 229, 255));
			sp.Children.Add(FluentIconFactory.Create(FluentIconKind.ModeAudition, 18));
			sp.Children.Add(new TextBlock { Text = "Audition DAW 模式 ▾", FontSize = 12, FontWeight = Microsoft.UI.Text.FontWeights.SemiBold, Foreground = new SolidColorBrush(Colors.White), VerticalAlignment = VerticalAlignment.Center });
			ToolTipService.SetToolTip(_modeSwitchBtn, "当前：Audition 专业母带工作台（点击切换到 PowerPoint 演示，右键选择）");
		}
		_modeSwitchBtn.Content = sp;
		SetupModeSwitchFlyout();
	}

	private void SetupModeSwitchFlyout()
	{
		if (_modeSwitchBtn == null) return;
		var flyout = new MenuFlyout();

		var itemDaw = new MenuFlyoutItem
		{
			Text = "Adobe Audition DAW 专业工作站",
			Icon = new FontIcon { Glyph = "\uE995" }
		};
		itemDaw.Click += (s, e) => SetLyricsMode(LyricsPageMode.AuditionDaw);
		flyout.Items.Add(itemDaw);

		var itemPpt = new MenuFlyoutItem
		{
			Text = "Microsoft PowerPoint 演示文稿（黑胶 / 流体 / 网易云）",
			Icon = new FontIcon { Glyph = "\uE8B8" }
		};
		itemPpt.Click += (s, e) => SetLyricsMode(LyricsPageMode.PowerPoint);
		flyout.Items.Add(itemPpt);

		_modeSwitchBtn.ContextFlyout = flyout;
	}

	private static readonly string s_pptPresetPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "FurinaPlayer", "ppt_user_preset.json");

	private void SavePptPreset(string json)
	{
		try
		{
			string dir = Path.GetDirectoryName(s_pptPresetPath)!;
			if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
			File.WriteAllText(s_pptPresetPath, json);
		}
		catch {}
	}

	private void SendPptPreset()
	{
		try
		{
			if (File.Exists(s_pptPresetPath))
			{
				string json = File.ReadAllText(s_pptPresetPath);
				using var doc = JsonDocument.Parse(json);
				PostDawMessage(new
				{
					type = "echo:powerpoint:presetData",
					preset = doc.RootElement.Clone()
				});
			}
		}
		catch {}
	}

	private void SendSystemFonts()
	{
		var fonts = new string[]
		{
			"Segoe UI", "Microsoft YaHei", "Arial", "Calibri", "Consolas", 
			"Cascadia Code", "SimSun", "SimHei", "KaiTi", "FangSong", "Comic Sans MS"
		};
		PostDawMessage(new
		{
			type = "echo:powerpoint:systemFonts",
			fonts
		});
	}

	private void OpenVst3EditorFromWeb()
	{
		DispatcherQueue.TryEnqueue(() =>
		{
			try
			{
				int sampleRate = 44100;
				try
				{
					var fmt = ViewModel?.GetAudioFormatDetails();
					if (fmt.HasValue && fmt.Value.SampleRate > 0) sampleRate = fmt.Value.SampleRate;
				}
				catch {}

				var settings = new SonicWave.Core.Services.SettingsService().Load();
				var activePlugin = settings.Vst3Plugins?.FirstOrDefault(p => p != null && p.IsActive);
				string? path = activePlugin?.Path ?? SonicWave.Audio.Vst3.Vst3NativeHost.Instance.LoadedPath;
				if (string.IsNullOrEmpty(path))
				{
					var first = settings.Vst3Plugins?.FirstOrDefault(p => p != null && !string.IsNullOrEmpty(p.Path));
					if (first != null)
					{
						first.Enabled = true;
						path = first.Path;
					}
				}
				if (!string.IsNullOrEmpty(path))
				{
					FurinaPlayer.UI.Helpers.Vst3EditorWindow.Open(path, sampleRate, 2, out string err);
				}
			}
			catch (Exception ex)
			{
				Debug.WriteLine("Error opening VST3 editor from web: " + ex.Message);
			}
		});
	}

	private static string GetTargetUriForMode(LyricsPageMode mode)
	{
		return (mode == LyricsPageMode.PowerPoint)
			? "https://powerpoint.theme/index.html?v=2.0.0"
			: "https://audition.daw/index.html?v=2.0.0";
	}

	private async Task EnsureDawWebViewAsync(LyricsPageMode mode = LyricsPageMode.AuditionDaw)
	{
		string targetUri = GetTargetUriForMode(mode);

		if (_webView != null)
		{
			if (_webView.CoreWebView2 != null)
			{
				string cur = _webView.Source?.ToString() ?? "";
				if (!cur.StartsWith(targetUri, StringComparison.OrdinalIgnoreCase))
				{
					_dawReady = false;
					_completedEnvelopeKey = null;
					CancelPendingEnvelopeWork();
					_webNavigationGeneration++;
					_webView.CoreWebView2.Navigate(targetUri);
				}
				else if (_dawReady)
				{
					ScheduleDawFullStateSync();
				}
				return;
			}
			if (_isInitializingWebView)
			{
				return;
			}
			_dawContainer?.Children.Remove(_webView);
			_webView = null;
		}

		if (_isInitializingWebView || _isUnloaded)
		{
			return;
		}

		_isInitializingWebView = true;
		try
		{
			_webView = new WebView2
			{
				HorizontalAlignment = HorizontalAlignment.Stretch,
				VerticalAlignment = VerticalAlignment.Stretch
			};

			_dawContainer?.Children.Add(_webView);

			await _webView.EnsureCoreWebView2Async();
			if (_isUnloaded || _webView?.CoreWebView2 == null)
			{
				if (_webView != null)
				{
					_dawContainer?.Children.Remove(_webView);
					_webView = null;
				}
				return;
			}

			string baseDir = AppDomain.CurrentDomain.BaseDirectory;
			string assetsDir = Path.Combine(baseDir, "Assets", "AuditionDaw");
			if (!Directory.Exists(assetsDir))
			{
				string devAudition = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "Assets", "AuditionDaw");
				if (Directory.Exists(devAudition)) assetsDir = Path.GetFullPath(devAudition);
				else assetsDir = @"C:\Users\Xeon\Desktop\FurinaPlayer\Assets\AuditionDaw";
			}

			string pptDir = Path.Combine(baseDir, "Assets", "PowerPointTheme");
			if (!Directory.Exists(pptDir))
			{
				string devPpt = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "Assets", "PowerPointTheme");
				if (Directory.Exists(devPpt)) pptDir = Path.GetFullPath(devPpt);
			}

			_webView.CoreWebView2.SetVirtualHostNameToFolderMapping(
				"audition.daw",
				assetsDir,
				CoreWebView2HostResourceAccessKind.Allow);

			if (Directory.Exists(pptDir))
			{
				_webView.CoreWebView2.SetVirtualHostNameToFolderMapping(
					"powerpoint.theme",
					pptDir,
					CoreWebView2HostResourceAccessKind.Allow);
			}

			_webView.CoreWebView2.Settings.IsWebMessageEnabled = true;
			_webView.CoreWebView2.Settings.AreDevToolsEnabled = true;
			_webView.CoreWebView2.Settings.IsScriptEnabled = true;

			_webView.CoreWebView2.WebMessageReceived += OnWebMessageReceived;
			_webView.NavigationCompleted += (s, e) =>
			{
				if (!e.IsSuccess || _isUnloaded) return;
				int navGen = _webNavigationGeneration;
				var dq = this.DispatcherQueue ?? DispatcherQueue.GetForCurrentThread();
				dq?.TryEnqueue(async () =>
				{
					await Task.Delay(350);
					if (!_dawReady && !_isUnloaded && navGen == _webNavigationGeneration && _webView?.CoreWebView2 != null)
					{
						_dawReady = true;
						SendFullDawState();
					}
				});
			};

			_dawReady = false;
			_completedEnvelopeKey = null;
			_webNavigationGeneration++;
			_webView.Source = new Uri(targetUri);
		}
		catch (Exception ex)
		{
			Debug.WriteLine($"Exception in EnsureDawWebViewAsync: {ex}");
			if (_webView != null && _webView.CoreWebView2 == null)
			{
				_dawContainer?.Children.Remove(_webView);
				_webView = null;
			}
		}
		finally
		{
			_isInitializingWebView = false;
		}
	}

	private void StartMeterTimer()
	{
		if (_meterTimer == null)
		{
			var dq = this.DispatcherQueue ?? DispatcherQueue.GetForCurrentThread();
			if (dq == null) return;
			_meterTimer = dq.CreateTimer();
			_meterTimer.Interval = TimeSpan.FromMilliseconds(25);
			_meterTimer.Tick += OnMeterTimerTick;
		}
		if (!_meterTimer.IsRunning)
		{
			_meterTimer.Start();
		}
	}

	private void StopMeterTimer()
	{
		if (_meterTimer != null && _meterTimer.IsRunning)
		{
			_meterTimer.Stop();
		}
	}

	private Track? GetAuthoritativeTrack() => ViewModel?.Engine?.CurrentTrack ?? ViewModel?.CurrentTrack;

	private static double SanitizeFinite(double value, double fallback = 0.0) =>
		double.IsFinite(value) ? value : fallback;

	private static float SanitizeFinite(float value, float fallback = 0.0f) =>
		float.IsFinite(value) ? value : fallback;

	private static float[] SanitizeFiniteArray(float[]? values, float fallback = 0.0f)
	{
		if (values == null || values.Length == 0)
		{
			return Array.Empty<float>();
		}
		for (int i = 0; i < values.Length; i++)
		{
			if (!float.IsFinite(values[i]))
			{
				float[] copy = new float[values.Length];
				for (int j = 0; j < values.Length; j++)
				{
					copy[j] = float.IsFinite(values[j]) ? values[j] : fallback;
				}
				return copy;
			}
		}
		return values;
	}

	private void OnMeterTimerTick(DispatcherQueueTimer sender, object args)
	{
		if (!_isDawMode || !_dawReady || _isUnloaded || _webView?.CoreWebView2 == null || ViewModel == null) return;

		try
		{
			var engine = ViewModel.Engine;
			var engineTrack = engine?.CurrentTrack;
			bool trackOutOfSync =
				(engineTrack != null && (ViewModel.CurrentTrack == null ||
				 ViewModel.CurrentTrack.Id != engineTrack.Id ||
				 !string.Equals(ViewModel.CurrentTrack.FilePath, engineTrack.FilePath, StringComparison.Ordinal))) ||
				(engineTrack == null && ViewModel.CurrentTrack != null);
			if (trackOutOfSync)
			{
				ViewModel.SyncWithEngine();
			}

			var activeTrack = GetAuthoritativeTrack();
			string trackIdStr = FormatDawTrackId(activeTrack);
			string trackKey = $"{trackIdStr}|{activeTrack?.FilePath ?? string.Empty}";
			bool isPlaying = ViewModel.IsPlaying || (engine?.State == SonicWave.Audio.PlaybackState.Playing);

			if (activeTrack != null && !string.Equals(_lastMeterTrackKey, trackKey, StringComparison.Ordinal))
			{
				_lastMeterTrackKey = trackKey;
				_lastMeterIsPlaying = isPlaying;
				_envelopeRetryCount = 0;
				_fullStateSyncPending = false;
				SendFullDawState();
			}
			else if (_lastMeterIsPlaying != isPlaying)
			{
				_lastMeterIsPlaying = isPlaying;
				SendDawPlaybackState();
				if (isPlaying)
				{
					SendDawAudioFormat();
					SendDawEnvelope();
				}
			}

			var meters = ViewModel.GetLiveAudioMeters();

			double posSec = SanitizeFinite(engine?.Position.TotalSeconds ?? ViewModel.Position.TotalSeconds, 0.0);
			double rawDurSec = ViewModel.Duration.TotalSeconds > 0
				? ViewModel.Duration.TotalSeconds
				: (activeTrack != null && activeTrack.DurationMilliseconds > 0
					? activeTrack.DurationMilliseconds / 1000.0
					: (engine?.Duration.TotalSeconds ?? 0.0));
			double durSec = SanitizeFinite(rawDurSec, 0.0);
			double playbackRate = SanitizeFinite(ViewModel.Speed > 0 ? ViewModel.Speed : 1.0, 1.0);

			PostDawMessage(new
			{
				protocolVersion = 1,
				type = "echo:workshop-ui:clock",
				positionSeconds = posSec,
				durationSeconds = durSec,
				clock = new
				{
					state = isPlaying ? "playing" : "paused",
					positionSeconds = posSec,
					durationSeconds = durSec,
					sampledAtMs = (double)DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
					playbackRate,
					currentTrackId = trackIdStr
				}
			});

			double peakDb = isPlaying ? SanitizeFinite(meters.PeakDb, -90.0) : -90.0;
			double rmsDb = isPlaying ? SanitizeFinite(meters.RmsDb, -90.0) : -90.0;
			float balance = isPlaying ? SanitizeFinite(meters.StereoBalance, 0.0f) : 0.0f;
			float correlation = isPlaying ? SanitizeFinite(meters.PhaseCorrelation, 1.0f) : 1.0f;
			double energy = isPlaying ? Math.Clamp(SanitizeFinite((rmsDb + 60.0) / 60.0, 0.0), 0.0, 1.0) : 0.0;
			float[] bands = isPlaying ? SanitizeFiniteArray(meters.Bands) : Array.Empty<float>();
			float[] vectorPoints = isPlaying ? SanitizeFiniteArray(meters.VectorPoints) : Array.Empty<float>();

			PostDawMessage(new
			{
				protocolVersion = 1,
				type = "echo:workshop-ui:audio",
				trackId = trackIdStr,
				levels = new
				{
					peakDb,
					rmsDb,
					balance,
					correlation
				},
				spectrum = new
				{
					bands,
					energy,
					balance,
					correlation,
					vectorPoints
				}
			});
		}
		catch (Exception ex)
		{
			Debug.WriteLine($"OnMeterTimerTick error: {ex.Message}");
		}
	}

	private void PostDawMessage(object message)
	{
		if (_webView == null) return;
		if (DispatcherQueue.HasThreadAccess)
		{
			try
			{
				if (_webView.CoreWebView2 != null)
				{
					string json = JsonSerializer.Serialize(message);
					_webView.CoreWebView2.PostWebMessageAsJson(json);
				}
			}
			catch (Exception ex)
			{
				Debug.WriteLine($"PostDawMessage error: {ex.Message}");
			}
		}
		else
		{
			DispatcherQueue.TryEnqueue(() =>
			{
				try
				{
					if (_webView?.CoreWebView2 != null)
					{
						string json = JsonSerializer.Serialize(message);
						_webView.CoreWebView2.PostWebMessageAsJson(json);
					}
				}
				catch (Exception ex)
				{
					Debug.WriteLine($"PostDawMessage error: {ex.Message}");
				}
			});
		}
	}

	private void OnWebMessageReceived(CoreWebView2 sender, CoreWebView2WebMessageReceivedEventArgs e)
	{
		try
		{
			string? raw = null;
			try { raw = e.TryGetWebMessageAsString(); } catch {}
			if (string.IsNullOrEmpty(raw))
			{
				raw = e.WebMessageAsJson;
			}
			if (string.IsNullOrEmpty(raw)) return;

			using var doc = JsonDocument.Parse(raw);
			var root = doc.RootElement;
			if (root.ValueKind == JsonValueKind.String)
			{
				string inner = root.GetString() ?? "";
				if (!string.IsNullOrEmpty(inner))
				{
					using var innerDoc = JsonDocument.Parse(inner);
					ProcessWebMessage(innerDoc.RootElement.Clone());
					return;
				}
			}
			ProcessWebMessage(root.Clone());
		}
		catch (Exception ex)
		{
			Debug.WriteLine($"Error handling web message: {ex.Message}");
		}
	}

	private void ProcessWebMessage(JsonElement root)
	{
		try
		{
			string type = root.TryGetProperty("type", out var tProp) ? tProp.GetString() ?? "" : "";

			if (type == "echo:workshop-ui:ready")
			{
				_dawReady = true;
				_completedEnvelopeKey = null;
				SendFullDawState();
				SendPptPreset();
				SendSystemFonts();
				return;
			}

			if (type == "echo:audition:getQueue" || type == "echo:powerpoint:getQueue")
			{
				SendDawQueue();
				return;
			}

			if (type == "echo:audition:getCover" || type == "echo:powerpoint:getCover")
			{
				var activeTrack = GetAuthoritativeTrack();
				string coverPath = !string.IsNullOrEmpty(activeTrack?.CoverPath) ? activeTrack.CoverPath : (ViewModel?.CoverPath ?? string.Empty);
				string coverUrl = GetCoverDataUrl(coverPath);
				string trackIdStr = FormatDawTrackId(activeTrack);
				PostDawMessage(new
				{
					type = "echo:powerpoint:coverData",
					coverDataUrl = coverUrl,
					trackId = trackIdStr
				});
				PostDawMessage(new
				{
					type = "echo:audition:coverData",
					coverDataUrl = coverUrl,
					trackId = trackIdStr
				});
				return;
			}

			if (type == "echo:audition:getLyrics" || type == "echo:powerpoint:getLyrics")
			{
				SendDawLyrics();
				return;
			}

			if (type == "echo:audition:getAudioStatus" || type == "echo:powerpoint:getAudioStatus")
			{
				SendDawAudioFormat();
				return;
			}

			if (type == "echo:audition:getEnvelope")
			{
				SendDawEnvelope(force: true);
				return;
			}

			if (type == "echo:powerpoint:getPreset")
			{
				SendPptPreset();
				return;
			}

			if (type == "echo:powerpoint:savePreset")
			{
				if (root.TryGetProperty("preset", out var presetEl))
				{
					SavePptPreset(presetEl.GetRawText());
				}
				return;
			}

			if (type == "echo:powerpoint:getSystemFonts")
			{
				SendSystemFonts();
				return;
			}

			if (type == "echo:vst3:toggleRack" || type == "echo:powerpoint:toggleVstRack" ||
			    type == "echo:vst3:openPluginManager" || type == "echo:powerpoint:openVstManager")
			{
				OpenVst3EditorFromWeb();
				return;
			}

			if (type == "furina:switchLyricsMode")
			{
				if (root.TryGetProperty("mode", out var modeEl))
				{
					string m = modeEl.GetString() ?? "";
					if (m.Equals("PowerPoint", StringComparison.OrdinalIgnoreCase))
						SetLyricsMode(LyricsPageMode.PowerPoint);
					else
						SetLyricsMode(LyricsPageMode.AuditionDaw);
				}
				return;
			}

			if (type == "echo:workshop-ui:command")
			{
				string requestId = root.TryGetProperty("requestId", out var rProp) ? rProp.GetString() ?? "" : "";
				string cmd = root.TryGetProperty("command", out var cProp) ? cProp.GetString() ?? "" : "";
				JsonElement payload = root.TryGetProperty("payload", out var pProp) ? pProp : default;

				HandleDawCommand(cmd, requestId, payload);
				return;
			}
		}
		catch (Exception ex)
		{
			Debug.WriteLine($"Error processing web message: {ex.Message}");
		}
	}

	private long _lastPlayPauseTick;
	private string _lastHandledRequestId = string.Empty;

	private void HandleDawCommand(string cmd, string requestId, JsonElement payload)
	{
		bool ok = true;
		object? extraValue = null;

		if (!string.IsNullOrEmpty(requestId) && requestId == _lastHandledRequestId)
		{
			return;
		}
		if (!string.IsNullOrEmpty(requestId))
		{
			_lastHandledRequestId = requestId;
		}

		switch (cmd)
		{
			case "playPause":
				long nowTick = Environment.TickCount64;
				if (nowTick - _lastPlayPauseTick >= 150)
				{
					_lastPlayPauseTick = nowTick;
					DispatcherQueue.TryEnqueue(() =>
					{
						ViewModel?.TogglePlayPauseCommand?.Execute(null);
						SendDawPlaybackState();
					});
				}
				break;
			case "play":
				DispatcherQueue.TryEnqueue(() =>
				{
					ViewModel?.PlayCommand?.Execute(null);
					SendDawPlaybackState();
				});
				break;
			case "pause":
				DispatcherQueue.TryEnqueue(() =>
				{
					ViewModel?.PauseCommand?.Execute(null);
					SendDawPlaybackState();
				});
				break;
			case "previous":
				DispatcherQueue.TryEnqueue(() => ViewModel?.PreviousCommand?.Execute(null));
				break;
			case "next":
				DispatcherQueue.TryEnqueue(() => ViewModel?.NextCommand?.Execute(null));
				break;
			case "stop":
				DispatcherQueue.TryEnqueue(() => ViewModel?.StopCommand?.Execute(null));
				break;
			case "seek":
				if (payload.ValueKind == JsonValueKind.Object)
				{
					double sec = 0.0;
					if (payload.TryGetProperty("positionSeconds", out var posProp))
					{
						sec = posProp.GetDouble();
					}
					else if (payload.TryGetProperty("time", out var timeProp))
					{
						sec = timeProp.GetDouble();
					}
					DispatcherQueue.TryEnqueue(() => ViewModel?.SeekSeconds(sec));
				}
				break;
			case "setVolume":
				if (payload.ValueKind == JsonValueKind.Object && payload.TryGetProperty("volume", out var volProp))
				{
					double vol = volProp.GetDouble();
					int intVol = vol <= 1.0 ? (int)Math.Round(vol * 100.0) : (int)Math.Round(vol);
					DispatcherQueue.TryEnqueue(() => ViewModel?.SetVolumePercent(intVol));
				}
				break;
			case "cycleRepeat":
				DispatcherQueue.TryEnqueue(() => ViewModel?.CycleModeCommand?.Execute(null));
				break;
			case "setRepeat":
				if (payload.ValueKind == JsonValueKind.Object && payload.TryGetProperty("mode", out var modeProp))
				{
					string mode = modeProp.GetString() ?? "off";
					DispatcherQueue.TryEnqueue(() =>
					{
						if (ViewModel?.Engine != null)
						{
							ViewModel.Engine.Mode = mode switch
							{
								"one" => PlaybackMode.RepeatOne,
								"all" => PlaybackMode.RepeatAll,
								_ => PlaybackMode.Sequential
							};
						}
					});
				}
				break;
			case "toggleShuffle":
				DispatcherQueue.TryEnqueue(() =>
				{
					if (ViewModel?.Engine != null)
					{
						ViewModel.Engine.Mode = ViewModel.Engine.Mode == PlaybackMode.Shuffle
							? PlaybackMode.Sequential
							: PlaybackMode.Shuffle;
					}
				});
				break;
			case "queue:playTrack":
			case "queue:playItem":
				long tid = 0;
				if (payload.ValueKind == JsonValueKind.Object)
				{
					if (payload.TryGetProperty("trackId", out var tidProp))
					{
						if (tidProp.ValueKind == JsonValueKind.Number) tid = tidProp.GetInt64();
						else if (tidProp.ValueKind == JsonValueKind.String && long.TryParse(tidProp.GetString(), out long parsedTid)) tid = parsedTid;
					}
					else if (payload.TryGetProperty("queueId", out var qidProp))
					{
						string qid = qidProp.GetString() ?? "";
						var parts = qid.Split('_');
						if (parts.Length >= 2 && long.TryParse(parts[1], out long qParsed))
						{
							tid = qParsed;
						}
					}
				}
				if (tid != 0)
				{
					DispatcherQueue.TryEnqueue(() =>
					{
						if (ViewModel?.Engine != null)
						{
							var trk = ViewModel.Engine.Queue.FirstOrDefault(t => t.Id == tid);
							if (trk != null) ViewModel.Engine.PlayTrack(trk);
						}
					});
				}
				break;
			case "queue:playAlbum":
			case "queue:playPlaylist":
				if (payload.ValueKind == JsonValueKind.Object && payload.TryGetProperty("firstTrackId", out var ftProp))
				{
					long ftid = 0;
					bool parsed = (ftProp.ValueKind == JsonValueKind.Number && ftProp.TryGetInt64(out ftid)) ||
					              (ftProp.ValueKind == JsonValueKind.String && long.TryParse(ftProp.GetString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out ftid));
					if (parsed)
					{
						DispatcherQueue.TryEnqueue(() =>
						{
							var trk = ViewModel?.Engine.Queue.FirstOrDefault(t => t.Id == ftid);
							if (trk != null) ViewModel?.Engine.PlayTrack(trk);
						});
					}
				}
				break;
			case "lyrics:get":
				SendDawLyrics();
				break;
			case "vst3:toggleRack":
			case "vst3:openPluginManager":
				OpenVst3EditorFromWeb();
				break;
			case "switchLyricsMode":
				if (payload.ValueKind == JsonValueKind.Object && payload.TryGetProperty("mode", out var mProp))
				{
					string m = mProp.GetString() ?? "";
					if (m.Equals("PowerPoint", StringComparison.OrdinalIgnoreCase))
						SetLyricsMode(LyricsPageMode.PowerPoint);
					else
						SetLyricsMode(LyricsPageMode.AuditionDaw);
				}
				break;
			case "storage:get":
				extraValue = null;
				break;
			case "storage:set":
				break;
			default:
				break;
		}

		if (!string.IsNullOrEmpty(requestId))
		{
			var resultObj = new Dictionary<string, object?>
			{
				["protocolVersion"] = 1,
				["type"] = "echo:workshop-ui:result",
				["requestId"] = requestId,
				["ok"] = ok
			};
			if (extraValue != null)
			{
				resultObj["value"] = extraValue;
			}
			PostDawMessage(resultObj);
		}
	}

	private static string FormatDawTrackId(Track? track)
	{
		return track != null ? track.Id.ToString(CultureInfo.InvariantCulture) : string.Empty;
	}

	private void CancelPendingEnvelopeWork()
	{
		_envelopeGeneration++;
		_activeEnvelopeKey = null;
		try
		{
			_envelopeCts?.Cancel();
			_envelopeCts?.Dispose();
		}
		catch
		{
		}
		_envelopeCts = null;
	}

	private void ScheduleDawFullStateSync()
	{
		if (!_isDawMode || !_dawReady || _isUnloaded || _webView?.CoreWebView2 == null || ViewModel == null) return;
		if (_fullStateSyncPending) return;
		_fullStateSyncPending = true;
		var dq = this.DispatcherQueue ?? DispatcherQueue.GetForCurrentThread();
		if (dq != null)
		{
			dq.TryEnqueue(DispatcherQueuePriority.Normal, () =>
			{
				_fullStateSyncPending = false;
				SendFullDawState();
			});
		}
		else
		{
			_fullStateSyncPending = false;
			SendFullDawState();
		}
	}

	private void SendFullDawState()
	{
		_fullStateSyncPending = false;
		if (ViewModel == null || !_dawReady || _isUnloaded || _webView?.CoreWebView2 == null) return;
		var track = GetAuthoritativeTrack();
		if (track != null && (ViewModel.CurrentTrack == null ||
		    ViewModel.CurrentTrack.Id != track.Id ||
		    !string.Equals(ViewModel.CurrentTrack.FilePath, track.FilePath, StringComparison.Ordinal)))
		{
			ViewModel.SyncWithEngine();
		}

		string trackIdStr = FormatDawTrackId(track);
		bool isPlaying = ViewModel.IsPlaying || (ViewModel.Engine?.State == SonicWave.Audio.PlaybackState.Playing);
		_lastMeterTrackKey = $"{trackIdStr}|{track?.FilePath ?? string.Empty}";
		_lastMeterIsPlaying = isPlaying;
		double volume01 = SanitizeFinite(Math.Clamp(ViewModel.Volume / 100.0, 0.0, 1.0), 0.8);

		string repeatMode = "off";
		bool isShuffle = false;
		if (ViewModel.Engine != null)
		{
			repeatMode = ViewModel.Engine.Mode switch
			{
				PlaybackMode.RepeatOne => "one",
				PlaybackMode.RepeatAll => "all",
				_ => "off"
			};
			isShuffle = ViewModel.Engine.Mode == PlaybackMode.Shuffle;
		}

		bool vmMatchesTrack = track != null && ViewModel.CurrentTrack != null && ViewModel.CurrentTrack.Id == track.Id;
		string rawCoverPath = vmMatchesTrack && !string.IsNullOrEmpty(ViewModel.CoverPath)
			? ViewModel.CoverPath
			: (track?.CoverPath ?? ViewModel.CoverPath);
		string coverUrl = GetCoverDataUrl(rawCoverPath);
		string title = vmMatchesTrack && !string.IsNullOrEmpty(ViewModel.Title) ? ViewModel.Title : (track?.DisplayTitle ?? ViewModel.Title);
		string artist = vmMatchesTrack && !string.IsNullOrEmpty(ViewModel.Artist) ? ViewModel.Artist : (track?.Artist ?? ViewModel.Artist);
		string album = vmMatchesTrack && !string.IsNullOrEmpty(ViewModel.Album) ? ViewModel.Album : (track?.Album ?? ViewModel.Album);
		double rawDurationSeconds = ViewModel.Duration.TotalSeconds > 0
			? ViewModel.Duration.TotalSeconds
			: (track != null && track.DurationMilliseconds > 0
				? track.DurationMilliseconds / 1000.0
				: (ViewModel.Engine?.Duration.TotalSeconds ?? 0.0));
		double durationSeconds = SanitizeFinite(rawDurationSeconds, 0.0);

		PostDawMessage(new
		{
			protocolVersion = 1,
			type = "echo:workshop-ui:init",
			appearance = new
			{
				accent = (s_currentMode == LyricsPageMode.PowerPoint) ? "#c43e1c" : "#00e5ff",
				accentText = "#000000",
				tone = "dark"
			}
		});

		PostDawMessage(new
		{
			type = "echo:powerpoint:coverData",
			coverDataUrl = coverUrl,
			trackId = trackIdStr
		});

		PostDawMessage(new
		{
			type = "echo:audition:coverData",
			coverDataUrl = coverUrl,
			trackId = trackIdStr
		});

		PostDawMessage(new
		{
			protocolVersion = 1,
			type = "echo:workshop-ui:state",
			playback = new
			{
				state = isPlaying ? "playing" : "paused",
				currentTrackId = trackIdStr,
				volume = volume01,
				repeatMode,
				shuffle = isShuffle
			},
			currentTrack = new
			{
				id = trackIdStr,
				title,
				artist,
				album,
				durationSeconds,
				coverUrl
			}
		});

		SendDawAudioFormat();
		SendDawLyrics();
		SendDawQueue();
		SendDawEnvelope();
	}

	private void ScheduleEnvelopeRetryIfNeeded(string envKey, int generation)
	{
		if (_isUnloaded || !_dawReady || ViewModel == null) return;
		if (!string.Equals(_envelopeRetryKey, envKey, StringComparison.Ordinal))
		{
			_envelopeRetryKey = envKey;
			_envelopeRetryCount = 0;
		}
		if (_envelopeRetryCount >= MaxEnvelopeRetries)
		{
			return;
		}
		_envelopeRetryCount++;
		var dq = this.DispatcherQueue ?? DispatcherQueue.GetForCurrentThread();
		dq?.TryEnqueue(async () =>
		{
			await Task.Delay(1500);
			if (_isUnloaded || !_dawReady || ViewModel == null) return;
			if (generation != _envelopeGeneration) return;
			var currentTrack = GetAuthoritativeTrack();
			if (currentTrack == null) return;
			string curKey = $"{FormatDawTrackId(currentTrack)}|{currentTrack.FilePath ?? string.Empty}";
			if (!string.Equals(curKey, envKey, StringComparison.Ordinal)) return;
			if (string.Equals(_completedEnvelopeKey, envKey, StringComparison.Ordinal)) return;
			bool isPlaying = ViewModel.IsPlaying || (ViewModel.Engine?.State == SonicWave.Audio.PlaybackState.Playing);
			if (!isPlaying && _envelopeRetryCount > 1) return;
			SendDawEnvelope(force: true);
		});
	}

	private void SendDawEnvelope(bool force = false)
	{
		if (ViewModel == null || !_dawReady || _isUnloaded || _webView?.CoreWebView2 == null) return;
		var track = GetAuthoritativeTrack();
		if (track == null) return;
		string trackIdStr = FormatDawTrackId(track);
		string? path = track.FilePath;
		if (string.IsNullOrWhiteSpace(path))
		{
			path = ViewModel.Engine?.CurrentTrack?.FilePath;
		}
		if (string.IsNullOrWhiteSpace(path)) return;

		string envKey = $"{trackIdStr}|{path}";
		// Never cancel an already in-flight download/computation for the exact same track & path.
		if (string.Equals(_activeEnvelopeKey, envKey, StringComparison.Ordinal))
		{
			return;
		}
		if (!force && string.Equals(_completedEnvelopeKey, envKey, StringComparison.Ordinal))
		{
			return;
		}

		CancelPendingEnvelopeWork();
		var cts = new CancellationTokenSource();
		_envelopeCts = cts;
		CancellationToken ct = cts.Token;
		int generation = _envelopeGeneration;
		_activeEnvelopeKey = envKey;
		var dq = this.DispatcherQueue ?? DispatcherQueue.GetForCurrentThread();

		_ = Task.Run(async () =>
		{
			bool postedValid = false;
			try
			{
				string resolvedPath = path;
				if (!File.Exists(resolvedPath) && StreamAudioCache.IsNetworkUrl(resolvedPath))
				{
					var (ok, cachedPath) = await StreamAudioCache.TryGetOrDownloadCachedFileAsync(resolvedPath, 10000, ct).ConfigureAwait(false);
					if (ok && !string.IsNullOrEmpty(cachedPath) && File.Exists(cachedPath))
					{
						resolvedPath = cachedPath;
					}
				}

				if (ct.IsCancellationRequested)
				{
					return;
				}

				WaveformRenderer.AuditionEnvelopeData? env = null;
				if (File.Exists(resolvedPath))
				{
					env = WaveformRenderer.ComputeAuditionEnvelopeFromFile(resolvedPath, 4096);
					if ((env == null || env.Peak == null || env.Peak.Length == 0) && !ct.IsCancellationRequested)
					{
						await Task.Delay(250, ct).ConfigureAwait(false);
						if (!ct.IsCancellationRequested && File.Exists(resolvedPath))
						{
							env = WaveformRenderer.ComputeAuditionEnvelopeFromFile(resolvedPath, 4096);
						}
					}
				}

				if (ct.IsCancellationRequested)
				{
					return;
				}

				if (env == null || env.Peak == null || env.Peak.Length == 0)
				{
					dq?.TryEnqueue(() =>
					{
						if (generation == _envelopeGeneration)
						{
							ScheduleEnvelopeRetryIfNeeded(envKey, generation);
						}
					});
					return;
				}

				float[] safePeak = SanitizeFiniteArray(env.Peak);
				float[] safeRms = SanitizeFiniteArray(env.Rms);
				float[] safeLMin = SanitizeFiniteArray(env.LeftMin);
				float[] safeLMax = SanitizeFiniteArray(env.LeftMax);
				float[] safeRMin = SanitizeFiniteArray(env.RightMin);
				float[] safeRMax = SanitizeFiniteArray(env.RightMax);
				postedValid = true;

				dq?.TryEnqueue(() =>
				{
					if (ct.IsCancellationRequested || _isUnloaded || !_dawReady || _webView?.CoreWebView2 == null)
					{
						return;
					}
					var currentTrack = GetAuthoritativeTrack();
					if (currentTrack == null)
					{
						return;
					}
					bool sameId = string.Equals(FormatDawTrackId(currentTrack), trackIdStr, StringComparison.Ordinal);
					bool samePath = string.Equals(currentTrack.FilePath, path, StringComparison.OrdinalIgnoreCase) ||
					                string.Equals(currentTrack.FilePath, resolvedPath, StringComparison.OrdinalIgnoreCase) ||
					                (StreamAudioCache.IsNetworkUrl(path) && StreamAudioCache.IsNetworkUrl(currentTrack.FilePath));
					if (sameId && samePath)
					{
						_completedEnvelopeKey = envKey;
						_envelopeRetryKey = envKey;
						_envelopeRetryCount = 0;
						PostDawMessage(new
						{
							protocolVersion = 1,
							type = "echo:audition:envelopeData",
							trackId = trackIdStr,
							p = safePeak,
							r = safeRms,
							lMin = safeLMin,
							lMax = safeLMax,
							rMin = safeRMin,
							rMax = safeRMax,
							specCols = env.SpecCols,
							specBins = env.SpecBins,
							sampleRate = env.SampleRate,
							specL = env.SpecLeftBase64,
							specR = env.SpecRightBase64
						});
					}
				});
			}
			catch (OperationCanceledException)
			{
			}
			catch (Exception ex)
			{
				Debug.WriteLine($"SendDawEnvelope error: {ex.Message}");
				if (!postedValid && !ct.IsCancellationRequested)
				{
					dq?.TryEnqueue(() =>
					{
						if (generation == _envelopeGeneration)
						{
							ScheduleEnvelopeRetryIfNeeded(envKey, generation);
						}
					});
				}
			}
			finally
			{
				dq?.TryEnqueue(() =>
				{
					if (generation == _envelopeGeneration && string.Equals(_activeEnvelopeKey, envKey, StringComparison.Ordinal))
					{
						_activeEnvelopeKey = null;
					}
				});
			}
		}, ct);
	}

	private void SendDawPlaybackState()
	{
		if (ViewModel == null || !_dawReady || _isUnloaded || _webView?.CoreWebView2 == null) return;
		var track = GetAuthoritativeTrack();
		string trackIdStr = FormatDawTrackId(track);
		bool isPlaying = ViewModel.IsPlaying || (ViewModel.Engine?.State == SonicWave.Audio.PlaybackState.Playing);
		double volume01 = SanitizeFinite(Math.Clamp(ViewModel.Volume / 100.0, 0.0, 1.0), 0.8);

		string repeatMode = "off";
		bool isShuffle = false;
		if (ViewModel.Engine != null)
		{
			repeatMode = ViewModel.Engine.Mode switch
			{
				PlaybackMode.RepeatOne => "one",
				PlaybackMode.RepeatAll => "all",
				_ => "off"
			};
			isShuffle = ViewModel.Engine.Mode == PlaybackMode.Shuffle;
		}

		PostDawMessage(new
		{
			protocolVersion = 1,
			type = "echo:workshop-ui:state",
			playback = new
			{
				state = isPlaying ? "playing" : "paused",
				currentTrackId = trackIdStr,
				volume = volume01,
				repeatMode,
				shuffle = isShuffle
			}
		});
	}

	private void SendDawAudioFormat()
	{
		if (ViewModel == null || !_dawReady || _isUnloaded || _webView?.CoreWebView2 == null) return;
		var activeTrack = GetAuthoritativeTrack();
		string trackIdStr = FormatDawTrackId(activeTrack);
		bool isPlaying = ViewModel.IsPlaying || (ViewModel.Engine?.State == SonicWave.Audio.PlaybackState.Playing);
		var fmt = ViewModel.GetAudioFormatDetails();
		var meters = ViewModel.GetLiveAudioMeters();

		double peakDb = isPlaying ? SanitizeFinite(meters.PeakDb, -90.0) : -90.0;
		double rmsDb = isPlaying ? SanitizeFinite(meters.RmsDb, -90.0) : -90.0;
		float balance = isPlaying ? SanitizeFinite(meters.StereoBalance, 0.0f) : 0.0f;
		float correlation = isPlaying ? SanitizeFinite(meters.PhaseCorrelation, 1.0f) : 1.0f;
		double energy = isPlaying ? Math.Clamp(SanitizeFinite((rmsDb + 60.0) / 60.0, 0.0), 0.0, 1.0) : 0.0;
		float[] bands = isPlaying ? SanitizeFiniteArray(meters.Bands) : Array.Empty<float>();
		float[] vectorPoints = isPlaying ? SanitizeFiniteArray(meters.VectorPoints) : Array.Empty<float>();

		PostDawMessage(new
		{
			protocolVersion = 1,
			type = "echo:workshop-ui:audio",
			trackId = trackIdStr,
			audio = new
			{
				codec = fmt.Codec,
				sampleRate = fmt.SampleRate,
				bitDepth = fmt.BitDepth,
				channels = fmt.Channels,
				outputDevice = fmt.OutputDevice,
				outputBackend = fmt.OutputEngine,
				outputMode = fmt.OutputMode
			},
			levels = new
			{
				peakDb,
				rmsDb,
				balance,
				correlation
			},
			spectrum = new
			{
				bands,
				energy,
				balance,
				correlation,
				vectorPoints
			}
		});

		PostDawMessage(new
		{
			type = "echo:powerpoint:audioStatus",
			status = new
			{
				codec = fmt.Codec,
				sampleRate = $"{fmt.SampleRate / 1000.0:F1} kHz",
				bitDepth = $"{fmt.BitDepth}-bit",
				bitrate = "1411 kbps",
				channels = fmt.Channels == 2 ? "双声道立体声" : $"{fmt.Channels} 声道",
				engine = fmt.OutputEngine
			}
		});

		PostDawMessage(new
		{
			type = "echo:audition:audioStatus",
			status = new
			{
				codec = fmt.Codec,
				sampleRate = fmt.SampleRate,
				fileSampleRate = fmt.SampleRate,
				actualDeviceSampleRate = fmt.SampleRate,
				bitrate = 1411000,
				bitDepth = fmt.BitDepth,
				channels = fmt.Channels,
				outputDeviceName = fmt.OutputDevice,
				outputMode = fmt.OutputMode
			}
		});
	}

	private void SendDawLyrics()
	{
		if (ViewModel == null || !_dawReady || _isUnloaded || _webView?.CoreWebView2 == null) return;
		var track = GetAuthoritativeTrack();
		if (track == null) return;
		string trackIdStr = FormatDawTrackId(track);

		var lyricLines = ViewModel.LyricLines;
		var linesList = new List<object>();

		if (lyricLines != null)
		{
			foreach (var l in lyricLines)
			{
				linesList.Add(new
				{
					timeMs = (long)l.Time.TotalMilliseconds,
					text = l.Text
				});
			}
		}

		double durationSeconds = SanitizeFinite(
			track.DurationMilliseconds > 0 ? track.DurationMilliseconds / 1000.0 : ViewModel.Duration.TotalSeconds,
			0.0);

		PostDawMessage(new
		{
			protocolVersion = 1,
			type = "echo:workshop-ui:lyrics",
			trackId = trackIdStr,
			lyrics = new
			{
				kind = linesList.Count > 0 ? "synced" : "empty",
				title = track.DisplayTitle,
				artist = track.Artist ?? "",
				album = track.Album ?? "",
				durationSeconds,
				lines = linesList
			}
		});
	}

	private void SendDawQueue()
	{
		if (ViewModel?.Engine == null || !_dawReady || _isUnloaded || _webView?.CoreWebView2 == null) return;
		var queue = ViewModel.Engine.Queue;
		if (queue == null) return;

		string currentTrackIdStr = FormatDawTrackId(GetAuthoritativeTrack());
		var items = queue.Select((t, i) =>
		{
			string itemIdStr = FormatDawTrackId(t);
			return new
			{
				queueId = $"q_{itemIdStr}_{i}",
				id = itemIdStr,
				trackId = itemIdStr,
				title = t.DisplayTitle,
				artist = t.Artist ?? "—",
				album = t.Album ?? "—",
				durationSeconds = t.DurationMilliseconds > 0 ? SanitizeFinite(t.DurationMilliseconds / 1000.0, 0.0) : 0.0,
				coverUrl = string.Empty,
				coverThumb = string.Empty,
				rawCoverUrl = string.Empty
			};
		}).ToList();

		PostDawMessage(new
		{
			type = "echo:audition:queueData",
			items
		});

		PostDawMessage(new
		{
			type = "echo:powerpoint:queueData",
			items
		});

		PostDawMessage(new
		{
			protocolVersion = 1,
			type = "echo:workshop-ui:state",
			queue = new
			{
				items,
				currentQueueId = items.FirstOrDefault(x => string.Equals(x.id, currentTrackIdStr, StringComparison.Ordinal))?.queueId
			}
		});
	}

	private static readonly string s_defaultCoverDataUrl = "data:image/svg+xml;base64," + Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(
		"<svg xmlns='http://www.w3.org/2000/svg' width='500' height='500' viewBox='0 0 500 500'>" +
		"<defs><linearGradient id='g' x1='0%' y1='0%' x2='100%' y2='100%'><stop offset='0%' stop-color='#1e293b'/><stop offset='100%' stop-color='#0f172a'/></linearGradient></defs>" +
		"<rect width='500' height='500' rx='32' fill='url(#g)'/>" +
		"<circle cx='250' cy='250' r='180' fill='#0b0f19' stroke='#38bdf8' stroke-width='4' opacity='0.85'/>" +
		"<circle cx='250' cy='250' r='130' fill='none' stroke='#334155' stroke-width='2' stroke-dasharray='6 6'/>" +
		"<circle cx='250' cy='250' r='60' fill='#0284c7'/>" +
		"<circle cx='250' cy='250' r='16' fill='#0b0f19'/>" +
		"<path d='M235 210 v80 l60 -40 Z' fill='#ffffff' opacity='0.9'/>" +
		"</svg>"));

	private static readonly System.Collections.Concurrent.ConcurrentDictionary<string, string> s_coverDataUrlCache = new();

	private static string GetCoverDataUrl(string? coverPath)
	{
		if (string.IsNullOrEmpty(coverPath) || !File.Exists(coverPath))
		{
			return s_defaultCoverDataUrl;
		}

		try
		{
			var fi = new FileInfo(coverPath);
			string cacheKey = $"{coverPath}|{fi.Length}|{fi.LastWriteTimeUtc.Ticks}";
			if (s_coverDataUrlCache.TryGetValue(cacheKey, out var cached))
			{
				return cached;
			}

			// If file is already small (<= 96KB), direct encode
			if (fi.Length <= 96 * 1024)
			{
				byte[] raw = File.ReadAllBytes(coverPath);
				string ext = Path.GetExtension(coverPath).ToLowerInvariant();
				string mime = ext switch
				{
					".png" => "image/png",
					".webp" => "image/webp",
					".bmp" => "image/bmp",
					_ => "image/jpeg"
				};
				string directUrl = $"data:{mime};base64,{Convert.ToBase64String(raw)}";
				s_coverDataUrlCache[cacheKey] = directUrl;
				return directUrl;
			}

			// Downsample large images using SkiaSharp to max 512x512 JPEG (compressed to ~60-80KB)
			using var codec = SkiaSharp.SKCodec.Create(coverPath);
			if (codec != null)
			{
				const int maxDim = 512;
				int origW = codec.Info.Width;
				int origH = codec.Info.Height;
				int targetW = origW;
				int targetH = origH;

				if (origW > maxDim || origH > maxDim)
				{
					if (origW >= origH)
					{
						targetW = maxDim;
						targetH = Math.Max(1, (int)Math.Round((double)origH * maxDim / origW));
					}
					else
					{
						targetH = maxDim;
						targetW = Math.Max(1, (int)Math.Round((double)origW * maxDim / origH));
					}
				}

				using var originalBmp = SkiaSharp.SKBitmap.Decode(codec);
				if (originalBmp != null)
				{
					using var scaledBmp = originalBmp.Resize(new SkiaSharp.SKImageInfo(targetW, targetH, SkiaSharp.SKColorType.Rgba8888, SkiaSharp.SKAlphaType.Premul), SkiaSharp.SKFilterQuality.Medium);
					using var image = SkiaSharp.SKImage.FromBitmap(scaledBmp ?? originalBmp);
					using var data = image.Encode(SkiaSharp.SKEncodedImageFormat.Jpeg, 82);
					if (data != null)
					{
						string compactUrl = "data:image/jpeg;base64," + Convert.ToBase64String(data.ToArray());
						s_coverDataUrlCache[cacheKey] = compactUrl;
						return compactUrl;
					}
				}
			}

			// Fallback to raw bytes if Skia decode failed
			byte[] fallbackBytes = File.ReadAllBytes(coverPath);
			string fallbackUrl = $"data:image/jpeg;base64,{Convert.ToBase64String(fallbackBytes)}";
			s_coverDataUrlCache[cacheKey] = fallbackUrl;
			return fallbackUrl;
		}
		catch
		{
			return s_defaultCoverDataUrl;
		}
	}

	private void OnPlayClick(object sender, RoutedEventArgs e)
	{
		ViewModel?.PlayCommand?.Execute(null);
	}

	private void OnPauseClick(object sender, RoutedEventArgs e)
	{
		ViewModel?.PauseCommand?.Execute(null);
	}

	private void OnNextClick(object sender, RoutedEventArgs e)
	{
		ViewModel?.NextCommand?.Execute(null);
	}

	private void OnPreviousClick(object sender, RoutedEventArgs e)
	{
		ViewModel?.PreviousCommand?.Execute(null);
	}

	private void OnStopClick(object sender, RoutedEventArgs e)
	{
		ViewModel?.StopCommand?.Execute(null);
	}

	private void OnCycleModeClick(object sender, RoutedEventArgs e)
	{
		ViewModel?.CycleModeCommand?.Execute(null);
	}

	private void OnProgressPointerPressed(object sender, PointerRoutedEventArgs e)
	{
		_isDraggingSlider = true;
		if (ViewModel != null)
		{
			ViewModel.BeginSeek();
		}
	}

	private void OnProgressPointerMoved(object sender, PointerRoutedEventArgs e)
	{
		if (ViewModel != null && !(ViewModel.Duration.TotalSeconds <= 0.0) && sender is Slider { ActualWidth: not (<=0.0) } slider)
		{
			TimeSpan t = TimeSpan.FromSeconds(Math.Clamp(e.GetCurrentPoint(slider).Position.X / slider.ActualWidth, 0.0, 1.0) * ViewModel.Duration.TotalSeconds);
			ToolTipService.SetToolTip(slider, FormatTime(t) + " / " + FormatTime(ViewModel.Duration));
		}
	}

	private void OnProgressPointerExited(object sender, PointerRoutedEventArgs e)
	{
		if (sender is Slider element)
		{
			ToolTipService.SetToolTip(element, null);
		}
	}

	internal static string FormatTime(TimeSpan t)
	{
		if (!(t.TotalHours >= 1.0))
		{
			return t.ToString("m\\:ss");
		}
		return t.ToString("h\\:mm\\:ss");
	}

	private void OnProgressPointerReleased(object sender, PointerRoutedEventArgs e)
	{
		_isDraggingSlider = false;
		if (ViewModel != null && ProgressSlider != null)
		{
			ViewModel.SeekToPercent(ProgressSlider.Value);
		}
	}

	private void OnSpeedChanged(object sender, SelectionChangedEventArgs e)
	{
		if (ViewModel != null && !(SpeedCombo == null) && SpeedCombo.SelectedIndex >= 0 && SpeedCombo.SelectedIndex < SpeedOptions.Length)
		{
			ViewModel.Speed = SpeedOptions[SpeedCombo.SelectedIndex];
		}
	}

	private void OnEqGainsChanged(double[] gains)
	{
		ViewModel?.ApplyEqualizer();
	}

	private void OnEqPresetChanged(object sender, SelectionChangedEventArgs e)
	{
		if (ViewModel != null && EqPresetCombo.SelectedItem is ComboBoxItem { Content: string content })
		{
			ViewModel.ApplyEqPresetCommand?.Execute(content);
			Equalizer.Gains = ViewModel.EqGains;
		}
	}

	private void OnEqResetClick(object sender, RoutedEventArgs e)
	{
		if (ViewModel != null)
		{
			double[] gains = new double[10];
			Equalizer.Gains = gains;
			ViewModel.ApplyEqualizer();
		}
	}

	private async void OnBatchImportLyricsClick(object sender, RoutedEventArgs e)
	{
		try
		{
			FileOpenPicker fileOpenPicker = new FileOpenPicker
			{
				FileTypeFilter = { ".lrc", ".txt" }
			};
			nint windowHandle = WindowNative.GetWindowHandle(WindowManager.CurrentWindow);
			InitializeWithWindow.Initialize(fileOpenPicker, windowHandle);
			IReadOnlyList<StorageFile> readOnlyList = await fileOpenPicker.PickMultipleFilesAsync();
			if (readOnlyList == null || readOnlyList.Count == 0)
			{
				return;
			}
			string[] array = new string[readOnlyList.Count];
			for (int i = 0; i < readOnlyList.Count; i++)
			{
				array[i] = readOnlyList[i].Path;
			}
			string content = await ViewModel.ImportLyricsBatchAsync(array);
			await new ContentDialog
			{
				Title = "歌词批量导入",
				Content = content,
				CloseButtonText = "好",
				XamlRoot = XamlRoot
			}.ShowAsync();
		}
		catch (Exception ex)
		{
			await new ContentDialog
			{
				Title = "歌词批量导入",
				Content = "导入失败：" + ex.Message,
				CloseButtonText = "好",
				XamlRoot = XamlRoot
			}.ShowAsync();
		}
	}

	[GeneratedCode("Microsoft.UI.Xaml.Markup.Compiler", " 3.0.0.2409")]
	[DebuggerNonUserCode]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("ms-appx:///FurinaPlayer.UI/Pages/NowPlayingPage.xaml");
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
			Lyrics = target.As<LyricsPanel>();
			break;
		case 3:
			target.As<Button>().Click += OnBatchImportLyricsClick;
			break;
		case 4:
			Equalizer = target.As<EqualizerControl>();
			Equalizer.GainsChanged += OnEqGainsChanged;
			break;
		case 5:
			EqPresetCombo = target.As<ComboBox>();
			EqPresetCombo.SelectionChanged += OnEqPresetChanged;
			break;
		case 6:
			target.As<Button>().Click += OnEqResetClick;
			break;
		case 7:
			SpeedCombo = target.As<ComboBox>();
			SpeedCombo.SelectionChanged += OnSpeedChanged;
			break;
		case 8:
			target.As<Button>().Click += OnCycleModeClick;
			break;
		case 9:
			target.As<Button>().Click += OnPreviousClick;
			break;
		case 10:
			target.As<Button>().Click += OnPlayClick;
			break;
		case 11:
			target.As<Button>().Click += OnPauseClick;
			break;
		case 12:
			target.As<Button>().Click += OnNextClick;
			break;
		case 13:
			target.As<Button>().Click += OnStopClick;
			break;
		case 14:
			ProgressSlider = target.As<Slider>();
			ProgressSlider.PointerPressed += OnProgressPointerPressed;
			ProgressSlider.PointerReleased += OnProgressPointerReleased;
			ProgressSlider.PointerCaptureLost += OnProgressPointerReleased;
			ProgressSlider.PointerMoved += OnProgressPointerMoved;
			ProgressSlider.PointerExited += OnProgressPointerExited;
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
