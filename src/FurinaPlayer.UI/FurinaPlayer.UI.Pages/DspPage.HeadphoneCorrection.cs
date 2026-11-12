using IOPath = System.IO.Path;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using FurinaPlayer.UI.Controls;
using Microsoft.UI;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI.Xaml.Shapes;
using SkiaSharp;
using SkiaSharp.Views.Windows;
using SonicWave.Audio.Dsp;
using Windows.Foundation;
using Windows.UI;

namespace FurinaPlayer.UI.Pages;

public sealed partial class DspPage
{
	private FrameworkElement? _headphoneCorrectionWorkbench;
	private TextBlock? _headphoneNavSubTb;

	// State
	private string _activeSource = "opra"; // "opra" or "autoeq"
	private string _activeTab = "catalog"; // "catalog", "favorites", "recent"
	private OpraVendor? _currentVendor = null;
	private OpraProduct? _currentProduct = null;
	private OpraHeadphonePreset? _currentPreset = null;
	private string? _appliedPresetId = null;
	private bool _isCalibrationEnabled = false;
	private string _calibrationMode = "original"; // "original" or "listening"
	private double? _userPreampDb = null;
	private string _searchQuery = "";

	private readonly HashSet<string> _favorites = new(StringComparer.OrdinalIgnoreCase);
	private readonly List<string> _recents = new();

	// UI Controls
	private TextBlock? _hcBannerTitleTb;
	private TextBlock? _hcBannerSubtitleTb;
	private TextBlock? _hcBannerStatsTb;

	private Button? _btnBreadcrumbVendor;
	private Button? _btnBreadcrumbModel;
	private Button? _btnBreadcrumbSource;
	private FontIcon? _chevronBreadcrumb1;
	private FontIcon? _chevronBreadcrumb2;

	private Button? _btnTabCatalog;
	private Button? _btnTabFavorites;
	private Border? _tabFavoritesBadge;
	private TextBlock? _tabFavoritesCountTb;
	private Button? _btnTabRecent;
	private Border? _tabRecentBadge;
	private TextBlock? _tabRecentCountTb;
	private TextBlock? _hcTabTotalCountTb;

	private TextBox? _hcSearchBox;
	private ToggleSwitch? _hcMasterSwitch;
	private Button? _hcSourceOpraBtn;
	private Button? _hcSourceAutoEqBtn;

	private Grid? _hcContentHost;
	private Grid? _hcRootGrid;
	private Border? _hcTitleIconBorder;
	private FontIcon? _hcTitleIcon;
	private Border? _hcSourceContainer;
	private Button? _hcRefreshBtn;
	private Border? _hcBannerBorder;
	private Border? _hcBannerIconBox;
	private Button? _hcBackBtn;
	private Border? _hcDetailPanelBorder;
	private Border? _hcCurveBorder;
	private Button? _hcRestorePreampBtn;
	private readonly List<Border> _hcVendorCards = new();
	private readonly List<Border> _hcModelRows = new();
	private HeadphoneCurveControl? _curveControl;
	private Slider? _hpPreampSlider;
	private TextBlock? _hpPreampValueTb;
	private Button? _btnApplyPreset;
	private Button? _btnFavPreset;
	private StackPanel? _sourcesStack;
	private Button? _modeOriginalBtn;
	private Button? _modeListeningBtn;
	private TextBlock? _modeDescTb;
	private TextBlock? _metricsFiltersTb;
	private TextBlock? _metricsAdjustedTb;
	private TextBlock? _curveAuthorTb;
	private TextBlock? _detailVendorTb;
	private TextBlock? _detailProductTb;
	private Grid? _detailPanel;
	private Grid? _emptyDetailPanel;

