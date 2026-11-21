using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using ATL;
using FurinaPlayer.Core.Models;

namespace FurinaPlayer.Core.Services;

public sealed record LibraryIntegrityProgress(
	int ProcessedCount,
	int TotalCount,
	string CurrentTrackTitle,
	int RepairedMetadataCount,
	int RepairedLyricsCount,
	int RepairedCoverCount,
	int AlreadyCompleteCount,
	int FailedCount)
{
	public double Percent => TotalCount > 0 ? Math.Clamp((double)ProcessedCount / TotalCount * 100.0, 0.0, 100.0) : 0.0;
}

public sealed record LibraryIntegrityReport(
	int TotalCount,
	int ProcessedCount,
	int RepairedMetadataCount,
	int RepairedLyricsCount,
	int RepairedCoverCount,
	int AlreadyCompleteCount,
	int FailedCount,
	bool Cancelled);

/// <summary>
/// 曲库音乐文件完整性扫描与多平台并行补齐服务：
/// 多线程并行检查本地曲库中每首歌曲的元数据、内嵌歌词、专辑封面完整性，
/// 按 网易云音乐 → QQ音乐 → Apple Music 顺序在线检索并回写音频标签与本地数据库。
/// </summary>
public sealed class LibraryIntegrityService
{
	private static readonly HttpClient Http = new() { Timeout = TimeSpan.FromSeconds(10) };

	private readonly LibraryService _library;
	private readonly LyricService _lyricService;

	public bool EnableOnlineLookup { get; set; } = true;

	public LibraryIntegrityService(LibraryService library, LyricService? lyricService = null)
	{
		_library = library ?? throw new ArgumentNullException(nameof(library));
		_lyricService = lyricService ?? new LyricService();
	}

	public async Task<LibraryIntegrityReport> CheckAndRepairLibraryAsync(
		IProgress<LibraryIntegrityProgress>? progress = null,
		int maxConcurrency = 4,
		CancellationToken ct = default)
	{
		List<FurinaPlayer.Core.Models.Track> tracks = await _library.GetAllTracksAsync();
		int total = tracks.Count;
		if (total == 0)
		{
			return new LibraryIntegrityReport(0, 0, 0, 0, 0, 0, 0, false);
		}

		int processed = 0;
		int repairedMeta = 0;
		int repairedLyrics = 0;
		int repairedCover = 0;
		int alreadyComplete = 0;
		int failed = 0;

		using var gate = new SemaphoreSlim(Math.Clamp(maxConcurrency, 1, 8));

		var tasks = tracks.Select(async track =>
		{
			await gate.WaitAsync(ct).ConfigureAwait(false);
			try
			{
				ct.ThrowIfCancellationRequested();
				var outcome = await InspectAndRepairTrackAsync(track, ct).ConfigureAwait(false);
				if (outcome.Failed)
				{
					Interlocked.Increment(ref failed);
				}
				else if (!outcome.MetaUpdated && !outcome.LyricsUpdated && !outcome.CoverUpdated)
				{
					Interlocked.Increment(ref alreadyComplete);
				}
				else
				{
					if (outcome.MetaUpdated) Interlocked.Increment(ref repairedMeta);
					if (outcome.LyricsUpdated) Interlocked.Increment(ref repairedLyrics);
					if (outcome.CoverUpdated) Interlocked.Increment(ref repairedCover);
				}
			}
			catch (OperationCanceledException)
			{
				throw;
			}
			catch
			{
				Interlocked.Increment(ref failed);
			}
			finally
			{
				int done = Interlocked.Increment(ref processed);
				progress?.Report(new LibraryIntegrityProgress(
					done,
					total,
					track.DisplayTitle,
					Volatile.Read(ref repairedMeta),
					Volatile.Read(ref repairedLyrics),
					Volatile.Read(ref repairedCover),
					Volatile.Read(ref alreadyComplete),
					Volatile.Read(ref failed)));
				gate.Release();
			}
		});

		bool cancelled = false;
		try
		{
			await Task.WhenAll(tasks).ConfigureAwait(false);
		}
		catch (OperationCanceledException)
		{
			cancelled = true;
		}

		return new LibraryIntegrityReport(
			total,
			processed,
			repairedMeta,
			repairedLyrics,
			repairedCover,
			alreadyComplete,
			failed,
			cancelled);
	}

