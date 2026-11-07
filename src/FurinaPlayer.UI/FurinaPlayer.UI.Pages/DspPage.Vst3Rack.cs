using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using FurinaPlayer.UI.Helpers;
using FurinaPlayer.UI.ViewModels;
using Microsoft.UI;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using SonicWave.Audio.Vst3;
using SonicWave.Core.Models;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.System;
using WinRT.Interop;

namespace FurinaPlayer.UI.Pages;

public sealed partial class DspPage
{
	private FrameworkElement? _vst3RackWorkbench;
	private TextBlock? _vst3NavSubTb;

	// Header Status Elements
	private Border? _vst3HeaderStatusPill;
	private Ellipse? _vst3HeaderStatusDot;
	private TextBlock? _vst3HeaderStatusTb;

	// Hero Signal Flow & Telemetry Elements
	private Border? _vst3NodeSlotBorder;
	private TextBlock? _vst3NodeSlotSubTb;
	private Border? _vst3NodeGuiBorder;
	private TextBlock? _vst3NodeGuiSubTb;
	private TextBlock? _vst3NodeInputSubTb;

	private TextBlock? _vst3ActiveNameTb;
	private Border? _vst3ActiveStateBadge;
	private TextBlock? _vst3ActiveStateBadgeTb;
	private TextBlock? _vst3ActiveVendorTb;
	private TextBlock? _vst3ActiveVersionTb;
	private TextBlock? _vst3ActiveHostStateTb;
	private TextBlock? _vst3ActiveSampleRateTb;
	private TextBlock? _vst3ActivePathTb;
	private TextBlock? _vst3LiveStatusTb;

	private Button? _vst3HeroOpenGuiBtn;
	private Button? _vst3HeroBypassBtn;
	private TextBlock? _vst3HeroBypassBtnTb;
	private Button? _vst3HeroUnloadAllBtn;

	// Left Card: Scan Folders
	private StackPanel? _vst3FoldersStack;
	private TextBlock? _vst3FolderCountBadgeTb;

	// Right Card: Discovered Plugins & Rack Slots
	private TextBox? _vst3SearchBox;
	private string _vst3SearchQuery = string.Empty;
	private string _vst3FilterMode = "all"; // "all" or "enabled"
	private Button? _vst3FilterAllBtn;
	private TextBlock? _vst3FilterAllTb;
	private Button? _vst3FilterEnabledBtn;
	private TextBlock? _vst3FilterEnabledTb;
	private TextBlock? _vst3TotalCountTb;
	private StackPanel? _vst3PluginSlotsStack;

	public FrameworkElement CreateVst3RackWorkbench()
	{
		if (_vst3RackWorkbench != null)
		{
			RefreshVst3RackUi();
			return _vst3RackWorkbench;
		}

		// Auto-scan once if plugin list is empty and scan folders exist
		if (ViewModel != null && ViewModel.Vst3Plugins.Count == 0 && ViewModel.Vst3Folders.Count > 0)
		{
			try
			{
				ViewModel.ScanVst3Command.Execute(null);
			}
			catch
			{
			}
		}

		var sv = new ScrollViewer
		{
			VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
			HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled
		};

		var root = new Grid
		{
			Background = CreateWhiteAcrylicBrush(0.72, 195),
			Padding = new Thickness(24, 20, 24, 28),
			RowSpacing = 16
		};

		root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // 0: Top Header Bar
		root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // 1: Hero Signal Chain & Active Slot Card
		root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // 2: Main 2-Column Content Grid

		// Row 0: Top Header Bar
		var headerBar = CreateVst3HeaderBar();
		Grid.SetRow(headerBar, 0);
		root.Children.Add(headerBar);

		// Row 1: Hero Card
		var heroCard = CreateVst3HeroCard();
		Grid.SetRow(heroCard, 1);
		root.Children.Add(heroCard);

		// Row 2: Main 2-Column Content Grid
		var mainGrid = CreateVst3MainContentGrid();
		Grid.SetRow(mainGrid, 2);
		root.Children.Add(mainGrid);

		sv.Content = root;
		_vst3RackWorkbench = sv;

		RefreshVst3RackUi();
		return _vst3RackWorkbench;
	}