	public FrameworkElement CreateHeadphoneCorrectionWorkbench()
	{
		if (_headphoneCorrectionWorkbench != null)
		{
			RefreshHeadphoneCorrectionTheme(IsDspDark);
			return _headphoneCorrectionWorkbench;
		}

		LoadHeadphoneState();

		var root = new Grid
		{
			Background = CreateWhiteAcrylicBrush(0.72, 195),
			Padding = new Thickness(24, 18, 24, 20),
			RowSpacing = 12
		};
		_hcRootGrid = root;

		root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // 0: Top Status Bar
		root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // 1: Preset Status & Breadcrumbs Banner
		root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // 2: Secondary Category Tabs
		root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1.0, GridUnitType.Star) }); // 3: Main Workbench Content

		// ==========================================
		// 0. TOP STATUS BAR (顶部状态栏)
		// ==========================================
		var topBar = new Grid();
		topBar.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto }); // Left title & icon
		topBar.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto }); // Mode switch [OPRA] [AutoEQ]
		topBar.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.0, GridUnitType.Star) }); // Center / Search
		topBar.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto }); // Right Master Switch & Pills

		// Left Title
		var titleSp = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 10, VerticalAlignment = VerticalAlignment.Center };
		_hcTitleIcon = new FontIcon
		{
			Glyph = "\uE7F5",
			FontSize = 17,
			Foreground = DspAccentSoftTextBrush,
			HorizontalAlignment = HorizontalAlignment.Center,
			VerticalAlignment = VerticalAlignment.Center
		};
		var iconBorder = new Border
		{
			Width = 34,
			Height = 34,
			CornerRadius = new CornerRadius(17),
			Background = DspAccentSoftBgBrush,
			Child = _hcTitleIcon
		};
		_hcTitleIconBorder = iconBorder;
		titleSp.Children.Add(iconBorder);
		titleSp.Children.Add(new TextBlock
		{
			Text = "耳机校正",
			FontSize = 18,
			FontWeight = FontWeights.SemiBold,
			Foreground = DspTextPrimaryBrush,
			VerticalAlignment = VerticalAlignment.Center
		});
		Grid.SetColumn(titleSp, 0);
		topBar.Children.Add(titleSp);

		// Source Selector [OPRA] [AutoEQ]
		var sourceContainer = new Border
		{
			Background = IsDspDark ? new SolidColorBrush(Color.FromArgb(255, 30, 41, 59)) : new SolidColorBrush(Color.FromArgb(255, 241, 245, 249)),
			BorderBrush = IsDspDark ? new SolidColorBrush(Color.FromArgb(70, 255, 255, 255)) : new SolidColorBrush(Color.FromArgb(255, 226, 232, 240)),
			BorderThickness = new Thickness(1),
			CornerRadius = new CornerRadius(16),
			Padding = new Thickness(2),
			Margin = new Thickness(18, 0, 16, 0),
			VerticalAlignment = VerticalAlignment.Center
		};
		_hcSourceContainer = sourceContainer;
		var sourceSp = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 2 };

		_hcSourceOpraBtn = CreateSourceButton("OPRA", true);
		_hcSourceOpraBtn.Click += (s, e) => SwitchSource("opra");

		_hcSourceAutoEqBtn = CreateSourceButton("AutoEQ", false);
		_hcSourceAutoEqBtn.Click += (s, e) => SwitchSource("autoeq");

		sourceSp.Children.Add(_hcSourceOpraBtn);
		sourceSp.Children.Add(_hcSourceAutoEqBtn);
		sourceContainer.Child = sourceSp;
		Grid.SetColumn(sourceContainer, 1);
		topBar.Children.Add(sourceContainer);

		// Center Search & Refresh
		var searchSp = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8, VerticalAlignment = VerticalAlignment.Center };
		_hcSearchBox = new TextBox
		{
			PlaceholderText = "搜索品牌或型号,例如 HD 650",
			Width = 270,
			Height = 34,
			CornerRadius = new CornerRadius(17),
			Background = IsDspDark ? new SolidColorBrush(Color.FromArgb(140, 30, 41, 59)) : new SolidColorBrush(Color.FromArgb(220, 255, 255, 255)),
			BorderBrush = IsDspDark ? new SolidColorBrush(Color.FromArgb(70, 255, 255, 255)) : new SolidColorBrush(Color.FromArgb(255, 226, 232, 240)),
			Foreground = DspTextPrimaryBrush,
			VerticalAlignment = VerticalAlignment.Center
		};
		_hcSearchBox.TextChanged += (s, e) =>
		{
			_searchQuery = _hcSearchBox.Text.Trim();
			OnSearchChanged();
		};
		searchSp.Children.Add(_hcSearchBox);

		var refreshBtn = new Button
		{
			Width = 34,
			Height = 34,
			CornerRadius = new CornerRadius(17),
			Padding = new Thickness(0),
			Background = IsDspDark ? new SolidColorBrush(Color.FromArgb(120, 30, 41, 59)) : new SolidColorBrush(Color.FromArgb(200, 255, 255, 255)),
			BorderBrush = IsDspDark ? new SolidColorBrush(Color.FromArgb(70, 255, 255, 255)) : new SolidColorBrush(Color.FromArgb(255, 226, 232, 240)),
			Content = new FontIcon { Glyph = "\uE72C", FontSize = 13, Foreground = DspTextMutedBrush }
		};
		_hcRefreshBtn = refreshBtn;
		ToolTipService.SetToolTip(refreshBtn, "刷新库");
		refreshBtn.Click += (s, e) =>
		{
			OpraDatabaseService.EnsureFullDatabaseLoaded();
			RefreshCurrentView();
		};
		searchSp.Children.Add(refreshBtn);

		Grid.SetColumn(searchSp, 2);
		topBar.Children.Add(searchSp);

		// Right Controls & Pills
		var rightSp = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 10, VerticalAlignment = VerticalAlignment.Center };

		// Master ToggleSwitch
		_hcMasterSwitch = new ToggleSwitch
		{
			OffContent = "已关闭",
			OnContent = "已开启",
			IsOn = _isCalibrationEnabled,
			VerticalAlignment = VerticalAlignment.Center,
			Margin = new Thickness(0, 0, 8, 0)
		};
		_hcMasterSwitch.Toggled += (s, e) => ToggleCalibration(_hcMasterSwitch.IsOn);
		rightSp.Children.Add(_hcMasterSwitch);

		Grid.SetColumn(rightSp, 3);
		topBar.Children.Add(rightSp);

		Grid.SetRow(topBar, 0);
		root.Children.Add(topBar);

		// ==========================================
		// 1. PRESET STATUS & BREADCRUMB GUIDE BANNER
		// ==========================================
		var banner = new Border
		{
			Background = IsDspDark ? new SolidColorBrush(Color.FromArgb(160, 30, 41, 59)) : new SolidColorBrush(Color.FromArgb(170, 255, 255, 255)),
			BorderBrush = IsDspDark ? new SolidColorBrush(Color.FromArgb(80, 255, 255, 255)) : new SolidColorBrush(Color.FromArgb(160, 226, 232, 240)),
			BorderThickness = new Thickness(1),
			CornerRadius = new CornerRadius(12),
			Padding = new Thickness(18, 12, 18, 12)
		};
		_hcBannerBorder = banner;

		var bannerGrid = new Grid();
		bannerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.0, GridUnitType.Star) });
		bannerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

		var bannerLeftSp = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 14, VerticalAlignment = VerticalAlignment.Center };
		var bannerIconBox = new Border
		{
			Width = 42,
			Height = 42,
			CornerRadius = new CornerRadius(10),
			Background = IsDspDark ? new SolidColorBrush(Color.FromArgb(255, 49, 46, 129)) : new SolidColorBrush(Color.FromArgb(255, 241, 245, 249)),
			Child = new FontIcon
			{
				Glyph = "\uE7F5",
				FontSize = 20,
				Foreground = new SolidColorBrush(Color.FromArgb(255, 99, 102, 241)),
				HorizontalAlignment = HorizontalAlignment.Center,
				VerticalAlignment = VerticalAlignment.Center
			}
		};
		_hcBannerIconBox = bannerIconBox;
		bannerLeftSp.Children.Add(bannerIconBox);

		var bannerTextSp = new StackPanel { Spacing = 2, VerticalAlignment = VerticalAlignment.Center };
		_hcBannerTitleTb = new TextBlock
		{
			Text = "未选择预设",
			FontSize = 14,
			FontWeight = FontWeights.SemiBold,
			Foreground = DspTextPrimaryBrush
		};
		_hcBannerSubtitleTb = new TextBlock
		{
			Text = "按生产商或型号浏览，找到合适的校正预设。",
			FontSize = 12,
			Foreground = DspTextMutedBrush
		};
		_hcBannerStatsTb = new TextBlock
		{
			Text = "714 个品牌  6230 款耳机",
			FontSize = 11,
			Foreground = DspTextSubHeaderBrush
		};
		bannerTextSp.Children.Add(_hcBannerTitleTb);
		bannerTextSp.Children.Add(_hcBannerSubtitleTb);
		bannerTextSp.Children.Add(_hcBannerStatsTb);
		bannerLeftSp.Children.Add(bannerTextSp);

		Grid.SetColumn(bannerLeftSp, 0);
		bannerGrid.Children.Add(bannerLeftSp);

		// Right Breadcrumb Guide
		var breadcrumbSp = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8, VerticalAlignment = VerticalAlignment.Center };

		_btnBreadcrumbVendor = CreateBreadcrumbButton("\uE71D", "制造商", true);
		_btnBreadcrumbVendor.Click += (s, e) => ShowAllVendorsView();

		_chevronBreadcrumb1 = new FontIcon { Glyph = "\uE76C", FontSize = 9, Foreground = DspTextMutedBrush };

		_btnBreadcrumbModel = CreateBreadcrumbButton("\uE7F5", "型号", false);
		_btnBreadcrumbModel.Click += (s, e) =>
		{
			if (_currentVendor != null) ShowVendorModelsView(_currentVendor);
		};

		_chevronBreadcrumb2 = new FontIcon { Glyph = "\uE76C", FontSize = 9, Foreground = DspTextMutedBrush };

		_btnBreadcrumbSource = CreateBreadcrumbButton("\uE995", "校正来源", false);

		breadcrumbSp.Children.Add(_btnBreadcrumbVendor);
		breadcrumbSp.Children.Add(_chevronBreadcrumb1);
		breadcrumbSp.Children.Add(_btnBreadcrumbModel);
		breadcrumbSp.Children.Add(_chevronBreadcrumb2);
		breadcrumbSp.Children.Add(_btnBreadcrumbSource);

		Grid.SetColumn(breadcrumbSp, 1);
		bannerGrid.Children.Add(breadcrumbSp);

		banner.Child = bannerGrid;
		Grid.SetRow(banner, 1);
		root.Children.Add(banner);

		// ==========================================
		// 2. SECONDARY CATEGORY TABS
		// ==========================================
		var tabGrid = new Grid();
		tabGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.0, GridUnitType.Star) });
		tabGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

		var tabsLeftSp = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8, VerticalAlignment = VerticalAlignment.Center };

		_btnTabCatalog = CreateCategoryTabButton("\uE71D", "所有生产商", true);
		_btnTabCatalog.Click += (s, e) => SwitchTab("catalog");

		_btnTabFavorites = CreateCategoryTabButton("\uE734", "收藏型号", false, out _tabFavoritesBadge, out _tabFavoritesCountTb);
		_btnTabFavorites.Click += (s, e) => SwitchTab("favorites");

		_btnTabRecent = CreateCategoryTabButton("\uE81C", "最近使用", false, out _tabRecentBadge, out _tabRecentCountTb);
		_btnTabRecent.Click += (s, e) => SwitchTab("recent");

		tabsLeftSp.Children.Add(_btnTabCatalog);
		tabsLeftSp.Children.Add(_btnTabFavorites);
		tabsLeftSp.Children.Add(_btnTabRecent);

		Grid.SetColumn(tabsLeftSp, 0);
		tabGrid.Children.Add(tabsLeftSp);

		_hcTabTotalCountTb = new TextBlock
		{
			Text = "714",
			FontSize = 13,
			FontWeight = FontWeights.Medium,
			Foreground = DspTextMutedBrush,
			VerticalAlignment = VerticalAlignment.Center,
			Margin = new Thickness(0, 0, 8, 0)
		};
		Grid.SetColumn(_hcTabTotalCountTb, 1);
		tabGrid.Children.Add(_hcTabTotalCountTb);

		Grid.SetRow(tabGrid, 2);
		root.Children.Add(tabGrid);

		// ==========================================
		// 3. MAIN WORKBENCH CONTENT HOST
		// ==========================================
		_hcContentHost = new Grid();
		Grid.SetRow(_hcContentHost, 3);
		root.Children.Add(_hcContentHost);

		_headphoneCorrectionWorkbench = new Border { Child = root };

		// Initial display
		UpdateFavoritesAndRecentsBadges();
		ShowAllVendorsView();

		// If a preset was previously applied, restore UI
		if (!string.IsNullOrEmpty(_appliedPresetId))
		{
			var p = OpraDatabaseService.GetPresetById(_appliedPresetId);
			if (p != null)
			{
				_currentPreset = p;
				_currentProduct = OpraDatabaseService.GetProductById(p.ProductId);
				UpdateBannerWithPreset(p);
			}
		}

		return _headphoneCorrectionWorkbench;
	}

	// ----------------------------------------------------
	// VIEWS RENDERING
	// ----------------------------------------------------

	private void ShowAllVendorsView()
	{
		if (_hcContentHost == null) return;
		_currentVendor = null;
		_hcBackBtn = null;
		_hcDetailPanelBorder = null;
		_hcCurveBorder = null;
		_hcRestorePreampBtn = null;
		_hcVendorCards.Clear();
		_hcModelRows.Clear();
		UpdateBreadcrumbs();

		_hcContentHost.Children.Clear();

		var container = new Grid { RowSpacing = 10 };
		container.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Header
		container.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1.0, GridUnitType.Star) }); // Cards Grid

		// Subheader: "所有生产商 6230款耳机 · 12594条曲线"
		var subHeaderSp = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 10, Margin = new Thickness(4, 2, 0, 4) };
		subHeaderSp.Children.Add(new TextBlock
		{
			Text = "所有生产商",
			FontSize = 14,
			FontWeight = FontWeights.Bold,
			Foreground = DspTextPrimaryBrush,
			VerticalAlignment = VerticalAlignment.Center
		});
		var (vCount, pCount, eqCount) = OpraDatabaseService.GetDatabaseStats();
		subHeaderSp.Children.Add(new TextBlock
		{
			Text = $"{pCount} 款耳机 · {eqCount} 条曲线",
			FontSize = 11,
			Foreground = DspTextMutedBrush,
			VerticalAlignment = VerticalAlignment.Center
		});
		Grid.SetRow(subHeaderSp, 0);
		container.Children.Add(subHeaderSp);

		// Brand Cards Grid in ScrollViewer with ample breathing padding
		var sv = new ScrollViewer
		{
			VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
			HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
			Padding = new Thickness(16, 12, 20, 28)
		};

		var wrapPanel = new SimpleWrapPanel
		{
			ItemWidth = 164,
			ItemHeight = 154
		};

		var vendors = OpraDatabaseService.GetVendors(_activeSource);
		foreach (var vendor in vendors)
		{
			wrapPanel.Children.Add(CreateVendorCard(vendor));
		}

		sv.Content = wrapPanel;
		Grid.SetRow(sv, 1);
		container.Children.Add(sv);

		_hcContentHost.Children.Add(container);
		ApplyThemeToHcContentHost(IsDspDark, container);
	}

	private FrameworkElement CreateVendorCard(OpraVendor vendor)
	{
		bool isDark = IsDspDark;
		var card = new Border
		{
			Width = 146,
			Height = 138,
			MinWidth = 146,
			MinHeight = 138,
			Margin = new Thickness(8),
			CornerRadius = new CornerRadius(14),
			Background = isDark ? new SolidColorBrush(Color.FromArgb(160, 30, 41, 59)) : new SolidColorBrush(Color.FromArgb(245, 255, 255, 255)),
			BorderBrush = isDark ? new SolidColorBrush(Color.FromArgb(85, 255, 255, 255)) : new SolidColorBrush(Color.FromArgb(255, 226, 232, 240)),
			BorderThickness = new Thickness(1.2),
			Padding = new Thickness(10, 12, 10, 10)
		};
		_hcVendorCards.Add(card);

		var sp = new StackPanel
		{
			Spacing = 4,
			VerticalAlignment = VerticalAlignment.Center,
			HorizontalAlignment = HorizontalAlignment.Center
		};

		// Logo Container
		var logoBox = new Border
		{
			Width = 96,
			Height = 46,
			HorizontalAlignment = HorizontalAlignment.Center,
			VerticalAlignment = VerticalAlignment.Center
		};

		if (!string.IsNullOrEmpty(vendor.LogoUrl))
		{
			try
			{
				var img = new Image
				{
					Source = new BitmapImage(new Uri(vendor.LogoUrl)),
					MaxWidth = 88,
					MaxHeight = 38,
					Stretch = Stretch.Uniform,
					HorizontalAlignment = HorizontalAlignment.Center,
					VerticalAlignment = VerticalAlignment.Center
				};
				logoBox.Child = img;
			}
			catch
			{
				logoBox.Child = CreateBrandTypography(vendor.Name, isDark);
			}
		}
		else
		{
			logoBox.Child = CreateBrandTypography(vendor.Name, isDark);
		}

		sp.Children.Add(logoBox);

		// Brand Name
		sp.Children.Add(new TextBlock
		{
			Text = vendor.Name,
			FontSize = 13,
			FontWeight = FontWeights.SemiBold,
			Foreground = DspTextPrimaryBrush,
			TextAlignment = TextAlignment.Center,
			TextTrimming = TextTrimming.CharacterEllipsis,
			MaxWidth = 120
		});

		// Count stats: e.g. "278 款 / 763 个预设"
		sp.Children.Add(new TextBlock
		{
			Text = $"{vendor.ProductCount} 款 / {vendor.EqCount} 个预设",
			FontSize = 11,
			Foreground = DspTextMutedBrush,
			TextAlignment = TextAlignment.Center,
			MaxWidth = 120
		});

		card.Child = sp;

		// Hover and Click (evaluate IsDspDark dynamically so cached cards adapt after theme switches)
		card.PointerEntered += (s, e) =>
		{
			bool darkNow = IsDspDark;
			card.BorderBrush = new SolidColorBrush(Color.FromArgb(255, 129, 140, 248));
			card.Background = darkNow ? new SolidColorBrush(Color.FromArgb(190, 51, 65, 85)) : new SolidColorBrush(Color.FromArgb(255, 248, 250, 252));
		};
		card.PointerExited += (s, e) =>
		{
			bool darkNow = IsDspDark;
			card.BorderBrush = darkNow ? new SolidColorBrush(Color.FromArgb(85, 255, 255, 255)) : new SolidColorBrush(Color.FromArgb(255, 226, 232, 240));
			card.Background = darkNow ? new SolidColorBrush(Color.FromArgb(160, 30, 41, 59)) : new SolidColorBrush(Color.FromArgb(245, 255, 255, 255));
		};
		card.Tapped += (s, e) => ShowVendorModelsView(vendor);

		return card;
	}

	private static FrameworkElement CreateBrandTypography(string brandName, bool isDark = false)
	{
		return new Border
		{
			Padding = new Thickness(4, 2, 4, 2),
			Child = new TextBlock
			{
				Text = brandName,
				FontSize = brandName.Length > 10 ? 11 : (brandName.Length > 7 ? 12 : 14),
				FontWeight = FontWeights.Bold,
				Foreground = isDark ? new SolidColorBrush(Color.FromArgb(255, 226, 232, 240)) : new SolidColorBrush(Color.FromArgb(255, 71, 85, 105)),
				HorizontalAlignment = HorizontalAlignment.Center,
				VerticalAlignment = VerticalAlignment.Center,
				TextAlignment = TextAlignment.Center,
				TextTrimming = TextTrimming.CharacterEllipsis
			}
		};
	}

	private void ShowVendorModelsView(OpraVendor vendor)
	{
		if (_hcContentHost == null) return;
		_currentVendor = vendor;
		_hcVendorCards.Clear();
		_hcModelRows.Clear();
		UpdateBreadcrumbs();

		_hcContentHost.Children.Clear();

		var splitGrid = new Grid();
		splitGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.0, GridUnitType.Star) }); // Left Models List
		splitGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(380) }); // Right Detail & Curve Panel

		// --- Left Column: Models List ---
		var leftContainer = new Grid { RowSpacing = 8, Margin = new Thickness(0, 0, 16, 0) };
		leftContainer.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Header with Back button
		leftContainer.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Column Titles
		leftContainer.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1.0, GridUnitType.Star) }); // Models List

		// Header
		var headerSp = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 10 };
		var backBtn = new Button
		{
			Content = new StackPanel
			{
				Orientation = Orientation.Horizontal,
				Spacing = 6,
				Children =
				{
					new FontIcon { Glyph = "\uE72B", FontSize = 12, Foreground = DspTextPrimaryBrush },
					new TextBlock { Text = "所有生产商", FontSize = 12, FontWeight = FontWeights.Medium, Foreground = DspTextPrimaryBrush }
				}
			},
			Padding = new Thickness(8, 4, 10, 4),
			CornerRadius = new CornerRadius(14),
			Background = DspSoftSurfaceBrush,
			BorderBrush = CreateLiquidGlassRimBrush(),
			BorderThickness = new Thickness(1),
			Foreground = DspTextPrimaryBrush,
			VerticalAlignment = VerticalAlignment.Center
		};
		_hcBackBtn = backBtn;
		backBtn.Click += (s, e) => ShowAllVendorsView();
		headerSp.Children.Add(backBtn);

		headerSp.Children.Add(new TextBlock
		{
			Text = vendor.Name,
			FontSize = 16,
			FontWeight = FontWeights.Bold,
			Foreground = DspTextPrimaryBrush,
			VerticalAlignment = VerticalAlignment.Center
		});
		headerSp.Children.Add(new TextBlock
		{
			Text = $"{vendor.ProductCount} 款耳机 · {vendor.EqCount} 个校正预设",
			FontSize = 11,
			Foreground = DspTextMutedBrush,
			VerticalAlignment = VerticalAlignment.Center
		});
		Grid.SetRow(headerSp, 0);
		leftContainer.Children.Add(headerSp);

		// Column headers
		var colHeaders = new Grid { Margin = new Thickness(12, 4, 12, 0) };
		colHeaders.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.0, GridUnitType.Star) });
		colHeaders.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(90) });
		colHeaders.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(60) });

		colHeaders.Children.Add(new TextBlock { Text = "型号", FontSize = 11, FontWeight = FontWeights.SemiBold, Foreground = DspTextMutedBrush });
		var th2 = new TextBlock { Text = "可用曲线", FontSize = 11, FontWeight = FontWeights.SemiBold, Foreground = DspTextMutedBrush, TextAlignment = TextAlignment.Center };
		Grid.SetColumn(th2, 1);
		colHeaders.Children.Add(th2);

		Grid.SetRow(colHeaders, 1);
		leftContainer.Children.Add(colHeaders);

		// Models ListView
		var models = OpraDatabaseService.GetProductsByVendor(vendor.Id, _activeSource);
		var modelsSv = new ScrollViewer
		{
			VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
			HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
			Padding = new Thickness(0, 0, 6, 12)
		};
		var modelsSp = new StackPanel { Spacing = 4 };

		foreach (var prod in models)
		{
			modelsSp.Children.Add(CreateModelRow(prod));
		}

		modelsSv.Content = modelsSp;
		Grid.SetRow(modelsSv, 2);
		leftContainer.Children.Add(modelsSv);

		Grid.SetColumn(leftContainer, 0);
		splitGrid.Children.Add(leftContainer);

		// --- Right Column: Detail & Curve Panel ---
		var rightDetailContainer = CreateDetailAndCurvePanel();
		Grid.SetColumn(rightDetailContainer, 1);
		splitGrid.Children.Add(rightDetailContainer);

		_hcContentHost.Children.Add(splitGrid);

		// If a model was already selected, preview it
		if (_currentProduct != null && _currentProduct.VendorId.Equals(vendor.Id, StringComparison.OrdinalIgnoreCase))
		{
			SelectModel(_currentProduct);
		}
		else if (models.Count > 0)
		{
			SelectModel(models[0]);
		}

		ApplyThemeToHcContentHost(IsDspDark, splitGrid);
	}

	private FrameworkElement CreateModelRow(OpraProduct prod)
	{
		bool isDark = IsDspDark;
		var row = new Border
		{
			Background = CreateAcrylicCardBrush(0.78, 220),
			BorderBrush = CreateLiquidGlassRimBrush(),
			BorderThickness = new Thickness(1),
			CornerRadius = new CornerRadius(8),
			Padding = new Thickness(12, 8, 12, 8)
		};
		LiquidGlassEngine.AttachInteractiveLiquidGlass(row, LiquidGlassVariant.Regular, isDark: isDark, enablePointerTracking: false);
		_hcModelRows.Add(row);

		var rowGrid = new Grid();
		rowGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.0, GridUnitType.Star) });
		rowGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(90) });
		rowGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(60) });

		// Col 0: Headphone Art + Name + Subtype
		var nameSp = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 10, VerticalAlignment = VerticalAlignment.Center };
		var artIcon = new Border
		{
			Width = 32,
			Height = 32,
			CornerRadius = new CornerRadius(6),
			Background = IsDspDark ? new SolidColorBrush(Color.FromArgb(40, 255, 255, 255)) : new SolidColorBrush(Color.FromArgb(255, 241, 245, 249)),
			Child = new FontIcon { Glyph = "\uE7F5", FontSize = 15, Foreground = DspTextMutedBrush }
		};
		nameSp.Children.Add(artIcon);

		var labelSp = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
		labelSp.Children.Add(new TextBlock
		{
			Text = prod.Name,
			FontSize = 13,
			FontWeight = FontWeights.SemiBold,
			Foreground = DspTextPrimaryBrush,
			TextTrimming = TextTrimming.CharacterEllipsis
		});
		if (!string.IsNullOrEmpty(prod.Subtype))
		{
			string subtypeCn = prod.Subtype switch
			{
				"in_ear" => "入耳式",
				"over_the_ear" or "over_ear" => "头戴包耳式",
				"on_ear" => "贴耳式",
				_ => prod.Subtype
			};
			labelSp.Children.Add(new TextBlock
			{
				Text = subtypeCn,
				FontSize = 10,
				Foreground = DspTextMutedBrush
			});
		}
		nameSp.Children.Add(labelSp);
		Grid.SetColumn(nameSp, 0);
		rowGrid.Children.Add(nameSp);

		// Col 1: Preset Count badge
		var countBadge = new Border
		{
			Background = IsDspDark ? new SolidColorBrush(Color.FromArgb(40, 255, 255, 255)) : new SolidColorBrush(Color.FromArgb(255, 241, 245, 249)),
			CornerRadius = new CornerRadius(10),
			Padding = new Thickness(8, 2, 8, 2),
			HorizontalAlignment = HorizontalAlignment.Center,
			VerticalAlignment = VerticalAlignment.Center,
			Child = new TextBlock
			{
				Text = $"{prod.PresetCount} 个预设",
				FontSize = 10,
				Foreground = DspTextSubHeaderBrush
			}
		};
		Grid.SetColumn(countBadge, 1);
		rowGrid.Children.Add(countBadge);

		// Col 2: Actions (Star & Arrow)
		var actionsSp = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 6, HorizontalAlignment = HorizontalAlignment.Right, VerticalAlignment = VerticalAlignment.Center };
		bool isFav = _favorites.Contains(prod.Id);
		var starBtn = new Button
		{
			Padding = new Thickness(4),
			Background = new SolidColorBrush(Colors.Transparent),
			BorderThickness = new Thickness(0),
			Content = new FontIcon
			{
				Glyph = isFav ? "\uE735" : "\uE734",
				FontSize = 13,
				Foreground = isFav ? new SolidColorBrush(Color.FromArgb(255, 234, 179, 8)) : DspTextMutedBrush
			}
		};
		starBtn.Click += (s, e) =>
		{
			ToggleFavorite(prod.Id);
			bool nowFav = _favorites.Contains(prod.Id);
			if (starBtn.Content is FontIcon fi)
			{
				fi.Glyph = nowFav ? "\uE735" : "\uE734";
				fi.Foreground = nowFav ? new SolidColorBrush(Color.FromArgb(255, 234, 179, 8)) : DspTextMutedBrush;
			}
		};
		actionsSp.Children.Add(starBtn);
		actionsSp.Children.Add(new FontIcon { Glyph = "\uE76C", FontSize = 11, Foreground = DspTextMutedBrush });

		Grid.SetColumn(actionsSp, 2);
		rowGrid.Children.Add(actionsSp);

		row.Child = rowGrid;

		// Click to select
		row.Tapped += (s, e) => SelectModel(prod);

		return row;
	}

	private FrameworkElement CreateDetailAndCurvePanel()
	{
		bool isDark = IsDspDark;
		var panelBorder = new Border
		{
			Background = CreateAcrylicCardBrush(0.88, 235),
			BorderBrush = CreateLiquidGlassRimBrush(),
			BorderThickness = new Thickness(1),
			CornerRadius = new CornerRadius(12),
			Padding = new Thickness(16, 14, 16, 14)
		};
		LiquidGlassEngine.AttachInteractiveLiquidGlass(panelBorder, LiquidGlassVariant.Regular, isDark: isDark, enablePointerTracking: false);
		_hcDetailPanelBorder = panelBorder;

		var mainGrid = new Grid();

		// Empty State
		_emptyDetailPanel = new Grid { VerticalAlignment = VerticalAlignment.Center, HorizontalAlignment = HorizontalAlignment.Center };
		var emptySp = new StackPanel { Spacing = 10, HorizontalAlignment = HorizontalAlignment.Center };
		emptySp.Children.Add(new FontIcon { Glyph = "\uE7F5", FontSize = 36, Foreground = DspTextMutedBrush, HorizontalAlignment = HorizontalAlignment.Center });
		emptySp.Children.Add(new TextBlock { Text = "未选择预设", FontSize = 14, FontWeight = FontWeights.SemiBold, Foreground = DspTextPrimaryBrush, TextAlignment = TextAlignment.Center });
		emptySp.Children.Add(new TextBlock { Text = "从左侧选择耳机型号，查看并应用校正曲线。", FontSize = 12, Foreground = DspTextMutedBrush, TextAlignment = TextAlignment.Center });
		_emptyDetailPanel.Children.Add(emptySp);
		mainGrid.Children.Add(_emptyDetailPanel);

		// Active Detail Panel
		_detailPanel = new Grid { Visibility = Visibility.Collapsed };
		var sv = new ScrollViewer
		{
			VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
			HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled
		};
		var detailSp = new StackPanel { Spacing = 12 };

		// 1. Header (Brand, Model, Apply Button, Fav Button)
		var headGrid = new Grid();
		headGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.0, GridUnitType.Star) });
		headGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

		var headTitleSp = new StackPanel { Spacing = 2 };
		_detailVendorTb = new TextBlock { Text = "Sennheiser", FontSize = 11, Foreground = DspTextMutedBrush };
		_detailProductTb = new TextBlock { Text = "HD 650", FontSize = 16, FontWeight = FontWeights.Bold, Foreground = DspTextPrimaryBrush };
		headTitleSp.Children.Add(_detailVendorTb);
		headTitleSp.Children.Add(_detailProductTb);
		Grid.SetColumn(headTitleSp, 0);
		headGrid.Children.Add(headTitleSp);

		var headActionsSp = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8, VerticalAlignment = VerticalAlignment.Center };
		_btnApplyPreset = new Button
		{
			Content = "应用",
			Padding = new Thickness(14, 5, 14, 5),
			CornerRadius = new CornerRadius(14),
			Background = new SolidColorBrush(Color.FromArgb(255, 99, 102, 241)),
			Foreground = new SolidColorBrush(Colors.White),
			FontWeight = FontWeights.SemiBold,
			FontSize = 12
		};
		_btnApplyPreset.Click += (s, e) => ApplyCurrentPreset();
		headActionsSp.Children.Add(_btnApplyPreset);

		_btnFavPreset = new Button
		{
			Padding = new Thickness(6),
			CornerRadius = new CornerRadius(14),
			Background = DspSoftSurfaceBrush,
			BorderBrush = CreateLiquidGlassRimBrush(),
			BorderThickness = new Thickness(1),
			Content = new FontIcon { Glyph = "\uE734", FontSize = 13, Foreground = DspTextMutedBrush }
		};
		_btnFavPreset.Click += (s, e) =>
		{
			if (_currentProduct != null)
			{
				ToggleFavorite(_currentProduct.Id);
				bool nowFav = _favorites.Contains(_currentProduct.Id);
				if (_btnFavPreset.Content is FontIcon fi)
				{
					fi.Glyph = nowFav ? "\uE735" : "\uE734";
					fi.Foreground = nowFav ? new SolidColorBrush(Color.FromArgb(255, 234, 179, 8)) : DspTextMutedBrush;
				}
			}
		};
		headActionsSp.Children.Add(_btnFavPreset);

		Grid.SetColumn(headActionsSp, 1);
		headGrid.Children.Add(headActionsSp);
		detailSp.Children.Add(headGrid);

		// 2. Frequency Response Curve Preview
		var curveSec = new StackPanel { Spacing = 6 };
		var curveHeadGrid = new Grid();
		curveHeadGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.0, GridUnitType.Star) });
		curveHeadGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

		curveHeadGrid.Children.Add(new TextBlock { Text = "EQ 频率响应", FontSize = 12, FontWeight = FontWeights.SemiBold, Foreground = DspTextSubHeaderBrush });
		_curveAuthorTb = new TextBlock { Text = "Oratory1990", FontSize = 11, Foreground = DspTextMutedBrush };
		Grid.SetColumn(_curveAuthorTb, 1);
		curveHeadGrid.Children.Add(_curveAuthorTb);
		curveSec.Children.Add(curveHeadGrid);

		var curveBorder = new Border
		{
			Height = 170,
			CornerRadius = new CornerRadius(8),
			Background = isDark ? new SolidColorBrush(Color.FromArgb(140, 15, 23, 42)) : new SolidColorBrush(Color.FromArgb(255, 248, 250, 252)),
			BorderBrush = CreateLiquidGlassRimBrush(),
			BorderThickness = new Thickness(1)
		};
		_hcCurveBorder = curveBorder;
		_curveControl = new HeadphoneCurveControl();
		_curveControl.SetDarkTheme(isDark);
		curveBorder.Child = _curveControl;
		curveSec.Children.Add(curveBorder);
		detailSp.Children.Add(curveSec);

		// 3. Correction Source Selector
		var srcSec = new StackPanel { Spacing = 6 };
		srcSec.Children.Add(new TextBlock { Text = "校正来源", FontSize = 12, FontWeight = FontWeights.SemiBold, Foreground = DspTextSubHeaderBrush });
		_sourcesStack = new StackPanel { Spacing = 4 };
		srcSec.Children.Add(_sourcesStack);
		detailSp.Children.Add(srcSec);

		// 4. Correction Mode Selector
		var modeSec = new StackPanel { Spacing = 6 };
		var modeSwitchSp = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 6 };
		_modeOriginalBtn = new Button
		{
			Content = "作者原始曲线",
			FontSize = 11,
			FontWeight = FontWeights.Medium,
			Padding = new Thickness(10, 4, 10, 4),
			CornerRadius = new CornerRadius(6),
			BorderThickness = new Thickness(1)
		};
		_modeOriginalBtn.Click += (s, e) => SetCalibrationMode("original");

		_modeListeningBtn = new Button
		{
			Content = "听感调整",
			FontSize = 11,
			FontWeight = FontWeights.Medium,
			Padding = new Thickness(10, 4, 10, 4),
			CornerRadius = new CornerRadius(6),
			BorderThickness = new Thickness(1)
		};
		_modeListeningBtn.Click += (s, e) => SetCalibrationMode("listening");

		modeSwitchSp.Children.Add(_modeOriginalBtn);
		modeSwitchSp.Children.Add(_modeListeningBtn);
		modeSec.Children.Add(modeSwitchSp);

		_modeDescTb = new TextBlock
		{
			Text = "保留作者频段与前级。校正独立运行，可与个人 EQ 叠加。",
			FontSize = 10,
			Foreground = DspTextMutedBrush,
			TextWrapping = TextWrapping.Wrap
		};
		modeSec.Children.Add(_modeDescTb);
		UpdateCalibrationModeButtonStyles(isDark);
		detailSp.Children.Add(modeSec);

		// 5. Preamp Control
		var preampSec = new StackPanel { Spacing = 4 };
		var preampHead = new Grid();
		preampHead.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.0, GridUnitType.Star) });
		preampHead.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
		preampHead.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

		preampHead.Children.Add(new TextBlock { Text = "前级", FontSize = 12, FontWeight = FontWeights.SemiBold, Foreground = DspTextSubHeaderBrush });

		_hpPreampValueTb = new TextBlock
		{
			Text = "-6.4 dB",
			FontSize = 12,
			FontWeight = FontWeights.SemiBold,
			Foreground = DspAccentSoftTextBrush,
			Margin = new Thickness(0, 0, 8, 0)
		};
		Grid.SetColumn(_hpPreampValueTb, 1);
		preampHead.Children.Add(_hpPreampValueTb);

		var restoreBtn = new Button
		{
			Content = "恢复默认",
			FontSize = 10,
			Padding = new Thickness(8, 3, 8, 3),
			CornerRadius = new CornerRadius(4),
			Background = DspSoftSurfaceBrush,
			BorderBrush = CreateLiquidGlassRimBrush(),
			BorderThickness = new Thickness(1),
			Foreground = DspTextSecondaryBrush
		};
		_hcRestorePreampBtn = restoreBtn;
		restoreBtn.Click += (s, e) =>
		{
			if (_currentPreset != null)
			{
				_userPreampDb = _currentPreset.PreampDb;
				if (_hpPreampSlider != null) _hpPreampSlider.Value = _userPreampDb.Value;
				UpdatePreampDisplay();
			}
		};
		Grid.SetColumn(restoreBtn, 2);
		preampHead.Children.Add(restoreBtn);
		preampSec.Children.Add(preampHead);

		_hpPreampSlider = new Slider
		{
			Minimum = -15.0,
			Maximum = 6.0,
			StepFrequency = 0.1,
			Value = -6.4,
			Margin = new Thickness(0, 2, 0, 0)
		};
		_hpPreampSlider.ValueChanged += (s, e) =>
		{
			_userPreampDb = Math.Round(_hpPreampSlider.Value, 1);
			UpdatePreampDisplay();
		};
		preampSec.Children.Add(_hpPreampSlider);
		detailSp.Children.Add(preampSec);

		// 6. Metrics & Warning
		var metricsGrid = new Grid { Margin = new Thickness(0, 4, 0, 0) };
		metricsGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.0, GridUnitType.Star) });
		metricsGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.0, GridUnitType.Star) });

		var m1Sp = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 6 };
		m1Sp.Children.Add(new TextBlock { Text = "OPRA 滤波器:", FontSize = 11, Foreground = DspTextMutedBrush });
		_metricsFiltersTb = new TextBlock { Text = "6/6", FontSize = 11, FontWeight = FontWeights.SemiBold, Foreground = DspTextPrimaryBrush };
		m1Sp.Children.Add(_metricsFiltersTb);
		Grid.SetColumn(m1Sp, 0);
		metricsGrid.Children.Add(m1Sp);

		var m2Sp = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 6, HorizontalAlignment = HorizontalAlignment.Right };
		m2Sp.Children.Add(new TextBlock { Text = "调整:", FontSize = 11, Foreground = DspTextMutedBrush });
		_metricsAdjustedTb = new TextBlock { Text = "0", FontSize = 11, FontWeight = FontWeights.SemiBold, Foreground = DspTextPrimaryBrush };
		m2Sp.Children.Add(_metricsAdjustedTb);
		Grid.SetColumn(m2Sp, 1);
		metricsGrid.Children.Add(m2Sp);

		detailSp.Children.Add(metricsGrid);

		// Bit-perfect warning
		detailSp.Children.Add(new TextBlock
		{
			Text = "开启后将使用 EQ，Bit-perfect 会暂停",
			FontSize = 10,
			Foreground = new SolidColorBrush(Color.FromArgb(255, 156, 163, 175)),
			Margin = new Thickness(0, 4, 0, 0)
		});

		sv.Content = detailSp;
		_detailPanel.Children.Add(sv);
		mainGrid.Children.Add(_detailPanel);

		panelBorder.Child = mainGrid;
		return panelBorder;
	}

	private void SelectModel(OpraProduct prod)
	{
		_currentProduct = prod;
		UpdateBreadcrumbs();

		if (_emptyDetailPanel != null) _emptyDetailPanel.Visibility = Visibility.Collapsed;
		if (_detailPanel != null) _detailPanel.Visibility = Visibility.Visible;

		if (_detailVendorTb != null) _detailVendorTb.Text = prod.VendorName;
		if (_detailProductTb != null) _detailProductTb.Text = prod.Name;

		// Update Fav button
		bool isFav = _favorites.Contains(prod.Id);
		if (_btnFavPreset != null && _btnFavPreset.Content is FontIcon fi)
		{
			fi.Glyph = isFav ? "\uE735" : "\uE734";
			fi.Foreground = isFav ? new SolidColorBrush(Color.FromArgb(255, 234, 179, 8)) : DspTextMutedBrush;
		}

		// Populate source buttons
		if (_sourcesStack != null)
		{
			_sourcesStack.Children.Clear();
			bool hasMatchingPreset = _currentPreset != null && prod.Eqs.Any(x => x.Id == _currentPreset.Id);
			for (int i = 0; i < prod.Eqs.Count; i++)
			{
				var eq = prod.Eqs[i];
				bool isSelected = hasMatchingPreset ? (_currentPreset!.Id == eq.Id) : (i == 0);
				if (isSelected) _currentPreset = eq;

				var srcBtn = CreateSourcePresetRow(eq, isSelected);
				_sourcesStack.Children.Add(srcBtn);
			}
		}

		if (_currentPreset != null)
		{
			SelectPreset(_currentPreset);
		}
	}

	private FrameworkElement CreateSourcePresetRow(OpraHeadphonePreset eq, bool isSelected)
	{
		bool isDark = IsDspDark;
		var btn = new Button
		{
			HorizontalAlignment = HorizontalAlignment.Stretch,
			HorizontalContentAlignment = HorizontalAlignment.Stretch,
			Padding = new Thickness(10, 6, 10, 6),
			CornerRadius = new CornerRadius(6),
			BorderThickness = new Thickness(1),
			Tag = eq
		};

		var g = new Grid();
		g.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.0, GridUnitType.Star) });
		g.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

		var sp = new StackPanel { Spacing = 2 };
		sp.Children.Add(new TextBlock
		{
			Text = eq.MeasurementSource,
			FontSize = 12,
			FontWeight = FontWeights.SemiBold
		});
		sp.Children.Add(new TextBlock
		{
			Text = $"{eq.TargetCurve} · {eq.Filters.Count} 个 OPRA 滤波器",
			FontSize = 10
		});
		Grid.SetColumn(sp, 0);
		g.Children.Add(sp);

		var check = new FontIcon
		{
			Glyph = "\uE73E",
			FontSize = 11,
			VerticalAlignment = VerticalAlignment.Center,
			Visibility = isSelected ? Visibility.Visible : Visibility.Collapsed
		};
		Grid.SetColumn(check, 1);
		g.Children.Add(check);

		btn.Content = g;
		ApplySourcePresetRowStyle(btn, isSelected, isDark);
		btn.Click += (s, e) => SelectPreset(eq);
		return btn;
	}

	private static void ApplySourcePresetRowStyle(Button btn, bool isSelected, bool isDark)
	{
		btn.Background = isSelected
			? new SolidColorBrush(isDark ? Color.FromArgb(85, 99, 102, 241) : Color.FromArgb(255, 238, 242, 255))
			: (isDark
				? LiquidGlassEngine.CreateSurfaceBrush(LiquidGlassVariant.Clear, isDark: true, opacityScale: 0.45)
				: new SolidColorBrush(Color.FromArgb(215, 241, 245, 249)));
		btn.BorderBrush = isSelected
			? new SolidColorBrush(isDark ? Color.FromArgb(180, 129, 140, 248) : Color.FromArgb(255, 129, 140, 248))
			: LiquidGlassEngine.CreateSpecularRimBrush(LiquidGlassVariant.Regular, isDark: isDark);

		var titleFg = isSelected
			? new SolidColorBrush(isDark ? Color.FromArgb(255, 199, 210, 254) : Color.FromArgb(255, 79, 70, 229))
			: new SolidColorBrush(isDark ? Colors.White : Color.FromArgb(255, 15, 23, 42));
		var subFg = new SolidColorBrush(isDark ? Color.FromArgb(255, 203, 213, 225) : Color.FromArgb(255, 100, 116, 139));
		var checkFg = new SolidColorBrush(isDark ? Color.FromArgb(255, 199, 210, 254) : Color.FromArgb(255, 79, 70, 229));

		if (btn.Content is Grid g)
		{
			if (g.Children.Count > 0 && g.Children[0] is StackPanel sp)
			{
				if (sp.Children.Count > 0 && sp.Children[0] is TextBlock t1) t1.Foreground = titleFg;
				if (sp.Children.Count > 1 && sp.Children[1] is TextBlock t2) t2.Foreground = subFg;
			}
			if (g.Children.Count > 1 && g.Children[1] is FontIcon check)
			{
				check.Visibility = isSelected ? Visibility.Visible : Visibility.Collapsed;
				check.Foreground = checkFg;
			}
		}
	}

	private void UpdateSourcePresetRowsStyles(bool isDark)
	{
		if (_sourcesStack == null) return;
		foreach (var child in _sourcesStack.Children)
		{
			if (child is Button btn && btn.Tag is OpraHeadphonePreset eq)
			{
				bool isSelected = _currentPreset != null && _currentPreset.Id == eq.Id;
				ApplySourcePresetRowStyle(btn, isSelected, isDark);
			}
		}
	}

	private void SelectPreset(OpraHeadphonePreset preset)
	{
		_currentPreset = preset;
		_userPreampDb = preset.PreampDb;

		if (_curveAuthorTb != null) _curveAuthorTb.Text = preset.MeasurementSource;
		if (_hpPreampSlider != null) _hpPreampSlider.Value = preset.PreampDb;
		if (_metricsFiltersTb != null) _metricsFiltersTb.Text = $"{preset.Filters.Count}/{preset.Filters.Count}";
		if (_metricsAdjustedTb != null) _metricsAdjustedTb.Text = "0";

		UpdateSourcePresetRowsStyles(IsDspDark);
		UpdateApplyButtonState();
		UpdatePreampDisplay();
		UpdateCurveView();
	}

	private void UpdateApplyButtonState()
	{
		if (_btnApplyPreset == null) return;

		bool isCurrentlyApplied = _currentPreset != null && _appliedPresetId == _currentPreset.Id;
		if (isCurrentlyApplied)
		{
			if (_isCalibrationEnabled)
			{
				_btnApplyPreset.Content = "已应用 (点击关闭)";
				_btnApplyPreset.Background = new SolidColorBrush(Color.FromArgb(255, 22, 163, 74));
			}
			else
			{
				_btnApplyPreset.Content = "开启校正";
				_btnApplyPreset.Background = new SolidColorBrush(Color.FromArgb(255, 99, 102, 241));
			}
		}
		else
		{
			_btnApplyPreset.Content = "应用";
			_btnApplyPreset.Background = new SolidColorBrush(Color.FromArgb(255, 99, 102, 241));
		}
	}

	private void ApplyCurrentPreset()
	{
		if (_currentPreset == null) return;

		bool isSame = _appliedPresetId == _currentPreset.Id;
		if (isSame && _isCalibrationEnabled)
		{
			ToggleCalibration(false);
			return;
		}

		_appliedPresetId = _currentPreset.Id;
		_isCalibrationEnabled = true;

		// Apply to audio engine
		var eqProcessor = ViewModel?.AudioEngine?.DspChain?.Eq;
		if (eqProcessor != null)
		{
			OpraDatabaseService.ApplyPreset(eqProcessor, _currentPreset, _userPreampDb);
			eqProcessor.CalibrationEnabled = true;
		}

		// Update Master Switch
		if (_hcMasterSwitch != null) _hcMasterSwitch.IsOn = true;

		// Add to recents
		if (_currentProduct != null)
		{
			_recents.Remove(_currentProduct.Id);
			_recents.Insert(0, _currentProduct.Id);
			if (_recents.Count > 20) _recents.RemoveAt(_recents.Count - 1);
		}

		UpdateBannerWithPreset(_currentPreset);
		UpdateApplyButtonState();
		UpdateFavoritesAndRecentsBadges();
		SaveHeadphoneState();

		// Update sidebar
		if (_headroomNavSubTb != null)
		{
			// Optional sync
		}
		if (_headphoneNavSubTb != null)
		{
			_headphoneNavSubTb.Text = $"{_currentPreset.Brand} {_currentPreset.Model}";
		}
	}

	private void ToggleCalibration(bool enabled)
	{
		_isCalibrationEnabled = enabled;

		var eqProcessor = ViewModel?.AudioEngine?.DspChain?.Eq;
		if (eqProcessor != null)
		{
			eqProcessor.CalibrationEnabled = enabled;
		}

		if (_hcMasterSwitch != null && _hcMasterSwitch.IsOn != enabled)
		{
			_hcMasterSwitch.IsOn = enabled;
		}

		UpdateApplyButtonState();

		if (_currentPreset != null && _appliedPresetId == _currentPreset.Id)
		{
			UpdateBannerWithPreset(_currentPreset);
		}

		if (_headphoneNavSubTb != null)
		{
			_headphoneNavSubTb.Text = enabled && _currentPreset != null
				? $"{_currentPreset.Brand} {_currentPreset.Model}"
				: "已旁路";
		}

		SaveHeadphoneState();
	}

	private void UpdateCalibrationModeButtonStyles(bool? isDarkOverride = null)
	{
		bool isDark = isDarkOverride ?? IsDspDark;
		bool isOrig = _calibrationMode == "original";
		Brush activeBg = new SolidColorBrush(Color.FromArgb(255, 99, 102, 241));
		Brush activeBorder = new SolidColorBrush(Color.FromArgb(255, 129, 140, 248));
		Brush activeFg = new SolidColorBrush(Colors.White);
		Brush inactiveBg = isDark
			? LiquidGlassEngine.CreateSurfaceBrush(LiquidGlassVariant.Clear, isDark: true, opacityScale: 0.45)
			: new SolidColorBrush(Color.FromArgb(255, 241, 245, 249));
		Brush inactiveBorder = LiquidGlassEngine.CreateSpecularRimBrush(LiquidGlassVariant.Regular, isDark: isDark);
		Brush inactiveFg = new SolidColorBrush(isDark ? Color.FromArgb(255, 226, 232, 240) : Color.FromArgb(255, 71, 85, 105));

		if (_modeOriginalBtn != null)
		{
			_modeOriginalBtn.Background = isOrig ? activeBg : inactiveBg;
			_modeOriginalBtn.BorderBrush = isOrig ? activeBorder : inactiveBorder;
			_modeOriginalBtn.Foreground = isOrig ? activeFg : inactiveFg;
		}
		if (_modeListeningBtn != null)
		{
			_modeListeningBtn.Background = !isOrig ? activeBg : inactiveBg;
			_modeListeningBtn.BorderBrush = !isOrig ? activeBorder : inactiveBorder;
			_modeListeningBtn.Foreground = !isOrig ? activeFg : inactiveFg;
		}
		if (_modeDescTb != null)
		{
			_modeDescTb.Text = isOrig
				? "保留作者频段与前级。校正独立运行，可与个人 EQ 叠加。"
				: "可选调整：减轻宽中频下切、收回部分提升并重算前级，偏离作者曲线。";
		}
	}

	private void SetCalibrationMode(string mode)
	{
		_calibrationMode = mode;
		UpdateCalibrationModeButtonStyles();

		UpdateCurveView();

		// Re-apply if active
		if (_isCalibrationEnabled && _currentPreset != null && _appliedPresetId == _currentPreset.Id)
		{
			var eqProcessor = ViewModel?.AudioEngine?.DspChain?.Eq;
			if (eqProcessor != null)
			{
				OpraDatabaseService.ApplyPreset(eqProcessor, _currentPreset, _userPreampDb);
			}
		}
	}

	private void UpdatePreampDisplay()
	{
		double p = _userPreampDb ?? (_currentPreset?.PreampDb ?? 0.0);
		if (_hpPreampValueTb != null)
		{
			_hpPreampValueTb.Text = $"{p:+0.0;-0.0;0.0} dB";
		}
		UpdateCurveView();

		if (_isCalibrationEnabled && _currentPreset != null && _appliedPresetId == _currentPreset.Id)
		{
			var eqProcessor = ViewModel?.AudioEngine?.DspChain?.Eq;
			if (eqProcessor != null)
			{
				eqProcessor.CalibrationPreampDb = p;
			}
		}
	}

	private void UpdateCurveView()
	{
		if (_curveControl != null)
		{
			_curveControl.SetCurve(_currentPreset, _calibrationMode, _userPreampDb);
		}
	}

	private void UpdateBannerWithPreset(OpraHeadphonePreset preset)
	{
		if (_hcBannerTitleTb != null)
		{
			_hcBannerTitleTb.Text = $"{preset.Brand} {preset.Model} ({preset.MeasurementSource})";
		}
		if (_hcBannerSubtitleTb != null)
		{
			double p = _userPreampDb ?? preset.PreampDb;
			_hcBannerSubtitleTb.Text = $"{preset.TargetCurve} · {preset.Filters.Count} 个滤波器 · 前级 {p:+0.0;-0.0;0.0} dB";
		}
		if (_hcBannerStatsTb != null)
		{
			_hcBannerStatsTb.Text = _isCalibrationEnabled ? "校正已启用并在音频管道中生效" : "校正已就绪 (已旁路)";
		}
	}

	private void ToggleFavorite(string productId)
	{
		if (_favorites.Contains(productId))
		{
			_favorites.Remove(productId);
		}
		else
		{
			_favorites.Add(productId);
		}

		UpdateFavoritesAndRecentsBadges();
		SaveHeadphoneState();
	}

	private void SwitchTab(string tab)
	{
		_activeTab = tab;
		UpdateTabButtonStyles();

		if (_hcContentHost == null) return;
		_hcContentHost.Children.Clear();

		if (tab == "catalog")
		{
			ShowAllVendorsView();
		}
		else if (tab == "favorites")
		{
			ShowFavoritesListView();
		}
		else if (tab == "recent")
		{
			ShowRecentListView();
		}
	}

	private void ShowFavoritesListView()
	{
		if (_hcContentHost == null) return;
		_hcVendorCards.Clear();
		_hcModelRows.Clear();
		_hcBackBtn = null;
		_hcDetailPanelBorder = null;
		_hcCurveBorder = null;
		_hcRestorePreampBtn = null;

		var container = new Grid { RowSpacing = 10 };
		container.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
		container.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1.0, GridUnitType.Star) });

		var headSp = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8, Margin = new Thickness(4, 2, 0, 4) };
		headSp.Children.Add(new FontIcon { Glyph = "\uE734", FontSize = 14, Foreground = new SolidColorBrush(Color.FromArgb(255, 234, 179, 8)) });
		headSp.Children.Add(new TextBlock { Text = "收藏型号", FontSize = 14, FontWeight = FontWeights.Bold, Foreground = DspTextPrimaryBrush });
		headSp.Children.Add(new TextBlock { Text = $"({_favorites.Count} 款)", FontSize = 12, Foreground = DspTextMutedBrush });
		Grid.SetRow(headSp, 0);
		container.Children.Add(headSp);

		var sv = new ScrollViewer { Padding = new Thickness(0, 0, 8, 16) };
		var sp = new StackPanel { Spacing = 6 };

		foreach (var fid in _favorites)
		{
			var prod = OpraDatabaseService.GetProductById(fid);
			if (prod != null)
			{
				sp.Children.Add(CreateModelRow(prod));
			}
		}

		if (_favorites.Count == 0)
		{
			sp.Children.Add(new TextBlock
			{
				Text = "还没有收藏型号。点击耳机型号旁的星星图标即可收藏。",
				FontSize = 13,
				Foreground = DspTextMutedBrush,
				Margin = new Thickness(8, 24, 0, 0)
			});
		}

		sv.Content = sp;
		Grid.SetRow(sv, 1);
		container.Children.Add(sv);

		_hcContentHost.Children.Add(container);
		ApplyThemeToHcContentHost(IsDspDark, container);
	}

	private void ShowRecentListView()
	{
		if (_hcContentHost == null) return;
		_hcVendorCards.Clear();
		_hcModelRows.Clear();
		_hcBackBtn = null;
		_hcDetailPanelBorder = null;
		_hcCurveBorder = null;
		_hcRestorePreampBtn = null;

		var container = new Grid { RowSpacing = 10 };
		container.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
		container.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1.0, GridUnitType.Star) });

		var headSp = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8, Margin = new Thickness(4, 2, 0, 4) };
		headSp.Children.Add(new FontIcon { Glyph = "\uE81C", FontSize = 14, Foreground = new SolidColorBrush(Color.FromArgb(255, 99, 102, 241)) });
		headSp.Children.Add(new TextBlock { Text = "最近使用", FontSize = 14, FontWeight = FontWeights.Bold, Foreground = DspTextPrimaryBrush });
		headSp.Children.Add(new TextBlock { Text = $"({_recents.Count} 款)", FontSize = 12, Foreground = DspTextMutedBrush });
		Grid.SetRow(headSp, 0);
		container.Children.Add(headSp);

		var sv = new ScrollViewer { Padding = new Thickness(0, 0, 8, 16) };
		var sp = new StackPanel { Spacing = 6 };

		foreach (var rid in _recents)
		{
			var prod = OpraDatabaseService.GetProductById(rid);
			if (prod != null)
			{
				sp.Children.Add(CreateModelRow(prod));
			}
		}

		if (_recents.Count == 0)
		{
			sp.Children.Add(new TextBlock
			{
				Text = "应用校正后，最近使用的型号会显示在这里。",
				FontSize = 13,
				Foreground = DspTextMutedBrush,
				Margin = new Thickness(8, 24, 0, 0)
			});
		}

		sv.Content = sp;
		Grid.SetRow(sv, 1);
		container.Children.Add(sv);

		_hcContentHost.Children.Add(container);
		ApplyThemeToHcContentHost(IsDspDark, container);
	}

	private void OnSearchChanged()
	{
		if (string.IsNullOrWhiteSpace(_searchQuery))
		{
			if (_activeTab == "catalog") ShowAllVendorsView();
			else if (_activeTab == "favorites") ShowFavoritesListView();
			else ShowRecentListView();
			return;
		}

		if (_hcContentHost == null) return;
		_hcVendorCards.Clear();
		_hcModelRows.Clear();
		_hcBackBtn = null;
		_hcDetailPanelBorder = null;
		_hcCurveBorder = null;
		_hcRestorePreampBtn = null;
		_hcContentHost.Children.Clear();

		var container = new Grid { RowSpacing = 8 };
		container.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
		container.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1.0, GridUnitType.Star) });

		var head = new TextBlock
		{
			Text = $"搜索结果: “{_searchQuery}”",
			FontSize = 13,
			FontWeight = FontWeights.SemiBold,
			Foreground = DspTextSubHeaderBrush,
			Margin = new Thickness(4, 2, 0, 4)
		};
		Grid.SetRow(head, 0);
		container.Children.Add(head);

		var results = OpraDatabaseService.SearchProducts(_searchQuery, _activeSource);
		var sv = new ScrollViewer { Padding = new Thickness(0, 0, 8, 16) };
		var sp = new StackPanel { Spacing = 4 };

		foreach (var prod in results)
		{
			sp.Children.Add(CreateModelRow(prod));
		}

		if (results.Count == 0)
		{
			sp.Children.Add(new TextBlock
			{
				Text = "未找到匹配的耳机型号，请尝试搜索品牌 (如 Sennheiser, Sony) 或型号 (如 HD 650, Blessing)。",
				FontSize = 13,
				Foreground = DspTextMutedBrush,
				Margin = new Thickness(8, 20, 0, 0)
			});
		}

		sv.Content = sp;
		Grid.SetRow(sv, 1);
		container.Children.Add(sv);

		_hcContentHost.Children.Add(container);
		ApplyThemeToHcContentHost(IsDspDark, container);
	}

	private void SwitchSource(string source)
	{
		_activeSource = source;
		bool isOpra = source == "opra";

		if (_hcSourceOpraBtn != null)
		{
			_hcSourceOpraBtn.Background = isOpra ? new SolidColorBrush(Color.FromArgb(255, 79, 70, 229)) : new SolidColorBrush(Colors.Transparent);
			_hcSourceOpraBtn.Foreground = isOpra ? new SolidColorBrush(Colors.White) : DspTextMutedBrush;
		}
		if (_hcSourceAutoEqBtn != null)
		{
			_hcSourceAutoEqBtn.Background = !isOpra ? new SolidColorBrush(Color.FromArgb(255, 79, 70, 229)) : new SolidColorBrush(Colors.Transparent);
			_hcSourceAutoEqBtn.Foreground = !isOpra ? new SolidColorBrush(Colors.White) : DspTextMutedBrush;
		}

		RefreshCurrentView();
	}

	private void RefreshCurrentView()
	{
		if (_activeTab == "catalog")
		{
			if (_currentVendor != null) ShowVendorModelsView(_currentVendor);
			else ShowAllVendorsView();
		}
		else if (_activeTab == "favorites")
		{
			ShowFavoritesListView();
		}
		else
		{
			ShowRecentListView();
		}
	}

	private void UpdateBreadcrumbs()
	{
		bool inVendor = _currentVendor != null;
		bool inModel = _currentProduct != null;

		if (_btnBreadcrumbVendor != null)
		{
			_btnBreadcrumbVendor.Foreground = new SolidColorBrush(IsDspDark ? Color.FromArgb(255, 147, 197, 253) : Color.FromArgb(255, 37, 99, 235));
			_btnBreadcrumbVendor.FontWeight = FontWeights.SemiBold;
		}
		if (_btnBreadcrumbModel != null)
		{
			_btnBreadcrumbModel.Foreground = inVendor
				? new SolidColorBrush(IsDspDark ? Color.FromArgb(255, 147, 197, 253) : Color.FromArgb(255, 37, 99, 235))
				: DspTextMutedBrush;
			_btnBreadcrumbModel.FontWeight = inVendor ? FontWeights.SemiBold : FontWeights.Normal;
		}
		if (_btnBreadcrumbSource != null)
		{
			_btnBreadcrumbSource.Foreground = inModel
				? new SolidColorBrush(IsDspDark ? Color.FromArgb(255, 147, 197, 253) : Color.FromArgb(255, 37, 99, 235))
				: DspTextMutedBrush;
			_btnBreadcrumbSource.FontWeight = inModel ? FontWeights.SemiBold : FontWeights.Normal;
		}
	}

	private void UpdateTabButtonStyles()
	{
		SetTabActive(_btnTabCatalog, _activeTab == "catalog");
		SetTabActive(_btnTabFavorites, _activeTab == "favorites");
		SetTabActive(_btnTabRecent, _activeTab == "recent");
	}

	private void SetTabActive(Button? btn, bool active)
	{
		if (btn == null) return;
		btn.Background = active
			? (IsDspDark ? new SolidColorBrush(Color.FromArgb(65, 99, 102, 241)) : new SolidColorBrush(Color.FromArgb(255, 238, 242, 255)))
			: new SolidColorBrush(Colors.Transparent);
		btn.Foreground = active
			? new SolidColorBrush(IsDspDark ? Color.FromArgb(255, 224, 231, 255) : Color.FromArgb(255, 79, 70, 229))
			: DspTextMutedBrush;
		btn.FontWeight = active ? FontWeights.SemiBold : FontWeights.Normal;
	}

	private void UpdateFavoritesAndRecentsBadges()
	{
		if (_tabFavoritesCountTb != null) _tabFavoritesCountTb.Text = _favorites.Count.ToString();
		if (_tabFavoritesBadge != null) _tabFavoritesBadge.Visibility = _favorites.Count > 0 ? Visibility.Visible : Visibility.Collapsed;

		if (_tabRecentCountTb != null) _tabRecentCountTb.Text = _recents.Count.ToString();
		if (_tabRecentBadge != null) _tabRecentBadge.Visibility = _recents.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
	}

	// ----------------------------------------------------
	// HELPERS & FACTORIES
	// ----------------------------------------------------

	private Button CreateSourceButton(string text, bool isSelected)
	{
		return new Button
		{
			Content = text,
			FontSize = 11,
			FontWeight = FontWeights.SemiBold,
			Padding = new Thickness(14, 4, 14, 4),
			CornerRadius = new CornerRadius(14),
			Background = isSelected ? new SolidColorBrush(Color.FromArgb(255, 79, 70, 229)) : new SolidColorBrush(Colors.Transparent),
			Foreground = isSelected ? new SolidColorBrush(Colors.White) : DspTextMutedBrush,
			BorderThickness = new Thickness(0)
		};
	}

	private Button CreateBreadcrumbButton(string glyph, string text, bool isActive)
	{
		var btn = new Button
		{
			Padding = new Thickness(8, 4, 8, 4),
			CornerRadius = new CornerRadius(6),
			Background = new SolidColorBrush(Colors.Transparent),
			BorderThickness = new Thickness(0)
		};
		var sp = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 6 };
		sp.Children.Add(new FontIcon { Glyph = glyph, FontSize = 12 });
		sp.Children.Add(new TextBlock { Text = text, FontSize = 12 });
		btn.Content = sp;
		btn.Foreground = isActive
			? new SolidColorBrush(IsDspDark ? Color.FromArgb(255, 147, 197, 253) : Color.FromArgb(255, 37, 99, 235))
			: DspTextMutedBrush;
		btn.FontWeight = isActive ? FontWeights.SemiBold : FontWeights.Normal;
		return btn;
	}

	private Button CreateCategoryTabButton(string glyph, string text, bool isActive)
	{
		return CreateCategoryTabButton(glyph, text, isActive, out _, out _);
	}

	private Button CreateCategoryTabButton(string glyph, string text, bool isActive, out Border badgeBorder, out TextBlock badgeCountTb)
	{
		var btn = new Button
		{
			Padding = new Thickness(12, 6, 12, 6),
			CornerRadius = new CornerRadius(14),
			Background = isActive
				? (IsDspDark ? new SolidColorBrush(Color.FromArgb(65, 99, 102, 241)) : new SolidColorBrush(Color.FromArgb(255, 238, 242, 255)))
				: new SolidColorBrush(Colors.Transparent),
			BorderThickness = new Thickness(0),
			Foreground = isActive
				? new SolidColorBrush(IsDspDark ? Color.FromArgb(255, 224, 231, 255) : Color.FromArgb(255, 79, 70, 229))
				: DspTextMutedBrush,
			FontWeight = isActive ? FontWeights.SemiBold : FontWeights.Normal
		};
		var sp = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 6 };
		sp.Children.Add(new FontIcon { Glyph = glyph, FontSize = 13 });
		sp.Children.Add(new TextBlock { Text = text, FontSize = 12 });

		badgeBorder = new Border
		{
			Background = DspAccentSoftBgBrush,
			CornerRadius = new CornerRadius(8),
			Padding = new Thickness(6, 1, 6, 1),
			Visibility = Visibility.Collapsed
		};
		badgeCountTb = new TextBlock { Text = "0", FontSize = 10, Foreground = DspAccentSoftTextBrush };
		badgeBorder.Child = badgeCountTb;
		sp.Children.Add(badgeBorder);

		btn.Content = sp;
		return btn;
	}

	public void RefreshHeadphoneCorrectionTheme(bool isDark)
	{
		if (_headphoneCorrectionWorkbench == null) return;

		Brush rootBg = LiquidGlassEngine.CreateSurfaceBrush(LiquidGlassVariant.Clear, isDark: isDark, opacityScale: Math.Clamp(0.72 / 0.85, 0.55, 1.0));
		Brush rimBrush = LiquidGlassEngine.CreateSpecularRimBrush(LiquidGlassVariant.Regular, isDark: isDark);
		Brush accentSoftBg = new SolidColorBrush(isDark ? Color.FromArgb(85, 99, 102, 241) : Color.FromArgb(255, 238, 242, 255));
		Brush accentSoftText = new SolidColorBrush(isDark ? Color.FromArgb(255, 199, 210, 254) : Color.FromArgb(255, 79, 70, 229));
		Brush primaryText = new SolidColorBrush(isDark ? Colors.White : Color.FromArgb(255, 15, 23, 42));
		Brush mutedText = new SolidColorBrush(isDark ? Color.FromArgb(255, 203, 213, 225) : Color.FromArgb(255, 100, 116, 139));

		if (_hcRootGrid != null) _hcRootGrid.Background = rootBg;
		if (_hcTitleIconBorder != null) _hcTitleIconBorder.Background = accentSoftBg;
		if (_hcTitleIcon != null) _hcTitleIcon.Foreground = accentSoftText;

		if (_hcSourceContainer != null)
		{
			_hcSourceContainer.Background = isDark ? new SolidColorBrush(Color.FromArgb(255, 30, 41, 59)) : new SolidColorBrush(Color.FromArgb(255, 241, 245, 249));
			_hcSourceContainer.BorderBrush = isDark ? new SolidColorBrush(Color.FromArgb(70, 255, 255, 255)) : new SolidColorBrush(Color.FromArgb(255, 226, 232, 240));
		}
		if (_hcSourceOpraBtn != null)
		{
			bool isOpra = _activeSource == "opra";
			_hcSourceOpraBtn.Background = isOpra ? new SolidColorBrush(Color.FromArgb(255, 79, 70, 229)) : new SolidColorBrush(Colors.Transparent);
			_hcSourceOpraBtn.Foreground = isOpra ? new SolidColorBrush(Colors.White) : mutedText;
		}
		if (_hcSourceAutoEqBtn != null)
		{
			bool isOpra = _activeSource == "opra";
			_hcSourceAutoEqBtn.Background = !isOpra ? new SolidColorBrush(Color.FromArgb(255, 79, 70, 229)) : new SolidColorBrush(Colors.Transparent);
			_hcSourceAutoEqBtn.Foreground = !isOpra ? new SolidColorBrush(Colors.White) : mutedText;
		}
		if (_hcSearchBox != null)
		{
			_hcSearchBox.Background = isDark ? new SolidColorBrush(Color.FromArgb(140, 30, 41, 59)) : new SolidColorBrush(Color.FromArgb(220, 255, 255, 255));
			_hcSearchBox.BorderBrush = isDark ? new SolidColorBrush(Color.FromArgb(70, 255, 255, 255)) : new SolidColorBrush(Color.FromArgb(255, 226, 232, 240));
			_hcSearchBox.Foreground = primaryText;
		}
		if (_hcRefreshBtn != null)
		{
			_hcRefreshBtn.Background = isDark ? new SolidColorBrush(Color.FromArgb(120, 30, 41, 59)) : new SolidColorBrush(Color.FromArgb(200, 255, 255, 255));
			_hcRefreshBtn.BorderBrush = isDark ? new SolidColorBrush(Color.FromArgb(70, 255, 255, 255)) : new SolidColorBrush(Color.FromArgb(255, 226, 232, 240));
			if (_hcRefreshBtn.Content is FontIcon rIcon) rIcon.Foreground = mutedText;
		}
		if (_hcBannerBorder != null)
		{
			_hcBannerBorder.Background = isDark ? new SolidColorBrush(Color.FromArgb(160, 30, 41, 59)) : new SolidColorBrush(Color.FromArgb(170, 255, 255, 255));
			_hcBannerBorder.BorderBrush = isDark ? new SolidColorBrush(Color.FromArgb(80, 255, 255, 255)) : new SolidColorBrush(Color.FromArgb(160, 226, 232, 240));
		}
		if (_hcBannerIconBox != null)
		{
			_hcBannerIconBox.Background = isDark ? new SolidColorBrush(Color.FromArgb(255, 49, 46, 129)) : new SolidColorBrush(Color.FromArgb(255, 241, 245, 249));
		}
		if (_tabFavoritesBadge != null) _tabFavoritesBadge.Background = accentSoftBg;
		if (_tabFavoritesCountTb != null) _tabFavoritesCountTb.Foreground = accentSoftText;
		if (_tabRecentBadge != null) _tabRecentBadge.Background = accentSoftBg;
		if (_tabRecentCountTb != null) _tabRecentCountTb.Foreground = accentSoftText;

		UpdateBreadcrumbs();
		UpdateTabButtonStyles();
		ApplyThemeToHcContentHost(isDark);
	}

	private void ApplyThemeToHcContentHost(bool isDark, FrameworkElement? newlyAddedContainer = null)
	{
		Brush cardBg = LiquidGlassEngine.CreateSurfaceBrush(LiquidGlassVariant.Regular, isDark: isDark, opacityScale: Math.Clamp(0.88 / 0.90, 0.72, 1.05));
		Brush rimBrush = LiquidGlassEngine.CreateSpecularRimBrush(LiquidGlassVariant.Regular, isDark: isDark);
		Brush softBg = isDark
			? LiquidGlassEngine.CreateSurfaceBrush(LiquidGlassVariant.Clear, isDark: true, opacityScale: 0.45)
			: new SolidColorBrush(Color.FromArgb(220, 241, 245, 249));
		Brush primaryText = new SolidColorBrush(isDark ? Colors.White : Color.FromArgb(255, 15, 23, 42));
		Brush secondaryText = new SolidColorBrush(isDark ? Color.FromArgb(255, 248, 250, 252) : Color.FromArgb(255, 51, 65, 85));
		Brush accentSoftText = new SolidColorBrush(isDark ? Color.FromArgb(255, 199, 210, 254) : Color.FromArgb(255, 79, 70, 229));

		foreach (var card in _hcVendorCards)
		{
			card.Background = isDark ? new SolidColorBrush(Color.FromArgb(160, 30, 41, 59)) : new SolidColorBrush(Color.FromArgb(245, 255, 255, 255));
			card.BorderBrush = isDark ? new SolidColorBrush(Color.FromArgb(85, 255, 255, 255)) : new SolidColorBrush(Color.FromArgb(255, 226, 232, 240));
		}

		foreach (var row in _hcModelRows)
		{
			row.Background = cardBg;
			row.BorderBrush = rimBrush;
			LiquidGlassEngine.AttachInteractiveLiquidGlass(row, LiquidGlassVariant.Regular, isDark: isDark, enablePointerTracking: false);
		}

		if (_hcBackBtn != null)
		{
			_hcBackBtn.Background = softBg;
			_hcBackBtn.BorderBrush = rimBrush;
			_hcBackBtn.Foreground = primaryText;
			if (_hcBackBtn.Content is StackPanel bsp)
			{
				foreach (var ch in bsp.Children)
				{
					if (ch is FontIcon bfi) bfi.Foreground = primaryText;
					else if (ch is TextBlock btb) btb.Foreground = primaryText;
				}
			}
		}

		if (_hcDetailPanelBorder != null)
		{
			_hcDetailPanelBorder.Background = cardBg;
			_hcDetailPanelBorder.BorderBrush = rimBrush;
			LiquidGlassEngine.AttachInteractiveLiquidGlass(_hcDetailPanelBorder, LiquidGlassVariant.Regular, isDark: isDark, enablePointerTracking: false);
		}

		if (_btnFavPreset != null)
		{
			_btnFavPreset.Background = softBg;
			_btnFavPreset.BorderBrush = rimBrush;
		}

		if (_hcCurveBorder != null)
		{
			_hcCurveBorder.Background = isDark ? new SolidColorBrush(Color.FromArgb(140, 15, 23, 42)) : new SolidColorBrush(Color.FromArgb(255, 248, 250, 252));
			_hcCurveBorder.BorderBrush = rimBrush;
		}
		_curveControl?.SetDarkTheme(isDark);

		if (_hpPreampValueTb != null) _hpPreampValueTb.Foreground = accentSoftText;
		if (_hcRestorePreampBtn != null)
		{
			_hcRestorePreampBtn.Background = softBg;
			_hcRestorePreampBtn.BorderBrush = rimBrush;
			_hcRestorePreampBtn.Foreground = secondaryText;
		}

		UpdateCalibrationModeButtonStyles(isDark);
		UpdateSourcePresetRowsStyles(isDark);

		if (_hcContentHost != null)
		{
			LiquidGlassEngine.UpgradeVisualTreeToLiquidGlass(_hcContentHost, isDark: isDark);
			RefreshWorkbenchTextContrast(_hcContentHost, isDark);
		}

		if (newlyAddedContainer != null && !newlyAddedContainer.IsLoaded)
		{
			void OnContainerLoaded(object s, RoutedEventArgs e)
			{
				newlyAddedContainer.Loaded -= OnContainerLoaded;
				bool currentDark = IsDspDark;
				if (_hcContentHost != null)
				{
					LiquidGlassEngine.UpgradeVisualTreeToLiquidGlass(_hcContentHost, isDark: currentDark);
					RefreshWorkbenchTextContrast(_hcContentHost, currentDark);
				}
			}
			newlyAddedContainer.Loaded += OnContainerLoaded;
		}
	}

	// ----------------------------------------------------
	// STATE PERSISTENCE
	// ----------------------------------------------------

	private void LoadHeadphoneState()
	{
		// Default built-in recents to match screenshot (最近使用 2)
		_recents.Clear();
		_recents.Add("sennheiser::hd_650");
		_recents.Add("moondrop::blessing2");

		try
		{
			string filePath = IOPath.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "FurinaPlayer", "headphone_correction_state.json");
			if (File.Exists(filePath))
			{
				string json = File.ReadAllText(filePath);
				using var doc = JsonDocument.Parse(json);
				var root = doc.RootElement;
				if (root.TryGetProperty("appliedPresetId", out var ap)) _appliedPresetId = ap.GetString();
				if (root.TryGetProperty("enabled", out var en)) _isCalibrationEnabled = en.GetBoolean();
				if (root.TryGetProperty("mode", out var m)) _calibrationMode = m.GetString() ?? "original";
				if (root.TryGetProperty("customPreamp", out var cp) && cp.ValueKind == JsonValueKind.Number) _userPreampDb = cp.GetDouble();

				if (root.TryGetProperty("favorites", out var favs) && favs.ValueKind == JsonValueKind.Array)
				{
					_favorites.Clear();
					foreach (var f in favs.EnumerateArray())
					{
						string? s = f.GetString();
						if (!string.IsNullOrEmpty(s)) _favorites.Add(s);
					}
				}

				if (root.TryGetProperty("recents", out var recs) && recs.ValueKind == JsonValueKind.Array)
				{
					_recents.Clear();
					foreach (var r in recs.EnumerateArray())
					{
						string? s = r.GetString();
						if (!string.IsNullOrEmpty(s)) _recents.Add(s);
					}
				}
			}
		}
		catch
		{
			// Ignore state loading error
		}
	}

	private void SaveHeadphoneState()
	{
		try
		{
			string dir = IOPath.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "FurinaPlayer");
			if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

			string filePath = IOPath.Combine(dir, "headphone_correction_state.json");
			var obj = new
			{
				appliedPresetId = _appliedPresetId,
				enabled = _isCalibrationEnabled,
				mode = _calibrationMode,
				customPreamp = _userPreampDb,
				favorites = _favorites.ToList(),
				recents = _recents.ToList()
			};
			string json = JsonSerializer.Serialize(obj, new JsonSerializerOptions { WriteIndented = true });
			File.WriteAllText(filePath, json);
		}
		catch
		{
			// Ignore save failure
		}
	}
}

