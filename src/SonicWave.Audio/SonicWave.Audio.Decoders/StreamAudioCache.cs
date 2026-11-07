using System;
using System.Collections.Concurrent;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading;

namespace SonicWave.Audio.Decoders;

public static class StreamAudioCache
{
	private static readonly HttpClient s_httpClient = CreateHttpClient();
	private static readonly ConcurrentDictionary<string, object> s_downloadLocks = new(StringComparer.OrdinalIgnoreCase);

	private static HttpClient CreateHttpClient()
	{
		var handler = new HttpClientHandler
		{
			AllowAutoRedirect = true,
			AutomaticDecompression = DecompressionMethods.All
		};
		var client = new HttpClient(handler)
		{
			Timeout = TimeSpan.FromSeconds(60)
		};
		client.DefaultRequestHeaders.TryAddWithoutValidation(
			"User-Agent",
			"Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/124.0.0.0 Safari/537.36");
		client.DefaultRequestHeaders.Referrer = new Uri("https://music.163.com/");
		return client;
	}

	public static bool IsNetworkUrl(string? path)
	{
		if (string.IsNullOrWhiteSpace(path))
		{
			return false;
		}
		return path.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
			|| path.StartsWith("https://", StringComparison.OrdinalIgnoreCase);
	}

	public static string GetExpectedCacheFilePath(string url)
	{
		string trimmedUrl = (url ?? string.Empty).Trim();
		string ext = GetAudioExtension(trimmedUrl);
		string hash = ComputeSha256Hex(trimmedUrl);
		string cacheDir = Path.Combine(Path.GetTempPath(), "FurinaPlayer_StreamCache");
		Directory.CreateDirectory(cacheDir);
		return Path.Combine(cacheDir, $"{hash}{ext}");
	}

	public static bool TryGetOrDownloadCachedFile(string url, out string localFilePath, int timeoutMs = 20000)
	{
		localFilePath = string.Empty;
		if (string.IsNullOrWhiteSpace(url))
		{
			return false;
		}

		if (!IsNetworkUrl(url))
		{
			if (File.Exists(url))
			{
				localFilePath = url;
				return true;
			}
			return false;
		}

		try
		{
			string trimmedUrl = url.Trim();
			string ext = GetAudioExtension(trimmedUrl);
			string hash = ComputeSha256Hex(trimmedUrl);
			string cacheDir = Path.Combine(Path.GetTempPath(), "FurinaPlayer_StreamCache");
			Directory.CreateDirectory(cacheDir);

			string cachedPath = Path.Combine(cacheDir, $"{hash}{ext}");
			if (IsValidCachedFile(cachedPath))
			{
				localFilePath = cachedPath;
				return true;
			}

			object lockObj = s_downloadLocks.GetOrAdd(cachedPath, _ => new object());
			lock (lockObj)
			{
				if (IsValidCachedFile(cachedPath))
				{
					localFilePath = cachedPath;
					return true;
				}

				using var cts = new CancellationTokenSource(Math.Max(2000, timeoutMs));
				using var request = new HttpRequestMessage(HttpMethod.Get, trimmedUrl);
				request.Headers.Referrer = new Uri("https://music.163.com/");

				using HttpResponseMessage response = s_httpClient
					.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cts.Token)
					.GetAwaiter()
					.GetResult();

				if (!response.IsSuccessStatusCode)
				{
					return false;
				}

				string? finalPath = response.RequestMessage?.RequestUri?.AbsolutePath;
				if (!string.IsNullOrEmpty(finalPath) && finalPath.EndsWith("/404", StringComparison.OrdinalIgnoreCase))
				{
					return false;
				}

				string? mediaType = response.Content.Headers.ContentType?.MediaType;
				if (!string.IsNullOrEmpty(mediaType) && mediaType.Contains("text/html", StringComparison.OrdinalIgnoreCase))
				{
					return false;
				}

				string tempFile = Path.Combine(cacheDir, $"{hash}.{Guid.NewGuid():N}.tmp");
				try
				{
					using (Stream netStream = response.Content.ReadAsStreamAsync(cts.Token).GetAwaiter().GetResult())
					using (FileStream fs = new FileStream(tempFile, FileMode.Create, FileAccess.Write, FileShare.None, 65536))
					{
						netStream.CopyTo(fs);
						fs.Flush();
					}

					var fi = new FileInfo(tempFile);
					if (!fi.Exists || fi.Length <= 2048)
					{
						TryDeleteFile(tempFile);
						return false;
					}

					File.Move(tempFile, cachedPath, overwrite: true);
					localFilePath = cachedPath;
					return true;
				}
				catch
				{
					TryDeleteFile(tempFile);
					return false;
				}
			}
		}
		catch
		{
			return false;
		}
	}

	private static bool IsValidCachedFile(string path)
	{
		try
		{
			if (!File.Exists(path))
			{
				return false;
			}
			return new FileInfo(path).Length > 2048;
		}
		catch
		{
			return false;
		}
	}

	private static string GetAudioExtension(string url)
	{
		try
		{
			if (Uri.TryCreate(url, UriKind.Absolute, out Uri? uri))
			{
				string ext = Path.GetExtension(uri.AbsolutePath).ToLowerInvariant();
				return ext switch
				{
					".flac" => ".flac",
					".wav" => ".wav",
					".m4a" => ".m4a",
					".ogg" => ".ogg",
					".mp3" => ".mp3",
					_ => ".mp3"
				};
			}
		}
		catch
		{
		}
		return ".mp3";
	}

	private static string ComputeSha256Hex(string input)
	{
		byte[] bytes = SHA256.HashData(Encoding.UTF8.GetBytes(input));
		return Convert.ToHexString(bytes).ToLowerInvariant();
	}

	private static void TryDeleteFile(string path)
	{
		try
		{
			if (File.Exists(path))
			{
				File.Delete(path);
			}
		}
		catch
		{
		}
	}
}
