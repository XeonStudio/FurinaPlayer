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
using SonicWave.Core.Models;

namespace SonicWave.Core.Services;

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
				LyricLine lyricLine = new LyricLine
				{
					Time = TimeSpan.FromMilliseconds(num * 60000 + num2 * 1000 + num3),
					Text = text3
				};
				if (text3.Contains('<') && text3.Contains('>'))
				{
					lyricLine.WordTimings = ParseWordTimings(text3);
				}
				lyricDocument.Lines.Add(lyricLine);
			}
		}
		lyricDocument.Lines.Sort((LyricLine a, LyricLine b) => a.Time.CompareTo(b.Time));
		return lyricDocument;
	}

	private static List<(int CharIndex, int StartMs)>? ParseWordTimings(string text)
	{
		List<(int, int)> list = new List<(int, int)>();
		int num = 0;
		foreach (Match item in Regex.Matches(text, "<(\\d{1,3}):(\\d{1,2})(?:[.:](\\d{1,3}))?>"))
		{
			int num2 = int.Parse(item.Groups[1].Value) * 60000 + int.Parse(item.Groups[2].Value) * 1000;
			if (item.Groups[3].Success && int.TryParse(item.Groups[3].Value, out var result))
			{
				num2 += ((item.Groups[3].Value.Length == 1) ? (result * 100) : ((item.Groups[3].Value.Length == 2) ? (result * 10) : result));
			}
			list.Add((num, num2));
			num += item.Value.Length;
		}
		if (list.Count <= 0)
		{
			return null;
		}
		return list;
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
				string text = value.GetString();
				if (text != null && text.Length > 0)
				{
					LyricDocument lyricDocument = ParseLrc(text);
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

	public async Task<LyricDocument?> SearchNeteaseAsync(string title, string artist, CancellationToken ct = default(CancellationToken))
	{
		_ = 3;
		try
		{
			string text = Uri.EscapeDataString((title + " " + artist).Trim());
			using HttpResponseMessage searchResp = await Http.GetAsync("https://music.163.com/api/search/get/web?s=" + text + "&type=1&limit=5", ct).ConfigureAwait(continueOnCapturedContext: false);
			if (!searchResp.IsSuccessStatusCode)
			{
				return null;
			}
			using JsonDocument searchDoc = JsonDocument.Parse(await searchResp.Content.ReadAsStringAsync(ct).ConfigureAwait(continueOnCapturedContext: false));
			if (!searchDoc.RootElement.TryGetProperty("result", out var value) || !value.TryGetProperty("songs", out var value2) || value2.ValueKind != JsonValueKind.Array)
			{
				return null;
			}
			long num = 0L;
			foreach (JsonElement item in value2.EnumerateArray())
			{
				if (!item.TryGetProperty("id", out var value3) || !value3.TryGetInt64(out var value4) || value4 <= 0)
				{
					continue;
				}
				if (item.TryGetProperty("artists", out var value5) && value5.ValueKind == JsonValueKind.Array)
				{
					foreach (JsonElement item2 in value5.EnumerateArray())
					{
						if (item2.TryGetProperty("name", out var value6) && string.Equals(Normalize(value6.GetString() ?? ""), Normalize(artist), StringComparison.OrdinalIgnoreCase))
						{
							num = value4;
							break;
						}
					}
					if (num > 0)
					{
						break;
					}
				}
				if (num == 0L)
				{
					num = value4;
				}
			}
			if (num <= 0)
			{
				return null;
			}
			using HttpResponseMessage lyricResp = await Http.GetAsync("https://music.163.com/api/song/lyric?id=" + num + "&lv=1&kv=1&tv=-1", ct).ConfigureAwait(continueOnCapturedContext: false);
			if (!lyricResp.IsSuccessStatusCode)
			{
				return null;
			}
			using JsonDocument jsonDocument = JsonDocument.Parse(await lyricResp.Content.ReadAsStringAsync(ct).ConfigureAwait(continueOnCapturedContext: false));
			if (jsonDocument.RootElement.TryGetProperty("lrc", out var value7) && value7.TryGetProperty("lyric", out var value8))
			{
				string text2 = value8.GetString();
				if (text2 != null && text2.Length > 0)
				{
					LyricDocument lyricDocument = ParseLrc(text2);
					if (lyricDocument.Lines.Count == 0)
					{
						return null;
					}
					lyricDocument.Source = "netease";
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

	public LyricDocument? LoadLocalLyrics(string audioPath, string? embeddedLyrics = null)
	{
		string text = MatchLocalLyrics(audioPath);
		if (text != null)
		{
			try
			{
				LyricDocument lyricDocument = ParseLrc(File.ReadAllText(text, Encoding.UTF8));
				lyricDocument.Source = "local";
				return lyricDocument;
			}
			catch
			{
			}
		}
		if (!string.IsNullOrWhiteSpace(embeddedLyrics))
		{
			LyricDocument lyricDocument2 = ParseLrc(embeddedLyrics);
			lyricDocument2.Source = "embedded";
			return lyricDocument2;
		}
		return null;
	}

	public static string Normalize(string s)
	{
		return Regex.Replace(Regex.Replace(Regex.Replace(s, "[\\(\\[【].*?[\\)\\]】]", ""), "\\d+", ""), "[^\\p{L}\\p{N}]", "").Trim().ToLowerInvariant();
	}
}