// ========================================================
// SUPPORTING CUSTOM CONTROLS
// ========================================================

public sealed class HeadphoneCurveControl : UserControl
{
	private readonly SKXamlCanvas _canvas;
	private OpraHeadphonePreset? _preset;
	private string _mode = "original";
	private double? _customPreamp = null;
	private bool _isDark = false;
	private readonly List<EchoBiquadFilter> _filters = new();

	public HeadphoneCurveControl()
	{
		_canvas = new SKXamlCanvas();
		_canvas.PaintSurface += OnPaintSurface;
		Content = _canvas;
		MinHeight = 160;
		_isDark = FurinaPlayer.UI.Controls.LiquidGlassEngine.IsElementDarkTheme(this);
	}

	public void SetDarkTheme(bool isDark)
	{
		if (_isDark == isDark) return;
		_isDark = isDark;
		_canvas.Invalidate();
	}

	public void SetCurve(OpraHeadphonePreset? preset, string mode, double? customPreamp)
	{
		_preset = preset;
		_mode = mode;
		_customPreamp = customPreamp;
		_filters.Clear();
		if (preset != null)
		{
			foreach (var f in preset.Filters)
			{
				var filter = new EchoBiquadFilter
				{
					FilterType = f.Type,
					FrequencyHz = f.FrequencyHz,
					GainDb = f.GainDb,
					Q = f.Q,
					Enabled = true
				};
				filter.UpdateCoefficients(48000.0);
				_filters.Add(filter);
			}
		}
		_canvas.Invalidate();
	}