	private sealed record TrackRepairOutcome(bool MetaUpdated, bool LyricsUpdated, bool CoverUpdated, bool Failed);

	private sealed class OnlineMatchResult
	{
		public string? Title { get; set; }
		public string? Artist { get; set; }
		public string? Album { get; set; }
		public string? CoverUrl { get; set; }
		public string? LrcText { get; set; }
	}

	private async Task<TrackRepairOutcome> InspectAndRepairTrackAsync(FurinaPlayer.Core.Models.Track track, CancellationToken ct)
	{
		if (string.IsNullOrWhiteSpace(track.FilePath) || !File.Exists(track.FilePath))
		{
			return new TrackRepairOutcome(false, false, false, true);
		}

		string fileBase = Path.GetFileNameWithoutExtension(track.FilePath);
		var atl = new ATL.Track(track.FilePath);

		bool needMeta = IsMissingTitle(atl.Title, fileBase) || IsMissingText(atl.Artist) || IsMissingText(atl.Album);
		string? currentLyrics = GetEmbeddedLyricsText(atl);
		if (string.IsNullOrWhiteSpace(currentLyrics)) currentLyrics = track.EmbeddedLyrics;
		bool needLyrics = string.IsNullOrWhiteSpace(currentLyrics);
		bool needCover = atl.EmbeddedPictures.Count == 0;
		bool needDbCover = string.IsNullOrWhiteSpace(track.CoverPath) || !File.Exists(track.CoverPath);

		bool metaUpdated = false;
		bool lyricsUpdated = false;
		bool coverUpdated = false;
		bool fileModified = false;

		// 1. 本地互相补齐：若数据库有封面文件但音频未内嵌，则写入音频；若音频有内嵌封面但数据库缺失，则提取缓存
		if (needCover && !needDbCover && File.Exists(track.CoverPath!))
		{
			try
			{
				byte[] diskCover = await File.ReadAllBytesAsync(track.CoverPath!, ct).ConfigureAwait(false);
				if (diskCover.Length > 64)
				{
					atl.EmbeddedPictures.Add(PictureInfo.fromBinaryData(diskCover, PictureInfo.PIC_TYPE.Front));
					needCover = false;
					coverUpdated = true;
					fileModified = true;
				}
			}
			catch { }
		}

		// 2. 文件名 "艺术家 - 歌名" 兜底补齐基础元数据
		var (parsedArtist, parsedTitle) = ParseArtistAndTitle(fileBase);
		if (IsMissingTitle(atl.Title, fileBase) && !string.IsNullOrWhiteSpace(parsedTitle))
		{
			atl.Title = parsedTitle;
			metaUpdated = true;
			fileModified = true;
		}
		if (IsMissingText(atl.Artist) && !string.IsNullOrWhiteSpace(parsedArtist))
		{
			atl.Artist = parsedArtist;
			metaUpdated = true;
			fileModified = true;
		}

		// 3. 若本地已有同文件夹 .lrc 歌词文件但音频未内嵌，先将其内嵌进音频
		if (needLyrics)
		{
			string? localLrc = _lyricService.MatchLocalLyrics(track.FilePath, atl.Title, atl.Artist);
			if (!string.IsNullOrWhiteSpace(localLrc) && File.Exists(localLrc))
			{
				try
				{
					string lrcText = await File.ReadAllTextAsync(localLrc, Encoding.UTF8, ct).ConfigureAwait(false);
					if (!string.IsNullOrWhiteSpace(lrcText))
					{
						atl.Lyrics.UnsynchronizedLyrics = lrcText;
						atl.AdditionalFields["LYRICS"] = lrcText;
						currentLyrics = lrcText;
						needLyrics = false;
						lyricsUpdated = true;
						fileModified = true;
					}
				}
				catch { }
			}
		}

		needMeta = IsMissingTitle(atl.Title, fileBase) || IsMissingText(atl.Artist) || IsMissingText(atl.Album);

		// 4. 在线多平台优先级补齐：网易云音乐 → QQ音乐 → Apple Music
		if (EnableOnlineLookup && (needMeta || needLyrics || needCover || needDbCover))
		{
			string qTitle = !IsMissingText(atl.Title) ? atl.Title.Trim() : (!string.IsNullOrWhiteSpace(parsedTitle) ? parsedTitle : fileBase);
			string qArtist = !IsMissingText(atl.Artist) ? atl.Artist.Trim() : parsedArtist;
			string query = string.IsNullOrWhiteSpace(qArtist) ? qTitle : $"{qTitle} {qArtist}";

			var candidates = new List<OnlineMatchResult>();

			var neMatch = await QueryNeteaseAsync(query, qTitle, qArtist, needLyrics, ct).ConfigureAwait(false);
			if (neMatch != null) candidates.Add(neMatch);

			if (needMeta || needLyrics || needCover || needDbCover)
			{
				bool stillNeedAfterNe = (needMeta && (IsMissingText(neMatch?.Album) || IsMissingText(neMatch?.Artist)))
					|| (needCover && string.IsNullOrWhiteSpace(neMatch?.CoverUrl))
					|| (needLyrics && string.IsNullOrWhiteSpace(neMatch?.LrcText));
				if (stillNeedAfterNe)
				{
					var qqMatch = await QueryQQMusicAsync(query, needLyrics, ct).ConfigureAwait(false);
					if (qqMatch != null) candidates.Add(qqMatch);
				}
			}

			bool stillNeedAfterQq = (needMeta && candidates.All(c => IsMissingText(c.Album)))
				|| ((needCover || needDbCover) && candidates.All(c => string.IsNullOrWhiteSpace(c.CoverUrl)));
			if (stillNeedAfterQq)
			{
				var appleMatch = await QueryAppleMusicAsync(query, ct).ConfigureAwait(false);
				if (appleMatch != null) candidates.Add(appleMatch);
			}

			foreach (var cand in candidates)
			{
				if (IsMissingTitle(atl.Title, fileBase) && !string.IsNullOrWhiteSpace(cand.Title))
				{
					atl.Title = cand.Title;
					metaUpdated = true;
					fileModified = true;
				}
				if (IsMissingText(atl.Artist) && !string.IsNullOrWhiteSpace(cand.Artist))
				{
					atl.Artist = cand.Artist;
					metaUpdated = true;
					fileModified = true;
				}
				if (IsMissingText(atl.Album) && !string.IsNullOrWhiteSpace(cand.Album))
				{
					atl.Album = cand.Album;
					metaUpdated = true;
					fileModified = true;
				}

				if (needLyrics && !string.IsNullOrWhiteSpace(cand.LrcText))
				{
					atl.Lyrics.UnsynchronizedLyrics = cand.LrcText;
					atl.AdditionalFields["LYRICS"] = cand.LrcText;
					currentLyrics = cand.LrcText;
					needLyrics = false;
					lyricsUpdated = true;
					fileModified = true;
				}

				if ((needCover || needDbCover) && !string.IsNullOrWhiteSpace(cand.CoverUrl))
				{
					byte[]? imgBytes = await DownloadBytesAsync(cand.CoverUrl, ct).ConfigureAwait(false);
					if (imgBytes is { Length: > 128 })
					{
						if (needCover)
						{
							atl.EmbeddedPictures.Add(PictureInfo.fromBinaryData(imgBytes, PictureInfo.PIC_TYPE.Front));
							needCover = false;
							coverUpdated = true;
							fileModified = true;
						}
						if (needDbCover)
						{
							string savedCover = SaveCoverToCache(track.FilePath, imgBytes);
							track.CoverPath = savedCover;
							needDbCover = false;
							coverUpdated = true;
						}
					}
				}
			}
		}

		if (fileModified)
		{
			atl.Save();
		}

		string? extractedCover = null;
		if (needDbCover && atl.EmbeddedPictures.Count > 0 && atl.EmbeddedPictures[0].PictureData is { Length: > 64 } embPic)
		{
			extractedCover = SaveCoverToCache(track.FilePath, embPic);
			coverUpdated = true;
		}

		if (metaUpdated || lyricsUpdated || coverUpdated)
		{
			await _library.UpdateTrackIntegrityAsync(
				track.Id,
				IsMissingText(atl.Title) ? null : atl.Title,
				IsMissingText(atl.Artist) ? null : atl.Artist,
				IsMissingText(atl.Album) ? null : atl.Album,
				currentLyrics,
				extractedCover ?? track.CoverPath).ConfigureAwait(false);
		}

		return new TrackRepairOutcome(metaUpdated, lyricsUpdated, coverUpdated, false);
	}

