using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Dispatching;
using SonicWave.Audio;
using SonicWave.Core.Models;
using SonicWave.Core.Services;

namespace FurinaPlayer.UI.ViewModels;

public class StreamingPlatformItem : ObservableObject
{
	private string name = string.Empty;
	public string Name
	{
		get => name;
		set => SetProperty(ref name, value);
	}

	private string subtitle = string.Empty;
	public string Subtitle
	{
		get => subtitle;
		set => SetProperty(ref subtitle, value);
	}

	private bool isAvailable = true;
	public bool IsAvailable
	{
		get => isAvailable;
		set => SetProperty(ref isAvailable, value);
	}

	private bool isSelected;
	public bool IsSelected
	{
		get => isSelected;
		set => SetProperty(ref isSelected, value);
	}

	private bool isLoggedIn;
	public bool IsLoggedIn
	{
		get => isLoggedIn;
		set => SetProperty(ref isLoggedIn, value);
	}

	private string statusBadgeText = "未登录";
	public string StatusBadgeText
	{
		get => statusBadgeText;
		set => SetProperty(ref statusBadgeText, value);
	}
}

public class StreamingItemViewModel : ObservableObject
{
	public int DisplayIndex { get; set; }
	public long Id { get; set; }
	public string Title { get; set; } = string.Empty;
	public string ArtistAndAlbum { get; set; } = string.Empty;
	public string? CoverUrl { get; set; }
	public string SourceAndQuality { get; set; } = string.Empty;
	public string DurationText { get; set; } = "0:00";
	public StreamingTrackInfo RawInfo { get; set; } = new();
}

public class StreamingViewModel : ObservableObject
{
	private readonly AudioEngine _engine;
	private readonly LibraryService? _library;
	private readonly AlbumArtService? _albumArt;
	private readonly StreamingService _streamingService;
	private readonly DispatcherQueue? _dispatcherQueue;

	public event Action? AccountStateChanged;

	public bool IsNeteaseLoggedIn => _streamingService.CurrentNeteaseAccount.IsLoggedIn;
	public string NeteaseNickname => _streamingService.CurrentNeteaseAccount.Nickname;
	public string NeteaseAvatarUrl => _streamingService.CurrentNeteaseAccount.AvatarUrl;
	public long NeteaseUserId => _streamingService.CurrentNeteaseAccount.UserId;
	public string NeteaseVipStatus => _streamingService.CurrentNeteaseAccount.VipStatus;

	public bool IsQQLoggedIn => _streamingService.CurrentQQAccount.IsLoggedIn;
	public string QQNickname => _streamingService.CurrentQQAccount.Nickname;
	public string QQAvatarUrl => _streamingService.CurrentQQAccount.AvatarUrl;
	public string QQUin => _streamingService.CurrentQQAccount.UserIdStr;
	public string QQVipStatus => _streamingService.CurrentQQAccount.VipStatus;

	public bool IsSpotifyLoggedIn => _streamingService.CurrentSpotifyAccount.IsLoggedIn;
	public string SpotifyNickname => _streamingService.CurrentSpotifyAccount.Nickname;
	public string SpotifyAvatarUrl => _streamingService.CurrentSpotifyAccount.AvatarUrl;
	public string SpotifyUserId => _streamingService.CurrentSpotifyAccount.UserIdStr;
	public string SpotifyVipStatus => _streamingService.CurrentSpotifyAccount.VipStatus;

	public bool IsCurrentPlatformLoggedIn => SelectedPlatform switch
	{
		"QQ 音乐" or "QQ音乐" => IsQQLoggedIn,
		"Spotify" => IsSpotifyLoggedIn,
		_ => IsNeteaseLoggedIn
	};

	public StreamingAccountInfo CurrentPlatformAccount => _streamingService.GetAccount(SelectedPlatform);

	public StreamingAccountInfo GetAccount(string? platform) => _streamingService.GetAccount(platform);

	private string searchQuery = "原神";
	public string SearchQuery
	{
		get => searchQuery;
		set
		{
			if (searchQuery != value)
			{
				searchQuery = value;
				OnPropertyChanged(nameof(SearchQuery));
			}
		}
	}

