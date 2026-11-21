using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Dispatching;
using FurinaPlayer.Audio;
using FurinaPlayer.Audio.Decoders;
using FurinaPlayer.Core.Models;
using FurinaPlayer.Core.Services;

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

	private string statusBadgeText = Loc.T("未登录");
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
	private readonly SettingsService? _settingsService;
	private readonly StreamingService _streamingService;
	public StreamingService StreamingService => _streamingService;
	private readonly DispatcherQueue? _dispatcherQueue;

	public AppSettings? GetCurrentSettings()
	{
		try
		{
			return (_settingsService ?? new SettingsService()).Load();
		}
		catch
		{
			return null;
		}
	}

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

	private bool isInAlbumDetail;
	public bool IsInAlbumDetail
	{
		get => isInAlbumDetail;
		set
		{
			if (isInAlbumDetail != value)
			{
				isInAlbumDetail = value;
				OnPropertyChanged(nameof(IsInAlbumDetail));
			}
		}
	}

	private bool isLoadingAlbumTracks;
	public bool IsLoadingAlbumTracks
	{
		get => isLoadingAlbumTracks;
		set
		{
			if (isLoadingAlbumTracks != value)
			{
				isLoadingAlbumTracks = value;
				OnPropertyChanged(nameof(IsLoadingAlbumTracks));
			}
		}
	}

	private StreamingItemViewModel? currentAlbumItem;
	public StreamingItemViewModel? CurrentAlbumItem
	{
		get => currentAlbumItem;
		set
		{
			if (currentAlbumItem != value)
			{
				currentAlbumItem = value;
				OnPropertyChanged(nameof(CurrentAlbumItem));
			}
		}
	}

	public ObservableCollection<StreamingItemViewModel> AlbumTracks { get; } = new();

	private bool isInArtistDetail;
	public bool IsInArtistDetail
	{
		get => isInArtistDetail;
		set
		{
			if (isInArtistDetail != value)
			{
				isInArtistDetail = value;
				OnPropertyChanged(nameof(IsInArtistDetail));
			}
		}
	}

	private bool isLoadingArtistDetail;
	public bool IsLoadingArtistDetail
	{
		get => isLoadingArtistDetail;
		set
		{
			if (isLoadingArtistDetail != value)
			{
				isLoadingArtistDetail = value;
				OnPropertyChanged(nameof(IsLoadingArtistDetail));
			}
		}
	}

	private bool isLoadingMoreArtistSongs;
	public bool IsLoadingMoreArtistSongs
	{
		get => isLoadingMoreArtistSongs;
		set
		{
			if (isLoadingMoreArtistSongs != value)
			{
				isLoadingMoreArtistSongs = value;
				OnPropertyChanged(nameof(IsLoadingMoreArtistSongs));
			}
		}
	}

	private bool isLoadingMoreArtistAlbums;
	public bool IsLoadingMoreArtistAlbums
	{
		get => isLoadingMoreArtistAlbums;
		set
		{
			if (isLoadingMoreArtistAlbums != value)
			{
				isLoadingMoreArtistAlbums = value;
				OnPropertyChanged(nameof(IsLoadingMoreArtistAlbums));
			}
		}
	}

	private bool hasMoreArtistSongs;
	public bool HasMoreArtistSongs
	{
		get => hasMoreArtistSongs;
		set
		{
			if (hasMoreArtistSongs != value)
			{
				hasMoreArtistSongs = value;
				OnPropertyChanged(nameof(HasMoreArtistSongs));
			}
		}
	}

	private bool hasMoreArtistAlbums;
	public bool HasMoreArtistAlbums
	{
		get => hasMoreArtistAlbums;
		set
		{
			if (hasMoreArtistAlbums != value)
			{
				hasMoreArtistAlbums = value;
				OnPropertyChanged(nameof(HasMoreArtistAlbums));
			}
		}
	}

	private StreamingItemViewModel? currentArtistItem;
	public StreamingItemViewModel? CurrentArtistItem
	{
		get => currentArtistItem;
		set
		{
			if (currentArtistItem != value)
			{
				currentArtistItem = value;
				OnPropertyChanged(nameof(CurrentArtistItem));
			}
		}
	}

	public ObservableCollection<StreamingItemViewModel> ArtistSongs { get; } = new();
	public ObservableCollection<StreamingItemViewModel> ArtistAlbums { get; } = new();

	private bool isInPlaylistDetail;
	public bool IsInPlaylistDetail
	{
		get => isInPlaylistDetail;
		set
		{
			if (isInPlaylistDetail != value)
			{
				isInPlaylistDetail = value;
				OnPropertyChanged(nameof(IsInPlaylistDetail));
			}
		}
	}

	private bool isLoadingPlaylistTracks;
	public bool IsLoadingPlaylistTracks
	{
		get => isLoadingPlaylistTracks;
		set
		{
			if (isLoadingPlaylistTracks != value)
			{
				isLoadingPlaylistTracks = value;
				OnPropertyChanged(nameof(IsLoadingPlaylistTracks));
			}
		}
	}

	private bool isLoadingMorePlaylistSongs;
	public bool IsLoadingMorePlaylistSongs
	{
		get => isLoadingMorePlaylistSongs;
		set
		{
			if (isLoadingMorePlaylistSongs != value)
			{
				isLoadingMorePlaylistSongs = value;
				OnPropertyChanged(nameof(IsLoadingMorePlaylistSongs));
			}
		}
	}

	private bool hasMorePlaylistSongs;
	public bool HasMorePlaylistSongs
	{
		get => hasMorePlaylistSongs;
		set
		{
			if (hasMorePlaylistSongs != value)
			{
				hasMorePlaylistSongs = value;
				OnPropertyChanged(nameof(HasMorePlaylistSongs));
			}
		}
	}

	private StreamingItemViewModel? currentPlaylistItem;
	public StreamingItemViewModel? CurrentPlaylistItem
	{
		get => currentPlaylistItem;
		set
		{
			if (currentPlaylistItem != value)
			{
				currentPlaylistItem = value;
				OnPropertyChanged(nameof(CurrentPlaylistItem));
			}
		}
	}

	public ObservableCollection<StreamingItemViewModel> PlaylistSongs { get; } = new();

	private string statusMessage = Loc.T("已读取 10 个歌单，可同步全部或勾选后同步。");
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
	private int _searchRequestSeq = 0;
	private const int PageSize = 25;

	public StreamingViewModel(AudioEngine engine, LibraryService? library = null, AlbumArtService? albumArt = null, SettingsService? settingsService = null)
	{
		_engine = engine;
		_library = library;
		_albumArt = albumArt;
		_settingsService = settingsService;
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
			Subtitle = IsNeteaseLoggedIn ? Loc.F("已登录 · {0}", NeteaseNickname) : Loc.T("未登录 (需认证)"),
			IsAvailable = true,
			IsLoggedIn = IsNeteaseLoggedIn,
			StatusBadgeText = IsNeteaseLoggedIn ? Loc.T("已登录") : Loc.T("未登录"),
			IsSelected = true
		});
		Platforms.Add(new StreamingPlatformItem
		{
			Name = "QQ 音乐",
			Subtitle = IsQQLoggedIn ? Loc.F("已登录 · {0}", QQNickname) : Loc.T("游客可用 (可导入Cookie)"),
			IsAvailable = true,
			IsLoggedIn = IsQQLoggedIn,
			StatusBadgeText = IsQQLoggedIn ? Loc.T("已登录") : Loc.T("可用"),
			IsSelected = false
		});
		Platforms.Add(new StreamingPlatformItem { Name = "Bilibili", Subtitle = Loc.T("游客模式"), IsAvailable = true, IsLoggedIn = false, StatusBadgeText = Loc.T("可用"), IsSelected = false });
		Platforms.Add(new StreamingPlatformItem { Name = "YouTube", Subtitle = Loc.T("无需登录"), IsAvailable = true, IsLoggedIn = false, StatusBadgeText = Loc.T("可用"), IsSelected = false });
		Platforms.Add(new StreamingPlatformItem { Name = "SoundCloud", Subtitle = Loc.T("未授权"), IsAvailable = false, IsLoggedIn = false, StatusBadgeText = Loc.T("未登录"), IsSelected = false });
		Platforms.Add(new StreamingPlatformItem
		{
			Name = "Spotify",
			Subtitle = IsSpotifyLoggedIn ? Loc.F("已登录 · {0}", SpotifyNickname) : Loc.T("未连接 (可导入Token)"),
			IsAvailable = true,
			IsLoggedIn = IsSpotifyLoggedIn,
			StatusBadgeText = IsSpotifyLoggedIn ? Loc.T("已登录") : Loc.T("未登录"),
			IsSelected = false
		});
		Platforms.Add(new StreamingPlatformItem { Name = "TIDAL", Subtitle = Loc.T("未登录"), IsAvailable = false, IsLoggedIn = false, StatusBadgeText = Loc.T("未登录"), IsSelected = false });
		Platforms.Add(new StreamingPlatformItem { Name = "Qobuz", Subtitle = Loc.T("未登录"), IsAvailable = false, IsLoggedIn = false, StatusBadgeText = Loc.T("未登录"), IsSelected = false });
		Platforms.Add(new StreamingPlatformItem { Name = "插件音源", Subtitle = Loc.T("已就绪"), IsAvailable = true, IsLoggedIn = false, StatusBadgeText = Loc.T("可用"), IsSelected = false });
	}

	public void UpdatePlatformsState()
	{
		var netease = Platforms.FirstOrDefault(p => p.Name == "网易云音乐");
		if (netease != null)
		{
			netease.IsAvailable = true;
			netease.IsLoggedIn = IsNeteaseLoggedIn;
			netease.Subtitle = IsNeteaseLoggedIn ? Loc.F("已登录 · {0}", NeteaseNickname) : Loc.T("未登录 (需认证)");
			netease.StatusBadgeText = IsNeteaseLoggedIn ? Loc.T("已登录") : Loc.T("未登录");
		}

		var qq = Platforms.FirstOrDefault(p => p.Name == "QQ 音乐" || p.Name == "QQ音乐");
		if (qq != null)
		{
			qq.IsAvailable = true;
			qq.IsLoggedIn = IsQQLoggedIn;
			qq.Subtitle = IsQQLoggedIn ? Loc.F("已登录 · {0}", QQNickname) : Loc.T("游客可用 (可导入Cookie)");
			qq.StatusBadgeText = IsQQLoggedIn ? Loc.T("已登录") : Loc.T("可用");
		}

		var sp = Platforms.FirstOrDefault(p => p.Name == "Spotify");
		if (sp != null)
		{
			sp.IsAvailable = true;
			sp.IsLoggedIn = IsSpotifyLoggedIn;
			sp.Subtitle = IsSpotifyLoggedIn ? Loc.F("已登录 · {0}", SpotifyNickname) : Loc.T("未连接 (可导入Token)");
			sp.StatusBadgeText = IsSpotifyLoggedIn ? Loc.T("已登录") : Loc.T("未登录");
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
				StatusMessage = Loc.F("网易云音乐登录成功！欢迎回来，{0}", account.Nickname);
				return (803, Loc.F("欢迎回来，{0}！", account.Nickname), true);
			}
			else if (result.cookie.Contains("MUSIC_U=", StringComparison.OrdinalIgnoreCase))
			{
				var fallbackAcc = new StreamingAccountInfo
				{
					Platform = "网易云音乐",
					IsLoggedIn = true,
					UserId = 0,
					Nickname = !string.IsNullOrEmpty(result.nickname) ? result.nickname : Loc.T("网易云用户"),
					AvatarUrl = result.avatarUrl ?? string.Empty,
					Cookie = result.cookie,
					VipStatus = Loc.T("认证用户"),
					LoginTime = DateTime.Now
				};
				_streamingService.SaveNeteaseAccount(fallbackAcc);
				StatusMessage = Loc.F("网易云音乐登录成功！欢迎回来，{0}", fallbackAcc.Nickname);
				return (803, Loc.F("欢迎回来，{0}！", fallbackAcc.Nickname), true);
			}
		}

		if (result.code == 803)
		{
			return (803, Loc.T("授权已确认，但未收到有效登录凭证，请重试或使用 Cookie 导入。"), false);
		}
		return (result.code, result.message, false);
	}

	public async Task<(bool success, string message)> LoginWithCookieAsync(string cookieInput)
	{
		if (string.IsNullOrWhiteSpace(cookieInput))
		{
			return (false, Loc.T("输入的 Cookie 不能为空。"));
		}

		var account = await _streamingService.VerifyAndFetchNeteaseAccountAsync(cookieInput);
		if (account != null && account.IsLoggedIn)
		{
			_streamingService.SaveNeteaseAccount(account);
			StatusMessage = Loc.F("Cookie 校验成功！已登录网易云账号: {0}", account.Nickname);
			return (true, Loc.F("欢迎回来，{0}！", account.Nickname));
		}
		else
		{
			return (false, Loc.T("Cookie 校验失败，请检查 MUSIC_U 是否有效或是否已过期。"));
		}
	}

	public async Task<(bool success, string message)> LoginQQWithCookieAsync(string cookieInput)
	{
		if (string.IsNullOrWhiteSpace(cookieInput))
		{
			return (false, Loc.T("输入的 QQ 音乐 Cookie 不能为空。"));
		}

		var account = await _streamingService.VerifyAndFetchQQAccountAsync(cookieInput);
		if (account != null && account.IsLoggedIn)
		{
			_streamingService.SaveQQAccount(account);
			StatusMessage = Loc.F("QQ 音乐 Cookie 导入成功！已登录账号: {0} ({1})", account.Nickname, account.UserIdStr);
			return (true, Loc.F("欢迎回来，{0}！", account.Nickname));
		}
		else
		{
			return (false, Loc.T("QQ 音乐凭据校验失败，请检查 Cookie 中是否包含 uin 或 qm_keyst 凭据。"));
		}
	}

	public async Task<(bool success, string message)> LoginSpotifyWithCredentialAsync(string credentialInput)
	{
		if (string.IsNullOrWhiteSpace(credentialInput))
		{
			return (false, Loc.T("输入的 Spotify Token 或 Cookie 不能为空。"));
		}

		var account = await _streamingService.VerifyAndFetchSpotifyAccountAsync(credentialInput);
		if (account != null && account.IsLoggedIn)
		{
			_streamingService.SaveSpotifyAccount(account);
			StatusMessage = Loc.F("Spotify 凭据导入成功！已连接账号: {0}", account.Nickname);
			return (true, Loc.F("欢迎回来，{0}！", account.Nickname));
		}
		else
		{
			return (false, Loc.T("Spotify 凭证校验失败，请检查 Access Token 或 sp_dc 是否有效。"));
		}
	}

	public async Task<(bool success, string message)> RefreshAccountStatusAsync(string? platform = null)
	{
		string targetPlat = platform ?? SelectedPlatform;
		bool ok = await _streamingService.RefreshAccountStatusAsync(targetPlat);
		var acc = _streamingService.GetAccount(targetPlat);
		if (ok && acc.IsLoggedIn)
		{
			StatusMessage = Loc.F("{0} 账号状态已刷新 · {1} ({2})", Loc.T(targetPlat), acc.Nickname, Loc.T(acc.VipStatus));
			return (true, Loc.F("{0} 账号状态刷新成功！", Loc.T(targetPlat)));
		}
		else
		{
			StatusMessage = Loc.F("{0} 登录状态已失效，已退出登录。", Loc.T(targetPlat));
			return (false, Loc.F("{0} 登录已失效或未登录，请重新认证。", Loc.T(targetPlat)));
		}
	}

	public void Logout(string? platform = null)
	{
		string targetPlat = platform ?? SelectedPlatform;
		var acc = _streamingService.GetAccount(targetPlat);
		string prevNick = acc.Nickname;
		_streamingService.LogoutPlatform(targetPlat);
		StatusMessage = Loc.F("已成功退出 {0} 账号 ({1})", Loc.T(targetPlat), prevNick);
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
		if (string.IsNullOrWhiteSpace(q))
		{
			if (SelectedTab == "歌单")
			{
				await LoadUserPlaylistsAsync();
			}
			return;
		}

		int requestSeq = Interlocked.Increment(ref _searchRequestSeq);
		string targetTab = SelectedTab;
		string targetPlatform = SelectedPlatform;
		string targetQuality = SelectedQuality;
		int requestOffset = isNew ? 0 : _currentOffset;

		RunOnUi(() =>
		{
			if (requestSeq != _searchRequestSeq) return;
			SearchQuery = q;
			if (isNew)
			{
				Interlocked.Increment(ref _albumDetailRequestSeq);
				Interlocked.Increment(ref _artistDetailRequestSeq);
				_returnToArtistOnAlbumBack = false;
				IsInAlbumDetail = false;
				IsLoadingAlbumTracks = false;
				CurrentAlbumItem = null;
				AlbumTracks.Clear();
				IsInArtistDetail = false;
				IsLoadingArtistDetail = false;
				CurrentArtistItem = null;
				ArtistSongs.Clear();
				ArtistAlbums.Clear();
				_artistAllSongs.Clear();
				_artistAllAlbums.Clear();
				HasMoreArtistSongs = false;
				HasMoreArtistAlbums = false;
				Interlocked.Increment(ref _playlistDetailRequestSeq);
				IsInPlaylistDetail = false;
				IsLoadingPlaylistTracks = false;
				CurrentPlaylistItem = null;
				PlaylistSongs.Clear();
				_playlistAllSongs.Clear();
				HasMorePlaylistSongs = false;
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
			int searchType = targetTab switch
			{
				"专辑" => 10,
				"艺人" => 100,
				"歌单" => 1000,
				_ => 1
			};

			var items = await _streamingService.SearchOnlineTracksAsync(q, offset: requestOffset, limit: PageSize, searchType: searchType, platform: targetPlatform, quality: targetQuality);

			RunOnUi(() =>
			{
				if (requestSeq != _searchRequestSeq) return;
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

				_currentOffset = requestOffset + items.Count;
				string unitLabel = targetTab switch
				{
					"专辑" => Loc.T("张专辑"),
					"艺人" => Loc.T("位艺人"),
					"歌单" => Loc.T("个歌单"),
					_ => Loc.T("首高品质流媒体曲目")
				};
				StatusMessage = Loc.F("已检索到 {0} {1} · 数据来源于 {2}", Results.Count, unitLabel, Loc.T(targetPlatform));
			});
		}
		finally
		{
			RunOnUi(() =>
			{
				if (requestSeq == _searchRequestSeq)
				{
					IsSearching = false;
				}
			});
		}
	}

	public async Task LoadMoreAsync()
	{
		if (IsSearching) return;
		await SearchAsync(SearchQuery, isNew: false);
	}

	private int _playRequestSeq;
	private CancellationTokenSource? _playRequestCts;

	public async Task PlayTrackAsync(StreamingItemViewModel? item)
	{
		if (item == null) return;

		int requestSeq = Interlocked.Increment(ref _playRequestSeq);
		CancellationTokenSource cts = new CancellationTokenSource();
		CancellationTokenSource? prevCts = Interlocked.Exchange(ref _playRequestCts, cts);
		try { prevCts?.Cancel(); prevCts?.Dispose(); } catch { }
		CancellationToken ct = cts.Token;

		// 1. 若用户点击非单曲项（专辑/艺人/歌单），自动解析其首曲；杜绝直接送入解码器
		if (StreamingService.IsCollectionItem(item.RawInfo))
		{
			StatusMessage = Loc.F("正在解析《{0}》中的首首曲目...", item.Title);
			try
			{
				var firstTrack = await _streamingService.ResolveCollectionFirstTrackAsync(item.RawInfo);
				if (requestSeq != Volatile.Read(ref _playRequestSeq) || ct.IsCancellationRequested) return;
				if (firstTrack != null && !StreamingService.IsCollectionItem(firstTrack))
				{
					StatusMessage = Loc.F("已自动解析《{0}》首曲：《{1}》，准备播放...", item.Title, firstTrack.Title);
					item = new StreamingItemViewModel
					{
						DisplayIndex = item.DisplayIndex,
						Id = firstTrack.Id,
						Title = firstTrack.Title,
						ArtistAndAlbum = $"{firstTrack.Artist} / {firstTrack.Album}",
						CoverUrl = firstTrack.CoverUrl ?? item.CoverUrl,
						SourceAndQuality = firstTrack.Quality,
						DurationText = firstTrack.Duration.TotalSeconds > 0 ? firstTrack.Duration.ToString("m\\:ss") : "--:--",
						RawInfo = firstTrack
					};
				}
				else
				{
					StatusMessage = Loc.F("提示：《{0}》为集合项（专辑/艺人），未包含可解析单曲，无法作为单曲直接播放。", item.Title);
					return;
				}
			}
			catch (Exception ex)
			{
				if (requestSeq != Volatile.Read(ref _playRequestSeq) || ct.IsCancellationRequested) return;
				StatusMessage = Loc.F("解析《{0}》首曲失败: {1}，已杜绝直接送入解码器。", item.Title, ex.Message);
				return;
			}
		}

		// 2. 二次防线：若 StreamUrl 依然为非音频流的网页链接，坚决杜绝送入解码器
		if (StreamAudioCache.IsNonPlayableWebUrl(item.RawInfo.StreamUrl))
		{
			StatusMessage = Loc.F("提示：《{0}》的串流地址为网页页面，无法作为单曲直接播放。", item.Title);
			return;
		}

		// 3. 尝试解析真实高保真/VIP播放流 (携带已登录凭据认证)
		StatusMessage = Loc.F("正在解析与准备《{0}》...", item.Title);
		string resolvedStreamUrl = item.RawInfo.StreamUrl;
		try
		{
			string realStreamUrl = await _streamingService.ResolvePlayUrlAsync(item.Id, item.RawInfo.Source, ct);
			if (requestSeq != Volatile.Read(ref _playRequestSeq) || ct.IsCancellationRequested) return;
			if (!string.IsNullOrEmpty(realStreamUrl) && Uri.IsWellFormedUriString(realStreamUrl, UriKind.Absolute))
			{
				resolvedStreamUrl = realStreamUrl;
				item.RawInfo.StreamUrl = realStreamUrl;
			}
		}
		catch
		{
			if (requestSeq != Volatile.Read(ref _playRequestSeq) || ct.IsCancellationRequested) return;
		}

		// 4. 后台异步缓冲网络音频流至本地缓存（不覆盖原 URL，不阻塞 UI 线程）
		string? cachedLocalAudioPath = null;
		if (StreamAudioCache.IsNetworkUrl(resolvedStreamUrl))
		{
			if (StreamAudioCache.TryGetCachedFilePathIfReady(resolvedStreamUrl, out string instantPath) && File.Exists(instantPath))
			{
				cachedLocalAudioPath = instantPath;
			}
			else
			{
				StatusMessage = Loc.F("正在缓冲《{0}》...", item.Title);
				try
				{
					var (cached, localPath) = await StreamAudioCache.TryGetOrDownloadCachedFileAsync(resolvedStreamUrl, timeoutMs: 8000, ct);
					if (requestSeq != Volatile.Read(ref _playRequestSeq) || ct.IsCancellationRequested) return;
					if (cached && File.Exists(localPath))
					{
						cachedLocalAudioPath = localPath;
					}
				}
				catch
				{
					if (requestSeq != Volatile.Read(ref _playRequestSeq) || ct.IsCancellationRequested) return;
				}
			}
		}

		if (requestSeq != Volatile.Read(ref _playRequestSeq) || ct.IsCancellationRequested) return;

		string coverDir = _library?.CoverDirectory ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "FurinaPlayer", "covers");
		try { Directory.CreateDirectory(coverDir); } catch { }
		string coverFile = Path.Combine(coverDir, $"stream_{Math.Abs(item.Id)}.jpg");

		string? initialCover = File.Exists(coverFile) ? coverFile : (!string.IsNullOrEmpty(item.CoverUrl) ? item.CoverUrl : null);
		Track track = _streamingService.ToTrack(item.RawInfo, initialCover);
		if (!string.IsNullOrEmpty(cachedLocalAudioPath))
		{
			track.FilePath = cachedLocalAudioPath;
		}

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
						var bytes = await hc.GetByteArrayAsync(item.CoverUrl, ct);
						await File.WriteAllBytesAsync(coverFile, bytes, ct);
						track.CoverPath = coverFile;
						if (_engine.CurrentTrack?.Id == track.Id)
						{
							_engine.CurrentTrack.CoverPath = coverFile;
						}
					}
					catch { }
				}, ct);
			}
		}

		await _engine.PlayTrackAsync(track, ct);
		if (requestSeq == Volatile.Read(ref _playRequestSeq) && !ct.IsCancellationRequested && _engine.State != FurinaPlayer.Audio.PlaybackState.Error)
		{
			StatusMessage = Loc.F("正在播放《{0}》 - {1}", track.Title, track.Artist);
		}
	}

	public async Task AddToQueueAsync(StreamingItemViewModel? item)
	{
		if (item == null) return;

		// 若为合集项，尝试解析首曲后再加入队列
		if (StreamingService.IsCollectionItem(item.RawInfo))
		{
			try
			{
				var firstTrack = await _streamingService.ResolveCollectionFirstTrackAsync(item.RawInfo);
				if (firstTrack != null && !StreamingService.IsCollectionItem(firstTrack))
				{
					item = new StreamingItemViewModel
					{
						DisplayIndex = item.DisplayIndex,
						Id = firstTrack.Id,
						Title = firstTrack.Title,
						ArtistAndAlbum = $"{firstTrack.Artist} / {firstTrack.Album}",
						CoverUrl = firstTrack.CoverUrl ?? item.CoverUrl,
						SourceAndQuality = firstTrack.Quality,
						DurationText = firstTrack.Duration.TotalSeconds > 0 ? firstTrack.Duration.ToString("m\\:ss") : "--:--",
						RawInfo = firstTrack
					};
				}
				else
				{
					StatusMessage = Loc.F("提示：《{0}》为集合项，无法直接加入单曲播放队列。", item.Title);
					return;
				}
			}
			catch
			{
				StatusMessage = Loc.F("提示：《{0}》为集合项，解析首曲失败，无法加入队列。", item.Title);
				return;
			}
		}

		if (StreamAudioCache.IsNonPlayableWebUrl(item.RawInfo.StreamUrl))
		{
			StatusMessage = Loc.F("提示：《{0}》为网页地址，无法作为单曲加入播放队列。", item.Title);
			return;
		}

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
		StatusMessage = Loc.F("已将《{0}》加入播放队列", item.Title);
		await Task.CompletedTask;
	}

	public async Task DownloadTrackAsync(StreamingItemViewModel? item)
	{
		if (item == null) return;

		// 若为合集项，尝试解析首曲后再下载
		if (StreamingService.IsCollectionItem(item.RawInfo))
		{
			try
			{
				var firstTrack = await _streamingService.ResolveCollectionFirstTrackAsync(item.RawInfo);
				if (firstTrack != null && !StreamingService.IsCollectionItem(firstTrack))
				{
					item = new StreamingItemViewModel
					{
						DisplayIndex = item.DisplayIndex,
						Id = firstTrack.Id,
						Title = firstTrack.Title,
						ArtistAndAlbum = $"{firstTrack.Artist} / {firstTrack.Album}",
						CoverUrl = firstTrack.CoverUrl ?? item.CoverUrl,
						SourceAndQuality = firstTrack.Quality,
						DurationText = firstTrack.Duration.TotalSeconds > 0 ? firstTrack.Duration.ToString("m\\:ss") : "--:--",
						RawInfo = firstTrack
					};
				}
				else
				{
					StatusMessage = Loc.F("提示：《{0}》为集合项，无法直接下载为单曲文件。", item.Title);
					return;
				}
			}
			catch
			{
				StatusMessage = Loc.F("提示：《{0}》为集合项，解析首曲失败，无法下载。", item.Title);
				return;
			}
		}

		if (string.IsNullOrEmpty(item.RawInfo.StreamUrl) || StreamAudioCache.IsNonPlayableWebUrl(item.RawInfo.StreamUrl))
		{
			StatusMessage = Loc.F("提示：《{0}》的串流地址非有效音频流，无法直接下载。", item.Title);
			return;
		}

		try
		{
			StatusMessage = Loc.F("正在缓冲与下载《{0}》...", item.Title);
			string musicDir = Environment.GetFolderPath(Environment.SpecialFolder.MyMusic);
			string targetFile = Path.Combine(musicDir, $"{item.RawInfo.Artist} - {item.Title}.mp3");

			using var hc = new HttpClient { Timeout = TimeSpan.FromMinutes(2) };
			var bytes = await hc.GetByteArrayAsync(item.RawInfo.StreamUrl);
			await File.WriteAllBytesAsync(targetFile, bytes);

			if (_library != null)
			{
				await _library.ImportFileAsync(targetFile);
			}
			StatusMessage = Loc.F("《{0}》已成功缓存至本地音乐库！", item.Title);
		}
		catch (Exception ex)
		{
			StatusMessage = Loc.F("下载失败: {0}", ex.Message);
		}
	}

	private RelayCommand? _clearRecentSearchesCommand;
	public RelayCommand ClearRecentSearchesCommand => _clearRecentSearchesCommand ??= new RelayCommand(ClearRecentSearches);

	private RelayCommand? _loadMoreCommand;
	public RelayCommand LoadMoreCommand => _loadMoreCommand ??= new RelayCommand(() => _ = LoadMoreAsync());

	private int _albumDetailRequestSeq;

	private static StreamingItemViewModel MapToItemViewModel(StreamingTrackInfo info, int displayIndex, string? fallbackCoverUrl = null)
	{
		return new StreamingItemViewModel
		{
			DisplayIndex = displayIndex,
			Id = info.Id,
			Title = info.Title,
			ArtistAndAlbum = $"{info.Artist} / {info.Album}",
			CoverUrl = !string.IsNullOrWhiteSpace(info.CoverUrl) ? info.CoverUrl : fallbackCoverUrl,
			SourceAndQuality = info.Quality,
			DurationText = info.Duration.TotalSeconds > 0 ? info.Duration.ToString("m\\:ss") : "--:--",
			RawInfo = info
		};
	}

	private Track BuildTrackFromStreamingInfo(StreamingTrackInfo info, string? fallbackCoverUrl = null)
	{
		string coverDir = _library?.CoverDirectory ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "FurinaPlayer", "covers");
		try { Directory.CreateDirectory(coverDir); } catch { }
		string coverFile = Path.Combine(coverDir, $"stream_{Math.Abs(info.Id)}.jpg");
		string? initialCover = File.Exists(coverFile)
			? coverFile
			: (!string.IsNullOrEmpty(info.CoverUrl) ? info.CoverUrl : fallbackCoverUrl);

		Track track = _streamingService.ToTrack(info, initialCover);
		if (string.IsNullOrWhiteSpace(track.Title)) track.Title = info.Title;
		if (string.IsNullOrWhiteSpace(track.Artist)) track.Artist = info.Artist;
		if (string.IsNullOrWhiteSpace(track.Album)) track.Album = info.Album;
		if (track.DurationMilliseconds <= 0 && info.Duration.TotalMilliseconds > 0)
		{
			track.DurationMilliseconds = (long)info.Duration.TotalMilliseconds;
		}
		return track;
	}

	/// <summary>
	/// 进入选定专辑的二级菜单，加载并展示专辑内曲目列表
	/// </summary>
	public async Task OpenAlbumAsync(StreamingItemViewModel? albumItem)
	{
		if (albumItem == null) return;

		int requestSeq = Interlocked.Increment(ref _albumDetailRequestSeq);
		RunOnUi(() =>
		{
			if (requestSeq != _albumDetailRequestSeq) return;
			CurrentAlbumItem = albumItem;
			IsInAlbumDetail = true;
			IsLoadingAlbumTracks = true;
			AlbumTracks.Clear();
			StatusMessage = Loc.F("正在加载专辑《{0}》内的曲目...", albumItem.Title);
		});

		try
		{
			var tracks = await _streamingService.GetAlbumTracksAsync(albumItem.RawInfo);
			RunOnUi(() =>
			{
				if (requestSeq != _albumDetailRequestSeq || !IsInAlbumDetail) return;
				AlbumTracks.Clear();
				int idx = 1;
				foreach (var t in tracks)
				{
					AlbumTracks.Add(MapToItemViewModel(t, idx++, albumItem.CoverUrl));
				}

				StatusMessage = AlbumTracks.Count > 0
					? Loc.F("专辑《{0}》共 {1} 首曲目 · {2}", albumItem.Title, AlbumTracks.Count, Loc.T(albumItem.RawInfo.Source))
					: Loc.F("未能获取到专辑《{0}》内的曲目，请检查网络或切换音源。", albumItem.Title);
			});
		}
		catch (Exception ex)
		{
			RunOnUi(() =>
			{
				if (requestSeq != _albumDetailRequestSeq) return;
				StatusMessage = Loc.F("加载专辑《{0}》曲目失败: {1}", albumItem.Title, ex.Message);
			});
		}
		finally
		{
			RunOnUi(() =>
			{
				if (requestSeq == _albumDetailRequestSeq)
				{
					IsLoadingAlbumTracks = false;
				}
			});
		}
	}

	/// <summary>
	/// 退出专辑二级菜单，返回一级专辑搜索结果列表
	/// </summary>
	public void BackToAlbumList()
	{
		Interlocked.Increment(ref _albumDetailRequestSeq);
		RunOnUi(() =>
		{
			IsInAlbumDetail = false;
			IsLoadingAlbumTracks = false;
			CurrentAlbumItem = null;
			AlbumTracks.Clear();
			if (_returnToArtistOnAlbumBack && CurrentArtistItem != null)
			{
				_returnToArtistOnAlbumBack = false;
				IsInArtistDetail = true;
				StatusMessage = Loc.F("艺人《{0}》· {1} 首歌曲 · {2} 张专辑",
					CurrentArtistItem.Title, _artistAllSongs.Count, _artistAllAlbums.Count);
				return;
			}

			string unitLabel = SelectedTab switch
			{
				"专辑" => Loc.T("张专辑"),
				"艺人" => Loc.T("位艺人"),
				"歌单" => Loc.T("个歌单"),
				_ => Loc.T("首高品质流媒体曲目")
			};
			if (Results.Count > 0)
			{
				StatusMessage = Loc.F("已检索到 {0} {1} · 数据来源于 {2}", Results.Count, unitLabel, Loc.T(SelectedPlatform));
			}
		});
	}

	private async Task<List<StreamingItemViewModel>> ResolveAlbumTrackItemsAsync(StreamingItemViewModel? albumItem)
	{
		if (albumItem == null)
		{
			return AlbumTracks.ToList();
		}

		if (CurrentAlbumItem != null && CurrentAlbumItem.Id == albumItem.Id && AlbumTracks.Count > 0)
		{
			return AlbumTracks.ToList();
		}

		var rawTracks = await _streamingService.GetAlbumTracksAsync(albumItem.RawInfo);
		var mapped = new List<StreamingItemViewModel>(rawTracks.Count);
		int idx = 1;
		foreach (var t in rawTracks)
		{
			mapped.Add(MapToItemViewModel(t, idx++, albumItem.CoverUrl));
		}
		return mapped;
	}

	/// <summary>
	/// 以整个选定专辑内的音乐作为播放队列进行播放（可指定起始曲目索引）
	/// </summary>
	public async Task PlayAlbumAllAsync(StreamingItemViewModel? albumItem = null, int startIndex = 0)
	{
		var targetAlbum = albumItem ?? CurrentAlbumItem;
		string albumTitle = targetAlbum?.Title ?? Loc.T("当前专辑");

		StatusMessage = Loc.F("正在准备专辑《{0}》播放队列...", albumTitle);
		var items = await ResolveAlbumTrackItemsAsync(albumItem);
		if (items.Count == 0)
		{
			StatusMessage = Loc.F("未能获取到专辑《{0}》内的曲目，无法创建播放队列。", albumTitle);
			return;
		}

		int clampedIndex = Math.Clamp(startIndex, 0, items.Count - 1);
		var queueTracks = new List<Track>(items.Count);
		foreach (var item in items)
		{
			queueTracks.Add(BuildTrackFromStreamingInfo(item.RawInfo, targetAlbum?.CoverUrl));
		}

		_engine.SetQueue(queueTracks, clampedIndex);
		var firstItem = items[clampedIndex];
		await PlayTrackAsync(firstItem);
		StatusMessage = Loc.F("正在播放专辑《{0}》队列（共 {1} 首，当前第 {2} 首：《{3}》）", albumTitle, items.Count, clampedIndex + 1, firstItem.Title);
	}

	/// <summary>
	/// 将选定专辑内的全部音乐追加到当前播放队列
	/// </summary>
	public async Task AddAlbumToQueueAsync(StreamingItemViewModel? albumItem = null)
	{
		var targetAlbum = albumItem ?? CurrentAlbumItem;
		string albumTitle = targetAlbum?.Title ?? Loc.T("当前专辑");

		var items = await ResolveAlbumTrackItemsAsync(albumItem);
		if (items.Count == 0)
		{
			StatusMessage = Loc.F("未能获取到专辑《{0}》内的曲目，无法加入播放队列。", albumTitle);
			return;
		}

		var currentQueue = _engine.Queue.ToList();
		foreach (var item in items)
		{
			currentQueue.Add(BuildTrackFromStreamingInfo(item.RawInfo, targetAlbum?.CoverUrl));
		}
		_engine.SetQueue(currentQueue, _engine.QueueIndex >= 0 ? _engine.QueueIndex : 0);
		StatusMessage = Loc.F("已将专辑《{0}》全部 {1} 首曲目加入播放队列", albumTitle, items.Count);
	}

	private int _artistDetailRequestSeq;
	private bool _returnToArtistOnAlbumBack;
	private const int ArtistPageChunkSize = 20;
	private int _artistVisibleSongLimit = ArtistPageChunkSize;
	private int _artistVisibleAlbumLimit = ArtistPageChunkSize;
	private bool _artistSongsServerHasMore;
	private bool _artistAlbumsServerHasMore;
	private readonly List<StreamingItemViewModel> _artistAllSongs = new();
	private readonly List<StreamingItemViewModel> _artistAllAlbums = new();

	public bool CanReturnToArtistFromAlbum => _returnToArtistOnAlbumBack && CurrentArtistItem != null;
	public int ArtistTotalSongsLoaded => _artistAllSongs.Count;
	public int ArtistTotalAlbumsLoaded => _artistAllAlbums.Count;

	private void SyncVisibleArtistCollections()
	{
		ArtistSongs.Clear();
		foreach (var s in _artistAllSongs.Take(_artistVisibleSongLimit))
		{
			ArtistSongs.Add(s);
		}
		HasMoreArtistSongs = _artistAllSongs.Count > ArtistSongs.Count || _artistSongsServerHasMore;

		ArtistAlbums.Clear();
		foreach (var a in _artistAllAlbums.Take(_artistVisibleAlbumLimit))
		{
			ArtistAlbums.Add(a);
		}
		HasMoreArtistAlbums = _artistAllAlbums.Count > ArtistAlbums.Count || _artistAlbumsServerHasMore;
	}

	/// <summary>
	/// 进入选定艺人的二级菜单，并行加载该艺人的歌曲与专辑列表（默认各展示 20 条，支持“显示更多...”）
	/// </summary>
	public async Task OpenArtistAsync(StreamingItemViewModel? artistItem)
	{
		if (artistItem == null) return;

		int requestSeq = Interlocked.Increment(ref _artistDetailRequestSeq);
		RunOnUi(() =>
		{
			if (requestSeq != _artistDetailRequestSeq) return;
			_returnToArtistOnAlbumBack = false;
			IsInAlbumDetail = false;
			CurrentAlbumItem = null;
			AlbumTracks.Clear();

			CurrentArtistItem = artistItem;
			IsInArtistDetail = true;
			IsLoadingArtistDetail = true;
			_artistVisibleSongLimit = ArtistPageChunkSize;
			_artistVisibleAlbumLimit = ArtistPageChunkSize;
			_artistSongsServerHasMore = false;
			_artistAlbumsServerHasMore = false;
			_artistAllSongs.Clear();
			_artistAllAlbums.Clear();
			ArtistSongs.Clear();
			ArtistAlbums.Clear();
			HasMoreArtistSongs = false;
			HasMoreArtistAlbums = false;
			StatusMessage = Loc.F("正在加载艺人《{0}》的歌曲与专辑...", artistItem.Title);
		});

		try
		{
			var songsTask = _streamingService.GetArtistSongsAsync(artistItem.RawInfo, offset: 0, limit: 40);
			var albumsTask = _streamingService.GetArtistAlbumsAsync(artistItem.RawInfo, offset: 0, limit: 40);
			await Task.WhenAll(songsTask, albumsTask);

			var (songs, songsMore) = await songsTask;
			var (albums, albumsMore) = await albumsTask;

			RunOnUi(() =>
			{
				if (requestSeq != _artistDetailRequestSeq || !IsInArtistDetail) return;

				_artistAllSongs.Clear();
				int sIdx = 1;
				foreach (var s in songs)
				{
					_artistAllSongs.Add(MapToItemViewModel(s, sIdx++, artistItem.CoverUrl));
				}
				_artistSongsServerHasMore = songsMore;

				_artistAllAlbums.Clear();
				int aIdx = 1;
				foreach (var a in albums)
				{
					_artistAllAlbums.Add(MapToItemViewModel(a, aIdx++, artistItem.CoverUrl));
				}
				_artistAlbumsServerHasMore = albumsMore;

				SyncVisibleArtistCollections();
				StatusMessage = (_artistAllSongs.Count > 0 || _artistAllAlbums.Count > 0)
					? Loc.F("艺人《{0}》· {1} 首歌曲 · {2} 张专辑", artistItem.Title, _artistAllSongs.Count, _artistAllAlbums.Count)
					: Loc.F("未能获取到艺人《{0}》的歌曲或专辑，请检查网络或切换音源。", artistItem.Title);
			});
		}
		catch (Exception ex)
		{
			RunOnUi(() =>
			{
				if (requestSeq != _artistDetailRequestSeq) return;
				StatusMessage = Loc.F("加载艺人《{0}》内容失败: {1}", artistItem.Title, ex.Message);
			});
		}
		finally
		{
			RunOnUi(() =>
			{
				if (requestSeq == _artistDetailRequestSeq)
				{
					IsLoadingArtistDetail = false;
				}
			});
		}
	}

	/// <summary>
	/// 艺人二级菜单 - 歌曲区点击「显示更多...」，展开/加载更多歌曲
	/// </summary>
	public async Task LoadMoreArtistSongsAsync()
	{
		if (CurrentArtistItem == null || IsLoadingMoreArtistSongs) return;

		int requestSeq = _artistDetailRequestSeq;
		IsLoadingMoreArtistSongs = true;
		try
		{
			_artistVisibleSongLimit += ArtistPageChunkSize;
			if (_artistAllSongs.Count < _artistVisibleSongLimit && _artistSongsServerHasMore)
			{
				var (moreSongs, hasMore) = await _streamingService.GetArtistSongsAsync(
					CurrentArtistItem.RawInfo,
					offset: _artistAllSongs.Count,
					limit: 40);

				if (requestSeq != _artistDetailRequestSeq) return;

				var existingIds = new HashSet<long>(_artistAllSongs.Select(x => x.Id));
				int nextIdx = _artistAllSongs.Count + 1;
				int added = 0;
				foreach (var s in moreSongs)
				{
					if (existingIds.Add(s.Id))
					{
						_artistAllSongs.Add(MapToItemViewModel(s, nextIdx++, CurrentArtistItem.CoverUrl));
						added++;
					}
				}
				_artistSongsServerHasMore = hasMore && added > 0;
			}

			RunOnUi(() =>
			{
				if (requestSeq != _artistDetailRequestSeq) return;
				SyncVisibleArtistCollections();
			});
		}
		finally
		{
			RunOnUi(() => IsLoadingMoreArtistSongs = false);
		}
	}

	/// <summary>
	/// 艺人二级菜单 - 专辑区点击「显示更多...」，展开/加载更多专辑
	/// </summary>
	public async Task LoadMoreArtistAlbumsAsync()
	{
		if (CurrentArtistItem == null || IsLoadingMoreArtistAlbums) return;

		int requestSeq = _artistDetailRequestSeq;
		IsLoadingMoreArtistAlbums = true;
		try
		{
			_artistVisibleAlbumLimit += ArtistPageChunkSize;
			if (_artistAllAlbums.Count < _artistVisibleAlbumLimit && _artistAlbumsServerHasMore)
			{
				var (moreAlbums, hasMore) = await _streamingService.GetArtistAlbumsAsync(
					CurrentArtistItem.RawInfo,
					offset: _artistAllAlbums.Count,
					limit: 40);

				if (requestSeq != _artistDetailRequestSeq) return;

				var existingIds = new HashSet<long>(_artistAllAlbums.Select(x => x.Id));
				int nextIdx = _artistAllAlbums.Count + 1;
				int added = 0;
				foreach (var a in moreAlbums)
				{
					if (existingIds.Add(a.Id))
					{
						_artistAllAlbums.Add(MapToItemViewModel(a, nextIdx++, CurrentArtistItem.CoverUrl));
						added++;
					}
				}
				_artistAlbumsServerHasMore = hasMore && added > 0;
			}

			RunOnUi(() =>
			{
				if (requestSeq != _artistDetailRequestSeq) return;
				SyncVisibleArtistCollections();
			});
		}
		finally
		{
			RunOnUi(() => IsLoadingMoreArtistAlbums = false);
		}
	}

	/// <summary>
	/// 从艺人二级菜单中点击某张专辑进入专辑曲目列表（返回时可回到艺人页）
	/// </summary>
	public async Task OpenAlbumFromArtistAsync(StreamingItemViewModel? albumItem)
	{
		if (albumItem == null) return;
		_returnToArtistOnAlbumBack = true;
		IsInArtistDetail = false;
		await OpenAlbumAsync(albumItem);
	}

	/// <summary>
	/// 退出艺人二级菜单，返回一级艺人搜索结果列表
	/// </summary>
	public void BackToArtistList()
	{
		Interlocked.Increment(ref _artistDetailRequestSeq);
		_returnToArtistOnAlbumBack = false;
		RunOnUi(() =>
		{
			IsInArtistDetail = false;
			IsLoadingArtistDetail = false;
			IsLoadingMoreArtistSongs = false;
			IsLoadingMoreArtistAlbums = false;
			CurrentArtistItem = null;
			_artistAllSongs.Clear();
			_artistAllAlbums.Clear();
			ArtistSongs.Clear();
			ArtistAlbums.Clear();
			HasMoreArtistSongs = false;
			HasMoreArtistAlbums = false;

			string unitLabel = SelectedTab switch
			{
				"专辑" => Loc.T("张专辑"),
				"艺人" => Loc.T("位艺人"),
				"歌单" => Loc.T("个歌单"),
				_ => Loc.T("首高品质流媒体曲目")
			};
			if (Results.Count > 0)
			{
				StatusMessage = Loc.F("已检索到 {0} {1} · 数据来源于 {2}", Results.Count, unitLabel, Loc.T(SelectedPlatform));
			}
		});
	}

	private async Task<List<StreamingItemViewModel>> ResolveArtistSongItemsAsync(StreamingItemViewModel? artistItem)
	{
		if (artistItem == null)
		{
			return _artistAllSongs.ToList();
		}

		if (CurrentArtistItem != null && CurrentArtistItem.Id == artistItem.Id && _artistAllSongs.Count > 0)
		{
			return _artistAllSongs.ToList();
		}

		var (rawTracks, _) = await _streamingService.GetArtistSongsAsync(artistItem.RawInfo, offset: 0, limit: 50);
		var mapped = new List<StreamingItemViewModel>(rawTracks.Count);
		int idx = 1;
		foreach (var t in rawTracks)
		{
			mapped.Add(MapToItemViewModel(t, idx++, artistItem.CoverUrl));
		}
		return mapped;
	}

	/// <summary>
	/// 以选定艺人的所有歌曲搜索结果作为播放队列进行播放
	/// </summary>
	public async Task PlayArtistAllAsync(StreamingItemViewModel? artistItem = null, int startIndex = 0)
	{
		var targetArtist = artistItem ?? CurrentArtistItem;
		string artistName = targetArtist?.Title ?? Loc.T("当前艺人");

		StatusMessage = Loc.F("正在准备艺人《{0}》歌曲播放队列...", artistName);
		var items = await ResolveArtistSongItemsAsync(artistItem);
		if (items.Count == 0)
		{
			StatusMessage = Loc.F("未能获取到艺人《{0}》下的歌曲，无法创建播放队列。", artistName);
			return;
		}

		int clampedIndex = Math.Clamp(startIndex, 0, items.Count - 1);
		var queueTracks = new List<Track>(items.Count);
		foreach (var item in items)
		{
			queueTracks.Add(BuildTrackFromStreamingInfo(item.RawInfo, targetArtist?.CoverUrl));
		}

		_engine.SetQueue(queueTracks, clampedIndex);
		var firstItem = items[clampedIndex];
		await PlayTrackAsync(firstItem);
		StatusMessage = Loc.F("正在播放艺人《{0}》全部歌曲队列（共 {1} 首，当前第 {2} 首：《{3}》）", artistName, items.Count, clampedIndex + 1, firstItem.Title);
	}

	/// <summary>
	/// 将选定艺人下的全部歌曲追加到当前播放队列
	/// </summary>
	public async Task AddArtistSongsToQueueAsync(StreamingItemViewModel? artistItem = null)
	{
		var targetArtist = artistItem ?? CurrentArtistItem;
		string artistName = targetArtist?.Title ?? Loc.T("当前艺人");

		var items = await ResolveArtistSongItemsAsync(artistItem);
		if (items.Count == 0)
		{
			StatusMessage = Loc.F("未能获取到艺人《{0}》下的歌曲，无法加入播放队列。", artistName);
			return;
		}

		var currentQueue = _engine.Queue.ToList();
		foreach (var item in items)
		{
			currentQueue.Add(BuildTrackFromStreamingInfo(item.RawInfo, targetArtist?.CoverUrl));
		}
		_engine.SetQueue(currentQueue, _engine.QueueIndex >= 0 ? _engine.QueueIndex : 0);
		StatusMessage = Loc.F("已将艺人《{0}》全部 {1} 首歌曲加入播放队列", artistName, items.Count);
	}

	private int _playlistDetailRequestSeq;
	private int _playlistVisibleSongLimit = ArtistPageChunkSize;
	private readonly List<StreamingItemViewModel> _playlistAllSongs = new();
	public int PlaylistTotalSongsLoaded => _playlistAllSongs.Count;

	private void SyncVisiblePlaylistSongs()
	{
		PlaylistSongs.Clear();
		foreach (var s in _playlistAllSongs.Take(_playlistVisibleSongLimit))
		{
			PlaylistSongs.Add(s);
		}
		HasMorePlaylistSongs = _playlistAllSongs.Count > PlaylistSongs.Count;
	}

	/// <summary>
	/// 当在歌单页未输入搜索词时，默认加载当前已登录用户的自建与收藏歌单
	/// </summary>
	public async Task LoadUserPlaylistsAsync()
	{
		int requestSeq = Interlocked.Increment(ref _searchRequestSeq);
		string targetPlatform = SelectedPlatform;

		RunOnUi(() =>
		{
			if (requestSeq != _searchRequestSeq) return;
			Interlocked.Increment(ref _albumDetailRequestSeq);
			Interlocked.Increment(ref _artistDetailRequestSeq);
			Interlocked.Increment(ref _playlistDetailRequestSeq);
			_returnToArtistOnAlbumBack = false;
			IsInAlbumDetail = false;
			IsInArtistDetail = false;
			IsInPlaylistDetail = false;
			CurrentAlbumItem = null;
			CurrentArtistItem = null;
			CurrentPlaylistItem = null;
			AlbumTracks.Clear();
			ArtistSongs.Clear();
			ArtistAlbums.Clear();
			PlaylistSongs.Clear();
			_playlistAllSongs.Clear();
			HasMorePlaylistSongs = false;
			_currentOffset = 0;
			IsSearching = true;
		});

		try
		{
			var items = await _streamingService.GetUserPlaylistsAsync(targetPlatform);
			RunOnUi(() =>
			{
				if (requestSeq != _searchRequestSeq) return;
				Results.Clear();
				int idx = 1;
				foreach (var item in items)
				{
					Results.Add(MapToItemViewModel(item, idx++));
				}

				bool loggedIn = _streamingService.GetAccount(targetPlatform).IsLoggedIn;
				if (!loggedIn)
				{
					StatusMessage = Loc.F("未登录 {0}，请先登录账号查看自建歌单，或输入关键词搜索歌单", Loc.T(targetPlatform));
				}
				else if (Results.Count > 0)
				{
					StatusMessage = Loc.F("已加载当前账号 {0} 个自建与收藏歌单 · 数据来源于 {1}", Results.Count, Loc.T(targetPlatform));
				}
				else
				{
					StatusMessage = Loc.F("当前账号暂无自建歌单，可输入关键词搜索全网歌单 · {0}", Loc.T(targetPlatform));
				}
			});
		}
		finally
		{
			RunOnUi(() =>
			{
				if (requestSeq == _searchRequestSeq)
				{
					IsSearching = false;
				}
			});
		}
	}

	/// <summary>
	/// 进入选定歌单的二级菜单，加载并展示歌单内曲目列表（默认展示 20 条，支持「显示更多...」）
	/// </summary>
	public async Task OpenPlaylistAsync(StreamingItemViewModel? playlistItem)
	{
		if (playlistItem == null) return;

		int requestSeq = Interlocked.Increment(ref _playlistDetailRequestSeq);
		RunOnUi(() =>
		{
			if (requestSeq != _playlistDetailRequestSeq) return;
			_returnToArtistOnAlbumBack = false;
			IsInAlbumDetail = false;
			IsInArtistDetail = false;
			CurrentPlaylistItem = playlistItem;
			IsInPlaylistDetail = true;
			IsLoadingPlaylistTracks = true;
			_playlistVisibleSongLimit = ArtistPageChunkSize;
			_playlistAllSongs.Clear();
			PlaylistSongs.Clear();
			HasMorePlaylistSongs = false;
			StatusMessage = Loc.F("正在加载歌单《{0}》内的曲目...", playlistItem.Title);
		});

		try
		{
			var tracks = await _streamingService.GetPlaylistTracksAsync(playlistItem.RawInfo);
			RunOnUi(() =>
			{
				if (requestSeq != _playlistDetailRequestSeq || !IsInPlaylistDetail) return;
				_playlistAllSongs.Clear();
				int idx = 1;
				foreach (var t in tracks)
				{
					_playlistAllSongs.Add(MapToItemViewModel(t, idx++, playlistItem.CoverUrl));
				}
				SyncVisiblePlaylistSongs();

				StatusMessage = _playlistAllSongs.Count > 0
					? Loc.F("歌单《{0}》共 {1} 首曲目 · {2}", playlistItem.Title, _playlistAllSongs.Count, Loc.T(playlistItem.RawInfo.Source))
					: Loc.F("未能获取到歌单《{0}》内的曲目，请检查网络或切换音源。", playlistItem.Title);
			});
		}
		catch (Exception ex)
		{
			RunOnUi(() =>
			{
				if (requestSeq != _playlistDetailRequestSeq) return;
				StatusMessage = Loc.F("加载歌单《{0}》曲目失败: {1}", playlistItem.Title, ex.Message);
			});
		}
		finally
		{
			RunOnUi(() =>
			{
				if (requestSeq == _playlistDetailRequestSeq)
				{
					IsLoadingPlaylistTracks = false;
				}
			});
		}
	}

	/// <summary>
	/// 歌单二级菜单点击「显示更多...」，每次展开剩余 20 条曲目
	/// </summary>
	public Task LoadMorePlaylistSongsAsync()
	{
		if (CurrentPlaylistItem == null || IsLoadingMorePlaylistSongs) return Task.CompletedTask;
		IsLoadingMorePlaylistSongs = true;
		try
		{
			_playlistVisibleSongLimit += ArtistPageChunkSize;
			RunOnUi(SyncVisiblePlaylistSongs);
		}
		finally
		{
			RunOnUi(() => IsLoadingMorePlaylistSongs = false);
		}
		return Task.CompletedTask;
	}

	/// <summary>
	/// 退出歌单二级菜单，返回一级歌单列表
	/// </summary>
	public void BackToPlaylistList()
	{
		Interlocked.Increment(ref _playlistDetailRequestSeq);
		RunOnUi(() =>
		{
			IsInPlaylistDetail = false;
			IsLoadingPlaylistTracks = false;
			IsLoadingMorePlaylistSongs = false;
			CurrentPlaylistItem = null;
			_playlistAllSongs.Clear();
			PlaylistSongs.Clear();
			HasMorePlaylistSongs = false;

			if (Results.Count > 0)
			{
				StatusMessage = Loc.F("已检索到 {0} {1} · 数据来源于 {2}", Results.Count, Loc.T("个歌单"), Loc.T(SelectedPlatform));
			}
		});
	}

	private async Task<List<StreamingItemViewModel>> ResolvePlaylistSongItemsAsync(StreamingItemViewModel? playlistItem)
	{
		if (playlistItem == null)
		{
			return _playlistAllSongs.ToList();
		}

		if (CurrentPlaylistItem != null && CurrentPlaylistItem.Id == playlistItem.Id && _playlistAllSongs.Count > 0)
		{
			return _playlistAllSongs.ToList();
		}

		var rawTracks = await _streamingService.GetPlaylistTracksAsync(playlistItem.RawInfo);
		var mapped = new List<StreamingItemViewModel>(rawTracks.Count);
		int idx = 1;
		foreach (var t in rawTracks)
		{
			mapped.Add(MapToItemViewModel(t, idx++, playlistItem.CoverUrl));
		}
		return mapped;
	}

	/// <summary>
	/// 以选定歌单内所有音乐作为播放队列进行播放
	/// </summary>
	public async Task PlayPlaylistAllAsync(StreamingItemViewModel? playlistItem = null, int startIndex = 0)
	{
		var targetPlaylist = playlistItem ?? CurrentPlaylistItem;
		string playlistTitle = targetPlaylist?.Title ?? Loc.T("当前歌单");

		StatusMessage = Loc.F("正在准备歌单《{0}》播放队列...", playlistTitle);
		var items = await ResolvePlaylistSongItemsAsync(playlistItem);
		if (items.Count == 0)
		{
			StatusMessage = Loc.F("未能获取到歌单《{0}》内的曲目，无法创建播放队列。", playlistTitle);
			return;
		}

		int clampedIndex = Math.Clamp(startIndex, 0, items.Count - 1);
		var queueTracks = new List<Track>(items.Count);
		foreach (var item in items)
		{
			queueTracks.Add(BuildTrackFromStreamingInfo(item.RawInfo, targetPlaylist?.CoverUrl));
		}

		_engine.SetQueue(queueTracks, clampedIndex);
		var firstItem = items[clampedIndex];
		await PlayTrackAsync(firstItem);
		StatusMessage = Loc.F("正在播放歌单《{0}》队列（共 {1} 首，当前第 {2} 首：《{3}》）", playlistTitle, items.Count, clampedIndex + 1, firstItem.Title);
	}

	/// <summary>
	/// 将选定歌单内的全部音乐追加到当前播放队列
	/// </summary>
	public async Task AddPlaylistSongsToQueueAsync(StreamingItemViewModel? playlistItem = null)
	{
		var targetPlaylist = playlistItem ?? CurrentPlaylistItem;
		string playlistTitle = targetPlaylist?.Title ?? Loc.T("当前歌单");

		var items = await ResolvePlaylistSongItemsAsync(playlistItem);
		if (items.Count == 0)
		{
			StatusMessage = Loc.F("未能获取到歌单《{0}》内的曲目，无法加入播放队列。", playlistTitle);
			return;
		}

		var currentQueue = _engine.Queue.ToList();
		foreach (var item in items)
		{
			currentQueue.Add(BuildTrackFromStreamingInfo(item.RawInfo, targetPlaylist?.CoverUrl));
		}
		_engine.SetQueue(currentQueue, _engine.QueueIndex >= 0 ? _engine.QueueIndex : 0);
		StatusMessage = Loc.F("已将歌单《{0}》全部 {1} 首曲目加入播放队列", playlistTitle, items.Count);
	}

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
		if (IsInAlbumDetail)
		{
			_returnToArtistOnAlbumBack = false;
			BackToAlbumList();
		}
		if (IsInArtistDetail)
		{
			BackToArtistList();
		}
		if (IsInPlaylistDetail)
		{
			BackToPlaylistList();
		}
		_ = SearchAsync(SearchQuery, isNew: true);
	}
}
