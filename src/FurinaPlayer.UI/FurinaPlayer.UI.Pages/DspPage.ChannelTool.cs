using System;
using System.Collections.Generic;
using System.Linq;
using FurinaPlayer.UI.Controls;
using FurinaPlayer.UI.ViewModels;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using SonicWave.Audio.Dsp;
using SonicWave.Core.Models;
using Windows.UI;

namespace FurinaPlayer.UI.Pages;

/// <summary>
/// FurinaPlayer 声道工具（Channel Tool）二级功能工作台分部实现。
/// 1:1 像素级复刻声像平衡、单侧偏音补偿、3频段左右校准、微秒延迟与相位矩阵控制。
/// </summary>
public sealed partial class DspPage
{
	private FrameworkElement? _channelToolWorkbench;
	private Grid? _channelRootGrid;
	private Border? _channelTitleIconBorder;
	private FontIcon? _channelTitleIcon;
	private TextBlock? _channelNavSubTb;

	// Theme tracking collections
	private readonly List<Border> _channelCardBorders = new();
	private readonly List<Border> _channelFactBoxBorders = new();
	private readonly List<TextBlock> _channelPrimaryTextBlocks = new();
	private readonly List<TextBlock> _channelSecondaryTextBlocks = new();
	private readonly List<TextBlock> _channelMutedTextBlocks = new();
	private readonly List<TextBlock> _channelAccentTextBlocks = new();

	// Top operation row
	private Button? _channelPowerBtn;
	private TextBlock? _channelPowerText;
	private FontIcon? _channelPowerIcon;
	private Button? _channelCompareBtn;
	private Button? _channelResetBtn;
	private FontIcon? _channelResetIcon;
	private TextBlock? _channelResetText;
	private Border? _channelModeSegBorder;
	private Button? _channelModeSimpleBtn;
	private Button? _channelModeProBtn;

	// Triple level readout
	private TextBlock? _leftOutputBigTb;
	private TextBlock? _rightOutputBigTb;
	private TextBlock? _centerStatusBigTb;
	private TextBlock? _centerStatusSubTb;
	private Grid? _leftMeterTrack;
	private Grid? _rightMeterTrack;
	private Border? _leftMeterFill;
	private Border? _rightMeterFill;

	// Pan balance
	private TextBlock? _balanceValTb;
	private Slider? _balanceSlider;

	// Channel bias compensation
	private double _currentTrimStep = 0.25;
	private readonly List<Button> _trimStepButtons = new();
	private Button? _leftTooLoudBtn;
	private Button? _rightTooLoudBtn;
	private HyperlinkButton? _channelClearAuxBtn;
	private HyperlinkButton? _channelSwapAuxBtn;

	// Pro controls
	private Grid? _proContainer;
	private Slider? _proLowLSlider;
	private Slider? _proLowRSlider;
	private Slider? _proMidLSlider;
	private Slider? _proMidRSlider;
	private Slider? _proHighLSlider;
	private Slider? _proHighRSlider;
	private TextBlock? _proLowLTb;
	private TextBlock? _proLowRTb;
	private TextBlock? _proMidLTb;
	private TextBlock? _proMidRTb;
	private TextBlock? _proHighLTb;
	private TextBlock? _proHighRTb;

	private NumberBox? _proLeftGainNb;
	private NumberBox? _proRightGainNb;
	private NumberBox? _proLeftDelayNb;
	private NumberBox? _proRightDelayNb;

	private readonly List<Button> _monoModeButtons = new();
	private CheckBox? _phaseConstantPowerCb;
	private CheckBox? _phaseSwapCb;
	private CheckBox? _phaseInvertLCb;
	private CheckBox? _phaseInvertRCb;

	// Presets
	private TextBlock? _presetEmptyTb;
	private ComboBox? _presetCombo;
	private Button? _presetApplyBtn;
	private Button? _presetRenameBtn;
	private Button? _presetDeleteBtn;
	private TextBox? _presetDraftTb;
	private Button? _presetSaveBtn;

	// Diagnostics Telemetry
	private Expander? _channelDiagExpander;
	private FontIcon? _channelDiagIcon;
	private TextBlock? _factLeftTb;
	private TextBlock? _factRightTb;
	private TextBlock? _factDelaySkewTb;
	private TextBlock? _factStatusDescTb;