	private string selectedPlatform = "网易云音乐";
	public string SelectedPlatform
	{
		get => selectedPlatform;
		set
		{
			if (selectedPlatform != value)
			{
				selectedPlatform = value;
				OnPropertyChanged(nameof(SelectedPlatform));
			}
		}
	}

	private string selectedTab = "歌曲";
	public string SelectedTab
	{
		get => selectedTab;
		set
		{
			if (selectedTab != value)
			{
				selectedTab = value;
				OnPropertyChanged(nameof(SelectedTab));
			}
		}
	}

	private string selectedQuality = "高音质";
	public string SelectedQuality
	{
		get => selectedQuality;
		set
		{
			if (selectedQuality != value)
			{
				selectedQuality = value;
				OnPropertyChanged(nameof(SelectedQuality));
			}
		}
	}

	private bool isSearching;
	public bool IsSearching
	{
		get => isSearching;
		set
		{
			if (isSearching != value)
			{
				isSearching = value;
				OnPropertyChanged(nameof(IsSearching));
			}
		}
	}

	private string statusMessage = "已读取 10 个歌单，可同步全部或勾选后同步。";
	public string StatusMessage
	{
		get => statusMessage;
		set
		{
			if (statusMessage != value)
			{
				statusMessage = value;
				OnPropertyChanged(nameof(StatusMessage));
			}
		}
	}

	public ObservableCollection<StreamingPlatformItem> Platforms { get; } = new();
	public ObservableCollection<string> RecentSearches { get; } = new();
	public ObservableCollection<StreamingItemViewModel> Results { get; } = new();

	private int _currentOffset = 0;
	private const int PageSize = 25;

	public StreamingViewModel(AudioEngine engine, LibraryService? library = null, AlbumArtService? albumArt = null, SettingsService? settingsService = null)
	{
		_engine = engine;
		_library = library;
		_albumArt = albumArt;
		_streamingService = new StreamingService(settingsService);

		try
		{
			_dispatcherQueue = DispatcherQueue.GetForCurrentThread();
		}
		catch { }

		_streamingService.AccountStateChanged += (s, acc) =>
		{
			RunOnUi(() =>
			{
				OnPropertyChanged(nameof(IsNeteaseLoggedIn));
				OnPropertyChanged(nameof(NeteaseNickname));
				OnPropertyChanged(nameof(NeteaseAvatarUrl));
				OnPropertyChanged(nameof(NeteaseUserId));
				OnPropertyChanged(nameof(NeteaseVipStatus));

				OnPropertyChanged(nameof(IsQQLoggedIn));
				OnPropertyChanged(nameof(QQNickname));
				OnPropertyChanged(nameof(QQAvatarUrl));
				OnPropertyChanged(nameof(QQUin));
				OnPropertyChanged(nameof(QQVipStatus));

				OnPropertyChanged(nameof(IsSpotifyLoggedIn));
				OnPropertyChanged(nameof(SpotifyNickname));
				OnPropertyChanged(nameof(SpotifyAvatarUrl));
				OnPropertyChanged(nameof(SpotifyUserId));
				OnPropertyChanged(nameof(SpotifyVipStatus));

				OnPropertyChanged(nameof(IsCurrentPlatformLoggedIn));
				OnPropertyChanged(nameof(CurrentPlatformAccount));

				UpdatePlatformsState();
				AccountStateChanged?.Invoke();
			});
		};

		InitPlatforms();

		var recents = new[] { "原神", "昔涟", "take me hand", "冬冬", "山海主韵", "山海主韵实体", "hoyomix" };
		foreach (var r in recents) RecentSearches.Add(r);

		_ = Task.Run(async () => await SearchAsync(SearchQuery, isNew: true));
	}

