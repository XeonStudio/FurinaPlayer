using System;
using System.Collections.Concurrent;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SonicWave.Audio.Decoders;

public static class StreamAudioCache
{
	private static readonly HttpClient s_httpClient = CreateHttpClient();
	private static readonly ConcurrentDictionary<string, SemaphoreSlim> s_downloadLocks = new(StringComparer.OrdinalIgnoreCase);
	private static readonly ConcurrentDictionary<string, DateTime> s_recentFailures = new(StringComparer.OrdinalIgnoreCase);
	private static readonly TimeSpan s_failureCooldown = TimeSpan.FromSeconds(12);

	private static HttpClient CreateHttpClient()
	{
		var handler = new HttpClientHandler
		{
			AllowAutoRedirect = true,
			AutomaticDecompression = DecompressionMethods.All
		};
		var client = new HttpClient(handler)
		{
			Timeout = TimeSpan.FromSeconds(15)
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

	/// <summary>
	/// 快速甄别非音频流的纯网页 URL (如专辑主页、艺人主页、歌单页面等)，杜绝送入音频引擎引起同步请求卡死 (AppHangB1)
	/// </summary>
	public static bool IsNonPlayableWebUrl(string? url)
	{
		if (string.IsNullOrWhiteSpace(url)) return false;
		if (!IsNetworkUrl(url)) return false;

		string u = url.Trim();

		// 明确属于单曲流媒体音频直链或带有扩展名文件的白名单特征
		if (u.Contains("/song/media/outer/url", StringComparison.OrdinalIgnoreCase) ||
		    u.Contains("/enhanc/player/url", StringComparison.OrdinalIgnoreCase) ||
		    u.EndsWith(".mp3", StringComparison.OrdinalIgnoreCase) ||
		    u.EndsWith(".flac", StringComparison.OrdinalIgnoreCase) ||
		    u.EndsWith(".m4a", StringComparison.OrdinalIgnoreCase) ||
		    u.EndsWith(".ogg", StringComparison.OrdinalIgnoreCase) ||
		    u.EndsWith(".wav", StringComparison.OrdinalIgnoreCase) ||
		    u.EndsWith(".aac", StringComparison.OrdinalIgnoreCase))
		{
			// 如果不是明确的网页结构，则判定为有效音频流
			if (!u.Contains("music.163.com/album") && !u.Contains("music.163.com/artist") && !u.Contains("music.163.com/playlist"))
			{
				return false;
			}
		}

		// 网页地址黑名单
		if (u.Contains("music.163.com/album", StringComparison.OrdinalIgnoreCase) ||
		    u.Contains("music.163.com/artist", StringComparison.OrdinalIgnoreCase) ||
		    u.Contains("music.163.com/playlist", StringComparison.OrdinalIgnoreCase) ||
		    u.Contains("y.qq.com/n/ryqq/albumDetail", StringComparison.OrdinalIgnoreCase) ||
		    u.Contains("y.qq.com/n/ryqq/singer", StringComparison.OrdinalIgnoreCase) ||
		    u.Contains("y.qq.com/n/ryqq/playlist", StringComparison.OrdinalIgnoreCase) ||
		    u.EndsWith(".html", StringComparison.OrdinalIgnoreCase) ||
		    u.EndsWith(".htm", StringComparison.OrdinalIgnoreCase))
		{
			return true;
		}

		return false;
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

	/// <summary>
	/// 零网络耗时快速检测缓存音频文件是否已在本地就绪。
	/// </summary>
	public static bool TryGetCachedFilePathIfReady(string? url, out string localFilePath)
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
		if (IsNonPlayableWebUrl(url))
		{
			return false;
		}
		try
		{
			string trimmedUrl = url.Trim();
			string ext = GetAudioExtension(trimmedUrl);
			string hash = ComputeSha256Hex(trimmedUrl);
			string cacheDir = Path.Combine(Path.GetTempPath(), "FurinaPlayer_StreamCache");
			string cachedPath = Path.Combine(cacheDir, $"{hash}{ext}");
			if (IsValidCachedFile(cachedPath))
			{
				s_recentFailures.TryRemove(hash, out _);
				localFilePath = cachedPath;
				return true;
			}
		}
		catch
		{
		}
		return false;
	}

	private static bool IsInFailureCooldown(string hash)
	{
		if (s_recentFailures.TryGetValue(hash, out DateTime cooldownUntil))
		{
			if (DateTime.UtcNow < cooldownUntil)
			{
				return true;
			}
			s_recentFailures.TryRemove(hash, out _);
		}
		return false;
	}

	private static void RecordDownloadFailure(string hash, CancellationToken callerToken)
	{
		if (!callerToken.IsCancellationRequested)
		{
			s_recentFailures[hash] = DateTime.UtcNow + s_failureCooldown;
		}
	}

	/// <summary>
	/// 异步非阻塞预取或下载网络音频流至本地缓存，带严格超时控制与异常保护，避免阻塞主线程
	/// </summary>
	public static async Task<(bool success, string localFilePath)> TryGetOrDownloadCachedFileAsync(
		string url, int timeoutMs = 8000, CancellationToken cancellationToken = default)
	{
		if (string.IsNullOrWhiteSpace(url)) return (false, string.Empty);
		if (!IsNetworkUrl(url))
		{
			if (File.Exists(url)) return (true, url);
			return (false, string.Empty);
		}

		if (IsNonPlayableWebUrl(url))
		{
			return (false, string.Empty);
		}

		string trimmedUrl = url.Trim();
		string hash = ComputeSha256Hex(trimmedUrl);
		try
		{
			string ext = GetAudioExtension(trimmedUrl);
			string cacheDir = Path.Combine(Path.GetTempPath(), "FurinaPlayer_StreamCache");
			Directory.CreateDirectory(cacheDir);

			string cachedPath = Path.Combine(cacheDir, $"{hash}{ext}");
			if (IsValidCachedFile(cachedPath))
			{
				s_recentFailures.TryRemove(hash, out _);
				return (true, cachedPath);
			}

			if (IsInFailureCooldown(hash))
			{
				return (false, string.Empty);
			}

			using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
			cts.CancelAfter(Math.Clamp(timeoutMs, 1000, 15000));

			SemaphoreSlim downloadLock = s_downloadLocks.GetOrAdd(hash, _ => new SemaphoreSlim(1, 1));
			await downloadLock.WaitAsync(cts.Token).ConfigureAwait(false);
			try
			{
				if (IsValidCachedFile(cachedPath))
				{
					s_recentFailures.TryRemove(hash, out _);
					return (true, cachedPath);
				}

				if (IsInFailureCooldown(hash))
				{
					return (false, string.Empty);
				}

				using var request = new HttpRequestMessage(HttpMethod.Get, trimmedUrl);
				request.Headers.Referrer = new Uri("https://music.163.com/");

				using HttpResponseMessage response = await s_httpClient
					.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cts.Token)
					.ConfigureAwait(false);

				if (!response.IsSuccessStatusCode)
				{
					RecordDownloadFailure(hash, cancellationToken);
					return (false, string.Empty);
				}

				string? finalPath = response.RequestMessage?.RequestUri?.AbsolutePath;
				if (!string.IsNullOrEmpty(finalPath) && finalPath.EndsWith("/404", StringComparison.OrdinalIgnoreCase))
				{
					RecordDownloadFailure(hash, cancellationToken);
					return (false, string.Empty);
				}

				string? mediaType = response.Content.Headers.ContentType?.MediaType;
				if (!string.IsNullOrEmpty(mediaType) && mediaType.Contains("text/html", StringComparison.OrdinalIgnoreCase))
				{
					RecordDownloadFailure(hash, cancellationToken);
					return (false, string.Empty);
				}

				string tempFile = Path.Combine(cacheDir, $"{hash}.{Guid.NewGuid():N}.tmp");
				try
				{
					using (Stream netStream = await response.Content.ReadAsStreamAsync(cts.Token).ConfigureAwait(false))
					using (FileStream fs = new FileStream(tempFile, FileMode.Create, FileAccess.Write, FileShare.None, 65536, useAsync: true))
					{
						await netStream.CopyToAsync(fs, 65536, cts.Token).ConfigureAwait(false);
						await fs.FlushAsync(cts.Token).ConfigureAwait(false);
					}

					var fi = new FileInfo(tempFile);
					if (!fi.Exists || fi.Length <= 2048)
					{
						TryDeleteFile(tempFile);
						RecordDownloadFailure(hash, cancellationToken);
						return (false, string.Empty);
					}

					File.Move(tempFile, cachedPath, overwrite: true);
					s_recentFailures.TryRemove(hash, out _);
					return (true, cachedPath);
				}
				catch
				{
					TryDeleteFile(tempFile);
					RecordDownloadFailure(hash, cancellationToken);
					return (false, string.Empty);
				}
			}
			finally
			{
				downloadLock.Release();
			}
		}
		catch
		{
			RecordDownloadFailure(hash, cancellationToken);
			return (false, string.Empty);
		}
	}

	public static bool TryGetOrDownloadCachedFile(string url, out string localFilePath, int timeoutMs = 6000)
		=> TryGetOrDownloadCachedFile(url, out localFilePath, timeoutMs, CancellationToken.None);

	public static bool TryGetOrDownloadCachedFile(string url, out string localFilePath, int timeoutMs, CancellationToken cancellationToken)
	{
		localFilePath = string.Empty;
		if (string.IsNullOrWhiteSpace(url) || cancellationToken.IsCancellationRequested)
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

		if (IsNonPlayableWebUrl(url))
		{
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
				s_recentFailures.TryRemove(hash, out _);
				localFilePath = cachedPath;
				return true;
			}

			if (IsInFailureCooldown(hash))
			{
				return false;
			}

			// 采用异步后台等待，附加安全超时与取消令牌支持
			int effectiveTimeout = Math.Clamp(timeoutMs, 1000, 8000);
			var task = Task.Run(() => TryGetOrDownloadCachedFileAsync(trimmedUrl, effectiveTimeout, cancellationToken), cancellationToken);
			if (task.Wait(effectiveTimeout + 300, cancellationToken))
			{
				var (ok, path) = task.GetAwaiter().GetResult();
				if (ok && !string.IsNullOrEmpty(path))
				{
					localFilePath = path;
					return true;
				}
			}

			return false;
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
