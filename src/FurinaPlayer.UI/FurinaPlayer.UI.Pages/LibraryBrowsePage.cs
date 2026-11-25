using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using FurinaPlayer.Audio;
using FurinaPlayer.Core.Models;
using FurinaPlayer.Core.Services;
using FurinaPlayer.UI.Controls;
using FurinaPlayer.UI.ViewModels;
using Microsoft.UI;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Windows.UI;

namespace FurinaPlayer.UI.Pages;

/// <summary>
/// Unified ECHO-inspired Library Page hosting:
/// - "home": Listening Dashboard (Continue Playing, 4 Stat Cards, Recent Activity, Weekly Heatmap, Recommended, Long-Time-No-Listen, Top Tracks, Artist Ranking)
/// - "albums": Dedicated Albums Grid + Album Drill-Down Tracklist
/// - "songs": Dedicated All Songs Table with tightened Title/Artist-to-Album spacing &amp; Heart Favorite buttons
/// - "favorites": Dedicated "音乐收藏" ("我喜欢的音乐") Playlist Page
/// </summary>
public sealed class LibraryBrowsePage : Page
{
	private string _mode = "home";
	private HomeViewModel? _homeVm;
	private NowPlayingViewModel? _nowPlayingVm;
	private readonly ScrollViewer _rootScroll;
	private readonly StackPanel _rootStack;

	private string _activityTab = "played"; // "played" | "added"
	private string _searchQuery = string.Empty;
	private Album? _selectedAlbum;
	private int _recommendSeed;
	private bool _libraryInitialized;

	private Border? _heroCoverBorder;
	private string? _heroRenderedCoverPath;
	private TextBlock? _heroTitleTb;
	private TextBlock? _heroSubtitleTb;
	private TextBlock? _heroFormatTb;
	private TextBlock? _heroLyricTb;
	private ObservableCollection<LyricLine>? _subscribedLyricLines;

	public event Action<string>? NavigateRequested;

	public LibraryBrowsePage()
	{
		Background = new SolidColorBrush(Colors.Transparent);
		_rootStack = new StackPanel
		{
			Spacing = 18,
			Padding = new Thickness(24, 18, 24, 32),
			MaxWidth = 1320,
			HorizontalAlignment = HorizontalAlignment.Stretch
		};
		var hostGrid = new Grid
		{
			HorizontalAlignment = HorizontalAlignment.Stretch,
			Background = new SolidColorBrush(Colors.Transparent)
		};
		hostGrid.Children.Add(_rootStack);
		_rootScroll = new ScrollViewer
		{
			VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
			HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
			HorizontalScrollMode = ScrollMode.Disabled,
			HorizontalContentAlignment = HorizontalAlignment.Stretch,
			Background = new SolidColorBrush(Colors.Transparent),
			Content = hostGrid
		};
		Content = _rootScroll;
		Loaded += (_, _) =>
		{
			if (_mode == "home")
			{
				UpdateHeroNowPlayingCard();
			}
		};
	}

	public void BindAndRender(string mode, HomeViewModel homeVm, NowPlayingViewModel? nowPlayingVm)
	{
		_mode = string.IsNullOrWhiteSpace(mode) ? "home" : mode.Trim().ToLowerInvariant();
		if (!ReferenceEquals(_homeVm, homeVm))
		{
			if (_homeVm != null)
			{
				_homeVm.PropertyChanged -= OnHomeVmPropertyChanged;
			}
			_homeVm = homeVm;
			_homeVm.PropertyChanged += OnHomeVmPropertyChanged;
			_libraryInitialized = false;
		}
		if (!ReferenceEquals(_nowPlayingVm, nowPlayingVm))
		{
			if (_nowPlayingVm != null)
			{
				_nowPlayingVm.PropertyChanged -= OnNowPlayingVmPropertyChanged;
			}
			if (_subscribedLyricLines != null)
			{
				_subscribedLyricLines.CollectionChanged -= OnLyricLinesCollectionChanged;
				_subscribedLyricLines = null;
			}
			_nowPlayingVm = nowPlayingVm;
			if (_nowPlayingVm != null)
			{
				_nowPlayingVm.PropertyChanged += OnNowPlayingVmPropertyChanged;
				AttachLyricLinesSubscription(_nowPlayingVm.LyricLines);
			}
		}
		else if (_nowPlayingVm != null && !ReferenceEquals(_subscribedLyricLines, _nowPlayingVm.LyricLines))
		{
			AttachLyricLinesSubscription(_nowPlayingVm.LyricLines);
		}
		Render();

		if (!_libraryInitialized)
		{
			_libraryInitialized = true;
			_ = EnsureLibraryLoadedAsync(homeVm);
		}
	}

	private void AttachLyricLinesSubscription(ObservableCollection<LyricLine>? lines)
	{
		if (ReferenceEquals(_subscribedLyricLines, lines)) return;
		if (_subscribedLyricLines != null)
		{
			_subscribedLyricLines.CollectionChanged -= OnLyricLinesCollectionChanged;
		}
		_subscribedLyricLines = lines;
		if (_subscribedLyricLines != null)
		{
			_subscribedLyricLines.CollectionChanged += OnLyricLinesCollectionChanged;
		}
	}

	private void OnNowPlayingVmPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
	{
		if (e.PropertyName == nameof(NowPlayingViewModel.LyricLines))
		{
			AttachLyricLinesSubscription(_nowPlayingVm?.LyricLines);
		}
		if (_mode == "home" && e.PropertyName is nameof(NowPlayingViewModel.CurrentTrack)
			or nameof(NowPlayingViewModel.CoverPath)
			or nameof(NowPlayingViewModel.Title)
			or nameof(NowPlayingViewModel.Artist)
			or nameof(NowPlayingViewModel.Album)
			or nameof(NowPlayingViewModel.CurrentLyricIndex)
			or nameof(NowPlayingViewModel.LyricLines)
			or nameof(NowPlayingViewModel.IsPlaying))
		{
			DispatcherQueue?.TryEnqueue(UpdateHeroNowPlayingCard);
		}
	}

	private void OnLyricLinesCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
	{
		if (_mode == "home")
		{
			DispatcherQueue?.TryEnqueue(UpdateHeroNowPlayingCard);
		}
	}

	private void OnHomeVmPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
	{
		if (e.PropertyName == nameof(HomeViewModel.Tracks) || e.PropertyName == nameof(HomeViewModel.Albums))
		{
			DispatcherQueue?.TryEnqueue(Render);
		}
	}

	private async System.Threading.Tasks.Task EnsureLibraryLoadedAsync(HomeViewModel vm)
	{
		try
		{
			await vm.InitializeAsync();
			DispatcherQueue?.TryEnqueue(Render);
		}
		catch
		{
		}
	}

	public void RefreshView()
	{
		Render();
	}

	private bool IsDark => LiquidGlassEngine.IsElementDarkTheme(this);

