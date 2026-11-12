using System;
using System.Collections.Generic;
using Microsoft.UI;
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
/// FurinaPlayer 真峰值安全限幅器 (Limiter) 二级功能工作台分部实现。
/// 1:1 像素级复刻原生界面：
/// 包含电源开关/旁路切换、自动/手动上限恢复、顶端大号增益降低与微型电平条、状态指示、
/// 增益与峰值安全卡片（3项状态、4列指标、提示及处理位置折叠区域）、
/// 限幅前真峰值电平条与上限指示线、手动上限及释放滑块、衰减轨迹历史折线图及底部说明。
/// </summary>
public sealed partial class DspPage
{
	// Limiter Workbench Root
	private Border? _limiterWorkbench;

	// Parameters & States
	private bool _limiterEnabled = true;
	private double? _limiterManualCeilingDb = null; // null 表示自动上限（-1.0 dBTP 若升频开启，否则 0.0 dBTP）
	private double _limiterReleaseMs = 80.0;       // 10 ~ 1000 ms，默认 80.0 ms

	// Telemetry State
	private float _lastPeakDb = -96.0f;
	private float _lastGrDb = 0.0f;
	private int _lastOverCeilingCount = 0;
	private float _lastDspAfterPeakDb = -96.0f;
	private readonly float[] _limiterGrHistory = new float[72];
	private int _limiterGrHistoryIndex = 0;
	private DispatcherTimer? _limiterTelemetryTimer;
	private bool _limiterNavHooked = false;

	// Top Toolbar Controls
	private Button? _limiterPowerBtn;
	private FontIcon? _limiterPowerIcon;
	private TextBlock? _limiterPowerTb;
	private Button? _limiterRestoreAutoBtn;

	// Dynamics Status & Big GR Readout
	private Ellipse? _limiterActivityDot;
	private TextBlock? _limiterStatusTitleTb;
	private TextBlock? _limiterStatusDetailTb;
	private TextBlock? _limiterGrBigTb;
	private TextBlock? _limiterGrUnitTb;
	private Border? _limiterGrMiniTrack;
	private Border? _limiterGrMiniFill;
	private TextBlock? _limiterGrSubTb;

	// Gain & Peak Safety Card Controls
	private TextBlock? _limiterSafetySubtitleTb;
	private TextBlock? _limiterStateRiskTb;
	private TextBlock? _limiterStatePeakTb;
	private TextBlock? _limiterStateLimitingTb;
	private TextBlock? _limiterMetricPreTb;
	private TextBlock? _limiterMetricPostTb;
	private TextBlock? _limiterMetricGrTb;
	private TextBlock? _limiterMetricHistoryTb;
	private Button? _limiterPlacementBtn;
	private Border? _limiterPlacementPanel;

	// Pre-Limiter Peak Track & Ceiling Indicator
	private TextBlock? _limiterPrePeakValTb;
	private Grid? _limiterPeakTrackContainer;
	private Border? _limiterPeakFillBar;
	private Border? _limiterCeilingIndicator;
	private TextBlock? _limiterCeilingScaleTb;

	// Sliders & Values
	private TextBlock? _limiterManualCeilingValTb;
	private Slider? _limiterCeilingSlider;
	private TextBlock? _limiterReleaseValTb;
	private Slider? _limiterReleaseSlider;

	// GR History Plot Controls
	private TextBlock? _limiterHistoryStatusTb;
	private Canvas? _limiterHistoryCanvas;
	private Path? _limiterHistoryLinePath;
	private Path? _limiterHistoryAreaPath;
	private Line? _limiterBaselineLine;
	private Line? _limiterGrid12Line;
	private Line? _limiterGrid24Line;

	// Sidebar Nav Integration
	private TextBlock? _limiterNavSubTb;
	private FontIcon? _limiterNavCheckIcon;

	#region Public Properties & Helpers

	private bool IsSrcActive =>
		ViewModel?.AudioEngine?.DspChain?.Limiter?.SrcActive == true ||
		(ViewModel?.FurinaSrcMode is { } mode && mode != "off");

	private double EffectiveCeilingDb =>
		_limiterManualCeilingDb ?? (IsSrcActive ? -1.0 : 0.0);

	#endregion

	#region Workbench Initialization

	/// <summary>
	/// 构建并返回 FurinaPlayer 限幅 (Limiter) 二级工作台。
	/// </summary>
	public FrameworkElement CreateLimiterWorkbench()
	{
		if (_limiterWorkbench != null)
		{
			EnsureLimiterNavHooked();
			return _limiterWorkbench;
		}

		InitLimiterStateFromEngine();

		var rootBorder = new Border
		{
			Background = CreateWhiteAcrylicBrush(0.72, 195)
		};

		var sv = new ScrollViewer
		{
			VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
			HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
			Padding = new Thickness(28, 20, 28, 24)
		};

		var mainStack = new StackPanel
		{
			Spacing = 20
		};

		// 1. Top Toolbar (Power Switch & Restore Auto Button)
		mainStack.Children.Add(CreateLimiterToolbar());

		// 2. Dynamics Status Headline & Big GR Readout
		mainStack.Children.Add(CreateLimiterDynamicsStatusSection());

		// 3. Gain and Peak Safety Card ("增益与峰值安全" 卡片)
		mainStack.Children.Add(CreateLimiterGainSafetyCard());

		// 4. Pre-Limiter True Peak Meter ("限幅前真峰值")
		mainStack.Children.Add(CreatePreLimiterPeakSection());

		// 5. Parameter Sliders ("参数控制区")
		mainStack.Children.Add(CreateLimiterParameterControlsSection());

		// 6. Gain Reduction History Plot ("衰减轨迹")
		mainStack.Children.Add(CreateDecayTrajectorySection());

		// 7. Bottom Note ("底部提示")
		mainStack.Children.Add(CreateLimiterBottomNoteSection());

		sv.Content = mainStack;
		rootBorder.Child = sv;
		_limiterWorkbench = rootBorder;

		// Initialize Visuals
		UpdateLimiterPowerButtonVisuals();
		UpdateLimiterCeilingVisuals();
		UpdateLimiterReleaseVisuals();
		UpdateLimiterStatusVisuals();
		UpdateLimiterGainReductionVisuals(0.0f);
		UpdateLimiterPeakTrackVisuals(-96.0f);
		UpdateLimiterHistoryPlotVisuals();
		UpdateLimiterSidebarStatus();

		// Hook Sidebar Nav Button for smooth switching
		EnsureLimiterNavHooked();

		// Start Real-time Telemetry Timer (40ms polling)
		StartLimiterTelemetryTimer();

		return _limiterWorkbench;
	}

