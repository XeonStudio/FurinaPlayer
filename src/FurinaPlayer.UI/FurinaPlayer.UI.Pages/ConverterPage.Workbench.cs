using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using FurinaPlayer.Core.Services;
using FurinaPlayer.UI.Controls;
using Microsoft.UI;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Windows.Storage.Pickers;
using Windows.UI;
using WinRT.Interop;

namespace FurinaPlayer.UI.Pages;

public sealed partial class ConverterPage
{
	private Grid? _toolboxHostGrid;
	private Border? _toolboxSidebar;
	private TextBlock? _toolboxBadgeTb;
	private TextBlock? _toolboxBrandTb;
	private TextBlock? _toolboxSubtitleTb;
	private readonly List<TextBlock> _toolboxSectionHeaders = new();
	private Button? _navConverterBtn;
	private Button? _navWorkbenchBtn;
	private FrameworkElement? _workbenchView;
	private string _activeToolboxTab = "converter";

	private sealed class WorkbenchTrackItem
	{
		public string FilePath { get; set; } = string.Empty;
		public string FileName => Path.GetFileName(FilePath);
		public string Title { get; set; } = string.Empty;
		public string Artist { get; set; } = string.Empty;
		public string Album { get; set; } = string.Empty;
		public int TrackNumber { get; set; }
		public int Year { get; set; }
		public string Genre { get; set; } = string.Empty;
		public bool HasEmbeddedArt { get; set; }
		public bool IsSelected { get; set; }
	}

	private readonly List<WorkbenchTrackItem> _wbTracks = new();
	private string _wbSearch = string.Empty;
	private StackPanel? _wbListPanel;
	private TextBlock? _wbStatsTb;
	private TextBlock? _wbStatusTb;

	private TextBox? _wbTitleBox;
	private TextBox? _wbArtistBox;
	private TextBox? _wbAlbumBox;
	private TextBox? _wbTrackNumBox;
	private TextBox? _wbYearBox;
	private TextBox? _wbGenreBox;
	private TextBox? _wbFindBox;
	private TextBox? _wbReplaceBox;
	private ComboBox? _wbPatternCombo;