	public FrameworkElement CreateChannelToolWorkbench()
	{
		bool isDark = IsDspDark;
		_channelCardBorders.Clear();
		_channelFactBoxBorders.Clear();
		_channelPrimaryTextBlocks.Clear();
		_channelSecondaryTextBlocks.Clear();
		_channelMutedTextBlocks.Clear();
		_channelAccentTextBlocks.Clear();

		var sv = new ScrollViewer
		{
			VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
			HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled
		};

		var root = new Grid
		{
			Background = CreateChannelSurfaceBrush(LiquidGlassVariant.Clear, isDark, Math.Clamp(0.72 / 0.85, 0.55, 1.0)),
			Padding = new Thickness(24, 20, 24, 24),
			RowSpacing = 16
		};
		_channelRootGrid = root;
		root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // 0: Top Header
		root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // 1: Operation row
		root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // 2: Output meter triple card
		root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // 3: Main 2-column card (Pan & Bias)
		root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // 4: Pro controls (collapsible)
		root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // 5: Presets card
		root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // 6: Realtime diagnostics expander

		// =========================================================================
		// 0. Top Header
		// =========================================================================
		var headerGrid = new Grid();
		headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.0, GridUnitType.Star) });
		headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

		var titleSp = new StackPanel
		{
			Orientation = Orientation.Horizontal,
			Spacing = 12,
			VerticalAlignment = VerticalAlignment.Center
		};

		_channelTitleIcon = new FontIcon
		{
			Glyph = "\uE7F6", // Headphone icon
			FontSize = 18,
			Foreground = DspAccentSoftTextBrush,
			HorizontalAlignment = HorizontalAlignment.Center,
			VerticalAlignment = VerticalAlignment.Center
		};
		_channelTitleIconBorder = new Border
		{
			Width = 36,
			Height = 36,
			CornerRadius = new CornerRadius(18),
			Background = DspAccentSoftBgBrush,
			Child = _channelTitleIcon
		};
		titleSp.Children.Add(_channelTitleIconBorder);
		var titleTb = new TextBlock
		{
			Text = "声道工具",
			FontSize = 22,
			FontWeight = Microsoft.UI.Text.FontWeights.Bold,
			Foreground = DspTextPrimaryBrush,
			VerticalAlignment = VerticalAlignment.Center
		};
		_channelPrimaryTextBlocks.Add(titleTb);
		titleSp.Children.Add(titleTb);
		Grid.SetColumn(titleSp, 0);
		headerGrid.Children.Add(titleSp);
		Grid.SetRow(headerGrid, 0);
		root.Children.Add(headerGrid);

		// =========================================================================
		// 1. Operation Row (Power, A/B Compare, Reset, [Simple | Pro])
		// =========================================================================
		var opGrid = new Grid();
		opGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.0, GridUnitType.Star) });
		opGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

		var opLeftSp = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 10, VerticalAlignment = VerticalAlignment.Center };

		// Power button
		_channelPowerBtn = new Button
		{
			Padding = new Thickness(12, 6, 12, 6),
			CornerRadius = new CornerRadius(8),
			Background = CreateChannelSurfaceBrush(LiquidGlassVariant.Clear, isDark, 0.92),
			BorderBrush = CreateChannelRimBrush(isDark),
			BorderThickness = new Thickness(1),
			Foreground = DspTextSubHeaderBrush
		};
		var powerSp = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 6 };
		_channelPowerIcon = new FontIcon { Glyph = "\uE7E8", FontSize = 14, Foreground = DspTextMutedBrush };
		_channelPowerText = new TextBlock { Text = "已关闭", FontSize = 13, FontWeight = Microsoft.UI.Text.FontWeights.SemiBold, Foreground = DspTextSubHeaderBrush };
		powerSp.Children.Add(_channelPowerIcon);
		powerSp.Children.Add(_channelPowerText);
		_channelPowerBtn.Content = powerSp;
		_channelPowerBtn.Click += (s, e) =>
		{
			if (ViewModel != null)
			{
				ViewModel.FurinaChannelToolEnabled = !ViewModel.FurinaChannelToolEnabled;
				ViewModel.ApplyChannelToolSettings();
				RefreshChannelToolStatus();
			}
		};
		opLeftSp.Children.Add(_channelPowerBtn);

		// A/B Compare button
		_channelCompareBtn = new Button
		{
			Content = "A/B 对比",
			Padding = new Thickness(12, 6, 12, 6),
			CornerRadius = new CornerRadius(8),
			Background = CreateChannelSurfaceBrush(LiquidGlassVariant.Clear, isDark, 0.92),
			BorderBrush = CreateChannelRimBrush(isDark),
			BorderThickness = new Thickness(1),
			Foreground = DspTextSubHeaderBrush
		};
		_channelCompareBtn.Click += (s, e) =>
		{
			if (ViewModel != null)
			{
				ViewModel.FurinaChannelCompareBypassed = !ViewModel.FurinaChannelCompareBypassed;
				ViewModel.ApplyChannelToolSettings();
				RefreshChannelToolStatus();
			}
		};
		opLeftSp.Children.Add(_channelCompareBtn);

		// Reset button
		_channelResetIcon = new FontIcon { Glyph = "\uE777", FontSize = 13, Foreground = DspTextMutedBrush };
		_channelResetText = new TextBlock { Text = "重置", FontSize = 13, Foreground = DspTextSubHeaderBrush };
		_channelResetBtn = new Button
		{
			Content = new StackPanel
			{
				Orientation = Orientation.Horizontal,
				Spacing = 5,
				Children =
				{
					_channelResetIcon,
					_channelResetText
				}
			},
			Padding = new Thickness(12, 6, 12, 6),
			CornerRadius = new CornerRadius(8),
			Background = CreateChannelSurfaceBrush(LiquidGlassVariant.Clear, isDark, 0.92),
			BorderBrush = CreateChannelRimBrush(isDark),
			BorderThickness = new Thickness(1),
			Foreground = DspTextSubHeaderBrush
		};
		_channelResetBtn.Click += (s, e) =>
		{
			if (ViewModel != null)
			{
				ViewModel.FurinaChannelBalance = 0.0;
				ViewModel.FurinaChannelLeftGainDb = 0.0;
				ViewModel.FurinaChannelRightGainDb = 0.0;
				ViewModel.FurinaChannelLowLeftGainDb = 0.0;
				ViewModel.FurinaChannelLowRightGainDb = 0.0;
				ViewModel.FurinaChannelMidLeftGainDb = 0.0;
				ViewModel.FurinaChannelMidRightGainDb = 0.0;
				ViewModel.FurinaChannelHighLeftGainDb = 0.0;
				ViewModel.FurinaChannelHighRightGainDb = 0.0;
				ViewModel.FurinaChannelLeftDelayMs = 0.0;
				ViewModel.FurinaChannelRightDelayMs = 0.0;
				ViewModel.FurinaChannelMonoMode = "off";
				ViewModel.FurinaChannelSwapLeftRight = false;
				ViewModel.FurinaChannelInvertLeft = false;
				ViewModel.FurinaChannelInvertRight = false;
				ViewModel.FurinaChannelCompareBypassed = false;
				ViewModel.ApplyChannelToolSettings();
				RefreshChannelToolStatus();
			}
		};
		opLeftSp.Children.Add(_channelResetBtn);
		Grid.SetColumn(opLeftSp, 0);
		opGrid.Children.Add(opLeftSp);

		// Right Segmented Switch: [ Simple | Pro ]
		_channelModeSegBorder = new Border
		{
			Background = GetChannelSoftSurfaceBrush(isDark),
			BorderBrush = CreateChannelRimBrush(isDark),
			BorderThickness = new Thickness(1),
			CornerRadius = new CornerRadius(16),
			Padding = new Thickness(3)
		};
		var modeSegSp = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 2 };

		_channelModeSimpleBtn = new Button
		{
			Content = "Simple",
			Padding = new Thickness(14, 4, 14, 4),
			CornerRadius = new CornerRadius(13),
			Background = new SolidColorBrush(isDark ? Color.FromArgb(95, 99, 102, 241) : Colors.White),
			BorderThickness = new Thickness(0),
			FontSize = 13,
			FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
			Foreground = DspAccentSoftTextBrush
		};
		_channelModeProBtn = new Button
		{
			Content = "Pro",
			Padding = new Thickness(14, 4, 14, 4),
			CornerRadius = new CornerRadius(13),
			Background = new SolidColorBrush(Colors.Transparent),
			BorderThickness = new Thickness(0),
			FontSize = 13,
			FontWeight = Microsoft.UI.Text.FontWeights.Normal,
			Foreground = DspTextMutedBrush
		};

		_channelModeSimpleBtn.Click += (s, e) => SetChannelMode("simple");
		_channelModeProBtn.Click += (s, e) => SetChannelMode("pro");

		modeSegSp.Children.Add(_channelModeSimpleBtn);
		modeSegSp.Children.Add(_channelModeProBtn);
		_channelModeSegBorder.Child = modeSegSp;
		Grid.SetColumn(_channelModeSegBorder, 1);
		opGrid.Children.Add(_channelModeSegBorder);

		Grid.SetRow(opGrid, 1);
		root.Children.Add(opGrid);

		// =========================================================================
		// 2. Output Meter Triple Card (Left, Center, Right)
		// =========================================================================
		var meterCard = new Border
		{
			Background = CreateChannelSurfaceBrush(LiquidGlassVariant.Regular, isDark, Math.Clamp(0.88 / 0.90, 0.72, 1.05)),
			BorderBrush = CreateChannelRimBrush(isDark),
			BorderThickness = new Thickness(1),
			CornerRadius = new CornerRadius(12),
			Padding = new Thickness(24, 16, 24, 18)
		};
		_channelCardBorders.Add(meterCard);

		var meterGrid = new Grid();
		meterGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.0, GridUnitType.Star) });
		meterGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(180, GridUnitType.Pixel) });
		meterGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.0, GridUnitType.Star) });

		// Left output
		var leftSp = new StackPanel { Spacing = 4, HorizontalAlignment = HorizontalAlignment.Left };
		var leftOutLbl = new TextBlock { Text = "左输出", FontSize = 12, Foreground = DspTextMutedBrush };
		_channelMutedTextBlocks.Add(leftOutLbl);
		leftSp.Children.Add(leftOutLbl);
		_leftOutputBigTb = new TextBlock
		{
			Text = "0 dB",
			FontSize = 30,
			FontWeight = Microsoft.UI.Text.FontWeights.Bold,
			Foreground = DspTextPrimaryBrush
		};
		_channelPrimaryTextBlocks.Add(_leftOutputBigTb);
		leftSp.Children.Add(_leftOutputBigTb);

		_leftMeterTrack = new Grid
		{
			Height = 6,
			CornerRadius = new CornerRadius(3),
			Background = GetChannelMeterTrackBrush(isDark),
			Margin = new Thickness(0, 4, 0, 0)
		};
		_leftMeterFill = new Border
		{
			HorizontalAlignment = HorizontalAlignment.Left,
			CornerRadius = new CornerRadius(3),
			Background = new SolidColorBrush(Color.FromArgb(255, 99, 102, 241)),
			Width = 140
		};
		_leftMeterTrack.Children.Add(_leftMeterFill);
		leftSp.Children.Add(_leftMeterTrack);
		Grid.SetColumn(leftSp, 0);
		meterGrid.Children.Add(leftSp);

		// Center status
		var centerSp = new StackPanel
		{
			Spacing = 4,
			HorizontalAlignment = HorizontalAlignment.Center,
			VerticalAlignment = VerticalAlignment.Center
		};
		_centerStatusBigTb = new TextBlock
		{
			Text = "中心稳定",
			FontSize = 18,
			FontWeight = Microsoft.UI.Text.FontWeights.Bold,
			Foreground = DspTextPrimaryBrush,
			HorizontalAlignment = HorizontalAlignment.Center
		};
		_channelPrimaryTextBlocks.Add(_centerStatusBigTb);
		_centerStatusSubTb = new TextBlock
		{
			Text = "已关闭",
			FontSize = 12,
			Foreground = DspTextMutedBrush,
			HorizontalAlignment = HorizontalAlignment.Center
		};
		centerSp.Children.Add(_centerStatusBigTb);
		centerSp.Children.Add(_centerStatusSubTb);
		Grid.SetColumn(centerSp, 1);
		meterGrid.Children.Add(centerSp);

		// Right output
		var rightSp = new StackPanel { Spacing = 4, HorizontalAlignment = HorizontalAlignment.Right };
		var rightOutLbl = new TextBlock { Text = "右输出", FontSize = 12, Foreground = DspTextMutedBrush, HorizontalAlignment = HorizontalAlignment.Right };
		_channelMutedTextBlocks.Add(rightOutLbl);
		rightSp.Children.Add(rightOutLbl);
		_rightOutputBigTb = new TextBlock
		{
			Text = "0 dB",
			FontSize = 30,
			FontWeight = Microsoft.UI.Text.FontWeights.Bold,
			Foreground = DspTextPrimaryBrush,
			HorizontalAlignment = HorizontalAlignment.Right
		};
		_channelPrimaryTextBlocks.Add(_rightOutputBigTb);
		rightSp.Children.Add(_rightOutputBigTb);

		_rightMeterTrack = new Grid
		{
			Height = 6,
			CornerRadius = new CornerRadius(3),
			Background = GetChannelMeterTrackBrush(isDark),
			Margin = new Thickness(0, 4, 0, 0)
		};
		_rightMeterFill = new Border
		{
			HorizontalAlignment = HorizontalAlignment.Right,
			CornerRadius = new CornerRadius(3),
			Background = new SolidColorBrush(Color.FromArgb(255, 99, 102, 241)),
			Width = 140
		};
		_rightMeterTrack.Children.Add(_rightMeterFill);
		rightSp.Children.Add(_rightMeterTrack);
		Grid.SetColumn(rightSp, 2);
		meterGrid.Children.Add(rightSp);

		meterCard.Child = meterGrid;
		Grid.SetRow(meterCard, 2);
		root.Children.Add(meterCard);

		// =========================================================================
		// 3. Main 2-Column Controls (Pan Balance & Channel Bias Compensation)
		// =========================================================================
		var mainControlsGrid = new Grid { ColumnSpacing = 20 };
		mainControlsGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.0, GridUnitType.Star) });
		mainControlsGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.0, GridUnitType.Star) });

		// --- Left Column: 声像平衡 ---
		var panCard = new Border
		{
			Background = CreateChannelSurfaceBrush(LiquidGlassVariant.Regular, isDark, Math.Clamp(0.88 / 0.90, 0.72, 1.05)),
			BorderBrush = CreateChannelRimBrush(isDark),
			BorderThickness = new Thickness(1),
			CornerRadius = new CornerRadius(12),
			Padding = new Thickness(20, 16, 20, 18)
		};
		_channelCardBorders.Add(panCard);
		var panSp = new StackPanel { Spacing = 14 };

		var panHeadGrid = new Grid();
		panHeadGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.0, GridUnitType.Star) });
		panHeadGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

		var panTitleTb = new TextBlock
		{
			Text = "声像平衡",
			FontSize = 16,
			FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
			Foreground = DspTextPrimaryBrush,
			VerticalAlignment = VerticalAlignment.Center
		};
		_channelPrimaryTextBlocks.Add(panTitleTb);
		_balanceValTb = new TextBlock
		{
			Text = "0%",
			FontSize = 14,
			FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
			Foreground = DspAccentSoftTextBrush,
			VerticalAlignment = VerticalAlignment.Center
		};
		_channelAccentTextBlocks.Add(_balanceValTb);
		Grid.SetColumn(panTitleTb, 0);
		Grid.SetColumn(_balanceValTb, 1);
		panHeadGrid.Children.Add(panTitleTb);
		panHeadGrid.Children.Add(_balanceValTb);
		panSp.Children.Add(panHeadGrid);

		_balanceSlider = new Slider
		{
			Minimum = -100,
			Maximum = 100,
			StepFrequency = 0.5,
			Value = 0,
			TickFrequency = 50,
			TickPlacement = Microsoft.UI.Xaml.Controls.Primitives.TickPlacement.None,
			Margin = new Thickness(0, 6, 0, 0)
		};
		_balanceSlider.ValueChanged += (s, e) =>
		{
			if (ViewModel != null)
			{
				ViewModel.FurinaChannelBalance = Math.Round(e.NewValue) / 100.0;
				ViewModel.FurinaChannelToolEnabled = true;
				ViewModel.ApplyChannelToolSettings();
				RefreshChannelToolStatus();
			}
		};
		panSp.Children.Add(_balanceSlider);

		var panLabelsGrid = new Grid();
		panLabelsGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.0, GridUnitType.Star) });
		panLabelsGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
		panLabelsGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.0, GridUnitType.Star) });

		var l100Tb = new TextBlock { Text = "L 100%", FontSize = 11, Foreground = DspTextMutedBrush, HorizontalAlignment = HorizontalAlignment.Left };
		var zeroTb = new TextBlock { Text = "0", FontSize = 11, Foreground = DspTextMutedBrush, HorizontalAlignment = HorizontalAlignment.Center };
		var r100Tb = new TextBlock { Text = "R 100%", FontSize = 11, Foreground = DspTextMutedBrush, HorizontalAlignment = HorizontalAlignment.Right };
		_channelMutedTextBlocks.Add(l100Tb);
		_channelMutedTextBlocks.Add(zeroTb);
		_channelMutedTextBlocks.Add(r100Tb);
		Grid.SetColumn(l100Tb, 0);
		Grid.SetColumn(zeroTb, 1);
		Grid.SetColumn(r100Tb, 2);
		panLabelsGrid.Children.Add(l100Tb);
		panLabelsGrid.Children.Add(zeroTb);
		panLabelsGrid.Children.Add(r100Tb);
		panSp.Children.Add(panLabelsGrid);

		panCard.Child = panSp;
		Grid.SetColumn(panCard, 0);
		mainControlsGrid.Children.Add(panCard);

		// --- Right Column: 偏音补偿 ---
		var biasCard = new Border
		{
			Background = CreateChannelSurfaceBrush(LiquidGlassVariant.Regular, isDark, Math.Clamp(0.88 / 0.90, 0.72, 1.05)),
			BorderBrush = CreateChannelRimBrush(isDark),
			BorderThickness = new Thickness(1),
			CornerRadius = new CornerRadius(12),
			Padding = new Thickness(20, 16, 20, 18)
		};
		_channelCardBorders.Add(biasCard);
		var biasSp = new StackPanel { Spacing = 12 };

		var biasHeadSp = new StackPanel { Spacing = 8 };

		var biasTitleTb = new TextBlock
		{
			Text = "偏音补偿",
			FontSize = 16,
			FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
			Foreground = DspTextPrimaryBrush
		};
		_channelPrimaryTextBlocks.Add(biasTitleTb);
		biasHeadSp.Children.Add(biasTitleTb);

		// Step gain pills: +0.1 dB, +0.25 dB, +0.5 dB, +1 dB
		var stepsSp = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 6 };
		_trimStepButtons.Clear();
		double[] steps = { 0.1, 0.25, 0.5, 1.0 };
		foreach (var step in steps)
		{
			var stepBtn = new Button
			{
				Content = $"+{step} dB",
				Padding = new Thickness(9, 3, 9, 3),
				CornerRadius = new CornerRadius(10),
				FontSize = 11,
				BorderThickness = new Thickness(1),
				Tag = step
			};
			stepBtn.Click += (s, e) =>
			{
				if (s is Button b && b.Tag is double val)
				{
					_currentTrimStep = val;
					if (ViewModel != null)
					{
						ViewModel.FurinaChannelTrimStepDb = val;
						ViewModel.SaveEchoSettings();
					}
					UpdateStepPills(IsDspDark);
				}
			};
			_trimStepButtons.Add(stepBtn);
			stepsSp.Children.Add(stepBtn);
		}
		biasHeadSp.Children.Add(stepsSp);
		biasSp.Children.Add(biasHeadSp);

		// Two Big Card Buttons: [ 左侧偏响 ] and [ 右侧偏响 ]
		var cardsGrid = new Grid { ColumnSpacing = 12 };
		cardsGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.0, GridUnitType.Star) });
		cardsGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.0, GridUnitType.Star) });

		_leftTooLoudBtn = new Button
		{
			Content = "左侧偏响",
			Height = 44,
			HorizontalAlignment = HorizontalAlignment.Stretch,
			CornerRadius = new CornerRadius(8),
			Background = CreateChannelSurfaceBrush(LiquidGlassVariant.Clear, isDark, 0.92),
			BorderBrush = CreateChannelRimBrush(isDark),
			BorderThickness = new Thickness(1),
			FontSize = 14,
			FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
			Foreground = DspTextPrimaryBrush
		};
		_leftTooLoudBtn.Click += (s, e) =>
		{
			if (ViewModel != null)
			{
				ViewModel.FurinaChannelLeftGainDb = Math.Clamp(ViewModel.FurinaChannelLeftGainDb - _currentTrimStep, -12.0, 6.0);
				ViewModel.FurinaChannelToolEnabled = true;
				ViewModel.ApplyChannelToolSettings();
				RefreshChannelToolStatus();
			}
		};
		Grid.SetColumn(_leftTooLoudBtn, 0);
		cardsGrid.Children.Add(_leftTooLoudBtn);

		_rightTooLoudBtn = new Button
		{
			Content = "右侧偏响",
			Height = 44,
			HorizontalAlignment = HorizontalAlignment.Stretch,
			CornerRadius = new CornerRadius(8),
			Background = CreateChannelSurfaceBrush(LiquidGlassVariant.Clear, isDark, 0.92),
			BorderBrush = CreateChannelRimBrush(isDark),
			BorderThickness = new Thickness(1),
			FontSize = 14,
			FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
			Foreground = DspTextPrimaryBrush
		};
		_rightTooLoudBtn.Click += (s, e) =>
		{
			if (ViewModel != null)
			{
				ViewModel.FurinaChannelRightGainDb = Math.Clamp(ViewModel.FurinaChannelRightGainDb - _currentTrimStep, -12.0, 6.0);
				ViewModel.FurinaChannelToolEnabled = true;
				ViewModel.ApplyChannelToolSettings();
				RefreshChannelToolStatus();
			}
		};
		Grid.SetColumn(_rightTooLoudBtn, 1);
		cardsGrid.Children.Add(_rightTooLoudBtn);
		biasSp.Children.Add(cardsGrid);

		// Description note
		var biasHintTb = new TextBlock
		{
			Text = "默认只降低偏响一侧，适合不可维修的耳机偏音补偿。",
			FontSize = 12,
			Foreground = DspTextMutedBrush
		};
		_channelMutedTextBlocks.Add(biasHintTb);
		biasSp.Children.Add(biasHintTb);

		// Auxiliary Action Links: "偏音清零" and "交换补偿方向"
		var auxSp = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 16 };

		_channelClearAuxBtn = new HyperlinkButton
		{
			Content = "偏音清零",
			FontSize = 12,
			Padding = new Thickness(0),
			Foreground = DspAccentSoftTextBrush
		};
		_channelClearAuxBtn.Click += (s, e) =>
		{
			if (ViewModel != null)
			{
				ViewModel.FurinaChannelLeftGainDb = 0.0;
				ViewModel.FurinaChannelRightGainDb = 0.0;
				ViewModel.FurinaChannelBalance = 0.0;
				ViewModel.FurinaChannelLowLeftGainDb = 0.0;
				ViewModel.FurinaChannelLowRightGainDb = 0.0;
				ViewModel.FurinaChannelMidLeftGainDb = 0.0;
				ViewModel.FurinaChannelMidRightGainDb = 0.0;
				ViewModel.FurinaChannelHighLeftGainDb = 0.0;
				ViewModel.FurinaChannelHighRightGainDb = 0.0;
				ViewModel.FurinaChannelLeftDelayMs = 0.0;
				ViewModel.FurinaChannelRightDelayMs = 0.0;
				ViewModel.ApplyChannelToolSettings();
				RefreshChannelToolStatus();
			}
		};
		auxSp.Children.Add(_channelClearAuxBtn);

		_channelSwapAuxBtn = new HyperlinkButton
		{
			Content = "交换补偿方向",
			FontSize = 12,
			Padding = new Thickness(0),
			Foreground = DspAccentSoftTextBrush
		};
		_channelSwapAuxBtn.Click += (s, e) =>
		{
			if (ViewModel != null)
			{
				double tmpGain = ViewModel.FurinaChannelLeftGainDb;
				ViewModel.FurinaChannelLeftGainDb = ViewModel.FurinaChannelRightGainDb;
				ViewModel.FurinaChannelRightGainDb = tmpGain;

				double tmpDelay = ViewModel.FurinaChannelLeftDelayMs;
				ViewModel.FurinaChannelLeftDelayMs = ViewModel.FurinaChannelRightDelayMs;
				ViewModel.FurinaChannelRightDelayMs = tmpDelay;

				double tmpLow = ViewModel.FurinaChannelLowLeftGainDb;
				ViewModel.FurinaChannelLowLeftGainDb = ViewModel.FurinaChannelLowRightGainDb;
				ViewModel.FurinaChannelLowRightGainDb = tmpLow;

				double tmpMid = ViewModel.FurinaChannelMidLeftGainDb;
				ViewModel.FurinaChannelMidLeftGainDb = ViewModel.FurinaChannelMidRightGainDb;
				ViewModel.FurinaChannelMidRightGainDb = tmpMid;

				double tmpHigh = ViewModel.FurinaChannelHighLeftGainDb;
				ViewModel.FurinaChannelHighLeftGainDb = ViewModel.FurinaChannelHighRightGainDb;
				ViewModel.FurinaChannelHighRightGainDb = tmpHigh;

				ViewModel.FurinaChannelToolEnabled = true;
				ViewModel.ApplyChannelToolSettings();
				RefreshChannelToolStatus();
			}
		};
		auxSp.Children.Add(_channelSwapAuxBtn);
		biasSp.Children.Add(auxSp);

		biasCard.Child = biasSp;
		Grid.SetColumn(biasCard, 1);
		mainControlsGrid.Children.Add(biasCard);

		Grid.SetRow(mainControlsGrid, 3);
		root.Children.Add(mainControlsGrid);

		// =========================================================================
		// 4. Pro Mode Advanced Controls (Bands, Delays, Mono, Phase)
		// =========================================================================
		_proContainer = CreateProControlsPanel(isDark);
		_proContainer.Visibility = Visibility.Collapsed;
		Grid.SetRow(_proContainer, 4);
		root.Children.Add(_proContainer);

		// =========================================================================
		// 5. Headphone Presets Card (耳机方案)
		// =========================================================================
		var presetCard = new Border
		{
			Background = CreateChannelSurfaceBrush(LiquidGlassVariant.Regular, isDark, Math.Clamp(0.88 / 0.90, 0.72, 1.05)),
			BorderBrush = CreateChannelRimBrush(isDark),
			BorderThickness = new Thickness(1),
			CornerRadius = new CornerRadius(12),
			Padding = new Thickness(20, 16, 20, 18)
		};
		_channelCardBorders.Add(presetCard);
		var presetGrid = new Grid();
		presetGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.0, GridUnitType.Star) });
		presetGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

		var presetLeftSp = new StackPanel { Spacing = 8, VerticalAlignment = VerticalAlignment.Center };
		var presetTitleTb = new TextBlock
		{
			Text = "耳机方案",
			FontSize = 16,
			FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
			Foreground = DspTextPrimaryBrush
		};
		_channelPrimaryTextBlocks.Add(presetTitleTb);
		presetLeftSp.Children.Add(presetTitleTb);

		var presetPickerSp = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
		_presetEmptyTb = new TextBlock
		{
			Text = "还没有保存的声道方案。",
			FontSize = 13,
			Foreground = DspTextMutedBrush,
			VerticalAlignment = VerticalAlignment.Center
		};
		_channelMutedTextBlocks.Add(_presetEmptyTb);
		presetPickerSp.Children.Add(_presetEmptyTb);

		_presetCombo = new ComboBox
		{
			Width = 180,
			Height = 36,
			PlaceholderText = "选择耳机方案",
			Background = CreateChannelSurfaceBrush(LiquidGlassVariant.Clear, isDark, 0.92),
			BorderBrush = CreateChannelRimBrush(isDark),
			BorderThickness = new Thickness(1),
			Foreground = DspTextPrimaryBrush,
			Visibility = Visibility.Collapsed
		};
		presetPickerSp.Children.Add(_presetCombo);

		_presetApplyBtn = new Button
		{
			Content = "切换",
			Height = 36,
			Padding = new Thickness(12, 0, 12, 0),
			CornerRadius = new CornerRadius(8),
			Background = CreateChannelSurfaceBrush(LiquidGlassVariant.Clear, isDark, 0.92),
			BorderBrush = CreateChannelRimBrush(isDark),
			BorderThickness = new Thickness(1),
			Foreground = DspTextPrimaryBrush,
			Visibility = Visibility.Collapsed
		};
		_presetApplyBtn.Click += (s, e) =>
		{
			if (_presetCombo?.SelectedItem is ChannelToolPreset p && ViewModel != null)
			{
				ViewModel.ApplyChannelPreset(p);
				RefreshChannelToolStatus();
			}
		};
		presetPickerSp.Children.Add(_presetApplyBtn);

		_presetRenameBtn = new Button
		{
			Content = "重命名",
			Height = 36,
			Padding = new Thickness(12, 0, 12, 0),
			CornerRadius = new CornerRadius(8),
			Background = CreateChannelSurfaceBrush(LiquidGlassVariant.Clear, isDark, 0.92),
			BorderBrush = CreateChannelRimBrush(isDark),
			BorderThickness = new Thickness(1),
			Foreground = DspTextPrimaryBrush,
			Visibility = Visibility.Collapsed
		};
		_presetRenameBtn.Click += (s, e) =>
		{
			if (_presetCombo?.SelectedItem is ChannelToolPreset p && ViewModel != null && _presetDraftTb != null)
			{
				ViewModel.RenameChannelPreset(p.Id, _presetDraftTb.Text);
				RefreshPresetsUI();
			}
		};
		presetPickerSp.Children.Add(_presetRenameBtn);

		_presetDeleteBtn = new Button
		{
			Content = "移除",
			Height = 36,
			Padding = new Thickness(12, 0, 12, 0),
			CornerRadius = new CornerRadius(8),
			Background = CreateChannelSurfaceBrush(LiquidGlassVariant.Clear, isDark, 0.92),
			BorderBrush = CreateChannelRimBrush(isDark),
			BorderThickness = new Thickness(1),
			Foreground = DspTextPrimaryBrush,
			Visibility = Visibility.Collapsed
		};
		_presetDeleteBtn.Click += (s, e) =>
		{
			if (_presetCombo?.SelectedItem is ChannelToolPreset p && ViewModel != null)
			{
				ViewModel.DeleteChannelPreset(p.Id);
				RefreshPresetsUI();
			}
		};
		presetPickerSp.Children.Add(_presetDeleteBtn);
		presetLeftSp.Children.Add(presetPickerSp);
		Grid.SetColumn(presetLeftSp, 0);
		presetGrid.Children.Add(presetLeftSp);

		// Right: Save draft and button
		var presetRightSp = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 10, VerticalAlignment = VerticalAlignment.Center };
		_presetDraftTb = new TextBox
		{
			Text = "耳机偏音补偿",
			Width = 160,
			Height = 36,
			CornerRadius = new CornerRadius(8),
			Background = GetChannelInputBgBrush(isDark),
			BorderBrush = CreateChannelRimBrush(isDark),
			Foreground = DspTextPrimaryBrush,
			VerticalAlignment = VerticalAlignment.Center
		};
		presetRightSp.Children.Add(_presetDraftTb);

		_presetSaveBtn = new Button
		{
			Content = new StackPanel
			{
				Orientation = Orientation.Horizontal,
				Spacing = 6,
				Children =
				{
					new FontIcon { Glyph = "\uE74E", FontSize = 13, Foreground = new SolidColorBrush(Colors.White) },
					new TextBlock { Text = "保存当前参数", FontSize = 13, FontWeight = Microsoft.UI.Text.FontWeights.SemiBold, Foreground = new SolidColorBrush(Colors.White) }
				}
			},
			Height = 36,
			Padding = new Thickness(14, 0, 14, 0),
			CornerRadius = new CornerRadius(8),
			Background = new SolidColorBrush(Color.FromArgb(255, 79, 70, 229)),
			Foreground = new SolidColorBrush(Colors.White),
			BorderThickness = new Thickness(0),
			VerticalAlignment = VerticalAlignment.Center
		};
		_presetSaveBtn.Click += (s, e) =>
		{
			if (ViewModel != null && _presetDraftTb != null)
			{
				ViewModel.SaveChannelPreset(_presetDraftTb.Text);
				RefreshPresetsUI();
			}
		};
		presetRightSp.Children.Add(_presetSaveBtn);
		Grid.SetColumn(presetRightSp, 1);
		presetGrid.Children.Add(presetRightSp);

		presetCard.Child = presetGrid;
		Grid.SetRow(presetCard, 5);
		root.Children.Add(presetCard);

		// =========================================================================
		// 6. Realtime Diagnostics Expander (实时状态)
		// =========================================================================
		_channelDiagExpander = new Expander
		{
			HorizontalAlignment = HorizontalAlignment.Stretch,
			Background = CreateChannelSurfaceBrush(LiquidGlassVariant.Regular, isDark, Math.Clamp(0.88 / 0.90, 0.72, 1.05)),
			BorderBrush = CreateChannelRimBrush(isDark),
			BorderThickness = new Thickness(1),
			Foreground = DspTextPrimaryBrush,
			CornerRadius = new CornerRadius(12),
			Padding = new Thickness(16, 12, 16, 12),
			IsExpanded = false
		};

		var expHeader = new StackPanel
		{
			Orientation = Orientation.Horizontal,
			Spacing = 8
		};
		_channelDiagIcon = new FontIcon { Glyph = "\uE95D", FontSize = 14, Foreground = DspAccentSoftTextBrush };
		var diagTitleTb = new TextBlock
		{
			Text = "实时状态",
			FontSize = 14,
			FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
			Foreground = DspTextPrimaryBrush
		};
		_channelPrimaryTextBlocks.Add(diagTitleTb);
		expHeader.Children.Add(_channelDiagIcon);
		expHeader.Children.Add(diagTitleTb);
		_channelDiagExpander.Header = expHeader;

		var expContentSp = new StackPanel { Spacing = 12 };

		var factsGrid = new Grid { ColumnSpacing = 16 };
		factsGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.0, GridUnitType.Star) });
		factsGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.0, GridUnitType.Star) });
		factsGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.0, GridUnitType.Star) });
		factsGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.0, GridUnitType.Star) });

		_factLeftTb = new TextBlock { Text = "0.00 dB", FontSize = 14, FontWeight = Microsoft.UI.Text.FontWeights.SemiBold, Foreground = DspTextPrimaryBrush };
		_factRightTb = new TextBlock { Text = "0.00 dB", FontSize = 14, FontWeight = Microsoft.UI.Text.FontWeights.SemiBold, Foreground = DspTextPrimaryBrush };
		_factDelaySkewTb = new TextBlock { Text = "0.00 ms", FontSize = 14, FontWeight = Microsoft.UI.Text.FontWeights.SemiBold, Foreground = DspTextPrimaryBrush };
		_factStatusDescTb = new TextBlock { Text = "建议从 0.25 dB 开始微调", FontSize = 13, Foreground = DspTextMutedBrush };
		_channelPrimaryTextBlocks.Add(_factLeftTb);
		_channelPrimaryTextBlocks.Add(_factRightTb);
		_channelPrimaryTextBlocks.Add(_factDelaySkewTb);
		_channelMutedTextBlocks.Add(_factStatusDescTb);

		factsGrid.Children.Add(CreateFactBox("左总增益", _factLeftTb, 0, isDark));
		factsGrid.Children.Add(CreateFactBox("右总增益", _factRightTb, 1, isDark));
		factsGrid.Children.Add(CreateFactBox("左右延迟差", _factDelaySkewTb, 2, isDark));
		factsGrid.Children.Add(CreateFactBox("推荐建议", _factStatusDescTb, 3, isDark));
		expContentSp.Children.Add(factsGrid);

		var noteTb = new TextBlock
		{
			Text = "声道工具已从参数 EQ 中分离，适合检查声像、左右耳差异和单声道兼容。",
			FontSize = 12,
			Foreground = DspTextMutedBrush,
			Margin = new Thickness(0, 4, 0, 0)
		};
		_channelMutedTextBlocks.Add(noteTb);
		expContentSp.Children.Add(noteTb);
		_channelDiagExpander.Content = expContentSp;

		Grid.SetRow(_channelDiagExpander, 6);
		root.Children.Add(_channelDiagExpander);

		sv.Content = root;
		_channelToolWorkbench = new Border
		{
			RequestedTheme = isDark ? ElementTheme.Dark : ElementTheme.Light,
			Child = sv
		};

		// Initial load
		RefreshChannelToolTheme(isDark);
		RefreshPresetsUI();

		return _channelToolWorkbench;
	}

	private Grid CreateProControlsPanel(bool isDark)
	{
		var proGrid = new Grid { RowSpacing = 16 };
		proGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // 0: 3-band table
		proGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // 1: Advanced fields & mono/phase

		// --- 0. 3-Band Equalization ---
		var bandCard = new Border
		{
			Background = CreateChannelSurfaceBrush(LiquidGlassVariant.Regular, isDark, Math.Clamp(0.88 / 0.90, 0.72, 1.05)),
			BorderBrush = CreateChannelRimBrush(isDark),
			BorderThickness = new Thickness(1),
			CornerRadius = new CornerRadius(12),
			Padding = new Thickness(20, 16, 20, 18)
		};
		_channelCardBorders.Add(bandCard);
		var bandSp = new StackPanel { Spacing = 12 };
		var bandTitleTb = new TextBlock
		{
			Text = "分频段左右补偿",
			FontSize = 16,
			FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
			Foreground = DspTextPrimaryBrush
		};
		_channelPrimaryTextBlocks.Add(bandTitleTb);
		bandSp.Children.Add(bandTitleTb);
		var bandSubTb = new TextBlock
		{
			Text = "默认只降低偏响一侧，适合高低频不均衡的耳机偏音校准。",
			FontSize = 12,
			Foreground = DspTextMutedBrush
		};
		_channelMutedTextBlocks.Add(bandSubTb);
		bandSp.Children.Add(bandSubTb);

		var bandGrid = new Grid { RowSpacing = 10 };
		bandGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(140, GridUnitType.Pixel) });
		bandGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.0, GridUnitType.Star) });
		bandGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.0, GridUnitType.Star) });

		// Header
		var h0 = new TextBlock { Text = "频段", FontSize = 12, FontWeight = Microsoft.UI.Text.FontWeights.SemiBold, Foreground = DspTextMutedBrush };
		var h1 = new TextBlock { Text = "左输出补偿 (dB)", FontSize = 12, FontWeight = Microsoft.UI.Text.FontWeights.SemiBold, Foreground = DspTextMutedBrush };
		var h2 = new TextBlock { Text = "右输出补偿 (dB)", FontSize = 12, FontWeight = Microsoft.UI.Text.FontWeights.SemiBold, Foreground = DspTextMutedBrush };
		_channelMutedTextBlocks.Add(h0);
		_channelMutedTextBlocks.Add(h1);
		_channelMutedTextBlocks.Add(h2);
		Grid.SetColumn(h0, 0); Grid.SetColumn(h1, 1); Grid.SetColumn(h2, 2);
		bandGrid.Children.Add(h0); bandGrid.Children.Add(h1); bandGrid.Children.Add(h2);

		// Low band
		bandGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
		bandGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
		var lowLbl = new TextBlock { Text = "低频 (< 250 Hz)", FontSize = 13, FontWeight = Microsoft.UI.Text.FontWeights.Medium, Foreground = DspTextSecondaryBrush, VerticalAlignment = VerticalAlignment.Center };
		_channelSecondaryTextBlocks.Add(lowLbl);
		_proLowLSlider = new Slider { Minimum = -6, Maximum = 3, StepFrequency = 0.1, Value = 0, Margin = new Thickness(0, 0, 16, 0) };
		_proLowRSlider = new Slider { Minimum = -6, Maximum = 3, StepFrequency = 0.1, Value = 0 };
		_proLowLTb = new TextBlock { Text = "0.0 dB", FontSize = 12, Foreground = DspAccentSoftTextBrush };
		_proLowRTb = new TextBlock { Text = "0.0 dB", FontSize = 12, Foreground = DspAccentSoftTextBrush };
		_channelAccentTextBlocks.Add(_proLowLTb);
		_channelAccentTextBlocks.Add(_proLowRTb);

		_proLowLSlider.ValueChanged += (s, e) =>
		{
			if (ViewModel != null)
			{
				ViewModel.FurinaChannelLowLeftGainDb = e.NewValue;
				ViewModel.ApplyChannelToolSettings();
				if (_proLowLTb != null) _proLowLTb.Text = $"{e.NewValue:+0.0;-0.0;0.0} dB";
				RefreshChannelToolStatus();
			}
		};
		_proLowRSlider.ValueChanged += (s, e) =>
		{
			if (ViewModel != null)
			{
				ViewModel.FurinaChannelLowRightGainDb = e.NewValue;
				ViewModel.ApplyChannelToolSettings();
				if (_proLowRTb != null) _proLowRTb.Text = $"{e.NewValue:+0.0;-0.0;0.0} dB";
				RefreshChannelToolStatus();
			}
		};

		var lowLSp = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 6, VerticalAlignment = VerticalAlignment.Center };
		lowLSp.Children.Add(_proLowLSlider); lowLSp.Children.Add(_proLowLTb);
		var lowRSp = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 6, VerticalAlignment = VerticalAlignment.Center };
		lowRSp.Children.Add(_proLowRSlider); lowRSp.Children.Add(_proLowRTb);

		Grid.SetRow(lowLbl, 1); Grid.SetColumn(lowLbl, 0);
		Grid.SetRow(lowLSp, 1); Grid.SetColumn(lowLSp, 1);
		Grid.SetRow(lowRSp, 1); Grid.SetColumn(lowRSp, 2);
		bandGrid.Children.Add(lowLbl); bandGrid.Children.Add(lowLSp); bandGrid.Children.Add(lowRSp);

		// Mid band
		bandGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
		var midLbl = new TextBlock { Text = "中频 (250~4k Hz)", FontSize = 13, FontWeight = Microsoft.UI.Text.FontWeights.Medium, Foreground = DspTextSecondaryBrush, VerticalAlignment = VerticalAlignment.Center };
		_channelSecondaryTextBlocks.Add(midLbl);
		_proMidLSlider = new Slider { Minimum = -6, Maximum = 3, StepFrequency = 0.1, Value = 0, Margin = new Thickness(0, 0, 16, 0) };
		_proMidRSlider = new Slider { Minimum = -6, Maximum = 3, StepFrequency = 0.1, Value = 0 };
		_proMidLTb = new TextBlock { Text = "0.0 dB", FontSize = 12, Foreground = DspAccentSoftTextBrush };
		_proMidRTb = new TextBlock { Text = "0.0 dB", FontSize = 12, Foreground = DspAccentSoftTextBrush };
		_channelAccentTextBlocks.Add(_proMidLTb);
		_channelAccentTextBlocks.Add(_proMidRTb);

		_proMidLSlider.ValueChanged += (s, e) =>
		{
			if (ViewModel != null)
			{
				ViewModel.FurinaChannelMidLeftGainDb = e.NewValue;
				ViewModel.ApplyChannelToolSettings();
				if (_proMidLTb != null) _proMidLTb.Text = $"{e.NewValue:+0.0;-0.0;0.0} dB";
				RefreshChannelToolStatus();
			}
		};
		_proMidRSlider.ValueChanged += (s, e) =>
		{
			if (ViewModel != null)
			{
				ViewModel.FurinaChannelMidRightGainDb = e.NewValue;
				ViewModel.ApplyChannelToolSettings();
				if (_proMidRTb != null) _proMidRTb.Text = $"{e.NewValue:+0.0;-0.0;0.0} dB";
				RefreshChannelToolStatus();
			}
		};

		var midLSp = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 6, VerticalAlignment = VerticalAlignment.Center };
		midLSp.Children.Add(_proMidLSlider); midLSp.Children.Add(_proMidLTb);
		var midRSp = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 6, VerticalAlignment = VerticalAlignment.Center };
		midRSp.Children.Add(_proMidRSlider); midRSp.Children.Add(_proMidRTb);

		Grid.SetRow(midLbl, 2); Grid.SetColumn(midLbl, 0);
		Grid.SetRow(midLSp, 2); Grid.SetColumn(midLSp, 1);
		Grid.SetRow(midRSp, 2); Grid.SetColumn(midRSp, 2);
		bandGrid.Children.Add(midLbl); bandGrid.Children.Add(midLSp); bandGrid.Children.Add(midRSp);

		// High band
		bandGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
		var highLbl = new TextBlock { Text = "高频 (> 4k Hz)", FontSize = 13, FontWeight = Microsoft.UI.Text.FontWeights.Medium, Foreground = DspTextSecondaryBrush, VerticalAlignment = VerticalAlignment.Center };
		_channelSecondaryTextBlocks.Add(highLbl);
		_proHighLSlider = new Slider { Minimum = -6, Maximum = 3, StepFrequency = 0.1, Value = 0, Margin = new Thickness(0, 0, 16, 0) };
		_proHighRSlider = new Slider { Minimum = -6, Maximum = 3, StepFrequency = 0.1, Value = 0 };
		_proHighLTb = new TextBlock { Text = "0.0 dB", FontSize = 12, Foreground = DspAccentSoftTextBrush };
		_proHighRTb = new TextBlock { Text = "0.0 dB", FontSize = 12, Foreground = DspAccentSoftTextBrush };
		_channelAccentTextBlocks.Add(_proHighLTb);
		_channelAccentTextBlocks.Add(_proHighRTb);

		_proHighLSlider.ValueChanged += (s, e) =>
		{
			if (ViewModel != null)
			{
				ViewModel.FurinaChannelHighLeftGainDb = e.NewValue;
				ViewModel.ApplyChannelToolSettings();
				if (_proHighLTb != null) _proHighLTb.Text = $"{e.NewValue:+0.0;-0.0;0.0} dB";
				RefreshChannelToolStatus();
			}
		};
		_proHighRSlider.ValueChanged += (s, e) =>
		{
			if (ViewModel != null)
			{
				ViewModel.FurinaChannelHighRightGainDb = e.NewValue;
				ViewModel.ApplyChannelToolSettings();
				if (_proHighRTb != null) _proHighRTb.Text = $"{e.NewValue:+0.0;-0.0;0.0} dB";
				RefreshChannelToolStatus();
			}
		};

		var highLSp = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 6, VerticalAlignment = VerticalAlignment.Center };
		highLSp.Children.Add(_proHighLSlider); highLSp.Children.Add(_proHighLTb);
		var highRSp = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 6, VerticalAlignment = VerticalAlignment.Center };
		highRSp.Children.Add(_proHighRSlider); highRSp.Children.Add(_proHighRTb);

		Grid.SetRow(highLbl, 3); Grid.SetColumn(highLbl, 0);
		Grid.SetRow(highLSp, 3); Grid.SetColumn(highLSp, 1);
		Grid.SetRow(highRSp, 3); Grid.SetColumn(highRSp, 2);
		bandGrid.Children.Add(highLbl); bandGrid.Children.Add(highLSp); bandGrid.Children.Add(highRSp);

		bandSp.Children.Add(bandGrid);
		bandCard.Child = bandSp;
		Grid.SetRow(bandCard, 0);
		proGrid.Children.Add(bandCard);

		// --- 1. Advanced Precision, Mono & Phase ---
		var row1Grid = new Grid { ColumnSpacing = 20 };
		row1Grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.0, GridUnitType.Star) });
		row1Grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.0, GridUnitType.Star) });

		// Advanced Delays & Precise Gains Card
		var precCard = new Border
		{
			Background = CreateChannelSurfaceBrush(LiquidGlassVariant.Regular, isDark, Math.Clamp(0.88 / 0.90, 0.72, 1.05)),
			BorderBrush = CreateChannelRimBrush(isDark),
			BorderThickness = new Thickness(1),
			CornerRadius = new CornerRadius(12),
			Padding = new Thickness(20, 16, 20, 18)
		};
		_channelCardBorders.Add(precCard);
		var precSp = new StackPanel { Spacing = 12 };
		var precTitleTb = new TextBlock
		{
			Text = "高级声道 (精确控制)",
			FontSize = 16,
			FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
			Foreground = DspTextPrimaryBrush
		};
		_channelPrimaryTextBlocks.Add(precTitleTb);
		precSp.Children.Add(precTitleTb);

		var fGrid = new Grid { RowSpacing = 8, ColumnSpacing = 12 };
		fGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.0, GridUnitType.Star) });
		fGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.0, GridUnitType.Star) });
		fGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
		fGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

		_proLeftGainNb = new NumberBox { Header = "左声道增益 (dB)", Minimum = -12, Maximum = 6, SmallChange = 0.05, Value = 0, Foreground = DspTextPrimaryBrush };
		_proRightGainNb = new NumberBox { Header = "右声道增益 (dB)", Minimum = -12, Maximum = 6, SmallChange = 0.05, Value = 0, Foreground = DspTextPrimaryBrush };
		_proLeftDelayNb = new NumberBox { Header = "左声道延迟 (ms)", Minimum = 0, Maximum = 10, SmallChange = 0.01, Value = 0, Foreground = DspTextPrimaryBrush };
		_proRightDelayNb = new NumberBox { Header = "右声道延迟 (ms)", Minimum = 0, Maximum = 10, SmallChange = 0.01, Value = 0, Foreground = DspTextPrimaryBrush };

		_proLeftGainNb.ValueChanged += (s, e) =>
		{
			if (ViewModel != null && !double.IsNaN(e.NewValue))
			{
				ViewModel.FurinaChannelLeftGainDb = e.NewValue;
				ViewModel.ApplyChannelToolSettings();
				RefreshChannelToolStatus();
			}
		};
		_proRightGainNb.ValueChanged += (s, e) =>
		{
			if (ViewModel != null && !double.IsNaN(e.NewValue))
			{
				ViewModel.FurinaChannelRightGainDb = e.NewValue;
				ViewModel.ApplyChannelToolSettings();
				RefreshChannelToolStatus();
			}
		};
		_proLeftDelayNb.ValueChanged += (s, e) =>
		{
			if (ViewModel != null && !double.IsNaN(e.NewValue))
			{
				ViewModel.FurinaChannelLeftDelayMs = e.NewValue;
				ViewModel.ApplyChannelToolSettings();
				RefreshChannelToolStatus();
			}
		};
		_proRightDelayNb.ValueChanged += (s, e) =>
		{
			if (ViewModel != null && !double.IsNaN(e.NewValue))
			{
				ViewModel.FurinaChannelRightDelayMs = e.NewValue;
				ViewModel.ApplyChannelToolSettings();
				RefreshChannelToolStatus();
			}
		};

		Grid.SetRow(_proLeftGainNb, 0); Grid.SetColumn(_proLeftGainNb, 0);
		Grid.SetRow(_proRightGainNb, 0); Grid.SetColumn(_proRightGainNb, 1);
		Grid.SetRow(_proLeftDelayNb, 1); Grid.SetColumn(_proLeftDelayNb, 0);
		Grid.SetRow(_proRightDelayNb, 1); Grid.SetColumn(_proRightDelayNb, 1);
		fGrid.Children.Add(_proLeftGainNb); fGrid.Children.Add(_proRightGainNb);
		fGrid.Children.Add(_proLeftDelayNb); fGrid.Children.Add(_proRightDelayNb);
		precSp.Children.Add(fGrid);
		precCard.Child = precSp;
		Grid.SetColumn(precCard, 0);
		row1Grid.Children.Add(precCard);

		// Mono & Phase Routing Card
		var routeCard = new Border
		{
			Background = CreateChannelSurfaceBrush(LiquidGlassVariant.Regular, isDark, Math.Clamp(0.88 / 0.90, 0.72, 1.05)),
			BorderBrush = CreateChannelRimBrush(isDark),
			BorderThickness = new Thickness(1),
			CornerRadius = new CornerRadius(12),
			Padding = new Thickness(20, 16, 20, 18)
		};
		_channelCardBorders.Add(routeCard);
		var routeSp = new StackPanel { Spacing = 14 };

		var routeTitleTb = new TextBlock
		{
			Text = "Mono 检查与相位路由",
			FontSize = 16,
			FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
			Foreground = DspTextPrimaryBrush
		};
		_channelPrimaryTextBlocks.Add(routeTitleTb);
		routeSp.Children.Add(routeTitleTb);

		// Mono Buttons
		var monoSp = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 6 };
		_monoModeButtons.Clear();
		(string key, string name)[] monoItems =
		{
			("off", "关闭 Mono"),
			("sum", "合并 Mono"),
			("left", "只听左声道"),
			("right", "只听右声道")
		};
		foreach (var item in monoItems)
		{
			var btn = new Button
			{
				Content = item.name,
				Padding = new Thickness(10, 4, 10, 4),
				CornerRadius = new CornerRadius(8),
				BorderThickness = new Thickness(1),
				FontSize = 12,
				Tag = item.key
			};
			btn.Click += (s, e) =>
			{
				if (s is Button b && b.Tag is string mk && ViewModel != null)
				{
					ViewModel.FurinaChannelMonoMode = mk;
					ViewModel.ApplyChannelToolSettings();
					UpdateMonoButtons(IsDspDark);
				}
			};
			_monoModeButtons.Add(btn);
			monoSp.Children.Add(btn);
		}
		routeSp.Children.Add(monoSp);

		// Phase CheckBoxes
		var cbSp = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 16 };
		_phaseConstantPowerCb = new CheckBox { Content = "恒功率平衡", Foreground = DspTextSecondaryBrush };
		_phaseSwapCb = new CheckBox { Content = "左右交换", Foreground = DspTextSecondaryBrush };
		_phaseInvertLCb = new CheckBox { Content = "左声道反相", Foreground = DspTextSecondaryBrush };
		_phaseInvertRCb = new CheckBox { Content = "右声道反相", Foreground = DspTextSecondaryBrush };

		_phaseConstantPowerCb.Click += (s, e) =>
		{
			if (ViewModel != null)
			{
				ViewModel.FurinaChannelConstantPower = _phaseConstantPowerCb.IsChecked ?? false;
				ViewModel.ApplyChannelToolSettings();
				RefreshChannelToolStatus();
			}
		};
		_phaseSwapCb.Click += (s, e) =>
		{
			if (ViewModel != null)
			{
				ViewModel.FurinaChannelSwapLeftRight = _phaseSwapCb.IsChecked ?? false;
				ViewModel.ApplyChannelToolSettings();
				RefreshChannelToolStatus();
			}
		};
		_phaseInvertLCb.Click += (s, e) =>
		{
			if (ViewModel != null)
			{
				ViewModel.FurinaChannelInvertLeft = _phaseInvertLCb.IsChecked ?? false;
				ViewModel.ApplyChannelToolSettings();
				RefreshChannelToolStatus();
			}
		};
		_phaseInvertRCb.Click += (s, e) =>
		{
			if (ViewModel != null)
			{
				ViewModel.FurinaChannelInvertRight = _phaseInvertRCb.IsChecked ?? false;
				ViewModel.ApplyChannelToolSettings();
				RefreshChannelToolStatus();
			}
		};

		cbSp.Children.Add(_phaseConstantPowerCb);
		cbSp.Children.Add(_phaseSwapCb);
		cbSp.Children.Add(_phaseInvertLCb);
		cbSp.Children.Add(_phaseInvertRCb);
		routeSp.Children.Add(cbSp);

		routeCard.Child = routeSp;
		Grid.SetColumn(routeCard, 1);
		row1Grid.Children.Add(routeCard);

		Grid.SetRow(row1Grid, 1);
		proGrid.Children.Add(row1Grid);

		return proGrid;
	}

	private Border CreateFactBox(string label, TextBlock valueTb, int col, bool isDark)
	{
		var b = new Border
		{
			Background = GetChannelSoftSurfaceBrush(isDark),
			BorderBrush = CreateChannelRimBrush(isDark),
			BorderThickness = new Thickness(1),
			CornerRadius = new CornerRadius(8),
			Padding = new Thickness(12, 8, 12, 8)
		};
		_channelFactBoxBorders.Add(b);
		var sp = new StackPanel { Spacing = 2 };
		var lblTb = new TextBlock { Text = label, FontSize = 11, Foreground = DspTextMutedBrush };
		_channelMutedTextBlocks.Add(lblTb);
		sp.Children.Add(lblTb);
		sp.Children.Add(valueTb);
		b.Child = sp;
		Grid.SetColumn(b, col);
		return b;
	}

	private static Brush CreateChannelSurfaceBrush(LiquidGlassVariant variant, bool isDark, double opacityScale = 1.0)
	{
		return LiquidGlassEngine.CreateSurfaceBrush(variant, isDark: isDark, opacityScale: opacityScale);
	}

	private static Brush CreateChannelRimBrush(bool isDark, bool isHovered = false, bool isSelected = false)
	{
		return LiquidGlassEngine.CreateSpecularRimBrush(
			LiquidGlassVariant.Regular,
			isDark: isDark,
			isHovered: isHovered,
			isSelected: isSelected);
	}

	private static Brush GetChannelSoftSurfaceBrush(bool isDark)
	{
		return isDark
			? LiquidGlassEngine.CreateSurfaceBrush(LiquidGlassVariant.Clear, isDark: true, opacityScale: 0.45)
			: new SolidColorBrush(Color.FromArgb(220, 241, 245, 249));
	}

	private static Brush GetChannelInputBgBrush(bool isDark)
	{
		return new SolidColorBrush(isDark ? Color.FromArgb(140, 30, 41, 59) : Color.FromArgb(220, 255, 255, 255));
	}

	private static Brush GetChannelMeterTrackBrush(bool isDark)
	{
		return new SolidColorBrush(isDark ? Color.FromArgb(70, 255, 255, 255) : Color.FromArgb(255, 226, 232, 240));
	}

	private static void ApplyChannelButtonSurface(Button? btn, bool isDark, Brush foregroundBrush)
	{
		if (btn == null) return;
		btn.Background = CreateChannelSurfaceBrush(LiquidGlassVariant.Clear, isDark, 0.92);
		btn.BorderBrush = CreateChannelRimBrush(isDark);
		btn.BorderThickness = new Thickness(1);
		btn.Foreground = foregroundBrush;
	}

	private void ApplyChannelPowerButtonTheme(bool isDark, bool enabled)
	{
		if (_channelPowerBtn == null) return;

		if (enabled)
		{
			var greenBrush = new SolidColorBrush(isDark ? Color.FromArgb(255, 74, 222, 128) : Color.FromArgb(255, 22, 163, 74));
			_channelPowerBtn.Background = new SolidColorBrush(isDark ? Color.FromArgb(55, 34, 197, 94) : Color.FromArgb(255, 240, 253, 244));
			_channelPowerBtn.BorderBrush = new SolidColorBrush(isDark ? Color.FromArgb(145, 74, 222, 128) : Color.FromArgb(255, 134, 239, 172));
			_channelPowerBtn.Foreground = greenBrush;
			if (_channelPowerText != null) _channelPowerText.Foreground = greenBrush;
			if (_channelPowerIcon != null) _channelPowerIcon.Foreground = greenBrush;
		}
		else
		{
			var subHeaderBrush = new SolidColorBrush(isDark ? Color.FromArgb(255, 226, 232, 240) : Color.FromArgb(255, 71, 85, 105));
			var mutedBrush = new SolidColorBrush(isDark ? Color.FromArgb(255, 203, 213, 225) : Color.FromArgb(255, 100, 116, 139));
			ApplyChannelButtonSurface(_channelPowerBtn, isDark, subHeaderBrush);
			if (_channelPowerText != null) _channelPowerText.Foreground = subHeaderBrush;
			if (_channelPowerIcon != null) _channelPowerIcon.Foreground = mutedBrush;
		}
	}

	private void ApplyChannelCompareButtonTheme(bool isDark, bool compareBypassed)
	{
		if (_channelCompareBtn == null) return;

		if (compareBypassed)
		{
			_channelCompareBtn.Background = new SolidColorBrush(isDark ? Color.FromArgb(75, 245, 158, 11) : Color.FromArgb(255, 254, 243, 199));
			_channelCompareBtn.BorderBrush = new SolidColorBrush(isDark ? Color.FromArgb(190, 251, 191, 36) : Color.FromArgb(255, 245, 158, 11));
			_channelCompareBtn.Foreground = new SolidColorBrush(isDark ? Color.FromArgb(255, 253, 230, 138) : Color.FromArgb(255, 146, 64, 14));
		}
		else
		{
			var subHeaderBrush = new SolidColorBrush(isDark ? Color.FromArgb(255, 226, 232, 240) : Color.FromArgb(255, 71, 85, 105));
			ApplyChannelButtonSurface(_channelCompareBtn, isDark, subHeaderBrush);
		}
	}

	public void RefreshChannelToolTheme(bool isDark)
	{
		if (_channelToolWorkbench == null) return;

		_channelToolWorkbench.RequestedTheme = isDark ? ElementTheme.Dark : ElementTheme.Light;

		Brush primaryText = new SolidColorBrush(isDark ? Colors.White : Color.FromArgb(255, 15, 23, 42));
		Brush secondaryText = new SolidColorBrush(isDark ? Color.FromArgb(255, 248, 250, 252) : Color.FromArgb(255, 51, 65, 85));
		Brush subHeaderText = new SolidColorBrush(isDark ? Color.FromArgb(255, 226, 232, 240) : Color.FromArgb(255, 71, 85, 105));
		Brush mutedText = new SolidColorBrush(isDark ? Color.FromArgb(255, 203, 213, 225) : Color.FromArgb(255, 100, 116, 139));
		Brush accentSoftBg = new SolidColorBrush(isDark ? Color.FromArgb(85, 99, 102, 241) : Color.FromArgb(255, 238, 242, 255));
		Brush accentSoftText = new SolidColorBrush(isDark ? Color.FromArgb(255, 199, 210, 254) : Color.FromArgb(255, 79, 70, 229));

		Brush cardBg = CreateChannelSurfaceBrush(LiquidGlassVariant.Regular, isDark, Math.Clamp(0.88 / 0.90, 0.72, 1.05));
		Brush rimBrush = CreateChannelRimBrush(isDark);
		Brush softBg = GetChannelSoftSurfaceBrush(isDark);

		if (_channelRootGrid != null)
		{
			_channelRootGrid.Background = CreateChannelSurfaceBrush(LiquidGlassVariant.Clear, isDark, Math.Clamp(0.72 / 0.85, 0.55, 1.0));
		}
		if (_channelTitleIconBorder != null) _channelTitleIconBorder.Background = accentSoftBg;
		if (_channelTitleIcon != null) _channelTitleIcon.Foreground = accentSoftText;

		foreach (var card in _channelCardBorders)
		{
			card.Background = cardBg;
			card.BorderBrush = rimBrush;
		}
		foreach (var box in _channelFactBoxBorders)
		{
			box.Background = softBg;
			box.BorderBrush = rimBrush;
		}

		foreach (var tb in _channelPrimaryTextBlocks) tb.Foreground = primaryText;
		foreach (var tb in _channelSecondaryTextBlocks) tb.Foreground = secondaryText;
		foreach (var tb in _channelMutedTextBlocks) tb.Foreground = mutedText;
		foreach (var tb in _channelAccentTextBlocks) tb.Foreground = accentSoftText;

		if (_leftMeterTrack != null) _leftMeterTrack.Background = GetChannelMeterTrackBrush(isDark);
		if (_rightMeterTrack != null) _rightMeterTrack.Background = GetChannelMeterTrackBrush(isDark);

		if (_channelModeSegBorder != null)
		{
			_channelModeSegBorder.Background = softBg;
			_channelModeSegBorder.BorderBrush = rimBrush;
		}

		ApplyChannelButtonSurface(_channelResetBtn, isDark, subHeaderText);
		if (_channelResetIcon != null) _channelResetIcon.Foreground = mutedText;
		if (_channelResetText != null) _channelResetText.Foreground = subHeaderText;

		ApplyChannelButtonSurface(_leftTooLoudBtn, isDark, primaryText);
		ApplyChannelButtonSurface(_rightTooLoudBtn, isDark, primaryText);

		if (_channelClearAuxBtn != null) _channelClearAuxBtn.Foreground = accentSoftText;
		if (_channelSwapAuxBtn != null) _channelSwapAuxBtn.Foreground = accentSoftText;

		if (_presetCombo != null)
		{
			_presetCombo.Background = CreateChannelSurfaceBrush(LiquidGlassVariant.Clear, isDark, 0.92);
			_presetCombo.BorderBrush = rimBrush;
			_presetCombo.Foreground = primaryText;
		}
		ApplyChannelButtonSurface(_presetApplyBtn, isDark, primaryText);
		ApplyChannelButtonSurface(_presetRenameBtn, isDark, primaryText);
		ApplyChannelButtonSurface(_presetDeleteBtn, isDark, primaryText);
		if (_presetDraftTb != null)
		{
			_presetDraftTb.Background = GetChannelInputBgBrush(isDark);
			_presetDraftTb.BorderBrush = rimBrush;
			_presetDraftTb.Foreground = primaryText;
		}

		if (_channelDiagExpander != null)
		{
			_channelDiagExpander.Background = cardBg;
			_channelDiagExpander.BorderBrush = rimBrush;
			_channelDiagExpander.Foreground = primaryText;
		}
		if (_channelDiagIcon != null) _channelDiagIcon.Foreground = accentSoftText;

		if (_proLeftGainNb != null) _proLeftGainNb.Foreground = primaryText;
		if (_proRightGainNb != null) _proRightGainNb.Foreground = primaryText;
		if (_proLeftDelayNb != null) _proLeftDelayNb.Foreground = primaryText;
		if (_proRightDelayNb != null) _proRightDelayNb.Foreground = primaryText;

		if (_phaseConstantPowerCb != null) _phaseConstantPowerCb.Foreground = secondaryText;
		if (_phaseSwapCb != null) _phaseSwapCb.Foreground = secondaryText;
		if (_phaseInvertLCb != null) _phaseInvertLCb.Foreground = secondaryText;
		if (_phaseInvertRCb != null) _phaseInvertRCb.Foreground = secondaryText;

		SetChannelMode(ViewModel?.FurinaChannelProMode ?? "simple", isDark, persist: false);
		UpdateStepPills(isDark);
		UpdateMonoButtons(isDark);
		RefreshChannelToolStatus(isDark);
	}

	private void SetChannelMode(string mode, bool? isDarkOverride = null, bool persist = true)
	{
		bool isPro = string.Equals(mode, "pro", StringComparison.OrdinalIgnoreCase);
		bool isDark = isDarkOverride ?? IsDspDark;
		var activeBg = isDark ? Color.FromArgb(95, 99, 102, 241) : Colors.White;
		var activeFg = isDark ? Color.FromArgb(255, 199, 210, 254) : Color.FromArgb(255, 79, 70, 229);
		var mutedBrush = new SolidColorBrush(isDark ? Color.FromArgb(255, 203, 213, 225) : Color.FromArgb(255, 100, 116, 139));
		if (_channelModeSimpleBtn != null)
		{
			_channelModeSimpleBtn.Background = new SolidColorBrush(isPro ? Colors.Transparent : activeBg);
			_channelModeSimpleBtn.Foreground = isPro ? mutedBrush : new SolidColorBrush(activeFg);
			_channelModeSimpleBtn.FontWeight = isPro ? Microsoft.UI.Text.FontWeights.Normal : Microsoft.UI.Text.FontWeights.SemiBold;
		}
		if (_channelModeProBtn != null)
		{
			_channelModeProBtn.Background = new SolidColorBrush(isPro ? activeBg : Colors.Transparent);
			_channelModeProBtn.Foreground = isPro ? new SolidColorBrush(activeFg) : mutedBrush;
			_channelModeProBtn.FontWeight = isPro ? Microsoft.UI.Text.FontWeights.SemiBold : Microsoft.UI.Text.FontWeights.Normal;
		}
		if (_proContainer != null)
		{
			_proContainer.Visibility = isPro ? Visibility.Visible : Visibility.Collapsed;
		}
		if (persist && ViewModel != null)
		{
			ViewModel.FurinaChannelProMode = mode;
			ViewModel.SaveEchoSettings();
		}
	}

	private void UpdateStepPills(bool? isDarkOverride = null)
	{
		bool isDark = isDarkOverride ?? IsDspDark;
		var mutedBrush = new SolidColorBrush(isDark ? Color.FromArgb(255, 203, 213, 225) : Color.FromArgb(255, 100, 116, 139));
		foreach (var btn in _trimStepButtons)
		{
			if (btn.Tag is double val)
			{
				bool active = Math.Abs(val - _currentTrimStep) < 0.01;
				btn.Background = new SolidColorBrush(active
					? (isDark ? Color.FromArgb(80, 99, 102, 241) : Color.FromArgb(255, 238, 242, 255))
					: Colors.Transparent);
				btn.BorderBrush = new SolidColorBrush(active
					? (isDark ? Color.FromArgb(180, 129, 140, 248) : Color.FromArgb(255, 199, 210, 254))
					: (isDark ? Color.FromArgb(70, 255, 255, 255) : Color.FromArgb(100, 226, 232, 240)));
				btn.Foreground = active
					? new SolidColorBrush(isDark ? Color.FromArgb(255, 199, 210, 254) : Color.FromArgb(255, 79, 70, 229))
					: mutedBrush;
				btn.FontWeight = active ? Microsoft.UI.Text.FontWeights.SemiBold : Microsoft.UI.Text.FontWeights.Normal;
			}
		}
	}

	private void UpdateMonoButtons(bool? isDarkOverride = null)
	{
		string activeKey = ViewModel?.FurinaChannelMonoMode ?? "off";
		bool isDark = isDarkOverride ?? IsDspDark;
		var subHeaderBrush = new SolidColorBrush(isDark ? Color.FromArgb(255, 226, 232, 240) : Color.FromArgb(255, 71, 85, 105));
		foreach (var btn in _monoModeButtons)
		{
			if (btn.Tag is string k)
			{
				bool active = string.Equals(k, activeKey, StringComparison.OrdinalIgnoreCase);
				btn.Background = active
					? new SolidColorBrush(isDark ? Color.FromArgb(80, 99, 102, 241) : Color.FromArgb(255, 238, 242, 255))
					: CreateChannelSurfaceBrush(LiquidGlassVariant.Clear, isDark, isDark ? 0.85 : 0.92);
				btn.BorderBrush = new SolidColorBrush(active
					? (isDark ? Color.FromArgb(180, 129, 140, 248) : Color.FromArgb(255, 199, 210, 254))
					: (isDark ? Color.FromArgb(70, 255, 255, 255) : Color.FromArgb(120, 226, 232, 240)));
				btn.Foreground = active
					? new SolidColorBrush(isDark ? Color.FromArgb(255, 199, 210, 254) : Color.FromArgb(255, 79, 70, 229))
					: subHeaderBrush;
				btn.FontWeight = active ? Microsoft.UI.Text.FontWeights.SemiBold : Microsoft.UI.Text.FontWeights.Normal;
			}
		}
	}

	private void RefreshPresetsUI()
	{
		if (ViewModel == null) return;
		var presets = ViewModel.FurinaChannelPresets;
		bool hasPresets = presets.Count > 0;

		if (_presetEmptyTb != null) _presetEmptyTb.Visibility = hasPresets ? Visibility.Collapsed : Visibility.Visible;
		if (_presetCombo != null)
		{
			_presetCombo.Visibility = hasPresets ? Visibility.Visible : Visibility.Collapsed;
			_presetCombo.ItemsSource = presets.Select(p => p.Name).ToList();
			if (hasPresets && _presetCombo.SelectedIndex < 0) _presetCombo.SelectedIndex = 0;
		}
		if (_presetApplyBtn != null) _presetApplyBtn.Visibility = hasPresets ? Visibility.Visible : Visibility.Collapsed;
		if (_presetRenameBtn != null) _presetRenameBtn.Visibility = hasPresets ? Visibility.Visible : Visibility.Collapsed;
		if (_presetDeleteBtn != null) _presetDeleteBtn.Visibility = hasPresets ? Visibility.Visible : Visibility.Collapsed;
	}

	public void RefreshChannelToolStatus() => RefreshChannelToolStatus(IsDspDark);

	private void RefreshChannelToolStatus(bool isDark)
	{
		bool enabled = ViewModel?.FurinaChannelToolEnabled ?? false;
		bool compareBypassed = ViewModel?.FurinaChannelCompareBypassed ?? false;

		if (_channelPowerText != null)
		{
			_channelPowerText.Text = enabled ? "已开启" : "已关闭";
		}
		ApplyChannelPowerButtonTheme(isDark, enabled);

		if (_channelCompareBtn != null)
		{
			_channelCompareBtn.Content = compareBypassed ? "已旁路 (对比中)" : "A/B 对比";
		}
		ApplyChannelCompareButtonTheme(isDark, compareBypassed);

		if (ViewModel == null) return;

		double bal = ViewModel.FurinaChannelBalance;
		bool constPower = ViewModel.FurinaChannelConstantPower;

		// 1. Calculate Balance and Bias Gains
		double leftBalDb = 0.0;
		double rightBalDb = 0.0;
		if (!constPower)
		{
			double linL = bal > 0 ? 1.0 - bal : 1.0;
			double linR = bal < 0 ? 1.0 + bal : 1.0;
			leftBalDb = linL <= 1e-4 ? -60.0 : 20.0 * Math.Log10(Math.Max(1e-4, linL));
			rightBalDb = linR <= 1e-4 ? -60.0 : 20.0 * Math.Log10(Math.Max(1e-4, linR));
		}
		else
		{
			double angle = (bal + 1.0) * Math.PI * 0.25;
			double sqrt2 = Math.Sqrt(2.0);
			double linL = Math.Min(1.0, Math.Cos(angle) * sqrt2);
			double linR = Math.Min(1.0, Math.Sin(angle) * sqrt2);
			leftBalDb = linL <= 1e-4 ? -60.0 : 20.0 * Math.Log10(Math.Max(1e-4, linL));
			rightBalDb = linR <= 1e-4 ? -60.0 : 20.0 * Math.Log10(Math.Max(1e-4, linR));
		}

		double totalLeftDb = Math.Round((ViewModel.FurinaChannelLeftGainDb + leftBalDb) * 100.0) / 100.0;
		double totalRightDb = Math.Round((ViewModel.FurinaChannelRightGainDb + rightBalDb) * 100.0) / 100.0;
		double skew = Math.Round((totalRightDb - totalLeftDb) * 100.0) / 100.0;

		// 3. Level Readouts
		if (_leftOutputBigTb != null)
		{
			_leftOutputBigTb.Text = totalLeftDb == 0.0 ? "0 dB" : $"{totalLeftDb:+0.00;-0.00;0.00} dB";
		}
		if (_rightOutputBigTb != null)
		{
			_rightOutputBigTb.Text = totalRightDb == 0.0 ? "0 dB" : $"{totalRightDb:+0.00;-0.00;0.00} dB";
		}

		string statusText;
		if (Math.Abs(skew) < 0.05)
		{
			statusText = "中心稳定";
		}
		else if (skew > 0)
		{
			statusText = $"偏右 +{skew:F2} dB";
		}
		else
		{
			statusText = $"偏左 {Math.Abs(skew):F2} dB";
		}

		if (_centerStatusBigTb != null) _centerStatusBigTb.Text = statusText;
		if (_centerStatusSubTb != null)
		{
			_centerStatusSubTb.Text = compareBypassed ? "已旁路 (对比中)" : (enabled ? "已开启" : "已关闭");
			_centerStatusSubTb.Foreground = enabled && !compareBypassed
				? new SolidColorBrush(isDark ? Color.FromArgb(255, 74, 222, 128) : Color.FromArgb(255, 22, 163, 74))
				: new SolidColorBrush(isDark ? Color.FromArgb(255, 203, 213, 225) : Color.FromArgb(255, 100, 116, 139));
		}

		// Meter bars (symmetric fill)
		double meterLeftPct = Math.Clamp(50.0 - skew * 8.0, 8.0, 92.0);
		double meterRightPct = 100.0 - meterLeftPct;
		if (_leftMeterFill != null) _leftMeterFill.Width = Math.Max(10, meterLeftPct * 2.8);
		if (_rightMeterFill != null) _rightMeterFill.Width = Math.Max(10, meterRightPct * 2.8);

		// 4. Pan Balance
		if (_balanceSlider != null && Math.Abs(_balanceSlider.Value - bal * 100.0) > 0.1)
		{
			_balanceSlider.Value = bal * 100.0;
		}
		if (_balanceValTb != null)
		{
			int pct = (int)Math.Round(Math.Abs(bal) * 100.0);
			_balanceValTb.Text = pct == 0 ? "0%" : (bal > 0 ? $"R {pct}%" : $"L {pct}%");
		}

		// 5. Diagnostics
		if (_factLeftTb != null) _factLeftTb.Text = $"{totalLeftDb:+0.00;-0.00;0.00} dB";
		if (_factRightTb != null) _factRightTb.Text = $"{totalRightDb:+0.00;-0.00;0.00} dB";
		double delaySkew = ViewModel.FurinaChannelRightDelayMs - ViewModel.FurinaChannelLeftDelayMs;
		if (_factDelaySkewTb != null) _factDelaySkewTb.Text = $"{delaySkew:+0.00;-0.00;0.00} ms";
		if (_factStatusDescTb != null)
		{
			_factStatusDescTb.Text = Math.Abs(skew) < 0.05
				? "左右电平平衡，声像居中"
				: $"检测到 {statusText}，已自动补偿";
		}

		// 6. Pro Controls sync
		if (_proLeftGainNb != null && Math.Abs(_proLeftGainNb.Value - ViewModel.FurinaChannelLeftGainDb) > 0.01)
			_proLeftGainNb.Value = ViewModel.FurinaChannelLeftGainDb;
		if (_proRightGainNb != null && Math.Abs(_proRightGainNb.Value - ViewModel.FurinaChannelRightGainDb) > 0.01)
			_proRightGainNb.Value = ViewModel.FurinaChannelRightGainDb;
		if (_proLeftDelayNb != null && Math.Abs(_proLeftDelayNb.Value - ViewModel.FurinaChannelLeftDelayMs) > 0.001)
			_proLeftDelayNb.Value = ViewModel.FurinaChannelLeftDelayMs;
		if (_proRightDelayNb != null && Math.Abs(_proRightDelayNb.Value - ViewModel.FurinaChannelRightDelayMs) > 0.001)
			_proRightDelayNb.Value = ViewModel.FurinaChannelRightDelayMs;

		if (_proLowLSlider != null) _proLowLSlider.Value = ViewModel.FurinaChannelLowLeftGainDb;
		if (_proLowRSlider != null) _proLowRSlider.Value = ViewModel.FurinaChannelLowRightGainDb;
		if (_proMidLSlider != null) _proMidLSlider.Value = ViewModel.FurinaChannelMidLeftGainDb;
		if (_proMidRSlider != null) _proMidRSlider.Value = ViewModel.FurinaChannelMidRightGainDb;
		if (_proHighLSlider != null) _proHighLSlider.Value = ViewModel.FurinaChannelHighLeftGainDb;
		if (_proHighRSlider != null) _proHighRSlider.Value = ViewModel.FurinaChannelHighRightGainDb;

		if (_phaseConstantPowerCb != null) _phaseConstantPowerCb.IsChecked = constPower;
		if (_phaseSwapCb != null) _phaseSwapCb.IsChecked = ViewModel.FurinaChannelSwapLeftRight;
		if (_phaseInvertLCb != null) _phaseInvertLCb.IsChecked = ViewModel.FurinaChannelInvertLeft;
		if (_phaseInvertRCb != null) _phaseInvertRCb.IsChecked = ViewModel.FurinaChannelInvertRight;
		UpdateMonoButtons(isDark);

		// 7. Sidebar text update
		if (_channelNavSubTb != null)
		{
			_channelNavSubTb.Text = !enabled ? "立体声直通" : statusText;
		}
	}
}
