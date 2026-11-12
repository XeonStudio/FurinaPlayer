using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FurinaPlayer.UI.Controls;
using FurinaPlayer.UI.ViewModels;
using Microsoft.UI;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.Web.WebView2.Core;
using SkiaSharp;
using SonicWave.Core.Models;
using Windows.ApplicationModel.DataTransfer;
using Windows.System;
using Windows.UI;

namespace FurinaPlayer.UI.Pages;

public sealed partial class StreamingPage : Page
{
	private static string? _cachedWallpaperPath;
	private static DateTime _cachedWallpaperWriteUtc;
	private static bool _cachedWallpaperIsDark;

	public StreamingViewModel? ViewModel { get; set; }

	public bool IsStreamingDark => ResolveIsStreamingDark();

	public Brush StreamingTextPrimaryBrush => new SolidColorBrush(IsStreamingDark
		? Colors.White                          // #FFFFFF
		: Color.FromArgb(255, 15, 23, 42));     // #0F172A

	public Brush StreamingTextSecondaryBrush => new SolidColorBrush(IsStreamingDark
		? Color.FromArgb(255, 241, 245, 249)   // #F1F5F9
		: Color.FromArgb(255, 51, 65, 85));     // #334155

	public Brush StreamingTextSubHeaderBrush => new SolidColorBrush(IsStreamingDark
		? Color.FromArgb(255, 226, 232, 240)   // #E2E8F0
		: Color.FromArgb(255, 71, 85, 105));    // #475569

	public Brush StreamingTextMutedBrush => new SolidColorBrush(IsStreamingDark
		? Color.FromArgb(255, 203, 213, 225)   // #CBD5E1
		: Color.FromArgb(255, 100, 116, 139));  // #64748B

	private Border? _sidebarBorder;
	private Border? _sidebarDivider;
	private ScrollViewer? _mainAreaScroll;
	private StackPanel? _mainAreaStack;
	private TextBlock? _pageTitleTb;
	private TextBlock? _platformHeaderTb;
	private Border? _platformCountBadge;
	private TextBlock? _platformCountTb;
	private TextBlock? _recentHeaderTb;
	private Border? _recentBadgeBorder;
	private TextBlock? _recentBadgeTb;
	private Button? _clearRecentBtn;
	private Border? _searchBarBorder;
	private FontIcon? _searchIcon;
	private TextBlock? _tableTitleHeaderTb;
	private readonly List<TextBlock> _tableHeaderCells = new();
	private TextBox? _searchTextBox;
	private Button? _loginBtn;
	private StackPanel? _accountCardContainer;
	private StackPanel? _platformListPanel;
	private StackPanel? _recentSearchesPanel;
	private StackPanel? _songListStackPanel;
	private StackPanel? _tabsPanel;
	private TextBlock? _statusNoticeTb;
	private Button? _loadMoreBtn;
	private ComboBox? _qualityCombo;
	private bool _refreshResultsQueued;
	private StreamingViewModel? _boundViewModel;

	public StreamingPage()
	{
		BuildLayout();
		Loaded += (s, e) => ApplyStreamingLiquidGlass();
		ActualThemeChanged += (s, e) => ApplyStreamingLiquidGlass();
		SizeChanged += (s, e) => SyncMainAreaWidth();
	}

