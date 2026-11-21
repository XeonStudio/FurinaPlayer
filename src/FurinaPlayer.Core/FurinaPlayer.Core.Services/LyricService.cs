using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using FurinaPlayer.Core.Models;

namespace FurinaPlayer.Core.Services;

public class LyricService
{
	private static readonly HttpClient Http = new HttpClient
	{
		Timeout = TimeSpan.FromSeconds(8.0)
	};

	private static readonly Regex _timeTagRegex = new Regex(@"\[(\d{1,3}):(\d{1,2})(?:[.:](\d{1,3}))?\]", RegexOptions.Compiled);
	private static Regex TimeTagRegex() => _timeTagRegex;

	public LyricDocument ParseLrc(string content)
	{
		LyricDocument lyricDocument = new LyricDocument();
		if (string.IsNullOrWhiteSpace(content))
		{
			return lyricDocument;
		}
		string[] array = content.Split('\n', '\r');
		for (int i = 0; i < array.Length; i++)
		{
			string text = array[i].Trim();
			if (text.Length == 0)
			{
				continue;
			}
			Match match = Regex.Match(text, "^\\[(ti|ar|al|by|offset):(.*)\\]$", RegexOptions.IgnoreCase);
			if (match.Success)
			{
				string text2 = match.Groups[2].Value.Trim();
				switch (match.Groups[1].Value.ToLowerInvariant())
				{
				case "ti":
					lyricDocument.Title = text2;
					break;
				case "ar":
					lyricDocument.Artist = text2;
					break;
				case "al":
					lyricDocument.Album = text2;
					break;
				}
				continue;
			}
			MatchCollection matchCollection = TimeTagRegex().Matches(text);
			if (matchCollection.Count == 0)
			{
				continue;
			}
			string text3 = TimeTagRegex().Replace(text, "").Trim();
			foreach (Match item in matchCollection)
			{
				int num = int.Parse(item.Groups[1].Value);
				int num2 = int.Parse(item.Groups[2].Value);
				int num3 = 0;
				if (item.Groups[3].Success && int.TryParse(item.Groups[3].Value, out var result))
				{
					if (item.Groups[3].Value.Length == 1)
					{
						num3 = result * 100;
					}
					else
					{
						num3 = ((item.Groups[3].Value.Length == 2) ? (result * 10) : result);
					}
				}
				int lineStartMs = num * 60000 + num2 * 1000 + num3;
				var (cleanText, timings, words) = ParseEnhancedInlineWords(text3, lineStartMs);
				LyricLine lyricLine = new LyricLine
				{
					Time = TimeSpan.FromMilliseconds(lineStartMs),
					Text = cleanText,
					OriginalText = cleanText,
					WordTimings = timings,
					Words = words
				};
				lyricDocument.Lines.Add(lyricLine);
			}
		}
		lyricDocument.Lines.Sort((LyricLine a, LyricLine b) => a.Time.CompareTo(b.Time));
		MergeSameTimestampTranslations(lyricDocument.Lines);
		return lyricDocument;
	}

	private static (string CleanText, List<(int CharIndex, int StartMs)>? Timings, List<LyricWord>? Words) ParseEnhancedInlineWords(string text, int lineStartMs)
	{
		if (!text.Contains('<') || !text.Contains('>'))
		{
			return (text, null, null);
		}
		var matches = Regex.Matches(text, "<(\\d{1,3}):(\\d{1,2})(?:[.:](\\d{1,3}))?>");
		if (matches.Count == 0)
		{
			return (text, null, null);
		}

		var timings = new List<(int CharIndex, int StartMs)>();
		var rawWords = new List<(string WordText, int StartMs)>();
		var cleanSb = new StringBuilder();
		int cursor = 0;

		for (int i = 0; i < matches.Count; i++)
		{
			Match m = matches[i];
			if (m.Index > cursor)
			{
				string prefix = text.Substring(cursor, m.Index - cursor);
				if (prefix.Length > 0)
				{
					cleanSb.Append(prefix);
				}
			}
			int ms = int.Parse(m.Groups[1].Value) * 60000 + int.Parse(m.Groups[2].Value) * 1000;
			if (m.Groups[3].Success && int.TryParse(m.Groups[3].Value, out int frac))
			{
				ms += m.Groups[3].Value.Length == 1 ? frac * 100 : (m.Groups[3].Value.Length == 2 ? frac * 10 : frac);
			}
			int nextStart = (i + 1 < matches.Count) ? matches[i + 1].Index : text.Length;
			string wordSeg = text.Substring(m.Index + m.Length, nextStart - (m.Index + m.Length));
			timings.Add((cleanSb.Length, ms));
			cleanSb.Append(wordSeg);
			if (wordSeg.Length > 0)
			{
				rawWords.Add((wordSeg, ms));
			}
			cursor = nextStart;
		}

		var words = new List<LyricWord>(rawWords.Count);
		for (int i = 0; i < rawWords.Count; i++)
		{
			int s = rawWords[i].StartMs;
			int e = (i + 1 < rawWords.Count) ? Math.Max(s + 50, rawWords[i + 1].StartMs) : Math.Max(s + 400, lineStartMs + 2000);
			words.Add(new LyricWord(rawWords[i].WordText, s, e));
		}

		string clean = cleanSb.ToString().Trim();
		return (clean, timings.Count > 0 ? timings : null, words.Count > 0 ? words : null);
	}