	private void UpdateHeroNowPlayingCard()
	{
		if (_mode != "home" || _heroTitleTb == null) return;

		Track? heroTrack = _nowPlayingVm?.CurrentTrack
			?? AudioEngine.Instance?.CurrentTrack
			?? _homeVm?.Tracks?.OrderByDescending(t => t.LastPlayedAt).FirstOrDefault();

		// 1. Cover Art (update only when path actually changes to prevent flicker)
		if (_heroCoverBorder != null)
		{
			string? rawCover = !string.IsNullOrWhiteSpace(_nowPlayingVm?.CoverPath)
				? _nowPlayingVm.CoverPath
				: heroTrack?.CoverPath;
			string normalizedCover = rawCover?.Trim() ?? string.Empty;
			if (!string.Equals(_heroRenderedCoverPath, normalizedCover, StringComparison.OrdinalIgnoreCase))
			{
				_heroRenderedCoverPath = normalizedCover;
				Uri? coverUri = null;
				if (!string.IsNullOrEmpty(normalizedCover))
				{
					if (Uri.TryCreate(normalizedCover, UriKind.Absolute, out Uri? parsedUri) &&
					    (parsedUri.Scheme == Uri.UriSchemeHttp || parsedUri.Scheme == Uri.UriSchemeHttps))
					{
						coverUri = parsedUri;
					}
					else if (System.IO.File.Exists(normalizedCover))
					{
						coverUri = new Uri(normalizedCover);
					}
				}

				if (coverUri != null)
				{
					_heroCoverBorder.Child = new Image
					{
						Stretch = Stretch.UniformToFill,
						Source = new BitmapImage
						{
							DecodePixelType = DecodePixelType.Logical,
							DecodePixelWidth = 304,
							UriSource = coverUri
						}
					};
				}
				else
				{
					_heroCoverBorder.Child = FluentIconFactory.Create(FluentIconKind.MusicHero, 56);
				}
			}
		}

		// 2. Title & Artist/Album
		string titleText = (_nowPlayingVm?.CurrentTrack != null && !string.IsNullOrWhiteSpace(_nowPlayingVm.Title))
			? _nowPlayingVm.Title
			: (heroTrack?.Title ?? Loc.T("暂无正在播放曲目"));
		_heroTitleTb.Text = titleText;

		if (_heroSubtitleTb != null)
		{
			if (heroTrack != null)
			{
				string artistText = (_nowPlayingVm?.CurrentTrack != null && !string.IsNullOrWhiteSpace(_nowPlayingVm.Artist))
					? _nowPlayingVm.Artist
					: (string.IsNullOrWhiteSpace(heroTrack.Artist) ? Loc.T("未知艺人") : heroTrack.Artist);
				string albumText = (_nowPlayingVm?.CurrentTrack != null && !string.IsNullOrWhiteSpace(_nowPlayingVm.Album))
					? _nowPlayingVm.Album
					: (string.IsNullOrWhiteSpace(heroTrack.Album) ? Loc.T("未知专辑") : heroTrack.Album);
				_heroSubtitleTb.Text = $"{artistText} · {albumText}";
			}
			else
			{
				_heroSubtitleTb.Text = Loc.T("请从下方曲库或推荐专辑选择一首歌曲开始聆听");
			}
		}

		// 3. Audio Format Info
		if (_heroFormatTb != null)
		{
			if (heroTrack != null)
			{
				string fmt = heroTrack.Format.ToString().ToUpperInvariant();
				string sr = heroTrack.SampleRate > 0 ? $"{heroTrack.SampleRate / 1000.0:0.#} kHz" : "44.1 kHz";
				string bits = heroTrack.BitDepth > 0 ? $"{heroTrack.BitDepth} bit" : "16 bit";
				_heroFormatTb.Text = $"{fmt} · {bits} · {sr} · 2 ch";
				_heroFormatTb.Visibility = Visibility.Visible;
			}
			else
			{
				_heroFormatTb.Text = string.Empty;
				_heroFormatTb.Visibility = Visibility.Collapsed;
			}
		}

		// 4. Live Lyric Preview
		if (_heroLyricTb != null)
		{
			string? curLyric = (_nowPlayingVm != null &&
				_nowPlayingVm.CurrentLyricIndex >= 0 &&
				_nowPlayingVm.CurrentLyricIndex < _nowPlayingVm.LyricLines.Count)
				? _nowPlayingVm.LyricLines[_nowPlayingVm.CurrentLyricIndex].Text
				: null;
			if (!string.IsNullOrWhiteSpace(curLyric))
			{
				_heroLyricTb.Text = curLyric;
				_heroLyricTb.Visibility = Visibility.Visible;
			}
			else
			{
				_heroLyricTb.Text = string.Empty;
				_heroLyricTb.Visibility = Visibility.Collapsed;
			}
		}
	}

	private void Render()
	{
		_heroCoverBorder = null;
		_heroRenderedCoverPath = null;
		_heroTitleTb = null;
		_heroSubtitleTb = null;
		_heroFormatTb = null;
		_heroLyricTb = null;
		_rootStack.Children.Clear();
		_rootScroll.ChangeView(0, 0, null, true);
		if (_homeVm == null) return;

		switch (_mode)
		{
			case "albums":
				RenderAlbumsView();
				break;
			case "songs":
				RenderSongsView();
				break;
			case "favorites":
				RenderFavoritesView();
				break;
			default:
				RenderEchoDashboardView();
				break;
		}
	}

	// ════════════════════════════════════════════════════════════════════════
	// 1. ECHO-INSPIRED HOME DASHBOARD
	// ════════════════════════════════════════════════════════════════════════
	private void RenderEchoDashboardView()
	{
		var vm = _homeVm!;
		var tracks = vm.Tracks?.ToList() ?? new List<Track>();
		var albums = vm.Albums?.ToList() ?? new List<Album>();
		var engine = AudioEngine.Instance;
		Track? heroTrack = _nowPlayingVm?.CurrentTrack ?? engine?.CurrentTrack ?? tracks.OrderByDescending(t => t.LastPlayedAt).FirstOrDefault();

		// ── Hero Now Playing / Continue Card ──
		var heroCard = CreateGlassCard(new Thickness(22, 20, 22, 20));
		var heroGrid = new Grid { ColumnSpacing = 22 };
		heroGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
		heroGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

		_heroCoverBorder = new Border
		{
			Width = 152,
			Height = 152,
			CornerRadius = new CornerRadius(14),
			Background = new SolidColorBrush(IsDark ? Color.FromArgb(40, 255, 255, 255) : Color.FromArgb(40, 148, 163, 184))
		};
		Grid.SetColumn(_heroCoverBorder, 0);
		heroGrid.Children.Add(_heroCoverBorder);

		var heroRight = new StackPanel { Spacing = 8, VerticalAlignment = VerticalAlignment.Center };
		_heroTitleTb = new TextBlock
		{
			FontSize = 26,
			FontWeight = FontWeights.Bold,
			TextTrimming = TextTrimming.CharacterEllipsis
		};
		heroRight.Children.Add(_heroTitleTb);
		_heroSubtitleTb = new TextBlock
		{
			FontSize = 14,
			Opacity = 0.78,
			TextTrimming = TextTrimming.CharacterEllipsis
		};
		heroRight.Children.Add(_heroSubtitleTb);

		var actionBtns = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8, Margin = new Thickness(0, 4, 0, 2) };
		var btnContinue = CreatePillButton(Loc.T("▶ 继续播放"), isAccent: true);
		btnContinue.Click += (_, _) =>
		{
			Track? currentHeroTrack = _nowPlayingVm?.CurrentTrack ?? AudioEngine.Instance?.CurrentTrack ?? tracks.OrderByDescending(t => t.LastPlayedAt).FirstOrDefault();
			if (engine?.CurrentTrack != null) engine.Play();
			else if (currentHeroTrack != null) vm.PlayTrackCommand.Execute(currentHeroTrack);
			else NavigateRequested?.Invoke("songs");
		};
		actionBtns.Children.Add(btnContinue);

