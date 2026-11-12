using System;
using System.Collections.Generic;
using FurinaPlayer.UI.Controls;
using FurinaPlayer.UI.Helpers;
using FurinaPlayer.UI.ViewModels;
using Microsoft.UI;
using Microsoft.UI.Input;
using Microsoft.UI.Text;
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
/// FurinaPlayer 交叉馈送 (Crossfeed) 二级 DSP 功能工作台分部实现。
/// 1:1 像素级还原原生侧声道频率响应曲线、Mid/Side 域低频衰减、Bauer BS2B 与 Chu Moy 经典算法、4个试听预设及参数控制流。
/// </summary>
public sealed partial class DspPage
{
	// 核心工作台容器与参数状态
	private FrameworkElement? _crossfeedWorkbench;
	private Border? _crossfeedRootBorder;
	private bool _crossfeedEnabled = false;
	private double _crossfeedAmount = 0.25;
	private double _crossfeedCutoffHz = 700.0;
	private string _crossfeedModel = "mid_side"; // "mid_side" / "bs2b" / "chu_moy"

	// 草稿参数与交互状态 (Draft State)
	private bool _draftCrossfeedEnabled = false;
	private double _draftCrossfeedAmount = 0.25;
	private double _draftCrossfeedCutoffHz = 700.0;
	private string _draftCrossfeedModel = "mid_side";
	private bool _isCrossfeedDirty = false;
	private bool _isDraggingCrossfeedCutoff = false;

	// 顶部操作栏控件
	private Border? _crossfeedHeaderIconBorder;
	private FontIcon? _crossfeedHeaderIcon;
	private Button? _crossfeedPowerBtn;
	private FontIcon? _crossfeedPowerIcon;
	private TextBlock? _crossfeedPowerText;
	private Button? _crossfeedResetBtn;
	private FontIcon? _crossfeedResetIcon;
	private TextBlock? _crossfeedResetText;
	private Button? _crossfeedApplyBtn;
	private FontIcon? _crossfeedApplyIcon;
	private TextBlock? _crossfeedApplyText;
	private Border? _crossfeedPendingBadge;
	private TextBlock? _crossfeedPendingBadgeText;
	private Border? _crossfeedParamModelPill;
	private TextBlock? _crossfeedParamModelText;

	// 响应曲线监视器控件
	private Border? _crossfeedChartStageBorder;
	private Border? _crossfeedDragHintTag;
	private TextBlock? _crossfeedDragHintArrowTb;
	private TextBlock? _crossfeedDragHintTextTb;
	private TextBlock? _crossfeedChartSubtitleTb;
	private Canvas? _crossfeedChartCanvas;
	private Canvas? _crossfeedRulerCanvas;
	private Path? _crossfeedCurvePath;
	private Path? _crossfeedAreaPath;
	private Path? _crossfeedGhostPath;
	private Line? _crossfeedCutoffLine;
	private Border? _crossfeedCutoffKnob;
	private Border? _crossfeedCutoffBadge;
	private TextBlock? _crossfeedCutoffBadgeTb;
	private TextBlock? _crossfeedMidLabelTb;
	private TextBlock? _crossfeedSideLabelTb;
	private TextBlock? _crossfeedTickMinus2Tb;
	private TextBlock? _crossfeedTickMinus4Tb;
	private Line? _crossfeedGridLine0;
	private Line? _crossfeedGridLineMinus2;
	private Line? _crossfeedGridLineMinus4;
	private Line? _crossfeedGridLine100;
	private Line? _crossfeedGridLine1k;
	private Line? _crossfeedGridLine10k;

	// 试听预设胶囊按钮
	private readonly List<Button> _crossfeedPresetButtons = new();
	private Button? _btnPresetSubtle;
	private Button? _btnPresetNatural;
	private Button? _btnPresetStrong;
	private Button? _btnPresetSpeaker;

	// 参数控制控件
	private Border? _crossfeedDividerBorder;
	private ComboBox? _crossfeedModelCombo;
	private Slider? _crossfeedAmountSlider;
	private TextBlock? _crossfeedAmountValueTb;
	private Slider? _crossfeedCutoffSlider;
	private TextBlock? _crossfeedCutoffValueTb;

	// 底部大号读数卡片
	private TextBlock? _statFeedValueTb;
	private TextBlock? _statLowCutValueTb;
	private TextBlock? _statCutoffValueTb;

	// 主题追踪集合
	private readonly List<Border> _crossfeedCardBorders = new();
	private readonly List<TextBlock> _crossfeedPrimaryTextBlocks = new();
	private readonly List<TextBlock> _crossfeedMutedTextBlocks = new();

	// 互斥冲突提示
	private InfoBar? _crossfeedMutualExclusionInfoBar;

	/// <summary>
	/// 创建并返回交叉馈送工作台根元素 (Border/ScrollViewer)。
	/// </summary>
	public FrameworkElement CreateCrossfeedWorkbench()
	{
		if (_crossfeedWorkbench != null)
		{
			return _crossfeedWorkbench;
		}

		InitCrossfeedState();
		bool isDark = IsDspDark;

		var rootBorder = new Border
		{
			Background = CreateCrossfeedSurfaceBrush(LiquidGlassVariant.Clear, isDark, Math.Clamp(0.72 / 0.85, 0.55, 1.0)),
			CornerRadius = new CornerRadius(0),
			RequestedTheme = isDark ? ElementTheme.Dark : ElementTheme.Light
		};
		_crossfeedRootBorder = rootBorder;

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

		// 1. 顶部 Header 栏 (标题、模块计数、就绪指示、刷新)
		mainStack.Children.Add(CreateCrossfeedHeaderBar());

		// 2. 顶部工具栏 (开关按钮、恢复默认、应用参数、PARAM MODEL 胶囊标)
		mainStack.Children.Add(CreateCrossfeedActionToolbar());

		// 3. 描述说明文本
		var descTb = new TextBlock
		{
			Text = "在 Mid / Side 域收窄低频左右差异，保留高频定位，缓解耳机硬分离。",
			FontSize = 13,
			Foreground = GetCrossfeedMutedTextBrush(isDark),
			Margin = new Thickness(2, -4, 0, 4)
		};
		_crossfeedMutedTextBlocks.Add(descTb);
		mainStack.Children.Add(descTb);

		// 4. 互斥冲突提示条 (声学联动)
		_crossfeedMutualExclusionInfoBar = new InfoBar
		{
			IsOpen = false,
			Severity = InfoBarSeverity.Informational,
			Title = "声学互斥联动",
			Message = "已启用交叉馈送，并自动关闭了冲突的虚拟声场处理模块以确保纯净监听。",
			Margin = new Thickness(0, 0, 0, 6)
		};
		mainStack.Children.Add(_crossfeedMutualExclusionInfoBar);

		// 5. 核心两列布局 (左: 侧声道频率响应图, 右: 试听预设与参数控制)
		mainStack.Children.Add(CreateCrossfeedMainStage());

		// 6. 底部三联大号读数卡片
		mainStack.Children.Add(CreateCrossfeedStatsSection());

		// 7. 底部说明提示
		var footerHintTb = new TextBlock
		{
			Text = "预设立即应用并启用效果；滑块和图上拖动需点击「应用参数」或按 Enter。曲线显示参数响应，不是实时声音测量。",
			FontSize = 11.5,
			Foreground = GetCrossfeedMutedTextBrush(isDark),
			Margin = new Thickness(2, 4, 0, 0)
		};
		_crossfeedMutedTextBlocks.Add(footerHintTb);
		mainStack.Children.Add(footerHintTb);

		sv.Content = mainStack;
		rootBorder.Child = sv;

		// 键盘快捷键监听: Enter 应用参数, Esc 放弃草稿
		rootBorder.KeyDown += (s, e) =>
		{
			if (e.Key == Windows.System.VirtualKey.Enter)
			{
				OnCrossfeedApplyParametersClick(this, new RoutedEventArgs());
				e.Handled = true;
			}
			else if (e.Key == Windows.System.VirtualKey.Escape && _isCrossfeedDirty)
			{
				RevertCrossfeedDraft();
				e.Handled = true;
			}
		};

		_crossfeedWorkbench = rootBorder;
		_spatialCrossfeedPanel = rootBorder;

		// 初始渲染
		RefreshCrossfeedTheme(isDark);

		return _crossfeedWorkbench;
	}

	private void InitCrossfeedState()
	{
		if (ViewModel != null)
		{
			_crossfeedEnabled = ViewModel.EchoCrossfeedEnabled;
			_crossfeedAmount = ViewModel.EchoCrossfeedAmount > 0.0 ? ViewModel.EchoCrossfeedAmount : 0.25;
			_crossfeedCutoffHz = ViewModel.EchoCrossfeedCutoffHz > 0.0 ? ViewModel.EchoCrossfeedCutoffHz : 700.0;
		}
		else
		{
			_crossfeedEnabled = false;
			_crossfeedAmount = 0.25;
			_crossfeedCutoffHz = 700.0;
		}

		_crossfeedModel = "mid_side";
		_draftCrossfeedEnabled = _crossfeedEnabled;
		_draftCrossfeedAmount = _crossfeedAmount;
		_draftCrossfeedCutoffHz = _crossfeedCutoffHz;
		_draftCrossfeedModel = _crossfeedModel;
		_isCrossfeedDirty = false;
	}