	private static void MergeSameTimestampTranslations(List<LyricLine> lines)
	{
		if (lines.Count < 2) return;
		var merged = new List<LyricLine>(lines.Count);
		for (int i = 0; i < lines.Count; i++)
		{
			LyricLine cur = lines[i];
			if (merged.Count > 0)
			{
				LyricLine prev = merged[^1];
				double deltaMs = Math.Abs((cur.Time - prev.Time).TotalMilliseconds);
				if (deltaMs <= 50.0 && !string.IsNullOrWhiteSpace(prev.DisplayOriginal) && !string.IsNullOrWhiteSpace(cur.DisplayOriginal))
				{
					string trans = cur.DisplayOriginal.Trim();
					if (!string.Equals(prev.DisplayOriginal.Trim(), trans, StringComparison.OrdinalIgnoreCase))
					{
						prev.OriginalText = prev.DisplayOriginal;
						prev.Translation = string.IsNullOrWhiteSpace(prev.Translation) ? trans : $"{prev.Translation} / {trans}";
						prev.Text = $"{prev.OriginalText}\n{prev.Translation}";
					}
					continue;
				}
			}
			merged.Add(cur);
		}
		lines.Clear();
		lines.AddRange(merged);
	}

	public LyricDocument ParseYrc(string content)
	{
		LyricDocument doc = new LyricDocument();
		if (string.IsNullOrWhiteSpace(content)) return doc;

		string[] rows = content.Split('\n', '\r');
		var lineHeaderRegex = new Regex(@"^\[(\d+),(\d+)\](.*)$");
		var wordRegex = new Regex(@"\((\d+),(\d+),\d+\)([^(]*)");

		foreach (string rawRow in rows)
		{
			string row = rawRow.Trim();
			if (row.Length == 0 || row.StartsWith('{')) continue;

			Match lm = lineHeaderRegex.Match(row);
			if (!lm.Success) continue;

			if (!int.TryParse(lm.Groups[1].Value, out int lineStartMs)) continue;
			string body = lm.Groups[3].Value;
			var wMatches = wordRegex.Matches(body);
			if (wMatches.Count == 0) continue;

			var sb = new StringBuilder();
			var words = new List<LyricWord>();
			var timings = new List<(int CharIndex, int StartMs)>();

			foreach (Match wm in wMatches)
			{
				int wStart = int.Parse(wm.Groups[1].Value);
				int wDur = int.Parse(wm.Groups[2].Value);
				string wText = wm.Groups[3].Value;
				if (wText.Length == 0) continue;

				timings.Add((sb.Length, wStart));
				sb.Append(wText);
				words.Add(new LyricWord(wText, wStart, wStart + Math.Max(40, wDur)));
			}

			string fullText = sb.ToString().Trim();
			if (fullText.Length == 0) continue;

			doc.Lines.Add(new LyricLine
			{
				Time = TimeSpan.FromMilliseconds(lineStartMs),
				Text = fullText,
				OriginalText = fullText,
				WordTimings = timings,
				Words = words
			});
		}

		doc.Lines.Sort((a, b) => a.Time.CompareTo(b.Time));
		return doc;
	}