	private async Task<OnlineMatchResult?> QueryNeteaseAsync(string query, string title, string artist, bool needLyrics, CancellationToken ct)
	{
		try
		{
			using var req = new HttpRequestMessage(HttpMethod.Get, $"https://music.163.com/api/cloudsearch/pc?s={Uri.EscapeDataString(query)}&type=1&limit=3");
			req.Headers.Referrer = new Uri("https://music.163.com/");
			req.Headers.TryAddWithoutValidation("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");
			using var resp = await Http.SendAsync(req, ct).ConfigureAwait(false);
			if (!resp.IsSuccessStatusCode) return null;

			using var doc = JsonDocument.Parse(await resp.Content.ReadAsStringAsync(ct).ConfigureAwait(false));
			if (!doc.RootElement.TryGetProperty("result", out var res) ||
				!res.TryGetProperty("songs", out var songs) ||
				songs.ValueKind != JsonValueKind.Array ||
				songs.GetArrayLength() == 0)
			{
				return null;
			}

			var first = songs[0];
			long songId = first.TryGetProperty("id", out var idP) ? idP.GetInt64() : 0;
			string? sTitle = first.TryGetProperty("name", out var nP) ? nP.GetString() : null;
			string? sArtist = null;
			if (first.TryGetProperty("ar", out var arP) && arP.ValueKind == JsonValueKind.Array)
			{
				sArtist = string.Join(" / ", arP.EnumerateArray()
					.Select(a => a.TryGetProperty("name", out var an) ? an.GetString() : null)
					.Where(n => !string.IsNullOrWhiteSpace(n)));
			}
			string? sAlbum = null;
			string? picUrl = null;
			if (first.TryGetProperty("al", out var alP) && alP.ValueKind == JsonValueKind.Object)
			{
				sAlbum = alP.TryGetProperty("name", out var aln) ? aln.GetString() : null;
				picUrl = alP.TryGetProperty("picUrl", out var picP) ? picP.GetString()?.Replace("http://", "https://", StringComparison.OrdinalIgnoreCase) : null;
			}

			string? lrcText = null;
			if (needLyrics && songId > 0)
			{
				var lrcDoc = await _lyricService.GetNeteaseLyricByIdAsync(songId, ct).ConfigureAwait(false);
				if (lrcDoc != null && lrcDoc.Lines.Count > 0)
				{
					lrcText = FormatLyricDocumentToLrc(lrcDoc);
				}
			}

			return new OnlineMatchResult
			{
				Title = sTitle,
				Artist = sArtist,
				Album = sAlbum,
				CoverUrl = picUrl,
				LrcText = lrcText
			};
		}
		catch
		{
			return null;
		}
	}

