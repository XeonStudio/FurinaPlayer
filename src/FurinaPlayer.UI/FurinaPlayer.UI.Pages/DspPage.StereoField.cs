using System;
using System.Collections.Generic;
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
	private Button? _sfPowerBtn;
	private FontIcon? _sfPowerIcon;
	private TextBlock? _sfPowerText;
	private Button? _sfResetBtn;
	private Button? _sfApplyBtn;
	private Border? _sfPendingBadge;
	private TextBlock? _sfHeaderWidthBigTb;
	private TextBlock? _sfHeaderWidthSubTb;

	// 左侧声像仪控件
	private Canvas? _radarCanvas;
	private Ellipse? _radarGhostEllipse;
	private Ellipse? _radarActiveEllipse;
	private Ellipse? _radarWidthHandle;
	private TextBlock? _radarStatMidTb;
	private TextBlock? _radarStatSideTb;
	private TextBlock? _radarCorrelationTb;
	private Border? _radarCorrIndicatorFill;

	// 右侧预设与参数滑块
	private readonly List<Button> _stereoPresetButtons = new();
	private Slider? _sliderWidth;
	private TextBlock? _tbWidthVal;
	private Slider? _sliderMidGain;
	private TextBlock? _tbMidGainVal;
	private Slider? _sliderSideGain;
	private TextBlock? _tbSideGainVal;
	private Slider? _sliderMonoBass;
	private TextBlock? _tbMonoBassVal;

	// 底部大号读数卡片
	private TextBlock? _bottomWidthBigTb;
	private TextBlock? _bottomWidthSubTb;
	private TextBlock? _bottomMidBigTb;
	private TextBlock? _bottomMidSubTb;
	private TextBlock? _bottomSideBigTb;
	private TextBlock? _bottomSideSubTb;

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
		if (_stereoFieldWorkbench != null)
		{
			InitStereoFieldDraftState();
			UpdateStereoFieldVisuals();
			return _stereoFieldWorkbench;
		}

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
			Background = CreateWhiteAcrylicBrush(0.70, 190),
			Child = sv
		};

		_stereoFieldWorkbench = rootBorder;

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
		var iconBorder = new Border
		{
			Width = 40,
			Height = 40,
			CornerRadius = new CornerRadius(20),
			Background = new SolidColorBrush(Color.FromArgb(255, 238, 242, 255)),
			Child = new FontIcon
			{
				Glyph = "\uE895",
				FontSize = 20,
				Foreground = new SolidColorBrush(Color.FromArgb(255, 99, 102, 241)),
				HorizontalAlignment = HorizontalAlignment.Center,
				VerticalAlignment = VerticalAlignment.Center
			}
		};
		titleSp.Children.Add(iconBorder);

		var textSp = new StackPanel { Spacing = 2, VerticalAlignment = VerticalAlignment.Center };
		var catTb = new TextBlock
		{
			Text = "FurinaPlayer Spatial Workbench · Mid / Side 声像重塑",
			FontSize = 11.5,
			FontWeight = FontWeights.Medium,
			Foreground = new SolidColorBrush(Color.FromArgb(255, 99, 102, 241))
		};
		var mainTitleTb = new TextBlock
		{
			Text = "立体声场 (Stereo Field)",
			FontSize = 22,
			FontWeight = FontWeights.Bold,
			Foreground = new SolidColorBrush(Color.FromArgb(255, 15, 23, 42))
		};
		textSp.Children.Add(catTb);
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
			Foreground = new SolidColorBrush(Color.FromArgb(255, 79, 70, 229)),
			HorizontalAlignment = HorizontalAlignment.Right
		};
		_sfHeaderWidthSubTb = new TextBlock
		{
			Text = "原声平衡",
			FontSize = 12,
			FontWeight = FontWeights.Medium,
			Foreground = new SolidColorBrush(Color.FromArgb(255, 100, 116, 139)),
			HorizontalAlignment = HorizontalAlignment.Right
		};
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
		var container = new StackPanel { Spacing = 10 };

		var barGrid = new Grid();
		barGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.0, GridUnitType.Star) });
		barGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

		// 左侧按钮组: [开关] [恢复默认] [应用参数] [未应用指示]
		var leftSp = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 10, VerticalAlignment = VerticalAlignment.Center };

		// 开关按钮
		_sfPowerIcon = new FontIcon { Glyph = "\uE7E8", FontSize = 14, Foreground = new SolidColorBrush(Color.FromArgb(255, 100, 116, 139)) };
		_sfPowerText = new TextBlock { Text = "立体声场已旁路", FontSize = 13, FontWeight = FontWeights.Medium };
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
		var resetSp = new StackPanel
		{
			Orientation = Orientation.Horizontal,
			Spacing = 6,
			Children =
			{
				new FontIcon { Glyph = "\uE777", FontSize = 13, Foreground = new SolidColorBrush(Color.FromArgb(255, 71, 85, 105)) },
				new TextBlock { Text = "恢复默认", FontSize = 13, Foreground = new SolidColorBrush(Color.FromArgb(255, 51, 65, 85)) }
			}
		};
		_sfResetBtn = new Button
		{
			Content = resetSp,
			Height = 36,
			CornerRadius = new CornerRadius(8),
			Padding = new Thickness(12, 0, 12, 0),
			Background = new SolidColorBrush(Color.FromArgb(220, 255, 255, 255)),
			BorderBrush = new SolidColorBrush(Color.FromArgb(160, 203, 213, 225))
		};
		_sfResetBtn.Click += (s, e) => ResetStereoFieldDefaults();
		leftSp.Children.Add(_sfResetBtn);

		// 应用参数按钮
		var applySp = new StackPanel
		{
			Orientation = Orientation.Horizontal,
			Spacing = 6,
			Children =
			{
				new FontIcon { Glyph = "\uE74E", FontSize = 13, Foreground = new SolidColorBrush(Color.FromArgb(255, 71, 85, 105)) },
				new TextBlock { Text = "应用参数", FontSize = 13, FontWeight = FontWeights.Medium, Foreground = new SolidColorBrush(Color.FromArgb(255, 51, 65, 85)) }
			}
		};
		_sfApplyBtn = new Button
		{
			Content = applySp,
			Height = 36,
			CornerRadius = new CornerRadius(8),
			Padding = new Thickness(14, 0, 14, 0),
			Background = new SolidColorBrush(Color.FromArgb(220, 255, 255, 255)),
			BorderBrush = new SolidColorBrush(Color.FromArgb(160, 203, 213, 225))
		};
		_sfApplyBtn.Click += (s, e) => ApplyStereoFieldSettings();
		leftSp.Children.Add(_sfApplyBtn);

		// 未应用状态胶囊 (Dirty Badge)
		_sfPendingBadge = new Border
		{
			Background = new SolidColorBrush(Color.FromArgb(255, 254, 243, 199)),
			BorderBrush = new SolidColorBrush(Color.FromArgb(255, 245, 158, 11)),
			BorderThickness = new Thickness(1),
			CornerRadius = new CornerRadius(6),
			Padding = new Thickness(8, 4, 8, 4),
			VerticalAlignment = VerticalAlignment.Center,
			Visibility = Visibility.Collapsed,
			Child = new TextBlock
			{
				Text = "未应用修改",
				FontSize = 11.5,
				FontWeight = FontWeights.SemiBold,
				Foreground = new SolidColorBrush(Color.FromArgb(255, 180, 83, 9))
			}
		};
		leftSp.Children.Add(_sfPendingBadge);

		Grid.SetColumn(leftSp, 0);
		barGrid.Children.Add(leftSp);

		container.Children.Add(barGrid);

		// 描述说明文字
		var descTb = new TextBlock
		{
			Text = "在 Mid / Side 域调节立体声场宽度、中央人声专注度与侧向空间感。0% 为完全单声道，100% 为原始立体声，200% 为超宽空间。",
			FontSize = 12.5,
			Foreground = new SolidColorBrush(Color.FromArgb(255, 100, 116, 139)),
			TextWrapping = TextWrapping.Wrap
		};
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
		var card = new Border
		{
			Background = CreateAcrylicCardBrush(0.88, 245),
			BorderBrush = new SolidColorBrush(Color.FromArgb(140, 226, 232, 240)),
			BorderThickness = new Thickness(1),
			CornerRadius = new CornerRadius(14),
			Padding = new Thickness(18, 16, 18, 16)
		};

		var stack = new StackPanel { Spacing = 12 };

		// 卡片头部
		var head = new Grid();
		head.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.0, GridUnitType.Star) });
		head.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

		var headLeft = new StackPanel { Spacing = 2 };
		headLeft.Children.Add(new TextBlock
		{
			Text = "M/S 声像仪",
			FontSize = 15,
			FontWeight = FontWeights.SemiBold,
			Foreground = new SolidColorBrush(Color.FromArgb(255, 30, 41, 59))
		});
		headLeft.Children.Add(new TextBlock
		{
			Text = "由宽度与增益推导的能量分布椭圆；虚线圆为 0dB 参考。",
			FontSize = 11,
			Foreground = new SolidColorBrush(Color.FromArgb(255, 100, 116, 139))
		});
		Grid.SetColumn(headLeft, 0);
		head.Children.Add(headLeft);

		var dragTag = new Border
		{
			Background = new SolidColorBrush(Color.FromArgb(255, 238, 242, 255)),
			BorderBrush = new SolidColorBrush(Color.FromArgb(255, 199, 210, 254)),
			BorderThickness = new Thickness(1),
			CornerRadius = new CornerRadius(10),
			Padding = new Thickness(8, 3, 8, 3),
			VerticalAlignment = VerticalAlignment.Center,
			Child = new StackPanel
			{
				Orientation = Orientation.Horizontal,
				Spacing = 4,
				Children =
				{
					new FontIcon { Glyph = "\uE749", FontSize = 10, Foreground = new SolidColorBrush(Color.FromArgb(255, 79, 70, 229)) },
					new TextBlock { Text = "可拖动 · 调整场宽", FontSize = 10.5, FontWeight = FontWeights.Medium, Foreground = new SolidColorBrush(Color.FromArgb(255, 79, 70, 229)) }
				}
			}
		};
		Grid.SetColumn(dragTag, 1);
		head.Children.Add(dragTag);

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
			Background = new SolidColorBrush(Color.FromArgb(180, 241, 245, 249)),
			BorderBrush = new SolidColorBrush(Color.FromArgb(100, 203, 213, 225)),
			BorderThickness = new Thickness(1),
			CornerRadius = new CornerRadius(10),
			Padding = new Thickness(12, 10, 12, 10)
		};

		var footerStack = new StackPanel { Spacing = 6 };

		var energyRow = new Grid();
		energyRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.0, GridUnitType.Star) });
		energyRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.0, GridUnitType.Star) });

		_radarStatMidTb = new TextBlock
		{
			Text = "中置能量: M × 1.00 (0.0 dB)",
			FontSize = 11.5,
			FontWeight = FontWeights.Medium,
			Foreground = new SolidColorBrush(Color.FromArgb(255, 51, 65, 85))
		};
		Grid.SetColumn(_radarStatMidTb, 0);
		energyRow.Children.Add(_radarStatMidTb);

		_radarStatSideTb = new TextBlock
		{
			Text = "侧向能量: S × 1.00 (0.0 dB)",
			FontSize = 11.5,
			FontWeight = FontWeights.Medium,
			Foreground = new SolidColorBrush(Color.FromArgb(255, 51, 65, 85)),
			HorizontalAlignment = HorizontalAlignment.Right
		};
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
			Foreground = new SolidColorBrush(Color.FromArgb(255, 100, 116, 139)),
			VerticalAlignment = VerticalAlignment.Center,
			Margin = new Thickness(0, 0, 8, 0)
		};
		Grid.SetColumn(corrLabel, 0);
		corrRow.Children.Add(corrLabel);

		// 相关度指示槽 (-1.0 到 +1.0)
		var trackBorder = new Border
		{
			Height = 6,
			CornerRadius = new CornerRadius(3),
			Background = new SolidColorBrush(Color.FromArgb(255, 203, 213, 225)),
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
		trackBorder.Child = trackGrid;
		Grid.SetColumn(trackBorder, 1);
		corrRow.Children.Add(trackBorder);

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
		var card = new Border
		{
			Background = CreateAcrylicCardBrush(0.88, 245),
			BorderBrush = new SolidColorBrush(Color.FromArgb(140, 226, 232, 240)),
			BorderThickness = new Thickness(1),
			CornerRadius = new CornerRadius(14),
			Padding = new Thickness(20, 16, 20, 16)
		};

		var stack = new StackPanel { Spacing = 16 };

		// Section A: 快速预设胶囊
		var presetsSection = new StackPanel { Spacing = 10 };
		var presetsHead = new Grid();
		presetsHead.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.0, GridUnitType.Star) });
		presetsHead.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

		var presetsTitleSp = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 6, VerticalAlignment = VerticalAlignment.Center };
		presetsTitleSp.Children.Add(new FontIcon { Glyph = "\uE8B7", FontSize = 13, Foreground = new SolidColorBrush(Color.FromArgb(255, 99, 102, 241)) });
		presetsTitleSp.Children.Add(new TextBlock
		{
			Text = "快速预设 (PRESETS)",
			FontSize = 13,
			FontWeight = FontWeights.Bold,
			Foreground = new SolidColorBrush(Color.FromArgb(255, 30, 41, 59))
		});
		Grid.SetColumn(presetsTitleSp, 0);
		presetsHead.Children.Add(presetsTitleSp);

		var presetsSub = new TextBlock
		{
			Text = "一键套用经典声像比例",
			FontSize = 11,
			Foreground = new SolidColorBrush(Color.FromArgb(255, 148, 163, 184)),
			VerticalAlignment = VerticalAlignment.Center
		};
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
			var btn = CreateStereoPresetCapsuleButton(preset);
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
		stack.Children.Add(new Border
		{
			Height = 1,
			Background = new SolidColorBrush(Color.FromArgb(60, 203, 213, 225)),
			Margin = new Thickness(0, 4, 0, 4)
		});

		// Section B: 参数控制
		var controlsSection = new StackPanel { Spacing = 12 };
		var controlsHead = new Grid();
		controlsHead.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.0, GridUnitType.Star) });
		controlsHead.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

		var controlsTitleSp = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 6, VerticalAlignment = VerticalAlignment.Center };
		controlsTitleSp.Children.Add(new FontIcon { Glyph = "\uE994", FontSize = 13, Foreground = new SolidColorBrush(Color.FromArgb(255, 99, 102, 241)) });
		controlsTitleSp.Children.Add(new TextBlock
		{
			Text = "参数控制 (CONTROLS)",
			FontSize = 13,
			FontWeight = FontWeights.Bold,
			Foreground = new SolidColorBrush(Color.FromArgb(255, 30, 41, 59))
		});
		Grid.SetColumn(controlsTitleSp, 0);
		controlsHead.Children.Add(controlsTitleSp);

		var controlsHint = new TextBlock
		{
			Text = "双击滑块可恢复默认",
			FontSize = 11,
			Foreground = new SolidColorBrush(Color.FromArgb(255, 148, 163, 184)),
			VerticalAlignment = VerticalAlignment.Center
		};
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

	private Button CreateStereoPresetCapsuleButton(StereoPreset preset)
	{
		var btn = new Button
		{
			Height = 44,
			CornerRadius = new CornerRadius(10),
			Padding = new Thickness(10, 4, 10, 4),
			HorizontalAlignment = HorizontalAlignment.Stretch,
			HorizontalContentAlignment = HorizontalAlignment.Center,
			Background = new SolidColorBrush(Color.FromArgb(220, 248, 250, 252)),
			BorderBrush = new SolidColorBrush(Color.FromArgb(160, 203, 213, 225)),
			BorderThickness = new Thickness(1),
			Tag = preset
		};

		var sp = new StackPanel { Spacing = 2, HorizontalAlignment = HorizontalAlignment.Center };
		var titleTb = new TextBlock
		{
			Text = preset.Name,
			FontSize = 12,
			FontWeight = FontWeights.SemiBold,
			Foreground = new SolidColorBrush(Color.FromArgb(255, 51, 65, 85)),
			HorizontalAlignment = HorizontalAlignment.Center
		};
		var subTb = new TextBlock
		{
			Text = preset.Subtitle,
			FontSize = 9.5,
			Foreground = new SolidColorBrush(Color.FromArgb(255, 100, 116, 139)),
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
		var container = new Border
		{
			Background = new SolidColorBrush(Color.FromArgb(120, 248, 250, 252)),
			BorderBrush = new SolidColorBrush(Color.FromArgb(80, 226, 232, 240)),
			BorderThickness = new Thickness(1),
			CornerRadius = new CornerRadius(10),
			Padding = new Thickness(12, 8, 12, 8)
		};

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
			Foreground = new SolidColorBrush(Color.FromArgb(255, 30, 41, 59))
		};
		var tipTb = new TextBlock
		{
			Text = tip,
			FontSize = 10,
			Foreground = new SolidColorBrush(Color.FromArgb(255, 148, 163, 184))
		};
		labelSp.Children.Add(labelTb);
		labelSp.Children.Add(tipTb);
		Grid.SetColumn(labelSp, 0);
		headerGrid.Children.Add(labelSp);

		var valTb = new TextBlock
		{
			Text = formatVal(initialVal),
			FontSize = 13,
			FontWeight = FontWeights.Bold,
			Foreground = new SolidColorBrush(Color.FromArgb(255, 79, 70, 229)),
			VerticalAlignment = VerticalAlignment.Center
		};
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
		Color accentColor,
		out TextBlock outBigTb,
		out TextBlock outSubTb)
	{
		var card = new Border
		{
			Background = CreateAcrylicCardBrush(0.88, 245),
			BorderBrush = new SolidColorBrush(Color.FromArgb(140, 226, 232, 240)),
			BorderThickness = new Thickness(1),
			CornerRadius = new CornerRadius(12),
			Padding = new Thickness(16, 14, 16, 14)
		};

		var stack = new StackPanel { Spacing = 6 };

		var topGrid = new Grid();
		topGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.0, GridUnitType.Star) });
		topGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

		var titleTb = new TextBlock
		{
			Text = title,
			FontSize = 12,
			FontWeight = FontWeights.Medium,
			Foreground = new SolidColorBrush(Color.FromArgb(255, 100, 116, 139))
		};
		Grid.SetColumn(titleTb, 0);
		topGrid.Children.Add(titleTb);

		var icon = new FontIcon
		{
			Glyph = glyph,
			FontSize = 14,
			Foreground = new SolidColorBrush(accentColor)
		};
		Grid.SetColumn(icon, 1);
		topGrid.Children.Add(icon);
		stack.Children.Add(topGrid);

		var bigTb = new TextBlock
		{
			Text = initialBig,
			FontSize = 24,
			FontWeight = FontWeights.Bold,
			Foreground = new SolidColorBrush(accentColor)
		};
		stack.Children.Add(bigTb);

		var subTb = new TextBlock
		{
			Text = initialSub,
			FontSize = 11,
			Foreground = new SolidColorBrush(Color.FromArgb(255, 100, 116, 139))
		};
		stack.Children.Add(subTb);

		card.Child = stack;
		outBigTb = bigTb;
		outSubTb = subTb;
		return card;
	}
	#endregion

	#region 6. Bottom Tip Banner
	private FrameworkElement CreateStereoBottomTipBanner()
	{
		var border = new Border
		{
			Background = new SolidColorBrush(Color.FromArgb(200, 248, 250, 252)),
			BorderBrush = new SolidColorBrush(Color.FromArgb(140, 226, 232, 240)),
			BorderThickness = new Thickness(1),
			CornerRadius = new CornerRadius(10),
			Padding = new Thickness(16, 12, 16, 12)
		};

		var sp = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 10, VerticalAlignment = VerticalAlignment.Center };
		sp.Children.Add(new FontIcon
		{
			Glyph = "\uE946",
			FontSize = 16,
			Foreground = new SolidColorBrush(Color.FromArgb(255, 99, 102, 241)),
			VerticalAlignment = VerticalAlignment.Center
		});

		sp.Children.Add(new TextBlock
		{
			Text = "双击滑块可恢复默认值。高宽度的侧向声场建议配合低频保留，以保持低音下潜扎实。",
			FontSize = 12.5,
			Foreground = new SolidColorBrush(Color.FromArgb(255, 71, 85, 105)),
			VerticalAlignment = VerticalAlignment.Center
		});

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
	private void UpdateStereoFieldVisuals()
	{
		UpdateStereoPowerButtonVisuals();
		UpdateStereoHeaderReadouts();
		UpdateStereoRadarVisuals();
		UpdateStereoPresetsSelection();
		UpdateStereoBottomStats();
		UpdateStereoDirtyStateBadge();
	}

	private void UpdateStereoPowerButtonVisuals()
	{
		if (_sfPowerBtn == null || _sfPowerText == null || _sfPowerIcon == null) return;

		if (_stereoFieldEnabled)
		{
			_sfPowerBtn.Background = new SolidColorBrush(Color.FromArgb(255, 16, 185, 129)); // Emerald 500
			_sfPowerBtn.BorderBrush = new SolidColorBrush(Color.FromArgb(255, 5, 150, 105));
			_sfPowerIcon.Glyph = "\uE7E8";
			_sfPowerIcon.Foreground = new SolidColorBrush(Colors.White);
			_sfPowerText.Text = "立体声场已开启";
			_sfPowerText.Foreground = new SolidColorBrush(Colors.White);
		}
		else
		{
			_sfPowerBtn.Background = new SolidColorBrush(Color.FromArgb(220, 241, 245, 249));
			_sfPowerBtn.BorderBrush = new SolidColorBrush(Color.FromArgb(160, 203, 213, 225));
			_sfPowerIcon.Glyph = "\uE7E8";
			_sfPowerIcon.Foreground = new SolidColorBrush(Color.FromArgb(255, 100, 116, 139));
			_sfPowerText.Text = "立体声场已旁路";
			_sfPowerText.Foreground = new SolidColorBrush(Color.FromArgb(255, 71, 85, 105));
		}
	}

	private void UpdateStereoHeaderReadouts()
	{
		string widthPercentStr = $"{Math.Round(_stereoFieldWidth * 100.0)}%";
		string widthDescStr = GetStereoWidthPresetLabel(_stereoFieldWidth, _stereoFieldMidGainDb, _stereoFieldSideGainDb);

		if (_sfHeaderWidthBigTb != null) _sfHeaderWidthBigTb.Text = widthPercentStr;
		if (_sfHeaderWidthSubTb != null) _sfHeaderWidthSubTb.Text = widthDescStr;
	}

	private void UpdateStereoRadarVisuals()
	{
		if (_radarCanvas == null || _radarActiveEllipse == null || _radarWidthHandle == null) return;

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
				> 0.30 => Color.FromArgb(255, 16, 185, 129), // Emerald
				> 0.0 => Color.FromArgb(255, 14, 165, 233),  // Sky
				_ => Color.FromArgb(255, 245, 158, 11)       // Amber
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

	private void UpdateStereoPresetsSelection()
	{
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
					t1.Foreground = isMatch
						? new SolidColorBrush(Colors.White)
						: new SolidColorBrush(Color.FromArgb(255, 51, 65, 85));
				}
				if (sp.Children[1] is TextBlock t2)
				{
					t2.Foreground = isMatch
						? new SolidColorBrush(Color.FromArgb(220, 238, 242, 255))
						: new SolidColorBrush(Color.FromArgb(255, 100, 116, 139));
				}
			}

			if (isMatch)
			{
				btn.Background = new SolidColorBrush(Color.FromArgb(255, 79, 70, 229)); // Indigo
				btn.BorderBrush = new SolidColorBrush(Color.FromArgb(255, 99, 102, 241));
			}
			else
			{
				btn.Background = new SolidColorBrush(Color.FromArgb(220, 248, 250, 252));
				btn.BorderBrush = new SolidColorBrush(Color.FromArgb(160, 203, 213, 225));
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

	private void UpdateStereoDirtyStateBadge()
	{
		bool isDirty = IsStereoFieldDirty();

		if (_sfPendingBadge != null)
		{
			_sfPendingBadge.Visibility = isDirty ? Visibility.Visible : Visibility.Collapsed;
		}

		if (_sfApplyBtn != null)
		{
			_sfApplyBtn.Background = isDirty
				? new SolidColorBrush(Color.FromArgb(255, 79, 70, 229))
				: new SolidColorBrush(Color.FromArgb(220, 255, 255, 255));
			_sfApplyBtn.BorderBrush = isDirty
				? new SolidColorBrush(Color.FromArgb(255, 99, 102, 241))
				: new SolidColorBrush(Color.FromArgb(160, 203, 213, 225));

			if (_sfApplyBtn.Content is StackPanel sp && sp.Children.Count >= 2)
			{
				if (sp.Children[0] is FontIcon icon)
				{
					icon.Foreground = isDirty
						? new SolidColorBrush(Colors.White)
						: new SolidColorBrush(Color.FromArgb(255, 71, 85, 105));
				}
				if (sp.Children[1] is TextBlock tb)
				{
					tb.Foreground = isDirty
						? new SolidColorBrush(Colors.White)
						: new SolidColorBrush(Color.FromArgb(255, 51, 65, 85));
				}
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