	#region 1. Top Header Bar
	private FrameworkElement CreateCrossfeedHeaderBar()
	{
		bool isDark = IsDspDark;
		var headerGrid = new Grid();
		headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.0, GridUnitType.Star) });
		headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

		var titleSp = new StackPanel
		{
			Orientation = Orientation.Horizontal,
			Spacing = 12,
			VerticalAlignment = VerticalAlignment.Center
		};

		_crossfeedHeaderIcon = new FontIcon
		{
			Glyph = "\uE895",
			FontSize = 18,
			Foreground = GetCrossfeedAccentSoftTextBrush(isDark),
			HorizontalAlignment = HorizontalAlignment.Center,
			VerticalAlignment = VerticalAlignment.Center
		};
		_crossfeedHeaderIconBorder = new Border
		{
			Width = 36,
			Height = 36,
			CornerRadius = new CornerRadius(18),
			Background = GetCrossfeedAccentSoftBgBrush(isDark),
			Child = _crossfeedHeaderIcon
		};
		titleSp.Children.Add(_crossfeedHeaderIconBorder);

		var titleTb = new TextBlock
		{
			Text = "交叉馈送",
			FontSize = 22,
			FontWeight = FontWeights.Bold,
			Foreground = GetCrossfeedPrimaryTextBrush(isDark),
			VerticalAlignment = VerticalAlignment.Center
		};
		_crossfeedPrimaryTextBlocks.Add(titleTb);
		titleSp.Children.Add(titleTb);
		Grid.SetColumn(titleSp, 0);
		headerGrid.Children.Add(titleSp);

		return headerGrid;
	}
	#endregion

	#region 2. Top Action Toolbar
	private FrameworkElement CreateCrossfeedActionToolbar()
	{
		bool isDark = IsDspDark;
		var toolbarGrid = new Grid();
		toolbarGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.0, GridUnitType.Star) });
		toolbarGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

		var leftStack = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 10, VerticalAlignment = VerticalAlignment.Center };

		// 开关按钮: 【已旁路 · 点击试听】/【已开启】
		_crossfeedPowerBtn = new Button
		{
			Padding = new Thickness(14, 7, 16, 7),
			CornerRadius = new CornerRadius(8),
			BorderThickness = new Thickness(1)
		};
		var powerSp = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
		_crossfeedPowerIcon = new FontIcon { Glyph = "\uE7E8", FontSize = 13 };
		_crossfeedPowerText = new TextBlock { Text = "已旁路 · 点击试听", FontSize = 13, FontWeight = FontWeights.SemiBold };
		powerSp.Children.Add(_crossfeedPowerIcon);
		powerSp.Children.Add(_crossfeedPowerText);
		_crossfeedPowerBtn.Content = powerSp;
		_crossfeedPowerBtn.Click += OnToggleCrossfeedPower;
		leftStack.Children.Add(_crossfeedPowerBtn);

		// 【恢复默认】按钮
		_crossfeedResetIcon = new FontIcon { Glyph = "\uE777", FontSize = 12, Foreground = GetCrossfeedSubHeaderTextBrush(isDark) };
		_crossfeedResetText = new TextBlock { Text = "恢复默认", FontSize = 13, Foreground = GetCrossfeedSecondaryTextBrush(isDark) };
		_crossfeedResetBtn = new Button
		{
			Content = new StackPanel
			{
				Orientation = Orientation.Horizontal,
				Spacing = 6,
				Children =
				{
					_crossfeedResetIcon,
					_crossfeedResetText
				}
			},
			Padding = new Thickness(12, 7, 14, 7),
			CornerRadius = new CornerRadius(8),
			BorderThickness = new Thickness(1)
		};
		ApplyCrossfeedNeutralButtonStyle(_crossfeedResetBtn, isDark, _crossfeedResetIcon, _crossfeedResetText);
		_crossfeedResetBtn.Click += OnResetCrossfeedClick;
		leftStack.Children.Add(_crossfeedResetBtn);

		// 【应用参数】按钮
		_crossfeedApplyBtn = new Button
		{
			Padding = new Thickness(14, 7, 16, 7),
			CornerRadius = new CornerRadius(8),
			BorderThickness = new Thickness(1)
		};
		_crossfeedApplyIcon = new FontIcon { Glyph = "\uE74E", FontSize = 12 };
		_crossfeedApplyText = new TextBlock { Text = "应用参数", FontSize = 13, FontWeight = FontWeights.SemiBold };
		var applySp = new StackPanel
		{
			Orientation = Orientation.Horizontal,
			Spacing = 6,
			Children =
			{
				_crossfeedApplyIcon,
				_crossfeedApplyText
			}
		};
		_crossfeedApplyBtn.Content = applySp;
		_crossfeedApplyBtn.Click += OnCrossfeedApplyParametersClick;
		leftStack.Children.Add(_crossfeedApplyBtn);

		// 未应用角标 (Pending Badge)
		_crossfeedPendingBadgeText = new TextBlock
		{
			Text = "未应用",
			FontSize = 11,
			FontWeight = FontWeights.SemiBold
		};
		_crossfeedPendingBadge = new Border
		{
			BorderThickness = new Thickness(1),
			CornerRadius = new CornerRadius(10),
			Padding = new Thickness(8, 2, 8, 2),
			VerticalAlignment = VerticalAlignment.Center,
			Visibility = Visibility.Collapsed,
			Child = _crossfeedPendingBadgeText
		};
		ApplyCrossfeedPendingBadgeTheme(isDark);
		leftStack.Children.Add(_crossfeedPendingBadge);

		Grid.SetColumn(leftStack, 0);
		toolbarGrid.Children.Add(leftStack);

		// 右上角胶囊标: PARAM MODEL
		_crossfeedParamModelText = new TextBlock
		{
			Text = "PARAM MODEL",
			FontSize = 11,
			FontWeight = FontWeights.SemiBold,
			Foreground = GetCrossfeedMutedTextBrush(isDark)
		};
		_crossfeedMutedTextBlocks.Add(_crossfeedParamModelText);
		_crossfeedParamModelPill = new Border
		{
			Background = GetCrossfeedSoftSurfaceBrush(isDark),
			BorderBrush = CreateCrossfeedRimBrush(isDark),
			BorderThickness = new Thickness(1),
			CornerRadius = new CornerRadius(12),
			Padding = new Thickness(12, 4, 12, 4),
			VerticalAlignment = VerticalAlignment.Center,
			Child = _crossfeedParamModelText
		};
		Grid.SetColumn(_crossfeedParamModelPill, 1);
		toolbarGrid.Children.Add(_crossfeedParamModelPill);

		return toolbarGrid;
	}
	#endregion

	#region 3. Main Stage (Left: Chart, Right: Presets & Controls)
	private FrameworkElement CreateCrossfeedMainStage()
	{
		var stageGrid = new Grid { ColumnSpacing = 20 };
		stageGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.15, GridUnitType.Star) });
		stageGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(0.95, GridUnitType.Star) });

		// 左侧: 侧声道频率响应图
		var chartCard = CreateCrossfeedChartCard();
		Grid.SetColumn(chartCard, 0);
		stageGrid.Children.Add(chartCard);

		// 右侧: 试听预设与参数控制卡片
		var controlsCard = CreateCrossfeedPresetsAndControlsCard();
		Grid.SetColumn(controlsCard, 1);
		stageGrid.Children.Add(controlsCard);

		return stageGrid;
	}

	private FrameworkElement CreateCrossfeedChartCard()
	{
		bool isDark = IsDspDark;
		var card = new Border
		{
			Background = CreateCrossfeedSurfaceBrush(LiquidGlassVariant.Regular, isDark, Math.Clamp(0.88 / 0.90, 0.72, 1.05)),
			BorderBrush = CreateCrossfeedRimBrush(isDark),
			BorderThickness = new Thickness(1),
			CornerRadius = new CornerRadius(12),
			Padding = new Thickness(18, 16, 18, 16)
		};
		_crossfeedCardBorders.Add(card);

		var stack = new StackPanel { Spacing = 12 };

		// 标题行
		var headGrid = new Grid();
		headGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.0, GridUnitType.Star) });
		headGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

		var titleStack = new StackPanel { Spacing = 2 };
		var chartTitleTb = new TextBlock
		{
			Text = "侧声道频率响应",
			FontSize = 14,
			FontWeight = FontWeights.SemiBold,
			Foreground = GetCrossfeedPrimaryTextBrush(isDark)
		};
		_crossfeedPrimaryTextBlocks.Add(chartTitleTb);
		titleStack.Children.Add(chartTitleTb);

		var chartDescTb = new TextBlock
		{
			Text = "按当前馈送量与低通截止推导的 Side 通道衰减曲线；在图上拖动可直接调整截止频率。",
			FontSize = 11.5,
			Foreground = GetCrossfeedMutedTextBrush(isDark)
		};
		_crossfeedMutedTextBlocks.Add(chartDescTb);
		titleStack.Children.Add(chartDescTb);
		Grid.SetColumn(titleStack, 0);
		headGrid.Children.Add(titleStack);

		_crossfeedChartSubtitleTb = new TextBlock
		{
			Text = "25% · 700 Hz · 自然",
			FontSize = 12,
			FontWeight = FontWeights.SemiBold,
			Foreground = GetCrossfeedMutedTextBrush(isDark),
			VerticalAlignment = VerticalAlignment.Center
		};
		_crossfeedMutedTextBlocks.Add(_crossfeedChartSubtitleTb);
		Grid.SetColumn(_crossfeedChartSubtitleTb, 1);
		headGrid.Children.Add(_crossfeedChartSubtitleTb);

		stack.Children.Add(headGrid);

		// 绘图区域容器
		_crossfeedChartStageBorder = new Border
		{
			Height = 270,
			Background = GetCrossfeedSoftSurfaceBrush(isDark),
			BorderBrush = CreateCrossfeedRimBrush(isDark),
			BorderThickness = new Thickness(1),
			CornerRadius = new CornerRadius(8),
			Clip = new RectangleGeometry { Rect = new Rect(0, 0, 1920, 270) }
		};

		var stageGrid = new Grid();

		// 主 Canvas
		_crossfeedChartCanvas = new Canvas
		{
			HorizontalAlignment = HorizontalAlignment.Stretch,
			VerticalAlignment = VerticalAlignment.Stretch,
			Background = new SolidColorBrush(Colors.Transparent)
		};

		// 辅助网格线
		var gridLineBrush = GetCrossfeedChartGridBrush(isDark);
		_crossfeedGridLine0 = new Line { Stroke = gridLineBrush, StrokeThickness = 1 };
		_crossfeedGridLineMinus2 = new Line { Stroke = gridLineBrush, StrokeThickness = 1 };
		_crossfeedGridLineMinus4 = new Line { Stroke = gridLineBrush, StrokeThickness = 1 };
		_crossfeedGridLine100 = new Line { Stroke = gridLineBrush, StrokeThickness = 1 };
		_crossfeedGridLine1k = new Line { Stroke = gridLineBrush, StrokeThickness = 1 };
		_crossfeedGridLine10k = new Line { Stroke = gridLineBrush, StrokeThickness = 1 };

		_crossfeedChartCanvas.Children.Add(_crossfeedGridLine0);
		_crossfeedChartCanvas.Children.Add(_crossfeedGridLineMinus2);
		_crossfeedChartCanvas.Children.Add(_crossfeedGridLineMinus4);
		_crossfeedChartCanvas.Children.Add(_crossfeedGridLine100);
		_crossfeedChartCanvas.Children.Add(_crossfeedGridLine1k);
		_crossfeedChartCanvas.Children.Add(_crossfeedGridLine10k);

		// 填充面与曲线
		_crossfeedAreaPath = new Path
		{
			Fill = new LinearGradientBrush
			{
				StartPoint = new Point(0, 0),
				EndPoint = new Point(0, 1),
				GradientStops =
				{
					new GradientStop { Color = Color.FromArgb(45, 129, 140, 248), Offset = 0.0 },
					new GradientStop { Color = Color.FromArgb(6, 129, 140, 248), Offset = 1.0 }
				}
			}
		};
		_crossfeedCurvePath = new Path
		{
			Stroke = new SolidColorBrush(Color.FromArgb(255, 129, 140, 248)),
			StrokeThickness = 2.0
		};
		_crossfeedGhostPath = new Path
		{
			Stroke = new SolidColorBrush(Color.FromArgb(140, 148, 163, 184)),
			StrokeThickness = 1.5,
			StrokeDashArray = new DoubleCollection { 3, 2 },
			Visibility = Visibility.Collapsed
		};

		_crossfeedChartCanvas.Children.Add(_crossfeedAreaPath);
		_crossfeedChartCanvas.Children.Add(_crossfeedGhostPath);
		_crossfeedChartCanvas.Children.Add(_crossfeedCurvePath);

		// 截止频率垂线与把手
		_crossfeedCutoffLine = new Line
		{
			Stroke = GetCrossfeedCutoffLineBrush(isDark),
			StrokeThickness = 1.5,
			StrokeDashArray = new DoubleCollection { 3, 3 }
		};
		_crossfeedCutoffKnob = new Border
		{
			Width = 14,
			Height = 14,
			CornerRadius = new CornerRadius(7),
			Background = GetCrossfeedCutoffKnobBgBrush(isDark),
			BorderBrush = GetCrossfeedCutoffKnobBorderBrush(isDark),
			BorderThickness = new Thickness(2.5)
		};
		_crossfeedCutoffBadge = new Border
		{
			Background = GetCrossfeedAccentSoftBgBrush(isDark),
			BorderBrush = GetCrossfeedAccentSoftBorderBrush(isDark),
			BorderThickness = new Thickness(1),
			CornerRadius = new CornerRadius(4),
			Padding = new Thickness(6, 2, 6, 2)
		};
		_crossfeedCutoffBadgeTb = new TextBlock
		{
			Text = "截止 700 Hz",
			FontSize = 10,
			FontWeight = FontWeights.SemiBold,
			Foreground = GetCrossfeedAccentSoftTextBrush(isDark)
		};
		_crossfeedCutoffBadge.Child = _crossfeedCutoffBadgeTb;

		_crossfeedChartCanvas.Children.Add(_crossfeedCutoffLine);
		_crossfeedChartCanvas.Children.Add(_crossfeedCutoffKnob);
		_crossfeedChartCanvas.Children.Add(_crossfeedCutoffBadge);

		// 图内刻度文字 (MID 0 dB, SIDE, -2, -4)
		var mutedBrush = GetCrossfeedMutedTextBrush(isDark);
		_crossfeedMidLabelTb = new TextBlock
		{
			Text = "MID 0 dB",
			FontSize = 11,
			FontWeight = FontWeights.SemiBold,
			Foreground = mutedBrush
		};
		_crossfeedSideLabelTb = new TextBlock
		{
			Text = "SIDE",
			FontSize = 11,
			FontWeight = FontWeights.SemiBold,
			Foreground = mutedBrush
		};
		_crossfeedTickMinus2Tb = new TextBlock
		{
			Text = "-2",
			FontSize = 11,
			Foreground = mutedBrush
		};
		_crossfeedTickMinus4Tb = new TextBlock
		{
			Text = "-4",
			FontSize = 11,
			Foreground = mutedBrush
		};
		_crossfeedMutedTextBlocks.Add(_crossfeedMidLabelTb);
		_crossfeedMutedTextBlocks.Add(_crossfeedSideLabelTb);
		_crossfeedMutedTextBlocks.Add(_crossfeedTickMinus2Tb);
		_crossfeedMutedTextBlocks.Add(_crossfeedTickMinus4Tb);

		_crossfeedChartCanvas.Children.Add(_crossfeedMidLabelTb);
		_crossfeedChartCanvas.Children.Add(_crossfeedSideLabelTb);
		_crossfeedChartCanvas.Children.Add(_crossfeedTickMinus2Tb);
		_crossfeedChartCanvas.Children.Add(_crossfeedTickMinus4Tb);

		// 右上角操作提示 Tag: ↔ 可拖动 · 调整截止
		_crossfeedDragHintArrowTb = new TextBlock { Text = "↔", FontSize = 10, Foreground = mutedBrush };
		_crossfeedDragHintTextTb = new TextBlock { Text = "可拖动 · 调整截止", FontSize = 10, Foreground = mutedBrush };
		_crossfeedMutedTextBlocks.Add(_crossfeedDragHintArrowTb);
		_crossfeedMutedTextBlocks.Add(_crossfeedDragHintTextTb);

		_crossfeedDragHintTag = new Border
		{
			Background = CreateCrossfeedSurfaceBrush(LiquidGlassVariant.Clear, isDark, 0.92),
			BorderBrush = CreateCrossfeedRimBrush(isDark),
			BorderThickness = new Thickness(1),
			CornerRadius = new CornerRadius(4),
			Padding = new Thickness(6, 3, 6, 3),
			HorizontalAlignment = HorizontalAlignment.Right,
			VerticalAlignment = VerticalAlignment.Top,
			Margin = new Thickness(0, 10, 12, 0),
			Child = new StackPanel
			{
				Orientation = Orientation.Horizontal,
				Spacing = 4,
				Children =
				{
					_crossfeedDragHintArrowTb,
					_crossfeedDragHintTextTb
				}
			}
		};

		// 鼠标/触控拖动截止频率事件
		_crossfeedChartCanvas.PointerPressed += OnCrossfeedChartPointerPressed;
		_crossfeedChartCanvas.PointerMoved += OnCrossfeedChartPointerMoved;
		_crossfeedChartCanvas.PointerReleased += OnCrossfeedChartPointerReleased;
		_crossfeedChartCanvas.PointerCanceled += (s, e) => { _isDraggingCrossfeedCutoff = false; };
		_crossfeedChartCanvas.SizeChanged += (s, e) => { RedrawCrossfeedCurve(); };
		_crossfeedChartCanvas.Loaded += (s, e) => { RedrawCrossfeedCurve(); };

		stageGrid.Children.Add(_crossfeedChartCanvas);
		stageGrid.Children.Add(_crossfeedDragHintTag);
		_crossfeedChartStageBorder.Child = stageGrid;
		stack.Children.Add(_crossfeedChartStageBorder);

		// X 轴频率标尺 (20, 100, 1k, 10k, 20k Hz)
		_crossfeedRulerCanvas = new Canvas
		{
			Height = 18,
			HorizontalAlignment = HorizontalAlignment.Stretch
		};
		_crossfeedRulerCanvas.SizeChanged += (s, e) => { UpdateCrossfeedRulerLayout(); };
		stack.Children.Add(_crossfeedRulerCanvas);

		card.Child = stack;
		return card;
	}

	private FrameworkElement CreateCrossfeedPresetsAndControlsCard()
	{
		bool isDark = IsDspDark;
		var card = new Border
		{
			Background = CreateCrossfeedSurfaceBrush(LiquidGlassVariant.Regular, isDark, Math.Clamp(0.88 / 0.90, 0.72, 1.05)),
			BorderBrush = CreateCrossfeedRimBrush(isDark),
			BorderThickness = new Thickness(1),
			CornerRadius = new CornerRadius(12),
			Padding = new Thickness(20, 16, 20, 18)
		};
		_crossfeedCardBorders.Add(card);

		var stack = new StackPanel { Spacing = 16 };

		// --- 1. 试听预设 (点击即生效) ---
		var presetsSection = new StackPanel { Spacing = 10 };
		var presetsHeaderTb = new TextBlock
		{
			Text = "试听预设 · 点击即生效",
			FontSize = 13,
			FontWeight = FontWeights.SemiBold,
			Foreground = GetCrossfeedPrimaryTextBrush(isDark)
		};
		_crossfeedPrimaryTextBlocks.Add(presetsHeaderTb);
		presetsSection.Children.Add(presetsHeaderTb);

		var pillsWrap = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };

		_btnPresetSubtle = CreateCrossfeedPresetPillButton("轻度", 0.10, 650.0);
		_btnPresetNatural = CreateCrossfeedPresetPillButton("自然", 0.25, 700.0);
		_btnPresetStrong = CreateCrossfeedPresetPillButton("强烈", 0.45, 750.0);
		_btnPresetSpeaker = CreateCrossfeedPresetPillButton("音箱模拟", 0.60, 800.0);

		pillsWrap.Children.Add(_btnPresetSubtle);
		pillsWrap.Children.Add(_btnPresetNatural);
		pillsWrap.Children.Add(_btnPresetStrong);
		pillsWrap.Children.Add(_btnPresetSpeaker);
		presetsSection.Children.Add(pillsWrap);

		// 试听提示
		var presetHintTb = new TextBlock
		{
			Text = "戴耳机从「强烈」开始，用右上角开关对比旁路。低截止与居中的人声、低音变化通常很轻；重点听左右分离的乐器是否向中间靠拢。开启交叉馈送会关闭虚拟声场。",
			FontSize = 11.5,
			Foreground = GetCrossfeedMutedTextBrush(isDark),
			TextWrapping = TextWrapping.Wrap,
			LineHeight = 17
		};
		_crossfeedMutedTextBlocks.Add(presetHintTb);
		presetsSection.Children.Add(presetHintTb);

		stack.Children.Add(presetsSection);

		// 分隔线
		_crossfeedDividerBorder = new Border
		{
			Height = 1,
			Background = GetCrossfeedDividerBrush(isDark),
			Margin = new Thickness(0, 2, 0, 2)
		};
		stack.Children.Add(_crossfeedDividerBorder);

		// --- 2. 参数控制 ---
		var controlsSection = new StackPanel { Spacing = 14 };
		var controlsHeaderTb = new TextBlock
		{
			Text = "参数控制",
			FontSize = 13,
			FontWeight = FontWeights.SemiBold,
			Foreground = GetCrossfeedPrimaryTextBrush(isDark)
		};
		_crossfeedPrimaryTextBlocks.Add(controlsHeaderTb);
		controlsSection.Children.Add(controlsHeaderTb);

		// 算法下拉框
		var modelRow = new Grid();
		modelRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.0, GridUnitType.Star) });
		modelRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

		var modelLabel = new TextBlock
		{
			Text = "算法",
			FontSize = 13,
			Foreground = GetCrossfeedPrimaryTextBrush(isDark),
			VerticalAlignment = VerticalAlignment.Center
		};
		_crossfeedPrimaryTextBlocks.Add(modelLabel);
		Grid.SetColumn(modelLabel, 0);
		modelRow.Children.Add(modelLabel);

		_crossfeedModelCombo = new ComboBox
		{
			MinWidth = 180,
			CornerRadius = new CornerRadius(6),
			Background = CreateCrossfeedSurfaceBrush(LiquidGlassVariant.Clear, isDark, 0.92),
			BorderBrush = CreateCrossfeedRimBrush(isDark),
			Foreground = GetCrossfeedPrimaryTextBrush(isDark),
			RequestedTheme = isDark ? ElementTheme.Dark : ElementTheme.Light
		};
		_crossfeedModelCombo.Items.Add(new ComboBoxItem { Content = "原有低频 Side 衰减", Tag = "mid_side" });
		_crossfeedModelCombo.Items.Add(new ComboBoxItem { Content = "Bauer BS2B (自然仿真)", Tag = "bs2b" });
		_crossfeedModelCombo.Items.Add(new ComboBoxItem { Content = "Chu Moy 经典", Tag = "chu_moy" });
		_crossfeedModelCombo.SelectedIndex = 0;
		_crossfeedModelCombo.SelectionChanged += (s, e) =>
		{
			if (_crossfeedModelCombo.SelectedItem is ComboBoxItem item && item.Tag is string tag)
			{
				_draftCrossfeedModel = tag;
				_isCrossfeedDirty = true;
				UpdateCrossfeedDirtyStateVisuals();
				RedrawCrossfeedCurve();
				UpdateCrossfeedBottomStats();
			}
		};
		Grid.SetColumn(_crossfeedModelCombo, 1);
		modelRow.Children.Add(_crossfeedModelCombo);
		controlsSection.Children.Add(modelRow);

		// 馈送量滑块
		var amountStack = new StackPanel { Spacing = 4 };
		var amountHead = new Grid();
		amountHead.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.0, GridUnitType.Star) });
		amountHead.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
		var amountLabelTb = new TextBlock { Text = "馈送量", FontSize = 13, Foreground = GetCrossfeedPrimaryTextBrush(isDark) };
		_crossfeedPrimaryTextBlocks.Add(amountLabelTb);
		amountHead.Children.Add(amountLabelTb);

		_crossfeedAmountValueTb = new TextBlock
		{
			Text = "25%",
			FontSize = 13,
			FontWeight = FontWeights.SemiBold,
			Foreground = GetCrossfeedPrimaryTextBrush(isDark)
		};
		_crossfeedPrimaryTextBlocks.Add(_crossfeedAmountValueTb);
		Grid.SetColumn(_crossfeedAmountValueTb, 1);
		amountHead.Children.Add(_crossfeedAmountValueTb);
		amountStack.Children.Add(amountHead);

		_crossfeedAmountSlider = new Slider
		{
			Minimum = 0.0,
			Maximum = 1.0,
			StepFrequency = 0.01,
			SmallChange = 0.01,
			LargeChange = 0.05,
			Value = _draftCrossfeedAmount,
			HorizontalAlignment = HorizontalAlignment.Stretch
		};
		_crossfeedAmountSlider.ValueChanged += (s, e) =>
		{
			_draftCrossfeedAmount = e.NewValue;
			if (_crossfeedAmountValueTb != null)
			{
				_crossfeedAmountValueTb.Text = $"{Math.Round(_draftCrossfeedAmount * 100)}%";
			}
			_isCrossfeedDirty = true;
			UpdateCrossfeedDirtyStateVisuals();
			RedrawCrossfeedCurve();
			UpdateCrossfeedBottomStats();
		};
		amountStack.Children.Add(_crossfeedAmountSlider);
		controlsSection.Children.Add(amountStack);

		// 低通截止滑块
		var cutoffStack = new StackPanel { Spacing = 4 };
		var cutoffHead = new Grid();
		cutoffHead.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.0, GridUnitType.Star) });
		cutoffHead.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
		var cutoffLabelTb = new TextBlock { Text = "低通截止", FontSize = 13, Foreground = GetCrossfeedPrimaryTextBrush(isDark) };
		_crossfeedPrimaryTextBlocks.Add(cutoffLabelTb);
		cutoffHead.Children.Add(cutoffLabelTb);

		_crossfeedCutoffValueTb = new TextBlock
		{
			Text = "700 Hz",
			FontSize = 13,
			FontWeight = FontWeights.SemiBold,
			Foreground = GetCrossfeedPrimaryTextBrush(isDark)
		};
		_crossfeedPrimaryTextBlocks.Add(_crossfeedCutoffValueTb);
		Grid.SetColumn(_crossfeedCutoffValueTb, 1);
		cutoffHead.Children.Add(_crossfeedCutoffValueTb);
		cutoffStack.Children.Add(cutoffHead);

		_crossfeedCutoffSlider = new Slider
		{
			Minimum = 200,
			Maximum = 2000,
			StepFrequency = 10,
			SmallChange = 10,
			LargeChange = 50,
			Value = _draftCrossfeedCutoffHz,
			HorizontalAlignment = HorizontalAlignment.Stretch
		};
		_crossfeedCutoffSlider.ValueChanged += (s, e) =>
		{
			_draftCrossfeedCutoffHz = e.NewValue;
			if (_crossfeedCutoffValueTb != null)
			{
				_crossfeedCutoffValueTb.Text = $"{Math.Round(_draftCrossfeedCutoffHz)} Hz";
			}
			_isCrossfeedDirty = true;
			UpdateCrossfeedDirtyStateVisuals();
			RedrawCrossfeedCurve();
			UpdateCrossfeedBottomStats();
		};
		cutoffStack.Children.Add(_crossfeedCutoffSlider);
		controlsSection.Children.Add(cutoffStack);

		stack.Children.Add(controlsSection);

		card.Child = stack;
		return card;
	}

	private Button CreateCrossfeedPresetPillButton(string label, double amount, double cutoffHz)
	{
		var btn = new Button
		{
			Content = label,
			Padding = new Thickness(16, 6, 16, 6),
			CornerRadius = new CornerRadius(16),
			FontSize = 12,
			Tag = (amount, cutoffHz, label),
			BorderThickness = new Thickness(1)
		};
		btn.Click += (s, e) =>
		{
			_draftCrossfeedAmount = amount;
			_draftCrossfeedCutoffHz = cutoffHz;
			_crossfeedAmount = amount;
			_crossfeedCutoffHz = cutoffHz;
			_crossfeedEnabled = true;
			_draftCrossfeedEnabled = true;

			// 同步滑块与下拉框
			if (_crossfeedAmountSlider != null) _crossfeedAmountSlider.Value = amount;
			if (_crossfeedCutoffSlider != null) _crossfeedCutoffSlider.Value = cutoffHz;

			ApplyCrossfeedSettings(isPresetImmediate: true);
		};
		_crossfeedPresetButtons.Add(btn);
		return btn;
	}
	#endregion

	#region 4. Bottom Stats Section (Triple Big Readout)
	private FrameworkElement CreateCrossfeedStatsSection()
	{
		var grid = new Grid { ColumnSpacing = 16 };
		grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.0, GridUnitType.Star) });
		grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.0, GridUnitType.Star) });
		grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.0, GridUnitType.Star) });

		// 1. 馈送量
		var card1 = CreateCrossfeedMetricBox("馈送量", $"{Math.Round(_draftCrossfeedAmount * 100)}%", out _statFeedValueTb);
		Grid.SetColumn(card1, 0);
		grid.Children.Add(card1);

		// 2. 低频侧衰减
		double lowCutDb = CalculateCrossfeedLowFrequencySideCutDb(_draftCrossfeedAmount);
		var card2 = CreateCrossfeedMetricBox("低频侧衰减", $"{lowCutDb:F1} dB", out _statLowCutValueTb);
		Grid.SetColumn(card2, 1);
		grid.Children.Add(card2);

		// 3. 过渡频率
		var card3 = CreateCrossfeedMetricBox("过渡频率", $"{Math.Round(_draftCrossfeedCutoffHz)} Hz", out _statCutoffValueTb);
		Grid.SetColumn(card3, 2);
		grid.Children.Add(card3);

		return grid;
	}

	private FrameworkElement CreateCrossfeedMetricBox(string header, string value, out TextBlock valTb)
	{
		bool isDark = IsDspDark;
		var border = new Border
		{
			Background = CreateCrossfeedSurfaceBrush(LiquidGlassVariant.Regular, isDark, Math.Clamp(0.88 / 0.90, 0.72, 1.05)),
			BorderBrush = CreateCrossfeedRimBrush(isDark),
			BorderThickness = new Thickness(1),
			CornerRadius = new CornerRadius(12),
			Padding = new Thickness(18, 14, 18, 16)
		};
		_crossfeedCardBorders.Add(border);

		var sp = new StackPanel { Spacing = 6 };
		var headerTb = new TextBlock
		{
			Text = header,
			FontSize = 12,
			FontWeight = FontWeights.Medium,
			Foreground = GetCrossfeedMutedTextBrush(isDark)
		};
		_crossfeedMutedTextBlocks.Add(headerTb);
		sp.Children.Add(headerTb);

		valTb = new TextBlock
		{
			Text = value,
			FontSize = 32,
			FontWeight = FontWeights.Bold,
			Foreground = GetCrossfeedPrimaryTextBrush(isDark),
			FontFamily = new FontFamily("Segoe UI Variable Display, Consolas, Segoe UI")
		};
		_crossfeedPrimaryTextBlocks.Add(valTb);
		sp.Children.Add(valTb);

		border.Child = sp;
		return border;
	}
	#endregion

	#region 5. Frequency Response Curve Math & Rendering
	private void RedrawCrossfeedCurve()
	{
		if (_crossfeedChartCanvas == null || _crossfeedCurvePath == null || _crossfeedAreaPath == null)
		{
			return;
		}

		double w = _crossfeedChartCanvas.ActualWidth;
		double h = _crossfeedChartCanvas.ActualHeight;
		if (w <= 20 || h <= 20)
		{
			return;
		}

		double topMargin = 24.0;
		double bottomMargin = 22.0;
		double floorDb = -6.0;

		// 计算当前低频衰减以动态调整 Y 轴下界 (最高 -24 dB)
		double currentLowCut = CalculateCrossfeedLowFrequencySideCutDb(_draftCrossfeedAmount);
		if (currentLowCut < -5.0)
		{
			floorDb = Math.Max(-24.0, Math.Floor(currentLowCut - 1.5));
		}

		double GetY(double db)
		{
			double clamped = Math.Clamp(db, floorDb, 0.0);
			double normY = (0.0 - clamped) / (0.0 - floorDb); // 0 dB 对应 normY=0, floorDb 对应 normY=1
			return topMargin + normY * (h - topMargin - bottomMargin);
		}

		double GetX(double hz)
		{
			double norm = Math.Log10(Math.Clamp(hz, 20.0, 20000.0) / 20.0) / 3.0;
			return Math.Clamp(norm * w, 0.0, w);
		}

		// 1. 更新网格线
		double y0 = GetY(0.0);
		double yMinus2 = GetY(-2.0);
		double yMinus4 = GetY(-4.0);

		UpdateCrossfeedGridLine(_crossfeedGridLine0, 0, w, y0, y0);
		UpdateCrossfeedGridLine(_crossfeedGridLineMinus2, 0, w, yMinus2, yMinus2);
		UpdateCrossfeedGridLine(_crossfeedGridLineMinus4, 0, w, yMinus4, yMinus4);

		double x100 = GetX(100.0);
		double x1k = GetX(1000.0);
		double x10k = GetX(10000.0);

		UpdateCrossfeedGridLine(_crossfeedGridLine100, x100, x100, topMargin, h - bottomMargin);
		UpdateCrossfeedGridLine(_crossfeedGridLine1k, x1k, x1k, topMargin, h - bottomMargin);
		UpdateCrossfeedGridLine(_crossfeedGridLine10k, x10k, x10k, topMargin, h - bottomMargin);

		// 2. 更新网格文字标签
		if (_crossfeedMidLabelTb != null)
		{
			Canvas.SetLeft(_crossfeedMidLabelTb, 12);
			Canvas.SetTop(_crossfeedMidLabelTb, Math.Max(4, y0 - 18));
		}
		if (_crossfeedSideLabelTb != null)
		{
			Canvas.SetLeft(_crossfeedSideLabelTb, 12);
			Canvas.SetTop(_crossfeedSideLabelTb, Math.Min(h - 32, GetY(currentLowCut) + 6));
		}
		if (_crossfeedTickMinus2Tb != null)
		{
			Canvas.SetLeft(_crossfeedTickMinus2Tb, w - 24);
			Canvas.SetTop(_crossfeedTickMinus2Tb, yMinus2 - 8);
		}
		if (_crossfeedTickMinus4Tb != null)
		{
			Canvas.SetLeft(_crossfeedTickMinus4Tb, w - 24);
			Canvas.SetTop(_crossfeedTickMinus4Tb, yMinus4 - 8);
		}

		// 3. 构建草稿曲线几何 (PathGeometry)
		var curveFigure = new PathFigure { IsClosed = false };
		var areaFigure = new PathFigure { IsClosed = true };

		int steps = 80;
		for (int i = 0; i <= steps; i++)
		{
			double norm = (double)i / steps;
			double freq = 20.0 * Math.Pow(1000.0, norm);
			double db = CalculateCrossfeedResponseDb(freq, _draftCrossfeedAmount, _draftCrossfeedCutoffHz, _draftCrossfeedModel);
			double ptX = norm * w;
			double ptY = GetY(db);

			if (i == 0)
			{
				curveFigure.StartPoint = new Point(ptX, ptY);
				areaFigure.StartPoint = new Point(0, h - bottomMargin);
				areaFigure.Segments.Add(new LineSegment { Point = new Point(ptX, ptY) });
			}
			else
			{
				curveFigure.Segments.Add(new LineSegment { Point = new Point(ptX, ptY) });
				areaFigure.Segments.Add(new LineSegment { Point = new Point(ptX, ptY) });
			}
		}

		areaFigure.Segments.Add(new LineSegment { Point = new Point(w, h - bottomMargin) });

		_crossfeedCurvePath.Data = new PathGeometry { Figures = { curveFigure } };
		_crossfeedAreaPath.Data = new PathGeometry { Figures = { areaFigure } };

		// 4. Ghost 虚线 (若已修改草稿)
		if (_isCrossfeedDirty && _crossfeedGhostPath != null)
		{
			_crossfeedGhostPath.Visibility = Visibility.Visible;
			var ghostFigure = new PathFigure { IsClosed = false };
			for (int i = 0; i <= steps; i++)
			{
				double norm = (double)i / steps;
				double freq = 20.0 * Math.Pow(1000.0, norm);
				double db = CalculateCrossfeedResponseDb(freq, _crossfeedAmount, _crossfeedCutoffHz, _crossfeedModel);
				double ptX = norm * w;
				double ptY = GetY(db);
				if (i == 0) ghostFigure.StartPoint = new Point(ptX, ptY);
				else ghostFigure.Segments.Add(new LineSegment { Point = new Point(ptX, ptY) });
			}
			_crossfeedGhostPath.Data = new PathGeometry { Figures = { ghostFigure } };
		}
		else if (_crossfeedGhostPath != null)
		{
			_crossfeedGhostPath.Visibility = Visibility.Collapsed;
		}

		// 5. 截止频率垂线与圆点
		double cutoffX = GetX(_draftCrossfeedCutoffHz);
		double cutoffDb = CalculateCrossfeedResponseDb(_draftCrossfeedCutoffHz, _draftCrossfeedAmount, _draftCrossfeedCutoffHz, _draftCrossfeedModel);
		double cutoffY = GetY(cutoffDb);

		if (_crossfeedCutoffLine != null)
		{
			_crossfeedCutoffLine.X1 = cutoffX;
			_crossfeedCutoffLine.X2 = cutoffX;
			_crossfeedCutoffLine.Y1 = topMargin;
			_crossfeedCutoffLine.Y2 = h - bottomMargin;
		}

		if (_crossfeedCutoffKnob != null)
		{
			Canvas.SetLeft(_crossfeedCutoffKnob, cutoffX - 7);
			Canvas.SetTop(_crossfeedCutoffKnob, cutoffY - 7);
		}

		if (_crossfeedCutoffBadge != null && _crossfeedCutoffBadgeTb != null)
		{
			_crossfeedCutoffBadgeTb.Text = $"截止 {Math.Round(_draftCrossfeedCutoffHz)} Hz";
			double badgeX = Math.Clamp(cutoffX + 6, 6, w - 90);
			Canvas.SetLeft(_crossfeedCutoffBadge, badgeX);
			Canvas.SetTop(_crossfeedCutoffBadge, h - bottomMargin - 24);
		}

		// 6. 更新副标题读数
		if (_crossfeedChartSubtitleTb != null)
		{
			string presetTag = GetCrossfeedMatchingPresetName(_draftCrossfeedAmount, _draftCrossfeedCutoffHz);
			_crossfeedChartSubtitleTb.Text = $"{Math.Round(_draftCrossfeedAmount * 100)}% · {Math.Round(_draftCrossfeedCutoffHz)} Hz{(string.IsNullOrEmpty(presetTag) ? "" : $" · {presetTag}")}";
		}
	}

	private static void UpdateCrossfeedGridLine(Line? line, double x1, double x2, double y1, double y2)
	{
		if (line == null) return;
		line.X1 = x1;
		line.X2 = x2;
		line.Y1 = y1;
		line.Y2 = y2;
	}

	private void UpdateCrossfeedRulerLayout(bool? isDarkOverride = null)
	{
		if (_crossfeedRulerCanvas == null) return;
		double w = _crossfeedRulerCanvas.ActualWidth;
		if (w <= 20) return;

		bool isDark = isDarkOverride ?? IsDspDark;
		Brush mutedBrush = GetCrossfeedMutedTextBrush(isDark);

		_crossfeedRulerCanvas.Children.Clear();

		double GetX(double hz)
		{
			double norm = Math.Log10(Math.Clamp(hz, 20.0, 20000.0) / 20.0) / 3.0;
			return Math.Clamp(norm * w, 0.0, w);
		}

		var ticks = new (double Freq, string Text)[]
		{
			(20, "20"),
			(100, "100"),
			(1000, "1k"),
			(10000, "10k"),
			(20000, "20k Hz")
		};

		foreach (var (freq, text) in ticks)
		{
			var tb = new TextBlock
			{
				Text = text,
				FontSize = 11,
				Foreground = mutedBrush
			};

			double x = GetX(freq);
			if (freq == 20)
			{
				Canvas.SetLeft(tb, 2);
			}
			else if (freq == 20000)
			{
				Canvas.SetLeft(tb, w - 42);
			}
			else
			{
				Canvas.SetLeft(tb, x - 12);
			}
			Canvas.SetTop(tb, 0);
			_crossfeedRulerCanvas.Children.Add(tb);
		}
	}

	private void OnCrossfeedChartPointerPressed(object sender, PointerRoutedEventArgs e)
	{
		if (_crossfeedChartCanvas == null) return;
		_crossfeedChartCanvas.CapturePointer(e.Pointer);
		_isDraggingCrossfeedCutoff = true;
		UpdateCrossfeedCutoffFromPointer(e.GetCurrentPoint(_crossfeedChartCanvas).Position.X);
		e.Handled = true;
	}

	private void OnCrossfeedChartPointerMoved(object sender, PointerRoutedEventArgs e)
	{
		if (!_isDraggingCrossfeedCutoff || _crossfeedChartCanvas == null) return;
		UpdateCrossfeedCutoffFromPointer(e.GetCurrentPoint(_crossfeedChartCanvas).Position.X);
		e.Handled = true;
	}

	private void OnCrossfeedChartPointerReleased(object sender, PointerRoutedEventArgs e)
	{
		if (_crossfeedChartCanvas != null)
		{
			_crossfeedChartCanvas.ReleasePointerCapture(e.Pointer);
		}
		_isDraggingCrossfeedCutoff = false;
		e.Handled = true;
	}

	private void UpdateCrossfeedCutoffFromPointer(double pointerX)
	{
		if (_crossfeedChartCanvas == null) return;
		double w = _crossfeedChartCanvas.ActualWidth;
		if (w <= 20) return;

		double norm = Math.Clamp(pointerX / w, 0.0, 1.0);
		double hz = 20.0 * Math.Pow(1000.0, norm);
		double clampedHz = Math.Clamp(Math.Round(hz / 10.0) * 10.0, 200.0, 2000.0);

		_draftCrossfeedCutoffHz = clampedHz;
		if (_crossfeedCutoffSlider != null)
		{
			_crossfeedCutoffSlider.Value = clampedHz;
		}
		if (_crossfeedCutoffValueTb != null)
		{
			_crossfeedCutoffValueTb.Text = $"{Math.Round(clampedHz)} Hz";
		}

		_isCrossfeedDirty = true;
		UpdateCrossfeedDirtyStateVisuals();
		RedrawCrossfeedCurve();
		UpdateCrossfeedBottomStats();
	}
	#endregion

	#region 6. DSP Mathematics & Algorithm Models
	private static double CalculateCrossfeedResponseDb(double freq, double amount, double cutoffHz, string model)
	{
		return model switch
		{
			"bs2b" => CalculateCrossfeedBs2bResponseDb(freq, cutoffHz, amount, isSide: true),
			"chu_moy" => CalculateCrossfeedChuMoyResponseDb(freq, amount, cutoffHz),
			_ => CalculateCrossfeedMidSideResponseDb(freq, amount, cutoffHz)
		};
	}

	/// <summary>
	/// Mid/Side 域原有低频 Side 衰减滤波曲线
	/// </summary>
	private static double CalculateCrossfeedMidSideResponseDb(double freq, double amount, double cutoffHz)
	{
		double t = 1.0 / Math.Sqrt(1.0 + Math.Pow(freq / Math.Max(1.0, cutoffHz), 2.0));
		double r = Math.Max(1e-4, 1.0 - Math.Clamp(amount, 0.0, 1.0) * t);
		return 20.0 * Math.Log10(r);
	}

	/// <summary>
	/// Chu Moy 经典一阶低通交叉馈送滤波响应
	/// </summary>
	private static double CalculateCrossfeedChuMoyResponseDb(double freq, double amount, double cutoffHz)
	{
		double omega = freq / Math.Max(1.0, cutoffHz);
		double t = 1.0 / Math.Sqrt(1.0 + omega * omega);
		double r = Math.Max(1e-4, 1.0 - Math.Clamp(amount * 1.12, 0.0, 0.99) * t);
		return 20.0 * Math.Log10(r);
	}

	/// <summary>
	/// Bauer BS2B (自然仿真) 双耳传递函数响应 (来自官方解包实现 $e)
	/// </summary>
	private static double CalculateCrossfeedBs2bResponseDb(double freq, double cutoffHz, double amount, bool isSide)
	{
		double feedDb = 1.0 + Math.Clamp(amount, 0.0, 1.0) * 14.0;
		double r = -feedDb * 5.0 / 6.0 - 3.0;
		double s = feedDb / 6.0 - 3.0;
		double i = Math.Pow(10.0, r / 20.0);
		double u = 1.0 - Math.Pow(10.0, s / 20.0);
		double g = cutoffHz * Math.Pow(2.0, (r - 20.0 * Math.Log10(u)) / 12.0);
		double c = Math.Exp(-2.0 * Math.PI * cutoffHz / 48000.0);
		double x = Math.Exp(-2.0 * Math.PI * g / 48000.0);
		double z = i * (1.0 - c);
		double j = 1.0 - u * (1.0 - x);
		double v = Math.Cos(2.0 * Math.PI * freq / 48000.0);
		double M = -Math.Sin(2.0 * Math.PI * freq / 48000.0);

		static (double Re, double Im) Transfer(double p, double m, double D, double vVal, double mVal)
		{
			double f = p + m * vVal;
			double R = m * mVal;
			double w = 1.0 - D * vVal;
			double h = -D * mVal;
			double S = w * w + h * h;
			if (S < 1e-12) S = 1e-12;
			return ((f * w + R * h) / S, (R * w - f * h) / S);
		}

		var y = Transfer(z, 0, c, v, M);
		var H = Transfer(j, -x, x, v, M);
		double C = isSide ? -1.0 : 1.0;
		double num = Math.Sqrt(Math.Pow(H.Re + C * y.Re, 2) + Math.Pow(H.Im + C * y.Im, 2));
		double denom = Math.Max(1e-6, 1.0 - u + i);
		return 20.0 * Math.Log10(Math.Max(1e-6, num / denom));
	}

	private static double CalculateCrossfeedLowFrequencySideCutDb(double amount)
	{
		double r = Math.Max(1e-4, 1.0 - Math.Clamp(amount, 0.0, 1.0));
		return 20.0 * Math.Log10(r);
	}

	private static string GetCrossfeedMatchingPresetName(double amount, double cutoffHz)
	{
		if (Math.Abs(amount - 0.10) < 0.02 && Math.Abs(cutoffHz - 650) < 25) return "轻度";
		if (Math.Abs(amount - 0.25) < 0.02 && Math.Abs(cutoffHz - 700) < 25) return "自然";
		if (Math.Abs(amount - 0.45) < 0.02 && Math.Abs(cutoffHz - 750) < 25) return "强烈";
		if (Math.Abs(amount - 0.60) < 0.02 && Math.Abs(cutoffHz - 800) < 25) return "音箱模拟";
		return string.Empty;
	}
	#endregion

	#region 7. Actions & DSP Integration
	private void OnToggleCrossfeedPower(object sender, RoutedEventArgs e)
	{
		_crossfeedEnabled = !_crossfeedEnabled;
		_draftCrossfeedEnabled = _crossfeedEnabled;
		ApplyCrossfeedSettings();
	}

	private void OnResetCrossfeedClick(object sender, RoutedEventArgs e)
	{
		_draftCrossfeedAmount = 0.25;
		_draftCrossfeedCutoffHz = 700.0;
		_draftCrossfeedModel = "mid_side";

		if (_crossfeedAmountSlider != null) _crossfeedAmountSlider.Value = 0.25;
		if (_crossfeedCutoffSlider != null) _crossfeedCutoffSlider.Value = 700.0;
		if (_crossfeedModelCombo != null) _crossfeedModelCombo.SelectedIndex = 0;

		_isCrossfeedDirty = true;
		UpdateCrossfeedDirtyStateVisuals();
		RedrawCrossfeedCurve();
		UpdateCrossfeedBottomStats();
	}

	private void OnCrossfeedApplyParametersClick(object sender, RoutedEventArgs e)
	{
		_crossfeedAmount = _draftCrossfeedAmount;
		_crossfeedCutoffHz = _draftCrossfeedCutoffHz;
		_crossfeedModel = _draftCrossfeedModel;
		_crossfeedEnabled = true;
		_draftCrossfeedEnabled = true;
		_isCrossfeedDirty = false;

		ApplyCrossfeedSettings();
	}

	private void RevertCrossfeedDraft()
	{
		_draftCrossfeedAmount = _crossfeedAmount;
		_draftCrossfeedCutoffHz = _crossfeedCutoffHz;
		_draftCrossfeedModel = _crossfeedModel;
		_draftCrossfeedEnabled = _crossfeedEnabled;

		if (_crossfeedAmountSlider != null) _crossfeedAmountSlider.Value = _crossfeedAmount;
		if (_crossfeedCutoffSlider != null) _crossfeedCutoffSlider.Value = _crossfeedCutoffHz;
		if (_crossfeedModelCombo != null)
		{
			_crossfeedModelCombo.SelectedIndex = _crossfeedModel switch
			{
				"bs2b" => 1,
				"chu_moy" => 2,
				_ => 0
			};
		}

		_isCrossfeedDirty = false;
		UpdateAllCrossfeedVisuals();
	}

	/// <summary>
	/// 对接底层 DSP 核心与声学互斥联动。
	/// </summary>
	private void ApplyCrossfeedSettings(bool isPresetImmediate = false)
	{
		// 1. 声学互斥联动: 开启交叉馈送时自动关闭虚拟声场
		if (_crossfeedEnabled)
		{
			var dspChain = ViewModel?.Engine?.DspChain;
			if (dspChain?.Spatial != null && dspChain.Spatial.VirtualStageEnabled)
			{
				dspChain.Spatial.VirtualStageEnabled = false;
				ViewModel?.Engine?.SetFurinaVirtualStage(
					false,
					dspChain.Spatial.VirtualStageStrength,
					dspChain.Spatial.VirtualStageAzimuth,
					dspChain.Spatial.VirtualStageReflection,
					dspChain.Spatial.VirtualStageHrtf,
					dspChain.Spatial.VirtualStageHrtfPreset);

				if (_crossfeedMutualExclusionInfoBar != null)
				{
					_crossfeedMutualExclusionInfoBar.IsOpen = true;
				}
			}
		}
		else
		{
			if (_crossfeedMutualExclusionInfoBar != null)
			{
				_crossfeedMutualExclusionInfoBar.IsOpen = false;
			}
		}

		// 2. 调用 AudioEngine / AudioSession 核心接口
		if (ViewModel?.Engine != null)
		{
			ViewModel.Engine.SetFurinaCrossfeed(
				_crossfeedEnabled,
				(float)_crossfeedAmount,
				(float)_crossfeedCutoffHz);
		}

		// 3. 同步至 ViewModel 及持久化设置
		if (ViewModel != null)
		{
			ViewModel.EchoCrossfeedEnabled = _crossfeedEnabled;
			ViewModel.EchoCrossfeedAmount = _crossfeedAmount;
			ViewModel.EchoCrossfeedCutoffHz = _crossfeedCutoffHz;
			ViewModel.SaveEchoSettings();
		}

		_isCrossfeedDirty = false;
		UpdateAllCrossfeedVisuals();
	}

	private void UpdateAllCrossfeedVisuals(bool? isDarkOverride = null)
	{
		bool isDark = isDarkOverride ?? IsDspDark;

		// 1. 电源按钮状态
		if (_crossfeedPowerBtn != null && _crossfeedPowerText != null && _crossfeedPowerIcon != null)
		{
			if (_crossfeedEnabled)
			{
				_crossfeedPowerText.Text = "已开启";
				ApplyCrossfeedAccentButtonStyle(
					_crossfeedPowerBtn,
					isDark,
					_crossfeedPowerIcon,
					_crossfeedPowerText,
					borderOverride: new SolidColorBrush(isDark ? Color.FromArgb(200, 129, 140, 248) : Color.FromArgb(255, 99, 102, 241)));
			}
			else
			{
				_crossfeedPowerText.Text = "已旁路 · 点击试听";
				ApplyCrossfeedNeutralButtonStyle(
					_crossfeedPowerBtn,
					isDark,
					_crossfeedPowerIcon,
					_crossfeedPowerText);
			}
		}

		// 2. 草稿脏状态
		UpdateCrossfeedDirtyStateVisuals(isDark);

		// 3. 预设按钮激活态
		UpdateCrossfeedPresetButtonsVisuals(isDark);

		// 4. 底部大号读数
		UpdateCrossfeedBottomStats();

		// 5. 侧声道响应曲线
		RedrawCrossfeedCurve();
		UpdateCrossfeedRulerLayout(isDark);
	}

	private void UpdateCrossfeedDirtyStateVisuals(bool? isDarkOverride = null)
	{
		bool isDark = isDarkOverride ?? IsDspDark;

		if (_crossfeedApplyBtn != null)
		{
			if (_isCrossfeedDirty)
			{
				var bg = new SolidColorBrush(Color.FromArgb(255, 99, 102, 241));
				var hoverBg = new SolidColorBrush(Color.FromArgb(255, 79, 70, 229));
				var pressedBg = new SolidColorBrush(Color.FromArgb(255, 67, 56, 202));
				var border = new SolidColorBrush(isDark ? Color.FromArgb(255, 129, 140, 248) : Color.FromArgb(255, 79, 70, 229));
				var hoverBorder = new SolidColorBrush(isDark ? Color.FromArgb(255, 165, 180, 252) : Color.FromArgb(255, 67, 56, 202));
				var fg = new SolidColorBrush(Colors.White);

				ApplyCrossfeedButtonPalette(
					_crossfeedApplyBtn,
					bg,
					hoverBg,
					pressedBg,
					border,
					hoverBorder,
					fg,
					_crossfeedApplyIcon,
					_crossfeedApplyText);
			}
			else
			{
				ApplyCrossfeedNeutralButtonStyle(
					_crossfeedApplyBtn,
					isDark,
					_crossfeedApplyIcon,
					_crossfeedApplyText);
			}
		}

		if (_crossfeedPendingBadge != null)
		{
			ApplyCrossfeedPendingBadgeTheme(isDark);
			_crossfeedPendingBadge.Visibility = _isCrossfeedDirty ? Visibility.Visible : Visibility.Collapsed;
		}
	}

	private void UpdateCrossfeedPresetButtonsVisuals(bool? isDarkOverride = null)
	{
		bool isDark = isDarkOverride ?? IsDspDark;

		foreach (var btn in _crossfeedPresetButtons)
		{
			if (btn.Tag is (double pAmount, double pCutoff, string _))
			{
				bool isActive = _crossfeedEnabled && !_isCrossfeedDirty &&
					Math.Abs(_crossfeedAmount - pAmount) < 0.015 &&
					Math.Abs(_crossfeedCutoffHz - pCutoff) < 15.0;

				if (isActive)
				{
					ApplyCrossfeedAccentButtonStyle(btn, isDark);
					btn.FontWeight = FontWeights.SemiBold;
				}
				else
				{
					ApplyCrossfeedNeutralButtonStyle(
						btn,
						isDark,
						foregroundOverride: GetCrossfeedSubHeaderTextBrush(isDark));
					btn.FontWeight = FontWeights.Normal;
				}
			}
		}
	}

	private void UpdateCrossfeedBottomStats()
	{
		if (_statFeedValueTb != null)
		{
			_statFeedValueTb.Text = $"{Math.Round(_draftCrossfeedAmount * 100)}%";
		}
		if (_statLowCutValueTb != null)
		{
			double lowCutDb = CalculateCrossfeedLowFrequencySideCutDb(_draftCrossfeedAmount);
			_statLowCutValueTb.Text = $"{lowCutDb:F1} dB";
		}
		if (_statCutoffValueTb != null)
		{
			_statCutoffValueTb.Text = $"{Math.Round(_draftCrossfeedCutoffHz)} Hz";
		}
	}

	public void RefreshCrossfeedTheme(bool isDark)
	{
		if (_crossfeedWorkbench == null) return;

		_crossfeedWorkbench.RequestedTheme = isDark ? ElementTheme.Dark : ElementTheme.Light;

		Brush primaryText = GetCrossfeedPrimaryTextBrush(isDark);
		Brush mutedText = GetCrossfeedMutedTextBrush(isDark);
		Brush accentSoftBg = GetCrossfeedAccentSoftBgBrush(isDark);
		Brush accentSoftBorder = GetCrossfeedAccentSoftBorderBrush(isDark);
		Brush accentSoftText = GetCrossfeedAccentSoftTextBrush(isDark);
		Brush cardBg = CreateCrossfeedSurfaceBrush(LiquidGlassVariant.Regular, isDark, Math.Clamp(0.88 / 0.90, 0.72, 1.05));
		Brush rimBrush = CreateCrossfeedRimBrush(isDark);
		Brush softBg = GetCrossfeedSoftSurfaceBrush(isDark);

		if (_crossfeedRootBorder != null)
		{
			_crossfeedRootBorder.Background = CreateCrossfeedSurfaceBrush(LiquidGlassVariant.Clear, isDark, Math.Clamp(0.72 / 0.85, 0.55, 1.0));
			_crossfeedRootBorder.RequestedTheme = isDark ? ElementTheme.Dark : ElementTheme.Light;
		}
		if (_crossfeedHeaderIconBorder != null) _crossfeedHeaderIconBorder.Background = accentSoftBg;
		if (_crossfeedHeaderIcon != null) _crossfeedHeaderIcon.Foreground = accentSoftText;

		foreach (var card in _crossfeedCardBorders)
		{
			card.Background = cardBg;
			card.BorderBrush = rimBrush;
		}

		foreach (var tb in _crossfeedPrimaryTextBlocks) tb.Foreground = primaryText;
		foreach (var tb in _crossfeedMutedTextBlocks) tb.Foreground = mutedText;

		if (_crossfeedParamModelPill != null)
		{
			_crossfeedParamModelPill.Background = softBg;
			_crossfeedParamModelPill.BorderBrush = rimBrush;
		}
		if (_crossfeedParamModelText != null) _crossfeedParamModelText.Foreground = mutedText;

		if (_crossfeedChartStageBorder != null)
		{
			_crossfeedChartStageBorder.Background = softBg;
			_crossfeedChartStageBorder.BorderBrush = rimBrush;
		}

		var gridBrush = GetCrossfeedChartGridBrush(isDark);
		if (_crossfeedGridLine0 != null) _crossfeedGridLine0.Stroke = gridBrush;
		if (_crossfeedGridLineMinus2 != null) _crossfeedGridLineMinus2.Stroke = gridBrush;
		if (_crossfeedGridLineMinus4 != null) _crossfeedGridLineMinus4.Stroke = gridBrush;
		if (_crossfeedGridLine100 != null) _crossfeedGridLine100.Stroke = gridBrush;
		if (_crossfeedGridLine1k != null) _crossfeedGridLine1k.Stroke = gridBrush;
		if (_crossfeedGridLine10k != null) _crossfeedGridLine10k.Stroke = gridBrush;

		if (_crossfeedCutoffLine != null) _crossfeedCutoffLine.Stroke = GetCrossfeedCutoffLineBrush(isDark);
		if (_crossfeedCutoffKnob != null)
		{
			_crossfeedCutoffKnob.Background = GetCrossfeedCutoffKnobBgBrush(isDark);
			_crossfeedCutoffKnob.BorderBrush = GetCrossfeedCutoffKnobBorderBrush(isDark);
		}
		if (_crossfeedCutoffBadge != null)
		{
			_crossfeedCutoffBadge.Background = accentSoftBg;
			_crossfeedCutoffBadge.BorderBrush = accentSoftBorder;
		}
		if (_crossfeedCutoffBadgeTb != null) _crossfeedCutoffBadgeTb.Foreground = accentSoftText;

		if (_crossfeedDragHintTag != null)
		{
			_crossfeedDragHintTag.Background = CreateCrossfeedSurfaceBrush(LiquidGlassVariant.Clear, isDark, 0.92);
			_crossfeedDragHintTag.BorderBrush = rimBrush;
		}
		if (_crossfeedDragHintArrowTb != null) _crossfeedDragHintArrowTb.Foreground = mutedText;
		if (_crossfeedDragHintTextTb != null) _crossfeedDragHintTextTb.Foreground = mutedText;

		if (_crossfeedDividerBorder != null) _crossfeedDividerBorder.Background = GetCrossfeedDividerBrush(isDark);

		if (_crossfeedMutualExclusionInfoBar != null)
		{
			_crossfeedMutualExclusionInfoBar.RequestedTheme = isDark ? ElementTheme.Dark : ElementTheme.Light;
		}

		if (_crossfeedModelCombo != null)
		{
			_crossfeedModelCombo.RequestedTheme = isDark ? ElementTheme.Dark : ElementTheme.Light;
			_crossfeedModelCombo.Background = CreateCrossfeedSurfaceBrush(LiquidGlassVariant.Clear, isDark, 0.92);
			_crossfeedModelCombo.BorderBrush = rimBrush;
			_crossfeedModelCombo.Foreground = primaryText;
		}
		if (_crossfeedAmountSlider != null) _crossfeedAmountSlider.RequestedTheme = isDark ? ElementTheme.Dark : ElementTheme.Light;
		if (_crossfeedCutoffSlider != null) _crossfeedCutoffSlider.RequestedTheme = isDark ? ElementTheme.Dark : ElementTheme.Light;

		ApplyCrossfeedNeutralButtonStyle(_crossfeedResetBtn, isDark, _crossfeedResetIcon, _crossfeedResetText);
		ApplyCrossfeedPendingBadgeTheme(isDark);
		UpdateAllCrossfeedVisuals(isDark);
	}

	private void ApplyCrossfeedPendingBadgeTheme(bool isDark)
	{
		if (_crossfeedPendingBadge == null) return;
		_crossfeedPendingBadge.Background = new SolidColorBrush(isDark ? Color.FromArgb(75, 245, 158, 11) : Color.FromArgb(255, 254, 243, 199));
		_crossfeedPendingBadge.BorderBrush = new SolidColorBrush(isDark ? Color.FromArgb(190, 251, 191, 36) : Color.FromArgb(255, 251, 191, 36));
		if (_crossfeedPendingBadgeText != null)
		{
			_crossfeedPendingBadgeText.Foreground = new SolidColorBrush(isDark ? Color.FromArgb(255, 253, 230, 138) : Color.FromArgb(255, 180, 83, 9));
		}
	}

	private static Brush CreateCrossfeedSurfaceBrush(LiquidGlassVariant variant, bool isDark, double opacityScale = 1.0)
	{
		return LiquidGlassEngine.CreateSurfaceBrush(variant, isDark: isDark, opacityScale: opacityScale);
	}

	private static Brush CreateCrossfeedRimBrush(bool isDark, bool isHovered = false, bool isSelected = false)
	{
		return LiquidGlassEngine.CreateSpecularRimBrush(
			LiquidGlassVariant.Regular,
			isDark: isDark,
			isHovered: isHovered,
			isSelected: isSelected);
	}

	private static Brush GetCrossfeedSoftSurfaceBrush(bool isDark)
	{
		return isDark
			? LiquidGlassEngine.CreateSurfaceBrush(LiquidGlassVariant.Clear, isDark: true, opacityScale: 0.45)
			: new SolidColorBrush(Color.FromArgb(220, 241, 245, 249));
	}

	private static Brush GetCrossfeedChartGridBrush(bool isDark)
		=> new SolidColorBrush(isDark ? Color.FromArgb(45, 255, 255, 255) : Color.FromArgb(255, 226, 232, 240));

	private static Brush GetCrossfeedDividerBrush(bool isDark)
		=> new SolidColorBrush(isDark ? Color.FromArgb(45, 255, 255, 255) : Color.FromArgb(255, 226, 232, 240));

	private static Brush GetCrossfeedCutoffLineBrush(bool isDark)
		=> new SolidColorBrush(isDark ? Color.FromArgb(180, 165, 180, 252) : Color.FromArgb(255, 199, 210, 254));

	private static Brush GetCrossfeedCutoffKnobBgBrush(bool isDark)
		=> new SolidColorBrush(isDark ? Color.FromArgb(255, 30, 41, 59) : Colors.White);

	private static Brush GetCrossfeedCutoffKnobBorderBrush(bool isDark)
		=> new SolidColorBrush(isDark ? Color.FromArgb(255, 129, 140, 248) : Color.FromArgb(255, 99, 102, 241));

	private static Brush GetCrossfeedPrimaryTextBrush(bool isDark)
		=> new SolidColorBrush(isDark ? Colors.White : Color.FromArgb(255, 15, 23, 42));

	private static Brush GetCrossfeedSecondaryTextBrush(bool isDark)
		=> new SolidColorBrush(isDark ? Color.FromArgb(255, 248, 250, 252) : Color.FromArgb(255, 51, 65, 85));

	private static Brush GetCrossfeedSubHeaderTextBrush(bool isDark)
		=> new SolidColorBrush(isDark ? Color.FromArgb(255, 226, 232, 240) : Color.FromArgb(255, 71, 85, 105));

	private static Brush GetCrossfeedMutedTextBrush(bool isDark)
		=> new SolidColorBrush(isDark ? Color.FromArgb(255, 203, 213, 225) : Color.FromArgb(255, 100, 116, 139));

	private static Brush GetCrossfeedAccentSoftBgBrush(bool isDark)
		=> new SolidColorBrush(isDark ? Color.FromArgb(85, 99, 102, 241) : Color.FromArgb(255, 238, 242, 255));

	private static Brush GetCrossfeedAccentSoftBorderBrush(bool isDark)
		=> new SolidColorBrush(isDark ? Color.FromArgb(180, 129, 140, 248) : Color.FromArgb(255, 199, 210, 254));

	private static Brush GetCrossfeedAccentSoftTextBrush(bool isDark)
		=> new SolidColorBrush(isDark ? Color.FromArgb(255, 199, 210, 254) : Color.FromArgb(255, 79, 70, 229));

	private static void ApplyCrossfeedNeutralButtonStyle(
		Button? btn,
		bool isDark,
		FontIcon? icon = null,
		TextBlock? label = null,
		Brush? foregroundOverride = null)
	{
		if (btn == null) return;

		Brush bg = CreateCrossfeedSurfaceBrush(LiquidGlassVariant.Clear, isDark, 0.92);
		Brush hoverBg = isDark
			? CreateCrossfeedSurfaceBrush(LiquidGlassVariant.Clear, isDark: true, 1.08)
			: new SolidColorBrush(Color.FromArgb(238, 248, 250, 252));
		Brush pressedBg = isDark
			? CreateCrossfeedSurfaceBrush(LiquidGlassVariant.Regular, isDark: true, 1.0)
			: new SolidColorBrush(Color.FromArgb(245, 226, 232, 240));
		Brush border = CreateCrossfeedRimBrush(isDark);
		Brush hoverBorder = CreateCrossfeedRimBrush(isDark, isHovered: true);
		Brush fg = foregroundOverride ?? GetCrossfeedSecondaryTextBrush(isDark);
		Brush iconFg = foregroundOverride ?? GetCrossfeedSubHeaderTextBrush(isDark);

		ApplyCrossfeedButtonPalette(btn, bg, hoverBg, pressedBg, border, hoverBorder, fg, icon, label, iconFg);
	}

	private static void ApplyCrossfeedAccentButtonStyle(
		Button? btn,
		bool isDark,
		FontIcon? icon = null,
		TextBlock? label = null,
		Brush? borderOverride = null)
	{
		if (btn == null) return;

		Brush bg = GetCrossfeedAccentSoftBgBrush(isDark);
		Brush hoverBg = new SolidColorBrush(isDark ? Color.FromArgb(115, 99, 102, 241) : Color.FromArgb(255, 224, 231, 255));
		Brush pressedBg = new SolidColorBrush(isDark ? Color.FromArgb(135, 99, 102, 241) : Color.FromArgb(255, 199, 210, 254));
		Brush border = borderOverride ?? GetCrossfeedAccentSoftBorderBrush(isDark);
		Brush hoverBorder = new SolidColorBrush(isDark ? Color.FromArgb(230, 165, 180, 252) : Color.FromArgb(255, 79, 70, 229));
		Brush fg = GetCrossfeedAccentSoftTextBrush(isDark);

		ApplyCrossfeedButtonPalette(btn, bg, hoverBg, pressedBg, border, hoverBorder, fg, icon, label);
	}

	private static void ApplyCrossfeedButtonPalette(
		Button? btn,
		Brush background,
		Brush pointerOverBackground,
		Brush pressedBackground,
		Brush borderBrush,
		Brush pointerOverBorderBrush,
		Brush foreground,
		FontIcon? icon = null,
		TextBlock? label = null,
		Brush? iconForeground = null)
	{
		if (btn == null) return;

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
		btn.Resources["ButtonForegroundPointerOver"] = foreground;
		btn.Resources["ButtonForegroundPressed"] = foreground;

		if (icon != null) icon.Foreground = iconForeground ?? foreground;
		if (label != null) label.Foreground = foreground;
	}
	#endregion
}