	private async Task<OnlineMatchResult?> QueryQQMusicAsync(string query, bool needLyrics, CancellationToken ct)
	{
		try
		{
			using var req = new HttpRequestMessage(HttpMethod.Get, $"https://c.y.qq.com/soso/fcgi-bin/client_search_cp?w={Uri.EscapeDataString(query)}&n=3&format=json");
			req.Headers.Referrer = new Uri("https://y.qq.com/");
			req.Headers.TryAddWithoutValidation("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");
			using var resp = await Http.SendAsync(req, ct).ConfigureAwait(false);
			if (!resp.IsSuccessStatusCode) return null;

			using var doc = JsonDocument.Parse(await resp.Content.ReadAsStringAsync(ct).ConfigureAwait(false));
			if (!doc.RootElement.TryGetProperty("data", out var data) ||
				!data.TryGetProperty("song", out var song) ||
				!song.TryGetProperty("list", out var list) ||
				list.ValueKind != JsonValueKind.Array ||
				list.GetArrayLength() == 0)
			{
				return null;
			}

			var first = list[0];
			string? sTitle = first.TryGetProperty("songname", out var sn) ? sn.GetString() : null;
			string? sAlbum = first.TryGetProperty("albumname", out var an) ? an.GetString() : null;
			string? albumMid = first.TryGetProperty("albummid", out var am) ? am.GetString() : null;
			string? songMid = first.TryGetProperty("songmid", out var sm) ? sm.GetString() : null;
			string? sArtist = null;
			if (first.TryGetProperty("singer", out var sg) && sg.ValueKind == JsonValueKind.Array)
			{
				sArtist = string.Join(" / ", sg.EnumerateArray()
					.Select(s => s.TryGetProperty("name", out var n) ? n.GetString() : null)
					.Where(n => !string.IsNullOrWhiteSpace(n)));
			}

			string? coverUrl = !string.IsNullOrWhiteSpace(albumMid)
				? $"https://y.gtimg.cn/music/photo_new/T002R500x500M000{albumMid}.jpg"
				: null;

			string? lrcText = null;
			if (needLyrics && !string.IsNullOrWhiteSpace(songMid))
			{
				try
				{
					using var lReq = new HttpRequestMessage(HttpMethod.Get, $"https://c.y.qq.com/lyric/fcgi-bin/fcg_query_lyric_new.fcg?songmid={Uri.EscapeDataString(songMid)}&format=json&nobase64=1");
					lReq.Headers.Referrer = new Uri("https://y.qq.com/");
					lReq.Headers.TryAddWithoutValidation("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");
					using var lResp = await Http.SendAsync(lReq, ct).ConfigureAwait(false);
					if (lResp.IsSuccessStatusCode)
					{
						using var lDoc = JsonDocument.Parse(await lResp.Content.ReadAsStringAsync(ct).ConfigureAwait(false));
						if (lDoc.RootElement.TryGetProperty("lyric", out var lp))
						{
							string? raw = lp.GetString();
							if (!string.IsNullOrWhiteSpace(raw) && raw.Contains('['))
							{
								lrcText = System.Net.WebUtility.HtmlDecode(raw);
							}
						}
					}
				}
				catch { }
			}

			return new OnlineMatchResult
			{
				Title = sTitle,
				Artist = sArtist,
				Album = sAlbum,
				CoverUrl = coverUrl,
				LrcText = lrcText
			};
		}
		catch
		{
			return null;
		}
	}

