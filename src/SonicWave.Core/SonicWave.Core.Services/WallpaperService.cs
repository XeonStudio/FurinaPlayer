using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace SonicWave.Core.Services;

public class WallpaperService
{
	public sealed record BingWallpaper(string Url, string Copyright, string Date);

	private static readonly HttpClient Http = CreateClient();

	private readonly string _cacheDir;

	public string CacheDirectory => _cacheDir;

	private static HttpClient CreateClient()
	{
		HttpClient httpClient = new HttpClient();
		httpClient.Timeout = TimeSpan.FromSeconds(10.0);
		httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/126.0 Safari/537.36 FurinaPlayer/1.0");
		httpClient.DefaultRequestHeaders.Accept.ParseAdd("application/json");
		return httpClient;
	}

	public WallpaperService(string? cacheDir = null)
	{
		_cacheDir = cacheDir ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "FurinaPlayer", "wallpapers");
		Directory.CreateDirectory(_cacheDir);
	}

	public async Task<List<BingWallpaper>> GetBingWallpapersAsync(int count = 1, CancellationToken ct = default(CancellationToken))
	{
		List<BingWallpaper> result = new List<BingWallpaper>();
		try
		{
			string requestUri = $"https://www.bing.com/HPImageArchive.aspx?format=js&idx=0&n={count}&mkt=zh-CN";
			using HttpResponseMessage resp = await Http.GetAsync(requestUri, ct).ConfigureAwait(continueOnCapturedContext: false);
			if (!resp.IsSuccessStatusCode)
			{
				return result;
			}
			using JsonDocument jsonDocument = JsonDocument.Parse(await resp.Content.ReadAsStringAsync(ct).ConfigureAwait(continueOnCapturedContext: false));
			foreach (JsonElement item in jsonDocument.RootElement.GetProperty("images").EnumerateArray())
			{
				string text = item.GetProperty("url").GetString() ?? "";
				string copyright = (item.TryGetProperty("copyright", out var value) ? (value.GetString() ?? "") : "");
				string date = (item.TryGetProperty("startdate", out var value2) ? (value2.GetString() ?? "") : "");
				result.Add(new BingWallpaper("https://www.bing.com" + text, copyright, date));
			}
		}
		catch
		{
		}
		return result;
	}

	public async Task<string?> DownloadBingWallpaperAsync(BingWallpaper info, CancellationToken ct = default(CancellationToken))
	{
		_ = 4;
		try
		{
			string text = Path.GetExtension(new Uri(info.Url).AbsolutePath);
			if (string.IsNullOrEmpty(text))
			{
				text = ".jpg";
			}
			string file = Path.Combine(_cacheDir, "bing_" + info.Date + text);
			if (File.Exists(file))
			{
				return file;
			}
			using HttpResponseMessage resp = await Http.GetAsync(info.Url, ct).ConfigureAwait(continueOnCapturedContext: false);
			if (!resp.IsSuccessStatusCode)
			{
				return null;
			}
			string result;
			await using (Stream stream = await resp.Content.ReadAsStreamAsync(ct).ConfigureAwait(continueOnCapturedContext: false))
			{
				string text2;
				await using (FileStream fs = File.Create(file))
				{
					await stream.CopyToAsync(fs, ct).ConfigureAwait(continueOnCapturedContext: false);
					text2 = file;
				}
				result = text2;
			}
			return result;
		}
		catch
		{
			return null;
		}
	}

	public string ImportLocalWallpaper(string sourcePath)
	{
		string extension = Path.GetExtension(sourcePath);
		string text = Path.Combine(_cacheDir, $"local_{DateTime.Now:yyyyMMdd_HHmmss}{extension}");
		File.Copy(sourcePath, text, overwrite: true);
		return text;
	}

	public List<string> GetHistory()
	{
		return (from f in Directory.EnumerateFiles(_cacheDir, "*.*")
			where f.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase) || f.EndsWith(".jpeg", StringComparison.OrdinalIgnoreCase) || f.EndsWith(".png", StringComparison.OrdinalIgnoreCase) || f.EndsWith(".bmp", StringComparison.OrdinalIgnoreCase)
			orderby File.GetLastWriteTime(f) descending
			select f).ToList();
	}
}
