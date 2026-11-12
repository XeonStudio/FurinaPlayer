using System;
using System.Collections.Generic;
using FurinaPlayer.UI.Controls;
using Microsoft.UI;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using SonicWave.Audio.Dsp;
using Windows.Foundation;
using Windows.UI;

namespace FurinaPlayer.UI.Pages;

/// <summary>
/// FurinaPlayer 立体声母带动态压缩器 (Compressor) 二级工作台分部实现。
/// 1:1 像素级精准还原原生 Dynamic Transfer Curve、8秒 GR 历史折线图、多轨峰值/RMS电平表及完整参数草稿控制流。
/// </summary>
public sealed partial class DspPage
{
	private Border? _compressorWorkbench;

	// Draft Parameter State
	private bool _compDraftEnabled = false;
	private double _compDraftThresholdDb = -18.0;
	private double _compDraftRatio = 4.0;
	private double _compDraftAttackMs = 10.0;
	private double _compDraftReleaseMs = 120.0;
	private double _compDraftKneeDb = 6.0;
	private double _compDraftMakeupDb = 0.0;
	private double _compDraftMix = 1.0;
	private string _compDraftDetectorMode = "Peak";
	private bool _compDraftSidechainHp = false;
	private double _compDraftSidechainHpHz = 80.0;
	private bool _compDraftAutoRelease = false;
	private double _compDraftRangeDb = 60.0;
	private double _compDraftStereoLink = 1.0;

	// UI Controls
	private Border? _compTitleIconBorder;
	private FontIcon? _compTitleIcon;
	private Button? _compPowerBtn;
	private FontIcon? _compPowerIcon;
	private TextBlock? _compPowerText;
	private Button? _compResetBtn;
	private FontIcon? _compResetIcon;
	private TextBlock? _compResetText;
	private Button? _compApplyBtn;
	private FontIcon? _compApplyIcon;
	private TextBlock? _compApplyText;
	private Border? _compPendingBadge;
	private TextBlock? _compPendingBadgeTb;
	private TextBlock? _compApplyHintTb;
	private bool _compIsDirty;

	// Mode Pill Controls
	private Border? _compModeBorder;
	private Button? _btnModeBasic;
	private Button? _btnModeAdvanced;
	private FrameworkElement? _basicControlsContainer;
	private FrameworkElement? _advancedControlsContainer;
	private string _compCurrentMode = "basic";

	// Theme-tracked collections
	private readonly List<Border> _compCardBorders = new();
	private readonly List<Grid> _compMeterTracks = new();
	private readonly List<TextBlock> _compPrimaryTextBlocks = new();
	private readonly List<TextBlock> _compSecondaryTextBlocks = new();
	private readonly List<TextBlock> _compMutedTextBlocks = new();

	// Top Meters Controls
	private TextBlock? _compInputHeadlineTb;
	private Border? _compInputRmsBarL;
	private Border? _compInputPeakTickL;
	private TextBlock? _compInputDbTbL;
	private Border? _compInputRmsBarR;
	private Border? _compInputPeakTickR;
	private TextBlock? _compInputDbTbR;

	private TextBlock? _compGrHeadlineTb;
	private Border? _compGrReductionBar;
	private TextBlock? _compHeadroomTb;

	private TextBlock? _compOutputHeadlineTb;
	private Border? _compOutputRmsBarL;
	private Border? _compOutputPeakTickL;
	private TextBlock? _compOutputDbTbL;
	private Border? _compOutputRmsBarR;
	private Border? _compOutputPeakTickR;
	private TextBlock? _compOutputDbTbR;

	// Transfer Curve Controls
	private Border? _compCurveRatioPill;
	private TextBlock? _compCurveRatioTb;
	private Canvas? _compCurveCanvas;
	private Border? _compThresholdTag;
	private TextBlock? _compThresholdTagTb;
	private Line? _compThresholdLineTop;
	private Line? _compThresholdLineBottom;
	private Ellipse? _compThresholdHandle;
	private Ellipse? _compLivePointDot;
	private Path? _compCurveStrokePath;
	private Path? _compCurveAreaPath;
	private bool _isDraggingThreshold = false;

	// Basic Sliders & Labels
	private Border? _compDetBorder;
	private Button? _btnDetPeak;
	private Button? _btnDetRms;
	private Slider? _sliderThreshold;
	private TextBlock? _tbThresholdVal;
	private Slider? _sliderRatio;
	private TextBlock? _tbRatioVal;
	private Slider? _sliderAttack;
	private TextBlock? _tbAttackVal;
	private Slider? _sliderRelease;
	private TextBlock? _tbReleaseVal;
	private Slider? _sliderMakeup;
	private TextBlock? _tbMakeupVal;
	private Slider? _sliderMix;
	private TextBlock? _tbMixVal;

	// Advanced Controls
	private ToggleSwitch? _toggleSidechainHp;
	private Slider? _sliderSidechainHpHz;
	private TextBlock? _tbSidechainHpHzVal;
	private ToggleSwitch? _toggleAutoRelease;
	private Slider? _sliderKnee;
	private TextBlock? _tbKneeVal;
	private Slider? _sliderRange;
	private TextBlock? _tbRangeVal;
	private Slider? _sliderStereoLink;
	private TextBlock? _tbStereoLinkVal;

	// GR History Plot
	private Canvas? _compHistoryCanvas;
	private Path? _compHistoryLinePath;
	private Path? _compHistoryAreaPath;
	private readonly float[] _compGrHistory = new float[80];
	private int _compGrHistoryIndex = 0;

	// Constants for 2D Coordinate Curves
	private const double CurvePlotSize = 320.0;
	private const double CurvePlotMargin = 14.0;
	private const double CurvePlotSpan = 292.0; // 320 - 2 * 14
	private const double HistoryWidth = 640.0;
	private const double HistoryHeight = 96.0;

	public FrameworkElement CreateCompressorWorkbench()
	{
		InitCompressorDraftState();

		_compCardBorders.Clear();
		_compMeterTracks.Clear();
		_compPrimaryTextBlocks.Clear();
		_compSecondaryTextBlocks.Clear();
		_compMutedTextBlocks.Clear();

		bool isDark = IsDspDark;

		var rootBorder = new Border
		{
			RequestedTheme = isDark ? ElementTheme.Dark : ElementTheme.Light,
			Background = CreateCompSurfaceBrush(LiquidGlassVariant.Clear, isDark, Math.Clamp(0.72 / 0.85, 0.55, 1.0)),
			CornerRadius = new CornerRadius(0)
		};

		var sv = new ScrollViewer
		{
			VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
			HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
			Padding = new Thickness(28, 20, 28, 24)
		};

		var mainStack = new StackPanel
		{
			Spacing = 16,
			HorizontalAlignment = HorizontalAlignment.Stretch,
			MaxWidth = 1380
		};

		// 1. Top Header Bar (Title, Icon, Module pills, Refresh button)
		mainStack.Children.Add(CreateCompTopHeaderBar());

		// 2. Action Toolbar & Mode Switch
		mainStack.Children.Add(CreateCompActionToolbar());

		// 3. Dynamic Level Display Bar (Input, Gain Reduction, Output Meters)
		mainStack.Children.Add(CreateCompHeaderMetersSection());

		// 4. Subtitle Lead Hint
		var leadHintTb = new TextBlock
		{
			Text = "左右拖动曲线调整阈值。压缩比、启动、释放和补偿在滑杆上。",
			FontSize = 13,
			Foreground = GetCompMutedTextBrush(isDark),
			Margin = new Thickness(2, -4, 0, 0)
		};
		_compMutedTextBlocks.Add(leadHintTb);
		mainStack.Children.Add(leadHintTb);

		// 5. Main 2-Column Stage (Left: Transfer Curve, Right: Basic / Advanced Controls)
		mainStack.Children.Add(CreateCompMainStage());

		// 6. Bottom Section: GR History Plot & Apply Hint
		mainStack.Children.Add(CreateCompHistorySection());

		sv.Content = mainStack;
		rootBorder.Child = sv;
		_compressorWorkbench = rootBorder;

		// Initial render of curves and theme state
		UpdateTransferCurveVisuals();
		UpdateHistoryPlotVisuals();
		UpdateDirtyStateBadge(isDark);
		RefreshCompressorTheme(isDark);

		return _compressorWorkbench;
	}

	private void InitCompressorDraftState()
	{
		if (ViewModel != null)
		{
			_compDraftEnabled = ViewModel.EchoCompressorEnabled;
			_compDraftThresholdDb = ViewModel.EchoCompressorThresholdDb;
			_compDraftRatio = ViewModel.EchoCompressorRatio;
			_compDraftAttackMs = ViewModel.EchoCompressorAttackMs;
			_compDraftReleaseMs = ViewModel.EchoCompressorReleaseMs;
			_compDraftKneeDb = ViewModel.EchoCompressorKneeDb;
			_compDraftMakeupDb = ViewModel.EchoCompressorMakeupDb;
			_compDraftMix = ViewModel.EchoCompressorMix;
			_compDraftDetectorMode = ViewModel.EchoCompressorDetectorMode ?? "Peak";
			_compDraftSidechainHp = ViewModel.EchoCompressorSidechainHighpassEnabled;
			_compDraftSidechainHpHz = ViewModel.EchoCompressorSidechainHighpassHz;
			_compDraftAutoRelease = ViewModel.EchoCompressorAutoRelease;
			_compDraftRangeDb = ViewModel.EchoCompressorRangeDb;
			_compDraftStereoLink = ViewModel.EchoCompressorStereoLink;
		}
	}