		var btnQueue = CreatePillButton(Loc.T("查看队列"));
		btnQueue.Click += (_, _) => NavigateRequested?.Invoke("playing");
		actionBtns.Children.Add(btnQueue);

		var btnRefresh = CreatePillButton(Loc.T("刷新"));
		btnRefresh.Click += async (_, _) =>
		{
			await vm.RefreshCommand.ExecuteAsync(null);
			Render();
		};
		actionBtns.Children.Add(btnRefresh);

		var btnShuffle = CreatePillButton(Loc.T("生成随机队列"));
		btnShuffle.Click += (_, _) =>
		{
			if (tracks.Count == 0) return;
			var rng = new Random();
			var shuffled = tracks.OrderBy(_ => rng.Next()).ToList();
			engine?.SetQueue(shuffled, 0);
			vm.PlayTrackCommand.Execute(shuffled[0]);
		};
		actionBtns.Children.Add(btnShuffle);
		heroRight.Children.Add(actionBtns);

		_heroFormatTb = new TextBlock
		{
			FontSize = 12,
			Opacity = 0.65,
			Visibility = Visibility.Collapsed
		};
		heroRight.Children.Add(_heroFormatTb);

		// Live lyric preview from NowPlayingViewModel
		_heroLyricTb = new TextBlock
		{
			FontSize = 13.5,
			FontWeight = FontWeights.SemiBold,
			Foreground = new SolidColorBrush(Color.FromArgb(255, 56, 189, 248)),
			TextWrapping = TextWrapping.Wrap,
			MaxLines = 2,
			TextTrimming = TextTrimming.CharacterEllipsis,
			Visibility = Visibility.Collapsed
		};
		heroRight.Children.Add(_heroLyricTb);

		Grid.SetColumn(heroRight, 1);
		heroGrid.Children.Add(heroRight);
		heroCard.Child = heroGrid;
		_rootStack.Children.Add(heroCard);
		UpdateHeroNowPlayingCard();

		// ── 4 Library Stat Cards (Artists, Albums, Tracks, Folders) ──
		int artistCount = tracks.Select(t => t.Artist).Where(a => !string.IsNullOrWhiteSpace(a)).Distinct(StringComparer.OrdinalIgnoreCase).Count();
		int folderCount = tracks.Select(t => System.IO.Path.GetDirectoryName(t.FilePath) ?? "").Where(p => !string.IsNullOrEmpty(p)).Distinct(StringComparer.OrdinalIgnoreCase).Count();

		var statsGrid = new Grid { ColumnSpacing = 12 };
		for (int c = 0; c < 4; c++) statsGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

		AddStatCard(statsGrid, 0, Loc.T("艺术家"), artistCount.ToString(), FluentIconKind.MusicHero, () => NavigateRequested?.Invoke("songs"));
		AddStatCard(statsGrid, 1, Loc.T("专辑"), albums.Count.ToString(), FluentIconKind.AlbumGrid, () => NavigateRequested?.Invoke("albums"));
		AddStatCard(statsGrid, 2, Loc.T("曲目"), tracks.Count.ToString(), FluentIconKind.NowPlaying, () => NavigateRequested?.Invoke("songs"));
		AddStatCard(statsGrid, 3, Loc.T("文件夹"), Math.Max(1, folderCount).ToString(), FluentIconKind.FolderScan, () => NavigateRequested?.Invoke("settings"));
		_rootStack.Children.Add(statsGrid);

