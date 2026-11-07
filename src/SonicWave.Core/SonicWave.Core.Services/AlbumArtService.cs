using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using SonicWave.Core.Models;

namespace SonicWave.Core.Services;

/// <summary>
/// 智能网络专辑封面匹配与检索服务
/// 支持多源在线检索：网易云音乐 (NetEase Cloud Music) 高清原画 + Apple iTunes + QQ音乐
/// 针对二次元、动漫、游戏配乐 (HOYO-MiX / 原神 / 崩铁) 与华语/流行音乐全面覆盖
/// </summary>
public sealed class AlbumArtService
{
	private static readonly HttpClient Http = new HttpClient(new SocketsHttpHandler
	{
		PooledConnectionLifetime = TimeSpan.FromMinutes(5),
		AutomaticDecompression = System.Net.DecompressionMethods.All
	})
	{
		Timeout = TimeSpan.FromSeconds(15.0)
	};

	private readonly LibraryService _library;
	private readonly string _coverDir;
	private int _isMatchingRunning;

	public event Action<Track>? CoverUpdated;

	public AlbumArtService(LibraryService library, string coverDir)
	{
		_library = library;
		_coverDir = coverDir;
		try
		{
			Directory.CreateDirectory(_coverDir);
		}
		catch
		{
		}
	}

	public async Task<Track?> FindMissingCoverTrackAsync(Track? preferred = null, CancellationToken ct = default)
	{
		List<Track> list = await _library.GetAllTracksAsync();
		Track? track = null;
		foreach (Track item in list)
		{
			if (string.IsNullOrWhiteSpace(item.CoverPath) || !File.Exists(item.CoverPath))
			{
				if (preferred != null && item.Id == preferred.Id)
				{
					return item;
				}
				track ??= item;
			}
		}
		return track;
	}

	public async Task<(bool Ok, string Message, Track? Track)> FetchAndApplyCoverAsync(Track track, CancellationToken ct = default)
	{
		try
		{
			string term = CleanSearchTerm(track.Title, track.Artist, track.Album);
			if (string.IsNullOrWhiteSpace(term))
			{
				return (Ok: false, Message: "歌曲缺少标题/艺术家信息，无法搜索", Track: null);
			}

			string? artworkUrl = await SearchOnlineArtworkUrlAsync(term, ct);
			if (string.IsNullOrWhiteSpace(artworkUrl))
			{
				// 尝试仅使用标题搜索
				if (!string.IsNullOrWhiteSpace(track.Title))
				{
					artworkUrl = await SearchOnlineArtworkUrlAsync(CleanTitle(track.Title), ct);
				}
			}

			if (string.IsNullOrWhiteSpace(artworkUrl))
			{
				return (Ok: false, Message: "网络未找到匹配的专辑封面", Track: null);
			}

			using var req = new HttpRequestMessage(HttpMethod.Get, artworkUrl);
			req.Headers.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");
			using var resp = await Http.SendAsync(req, ct).ConfigureAwait(false);
			resp.EnsureSuccessStatusCode();

			byte[] array = await resp.Content.ReadAsByteArrayAsync(ct).ConfigureAwait(false);
			if (array.Length < 100)
			{
				return (Ok: false, Message: "封面下载内容异常", Track: null);
			}

			Directory.CreateDirectory(_coverDir);
			string ext = array.Length > 8 && array[0] == 137 && array[1] == 80 ? ".png" : ".jpg";
			string fileName = $"art_{track.Id}_{Math.Abs(term.GetHashCode())}{ext}";
			string localPath = Path.Combine(_coverDir, fileName);

			await File.WriteAllBytesAsync(localPath, array, ct).ConfigureAwait(false);
			track.CoverPath = localPath;
			await _library.UpdateCoverPathAsync(track.Id, localPath).ConfigureAwait(false);

			CoverUpdated?.Invoke(track);
			return (Ok: true, Message: "已智能匹配并应用网络封面：" + track.DisplayTitle, Track: track);
		}
		catch (OperationCanceledException)
		{
			return (Ok: false, Message: "操作已取消", Track: null);
		}
		catch (Exception ex)
		{
			return (Ok: false, Message: "搜索失败：" + ex.Message, Track: null);
		}
	}

	/// <summary>
	/// 在后台自动扫描曲库中所有缺少封面的本地歌曲，并智能匹配下载高清网络封面
	/// </summary>
	public void QueueAutoMatchMissingCovers()
	{
		if (Interlocked.CompareExchange(ref _isMatchingRunning, 1, 0) != 0)
		{
			return;
		}

		_ = Task.Run(async () =>
		{
			try
			{
				var allTracks = await _library.GetAllTracksAsync();
				foreach (var t in allTracks)
				{
					if (string.IsNullOrWhiteSpace(t.CoverPath) || !File.Exists(t.CoverPath))
					{
						await FetchAndApplyCoverAsync(t).ConfigureAwait(false);
						await Task.Delay(250).ConfigureAwait(false); // 礼貌限速
					}
				}
			}
			catch (Exception ex)
			{
				Debug.WriteLine($"AutoMatchMissingCovers background error: {ex.Message}");
			}
			finally
			{
				Interlocked.Exchange(ref _isMatchingRunning, 0);
			}
		});
	}

