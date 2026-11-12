using System;
using System.Collections.Generic;
using FurinaPlayer.UI.Controls;
using FurinaPlayer.UI.ViewModels;
using Microsoft.UI;
using Microsoft.UI.Dispatching;
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
/// FurinaPlayer 三频段压缩 (Multiband Compressor) 二级功能工作台分部实现。
/// 1:1 像素级精确还原，提供三频段动态色块分频图、150Hz / 2.5kHz 可拖拽分频线、整机与单频段双栏控制及专业软拐点动态压缩处理。
/// </summary>
public sealed partial class DspPage
{
	private Border? _multibandCompressorWorkbench;
	private Grid? _mbRootGrid;
	private Border? _mbTitleIconBorder;
	private FontIcon? _mbTitleIcon;
	private int _multibandSelectedBand = 1; // 0: 低频, 1: 中频, 2: 高频 (默认选中中频)
	private DispatcherTimer? _multibandTimer;
	private bool _isUpdatingMultibandUi = false;

	// 主题追踪集合
	private readonly List<TextBlock> _mbPrimaryTextBlocks = new();
	private readonly List<TextBlock> _mbSecondaryTextBlocks = new();
	private readonly List<TextBlock> _mbMutedTextBlocks = new();
	private readonly List<Button> _mbPresetButtons = new();

	// 顶部与操作栏控件
	private Button? _mbPowerButton;
	private FontIcon? _mbPowerIcon;
	private TextBlock? _mbPowerText;
	private Button? _mbRestoreDefaultBtn;
	private FontIcon? _mbRestoreDefaultIcon;
	private TextBlock? _mbRestoreDefaultText;
	private Border? _mbBandSwitcherBorder;
	private readonly List<Button> _mbBandSelectorButtons = new();

	// 试听快捷预设
	private Button? _mbPresetGentleBtn;
	private Button? _mbPresetBassBtn;
	private Button? _mbPresetStrongBtn;

	// 状态说明与增益衰减读数
	private TextBlock? _mbStatusDot;
	private TextBlock? _mbStatusTitle;
	private TextBlock? _mbStatusDetail;
	private TextBlock? _mbMaxGrNumberTb;
	private TextBlock? _mbMaxGrSubTb;

	// 中央分频可视化卡片
	private Border? _mbCrossoverCardBorder;
	private Canvas? _mbPlotCanvas;
	private Border? _mbBandRegionLow;
	private Border? _mbBandRegionMid;
	private Border? _mbBandRegionHigh;
	private readonly TextBlock?[] _mbBandRegionTitleTbs = new TextBlock?[3];
	private TextBlock? _mbRegionLowGrTb;
	private TextBlock? _mbRegionMidGrTb;
	private TextBlock? _mbRegionHighGrTb;
	private Line? _mbDividerLowLine;
	private Line? _mbDividerHighLine;
	private Border? _mbDividerLowHandle;
	private Border? _mbDividerHighHandle;
	private TextBlock? _mbDividerLowHzTb;
	private TextBlock? _mbDividerHighHzTb;

	private string? _draggingDivider = null; // "low" or "high"

	// 底部整机控制 (Column 1)
	private Slider? _mbMasterLowCrossSlider;
	private TextBlock? _mbMasterLowCrossValueTb;
	private Slider? _mbMasterHighCrossSlider;
	private TextBlock? _mbMasterHighCrossValueTb;
	private Slider? _mbMasterMixSlider;
	private TextBlock? _mbMasterMixValueTb;

	// 底部单频段控制 (Column 2)
	private TextBlock? _mbBandPanelTitleTb;
	private TextBlock? _mbBandPanelGrTb;
	private ToggleSwitch? _mbBandEnableSwitch;
	private TextBlock? _mbBandEnableStateTb;
	private Slider? _mbBandThresholdSlider;
	private TextBlock? _mbBandThresholdValueTb;
	private Slider? _mbBandRatioSlider;
	private TextBlock? _mbBandRatioValueTb;

	// 底部快慢控制 (Column 3)
	private TextBlock? _mbSpeedTitleTb;
	private TextBlock? _mbSpeedScopeTb;
	private Slider? _mbBandAttackSlider;
	private TextBlock? _mbBandAttackValueTb;
	private Slider? _mbBandReleaseSlider;
	private TextBlock? _mbBandReleaseValueTb;

	public FrameworkElement CreateMultibandCompressorWorkbench()
	{
		if (_multibandCompressorWorkbench != null)
		{
			RefreshMultibandTheme(IsDspDark);
			RefreshMultibandState();
			return _multibandCompressorWorkbench;
		}

		bool isDark = IsDspDark;
		_mbPrimaryTextBlocks.Clear();
		_mbSecondaryTextBlocks.Clear();
		_mbMutedTextBlocks.Clear();
		_mbPresetButtons.Clear();
		_mbBandSelectorButtons.Clear();

		var sv = new ScrollViewer
		{
			VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
			HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled
		};

		var root = new Grid
		{
			Background = CreateMbSurfaceBrush(LiquidGlassVariant.Clear, isDark, Math.Clamp(0.72 / 0.85, 0.55, 1.0)),
			Padding = new Thickness(28, 22, 28, 26),
			RowSpacing = 16
		};
		_mbRootGrid = root;

		root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // 0: Top Header & Status Bar
		root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // 1: Power & Band Tabs
		root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // 2: Audition & Max Gain Reduction Readout
		root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // 3: Status Details & Crossover Header
		root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // 4: Central Spectrum Crossover Card
		root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // 5: Bottom 3-Column Control Panels

		// 0: Top Header & Status Bar
		root.Children.Add(CreateMultibandTopHeader());

		// 1: Power & Band Tabs
		var actionRow = CreateMultibandActionRow();
		Grid.SetRow(actionRow, 1);
		root.Children.Add(actionRow);

		// 2: Audition & Max Gain Reduction Readout
		var auditionRow = CreateMultibandAuditionRow();
		Grid.SetRow(auditionRow, 2);
		root.Children.Add(auditionRow);

		// 3: Status Details & Crossover Header
		var statusRow = CreateMultibandStatusRow();
		Grid.SetRow(statusRow, 3);
		root.Children.Add(statusRow);

		// 4: Central Spectrum Crossover Card
		var crossoverCard = CreateMultibandCrossoverCard();
		Grid.SetRow(crossoverCard, 4);
		root.Children.Add(crossoverCard);

		// 5: Bottom 3-Column Control Panels
		var bottomGrid = CreateMultibandBottomGrid();
		Grid.SetRow(bottomGrid, 5);
		root.Children.Add(bottomGrid);

		sv.Content = root;
		_multibandCompressorWorkbench = new Border
		{
			Child = sv,
			RequestedTheme = isDark ? ElementTheme.Dark : ElementTheme.Light
		};

		// Start real-time 30fps dynamic metering timer
		StartMultibandMeterTimer();

		// Initial sync
		RefreshMultibandTheme(isDark);
		RefreshMultibandState();

		return _multibandCompressorWorkbench;
	}

	#region 0. Top Header