		// ── Row 3: 最近活动 (左) + 本周聆听热力图 (右) ──
		var actRow = new Grid { ColumnSpacing = 16 };
		actRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(2.1, GridUnitType.Star) });
		actRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.0, GridUnitType.Star) });

		var recentCard = CreateGlassCard(new Thickness(18, 16, 18, 16));
		var recentStack = new StackPanel { Spacing = 12 };
		var recentHdr = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 12 };
		recentHdr.Children.Add(new TextBlock { Text = Loc.T("最近活动"), FontSize = 17, FontWeight = FontWeights.SemiBold, VerticalAlignment = VerticalAlignment.Center });

		var btnPlayed = CreatePillButton(Loc.T("已播放"), isAccent: _activityTab == "played");
		var btnAdded = CreatePillButton(Loc.T("添加于"), isAccent: _activityTab == "added");
		btnPlayed.Click += (_, _) => { _activityTab = "played"; Render(); };
		btnAdded.Click += (_, _) => { _activityTab = "added"; Render(); };
		recentHdr.Children.Add(btnPlayed);
		recentHdr.Children.Add(btnAdded);
		recentStack.Children.Add(recentHdr);

		var actTracks = _activityTab == "played"
			? tracks.Where(t => t.PlayCount > 0 || t.LastPlayedAt > DateTime.MinValue).OrderByDescending(t => t.LastPlayedAt).Take(4).ToList()
			: tracks.OrderByDescending(t => t.AddedAt).Take(4).ToList();
		if (actTracks.Count == 0) actTracks = tracks.Take(4).ToList();

		var actCardsGrid = new Grid { ColumnSpacing = 12 };
		for (int c = 0; c < 4; c++) actCardsGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
		for (int i = 0; i < actTracks.Count && i < 4; i++)
		{
			AddTrackSquareTile(actCardsGrid, i, actTracks[i], vm);
		}
		recentStack.Children.Add(actCardsGrid);
		recentCard.Child = recentStack;
		Grid.SetColumn(recentCard, 0);
		actRow.Children.Add(recentCard);

		// Weekly Stats & 4-Week Heatmap
		var weekCard = CreateGlassCard(new Thickness(18, 16, 18, 16));
		var weekStack = new StackPanel { Spacing = 10 };
		weekStack.Children.Add(new TextBlock { Text = Loc.T("本周"), FontSize = 17, FontWeight = FontWeights.SemiBold });

		long weekPlays = tracks.Sum(t => t.PlayCount);
		double weekMinutes = tracks.Sum(t => t.PlayCount * (t.DurationMilliseconds / 60000.0));
		var weekNums = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 22 };
		weekNums.Children.Add(MakeStatPair($"{weekPlays}", Loc.T("累计播放")));
		weekNums.Children.Add(MakeStatPair($"{Math.Round(weekMinutes)} {Loc.T("分钟")}", Loc.T("聆听时长")));
		weekStack.Children.Add(weekNums);

		// 4-week x 7-day heatmap
		var heatGrid = new Grid { RowSpacing = 4, ColumnSpacing = 4, Margin = new Thickness(0, 4, 0, 0) };
		for (int c = 0; c < 10; c++) heatGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
		for (int r = 0; r < 5; r++) heatGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
		var now = DateTime.Now.Date;
		var dayPlayMap = tracks.Where(t => t.LastPlayedAt > DateTime.MinValue)
			.GroupBy(t => t.LastPlayedAt.Date)
			.ToDictionary(g => g.Key, g => g.Sum(x => x.PlayCount));
		for (int c = 0; c < 10; c++)
		{
			for (int r = 0; r < 5; r++)
			{
				int daysAgo = (9 - c) * 5 + (4 - r);
				var d = now.AddDays(-daysAgo);
				long cnt = dayPlayMap.TryGetValue(d, out var v) ? v : 0;
				byte alpha = cnt == 0 ? (byte)(IsDark ? 28 : 22) : (byte)Math.Min(245, 80 + cnt * 35);
				Color cellCol = cnt == 0
					? (IsDark ? Color.FromArgb(alpha, 255, 255, 255) : Color.FromArgb(alpha, 100, 116, 139))
					: Color.FromArgb(alpha, 99, 102, 241);
				var dot = new Border
				{
					Width = 14,
					Height = 14,
					CornerRadius = new CornerRadius(3),
					Background = new SolidColorBrush(cellCol)
				};
				Grid.SetColumn(dot, c);
				Grid.SetRow(dot, r);
				heatGrid.Children.Add(dot);
			}
		}
		weekStack.Children.Add(heatGrid);
		weekStack.Children.Add(new TextBlock { Text = Loc.T("近 50 天聆听活跃分布"), FontSize = 11.5, Opacity = 0.65 });
		weekCard.Child = weekStack;
		Grid.SetColumn(weekCard, 1);
		actRow.Children.Add(weekCard);
		_rootStack.Children.Add(actRow);

		// ── Row 4: 为你推荐 + 好久没听 (左) & 艺人排行榜 (右) ──
		var bottomGrid = new Grid { ColumnSpacing = 16 };
		bottomGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(2.1, GridUnitType.Star) });
		bottomGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.0, GridUnitType.Star) });

		var leftCol = new StackPanel { Spacing = 16 };

		// 为你推荐
		var recCard = CreateGlassCard(new Thickness(18, 16, 18, 16));
		var recStack = new StackPanel { Spacing = 12 };
		var recHdr = new Grid();
		recHdr.Children.Add(new TextBlock { Text = Loc.T("为你推荐"), FontSize = 17, FontWeight = FontWeights.SemiBold, VerticalAlignment = VerticalAlignment.Center });
		var recRefreshBtn = CreatePillButton(Loc.T("换一批"));
		recRefreshBtn.HorizontalAlignment = HorizontalAlignment.Right;
		recRefreshBtn.Click += (_, _) => { _recommendSeed++; Render(); };
		recHdr.Children.Add(recRefreshBtn);
		recStack.Children.Add(recHdr);

		var recRng = new Random(1009 + _recommendSeed * 37);
		var recAlbums = albums.OrderBy(_ => recRng.Next()).Take(4).ToList();
		var recGrid = new Grid { ColumnSpacing = 12 };
		for (int c = 0; c < 4; c++) recGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
		for (int i = 0; i < recAlbums.Count; i++)
		{
			AddAlbumSquareTile(recGrid, i, recAlbums[i], vm);
		}
		recStack.Children.Add(recGrid);
		recCard.Child = recStack;
		leftCol.Children.Add(recCard);

		// 好久没听
		var oldCard = CreateGlassCard(new Thickness(18, 16, 18, 16));
		var oldStack = new StackPanel { Spacing = 12 };
		oldStack.Children.Add(new TextBlock { Text = Loc.T("好久没听"), FontSize = 17, FontWeight = FontWeights.SemiBold });
		var oldTracks = tracks.OrderBy(t => t.LastPlayedAt).Take(4).ToList();
		var oldGrid = new Grid { ColumnSpacing = 12 };
		for (int c = 0; c < 4; c++) oldGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
		for (int i = 0; i < oldTracks.Count; i++)
		{
			AddTrackSquareTile(oldGrid, i, oldTracks[i], vm);
		}
		oldStack.Children.Add(oldGrid);
		oldCard.Child = oldStack;
		leftCol.Children.Add(oldCard);

		// 常听单曲 (2 columns)
		var topTracksCard = CreateGlassCard(new Thickness(18, 16, 18, 16));
		var topStack = new StackPanel { Spacing = 10 };
		topStack.Children.Add(new TextBlock { Text = Loc.T("常听单曲"), FontSize = 17, FontWeight = FontWeights.SemiBold });
		var topTracks = tracks.OrderByDescending(t => t.PlayCount).ThenByDescending(t => t.LastPlayedAt).Take(6).ToList();
		var topGrid = new Grid { ColumnSpacing = 16, RowSpacing = 10 };
		topGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
		topGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
		for (int r = 0; r < 3; r++) topGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
		for (int i = 0; i < topTracks.Count; i++)
		{
			AddRankedTrackTile(topGrid, i / 2, i % 2, i + 1, topTracks[i], vm);
		}
		topStack.Children.Add(topGrid);
		topTracksCard.Child = topStack;
		leftCol.Children.Add(topTracksCard);

		Grid.SetColumn(leftCol, 0);
		bottomGrid.Children.Add(leftCol);

		// 艺人排行榜
		var artistCard = CreateGlassCard(new Thickness(18, 16, 18, 16));
		var artistStack = new StackPanel { Spacing = 12 };
		artistStack.Children.Add(new TextBlock { Text = Loc.T("艺人排行榜"), FontSize = 17, FontWeight = FontWeights.SemiBold });
		var topArtists = tracks
			.GroupBy(t => string.IsNullOrWhiteSpace(t.Artist) ? Loc.T("未知艺人") : t.Artist)
			.Select(g => new
			{
				Artist = g.Key,
				Plays = g.Sum(x => Math.Max(1, x.PlayCount)),
				Minutes = g.Sum(x => Math.Max(1, x.PlayCount) * (x.DurationMilliseconds / 60000.0))
			})
			.OrderByDescending(x => x.Plays)
			.Take(8)
			.ToList();
		long maxArtistPlays = Math.Max(1, topArtists.FirstOrDefault()?.Plays ?? 1);
		for (int i = 0; i < topArtists.Count; i++)
		{
			var a = topArtists[i];
			var row = new Grid { ColumnSpacing = 10 };
			row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(28) });
			row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
			row.Children.Add(new TextBlock { Text = $"{i + 1:D2}", FontSize = 13, FontWeight = FontWeights.SemiBold, Opacity = 0.6, VerticalAlignment = VerticalAlignment.Center });

			var info = new StackPanel { Spacing = 3 };
			info.Children.Add(new TextBlock { Text = a.Artist, FontSize = 13.5, FontWeight = FontWeights.SemiBold, TextTrimming = TextTrimming.CharacterEllipsis });
			info.Children.Add(new TextBlock { Text = Loc.F("{0} 次 · {1} 分钟", a.Plays, Math.Round(a.Minutes)), FontSize = 11.5, Opacity = 0.68 });
			info.Children.Add(new ProgressBar { Minimum = 0, Maximum = maxArtistPlays, Value = a.Plays, Height = 3, CornerRadius = new CornerRadius(2) });
			Grid.SetColumn(info, 1);
			row.Children.Add(info);
			artistStack.Children.Add(row);
		}
		artistCard.Child = artistStack;
		Grid.SetColumn(artistCard, 1);
		bottomGrid.Children.Add(artistCard);

		_rootStack.Children.Add(bottomGrid);
	}

	// ════════════════════════════════════════════════════════════════════════
	// 2. DEDICATED ALBUMS PAGE ("专辑")
	// ════════════════════════════════════════════════════════════════════════
	private void RenderAlbumsView()
	{
		var vm = _homeVm!;
		var albums = vm.Albums?.ToList() ?? new List<Album>();
		if (!string.IsNullOrWhiteSpace(_searchQuery))
		{
			albums = albums.Where(a =>
				a.Name.Contains(_searchQuery, StringComparison.OrdinalIgnoreCase) ||
				a.Artist.Contains(_searchQuery, StringComparison.OrdinalIgnoreCase)).ToList();
		}

		// Header + Search
		var hdr = new Grid { ColumnSpacing = 12 };
		hdr.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
		hdr.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

		var titleSp = new StackPanel { Spacing = 2 };
		titleSp.Children.Add(new TextBlock { Text = Loc.T("专辑"), FontSize = 24, FontWeight = FontWeights.Bold });
		titleSp.Children.Add(new TextBlock { Text = Loc.F("共 {0} 张专辑", albums.Count), FontSize = 13, Opacity = 0.72 });
		hdr.Children.Add(titleSp);

		var searchBox = new TextBox
		{
			Width = 240,
			PlaceholderText = Loc.T("搜索专辑或艺术家..."),
			Text = _searchQuery,
			CornerRadius = new CornerRadius(10)
		};
		searchBox.TextChanged += (_, _) => { _searchQuery = searchBox.Text; Render(); };
		Grid.SetColumn(searchBox, 1);
		hdr.Children.Add(searchBox);
		_rootStack.Children.Add(hdr);

		// If an album is selected, show album detail header + tracklist
		if (_selectedAlbum != null)
		{
			var detailCard = CreateGlassCard(new Thickness(18, 16, 18, 16));
			var dStack = new StackPanel { Spacing = 12 };
			var backBtn = CreatePillButton(Loc.T("← 返回全部专辑"));
			backBtn.Click += (_, _) => { _selectedAlbum = null; Render(); };
			dStack.Children.Add(backBtn);

			dStack.Children.Add(new TextBlock { Text = _selectedAlbum.Name, FontSize = 20, FontWeight = FontWeights.Bold });
			dStack.Children.Add(new TextBlock { Text = $"{_selectedAlbum.Artist} · {_selectedAlbum.Tracks.Count} {Loc.T("首")}", FontSize = 13, Opacity = 0.75 });

			var playAllBtn = CreatePillButton(Loc.T("▶ 播放整张专辑"), isAccent: true);
			playAllBtn.Click += (_, _) =>
			{
				if (_selectedAlbum.Tracks.Count > 0)
				{
					AudioEngine.Instance?.SetQueue(_selectedAlbum.Tracks, 0);
					vm.PlayTrackCommand.Execute(_selectedAlbum.Tracks[0]);
				}
			};
			dStack.Children.Add(playAllBtn);

			foreach (var t in _selectedAlbum.Tracks)
			{
				dStack.Children.Add(CreateSongRow(t, vm, showAlbumCol: false));
			}
			detailCard.Child = dStack;
			_rootStack.Children.Add(detailCard);
			return;
		}

		// Responsive 4-column Album Grid
		var grid = new Grid { ColumnSpacing = 14, RowSpacing = 14 };
		const int cols = 4;
		for (int c = 0; c < cols; c++) grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
		int rows = (int)Math.Ceiling(albums.Count / (double)cols);
		for (int r = 0; r < rows; r++) grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

		for (int i = 0; i < albums.Count; i++)
		{
			var alb = albums[i];
			var card = CreateGlassCard(new Thickness(12));
			var st = new StackPanel { Spacing = 8 };
			var imgBox = new Border
			{
				Height = 175,
				CornerRadius = new CornerRadius(10),
				Background = new SolidColorBrush(IsDark ? Color.FromArgb(30, 255, 255, 255) : Color.FromArgb(30, 148, 163, 184))
			};
			if (!string.IsNullOrEmpty(alb.CoverPath) && System.IO.File.Exists(alb.CoverPath))
			{
				imgBox.Child = new Image
				{
					Stretch = Stretch.UniformToFill,
					Source = new BitmapImage { DecodePixelType = DecodePixelType.Logical, DecodePixelWidth = 300, UriSource = new Uri(alb.CoverPath) }
				};
			}
			else
			{
				imgBox.Child = FluentIconFactory.Create(FluentIconKind.AlbumGrid, 42);
			}
			st.Children.Add(imgBox);
			st.Children.Add(new TextBlock { Text = alb.Name, FontSize = 13.5, FontWeight = FontWeights.SemiBold, TextTrimming = TextTrimming.CharacterEllipsis });
			st.Children.Add(new TextBlock { Text = $"{alb.Artist} · {alb.Tracks.Count} {Loc.T("首")}", FontSize = 11.5, Opacity = 0.72, TextTrimming = TextTrimming.CharacterEllipsis });

			var btn = new Button
			{
				Content = st,
				HorizontalAlignment = HorizontalAlignment.Stretch,
				HorizontalContentAlignment = HorizontalAlignment.Stretch,
				Padding = new Thickness(0),
				Background = new SolidColorBrush(Colors.Transparent),
				BorderThickness = new Thickness(0)
			};
			btn.Click += (_, _) => { _selectedAlbum = alb; Render(); };
			card.Child = btn;
			Grid.SetRow(card, i / cols);
			Grid.SetColumn(card, i % cols);
			grid.Children.Add(card);
		}
		_rootStack.Children.Add(grid);
	}

	// ════════════════════════════════════════════════════════════════════════
	// 3. DEDICATED SONGS PAGE ("歌曲") — Tight spacing & Heart Favorite Button
	// ════════════════════════════════════════════════════════════════════════
	private void RenderSongsView()
	{
		var vm = _homeVm!;
		var tracks = vm.Tracks?.ToList() ?? new List<Track>();
		if (!string.IsNullOrWhiteSpace(_searchQuery))
		{
			tracks = tracks.Where(t =>
				t.Title.Contains(_searchQuery, StringComparison.OrdinalIgnoreCase) ||
				t.Artist.Contains(_searchQuery, StringComparison.OrdinalIgnoreCase) ||
				t.Album.Contains(_searchQuery, StringComparison.OrdinalIgnoreCase)).ToList();
		}

		var hdr = new Grid { ColumnSpacing = 12 };
		hdr.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
		hdr.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
		hdr.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

		var titleSp = new StackPanel { Spacing = 2 };
		titleSp.Children.Add(new TextBlock { Text = Loc.T("全部歌曲"), FontSize = 24, FontWeight = FontWeights.Bold });
		titleSp.Children.Add(new TextBlock { Text = Loc.F("共 {0} 首歌曲 · {1} 张专辑", tracks.Count, vm.Albums?.Count ?? 0), FontSize = 13, Opacity = 0.72 });
		hdr.Children.Add(titleSp);

		var playAllBtn = CreatePillButton(Loc.T("▶ 播放全部"), isAccent: true);
		playAllBtn.Click += (_, _) =>
		{
			if (tracks.Count > 0)
			{
				AudioEngine.Instance?.SetQueue(tracks, 0);
				vm.PlayTrackCommand.Execute(tracks[0]);
			}
		};
		Grid.SetColumn(playAllBtn, 1);
		hdr.Children.Add(playAllBtn);

		var searchBox = new TextBox
		{
			Width = 230,
			PlaceholderText = Loc.T("搜索歌名、歌手或专辑..."),
			Text = _searchQuery,
			CornerRadius = new CornerRadius(10)
		};
		searchBox.TextChanged += (_, _) => { _searchQuery = searchBox.Text; Render(); };
		Grid.SetColumn(searchBox, 2);
		hdr.Children.Add(searchBox);
		_rootStack.Children.Add(hdr);

		var tableCard = CreateGlassCard(new Thickness(12, 10, 12, 12));
		var listStack = new StackPanel { Spacing = 6 };
		listStack.Children.Add(CreateSongHeaderRow());
		foreach (var t in tracks)
		{
			listStack.Children.Add(CreateSongRow(t, vm, showAlbumCol: true));
		}
		tableCard.Child = listStack;
		_rootStack.Children.Add(tableCard);
	}

	// ════════════════════════════════════════════════════════════════════════
	// 4. FAVORITES PAGE ("音乐收藏" — "我喜欢的音乐")
	// ════════════════════════════════════════════════════════════════════════
	private void RenderFavoritesView()
	{
		var vm = _homeVm!;
		var settingsSvc = new SettingsService();
		var savedOrder = settingsSvc.Load().FavoriteTrackOrder ?? new List<string>();
		var orderMap = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
		for (int i = 0; i < savedOrder.Count; i++)
		{
			if (!string.IsNullOrWhiteSpace(savedOrder[i]) && !orderMap.ContainsKey(savedOrder[i]))
			{
				orderMap[savedOrder[i]] = i;
			}
		}
		var favs = (vm.Tracks ?? Enumerable.Empty<Track>())
			.Where(t => t.IsFavorite)
			.OrderBy(t => orderMap.TryGetValue(t.FilePath, out int idx) ? idx : int.MaxValue)
			.ToList();

		var heroCard = CreateGlassCard(new Thickness(22, 20, 22, 20));
		heroCard.HorizontalAlignment = HorizontalAlignment.Stretch;
		var hGrid = new Grid { ColumnSpacing = 20, HorizontalAlignment = HorizontalAlignment.Stretch };
		hGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
		hGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

		var heartBadge = new Border
		{
			Width = 112,
			Height = 112,
			CornerRadius = new CornerRadius(18),
			Background = new SolidColorBrush(Color.FromArgb(220, 225, 29, 72)),
			Child = new TextBlock
			{
				Text = "♥",
				FontSize = 54,
				Foreground = new SolidColorBrush(Colors.White),
				HorizontalAlignment = HorizontalAlignment.Center,
				VerticalAlignment = VerticalAlignment.Center
			}
		};
		hGrid.Children.Add(heartBadge);

		var hRight = new StackPanel { Spacing = 8, VerticalAlignment = VerticalAlignment.Center };
		hRight.Children.Add(new TextBlock { Text = Loc.T("我喜欢的音乐"), FontSize = 24, FontWeight = FontWeights.Bold });
		hRight.Children.Add(new TextBlock { Text = Loc.F("音乐收藏歌单 · 已收藏 {0} 首歌曲", favs.Count), FontSize = 13.5, Opacity = 0.78 });

		var btns = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 10, Margin = new Thickness(0, 4, 0, 0) };
		var btnPlay = CreatePillButton(Loc.T("▶ 播放收藏全部"), isAccent: true);
		btnPlay.Click += (_, _) =>
		{
			if (favs.Count > 0)
			{
				AudioEngine.Instance?.SetQueue(favs, 0);
				vm.PlayTrackCommand.Execute(favs[0]);
			}
		};
		btns.Children.Add(btnPlay);

		var btnShuffle = CreatePillButton(Loc.T("🎲 随机播放收藏"));
		btnShuffle.Click += (_, _) =>
		{
			if (favs.Count > 0)
			{
				var rng = new Random();
				var shuf = favs.OrderBy(_ => rng.Next()).ToList();
				AudioEngine.Instance?.SetQueue(shuf, 0);
				vm.PlayTrackCommand.Execute(shuf[0]);
			}
		};
		btns.Children.Add(btnShuffle);
		hRight.Children.Add(btns);

		Grid.SetColumn(hRight, 1);
		hGrid.Children.Add(hRight);
		heroCard.Child = hGrid;
		_rootStack.Children.Add(heroCard);

		var listCard = CreateGlassCard(new Thickness(12, 10, 12, 12));
		listCard.HorizontalAlignment = HorizontalAlignment.Stretch;
		var listStack = new StackPanel { Spacing = 6, HorizontalAlignment = HorizontalAlignment.Stretch };
		if (favs.Count == 0)
		{
			listStack.Children.Add(new TextBlock
			{
				Text = Loc.T("暂无“我喜欢的音乐”收藏：请在“歌曲”页点击歌曲后的心形图标即可加入此歌单。"),
				FontSize = 13.5,
				Opacity = 0.72,
				Margin = new Thickness(8, 18, 8, 18),
				HorizontalAlignment = HorizontalAlignment.Center
			});
		}
		else
		{
			listStack.Children.Add(new TextBlock
			{
				Text = Loc.T("提示：按住鼠标拖动歌曲行可自定义歌单排序"),
				FontSize = 11.5,
				Opacity = 0.62,
				Margin = new Thickness(10, 0, 10, 2)
			});
			listStack.Children.Add(CreateSongHeaderRow());

			var reorderObs = new System.Collections.ObjectModel.ObservableCollection<Border>();
			foreach (var t in favs)
			{
				var row = CreateSongRow(t, vm, showAlbumCol: true);
				row.Tag = t;
				row.Margin = new Thickness(0, 2, 0, 2);
				reorderObs.Add(row);
			}

			var favListView = new ListView
			{
				ItemsSource = reorderObs,
				CanReorderItems = true,
				CanDragItems = true,
				AllowDrop = true,
				SelectionMode = ListViewSelectionMode.None,
				HorizontalAlignment = HorizontalAlignment.Stretch,
				IsItemClickEnabled = false
			};
			favListView.DragItemsCompleted += (_, _) =>
			{
				var newOrder = new List<string>(reorderObs.Count);
				var reorderedTracks = new List<Track>(reorderObs.Count);
				foreach (var row in reorderObs)
				{
					if (row.Tag is Track tr)
					{
						newOrder.Add(tr.FilePath);
						reorderedTracks.Add(tr);
					}
				}
				settingsSvc.Update(s => s.FavoriteTrackOrder = newOrder);
				favs = reorderedTracks;
			};
			listStack.Children.Add(favListView);
		}
		listCard.Child = listStack;
		_rootStack.Children.Add(listCard);
	}

	// ── Shared Song Row (tightened Title/Artist-to-Album spacing + Heart Favorite Button) ──
	private Grid CreateSongHeaderRow()
	{
		var g = new Grid { ColumnSpacing = 12, Padding = new Thickness(10, 4, 10, 6) };
		g.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(44) }); // Cover
		g.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.65, GridUnitType.Star) }); // Title / Artist
		g.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(48) }); // Heart
		g.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.15, GridUnitType.Star) }); // Album (closer!)
		g.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(72) }); // Format
		g.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(60) }); // Duration

		void AddCol(int col, string txt)
		{
			var tb = new TextBlock { Text = txt, FontSize = 12, Opacity = 0.65, FontWeight = FontWeights.SemiBold };
			Grid.SetColumn(tb, col);
			g.Children.Add(tb);
		}
		AddCol(0, Loc.T("封面"));
		AddCol(1, Loc.T("歌名 / 歌手"));
		AddCol(2, Loc.T("喜欢"));
		AddCol(3, Loc.T("专辑"));
		AddCol(4, Loc.T("格式"));
		AddCol(5, Loc.T("时长"));
		return g;
	}

	private Border CreateSongRow(Track track, HomeViewModel vm, bool showAlbumCol)
	{
		var rowBorder = new Border
		{
			CornerRadius = new CornerRadius(10),
			Padding = new Thickness(10, 6, 10, 6),
			HorizontalAlignment = HorizontalAlignment.Stretch,
			Background = new SolidColorBrush(IsDark ? Color.FromArgb(24, 255, 255, 255) : Color.FromArgb(95, 255, 255, 255)),
			BorderThickness = new Thickness(1),
			BorderBrush = new SolidColorBrush(IsDark ? Color.FromArgb(26, 255, 255, 255) : Color.FromArgb(40, 148, 163, 184))
		};

		var g = new Grid { ColumnSpacing = 12, HorizontalAlignment = HorizontalAlignment.Stretch };
		g.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(44) });
		g.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.65, GridUnitType.Star) });
		g.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(48) });
		if (showAlbumCol)
		{
			g.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.15, GridUnitType.Star) });
		}
		g.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(72) });
		g.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(60) });

		// Cover
		var cov = new Border
		{
			Width = 40,
			Height = 40,
			CornerRadius = new CornerRadius(8),
			Background = new SolidColorBrush(IsDark ? Color.FromArgb(30, 255, 255, 255) : Color.FromArgb(30, 148, 163, 184))
		};
		if (!string.IsNullOrEmpty(track.CoverPath) && System.IO.File.Exists(track.CoverPath))
		{
			cov.Child = new Image
			{
				Stretch = Stretch.UniformToFill,
				Source = new BitmapImage { DecodePixelType = DecodePixelType.Logical, DecodePixelWidth = 96, UriSource = new Uri(track.CoverPath) }
			};
		}
		else
		{
			cov.Child = FluentIconFactory.Create(FluentIconKind.NowPlaying, 18);
		}
		Grid.SetColumn(cov, 0);
		g.Children.Add(cov);

		// Title / Artist (clickable to play)
		var titleBtn = new Button
		{
			HorizontalAlignment = HorizontalAlignment.Stretch,
			HorizontalContentAlignment = HorizontalAlignment.Left,
			Padding = new Thickness(2),
			Background = new SolidColorBrush(Colors.Transparent),
			BorderThickness = new Thickness(0)
		};
		var titleStack = new StackPanel { Spacing = 1 };
		titleStack.Children.Add(new TextBlock { Text = track.DisplayTitle, FontSize = 13.5, FontWeight = FontWeights.SemiBold, TextTrimming = TextTrimming.CharacterEllipsis });
		titleStack.Children.Add(new TextBlock { Text = string.IsNullOrWhiteSpace(track.Artist) ? Loc.T("未知艺人") : track.Artist, FontSize = 11.5, Opacity = 0.72, TextTrimming = TextTrimming.CharacterEllipsis });
		titleBtn.Content = titleStack;
		titleBtn.Click += (_, _) => vm.PlayTrackCommand.Execute(track);
		Grid.SetColumn(titleBtn, 1);
		g.Children.Add(titleBtn);

		// Heart Favorite Button ("喜欢")
		var heartTb = new TextBlock
		{
			Text = track.IsFavorite ? "♥" : "♡",
			FontSize = 17,
			Foreground = new SolidColorBrush(track.IsFavorite ? Color.FromArgb(255, 239, 68, 68) : (IsDark ? Color.FromArgb(190, 226, 232, 240) : Color.FromArgb(180, 100, 116, 139))),
			HorizontalAlignment = HorizontalAlignment.Center,
			VerticalAlignment = VerticalAlignment.Center
		};
		var heartBtn = new Button
		{
			Content = heartTb,
			Width = 36,
			Height = 32,
			Padding = new Thickness(0),
			CornerRadius = new CornerRadius(8),
			Background = new SolidColorBrush(Colors.Transparent),
			BorderThickness = new Thickness(0),
			VerticalAlignment = VerticalAlignment.Center
		};
		ToolTipService.SetToolTip(heartBtn, Loc.T("喜欢 / 加入我喜欢的音乐"));
		heartBtn.Click += async (_, _) =>
		{
			await vm.ToggleFavoriteCommand.ExecuteAsync(track);
			if (_mode == "favorites")
			{
				Render();
			}
			else
			{
				heartTb.Text = track.IsFavorite ? "♥" : "♡";
				heartTb.Foreground = new SolidColorBrush(track.IsFavorite ? Color.FromArgb(255, 239, 68, 68) : (IsDark ? Color.FromArgb(190, 226, 232, 240) : Color.FromArgb(180, 100, 116, 139)));
			}
		};
		Grid.SetColumn(heartBtn, 2);
		g.Children.Add(heartBtn);

		int nextCol = 3;
		if (showAlbumCol)
		{
			var albTb = new TextBlock
			{
				Text = string.IsNullOrWhiteSpace(track.Album) ? "-" : track.Album,
				FontSize = 12.5,
				Opacity = 0.78,
				VerticalAlignment = VerticalAlignment.Center,
				TextTrimming = TextTrimming.CharacterEllipsis
			};
			Grid.SetColumn(albTb, nextCol++);
			g.Children.Add(albTb);
		}

		var fmtTb = new TextBlock
		{
			Text = track.Format.ToString(),
			FontSize = 12,
			Opacity = 0.72,
			VerticalAlignment = VerticalAlignment.Center
		};
		Grid.SetColumn(fmtTb, nextCol++);
		g.Children.Add(fmtTb);

		var durTb = new TextBlock
		{
			Text = track.DurationText,
			FontSize = 12,
			Opacity = 0.72,
			VerticalAlignment = VerticalAlignment.Center
		};
		Grid.SetColumn(durTb, nextCol);
		g.Children.Add(durTb);

		rowBorder.Child = g;
		return rowBorder;
	}

	// ── UI Helpers ──
	private Border CreateGlassCard(Thickness padding)
	{
		var b = new Border
		{
			CornerRadius = new CornerRadius(14),
			Padding = padding,
			Background = new SolidColorBrush(IsDark ? Color.FromArgb(44, 30, 41, 59) : Color.FromArgb(165, 255, 255, 255)),
			BorderThickness = new Thickness(1),
			BorderBrush = new SolidColorBrush(IsDark ? Color.FromArgb(38, 255, 255, 255) : Color.FromArgb(65, 148, 163, 184))
		};
		LiquidGlassEngine.AttachInteractiveLiquidGlass(b, LiquidGlassVariant.Regular, IsDark);
		return b;
	}

	private Button CreatePillButton(string text, bool isAccent = false)
	{
		var btn = new Button
		{
			Content = text,
			CornerRadius = new CornerRadius(10),
			Padding = new Thickness(12, 6, 12, 6),
			FontSize = 12.5,
			FontWeight = FontWeights.SemiBold
		};
		LiquidGlassEngine.AttachInteractiveLiquidGlass(btn, isAccent ? LiquidGlassVariant.AccentLensed : LiquidGlassVariant.Clear, IsDark);
		return btn;
	}

	private void AddStatCard(Grid grid, int col, string label, string value, FluentIconKind icon, Action onClick)
	{
		var card = CreateGlassCard(new Thickness(14, 12, 14, 12));
		var row = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 12 };
		row.Children.Add(FluentIconFactory.Create(icon, 24));
		var sp = new StackPanel { Spacing = 1 };
		sp.Children.Add(new TextBlock { Text = value, FontSize = 20, FontWeight = FontWeights.Bold });
		sp.Children.Add(new TextBlock { Text = label, FontSize = 12, Opacity = 0.72 });
		row.Children.Add(sp);

		var btn = new Button
		{
			Content = row,
			HorizontalAlignment = HorizontalAlignment.Stretch,
			HorizontalContentAlignment = HorizontalAlignment.Left,
			Padding = new Thickness(0),
			Background = new SolidColorBrush(Colors.Transparent),
			BorderThickness = new Thickness(0)
		};
		btn.Click += (_, _) => onClick();
		card.Child = btn;
		Grid.SetColumn(card, col);
		grid.Children.Add(card);
	}

	private static StackPanel MakeStatPair(string big, string sub) => new StackPanel
	{
		Spacing = 1,
		Children =
		{
			new TextBlock { Text = big, FontSize = 20, FontWeight = FontWeights.Bold },
			new TextBlock { Text = sub, FontSize = 11.5, Opacity = 0.7 }
		}
	};

	private void AddTrackSquareTile(Grid grid, int col, Track track, HomeViewModel vm)
	{
		var sp = new StackPanel { Spacing = 6 };
		var imgBox = new Border
		{
			Height = 130,
			CornerRadius = new CornerRadius(10),
			Background = new SolidColorBrush(IsDark ? Color.FromArgb(30, 255, 255, 255) : Color.FromArgb(30, 148, 163, 184))
		};
		if (!string.IsNullOrEmpty(track.CoverPath) && System.IO.File.Exists(track.CoverPath))
		{
			imgBox.Child = new Image
			{
				Stretch = Stretch.UniformToFill,
				Source = new BitmapImage { DecodePixelType = DecodePixelType.Logical, DecodePixelWidth = 240, UriSource = new Uri(track.CoverPath) }
			};
		}
		else
		{
			imgBox.Child = FluentIconFactory.Create(FluentIconKind.NowPlaying, 34);
		}
		sp.Children.Add(imgBox);
		sp.Children.Add(new TextBlock { Text = track.DisplayTitle, FontSize = 13, FontWeight = FontWeights.SemiBold, TextTrimming = TextTrimming.CharacterEllipsis });
		sp.Children.Add(new TextBlock { Text = track.Artist, FontSize = 11.5, Opacity = 0.72, TextTrimming = TextTrimming.CharacterEllipsis });

		var btn = new Button
		{
			Content = sp,
			HorizontalAlignment = HorizontalAlignment.Stretch,
			HorizontalContentAlignment = HorizontalAlignment.Stretch,
			Padding = new Thickness(4),
			CornerRadius = new CornerRadius(10),
			Background = new SolidColorBrush(Colors.Transparent),
			BorderThickness = new Thickness(0)
		};
		btn.Click += (_, _) => vm.PlayTrackCommand.Execute(track);
		Grid.SetColumn(btn, col);
		grid.Children.Add(btn);
	}

	private void AddAlbumSquareTile(Grid grid, int col, Album album, HomeViewModel vm)
	{
		var sp = new StackPanel { Spacing = 6 };
		var imgBox = new Border
		{
			Height = 130,
			CornerRadius = new CornerRadius(10),
			Background = new SolidColorBrush(IsDark ? Color.FromArgb(30, 255, 255, 255) : Color.FromArgb(30, 148, 163, 184))
		};
		if (!string.IsNullOrEmpty(album.CoverPath) && System.IO.File.Exists(album.CoverPath))
		{
			imgBox.Child = new Image
			{
				Stretch = Stretch.UniformToFill,
				Source = new BitmapImage { DecodePixelType = DecodePixelType.Logical, DecodePixelWidth = 240, UriSource = new Uri(album.CoverPath) }
			};
		}
		else
		{
			imgBox.Child = FluentIconFactory.Create(FluentIconKind.AlbumGrid, 34);
		}
		sp.Children.Add(imgBox);
		sp.Children.Add(new TextBlock { Text = album.Name, FontSize = 13, FontWeight = FontWeights.SemiBold, TextTrimming = TextTrimming.CharacterEllipsis });
		sp.Children.Add(new TextBlock { Text = $"{album.Artist} · {album.Tracks.Count} {Loc.T("首")}", FontSize = 11.5, Opacity = 0.72, TextTrimming = TextTrimming.CharacterEllipsis });

		var btn = new Button
		{
			Content = sp,
			HorizontalAlignment = HorizontalAlignment.Stretch,
			HorizontalContentAlignment = HorizontalAlignment.Stretch,
			Padding = new Thickness(4),
			CornerRadius = new CornerRadius(10),
			Background = new SolidColorBrush(Colors.Transparent),
			BorderThickness = new Thickness(0)
		};
		btn.Click += (_, _) =>
		{
			_selectedAlbum = album;
			NavigateRequested?.Invoke("albums");
		};
		Grid.SetColumn(btn, col);
		grid.Children.Add(btn);
	}

	private void AddRankedTrackTile(Grid grid, int row, int col, int rank, Track track, HomeViewModel vm)
	{
		var g = new Grid { ColumnSpacing = 10 };
		g.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(24) });
		g.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(42) });
		g.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

		g.Children.Add(new TextBlock { Text = $"{rank:D2}", FontSize = 13, FontWeight = FontWeights.SemiBold, Opacity = 0.65, VerticalAlignment = VerticalAlignment.Center });

		var cov = new Border
		{
			Width = 40,
			Height = 40,
			CornerRadius = new CornerRadius(8),
			Background = new SolidColorBrush(IsDark ? Color.FromArgb(30, 255, 255, 255) : Color.FromArgb(30, 148, 163, 184))
		};
		if (!string.IsNullOrEmpty(track.CoverPath) && System.IO.File.Exists(track.CoverPath))
		{
			cov.Child = new Image
			{
				Stretch = Stretch.UniformToFill,
				Source = new BitmapImage { DecodePixelType = DecodePixelType.Logical, DecodePixelWidth = 80, UriSource = new Uri(track.CoverPath) }
			};
		}
		Grid.SetColumn(cov, 1);
		g.Children.Add(cov);

		var sp = new StackPanel { Spacing = 1, VerticalAlignment = VerticalAlignment.Center };
		sp.Children.Add(new TextBlock { Text = track.DisplayTitle, FontSize = 13, FontWeight = FontWeights.SemiBold, TextTrimming = TextTrimming.CharacterEllipsis });
		sp.Children.Add(new TextBlock { Text = $"{track.Artist} · {Math.Max(1, track.PlayCount)} {Loc.T("次")}", FontSize = 11.5, Opacity = 0.72, TextTrimming = TextTrimming.CharacterEllipsis });
		Grid.SetColumn(sp, 2);
		g.Children.Add(sp);

		var btn = new Button
		{
			Content = g,
			HorizontalAlignment = HorizontalAlignment.Stretch,
			HorizontalContentAlignment = HorizontalAlignment.Stretch,
			Padding = new Thickness(4),
			CornerRadius = new CornerRadius(8),
			Background = new SolidColorBrush(Colors.Transparent),
			BorderThickness = new Thickness(0)
		};
		btn.Click += (_, _) => vm.PlayTrackCommand.Execute(track);
		Grid.SetRow(btn, row);
		Grid.SetColumn(btn, col);
		grid.Children.Add(btn);
	}
}
