using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using FurinaPlayer.Audio.Services;
using FurinaPlayer.Core.Services;
using FurinaPlayer.UI.Controls;
using FurinaPlayer.UI.ViewModels;
using Microsoft.UI;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.UI;
using WinRT.Interop;

namespace FurinaPlayer.UI.Pages;

/// <summary>
/// 音频格式转换页面：支持 .ncm / .mgg / .kgm / .kwm 解密转码为 MP3/FLAC，
/// 自动检查并补齐音乐元数据、内嵌歌词与专辑封面，支持自定义保存路径与文件夹导出。
/// </summary>
public sealed class ConverterPage : Page
{
	public ConverterViewModel ViewModel { get; set; }

	private readonly Grid _rootGrid;
	private readonly Border _headerCard;
	private readonly Border _settingsCard;
	private readonly Border _progressCard;
	private readonly Border _listCard;
	private readonly StackPanel _itemsPanel;
	private readonly Border _emptyStatePanel;
	private readonly ProgressBar _overallProgressBar;
	private readonly TextBlock _overallStatusTb;
	private readonly TextBlock _overallPctTb;
	private readonly ComboBox _formatCombo;
	private readonly TextBox _outputDirBox;
	private readonly CheckBox _autoEnrichCheck;
	private readonly Button _startBtn;
	private readonly Button _stopBtn;
	private readonly Button _clearBtn;
	private readonly Button _openOutDirBtn;

	private bool IsDark => ActualTheme == ElementTheme.Dark;

	public ConverterPage() : this(new ConverterViewModel()) { }