	private void InitPlatforms()
	{
		Platforms.Clear();
		Platforms.Add(new StreamingPlatformItem
		{
			Name = "网易云音乐",
			Subtitle = IsNeteaseLoggedIn ? $"已登录 · {NeteaseNickname}" : "未登录 (需认证)",
			IsAvailable = true,
			IsLoggedIn = IsNeteaseLoggedIn,
			StatusBadgeText = IsNeteaseLoggedIn ? "已登录" : "未登录",
			IsSelected = true
		});
		Platforms.Add(new StreamingPlatformItem
		{
			Name = "QQ 音乐",
			Subtitle = IsQQLoggedIn ? $"已登录 · {QQNickname}" : "游客可用 (可导入Cookie)",
			IsAvailable = true,
			IsLoggedIn = IsQQLoggedIn,
			StatusBadgeText = IsQQLoggedIn ? "已登录" : "可用",
			IsSelected = false
		});
		Platforms.Add(new StreamingPlatformItem { Name = "Bilibili", Subtitle = "游客模式", IsAvailable = true, IsLoggedIn = false, StatusBadgeText = "可用", IsSelected = false });
		Platforms.Add(new StreamingPlatformItem { Name = "YouTube", Subtitle = "无需登录", IsAvailable = true, IsLoggedIn = false, StatusBadgeText = "可用", IsSelected = false });
		Platforms.Add(new StreamingPlatformItem { Name = "SoundCloud", Subtitle = "未授权", IsAvailable = false, IsLoggedIn = false, StatusBadgeText = "未登录", IsSelected = false });
		Platforms.Add(new StreamingPlatformItem
		{
			Name = "Spotify",
			Subtitle = IsSpotifyLoggedIn ? $"已登录 · {SpotifyNickname}" : "未连接 (可导入Token)",
			IsAvailable = true,
			IsLoggedIn = IsSpotifyLoggedIn,
			StatusBadgeText = IsSpotifyLoggedIn ? "已登录" : "未登录",
			IsSelected = false
		});
		Platforms.Add(new StreamingPlatformItem { Name = "TIDAL", Subtitle = "未登录", IsAvailable = false, IsLoggedIn = false, StatusBadgeText = "未登录", IsSelected = false });
		Platforms.Add(new StreamingPlatformItem { Name = "Qobuz", Subtitle = "未登录", IsAvailable = false, IsLoggedIn = false, StatusBadgeText = "未登录", IsSelected = false });
		Platforms.Add(new StreamingPlatformItem { Name = "插件音源", Subtitle = "已就绪", IsAvailable = true, IsLoggedIn = false, StatusBadgeText = "可用", IsSelected = false });
	}

	public void UpdatePlatformsState()
	{
		var netease = Platforms.FirstOrDefault(p => p.Name == "网易云音乐");
		if (netease != null)
		{
			netease.IsAvailable = true;
			netease.IsLoggedIn = IsNeteaseLoggedIn;
			netease.Subtitle = IsNeteaseLoggedIn ? $"已登录 · {NeteaseNickname}" : "未登录 (需认证)";
			netease.StatusBadgeText = IsNeteaseLoggedIn ? "已登录" : "未登录";
		}

		var qq = Platforms.FirstOrDefault(p => p.Name == "QQ 音乐" || p.Name == "QQ音乐");
		if (qq != null)
		{
			qq.IsAvailable = true;
			qq.IsLoggedIn = IsQQLoggedIn;
			qq.Subtitle = IsQQLoggedIn ? $"已登录 · {QQNickname}" : "游客可用 (可导入Cookie)";
			qq.StatusBadgeText = IsQQLoggedIn ? "已登录" : "可用";
		}

		var sp = Platforms.FirstOrDefault(p => p.Name == "Spotify");
		if (sp != null)
		{
			sp.IsAvailable = true;
			sp.IsLoggedIn = IsSpotifyLoggedIn;
			sp.Subtitle = IsSpotifyLoggedIn ? $"已登录 · {SpotifyNickname}" : "未连接 (可导入Token)";
			sp.StatusBadgeText = IsSpotifyLoggedIn ? "已登录" : "未登录";
		}
	}

	#region 登录认证操作封装

	public async Task<string?> RequestQrKeyAsync()
	{
		return await _streamingService.RequestNeteaseQrKeyAsync();
	}

	public async Task<(string qrUrl, string? qrImgBase64)> CreateQrAsync(string key)
	{
		return await _streamingService.CreateNeteaseQrAsync(key);
	}

