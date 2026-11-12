using System;
using System.Collections.Generic;
using FurinaPlayer.UI.ViewModels;
using Microsoft.UI;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using SonicWave.Audio.Dsp;
using Windows.Foundation;

namespace FurinaPlayer.UI.Pages;

public sealed partial class DspPage
{
	private FrameworkElement? _srcUpsamplingWorkbench;
	private Canvas? _srcFilterCurveCanvas;
	private TextBlock? _srcFilterNameText;
	private TextBlock? _srcPhaseBadgeText;
	private TextBlock? _srcFilterCaptionText;
	private TextBlock? _srcFilterPlan1xName;
	private TextBlock? _srcFilterPlan1xDesc;
	private TextBlock? _srcFilterPlan1xPhase;
	private TextBlock? _srcFilterPlanNxName;
	private TextBlock? _srcFilterPlanNxDesc;
	private TextBlock? _srcFilterPlanNxPhase;
	private Border? _srcFilterPlan1xSlot;
	private Border? _srcFilterPlanNxSlot;

	// Hero Readouts
	private Ellipse? _srcStatusDot;
	private TextBlock? _srcStatusText;
	private TextBlock? _srcSourceRateText;
	private TextBlock? _srcTargetRateText;
	private TextBlock? _srcRatioBadge;
	private TextBlock? _srcPrecisionValue;
	private TextBlock? _srcFilterSlotValue;
	private TextBlock? _srcDitherSlotValue;

	// 4-Stage Step Bar
	private TextBlock? _srcStep1Val;
	private TextBlock? _srcStep2Val;
	private TextBlock? _srcStep3Val;
	private TextBlock? _srcStep4Val;

	// Hint Texts
	private TextBlock? _srcModeHint;
	private TextBlock? _srcQualityHint;
	private TextBlock? _srcFilter1xHint;
	private TextBlock? _srcFilterNxHint;
	private TextBlock? _srcComputeHint;
	private TextBlock? _srcDitherHint;

	// Interactive Controls
	private readonly List<Button> _srcModeButtons = new();
	private readonly List<Button> _srcQualityButtons = new();
	private ComboBox? _srcFilter1xCombo;
	private ComboBox? _srcFilterNxCombo;
	private ComboBox? _srcComputeCombo;
	private ComboBox? _srcDitherCombo;
	private CheckBox? _srcAbBenchmarkCheck;
	private FrameworkElement? _srcAdvancedPanel;
	private FontIcon? _srcAdvancedChevron;

	public FrameworkElement CreateSrcUpsamplingWorkbench()
	{
		if (_srcUpsamplingWorkbench != null)
		{
			RefreshSrcUpsamplingState();
			return _srcUpsamplingWorkbench;
		}

		var sv = new ScrollViewer
		{
			VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
			HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled
		};

		var root = new Grid
		{
			Background = CreateWhiteAcrylicBrush(0.72, 195),
			Padding = new Thickness(24, 20, 24, 20),
			RowSpacing = 16
		};

		root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // 0: Top Header
		root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // 1: Hero Route & Precision Card
		root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // 2: Control Deck Card (Modes, Quality, Combos)
		root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // 3: Filter Design Preview Card
		root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // 4: Bottom Disclaimer Note

		// --- Row 0: Top Header & Status Indicators ---
		root.Children.Add(CreateSrcHeaderBar());

		// --- Row 1: Hero Route & Precision Card ---
		var heroCard = CreateSrcHeroCard();
		Grid.SetRow(heroCard, 1);
		root.Children.Add(heroCard);

		// --- Row 2: Control Deck Card ---
		var controlDeck = CreateSrcControlDeck();
		Grid.SetRow(controlDeck, 2);
		root.Children.Add(controlDeck);

		// --- Row 3: Filter Design Preview Card ---
		var filterPreview = CreateSrcFilterPreviewCard();
		Grid.SetRow(filterPreview, 3);
		root.Children.Add(filterPreview);

		// --- Row 4: Bottom Disclaimer Note ---
		var bottomNote = new TextBlock
		{
			Text = "只处理 PCM，共享输出、DSD 输出或 HQPlayer 协议时不会叠加热转。",
			FontSize = 11,
			Foreground = DspTextMutedBrush,
			Margin = new Thickness(4, 4, 4, 12)
		};
		Grid.SetRow(bottomNote, 4);
		root.Children.Add(bottomNote);

		sv.Content = root;
		_srcUpsamplingWorkbench = sv;

		RefreshSrcUpsamplingState();
		UpdateFilterDesignCurve();

		return _srcUpsamplingWorkbench;
	}

