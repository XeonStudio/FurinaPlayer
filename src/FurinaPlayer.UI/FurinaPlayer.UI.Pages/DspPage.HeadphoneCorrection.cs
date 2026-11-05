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

	// Top Pills
	private TextBlock? _pillModulesTb;
	private TextBlock? _pillStatusTb;

	public FrameworkElement CreateHeadphoneCorrectionWorkbench()
	{
		if (_headphoneCorrectionWorkbench != null)
		{
			return _headphoneCorrectionWorkbench;
		}

		LoadHeadphoneState();

		var root = new Grid
		{
			Background = CreateWhiteAcrylicBrush(0.72, 195),
			Padding = new Thickness(24, 18, 24, 20),
			RowSpacing = 12
		};

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
		var iconBorder = new Border
		{
			Width = 34,
			Height = 34,
			CornerRadius = new CornerRadius(17),
			Background = new SolidColorBrush(Color.FromArgb(255, 238, 242, 255)),
			Child = new FontIcon
			{
				Glyph = "\uE7F5",
				FontSize = 17,
				Foreground = new SolidColorBrush(Color.FromArgb(255, 99, 102, 241)),
				HorizontalAlignment = HorizontalAlignment.Center,
				VerticalAlignment = VerticalAlignment.Center
			}
		};
		titleSp.Children.Add(iconBorder);
		titleSp.Children.Add(new TextBlock
		{
			Text = "耳机校正",
			FontSize = 18,
			FontWeight = FontWeights.SemiBold,
			Foreground = new SolidColorBrush(Color.FromArgb(255, 15, 23, 42)),
			VerticalAlignment = VerticalAlignment.Center
		});
		Grid.SetColumn(titleSp, 0);
		topBar.Children.Add(titleSp);

		// Source Selector [OPRA] [AutoEQ]
		var sourceContainer = new Border
		{
			Background = new SolidColorBrush(Color.FromArgb(255, 241, 245, 249)),
			BorderBrush = new SolidColorBrush(Color.FromArgb(255, 226, 232, 240)),
			BorderThickness = new Thickness(1),
			CornerRadius = new CornerRadius(16),
			Padding = new Thickness(2),
			Margin = new Thickness(18, 0, 16, 0),
			VerticalAlignment = VerticalAlignment.Center
		};
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
			Background = new SolidColorBrush(Color.FromArgb(220, 255, 255, 255)),
			BorderBrush = new SolidColorBrush(Color.FromArgb(255, 226, 232, 240)),
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
			Background = new SolidColorBrush(Color.FromArgb(200, 255, 255, 255)),
			BorderBrush = new SolidColorBrush(Color.FromArgb(255, 226, 232, 240)),
			Content = new FontIcon { Glyph = "\uE72C", FontSize = 13, Foreground = new SolidColorBrush(Color.FromArgb(255, 100, 116, 139)) }
		};
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

		// Pill 1: Modules count
		var pill1 = new Border
		{
			Background = new SolidColorBrush(Color.FromArgb(255, 238, 242, 255)),
			BorderBrush = new SolidColorBrush(Color.FromArgb(255, 224, 231, 255)),
			BorderThickness = new Thickness(1),
			CornerRadius = new CornerRadius(12),
			Padding = new Thickness(10, 4, 10, 4),
			VerticalAlignment = VerticalAlignment.Center
		};
		var pill1Sp = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 5 };
		pill1Sp.Children.Add(new FontIcon { Glyph = "\uE945", FontSize = 11, Foreground = new SolidColorBrush(Color.FromArgb(255, 99, 102, 241)) });
		_pillModulesTb = new TextBlock
		{
			Text = "4 个模块启用",
			FontSize = 11,
			FontWeight = FontWeights.Medium,
			Foreground = new SolidColorBrush(Color.FromArgb(255, 79, 70, 229))
		};
		pill1Sp.Children.Add(_pillModulesTb);
		pill1.Child = pill1Sp;
		rightSp.Children.Add(pill1);

		// Pill 2: Ready Status
		var pill2 = new Border
		{
			Background = new SolidColorBrush(Color.FromArgb(255, 240, 253, 244)),
			BorderBrush = new SolidColorBrush(Color.FromArgb(255, 220, 252, 231)),
			BorderThickness = new Thickness(1),
			CornerRadius = new CornerRadius(12),
			Padding = new Thickness(10, 4, 10, 4),
			VerticalAlignment = VerticalAlignment.Center
		};
		var pill2Sp = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 5 };
		pill2Sp.Children.Add(new FontIcon { Glyph = "\uE995", FontSize = 11, Foreground = new SolidColorBrush(Color.FromArgb(255, 22, 163, 74)) });
		_pillStatusTb = new TextBlock
		{
			Text = "就绪",
			FontSize = 11,
			FontWeight = FontWeights.Medium,
			Foreground = new SolidColorBrush(Color.FromArgb(255, 22, 163, 74))
		};
		pill2Sp.Children.Add(_pillStatusTb);
		pill2.Child = pill2Sp;
		rightSp.Children.Add(pill2);

		// Pill 3: Refresh Status button
		var pill3Btn = new Button
		{
			Background = new SolidColorBrush(Color.FromArgb(255, 248, 250, 252)),
			BorderBrush = new SolidColorBrush(Color.FromArgb(255, 226, 232, 240)),
			BorderThickness = new Thickness(1),
			CornerRadius = new CornerRadius(12),
			Padding = new Thickness(10, 4, 10, 4),
			VerticalAlignment = VerticalAlignment.Center
		};
		var pill3Sp = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 5 };
		pill3Sp.Children.Add(new FontIcon { Glyph = "\uE72C", FontSize = 11, Foreground = new SolidColorBrush(Color.FromArgb(255, 71, 85, 105)) });
		pill3Sp.Children.Add(new TextBlock
		{
			Text = "刷新状态",
			FontSize = 11,
			FontWeight = FontWeights.Medium,
			Foreground = new SolidColorBrush(Color.FromArgb(255, 71, 85, 105))
		});
		pill3Btn.Content = pill3Sp;
		pill3Btn.Click += (s, e) => UpdateEngineStatusPills();
		rightSp.Children.Add(pill3Btn);

		Grid.SetColumn(rightSp, 3);
		topBar.Children.Add(rightSp);

		Grid.SetRow(topBar, 0);
		root.Children.Add(topBar);

		// ==========================================
		// 1. PRESET STATUS & BREADCRUMB GUIDE BANNER
		// ==========================================
		var banner = new Border
		{
			Background = new SolidColorBrush(Color.FromArgb(170, 255, 255, 255)),
			BorderBrush = new SolidColorBrush(Color.FromArgb(160, 226, 232, 240)),
			BorderThickness = new Thickness(1),
			CornerRadius = new CornerRadius(12),
			Padding = new Thickness(18, 12, 18, 12)
		};

		var bannerGrid = new Grid();
		bannerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.0, GridUnitType.Star) });
		bannerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

		var bannerLeftSp = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 14, VerticalAlignment = VerticalAlignment.Center };
		var bannerIconBox = new Border
		{
			Width = 42,
			Height = 42,
			CornerRadius = new CornerRadius(10),
			Background = new SolidColorBrush(Color.FromArgb(255, 241, 245, 249)),
			Child = new FontIcon
			{
				Glyph = "\uE7F5",
				FontSize = 20,
				Foreground = new SolidColorBrush(Color.FromArgb(255, 99, 102, 241)),
				HorizontalAlignment = HorizontalAlignment.Center,
				VerticalAlignment = VerticalAlignment.Center
			}
		};
		bannerLeftSp.Children.Add(bannerIconBox);

		var bannerTextSp = new StackPanel { Spacing = 2, VerticalAlignment = VerticalAlignment.Center };
		_hcBannerTitleTb = new TextBlock
		{
			Text = "未选择预设",
			FontSize = 14,
			FontWeight = FontWeights.SemiBold,
			Foreground = new SolidColorBrush(Color.FromArgb(255, 15, 23, 42))
		};
		_hcBannerSubtitleTb = new TextBlock
		{
			Text = "按生产商或型号浏览，找到合适的校正预设。",
			FontSize = 12,
			Foreground = new SolidColorBrush(Color.FromArgb(255, 100, 116, 139))
		};
		_hcBannerStatsTb = new TextBlock
		{
			Text = "714 个品牌  6230 款耳机",
			FontSize = 11,
			Foreground = new SolidColorBrush(Color.FromArgb(255, 148, 163, 184))
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

		_chevronBreadcrumb1 = new FontIcon { Glyph = "\uE76C", FontSize = 9, Foreground = new SolidColorBrush(Color.FromArgb(255, 148, 163, 184)) };

		_btnBreadcrumbModel = CreateBreadcrumbButton("\uE7F5", "型号", false);
		_btnBreadcrumbModel.Click += (s, e) =>
		{
			if (_currentVendor != null) ShowVendorModelsView(_currentVendor);
		};

		_chevronBreadcrumb2 = new FontIcon { Glyph = "\uE76C", FontSize = 9, Foreground = new SolidColorBrush(Color.FromArgb(255, 148, 163, 184)) };

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
			Foreground = new SolidColorBrush(Color.FromArgb(255, 100, 116, 139)),
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

		UpdateEngineStatusPills();
		return _headphoneCorrectionWorkbench;
	}

	// ----------------------------------------------------
	// VIEWS RENDERING
	// ----------------------------------------------------

	private void ShowAllVendorsView()
	{
		if (_hcContentHost == null) return;
		_currentVendor = null;
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
			Foreground = new SolidColorBrush(Color.FromArgb(255, 15, 23, 42)),
			VerticalAlignment = VerticalAlignment.Center
		});
		var (vCount, pCount, eqCount) = OpraDatabaseService.GetDatabaseStats();
		subHeaderSp.Children.Add(new TextBlock
		{
			Text = $"{pCount} 款耳机 · {eqCount} 条曲线",
			FontSize = 11,
			Foreground = new SolidColorBrush(Color.FromArgb(255, 100, 116, 139)),
			VerticalAlignment = VerticalAlignment.Center
		});
		Grid.SetRow(subHeaderSp, 0);
		container.Children.Add(subHeaderSp);

		// Brand Cards Grid in ScrollViewer
		var sv = new ScrollViewer
		{
			VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
			HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
			Padding = new Thickness(0, 0, 8, 16)
		};

		var wrapPanel = new SimpleWrapPanel
		{
			ItemWidth = 116,
			ItemHeight = 128
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
	}

	private FrameworkElement CreateVendorCard(OpraVendor vendor)
	{
		var card = new Border
		{
			Width = 114,
			Height = 126,
			Margin = new Thickness(4),
			CornerRadius = new CornerRadius(10),
			Background = new SolidColorBrush(Color.FromArgb(240, 255, 255, 255)),
			BorderBrush = new SolidColorBrush(Color.FromArgb(255, 226, 232, 240)),
			BorderThickness = new Thickness(1),
			Padding = new Thickness(6, 8, 6, 6)
		};

		var sp = new StackPanel
		{
			Spacing = 4,
			VerticalAlignment = VerticalAlignment.Center,
			HorizontalAlignment = HorizontalAlignment.Center
		};

		// Logo Container
		var logoBox = new Border
		{
			Width = 80,
			Height = 44,
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
					MaxWidth = 72,
					MaxHeight = 36,
					Stretch = Stretch.Uniform,
					HorizontalAlignment = HorizontalAlignment.Center,
					VerticalAlignment = VerticalAlignment.Center
				};
				logoBox.Child = img;
			}
			catch
			{
				logoBox.Child = CreateBrandTypography(vendor.Name);
			}
		}
		else
		{
			logoBox.Child = CreateBrandTypography(vendor.Name);
		}

		sp.Children.Add(logoBox);

		// Brand Name
		sp.Children.Add(new TextBlock
		{
			Text = vendor.Name,
			FontSize = 12,
			FontWeight = FontWeights.SemiBold,
			Foreground = new SolidColorBrush(Color.FromArgb(255, 15, 23, 42)),
			TextAlignment = TextAlignment.Center,
			TextTrimming = TextTrimming.CharacterEllipsis,
			MaxWidth = 102
		});

		// Count stats: e.g. "278 款 / 763 个预设"
		sp.Children.Add(new TextBlock
		{
			Text = $"{vendor.ProductCount} 款 / {vendor.EqCount} 个预设",
			FontSize = 10,
			Foreground = new SolidColorBrush(Color.FromArgb(255, 148, 163, 184)),
			TextAlignment = TextAlignment.Center,
			MaxWidth = 102
		});

		card.Child = sp;

		// Hover and Click
		card.PointerEntered += (s, e) =>
		{
			card.BorderBrush = new SolidColorBrush(Color.FromArgb(255, 129, 140, 248));
			card.Background = new SolidColorBrush(Color.FromArgb(255, 248, 250, 252));
		};
		card.PointerExited += (s, e) =>
		{
			card.BorderBrush = new SolidColorBrush(Color.FromArgb(255, 226, 232, 240));
			card.Background = new SolidColorBrush(Color.FromArgb(240, 255, 255, 255));
		};
		card.Tapped += (s, e) => ShowVendorModelsView(vendor);

		return card;
	}

	private static FrameworkElement CreateBrandTypography(string brandName)
	{
		return new Border
		{
			Padding = new Thickness(4, 2, 4, 2),
			Child = new TextBlock
			{
				Text = brandName,
				FontSize = brandName.Length > 8 ? 11 : 13,
				FontWeight = FontWeights.Bold,
				Foreground = new SolidColorBrush(Color.FromArgb(255, 71, 85, 105)),
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
					new FontIcon { Glyph = "\uE72B", FontSize = 12 },
					new TextBlock { Text = "所有生产商", FontSize = 12, FontWeight = FontWeights.Medium }
				}
			},
			Padding = new Thickness(8, 4, 10, 4),
			CornerRadius = new CornerRadius(14),
			Background = new SolidColorBrush(Color.FromArgb(200, 255, 255, 255)),
			BorderBrush = new SolidColorBrush(Color.FromArgb(255, 226, 232, 240)),
			VerticalAlignment = VerticalAlignment.Center
		};
		backBtn.Click += (s, e) => ShowAllVendorsView();
		headerSp.Children.Add(backBtn);

		headerSp.Children.Add(new TextBlock
		{
			Text = vendor.Name,
			FontSize = 16,
			FontWeight = FontWeights.Bold,
			Foreground = new SolidColorBrush(Color.FromArgb(255, 15, 23, 42)),
			VerticalAlignment = VerticalAlignment.Center
		});
		headerSp.Children.Add(new TextBlock
		{
			Text = $"{vendor.ProductCount} 款耳机 · {vendor.EqCount} 个校正预设",
			FontSize = 11,
			Foreground = new SolidColorBrush(Color.FromArgb(255, 100, 116, 139)),
			VerticalAlignment = VerticalAlignment.Center
		});
		Grid.SetRow(headerSp, 0);
		leftContainer.Children.Add(headerSp);

		// Column headers
		var colHeaders = new Grid { Margin = new Thickness(12, 4, 12, 0) };
		colHeaders.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.0, GridUnitType.Star) });
		colHeaders.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(90) });
		colHeaders.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(60) });

		colHeaders.Children.Add(new TextBlock { Text = "型号", FontSize = 11, FontWeight = FontWeights.SemiBold, Foreground = new SolidColorBrush(Color.FromArgb(255, 148, 163, 184)) });
		var th2 = new TextBlock { Text = "可用曲线", FontSize = 11, FontWeight = FontWeights.SemiBold, Foreground = new SolidColorBrush(Color.FromArgb(255, 148, 163, 184)), TextAlignment = TextAlignment.Center };
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
	}

	private FrameworkElement CreateModelRow(OpraProduct prod)
	{
		var row = new Border
		{
			Background = new SolidColorBrush(Color.FromArgb(220, 255, 255, 255)),
			BorderBrush = new SolidColorBrush(Color.FromArgb(255, 226, 232, 240)),
			BorderThickness = new Thickness(1),
			CornerRadius = new CornerRadius(8),
			Padding = new Thickness(12, 8, 12, 8)
		};

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
			Background = new SolidColorBrush(Color.FromArgb(255, 241, 245, 249)),
			Child = new FontIcon { Glyph = "\uE7F5", FontSize = 15, Foreground = new SolidColorBrush(Color.FromArgb(255, 100, 116, 139)) }
		};
		nameSp.Children.Add(artIcon);

		var labelSp = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
		labelSp.Children.Add(new TextBlock
		{
			Text = prod.Name,
			FontSize = 13,
			FontWeight = FontWeights.SemiBold,
			Foreground = new SolidColorBrush(Color.FromArgb(255, 15, 23, 42)),
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
				Foreground = new SolidColorBrush(Color.FromArgb(255, 148, 163, 184))
			});
		}
		nameSp.Children.Add(labelSp);
		Grid.SetColumn(nameSp, 0);
		rowGrid.Children.Add(nameSp);

		// Col 1: Preset Count badge
		var countBadge = new Border
		{
			Background = new SolidColorBrush(Color.FromArgb(255, 241, 245, 249)),
			CornerRadius = new CornerRadius(10),
			Padding = new Thickness(8, 2, 8, 2),
			HorizontalAlignment = HorizontalAlignment.Center,
			VerticalAlignment = VerticalAlignment.Center,
			Child = new TextBlock
			{
				Text = $"{prod.PresetCount} 个预设",
				FontSize = 10,
				Foreground = new SolidColorBrush(Color.FromArgb(255, 71, 85, 105))
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
				Foreground = new SolidColorBrush(isFav ? Color.FromArgb(255, 234, 179, 8) : Color.FromArgb(255, 148, 163, 184))
			}
		};
		starBtn.Click += (s, e) =>
		{
			ToggleFavorite(prod.Id);
			bool nowFav = _favorites.Contains(prod.Id);
			if (starBtn.Content is FontIcon fi)
			{
				fi.Glyph = nowFav ? "\uE735" : "\uE734";
				fi.Foreground = new SolidColorBrush(nowFav ? Color.FromArgb(255, 234, 179, 8) : Color.FromArgb(255, 148, 163, 184));
			}
		};
		actionsSp.Children.Add(starBtn);
		actionsSp.Children.Add(new FontIcon { Glyph = "\uE76C", FontSize = 11, Foreground = new SolidColorBrush(Color.FromArgb(255, 148, 163, 184)) });

		Grid.SetColumn(actionsSp, 2);
		rowGrid.Children.Add(actionsSp);

		row.Child = rowGrid;

		// Click to select
		row.Tapped += (s, e) => SelectModel(prod);

		return row;
	}

	private FrameworkElement CreateDetailAndCurvePanel()
	{
		var panelBorder = new Border
		{
			Background = new SolidColorBrush(Color.FromArgb(220, 255, 255, 255)),
			BorderBrush = new SolidColorBrush(Color.FromArgb(255, 226, 232, 240)),
			BorderThickness = new Thickness(1),
			CornerRadius = new CornerRadius(12),
			Padding = new Thickness(16, 14, 16, 14)
		};

		var mainGrid = new Grid();

		// Empty State
		_emptyDetailPanel = new Grid { VerticalAlignment = VerticalAlignment.Center, HorizontalAlignment = HorizontalAlignment.Center };
		var emptySp = new StackPanel { Spacing = 10, HorizontalAlignment = HorizontalAlignment.Center };
		emptySp.Children.Add(new FontIcon { Glyph = "\uE7F5", FontSize = 36, Foreground = new SolidColorBrush(Color.FromArgb(255, 203, 213, 225)), HorizontalAlignment = HorizontalAlignment.Center });
		emptySp.Children.Add(new TextBlock { Text = "未选择预设", FontSize = 14, FontWeight = FontWeights.SemiBold, Foreground = new SolidColorBrush(Color.FromArgb(255, 15, 23, 42)), TextAlignment = TextAlignment.Center });
		emptySp.Children.Add(new TextBlock { Text = "从左侧选择耳机型号，查看并应用校正曲线。", FontSize = 12, Foreground = new SolidColorBrush(Color.FromArgb(255, 100, 116, 139)), TextAlignment = TextAlignment.Center });
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
		_detailVendorTb = new TextBlock { Text = "Sennheiser", FontSize = 11, Foreground = new SolidColorBrush(Color.FromArgb(255, 100, 116, 139)) };
		_detailProductTb = new TextBlock { Text = "HD 650", FontSize = 16, FontWeight = FontWeights.Bold, Foreground = new SolidColorBrush(Color.FromArgb(255, 15, 23, 42)) };
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
			Background = new SolidColorBrush(Color.FromArgb(255, 241, 245, 249)),
			BorderThickness = new Thickness(0),
			Content = new FontIcon { Glyph = "\uE734", FontSize = 13, Foreground = new SolidColorBrush(Color.FromArgb(255, 100, 116, 139)) }
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
					fi.Foreground = new SolidColorBrush(nowFav ? Color.FromArgb(255, 234, 179, 8) : Color.FromArgb(255, 100, 116, 139));
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

		curveHeadGrid.Children.Add(new TextBlock { Text = "EQ 频率响应", FontSize = 12, FontWeight = FontWeights.SemiBold, Foreground = new SolidColorBrush(Color.FromArgb(255, 71, 85, 105)) });
		_curveAuthorTb = new TextBlock { Text = "Oratory1990", FontSize = 11, Foreground = new SolidColorBrush(Color.FromArgb(255, 148, 163, 184)) };
		Grid.SetColumn(_curveAuthorTb, 1);
		curveHeadGrid.Children.Add(_curveAuthorTb);
		curveSec.Children.Add(curveHeadGrid);

		var curveBorder = new Border
		{
			Height = 170,
			CornerRadius = new CornerRadius(8),
			Background = new SolidColorBrush(Color.FromArgb(255, 248, 250, 252)),
			BorderBrush = new SolidColorBrush(Color.FromArgb(255, 226, 232, 240)),
			BorderThickness = new Thickness(1)
		};
		_curveControl = new HeadphoneCurveControl();
		curveBorder.Child = _curveControl;
		curveSec.Children.Add(curveBorder);
		detailSp.Children.Add(curveSec);

		// 3. Correction Source Selector
		var srcSec = new StackPanel { Spacing = 6 };
		srcSec.Children.Add(new TextBlock { Text = "校正来源", FontSize = 12, FontWeight = FontWeights.SemiBold, Foreground = new SolidColorBrush(Color.FromArgb(255, 71, 85, 105)) });
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
			Background = new SolidColorBrush(Color.FromArgb(255, 99, 102, 241)),
			Foreground = new SolidColorBrush(Colors.White)
		};
		_modeOriginalBtn.Click += (s, e) => SetCalibrationMode("original");

		_modeListeningBtn = new Button
		{
			Content = "听感调整",
			FontSize = 11,
			FontWeight = FontWeights.Medium,
			Padding = new Thickness(10, 4, 10, 4),
			CornerRadius = new CornerRadius(6),
			Background = new SolidColorBrush(Color.FromArgb(255, 241, 245, 249)),
			Foreground = new SolidColorBrush(Color.FromArgb(255, 71, 85, 105))
		};
		_modeListeningBtn.Click += (s, e) => SetCalibrationMode("listening");

		modeSwitchSp.Children.Add(_modeOriginalBtn);
		modeSwitchSp.Children.Add(_modeListeningBtn);
		modeSec.Children.Add(modeSwitchSp);

		_modeDescTb = new TextBlock
		{
			Text = "保留作者频段与前级。校正独立运行，可与个人 EQ 叠加。",
			FontSize = 10,
			Foreground = new SolidColorBrush(Color.FromArgb(255, 148, 163, 184)),
			TextWrapping = TextWrapping.Wrap
		};
		modeSec.Children.Add(_modeDescTb);
		detailSp.Children.Add(modeSec);

		// 5. Preamp Control
		var preampSec = new StackPanel { Spacing = 4 };
		var preampHead = new Grid();
		preampHead.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.0, GridUnitType.Star) });
		preampHead.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
		preampHead.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

		preampHead.Children.Add(new TextBlock { Text = "前级", FontSize = 12, FontWeight = FontWeights.SemiBold, Foreground = new SolidColorBrush(Color.FromArgb(255, 71, 85, 105)) });

		_hpPreampValueTb = new TextBlock
		{
			Text = "-6.4 dB",
			FontSize = 12,
			FontWeight = FontWeights.SemiBold,
			Foreground = new SolidColorBrush(Color.FromArgb(255, 99, 102, 241)),
			Margin = new Thickness(0, 0, 8, 0)
		};
		Grid.SetColumn(_hpPreampValueTb, 1);
		preampHead.Children.Add(_hpPreampValueTb);

		var restoreBtn = new Button
		{
			Content = "恢复默认",
			FontSize = 10,
			Padding = new Thickness(6, 2, 6, 2),
			CornerRadius = new CornerRadius(4),
			Background = new SolidColorBrush(Color.FromArgb(255, 241, 245, 249)),
			BorderThickness = new Thickness(0)
		};
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
		m1Sp.Children.Add(new TextBlock { Text = "OPRA 滤波器:", FontSize = 11, Foreground = new SolidColorBrush(Color.FromArgb(255, 148, 163, 184)) });
		_metricsFiltersTb = new TextBlock { Text = "6/6", FontSize = 11, FontWeight = FontWeights.SemiBold, Foreground = new SolidColorBrush(Color.FromArgb(255, 15, 23, 42)) };
		m1Sp.Children.Add(_metricsFiltersTb);
		Grid.SetColumn(m1Sp, 0);
		metricsGrid.Children.Add(m1Sp);

		var m2Sp = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 6, HorizontalAlignment = HorizontalAlignment.Right };
		m2Sp.Children.Add(new TextBlock { Text = "调整:", FontSize = 11, Foreground = new SolidColorBrush(Color.FromArgb(255, 148, 163, 184)) });
		_metricsAdjustedTb = new TextBlock { Text = "0", FontSize = 11, FontWeight = FontWeights.SemiBold, Foreground = new SolidColorBrush(Color.FromArgb(255, 15, 23, 42)) };
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
			fi.Foreground = new SolidColorBrush(isFav ? Color.FromArgb(255, 234, 179, 8) : Color.FromArgb(255, 100, 116, 139));
		}

		// Populate source buttons
		if (_sourcesStack != null)
		{
			_sourcesStack.Children.Clear();
			for (int i = 0; i < prod.Eqs.Count; i++)
			{
				var eq = prod.Eqs[i];
				bool isSelected = (_currentPreset != null && _currentPreset.Id == eq.Id) || (i == 0 && _currentPreset == null);
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
		var btn = new Button
		{
			HorizontalAlignment = HorizontalAlignment.Stretch,
			HorizontalContentAlignment = HorizontalAlignment.Stretch,
			Padding = new Thickness(10, 6, 10, 6),
			CornerRadius = new CornerRadius(6),
			Background = isSelected
				? new SolidColorBrush(Color.FromArgb(255, 238, 242, 255))
				: new SolidColorBrush(Color.FromArgb(180, 255, 255, 255)),
			BorderBrush = isSelected
				? new SolidColorBrush(Color.FromArgb(255, 129, 140, 248))
				: new SolidColorBrush(Color.FromArgb(255, 226, 232, 240)),
			BorderThickness = new Thickness(1)
		};

		var g = new Grid();
		g.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.0, GridUnitType.Star) });
		g.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

		var sp = new StackPanel { Spacing = 2 };
		sp.Children.Add(new TextBlock
		{
			Text = eq.MeasurementSource,
			FontSize = 12,
			FontWeight = FontWeights.SemiBold,
			Foreground = new SolidColorBrush(isSelected ? Color.FromArgb(255, 79, 70, 229) : Color.FromArgb(255, 15, 23, 42))
		});
		sp.Children.Add(new TextBlock
		{
			Text = $"{eq.TargetCurve} · {eq.Filters.Count} 个 OPRA 滤波器",
			FontSize = 10,
			Foreground = new SolidColorBrush(Color.FromArgb(255, 148, 163, 184))
		});
		Grid.SetColumn(sp, 0);
		g.Children.Add(sp);

		if (isSelected)
		{
			var check = new FontIcon
			{
				Glyph = "\uE73E",
				FontSize = 11,
				Foreground = new SolidColorBrush(Color.FromArgb(255, 79, 70, 229)),
				VerticalAlignment = VerticalAlignment.Center
			};
			Grid.SetColumn(check, 1);
			g.Children.Add(check);
		}

		btn.Content = g;
		btn.Click += (s, e) => SelectPreset(eq);
		return btn;
	}

	private void SelectPreset(OpraHeadphonePreset preset)
	{
		_currentPreset = preset;
		_userPreampDb = preset.PreampDb;

		if (_curveAuthorTb != null) _curveAuthorTb.Text = preset.MeasurementSource;
		if (_hpPreampSlider != null) _hpPreampSlider.Value = preset.PreampDb;
		if (_metricsFiltersTb != null) _metricsFiltersTb.Text = $"{preset.Filters.Count}/{preset.Filters.Count}";
		if (_metricsAdjustedTb != null) _metricsAdjustedTb.Text = "0";

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

	private void SetCalibrationMode(string mode)
	{
		_calibrationMode = mode;
		bool isOrig = mode == "original";

		if (_modeOriginalBtn != null)
		{
			_modeOriginalBtn.Background = isOrig ? new SolidColorBrush(Color.FromArgb(255, 99, 102, 241)) : new SolidColorBrush(Color.FromArgb(255, 241, 245, 249));
			_modeOriginalBtn.Foreground = isOrig ? new SolidColorBrush(Colors.White) : new SolidColorBrush(Color.FromArgb(255, 71, 85, 105));
		}
		if (_modeListeningBtn != null)
		{
			_modeListeningBtn.Background = !isOrig ? new SolidColorBrush(Color.FromArgb(255, 99, 102, 241)) : new SolidColorBrush(Color.FromArgb(255, 241, 245, 249));
			_modeListeningBtn.Foreground = !isOrig ? new SolidColorBrush(Colors.White) : new SolidColorBrush(Color.FromArgb(255, 71, 85, 105));
		}
		if (_modeDescTb != null)
		{
			_modeDescTb.Text = isOrig
				? "保留作者频段与前级。校正独立运行，可与个人 EQ 叠加。"
				: "可选调整：减轻宽中频下切、收回部分提升并重算前级，偏离作者曲线。";
		}

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
		var container = new Grid { RowSpacing = 10 };
		container.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
		container.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1.0, GridUnitType.Star) });

		var headSp = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8, Margin = new Thickness(4, 2, 0, 4) };
		headSp.Children.Add(new FontIcon { Glyph = "\uE734", FontSize = 14, Foreground = new SolidColorBrush(Color.FromArgb(255, 234, 179, 8)) });
		headSp.Children.Add(new TextBlock { Text = "收藏型号", FontSize = 14, FontWeight = FontWeights.Bold });
		headSp.Children.Add(new TextBlock { Text = $"({_favorites.Count} 款)", FontSize = 12, Foreground = new SolidColorBrush(Color.FromArgb(255, 148, 163, 184)) });
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
				Foreground = new SolidColorBrush(Color.FromArgb(255, 148, 163, 184)),
				Margin = new Thickness(8, 24, 0, 0)
			});
		}

		sv.Content = sp;
		Grid.SetRow(sv, 1);
		container.Children.Add(sv);

		_hcContentHost.Children.Add(container);
	}

	private void ShowRecentListView()
	{
		if (_hcContentHost == null) return;
		var container = new Grid { RowSpacing = 10 };
		container.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
		container.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1.0, GridUnitType.Star) });

		var headSp = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8, Margin = new Thickness(4, 2, 0, 4) };
		headSp.Children.Add(new FontIcon { Glyph = "\uE81C", FontSize = 14, Foreground = new SolidColorBrush(Color.FromArgb(255, 99, 102, 241)) });
		headSp.Children.Add(new TextBlock { Text = "最近使用", FontSize = 14, FontWeight = FontWeights.Bold });
		headSp.Children.Add(new TextBlock { Text = $"({_recents.Count} 款)", FontSize = 12, Foreground = new SolidColorBrush(Color.FromArgb(255, 148, 163, 184)) });
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
				Foreground = new SolidColorBrush(Color.FromArgb(255, 148, 163, 184)),
				Margin = new Thickness(8, 24, 0, 0)
			});
		}

		sv.Content = sp;
		Grid.SetRow(sv, 1);
		container.Children.Add(sv);

		_hcContentHost.Children.Add(container);
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
		_hcContentHost.Children.Clear();

		var container = new Grid { RowSpacing = 8 };
		container.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
		container.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1.0, GridUnitType.Star) });

		var head = new TextBlock
		{
			Text = $"搜索结果: “{_searchQuery}”",
			FontSize = 13,
			FontWeight = FontWeights.SemiBold,
			Foreground = new SolidColorBrush(Color.FromArgb(255, 71, 85, 105)),
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
				Foreground = new SolidColorBrush(Color.FromArgb(255, 148, 163, 184)),
				Margin = new Thickness(8, 20, 0, 0)
			});
		}

		sv.Content = sp;
		Grid.SetRow(sv, 1);
		container.Children.Add(sv);

		_hcContentHost.Children.Add(container);
	}

	private void SwitchSource(string source)
	{
		_activeSource = source;
		bool isOpra = source == "opra";

		if (_hcSourceOpraBtn != null)
		{
			_hcSourceOpraBtn.Background = isOpra ? new SolidColorBrush(Color.FromArgb(255, 30, 41, 59)) : new SolidColorBrush(Colors.Transparent);
			_hcSourceOpraBtn.Foreground = isOpra ? new SolidColorBrush(Colors.White) : new SolidColorBrush(Color.FromArgb(255, 100, 116, 139));
		}
		if (_hcSourceAutoEqBtn != null)
		{
			_hcSourceAutoEqBtn.Background = !isOpra ? new SolidColorBrush(Color.FromArgb(255, 30, 41, 59)) : new SolidColorBrush(Colors.Transparent);
			_hcSourceAutoEqBtn.Foreground = !isOpra ? new SolidColorBrush(Colors.White) : new SolidColorBrush(Color.FromArgb(255, 100, 116, 139));
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
			_btnBreadcrumbVendor.Foreground = new SolidColorBrush(Color.FromArgb(255, 37, 99, 235));
			_btnBreadcrumbVendor.FontWeight = FontWeights.SemiBold;
		}
		if (_btnBreadcrumbModel != null)
		{
			_btnBreadcrumbModel.Foreground = inVendor
				? new SolidColorBrush(Color.FromArgb(255, 37, 99, 235))
				: new SolidColorBrush(Color.FromArgb(255, 148, 163, 184));
			_btnBreadcrumbModel.FontWeight = inVendor ? FontWeights.SemiBold : FontWeights.Normal;
		}
		if (_btnBreadcrumbSource != null)
		{
			_btnBreadcrumbSource.Foreground = inModel
				? new SolidColorBrush(Color.FromArgb(255, 37, 99, 235))
				: new SolidColorBrush(Color.FromArgb(255, 148, 163, 184));
			_btnBreadcrumbSource.FontWeight = inModel ? FontWeights.SemiBold : FontWeights.Normal;
		}
	}

	private void UpdateTabButtonStyles()
	{
		SetTabActive(_btnTabCatalog, _activeTab == "catalog");
		SetTabActive(_btnTabFavorites, _activeTab == "favorites");
		SetTabActive(_btnTabRecent, _activeTab == "recent");
	}

	private static void SetTabActive(Button? btn, bool active)
	{
		if (btn == null) return;
		btn.Background = active ? new SolidColorBrush(Color.FromArgb(255, 238, 242, 255)) : new SolidColorBrush(Colors.Transparent);
		btn.Foreground = active ? new SolidColorBrush(Color.FromArgb(255, 79, 70, 229)) : new SolidColorBrush(Color.FromArgb(255, 100, 116, 139));
		btn.FontWeight = active ? FontWeights.SemiBold : FontWeights.Normal;
	}

	private void UpdateFavoritesAndRecentsBadges()
	{
		if (_tabFavoritesCountTb != null) _tabFavoritesCountTb.Text = _favorites.Count.ToString();
		if (_tabFavoritesBadge != null) _tabFavoritesBadge.Visibility = _favorites.Count > 0 ? Visibility.Visible : Visibility.Collapsed;

		if (_tabRecentCountTb != null) _tabRecentCountTb.Text = _recents.Count.ToString();
		if (_tabRecentBadge != null) _tabRecentBadge.Visibility = _recents.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
	}

	private void UpdateEngineStatusPills()
	{
		if (_pillModulesTb != null)
		{
			int activeModules = 1;
			if (_isCalibrationEnabled) activeModules++;
			if (ViewModel?.EchoHeadroomEnabled == true) activeModules++;
			if (ViewModel?.EchoEqEnabled == true) activeModules++;
			if (ViewModel?.SpatialWidth > 0.0) activeModules++;
			_pillModulesTb.Text = $"{activeModules} 个模块启用";
		}
		if (_pillStatusTb != null)
		{
			_pillStatusTb.Text = "就绪";
		}
	}

	// ----------------------------------------------------
	// HELPERS & FACTORIES
	// ----------------------------------------------------

	private static Button CreateSourceButton(string text, bool isSelected)
	{
		return new Button
		{
			Content = text,
			FontSize = 11,
			FontWeight = FontWeights.SemiBold,
			Padding = new Thickness(14, 4, 14, 4),
			CornerRadius = new CornerRadius(14),
			Background = isSelected ? new SolidColorBrush(Color.FromArgb(255, 30, 41, 59)) : new SolidColorBrush(Colors.Transparent),
			Foreground = isSelected ? new SolidColorBrush(Colors.White) : new SolidColorBrush(Color.FromArgb(255, 100, 116, 139)),
			BorderThickness = new Thickness(0)
		};
	}

	private static Button CreateBreadcrumbButton(string glyph, string text, bool isActive)
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
		btn.Foreground = isActive ? new SolidColorBrush(Color.FromArgb(255, 37, 99, 235)) : new SolidColorBrush(Color.FromArgb(255, 148, 163, 184));
		btn.FontWeight = isActive ? FontWeights.SemiBold : FontWeights.Normal;
		return btn;
	}

	private static Button CreateCategoryTabButton(string glyph, string text, bool isActive)
	{
		return CreateCategoryTabButton(glyph, text, isActive, out _, out _);
	}

	private static Button CreateCategoryTabButton(string glyph, string text, bool isActive, out Border badgeBorder, out TextBlock badgeCountTb)
	{
		var btn = new Button
		{
			Padding = new Thickness(12, 6, 12, 6),
			CornerRadius = new CornerRadius(14),
			Background = isActive ? new SolidColorBrush(Color.FromArgb(255, 238, 242, 255)) : new SolidColorBrush(Colors.Transparent),
			BorderThickness = new Thickness(0),
			Foreground = isActive ? new SolidColorBrush(Color.FromArgb(255, 79, 70, 229)) : new SolidColorBrush(Color.FromArgb(255, 100, 116, 139)),
			FontWeight = isActive ? FontWeights.SemiBold : FontWeights.Normal
		};
		var sp = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 6 };
		sp.Children.Add(new FontIcon { Glyph = glyph, FontSize = 13 });
		sp.Children.Add(new TextBlock { Text = text, FontSize = 12 });

		badgeBorder = new Border
		{
			Background = new SolidColorBrush(Color.FromArgb(255, 224, 231, 255)),
			CornerRadius = new CornerRadius(8),
			Padding = new Thickness(6, 1, 6, 1),
			Visibility = Visibility.Collapsed
		};
		badgeCountTb = new TextBlock { Text = "0", FontSize = 10, Foreground = new SolidColorBrush(Color.FromArgb(255, 79, 70, 229)) };
		badgeBorder.Child = badgeCountTb;
		sp.Children.Add(badgeBorder);

		btn.Content = sp;
		return btn;
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
	private readonly List<EchoBiquadFilter> _filters = new();

	public HeadphoneCurveControl()
	{
		_canvas = new SKXamlCanvas();
		_canvas.PaintSurface += OnPaintSurface;
		Content = _canvas;
		MinHeight = 160;
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
		canvas.Clear(new SKColor(248, 250, 252));

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
			Color = new SKColor(226, 232, 240),
			StrokeWidth = 1f,
			Style = SKPaintStyle.Stroke
		};
		using var zeroLinePaint = new SKPaint
		{
			Color = new SKColor(203, 213, 225),
			StrokeWidth = 1.2f,
			Style = SKPaintStyle.Stroke
		};
		using var textPaint = new SKPaint
		{
			Color = new SKColor(148, 163, 184),
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
				Color = new SKColor(148, 163, 184, 180),
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
			Color = new SKColor(99, 102, 241, 35),
			Style = SKPaintStyle.Fill,
			IsAntialias = true
		};
		canvas.DrawPath(fillPath, fillPaint);

		// Draw active curve stroke
		using var curvePaint = new SKPaint
		{
			Color = new SKColor(99, 102, 241),
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