	public async Task<(int code, string message, bool success)> CheckQrStatusAsync(string key)
	{
		var result = await _streamingService.CheckNeteaseQrStatusAsync(key);
		if (result.code == 803 && !string.IsNullOrEmpty(result.cookie))
		{
			// 登录成功，获取详细用户信息并持久化
			var account = await _streamingService.VerifyAndFetchNeteaseAccountAsync(result.cookie);
			if (account != null)
			{
				_streamingService.SaveNeteaseAccount(account);
				StatusMessage = $"网易云音乐登录成功！欢迎回来，{account.Nickname}";
				return (803, "登录成功", true);
			}
			else
			{
				var fallbackAcc = new StreamingAccountInfo
				{
					Platform = "网易云音乐",
					IsLoggedIn = true,
					UserId = 0,
					Nickname = !string.IsNullOrEmpty(result.nickname) ? result.nickname : "网易云用户",
					AvatarUrl = result.avatarUrl ?? string.Empty,
					Cookie = result.cookie,
					VipStatus = "认证用户",
					LoginTime = DateTime.Now
				};
				_streamingService.SaveNeteaseAccount(fallbackAcc);
				StatusMessage = $"网易云音乐登录成功！欢迎回来，{fallbackAcc.Nickname}";
				return (803, "登录成功", true);
			}
		}

		return (result.code, result.message, false);
	}

	public async Task<(bool success, string message)> LoginWithCookieAsync(string cookieInput)
	{
		if (string.IsNullOrWhiteSpace(cookieInput))
		{
			return (false, "输入的 Cookie 不能为空。");
		}

		var account = await _streamingService.VerifyAndFetchNeteaseAccountAsync(cookieInput);
		if (account != null && account.IsLoggedIn)
		{
			_streamingService.SaveNeteaseAccount(account);
			StatusMessage = $"Cookie 校验成功！已登录网易云账号: {account.Nickname}";
			return (true, $"欢迎回来，{account.Nickname}！");
		}
		else
		{
			return (false, "Cookie 校验失败，请检查 MUSIC_U 是否有效或是否已过期。");
		}
	}

	public async Task<(bool success, string message)> LoginQQWithCookieAsync(string cookieInput)
	{
		if (string.IsNullOrWhiteSpace(cookieInput))
		{
			return (false, "输入的 QQ 音乐 Cookie 不能为空。");
		}

		var account = await _streamingService.VerifyAndFetchQQAccountAsync(cookieInput);
		if (account != null && account.IsLoggedIn)
		{
			_streamingService.SaveQQAccount(account);
			StatusMessage = $"QQ 音乐 Cookie 导入成功！已登录账号: {account.Nickname} ({account.UserIdStr})";
			return (true, $"欢迎回来，{account.Nickname}！");
		}
		else
		{
			return (false, "QQ 音乐凭据校验失败，请检查 Cookie 中是否包含 uin 或 qm_keyst 凭据。");
		}
	}

	public async Task<(bool success, string message)> LoginSpotifyWithCredentialAsync(string credentialInput)
	{
		if (string.IsNullOrWhiteSpace(credentialInput))
		{
			return (false, "输入的 Spotify Token 或 Cookie 不能为空。");
		}

		var account = await _streamingService.VerifyAndFetchSpotifyAccountAsync(credentialInput);
		if (account != null && account.IsLoggedIn)
		{
			_streamingService.SaveSpotifyAccount(account);
			StatusMessage = $"Spotify 凭据导入成功！已连接账号: {account.Nickname}";
			return (true, $"欢迎回来，{account.Nickname}！");
		}
		else
		{
			return (false, "Spotify 凭证校验失败，请检查 Access Token 或 sp_dc 是否有效。");
		}
	}

	public async Task<(bool success, string message)> RefreshAccountStatusAsync(string? platform = null)
	{
		string targetPlat = platform ?? SelectedPlatform;
		bool ok = await _streamingService.RefreshAccountStatusAsync(targetPlat);
		var acc = _streamingService.GetAccount(targetPlat);
		if (ok && acc.IsLoggedIn)
		{
			StatusMessage = $"{targetPlat} 账号状态已刷新 · {acc.Nickname} ({acc.VipStatus})";
			return (true, $"{targetPlat} 账号状态刷新成功！");
		}
		else
		{
			StatusMessage = $"{targetPlat} 登录状态已失效，已退出登录。";
			return (false, $"{targetPlat} 登录已失效或未登录，请重新认证。");
		}
	}