	private FrameworkElement CreateSrcHeaderBar()
	{
		var headerGrid = new Grid();
		headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.0, GridUnitType.Star) });
		headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

		// Left Title & Status
		var leftStack = new StackPanel { Spacing = 4 };
		var titleRow = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 10, VerticalAlignment = VerticalAlignment.Center };

		var iconBorder = new Border
		{
			Width = 34,
			Height = 34,
			CornerRadius = new CornerRadius(17),
			Background = DspAccentSoftBgBrush,
			Child = new FontIcon
			{
				Glyph = "\uE774", // Audio / broadcast icon
				FontSize = 16,
				Foreground = DspAccentSoftTextBrush,
				HorizontalAlignment = HorizontalAlignment.Center,
				VerticalAlignment = VerticalAlignment.Center
			}
		};
		titleRow.Children.Add(iconBorder);
		titleRow.Children.Add(new TextBlock
		{
			Text = "FurinaPlayer SRC / 升频",
			FontSize = 20,
			FontWeight = FontWeights.Bold,
			Foreground = DspTextPrimaryBrush,
			VerticalAlignment = VerticalAlignment.Center
		});
		leftStack.Children.Add(titleRow);

		// Native Pass-through / Active status dot
		var statusRow = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 6, Margin = new Thickness(44, 0, 0, 0) };
		_srcStatusDot = new Ellipse
		{
			Width = 7,
			Height = 7,
			Fill = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 16, 185, 129)), // Green
			VerticalAlignment = VerticalAlignment.Center
		};
		_srcStatusText = new TextBlock
		{
			Text = "原生直通",
			FontSize = 12,
			FontWeight = FontWeights.Medium,
			Foreground = DspTextMutedBrush,
			VerticalAlignment = VerticalAlignment.Center
		};
		statusRow.Children.Add(_srcStatusDot);
		statusRow.Children.Add(_srcStatusText);
		leftStack.Children.Add(statusRow);

		Grid.SetColumn(leftStack, 0);
		headerGrid.Children.Add(leftStack);

		// Right Actions: A/B Benchmark CheckBox
		var rightStack = new StackPanel { Spacing = 8, HorizontalAlignment = HorizontalAlignment.Right };

		// Sub-row: A/B Benchmark CheckBox
		var subActionRow = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 14, HorizontalAlignment = HorizontalAlignment.Right };
		_srcAbBenchmarkCheck = new CheckBox
		{
			Content = "A/B SOXR 基准",
			FontSize = 12,
			IsChecked = ViewModel?.FurinaSrcAbBenchmark ?? false
		};
		_srcAbBenchmarkCheck.Checked += (s, e) =>
		{
			if (ViewModel != null)
			{
				ViewModel.FurinaSrcAbBenchmark = true;
				ViewModel.SaveEchoSettings();
				RefreshSrcUpsamplingState();
			}
		};
		_srcAbBenchmarkCheck.Unchecked += (s, e) =>
		{
			if (ViewModel != null)
			{
				ViewModel.FurinaSrcAbBenchmark = false;
				ViewModel.SaveEchoSettings();
				RefreshSrcUpsamplingState();
			}
		};

		subActionRow.Children.Add(_srcAbBenchmarkCheck);
		rightStack.Children.Add(subActionRow);

		Grid.SetColumn(rightStack, 1);
		headerGrid.Children.Add(rightStack);

		return headerGrid;
	}

	private FrameworkElement CreateSrcHeroCard()
	{
		var card = new Border
		{
			Background = CreateAcrylicCardBrush(0.88, 235),
			BorderBrush = CreateLiquidGlassRimBrush(),
			BorderThickness = new Thickness(1),
			CornerRadius = new CornerRadius(12),
			Padding = new Thickness(22, 18, 22, 18)
		};

		var mainGrid = new Grid();
		mainGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.0, GridUnitType.Star) });
		mainGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(340, GridUnitType.Pixel) });

		// --- Left Section: Route Path & 4-Stage Step Bar ---
		var leftStack = new StackPanel { Spacing = 20, Margin = new Thickness(0, 0, 32, 0) };

		// Route Title
		var routeTitleSp = new StackPanel { Spacing = 8 };
		routeTitleSp.Children.Add(new TextBlock
		{
			Text = "路径",
			FontSize = 11,
			FontWeight = FontWeights.Bold,
			Foreground = DspTextMutedBrush
		});

		// Sample Rate Transform Display
		var rateGrid = new Grid();
		rateGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
		rateGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(56, GridUnitType.Pixel) });
		rateGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
		rateGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

		// Source Rate
		var srcSp = new StackPanel { Spacing = 2 };
		srcSp.Children.Add(new TextBlock { Text = "源采样率", FontSize = 11, Foreground = DspTextMutedBrush });
		_srcSourceRateText = new TextBlock
		{
			Text = "--",
			FontSize = 26,
			FontWeight = FontWeights.Normal,
			Foreground = DspTextSecondaryBrush
		};
		srcSp.Children.Add(_srcSourceRateText);
		Grid.SetColumn(srcSp, 0);
		rateGrid.Children.Add(srcSp);

		// Arrow
		var arrowSp = new StackPanel { VerticalAlignment = VerticalAlignment.Center, HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(8, 12, 8, 0) };
		var arrowIcon = new TextBlock
		{
			Text = "──▶",
			FontSize = 16,
			FontWeight = FontWeights.Bold,
			Foreground = DspTextMutedBrush,
			HorizontalAlignment = HorizontalAlignment.Center
		};
		arrowSp.Children.Add(arrowIcon);
		Grid.SetColumn(arrowSp, 1);
		rateGrid.Children.Add(arrowSp);

		// Target Rate
		var dstSp = new StackPanel { Spacing = 2 };
		dstSp.Children.Add(new TextBlock { Text = "目标采样率", FontSize = 11, Foreground = DspTextMutedBrush });
		_srcTargetRateText = new TextBlock
		{
			Text = "--",
			FontSize = 36,
			FontWeight = FontWeights.Light,
			Foreground = DspTextPrimaryBrush
		};
		dstSp.Children.Add(_srcTargetRateText);
		Grid.SetColumn(dstSp, 2);
		rateGrid.Children.Add(dstSp);

		// Multiplier Badge
		_srcRatioBadge = new TextBlock
		{
			Text = "",
			FontSize = 16,
			FontWeight = FontWeights.SemiBold,
			Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 37, 99, 235)),
			FontFamily = new FontFamily("Consolas, Segoe UI"),
			VerticalAlignment = VerticalAlignment.Bottom,
			Margin = new Thickness(14, 0, 0, 6)
		};
		Grid.SetColumn(_srcRatioBadge, 3);
		rateGrid.Children.Add(_srcRatioBadge);

		routeTitleSp.Children.Add(rateGrid);
		leftStack.Children.Add(routeTitleSp);

		// 4-Stage Step Bar
		var stepBarGrid = new Grid { ColumnSpacing = 16 };
		for (int i = 0; i < 4; i++)
		{
			stepBarGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.0, GridUnitType.Star) });
		}

		// Step 01
		stepBarGrid.Children.Add(CreateStepIndicator(0, "01 FurinaPlayer SRC 控制模式", "关闭", out _srcStep1Val, false));
		// Step 02
		stepBarGrid.Children.Add(CreateStepIndicator(1, "02 质量策略", "Transparent", out _srcStep2Val, false));
		// Step 03
		stepBarGrid.Children.Add(CreateStepIndicator(2, "03 倍率", "--", out _srcStep3Val, false));
		// Step 04
		stepBarGrid.Children.Add(CreateStepIndicator(3, "04 Compute", "CPU 实时路径,未请求 CUDA。", out _srcStep4Val, true));

		leftStack.Children.Add(stepBarGrid);
		Grid.SetColumn(leftStack, 0);
		mainGrid.Children.Add(leftStack);

		// --- Right Section: Precision & Stats ---
		var rightBorder = new Border
		{
			BorderBrush = CreateLiquidGlassRimBrush(),
			BorderThickness = new Thickness(1, 0, 0, 0),
			Padding = new Thickness(28, 0, 0, 0)
		};
		var rightStack = new StackPanel { Spacing = 14 };

		rightStack.Children.Add(new TextBlock
		{
			Text = "精度",
			FontSize = 11,
			FontWeight = FontWeights.Bold,
			Foreground = DspTextMutedBrush
		});

		_srcPrecisionValue = new TextBlock
		{
			Text = "SOXR precision 28",
			FontSize = 26,
			FontWeight = FontWeights.SemiBold,
			Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 37, 99, 235)),
			FontFamily = new FontFamily("Consolas, Segoe UI")
		};
		rightStack.Children.Add(_srcPrecisionValue);

		var statColsGrid = new Grid { ColumnSpacing = 20, Margin = new Thickness(0, 4, 0, 0) };
		statColsGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.0, GridUnitType.Star) });
		statColsGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.0, GridUnitType.Star) });

		// Filter Slot
		var slotSp = new StackPanel { Spacing = 2 };
		slotSp.Children.Add(new TextBlock { Text = "过滤器插槽", FontSize = 11, Foreground = DspTextMutedBrush });
		_srcFilterSlotValue = new TextBlock { Text = "Standby", FontSize = 13, FontWeight = FontWeights.SemiBold, Foreground = DspTextPrimaryBrush };
		slotSp.Children.Add(_srcFilterSlotValue);
		Grid.SetColumn(slotSp, 0);
		statColsGrid.Children.Add(slotSp);

		// PCM Dither
		var ditherSp = new StackPanel { Spacing = 2 };
		ditherSp.Children.Add(new TextBlock { Text = "PCM Dither / Noise Shaping", FontSize = 11, Foreground = DspTextMutedBrush });
		_srcDitherSlotValue = new TextBlock { Text = "关闭", FontSize = 13, FontWeight = FontWeights.SemiBold, Foreground = DspTextPrimaryBrush };
		ditherSp.Children.Add(_srcDitherSlotValue);
		Grid.SetColumn(ditherSp, 1);
		statColsGrid.Children.Add(ditherSp);

		rightStack.Children.Add(statColsGrid);
		rightBorder.Child = rightStack;

		Grid.SetColumn(rightBorder, 1);
		mainGrid.Children.Add(rightBorder);

		card.Child = mainGrid;
		return card;
	}

	private FrameworkElement CreateStepIndicator(int col, string label, string defaultValue, out TextBlock valBlock, bool isLast)
	{
		var sp = new StackPanel { Spacing = 4 };
		// Top Accent Bar
		var topBar = new Rectangle
		{
			Height = 2,
			Fill = new SolidColorBrush(isLast ? Windows.UI.Color.FromArgb(255, 16, 185, 129) : Windows.UI.Color.FromArgb(255, 59, 130, 246)),
			HorizontalAlignment = HorizontalAlignment.Stretch,
			Margin = new Thickness(0, 0, 0, 4)
		};
		sp.Children.Add(topBar);

		sp.Children.Add(new TextBlock
		{
			Text = label,
			FontSize = 11,
			Foreground = DspTextMutedBrush
		});

		valBlock = new TextBlock
		{
			Text = defaultValue,
			FontSize = 13,
			FontWeight = FontWeights.SemiBold,
			Foreground = DspTextPrimaryBrush,
			TextTrimming = TextTrimming.CharacterEllipsis
		};
		sp.Children.Add(valBlock);

		Grid.SetColumn(sp, col);
		return sp;
	}

	private FrameworkElement CreateSrcControlDeck()
	{
		var card = new Border
		{
			Background = CreateAcrylicCardBrush(0.88, 235),
			BorderBrush = CreateLiquidGlassRimBrush(),
			BorderThickness = new Thickness(1),
			CornerRadius = new CornerRadius(12),
			Padding = new Thickness(22, 16, 22, 18)
		};

		var stack = new StackPanel { Spacing = 16 };

		// Header Right: Advanced Toggle Button
		var topHeaderRow = new Grid();
		topHeaderRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.0, GridUnitType.Star) });
		topHeaderRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

		var btnToggleAdvanced = new HyperlinkButton
		{
			Padding = new Thickness(6, 2, 6, 2)
		};
		var advSp = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 5 };
		var advText = new TextBlock { Text = "高级", FontSize = 12, FontWeight = FontWeights.SemiBold, Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 37, 99, 235)) };
		_srcAdvancedChevron = new FontIcon { Glyph = "\uE70D", FontSize = 11, Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 37, 99, 235)) }; // Chevron Down
		advSp.Children.Add(advText);
		advSp.Children.Add(_srcAdvancedChevron);
		btnToggleAdvanced.Content = advSp;

		btnToggleAdvanced.Click += (s, e) =>
		{
			if (_srcAdvancedPanel != null)
			{
				bool isVisible = _srcAdvancedPanel.Visibility == Visibility.Visible;
				_srcAdvancedPanel.Visibility = isVisible ? Visibility.Collapsed : Visibility.Visible;
				_srcAdvancedChevron.Glyph = isVisible ? "\uE70D" : "\uE70E"; // Down vs Up
				if (ViewModel != null)
				{
					ViewModel.FurinaSrcAdvanced = !isVisible;
					ViewModel.SaveEchoSettings();
				}
			}
		};

		Grid.SetColumn(btnToggleAdvanced, 1);
		topHeaderRow.Children.Add(btnToggleAdvanced);
		stack.Children.Add(topHeaderRow);

		// --- Row 1: Segmented Button Groups (Mode & Quality Profile) ---
		var row1Grid = new Grid { ColumnSpacing = 24 };
		row1Grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.6, GridUnitType.Star) });
		row1Grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.0, GridUnitType.Star) });

		// Mode Segmented Group
		var modeStack = new StackPanel { Spacing = 8 };
		modeStack.Children.Add(new TextBlock
		{
			Text = "FurinaPlayer SRC 控制模式",
			FontSize = 12,
			FontWeight = FontWeights.Bold,
			Foreground = DspTextSubHeaderBrush
		});

		var modeOptions = new (string Key, string Label)[]
		{
			("off", "关闭"),
			("compatibility48", "48 kHz 兼容"),
			("family2x", "2x PCM"),
			("family4x", "4x PCM"),
			("family8x", "8x Ultra")
		};
		var modeSegBar = CreateSegmentedBar(modeOptions, ViewModel?.FurinaSrcMode ?? "off", _srcModeButtons, OnModeSelected);
		modeStack.Children.Add(modeSegBar);

		_srcModeHint = new TextBlock
		{
			Text = "保持源采样率，Bit-perfect 条件不受 FurinaPlayer SRC 影响。",
			FontSize = 11,
			Foreground = DspTextMutedBrush,
			Margin = new Thickness(2, 0, 0, 0)
		};
		modeStack.Children.Add(_srcModeHint);
		Grid.SetColumn(modeStack, 0);
		row1Grid.Children.Add(modeStack);

		// Quality Profile Segmented Group
		var qualityStack = new StackPanel { Spacing = 8 };
		qualityStack.Children.Add(new TextBlock
		{
			Text = "质量策略",
			FontSize = 12,
			FontWeight = FontWeights.Bold,
			Foreground = DspTextSubHeaderBrush
		});

		var qualityOptions = new (string Key, string Label)[]
		{
			("transparent", "Transparent"),
			("balanced", "Balanced"),
			("lowLatency", "Low latency")
		};
		var qualitySegBar = CreateSegmentedBar(qualityOptions, ViewModel?.FurinaSrcQuality ?? "transparent", _srcQualityButtons, OnQualitySelected);
		qualityStack.Children.Add(qualitySegBar);

		_srcQualityHint = new TextBlock
		{
			Text = "最高精度 SOXR，优先透明度和截断失真。",
			FontSize = 11,
			Foreground = DspTextMutedBrush,
			Margin = new Thickness(2, 0, 0, 0)
		};
		qualityStack.Children.Add(_srcQualityHint);
		Grid.SetColumn(qualityStack, 1);
		row1Grid.Children.Add(qualityStack);

		stack.Children.Add(row1Grid);

		// --- Row 2: 4 Dropdown Columns ---
		var row2Grid = new Grid { ColumnSpacing = 16 };
		for (int i = 0; i < 4; i++)
		{
			row2Grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.0, GridUnitType.Star) });
		}

		// Col 0: 过滤器 1x
		var col0 = new StackPanel { Spacing = 6 };
		col0.Children.Add(new TextBlock { Text = "过滤器 1x", FontSize = 12, FontWeight = FontWeights.SemiBold, Foreground = DspTextSubHeaderBrush });
		_srcFilter1xCombo = new ComboBox
		{
			HorizontalAlignment = HorizontalAlignment.Stretch,
			CornerRadius = new CornerRadius(8)
		};
		_srcFilter1xCombo.Items.Add("FurinaPlayer Gauss Long");
		_srcFilter1xCombo.Items.Add("FurinaPlayer Gauss Short");
		_srcFilter1xCombo.Items.Add("Linear Phase");
		_srcFilter1xCombo.Items.Add("Min Phase");
		_srcFilter1xCombo.Items.Add("FurinaPlayer Steep Short");
		_srcFilter1xCombo.Items.Add("FurinaPlayer Steep Long");
		_srcFilter1xCombo.SelectedItem = SelectMatchingItem(_srcFilter1xCombo, ViewModel?.FurinaSrcFilter1x, "FurinaPlayer Gauss Long");
		_srcFilter1xCombo.SelectionChanged += (s, e) =>
		{
			if (_srcFilter1xCombo.SelectedItem is string sel && ViewModel != null)
			{
				ViewModel.FurinaSrcFilter1x = sel;
				ViewModel.SaveEchoSettings();
				UpdateFilter1xHint(sel);
				UpdateFilterDesignCurve();
			}
		};
		col0.Children.Add(_srcFilter1xCombo);
		_srcFilter1xHint = new TextBlock
		{
			Text = "高阶量 sinc 滤波，听感更柔和，兼顾瞬态。",
			FontSize = 11,
			Foreground = DspTextMutedBrush,
			TextWrapping = TextWrapping.Wrap
		};
		col0.Children.Add(_srcFilter1xHint);
		Grid.SetColumn(col0, 0);
		row2Grid.Children.Add(col0);

		// Col 1: 过滤器 Nx
		var col1 = new StackPanel { Spacing = 6 };
		col1.Children.Add(new TextBlock { Text = "过滤器 Nx", FontSize = 12, FontWeight = FontWeights.SemiBold, Foreground = DspTextSubHeaderBrush });
		_srcFilterNxCombo = new ComboBox
		{
			HorizontalAlignment = HorizontalAlignment.Stretch,
			CornerRadius = new CornerRadius(8)
		};
		_srcFilterNxCombo.Items.Add("FurinaPlayer Halfband");
		_srcFilterNxCombo.Items.Add("Sinc Polyphase");
		_srcFilterNxCombo.Items.Add("Steep");
		_srcFilterNxCombo.Items.Add("FurinaPlayer Steep XL");
		_srcFilterNxCombo.SelectedItem = SelectMatchingItem(_srcFilterNxCombo, ViewModel?.FurinaSrcFilterNx, "FurinaPlayer Halfband");
		_srcFilterNxCombo.SelectionChanged += (s, e) =>
		{
			if (_srcFilterNxCombo.SelectedItem is string sel && ViewModel != null)
			{
				ViewModel.FurinaSrcFilterNx = sel;
				ViewModel.SaveEchoSettings();
				UpdateFilterNxHint(sel);
				UpdateFilterDesignCurve();
			}
		};
		col1.Children.Add(_srcFilterNxCombo);
		_srcFilterNxHint = new TextBlock
		{
			Text = "半带 sinc 滤波，适合 2x/4x 先倒频定实时性。",
			FontSize = 11,
			Foreground = DspTextMutedBrush,
			TextWrapping = TextWrapping.Wrap
		};
		col1.Children.Add(_srcFilterNxHint);
		Grid.SetColumn(col1, 1);
		row2Grid.Children.Add(col1);

		// Col 2: Compute
		var col2 = new StackPanel { Spacing = 6 };
		col2.Children.Add(new TextBlock { Text = "Compute", FontSize = 12, FontWeight = FontWeights.SemiBold, Foreground = DspTextSubHeaderBrush });
		_srcComputeCombo = new ComboBox
		{
			HorizontalAlignment = HorizontalAlignment.Stretch,
			CornerRadius = new CornerRadius(8)
		};
		_srcComputeCombo.Items.Add("CPU SIMD");
		_srcComputeCombo.Items.Add("CPU 多线程");
		_srcComputeCombo.Items.Add("GPU 加速 (若支持)");
		_srcComputeCombo.SelectedItem = SelectMatchingItem(_srcComputeCombo, ViewModel?.FurinaSrcCompute, "CPU SIMD");
		_srcComputeCombo.SelectionChanged += (s, e) =>
		{
			if (_srcComputeCombo.SelectedItem is string sel && ViewModel != null)
			{
				ViewModel.FurinaSrcCompute = sel;
				ViewModel.SaveEchoSettings();
				UpdateComputeHint(sel);
			}
		};
		col2.Children.Add(_srcComputeCombo);
		_srcComputeHint = new TextBlock
		{
			Text = "默认实时路径：极优，无降采样风险，并保证随机可用。",
			FontSize = 11,
			Foreground = DspTextMutedBrush,
			TextWrapping = TextWrapping.Wrap
		};
		col2.Children.Add(_srcComputeHint);
		Grid.SetColumn(col2, 2);
		row2Grid.Children.Add(col2);

		// Col 3: PCM Dither / Noise Shaping
		var col3 = new StackPanel { Spacing = 6 };
		col3.Children.Add(new TextBlock { Text = "PCM Dither / Noise Shaping", FontSize = 12, FontWeight = FontWeights.SemiBold, Foreground = DspTextSubHeaderBrush });
		_srcDitherCombo = new ComboBox
		{
			HorizontalAlignment = HorizontalAlignment.Stretch,
			CornerRadius = new CornerRadius(8)
		};
		_srcDitherCombo.Items.Add("Off");
		_srcDitherCombo.Items.Add("TPDF");
		_srcDitherCombo.Items.Add("High-Pass Noise Shaping");
		_srcDitherCombo.SelectedItem = SelectMatchingItem(_srcDitherCombo, ViewModel?.FurinaSrcDither, "Off");
		_srcDitherCombo.SelectionChanged += (s, e) =>
		{
			if (_srcDitherCombo.SelectedItem is string sel && ViewModel != null)
			{
				ViewModel.FurinaSrcDither = sel;
				ViewModel.SaveEchoSettings();
				UpdateDitherHint(sel);
				if (_srcDitherSlotValue != null)
				{
					_srcDitherSlotValue.Text = sel == "Off" ? "关闭" : sel;
				}
			}
		};
		col3.Children.Add(_srcDitherCombo);
		_srcDitherHint = new TextBlock
		{
			Text = "保持 Float32 PCM 链原图标：默认关闭，避免无意义加噪。",
			FontSize = 11,
			Foreground = DspTextMutedBrush,
			TextWrapping = TextWrapping.Wrap
		};
		col3.Children.Add(_srcDitherHint);
		Grid.SetColumn(col3, 3);
		row2Grid.Children.Add(col3);

		stack.Children.Add(row2Grid);

		// --- Advanced Ladder Collapsible Panel ---
		_srcAdvancedPanel = CreateAdvancedLadderPanel();
		_srcAdvancedPanel.Visibility = (ViewModel?.FurinaSrcAdvanced ?? false) ? Visibility.Visible : Visibility.Collapsed;
		stack.Children.Add(_srcAdvancedPanel);

		card.Child = stack;
		return card;
	}

	private FrameworkElement CreateAdvancedLadderPanel()
	{
		var border = new Border
		{
			BorderBrush = CreateLiquidGlassRimBrush(),
			BorderThickness = new Thickness(0, 1, 0, 0),
			Padding = new Thickness(0, 14, 0, 0),
			Margin = new Thickness(0, 4, 0, 0)
		};

		var stack = new StackPanel { Spacing = 10 };

		var titleRow = new Grid();
		titleRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.0, GridUnitType.Star) });
		titleRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

		var titleSp = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
		titleSp.Children.Add(new TextBlock { Text = "高级", FontSize = 12, FontWeight = FontWeights.Bold, Foreground = DspTextSubHeaderBrush });
		titleSp.Children.Add(new TextBlock { Text = "快速套用进阶调谐梯度", FontSize = 11, Foreground = DspTextMutedBrush, VerticalAlignment = VerticalAlignment.Center });
		Grid.SetColumn(titleSp, 0);
		titleRow.Children.Add(titleSp);

		var firPlannedText = new TextBlock { Text = "FIR planned", FontSize = 11, FontFamily = new FontFamily("Consolas, Segoe UI"), Foreground = DspTextMutedBrush };
		Grid.SetColumn(firPlannedText, 1);
		titleRow.Children.Add(firPlannedText);
		stack.Children.Add(titleRow);

		// 4 Ladder buttons
		var ladderGrid = new Grid { ColumnSpacing = 12 };
		for (int i = 0; i < 4; i++)
		{
			ladderGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.0, GridUnitType.Star) });
		}

		ladderGrid.Children.Add(CreateLadderCard(0, "Realtime Safe", "4x / Halfband 1x+Nx / CPU", "低延迟 / CPU", () =>
		{
			ApplyLadderPreset("family4x", "lowLatency", "FurinaPlayer Halfband", "FurinaPlayer Halfband", "CPU SIMD");
		}));

		ladderGrid.Children.Add(CreateLadderCard(1, "Hi-Fi Balanced", "4x / Gauss Long 1x / Halfband Nx / CPU", "中等延迟 / CPU", () =>
		{
			ApplyLadderPreset("family4x", "balanced", "FurinaPlayer Gauss Long", "FurinaPlayer Halfband", "CPU SIMD");
		}));

		ladderGrid.Children.Add(CreateLadderCard(2, "Reference", "8x / Gauss Long 1x / Steep Nx / CPU", "高延迟 / CPU", () =>
		{
			ApplyLadderPreset("family8x", "transparent", "FurinaPlayer Gauss Long", "Steep", "CPU SIMD");
		}));

		ladderGrid.Children.Add(CreateLadderCard(3, "Ultra Precision", "8x / Steep XL 1x / Sinc Polyphase Nx", "超高延迟 / CPU", () =>
		{
			ApplyLadderPreset("family8x", "transparent", "FurinaPlayer Steep Long", "FurinaPlayer Steep XL", "CPU SIMD");
		}));

		stack.Children.Add(ladderGrid);
		border.Child = stack;
		return border;
	}

	private FrameworkElement CreateLadderCard(int col, string title, string detail, string meta, Action onApply)
	{
		var btn = new Button
		{
			HorizontalAlignment = HorizontalAlignment.Stretch,
			HorizontalContentAlignment = HorizontalAlignment.Left,
			Padding = new Thickness(12, 10, 12, 10),
			CornerRadius = new CornerRadius(8),
			Background = DspSoftSurfaceBrush,
			BorderBrush = CreateLiquidGlassRimBrush(),
			BorderThickness = new Thickness(1)
		};

		var sp = new StackPanel { Spacing = 3 };
		sp.Children.Add(new TextBlock { Text = title, FontSize = 13, FontWeight = FontWeights.SemiBold, Foreground = DspTextPrimaryBrush });
		sp.Children.Add(new TextBlock { Text = detail, FontSize = 11, Foreground = DspTextMutedBrush, TextTrimming = TextTrimming.CharacterEllipsis });
		sp.Children.Add(new TextBlock { Text = meta, FontSize = 10.5, FontFamily = new FontFamily("Consolas, Segoe UI"), Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 37, 99, 235)) });

		btn.Content = sp;
		btn.Click += (s, e) => onApply();

		Grid.SetColumn(btn, col);
		return btn;
	}

	private void ApplyLadderPreset(string mode, string quality, string f1x, string fnx, string compute)
	{
		OnModeSelected(mode);
		OnQualitySelected(quality);
		if (_srcFilter1xCombo != null) _srcFilter1xCombo.SelectedItem = SelectMatchingItem(_srcFilter1xCombo, f1x, f1x);
		if (_srcFilterNxCombo != null) _srcFilterNxCombo.SelectedItem = SelectMatchingItem(_srcFilterNxCombo, fnx, fnx);
		if (_srcComputeCombo != null) _srcComputeCombo.SelectedItem = SelectMatchingItem(_srcComputeCombo, compute, compute);
		RefreshSrcUpsamplingState();
		UpdateFilterDesignCurve();
	}

	private FrameworkElement CreateSrcFilterPreviewCard()
	{
		var card = new Border
		{
			Background = CreateAcrylicCardBrush(0.88, 235),
			BorderBrush = CreateLiquidGlassRimBrush(),
			BorderThickness = new Thickness(1),
			CornerRadius = new CornerRadius(12),
			Padding = new Thickness(22, 18, 22, 18)
		};

		var mainGrid = new Grid();
		mainGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.0, GridUnitType.Star) });
		mainGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(280, GridUnitType.Pixel) });

		// --- Left Section: Chart Area & Header ---
		var leftStack = new StackPanel { Spacing = 10, Margin = new Thickness(0, 0, 32, 0) };

		// Preview Header
		var headGrid = new Grid();
		headGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.0, GridUnitType.Star) });
		headGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

		var titleSp = new StackPanel { Spacing = 2 };
		titleSp.Children.Add(new TextBlock
		{
			Text = "FILTER : DESIGN PREVIEW",
			FontSize = 11,
			FontWeight = FontWeights.Bold,
			Foreground = DspTextMutedBrush
		});
		_srcFilterNameText = new TextBlock
		{
			Text = "furina-gauss-long",
			FontSize = 18,
			FontWeight = FontWeights.SemiBold,
			Foreground = DspTextPrimaryBrush
		};
		titleSp.Children.Add(_srcFilterNameText);
		Grid.SetColumn(titleSp, 0);
		headGrid.Children.Add(titleSp);

		// Phase Pill Badge
		var phaseBadgeBorder = new Border
		{
			Background = DspSoftSurfaceBrush,
			BorderBrush = CreateLiquidGlassRimBrush(),
			BorderThickness = new Thickness(1),
			CornerRadius = new CornerRadius(6),
			Padding = new Thickness(8, 3, 8, 3),
			VerticalAlignment = VerticalAlignment.Center
		};
		_srcPhaseBadgeText = new TextBlock
		{
			Text = "Linear phase",
			FontSize = 11,
			FontFamily = new FontFamily("Consolas, Segoe UI"),
			Foreground = DspTextSubHeaderBrush
		};
		phaseBadgeBorder.Child = _srcPhaseBadgeText;
		Grid.SetColumn(phaseBadgeBorder, 1);
		headGrid.Children.Add(phaseBadgeBorder);

		leftStack.Children.Add(headGrid);

		// Chart Canvas & Viewbox
		var chartViewbox = new Viewbox
		{
			Stretch = Stretch.Uniform,
			Height = 180,
			HorizontalAlignment = HorizontalAlignment.Stretch
		};
		_srcFilterCurveCanvas = new Canvas
		{
			Width = 720,
			Height = 220
		};
		chartViewbox.Child = _srcFilterCurveCanvas;
		leftStack.Children.Add(chartViewbox);

		// Chart Footer Note
		_srcFilterCaptionText = new TextBlock
		{
			Text = "高阶量 sinc 滤波，听感更柔和，兼顾瞬态。44.1/48 kHz 音源的差异主要集中在最高频段，相位与瞬态响应：透明 SRC 本身就不应产生明显染色。",
			FontSize = 11,
			Foreground = DspTextMutedBrush,
			TextWrapping = TextWrapping.Wrap,
			Margin = new Thickness(4, 2, 4, 0)
		};
		leftStack.Children.Add(_srcFilterCaptionText);

		Grid.SetColumn(leftStack, 0);
		mainGrid.Children.Add(leftStack);

		// --- Right Section: Filter Plan 1x / Nx Details ---
		var rightBorder = new Border
		{
			BorderBrush = CreateLiquidGlassRimBrush(),
			BorderThickness = new Thickness(1, 0, 0, 0),
			Padding = new Thickness(24, 0, 0, 0)
		};
		var rightStack = new StackPanel { Spacing = 14 };

		rightStack.Children.Add(new TextBlock
		{
			Text = "Filter 1x / Nx",
			FontSize = 12,
			FontWeight = FontWeights.Bold,
			Foreground = DspTextMutedBrush
		});

		// 1x Details Box
		_srcFilterPlan1xSlot = new Border { Padding = new Thickness(0, 0, 0, 10) };
		var f1xSp = new StackPanel { Spacing = 4 };
		var f1xHeader = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 6 };
		f1xHeader.Children.Add(new TextBlock { Text = "1X", FontSize = 11, FontWeight = FontWeights.Bold, FontFamily = new FontFamily("Consolas, Segoe UI"), Foreground = DspTextMutedBrush });
		f1xHeader.Children.Add(new TextBlock { Text = "· 激活", FontSize = 10.5, FontWeight = FontWeights.SemiBold, Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 16, 185, 129)) });
		f1xSp.Children.Add(f1xHeader);

		_srcFilterPlan1xName = new TextBlock { Text = "FurinaPlayer Gauss Long", FontSize = 14, FontWeight = FontWeights.SemiBold, Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 37, 99, 235)) };
		f1xSp.Children.Add(_srcFilterPlan1xName);

		_srcFilterPlan1xDesc = new TextBlock { Text = "高阶量 sinc 滤波，听感更柔和，兼顾瞬态。", FontSize = 11, Foreground = DspTextMutedBrush, TextWrapping = TextWrapping.Wrap };
		f1xSp.Children.Add(_srcFilterPlan1xDesc);

		_srcFilterPlan1xPhase = new TextBlock { Text = "Linear phase", FontSize = 11, FontFamily = new FontFamily("Consolas, Segoe UI"), Foreground = DspTextMutedBrush };
		f1xSp.Children.Add(_srcFilterPlan1xPhase);
		_srcFilterPlan1xSlot.Child = f1xSp;
		rightStack.Children.Add(_srcFilterPlan1xSlot);

		// Divider
		rightStack.Children.Add(new Rectangle { Height = 1, Fill = new SolidColorBrush(Windows.UI.Color.FromArgb(140, 226, 232, 240)), HorizontalAlignment = HorizontalAlignment.Stretch });

		// Nx Details Box
		_srcFilterPlanNxSlot = new Border { Padding = new Thickness(0, 10, 0, 0) };
		var fnxSp = new StackPanel { Spacing = 4 };
		fnxSp.Children.Add(new TextBlock { Text = "NX", FontSize = 11, FontWeight = FontWeights.Bold, FontFamily = new FontFamily("Consolas, Segoe UI"), Foreground = DspTextMutedBrush });

		_srcFilterPlanNxName = new TextBlock { Text = "FurinaPlayer Halfband", FontSize = 14, FontWeight = FontWeights.SemiBold, Foreground = DspTextPrimaryBrush };
		fnxSp.Children.Add(_srcFilterPlanNxName);

		_srcFilterPlanNxDesc = new TextBlock { Text = "半带 sinc 滤波，适合 2x/4x 先倒频定实时性。", FontSize = 11, Foreground = DspTextMutedBrush, TextWrapping = TextWrapping.Wrap };
		fnxSp.Children.Add(_srcFilterPlanNxDesc);

		_srcFilterPlanNxPhase = new TextBlock { Text = "Linear phase", FontSize = 11, FontFamily = new FontFamily("Consolas, Segoe UI"), Foreground = DspTextMutedBrush };
		fnxSp.Children.Add(_srcFilterPlanNxPhase);
		_srcFilterPlanNxSlot.Child = fnxSp;
		rightStack.Children.Add(_srcFilterPlanNxSlot);

		rightBorder.Child = rightStack;
		Grid.SetColumn(rightBorder, 1);
		mainGrid.Children.Add(rightBorder);

		card.Child = mainGrid;
		return card;
	}

	private Border CreateSegmentedBar((string Key, string Label)[] items, string currentKey, List<Button> buttonRegistry, Action<string> onSelect)
	{
		var container = new Border
		{
			Background = DspSoftSurfaceBrush,
			BorderBrush = CreateLiquidGlassRimBrush(),
			BorderThickness = new Thickness(1),
			CornerRadius = new CornerRadius(9),
			Padding = new Thickness(3)
		};

		var grid = new Grid();
		for (int i = 0; i < items.Length; i++)
		{
			grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.0, GridUnitType.Star) });
		}

		for (int i = 0; i < items.Length; i++)
		{
			var item = items[i];
			bool isSelected = string.Equals(item.Key, currentKey, StringComparison.OrdinalIgnoreCase);

			var btn = new Button
			{
				Content = item.Label,
				Tag = item.Key,
				HorizontalAlignment = HorizontalAlignment.Stretch,
				VerticalAlignment = VerticalAlignment.Stretch,
				CornerRadius = new CornerRadius(7),
				Padding = new Thickness(8, 6, 8, 6),
				FontSize = 13
			};

			ApplySegmentedButtonStyle(btn, isSelected);

			btn.Click += (s, e) =>
			{
				string key = btn.Tag?.ToString() ?? "";
				onSelect(key);
				foreach (var otherBtn in buttonRegistry)
				{
					bool isCurrent = string.Equals(otherBtn.Tag?.ToString(), key, StringComparison.OrdinalIgnoreCase);
					ApplySegmentedButtonStyle(otherBtn, isCurrent);
				}
			};

			buttonRegistry.Add(btn);
			Grid.SetColumn(btn, i);
			grid.Children.Add(btn);
		}

		container.Child = grid;
		return container;
	}

	private void ApplySegmentedButtonStyle(Button btn, bool isSelected)
	{
		bool isDark = IsDspDark;
		if (isSelected)
		{
			btn.Background = new SolidColorBrush(isDark ? Windows.UI.Color.FromArgb(95, 59, 130, 246) : Colors.White);
			btn.BorderBrush = new SolidColorBrush(isDark ? Windows.UI.Color.FromArgb(160, 147, 197, 253) : Windows.UI.Color.FromArgb(200, 203, 213, 225));
			btn.BorderThickness = new Thickness(1);
			btn.Foreground = new SolidColorBrush(isDark ? Windows.UI.Color.FromArgb(255, 224, 242, 254) : Windows.UI.Color.FromArgb(255, 37, 99, 235));
			btn.FontWeight = FontWeights.Bold;
		}
		else
		{
			btn.Background = new SolidColorBrush(Colors.Transparent);
			btn.BorderThickness = new Thickness(0);
			btn.Foreground = DspTextMutedBrush;
			btn.FontWeight = FontWeights.Medium;
		}
	}

	private void OnModeSelected(string mode)
	{
		if (ViewModel == null) return;
		ViewModel.FurinaSrcMode = mode;

		int inputRate = ViewModel.EngineSampleRate > 0 ? ViewModel.EngineSampleRate : 44100;
		if (inputRate <= 0) inputRate = 44100;

		switch (mode)
		{
			case "off":
				ViewModel.EchoResamplerEnabled = false;
				break;
			case "compatibility48":
				ViewModel.EchoResamplerEnabled = true;
				ViewModel.EchoResamplerTargetRate = 48000;
				break;
			case "family2x":
				ViewModel.EchoResamplerEnabled = true;
				ViewModel.EchoResamplerTargetRate = inputRate == 44100 ? 88200 : (inputRate == 48000 ? 96000 : inputRate * 2);
				break;
			case "family4x":
				ViewModel.EchoResamplerEnabled = true;
				ViewModel.EchoResamplerTargetRate = inputRate == 44100 ? 176400 : (inputRate == 48000 ? 192000 : inputRate * 4);
				break;
			case "family8x":
				ViewModel.EchoResamplerEnabled = true;
				ViewModel.EchoResamplerTargetRate = inputRate == 44100 ? 352800 : (inputRate == 48000 ? 384000 : inputRate * 8);
				break;
		}

		ViewModel.SaveEchoSettings();
		RefreshSrcUpsamplingState();
	}

	private void OnQualitySelected(string quality)
	{
		if (ViewModel == null) return;
		ViewModel.FurinaSrcQuality = quality;
		ViewModel.SaveEchoSettings();
		RefreshSrcUpsamplingState();
	}

	private void RefreshSrcUpsamplingState()
	{
		string mode = ViewModel?.FurinaSrcMode ?? "off";
		string quality = ViewModel?.FurinaSrcQuality ?? "transparent";
		bool enabled = ViewModel?.EchoResamplerEnabled ?? false;

		int inputRate = (ViewModel != null && ViewModel.EngineSampleRate > 0) ? ViewModel.EngineSampleRate : 44100;
		if (inputRate <= 0) inputRate = 44100;

		int targetRate = ViewModel?.EchoResamplerTargetRate ?? 192000;

		// Status dot & text
		if (mode == "off" || !enabled)
		{
			if (_srcStatusDot != null) _srcStatusDot.Fill = DspTextMutedBrush; // Gray
			if (_srcStatusText != null) _srcStatusText.Text = "原生直通";
			if (_srcSourceRateText != null) _srcSourceRateText.Text = $"{inputRate / 1000.0:F1} kHz";
			if (_srcTargetRateText != null) _srcTargetRateText.Text = "--";
			if (_srcRatioBadge != null) _srcRatioBadge.Text = "";
			if (_srcStep1Val != null) _srcStep1Val.Text = "关闭";
			if (_srcStep3Val != null) _srcStep3Val.Text = "--";
			if (_srcFilterSlotValue != null) _srcFilterSlotValue.Text = "Standby";
		}
		else
		{
			if (_srcStatusDot != null) _srcStatusDot.Fill = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 16, 185, 129)); // Green
			if (_srcStatusText != null) _srcStatusText.Text = "升频激活";
			if (_srcSourceRateText != null) _srcSourceRateText.Text = $"{inputRate / 1000.0:F1} kHz";
			if (_srcTargetRateText != null) _srcTargetRateText.Text = $"{targetRate / 1000.0:F1} kHz";

			double ratio = (double)targetRate / inputRate;
			if (_srcRatioBadge != null) _srcRatioBadge.Text = $"×{ratio:0.##}";

			string modeLabel = mode switch
			{
				"compatibility48" => "48 kHz 兼容",
				"family2x" => "2x PCM",
				"family4x" => "4x PCM",
				"family8x" => "8x Ultra",
				_ => "关闭"
			};
			if (_srcStep1Val != null) _srcStep1Val.Text = modeLabel;
			if (_srcStep3Val != null) _srcStep3Val.Text = $"{ratio:0.##}x";

			string f1xName = ViewModel?.FurinaSrcFilter1x ?? "FurinaPlayer Gauss Long";
			if (_srcFilterSlotValue != null) _srcFilterSlotValue.Text = $"1X · {f1xName.ToUpperInvariant()}";
		}

		// Quality precision
		string precision = quality switch
		{
			"balanced" => "SOXR precision 20",
			"lowLatency" => "SOXR precision 16",
			_ => "SOXR precision 28"
		};
		if (_srcPrecisionValue != null) _srcPrecisionValue.Text = precision;
		if (_srcStep2Val != null)
		{
			_srcStep2Val.Text = quality switch
			{
				"balanced" => "Balanced",
				"lowLatency" => "Low latency",
				_ => "Transparent"
			};
		}

		// Mode Hint Text
		if (_srcModeHint != null)
		{
			_srcModeHint.Text = mode switch
			{
				"compatibility48" => "将 PCM 固定转换到 48 kHz，适合只接受 48 kHz 的独占耳机或声卡；默认关闭。",
				"family2x" => "44.1k 家族升到 88.2k，48k 家族升到 96k。",
				"family4x" => "44.1k 家族升到 176.4k，48k 家族升到 192k。",
				"family8x" => "实验档：44.1k 家族升到 352.8k，48k 家族升到 384k。",
				_ => "保持源采样率，Bit-perfect 条件不受 FurinaPlayer SRC 影响。"
			};
		}

		// Quality Hint Text
		if (_srcQualityHint != null)
		{
			_srcQualityHint.Text = quality switch
			{
				"balanced" => "保持原有 SOXR 档位，兼顾稳定和开销。",
				"lowLatency" => "降低 SRC 开销，适合低延迟输出。",
				_ => "最高精度 SOXR，优先透明度和截断失真。"
			};
		}

		// Compute step 4
		string compute = ViewModel?.FurinaSrcCompute ?? "CPU SIMD";
		if (_srcStep4Val != null)
		{
			_srcStep4Val.Text = compute switch
			{
				"CPU 多线程" => "CPU 多线程实时路径。",
				"GPU 加速 (若支持)" => "CUDA FIR 准备中，自动兼顾回落。",
				_ => "CPU 实时路径,未请求 CUDA。"
			};
		}

		// Dither slot
		string dither = ViewModel?.FurinaSrcDither ?? "Off";
		if (_srcDitherSlotValue != null) _srcDitherSlotValue.Text = dither == "Off" ? "关闭" : dither;
	}

	private void UpdateFilter1xHint(string sel)
	{
		if (_srcFilter1xHint == null) return;
		_srcFilter1xHint.Text = sel switch
		{
			"FurinaPlayer Gauss Short" => "低阶高斯窗 sinc 滤波，瞬态响应快，听感温暖。",
			"Linear Phase" => "对称 sinc 滤波，群延迟恒定，零相位失真。",
			"Min Phase" => "最小相位滤波，零前振铃，能量集中于瞬态起音。",
			"FurinaPlayer Steep Short" => "短阶陡峭滤波，截止带陡峭，适合实时处理。",
			"FurinaPlayer Steep Long" => "长阶陡峭滤波，>100dB 阻带衰减，母带级透明度。",
			_ => "高阶量 sinc 滤波，听感更柔和，兼顾瞬态。"
		};
	}

	private void UpdateFilterNxHint(string sel)
	{
		if (_srcFilterNxHint == null) return;
		_srcFilterNxHint.Text = sel switch
		{
			"Sinc Polyphase" => "多相 sinc 滤波，频响平直，极低互调失真。",
			"Steep" => "陡峭过渡带滤波，阻带深度抑制镜像混叠。",
			"FurinaPlayer Steep XL" => "极限 3071 阶陡峭滤波，用于测量级对照。",
			_ => "半带 sinc 滤波，适合 2x/4x 先倒频定实时性。"
		};
	}

	private void UpdateComputeHint(string sel)
	{
		if (_srcComputeHint == null) return;
		_srcComputeHint.Text = sel switch
		{
			"CPU 多线程" => "利用多核心并发执行块滤波，降低单核负载与发热。",
			"GPU 加速 (若支持)" => "实验性 CUDA FIR 路径；需 NVIDIA 显卡驱动支持。",
			_ => "默认实时路径：极优，无降采样风险，并保证随机可用。"
		};
		if (_srcStep4Val != null)
		{
			_srcStep4Val.Text = sel switch
			{
				"CPU 多线程" => "CPU 多线程实时路径。",
				"GPU 加速 (若支持)" => "CUDA FIR 准备中，自动兼顾回落。",
				_ => "CPU 实时路径,未请求 CUDA。"
			};
		}
	}

	private void UpdateDitherHint(string sel)
	{
		if (_srcDitherHint == null) return;
		_srcDitherHint.Text = sel switch
		{
			"TPDF" => "标准三角形概率密度抖动，消除 16/24-bit 量化相关失真。",
			"High-Pass Noise Shaping" => "高通噪声整形：将量化噪声推向 18kHz 以上高频不敏感区。",
			_ => "保持 Float32 PCM 链原图标：默认关闭，避免无意义加噪。"
		};
	}

	private void UpdateFilterDesignCurve()
	{
		try
		{
			if (_srcFilterCurveCanvas == null) return;
			_srcFilterCurveCanvas.Children.Clear();

		string f1x = ViewModel?.FurinaSrcFilter1x ?? "FurinaPlayer Gauss Long";
		string fnx = ViewModel?.FurinaSrcFilterNx ?? "FurinaPlayer Halfband";

		// Update Labels on Right Plan
		if (_srcFilterPlan1xName != null) _srcFilterPlan1xName.Text = f1x;
		if (_srcFilterPlanNxName != null) _srcFilterPlanNxName.Text = fnx;
		if (_srcFilterPlan1xDesc != null && _srcFilter1xHint != null) _srcFilterPlan1xDesc.Text = _srcFilter1xHint.Text;
		if (_srcFilterPlanNxDesc != null && _srcFilterNxHint != null) _srcFilterPlanNxDesc.Text = _srcFilterNxHint.Text;

		string l1x = f1x.ToLowerInvariant();
		bool isMinPhase = l1x.Contains("min") || l1x.Contains("min-phase") || l1x.Contains("minimum");
		string phaseStr = isMinPhase ? "Minimum phase" : "Linear phase";

		if (_srcPhaseBadgeText != null) _srcPhaseBadgeText.Text = phaseStr;
		if (_srcFilterPlan1xPhase != null) _srcFilterPlan1xPhase.Text = phaseStr;
		if (_srcFilterNameText != null)
		{
			_srcFilterNameText.Text = f1x.Replace("FurinaPlayer ", "furina-").Replace(" ", "-").ToLowerInvariant();
		}

		// Calculate Curve Parameters
		bool isSoft = l1x.Contains("soft") || l1x.Contains("gauss");
		bool isSteep = l1x.Contains("brickwall") || l1x.Contains("extreme") || l1x.Contains("steep") || l1x.Contains("xl");

		double xKnee = isSoft ? 505 : (isSteep ? 570 : 545);
		double tEnd = Math.Min(640, xKnee + (isSteep ? 28 : (isSoft ? 82 : 52)));

		// --- 1. Grid Lines ---
		var gridBrush = new SolidColorBrush(Windows.UI.Color.FromArgb(100, 226, 232, 240));

		// Vertical lines
		double[] vLines = { 52, 156, 260, 364, 468, 572, 676 };
		foreach (double vx in vLines)
		{
			_srcFilterCurveCanvas.Children.Add(new Line
			{
				X1 = vx,
				Y1 = 28,
				X2 = vx,
				Y2 = 188,
				Stroke = gridBrush,
				StrokeThickness = 1
			});
		}

		// Horizontal lines
		double[] hLines = { 54, 88, 122, 156, 188 };
		foreach (double hy in hLines)
		{
			_srcFilterCurveCanvas.Children.Add(new Line
			{
				X1 = 52,
				Y1 = hy,
				X2 = 676,
				Y2 = hy,
				Stroke = gridBrush,
				StrokeThickness = 1
			});
		}

		// Reference dashed line (at y = 66, approx -8dB)
		_srcFilterCurveCanvas.Children.Add(new Line
		{
			X1 = 52,
			Y1 = 66,
			X2 = 676,
			Y2 = 66,
			Stroke = new SolidColorBrush(Windows.UI.Color.FromArgb(120, 203, 213, 225)),
			StrokeThickness = 1,
			StrokeDashArray = new DoubleCollection { 4, 5 }
		});

		// Knee dashed vertical line (at cutoff)
		_srcFilterCurveCanvas.Children.Add(new Line
		{
			X1 = xKnee,
			Y1 = 28,
			X2 = xKnee,
			Y2 = 188,
			Stroke = new SolidColorBrush(Windows.UI.Color.FromArgb(160, 16, 185, 129)),
			StrokeThickness = 1,
			StrokeDashArray = new DoubleCollection { 3, 4 }
		});

		// --- 2. Build Bezier Path Geometry ---
		var areaFig = new PathFigure { StartPoint = new Point(52, 54), IsClosed = true, IsFilled = true };
		var curveFig = new PathFigure { StartPoint = new Point(52, 54), IsClosed = false, IsFilled = false };

		// Bezier 1: Flat passband
		var b1 = new BezierSegment { Point1 = new Point(180, 54), Point2 = new Point(360, 54), Point3 = new Point(xKnee - 42, 55) };
		// Bezier 2: Knee roll-off start
		var b2 = new BezierSegment { Point1 = new Point(xKnee - 8, 56), Point2 = new Point(xKnee, 60), Point3 = new Point(xKnee + 8, 78) };
		// Bezier 3: Transition steep fall
		var b3 = new BezierSegment { Point1 = new Point(xKnee + 18, 106), Point2 = new Point(tEnd - 22, 164), Point3 = new Point(tEnd, 177) };
		// Bezier 4: Stopband landing at -96dB (y = 188)
		var b4 = new BezierSegment { Point1 = new Point(tEnd + 20, 188), Point2 = new Point(650, 188), Point3 = new Point(676, 188) };

		curveFig.Segments.Add(b1);
		curveFig.Segments.Add(b2);
		curveFig.Segments.Add(b3);
		curveFig.Segments.Add(b4);

		// Area figure requires distinct PathSegment instances to avoid COMException (0x800F1000)
		var ab1 = new BezierSegment { Point1 = b1.Point1, Point2 = b1.Point2, Point3 = b1.Point3 };
		var ab2 = new BezierSegment { Point1 = b2.Point1, Point2 = b2.Point2, Point3 = b2.Point3 };
		var ab3 = new BezierSegment { Point1 = b3.Point1, Point2 = b3.Point2, Point3 = b3.Point3 };
		var ab4 = new BezierSegment { Point1 = b4.Point1, Point2 = b4.Point2, Point3 = b4.Point3 };

		areaFig.Segments.Add(ab1);
		areaFig.Segments.Add(ab2);
		areaFig.Segments.Add(ab3);
		areaFig.Segments.Add(ab4);
		areaFig.Segments.Add(new LineSegment { Point = new Point(676, 188) });
		areaFig.Segments.Add(new LineSegment { Point = new Point(52, 188) });

		// Add Area Fill Path
		var areaGeo = new PathGeometry();
		areaGeo.Figures.Add(areaFig);
		_srcFilterCurveCanvas.Children.Add(new Path
		{
			Data = areaGeo,
			Fill = new SolidColorBrush(Windows.UI.Color.FromArgb(24, 37, 99, 235)) // Translucent Blue
		});

		// Add Curve Stroke Path
		var curveGeo = new PathGeometry();
		curveGeo.Figures.Add(curveFig);
		_srcFilterCurveCanvas.Children.Add(new Path
		{
			Data = curveGeo,
			Stroke = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 37, 99, 235)), // Vibrant Blue
			StrokeThickness = 2.4,
			StrokeStartLineCap = PenLineCap.Round,
			StrokeEndLineCap = PenLineCap.Round
		});

		// --- 3. Text Labels on Canvas ---
		var labelBrush = DspTextMutedBrush;
		var monoFont = new FontFamily("Consolas, Segoe UI");

		// Y-axis labels
		AddSrcCanvasText(_srcFilterCurveCanvas, "0", 38, 47, labelBrush, monoFont, 10);
		AddSrcCanvasText(_srcFilterCurveCanvas, "-24", 26, 81, labelBrush, monoFont, 10);
		AddSrcCanvasText(_srcFilterCurveCanvas, "-48", 26, 115, labelBrush, monoFont, 10);
		AddSrcCanvasText(_srcFilterCurveCanvas, "-72", 26, 149, labelBrush, monoFont, 10);
		AddSrcCanvasText(_srcFilterCurveCanvas, "-96", 26, 181, labelBrush, monoFont, 10);

		// X-axis labels
		AddSrcCanvasText(_srcFilterCurveCanvas, "20 Hz", 52, 196, labelBrush, monoFont, 10.5);
		AddSrcCanvasText(_srcFilterCurveCanvas, "10 kHz", 346, 196, labelBrush, monoFont, 10.5);
		AddSrcCanvasText(_srcFilterCurveCanvas, "Nyquist", 640, 196, labelBrush, monoFont, 10.5);

		// Inner Chart Phase Text
		AddSrcCanvasText(_srcFilterCurveCanvas, phaseStr, 590, 40, DspTextMutedBrush, monoFont, 11);
		}
		catch (Exception ex)
		{
			System.Diagnostics.Debug.WriteLine($"UpdateFilterDesignCurve failed: {ex.Message}");
		}
	}

	private static void AddSrcCanvasText(Canvas canvas, string text, double left, double top, Brush foreground, FontFamily font, double fontSize)
	{
		var tb = new TextBlock
		{
			Text = text,
			FontSize = fontSize,
			Foreground = foreground,
			FontFamily = font
		};
		Canvas.SetLeft(tb, left);
		Canvas.SetTop(tb, top);
		canvas.Children.Add(tb);
	}

	private static string SelectMatchingItem(ComboBox combo, string? value, string defaultValue)
	{
		if (string.IsNullOrEmpty(value)) return defaultValue;
		foreach (var item in combo.Items)
		{
			if (item is string s && (string.Equals(s, value, StringComparison.OrdinalIgnoreCase) || s.Contains(value, StringComparison.OrdinalIgnoreCase)))
			{
				return s;
			}
		}
		return defaultValue;
	}
}
