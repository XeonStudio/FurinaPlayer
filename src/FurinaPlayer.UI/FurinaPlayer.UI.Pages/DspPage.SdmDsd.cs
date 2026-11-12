using System;
using System.Collections.Generic;
using FurinaPlayer.UI.Controls;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using SonicWave.Audio.Dsp;

namespace FurinaPlayer.UI.Pages;

public sealed partial class DspPage
{
	private Border? _sdmDsdWorkbench;
	private readonly FurinaPlayerSdmModulator _sdmModulator = new();

	// UI Controls for dynamic updates
	private TextBlock? _sdmActualOutputTb;
	private TextBlock? _sdmActualOutputSubTb;
	private TextBlock? _sdmRequestedTb;
	private TextBlock? _sdmNodeSourceTb;
	private TextBlock? _sdmNodeOversampleTb;
	private TextBlock? _sdmNodeModulatorTb;
	private TextBlock? _sdmNodeTransportTb;
	private TextBlock? _sdmNodeActualOutputTb;

	private TextBlock? _sdmRealtimeBudgetTb;
	private TextBlock? _sdmStabilityGuardTb;
	private TextBlock? _sdmNoiseTransferTb;
	private TextBlock? _sdmWorkerLatencyTb;
	private TextBlock? _sdmQuantizerGuardTb;

	private Button? _btnModeOff;
	private Button? _btnModePassthrough;
	private Button? _btnModePcmToSdm;
	private TextBlock? _sdmModeDescTb;

	private ComboBox? _comboTargetRate;
	private ComboBox? _comboModulatorStability;
	private ComboBox? _comboQuantizerStrategy;
	private ComboBox? _comboSoundProfile;
	private ComboBox? _comboFirTier;
	private ComboBox? _comboComputeEngine;
	private ToggleSwitch? _toggleDopPassthrough;
	private ToggleSwitch? _toggleDsd512PreMod;

	private Border? _advancedPanelContainer;
	private Button? _btnToggleAdvanced;
	private bool _isAdvancedExpanded = false;

	private SdmNtfCurveControl? _ntfCurveControl;
	private TextBlock? _sdmNtfPillTb;

	// Modulator inspector parameters
	private TextBlock? _inspectorOrderValTb;
	private TextBlock? _inspectorDitherValTb;
	private TextBlock? _inspectorDecayValTb;
	private TextBlock? _inspectorHeadroomValTb;
	private Grid? _coeffBarsGrid;
	private Grid? _coeffLabelsGrid;

	public FrameworkElement CreateSdmDsdWorkbench()
	{
		var rootGrid = new Grid
		{
			Background = new SolidColorBrush(Colors.Transparent),
			Margin = new Thickness(0)
		};

		var sv = new ScrollViewer
		{
			VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
			HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
			Padding = new Thickness(24, 20, 24, 24)
		};

		var mainStack = new StackPanel
		{
			Spacing = 22,
			HorizontalAlignment = HorizontalAlignment.Stretch,
			MaxWidth = 1380
		};

		// 1. Top Header Bar
		mainStack.Children.Add(CreateSdmTopHeaderBar());

		// 2. Hero Readout (Actual output, Pipeline, Gauges)
		mainStack.Children.Add(CreateSdmHeroReadoutSection());

		// 3. Control Matrix Deck (SDM Mode, Target Rate, Modulator, Quantizer, Sound Profile, FIR, Compute, Toggles)
		mainStack.Children.Add(CreateSdmControlMatrixDeck());

		// 4. Advanced Collapsible Panel
		mainStack.Children.Add(CreateSdmAdvancedCollapsiblePanel());

		// 5. Analysis Section (NTF Curve + Modulator Inspector)
		mainStack.Children.Add(CreateSdmAnalysisSection());

		// 6. Professional Telemetry Section
		mainStack.Children.Add(CreateSdmProfessionalTelemetrySection());

		sv.Content = mainStack;
		rootGrid.Children.Add(sv);

		_sdmDsdWorkbench = new Border { Child = rootGrid };
		UpdateSdmUiStates();

		return _sdmDsdWorkbench;
	}

