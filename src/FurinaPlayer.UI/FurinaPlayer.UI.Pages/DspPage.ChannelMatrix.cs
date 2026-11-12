using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using FurinaPlayer.UI.ViewModels;
using Microsoft.UI;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using SonicWave.Audio.Dsp;
using SonicWave.Core.Models;
using Windows.UI;

namespace FurinaPlayer.UI.Pages;

/// <summary>
/// FurinaPlayer 声道矩阵（Channel Matrix）二级功能工作台分部实现。
/// 1:1 像素级复刻 2x2 声道混音与相位拓扑矩阵、动态信号流 Canvas 可视化、快速预设与精细系数控制。
/// </summary>
public sealed partial class DspPage
{
	#region Private Fields & Parameters State

	private FrameworkElement? _channelMatrixWorkbench;

	// Matrix State (-2.00 ~ +2.00, defaults: L->L: 1.0, R->L: 0.0, L->R: 0.0, R->R: 1.0)
	private bool _channelMatrixEnabled = false;
	private double _matLeftToLeft = 1.00;
	private double _matRightToLeft = 0.00;
	private double _matLeftToRight = 0.00;
	private double _matRightToRight = 1.00;

	// Header Controls
	private Button? _matrixPowerBtn;
	private TextBlock? _matrixPowerText;
	private FontIcon? _matrixPowerIcon;
	private Button? _matrixResetBtn;
	private Button? _matrixApplyBtn;
	private TextBlock? _matrixApplyBtnText;
	private TextBlock? _headerPeakGainTb;
	private TextBlock? _headerPeakGainSubTb;

	// Visual Canvas Elements
	private Canvas? _matrixCanvas;
	private Line? _lineL2L;
	private Line? _lineR2L;
	private Line? _lineL2R;
	private Line? _lineR2R;
	private Border? _capsuleL2L;
	private TextBlock? _capsuleTbL2L;
	private Border? _capsuleR2L;
	private TextBlock? _capsuleTbR2L;
	private Border? _capsuleL2R;
	private TextBlock? _capsuleTbL2R;
	private Border? _capsuleR2R;
	private TextBlock? _capsuleTbR2R;

	// Card Footer Labels
	private TextBlock? _cardFooterLeftTb;
	private TextBlock? _cardFooterRightTb;

	// Right Metrics Readouts
	private TextBlock? _leftPeakGainTb;
	private TextBlock? _rightPeakGainTb;
	private TextBlock? _phaseStateTb;
	private TextBlock? _safetyStatusTb;

	// Quick Presets
	private readonly List<(Button Button, string Name, double L2L, double R2L, double L2R, double R2R)> _matrixPresetButtons = new();

	// 4 Parameter Sliders & Values
	private Slider? _sliderL2L;
	private Slider? _sliderR2L;
	private Slider? _sliderL2R;
	private Slider? _sliderR2R;
	private TextBlock? _valL2LTb;
	private TextBlock? _valR2LTb;
	private TextBlock? _valL2RTb;
	private TextBlock? _valR2RTb;

	private bool _isUpdatingMatrixUi = false;

	#endregion

	#region Public Workbench Creator

	/// <summary>
	/// 构建并返回 FurinaPlayer 声道矩阵二级功能工作台界面元素。
	/// </summary>
	public FrameworkElement CreateChannelMatrixWorkbench()
	{
		if (_channelMatrixWorkbench != null)
		{
			return _channelMatrixWorkbench;
		}

		// Initialize parameters from AudioEngine if available
		try
		{
			if (ViewModel?.AudioEngine?.DspChain?.Spatial is { } spatial)
			{
				_channelMatrixEnabled = spatial.ChannelMatrixEnabled;
				_matLeftToLeft = Math.Clamp(spatial.MatrixLeftToLeft, -2.0, 2.0);
				_matRightToLeft = Math.Clamp(spatial.MatrixRightToLeft, -2.0, 2.0);
				_matLeftToRight = Math.Clamp(spatial.MatrixLeftToRight, -2.0, 2.0);
				_matRightToRight = Math.Clamp(spatial.MatrixRightToRight, -2.0, 2.0);
			}
		}
		catch (Exception ex)
		{
			Debug.WriteLine($"[FurinaPlayer] Init ChannelMatrix params failed: {ex.Message}");
		}

		var sv = new ScrollViewer
		{
			VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
			HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled
		};

		var root = new Grid
		{
			Background = CreateWhiteAcrylicBrush(0.72, 195),
			Padding = new Thickness(24, 20, 24, 24),
			RowSpacing = 16
		};

		root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // 0: Top Header & Toolbar
		root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // 1: 2x2 Signal Flow Canvas & Metrics Card
		root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // 2: Quick Presets
		root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // 3: Parameter Controls Area
		root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // 4: Bottom Hints & Actions Bar

		// -------------------------------------------------------------------------
		// Row 0: Top Header & Toolbar
		// -------------------------------------------------------------------------
		var headerGrid = CreateTopHeaderSection();
		Grid.SetRow(headerGrid, 0);
		root.Children.Add(headerGrid);

		// -------------------------------------------------------------------------
		// Row 1: 2x2 Signal Flow Canvas & Metrics Card
		// -------------------------------------------------------------------------
		var visualCard = CreateSignalFlowCard();
		Grid.SetRow(visualCard, 1);
		root.Children.Add(visualCard);

		// -------------------------------------------------------------------------
		// Row 2: Quick Presets Section
		// -------------------------------------------------------------------------
		var presetsSection = CreateQuickPresetsSection();
		Grid.SetRow(presetsSection, 2);
		root.Children.Add(presetsSection);

		// -------------------------------------------------------------------------
		// Row 3: Parameter Controls Area
		// -------------------------------------------------------------------------
		var controlsCard = CreateParameterControlsCard();
		Grid.SetRow(controlsCard, 3);
		root.Children.Add(controlsCard);

		// -------------------------------------------------------------------------
		// Row 4: Bottom Hints & Actions Bar
		// -------------------------------------------------------------------------
		var bottomBar = CreateBottomActionBar();
		Grid.SetRow(bottomBar, 4);
		root.Children.Add(bottomBar);

		sv.Content = root;
		_channelMatrixWorkbench = sv;

		// Initial refresh of visuals and readouts
		UpdateMatrixPowerVisual();
		UpdateMatrixCanvas();
		UpdateMatrixReadouts();
		UpdateMatrixPresetHighlights();

		return _channelMatrixWorkbench;
	}

