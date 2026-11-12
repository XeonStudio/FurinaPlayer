using System;
using System.Collections.Generic;
using FurinaPlayer.UI.Controls;
using FurinaPlayer.UI.ViewModels;
using Microsoft.UI;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using Windows.Foundation;
using Windows.UI;

namespace FurinaPlayer.UI.Pages;

/// <summary>
/// FurinaPlayer 立体声场 (Stereo Field) 二级功能工作台分部实现。
/// 包含 M/S 声像仪能量分布雷达图、可拖拽场宽、中心/侧声道增益控制、单声道低频保留及快速预设胶囊。
/// </summary>
public sealed partial class DspPage
{
	#region Parameter State & Fields
	private FrameworkElement? _stereoFieldWorkbench;

	// 核心参数状态
	private bool _stereoFieldEnabled = false;
	private double _stereoFieldWidth = 1.0;          // 0.0 ~ 2.0 (默认 1.0 即 100%)
	private double _stereoFieldMidGainDb = 0.0;       // -12.0 ~ +6.0 dB (默认 0.0 dB)
	private double _stereoFieldSideGainDb = 0.0;      // -12.0 ~ +6.0 dB (默认 0.0 dB)
	private double _stereoFieldMonoBassHz = 80.0;     // 20 ~ 300 Hz (默认 80 Hz)

	// 已应用状态 (用于草稿判断与 Ghost 曲线)
	private bool _appliedStereoFieldEnabled = false;
	private double _appliedStereoFieldWidth = 1.0;
	private double _appliedStereoFieldMidGainDb = 0.0;
	private double _appliedStereoFieldSideGainDb = 0.0;
	private double _appliedStereoFieldMonoBassHz = 80.0;

	private bool _isUpdatingStereoUi = false;
	private bool _isDraggingRadar = false;

	// 顶部与操作栏控件
	private Border? _sfRootBorder;
	private Border? _sfTitleIconBorder;
	private FontIcon? _sfTitleIcon;
	private TextBlock? _sfSubtitleCatTb;
	private Button? _sfPowerBtn;
	private FontIcon? _sfPowerIcon;
	private TextBlock? _sfPowerText;
	private Button? _sfResetBtn;
	private FontIcon? _sfResetIcon;
	private TextBlock? _sfResetText;
	private Button? _sfApplyBtn;
	private FontIcon? _sfApplyIcon;
	private TextBlock? _sfApplyText;
	private Border? _sfPendingBadge;
	private TextBlock? _sfPendingBadgeText;
	private TextBlock? _sfHeaderWidthBigTb;
	private TextBlock? _sfHeaderWidthSubTb;

	// 左侧声像仪控件
	private Canvas? _radarCanvas;
	private Ellipse? _radarGhostEllipse;
	private Ellipse? _radarActiveEllipse;
	private Ellipse? _radarWidthHandle;
	private Border? _sfRadarDragTag;
	private FontIcon? _sfRadarDragIcon;
	private TextBlock? _sfRadarDragText;
	private Border? _sfRadarCorrTrackBorder;
	private TextBlock? _radarStatMidTb;
	private TextBlock? _radarStatSideTb;
	private TextBlock? _radarCorrelationTb;
	private Border? _radarCorrIndicatorFill;

	// 右侧预设与参数滑块
	private readonly List<Button> _stereoPresetButtons = new();
	private FontIcon? _sfPresetsIcon;
	private Border? _sfControlsDivider;
	private FontIcon? _sfControlsIcon;
	private Slider? _sliderWidth;
	private TextBlock? _tbWidthVal;
	private Slider? _sliderMidGain;
	private TextBlock? _tbMidGainVal;
	private Slider? _sliderSideGain;
	private TextBlock? _tbSideGainVal;
	private Slider? _sliderMonoBass;
	private TextBlock? _tbMonoBassVal;

	// 底部大号读数卡片与提示横幅
	private TextBlock? _bottomWidthBigTb;
	private TextBlock? _bottomWidthSubTb;
	private TextBlock? _bottomMidBigTb;
	private TextBlock? _bottomMidSubTb;
	private TextBlock? _bottomSideBigTb;
	private TextBlock? _bottomSideSubTb;
	private FontIcon? _sfTipIcon;

	// 主题刷新追踪集合
	private readonly List<Border> _sfCardBorders = new();
	private readonly List<Border> _sfSoftBorders = new();
	private readonly List<TextBlock> _sfPrimaryTextBlocks = new();
	private readonly List<TextBlock> _sfSecondaryTextBlocks = new();
	private readonly List<TextBlock> _sfSubHeaderTextBlocks = new();
	private readonly List<TextBlock> _sfMutedTextBlocks = new();
	private readonly List<TextBlock> _sfAccentTextBlocks = new();
	private readonly List<(FontIcon Icon, TextBlock BigTb, Color LightAccent, Color DarkAccent)> _sfBottomStatAccents = new();

	// 常量定义
	private const double RadarCanvasSize = 320.0;
	private const double RadarCenter = 160.0;
	private const double RadarRefRadius = 80.0;  // 1.0x (0 dB) 单位参考圆
	private const double RadarMaxRadius = 145.0; // 最大安全显示半径
	#endregion

	#region Presets Definition
	private sealed class StereoPreset
	{
		public string Id { get; init; } = "";
		public string Name { get; init; } = "";
		public string Subtitle { get; init; } = "";
		public double Width { get; init; }
		public double MidGainDb { get; init; }
		public double SideGainDb { get; init; }
		public double MonoBassHz { get; init; } = 80.0;
	}

	private static readonly StereoPreset[] StereoPresets =
	{
		new() { Id = "mono", Name = "纯单声道", Subtitle = "0% · 纯中置", Width = 0.0, MidGainDb = 0.0, SideGainDb = 0.0, MonoBassHz = 80.0 },
		new() { Id = "focus_mid", Name = "聚焦中置", Subtitle = "70% · 人声突出", Width = 0.70, MidGainDb = 1.5, SideGainDb = 0.0, MonoBassHz = 80.0 },
		new() { Id = "neutral", Name = "原声平衡", Subtitle = "100% · 原始立体声", Width = 1.0, MidGainDb = 0.0, SideGainDb = 0.0, MonoBassHz = 80.0 },
		new() { Id = "wide", Name = "宽阔声场", Subtitle = "130% · 空间延展", Width = 1.30, MidGainDb = 0.0, SideGainDb = 1.5, MonoBassHz = 90.0 },
		new() { Id = "super_wide", Name = "超宽环绕", Subtitle = "175% · 环绕沉浸", Width = 1.75, MidGainDb = 0.0, SideGainDb = 3.0, MonoBassHz = 110.0 }
	};
	#endregion

	#region Main Entry: CreateStereoFieldWorkbench
	public FrameworkElement CreateStereoFieldWorkbench()
	{
		bool isDark = IsDspDark;
		if (_stereoFieldWorkbench != null)
		{
			InitStereoFieldDraftState();
			RefreshStereoFieldTheme(isDark);
			UpdateStereoFieldVisuals();
			return _stereoFieldWorkbench;
		}

		_sfCardBorders.Clear();
		_sfSoftBorders.Clear();
		_sfPrimaryTextBlocks.Clear();
		_sfSecondaryTextBlocks.Clear();
		_sfSubHeaderTextBlocks.Clear();
		_sfMutedTextBlocks.Clear();
		_sfAccentTextBlocks.Clear();
		_sfBottomStatAccents.Clear();
		_stereoPresetButtons.Clear();

		InitStereoFieldDraftState();

		var sv = new ScrollViewer
		{
			VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
			HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled
		};

		var mainStack = new StackPanel
		{
			Spacing = 16,
			Padding = new Thickness(24, 20, 24, 24)
		};

		// 1. 顶部面包屑与大号读数标题
		mainStack.Children.Add(CreateStereoFieldTopHeader());

		// 2. 顶部工具栏 (开关/恢复默认/应用参数/未应用指示)
		mainStack.Children.Add(CreateStereoFieldOperationBar());

		// 3. 主舞台双栏布局 (左: M/S 声像仪雷达, 右: 快速预设与参数控制)
		mainStack.Children.Add(CreateStereoFieldMainStage());

		// 4. 底部三联大号状态卡片 (场宽 / 中置偏置 / 侧向增益)
		mainStack.Children.Add(CreateStereoBottomStatsCards());

		// 5. 底部温馨提示横幅
		mainStack.Children.Add(CreateStereoBottomTipBanner());

		sv.Content = mainStack;
		var rootBorder = new Border
		{
			Background = CreateStereoSurfaceBrush(LiquidGlassVariant.Clear, isDark, Math.Clamp(0.70 / 0.85, 0.55, 1.0)),
			RequestedTheme = isDark ? ElementTheme.Dark : ElementTheme.Light,
			Child = sv
		};

		_sfRootBorder = rootBorder;
		_stereoFieldWorkbench = rootBorder;

		RefreshStereoFieldTheme(isDark);
		UpdateStereoFieldVisuals();
		return _stereoFieldWorkbench;
	}
	#endregion

