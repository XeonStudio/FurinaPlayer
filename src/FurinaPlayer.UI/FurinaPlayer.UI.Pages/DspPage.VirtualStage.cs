using System;
using System.Collections.Generic;
using System.Diagnostics;
using FurinaPlayer.UI.Controls;
using Microsoft.UI;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using SonicWave.Audio.Dsp;
using Windows.Foundation;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.UI;
using WinRT.Interop;

namespace FurinaPlayer.UI.Pages;

/// <summary>
/// FurinaPlayer 虚拟声场 (Virtual Soundstage) 二级 DSP 功能工作台分部实现。
/// 1:1 像素级复刻两耳时间差、头部阴影衰减、短反射空间感与 HRTF 声场扩展。
/// </summary>
public sealed partial class DspPage
{
	private Border? _virtualStageWorkbench;

	// --- 1. Core Parameter States ---
	private bool _virtualStageEnabled = false;
	private double _virtualStageStrength = 0.45; // 0.0 ~ 1.0 (45%)
	private double _virtualStageAzimuthDegrees = 30.0; // 15° ~ 75° (±30°)
	private double _virtualStageReflection = 1.0; // 0.0 ~ 1.0 (100%)
	private bool _virtualStageHrtfEnabled = false;
	private string _virtualStageHrtfProfile = "SADIE II · 标准";

	// --- 2. Top Header & Toolbar Controls ---
	private Button? _vsPowerBtn;
	private FontIcon? _vsPowerIcon;
	private TextBlock? _vsPowerText;
	private Button? _vsResetBtn;
	private TextBlock? _vsTopAngleBadgeTb;

	// --- 3. Diagram Controls ---
	private Canvas? _vsCanvas;
	private Line? _vsCenterRefLine;
	private Line? _vsLeftRayLine;
	private Line? _vsRightRayLine;
	private Border? _vsLeftSpeakerNode;
	private Border? _vsRightSpeakerNode;
	private TextBlock? _vsLeftSpeakerLabel;
	private TextBlock? _vsRightSpeakerLabel;
	private Ellipse? _vsListenerDot;
	private TextBlock? _vsDiagramAngleTb;
	private TextBlock? _vsStatusDescTb;
	private bool _isDraggingDiagram = false;

	// --- 4. Bottom Triple Stats Readout Controls ---
	private TextBlock? _vsStatAngleTb;
	private TextBlock? _vsStatReflectionTb;
	private TextBlock? _vsStatCenterTb;

	// --- 5. Right Console Controls ---
	// HRTF Group
	private CheckBox? _vsHrtfCheckBox;
	private ComboBox? _vsHrtfCombo;
	private Button? _vsAuditionMatchBtn;
	private FrameworkElement? _vsAuditionPanel;
	private readonly List<Button> _vsAuditionButtons = new();

	// Quick Presets
	private Button? _vsPresetLightBtn;
	private Button? _vsPresetNaturalBtn;
	private Button? _vsPresetStrongBtn;

	// Sliders & Readouts
	private Slider? _vsStrengthSlider;
	private TextBlock? _vsStrengthValTb;
	private Slider? _vsAzimuthSlider;
	private TextBlock? _vsAzimuthValTb;
	private Slider? _vsReflectionSlider;
	private TextBlock? _vsReflectionValTb;

	// Diagram Layout Constants
	private const double VsDiagramWidth = 560.0;
	private const double VsDiagramHeight = 250.0;
	private const double VsListenerCx = 280.0;
	private const double VsListenerCy = 195.0;
	private const double VsStageRadius = 150.0;