	public string? MatchLocalLyrics(string audioPath, string? title = null, string? artist = null)
	{
		string directoryName = Path.GetDirectoryName(audioPath);
		string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(audioPath);
		if (string.IsNullOrEmpty(directoryName))
		{
			return null;
		}
		string text = Path.Combine(directoryName, fileNameWithoutExtension + ".lrc");
		if (File.Exists(text))
		{
			return text;
		}
		string text2 = Path.Combine(directoryName, "lyrics");
		if (Directory.Exists(text2))
		{
			string text3 = Path.Combine(text2, fileNameWithoutExtension + ".lrc");
			if (File.Exists(text3))
			{
				return text3;
			}
		}
		string b = Normalize(fileNameWithoutExtension);
		List<string> list = Directory.EnumerateFiles(directoryName, "*.lrc", SearchOption.AllDirectories).ToList();
		if (Directory.Exists(text2))
		{
			list.AddRange(Directory.EnumerateFiles(text2, "*.lrc", SearchOption.AllDirectories));
		}
		foreach (string item in list)
		{
			if (string.Equals(Normalize(Path.GetFileNameWithoutExtension(item)), b, StringComparison.OrdinalIgnoreCase))
			{
				return item;
			}
			if (!string.IsNullOrEmpty(title) && !string.IsNullOrEmpty(artist))
			{
				LyricDocument lyricDocument = ParseLrc(File.ReadAllText(item, Encoding.UTF8));
				if (lyricDocument.Title.Length > 0 && string.Equals(Normalize(lyricDocument.Title), Normalize(title), StringComparison.OrdinalIgnoreCase) && string.Equals(Normalize(lyricDocument.Artist), Normalize(artist), StringComparison.OrdinalIgnoreCase))
				{
					return item;
				}
			}
		}
		return null;
	}

	public async Task<LyricDocument?> SearchOnlineAsync(string title, string artist, CancellationToken ct = default(CancellationToken))
	{
		_ = 1;
		try
		{
			string requestUri = "https://lrclib.net/api/get?artist_name=" + Uri.EscapeDataString(artist) + "&track_name=" + Uri.EscapeDataString(title);
			using HttpResponseMessage resp = await Http.GetAsync(requestUri, ct).ConfigureAwait(continueOnCapturedContext: false);
			if (!resp.IsSuccessStatusCode)
			{
				return null;
			}
			using JsonDocument jsonDocument = JsonDocument.Parse(await resp.Content.ReadAsStringAsync(ct).ConfigureAwait(continueOnCapturedContext: false));
			if (jsonDocument.RootElement.TryGetProperty("syncedLyrics", out var value))
			{
				string? text = value.GetString();
				if (!string.IsNullOrWhiteSpace(text))
				{
					LyricDocument lyricDocument = ParseLrc(text);
					lyricDocument.Source = "lrclib";
					lyricDocument.Title = title;
					lyricDocument.Artist = artist;
					return lyricDocument;
				}
			}
			if (jsonDocument.RootElement.TryGetProperty("plainLyrics", out var plainProp))
			{
				string? plainText = plainProp.GetString();
				if (!string.IsNullOrWhiteSpace(plainText))
				{
					LyricDocument lyricDocument = ParseLrc(plainText);
					lyricDocument.Source = "lrclib";
					lyricDocument.Title = title;
					lyricDocument.Artist = artist;
					return lyricDocument;
				}
			}
			return null;
		}
		catch
		{
			return null;
		}
	}

	private static string SanitizeArtistForSearch(string? artist)
	{
		if (string.IsNullOrWhiteSpace(artist)) return string.Empty;
		string a = artist.Trim();
		if (a.Equals("未知艺术家", StringComparison.OrdinalIgnoreCase) ||
			a.Equals("未知歌手", StringComparison.OrdinalIgnoreCase) ||
			a.Equals("Unknown Artist", StringComparison.OrdinalIgnoreCase) ||
			a.Equals("Unknown", StringComparison.OrdinalIgnoreCase) ||
			a.Equals("<unknown>", StringComparison.OrdinalIgnoreCase))
		{
			return string.Empty;
		}
		return a;
	}