	private async Task<OnlineMatchResult?> QueryAppleMusicAsync(string query, CancellationToken ct)
	{
		try
		{
			using var req = new HttpRequestMessage(HttpMethod.Get, $"https://itunes.apple.com/search?entity=song&media=music&limit=3&term={Uri.EscapeDataString(query)}");
			req.Headers.TryAddWithoutValidation("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64)");
			using var resp = await Http.SendAsync(req, ct).ConfigureAwait(false);
			if (!resp.IsSuccessStatusCode) return null;

			using var doc = JsonDocument.Parse(await resp.Content.ReadAsStringAsync(ct).ConfigureAwait(false));
			if (!doc.RootElement.TryGetProperty("results", out var results) ||
				results.ValueKind != JsonValueKind.Array ||
				results.GetArrayLength() == 0)
			{
				return null;
			}

			var first = results[0];
			string? art = first.TryGetProperty("artworkUrl100", out var aw) ? aw.GetString()?.Replace("100x100bb", "600x600bb") : null;
			return new OnlineMatchResult
			{
				Title = first.TryGetProperty("trackName", out var tn) ? tn.GetString() : null,
				Artist = first.TryGetProperty("artistName", out var an) ? an.GetString() : null,
				Album = first.TryGetProperty("collectionName", out var cn) ? cn.GetString() : null,
				CoverUrl = art
			};
		}
		catch
		{
			return null;
		}
	}

