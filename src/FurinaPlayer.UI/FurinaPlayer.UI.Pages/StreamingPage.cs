using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FurinaPlayer.UI.Controls;
using FurinaPlayer.UI.ViewModels;
using Microsoft.UI;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI.Xaml.Navigation;
using Windows.ApplicationModel.DataTransfer;
using Windows.System;
using Windows.UI;

namespace FurinaPlayer.UI.Pages;

public sealed partial class StreamingPage : Page
{
	public StreamingViewModel? ViewModel { get; set; }

	private TextBox? _searchTextBox;
	private Button? _loginBtn;
	private StackPanel? _accountCardContainer;
	private StackPanel? _platformListPanel;
	private StackPanel? _recentSearchesPanel;
	private StackPanel? _songListStackPanel;
	private StackPanel? _tabsPanel;
	private TextBlock? _statusNoticeTb;
	private Button? _loadMoreBtn;
	private ComboBox? _qualityCombo;

	public StreamingPage()
	{
		BuildLayout();
	}

	public StreamingPage(StreamingViewModel viewModel) : this()
	{
		ViewModel = viewModel;
		DataContext = viewModel;
		BindViewModel();
	}

	protected override void OnNavigatedTo(NavigationEventArgs e)
	{
		base.OnNavigatedTo(e);
		if (e.Parameter is StreamingViewModel vm)
		{
			ViewModel = vm;
			DataContext = vm;
			BindViewModel();
		}
	}

