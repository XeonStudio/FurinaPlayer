using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using SonicWave.Core.Models;

namespace SonicWave.Core.Services;

public sealed class AlbumArtService
{
	private static readonly HttpClient Http = new HttpClient
	{
		Timeout = TimeSpan.FromSeconds(12.0)
	};

	private readonly LibraryService _library;

	private readonly string _coverDir;

	public AlbumArtService(LibraryService library, string coverDir)
	{
		_library = library;
		_coverDir = coverDir;
	}

	public async Task<Track?> FindMissingCoverTrackAsync(Track? preferred = null, CancellationToken ct = default(CancellationToken))
	{
		List<Track> list = await _library.GetAllTracksAsync();
		Track track = null;
		foreach (Track item in list)
		{
			if (string.IsNullOrWhiteSpace(item.CoverPath) || !File.Exists(item.CoverPath))
			{
				if (preferred != null && item.Id == preferred.Id)
				{
					return item;
				}
				if (track == null)
				{
					track = item;
				}
			}
		}
		return track;
	}

	public async Task<(bool Ok, string Message, Track? Track)> FetchAndApplyCoverAsync(Track track, CancellationToken ct = default(CancellationToken))
	{
		_ = 4;
		try
		{
			string term = (track.Title + " " + track.Artist).Trim();
			if (string.IsNullOrWhiteSpace(term))
			{
				return (Ok: false, Message: "歌曲缺少标题/艺术家信息，无法搜索", Track: null);
			}
			string requestUri = "https://itunes.apple.com/search?entity=song&media=music&limit=1&term=" + Uri.EscapeDataString(term);
			using HttpResponseMessage resp = await Http.GetAsync(requestUri, ct).ConfigureAwait(continueOnCapturedContext: false);
			resp.EnsureSuccessStatusCode();
			string text = ParseArtworkUrl(await resp.Content.ReadAsStringAsync(ct).ConfigureAwait(continueOnCapturedContext: false));
			if (string.IsNullOrWhiteSpace(text))
			{
				return (Ok: false, Message: "在线未找到该歌曲的专辑封面", Track: null);
			}
			byte[] array = await Http.GetByteArrayAsync(text).ConfigureAwait(continueOnCapturedContext: false);
			if (array.Length < 100)
			{
				return (Ok: false, Message: "封面下载内容异常", Track: null);
			}
			Directory.CreateDirectory(_coverDir);
			string path = "art_" + track.Id + "_" + Math.Abs(term.GetHashCode()) + ".jpg";
			string path2 = Path.Combine(_coverDir, path);
			await File.WriteAllBytesAsync(path2, array, ct).ConfigureAwait(continueOnCapturedContext: false);
			track.CoverPath = path2;
			await _library.UpdateCoverPathAsync(track.Id, path2).ConfigureAwait(continueOnCapturedContext: false);
			return (Ok: true, Message: "已添加封面：" + track.DisplayTitle, Track: track);
		}
		catch (OperationCanceledException)
		{
			return (Ok: false, Message: "操作已取消", Track: null);
		}
		catch (Exception ex2)
		{
			return (Ok: false, Message: "搜索失败：" + ex2.Message, Track: null);
		}
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

	public static Task<bool> WaitForSlotAsync(TimeSpan maxWait, CancellationToken ct = default(CancellationToken))
	{
		return AlbumArtRateLimit.WaitForSlotAsync(maxWait, ct);
	}
}