	#endregion

	#region UI Construction Helpers

	private Grid CreateTopHeaderSection()
	{
		var headerGrid = new Grid();
		headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.0, GridUnitType.Star) });
		headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

		// Left: Titles & Description
		var titleSp = new StackPanel { Spacing = 2, VerticalAlignment = VerticalAlignment.Center };

		var eyebrowTb = new TextBlock
		{
			Text = "FurinaPlayer Spatial Workbench",
			FontSize = 11,
			FontWeight = FontWeights.SemiBold,
			Foreground = DspTextMutedBrush // Slate 400
		};
		titleSp.Children.Add(eyebrowTb);

		var mainTitleSp = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 10, VerticalAlignment = VerticalAlignment.Center };
		mainTitleSp.Children.Add(new TextBlock
		{
			Text = "声道矩阵",
			FontSize = 24,
			FontWeight = FontWeights.ExtraBold,
			Foreground = DspTextPrimaryBrush // Slate 900
		});
		titleSp.Children.Add(mainTitleSp);

		var descTb = new TextBlock
		{
			Text = "自定义 2x2 声道混音与相位矩阵，支持左右互换、反相抵消、单声道相加及精细系数加权。",
			FontSize = 12,
			Foreground = DspTextMutedBrush, // Slate 500
			Margin = new Thickness(0, 3, 0, 0)
		};
		titleSp.Children.Add(descTb);

		Grid.SetColumn(titleSp, 0);
		headerGrid.Children.Add(titleSp);

		// Right: Actions Toolbar
		var actionsSp = new StackPanel
		{
			Orientation = Orientation.Horizontal,
			Spacing = 10,
			VerticalAlignment = VerticalAlignment.Center
		};

		// 1. Peak Gain Readout on right of toolbar
		var topReadoutSp = new StackPanel
		{
			Spacing = 1,
			VerticalAlignment = VerticalAlignment.Center,
			Margin = new Thickness(0, 0, 6, 0)
		};
		_headerPeakGainTb = new TextBlock
		{
			Text = "1.00× · +0.0 dB",
			FontSize = 14,
			FontWeight = FontWeights.Bold,
			Foreground = DspTextPrimaryBrush,
			HorizontalAlignment = HorizontalAlignment.Right
		};
		_headerPeakGainSubTb = new TextBlock
		{
			Text = "安全线性增益",
			FontSize = 10,
			Foreground = new SolidColorBrush(Color.FromArgb(255, 16, 185, 129)), // Emerald 500
			HorizontalAlignment = HorizontalAlignment.Right
		};
		topReadoutSp.Children.Add(_headerPeakGainTb);
		topReadoutSp.Children.Add(_headerPeakGainSubTb);
		actionsSp.Children.Add(topReadoutSp);

		// 2. "PARAM MODEL" Badge
		var paramModelBadge = new Border
		{
			Background = DspSoftSurfaceBrush,
			BorderBrush = CreateLiquidGlassRimBrush(),
			BorderThickness = new Thickness(1),
			CornerRadius = new CornerRadius(14),
			Padding = new Thickness(10, 4, 10, 4),
			VerticalAlignment = VerticalAlignment.Center,
			Child = new StackPanel
			{
				Orientation = Orientation.Horizontal,
				Spacing = 5,
				Children =
				{
					new FontIcon { Glyph = "\uE9E9", FontSize = 12, Foreground = DspTextMutedBrush },
					new TextBlock { Text = "PARAM MODEL", FontSize = 11, FontWeight = FontWeights.SemiBold, Foreground = DspTextMutedBrush }
				}
			}
		};
		actionsSp.Children.Add(paramModelBadge);

		// 3. Power Button: [声道矩阵已开启] / [声道矩阵已旁路]
		_matrixPowerBtn = new Button
		{
			Padding = new Thickness(12, 6, 12, 6),
			CornerRadius = new CornerRadius(8),
			VerticalAlignment = VerticalAlignment.Center
		};
		var powerSp = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 6 };
		_matrixPowerIcon = new FontIcon { Glyph = "\uE7E8", FontSize = 12 };
		_matrixPowerText = new TextBlock { Text = "声道矩阵已开启", FontSize = 12, FontWeight = FontWeights.SemiBold };
		powerSp.Children.Add(_matrixPowerIcon);
		powerSp.Children.Add(_matrixPowerText);
		_matrixPowerBtn.Content = powerSp;
		_matrixPowerBtn.Click += (s, e) =>
		{
			_channelMatrixEnabled = !_channelMatrixEnabled;
			UpdateMatrixPowerVisual();
			ApplyChannelMatrixToAudioEngine(true);
		};
		actionsSp.Children.Add(_matrixPowerBtn);

		// 4. Reset Button: [恢复直通]
		_matrixResetBtn = new Button
		{
			Padding = new Thickness(12, 6, 12, 6),
			CornerRadius = new CornerRadius(8),
			Background = new SolidColorBrush(Color.FromArgb(220, 255, 255, 255)),
			BorderBrush = CreateLiquidGlassRimBrush(),
			VerticalAlignment = VerticalAlignment.Center
		};
		var resetSp = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 6 };
		resetSp.Children.Add(new FontIcon { Glyph = "\uE7A7", FontSize = 12, Foreground = DspTextSubHeaderBrush });
		resetSp.Children.Add(new TextBlock { Text = "恢复直通", FontSize = 12, Foreground = DspTextSecondaryBrush });
		_matrixResetBtn.Content = resetSp;
		_matrixResetBtn.Click += (s, e) => ResetMatrixToIdentity();
		actionsSp.Children.Add(_matrixResetBtn);

		// 5. Apply Button: [应用参数]
		_matrixApplyBtn = new Button
		{
			Padding = new Thickness(14, 6, 14, 6),
			CornerRadius = new CornerRadius(8),
			Background = new SolidColorBrush(Color.FromArgb(255, 79, 70, 229)), // Indigo 600
			VerticalAlignment = VerticalAlignment.Center
		};
		var applySp = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 6 };
		applySp.Children.Add(new FontIcon { Glyph = "\uE74E", FontSize = 12, Foreground = new SolidColorBrush(Colors.White) });
		_matrixApplyBtnText = new TextBlock
		{
			Text = "应用参数",
			FontSize = 12,
			FontWeight = FontWeights.SemiBold,
			Foreground = new SolidColorBrush(Colors.White)
		};
		applySp.Children.Add(_matrixApplyBtnText);
		_matrixApplyBtn.Content = applySp;
		_matrixApplyBtn.Click += async (s, e) =>
		{
			ApplyChannelMatrixToAudioEngine(true);
			if (_matrixApplyBtnText != null)
			{
				_matrixApplyBtnText.Text = "已应用";
				await Task.Delay(1200);
				_matrixApplyBtnText.Text = "应用参数";
			}
		};
		actionsSp.Children.Add(_matrixApplyBtn);

		Grid.SetColumn(actionsSp, 1);
		headerGrid.Children.Add(actionsSp);

		return headerGrid;
	}

	private Border CreateSignalFlowCard()
	{
		var card = new Border
		{
			Background = CreateAcrylicCardBrush(0.88, 235),
			BorderBrush = CreateLiquidGlassRimBrush(),
			BorderThickness = new Thickness(1),
			CornerRadius = new CornerRadius(12),
			Padding = new Thickness(20)
		};

		var grid = new Grid { RowSpacing = 12 };
		grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Header
		grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Visual Canvas & Metrics

		// 1. Card Header
		var headerGrid = new Grid();
		headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.0, GridUnitType.Star) });
		headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

		var titleSp = new StackPanel { Spacing = 2 };
		titleSp.Children.Add(new TextBlock
		{
			Text = "信号流矩阵",
			FontSize = 14,
			FontWeight = FontWeights.Bold,
			Foreground = DspTextPrimaryBrush
		});
		titleSp.Children.Add(new TextBlock
		{
			Text = "线宽对应系数绝对值，虚线表示反相；数值为当前草稿系数。",
			FontSize = 11,
			Foreground = DspTextMutedBrush
		});
		Grid.SetColumn(titleSp, 0);
		headerGrid.Children.Add(titleSp);

		var tag2x2 = new TextBlock
		{
			Text = "2 × 2",
			FontSize = 12,
			FontWeight = FontWeights.SemiBold,
			Foreground = DspTextMutedBrush,
			VerticalAlignment = VerticalAlignment.Center
		};
		Grid.SetColumn(tag2x2, 1);
		headerGrid.Children.Add(tag2x2);

		Grid.SetRow(headerGrid, 0);
		grid.Children.Add(headerGrid);

		// 2. Split Stage: Left Canvas with sub-footers, Right Stats Card
		var stageGrid = new Grid();
		stageGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.0, GridUnitType.Star) });
		stageGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Pixel) });
		stageGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(220, GridUnitType.Pixel) });

		// Left: Canvas & Footer
		var canvasHost = new StackPanel { Spacing = 8, HorizontalAlignment = HorizontalAlignment.Center };

		_matrixCanvas = new Canvas
		{
			Width = 460,
			Height = 230,
			HorizontalAlignment = HorizontalAlignment.Center
		};

		// Headers above nodes
		AddCanvasHeader(_matrixCanvas, "输入", 50, 10);
		AddCanvasHeader(_matrixCanvas, "输出", 380, 10);

		// 4 Connection Lines
		_lineL2L = CreateCanvasLine(83, 55, 377, 55);
		_lineR2L = CreateCanvasLine(81, 173, 379, 67);
		_lineL2R = CreateCanvasLine(81, 67, 379, 173);
		_lineR2R = CreateCanvasLine(83, 185, 377, 185);

		_matrixCanvas.Children.Add(_lineL2L);
		_matrixCanvas.Children.Add(_lineR2L);
		_matrixCanvas.Children.Add(_lineL2R);
		_matrixCanvas.Children.Add(_lineR2R);

		// 4 Value Capsules
		_capsuleL2L = CreateCanvasCapsule(out _capsuleTbL2L, 210, 24);
		_capsuleR2L = CreateCanvasCapsule(out _capsuleTbR2L, 265, 82);
		_capsuleL2R = CreateCanvasCapsule(out _capsuleTbL2R, 265, 142);
		_capsuleR2R = CreateCanvasCapsule(out _capsuleTbR2R, 210, 194);

		_matrixCanvas.Children.Add(_capsuleL2L);
		_matrixCanvas.Children.Add(_capsuleR2L);
		_matrixCanvas.Children.Add(_capsuleL2R);
		_matrixCanvas.Children.Add(_capsuleR2R);

		// 4 Nodes: Input L, Input R, Output L, Output R
		var nodeInL = CreateCanvasNode("L", 47, 37);
		var nodeInR = CreateCanvasNode("R", 47, 167);
		var nodeOutL = CreateCanvasNode("L", 377, 37);
		var nodeOutR = CreateCanvasNode("R", 377, 167);

		_matrixCanvas.Children.Add(nodeInL);
		_matrixCanvas.Children.Add(nodeInR);
		_matrixCanvas.Children.Add(nodeOutL);
		_matrixCanvas.Children.Add(nodeOutR);

		canvasHost.Children.Add(_matrixCanvas);

		// Footer line under Canvas
		var canvasFooter = new Grid { Width = 460 };
		canvasFooter.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.0, GridUnitType.Star) });
		canvasFooter.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.0, GridUnitType.Star) });

		_cardFooterLeftTb = new TextBlock
		{
			Text = "L → L 1.00 · R → L 0.00",
			FontSize = 11,
			FontFamily = new FontFamily("Segoe UI Variable Text, Consolas, Segoe UI"),
			Foreground = DspTextMutedBrush
		};
		Grid.SetColumn(_cardFooterLeftTb, 0);
		canvasFooter.Children.Add(_cardFooterLeftTb);

		_cardFooterRightTb = new TextBlock
		{
			Text = "L → R 0.00 · R → R 1.00",
			FontSize = 11,
			FontFamily = new FontFamily("Segoe UI Variable Text, Consolas, Segoe UI"),
			Foreground = DspTextMutedBrush,
			HorizontalAlignment = HorizontalAlignment.Right
		};
		Grid.SetColumn(_cardFooterRightTb, 1);
		canvasFooter.Children.Add(_cardFooterRightTb);

		canvasHost.Children.Add(canvasFooter);

		Grid.SetColumn(canvasHost, 0);
		stageGrid.Children.Add(canvasHost);

		// Divider
		var divider = new Border
		{
			Background = new SolidColorBrush(Color.FromArgb(120, 226, 232, 240)),
			Margin = new Thickness(16, 8, 16, 8)
		};
		Grid.SetColumn(divider, 1);
		stageGrid.Children.Add(divider);

		// Right: Stats Card
		var statsSp = new StackPanel
		{
			Spacing = 16,
			VerticalAlignment = VerticalAlignment.Center,
			Padding = new Thickness(8, 0, 0, 0)
		};

		// 1. Left Output Peak Gain
		statsSp.Children.Add(CreateStatItem("左输出峰值增益", out _leftPeakGainTb, "1.00× · +0.0 dB"));

		// 2. Right Output Peak Gain
		statsSp.Children.Add(CreateStatItem("右输出峰值增益", out _rightPeakGainTb, "1.00× · +0.0 dB"));

		// 3. Phase State
		statsSp.Children.Add(CreateStatItem("相位状态", out _phaseStateTb, "同相"));

		// 4. Safety Indicator
		var safetyBox = new Border
		{
			Background = new SolidColorBrush(Color.FromArgb(255, 240, 253, 244)), // Emerald 50
			BorderBrush = new SolidColorBrush(Color.FromArgb(255, 187, 247, 208)), // Emerald 200
			BorderThickness = new Thickness(1),
			CornerRadius = new CornerRadius(6),
			Padding = new Thickness(10, 5, 10, 5),
			HorizontalAlignment = HorizontalAlignment.Left
		};
		_safetyStatusTb = new TextBlock
		{
			Text = "安全线性增益",
			FontSize = 11,
			FontWeight = FontWeights.SemiBold,
			Foreground = new SolidColorBrush(Color.FromArgb(255, 5, 150, 105)) // Emerald 600
		};
		safetyBox.Child = _safetyStatusTb;
		statsSp.Children.Add(safetyBox);

		Grid.SetColumn(statsSp, 2);
		stageGrid.Children.Add(statsSp);

		Grid.SetRow(stageGrid, 1);
		grid.Children.Add(stageGrid);

		card.Child = grid;
		return card;
	}

	private StackPanel CreateStatItem(string label, out TextBlock valueTb, string defaultValue)
	{
		var sp = new StackPanel { Spacing = 2 };
		sp.Children.Add(new TextBlock
		{
			Text = label,
			FontSize = 11,
			Foreground = DspTextMutedBrush
		});
		valueTb = new TextBlock
		{
			Text = defaultValue,
			FontSize = 20,
			FontWeight = FontWeights.Bold,
			Foreground = DspTextPrimaryBrush,
			FontFamily = new FontFamily("Segoe UI Variable Display, Segoe UI")
		};
		sp.Children.Add(valueTb);
		return sp;
	}

	private StackPanel CreateQuickPresetsSection()
	{
		var section = new StackPanel { Spacing = 8 };

		section.Children.Add(new TextBlock
		{
			Text = "快速预设",
			FontSize = 13,
			FontWeight = FontWeights.Bold,
			Foreground = DspTextSecondaryBrush
		});

		var presetsSv = new ScrollViewer
		{
			HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
			VerticalScrollBarVisibility = ScrollBarVisibility.Disabled,
			HorizontalScrollMode = ScrollMode.Enabled,
			VerticalScrollMode = ScrollMode.Disabled
		};

		var presetsStack = new StackPanel
		{
			Orientation = Orientation.Horizontal,
			Spacing = 8
		};

		_matrixPresetButtons.Clear();

		// 7 Standard Presets
		AddPresetPill(presetsStack, "标准直通", 1.0, 0.0, 0.0, 1.0, "L→L 1.00 · R→L 0.00 · L→R 0.00 · R→R 1.00 (标准双声道直通)");
		AddPresetPill(presetsStack, "立体声互换", 0.0, 1.0, 1.0, 0.0, "L→L 0.00 · R→L 1.00 · L→R 1.00 · R→R 0.00 (左右声道完全调换)");
		AddPresetPill(presetsStack, "单声道混音", 0.5, 0.5, 0.5, 0.5, "L→L 0.50 · R→L 0.50 · L→R 0.50 · R→R 0.50 (等增益合成立体声单声道)");
		AddPresetPill(presetsStack, "仅保留左", 1.0, 0.0, 1.0, 0.0, "L→L 1.00 · R→L 0.00 · L→R 1.00 · R→R 0.00 (左声道复制到左右两侧)");
		AddPresetPill(presetsStack, "仅保留右", 0.0, 1.0, 0.0, 1.0, "L→L 0.00 · R→L 1.00 · L→R 0.00 · R→R 1.00 (右声道复制到左右两侧)");
		AddPresetPill(presetsStack, "中置抵消 (侧声道提取)", 1.0, -1.0, -1.0, 1.0, "L→L 1.00 · R→L -1.00 · L→R -1.00 · R→R 1.00 (相消居中人声，提取纯侧向声场)");
		AddPresetPill(presetsStack, "右声道反相", 1.0, 0.0, 0.0, -1.0, "L→L 1.00 · R→L 0.00 · L→R 0.00 · R→R -1.00 (反转右声道相位，消除特定声学相位抵消)");

		presetsSv.Content = presetsStack;
		section.Children.Add(presetsSv);

		return section;
	}

	private void AddPresetPill(StackPanel container, string name, double l2l, double r2l, double l2r, double r2r, string hint)
	{
		var btn = new Button
		{
			Content = name,
			FontSize = 12,
			CornerRadius = new CornerRadius(14),
			Padding = new Thickness(14, 6, 14, 6),
			Background = new SolidColorBrush(Color.FromArgb(220, 255, 255, 255)),
			BorderBrush = CreateLiquidGlassRimBrush(),
			BorderThickness = new Thickness(1)
		};

		ToolTipService.SetToolTip(btn, hint);

		btn.Click += (s, e) =>
		{
			ApplyMatrixPreset(l2l, r2l, l2r, r2r);
		};

		_matrixPresetButtons.Add((btn, name, l2l, r2l, l2r, r2r));
		container.Children.Add(btn);
	}

	private Border CreateParameterControlsCard()
	{
		var card = new Border
		{
			Background = CreateAcrylicCardBrush(0.88, 235),
			BorderBrush = CreateLiquidGlassRimBrush(),
			BorderThickness = new Thickness(1),
			CornerRadius = new CornerRadius(12),
			Padding = new Thickness(18)
		};

		var stack = new StackPanel { Spacing = 14 };

		stack.Children.Add(new TextBlock
		{
			Text = "参数控制",
			FontSize = 13,
			FontWeight = FontWeights.Bold,
			Foreground = DspTextSecondaryBrush
		});

		var grid = new Grid { ColumnSpacing = 14 };
		grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.0, GridUnitType.Star) });
		grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.0, GridUnitType.Star) });
		grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.0, GridUnitType.Star) });
		grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.0, GridUnitType.Star) });

		// Col 0: L -> L
		var col0 = CreateSliderControlGroup(
			"输入 L → 输出 L",
			1.00,
			out _sliderL2L,
			out _valL2LTb,
			val => { _matLeftToLeft = val; OnCoefficientsChanged(); },
			() => { _matLeftToLeft = -_matLeftToLeft; SyncSliderValue(_sliderL2L, _matLeftToLeft); OnCoefficientsChanged(); },
			1.00);
		Grid.SetColumn(col0, 0);
		grid.Children.Add(col0);

		// Col 1: R -> L
		var col1 = CreateSliderControlGroup(
			"输入 R → 输出 L",
			0.00,
			out _sliderR2L,
			out _valR2LTb,
			val => { _matRightToLeft = val; OnCoefficientsChanged(); },
			() => { _matRightToLeft = -_matRightToLeft; SyncSliderValue(_sliderR2L, _matRightToLeft); OnCoefficientsChanged(); },
			0.00);
		Grid.SetColumn(col1, 1);
		grid.Children.Add(col1);

		// Col 2: L -> R
		var col2 = CreateSliderControlGroup(
			"输入 L → 输出 R",
			0.00,
			out _sliderL2R,
			out _valL2RTb,
			val => { _matLeftToRight = val; OnCoefficientsChanged(); },
			() => { _matLeftToRight = -_matLeftToRight; SyncSliderValue(_sliderL2R, _matLeftToRight); OnCoefficientsChanged(); },
			0.00);
		Grid.SetColumn(col2, 2);
		grid.Children.Add(col2);

		// Col 3: R -> R
		var col3 = CreateSliderControlGroup(
			"输入 R → 输出 R",
			1.00,
			out _sliderR2R,
			out _valR2RTb,
			val => { _matRightToRight = val; OnCoefficientsChanged(); },
			() => { _matRightToRight = -_matRightToRight; SyncSliderValue(_sliderR2R, _matRightToRight); OnCoefficientsChanged(); },
			1.00);
		Grid.SetColumn(col3, 3);
		grid.Children.Add(col3);

		stack.Children.Add(grid);
		card.Child = stack;
		return card;
	}

	private Border CreateSliderControlGroup(
		string title,
		double defaultValue,
		out Slider slider,
		out TextBlock valTb,
		Action<double> onValueChanged,
		Action onInvertClicked,
		double defaultResetValue)
	{
		var border = new Border
		{
			Background = new SolidColorBrush(Color.FromArgb(200, 255, 255, 255)),
			BorderBrush = CreateLiquidGlassRimBrush(),
			BorderThickness = new Thickness(1),
			CornerRadius = new CornerRadius(8),
			Padding = new Thickness(12, 10, 12, 10)
		};

		var sp = new StackPanel { Spacing = 6 };

		// Header Row: Title on Left, Value + "±" on Right
		var hGrid = new Grid();
		hGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.0, GridUnitType.Star) });
		hGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

		var titleTb = new TextBlock
		{
			Text = title,
			FontSize = 11.5,
			FontWeight = FontWeights.SemiBold,
			Foreground = DspTextSecondaryBrush,
			VerticalAlignment = VerticalAlignment.Center
		};
		Grid.SetColumn(titleTb, 0);
		hGrid.Children.Add(titleTb);

		var rightSp = new StackPanel
		{
			Orientation = Orientation.Horizontal,
			Spacing = 6,
			VerticalAlignment = VerticalAlignment.Center
		};

		valTb = new TextBlock
		{
			Text = defaultValue.ToString("F2"),
			FontSize = 12.5,
			FontWeight = FontWeights.Bold,
			FontFamily = new FontFamily("Segoe UI Variable Text, Consolas, Segoe UI"),
			Foreground = DspAccentSoftTextBrush,
			Width = 36,
			TextAlignment = TextAlignment.Right,
			VerticalAlignment = VerticalAlignment.Center
		};
		rightSp.Children.Add(valTb);

		var invertBtn = new Button
		{
			Content = "±",
			FontSize = 12,
			FontWeight = FontWeights.Bold,
			Width = 24,
			Height = 24,
			CornerRadius = new CornerRadius(12),
			Padding = new Thickness(0),
			Background = DspSoftSurfaceBrush,
			BorderBrush = CreateLiquidGlassRimBrush(),
			Foreground = DspTextSubHeaderBrush
		};
		ToolTipService.SetToolTip(invertBtn, "反相 (±)");
		invertBtn.Click += (s, e) => onInvertClicked();
		rightSp.Children.Add(invertBtn);

		Grid.SetColumn(rightSp, 1);
		hGrid.Children.Add(rightSp);
		sp.Children.Add(hGrid);

		// Slider (-2.00 ~ +2.00, step 0.01)
		var s = new Slider
		{
			Minimum = -2.00,
			Maximum = 2.00,
			StepFrequency = 0.01,
			SmallChange = 0.01,
			LargeChange = 0.1,
			Value = defaultValue,
			Margin = new Thickness(0, 2, 0, 0)
		};

		// Double-click reset to default
		s.DoubleTapped += (sender, e) =>
		{
			s.Value = defaultResetValue;
		};
		border.DoubleTapped += (sender, e) =>
		{
			s.Value = defaultResetValue;
		};

		s.ValueChanged += (sender, e) =>
		{
			if (_isUpdatingMatrixUi) return;
			onValueChanged(e.NewValue);
		};

		slider = s;
		sp.Children.Add(s);

		// Hint text
		var hintTb = new TextBlock
		{
			Text = "双击滑杆重置默认",
			FontSize = 9.5,
			Foreground = DspTextMutedBrush,
			HorizontalAlignment = HorizontalAlignment.Center
		};
		sp.Children.Add(hintTb);

		border.Child = sp;
		return border;
	}

	private Grid CreateBottomActionBar()
	{
		var bar = new Grid();
		bar.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.0, GridUnitType.Star) });
		bar.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

		// Left: Operational note and safety warning
		var noteSp = new StackPanel { Spacing = 3, VerticalAlignment = VerticalAlignment.Center };

		noteSp.Children.Add(new TextBlock
		{
			Text = "编辑先进入草稿；点击应用后由原生主机平滑接管。图形由参数推导，处理事实在 Audio Core。",
			FontSize = 11,
			Foreground = DspTextMutedBrush
		});

		noteSp.Children.Add(new TextBlock
		{
			Text = "矩阵系数绝对值之和大于 1.0 时可能存在电平放大风险，建议配合机架限幅器或前级增益使用。",
			FontSize = 11,
			FontWeight = FontWeights.SemiBold,
			Foreground = new SolidColorBrush(Color.FromArgb(255, 234, 88, 12)) // Amber/Orange 600
		});

		Grid.SetColumn(noteSp, 0);
		bar.Children.Add(noteSp);

		// Right: Reset & Apply Buttons
		var rightSp = new StackPanel
		{
			Orientation = Orientation.Horizontal,
			Spacing = 10,
			VerticalAlignment = VerticalAlignment.Center,
			Margin = new Thickness(16, 0, 0, 0)
		};

		var resetBtn = new Button
		{
			Padding = new Thickness(14, 7, 14, 7),
			CornerRadius = new CornerRadius(8),
			Background = new SolidColorBrush(Color.FromArgb(220, 255, 255, 255)),
			BorderBrush = CreateLiquidGlassRimBrush()
		};
		var resetSp = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 6 };
		resetSp.Children.Add(new FontIcon { Glyph = "\uE7A7", FontSize = 12, Foreground = DspTextSubHeaderBrush });
		resetSp.Children.Add(new TextBlock { Text = "恢复直通", FontSize = 12, Foreground = DspTextSecondaryBrush });
		resetBtn.Content = resetSp;
		resetBtn.Click += (s, e) => ResetMatrixToIdentity();
		rightSp.Children.Add(resetBtn);

		var applyBtn = new Button
		{
			Padding = new Thickness(16, 7, 16, 7),
			CornerRadius = new CornerRadius(8),
			Background = new SolidColorBrush(Color.FromArgb(255, 79, 70, 229)), // Indigo 600
			Foreground = new SolidColorBrush(Colors.White)
		};
		var applySp = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 6 };
		applySp.Children.Add(new FontIcon { Glyph = "\uE74E", FontSize = 12, Foreground = new SolidColorBrush(Colors.White) });
		var applyBtnText = new TextBlock { Text = "应用参数", FontSize = 12, FontWeight = FontWeights.SemiBold, Foreground = new SolidColorBrush(Colors.White) };
		applySp.Children.Add(applyBtnText);
		applyBtn.Content = applySp;
		applyBtn.Click += async (s, e) =>
		{
			ApplyChannelMatrixToAudioEngine(true);
			applyBtnText.Text = "已应用";
			await Task.Delay(1200);
			applyBtnText.Text = "应用参数";
		};
		rightSp.Children.Add(applyBtn);

		Grid.SetColumn(rightSp, 1);
		bar.Children.Add(rightSp);

		return bar;
	}

	#endregion

	#region Canvas Element Factories

	private void AddCanvasHeader(Canvas canvas, string text, double left, double top)
	{
		var tb = new TextBlock
		{
			Text = text,
			FontSize = 12,
			FontWeight = FontWeights.SemiBold,
			Foreground = DspTextMutedBrush,
			IsHitTestVisible = false
		};
		Canvas.SetLeft(tb, left);
		Canvas.SetTop(tb, top);
		canvas.Children.Add(tb);
	}

	private static Line CreateCanvasLine(double x1, double y1, double x2, double y2)
	{
		var line = new Line
		{
			X1 = x1,
			Y1 = y1,
			X2 = x2,
			Y2 = y2,
			StrokeStartLineCap = PenLineCap.Round,
			StrokeEndLineCap = PenLineCap.Round,
			IsHitTestVisible = false
		};
		Canvas.SetZIndex(line, 1);
		return line;
	}

	private Border CreateCanvasCapsule(out TextBlock labelTb, double left, double top)
	{
		bool isDark = IsDspDark;
		labelTb = new TextBlock
		{
			Text = "1.00",
			FontSize = 11,
			FontWeight = FontWeights.Bold,
			FontFamily = new FontFamily("Segoe UI Variable Text, Consolas, Segoe UI"),
			Foreground = new SolidColorBrush(isDark ? Color.FromArgb(255, 165, 180, 252) : Color.FromArgb(255, 79, 70, 229)),
			HorizontalAlignment = HorizontalAlignment.Center,
			VerticalAlignment = VerticalAlignment.Center
		};

		var border = new Border
		{
			Background = new SolidColorBrush(isDark ? Color.FromArgb(225, 30, 41, 59) : Color.FromArgb(240, 255, 255, 255)),
			BorderBrush = new SolidColorBrush(isDark ? Color.FromArgb(140, 129, 140, 248) : Color.FromArgb(140, 203, 213, 225)),
			BorderThickness = new Thickness(1),
			CornerRadius = new CornerRadius(10),
			Padding = new Thickness(8, 2, 8, 2),
			Child = labelTb,
			IsHitTestVisible = false
		};

		Canvas.SetLeft(border, left);
		Canvas.SetTop(border, top);
		Canvas.SetZIndex(border, 10);
		return border;
	}

	private Border CreateCanvasNode(string label, double left, double top)
	{
		bool isDark = IsDspDark;
		var tb = new TextBlock
		{
			Text = label,
			FontSize = 14,
			FontWeight = FontWeights.Bold,
			Foreground = DspTextSecondaryBrush,
			HorizontalAlignment = HorizontalAlignment.Center,
			VerticalAlignment = VerticalAlignment.Center
		};

		var border = new Border
		{
			Width = 36,
			Height = 36,
			CornerRadius = new CornerRadius(18),
			Background = new SolidColorBrush(isDark ? Color.FromArgb(220, 30, 41, 59) : Colors.White),
			BorderBrush = new SolidColorBrush(isDark ? Color.FromArgb(160, 148, 163, 184) : Color.FromArgb(255, 203, 213, 225)),
			BorderThickness = new Thickness(1.5),
			Child = tb,
			IsHitTestVisible = false
		};

		Canvas.SetLeft(border, left);
		Canvas.SetTop(border, top);
		Canvas.SetZIndex(border, 20);
		return border;
	}

	#endregion

	#region Dynamics, Visuals & DSP Update Loop

	private void OnCoefficientsChanged()
	{
		UpdateMatrixCanvas();
		UpdateMatrixReadouts();
		UpdateMatrixPresetHighlights();
		ApplyChannelMatrixToAudioEngine(false);
	}

	private void UpdateMatrixCanvas()
	{
		UpdateLineVisual(_lineL2L, _capsuleL2L, _capsuleTbL2L, _matLeftToLeft);
		UpdateLineVisual(_lineR2L, _capsuleR2L, _capsuleTbR2L, _matRightToLeft);
		UpdateLineVisual(_lineL2R, _capsuleL2R, _capsuleTbL2R, _matLeftToRight);
		UpdateLineVisual(_lineR2R, _capsuleR2R, _capsuleTbR2R, _matRightToRight);

		if (_cardFooterLeftTb != null)
		{
			_cardFooterLeftTb.Text = $"L → L {_matLeftToLeft:F2} · R → L {_matRightToLeft:F2}";
		}
		if (_cardFooterRightTb != null)
		{
			_cardFooterRightTb.Text = $"L → R {_matLeftToRight:F2} · R → R {_matRightToRight:F2}";
		}

		if (_valL2LTb != null) _valL2LTb.Text = _matLeftToLeft.ToString("F2");
		if (_valR2LTb != null) _valR2LTb.Text = _matRightToLeft.ToString("F2");
		if (_valL2RTb != null) _valL2RTb.Text = _matLeftToRight.ToString("F2");
		if (_valR2RTb != null) _valR2RTb.Text = _matRightToRight.ToString("F2");
	}

	private void UpdateLineVisual(Line? line, Border? capsule, TextBlock? labelTb, double coeff)
	{
		if (line == null || capsule == null || labelTb == null) return;

		double absCoeff = Math.Abs(coeff);
		labelTb.Text = coeff.ToString("F2");

		if (absCoeff <= 0.005)
		{
			// Faint gray line for zero or near zero
			line.Stroke = new SolidColorBrush(Color.FromArgb(40, 148, 163, 184));
			line.StrokeThickness = 1.0;
			line.StrokeDashArray = null;

			labelTb.Foreground = DspTextMutedBrush;
			capsule.Opacity = 0.65;
		}
		else if (coeff < 0)
		{
			// Negative / Inverted polarity: Orange-Red Dashed
			line.Stroke = new SolidColorBrush(Color.FromArgb(255, 239, 68, 68)); // #EF4444
			line.StrokeThickness = Math.Clamp(1.2 + absCoeff * 3.5, 1.5, 8.0);
			line.StrokeDashArray = new DoubleCollection { 6, 4 };

			labelTb.Foreground = new SolidColorBrush(Color.FromArgb(255, 239, 68, 68));
			capsule.Opacity = 1.0;
		}
		else
		{
			// Positive coefficient: Indigo Solid
			line.Stroke = new SolidColorBrush(Color.FromArgb(255, 99, 102, 241)); // #6366F1
			line.StrokeThickness = Math.Clamp(1.2 + absCoeff * 3.5, 1.5, 8.0);
			line.StrokeDashArray = null;

			labelTb.Foreground = DspAccentSoftTextBrush;
			capsule.Opacity = 1.0;
		}
	}

	private void UpdateMatrixReadouts()
	{
		double leftPeakLinear = Math.Abs(_matLeftToLeft) + Math.Abs(_matRightToLeft);
		double rightPeakLinear = Math.Abs(_matLeftToRight) + Math.Abs(_matRightToRight);

		string leftReadout = FormatGainLinearAndDb(leftPeakLinear);
		string rightReadout = FormatGainLinearAndDb(rightPeakLinear);

		if (_leftPeakGainTb != null) _leftPeakGainTb.Text = leftReadout;
		if (_rightPeakGainTb != null) _rightPeakGainTb.Text = rightReadout;

		bool hasInversion = _matLeftToLeft < -0.005 || _matRightToLeft < -0.005 || _matLeftToRight < -0.005 || _matRightToRight < -0.005;

		if (_phaseStateTb != null)
		{
			if (hasInversion)
			{
				_phaseStateTb.Text = "含反相";
				_phaseStateTb.Foreground = new SolidColorBrush(Color.FromArgb(255, 239, 68, 68)); // Red 500
			}
			else
			{
				_phaseStateTb.Text = "同相";
				_phaseStateTb.Foreground = DspTextPrimaryBrush; // Slate 800
			}
		}

		double maxPeak = Math.Max(leftPeakLinear, rightPeakLinear);
		bool isClippingRisk = maxPeak > 1.001;

		if (_safetyStatusTb != null)
		{
			if (isClippingRisk)
			{
				_safetyStatusTb.Text = "电平放大风险 (满刻度可能削波)";
				_safetyStatusTb.Foreground = new SolidColorBrush(Color.FromArgb(255, 234, 88, 12)); // Orange 600
			}
			else
			{
				_safetyStatusTb.Text = "安全线性增益";
				_safetyStatusTb.Foreground = new SolidColorBrush(Color.FromArgb(255, 5, 150, 105)); // Emerald 600
			}
		}

		// Update Header Readout
		if (_headerPeakGainTb != null)
		{
			_headerPeakGainTb.Text = FormatGainLinearAndDb(maxPeak);
		}
		if (_headerPeakGainSubTb != null)
		{
			if (isClippingRisk)
			{
				_headerPeakGainSubTb.Text = "电平放大风险";
				_headerPeakGainSubTb.Foreground = new SolidColorBrush(Color.FromArgb(255, 234, 88, 12));
			}
			else
			{
				_headerPeakGainSubTb.Text = "安全线性增益";
				_headerPeakGainSubTb.Foreground = new SolidColorBrush(Color.FromArgb(255, 16, 185, 129));
			}
		}
	}

	private static string FormatGainLinearAndDb(double linear)
	{
		if (linear <= 0.0001)
		{
			return $"{linear:F2}× · −∞ dB";
		}
		double db = 20.0 * Math.Log10(linear);
		string sign = db >= 0.0 ? "+" : "";
		return $"{linear:F2}× · {sign}{db:F1} dB";
	}

	private void UpdateMatrixPresetHighlights()
	{
		foreach (var (btn, _, l2l, r2l, l2r, r2r) in _matrixPresetButtons)
		{
			bool isActive = Math.Abs(_matLeftToLeft - l2l) < 0.005
			             && Math.Abs(_matRightToLeft - r2l) < 0.005
			             && Math.Abs(_matLeftToRight - l2r) < 0.005
			             && Math.Abs(_matRightToRight - r2r) < 0.005;

			if (isActive)
			{
				btn.Background = DspAccentSoftBgBrush;
				btn.BorderBrush = DspAccentSoftBorderBrush;
				btn.Foreground = DspAccentSoftTextBrush;
			}
			else
			{
				btn.Background = DspSoftSurfaceBrush;
				btn.BorderBrush = CreateLiquidGlassRimBrush();
				btn.Foreground = DspTextSubHeaderBrush;
			}
		}
	}

	private void UpdateMatrixPowerVisual()
	{
		if (_matrixPowerBtn == null || _matrixPowerText == null || _matrixPowerIcon == null) return;

		if (_channelMatrixEnabled)
		{
			_matrixPowerText.Text = "声道矩阵已开启";
			_matrixPowerBtn.Background = DspAccentSoftBgBrush;
			_matrixPowerBtn.BorderBrush = DspAccentSoftBorderBrush;
			_matrixPowerText.Foreground = DspAccentSoftTextBrush;
			_matrixPowerIcon.Foreground = DspAccentSoftTextBrush;
		}
		else
		{
			_matrixPowerText.Text = "声道矩阵已旁路";
			_matrixPowerBtn.Background = DspSoftSurfaceBrush;
			_matrixPowerBtn.BorderBrush = CreateLiquidGlassRimBrush();
			_matrixPowerText.Foreground = DspTextMutedBrush;
			_matrixPowerIcon.Foreground = DspTextMutedBrush;
		}
	}

	private void SyncSliderValue(Slider? slider, double val)
	{
		if (slider == null) return;
		_isUpdatingMatrixUi = true;
		try
		{
			slider.Value = Math.Clamp(val, -2.0, 2.0);
		}
		finally
		{
			_isUpdatingMatrixUi = false;
		}
	}

	private void ApplyMatrixPreset(double l2l, double r2l, double l2r, double r2r)
	{
		_matLeftToLeft = l2l;
		_matRightToLeft = r2l;
		_matLeftToRight = l2r;
		_matRightToRight = r2r;
		_channelMatrixEnabled = true;

		SyncSliderValue(_sliderL2L, _matLeftToLeft);
		SyncSliderValue(_sliderR2L, _matRightToLeft);
		SyncSliderValue(_sliderL2R, _matLeftToRight);
		SyncSliderValue(_sliderR2R, _matRightToRight);

		UpdateMatrixPowerVisual();
		UpdateMatrixCanvas();
		UpdateMatrixReadouts();
		UpdateMatrixPresetHighlights();

		ApplyChannelMatrixToAudioEngine(true);
	}

	private void ResetMatrixToIdentity()
	{
		_matLeftToLeft = 1.00;
		_matRightToLeft = 0.00;
		_matLeftToRight = 0.00;
		_matRightToRight = 1.00;

		SyncSliderValue(_sliderL2L, _matLeftToLeft);
		SyncSliderValue(_sliderR2L, _matRightToLeft);
		SyncSliderValue(_sliderL2R, _matLeftToRight);
		SyncSliderValue(_sliderR2R, _matRightToRight);

		UpdateMatrixCanvas();
		UpdateMatrixReadouts();
		UpdateMatrixPresetHighlights();

		ApplyChannelMatrixToAudioEngine(true);
	}

	private void ApplyChannelMatrixToAudioEngine(bool saveSettings = false)
	{
		try
		{
			var engine = ViewModel?.AudioEngine;
			if (engine != null)
			{
				engine.SetFurinaChannelMatrixDirect(
					_channelMatrixEnabled,
					(float)_matLeftToLeft,
					(float)_matRightToLeft,
					(float)_matLeftToRight,
					(float)_matRightToRight);
			}

			if (saveSettings)
			{
				ViewModel?.SaveEchoSettings();
			}
		}
		catch (Exception ex)
		{
			Debug.WriteLine($"[FurinaPlayer] ChannelMatrix apply failed: {ex.Message}");
		}
	}

	#endregion
}
