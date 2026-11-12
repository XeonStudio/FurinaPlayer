using System;
using System.Collections.Generic;
using FurinaPlayer.UI.Controls;
using FurinaPlayer.UI.ViewModels;
using Microsoft.UI;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Input;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using SonicWave.Audio;
using SonicWave.Audio.Dsp;
using Windows.Foundation;
using Windows.UI;

namespace FurinaPlayer.UI.Pages;

/// <summary>
/// FurinaPlayer 专业级动态去齿音 (De-Esser) 二级功能工作台分部实现。
/// 1:1 像素级精准还原原生检测带高斯钟形曲线（支持图上横向拖拽调节中心频率）、
/// 动态衰减切片切角、12秒 GR 衰减轨迹折线图、实时状态裁决与三组完整专业参数矩阵。
/// </summary>
public sealed partial class DspPage
{
	private Border? _deEsserWorkbench;

	// 参数状态 (严格遵循规范默认值)
	private bool _deEsserEnabled = false;
	private double _deEsserFrequencyHz = 6500.0;
	private double _deEsserQ = 4.0;
	private double _deEsserThresholdDb = -20.0;
	private double _deEsserRatio = 4.0;
	private double _deEsserKneeDb = 6.0;
	private double _deEsserMaxReductionDb = 24.0;
	private double _deEsserMix = 100.0;
	private double _deEsserAttackMs = 1.0;
	private double _deEsserReleaseMs = 40.0;
	private bool _deEsserAudition = false;

	// 顶部与操作栏控件
	private Border? _deEsserTitleIconBorder;
	private FontIcon? _deEsserTitleIcon;
	private TextBlock? _deEsserTitleTb;
	private Button? _deEsserPowerBtn;
	private FontIcon? _deEsserPowerIcon;
	private TextBlock? _deEsserPowerText;
	private Button? _deEsserAuditionBtn;
	private FontIcon? _deEsserAuditionIcon;
	private TextBlock? _deEsserAuditionText;
	private Button? _deEsserResetBtn;
	private FontIcon? _deEsserResetIcon;
	private TextBlock? _deEsserResetText;

	// 状态说明与增益降低读数
	private Ellipse? _deEsserStatusDot;
	private TextBlock? _deEsserStatusTitleTb;
	private TextBlock? _deEsserStatusDetailTb;
	private TextBlock? _deEsserGrReadoutTb;
	private TextBlock? _deEsserGrUnitTb;
	private Border? _deEsserGrTrackContainer;
	private Border? _deEsserGrTrackBar;
	private TextBlock? _deEsserGrSubTb;

	// 检测带交互曲线区域
	private TextBlock? _deEsserBandHeaderSubTb;
	private Canvas? _deEsserBandCanvas;
	private Path? _deEsserBandAreaPath;
	private Path? _deEsserBandCutPath;
	private Path? _deEsserBandLinePath;
	private Line? _deEsserBandCenterLine;
	private Line? _deEsserBandBaseLine;
	private TextBlock? _deEsserBandCenterLabelTb;
	private bool _isDraggingDeEsserBand = false;

	// 衰减轨迹历史图
	private TextBlock? _deEsserHistoryWindowTb;
	private Canvas? _deEsserHistoryCanvas;
	private Path? _deEsserHistoryLinePath;
	private Path? _deEsserHistoryAreaPath;
	private readonly float[] _deEsserGrHistory = new float[96];
	private int _deEsserGrHistoryIndex = 0;
	private float _deEsserCurrentGrDb = 0f;

	// 主题刷新追踪集合
	private readonly List<Border> _deEsserCardBorders = new();
	private readonly List<Border> _deEsserPlotBorders = new();
	private readonly List<TextBlock> _deEsserPrimaryTextBlocks = new();
	private readonly List<TextBlock> _deEsserSecondaryTextBlocks = new();
	private readonly List<TextBlock> _deEsserMutedTextBlocks = new();
	private readonly List<Line> _deEsserBandGridLines = new();
	private readonly List<Line> _deEsserHistoryGridLines = new();
	private readonly List<Slider> _deEsserSliders = new();

	// 三组参数滑块与数值显示控件
	// 找齿音
	private Slider? _sliderDeEsserFreq;
	private TextBlock? _tbDeEsserFreqVal;
	private Slider? _sliderDeEsserQ;
	private TextBlock? _tbDeEsserQVal;

	// 衰减多少
	private Slider? _sliderDeEsserThresh;
	private TextBlock? _tbDeEsserThreshVal;
	private Slider? _sliderDeEsserRatio;
	private TextBlock? _tbDeEsserRatioVal;
	private Slider? _sliderDeEsserKnee;
	private TextBlock? _tbDeEsserKneeVal;
	private Slider? _sliderDeEsserMaxReduction;
	private TextBlock? _tbDeEsserMaxReductionVal;
	private Slider? _sliderDeEsserMix;
	private TextBlock? _tbDeEsserMixVal;

	// 快慢
	private Slider? _sliderDeEsserAttack;
	private TextBlock? _tbDeEsserAttackVal;
	private Slider? _sliderDeEsserRelease;
	private TextBlock? _tbDeEsserReleaseVal;

	// 定时遥测刷新器
	private DispatcherTimer? _deEsserTelemetryTimer;

