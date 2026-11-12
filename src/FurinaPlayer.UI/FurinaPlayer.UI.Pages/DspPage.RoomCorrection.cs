using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using FurinaPlayer.UI.Controls;
using FurinaPlayer.UI.Helpers;
using FurinaPlayer.UI.ViewModels;
using Microsoft.UI;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using SonicWave.Audio.Dsp;
using Windows.Storage;
using Windows.Storage.Pickers;
using WinRT.Interop;

namespace FurinaPlayer.UI.Pages;

public sealed partial class DspPage
{
	private FrameworkElement? _roomCorrectionWorkbench;
	private TextBlock? _roomNavSubTb;
	private Grid? _roomRootGrid;
	private Border? _roomWaveIconBorder;
	private FontIcon? _roomWaveIcon;
	private Border? _roomHeroCard;
	private Border? _roomProBadge;
	private readonly List<Rectangle> _roomDividers = new();
	private readonly List<Border> _roomSignalPathNodes = new();
	private readonly List<FontIcon> _roomSignalPathArrows = new();

	// Hero Card Elements
	private Ellipse? _roomStatusDot;
	private TextBlock? _roomStatusText;
	private StackPanel? _roomActionPanel;
	

	// Signal Path Nodes
	private TextBlock? _roomHostStatusTb;
	private TextBlock? _roomInputChTb;
	private TextBlock? _roomDspBitsTb;
	private TextBlock? _roomConvolverSlotTb;
	private TextBlock? _roomDeviceOutTb;

	// Active IR Controls Card (Visible when IR loaded)
	private Border? _roomActiveIrCard;
	private TextBlock? _roomIrFileNameTb;
	private TextBlock? _roomIrSpecsTb;
	private Button? _roomIrReimportBtn;
	private Button? _roomIrClearBtn;
	private FontIcon? _roomTrimIcon;
	private Slider? _roomTrimSlider;
	private TextBlock? _roomTrimValueTb;
	private readonly List<Button> _roomTrimPresetButtons = new();
	private Border? _roomClippingBanner;
	private FontIcon? _roomClippingIcon;
	private TextBlock? _roomClippingTextTb;
	private readonly List<Button> _roomWorkflowCardButtons = new();

	// REW Guide Collapsible Card
	private Border? _roomRewExpanderCard;
	private StackPanel? _roomRewExpanderBody;
	private FontIcon? _roomRewChevron;
	private Border? _roomRewCodeBox;
	private TextBlock? _roomRewGuideFooterTb;

	public FrameworkElement CreateRoomCorrectionWorkbench()
	{
		if (_roomCorrectionWorkbench != null)
		{
			RefreshRoomCorrectionTheme(IsDspDark);
			return _roomCorrectionWorkbench;
		}

		_roomDividers.Clear();
		_roomSignalPathNodes.Clear();
		_roomSignalPathArrows.Clear();
		_roomWorkflowCardButtons.Clear();

		var sv = new ScrollViewer
		{
			VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
			HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled
		};

		bool isDark = IsDspDark;
		var root = new Grid
		{
			Background = CreateWhiteAcrylicBrush(0.72, 195),
			Padding = new Thickness(24, 20, 24, 24),
			RowSpacing = 16
		};
		_roomRootGrid = root;

		root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // 0: Top Header & Status Bar
		root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // 1: Main Room Correction Card
		root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // 2: Three Quick Navigation Cards
		root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // 3: Collapsible REW Guide Card

		// --- Row 0: Top Header & Status Bar ---
		root.Children.Add(CreateRoomHeaderBar());

		// --- Row 1: Hero Main Room Correction Card ---
		var heroCard = CreateRoomHeroCard();
		Grid.SetRow(heroCard, 1);
		root.Children.Add(heroCard);

		// --- Row 2: Three Quick Navigation Cards ---
		var workflowCards = CreateRoomWorkflowCards();
		Grid.SetRow(workflowCards, 2);
		root.Children.Add(workflowCards);

		// --- Row 3: Collapsible REW Guide Card ---
		var rewGuideCard = CreateRewGuideCard();
		Grid.SetRow(rewGuideCard, 3);
		root.Children.Add(rewGuideCard);

		sv.Content = root;
		_roomCorrectionWorkbench = sv;

		RefreshRoomCorrectionState();

		return _roomCorrectionWorkbench;
	}