	#region 1. Top Header
	private FrameworkElement CreateStereoFieldTopHeader()
	{
		var headerGrid = new Grid();
		headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.0, GridUnitType.Star) });
		headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

		// 左侧图标 + 标题 + 面包屑
		var titleSp = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 12, VerticalAlignment = VerticalAlignment.Center };
		_sfTitleIcon = new FontIcon
		{
			Glyph = "\uE895",
			FontSize = 20,
			Foreground = DspAccentSoftTextBrush,
			HorizontalAlignment = HorizontalAlignment.Center,
			VerticalAlignment = VerticalAlignment.Center
		};
		_sfTitleIconBorder = new Border
		{
			Width = 40,
			Height = 40,
			CornerRadius = new CornerRadius(20),
			Background = DspAccentSoftBgBrush,
			Child = _sfTitleIcon
		};
		titleSp.Children.Add(_sfTitleIconBorder);

		var textSp = new StackPanel { Spacing = 2, VerticalAlignment = VerticalAlignment.Center };
		_sfSubtitleCatTb = new TextBlock
		{
			Text = "FurinaPlayer Spatial Workbench · Mid / Side 声像重塑",
			FontSize = 11.5,
			FontWeight = FontWeights.Medium,
			Foreground = DspAccentSoftTextBrush
		};
		_sfAccentTextBlocks.Add(_sfSubtitleCatTb);

		var mainTitleTb = new TextBlock
		{
			Text = "立体声场 (Stereo Field)",
			FontSize = 22,
			FontWeight = FontWeights.Bold,
			Foreground = DspTextPrimaryBrush
		};
		_sfPrimaryTextBlocks.Add(mainTitleTb);

		textSp.Children.Add(_sfSubtitleCatTb);
		textSp.Children.Add(mainTitleTb);
		titleSp.Children.Add(textSp);

		Grid.SetColumn(titleSp, 0);
		headerGrid.Children.Add(titleSp);

		// 右侧立体声场宽读数：大号“100%”，副标“原声平衡”
		var readoutSp = new StackPanel { HorizontalAlignment = HorizontalAlignment.Right, VerticalAlignment = VerticalAlignment.Center };
		_sfHeaderWidthBigTb = new TextBlock
		{
			Text = "100%",
			FontSize = 28,
			FontWeight = FontWeights.Bold,
			Foreground = DspAccentSoftTextBrush,
			HorizontalAlignment = HorizontalAlignment.Right
		};
		_sfAccentTextBlocks.Add(_sfHeaderWidthBigTb);

		_sfHeaderWidthSubTb = new TextBlock
		{
			Text = "原声平衡",
			FontSize = 12,
			FontWeight = FontWeights.Medium,
			Foreground = DspTextMutedBrush,
			HorizontalAlignment = HorizontalAlignment.Right
		};
		_sfMutedTextBlocks.Add(_sfHeaderWidthSubTb);

		readoutSp.Children.Add(_sfHeaderWidthBigTb);
		readoutSp.Children.Add(_sfHeaderWidthSubTb);

		Grid.SetColumn(readoutSp, 1);
		headerGrid.Children.Add(readoutSp);

		return headerGrid;
	}
	#endregion

	#region 2. Operation Bar
	private FrameworkElement CreateStereoFieldOperationBar()
	{
		bool isDark = IsDspDark;
		var container = new StackPanel { Spacing = 10 };

		var barGrid = new Grid();
		barGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.0, GridUnitType.Star) });
		barGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

		// 左侧按钮组: [开关] [恢复默认] [应用参数] [未应用指示]
		var leftSp = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 10, VerticalAlignment = VerticalAlignment.Center };

		// 开关按钮
		_sfPowerIcon = new FontIcon { Glyph = "\uE7E8", FontSize = 14, Foreground = DspTextMutedBrush };
		_sfPowerText = new TextBlock { Text = "立体声场已旁路", FontSize = 13, FontWeight = FontWeights.Medium, Foreground = DspTextSubHeaderBrush };
		var powerSp = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 6, Children = { _sfPowerIcon, _sfPowerText } };
		_sfPowerBtn = new Button
		{
			Content = powerSp,
			Height = 36,
			CornerRadius = new CornerRadius(8),
			Padding = new Thickness(14, 0, 14, 0)
		};
		_sfPowerBtn.Click += (s, e) =>
		{
			_stereoFieldEnabled = !_stereoFieldEnabled;
			PushStereoFieldToDsp(immediatePersistence: true);
			UpdateStereoPowerButtonVisuals();
		};
		leftSp.Children.Add(_sfPowerBtn);

		// 恢复默认按钮
		_sfResetIcon = new FontIcon { Glyph = "\uE777", FontSize = 13, Foreground = DspTextSubHeaderBrush };
		_sfResetText = new TextBlock { Text = "恢复默认", FontSize = 13, Foreground = DspTextSecondaryBrush };
		var resetSp = new StackPanel
		{
			Orientation = Orientation.Horizontal,
			Spacing = 6,
			Children = { _sfResetIcon, _sfResetText }
		};
		_sfResetBtn = new Button
		{
			Content = resetSp,
			Height = 36,
			CornerRadius = new CornerRadius(8),
			Padding = new Thickness(12, 0, 12, 0)
		};
		ApplyStereoNeutralButtonSurface(_sfResetBtn, isDark, DspTextSecondaryBrush);
		_sfResetBtn.Click += (s, e) => ResetStereoFieldDefaults();
		leftSp.Children.Add(_sfResetBtn);

		// 应用参数按钮
		_sfApplyIcon = new FontIcon { Glyph = "\uE74E", FontSize = 13, Foreground = DspTextSubHeaderBrush };
		_sfApplyText = new TextBlock { Text = "应用参数", FontSize = 13, FontWeight = FontWeights.Medium, Foreground = DspTextSecondaryBrush };
		var applySp = new StackPanel
		{
			Orientation = Orientation.Horizontal,
			Spacing = 6,
			Children = { _sfApplyIcon, _sfApplyText }
		};
		_sfApplyBtn = new Button
		{
			Content = applySp,
			Height = 36,
			CornerRadius = new CornerRadius(8),
			Padding = new Thickness(14, 0, 14, 0)
		};
		ApplyStereoNeutralButtonSurface(_sfApplyBtn, isDark, DspTextSecondaryBrush);
		_sfApplyBtn.Click += (s, e) => ApplyStereoFieldSettings();
		leftSp.Children.Add(_sfApplyBtn);

		// 未应用状态胶囊 (Dirty Badge)
		_sfPendingBadgeText = new TextBlock
		{
			Text = "未应用修改",
			FontSize = 11.5,
			FontWeight = FontWeights.SemiBold
		};
		_sfPendingBadge = new Border
		{
			BorderThickness = new Thickness(1),
			CornerRadius = new CornerRadius(6),
			Padding = new Thickness(8, 4, 8, 4),
			VerticalAlignment = VerticalAlignment.Center,
			Visibility = Visibility.Collapsed,
			Child = _sfPendingBadgeText
		};
		ApplyStereoPendingBadgeTheme(isDark);
		leftSp.Children.Add(_sfPendingBadge);

		Grid.SetColumn(leftSp, 0);
		barGrid.Children.Add(leftSp);

		container.Children.Add(barGrid);

		// 描述说明文字
		var descTb = new TextBlock
		{
			Text = "在 Mid / Side 域调节立体声场宽度、中央人声专注度与侧向空间感。0% 为完全单声道，100% 为原始立体声，200% 为超宽空间。",
			FontSize = 12.5,
			Foreground = DspTextMutedBrush,
			TextWrapping = TextWrapping.Wrap
		};
		_sfMutedTextBlocks.Add(descTb);
		container.Children.Add(descTb);

		return container;
	}
	#endregion

	#region 3. Main Stage (Left: M/S Goniometer Radar, Right: Presets & Controls)
	private FrameworkElement CreateStereoFieldMainStage()
	{
		var stageGrid = new Grid { ColumnSpacing = 16 };
		stageGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(400, GridUnitType.Pixel) });
		stageGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.0, GridUnitType.Star) });

		// 左侧：M/S 声像仪能量分布雷达图卡片
		var radarCard = CreateStereoRadarGoniometerCard();
		Grid.SetColumn(radarCard, 0);
		stageGrid.Children.Add(radarCard);

		// 右侧：快速预设与参数控制卡片
		var controlsCard = CreateStereoPresetsAndControlsCard();
		Grid.SetColumn(controlsCard, 1);
		stageGrid.Children.Add(controlsCard);

		return stageGrid;
	}

	private FrameworkElement CreateStereoRadarGoniometerCard()
	{
		bool isDark = IsDspDark;
		var card = new Border
		{
			Background = CreateStereoSurfaceBrush(LiquidGlassVariant.Regular, isDark, Math.Clamp(0.88 / 0.90, 0.72, 1.05)),
			BorderBrush = CreateStereoRimBrush(isDark),
			BorderThickness = new Thickness(1),
			CornerRadius = new CornerRadius(14),
			Padding = new Thickness(18, 16, 18, 16)
		};
		_sfCardBorders.Add(card);

		var stack = new StackPanel { Spacing = 12 };

		// 卡片头部
		var head = new Grid();
		head.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.0, GridUnitType.Star) });
		head.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

		var headLeft = new StackPanel { Spacing = 2 };
		var radarTitleTb = new TextBlock
		{
			Text = "M/S 声像仪",
			FontSize = 15,
			FontWeight = FontWeights.SemiBold,
			Foreground = DspTextPrimaryBrush
		};
		_sfPrimaryTextBlocks.Add(radarTitleTb);
		headLeft.Children.Add(radarTitleTb);

		var radarSubTb = new TextBlock
		{
			Text = "由宽度与增益推导的能量分布椭圆；虚线圆为 0dB 参考。",
			FontSize = 11,
			Foreground = DspTextMutedBrush
		};
		_sfMutedTextBlocks.Add(radarSubTb);
		headLeft.Children.Add(radarSubTb);
		Grid.SetColumn(headLeft, 0);
		head.Children.Add(headLeft);

		_sfRadarDragIcon = new FontIcon { Glyph = "\uE749", FontSize = 10, Foreground = DspAccentSoftTextBrush };
		_sfRadarDragText = new TextBlock { Text = "可拖动 · 调整场宽", FontSize = 10.5, FontWeight = FontWeights.Medium, Foreground = DspAccentSoftTextBrush };
		_sfRadarDragTag = new Border
		{
			Background = DspAccentSoftBgBrush,
			BorderBrush = DspAccentSoftBorderBrush,
			BorderThickness = new Thickness(1),
			CornerRadius = new CornerRadius(10),
			Padding = new Thickness(8, 3, 8, 3),
			VerticalAlignment = VerticalAlignment.Center,
			Child = new StackPanel
			{
				Orientation = Orientation.Horizontal,
				Spacing = 4,
				Children = { _sfRadarDragIcon, _sfRadarDragText }
			}
		};
		Grid.SetColumn(_sfRadarDragTag, 1);
		head.Children.Add(_sfRadarDragTag);

		stack.Children.Add(head);

		// 雷达暗色绘图舞台
		var stageBorder = new Border
		{
			Height = 320,
			CornerRadius = new CornerRadius(12),
			Background = new SolidColorBrush(Color.FromArgb(255, 11, 15, 25)),
			BorderBrush = new SolidColorBrush(Color.FromArgb(120, 51, 65, 85)),
			BorderThickness = new Thickness(1),
			Clip = new RectangleGeometry { Rect = new Rect(0, 0, 360, 320) }
		};

		var viewbox = new Viewbox
		{
			Stretch = Stretch.Uniform,
			Width = 320,
			Height = 320,
			HorizontalAlignment = HorizontalAlignment.Center
		};

		_radarCanvas = new Canvas
		{
			Width = RadarCanvasSize,
			Height = RadarCanvasSize,
			Background = new SolidColorBrush(Colors.Transparent)
		};

		// 1. 45度十字对角辅助标线 (L-R 交叉)
		_radarCanvas.Children.Add(new Line
		{
			X1 = 25, Y1 = 295, X2 = 295, Y2 = 25,
			Stroke = new SolidColorBrush(Color.FromArgb(45, 148, 163, 184)),
			StrokeThickness = 1,
			StrokeDashArray = new DoubleCollection { 3, 3 }
		});
		_radarCanvas.Children.Add(new Line
		{
			X1 = 25, Y1 = 25, X2 = 295, Y2 = 295,
			Stroke = new SolidColorBrush(Color.FromArgb(45, 148, 163, 184)),
			StrokeThickness = 1,
			StrokeDashArray = new DoubleCollection { 3, 3 }
		});

		// 2. M(纵向中置) 与 S(横向侧声道) 标线
		_radarCanvas.Children.Add(new Line
		{
			X1 = RadarCenter, Y1 = 15, X2 = RadarCenter, Y2 = 305,
			Stroke = new SolidColorBrush(Color.FromArgb(80, 148, 163, 184)),
			StrokeThickness = 1.2
		});
		_radarCanvas.Children.Add(new Line
		{
			X1 = 15, Y1 = RadarCenter, X2 = 305, Y2 = RadarCenter,
			Stroke = new SolidColorBrush(Color.FromArgb(80, 148, 163, 184)),
			StrokeThickness = 1.2
		});

		// 3. 刻度同心圆
		// 0.5x 刻度圈
		var circle05 = new Ellipse
		{
			Width = RadarRefRadius,
			Height = RadarRefRadius,
			Stroke = new SolidColorBrush(Color.FromArgb(30, 148, 163, 184)),
			StrokeThickness = 1,
			StrokeDashArray = new DoubleCollection { 2, 4 }
		};
		Canvas.SetLeft(circle05, RadarCenter - RadarRefRadius / 2);
		Canvas.SetTop(circle05, RadarCenter - RadarRefRadius / 2);
		_radarCanvas.Children.Add(circle05);

		// 1.0x (0 dB) 单位参考圆
		var circle10 = new Ellipse
		{
			Width = RadarRefRadius * 2,
			Height = RadarRefRadius * 2,
			Stroke = new SolidColorBrush(Color.FromArgb(90, 56, 189, 248)),
			StrokeThickness = 1.2,
			StrokeDashArray = new DoubleCollection { 4, 4 }
		};
		Canvas.SetLeft(circle10, RadarCenter - RadarRefRadius);
		Canvas.SetTop(circle10, RadarCenter - RadarRefRadius);
		_radarCanvas.Children.Add(circle10);

		// 1.5x 刻度圈
		var circle15 = new Ellipse
		{
			Width = RadarRefRadius * 3,
			Height = RadarRefRadius * 3,
			Stroke = new SolidColorBrush(Color.FromArgb(30, 148, 163, 184)),
			StrokeThickness = 1,
			StrokeDashArray = new DoubleCollection { 2, 4 }
		};
		Canvas.SetLeft(circle15, RadarCenter - RadarRefRadius * 1.5);
		Canvas.SetTop(circle15, RadarCenter - RadarRefRadius * 1.5);
		_radarCanvas.Children.Add(circle15);

		// 4. 文字标示 (M, S, L, R, 0dB)
		AddStereoCanvasText(_radarCanvas, "M", RadarCenter - 6, 8, new SolidColorBrush(Color.FromArgb(200, 148, 163, 184)), 11, true);
		AddStereoCanvasText(_radarCanvas, "S", 307, RadarCenter - 8, new SolidColorBrush(Color.FromArgb(200, 148, 163, 184)), 11, true);
		AddStereoCanvasText(_radarCanvas, "L", 22, 16, new SolidColorBrush(Color.FromArgb(180, 56, 189, 248)), 11, true);
		AddStereoCanvasText(_radarCanvas, "R", 290, 16, new SolidColorBrush(Color.FromArgb(180, 56, 189, 248)), 11, true);
		AddStereoCanvasText(_radarCanvas, "0 dB", RadarCenter + 6, RadarCenter - RadarRefRadius + 4, new SolidColorBrush(Color.FromArgb(120, 148, 163, 184)), 9);

		// 5. 幽灵椭圆 (已应用参考)
		_radarGhostEllipse = new Ellipse
		{
			Stroke = new SolidColorBrush(Color.FromArgb(160, 168, 85, 247)),
			StrokeThickness = 1.5,
			StrokeDashArray = new DoubleCollection { 3, 3 },
			Visibility = Visibility.Collapsed
		};
		_radarCanvas.Children.Add(_radarGhostEllipse);

		// 6. 动态声像椭圆 / 能量分布光晕
		_radarActiveEllipse = new Ellipse
		{
			Fill = new SolidColorBrush(Color.FromArgb(40, 6, 182, 212)),
			Stroke = new SolidColorBrush(Color.FromArgb(240, 6, 182, 212)),
			StrokeThickness = 2.5
		};
		_radarCanvas.Children.Add(_radarActiveEllipse);

		// 7. 可拖动手柄
		_radarWidthHandle = new Ellipse
		{
			Width = 14,
			Height = 14,
			Fill = new SolidColorBrush(Colors.White),
			Stroke = new SolidColorBrush(Color.FromArgb(255, 6, 182, 212)),
			StrokeThickness = 2.5
		};
		_radarCanvas.Children.Add(_radarWidthHandle);

		// 交互事件监听
		_radarCanvas.PointerPressed += OnRadarPointerPressed;
		_radarCanvas.PointerMoved += OnRadarPointerMoved;
		_radarCanvas.PointerReleased += OnRadarPointerReleased;
		_radarCanvas.PointerCanceled += (s, e) => { _isDraggingRadar = false; };

		viewbox.Child = _radarCanvas;
		stageBorder.Child = viewbox;
		stack.Children.Add(stageBorder);

		// 底部读数与相位相关度卡片
		var footerCard = new Border
		{
			Background = GetStereoSoftSurfaceBrush(isDark),
			BorderBrush = CreateStereoRimBrush(isDark),
			BorderThickness = new Thickness(1),
			CornerRadius = new CornerRadius(10),
			Padding = new Thickness(12, 10, 12, 10)
		};
		_sfSoftBorders.Add(footerCard);

		var footerStack = new StackPanel { Spacing = 6 };

		var energyRow = new Grid();
		energyRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.0, GridUnitType.Star) });
		energyRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.0, GridUnitType.Star) });

		_radarStatMidTb = new TextBlock
		{
			Text = "中置能量: M × 1.00 (0.0 dB)",
			FontSize = 11.5,
			FontWeight = FontWeights.Medium,
			Foreground = DspTextSecondaryBrush
		};
		_sfSecondaryTextBlocks.Add(_radarStatMidTb);
		Grid.SetColumn(_radarStatMidTb, 0);
		energyRow.Children.Add(_radarStatMidTb);

		_radarStatSideTb = new TextBlock
		{
			Text = "侧向能量: S × 1.00 (0.0 dB)",
			FontSize = 11.5,
			FontWeight = FontWeights.Medium,
			Foreground = DspTextSecondaryBrush,
			HorizontalAlignment = HorizontalAlignment.Right
		};
		_sfSecondaryTextBlocks.Add(_radarStatSideTb);
		Grid.SetColumn(_radarStatSideTb, 1);
		energyRow.Children.Add(_radarStatSideTb);

		footerStack.Children.Add(energyRow);

		// 相位相关度标尺
		var corrRow = new Grid();
		corrRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
		corrRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.0, GridUnitType.Star) });
		corrRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

		var corrLabel = new TextBlock
		{
			Text = "声像相关性",
			FontSize = 11,
			Foreground = DspTextMutedBrush,
			VerticalAlignment = VerticalAlignment.Center,
			Margin = new Thickness(0, 0, 8, 0)
		};
		_sfMutedTextBlocks.Add(corrLabel);
		Grid.SetColumn(corrLabel, 0);
		corrRow.Children.Add(corrLabel);

		// 相关度指示槽 (-1.0 到 +1.0)
		_sfRadarCorrTrackBorder = new Border
		{
			Height = 6,
			CornerRadius = new CornerRadius(3),
			Background = GetStereoMeterTrackBrush(isDark),
			VerticalAlignment = VerticalAlignment.Center,
			Margin = new Thickness(4, 0, 8, 0)
		};
		var trackGrid = new Grid();
		_radarCorrIndicatorFill = new Border
		{
			Width = 36,
			Height = 6,
			CornerRadius = new CornerRadius(3),
			Background = new SolidColorBrush(Color.FromArgb(255, 16, 185, 129)),
			HorizontalAlignment = HorizontalAlignment.Center
		};
		trackGrid.Children.Add(_radarCorrIndicatorFill);
		_sfRadarCorrTrackBorder.Child = trackGrid;
		Grid.SetColumn(_sfRadarCorrTrackBorder, 1);
		corrRow.Children.Add(_sfRadarCorrTrackBorder);

		_radarCorrelationTb = new TextBlock
		{
			Text = "+1.00 (聚焦中置)",
			FontSize = 11,
			FontWeight = FontWeights.SemiBold,
			Foreground = new SolidColorBrush(Color.FromArgb(255, 16, 185, 129)),
			VerticalAlignment = VerticalAlignment.Center
		};
		Grid.SetColumn(_radarCorrelationTb, 2);
		corrRow.Children.Add(_radarCorrelationTb);

		footerStack.Children.Add(corrRow);
		footerCard.Child = footerStack;
		stack.Children.Add(footerCard);

		card.Child = stack;
		return card;
	}
	#endregion

	#region Radar Pointer Handling
	private void OnRadarPointerPressed(object sender, PointerRoutedEventArgs e)
	{
		if (_radarCanvas == null) return;
		_radarCanvas.CapturePointer(e.Pointer);
		_isDraggingRadar = true;
		var pt = e.GetCurrentPoint(_radarCanvas).Position;
		UpdateWidthFromRadarPointer(pt.X);
		e.Handled = true;
	}

	private void OnRadarPointerMoved(object sender, PointerRoutedEventArgs e)
	{
		if (!_isDraggingRadar || _radarCanvas == null) return;
		var pt = e.GetCurrentPoint(_radarCanvas).Position;
		UpdateWidthFromRadarPointer(pt.X);
		e.Handled = true;
	}

	private void OnRadarPointerReleased(object sender, PointerRoutedEventArgs e)
	{
		if (_isDraggingRadar && _radarCanvas != null)
		{
			_isDraggingRadar = false;
			_radarCanvas.ReleasePointerCapture(e.Pointer);
			e.Handled = true;
		}
	}

	private void UpdateWidthFromRadarPointer(double pointerX)
	{
		double dx = Math.Abs(pointerX - RadarCenter);
		double sideGainLin = Math.Pow(10.0, _stereoFieldSideGainDb / 20.0);
		double calculatedWidth = Math.Clamp(dx / (RadarRefRadius * Math.Max(sideGainLin, 0.001)), 0.0, 2.0);
		calculatedWidth = Math.Round(calculatedWidth * 100.0) / 100.0;

		SetStereoFieldWidth(calculatedWidth, fromDrag: true);
	}
	#endregion

	#region 4. Presets and Controls Card
	private FrameworkElement CreateStereoPresetsAndControlsCard()
	{
		bool isDark = IsDspDark;
		var card = new Border
		{
			Background = CreateStereoSurfaceBrush(LiquidGlassVariant.Regular, isDark, Math.Clamp(0.88 / 0.90, 0.72, 1.05)),
			BorderBrush = CreateStereoRimBrush(isDark),
			BorderThickness = new Thickness(1),
			CornerRadius = new CornerRadius(14),
			Padding = new Thickness(20, 16, 20, 16)
		};
		_sfCardBorders.Add(card);

		var stack = new StackPanel { Spacing = 16 };

		// Section A: 快速预设胶囊
		var presetsSection = new StackPanel { Spacing = 10 };
		var presetsHead = new Grid();
		presetsHead.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.0, GridUnitType.Star) });
		presetsHead.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

		var presetsTitleSp = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 6, VerticalAlignment = VerticalAlignment.Center };
		_sfPresetsIcon = new FontIcon { Glyph = "\uE8B7", FontSize = 13, Foreground = DspAccentSoftTextBrush };
		var presetsTitleTb = new TextBlock
		{
			Text = "快速预设 (PRESETS)",
			FontSize = 13,
			FontWeight = FontWeights.Bold,
			Foreground = DspTextPrimaryBrush
		};
		_sfPrimaryTextBlocks.Add(presetsTitleTb);
		presetsTitleSp.Children.Add(_sfPresetsIcon);
		presetsTitleSp.Children.Add(presetsTitleTb);
		Grid.SetColumn(presetsTitleSp, 0);
		presetsHead.Children.Add(presetsTitleSp);

		var presetsSub = new TextBlock
		{
			Text = "一键套用经典声像比例",
			FontSize = 11,
			Foreground = DspTextMutedBrush,
			VerticalAlignment = VerticalAlignment.Center
		};
		_sfMutedTextBlocks.Add(presetsSub);
		Grid.SetColumn(presetsSub, 1);
		presetsHead.Children.Add(presetsSub);
		presetsSection.Children.Add(presetsHead);

		// 胶囊预设按钮排布 (两行弹性网格，均匀且不拥挤)
		var capsulesGrid = new Grid { RowSpacing = 8, ColumnSpacing = 8 };
		capsulesGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
		capsulesGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
		capsulesGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.0, GridUnitType.Star) });
		capsulesGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.0, GridUnitType.Star) });
		capsulesGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.0, GridUnitType.Star) });

		_stereoPresetButtons.Clear();

		for (int i = 0; i < StereoPresets.Length; i++)
		{
			var preset = StereoPresets[i];
			var btn = CreateStereoPresetCapsuleButton(preset, isDark);
			_stereoPresetButtons.Add(btn);

			int row = i < 3 ? 0 : 1;
			int col = i < 3 ? i : (i - 3);

			Grid.SetRow(btn, row);
			Grid.SetColumn(btn, col);
			if (i == 4)
			{
				Grid.SetColumnSpan(btn, 2); // 最后一个占据剩余空间
			}
			capsulesGrid.Children.Add(btn);
		}
		presetsSection.Children.Add(capsulesGrid);
		stack.Children.Add(presetsSection);

		// 分割线
		_sfControlsDivider = new Border
		{
			Height = 1,
			Background = GetStereoDividerBrush(isDark),
			Margin = new Thickness(0, 4, 0, 4)
		};
		stack.Children.Add(_sfControlsDivider);

		// Section B: 参数控制
		var controlsSection = new StackPanel { Spacing = 12 };
		var controlsHead = new Grid();
		controlsHead.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.0, GridUnitType.Star) });
		controlsHead.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

		var controlsTitleSp = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 6, VerticalAlignment = VerticalAlignment.Center };
		_sfControlsIcon = new FontIcon { Glyph = "\uE994", FontSize = 13, Foreground = DspAccentSoftTextBrush };
		var controlsTitleTb = new TextBlock
		{
			Text = "参数控制 (CONTROLS)",
			FontSize = 13,
			FontWeight = FontWeights.Bold,
			Foreground = DspTextPrimaryBrush
		};
		_sfPrimaryTextBlocks.Add(controlsTitleTb);
		controlsTitleSp.Children.Add(_sfControlsIcon);
		controlsTitleSp.Children.Add(controlsTitleTb);
		Grid.SetColumn(controlsTitleSp, 0);
		controlsHead.Children.Add(controlsTitleSp);

		var controlsHint = new TextBlock
		{
			Text = "双击滑块可恢复默认",
			FontSize = 11,
			Foreground = DspTextMutedBrush,
			VerticalAlignment = VerticalAlignment.Center
		};
		_sfMutedTextBlocks.Add(controlsHint);
		Grid.SetColumn(controlsHint, 1);
		controlsHead.Children.Add(controlsHint);
		controlsSection.Children.Add(controlsHead);

		// 滑块 1: 场宽 (Width) 0% ~ 200%
		var widthSliderCard = CreateStereoSliderRow(
			"场宽 (Stereo Width)",
			"0% (单声道) 至 200% (超宽)",
			0.0, 200.0, 1.0,
			_stereoFieldWidth * 100.0,
			100.0,
			val => $"{Math.Round(val)}%",
			out _sliderWidth,
			out _tbWidthVal,
			newVal => SetStereoFieldWidth(newVal / 100.0)
		);
		controlsSection.Children.Add(widthSliderCard);

		// 滑块 2: 中置增益 (Mid Gain) -12.0 ~ +6.0 dB
		var midSliderCard = CreateStereoSliderRow(
			"中置增益 (Mid Gain)",
			"调整居中人声与主乐器音量权重",
			-12.0, 6.0, 0.1,
			_stereoFieldMidGainDb,
			0.0,
			val => $"{val:+0.0;-0.0;0.0} dB",
			out _sliderMidGain,
			out _tbMidGainVal,
			newVal => SetStereoFieldMidGain(newVal)
		);
		controlsSection.Children.Add(midSliderCard);

		// 滑块 3: 侧边增益 (Side Gain) -12.0 ~ +6.0 dB
		var sideSliderCard = CreateStereoSliderRow(
			"侧向增益 (Side Gain)",
			"增强或抑制声场两侧空间氛围",
			-12.0, 6.0, 0.1,
			_stereoFieldSideGainDb,
			0.0,
			val => $"{val:+0.0;-0.0;0.0} dB",
			out _sliderSideGain,
			out _tbSideGainVal,
			newVal => SetStereoFieldSideGain(newVal)
		);
		controlsSection.Children.Add(sideSliderCard);

		// 滑块 4: 单声道低频保留 20 ~ 300 Hz
		var monoBassSliderCard = CreateStereoSliderRow(
			"单声道低频保留 (Mono Bass)",
			"将设定频点以下合并为单声道，保障低音扎实下潜",
			20.0, 300.0, 1.0,
			_stereoFieldMonoBassHz,
			80.0,
			val => $"{Math.Round(val)} Hz",
			out _sliderMonoBass,
			out _tbMonoBassVal,
			newVal => SetStereoFieldMonoBass(newVal)
		);
		controlsSection.Children.Add(monoBassSliderCard);

		stack.Children.Add(controlsSection);
		card.Child = stack;
		return card;
	}

	private Button CreateStereoPresetCapsuleButton(StereoPreset preset, bool isDark)
	{
		var btn = new Button
		{
			Height = 44,
			CornerRadius = new CornerRadius(10),
			Padding = new Thickness(10, 4, 10, 4),
			HorizontalAlignment = HorizontalAlignment.Stretch,
			HorizontalContentAlignment = HorizontalAlignment.Center,
			Tag = preset
		};
		ApplyStereoNeutralButtonSurface(btn, isDark, DspTextSecondaryBrush);

		var sp = new StackPanel { Spacing = 2, HorizontalAlignment = HorizontalAlignment.Center };
		var titleTb = new TextBlock
		{
			Text = preset.Name,
			FontSize = 12,
			FontWeight = FontWeights.SemiBold,
			Foreground = DspTextSecondaryBrush,
			HorizontalAlignment = HorizontalAlignment.Center
		};
		var subTb = new TextBlock
		{
			Text = preset.Subtitle,
			FontSize = 9.5,
			Foreground = DspTextMutedBrush,
			HorizontalAlignment = HorizontalAlignment.Center
		};
		sp.Children.Add(titleTb);
		sp.Children.Add(subTb);
		btn.Content = sp;

		btn.Click += (s, e) => ApplyStereoPreset(preset);
		return btn;
	}

	private FrameworkElement CreateStereoSliderRow(
		string label,
		string tip,
		double min,
		double max,
		double step,
		double initialVal,
		double defaultVal,
		Func<double, string> formatVal,
		out Slider outSlider,
		out TextBlock outValTb,
		Action<double> onValueChanged)
	{
		bool isDark = IsDspDark;
		var container = new Border
		{
			Background = GetStereoSoftSurfaceBrush(isDark),
			BorderBrush = CreateStereoRimBrush(isDark),
			BorderThickness = new Thickness(1),
			CornerRadius = new CornerRadius(10),
			Padding = new Thickness(12, 8, 12, 8)
		};
		_sfSoftBorders.Add(container);

		var stack = new StackPanel { Spacing = 4 };

		var headerGrid = new Grid();
		headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.0, GridUnitType.Star) });
		headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

		var labelSp = new StackPanel { Spacing = 1 };
		var labelTb = new TextBlock
		{
			Text = label,
			FontSize = 12.5,
			FontWeight = FontWeights.SemiBold,
			Foreground = DspTextPrimaryBrush
		};
		_sfPrimaryTextBlocks.Add(labelTb);

		var tipTb = new TextBlock
		{
			Text = tip,
			FontSize = 10,
			Foreground = DspTextMutedBrush
		};
		_sfMutedTextBlocks.Add(tipTb);

		labelSp.Children.Add(labelTb);
		labelSp.Children.Add(tipTb);
		Grid.SetColumn(labelSp, 0);
		headerGrid.Children.Add(labelSp);

		var valTb = new TextBlock
		{
			Text = formatVal(initialVal),
			FontSize = 13,
			FontWeight = FontWeights.Bold,
			Foreground = DspAccentSoftTextBrush,
			VerticalAlignment = VerticalAlignment.Center
		};
		_sfAccentTextBlocks.Add(valTb);
		Grid.SetColumn(valTb, 1);
		headerGrid.Children.Add(valTb);
		stack.Children.Add(headerGrid);

		var slider = new Slider
		{
			Minimum = min,
			Maximum = max,
			StepFrequency = step,
			Value = initialVal,
			Margin = new Thickness(0, 0, 0, 0)
		};

		slider.ValueChanged += (s, e) =>
		{
			if (_isUpdatingStereoUi) return;
			valTb.Text = formatVal(e.NewValue);
			onValueChanged(e.NewValue);
		};

		// 双击恢复默认
		slider.DoubleTapped += (s, e) =>
		{
			slider.Value = defaultVal;
			e.Handled = true;
		};
		container.DoubleTapped += (s, e) =>
		{
			slider.Value = defaultVal;
			e.Handled = true;
		};

		stack.Children.Add(slider);
		container.Child = stack;

		outSlider = slider;
		outValTb = valTb;
		return container;
	}
	#endregion

	#region 5. Bottom Stats Cards
	private FrameworkElement CreateStereoBottomStatsCards()
	{
		var grid = new Grid { ColumnSpacing = 16 };
		grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.0, GridUnitType.Star) });
		grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.0, GridUnitType.Star) });
		grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.0, GridUnitType.Star) });

		// 卡片 1: 立体声场宽
		var card1 = CreateStereoSingleStatCard(
			"立体声场宽",
			"\uE895",
			"100%",
			"原声平衡",
			Color.FromArgb(255, 79, 70, 229),
			Color.FromArgb(255, 165, 180, 252),
			out _bottomWidthBigTb,
			out _bottomWidthSubTb);
		Grid.SetColumn(card1, 0);
		grid.Children.Add(card1);

		// 卡片 2: 中置偏置 (Mid)
		var card2 = CreateStereoSingleStatCard(
			"中置偏置 (Mid)",
			"\uE994",
			"0.0 dB",
			"中央人声基准",
			Color.FromArgb(255, 5, 150, 105),
			Color.FromArgb(255, 52, 211, 153),
			out _bottomMidBigTb,
			out _bottomMidSubTb);
		Grid.SetColumn(card2, 1);
		grid.Children.Add(card2);

		// 卡片 3: 侧向增益 (Side)
		var card3 = CreateStereoSingleStatCard(
			"侧向增益 (Side)",
			"\uE8B8",
			"0.0 dB",
			"空间氛围自然",
			Color.FromArgb(255, 139, 92, 246),
			Color.FromArgb(255, 196, 181, 253),
			out _bottomSideBigTb,
			out _bottomSideSubTb);
		Grid.SetColumn(card3, 2);
		grid.Children.Add(card3);

		return grid;
	}

	private Border CreateStereoSingleStatCard(
		string title,
		string glyph,
		string initialBig,
		string initialSub,
		Color lightAccentColor,
		Color darkAccentColor,
		out TextBlock outBigTb,
		out TextBlock outSubTb)
	{
		bool isDark = IsDspDark;
		var accentBrush = new SolidColorBrush(isDark ? darkAccentColor : lightAccentColor);
		var card = new Border
		{
			Background = CreateStereoSurfaceBrush(LiquidGlassVariant.Regular, isDark, Math.Clamp(0.88 / 0.90, 0.72, 1.05)),
			BorderBrush = CreateStereoRimBrush(isDark),
			BorderThickness = new Thickness(1),
			CornerRadius = new CornerRadius(12),
			Padding = new Thickness(16, 14, 16, 14)
		};
		_sfCardBorders.Add(card);

		var stack = new StackPanel { Spacing = 6 };

		var topGrid = new Grid();
		topGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.0, GridUnitType.Star) });
		topGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

		var titleTb = new TextBlock
		{
			Text = title,
			FontSize = 12,
			FontWeight = FontWeights.Medium,
			Foreground = DspTextMutedBrush
		};
		_sfMutedTextBlocks.Add(titleTb);
		Grid.SetColumn(titleTb, 0);
		topGrid.Children.Add(titleTb);

		var icon = new FontIcon
		{
			Glyph = glyph,
			FontSize = 14,
			Foreground = accentBrush
		};
		Grid.SetColumn(icon, 1);
		topGrid.Children.Add(icon);
		stack.Children.Add(topGrid);

		var bigTb = new TextBlock
		{
			Text = initialBig,
			FontSize = 24,
			FontWeight = FontWeights.Bold,
			Foreground = accentBrush
		};
		stack.Children.Add(bigTb);

		var subTb = new TextBlock
		{
			Text = initialSub,
			FontSize = 11,
			Foreground = DspTextMutedBrush
		};
		_sfMutedTextBlocks.Add(subTb);
		stack.Children.Add(subTb);

		_sfBottomStatAccents.Add((icon, bigTb, lightAccentColor, darkAccentColor));

		card.Child = stack;
		outBigTb = bigTb;
		outSubTb = subTb;
		return card;
	}
	#endregion

	#region 6. Bottom Tip Banner
	private FrameworkElement CreateStereoBottomTipBanner()
	{
		bool isDark = IsDspDark;
		var border = new Border
		{
			Background = GetStereoSoftSurfaceBrush(isDark),
			BorderBrush = CreateStereoRimBrush(isDark),
			BorderThickness = new Thickness(1),
			CornerRadius = new CornerRadius(10),
			Padding = new Thickness(16, 12, 16, 12)
		};
		_sfSoftBorders.Add(border);

		var sp = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 10, VerticalAlignment = VerticalAlignment.Center };
		_sfTipIcon = new FontIcon
		{
			Glyph = "\uE946",
			FontSize = 16,
			Foreground = DspAccentSoftTextBrush,
			VerticalAlignment = VerticalAlignment.Center
		};
		sp.Children.Add(_sfTipIcon);

		var tipTextTb = new TextBlock
		{
			Text = "双击滑块可恢复默认值。高宽度的侧向声场建议配合低频保留，以保持低音下潜扎实。",
			FontSize = 12.5,
			Foreground = DspTextSubHeaderBrush,
			VerticalAlignment = VerticalAlignment.Center
		};
		_sfSubHeaderTextBlocks.Add(tipTextTb);
		sp.Children.Add(tipTextTb);

		border.Child = sp;
		return border;
	}
	#endregion

	#region State Synchronization & DSP Dispatch
	private void InitStereoFieldDraftState()
	{
		if (ViewModel != null)
		{
			_stereoFieldEnabled = ViewModel.EchoStereoFieldEnabled;
			_stereoFieldWidth = ViewModel.EchoStereoWidth;
			_stereoFieldMidGainDb = ViewModel.EchoStereoCenterGainDb;
			_stereoFieldSideGainDb = ViewModel.EchoStereoSideGainDb;
			_stereoFieldMonoBassHz = 80.0;
		}
		else
		{
			_stereoFieldEnabled = false;
			_stereoFieldWidth = 1.0;
			_stereoFieldMidGainDb = 0.0;
			_stereoFieldSideGainDb = 0.0;
			_stereoFieldMonoBassHz = 80.0;
		}

		_appliedStereoFieldEnabled = _stereoFieldEnabled;
		_appliedStereoFieldWidth = _stereoFieldWidth;
		_appliedStereoFieldMidGainDb = _stereoFieldMidGainDb;
		_appliedStereoFieldSideGainDb = _stereoFieldSideGainDb;
		_appliedStereoFieldMonoBassHz = _stereoFieldMonoBassHz;
	}

	private void SetStereoFieldWidth(double width, bool fromDrag = false)
	{
		_stereoFieldWidth = Math.Clamp(width, 0.0, 2.0);

		if (!fromDrag && _sliderWidth != null && Math.Abs(_sliderWidth.Value - _stereoFieldWidth * 100.0) > 0.01)
		{
			_isUpdatingStereoUi = true;
			_sliderWidth.Value = _stereoFieldWidth * 100.0;
			_isUpdatingStereoUi = false;
		}
		else if (fromDrag && _sliderWidth != null)
		{
			_isUpdatingStereoUi = true;
			_sliderWidth.Value = _stereoFieldWidth * 100.0;
			_isUpdatingStereoUi = false;
		}

		if (_tbWidthVal != null)
		{
			_tbWidthVal.Text = $"{Math.Round(_stereoFieldWidth * 100.0)}%";
		}

		PushStereoFieldToDsp(immediatePersistence: false);
		UpdateStereoFieldVisuals();
	}

	private void SetStereoFieldMidGain(double gainDb)
	{
		_stereoFieldMidGainDb = Math.Clamp(gainDb, -12.0, 6.0);
		PushStereoFieldToDsp(immediatePersistence: false);
		UpdateStereoFieldVisuals();
	}

	private void SetStereoFieldSideGain(double gainDb)
	{
		_stereoFieldSideGainDb = Math.Clamp(gainDb, -12.0, 6.0);
		PushStereoFieldToDsp(immediatePersistence: false);
		UpdateStereoFieldVisuals();
	}

	private void SetStereoFieldMonoBass(double hz)
	{
		_stereoFieldMonoBassHz = Math.Clamp(hz, 20.0, 300.0);
		UpdateStereoDirtyStateBadge();
	}

	private void ApplyStereoPreset(StereoPreset preset)
	{
		_isUpdatingStereoUi = true;

		_stereoFieldWidth = preset.Width;
		_stereoFieldMidGainDb = preset.MidGainDb;
		_stereoFieldSideGainDb = preset.SideGainDb;
		_stereoFieldMonoBassHz = preset.MonoBassHz;

		if (_sliderWidth != null) _sliderWidth.Value = _stereoFieldWidth * 100.0;
		if (_tbWidthVal != null) _tbWidthVal.Text = $"{Math.Round(_stereoFieldWidth * 100.0)}%";

		if (_sliderMidGain != null) _sliderMidGain.Value = _stereoFieldMidGainDb;
		if (_tbMidGainVal != null) _tbMidGainVal.Text = $"{_stereoFieldMidGainDb:+0.0;-0.0;0.0} dB";

		if (_sliderSideGain != null) _sliderSideGain.Value = _stereoFieldSideGainDb;
		if (_tbSideGainVal != null) _tbSideGainVal.Text = $"{_stereoFieldSideGainDb:+0.0;-0.0;0.0} dB";

		if (_sliderMonoBass != null) _sliderMonoBass.Value = _stereoFieldMonoBassHz;
		if (_tbMonoBassVal != null) _tbMonoBassVal.Text = $"{Math.Round(_stereoFieldMonoBassHz)} Hz";

		_isUpdatingStereoUi = false;

		PushStereoFieldToDsp(immediatePersistence: false);
		UpdateStereoFieldVisuals();
	}

	private void ResetStereoFieldDefaults()
	{
		_isUpdatingStereoUi = true;

		_stereoFieldWidth = 1.0;
		_stereoFieldMidGainDb = 0.0;
		_stereoFieldSideGainDb = 0.0;
		_stereoFieldMonoBassHz = 80.0;

		if (_sliderWidth != null) _sliderWidth.Value = 100.0;
		if (_tbWidthVal != null) _tbWidthVal.Text = "100%";

		if (_sliderMidGain != null) _sliderMidGain.Value = 0.0;
		if (_tbMidGainVal != null) _tbMidGainVal.Text = "0.0 dB";

		if (_sliderSideGain != null) _sliderSideGain.Value = 0.0;
		if (_tbSideGainVal != null) _tbSideGainVal.Text = "0.0 dB";

		if (_sliderMonoBass != null) _sliderMonoBass.Value = 80.0;
		if (_tbMonoBassVal != null) _tbMonoBassVal.Text = "80 Hz";

		_isUpdatingStereoUi = false;

		PushStereoFieldToDsp(immediatePersistence: false);
		UpdateStereoFieldVisuals();
	}

	private void ApplyStereoFieldSettings()
	{
		_appliedStereoFieldEnabled = _stereoFieldEnabled;
		_appliedStereoFieldWidth = _stereoFieldWidth;
		_appliedStereoFieldMidGainDb = _stereoFieldMidGainDb;
		_appliedStereoFieldSideGainDb = _stereoFieldSideGainDb;
		_appliedStereoFieldMonoBassHz = _stereoFieldMonoBassHz;

		PushStereoFieldToDsp(immediatePersistence: true);

		UpdateStereoDirtyStateBadge();
		UpdateStereoRadarVisuals();
	}

	/// <summary>
	/// 对接底层 DSP 与 ViewModel
	/// </summary>
	private void PushStereoFieldToDsp(bool immediatePersistence)
	{
		if (ViewModel == null) return;

		ViewModel.EchoStereoFieldEnabled = _stereoFieldEnabled;
		ViewModel.EchoStereoWidth = _stereoFieldWidth;
		ViewModel.EchoStereoCenterGainDb = _stereoFieldMidGainDb;
		ViewModel.EchoStereoSideGainDb = _stereoFieldSideGainDb;

		// 调用 AudioEngine 底层方法 SetFurinaStereoField
		ViewModel.AudioEngine?.SetFurinaStereoField(
			_stereoFieldEnabled,
			(float)_stereoFieldWidth,
			(float)_stereoFieldMidGainDb,
			(float)_stereoFieldSideGainDb);

		if (immediatePersistence)
		{
			ViewModel.SaveEchoSettings();
		}
	}
	#endregion

	#region Visuals Refresh & Radar Math
	private static Brush CreateStereoSurfaceBrush(LiquidGlassVariant variant, bool isDark, double opacityScale = 1.0)
	{
		return LiquidGlassEngine.CreateSurfaceBrush(variant, isDark: isDark, opacityScale: opacityScale);
	}

	private static Brush CreateStereoRimBrush(bool isDark, bool isHovered = false, bool isSelected = false)
	{
		return LiquidGlassEngine.CreateSpecularRimBrush(
			LiquidGlassVariant.Regular,
			isDark: isDark,
			isHovered: isHovered,
			isSelected: isSelected);
	}

	private static Brush GetStereoSoftSurfaceBrush(bool isDark)
	{
		return isDark
			? LiquidGlassEngine.CreateSurfaceBrush(LiquidGlassVariant.Clear, isDark: true, opacityScale: 0.45)
			: new SolidColorBrush(Color.FromArgb(220, 241, 245, 249));
	}

	private static Brush GetStereoMeterTrackBrush(bool isDark)
	{
		return new SolidColorBrush(isDark ? Color.FromArgb(70, 255, 255, 255) : Color.FromArgb(255, 203, 213, 225));
	}

	private static Brush GetStereoDividerBrush(bool isDark)
	{
		return new SolidColorBrush(isDark ? Color.FromArgb(50, 255, 255, 255) : Color.FromArgb(120, 203, 213, 225));
	}

	private static void ApplyStereoButtonChrome(
		Button? btn,
		Brush normalBg,
		Brush hoverBg,
		Brush pressedBg,
		Brush normalBorder,
		Brush hoverBorder,
		Brush foregroundBrush)
	{
		if (btn == null) return;
		btn.Background = normalBg;
		btn.BorderBrush = normalBorder;
		btn.BorderThickness = new Thickness(1);
		btn.Foreground = foregroundBrush;

		btn.Resources["ButtonBackground"] = normalBg;
		btn.Resources["ButtonBackgroundPointerOver"] = hoverBg;
		btn.Resources["ButtonBackgroundPressed"] = pressedBg;
		btn.Resources["ButtonBorderBrush"] = normalBorder;
		btn.Resources["ButtonBorderBrushPointerOver"] = hoverBorder;
		btn.Resources["ButtonBorderBrushPressed"] = hoverBorder;
		btn.Resources["ButtonForeground"] = foregroundBrush;
		btn.Resources["ButtonForegroundPointerOver"] = foregroundBrush;
		btn.Resources["ButtonForegroundPressed"] = foregroundBrush;
	}

	private static void ApplyStereoNeutralButtonSurface(Button? btn, bool isDark, Brush foregroundBrush)
	{
		if (btn == null) return;
		Brush normalBg = CreateStereoSurfaceBrush(LiquidGlassVariant.Clear, isDark, 0.92);
		Brush hoverBg = CreateStereoSurfaceBrush(LiquidGlassVariant.Regular, isDark, isDark ? 0.95 : 1.0);
		Brush pressedBg = CreateStereoSurfaceBrush(LiquidGlassVariant.Clear, isDark, isDark ? 0.72 : 0.80);
		Brush normalBorder = CreateStereoRimBrush(isDark);
		Brush hoverBorder = CreateStereoRimBrush(isDark, isHovered: true);
		ApplyStereoButtonChrome(btn, normalBg, hoverBg, pressedBg, normalBorder, hoverBorder, foregroundBrush);
	}

	private static void ApplyStereoIndigoAccentButton(Button? btn, bool isDark, Brush foregroundBrush)
	{
		if (btn == null) return;
		Brush normalBg = new SolidColorBrush(Color.FromArgb(255, 79, 70, 229));
		Brush hoverBg = new SolidColorBrush(Color.FromArgb(255, 99, 102, 241));
		Brush pressedBg = new SolidColorBrush(Color.FromArgb(255, 67, 56, 202));
		Brush normalBorder = new SolidColorBrush(isDark ? Color.FromArgb(255, 129, 140, 248) : Color.FromArgb(255, 99, 102, 241));
		Brush hoverBorder = new SolidColorBrush(Color.FromArgb(255, 165, 180, 252));
		ApplyStereoButtonChrome(btn, normalBg, hoverBg, pressedBg, normalBorder, hoverBorder, foregroundBrush);
	}

	private void ApplyStereoPendingBadgeTheme(bool isDark)
	{
		if (_sfPendingBadge != null)
		{
			_sfPendingBadge.Background = new SolidColorBrush(isDark ? Color.FromArgb(75, 245, 158, 11) : Color.FromArgb(255, 254, 243, 199));
			_sfPendingBadge.BorderBrush = new SolidColorBrush(isDark ? Color.FromArgb(190, 251, 191, 36) : Color.FromArgb(255, 245, 158, 11));
		}
		if (_sfPendingBadgeText != null)
		{
			_sfPendingBadgeText.Foreground = new SolidColorBrush(isDark ? Color.FromArgb(255, 253, 230, 138) : Color.FromArgb(255, 180, 83, 9));
		}
	}

	public void RefreshStereoFieldTheme(bool isDark)
	{
		if (_stereoFieldWorkbench == null) return;

		_stereoFieldWorkbench.RequestedTheme = isDark ? ElementTheme.Dark : ElementTheme.Light;

		Brush primaryText = new SolidColorBrush(isDark ? Colors.White : Color.FromArgb(255, 15, 23, 42));
		Brush secondaryText = new SolidColorBrush(isDark ? Color.FromArgb(255, 248, 250, 252) : Color.FromArgb(255, 51, 65, 85));
		Brush subHeaderText = new SolidColorBrush(isDark ? Color.FromArgb(255, 226, 232, 240) : Color.FromArgb(255, 71, 85, 105));
		Brush mutedText = new SolidColorBrush(isDark ? Color.FromArgb(255, 203, 213, 225) : Color.FromArgb(255, 100, 116, 139));
		Brush accentSoftBg = new SolidColorBrush(isDark ? Color.FromArgb(85, 99, 102, 241) : Color.FromArgb(255, 238, 242, 255));
		Brush accentSoftBorder = new SolidColorBrush(isDark ? Color.FromArgb(180, 129, 140, 248) : Color.FromArgb(255, 199, 210, 254));
		Brush accentSoftText = new SolidColorBrush(isDark ? Color.FromArgb(255, 199, 210, 254) : Color.FromArgb(255, 79, 70, 229));

		Brush cardBg = CreateStereoSurfaceBrush(LiquidGlassVariant.Regular, isDark, Math.Clamp(0.88 / 0.90, 0.72, 1.05));
		Brush rimBrush = CreateStereoRimBrush(isDark);
		Brush softBg = GetStereoSoftSurfaceBrush(isDark);

		if (_sfRootBorder != null)
		{
			_sfRootBorder.Background = CreateStereoSurfaceBrush(LiquidGlassVariant.Clear, isDark, Math.Clamp(0.70 / 0.85, 0.55, 1.0));
		}

		if (_sfTitleIconBorder != null) _sfTitleIconBorder.Background = accentSoftBg;
		if (_sfTitleIcon != null) _sfTitleIcon.Foreground = accentSoftText;

		if (_sfRadarDragTag != null)
		{
			_sfRadarDragTag.Background = accentSoftBg;
			_sfRadarDragTag.BorderBrush = accentSoftBorder;
		}
		if (_sfRadarDragIcon != null) _sfRadarDragIcon.Foreground = accentSoftText;
		if (_sfRadarDragText != null) _sfRadarDragText.Foreground = accentSoftText;

		if (_sfPresetsIcon != null) _sfPresetsIcon.Foreground = accentSoftText;
		if (_sfControlsIcon != null) _sfControlsIcon.Foreground = accentSoftText;
		if (_sfTipIcon != null) _sfTipIcon.Foreground = accentSoftText;
		if (_sfControlsDivider != null) _sfControlsDivider.Background = GetStereoDividerBrush(isDark);
		if (_sfRadarCorrTrackBorder != null) _sfRadarCorrTrackBorder.Background = GetStereoMeterTrackBrush(isDark);

		foreach (var card in _sfCardBorders)
		{
			card.Background = cardBg;
			card.BorderBrush = rimBrush;
		}
		foreach (var box in _sfSoftBorders)
		{
			box.Background = softBg;
			box.BorderBrush = rimBrush;
		}

		foreach (var tb in _sfPrimaryTextBlocks) tb.Foreground = primaryText;
		foreach (var tb in _sfSecondaryTextBlocks) tb.Foreground = secondaryText;
		foreach (var tb in _sfSubHeaderTextBlocks) tb.Foreground = subHeaderText;
		foreach (var tb in _sfMutedTextBlocks) tb.Foreground = mutedText;
		foreach (var tb in _sfAccentTextBlocks) tb.Foreground = accentSoftText;

		foreach (var (icon, bigTb, lightAccent, darkAccent) in _sfBottomStatAccents)
		{
			var brush = new SolidColorBrush(isDark ? darkAccent : lightAccent);
			icon.Foreground = brush;
			bigTb.Foreground = brush;
		}

		if (_sfResetBtn != null)
		{
			ApplyStereoNeutralButtonSurface(_sfResetBtn, isDark, secondaryText);
			if (_sfResetIcon != null) _sfResetIcon.Foreground = subHeaderText;
			if (_sfResetText != null) _sfResetText.Foreground = secondaryText;
		}

		ApplyStereoPendingBadgeTheme(isDark);
		UpdateStereoPowerButtonVisuals(isDark);
		UpdateStereoDirtyStateBadge(isDark);
		UpdateStereoPresetsSelection(isDark);
		UpdateStereoRadarVisuals(isDark);
	}

	private void UpdateStereoFieldVisuals()
	{
		bool isDark = IsDspDark;
		UpdateStereoPowerButtonVisuals(isDark);
		UpdateStereoHeaderReadouts();
		UpdateStereoRadarVisuals(isDark);
		UpdateStereoPresetsSelection(isDark);
		UpdateStereoBottomStats();
		UpdateStereoDirtyStateBadge(isDark);
	}

	private void UpdateStereoPowerButtonVisuals(bool? isDarkOverride = null)
	{
		if (_sfPowerBtn == null || _sfPowerText == null || _sfPowerIcon == null) return;

		bool isDark = isDarkOverride ?? IsDspDark;

		if (_stereoFieldEnabled)
		{
			Brush whiteBrush = new SolidColorBrush(Colors.White);
			Brush normalBg = new SolidColorBrush(isDark ? Color.FromArgb(255, 5, 150, 105) : Color.FromArgb(255, 16, 185, 129));
			Brush hoverBg = new SolidColorBrush(isDark ? Color.FromArgb(255, 16, 185, 129) : Color.FromArgb(255, 5, 150, 105));
			Brush pressedBg = new SolidColorBrush(Color.FromArgb(255, 4, 120, 87));
			Brush normalBorder = new SolidColorBrush(isDark ? Color.FromArgb(255, 52, 211, 153) : Color.FromArgb(255, 5, 150, 105));
			Brush hoverBorder = new SolidColorBrush(Color.FromArgb(255, 110, 231, 183));
			ApplyStereoButtonChrome(_sfPowerBtn, normalBg, hoverBg, pressedBg, normalBorder, hoverBorder, whiteBrush);

			_sfPowerIcon.Glyph = "\uE7E8";
			_sfPowerIcon.Foreground = whiteBrush;
			_sfPowerText.Text = "立体声场已开启";
			_sfPowerText.Foreground = whiteBrush;
		}
		else
		{
			Brush subHeaderBrush = new SolidColorBrush(isDark ? Color.FromArgb(255, 226, 232, 240) : Color.FromArgb(255, 71, 85, 105));
			Brush mutedBrush = new SolidColorBrush(isDark ? Color.FromArgb(255, 203, 213, 225) : Color.FromArgb(255, 100, 116, 139));
			ApplyStereoNeutralButtonSurface(_sfPowerBtn, isDark, subHeaderBrush);

			_sfPowerIcon.Glyph = "\uE7E8";
			_sfPowerIcon.Foreground = mutedBrush;
			_sfPowerText.Text = "立体声场已旁路";
			_sfPowerText.Foreground = subHeaderBrush;
		}
	}

	private void UpdateStereoHeaderReadouts()
	{
		string widthPercentStr = $"{Math.Round(_stereoFieldWidth * 100.0)}%";
		string widthDescStr = GetStereoWidthPresetLabel(_stereoFieldWidth, _stereoFieldMidGainDb, _stereoFieldSideGainDb);

		if (_sfHeaderWidthBigTb != null) _sfHeaderWidthBigTb.Text = widthPercentStr;
		if (_sfHeaderWidthSubTb != null) _sfHeaderWidthSubTb.Text = widthDescStr;
	}

	private void UpdateStereoRadarVisuals(bool? isDarkOverride = null)
	{
		if (_radarCanvas == null || _radarActiveEllipse == null || _radarWidthHandle == null) return;
		bool isDark = isDarkOverride ?? IsDspDark;

		// 1. 计算当前声像尺寸
		double midLevel = Math.Pow(10.0, _stereoFieldMidGainDb / 20.0);
		double sideLevel = _stereoFieldWidth * Math.Pow(10.0, _stereoFieldSideGainDb / 20.0);

		double rx = Math.Clamp(sideLevel * RadarRefRadius, 2.0, RadarMaxRadius);
		double ry = Math.Clamp(midLevel * RadarRefRadius, 2.0, RadarMaxRadius);

		// 更新动态声像椭圆
		Canvas.SetLeft(_radarActiveEllipse, RadarCenter - rx);
		Canvas.SetTop(_radarActiveEllipse, RadarCenter - ry);
		_radarActiveEllipse.Width = rx * 2.0;
		_radarActiveEllipse.Height = ry * 2.0;

		// 更新拖动手柄
		Canvas.SetLeft(_radarWidthHandle, RadarCenter + rx - 7.0);
		Canvas.SetTop(_radarWidthHandle, RadarCenter - 7.0);

		// 2. 更新幽灵轮廓 (Ghost Ellipse)
		bool isDirty = IsStereoFieldDirty();
		if (_radarGhostEllipse != null)
		{
			if (isDirty)
			{
				double appliedMid = Math.Pow(10.0, _appliedStereoFieldMidGainDb / 20.0);
				double appliedSide = _appliedStereoFieldWidth * Math.Pow(10.0, _appliedStereoFieldSideGainDb / 20.0);
				double ghostRx = Math.Clamp(appliedSide * RadarRefRadius, 2.0, RadarMaxRadius);
				double ghostRy = Math.Clamp(appliedMid * RadarRefRadius, 2.0, RadarMaxRadius);

				Canvas.SetLeft(_radarGhostEllipse, RadarCenter - ghostRx);
				Canvas.SetTop(_radarGhostEllipse, RadarCenter - ghostRy);
				_radarGhostEllipse.Width = ghostRx * 2.0;
				_radarGhostEllipse.Height = ghostRy * 2.0;
				_radarGhostEllipse.Visibility = Visibility.Visible;
			}
			else
			{
				_radarGhostEllipse.Visibility = Visibility.Collapsed;
			}
		}

		// 3. 更新声像仪底部读数与相位相关度
		if (_radarStatMidTb != null)
		{
			_radarStatMidTb.Text = $"中置能量: M × {midLevel:0.00} ({_stereoFieldMidGainDb:+0.0;-0.0;0.0} dB)";
		}
		if (_radarStatSideTb != null)
		{
			string sideDbStr = sideLevel <= 0.0001 ? "−∞ dB" : $"{_stereoFieldSideGainDb:+0.0;-0.0;0.0} dB";
			_radarStatSideTb.Text = $"侧向能量: S × {sideLevel:0.00} ({sideDbStr})";
		}

		// 相位相关度: r = (M^2 - S^2) / (M^2 + S^2)
		double t = midLevel * midLevel + sideLevel * sideLevel;
		double corr = t < 1e-9 ? 1.0 : (midLevel * midLevel - sideLevel * sideLevel) / t;
		corr = Math.Clamp(corr, -1.0, 1.0);

		if (_radarCorrelationTb != null)
		{
			string corrDesc = corr switch
			{
				> 0.85 => "纯同相 · 聚焦中置",
				> 0.40 => "同相 · 原声立体声",
				> 0.05 => "开阔 · 空间漫射",
				> -0.15 => "边缘 · 宽体临界",
				_ => "反相风险 · 宽体抵消"
			};
			Color corrColor = corr switch
			{
				> 0.30 => isDark ? Color.FromArgb(255, 52, 211, 153) : Color.FromArgb(255, 16, 185, 129), // Emerald
				> 0.0 => isDark ? Color.FromArgb(255, 56, 189, 248) : Color.FromArgb(255, 2, 132, 199),   // Sky
				_ => isDark ? Color.FromArgb(255, 251, 191, 36) : Color.FromArgb(255, 217, 119, 6)       // Amber
			};
			_radarCorrelationTb.Text = $"{corr:+0.00;-0.00;0.00} ({corrDesc})";
			_radarCorrelationTb.Foreground = new SolidColorBrush(corrColor);

			if (_radarCorrIndicatorFill != null)
			{
				_radarCorrIndicatorFill.Background = new SolidColorBrush(corrColor);
				// 将 -1.0..+1.0 映射到水平偏移 (-40..+40)
				double offset = corr * 40.0;
				_radarCorrIndicatorFill.Margin = new Thickness(offset, 0, -offset, 0);
			}
		}
	}

	private void UpdateStereoPresetsSelection(bool? isDarkOverride = null)
	{
		bool isDark = isDarkOverride ?? IsDspDark;
		Brush secondaryBrush = new SolidColorBrush(isDark ? Color.FromArgb(255, 248, 250, 252) : Color.FromArgb(255, 51, 65, 85));
		Brush mutedBrush = new SolidColorBrush(isDark ? Color.FromArgb(255, 203, 213, 225) : Color.FromArgb(255, 100, 116, 139));
		Brush whiteBrush = new SolidColorBrush(Colors.White);
		Brush selectedSubBrush = new SolidColorBrush(Color.FromArgb(235, 224, 231, 255));

		foreach (var btn in _stereoPresetButtons)
		{
			if (btn.Tag is not StereoPreset p) continue;

			bool isMatch = Math.Abs(_stereoFieldWidth - p.Width) < 0.02
				&& Math.Abs(_stereoFieldMidGainDb - p.MidGainDb) < 0.15
				&& Math.Abs(_stereoFieldSideGainDb - p.SideGainDb) < 0.15;

			if (btn.Content is StackPanel sp && sp.Children.Count >= 2)
			{
				if (sp.Children[0] is TextBlock t1)
				{
					t1.Foreground = isMatch ? whiteBrush : secondaryBrush;
				}
				if (sp.Children[1] is TextBlock t2)
				{
					t2.Foreground = isMatch ? selectedSubBrush : mutedBrush;
				}
			}

			if (isMatch)
			{
				ApplyStereoIndigoAccentButton(btn, isDark, whiteBrush);
			}
			else
			{
				ApplyStereoNeutralButtonSurface(btn, isDark, secondaryBrush);
			}
		}
	}

	private void UpdateStereoBottomStats()
	{
		string widthPercent = $"{Math.Round(_stereoFieldWidth * 100.0)}%";
		string widthDesc = GetStereoWidthPresetLabel(_stereoFieldWidth, _stereoFieldMidGainDb, _stereoFieldSideGainDb);

		if (_bottomWidthBigTb != null) _bottomWidthBigTb.Text = widthPercent;
		if (_bottomWidthSubTb != null) _bottomWidthSubTb.Text = widthDesc;

		if (_bottomMidBigTb != null) _bottomMidBigTb.Text = $"{_stereoFieldMidGainDb:+0.0;-0.0;0.0} dB";
		if (_bottomMidSubTb != null)
		{
			_bottomMidSubTb.Text = _stereoFieldMidGainDb switch
			{
				> 1.0 => "人声增强强化",
				< -1.0 => "弱化中心伴奏",
				_ => "中央人声基准"
			};
		}

		if (_bottomSideBigTb != null) _bottomSideBigTb.Text = $"{_stereoFieldSideGainDb:+0.0;-0.0;0.0} dB";
		if (_bottomSideSubTb != null)
		{
			_bottomSideSubTb.Text = _stereoFieldSideGainDb switch
			{
				> 1.0 => "空间氛围扩展",
				< -1.0 => "收敛侧声道混响",
				_ => "自然空间感"
			};
		}
	}

	private bool IsStereoFieldDirty()
	{
		return Math.Abs(_stereoFieldWidth - _appliedStereoFieldWidth) > 0.005
			|| Math.Abs(_stereoFieldMidGainDb - _appliedStereoFieldMidGainDb) > 0.05
			|| Math.Abs(_stereoFieldSideGainDb - _appliedStereoFieldSideGainDb) > 0.05
			|| Math.Abs(_stereoFieldMonoBassHz - _appliedStereoFieldMonoBassHz) > 0.5
			|| _stereoFieldEnabled != _appliedStereoFieldEnabled;
	}

	private void UpdateStereoDirtyStateBadge(bool? isDarkOverride = null)
	{
		bool isDark = isDarkOverride ?? IsDspDark;
		bool isDirty = IsStereoFieldDirty();

		if (_sfPendingBadge != null)
		{
			_sfPendingBadge.Visibility = isDirty ? Visibility.Visible : Visibility.Collapsed;
			ApplyStereoPendingBadgeTheme(isDark);
		}

		if (_sfApplyBtn != null)
		{
			if (isDirty)
			{
				Brush whiteBrush = new SolidColorBrush(Colors.White);
				ApplyStereoIndigoAccentButton(_sfApplyBtn, isDark, whiteBrush);
				if (_sfApplyIcon != null) _sfApplyIcon.Foreground = whiteBrush;
				if (_sfApplyText != null) _sfApplyText.Foreground = whiteBrush;
			}
			else
			{
				Brush secondaryBrush = new SolidColorBrush(isDark ? Color.FromArgb(255, 248, 250, 252) : Color.FromArgb(255, 51, 65, 85));
				Brush subHeaderBrush = new SolidColorBrush(isDark ? Color.FromArgb(255, 226, 232, 240) : Color.FromArgb(255, 71, 85, 105));
				ApplyStereoNeutralButtonSurface(_sfApplyBtn, isDark, secondaryBrush);
				if (_sfApplyIcon != null) _sfApplyIcon.Foreground = subHeaderBrush;
				if (_sfApplyText != null) _sfApplyText.Foreground = secondaryBrush;
			}
		}
	}

	private static string GetStereoWidthPresetLabel(double width, double midDb, double sideDb)
	{
		if (width <= 0.05) return "纯单声道";
		if (width < 0.85) return midDb > 0.5 ? "聚焦中置" : "收拢场宽";
		if (Math.Abs(width - 1.0) <= 0.08 && Math.Abs(midDb) <= 0.2 && Math.Abs(sideDb) <= 0.2) return "原声平衡";
		if (width < 1.45) return "宽阔声场";
		return "超宽环绕";
	}

	private static void AddStereoCanvasText(Canvas canvas, string text, double left, double top, Brush foreground, double fontSize = 11, bool isBold = false)
	{
		var tb = new TextBlock
		{
			Text = text,
			FontSize = fontSize,
			FontWeight = isBold ? FontWeights.Bold : FontWeights.Normal,
			Foreground = foreground
		};
		Canvas.SetLeft(tb, left);
		Canvas.SetTop(tb, top);
		canvas.Children.Add(tb);
	}
	#endregion
}