	public LyricDocument? ParseNeteaseLyricJson(string json)
	{
		if (string.IsNullOrWhiteSpace(json)) return null;
		using JsonDocument jsonDocument = JsonDocument.Parse(json);
		string? TryGetField(string propName)
		{
			if (jsonDocument.RootElement.TryGetProperty(propName, out var container) &&
				container.ValueKind == JsonValueKind.Object &&
				container.TryGetProperty("lyric", out var lyrProp))
			{
				return lyrProp.GetString();
			}
			return null;
		}

		string? yrcRaw = TryGetField("yrc");
		string? lrcRaw = TryGetField("lrc");
		string? tlyricRaw = TryGetField("tlyric");
		string? romaRaw = TryGetField("romalrc");

		// 1. 优先解析原歌词（逐字 yrc 优先，其次逐行 lrc），绝不单独只展示翻译而丢弃原歌词
		LyricDocument? primary = null;
		if (!string.IsNullOrWhiteSpace(yrcRaw))
		{
			var yrcDoc = ParseYrc(yrcRaw);
			if (yrcDoc.Lines.Any(l => !string.IsNullOrWhiteSpace(l.Text)))
			{
				primary = yrcDoc;
			}
		}
		if (primary == null && !string.IsNullOrWhiteSpace(lrcRaw))
		{
			var lrcDoc = ParseLrc(lrcRaw);
			if (lrcDoc.Lines.Any(l => !string.IsNullOrWhiteSpace(l.Text)))
			{
				primary = lrcDoc;
			}
		}

		// 2. 若原歌词存在，按时间轴合并 tlyric 翻译（若无 tlyric 再合并 romalrc 罗马音）
		if (primary != null)
		{
			string? transRaw = !string.IsNullOrWhiteSpace(tlyricRaw) ? tlyricRaw : romaRaw;
			if (!string.IsNullOrWhiteSpace(transRaw))
			{
				var transDoc = ParseLrc(transRaw);
				MergeTranslationDoc(primary, transDoc);
			}
			primary.Source = "netease";
			return primary;
		}

		// 3. 仅当 yrc 与 lrc 均为空时（如部分歌曲歌词仅存放在 tlyric 中），才回退使用 tlyric / romalrc
		foreach (string? fallbackRaw in new[] { tlyricRaw, romaRaw })
		{
			if (string.IsNullOrWhiteSpace(fallbackRaw)) continue;
			LyricDocument doc = ParseLrc(fallbackRaw);
			if (doc.Lines.Any(l => !string.IsNullOrWhiteSpace(l.Text)))
			{
				doc.Source = "netease";
				return doc;
			}
		}
		return null;
	}

	private static void MergeTranslationDoc(LyricDocument primary, LyricDocument transDoc)
	{
		var transLines = transDoc.Lines.Where(l => !string.IsNullOrWhiteSpace(l.DisplayOriginal)).ToList();
		if (transLines.Count == 0 || primary.Lines.Count == 0) return;

		foreach (var orig in primary.Lines)
		{
			if (string.IsNullOrWhiteSpace(orig.DisplayOriginal)) continue;
			LyricLine? best = null;
			double bestDelta = 400.0;
			foreach (var tr in transLines)
			{
				double d = Math.Abs((orig.Time - tr.Time).TotalMilliseconds);
				if (d <= bestDelta)
				{
					bestDelta = d;
					best = tr;
				}
			}
			if (best != null)
			{
				string trText = best.DisplayOriginal.Trim();
				if (!string.Equals(orig.DisplayOriginal.Trim(), trText, StringComparison.OrdinalIgnoreCase))
				{
					orig.OriginalText = orig.DisplayOriginal;
					orig.Translation = trText;
					orig.Text = $"{orig.OriginalText}\n{trText}";
				}
			}
		}
	}

	public async Task<LyricDocument?> GetNeteaseLyricByIdAsync(long songId, CancellationToken ct = default(CancellationToken))
	{
		try
		{
			using var req = new HttpRequestMessage(HttpMethod.Get, "https://music.163.com/api/song/lyric/v1?id=" + songId + "&yv=1&lv=1&kv=1&tv=-1&rv=-1");
			req.Headers.Referrer = new Uri("https://music.163.com/");
			req.Headers.TryAddWithoutValidation("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");
			using HttpResponseMessage lyricResp = await Http.SendAsync(req, ct).ConfigureAwait(continueOnCapturedContext: false);
			if (!lyricResp.IsSuccessStatusCode)
			{
				return null;
			}
			string json = await lyricResp.Content.ReadAsStringAsync(ct).ConfigureAwait(continueOnCapturedContext: false);
			return ParseNeteaseLyricJson(json);
		}
		catch
		{
			return null;
		}
	}