	#region 核心入口 CreateDeEsserWorkbench
	/// <summary>
	/// 构建并返回 FurinaPlayer 去齿音二级 DSP 功能工作台根元素。
	/// </summary>
	public FrameworkElement CreateDeEsserWorkbench()
	{
		if (_deEsserWorkbench != null)
		{
			SyncDeEsserControls();
			UpdateDeEsserBandVisuals();
			UpdateDeEsserHistoryVisuals();
			UpdateDeEsserVerdict();
			RefreshDeEsserTheme(IsDspDark);
			return _deEsserWorkbench;
		}

		_deEsserCardBorders.Clear();
		_deEsserPlotBorders.Clear();
		_deEsserPrimaryTextBlocks.Clear();
		_deEsserSecondaryTextBlocks.Clear();
		_deEsserMutedTextBlocks.Clear();
		_deEsserBandGridLines.Clear();
		_deEsserHistoryGridLines.Clear();
		_deEsserSliders.Clear();

		// 从底层处理器同步现有状态（若已初始化）
		try
		{
			if (ViewModel?.AudioEngine?.DspChain?.DeEsser is FurinaPlayerDeEsserProcessor deesser)
			{
				_deEsserEnabled = deesser.Enabled;
				_deEsserFrequencyHz = deesser.FrequencyHz;
				_deEsserQ = deesser.Q;
				_deEsserThresholdDb = deesser.ThresholdDb;
				_deEsserRatio = deesser.Ratio;
				_deEsserKneeDb = deesser.KneeDb;
				_deEsserMaxReductionDb = deesser.MaxReductionDb;
				_deEsserMix = deesser.Mix * 100.0;
				_deEsserAttackMs = deesser.AttackMs;
				_deEsserReleaseMs = deesser.ReleaseMs;
				_deEsserAudition = deesser.AuditionBand;
			}
		}
		catch { }

		bool isDark = IsDspDark;

		var rootBorder = new Border
		{
			Background = CreateDeEsserSurfaceBrush(LiquidGlassVariant.Clear, isDark, Math.Clamp(0.72 / 0.85, 0.55, 1.0)),
			CornerRadius = new CornerRadius(0),
			RequestedTheme = isDark ? ElementTheme.Dark : ElementTheme.Light
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

		// 1. 顶部标题与状态徽章行
		mainStack.Children.Add(CreateDeEsserTopHeaderBar());

		// 2. 顶部操作工具栏 (电源开关、按住试听、恢复默认)
		mainStack.Children.Add(CreateDeEsserActionToolbar());

		// 3. 状态说明与增益降低读数行 (已旁路 / 正在压齿音、0.0 dB)
		mainStack.Children.Add(CreateDeEsserStatusRow());

		// 4. 检测带高斯钟形曲线卡片 (可拖动调节中心频率)
		mainStack.Children.Add(CreateDeEsserBandCard());

		// 5. 衰减轨迹历史折线图卡片 (12秒窗口)
		mainStack.Children.Add(CreateDeEsserHistoryCard());

		// 6. 三组参数控制网格 (找齿音、衰减多少、快慢)
		mainStack.Children.Add(CreateDeEsserControlColumnsGrid());

		// 7. 底部提示说明卡片
		mainStack.Children.Add(CreateDeEsserBottomHintNote());

		sv.Content = mainStack;
		rootBorder.Child = sv;
		_deEsserWorkbench = rootBorder;

		// 初始化视觉呈现与主题资源
		RefreshDeEsserTheme(isDark);
		UpdateDeEsserBandVisuals();
		UpdateDeEsserHistoryVisuals();
		UpdateDeEsserVerdict();

		// 启动自动遥测定时刷新
		StartDeEsserTelemetryTimer();

		return _deEsserWorkbench;
	}
	#endregion

	#region 1. 顶部标题栏
	private FrameworkElement CreateDeEsserTopHeaderBar()
	{
		var headerGrid = new Grid();
		headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.0, GridUnitType.Star) });
		headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

		var titleSp = new StackPanel
		{
			Orientation = Orientation.Horizontal,
			Spacing = 12,
			VerticalAlignment = VerticalAlignment.Center
		};

		_deEsserTitleIcon = new FontIcon
		{
			Glyph = "\uE74F", // 去齿音 / 频带滤波标志
			FontSize = 18,
			Foreground = DspAccentSoftTextBrush,
			HorizontalAlignment = HorizontalAlignment.Center,
			VerticalAlignment = VerticalAlignment.Center
		};
		_deEsserTitleIconBorder = new Border
		{
			Width = 36,
			Height = 36,
			CornerRadius = new CornerRadius(18),
			Background = DspAccentSoftBgBrush,
			BorderBrush = DspAccentSoftBorderBrush,
			BorderThickness = new Thickness(1),
			Child = _deEsserTitleIcon
		};
		titleSp.Children.Add(_deEsserTitleIconBorder);
		_deEsserTitleTb = new TextBlock
		{
			Text = "去齿音",
			FontSize = 22,
			FontWeight = FontWeights.Bold,
			Foreground = DspTextPrimaryBrush,
			VerticalAlignment = VerticalAlignment.Center
		};
		_deEsserPrimaryTextBlocks.Add(_deEsserTitleTb);
		titleSp.Children.Add(_deEsserTitleTb);
		Grid.SetColumn(titleSp, 0);
		headerGrid.Children.Add(titleSp);

		return headerGrid;
	}
	#endregion

	#region 2. 顶部操作工具栏
	private FrameworkElement CreateDeEsserActionToolbar()
	{
		bool isDark = IsDspDark;
		var toolbarGrid = new Grid();
		toolbarGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.0, GridUnitType.Star) });
		toolbarGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

		var leftSp = new StackPanel
		{
			Orientation = Orientation.Horizontal,
			Spacing = 10,
			VerticalAlignment = VerticalAlignment.Center
		};

		// 1. 电源开关按钮: 【去齿音开启】/【去齿音旁路】
		_deEsserPowerIcon = new FontIcon
		{
			Glyph = "\uE7E8",
			FontSize = 14,
			Foreground = DspTextMutedBrush
		};
		_deEsserPowerText = new TextBlock
		{
			Text = _deEsserEnabled ? "去齿音开启" : "去齿音旁路",
			FontSize = 13,
			FontWeight = FontWeights.Medium,
			Foreground = DspTextSubHeaderBrush
		};
		var powerSp = new StackPanel
		{
			Orientation = Orientation.Horizontal,
			Spacing = 6,
			Children = { _deEsserPowerIcon, _deEsserPowerText }
		};

		_deEsserPowerBtn = new Button
		{
			Content = powerSp,
			Padding = new Thickness(12, 6, 14, 6),
			CornerRadius = new CornerRadius(8),
			BorderThickness = new Thickness(1)
		};
		UpdateDeEsserPowerButtonVisuals(isDark);
		_deEsserPowerBtn.Click += (s, e) =>
		{
			_deEsserEnabled = !_deEsserEnabled;
			UpdateDeEsserPowerButtonVisuals(IsDspDark);
			SyncDeEsserToEngine();
			UpdateDeEsserVerdict();
		};
		leftSp.Children.Add(_deEsserPowerBtn);

		// 2. 【按住试听齿音频带】独听按钮 (按住开启独听，松开恢复)
		_deEsserAuditionIcon = new FontIcon
		{
			Glyph = "\uE7F5", // 耳机/独听标志
			FontSize = 13,
			Foreground = DspTextSubHeaderBrush
		};
		_deEsserAuditionText = new TextBlock
		{
			Text = "按住试听齿音频带",
			FontSize = 13,
			Foreground = DspTextSecondaryBrush,
			FontWeight = FontWeights.Medium
		};
		var auditionSp = new StackPanel
		{
			Orientation = Orientation.Horizontal,
			Spacing = 6,
			Children = { _deEsserAuditionIcon, _deEsserAuditionText }
		};

		_deEsserAuditionBtn = new Button
		{
			Content = auditionSp,
			Padding = new Thickness(12, 6, 14, 6),
			CornerRadius = new CornerRadius(8),
			BorderThickness = new Thickness(1)
		};
		UpdateDeEsserAuditionButtonVisuals(isDark);

		// 独听按住交互绑定 (指针与键盘)
		_deEsserAuditionBtn.PointerPressed += (s, e) =>
		{
			if (s is UIElement el)
			{
				el.CapturePointer(e.Pointer);
				SetDeEsserAuditionState(true);
				e.Handled = true;
			}
		};
		_deEsserAuditionBtn.PointerReleased += (s, e) =>
		{
			if (s is UIElement el)
			{
				el.ReleasePointerCapture(e.Pointer);
				SetDeEsserAuditionState(false);
				e.Handled = true;
			}
		};
		_deEsserAuditionBtn.PointerExited += (s, e) =>
		{
			if (!e.Pointer.IsInContact || (s is UIElement el && (el.PointerCaptures == null || el.PointerCaptures.Count == 0)))
			{
				SetDeEsserAuditionState(false);
			}
			UpdateDeEsserAuditionButtonVisuals(IsDspDark);
		};
		_deEsserAuditionBtn.PointerCanceled += (s, e) =>
		{
			if (s is UIElement el)
			{
				el.ReleasePointerCapture(e.Pointer);
				SetDeEsserAuditionState(false);
				e.Handled = true;
			}
		};
		_deEsserAuditionBtn.PointerCaptureLost += (s, e) =>
		{
			SetDeEsserAuditionState(false);
		};
		_deEsserAuditionBtn.KeyDown += (s, e) =>
		{
			if (!e.KeyStatus.IsMenuKeyDown && !e.KeyStatus.WasKeyDown && (e.Key == Windows.System.VirtualKey.Space || e.Key == Windows.System.VirtualKey.Enter))
			{
				SetDeEsserAuditionState(true);
				e.Handled = true;
			}
		};
		_deEsserAuditionBtn.KeyUp += (s, e) =>
		{
			if (e.Key == Windows.System.VirtualKey.Space || e.Key == Windows.System.VirtualKey.Enter)
			{
				SetDeEsserAuditionState(false);
				e.Handled = true;
			}
		};
		_deEsserAuditionBtn.LostFocus += (s, e) =>
		{
			SetDeEsserAuditionState(false);
		};
		leftSp.Children.Add(_deEsserAuditionBtn);

		// 3. 【恢复默认】按钮
		_deEsserResetIcon = new FontIcon { Glyph = "\uE7A7", FontSize = 13, Foreground = DspTextSubHeaderBrush };
		_deEsserResetText = new TextBlock { Text = "恢复默认", FontSize = 13, Foreground = DspTextSecondaryBrush, FontWeight = FontWeights.Medium };
		_deEsserResetBtn = new Button
		{
			Content = new StackPanel
			{
				Orientation = Orientation.Horizontal,
				Spacing = 6,
				Children =
				{
					_deEsserResetIcon,
					_deEsserResetText
				}
			},
			Padding = new Thickness(12, 6, 14, 6),
			CornerRadius = new CornerRadius(8),
			BorderThickness = new Thickness(1)
		};
		UpdateDeEsserResetButtonVisuals(isDark);
		_deEsserResetBtn.Click += (s, e) =>
		{
			_deEsserFrequencyHz = 6500.0;
			_deEsserQ = 4.0;
			_deEsserThresholdDb = -20.0;
			_deEsserRatio = 4.0;
			_deEsserKneeDb = 6.0;
			_deEsserMaxReductionDb = 24.0;
			_deEsserMix = 100.0;
			_deEsserAttackMs = 1.0;
			_deEsserReleaseMs = 40.0;
			_deEsserAudition = false;

			SyncDeEsserControls();
			SyncDeEsserToEngine();
			UpdateDeEsserBandVisuals();
			UpdateDeEsserVerdict();
		};
		leftSp.Children.Add(_deEsserResetBtn);

		Grid.SetColumn(leftSp, 0);
		toolbarGrid.Children.Add(leftSp);

		return toolbarGrid;
	}
	#endregion

	#region 3. 状态说明与增益降低读数行
	private FrameworkElement CreateDeEsserStatusRow()
	{
		bool isDark = IsDspDark;
		var grid = new Grid
		{
			Margin = new Thickness(0, 4, 0, 4)
		};
		grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.0, GridUnitType.Star) });
		grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(168, GridUnitType.Pixel) });

		// 左侧状态判定区
		var verdictGrid = new Grid { VerticalAlignment = VerticalAlignment.Center };
		verdictGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
		verdictGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.0, GridUnitType.Star) });

		_deEsserStatusDot = new Ellipse
		{
			Width = 8,
			Height = 8,
			Fill = DspTextMutedBrush,
			VerticalAlignment = VerticalAlignment.Top,
			Margin = new Thickness(0, 6, 12, 0)
		};
		Grid.SetColumn(_deEsserStatusDot, 0);
		verdictGrid.Children.Add(_deEsserStatusDot);

		var textSp = new StackPanel { Spacing = 4 };
		_deEsserStatusTitleTb = new TextBlock
		{
			Text = "已旁路",
			FontSize = 18,
			FontWeight = FontWeights.Bold,
			Foreground = DspTextPrimaryBrush
		};
		_deEsserPrimaryTextBlocks.Add(_deEsserStatusTitleTb);
		_deEsserStatusDetailTb = new TextBlock
		{
			Text = "齿音会原样通过。打开之后，只有检测带里超出阈值的成分会被衰减。",
			FontSize = 13,
			Foreground = DspTextMutedBrush,
			TextWrapping = TextWrapping.Wrap,
			MaxWidth = 720
		};
		_deEsserMutedTextBlocks.Add(_deEsserStatusDetailTb);
		textSp.Children.Add(_deEsserStatusTitleTb);
		textSp.Children.Add(_deEsserStatusDetailTb);
		Grid.SetColumn(textSp, 1);
		verdictGrid.Children.Add(textSp);

		Grid.SetColumn(verdictGrid, 0);
		grid.Children.Add(verdictGrid);

		// 右侧大号增益降低读数
		var grSp = new StackPanel
		{
			HorizontalAlignment = HorizontalAlignment.Right,
			VerticalAlignment = VerticalAlignment.Bottom,
			Spacing = 4,
			Width = 168
		};

		var readoutGrid = new Grid { HorizontalAlignment = HorizontalAlignment.Right };
		readoutGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
		readoutGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

		_deEsserGrReadoutTb = new TextBlock
		{
			Text = "0.0",
			FontSize = 40,
			FontWeight = FontWeights.SemiBold,
			Foreground = DspTextPrimaryBrush,
			FontFamily = new FontFamily("Segoe UI Variable Display, Consolas, Segoe UI")
		};
		Grid.SetColumn(_deEsserGrReadoutTb, 0);
		readoutGrid.Children.Add(_deEsserGrReadoutTb);

		_deEsserGrUnitTb = new TextBlock
		{
			Text = "dB",
			FontSize = 14,
			FontWeight = FontWeights.SemiBold,
			Foreground = DspTextMutedBrush,
			VerticalAlignment = VerticalAlignment.Bottom,
			Margin = new Thickness(4, 0, 0, 7)
		};
		_deEsserMutedTextBlocks.Add(_deEsserGrUnitTb);
		Grid.SetColumn(_deEsserGrUnitTb, 1);
		readoutGrid.Children.Add(_deEsserGrUnitTb);

		grSp.Children.Add(readoutGrid);

		// 动态衰减刻度条
		_deEsserGrTrackContainer = new Border
		{
			Height = 4,
			CornerRadius = new CornerRadius(2),
			Background = GetDeEsserMeterTrackBrush(isDark),
			HorizontalAlignment = HorizontalAlignment.Stretch
		};
		_deEsserGrTrackBar = new Border
		{
			Height = 4,
			CornerRadius = new CornerRadius(2),
			Background = GetDeEsserIndigoStrokeBrush(isDark),
			HorizontalAlignment = HorizontalAlignment.Left,
			Width = 0
		};
		_deEsserGrTrackContainer.Child = _deEsserGrTrackBar;
		grSp.Children.Add(_deEsserGrTrackContainer);

		_deEsserGrSubTb = new TextBlock
		{
			Text = "增益降低  当前没有衰减",
			FontSize = 12,
			FontWeight = FontWeights.SemiBold,
			Foreground = DspTextMutedBrush,
			HorizontalAlignment = HorizontalAlignment.Right,
			Margin = new Thickness(0, 2, 0, 0)
		};
		_deEsserMutedTextBlocks.Add(_deEsserGrSubTb);
		grSp.Children.Add(_deEsserGrSubTb);

		Grid.SetColumn(grSp, 1);
		grid.Children.Add(grSp);

		return grid;
	}
	#endregion

	#region 4. 检测带高斯钟形曲线卡片
	private FrameworkElement CreateDeEsserBandCard()
	{
		bool isDark = IsDspDark;
		var card = CreateDeEsserCardBorder(isDark, new Thickness(18, 14, 18, 14));

		var stack = new StackPanel { Spacing = 10 };

		var head = new Grid();
		head.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.0, GridUnitType.Star) });
		head.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

		var tbTitle = new TextBlock
		{
			Text = "检测带",
			FontSize = 14,
			FontWeight = FontWeights.Bold,
			Foreground = DspTextPrimaryBrush
		};
		_deEsserPrimaryTextBlocks.Add(tbTitle);
		double bw = Math.Round(_deEsserFrequencyHz / Math.Max(0.5, _deEsserQ));
		_deEsserBandHeaderSubTb = new TextBlock
		{
			Text = $"约 {bw:F0} Hz 宽 · 拖动曲线改变频率",
			FontSize = 12,
			FontWeight = FontWeights.SemiBold,
			Foreground = DspTextMutedBrush
		};
		_deEsserMutedTextBlocks.Add(_deEsserBandHeaderSubTb);
		Grid.SetColumn(tbTitle, 0); Grid.SetColumn(_deEsserBandHeaderSubTb, 1);
		head.Children.Add(tbTitle); head.Children.Add(_deEsserBandHeaderSubTb);
		stack.Children.Add(head);

		// Canvas 容器与视图缩放 Viewbox
		var plotBorder = new Border
		{
			CornerRadius = new CornerRadius(8),
			BorderBrush = CreateDeEsserRimBrush(isDark),
			BorderThickness = new Thickness(1),
			Background = GetDeEsserSoftSurfaceBrush(isDark)
		};
		_deEsserPlotBorders.Add(plotBorder);

		var viewbox = new Viewbox
		{
			Stretch = Stretch.Uniform,
			Height = 148,
			HorizontalAlignment = HorizontalAlignment.Stretch
		};

		_deEsserBandCanvas = new Canvas
		{
			Width = 640,
			Height = 148,
			Background = new SolidColorBrush(Colors.Transparent)
		};

		try
		{
			var prop = typeof(UIElement).GetProperty("ProtectedCursor", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
			prop?.SetValue(_deEsserBandCanvas, InputSystemCursor.Create(InputSystemCursorShape.SizeWestEast));
		}
		catch { }

		_deEsserBandCanvas.PointerPressed += OnDeEsserBandPointerDown;
		_deEsserBandCanvas.PointerMoved += OnDeEsserBandPointerMove;
		_deEsserBandCanvas.PointerReleased += OnDeEsserBandPointerUp;
		_deEsserBandCanvas.PointerCanceled += OnDeEsserBandPointerUp;

		// 绘制频率网格垂直线与标签：2k, 4k, 8k, 12k
		double[] ticks = { 2000, 4000, 8000, 12000 };
		var monoFont = new FontFamily("Segoe UI Variable Text, Consolas, Segoe UI");
		var tickBrush = DspTextMutedBrush;
		var gridLineBrush = GetDeEsserGridLineBrush(isDark, isBand: true);

		foreach (double tk in ticks)
		{
			double x = XForDeEsserHz(tk);
			var gridLine = new Line
			{
				X1 = x, X2 = x, Y1 = 18, Y2 = 122,
				Stroke = gridLineBrush,
				StrokeThickness = 1
			};
			_deEsserBandGridLines.Add(gridLine);
			_deEsserBandCanvas.Children.Add(gridLine);
			string label = $"{Math.Round(tk / 1000.0):F0}k";
			double leftPos = tk switch
			{
				2000 => x,
				12000 => x - 22,
				_ => x - 8
			};
			var tickTb = AddDeEsserCanvasText(_deEsserBandCanvas, label, leftPos, 126, tickBrush, monoFont, 11);
			_deEsserMutedTextBlocks.Add(tickTb);
		}

		// 底部水平基准线 (y = 122)
		_deEsserBandBaseLine = new Line
		{
			X1 = 18, X2 = 622, Y1 = 122, Y2 = 122,
			Stroke = GetDeEsserBaseLineBrush(isDark),
			StrokeThickness = 1
		};
		_deEsserBandCanvas.Children.Add(_deEsserBandBaseLine);

		// 高斯钟形曲线填充区
		_deEsserBandAreaPath = new Path
		{
			Fill = GetDeEsserBandAreaBrush(isDark)
		};
		// 动态衰减切片切角填充区
		_deEsserBandCutPath = new Path
		{
			Fill = GetDeEsserBandCutBrush(isDark),
			Visibility = Visibility.Collapsed
		};
		// 高斯曲线轮廓线
		_deEsserBandLinePath = new Path
		{
			Stroke = GetDeEsserIndigoStrokeBrush(isDark),
			StrokeThickness = 2.0,
			StrokeStartLineCap = PenLineCap.Round,
			StrokeEndLineCap = PenLineCap.Round,
			StrokeLineJoin = PenLineJoin.Round
		};

		// 中心频率虚线
		_deEsserBandCenterLine = new Line
		{
			Stroke = GetDeEsserIndigoStrokeBrush(isDark),
			StrokeThickness = 1.0,
			StrokeDashArray = new DoubleCollection { 3, 3 }
		};

		// 中心频率数值标签
		_deEsserBandCenterLabelTb = new TextBlock
		{
			Text = FormatDeEsserFrequency(_deEsserFrequencyHz),
			FontSize = 12,
			FontWeight = FontWeights.Bold,
			Foreground = DspTextPrimaryBrush,
			FontFamily = monoFont
		};
		_deEsserPrimaryTextBlocks.Add(_deEsserBandCenterLabelTb);

		_deEsserBandCanvas.Children.Add(_deEsserBandAreaPath);
		_deEsserBandCanvas.Children.Add(_deEsserBandCutPath);
		_deEsserBandCanvas.Children.Add(_deEsserBandLinePath);
		_deEsserBandCanvas.Children.Add(_deEsserBandCenterLine);
		_deEsserBandCanvas.Children.Add(_deEsserBandCenterLabelTb);

		viewbox.Child = _deEsserBandCanvas;
		plotBorder.Child = viewbox;
		stack.Children.Add(plotBorder);

		card.Child = stack;
		return card;
	}

	private void OnDeEsserBandPointerDown(object sender, PointerRoutedEventArgs e)
	{
		if (sender is FrameworkElement fe)
		{
			_isDraggingDeEsserBand = true;
			fe.CapturePointer(e.Pointer);
			var pt = e.GetCurrentPoint(fe).Position;
			UpdateDeEsserFrequencyFromX(pt.X);
			e.Handled = true;
		}
	}

	private void OnDeEsserBandPointerMove(object sender, PointerRoutedEventArgs e)
	{
		if (_isDraggingDeEsserBand && sender is FrameworkElement fe)
		{
			var pt = e.GetCurrentPoint(fe).Position;
			UpdateDeEsserFrequencyFromX(pt.X);
			e.Handled = true;
		}
	}

	private void OnDeEsserBandPointerUp(object sender, PointerRoutedEventArgs e)
	{
		if (sender is FrameworkElement fe)
		{
			_isDraggingDeEsserBand = false;
			fe.ReleasePointerCapture(e.Pointer);
			e.Handled = true;
		}
	}

	private void UpdateDeEsserFrequencyFromX(double x)
	{
		double freq = Math.Clamp(Math.Round(HzForDeEsserX(x) / 10.0) * 10.0, 2000.0, 12000.0);
		if (Math.Abs(_deEsserFrequencyHz - freq) < 1.0) return;
		_deEsserFrequencyHz = freq;
		if (_sliderDeEsserFreq != null) _sliderDeEsserFreq.Value = freq;
		if (_tbDeEsserFreqVal != null) _tbDeEsserFreqVal.Text = $"{freq:F0} Hz";
		if (_deEsserBandHeaderSubTb != null)
		{
			double bw = Math.Round(_deEsserFrequencyHz / Math.Max(0.5, _deEsserQ));
			_deEsserBandHeaderSubTb.Text = $"约 {bw:F0} Hz 宽 · 拖动曲线改变频率";
		}
		SyncDeEsserToEngine();
		UpdateDeEsserBandVisuals();
	}

	private void UpdateDeEsserBandVisuals()
	{
		if (_deEsserBandCanvas == null || _deEsserBandLinePath == null || _deEsserBandAreaPath == null) return;

		const double MarginB = 18.0;
		const double Span = 604.0;
		const double MinHz = 2000.0;
		const double MaxHz = 12000.0;
		double logSpan = Math.Log(MaxHz) - Math.Log(MinHz);

		double b = 122.0;
		double S = 104.0;
		double q = Math.Max(_deEsserQ, 0.5);
		double sigma = _deEsserFrequencyHz / q / 2.355;
		double grNorm = Math.Clamp(_deEsserCurrentGrDb / 12.0f, 0.0, 1.0);

		int ptCount = 96;
		Point[] curvePoints = new Point[ptCount];
		Point[] cutPoints = new Point[ptCount];

		for (int k = 0; k < ptCount; k++)
		{
			double norm = (double)k / (ptCount - 1);
			double hz = Math.Exp(Math.Log(MinHz) + norm * logSpan);
			double diff = (hz - _deEsserFrequencyHz) / sigma;
			double g = Math.Exp(-0.5 * diff * diff);
			double y = b - g * S;
			double cutY = y + (b - y) * grNorm;
			double x = MarginB + norm * Span;
			curvePoints[k] = new Point(x, y);
			cutPoints[k] = new Point(x, cutY);
		}

		// 1. 曲线轮廓
		var lineGeom = new PathGeometry();
		var lineFig = new PathFigure { StartPoint = curvePoints[0], IsClosed = false };
		for (int i = 1; i < ptCount; i++)
			lineFig.Segments.Add(new LineSegment { Point = curvePoints[i] });
		lineGeom.Figures.Add(lineFig);
		_deEsserBandLinePath.Data = lineGeom;

		// 2. 曲线下半部分背景填充
		var areaGeom = new PathGeometry();
		var areaFig = new PathFigure { StartPoint = new Point(MarginB, b), IsClosed = true };
		for (int i = 0; i < ptCount; i++)
			areaFig.Segments.Add(new LineSegment { Point = curvePoints[i] });
		areaFig.Segments.Add(new LineSegment { Point = new Point(MarginB + Span, b) });
		areaGeom.Figures.Add(areaFig);
		_deEsserBandAreaPath.Data = areaGeom;

		// 3. 动态衰减切片
		if (grNorm > 0.01 && _deEsserBandCutPath != null)
		{
			_deEsserBandCutPath.Visibility = Visibility.Visible;
			var cutGeom = new PathGeometry();
			var cutFig = new PathFigure { StartPoint = curvePoints[0], IsClosed = true };
			for (int i = 1; i < ptCount; i++)
				cutFig.Segments.Add(new LineSegment { Point = curvePoints[i] });
			for (int i = ptCount - 1; i >= 0; i--)
				cutFig.Segments.Add(new LineSegment { Point = cutPoints[i] });
			cutGeom.Figures.Add(cutFig);
			_deEsserBandCutPath.Data = cutGeom;
		}
		else if (_deEsserBandCutPath != null)
		{
			_deEsserBandCutPath.Visibility = Visibility.Collapsed;
		}

		// 4. 中心频率竖线与标示
		double cx = XForDeEsserHz(_deEsserFrequencyHz);
		if (_deEsserBandCenterLine != null)
		{
			_deEsserBandCenterLine.X1 = cx;
			_deEsserBandCenterLine.X2 = cx;
			_deEsserBandCenterLine.Y1 = 18.0;
			_deEsserBandCenterLine.Y2 = b;
		}

		if (_deEsserBandCenterLabelTb != null)
		{
			_deEsserBandCenterLabelTb.Text = FormatDeEsserFrequency(_deEsserFrequencyHz);
			Canvas.SetTop(_deEsserBandCenterLabelTb, 2.0);
			if (cx > 576.0)
			{
				Canvas.SetLeft(_deEsserBandCenterLabelTb, cx - 52.0);
				_deEsserBandCenterLabelTb.TextAlignment = TextAlignment.Right;
			}
			else if (cx < 64.0)
			{
				Canvas.SetLeft(_deEsserBandCenterLabelTb, cx);
				_deEsserBandCenterLabelTb.TextAlignment = TextAlignment.Left;
			}
			else
			{
				Canvas.SetLeft(_deEsserBandCenterLabelTb, cx - 26.0);
				_deEsserBandCenterLabelTb.TextAlignment = TextAlignment.Center;
			}
		}
	}
	#endregion

	#region 5. 衰减轨迹历史折线图卡片
	private FrameworkElement CreateDeEsserHistoryCard()
	{
		bool isDark = IsDspDark;
		var card = CreateDeEsserCardBorder(isDark, new Thickness(18, 14, 18, 14));

		var stack = new StackPanel { Spacing = 10 };

		var head = new Grid();
		head.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.0, GridUnitType.Star) });
		head.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

		var tbTitle = new TextBlock
		{
			Text = "衰减轨迹",
			FontSize = 14,
			FontWeight = FontWeights.Bold,
			Foreground = DspTextPrimaryBrush
		};
		_deEsserPrimaryTextBlocks.Add(tbTitle);
		_deEsserHistoryWindowTb = new TextBlock
		{
			Text = "播放后开始记录",
			FontSize = 12,
			FontWeight = FontWeights.SemiBold,
			Foreground = DspTextMutedBrush
		};
		_deEsserMutedTextBlocks.Add(_deEsserHistoryWindowTb);
		Grid.SetColumn(tbTitle, 0); Grid.SetColumn(_deEsserHistoryWindowTb, 1);
		head.Children.Add(tbTitle); head.Children.Add(_deEsserHistoryWindowTb);
		stack.Children.Add(head);

		var plotBorder = new Border
		{
			CornerRadius = new CornerRadius(8),
			BorderBrush = CreateDeEsserRimBrush(isDark),
			BorderThickness = new Thickness(1),
			Background = GetDeEsserSoftSurfaceBrush(isDark)
		};
		_deEsserPlotBorders.Add(plotBorder);

		var viewbox = new Viewbox
		{
			Stretch = Stretch.Uniform,
			Height = 96,
			HorizontalAlignment = HorizontalAlignment.Stretch
		};

		_deEsserHistoryCanvas = new Canvas
		{
			Width = 640,
			Height = 96,
			Background = new SolidColorBrush(Colors.Transparent)
		};

		// 标尺水平线: 0, -12, -24 dB
		double[] ticks = { 0, 12, 24 };
		var monoFont = new FontFamily("Segoe UI Variable Text, Consolas, Segoe UI");
		var labelBrush = DspTextMutedBrush;
		var gridLineBrush = GetDeEsserGridLineBrush(isDark, isBand: false);

		foreach (double t in ticks)
		{
			double y = 86.0 - (t / 24.0) * 74.0;
			var gridLine = new Line
			{
				X1 = 28, X2 = 640, Y1 = y, Y2 = y,
				Stroke = gridLineBrush,
				StrokeThickness = 1
			};
			_deEsserHistoryGridLines.Add(gridLine);
			_deEsserHistoryCanvas.Children.Add(gridLine);
			string txt = t == 0 ? "0" : $"-{t:F0}";
			var tickTb = AddDeEsserCanvasText(_deEsserHistoryCanvas, txt, 4, y - 7, labelBrush, monoFont, 9.5);
			_deEsserMutedTextBlocks.Add(tickTb);
		}

		_deEsserHistoryAreaPath = new Path
		{
			Fill = GetDeEsserHistoryAreaBrush(isDark)
		};
		_deEsserHistoryLinePath = new Path
		{
			Stroke = GetDeEsserIndigoStrokeBrush(isDark),
			StrokeThickness = 1.8,
			StrokeStartLineCap = PenLineCap.Round,
			StrokeEndLineCap = PenLineCap.Round
		};

		_deEsserHistoryCanvas.Children.Add(_deEsserHistoryAreaPath);
		_deEsserHistoryCanvas.Children.Add(_deEsserHistoryLinePath);

		viewbox.Child = _deEsserHistoryCanvas;
		plotBorder.Child = viewbox;
		stack.Children.Add(plotBorder);

		card.Child = stack;
		return card;
	}

	private void UpdateDeEsserHistoryVisuals()
	{
		if (_deEsserHistoryCanvas == null || _deEsserHistoryLinePath == null || _deEsserHistoryAreaPath == null) return;

		int n = _deEsserGrHistory.Length;
		Point[] pts = new Point[n];
		for (int i = 0; i < n; i++)
		{
			int bufIdx = (_deEsserGrHistoryIndex + i) % n;
			float val = _deEsserGrHistory[bufIdx];
			double x = 28.0 + (double)i / (n - 1) * (640.0 - 28.0);
			double y = 86.0 - Math.Clamp(val / 24.0, 0.0, 1.0) * 74.0;
			pts[i] = new Point(x, y);
		}

		var lineGeom = new PathGeometry();
		var lineFig = new PathFigure { StartPoint = pts[0], IsClosed = false };
		for (int i = 1; i < n; i++)
			lineFig.Segments.Add(new LineSegment { Point = pts[i] });
		lineGeom.Figures.Add(lineFig);
		_deEsserHistoryLinePath.Data = lineGeom;

		var areaGeom = new PathGeometry();
		var areaFig = new PathFigure { StartPoint = new Point(28.0, 86.0), IsClosed = true };
		for (int i = 0; i < n; i++)
			areaFig.Segments.Add(new LineSegment { Point = pts[i] });
		areaFig.Segments.Add(new LineSegment { Point = new Point(640.0, 86.0) });
		areaGeom.Figures.Add(areaFig);
		_deEsserHistoryAreaPath.Data = areaGeom;

		if (_deEsserHistoryWindowTb != null)
		{
			bool isPlaying = ViewModel?.AudioEngine?.State == PlaybackState.Playing;
			_deEsserHistoryWindowTb.Text = (_deEsserCurrentGrDb > 0.05f || isPlaying) ? "约 12 秒窗口" : "播放后开始记录";
		}
	}
	#endregion

	#region 6. 三组参数控制网格
	private FrameworkElement CreateDeEsserControlColumnsGrid()
	{
		bool isDark = IsDspDark;
		var grid = new Grid { ColumnSpacing = 20 };
		grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.0, GridUnitType.Star) });
		grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.0, GridUnitType.Star) });
		grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.0, GridUnitType.Star) });

		// 列 0: 找齿音
		var card0 = CreateDeEsserCardBorder(isDark, new Thickness(18, 16, 18, 16));
		var sp0 = new StackPanel { Spacing = 14 };
		var h0 = new Grid();
		h0.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.0, GridUnitType.Star) });
		h0.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
		var title0 = new TextBlock { Text = "找齿音", FontSize = 14, FontWeight = FontWeights.Bold, Foreground = DspTextPrimaryBrush };
		_deEsserPrimaryTextBlocks.Add(title0);
		h0.Children.Add(title0);
		var sub0 = new TextBlock { Text = "Q 越大越窄", FontSize = 12, FontWeight = FontWeights.SemiBold, Foreground = DspTextMutedBrush };
		_deEsserMutedTextBlocks.Add(sub0);
		Grid.SetColumn(sub0, 1); h0.Children.Add(sub0);
		sp0.Children.Add(h0);

		sp0.Children.Add(CreateDeEsserSliderControl("频率", $"{_deEsserFrequencyHz:F0} Hz", 2000.0, 12000.0, 10.0, _deEsserFrequencyHz, out _sliderDeEsserFreq, out _tbDeEsserFreqVal, val =>
		{
			_deEsserFrequencyHz = Math.Clamp(Math.Round(val / 10.0) * 10.0, 2000.0, 12000.0);
			if (_tbDeEsserFreqVal != null) _tbDeEsserFreqVal.Text = $"{_deEsserFrequencyHz:F0} Hz";
			if (_deEsserBandHeaderSubTb != null)
			{
				double bw = Math.Round(_deEsserFrequencyHz / Math.Max(0.5, _deEsserQ));
				_deEsserBandHeaderSubTb.Text = $"约 {bw:F0} Hz 宽 · 拖动曲线改变频率";
			}
			SyncDeEsserToEngine();
			UpdateDeEsserBandVisuals();
		}));

		sp0.Children.Add(CreateDeEsserSliderControl("Q", $"{_deEsserQ:F1}", 0.5, 12.0, 0.1, _deEsserQ, out _sliderDeEsserQ, out _tbDeEsserQVal, val =>
		{
			_deEsserQ = Math.Clamp(Math.Round(val, 1), 0.5, 12.0);
			if (_tbDeEsserQVal != null) _tbDeEsserQVal.Text = $"{_deEsserQ:F1}";
			if (_deEsserBandHeaderSubTb != null)
			{
				double bw = Math.Round(_deEsserFrequencyHz / Math.Max(0.5, _deEsserQ));
				_deEsserBandHeaderSubTb.Text = $"约 {bw:F0} Hz 宽 · 拖动曲线改变频率";
			}
			SyncDeEsserToEngine();
			UpdateDeEsserBandVisuals();
		}));

		card0.Child = sp0;
		Grid.SetColumn(card0, 0);
		grid.Children.Add(card0);

		// 列 1: 衰减多少
		var card1 = CreateDeEsserCardBorder(isDark, new Thickness(18, 16, 18, 16));
		var sp1 = new StackPanel { Spacing = 14 };
		var h1 = new Grid();
		h1.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.0, GridUnitType.Star) });
		h1.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
		var title1 = new TextBlock { Text = "衰减多少", FontSize = 14, FontWeight = FontWeights.Bold, Foreground = DspTextPrimaryBrush };
		_deEsserPrimaryTextBlocks.Add(title1);
		h1.Children.Add(title1);
		var sub1 = new TextBlock { Text = "阈值越低越容易触发", FontSize = 12, FontWeight = FontWeights.SemiBold, Foreground = DspTextMutedBrush };
		_deEsserMutedTextBlocks.Add(sub1);
		Grid.SetColumn(sub1, 1); h1.Children.Add(sub1);
		sp1.Children.Add(h1);

		sp1.Children.Add(CreateDeEsserSliderControl("阈值", $"{_deEsserThresholdDb:F1} dB", -48.0, 0.0, 0.5, _deEsserThresholdDb, out _sliderDeEsserThresh, out _tbDeEsserThreshVal, val =>
		{
			_deEsserThresholdDb = Math.Clamp(Math.Round(val, 1), -48.0, 0.0);
			if (_tbDeEsserThreshVal != null) _tbDeEsserThreshVal.Text = $"{_deEsserThresholdDb:F1} dB";
			UpdateDeEsserVerdict();
			SyncDeEsserToEngine();
		}));

		sp1.Children.Add(CreateDeEsserSliderControl("比率", $"{_deEsserRatio:F1} :1", 1.0, 20.0, 0.1, _deEsserRatio, out _sliderDeEsserRatio, out _tbDeEsserRatioVal, val =>
		{
			_deEsserRatio = Math.Clamp(Math.Round(val, 1), 1.0, 20.0);
			if (_tbDeEsserRatioVal != null) _tbDeEsserRatioVal.Text = $"{_deEsserRatio:F1} :1";
			SyncDeEsserToEngine();
		}));

		sp1.Children.Add(CreateDeEsserSliderControl("拐点", $"{_deEsserKneeDb:F1} dB", 0.0, 18.0, 0.5, _deEsserKneeDb, out _sliderDeEsserKnee, out _tbDeEsserKneeVal, val =>
		{
			_deEsserKneeDb = Math.Clamp(Math.Round(val, 1), 0.0, 18.0);
			if (_tbDeEsserKneeVal != null) _tbDeEsserKneeVal.Text = $"{_deEsserKneeDb:F1} dB";
			SyncDeEsserToEngine();
		}));

		sp1.Children.Add(CreateDeEsserSliderControl("最大衰减", $"{_deEsserMaxReductionDb:F1} dB", 0.0, 24.0, 0.5, _deEsserMaxReductionDb, out _sliderDeEsserMaxReduction, out _tbDeEsserMaxReductionVal, val =>
		{
			_deEsserMaxReductionDb = Math.Clamp(Math.Round(val, 1), 0.0, 24.0);
			if (_tbDeEsserMaxReductionVal != null) _tbDeEsserMaxReductionVal.Text = $"{_deEsserMaxReductionDb:F1} dB";
			SyncDeEsserToEngine();
		}));

		sp1.Children.Add(CreateDeEsserSliderControl("湿声", $"{_deEsserMix:F0} %", 0.0, 100.0, 1.0, _deEsserMix, out _sliderDeEsserMix, out _tbDeEsserMixVal, val =>
		{
			_deEsserMix = Math.Clamp(Math.Round(val), 0.0, 100.0);
			if (_tbDeEsserMixVal != null) _tbDeEsserMixVal.Text = $"{_deEsserMix:F0} %";
			UpdateDeEsserVerdict();
			SyncDeEsserToEngine();
		}));

		card1.Child = sp1;
		Grid.SetColumn(card1, 1);
		grid.Children.Add(card1);

		// 列 2: 快慢
		var card2 = CreateDeEsserCardBorder(isDark, new Thickness(18, 16, 18, 16));
		var sp2 = new StackPanel { Spacing = 14 };
		var h2 = new Grid();
		h2.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.0, GridUnitType.Star) });
		h2.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
		var title2 = new TextBlock { Text = "快慢", FontSize = 14, FontWeight = FontWeights.Bold, Foreground = DspTextPrimaryBrush };
		_deEsserPrimaryTextBlocks.Add(title2);
		h2.Children.Add(title2);
		var sub2 = new TextBlock { Text = "齿音很短", FontSize = 12, FontWeight = FontWeights.SemiBold, Foreground = DspTextMutedBrush };
		_deEsserMutedTextBlocks.Add(sub2);
		Grid.SetColumn(sub2, 1); h2.Children.Add(sub2);
		sp2.Children.Add(h2);

		sp2.Children.Add(CreateDeEsserSliderControl("启动", $"{_deEsserAttackMs:F1} ms", 0.1, 30.0, 0.1, _deEsserAttackMs, out _sliderDeEsserAttack, out _tbDeEsserAttackVal, val =>
		{
			_deEsserAttackMs = Math.Clamp(Math.Round(val, 1), 0.1, 30.0);
			if (_tbDeEsserAttackVal != null) _tbDeEsserAttackVal.Text = $"{_deEsserAttackMs:F1} ms";
			SyncDeEsserToEngine();
		}));

		sp2.Children.Add(CreateDeEsserSliderControl("释放", $"{_deEsserReleaseMs:F0} ms", 5.0, 200.0, 1.0, _deEsserReleaseMs, out _sliderDeEsserRelease, out _tbDeEsserReleaseVal, val =>
		{
			_deEsserReleaseMs = Math.Clamp(Math.Round(val), 5.0, 200.0);
			if (_tbDeEsserReleaseVal != null) _tbDeEsserReleaseVal.Text = $"{_deEsserReleaseMs:F0} ms";
			SyncDeEsserToEngine();
		}));

		card2.Child = sp2;
		Grid.SetColumn(card2, 2);
		grid.Children.Add(card2);

		return grid;
	}
	#endregion

	#region 7. 底部提示说明卡片
	private FrameworkElement CreateDeEsserBottomHintNote()
	{
		var noteTb = new TextBlock
		{
			Text = "增益降低离开 0，就说明这一级正在压齿音；一直是 0，说明检测带还没超过阈值，不是没接上。按住试听只听这一段，松手恢复。",
			FontSize = 12,
			Foreground = DspTextMutedBrush,
			TextWrapping = TextWrapping.Wrap,
			Margin = new Thickness(4, 4, 4, 0)
		};
		_deEsserMutedTextBlocks.Add(noteTb);
		return noteTb;
	}
	#endregion

	#region 8. 实时遥测与 DSP 同步
	/// <summary>
	/// 供宿主 DspPage 定时器或音频回调调用的遥测更新接口。
	/// 实时刷新右侧大号增益降低读数、刻度条、状态判定点、钟形切片与历史轨迹。
	/// </summary>
	public void UpdateDeEsserTelemetry(float grDb)
	{
		_deEsserCurrentGrDb = grDb;
		_deEsserGrHistory[_deEsserGrHistoryIndex] = grDb;
		_deEsserGrHistoryIndex = (_deEsserGrHistoryIndex + 1) % _deEsserGrHistory.Length;

		// 1. 刷新大号读数与刻度条
		if (_deEsserGrReadoutTb != null)
		{
			bool isDark = IsDspDark;
			_deEsserGrReadoutTb.Text = grDb > 0.05f ? $"{grDb:F1}" : "0.0";
			_deEsserGrReadoutTb.Foreground = grDb >= 0.2f
				? GetDeEsserIndigoReadoutBrush(isDark)
				: GetDeEsserPrimaryTextBrush(isDark);
		}
		if (_deEsserGrTrackBar != null)
		{
			double trackW = 140.0;
			if (_deEsserGrTrackBar.Parent is FrameworkElement p && p.ActualWidth > 20.0)
				trackW = p.ActualWidth;
			double norm = Math.Clamp(grDb / 12.0f, 0.0, 1.0);
			_deEsserGrTrackBar.Width = norm * trackW;
		}
		if (_deEsserGrSubTb != null)
		{
			_deEsserGrSubTb.Text = grDb >= 0.2f ? $"增益降低  -{grDb:F1} dB" : "增益降低  当前没有衰减";
		}

		// 2. 刷新状态描述
		UpdateDeEsserVerdict();

		// 3. 刷新检测带削峰动态切片
		UpdateDeEsserBandVisuals();

		// 4. 刷新历史衰减轨迹
		UpdateDeEsserHistoryVisuals();
	}

	private void StartDeEsserTelemetryTimer()
	{
		if (_deEsserTelemetryTimer != null) return;
		_deEsserTelemetryTimer = new DispatcherTimer
		{
			Interval = TimeSpan.FromMilliseconds(40) // ~25fps 平滑遥测
		};
		_deEsserTelemetryTimer.Tick += (s, e) =>
		{
			if (_deEsserWorkbench == null || _deEsserWorkbench.Visibility != Visibility.Visible) return;
			float gr = 0f;
			try
			{
				if (ViewModel?.AudioEngine?.DspChain?.DeEsser != null)
				{
					gr = ViewModel.AudioEngine.DspChain.DeEsser.CurrentGainReductionDb;
				}
			}
			catch { }
			UpdateDeEsserTelemetry(gr);
		};
		_deEsserTelemetryTimer.Start();
	}

	private void SyncDeEsserToEngine()
	{
		try
		{
			ViewModel?.AudioEngine?.SetFurinaDeEsser(
				_deEsserEnabled,
				(float)_deEsserFrequencyHz,
				(float)_deEsserQ,
				(float)_deEsserThresholdDb,
				(float)_deEsserRatio,
				(float)_deEsserKneeDb,
				(float)_deEsserMaxReductionDb,
				(float)_deEsserAttackMs,
				(float)_deEsserReleaseMs,
				(float)(_deEsserMix / 100.0),
				_deEsserAudition
			);
		}
		catch { }
	}

	private void SetDeEsserAuditionState(bool audition)
	{
		if (_deEsserAudition == audition) return;
		_deEsserAudition = audition;
		UpdateDeEsserAuditionButtonVisuals(IsDspDark);
		SyncDeEsserToEngine();
		UpdateDeEsserVerdict();
	}

	public void RefreshDeEsserTheme(bool isDark)
	{
		if (_deEsserWorkbench == null) return;

		_deEsserWorkbench.RequestedTheme = isDark ? ElementTheme.Dark : ElementTheme.Light;
		_deEsserWorkbench.Background = CreateDeEsserSurfaceBrush(LiquidGlassVariant.Clear, isDark, Math.Clamp(0.72 / 0.85, 0.55, 1.0));

		Brush primaryText = GetDeEsserPrimaryTextBrush(isDark);
		Brush secondaryText = GetDeEsserSecondaryTextBrush(isDark);
		Brush mutedText = GetDeEsserMutedTextBrush(isDark);
		Brush accentSoftBg = GetDeEsserAccentSoftBgBrush(isDark);
		Brush accentSoftBorder = GetDeEsserAccentSoftBorderBrush(isDark);
		Brush accentSoftText = GetDeEsserAccentSoftTextBrush(isDark);

		Brush cardBg = CreateDeEsserSurfaceBrush(LiquidGlassVariant.Regular, isDark, Math.Clamp(0.88 / 0.90, 0.72, 1.05));
		Brush rimBrush = CreateDeEsserRimBrush(isDark);
		Brush softBg = GetDeEsserSoftSurfaceBrush(isDark);

		if (_deEsserTitleIconBorder != null)
		{
			_deEsserTitleIconBorder.Background = accentSoftBg;
			_deEsserTitleIconBorder.BorderBrush = accentSoftBorder;
		}
		if (_deEsserTitleIcon != null) _deEsserTitleIcon.Foreground = accentSoftText;

		foreach (var card in _deEsserCardBorders)
		{
			card.Background = cardBg;
			card.BorderBrush = rimBrush;
		}
		foreach (var plot in _deEsserPlotBorders)
		{
			plot.Background = softBg;
			plot.BorderBrush = rimBrush;
		}

		foreach (var tb in _deEsserPrimaryTextBlocks) tb.Foreground = primaryText;
		foreach (var tb in _deEsserSecondaryTextBlocks) tb.Foreground = secondaryText;
		foreach (var tb in _deEsserMutedTextBlocks) tb.Foreground = mutedText;

		if (_deEsserGrReadoutTb != null)
		{
			_deEsserGrReadoutTb.Foreground = _deEsserCurrentGrDb >= 0.2f
				? GetDeEsserIndigoReadoutBrush(isDark)
				: primaryText;
		}
		if (_deEsserGrTrackContainer != null) _deEsserGrTrackContainer.Background = GetDeEsserMeterTrackBrush(isDark);
		if (_deEsserGrTrackBar != null) _deEsserGrTrackBar.Background = GetDeEsserIndigoStrokeBrush(isDark);

		Brush bandGridBrush = GetDeEsserGridLineBrush(isDark, isBand: true);
		foreach (var line in _deEsserBandGridLines) line.Stroke = bandGridBrush;
		if (_deEsserBandBaseLine != null) _deEsserBandBaseLine.Stroke = GetDeEsserBaseLineBrush(isDark);
		if (_deEsserBandAreaPath != null) _deEsserBandAreaPath.Fill = GetDeEsserBandAreaBrush(isDark);
		if (_deEsserBandCutPath != null) _deEsserBandCutPath.Fill = GetDeEsserBandCutBrush(isDark);
		if (_deEsserBandLinePath != null) _deEsserBandLinePath.Stroke = GetDeEsserIndigoStrokeBrush(isDark);
		if (_deEsserBandCenterLine != null) _deEsserBandCenterLine.Stroke = GetDeEsserIndigoStrokeBrush(isDark);

		Brush historyGridBrush = GetDeEsserGridLineBrush(isDark, isBand: false);
		foreach (var line in _deEsserHistoryGridLines) line.Stroke = historyGridBrush;
		if (_deEsserHistoryAreaPath != null) _deEsserHistoryAreaPath.Fill = GetDeEsserHistoryAreaBrush(isDark);
		if (_deEsserHistoryLinePath != null) _deEsserHistoryLinePath.Stroke = GetDeEsserIndigoStrokeBrush(isDark);

		foreach (var slider in _deEsserSliders)
		{
			slider.RequestedTheme = isDark ? ElementTheme.Dark : ElementTheme.Light;
		}

		UpdateDeEsserPowerButtonVisuals(isDark);
		UpdateDeEsserAuditionButtonVisuals(isDark);
		UpdateDeEsserResetButtonVisuals(isDark);
		UpdateDeEsserVerdict(isDark);
	}

	private void UpdateDeEsserPowerButtonVisuals(bool? isDarkOverride = null)
	{
		if (_deEsserPowerBtn == null || _deEsserPowerIcon == null || _deEsserPowerText == null) return;
		bool isDark = isDarkOverride ?? IsDspDark;

		if (_deEsserEnabled)
		{
			_deEsserPowerText.Text = "去齿音开启";
			Brush activeFg = GetDeEsserAccentSoftTextBrush(isDark);
			Brush activeBg = GetDeEsserAccentSoftBgBrush(isDark);
			Brush activeHoverBg = new SolidColorBrush(isDark ? Color.FromArgb(115, 99, 102, 241) : Color.FromArgb(255, 224, 231, 255));
			Brush activePressedBg = new SolidColorBrush(isDark ? Color.FromArgb(145, 99, 102, 241) : Color.FromArgb(255, 199, 210, 254));
			Brush activeBorder = GetDeEsserAccentSoftBorderBrush(isDark);

			_deEsserPowerText.Foreground = activeFg;
			_deEsserPowerIcon.Foreground = activeFg;
			ApplyDeEsserButtonResources(_deEsserPowerBtn, activeBg, activeHoverBg, activePressedBg, activeBorder, activeBorder, activeFg, activeFg);
		}
		else
		{
			_deEsserPowerText.Text = "去齿音旁路";
			Brush textFg = GetDeEsserSubHeaderTextBrush(isDark);
			Brush iconFg = GetDeEsserMutedTextBrush(isDark);
			_deEsserPowerText.Foreground = textFg;
			_deEsserPowerIcon.Foreground = iconFg;
			ApplyDeEsserNeutralButtonSurface(_deEsserPowerBtn, isDark, textFg);
		}
	}

	private void UpdateDeEsserAuditionButtonVisuals(bool? isDarkOverride = null)
	{
		if (_deEsserAuditionBtn == null) return;
		bool isDark = isDarkOverride ?? IsDspDark;

		if (_deEsserAudition)
		{
			Brush activeBg = new SolidColorBrush(Color.FromArgb(255, 99, 102, 241));
			Brush activeHoverBg = new SolidColorBrush(Color.FromArgb(255, 79, 70, 229));
			Brush activePressedBg = new SolidColorBrush(Color.FromArgb(255, 67, 56, 202));
			Brush activeBorder = new SolidColorBrush(isDark ? Color.FromArgb(255, 129, 140, 248) : Color.FromArgb(255, 79, 70, 229));
			Brush activeFg = new SolidColorBrush(Colors.White);

			if (_deEsserAuditionText != null) _deEsserAuditionText.Foreground = activeFg;
			if (_deEsserAuditionIcon != null) _deEsserAuditionIcon.Foreground = activeFg;
			ApplyDeEsserButtonResources(_deEsserAuditionBtn, activeBg, activeHoverBg, activePressedBg, activeBorder, activeBorder, activeFg, activeFg);
		}
		else
		{
			Brush textFg = GetDeEsserSecondaryTextBrush(isDark);
			Brush iconFg = GetDeEsserSubHeaderTextBrush(isDark);
			if (_deEsserAuditionText != null) _deEsserAuditionText.Foreground = textFg;
			if (_deEsserAuditionIcon != null) _deEsserAuditionIcon.Foreground = iconFg;
			ApplyDeEsserNeutralButtonSurface(_deEsserAuditionBtn, isDark, textFg);
		}
	}

	private void UpdateDeEsserResetButtonVisuals(bool? isDarkOverride = null)
	{
		if (_deEsserResetBtn == null) return;
		bool isDark = isDarkOverride ?? IsDspDark;
		Brush textFg = GetDeEsserSecondaryTextBrush(isDark);
		Brush iconFg = GetDeEsserSubHeaderTextBrush(isDark);
		if (_deEsserResetText != null) _deEsserResetText.Foreground = textFg;
		if (_deEsserResetIcon != null) _deEsserResetIcon.Foreground = iconFg;
		ApplyDeEsserNeutralButtonSurface(_deEsserResetBtn, isDark, textFg);
	}

	private void UpdateDeEsserVerdict(bool? isDarkOverride = null)
	{
		bool isDark = isDarkOverride ?? IsDspDark;
		bool isPlaying = ViewModel?.AudioEngine?.State == PlaybackState.Playing;
		string title;
		string detail;
		Color dotColor;

		if (_deEsserAudition)
		{
			title = "正在只听齿音带";
			detail = "用来确认频率有没有对准齿音。松开按钮恢复整段声音，试听不会写入存档。";
			dotColor = isDark ? Color.FromArgb(255, 129, 140, 248) : Color.FromArgb(255, 99, 102, 241);
		}
		else if (!_deEsserEnabled)
		{
			title = "已旁路";
			detail = "齿音会原样通过。打开之后，只有检测带里超出阈值的成分会被衰减。";
			dotColor = Color.FromArgb(255, 148, 163, 184);
		}
		else if (_deEsserMix <= 0.001)
		{
			title = "湿声是 0%";
			detail = "模块开着，但处理结果没有混进输出，听起来和旁路一样。把湿声拉高才会起作用。";
			dotColor = Color.FromArgb(255, 245, 158, 11);
		}
		else if (!isPlaying)
		{
			title = "已接入，等待播放开始";
			detail = "模块已经在链路里。开始播放后，超过阈值时右边的增益降低会离开 0。";
			dotColor = isDark ? Color.FromArgb(255, 96, 165, 250) : Color.FromArgb(255, 59, 130, 246);
		}
		else if (_deEsserCurrentGrDb >= 0.2f)
		{
			title = "正在衰减齿音";
			detail = "检测带里超出阈值的成分正在被压下去，其余频段保持原样。";
			dotColor = isDark ? Color.FromArgb(255, 52, 211, 153) : Color.FromArgb(255, 16, 185, 129);
		}
		else
		{
			title = "已接入，还没触发";
			detail = $"播放中，检测带还没超过 {_deEsserThresholdDb:F1} dB，所以增益降低停在 0。齿音还在的话，把频率对准齿音，或把阈值再调低。";
			dotColor = isDark ? Color.FromArgb(255, 129, 140, 248) : Color.FromArgb(255, 99, 102, 241);
		}

		if (_deEsserStatusTitleTb != null) _deEsserStatusTitleTb.Text = title;
		if (_deEsserStatusDetailTb != null) _deEsserStatusDetailTb.Text = detail;
		if (_deEsserStatusDot != null) _deEsserStatusDot.Fill = new SolidColorBrush(dotColor);
	}

	private void SyncDeEsserControls()
	{
		if (_sliderDeEsserFreq != null) _sliderDeEsserFreq.Value = _deEsserFrequencyHz;
		if (_tbDeEsserFreqVal != null) _tbDeEsserFreqVal.Text = $"{_deEsserFrequencyHz:F0} Hz";

		if (_sliderDeEsserQ != null) _sliderDeEsserQ.Value = _deEsserQ;
		if (_tbDeEsserQVal != null) _tbDeEsserQVal.Text = $"{_deEsserQ:F1}";

		if (_sliderDeEsserThresh != null) _sliderDeEsserThresh.Value = _deEsserThresholdDb;
		if (_tbDeEsserThreshVal != null) _tbDeEsserThreshVal.Text = $"{_deEsserThresholdDb:F1} dB";

		if (_sliderDeEsserRatio != null) _sliderDeEsserRatio.Value = _deEsserRatio;
		if (_tbDeEsserRatioVal != null) _tbDeEsserRatioVal.Text = $"{_deEsserRatio:F1} :1";

		if (_sliderDeEsserKnee != null) _sliderDeEsserKnee.Value = _deEsserKneeDb;
		if (_tbDeEsserKneeVal != null) _tbDeEsserKneeVal.Text = $"{_deEsserKneeDb:F1} dB";

		if (_sliderDeEsserMaxReduction != null) _sliderDeEsserMaxReduction.Value = _deEsserMaxReductionDb;
		if (_tbDeEsserMaxReductionVal != null) _tbDeEsserMaxReductionVal.Text = $"{_deEsserMaxReductionDb:F1} dB";

		if (_sliderDeEsserMix != null) _sliderDeEsserMix.Value = _deEsserMix;
		if (_tbDeEsserMixVal != null) _tbDeEsserMixVal.Text = $"{_deEsserMix:F0} %";

		if (_sliderDeEsserAttack != null) _sliderDeEsserAttack.Value = _deEsserAttackMs;
		if (_tbDeEsserAttackVal != null) _tbDeEsserAttackVal.Text = $"{_deEsserAttackMs:F1} ms";

		if (_sliderDeEsserRelease != null) _sliderDeEsserRelease.Value = _deEsserReleaseMs;
		if (_tbDeEsserReleaseVal != null) _tbDeEsserReleaseVal.Text = $"{_deEsserReleaseMs:F0} ms";

		if (_deEsserBandHeaderSubTb != null)
		{
			double bw = Math.Round(_deEsserFrequencyHz / Math.Max(0.5, _deEsserQ));
			_deEsserBandHeaderSubTb.Text = $"约 {bw:F0} Hz 宽 · 拖动曲线改变频率";
		}

		bool isDark = IsDspDark;
		UpdateDeEsserPowerButtonVisuals(isDark);
		UpdateDeEsserAuditionButtonVisuals(isDark);
		UpdateDeEsserResetButtonVisuals(isDark);
	}
	#endregion

	#region 9. 辅助坐标转换与控件工厂
	private static Brush CreateDeEsserSurfaceBrush(LiquidGlassVariant variant, bool isDark, double opacityScale = 1.0)
		=> LiquidGlassEngine.CreateSurfaceBrush(variant, isDark: isDark, opacityScale: opacityScale);

	private static Brush CreateDeEsserRimBrush(bool isDark, bool isHovered = false, bool isSelected = false)
		=> LiquidGlassEngine.CreateSpecularRimBrush(LiquidGlassVariant.Regular, isDark: isDark, isHovered: isHovered, isSelected: isSelected);

	private static Brush GetDeEsserSoftSurfaceBrush(bool isDark)
		=> isDark
			? LiquidGlassEngine.CreateSurfaceBrush(LiquidGlassVariant.Clear, isDark: true, opacityScale: 0.45)
			: new SolidColorBrush(Color.FromArgb(220, 241, 245, 249));

	private static Brush GetDeEsserPrimaryTextBrush(bool isDark)
		=> new SolidColorBrush(isDark ? Colors.White : Color.FromArgb(255, 15, 23, 42));

	private static Brush GetDeEsserSecondaryTextBrush(bool isDark)
		=> new SolidColorBrush(isDark ? Color.FromArgb(255, 248, 250, 252) : Color.FromArgb(255, 51, 65, 85));

	private static Brush GetDeEsserSubHeaderTextBrush(bool isDark)
		=> new SolidColorBrush(isDark ? Color.FromArgb(255, 226, 232, 240) : Color.FromArgb(255, 71, 85, 105));

	private static Brush GetDeEsserMutedTextBrush(bool isDark)
		=> new SolidColorBrush(isDark ? Color.FromArgb(255, 203, 213, 225) : Color.FromArgb(255, 100, 116, 139));

	private static Brush GetDeEsserAccentSoftBgBrush(bool isDark)
		=> new SolidColorBrush(isDark ? Color.FromArgb(85, 99, 102, 241) : Color.FromArgb(255, 238, 242, 255));

	private static Brush GetDeEsserAccentSoftBorderBrush(bool isDark)
		=> new SolidColorBrush(isDark ? Color.FromArgb(180, 129, 140, 248) : Color.FromArgb(255, 199, 210, 254));

	private static Brush GetDeEsserAccentSoftTextBrush(bool isDark)
		=> new SolidColorBrush(isDark ? Color.FromArgb(255, 199, 210, 254) : Color.FromArgb(255, 79, 70, 229));

	private static Brush GetDeEsserMeterTrackBrush(bool isDark)
		=> new SolidColorBrush(isDark ? Color.FromArgb(70, 255, 255, 255) : Color.FromArgb(255, 226, 232, 240));

	private static Brush GetDeEsserIndigoStrokeBrush(bool isDark)
		=> new SolidColorBrush(isDark ? Color.FromArgb(255, 129, 140, 248) : Color.FromArgb(255, 99, 102, 241));

	private static Brush GetDeEsserIndigoReadoutBrush(bool isDark)
		=> new SolidColorBrush(isDark ? Color.FromArgb(255, 165, 180, 252) : Color.FromArgb(255, 99, 102, 241));

	private static Brush GetDeEsserBandAreaBrush(bool isDark)
		=> new SolidColorBrush(Color.FromArgb(isDark ? (byte)48 : (byte)36, 99, 102, 241));

	private static Brush GetDeEsserBandCutBrush(bool isDark)
		=> new SolidColorBrush(Color.FromArgb(isDark ? (byte)115 : (byte)95, 99, 102, 241));

	private static Brush GetDeEsserHistoryAreaBrush(bool isDark)
		=> new SolidColorBrush(Color.FromArgb(isDark ? (byte)44 : (byte)30, 99, 102, 241));

	private static Brush GetDeEsserGridLineBrush(bool isDark, bool isBand)
		=> new SolidColorBrush(Color.FromArgb(isBand ? (isDark ? (byte)64 : (byte)50) : (isDark ? (byte)56 : (byte)40), 148, 163, 184));

	private static Brush GetDeEsserBaseLineBrush(bool isDark)
		=> new SolidColorBrush(Color.FromArgb(isDark ? (byte)115 : (byte)90, 148, 163, 184));

	private Border CreateDeEsserCardBorder(bool isDark, Thickness padding)
	{
		var card = new Border
		{
			Background = CreateDeEsserSurfaceBrush(LiquidGlassVariant.Regular, isDark, Math.Clamp(0.88 / 0.90, 0.72, 1.05)),
			BorderBrush = CreateDeEsserRimBrush(isDark),
			BorderThickness = new Thickness(1),
			CornerRadius = new CornerRadius(12),
			Padding = padding
		};
		_deEsserCardBorders.Add(card);
		return card;
	}

	private static void ApplyDeEsserNeutralButtonSurface(Button btn, bool isDark, Brush foregroundBrush)
	{
		Brush bg = CreateDeEsserSurfaceBrush(LiquidGlassVariant.Clear, isDark, 0.92);
		Brush hoverBg = new SolidColorBrush(isDark ? Color.FromArgb(185, 51, 65, 85) : Color.FromArgb(240, 241, 245, 249));
		Brush pressedBg = new SolidColorBrush(isDark ? Color.FromArgb(215, 30, 41, 59) : Color.FromArgb(245, 226, 232, 240));
		Brush border = CreateDeEsserRimBrush(isDark);
		Brush hoverBorder = CreateDeEsserRimBrush(isDark, isHovered: true);
		Brush hoverFg = GetDeEsserPrimaryTextBrush(isDark);

		ApplyDeEsserButtonResources(btn, bg, hoverBg, pressedBg, border, hoverBorder, foregroundBrush, hoverFg);
	}

	private static void ApplyDeEsserButtonResources(
		Button btn,
		Brush background,
		Brush pointerOverBackground,
		Brush pressedBackground,
		Brush borderBrush,
		Brush pointerOverBorderBrush,
		Brush foreground,
		Brush pointerOverForeground)
	{
		btn.Background = background;
		btn.BorderBrush = borderBrush;
		btn.BorderThickness = new Thickness(1);
		btn.Foreground = foreground;

		btn.Resources["ButtonBackground"] = background;
		btn.Resources["ButtonBackgroundPointerOver"] = pointerOverBackground;
		btn.Resources["ButtonBackgroundPressed"] = pressedBackground;
		btn.Resources["ButtonBorderBrush"] = borderBrush;
		btn.Resources["ButtonBorderBrushPointerOver"] = pointerOverBorderBrush;
		btn.Resources["ButtonBorderBrushPressed"] = pointerOverBorderBrush;
		btn.Resources["ButtonForeground"] = foreground;
		btn.Resources["ButtonForegroundPointerOver"] = pointerOverForeground;
		btn.Resources["ButtonForegroundPressed"] = pointerOverForeground;
	}

	private static double XForDeEsserHz(double hz)
	{
		const double MarginB = 18.0;
		const double Span = 604.0;
		const double MinHz = 2000.0;
		const double MaxHz = 12000.0;
		double logSpan = Math.Log(MaxHz) - Math.Log(MinHz);

		double clamped = Math.Clamp(hz, MinHz, MaxHz);
		double norm = (Math.Log(clamped) - Math.Log(MinHz)) / logSpan;
		return MarginB + norm * Span;
	}

	private static double HzForDeEsserX(double x)
	{
		const double MarginB = 18.0;
		const double Span = 604.0;
		const double MinHz = 2000.0;
		const double MaxHz = 12000.0;
		double logSpan = Math.Log(MaxHz) - Math.Log(MinHz);

		double norm = Math.Clamp((x - MarginB) / Span, 0.0, 1.0);
		return Math.Exp(Math.Log(MinHz) + norm * logSpan);
	}

	private static string FormatDeEsserFrequency(double freq)
	{
		if (freq >= 1000.0)
		{
			double khz = Math.Round(freq / 10.0) / 100.0;
			return $"{khz:0.0#} kHz";
		}
		return $"{freq:F0} Hz";
	}

	private static TextBlock AddDeEsserCanvasText(Canvas canvas, string text, double left, double top, Brush foreground, FontFamily font, double fontSize)
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
		return tb;
	}

	private FrameworkElement CreateDeEsserSliderControl(
		string label,
		string initialValueText,
		double min,
		double max,
		double step,
		double initialValue,
		out Slider slider,
		out TextBlock valueTb,
		Action<double> onValueChanged)
	{
		var sp = new StackPanel { Spacing = 6 };

		var head = new Grid();
		head.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.0, GridUnitType.Star) });
		head.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

		var tbLabel = new TextBlock
		{
			Text = label,
			FontSize = 13,
			FontWeight = FontWeights.SemiBold,
			Foreground = DspTextSecondaryBrush
		};
		_deEsserSecondaryTextBlocks.Add(tbLabel);
		Grid.SetColumn(tbLabel, 0);
		head.Children.Add(tbLabel);

		valueTb = new TextBlock
		{
			Text = initialValueText,
			FontSize = 13,
			FontWeight = FontWeights.Bold,
			Foreground = DspTextPrimaryBrush,
			FontFamily = new FontFamily("Segoe UI Variable Text, Consolas, Segoe UI")
		};
		_deEsserPrimaryTextBlocks.Add(valueTb);
		Grid.SetColumn(valueTb, 1);
		head.Children.Add(valueTb);
		sp.Children.Add(head);

		slider = new Slider
		{
			Minimum = min,
			Maximum = max,
			StepFrequency = step,
			Value = initialValue,
			Margin = new Thickness(0, -2, 0, 0),
			RequestedTheme = IsDspDark ? ElementTheme.Dark : ElementTheme.Light
		};
		_deEsserSliders.Add(slider);
		slider.ValueChanged += (s, e) => onValueChanged(e.NewValue);
		sp.Children.Add(slider);

		return sp;
	}
	#endregion
}