	private void OnPaintSurface(object? sender, SKPaintSurfaceEventArgs e)
	{
		var canvas = e.Surface.Canvas;
		bool isDark = _isDark;
		canvas.Clear(isDark ? new SKColor(15, 23, 42, 165) : new SKColor(248, 250, 252));

		float w = e.Info.Width;
		float h = e.Info.Height;
		float padLeft = 36f;
		float padRight = 48f;
		float padTop = 16f;
		float padBottom = 22f;
		float plotW = Math.Max(1f, w - padLeft - padRight);
		float plotH = Math.Max(1f, h - padTop - padBottom);

		double logMin = Math.Log10(20.0);
		double logMax = Math.Log10(20000.0);

		using var gridPaint = new SKPaint
		{
			Color = isDark ? new SKColor(255, 255, 255, 28) : new SKColor(226, 232, 240),
			StrokeWidth = 1f,
			Style = SKPaintStyle.Stroke
		};
		using var zeroLinePaint = new SKPaint
		{
			Color = isDark ? new SKColor(255, 255, 255, 72) : new SKColor(203, 213, 225),
			StrokeWidth = 1.2f,
			Style = SKPaintStyle.Stroke
		};
		using var textPaint = new SKPaint
		{
			Color = isDark ? new SKColor(203, 213, 225) : new SKColor(148, 163, 184),
			TextSize = 10f,
			IsAntialias = true
		};

		// Frequencies
		int[] freqs = { 20, 64, 125, 250, 500, 1000, 2000, 4000, 8000, 16000 };
		string[] freqLabels = { "20", "64", "125", "250", "500", "1k", "2k", "4k", "8k", "16k" };
		for (int i = 0; i < freqs.Length; i++)
		{
			float normX = (float)((Math.Log10(freqs[i]) - logMin) / (logMax - logMin));
			float x = padLeft + normX * plotW;
			canvas.DrawLine(x, padTop, x, padTop + plotH, gridPaint);
			float labelW = textPaint.MeasureText(freqLabels[i]);
			canvas.DrawText(freqLabels[i], x - labelW / 2f, padTop + plotH + 16f, textPaint);
		}

		// Gains
		int[] gains = { 12, 6, 0, -6, -12 };
		foreach (int g in gains)
		{
			float normY = (float)((18.0 - g) / 36.0);
			float y = padTop + normY * plotH;
			canvas.DrawLine(padLeft, y, padLeft + plotW, y, g == 0 ? zeroLinePaint : gridPaint);
			string gText = $"{g:+0;-0;0} dB";
			canvas.DrawText(gText, padLeft + plotW + 6f, y + 3.5f, textPaint);
		}

		if (_preset == null || _filters.Count == 0) return;

		double origPreamp = _preset.PreampDb;
		double effPreamp = _customPreamp ?? origPreamp;

		int points = 96;
		var curvePts = new SKPoint[points];
		var origPts = new SKPoint[points];

		for (int i = 0; i < points; i++)
		{
			double freq = 20.0 * Math.Pow(1000.0, (double)i / (points - 1));
			float normX = (float)((Math.Log10(freq) - logMin) / (logMax - logMin));
			float x = padLeft + normX * plotW;

			double origResp = origPreamp;
			foreach (var f in _filters) origResp += f.CalculateResponseDb(freq, 48000.0);
			origResp = Math.Clamp(origResp, -18.0, 18.0);
			float origNormY = (float)((18.0 - origResp) / 36.0);
			origPts[i] = new SKPoint(x, padTop + origNormY * plotH);

			double curResp = effPreamp;
			if (_mode == "listening")
			{
				foreach (var f in _filters)
				{
					double r = f.CalculateResponseDb(freq, 48000.0);
					if (r < -2.0 && freq >= 500 && freq <= 4000) r *= 0.65;
					curResp += r;
				}
			}
			else
			{
				foreach (var f in _filters) curResp += f.CalculateResponseDb(freq, 48000.0);
			}
			curResp = Math.Clamp(curResp, -18.0, 18.0);
			float curNormY = (float)((18.0 - curResp) / 36.0);
			curvePts[i] = new SKPoint(x, padTop + curNormY * plotH);
		}

		// Draw original dashed curve if in listening mode or custom preamp
		if (_mode == "listening" || _customPreamp.HasValue)
		{
			using var dashedPaint = new SKPaint
			{
				Color = isDark ? new SKColor(148, 163, 184, 210) : new SKColor(148, 163, 184, 180),
				StrokeWidth = 1.5f,
				Style = SKPaintStyle.Stroke,
				PathEffect = SKPathEffect.CreateDash(new float[] { 5f, 4f }, 0),
				IsAntialias = true
			};
			using var origPath = new SKPath();
			origPath.MoveTo(origPts[0]);
			for (int i = 1; i < points; i++) origPath.LineTo(origPts[i]);
			canvas.DrawPath(origPath, dashedPaint);
		}

		// Draw filled gradient area
		float zeroY = padTop + 0.5f * plotH;
		using var fillPath = new SKPath();
		fillPath.MoveTo(curvePts[0].X, zeroY);
		fillPath.LineTo(curvePts[0]);
		for (int i = 1; i < points; i++) fillPath.LineTo(curvePts[i]);
		fillPath.LineTo(curvePts[points - 1].X, zeroY);
		fillPath.Close();

		using var fillPaint = new SKPaint
		{
			Color = isDark ? new SKColor(129, 140, 248, 52) : new SKColor(99, 102, 241, 35),
			Style = SKPaintStyle.Fill,
			IsAntialias = true
		};
		canvas.DrawPath(fillPath, fillPaint);

		// Draw active curve stroke
		using var curvePaint = new SKPaint
		{
			Color = isDark ? new SKColor(129, 140, 248) : new SKColor(99, 102, 241),
			StrokeWidth = 2.5f,
			Style = SKPaintStyle.Stroke,
			IsAntialias = true
		};
		using var curvePath = new SKPath();
		curvePath.MoveTo(curvePts[0]);
		for (int i = 1; i < points; i++) curvePath.LineTo(curvePts[i]);
		canvas.DrawPath(curvePath, curvePaint);
	}
}

