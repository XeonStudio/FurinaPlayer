using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using ATL;
using FurinaPlayer.Audio.Decoders;
using FurinaPlayer.Core.Services;
using LibVLCSharp.Shared;
using NAudio.MediaFoundation;
using NAudio.Wave;

namespace FurinaPlayer.Audio.Services;

public enum ConvertTargetFormat
{
	AutoMp3OrFlac = 0,
	Mp3 = 1,
	Flac = 2
}

public sealed class ConvertRequest
{
	public required string InputPath { get; init; }
	public string? OutputDirectory { get; init; }
	public ConvertTargetFormat TargetFormat { get; init; } = ConvertTargetFormat.AutoMp3OrFlac;
	public bool AutoEnrichMetadata { get; init; } = true;
	public bool EnableOnlineEnrich { get; init; } = true;
	public string? EKey { get; init; }
	public string? EKeyDbPath { get; init; }
}

public sealed record ConvertProgressInfo(int Percent, string Stage);

public sealed class ConvertResult
{
	public bool Success { get; init; }
	public string? ErrorMessage { get; init; }
	public string? OutputPath { get; init; }
	public string SourceFormat { get; init; } = string.Empty;
	public string DecryptedContainer { get; init; } = string.Empty;
	public string TargetContainer { get; init; } = string.Empty;

	public string Title { get; init; } = string.Empty;
	public string Artist { get; init; } = string.Empty;
	public string Album { get; init; } = string.Empty;

	public bool HasMetadata { get; init; }
	public bool MetadataCompleted { get; init; }
	public bool HasLyrics { get; init; }
	public bool LyricsCompleted { get; init; }
	public bool HasCover { get; init; }
	public bool CoverCompleted { get; init; }
}

/// <summary>
/// 加密音频解密、MP3/FLAC 转码与元数据/内嵌歌词/专辑封面完整性检查与补齐服务
/// </summary>
public sealed class FormatConverterService
{
	private static readonly HttpClient _http = new() { Timeout = TimeSpan.FromSeconds(12) };
	private static LibVLC? _transcodeVlc;
	private static readonly object _vlcLock = new();

	private readonly StreamingService _streamingService;
	private readonly LyricService _lyricService;

	public FormatConverterService(StreamingService? streamingService = null, LyricService? lyricService = null)
	{
		_streamingService = streamingService ?? new StreamingService();
		_lyricService = lyricService ?? new LyricService();
	}