	public void Logout(string? platform = null)
	{
		string targetPlat = platform ?? SelectedPlatform;
		var acc = _streamingService.GetAccount(targetPlat);
		string prevNick = acc.Nickname;
		_streamingService.LogoutPlatform(targetPlat);
		StatusMessage = $"已成功退出 {targetPlat} 账号 ({prevNick})";
	}

	#endregion

	private void RunOnUi(Action action)
	{
		if (_dispatcherQueue != null && !_dispatcherQueue.HasThreadAccess)
		{
			_dispatcherQueue.TryEnqueue(() => action());
		}
		else
		{
			action();
		}
	}

	public async Task SearchAsync(string? query = null, bool isNew = true)
	{
		string q = (query ?? SearchQuery).Trim();
		if (string.IsNullOrWhiteSpace(q)) return;

		RunOnUi(() =>
		{
			SearchQuery = q;
			if (isNew)
			{
				_currentOffset = 0;
				if (!RecentSearches.Contains(q))
				{
					RecentSearches.Insert(0, q);
					if (RecentSearches.Count > 10) RecentSearches.RemoveAt(RecentSearches.Count - 1);
				}
			}
			IsSearching = true;
		});

		try
		{
			int searchType = SelectedTab switch
			{
				"专辑" => 10,
				"艺人" => 100,
				"歌单" => 1000,
				_ => 1
			};

			var items = await _streamingService.SearchOnlineTracksAsync(q, offset: _currentOffset, limit: PageSize, searchType: searchType, platform: SelectedPlatform, quality: SelectedQuality);

			RunOnUi(() =>
			{
				if (isNew) Results.Clear();

				int startIdx = Results.Count + 1;
				foreach (var item in items)
				{
					Results.Add(new StreamingItemViewModel
					{
						DisplayIndex = startIdx++,
						Id = item.Id,
						Title = item.Title,
						ArtistAndAlbum = $"{item.Artist} / {item.Album}",
						CoverUrl = item.CoverUrl,
						SourceAndQuality = item.Quality,
						DurationText = item.Duration.TotalSeconds > 0 ? item.Duration.ToString("m\\:ss") : "--:--",
						RawInfo = item
					});
				}

				_currentOffset += items.Count;
				StatusMessage = $"已检索到 {Results.Count} 首高品质流媒体曲目 · 数据来源于 {SelectedPlatform}";
			});
		}
		finally
		{
			RunOnUi(() => IsSearching = false);
		}
	}

	public async Task LoadMoreAsync()
	{
		if (IsSearching) return;
		await SearchAsync(SearchQuery, isNew: false);
	}

	public async Task PlayTrackAsync(StreamingItemViewModel? item)
	{
		if (item == null) return;

		// 尝试解析真实高保真/VIP播放流 (携带已登录凭据认证)
		try
		{
			string realStreamUrl = await _streamingService.ResolvePlayUrlAsync(item.Id, item.RawInfo.Source);
			if (!string.IsNullOrEmpty(realStreamUrl) && Uri.IsWellFormedUriString(realStreamUrl, UriKind.Absolute))
			{
				item.RawInfo.StreamUrl = realStreamUrl;
			}
		}
		catch { }

		string coverDir = _library?.CoverDirectory ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "FurinaPlayer", "covers");
		try { Directory.CreateDirectory(coverDir); } catch { }
		string coverFile = Path.Combine(coverDir, $"stream_{Math.Abs(item.Id)}.jpg");

		string? initialCover = File.Exists(coverFile) ? coverFile : (!string.IsNullOrEmpty(item.CoverUrl) ? item.CoverUrl : null);
		Track track = _streamingService.ToTrack(item.RawInfo, initialCover);

		if (string.IsNullOrWhiteSpace(track.Title)) track.Title = item.Title;
		if (string.IsNullOrWhiteSpace(track.Artist)) track.Artist = item.RawInfo.Artist;
		if (string.IsNullOrWhiteSpace(track.Album)) track.Album = item.RawInfo.Album;
		if (track.DurationMilliseconds <= 0 && item.RawInfo.Duration.TotalMilliseconds > 0)
		{
			track.DurationMilliseconds = (long)item.RawInfo.Duration.TotalMilliseconds;
		}