	private bool ResolveIsStreamingDark()
	{
		try
		{
			if (IsLoaded && ActualTheme == ElementTheme.Dark)
			{
				return true;
			}

			var settings = ViewModel?.GetCurrentSettings();
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

			if (settings != null)
			{
				if (settings.Theme == ThemeMode.Dark)
				{
					return !invert;
				}
				if (settings.Theme == ThemeMode.Light)
				{
					return invert;
				}
			}

			if (IsLoaded && ActualTheme != ElementTheme.Default)
			{
				return ActualTheme == ElementTheme.Dark;
			}

			return LiquidGlassEngine.IsElementDarkTheme(this);
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
			if (!File.Exists(path)) return false;
			DateTime writeUtc = File.GetLastWriteTimeUtc(path);
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

	public void ApplyStreamingLiquidGlass()
	{
		bool isDark = IsStreamingDark;
		var primaryBrush = new SolidColorBrush(isDark ? Colors.White : Color.FromArgb(255, 15, 23, 42));
		var secondaryBrush = new SolidColorBrush(isDark ? Color.FromArgb(255, 241, 245, 249) : Color.FromArgb(255, 51, 65, 85));
		var subHeaderBrush = new SolidColorBrush(isDark ? Color.FromArgb(255, 226, 232, 240) : Color.FromArgb(255, 71, 85, 105));
		var mutedBrush = new SolidColorBrush(isDark ? Color.FromArgb(255, 203, 213, 225) : Color.FromArgb(255, 100, 116, 139));

		if (_sidebarBorder != null)
		{
			LiquidGlassEngine.AttachInteractiveLiquidGlass(_sidebarBorder, LiquidGlassVariant.FloatingBar, isDark: isDark, enablePointerTracking: true);
		}
		if (_searchBarBorder != null)
		{
			LiquidGlassEngine.AttachInteractiveLiquidGlass(_searchBarBorder, LiquidGlassVariant.Regular, isDark: isDark, enablePointerTracking: true);
		}
		if (_qualityCombo != null)
		{
			LiquidGlassEngine.AttachInteractiveLiquidGlass(_qualityCombo, LiquidGlassVariant.Clear, isDark: isDark, enablePointerTracking: false);
		}
		if (_loadMoreBtn != null)
		{
			LiquidGlassEngine.AttachInteractiveLiquidGlass(_loadMoreBtn, LiquidGlassVariant.AccentLensed, isDark: isDark, enablePointerTracking: true);
		}
		LiquidGlassEngine.UpgradeVisualTreeToLiquidGlass(this, isDark);

		if (_sidebarDivider != null)
		{
			_sidebarDivider.Background = new SolidColorBrush(isDark ? Color.FromArgb(45, 255, 255, 255) : Color.FromArgb(25, 148, 163, 184));
		}
		if (_pageTitleTb != null)
		{
			_pageTitleTb.Foreground = primaryBrush;
		}
		if (_platformHeaderTb != null)
		{
			_platformHeaderTb.Foreground = subHeaderBrush;
		}
		if (_platformCountBadge != null)
		{
			_platformCountBadge.Background = new SolidColorBrush(isDark ? Color.FromArgb(70, 99, 102, 241) : Color.FromArgb(255, 238, 242, 255));
		}
		if (_platformCountTb != null)
		{
			_platformCountTb.Foreground = new SolidColorBrush(isDark ? Color.FromArgb(255, 199, 210, 254) : Color.FromArgb(255, 99, 102, 241));
		}
		if (_recentHeaderTb != null)
		{
			_recentHeaderTb.Foreground = subHeaderBrush;
		}
		if (_recentBadgeBorder != null)
		{
			_recentBadgeBorder.Background = new SolidColorBrush(isDark ? Color.FromArgb(52, 255, 255, 255) : Color.FromArgb(255, 241, 245, 249));
		}
		if (_recentBadgeTb != null)
		{
			_recentBadgeTb.Foreground = isDark ? subHeaderBrush : mutedBrush;
		}
		if (_clearRecentBtn != null)
		{
			_clearRecentBtn.Foreground = mutedBrush;
			ApplyInteractiveButtonResources(
				_clearRecentBtn,
				normalBg: Colors.Transparent,
				hoverBg: isDark ? Color.FromArgb(28, 255, 255, 255) : Color.FromArgb(16, 15, 23, 42),
				pressedBg: isDark ? Color.FromArgb(46, 255, 255, 255) : Color.FromArgb(28, 15, 23, 42),
				normalBorder: Colors.Transparent,
				hoverBorder: Colors.Transparent,
				pressedBorder: Colors.Transparent,
				normalFg: mutedBrush,
				hoverFg: primaryBrush,
				pressedFg: primaryBrush);
		}
		if (_searchIcon != null)
		{
			_searchIcon.Foreground = mutedBrush;
		}
		if (_searchTextBox != null)
		{
			_searchTextBox.Foreground = primaryBrush;
			_searchTextBox.PlaceholderForeground = mutedBrush;
			_searchTextBox.Resources["TextControlForeground"] = primaryBrush;
			_searchTextBox.Resources["TextControlForegroundPointerOver"] = primaryBrush;
			_searchTextBox.Resources["TextControlForegroundFocused"] = primaryBrush;
			_searchTextBox.Resources["TextControlPlaceholderForeground"] = mutedBrush;
			_searchTextBox.Resources["TextControlPlaceholderForegroundPointerOver"] = mutedBrush;
			_searchTextBox.Resources["TextControlPlaceholderForegroundFocused"] = mutedBrush;
		}
		if (_qualityCombo != null)
		{
			_qualityCombo.Foreground = primaryBrush;
		}
		if (_loadMoreBtn != null)
		{
			_loadMoreBtn.Foreground = new SolidColorBrush(isDark ? Color.FromArgb(255, 199, 210, 254) : Color.FromArgb(255, 99, 102, 241));
		}
		if (_statusNoticeTb != null)
		{
			_statusNoticeTb.Foreground = mutedBrush;
		}
		foreach (var cell in _tableHeaderCells)
		{
			cell.Foreground = subHeaderBrush;
		}
		if (ViewModel != null)
		{
			UpdateTabHighlights(ViewModel.SelectedTab);
			RefreshPlatformsUi();
			UpdateLoginButtonUi();
			RefreshAccountCardUi();
			RefreshRecentSearchesUi();
			RefreshResultsUi();
		}
		else
		{
			UpdateLoginButtonUi();
		}
	}

	public StreamingPage(StreamingViewModel viewModel) : this()
	{
		ViewModel = viewModel;
		DataContext = viewModel;
		BindViewModel();
	}

	protected override void OnNavigatedTo(NavigationEventArgs e)
	{
		base.OnNavigatedTo(e);
		if (e.Parameter is StreamingViewModel vm)
		{
			ViewModel = vm;
			DataContext = vm;
			BindViewModel();
		}
		ApplyStreamingLiquidGlass();
	}

	private void BuildLayout()
	{
		Background = new SolidColorBrush(Colors.Transparent);

		var mainGrid = new Grid();
		mainGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(240) }); // 左侧二级侧栏
		mainGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.0, GridUnitType.Star) }); // 右侧主工作区

		// ==================== 1. 左侧二级侧边栏 ====================
		var sidebarBorder = new Border
		{
			BorderThickness = new Thickness(0, 0, 1, 0),
			BorderBrush = new SolidColorBrush(Color.FromArgb(35, 148, 163, 184)),
			Padding = new Thickness(14, 16, 14, 16)
		};
		_sidebarBorder = sidebarBorder;
		LiquidGlassEngine.AttachInteractiveLiquidGlass(sidebarBorder, LiquidGlassVariant.FloatingBar, isDark: IsStreamingDark, enablePointerTracking: true);

		var sidebarStack = new StackPanel { Spacing = 14 };

		// 1.1 平台列表头部
		var platformHeaderGrid = new Grid();
		platformHeaderGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.0, GridUnitType.Star) });
		platformHeaderGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

		var platTitleSp = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 6, VerticalAlignment = VerticalAlignment.Center };
		_platformHeaderTb = new TextBlock
		{
			Text = "流媒体平台",
			FontSize = 11,
			FontWeight = FontWeights.Bold,
			Foreground = StreamingTextSubHeaderBrush
		};
		platTitleSp.Children.Add(_platformHeaderTb);
		_platformCountTb = new TextBlock
		{
			Text = "5",
			FontSize = 10,
			FontWeight = FontWeights.Bold,
			Foreground = new SolidColorBrush(IsStreamingDark ? Color.FromArgb(255, 199, 210, 254) : Color.FromArgb(255, 99, 102, 241))
		};
		_platformCountBadge = new Border
		{
			Background = new SolidColorBrush(IsStreamingDark ? Color.FromArgb(70, 99, 102, 241) : Color.FromArgb(255, 238, 242, 255)),
			CornerRadius = new CornerRadius(8),
			Padding = new Thickness(6, 1, 6, 1),
			Child = _platformCountTb
		};
		platTitleSp.Children.Add(_platformCountBadge);
		Grid.SetColumn(platTitleSp, 0);
		platformHeaderGrid.Children.Add(platTitleSp);

		// 账号登录按钮 (未登录显示「账号登录」，已登录显示昵称/头像)
		_loginBtn = new Button
		{
			Padding = new Thickness(6, 3, 6, 3),
			Background = new SolidColorBrush(Colors.Transparent),
			BorderThickness = new Thickness(0),
			CornerRadius = new CornerRadius(6)
		};
		_loginBtn.Click += async (s, e) =>
		{
			string plat = ViewModel?.SelectedPlatform ?? "网易云音乐";
			if (ViewModel?.IsCurrentPlatformLoggedIn == true)
			{
				await ShowAccountDetailsDialogAsync(plat);
			}
			else
			{
				await ShowLoginDialogAsync(plat);
			}
		};
		Grid.SetColumn(_loginBtn, 1);
		platformHeaderGrid.Children.Add(_loginBtn);
		sidebarStack.Children.Add(platformHeaderGrid);

		// 1.2 账号信息卡片容器 (已登录时在平台上方展示 ShinawaseLoader 风格的账号状态)
		_accountCardContainer = new StackPanel();
		sidebarStack.Children.Add(_accountCardContainer);

		// 1.3 平台列表面板
		_platformListPanel = new StackPanel { Spacing = 2 };
		sidebarStack.Children.Add(_platformListPanel);

		// 1.4 分割线
		_sidebarDivider = new Border
		{
			Height = 1,
			Background = new SolidColorBrush(IsStreamingDark ? Color.FromArgb(45, 255, 255, 255) : Color.FromArgb(25, 148, 163, 184)),
			Margin = new Thickness(0, 6, 0, 6)
		};
		sidebarStack.Children.Add(_sidebarDivider);

		// 1.5 最近搜索头部
		var recentHeaderGrid = new Grid();
		recentHeaderGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.0, GridUnitType.Star) });
		recentHeaderGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

		var recTitleSp = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 6, VerticalAlignment = VerticalAlignment.Center };
		_recentHeaderTb = new TextBlock
		{
			Text = "最近搜索",
			FontSize = 11,
			FontWeight = FontWeights.Bold,
			Foreground = StreamingTextSubHeaderBrush
		};
		recTitleSp.Children.Add(_recentHeaderTb);
		_recentBadgeTb = new TextBlock
		{
			Text = "7",
			FontSize = 10,
			FontWeight = FontWeights.Bold,
			Foreground = IsStreamingDark ? StreamingTextSubHeaderBrush : StreamingTextMutedBrush
		};
		_recentBadgeBorder = new Border
		{
			Background = IsStreamingDark ? new SolidColorBrush(Color.FromArgb(52, 255, 255, 255)) : new SolidColorBrush(Color.FromArgb(255, 241, 245, 249)),
			CornerRadius = new CornerRadius(8),
			Padding = new Thickness(6, 1, 6, 1),
			Child = _recentBadgeTb
		};
		recTitleSp.Children.Add(_recentBadgeBorder);
		Grid.SetColumn(recTitleSp, 0);
		recentHeaderGrid.Children.Add(recTitleSp);

		_clearRecentBtn = new Button
		{
			Content = "清空",
			FontSize = 11,
			Padding = new Thickness(6, 2, 6, 2),
			Background = new SolidColorBrush(Colors.Transparent),
			BorderThickness = new Thickness(0),
			Foreground = StreamingTextMutedBrush
		};
		_clearRecentBtn.Click += (s, e) => ViewModel?.ClearRecentSearchesCommand.Execute(null);
		Grid.SetColumn(_clearRecentBtn, 1);
		recentHeaderGrid.Children.Add(_clearRecentBtn);
		sidebarStack.Children.Add(recentHeaderGrid);

		// 1.6 最近搜索项列表
		_recentSearchesPanel = new StackPanel { Spacing = 2 };
		sidebarStack.Children.Add(_recentSearchesPanel);

		var sidebarSv = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto, HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled, Content = sidebarStack };
		sidebarBorder.Child = sidebarSv;
		Grid.SetColumn(sidebarBorder, 0);
		mainGrid.Children.Add(sidebarBorder);

		// ==================== 2. 右侧主工作区 ====================
		_mainAreaScroll = new ScrollViewer
		{
			VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
			HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
			HorizontalScrollMode = ScrollMode.Disabled,
			Padding = new Thickness(18, 16, 18, 24),
			HorizontalAlignment = HorizontalAlignment.Stretch
		};
		_mainAreaScroll.Loaded += (s, e) => SyncMainAreaWidth();
		_mainAreaScroll.SizeChanged += (s, e) => SyncMainAreaWidth();

		var mainAreaStack = new StackPanel { Spacing = 16, MaxWidth = 1040, HorizontalAlignment = HorizontalAlignment.Center, MinWidth = 0 };
		_mainAreaStack = mainAreaStack;

		// 2.1 主标题
		_pageTitleTb = new TextBlock
		{
			Text = "流媒体音乐",
			FontSize = 22,
			FontWeight = FontWeights.ExtraBold,
			Foreground = StreamingTextPrimaryBrush
		};
		mainAreaStack.Children.Add(_pageTitleTb);

		// 2.2 大搜索框条 (Liquid Glass 质感)
		_searchBarBorder = new Border
		{
			CornerRadius = new CornerRadius(12),
			Padding = new Thickness(12, 4, 8, 4),
			Background = IsStreamingDark ? new SolidColorBrush(Color.FromArgb(90, 30, 41, 59)) : new SolidColorBrush(Color.FromArgb(170, 255, 255, 255)),
			BorderBrush = IsStreamingDark ? new SolidColorBrush(Color.FromArgb(50, 255, 255, 255)) : new SolidColorBrush(Color.FromArgb(70, 255, 255, 255)),
			BorderThickness = new Thickness(1.2),
			HorizontalAlignment = HorizontalAlignment.Stretch
		};
		LiquidGlassEngine.AttachInteractiveLiquidGlass(_searchBarBorder, LiquidGlassVariant.Regular, isDark: IsStreamingDark, enablePointerTracking: true);

		var searchBarGrid = new Grid();
		searchBarGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
		searchBarGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.0, GridUnitType.Star) });
		searchBarGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

		_searchIcon = new FontIcon
		{
			Glyph = "\uE721",
			FontSize = 16,
			Foreground = StreamingTextMutedBrush,
			Margin = new Thickness(4, 0, 10, 0),
			VerticalAlignment = VerticalAlignment.Center
		};
		Grid.SetColumn(_searchIcon, 0);
		searchBarGrid.Children.Add(_searchIcon);

		_searchTextBox = new TextBox
		{
			PlaceholderText = "输入歌曲名、歌手、原声带或直接粘贴网络直链 (HTTP/HTTPS/M3U8)...",
			Text = "原神",
			FontSize = 14,
			BorderThickness = new Thickness(0),
			Background = new SolidColorBrush(Colors.Transparent),
			Foreground = StreamingTextPrimaryBrush,
			PlaceholderForeground = StreamingTextMutedBrush,
			MinWidth = 0,
			HorizontalAlignment = HorizontalAlignment.Stretch,
			VerticalAlignment = VerticalAlignment.Center
		};
		_searchTextBox.KeyDown += (s, e) =>
		{
			if (e.Key == Windows.System.VirtualKey.Enter)
			{
				TriggerSearch();
			}
		};
		Grid.SetColumn(_searchTextBox, 1);
		searchBarGrid.Children.Add(_searchTextBox);

		var searchDoBtn = new Button
		{
			Width = 36,
			Height = 36,
			CornerRadius = new CornerRadius(18),
			Background = new SolidColorBrush(Color.FromArgb(255, 99, 102, 241)),
			Foreground = new SolidColorBrush(Colors.White),
			BorderThickness = new Thickness(0),
			Padding = new Thickness(0),
			Content = new FontIcon { Glyph = "\uE721", FontSize = 14, Foreground = new SolidColorBrush(Colors.White) }
		};
		ToolTipService.SetToolTip(searchDoBtn, "检索在线曲目 (Enter)");
		searchDoBtn.PointerEntered += (s, e) => searchDoBtn.Background = new SolidColorBrush(Color.FromArgb(255, 79, 70, 229));
		searchDoBtn.PointerExited += (s, e) => searchDoBtn.Background = new SolidColorBrush(Color.FromArgb(255, 99, 102, 241));
		searchDoBtn.Click += (s, e) => TriggerSearch();
		Grid.SetColumn(searchDoBtn, 2);
		searchBarGrid.Children.Add(searchDoBtn);

		_searchBarBorder.Child = searchBarGrid;
		mainAreaStack.Children.Add(_searchBarBorder);

		// 2.3 分类 Tab + 音质选择
		var filterBarGrid = new Grid { HorizontalAlignment = HorizontalAlignment.Stretch, ColumnSpacing = 8, MinWidth = 0 };
		filterBarGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.0, GridUnitType.Star) });
		filterBarGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

		_tabsPanel = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8, HorizontalAlignment = HorizontalAlignment.Left, MinWidth = 0 };
		var tabs = new[] { "歌曲", "专辑", "艺人", "歌单" };
		foreach (var t in tabs)
		{
			var tabBtn = CreateTabButton(t, isSelected: (t == "歌曲"));
			_tabsPanel.Children.Add(tabBtn);
		}
		Grid.SetColumn(_tabsPanel, 0);
		filterBarGrid.Children.Add(_tabsPanel);

		_qualityCombo = new ComboBox
		{
			CornerRadius = new CornerRadius(8),
			Padding = new Thickness(8, 4, 8, 4),
			FontSize = 12,
			MinWidth = 96,
			Margin = new Thickness(0, 0, 2, 0),
			HorizontalAlignment = HorizontalAlignment.Right,
			VerticalAlignment = VerticalAlignment.Center
		};
		_qualityCombo.Items.Add(new ComboBoxItem { Content = "高音质" });
		_qualityCombo.Items.Add(new ComboBoxItem { Content = "无损 FLAC" });
		_qualityCombo.Items.Add(new ComboBoxItem { Content = "Hi-Res FLAC" });
		_qualityCombo.Items.Add(new ComboBoxItem { Content = "标准 128k" });
		_qualityCombo.SelectedIndex = 0;
		_qualityCombo.SelectionChanged += (s, e) =>
		{
			if (_qualityCombo.SelectedItem is ComboBoxItem item && ViewModel != null)
			{
				ViewModel.SelectedQuality = item.Content.ToString() ?? "高音质";
			}
		};
		Grid.SetColumn(_qualityCombo, 1);
		filterBarGrid.Children.Add(_qualityCombo);

		mainAreaStack.Children.Add(filterBarGrid);

		// 2.4 状态通知条
		_statusNoticeTb = new TextBlock
		{
			Text = "已读取 10 个歌单，可同步全部或勾选后同步。",
			FontSize = 11,
			Foreground = StreamingTextMutedBrush,
			TextTrimming = TextTrimming.CharacterEllipsis,
			TextWrapping = TextWrapping.NoWrap,
			MaxLines = 1,
			Margin = new Thickness(2, -4, 0, 0)
		};
		mainAreaStack.Children.Add(_statusNoticeTb);

		// 2.5 歌曲列表表头
		var tableHeaderGrid = new Grid { Padding = new Thickness(8, 8, 8, 8), HorizontalAlignment = HorizontalAlignment.Stretch, MinWidth = 0 };
		ApplyResultTableColumns(tableHeaderGrid);

		_tableHeaderCells.Clear();
		TextBlock AddHeaderCell(string text, int col, HorizontalAlignment align = HorizontalAlignment.Left)
		{
			var tb = new TextBlock
			{
				Text = text,
				FontSize = 11,
				FontWeight = FontWeights.SemiBold,
				Foreground = StreamingTextSubHeaderBrush,
				HorizontalAlignment = align,
				MinWidth = 0,
				TextTrimming = TextTrimming.CharacterEllipsis
			};
			_tableHeaderCells.Add(tb);
			Grid.SetColumn(tb, col);
			tableHeaderGrid.Children.Add(tb);
			return tb;
		}

		AddHeaderCell("#", 0);
		_tableTitleHeaderTb = AddHeaderCell("歌曲", 1);
		AddHeaderCell("来源 / 音质", 2);
		AddHeaderCell("时长", 3, HorizontalAlignment.Right);
		var actionsHeaderTb = AddHeaderCell("操作", 4, HorizontalAlignment.Right);
		actionsHeaderTb.Margin = new Thickness(0, 0, 4, 0);
		mainAreaStack.Children.Add(tableHeaderGrid);

		// 2.6 歌曲数据列表
		_songListStackPanel = new StackPanel { Spacing = 2, HorizontalAlignment = HorizontalAlignment.Stretch, MinWidth = 0 };
		mainAreaStack.Children.Add(_songListStackPanel);

		// 2.7 加载更多按钮
		_loadMoreBtn = new Button
		{
			Content = "加载更多",
			HorizontalAlignment = HorizontalAlignment.Center,
			Margin = new Thickness(0, 14, 0, 10),
			Padding = new Thickness(28, 8, 28, 8),
			CornerRadius = new CornerRadius(16),
			Background = new SolidColorBrush(Color.FromArgb(25, 99, 102, 241)),
			Foreground = new SolidColorBrush(IsStreamingDark ? Color.FromArgb(255, 199, 210, 254) : Color.FromArgb(255, 99, 102, 241)),
			FontWeight = FontWeights.SemiBold
		};
		LiquidGlassEngine.AttachInteractiveLiquidGlass(_loadMoreBtn, LiquidGlassVariant.AccentLensed, isDark: IsStreamingDark, enablePointerTracking: true);
		_loadMoreBtn.Click += (s, e) => ViewModel?.LoadMoreCommand.Execute(null);
		mainAreaStack.Children.Add(_loadMoreBtn);

		var mainAreaHostGrid = new Grid { HorizontalAlignment = HorizontalAlignment.Stretch };
		mainAreaHostGrid.Children.Add(mainAreaStack);
		_mainAreaScroll.Content = mainAreaHostGrid;
		Grid.SetColumn(_mainAreaScroll, 1);
		mainGrid.Children.Add(_mainAreaScroll);

		Content = mainGrid;

		UpdateLoginButtonUi();
	}

	private void SyncMainAreaWidth()
	{
		if (_mainAreaScroll == null || _mainAreaStack == null) return;
		double hostW = _mainAreaScroll.ViewportWidth > 0 ? _mainAreaScroll.ViewportWidth : _mainAreaScroll.ActualWidth;
		if (hostW > 60)
		{
			// Subtract horizontal Padding (18+18=36) + scrollbar gutter (8) so children NEVER overflow right at any DPI/width
			double targetW = Math.Min(1040.0, Math.Max(220.0, hostW - 44.0));
			_mainAreaStack.Width = targetW;
		}
	}

	private static void ApplyResultTableColumns(Grid grid)
	{
		grid.ColumnSpacing = 6;
		grid.ColumnDefinitions.Clear();
		grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(24) }); // #
		grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.8, GridUnitType.Star) }); // 歌曲 / 专辑 / 艺人 / 歌单
		grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(0.85, GridUnitType.Star), MaxWidth = 190 }); // 来源 / 音质
		grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(42) }); // 时长
		grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(96) }); // 操作
	}

	private Border CreateTabButton(string tabName, bool isSelected)
	{
		var tabContainer = new Border
		{
			MinWidth = 44,
			HorizontalAlignment = HorizontalAlignment.Center,
			Padding = new Thickness(6, 4, 6, 6),
			CornerRadius = new CornerRadius(8),
			Background = new SolidColorBrush(Colors.Transparent),
			BorderThickness = new Thickness(0),
			BorderBrush = new SolidColorBrush(Colors.Transparent),
			Tag = tabName
		};

		var sp = new StackPanel { Spacing = 4, HorizontalAlignment = HorizontalAlignment.Center };
		var tb = new TextBlock
		{
			Text = tabName,
			FontSize = 13,
			TextAlignment = TextAlignment.Center,
			HorizontalAlignment = HorizontalAlignment.Center,
			FontWeight = isSelected ? FontWeights.Bold : FontWeights.Normal,
			Foreground = isSelected ? StreamingTextPrimaryBrush : StreamingTextMutedBrush
		};
		var indicator = new Border
		{
			Height = 2.5,
			Width = 24,
			CornerRadius = new CornerRadius(1.5),
			HorizontalAlignment = HorizontalAlignment.Center,
			Background = isSelected ? new SolidColorBrush(Color.FromArgb(255, 99, 102, 241)) : new SolidColorBrush(Colors.Transparent)
		};
		sp.Children.Add(tb);
		sp.Children.Add(indicator);
		tabContainer.Child = sp;

		tabContainer.PointerEntered += (s, e) =>
		{
			if (s is Border b)
			{
				b.Background = new SolidColorBrush(IsStreamingDark ? Color.FromArgb(24, 255, 255, 255) : Color.FromArgb(16, 15, 23, 42));
			}
		};
		tabContainer.PointerExited += (s, e) =>
		{
			if (s is Border b)
			{
				b.Background = new SolidColorBrush(Colors.Transparent);
			}
		};
		tabContainer.Tapped += (s, e) => SelectSearchTab(tabName);
		return tabContainer;
	}

	public void SelectSearchTab(string tabName)
	{
		if (ViewModel != null)
		{
			ViewModel.SelectTab(tabName);
			UpdateTabHighlights(tabName);
			if (_tableTitleHeaderTb != null)
			{
				_tableTitleHeaderTb.Text = tabName;
			}
			_mainAreaScroll?.ChangeView(null, 0, null, disableAnimation: true);
		}
	}

	private void UpdateTabHighlights(string activeTab)
	{
		if (_tabsPanel == null) return;
		foreach (var child in _tabsPanel.Children)
		{
			if (child is Border b && b.Tag is string t && b.Child is StackPanel sp)
			{
				bool isSel = (t == activeTab);
				if (sp.Children[0] is TextBlock tb)
				{
					tb.FontWeight = isSel ? FontWeights.Bold : FontWeights.Normal;
					tb.Foreground = isSel ? StreamingTextPrimaryBrush : StreamingTextMutedBrush;
				}
				if (sp.Children[1] is Border ind)
				{
					ind.Background = isSel ? new SolidColorBrush(Color.FromArgb(255, 99, 102, 241)) : new SolidColorBrush(Colors.Transparent);
				}
			}
		}
	}

	private void TriggerSearch()
	{
		if (_searchTextBox == null || ViewModel == null) return;
		string q = _searchTextBox.Text.Trim();
		if (!string.IsNullOrEmpty(q))
		{
			ViewModel.SearchAsync(q, isNew: true);
		}
	}

	private void BindViewModel()
	{
		if (ViewModel == null) return;

		if (!ReferenceEquals(_boundViewModel, ViewModel))
		{
			_boundViewModel = ViewModel;

			// 账号登录与状态变动事件监听
			ViewModel.AccountStateChanged += () =>
			{
				DispatcherQueue.TryEnqueue(() =>
				{
					UpdateLoginButtonUi();
					RefreshPlatformsUi();
					RefreshAccountCardUi();
				});
			};

			// 监听 ViewModel 属性变化
			ViewModel.PropertyChanged += (s, e) =>
			{
				DispatcherQueue.TryEnqueue(() =>
				{
					if (e.PropertyName == nameof(ViewModel.StatusMessage) && _statusNoticeTb != null)
					{
						_statusNoticeTb.Text = ViewModel.StatusMessage;
					}
					else if (e.PropertyName == nameof(ViewModel.IsSearching))
					{
						if (_loadMoreBtn != null)
						{
							_loadMoreBtn.IsEnabled = !ViewModel.IsSearching;
							_loadMoreBtn.Content = ViewModel.IsSearching ? "加载中..." : "加载更多";
						}
					}
					else if (e.PropertyName == nameof(ViewModel.IsNeteaseLoggedIn) ||
					         e.PropertyName == nameof(ViewModel.NeteaseNickname) ||
					         e.PropertyName == nameof(ViewModel.NeteaseAvatarUrl))
					{
						UpdateLoginButtonUi();
						RefreshPlatformsUi();
						RefreshAccountCardUi();
					}
				});
			};

			ViewModel.RecentSearches.CollectionChanged += (s, e) =>
			{
				DispatcherQueue.TryEnqueue(RefreshRecentSearchesUi);
			};

			ViewModel.Results.CollectionChanged += (s, e) =>
			{
				if (_refreshResultsQueued) return;
				_refreshResultsQueued = true;
				DispatcherQueue.TryEnqueue(() =>
				{
					_refreshResultsQueued = false;
					RefreshResultsUi();
				});
			};
		}

		// 初始刷新 UI
		UpdateLoginButtonUi();
		RefreshAccountCardUi();
		RefreshPlatformsUi();
		UpdateTabHighlights(ViewModel.SelectedTab);
		if (_tableTitleHeaderTb != null)
		{
			_tableTitleHeaderTb.Text = ViewModel.SelectedTab;
		}
		if (_statusNoticeTb != null)
		{
			_statusNoticeTb.Text = ViewModel.StatusMessage;
		}
		RefreshRecentSearchesUi();
		RefreshResultsUi();
	}

	/// <summary>
	/// 顶部登录按钮 UI 联动：未登录显示「登录 {平台}」，已登录显示头像、昵称与小箭头
	/// </summary>
	private void UpdateLoginButtonUi()
	{
		if (_loginBtn == null || ViewModel == null) return;

		bool isDark = IsStreamingDark;
		var primaryBrush = new SolidColorBrush(isDark ? Colors.White : Color.FromArgb(255, 15, 23, 42));
		var secondaryBrush = new SolidColorBrush(isDark ? Color.FromArgb(255, 241, 245, 249) : Color.FromArgb(255, 51, 65, 85));

		var acc = ViewModel.CurrentPlatformAccount;
		if (acc.IsLoggedIn)
		{
			Color themeColor = acc.Platform switch
			{
				"QQ 音乐" or "QQ音乐" => Color.FromArgb(255, 16, 185, 129),
				"Spotify" => Color.FromArgb(255, 30, 215, 96),
				_ => Color.FromArgb(255, 99, 102, 241)
			};
			Color iconColor = isDark && acc.Platform != "QQ 音乐" && acc.Platform != "QQ音乐" && acc.Platform != "Spotify"
				? Color.FromArgb(255, 165, 180, 252)
				: themeColor;

			var normalBg = Color.FromArgb(isDark ? (byte)38 : (byte)22, themeColor.R, themeColor.G, themeColor.B);
			var hoverBg = Color.FromArgb(isDark ? (byte)56 : (byte)34, themeColor.R, themeColor.G, themeColor.B);
			var pressedBg = Color.FromArgb(isDark ? (byte)72 : (byte)48, themeColor.R, themeColor.G, themeColor.B);
			var borderCol = Color.FromArgb(isDark ? (byte)85 : (byte)50, themeColor.R, themeColor.G, themeColor.B);

			_loginBtn.Padding = new Thickness(6, 3, 8, 3);
			_loginBtn.Background = new SolidColorBrush(normalBg);
			_loginBtn.BorderBrush = new SolidColorBrush(borderCol);
			_loginBtn.BorderThickness = new Thickness(1);
			_loginBtn.Foreground = primaryBrush;
			ApplyInteractiveButtonResources(_loginBtn, normalBg, hoverBg, pressedBg, borderCol, borderCol, borderCol, primaryBrush, primaryBrush, primaryBrush);

			var sp = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 6, VerticalAlignment = VerticalAlignment.Center };

			var avatarBorder = new Border
			{
				Width = 20,
				Height = 20,
				CornerRadius = new CornerRadius(10),
				Background = new SolidColorBrush(isDark ? Color.FromArgb(55, 99, 102, 241) : Color.FromArgb(255, 238, 242, 255)),
				Clip = new RectangleGeometry { Rect = new Windows.Foundation.Rect(0, 0, 20, 20) }
			};

			if (!string.IsNullOrEmpty(acc.AvatarUrl) && Uri.TryCreate(acc.AvatarUrl, UriKind.Absolute, out var uri))
			{
				try
				{
					avatarBorder.Child = new Image { Source = new BitmapImage(uri), Stretch = Stretch.UniformToFill };
				}
				catch
				{
					avatarBorder.Child = new FontIcon { Glyph = "\uE77B", FontSize = 10, Foreground = new SolidColorBrush(iconColor) };
				}
			}
			else
			{
				avatarBorder.Child = new FontIcon { Glyph = "\uE77B", FontSize = 10, Foreground = new SolidColorBrush(iconColor) };
			}
			sp.Children.Add(avatarBorder);

			sp.Children.Add(new TextBlock
			{
				Text = !string.IsNullOrEmpty(acc.Nickname) ? acc.Nickname : acc.UserIdStr,
				FontSize = 11,
				FontWeight = FontWeights.SemiBold,
				Foreground = primaryBrush,
				MaxWidth = 72,
				TextTrimming = TextTrimming.CharacterEllipsis,
				VerticalAlignment = VerticalAlignment.Center
			});

			sp.Children.Add(new FontIcon
			{
				Glyph = "\uE70D",
				FontSize = 8,
				Foreground = new SolidColorBrush(iconColor),
				VerticalAlignment = VerticalAlignment.Center
			});

			_loginBtn.Content = sp;
		}
		else
		{
			_loginBtn.Padding = new Thickness(6, 3, 6, 3);
			_loginBtn.Background = new SolidColorBrush(Colors.Transparent);
			_loginBtn.BorderBrush = new SolidColorBrush(Colors.Transparent);
			_loginBtn.BorderThickness = new Thickness(0);
			_loginBtn.Foreground = secondaryBrush;
			ApplyInteractiveButtonResources(
				_loginBtn,
				normalBg: Colors.Transparent,
				hoverBg: isDark ? Color.FromArgb(28, 255, 255, 255) : Color.FromArgb(16, 15, 23, 42),
				pressedBg: isDark ? Color.FromArgb(46, 255, 255, 255) : Color.FromArgb(28, 15, 23, 42),
				normalBorder: Colors.Transparent,
				hoverBorder: Colors.Transparent,
				pressedBorder: Colors.Transparent,
				normalFg: secondaryBrush,
				hoverFg: primaryBrush,
				pressedFg: primaryBrush);

			var sp = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 4, VerticalAlignment = VerticalAlignment.Center };
			sp.Children.Add(new FontIcon { Glyph = "\uE77B", FontSize = 11, Foreground = new SolidColorBrush(isDark ? Color.FromArgb(255, 165, 180, 252) : Color.FromArgb(255, 99, 102, 241)) });
			sp.Children.Add(new TextBlock { Text = $"登录 {ViewModel.SelectedPlatform}", FontSize = 11, Foreground = secondaryBrush });

			_loginBtn.Content = sp;
		}
	}

	/// <summary>
	/// 侧边栏账号概览卡片（ShinawaseLoader 风格）
	/// </summary>
	private void RefreshAccountCardUi()
	{
		if (_accountCardContainer == null || ViewModel == null) return;
		_accountCardContainer.Children.Clear();

		bool isDark = IsStreamingDark;
		var primaryBrush = new SolidColorBrush(isDark ? Colors.White : Color.FromArgb(255, 15, 23, 42));
		var mutedBrush = new SolidColorBrush(isDark ? Color.FromArgb(255, 203, 213, 225) : Color.FromArgb(255, 100, 116, 139));

		var acc = ViewModel.CurrentPlatformAccount;
		if (acc.IsLoggedIn)
		{
			Color themeColor = acc.Platform switch
			{
				"QQ 音乐" or "QQ音乐" => Color.FromArgb(255, 16, 185, 129),
				"Spotify" => Color.FromArgb(255, 30, 215, 96),
				_ => Color.FromArgb(255, 99, 102, 241)
			};
			Color iconColor = isDark && acc.Platform != "QQ 音乐" && acc.Platform != "QQ音乐" && acc.Platform != "Spotify"
				? Color.FromArgb(255, 165, 180, 252)
				: themeColor;

			var card = new Border
			{
				Background = new SolidColorBrush(Color.FromArgb(isDark ? (byte)36 : (byte)18, themeColor.R, themeColor.G, themeColor.B)),
				BorderBrush = new SolidColorBrush(Color.FromArgb(isDark ? (byte)75 : (byte)40, themeColor.R, themeColor.G, themeColor.B)),
				BorderThickness = new Thickness(1),
				CornerRadius = new CornerRadius(8),
				Padding = new Thickness(8, 6, 8, 6),
				Margin = new Thickness(0, 2, 0, 4)
			};

			var g = new Grid { ColumnSpacing = 8 };
			g.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
			g.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.0, GridUnitType.Star) });
			g.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

			var av = new Border
			{
				Width = 28,
				Height = 28,
				CornerRadius = new CornerRadius(14),
				Background = new SolidColorBrush(isDark ? Color.FromArgb(55, 99, 102, 241) : Color.FromArgb(255, 238, 242, 255)),
				Clip = new RectangleGeometry { Rect = new Windows.Foundation.Rect(0, 0, 28, 28) }
			};
			if (!string.IsNullOrEmpty(acc.AvatarUrl) && Uri.TryCreate(acc.AvatarUrl, UriKind.Absolute, out var uri))
			{
				try
				{
					av.Child = new Image { Source = new BitmapImage(uri), Stretch = Stretch.UniformToFill };
				}
				catch
				{
					av.Child = new FontIcon { Glyph = "\uE77B", FontSize = 12, Foreground = new SolidColorBrush(iconColor) };
				}
			}
			else
			{
				av.Child = new FontIcon { Glyph = "\uE77B", FontSize = 12, Foreground = new SolidColorBrush(iconColor) };
			}
			Grid.SetColumn(av, 0);
			g.Children.Add(av);

			var info = new StackPanel { Spacing = 1, VerticalAlignment = VerticalAlignment.Center };
			info.Children.Add(new TextBlock
			{
				Text = !string.IsNullOrEmpty(acc.Nickname) ? acc.Nickname : acc.UserIdStr,
				FontSize = 11.5,
				FontWeight = FontWeights.Bold,
				Foreground = primaryBrush,
				TextTrimming = TextTrimming.CharacterEllipsis
			});
			info.Children.Add(new TextBlock
			{
				Text = $"{acc.Platform} · {acc.VipStatus} · 在线",
				FontSize = 9.5,
				Foreground = new SolidColorBrush(isDark ? Color.FromArgb(255, 74, 222, 128) : Color.FromArgb(255, 22, 163, 74))
			});
			Grid.SetColumn(info, 1);
			g.Children.Add(info);

			var optBtn = new Button
			{
				Width = 24,
				Height = 24,
				Padding = new Thickness(0),
				CornerRadius = new CornerRadius(12),
				Background = new SolidColorBrush(Colors.Transparent),
				BorderThickness = new Thickness(0),
				BorderBrush = new SolidColorBrush(Colors.Transparent),
				Content = new FontIcon { Glyph = "\uE712", FontSize = 11, Foreground = mutedBrush }
			};
			ApplyFlatActionButtonStyle(optBtn);
			optBtn.Click += async (s, e) => await ShowAccountDetailsDialogAsync(acc.Platform);
			Grid.SetColumn(optBtn, 2);
			g.Children.Add(optBtn);

			card.Child = g;
			_accountCardContainer.Children.Add(card);
		}
	}

	/// <summary>
	/// 侧边栏多平台状态渲染（ShinawaseLoader 风格：状态圆点 + 平台名称与副标题 + 状态徽章）
	/// </summary>
	private void RefreshPlatformsUi()
	{
		if (_platformListPanel == null || ViewModel == null) return;
		_platformListPanel.Children.Clear();
		if (_platformCountTb != null)
		{
			_platformCountTb.Text = ViewModel.Platforms.Count.ToString();
		}

		bool isDark = IsStreamingDark;
		var unselectedTitleBrush = new SolidColorBrush(isDark ? Color.FromArgb(255, 248, 250, 252) : Color.FromArgb(255, 15, 23, 42));
		var selectedTitleBrush = new SolidColorBrush(isDark ? Color.FromArgb(255, 199, 210, 254) : Color.FromArgb(255, 79, 70, 229));
		var subtitleBrush = new SolidColorBrush(isDark ? Color.FromArgb(255, 203, 213, 225) : Color.FromArgb(255, 100, 116, 139));
		var neutralBadgeFg = new SolidColorBrush(isDark ? Color.FromArgb(255, 226, 232, 240) : Color.FromArgb(255, 100, 116, 139));

		foreach (var p in ViewModel.Platforms)
		{
			var normalBg = p.IsSelected
				? Color.FromArgb(isDark ? (byte)52 : (byte)25, 99, 102, 241)
				: Colors.Transparent;
			var hoverBg = p.IsSelected
				? Color.FromArgb(isDark ? (byte)68 : (byte)36, 99, 102, 241)
				: (isDark ? Color.FromArgb(26, 255, 255, 255) : Color.FromArgb(14, 15, 23, 42));
			var pressedBg = p.IsSelected
				? Color.FromArgb(isDark ? (byte)84 : (byte)48, 99, 102, 241)
				: (isDark ? Color.FromArgb(40, 255, 255, 255) : Color.FromArgb(24, 15, 23, 42));
			var normalBorder = p.IsSelected
				? Color.FromArgb(isDark ? (byte)110 : (byte)60, 129, 140, 248)
				: Colors.Transparent;
			var titleBrush = p.IsSelected ? selectedTitleBrush : unselectedTitleBrush;

			var btn = new Button
			{
				HorizontalAlignment = HorizontalAlignment.Stretch,
				HorizontalContentAlignment = HorizontalAlignment.Stretch,
				Padding = new Thickness(10, 7, 10, 7),
				CornerRadius = new CornerRadius(8),
				BorderThickness = new Thickness(p.IsSelected ? 1 : 0),
				BorderBrush = new SolidColorBrush(normalBorder),
				Background = new SolidColorBrush(normalBg),
				Foreground = titleBrush,
				Tag = p.Name
			};
			ApplyInteractiveButtonResources(btn, normalBg, hoverBg, pressedBg, normalBorder, normalBorder, normalBorder, titleBrush, titleBrush, titleBrush);

			var rowGrid = new Grid { ColumnSpacing = 8 };
			rowGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto }); // 状态圆点
			rowGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.0, GridUnitType.Star) }); // 平台与状态副标题
			rowGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto }); // 状态徽章 (已登录/未登录)

			// 状态圆点 (ShinawaseLoader 风格)
			bool isGood = p.IsLoggedIn || (p.IsAvailable && p.Name != "网易云音乐");
			var dot = new Border
			{
				Width = 8,
				Height = 8,
				CornerRadius = new CornerRadius(4),
				Background = isGood
					? new SolidColorBrush(Color.FromArgb(255, 34, 197, 94)) // 翠绿色
					: new SolidColorBrush(isDark ? Color.FromArgb(255, 203, 213, 225) : Color.FromArgb(255, 148, 163, 184)),
				VerticalAlignment = VerticalAlignment.Center
			};
			Grid.SetColumn(dot, 0);
			rowGrid.Children.Add(dot);

			var sp = new StackPanel { Spacing = 1, VerticalAlignment = VerticalAlignment.Center };
			sp.Children.Add(new TextBlock
			{
				Text = p.Name,
				FontSize = 12,
				FontWeight = p.IsSelected ? FontWeights.Bold : FontWeights.SemiBold,
				Foreground = titleBrush
			});
			sp.Children.Add(new TextBlock
			{
				Text = p.Subtitle,
				FontSize = 10,
				Foreground = subtitleBrush,
				TextTrimming = TextTrimming.CharacterEllipsis
			});
			Grid.SetColumn(sp, 1);
			rowGrid.Children.Add(sp);

			// 状态徽章标签 (ShinawaseLoader 风格)
			var badgeBorder = new Border
			{
				CornerRadius = new CornerRadius(4),
				Padding = new Thickness(5, 2, 5, 2),
				VerticalAlignment = VerticalAlignment.Center,
				Background = isGood
					? (isDark ? new SolidColorBrush(Color.FromArgb(65, 22, 163, 74)) : new SolidColorBrush(Color.FromArgb(255, 235, 253, 242)))
					: (isDark ? new SolidColorBrush(Color.FromArgb(52, 255, 255, 255)) : new SolidColorBrush(Color.FromArgb(255, 241, 245, 249)))
			};
			var badgeTb = new TextBlock
			{
				Text = p.StatusBadgeText,
				FontSize = 9.5,
				FontWeight = FontWeights.SemiBold,
				Foreground = isGood
					? new SolidColorBrush(isDark ? Color.FromArgb(255, 134, 239, 172) : Color.FromArgb(255, 34, 197, 94))
					: neutralBadgeFg
			};
			badgeBorder.Child = badgeTb;
			Grid.SetColumn(badgeBorder, 2);
			rowGrid.Children.Add(badgeBorder);

			btn.Content = rowGrid;
			btn.Click += (s, e) =>
			{
				if (s is Button b && b.Tag is string platName)
				{
					ViewModel.SelectPlatform(platName);
					RefreshPlatformsUi();
					UpdateLoginButtonUi();
					RefreshAccountCardUi();
				}
			};
			_platformListPanel.Children.Add(btn);
		}
	}

	/// <summary>
	/// 弹出登录认证对话框：支持网易云扫码登录（带轮询与超时处理）与 Cookie (MUSIC_U) 导入认证、QQ 音乐 Cookie 导入、Spotify 凭证导入
	/// </summary>
	private async Task ShowLoginDialogAsync(string? defaultPlatform = null)
	{
		if (ViewModel == null || this.XamlRoot == null) return;

		var dialog = new ContentDialog
		{
			Title = "流媒体账号认证与登录",
			CloseButtonText = "取消",
			CornerRadius = new CornerRadius(16),
			XamlRoot = this.XamlRoot
		};

		var cts = new CancellationTokenSource();
		dialog.Closed += (s, e) =>
		{
			try
			{
				cts.Cancel();
				cts.Dispose();
			}
			catch { }
		};

		var rootSp = new StackPanel { Width = 480, Spacing = 14 };
		bool isDark = IsStreamingDark;
		var dialogPrimaryBrush = new SolidColorBrush(isDark ? Colors.White : Color.FromArgb(255, 15, 23, 42));
		var dialogSubHeaderBrush = new SolidColorBrush(isDark ? Color.FromArgb(255, 226, 232, 240) : Color.FromArgb(255, 71, 85, 105));
		var dialogMutedBrush = new SolidColorBrush(isDark ? Color.FromArgb(255, 203, 213, 225) : Color.FromArgb(255, 100, 116, 139));

		var introTb = new TextBlock
		{
			Text = "连接各大流媒体音乐平台账号，可同步个人收藏、导入专属歌单，并直通最高音质与 VIP 音源解析播放。凭证将自动加密保存在本地配置中。",
			FontSize = 12,
			Foreground = dialogMutedBrush,
			TextWrapping = TextWrapping.Wrap
		};
		rootSp.Children.Add(introTb);

		var pivot = new Pivot();

		// ==================== 1. 网易云扫码快速登录 ====================
		var qrTab = new PivotItem
		{
			Header = new StackPanel
			{
				Orientation = Orientation.Horizontal,
				Spacing = 6,
				Children =
				{
					new FontIcon { Glyph = "\uEC64", FontSize = 12 },
					new TextBlock { Text = "网易云扫码", FontSize = 12 }
				}
			}
		};

		var qrStack = new StackPanel { Spacing = 12, HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0, 10, 0, 0) };

		var qrBorder = new Border
		{
			Width = 200,
			Height = 200,
			CornerRadius = new CornerRadius(12),
			Background = new SolidColorBrush(Colors.White),
			BorderBrush = new SolidColorBrush(Color.FromArgb(255, 226, 232, 240)),
			BorderThickness = new Thickness(1.5),
			Padding = new Thickness(10)
		};

		var qrGrid = new Grid();
		var qrImg = new Image
		{
			Width = 180,
			Height = 180,
			Stretch = Stretch.Uniform
		};
		var qrLoadingRing = new ProgressRing
		{
			Width = 36,
			Height = 36,
			IsActive = true,
			HorizontalAlignment = HorizontalAlignment.Center,
			VerticalAlignment = VerticalAlignment.Center
		};

		var expiredOverlay = new Border
		{
			Background = new SolidColorBrush(Color.FromArgb(210, 255, 255, 255)),
			CornerRadius = new CornerRadius(12),
			Visibility = Visibility.Collapsed
		};
		var refreshOverlayBtn = new Button
		{
			Content = "二维码已失效，点击刷新",
			FontSize = 11.5,
			Padding = new Thickness(12, 6, 12, 6),
			CornerRadius = new CornerRadius(6),
			HorizontalAlignment = HorizontalAlignment.Center,
			VerticalAlignment = VerticalAlignment.Center
		};
		expiredOverlay.Child = refreshOverlayBtn;

		qrGrid.Children.Add(qrImg);
		qrGrid.Children.Add(qrLoadingRing);
		qrGrid.Children.Add(expiredOverlay);
		qrBorder.Child = qrGrid;
		qrStack.Children.Add(qrBorder);

		var statusTb = new TextBlock
		{
			Text = "正在生成登录二维码...",
			FontSize = 12,
			FontWeight = FontWeights.SemiBold,
			Foreground = dialogSubHeaderBrush,
			HorizontalAlignment = HorizontalAlignment.Center
		};
		qrStack.Children.Add(statusTb);

		var qrActionSp = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 10, HorizontalAlignment = HorizontalAlignment.Center };
		var refreshBtn = new Button
		{
			Content = "刷新二维码",
			FontSize = 11,
			CornerRadius = new CornerRadius(6)
		};
		var copyLinkBtn = new Button
		{
			Content = "复制扫码直链",
			FontSize = 11,
			CornerRadius = new CornerRadius(6)
		};
		qrActionSp.Children.Add(refreshBtn);
		qrActionSp.Children.Add(copyLinkBtn);
		qrStack.Children.Add(qrActionSp);

		qrTab.Content = qrStack;
		pivot.Items.Add(qrTab);

		// ==================== 2. 网易云 Cookie 凭证导入 ====================
		var cookieTab = new PivotItem
		{
			Header = new StackPanel
			{
				Orientation = Orientation.Horizontal,
				Spacing = 6,
				Children =
				{
					new FontIcon { Glyph = "\uE8C8", FontSize = 12 },
					new TextBlock { Text = "网易云 Cookie", FontSize = 12 }
				}
			}
		};

		var cookieStack = new StackPanel { Spacing = 10, Margin = new Thickness(0, 10, 0, 0) };
		cookieStack.Children.Add(new TextBlock
		{
			Text = "支持直接粘贴网易云音乐账号凭据中的 MUSIC_U 字段值或完整 Cookie 字符串：",
			FontSize = 12,
			Foreground = dialogSubHeaderBrush,
			TextWrapping = TextWrapping.Wrap
		});

		var cookieBox = new TextBox
		{
			PlaceholderText = "粘贴包含 MUSIC_U=xxxx 的 Cookie 字符串，或直接粘贴 MUSIC_U 值...",
			AcceptsReturn = true,
			TextWrapping = TextWrapping.Wrap,
			Height = 85,
			FontSize = 12,
			CornerRadius = new CornerRadius(8)
		};
		cookieStack.Children.Add(cookieBox);

		var cookieStatusTb = new TextBlock
		{
			FontSize = 11.5,
			TextWrapping = TextWrapping.Wrap,
			Visibility = Visibility.Collapsed
		};
		cookieStack.Children.Add(cookieStatusTb);

		var cookieLoginBtn = new Button
		{
			Content = "验证并登录网易云",
			Background = new SolidColorBrush(Color.FromArgb(255, 99, 102, 241)),
			Foreground = new SolidColorBrush(Colors.White),
			FontWeight = FontWeights.SemiBold,
			CornerRadius = new CornerRadius(8),
			Padding = new Thickness(24, 7, 24, 7),
			HorizontalAlignment = HorizontalAlignment.Right
		};
		cookieLoginBtn.Click += async (s, e) =>
		{
			string txt = cookieBox.Text.Trim();
			if (string.IsNullOrEmpty(txt))
			{
				cookieStatusTb.Text = "请输入或粘贴 Cookie 内容！";
				cookieStatusTb.Foreground = new SolidColorBrush(Color.FromArgb(255, 239, 68, 68));
				cookieStatusTb.Visibility = Visibility.Visible;
				return;
			}

			cookieLoginBtn.IsEnabled = false;
			cookieLoginBtn.Content = "正在校验...";
			cookieStatusTb.Text = "正在向网易云服务器验证登录凭证...";
			cookieStatusTb.Foreground = new SolidColorBrush(Color.FromArgb(255, 99, 102, 241));
			cookieStatusTb.Visibility = Visibility.Visible;

			var (ok, msg) = await ViewModel.LoginWithCookieAsync(txt);
			cookieLoginBtn.IsEnabled = true;
			cookieLoginBtn.Content = "验证并登录网易云";

			if (ok)
			{
				cookieStatusTb.Text = msg;
				cookieStatusTb.Foreground = new SolidColorBrush(Color.FromArgb(255, 34, 197, 94));
				await Task.Delay(1000);
				dialog.Hide();
			}
			else
			{
				cookieStatusTb.Text = msg;
				cookieStatusTb.Foreground = new SolidColorBrush(Color.FromArgb(255, 239, 68, 68));
			}
		};
		cookieStack.Children.Add(cookieLoginBtn);

		cookieTab.Content = cookieStack;
		pivot.Items.Add(cookieTab);

		// ==================== 3. QQ 音乐扫码登录 (WebView2 内嵌浏览器) ====================
		var qqTab = new PivotItem
		{
			Header = new StackPanel
			{
				Orientation = Orientation.Horizontal,
				Spacing = 6,
				Children =
				{
					new FontIcon { Glyph = "\uE717", FontSize = 12 },
					new TextBlock { Text = "QQ音乐 扫码登录", FontSize = 12 }
				}
			}
		};

		var qqStack = new StackPanel { Spacing = 10, Margin = new Thickness(0, 10, 0, 0) };

		// 头部提示与工具栏
		var qqHeaderGrid = new Grid();
		qqHeaderGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.0, GridUnitType.Star) });
		qqHeaderGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

		var qqIntroSp = new StackPanel { Spacing = 2 };
		qqIntroSp.Children.Add(new TextBlock
		{
			Text = "请使用手机 QQ 或微信扫描下方二维码登录：",
			FontSize = 12,
			FontWeight = FontWeights.SemiBold,
			Foreground = dialogPrimaryBrush
		});
		qqIntroSp.Children.Add(new TextBlock
		{
			Text = "授权后自动拦截凭证并完成绿钻与 VIP 无损解析绑定",
			FontSize = 11,
			Foreground = dialogMutedBrush
		});
		Grid.SetColumn(qqIntroSp, 0);
		qqHeaderGrid.Children.Add(qqIntroSp);

		bool qqUseWeChatMode = false;
		var qqToolsSp = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 6, VerticalAlignment = VerticalAlignment.Center };
		var qqModeBtn = new Button
		{
			Content = "QQ 扫码",
			FontSize = 11,
			Padding = new Thickness(8, 4, 8, 4),
			CornerRadius = new CornerRadius(6),
			Background = new SolidColorBrush(Color.FromArgb(255, 16, 185, 129)),
			Foreground = new SolidColorBrush(Colors.White)
		};
		var wxModeBtn = new Button
		{
			Content = "微信扫码",
			FontSize = 11,
			Padding = new Thickness(8, 4, 8, 4),
			CornerRadius = new CornerRadius(6)
		};
		var qqRefreshBtn = new Button
		{
			Content = "刷新二维码",
			FontSize = 11,
			Padding = new Thickness(8, 4, 8, 4),
			CornerRadius = new CornerRadius(6)
		};
		var qqToggleManualBtn = new Button
		{
			Content = "手动 Cookie",
			FontSize = 11,
			Padding = new Thickness(8, 4, 8, 4),
			CornerRadius = new CornerRadius(6)
		};
		qqToolsSp.Children.Add(qqModeBtn);
		qqToolsSp.Children.Add(wxModeBtn);
		qqToolsSp.Children.Add(qqRefreshBtn);
		qqToolsSp.Children.Add(qqToggleManualBtn);
		Grid.SetColumn(qqToolsSp, 1);
		qqHeaderGrid.Children.Add(qqToolsSp);
		qqStack.Children.Add(qqHeaderGrid);

		// 内嵌 WebView2 容器
		var qqWebContainer = new Grid
		{
			Width = 460,
			Height = 360,
			CornerRadius = new CornerRadius(10),
			Background = new SolidColorBrush(Color.FromArgb(255, 248, 250, 252)),
			HorizontalAlignment = HorizontalAlignment.Center
		};

		var qqWebView = new WebView2
		{
			HorizontalAlignment = HorizontalAlignment.Stretch,
			VerticalAlignment = VerticalAlignment.Stretch
		};
		qqWebContainer.Children.Add(qqWebView);

		// 加载遮罩
		var qqLoadingOverlay = new Border
		{
			Background = new SolidColorBrush(Color.FromArgb(230, 248, 250, 252)),
			CornerRadius = new CornerRadius(10),
			HorizontalAlignment = HorizontalAlignment.Stretch,
			VerticalAlignment = VerticalAlignment.Stretch
		};
		var qqLoadingSp = new StackPanel { HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center, Spacing = 8 };
		var qqLoadingRing = new ProgressRing { IsActive = true, Width = 32, Height = 32 };
		var qqLoadingTb = new TextBlock
		{
			Text = "正在加载 QQ 音乐扫码登录页面...",
			FontSize = 12,
			Foreground = new SolidColorBrush(Color.FromArgb(255, 100, 116, 139))
		};
		qqLoadingSp.Children.Add(qqLoadingRing);
		qqLoadingSp.Children.Add(qqLoadingTb);
		qqLoadingOverlay.Child = qqLoadingSp;
		qqWebContainer.Children.Add(qqLoadingOverlay);

		qqStack.Children.Add(qqWebContainer);

		var qqStatusTb = new TextBlock
		{
			FontSize = 11.5,
			TextWrapping = TextWrapping.Wrap,
			Visibility = Visibility.Collapsed
		};
		qqStack.Children.Add(qqStatusTb);

		// 手动输入 Cookie 备用面板
		var qqManualPanel = new StackPanel { Spacing = 8, Visibility = Visibility.Collapsed };
		qqManualPanel.Children.Add(new TextBlock
		{
			Text = "备用方案：手动粘贴包含 uin=xxxx; qm_keyst=xxxx 的完整 Cookie：",
			FontSize = 11.5,
			Foreground = dialogMutedBrush
		});
		var qqCookieBox = new TextBox
		{
			PlaceholderText = "粘贴包含 uin=xxxx; qm_keyst=xxxx 的完整 Cookie 字符串...",
			AcceptsReturn = true,
			TextWrapping = TextWrapping.Wrap,
			Height = 70,
			FontSize = 11.5,
			CornerRadius = new CornerRadius(6)
		};
		qqManualPanel.Children.Add(qqCookieBox);
		var qqManualLoginBtn = new Button
		{
			Content = "验证并导入 Cookie",
			Background = new SolidColorBrush(Color.FromArgb(255, 16, 185, 129)),
			Foreground = new SolidColorBrush(Colors.White),
			FontWeight = FontWeights.SemiBold,
			CornerRadius = new CornerRadius(6),
			Padding = new Thickness(16, 5, 16, 5),
			HorizontalAlignment = HorizontalAlignment.Right
		};
		qqManualPanel.Children.Add(qqManualLoginBtn);
		qqStack.Children.Add(qqManualPanel);

		qqToggleManualBtn.Click += (s, e) =>
		{
			qqManualPanel.Visibility = qqManualPanel.Visibility == Visibility.Visible ? Visibility.Collapsed : Visibility.Visible;
		};

		var qqTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(1500) };
		bool qqIsProcessing = false;
		bool qqIsInitialized = false;

		string GetQQAuthTargetUrl() => qqUseWeChatMode
			? SonicWave.Core.Services.QQMusicAuthHelper.WeChatLoginQrUrl
			: SonicWave.Core.Services.QQMusicAuthHelper.QQLoginAuthorizeUrl;

		void UpdateQQModeButtonsUi()
		{
			var activeBg = new SolidColorBrush(Color.FromArgb(255, 16, 185, 129));
			var activeFg = new SolidColorBrush(Colors.White);
			var inactiveBg = new SolidColorBrush(isDark ? Color.FromArgb(50, 255, 255, 255) : Color.FromArgb(255, 241, 245, 249));
			var inactiveFg = new SolidColorBrush(isDark ? Color.FromArgb(255, 226, 232, 240) : Color.FromArgb(255, 51, 65, 85));
			qqModeBtn.Background = qqUseWeChatMode ? inactiveBg : activeBg;
			qqModeBtn.Foreground = qqUseWeChatMode ? inactiveFg : activeFg;
			wxModeBtn.Background = qqUseWeChatMode ? activeBg : inactiveBg;
			wxModeBtn.Foreground = qqUseWeChatMode ? activeFg : inactiveFg;
		}

		async Task ClearStaleQQWebCookiesAsync()
		{
			if (qqWebView.CoreWebView2 == null) return;
			var cm = qqWebView.CoreWebView2.CookieManager;
			foreach (string origin in SonicWave.Core.Services.QQMusicAuthHelper.CookieOrigins)
			{
				try
				{
					var list = await cm.GetCookiesAsync(origin);
					if (list == null) continue;
					foreach (var c in list)
					{
						cm.DeleteCookie(c);
					}
				}
				catch { }
			}
		}

		async Task NavigateQQAuthAsync(bool clearCookies)
		{
			if (qqWebView.CoreWebView2 == null) return;
			qqLoadingOverlay.Visibility = Visibility.Visible;
			qqLoadingTb.Text = qqUseWeChatMode ? "正在加载微信扫码登录页面..." : "正在加载 QQ 扫码登录页面...";
			if (clearCookies)
			{
				await ClearStaleQQWebCookiesAsync();
			}
			qqWebView.CoreWebView2.Navigate(GetQQAuthTargetUrl());
		}

		async Task EnsureQQWebViewInitializedAsync()
		{
			if (qqIsInitialized) return;
			qqIsInitialized = true;
			try
			{
				qqLoadingOverlay.Visibility = Visibility.Visible;
				qqLoadingTb.Text = "正在初始化安全内嵌浏览器...";
				await qqWebView.EnsureCoreWebView2Async();
				qqWebView.CoreWebView2.Settings.IsWebMessageEnabled = false;
				qqWebView.CoreWebView2.Settings.AreDevToolsEnabled = false;
				qqWebView.CoreWebView2.Settings.IsStatusBarEnabled = false;

				qqWebView.CoreWebView2.NewWindowRequested += (s, args) =>
				{
					args.Handled = true;
					if (!string.IsNullOrWhiteSpace(args.Uri))
					{
						qqWebView.CoreWebView2.Navigate(args.Uri);
					}
				};

				qqWebView.CoreWebView2.NavigationCompleted += async (s, args) =>
				{
					qqLoadingOverlay.Visibility = Visibility.Collapsed;
					string currentUrl = qqWebView.CoreWebView2?.Source ?? string.Empty;
					if (currentUrl.Contains("graph.qq.com/oauth2.0/show", StringComparison.OrdinalIgnoreCase))
					{
						try
						{
							await qqWebView.CoreWebView2!.ExecuteScriptAsync(SonicWave.Core.Services.QQMusicAuthHelper.CompactOAuthLayoutScript);
						}
						catch { }
					}
					if (!qqIsProcessing) qqTimer.Start();
				};

				await NavigateQQAuthAsync(clearCookies: true);
			}
			catch (Exception ex)
			{
				qqLoadingOverlay.Visibility = Visibility.Collapsed;
				qqStatusTb.Text = $"内嵌浏览器加载失败 ({ex.Message})，请使用手动 Cookie 导入。";
				qqStatusTb.Foreground = new SolidColorBrush(Color.FromArgb(255, 239, 68, 68));
				qqStatusTb.Visibility = Visibility.Visible;
				qqManualPanel.Visibility = Visibility.Visible;
			}
		}

		qqModeBtn.Click += async (s, e) =>
		{
			qqUseWeChatMode = false;
			UpdateQQModeButtonsUi();
			if (qqWebView.CoreWebView2 != null) await NavigateQQAuthAsync(clearCookies: false);
			else await EnsureQQWebViewInitializedAsync();
		};

		wxModeBtn.Click += async (s, e) =>
		{
			qqUseWeChatMode = true;
			UpdateQQModeButtonsUi();
			if (qqWebView.CoreWebView2 != null) await NavigateQQAuthAsync(clearCookies: false);
			else await EnsureQQWebViewInitializedAsync();
		};

		qqRefreshBtn.Click += async (s, e) =>
		{
			if (qqWebView.CoreWebView2 != null)
			{
				await NavigateQQAuthAsync(clearCookies: true);
			}
			else
			{
				await EnsureQQWebViewInitializedAsync();
			}
		};

		qqTimer.Tick += async (ts, te) =>
		{
			if (qqIsProcessing || cts.IsCancellationRequested || qqWebView.CoreWebView2 == null) return;

			try
			{
				var cookieManager = qqWebView.CoreWebView2.CookieManager;
				var cookieDict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

				foreach (string origin in SonicWave.Core.Services.QQMusicAuthHelper.CookieOrigins)
				{
					try
					{
						var list = await cookieManager.GetCookiesAsync(origin);
						if (list == null) continue;
						foreach (var c in list)
						{
							if (!string.IsNullOrWhiteSpace(c.Name) && !string.IsNullOrWhiteSpace(c.Value))
							{
								cookieDict[c.Name] = c.Value;
							}
						}
					}
					catch { }
				}

				if (!SonicWave.Core.Services.QQMusicAuthHelper.TryBuildAuthenticatedCookieHeader(
					cookieDict,
					qqWebView.CoreWebView2.Source,
					out string detectedUserId,
					out string cookieHeader))
				{
					return;
				}

				qqIsProcessing = true;
				qqTimer.Stop();

				qqStatusTb.Text = $"检测到登录凭据 ({detectedUserId})，正在验证账号并保存配置...";
				qqStatusTb.Foreground = new SolidColorBrush(Color.FromArgb(255, 16, 185, 129));
				qqStatusTb.Visibility = Visibility.Visible;

				var (ok, msg) = await ViewModel.LoginQQWithCookieAsync(cookieHeader);
				if (ok)
				{
					qqStatusTb.Text = $"🎉 {msg} 登录成功，正在关闭...";
					qqStatusTb.Foreground = new SolidColorBrush(Color.FromArgb(255, 34, 197, 94));
					await Task.Delay(1000);
					dialog.Hide();
				}
				else
				{
					qqStatusTb.Text = msg;
					qqStatusTb.Foreground = new SolidColorBrush(Color.FromArgb(255, 239, 68, 68));
					qqIsProcessing = false;
					qqTimer.Start();
				}
			}
			catch (Exception ex)
			{
				System.Diagnostics.Debug.WriteLine($"QQ Cookie poll exception: {ex.Message}");
			}
		};

		qqManualLoginBtn.Click += async (s, e) =>
		{
			string txt = qqCookieBox.Text.Trim();
			if (string.IsNullOrEmpty(txt))
			{
				qqStatusTb.Text = "请输入或粘贴 QQ 音乐 Cookie 内容！";
				qqStatusTb.Foreground = new SolidColorBrush(Color.FromArgb(255, 239, 68, 68));
				qqStatusTb.Visibility = Visibility.Visible;
				return;
			}

			qqManualLoginBtn.IsEnabled = false;
			qqManualLoginBtn.Content = "正在校验...";
			qqStatusTb.Text = "正在解析 QQ 音乐账号凭据...";
			qqStatusTb.Foreground = new SolidColorBrush(Color.FromArgb(255, 16, 185, 129));
			qqStatusTb.Visibility = Visibility.Visible;

			var (ok, msg) = await ViewModel.LoginQQWithCookieAsync(txt);
			qqManualLoginBtn.IsEnabled = true;
			qqManualLoginBtn.Content = "验证并导入 Cookie";

			if (ok)
			{
				qqStatusTb.Text = msg;
				qqStatusTb.Foreground = new SolidColorBrush(Color.FromArgb(255, 34, 197, 94));
				await Task.Delay(1000);
				dialog.Hide();
			}
			else
			{
				qqStatusTb.Text = msg;
				qqStatusTb.Foreground = new SolidColorBrush(Color.FromArgb(255, 239, 68, 68));
			}
		};

		dialog.Closed += (s, e) =>
		{
			try
			{
				qqTimer.Stop();
				qqWebView.Close();
			}
			catch { }
		};

		qqTab.Content = qqStack;
		pivot.Items.Add(qqTab);

		// ==================== 4. Spotify 凭据导入 ====================
		var spTab = new PivotItem
		{
			Header = new StackPanel
			{
				Orientation = Orientation.Horizontal,
				Spacing = 6,
				Children =
				{
					new FontIcon { Glyph = "\uE93C", FontSize = 12 },
					new TextBlock { Text = "Spotify 凭证", FontSize = 12 }
				}
			}
		};

		var spStack = new StackPanel { Spacing = 10, Margin = new Thickness(0, 10, 0, 0) };
		spStack.Children.Add(new TextBlock
		{
			Text = "支持导入 Spotify 官方 Access Token (BQD...) 或网页端 sp_dc 会话 Cookie，用于同步 Spotify 音乐账户：",
			FontSize = 12,
			Foreground = dialogSubHeaderBrush,
			TextWrapping = TextWrapping.Wrap
		});

		var spCredentialBox = new TextBox
		{
			PlaceholderText = "粘贴 Spotify Access Token 或 sp_dc=xxxx Cookie 字符串...",
			AcceptsReturn = true,
			TextWrapping = TextWrapping.Wrap,
			Height = 85,
			FontSize = 12,
			CornerRadius = new CornerRadius(8)
		};
		spStack.Children.Add(spCredentialBox);

		var spStatusTb = new TextBlock
		{
			FontSize = 11.5,
			TextWrapping = TextWrapping.Wrap,
			Visibility = Visibility.Collapsed
		};
		spStack.Children.Add(spStatusTb);

		var spLoginBtn = new Button
		{
			Content = "验证并登录 Spotify",
			Background = new SolidColorBrush(Color.FromArgb(255, 30, 215, 96)),
			Foreground = new SolidColorBrush(Colors.White),
			FontWeight = FontWeights.SemiBold,
			CornerRadius = new CornerRadius(8),
			Padding = new Thickness(24, 7, 24, 7),
			HorizontalAlignment = HorizontalAlignment.Right
		};
		spLoginBtn.Click += async (s, e) =>
		{
			string txt = spCredentialBox.Text.Trim();
			if (string.IsNullOrEmpty(txt))
			{
				spStatusTb.Text = "请输入或粘贴 Spotify Token 或 Cookie！";
				spStatusTb.Foreground = new SolidColorBrush(Color.FromArgb(255, 239, 68, 68));
				spStatusTb.Visibility = Visibility.Visible;
				return;
			}

			spLoginBtn.IsEnabled = false;
			spLoginBtn.Content = "正在校验...";
			spStatusTb.Text = "正在向 Spotify 验证账户凭证...";
			spStatusTb.Foreground = new SolidColorBrush(Color.FromArgb(255, 30, 215, 96));
			spStatusTb.Visibility = Visibility.Visible;

			var (ok, msg) = await ViewModel.LoginSpotifyWithCredentialAsync(txt);
			spLoginBtn.IsEnabled = true;
			spLoginBtn.Content = "验证并登录 Spotify";

			if (ok)
			{
				spStatusTb.Text = msg;
				spStatusTb.Foreground = new SolidColorBrush(Color.FromArgb(255, 34, 197, 94));
				await Task.Delay(1000);
				dialog.Hide();
			}
			else
			{
				spStatusTb.Text = msg;
				spStatusTb.Foreground = new SolidColorBrush(Color.FromArgb(255, 239, 68, 68));
			}
		};
		spStack.Children.Add(spLoginBtn);

		spTab.Content = spStack;
		pivot.Items.Add(spTab);

		// 默认激活对应所选平台的 Tab
		string activePlat = defaultPlatform ?? ViewModel.SelectedPlatform;
		if (activePlat == "QQ 音乐" || activePlat == "QQ音乐")
		{
			pivot.SelectedIndex = 2;
		}
		else if (activePlat == "Spotify")
		{
			pivot.SelectedIndex = 3;
		}
		else
		{
			pivot.SelectedIndex = 0;
		}

		rootSp.Children.Add(pivot);
		dialog.Content = rootSp;

		// 扫码逻辑控制器
		string currentUnikey = "";
		string currentQrUrl = "";

		async Task LoadQrAsync()
		{
			expiredOverlay.Visibility = Visibility.Collapsed;
			qrLoadingRing.IsActive = true;
			statusTb.Text = "正在向网易云请求登录二维码...";
			statusTb.Foreground = dialogSubHeaderBrush;

			string? unikey = await ViewModel.RequestQrKeyAsync();
			if (string.IsNullOrEmpty(unikey))
			{
				qrLoadingRing.IsActive = false;
				statusTb.Text = "获取二维码密钥失败，请检查网络后重试。";
				statusTb.Foreground = new SolidColorBrush(Color.FromArgb(255, 239, 68, 68));
				expiredOverlay.Visibility = Visibility.Visible;
				return;
			}

			currentUnikey = unikey;
			var (qrUrl, qrImgBase64) = await ViewModel.CreateQrAsync(unikey);
			currentQrUrl = qrUrl;

			try
			{
				if (!string.IsNullOrEmpty(qrImgBase64) && qrImgBase64.StartsWith("data:image"))
				{
					string pureBase64 = qrImgBase64.Substring(qrImgBase64.IndexOf(',') + 1);
					byte[] imgBytes = Convert.FromBase64String(pureBase64);
					using var ms = new MemoryStream(imgBytes);
					var ras = ms.AsRandomAccessStream();
					var bi = new BitmapImage();
					await bi.SetSourceAsync(ras);
					qrImg.Source = bi;
				}
				else
				{
					string qrApiUrl = $"https://api.qrserver.com/v1/create-qr-code/?size=200x200&margin=8&data={Uri.EscapeDataString(qrUrl)}";
					qrImg.Source = new BitmapImage(new Uri(qrApiUrl));
				}
			}
			catch
			{
				string fallbackQr = $"https://quickchart.io/qr?size=200&text={Uri.EscapeDataString(qrUrl)}";
				qrImg.Source = new BitmapImage(new Uri(fallbackQr));
			}

			qrLoadingRing.IsActive = false;
			statusTb.Text = "请使用网易云音乐手机 App 扫码登录";
			statusTb.Foreground = dialogSubHeaderBrush;

			_ = PollQrStatusAsync(unikey, cts.Token);
		}

		async Task PollQrStatusAsync(string unikey, CancellationToken ct)
		{
			while (!ct.IsCancellationRequested)
			{
				await Task.Delay(2000, ct).ContinueWith(_ => { });
				if (ct.IsCancellationRequested || unikey != currentUnikey) break;

				var (code, msg, success) = await ViewModel.CheckQrStatusAsync(unikey);
				if (success)
				{
					statusTb.Text = "授权登录成功！正在加载账号资料...";
					statusTb.Foreground = new SolidColorBrush(Color.FromArgb(255, 34, 197, 94));
					await Task.Delay(1000);
					dialog.Hide();
					break;
				}

				if (code == 800) // 二维码已过期
				{
					statusTb.Text = "二维码已过期，请点击刷新";
					statusTb.Foreground = new SolidColorBrush(Color.FromArgb(255, 239, 68, 68));
					expiredOverlay.Visibility = Visibility.Visible;
					break;
				}
				else if (code == 802) // 已扫码待确认
				{
					statusTb.Text = "扫描成功，请在手机端点击【确认登录】";
					statusTb.Foreground = new SolidColorBrush(Color.FromArgb(255, 234, 88, 12));
				}
			}
		}

		refreshBtn.Click += async (s, e) => await LoadQrAsync();
		refreshOverlayBtn.Click += async (s, e) => await LoadQrAsync();
		copyLinkBtn.Click += (s, e) =>
		{
			if (!string.IsNullOrEmpty(currentQrUrl))
			{
				var dp = new DataPackage();
				dp.SetText(currentQrUrl);
				Clipboard.SetContent(dp);
				copyLinkBtn.Content = "已复制直链！";
				_ = Task.Delay(2000).ContinueWith(_ => DispatcherQueue.TryEnqueue(() => copyLinkBtn.Content = "复制扫码直链"));
			}
		};

		pivot.SelectionChanged += async (s, e) =>
		{
			if (pivot.SelectedIndex == 0)
			{
				qqTimer.Stop();
				if (string.IsNullOrEmpty(currentUnikey)) _ = LoadQrAsync();
			}
			else if (pivot.SelectedIndex == 2)
			{
				await EnsureQQWebViewInitializedAsync();
				if (!qqIsProcessing) qqTimer.Start();
			}
			else
			{
				qqTimer.Stop();
			}
		};

		if (pivot.SelectedIndex == 0)
		{
			_ = LoadQrAsync();
		}
		else if (pivot.SelectedIndex == 2)
		{
			_ = EnsureQQWebViewInitializedAsync();
		}

		await dialog.ShowAsync();
	}

	/// <summary>
	/// 弹出 QQ 音乐内嵌 WebView2 扫码登录浮层/弹窗
	/// </summary>
	public async Task ShowQQWebLoginDialogAsync()
	{
		await ShowLoginDialogAsync("QQ 音乐");
	}

	/// <summary>
	/// 弹出已登录账号详情面板：查看平台、昵称、头像、UID/UIN、认证状态、凭据脱敏预览，支持刷新与安全退出
	/// </summary>
	private async Task ShowAccountDetailsDialogAsync(string? targetPlatform = null)
	{
		if (ViewModel == null || this.XamlRoot == null) return;

		string platform = targetPlatform ?? ViewModel.SelectedPlatform;
		var acc = ViewModel.GetAccount(platform);

		Color themeColor = platform switch
		{
			"QQ 音乐" or "QQ音乐" => Color.FromArgb(255, 16, 185, 129),
			"Spotify" => Color.FromArgb(255, 30, 215, 96),
			_ => Color.FromArgb(255, 99, 102, 241)
		};

		var dialog = new ContentDialog
		{
			Title = $"{platform} · 账号信息",
			CloseButtonText = "关闭",
			CornerRadius = new CornerRadius(16),
			XamlRoot = this.XamlRoot
		};

		var rootSp = new StackPanel { Width = 400, Spacing = 16 };
		bool isDark = IsStreamingDark;
		Color iconColor = isDark && platform != "QQ 音乐" && platform != "QQ音乐" && platform != "Spotify"
			? Color.FromArgb(255, 165, 180, 252)
			: themeColor;
		var primaryBrush = new SolidColorBrush(isDark ? Colors.White : Color.FromArgb(255, 15, 23, 42));
		var mutedBrush = new SolidColorBrush(isDark ? Color.FromArgb(255, 203, 213, 225) : Color.FromArgb(255, 100, 116, 139));

		// 账号卡片
		var cardBorder = new Border
		{
			Background = new SolidColorBrush(Color.FromArgb(isDark ? (byte)36 : (byte)18, themeColor.R, themeColor.G, themeColor.B)),
			BorderBrush = new SolidColorBrush(Color.FromArgb(isDark ? (byte)75 : (byte)40, themeColor.R, themeColor.G, themeColor.B)),
			BorderThickness = new Thickness(1),
			CornerRadius = new CornerRadius(12),
			Padding = new Thickness(16)
		};

		var cardGrid = new Grid { ColumnSpacing = 14 };
		cardGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
		cardGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.0, GridUnitType.Star) });

		var avatarBorder = new Border
		{
			Width = 56,
			Height = 56,
			CornerRadius = new CornerRadius(28),
			Background = new SolidColorBrush(isDark ? Color.FromArgb(55, 99, 102, 241) : Color.FromArgb(255, 238, 242, 255)),
			Clip = new RectangleGeometry { Rect = new Windows.Foundation.Rect(0, 0, 56, 56) }
		};
		if (!string.IsNullOrEmpty(acc.AvatarUrl) && Uri.TryCreate(acc.AvatarUrl, UriKind.Absolute, out var uri))
		{
			try
			{
				avatarBorder.Child = new Image { Source = new BitmapImage(uri), Stretch = Stretch.UniformToFill };
			}
			catch
			{
				avatarBorder.Child = new FontIcon { Glyph = "\uE77B", FontSize = 22, Foreground = new SolidColorBrush(iconColor) };
			}
		}
		else
		{
			avatarBorder.Child = new FontIcon { Glyph = "\uE77B", FontSize = 22, Foreground = new SolidColorBrush(iconColor) };
		}
		Grid.SetColumn(avatarBorder, 0);
		cardGrid.Children.Add(avatarBorder);

		var infoSp = new StackPanel { Spacing = 4, VerticalAlignment = VerticalAlignment.Center };
		infoSp.Children.Add(new TextBlock
		{
			Text = !string.IsNullOrEmpty(acc.Nickname) ? acc.Nickname : (string.IsNullOrEmpty(acc.UserIdStr) ? "认证用户" : acc.UserIdStr),
			FontSize = 16,
			FontWeight = FontWeights.Bold,
			Foreground = primaryBrush
		});

		var tagSp = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 6 };
		var vipBadge = new Border
		{
			Background = new SolidColorBrush(isDark ? Color.FromArgb(65, 99, 102, 241) : Color.FromArgb(255, 238, 242, 255)),
			CornerRadius = new CornerRadius(4),
			Padding = new Thickness(6, 1, 6, 1),
			Child = new TextBlock
			{
				Text = acc.VipStatus,
				FontSize = 10,
				FontWeight = FontWeights.SemiBold,
				Foreground = new SolidColorBrush(isDark ? Color.FromArgb(255, 199, 210, 254) : themeColor)
			}
		};
		tagSp.Children.Add(vipBadge);

		string idLabel = platform switch
		{
			"QQ 音乐" or "QQ音乐" => $"QQ: {acc.UserIdStr}",
			"Spotify" => $"ID: {acc.UserIdStr}",
			_ => $"UID: {(acc.UserId > 0 ? acc.UserId.ToString() : acc.UserIdStr)}"
		};

		if (!string.IsNullOrEmpty(acc.UserIdStr) || acc.UserId > 0)
		{
			tagSp.Children.Add(new TextBlock
			{
				Text = idLabel,
				FontSize = 10,
				Foreground = mutedBrush,
				VerticalAlignment = VerticalAlignment.Center
			});
		}
		infoSp.Children.Add(tagSp);

		infoSp.Children.Add(new TextBlock
		{
			Text = "状态: 已通过认证 · 凭据已持久化",
			FontSize = 10.5,
			Foreground = new SolidColorBrush(isDark ? Color.FromArgb(255, 74, 222, 128) : Color.FromArgb(255, 34, 197, 94))
		});

		// 凭证脱敏预览
		if (!string.IsNullOrEmpty(acc.Cookie))
		{
			string c = acc.Cookie.Trim();
			string preview = c.Length > 24
				? $"{c.Substring(0, 12)}...{c.Substring(c.Length - 8)}"
				: c;
			infoSp.Children.Add(new TextBlock
			{
				Text = $"凭证: {preview}",
				FontSize = 9.5,
				Foreground = mutedBrush,
				TextTrimming = TextTrimming.CharacterEllipsis
			});
		}

		Grid.SetColumn(infoSp, 1);
		cardGrid.Children.Add(infoSp);
		cardBorder.Child = cardGrid;
		rootSp.Children.Add(cardBorder);

		// 操作按钮
		var actionsGrid = new Grid { ColumnSpacing = 10 };
		actionsGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.0, GridUnitType.Star) });
		actionsGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.0, GridUnitType.Star) });

		var refreshBtn = new Button
		{
			Content = "刷新状态",
			HorizontalAlignment = HorizontalAlignment.Stretch,
			CornerRadius = new CornerRadius(8)
		};
		refreshBtn.Click += async (s, e) =>
		{
			refreshBtn.IsEnabled = false;
			refreshBtn.Content = "刷新中...";
			var (ok, msg) = await ViewModel.RefreshAccountStatusAsync(platform);
			refreshBtn.IsEnabled = true;
			refreshBtn.Content = "刷新状态";
			if (!ok)
			{
				dialog.Hide();
			}
		};
		Grid.SetColumn(refreshBtn, 0);
		actionsGrid.Children.Add(refreshBtn);

		var logoutBtn = new Button
		{
			Content = "退出登录",
			Foreground = new SolidColorBrush(Color.FromArgb(255, 239, 68, 68)),
			HorizontalAlignment = HorizontalAlignment.Stretch,
			CornerRadius = new CornerRadius(8)
		};
		logoutBtn.Click += (s, e) =>
		{
			ViewModel.Logout(platform);
			dialog.Hide();
		};
		Grid.SetColumn(logoutBtn, 1);
		actionsGrid.Children.Add(logoutBtn);

		rootSp.Children.Add(actionsGrid);
		dialog.Content = rootSp;

		await dialog.ShowAsync();
	}

	private void RefreshRecentSearchesUi()
	{
		if (_recentSearchesPanel == null || ViewModel == null) return;
		_recentSearchesPanel.Children.Clear();
		if (_recentBadgeTb != null)
		{
			_recentBadgeTb.Text = ViewModel.RecentSearches.Count.ToString();
		}

		bool isDark = IsStreamingDark;
		var iconBrush = new SolidColorBrush(isDark ? Color.FromArgb(255, 203, 213, 225) : Color.FromArgb(255, 100, 116, 139));
		var textBrush = new SolidColorBrush(isDark ? Color.FromArgb(255, 241, 245, 249) : Color.FromArgb(255, 51, 65, 85));
		var hoverTextBrush = new SolidColorBrush(isDark ? Colors.White : Color.FromArgb(255, 15, 23, 42));
		var hoverBg = isDark ? Color.FromArgb(26, 255, 255, 255) : Color.FromArgb(14, 15, 23, 42);
		var pressedBg = isDark ? Color.FromArgb(40, 255, 255, 255) : Color.FromArgb(24, 15, 23, 42);

		foreach (var r in ViewModel.RecentSearches)
		{
			var btn = new Button
			{
				HorizontalAlignment = HorizontalAlignment.Stretch,
				HorizontalContentAlignment = HorizontalAlignment.Stretch,
				Padding = new Thickness(8, 4, 8, 4),
				CornerRadius = new CornerRadius(6),
				Background = new SolidColorBrush(Colors.Transparent),
				BorderThickness = new Thickness(0),
				BorderBrush = new SolidColorBrush(Colors.Transparent),
				Foreground = textBrush,
				Tag = r
			};
			ApplyInteractiveButtonResources(btn, Colors.Transparent, hoverBg, pressedBg, Colors.Transparent, Colors.Transparent, Colors.Transparent, textBrush, hoverTextBrush, hoverTextBrush);

			var sp = new Grid { ColumnSpacing = 8 };
			sp.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
			sp.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.0, GridUnitType.Star) });
			var icon = new FontIcon { Glyph = "\uE721", FontSize = 11, Foreground = iconBrush, VerticalAlignment = VerticalAlignment.Center };
			Grid.SetColumn(icon, 0);
			sp.Children.Add(icon);
			var termTb = new TextBlock
			{
				Text = r,
				FontSize = 11,
				Foreground = textBrush,
				VerticalAlignment = VerticalAlignment.Center,
				TextTrimming = TextTrimming.CharacterEllipsis,
				MaxLines = 1
			};
			Grid.SetColumn(termTb, 1);
			sp.Children.Add(termTb);

			btn.Content = sp;
			btn.Click += (s, e) =>
			{
				if (s is Button b && b.Tag is string term)
				{
					if (_searchTextBox != null) _searchTextBox.Text = term;
					ViewModel.SearchAsync(term, isNew: true);
				}
			};
			_recentSearchesPanel.Children.Add(btn);
		}
	}

	private void RefreshResultsUi()
	{
		if (_songListStackPanel == null || ViewModel == null) return;
		_songListStackPanel.Children.Clear();

		bool isDark = IsStreamingDark;
		if (_tableTitleHeaderTb != null && !string.IsNullOrWhiteSpace(ViewModel.SelectedTab))
		{
			_tableTitleHeaderTb.Text = ViewModel.SelectedTab;
		}

		var titleBrush = isDark ? new SolidColorBrush(Colors.White) : StreamingTextPrimaryBrush;
		var subtitleBrush = isDark ? new SolidColorBrush(Color.FromArgb(255, 203, 213, 225)) : StreamingTextMutedBrush;
		var sourceBrush = isDark ? new SolidColorBrush(Color.FromArgb(255, 226, 232, 240)) : StreamingTextSubHeaderBrush;
		var mutedBrush = StreamingTextMutedBrush;
		var accentIconColor = isDark ? Color.FromArgb(255, 165, 180, 252) : Color.FromArgb(255, 99, 102, 241);

		foreach (var item in ViewModel.Results)
		{
			var rowCard = new Border
			{
				CornerRadius = new CornerRadius(8),
				Padding = new Thickness(8, 6, 8, 6),
				Margin = new Thickness(0, 1, 0, 1),
				Background = new SolidColorBrush(Colors.Transparent),
				HorizontalAlignment = HorizontalAlignment.Stretch,
				MinWidth = 0
			};

			rowCard.PointerEntered += (s, e) =>
			{
				if (s is Border b)
				{
					b.Background = new SolidColorBrush(Color.FromArgb(isDark ? (byte)36 : (byte)25, 99, 102, 241));
				}
			};
			rowCard.PointerExited += (s, e) =>
			{
				if (s is Border b)
				{
					b.Background = new SolidColorBrush(Colors.Transparent);
				}
			};

			var rowGrid = new Grid { HorizontalAlignment = HorizontalAlignment.Stretch, MinWidth = 0 };
			ApplyResultTableColumns(rowGrid);

			// 0. 序号
			var idxTb = new TextBlock
			{
				Text = item.DisplayIndex.ToString(),
				FontSize = 11,
				Foreground = mutedBrush,
				MinWidth = 0,
				VerticalAlignment = VerticalAlignment.Center
			};
			Grid.SetColumn(idxTb, 0);
			rowGrid.Children.Add(idxTb);

			// 1. 歌曲 / 专辑 / 艺人 / 歌单 (使用 Grid Auto + 1* 约束，彻底杜绝长标题/长艺人列表撑破右侧列与筛选栏)
			var songInfoGrid = new Grid { ColumnSpacing = 10, VerticalAlignment = VerticalAlignment.Center, HorizontalAlignment = HorizontalAlignment.Stretch, MinWidth = 0 };
			songInfoGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
			songInfoGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.0, GridUnitType.Star) });

			var coverBorder = new Border
			{
				Width = 36,
				Height = 36,
				CornerRadius = new CornerRadius(6),
				Background = isDark ? new SolidColorBrush(Color.FromArgb(40, 255, 255, 255)) : new SolidColorBrush(Color.FromArgb(255, 238, 242, 255))
			};
			if (!string.IsNullOrEmpty(item.CoverUrl) && Uri.TryCreate(item.CoverUrl, UriKind.Absolute, out var coverUri))
			{
				try
				{
					coverBorder.Background = new ImageBrush
					{
						ImageSource = new BitmapImage(coverUri)
						{
							DecodePixelWidth = 72,
							DecodePixelHeight = 72
						},
						Stretch = Stretch.UniformToFill
					};
				}
				catch
				{
					coverBorder.Child = new FontIcon { Glyph = "\uE774", FontSize = 14, Foreground = new SolidColorBrush(accentIconColor) };
				}
			}
			else
			{
				coverBorder.Child = new FontIcon { Glyph = "\uE774", FontSize = 14, Foreground = new SolidColorBrush(accentIconColor) };
			}
			Grid.SetColumn(coverBorder, 0);
			songInfoGrid.Children.Add(coverBorder);

			var titleStack = new StackPanel { Spacing = 2, VerticalAlignment = VerticalAlignment.Center, HorizontalAlignment = HorizontalAlignment.Stretch, MinWidth = 0 };
			titleStack.Children.Add(new TextBlock
			{
				Text = item.Title,
				FontSize = 12.5,
				FontWeight = isDark ? FontWeights.Bold : FontWeights.SemiBold,
				Foreground = titleBrush,
				TextTrimming = TextTrimming.CharacterEllipsis,
				TextWrapping = TextWrapping.NoWrap,
				MaxLines = 1,
				MinWidth = 0,
				HorizontalAlignment = HorizontalAlignment.Stretch
			});
			titleStack.Children.Add(new TextBlock
			{
				Text = item.ArtistAndAlbum,
				FontSize = 10.5,
				Foreground = subtitleBrush,
				TextTrimming = TextTrimming.CharacterEllipsis,
				TextWrapping = TextWrapping.NoWrap,
				MaxLines = 1,
				MinWidth = 0,
				HorizontalAlignment = HorizontalAlignment.Stretch
			});
			Grid.SetColumn(titleStack, 1);
			songInfoGrid.Children.Add(titleStack);
			Grid.SetColumn(songInfoGrid, 1);
			rowGrid.Children.Add(songInfoGrid);

			// 2. 来源 / 音质
			var qualitySp = new StackPanel { Spacing = 2, VerticalAlignment = VerticalAlignment.Center, HorizontalAlignment = HorizontalAlignment.Stretch, MinWidth = 0 };
			qualitySp.Children.Add(new TextBlock
			{
				Text = item.RawInfo.Source,
				FontSize = 11,
				FontWeight = FontWeights.SemiBold,
				Foreground = sourceBrush,
				TextTrimming = TextTrimming.CharacterEllipsis,
				TextWrapping = TextWrapping.NoWrap,
				MaxLines = 1,
				MinWidth = 0,
				HorizontalAlignment = HorizontalAlignment.Stretch
			});
			qualitySp.Children.Add(new TextBlock
			{
				Text = item.SourceAndQuality,
				FontSize = 10,
				Foreground = mutedBrush,
				TextTrimming = TextTrimming.CharacterEllipsis,
				TextWrapping = TextWrapping.NoWrap,
				MaxLines = 1,
				MinWidth = 0,
				HorizontalAlignment = HorizontalAlignment.Stretch
			});
			Grid.SetColumn(qualitySp, 2);
			rowGrid.Children.Add(qualitySp);

			// 3. 时长
			var durTb = new TextBlock
			{
				Text = item.DurationText,
				FontSize = 11,
				Foreground = mutedBrush,
				MinWidth = 0,
				HorizontalAlignment = HorizontalAlignment.Right,
				VerticalAlignment = VerticalAlignment.Center
			};
			Grid.SetColumn(durTb, 3);
			rowGrid.Children.Add(durTb);

			// 4. 操作按钮组
			var actionsSp = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 2, HorizontalAlignment = HorizontalAlignment.Right, VerticalAlignment = VerticalAlignment.Center, MinWidth = 0 };

			var playBtn = new Button
			{
				Width = 28,
				Height = 28,
				Padding = new Thickness(0),
				CornerRadius = new CornerRadius(14),
				Background = new SolidColorBrush(Colors.Transparent),
				BorderThickness = new Thickness(0),
				BorderBrush = new SolidColorBrush(Colors.Transparent),
				Content = new FontIcon { Glyph = "\uE768", FontSize = 12, Foreground = new SolidColorBrush(accentIconColor) },
				Tag = item
			};
			ApplyFlatActionButtonStyle(playBtn);
			playBtn.Click += async (s, e) =>
			{
				if (s is Button b && b.Tag is StreamingItemViewModel m)
				{
					await ViewModel.PlayTrackAsync(m);
				}
			};
			actionsSp.Children.Add(playBtn);

			var queueBtn = new Button
			{
				Width = 28,
				Height = 28,
				Padding = new Thickness(0),
				CornerRadius = new CornerRadius(14),
				Background = new SolidColorBrush(Colors.Transparent),
				BorderThickness = new Thickness(0),
				BorderBrush = new SolidColorBrush(Colors.Transparent),
				Content = new FontIcon { Glyph = "\uE93C", FontSize = 11, Foreground = mutedBrush },
				Tag = item
			};
			ApplyFlatActionButtonStyle(queueBtn);
			queueBtn.Click += async (s, e) =>
			{
				if (s is Button b && b.Tag is StreamingItemViewModel m)
				{
					await ViewModel.AddToQueueAsync(m);
				}
			};
			actionsSp.Children.Add(queueBtn);

			var downloadBtn = new Button
			{
				Width = 28,
				Height = 28,
				Padding = new Thickness(0),
				CornerRadius = new CornerRadius(14),
				Background = new SolidColorBrush(Colors.Transparent),
				BorderThickness = new Thickness(0),
				BorderBrush = new SolidColorBrush(Colors.Transparent),
				Content = new FontIcon { Glyph = "\uE896", FontSize = 11, Foreground = mutedBrush },
				Tag = item
			};
			ApplyFlatActionButtonStyle(downloadBtn);
			downloadBtn.Click += async (s, e) =>
			{
				if (s is Button b && b.Tag is StreamingItemViewModel m)
				{
					await ViewModel.DownloadTrackAsync(m);
				}
			};
			actionsSp.Children.Add(downloadBtn);

			Grid.SetColumn(actionsSp, 4);
			rowGrid.Children.Add(actionsSp);

			rowCard.Child = rowGrid;

			rowCard.DoubleTapped += async (s, e) =>
			{
				await ViewModel.PlayTrackAsync(item);
			};

			_songListStackPanel.Children.Add(rowCard);
		}

		SyncMainAreaWidth();
	}

	private static void ApplyInteractiveButtonResources(
		Button btn,
		Color normalBg,
		Color hoverBg,
		Color pressedBg,
		Color normalBorder,
		Color hoverBorder,
		Color pressedBorder,
		Brush normalFg,
		Brush hoverFg,
		Brush pressedFg)
	{
		btn.Resources["ButtonBackground"] = new SolidColorBrush(normalBg);
		btn.Resources["ButtonBackgroundPointerOver"] = new SolidColorBrush(hoverBg);
		btn.Resources["ButtonBackgroundPressed"] = new SolidColorBrush(pressedBg);
		btn.Resources["ButtonBorderBrush"] = new SolidColorBrush(normalBorder);
		btn.Resources["ButtonBorderBrushPointerOver"] = new SolidColorBrush(hoverBorder);
		btn.Resources["ButtonBorderBrushPressed"] = new SolidColorBrush(pressedBorder);
		btn.Resources["ButtonForeground"] = normalFg;
		btn.Resources["ButtonForegroundPointerOver"] = hoverFg;
		btn.Resources["ButtonForegroundPressed"] = pressedFg;
	}

	private static void ApplyFlatActionButtonStyle(Button btn)
	{
		btn.Background = new SolidColorBrush(Colors.Transparent);
		btn.BorderThickness = new Thickness(0);
		btn.BorderBrush = new SolidColorBrush(Colors.Transparent);
		btn.Resources["ButtonBorderThemeThickness"] = new Thickness(0);
		btn.Resources["ButtonBorderBrush"] = new SolidColorBrush(Colors.Transparent);
		btn.Resources["ButtonBorderBrushPointerOver"] = new SolidColorBrush(Colors.Transparent);
		btn.Resources["ButtonBorderBrushPressed"] = new SolidColorBrush(Colors.Transparent);
		btn.Resources["ButtonBackground"] = new SolidColorBrush(Colors.Transparent);
		btn.Resources["ButtonBackgroundPointerOver"] = new SolidColorBrush(Color.FromArgb(20, 99, 102, 241));
		btn.Resources["ButtonBackgroundPressed"] = new SolidColorBrush(Color.FromArgb(40, 99, 102, 241));
		btn.PointerEntered += (s, e) =>
		{
			btn.Background = new SolidColorBrush(Color.FromArgb(20, 99, 102, 241));
			btn.BorderThickness = new Thickness(0);
			btn.BorderBrush = new SolidColorBrush(Colors.Transparent);
		};
		btn.PointerExited += (s, e) =>
		{
			btn.Background = new SolidColorBrush(Colors.Transparent);
			btn.BorderThickness = new Thickness(0);
			btn.BorderBrush = new SolidColorBrush(Colors.Transparent);
		};
	}
}