	public ConverterPage(ConverterViewModel vm)
	{
		ViewModel = vm;
		DataContext = vm;
		NavigationCacheMode = Microsoft.UI.Xaml.Navigation.NavigationCacheMode.Required;

		_rootGrid = new Grid
		{
			Padding = new Thickness(28, 22, 28, 24),
			RowSpacing = 16,
			AllowDrop = true
		};
		_rootGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Header
		_rootGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Settings
		_rootGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Overall Progress
		_rootGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) }); // Queue List

		_rootGrid.DragOver += OnPageDragOver;
		_rootGrid.Drop += OnPageDrop;

		// 1. Header Card
		_headerCard = CreateCardBorder(new Thickness(20, 16, 20, 16));
		var headerGrid = new Grid { ColumnSpacing = 16 };
		headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
		headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
		headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

		var iconContainer = new Border
		{
			Width = 48,
			Height = 48,
			CornerRadius = new CornerRadius(12),
			VerticalAlignment = VerticalAlignment.Center
		};
		iconContainer.Child = FluentIconFactory.Create(FluentIconKind.FormatConverter, 32);
		Grid.SetColumn(iconContainer, 0);
		headerGrid.Children.Add(iconContainer);

		var titleStack = new StackPanel { Spacing = 4, VerticalAlignment = VerticalAlignment.Center };
		titleStack.Children.Add(new TextBlock
		{
			Text = Loc.T("音频格式转换与标签补全"),
			FontSize = 20,
			FontWeight = FontWeights.SemiBold
		});
		titleStack.Children.Add(new TextBlock
		{
			Text = Loc.T("支持网易云 (.ncm)、QQ 音乐 (.mgg / .mflac)、酷狗 (.kgm / .vpr)、酷我 (.kwm) 解密转换为通用 MP3 / FLAC，并自动检查补齐元数据、内嵌歌词与专辑封面。"),
			FontSize = 12,
			Opacity = 0.72,
			TextWrapping = TextWrapping.Wrap
		});
		Grid.SetColumn(titleStack, 1);
		headerGrid.Children.Add(titleStack);

		var actionButtons = new StackPanel
		{
			Orientation = Orientation.Horizontal,
			Spacing = 10,
			VerticalAlignment = VerticalAlignment.Center
		};
		var addFilesBtn = new Button { Content = CreateIconLabel(FluentIconKind.FileAdd, Loc.T("添加文件")), CornerRadius = new CornerRadius(8), Padding = new Thickness(12, 8, 12, 8) };
		addFilesBtn.Click += async (_, _) => await PickFilesAsync();

		var addFolderBtn = new Button { Content = CreateIconLabel(FluentIconKind.FolderScan, Loc.T("添加文件夹")), CornerRadius = new CornerRadius(8), Padding = new Thickness(12, 8, 12, 8) };
		addFolderBtn.Click += async (_, _) => await PickInputFolderAsync();

		_startBtn = new Button
		{
			Content = CreateIconLabel(FluentIconKind.Play, Loc.T("开始转换")),
			CornerRadius = new CornerRadius(8),
			Padding = new Thickness(14, 8, 14, 8),
			Background = new SolidColorBrush(Color.FromArgb(255, 14, 165, 233)),
			Foreground = new SolidColorBrush(Colors.White)
		};
		_startBtn.Click += async (_, _) => await StartConversionAsync();

		_stopBtn = new Button
		{
			Content = CreateIconLabel(FluentIconKind.Stop, Loc.T("停止转换")),
			CornerRadius = new CornerRadius(8),
			Padding = new Thickness(12, 8, 12, 8),
			Visibility = Visibility.Collapsed
		};
		_stopBtn.Click += (_, _) => ViewModel.CancelConversion();

		_clearBtn = new Button
		{
			Content = CreateIconLabel(FluentIconKind.ClearFilter, Loc.T("清空列表")),
			CornerRadius = new CornerRadius(8),
			Padding = new Thickness(12, 8, 12, 8)
		};
		_clearBtn.Click += (_, _) => ViewModel.ClearList();

		actionButtons.Children.Add(addFilesBtn);
		actionButtons.Children.Add(addFolderBtn);
		actionButtons.Children.Add(_startBtn);
		actionButtons.Children.Add(_stopBtn);
		actionButtons.Children.Add(_clearBtn);
		Grid.SetColumn(actionButtons, 2);
		headerGrid.Children.Add(actionButtons);

		_headerCard.Child = headerGrid;
		Grid.SetRow(_headerCard, 0);
		_rootGrid.Children.Add(_headerCard);

		// 2. Settings Card (Target Format + Output Directory + Auto-Enrich)
		_settingsCard = CreateCardBorder(new Thickness(20, 14, 20, 14));
		var settingsGrid = new Grid { RowSpacing = 12, ColumnSpacing = 16 };
		settingsGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
		settingsGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
		settingsGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
		settingsGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
		settingsGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

		var fmtLabel = new TextBlock { Text = Loc.T("目标格式"), VerticalAlignment = VerticalAlignment.Center, FontWeight = FontWeights.Medium };
		Grid.SetRow(fmtLabel, 0);
		Grid.SetColumn(fmtLabel, 0);
		settingsGrid.Children.Add(fmtLabel);

		var fmtRow = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 18, VerticalAlignment = VerticalAlignment.Center };
		_formatCombo = new ComboBox { Width = 220, CornerRadius = new CornerRadius(8) };
		_formatCombo.Items.Add(Loc.T("自动匹配 (.mp3 / .flac)"));
		_formatCombo.Items.Add(Loc.T("MP3 (320 kbps)"));
		_formatCombo.Items.Add(Loc.T("FLAC (无损)"));
		_formatCombo.SelectedIndex = (int)ViewModel.TargetFormat;
		_formatCombo.SelectionChanged += (_, _) =>
		{
			if (_formatCombo.SelectedIndex >= 0)
				ViewModel.TargetFormat = (ConvertTargetFormat)_formatCombo.SelectedIndex;
		};
		fmtRow.Children.Add(_formatCombo);

		_autoEnrichCheck = new CheckBox
		{
			Content = Loc.T("自动检查并补齐音乐元数据、内嵌歌词与专辑封面"),
			IsChecked = ViewModel.AutoEnrichMetadata,
			VerticalAlignment = VerticalAlignment.Center
		};
		_autoEnrichCheck.Checked += (_, _) => ViewModel.AutoEnrichMetadata = true;
		_autoEnrichCheck.Unchecked += (_, _) => ViewModel.AutoEnrichMetadata = false;
		fmtRow.Children.Add(_autoEnrichCheck);

		Grid.SetRow(fmtRow, 0);
		Grid.SetColumn(fmtRow, 1);
		Grid.SetColumnSpan(fmtRow, 2);
		settingsGrid.Children.Add(fmtRow);

		var pathLabel = new TextBlock { Text = Loc.T("保存路径"), VerticalAlignment = VerticalAlignment.Center, FontWeight = FontWeights.Medium };
		Grid.SetRow(pathLabel, 1);
		Grid.SetColumn(pathLabel, 0);
		settingsGrid.Children.Add(pathLabel);

		_outputDirBox = new TextBox
		{
			PlaceholderText = Loc.T("与源文件同目录（可在右侧自定义路径）"),
			Text = ViewModel.OutputDirectory,
			CornerRadius = new CornerRadius(8),
			VerticalAlignment = VerticalAlignment.Center
		};
		_outputDirBox.TextChanged += (_, _) => ViewModel.OutputDirectory = _outputDirBox.Text?.Trim() ?? string.Empty;
		Grid.SetRow(_outputDirBox, 1);
		Grid.SetColumn(_outputDirBox, 1);
		settingsGrid.Children.Add(_outputDirBox);

		var pathBtns = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
		var browseOutBtn = new Button { Content = Loc.T("浏览文件夹..."), CornerRadius = new CornerRadius(8), Padding = new Thickness(12, 6, 12, 6) };
		browseOutBtn.Click += async (_, _) => await PickOutputFolderAsync();
		_openOutDirBtn = new Button { Content = Loc.T("打开输出目录"), CornerRadius = new CornerRadius(8), Padding = new Thickness(12, 6, 12, 6) };
		_openOutDirBtn.Click += (_, _) => OpenOutputDirectory();
		pathBtns.Children.Add(browseOutBtn);
		pathBtns.Children.Add(_openOutDirBtn);
		Grid.SetRow(pathBtns, 1);
		Grid.SetColumn(pathBtns, 2);
		settingsGrid.Children.Add(pathBtns);

		_settingsCard.Child = settingsGrid;
		Grid.SetRow(_settingsCard, 1);
		_rootGrid.Children.Add(_settingsCard);

		// 3. Overall Progress Card
		_progressCard = CreateCardBorder(new Thickness(20, 12, 20, 12));
		_progressCard.Visibility = Visibility.Collapsed;
		var progGrid = new Grid { RowSpacing = 6, ColumnSpacing = 12 };
		progGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
		progGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
		progGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
		progGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
		progGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

		var progTitle = new TextBlock { Text = Loc.T("总进度"), FontWeight = FontWeights.SemiBold, FontSize = 13 };
		Grid.SetRow(progTitle, 0);
		Grid.SetColumn(progTitle, 0);
		progGrid.Children.Add(progTitle);

		_overallStatusTb = new TextBlock { FontSize = 13, Opacity = 0.8 };
		Grid.SetRow(_overallStatusTb, 0);
		Grid.SetColumn(_overallStatusTb, 1);
		progGrid.Children.Add(_overallStatusTb);

		_overallPctTb = new TextBlock { FontSize = 13, FontWeight = FontWeights.SemiBold };
		Grid.SetRow(_overallPctTb, 0);
		Grid.SetColumn(_overallPctTb, 2);
		progGrid.Children.Add(_overallPctTb);

		_overallProgressBar = new ProgressBar { Minimum = 0, Maximum = 100, Value = 0, Height = 6, CornerRadius = new CornerRadius(3) };
		Grid.SetRow(_overallProgressBar, 1);
		Grid.SetColumnSpan(_overallProgressBar, 3);
		progGrid.Children.Add(_overallProgressBar);

		_progressCard.Child = progGrid;
		Grid.SetRow(_progressCard, 2);
		_rootGrid.Children.Add(_progressCard);

		// 4. Task Queue List Card
		_listCard = CreateCardBorder(new Thickness(14));
		var listGrid = new Grid();

		_emptyStatePanel = new Border
		{
			Padding = new Thickness(24),
			CornerRadius = new CornerRadius(12),
			BorderThickness = new Thickness(1.5),
			HorizontalAlignment = HorizontalAlignment.Center,
			VerticalAlignment = VerticalAlignment.Center
		};
		var emptyStack = new StackPanel { Spacing = 12, HorizontalAlignment = HorizontalAlignment.Center };
		emptyStack.Children.Add(FluentIconFactory.Create(FluentIconKind.FormatConverter, 48));
		emptyStack.Children.Add(new TextBlock
		{
			Text = Loc.T("拖放 .ncm / .mgg / .kgm / .kwm 文件或文件夹到此处，或点击上方按钮添加"),
			FontSize = 14,
			Opacity = 0.78,
			TextAlignment = TextAlignment.Center,
			TextWrapping = TextWrapping.Wrap,
			MaxWidth = 460
		});
		_emptyStatePanel.Child = emptyStack;
		listGrid.Children.Add(_emptyStatePanel);

		_itemsPanel = new StackPanel { Spacing = 10 };
		var scroll = new ScrollViewer
		{
			Content = _itemsPanel,
			VerticalScrollBarVisibility = ScrollBarVisibility.Auto
		};
		listGrid.Children.Add(scroll);

		_listCard.Child = listGrid;
		Grid.SetRow(_listCard, 3);
		_rootGrid.Children.Add(_listCard);

		Content = _rootGrid;

		ViewModel.Items.CollectionChanged += OnItemsCollectionChanged;
		ViewModel.PropertyChanged += OnViewModelPropertyChanged;
		ActualThemeChanged += (_, _) => ApplyThemeBrushes();
		Loaded += (_, _) =>
		{
			ApplyThemeBrushes();
			RebuildItemsList();
		};
	}

	private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
	{
		DispatcherQueue?.TryEnqueue(() =>
		{
			if (e.PropertyName == nameof(ConverterViewModel.IsConverting))
			{
				_startBtn.Visibility = ViewModel.IsConverting ? Visibility.Collapsed : Visibility.Visible;
				_stopBtn.Visibility = ViewModel.IsConverting ? Visibility.Visible : Visibility.Collapsed;
				_clearBtn.IsEnabled = !ViewModel.IsConverting;
				_formatCombo.IsEnabled = !ViewModel.IsConverting;
			}
			else if (e.PropertyName is nameof(ConverterViewModel.OverallProgress) or nameof(ConverterViewModel.OverallStatusText) or nameof(ConverterViewModel.TotalCount))
			{
				_progressCard.Visibility = ViewModel.TotalCount > 0 ? Visibility.Visible : Visibility.Collapsed;
				_overallProgressBar.Value = ViewModel.OverallProgress;
				_overallStatusTb.Text = ViewModel.OverallStatusText;
				_overallPctTb.Text = $"{ViewModel.OverallProgress:0}%";
			}
		});
	}

	private void OnItemsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
	{
		DispatcherQueue?.TryEnqueue(RebuildItemsList);
	}

	private void RebuildItemsList()
	{
		_itemsPanel.Children.Clear();
		bool hasItems = ViewModel.Items.Count > 0;
		_emptyStatePanel.Visibility = hasItems ? Visibility.Collapsed : Visibility.Visible;
		_progressCard.Visibility = hasItems ? Visibility.Visible : Visibility.Collapsed;
		_overallProgressBar.Value = ViewModel.OverallProgress;
		_overallStatusTb.Text = ViewModel.OverallStatusText;
		_overallPctTb.Text = $"{ViewModel.OverallProgress:0}%";

		foreach (var item in ViewModel.Items)
		{
			_itemsPanel.Children.Add(BuildTaskRow(item));
		}
	}

	private Border BuildTaskRow(ConverterTaskItem item)
	{
		var rowCard = new Border
		{
			CornerRadius = new CornerRadius(10),
			Padding = new Thickness(14, 12, 14, 12),
			BorderThickness = new Thickness(1),
			Background = new SolidColorBrush(IsDark ? Color.FromArgb(40, 255, 255, 255) : Color.FromArgb(145, 255, 255, 255)),
			BorderBrush = new SolidColorBrush(IsDark ? Color.FromArgb(35, 255, 255, 255) : Color.FromArgb(40, 15, 23, 42))
		};

		var grid = new Grid { ColumnSpacing = 14, RowSpacing = 6 };
		grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
		grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
		grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
		grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
		grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

		// Format badge
		var extBadge = new Border
		{
			CornerRadius = new CornerRadius(6),
			Padding = new Thickness(8, 4, 8, 4),
			VerticalAlignment = VerticalAlignment.Center,
			Background = new SolidColorBrush(GetFormatAccent(item.SourceExt))
		};
		var extTb = new TextBlock
		{
			Text = item.SourceExt,
			FontSize = 11,
			FontWeight = FontWeights.Bold,
			Foreground = new SolidColorBrush(Colors.White)
		};
		extBadge.Child = extTb;
		Grid.SetRowSpan(extBadge, 2);
		Grid.SetColumn(extBadge, 0);
		grid.Children.Add(extBadge);

		// Info + integrity badges
		var infoStack = new StackPanel { Spacing = 3 };
		var titleRow = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
		var nameTb = new TextBlock { Text = item.FileName, FontSize = 14, FontWeight = FontWeights.SemiBold };
		var sizeTb = new TextBlock { Text = item.FileSizeText, FontSize = 12, Opacity = 0.65, VerticalAlignment = VerticalAlignment.Center };
		var targetTb = new TextBlock
		{
			FontSize = 11,
			FontWeight = FontWeights.SemiBold,
			Foreground = new SolidColorBrush(Color.FromArgb(255, 14, 165, 233)),
			VerticalAlignment = VerticalAlignment.Center
		};
		titleRow.Children.Add(nameTb);
		titleRow.Children.Add(sizeTb);
		titleRow.Children.Add(targetTb);
		infoStack.Children.Add(titleRow);

		var statusRow = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
		var statusTb = new TextBlock { Text = item.StatusText, FontSize = 12, Opacity = 0.8 };
		var chipsPanel = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 6 };
		statusRow.Children.Add(statusTb);
		statusRow.Children.Add(chipsPanel);
		infoStack.Children.Add(statusRow);

		Grid.SetRow(infoStack, 0);
		Grid.SetColumn(infoStack, 1);
		grid.Children.Add(infoStack);

		// Progress bar
		var itemProgress = new ProgressBar
		{
			Minimum = 0,
			Maximum = 100,
			Value = item.Progress,
			Height = 4,
			CornerRadius = new CornerRadius(2)
		};
		Grid.SetRow(itemProgress, 1);
		Grid.SetColumn(itemProgress, 1);
		grid.Children.Add(itemProgress);

		// Actions
		var actPanel = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 6, VerticalAlignment = VerticalAlignment.Center };
		var openBtn = new Button
		{
			Content = Loc.T("打开所在位置"),
			FontSize = 12,
			Padding = new Thickness(10, 5, 10, 5),
			CornerRadius = new CornerRadius(6),
			Visibility = string.IsNullOrEmpty(item.OutputPath) ? Visibility.Collapsed : Visibility.Visible
		};
		openBtn.Click += (_, _) =>
		{
			if (!string.IsNullOrEmpty(item.OutputPath) && File.Exists(item.OutputPath))
			{
				Process.Start(new ProcessStartInfo("explorer.exe", $"/select,\"{item.OutputPath}\"") { UseShellExecute = true });
			}
		};

		var removeBtn = new Button
		{
			Content = Loc.T("移除"),
			FontSize = 12,
			Padding = new Thickness(10, 5, 10, 5),
			CornerRadius = new CornerRadius(6)
		};
		removeBtn.Click += (_, _) => ViewModel.RemoveItem(item);

		actPanel.Children.Add(openBtn);
		actPanel.Children.Add(removeBtn);
		Grid.SetRowSpan(actPanel, 2);
		Grid.SetColumn(actPanel, 2);
		grid.Children.Add(actPanel);

		void SyncItemUi()
		{
			itemProgress.Value = item.Progress;
			statusTb.Text = item.StatusText;
			targetTb.Text = string.IsNullOrEmpty(item.TargetContainer) ? string.Empty : $"→ {item.TargetContainer}";
			openBtn.Visibility = !string.IsNullOrEmpty(item.OutputPath) ? Visibility.Visible : Visibility.Collapsed;
			statusTb.Foreground = item.State switch
			{
				ConverterItemState.Succeeded => new SolidColorBrush(Color.FromArgb(255, 16, 185, 129)),
				ConverterItemState.Failed => new SolidColorBrush(Color.FromArgb(255, 239, 68, 68)),
				ConverterItemState.Running => new SolidColorBrush(Color.FromArgb(255, 14, 165, 233)),
				_ => new SolidColorBrush(IsDark ? Colors.White : Color.FromArgb(255, 15, 23, 42))
			};

			chipsPanel.Children.Clear();
			if (item.State == ConverterItemState.Succeeded)
			{
				chipsPanel.Children.Add(BuildIntegrityChip(
					item.MetadataCompleted ? Loc.T("元数据(已补齐)") : (item.HasMetadata ? Loc.T("元数据 ✓") : Loc.T("元数据缺失")),
					item.HasMetadata));
				chipsPanel.Children.Add(BuildIntegrityChip(
					item.LyricsCompleted ? Loc.T("歌词(已补齐)") : (item.HasLyrics ? Loc.T("内嵌歌词 ✓") : Loc.T("无内嵌歌词")),
					item.HasLyrics));
				chipsPanel.Children.Add(BuildIntegrityChip(
					item.CoverCompleted ? Loc.T("封面(已补齐)") : (item.HasCover ? Loc.T("封面 ✓") : Loc.T("无封面")),
					item.HasCover));
			}
		}

		SyncItemUi();
		item.PropertyChanged += (_, _) => DispatcherQueue?.TryEnqueue(SyncItemUi);

		rowCard.Child = grid;
		return rowCard;
	}

	private static Border BuildIntegrityChip(string text, bool ok)
	{
		Color c = ok ? Color.FromArgb(255, 16, 185, 129) : Color.FromArgb(255, 245, 158, 11);
		return new Border
		{
			CornerRadius = new CornerRadius(5),
			Padding = new Thickness(6, 2, 6, 2),
			Background = new SolidColorBrush(Color.FromArgb(30, c.R, c.G, c.B)),
			BorderBrush = new SolidColorBrush(Color.FromArgb(90, c.R, c.G, c.B)),
			BorderThickness = new Thickness(1),
			Child = new TextBlock
			{
				Text = text,
				FontSize = 11,
				Foreground = new SolidColorBrush(c),
				FontWeight = FontWeights.Medium
			}
		};
	}

	private static Color GetFormatAccent(string ext) => ext.ToUpperInvariant() switch
	{
		"NCM" => Color.FromArgb(255, 225, 29, 72),
		"MGG" or "MGG0" or "MGG1" or "MFLAC" or "QMCFLAC" => Color.FromArgb(255, 16, 185, 129),
		"KGM" or "KGMA" or "VPR" => Color.FromArgb(255, 37, 99, 235),
		"KWM" => Color.FromArgb(255, 245, 158, 11),
		_ => Color.FromArgb(255, 99, 102, 241)
	};

	private static StackPanel CreateIconLabel(FluentIconKind kind, string label)
	{
		var sp = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
		sp.Children.Add(FluentIconFactory.Create(kind, 16));
		sp.Children.Add(new TextBlock { Text = label, VerticalAlignment = VerticalAlignment.Center });
		return sp;
	}

	private Border CreateCardBorder(Thickness padding) => new()
	{
		CornerRadius = new CornerRadius(14),
		Padding = padding,
		BorderThickness = new Thickness(1)
	};

	public void ApplyThemeBrushes()
	{
		bool dark = IsDark;
		var bg = new SolidColorBrush(dark ? Color.FromArgb(38, 255, 255, 255) : Color.FromArgb(165, 255, 255, 255));
		var border = new SolidColorBrush(dark ? Color.FromArgb(42, 255, 255, 255) : Color.FromArgb(42, 15, 23, 42));

		foreach (var card in new[] { _headerCard, _settingsCard, _progressCard, _listCard })
		{
			card.Background = bg;
			card.BorderBrush = border;
		}
		_emptyStatePanel.BorderBrush = new SolidColorBrush(dark ? Color.FromArgb(55, 255, 255, 255) : Color.FromArgb(55, 15, 23, 42));
	}

	private async Task PickFilesAsync()
	{
		try
		{
			var picker = new FileOpenPicker
			{
				SuggestedStartLocation = PickerLocationId.MusicLibrary
			};
			foreach (var ext in new[] { ".ncm", ".mgg", ".mgg0", ".mgg1", ".mflac", ".mflac0", ".qmcflac", ".qmc0", ".qmc3", ".kgm", ".kgma", ".vpr", ".kwm" })
			{
				picker.FileTypeFilter.Add(ext);
			}
			nint hwnd = WindowNative.GetWindowHandle(WindowManager.CurrentWindow);
			InitializeWithWindow.Initialize(picker, hwnd);
			var files = await picker.PickMultipleFilesAsync();
			if (files is { Count: > 0 })
			{
				ViewModel.AddFiles(files.Select(f => f.Path));
			}
		}
		catch { }
	}

	private async Task PickInputFolderAsync()
	{
		try
		{
			var picker = new FolderPicker
			{
				SuggestedStartLocation = PickerLocationId.MusicLibrary,
				FileTypeFilter = { "*" }
			};
			nint hwnd = WindowNative.GetWindowHandle(WindowManager.CurrentWindow);
			InitializeWithWindow.Initialize(picker, hwnd);
			var folder = await picker.PickSingleFolderAsync();
			if (folder != null)
			{
				ViewModel.AddFolder(folder.Path);
			}
		}
		catch { }
	}

	private async Task PickOutputFolderAsync()
	{
		try
		{
			var picker = new FolderPicker
			{
				SuggestedStartLocation = PickerLocationId.MusicLibrary,
				FileTypeFilter = { "*" }
			};
			nint hwnd = WindowNative.GetWindowHandle(WindowManager.CurrentWindow);
			InitializeWithWindow.Initialize(picker, hwnd);
			var folder = await picker.PickSingleFolderAsync();
			if (folder != null)
			{
				_outputDirBox.Text = folder.Path;
				ViewModel.OutputDirectory = folder.Path;
			}
		}
		catch { }
	}

	private void OpenOutputDirectory()
	{
		string dir = ViewModel.OutputDirectory;
		if (string.IsNullOrWhiteSpace(dir))
		{
			var firstWithOut = ViewModel.Items.FirstOrDefault(i => !string.IsNullOrEmpty(i.OutputPath));
			if (firstWithOut != null) dir = Path.GetDirectoryName(firstWithOut.OutputPath) ?? string.Empty;
			else if (ViewModel.Items.Count > 0) dir = Path.GetDirectoryName(ViewModel.Items[0].InputPath) ?? string.Empty;
		}
		if (!string.IsNullOrWhiteSpace(dir) && Directory.Exists(dir))
		{
			Process.Start(new ProcessStartInfo("explorer.exe", $"\"{dir}\"") { UseShellExecute = true });
		}
	}

	private async Task StartConversionAsync()
	{
		await ViewModel.StartConversionAsync(action =>
		{
			if (DispatcherQueue != null)
				DispatcherQueue.TryEnqueue(() => action());
			else
				action();
		});
	}

	private void OnPageDragOver(object sender, DragEventArgs e)
	{
		if (e.DataView.Contains(StandardDataFormats.StorageItems))
		{
			e.AcceptedOperation = DataPackageOperation.Copy;
		}
	}

	private async void OnPageDrop(object sender, DragEventArgs e)
	{
		if (!e.DataView.Contains(StandardDataFormats.StorageItems)) return;
		var items = await e.DataView.GetStorageItemsAsync();
		var files = new List<string>();
		foreach (var it in items)
		{
			if (it is StorageFile sf) files.Add(sf.Path);
			else if (it is StorageFolder sd) ViewModel.AddFolder(sd.Path);
		}
		if (files.Count > 0) ViewModel.AddFiles(files);
	}
}