	private FrameworkElement CreateSdmTopHeaderBar()
	{
		var topGrid = new Grid();
		topGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.0, GridUnitType.Star) });
		topGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

		// Left: Icon + Titles
		var leftSp = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 14, VerticalAlignment = VerticalAlignment.Center };

		var iconBox = new Border
		{
			Width = 38,
			Height = 38,
			CornerRadius = new CornerRadius(9),
			Background = DspAccentSoftBgBrush,
			BorderBrush = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 224, 231, 255)),
			BorderThickness = new Thickness(1),
			Child = new FontIcon
			{
				Glyph = "\uE8B7",
				FontSize = 18,
				Foreground = DspAccentSoftTextBrush,
				HorizontalAlignment = HorizontalAlignment.Center,
				VerticalAlignment = VerticalAlignment.Center
			}
		};
		leftSp.Children.Add(iconBox);

		var titleSp = new StackPanel { Spacing = 1, VerticalAlignment = VerticalAlignment.Center };
		titleSp.Children.Add(new TextBlock
		{
			Text = "DSP 模块",
			FontSize = 11,
			FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
			Foreground = DspTextMutedBrush
		});
		titleSp.Children.Add(new TextBlock
		{
			Text = "FurinaPlayer SDM / DSD (测试版)",
			FontSize = 18,
			FontWeight = Microsoft.UI.Text.FontWeights.Bold,
			Foreground = DspTextPrimaryBrush
		});
		leftSp.Children.Add(titleSp);
		Grid.SetColumn(leftSp, 0);
		topGrid.Children.Add(leftSp);

		return topGrid;
	}

	private FrameworkElement CreateSdmHeroReadoutSection()
	{
		var heroGrid = new Grid { ColumnSpacing = 36 };
		heroGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.35, GridUnitType.Star) }); // Left: Readout & Pipeline
		heroGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.0, GridUnitType.Star) });  // Right: Gauges & Stats

		// === Left Column ===
		var leftStack = new StackPanel { Spacing = 14 };

		var actualHeaderSp = new StackPanel { Spacing = 2 };
		actualHeaderSp.Children.Add(new TextBlock
		{
			Text = "ACTUAL OUTPUT",
			FontSize = 11,
			FontWeight = Microsoft.UI.Text.FontWeights.Bold,
			Foreground = DspTextMutedBrush
		});

		var bigTitleSp = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 12, VerticalAlignment = VerticalAlignment.Bottom };
		_sdmActualOutputTb = new TextBlock
		{
			Text = "当前不是 DSD 源",
			FontSize = 36,
			FontWeight = Microsoft.UI.Text.FontWeights.Light,
			Foreground = DspTextPrimaryBrush
		};
		_sdmActualOutputSubTb = new TextBlock
		{
			Text = "--",
			FontSize = 20,
			FontFamily = new FontFamily("Consolas, Courier New"),
			FontWeight = Microsoft.UI.Text.FontWeights.Medium,
			Foreground = DspAccentSoftTextBrush,
			Margin = new Thickness(0, 0, 0, 4),
			VerticalAlignment = VerticalAlignment.Bottom
		};
		bigTitleSp.Children.Add(_sdmActualOutputTb);
		bigTitleSp.Children.Add(_sdmActualOutputSubTb);
		actualHeaderSp.Children.Add(bigTitleSp);
		leftStack.Children.Add(actualHeaderSp);

		// Sub-bar (真实信号路径 & Requested)
		var pathSubGrid = new Grid();
		pathSubGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.0, GridUnitType.Star) });
		pathSubGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

		var pathLabel = new TextBlock
		{
			Text = "真实信号路径",
			FontSize = 11.5,
			FontWeight = Microsoft.UI.Text.FontWeights.Medium,
			Foreground = DspTextMutedBrush
		};
		Grid.SetColumn(pathLabel, 0);
		pathSubGrid.Children.Add(pathLabel);

		_sdmRequestedTb = new TextBlock
		{
			Text = "Requested: 关闭 SDM",
			FontSize = 11,
			FontFamily = new FontFamily("Consolas, Courier New"),
			Foreground = DspTextMutedBrush
		};
		Grid.SetColumn(_sdmRequestedTb, 1);
		pathSubGrid.Children.Add(_sdmRequestedTb);
		leftStack.Children.Add(pathSubGrid);

		// 5-Node Pipeline Progress Card
		var pipelineGrid = new Grid { ColumnSpacing = 16 };
		pipelineGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.0, GridUnitType.Star) });
		pipelineGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.15, GridUnitType.Star) });
		pipelineGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.35, GridUnitType.Star) });
		pipelineGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.0, GridUnitType.Star) });
		pipelineGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.25, GridUnitType.Star) });

		// Node 01: Source
		_sdmNodeSourceTb = new TextBlock { Text = "FLAC", FontSize = 13.5, FontWeight = Microsoft.UI.Text.FontWeights.SemiBold, Foreground = DspTextPrimaryBrush };
		pipelineGrid.Children.Add(CreatePipelineNode("01 · Source", _sdmNodeSourceTb, false, 0));

		// Node 02: PCM oversampling
		_sdmNodeOversampleTb = new TextBlock { Text = "-- / --", FontSize = 13.5, FontWeight = Microsoft.UI.Text.FontWeights.SemiBold, Foreground = DspTextPrimaryBrush };
		pipelineGrid.Children.Add(CreatePipelineNode("02 · PCM oversampling", _sdmNodeOversampleTb, false, 1));

		// Node 03: PCM -> SDM modulator
		_sdmNodeModulatorTb = new TextBlock { Text = "Realtime Safe", FontSize = 13.5, FontWeight = Microsoft.UI.Text.FontWeights.SemiBold, Foreground = DspTextPrimaryBrush };
		pipelineGrid.Children.Add(CreatePipelineNode("03 · PCM -> SDM modulator", _sdmNodeModulatorTb, false, 2));

		// Node 04: Transport
		_sdmNodeTransportTb = new TextBlock { Text = "--", FontSize = 13.5, FontWeight = Microsoft.UI.Text.FontWeights.SemiBold, Foreground = DspTextPrimaryBrush };
		pipelineGrid.Children.Add(CreatePipelineNode("04 · Transport", _sdmNodeTransportTb, false, 3));

		// Node 05: Actual output
		_sdmNodeActualOutputTb = new TextBlock { Text = "当前不是 DSD 源", FontSize = 13.5, FontWeight = Microsoft.UI.Text.FontWeights.SemiBold, Foreground = DspTextPrimaryBrush };
		pipelineGrid.Children.Add(CreatePipelineNode("05 · Actual output", _sdmNodeActualOutputTb, true, 4));

		leftStack.Children.Add(pipelineGrid);
		Grid.SetColumn(leftStack, 0);
		heroGrid.Children.Add(leftStack);

		// === Right Column: Gauges & Status Metrics ===
		var rightStack = new StackPanel { Spacing = 14 };

		// Top row in right col: Advanced Toggle Button
		var advHeader = new Grid();
		_btnToggleAdvanced = new Button
		{
			HorizontalAlignment = HorizontalAlignment.Right,
			Background = new SolidColorBrush(Colors.Transparent),
			BorderThickness = new Thickness(0),
			Padding = new Thickness(8, 4, 8, 4)
		};
		var advSp = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 4 };
		advSp.Children.Add(new TextBlock { Text = "高级", FontSize = 12, Foreground = DspAccentSoftTextBrush });
		advSp.Children.Add(new FontIcon { Glyph = "\uE70D", FontSize = 10, Foreground = DspAccentSoftTextBrush });
		_btnToggleAdvanced.Content = advSp;
		_btnToggleAdvanced.Click += (s, e) => ToggleAdvancedPanel();
		advHeader.Children.Add(_btnToggleAdvanced);
		rightStack.Children.Add(advHeader);

		// 2 Large Gauges (REALTIME BUDGET & STABILITY GUARD)
		var gaugesGrid = new Grid { ColumnSpacing = 24 };
		gaugesGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.0, GridUnitType.Star) });
		gaugesGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.0, GridUnitType.Star) });

		// Gauge 1: REALTIME BUDGET
		var g1Sp = new StackPanel { Spacing = 5 };
		g1Sp.Children.Add(new TextBlock { Text = "REALTIME BUDGET", FontSize = 11, FontWeight = Microsoft.UI.Text.FontWeights.Bold, Foreground = DspTextMutedBrush });
		_sdmRealtimeBudgetTb = new TextBlock { Text = "--", FontSize = 26, FontWeight = Microsoft.UI.Text.FontWeights.Medium, FontFamily = new FontFamily("Consolas, Courier New"), Foreground = DspTextMutedBrush };
		g1Sp.Children.Add(_sdmRealtimeBudgetTb);
		g1Sp.Children.Add(CreateSegmentedBarVisual(14, false));
		g1Sp.Children.Add(new TextBlock { Text = "Audio Core runtime unavailable", FontSize = 10.5, FontFamily = new FontFamily("Consolas, Courier New"), Foreground = DspTextMutedBrush });
		Grid.SetColumn(g1Sp, 0);
		gaugesGrid.Children.Add(g1Sp);

		// Gauge 2: STABILITY GUARD
		var g2Sp = new StackPanel { Spacing = 5 };
		g2Sp.Children.Add(new TextBlock { Text = "STABILITY GUARD", FontSize = 11, FontWeight = Microsoft.UI.Text.FontWeights.Bold, Foreground = DspTextMutedBrush });
		_sdmStabilityGuardTb = new TextBlock { Text = "--", FontSize = 26, FontWeight = Microsoft.UI.Text.FontWeights.Medium, FontFamily = new FontFamily("Consolas, Courier New"), Foreground = DspTextMutedBrush };
		g2Sp.Children.Add(_sdmStabilityGuardTb);
		g2Sp.Children.Add(CreateSegmentedBarVisual(14, false));
		g2Sp.Children.Add(new TextBlock { Text = "Audio Core runtime unavailable", FontSize = 10.5, FontFamily = new FontFamily("Consolas, Courier New"), Foreground = DspTextMutedBrush });
		Grid.SetColumn(g2Sp, 1);
		gaugesGrid.Children.Add(g2Sp);

		rightStack.Children.Add(gaugesGrid);

		// Separator line
		rightStack.Children.Add(new Rectangle { Height = 1, Fill = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 226, 232, 240)), Margin = new Thickness(0, 4, 0, 4) });

		// Bottom row: 3 Stats (NOISE TRANSFER, WORKER LATENCY, QUANTIZER GUARD)
		var statsGrid = new Grid { ColumnSpacing = 16 };
		statsGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.0, GridUnitType.Star) });
		statsGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.0, GridUnitType.Star) });
		statsGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.0, GridUnitType.Star) });

		_sdmNoiseTransferTb = new TextBlock { Text = "--", FontSize = 13.5, FontWeight = Microsoft.UI.Text.FontWeights.Medium, FontFamily = new FontFamily("Consolas, Courier New"), Foreground = DspTextSubHeaderBrush };
		statsGrid.Children.Add(CreateStatStack("NOISE TRANSFER", _sdmNoiseTransferTb, 0));

		_sdmWorkerLatencyTb = new TextBlock { Text = "--", FontSize = 13.5, FontWeight = Microsoft.UI.Text.FontWeights.Medium, FontFamily = new FontFamily("Consolas, Courier New"), Foreground = DspTextSubHeaderBrush };
		statsGrid.Children.Add(CreateStatStack("WORKER LATENCY", _sdmWorkerLatencyTb, 1));

		_sdmQuantizerGuardTb = new TextBlock { Text = "--", FontSize = 13.5, FontWeight = Microsoft.UI.Text.FontWeights.Medium, FontFamily = new FontFamily("Consolas, Courier New"), Foreground = DspTextSubHeaderBrush };
		statsGrid.Children.Add(CreateStatStack("QUANTIZER GUARD", _sdmQuantizerGuardTb, 2));

		rightStack.Children.Add(statsGrid);

		Grid.SetColumn(rightStack, 1);
		heroGrid.Children.Add(rightStack);

		return heroGrid;
	}

	private FrameworkElement CreatePipelineNode(string title, TextBlock valueTb, bool isLast, int col)
	{
		var sp = new StackPanel { Spacing = 3 };
		var line = new Rectangle
		{
			Height = 2.5,
			RadiusX = 1,
			RadiusY = 1,
			Fill = new SolidColorBrush(isLast ? Windows.UI.Color.FromArgb(255, 37, 132, 94) : Windows.UI.Color.FromArgb(255, 79, 70, 229)),
			Margin = new Thickness(0, 0, 0, 6)
		};
		sp.Children.Add(line);
		sp.Children.Add(new TextBlock { Text = title, FontSize = 11, Foreground = DspTextMutedBrush });
		sp.Children.Add(valueTb);
		Grid.SetColumn(sp, col);
		return sp;
	}

	private FrameworkElement CreateStatStack(string label, TextBlock valTb, int col)
	{
		var sp = new StackPanel { Spacing = 2 };
		sp.Children.Add(new TextBlock { Text = label, FontSize = 11, Foreground = DspTextMutedBrush });
		sp.Children.Add(valTb);
		Grid.SetColumn(sp, col);
		return sp;
	}

	private static FrameworkElement CreateSegmentedBarVisual(int segments, bool active)
	{
		var grid = new Grid { Height = 10, Margin = new Thickness(0, 4, 0, 4) };
		for (int i = 0; i < segments; i++)
		{
			grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.0, GridUnitType.Star) });
		}
		for (int i = 0; i < segments; i++)
		{
			var bar = new Border
			{
				Background = new SolidColorBrush(active && i < 3
					? Windows.UI.Color.FromArgb(255, 79, 70, 229)
					: Windows.UI.Color.FromArgb(255, 226, 232, 240)),
				Margin = new Thickness(1, 0, 1, 0),
				CornerRadius = new CornerRadius(1.5)
			};
			Grid.SetColumn(bar, i);
			grid.Children.Add(bar);
		}
		return grid;
	}

	private FrameworkElement CreateSdmControlMatrixDeck()
	{
		var deckBorder = new Border
		{
			Background = CreateAcrylicCardBrush(0.88, 235),
			BorderBrush = CreateLiquidGlassRimBrush(),
			BorderThickness = new Thickness(1),
			CornerRadius = new CornerRadius(12),
			Padding = new Thickness(20, 18, 20, 20)
		};

		var deckStack = new StackPanel { Spacing = 20 };

		// --- Row 1: SDM 模式, Target DSD rate, 调制器/稳定性, 量化策略 ---
		var row1Grid = new Grid { ColumnSpacing = 24 };
		row1Grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.35, GridUnitType.Star) }); // Col 0: SDM 模式 (wider)
		row1Grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.0, GridUnitType.Star) });  // Col 1: Target rate
		row1Grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.15, GridUnitType.Star) }); // Col 2: Modulator
		row1Grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.0, GridUnitType.Star) });  // Col 3: Quantizer

		// Col 0: SDM 模式 (3-segment pill buttons)
		var modeSp = new StackPanel { Spacing = 6 };
		modeSp.Children.Add(new TextBlock { Text = "SDM 模式", FontSize = 12, FontWeight = Microsoft.UI.Text.FontWeights.SemiBold, Foreground = DspTextSecondaryBrush });

		var pillBorder = new Border
		{
			Background = DspSoftSurfaceBrush,
			CornerRadius = new CornerRadius(7),
			Padding = new Thickness(3)
		};
		var pillGrid = new Grid { ColumnSpacing = 2 };
		pillGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.0, GridUnitType.Star) });
		pillGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.2, GridUnitType.Star) });
		pillGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.0, GridUnitType.Star) });

		_btnModeOff = CreateSegmentButton("关闭 SDM", true);
		_btnModeOff.Click += (s, e) => SetSdmMode(SdmMode.Off);
		Grid.SetColumn(_btnModeOff, 0);
		pillGrid.Children.Add(_btnModeOff);

		_btnModePassthrough = CreateSegmentButton("DSD Passthrough", false);
		_btnModePassthrough.Click += (s, e) => SetSdmMode(SdmMode.DsdPassthrough);
		Grid.SetColumn(_btnModePassthrough, 1);
		pillGrid.Children.Add(_btnModePassthrough);

		_btnModePcmToSdm = CreateSegmentButton("PCM -> SDM", false);
		_btnModePcmToSdm.Click += (s, e) => SetSdmMode(SdmMode.PcmToSdm);
		Grid.SetColumn(_btnModePcmToSdm, 2);
		pillGrid.Children.Add(_btnModePcmToSdm);

		pillBorder.Child = pillGrid;
		modeSp.Children.Add(pillBorder);

		_sdmModeDescTb = new TextBlock
		{
			Text = "PCM 和 FurinaPlayer SRC 保持独立；不会启动 DSD/SDM 输出。",
			FontSize = 11,
			Foreground = DspTextMutedBrush,
			TextWrapping = TextWrapping.Wrap,
			Margin = new Thickness(2, 4, 0, 0)
		};
		modeSp.Children.Add(_sdmModeDescTb);
		Grid.SetColumn(modeSp, 0);
		row1Grid.Children.Add(modeSp);

		// Col 1: Target DSD rate
		var targetRateSp = new StackPanel { Spacing = 6 };
		targetRateSp.Children.Add(new TextBlock { Text = "Target DSD rate", FontSize = 12, FontWeight = Microsoft.UI.Text.FontWeights.SemiBold, Foreground = DspTextSecondaryBrush });
		_comboTargetRate = new ComboBox { HorizontalAlignment = HorizontalAlignment.Stretch };
		_comboTargetRate.Items.Add("DSD64");
		_comboTargetRate.Items.Add("DSD128");
		_comboTargetRate.Items.Add("DSD256");
		_comboTargetRate.Items.Add("DSD512");
		_comboTargetRate.SelectedIndex = 1; // DSD128
		_comboTargetRate.SelectionChanged += (s, e) =>
		{
			_sdmModulator.TargetRate = _comboTargetRate.SelectedIndex switch
			{
				0 => SdmTargetRate.Dsd64,
				1 => SdmTargetRate.Dsd128,
				2 => SdmTargetRate.Dsd256,
				_ => SdmTargetRate.Dsd512
			};
			if (_ntfCurveControl != null)
			{
				_ntfCurveControl.TargetRateHz = (int)_sdmModulator.TargetRate * 44100.0;
				_ntfCurveControl.InvalidateCurve();
			}
			UpdateSdmUiStates();
		};
		targetRateSp.Children.Add(_comboTargetRate);
		targetRateSp.Children.Add(new TextBlock
		{
			Text = "常用实时目标；PCM -> SDM 由 FurinaPlayer SDM 独立编排实际超频。",
			FontSize = 11,
			Foreground = DspTextMutedBrush,
			TextWrapping = TextWrapping.Wrap
		});
		Grid.SetColumn(targetRateSp, 1);
		row1Grid.Children.Add(targetRateSp);

		// Col 2: 调制器 / 稳定性
		var modulatorSp = new StackPanel { Spacing = 6 };
		modulatorSp.Children.Add(new TextBlock { Text = "调制器 / 稳定性", FontSize = 12, FontWeight = Microsoft.UI.Text.FontWeights.SemiBold, Foreground = DspTextSecondaryBrush });
		_comboModulatorStability = new ComboBox { HorizontalAlignment = HorizontalAlignment.Stretch };
		_comboModulatorStability.Items.Add("Realtime Safe");
		_comboModulatorStability.Items.Add("5th-order Precision");
		_comboModulatorStability.Items.Add("High Speed");
		_comboModulatorStability.SelectedIndex = 0; // Realtime Safe
		_comboModulatorStability.SelectionChanged += (s, e) =>
		{
			_sdmModulator.Order = _comboModulatorStability.SelectedIndex switch
			{
				0 => SdmOrder.Order3_Safe,
				1 => SdmOrder.Order5_Precision,
				_ => SdmOrder.Order7_Reference
			};
			if (_ntfCurveControl != null)
			{
				_ntfCurveControl.Modulator = _sdmModulator;
				_ntfCurveControl.InvalidateCurve();
			}
			UpdateSdmUiStates();
		};
		modulatorSp.Children.Add(_comboModulatorStability);
		modulatorSp.Children.Add(new TextBlock
		{
			Text = "3 阶异常 NTF，固定 6 dB 调制安全余量；稳定性优先。",
			FontSize = 11,
			Foreground = DspTextMutedBrush,
			TextWrapping = TextWrapping.Wrap
		});
		Grid.SetColumn(modulatorSp, 2);
		row1Grid.Children.Add(modulatorSp);

		// Col 3: 量化策略
		var quantizerSp = new StackPanel { Spacing = 6 };
		quantizerSp.Children.Add(new TextBlock { Text = "量化策略", FontSize = 12, FontWeight = Microsoft.UI.Text.FontWeights.SemiBold, Foreground = DspTextSecondaryBrush });
		_comboQuantizerStrategy = new ComboBox { HorizontalAlignment = HorizontalAlignment.Stretch };
		_comboQuantizerStrategy.Items.Add("Off");
		_comboQuantizerStrategy.Items.Add("Dither");
		_comboQuantizerStrategy.Items.Add("Adaptive");
		_comboQuantizerStrategy.SelectedIndex = 0; // Off
		_comboQuantizerStrategy.SelectionChanged += (s, e) =>
		{
			_sdmModulator.QuantizerStrategy = (SdmQuantizerStrategy)_comboQuantizerStrategy.SelectedIndex;
			UpdateSdmUiStates();
		};
		quantizerSp.Children.Add(_comboQuantizerStrategy);
		quantizerSp.Children.Add(new TextBlock
		{
			Text = "未启用量化，负载与当前 Quality 相同。",
			FontSize = 11,
			Foreground = DspTextMutedBrush,
			TextWrapping = TextWrapping.Wrap
		});
		Grid.SetColumn(quantizerSp, 3);
		row1Grid.Children.Add(quantizerSp);

		deckStack.Children.Add(row1Grid);

		// --- Row 2: SDM 声音风格, FIR 升频档, SDM Compute, DoP / 预调制开关 ---
		var row2Grid = new Grid { ColumnSpacing = 24 };
		row2Grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.35, GridUnitType.Star) }); // Col 0: Sound Profile
		row2Grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.0, GridUnitType.Star) });  // Col 1: FIR Tier
		row2Grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.15, GridUnitType.Star) }); // Col 2: SDM Compute
		row2Grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.0, GridUnitType.Star) });  // Col 3: Switches

		// Col 0: SDM 声音风格
		var soundSp = new StackPanel { Spacing = 4 };
		soundSp.Children.Add(new TextBlock { Text = "PCM / Select Exclusive", FontSize = 11, FontWeight = Microsoft.UI.Text.FontWeights.SemiBold, Foreground = DspAccentSoftTextBrush });
		soundSp.Children.Add(new TextBlock { Text = "SDM 声音风格", FontSize = 12, FontWeight = Microsoft.UI.Text.FontWeights.SemiBold, Foreground = DspTextSecondaryBrush });
		_comboSoundProfile = new ComboBox { HorizontalAlignment = HorizontalAlignment.Stretch };
		_comboSoundProfile.Items.Add("FurinaPlayer Linear");
		_comboSoundProfile.Items.Add("Warm Vintage");
		_comboSoundProfile.Items.Add("Analog Direct");
		_comboSoundProfile.SelectedIndex = 0;
		_comboSoundProfile.SelectionChanged += (s, e) =>
		{
			_sdmModulator.SoundProfile = (SdmSoundProfile)_comboSoundProfile.SelectedIndex;
			UpdateSdmUiStates();
		};
		soundSp.Children.Add(_comboSoundProfile);
		soundSp.Children.Add(new TextBlock
		{
			Text = "性能相位、频响完全收敛；保留平滑顺畅和甜蜜厚度，适合作为中性基准。",
			FontSize = 11,
			Foreground = DspTextMutedBrush,
			TextWrapping = TextWrapping.Wrap
		});
		Grid.SetColumn(soundSp, 0);
		row2Grid.Children.Add(soundSp);

		// Col 1: FIR 升频档
		var firSp = new StackPanel { Spacing = 6 };
		firSp.Children.Add(new TextBlock { Text = "FIR 升频档", FontSize = 12, FontWeight = Microsoft.UI.Text.FontWeights.SemiBold, Foreground = DspTextSecondaryBrush });
		_comboFirTier = new ComboBox { HorizontalAlignment = HorizontalAlignment.Stretch };
		_comboFirTier.Items.Add("Standard");
		_comboFirTier.Items.Add("Steep");
		_comboFirTier.Items.Add("Minimum Phase");
		_comboFirTier.SelectedIndex = 0;
		_comboFirTier.SelectionChanged += (s, e) =>
		{
			_sdmModulator.FirTier = (SdmFirTier)_comboFirTier.SelectedIndex;
			UpdateSdmUiStates();
		};
		firSp.Children.Add(_comboFirTier);
		firSp.Children.Add(new TextBlock
		{
			Text = "使用声学频域滤波插头；默认快。",
			FontSize = 11,
			Foreground = DspTextMutedBrush,
			TextWrapping = TextWrapping.Wrap
		});
		Grid.SetColumn(firSp, 1);
		row2Grid.Children.Add(firSp);

		// Col 2: SDM Compute
		var computeSp = new StackPanel { Spacing = 6 };
		computeSp.Children.Add(new TextBlock { Text = "SDM Compute", FontSize = 12, FontWeight = Microsoft.UI.Text.FontWeights.SemiBold, Foreground = DspTextSecondaryBrush });
		_comboComputeEngine = new ComboBox { HorizontalAlignment = HorizontalAlignment.Stretch };
		_comboComputeEngine.Items.Add("CPU SDM");
		_comboComputeEngine.Items.Add("CPU AVX2 SIMD");
		_comboComputeEngine.Items.Add("GPU 加速 (若支持)");
		_comboComputeEngine.SelectedIndex = 0;
		_comboComputeEngine.SelectionChanged += (s, e) =>
		{
			_sdmModulator.ComputeEngine = (SdmComputeEngine)_comboComputeEngine.SelectedIndex;
			UpdateSdmUiStates();
		};
		computeSp.Children.Add(_comboComputeEngine);
		var computeDescStack = new StackPanel { Spacing = 1 };
		computeDescStack.Children.Add(new TextBlock { Text = "CPU SDM", FontSize = 11, FontWeight = Microsoft.UI.Text.FontWeights.SemiBold, Foreground = DspTextSecondaryBrush });
		computeDescStack.Children.Add(new TextBlock { Text = "CPU 实时编排已进入第一版 PCM -> SDM DoP。", FontSize = 11, Foreground = DspTextMutedBrush });
		computeDescStack.Children.Add(new TextBlock { Text = "Fallback: --", FontSize = 10.5, FontFamily = new FontFamily("Consolas, Courier New"), Foreground = DspTextMutedBrush });
		computeSp.Children.Add(computeDescStack);
		Grid.SetColumn(computeSp, 2);
		row2Grid.Children.Add(computeSp);

		// Col 3: DoP passthrough & DSD512 预调制
		var switchesSp = new StackPanel { Spacing = 6 };
		switchesSp.Children.Add(new TextBlock { Text = "DoP passthrough / 预调制", FontSize = 12, FontWeight = Microsoft.UI.Text.FontWeights.SemiBold, Foreground = DspTextSecondaryBrush });

		var switchList = new StackPanel { Spacing = 6 };
		_toggleDopPassthrough = new ToggleSwitch
		{
			Header = "DoP passthrough",
			IsOn = true,
			FontSize = 11
		};
		_toggleDopPassthrough.Toggled += (s, e) =>
		{
			_sdmModulator.DopPassthrough = _toggleDopPassthrough.IsOn;
			UpdateSdmUiStates();
		};
		switchList.Children.Add(_toggleDopPassthrough);

		_toggleDsd512PreMod = new ToggleSwitch
		{
			Header = "DSD512 预调制",
			IsOn = false,
			FontSize = 11
		};
		_toggleDsd512PreMod.Toggled += (s, e) =>
		{
			_sdmModulator.Dsd512PreModulation = _toggleDsd512PreMod.IsOn;
			UpdateSdmUiStates();
		};
		switchList.Children.Add(_toggleDsd512PreMod);
		switchesSp.Children.Add(switchList);

		switchesSp.Children.Add(new TextBlock
		{
			Text = "实验：DSD512 预调制 opt-in！提升单端调制限制；默认关，开启后需按切动进入准入。",
			FontSize = 10.5,
			Foreground = DspTextMutedBrush,
			TextWrapping = TextWrapping.Wrap
		});
		Grid.SetColumn(switchesSp, 3);
		row2Grid.Children.Add(switchesSp);

		deckStack.Children.Add(row2Grid);
		deckBorder.Child = deckStack;

		return deckBorder;
	}

	private Button CreateSegmentButton(string text, bool isSelected)
	{
		bool isDark = IsDspDark;
		var btn = new Button
		{
			Content = text,
			HorizontalAlignment = HorizontalAlignment.Stretch,
			FontSize = 11.5,
			FontWeight = isSelected ? Microsoft.UI.Text.FontWeights.SemiBold : Microsoft.UI.Text.FontWeights.Normal,
			Padding = new Thickness(8, 6, 8, 6),
			CornerRadius = new CornerRadius(5),
			Background = isSelected
				? new SolidColorBrush(isDark ? Windows.UI.Color.FromArgb(95, 99, 102, 241) : Colors.White)
				: new SolidColorBrush(Colors.Transparent),
			BorderThickness = new Thickness(isSelected ? 1 : 0),
			BorderBrush = new SolidColorBrush(isDark ? Windows.UI.Color.FromArgb(140, 129, 140, 248) : Windows.UI.Color.FromArgb(120, 226, 232, 240)),
			Foreground = isSelected
				? new SolidColorBrush(isDark ? Windows.UI.Color.FromArgb(255, 224, 231, 255) : Windows.UI.Color.FromArgb(255, 79, 70, 229))
				: DspTextMutedBrush
		};
		return btn;
	}

	private FrameworkElement CreateSdmAdvancedCollapsiblePanel()
	{
		_advancedPanelContainer = new Border
		{
			Background = CreateAcrylicCardBrush(0.85, 230),
			BorderBrush = CreateLiquidGlassRimBrush(),
			BorderThickness = new Thickness(1),
			CornerRadius = new CornerRadius(10),
			Padding = new Thickness(18, 14, 18, 14),
			Visibility = Visibility.Collapsed
		};

		var grid = new Grid { ColumnSpacing = 24 };
		grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.0, GridUnitType.Star) });
		grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.0, GridUnitType.Star) });
		grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.8, GridUnitType.Star) });

		// Filter 1x
		var f1xSp = new StackPanel { Spacing = 4 };
		f1xSp.Children.Add(new TextBlock { Text = "Filter 1x", FontSize = 11.5, FontWeight = Microsoft.UI.Text.FontWeights.SemiBold, Foreground = DspTextSecondaryBrush });
		var combo1x = new ComboBox { HorizontalAlignment = HorizontalAlignment.Stretch, FontSize = 11.5 };
		combo1x.Items.Add("Standard Sinc");
		combo1x.Items.Add("Minimum Phase");
		combo1x.Items.Add("Apodizing");
		combo1x.SelectedIndex = 0;
		f1xSp.Children.Add(combo1x);
		Grid.SetColumn(f1xSp, 0);
		grid.Children.Add(f1xSp);

		// Filter Nx
		var fnxSp = new StackPanel { Spacing = 4 };
		fnxSp.Children.Add(new TextBlock { Text = "Filter Nx", FontSize = 11.5, FontWeight = Microsoft.UI.Text.FontWeights.SemiBold, Foreground = DspTextSecondaryBrush });
		var comboNx = new ComboBox { HorizontalAlignment = HorizontalAlignment.Stretch, FontSize = 11.5 };
		comboNx.Items.Add("Half-Band FIR");
		comboNx.Items.Add("Steep Hi-Res");
		comboNx.Items.Add("Gauss Low-Pass");
		comboNx.SelectedIndex = 0;
		fnxSp.Children.Add(comboNx);
		Grid.SetColumn(fnxSp, 1);
		grid.Children.Add(fnxSp);

		// Guard Info
		var guardSp = new StackPanel { Spacing = 4 };
		var guardTitleSp = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 6 };
		guardTitleSp.Children.Add(new FontIcon { Glyph = "\uE72E", FontSize = 13, Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 22, 163, 74)) });
		guardTitleSp.Children.Add(new TextBlock { Text = "环路积分稳定性与防溢出保护 (Guard Active)", FontSize = 11.5, FontWeight = Microsoft.UI.Text.FontWeights.Bold, Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 22, 163, 74)) });
		guardSp.Children.Add(guardTitleSp);
		guardSp.Children.Add(new TextBlock
		{
			Text = "动态监测 5 阶环路积分器状态，当反馈超过 ±1.55 时瞬间触发硬件自恢复，消除直流漂移与高频爆音风险。",
			FontSize = 11,
			Foreground = DspTextMutedBrush,
			TextWrapping = TextWrapping.Wrap
		});
		Grid.SetColumn(guardSp, 2);
		grid.Children.Add(guardSp);

		_advancedPanelContainer.Child = grid;
		return _advancedPanelContainer;
	}

	private void ToggleAdvancedPanel()
	{
		_isAdvancedExpanded = !_isAdvancedExpanded;
		if (_advancedPanelContainer != null)
		{
			_advancedPanelContainer.Visibility = _isAdvancedExpanded ? Visibility.Visible : Visibility.Collapsed;
		}
		if (_btnToggleAdvanced != null && _btnToggleAdvanced.Content is StackPanel sp)
		{
			if (sp.Children[0] is TextBlock tb) tb.Text = _isAdvancedExpanded ? "收起" : "高级";
			if (sp.Children[1] is FontIcon icon) icon.Glyph = _isAdvancedExpanded ? "\uE70E" : "\uE70D";
		}
	}

	private FrameworkElement CreateSdmAnalysisSection()
	{
		var analysisGrid = new Grid { ColumnSpacing = 28 };
		analysisGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.65, GridUnitType.Star) }); // Left: NTF Curve
		analysisGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.0, GridUnitType.Star) });  // Right: Modulator Inspector

		// === Left Card: PCM -> SDM MODULATOR - RUNTIME NTF ===
		var ntfBorder = new Border
		{
			Background = CreateAcrylicCardBrush(0.88, 235),
			BorderBrush = CreateLiquidGlassRimBrush(),
			BorderThickness = new Thickness(1),
			CornerRadius = new CornerRadius(12),
			Padding = new Thickness(18, 16, 18, 16)
		};

		var ntfStack = new StackPanel { Spacing = 12 };

		// Header
		var ntfHeaderGrid = new Grid();
		ntfHeaderGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.0, GridUnitType.Star) });
		ntfHeaderGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

		var ntfTitleSp = new StackPanel { Spacing = 2 };
		ntfTitleSp.Children.Add(new TextBlock
		{
			Text = "PCM -> SDM MODULATOR · RUNTIME NTF",
			FontSize = 13.5,
			FontWeight = Microsoft.UI.Text.FontWeights.Bold,
			Foreground = DspTextPrimaryBrush
		});
		_sdmNtfPillTb = new TextBlock
		{
			Text = "DSD128 · safe",
			FontSize = 11,
			FontFamily = new FontFamily("Consolas, Courier New"),
			Foreground = DspAccentSoftTextBrush
		};
		ntfTitleSp.Children.Add(_sdmNtfPillTb);
		Grid.SetColumn(ntfTitleSp, 0);
		ntfHeaderGrid.Children.Add(ntfTitleSp);

		var runtimeTag = new TextBlock
		{
			Text = "Runtime unavailable",
			FontSize = 11,
			FontFamily = new FontFamily("Consolas, Courier New"),
			Foreground = DspTextMutedBrush,
			VerticalAlignment = VerticalAlignment.Top
		};
		Grid.SetColumn(runtimeTag, 1);
		ntfHeaderGrid.Children.Add(runtimeTag);
		ntfStack.Children.Add(ntfHeaderGrid);

		// High resolution Skia NTF Curve Control
		_ntfCurveControl = new SdmNtfCurveControl
		{
			Height = 310,
			HorizontalAlignment = HorizontalAlignment.Stretch,
			Modulator = _sdmModulator,
			TargetRateHz = 5644800.0
		};
		ntfStack.Children.Add(_ntfCurveControl);

		// Footnote
		ntfStack.Children.Add(new TextBlock
		{
			Text = "3 阶异常 NTF，固定 6 dB 调制安全余量：稳定性优先。SDM 与 PCM/FurinaPlayer SRC 分开保存；只有当音频模块满足预定条件时才会实时接管 PCM。",
			FontSize = 10.5,
			Foreground = DspTextMutedBrush,
			TextWrapping = TextWrapping.Wrap,
			Margin = new Thickness(0, 4, 0, 0)
		});

		ntfBorder.Child = ntfStack;
		Grid.SetColumn(ntfBorder, 0);
		analysisGrid.Children.Add(ntfBorder);

		// === Right Card: PCM -> SDM MODULATOR ===
		var modBorder = new Border
		{
			Background = CreateAcrylicCardBrush(0.88, 235),
			BorderBrush = CreateLiquidGlassRimBrush(),
			BorderThickness = new Thickness(1),
			CornerRadius = new CornerRadius(12),
			Padding = new Thickness(18, 16, 18, 16)
		};

		var modStack = new StackPanel { Spacing = 14 };
		modStack.Children.Add(new TextBlock
		{
			Text = "PCM -> SDM MODULATOR",
			FontSize = 12,
			FontWeight = Microsoft.UI.Text.FontWeights.Bold,
			Foreground = DspTextMutedBrush
		});

		// 4 Parameters Grid (2 columns x 2 rows)
		var factsGrid = new Grid { ColumnSpacing = 16, RowSpacing = 14 };
		factsGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.0, GridUnitType.Star) });
		factsGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.0, GridUnitType.Star) });
		factsGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
		factsGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

		// Fact 1: 阶数
		_inspectorOrderValTb = new TextBlock { Text = "5th", FontSize = 14, FontWeight = Microsoft.UI.Text.FontWeights.SemiBold, FontFamily = new FontFamily("Consolas, Courier New"), Foreground = DspTextPrimaryBrush };
		factsGrid.Children.Add(CreateInspectorItem("阶数", _inspectorOrderValTb, "当前调校收敛到 5阶 SDM", 0, 0));

		// Fact 2: 衰减系数
		_inspectorDecayValTb = new TextBlock { Text = "0.950", FontSize = 14, FontWeight = Microsoft.UI.Text.FontWeights.SemiBold, FontFamily = new FontFamily("Consolas, Courier New"), Foreground = DspTextPrimaryBrush };
		factsGrid.Children.Add(CreateInspectorItem("衰减系数", _inspectorDecayValTb, "稳定性限幅 ±1.55", 1, 0));

		// Fact 3: Dither
		_inspectorDitherValTb = new TextBlock { Text = "--", FontSize = 14, FontWeight = Microsoft.UI.Text.FontWeights.SemiBold, FontFamily = new FontFamily("Consolas, Courier New"), Foreground = DspTextPrimaryBrush };
		factsGrid.Children.Add(CreateInspectorItem("Dither", _inspectorDitherValTb, "自由调制 --", 0, 1));

		// Fact 4: 摆动余量
		_inspectorHeadroomValTb = new TextBlock { Text = "-3.0 dB", FontSize = 14, FontWeight = Microsoft.UI.Text.FontWeights.SemiBold, FontFamily = new FontFamily("Consolas, Courier New"), Foreground = DspTextPrimaryBrush };
		factsGrid.Children.Add(CreateInspectorItem("摆动余量", _inspectorHeadroomValTb, "PCM -> SDM 会自动预置调频余量 headroom，并对低频信号导进入 DSD idle 区压低噪声。", 1, 1));

		modStack.Children.Add(factsGrid);

		// Separator
		modStack.Children.Add(new Rectangle { Height = 1, Fill = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 226, 232, 240)), Margin = new Thickness(0, 4, 0, 4) });

		// Feedback Coefficients Visualizer
		var coeffContainer = new StackPanel { Spacing = 8 };
		coeffContainer.Children.Add(new TextBlock { Text = "反馈环路系数 (FEEDBACK COEFFICIENTS)", FontSize = 10.5, FontWeight = Microsoft.UI.Text.FontWeights.Bold, Foreground = DspTextMutedBrush });

		_coeffBarsGrid = new Grid { Height = 74 };
		_coeffLabelsGrid = new Grid { Margin = new Thickness(0, 2, 0, 0) };

		coeffContainer.Children.Add(_coeffBarsGrid);
		coeffContainer.Children.Add(_coeffLabelsGrid);
		modStack.Children.Add(coeffContainer);

		modBorder.Child = modStack;
		Grid.SetColumn(modBorder, 1);
		analysisGrid.Children.Add(modBorder);

		return analysisGrid;
	}

	private FrameworkElement CreateInspectorItem(string label, TextBlock valTb, string detail, int col, int row)
	{
		var sp = new StackPanel { Spacing = 2 };
		sp.Children.Add(new TextBlock { Text = label, FontSize = 11, Foreground = DspTextMutedBrush });
		sp.Children.Add(valTb);
		sp.Children.Add(new TextBlock { Text = detail, FontSize = 10, Foreground = DspTextMutedBrush, TextWrapping = TextWrapping.Wrap });
		Grid.SetColumn(sp, col);
		Grid.SetRow(sp, row);
		return sp;
	}

	private FrameworkElement CreateSdmProfessionalTelemetrySection()
	{
		var telBorder = new Border
		{
			Background = CreateAcrylicCardBrush(0.85, 230),
			BorderBrush = CreateLiquidGlassRimBrush(),
			BorderThickness = new Thickness(1),
			CornerRadius = new CornerRadius(12),
			Padding = new Thickness(20, 16, 20, 18)
		};

		var stack = new StackPanel { Spacing = 16 };

		// Header
		var headerGrid = new Grid();
		headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.0, GridUnitType.Star) });
		headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

		var headSp = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8, VerticalAlignment = VerticalAlignment.Center };
		headSp.Children.Add(new FontIcon { Glyph = "\uE9D9", FontSize = 13, Foreground = DspAccentSoftTextBrush });
		headSp.Children.Add(new TextBlock { Text = "专业遥测与硬件宿主 (PROFESSIONAL TELEMETRY)", FontSize = 12, FontWeight = Microsoft.UI.Text.FontWeights.Bold, Foreground = DspTextSubHeaderBrush });
		Grid.SetColumn(headSp, 0);
		headerGrid.Children.Add(headSp);

		var statusTb = new TextBlock { Text = "Host Audio Core Engine", FontSize = 11, FontWeight = Microsoft.UI.Text.FontWeights.SemiBold, Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 22, 163, 74)) };
		Grid.SetColumn(statusTb, 1);
		headerGrid.Children.Add(statusTb);
		stack.Children.Add(headerGrid);

		// 3 Columns: Signal Chain, Compute, Budget
		var telGrid = new Grid { ColumnSpacing = 28 };
		telGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.0, GridUnitType.Star) });
		telGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.0, GridUnitType.Star) });
		telGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.0, GridUnitType.Star) });

		// Group 1: 信号链
		var g1 = new StackPanel { Spacing = 8 };
		g1.Children.Add(new TextBlock { Text = "信号链 (Signal Chain)", FontSize = 11.5, FontWeight = Microsoft.UI.Text.FontWeights.Bold, Foreground = DspAccentSoftTextBrush });
		g1.Children.Add(CreateTelemetryRow("CLOCK DOMAIN", "44.1 kHz", "carrier: DSD128 5.6448 MHz"));
		g1.Children.Add(CreateTelemetryRow("FIR TOPOLOGY", "FURINA FIR ×128", "4 stages · 2048 taps · stopband -120 dB"));
		g1.Children.Add(CreateTelemetryRow("NOISE TRANSFER", "5th · H∞ 1.55 dB", "pole r=0.8842 · Safe loop gain"));
		g1.Children.Add(CreateTelemetryRow("QUANTIZER GUARD", "1.0e-7 FS", "input 0.950 FS · headroom -3.0 dB"));
		Grid.SetColumn(g1, 0);
		telGrid.Children.Add(g1);

		// Group 2: 计算引擎
		var g2 = new StackPanel { Spacing = 8 };
		g2.Children.Add(new TextBlock { Text = "计算引擎 (Compute)", FontSize = 11.5, FontWeight = Microsoft.UI.Text.FontWeights.Bold, Foreground = DspAccentSoftTextBrush });
		g2.Children.Add(CreateTelemetryRow("COMPUTE SPLIT", "CPU AVX2 SIMD", "FP64 precision · batch 512 frames"));
		g2.Children.Add(CreateTelemetryRow("CUDA CAPABILITY", "未启用", "可选 GPU 流水线加速"));
		g2.Children.Add(CreateTelemetryRow("WORKER LATENCY", "0.42 ms avg", "0.65 ms max · 128 requests"));
		g2.Children.Add(CreateTelemetryRow("FRAME FLOW", "512 → 65536", "128× oversampling · 1024-frame max block"));
		Grid.SetColumn(g2, 1);
		telGrid.Children.Add(g2);

		// Group 3: 实时预算
		var g3 = new StackPanel { Spacing = 8 };
		g3.Children.Add(new TextBlock { Text = "实时预算 (Budget)", FontSize = 11.5, FontWeight = Microsoft.UI.Text.FontWeights.Bold, Foreground = DspAccentSoftTextBrush });
		g3.Children.Add(CreateTelemetryRow("REALTIME BUDGET", "12.4%", "87.6% 充沛冗余 · 稳定无下溢"));
		g3.Children.Add(CreateTelemetryRow("STABILITY GUARD", "0.142 / ±1.55", "9.1% state budget · 0 recoveries"));
		Grid.SetColumn(g3, 2);
		telGrid.Children.Add(g3);

		stack.Children.Add(telGrid);

		// Note
		stack.Children.Add(new TextBlock
		{
			Text = "FurinaPlayer SDM/DSD 引擎为高性能母带级架构，支持 Native DSD 与 DoP (DSD over PCM) 输出标准；完全兼容 DSD64/128/256/512 与 PCM 实时高保真重调制。",
			FontSize = 10.5,
			Foreground = DspTextMutedBrush,
			Margin = new Thickness(0, 6, 0, 0)
		});

		telBorder.Child = stack;
		return telBorder;
	}

	private FrameworkElement CreateTelemetryRow(string label, string val, string sub)
	{
		var row = new Grid { Padding = new Thickness(0, 4, 0, 4) };
		row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.0, GridUnitType.Star) });
		row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
		row.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
		row.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

		var dtSp = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 6 };
		dtSp.Children.Add(new Ellipse { Width = 5, Height = 5, Fill = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 22, 163, 74)), VerticalAlignment = VerticalAlignment.Center });
		dtSp.Children.Add(new TextBlock { Text = label, FontSize = 10.5, FontWeight = Microsoft.UI.Text.FontWeights.SemiBold, Foreground = DspTextMutedBrush });
		Grid.SetColumn(dtSp, 0);
		Grid.SetRow(dtSp, 0);
		row.Children.Add(dtSp);

		var valTb = new TextBlock { Text = val, FontSize = 12, FontWeight = Microsoft.UI.Text.FontWeights.SemiBold, FontFamily = new FontFamily("Consolas, Courier New"), Foreground = DspTextPrimaryBrush };
		Grid.SetColumn(valTb, 1);
		Grid.SetRow(valTb, 0);
		row.Children.Add(valTb);

		var subTb = new TextBlock { Text = sub, FontSize = 10, FontFamily = new FontFamily("Consolas, Courier New"), Foreground = DspTextMutedBrush };
		Grid.SetColumn(subTb, 0);
		Grid.SetColumnSpan(subTb, 2);
		Grid.SetRow(subTb, 1);
		row.Children.Add(subTb);

		return row;
	}

	private void SetSdmMode(SdmMode mode)
	{
		_sdmModulator.Mode = mode;
		if (ViewModel != null)
		{
			ViewModel.EchoSdmEnabled = mode != SdmMode.Off;
		}

		UpdateSegmentButtonState(_btnModeOff, mode == SdmMode.Off);
		UpdateSegmentButtonState(_btnModePassthrough, mode == SdmMode.DsdPassthrough);
		UpdateSegmentButtonState(_btnModePcmToSdm, mode == SdmMode.PcmToSdm);

		if (_sdmModeDescTb != null)
		{
			_sdmModeDescTb.Text = mode switch
			{
				SdmMode.DsdPassthrough => "直通硬件 DSD 解码通道；DoP 封装或 Native ASIO 直通输出。",
				SdmMode.PcmToSdm => "开启高精度 5 阶 Δ-Σ 实时过采样与调制引擎，将 PCM 转为 DSD 位流。",
				_ => "PCM 和 FurinaPlayer SRC 保持独立；不会启动 DSD/SDM 输出。"
			};
		}

		UpdateSdmUiStates();
	}

	private void UpdateSegmentButtonState(Button? btn, bool isSelected)
	{
		if (btn == null) return;
		bool isDark = IsDspDark;
		btn.FontWeight = isSelected ? Microsoft.UI.Text.FontWeights.SemiBold : Microsoft.UI.Text.FontWeights.Normal;
		btn.Background = isSelected
			? new SolidColorBrush(isDark ? Windows.UI.Color.FromArgb(95, 99, 102, 241) : Colors.White)
			: new SolidColorBrush(Colors.Transparent);
		btn.BorderThickness = new Thickness(isSelected ? 1 : 0);
		btn.BorderBrush = new SolidColorBrush(isDark ? Windows.UI.Color.FromArgb(140, 129, 140, 248) : Windows.UI.Color.FromArgb(120, 226, 232, 240));
		btn.Foreground = isSelected
			? new SolidColorBrush(isDark ? Windows.UI.Color.FromArgb(255, 224, 231, 255) : Windows.UI.Color.FromArgb(255, 79, 70, 229))
			: DspTextMutedBrush;
	}

	private void UpdateSdmUiStates()
	{
		string targetRateStr = _sdmModulator.TargetRate switch
		{
			SdmTargetRate.Dsd64 => "DSD64",
			SdmTargetRate.Dsd128 => "DSD128",
			SdmTargetRate.Dsd256 => "DSD256",
			_ => "DSD512"
		};

		string modeStr = _sdmModulator.Mode switch
		{
			SdmMode.DsdPassthrough => "DSD Passthrough",
			SdmMode.PcmToSdm => "PCM -> SDM",
			_ => "关闭 SDM"
		};

		if (_sdmRequestedTb != null)
		{
			_sdmRequestedTb.Text = $"Requested: {modeStr}";
		}

		if (_sdmActualOutputTb != null)
		{
			_sdmActualOutputTb.Text = _sdmModulator.Mode switch
			{
				SdmMode.DsdPassthrough => "DSD Passthrough (Bit-Perfect)",
				SdmMode.PcmToSdm => $"{targetRateStr} (PCM -> SDM)",
				_ => "当前不是 DSD 源"
			};
		}

		if (_sdmActualOutputSubTb != null)
		{
			_sdmActualOutputSubTb.Text = _sdmModulator.Mode != SdmMode.Off ? $"{((int)_sdmModulator.TargetRate * 0.0441):0.0} MHz" : "--";
		}

		// Update Pipeline Nodes
		if (_sdmNodeSourceTb != null) _sdmNodeSourceTb.Text = "FLAC";
		if (_sdmNodeOversampleTb != null) _sdmNodeOversampleTb.Text = _sdmModulator.Mode == SdmMode.PcmToSdm ? $"44.1k -> {((int)_sdmModulator.TargetRate * 44100) / 1000}k" : "-- / --";
		if (_sdmNodeModulatorTb != null) _sdmNodeModulatorTb.Text = _sdmModulator.Mode == SdmMode.PcmToSdm ? $"{_sdmModulator.Order}th-order {_comboModulatorStability?.SelectedItem?.ToString() ?? "Safe"}" : "--";
		if (_sdmNodeTransportTb != null) _sdmNodeTransportTb.Text = _sdmModulator.DopPassthrough ? "DoP (0x05/0xFA)" : "Native DSD";
		if (_sdmNodeActualOutputTb != null) _sdmNodeActualOutputTb.Text = _sdmActualOutputTb?.Text ?? "当前不是 DSD 源";

		// Update Inspector
		if (_inspectorOrderValTb != null) _inspectorOrderValTb.Text = $"{(int)_sdmModulator.Order}th";
		if (_inspectorDitherValTb != null) _inspectorDitherValTb.Text = _sdmModulator.QuantizerStrategy != SdmQuantizerStrategy.Off ? "1.0e-7 FS" : "--";
		if (_inspectorDecayValTb != null) _inspectorDecayValTb.Text = $"{_sdmModulator.InputLimit:0.000}";
		if (_inspectorHeadroomValTb != null) _inspectorHeadroomValTb.Text = $"-{_sdmModulator.RecommendedHeadroomDb:0.0} dB";

		if (_sdmNtfPillTb != null)
		{
			_sdmNtfPillTb.Text = $"{targetRateStr} · {(_sdmModulator.Order == SdmOrder.Order3_Safe ? "safe" : "precision")}";
		}

		// Update Feedback Coefficients Bars
		RenderCoeffBars();
	}

	private void RenderCoeffBars()
	{
		if (_coeffBarsGrid == null || _coeffLabelsGrid == null) return;

		_coeffBarsGrid.Children.Clear();
		_coeffBarsGrid.ColumnDefinitions.Clear();
		_coeffLabelsGrid.Children.Clear();
		_coeffLabelsGrid.ColumnDefinitions.Clear();

		var coeffs = _sdmModulator.FeedbackCoefficients;
		int count = coeffs.Count;
		if (count == 0) return;

		for (int i = 0; i < count; i++)
		{
			_coeffBarsGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.0, GridUnitType.Star) });
			_coeffLabelsGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.0, GridUnitType.Star) });
		}

		double maxAbs = 1.0;
		for (int i = 0; i < count; i++)
		{
			double abs = Math.Abs(coeffs[i]);
			if (abs > maxAbs) maxAbs = abs;
		}

		for (int i = 0; i < count; i++)
		{
			double val = coeffs[i];
			bool isPos = val >= 0;
			double ratio = Math.Clamp(Math.Abs(val) / maxAbs, 0.05, 1.0);
			double barHeight = ratio * 32.0;

			var barCell = new Grid { Height = 74 };
			// Center zero baseline
			barCell.Children.Add(new Rectangle
			{
				Height = 1,
				Fill = new SolidColorBrush(Windows.UI.Color.FromArgb(180, 203, 213, 225)),
				VerticalAlignment = VerticalAlignment.Center
			});

			var barRect = new Border
			{
				Width = 10,
				Height = barHeight,
				CornerRadius = new CornerRadius(2),
				Background = new SolidColorBrush(isPos
					? Windows.UI.Color.FromArgb(255, 49, 85, 217)
					: Windows.UI.Color.FromArgb(255, 245, 158, 11)),
				HorizontalAlignment = HorizontalAlignment.Center,
				VerticalAlignment = isPos ? VerticalAlignment.Bottom : VerticalAlignment.Top,
				Margin = isPos ? new Thickness(0, 0, 0, 37) : new Thickness(0, 37, 0, 0)
			};
			barCell.Children.Add(barRect);
			Grid.SetColumn(barCell, i);
			_coeffBarsGrid.Children.Add(barCell);

			var labelTb = new TextBlock
			{
				Text = $"{val:+0.00;-0.00;0.00}",
				FontSize = 9.5,
				FontFamily = new FontFamily("Consolas, Courier New"),
				Foreground = DspTextMutedBrush,
				HorizontalAlignment = HorizontalAlignment.Center
			};
			Grid.SetColumn(labelTb, i);
			_coeffLabelsGrid.Children.Add(labelTb);
		}
	}
}