	private void BuildLayout()
	{
		Background = new SolidColorBrush(Colors.Transparent);

		var mainGrid = new Grid();
		mainGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(240) }); // 左侧二级侧栏
		mainGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.0, GridUnitType.Star) }); // 右侧主工作区

		// ==================== 1. 左侧二级侧边栏 ====================
		var sidebarBorder = new Border
		{
			BorderThickness = new Thickness(0, 0, 1, 0),
			BorderBrush = new SolidColorBrush(Color.FromArgb(35, 148, 163, 184)),
			Padding = new Thickness(14, 16, 14, 16)
		};
		LiquidGlassEngine.AttachInteractiveLiquidGlass(sidebarBorder, LiquidGlassVariant.FloatingBar, isDark: false, enablePointerTracking: true);

		var sidebarStack = new StackPanel { Spacing = 14 };

		// 1.1 平台列表头部
		var platformHeaderGrid = new Grid();
		platformHeaderGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.0, GridUnitType.Star) });
		platformHeaderGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

		var platTitleSp = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 6, VerticalAlignment = VerticalAlignment.Center };
		platTitleSp.Children.Add(new TextBlock
		{
			Text = "流媒体平台",
			FontSize = 11,
			FontWeight = FontWeights.Bold,
			Foreground = new SolidColorBrush(Color.FromArgb(255, 71, 85, 105))
		});
		var badge5 = new Border
		{
			Background = new SolidColorBrush(Color.FromArgb(255, 238, 242, 255)),
			CornerRadius = new CornerRadius(8),
			Padding = new Thickness(6, 1, 6, 1),
			Child = new TextBlock { Text = "5", FontSize = 10, FontWeight = FontWeights.Bold, Foreground = new SolidColorBrush(Color.FromArgb(255, 99, 102, 241)) }
		};
		platTitleSp.Children.Add(badge5);
		Grid.SetColumn(platTitleSp, 0);
		platformHeaderGrid.Children.Add(platTitleSp);

		// 账号登录按钮 (未登录显示「账号登录」，已登录显示昵称/头像)
		_loginBtn = new Button
		{
			Padding = new Thickness(6, 3, 6, 3),
			Background = new SolidColorBrush(Colors.Transparent),
			BorderThickness = new Thickness(0),
			CornerRadius = new CornerRadius(6)
		};
		_loginBtn.Click += async (s, e) =>
		{
			string plat = ViewModel?.SelectedPlatform ?? "网易云音乐";
			if (ViewModel?.IsCurrentPlatformLoggedIn == true)
			{
				await ShowAccountDetailsDialogAsync(plat);
			}
			else
			{
				await ShowLoginDialogAsync(plat);
			}
		};
		Grid.SetColumn(_loginBtn, 1);
		platformHeaderGrid.Children.Add(_loginBtn);
		sidebarStack.Children.Add(platformHeaderGrid);

		// 1.2 账号信息卡片容器 (已登录时在平台上方展示 ShinawaseLoader 风格的账号状态)
		_accountCardContainer = new StackPanel();
		sidebarStack.Children.Add(_accountCardContainer);

		// 1.3 平台列表面板
		_platformListPanel = new StackPanel { Spacing = 2 };
		sidebarStack.Children.Add(_platformListPanel);

		// 1.4 分割线
		sidebarStack.Children.Add(new Border { Height = 1, Background = new SolidColorBrush(Color.FromArgb(25, 148, 163, 184)), Margin = new Thickness(0, 6, 0, 6) });

		// 1.5 最近搜索头部
		var recentHeaderGrid = new Grid();
		recentHeaderGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.0, GridUnitType.Star) });
		recentHeaderGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

		var recTitleSp = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 6, VerticalAlignment = VerticalAlignment.Center };
		recTitleSp.Children.Add(new TextBlock
		{
			Text = "最近搜索",
			FontSize = 11,
			FontWeight = FontWeights.Bold,
			Foreground = new SolidColorBrush(Color.FromArgb(255, 71, 85, 105))
		});
		var badgeRec = new Border
		{
			Background = new SolidColorBrush(Color.FromArgb(255, 241, 245, 249)),
			CornerRadius = new CornerRadius(8),
			Padding = new Thickness(6, 1, 6, 1),
			Child = new TextBlock { Text = "7", FontSize = 10, FontWeight = FontWeights.Bold, Foreground = new SolidColorBrush(Color.FromArgb(255, 100, 116, 139)) }
		};
		recTitleSp.Children.Add(badgeRec);
		Grid.SetColumn(recTitleSp, 0);
		recentHeaderGrid.Children.Add(recTitleSp);

		var clearBtn = new Button
		{
			Content = "清空",
			FontSize = 11,
			Padding = new Thickness(6, 2, 6, 2),
			Background = new SolidColorBrush(Colors.Transparent),
			BorderThickness = new Thickness(0),
			Foreground = new SolidColorBrush(Color.FromArgb(255, 148, 163, 184))
		};
		clearBtn.Click += (s, e) => ViewModel?.ClearRecentSearchesCommand.Execute(null);
		Grid.SetColumn(clearBtn, 1);
		recentHeaderGrid.Children.Add(clearBtn);
		sidebarStack.Children.Add(recentHeaderGrid);

		// 1.6 最近搜索项列表
		_recentSearchesPanel = new StackPanel { Spacing = 2 };
		sidebarStack.Children.Add(_recentSearchesPanel);

		var sidebarSv = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto, HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled, Content = sidebarStack };
		sidebarBorder.Child = sidebarSv;
		Grid.SetColumn(sidebarBorder, 0);
		mainGrid.Children.Add(sidebarBorder);

		// ==================== 2. 右侧主工作区 ====================
		var mainAreaScroll = new ScrollViewer
		{
			VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
			HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
			Padding = new Thickness(28, 20, 28, 28)
		};

		var mainAreaStack = new StackPanel { Spacing = 16, MaxWidth = 1400 };

		// 2.1 主标题
		mainAreaStack.Children.Add(new TextBlock
		{
			Text = "流媒体音乐",
			FontSize = 22,
			FontWeight = FontWeights.ExtraBold,
			Foreground = new SolidColorBrush(Color.FromArgb(255, 15, 23, 42))
		});

		// 2.2 大搜索框条 (Liquid Glass 质感)
		var searchBarBorder = new Border
		{
			CornerRadius = new CornerRadius(12),
			Padding = new Thickness(12, 4, 8, 4),
			Background = new SolidColorBrush(Color.FromArgb(170, 255, 255, 255)),
			BorderBrush = new SolidColorBrush(Color.FromArgb(70, 255, 255, 255)),
			BorderThickness = new Thickness(1.2)
		};
		LiquidGlassEngine.AttachInteractiveLiquidGlass(searchBarBorder, LiquidGlassVariant.Regular, isDark: false, enablePointerTracking: true);

		var searchBarGrid = new Grid();
		searchBarGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
		searchBarGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.0, GridUnitType.Star) });
		searchBarGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

		var searchIcon = new FontIcon
		{
			Glyph = "\uE721",
			FontSize = 16,
			Foreground = new SolidColorBrush(Color.FromArgb(255, 100, 116, 139)),
			Margin = new Thickness(4, 0, 10, 0),
			VerticalAlignment = VerticalAlignment.Center
		};
		Grid.SetColumn(searchIcon, 0);
		searchBarGrid.Children.Add(searchIcon);

		_searchTextBox = new TextBox
		{
			PlaceholderText = "输入歌曲名、歌手、原声带或直接粘贴网络直链 (HTTP/HTTPS/M3U8)...",
			Text = "原神",
			FontSize = 14,
			BorderThickness = new Thickness(0),
			Background = new SolidColorBrush(Colors.Transparent),
			VerticalAlignment = VerticalAlignment.Center
		};
		_searchTextBox.KeyDown += (s, e) =>
		{
			if (e.Key == Windows.System.VirtualKey.Enter)
			{
				TriggerSearch();
			}
		};
		Grid.SetColumn(_searchTextBox, 1);
		searchBarGrid.Children.Add(_searchTextBox);

		var searchDoBtn = new Button
		{
			Width = 36,
			Height = 36,
			CornerRadius = new CornerRadius(18),
			Background = new SolidColorBrush(Color.FromArgb(255, 99, 102, 241)),
			Foreground = new SolidColorBrush(Colors.White),
			BorderThickness = new Thickness(0),
			Padding = new Thickness(0),
			Content = new FontIcon { Glyph = "\uE721", FontSize = 14, Foreground = new SolidColorBrush(Colors.White) }
		};
		ToolTipService.SetToolTip(searchDoBtn, "检索在线曲目 (Enter)");
		searchDoBtn.PointerEntered += (s, e) => searchDoBtn.Background = new SolidColorBrush(Color.FromArgb(255, 79, 70, 229));
		searchDoBtn.PointerExited += (s, e) => searchDoBtn.Background = new SolidColorBrush(Color.FromArgb(255, 99, 102, 241));
		searchDoBtn.Click += (s, e) => TriggerSearch();
		Grid.SetColumn(searchDoBtn, 2);
		searchBarGrid.Children.Add(searchDoBtn);

		searchBarBorder.Child = searchBarGrid;
		mainAreaStack.Children.Add(searchBarBorder);

		// 2.3 分类 Tab + 音质选择
		var filterBarGrid = new Grid();
		filterBarGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.0, GridUnitType.Star) });
		filterBarGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

		_tabsPanel = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 24 };
		var tabs = new[] { "歌曲", "专辑", "艺人", "歌单" };
		foreach (var t in tabs)
		{
			var tabBtn = CreateTabButton(t, isSelected: (t == "歌曲"));
			_tabsPanel.Children.Add(tabBtn);
		}
		Grid.SetColumn(_tabsPanel, 0);
		filterBarGrid.Children.Add(_tabsPanel);

		_qualityCombo = new ComboBox
		{
			CornerRadius = new CornerRadius(8),
			Padding = new Thickness(10, 4, 10, 4),
			FontSize = 12,
			MinWidth = 120
		};
		_qualityCombo.Items.Add(new ComboBoxItem { Content = "高音质" });
		_qualityCombo.Items.Add(new ComboBoxItem { Content = "无损 FLAC" });
		_qualityCombo.Items.Add(new ComboBoxItem { Content = "Hi-Res FLAC" });
		_qualityCombo.Items.Add(new ComboBoxItem { Content = "标准 128k" });
		_qualityCombo.SelectedIndex = 0;
		_qualityCombo.SelectionChanged += (s, e) =>
		{
			if (_qualityCombo.SelectedItem is ComboBoxItem item && ViewModel != null)
			{
				ViewModel.SelectedQuality = item.Content.ToString() ?? "高音质";
			}
		};
		Grid.SetColumn(_qualityCombo, 1);
		filterBarGrid.Children.Add(_qualityCombo);

		mainAreaStack.Children.Add(filterBarGrid);

		// 2.4 状态通知条
		_statusNoticeTb = new TextBlock
		{
			Text = "已读取 10 个歌单，可同步全部或勾选后同步。",
			FontSize = 11,
			Foreground = new SolidColorBrush(Color.FromArgb(255, 100, 116, 139)),
			Margin = new Thickness(2, -4, 0, 0)
		};
		mainAreaStack.Children.Add(_statusNoticeTb);

		// 2.5 歌曲列表表头
		var tableHeaderGrid = new Grid { Padding = new Thickness(10, 8, 10, 8) };
		tableHeaderGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(36) }); // #
		tableHeaderGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.0, GridUnitType.Star) }); // 歌曲
		tableHeaderGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(280) }); // 来源 / 音质
		tableHeaderGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(60) }); // 时长
		tableHeaderGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(110) }); // 操作

		void AddHeaderCell(string text, int col, HorizontalAlignment align = HorizontalAlignment.Left)
		{
			var tb = new TextBlock
			{
				Text = text,
				FontSize = 11,
				FontWeight = FontWeights.SemiBold,
				Foreground = new SolidColorBrush(Color.FromArgb(255, 100, 116, 139)),
				HorizontalAlignment = align
			};
			Grid.SetColumn(tb, col);
			tableHeaderGrid.Children.Add(tb);
		}

		AddHeaderCell("#", 0);
		AddHeaderCell("歌曲", 1);
		AddHeaderCell("来源 / 音质", 2);
		AddHeaderCell("时长", 3, HorizontalAlignment.Right);
		AddHeaderCell("操作", 4, HorizontalAlignment.Right);
		mainAreaStack.Children.Add(tableHeaderGrid);

		// 2.6 歌曲数据列表
		_songListStackPanel = new StackPanel { Spacing = 2 };
		mainAreaStack.Children.Add(_songListStackPanel);

		// 2.7 加载更多按钮
		_loadMoreBtn = new Button
		{
			Content = "加载更多",
			HorizontalAlignment = HorizontalAlignment.Center,
			Margin = new Thickness(0, 14, 0, 10),
			Padding = new Thickness(28, 8, 28, 8),
			CornerRadius = new CornerRadius(16),
			Background = new SolidColorBrush(Color.FromArgb(25, 99, 102, 241)),
			Foreground = new SolidColorBrush(Color.FromArgb(255, 99, 102, 241)),
			FontWeight = FontWeights.SemiBold
		};
		LiquidGlassEngine.AttachInteractiveLiquidGlass(_loadMoreBtn, LiquidGlassVariant.AccentLensed, isDark: false, enablePointerTracking: true);
		_loadMoreBtn.Click += (s, e) => ViewModel?.LoadMoreCommand.Execute(null);
		mainAreaStack.Children.Add(_loadMoreBtn);

		mainAreaScroll.Content = mainAreaStack;
		Grid.SetColumn(mainAreaScroll, 1);
		mainGrid.Children.Add(mainAreaScroll);

		Content = mainGrid;

		UpdateLoginButtonUi();
	}

	private Button CreateTabButton(string tabName, bool isSelected)
	{
		var btn = new Button
		{
			Padding = new Thickness(4, 4, 4, 8),
			Background = new SolidColorBrush(Colors.Transparent),
			BorderThickness = new Thickness(0),
			Tag = tabName
		};

		var sp = new StackPanel { Spacing = 4, HorizontalAlignment = HorizontalAlignment.Center };
		var tb = new TextBlock
		{
			Text = tabName,
			FontSize = 13,
			FontWeight = isSelected ? FontWeights.Bold : FontWeights.Normal,
			Foreground = isSelected ? new SolidColorBrush(Color.FromArgb(255, 15, 23, 42)) : new SolidColorBrush(Color.FromArgb(255, 100, 116, 139))
		};
		var indicator = new Border
		{
			Height = 2.5,
			CornerRadius = new CornerRadius(1.5),
			Background = isSelected ? new SolidColorBrush(Color.FromArgb(255, 99, 102, 241)) : new SolidColorBrush(Colors.Transparent)
		};
		sp.Children.Add(tb);
		sp.Children.Add(indicator);
		btn.Content = sp;

		btn.Click += (s, e) =>
		{
			if (ViewModel != null)
			{
				ViewModel.SelectTab(tabName);
				UpdateTabHighlights(tabName);
			}
		};
		return btn;
	}

	private void UpdateTabHighlights(string activeTab)
	{
		if (_tabsPanel == null) return;
		foreach (var child in _tabsPanel.Children)
		{
			if (child is Button b && b.Tag is string t && b.Content is StackPanel sp)
			{
				bool isSel = (t == activeTab);
				if (sp.Children[0] is TextBlock tb)
				{
					tb.FontWeight = isSel ? FontWeights.Bold : FontWeights.Normal;
					tb.Foreground = isSel ? new SolidColorBrush(Color.FromArgb(255, 15, 23, 42)) : new SolidColorBrush(Color.FromArgb(255, 100, 116, 139));
				}
				if (sp.Children[1] is Border ind)
				{
					ind.Background = isSel ? new SolidColorBrush(Color.FromArgb(255, 99, 102, 241)) : new SolidColorBrush(Colors.Transparent);
				}
			}
		}
	}

	private void TriggerSearch()
	{
		if (_searchTextBox == null || ViewModel == null) return;
		string q = _searchTextBox.Text.Trim();
		if (!string.IsNullOrEmpty(q))
		{
			ViewModel.SearchAsync(q, isNew: true);
		}
	}

	private void BindViewModel()
	{
		if (ViewModel == null) return;

		// 账号登录与状态变动事件监听
		ViewModel.AccountStateChanged += () =>
		{
			DispatcherQueue.TryEnqueue(() =>
			{
				UpdateLoginButtonUi();
				RefreshPlatformsUi();
				RefreshAccountCardUi();
			});
		};

		// 监听 ViewModel 属性变化
		ViewModel.PropertyChanged += (s, e) =>
		{
			DispatcherQueue.TryEnqueue(() =>
			{
				if (e.PropertyName == nameof(ViewModel.StatusMessage) && _statusNoticeTb != null)
				{
					_statusNoticeTb.Text = ViewModel.StatusMessage;
				}
				else if (e.PropertyName == nameof(ViewModel.IsSearching))
				{
					if (_loadMoreBtn != null)
					{
						_loadMoreBtn.IsEnabled = !ViewModel.IsSearching;
						_loadMoreBtn.Content = ViewModel.IsSearching ? "加载中..." : "加载更多";
					}
				}
				else if (e.PropertyName == nameof(ViewModel.IsNeteaseLoggedIn) ||
				         e.PropertyName == nameof(ViewModel.NeteaseNickname) ||
				         e.PropertyName == nameof(ViewModel.NeteaseAvatarUrl))
				{
					UpdateLoginButtonUi();
					RefreshPlatformsUi();
					RefreshAccountCardUi();
				}
			});
		};

		// 初始刷新 UI
		UpdateLoginButtonUi();
		RefreshAccountCardUi();
		RefreshPlatformsUi();

		// 刷新最近搜索列表 UI
		RefreshRecentSearchesUi();
		ViewModel.RecentSearches.CollectionChanged += (s, e) =>
		{
			DispatcherQueue.TryEnqueue(RefreshRecentSearchesUi);
		};

		// 刷新歌曲结果列表 UI
		RefreshResultsUi();
		ViewModel.Results.CollectionChanged += (s, e) =>
		{
			DispatcherQueue.TryEnqueue(RefreshResultsUi);
		};
	}

	/// <summary>
	/// 顶部登录按钮 UI 联动：未登录显示「登录 {平台}」，已登录显示头像、昵称与小箭头
	/// </summary>
	private void UpdateLoginButtonUi()
	{
		if (_loginBtn == null || ViewModel == null) return;

		var acc = ViewModel.CurrentPlatformAccount;
		if (acc.IsLoggedIn)
		{
			Color themeColor = acc.Platform switch
			{
				"QQ 音乐" or "QQ音乐" => Color.FromArgb(255, 16, 185, 129),
				"Spotify" => Color.FromArgb(255, 30, 215, 96),
				_ => Color.FromArgb(255, 99, 102, 241)
			};

			_loginBtn.Padding = new Thickness(6, 3, 8, 3);
			_loginBtn.Background = new SolidColorBrush(Color.FromArgb(22, themeColor.R, themeColor.G, themeColor.B));
			_loginBtn.BorderBrush = new SolidColorBrush(Color.FromArgb(50, themeColor.R, themeColor.G, themeColor.B));
			_loginBtn.BorderThickness = new Thickness(1);

			var sp = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 6, VerticalAlignment = VerticalAlignment.Center };

			var avatarBorder = new Border
			{
				Width = 20,
				Height = 20,
				CornerRadius = new CornerRadius(10),
				Background = new SolidColorBrush(Color.FromArgb(255, 238, 242, 255)),
				Clip = new RectangleGeometry { Rect = new Windows.Foundation.Rect(0, 0, 20, 20) }
			};

			if (!string.IsNullOrEmpty(acc.AvatarUrl) && Uri.TryCreate(acc.AvatarUrl, UriKind.Absolute, out var uri))
			{
				try
				{
					avatarBorder.Child = new Image { Source = new BitmapImage(uri), Stretch = Stretch.UniformToFill };
				}
				catch
				{
					avatarBorder.Child = new FontIcon { Glyph = "\uE77B", FontSize = 10, Foreground = new SolidColorBrush(themeColor) };
				}
			}
			else
			{
				avatarBorder.Child = new FontIcon { Glyph = "\uE77B", FontSize = 10, Foreground = new SolidColorBrush(themeColor) };
			}
			sp.Children.Add(avatarBorder);

			sp.Children.Add(new TextBlock
			{
				Text = !string.IsNullOrEmpty(acc.Nickname) ? acc.Nickname : acc.UserIdStr,
				FontSize = 11,
				FontWeight = FontWeights.SemiBold,
				Foreground = new SolidColorBrush(Color.FromArgb(255, 30, 41, 59)),
				MaxWidth = 72,
				TextTrimming = TextTrimming.CharacterEllipsis,
				VerticalAlignment = VerticalAlignment.Center
			});

			sp.Children.Add(new FontIcon
			{
				Glyph = "\uE70D",
				FontSize = 8,
				Foreground = new SolidColorBrush(themeColor),
				VerticalAlignment = VerticalAlignment.Center
			});

			_loginBtn.Content = sp;
		}
		else
		{
			_loginBtn.Padding = new Thickness(6, 3, 6, 3);
			_loginBtn.Background = new SolidColorBrush(Colors.Transparent);
			_loginBtn.BorderThickness = new Thickness(0);

			var sp = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 4, VerticalAlignment = VerticalAlignment.Center };
			sp.Children.Add(new FontIcon { Glyph = "\uE77B", FontSize = 11, Foreground = new SolidColorBrush(Color.FromArgb(255, 99, 102, 241)) });
			sp.Children.Add(new TextBlock { Text = $"登录 {ViewModel.SelectedPlatform}", FontSize = 11, Foreground = new SolidColorBrush(Color.FromArgb(255, 71, 85, 105)) });

			_loginBtn.Content = sp;
		}
	}

	/// <summary>
	/// 侧边栏账号概览卡片（ShinawaseLoader 风格）
	/// </summary>
	private void RefreshAccountCardUi()
	{
		if (_accountCardContainer == null || ViewModel == null) return;
		_accountCardContainer.Children.Clear();

		var acc = ViewModel.CurrentPlatformAccount;
		if (acc.IsLoggedIn)
		{
			Color themeColor = acc.Platform switch
			{
				"QQ 音乐" or "QQ音乐" => Color.FromArgb(255, 16, 185, 129),
				"Spotify" => Color.FromArgb(255, 30, 215, 96),
				_ => Color.FromArgb(255, 99, 102, 241)
			};

			var card = new Border
			{
				Background = new SolidColorBrush(Color.FromArgb(18, themeColor.R, themeColor.G, themeColor.B)),
				BorderBrush = new SolidColorBrush(Color.FromArgb(40, themeColor.R, themeColor.G, themeColor.B)),
				BorderThickness = new Thickness(1),
				CornerRadius = new CornerRadius(8),
				Padding = new Thickness(8, 6, 8, 6),
				Margin = new Thickness(0, 2, 0, 4)
			};

			var g = new Grid { ColumnSpacing = 8 };
			g.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
			g.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.0, GridUnitType.Star) });
			g.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

			var av = new Border
			{
				Width = 28,
				Height = 28,
				CornerRadius = new CornerRadius(14),
				Background = new SolidColorBrush(Color.FromArgb(255, 238, 242, 255)),
				Clip = new RectangleGeometry { Rect = new Windows.Foundation.Rect(0, 0, 28, 28) }
			};
			if (!string.IsNullOrEmpty(acc.AvatarUrl) && Uri.TryCreate(acc.AvatarUrl, UriKind.Absolute, out var uri))
			{
				try
				{
					av.Child = new Image { Source = new BitmapImage(uri), Stretch = Stretch.UniformToFill };
				}
				catch
				{
					av.Child = new FontIcon { Glyph = "\uE77B", FontSize = 12, Foreground = new SolidColorBrush(themeColor) };
				}
			}
			else
			{
				av.Child = new FontIcon { Glyph = "\uE77B", FontSize = 12, Foreground = new SolidColorBrush(themeColor) };
			}
			Grid.SetColumn(av, 0);
			g.Children.Add(av);

			var info = new StackPanel { Spacing = 1, VerticalAlignment = VerticalAlignment.Center };
			info.Children.Add(new TextBlock
			{
				Text = !string.IsNullOrEmpty(acc.Nickname) ? acc.Nickname : acc.UserIdStr,
				FontSize = 11.5,
				FontWeight = FontWeights.Bold,
				Foreground = new SolidColorBrush(Color.FromArgb(255, 30, 41, 59)),
				TextTrimming = TextTrimming.CharacterEllipsis
			});
			info.Children.Add(new TextBlock
			{
				Text = $"{acc.Platform} · {acc.VipStatus} · 在线",
				FontSize = 9.5,
				Foreground = new SolidColorBrush(Color.FromArgb(255, 22, 163, 74))
			});
			Grid.SetColumn(info, 1);
			g.Children.Add(info);

			var optBtn = new Button
			{
				Width = 24,
				Height = 24,
				Padding = new Thickness(0),
				CornerRadius = new CornerRadius(12),
				Background = new SolidColorBrush(Colors.Transparent),
				BorderThickness = new Thickness(0),
				Content = new FontIcon { Glyph = "\uE712", FontSize = 11, Foreground = new SolidColorBrush(Color.FromArgb(255, 100, 116, 139)) }
			};
			optBtn.Click += async (s, e) => await ShowAccountDetailsDialogAsync(acc.Platform);
			Grid.SetColumn(optBtn, 2);
			g.Children.Add(optBtn);

			card.Child = g;
			_accountCardContainer.Children.Add(card);
		}
	}

	/// <summary>
	/// 侧边栏多平台状态渲染（ShinawaseLoader 风格：状态圆点 + 平台名称与副标题 + 状态徽章）
	/// </summary>
	private void RefreshPlatformsUi()
	{
		if (_platformListPanel == null || ViewModel == null) return;
		_platformListPanel.Children.Clear();

		foreach (var p in ViewModel.Platforms)
		{
			var btn = new Button
			{
				HorizontalAlignment = HorizontalAlignment.Stretch,
				HorizontalContentAlignment = HorizontalAlignment.Stretch,
				Padding = new Thickness(10, 7, 10, 7),
				CornerRadius = new CornerRadius(8),
				BorderThickness = new Thickness(p.IsSelected ? 1 : 0),
				BorderBrush = p.IsSelected ? new SolidColorBrush(Color.FromArgb(60, 99, 102, 241)) : new SolidColorBrush(Colors.Transparent),
				Background = p.IsSelected ? new SolidColorBrush(Color.FromArgb(25, 99, 102, 241)) : new SolidColorBrush(Colors.Transparent),
				Tag = p.Name
			};

			var rowGrid = new Grid { ColumnSpacing = 8 };
			rowGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto }); // 状态圆点
			rowGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.0, GridUnitType.Star) }); // 平台与状态副标题
			rowGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto }); // 状态徽章 (已登录/未登录)

			// 状态圆点 (ShinawaseLoader 风格)
			bool isGood = p.IsLoggedIn || (p.IsAvailable && p.Name != "网易云音乐");
			var dot = new Border
			{
				Width = 8,
				Height = 8,
				CornerRadius = new CornerRadius(4),
				Background = isGood
					? new SolidColorBrush(Color.FromArgb(255, 34, 197, 94)) // 翠绿色
					: new SolidColorBrush(Color.FromArgb(255, 148, 163, 184)), // 状态灰
				VerticalAlignment = VerticalAlignment.Center
			};
			Grid.SetColumn(dot, 0);
			rowGrid.Children.Add(dot);

			var sp = new StackPanel { Spacing = 1, VerticalAlignment = VerticalAlignment.Center };
			sp.Children.Add(new TextBlock
			{
				Text = p.Name,
				FontSize = 12,
				FontWeight = p.IsSelected ? FontWeights.Bold : FontWeights.SemiBold,
				Foreground = p.IsSelected ? new SolidColorBrush(Color.FromArgb(255, 79, 70, 229)) : new SolidColorBrush(Color.FromArgb(255, 30, 41, 59))
			});
			sp.Children.Add(new TextBlock
			{
				Text = p.Subtitle,
				FontSize = 10,
				Foreground = new SolidColorBrush(Color.FromArgb(255, 148, 163, 184)),
				TextTrimming = TextTrimming.CharacterEllipsis
			});
			Grid.SetColumn(sp, 1);
			rowGrid.Children.Add(sp);

			// 状态徽章标签 (ShinawaseLoader 风格)
			var badgeBorder = new Border
			{
				CornerRadius = new CornerRadius(4),
				Padding = new Thickness(5, 2, 5, 2),
				VerticalAlignment = VerticalAlignment.Center,
				Background = isGood
					? new SolidColorBrush(Color.FromArgb(255, 235, 253, 242)) // 浅绿
					: new SolidColorBrush(Color.FromArgb(255, 241, 245, 249))  // 浅灰
			};
			var badgeTb = new TextBlock
			{
				Text = p.StatusBadgeText,
				FontSize = 9.5,
				FontWeight = FontWeights.SemiBold,
				Foreground = isGood
					? new SolidColorBrush(Color.FromArgb(255, 22, 163, 74)) // 绿字
					: new SolidColorBrush(Color.FromArgb(255, 148, 163, 184)) // 灰字
			};
			badgeBorder.Child = badgeTb;
			Grid.SetColumn(badgeBorder, 2);
			rowGrid.Children.Add(badgeBorder);

			btn.Content = rowGrid;
			btn.Click += (s, e) =>
			{
				if (s is Button b && b.Tag is string platName)
				{
					ViewModel.SelectPlatform(platName);
					RefreshPlatformsUi();
					UpdateLoginButtonUi();
					RefreshAccountCardUi();
				}
			};
			_platformListPanel.Children.Add(btn);
		}
	}

	/// <summary>
	/// 弹出登录认证对话框：支持网易云扫码登录（带轮询与超时处理）与 Cookie (MUSIC_U) 导入认证、QQ 音乐 Cookie 导入、Spotify 凭证导入
	/// </summary>
	private async Task ShowLoginDialogAsync(string? defaultPlatform = null)
	{
		if (ViewModel == null || this.XamlRoot == null) return;

		var dialog = new ContentDialog
		{
			Title = "流媒体账号认证与登录",
			CloseButtonText = "取消",
			CornerRadius = new CornerRadius(16),
			XamlRoot = this.XamlRoot
		};

		var cts = new CancellationTokenSource();
		dialog.Closed += (s, e) =>
		{
			try
			{
				cts.Cancel();
				cts.Dispose();
			}
			catch { }
		};

		var rootSp = new StackPanel { Width = 460, Spacing = 14 };

		var introTb = new TextBlock
		{
			Text = "连接各大流媒体音乐平台账号，可同步个人收藏、导入专属歌单，并直通最高音质与 VIP 音源解析播放。凭证将自动加密保存在本地配置中。",
			FontSize = 12,
			Foreground = new SolidColorBrush(Color.FromArgb(255, 100, 116, 139)),
			TextWrapping = TextWrapping.Wrap
		};
		rootSp.Children.Add(introTb);

		var pivot = new Pivot();

		// ==================== 1. 网易云扫码快速登录 ====================
		var qrTab = new PivotItem
		{
			Header = new StackPanel
			{
				Orientation = Orientation.Horizontal,
				Spacing = 6,
				Children =
				{
					new FontIcon { Glyph = "\uEC64", FontSize = 12 },
					new TextBlock { Text = "网易云扫码", FontSize = 12 }
				}
			}
		};

		var qrStack = new StackPanel { Spacing = 12, HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0, 10, 0, 0) };

		var qrBorder = new Border
		{
			Width = 200,
			Height = 200,
			CornerRadius = new CornerRadius(12),
			Background = new SolidColorBrush(Colors.White),
			BorderBrush = new SolidColorBrush(Color.FromArgb(255, 226, 232, 240)),
			BorderThickness = new Thickness(1.5),
			Padding = new Thickness(10)
		};

		var qrGrid = new Grid();
		var qrImg = new Image
		{
			Width = 180,
			Height = 180,
			Stretch = Stretch.Uniform
		};
		var qrLoadingRing = new ProgressRing
		{
			Width = 36,
			Height = 36,
			IsActive = true,
			HorizontalAlignment = HorizontalAlignment.Center,
			VerticalAlignment = VerticalAlignment.Center
		};

		var expiredOverlay = new Border
		{
			Background = new SolidColorBrush(Color.FromArgb(210, 255, 255, 255)),
			CornerRadius = new CornerRadius(12),
			Visibility = Visibility.Collapsed
		};
		var refreshOverlayBtn = new Button
		{
			Content = "二维码已失效，点击刷新",
			FontSize = 11.5,
			Padding = new Thickness(12, 6, 12, 6),
			CornerRadius = new CornerRadius(6),
			HorizontalAlignment = HorizontalAlignment.Center,
			VerticalAlignment = VerticalAlignment.Center
		};
		expiredOverlay.Child = refreshOverlayBtn;

		qrGrid.Children.Add(qrImg);
		qrGrid.Children.Add(qrLoadingRing);
		qrGrid.Children.Add(expiredOverlay);
		qrBorder.Child = qrGrid;
		qrStack.Children.Add(qrBorder);

		var statusTb = new TextBlock
		{
			Text = "正在生成登录二维码...",
			FontSize = 12,
			FontWeight = FontWeights.SemiBold,
			Foreground = new SolidColorBrush(Color.FromArgb(255, 71, 85, 105)),
			HorizontalAlignment = HorizontalAlignment.Center
		};
		qrStack.Children.Add(statusTb);

		var qrActionSp = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 10, HorizontalAlignment = HorizontalAlignment.Center };
		var refreshBtn = new Button
		{
			Content = "刷新二维码",
			FontSize = 11,
			CornerRadius = new CornerRadius(6)
		};
		var copyLinkBtn = new Button
		{
			Content = "复制扫码直链",
			FontSize = 11,
			CornerRadius = new CornerRadius(6)
		};
		qrActionSp.Children.Add(refreshBtn);
		qrActionSp.Children.Add(copyLinkBtn);
		qrStack.Children.Add(qrActionSp);

		qrTab.Content = qrStack;
		pivot.Items.Add(qrTab);

		// ==================== 2. 网易云 Cookie 凭证导入 ====================
		var cookieTab = new PivotItem
		{
			Header = new StackPanel
			{
				Orientation = Orientation.Horizontal,
				Spacing = 6,
				Children =
				{
					new FontIcon { Glyph = "\uE8C8", FontSize = 12 },
					new TextBlock { Text = "网易云 Cookie", FontSize = 12 }
				}
			}
		};

		var cookieStack = new StackPanel { Spacing = 10, Margin = new Thickness(0, 10, 0, 0) };
		cookieStack.Children.Add(new TextBlock
		{
			Text = "支持直接粘贴网易云音乐账号凭据中的 MUSIC_U 字段值或完整 Cookie 字符串：",
			FontSize = 12,
			Foreground = new SolidColorBrush(Color.FromArgb(255, 71, 85, 105)),
			TextWrapping = TextWrapping.Wrap
		});

		var cookieBox = new TextBox
		{
			PlaceholderText = "粘贴包含 MUSIC_U=xxxx 的 Cookie 字符串，或直接粘贴 MUSIC_U 值...",
			AcceptsReturn = true,
			TextWrapping = TextWrapping.Wrap,
			Height = 85,
			FontSize = 12,
			CornerRadius = new CornerRadius(8)
		};
		cookieStack.Children.Add(cookieBox);

		var cookieStatusTb = new TextBlock
		{
			FontSize = 11.5,
			TextWrapping = TextWrapping.Wrap,
			Visibility = Visibility.Collapsed
		};
		cookieStack.Children.Add(cookieStatusTb);

		var cookieLoginBtn = new Button
		{
			Content = "验证并登录网易云",
			Background = new SolidColorBrush(Color.FromArgb(255, 99, 102, 241)),
			Foreground = new SolidColorBrush(Colors.White),
			FontWeight = FontWeights.SemiBold,
			CornerRadius = new CornerRadius(8),
			Padding = new Thickness(24, 7, 24, 7),
			HorizontalAlignment = HorizontalAlignment.Right
		};
		cookieLoginBtn.Click += async (s, e) =>
		{
			string txt = cookieBox.Text.Trim();
			if (string.IsNullOrEmpty(txt))
			{
				cookieStatusTb.Text = "请输入或粘贴 Cookie 内容！";
				cookieStatusTb.Foreground = new SolidColorBrush(Color.FromArgb(255, 239, 68, 68));
				cookieStatusTb.Visibility = Visibility.Visible;
				return;
			}

			cookieLoginBtn.IsEnabled = false;
			cookieLoginBtn.Content = "正在校验...";
			cookieStatusTb.Text = "正在向网易云服务器验证登录凭证...";
			cookieStatusTb.Foreground = new SolidColorBrush(Color.FromArgb(255, 99, 102, 241));
			cookieStatusTb.Visibility = Visibility.Visible;

			var (ok, msg) = await ViewModel.LoginWithCookieAsync(txt);
			cookieLoginBtn.IsEnabled = true;
			cookieLoginBtn.Content = "验证并登录网易云";

			if (ok)
			{
				cookieStatusTb.Text = msg;
				cookieStatusTb.Foreground = new SolidColorBrush(Color.FromArgb(255, 34, 197, 94));
				await Task.Delay(1000);
				dialog.Hide();
			}
			else
			{
				cookieStatusTb.Text = msg;
				cookieStatusTb.Foreground = new SolidColorBrush(Color.FromArgb(255, 239, 68, 68));
			}
		};
		cookieStack.Children.Add(cookieLoginBtn);

		cookieTab.Content = cookieStack;
		pivot.Items.Add(cookieTab);

		// ==================== 3. QQ 音乐 Cookie 导入 ====================
		var qqTab = new PivotItem
		{
			Header = new StackPanel
			{
				Orientation = Orientation.Horizontal,
				Spacing = 6,
				Children =
				{
					new FontIcon { Glyph = "\uE8D6", FontSize = 12 },
					new TextBlock { Text = "QQ音乐 Cookie", FontSize = 12 }
				}
			}
		};

		var qqStack = new StackPanel { Spacing = 10, Margin = new Thickness(0, 10, 0, 0) };
		qqStack.Children.Add(new TextBlock
		{
			Text = "支持导入 QQ 音乐网页端 (y.qq.com) Cookie。需包含 uin (QQ号) 与 qm_keyst / qqmusic_key 凭证，用于登录认证与无损音频解析：",
			FontSize = 12,
			Foreground = new SolidColorBrush(Color.FromArgb(255, 71, 85, 105)),
			TextWrapping = TextWrapping.Wrap
		});

		var qqCookieBox = new TextBox
		{
			PlaceholderText = "粘贴包含 uin=xxxx; qm_keyst=xxxx 的完整 Cookie 字符串...",
			AcceptsReturn = true,
			TextWrapping = TextWrapping.Wrap,
			Height = 85,
			FontSize = 12,
			CornerRadius = new CornerRadius(8)
		};
		qqStack.Children.Add(qqCookieBox);

		var qqStatusTb = new TextBlock
		{
			FontSize = 11.5,
			TextWrapping = TextWrapping.Wrap,
			Visibility = Visibility.Collapsed
		};
		qqStack.Children.Add(qqStatusTb);

		var qqLoginBtn = new Button
		{
			Content = "验证并登录 QQ 音乐",
			Background = new SolidColorBrush(Color.FromArgb(255, 16, 185, 129)),
			Foreground = new SolidColorBrush(Colors.White),
			FontWeight = FontWeights.SemiBold,
			CornerRadius = new CornerRadius(8),
			Padding = new Thickness(24, 7, 24, 7),
			HorizontalAlignment = HorizontalAlignment.Right
		};
		qqLoginBtn.Click += async (s, e) =>
		{
			string txt = qqCookieBox.Text.Trim();
			if (string.IsNullOrEmpty(txt))
			{
				qqStatusTb.Text = "请输入或粘贴 QQ 音乐 Cookie 内容！";
				qqStatusTb.Foreground = new SolidColorBrush(Color.FromArgb(255, 239, 68, 68));
				qqStatusTb.Visibility = Visibility.Visible;
				return;
			}

			qqLoginBtn.IsEnabled = false;
			qqLoginBtn.Content = "正在校验...";
			qqStatusTb.Text = "正在解析 QQ 音乐账号凭据...";
			qqStatusTb.Foreground = new SolidColorBrush(Color.FromArgb(255, 16, 185, 129));
			qqStatusTb.Visibility = Visibility.Visible;

			var (ok, msg) = await ViewModel.LoginQQWithCookieAsync(txt);
			qqLoginBtn.IsEnabled = true;
			qqLoginBtn.Content = "验证并登录 QQ 音乐";

			if (ok)
			{
				qqStatusTb.Text = msg;
				qqStatusTb.Foreground = new SolidColorBrush(Color.FromArgb(255, 34, 197, 94));
				await Task.Delay(1000);
				dialog.Hide();
			}
			else
			{
				qqStatusTb.Text = msg;
				qqStatusTb.Foreground = new SolidColorBrush(Color.FromArgb(255, 239, 68, 68));
			}
		};
		qqStack.Children.Add(qqLoginBtn);

		qqTab.Content = qqStack;
		pivot.Items.Add(qqTab);

		// ==================== 4. Spotify 凭据导入 ====================
		var spTab = new PivotItem
		{
			Header = new StackPanel
			{
				Orientation = Orientation.Horizontal,
				Spacing = 6,
				Children =
				{
					new FontIcon { Glyph = "\uE93C", FontSize = 12 },
					new TextBlock { Text = "Spotify 凭证", FontSize = 12 }
				}
			}
		};

		var spStack = new StackPanel { Spacing = 10, Margin = new Thickness(0, 10, 0, 0) };
		spStack.Children.Add(new TextBlock
		{
			Text = "支持导入 Spotify 官方 Access Token (BQD...) 或网页端 sp_dc 会话 Cookie，用于同步 Spotify 音乐账户：",
			FontSize = 12,
			Foreground = new SolidColorBrush(Color.FromArgb(255, 71, 85, 105)),
			TextWrapping = TextWrapping.Wrap
		});

		var spCredentialBox = new TextBox
		{
			PlaceholderText = "粘贴 Spotify Access Token 或 sp_dc=xxxx Cookie 字符串...",
			AcceptsReturn = true,
			TextWrapping = TextWrapping.Wrap,
			Height = 85,
			FontSize = 12,
			CornerRadius = new CornerRadius(8)
		};
		spStack.Children.Add(spCredentialBox);

		var spStatusTb = new TextBlock
		{
			FontSize = 11.5,
			TextWrapping = TextWrapping.Wrap,
			Visibility = Visibility.Collapsed
		};
		spStack.Children.Add(spStatusTb);

		var spLoginBtn = new Button
		{
			Content = "验证并登录 Spotify",
			Background = new SolidColorBrush(Color.FromArgb(255, 30, 215, 96)),
			Foreground = new SolidColorBrush(Colors.White),
			FontWeight = FontWeights.SemiBold,
			CornerRadius = new CornerRadius(8),
			Padding = new Thickness(24, 7, 24, 7),
			HorizontalAlignment = HorizontalAlignment.Right
		};
		spLoginBtn.Click += async (s, e) =>
		{
			string txt = spCredentialBox.Text.Trim();
			if (string.IsNullOrEmpty(txt))
			{
				spStatusTb.Text = "请输入或粘贴 Spotify Token 或 Cookie！";
				spStatusTb.Foreground = new SolidColorBrush(Color.FromArgb(255, 239, 68, 68));
				spStatusTb.Visibility = Visibility.Visible;
				return;
			}

			spLoginBtn.IsEnabled = false;
			spLoginBtn.Content = "正在校验...";
			spStatusTb.Text = "正在向 Spotify 验证账户凭证...";
			spStatusTb.Foreground = new SolidColorBrush(Color.FromArgb(255, 30, 215, 96));
			spStatusTb.Visibility = Visibility.Visible;

			var (ok, msg) = await ViewModel.LoginSpotifyWithCredentialAsync(txt);
			spLoginBtn.IsEnabled = true;
			spLoginBtn.Content = "验证并登录 Spotify";

			if (ok)
			{
				spStatusTb.Text = msg;
				spStatusTb.Foreground = new SolidColorBrush(Color.FromArgb(255, 34, 197, 94));
				await Task.Delay(1000);
				dialog.Hide();
			}
			else
			{
				spStatusTb.Text = msg;
				spStatusTb.Foreground = new SolidColorBrush(Color.FromArgb(255, 239, 68, 68));
			}
		};
		spStack.Children.Add(spLoginBtn);

		spTab.Content = spStack;
		pivot.Items.Add(spTab);

		// 默认激活对应所选平台的 Tab
		string activePlat = defaultPlatform ?? ViewModel.SelectedPlatform;
		if (activePlat == "QQ 音乐" || activePlat == "QQ音乐")
		{
			pivot.SelectedIndex = 2;
		}
		else if (activePlat == "Spotify")
		{
			pivot.SelectedIndex = 3;
		}
		else
		{
			pivot.SelectedIndex = 0;
		}

		rootSp.Children.Add(pivot);
		dialog.Content = rootSp;

		// 扫码逻辑控制器
		string currentUnikey = "";
		string currentQrUrl = "";

		async Task LoadQrAsync()
		{
			expiredOverlay.Visibility = Visibility.Collapsed;
			qrLoadingRing.IsActive = true;
			statusTb.Text = "正在向网易云请求登录二维码...";
			statusTb.Foreground = new SolidColorBrush(Color.FromArgb(255, 71, 85, 105));

			string? unikey = await ViewModel.RequestQrKeyAsync();
			if (string.IsNullOrEmpty(unikey))
			{
				qrLoadingRing.IsActive = false;
				statusTb.Text = "获取二维码密钥失败，请检查网络后重试。";
				statusTb.Foreground = new SolidColorBrush(Color.FromArgb(255, 239, 68, 68));
				expiredOverlay.Visibility = Visibility.Visible;
				return;
			}

			currentUnikey = unikey;
			var (qrUrl, qrImgBase64) = await ViewModel.CreateQrAsync(unikey);
			currentQrUrl = qrUrl;

			try
			{
				if (!string.IsNullOrEmpty(qrImgBase64) && qrImgBase64.StartsWith("data:image"))
				{
					string pureBase64 = qrImgBase64.Substring(qrImgBase64.IndexOf(',') + 1);
					byte[] imgBytes = Convert.FromBase64String(pureBase64);
					using var ms = new MemoryStream(imgBytes);
					var ras = ms.AsRandomAccessStream();
					var bi = new BitmapImage();
					await bi.SetSourceAsync(ras);
					qrImg.Source = bi;
				}
				else
				{
					string qrApiUrl = $"https://api.qrserver.com/v1/create-qr-code/?size=200x200&margin=8&data={Uri.EscapeDataString(qrUrl)}";
					qrImg.Source = new BitmapImage(new Uri(qrApiUrl));
				}
			}
			catch
			{
				string fallbackQr = $"https://quickchart.io/qr?size=200&text={Uri.EscapeDataString(qrUrl)}";
				qrImg.Source = new BitmapImage(new Uri(fallbackQr));
			}

			qrLoadingRing.IsActive = false;
			statusTb.Text = "请使用网易云音乐手机 App 扫码登录";
			statusTb.Foreground = new SolidColorBrush(Color.FromArgb(255, 71, 85, 105));

			_ = PollQrStatusAsync(unikey, cts.Token);
		}

		async Task PollQrStatusAsync(string unikey, CancellationToken ct)
		{
			while (!ct.IsCancellationRequested)
			{
				await Task.Delay(2000, ct).ContinueWith(_ => { });
				if (ct.IsCancellationRequested || unikey != currentUnikey) break;

				var (code, msg, success) = await ViewModel.CheckQrStatusAsync(unikey);
				if (success)
				{
					statusTb.Text = "授权登录成功！正在加载账号资料...";
					statusTb.Foreground = new SolidColorBrush(Color.FromArgb(255, 34, 197, 94));
					await Task.Delay(1000);
					dialog.Hide();
					break;
				}

				if (code == 800) // 二维码已过期
				{
					statusTb.Text = "二维码已过期，请点击刷新";
					statusTb.Foreground = new SolidColorBrush(Color.FromArgb(255, 239, 68, 68));
					expiredOverlay.Visibility = Visibility.Visible;
					break;
				}
				else if (code == 802) // 已扫码待确认
				{
					statusTb.Text = "扫描成功，请在手机端点击【确认登录】";
					statusTb.Foreground = new SolidColorBrush(Color.FromArgb(255, 234, 88, 12));
				}
			}
		}

		refreshBtn.Click += async (s, e) => await LoadQrAsync();
		refreshOverlayBtn.Click += async (s, e) => await LoadQrAsync();
		copyLinkBtn.Click += (s, e) =>
		{
			if (!string.IsNullOrEmpty(currentQrUrl))
			{
				var dp = new DataPackage();
				dp.SetText(currentQrUrl);
				Clipboard.SetContent(dp);
				copyLinkBtn.Content = "已复制直链！";
				_ = Task.Delay(2000).ContinueWith(_ => DispatcherQueue.TryEnqueue(() => copyLinkBtn.Content = "复制扫码直链"));
			}
		};

		if (pivot.SelectedIndex == 0)
		{
			_ = LoadQrAsync();
		}

		await dialog.ShowAsync();
	}

	/// <summary>
	/// 弹出已登录账号详情面板：查看平台、昵称、头像、UID/UIN、认证状态、凭据脱敏预览，支持刷新与安全退出
	/// </summary>
	private async Task ShowAccountDetailsDialogAsync(string? targetPlatform = null)
	{
		if (ViewModel == null || this.XamlRoot == null) return;

		string platform = targetPlatform ?? ViewModel.SelectedPlatform;
		var acc = ViewModel.GetAccount(platform);

		Color themeColor = platform switch
		{
			"QQ 音乐" or "QQ音乐" => Color.FromArgb(255, 16, 185, 129),
			"Spotify" => Color.FromArgb(255, 30, 215, 96),
			_ => Color.FromArgb(255, 99, 102, 241)
		};

		var dialog = new ContentDialog
		{
			Title = $"{platform} · 账号信息",
			CloseButtonText = "关闭",
			CornerRadius = new CornerRadius(16),
			XamlRoot = this.XamlRoot
		};

		var rootSp = new StackPanel { Width = 400, Spacing = 16 };

		// 账号卡片
		var cardBorder = new Border
		{
			Background = new SolidColorBrush(Color.FromArgb(18, themeColor.R, themeColor.G, themeColor.B)),
			BorderBrush = new SolidColorBrush(Color.FromArgb(40, themeColor.R, themeColor.G, themeColor.B)),
			BorderThickness = new Thickness(1),
			CornerRadius = new CornerRadius(12),
			Padding = new Thickness(16)
		};

		var cardGrid = new Grid { ColumnSpacing = 14 };
		cardGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
		cardGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.0, GridUnitType.Star) });

		var avatarBorder = new Border
		{
			Width = 56,
			Height = 56,
			CornerRadius = new CornerRadius(28),
			Background = new SolidColorBrush(Color.FromArgb(255, 238, 242, 255)),
			Clip = new RectangleGeometry { Rect = new Windows.Foundation.Rect(0, 0, 56, 56) }
		};
		if (!string.IsNullOrEmpty(acc.AvatarUrl) && Uri.TryCreate(acc.AvatarUrl, UriKind.Absolute, out var uri))
		{
			try
			{
				avatarBorder.Child = new Image { Source = new BitmapImage(uri), Stretch = Stretch.UniformToFill };
			}
			catch
			{
				avatarBorder.Child = new FontIcon { Glyph = "\uE77B", FontSize = 22, Foreground = new SolidColorBrush(themeColor) };
			}
		}
		else
		{
			avatarBorder.Child = new FontIcon { Glyph = "\uE77B", FontSize = 22, Foreground = new SolidColorBrush(themeColor) };
		}
		Grid.SetColumn(avatarBorder, 0);
		cardGrid.Children.Add(avatarBorder);

		var infoSp = new StackPanel { Spacing = 4, VerticalAlignment = VerticalAlignment.Center };
		infoSp.Children.Add(new TextBlock
		{
			Text = !string.IsNullOrEmpty(acc.Nickname) ? acc.Nickname : (string.IsNullOrEmpty(acc.UserIdStr) ? "认证用户" : acc.UserIdStr),
			FontSize = 16,
			FontWeight = FontWeights.Bold,
			Foreground = new SolidColorBrush(Color.FromArgb(255, 15, 23, 42))
		});

		var tagSp = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 6 };
		var vipBadge = new Border
		{
			Background = new SolidColorBrush(Color.FromArgb(255, 238, 242, 255)),
			CornerRadius = new CornerRadius(4),
			Padding = new Thickness(6, 1, 6, 1),
			Child = new TextBlock
			{
				Text = acc.VipStatus,
				FontSize = 10,
				FontWeight = FontWeights.SemiBold,
				Foreground = new SolidColorBrush(themeColor)
			}
		};
		tagSp.Children.Add(vipBadge);

		string idLabel = platform switch
		{
			"QQ 音乐" or "QQ音乐" => $"QQ: {acc.UserIdStr}",
			"Spotify" => $"ID: {acc.UserIdStr}",
			_ => $"UID: {(acc.UserId > 0 ? acc.UserId.ToString() : acc.UserIdStr)}"
		};

		if (!string.IsNullOrEmpty(acc.UserIdStr) || acc.UserId > 0)
		{
			tagSp.Children.Add(new TextBlock
			{
				Text = idLabel,
				FontSize = 10,
				Foreground = new SolidColorBrush(Color.FromArgb(255, 148, 163, 184)),
				VerticalAlignment = VerticalAlignment.Center
			});
		}
		infoSp.Children.Add(tagSp);

		infoSp.Children.Add(new TextBlock
		{
			Text = "状态: 已通过认证 · 凭据已持久化",
			FontSize = 10.5,
			Foreground = new SolidColorBrush(Color.FromArgb(255, 34, 197, 94))
		});

		// 凭证脱敏预览
		if (!string.IsNullOrEmpty(acc.Cookie))
		{
			string c = acc.Cookie.Trim();
			string preview = c.Length > 24
				? $"{c.Substring(0, 12)}...{c.Substring(c.Length - 8)}"
				: c;
			infoSp.Children.Add(new TextBlock
			{
				Text = $"凭证: {preview}",
				FontSize = 9.5,
				Foreground = new SolidColorBrush(Color.FromArgb(255, 148, 163, 184)),
				TextTrimming = TextTrimming.CharacterEllipsis
			});
		}

		Grid.SetColumn(infoSp, 1);
		cardGrid.Children.Add(infoSp);
		cardBorder.Child = cardGrid;
		rootSp.Children.Add(cardBorder);

		// 操作按钮
		var actionsGrid = new Grid { ColumnSpacing = 10 };
		actionsGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.0, GridUnitType.Star) });
		actionsGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.0, GridUnitType.Star) });

		var refreshBtn = new Button
		{
			Content = "刷新状态",
			HorizontalAlignment = HorizontalAlignment.Stretch,
			CornerRadius = new CornerRadius(8)
		};
		refreshBtn.Click += async (s, e) =>
		{
			refreshBtn.IsEnabled = false;
			refreshBtn.Content = "刷新中...";
			var (ok, msg) = await ViewModel.RefreshAccountStatusAsync(platform);
			refreshBtn.IsEnabled = true;
			refreshBtn.Content = "刷新状态";
			if (!ok)
			{
				dialog.Hide();
			}
		};
		Grid.SetColumn(refreshBtn, 0);
		actionsGrid.Children.Add(refreshBtn);

		var logoutBtn = new Button
		{
			Content = "退出登录",
			Foreground = new SolidColorBrush(Color.FromArgb(255, 239, 68, 68)),
			HorizontalAlignment = HorizontalAlignment.Stretch,
			CornerRadius = new CornerRadius(8)
		};
		logoutBtn.Click += (s, e) =>
		{
			ViewModel.Logout(platform);
			dialog.Hide();
		};
		Grid.SetColumn(logoutBtn, 1);
		actionsGrid.Children.Add(logoutBtn);

		rootSp.Children.Add(actionsGrid);
		dialog.Content = rootSp;

		await dialog.ShowAsync();
	}

	private void RefreshRecentSearchesUi()
	{
		if (_recentSearchesPanel == null || ViewModel == null) return;
		_recentSearchesPanel.Children.Clear();

		foreach (var r in ViewModel.RecentSearches)
		{
			var btn = new Button
			{
				HorizontalAlignment = HorizontalAlignment.Stretch,
				HorizontalContentAlignment = HorizontalAlignment.Left,
				Padding = new Thickness(8, 4, 8, 4),
				CornerRadius = new CornerRadius(6),
				Background = new SolidColorBrush(Colors.Transparent),
				BorderThickness = new Thickness(0),
				Tag = r
			};

			var sp = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
			sp.Children.Add(new FontIcon { Glyph = "\uE721", FontSize = 11, Foreground = new SolidColorBrush(Color.FromArgb(255, 148, 163, 184)), VerticalAlignment = VerticalAlignment.Center });
			sp.Children.Add(new TextBlock { Text = r, FontSize = 11, Foreground = new SolidColorBrush(Color.FromArgb(255, 71, 85, 105)), VerticalAlignment = VerticalAlignment.Center });

			btn.Content = sp;
			btn.Click += (s, e) =>
			{
				if (s is Button b && b.Tag is string term)
				{
					if (_searchTextBox != null) _searchTextBox.Text = term;
					ViewModel.SearchAsync(term, isNew: true);
				}
			};
			_recentSearchesPanel.Children.Add(btn);
		}
	}

	private void RefreshResultsUi()
	{
		if (_songListStackPanel == null || ViewModel == null) return;
		_songListStackPanel.Children.Clear();

		foreach (var item in ViewModel.Results)
		{
			var rowCard = new Border
			{
				CornerRadius = new CornerRadius(8),
				Padding = new Thickness(10, 6, 10, 6),
				Margin = new Thickness(0, 1, 0, 1),
				Background = new SolidColorBrush(Colors.Transparent)
			};

			rowCard.PointerEntered += (s, e) =>
			{
				if (s is Border b)
				{
					b.Background = new SolidColorBrush(Color.FromArgb(25, 99, 102, 241));
				}
			};
			rowCard.PointerExited += (s, e) =>
			{
				if (s is Border b)
				{
					b.Background = new SolidColorBrush(Colors.Transparent);
				}
			};

			var rowGrid = new Grid();
			rowGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(36) }); // #
			rowGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.0, GridUnitType.Star) }); // 歌曲
			rowGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(280) }); // 来源 / 音质
			rowGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(60) }); // 时长
			rowGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(110) }); // 操作

			// 0. 序号
			var idxTb = new TextBlock
			{
				Text = item.DisplayIndex.ToString(),
				FontSize = 11,
				Foreground = new SolidColorBrush(Color.FromArgb(255, 148, 163, 184)),
				VerticalAlignment = VerticalAlignment.Center
			};
			Grid.SetColumn(idxTb, 0);
			rowGrid.Children.Add(idxTb);

			// 1. 歌曲 (封面 + 标题 + 歌手专辑)
			var songInfoSp = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 10, VerticalAlignment = VerticalAlignment.Center };
			var coverBorder = new Border
			{
				Width = 36,
				Height = 36,
				CornerRadius = new CornerRadius(6),
				Background = new SolidColorBrush(Color.FromArgb(255, 238, 242, 255))
			};
			if (!string.IsNullOrEmpty(item.CoverUrl) && Uri.TryCreate(item.CoverUrl, UriKind.Absolute, out var coverUri))
			{
				try
				{
					coverBorder.Background = new ImageBrush
					{
						ImageSource = new BitmapImage(coverUri)
						{
							DecodePixelWidth = 72,
							DecodePixelHeight = 72
						},
						Stretch = Stretch.UniformToFill
					};
				}
				catch
				{
					coverBorder.Child = new FontIcon { Glyph = "\uE774", FontSize = 14, Foreground = new SolidColorBrush(Color.FromArgb(255, 99, 102, 241)) };
				}
			}
			else
			{
				coverBorder.Child = new FontIcon { Glyph = "\uE774", FontSize = 14, Foreground = new SolidColorBrush(Color.FromArgb(255, 99, 102, 241)) };
			}
			songInfoSp.Children.Add(coverBorder);

			var titleStack = new StackPanel { Spacing = 2, VerticalAlignment = VerticalAlignment.Center };
			titleStack.Children.Add(new TextBlock
			{
				Text = item.Title,
				FontSize = 12.5,
				FontWeight = FontWeights.SemiBold,
				Foreground = new SolidColorBrush(Color.FromArgb(255, 15, 23, 42)),
				TextTrimming = TextTrimming.CharacterEllipsis,
				MaxWidth = 420
			});
			titleStack.Children.Add(new TextBlock
			{
				Text = item.ArtistAndAlbum,
				FontSize = 10.5,
				Foreground = new SolidColorBrush(Color.FromArgb(255, 100, 116, 139)),
				TextTrimming = TextTrimming.CharacterEllipsis,
				MaxWidth = 420
			});
			songInfoSp.Children.Add(titleStack);
			Grid.SetColumn(songInfoSp, 1);
			rowGrid.Children.Add(songInfoSp);

			// 2. 来源 / 音质
			var qualitySp = new StackPanel { Spacing = 2, VerticalAlignment = VerticalAlignment.Center };
			qualitySp.Children.Add(new TextBlock
			{
				Text = item.RawInfo.Source,
				FontSize = 11,
				FontWeight = FontWeights.SemiBold,
				Foreground = new SolidColorBrush(Color.FromArgb(255, 71, 85, 105))
			});
			qualitySp.Children.Add(new TextBlock
			{
				Text = item.SourceAndQuality,
				FontSize = 10,
				Foreground = new SolidColorBrush(Color.FromArgb(255, 148, 163, 184)),
				TextTrimming = TextTrimming.CharacterEllipsis
			});
			Grid.SetColumn(qualitySp, 2);
			rowGrid.Children.Add(qualitySp);

			// 3. 时长
			var durTb = new TextBlock
			{
				Text = item.DurationText,
				FontSize = 11,
				Foreground = new SolidColorBrush(Color.FromArgb(255, 100, 116, 139)),
				HorizontalAlignment = HorizontalAlignment.Right,
				VerticalAlignment = VerticalAlignment.Center
			};
			Grid.SetColumn(durTb, 3);
			rowGrid.Children.Add(durTb);

			// 4. 操作按钮组
			var actionsSp = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 4, HorizontalAlignment = HorizontalAlignment.Right, VerticalAlignment = VerticalAlignment.Center };

			var playBtn = new Button
			{
				Width = 28,
				Height = 28,
				Padding = new Thickness(0),
				CornerRadius = new CornerRadius(14),
				Background = new SolidColorBrush(Colors.Transparent),
				BorderThickness = new Thickness(0),
				Content = new FontIcon { Glyph = "\uE768", FontSize = 12, Foreground = new SolidColorBrush(Color.FromArgb(255, 99, 102, 241)) },
				Tag = item
			};
			playBtn.Click += async (s, e) =>
			{
				if (s is Button b && b.Tag is StreamingItemViewModel m)
				{
					await ViewModel.PlayTrackAsync(m);
				}
			};
			actionsSp.Children.Add(playBtn);

			var queueBtn = new Button
			{
				Width = 28,
				Height = 28,
				Padding = new Thickness(0),
				CornerRadius = new CornerRadius(14),
				Background = new SolidColorBrush(Colors.Transparent),
				BorderThickness = new Thickness(0),
				Content = new FontIcon { Glyph = "\uE93C", FontSize = 11, Foreground = new SolidColorBrush(Color.FromArgb(255, 100, 116, 139)) },
				Tag = item
			};
			queueBtn.Click += async (s, e) =>
			{
				if (s is Button b && b.Tag is StreamingItemViewModel m)
				{
					await ViewModel.AddToQueueAsync(m);
				}
			};
			actionsSp.Children.Add(queueBtn);

			var downloadBtn = new Button
			{
				Width = 28,
				Height = 28,
				Padding = new Thickness(0),
				CornerRadius = new CornerRadius(14),
				Background = new SolidColorBrush(Colors.Transparent),
				BorderThickness = new Thickness(0),
				Content = new FontIcon { Glyph = "\uE896", FontSize = 11, Foreground = new SolidColorBrush(Color.FromArgb(255, 100, 116, 139)) },
				Tag = item
			};
			downloadBtn.Click += async (s, e) =>
			{
				if (s is Button b && b.Tag is StreamingItemViewModel m)
				{
					await ViewModel.DownloadTrackAsync(m);
				}
			};
			actionsSp.Children.Add(downloadBtn);

			Grid.SetColumn(actionsSp, 4);
			rowGrid.Children.Add(actionsSp);

			rowCard.Child = rowGrid;

			rowCard.DoubleTapped += async (s, e) =>
			{
				await ViewModel.PlayTrackAsync(item);
			};

			_songListStackPanel.Children.Add(rowCard);
		}
	}
}
