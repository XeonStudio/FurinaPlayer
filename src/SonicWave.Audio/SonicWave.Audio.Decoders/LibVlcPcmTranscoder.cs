using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using LibVLCSharp.Shared;

namespace SonicWave.Audio.Decoders;

/// <summary>
/// Decodes audio formats unsupported by Windows Media Foundation (such as OGG Vorbis, Opus, WebM, MKA, SPX)
/// into cached 16-bit 48kHz stereo PCM WAV files using LibVLC so that NAudio and the full DSP/metering
/// pipeline can process them identically to native PCM/FLAC/MP3 streams.
/// </summary>
public static class LibVlcPcmTranscoder
{
	private const int TranscodeTimeoutMs = 45000;
	private const int MaxCacheFiles = 24;
	private const long MaxCacheBytes = 1_500_000_000L;
	private const long MinValidWavBytes = 44L;

	private static readonly object s_libVlcLock = new();
	private static LibVLC? s_libVlc;
	private static bool s_libVlcInitFailed;

	private static readonly ConcurrentDictionary<string, Lazy<string?>> s_inFlight = new(StringComparer.Ordinal);

	public static bool NeedsLibVlcFallbackExtension(string? ext)
	{
		if (string.IsNullOrWhiteSpace(ext))
		{
			return false;
		}
		string normalized = ext.Trim().ToLowerInvariant();
		if (!normalized.StartsWith('.'))
		{
			normalized = "." + normalized;
		}
		return normalized is ".ogg" or ".oga" or ".opus" or ".webm" or ".mka" or ".spx";
	}

	public static bool TryDecodeToWav(string sourcePath, out string wavPath)
	{
		wavPath = string.Empty;
		if (string.IsNullOrWhiteSpace(sourcePath))
		{
			return false;
		}

		string fullPath;
		FileInfo sourceInfo;
		try
		{
			fullPath = Path.GetFullPath(sourcePath);
			sourceInfo = new FileInfo(fullPath);
			if (!sourceInfo.Exists || sourceInfo.Length <= 0)
			{
				return false;
			}
		}
		catch
		{
			return false;
		}

		string cacheDir;
		try
		{
			cacheDir = GetCacheDirectory();
		}
		catch
		{
			return false;
		}

		string cacheKey = ComputeCacheKey(fullPath, sourceInfo.Length, sourceInfo.LastWriteTimeUtc.Ticks);
		string finalWav = Path.Combine(cacheDir, cacheKey + ".wav");

		if (IsValidWavFile(finalWav))
		{
			TouchFileUtc(finalWav);
			wavPath = finalWav;
			return true;
		}

		Lazy<string?> lazy = s_inFlight.GetOrAdd(
			cacheKey,
			_ => new Lazy<string?>(
				() => TranscodeToCache(fullPath, cacheDir, cacheKey, finalWav),
				LazyThreadSafetyMode.ExecutionAndPublication));

		try
		{
			string? decodedPath = lazy.Value;
			if (!string.IsNullOrEmpty(decodedPath) && IsValidWavFile(decodedPath))
			{
				wavPath = decodedPath;
				return true;
			}
			return false;
		}
		catch
		{
			return false;
		}
		finally
		{
			s_inFlight.TryRemove(new KeyValuePair<string, Lazy<string?>>(cacheKey, lazy));
		}
	}

	private static string? TranscodeToCache(string fullPath, string cacheDir, string cacheKey, string finalWav)
	{
		if (IsValidWavFile(finalWav))
		{
			TouchFileUtc(finalWav);
			return finalWav;
		}

		LibVLC? libVlc = GetOrCreateLibVlc();
		if (libVlc == null)
		{
			return null;
		}

		string tmpWav = Path.Combine(cacheDir, $"{cacheKey}.{Guid.NewGuid():N}.tmp");
		string escapedDst = tmpWav.Replace("\\", "\\\\").Replace("'", "\\'");

		bool success = false;
		using var done = new ManualResetEventSlim(false);
		int encounteredError = 0;

		Media? media = null;
		MediaPlayer? player = null;
		EventHandler<EventArgs>? onEnd = null;
		EventHandler<EventArgs>? onError = null;

		try
		{
			media = new Media(libVlc, fullPath, FromType.FromPath);
			media.AddOption(":no-video");
			media.AddOption(":no-sout-all");
			media.AddOption($":sout=#transcode{{acodec=s16l,channels=2,samplerate=48000}}:std{{access=file,mux=wav,dst='{escapedDst}'}}");

			player = new MediaPlayer(libVlc);
			onEnd = (_, _) => done.Set();
			onError = (_, _) =>
			{
				Volatile.Write(ref encounteredError, 1);
				done.Set();
			};
			player.EndReached += onEnd;
			player.EncounteredError += onError;

			if (player.Play(media))
			{
				bool signaled = done.Wait(TranscodeTimeoutMs);
				success = signaled && Volatile.Read(ref encounteredError) == 0;
			}
		}
		catch
		{
			success = false;
		}
		finally
		{
			if (player != null)
			{
				if (onEnd != null) player.EndReached -= onEnd;
				if (onError != null) player.EncounteredError -= onError;
				try
				{
					player.Stop();
				}
				catch
				{
				}
				try { player.Dispose(); } catch { }
			}
			try { media?.Dispose(); } catch { }
		}

		if (!success || !IsValidWavFile(tmpWav))
		{
			TryDelete(tmpWav);
			return null;
		}

		try
		{
			File.Move(tmpWav, finalWav, overwrite: true);
			if (!IsValidWavFile(finalWav))
			{
				return null;
			}
			TrimCacheIfNeeded(cacheDir, finalWav);
			return finalWav;
		}
		catch
		{
			TryDelete(tmpWav);
			if (IsValidWavFile(finalWav))
			{
				return finalWav;
			}
			return null;
		}
	}