	public async Task<LyricDocument?> SearchNeteaseAsync(string title, string artist, CancellationToken ct = default(CancellationToken))
	{
		try
		{
			string cleanTitle = (title ?? string.Empty).Trim();
			if (string.IsNullOrWhiteSpace(cleanTitle)) return null;
			string cleanArtist = SanitizeArtistForSearch(artist);
			string query = string.IsNullOrEmpty(cleanArtist) ? cleanTitle : $"{cleanTitle} {cleanArtist}";

			using var req = new HttpRequestMessage(HttpMethod.Get, "https://music.163.com/api/cloudsearch/pc?s=" + Uri.EscapeDataString(query) + "&type=1&limit=5");
			req.Headers.Referrer = new Uri("https://music.163.com/");
			req.Headers.TryAddWithoutValidation("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");

			using HttpResponseMessage searchResp = await Http.SendAsync(req, ct).ConfigureAwait(continueOnCapturedContext: false);
			if (!searchResp.IsSuccessStatusCode)
			{
				return null;
			}
			using JsonDocument searchDoc = JsonDocument.Parse(await searchResp.Content.ReadAsStringAsync(ct).ConfigureAwait(continueOnCapturedContext: false));
			if (!searchDoc.RootElement.TryGetProperty("result", out var value) || !value.TryGetProperty("songs", out var value2) || value2.ValueKind != JsonValueKind.Array)
			{
				return null;
			}

			var prioritizedIds = new List<long>();
			var fallbackIds = new List<long>();
			string normArtist = Normalize(cleanArtist);

			foreach (JsonElement item in value2.EnumerateArray())
			{
				if (!item.TryGetProperty("id", out var value3) || !value3.TryGetInt64(out var songId) || songId <= 0)
				{
					continue;
				}
				bool artistMatched = false;
				if (!string.IsNullOrEmpty(normArtist))
				{
					if (item.TryGetProperty("ar", out var value5) && value5.ValueKind == JsonValueKind.Array)
					{
						foreach (JsonElement item2 in value5.EnumerateArray())
						{
							if (item2.TryGetProperty("name", out var value6) && string.Equals(Normalize(value6.GetString() ?? ""), normArtist, StringComparison.OrdinalIgnoreCase))
							{
								artistMatched = true;
								break;
							}
						}
					}
					if (!artistMatched && item.TryGetProperty("artists", out var valueOldAr) && valueOldAr.ValueKind == JsonValueKind.Array)
					{
						foreach (JsonElement itemOld in valueOldAr.EnumerateArray())
						{
							if (itemOld.TryGetProperty("name", out var nameProp) && string.Equals(Normalize(nameProp.GetString() ?? ""), normArtist, StringComparison.OrdinalIgnoreCase))
							{
								artistMatched = true;
								break;
							}
						}
					}
				}
				if (artistMatched) prioritizedIds.Add(songId);
				else fallbackIds.Add(songId);
			}

			foreach (long id in prioritizedIds.Concat(fallbackIds))
			{
				LyricDocument? doc = await GetNeteaseLyricByIdAsync(id, ct).ConfigureAwait(continueOnCapturedContext: false);
				if (doc != null && doc.Lines.Any(l => !string.IsNullOrWhiteSpace(l.Text)))
				{
					doc.Title = cleanTitle;
					doc.Artist = cleanArtist;
					return doc;
				}
			}
			return null;
		}
		catch
		{
			return null;
		}
	}