	private void InitLimiterStateFromEngine()
	{
		try
		{
			if (ViewModel?.AudioEngine?.DspChain?.Limiter is { } limiter)
			{
				_limiterEnabled = limiter.Enabled;
				_limiterManualCeilingDb = limiter.AutoCeiling ? null : (double)limiter.ManualCeilingDb;
				_limiterReleaseMs = limiter.ReleaseMs;
			}
		}
		catch
		{
			_limiterEnabled = true;
			_limiterManualCeilingDb = null;
			_limiterReleaseMs = 80.0;
		}
	}

	#endregion

	#region 1. Top Toolbar

	private FrameworkElement CreateLimiterToolbar()
	{
		var toolbarGrid = new Grid();
		toolbarGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.0, GridUnitType.Star) });

		var actionsSp = new StackPanel
		{
			Orientation = Orientation.Horizontal,
			Spacing = 10,
			VerticalAlignment = VerticalAlignment.Center
		};

		// 1. Power Switch Button (限幅开启 / 限幅旁路)
		_limiterPowerBtn = new Button
		{
			CornerRadius = new CornerRadius(16),
			Padding = new Thickness(14, 6, 16, 6)
		};

		var powerSp = new StackPanel
		{
			Orientation = Orientation.Horizontal,
			Spacing = 7,
			VerticalAlignment = VerticalAlignment.Center
		};

		_limiterPowerIcon = new FontIcon
		{
			Glyph = "\uE7E8",
			FontSize = 14
		};
		powerSp.Children.Add(_limiterPowerIcon);

		_limiterPowerTb = new TextBlock
		{
			FontSize = 13,
			FontWeight = Microsoft.UI.Text.FontWeights.SemiBold
		};
		powerSp.Children.Add(_limiterPowerTb);

		_limiterPowerBtn.Content = powerSp;
		_limiterPowerBtn.Click += (s, e) =>
		{
			_limiterEnabled = !_limiterEnabled;
			UpdateLimiterPowerButtonVisuals();
			UpdateLimiterStatusVisuals();
			UpdateLimiterSidebarStatus();
			ApplyLimiterSettings();
		};
		actionsSp.Children.Add(_limiterPowerBtn);

		// 2. Restore Automatic Ceiling Button (恢复自动上限)
		_limiterRestoreAutoBtn = new Button
		{
			CornerRadius = new CornerRadius(16),
			Padding = new Thickness(14, 6, 16, 6),
			Background = DspSoftSurfaceBrush,
			BorderBrush = CreateLiquidGlassRimBrush(),
			BorderThickness = new Thickness(1),
			IsEnabled = _limiterManualCeilingDb != null
		};

		var restoreSp = new StackPanel
		{
			Orientation = Orientation.Horizontal,
			Spacing = 7,
			VerticalAlignment = VerticalAlignment.Center
		};

		restoreSp.Children.Add(new FontIcon
		{
			Glyph = "\uE7A7",
			FontSize = 13,
			Foreground = DspTextMutedBrush
		});

		restoreSp.Children.Add(new TextBlock
		{
			Text = "恢复自动上限",
			FontSize = 13,
			FontWeight = Microsoft.UI.Text.FontWeights.Normal,
			Foreground = DspTextSecondaryBrush
		});

		_limiterRestoreAutoBtn.Content = restoreSp;
		_limiterRestoreAutoBtn.Click += (s, e) =>
		{
			_limiterManualCeilingDb = null;
			if (_limiterCeilingSlider != null)
			{
				_limiterCeilingSlider.Value = IsSrcActive ? -1.0 : 0.0;
			}
			UpdateLimiterCeilingVisuals();
			UpdateLimiterStatusVisuals();
			UpdateLimiterSidebarStatus();
			ApplyLimiterSettings();
		};
		actionsSp.Children.Add(_limiterRestoreAutoBtn);

		toolbarGrid.Children.Add(actionsSp);
		return toolbarGrid;
	}

	private void UpdateLimiterPowerButtonVisuals()
	{
		if (_limiterPowerBtn == null || _limiterPowerIcon == null || _limiterPowerTb == null) return;

		if (_limiterEnabled)
		{
			_limiterPowerBtn.Background = DspAccentSoftBgBrush; // Indigo 50
			_limiterPowerBtn.BorderBrush = DspAccentSoftBorderBrush; // Indigo 200
			_limiterPowerBtn.BorderThickness = new Thickness(1);
			_limiterPowerIcon.Foreground = DspAccentSoftTextBrush;  // Indigo 600
			_limiterPowerTb.Foreground = DspAccentSoftTextBrush;
			_limiterPowerTb.Text = "限幅开启";
		}
		else
		{
			_limiterPowerBtn.Background = DspSoftSurfaceBrush; // Slate 100
			_limiterPowerBtn.BorderBrush = CreateLiquidGlassRimBrush(); // Slate 200
			_limiterPowerBtn.BorderThickness = new Thickness(1);
			_limiterPowerIcon.Foreground = DspTextMutedBrush; // Slate 500
			_limiterPowerTb.Foreground = DspTextMutedBrush;
			_limiterPowerTb.Text = "限幅旁路";
		}

		if (_limiterCeilingSlider != null) _limiterCeilingSlider.IsEnabled = _limiterEnabled;
		if (_limiterReleaseSlider != null) _limiterReleaseSlider.IsEnabled = _limiterEnabled;
	}

	#endregion

	#region 2. Dynamics Status & Big GR Readout

	private FrameworkElement CreateLimiterDynamicsStatusSection()
	{
		var grid = new Grid
		{
			Margin = new Thickness(0, 4, 0, 0)
		};
		grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.0, GridUnitType.Star) });
		grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

		// Left Column: Dot + Headline + Detailed Subtitle
		var leftSp = new StackPanel
		{
			Orientation = Orientation.Horizontal,
			Spacing = 12,
			VerticalAlignment = VerticalAlignment.Center
		};

		_limiterActivityDot = new Ellipse
		{
			Width = 8,
			Height = 8,
			Fill = new SolidColorBrush(Color.FromArgb(255, 59, 130, 246)),
			Margin = new Thickness(0, 7, 0, 0),
			VerticalAlignment = VerticalAlignment.Top
		};
		leftSp.Children.Add(_limiterActivityDot);

		var textSp = new StackPanel
		{
			Spacing = 4,
			VerticalAlignment = VerticalAlignment.Center
		};

		_limiterStatusTitleTb = new TextBlock
		{
			Text = "已接入，等待播放开始",
			FontSize = 18,
			FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
			Foreground = DspTextPrimaryBrush
		};
		textSp.Children.Add(_limiterStatusTitleTb);

		_limiterStatusDetailTb = new TextBlock
		{
			Text = "当前上限 0.0 dBTP，自动。开始播放后，真峰值顶到这条线时，增益降低会离开 0。",
			FontSize = 13,
			Foreground = DspTextMutedBrush,
			TextWrapping = TextWrapping.Wrap
		};
		textSp.Children.Add(_limiterStatusDetailTb);

		leftSp.Children.Add(textSp);
		Grid.SetColumn(leftSp, 0);
		grid.Children.Add(leftSp);

		// Right Column: Big GR Readout
		var rightSp = new StackPanel
		{
			HorizontalAlignment = HorizontalAlignment.Right,
			VerticalAlignment = VerticalAlignment.Center,
			Spacing = 3
		};

		var numberSp = new StackPanel
		{
			Orientation = Orientation.Horizontal,
			HorizontalAlignment = HorizontalAlignment.Right
		};

		_limiterGrBigTb = new TextBlock
		{
			Text = "0.0",
			FontSize = 42,
			FontWeight = Microsoft.UI.Text.FontWeights.Bold,
			Foreground = DspTextPrimaryBrush,
			VerticalAlignment = VerticalAlignment.Bottom
		};
		numberSp.Children.Add(_limiterGrBigTb);

		_limiterGrUnitTb = new TextBlock
		{
			Text = " dB",
			FontSize = 18,
			FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
			Foreground = DspTextPrimaryBrush,
			VerticalAlignment = VerticalAlignment.Bottom,
			Margin = new Thickness(2, 0, 0, 6)
		};
		numberSp.Children.Add(_limiterGrUnitTb);
		rightSp.Children.Add(numberSp);

		// Mini GR Bar (140px track)
		_limiterGrMiniTrack = new Border
		{
			Width = 140,
			Height = 4,
			CornerRadius = new CornerRadius(2),
			Background = new SolidColorBrush(Color.FromArgb(255, 226, 232, 240)),
			HorizontalAlignment = HorizontalAlignment.Right
		};

		_limiterGrMiniFill = new Border
		{
			Width = 0,
			Height = 4,
			CornerRadius = new CornerRadius(2),
			Background = new SolidColorBrush(Color.FromArgb(255, 239, 68, 68)),
			HorizontalAlignment = HorizontalAlignment.Left
		};
		_limiterGrMiniTrack.Child = _limiterGrMiniFill;
		rightSp.Children.Add(_limiterGrMiniTrack);

		_limiterGrSubTb = new TextBlock
		{
			Text = "增益降低 当前没有限幅",
			FontSize = 11,
			Foreground = DspTextMutedBrush,
			HorizontalAlignment = HorizontalAlignment.Right,
			Margin = new Thickness(0, 2, 0, 0)
		};
		rightSp.Children.Add(_limiterGrSubTb);

		Grid.SetColumn(rightSp, 1);
		grid.Children.Add(rightSp);

		return grid;
	}

	private void UpdateLimiterStatusVisuals()
	{
		if (_limiterStatusTitleTb == null || _limiterStatusDetailTb == null || _limiterActivityDot == null) return;

		double ceiling = EffectiveCeilingDb;
		string ceilingDesc = $"{ceiling:0.0} dBTP，{(_limiterManualCeilingDb == null ? "自动" : "手动")}";

		if (!_limiterEnabled)
		{
			_limiterStatusTitleTb.Text = "未开启";
			_limiterStatusDetailTb.Text = "开启后真峰值将受到上限压制，防止 DAC 间际过载与削波。";
			_limiterActivityDot.Fill = DspTextMutedBrush; // Slate 400
		}
		else if (_lastGrDb >= 0.1f)
		{
			_limiterStatusTitleTb.Text = "正在限幅";
			_limiterStatusDetailTb.Text = $"增益正在衰减。上限 {ceiling:0.0} dBTP，当前峰值已顶到或越过此边界。";
			_limiterActivityDot.Fill = new SolidColorBrush(Color.FromArgb(255, 239, 68, 68)); // Red 500
		}
		else if (_lastPeakDb > -80.0f)
		{
			_limiterStatusTitleTb.Text = "已接入，上限未触发";
			_limiterStatusDetailTb.Text = $"真峰值 {_lastPeakDb:0.0} dBTP，上限 {ceiling:0.0} dBTP，尚未触顶。";
			_limiterActivityDot.Fill = new SolidColorBrush(Color.FromArgb(255, 16, 185, 129)); // Emerald 500
		}
		else
		{
			_limiterStatusTitleTb.Text = "已接入，等待播放开始";
			_limiterStatusDetailTb.Text = $"当前上限 {ceilingDesc}。开始播放后，真峰值顶到这条线时，增益降低会离开 0。";
			_limiterActivityDot.Fill = new SolidColorBrush(Color.FromArgb(255, 59, 130, 246)); // Blue 500
		}
	}

	private void UpdateLimiterGainReductionVisuals(float grDb)
	{
		if (_limiterGrBigTb == null || _limiterGrSubTb == null || _limiterGrMiniFill == null) return;

		if (grDb < 0.05f)
		{
			_limiterGrBigTb.Text = "0.0";
			_limiterGrSubTb.Text = "增益降低 当前没有限幅";
			_limiterGrMiniFill.Width = 0;
		}
		else
		{
			_limiterGrBigTb.Text = $"-{grDb:0.0}";
			_limiterGrSubTb.Text = "增益降低 正在限幅";
			double w = Math.Clamp(grDb / 12.0, 0.0, 1.0) * 140.0;
			_limiterGrMiniFill.Width = w;
		}
	}

	#endregion

	#region 3. Gain and Peak Safety Card ("增益与峰值安全" 卡片)

	private FrameworkElement CreateLimiterGainSafetyCard()
	{
		var card = new Border
		{
			Background = CreateAcrylicCardBrush(0.88, 235),
			BorderBrush = CreateLiquidGlassRimBrush(),
			BorderThickness = new Thickness(1),
			CornerRadius = new CornerRadius(12),
			Padding = new Thickness(18, 14, 18, 14)
		};

		var stack = new StackPanel { Spacing = 10 };

		// Card Header: Title + Subtitle
		var titleSp = new StackPanel
		{
			Orientation = Orientation.Horizontal,
			Spacing = 12,
			VerticalAlignment = VerticalAlignment.Center
		};

		titleSp.Children.Add(new TextBlock
		{
			Text = "增益与峰值安全",
			FontSize = 14,
			FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
			Foreground = DspTextPrimaryBrush
		});

		_limiterSafetySubtitleTb = new TextBlock
		{
			Text = "等待当前宿主读数",
			FontSize = 12,
			Foreground = DspTextMutedBrush,
			VerticalAlignment = VerticalAlignment.Center
		};
		titleSp.Children.Add(_limiterSafetySubtitleTb);
		stack.Children.Add(titleSp);

		// States Row (3 items)
		var statesGrid = new Grid { ColumnSpacing = 16 };
		statesGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.0, GridUnitType.Star) });
		statesGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.0, GridUnitType.Star) });
		statesGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.0, GridUnitType.Star) });

		_limiterStateRiskTb = new TextBlock
		{
			Text = "配置风险估计: 未报告",
			FontSize = 12,
			Foreground = DspTextSubHeaderBrush
		};
		Grid.SetColumn(_limiterStateRiskTb, 0);
		statesGrid.Children.Add(_limiterStateRiskTb);

		_limiterStatePeakTb = new TextBlock
		{
			Text = "实测峰值超限: 未知",
			FontSize = 12,
			Foreground = DspTextSubHeaderBrush
		};
		Grid.SetColumn(_limiterStatePeakTb, 1);
		statesGrid.Children.Add(_limiterStatePeakTb);

		_limiterStateLimitingTb = new TextBlock
		{
			Text = "当前限幅: 未知",
			FontSize = 12,
			Foreground = DspTextSubHeaderBrush
		};
		Grid.SetColumn(_limiterStateLimitingTb, 2);
		statesGrid.Children.Add(_limiterStateLimitingTb);

		stack.Children.Add(statesGrid);

		// 4-Column Metric Grid
		var metricsGrid = new Grid
		{
			ColumnSpacing = 16,
			Margin = new Thickness(0, 4, 0, 4)
		};
		metricsGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.0, GridUnitType.Star) });
		metricsGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.0, GridUnitType.Star) });
		metricsGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.0, GridUnitType.Star) });
		metricsGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.0, GridUnitType.Star) });

		var m1 = CreateLimiterMetricBox("限幅前", "未知", out _limiterMetricPreTb);
		var m2 = CreateLimiterMetricBox("DSP 后", "未知", out _limiterMetricPostTb);
		var m3 = CreateLimiterMetricBox("Limiter 衰减", "未知", out _limiterMetricGrTb);
		var m4 = CreateLimiterMetricBox("历史输出超限次数", "未知", out _limiterMetricHistoryTb);

		Grid.SetColumn(m1, 0); metricsGrid.Children.Add(m1);
		Grid.SetColumn(m2, 1); metricsGrid.Children.Add(m2);
		Grid.SetColumn(m3, 2); metricsGrid.Children.Add(m3);
		Grid.SetColumn(m4, 3); metricsGrid.Children.Add(m4);
		stack.Children.Add(metricsGrid);

		// Notice Note
		stack.Children.Add(new TextBlock
		{
			Text = "限幅前超过 0 dBTP 不等于输出已被削波。历史次数不代表当前仍在削波；读数只覆盖所报告的阶段。",
			FontSize = 11,
			Foreground = DspTextMutedBrush,
			TextWrapping = TextWrapping.Wrap
		});

		// Collapsible Details (处理位置与可见边界)
		_limiterPlacementBtn = new Button
		{
			Content = "▶ 处理位置与可见边界",
			Background = new SolidColorBrush(Colors.Transparent),
			BorderThickness = new Thickness(0),
			Padding = new Thickness(0, 4, 0, 4),
			FontSize = 12,
			Foreground = DspAccentSoftTextBrush,
			FontWeight = Microsoft.UI.Text.FontWeights.Medium
		};

		_limiterPlacementPanel = new Border
		{
			Visibility = Visibility.Collapsed,
			Background = DspSoftSurfaceBrush,
			BorderBrush = CreateLiquidGlassRimBrush(),
			BorderThickness = new Thickness(1),
			CornerRadius = new CornerRadius(8),
			Padding = new Thickness(12, 10, 12, 10),
			Child = new TextBlock
			{
				Text = "当前 native 链路在 Rack 后应用 Headroom，再经过 Limiter、变速和输出表。EQ/FIR 保留浮点超限样本；非线性效果仍可能改变信号。",
				FontSize = 12,
				Foreground = DspTextSecondaryBrush,
				TextWrapping = TextWrapping.Wrap,
				LineHeight = 18
			}
		};

		_limiterPlacementBtn.Click += (s, e) =>
		{
			if (_limiterPlacementPanel.Visibility == Visibility.Collapsed)
			{
				_limiterPlacementPanel.Visibility = Visibility.Visible;
				_limiterPlacementBtn.Content = "▼ 处理位置与可见边界";
			}
			else
			{
				_limiterPlacementPanel.Visibility = Visibility.Collapsed;
				_limiterPlacementBtn.Content = "▶ 处理位置与可见边界";
			}
		};

		stack.Children.Add(_limiterPlacementBtn);
		stack.Children.Add(_limiterPlacementPanel);

		card.Child = stack;
		return card;
	}

	private StackPanel CreateLimiterMetricBox(string label, string initialVal, out TextBlock valTb)
	{
		var box = new StackPanel { Spacing = 2 };
		box.Children.Add(new TextBlock
		{
			Text = label,
			FontSize = 11,
			Foreground = DspTextMutedBrush
		});

		valTb = new TextBlock
		{
			Text = initialVal,
			FontSize = 13,
			FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
			Foreground = DspTextPrimaryBrush
		};
		box.Children.Add(valTb);
		return box;
	}

	#endregion

	#region 4. Pre-Limiter Peak Meter ("限幅前真峰值")

	private FrameworkElement CreatePreLimiterPeakSection()
	{
		var stack = new StackPanel { Spacing = 6, Margin = new Thickness(0, 4, 0, 0) };

		// Header: Title + Right Readout
		var headerGrid = new Grid();
		headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.0, GridUnitType.Star) });
		headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

		var titleTb = new TextBlock
		{
			Text = "限幅前真峰值",
			FontSize = 14,
			FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
			Foreground = DspTextPrimaryBrush,
			VerticalAlignment = VerticalAlignment.Center
		};
		Grid.SetColumn(titleTb, 0);
		headerGrid.Children.Add(titleTb);

		_limiterPrePeakValTb = new TextBlock
		{
			Text = "等待读数",
			FontSize = 13,
			Foreground = DspTextMutedBrush,
			VerticalAlignment = VerticalAlignment.Center
		};
		Grid.SetColumn(_limiterPrePeakValTb, 1);
		headerGrid.Children.Add(_limiterPrePeakValTb);
		stack.Children.Add(headerGrid);

		// Track Container (Background Track + Fill Bar + Ceiling Marker)
		_limiterPeakTrackContainer = new Grid
		{
			Height = 8,
			Background = new SolidColorBrush(Color.FromArgb(255, 226, 232, 240)), // Slate 200
			CornerRadius = new CornerRadius(4),
			HorizontalAlignment = HorizontalAlignment.Stretch
		};

		_limiterPeakFillBar = new Border
		{
			Width = 0,
			Height = 8,
			CornerRadius = new CornerRadius(4),
			Background = new SolidColorBrush(Color.FromArgb(255, 59, 130, 246)), // Blue 500
			HorizontalAlignment = HorizontalAlignment.Left
		};
		_limiterPeakTrackContainer.Children.Add(_limiterPeakFillBar);

		_limiterCeilingIndicator = new Border
		{
			Width = 2.5,
			Height = 14,
			CornerRadius = new CornerRadius(1.25),
			Background = new SolidColorBrush(Color.FromArgb(255, 37, 99, 235)), // Blue 600
			HorizontalAlignment = HorizontalAlignment.Left,
			VerticalAlignment = VerticalAlignment.Center
		};
		_limiterPeakTrackContainer.Children.Add(_limiterCeilingIndicator);

		_limiterPeakTrackContainer.SizeChanged += (s, e) =>
		{
			UpdateLimiterPeakTrackVisuals(_lastPeakDb);
		};

		stack.Children.Add(_limiterPeakTrackContainer);

		// Scale Row (-12, 上限 0.0 dBTP, 0)
		var scaleGrid = new Grid();
		scaleGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
		scaleGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.0, GridUnitType.Star) });
		scaleGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

		var leftScaleTb = new TextBlock
		{
			Text = "-12",
			FontSize = 11,
			Foreground = DspTextMutedBrush
		};
		Grid.SetColumn(leftScaleTb, 0);
		scaleGrid.Children.Add(leftScaleTb);

		_limiterCeilingScaleTb = new TextBlock
		{
			Text = "上限 0.0 dBTP",
			FontSize = 11,
			Foreground = DspTextMutedBrush,
			HorizontalAlignment = HorizontalAlignment.Center
		};
		Grid.SetColumn(_limiterCeilingScaleTb, 1);
		scaleGrid.Children.Add(_limiterCeilingScaleTb);

		var rightScaleTb = new TextBlock
		{
			Text = "0",
			FontSize = 11,
			Foreground = DspTextMutedBrush,
			HorizontalAlignment = HorizontalAlignment.Right
		};
		Grid.SetColumn(rightScaleTb, 2);
		scaleGrid.Children.Add(rightScaleTb);

		stack.Children.Add(scaleGrid);
		return stack;
	}

	private void UpdateLimiterPeakTrackVisuals(float peakDb)
	{
		if (_limiterPeakFillBar == null || _limiterCeilingIndicator == null || _limiterPrePeakValTb == null) return;

		double trackW = _limiterPeakTrackContainer?.ActualWidth ?? 600.0;
		if (trackW < 50.0) trackW = 600.0;

		if (peakDb <= -90.0f)
		{
			_limiterPrePeakValTb.Text = "等待读数";
			_limiterPeakFillBar.Width = 0;
		}
		else
		{
			_limiterPrePeakValTb.Text = $"{peakDb:0.0} dBTP";
			double norm = Math.Clamp((peakDb - (-12.0)) / 12.0, 0.0, 1.0);
			_limiterPeakFillBar.Width = norm * trackW;

			if (peakDb > EffectiveCeilingDb + 0.01)
			{
				_limiterPeakFillBar.Background = new SolidColorBrush(Color.FromArgb(255, 239, 68, 68)); // Red
			}
			else
			{
				_limiterPeakFillBar.Background = new SolidColorBrush(Color.FromArgb(255, 59, 130, 246)); // Blue
			}
		}

		UpdateLimiterCeilingMarkerPosition();
	}

	private void UpdateLimiterCeilingMarkerPosition()
	{
		if (_limiterCeilingIndicator == null || _limiterCeilingScaleTb == null) return;

		double ceiling = EffectiveCeilingDb;
		_limiterCeilingScaleTb.Text = $"上限 {ceiling:0.0} dBTP";

		double trackW = _limiterPeakTrackContainer?.ActualWidth ?? 600.0;
		if (trackW < 50.0) trackW = 600.0;

		double normCeiling = Math.Clamp((ceiling - (-12.0)) / 12.0, 0.0, 1.0);
		double markerX = normCeiling * trackW;

		_limiterCeilingIndicator.Margin = new Thickness(Math.Clamp(markerX - 1.25, 0.0, trackW - 2.5), 0, 0, 0);
	}

	#endregion

	#region 5. Parameter Controls Section ("参数控制区")

	private FrameworkElement CreateLimiterParameterControlsSection()
	{
		var stack = new StackPanel
		{
			Spacing = 16,
			Margin = new Thickness(0, 8, 0, 0)
		};

		// 1. Manual Ceiling Slider (手动上限)
		var ceilingControlStack = new StackPanel { Spacing = 4 };
		var ceilingHeaderGrid = new Grid();
		ceilingHeaderGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.0, GridUnitType.Star) });
		ceilingHeaderGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

		var ceilingLabelTb = new TextBlock
		{
			Text = "手动上限",
			FontSize = 14,
			FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
			Foreground = DspTextPrimaryBrush
		};
		Grid.SetColumn(ceilingLabelTb, 0);
		ceilingHeaderGrid.Children.Add(ceilingLabelTb);

		_limiterManualCeilingValTb = new TextBlock
		{
			Text = _limiterManualCeilingDb == null ? "自动" : $"{_limiterManualCeilingDb.Value:0.0} dBTP",
			FontSize = 13,
			Foreground = DspTextSubHeaderBrush
		};
		Grid.SetColumn(_limiterManualCeilingValTb, 1);
		ceilingHeaderGrid.Children.Add(_limiterManualCeilingValTb);
		ceilingControlStack.Children.Add(ceilingHeaderGrid);

		_limiterCeilingSlider = new Slider
		{
			Minimum = -12.0,
			Maximum = 0.0,
			StepFrequency = 0.1,
			Value = _limiterManualCeilingDb ?? (IsSrcActive ? -1.0 : 0.0),
			HorizontalAlignment = HorizontalAlignment.Stretch,
			IsEnabled = _limiterEnabled
		};
		ToolTipService.SetToolTip(_limiterCeilingSlider, "双击恢复自动上限");

		_limiterCeilingSlider.ValueChanged += (s, e) =>
		{
			_limiterManualCeilingDb = Math.Round(e.NewValue, 1);
			UpdateLimiterCeilingVisuals();
			UpdateLimiterStatusVisuals();
			UpdateLimiterSidebarStatus();
			ApplyLimiterSettings();
		};

		_limiterCeilingSlider.DoubleTapped += (s, e) =>
		{
			_limiterManualCeilingDb = null;
			_limiterCeilingSlider.Value = IsSrcActive ? -1.0 : 0.0;
			UpdateLimiterCeilingVisuals();
			UpdateLimiterStatusVisuals();
			UpdateLimiterSidebarStatus();
			ApplyLimiterSettings();
		};

		ceilingControlStack.Children.Add(_limiterCeilingSlider);
		stack.Children.Add(ceilingControlStack);

		// 2. Release Slider (释放)
		var releaseControlStack = new StackPanel { Spacing = 4 };
		var releaseHeaderGrid = new Grid();
		releaseHeaderGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.0, GridUnitType.Star) });
		releaseHeaderGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

		var releaseLabelTb = new TextBlock
		{
			Text = "释放",
			FontSize = 14,
			FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
			Foreground = DspTextPrimaryBrush
		};
		Grid.SetColumn(releaseLabelTb, 0);
		releaseHeaderGrid.Children.Add(releaseLabelTb);

		_limiterReleaseValTb = new TextBlock
		{
			Text = $"{Math.Round(_limiterReleaseMs)} ms",
			FontSize = 13,
			Foreground = DspTextSubHeaderBrush
		};
		Grid.SetColumn(_limiterReleaseValTb, 1);
		releaseHeaderGrid.Children.Add(_limiterReleaseValTb);
		releaseControlStack.Children.Add(releaseHeaderGrid);

		_limiterReleaseSlider = new Slider
		{
			Minimum = 10.0,
			Maximum = 1000.0,
			StepFrequency = 1.0,
			Value = _limiterReleaseMs,
			HorizontalAlignment = HorizontalAlignment.Stretch,
			IsEnabled = _limiterEnabled
		};
		ToolTipService.SetToolTip(_limiterReleaseSlider, "双击恢复 80 ms");

		_limiterReleaseSlider.ValueChanged += (s, e) =>
		{
			_limiterReleaseMs = Math.Round(e.NewValue);
			UpdateLimiterReleaseVisuals();
			ApplyLimiterSettings();
		};

		_limiterReleaseSlider.DoubleTapped += (s, e) =>
		{
			_limiterReleaseMs = 80.0;
			_limiterReleaseSlider.Value = 80.0;
			UpdateLimiterReleaseVisuals();
			ApplyLimiterSettings();
		};

		releaseControlStack.Children.Add(_limiterReleaseSlider);
		stack.Children.Add(releaseControlStack);

		return stack;
	}

	private void UpdateLimiterCeilingVisuals()
	{
		if (_limiterManualCeilingValTb != null)
		{
			_limiterManualCeilingValTb.Text = _limiterManualCeilingDb == null
				? "自动"
				: $"{_limiterManualCeilingDb.Value:0.0} dBTP";
		}

		if (_limiterRestoreAutoBtn != null)
		{
			_limiterRestoreAutoBtn.IsEnabled = _limiterManualCeilingDb != null;
		}

		UpdateLimiterCeilingMarkerPosition();
	}

	private void UpdateLimiterReleaseVisuals()
	{
		if (_limiterReleaseValTb != null)
		{
			_limiterReleaseValTb.Text = $"{Math.Round(_limiterReleaseMs)} ms";
		}
	}

	#endregion

	#region 6. Decay Trajectory Plot ("衰减轨迹")

	private FrameworkElement CreateDecayTrajectorySection()
	{
		var stack = new StackPanel { Spacing = 6, Margin = new Thickness(0, 8, 0, 0) };

		// Header: Title + Status
		var headerGrid = new Grid();
		headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.0, GridUnitType.Star) });
		headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

		var titleTb = new TextBlock
		{
			Text = "衰减轨迹",
			FontSize = 14,
			FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
			Foreground = DspTextPrimaryBrush,
			VerticalAlignment = VerticalAlignment.Center
		};
		Grid.SetColumn(titleTb, 0);
		headerGrid.Children.Add(titleTb);

		_limiterHistoryStatusTb = new TextBlock
		{
			Text = "播放后开始记录",
			FontSize = 13,
			Foreground = DspTextMutedBrush,
			VerticalAlignment = VerticalAlignment.Center
		};
		Grid.SetColumn(_limiterHistoryStatusTb, 1);
		headerGrid.Children.Add(_limiterHistoryStatusTb);
		stack.Children.Add(headerGrid);

		// Canvas Plot (Height 110px)
		_limiterHistoryCanvas = new Canvas
		{
			Height = 110.0,
			HorizontalAlignment = HorizontalAlignment.Stretch,
			Clip = new RectangleGeometry { Rect = new Rect(0, 0, 2000, 110) }
		};

		// 3 Horizontal Grid Lines (-24 at Y=16, -12 at Y=56, 0 at Y=96)
		_limiterGrid24Line = new Line
		{
			X1 = 40.0,
			Y1 = 16.0,
			X2 = 1200.0,
			Y2 = 16.0,
			Stroke = new SolidColorBrush(Color.FromArgb(255, 241, 245, 249)),
			StrokeThickness = 1.0
		};
		_limiterHistoryCanvas.Children.Add(_limiterGrid24Line);

		_limiterGrid12Line = new Line
		{
			X1 = 40.0,
			Y1 = 56.0,
			X2 = 1200.0,
			Y2 = 56.0,
			Stroke = new SolidColorBrush(Color.FromArgb(255, 226, 232, 240)),
			StrokeThickness = 1.0
		};
		_limiterHistoryCanvas.Children.Add(_limiterGrid12Line);

		// Baseline line (solid blue at 0 dB, Y=96.0)
		_limiterBaselineLine = new Line
		{
			X1 = 40.0,
			Y1 = 96.0,
			X2 = 1200.0,
			Y2 = 96.0,
			Stroke = new SolidColorBrush(Color.FromArgb(255, 59, 130, 246)),
			StrokeThickness = 1.5
		};
		_limiterHistoryCanvas.Children.Add(_limiterBaselineLine);

		// Y-Axis Labels
		var lbl24 = new TextBlock
		{
			Text = "-24",
			FontSize = 11,
			Foreground = DspTextMutedBrush
		};
		Canvas.SetLeft(lbl24, 8);
		Canvas.SetTop(lbl24, 8);
		_limiterHistoryCanvas.Children.Add(lbl24);

		var lbl12 = new TextBlock
		{
			Text = "-12",
			FontSize = 11,
			Foreground = DspTextMutedBrush
		};
		Canvas.SetLeft(lbl12, 8);
		Canvas.SetTop(lbl12, 48);
		_limiterHistoryCanvas.Children.Add(lbl12);

		var lbl0 = new TextBlock
		{
			Text = "0",
			FontSize = 11,
			Foreground = DspTextMutedBrush
		};
		Canvas.SetLeft(lbl0, 16);
		Canvas.SetTop(lbl0, 88);
		_limiterHistoryCanvas.Children.Add(lbl0);

		// History Area & Line Paths
		_limiterHistoryAreaPath = new Path
		{
			Fill = new SolidColorBrush(Color.FromArgb(32, 59, 130, 246)) // Translucent Blue
		};
		_limiterHistoryCanvas.Children.Add(_limiterHistoryAreaPath);

		_limiterHistoryLinePath = new Path
		{
			Stroke = new SolidColorBrush(Color.FromArgb(255, 59, 130, 246)),
			StrokeThickness = 2.0,
			StrokeLineJoin = PenLineJoin.Round
		};
		_limiterHistoryCanvas.Children.Add(_limiterHistoryLinePath);

		_limiterHistoryCanvas.SizeChanged += (s, e) =>
		{
			double w = e.NewSize.Width;
			if (w > 50.0)
			{
				if (_limiterGrid24Line != null) _limiterGrid24Line.X2 = w;
				if (_limiterGrid12Line != null) _limiterGrid12Line.X2 = w;
				if (_limiterBaselineLine != null) _limiterBaselineLine.X2 = w;
				if (_limiterHistoryCanvas.Clip is RectangleGeometry rg)
				{
					rg.Rect = new Rect(0, 0, w, 110.0);
				}
				UpdateLimiterHistoryPlotVisuals();
			}
		};

		stack.Children.Add(_limiterHistoryCanvas);
		return stack;
	}

	private static double YForLimiterGr(double grDb)
	{
		double norm = Math.Clamp(grDb / 24.0, 0.0, 1.0);
		return 96.0 - norm * 80.0;
	}

	private void UpdateLimiterHistoryPlotVisuals()
	{
		if (_limiterHistoryLinePath == null || _limiterHistoryAreaPath == null || _limiterHistoryCanvas == null) return;

		double canvasW = _limiterHistoryCanvas.ActualWidth;
		if (canvasW < 100.0) canvasW = 640.0;

		double startX = 40.0;
		double plotW = canvasW - startX;
		if (plotW <= 10.0) return;

		double yZero = 96.0;

		var lineFig = new PathFigure { IsClosed = false, IsFilled = false };
		var areaFig = new PathFigure { IsClosed = true, IsFilled = true };

		int count = _limiterGrHistory.Length;
		for (int i = 0; i < count; i++)
		{
			int circularIdx = (_limiterGrHistoryIndex + i) % count;
			float gr = _limiterGrHistory[circularIdx];
			double x = startX + (double)i / (count - 1) * plotW;
			double y = YForLimiterGr(gr);

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

		areaFig.Segments.Add(new LineSegment { Point = new Point(canvasW, yZero) });

		var lineGeo = new PathGeometry();
		lineGeo.Figures.Add(lineFig);
		_limiterHistoryLinePath.Data = lineGeo;

		var areaGeo = new PathGeometry();
		areaGeo.Figures.Add(areaFig);
		_limiterHistoryAreaPath.Data = areaGeo;
	}

	#endregion

	#region 7. Bottom Note ("底部提示")

	private FrameworkElement CreateLimiterBottomNoteSection()
	{
		return new TextBlock
		{
			Text = "增益降低离开 0，就说明峰值已经顶到上限。释放越短，压完回得越快，默认 80 ms。未手动设置上限时，升频采样开着是 -1 dBTP，否则是 0。",
			FontSize = 12,
			Foreground = DspTextMutedBrush,
			Margin = new Thickness(0, 4, 0, 16),
			TextWrapping = TextWrapping.Wrap,
			LineHeight = 18
		};
	}

	#endregion

	#region 8. Audio DSP Integration & Telemetry

	private void ApplyLimiterSettings()
	{
		try
		{
			bool autoCeiling = !_limiterManualCeilingDb.HasValue;
			float manualCeiling = (float)(_limiterManualCeilingDb ?? 0.0);
			float releaseMs = (float)_limiterReleaseMs;

			// 1. AudioEngine API
			ViewModel?.AudioEngine?.SetFurinaLimiter(_limiterEnabled, autoCeiling, manualCeiling, releaseMs);

			// 2. Direct DSP Chain Processor Fallback
			if (ViewModel?.AudioEngine?.DspChain?.Limiter is { } limiter)
			{
				limiter.Enabled = _limiterEnabled;
				limiter.AutoCeiling = autoCeiling;
				limiter.ManualCeilingDb = manualCeiling;
				limiter.ReleaseMs = releaseMs;
			}
		}
		catch (Exception ex)
		{
			System.Diagnostics.Debug.WriteLine($"[FurinaPlayer] Error applying limiter settings: {ex.Message}");
		}
	}

	/// <summary>
	/// 供宿主或音频引擎实时刷新限幅器遥测数据（电平、衰减与超限次数）。
	/// </summary>
	/// <param name="peakDb">限幅前真峰值电平 (dBTP)</param>
	/// <param name="grDb">增益降低衰减量 (dB)</param>
	/// <param name="overCeilingCount">历史输出超限次数</param>
	public void UpdateLimiterTelemetry(float peakDb, float grDb, int overCeilingCount)
	{
		var dq = DispatcherQueue;
		if (dq != null && !dq.HasThreadAccess)
		{
			dq.TryEnqueue(() => UpdateLimiterTelemetryInternal(peakDb, grDb, overCeilingCount));
		}
		else
		{
			UpdateLimiterTelemetryInternal(peakDb, grDb, overCeilingCount);
		}
	}

	private void UpdateLimiterTelemetryInternal(float peakDb, float grDb, int overCeilingCount)
	{
		_lastPeakDb = peakDb;
		_lastGrDb = grDb;
		_lastOverCeilingCount = overCeilingCount;

		// Calculate DSP post peak
		if (peakDb > -90.0f)
		{
			float postPeak = (float)Math.Min(peakDb - grDb, EffectiveCeilingDb);
			_lastDspAfterPeakDb = postPeak;
		}
		else
		{
			_lastDspAfterPeakDb = -96.0f;
		}

		// Push to GR history buffer
		_limiterGrHistory[_limiterGrHistoryIndex] = grDb;
		_limiterGrHistoryIndex = (_limiterGrHistoryIndex + 1) % _limiterGrHistory.Length;

		// Update Visuals
		UpdateLimiterGainReductionVisuals(grDb);
		UpdateLimiterStatusVisuals();
		UpdateLimiterPeakTrackVisuals(peakDb);
		UpdateLimiterHistoryPlotVisuals();
		UpdateGainSafetyCardMetrics(peakDb, _lastDspAfterPeakDb, grDb, overCeilingCount);

		if (_limiterHistoryStatusTb != null)
		{
			_limiterHistoryStatusTb.Text = peakDb > -80.0f ? "播放中增益降低" : "播放后开始记录";
		}
	}

	private void UpdateGainSafetyCardMetrics(float peakDb, float postPeakDb, float grDb, int overCeilingCount)
	{
		if (_limiterMetricPreTb == null || _limiterMetricPostTb == null ||
		    _limiterMetricGrTb == null || _limiterMetricHistoryTb == null) return;

		bool hasSignal = peakDb > -80.0f;

		// 1. Metric values
		_limiterMetricPreTb.Text = hasSignal ? $"{peakDb:0.0} dBTP" : "未知";
		_limiterMetricPostTb.Text = hasSignal ? $"{postPeakDb:0.0} dBFS" : "未知";
		_limiterMetricGrTb.Text = hasSignal ? (grDb < 0.05f ? "0.0 dB" : $"-{grDb:0.0} dB") : "未知";
		_limiterMetricHistoryTb.Text = hasSignal ? overCeilingCount.ToString() : "未知";

		// 2. States indicators
		if (_limiterStatePeakTb != null)
		{
			_limiterStatePeakTb.Text = hasSignal
				? (peakDb > EffectiveCeilingDb ? "实测峰值超限: 已报告" : "实测峰值超限: 未报告")
				: "实测峰值超限: 未知";
		}

		if (_limiterStateLimitingTb != null)
		{
			_limiterStateLimitingTb.Text = hasSignal
				? (grDb >= 0.05f ? $"当前限幅: -{grDb:0.0} dB" : "当前限幅: 0.0 dB")
				: "当前限幅: 未知";
		}

		// 3. Subtitle
		if (_limiterSafetySubtitleTb != null)
		{
			if (!hasSignal)
			{
				_limiterSafetySubtitleTb.Text = "等待当前宿主读数";
			}
			else if (grDb >= 0.05f)
			{
				_limiterSafetySubtitleTb.Text = "Limiter 正在衰减";
			}
			else
			{
				_limiterSafetySubtitleTb.Text = "未报告当前峰值超限";
			}
		}
	}

	private void StartLimiterTelemetryTimer()
	{
		if (_limiterTelemetryTimer != null) return;

		_limiterTelemetryTimer = new DispatcherTimer
		{
			Interval = TimeSpan.FromMilliseconds(40)
		};
		_limiterTelemetryTimer.Tick += (s, e) =>
		{
			if (ViewModel?.AudioEngine?.DspChain?.Limiter is { } limiter)
			{
				UpdateLimiterTelemetryInternal(limiter.InputPeakDb, limiter.CurrentGainReductionDb, limiter.HistoryOverCeilingCount);
			}
		};
		_limiterTelemetryTimer.Start();
	}

	#endregion

	#region 9. Sidebar Navigation Linkage

	private void EnsureLimiterNavHooked()
	{
		if (_limiterNavHooked) return;

		foreach (var btn in _sidebarNavButtons)
		{
			if (btn.Tag is "limiter")
			{
				if (btn.Content is Grid g)
				{
					if (g.Children.Count > 1 && g.Children[1] is StackPanel sp && sp.Children.Count > 1 && sp.Children[1] is TextBlock sub)
					{
						_limiterNavSubTb = sub;
					}
					if (g.Children.Count > 2 && g.Children[2] is FontIcon checkIc)
					{
						_limiterNavCheckIcon = checkIc;
					}
				}

				btn.Click -= OnLimiterNavButtonClicked;
				btn.Click += OnLimiterNavButtonClicked;
				_limiterNavHooked = true;
				break;
			}
		}
	}

	private void OnLimiterNavButtonClicked(object? sender, RoutedEventArgs e)
	{
		if (_workbenchHost != null)
		{
			_workbenchHost.Children.Clear();
			var wb = _limiterWorkbench ?? CreateLimiterWorkbench();
			Grid.SetRow(wb, 0);
			Grid.SetColumn(wb, 0);
			_workbenchHost.Children.Add(wb);
		}
	}

	private void UpdateLimiterSidebarStatus()
	{
		if (_limiterNavSubTb != null)
		{
			_limiterNavSubTb.Text = !_limiterEnabled
				? "已旁路"
				: (_limiterManualCeilingDb.HasValue ? $"{_limiterManualCeilingDb.Value:0.0} dBTP" : "自动上限");
		}

		if (_limiterNavCheckIcon != null)
		{
			_limiterNavCheckIcon.Visibility = _limiterEnabled ? Visibility.Visible : Visibility.Collapsed;
		}
	}

	#endregion
}