	private async Task<byte[]?> DownloadBytesAsync(string url, CancellationToken ct)
	{
		try
		{
			using var req = new HttpRequestMessage(HttpMethod.Get, url);
			req.Headers.TryAddWithoutValidation("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64)");
			using var resp = await Http.SendAsync(req, ct).ConfigureAwait(false);
			if (!resp.IsSuccessStatusCode) return null;
			return await resp.Content.ReadAsByteArrayAsync(ct).ConfigureAwait(false);
		}
		catch
		{
			return null;
		}
	}

	private string SaveCoverToCache(string audioPath, byte[] bytes)
	{
		Directory.CreateDirectory(_library.CoverDirectory);
		byte[] hash = SHA1.HashData(Encoding.UTF8.GetBytes(audioPath));
		string fileName = Convert.ToHexString(hash).ToLowerInvariant() + ".jpg";
		string outPath = Path.Combine(_library.CoverDirectory, fileName);
		File.WriteAllBytes(outPath, bytes);
		return outPath;
	}

	private static string? GetEmbeddedLyricsText(ATL.Track atl)
	{
		if (!string.IsNullOrWhiteSpace(atl.Lyrics?.UnsynchronizedLyrics))
			return atl.Lyrics.UnsynchronizedLyrics;
		if (atl.AdditionalFields != null && atl.AdditionalFields.TryGetValue("LYRICS", out string? extra) && !string.IsNullOrWhiteSpace(extra))
			return extra;
		return null;
	}

	private static string FormatLyricDocumentToLrc(LyricDocument doc)
	{
		var lines = new List<string>(doc.Lines.Count);
		foreach (var l in doc.Lines)
		{
			int ms = (int)Math.Max(0, l.Time.TotalMilliseconds);
			lines.Add($"[{ms / 60000:00}:{(ms % 60000) / 1000:00}.{(ms % 1000) / 10:00}]{l.DisplayOriginal}");
			if (!string.IsNullOrWhiteSpace(l.Translation))
			{
				lines.Add($"[{ms / 60000:00}:{(ms % 60000) / 1000:00}.{(ms % 1000) / 10:00}]{l.Translation}");
			}
		}
		return string.Join(Environment.NewLine, lines);
	}

	private static bool IsMissingText(string? value)
	{
		if (string.IsNullOrWhiteSpace(value)) return true;
		string v = value.Trim();
		return v.Equals("未知曲目", StringComparison.OrdinalIgnoreCase)
			|| v.Equals("未知艺术家", StringComparison.OrdinalIgnoreCase)
			|| v.Equals("未知专辑", StringComparison.OrdinalIgnoreCase)
			|| v.Equals("Unknown", StringComparison.OrdinalIgnoreCase)
			|| v.Equals("Unknown Artist", StringComparison.OrdinalIgnoreCase)
			|| v.Equals("Unknown Album", StringComparison.OrdinalIgnoreCase);
	}

	private static bool IsMissingTitle(string? value, string fileBase)
	{
		if (IsMissingText(value)) return true;
		return string.Equals(value!.Trim(), fileBase.Trim(), StringComparison.OrdinalIgnoreCase);
	}

	private static (string artist, string title) ParseArtistAndTitle(string baseName)
	{
		int idx = baseName.IndexOf(" - ", StringComparison.Ordinal);
		if (idx > 0 && idx + 3 < baseName.Length)
		{
			return (baseName[..idx].Trim(), baseName[(idx + 3)..].Trim());
		}
		return (string.Empty, baseName.Trim());
	}
}
