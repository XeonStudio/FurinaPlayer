using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using FurinaPlayer.Audio.Dsp;
using FurinaPlayer.Core.Services;
using Microsoft.UI;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using Windows.Foundation;

namespace FurinaPlayer.UI.Pages;

public sealed partial class DspPage
{
	private FrameworkElement? _srcUpsamplingWorkbench;
	private Canvas? _srcFilterCurveCanvas;
	private TextBlock? _srcFilterNameText;
	private TextBlock? _srcPhaseBadgeText;
	private TextBlock? _srcTapsBadgeText;
	private TextBlock? _srcAttenBadgeText;
	private TextBlock? _srcDelayBadgeText;
	private TextBlock? _srcFilterCaptionText;
	private TextBlock? _srcFilterPlan1xStatus;
	private TextBlock? _srcFilterPlan1xName;
	private TextBlock? _srcFilterPlan1xDesc;
	private TextBlock? _srcFilterPlan1xPhase;
	private TextBlock? _srcFilterPlanNxStatus;
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
	private TextBlock? _srcFallbackNoticeText;

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
	private readonly List<Button> _srcChartModeButtons = new();
	private ComboBox? _srcFilter1xCombo;
	private ComboBox? _srcFilterNxCombo;
	private ComboBox? _srcComputeCombo;
	private ComboBox? _srcDitherCombo;
	private CheckBox? _srcAbBenchmarkCheck;
	private FrameworkElement? _srcAdvancedPanel;
	private FontIcon? _srcAdvancedChevron;

	private bool _srcSyncingUi;
	private bool _srcShowImpulseMode;
	private DispatcherTimer? _srcTelemetryTimer;

	private sealed record SrcChoiceItem(string Id, string Label)
	{
		public override string ToString() => Label;
	}

	public FrameworkElement CreateSrcUpsamplingWorkbench()
	{
		if (_srcUpsamplingWorkbench != null)
		{
			RefreshSrcUpsamplingState();
			UpdateFilterDesignCurve();
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
		root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // 1: Hero Route & Telemetry Card
		root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // 2: Control Deck Card
		root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // 3: True FFT & Impulse Preview Card
		root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // 4: Engine Note

		root.Children.Add(CreateSrcHeaderBar());

		var heroCard = CreateSrcHeroCard();
		Grid.SetRow(heroCard, 1);
		root.Children.Add(heroCard);

		var controlDeck = CreateSrcControlDeck();
		Grid.SetRow(controlDeck, 2);
		root.Children.Add(controlDeck);

		var filterPreview = CreateSrcFilterPreviewCard();
		Grid.SetRow(filterPreview, 3);
		root.Children.Add(filterPreview);

		var bottomNote = new TextBlock
		{
			Text = Loc.T("基于 FurinaPlayer 原生 AVX2 / SIMD 多级联多相 FIR 引擎，全面兼容 WASAPI 共享、ASIO 独占与 WaveOut，支持硬件采样率自动协商与无断音切换。"),
			FontSize = 11,
			Foreground = DspTextMutedBrush,
			TextWrapping = TextWrapping.Wrap,
			Margin = new Thickness(4, 4, 4, 12)
		};
		Grid.SetRow(bottomNote, 4);
		root.Children.Add(bottomNote);

		sv.Content = root;
		sv.Loaded += (s, e) =>
		{
			StartSrcTelemetryTimer();
			RefreshSrcUpsamplingState();
			UpdateFilterDesignCurve();
		};
		sv.Unloaded += (s, e) => StopSrcTelemetryTimer();

		_srcUpsamplingWorkbench = sv;

		RefreshSrcUpsamplingState();
		UpdateFilterDesignCurve();

		return _srcUpsamplingWorkbench;
	}

	private void StartSrcTelemetryTimer()
	{
		if (_srcTelemetryTimer == null)
		{
			_srcTelemetryTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(500) };
			_srcTelemetryTimer.Tick += (s, e) => RefreshSrcUpsamplingState();
		}
		if (!_srcTelemetryTimer.IsEnabled)
		{
			_srcTelemetryTimer.Start();
		}
	}