	private static LibVLC? GetOrCreateLibVlc()
	{
		if (s_libVlc != null)
		{
			return s_libVlc;
		}
		lock (s_libVlcLock)
		{
			if (s_libVlc != null)
			{
				return s_libVlc;
			}
			if (s_libVlcInitFailed)
			{
				return null;
			}
			try
			{
				string? vlcDir = ResolveLibVlcDirectory();
				if (vlcDir != null)
				{
					LibVLCSharp.Shared.Core.Initialize(vlcDir);
				}
				else
				{
					LibVLCSharp.Shared.Core.Initialize();
				}
				s_libVlc = new LibVLC("--no-video", "--quiet");
				return s_libVlc;
			}
			catch
			{
				s_libVlcInitFailed = true;
				return null;
			}
		}
	}

	private static string? ResolveLibVlcDirectory()
	{
		string baseDirectory = AppContext.BaseDirectory;
		var candidates = new List<string>
		{
			Path.Combine(baseDirectory, "libvlc", "win-x64"),
			Path.Combine(baseDirectory, "libvlc", "win-x86"),
			Path.Combine(baseDirectory, "libvlc"),
			baseDirectory
		};

		string? current = baseDirectory;
		for (int i = 0; i < 8 && current != null; i++)
		{
			candidates.Add(Path.Combine(current, "libvlc", "win-x64"));
			candidates.Add(Path.Combine(current, "libvlc"));
			current = Directory.GetParent(current)?.FullName;
		}
		try
		{
			string cwd = Environment.CurrentDirectory;
			if (!string.IsNullOrWhiteSpace(cwd))
			{
				candidates.Add(Path.Combine(cwd, "libvlc", "win-x64"));
				candidates.Add(Path.Combine(cwd, "libvlc"));
			}
		}
		catch
		{
		}

		foreach (string candidate in candidates)
		{
			try
			{
				if (File.Exists(Path.Combine(candidate, "libvlc.dll")))
				{
					return candidate;
				}
			}
			catch
			{
			}
		}
		return null;
	}

	private static string GetCacheDirectory()
	{
		string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
		string baseDir = !string.IsNullOrWhiteSpace(localAppData) ? localAppData : Path.GetTempPath();
		string dir = Path.Combine(baseDir, "FurinaPlayer", "PcmDecodeCache");
		Directory.CreateDirectory(dir);
		return dir;
	}

	private static string ComputeCacheKey(string fullPath, long fileLength, long lastWriteUtcTicks)
	{
		string raw = $"{fullPath}|{fileLength}|{lastWriteUtcTicks}";
		byte[] hash = SHA256.HashData(Encoding.UTF8.GetBytes(raw));
		return Convert.ToHexString(hash).ToLowerInvariant();
	}

	private static bool IsValidWavFile(string path)
	{
		try
		{
			var fi = new FileInfo(path);
			if (!fi.Exists || fi.Length <= MinValidWavBytes)
			{
				return false;
			}
			using var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
			Span<byte> header = stackalloc byte[12];
			if (fs.Read(header) < 12)
			{
				return false;
			}
			return header[0] == (byte)'R' && header[1] == (byte)'I' && header[2] == (byte)'F' && header[3] == (byte)'F'
				&& header[8] == (byte)'W' && header[9] == (byte)'A' && header[10] == (byte)'V' && header[11] == (byte)'E';
		}
		catch
		{
			return false;
		}
	}

	private static void TouchFileUtc(string path)
	{
		try
		{
			File.SetLastWriteTimeUtc(path, DateTime.UtcNow);
		}
		catch
		{
		}
	}

	private static void TrimCacheIfNeeded(string cacheDir, string keepPath)
	{
		try
		{
			DateTime staleTmpCutoff = DateTime.UtcNow.AddMinutes(-30);
			foreach (string tmp in Directory.EnumerateFiles(cacheDir, "*.tmp"))
			{
				try
				{
					if (File.GetLastWriteTimeUtc(tmp) < staleTmpCutoff)
					{
						File.Delete(tmp);
					}
				}
				catch
				{
				}
			}

			var wavFiles = Directory.EnumerateFiles(cacheDir, "*.wav")
				.Select(p => new FileInfo(p))
				.Where(fi => fi.Exists)
				.ToList();

			long totalBytes = 0;
			foreach (FileInfo fi in wavFiles)
			{
				totalBytes += fi.Length;
			}

			int remainingCount = wavFiles.Count;
			if (remainingCount <= MaxCacheFiles && totalBytes <= MaxCacheBytes)
			{
				return;
			}

			foreach (FileInfo fi in wavFiles.OrderBy(f => f.LastWriteTimeUtc))
			{
				if (remainingCount <= MaxCacheFiles && totalBytes <= MaxCacheBytes)
				{
					break;
				}
				if (string.Equals(fi.FullName, keepPath, StringComparison.OrdinalIgnoreCase))
				{
					continue;
				}
				try
				{
					long len = fi.Length;
					fi.Delete();
					totalBytes -= len;
					remainingCount--;
				}
				catch
				{
				}
			}
		}
		catch
		{
		}
	}

	private static void TryDelete(string path)
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
