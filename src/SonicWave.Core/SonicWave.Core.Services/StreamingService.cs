using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using SonicWave.Core.Models;

namespace SonicWave.Core.Services;

public class StreamingTrackInfo
{
	public long Id { get; set; }
	public string Title { get; set; } = string.Empty;
	public string Artist { get; set; } = string.Empty;
	public string Album { get; set; } = string.Empty;
	public string StreamUrl { get; set; } = string.Empty;
	public string? CoverUrl { get; set; }
	public TimeSpan Duration { get; set; }
	public string Source { get; set; } = "Online";
}

/// <summary>
/// FurinaPlayer 流媒体服务：支持直接播放网络音频直链（HTTP/HTTPS/HLS/M3U8），
/// 并借鉴 ShinawaseLoader 架构集成在线流媒体搜索、解析与直链音频播放。
/// </summary>
public class StreamingService
{
	private static readonly HttpClient _http = new()
	{
		Timeout = TimeSpan.FromSeconds(15)
	};

	static StreamingService()
	{
		try
		{
			_http.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");
		}
		catch
		{
		}
	}

	/// <summary>
	/// 解析任意网络音频直接链接 (HTTP/HTTPS/HLS/M3U8) 并返回 Track 对象
	/// </summary>
	public Track CreateTrackFromDirectUrl(string url, string? title = null, string? artist = null, string? album = null)
	{
		url = (url ?? string.Empty).Trim();
		string fallbackTitle = title ?? string.Empty;
		if (string.IsNullOrWhiteSpace(fallbackTitle))
		{
			try
			{
				var uri = new Uri(url);
				fallbackTitle = Path.GetFileNameWithoutExtension(uri.LocalPath);
				if (string.IsNullOrWhiteSpace(fallbackTitle))
				{
					fallbackTitle = uri.Host;
				}
			}
			catch
			{
				fallbackTitle = "网络音频流";
			}
		}

		return new Track
		{
			Id = -Math.Abs(DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()),
			Title = fallbackTitle,
			Artist = string.IsNullOrWhiteSpace(artist) ? "网络流媒体" : artist,
			Album = string.IsNullOrWhiteSpace(album) ? "网络串流" : album,
			FilePath = url,
			DurationMilliseconds = 0,
			AddedAt = DateTime.UtcNow,
			Genre = "Streaming"
		};
	}

	/// <summary>
	/// 搜索在线音乐（基于开放网易云音乐流媒体接口，实现类似 ShinawaseLoader 的流式解析）
	/// </summary>
	public async Task<List<StreamingTrackInfo>> SearchOnlineTracksAsync(string query, int limit = 15)
	{
		var results = new List<StreamingTrackInfo>();
		if (string.IsNullOrWhiteSpace(query)) return results;

		try
		{
			string url = $"http://music.163.com/api/search/get/web?csrf_token=&hlpretag=&hlposttag=&s={Uri.EscapeDataString(query)}&type=1&offset=0&total=true&limit={limit}";
			using var req = new HttpRequestMessage(HttpMethod.Get, url);
			req.Headers.Referrer = new Uri("http://music.163.com/");
			using var resp = await _http.SendAsync(req);
			if (!resp.IsSuccessStatusCode) return results;

			string json = await resp.Content.ReadAsStringAsync();
			using var doc = JsonDocument.Parse(json);
			if (doc.RootElement.TryGetProperty("result", out var resultElem) &&
				resultElem.TryGetProperty("songs", out var songsElem) &&
				songsElem.ValueKind == JsonValueKind.Array)
			{
				foreach (var song in songsElem.EnumerateArray())
				{
					long songId = 0;
					if (song.TryGetProperty("id", out var idProp))
					{
						songId = idProp.GetInt64();
					}
					if (songId <= 0) continue;

					string name = song.TryGetProperty("name", out var nameProp) ? nameProp.GetString() ?? "未知曲目" : "未知曲目";

					string artistName = "未知艺术家";
					if (song.TryGetProperty("artists", out var artistsElem) && artistsElem.ValueKind == JsonValueKind.Array)
					{
						var artistList = new List<string>();
						foreach (var art in artistsElem.EnumerateArray())
						{
							if (art.TryGetProperty("name", out var aName))
							{
								var s = aName.GetString();
								if (!string.IsNullOrEmpty(s)) artistList.Add(s);
							}
						}
						if (artistList.Count > 0) artistName = string.Join(" / ", artistList);
					}

					string albumName = "未知专辑";
					string? picUrl = null;
					if (song.TryGetProperty("album", out var albumElem))
					{
						if (albumElem.TryGetProperty("name", out var albName))
						{
							albumName = albName.GetString() ?? "未知专辑";
						}
						if (albumElem.TryGetProperty("picUrl", out var picProp))
						{
							picUrl = picProp.GetString();
						}
					}

					int durationMs = 0;
					if (song.TryGetProperty("duration", out var durProp))
					{
						durationMs = durProp.GetInt32();
					}

					// ShinawaseLoader direct outer stream url
					string streamUrl = $"http://music.163.com/song/media/outer/url?id={songId}.mp3";

					results.Add(new StreamingTrackInfo
					{
						Id = songId,
						Title = name,
						Artist = artistName,
						Album = albumName,
						StreamUrl = streamUrl,
						CoverUrl = picUrl,
						Duration = TimeSpan.FromMilliseconds(durationMs),
						Source = "NetEase"
					});
				}
			}
		}
		catch (Exception ex)
		{
			System.Diagnostics.Debug.WriteLine($"SearchOnlineTracksAsync failed: {ex.Message}");
		}

		return results;
	}

	/// <summary>
	/// 将 StreamingTrackInfo 转换为 FurinaPlayer 可直接播放的 Track 对象
	/// </summary>
	public Track ToTrack(StreamingTrackInfo info, string? localCoverPath = null)
	{
		return new Track
		{
			Id = -info.Id,
			Title = info.Title,
			Artist = info.Artist,
			Album = info.Album,
			FilePath = info.StreamUrl,
			DurationMilliseconds = (long)info.Duration.TotalMilliseconds,
			CoverPath = localCoverPath,
			AddedAt = DateTime.UtcNow,
			Genre = "Streaming"
		};
	}
}