	private FrameworkElement CreateVst3HeaderBar()
	{
		var headerGrid = new Grid { ColumnSpacing = 12 };
		headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.0, GridUnitType.Star) });
		headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

		// Left: Icon badge + Title + Subtitle
		var leftStack = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 12, VerticalAlignment = VerticalAlignment.Center };

		var iconBorder = new Border
		{
			Width = 40,
			Height = 40,
			CornerRadius = new CornerRadius(10),
			Background = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 238, 242, 255)),
			BorderBrush = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 199, 210, 254)),
			BorderThickness = new Thickness(1),
			Child = new FontIcon
			{
				Glyph = "\uE8B8",
				FontSize = 18,
				Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 79, 70, 229)),
				HorizontalAlignment = HorizontalAlignment.Center,
				VerticalAlignment = VerticalAlignment.Center
			}
		};
		leftStack.Children.Add(iconBorder);

		var titleStack = new StackPanel { Spacing = 2, VerticalAlignment = VerticalAlignment.Center };
		titleStack.Children.Add(new TextBlock
		{
			Text = "VST3 机架 / 插件管理器",
			FontSize = 20,
			FontWeight = FontWeights.Bold,
			Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 15, 23, 42))
		});
		titleStack.Children.Add(new TextBlock
		{
			Text = "Steinberg VST3 64位原生宿主 · 支持实时音频流宿主渲染与原生插件 GUI 窗口交互",
			FontSize = 12,
			Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 100, 116, 139))
		});
		leftStack.Children.Add(titleStack);

		Grid.SetColumn(leftStack, 0);
		headerGrid.Children.Add(leftStack);

		// Right: Status pill + Action buttons
		var rightStack = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8, VerticalAlignment = VerticalAlignment.Center };

		_vst3HeaderStatusPill = new Border
		{
			Background = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 238, 242, 255)),
			BorderBrush = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 199, 210, 254)),
			BorderThickness = new Thickness(1),
			CornerRadius = new CornerRadius(16),
			Padding = new Thickness(12, 5, 12, 5),
			VerticalAlignment = VerticalAlignment.Center
		};
		var pillRow = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 6, VerticalAlignment = VerticalAlignment.Center };
		_vst3HeaderStatusDot = new Ellipse
		{
			Width = 8,
			Height = 8,
			Fill = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 79, 70, 229)),
			VerticalAlignment = VerticalAlignment.Center
		};
		_vst3HeaderStatusTb = new TextBlock
		{
			Text = "宿主就绪",
			FontSize = 12,
			FontWeight = FontWeights.SemiBold,
			Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 79, 70, 229)),
			VerticalAlignment = VerticalAlignment.Center
		};
		pillRow.Children.Add(_vst3HeaderStatusDot);
		pillRow.Children.Add(_vst3HeaderStatusTb);
		_vst3HeaderStatusPill.Child = pillRow;
		rightStack.Children.Add(_vst3HeaderStatusPill);

		var btnScan = new Button
		{
			Content = "🔍 扫描目录",
			FontSize = 12,
			FontWeight = FontWeights.SemiBold,
			Padding = new Thickness(12, 6, 12, 6),
			CornerRadius = new CornerRadius(8),
			Background = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 255, 255, 255)),
			BorderBrush = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 203, 213, 225)),
			BorderThickness = new Thickness(1),
			Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 30, 41, 59))
		};
		btnScan.Click += (s, e) =>
		{
			ViewModel?.ScanVst3Command.Execute(null);
			RefreshVst3RackUi();
		};
		rightStack.Children.Add(btnScan);

		var btnLoadFile = new Button
		{
			Content = "➕ 加载 .vst3 文件",
			FontSize = 12,
			FontWeight = FontWeights.SemiBold,
			Padding = new Thickness(12, 6, 12, 6),
			CornerRadius = new CornerRadius(8),
			Background = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 79, 70, 229)),
			BorderBrush = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 67, 56, 202)),
			BorderThickness = new Thickness(1),
			Foreground = new SolidColorBrush(Colors.White)
		};
		btnLoadFile.Click += OnLoadSingleVst3FileClick;
		rightStack.Children.Add(btnLoadFile);

		var btnRefresh = new Button
		{
			Content = "🔄 刷新状态",
			FontSize = 12,
			FontWeight = FontWeights.Medium,
			Padding = new Thickness(12, 6, 12, 6),
			CornerRadius = new CornerRadius(8),
			Background = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 255, 255, 255)),
			BorderBrush = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 226, 232, 240)),
			BorderThickness = new Thickness(1),
			Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 71, 85, 105))
		};
		btnRefresh.Click += (s, e) => RefreshVst3RackUi();
		rightStack.Children.Add(btnRefresh);

		Grid.SetColumn(rightStack, 1);
		headerGrid.Children.Add(rightStack);

		return headerGrid;
	}

	private FrameworkElement CreateVst3HeroCard()
	{
		var card = new Border
		{
			Background = CreateAcrylicCardBrush(0.90, 238),
			BorderBrush = CreateLiquidGlassRimBrush(),
			BorderThickness = new Thickness(1),
			CornerRadius = new CornerRadius(14),
			Padding = new Thickness(22, 18, 22, 18)
		};

		var stack = new StackPanel { Spacing = 16 };

		// 1. Signal Chain Header + 5-Node Signal Flow
		var flowHeaderGrid = new Grid();
		flowHeaderGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.0, GridUnitType.Star) });
		flowHeaderGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

		var flowTitleSp = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8, VerticalAlignment = VerticalAlignment.Center };
		flowTitleSp.Children.Add(new TextBlock
		{
			Text = "实时 VST3 宿主信号链路 (Active VST3 Signal Chain)",
			FontSize = 13,
			FontWeight = FontWeights.Bold,
			Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 30, 41, 59))
		});
		var archBadge = new Border
		{
			Background = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 238, 242, 255)),
			BorderBrush = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 199, 210, 254)),
			BorderThickness = new Thickness(1),
			CornerRadius = new CornerRadius(6),
			Padding = new Thickness(7, 2, 7, 2),
			Child = new TextBlock
			{
				Text = "Steinberg IAudioProcessor / IEditController / IPlugView",
				FontSize = 10,
				FontWeight = FontWeights.SemiBold,
				Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 79, 70, 229))
			}
		};
		flowTitleSp.Children.Add(archBadge);
		Grid.SetColumn(flowTitleSp, 0);
		flowHeaderGrid.Children.Add(flowTitleSp);

		_vst3LiveStatusTb = new TextBlock
		{
			Text = ViewModel?.Vst3StatusText ?? "宿主就绪",
			FontSize = 11,
			Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 100, 116, 139)),
			VerticalAlignment = VerticalAlignment.Center
		};
		Grid.SetColumn(_vst3LiveStatusTb, 1);
		flowHeaderGrid.Children.Add(_vst3LiveStatusTb);
		stack.Children.Add(flowHeaderGrid);

		// 5-Node Signal Flow Diagram
		var nodesGrid = new Grid();
		for (int i = 0; i < 9; i++)
		{
			nodesGrid.ColumnDefinitions.Add(new ColumnDefinition
			{
				Width = (i % 2 == 0) ? new GridLength(1.0, GridUnitType.Star) : new GridLength(28, GridUnitType.Pixel)
			});
		}

		var node0 = CreateVst3FlowNode("\uE767", "音频解码输入", "PCM 32-bit Float", out _vst3NodeInputSubTb, out _);
		Grid.SetColumn(node0, 0);
		nodesGrid.Children.Add(node0);

		var arr0 = CreateVst3FlowArrow();
		Grid.SetColumn(arr0, 1);
		nodesGrid.Children.Add(arr0);

		var node1 = CreateVst3FlowNode("\uE995", "ECHO DSP 前级链路", "EQ / 压缩 / 空间处理", out _, out _);
		Grid.SetColumn(node1, 2);
		nodesGrid.Children.Add(node1);

		var arr1 = CreateVst3FlowArrow();
		Grid.SetColumn(arr1, 3);
		nodesGrid.Children.Add(arr1);

		var node2 = CreateVst3FlowNode("\uE8B8", "VST3 原生宿主插槽", "Active Slot (待命)", out _vst3NodeSlotSubTb, out _vst3NodeSlotBorder);
		Grid.SetColumn(node2, 4);
		nodesGrid.Children.Add(node2);

		var arr2 = CreateVst3FlowArrow();
		Grid.SetColumn(arr2, 5);
		nodesGrid.Children.Add(arr2);

		var node3 = CreateVst3FlowNode("\uE737", "原生 GUI 编辑器", "IPlugView 窗口", out _vst3NodeGuiSubTb, out _vst3NodeGuiBorder);
		Grid.SetColumn(node3, 6);
		nodesGrid.Children.Add(node3);

		var arr3 = CreateVst3FlowArrow();
		Grid.SetColumn(arr3, 7);
		nodesGrid.Children.Add(arr3);

		var node4 = CreateVst3FlowNode("\uE72E", "安全限幅输出", "防削波直通音频设备", out _, out _);
		Grid.SetColumn(node4, 8);
		nodesGrid.Children.Add(node4);

		stack.Children.Add(nodesGrid);

		// Divider
		stack.Children.Add(new Rectangle
		{
			Height = 1,
			Fill = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 226, 232, 240))
		});

		// 2. Active Plugin Telemetry & Quick Actions Row
		var telemetryGrid = new Grid { ColumnSpacing = 20 };
		telemetryGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.0, GridUnitType.Star) });
		telemetryGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

		// Left: Active Plugin Details
		var detailsStack = new StackPanel { Spacing = 8 };

		var activeTitleRow = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 10, VerticalAlignment = VerticalAlignment.Center };
		activeTitleRow.Children.Add(new TextBlock
		{
			Text = "当前挂载插件：",
			FontSize = 12,
			FontWeight = FontWeights.SemiBold,
			Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 100, 116, 139)),
			VerticalAlignment = VerticalAlignment.Center
		});
		_vst3ActiveNameTb = new TextBlock
		{
			Text = "未启用插件 (直通模式)",
			FontSize = 16,
			FontWeight = FontWeights.Bold,
			Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 15, 23, 42)),
			VerticalAlignment = VerticalAlignment.Center
		};
		activeTitleRow.Children.Add(_vst3ActiveNameTb);

		_vst3ActiveStateBadge = new Border
		{
			Background = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 241, 245, 249)),
			BorderBrush = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 203, 213, 225)),
			BorderThickness = new Thickness(1),
			CornerRadius = new CornerRadius(10),
			Padding = new Thickness(8, 2, 8, 2),
			VerticalAlignment = VerticalAlignment.Center
		};
		_vst3ActiveStateBadgeTb = new TextBlock
		{
			Text = "待命 (直通)",
			FontSize = 11,
			FontWeight = FontWeights.SemiBold,
			Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 71, 85, 105))
		};
		_vst3ActiveStateBadge.Child = _vst3ActiveStateBadgeTb;
		activeTitleRow.Children.Add(_vst3ActiveStateBadge);
		detailsStack.Children.Add(activeTitleRow);

		// Metadata badges row: Vendor | Version | Host Status | SampleRate
		var metaWrap = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 16 };
		metaWrap.Children.Add(CreateTelemetryMetric("厂商 (Vendor)", out _vst3ActiveVendorTb, "--"));
		metaWrap.Children.Add(CreateTelemetryMetric("版本 (Version)", out _vst3ActiveVersionTb, "--"));
		metaWrap.Children.Add(CreateTelemetryMetric("宿主状态 (Host)", out _vst3ActiveHostStateTb, "待命 (直通)"));
		metaWrap.Children.Add(CreateTelemetryMetric("采样率与声道", out _vst3ActiveSampleRateTb, "48000 Hz / Stereo"));
		detailsStack.Children.Add(metaWrap);

		_vst3ActivePathTb = new TextBlock
		{
			Text = "插件路径：可在下方机架列表中启用任意 VST3 插件，或直接点击“🎛️ 界面”一键挂载并打开原生窗口。",
			FontSize = 11,
			Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 100, 116, 139)),
			TextTrimming = TextTrimming.CharacterEllipsis
		};
		detailsStack.Children.Add(_vst3ActivePathTb);

		Grid.SetColumn(detailsStack, 0);
		telemetryGrid.Children.Add(detailsStack);

		// Right: Quick Action Buttons for Active Plugin
		var quickActionsStack = new StackPanel
		{
			Orientation = Orientation.Horizontal,
			Spacing = 8,
			VerticalAlignment = VerticalAlignment.Center
		};

		_vst3HeroOpenGuiBtn = new Button
		{
			Content = "🎛️ 打开选中/活动插件原生界面",
			FontSize = 12,
			FontWeight = FontWeights.SemiBold,
			Padding = new Thickness(14, 8, 14, 8),
			CornerRadius = new CornerRadius(8),
			Background = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 79, 70, 229)),
			BorderBrush = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 67, 56, 202)),
			BorderThickness = new Thickness(1),
			Foreground = new SolidColorBrush(Colors.White)
		};
		_vst3HeroOpenGuiBtn.Click += OnHeroOpenActiveGuiClick;
		quickActionsStack.Children.Add(_vst3HeroOpenGuiBtn);

		_vst3HeroBypassBtn = new Button
		{
			Padding = new Thickness(12, 8, 12, 8),
			CornerRadius = new CornerRadius(8),
			Background = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 255, 255, 255)),
			BorderBrush = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 203, 213, 225)),
			BorderThickness = new Thickness(1)
		};
		_vst3HeroBypassBtnTb = new TextBlock
		{
			Text = "⏸ 旁通当前插件",
			FontSize = 12,
			FontWeight = FontWeights.SemiBold,
			Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 51, 65, 85))
		};
		_vst3HeroBypassBtn.Content = _vst3HeroBypassBtnTb;
		_vst3HeroBypassBtn.Click += OnHeroToggleBypassClick;
		quickActionsStack.Children.Add(_vst3HeroBypassBtn);

		_vst3HeroUnloadAllBtn = new Button
		{
			Content = "⏹ 全部停用 (卸载宿主)",
			FontSize = 12,
			FontWeight = FontWeights.SemiBold,
			Padding = new Thickness(12, 8, 12, 8),
			CornerRadius = new CornerRadius(8),
			Background = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 254, 242, 242)),
			BorderBrush = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 254, 202, 202)),
			BorderThickness = new Thickness(1),
			Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 220, 38, 38))
		};
		_vst3HeroUnloadAllBtn.Click += OnHeroUnloadAllClick;
		quickActionsStack.Children.Add(_vst3HeroUnloadAllBtn);

		Grid.SetColumn(quickActionsStack, 1);
		telemetryGrid.Children.Add(quickActionsStack);

		stack.Children.Add(telemetryGrid);
		card.Child = stack;
		return card;
	}

	private static Border CreateVst3FlowNode(string glyph, string title, string subtitle, out TextBlock subTb, out Border borderRef)
	{
		var border = new Border
		{
			Background = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 248, 250, 252)),
			BorderBrush = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 226, 232, 240)),
			BorderThickness = new Thickness(1),
			CornerRadius = new CornerRadius(10),
			Padding = new Thickness(10, 8, 10, 8)
		};
		borderRef = border;

		var row = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8, VerticalAlignment = VerticalAlignment.Center };
		var iconBox = new Border
		{
			Width = 28,
			Height = 28,
			CornerRadius = new CornerRadius(7),
			Background = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 238, 242, 255)),
			Child = new FontIcon
			{
				Glyph = glyph,
				FontSize = 13,
				Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 79, 70, 229)),
				HorizontalAlignment = HorizontalAlignment.Center,
				VerticalAlignment = VerticalAlignment.Center
			}
		};
		row.Children.Add(iconBox);

		var textStack = new StackPanel { Spacing = 1, VerticalAlignment = VerticalAlignment.Center };
		textStack.Children.Add(new TextBlock
		{
			Text = title,
			FontSize = 12,
			FontWeight = FontWeights.Bold,
			Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 30, 41, 59)),
			TextTrimming = TextTrimming.CharacterEllipsis
		});
		subTb = new TextBlock
		{
			Text = subtitle,
			FontSize = 10,
			Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 100, 116, 139)),
			TextTrimming = TextTrimming.CharacterEllipsis
		};
		textStack.Children.Add(subTb);
		row.Children.Add(textStack);

		border.Child = row;
		return border;
	}

	private static FrameworkElement CreateVst3FlowArrow()
	{
		return new FontIcon
		{
			Glyph = "\uE76C",
			FontSize = 12,
			Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 148, 163, 184)),
			HorizontalAlignment = HorizontalAlignment.Center,
			VerticalAlignment = VerticalAlignment.Center
		};
	}

	private static StackPanel CreateTelemetryMetric(string label, out TextBlock valueTb, string defaultVal)
	{
		var sp = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 4 };
		sp.Children.Add(new TextBlock
		{
			Text = label + "：",
			FontSize = 11,
			Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 100, 116, 139))
		});
		valueTb = new TextBlock
		{
			Text = defaultVal,
			FontSize = 11,
			FontWeight = FontWeights.SemiBold,
			Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 30, 41, 59))
		};
		sp.Children.Add(valueTb);
		return sp;
	}

	private FrameworkElement CreateVst3MainContentGrid()
	{
		var grid = new Grid { ColumnSpacing = 16 };
		grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(330, GridUnitType.Pixel) });
		grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.0, GridUnitType.Star) });

		var leftCard = CreateVst3ScanFoldersCard();
		Grid.SetColumn(leftCard, 0);
		grid.Children.Add(leftCard);

		var rightCard = CreateVst3PluginRackCard();
		Grid.SetColumn(rightCard, 1);
		grid.Children.Add(rightCard);

		return grid;
	}

	private FrameworkElement CreateVst3ScanFoldersCard()
	{
		var card = new Border
		{
			Background = CreateAcrylicCardBrush(0.90, 238),
			BorderBrush = CreateLiquidGlassRimBrush(),
			BorderThickness = new Thickness(1),
			CornerRadius = new CornerRadius(14),
			Padding = new Thickness(18, 16, 18, 18),
			VerticalAlignment = VerticalAlignment.Top
		};

		var stack = new StackPanel { Spacing = 14 };

		// Card Header
		var headerGrid = new Grid();
		headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.0, GridUnitType.Star) });
		headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

		var titleStack = new StackPanel { Spacing = 2 };
		titleStack.Children.Add(new TextBlock
		{
			Text = "扫描目录管理",
			FontSize = 15,
			FontWeight = FontWeights.Bold,
			Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 15, 23, 42))
		});
		titleStack.Children.Add(new TextBlock
		{
			Text = "管理 VST3 插件搜索目录（无高度截断）",
			FontSize = 11,
			Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 100, 116, 139))
		});
		Grid.SetColumn(titleStack, 0);
		headerGrid.Children.Add(titleStack);

		var countBadge = new Border
		{
			Background = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 241, 245, 249)),
			BorderBrush = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 226, 232, 240)),
			BorderThickness = new Thickness(1),
			CornerRadius = new CornerRadius(12),
			Padding = new Thickness(8, 2, 8, 2),
			VerticalAlignment = VerticalAlignment.Top
		};
		_vst3FolderCountBadgeTb = new TextBlock
		{
			Text = "0 个目录",
			FontSize = 11,
			FontWeight = FontWeights.SemiBold,
			Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 71, 85, 105))
		};
		countBadge.Child = _vst3FolderCountBadgeTb;
		Grid.SetColumn(countBadge, 1);
		headerGrid.Children.Add(countBadge);
		stack.Children.Add(headerGrid);

		// Action Buttons
		var actionsStack = new StackPanel { Spacing = 8 };

		var btnScanAll = new Button
		{
			Content = "⚡ 立即扫描全部目录",
			HorizontalAlignment = HorizontalAlignment.Stretch,
			FontSize = 12,
			FontWeight = FontWeights.SemiBold,
			Padding = new Thickness(12, 8, 12, 8),
			CornerRadius = new CornerRadius(8),
			Background = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 79, 70, 229)),
			BorderBrush = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 67, 56, 202)),
			BorderThickness = new Thickness(1),
			Foreground = new SolidColorBrush(Colors.White)
		};
		btnScanAll.Click += (s, e) =>
		{
			ViewModel?.ScanVst3Command.Execute(null);
			RefreshVst3RackUi();
		};
		actionsStack.Children.Add(btnScanAll);

		var subButtonsGrid = new Grid { ColumnSpacing = 8 };
		subButtonsGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.0, GridUnitType.Star) });
		subButtonsGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.0, GridUnitType.Star) });

		var btnAddFolder = new Button
		{
			Content = "📁 添加扫描目录",
			HorizontalAlignment = HorizontalAlignment.Stretch,
			FontSize = 12,
			FontWeight = FontWeights.Medium,
			Padding = new Thickness(8, 7, 8, 7),
			CornerRadius = new CornerRadius(8),
			Background = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 255, 255, 255)),
			BorderBrush = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 203, 213, 225)),
			BorderThickness = new Thickness(1),
			Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 30, 41, 59))
		};
		btnAddFolder.Click += OnAddVst3FolderAndRefreshClick;
		Grid.SetColumn(btnAddFolder, 0);
		subButtonsGrid.Children.Add(btnAddFolder);

		var btnRestoreDefault = new Button
		{
			Content = "恢复默认目录",
			HorizontalAlignment = HorizontalAlignment.Stretch,
			FontSize = 12,
			FontWeight = FontWeights.Medium,
			Padding = new Thickness(8, 7, 8, 7),
			CornerRadius = new CornerRadius(8),
			Background = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 248, 250, 252)),
			BorderBrush = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 226, 232, 240)),
			BorderThickness = new Thickness(1),
			Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 71, 85, 105))
		};
		btnRestoreDefault.Click += (s, e) =>
		{
			ViewModel?.RestoreDefaultVst3Folders();
			RefreshVst3RackUi();
		};
		Grid.SetColumn(btnRestoreDefault, 1);
		subButtonsGrid.Children.Add(btnRestoreDefault);

		actionsStack.Children.Add(subButtonsGrid);
		stack.Children.Add(actionsStack);

		// Divider
		stack.Children.Add(new Rectangle
		{
			Height = 1,
			Fill = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 226, 232, 240))
		});

		// Folder list container (Scrollable, NO MaxHeight=108 restriction)
		var folderScroll = new ScrollViewer
		{
			VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
			HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled
		};
		_vst3FoldersStack = new StackPanel { Spacing = 8 };
		folderScroll.Content = _vst3FoldersStack;
		stack.Children.Add(folderScroll);

		card.Child = stack;
		return card;
	}

	private FrameworkElement CreateVst3PluginRackCard()
	{
		var card = new Border
		{
			Background = CreateAcrylicCardBrush(0.90, 238),
			BorderBrush = CreateLiquidGlassRimBrush(),
			BorderThickness = new Thickness(1),
			CornerRadius = new CornerRadius(14),
			Padding = new Thickness(20, 16, 20, 20),
			VerticalAlignment = VerticalAlignment.Top
		};

		var stack = new StackPanel { Spacing = 14 };

		// Top Filter Bar
		var filterGrid = new Grid { ColumnSpacing = 12 };
		filterGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.0, GridUnitType.Star) });
		filterGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
		filterGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

		_vst3SearchBox = new TextBox
		{
			PlaceholderText = "搜索插件名称、厂商 (如 Insight, Melodyne, FabFilter, iZotope)...",
			FontSize = 12,
			CornerRadius = new CornerRadius(8),
			VerticalAlignment = VerticalAlignment.Center
		};
		_vst3SearchBox.TextChanged += (s, e) =>
		{
			_vst3SearchQuery = _vst3SearchBox.Text?.Trim() ?? string.Empty;
			RebuildVst3PluginSlotsList();
		};
		Grid.SetColumn(_vst3SearchBox, 0);
		filterGrid.Children.Add(_vst3SearchBox);

		// Filter Pills: 全部插件 | 已启用
		var filterPillsStack = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 6, VerticalAlignment = VerticalAlignment.Center };

		_vst3FilterAllBtn = new Button
		{
			Padding = new Thickness(12, 6, 12, 6),
			CornerRadius = new CornerRadius(16),
			BorderThickness = new Thickness(1)
		};
		_vst3FilterAllTb = new TextBlock { Text = "全部插件 (0)", FontSize = 12, FontWeight = FontWeights.SemiBold };
		_vst3FilterAllBtn.Content = _vst3FilterAllTb;
		_vst3FilterAllBtn.Click += (s, e) =>
		{
			_vst3FilterMode = "all";
			RefreshVst3RackUi();
		};
		filterPillsStack.Children.Add(_vst3FilterAllBtn);

		_vst3FilterEnabledBtn = new Button
		{
			Padding = new Thickness(12, 6, 12, 6),
			CornerRadius = new CornerRadius(16),
			BorderThickness = new Thickness(1)
		};
		_vst3FilterEnabledTb = new TextBlock { Text = "已启用 (0)", FontSize = 12, FontWeight = FontWeights.SemiBold };
		_vst3FilterEnabledBtn.Content = _vst3FilterEnabledTb;
		_vst3FilterEnabledBtn.Click += (s, e) =>
		{
			_vst3FilterMode = "enabled";
			RefreshVst3RackUi();
		};
		filterPillsStack.Children.Add(_vst3FilterEnabledBtn);

		Grid.SetColumn(filterPillsStack, 1);
		filterGrid.Children.Add(filterPillsStack);

		_vst3TotalCountTb = new TextBlock
		{
			Text = "共发现 0 个 VST3 插件",
			FontSize = 12,
			FontWeight = FontWeights.Medium,
			Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 100, 116, 139)),
			VerticalAlignment = VerticalAlignment.Center
		};
		Grid.SetColumn(_vst3TotalCountTb, 2);
		filterGrid.Children.Add(_vst3TotalCountTb);

		stack.Children.Add(filterGrid);

		// Divider
		stack.Children.Add(new Rectangle
		{
			Height = 1,
			Fill = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 226, 232, 240))
		});

		// Plugin Rack Slot List (NO MaxHeight=140 restriction!)
		var listScroll = new ScrollViewer
		{
			VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
			HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled
		};
		_vst3PluginSlotsStack = new StackPanel { Spacing = 8 };
		listScroll.Content = _vst3PluginSlotsStack;
		stack.Children.Add(listScroll);

		card.Child = stack;
		return card;
	}

	public void RefreshVst3RackUi()
	{
		try
		{
			UpdateVst3SidebarSubtitle();
			if (_vst3RackWorkbench == null || ViewModel == null)
			{
				return;
			}

			var plugins = ViewModel.Vst3Plugins;
			int totalCount = plugins.Count;
			int enabledCount = plugins.Count(p => p != null && p.Enabled);
			Vst3PluginState? activePlugin = plugins.FirstOrDefault(p => p != null && p.IsActive)
				?? plugins.FirstOrDefault(p => p != null && p.Enabled);

			bool hostLoaded = Vst3NativeHost.Instance.IsLoaded;
			bool editorOpen = _vst3EditorWindow != null;
			int sampleRate = (ViewModel.EngineSampleRate > 0) ? ViewModel.EngineSampleRate : 48000;

			// 1. Update Top Header Status Pill
			if (_vst3HeaderStatusTb != null && _vst3HeaderStatusDot != null && _vst3HeaderStatusPill != null)
			{
				if (activePlugin != null && activePlugin.IsActive)
				{
					_vst3HeaderStatusTb.Text = $"正在运行: {activePlugin.Name}";
					_vst3HeaderStatusTb.Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 21, 128, 61));
					_vst3HeaderStatusDot.Fill = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 34, 197, 94));
					_vst3HeaderStatusPill.Background = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 240, 253, 244));
					_vst3HeaderStatusPill.BorderBrush = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 187, 247, 208));
				}
				else if (activePlugin != null && activePlugin.Bypass)
				{
					_vst3HeaderStatusTb.Text = $"已旁通: {activePlugin.Name}";
					_vst3HeaderStatusTb.Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 180, 83, 9));
					_vst3HeaderStatusDot.Fill = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 245, 158, 11));
					_vst3HeaderStatusPill.Background = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 254, 252, 232));
					_vst3HeaderStatusPill.BorderBrush = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 253, 230, 138));
				}
				else
				{
					_vst3HeaderStatusTb.Text = $"宿主就绪 · 已发现 {totalCount} 个插件";
					_vst3HeaderStatusTb.Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 79, 70, 229));
					_vst3HeaderStatusDot.Fill = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 79, 70, 229));
					_vst3HeaderStatusPill.Background = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 238, 242, 255));
					_vst3HeaderStatusPill.BorderBrush = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 199, 210, 254));
				}
			}

			// 2. Update Hero Card Signal Chain & Telemetry
			if (_vst3NodeInputSubTb != null)
			{
				_vst3NodeInputSubTb.Text = $"{sampleRate} Hz / Stereo";
			}
			if (_vst3NodeSlotSubTb != null && _vst3NodeSlotBorder != null)
			{
				if (activePlugin != null && activePlugin.IsActive)
				{
					_vst3NodeSlotSubTb.Text = activePlugin.Name;
					_vst3NodeSlotBorder.Background = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 240, 253, 244));
					_vst3NodeSlotBorder.BorderBrush = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 134, 239, 172));
				}
				else if (activePlugin != null && activePlugin.Bypass)
				{
					_vst3NodeSlotSubTb.Text = $"{activePlugin.Name} (已旁通)";
					_vst3NodeSlotBorder.Background = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 254, 252, 232));
					_vst3NodeSlotBorder.BorderBrush = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 253, 224, 71));
				}
				else
				{
					_vst3NodeSlotSubTb.Text = "Active Slot (直通待命)";
					_vst3NodeSlotBorder.Background = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 248, 250, 252));
					_vst3NodeSlotBorder.BorderBrush = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 226, 232, 240));
				}
			}
			if (_vst3NodeGuiSubTb != null && _vst3NodeGuiBorder != null)
			{
				if (editorOpen)
				{
					_vst3NodeGuiSubTb.Text = "原生窗口已打开 (Active)";
					_vst3NodeGuiBorder.Background = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 238, 242, 255));
					_vst3NodeGuiBorder.BorderBrush = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 165, 180, 252));
				}
				else
				{
					_vst3NodeGuiSubTb.Text = "点击 [🎛️ 界面] 随时唤起";
					_vst3NodeGuiBorder.Background = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 248, 250, 252));
					_vst3NodeGuiBorder.BorderBrush = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 226, 232, 240));
				}
			}

			if (_vst3LiveStatusTb != null)
			{
				_vst3LiveStatusTb.Text = ViewModel.Vst3StatusText;
			}

			if (_vst3ActiveNameTb != null && _vst3ActiveStateBadge != null && _vst3ActiveStateBadgeTb != null)
			{
				if (activePlugin != null)
				{
					_vst3ActiveNameTb.Text = activePlugin.Name;
					if (activePlugin.IsActive)
					{
						_vst3ActiveStateBadgeTb.Text = "实时运行中";
						_vst3ActiveStateBadgeTb.Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 21, 128, 61));
						_vst3ActiveStateBadge.Background = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 220, 252, 231));
						_vst3ActiveStateBadge.BorderBrush = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 134, 239, 172));
					}
					else
					{
						_vst3ActiveStateBadgeTb.Text = "已旁通 (Bypass)";
						_vst3ActiveStateBadgeTb.Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 180, 83, 9));
						_vst3ActiveStateBadge.Background = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 254, 243, 199));
						_vst3ActiveStateBadge.BorderBrush = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 252, 211, 77));
					}
				}
				else
				{
					_vst3ActiveNameTb.Text = "未启用插件 (直通模式)";
					_vst3ActiveStateBadgeTb.Text = "待命 (直通)";
					_vst3ActiveStateBadgeTb.Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 71, 85, 105));
					_vst3ActiveStateBadge.Background = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 241, 245, 249));
					_vst3ActiveStateBadge.BorderBrush = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 203, 213, 225));
				}
			}

			if (_vst3ActiveVendorTb != null)
			{
				_vst3ActiveVendorTb.Text = activePlugin?.Vendor ?? "--";
			}
			if (_vst3ActiveVersionTb != null)
			{
				_vst3ActiveVersionTb.Text = activePlugin != null ? $"v{activePlugin.Version}" : "--";
			}
			if (_vst3ActiveHostStateTb != null)
			{
				_vst3ActiveHostStateTb.Text = hostLoaded ? "原生宿主已挂载 (Native Active)" : "待命 (直通)";
			}
			if (_vst3ActiveSampleRateTb != null)
			{
				_vst3ActiveSampleRateTb.Text = $"{sampleRate} Hz / Stereo";
			}
			if (_vst3ActivePathTb != null)
			{
				_vst3ActivePathTb.Text = activePlugin != null
					? $"插件路径：{activePlugin.Path}"
					: "插件路径：可在下方机架列表中启用任意 VST3 插件，或直接点击“🎛️ 界面”一键挂载并打开原生窗口。";
			}

			if (_vst3HeroBypassBtnTb != null)
			{
				_vst3HeroBypassBtnTb.Text = (activePlugin != null && activePlugin.Bypass) ? "▶ 恢复插件处理" : "⏸ 旁通当前插件";
			}

			// 3. Filter Pills Appearance
			if (_vst3FilterAllTb != null) _vst3FilterAllTb.Text = $"全部插件 ({totalCount})";
			if (_vst3FilterEnabledTb != null) _vst3FilterEnabledTb.Text = $"已启用 ({enabledCount})";
			if (_vst3TotalCountTb != null) _vst3TotalCountTb.Text = $"共发现 {totalCount} 个 VST3 插件";

			UpdateFilterButtonVisual(_vst3FilterAllBtn, _vst3FilterAllTb, _vst3FilterMode == "all");
			UpdateFilterButtonVisual(_vst3FilterEnabledBtn, _vst3FilterEnabledTb, _vst3FilterMode == "enabled");

			// 4. Rebuild Left Scan Folders List & Right Plugin Rack Slots List
			RebuildVst3FoldersList();
			RebuildVst3PluginSlotsList();
		}
		catch (Exception ex)
		{
			Debug.WriteLine("RefreshVst3RackUi error: " + ex.Message);
		}
	}

	private static void UpdateFilterButtonVisual(Button? btn, TextBlock? tb, bool isSelected)
	{
		if (btn == null || tb == null) return;
		if (isSelected)
		{
			btn.Background = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 238, 242, 255));
			btn.BorderBrush = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 165, 180, 252));
			tb.Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 79, 70, 229));
		}
		else
		{
			btn.Background = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 248, 250, 252));
			btn.BorderBrush = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 226, 232, 240));
			tb.Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 100, 116, 139));
		}
	}

	private void RebuildVst3FoldersList()
	{
		if (_vst3FoldersStack == null || ViewModel == null) return;
		_vst3FoldersStack.Children.Clear();

		var folders = ViewModel.Vst3Folders;
		if (_vst3FolderCountBadgeTb != null)
		{
			_vst3FolderCountBadgeTb.Text = $"{folders.Count} 个目录";
		}

		if (folders.Count == 0)
		{
			var emptyBorder = new Border
			{
				Background = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 248, 250, 252)),
				BorderBrush = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 226, 232, 240)),
				BorderThickness = new Thickness(1),
				CornerRadius = new CornerRadius(10),
				Padding = new Thickness(14, 16, 14, 16),
				Child = new TextBlock
				{
					Text = "暂未配置扫描目录。可点击“恢复默认目录”或“📁 添加扫描目录”。",
					FontSize = 12,
					Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 100, 116, 139)),
					TextWrapping = TextWrapping.Wrap
				}
			};
			_vst3FoldersStack.Children.Add(emptyBorder);
			return;
		}

		foreach (string folder in folders.ToList())
		{
			string currentFolder = folder;
			bool exists = Directory.Exists(currentFolder);

			var rowBorder = new Border
			{
				Background = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 248, 250, 252)),
				BorderBrush = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 226, 232, 240)),
				BorderThickness = new Thickness(1),
				CornerRadius = new CornerRadius(10),
				Padding = new Thickness(12, 10, 12, 10)
			};

			var rowStack = new StackPanel { Spacing = 8 };

			var topRow = new Grid { ColumnSpacing = 8 };
			topRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
			topRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.0, GridUnitType.Star) });

			var folderIcon = new FontIcon
			{
				Glyph = "\uE8B7",
				FontSize = 15,
				Foreground = new SolidColorBrush(exists
					? Windows.UI.Color.FromArgb(255, 79, 70, 229)
					: Windows.UI.Color.FromArgb(255, 148, 163, 184)),
				VerticalAlignment = VerticalAlignment.Top,
				Margin = new Thickness(0, 2, 0, 0)
			};
			Grid.SetColumn(folderIcon, 0);
			topRow.Children.Add(folderIcon);

			var pathTb = new TextBlock
			{
				Text = currentFolder,
				FontSize = 12,
				FontWeight = FontWeights.Medium,
				Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 30, 41, 59)),
				TextWrapping = TextWrapping.Wrap
			};
			ToolTipService.SetToolTip(pathTb, currentFolder);
			Grid.SetColumn(pathTb, 1);
			topRow.Children.Add(pathTb);
			rowStack.Children.Add(topRow);

			var btnRow = new StackPanel
			{
				Orientation = Orientation.Horizontal,
				Spacing = 6,
				HorizontalAlignment = HorizontalAlignment.Right
			};

			var btnOpenDir = new Button
			{
				Content = "打开文件夹",
				FontSize = 11,
				Padding = new Thickness(8, 3, 8, 3),
				CornerRadius = new CornerRadius(6),
				Background = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 255, 255, 255)),
				BorderBrush = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 203, 213, 225)),
				BorderThickness = new Thickness(1),
				Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 51, 65, 85))
			};
			btnOpenDir.Click += async (s, e) =>
			{
				try
				{
					if (Directory.Exists(currentFolder))
					{
						await Launcher.LaunchFolderPathAsync(currentFolder);
					}
					else if (ViewModel != null)
					{
						ViewModel.Vst3StatusText = "该目录当前不存在：" + currentFolder;
						RefreshVst3RackUi();
					}
				}
				catch (Exception ex)
				{
					Debug.WriteLine("Open folder failed: " + ex.Message);
				}
			};
			btnRow.Children.Add(btnOpenDir);

			var btnRemoveDir = new Button
			{
				Padding = new Thickness(8, 3, 8, 3),
				CornerRadius = new CornerRadius(6),
				Background = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 254, 242, 242)),
				BorderBrush = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 254, 202, 202)),
				BorderThickness = new Thickness(1)
			};
			var removeContent = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 4 };
			removeContent.Children.Add(new FontIcon
			{
				Glyph = "\uE74D",
				FontSize = 11,
				Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 220, 38, 38))
			});
			removeContent.Children.Add(new TextBlock
			{
				Text = "移除",
				FontSize = 11,
				Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 220, 38, 38))
			});
			btnRemoveDir.Content = removeContent;
			btnRemoveDir.Click += (s, e) =>
			{
				ViewModel?.RemoveVst3FolderCommand.Execute(currentFolder);
				RefreshVst3RackUi();
			};
			btnRow.Children.Add(btnRemoveDir);

			rowStack.Children.Add(btnRow);
			rowBorder.Child = rowStack;
			_vst3FoldersStack.Children.Add(rowBorder);
		}
	}

	private void RebuildVst3PluginSlotsList()
	{
		if (_vst3PluginSlotsStack == null || ViewModel == null) return;
		_vst3PluginSlotsStack.Children.Clear();

		IEnumerable<Vst3PluginState> query = ViewModel.Vst3Plugins.Where(p => p != null);
		if (_vst3FilterMode == "enabled")
		{
			query = query.Where(p => p.Enabled);
		}
		if (!string.IsNullOrWhiteSpace(_vst3SearchQuery))
		{
			string q = _vst3SearchQuery;
			query = query.Where(p =>
				(p.Name != null && p.Name.Contains(q, StringComparison.OrdinalIgnoreCase)) ||
				(p.Vendor != null && p.Vendor.Contains(q, StringComparison.OrdinalIgnoreCase)) ||
				(p.Path != null && p.Path.Contains(q, StringComparison.OrdinalIgnoreCase)));
		}

		List<Vst3PluginState> filtered = query.ToList();
		if (filtered.Count == 0)
		{
			var emptyCard = new Border
			{
				Background = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 248, 250, 252)),
				BorderBrush = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 226, 232, 240)),
				BorderThickness = new Thickness(1),
				CornerRadius = new CornerRadius(12),
				Padding = new Thickness(24, 28, 24, 28),
				Child = new StackPanel
				{
					Spacing = 8,
					HorizontalAlignment = HorizontalAlignment.Center,
					Children =
					{
						new TextBlock
						{
							Text = ViewModel.Vst3Plugins.Count == 0 ? "尚未发现任何 VST3 插件" : "没有符合当前筛选或搜索条件的插件",
							FontSize = 14,
							FontWeight = FontWeights.Bold,
							Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 51, 65, 85)),
							HorizontalAlignment = HorizontalAlignment.Center
						},
						new TextBlock
						{
							Text = ViewModel.Vst3Plugins.Count == 0
								? "点击左侧“⚡ 立即扫描全部目录”自动发现系统中的 Steinberg VST3 插件，或点击顶部“➕ 加载 .vst3 文件”直接载入。"
								: "请尝试清空搜索关键词或切换回“全部插件”视图。",
							FontSize = 12,
							Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 100, 116, 139)),
							HorizontalAlignment = HorizontalAlignment.Center,
							TextWrapping = TextWrapping.Wrap
						}
					}
				}
			};
			_vst3PluginSlotsStack.Children.Add(emptyCard);
			return;
		}

		for (int i = 0; i < filtered.Count; i++)
		{
			Vst3PluginState plugin = filtered[i];
			int slotIndex = i + 1;
			_vst3PluginSlotsStack.Children.Add(CreateVst3RackSlotCard(plugin, slotIndex));
		}
	}

	private FrameworkElement CreateVst3RackSlotCard(Vst3PluginState plugin, int slotIndex)
	{
		bool isActive = plugin.IsActive;
		bool isBypassed = plugin.Enabled && plugin.Bypass;
		bool isEditorOpenForThis = _vst3EditorWindow != null &&
			string.Equals(_vst3EditorWindow.PluginPath, plugin.Path, StringComparison.OrdinalIgnoreCase);

		var slotBorder = new Border
		{
			Background = isActive
				? new SolidColorBrush(Windows.UI.Color.FromArgb(255, 245, 247, 255))
				: new SolidColorBrush(Windows.UI.Color.FromArgb(255, 255, 255, 255)),
			BorderBrush = isActive
				? new SolidColorBrush(Windows.UI.Color.FromArgb(255, 165, 180, 252))
				: new SolidColorBrush(Windows.UI.Color.FromArgb(255, 226, 232, 240)),
			BorderThickness = new Thickness(isActive ? 1.5 : 1),
			CornerRadius = new CornerRadius(12),
			Padding = new Thickness(14, 12, 14, 12)
		};

		var rowGrid = new Grid { ColumnSpacing = 14 };
		rowGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto }); // Left: Slot badge + LED
		rowGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.0, GridUnitType.Star) }); // Middle: Name, Vendor, Path
		rowGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto }); // Right: Controls

		// Left: Slot index badge (#01, #02...) + Status LED dot
		var leftStack = new StackPanel
		{
			Orientation = Orientation.Horizontal,
			Spacing = 8,
			VerticalAlignment = VerticalAlignment.Center
		};

		var indexBadge = new Border
		{
			Background = isActive
				? new SolidColorBrush(Windows.UI.Color.FromArgb(255, 79, 70, 229))
				: new SolidColorBrush(Windows.UI.Color.FromArgb(255, 241, 245, 249)),
			CornerRadius = new CornerRadius(6),
			Padding = new Thickness(7, 3, 7, 3),
			VerticalAlignment = VerticalAlignment.Center,
			Child = new TextBlock
			{
				Text = $"#{slotIndex:D2}",
				FontSize = 11,
				FontWeight = FontWeights.Bold,
				Foreground = isActive
					? new SolidColorBrush(Colors.White)
					: new SolidColorBrush(Windows.UI.Color.FromArgb(255, 71, 85, 105))
			}
		};
		leftStack.Children.Add(indexBadge);

		Windows.UI.Color ledColor = isActive
			? Windows.UI.Color.FromArgb(255, 34, 197, 94) // #22C55E emerald
			: (isBypassed
				? Windows.UI.Color.FromArgb(255, 245, 158, 11) // #F59E0B amber
				: Windows.UI.Color.FromArgb(255, 148, 163, 184)); // #94A3B8 slate

		var ledDot = new Ellipse
		{
			Width = 9,
			Height = 9,
			Fill = new SolidColorBrush(ledColor),
			VerticalAlignment = VerticalAlignment.Center
		};
		ToolTipService.SetToolTip(ledDot, isActive ? "处理中 (Active)" : (isBypassed ? "已启用但旁通 (Bypassed)" : "未启用"));
		leftStack.Children.Add(ledDot);

		Grid.SetColumn(leftStack, 0);
		rowGrid.Children.Add(leftStack);

		// Middle: Plugin Name + Status Badge + Subtitle
		var midStack = new StackPanel { Spacing = 3, VerticalAlignment = VerticalAlignment.Center };

		var nameRow = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8, VerticalAlignment = VerticalAlignment.Center };
		nameRow.Children.Add(new TextBlock
		{
			Text = string.IsNullOrWhiteSpace(plugin.Name) ? System.IO.Path.GetFileNameWithoutExtension(plugin.Path) : plugin.Name,
			FontSize = 14,
			FontWeight = FontWeights.Bold,
			Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 15, 23, 42)),
			VerticalAlignment = VerticalAlignment.Center,
			TextTrimming = TextTrimming.CharacterEllipsis
		});

		if (isActive)
		{
			nameRow.Children.Add(new Border
			{
				Background = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 220, 252, 231)),
				BorderBrush = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 134, 239, 172)),
				BorderThickness = new Thickness(1),
				CornerRadius = new CornerRadius(6),
				Padding = new Thickness(6, 1, 6, 1),
				VerticalAlignment = VerticalAlignment.Center,
				Child = new TextBlock
				{
					Text = isEditorOpenForThis ? "运行中 · 界面已开" : "运行中",
					FontSize = 10,
					FontWeight = FontWeights.Bold,
					Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 21, 128, 61))
				}
			});
		}
		else if (isBypassed)
		{
			nameRow.Children.Add(new Border
			{
				Background = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 254, 243, 199)),
				BorderBrush = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 252, 211, 77)),
				BorderThickness = new Thickness(1),
				CornerRadius = new CornerRadius(6),
				Padding = new Thickness(6, 1, 6, 1),
				VerticalAlignment = VerticalAlignment.Center,
				Child = new TextBlock
				{
					Text = "已旁通",
					FontSize = 10,
					FontWeight = FontWeights.Bold,
					Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 180, 83, 9))
				}
			});
		}
		midStack.Children.Add(nameRow);

		string fileName = string.Empty;
		try { fileName = System.IO.Path.GetFileName(plugin.Path); } catch { fileName = plugin.Path; }
		string vendor = string.IsNullOrWhiteSpace(plugin.Vendor) ? "VST3 Native Plugin" : plugin.Vendor;
		string version = string.IsNullOrWhiteSpace(plugin.Version) ? "1.0.0" : plugin.Version;

		var subInfoTb = new TextBlock
		{
			Text = $"{vendor} · v{version} · {fileName}",
			FontSize = 11,
			Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 100, 116, 139)),
			TextTrimming = TextTrimming.CharacterEllipsis
		};
		ToolTipService.SetToolTip(subInfoTb, plugin.Path);
		midStack.Children.Add(subInfoTb);

		Grid.SetColumn(midStack, 1);
		rowGrid.Children.Add(midStack);

		// Right: Studio Controls (Enable ToggleSwitch, Bypass Button, Open GUI Button)
		var rightControls = new StackPanel
		{
			Orientation = Orientation.Horizontal,
			Spacing = 8,
			VerticalAlignment = VerticalAlignment.Center
		};

		// 1. Enable Pill Button / Toggle
		var btnEnable = new Button
		{
			Content = plugin.Enabled ? "● 已启用" : "○ 启用",
			FontSize = 12,
			FontWeight = FontWeights.SemiBold,
			Padding = new Thickness(12, 6, 12, 6),
			CornerRadius = new CornerRadius(8),
			Background = plugin.Enabled
				? new SolidColorBrush(Windows.UI.Color.FromArgb(255, 220, 252, 231))
				: new SolidColorBrush(Windows.UI.Color.FromArgb(255, 248, 250, 252)),
			BorderBrush = plugin.Enabled
				? new SolidColorBrush(Windows.UI.Color.FromArgb(255, 134, 239, 172))
				: new SolidColorBrush(Windows.UI.Color.FromArgb(255, 203, 213, 225)),
			BorderThickness = new Thickness(1),
			Foreground = plugin.Enabled
				? new SolidColorBrush(Windows.UI.Color.FromArgb(255, 21, 128, 61))
				: new SolidColorBrush(Windows.UI.Color.FromArgb(255, 51, 65, 85))
		};
		btnEnable.Click += (s, e) => TogglePluginEnabled(plugin);
		rightControls.Children.Add(btnEnable);

		// 2. Bypass Button
		var btnBypass = new Button
		{
			Content = plugin.Bypass ? "已旁通" : "旁通",
			FontSize = 12,
			FontWeight = FontWeights.Medium,
			Padding = new Thickness(10, 6, 10, 6),
			CornerRadius = new CornerRadius(8),
			Background = plugin.Bypass
				? new SolidColorBrush(Windows.UI.Color.FromArgb(255, 254, 243, 199))
				: new SolidColorBrush(Windows.UI.Color.FromArgb(255, 255, 255, 255)),
			BorderBrush = plugin.Bypass
				? new SolidColorBrush(Windows.UI.Color.FromArgb(255, 252, 211, 77))
				: new SolidColorBrush(Windows.UI.Color.FromArgb(255, 226, 232, 240)),
			BorderThickness = new Thickness(1),
			Foreground = plugin.Bypass
				? new SolidColorBrush(Windows.UI.Color.FromArgb(255, 180, 83, 9))
				: new SolidColorBrush(Windows.UI.Color.FromArgb(255, 71, 85, 105))
		};
		btnBypass.Click += (s, e) =>
		{
			if (!plugin.Enabled)
			{
				plugin.Enabled = true;
				plugin.Bypass = true;
			}
			else
			{
				plugin.Bypass = !plugin.Bypass;
			}
			ViewModel?.ApplyVst3Command.Execute(null);
			if (ViewModel != null)
			{
				ViewModel.Vst3StatusText = plugin.Bypass
					? $"已旁通插件：{plugin.Name}"
					: $"已恢复插件处理：{plugin.Name}";
			}
			RefreshVst3RackUi();
		};
		rightControls.Children.Add(btnBypass);

		// 3. Open Native GUI Button ("🎛️ 界面")
		var btnGui = new Button
		{
			Content = "🎛️ 界面",
			FontSize = 12,
			FontWeight = FontWeights.SemiBold,
			Padding = new Thickness(12, 6, 12, 6),
			CornerRadius = new CornerRadius(8),
			Background = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 79, 70, 229)),
			BorderBrush = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 67, 56, 202)),
			BorderThickness = new Thickness(1),
			Foreground = new SolidColorBrush(Colors.White)
		};
		btnGui.Click += (s, e) => OpenVst3EditorForPlugin(plugin);
		rightControls.Children.Add(btnGui);

		Grid.SetColumn(rightControls, 2);
		rowGrid.Children.Add(rightControls);

		slotBorder.Child = rowGrid;
		return slotBorder;
	}

	private void TogglePluginEnabled(Vst3PluginState plugin)
	{
		if (ViewModel == null) return;
		bool targetState = !plugin.Enabled;
		if (targetState)
		{
			// Single active native host slot: disable others so the selected plugin takes the active slot cleanly
			foreach (var item in ViewModel.Vst3Plugins)
			{
				if (item != null && item != plugin)
				{
					item.Enabled = false;
					item.Bypass = false;
				}
			}
			plugin.Enabled = true;
			plugin.Bypass = false;
			ViewModel.Vst3StatusText = $"已启用 VST3 插件：{plugin.Name} ({plugin.Vendor})";
		}
		else
		{
			plugin.Enabled = false;
			plugin.Bypass = false;
			if (_vst3EditorWindow != null && string.Equals(_vst3EditorWindow.PluginPath, plugin.Path, StringComparison.OrdinalIgnoreCase))
			{
				CloseVst3Editor();
			}
			ViewModel.Vst3StatusText = $"已停用 VST3 插件：{plugin.Name}";
		}
		ViewModel.ApplyVst3Command.Execute(null);
		RefreshVst3RackUi();
	}

	public void OpenVst3EditorForPlugin(Vst3PluginState tag)
	{
		if (tag == null || ViewModel == null) return;
		try
		{
			if (_vst3EditorWindow != null && string.Equals(_vst3EditorWindow.PluginPath, tag.Path, StringComparison.OrdinalIgnoreCase))
			{
				_vst3EditorWindow.BringToFront();
				RefreshVst3RackUi();
				return;
			}
			CloseVst3Editor();
			int sampleRate = (ViewModel.EngineSampleRate > 0) ? ViewModel.EngineSampleRate : 48000;
			Vst3EditorWindow? editor = Vst3EditorWindow.Open(tag.Path, sampleRate, 2, out string error);
			if (editor == null)
			{
				ViewModel.Vst3StatusText = "打开插件界面失败：" + error;
				RefreshVst3RackUi();
				return;
			}
			_vst3EditorWindow = editor;
			editor.Closed += () => OnVst3EditorClosed(editor);
			foreach (Vst3PluginState vst3Plugin in ViewModel.Vst3Plugins)
			{
				if (vst3Plugin != null && vst3Plugin != tag)
				{
					vst3Plugin.Enabled = false;
					vst3Plugin.Bypass = false;
				}
			}
			tag.Enabled = true;
			tag.Bypass = false;
			ViewModel.ApplyVst3Command.Execute(null);
			ViewModel.Vst3StatusText = "已打开插件原生界面：" + tag.Name + "，实时处理当前音频流";
			RefreshVst3RackUi();
		}
		catch (Exception ex)
		{
			ViewModel.Vst3StatusText = "打开插件界面异常：" + ex.Message;
			RefreshVst3RackUi();
		}
	}

	private void OnHeroOpenActiveGuiClick(object sender, RoutedEventArgs e)
	{
		if (ViewModel == null) return;
		Vst3PluginState? target = ViewModel.Vst3Plugins.FirstOrDefault(p => p != null && p.IsActive)
			?? ViewModel.Vst3Plugins.FirstOrDefault(p => p != null && p.Enabled)
			?? ViewModel.Vst3Plugins.FirstOrDefault(p => p != null);
		if (target == null)
		{
			ViewModel.Vst3StatusText = "当前列表中暂无可用插件，请先扫描目录或加载 .vst3 文件";
			RefreshVst3RackUi();
			return;
		}
		OpenVst3EditorForPlugin(target);
	}

	private void OnHeroToggleBypassClick(object sender, RoutedEventArgs e)
	{
		if (ViewModel == null) return;
		Vst3PluginState? active = ViewModel.Vst3Plugins.FirstOrDefault(p => p != null && p.Enabled);
		if (active == null)
		{
			ViewModel.Vst3StatusText = "当前没有已启用的插件可供旁通";
			RefreshVst3RackUi();
			return;
		}
		active.Bypass = !active.Bypass;
		ViewModel.ApplyVst3Command.Execute(null);
		ViewModel.Vst3StatusText = active.Bypass
			? $"已旁通当前插件：{active.Name}"
			: $"已恢复当前插件处理：{active.Name}";
		RefreshVst3RackUi();
	}

	private void OnHeroUnloadAllClick(object sender, RoutedEventArgs e)
	{
		if (ViewModel == null) return;
		CloseVst3Editor();
		foreach (var p in ViewModel.Vst3Plugins)
		{
			if (p != null)
			{
				p.Enabled = false;
				p.Bypass = false;
			}
		}
		Vst3NativeHost.Instance.Unload();
		ViewModel.ApplyVst3Command.Execute(null);
		ViewModel.Vst3StatusText = "已全部停用并卸载 VST3 原生宿主，音频恢复直通";
		RefreshVst3RackUi();
	}

	private async void OnAddVst3FolderAndRefreshClick(object sender, RoutedEventArgs e)
	{
		try
		{
			FolderPicker folderPicker = new FolderPicker
			{
				FileTypeFilter = { "*" },
				SuggestedStartLocation = PickerLocationId.ComputerFolder
			};
			nint windowHandle = WindowNative.GetWindowHandle(WindowManager.CurrentWindow);
			InitializeWithWindow.Initialize(folderPicker, windowHandle);
			StorageFolder storageFolder = await folderPicker.PickSingleFolderAsync();
			if (storageFolder != null && ViewModel != null)
			{
				ViewModel.AddVst3FolderCommand.Execute(storageFolder.Path);
				ViewModel.ScanVst3Command.Execute(null);
				RefreshVst3RackUi();
			}
		}
		catch (Exception ex)
		{
			if (ViewModel != null)
			{
				ViewModel.Vst3StatusText = "添加目录失败：" + ex.Message;
			}
			RefreshVst3RackUi();
		}
	}

	private async void OnLoadSingleVst3FileClick(object sender, RoutedEventArgs e)
	{
		try
		{
			FileOpenPicker picker = new FileOpenPicker
			{
				FileTypeFilter = { ".vst3", ".dll" },
				SuggestedStartLocation = PickerLocationId.ComputerFolder
			};
			nint windowHandle = WindowNative.GetWindowHandle(WindowManager.CurrentWindow);
			InitializeWithWindow.Initialize(picker, windowHandle);
			StorageFile file = await picker.PickSingleFileAsync();
			if (file != null && ViewModel != null)
			{
				Vst3PluginState? plugin = ViewModel.AddOrSelectVst3PluginFile(file.Path);
				if (plugin != null)
				{
					OpenVst3EditorForPlugin(plugin);
				}
				else
				{
					RefreshVst3RackUi();
				}
			}
		}
		catch (Exception ex)
		{
			if (ViewModel != null)
			{
				ViewModel.Vst3StatusText = "加载 .vst3 文件失败：" + ex.Message;
			}
			RefreshVst3RackUi();
		}
	}

	public void UpdateVst3SidebarSubtitle()
	{
		if (_vst3NavSubTb == null || ViewModel == null) return;
		Vst3PluginState? activePlugin = ViewModel.Vst3Plugins.FirstOrDefault(p => p != null && p.IsActive);
		if (activePlugin != null)
		{
			_vst3NavSubTb.Text = "已启用 · " + activePlugin.Name;
		}
		else if (ViewModel.Vst3Plugins.Count > 0)
		{
			_vst3NavSubTb.Text = $"已就绪 · {ViewModel.Vst3Plugins.Count} 个插件";
		}
		else
		{
			_vst3NavSubTb.Text = "原生插件宿主";
		}
	}
}