	private async Task<string?> SearchOnlineArtworkUrlAsync(string query, CancellationToken ct)
	{
		if (string.IsNullOrWhiteSpace(query)) return null;

		// 1. 优先调用网易云音乐 API（ACG、原神/HOYO-MiX、华语流行、日韩与欧美曲库极其完善）
		try
		{
			string searchUrl = "https://music.163.com/api/search/get/web?csrf_token=&hlpretag=&hlposttag=&s=" + Uri.EscapeDataString(query) + "&type=1&offset=0&total=true&limit=1";
			using var req = new HttpRequestMessage(HttpMethod.Get, searchUrl);
			req.Headers.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");
			using var resp = await Http.SendAsync(req, ct).ConfigureAwait(false);
			if (resp.IsSuccessStatusCode)
			{
				string json = await resp.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
				using var doc = JsonDocument.Parse(json);
				if (doc.RootElement.TryGetProperty("result", out var res) &&
				    res.TryGetProperty("songs", out var songs) &&
				    songs.GetArrayLength() > 0)
				{
					long songId = songs[0].GetProperty("id").GetInt64();
					string detailUrl = $"https://music.163.com/api/song/detail/?id={songId}&ids=[{songId}]";
					using var dReq = new HttpRequestMessage(HttpMethod.Get, detailUrl);
					dReq.Headers.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");
					using var dResp = await Http.SendAsync(dReq, ct).ConfigureAwait(false);
					if (dResp.IsSuccessStatusCode)
					{
						string dJson = await dResp.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
						using var dDoc = JsonDocument.Parse(dJson);
						if (dDoc.RootElement.TryGetProperty("songs", out var dSongs) && dSongs.GetArrayLength() > 0)
						{
							var s = dSongs[0];
							if (s.TryGetProperty("album", out var album) && album.TryGetProperty("picUrl", out var picUrlProp))
							{
								string? pic = picUrlProp.GetString();
								if (!string.IsNullOrEmpty(pic))
								{
									return pic.Replace("http://", "https://", StringComparison.OrdinalIgnoreCase);
								}
							}
						}
					}
				}
			}
		}
		catch
		{
		}

		// 2. 次级候选：Apple iTunes Search API
		try
		{
			string requestUri = "https://itunes.apple.com/search?entity=song&media=music&limit=1&term=" + Uri.EscapeDataString(query);
			using var req = new HttpRequestMessage(HttpMethod.Get, requestUri);
			req.Headers.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64)");
			using var resp = await Http.SendAsync(req, ct).ConfigureAwait(false);
			if (resp.IsSuccessStatusCode)
			{
				string json = await resp.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
				string? itunesPic = ParseArtworkUrl(json);
				if (!string.IsNullOrWhiteSpace(itunesPic)) return itunesPic;
			}
		}
		catch
		{
		}

		// 3. 第三候选：QQ音乐搜素 API
		try
		{
			string qqSearch = "https://c.y.qq.com/soso/fcgi-bin/client_search_cp?w=" + Uri.EscapeDataString(query) + "&n=1&format=json";
			using var req = new HttpRequestMessage(HttpMethod.Get, qqSearch);
			req.Headers.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64)");
			using var resp = await Http.SendAsync(req, ct).ConfigureAwait(false);
			if (resp.IsSuccessStatusCode)
			{
				string json = await resp.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
				using var doc = JsonDocument.Parse(json);
				if (doc.RootElement.TryGetProperty("data", out var data) &&
				    data.TryGetProperty("song", out var song) &&
				    song.TryGetProperty("list", out var list) &&
				    list.GetArrayLength() > 0)
				{
					string? albumMid = list[0].TryGetProperty("albummid", out var midProp) ? midProp.GetString() : null;
					if (!string.IsNullOrEmpty(albumMid))
					{
						return $"https://y.gtimg.cn/music/photo_new/T002R500x500M000{albumMid}.jpg";
					}
				}
			}
		}
		catch
		{
		}

		return null;
	}

	private static string CleanSearchTerm(string? title, string? artist, string? album)
	{
		string t = CleanTitle(title);
		string a = CleanArtist(artist);
		if (!string.IsNullOrWhiteSpace(t) && !string.IsNullOrWhiteSpace(a))
		{
			return $"{t} {a}".Trim();
		}
		if (!string.IsNullOrWhiteSpace(t))
		{
			return t;
		}
		return (album ?? string.Empty).Trim();
	}

	private static string CleanTitle(string? title)
	{
		if (string.IsNullOrWhiteSpace(title)) return string.Empty;
		string s = title;
		s = Regex.Replace(s, @"\[mqms\d*\]", "", RegexOptions.IgnoreCase);
		s = Regex.Replace(s, @"\.(flac|mp3|ogg|wav|m4a|aac)$", "", RegexOptions.IgnoreCase);
		s = Regex.Replace(s, @"[\(\[].*?(feat|explicit|remix|version|hq|hires|deluxe).*?[\)\]]", "", RegexOptions.IgnoreCase);
		return s.Trim();
	}

	private static string CleanArtist(string? artist)
	{
		if (string.IsNullOrWhiteSpace(artist)) return string.Empty;
		string a = artist.Replace("/", " ").Replace("&", " ");
		return a.Trim();
	}

	private static string? ParseArtworkUrl(string json)
	{
		try
		{
			using JsonDocument jsonDocument = JsonDocument.Parse(json);
			if (jsonDocument.RootElement.TryGetProperty("results", out var value) && value.GetArrayLength() > 0 && value[0].TryGetProperty("artworkUrl100", out var value2) && value2.ValueKind == JsonValueKind.String)
			{
				return (value2.GetString() ?? "").Replace("100x100", "600x600", StringComparison.OrdinalIgnoreCase);
			}
		}
		catch
		{
		}
		return null;
	}

	public static string RateStatusText()
	{
		return AlbumArtRateLimit.StatusText();
	}

	public static Task<bool> WaitForSlotAsync(TimeSpan maxWait, CancellationToken ct = default)
	{
		return AlbumArtRateLimit.WaitForSlotAsync(maxWait, ct);
	}
}