	private UIElement BuildToolboxShell(FrameworkElement converterContent)
	{
		var shell = new Grid();
		shell.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(236) });
		shell.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

		_toolboxSidebar = new Border
		{
			Padding = new Thickness(12, 16, 12, 12),
			BorderThickness = new Thickness(0, 0, 1.2, 0)
		};

		var sideGrid = new Grid();
		sideGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
		sideGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

		// Header modeled on DSP Settings sidebar
		var hdrStack = new StackPanel { Spacing = 2, Margin = new Thickness(6, 0, 0, 14) };
		_toolboxBadgeTb = new TextBlock
		{
			Text = "TOOLBOX",
			FontSize = 10,
			FontWeight = FontWeights.Bold
		};
		hdrStack.Children.Add(_toolboxBadgeTb);
		_toolboxBrandTb = new TextBlock
		{
			Text = "FurinaPlayer",
			FontSize = 20,
			FontWeight = FontWeights.ExtraBold
		};
		hdrStack.Children.Add(_toolboxBrandTb);
		_toolboxSubtitleTb = new TextBlock
		{
			Text = Loc.T("音频工具集"),
			FontSize = 11
		};
		hdrStack.Children.Add(_toolboxSubtitleTb);
		Grid.SetRow(hdrStack, 0);
		sideGrid.Children.Add(hdrStack);

		_toolboxSectionHeaders.Clear();
		TextBlock CreateToolboxSectionHeader(string title, double topMargin)
		{
			var tb = new TextBlock
			{
				Text = title,
				FontSize = 11,
				FontWeight = FontWeights.Bold,
				Margin = new Thickness(8, topMargin, 0, 4)
			};
			_toolboxSectionHeaders.Add(tb);
			return tb;
		}

		var sideStack = new StackPanel { Spacing = 4 };
		sideStack.Children.Add(CreateToolboxSectionHeader(Loc.T("格式与标签"), 2));
		_navConverterBtn = CreateToolboxNavButton("\uE8AB", Loc.T("格式转换"), Loc.T("NCM / MGG 解密与无损转码"), "converter");
		_navWorkbenchBtn = CreateToolboxNavButton("\uE90F", Loc.T("工作台"), Loc.T("批量标签清洗与文件整理"), "workbench");
		sideStack.Children.Add(_navConverterBtn);
		sideStack.Children.Add(_navWorkbenchBtn);

		sideStack.Children.Add(CreateToolboxSectionHeader(Loc.T("声学与听感"), 12));
		_navHearingBtn = CreateToolboxNavButton("\uE7F6", Loc.T("听力检测与黄金听感"), Loc.T("纯音测听与 EQ 补偿联动"), "hearing");
		sideStack.Children.Add(_navHearingBtn);

		var sideScroll = new ScrollViewer
		{
			VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
			HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
			Content = sideStack
		};
		Grid.SetRow(sideScroll, 1);
		sideGrid.Children.Add(sideScroll);

		_toolboxSidebar.Child = sideGrid;
		Grid.SetColumn(_toolboxSidebar, 0);
		shell.Children.Add(_toolboxSidebar);

		_toolboxHostGrid = new Grid();
		_toolboxHostGrid.Children.Add(converterContent);
		Grid.SetColumn(_toolboxHostGrid, 1);
		shell.Children.Add(_toolboxHostGrid);

		UpdateToolboxNavStyles();
		return shell;
	}

	private Button CreateToolboxNavButton(string glyph, string label, string subtitle, string tabKey)
	{
		var grid = new Grid { ColumnSpacing = 10 };
		grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
		grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
		grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

		var icon = new FontIcon { Glyph = glyph, FontSize = 16, VerticalAlignment = VerticalAlignment.Center };
		Grid.SetColumn(icon, 0);
		grid.Children.Add(icon);

		var textStack = new StackPanel { Spacing = 1, VerticalAlignment = VerticalAlignment.Center };
		textStack.Children.Add(new TextBlock { Text = label, FontSize = 13.5, FontWeight = FontWeights.SemiBold, TextTrimming = TextTrimming.CharacterEllipsis });
		textStack.Children.Add(new TextBlock { Text = subtitle, FontSize = 11, Opacity = 0.68, TextTrimming = TextTrimming.CharacterEllipsis });
		Grid.SetColumn(textStack, 1);
		grid.Children.Add(textStack);

		var checkTb = new TextBlock { Text = "✓", FontSize = 12, FontWeight = FontWeights.Bold, Foreground = new SolidColorBrush(Color.FromArgb(255, 52, 211, 153)), VerticalAlignment = VerticalAlignment.Center, Visibility = Visibility.Collapsed };
		Grid.SetColumn(checkTb, 2);
		grid.Children.Add(checkTb);

		var btn = new Button
		{
			Tag = checkTb,
			Content = grid,
			HorizontalAlignment = HorizontalAlignment.Stretch,
			HorizontalContentAlignment = HorizontalAlignment.Stretch,
			Padding = new Thickness(12, 10, 12, 10),
			CornerRadius = new CornerRadius(12),
			BorderThickness = new Thickness(1)
		};
		btn.Click += (_, _) => SwitchToolboxTab(tabKey);
		return btn;
	}

	private void SwitchToolboxTab(string tabKey)
	{
		if (_toolboxHostGrid == null) return;
		_activeToolboxTab = tabKey;
		_toolboxHostGrid.Children.Clear();
		if (tabKey == "workbench")
		{
			_workbenchView ??= CreateWorkbenchView();
			_toolboxHostGrid.Children.Add(_workbenchView);
			if (_wbTracks.Count == 0)
			{
				_ = LoadLibraryTracksIntoWorkbenchAsync();
			}
		}
		else if (tabKey == "hearing")
		{
			_hearingView ??= CreateHearingTestView();
			_toolboxHostGrid.Children.Add(_hearingView);
		}
		else
		{
			_toolboxHostGrid.Children.Add(_rootGrid);
		}
		UpdateToolboxNavStyles();
	}

	private void UpdateToolboxNavStyles()
	{
		bool isDark = LiquidGlassEngine.IsElementDarkTheme(this);
		if (_toolboxSidebar != null)
		{
			LiquidGlassEngine.AttachInteractiveLiquidGlass(_toolboxSidebar, LiquidGlassVariant.FloatingBar, isDark: isDark, enablePointerTracking: true);
		}
		if (_toolboxBadgeTb != null)
		{
			_toolboxBadgeTb.Foreground = new SolidColorBrush(isDark
				? Color.FromArgb(255, 165, 180, 252)
				: Color.FromArgb(255, 99, 102, 241));
		}
		if (_toolboxBrandTb != null)
		{
			_toolboxBrandTb.Foreground = new SolidColorBrush(isDark
				? Colors.White
				: Color.FromArgb(255, 15, 23, 42));
		}
		var subHeaderBrush = new SolidColorBrush(isDark
			? Color.FromArgb(255, 226, 232, 240)
			: Color.FromArgb(255, 71, 85, 105));
		if (_toolboxSubtitleTb != null)
		{
			_toolboxSubtitleTb.Foreground = subHeaderBrush;
		}
		foreach (var hdr in _toolboxSectionHeaders)
		{
			hdr.Foreground = subHeaderBrush;
		}

		void StyleBtn(Button? b, bool active)
		{
			if (b == null) return;
			if (active)
			{
				b.Background = LiquidGlassEngine.CreateSurfaceBrush(LiquidGlassVariant.AccentLensed, isDark: isDark, 0.95);
				b.BorderBrush = LiquidGlassEngine.CreateSpecularRimBrush(LiquidGlassVariant.AccentLensed, isDark: isDark, isHovered: true, isSelected: true);
				b.BorderThickness = new Thickness(1);
			}
			else
			{
				b.Background = new SolidColorBrush(Colors.Transparent);
				b.BorderBrush = new SolidColorBrush(Colors.Transparent);
				b.BorderThickness = new Thickness(0);
			}

			if (b.Content is Grid g)
			{
				if (g.Children.Count > 0 && g.Children[0] is FontIcon ic)
				{
					ic.Foreground = new SolidColorBrush(active
						? (isDark ? Color.FromArgb(255, 224, 231, 255) : Color.FromArgb(255, 79, 70, 229))
						: (isDark ? Color.FromArgb(255, 203, 213, 225) : Color.FromArgb(255, 100, 116, 139)));
				}
				if (g.Children.Count > 1 && g.Children[1] is StackPanel sp)
				{
					if (sp.Children.Count > 0 && sp.Children[0] is TextBlock t1)
					{
						t1.Foreground = new SolidColorBrush(active
							? (isDark ? Colors.White : Color.FromArgb(255, 67, 56, 202))
							: (isDark ? Colors.White : Color.FromArgb(255, 30, 41, 59)));
					}
					if (sp.Children.Count > 1 && sp.Children[1] is TextBlock t2)
					{
						t2.Foreground = new SolidColorBrush(active
							? (isDark ? Color.FromArgb(255, 203, 213, 225) : Color.FromArgb(255, 79, 70, 229))
							: (isDark ? Color.FromArgb(255, 203, 213, 225) : Color.FromArgb(255, 100, 116, 139)));
					}
				}
			}
			if (b.Tag is TextBlock checkTb)
			{
				checkTb.Foreground = new SolidColorBrush(isDark
					? Color.FromArgb(255, 52, 211, 153)
					: Color.FromArgb(255, 16, 185, 129));
				checkTb.Visibility = active ? Visibility.Visible : Visibility.Collapsed;
			}
		}
		StyleBtn(_navConverterBtn, _activeToolboxTab == "converter");
		StyleBtn(_navWorkbenchBtn, _activeToolboxTab == "workbench");
		StyleBtn(_navHearingBtn, _activeToolboxTab == "hearing");
	}

	private FrameworkElement CreateWorkbenchView()
	{
		var grid = new Grid { Padding = new Thickness(24, 18, 24, 24), RowSpacing = 14, ColumnSpacing = 16 };
		grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
		grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
		grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.45, GridUnitType.Star) });
		grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.0, GridUnitType.Star) });

		// Header Bar
		var headerCard = CreateCardBorder(new Thickness(18, 14, 18, 14));
		var hGrid = new Grid { ColumnSpacing = 12 };
		hGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
		hGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

		var hLeft = new StackPanel { Spacing = 3 };
		hLeft.Children.Add(new TextBlock
		{
			Text = Loc.T("工作台 · 批量标签与音频元数据整理"),
			FontSize = 20,
			FontWeight = FontWeights.Bold
		});
		_wbStatsTb = new TextBlock
		{
			Text = Loc.T("点击“从音乐库载入”或添加本地音频文件开始批量处理"),
			FontSize = 12.5,
			Foreground = new SolidColorBrush(Color.FromArgb(255, 92, 98, 114))
		};
		hLeft.Children.Add(_wbStatsTb);
		Grid.SetColumn(hLeft, 0);
		hGrid.Children.Add(hLeft);

		var hBtns = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8, VerticalAlignment = VerticalAlignment.Center };
		var loadLibBtn = new Button { Content = Loc.T("从音乐库载入"), CornerRadius = new CornerRadius(8), Padding = new Thickness(12, 7, 12, 7) };
		loadLibBtn.Click += async (_, _) => await LoadLibraryTracksIntoWorkbenchAsync();
		var addFilesBtn = new Button { Content = Loc.T("添加音频文件"), CornerRadius = new CornerRadius(8), Padding = new Thickness(12, 7, 12, 7) };
		addFilesBtn.Click += async (_, _) => await PickWorkbenchFilesAsync();
		var checkBtn = new Button { Content = Loc.T("一致性检查"), CornerRadius = new CornerRadius(8), Padding = new Thickness(12, 7, 12, 7) };
		checkBtn.Click += (_, _) => RunWorkbenchConsistencyCheck();
		hBtns.Children.Add(loadLibBtn);
		hBtns.Children.Add(addFilesBtn);
		hBtns.Children.Add(checkBtn);
		Grid.SetColumn(hBtns, 1);
		hGrid.Children.Add(hBtns);
		headerCard.Child = hGrid;
		Grid.SetRow(headerCard, 0);
		Grid.SetColumnSpan(headerCard, 2);
		grid.Children.Add(headerCard);

		// Left: Track Table
		var leftCard = CreateCardBorder(new Thickness(14));
		var leftGrid = new Grid { RowSpacing = 10 };
		leftGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
		leftGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

		var filterRow = new Grid { ColumnSpacing = 8 };
		filterRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
		filterRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
		filterRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

		var searchBox = new TextBox
		{
			PlaceholderText = Loc.T("筛选歌名、歌手、专辑或文件名..."),
			CornerRadius = new CornerRadius(8)
		};
		searchBox.TextChanged += (_, _) =>
		{
			_wbSearch = searchBox.Text?.Trim() ?? string.Empty;
			RebuildWorkbenchList();
		};
		Grid.SetColumn(searchBox, 0);
		filterRow.Children.Add(searchBox);

		var selectAllBtn = new Button { Content = Loc.T("全选"), CornerRadius = new CornerRadius(8), Padding = new Thickness(10, 6, 10, 6) };
		selectAllBtn.Click += (_, _) =>
		{
			foreach (var item in GetFilteredWorkbenchTracks()) item.IsSelected = true;
			RebuildWorkbenchList();
		};
		Grid.SetColumn(selectAllBtn, 1);
		filterRow.Children.Add(selectAllBtn);

		var clearSelBtn = new Button { Content = Loc.T("取消"), CornerRadius = new CornerRadius(8), Padding = new Thickness(10, 6, 10, 6) };
		clearSelBtn.Click += (_, _) =>
		{
			foreach (var item in _wbTracks) item.IsSelected = false;
			RebuildWorkbenchList();
		};
		Grid.SetColumn(clearSelBtn, 2);
		filterRow.Children.Add(clearSelBtn);

		Grid.SetRow(filterRow, 0);
		leftGrid.Children.Add(filterRow);

		_wbListPanel = new StackPanel { Spacing = 6 };
		var leftScroll = new ScrollViewer
		{
			VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
			Content = _wbListPanel
		};
		Grid.SetRow(leftScroll, 1);
		leftGrid.Children.Add(leftScroll);
		leftCard.Child = leftGrid;
		Grid.SetRow(leftCard, 1);
		Grid.SetColumn(leftCard, 0);
		grid.Children.Add(leftCard);

		// Right: Inspector (Batch Tag Editor + Action Groups + Filename<->Tag)
		var rightCard = CreateCardBorder(new Thickness(16));
		var rightScroll = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
		var rightStack = new StackPanel { Spacing = 14 };

		rightStack.Children.Add(new TextBlock { Text = Loc.T("批量标签编辑 (留空项保持原值)"), FontSize = 15, FontWeight = FontWeights.SemiBold });

		_wbTitleBox = new TextBox { Header = Loc.T("标题 (Title)"), PlaceholderText = Loc.T("保持不变"), CornerRadius = new CornerRadius(8) };
		_wbArtistBox = new TextBox { Header = Loc.T("艺术家 (Artist)"), PlaceholderText = Loc.T("保持不变"), CornerRadius = new CornerRadius(8) };
		_wbAlbumBox = new TextBox { Header = Loc.T("专辑 (Album)"), PlaceholderText = Loc.T("保持不变"), CornerRadius = new CornerRadius(8) };

		var numRow = new Grid { ColumnSpacing = 8 };
		numRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
		numRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
		numRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.4, GridUnitType.Star) });
		_wbTrackNumBox = new TextBox { Header = Loc.T("音轨号"), PlaceholderText = "-", CornerRadius = new CornerRadius(8) };
		_wbYearBox = new TextBox { Header = Loc.T("年份"), PlaceholderText = "-", CornerRadius = new CornerRadius(8) };
		_wbGenreBox = new TextBox { Header = Loc.T("流派"), PlaceholderText = "-", CornerRadius = new CornerRadius(8) };
		Grid.SetColumn(_wbTrackNumBox, 0);
		Grid.SetColumn(_wbYearBox, 1);
		Grid.SetColumn(_wbGenreBox, 2);
		numRow.Children.Add(_wbTrackNumBox);
		numRow.Children.Add(_wbYearBox);
		numRow.Children.Add(_wbGenreBox);

		rightStack.Children.Add(_wbTitleBox);
		rightStack.Children.Add(_wbArtistBox);
		rightStack.Children.Add(_wbAlbumBox);
		rightStack.Children.Add(numRow);

		var applyTagsBtn = new Button
		{
			Content = Loc.T("保存选中曲目标签"),
			HorizontalAlignment = HorizontalAlignment.Stretch,
			CornerRadius = new CornerRadius(8),
			Padding = new Thickness(12, 9, 12, 9),
			Background = new SolidColorBrush(Color.FromArgb(255, 37, 99, 235)),
			Foreground = new SolidColorBrush(Colors.White),
			FontWeight = FontWeights.SemiBold
		};
		applyTagsBtn.Click += async (_, _) => await ApplyBatchTagsAsync();
		rightStack.Children.Add(applyTagsBtn);

		// Action Groups
		rightStack.Children.Add(new Border { Height = 1, Background = new SolidColorBrush(Color.FromArgb(24, 0, 0, 0)), Margin = new Thickness(0, 4, 0, 4) });
		rightStack.Children.Add(new TextBlock { Text = Loc.T("动作组 (批量清洗选中曲目)"), FontSize = 15, FontWeight = FontWeights.SemiBold });

		var actionRow = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 6 };
		foreach (var act in new[] { ("trim", "去空格"), ("upper", "全部大写"), ("lower", "全部小写"), ("title", "首字母大写") })
		{
			var b = new Button { Content = Loc.T(act.Item2), CornerRadius = new CornerRadius(8), Padding = new Thickness(9, 6, 9, 6), FontSize = 12 };
			string mode = act.Item1;
			b.Click += async (_, _) => await RunActionGroupAsync(mode);
			actionRow.Children.Add(b);
		}
		rightStack.Children.Add(actionRow);

		var replaceRow = new Grid { ColumnSpacing = 6 };
		replaceRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
		replaceRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
		replaceRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
		_wbFindBox = new TextBox { PlaceholderText = Loc.T("查找文本"), CornerRadius = new CornerRadius(8) };
		_wbReplaceBox = new TextBox { PlaceholderText = Loc.T("替换为"), CornerRadius = new CornerRadius(8) };
		var replaceBtn = new Button { Content = Loc.T("替换"), CornerRadius = new CornerRadius(8), Padding = new Thickness(10, 6, 10, 6) };
		replaceBtn.Click += async (_, _) => await RunTextReplaceAsync();
		Grid.SetColumn(_wbFindBox, 0);
		Grid.SetColumn(_wbReplaceBox, 1);
		Grid.SetColumn(replaceBtn, 2);
		replaceRow.Children.Add(_wbFindBox);
		replaceRow.Children.Add(_wbReplaceBox);
		replaceRow.Children.Add(replaceBtn);
		rightStack.Children.Add(replaceRow);

		// Filename <-> Tag
		rightStack.Children.Add(new Border { Height = 1, Background = new SolidColorBrush(Color.FromArgb(24, 0, 0, 0)), Margin = new Thickness(0, 4, 0, 4) });
		rightStack.Children.Add(new TextBlock { Text = Loc.T("文件名与标签互转"), FontSize = 15, FontWeight = FontWeights.SemiBold });

		_wbPatternCombo = new ComboBox { HorizontalAlignment = HorizontalAlignment.Stretch, CornerRadius = new CornerRadius(8) };
		_wbPatternCombo.Items.Add("{Artist} - {Title}");
		_wbPatternCombo.Items.Add("{Track} - {Title}");
		_wbPatternCombo.Items.Add("{Artist} - {Album} - {Title}");
		_wbPatternCombo.SelectedIndex = 0;
		rightStack.Children.Add(_wbPatternCombo);

		var fnBtns = new Grid { ColumnSpacing = 8 };
		fnBtns.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
		fnBtns.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
		var extractBtn = new Button { Content = Loc.T("从文件名提取标签"), HorizontalAlignment = HorizontalAlignment.Stretch, CornerRadius = new CornerRadius(8), Padding = new Thickness(10, 7, 10, 7) };
		extractBtn.Click += async (_, _) => await ExtractTagsFromFileNameAsync();
		var renameBtn = new Button { Content = Loc.T("按标签重命名文件"), HorizontalAlignment = HorizontalAlignment.Stretch, CornerRadius = new CornerRadius(8), Padding = new Thickness(10, 7, 10, 7) };
		renameBtn.Click += async (_, _) => await RenameFilesFromTagsAsync();
		Grid.SetColumn(extractBtn, 0);
		Grid.SetColumn(renameBtn, 1);
		fnBtns.Children.Add(extractBtn);
		fnBtns.Children.Add(renameBtn);
		rightStack.Children.Add(fnBtns);

		_wbStatusTb = new TextBlock
		{
			Text = string.Empty,
			FontSize = 12,
			TextWrapping = TextWrapping.Wrap,
			Foreground = new SolidColorBrush(Color.FromArgb(255, 37, 99, 235))
		};
		rightStack.Children.Add(_wbStatusTb);

		rightScroll.Content = rightStack;
		rightCard.Child = rightScroll;
		Grid.SetRow(rightCard, 1);
		Grid.SetColumn(rightCard, 1);
		grid.Children.Add(rightCard);

		return grid;
	}

	private async Task LoadLibraryTracksIntoWorkbenchAsync()
	{
		try
		{
			string dbPath = Path.Combine(
				Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
				"FurinaPlayer",
				"library.db");
			if (!File.Exists(dbPath))
			{
				SetWorkbenchStatus(Loc.T("未找到音乐库数据库，请点击“添加音频文件”。"));
				return;
			}

			using var libSvc = new LibraryService(dbPath);
			var allTracks = await libSvc.GetAllTracksAsync();
			var loaded = allTracks
				.OrderBy(t => t.Album)
				.ThenBy(t => t.TrackNumber)
				.Select(t => new WorkbenchTrackItem
				{
					FilePath = t.FilePath,
					Title = t.Title,
					Artist = t.Artist,
					Album = t.Album,
					TrackNumber = t.TrackNumber,
					Year = t.Year,
					Genre = t.Genre ?? string.Empty,
					HasEmbeddedArt = !string.IsNullOrEmpty(t.CoverPath)
				})
				.ToList();

			_wbTracks.Clear();
			_wbTracks.AddRange(loaded.Where(t => !string.IsNullOrWhiteSpace(t.FilePath)));
			RebuildWorkbenchList();
			SetWorkbenchStatus(string.Format(Loc.T("已从音乐库载入 {0} 首曲目。"), _wbTracks.Count));
		}
		catch (Exception ex)
		{
			SetWorkbenchStatus($"载入失败: {ex.Message}");
		}
	}

	private async Task PickWorkbenchFilesAsync()
	{
		var picker = new FileOpenPicker();
		var hwnd = WindowNative.GetWindowHandle(WindowManager.CurrentWindow);
		InitializeWithWindow.Initialize(picker, hwnd);
		foreach (var ext in new[] { ".flac", ".mp3", ".wav", ".m4a", ".ogg", ".opus", ".aac" })
		{
			picker.FileTypeFilter.Add(ext);
		}
		var files = await picker.PickMultipleFilesAsync();
		if (files == null || files.Count == 0) return;

		int added = 0;
		await Task.Run(() =>
		{
			foreach (var file in files)
			{
				if (_wbTracks.Any(t => string.Equals(t.FilePath, file.Path, StringComparison.OrdinalIgnoreCase)))
					continue;
				var tags = MetadataService.ReadBasicTags(file.Path);
				_wbTracks.Add(new WorkbenchTrackItem
				{
					FilePath = file.Path,
					Title = tags.Title,
					Artist = tags.Artist,
					Album = tags.Album,
					TrackNumber = tags.TrackNumber,
					Year = tags.Year,
					Genre = tags.Genre,
					HasEmbeddedArt = tags.HasArt,
					IsSelected = true
				});
				added++;
			}
		});

		RebuildWorkbenchList();
		SetWorkbenchStatus(string.Format(Loc.T("已添加 {0} 个本地文件到工作台。"), added));
	}

	private List<WorkbenchTrackItem> GetFilteredWorkbenchTracks()
	{
		if (string.IsNullOrWhiteSpace(_wbSearch)) return _wbTracks;
		string q = _wbSearch.ToLowerInvariant();
		return _wbTracks.Where(t =>
			t.Title.ToLowerInvariant().Contains(q) ||
			t.Artist.ToLowerInvariant().Contains(q) ||
			t.Album.ToLowerInvariant().Contains(q) ||
			t.FileName.ToLowerInvariant().Contains(q)).ToList();
	}

	private void RebuildWorkbenchList()
	{
		if (_wbListPanel == null) return;
		_wbListPanel.Children.Clear();
		var filtered = GetFilteredWorkbenchTracks();
		int selectedCount = _wbTracks.Count(t => t.IsSelected);
		if (_wbStatsTb != null)
		{
			_wbStatsTb.Text = string.Format(Loc.T("共 {0} 首曲目（当前显示 {1} 首，已选中 {2} 首）"), _wbTracks.Count, filtered.Count, selectedCount);
		}

		foreach (var item in filtered.Take(300))
		{
			var rowBorder = new Border
			{
				CornerRadius = new CornerRadius(8),
				Padding = new Thickness(10, 7, 10, 7),
				Background = new SolidColorBrush(item.IsSelected ? Color.FromArgb(20, 37, 99, 235) : Color.FromArgb(12, 0, 0, 0))
			};

			var rowGrid = new Grid { ColumnSpacing = 10 };
			rowGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
			rowGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.4, GridUnitType.Star) });
			rowGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.0, GridUnitType.Star) });
			rowGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.1, GridUnitType.Star) });
			rowGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

			var cb = new CheckBox { IsChecked = item.IsSelected, MinWidth = 28, VerticalAlignment = VerticalAlignment.Center };
			var captured = item;
			cb.Checked += (_, _) => { captured.IsSelected = true; UpdateWbSelectionStats(); };
			cb.Unchecked += (_, _) => { captured.IsSelected = false; UpdateWbSelectionStats(); };
			Grid.SetColumn(cb, 0);
			rowGrid.Children.Add(cb);

			var titleTb = new TextBlock { Text = string.IsNullOrWhiteSpace(item.Title) ? item.FileName : item.Title, FontWeight = FontWeights.SemiBold, FontSize = 13, TextTrimming = TextTrimming.CharacterEllipsis, VerticalAlignment = VerticalAlignment.Center };
			var artistTb = new TextBlock { Text = item.Artist, FontSize = 12, Foreground = new SolidColorBrush(Color.FromArgb(255, 92, 98, 114)), TextTrimming = TextTrimming.CharacterEllipsis, VerticalAlignment = VerticalAlignment.Center };
			var albumTb = new TextBlock { Text = item.Album, FontSize = 12, Foreground = new SolidColorBrush(Color.FromArgb(255, 92, 98, 114)), TextTrimming = TextTrimming.CharacterEllipsis, VerticalAlignment = VerticalAlignment.Center };
			var metaTb = new TextBlock { Text = $"#{item.TrackNumber} · {(item.Year > 0 ? item.Year.ToString() : "-")}", FontSize = 11.5, Foreground = new SolidColorBrush(Color.FromArgb(255, 110, 118, 134)), VerticalAlignment = VerticalAlignment.Center };

			Grid.SetColumn(titleTb, 1);
			Grid.SetColumn(artistTb, 2);
			Grid.SetColumn(albumTb, 3);
			Grid.SetColumn(metaTb, 4);
			rowGrid.Children.Add(titleTb);
			rowGrid.Children.Add(artistTb);
			rowGrid.Children.Add(albumTb);
			rowGrid.Children.Add(metaTb);

			rowBorder.Child = rowGrid;
			_wbListPanel.Children.Add(rowBorder);
		}
	}

	private void UpdateWbSelectionStats()
	{
		if (_wbStatsTb == null) return;
		var filtered = GetFilteredWorkbenchTracks();
		int selectedCount = _wbTracks.Count(t => t.IsSelected);
		_wbStatsTb.Text = string.Format(Loc.T("共 {0} 首曲目（当前显示 {1} 首，已选中 {2} 首）"), _wbTracks.Count, filtered.Count, selectedCount);
	}

	private List<WorkbenchTrackItem> GetSelectedTracks() => _wbTracks.Where(t => t.IsSelected).ToList();

	private async Task ApplyBatchTagsAsync()
	{
		var selected = GetSelectedTracks();
		if (selected.Count == 0) { SetWorkbenchStatus(Loc.T("请先勾选需要修改标签的曲目。")); return; }

		string? title = string.IsNullOrWhiteSpace(_wbTitleBox?.Text) ? null : _wbTitleBox.Text.Trim();
		string? artist = string.IsNullOrWhiteSpace(_wbArtistBox?.Text) ? null : _wbArtistBox.Text.Trim();
		string? album = string.IsNullOrWhiteSpace(_wbAlbumBox?.Text) ? null : _wbAlbumBox.Text.Trim();
		int? trackNum = int.TryParse(_wbTrackNumBox?.Text?.Trim(), out int tn) ? tn : null;
		int? year = int.TryParse(_wbYearBox?.Text?.Trim(), out int yr) ? yr : null;
		string? genre = string.IsNullOrWhiteSpace(_wbGenreBox?.Text) ? null : _wbGenreBox.Text.Trim();

		int ok = 0;
		await Task.Run(() =>
		{
			foreach (var item in selected)
			{
				if (!File.Exists(item.FilePath)) continue;
				if (MetadataService.WriteBasicTags(item.FilePath, title, artist, album, trackNum, year, genre))
				{
					if (title != null) item.Title = title;
					if (artist != null) item.Artist = artist;
					if (album != null) item.Album = album;
					if (trackNum.HasValue) item.TrackNumber = trackNum.Value;
					if (year.HasValue) item.Year = year.Value;
					if (genre != null) item.Genre = genre;
					ok++;
				}
			}
			SyncWorkbenchChangesToDb(selected);
		});

		RebuildWorkbenchList();
		SetWorkbenchStatus(string.Format(Loc.T("已保存 {0}/{1} 首曲目的标签写入音频文件。"), ok, selected.Count));
	}

	private async Task RunActionGroupAsync(string mode)
	{
		var selected = GetSelectedTracks();
		if (selected.Count == 0) { SetWorkbenchStatus(Loc.T("请先勾选需要清洗的曲目。")); return; }

		static string Transform(string s, string m)
		{
			if (string.IsNullOrEmpty(s)) return s;
			return m switch
			{
				"trim" => Regex.Replace(s.Trim(), @"\s{2,}", " "),
				"upper" => s.ToUpperInvariant(),
				"lower" => s.ToLowerInvariant(),
				"title" => CultureInfo.InvariantCulture.TextInfo.ToTitleCase(s.ToLowerInvariant()),
				_ => s
			};
		}

		int ok = 0;
		await Task.Run(() =>
		{
			foreach (var item in selected)
			{
				if (!File.Exists(item.FilePath)) continue;
				item.Title = Transform(item.Title, mode);
				item.Artist = Transform(item.Artist, mode);
				item.Album = Transform(item.Album, mode);
				if (MetadataService.WriteBasicTags(item.FilePath, item.Title, item.Artist, item.Album, null, null, null)) ok++;
			}
			SyncWorkbenchChangesToDb(selected);
		});

		RebuildWorkbenchList();
		SetWorkbenchStatus(string.Format(Loc.T("动作组执行完成：已更新 {0}/{1} 首曲目。"), ok, selected.Count));
	}

	private async Task RunTextReplaceAsync()
	{
		var selected = GetSelectedTracks();
		string find = _wbFindBox?.Text ?? string.Empty;
		string rep = _wbReplaceBox?.Text ?? string.Empty;
		if (selected.Count == 0 || string.IsNullOrEmpty(find))
		{
			SetWorkbenchStatus(Loc.T("请先勾选曲目并填写“查找文本”。"));
			return;
		}

		int ok = 0;
		await Task.Run(() =>
		{
			foreach (var item in selected)
			{
				if (!File.Exists(item.FilePath)) continue;
				item.Title = item.Title.Replace(find, rep, StringComparison.OrdinalIgnoreCase);
				item.Artist = item.Artist.Replace(find, rep, StringComparison.OrdinalIgnoreCase);
				item.Album = item.Album.Replace(find, rep, StringComparison.OrdinalIgnoreCase);
				if (MetadataService.WriteBasicTags(item.FilePath, item.Title, item.Artist, item.Album, null, null, null)) ok++;
			}
			SyncWorkbenchChangesToDb(selected);
		});

		RebuildWorkbenchList();
		SetWorkbenchStatus(string.Format(Loc.T("文本替换完成：已更新 {0}/{1} 首曲目。"), ok, selected.Count));
	}

	private async Task ExtractTagsFromFileNameAsync()
	{
		var selected = GetSelectedTracks();
		if (selected.Count == 0) { SetWorkbenchStatus(Loc.T("请先勾选曲目。")); return; }
		string pattern = _wbPatternCombo?.SelectedItem?.ToString() ?? "{Artist} - {Title}";

		int ok = 0;
		await Task.Run(() =>
		{
			foreach (var item in selected)
			{
				if (!File.Exists(item.FilePath)) continue;
				string stem = Path.GetFileNameWithoutExtension(item.FilePath);
				var parts = stem.Split(" - ", StringSplitOptions.TrimEntries);
				if (pattern == "{Artist} - {Title}" && parts.Length >= 2)
				{
					item.Artist = parts[0];
					item.Title = string.Join(" - ", parts.Skip(1));
				}
				else if (pattern == "{Track} - {Title}" && parts.Length >= 2 && int.TryParse(parts[0], out int tn))
				{
					item.TrackNumber = tn;
					item.Title = string.Join(" - ", parts.Skip(1));
				}
				else if (pattern == "{Artist} - {Album} - {Title}" && parts.Length >= 3)
				{
					item.Artist = parts[0];
					item.Album = parts[1];
					item.Title = string.Join(" - ", parts.Skip(2));
				}
				else continue;

				if (MetadataService.WriteBasicTags(item.FilePath, item.Title, item.Artist, item.Album, item.TrackNumber > 0 ? item.TrackNumber : null, null, null)) ok++;
			}
			SyncWorkbenchChangesToDb(selected);
		});

		RebuildWorkbenchList();
		SetWorkbenchStatus(string.Format(Loc.T("已从文件名提取并写入 {0}/{1} 首曲目标签。"), ok, selected.Count));
	}

	private async Task RenameFilesFromTagsAsync()
	{
		var selected = GetSelectedTracks();
		if (selected.Count == 0) { SetWorkbenchStatus(Loc.T("请先勾选曲目。")); return; }
		string pattern = _wbPatternCombo?.SelectedItem?.ToString() ?? "{Artist} - {Title}";

		static string Sanitize(string s)
		{
			foreach (char c in Path.GetInvalidFileNameChars()) s = s.Replace(c, '_');
			return s.Trim();
		}

		int ok = 0;
		await Task.Run(() =>
		{
			foreach (var item in selected)
			{
				if (!File.Exists(item.FilePath)) continue;
				string ext = Path.GetExtension(item.FilePath);
				string dir = Path.GetDirectoryName(item.FilePath) ?? ".";
				string baseName = pattern switch
				{
					"{Track} - {Title}" => $"{item.TrackNumber:D2} - {item.Title}",
					"{Artist} - {Album} - {Title}" => $"{item.Artist} - {item.Album} - {item.Title}",
					_ => $"{item.Artist} - {item.Title}"
				};
				string cleanName = Sanitize(baseName);
				if (string.IsNullOrWhiteSpace(cleanName)) continue;
				string dest = Path.Combine(dir, cleanName + ext);
				if (string.Equals(dest, item.FilePath, StringComparison.OrdinalIgnoreCase) || File.Exists(dest))
					continue;
				try
				{
					File.Move(item.FilePath, dest);
					item.FilePath = dest;
					ok++;
				}
				catch { }
			}
		});

		RebuildWorkbenchList();
		SetWorkbenchStatus(string.Format(Loc.T("已按标签重命名 {0}/{1} 个文件。"), ok, selected.Count));
	}

	private void RunWorkbenchConsistencyCheck()
	{
		var target = GetSelectedTracks();
		if (target.Count == 0) target = _wbTracks;
		int missingTitle = target.Count(t => string.IsNullOrWhiteSpace(t.Title));
		int missingArtist = target.Count(t => string.IsNullOrWhiteSpace(t.Artist) || t.Artist == "Unknown Artist");
		int missingAlbum = target.Count(t => string.IsNullOrWhiteSpace(t.Album) || t.Album == "Unknown Album");
		int whitespaceIssues = target.Count(t =>
			t.Title != t.Title.Trim() || t.Artist != t.Artist.Trim() || t.Album != t.Album.Trim());

		SetWorkbenchStatus(
			$"一致性检查 ({target.Count} 首): 缺标题 {missingTitle} · 缺歌手 {missingArtist} · 缺专辑 {missingAlbum} · 首尾多余空格 {whitespaceIssues}");
	}

	private static void SyncWorkbenchChangesToDb(IEnumerable<WorkbenchTrackItem> updated)
	{
		// Metadata tags are persisted directly to the audio files via ATL.Track.Save().
		_ = updated;
	}

	private void SetWorkbenchStatus(string text)
	{
		if (_wbStatusTb != null)
		{
			_wbStatusTb.Text = text;
		}
	}
}