public class SimpleWrapPanel : Panel
{
	public double ItemWidth { get; set; } = double.NaN;
	public double ItemHeight { get; set; } = double.NaN;

	protected override Size MeasureOverride(Size availableSize)
	{
		double curX = 0, curY = 0, rowMaxH = 0, maxW = 0;
		foreach (UIElement child in Children)
		{
			child.Measure(new Size(double.IsNaN(ItemWidth) ? availableSize.Width : ItemWidth,
								   double.IsNaN(ItemHeight) ? availableSize.Height : ItemHeight));
			var desired = child.DesiredSize;
			if (curX + desired.Width > availableSize.Width && curX > 0)
			{
				curY += rowMaxH;
				curX = 0;
				rowMaxH = 0;
			}
			curX += desired.Width;
			rowMaxH = Math.Max(rowMaxH, desired.Height);
			maxW = Math.Max(maxW, curX);
		}
		return new Size(double.IsPositiveInfinity(availableSize.Width) ? maxW : availableSize.Width, curY + rowMaxH);
	}

	protected override Size ArrangeOverride(Size finalSize)
	{
		double curX = 0, curY = 0, rowMaxH = 0;
		foreach (UIElement child in Children)
		{
			var desired = child.DesiredSize;
			if (curX + desired.Width > finalSize.Width && curX > 0)
			{
				curY += rowMaxH;
				curX = 0;
				rowMaxH = 0;
			}
			child.Arrange(new Rect(curX, curY, desired.Width, desired.Height));
			curX += desired.Width;
			rowMaxH = Math.Max(rowMaxH, desired.Height);
		}
		return finalSize;
	}
}