	public Track? MatchLyricToTrack(string lrcPath, IReadOnlyList<Track> tracks)
	{
		string text = Normalize(Path.GetFileNameWithoutExtension(lrcPath));
		if (text.Length == 0)
		{
			return null;
		}
		Track result = null;
		double num = 0.0;
		foreach (Track track in tracks)
		{
			if (string.IsNullOrEmpty(track.FilePath))
			{
				continue;
			}
			double num2 = 0.0;
			string text2 = Normalize(Path.GetFileNameWithoutExtension(track.FilePath));
			if (text2.Length > 0)
			{
				if (text2 == text)
				{
					num2 += 4.0;
				}
				else if (text2.Contains(text) || text.Contains(text2))
				{
					num2 += 2.0;
				}
			}
			string text3 = Normalize(track.DisplayTitle);
			string text4 = Normalize(track.Artist);
			if (text3.Length > 0)
			{
				if (text3 == text)
				{
					num2 += 3.0;
				}
				else if (text3.Contains(text) || text.Contains(text3))
				{
					num2 += 1.5;
				}
				if (text4.Length > 0)
				{
					string text5 = text3 + text4;
					if (text5 == text)
					{
						num2 += 3.0;
					}
					else if (text5.Contains(text) || text.Contains(text5))
					{
						num2++;
					}
				}
			}
			if (num2 > num)
			{
				num = num2;
				result = track;
			}
		}
		if (!(num >= 1.5))
		{
			return null;
		}
		return result;
	}

	public async Task<LyricDocument?> SearchOnlineWithFallbackAsync(string title, string artist, CancellationToken ct = default(CancellationToken))
	{
		if (string.IsNullOrWhiteSpace(title)) return null;
		// 1. 优先网易云音乐歌词接口
		LyricDocument? doc = await SearchNeteaseAsync(title, artist, ct).ConfigureAwait(continueOnCapturedContext: false);
		if (doc != null && doc.Lines.Count > 0)
		{
			return doc;
		}
		// 2. 最后使用 lrclib 在线歌词 API 兜底
		doc = await SearchOnlineAsync(title, artist, ct).ConfigureAwait(continueOnCapturedContext: false);
		if (doc != null && doc.Lines.Count > 0)
		{
			return doc;
		}
		return null;
	}

	public LyricDocument? LoadLocalLyrics(string audioPath, string? embeddedLyrics = null, string? title = null, string? artist = null)
	{
		// 1. 读取内嵌歌词（若传入为空则尝试直接从音频标签读取）
		string? embText = embeddedLyrics;
		if (string.IsNullOrWhiteSpace(embText) && !string.IsNullOrWhiteSpace(audioPath) && File.Exists(audioPath))
		{
			try
			{
				var atl = new ATL.Track(audioPath);
				embText = atl.Lyrics?.UnsynchronizedLyrics;
				if (string.IsNullOrWhiteSpace(embText) && atl.AdditionalFields != null && atl.AdditionalFields.TryGetValue("LYRICS", out string? extra))
				{
					embText = extra;
				}
			}
			catch { }
		}

		LyricDocument? embeddedDoc = null;
		if (!string.IsNullOrWhiteSpace(embText))
		{
			embeddedDoc = ParseLrc(embText);
			embeddedDoc.Source = "embedded";
		}

		// 2. 匹配同文件夹随附的 .lrc 文件（结合文件名、歌名与艺人）
		string? localLrcPath = MatchLocalLyrics(audioPath, title, artist);
		LyricDocument? localFileDoc = null;
		if (localLrcPath != null)
		{
			try
			{
				localFileDoc = ParseLrc(File.ReadAllText(localLrcPath, Encoding.UTF8));
				localFileDoc.Source = "local";
			}
			catch { }
		}

		if (embeddedDoc != null && embeddedDoc.Lines.Count > 0)
		{
			bool embHasSync = embeddedDoc.Lines.Any(l => l.Time > TimeSpan.Zero);
			if (!embHasSync && localFileDoc != null && localFileDoc.Lines.Any(l => l.Time > TimeSpan.Zero))
			{
				return localFileDoc;
			}
			return embeddedDoc;
		}

		if (localFileDoc != null && localFileDoc.Lines.Count > 0)
		{
			return localFileDoc;
		}

		return embeddedDoc ?? localFileDoc;
	}

	public static string Normalize(string s)
	{
		return Regex.Replace(Regex.Replace(Regex.Replace(s, "[\\(\\[【].*?[\\)\\]】]", ""), "\\d+", ""), "[^\\p{L}\\p{N}]", "").Trim().ToLowerInvariant();
	}
}