		if (!string.IsNullOrEmpty(item.CoverUrl))
		{
			if (File.Exists(coverFile))
			{
				track.CoverPath = coverFile;
			}
			else
			{
				_ = Task.Run(async () =>
				{
					try
					{
						using var hc = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
						var bytes = await hc.GetByteArrayAsync(item.CoverUrl);
						await File.WriteAllBytesAsync(coverFile, bytes);
						track.CoverPath = coverFile;
						if (_engine.CurrentTrack?.Id == track.Id)
						{
							_engine.CurrentTrack.CoverPath = coverFile;
						}
					}
					catch { }
				});
			}
		}

		_engine.PlayTrack(track);
		await Task.CompletedTask;
	}

	public async Task AddToQueueAsync(StreamingItemViewModel? item)
	{
		if (item == null) return;
		string coverDir = _library?.CoverDirectory ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "FurinaPlayer", "covers");
		string coverFile = Path.Combine(coverDir, $"stream_{Math.Abs(item.Id)}.jpg");
		string? initialCover = File.Exists(coverFile) ? coverFile : (!string.IsNullOrEmpty(item.CoverUrl) ? item.CoverUrl : null);
		Track track = _streamingService.ToTrack(item.RawInfo, initialCover);
		if (string.IsNullOrWhiteSpace(track.Title)) track.Title = item.Title;
		if (string.IsNullOrWhiteSpace(track.Artist)) track.Artist = item.RawInfo.Artist;
		if (string.IsNullOrWhiteSpace(track.Album)) track.Album = item.RawInfo.Album;
		if (track.DurationMilliseconds <= 0 && item.RawInfo.Duration.TotalMilliseconds > 0)
		{
			track.DurationMilliseconds = (long)item.RawInfo.Duration.TotalMilliseconds;
		}
		var currentQueue = _engine.Queue.ToList();
		currentQueue.Add(track);
		_engine.SetQueue(currentQueue, _engine.QueueIndex >= 0 ? _engine.QueueIndex : 0);
		StatusMessage = $"已将《{item.Title}》加入播放队列";
		await Task.CompletedTask;
	}

	public async Task DownloadTrackAsync(StreamingItemViewModel? item)
	{
		if (item == null || string.IsNullOrEmpty(item.RawInfo.StreamUrl)) return;
		try
		{
			StatusMessage = $"正在缓冲与下载《{item.Title}》...";
			string musicDir = Environment.GetFolderPath(Environment.SpecialFolder.MyMusic);
			string targetFile = Path.Combine(musicDir, $"{item.RawInfo.Artist} - {item.Title}.mp3");

			using var hc = new HttpClient { Timeout = TimeSpan.FromMinutes(2) };
			var bytes = await hc.GetByteArrayAsync(item.RawInfo.StreamUrl);
			await File.WriteAllBytesAsync(targetFile, bytes);

			if (_library != null)
			{
				await _library.ImportFileAsync(targetFile);
			}
			StatusMessage = $"《{item.Title}》已成功缓存至本地音乐库！";
		}
		catch (Exception ex)
		{
			StatusMessage = $"下载失败: {ex.Message}";
		}
	}

	private RelayCommand? _clearRecentSearchesCommand;
	public RelayCommand ClearRecentSearchesCommand => _clearRecentSearchesCommand ??= new RelayCommand(ClearRecentSearches);

	private RelayCommand? _loadMoreCommand;
	public RelayCommand LoadMoreCommand => _loadMoreCommand ??= new RelayCommand(() => _ = LoadMoreAsync());

	public void ClearRecentSearches()
	{
		RecentSearches.Clear();
	}

	public void SelectPlatform(string platformName)
	{
		SelectedPlatform = platformName;
		foreach (var p in Platforms)
		{
			p.IsSelected = (p.Name == platformName);
		}
		OnPropertyChanged(nameof(IsCurrentPlatformLoggedIn));
		OnPropertyChanged(nameof(CurrentPlatformAccount));
		_ = SearchAsync(SearchQuery, isNew: true);
	}

	public void SelectTab(string tabName)
	{
		SelectedTab = tabName;
		_ = SearchAsync(SearchQuery, isNew: true);
	}
}