	public async Task<ConvertResult> ConvertFileAsync(
		ConvertRequest request,
		IProgress<ConvertProgressInfo>? progress = null,
		CancellationToken ct = default)
	{
		string? tempInput = null;
		string? tempWav = null;
		try
		{
			ct.ThrowIfCancellationRequested();
			progress?.Report(new ConvertProgressInfo(10, Loc.T("正在解密私有音频容器...")));

			DecodedAudioResult decoded = await Task.Run(() =>
				AudioDecoders.DecodeFile(request.InputPath, new AudioDecodeOptions
				{
					EKey = request.EKey,
					EKeyDbPath = request.EKeyDbPath
				}), ct);

			string srcContainer = decoded.Container.ToLowerInvariant();
			if (srcContainer == "bin")
			{
				return new ConvertResult
				{
					Success = false,
					SourceFormat = decoded.SourceFormat,
					ErrorMessage = Loc.T("解密后的音频流格式无法识别，请确认原文件未损坏")
				};
			}

			string targetExt = request.TargetFormat switch
			{
				ConvertTargetFormat.Mp3 => "mp3",
				ConvertTargetFormat.Flac => "flac",
				_ => srcContainer == "mp3" ? "mp3" : "flac"
			};

			string outputDir = !string.IsNullOrWhiteSpace(request.OutputDirectory)
				? request.OutputDirectory
				: (Path.GetDirectoryName(request.InputPath) ?? Environment.GetFolderPath(Environment.SpecialFolder.MyMusic));
			Directory.CreateDirectory(outputDir);

			string baseName = Path.GetFileNameWithoutExtension(request.InputPath);
			string outputPath = GetUniqueOutputPath(outputDir, baseName, targetExt);

			ct.ThrowIfCancellationRequested();

			if (string.Equals(srcContainer, targetExt, StringComparison.OrdinalIgnoreCase))
			{
				progress?.Report(new ConvertProgressInfo(55, Loc.T("原音质匹配，正在无损写出音频...")));
				await File.WriteAllBytesAsync(outputPath, decoded.AudioBytes, ct);
			}
			else
			{
				progress?.Report(new ConvertProgressInfo(40, Loc.F("正在转码为 {0}...", targetExt.ToUpperInvariant())));
				string tempDir = Path.Combine(Path.GetTempPath(), "FurinaPlayer_Convert");
				Directory.CreateDirectory(tempDir);
				tempInput = Path.Combine(tempDir, $"{Guid.NewGuid():N}.{srcContainer}");
				await File.WriteAllBytesAsync(tempInput, decoded.AudioBytes, ct);

				if (targetExt == "mp3")
				{
					string wavSource = tempInput;
					if (srcContainer != "wav")
					{
						if (!LibVlcPcmTranscoder.TryDecodeToWav(tempInput, out string decodedWav))
						{
							throw new InvalidOperationException(Loc.F("无法将 {0} 解码为 PCM 进行 MP3 编码", srcContainer.ToUpperInvariant()));
						}
						tempWav = decodedWav;
						wavSource = decodedWav;
					}
					await Task.Run(() => EncodeWavToMp3(wavSource, outputPath), ct);
				}
				else // flac
				{
					await Task.Run(() => TranscodeToFlacWithLibVlc(tempInput, outputPath, ct), ct);
				}
			}

			ct.ThrowIfCancellationRequested();
			progress?.Report(new ConvertProgressInfo(75, Loc.T("正在检查并补齐元数据、内嵌歌词与封面...")));

			var enrich = await CheckAndEnrichTagsAsync(
				outputPath,
				baseName,
				decoded,
				request.AutoEnrichMetadata,
				request.EnableOnlineEnrich,
				ct);

			progress?.Report(new ConvertProgressInfo(100, Loc.T("转换完成")));

			return new ConvertResult
			{
				Success = true,
				OutputPath = outputPath,
				SourceFormat = decoded.SourceFormat,
				DecryptedContainer = srcContainer,
				TargetContainer = targetExt,
				Title = enrich.Title,
				Artist = enrich.Artist,
				Album = enrich.Album,
				HasMetadata = enrich.HasMetadata,
				MetadataCompleted = enrich.MetadataCompleted,
				HasLyrics = enrich.HasLyrics,
				LyricsCompleted = enrich.LyricsCompleted,
				HasCover = enrich.HasCover,
				CoverCompleted = enrich.CoverCompleted
			};
		}
		catch (OperationCanceledException)
		{
			return new ConvertResult
			{
				Success = false,
				ErrorMessage = Loc.T("任务已取消")
			};
		}
		catch (Exception ex)
		{
			return new ConvertResult
			{
				Success = false,
				ErrorMessage = ex.Message
			};
		}
		finally
		{
			TryDeleteFile(tempInput);
			TryDeleteFile(tempWav);
		}
	}

	private static string GetUniqueOutputPath(string dir, string baseName, string ext)
	{
		string candidate = Path.Combine(dir, $"{baseName}.{ext}");
		if (!File.Exists(candidate)) return candidate;
		for (int i = 1; i < 1000; i++)
		{
			string alt = Path.Combine(dir, $"{baseName} ({i}).{ext}");
			if (!File.Exists(alt)) return alt;
		}
		return Path.Combine(dir, $"{baseName}_{Guid.NewGuid():N}.{ext}");
	}

	private static void EncodeWavToMp3(string wavPath, string outputMp3Path)
	{
		MediaFoundationApi.Startup();
		using var reader = new AudioFileReader(wavPath);
		MediaFoundationEncoder.EncodeToMp3(reader, outputMp3Path, 320000);
	}

	private static void TranscodeToFlacWithLibVlc(string inputPath, string outputFlacPath, CancellationToken ct)
	{
		LibVLC vlc = GetOrCreateTranscodeVlc();
		using var media = new Media(vlc, new Uri(inputPath));
		string escapedDst = outputFlacPath.Replace("\"", "\\\"");
		media.AddOption($":sout=#transcode{{acodec=flac,channels=2}}:std{{access=file,mux=raw,dst=\"{escapedDst}\"}}");
		media.AddOption(":sout-keep");

		using var mp = new MediaPlayer(media);
		using var done = new ManualResetEventSlim(false);
		bool failed = false;

		mp.EndReached += (_, _) => done.Set();
		mp.EncounteredError += (_, _) =>
		{
			failed = true;
			done.Set();
		};
		mp.Stopped += (_, _) => done.Set();

		if (!mp.Play())
			throw new InvalidOperationException("LibVLC 无法启动 FLAC 转码管线");

		while (!done.Wait(150))
		{
			ct.ThrowIfCancellationRequested();
		}

		if (failed || !File.Exists(outputFlacPath) || new FileInfo(outputFlacPath).Length == 0)
			throw new InvalidOperationException("LibVLC FLAC 转码失败");
	}