	/// <summary>
	/// 创建虚拟声场二级功能工作台界面。
	/// </summary>
	public FrameworkElement CreateVirtualStageWorkbench()
	{
		InitVirtualStageState();

		var rootBorder = new Border
		{
			Background = CreateWhiteAcrylicBrush(0.72, 195),
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

		// 1. 顶部标题栏 (标题、模块 Pill、就绪状态、刷新按钮)
		mainStack.Children.Add(CreateVsTopHeaderBar());

		// 2. 顶部操作工具栏 (电源旁路切换、恢复默认、右侧角度读数)
		mainStack.Children.Add(CreateVsActionToolbar());

		// 3. 描述说明文本
		mainStack.Children.Add(CreateVsDescriptionSection());

		// 4. 主双列工作区 (左侧：声场雷达图与底部三联读数；右侧：HRTF、预设、滑块控制台)
		mainStack.Children.Add(CreateVsMainStage());

		sv.Content = mainStack;
		rootBorder.Child = sv;
		_virtualStageWorkbench = rootBorder;

		// 同步并渲染初始状态
		UpdateVsAllVisuals();

		return _virtualStageWorkbench;
	}

	/// <summary>
	/// 初始化虚拟声场参数状态（优先从底层的 DspChain 读取已保存的设置）。
	/// </summary>
	private void InitVirtualStageState()
	{
		try
		{
			var spatial = ViewModel?.AudioEngine?.DspChain?.Spatial;
			if (spatial != null)
			{
				_virtualStageEnabled = spatial.VirtualStageEnabled;
				_virtualStageStrength = spatial.VirtualStageStrength;
				_virtualStageAzimuthDegrees = spatial.VirtualStageAzimuth;
				_virtualStageReflection = spatial.VirtualStageReflection;
				_virtualStageHrtfEnabled = spatial.VirtualStageHrtf;
				if (!string.IsNullOrEmpty(spatial.VirtualStageHrtfPreset))
				{
					_virtualStageHrtfProfile = spatial.VirtualStageHrtfPreset;
				}
			}
		}
		catch (Exception ex)
		{
			Debug.WriteLine($"[FurinaPlayer] Init Virtual Stage state error: {ex.Message}");
		}
	}

	// ==========================================
	// 顶部与工具栏构建
	// ==========================================

	private FrameworkElement CreateVsTopHeaderBar()
	{
		var headerGrid = new Grid();
		headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.0, GridUnitType.Star) });
		headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

		// 左侧：Icon + 标题
		var titleSp = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 12, VerticalAlignment = VerticalAlignment.Center };
		var iconBorder = new Border
		{
			Width = 36,
			Height = 36,
			CornerRadius = new CornerRadius(18),
			Background = DspAccentSoftBgBrush,
			Child = new FontIcon
			{
				Glyph = "\uE995",
				FontSize = 18,
				Foreground = new SolidColorBrush(Color.FromArgb(255, 99, 102, 241)),
				HorizontalAlignment = HorizontalAlignment.Center,
				VerticalAlignment = VerticalAlignment.Center
			}
		};
		titleSp.Children.Add(iconBorder);
		titleSp.Children.Add(new TextBlock
		{
			Text = "虚拟声场",
			FontSize = 22,
			FontWeight = FontWeights.Bold,
			Foreground = DspTextPrimaryBrush,
			VerticalAlignment = VerticalAlignment.Center
		});
		Grid.SetColumn(titleSp, 0);
		headerGrid.Children.Add(titleSp);

		return headerGrid;
	}

	private FrameworkElement CreateVsActionToolbar()
	{
		var barGrid = new Grid();
		barGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.0, GridUnitType.Star) });
		barGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

		var leftSp = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 10, VerticalAlignment = VerticalAlignment.Center };

		// 开关按钮：【已旁路 · 开始试听】/【已开启】
		_vsPowerBtn = new Button
		{
			CornerRadius = new CornerRadius(6),
			Padding = new Thickness(14, 6, 16, 6)
		};
		var powerSp = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
		_vsPowerIcon = new FontIcon { Glyph = "\uE7E8", FontSize = 14 };
		_vsPowerText = new TextBlock { Text = "已旁路 · 开始试听", FontSize = 13, FontWeight = FontWeights.Medium };
		powerSp.Children.Add(_vsPowerIcon);
		powerSp.Children.Add(_vsPowerText);
		_vsPowerBtn.Content = powerSp;
		_vsPowerBtn.Click += (s, e) =>
		{
			_virtualStageEnabled = !_virtualStageEnabled;
			UpdateVsAllVisuals();
			ApplyVirtualStageParameters();
		};
		leftSp.Children.Add(_vsPowerBtn);

		// 【恢复默认】按钮
		_vsResetBtn = new Button
		{
			CornerRadius = new CornerRadius(6),
			Padding = new Thickness(12, 6, 14, 6),
			Content = new StackPanel
			{
				Orientation = Orientation.Horizontal,
				Spacing = 6,
				Children =
				{
					new FontIcon { Glyph = "\uE72C", FontSize = 13, Foreground = DspTextSubHeaderBrush },
					new TextBlock { Text = "恢复默认", FontSize = 13, Foreground = DspTextSubHeaderBrush }
				}
			}
		};
		_vsResetBtn.Click += (s, e) => ResetVirtualStageToDefaults();
		leftSp.Children.Add(_vsResetBtn);

		Grid.SetColumn(leftSp, 0);
		barGrid.Children.Add(leftSp);

		// 右侧显示角度读数：“±30°”
		_vsTopAngleBadgeTb = new TextBlock
		{
			Text = "±30°",
			FontSize = 13,
			FontWeight = FontWeights.SemiBold,
			Foreground = DspTextMutedBrush,
			VerticalAlignment = VerticalAlignment.Center
		};
		Grid.SetColumn(_vsTopAngleBadgeTb, 1);
		barGrid.Children.Add(_vsTopAngleBadgeTb);

		return barGrid;
	}

	private FrameworkElement CreateVsDescriptionSection()
	{
		return new TextBlock
		{
			Text = "用两耳时间差、频率衰减和短反射模拟音箱空间感，保留中置直达声的位置；适合耳机，开启后会关闭交叉馈送；听感因耳机和个人而异。",
			FontSize = 13,
			Foreground = DspTextMutedBrush,
			TextWrapping = TextWrapping.Wrap,
			Margin = new Thickness(2, -4, 0, 4)
		};
	}

	// ==========================================
	// 主双列工作区构建
	// ==========================================

	private FrameworkElement CreateVsMainStage()
	{
		var stageGrid = new Grid { ColumnSpacing = 20 };
		stageGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.18, GridUnitType.Star) }); // 左侧监控与读数
		stageGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.0, GridUnitType.Star) });  // 右侧控制台

		// 左侧
		var leftCard = CreateVsLeftMonitorCard();
		Grid.SetColumn(leftCard, 0);
		stageGrid.Children.Add(leftCard);

		// 右侧
		var rightCard = CreateVsRightControlsCard();
		Grid.SetColumn(rightCard, 1);
		stageGrid.Children.Add(rightCard);

		return stageGrid;
	}

	// ==========================================
	// 左侧：声场雷达图、状态与底部三联读数
	// ==========================================

	private FrameworkElement CreateVsLeftMonitorCard()
	{
		var cardBorder = new Border
		{
			Background = CreateAcrylicCardBrush(0.85, 238),
			BorderBrush = CreateLiquidGlassRimBrush(),
			BorderThickness = new Thickness(1),
			CornerRadius = new CornerRadius(12),
			Padding = new Thickness(22, 18, 22, 20)
		};

		var stack = new StackPanel { Spacing = 14 };

		// 1. 声场示意图容器 (Viewbox 保持完美等比例缩放与清晰度)
		var viewbox = new Viewbox
		{
			Height = VsDiagramHeight,
			Stretch = Stretch.Uniform,
			HorizontalAlignment = HorizontalAlignment.Center
		};

		_vsCanvas = new Canvas
		{
			Width = VsDiagramWidth,
			Height = VsDiagramHeight,
			Background = new SolidColorBrush(Colors.Transparent)
		};

		// 绘制刻度弧形半圆雷达 (从 -75° 到 +75°)
		DrawVsStageRadarElements(_vsCanvas);

		// 支持在雷达图上交互式拖动调整音箱角度
		_vsCanvas.PointerPressed += OnVsCanvasPointerPressed;
		_vsCanvas.PointerMoved += OnVsCanvasPointerMoved;
		_vsCanvas.PointerReleased += OnVsCanvasPointerReleased;
		_vsCanvas.PointerCanceled += OnVsCanvasPointerReleased;

		viewbox.Child = _vsCanvas;
		stack.Children.Add(viewbox);

		// 2. 状态说明文字
		_vsStatusDescTb = new TextBlock
		{
			Text = "已旁路，保留原始立体声，不添加空间处理。",
			FontSize = 12.5,
			Foreground = DspTextMutedBrush,
			Margin = new Thickness(4, 0, 4, 0),
			TextWrapping = TextWrapping.Wrap
		};
		stack.Children.Add(_vsStatusDescTb);

		// 3. 底部三联大号读数卡片 (模拟角度、反射量、中置)
		stack.Children.Add(CreateVsTripleStatsCard());

		// 4. 底部提示
		stack.Children.Add(new TextBlock
		{
			Text = "参数实时同步。点击预设开始试听，使用电源按钮对比原声。",
			FontSize = 12,
			Foreground = DspTextMutedBrush,
			Margin = new Thickness(4, 2, 4, 0)
		});

		cardBorder.Child = stack;
		return cardBorder;
	}

	/// <summary>
	/// 构建弧形半圆雷达背景、音箱节点、中心听音点与连线。
	/// </summary>
	private void DrawVsStageRadarElements(Canvas canvas)
	{
		// 半圆刻度弧 (R = 150, 从 -75° 到 +75°)
		double angleLimitRad = 75.0 * Math.PI / 180.0;
		double startX = VsListenerCx - VsStageRadius * Math.Sin(angleLimitRad);
		double startY = VsListenerCy - VsStageRadius * Math.Cos(angleLimitRad);
		double endX = VsListenerCx + VsStageRadius * Math.Sin(angleLimitRad);
		double endY = VsListenerCy - VsStageRadius * Math.Cos(angleLimitRad);

		var pathFigure = new PathFigure
		{
			StartPoint = new Point(startX, startY),
			IsClosed = false
		};
		pathFigure.Segments.Add(new ArcSegment
		{
			Point = new Point(endX, endY),
			Size = new Size(VsStageRadius, VsStageRadius),
			SweepDirection = SweepDirection.Clockwise,
			IsLargeArc = false
		});
		var pathGeometry = new PathGeometry();
		pathGeometry.Figures.Add(pathFigure);

		var arcPath = new Path
		{
			Data = pathGeometry,
			Stroke = new SolidColorBrush(Color.FromArgb(130, 203, 213, 225)),
			StrokeThickness = 1.6,
			StrokeDashArray = new DoubleCollection { 4, 3 }
		};
		canvas.Children.Add(arcPath);

		// 中置垂直参考虚线
		_vsCenterRefLine = new Line
		{
			X1 = VsListenerCx,
			Y1 = VsListenerCy,
			X2 = VsListenerCx,
			Y2 = VsListenerCy - VsStageRadius,
			Stroke = new SolidColorBrush(Color.FromArgb(90, 203, 213, 225)),
			StrokeThickness = 1.2,
			StrokeDashArray = new DoubleCollection { 2, 2 }
		};
		canvas.Children.Add(_vsCenterRefLine);

		// “中置”标签
		var centerLabel = new TextBlock
		{
			Text = "中置",
			FontSize = 12,
			FontWeight = FontWeights.Medium,
			Foreground = DspTextMutedBrush,
			IsHitTestVisible = false
		};
		Canvas.SetLeft(centerLabel, VsListenerCx - 12.0);
		Canvas.SetTop(centerLabel, VsListenerCy - VsStageRadius - 24.0);
		canvas.Children.Add(centerLabel);

		// 左右扬声器放射线 (汇聚到中心听音位置)
		_vsLeftRayLine = new Line
		{
			StrokeThickness = 2.0,
			Stroke = new SolidColorBrush(Color.FromArgb(160, 148, 163, 184))
		};
		_vsRightRayLine = new Line
		{
			StrokeThickness = 2.0,
			Stroke = new SolidColorBrush(Color.FromArgb(160, 148, 163, 184))
		};
		canvas.Children.Add(_vsLeftRayLine);
		canvas.Children.Add(_vsRightRayLine);

		// 左扬声器节点 (带外圆与同心圆内点)
		_vsLeftSpeakerNode = CreateVsSpeakerNodeBorder();
		_vsLeftSpeakerLabel = new TextBlock
		{
			Text = "左",
			FontSize = 12,
			FontWeight = FontWeights.SemiBold,
			Foreground = DspTextMutedBrush,
			IsHitTestVisible = false
		};
		canvas.Children.Add(_vsLeftSpeakerNode);
		canvas.Children.Add(_vsLeftSpeakerLabel);

		// 右扬声器节点
		_vsRightSpeakerNode = CreateVsSpeakerNodeBorder();
		_vsRightSpeakerLabel = new TextBlock
		{
			Text = "右",
			FontSize = 12,
			FontWeight = FontWeights.SemiBold,
			Foreground = DspTextMutedBrush,
			IsHitTestVisible = false
		};
		canvas.Children.Add(_vsRightSpeakerNode);
		canvas.Children.Add(_vsRightSpeakerLabel);

		// 中心听音点 (Listener Dot)
		_vsListenerDot = new Ellipse
		{
			Width = 10,
			Height = 10,
			Fill = new SolidColorBrush(Color.FromArgb(255, 79, 70, 229)),
			IsHitTestVisible = false
		};
		Canvas.SetLeft(_vsListenerDot, VsListenerCx - 5.0);
		Canvas.SetTop(_vsListenerDot, VsListenerCy - 5.0);
		canvas.Children.Add(_vsListenerDot);

		// 中心听音点下方“模拟 ±30°”读数
		_vsDiagramAngleTb = new TextBlock
		{
			Text = "模拟 ±30°",
			FontSize = 12.5,
			FontWeight = FontWeights.SemiBold,
			Foreground = DspTextSubHeaderBrush,
			IsHitTestVisible = false
		};
		Canvas.SetLeft(_vsDiagramAngleTb, VsListenerCx - 32.0);
		Canvas.SetTop(_vsDiagramAngleTb, VsListenerCy + 18.0);
		canvas.Children.Add(_vsDiagramAngleTb);
	}

	private Border CreateVsSpeakerNodeBorder()
	{
		bool isDark = IsDspDark;
		return new Border
		{
			Width = 28,
			Height = 28,
			CornerRadius = new CornerRadius(14),
			Background = new SolidColorBrush(isDark ? Color.FromArgb(220, 30, 41, 59) : Colors.White),
			BorderThickness = new Thickness(2),
			BorderBrush = new SolidColorBrush(isDark ? Color.FromArgb(255, 148, 163, 184) : Color.FromArgb(255, 148, 163, 184)),
			Child = new Border
			{
				Width = 8,
				Height = 8,
				CornerRadius = new CornerRadius(4),
				Background = new SolidColorBrush(isDark ? Color.FromArgb(255, 203, 213, 225) : Color.FromArgb(255, 148, 163, 184)),
				HorizontalAlignment = HorizontalAlignment.Center,
				VerticalAlignment = VerticalAlignment.Center
			}
		};
	}

	private FrameworkElement CreateVsTripleStatsCard()
	{
		var card = new Border
		{
			Background = DspSoftSurfaceBrush,
			BorderBrush = CreateLiquidGlassRimBrush(),
			BorderThickness = new Thickness(1),
			CornerRadius = new CornerRadius(10),
			Padding = new Thickness(18, 14, 18, 14)
		};

		var grid = new Grid();
		grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.0, GridUnitType.Star) });
		grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.0, GridUnitType.Star) });
		grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.0, GridUnitType.Star) });

		// 列 0：模拟角度
		var sp0 = new StackPanel { Spacing = 3 };
		sp0.Children.Add(new TextBlock { Text = "模拟角度", FontSize = 12, Foreground = DspTextMutedBrush });
		_vsStatAngleTb = new TextBlock
		{
			Text = "±30°",
			FontSize = 24,
			FontWeight = FontWeights.SemiBold,
			Foreground = DspTextPrimaryBrush
		};
		sp0.Children.Add(_vsStatAngleTb);
		Grid.SetColumn(sp0, 0);
		grid.Children.Add(sp0);

		// 列 1：反射量
		var sp1 = new StackPanel { Spacing = 3 };
		sp1.Children.Add(new TextBlock { Text = "反射量", FontSize = 12, Foreground = DspTextMutedBrush });
		_vsStatReflectionTb = new TextBlock
		{
			Text = "100%",
			FontSize = 24,
			FontWeight = FontWeights.SemiBold,
			Foreground = DspTextPrimaryBrush
		};
		sp1.Children.Add(_vsStatReflectionTb);
		Grid.SetColumn(sp1, 1);
		grid.Children.Add(sp1);

		// 列 2：中置
		var sp2 = new StackPanel { Spacing = 3 };
		sp2.Children.Add(new TextBlock { Text = "中置", FontSize = 12, Foreground = DspTextMutedBrush });
		_vsStatCenterTb = new TextBlock
		{
			Text = "直达声居中",
			FontSize = 24,
			FontWeight = FontWeights.SemiBold,
			Foreground = DspTextPrimaryBrush
		};
		sp2.Children.Add(_vsStatCenterTb);
		Grid.SetColumn(sp2, 2);
		grid.Children.Add(sp2);

		card.Child = grid;
		return card;
	}

	// ==========================================
	// 右侧：控制选项控制台 (HRTF、预设、滑块)
	// ==========================================

	private FrameworkElement CreateVsRightControlsCard()
	{
		var panel = new StackPanel { Spacing = 16 };

		// 1. HRTF 声场扩展组
		panel.Children.Add(CreateVsHrtfSection());

		// 2. 快速预设
		panel.Children.Add(CreateVsPresetsSection());

		// 3. 参数控制 (强度、角度、反射量滑块)
		panel.Children.Add(CreateVsSlidersSection());

		return panel;
	}

	private FrameworkElement CreateVsHrtfSection()
	{
		var card = new Border
		{
			Background = CreateAcrylicCardBrush(0.85, 238),
			BorderBrush = CreateLiquidGlassRimBrush(),
			BorderThickness = new Thickness(1),
			CornerRadius = new CornerRadius(12),
			Padding = new Thickness(18, 16, 18, 16)
		};

		var sp = new StackPanel { Spacing = 12 };

		// 首行：[✓] HRTF 声场   [导入 SOFA]
		var topRow = new Grid();
		topRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.0, GridUnitType.Star) });
		topRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

		_vsHrtfCheckBox = new CheckBox
		{
			Content = "HRTF 声场",
			FontSize = 13.5,
			FontWeight = FontWeights.SemiBold,
			IsChecked = _virtualStageHrtfEnabled,
			VerticalAlignment = VerticalAlignment.Center
		};
		_vsHrtfCheckBox.Checked += (s, e) =>
		{
			_virtualStageHrtfEnabled = true;
			UpdateVsStatusText();
			ApplyVirtualStageParameters();
		};
		_vsHrtfCheckBox.Unchecked += (s, e) =>
		{
			_virtualStageHrtfEnabled = false;
			UpdateVsStatusText();
			ApplyVirtualStageParameters();
		};
		Grid.SetColumn(_vsHrtfCheckBox, 0);
		topRow.Children.Add(_vsHrtfCheckBox);

		var btnImportSofa = new HyperlinkButton
		{
			Content = "导入 SOFA",
			FontSize = 12,
			Foreground = DspAccentSoftTextBrush,
			Padding = new Thickness(6, 2, 6, 2),
			VerticalAlignment = VerticalAlignment.Center
		};
		btnImportSofa.Click += (s, e) => TriggerVsImportSofaFilePicker();
		Grid.SetColumn(btnImportSofa, 1);
		topRow.Children.Add(btnImportSofa);

		sp.Children.Add(topRow);

		// 第二行：方案下拉框 + [试听匹配]
		var profileRow = new Grid { ColumnSpacing = 10 };
		profileRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
		profileRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.0, GridUnitType.Star) });
		profileRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

		var lbl = new TextBlock
		{
			Text = "方案",
			FontSize = 13,
			Foreground = DspTextSubHeaderBrush,
			VerticalAlignment = VerticalAlignment.Center
		};
		Grid.SetColumn(lbl, 0);
		profileRow.Children.Add(lbl);

		_vsHrtfCombo = new ComboBox
		{
			HorizontalAlignment = HorizontalAlignment.Stretch,
			CornerRadius = new CornerRadius(6)
		};
		_vsHrtfCombo.Items.Add(new ComboBoxItem { Content = "SADIE II · 标准" });
		_vsHrtfCombo.Items.Add(new ComboBoxItem { Content = "KEMAR · 扩散场" });
		_vsHrtfCombo.Items.Add(new ComboBoxItem { Content = "IRCAM · 监听" });
		_vsHrtfCombo.Items.Add(new ComboBoxItem { Content = "Furina 3D 沉浸" });

		// 选中当前方案
		SelectVsComboProfile(_virtualStageHrtfProfile);
		_vsHrtfCombo.SelectionChanged += (s, e) =>
		{
			if (_vsHrtfCombo.SelectedItem is ComboBoxItem cbi && cbi.Content is string text)
			{
				_virtualStageHrtfProfile = text;
				UpdateVsStatusText();
				ApplyVirtualStageParameters();
			}
		};
		Grid.SetColumn(_vsHrtfCombo, 1);
		profileRow.Children.Add(_vsHrtfCombo);

		_vsAuditionMatchBtn = new Button
		{
			Content = "试听匹配",
			FontSize = 12,
			CornerRadius = new CornerRadius(6),
			Padding = new Thickness(10, 4, 10, 4),
			VerticalAlignment = VerticalAlignment.Center
		};
		_vsAuditionMatchBtn.Click += (s, e) =>
		{
			if (_vsAuditionPanel != null)
			{
				_vsAuditionPanel.Visibility = _vsAuditionPanel.Visibility == Visibility.Visible
					? Visibility.Collapsed
					: Visibility.Visible;
			}
		};
		Grid.SetColumn(_vsAuditionMatchBtn, 2);
		profileRow.Children.Add(_vsAuditionMatchBtn);

		sp.Children.Add(profileRow);

		// 第三行：试听匹配展开条 (点击可快速在音乐播放中即时对比各内置 HRTF 方案)
		_vsAuditionPanel = CreateVsAuditionMatchSubPanel();
		sp.Children.Add(_vsAuditionPanel);

		card.Child = sp;
		return card;
	}

	private FrameworkElement CreateVsAuditionMatchSubPanel()
	{
		var panel = new StackPanel
		{
			Spacing = 8,
			Visibility = Visibility.Collapsed,
			Padding = new Thickness(8, 6, 8, 4),
			Background = DspSoftSurfaceBrush,
			CornerRadius = new CornerRadius(6)
		};

		panel.Children.Add(new TextBlock
		{
			Text = "用当前音乐切换比较，选择声像与音色最自然的一项。",
			FontSize = 11.5,
			Foreground = DspTextMutedBrush
		});

		var buttonsSp = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
		string[] profiles = { "SADIE II · 标准", "KEMAR · 扩散场", "IRCAM · 监听", "Furina 3D 沉浸" };

		_vsAuditionButtons.Clear();
		foreach (var prof in profiles)
		{
			var btn = new Button
			{
				Content = prof,
				FontSize = 11.5,
				CornerRadius = new CornerRadius(4),
				Padding = new Thickness(8, 3, 8, 3)
			};
			btn.Click += (s, e) =>
			{
				_virtualStageHrtfProfile = prof;
				_virtualStageHrtfEnabled = true;
				if (_vsHrtfCheckBox != null) _vsHrtfCheckBox.IsChecked = true;
				SelectVsComboProfile(prof);
				UpdateVsAuditionButtonsStyle();
				UpdateVsStatusText();
				ApplyVirtualStageParameters();
			};
			_vsAuditionButtons.Add(btn);
			buttonsSp.Children.Add(btn);
		}

		UpdateVsAuditionButtonsStyle();
		panel.Children.Add(buttonsSp);
		return panel;
	}

	private void SelectVsComboProfile(string profile)
	{
		if (_vsHrtfCombo == null) return;
		for (int i = 0; i < _vsHrtfCombo.Items.Count; i++)
		{
			if (_vsHrtfCombo.Items[i] is ComboBoxItem item && (item.Content as string) == profile)
			{
				_vsHrtfCombo.SelectedIndex = i;
				return;
			}
		}
	}

	private void UpdateVsAuditionButtonsStyle()
	{
		foreach (var btn in _vsAuditionButtons)
		{
			bool active = (btn.Content as string) == _virtualStageHrtfProfile;
			btn.Background = active
				? new SolidColorBrush(Color.FromArgb(255, 238, 242, 255))
				: new SolidColorBrush(Color.FromArgb(255, 248, 250, 252));
			btn.BorderBrush = active
				? new SolidColorBrush(Color.FromArgb(255, 99, 102, 241))
				: new SolidColorBrush(Color.FromArgb(255, 226, 232, 240));
			btn.Foreground = active
				? new SolidColorBrush(Color.FromArgb(255, 79, 70, 229))
				: DspTextSubHeaderBrush;
		}
	}

	private FrameworkElement CreateVsPresetsSection()
	{
		var card = new Border
		{
			Background = CreateAcrylicCardBrush(0.85, 238),
			BorderBrush = CreateLiquidGlassRimBrush(),
			BorderThickness = new Thickness(1),
			CornerRadius = new CornerRadius(12),
			Padding = new Thickness(18, 14, 18, 14)
		};

		var sp = new StackPanel { Spacing = 10 };
		sp.Children.Add(new TextBlock
		{
			Text = "快速预设",
			FontSize = 12.5,
			FontWeight = FontWeights.SemiBold,
			Foreground = DspTextMutedBrush
		});

		var buttonsSp = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 10 };

		// 【轻度】(强度 25%, 角度 ±20°, 反射 40%)
		_vsPresetLightBtn = new Button
		{
			Content = "轻度",
			FontSize = 13,
			CornerRadius = new CornerRadius(6),
			Padding = new Thickness(18, 6, 18, 6)
		};
		ToolTipService.SetToolTip(_vsPresetLightBtn, "轻度：强度 25% · 角度 ±20° · 反射 40%");
		_vsPresetLightBtn.Click += (s, e) => ApplyVsPreset(0.25, 20.0, 0.40);
		buttonsSp.Children.Add(_vsPresetLightBtn);

		// 【自然】(强度 45%, 角度 ±30°, 反射 100%)
		_vsPresetNaturalBtn = new Button
		{
			Content = "自然",
			FontSize = 13,
			CornerRadius = new CornerRadius(6),
			Padding = new Thickness(18, 6, 18, 6)
		};
		ToolTipService.SetToolTip(_vsPresetNaturalBtn, "自然：强度 45% · 角度 ±30° · 反射 100%");
		_vsPresetNaturalBtn.Click += (s, e) => ApplyVsPreset(0.45, 30.0, 1.00);
		buttonsSp.Children.Add(_vsPresetNaturalBtn);

		// 【强烈】(强度 70%, 角度 ±45°, 反射 100%)
		_vsPresetStrongBtn = new Button
		{
			Content = "强烈",
			FontSize = 13,
			CornerRadius = new CornerRadius(6),
			Padding = new Thickness(18, 6, 18, 6)
		};
		ToolTipService.SetToolTip(_vsPresetStrongBtn, "强烈：强度 70% · 角度 ±45° · 反射 100%");
		_vsPresetStrongBtn.Click += (s, e) => ApplyVsPreset(0.70, 45.0, 1.00);
		buttonsSp.Children.Add(_vsPresetStrongBtn);

		sp.Children.Add(buttonsSp);
		card.Child = sp;
		return card;
	}

	private void ApplyVsPreset(double strength, double azimuth, double reflection)
	{
		_virtualStageStrength = strength;
		_virtualStageAzimuthDegrees = azimuth;
		_virtualStageReflection = reflection;
		_virtualStageEnabled = true;

		if (_vsStrengthSlider != null) _vsStrengthSlider.Value = Math.Round(strength * 100.0);
		if (_vsAzimuthSlider != null) _vsAzimuthSlider.Value = azimuth;
		if (_vsReflectionSlider != null) _vsReflectionSlider.Value = Math.Round(reflection * 100.0);

		UpdateVsAllVisuals();
		ApplyVirtualStageParameters();
	}

	private FrameworkElement CreateVsSlidersSection()
	{
		var card = new Border
		{
			Background = CreateAcrylicCardBrush(0.85, 238),
			BorderBrush = CreateLiquidGlassRimBrush(),
			BorderThickness = new Thickness(1),
			CornerRadius = new CornerRadius(12),
			Padding = new Thickness(18, 14, 18, 16)
		};

		var sp = new StackPanel { Spacing = 14 };
		sp.Children.Add(new TextBlock
		{
			Text = "参数控制",
			FontSize = 12.5,
			FontWeight = FontWeights.SemiBold,
			Foreground = DspTextMutedBrush
		});

		// 1. 强度滑块 (0% ~ 100%, 默认 45%)
		var strHeader = new Grid();
		strHeader.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.0, GridUnitType.Star) });
		strHeader.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
		var strLbl = new TextBlock { Text = "强度", FontSize = 13, Foreground = DspTextSecondaryBrush };
		_vsStrengthValTb = new TextBlock
		{
			Text = $"{Math.Round(_virtualStageStrength * 100)}%",
			FontSize = 13,
			FontWeight = FontWeights.SemiBold,
			Foreground = DspTextPrimaryBrush
		};
		Grid.SetColumn(strLbl, 0);
		Grid.SetColumn(_vsStrengthValTb, 1);
		strHeader.Children.Add(strLbl);
		strHeader.Children.Add(_vsStrengthValTb);
		sp.Children.Add(strHeader);

		_vsStrengthSlider = new Slider
		{
			Minimum = 0,
			Maximum = 100,
			StepFrequency = 1,
			Value = Math.Round(_virtualStageStrength * 100),
			Margin = new Thickness(0, -2, 0, 4)
		};
		_vsStrengthSlider.ValueChanged += (s, e) =>
		{
			_virtualStageStrength = e.NewValue / 100.0;
			if (_vsStrengthValTb != null) _vsStrengthValTb.Text = $"{Math.Round(e.NewValue)}%";
			UpdateVsPresetButtons();
			UpdateVsStatusText();
			ApplyVirtualStageParameters();
		};
		_vsStrengthSlider.DoubleTapped += (s, e) =>
		{
			if (_vsStrengthSlider != null) _vsStrengthSlider.Value = 45;
		};
		sp.Children.Add(_vsStrengthSlider);

		// 2. 模拟角度滑块 (±15° ~ ±75°, 默认 ±30°)
		var azHeader = new Grid();
		azHeader.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.0, GridUnitType.Star) });
		azHeader.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
		var azLbl = new TextBlock { Text = "模拟角度", FontSize = 13, Foreground = DspTextSecondaryBrush };
		_vsAzimuthValTb = new TextBlock
		{
			Text = $"±{Math.Round(_virtualStageAzimuthDegrees)}°",
			FontSize = 13,
			FontWeight = FontWeights.SemiBold,
			Foreground = DspTextPrimaryBrush
		};
		Grid.SetColumn(azLbl, 0);
		Grid.SetColumn(_vsAzimuthValTb, 1);
		azHeader.Children.Add(azLbl);
		azHeader.Children.Add(_vsAzimuthValTb);
		sp.Children.Add(azHeader);

		_vsAzimuthSlider = new Slider
		{
			Minimum = 15,
			Maximum = 75,
			StepFrequency = 1,
			Value = _virtualStageAzimuthDegrees,
			Margin = new Thickness(0, -2, 0, 4)
		};
		_vsAzimuthSlider.ValueChanged += (s, e) =>
		{
			_virtualStageAzimuthDegrees = e.NewValue;
			if (_vsAzimuthValTb != null) _vsAzimuthValTb.Text = $"±{Math.Round(e.NewValue)}°";
			if (_vsTopAngleBadgeTb != null) _vsTopAngleBadgeTb.Text = $"±{Math.Round(e.NewValue)}°";
			if (_vsStatAngleTb != null) _vsStatAngleTb.Text = $"±{Math.Round(e.NewValue)}°";
			UpdateVsDiagramVisuals();
			UpdateVsPresetButtons();
			UpdateVsStatusText();
			ApplyVirtualStageParameters();
		};
		_vsAzimuthSlider.DoubleTapped += (s, e) =>
		{
			if (_vsAzimuthSlider != null) _vsAzimuthSlider.Value = 30;
		};
		sp.Children.Add(_vsAzimuthSlider);

		// 3. 反射量滑块 (0% ~ 100%, 默认 100%)
		var refHeader = new Grid();
		refHeader.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.0, GridUnitType.Star) });
		refHeader.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
		var refLbl = new TextBlock { Text = "反射量", FontSize = 13, Foreground = DspTextSecondaryBrush };
		_vsReflectionValTb = new TextBlock
		{
			Text = $"{Math.Round(_virtualStageReflection * 100)}%",
			FontSize = 13,
			FontWeight = FontWeights.SemiBold,
			Foreground = DspTextPrimaryBrush
		};
		Grid.SetColumn(refLbl, 0);
		Grid.SetColumn(_vsReflectionValTb, 1);
		refHeader.Children.Add(refLbl);
		refHeader.Children.Add(_vsReflectionValTb);
		sp.Children.Add(refHeader);

		_vsReflectionSlider = new Slider
		{
			Minimum = 0,
			Maximum = 100,
			StepFrequency = 1,
			Value = Math.Round(_virtualStageReflection * 100),
			Margin = new Thickness(0, -2, 0, 4)
		};
		_vsReflectionSlider.ValueChanged += (s, e) =>
		{
			_virtualStageReflection = e.NewValue / 100.0;
			if (_vsReflectionValTb != null) _vsReflectionValTb.Text = $"{Math.Round(e.NewValue)}%";
			if (_vsStatReflectionTb != null) _vsStatReflectionTb.Text = $"{Math.Round(e.NewValue)}%";
			UpdateVsPresetButtons();
			UpdateVsStatusText();
			ApplyVirtualStageParameters();
		};
		_vsReflectionSlider.DoubleTapped += (s, e) =>
		{
			if (_vsReflectionSlider != null) _vsReflectionSlider.Value = 100;
		};
		sp.Children.Add(_vsReflectionSlider);

		card.Child = sp;
		return card;
	}

	// ==========================================
	// 动态图表与参数同步
	// ==========================================

	private void UpdateVsAllVisuals()
	{
		UpdateVsPowerButtonVisuals();
		UpdateVsDiagramVisuals();
		UpdateVsStatusText();
		UpdateVsPresetButtons();
		UpdateVsSliderReadouts();
	}

	private void UpdateVsPowerButtonVisuals()
	{
		if (_vsPowerBtn == null || _vsPowerIcon == null || _vsPowerText == null) return;

		if (_virtualStageEnabled)
		{
			_vsPowerText.Text = "已开启";
			_vsPowerText.Foreground = DspAccentSoftTextBrush;
			_vsPowerIcon.Foreground = DspAccentSoftTextBrush;
			_vsPowerBtn.Background = DspAccentSoftBgBrush;
			_vsPowerBtn.BorderBrush = DspAccentSoftBorderBrush;
		}
		else
		{
			_vsPowerText.Text = "已旁路 · 开始试听";
			_vsPowerText.Foreground = DspTextSubHeaderBrush;
			_vsPowerIcon.Foreground = DspTextMutedBrush;
			_vsPowerBtn.Background = DspSoftSurfaceBrush;
			_vsPowerBtn.BorderBrush = CreateLiquidGlassRimBrush();
		}
	}

	private void UpdateVsDiagramVisuals()
	{
		if (_vsCanvas == null) return;

		double angle = Math.Clamp(_virtualStageAzimuthDegrees, 15.0, 75.0);
		double rad = angle * Math.PI / 180.0;

		double sinVal = Math.Sin(rad);
		double cosVal = Math.Cos(rad);

		double xL = VsListenerCx - VsStageRadius * sinVal;
		double yL = VsListenerCy - VsStageRadius * cosVal;
		double xR = VsListenerCx + VsStageRadius * sinVal;
		double yR = VsListenerCy - VsStageRadius * cosVal;

		// 左右放射线
		if (_vsLeftRayLine != null)
		{
			_vsLeftRayLine.X1 = VsListenerCx;
			_vsLeftRayLine.Y1 = VsListenerCy;
			_vsLeftRayLine.X2 = xL;
			_vsLeftRayLine.Y2 = yL;
			_vsLeftRayLine.Stroke = _virtualStageEnabled
				? new SolidColorBrush(Color.FromArgb(255, 99, 102, 241))
				: new SolidColorBrush(Color.FromArgb(160, 148, 163, 184));
		}

		if (_vsRightRayLine != null)
		{
			_vsRightRayLine.X1 = VsListenerCx;
			_vsRightRayLine.Y1 = VsListenerCy;
			_vsRightRayLine.X2 = xR;
			_vsRightRayLine.Y2 = yR;
			_vsRightRayLine.Stroke = _virtualStageEnabled
				? new SolidColorBrush(Color.FromArgb(255, 99, 102, 241))
				: new SolidColorBrush(Color.FromArgb(160, 148, 163, 184));
		}

		// 左扬声器节点与文字
		if (_vsLeftSpeakerNode != null)
		{
			Canvas.SetLeft(_vsLeftSpeakerNode, xL - 14.0);
			Canvas.SetTop(_vsLeftSpeakerNode, yL - 14.0);
			_vsLeftSpeakerNode.BorderBrush = _virtualStageEnabled
				? new SolidColorBrush(Color.FromArgb(255, 99, 102, 241))
				: DspTextMutedBrush;
			if (_vsLeftSpeakerNode.Child is Border inner)
			{
				inner.Background = _virtualStageEnabled
					? new SolidColorBrush(Color.FromArgb(255, 99, 102, 241))
					: DspTextMutedBrush;
			}
		}

		if (_vsLeftSpeakerLabel != null)
		{
			Canvas.SetLeft(_vsLeftSpeakerLabel, xL - 6.0);
			Canvas.SetTop(_vsLeftSpeakerLabel, yL - 26.0);
		}

		// 右扬声器节点与文字
		if (_vsRightSpeakerNode != null)
		{
			Canvas.SetLeft(_vsRightSpeakerNode, xR - 14.0);
			Canvas.SetTop(_vsRightSpeakerNode, yR - 14.0);
			_vsRightSpeakerNode.BorderBrush = _virtualStageEnabled
				? new SolidColorBrush(Color.FromArgb(255, 99, 102, 241))
				: DspTextMutedBrush;
			if (_vsRightSpeakerNode.Child is Border inner)
			{
				inner.Background = _virtualStageEnabled
					? new SolidColorBrush(Color.FromArgb(255, 99, 102, 241))
					: DspTextMutedBrush;
			}
		}

		if (_vsRightSpeakerLabel != null)
		{
			Canvas.SetLeft(_vsRightSpeakerLabel, xR - 6.0);
			Canvas.SetTop(_vsRightSpeakerLabel, yR - 26.0);
		}

		// 听音点中心角度读数
		if (_vsDiagramAngleTb != null)
		{
			_vsDiagramAngleTb.Text = $"模拟 ±{Math.Round(angle)}°";
			Canvas.SetLeft(_vsDiagramAngleTb, VsListenerCx - 32.0);
			Canvas.SetTop(_vsDiagramAngleTb, VsListenerCy + 18.0);
		}
	}

	private void UpdateVsStatusText()
	{
		if (_vsStatusDescTb == null) return;

		if (!_virtualStageEnabled)
		{
			_vsStatusDescTb.Text = "已旁路，保留原始立体声，不添加空间处理。";
			_vsStatusDescTb.Foreground = DspTextMutedBrush;
		}
		else
		{
			if (_virtualStageStrength <= 0.001)
			{
				_vsStatusDescTb.Text = "强度为 0%，平滑过渡后恢复原始立体声。提高强度可加入空间线索。";
				_vsStatusDescTb.Foreground = DspTextMutedBrush;
			}
			else if (_virtualStageHrtfEnabled)
			{
				_vsStatusDescTb.Text = $"已开启 · 虚拟空间声场渲染中（HRTF: {_virtualStageHrtfProfile} · 模拟角度 ±{Math.Round(_virtualStageAzimuthDegrees)}°）";
				_vsStatusDescTb.Foreground = DspAccentSoftTextBrush;
			}
			else
			{
				_vsStatusDescTb.Text = "已开启 · 虚拟空间声场渲染中";
				_vsStatusDescTb.Foreground = DspAccentSoftTextBrush;
			}
		}
	}

	private void UpdateVsPresetButtons()
	{
		bool isLight = _virtualStageEnabled &&
			Math.Abs(_virtualStageStrength - 0.25) < 0.02 &&
			Math.Abs(_virtualStageAzimuthDegrees - 20.0) < 1.0 &&
			Math.Abs(_virtualStageReflection - 0.40) < 0.02;

		bool isNatural = _virtualStageEnabled &&
			Math.Abs(_virtualStageStrength - 0.45) < 0.02 &&
			Math.Abs(_virtualStageAzimuthDegrees - 30.0) < 1.0 &&
			Math.Abs(_virtualStageReflection - 1.00) < 0.02;

		bool isStrong = _virtualStageEnabled &&
			Math.Abs(_virtualStageStrength - 0.70) < 0.02 &&
			Math.Abs(_virtualStageAzimuthDegrees - 45.0) < 1.0 &&
			Math.Abs(_virtualStageReflection - 1.00) < 0.02;

		SetVsPresetButtonStyle(_vsPresetLightBtn, isLight);
		SetVsPresetButtonStyle(_vsPresetNaturalBtn, isNatural);
		SetVsPresetButtonStyle(_vsPresetStrongBtn, isStrong);
	}

	private void SetVsPresetButtonStyle(Button? btn, bool active)
	{
		if (btn == null) return;
		bool isDark = IsDspDark;
		if (active)
		{
			btn.Background = new SolidColorBrush(isDark ? Color.FromArgb(85, 99, 102, 241) : Color.FromArgb(255, 238, 242, 255));
			btn.BorderBrush = new SolidColorBrush(isDark ? Color.FromArgb(200, 129, 140, 248) : Color.FromArgb(255, 99, 102, 241));
			btn.Foreground = new SolidColorBrush(isDark ? Color.FromArgb(255, 224, 231, 255) : Color.FromArgb(255, 79, 70, 229));
			btn.FontWeight = FontWeights.SemiBold;
		}
		else
		{
			btn.Background = CreateWhiteAcrylicBrush(isDark ? 0.16 : 0.78, isDark ? (byte)45 : (byte)200);
			btn.BorderBrush = CreateLiquidGlassRimBrush();
			btn.Foreground = DspTextSubHeaderBrush;
			btn.FontWeight = FontWeights.Normal;
		}
	}

	private void UpdateVsSliderReadouts()
	{
		if (_vsStrengthValTb != null) _vsStrengthValTb.Text = $"{Math.Round(_virtualStageStrength * 100)}%";
		if (_vsStrengthSlider != null) _vsStrengthSlider.Value = Math.Round(_virtualStageStrength * 100);

		if (_vsAzimuthValTb != null) _vsAzimuthValTb.Text = $"±{Math.Round(_virtualStageAzimuthDegrees)}°";
		if (_vsTopAngleBadgeTb != null) _vsTopAngleBadgeTb.Text = $"±{Math.Round(_virtualStageAzimuthDegrees)}°";
		if (_vsStatAngleTb != null) _vsStatAngleTb.Text = $"±{Math.Round(_virtualStageAzimuthDegrees)}°";
		if (_vsAzimuthSlider != null) _vsAzimuthSlider.Value = _virtualStageAzimuthDegrees;

		if (_vsReflectionValTb != null) _vsReflectionValTb.Text = $"{Math.Round(_virtualStageReflection * 100)}%";
		if (_vsStatReflectionTb != null) _vsStatReflectionTb.Text = $"{Math.Round(_virtualStageReflection * 100)}%";
		if (_vsReflectionSlider != null) _vsReflectionSlider.Value = Math.Round(_virtualStageReflection * 100);

		if (_vsStatCenterTb != null) _vsStatCenterTb.Text = "直达声居中";

		if (_vsHrtfCheckBox != null) _vsHrtfCheckBox.IsChecked = _virtualStageHrtfEnabled;
		SelectVsComboProfile(_virtualStageHrtfProfile);
		UpdateVsAuditionButtonsStyle();
	}

	// ==========================================
	// 雷达图鼠标拖拽交互
	// ==========================================

	private void OnVsCanvasPointerPressed(object sender, PointerRoutedEventArgs e)
	{
		var pt = e.GetCurrentPoint(_vsCanvas);
		if (pt.Properties.IsLeftButtonPressed)
		{
			_isDraggingDiagram = true;
			_vsCanvas?.CapturePointer(e.Pointer);
			UpdateVsAngleFromPointer(pt.Position);
			e.Handled = true;
		}
	}

	private void OnVsCanvasPointerMoved(object sender, PointerRoutedEventArgs e)
	{
		if (_isDraggingDiagram && _vsCanvas != null)
		{
			var pt = e.GetCurrentPoint(_vsCanvas);
			UpdateVsAngleFromPointer(pt.Position);
			e.Handled = true;
		}
	}

	private void OnVsCanvasPointerReleased(object sender, PointerRoutedEventArgs e)
	{
		if (_isDraggingDiagram && _vsCanvas != null)
		{
			_isDraggingDiagram = false;
			_vsCanvas.ReleasePointerCapture(e.Pointer);
			e.Handled = true;
		}
	}

	private void UpdateVsAngleFromPointer(Point pos)
	{
		double dx = pos.X - VsListenerCx;
		double dy = VsListenerCy - pos.Y;
		if (dy > 4.0)
		{
			double angleRad = Math.Atan2(Math.Abs(dx), dy);
			double angleDeg = angleRad * 180.0 / Math.PI;
			double clamped = Math.Clamp(Math.Round(angleDeg), 15.0, 75.0);
			if (Math.Abs(_virtualStageAzimuthDegrees - clamped) > 0.01)
			{
				_virtualStageAzimuthDegrees = clamped;
				if (_vsAzimuthSlider != null)
				{
					_vsAzimuthSlider.Value = clamped;
				}
				UpdateVsDiagramVisuals();
				UpdateVsPresetButtons();
				UpdateVsStatusText();
				ApplyVirtualStageParameters();
			}
		}
	}

	// ==========================================
	// 导入 SOFA 文件
	// ==========================================

	private async void TriggerVsImportSofaFilePicker()
	{
		try
		{
			var picker = new FileOpenPicker
			{
				SuggestedStartLocation = PickerLocationId.MusicLibrary
			};
			picker.FileTypeFilter.Add(".sofa");
			picker.FileTypeFilter.Add(".wav");
			picker.FileTypeFilter.Add(".dat");
			picker.FileTypeFilter.Add("*");

			nint windowHandle = WindowNative.GetWindowHandle(WindowManager.CurrentWindow);
			InitializeWithWindow.Initialize(picker, windowHandle);

			var file = await picker.PickSingleFileAsync();
			if (file != null)
			{
				string profileName = file.DisplayName ?? file.Name;
				_virtualStageHrtfProfile = profileName;

				// 添加至下拉框中
				bool exists = false;
				if (_vsHrtfCombo != null)
				{
					for (int i = 0; i < _vsHrtfCombo.Items.Count; i++)
					{
						if (_vsHrtfCombo.Items[i] is ComboBoxItem cbi && (cbi.Content as string) == profileName)
						{
							_vsHrtfCombo.SelectedIndex = i;
							exists = true;
							break;
						}
					}
					if (!exists)
					{
						var newItem = new ComboBoxItem { Content = profileName };
						_vsHrtfCombo.Items.Add(newItem);
						_vsHrtfCombo.SelectedItem = newItem;
					}
				}

				_virtualStageHrtfEnabled = true;
				if (_vsHrtfCheckBox != null) _vsHrtfCheckBox.IsChecked = true;

				UpdateVsStatusText();
				ApplyVirtualStageParameters();
				Debug.WriteLine($"[FurinaPlayer] SOFA profile imported: {file.Path}");
			}
		}
		catch (Exception ex)
		{
			Debug.WriteLine($"[FurinaPlayer] Import SOFA failed: {ex.Message}");
		}
	}

	// ==========================================
	// 恢复默认与底层 DSP 同步
	// ==========================================

	private void ResetVirtualStageToDefaults()
	{
		_virtualStageEnabled = false;
		_virtualStageStrength = 0.45;
		_virtualStageAzimuthDegrees = 30.0;
		_virtualStageReflection = 1.0;
		_virtualStageHrtfEnabled = false;
		_virtualStageHrtfProfile = "SADIE II · 标准";

		if (_vsStrengthSlider != null) _vsStrengthSlider.Value = 45;
		if (_vsAzimuthSlider != null) _vsAzimuthSlider.Value = 30;
		if (_vsReflectionSlider != null) _vsReflectionSlider.Value = 100;
		if (_vsHrtfCheckBox != null) _vsHrtfCheckBox.IsChecked = false;
		if (_vsHrtfCombo != null) _vsHrtfCombo.SelectedIndex = 0;
		if (_vsAuditionPanel != null) _vsAuditionPanel.Visibility = Visibility.Collapsed;

		UpdateVsAllVisuals();
		ApplyVirtualStageParameters();
	}

	/// <summary>
	/// 对接底层 DSP 引擎：
	/// 1. 调用 AudioEngine / AudioSession 的 SetFurinaVirtualStage。
	/// 2. 声学互斥联动：开启虚拟声场时，若交叉馈送开启，则联动关闭交叉馈送。
	/// </summary>
	private void ApplyVirtualStageParameters()
	{
		try
		{
			if (_virtualStageEnabled)
			{
				// 声学互斥联动：开启虚拟声场时，若交叉馈送开启，则联动关闭交叉馈送
				if (ViewModel != null && ViewModel.EchoCrossfeedEnabled)
				{
					ViewModel.EchoCrossfeedEnabled = false;
				}
				var chain = ViewModel?.AudioEngine?.DspChain;
				if (chain != null && chain.Spatial.CrossfeedEnabled)
				{
					chain.Spatial.CrossfeedEnabled = false;
				}
			}

			ViewModel?.AudioEngine?.SetFurinaVirtualStage(
				_virtualStageEnabled,
				(float)_virtualStageStrength,
				(float)_virtualStageAzimuthDegrees,
				(float)_virtualStageReflection,
				_virtualStageHrtfEnabled,
				_virtualStageHrtfProfile
			);
		}
		catch (Exception ex)
		{
			Debug.WriteLine($"[FurinaPlayer] Apply Virtual Stage parameters error: {ex.Message}");
		}
	}
}