	#region 1. Top Header Bar
	private FrameworkElement CreateCompTopHeaderBar()
	{
		bool isDark = IsDspDark;
		var headerGrid = new Grid();
		headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.0, GridUnitType.Star) });
		headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

		var titleSp = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 12, VerticalAlignment = VerticalAlignment.Center };
		_compTitleIcon = new FontIcon
		{
			Glyph = "\uE958",
			FontSize = 18,
			Foreground = GetCompAccentSoftTextBrush(isDark),
			HorizontalAlignment = HorizontalAlignment.Center,
			VerticalAlignment = VerticalAlignment.Center
		};
		_compTitleIconBorder = new Border
		{
			Width = 36,
			Height = 36,
			CornerRadius = new CornerRadius(18),
			Background = GetCompAccentSoftBgBrush(isDark),
			Child = _compTitleIcon
		};
		titleSp.Children.Add(_compTitleIconBorder);
		var titleTb = new TextBlock
		{
			Text = "压缩器",
			FontSize = 22,
			FontWeight = Microsoft.UI.Text.FontWeights.Bold,
			Foreground = GetCompPrimaryTextBrush(isDark),
			VerticalAlignment = VerticalAlignment.Center
		};
		_compPrimaryTextBlocks.Add(titleTb);
		titleSp.Children.Add(titleTb);
		Grid.SetColumn(titleSp, 0);
		headerGrid.Children.Add(titleSp);

		return headerGrid;
	}
	#endregion

	#region 2. Top Action Toolbar
	private FrameworkElement CreateCompActionToolbar()
	{
		bool isDark = IsDspDark;
		var barGrid = new Grid();
		barGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.0, GridUnitType.Star) });
		barGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

		// Left Action Buttons: [Power] [Reset] [Apply] [Pending Badge]
		var leftSp = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 10, VerticalAlignment = VerticalAlignment.Center };

		_compPowerIcon = new FontIcon { Glyph = "\uE7E8", FontSize = 14 };
		_compPowerText = new TextBlock { Text = _compDraftEnabled ? "已启用" : "已旁路", FontSize = 13, FontWeight = Microsoft.UI.Text.FontWeights.Medium };
		var powerSp = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 6, Children = { _compPowerIcon, _compPowerText } };

		_compPowerBtn = new Button
		{
			Content = powerSp,
			Padding = new Thickness(12, 6, 14, 6),
			CornerRadius = new CornerRadius(8),
			BorderThickness = new Thickness(1)
		};
		_compPowerBtn.Click += (s, e) =>
		{
			_compDraftEnabled = !_compDraftEnabled;
			if (ViewModel != null)
			{
				ViewModel.EchoCompressorEnabled = _compDraftEnabled;
				ViewModel.ApplyEchoDsp();
			}
			UpdatePowerButtonVisuals();
		};
		UpdatePowerButtonVisuals(isDark);
		leftSp.Children.Add(_compPowerBtn);

		// 恢复默认 Button
		_compResetIcon = new FontIcon { Glyph = "\uE7A7", FontSize = 13 };
		_compResetText = new TextBlock { Text = "恢复默认", FontSize = 13 };
		_compResetBtn = new Button
		{
			Content = new StackPanel
			{
				Orientation = Orientation.Horizontal,
				Spacing = 6,
				Children = { _compResetIcon, _compResetText }
			},
			Padding = new Thickness(12, 6, 14, 6),
			CornerRadius = new CornerRadius(8),
			BorderThickness = new Thickness(1)
		};
		ApplyCompNeutralButtonSurface(_compResetBtn, isDark, GetCompSecondaryTextBrush(isDark), _compResetIcon, _compResetText, GetCompSubHeaderTextBrush(isDark));
		_compResetBtn.Click += (s, e) => ResetCompressorDefaults();
		leftSp.Children.Add(_compResetBtn);

		// 应用参数 Button
		_compApplyIcon = new FontIcon { Glyph = "\uE74E", FontSize = 13 };
		_compApplyText = new TextBlock { Text = "应用参数", FontSize = 13, FontWeight = Microsoft.UI.Text.FontWeights.SemiBold };
		_compApplyBtn = new Button
		{
			Content = new StackPanel
			{
				Orientation = Orientation.Horizontal,
				Spacing = 6,
				Children = { _compApplyIcon, _compApplyText }
			},
			Padding = new Thickness(12, 6, 14, 6),
			CornerRadius = new CornerRadius(8),
			BorderThickness = new Thickness(1)
		};
		ApplyCompNeutralButtonSurface(_compApplyBtn, isDark, GetCompSecondaryTextBrush(isDark), _compApplyIcon, _compApplyText, GetCompSubHeaderTextBrush(isDark));
		_compApplyBtn.Click += (s, e) => ApplyCompressorDraftParameters();
		leftSp.Children.Add(_compApplyBtn);

		// Pending Badge
		_compPendingBadgeTb = new TextBlock
		{
			Text = "未应用 · 1",
			FontSize = 11,
			FontWeight = Microsoft.UI.Text.FontWeights.SemiBold
		};
		_compPendingBadge = new Border
		{
			BorderThickness = new Thickness(1),
			CornerRadius = new CornerRadius(12),
			Padding = new Thickness(8, 3, 8, 3),
			Child = _compPendingBadgeTb,
			Visibility = Visibility.Collapsed
		};
		leftSp.Children.Add(_compPendingBadge);

		Grid.SetColumn(leftSp, 0);
		barGrid.Children.Add(leftSp);

		// Right Mode Pills: [基础控制] [高级检测与联动]
		_compModeBorder = new Border
		{
			Background = GetCompSoftSurfaceBrush(isDark),
			BorderBrush = CreateCompRimBrush(isDark),
			BorderThickness = new Thickness(1),
			CornerRadius = new CornerRadius(16),
			Padding = new Thickness(3)
		};
		var modeSp = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 2 };

		_btnModeBasic = new Button
		{
			Content = "基础控制",
			Padding = new Thickness(14, 5, 14, 5),
			CornerRadius = new CornerRadius(14),
			FontSize = 12.5,
			FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
			BorderThickness = new Thickness(0)
		};
		_btnModeBasic.Click += (s, e) => SwitchCompressorMode("basic");

		_btnModeAdvanced = new Button
		{
			Content = "高级检测与联动",
			Padding = new Thickness(14, 5, 14, 5),
			CornerRadius = new CornerRadius(14),
			FontSize = 12.5,
			FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
			BorderThickness = new Thickness(0)
		};
		_btnModeAdvanced.Click += (s, e) => SwitchCompressorMode("advanced");

		modeSp.Children.Add(_btnModeBasic);
		modeSp.Children.Add(_btnModeAdvanced);
		_compModeBorder.Child = modeSp;

		Grid.SetColumn(_compModeBorder, 1);
		barGrid.Children.Add(_compModeBorder);

		UpdateModePillVisuals(isDark);

		return barGrid;
	}

	private void UpdatePowerButtonVisuals(bool? isDarkOverride = null)
	{
		bool isDark = isDarkOverride ?? IsDspDark;
		if (_compPowerText != null)
		{
			_compPowerText.Text = _compDraftEnabled ? "已启用" : "已旁路";
		}

		if (_compDraftEnabled)
		{
			var bg = new SolidColorBrush(isDark ? Color.FromArgb(85, 99, 102, 241) : Color.FromArgb(255, 238, 242, 255));
			var hoverBg = new SolidColorBrush(isDark ? Color.FromArgb(115, 99, 102, 241) : Color.FromArgb(255, 224, 231, 255));
			var pressedBg = new SolidColorBrush(isDark ? Color.FromArgb(140, 99, 102, 241) : Color.FromArgb(255, 199, 210, 254));
			var border = new SolidColorBrush(isDark ? Color.FromArgb(180, 129, 140, 248) : Color.FromArgb(255, 199, 210, 254));
			var hoverBorder = new SolidColorBrush(isDark ? Color.FromArgb(220, 165, 180, 252) : Color.FromArgb(255, 165, 180, 252));
			var fg = GetCompAccentSoftTextBrush(isDark);
			ApplyCompButtonVisuals(_compPowerBtn, bg, hoverBg, pressedBg, border, hoverBorder, fg, _compPowerIcon, _compPowerText);
		}
		else
		{
			ApplyCompNeutralButtonSurface(
				_compPowerBtn,
				isDark,
				GetCompSubHeaderTextBrush(isDark),
				_compPowerIcon,
				_compPowerText,
				GetCompMutedTextBrush(isDark));
		}
	}

	private void SwitchCompressorMode(string mode)
	{
		_compCurrentMode = mode;
		UpdateModePillVisuals();
		if (_basicControlsContainer != null)
		{
			_basicControlsContainer.Visibility = mode == "basic" ? Visibility.Visible : Visibility.Collapsed;
		}
		if (_advancedControlsContainer != null)
		{
			_advancedControlsContainer.Visibility = mode == "advanced" ? Visibility.Visible : Visibility.Collapsed;
		}
	}

	private void UpdateModePillVisuals(bool? isDarkOverride = null)
	{
		bool isDark = isDarkOverride ?? IsDspDark;
		ApplyCompPillButtonStyle(_btnModeBasic, _compCurrentMode == "basic", isDark);
		ApplyCompPillButtonStyle(_btnModeAdvanced, _compCurrentMode == "advanced", isDark);
	}
	#endregion

	#region 3. Header Meters Section (Input, GR, Output)
	private FrameworkElement CreateCompHeaderMetersSection()
	{
		bool isDark = IsDspDark;
		Brush primaryText = GetCompPrimaryTextBrush(isDark);
		Brush mutedText = GetCompMutedTextBrush(isDark);

		var grid = new Grid { ColumnSpacing = 16 };
		grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.0, GridUnitType.Star) });
		grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(0.85, GridUnitType.Star) });
		grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.0, GridUnitType.Star) });

		// Card 1: Input Level Meter
		var inputCard = CreateCompCard(isDark);
		var inputStack = new StackPanel { Spacing = 8 };

		var inHead = new Grid();
		inHead.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.0, GridUnitType.Star) });
		inHead.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
		var tbInTitle = new TextBlock { Text = "输入", FontSize = 12, FontWeight = Microsoft.UI.Text.FontWeights.SemiBold, Foreground = mutedText };
		var tbInType = new TextBlock { Text = "峰值 / RMS", FontSize = 11, Foreground = mutedText };
		_compMutedTextBlocks.Add(tbInTitle);
		_compMutedTextBlocks.Add(tbInType);
		Grid.SetColumn(tbInTitle, 0); Grid.SetColumn(tbInType, 1);
		inHead.Children.Add(tbInTitle); inHead.Children.Add(tbInType);
		inputStack.Children.Add(inHead);

		_compInputHeadlineTb = new TextBlock
		{
			Text = "-96.0 dB",
			FontSize = 32,
			FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
			Foreground = primaryText,
			FontFamily = new FontFamily("Segoe UI Variable Display, Consolas, Segoe UI")
		};
		_compPrimaryTextBlocks.Add(_compInputHeadlineTb);
		inputStack.Children.Add(_compInputHeadlineTb);

		// Track L
		var trackL = CreateDualTrackMeterRow("L", isDark, out _compInputRmsBarL, out _compInputPeakTickL, out _compInputDbTbL);
		inputStack.Children.Add(trackL);

		// Track R
		var trackR = CreateDualTrackMeterRow("R", isDark, out _compInputRmsBarR, out _compInputPeakTickR, out _compInputDbTbR);
		inputStack.Children.Add(trackR);

		inputCard.Child = inputStack;
		Grid.SetColumn(inputCard, 0);
		grid.Children.Add(inputCard);

		// Card 2: Gain Reduction Meter
		var grCard = CreateCompCard(isDark);
		var grStack = new StackPanel { Spacing = 8 };

		var grHead = new Grid();
		grHead.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.0, GridUnitType.Star) });
		grHead.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
		var tbGrTitle = new TextBlock { Text = "增益衰减 GR", FontSize = 12, FontWeight = Microsoft.UI.Text.FontWeights.SemiBold, Foreground = mutedText };
		var tbGrUnit = new TextBlock { Text = "GR", FontSize = 11, Foreground = mutedText };
		_compMutedTextBlocks.Add(tbGrTitle);
		_compMutedTextBlocks.Add(tbGrUnit);
		Grid.SetColumn(tbGrTitle, 0); Grid.SetColumn(tbGrUnit, 1);
		grHead.Children.Add(tbGrTitle); grHead.Children.Add(tbGrUnit);
		grStack.Children.Add(grHead);

		var grReadoutSp = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 4, VerticalAlignment = VerticalAlignment.Center };
		_compGrHeadlineTb = new TextBlock
		{
			Text = "0.0",
			FontSize = 34,
			FontWeight = Microsoft.UI.Text.FontWeights.Bold,
			Foreground = primaryText,
			FontFamily = new FontFamily("Segoe UI Variable Display, Consolas, Segoe UI")
		};
		_compPrimaryTextBlocks.Add(_compGrHeadlineTb);
		var grDbSuffix = new TextBlock
		{
			Text = "dB",
			FontSize = 14,
			FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
			Foreground = mutedText,
			VerticalAlignment = VerticalAlignment.Bottom,
			Margin = new Thickness(0, 0, 0, 4)
		};
		_compMutedTextBlocks.Add(grDbSuffix);
		grReadoutSp.Children.Add(_compGrHeadlineTb);
		grReadoutSp.Children.Add(grDbSuffix);
		grStack.Children.Add(grReadoutSp);

		// Horizontal GR Bar
		var grTrackContainer = new Grid
		{
			Height = 6,
			Background = GetCompMeterTrackBrush(isDark),
			CornerRadius = new CornerRadius(3)
		};
		_compMeterTracks.Add(grTrackContainer);
		_compGrReductionBar = new Border
		{
			HorizontalAlignment = HorizontalAlignment.Left,
			Width = 0,
			Background = new SolidColorBrush(Color.FromArgb(255, 99, 102, 241)),
			CornerRadius = new CornerRadius(3)
		};
		grTrackContainer.Children.Add(_compGrReductionBar);
		grStack.Children.Add(grTrackContainer);

		// Headroom readout
		_compHeadroomTb = new TextBlock
		{
			Text = "输出余量 96.0 dB",
			FontSize = 12,
			Foreground = mutedText,
			Margin = new Thickness(0, 4, 0, 0)
		};
		_compMutedTextBlocks.Add(_compHeadroomTb);
		grStack.Children.Add(_compHeadroomTb);

		grCard.Child = grStack;
		Grid.SetColumn(grCard, 1);
		grid.Children.Add(grCard);

		// Card 3: Output Level Meter
		var outputCard = CreateCompCard(isDark);
		var outStack = new StackPanel { Spacing = 8 };

		var outHead = new Grid();
		outHead.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.0, GridUnitType.Star) });
		outHead.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
		var tbOutTitle = new TextBlock { Text = "输出", FontSize = 12, FontWeight = Microsoft.UI.Text.FontWeights.SemiBold, Foreground = mutedText };
		var tbOutType = new TextBlock { Text = "峰值 / RMS", FontSize = 11, Foreground = mutedText };
		_compMutedTextBlocks.Add(tbOutTitle);
		_compMutedTextBlocks.Add(tbOutType);
		Grid.SetColumn(tbOutTitle, 0); Grid.SetColumn(tbOutType, 1);
		outHead.Children.Add(tbOutTitle); outHead.Children.Add(tbOutType);
		outStack.Children.Add(outHead);

		_compOutputHeadlineTb = new TextBlock
		{
			Text = "-96.0 dB",
			FontSize = 32,
			FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
			Foreground = primaryText,
			FontFamily = new FontFamily("Segoe UI Variable Display, Consolas, Segoe UI")
		};
		_compPrimaryTextBlocks.Add(_compOutputHeadlineTb);
		outStack.Children.Add(_compOutputHeadlineTb);

		// Track L
		var outTrackL = CreateDualTrackMeterRow("L", isDark, out _compOutputRmsBarL, out _compOutputPeakTickL, out _compOutputDbTbL);
		outStack.Children.Add(outTrackL);

		// Track R
		var outTrackR = CreateDualTrackMeterRow("R", isDark, out _compOutputRmsBarR, out _compOutputPeakTickR, out _compOutputDbTbR);
		outStack.Children.Add(outTrackR);

		outputCard.Child = outStack;
		Grid.SetColumn(outputCard, 2);
		grid.Children.Add(outputCard);

		return grid;
	}

	private FrameworkElement CreateDualTrackMeterRow(string channelLabel, bool isDark, out Border rmsBar, out Border peakTick, out TextBlock dbTb)
	{
		Brush mutedText = GetCompMutedTextBrush(isDark);

		var rowGrid = new Grid { ColumnSpacing = 8 };
		rowGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(14) });
		rowGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.0, GridUnitType.Star) });
		rowGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(56) });

		var tbLabel = new TextBlock
		{
			Text = channelLabel,
			FontSize = 11,
			FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
			Foreground = mutedText,
			VerticalAlignment = VerticalAlignment.Center
		};
		_compMutedTextBlocks.Add(tbLabel);
		Grid.SetColumn(tbLabel, 0);
		rowGrid.Children.Add(tbLabel);

		var trackBox = new Grid
		{
			Height = 6,
			Background = GetCompMeterTrackBrush(isDark),
			CornerRadius = new CornerRadius(3),
			VerticalAlignment = VerticalAlignment.Center
		};
		_compMeterTracks.Add(trackBox);
		rmsBar = new Border
		{
			HorizontalAlignment = HorizontalAlignment.Left,
			Width = 0,
			Background = new SolidColorBrush(Color.FromArgb(255, 99, 102, 241)),
			CornerRadius = new CornerRadius(3)
		};
		peakTick = new Border
		{
			HorizontalAlignment = HorizontalAlignment.Left,
			Width = 3,
			Height = 6,
			Background = new SolidColorBrush(Colors.White),
			BorderBrush = new SolidColorBrush(Color.FromArgb(255, 79, 70, 229)),
			BorderThickness = new Thickness(0.5),
			CornerRadius = new CornerRadius(1.5),
			Margin = new Thickness(0)
		};
		trackBox.Children.Add(rmsBar);
		trackBox.Children.Add(peakTick);
		Grid.SetColumn(trackBox, 1);
		rowGrid.Children.Add(trackBox);

		dbTb = new TextBlock
		{
			Text = "-96.0 dB",
			FontSize = 11,
			FontFamily = new FontFamily("Segoe UI Variable Text, Consolas, Segoe UI"),
			Foreground = mutedText,
			HorizontalAlignment = HorizontalAlignment.Right,
			VerticalAlignment = VerticalAlignment.Center
		};
		_compMutedTextBlocks.Add(dbTb);
		Grid.SetColumn(dbTb, 2);
		rowGrid.Children.Add(dbTb);

		return rowGrid;
	}
	#endregion

	#region 4. Main Stage (Left: Transfer Curve, Right: Basic / Advanced Controls)
	private FrameworkElement CreateCompMainStage()
	{
		var stageGrid = new Grid { ColumnSpacing = 16 };
		stageGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(380, GridUnitType.Pixel) });
		stageGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.0, GridUnitType.Star) });

		// Left Card: Dynamic Transfer Curve
		var curveCard = CreateTransferCurveCard();
		Grid.SetColumn(curveCard, 0);
		stageGrid.Children.Add(curveCard);

		// Right Container: Switches between Basic Controls & Advanced Controls
		var rightContainer = new Grid();
		_basicControlsContainer = CreateBasicControlsCard();
		_advancedControlsContainer = CreateAdvancedControlsCard();
		_advancedControlsContainer.Visibility = Visibility.Collapsed;

		rightContainer.Children.Add(_basicControlsContainer);
		rightContainer.Children.Add(_advancedControlsContainer);
		Grid.SetColumn(rightContainer, 1);
		stageGrid.Children.Add(rightContainer);

		return stageGrid;
	}

	private FrameworkElement CreateTransferCurveCard()
	{
		bool isDark = IsDspDark;
		var card = CreateCompCard(isDark);

		var stack = new StackPanel { Spacing = 10 };

		// Card Header: Title + Ratio Pill
		var head = new Grid();
		head.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.0, GridUnitType.Star) });
		head.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

		var titleSp = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8, VerticalAlignment = VerticalAlignment.Center };
		var curveTitleTb = new TextBlock
		{
			Text = "动态传输曲线",
			FontSize = 14.5,
			FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
			Foreground = GetCompPrimaryTextBrush(isDark)
		};
		_compPrimaryTextBlocks.Add(curveTitleTb);
		titleSp.Children.Add(curveTitleTb);
		_compCurveRatioTb = new TextBlock
		{
			Text = $"{_compDraftRatio:F1}:1",
			FontSize = 12,
			FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
			Foreground = GetCompAccentSoftTextBrush(isDark),
			VerticalAlignment = VerticalAlignment.Center
		};
		_compCurveRatioPill = new Border
		{
			Background = GetCompAccentSoftBgBrush(isDark),
			BorderBrush = GetCompAccentSoftBorderBrush(isDark),
			BorderThickness = new Thickness(1),
			CornerRadius = new CornerRadius(10),
			Padding = new Thickness(7, 2, 7, 2),
			Child = _compCurveRatioTb
		};
		titleSp.Children.Add(_compCurveRatioPill);
		Grid.SetColumn(titleSp, 0);
		head.Children.Add(titleSp);
		stack.Children.Add(head);

		// Canvas Viewbox
		var viewbox = new Viewbox
		{
			Stretch = Stretch.Uniform,
			Width = 340,
			Height = 340,
			HorizontalAlignment = HorizontalAlignment.Center
		};

		_compCurveCanvas = new Canvas
		{
			Width = CurvePlotSize,
			Height = CurvePlotSize,
			Background = new SolidColorBrush(Colors.Transparent)
		};

		// 1. Grid lines (-60, -45, -30, -15, 0)
		double[] gridTicks = { 0, -15, -30, -45, -60 };
		for (int i = 1; i < gridTicks.Length - 1; i++)
		{
			double db = gridTicks[i];
			double x = XForDb(db);
			double y = YForDb(db);

			// Horizontal line
			_compCurveCanvas.Children.Add(new Line
			{
				X1 = CurvePlotMargin,
				X2 = CurvePlotSize - CurvePlotMargin,
				Y1 = y,
				Y2 = y,
				Stroke = new SolidColorBrush(Color.FromArgb(60, 148, 163, 184)),
				StrokeThickness = 1
			});

			// Vertical line
			_compCurveCanvas.Children.Add(new Line
			{
				X1 = x,
				X2 = x,
				Y1 = CurvePlotMargin,
				Y2 = CurvePlotSize - CurvePlotMargin,
				Stroke = new SolidColorBrush(Color.FromArgb(60, 148, 163, 184)),
				StrokeThickness = 1
			});
		}

		// 2. Unity 1:1 diagonal line (from -60 to 0)
		_compCurveCanvas.Children.Add(new Line
		{
			X1 = XForDb(-60),
			Y1 = YForDb(-60),
			X2 = XForDb(0),
			Y2 = YForDb(0),
			Stroke = new SolidColorBrush(Color.FromArgb(120, 148, 163, 184)),
			StrokeThickness = 1.2
		});

		// 3. Shaded Reduction Area Path
		_compCurveAreaPath = new Path
		{
			Fill = new SolidColorBrush(Color.FromArgb(42, 99, 102, 241))
		};
		_compCurveCanvas.Children.Add(_compCurveAreaPath);

		// 4. Characteristic Curve Stroke Path
		_compCurveStrokePath = new Path
		{
			Stroke = new SolidColorBrush(Color.FromArgb(255, 99, 102, 241)),
			StrokeThickness = 2.4,
			StrokeStartLineCap = PenLineCap.Round,
			StrokeEndLineCap = PenLineCap.Round
		};
		_compCurveCanvas.Children.Add(_compCurveStrokePath);

		// 5. Threshold Vertical Guide Lines
		_compThresholdLineTop = new Line
		{
			Stroke = new SolidColorBrush(Color.FromArgb(180, 165, 180, 252)),
			StrokeThickness = 1.2,
			StrokeDashArray = new DoubleCollection { 3, 3 }
		};
		_compThresholdLineBottom = new Line
		{
			Stroke = new SolidColorBrush(Color.FromArgb(180, 165, 180, 252)),
			StrokeThickness = 1.2,
			StrokeDashArray = new DoubleCollection { 3, 3 }
		};
		_compCurveCanvas.Children.Add(_compThresholdLineTop);
		_compCurveCanvas.Children.Add(_compThresholdLineBottom);

		// 6. Threshold Drag Handle
		_compThresholdHandle = new Ellipse
		{
			Width = 14,
			Height = 14,
			Fill = new SolidColorBrush(Colors.White),
			Stroke = new SolidColorBrush(Color.FromArgb(255, 99, 102, 241)),
			StrokeThickness = 2.5
		};
		_compCurveCanvas.Children.Add(_compThresholdHandle);

		// 7. Live Dot
		_compLivePointDot = new Ellipse
		{
			Width = 8,
			Height = 8,
			Fill = new SolidColorBrush(Color.FromArgb(255, 34, 197, 94)),
			Stroke = new SolidColorBrush(Colors.White),
			StrokeThickness = 1.5,
			Visibility = Visibility.Collapsed
		};
		_compCurveCanvas.Children.Add(_compLivePointDot);

		// 8. Labels on Canvas (0, -15, -30, -45, -60, 输出, 输入)
		var monoFont = new FontFamily("Segoe UI Variable Text, Consolas, Segoe UI");
		var labelBrush = GetCompMutedTextBrush(isDark);

		// Y Labels (Left)
		AddCanvasText(_compCurveCanvas, "0", 0, YForDb(0) - 7, labelBrush, monoFont, 9.5);
		AddCanvasText(_compCurveCanvas, "-15", 0, YForDb(-15) - 7, labelBrush, monoFont, 9.5);
		AddCanvasText(_compCurveCanvas, "-30", 0, YForDb(-30) - 7, labelBrush, monoFont, 9.5);
		AddCanvasText(_compCurveCanvas, "-45", 0, YForDb(-45) - 7, labelBrush, monoFont, 9.5);
		AddCanvasText(_compCurveCanvas, "-60", 0, YForDb(-60) - 7, labelBrush, monoFont, 9.5);
		AddCanvasText(_compCurveCanvas, "输出", 18, YForDb(0) + 4, labelBrush, monoFont, 10);

		// X Labels (Bottom)
		AddCanvasText(_compCurveCanvas, "-60", XForDb(-60) - 10, CurvePlotSize - 12, labelBrush, monoFont, 9.5);
		AddCanvasText(_compCurveCanvas, "-45", XForDb(-45) - 10, CurvePlotSize - 12, labelBrush, monoFont, 9.5);
		AddCanvasText(_compCurveCanvas, "-30", XForDb(-30) - 10, CurvePlotSize - 12, labelBrush, monoFont, 9.5);
		AddCanvasText(_compCurveCanvas, "-15", XForDb(-15) - 10, CurvePlotSize - 12, labelBrush, monoFont, 9.5);
		AddCanvasText(_compCurveCanvas, "0", XForDb(0) - 4, CurvePlotSize - 12, labelBrush, monoFont, 9.5);
		AddCanvasText(_compCurveCanvas, "输入", XForDb(0) - 26, YForDb(-60) - 16, labelBrush, monoFont, 10);

		// Floating Threshold Tag
		_compThresholdTagTb = new TextBlock
		{
			Text = $"阈值 {_compDraftThresholdDb:F1} dB",
			FontSize = 10,
			FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
			Foreground = GetCompAccentSoftTextBrush(isDark)
		};
		_compThresholdTag = new Border
		{
			Background = GetCompAccentSoftBgBrush(isDark),
			BorderBrush = GetCompAccentSoftBorderBrush(isDark),
			BorderThickness = new Thickness(1),
			CornerRadius = new CornerRadius(5),
			Padding = new Thickness(5, 2, 5, 2),
			Child = _compThresholdTagTb
		};
		_compCurveCanvas.Children.Add(_compThresholdTag);

		// Drag hint at bottom
		AddCanvasText(_compCurveCanvas, "可拖动 · 调整阈值", XForDb(-60), CurvePlotSize - 1, labelBrush, monoFont, 9);

		// Pointer Events on Curve Canvas for Dragging Threshold
		_compCurveCanvas.PointerPressed += OnCurveCanvasPointerPressed;
		_compCurveCanvas.PointerMoved += OnCurveCanvasPointerMoved;
		_compCurveCanvas.PointerReleased += OnCurveCanvasPointerReleased;
		_compCurveCanvas.PointerCanceled += (s, e) => { _isDraggingThreshold = false; };

		viewbox.Child = _compCurveCanvas;
		stack.Children.Add(viewbox);

		card.Child = stack;
		return card;
	}

	private void OnCurveCanvasPointerPressed(object sender, PointerRoutedEventArgs e)
	{
		if (_compCurveCanvas == null) return;
		_compCurveCanvas.CapturePointer(e.Pointer);
		_isDraggingThreshold = true;
		var pt = e.GetCurrentPoint(_compCurveCanvas).Position;
		UpdateThresholdFromPointer(pt.X);
	}

	private void OnCurveCanvasPointerMoved(object sender, PointerRoutedEventArgs e)
	{
		if (!_isDraggingThreshold || _compCurveCanvas == null) return;
		var pt = e.GetCurrentPoint(_compCurveCanvas).Position;
		UpdateThresholdFromPointer(pt.X);
	}

	private void OnCurveCanvasPointerReleased(object sender, PointerRoutedEventArgs e)
	{
		if (_isDraggingThreshold && _compCurveCanvas != null)
		{
			_isDraggingThreshold = false;
			_compCurveCanvas.ReleasePointerCapture(e.Pointer);
		}
	}

	private void UpdateThresholdFromPointer(double pointerX)
	{
		double norm = Math.Clamp((pointerX - CurvePlotMargin) / CurvePlotSpan, 0.0, 1.0);
		double db = -60.0 + norm * 60.0;
		// Round to nearest 0.5
		db = Math.Round(db * 2.0) / 2.0;
		db = Math.Clamp(db, -60.0, 0.0);

		if (Math.Abs(_compDraftThresholdDb - db) > 0.01)
		{
			_compDraftThresholdDb = db;
			if (_sliderThreshold != null && Math.Abs(_sliderThreshold.Value - db) > 0.1)
			{
				_sliderThreshold.Value = db;
			}
			if (_tbThresholdVal != null)
			{
				_tbThresholdVal.Text = $"{db:F0} dB";
			}
			UpdateTransferCurveVisuals();
			UpdateDirtyStateBadge();
		}
	}

	private static double XForDb(double db)
	{
		double norm = Math.Clamp((db - (-60.0)) / 60.0, 0.0, 1.0);
		return CurvePlotMargin + norm * CurvePlotSpan;
	}

	private static double YForDb(double db)
	{
		double norm = Math.Clamp((db - (-60.0)) / 60.0, 0.0, 1.0);
		return CurvePlotMargin + (1.0 - norm) * CurvePlotSpan;
	}

	private void AddCanvasText(Canvas canvas, string text, double left, double top, Brush foreground, FontFamily font, double fontSize)
	{
		var tb = new TextBlock
		{
			Text = text,
			FontSize = fontSize,
			Foreground = foreground,
			FontFamily = font,
			IsHitTestVisible = false
		};
		_compMutedTextBlocks.Add(tb);
		Canvas.SetLeft(tb, left);
		Canvas.SetTop(tb, top);
		canvas.Children.Add(tb);
	}

	private void UpdateTransferCurveVisuals()
	{
		if (_compCurveStrokePath == null || _compCurveAreaPath == null) return;

		if (_compCurveRatioTb != null)
		{
			_compCurveRatioTb.Text = $"{_compDraftRatio:F1}:1";
		}

		double threshX = XForDb(_compDraftThresholdDb);
		double threshY = YForDb(FurinaPlayerCompressorProcessor.ComputeMixedOutputDb((float)_compDraftThresholdDb, (float)_compDraftThresholdDb, (float)_compDraftRatio, (float)_compDraftKneeDb, (float)_compDraftMakeupDb, (float)_compDraftMix, (float)_compDraftRangeDb));

		// Update Threshold Guide Line and Handle
		if (_compThresholdLineTop != null)
		{
			_compThresholdLineTop.X1 = threshX;
			_compThresholdLineTop.X2 = threshX;
			_compThresholdLineTop.Y1 = CurvePlotMargin;
			_compThresholdLineTop.Y2 = Math.Max(CurvePlotMargin, threshY - 9);
		}
		if (_compThresholdLineBottom != null)
		{
			_compThresholdLineBottom.X1 = threshX;
			_compThresholdLineBottom.X2 = threshX;
			_compThresholdLineBottom.Y1 = Math.Min(CurvePlotSize - CurvePlotMargin, threshY + 9);
			_compThresholdLineBottom.Y2 = CurvePlotSize - CurvePlotMargin;
		}
		if (_compThresholdHandle != null)
		{
			Canvas.SetLeft(_compThresholdHandle, threshX - 7);
			Canvas.SetTop(_compThresholdHandle, threshY - 7);
		}
		if (_compThresholdTag != null && _compThresholdTagTb != null)
		{
			_compThresholdTagTb.Text = $"阈值 {_compDraftThresholdDb:F1} dB";
			Canvas.SetLeft(_compThresholdTag, Math.Clamp(threshX - 35, 10, CurvePlotSize - 80));
			Canvas.SetTop(_compThresholdTag, Math.Max(0, CurvePlotMargin - 12));
		}

		// Compute 121 curve points (-60 to 0 dB, step 0.5)
		var curveFig = new PathFigure
		{
			StartPoint = new Point(XForDb(-60), YForDb(-60)),
			IsClosed = false,
			IsFilled = false
		};
		var areaFig = new PathFigure
		{
			StartPoint = new Point(XForDb(-60), YForDb(-60)),
			IsClosed = true,
			IsFilled = true
		};

		var curvePts = new List<Point>(121);
		for (int i = 0; i <= 120; i++)
		{
			double inDb = -60.0 + i * 0.5;
			double outDb = FurinaPlayerCompressorProcessor.ComputeMixedOutputDb((float)inDb, (float)_compDraftThresholdDb, (float)_compDraftRatio, (float)_compDraftKneeDb, (float)_compDraftMakeupDb, (float)_compDraftMix, (float)_compDraftRangeDb);
			curvePts.Add(new Point(XForDb(inDb), YForDb(outDb)));
		}

		for (int i = 0; i < curvePts.Count; i++)
		{
			curveFig.Segments.Add(new LineSegment { Point = curvePts[i] });
			areaFig.Segments.Add(new LineSegment { Point = curvePts[i] });
		}

		// Close area down along diagonal unity line
		areaFig.Segments.Add(new LineSegment { Point = new Point(XForDb(0), YForDb(0)) });
		areaFig.Segments.Add(new LineSegment { Point = new Point(XForDb(-60), YForDb(-60)) });

		var curveGeo = new PathGeometry();
		curveGeo.Figures.Add(curveFig);
		_compCurveStrokePath.Data = curveGeo;

		var areaGeo = new PathGeometry();
		areaGeo.Figures.Add(areaFig);
		_compCurveAreaPath.Data = areaGeo;
	}
	#endregion

	#region 5. Basic Controls Card
	private FrameworkElement CreateBasicControlsCard()
	{
		bool isDark = IsDspDark;
		var card = CreateCompCard(isDark, new Thickness(20, 16, 20, 18));

		var stack = new StackPanel { Spacing = 16 };

		// Card Header: Title + Detector Mode Pills [峰值] [RMS]
		var head = new Grid();
		head.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.0, GridUnitType.Star) });
		head.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

		var tbTitle = new TextBlock
		{
			Text = "基础控制",
			FontSize = 15,
			FontWeight = Microsoft.UI.Text.FontWeights.Bold,
			Foreground = GetCompPrimaryTextBrush(isDark),
			VerticalAlignment = VerticalAlignment.Center
		};
		_compPrimaryTextBlocks.Add(tbTitle);
		Grid.SetColumn(tbTitle, 0);
		head.Children.Add(tbTitle);

		// Detector Mode Pills
		_compDetBorder = new Border
		{
			Background = GetCompSoftSurfaceBrush(isDark),
			BorderBrush = CreateCompRimBrush(isDark),
			BorderThickness = new Thickness(1),
			CornerRadius = new CornerRadius(14),
			Padding = new Thickness(2)
		};
		var detSp = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 2 };

		_btnDetPeak = new Button
		{
			Content = "峰值",
			Padding = new Thickness(12, 4, 12, 4),
			CornerRadius = new CornerRadius(12),
			FontSize = 12,
			FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
			BorderThickness = new Thickness(0)
		};
		_btnDetPeak.Click += (s, e) => SetDetectorMode("Peak");

		_btnDetRms = new Button
		{
			Content = "RMS",
			Padding = new Thickness(12, 4, 12, 4),
			CornerRadius = new CornerRadius(12),
			FontSize = 12,
			FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
			BorderThickness = new Thickness(0)
		};
		_btnDetRms.Click += (s, e) => SetDetectorMode("RMS");

		detSp.Children.Add(_btnDetPeak);
		detSp.Children.Add(_btnDetRms);
		_compDetBorder.Child = detSp;
		Grid.SetColumn(_compDetBorder, 1);
		head.Children.Add(_compDetBorder);
		stack.Children.Add(head);

		UpdateDetectorPillVisuals(isDark);

		// 2-Column Controls Grid (3 rows x 2 cols)
		var grid = new Grid { ColumnSpacing = 24, RowSpacing = 18 };
		grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.0, GridUnitType.Star) });
		grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.0, GridUnitType.Star) });
		grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
		grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
		grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

		// Row 0, Col 0: 阈值 (-60 to 0 dB, default -18 dB)
		var ctlThresh = CreateSliderControl("阈值", $"{_compDraftThresholdDb:F0} dB", -60.0, 0.0, 0.5, _compDraftThresholdDb, out _sliderThreshold, out _tbThresholdVal, val =>
		{
			_compDraftThresholdDb = val;
			if (_tbThresholdVal != null) _tbThresholdVal.Text = $"{val:F0} dB";
			UpdateTransferCurveVisuals();
			UpdateDirtyStateBadge();
		});
		Grid.SetRow(ctlThresh, 0); Grid.SetColumn(ctlThresh, 0);
		grid.Children.Add(ctlThresh);

		// Row 0, Col 1: 压缩比 (1.0 to 20.0, default 4.0:1)
		var ctlRatio = CreateSliderControl("压缩比", $"{_compDraftRatio:F1} : 1", 1.0, 20.0, 0.1, _compDraftRatio, out _sliderRatio, out _tbRatioVal, val =>
		{
			_compDraftRatio = val;
			if (_tbRatioVal != null) _tbRatioVal.Text = $"{val:F1} : 1";
			UpdateTransferCurveVisuals();
			UpdateDirtyStateBadge();
		});
		Grid.SetRow(ctlRatio, 0); Grid.SetColumn(ctlRatio, 1);
		grid.Children.Add(ctlRatio);

		// Row 1, Col 0: 启动 (0.1 to 100 ms, default 10 ms)
		var ctlAttack = CreateSliderControl("启动", $"{_compDraftAttackMs:F0} ms", 0.1, 100.0, 0.5, _compDraftAttackMs, out _sliderAttack, out _tbAttackVal, val =>
		{
			_compDraftAttackMs = val;
			if (_tbAttackVal != null) _tbAttackVal.Text = $"{val:F0} ms";
			UpdateDirtyStateBadge();
		});
		Grid.SetRow(ctlAttack, 1); Grid.SetColumn(ctlAttack, 0);
		grid.Children.Add(ctlAttack);

		// Row 1, Col 1: 释放 (10 to 1000 ms, default 120 ms)
		var ctlRelease = CreateSliderControl("释放", $"{_compDraftReleaseMs:F0} ms", 10.0, 1000.0, 5.0, _compDraftReleaseMs, out _sliderRelease, out _tbReleaseVal, val =>
		{
			_compDraftReleaseMs = val;
			if (_tbReleaseVal != null) _tbReleaseVal.Text = $"{val:F0} ms";
			UpdateDirtyStateBadge();
		});
		Grid.SetRow(ctlRelease, 1); Grid.SetColumn(ctlRelease, 1);
		grid.Children.Add(ctlRelease);

		// Row 2, Col 0: 补偿增益 (0 to +24 dB, default 0 dB)
		var ctlMakeup = CreateSliderControl("补偿增益", $"{_compDraftMakeupDb:F0} dB", 0.0, 24.0, 0.5, _compDraftMakeupDb, out _sliderMakeup, out _tbMakeupVal, val =>
		{
			_compDraftMakeupDb = val;
			if (_tbMakeupVal != null) _tbMakeupVal.Text = $"{val:F0} dB";
			UpdateTransferCurveVisuals();
			UpdateDirtyStateBadge();
		});
		Grid.SetRow(ctlMakeup, 2); Grid.SetColumn(ctlMakeup, 0);
		grid.Children.Add(ctlMakeup);

		// Row 2, Col 1: 并行混合 (0% to 100%, default 100%)
		var ctlMix = CreateSliderControl("并行混合", $"{Math.Round(_compDraftMix * 100)} %", 0.0, 100.0, 1.0, _compDraftMix * 100.0, out _sliderMix, out _tbMixVal, val =>
		{
			_compDraftMix = val / 100.0;
			if (_tbMixVal != null) _tbMixVal.Text = $"{val:F0} %";
			UpdateTransferCurveVisuals();
			UpdateDirtyStateBadge();
		});
		Grid.SetRow(ctlMix, 2); Grid.SetColumn(ctlMix, 1);
		grid.Children.Add(ctlMix);

		stack.Children.Add(grid);
		card.Child = stack;
		return card;
	}

	private void SetDetectorMode(string mode)
	{
		_compDraftDetectorMode = mode;
		UpdateDetectorPillVisuals();
		UpdateDirtyStateBadge();
	}

	private void UpdateDetectorPillVisuals(bool? isDarkOverride = null)
	{
		bool isDark = isDarkOverride ?? IsDspDark;
		bool isPeak = string.Equals(_compDraftDetectorMode, "Peak", StringComparison.OrdinalIgnoreCase);
		ApplyCompPillButtonStyle(_btnDetPeak, isPeak, isDark);
		ApplyCompPillButtonStyle(_btnDetRms, !isPeak, isDark);
	}

	private FrameworkElement CreateSliderControl(string label, string initialValueText, double min, double max, double step, double initialValue, out Slider slider, out TextBlock valueTb, Action<double> onValueChanged)
	{
		bool isDark = IsDspDark;
		var sp = new StackPanel { Spacing = 6 };

		var head = new Grid();
		head.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.0, GridUnitType.Star) });
		head.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

		var tbLabel = new TextBlock
		{
			Text = label,
			FontSize = 13,
			FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
			Foreground = GetCompSecondaryTextBrush(isDark)
		};
		_compSecondaryTextBlocks.Add(tbLabel);
		Grid.SetColumn(tbLabel, 0);
		head.Children.Add(tbLabel);

		valueTb = new TextBlock
		{
			Text = initialValueText,
			FontSize = 13,
			FontWeight = Microsoft.UI.Text.FontWeights.Bold,
			Foreground = GetCompPrimaryTextBrush(isDark),
			FontFamily = new FontFamily("Segoe UI Variable Text, Consolas, Segoe UI")
		};
		_compPrimaryTextBlocks.Add(valueTb);
		Grid.SetColumn(valueTb, 1);
		head.Children.Add(valueTb);
		sp.Children.Add(head);

		slider = new Slider
		{
			Minimum = min,
			Maximum = max,
			StepFrequency = step,
			Value = initialValue,
			Margin = new Thickness(0, -2, 0, 0)
		};
		slider.ValueChanged += (s, e) => onValueChanged(e.NewValue);
		sp.Children.Add(slider);

		return sp;
	}
	#endregion

	#region 6. Advanced Controls Card
	private FrameworkElement CreateAdvancedControlsCard()
	{
		bool isDark = IsDspDark;
		Brush primaryText = GetCompPrimaryTextBrush(isDark);
		Brush mutedText = GetCompMutedTextBrush(isDark);

		var card = CreateCompCard(isDark, new Thickness(20, 16, 20, 18));

		var stack = new StackPanel { Spacing = 16 };

		var advTitleTb = new TextBlock
		{
			Text = "高级检测与联动",
			FontSize = 15,
			FontWeight = Microsoft.UI.Text.FontWeights.Bold,
			Foreground = primaryText
		};
		_compPrimaryTextBlocks.Add(advTitleTb);
		stack.Children.Add(advTitleTb);

		var grid = new Grid { ColumnSpacing = 24, RowSpacing = 16 };
		grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.0, GridUnitType.Star) });
		grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.0, GridUnitType.Star) });
		grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
		grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
		grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

		// Row 0, Col 0: 侧链高通开关 & 频率
		var scHpSp = new StackPanel { Spacing = 4 };
		_toggleSidechainHp = new ToggleSwitch
		{
			Header = "侧链高通滤波",
			IsOn = _compDraftSidechainHp,
			OnContent = "已开启",
			OffContent = "已关闭",
			Foreground = primaryText
		};
		_toggleSidechainHp.Toggled += (s, e) =>
		{
			_compDraftSidechainHp = _toggleSidechainHp.IsOn;
			if (_sliderSidechainHpHz != null) _sliderSidechainHpHz.IsEnabled = _compDraftSidechainHp;
			UpdateDirtyStateBadge();
		};
		scHpSp.Children.Add(_toggleSidechainHp);

		var ctlScHz = CreateSliderControl("高通截止频率", $"{_compDraftSidechainHpHz:F0} Hz", 20.0, 400.0, 5.0, _compDraftSidechainHpHz, out _sliderSidechainHpHz, out _tbSidechainHpHzVal, val =>
		{
			_compDraftSidechainHpHz = val;
			if (_tbSidechainHpHzVal != null) _tbSidechainHpHzVal.Text = $"{val:F0} Hz";
			UpdateDirtyStateBadge();
		});
		_sliderSidechainHpHz.IsEnabled = _compDraftSidechainHp;
		scHpSp.Children.Add(ctlScHz);
		Grid.SetRow(scHpSp, 0); Grid.SetColumn(scHpSp, 0);
		grid.Children.Add(scHpSp);

		// Row 0, Col 1: 自动释放开关
		var arSp = new StackPanel { Spacing = 6 };
		_toggleAutoRelease = new ToggleSwitch
		{
			Header = "自动释放 (Auto Release)",
			IsOn = _compDraftAutoRelease,
			OnContent = "开启",
			OffContent = "关闭",
			Foreground = primaryText
		};
		_toggleAutoRelease.Toggled += (s, e) =>
		{
			_compDraftAutoRelease = _toggleAutoRelease.IsOn;
			UpdateDirtyStateBadge();
		};
		arSp.Children.Add(_toggleAutoRelease);
		var arDescTb = new TextBlock
		{
			Text = "依据节目动态自适应释放时间：瞬态快速恢复，持续压缩平滑衰减。",
			FontSize = 11,
			Foreground = mutedText,
			TextWrapping = TextWrapping.Wrap
		};
		_compMutedTextBlocks.Add(arDescTb);
		arSp.Children.Add(arDescTb);
		Grid.SetRow(arSp, 0); Grid.SetColumn(arSp, 1);
		grid.Children.Add(arSp);

		// Row 1, Col 0: 软拐点宽度 (Knee: 0 to 24 dB, default 6 dB)
		var ctlKnee = CreateSliderControl("软拐点宽度 (Knee)", $"{_compDraftKneeDb:F1} dB", 0.0, 24.0, 0.5, _compDraftKneeDb, out _sliderKnee, out _tbKneeVal, val =>
		{
			_compDraftKneeDb = val;
			if (_tbKneeVal != null) _tbKneeVal.Text = $"{val:F1} dB";
			UpdateTransferCurveVisuals();
			UpdateDirtyStateBadge();
		});
		Grid.SetRow(ctlKnee, 1); Grid.SetColumn(ctlKnee, 0);
		grid.Children.Add(ctlKnee);

		// Row 1, Col 1: 最大压缩范围 (Range: 0 to 72 dB, default 60 dB)
		var ctlRange = CreateSliderControl("最大压缩范围 (Range)", $"{_compDraftRangeDb:F0} dB", 0.0, 72.0, 1.0, _compDraftRangeDb, out _sliderRange, out _tbRangeVal, val =>
		{
			_compDraftRangeDb = val;
			if (_tbRangeVal != null) _tbRangeVal.Text = $"{val:F0} dB";
			UpdateTransferCurveVisuals();
			UpdateDirtyStateBadge();
		});
		Grid.SetRow(ctlRange, 1); Grid.SetColumn(ctlRange, 1);
		grid.Children.Add(ctlRange);

		// Row 2, Col 0: 立体声联动 (Stereo Link: 0% to 100%, default 100%)
		var ctlStereoLink = CreateSliderControl("立体声联动 (Stereo Link)", $"{Math.Round(_compDraftStereoLink * 100)} %", 0.0, 100.0, 1.0, _compDraftStereoLink * 100.0, out _sliderStereoLink, out _tbStereoLinkVal, val =>
		{
			_compDraftStereoLink = val / 100.0;
			if (_tbStereoLinkVal != null) _tbStereoLinkVal.Text = $"{val:F0} %";
			UpdateDirtyStateBadge();
		});
		Grid.SetRow(ctlStereoLink, 2); Grid.SetColumn(ctlStereoLink, 0);
		grid.Children.Add(ctlStereoLink);

		stack.Children.Add(grid);
		card.Child = stack;
		return card;
	}
	#endregion

	#region 7. GR History Plot Section
	private FrameworkElement CreateCompHistorySection()
	{
		bool isDark = IsDspDark;
		Brush primaryText = GetCompPrimaryTextBrush(isDark);
		Brush mutedText = GetCompMutedTextBrush(isDark);

		var card = CreateCompCard(isDark);

		var stack = new StackPanel { Spacing = 8 };

		// Card Header: GR 历史 (left) & 最近 8 秒 (right)
		var head = new Grid();
		head.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.0, GridUnitType.Star) });
		head.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

		var tbTitle = new TextBlock
		{
			Text = "GR 历史",
			FontSize = 14.5,
			FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
			Foreground = primaryText
		};
		_compPrimaryTextBlocks.Add(tbTitle);
		var tbWin = new TextBlock
		{
			Text = "最近 8 秒",
			FontSize = 12,
			Foreground = mutedText
		};
		_compMutedTextBlocks.Add(tbWin);
		Grid.SetColumn(tbTitle, 0); Grid.SetColumn(tbWin, 1);
		head.Children.Add(tbTitle); head.Children.Add(tbWin);
		stack.Children.Add(head);

		// History Viewbox & Canvas
		var viewbox = new Viewbox
		{
			Stretch = Stretch.Uniform,
			Height = 110,
			HorizontalAlignment = HorizontalAlignment.Stretch
		};

		_compHistoryCanvas = new Canvas
		{
			Width = HistoryWidth,
			Height = HistoryHeight,
			Background = new SolidColorBrush(Colors.Transparent)
		};

		// Grid lines & labels: 0, -12, -24
		double[] ticks = { 0, 12, 24 };
		var monoFont = new FontFamily("Segoe UI Variable Text, Consolas, Segoe UI");

		foreach (double t in ticks)
		{
			double y = YForReduction(t);
			_compHistoryCanvas.Children.Add(new Line
			{
				X1 = 28,
				X2 = HistoryWidth,
				Y1 = y,
				Y2 = y,
				Stroke = new SolidColorBrush(Color.FromArgb(50, 148, 163, 184)),
				StrokeThickness = 1
			});
			string txt = t == 0 ? "0" : $"-{t:F0}";
			AddCanvasText(_compHistoryCanvas, txt, 2, y - 6, mutedText, monoFont, 9.5);
		}

		_compHistoryAreaPath = new Path
		{
			Fill = new SolidColorBrush(Color.FromArgb(32, 99, 102, 241))
		};
		_compHistoryLinePath = new Path
		{
			Stroke = new SolidColorBrush(Color.FromArgb(255, 99, 102, 241)),
			StrokeThickness = 1.8,
			StrokeStartLineCap = PenLineCap.Round,
			StrokeEndLineCap = PenLineCap.Round
		};

		_compHistoryCanvas.Children.Add(_compHistoryAreaPath);
		_compHistoryCanvas.Children.Add(_compHistoryLinePath);

		viewbox.Child = _compHistoryCanvas;
		stack.Children.Add(viewbox);

		// Apply Hint Note at Bottom
		_compApplyHintTb = new TextBlock
		{
			Text = "编辑先进入草稿；点击应用后由原生主机平滑接管。",
			FontSize = 12,
			Foreground = mutedText,
			Margin = new Thickness(2, 2, 0, 0)
		};
		stack.Children.Add(_compApplyHintTb);

		card.Child = stack;
		return card;
	}

	private static double YForReduction(double grDb)
	{
		double norm = Math.Clamp(grDb / 24.0, 0.0, 1.0);
		return 86.0 - norm * 74.0;
	}

	private void UpdateHistoryPlotVisuals()
	{
		if (_compHistoryLinePath == null || _compHistoryAreaPath == null) return;

		var lineFig = new PathFigure { IsClosed = false, IsFilled = false };
		var areaFig = new PathFigure { IsClosed = true, IsFilled = true };

		int count = _compGrHistory.Length;
		double startX = 28.0;
		double plotW = HistoryWidth - startX;
		double yZero = YForReduction(0.0);

		for (int i = 0; i < count; i++)
		{
			int circularIdx = (_compGrHistoryIndex + i) % count;
			float gr = _compGrHistory[circularIdx];
			double x = startX + (double)i / (count - 1) * plotW;
			double y = YForReduction(gr);

			if (i == 0)
			{
				lineFig.StartPoint = new Point(x, y);
				areaFig.StartPoint = new Point(x, yZero);
				areaFig.Segments.Add(new LineSegment { Point = new Point(x, y) });
			}
			else
			{
				lineFig.Segments.Add(new LineSegment { Point = new Point(x, y) });
				areaFig.Segments.Add(new LineSegment { Point = new Point(x, y) });
			}
		}

		areaFig.Segments.Add(new LineSegment { Point = new Point(HistoryWidth, yZero) });

		var lineGeo = new PathGeometry();
		lineGeo.Figures.Add(lineFig);
		_compHistoryLinePath.Data = lineGeo;

		var areaGeo = new PathGeometry();
		areaGeo.Figures.Add(areaFig);
		_compHistoryAreaPath.Data = areaGeo;
	}
	#endregion

	#region 8. Draft & Apply Logic
	private void ResetCompressorDefaults()
	{
		_compDraftThresholdDb = -18.0;
		_compDraftRatio = 4.0;
		_compDraftAttackMs = 10.0;
		_compDraftReleaseMs = 120.0;
		_compDraftKneeDb = 6.0;
		_compDraftMakeupDb = 0.0;
		_compDraftMix = 1.0;
		_compDraftDetectorMode = "Peak";
		_compDraftSidechainHp = false;
		_compDraftSidechainHpHz = 80.0;
		_compDraftAutoRelease = false;
		_compDraftRangeDb = 60.0;
		_compDraftStereoLink = 1.0;

		// Sync UI sliders
		if (_sliderThreshold != null) _sliderThreshold.Value = _compDraftThresholdDb;
		if (_sliderRatio != null) _sliderRatio.Value = _compDraftRatio;
		if (_sliderAttack != null) _sliderAttack.Value = _compDraftAttackMs;
		if (_sliderRelease != null) _sliderRelease.Value = _compDraftReleaseMs;
		if (_sliderMakeup != null) _sliderMakeup.Value = _compDraftMakeupDb;
		if (_sliderMix != null) _sliderMix.Value = 100.0;
		if (_sliderKnee != null) _sliderKnee.Value = _compDraftKneeDb;
		if (_sliderRange != null) _sliderRange.Value = _compDraftRangeDb;
		if (_sliderStereoLink != null) _sliderStereoLink.Value = 100.0;
		if (_sliderSidechainHpHz != null) _sliderSidechainHpHz.Value = _compDraftSidechainHpHz;
		if (_toggleSidechainHp != null) _toggleSidechainHp.IsOn = false;
		if (_toggleAutoRelease != null) _toggleAutoRelease.IsOn = false;

		UpdateDetectorPillVisuals();
		UpdateTransferCurveVisuals();
		UpdateDirtyStateBadge();
	}

	private void ApplyCompressorDraftParameters()
	{
		if (ViewModel == null) return;

		ViewModel.EchoCompressorEnabled = _compDraftEnabled;
		ViewModel.EchoCompressorThresholdDb = _compDraftThresholdDb;
		ViewModel.EchoCompressorRatio = _compDraftRatio;
		ViewModel.EchoCompressorAttackMs = _compDraftAttackMs;
		ViewModel.EchoCompressorReleaseMs = _compDraftReleaseMs;
		ViewModel.EchoCompressorKneeDb = _compDraftKneeDb;
		ViewModel.EchoCompressorMakeupDb = _compDraftMakeupDb;
		ViewModel.EchoCompressorMix = _compDraftMix;
		ViewModel.EchoCompressorDetectorMode = _compDraftDetectorMode;
		ViewModel.EchoCompressorSidechainHighpassEnabled = _compDraftSidechainHp;
		ViewModel.EchoCompressorSidechainHighpassHz = _compDraftSidechainHpHz;
		ViewModel.EchoCompressorAutoRelease = _compDraftAutoRelease;
		ViewModel.EchoCompressorRangeDb = _compDraftRangeDb;
		ViewModel.EchoCompressorStereoLink = _compDraftStereoLink;

		ViewModel.ApplyEchoDsp();
		UpdatePowerButtonVisuals();
		UpdateDirtyStateBadge();
	}

	private void UpdateDirtyStateBadge(bool? isDarkOverride = null)
	{
		bool isDark = isDarkOverride ?? IsDspDark;
		int dirtyCount = 0;

		if (ViewModel != null)
		{
			if (Math.Abs(_compDraftThresholdDb - ViewModel.EchoCompressorThresholdDb) > 0.1) dirtyCount++;
			if (Math.Abs(_compDraftRatio - ViewModel.EchoCompressorRatio) > 0.05) dirtyCount++;
			if (Math.Abs(_compDraftAttackMs - ViewModel.EchoCompressorAttackMs) > 0.1) dirtyCount++;
			if (Math.Abs(_compDraftReleaseMs - ViewModel.EchoCompressorReleaseMs) > 0.5) dirtyCount++;
			if (Math.Abs(_compDraftKneeDb - ViewModel.EchoCompressorKneeDb) > 0.1) dirtyCount++;
			if (Math.Abs(_compDraftMakeupDb - ViewModel.EchoCompressorMakeupDb) > 0.1) dirtyCount++;
			if (Math.Abs(_compDraftMix - ViewModel.EchoCompressorMix) > 0.01) dirtyCount++;
			if (!string.Equals(_compDraftDetectorMode, ViewModel.EchoCompressorDetectorMode, StringComparison.OrdinalIgnoreCase)) dirtyCount++;
			if (_compDraftSidechainHp != ViewModel.EchoCompressorSidechainHighpassEnabled) dirtyCount++;
			if (Math.Abs(_compDraftSidechainHpHz - ViewModel.EchoCompressorSidechainHighpassHz) > 1.0) dirtyCount++;
			if (_compDraftAutoRelease != ViewModel.EchoCompressorAutoRelease) dirtyCount++;
			if (Math.Abs(_compDraftRangeDb - ViewModel.EchoCompressorRangeDb) > 0.5) dirtyCount++;
			if (Math.Abs(_compDraftStereoLink - ViewModel.EchoCompressorStereoLink) > 0.01) dirtyCount++;
		}

		_compIsDirty = dirtyCount > 0;
		if (_compPendingBadge != null)
		{
			_compPendingBadge.Visibility = _compIsDirty ? Visibility.Visible : Visibility.Collapsed;
		}
		if (_compPendingBadgeTb != null)
		{
			_compPendingBadgeTb.Text = $"未应用 · {dirtyCount}";
		}

		ApplyDirtyStateVisuals(isDark, _compIsDirty);
	}

	private void ApplyDirtyStateVisuals(bool isDark, bool isDirty)
	{
		if (_compPendingBadge != null)
		{
			_compPendingBadge.Background = new SolidColorBrush(isDark ? Color.FromArgb(75, 245, 158, 11) : Color.FromArgb(255, 254, 243, 199));
			_compPendingBadge.BorderBrush = new SolidColorBrush(isDark ? Color.FromArgb(190, 251, 191, 36) : Color.FromArgb(255, 252, 211, 77));
		}
		if (_compPendingBadgeTb != null)
		{
			_compPendingBadgeTb.Foreground = new SolidColorBrush(isDark ? Color.FromArgb(255, 253, 230, 138) : Color.FromArgb(255, 180, 83, 9));
		}
		if (_compApplyHintTb != null)
		{
			_compApplyHintTb.Text = isDirty
				? "草稿已修改，当前听到的仍是上次应用的参数；点击「应用参数」后生效。"
				: "编辑先进入草稿；点击应用后由原生主机平滑接管。";
			_compApplyHintTb.Foreground = isDirty
				? new SolidColorBrush(isDark ? Color.FromArgb(255, 251, 191, 36) : Color.FromArgb(255, 217, 119, 6))
				: GetCompMutedTextBrush(isDark);
		}
		if (_compApplyBtn != null)
		{
			if (isDirty)
			{
				var bg = new SolidColorBrush(Color.FromArgb(255, 79, 70, 229));
				var hoverBg = new SolidColorBrush(Color.FromArgb(255, 99, 102, 241));
				var pressedBg = new SolidColorBrush(Color.FromArgb(255, 67, 56, 202));
				var border = new SolidColorBrush(Color.FromArgb(255, 129, 140, 248));
				var hoverBorder = new SolidColorBrush(Color.FromArgb(255, 165, 180, 252));
				var fg = new SolidColorBrush(Colors.White);
				ApplyCompButtonVisuals(_compApplyBtn, bg, hoverBg, pressedBg, border, hoverBorder, fg, _compApplyIcon, _compApplyText);
			}
			else
			{
				ApplyCompNeutralButtonSurface(
					_compApplyBtn,
					isDark,
					GetCompSecondaryTextBrush(isDark),
					_compApplyIcon,
					_compApplyText,
					GetCompSubHeaderTextBrush(isDark));
			}
		}
	}
	#endregion

	#region 9. 50ms Realtime Meter Tick
	/// <summary>
	/// 由宿主 DspPage 定时器（50ms / 20fps）驱动的高性能无锁仪表与轨迹刷新。
	/// </summary>
	public void UpdateCompressorMetersTick()
	{
		try
		{
			if (ViewModel?.Engine?.DspChain == null) return;
			var comp = ViewModel.Engine.DspChain.Compressor;
			if (comp == null) return;

			// 1. Read telemetry facts
			float inPeakL = comp.InputPeakDbL;
			float inPeakR = comp.InputPeakDbR;
			float inRmsL = comp.InputRmsDbL;
			float inRmsR = comp.InputRmsDbR;
			float inHeadline = Math.Max(inPeakL, inPeakR);

			float outPeakL = comp.OutputPeakDbL;
			float outPeakR = comp.OutputPeakDbR;
			float outRmsL = comp.OutputRmsDbL;
			float outRmsR = comp.OutputRmsDbR;
			float outHeadline = Math.Max(outPeakL, outPeakR);

			float gr = comp.GainReductionDb;
			float headroom = comp.OutputHeadroomDb;

			// 2. Update Input Meter
			if (_compInputHeadlineTb != null) _compInputHeadlineTb.Text = $"{inHeadline:F1} dB";
			if (_compInputDbTbL != null) _compInputDbTbL.Text = $"{inPeakL:F1} dB";
			if (_compInputDbTbR != null) _compInputDbTbR.Text = $"{inPeakR:F1} dB";
			UpdateTrackMeterBar(_compInputRmsBarL, _compInputPeakTickL, inRmsL, inPeakL);
			UpdateTrackMeterBar(_compInputRmsBarR, _compInputPeakTickR, inRmsR, inPeakR);

			// 3. Update GR Meter
			if (_compGrHeadlineTb != null) _compGrHeadlineTb.Text = $"{gr:F1}";
			if (_compGrReductionBar != null)
			{
				double grNorm = Math.Clamp(gr / 24.0, 0.0, 1.0);
				_compGrReductionBar.Width = grNorm * 180.0;
			}
			if (_compHeadroomTb != null) _compHeadroomTb.Text = $"输出余量 {headroom:F1} dB";

			// 4. Update Output Meter
			if (_compOutputHeadlineTb != null) _compOutputHeadlineTb.Text = $"{outHeadline:F1} dB";
			if (_compOutputDbTbL != null) _compOutputDbTbL.Text = $"{outPeakL:F1} dB";
			if (_compOutputDbTbR != null) _compOutputDbTbR.Text = $"{outPeakR:F1} dB";
			UpdateTrackMeterBar(_compOutputRmsBarL, _compOutputPeakTickL, outRmsL, outPeakL);
			UpdateTrackMeterBar(_compOutputRmsBarR, _compOutputPeakTickR, outRmsR, outPeakR);

			// 5. Update Live Dot on Transfer Curve
			if (_compLivePointDot != null)
			{
				if (inHeadline > -59.5f)
				{
					_compLivePointDot.Visibility = Visibility.Visible;
					Canvas.SetLeft(_compLivePointDot, XForDb(inHeadline) - 4);
					Canvas.SetTop(_compLivePointDot, YForDb(outHeadline) - 4);
				}
				else
				{
					_compLivePointDot.Visibility = Visibility.Collapsed;
				}
			}

			// 6. Push to GR History rolling buffer and refresh plot
			_compGrHistory[_compGrHistoryIndex] = gr;
			_compGrHistoryIndex = (_compGrHistoryIndex + 1) % _compGrHistory.Length;
			UpdateHistoryPlotVisuals();
		}
		catch { }
	}

	private static void UpdateTrackMeterBar(Border? rmsBar, Border? peakTick, float rmsDb, float peakDb)
	{
		if (rmsBar == null && peakTick == null) return;
		double parentWidth = 140.0;
		if (rmsBar?.Parent is FrameworkElement p && p.ActualWidth > 10.0)
		{
			parentWidth = p.ActualWidth;
		}

		double rmsNorm = Math.Clamp((rmsDb - (-60.0)) / 60.0, 0.0, 1.0);
		double peakNorm = Math.Clamp((peakDb - (-60.0)) / 60.0, 0.0, 1.0);

		if (rmsBar != null)
		{
			rmsBar.Width = rmsNorm * parentWidth;
		}
		if (peakTick != null)
		{
			double left = Math.Clamp(peakNorm * parentWidth - 2, 0.0, parentWidth - 3);
			peakTick.Margin = new Thickness(left, 0, 0, 0);
		}
	}

	private void RefreshCompressorStatus()
	{
		InitCompressorDraftState();
		UpdatePowerButtonVisuals();
		UpdateDetectorPillVisuals();
		UpdateDirtyStateBadge();
		UpdateTransferCurveVisuals();
	}
	#endregion

	#region 10. Theme Helpers & Refresh
	private Border CreateCompCard(bool isDark, Thickness? padding = null)
	{
		var card = new Border
		{
			Background = CreateCompSurfaceBrush(LiquidGlassVariant.Regular, isDark, Math.Clamp(0.88 / 0.90, 0.72, 1.05)),
			BorderBrush = CreateCompRimBrush(isDark),
			BorderThickness = new Thickness(1),
			CornerRadius = new CornerRadius(12),
			Padding = padding ?? new Thickness(18, 14, 18, 14)
		};
		_compCardBorders.Add(card);
		return card;
	}

	private static Brush CreateCompSurfaceBrush(LiquidGlassVariant variant, bool isDark, double opacityScale = 1.0)
	{
		return LiquidGlassEngine.CreateSurfaceBrush(variant, isDark: isDark, opacityScale: opacityScale);
	}

	private static Brush CreateCompRimBrush(bool isDark, bool isHovered = false, bool isSelected = false)
	{
		return LiquidGlassEngine.CreateSpecularRimBrush(
			LiquidGlassVariant.Regular,
			isDark: isDark,
			isHovered: isHovered,
			isSelected: isSelected);
	}

	private static Brush GetCompSoftSurfaceBrush(bool isDark)
	{
		return isDark
			? CreateCompSurfaceBrush(LiquidGlassVariant.Clear, isDark: true, opacityScale: 0.45)
			: new SolidColorBrush(Color.FromArgb(220, 241, 245, 249));
	}

	private static Brush GetCompMeterTrackBrush(bool isDark)
	{
		return new SolidColorBrush(isDark ? Color.FromArgb(70, 255, 255, 255) : Color.FromArgb(255, 226, 232, 240));
	}

	private static Brush GetCompPrimaryTextBrush(bool isDark) => new SolidColorBrush(isDark ? Colors.White : Color.FromArgb(255, 15, 23, 42));
	private static Brush GetCompSecondaryTextBrush(bool isDark) => new SolidColorBrush(isDark ? Color.FromArgb(255, 248, 250, 252) : Color.FromArgb(255, 51, 65, 85));
	private static Brush GetCompSubHeaderTextBrush(bool isDark) => new SolidColorBrush(isDark ? Color.FromArgb(255, 226, 232, 240) : Color.FromArgb(255, 71, 85, 105));
	private static Brush GetCompMutedTextBrush(bool isDark) => new SolidColorBrush(isDark ? Color.FromArgb(255, 203, 213, 225) : Color.FromArgb(255, 100, 116, 139));
	private static Brush GetCompAccentSoftBgBrush(bool isDark) => new SolidColorBrush(isDark ? Color.FromArgb(85, 99, 102, 241) : Color.FromArgb(255, 238, 242, 255));
	private static Brush GetCompAccentSoftBorderBrush(bool isDark) => new SolidColorBrush(isDark ? Color.FromArgb(180, 129, 140, 248) : Color.FromArgb(255, 199, 210, 254));
	private static Brush GetCompAccentSoftTextBrush(bool isDark) => new SolidColorBrush(isDark ? Color.FromArgb(255, 199, 210, 254) : Color.FromArgb(255, 79, 70, 229));

	private static void ApplyCompButtonVisuals(
		Button? btn,
		Brush background,
		Brush pointerOverBg,
		Brush pressedBg,
		Brush borderBrush,
		Brush pointerOverBorder,
		Brush foreground,
		FontIcon? icon = null,
		TextBlock? label = null,
		Brush? iconForeground = null)
	{
		if (btn == null) return;

		btn.Background = background;
		btn.BorderBrush = borderBrush;
		btn.Foreground = foreground;

		btn.Resources["ButtonBackground"] = background;
		btn.Resources["ButtonBackgroundPointerOver"] = pointerOverBg;
		btn.Resources["ButtonBackgroundPressed"] = pressedBg;
		btn.Resources["ButtonBorderBrush"] = borderBrush;
		btn.Resources["ButtonBorderBrushPointerOver"] = pointerOverBorder;
		btn.Resources["ButtonBorderBrushPressed"] = pointerOverBorder;
		btn.Resources["ButtonForeground"] = foreground;
		btn.Resources["ButtonForegroundPointerOver"] = foreground;
		btn.Resources["ButtonForegroundPressed"] = foreground;

		if (icon != null) icon.Foreground = iconForeground ?? foreground;
		if (label != null) label.Foreground = foreground;
	}

	private static void ApplyCompNeutralButtonSurface(
		Button? btn,
		bool isDark,
		Brush foreground,
		FontIcon? icon = null,
		TextBlock? label = null,
		Brush? iconForeground = null)
	{
		Brush normalBg = CreateCompSurfaceBrush(LiquidGlassVariant.Clear, isDark, 0.92);
		Brush hoverBg = CreateCompSurfaceBrush(LiquidGlassVariant.Clear, isDark, 1.0);
		Brush pressedBg = CreateCompSurfaceBrush(LiquidGlassVariant.Regular, isDark, 0.96);
		Brush rim = CreateCompRimBrush(isDark);
		Brush hoverRim = CreateCompRimBrush(isDark, isHovered: true);

		ApplyCompButtonVisuals(btn, normalBg, hoverBg, pressedBg, rim, hoverRim, foreground, icon, label, iconForeground);
	}

	private static void ApplyCompPillButtonStyle(Button? btn, bool isSelected, bool isDark)
	{
		if (btn == null) return;

		if (isSelected)
		{
			var bg = new SolidColorBrush(Color.FromArgb(255, 99, 102, 241));
			var hoverBg = new SolidColorBrush(Color.FromArgb(255, 129, 140, 248));
			var pressedBg = new SolidColorBrush(Color.FromArgb(255, 79, 70, 229));
			var border = new SolidColorBrush(Colors.Transparent);
			var fg = new SolidColorBrush(Colors.White);
			ApplyCompButtonVisuals(btn, bg, hoverBg, pressedBg, border, border, fg);
		}
		else
		{
			var bg = new SolidColorBrush(Colors.Transparent);
			var hoverBg = new SolidColorBrush(isDark ? Color.FromArgb(40, 255, 255, 255) : Color.FromArgb(22, 15, 23, 42));
			var pressedBg = new SolidColorBrush(isDark ? Color.FromArgb(60, 255, 255, 255) : Color.FromArgb(38, 15, 23, 42));
			var border = new SolidColorBrush(Colors.Transparent);
			var fg = GetCompMutedTextBrush(isDark);
			ApplyCompButtonVisuals(btn, bg, hoverBg, pressedBg, border, border, fg);
		}
	}

	public void RefreshCompressorTheme(bool isDark)
	{
		if (_compressorWorkbench == null) return;

		_compressorWorkbench.RequestedTheme = isDark ? ElementTheme.Dark : ElementTheme.Light;
		_compressorWorkbench.Background = CreateCompSurfaceBrush(LiquidGlassVariant.Clear, isDark, Math.Clamp(0.72 / 0.85, 0.55, 1.0));

		Brush primaryText = GetCompPrimaryTextBrush(isDark);
		Brush secondaryText = GetCompSecondaryTextBrush(isDark);
		Brush subHeaderText = GetCompSubHeaderTextBrush(isDark);
		Brush mutedText = GetCompMutedTextBrush(isDark);
		Brush accentSoftBg = GetCompAccentSoftBgBrush(isDark);
		Brush accentSoftBorder = GetCompAccentSoftBorderBrush(isDark);
		Brush accentSoftText = GetCompAccentSoftTextBrush(isDark);

		Brush cardBg = CreateCompSurfaceBrush(LiquidGlassVariant.Regular, isDark, Math.Clamp(0.88 / 0.90, 0.72, 1.05));
		Brush rimBrush = CreateCompRimBrush(isDark);
		Brush softBg = GetCompSoftSurfaceBrush(isDark);
		Brush meterTrackBg = GetCompMeterTrackBrush(isDark);

		if (_compTitleIconBorder != null) _compTitleIconBorder.Background = accentSoftBg;
		if (_compTitleIcon != null) _compTitleIcon.Foreground = accentSoftText;

		foreach (var card in _compCardBorders)
		{
			card.Background = cardBg;
			card.BorderBrush = rimBrush;
		}
		foreach (var track in _compMeterTracks)
		{
			track.Background = meterTrackBg;
		}

		foreach (var tb in _compPrimaryTextBlocks) tb.Foreground = primaryText;
		foreach (var tb in _compSecondaryTextBlocks) tb.Foreground = secondaryText;
		foreach (var tb in _compMutedTextBlocks) tb.Foreground = mutedText;

		if (_compModeBorder != null)
		{
			_compModeBorder.Background = softBg;
			_compModeBorder.BorderBrush = rimBrush;
		}
		if (_compDetBorder != null)
		{
			_compDetBorder.Background = softBg;
			_compDetBorder.BorderBrush = rimBrush;
		}

		if (_compCurveRatioPill != null)
		{
			_compCurveRatioPill.Background = accentSoftBg;
			_compCurveRatioPill.BorderBrush = accentSoftBorder;
		}
		if (_compCurveRatioTb != null) _compCurveRatioTb.Foreground = accentSoftText;

		if (_compThresholdTag != null)
		{
			_compThresholdTag.Background = accentSoftBg;
			_compThresholdTag.BorderBrush = accentSoftBorder;
		}
		if (_compThresholdTagTb != null) _compThresholdTagTb.Foreground = accentSoftText;

		if (_toggleSidechainHp != null) _toggleSidechainHp.Foreground = primaryText;
		if (_toggleAutoRelease != null) _toggleAutoRelease.Foreground = primaryText;

		ApplyCompNeutralButtonSurface(_compResetBtn, isDark, secondaryText, _compResetIcon, _compResetText, subHeaderText);
		UpdatePowerButtonVisuals(isDark);
		UpdateModePillVisuals(isDark);
		UpdateDetectorPillVisuals(isDark);
		ApplyDirtyStateVisuals(isDark, _compIsDirty);
	}
	#endregion
}