	private void StopSrcTelemetryTimer()
	{
		_srcTelemetryTimer?.Stop();
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
				Glyph = "\uE774",
				FontSize = 16,
				Foreground = DspAccentSoftTextBrush,
				HorizontalAlignment = HorizontalAlignment.Center,
				VerticalAlignment = VerticalAlignment.Center
			}
		};
		titleRow.Children.Add(iconBorder);
		titleRow.Children.Add(new TextBlock
		{
			Text = Loc.T("FurinaPlayer SRC / 升频"),
			FontSize = 20,
			FontWeight = FontWeights.Bold,
			Foreground = DspTextPrimaryBrush,
			VerticalAlignment = VerticalAlignment.Center
		});
		leftStack.Children.Add(titleRow);

		var statusRow = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8, Margin = new Thickness(44, 0, 0, 0) };
		_srcStatusDot = new Ellipse
		{
			Width = 7,
			Height = 7,
			Fill = DspTextMutedBrush,
			VerticalAlignment = VerticalAlignment.Center
		};
		_srcStatusText = new TextBlock
		{
			Text = Loc.T("原生直通"),
			FontSize = 12,
			FontWeight = FontWeights.Medium,
			Foreground = DspTextMutedBrush,
			VerticalAlignment = VerticalAlignment.Center
		};
		_srcFallbackNoticeText = new TextBlock
		{
			Text = "",
			FontSize = 11,
			Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 217, 119, 6)),
			VerticalAlignment = VerticalAlignment.Center,
			Visibility = Visibility.Collapsed
		};
		statusRow.Children.Add(_srcStatusDot);
		statusRow.Children.Add(_srcStatusText);
		statusRow.Children.Add(_srcFallbackNoticeText);
		leftStack.Children.Add(statusRow);

		Grid.SetColumn(leftStack, 0);
		headerGrid.Children.Add(leftStack);

		// Right Actions: A/B Blind Compare + Telemetry Refresh
		var subActionRow = new StackPanel
		{
			Orientation = Orientation.Horizontal,
			Spacing = 12,
			HorizontalAlignment = HorizontalAlignment.Right,
			VerticalAlignment = VerticalAlignment.Center
		};

		_srcAbBenchmarkCheck = new CheckBox
		{
			Content = Loc.T("A/B 实时盲听对比"),
			FontSize = 12,
			IsChecked = ViewModel?.FurinaSrcAbBenchmark ?? false,
			VerticalAlignment = VerticalAlignment.Center
		};
		_srcAbBenchmarkCheck.Checked += (s, e) =>
		{
			if (_srcSyncingUi || ViewModel == null) return;
			ViewModel.FurinaSrcAbBenchmark = true;
			ViewModel.SaveFurinaPlayerSettings();
			ViewModel.ApplySrcSettings(reopenIfRateChanged: false);
			RefreshSrcUpsamplingState();
		};
		_srcAbBenchmarkCheck.Unchecked += (s, e) =>
		{
			if (_srcSyncingUi || ViewModel == null) return;
			ViewModel.FurinaSrcAbBenchmark = false;
			ViewModel.SaveFurinaPlayerSettings();
			ViewModel.ApplySrcSettings(reopenIfRateChanged: false);
			RefreshSrcUpsamplingState();
		};
		subActionRow.Children.Add(_srcAbBenchmarkCheck);

		var btnRefresh = new Button
		{
			CornerRadius = new CornerRadius(8),
			Padding = new Thickness(10, 5, 10, 5),
			Background = DspSoftSurfaceBrush,
			BorderBrush = CreateLiquidGlassRimBrush(),
			BorderThickness = new Thickness(1),
			Content = new StackPanel
			{
				Orientation = Orientation.Horizontal,
				Spacing = 5,
				Children =
				{
					new FontIcon { Glyph = "\uE72C", FontSize = 12, Foreground = DspTextSubHeaderBrush },
					new TextBlock { Text = Loc.T("刷新"), FontSize = 12, Foreground = DspTextSubHeaderBrush }
				}
			}
		};
		btnRefresh.Click += (s, e) =>
		{
			ViewModel?.ApplySrcSettings(reopenIfRateChanged: false);
			RefreshSrcUpsamplingState();
			UpdateFilterDesignCurve();
		};
		subActionRow.Children.Add(btnRefresh);

		Grid.SetColumn(subActionRow, 1);
		headerGrid.Children.Add(subActionRow);

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
		mainGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(360, GridUnitType.Pixel) });

		// --- Left Section: Route Path & 4-Stage Step Bar ---
		var leftStack = new StackPanel { Spacing = 20, Margin = new Thickness(0, 0, 32, 0) };

		var routeTitleSp = new StackPanel { Spacing = 8 };
		routeTitleSp.Children.Add(new TextBlock
		{
			Text = Loc.T("路径"),
			FontSize = 11,
			FontWeight = FontWeights.Bold,
			Foreground = DspTextMutedBrush
		});

		var rateGrid = new Grid();
		rateGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
		rateGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(56, GridUnitType.Pixel) });
		rateGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
		rateGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

		var srcSp = new StackPanel { Spacing = 2 };
		srcSp.Children.Add(new TextBlock { Text = Loc.T("源采样率"), FontSize = 11, Foreground = DspTextMutedBrush });
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

		var arrowSp = new StackPanel { VerticalAlignment = VerticalAlignment.Center, HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(8, 12, 8, 0) };
		arrowSp.Children.Add(new TextBlock
		{
			Text = "──▶",
			FontSize = 16,
			FontWeight = FontWeights.Bold,
			Foreground = DspTextMutedBrush,
			HorizontalAlignment = HorizontalAlignment.Center
		});
		Grid.SetColumn(arrowSp, 1);
		rateGrid.Children.Add(arrowSp);

		var dstSp = new StackPanel { Spacing = 2 };
		dstSp.Children.Add(new TextBlock { Text = Loc.T("目标采样率"), FontSize = 11, Foreground = DspTextMutedBrush });
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

		stepBarGrid.Children.Add(CreateStepIndicator(0, Loc.T("01 FurinaPlayer SRC 控制模式"), Loc.T("关闭"), out _srcStep1Val, false));
		stepBarGrid.Children.Add(CreateStepIndicator(1, Loc.T("02 质量策略"), "Transparent", out _srcStep2Val, false));
		stepBarGrid.Children.Add(CreateStepIndicator(2, Loc.T("03 倍率"), "--", out _srcStep3Val, false));
		stepBarGrid.Children.Add(CreateStepIndicator(3, "04 SIMD Compute", "AVX2 Vector<float>", out _srcStep4Val, true));

		leftStack.Children.Add(stepBarGrid);
		Grid.SetColumn(leftStack, 0);
		mainGrid.Children.Add(leftStack);

		// --- Right Section: Live Precision, FIR Stage & Telemetry ---
		var rightBorder = new Border
		{
			BorderBrush = CreateLiquidGlassRimBrush(),
			BorderThickness = new Thickness(1, 0, 0, 0),
			Padding = new Thickness(28, 0, 0, 0)
		};
		var rightStack = new StackPanel { Spacing = 14 };

		rightStack.Children.Add(new TextBlock
		{
			Text = Loc.T("精度与实时引擎遥测"),
			FontSize = 11,
			FontWeight = FontWeights.Bold,
			Foreground = DspTextMutedBrush
		});

		_srcPrecisionValue = new TextBlock
		{
			Text = "Furina FIR · AVX2 SIMD",
			FontSize = 21,
			FontWeight = FontWeights.SemiBold,
			Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 37, 99, 235)),
			FontFamily = new FontFamily("Consolas, Segoe UI")
		};
		rightStack.Children.Add(_srcPrecisionValue);

		var statColsGrid = new Grid { ColumnSpacing = 18, Margin = new Thickness(0, 4, 0, 0) };
		statColsGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.0, GridUnitType.Star) });
		statColsGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.0, GridUnitType.Star) });

		var slotSp = new StackPanel { Spacing = 2 };
		slotSp.Children.Add(new TextBlock { Text = Loc.T("过滤器插槽"), FontSize = 11, Foreground = DspTextMutedBrush });
		_srcFilterSlotValue = new TextBlock { Text = "Standby", FontSize = 12.5, FontWeight = FontWeights.SemiBold, Foreground = DspTextPrimaryBrush, TextTrimming = TextTrimming.CharacterEllipsis };
		slotSp.Children.Add(_srcFilterSlotValue);
		Grid.SetColumn(slotSp, 0);
		statColsGrid.Children.Add(slotSp);

		var ditherSp = new StackPanel { Spacing = 2 };
		ditherSp.Children.Add(new TextBlock { Text = "PCM Dither / Noise Shaping", FontSize = 11, Foreground = DspTextMutedBrush });
		_srcDitherSlotValue = new TextBlock { Text = Loc.T("关闭"), FontSize = 12.5, FontWeight = FontWeights.SemiBold, Foreground = DspTextPrimaryBrush, TextTrimming = TextTrimming.CharacterEllipsis };
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
		sp.Children.Add(new Rectangle
		{
			Height = 2,
			Fill = new SolidColorBrush(isLast ? Windows.UI.Color.FromArgb(255, 16, 185, 129) : Windows.UI.Color.FromArgb(255, 59, 130, 246)),
			HorizontalAlignment = HorizontalAlignment.Stretch,
			Margin = new Thickness(0, 0, 0, 4)
		});

		sp.Children.Add(new TextBlock
		{
			Text = label,
			FontSize = 11,
			Foreground = DspTextMutedBrush
		});

		valBlock = new TextBlock
		{
			Text = defaultValue,
			FontSize = 12.5,
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

		// Top row: Advanced Ladder Presets Toggle
		var topHeaderRow = new Grid();
		topHeaderRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.0, GridUnitType.Star) });
		topHeaderRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

		var btnToggleAdvanced = new HyperlinkButton { Padding = new Thickness(6, 2, 6, 2) };
		var advSp = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 5 };
		advSp.Children.Add(new TextBlock { Text = Loc.T("快速预设阶梯"), FontSize = 12, FontWeight = FontWeights.SemiBold, Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 37, 99, 235)) });
		bool advOpen = ViewModel?.FurinaSrcAdvanced ?? false;
		_srcAdvancedChevron = new FontIcon { Glyph = advOpen ? "\uE70E" : "\uE70D", FontSize = 11, Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 37, 99, 235)) };
		advSp.Children.Add(_srcAdvancedChevron);
		btnToggleAdvanced.Content = advSp;

		btnToggleAdvanced.Click += (s, e) =>
		{
			if (_srcAdvancedPanel != null)
			{
				bool isVisible = _srcAdvancedPanel.Visibility == Visibility.Visible;
				_srcAdvancedPanel.Visibility = isVisible ? Visibility.Collapsed : Visibility.Visible;
				_srcAdvancedChevron.Glyph = isVisible ? "\uE70D" : "\uE70E";
				if (ViewModel != null)
				{
					ViewModel.FurinaSrcAdvanced = !isVisible;
					ViewModel.SaveFurinaPlayerSettings();
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

		var modeStack = new StackPanel { Spacing = 8 };
		modeStack.Children.Add(new TextBlock
		{
			Text = Loc.T("FurinaPlayer SRC 控制模式"),
			FontSize = 12,
			FontWeight = FontWeights.Bold,
			Foreground = DspTextSubHeaderBrush
		});

		string activeModeKey = (ViewModel?.FurinaPlayerResamplerEnabled ?? false)
			? (ViewModel?.FurinaSrcMode ?? "off")
			: "off";
		var modeOptions = new (string Key, string Label)[]
		{
			("off", Loc.T("关闭")),
			("compatibility48", Loc.T("48 kHz 兼容")),
			("family2x", "2x PCM"),
			("family4x", "4x PCM"),
			("family8x", "8x Ultra")
		};
		_srcModeButtons.Clear();
		modeStack.Children.Add(CreateSegmentedBar(modeOptions, activeModeKey, _srcModeButtons, OnModeSelected));

		_srcModeHint = new TextBlock
		{
			Text = Loc.T("保持源采样率，Bit-perfect 条件不受 FurinaPlayer SRC 影响。"),
			FontSize = 11,
			Foreground = DspTextMutedBrush,
			Margin = new Thickness(2, 0, 0, 0),
			TextWrapping = TextWrapping.Wrap
		};
		modeStack.Children.Add(_srcModeHint);
		Grid.SetColumn(modeStack, 0);
		row1Grid.Children.Add(modeStack);

		var qualityStack = new StackPanel { Spacing = 8 };
		qualityStack.Children.Add(new TextBlock
		{
			Text = Loc.T("质量策略"),
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
		_srcQualityButtons.Clear();
		qualityStack.Children.Add(CreateSegmentedBar(qualityOptions, ViewModel?.FurinaSrcQuality ?? "transparent", _srcQualityButtons, OnQualitySelected));

		_srcQualityHint = new TextBlock
		{
			Text = Loc.T("全精度多相级联参考档，优先极低通带波纹、零染色与极限阻带抑制。"),
			FontSize = 11,
			Foreground = DspTextMutedBrush,
			Margin = new Thickness(2, 0, 0, 0),
			TextWrapping = TextWrapping.Wrap
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

		// Col 0: Filter 1x (< 50kHz sources: 44.1 / 48kHz)
		var col0 = new StackPanel { Spacing = 6 };
		col0.Children.Add(new TextBlock { Text = Loc.T("过滤器 1x (CD/48k 主力级)"), FontSize = 12, FontWeight = FontWeights.SemiBold, Foreground = DspTextSubHeaderBrush });
		_srcFilter1xCombo = new ComboBox { HorizontalAlignment = HorizontalAlignment.Stretch, CornerRadius = new CornerRadius(8) };
		foreach (var spec in FurinaSrcCatalog.Profiles)
		{
			_srcFilter1xCombo.Items.Add(new SrcChoiceItem(spec.Id, $"{spec.DisplayName} · {(spec.Phase == FurinaSrcPhaseType.Minimum ? "MP" : "LP")}"));
		}
		string norm1x = FurinaSrcCatalog.NormalizeProfileId(ViewModel?.FurinaSrcFilter1x, "furina-gauss-long");
		_srcFilter1xCombo.SelectedItem = FindChoiceById(_srcFilter1xCombo, norm1x);
		_srcFilter1xCombo.SelectionChanged += (s, e) =>
		{
			if (_srcSyncingUi || ViewModel == null) return;
			if (_srcFilter1xCombo.SelectedItem is SrcChoiceItem item)
			{
				ViewModel.FurinaSrcFilter1x = item.Id;
				ViewModel.SaveFurinaPlayerSettings();
				ViewModel.ApplySrcSettings(reopenIfRateChanged: false);
				UpdateFilter1xHint(item.Id);
				RefreshSrcUpsamplingState();
				UpdateFilterDesignCurve();
			}
		};
		col0.Children.Add(_srcFilter1xCombo);
		_srcFilter1xHint = new TextBlock { FontSize = 11, Foreground = DspTextMutedBrush, TextWrapping = TextWrapping.Wrap };
		UpdateFilter1xHint(norm1x);
		col0.Children.Add(_srcFilter1xHint);
		Grid.SetColumn(col0, 0);
		row2Grid.Children.Add(col0);

		// Col 1: Filter Nx (>= 50kHz high-rate cascade stages)
		var col1 = new StackPanel { Spacing = 6 };
		col1.Children.Add(new TextBlock { Text = Loc.T("过滤器 Nx (88.2k+ 级联载波级)"), FontSize = 12, FontWeight = FontWeights.SemiBold, Foreground = DspTextSubHeaderBrush });
		_srcFilterNxCombo = new ComboBox { HorizontalAlignment = HorizontalAlignment.Stretch, CornerRadius = new CornerRadius(8) };
		foreach (var spec in FurinaSrcCatalog.Profiles)
		{
			_srcFilterNxCombo.Items.Add(new SrcChoiceItem(spec.Id, $"{spec.DisplayName} · {(spec.Phase == FurinaSrcPhaseType.Minimum ? "MP" : "LP")}"));
		}
		string normNx = FurinaSrcCatalog.NormalizeProfileId(ViewModel?.FurinaSrcFilterNx, "furina-hb");
		_srcFilterNxCombo.SelectedItem = FindChoiceById(_srcFilterNxCombo, normNx);
		_srcFilterNxCombo.SelectionChanged += (s, e) =>
		{
			if (_srcSyncingUi || ViewModel == null) return;
			if (_srcFilterNxCombo.SelectedItem is SrcChoiceItem item)
			{
				ViewModel.FurinaSrcFilterNx = item.Id;
				ViewModel.SaveFurinaPlayerSettings();
				ViewModel.ApplySrcSettings(reopenIfRateChanged: false);
				UpdateFilterNxHint(item.Id);
				RefreshSrcUpsamplingState();
				UpdateFilterDesignCurve();
			}
		};
		col1.Children.Add(_srcFilterNxCombo);
		_srcFilterNxHint = new TextBlock { FontSize = 11, Foreground = DspTextMutedBrush, TextWrapping = TextWrapping.Wrap };
		UpdateFilterNxHint(normNx);
		col1.Children.Add(_srcFilterNxHint);
		Grid.SetColumn(col1, 1);
		row2Grid.Children.Add(col1);

		// Col 2: Compute Backend
		var col2 = new StackPanel { Spacing = 6 };
		col2.Children.Add(new TextBlock { Text = Loc.T("SIMD 硬件计算后端"), FontSize = 12, FontWeight = FontWeights.SemiBold, Foreground = DspTextSubHeaderBrush });
		_srcComputeCombo = new ComboBox { HorizontalAlignment = HorizontalAlignment.Stretch, CornerRadius = new CornerRadius(8) };
		_srcComputeCombo.Items.Add(new SrcChoiceItem("simd", "CPU AVX2 / SIMD 实时矢量"));
		_srcComputeCombo.Items.Add(new SrcChoiceItem("simd-mt", "CPU SIMD 多核双声道并发"));
		string normCompute = FurinaSrcCatalog.ParseComputeBackend(ViewModel?.FurinaSrcCompute) == FurinaComputeBackend.CpuMultiThread ? "simd-mt" : "simd";
		_srcComputeCombo.SelectedItem = FindChoiceById(_srcComputeCombo, normCompute);
		_srcComputeCombo.SelectionChanged += (s, e) =>
		{
			if (_srcSyncingUi || ViewModel == null) return;
			if (_srcComputeCombo.SelectedItem is SrcChoiceItem item)
			{
				ViewModel.FurinaSrcCompute = item.Id;
				ViewModel.SaveFurinaPlayerSettings();
				ViewModel.ApplySrcSettings(reopenIfRateChanged: false);
				UpdateComputeHint(item.Id);
				RefreshSrcUpsamplingState();
			}
		};
		col2.Children.Add(_srcComputeCombo);
		_srcComputeHint = new TextBlock { FontSize = 11, Foreground = DspTextMutedBrush, TextWrapping = TextWrapping.Wrap };
		UpdateComputeHint(normCompute);
		col2.Children.Add(_srcComputeHint);
		Grid.SetColumn(col2, 2);
		row2Grid.Children.Add(col2);

		// Col 3: Dither & Noise Shaping (6 modes matching & surpassing audiophile mastering chains)
		var col3 = new StackPanel { Spacing = 6 };
		col3.Children.Add(new TextBlock { Text = "PCM Dither / Noise Shaping", FontSize = 12, FontWeight = FontWeights.SemiBold, Foreground = DspTextSubHeaderBrush });
		_srcDitherCombo = new ComboBox { HorizontalAlignment = HorizontalAlignment.Stretch, CornerRadius = new CornerRadius(8) };
		_srcDitherCombo.Items.Add(new SrcChoiceItem("off", "Off (Float32 无损直出)"));
		_srcDitherCombo.Items.Add(new SrcChoiceItem("tpdf", "TPDF (标准 24-bit 三角抖动)"));
		_srcDitherCombo.Items.Add(new SrcChoiceItem("highpass-tpdf", "High-Pass TPDF (高通偏置)"));
		_srcDitherCombo.Items.Add(new SrcChoiceItem("ns-5", "NS-5 (5阶心理声学噪声整形)"));
		_srcDitherCombo.Items.Add(new SrcChoiceItem("ns-9", "NS-9 (9阶母带级深度整形)"));
		_srcDitherCombo.Items.Add(new SrcChoiceItem("ultra-shaped", "Ultra-Shaped (超高频段终极整形)"));

		string normDither = NormalizeDitherId(ViewModel?.FurinaSrcDither);
		_srcDitherCombo.SelectedItem = FindChoiceById(_srcDitherCombo, normDither);
		_srcDitherCombo.SelectionChanged += (s, e) =>
		{
			if (_srcSyncingUi || ViewModel == null) return;
			if (_srcDitherCombo.SelectedItem is SrcChoiceItem item)
			{
				ViewModel.FurinaSrcDither = item.Id;
				ViewModel.SaveFurinaPlayerSettings();
				ViewModel.ApplySrcSettings(reopenIfRateChanged: false);
				UpdateDitherHint(item.Id);
				RefreshSrcUpsamplingState();
			}
		};
		col3.Children.Add(_srcDitherCombo);
		_srcDitherHint = new TextBlock { FontSize = 11, Foreground = DspTextMutedBrush, TextWrapping = TextWrapping.Wrap };
		UpdateDitherHint(normDither);
		col3.Children.Add(_srcDitherHint);
		Grid.SetColumn(col3, 3);
		row2Grid.Children.Add(col3);

		stack.Children.Add(row2Grid);

		// --- Advanced Ladder Collapsible Panel ---
		_srcAdvancedPanel = CreateAdvancedLadderPanel();
		_srcAdvancedPanel.Visibility = advOpen ? Visibility.Visible : Visibility.Collapsed;
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
		titleSp.Children.Add(new TextBlock { Text = Loc.T("进阶调谐阶梯"), FontSize = 12, FontWeight = FontWeights.Bold, Foreground = DspTextSubHeaderBrush });
		titleSp.Children.Add(new TextBlock { Text = Loc.T("一键配置级联倍率、1x/Nx 滤波器组合与噪声整形策略"), FontSize = 11, Foreground = DspTextMutedBrush, VerticalAlignment = VerticalAlignment.Center });
		Grid.SetColumn(titleSp, 0);
		titleRow.Children.Add(titleSp);

		int simdBits = Vector.IsHardwareAccelerated ? Vector<float>.Count * 32 : 32;
		var simdBadge = new TextBlock { Text = $"Furina FIR · {simdBits}-bit SIMD", FontSize = 11, FontFamily = new FontFamily("Consolas, Segoe UI"), Foreground = DspTextMutedBrush };
		Grid.SetColumn(simdBadge, 1);
		titleRow.Children.Add(simdBadge);
		stack.Children.Add(titleRow);

		var ladderGrid = new Grid { ColumnSpacing = 12 };
		for (int i = 0; i < 4; i++)
		{
			ladderGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.0, GridUnitType.Star) });
		}

		ladderGrid.Children.Add(CreateLadderCard(0, "Realtime Safe", "2x · Gauss Short · Halfband · LowLatency", "< 0.4 ms · 低负载", () =>
		{
			ApplyLadderPreset("family2x", "lowLatency", "furina-gauss-short", "furina-hb", "simd", "off");
		}));

		ladderGrid.Children.Add(CreateLadderCard(1, "Musical Reference", "4x · Gauss Long · Halfband · TPDF", "~0.9 ms · 均衡润泽", () =>
		{
			ApplyLadderPreset("family4x", "balanced", "furina-gauss-long", "furina-hb", "simd", "tpdf");
		}));

		ladderGrid.Children.Add(CreateLadderCard(2, "Zero Pre-Ring Attack", "4x · Apodizing MinRing · XTR MP · NS-5", "0 前振铃 · 极致起音", () =>
		{
			ApplyLadderPreset("family4x", "transparent", "furina-apod-minring", "furina-xtr-mp", "simd-mt", "ns-5");
		}));

		ladderGrid.Children.Add(CreateLadderCard(3, "384kHz Studio Mastering", "8x · Steep3 Ultra · Steep XL · NS-9", ">175 dB · 旗舰级", () =>
		{
			ApplyLadderPreset("family8x", "transparent", "furina-steep3-long", "furina-steep-xl", "simd-mt", "ns-9");
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

	private void ApplyLadderPreset(string mode, string quality, string f1xId, string fnxId, string computeId, string ditherId)
	{
		if (ViewModel == null) return;
		_srcSyncingUi = true;
		try
		{
			ViewModel.FurinaSrcMode = mode;
			ViewModel.FurinaSrcQuality = quality;
			ViewModel.FurinaSrcFilter1x = f1xId;
			ViewModel.FurinaSrcFilterNx = fnxId;
			ViewModel.FurinaSrcCompute = computeId;
			ViewModel.FurinaSrcDither = ditherId;

			int inputRate = ViewModel.EngineSampleRate > 0 ? ViewModel.EngineSampleRate : 44100;
			ViewModel.FurinaPlayerResamplerEnabled = mode != "off";
			ViewModel.FurinaPlayerResamplerTargetRate = FurinaSrcCatalog.ResolveTargetSampleRate(inputRate, mode, ViewModel.FurinaPlayerResamplerTargetRate);

			UpdateSegmentedSelection(_srcModeButtons, mode);
			UpdateSegmentedSelection(_srcQualityButtons, quality);
			if (_srcFilter1xCombo != null) _srcFilter1xCombo.SelectedItem = FindChoiceById(_srcFilter1xCombo, f1xId);
			if (_srcFilterNxCombo != null) _srcFilterNxCombo.SelectedItem = FindChoiceById(_srcFilterNxCombo, fnxId);
			if (_srcComputeCombo != null) _srcComputeCombo.SelectedItem = FindChoiceById(_srcComputeCombo, computeId);
			if (_srcDitherCombo != null) _srcDitherCombo.SelectedItem = FindChoiceById(_srcDitherCombo, ditherId);

			UpdateFilter1xHint(f1xId);
			UpdateFilterNxHint(fnxId);
			UpdateComputeHint(computeId);
			UpdateDitherHint(ditherId);
		}
		finally
		{
			_srcSyncingUi = false;
		}

		ViewModel.SaveFurinaPlayerSettings();
		ViewModel.ApplySrcSettings(reopenIfRateChanged: true);
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
		mainGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(290, GridUnitType.Pixel) });

		// --- Left Section: Real FFT & Impulse Canvas ---
		var leftStack = new StackPanel { Spacing = 10, Margin = new Thickness(0, 0, 32, 0) };

		var headGrid = new Grid();
		headGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.0, GridUnitType.Star) });
		headGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

		var titleSp = new StackPanel { Spacing = 2 };
		titleSp.Children.Add(new TextBlock
		{
			Text = "FILTER : REAL-TIME FFT & IMPULSE ANALYSIS",
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

		var badgeRow = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8, VerticalAlignment = VerticalAlignment.Center };

		// Chart view switch (Magnitude dB vs Impulse Time-Domain)
		_srcChartModeButtons.Clear();
		var chartOptions = new (string Key, string Label)[]
		{
			("mag", Loc.T("幅频响应 (dB)")),
			("imp", Loc.T("脉冲响应 (Impulse)"))
		};
		badgeRow.Children.Add(CreateSegmentedBar(chartOptions, "mag", _srcChartModeButtons, key =>
		{
			_srcShowImpulseMode = key == "imp";
			UpdateFilterDesignCurve();
		}));

		_srcPhaseBadgeText = CreateStatPill("Linear phase", badgeRow);
		_srcTapsBadgeText = CreateStatPill("191 Taps", badgeRow);
		_srcAttenBadgeText = CreateStatPill("-145 dB", badgeRow);
		_srcDelayBadgeText = CreateStatPill("1.0 ms", badgeRow);

		Grid.SetColumn(badgeRow, 1);
		headGrid.Children.Add(badgeRow);
		leftStack.Children.Add(headGrid);

		var chartViewbox = new Viewbox
		{
			Stretch = Stretch.Uniform,
			Height = 195,
			HorizontalAlignment = HorizontalAlignment.Stretch
		};
		_srcFilterCurveCanvas = new Canvas { Width = 720, Height = 220 };
		chartViewbox.Child = _srcFilterCurveCanvas;
		leftStack.Children.Add(chartViewbox);

		_srcFilterCaptionText = new TextBlock
		{
			Text = Loc.T("实测 FIR 卷积核 FFT 频域与时域冲击响应，直接反映所选质量策略与相位特性（Minimum Phase 零前振铃）。"),
			FontSize = 11,
			Foreground = DspTextMutedBrush,
			TextWrapping = TextWrapping.Wrap,
			Margin = new Thickness(4, 2, 4, 0)
		};
		leftStack.Children.Add(_srcFilterCaptionText);

		Grid.SetColumn(leftStack, 0);
		mainGrid.Children.Add(leftStack);

		// --- Right Section: Active Filter Plan 1x / Nx ---
		var rightBorder = new Border
		{
			BorderBrush = CreateLiquidGlassRimBrush(),
			BorderThickness = new Thickness(1, 0, 0, 0),
			Padding = new Thickness(24, 0, 0, 0)
		};
		var rightStack = new StackPanel { Spacing = 14 };

		rightStack.Children.Add(new TextBlock
		{
			Text = "Filter Plan (1x / Nx Cascade)",
			FontSize = 12,
			FontWeight = FontWeights.Bold,
			Foreground = DspTextMutedBrush
		});

		_srcFilterPlan1xSlot = new Border { Padding = new Thickness(0, 0, 0, 10) };
		var f1xSp = new StackPanel { Spacing = 4 };
		var f1xHeader = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 6 };
		f1xHeader.Children.Add(new TextBlock { Text = "1X (< 50kHz)", FontSize = 11, FontWeight = FontWeights.Bold, FontFamily = new FontFamily("Consolas, Segoe UI"), Foreground = DspTextMutedBrush });
		_srcFilterPlan1xStatus = new TextBlock { Text = Loc.T("· 激活"), FontSize = 10.5, FontWeight = FontWeights.SemiBold, Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 16, 185, 129)) };
		f1xHeader.Children.Add(_srcFilterPlan1xStatus);
		f1xSp.Children.Add(f1xHeader);

		_srcFilterPlan1xName = new TextBlock { Text = "FurinaPlayer Gauss Long", FontSize = 14, FontWeight = FontWeights.SemiBold, Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 37, 99, 235)) };
		f1xSp.Children.Add(_srcFilterPlan1xName);

		_srcFilterPlan1xDesc = new TextBlock { FontSize = 11, Foreground = DspTextMutedBrush, TextWrapping = TextWrapping.Wrap };
		f1xSp.Children.Add(_srcFilterPlan1xDesc);

		_srcFilterPlan1xPhase = new TextBlock { Text = "Linear phase", FontSize = 11, FontFamily = new FontFamily("Consolas, Segoe UI"), Foreground = DspTextMutedBrush };
		f1xSp.Children.Add(_srcFilterPlan1xPhase);
		_srcFilterPlan1xSlot.Child = f1xSp;
		rightStack.Children.Add(_srcFilterPlan1xSlot);

		rightStack.Children.Add(new Rectangle { Height = 1, Fill = CreateLiquidGlassRimBrush(), HorizontalAlignment = HorizontalAlignment.Stretch });

		_srcFilterPlanNxSlot = new Border { Padding = new Thickness(0, 10, 0, 0) };
		var fnxSp = new StackPanel { Spacing = 4 };
		var fnxHeader = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 6 };
		fnxHeader.Children.Add(new TextBlock { Text = "NX (88.2k - 384k)", FontSize = 11, FontWeight = FontWeights.Bold, FontFamily = new FontFamily("Consolas, Segoe UI"), Foreground = DspTextMutedBrush });
		_srcFilterPlanNxStatus = new TextBlock { Text = Loc.T("· 级联待命"), FontSize = 10.5, FontWeight = FontWeights.SemiBold, Foreground = DspTextMutedBrush };
		fnxHeader.Children.Add(_srcFilterPlanNxStatus);
		fnxSp.Children.Add(fnxHeader);

		_srcFilterPlanNxName = new TextBlock { Text = "FurinaPlayer Halfband", FontSize = 14, FontWeight = FontWeights.SemiBold, Foreground = DspTextPrimaryBrush };
		fnxSp.Children.Add(_srcFilterPlanNxName);

		_srcFilterPlanNxDesc = new TextBlock { FontSize = 11, Foreground = DspTextMutedBrush, TextWrapping = TextWrapping.Wrap };
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

	private TextBlock CreateStatPill(string initialText, StackPanel parent)
	{
		var border = new Border
		{
			Background = DspSoftSurfaceBrush,
			BorderBrush = CreateLiquidGlassRimBrush(),
			BorderThickness = new Thickness(1),
			CornerRadius = new CornerRadius(6),
			Padding = new Thickness(8, 3, 8, 3),
			VerticalAlignment = VerticalAlignment.Center
		};
		var tb = new TextBlock
		{
			Text = initialText,
			FontSize = 11,
			FontFamily = new FontFamily("Consolas, Segoe UI"),
			Foreground = DspTextSubHeaderBrush
		};
		border.Child = tb;
		parent.Children.Add(border);
		return tb;
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
				FontSize = 12.5
			};

			ApplySegmentedButtonStyle(btn, isSelected);

			btn.Click += (s, e) =>
			{
				string key = btn.Tag?.ToString() ?? "";
				onSelect(key);
				UpdateSegmentedSelection(buttonRegistry, key);
			};

			buttonRegistry.Add(btn);
			Grid.SetColumn(btn, i);
			grid.Children.Add(btn);
		}

		container.Child = grid;
		return container;
	}

	private void UpdateSegmentedSelection(List<Button> buttonRegistry, string activeKey)
	{
		foreach (var btn in buttonRegistry)
		{
			bool isCurrent = string.Equals(btn.Tag?.ToString(), activeKey, StringComparison.OrdinalIgnoreCase);
			ApplySegmentedButtonStyle(btn, isCurrent);
		}
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
		ViewModel.FurinaPlayerResamplerEnabled = mode != "off";
		if (mode != "off")
		{
			ViewModel.FurinaPlayerResamplerTargetRate = FurinaSrcCatalog.ResolveTargetSampleRate(inputRate, mode, ViewModel.FurinaPlayerResamplerTargetRate);
		}

		ViewModel.SaveFurinaPlayerSettings();
		ViewModel.ApplySrcSettings(reopenIfRateChanged: true);
		RefreshSrcUpsamplingState();
		UpdateFilterDesignCurve();
	}

	private void OnQualitySelected(string quality)
	{
		if (ViewModel == null) return;
		ViewModel.FurinaSrcQuality = quality;
		ViewModel.SaveFurinaPlayerSettings();
		ViewModel.ApplySrcSettings(reopenIfRateChanged: false);
		RefreshSrcUpsamplingState();
		UpdateFilterDesignCurve();
	}

	private void RefreshSrcUpsamplingState()
	{
		string mode = ViewModel?.FurinaSrcMode ?? "off";
		string quality = ViewModel?.FurinaSrcQuality ?? "transparent";
		bool enabled = (ViewModel?.FurinaPlayerResamplerEnabled ?? false) && mode != "off";

		var telemetry = ViewModel?.GetSrcTelemetry();
		int inputRate = telemetry != null && telemetry.SourceSampleRate > 0
			? telemetry.SourceSampleRate
			: ((ViewModel != null && ViewModel.EngineSampleRate > 0) ? ViewModel.EngineSampleRate : 44100);

		int desiredRate = enabled
			? FurinaSrcCatalog.ResolveTargetSampleRate(inputRate, mode, ViewModel?.FurinaPlayerResamplerTargetRate ?? 192000)
			: inputRate;
		int liveTargetRate = enabled && telemetry != null && telemetry.OutputDeviceSampleRate > 0
			? telemetry.OutputDeviceSampleRate
			: desiredRate;

		bool abBypass = ViewModel?.FurinaSrcAbBenchmark ?? false;

		if (!enabled)
		{
			if (_srcStatusDot != null) _srcStatusDot.Fill = DspTextMutedBrush;
			if (_srcStatusText != null) _srcStatusText.Text = Loc.T("原生直通");
			if (_srcSourceRateText != null) _srcSourceRateText.Text = $"{inputRate / 1000.0:0.#} kHz";
			if (_srcTargetRateText != null) _srcTargetRateText.Text = "--";
			if (_srcRatioBadge != null) _srcRatioBadge.Text = "";
			if (_srcStep1Val != null) _srcStep1Val.Text = Loc.T("关闭");
			if (_srcStep3Val != null) _srcStep3Val.Text = "--";
			if (_srcFilterSlotValue != null) _srcFilterSlotValue.Text = "Standby";
			if (_srcFallbackNoticeText != null) _srcFallbackNoticeText.Visibility = Visibility.Collapsed;
		}
		else
		{
			if (_srcStatusDot != null)
			{
				_srcStatusDot.Fill = new SolidColorBrush(abBypass
					? Windows.UI.Color.FromArgb(255, 245, 158, 11)
					: Windows.UI.Color.FromArgb(255, 16, 185, 129));
			}
			if (_srcStatusText != null)
			{
				_srcStatusText.Text = abBypass
					? Loc.T("A/B 盲听对比 · 标准线性直通")
					: Loc.T("升频激活") + (telemetry != null ? $" · {telemetry.StageSummary}" : "");
			}
			if (_srcSourceRateText != null) _srcSourceRateText.Text = $"{inputRate / 1000.0:0.#} kHz";
			if (_srcTargetRateText != null) _srcTargetRateText.Text = $"{liveTargetRate / 1000.0:0.#} kHz";

			double ratio = (double)liveTargetRate / Math.Max(1, inputRate);
			if (_srcRatioBadge != null) _srcRatioBadge.Text = $"×{ratio:0.##}";

			string modeLabel = mode switch
			{
				"compatibility48" => Loc.T("48 kHz 兼容"),
				"family2x" => "2x PCM",
				"family4x" => "4x PCM",
				"family8x" => "8x Ultra",
				_ => $"{liveTargetRate / 1000.0:0.#}k Custom"
			};
			if (_srcStep1Val != null) _srcStep1Val.Text = modeLabel;
			if (_srcStep3Val != null) _srcStep3Val.Text = $"{ratio:0.##}x · {telemetry?.StageCount ?? 1} Stage";

			string activeId = inputRate < 50000 ? (ViewModel?.FurinaSrcFilter1x ?? "furina-gauss-long") : (ViewModel?.FurinaSrcFilterNx ?? "furina-hb");
			var spec = FurinaSrcCatalog.GetProfile(activeId);
			string slotPrefix = inputRate < 50000 ? "1X" : "NX";
			if (_srcFilterSlotValue != null) _srcFilterSlotValue.Text = $"{slotPrefix} · {spec.Id.ToUpperInvariant()}";

			if (_srcFallbackNoticeText != null)
			{
				if (telemetry != null && telemetry.IsFallbackRate)
				{
					_srcFallbackNoticeText.Text = Loc.F("硬件上限协商至 {0:0.#} kHz", liveTargetRate / 1000.0);
					_srcFallbackNoticeText.Visibility = Visibility.Visible;
				}
				else
				{
					_srcFallbackNoticeText.Visibility = Visibility.Collapsed;
				}
			}
		}

		// Quality & SIMD Precision Readout
		int simdBits = telemetry?.SimdWidthBits > 0
			? telemetry.SimdWidthBits
			: (Vector.IsHardwareAccelerated ? Vector<float>.Count * 32 : 32);
		var qualEnum = FurinaSrcCatalog.ParseQuality(quality);
		int sampleTaps = telemetry != null && telemetry.TotalTaps > 0
			? telemetry.TotalTaps
			: FurinaSrcCatalog.ResolveRuntimeTapCount(FurinaSrcCatalog.GetProfile(ViewModel?.FurinaSrcFilter1x), qualEnum, 0);

		string telemetryTail = telemetry != null && telemetry.Active
			? $" · {telemetry.ProcessTimeMs:F2}ms ({Math.Max(0, 100.0 - telemetry.CpuLoadPercent):F0}% Headroom)"
			: $" · {simdBits}-bit Vector<float>";
		if (_srcPrecisionValue != null)
		{
			_srcPrecisionValue.Text = qualEnum switch
			{
				FurinaSrcQualityProfile.Balanced => $"Furina FIR Balanced · {sampleTaps} Taps{telemetryTail}",
				FurinaSrcQualityProfile.LowLatency => $"Furina FIR Realtime · {sampleTaps} Taps{telemetryTail}",
				_ => $"Furina FIR Reference · {sampleTaps} Taps{telemetryTail}"
			};
		}

		if (_srcStep2Val != null)
		{
			_srcStep2Val.Text = quality switch
			{
				"balanced" => "Balanced",
				"lowLatency" => "Low latency",
				_ => "Transparent"
			};
		}

		if (_srcModeHint != null)
		{
			_srcModeHint.Text = mode switch
			{
				"compatibility48" => Loc.T("将 PCM 固定转换到 48 kHz，适合只接受 48 kHz 的独占耳机或声卡；默认关闭。"),
				"family2x" => Loc.T("44.1k 家族升到 88.2k，48k 家族升到 96k（多相 2x 级联）。"),
				"family4x" => Loc.T("44.1k 家族升到 176.4k，48k 家族升到 192k（4x 两级联 FIR）。"),
				"family8x" => Loc.T("实验档：44.1k 家族升到 352.8k，48k 家族升到 384k（8x 三级联；若 DAC 不支持则自动降阶协商）。"),
				_ => Loc.T("保持源采样率，Bit-perfect 条件不受 FurinaPlayer SRC 影响。")
			};
		}

		if (_srcQualityHint != null)
		{
			_srcQualityHint.Text = quality switch
			{
				"balanced" => Loc.T("均衡级多相 FIR 档位，兼顾低延迟、温和 CPU 负载与高阻带抑制。"),
				"lowLatency" => Loc.T("降低 SRC 开销，适合低延迟输出。"),
				_ => Loc.T("全精度多相级联参考档，优先极低通带波纹、零染色与极限阻带抑制。")
			};
		}

		string compute = ViewModel?.FurinaSrcCompute ?? "simd";
		bool isMt = FurinaSrcCatalog.ParseComputeBackend(compute) == FurinaComputeBackend.CpuMultiThread;
		if (_srcStep4Val != null)
		{
			_srcStep4Val.Text = isMt
				? $"{simdBits}-bit SIMD Multi-Thread"
				: $"{simdBits}-bit SIMD Vector<float>";
		}

		string ditherId = NormalizeDitherId(ViewModel?.FurinaSrcDither);
		if (_srcDitherSlotValue != null)
		{
			_srcDitherSlotValue.Text = ditherId switch
			{
				"off" => Loc.T("关闭"),
				"tpdf" => "TPDF 24-bit",
				"highpass-tpdf" => "HP-TPDF",
				"ns-5" => "NS-5 (5阶整形)",
				"ns-9" => "NS-9 (9阶整形)",
				"ultra-shaped" => "Ultra-Shaped",
				_ => ditherId
			};
		}

		bool is1xActive = inputRate < 50000;
		if (_srcFilterPlan1xStatus != null)
		{
			_srcFilterPlan1xStatus.Text = is1xActive ? Loc.T("· 当前激活") : Loc.T("· 首级待命");
			_srcFilterPlan1xStatus.Foreground = new SolidColorBrush(is1xActive
				? Windows.UI.Color.FromArgb(255, 16, 185, 129)
				: Windows.UI.Color.FromArgb(255, 148, 163, 184));
		}
		if (_srcFilterPlanNxStatus != null)
		{
			_srcFilterPlanNxStatus.Text = !is1xActive ? Loc.T("· 当前激活") : (enabled && liveTargetRate > inputRate * 2 ? Loc.T("· 多级联激活") : Loc.T("· 级联待命"));
			_srcFilterPlanNxStatus.Foreground = new SolidColorBrush((!is1xActive || (enabled && liveTargetRate > inputRate * 2))
				? Windows.UI.Color.FromArgb(255, 16, 185, 129)
				: Windows.UI.Color.FromArgb(255, 148, 163, 184));
		}
	}

	private void UpdateFilter1xHint(string filterId)
	{
		if (_srcFilter1xHint == null) return;
		var spec = FurinaSrcCatalog.GetProfile(filterId, "furina-gauss-long");
		_srcFilter1xHint.Text = $"{spec.Description} ({spec.AttenuationDb:F0} dB · {(spec.Phase == FurinaSrcPhaseType.Minimum ? "Zero Pre-Ring MP" : "Linear Phase")})";
	}

	private void UpdateFilterNxHint(string filterId)
	{
		if (_srcFilterNxHint == null) return;
		var spec = FurinaSrcCatalog.GetProfile(filterId, "furina-hb");
		_srcFilterNxHint.Text = $"{spec.Description} ({spec.AttenuationDb:F0} dB · {(spec.Phase == FurinaSrcPhaseType.Minimum ? "MP" : "LP")})";
	}

	private void UpdateComputeHint(string computeId)
	{
		bool isMt = FurinaSrcCatalog.ParseComputeBackend(computeId) == FurinaComputeBackend.CpuMultiThread;
		int bits = Vector.IsHardwareAccelerated ? Vector<float>.Count * 32 : 32;
		if (_srcComputeHint != null)
		{
			_srcComputeHint.Text = isMt
				? $"启用 {bits}-bit SIMD 向量指令 + 左右声道并发执行，适合 352.8k/384k 高倍率级联。"
				: $"启用硬件 {bits}-bit SIMD (Vector<float>) 零堆分配单周期点积，稳定低发热且无线程切换抖动。";
		}
	}

	private void UpdateDitherHint(string ditherId)
	{
		if (_srcDitherHint == null) return;
		_srcDitherHint.Text = NormalizeDitherId(ditherId) switch
		{
			"tpdf" => "24-bit 标准三角分布抖动，彻底打散极微弱信号下的量化谐波失真。",
			"highpass-tpdf" => "1 阶高通噪声整形：将极微弱抖动能量推向 15 kHz 以上人耳不敏感区。",
			"ns-5" => "5 阶 Wannamaker 心理声学噪声整形，在 1-5 kHz 敏感带压低本底噪至 <-150 dB。",
			"ns-9" => "9 阶 Fletcher-Munson 曲线深度噪声整形，提供母带参照级超低听感噪底。",
			"ultra-shaped" => "专为 88.2k-384k 高采样 DAC 调制器调校的超频段 9 阶复合噪声整形。",
			_ => "保持 Float32 链无损直出：默认关闭，避免对 32-bit DAC 施加冗余加噪。"
		};
	}

	private void UpdateFilterDesignCurve()
	{
		try
		{
			if (_srcFilterCurveCanvas == null) return;
			_srcFilterCurveCanvas.Children.Clear();

			int inputRate = (ViewModel != null && ViewModel.EngineSampleRate > 0) ? ViewModel.EngineSampleRate : 44100;
			var quality = FurinaSrcCatalog.ParseQuality(ViewModel?.FurinaSrcQuality);

			string f1xId = FurinaSrcCatalog.NormalizeProfileId(ViewModel?.FurinaSrcFilter1x, "furina-gauss-long");
			string fnxId = FurinaSrcCatalog.NormalizeProfileId(ViewModel?.FurinaSrcFilterNx, "furina-hb");

			var spec1x = FurinaSrcCatalog.GetProfile(f1xId, "furina-gauss-long");
			var specNx = FurinaSrcCatalog.GetProfile(fnxId, "furina-hb");

			if (_srcFilterPlan1xName != null) _srcFilterPlan1xName.Text = spec1x.DisplayName;
			if (_srcFilterPlanNxName != null) _srcFilterPlanNxName.Text = specNx.DisplayName;
			if (_srcFilterPlan1xDesc != null) _srcFilterPlan1xDesc.Text = spec1x.Description;
			if (_srcFilterPlanNxDesc != null) _srcFilterPlanNxDesc.Text = specNx.Description;
			if (_srcFilterPlan1xPhase != null) _srcFilterPlan1xPhase.Text = $"{(spec1x.Phase == FurinaSrcPhaseType.Minimum ? "Minimum phase" : "Linear phase")} · {FurinaSrcCatalog.ResolveRuntimeTapCount(spec1x, quality, 0)} taps · {spec1x.AttenuationDb:F0} dB";
			if (_srcFilterPlanNxPhase != null) _srcFilterPlanNxPhase.Text = $"{(specNx.Phase == FurinaSrcPhaseType.Minimum ? "Minimum phase" : "Linear phase")} · {FurinaSrcCatalog.ResolveRuntimeTapCount(specNx, quality, 1)} taps · {specNx.AttenuationDb:F0} dB";

			var activeSpec = inputRate < 50000 ? spec1x : specNx;
			var analysis = FurinaSrcCatalog.AnalyzeFilter(activeSpec.Id, quality, inputRate);

			if (_srcFilterNameText != null) _srcFilterNameText.Text = $"{activeSpec.DisplayName} ({activeSpec.Id})";
			if (_srcPhaseBadgeText != null) _srcPhaseBadgeText.Text = activeSpec.Phase == FurinaSrcPhaseType.Minimum ? "Minimum phase (Zero Pre-Ring)" : "Linear phase";
			if (_srcTapsBadgeText != null) _srcTapsBadgeText.Text = $"{analysis.AnalyzedTaps} Taps";
			if (_srcAttenBadgeText != null) _srcAttenBadgeText.Text = $"-{analysis.StopbandAttenuationDb:F0} dB";
			if (_srcDelayBadgeText != null) _srcDelayBadgeText.Text = $"{analysis.GroupDelayMsAt44k:F2} ms";

			var gridBrush = new SolidColorBrush(Windows.UI.Color.FromArgb(100, 226, 232, 240));
			var monoFont = new FontFamily("Consolas, Segoe UI");
			var labelBrush = DspTextMutedBrush;

			const double xLeft = 52;
			const double xRight = 684;
			const double yTop = 24;
			const double yBottom = 188;
			double chartWidth = xRight - xLeft;
			double chartHeight = yBottom - yTop;

			for (int i = 0; i <= 6; i++)
			{
				double vx = xLeft + chartWidth * i / 6.0;
				_srcFilterCurveCanvas.Children.Add(new Line { X1 = vx, Y1 = yTop, X2 = vx, Y2 = yBottom, Stroke = gridBrush, StrokeThickness = 1 });
			}
			for (int i = 0; i <= 4; i++)
			{
				double hy = yTop + chartHeight * i / 4.0;
				_srcFilterCurveCanvas.Children.Add(new Line { X1 = xLeft, Y1 = hy, X2 = xRight, Y2 = hy, Stroke = gridBrush, StrokeThickness = 1 });
			}

			if (_srcShowImpulseMode)
			{
				// --- Impulse Time-Domain Response from actual FIR kernel ---
				double[] imp = analysis.ImpulseResponse;
				double zeroY = yTop + chartHeight * 0.72;
				_srcFilterCurveCanvas.Children.Add(new Line
				{
					X1 = xLeft, Y1 = zeroY, X2 = xRight, Y2 = zeroY,
					Stroke = new SolidColorBrush(Windows.UI.Color.FromArgb(160, 148, 163, 184)),
					StrokeThickness = 1,
					StrokeDashArray = new DoubleCollection { 4, 4 }
				});

				// Window around main impulse peak so both linear symmetry & min-phase causal decay are clear
				int peakIdx = 0;
				double peakVal = -1;
				for (int i = 0; i < imp.Length; i++)
				{
					if (Math.Abs(imp[i]) > peakVal) { peakVal = Math.Abs(imp[i]); peakIdx = i; }
				}

				int halfWin = Math.Min(imp.Length / 2, 48);
				int startIdx = activeSpec.Phase == FurinaSrcPhaseType.Minimum ? 0 : Math.Max(0, peakIdx - halfWin);
				int viewCount = Math.Min(imp.Length - startIdx, halfWin * 2 + 1);

				var polylinePoints = new PointCollection();
				var fillFig = new PathFigure { StartPoint = new Point(xLeft, zeroY), IsClosed = true, IsFilled = true };

				for (int i = 0; i < viewCount; i++)
				{
					double x = xLeft + chartWidth * i / Math.Max(1, viewCount - 1);
					double val = imp[startIdx + i]; // -0.25 .. +1.0
					double y = Math.Clamp(zeroY - val * (chartHeight * 0.65), yTop, yBottom);
					var pt = new Point(x, y);
					polylinePoints.Add(pt);
					fillFig.Segments.Add(new LineSegment { Point = pt });

					if (i == viewCount - 1)
					{
						fillFig.Segments.Add(new LineSegment { Point = new Point(x, zeroY) });
					}
				}

				var fillGeo = new PathGeometry();
				fillGeo.Figures.Add(fillFig);
				_srcFilterCurveCanvas.Children.Add(new Path { Data = fillGeo, Fill = new SolidColorBrush(Windows.UI.Color.FromArgb(26, 16, 185, 129)) });
				_srcFilterCurveCanvas.Children.Add(new Polyline
				{
					Points = polylinePoints,
					Stroke = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 16, 185, 129)),
					StrokeThickness = 2.2,
					StrokeLineJoin = PenLineJoin.Round
				});

				AddSrcCanvasText(_srcFilterCurveCanvas, "+1.0", 20, yTop + 4, labelBrush, monoFont, 10);
				AddSrcCanvasText(_srcFilterCurveCanvas, "0.0", 24, zeroY - 7, labelBrush, monoFont, 10);
				AddSrcCanvasText(_srcFilterCurveCanvas, "-0.25", 16, yBottom - 10, labelBrush, monoFont, 10);
				AddSrcCanvasText(_srcFilterCurveCanvas, activeSpec.Phase == FurinaSrcPhaseType.Minimum ? "t=0 ms (Zero Pre-Ringing)" : "-Delay (Symmetrical)", xLeft, 195, labelBrush, monoFont, 10.5);
				AddSrcCanvasText(_srcFilterCurveCanvas, activeSpec.Phase == FurinaSrcPhaseType.Minimum ? "+Tail ms" : "+Delay", 620, 195, labelBrush, monoFont, 10.5);
			}
			else
			{
				// --- FFT Frequency Magnitude Response (0 dB down to -160 dB) ---
				const double minDb = -160.0;
				double[] magDb = analysis.MagnitudeDb;
				var pts = new PointCollection();
				var areaFig = new PathFigure { StartPoint = new Point(xLeft, yBottom), IsClosed = true, IsFilled = true };

				for (int i = 0; i < magDb.Length; i++)
				{
					double normX = (double)i / (magDb.Length - 1);
					double x = xLeft + chartWidth * normX;
					double db = Math.Clamp(magDb[i], minDb, 0.0);
					double y = yTop + chartHeight * (-db / (-minDb));
					var pt = new Point(x, y);
					pts.Add(pt);
					areaFig.Segments.Add(new LineSegment { Point = pt });
				}
				areaFig.Segments.Add(new LineSegment { Point = new Point(xRight, yBottom) });

				// Source Nyquist marker at 0.5x stage bandwidth (22.05k / 24kHz)
				double nyqX = xLeft + chartWidth * 0.5;
				_srcFilterCurveCanvas.Children.Add(new Line
				{
					X1 = nyqX, Y1 = yTop, X2 = nyqX, Y2 = yBottom,
					Stroke = new SolidColorBrush(Windows.UI.Color.FromArgb(165, 16, 185, 129)),
					StrokeThickness = 1.2,
					StrokeDashArray = new DoubleCollection { 4, 4 }
				});
				AddSrcCanvasText(_srcFilterCurveCanvas, $"{inputRate * 0.5 / 1000.0:0.##} kHz (Src Nyq)", nyqX + 6, yTop + 4, new SolidColorBrush(Windows.UI.Color.FromArgb(255, 16, 185, 129)), monoFont, 10);

				var areaGeo = new PathGeometry();
				areaGeo.Figures.Add(areaFig);
				_srcFilterCurveCanvas.Children.Add(new Path { Data = areaGeo, Fill = new SolidColorBrush(Windows.UI.Color.FromArgb(26, 37, 99, 235)) });
				_srcFilterCurveCanvas.Children.Add(new Polyline
				{
					Points = pts,
					Stroke = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 37, 99, 235)),
					StrokeThickness = 2.3,
					StrokeLineJoin = PenLineJoin.Round
				});

				AddSrcCanvasText(_srcFilterCurveCanvas, "0 dB", 22, yTop - 6, labelBrush, monoFont, 10);
				AddSrcCanvasText(_srcFilterCurveCanvas, "-40", 26, yTop + chartHeight * 0.25 - 7, labelBrush, monoFont, 10);
				AddSrcCanvasText(_srcFilterCurveCanvas, "-80", 26, yTop + chartHeight * 0.50 - 7, labelBrush, monoFont, 10);
				AddSrcCanvasText(_srcFilterCurveCanvas, "-120", 20, yTop + chartHeight * 0.75 - 7, labelBrush, monoFont, 10);
				AddSrcCanvasText(_srcFilterCurveCanvas, "-160", 20, yBottom - 8, labelBrush, monoFont, 10);

				AddSrcCanvasText(_srcFilterCurveCanvas, "0 Hz", xLeft, 195, labelBrush, monoFont, 10.5);
				AddSrcCanvasText(_srcFilterCurveCanvas, $"{inputRate * 0.5 / 1000.0:0.##} kHz", nyqX - 18, 195, labelBrush, monoFont, 10.5);
				AddSrcCanvasText(_srcFilterCurveCanvas, $"{inputRate / 1000.0:0.#} kHz (2x Nyq)", xRight - 102, 195, labelBrush, monoFont, 10.5);
			}
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

	private static object? FindChoiceById(ComboBox combo, string targetId)
	{
		foreach (var item in combo.Items)
		{
			if (item is SrcChoiceItem c && string.Equals(c.Id, targetId, StringComparison.OrdinalIgnoreCase))
				return c;
		}
		return combo.Items.FirstOrDefault();
	}

	private static string NormalizeDitherId(string? value)
	{
		return FurinaSrcCatalog.ParseDitherMode(value) switch
		{
			FurinaDitherMode.Tpdf => "tpdf",
			FurinaDitherMode.HighPass => "highpass-tpdf",
			FurinaDitherMode.Ns5 => "ns-5",
			FurinaDitherMode.Ns9 => "ns-9",
			FurinaDitherMode.UltraShaped => "ultra-shaped",
			_ => "off"
		};
	}
}