	private static LibVLC GetOrCreateTranscodeVlc()
	{
		lock (_vlcLock)
		{
			if (_transcodeVlc != null) return _transcodeVlc;
			string baseDir = AppContext.BaseDirectory;
			string libvlcDir = Path.Combine(baseDir, "libvlc", "win-x64");
			if (Directory.Exists(libvlcDir))
			{
				LibVLCSharp.Shared.Core.Initialize(libvlcDir);
			}
			else
			{
				LibVLCSharp.Shared.Core.Initialize();
			}
			_transcodeVlc = new LibVLC("--no-video", "--quiet");
			return _transcodeVlc;
		}
	}

	private sealed record EnrichOutcome(
		string Title,
		string Artist,
		string Album,
		bool HasMetadata,
		bool MetadataCompleted,
		bool HasLyrics,
		bool LyricsCompleted,
		bool HasCover,
		bool CoverCompleted);

	private async Task<EnrichOutcome> CheckAndEnrichTagsAsync(
		string outputPath,
		string fileBaseName,
		DecodedAudioResult decoded,
		bool autoEnrich,
		bool enableOnline,
		CancellationToken ct)
	{
		var track = new ATL.Track(outputPath);
		bool modified = false;
		bool metaCompleted = false;
		bool lyricsCompleted = false;
		bool coverCompleted = false;

		bool titleMissing = string.IsNullOrWhiteSpace(track.Title) || string.Equals(track.Title.Trim(), fileBaseName.Trim(), StringComparison.OrdinalIgnoreCase);

		// 1. 应用解密容器自带的元信息与封面（如 .ncm）
		if (titleMissing && !string.IsNullOrWhiteSpace(decoded.Title))
		{
			track.Title = decoded.Title;
			titleMissing = false;
			modified = true;
			metaCompleted = true;
		}
		if (string.IsNullOrWhiteSpace(track.Artist) && !string.IsNullOrWhiteSpace(decoded.Artist))
		{
			track.Artist = decoded.Artist;
			modified = true;
			metaCompleted = true;
		}
		if (string.IsNullOrWhiteSpace(track.Album) && !string.IsNullOrWhiteSpace(decoded.Album))
		{
			track.Album = decoded.Album;
			modified = true;
			metaCompleted = true;
		}
		if (track.EmbeddedPictures.Count == 0 && decoded.CoverBytes is { Length: > 64 })
		{
			track.EmbeddedPictures.Add(PictureInfo.fromBinaryData(decoded.CoverBytes, PictureInfo.PIC_TYPE.Front));
			modified = true;
			coverCompleted = true;
		}

		// 2. 文件名解析兜底 ("Artist - Title" / "Title")
		var (parsedArtist, parsedTitle) = ParseArtistAndTitleFromFileName(fileBaseName);
		if (titleMissing && !string.IsNullOrWhiteSpace(parsedTitle))
		{
			track.Title = parsedTitle;
			titleMissing = false;
			modified = true;
			metaCompleted = true;
		}
		if (string.IsNullOrWhiteSpace(track.Artist) && !string.IsNullOrWhiteSpace(parsedArtist))
		{
			track.Artist = parsedArtist;
			modified = true;
			metaCompleted = true;
		}

		bool hasLyricsNow = HasEmbeddedLyrics(track);
		bool hasCoverNow = track.EmbeddedPictures.Count > 0;
		bool hasFullMetaNow = !string.IsNullOrWhiteSpace(track.Title) && !string.IsNullOrWhiteSpace(track.Artist) && !string.IsNullOrWhiteSpace(track.Album);

		// 3. 在线完整性补齐（元数据 / 内嵌歌词 / 专辑封面）
		if (autoEnrich && enableOnline && (!hasFullMetaNow || !hasLyricsNow || !hasCoverNow))
		{
			string qTitle = !string.IsNullOrWhiteSpace(track.Title) ? track.Title : parsedTitle;
			string qArtist = !string.IsNullOrWhiteSpace(track.Artist) ? track.Artist : parsedArtist;
			string searchQuery = string.IsNullOrWhiteSpace(qArtist) ? qTitle : $"{qTitle} {qArtist}";

			if (!string.IsNullOrWhiteSpace(searchQuery))
			{
				try
				{
					var results = await _streamingService.SearchOnlineTracksAsync(searchQuery, offset: 0, limit: 5, searchType: 1, platform: "网易云音乐");
					var best = results.FirstOrDefault();
					if (best != null)
					{
						if (titleMissing && !string.IsNullOrWhiteSpace(best.Title))
						{
							track.Title = best.Title;
							titleMissing = false;
							modified = true;
							metaCompleted = true;
						}
						if (string.IsNullOrWhiteSpace(track.Artist) && !string.IsNullOrWhiteSpace(best.Artist))
						{
							track.Artist = best.Artist;
							modified = true;
							metaCompleted = true;
						}
						if (string.IsNullOrWhiteSpace(track.Album) && !string.IsNullOrWhiteSpace(best.Album))
						{
							track.Album = best.Album;
							modified = true;
							metaCompleted = true;
						}

						if (track.EmbeddedPictures.Count == 0 && !string.IsNullOrWhiteSpace(best.CoverUrl))
						{
							try
							{
								byte[] coverData = await _http.GetByteArrayAsync(best.CoverUrl, ct);
								if (coverData.Length > 128)
								{
									track.EmbeddedPictures.Add(PictureInfo.fromBinaryData(coverData, PictureInfo.PIC_TYPE.Front));
									modified = true;
									coverCompleted = true;
								}
							}
							catch { }
						}
					}
				}
				catch { }

				if (!HasEmbeddedLyrics(track))
				{
					try
					{
						var lrcDoc = await _lyricService.SearchOnlineWithFallbackAsync(qTitle, qArtist, ct);
						if (lrcDoc != null && lrcDoc.Lines.Count > 0)
						{
							string lrcText = FormatLrcText(lrcDoc);
							if (!string.IsNullOrWhiteSpace(lrcText))
							{
								track.Lyrics.UnsynchronizedLyrics = lrcText;
								track.AdditionalFields["LYRICS"] = lrcText;
								modified = true;
								lyricsCompleted = true;
							}
						}
					}
					catch { }
				}
			}
		}

		if (modified)
		{
			track.Save();
		}

		// 重新读取校验最终完整性
		var verified = new ATL.Track(outputPath);
		bool finalMeta = !string.IsNullOrWhiteSpace(verified.Title) && !string.IsNullOrWhiteSpace(verified.Artist);
		bool finalLyrics = HasEmbeddedLyrics(verified);
		bool finalCover = verified.EmbeddedPictures.Count > 0;

		return new EnrichOutcome(
			verified.Title ?? fileBaseName,
			verified.Artist ?? string.Empty,
			verified.Album ?? string.Empty,
			finalMeta,
			metaCompleted && finalMeta,
			finalLyrics,
			lyricsCompleted && finalLyrics,
			finalCover,
			coverCompleted && finalCover);
	}