	/// <summary>
	/// Top status bar: Wave icon + "FIR / 房间校正", "4 个模块启用", "就绪", "[刷新状态]".
	/// </summary>
	private FrameworkElement CreateRoomHeaderBar()
	{
		var headerGrid = new Grid();
		headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.0, GridUnitType.Star) });
		headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

		// Left Title Row
		var leftStack = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 10, VerticalAlignment = VerticalAlignment.Center };

		_roomWaveIcon = new FontIcon
		{
			Glyph = "\uE774", // Audio wave lines
			FontSize = 16,
			Foreground = DspAccentSoftTextBrush,
			HorizontalAlignment = HorizontalAlignment.Center,
			VerticalAlignment = VerticalAlignment.Center
		};
		_roomWaveIconBorder = new Border
		{
			Width = 32,
			Height = 32,
			CornerRadius = new CornerRadius(8),
			Background = DspAccentSoftBgBrush,
			Child = _roomWaveIcon
		};
		leftStack.Children.Add(_roomWaveIconBorder);

		var titleTb = new TextBlock
		{
			Text = "FIR / 房间校正",
			FontSize = 18,
			FontWeight = FontWeights.Bold,
			Foreground = DspTextPrimaryBrush,
			VerticalAlignment = VerticalAlignment.Center
		};
		leftStack.Children.Add(titleTb);
		Grid.SetColumn(leftStack, 0);
		headerGrid.Children.Add(leftStack);

		return headerGrid;
	}

	/// <summary>
	/// Main Hero Card: Intro header, status dot, action buttons, real signal path, and formats note.
	/// </summary>
	private FrameworkElement CreateRoomHeroCard()
	{
		bool isDark = IsDspDark;
		var card = new Border
		{
			Background = CreateAcrylicCardBrush(0.88, 235),
			BorderBrush = CreateLiquidGlassRimBrush(),
			BorderThickness = new Thickness(1),
			CornerRadius = new CornerRadius(14),
			Padding = new Thickness(24, 20, 24, 22)
		};
		LiquidGlassEngine.AttachInteractiveLiquidGlass(card, LiquidGlassVariant.Regular, isDark: isDark, enablePointerTracking: false);
		_roomHeroCard = card;

		var contentStack = new StackPanel { Spacing = 18 };

		// 1. Top Section: Titles (Left) & Actions (Right)
		var topSection = new Grid();
		topSection.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.0, GridUnitType.Star) });
		topSection.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

		var introStack = new StackPanel { Spacing = 3 };

		// Eyebrow: "空间处理 / IR" + "FurinaPlayer Pro" Badge
		var eyebrowRow = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8, VerticalAlignment = VerticalAlignment.Center };
		eyebrowRow.Children.Add(new TextBlock
		{
			Text = "空间处理 / IR",
			FontSize = 11,
			FontWeight = FontWeights.SemiBold,
			Foreground = DspTextMutedBrush,
			VerticalAlignment = VerticalAlignment.Center
		});
		var proBadge = new Border
		{
			Background = DspSoftSurfaceBrush,
			BorderBrush = CreateLiquidGlassRimBrush(),
			BorderThickness = new Thickness(1),
			CornerRadius = new CornerRadius(4),
			Padding = new Thickness(6, 1, 6, 1),
			VerticalAlignment = VerticalAlignment.Center
		};
		_roomProBadge = proBadge;
		proBadge.Child = new TextBlock
		{
			Text = "FurinaPlayer Pro",
			FontSize = 10,
			FontWeight = FontWeights.Bold,
			Foreground = DspTextPrimaryBrush
		};
		eyebrowRow.Children.Add(proBadge);
		introStack.Children.Add(eyebrowRow);

		// Big Title
		introStack.Children.Add(new TextBlock
		{
			Text = "房间与音箱校正",
			FontSize = 24,
			FontWeight = FontWeights.Bold,
			Foreground = DspTextPrimaryBrush,
			Margin = new Thickness(0, 2, 0, 2)
		});

		// Subtitle
		introStack.Children.Add(new TextBlock
		{
			Text = "导入 REW 或其他测量软件生成的校正滤波器。",
			FontSize = 12,
			Foreground = DspTextMutedBrush
		});

		Grid.SetColumn(introStack, 0);
		topSection.Children.Add(introStack);

		// Top Right: Status dot + Primary Action Buttons
		var actionsStack = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 14, VerticalAlignment = VerticalAlignment.Center };

		var statusDotStack = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 6, VerticalAlignment = VerticalAlignment.Center };
		_roomStatusDot = new Ellipse
		{
			Width = 7,
			Height = 7,
			Fill = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 16, 185, 129)), // Green
			VerticalAlignment = VerticalAlignment.Center
		};
		_roomStatusText = new TextBlock
		{
			Text = "准备音频校正",
			FontSize = 12,
			FontWeight = FontWeights.Medium,
			Foreground = DspTextSubHeaderBrush,
			VerticalAlignment = VerticalAlignment.Center
		};
		statusDotStack.Children.Add(_roomStatusDot);
		statusDotStack.Children.Add(_roomStatusText);
		actionsStack.Children.Add(statusDotStack);

		_roomActionPanel = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8, VerticalAlignment = VerticalAlignment.Center };
		actionsStack.Children.Add(_roomActionPanel);

		Grid.SetColumn(actionsStack, 1);
		topSection.Children.Add(actionsStack);
		contentStack.Children.Add(topSection);

		// Divider
		var heroDivider = new Rectangle
		{
			Height = 1,
			Fill = GetRoomDividerBrush(isDark),
			Margin = new Thickness(0, 4, 0, 4)
		};
		_roomDividers.Add(heroDivider);
		contentStack.Children.Add(heroDivider);

		// 2. Real Signal Path Section
		contentStack.Children.Add(CreateSignalPathSection());

		// 3. Formats Support Footnote
		var formatsNote = new TextBlock
		{
			Text = "WAV · FLAC · AIFF · ALAC(.m4a) · ZIP, ZIP 支持 Convolver .cfg 及其引用的 .pcm / .dbl, 按播放采样率与声道自动匹配。",
			FontSize = 11,
			Foreground = DspTextMutedBrush,
			Margin = new Thickness(0, 2, 0, 2)
		};
		contentStack.Children.Add(formatsNote);

		// 4. Active IR Controls Card (appears when IR is loaded)
		_roomActiveIrCard = CreateActiveIrControlDeck();
		contentStack.Children.Add(_roomActiveIrCard);

		card.Child = contentStack;
		return card;
	}

	/// <summary>
	/// Real Signal Path: 音频输入 (-- / --) ──▶ 内置 DSP (--) ──▶ 卷积插槽 (--) ──▶ 设备输出 (--).
	/// </summary>
	private FrameworkElement CreateSignalPathSection()
	{
		var sectionStack = new StackPanel { Spacing = 12 };

		// Heading line
		var headingGrid = new Grid();
		headingGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.0, GridUnitType.Star) });
		headingGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

		var headingTb = new TextBlock
		{
			Text = "实际信号路径",
			FontSize = 12,
			FontWeight = FontWeights.Bold,
			Foreground = DspTextSecondaryBrush
		};
		Grid.SetColumn(headingTb, 0);
		headingGrid.Children.Add(headingTb);

		_roomHostStatusTb = new TextBlock
		{
			Text = "等待宿主确认",
			FontSize = 11,
			Foreground = DspTextMutedBrush
		};
		Grid.SetColumn(_roomHostStatusTb, 1);
		headingGrid.Children.Add(_roomHostStatusTb);

		sectionStack.Children.Add(headingGrid);

		// 4-Node Path Grid
		var pathGrid = new Grid();
		// Col 0: Input, Col 1: Arrow, Col 2: DSP, Col 3: Arrow, Col 4: Convolver, Col 5: Arrow, Col 6: Device Output
		pathGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.0, GridUnitType.Star) });
		pathGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(32) });
		pathGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.0, GridUnitType.Star) });
		pathGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(32) });
		pathGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.0, GridUnitType.Star) });
		pathGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(32) });
		pathGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.0, GridUnitType.Star) });

		// Node 1: 音频输入
		var node1 = CreateSignalPathNode("\uE720", "音频输入", out _roomInputChTb);
		Grid.SetColumn(node1, 0);
		pathGrid.Children.Add(node1);

		var arrow1 = CreateSignalPathArrow();
		Grid.SetColumn(arrow1, 1);
		pathGrid.Children.Add(arrow1);

		// Node 2: 内置 DSP
		var node2 = CreateSignalPathNode("\uE950", "内置 DSP", out _roomDspBitsTb);
		Grid.SetColumn(node2, 2);
		pathGrid.Children.Add(node2);

		var arrow2 = CreateSignalPathArrow();
		Grid.SetColumn(arrow2, 3);
		pathGrid.Children.Add(arrow2);

		// Node 3: 卷积插槽
		var node3 = CreateSignalPathNode("\uE9E9", "卷积插槽", out _roomConvolverSlotTb);
		Grid.SetColumn(node3, 4);
		pathGrid.Children.Add(node3);

		var arrow3 = CreateSignalPathArrow();
		Grid.SetColumn(arrow3, 5);
		pathGrid.Children.Add(arrow3);

		// Node 4: 设备输出
		var node4 = CreateSignalPathNode("\uE7F5", "设备输出", out _roomDeviceOutTb);
		Grid.SetColumn(node4, 6);
		pathGrid.Children.Add(node4);

		sectionStack.Children.Add(pathGrid);
		return sectionStack;
	}

	private FrameworkElement CreateSignalPathNode(string glyph, string label, out TextBlock valueTb)
	{
		var nodeBorder = new Border
		{
			Background = DspSoftSurfaceBrush,
			BorderBrush = CreateLiquidGlassRimBrush(),
			BorderThickness = new Thickness(1),
			CornerRadius = new CornerRadius(8),
			Padding = new Thickness(12, 10, 12, 10)
		};
		_roomSignalPathNodes.Add(nodeBorder);

		var stack = new StackPanel { Spacing = 4, HorizontalAlignment = HorizontalAlignment.Center };

		var topRow = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 6, HorizontalAlignment = HorizontalAlignment.Center };
		topRow.Children.Add(new FontIcon
		{
			Glyph = glyph,
			FontSize = 13,
			Foreground = DspTextMutedBrush,
			VerticalAlignment = VerticalAlignment.Center
		});
		topRow.Children.Add(new TextBlock
		{
			Text = label,
			FontSize = 11,
			FontWeight = FontWeights.Medium,
			Foreground = DspTextMutedBrush,
			VerticalAlignment = VerticalAlignment.Center
		});
		stack.Children.Add(topRow);

		valueTb = new TextBlock
		{
			Text = "—",
			FontSize = 13,
			FontWeight = FontWeights.Bold,
			Foreground = DspTextPrimaryBrush,
			HorizontalAlignment = HorizontalAlignment.Center
		};
		stack.Children.Add(valueTb);

		nodeBorder.Child = stack;
		return nodeBorder;
	}

	private FrameworkElement CreateSignalPathArrow()
	{
		var arrow = new FontIcon
		{
			Glyph = "\uE72A", // Chevron / Arrow right
			FontSize = 12,
			Foreground = DspTextMutedBrush,
			HorizontalAlignment = HorizontalAlignment.Center,
			VerticalAlignment = VerticalAlignment.Center
		};
		_roomSignalPathArrows.Add(arrow);
		return arrow;
	}

	/// <summary>
	/// Active IR Control Deck: Shows loaded file details, Trim slider (-24dB to +6dB), Quick Trim buttons, and Clear action.
	/// </summary>
	private Border CreateActiveIrControlDeck()
	{
		var card = new Border
		{
			Background = DspSoftSurfaceBrush,
			BorderBrush = CreateLiquidGlassRimBrush(),
			BorderThickness = new Thickness(1),
			CornerRadius = new CornerRadius(10),
			Padding = new Thickness(18, 14, 18, 16),
			Visibility = Visibility.Collapsed // default hidden until IR loaded
		};

		var stack = new StackPanel { Spacing = 14 };

		// 1. File info row
		var fileRow = new Grid();
		fileRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.0, GridUnitType.Star) });
		fileRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

		var fileLeft = new StackPanel { Spacing = 2 };
		fileLeft.Children.Add(new TextBlock
		{
			Text = "已载入 IR 脉冲文件",
			FontSize = 11,
			FontWeight = FontWeights.Medium,
			Foreground = DspTextMutedBrush
		});
		_roomIrFileNameTb = new TextBlock
		{
			Text = "room_correction_48k.wav",
			FontSize = 14,
			FontWeight = FontWeights.Bold,
			Foreground = DspTextPrimaryBrush
		};
		fileLeft.Children.Add(_roomIrFileNameTb);

		_roomIrSpecsTb = new TextBlock
		{
			Text = "48 kHz · 立体声 · 65,536 Taps · 1,365 ms",
			FontSize = 11,
			Foreground = DspTextMutedBrush
		};
		fileLeft.Children.Add(_roomIrSpecsTb);
		Grid.SetColumn(fileLeft, 0);
		fileRow.Children.Add(fileLeft);

		var fileRight = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8, VerticalAlignment = VerticalAlignment.Center };

		bool isDark = IsDspDark;

		var reimportBtn = new Button
		{
			Background = DspSoftSurfaceBrush,
			BorderBrush = CreateLiquidGlassRimBrush(),
			BorderThickness = new Thickness(1),
			CornerRadius = new CornerRadius(6),
			Padding = new Thickness(12, 4, 12, 4),
			Foreground = DspTextPrimaryBrush
		};
		reimportBtn.Content = new TextBlock { Text = "重新导入", FontSize = 12, FontWeight = FontWeights.Medium, Foreground = DspTextPrimaryBrush };
		reimportBtn.Click += (s, e) => TriggerImportFilePicker();
		_roomIrReimportBtn = reimportBtn;
		fileRight.Children.Add(reimportBtn);

		var clearBtn = new Button
		{
			BorderThickness = new Thickness(1),
			CornerRadius = new CornerRadius(6),
			Padding = new Thickness(10, 4, 12, 4)
		};
		var clearStack = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 6 };
		clearStack.Children.Add(new FontIcon
		{
			Glyph = "\uE74D", // Trash / delete
			FontSize = 12,
			VerticalAlignment = VerticalAlignment.Center
		});
		clearStack.Children.Add(new TextBlock
		{
			Text = "清除",
			FontSize = 12,
			FontWeight = FontWeights.Medium,
			VerticalAlignment = VerticalAlignment.Center
		});
		clearBtn.Content = clearStack;
		ApplyRoomDangerButtonTheme(clearBtn, isDark);
		clearBtn.Click += (s, e) =>
		{
			if (ViewModel != null)
			{
				ViewModel.ClearConvolutionIr();
				RefreshRoomCorrectionState();
			}
		};
		_roomIrClearBtn = clearBtn;
		fileRight.Children.Add(clearBtn);

		Grid.SetColumn(fileRight, 1);
		fileRow.Children.Add(fileRight);
		stack.Children.Add(fileRow);

		// Divider
		var irDivider = new Rectangle { Height = 1, Fill = GetRoomDividerBrush(isDark) };
		_roomDividers.Add(irDivider);
		stack.Children.Add(irDivider);

		// 2. Trim Gain Controls
		var trimHeaderGrid = new Grid();
		trimHeaderGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.0, GridUnitType.Star) });
		trimHeaderGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

		var trimTitleStack = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 6, VerticalAlignment = VerticalAlignment.Center };
		_roomTrimIcon = new FontIcon
		{
			Glyph = "\uE9E9",
			FontSize = 13,
			Foreground = DspAccentSoftTextBrush,
			VerticalAlignment = VerticalAlignment.Center
		};
		trimTitleStack.Children.Add(_roomTrimIcon);
		trimTitleStack.Children.Add(new TextBlock
		{
			Text = "校正增益 (Correction Trim)",
			FontSize = 12,
			FontWeight = FontWeights.Bold,
			Foreground = DspTextPrimaryBrush,
			VerticalAlignment = VerticalAlignment.Center
		});
		Grid.SetColumn(trimTitleStack, 0);
		trimHeaderGrid.Children.Add(trimTitleStack);

		_roomTrimValueTb = new TextBlock
		{
			Text = "0.0 dB",
			FontSize = 14,
			FontWeight = FontWeights.Bold,
			Foreground = DspAccentSoftTextBrush,
			VerticalAlignment = VerticalAlignment.Center
		};
		Grid.SetColumn(_roomTrimValueTb, 1);
		trimHeaderGrid.Children.Add(_roomTrimValueTb);
		stack.Children.Add(trimHeaderGrid);

		// Trim Slider
		_roomTrimSlider = new Slider
		{
			Minimum = -24.0,
			Maximum = 6.0,
			StepFrequency = 0.1,
			TickFrequency = 3.0,
			Value = ViewModel?.EchoConvolutionTrimDb ?? 0.0,
			Margin = new Thickness(0, 2, 0, 2)
		};
		_roomTrimSlider.ValueChanged += (s, e) =>
		{
			if (ViewModel != null)
			{
				ViewModel.EchoConvolutionTrimDb = e.NewValue;
				ViewModel.ApplyConvolutionSettings();
				if (_roomTrimValueTb != null)
				{
					_roomTrimValueTb.Text = $"{e.NewValue:+0.0;-0.0;0.0} dB";
				}
				UpdateTrimPresetHighlights(e.NewValue);
			}
		};
		stack.Children.Add(_roomTrimSlider);

		// Quick Trim Preset Pills
		var presetRow = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8, VerticalAlignment = VerticalAlignment.Center };
		presetRow.Children.Add(new TextBlock
		{
			Text = "快捷增益:",
			FontSize = 11,
			FontWeight = FontWeights.Medium,
			Foreground = DspTextMutedBrush,
			VerticalAlignment = VerticalAlignment.Center
		});

		double[] presets = new double[] { -6.0, -3.0, 0.0 };
		_roomTrimPresetButtons.Clear();
		foreach (double pVal in presets)
		{
			var pBtn = new Button
			{
				Background = DspSoftSurfaceBrush,
				BorderBrush = CreateLiquidGlassRimBrush(),
				BorderThickness = new Thickness(1),
				CornerRadius = new CornerRadius(14),
				Padding = new Thickness(12, 3, 12, 3),
				Tag = pVal
			};
			pBtn.Content = new TextBlock
			{
				Text = $"{pVal:+0;-0;0} dB",
				FontSize = 11,
				FontWeight = FontWeights.Medium,
				Foreground = DspTextSecondaryBrush
			};
			pBtn.Click += (s, e) =>
			{
				if (s is Button b && b.Tag is double val && _roomTrimSlider != null)
				{
					_roomTrimSlider.Value = val;
				}
			};
			_roomTrimPresetButtons.Add(pBtn);
			presetRow.Children.Add(pBtn);
		}
		stack.Children.Add(presetRow);

		// Clipping Warning Banner (shown when overload detected)
		_roomClippingBanner = new Border
		{
			BorderThickness = new Thickness(1),
			CornerRadius = new CornerRadius(6),
			Padding = new Thickness(12, 8, 12, 8),
			Visibility = Visibility.Collapsed
		};
		var clipStack = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8, VerticalAlignment = VerticalAlignment.Center };
		_roomClippingIcon = new FontIcon
		{
			Glyph = "\uE783", // Warning triangle
			FontSize = 14
		};
		clipStack.Children.Add(_roomClippingIcon);
		_roomClippingTextTb = new TextBlock
		{
			Text = "检测到卷积输出存在削波风险！建议将校正增益衰减至 -6 dB 以下，并在 Headroom 页面配置数字余量。",
			FontSize = 11
		};
		clipStack.Children.Add(_roomClippingTextTb);
		_roomClippingBanner.Child = clipStack;
		ApplyRoomClippingBannerTheme(isDark);
		stack.Children.Add(_roomClippingBanner);

		card.Child = stack;
		return card;
	}

	/// <summary>
	/// Three Quick Workflow Navigation Cards:
	/// 1. 扬声器补偿 (左右增益、延迟与反相 ↗)
	/// 2. 余量与削波 (检查实际输出峰值 ↗)
	/// 3. 保存校正场景 (绑定输出设备与对比试听 ↗)
	/// </summary>
	private FrameworkElement CreateRoomWorkflowCards()
	{
		var grid = new Grid { ColumnSpacing = 14 };
		grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.0, GridUnitType.Star) });
		grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.0, GridUnitType.Star) });
		grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.0, GridUnitType.Star) });

		// Card 1: 扬声器补偿
		var card1 = CreateWorkflowNavCard("扬声器补偿", "左右增益、延迟与反相", () => SelectDspCategory("spatial"));
		Grid.SetColumn(card1, 0);
		grid.Children.Add(card1);

		// Card 2: 余量与削波
		var card2 = CreateWorkflowNavCard("余量与削波", "检查实际输出峰值", () => SelectDspCategory("headroom"));
		Grid.SetColumn(card2, 1);
		grid.Children.Add(card2);

		// Card 3: 保存校正场景
		var card3 = CreateWorkflowNavCard("保存校正场景", "绑定输出设备与对比试听", () => SelectDspCategory("listening_plan"));
		Grid.SetColumn(card3, 2);
		grid.Children.Add(card3);

		return grid;
	}

	private FrameworkElement CreateWorkflowNavCard(string title, string subtitle, Action onClick)
	{
		bool isDark = IsDspDark;
		var btn = new Button
		{
			HorizontalAlignment = HorizontalAlignment.Stretch,
			HorizontalContentAlignment = HorizontalAlignment.Stretch,
			Background = CreateAcrylicCardBrush(0.88, 235),
			BorderBrush = CreateLiquidGlassRimBrush(),
			BorderThickness = new Thickness(1),
			CornerRadius = new CornerRadius(10),
			Padding = new Thickness(16, 12, 14, 12),
			Foreground = DspTextPrimaryBrush
		};
		LiquidGlassEngine.AttachInteractiveLiquidGlass(btn, LiquidGlassVariant.Regular, isDark: isDark, enablePointerTracking: true);
		_roomWorkflowCardButtons.Add(btn);

		var innerGrid = new Grid();
		innerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.0, GridUnitType.Star) });
		innerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

		var textStack = new StackPanel { Spacing = 2 };
		textStack.Children.Add(new TextBlock
		{
			Text = title,
			FontSize = 13,
			FontWeight = FontWeights.Bold,
			Foreground = DspTextPrimaryBrush
		});
		textStack.Children.Add(new TextBlock
		{
			Text = subtitle,
			FontSize = 11,
			Foreground = DspTextMutedBrush
		});
		Grid.SetColumn(textStack, 0);
		innerGrid.Children.Add(textStack);

		var arrowIcon = new FontIcon
		{
			Glyph = "\uE8A7", // ArrowUpRight ↗
			FontSize = 13,
			Foreground = DspTextMutedBrush,
			VerticalAlignment = VerticalAlignment.Center
		};
		Grid.SetColumn(arrowIcon, 1);
		innerGrid.Children.Add(arrowIcon);

		btn.Content = innerGrid;
		btn.Click += (s, e) => onClick?.Invoke();
		return btn;
	}

	/// <summary>
	/// Collapsible REW Guide Card: "用 REW 制作音箱校正 ^"
	/// 4 steps with clear instructions and export command box.
	/// </summary>
	private FrameworkElement CreateRewGuideCard()
	{
		bool isDark = IsDspDark;
		_roomRewExpanderCard = new Border
		{
			Background = CreateAcrylicCardBrush(0.88, 235),
			BorderBrush = CreateLiquidGlassRimBrush(),
			BorderThickness = new Thickness(1),
			CornerRadius = new CornerRadius(12),
			Padding = new Thickness(20, 16, 20, 16)
		};
		LiquidGlassEngine.AttachInteractiveLiquidGlass(_roomRewExpanderCard, LiquidGlassVariant.Regular, isDark: isDark, enablePointerTracking: false);

		var rootStack = new StackPanel { Spacing = 14 };

		// Clickable Header Row
		var headerBtn = new Button
		{
			HorizontalAlignment = HorizontalAlignment.Stretch,
			HorizontalContentAlignment = HorizontalAlignment.Stretch,
			Background = new SolidColorBrush(Colors.Transparent),
			BorderThickness = new Thickness(0),
			Padding = new Thickness(0)
		};

		var headerGrid = new Grid();
		headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.0, GridUnitType.Star) });
		headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

		var headerTitle = new TextBlock
		{
			Text = "用 REW 制作音箱校正",
			FontSize = 13,
			FontWeight = FontWeights.Bold,
			Foreground = DspTextPrimaryBrush,
			VerticalAlignment = VerticalAlignment.Center
		};
		Grid.SetColumn(headerTitle, 0);
		headerGrid.Children.Add(headerTitle);

		_roomRewChevron = new FontIcon
		{
			Glyph = "\uE70E", // ChevronUp (expanded)
			FontSize = 12,
			Foreground = DspTextMutedBrush,
			VerticalAlignment = VerticalAlignment.Center
		};
		Grid.SetColumn(_roomRewChevron, 1);
		headerGrid.Children.Add(_roomRewChevron);

		headerBtn.Content = headerGrid;
		headerBtn.Click += (s, e) =>
		{
			if (_roomRewExpanderBody != null && _roomRewChevron != null)
			{
				bool isExpanded = _roomRewExpanderBody.Visibility == Visibility.Visible;
				_roomRewExpanderBody.Visibility = isExpanded ? Visibility.Collapsed : Visibility.Visible;
				_roomRewChevron.Glyph = isExpanded ? "\uE70D" : "\uE70E";
			}
		};
		rootStack.Children.Add(headerBtn);

		// Collapsible Body
		_roomRewExpanderBody = new StackPanel { Spacing = 14 };

		var introText = new TextBlock
		{
			Text = "在 REW 中测量与生成滤波器，再由 FurinaPlayer 应用于播放。需要带校准文件的测量麦克风；本页不会录音或自动测量。",
			FontSize = 12,
			Foreground = DspTextSubHeaderBrush
		};
		_roomRewExpanderBody.Children.Add(introText);

		// 4 Numbered Steps
		_roomRewExpanderBody.Children.Add(CreateRewStepRow(
			"1",
			"测量左右音箱",
			"在 REW 中载入麦克风校准文件，将麦克风放在听音位置，与耳朵同高，检查输入和输出电平，从较低音量开始，分别测量左、右音箱并保存。"
		));

		_roomRewExpanderBody.Children.Add(CreateRewStepRow(
			"2",
			"生成校正",
			"在 REW 的 EQ 窗口设置目标曲线和校正频段，优先处理低频峰值，限制提升量，保留音箱自然低频衰减，不强行填平深谷。左右声道分别生成滤波器。"
		));

		// Step 3 with Code Callout
		var step3Stack = new StackPanel { Spacing = 6 };
		var step3Header = new Grid();
		step3Header.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(28) });
		step3Header.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.0, GridUnitType.Star) });

		var num3 = new TextBlock
		{
			Text = "3",
			FontSize = 13,
			FontWeight = FontWeights.Bold,
			Foreground = DspTextPrimaryBrush
		};
		Grid.SetColumn(num3, 0);
		step3Header.Children.Add(num3);

		var step3BodyStack = new StackPanel { Spacing = 8 };
		step3BodyStack.Children.Add(new TextBlock
		{
			Text = "导出立体声滤波器",
			FontSize = 13,
			FontWeight = FontWeights.Bold,
			Foreground = DspTextPrimaryBrush
		});
		step3BodyStack.Children.Add(new TextBlock
		{
			Text = "选择下方导出菜单，使用 Stereo, 32-bit Float，左、右分别对应刚才的测量，关闭峰值归一化以保留校正增益关系。导出校正滤波器，不要导出原始房间响应。",
			FontSize = 12,
			Foreground = DspTextSubHeaderBrush,
			TextWrapping = TextWrapping.Wrap
		});

		// Code Callout Box
		var codeBox = new Border
		{
			Background = DspSoftSurfaceBrush,
			BorderBrush = CreateLiquidGlassRimBrush(),
			BorderThickness = new Thickness(1),
			CornerRadius = new CornerRadius(6),
			Padding = new Thickness(12, 6, 12, 6),
			HorizontalAlignment = HorizontalAlignment.Left
		};
		_roomRewCodeBox = codeBox;
		codeBox.Child = new TextBlock
		{
			Text = "File → Export → Export filters impulse response as WAV",
			FontFamily = new FontFamily("Consolas, Courier New, monospace"),
			FontSize = 11,
			Foreground = DspTextSecondaryBrush
		};
		step3BodyStack.Children.Add(codeBox);

		step3BodyStack.Children.Add(new TextBlock
		{
			Text = "每声道最多 131,072 个采样点，总计不超过 262,144。为实际使用的采样率分别导出（如 44.1 / 48 / 96 kHz），可将 WAV 打成 ZIP，每种采样率只放一份立体声滤波器；分开的左右 IR 可用 Convolver .cfg 明确对应，并连同 IR 一起打包。",
			FontSize = 11,
			Foreground = DspTextMutedBrush,
			TextWrapping = TextWrapping.Wrap
		});

		Grid.SetColumn(step3BodyStack, 1);
		step3Header.Children.Add(step3BodyStack);
		_roomRewExpanderBody.Children.Add(step3Header);

		// Step 4
		_roomRewExpanderBody.Children.Add(CreateRewStepRow(
			"4",
			"启用、试听与复测",
			"导入后使用“安全启用 FIR”，低音量试听并观察削波；预留 6 dB 只是起点，比较前后时匹配听感音量。复测信号须经过 FurinaPlayer：可用 REW 导出的测量扫频文件在 FurinaPlayer 播放，再按 REW 离线测量流程录制、分析；直接由 REW 输出的扫频不会经过这里的校正。"
		));

		// Bottom Hint
		_roomRewGuideFooterTb = new TextBlock
		{
			Text = "已有校正滤波器？使用上方“导入 IR”。",
			FontSize = 12,
			Foreground = DspTextMutedBrush,
			Margin = new Thickness(28, 4, 0, 0)
		};
		_roomRewExpanderBody.Children.Add(_roomRewGuideFooterTb);

		rootStack.Children.Add(_roomRewExpanderBody);
		_roomRewExpanderCard.Child = rootStack;
		return _roomRewExpanderCard;
	}

	private FrameworkElement CreateRewStepRow(string stepNum, string title, string description)
	{
		var grid = new Grid();
		grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(28) });
		grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.0, GridUnitType.Star) });

		var numTb = new TextBlock
		{
			Text = stepNum,
			FontSize = 13,
			FontWeight = FontWeights.Bold,
			Foreground = DspTextPrimaryBrush,
			VerticalAlignment = VerticalAlignment.Top
		};
		Grid.SetColumn(numTb, 0);
		grid.Children.Add(numTb);

		var textStack = new StackPanel { Spacing = 3 };
		textStack.Children.Add(new TextBlock
		{
			Text = title,
			FontSize = 13,
			FontWeight = FontWeights.Bold,
			Foreground = DspTextPrimaryBrush
		});
		textStack.Children.Add(new TextBlock
		{
			Text = description,
			FontSize = 12,
			Foreground = DspTextSubHeaderBrush,
			TextWrapping = TextWrapping.Wrap
		});
		Grid.SetColumn(textStack, 1);
		grid.Children.Add(textStack);

		return grid;
	}

	/// <summary>
	/// Opens FileOpenPicker for IR impulses (.wav, .flac, .zip, .cfg, .aiff, .m4a).
	/// </summary>
	private async void TriggerImportFilePicker()
	{
		try
		{
			var picker = new FileOpenPicker
			{
				SuggestedStartLocation = PickerLocationId.MusicLibrary
			};
			picker.FileTypeFilter.Add(".wav");
			picker.FileTypeFilter.Add(".flac");
			picker.FileTypeFilter.Add(".zip");
			picker.FileTypeFilter.Add(".cfg");
			picker.FileTypeFilter.Add(".aiff");
			picker.FileTypeFilter.Add(".aif");
			picker.FileTypeFilter.Add(".m4a");

			nint windowHandle = WindowNative.GetWindowHandle(WindowManager.CurrentWindow);
			InitializeWithWindow.Initialize(picker, windowHandle);

			var file = await picker.PickSingleFileAsync();
			if (file != null && ViewModel != null)
			{
				bool ok = ViewModel.LoadConvolutionIr(file.Path);
				RefreshRoomCorrectionState();
			}
		}
		catch (Exception ex)
		{
			Debug.WriteLine("Room correction IR picker error: " + ex.Message);
		}
	}

	/// <summary>
	/// Refreshes all UI readouts, buttons, and state indicators to match the underlying DSP engine.
	/// </summary>
	private void RefreshRoomCorrectionState()
	{
		if (ViewModel == null) return;

		bool hasIr = !string.IsNullOrEmpty(ViewModel.EchoConvolutionFilePath);
		bool isEnabled = ViewModel.EchoConvolutionEnabled;
		var conv = ViewModel.AudioEngine?.DspChain?.Convolution;

		// 1. Module count badge
		int activeModules = 0;
		var chain = ViewModel.AudioEngine?.DspChain;
		if (chain != null)
		{
			if (chain.Eq.Enabled) activeModules++;
			if (chain.Headroom.Enabled) activeModules++;
			if (chain.ReplayGain.Mode != SonicWave.Audio.Dsp.ReplayGainMode.Off) activeModules++;
			if (chain.Spatial.CrossfeedEnabled || chain.Spatial.StereoFieldEnabled) activeModules++;
			if (chain.Resampler.Enabled) activeModules++;
			if (chain.Convolution.Enabled) activeModules++;
			if (chain.Compressor.Enabled) activeModules++;
		}
		// Hero Status Dot & Text
		if (_roomStatusDot != null && _roomStatusText != null)
		{
			if (!hasIr)
			{
				_roomStatusDot.Fill = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 16, 185, 129)); // Green dot
				_roomStatusText.Text = "准备音频校正";
			}
			else if (isEnabled)
			{
				_roomStatusDot.Fill = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 79, 70, 229)); // Indigo dot
				_roomStatusText.Text = "校正正在应用";
			}
			else
			{
				_roomStatusDot.Fill = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 245, 158, 11)); // Amber dot
				_roomStatusText.Text = "滤波器已准备";
			}
		}

		// 4. Hero Action Buttons
		if (_roomActionPanel != null)
		{
			bool isDark = IsDspDark;
			_roomActionPanel.Children.Clear();

			if (!hasIr)
			{
				// Primary [导入 IR] Button
				var importBtn = new Button
				{
					Background = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 79, 70, 229)),
					Foreground = new SolidColorBrush(Colors.White),
					CornerRadius = new CornerRadius(8),
					Padding = new Thickness(16, 8, 16, 8)
				};
				var btnStack = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 6 };
				btnStack.Children.Add(new FontIcon { Glyph = "\uE896", FontSize = 13, Foreground = new SolidColorBrush(Colors.White) });
				btnStack.Children.Add(new TextBlock { Text = "导入 IR", FontSize = 12, FontWeight = FontWeights.SemiBold, Foreground = new SolidColorBrush(Colors.White) });
				importBtn.Content = btnStack;
				importBtn.Click += (s, e) => TriggerImportFilePicker();
				_roomActionPanel.Children.Add(importBtn);
			}
			else
			{
				if (isEnabled)
				{
					// [停用 FIR] Button
					var disableBtn = new Button
					{
						BorderThickness = new Thickness(1),
						CornerRadius = new CornerRadius(8),
						Padding = new Thickness(14, 7, 14, 7)
					};
					var disStack = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 6 };
					disStack.Children.Add(new FontIcon { Glyph = "\uE71A", FontSize = 12 });
					disStack.Children.Add(new TextBlock { Text = "停用 FIR", FontSize = 12, FontWeight = FontWeights.Medium });
					disableBtn.Content = disStack;
					ApplyRoomDangerButtonTheme(disableBtn, isDark);
					disableBtn.Click += (s, e) =>
					{
						ViewModel.EchoConvolutionEnabled = false;
						ViewModel.ApplyConvolutionSettings();
						RefreshRoomCorrectionState();
					};
					_roomActionPanel.Children.Add(disableBtn);
				}
				else
				{
					// [安全启用 FIR] Primary Button (limits trim to -6dB)
					var safeEnableBtn = new Button
					{
						Background = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 79, 70, 229)),
						Foreground = new SolidColorBrush(Colors.White),
						CornerRadius = new CornerRadius(8),
						Padding = new Thickness(14, 7, 14, 7)
					};
					var safeStack = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 6 };
					safeStack.Children.Add(new FontIcon { Glyph = "\uE73E", FontSize = 12, Foreground = new SolidColorBrush(Colors.White) });
					safeStack.Children.Add(new TextBlock { Text = "安全启用 FIR", FontSize = 12, FontWeight = FontWeights.SemiBold, Foreground = new SolidColorBrush(Colors.White) });
					safeEnableBtn.Content = safeStack;
					safeEnableBtn.Click += (s, e) =>
					{
						ViewModel.EchoConvolutionTrimDb = Math.Min(ViewModel.EchoConvolutionTrimDb, -6.0);
						ViewModel.EchoConvolutionEnabled = true;
						ViewModel.ApplyConvolutionSettings();
						RefreshRoomCorrectionState();
					};
					_roomActionPanel.Children.Add(safeEnableBtn);

					// [启用 FIR] Subtle Button
					var enableBtn = new Button
					{
						Background = DspSoftSurfaceBrush,
						BorderBrush = CreateLiquidGlassRimBrush(),
						BorderThickness = new Thickness(1),
						CornerRadius = new CornerRadius(8),
						Padding = new Thickness(12, 7, 12, 7),
						Foreground = DspTextPrimaryBrush
					};
					enableBtn.Content = new TextBlock { Text = "启用 FIR", FontSize = 12, FontWeight = FontWeights.Medium, Foreground = DspTextPrimaryBrush };
					enableBtn.Click += (s, e) =>
					{
						ViewModel.EchoConvolutionEnabled = true;
						ViewModel.ApplyConvolutionSettings();
						RefreshRoomCorrectionState();
					};
					_roomActionPanel.Children.Add(enableBtn);
				}

				// [重新导入] Secondary button
				var reimportBtn = new Button
				{
					Background = DspSoftSurfaceBrush,
					BorderBrush = CreateLiquidGlassRimBrush(),
					BorderThickness = new Thickness(1),
					CornerRadius = new CornerRadius(8),
					Padding = new Thickness(12, 7, 12, 7),
					Foreground = DspTextPrimaryBrush
				};
				reimportBtn.Content = new TextBlock { Text = "导入 IR", FontSize = 12, FontWeight = FontWeights.Medium, Foreground = DspTextPrimaryBrush };
				reimportBtn.Click += (s, e) => TriggerImportFilePicker();
				_roomActionPanel.Children.Add(reimportBtn);
			}
		}

		// 5. Signal Path Readouts
		int currentRate = ViewModel.EngineSampleRate > 0 ? ViewModel.EngineSampleRate : 48000;
		int channels = 2;

		if (_roomHostStatusTb != null)
		{
			_roomHostStatusTb.Text = hasIr ? "来自音频宿主" : "等待宿主确认";
		}
		if (_roomInputChTb != null)
		{
			_roomInputChTb.Text = hasIr ? $"{channels} ch" : "—";
		}
		if (_roomDspBitsTb != null)
		{
			_roomDspBitsTb.Text = hasIr ? "64-bit" : "—";
		}
		if (_roomConvolverSlotTb != null)
		{
			_roomConvolverSlotTb.Text = hasIr ? $"{channels} ch → {channels} ch" : "—";
		}
		if (_roomDeviceOutTb != null)
		{
			_roomDeviceOutTb.Text = hasIr ? $"{channels} ch" : "—";
		}

		// 6. Active IR Control Deck Visibility & Data
		if (_roomActiveIrCard != null)
		{
			_roomActiveIrCard.Visibility = hasIr ? Visibility.Visible : Visibility.Collapsed;
			if (hasIr)
			{
				string fileName = System.IO.Path.GetFileName(ViewModel.EchoConvolutionFilePath) ?? "Impulse Response";
				if (_roomIrFileNameTb != null) _roomIrFileNameTb.Text = fileName;

				int taps = conv?.TapCount > 0 ? conv.TapCount : 65536;
				int fRate = conv?.FilterSampleRate > 0 ? conv.FilterSampleRate : currentRate;
				double ms = taps * 1000.0 / fRate;
				if (_roomIrSpecsTb != null)
				{
					_roomIrSpecsTb.Text = $"{fRate / 1000.0:F1} kHz · 立体声 · {taps:N0} Taps · {ms:F1} ms";
				}

				if (_roomTrimSlider != null)
				{
					_roomTrimSlider.Value = ViewModel.EchoConvolutionTrimDb;
				}
				if (_roomTrimValueTb != null)
				{
					_roomTrimValueTb.Text = $"{ViewModel.EchoConvolutionTrimDb:+0.0;-0.0;0.0} dB";
				}
				UpdateTrimPresetHighlights(ViewModel.EchoConvolutionTrimDb);

				// Clipping risk check
				bool risk = conv?.ClippingRisk ?? false;
				if (_roomClippingBanner != null)
				{
					_roomClippingBanner.Visibility = risk ? Visibility.Visible : Visibility.Collapsed;
				}
			}
		}

		// 7. Update REW Guide Bottom Status
		if (_roomRewGuideFooterTb != null)
		{
			if (isEnabled)
			{
				_roomRewGuideFooterTb.Text = "已启用校正。实际改善需要试听与复测确认；更换音箱、摆位或听音位置后应重新测量。";
			}
			else if (hasIr)
			{
				_roomRewGuideFooterTb.Text = "滤波器已载入。确认输出仍是测量时的音箱与摆位，再启用。";
			}
			else
			{
				_roomRewGuideFooterTb.Text = "已有校正滤波器？使用上方“导入 IR”。";
			}
		}

		// 8. Update Left Sidebar Subtitle
		if (_roomNavSubTb != null)
		{
			_roomNavSubTb.Text = !hasIr ? "无 IR" : (isEnabled ? "已启用" : "已载入");
		}
	}

	private void UpdateTrimPresetHighlights(double currentVal)
	{
		bool isDark = IsDspDark;
		Brush matchBg = isDark
			? new SolidColorBrush(Windows.UI.Color.FromArgb(85, 99, 102, 241))
			: new SolidColorBrush(Windows.UI.Color.FromArgb(255, 238, 242, 255));
		Brush matchBorder = new SolidColorBrush(isDark
			? Windows.UI.Color.FromArgb(180, 129, 140, 248)
			: Windows.UI.Color.FromArgb(255, 129, 140, 248));
		Brush matchText = new SolidColorBrush(isDark
			? Windows.UI.Color.FromArgb(255, 199, 210, 254)
			: Windows.UI.Color.FromArgb(255, 79, 70, 229));

		Brush normalBg = isDark
			? LiquidGlassEngine.CreateSurfaceBrush(LiquidGlassVariant.Clear, isDark: true, opacityScale: 0.45)
			: new SolidColorBrush(Windows.UI.Color.FromArgb(220, 241, 245, 249));
		Brush normalBorder = LiquidGlassEngine.CreateSpecularRimBrush(LiquidGlassVariant.Regular, isDark: isDark);
		Brush normalText = new SolidColorBrush(isDark
			? Windows.UI.Color.FromArgb(255, 226, 232, 240)
			: Windows.UI.Color.FromArgb(255, 51, 65, 85));

		foreach (var btn in _roomTrimPresetButtons)
		{
			if (btn.Tag is double pVal)
			{
				bool isMatch = Math.Abs(currentVal - pVal) < 0.15;
				btn.Background = isMatch ? matchBg : normalBg;
				btn.BorderBrush = isMatch ? matchBorder : normalBorder;
				btn.Foreground = isMatch ? matchText : normalText;
				if (btn.Content is TextBlock tb)
				{
					tb.Foreground = isMatch ? matchText : normalText;
				}
			}
		}
	}

	private static Brush GetRoomDividerBrush(bool isDark) => new SolidColorBrush(isDark
		? Windows.UI.Color.FromArgb(45, 255, 255, 255)
		: Windows.UI.Color.FromArgb(255, 241, 245, 249));

	private static void ApplyRoomDangerButtonTheme(Button btn, bool isDark)
	{
		btn.Background = new SolidColorBrush(isDark
			? Windows.UI.Color.FromArgb(50, 239, 68, 68)
			: Windows.UI.Color.FromArgb(255, 254, 242, 242));
		btn.BorderBrush = new SolidColorBrush(isDark
			? Windows.UI.Color.FromArgb(120, 248, 113, 113)
			: Windows.UI.Color.FromArgb(255, 254, 202, 202));
		var fg = new SolidColorBrush(isDark
			? Windows.UI.Color.FromArgb(255, 252, 165, 165)
			: Windows.UI.Color.FromArgb(255, 220, 38, 38));
		btn.Foreground = fg;
		if (btn.Content is StackPanel sp)
		{
			foreach (var child in sp.Children)
			{
				if (child is FontIcon fi) fi.Foreground = fg;
				else if (child is TextBlock tb) tb.Foreground = fg;
			}
		}
	}

	private void ApplyRoomClippingBannerTheme(bool isDark)
	{
		if (_roomClippingBanner == null) return;
		_roomClippingBanner.Background = new SolidColorBrush(isDark
			? Windows.UI.Color.FromArgb(55, 239, 68, 68)
			: Windows.UI.Color.FromArgb(255, 254, 242, 242));
		_roomClippingBanner.BorderBrush = new SolidColorBrush(isDark
			? Windows.UI.Color.FromArgb(130, 248, 113, 113)
			: Windows.UI.Color.FromArgb(255, 248, 113, 113));
		if (_roomClippingIcon != null)
		{
			_roomClippingIcon.Foreground = new SolidColorBrush(isDark
				? Windows.UI.Color.FromArgb(255, 252, 165, 165)
				: Windows.UI.Color.FromArgb(255, 220, 38, 38));
		}
		if (_roomClippingTextTb != null)
		{
			_roomClippingTextTb.Foreground = new SolidColorBrush(isDark
				? Windows.UI.Color.FromArgb(255, 254, 202, 202)
				: Windows.UI.Color.FromArgb(255, 185, 28, 28));
		}
	}

	public void RefreshRoomCorrectionTheme(bool isDark)
	{
		if (_roomCorrectionWorkbench == null) return;

		Brush rootBg = LiquidGlassEngine.CreateSurfaceBrush(LiquidGlassVariant.Clear, isDark: isDark, opacityScale: Math.Clamp(0.72 / 0.85, 0.55, 1.0));
		Brush cardBg = LiquidGlassEngine.CreateSurfaceBrush(LiquidGlassVariant.Regular, isDark: isDark, opacityScale: Math.Clamp(0.88 / 0.90, 0.72, 1.05));
		Brush rimBrush = LiquidGlassEngine.CreateSpecularRimBrush(LiquidGlassVariant.Regular, isDark: isDark);
		Brush softBg = isDark
			? LiquidGlassEngine.CreateSurfaceBrush(LiquidGlassVariant.Clear, isDark: true, opacityScale: 0.45)
			: new SolidColorBrush(Windows.UI.Color.FromArgb(220, 241, 245, 249));
		Brush accentSoftBg = new SolidColorBrush(isDark
			? Windows.UI.Color.FromArgb(85, 99, 102, 241)
			: Windows.UI.Color.FromArgb(255, 238, 242, 255));
		Brush accentSoftText = new SolidColorBrush(isDark
			? Windows.UI.Color.FromArgb(255, 199, 210, 254)
			: Windows.UI.Color.FromArgb(255, 79, 70, 229));
		Brush primaryText = new SolidColorBrush(isDark ? Colors.White : Windows.UI.Color.FromArgb(255, 15, 23, 42));
		Brush mutedText = new SolidColorBrush(isDark
			? Windows.UI.Color.FromArgb(255, 203, 213, 225)
			: Windows.UI.Color.FromArgb(255, 100, 116, 139));

		if (_roomRootGrid != null) _roomRootGrid.Background = rootBg;
		if (_roomWaveIconBorder != null) _roomWaveIconBorder.Background = accentSoftBg;
		if (_roomWaveIcon != null) _roomWaveIcon.Foreground = accentSoftText;

		if (_roomHeroCard != null)
		{
			_roomHeroCard.Background = cardBg;
			_roomHeroCard.BorderBrush = rimBrush;
			LiquidGlassEngine.AttachInteractiveLiquidGlass(_roomHeroCard, LiquidGlassVariant.Regular, isDark: isDark, enablePointerTracking: false);
		}
		if (_roomProBadge != null)
		{
			_roomProBadge.Background = softBg;
			_roomProBadge.BorderBrush = rimBrush;
		}
		Brush dividerBrush = GetRoomDividerBrush(isDark);
		foreach (var div in _roomDividers)
		{
			div.Fill = dividerBrush;
		}
		foreach (var node in _roomSignalPathNodes)
		{
			node.Background = softBg;
			node.BorderBrush = rimBrush;
		}
		foreach (var arrow in _roomSignalPathArrows)
		{
			arrow.Foreground = mutedText;
		}
		if (_roomActiveIrCard != null)
		{
			_roomActiveIrCard.Background = softBg;
			_roomActiveIrCard.BorderBrush = rimBrush;
		}
		if (_roomIrReimportBtn != null)
		{
			_roomIrReimportBtn.Background = softBg;
			_roomIrReimportBtn.BorderBrush = rimBrush;
			_roomIrReimportBtn.Foreground = primaryText;
			if (_roomIrReimportBtn.Content is TextBlock tb) tb.Foreground = primaryText;
		}
		if (_roomIrClearBtn != null)
		{
			ApplyRoomDangerButtonTheme(_roomIrClearBtn, isDark);
		}
		if (_roomTrimIcon != null) _roomTrimIcon.Foreground = accentSoftText;
		if (_roomTrimValueTb != null) _roomTrimValueTb.Foreground = accentSoftText;
		ApplyRoomClippingBannerTheme(isDark);

		foreach (var wBtn in _roomWorkflowCardButtons)
		{
			wBtn.Background = cardBg;
			wBtn.BorderBrush = rimBrush;
			wBtn.Foreground = primaryText;
			LiquidGlassEngine.AttachInteractiveLiquidGlass(wBtn, LiquidGlassVariant.Regular, isDark: isDark, enablePointerTracking: true);
		}
		if (_roomRewExpanderCard != null)
		{
			_roomRewExpanderCard.Background = cardBg;
			_roomRewExpanderCard.BorderBrush = rimBrush;
			LiquidGlassEngine.AttachInteractiveLiquidGlass(_roomRewExpanderCard, LiquidGlassVariant.Regular, isDark: isDark, enablePointerTracking: false);
		}
		if (_roomRewCodeBox != null)
		{
			_roomRewCodeBox.Background = softBg;
			_roomRewCodeBox.BorderBrush = rimBrush;
		}

		RefreshRoomCorrectionState();
		UpdateTrimPresetHighlights(ViewModel?.EchoConvolutionTrimDb ?? 0.0);
		RefreshWorkbenchTextContrast(_roomCorrectionWorkbench, isDark);
	}
}