	private FrameworkElement CreateMultibandTopHeader()
	{
		bool isDark = IsDspDark;
		var headerGrid = new Grid();
		headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.0, GridUnitType.Star) });
		headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

		// Left Title with Layered/Stack Wave Icon
		var titleSp = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 12, VerticalAlignment = VerticalAlignment.Center };
		_mbTitleIcon = new FontIcon
		{
			Glyph = "\uE81E", // Stacked discs / layers
			FontSize = 18,
			Foreground = GetMbAccentTextBrush(isDark),
			HorizontalAlignment = HorizontalAlignment.Center,
			VerticalAlignment = VerticalAlignment.Center
		};
		_mbTitleIconBorder = new Border
		{
			Width = 36,
			Height = 36,
			CornerRadius = new CornerRadius(18),
			Background = GetMbAccentBgBrush(isDark),
			BorderBrush = GetMbAccentBorderBrush(isDark),
			BorderThickness = new Thickness(1),
			Child = _mbTitleIcon
		};
		titleSp.Children.Add(_mbTitleIconBorder);
		var titleTb = new TextBlock
		{
			Text = "三频段压缩",
			FontSize = 22,
			FontWeight = FontWeights.Bold,
			Foreground = GetMbPrimaryTextBrush(isDark),
			VerticalAlignment = VerticalAlignment.Center
		};
		_mbPrimaryTextBlocks.Add(titleTb);
		titleSp.Children.Add(titleTb);
		Grid.SetColumn(titleSp, 0);
		headerGrid.Children.Add(titleSp);

		return headerGrid;
	}

	#endregion

	#region 1. Power Action & Band Tabs

	private FrameworkElement CreateMultibandActionRow()
	{
		bool isDark = IsDspDark;
		var rowGrid = new Grid();
		rowGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.0, GridUnitType.Star) });
		rowGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

		// Left: Power Toggle + Restore Defaults
		var leftSp = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 10, VerticalAlignment = VerticalAlignment.Center };

		_mbPowerIcon = new FontIcon { Glyph = "\uE7E8", FontSize = 14, Foreground = GetMbMutedTextBrush(isDark) };
		_mbPowerText = new TextBlock { Text = "多段压缩旁路", FontSize = 13, FontWeight = FontWeights.SemiBold, Foreground = GetMbSecondaryTextBrush(isDark) };
		var powerSp = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8, VerticalAlignment = VerticalAlignment.Center };
		powerSp.Children.Add(_mbPowerIcon);
		powerSp.Children.Add(_mbPowerText);

		_mbPowerButton = new Button
		{
			Content = powerSp,
			Padding = new Thickness(12, 6, 14, 6),
			CornerRadius = new CornerRadius(8),
			BorderThickness = new Thickness(1)
		};
		UpdateMultibandPowerButtonVisual(ViewModel?.FurinaMultibandEnabled ?? false, isDark);
		_mbPowerButton.Click += (s, e) =>
		{
			bool enabled = !(ViewModel?.FurinaMultibandEnabled ?? false);
			if (ViewModel != null) ViewModel.FurinaMultibandEnabled = enabled;
			var mb = ViewModel?.AudioEngine?.DspChain?.Multiband;
			if (mb != null) mb.Enabled = enabled;
			ViewModel?.SaveEchoSettings();
			UpdateMultibandPowerButtonVisual(enabled);
			UpdateMultibandStatusVisual(enabled);
		};
		leftSp.Children.Add(_mbPowerButton);

		_mbRestoreDefaultIcon = new FontIcon { Glyph = "\uE7A7", FontSize = 13, Foreground = GetMbMutedTextBrush(isDark) };
		_mbRestoreDefaultText = new TextBlock { Text = "恢复默认", FontSize = 13, Foreground = GetMbSubHeaderTextBrush(isDark) };
		_mbRestoreDefaultBtn = new Button
		{
			Content = new StackPanel
			{
				Orientation = Orientation.Horizontal,
				Spacing = 6,
				Children =
				{
					_mbRestoreDefaultIcon,
					_mbRestoreDefaultText
				}
			},
			Padding = new Thickness(10, 6, 12, 6),
			CornerRadius = new CornerRadius(8),
			BorderThickness = new Thickness(1)
		};
		ApplyMultibandRestoreDefaultTheme(isDark);
		_mbRestoreDefaultBtn.Click += (s, e) =>
		{
			var mb = ViewModel?.AudioEngine?.DspChain?.Multiband;
			if (mb != null)
			{
				mb.ApplyPreset("default");
				SyncViewModelFromProcessor(mb);
			}
			RefreshMultibandState();
		};
		leftSp.Children.Add(_mbRestoreDefaultBtn);
		Grid.SetColumn(leftSp, 0);
		rowGrid.Children.Add(leftSp);

		// Right: Band Switcher [低频] [中频] [高频]
		_mbBandSwitcherBorder = new Border
		{
			Background = GetMbSoftSurfaceBrush(isDark),
			BorderBrush = CreateMbRimBrush(isDark),
			BorderThickness = new Thickness(1),
			CornerRadius = new CornerRadius(8),
			Padding = new Thickness(2)
		};
		var bandTabsSp = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 2 };
		string[] bandNames = new[] { "低频", "中频", "高频" };
		_mbBandSelectorButtons.Clear();

		for (int i = 0; i < 3; i++)
		{
			int bandIdx = i;
			var btn = new Button
			{
				Content = bandNames[i],
				FontSize = 13,
				Padding = new Thickness(14, 5, 14, 5),
				CornerRadius = new CornerRadius(6),
				Tag = bandIdx
			};
			btn.Click += (s, e) => SetSelectedBand(bandIdx);
			_mbBandSelectorButtons.Add(btn);
			bandTabsSp.Children.Add(btn);
		}
		_mbBandSwitcherBorder.Child = bandTabsSp;
		Grid.SetColumn(_mbBandSwitcherBorder, 1);
		rowGrid.Children.Add(_mbBandSwitcherBorder);

		UpdateBandTabVisuals(isDark);
		return rowGrid;
	}

	#endregion

	#region 2. Audition Presets & Max Gain Reduction Readout

	private FrameworkElement CreateMultibandAuditionRow()
	{
		bool isDark = IsDspDark;
		var rowGrid = new Grid();
		rowGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.0, GridUnitType.Star) });
		rowGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

		// Left: Audition Pills and Subtext
		var leftSp = new StackPanel { Spacing = 6, VerticalAlignment = VerticalAlignment.Center };
		var pillsRow = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8, VerticalAlignment = VerticalAlignment.Center };

		var auditionTitleTb = new TextBlock
		{
			Text = "快捷试听",
			FontSize = 13,
			FontWeight = FontWeights.SemiBold,
			Foreground = GetMbSecondaryTextBrush(isDark),
			VerticalAlignment = VerticalAlignment.Center
		};
		_mbSecondaryTextBlocks.Add(auditionTitleTb);
		pillsRow.Children.Add(auditionTitleTb);

		_mbPresetButtons.Clear();
		_mbPresetGentleBtn = CreateAuditionPresetButton("轻柔均衡", "gentle", isDark);
		_mbPresetBassBtn = CreateAuditionPresetButton("低频收紧", "bass", isDark);
		_mbPresetStrongBtn = CreateAuditionPresetButton("明显压缩", "strong", isDark);

		pillsRow.Children.Add(_mbPresetGentleBtn);
		pillsRow.Children.Add(_mbPresetBassBtn);
		pillsRow.Children.Add(_mbPresetStrongBtn);
		leftSp.Children.Add(pillsRow);

		var auditionHintTb = new TextBlock
		{
			Text = "听不出变化？试试“明显压缩”，再用上方开关对比原声。点击预设会开启压缩并替换本模块参数。",
			FontSize = 12,
			Foreground = GetMbMutedTextBrush(isDark),
			TextWrapping = TextWrapping.Wrap
		};
		_mbMutedTextBlocks.Add(auditionHintTb);
		leftSp.Children.Add(auditionHintTb);

		Grid.SetColumn(leftSp, 0);
		rowGrid.Children.Add(leftSp);

		// Right: Giant Gain Reduction Readout
		var rightSp = new StackPanel { HorizontalAlignment = HorizontalAlignment.Right, VerticalAlignment = VerticalAlignment.Center };
		var numberRow = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 3, HorizontalAlignment = HorizontalAlignment.Right };

		_mbMaxGrNumberTb = new TextBlock
		{
			Text = "0.0",
			FontSize = 38,
			FontWeight = FontWeights.Bold,
			Foreground = GetMbPrimaryTextBrush(isDark),
			VerticalAlignment = VerticalAlignment.Bottom
		};
		_mbPrimaryTextBlocks.Add(_mbMaxGrNumberTb);
		_mbMaxGrSubTb = new TextBlock
		{
			Text = "dB",
			FontSize = 14,
			FontWeight = FontWeights.SemiBold,
			Foreground = GetMbMutedTextBrush(isDark),
			Margin = new Thickness(0, 0, 0, 8),
			VerticalAlignment = VerticalAlignment.Bottom
		};
		_mbMutedTextBlocks.Add(_mbMaxGrSubTb);
		numberRow.Children.Add(_mbMaxGrNumberTb);
		numberRow.Children.Add(_mbMaxGrSubTb);
		rightSp.Children.Add(numberRow);

		var grLabelTb = new TextBlock
		{
			Text = "最大增益衰减：当前没有压缩",
			FontSize = 12,
			Foreground = GetMbMutedTextBrush(isDark),
			HorizontalAlignment = HorizontalAlignment.Right
		};
		_mbMutedTextBlocks.Add(grLabelTb);
		rightSp.Children.Add(grLabelTb);
		Grid.SetColumn(rightSp, 1);
		rowGrid.Children.Add(rightSp);

		return rowGrid;
	}

	private Button CreateAuditionPresetButton(string label, string presetId, bool isDark)
	{
		var btn = new Button
		{
			Content = label,
			FontSize = 12,
			Padding = new Thickness(10, 4, 10, 4),
			CornerRadius = new CornerRadius(14),
			BorderThickness = new Thickness(1)
		};
		ApplyMultibandSurfaceButtonTheme(btn, isDark, GetMbSecondaryTextBrush(isDark));
		_mbPresetButtons.Add(btn);
		btn.Click += (s, e) =>
		{
			var mb = ViewModel?.AudioEngine?.DspChain?.Multiband;
			if (mb != null)
			{
				mb.ApplyPreset(presetId);
				SyncViewModelFromProcessor(mb);
			}
			RefreshMultibandState();
		};
		return btn;
	}

	#endregion

	#region 3. Status Row & Crossover Section Title

	private FrameworkElement CreateMultibandStatusRow()
	{
		bool isDark = IsDspDark;
		var rowGrid = new Grid { Margin = new Thickness(0, 4, 0, -4) };
		rowGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.0, GridUnitType.Star) });
		rowGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

		// Left: Status dot + status text
		var leftSp = new StackPanel { Spacing = 3, VerticalAlignment = VerticalAlignment.Center };
		var statusHeaderSp = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 6, VerticalAlignment = VerticalAlignment.Center };

		_mbStatusDot = new TextBlock
		{
			Text = "●",
			FontSize = 12,
			Foreground = GetMbMutedTextBrush(isDark),
			VerticalAlignment = VerticalAlignment.Center
		};
		_mbStatusTitle = new TextBlock
		{
			Text = "已旁路",
			FontSize = 13,
			FontWeight = FontWeights.Bold,
			Foreground = GetMbSecondaryTextBrush(isDark),
			VerticalAlignment = VerticalAlignment.Center
		};
		statusHeaderSp.Children.Add(_mbStatusDot);
		statusHeaderSp.Children.Add(_mbStatusTitle);
		leftSp.Children.Add(statusHeaderSp);

		_mbStatusDetail = new TextBlock
		{
			Text = "三段都直接通过。打开之后，只有超过各自阈值的频段会被压缩。",
			FontSize = 12,
			Foreground = GetMbMutedTextBrush(isDark)
		};
		_mbMutedTextBlocks.Add(_mbStatusDetail);
		leftSp.Children.Add(_mbStatusDetail);
		Grid.SetColumn(leftSp, 0);
		rowGrid.Children.Add(leftSp);

		// Right: "拖动分频线改变分频点，点击频段编辑"
		var hintTb = new TextBlock
		{
			Text = "拖动分频线改变分频点，点击频段编辑",
			FontSize = 12,
			Foreground = GetMbMutedTextBrush(isDark),
			VerticalAlignment = VerticalAlignment.Bottom
		};
		_mbMutedTextBlocks.Add(hintTb);
		Grid.SetColumn(hintTb, 1);
		rowGrid.Children.Add(hintTb);

		return rowGrid;
	}

	#endregion

	#region 4. Central Hero Crossover Spectrum Card (三频段动态色块分频图)

	private FrameworkElement CreateMultibandCrossoverCard()
	{
		bool isDark = IsDspDark;
		_mbCrossoverCardBorder = new Border
		{
			Height = 150,
			CornerRadius = new CornerRadius(12),
			Background = GetMbSoftSurfaceBrush(isDark),
			BorderBrush = CreateMbRimBrush(isDark),
			BorderThickness = new Thickness(1),
			Clip = new RectangleGeometry { Rect = new Rect(0, 0, 2000, 150) }
		};

		_mbPlotCanvas = new Canvas
		{
			Height = 150,
			HorizontalAlignment = HorizontalAlignment.Stretch,
			VerticalAlignment = VerticalAlignment.Stretch
		};
		_mbPlotCanvas.SizeChanged += (s, e) => UpdateMultibandPlotLayout();

		// Region 0: Low Band
		_mbBandRegionLow = CreateBandRegionBorder(0, "低频", isDark, out _mbRegionLowGrTb);
		_mbBandRegionLow.PointerPressed += (s, e) => SetSelectedBand(0);
		_mbPlotCanvas.Children.Add(_mbBandRegionLow);

		// Region 1: Mid Band
		_mbBandRegionMid = CreateBandRegionBorder(1, "中频", isDark, out _mbRegionMidGrTb);
		_mbBandRegionMid.PointerPressed += (s, e) => SetSelectedBand(1);
		_mbPlotCanvas.Children.Add(_mbBandRegionMid);

		// Region 2: High Band
		_mbBandRegionHigh = CreateBandRegionBorder(2, "高频", isDark, out _mbRegionHighGrTb);
		_mbBandRegionHigh.PointerPressed += (s, e) => SetSelectedBand(2);
		_mbPlotCanvas.Children.Add(_mbBandRegionHigh);

		// Divider 1 (Low / Mid crossover): Line, handle, label
		var dividerBrush = GetMbDividerLineBrush(isDark);
		_mbDividerLowLine = new Line
		{
			Stroke = dividerBrush,
			StrokeThickness = 2.0,
			Y1 = 0,
			Y2 = 100
		};
		_mbDividerLowHandle = CreateDraggableHandle("low");
		_mbDividerLowHzTb = new TextBlock
		{
			Text = "150 Hz",
			FontSize = 22,
			FontWeight = FontWeights.Bold,
			Foreground = GetMbPrimaryTextBrush(isDark),
			IsHitTestVisible = false
		};
		_mbPrimaryTextBlocks.Add(_mbDividerLowHzTb);
		_mbPlotCanvas.Children.Add(_mbDividerLowLine);
		_mbPlotCanvas.Children.Add(_mbDividerLowHandle);
		_mbPlotCanvas.Children.Add(_mbDividerLowHzTb);

		// Divider 2 (Mid / High crossover): Line, handle, label
		_mbDividerHighLine = new Line
		{
			Stroke = dividerBrush,
			StrokeThickness = 2.0,
			Y1 = 0,
			Y2 = 100
		};
		_mbDividerHighHandle = CreateDraggableHandle("high");
		_mbDividerHighHzTb = new TextBlock
		{
			Text = "2.5 kHz",
			FontSize = 22,
			FontWeight = FontWeights.Bold,
			Foreground = GetMbPrimaryTextBrush(isDark),
			IsHitTestVisible = false
		};
		_mbPrimaryTextBlocks.Add(_mbDividerHighHzTb);
		_mbPlotCanvas.Children.Add(_mbDividerHighLine);
		_mbPlotCanvas.Children.Add(_mbDividerHighHandle);
		_mbPlotCanvas.Children.Add(_mbDividerHighHzTb);

		_mbCrossoverCardBorder.Child = _mbPlotCanvas;
		return _mbCrossoverCardBorder;
	}

	private Border CreateBandRegionBorder(int bandIdx, string title, bool isDark, out TextBlock grTb)
	{
		var b = new Border
		{
			CornerRadius = new CornerRadius(0),
			Background = GetMbSoftSurfaceBrush(isDark),
			Padding = new Thickness(10, 16, 10, 10)
		};

		var sp = new StackPanel
		{
			HorizontalAlignment = HorizontalAlignment.Center,
			VerticalAlignment = VerticalAlignment.Center,
			Spacing = 6
		};

		var titleTb = new TextBlock
		{
			Text = title,
			FontSize = 24,
			FontWeight = FontWeights.Bold,
			Foreground = GetMbPrimaryTextBrush(isDark),
			HorizontalAlignment = HorizontalAlignment.Center
		};
		_mbBandRegionTitleTbs[bandIdx] = titleTb;

		grTb = new TextBlock
		{
			Text = "0.0 dB",
			FontSize = 18,
			FontWeight = FontWeights.Bold,
			Foreground = GetMbSecondaryTextBrush(isDark),
			HorizontalAlignment = HorizontalAlignment.Center
		};

		sp.Children.Add(titleTb);
		sp.Children.Add(grTb);
		b.Child = sp;
		return b;
	}

	private Border CreateDraggableHandle(string dividerId)
	{
		var handle = new Border
		{
			Width = 24,
			Height = 100,
			Background = new SolidColorBrush(Colors.Transparent)
		};

		handle.PointerPressed += (s, e) =>
		{
			_draggingDivider = dividerId;
			handle.CapturePointer(e.Pointer);
			e.Handled = true;
		};

		handle.PointerMoved += (s, e) =>
		{
			if (_draggingDivider != dividerId || _mbPlotCanvas == null) return;
			var pt = e.GetCurrentPoint(_mbPlotCanvas);
			double w = _mbPlotCanvas.ActualWidth;
			if (w <= 50) return;

			double norm = Math.Clamp(pt.Position.X / w, 0.05, 0.95);
			// Logarithmic frequency conversion: min 20Hz, max 20000Hz
			double hz = 20.0 * Math.Pow(20000.0 / 20.0, norm);

			var mb = ViewModel?.AudioEngine?.DspChain?.Multiband;
			if (dividerId == "low")
			{
				float newLow = (float)Math.Clamp(Math.Round(hz), 40.0, 500.0);
				if (mb != null) mb.LowCrossoverHz = newLow;
				if (ViewModel != null) ViewModel.FurinaMultibandLowCrossoverHz = newLow;
				if (_mbMasterLowCrossSlider != null) _mbMasterLowCrossSlider.Value = newLow;
				if (_mbMasterLowCrossValueTb != null) _mbMasterLowCrossValueTb.Text = $"{newLow:F0} Hz";
			}
			else
			{
				float newHigh = (float)Math.Clamp(Math.Round(hz / 10.0) * 10.0, 800.0, 8000.0);
				if (mb != null) mb.HighCrossoverHz = newHigh;
				if (ViewModel != null) ViewModel.FurinaMultibandHighCrossoverHz = newHigh;
				if (_mbMasterHighCrossSlider != null) _mbMasterHighCrossSlider.Value = newHigh;
				if (_mbMasterHighCrossValueTb != null) _mbMasterHighCrossValueTb.Text = $"{newHigh:F0} Hz";
			}

			UpdateMultibandPlotLayout();
			e.Handled = true;
		};

		handle.PointerReleased += (s, e) =>
		{
			_draggingDivider = null;
			handle.ReleasePointerCapture(e.Pointer);
			ViewModel?.SaveEchoSettings();
			e.Handled = true;
		};

		return handle;
	}

	private void UpdateMultibandPlotLayout()
	{
		if (_mbPlotCanvas == null || _mbPlotCanvas.ActualWidth < 100) return;

		double w = _mbPlotCanvas.ActualWidth;
		double h = 100.0; // Region content height

		double lowHz = ViewModel?.FurinaMultibandLowCrossoverHz ?? 150.0;
		double highHz = ViewModel?.FurinaMultibandHighCrossoverHz ?? 2500.0;

		// Log scale mapping across 20Hz to 20,000Hz
		double logRange = Math.Log(20000.0 / 20.0);
		double normLow = Math.Clamp(Math.Log(Math.Max(20.0, lowHz) / 20.0) / logRange, 0.05, 0.85);
		double normHigh = Math.Clamp(Math.Log(Math.Max(20.0, highHz) / 20.0) / logRange, normLow + 0.05, 0.95);

		double xLow = w * normLow;
		double xHigh = w * normHigh;

		// Low region
		if (_mbBandRegionLow != null)
		{
			Canvas.SetLeft(_mbBandRegionLow, 0);
			Canvas.SetTop(_mbBandRegionLow, 0);
			_mbBandRegionLow.Width = Math.Max(0, xLow);
			_mbBandRegionLow.Height = h;
		}

		// Mid region
		if (_mbBandRegionMid != null)
		{
			Canvas.SetLeft(_mbBandRegionMid, xLow);
			Canvas.SetTop(_mbBandRegionMid, 0);
			_mbBandRegionMid.Width = Math.Max(0, xHigh - xLow);
			_mbBandRegionMid.Height = h;
		}

		// High region
		if (_mbBandRegionHigh != null)
		{
			Canvas.SetLeft(_mbBandRegionHigh, xHigh);
			Canvas.SetTop(_mbBandRegionHigh, 0);
			_mbBandRegionHigh.Width = Math.Max(0, w - xHigh);
			_mbBandRegionHigh.Height = h;
		}

		// Dividers
		if (_mbDividerLowLine != null)
		{
			_mbDividerLowLine.X1 = xLow;
			_mbDividerLowLine.X2 = xLow;
		}
		if (_mbDividerLowHandle != null)
		{
			Canvas.SetLeft(_mbDividerLowHandle, xLow - 12);
			Canvas.SetTop(_mbDividerLowHandle, 0);
		}
		if (_mbDividerLowHzTb != null)
		{
			_mbDividerLowHzTb.Text = FormatHz(lowHz);
			Canvas.SetLeft(_mbDividerLowHzTb, Math.Max(10, xLow - 38));
			Canvas.SetTop(_mbDividerLowHzTb, 108);
		}

		if (_mbDividerHighLine != null)
		{
			_mbDividerHighLine.X1 = xHigh;
			_mbDividerHighLine.X2 = xHigh;
		}
		if (_mbDividerHighHandle != null)
		{
			Canvas.SetLeft(_mbDividerHighHandle, xHigh - 12);
			Canvas.SetTop(_mbDividerHighHandle, 0);
		}
		if (_mbDividerHighHzTb != null)
		{
			_mbDividerHighHzTb.Text = FormatHz(highHz);
			Canvas.SetLeft(_mbDividerHighHzTb, Math.Min(w - 75, xHigh - 38));
			Canvas.SetTop(_mbDividerHighHzTb, 108);
		}

		UpdateBandRegionHighlight();
	}

	private static string FormatHz(double hz)
	{
		if (hz >= 1000.0)
		{
			double khz = Math.Round(hz / 100.0) / 10.0;
			return $"{khz:F1} kHz";
		}
		return $"{Math.Round(hz)} Hz";
	}

	private void UpdateBandRegionHighlight(bool? isDarkOverride = null)
	{
		bool isDark = isDarkOverride ?? IsDspDark;

		var selectedBrush = new SolidColorBrush(isDark
			? Color.FromArgb(105, 99, 102, 241)
			: Color.FromArgb(200, 224, 231, 255));
		var unselectedBrush = new SolidColorBrush(isDark
			? Color.FromArgb(35, 15, 23, 42)
			: Color.FromArgb(70, 248, 250, 252));

		if (_mbBandRegionLow != null)
			_mbBandRegionLow.Background = _multibandSelectedBand == 0 ? selectedBrush : unselectedBrush;
		if (_mbBandRegionMid != null)
			_mbBandRegionMid.Background = _multibandSelectedBand == 1 ? selectedBrush : unselectedBrush;
		if (_mbBandRegionHigh != null)
			_mbBandRegionHigh.Background = _multibandSelectedBand == 2 ? selectedBrush : unselectedBrush;

		Brush primaryText = GetMbPrimaryTextBrush(isDark);
		Brush secondaryText = GetMbSecondaryTextBrush(isDark);
		Brush selectedTitleText = new SolidColorBrush(isDark ? Colors.White : Color.FromArgb(255, 30, 27, 75));
		Brush selectedGrText = new SolidColorBrush(isDark ? Color.FromArgb(255, 224, 231, 255) : Color.FromArgb(255, 67, 56, 202));

		for (int i = 0; i < _mbBandRegionTitleTbs.Length; i++)
		{
			var tb = _mbBandRegionTitleTbs[i];
			if (tb != null)
			{
				tb.Foreground = i == _multibandSelectedBand ? selectedTitleText : primaryText;
			}
		}

		if (_mbRegionLowGrTb != null)
			_mbRegionLowGrTb.Foreground = _multibandSelectedBand == 0 ? selectedGrText : secondaryText;
		if (_mbRegionMidGrTb != null)
			_mbRegionMidGrTb.Foreground = _multibandSelectedBand == 1 ? selectedGrText : secondaryText;
		if (_mbRegionHighGrTb != null)
			_mbRegionHighGrTb.Foreground = _multibandSelectedBand == 2 ? selectedGrText : secondaryText;
	}

	#endregion

	#region 5. Bottom 3-Column Control Panels

	private FrameworkElement CreateMultibandBottomGrid()
	{
		var grid = new Grid { ColumnSpacing = 24 };
		grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.0, GridUnitType.Star) }); // Col 0: 整机
		grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.0, GridUnitType.Star) }); // Col 1: 频段动态
		grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.0, GridUnitType.Star) }); // Col 2: 快慢

		// Col 0: 整机
		var masterCol = CreateMasterColumn();
		Grid.SetColumn(masterCol, 0);
		grid.Children.Add(masterCol);

		// Col 1: 当前频段动态 (中频 / 低频 / 高频)
		var bandCol = CreateBandDynamicsColumn();
		Grid.SetColumn(bandCol, 1);
		grid.Children.Add(bandCol);

		// Col 2: 快慢
		var speedCol = CreateSpeedColumn();
		Grid.SetColumn(speedCol, 2);
		grid.Children.Add(speedCol);

		return grid;
	}

	private FrameworkElement CreateMasterColumn()
	{
		bool isDark = IsDspDark;
		var sp = new StackPanel { Spacing = 14 };

		// Header: "整机" + "分频点把频谱切开"
		var headerGrid = new Grid();
		headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.0, GridUnitType.Star) });
		headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

		var titleTb = new TextBlock
		{
			Text = "整机",
			FontSize = 14,
			FontWeight = FontWeights.Bold,
			Foreground = GetMbPrimaryTextBrush(isDark)
		};
		_mbPrimaryTextBlocks.Add(titleTb);
		var subTb = new TextBlock
		{
			Text = "分频点把频谱切开",
			FontSize = 11,
			Foreground = GetMbMutedTextBrush(isDark),
			VerticalAlignment = VerticalAlignment.Center
		};
		_mbMutedTextBlocks.Add(subTb);
		Grid.SetColumn(titleTb, 0);
		Grid.SetColumn(subTb, 1);
		headerGrid.Children.Add(titleTb);
		headerGrid.Children.Add(subTb);
		sp.Children.Add(headerGrid);

		// Control 1: 低频分频 (40..500 Hz)
		sp.Children.Add(CreateSliderControl("低频分频", 40, 500, 150, 1, out _mbMasterLowCrossSlider, out _mbMasterLowCrossValueTb, " Hz", v =>
		{
			if (ViewModel != null) ViewModel.FurinaMultibandLowCrossoverHz = v;
			var mb = ViewModel?.AudioEngine?.DspChain?.Multiband;
			if (mb != null) mb.LowCrossoverHz = (float)v;
			UpdateMultibandPlotLayout();
			ViewModel?.SaveEchoSettings();
		}));

		// Control 2: 高频分频 (800..8000 Hz)
		sp.Children.Add(CreateSliderControl("高频分频", 800, 8000, 2500, 10, out _mbMasterHighCrossSlider, out _mbMasterHighCrossValueTb, " Hz", v =>
		{
			if (ViewModel != null) ViewModel.FurinaMultibandHighCrossoverHz = v;
			var mb = ViewModel?.AudioEngine?.DspChain?.Multiband;
			if (mb != null) mb.HighCrossoverHz = (float)v;
			UpdateMultibandPlotLayout();
			ViewModel?.SaveEchoSettings();
		}));

		// Control 3: 湿声 (0..100 %)
		sp.Children.Add(CreateSliderControl("湿声", 0, 100, 100, 1, out _mbMasterMixSlider, out _mbMasterMixValueTb, " %", v =>
		{
			float mix = (float)(v / 100.0);
			if (ViewModel != null) ViewModel.FurinaMultibandMix = mix;
			var mb = ViewModel?.AudioEngine?.DspChain?.Multiband;
			if (mb != null) mb.Mix = mix;
			ViewModel?.SaveEchoSettings();
		}));

		return sp;
	}

	private FrameworkElement CreateBandDynamicsColumn()
	{
		bool isDark = IsDspDark;
		var sp = new StackPanel { Spacing = 14 };

		// Header: [当前频带名] + [当前GR 0.0 dB]
		var headerGrid = new Grid();
		headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.0, GridUnitType.Star) });
		headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

		_mbBandPanelTitleTb = new TextBlock
		{
			Text = "中频",
			FontSize = 14,
			FontWeight = FontWeights.Bold,
			Foreground = GetMbPrimaryTextBrush(isDark)
		};
		_mbPrimaryTextBlocks.Add(_mbBandPanelTitleTb);
		_mbBandPanelGrTb = new TextBlock
		{
			Text = "0.0 dB",
			FontSize = 12,
			FontWeight = FontWeights.SemiBold,
			Foreground = GetMbMutedTextBrush(isDark),
			VerticalAlignment = VerticalAlignment.Center
		};
		_mbMutedTextBlocks.Add(_mbBandPanelGrTb);
		Grid.SetColumn(_mbBandPanelTitleTb, 0);
		Grid.SetColumn(_mbBandPanelGrTb, 1);
		headerGrid.Children.Add(_mbBandPanelTitleTb);
		headerGrid.Children.Add(_mbBandPanelGrTb);
		sp.Children.Add(headerGrid);

		// Band Enable Toggle Row
		var enableGrid = new Grid();
		enableGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.0, GridUnitType.Star) });
		enableGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

		var labelSp = new StackPanel { Spacing = 2, VerticalAlignment = VerticalAlignment.Center };
		var enableLabel = new TextBlock
		{
			Text = "中频启用",
			FontSize = 13,
			FontWeight = FontWeights.SemiBold,
			Foreground = GetMbPrimaryTextBrush(isDark)
		};
		_mbPrimaryTextBlocks.Add(enableLabel);
		_mbBandEnableStateTb = new TextBlock
		{
			Text = "开",
			FontSize = 11,
			Foreground = GetMbMutedTextBrush(isDark)
		};
		_mbMutedTextBlocks.Add(_mbBandEnableStateTb);
		labelSp.Children.Add(enableLabel);
		labelSp.Children.Add(_mbBandEnableStateTb);
		Grid.SetColumn(labelSp, 0);
		enableGrid.Children.Add(labelSp);

		_mbBandEnableSwitch = new ToggleSwitch
		{
			IsOn = true,
			OnContent = null,
			OffContent = null,
			Foreground = GetMbPrimaryTextBrush(isDark),
			VerticalAlignment = VerticalAlignment.Center
		};
		_mbBandEnableSwitch.Toggled += (s, e) =>
		{
			if (_isUpdatingMultibandUi) return;
			bool isEnabled = _mbBandEnableSwitch.IsOn;
			if (_mbBandEnableStateTb != null) _mbBandEnableStateTb.Text = isEnabled ? "开" : "关";
			SetCurrentBandEnabled(isEnabled);
		};
		Grid.SetColumn(_mbBandEnableSwitch, 1);
		enableGrid.Children.Add(_mbBandEnableSwitch);
		sp.Children.Add(enableGrid);

		// Control 1: 阈值 (-60..0 dB)
		sp.Children.Add(CreateSliderControl("阈值", -60, 0, -18.0, 0.5, out _mbBandThresholdSlider, out _mbBandThresholdValueTb, " dB", v =>
		{
			SetCurrentBandThreshold((float)v);
		}, isDb: true));

		// Control 2: 比率 (1..20 : 1)
		sp.Children.Add(CreateSliderControl("比率", 1.0, 20.0, 2.0, 0.1, out _mbBandRatioSlider, out _mbBandRatioValueTb, " : 1", v =>
		{
			SetCurrentBandRatio((float)v);
		}));

		return sp;
	}

	private FrameworkElement CreateSpeedColumn()
	{
		bool isDark = IsDspDark;
		var sp = new StackPanel { Spacing = 14 };

		// Header: "快慢" + "只作用于中频"
		var headerGrid = new Grid();
		headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.0, GridUnitType.Star) });
		headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

		_mbSpeedTitleTb = new TextBlock
		{
			Text = "快慢",
			FontSize = 14,
			FontWeight = FontWeights.Bold,
			Foreground = GetMbPrimaryTextBrush(isDark)
		};
		_mbPrimaryTextBlocks.Add(_mbSpeedTitleTb);
		_mbSpeedScopeTb = new TextBlock
		{
			Text = "只作用于中频",
			FontSize = 11,
			Foreground = GetMbMutedTextBrush(isDark),
			VerticalAlignment = VerticalAlignment.Center
		};
		_mbMutedTextBlocks.Add(_mbSpeedScopeTb);
		Grid.SetColumn(_mbSpeedTitleTb, 0);
		Grid.SetColumn(_mbSpeedScopeTb, 1);
		headerGrid.Children.Add(_mbSpeedTitleTb);
		headerGrid.Children.Add(_mbSpeedScopeTb);
		sp.Children.Add(headerGrid);

		// Control 1: 启动 (0.1..100 ms)
		sp.Children.Add(CreateSliderControl("启动", 0.1, 100.0, 10.0, 0.1, out _mbBandAttackSlider, out _mbBandAttackValueTb, " ms", v =>
		{
			SetCurrentBandAttack((float)v);
		}));

		// Control 2: 释放 (10..1000 ms)
		sp.Children.Add(CreateSliderControl("释放", 10.0, 1000.0, 120.0, 1.0, out _mbBandReleaseSlider, out _mbBandReleaseValueTb, " ms", v =>
		{
			SetCurrentBandRelease((float)v);
		}));

		return sp;
	}

	private FrameworkElement CreateSliderControl(string label, double min, double max, double defaultVal, double step, out Slider slider, out TextBlock valueTb, string unit, Action<double> onChanged, bool isDb = false)
	{
		bool isDark = IsDspDark;
		var sp = new StackPanel { Spacing = 4 };

		var labelRow = new Grid();
		labelRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.0, GridUnitType.Star) });
		labelRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

		var lbl = new TextBlock
		{
			Text = label,
			FontSize = 13,
			FontWeight = FontWeights.SemiBold,
			Foreground = GetMbPrimaryTextBrush(isDark)
		};
		_mbPrimaryTextBlocks.Add(lbl);
		var val = new TextBlock
		{
			Text = isDb ? $"{defaultVal:F1}{unit}" : (unit.Contains("ms") || unit.Contains(": 1") ? $"{defaultVal:F1}{unit}" : $"{defaultVal:F0}{unit}"),
			FontSize = 13,
			FontWeight = FontWeights.SemiBold,
			Foreground = GetMbPrimaryTextBrush(isDark)
		};
		_mbPrimaryTextBlocks.Add(val);
		Grid.SetColumn(lbl, 0);
		Grid.SetColumn(val, 1);
		labelRow.Children.Add(lbl);
		labelRow.Children.Add(val);
		sp.Children.Add(labelRow);

		var sl = new Slider
		{
			Minimum = min,
			Maximum = max,
			Value = defaultVal,
			StepFrequency = step,
			HorizontalAlignment = HorizontalAlignment.Stretch,
			Margin = new Thickness(0, -2, 0, 0)
		};

		var captureVal = val;
		sl.ValueChanged += (s, e) =>
		{
			if (_isUpdatingMultibandUi) return;
			double v = e.NewValue;
			captureVal.Text = isDb ? $"{v:F1}{unit}" : (unit.Contains("ms") || unit.Contains(": 1") ? $"{v:F1}{unit}" : $"{v:F0}{unit}");
			onChanged(v);
		};

		sp.Children.Add(sl);
		slider = sl;
		valueTb = val;
		return sp;
	}

	#endregion

	#region Band State Management & Switching

	private void SetSelectedBand(int bandIdx)
	{
		if (bandIdx < 0 || bandIdx > 2) return;
		_multibandSelectedBand = bandIdx;

		UpdateBandTabVisuals();
		UpdateBandRegionHighlight();
		LoadSelectedBandDataToInputs();
	}

	private void UpdateBandTabVisuals(bool? isDarkOverride = null)
	{
		bool isDark = isDarkOverride ?? IsDspDark;
		Brush mutedBrush = GetMbMutedTextBrush(isDark);
		Brush primaryBrush = GetMbPrimaryTextBrush(isDark);
		Brush transparentBorder = new SolidColorBrush(Colors.Transparent);

		for (int i = 0; i < _mbBandSelectorButtons.Count; i++)
		{
			var btn = _mbBandSelectorButtons[i];
			bool isSelected = i == _multibandSelectedBand;
			if (isSelected)
			{
				Brush normalBg = new SolidColorBrush(isDark ? Color.FromArgb(95, 59, 130, 246) : Colors.White);
				Brush hoverBg = new SolidColorBrush(isDark ? Color.FromArgb(125, 59, 130, 246) : Color.FromArgb(255, 248, 250, 252));
				Brush pressedBg = new SolidColorBrush(isDark ? Color.FromArgb(145, 37, 99, 235) : Color.FromArgb(255, 241, 245, 249));
				Brush normalBorder = new SolidColorBrush(isDark ? Color.FromArgb(170, 96, 165, 250) : Color.FromArgb(180, 191, 219, 254));
				Brush hoverBorder = new SolidColorBrush(isDark ? Color.FromArgb(220, 147, 197, 253) : Color.FromArgb(255, 147, 197, 253));
				Brush fg = new SolidColorBrush(isDark ? Color.FromArgb(255, 224, 242, 254) : Color.FromArgb(255, 59, 130, 246));
				Brush hoverFg = new SolidColorBrush(isDark ? Colors.White : Color.FromArgb(255, 37, 99, 235));

				ApplyMultibandButtonVisualResources(
					btn,
					normalBg, hoverBg, pressedBg,
					normalBorder, hoverBorder, normalBorder,
					fg, hoverFg, fg,
					new Thickness(1));
				btn.FontWeight = FontWeights.Bold;
			}
			else
			{
				Brush normalBg = new SolidColorBrush(Colors.Transparent);
				Brush hoverBg = new SolidColorBrush(isDark ? Color.FromArgb(40, 255, 255, 255) : Color.FromArgb(140, 255, 255, 255));
				Brush pressedBg = new SolidColorBrush(isDark ? Color.FromArgb(65, 255, 255, 255) : Color.FromArgb(180, 226, 232, 240));

				ApplyMultibandButtonVisualResources(
					btn,
					normalBg, hoverBg, pressedBg,
					transparentBorder, transparentBorder, transparentBorder,
					mutedBrush, primaryBrush, primaryBrush,
					new Thickness(0));
				btn.FontWeight = FontWeights.Normal;
			}
		}
	}

	private void LoadSelectedBandDataToInputs()
	{
		_isUpdatingMultibandUi = true;
		try
		{
			string[] names = new[] { "低频", "中频", "高频" };
			string curName = names[_multibandSelectedBand];

			if (_mbBandPanelTitleTb != null) _mbBandPanelTitleTb.Text = curName;
			if (_mbSpeedScopeTb != null) _mbSpeedScopeTb.Text = $"只作用于{curName}";

			bool isEnabled = true;
			double thresh = -18.0;
			double ratio = 2.0;
			double attack = 10.0;
			double release = 120.0;

			var mb = ViewModel?.AudioEngine?.DspChain?.Multiband;
			if (mb != null && _multibandSelectedBand < mb.Bands.Length)
			{
				var b = mb.Bands[_multibandSelectedBand];
				isEnabled = b.Enabled;
				thresh = b.ThresholdDb;
				ratio = b.Ratio;
				attack = b.AttackMs;
				release = b.ReleaseMs;
			}
			else if (ViewModel != null)
			{
				switch (_multibandSelectedBand)
				{
					case 0:
						isEnabled = ViewModel.FurinaMultibandLowEnabled;
						thresh = ViewModel.FurinaMultibandLowThresholdDb;
						ratio = ViewModel.FurinaMultibandLowRatio;
						attack = ViewModel.FurinaMultibandLowAttackMs;
						release = ViewModel.FurinaMultibandLowReleaseMs;
						break;
					case 1:
						isEnabled = ViewModel.FurinaMultibandMidEnabled;
						thresh = ViewModel.FurinaMultibandMidThresholdDb;
						ratio = ViewModel.FurinaMultibandMidRatio;
						attack = ViewModel.FurinaMultibandMidAttackMs;
						release = ViewModel.FurinaMultibandMidReleaseMs;
						break;
					case 2:
						isEnabled = ViewModel.FurinaMultibandHighEnabled;
						thresh = ViewModel.FurinaMultibandHighThresholdDb;
						ratio = ViewModel.FurinaMultibandHighRatio;
						attack = ViewModel.FurinaMultibandHighAttackMs;
						release = ViewModel.FurinaMultibandHighReleaseMs;
						break;
				}
			}

			if (_mbBandEnableSwitch != null) _mbBandEnableSwitch.IsOn = isEnabled;
			if (_mbBandEnableStateTb != null) _mbBandEnableStateTb.Text = isEnabled ? "开" : "关";

			if (_mbBandThresholdSlider != null) _mbBandThresholdSlider.Value = thresh;
			if (_mbBandThresholdValueTb != null) _mbBandThresholdValueTb.Text = $"{thresh:F1} dB";

			if (_mbBandRatioSlider != null) _mbBandRatioSlider.Value = ratio;
			if (_mbBandRatioValueTb != null) _mbBandRatioValueTb.Text = $"{ratio:F1} : 1";

			if (_mbBandAttackSlider != null) _mbBandAttackSlider.Value = attack;
			if (_mbBandAttackValueTb != null) _mbBandAttackValueTb.Text = $"{attack:F1} ms";

			if (_mbBandReleaseSlider != null) _mbBandReleaseSlider.Value = release;
			if (_mbBandReleaseValueTb != null) _mbBandReleaseValueTb.Text = $"{release:F0} ms";
		}
		finally
		{
			_isUpdatingMultibandUi = false;
		}
	}

	private void SetCurrentBandEnabled(bool isEnabled)
	{
		var mb = ViewModel?.AudioEngine?.DspChain?.Multiband;
		if (mb != null && _multibandSelectedBand < mb.Bands.Length)
		{
			mb.Bands[_multibandSelectedBand].Enabled = isEnabled;
		}
		if (ViewModel != null)
		{
			switch (_multibandSelectedBand)
			{
				case 0: ViewModel.FurinaMultibandLowEnabled = isEnabled; break;
				case 1: ViewModel.FurinaMultibandMidEnabled = isEnabled; break;
				case 2: ViewModel.FurinaMultibandHighEnabled = isEnabled; break;
			}
			ViewModel.SaveEchoSettings();
		}
	}

	private void SetCurrentBandThreshold(float val)
	{
		var mb = ViewModel?.AudioEngine?.DspChain?.Multiband;
		if (mb != null && _multibandSelectedBand < mb.Bands.Length)
		{
			mb.Bands[_multibandSelectedBand].ThresholdDb = val;
		}
		if (ViewModel != null)
		{
			switch (_multibandSelectedBand)
			{
				case 0: ViewModel.FurinaMultibandLowThresholdDb = val; break;
				case 1: ViewModel.FurinaMultibandMidThresholdDb = val; break;
				case 2: ViewModel.FurinaMultibandHighThresholdDb = val; break;
			}
			ViewModel.SaveEchoSettings();
		}
	}

	private void SetCurrentBandRatio(float val)
	{
		var mb = ViewModel?.AudioEngine?.DspChain?.Multiband;
		if (mb != null && _multibandSelectedBand < mb.Bands.Length)
		{
			mb.Bands[_multibandSelectedBand].Ratio = val;
		}
		if (ViewModel != null)
		{
			switch (_multibandSelectedBand)
			{
				case 0: ViewModel.FurinaMultibandLowRatio = val; break;
				case 1: ViewModel.FurinaMultibandMidRatio = val; break;
				case 2: ViewModel.FurinaMultibandHighRatio = val; break;
			}
			ViewModel.SaveEchoSettings();
		}
	}

	private void SetCurrentBandAttack(float val)
	{
		var mb = ViewModel?.AudioEngine?.DspChain?.Multiband;
		if (mb != null && _multibandSelectedBand < mb.Bands.Length)
		{
			mb.Bands[_multibandSelectedBand].AttackMs = val;
		}
		if (ViewModel != null)
		{
			switch (_multibandSelectedBand)
			{
				case 0: ViewModel.FurinaMultibandLowAttackMs = val; break;
				case 1: ViewModel.FurinaMultibandMidAttackMs = val; break;
				case 2: ViewModel.FurinaMultibandHighAttackMs = val; break;
			}
			ViewModel.SaveEchoSettings();
		}
	}

	private void SetCurrentBandRelease(float val)
	{
		var mb = ViewModel?.AudioEngine?.DspChain?.Multiband;
		if (mb != null && _multibandSelectedBand < mb.Bands.Length)
		{
			mb.Bands[_multibandSelectedBand].ReleaseMs = val;
		}
		if (ViewModel != null)
		{
			switch (_multibandSelectedBand)
			{
				case 0: ViewModel.FurinaMultibandLowReleaseMs = val; break;
				case 1: ViewModel.FurinaMultibandMidReleaseMs = val; break;
				case 2: ViewModel.FurinaMultibandHighReleaseMs = val; break;
			}
			ViewModel.SaveEchoSettings();
		}
	}

	private void SyncViewModelFromProcessor(FurinaPlayerMultibandProcessor mb)
	{
		if (ViewModel == null) return;
		ViewModel.FurinaMultibandEnabled = mb.Enabled;
		ViewModel.FurinaMultibandLowCrossoverHz = mb.LowCrossoverHz;
		ViewModel.FurinaMultibandHighCrossoverHz = mb.HighCrossoverHz;
		ViewModel.FurinaMultibandMix = mb.Mix;
		ViewModel.FurinaMultibandStereoLink = mb.StereoLink;

		ViewModel.FurinaMultibandLowEnabled = mb.Bands[0].Enabled;
		ViewModel.FurinaMultibandLowThresholdDb = mb.Bands[0].ThresholdDb;
		ViewModel.FurinaMultibandLowRatio = mb.Bands[0].Ratio;
		ViewModel.FurinaMultibandLowAttackMs = mb.Bands[0].AttackMs;
		ViewModel.FurinaMultibandLowReleaseMs = mb.Bands[0].ReleaseMs;

		ViewModel.FurinaMultibandMidEnabled = mb.Bands[1].Enabled;
		ViewModel.FurinaMultibandMidThresholdDb = mb.Bands[1].ThresholdDb;
		ViewModel.FurinaMultibandMidRatio = mb.Bands[1].Ratio;
		ViewModel.FurinaMultibandMidAttackMs = mb.Bands[1].AttackMs;
		ViewModel.FurinaMultibandMidReleaseMs = mb.Bands[1].ReleaseMs;

		ViewModel.FurinaMultibandHighEnabled = mb.Bands[2].Enabled;
		ViewModel.FurinaMultibandHighThresholdDb = mb.Bands[2].ThresholdDb;
		ViewModel.FurinaMultibandHighRatio = mb.Bands[2].Ratio;
		ViewModel.FurinaMultibandHighAttackMs = mb.Bands[2].AttackMs;
		ViewModel.FurinaMultibandHighReleaseMs = mb.Bands[2].ReleaseMs;

		ViewModel.SaveEchoSettings();
	}

	#endregion

	#region State Refresh & Real-time Metering

	private void StartMultibandMeterTimer()
	{
		if (_multibandTimer != null) return;
		_multibandTimer = new DispatcherTimer
		{
			Interval = TimeSpan.FromMilliseconds(40) // ~25-30fps smooth metering
		};
		_multibandTimer.Tick += (s, e) => UpdateMultibandMeters();
		_multibandTimer.Start();
	}

	private void UpdateMultibandMeters()
	{
		var mb = ViewModel?.AudioEngine?.DspChain?.Multiband;
		if (mb == null) return;

		float lowGr = mb.Bands[0].GainReductionDb;
		float midGr = mb.Bands[1].GainReductionDb;
		float highGr = mb.Bands[2].GainReductionDb;
		float maxGr = mb.GainReductionDb;

		// Update spectrum region text
		if (_mbRegionLowGrTb != null)
			_mbRegionLowGrTb.Text = lowGr < 0.05f ? "0.0 dB" : $"-{lowGr:F1} dB";
		if (_mbRegionMidGrTb != null)
			_mbRegionMidGrTb.Text = midGr < 0.05f ? "0.0 dB" : $"-{midGr:F1} dB";
		if (_mbRegionHighGrTb != null)
			_mbRegionHighGrTb.Text = highGr < 0.05f ? "0.0 dB" : $"-{highGr:F1} dB";

		// Update column 2 band readout
		if (_mbBandPanelGrTb != null)
		{
			float curGr = _multibandSelectedBand switch
			{
				0 => lowGr,
				1 => midGr,
				2 => highGr,
				_ => 0f
			};
			_mbBandPanelGrTb.Text = curGr < 0.05f ? "0.0 dB" : $"-{curGr:F1} dB";
		}

		// Update max gain reduction hero readout
		if (_mbMaxGrNumberTb != null)
		{
			_mbMaxGrNumberTb.Text = maxGr < 0.05f ? "0.0" : $"-{maxGr:F1}";
		}
	}

	private void RefreshMultibandState()
	{
		bool isEnabled = ViewModel?.FurinaMultibandEnabled ?? false;
		double lowHz = ViewModel?.FurinaMultibandLowCrossoverHz ?? 150.0;
		double highHz = ViewModel?.FurinaMultibandHighCrossoverHz ?? 2500.0;
		double mix = (ViewModel?.FurinaMultibandMix ?? 1.0) * 100.0;

		UpdateMultibandPowerButtonVisual(isEnabled);
		UpdateMultibandStatusVisual(isEnabled);

		_isUpdatingMultibandUi = true;
		try
		{
			if (_mbMasterLowCrossSlider != null) _mbMasterLowCrossSlider.Value = lowHz;
			if (_mbMasterLowCrossValueTb != null) _mbMasterLowCrossValueTb.Text = $"{lowHz:F0} Hz";

			if (_mbMasterHighCrossSlider != null) _mbMasterHighCrossSlider.Value = highHz;
			if (_mbMasterHighCrossValueTb != null) _mbMasterHighCrossValueTb.Text = $"{highHz:F0} Hz";

			if (_mbMasterMixSlider != null) _mbMasterMixSlider.Value = mix;
			if (_mbMasterMixValueTb != null) _mbMasterMixValueTb.Text = $"{mix:F0} %";

			LoadSelectedBandDataToInputs();
			UpdateMultibandPlotLayout();
		}
		finally
		{
			_isUpdatingMultibandUi = false;
		}
	}

	private void UpdateMultibandPowerButtonVisual(bool isEnabled, bool? isDarkOverride = null)
	{
		bool isDark = isDarkOverride ?? IsDspDark;

		if (_mbPowerButton != null)
		{
			if (isEnabled)
			{
				Brush normalBg = GetMbAccentBgBrush(isDark);
				Brush hoverBg = new SolidColorBrush(isDark ? Color.FromArgb(115, 99, 102, 241) : Color.FromArgb(255, 224, 231, 255));
				Brush pressedBg = new SolidColorBrush(isDark ? Color.FromArgb(135, 79, 70, 229) : Color.FromArgb(255, 199, 210, 254));
				Brush normalBorder = GetMbAccentBorderBrush(isDark);
				Brush hoverBorder = new SolidColorBrush(isDark ? Color.FromArgb(220, 165, 180, 252) : Color.FromArgb(255, 129, 140, 248));
				Brush accentFg = GetMbAccentTextBrush(isDark);
				Brush hoverFg = new SolidColorBrush(isDark ? Colors.White : Color.FromArgb(255, 67, 56, 202));

				ApplyMultibandButtonVisualResources(
					_mbPowerButton,
					normalBg, hoverBg, pressedBg,
					normalBorder, hoverBorder, normalBorder,
					accentFg, hoverFg, accentFg,
					new Thickness(1));
			}
			else
			{
				ApplyMultibandSurfaceButtonTheme(_mbPowerButton, isDark, GetMbSecondaryTextBrush(isDark));
			}
		}

		if (_mbPowerIcon != null)
		{
			_mbPowerIcon.Foreground = isEnabled
				? GetMbAccentTextBrush(isDark)
				: GetMbMutedTextBrush(isDark);
		}
		if (_mbPowerText != null)
		{
			_mbPowerText.Text = isEnabled ? "多段压缩已启用" : "多段压缩旁路";
			_mbPowerText.Foreground = isEnabled
				? GetMbAccentTextBrush(isDark)
				: GetMbSecondaryTextBrush(isDark);
		}
	}

	private void UpdateMultibandStatusVisual(bool isEnabled, bool? isDarkOverride = null)
	{
		bool isDark = isDarkOverride ?? IsDspDark;

		if (_mbStatusDot != null)
		{
			_mbStatusDot.Foreground = isEnabled
				? new SolidColorBrush(isDark ? Color.FromArgb(255, 74, 222, 128) : Color.FromArgb(255, 22, 163, 74))
				: GetMbMutedTextBrush(isDark);
		}
		if (_mbStatusTitle != null)
		{
			_mbStatusTitle.Text = isEnabled ? "已启用" : "已旁路";
			_mbStatusTitle.Foreground = isEnabled ? GetMbPrimaryTextBrush(isDark) : GetMbSecondaryTextBrush(isDark);
		}
		if (_mbStatusDetail != null)
		{
			_mbStatusDetail.Text = isEnabled
				? "三段压缩正全速运作中。超过各自阈值的频段正被动态规整。"
				: "三段都直接通过。打开之后，只有超过各自阈值的频段会被压缩。";
			_mbStatusDetail.Foreground = GetMbMutedTextBrush(isDark);
		}
	}

	private void ApplyMultibandRestoreDefaultTheme(bool isDark)
	{
		Brush subHeaderBrush = GetMbSubHeaderTextBrush(isDark);
		Brush mutedBrush = GetMbMutedTextBrush(isDark);
		ApplyMultibandSurfaceButtonTheme(_mbRestoreDefaultBtn, isDark, subHeaderBrush);
		if (_mbRestoreDefaultIcon != null) _mbRestoreDefaultIcon.Foreground = mutedBrush;
		if (_mbRestoreDefaultText != null) _mbRestoreDefaultText.Foreground = subHeaderBrush;
	}

	private static Brush CreateMbSurfaceBrush(LiquidGlassVariant variant, bool isDark, double opacityScale = 1.0)
	{
		return LiquidGlassEngine.CreateSurfaceBrush(variant, isDark: isDark, opacityScale: opacityScale);
	}

	private static Brush CreateMbRimBrush(bool isDark, bool isHovered = false, bool isSelected = false)
	{
		return LiquidGlassEngine.CreateSpecularRimBrush(
			LiquidGlassVariant.Regular,
			isDark: isDark,
			isHovered: isHovered,
			isSelected: isSelected);
	}

	private static Brush GetMbSoftSurfaceBrush(bool isDark)
	{
		return isDark
			? LiquidGlassEngine.CreateSurfaceBrush(LiquidGlassVariant.Clear, isDark: true, opacityScale: 0.45)
			: new SolidColorBrush(Color.FromArgb(220, 241, 245, 249));
	}

	private static Brush GetMbDividerLineBrush(bool isDark)
	{
		return new SolidColorBrush(isDark ? Color.FromArgb(255, 129, 140, 248) : Color.FromArgb(255, 99, 102, 241));
	}

	private static Brush GetMbPrimaryTextBrush(bool isDark) => new SolidColorBrush(isDark ? Colors.White : Color.FromArgb(255, 15, 23, 42));
	private static Brush GetMbSecondaryTextBrush(bool isDark) => new SolidColorBrush(isDark ? Color.FromArgb(255, 248, 250, 252) : Color.FromArgb(255, 51, 65, 85));
	private static Brush GetMbSubHeaderTextBrush(bool isDark) => new SolidColorBrush(isDark ? Color.FromArgb(255, 226, 232, 240) : Color.FromArgb(255, 71, 85, 105));
	private static Brush GetMbMutedTextBrush(bool isDark) => new SolidColorBrush(isDark ? Color.FromArgb(255, 203, 213, 225) : Color.FromArgb(255, 100, 116, 139));
	private static Brush GetMbAccentBgBrush(bool isDark) => new SolidColorBrush(isDark ? Color.FromArgb(85, 99, 102, 241) : Color.FromArgb(255, 238, 242, 255));
	private static Brush GetMbAccentBorderBrush(bool isDark) => new SolidColorBrush(isDark ? Color.FromArgb(180, 129, 140, 248) : Color.FromArgb(255, 199, 210, 254));
	private static Brush GetMbAccentTextBrush(bool isDark) => new SolidColorBrush(isDark ? Color.FromArgb(255, 199, 210, 254) : Color.FromArgb(255, 79, 70, 229));

	private static void ApplyMultibandButtonVisualResources(
		Button? btn,
		Brush normalBg,
		Brush hoverBg,
		Brush pressedBg,
		Brush normalBorder,
		Brush hoverBorder,
		Brush pressedBorder,
		Brush normalFg,
		Brush hoverFg,
		Brush pressedFg,
		Thickness? borderThickness = null)
	{
		if (btn == null) return;
		btn.Background = normalBg;
		btn.BorderBrush = normalBorder;
		btn.BorderThickness = borderThickness ?? new Thickness(1);
		btn.Foreground = normalFg;

		btn.Resources["ButtonBackground"] = normalBg;
		btn.Resources["ButtonBackgroundPointerOver"] = hoverBg;
		btn.Resources["ButtonBackgroundPressed"] = pressedBg;
		btn.Resources["ButtonBorderBrush"] = normalBorder;
		btn.Resources["ButtonBorderBrushPointerOver"] = hoverBorder;
		btn.Resources["ButtonBorderBrushPressed"] = pressedBorder;
		btn.Resources["ButtonForeground"] = normalFg;
		btn.Resources["ButtonForegroundPointerOver"] = hoverFg;
		btn.Resources["ButtonForegroundPressed"] = pressedFg;
	}

	private static void ApplyMultibandSurfaceButtonTheme(Button? btn, bool isDark, Brush foregroundBrush)
	{
		if (btn == null) return;
		Brush normalBg = CreateMbSurfaceBrush(LiquidGlassVariant.Clear, isDark, 0.92);
		Brush hoverBg = CreateMbSurfaceBrush(LiquidGlassVariant.Regular, isDark, 1.0);
		Brush pressedBg = CreateMbSurfaceBrush(LiquidGlassVariant.Clear, isDark, 0.80);
		Brush normalBorder = CreateMbRimBrush(isDark);
		Brush hoverBorder = CreateMbRimBrush(isDark, isHovered: true);
		Brush hoverFg = GetMbPrimaryTextBrush(isDark);

		ApplyMultibandButtonVisualResources(
			btn,
			normalBg, hoverBg, pressedBg,
			normalBorder, hoverBorder, normalBorder,
			foregroundBrush, hoverFg, foregroundBrush,
			new Thickness(1));
	}

	public void RefreshMultibandTheme(bool isDark)
	{
		if (_multibandCompressorWorkbench == null) return;

		_multibandCompressorWorkbench.RequestedTheme = isDark ? ElementTheme.Dark : ElementTheme.Light;

		Brush primaryText = GetMbPrimaryTextBrush(isDark);
		Brush secondaryText = GetMbSecondaryTextBrush(isDark);
		Brush mutedText = GetMbMutedTextBrush(isDark);
		Brush softBg = GetMbSoftSurfaceBrush(isDark);
		Brush rimBrush = CreateMbRimBrush(isDark);

		if (_mbRootGrid != null)
		{
			_mbRootGrid.Background = CreateMbSurfaceBrush(LiquidGlassVariant.Clear, isDark, Math.Clamp(0.72 / 0.85, 0.55, 1.0));
		}
		if (_mbTitleIconBorder != null)
		{
			_mbTitleIconBorder.Background = GetMbAccentBgBrush(isDark);
			_mbTitleIconBorder.BorderBrush = GetMbAccentBorderBrush(isDark);
		}
		if (_mbTitleIcon != null)
		{
			_mbTitleIcon.Foreground = GetMbAccentTextBrush(isDark);
		}

		foreach (var tb in _mbPrimaryTextBlocks) tb.Foreground = primaryText;
		foreach (var tb in _mbSecondaryTextBlocks) tb.Foreground = secondaryText;
		foreach (var tb in _mbMutedTextBlocks) tb.Foreground = mutedText;

		if (_mbBandSwitcherBorder != null)
		{
			_mbBandSwitcherBorder.Background = softBg;
			_mbBandSwitcherBorder.BorderBrush = rimBrush;
		}
		if (_mbCrossoverCardBorder != null)
		{
			_mbCrossoverCardBorder.Background = softBg;
			_mbCrossoverCardBorder.BorderBrush = rimBrush;
		}

		Brush dividerBrush = GetMbDividerLineBrush(isDark);
		if (_mbDividerLowLine != null) _mbDividerLowLine.Stroke = dividerBrush;
		if (_mbDividerHighLine != null) _mbDividerHighLine.Stroke = dividerBrush;

		if (_mbBandEnableSwitch != null) _mbBandEnableSwitch.Foreground = primaryText;

		ApplyMultibandRestoreDefaultTheme(isDark);
		foreach (var btn in _mbPresetButtons)
		{
			ApplyMultibandSurfaceButtonTheme(btn, isDark, secondaryText);
		}

		bool isEnabled = ViewModel?.FurinaMultibandEnabled ?? false;
		UpdateMultibandPowerButtonVisual(isEnabled, isDark);
		UpdateMultibandStatusVisual(isEnabled, isDark);
		UpdateBandTabVisuals(isDark);
		UpdateBandRegionHighlight(isDark);
	}

	#endregion
}