	private static bool HasEmbeddedLyrics(ATL.Track track)
	{
		if (!string.IsNullOrWhiteSpace(track.Lyrics?.UnsynchronizedLyrics)) return true;
		if (track.Lyrics?.SynchronizedLyrics is { Count: > 0 }) return true;
		if (track.AdditionalFields != null && track.AdditionalFields.TryGetValue("LYRICS", out string? v) && !string.IsNullOrWhiteSpace(v))
			return true;
		return false;
	}

	private static string FormatLrcText(FurinaPlayer.Core.Models.LyricDocument doc)
	{
		var lines = new List<string>(doc.Lines.Count);
		foreach (var line in doc.Lines)
		{
			int totalMs = (int)Math.Max(0, line.Time.TotalMilliseconds);
			int min = totalMs / 60000;
			int sec = (totalMs % 60000) / 1000;
			int cs = (totalMs % 1000) / 10;
			lines.Add($"[{min:00}:{sec:00}.{cs:00}]{line.Text}");
		}
		return string.Join(Environment.NewLine, lines);
	}

	private static (string artist, string title) ParseArtistAndTitleFromFileName(string name)
	{
		int dash = name.IndexOf(" - ", StringComparison.Ordinal);
		if (dash > 0 && dash + 3 < name.Length)
		{
			return (name[..dash].Trim(), name[(dash + 3)..].Trim());
		}
		return (string.Empty, name.Trim());
	}

	private static void TryDeleteFile(string? path)
	{
		if (string.IsNullOrEmpty(path)) return;
		try { if (File.Exists(path)) File.Delete(path); } catch { }
	}
}
