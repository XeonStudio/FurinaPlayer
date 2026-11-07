using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using ATL;
using SonicWave.Core.Helpers;
using SonicWave.Core.Models;

namespace SonicWave.Core.Services;

public class MetadataService
{
	public SonicWave.Core.Models.Track ReadTrack(string filePath)
	{
		SonicWave.Core.Models.Track track = new SonicWave.Core.Models.Track
		{
			FilePath = filePath,
			Format = AudioFormatDetector.Detect(filePath)
		};
		FileInfo fileInfo = new FileInfo(filePath);
		track.FileSize = (fileInfo.Exists ? fileInfo.Length : 0);
		try
		{
			ATL.Track track2 = new ATL.Track(filePath);
			track.Title = Safe(track2.Title);
			track.Artist = Safe(track2.Artist);
			track.Album = Safe(track2.Album);
			track.AlbumArtist = Safe(track2.AlbumArtist);
			track.Genre = Safe(track2.Genre);
			track.Year = track2.Year.GetValueOrDefault();
			track.TrackNumber = track2.TrackNumber.GetValueOrDefault();
			track.DiscNumber = track2.DiscNumber.GetValueOrDefault();
			track.DurationMilliseconds = (long)track2.DurationMs;
			track.SampleRate = (int)track2.SampleRate;
			track.BitDepth = ((track2.BitDepth > 0) ? track2.BitDepth : 0);
			track.Bitrate = track2.Bitrate;
			track.EmbeddedLyrics = (string.IsNullOrWhiteSpace(track2.Lyrics?.UnsynchronizedLyrics) ? null : track2.Lyrics.UnsynchronizedLyrics);
		}
		catch
		{
		}
		if (string.IsNullOrWhiteSpace(track.Title))
		{
			track.Title = Path.GetFileNameWithoutExtension(filePath);
		}
		return track;
	}

	public byte[]? ExtractCover(string filePath)
	{
		try
		{
			ATL.Track track = new ATL.Track(filePath);
			PictureInfo pictureInfo = track.EmbeddedPictures.FirstOrDefault((PictureInfo p) => p.PicType == PictureInfo.PIC_TYPE.Generic || p.PicType == PictureInfo.PIC_TYPE.Front || p.PicType == PictureInfo.PIC_TYPE.Back);
			if (pictureInfo == null)
			{
				pictureInfo = track.EmbeddedPictures.FirstOrDefault();
			}
			return pictureInfo?.PictureData;
		}
		catch
		{
			return null;
		}
	}

	public string? ExtractCoverToFile(string filePath, string cacheDir)
	{
		byte[]? array = ExtractCover(filePath);
		if (array == null || array.Length == 0)
		{
			// Check if folder contains cover images (cover.jpg, folder.jpg, front.jpg, [trackname].jpg, etc.)
			array = FindFolderCoverBytes(filePath);
		}
		if (array == null || array.Length == 0)
		{
			return null;
		}
		try
		{
			Directory.CreateDirectory(cacheDir);
			string text = DetectImageExtension(array);
			string text2 = Convert.ToHexString(SHA1.HashData(array)).ToLowerInvariant().Substring(0, 16);
			string text3 = Path.Combine(cacheDir, "cover_" + text2 + text);
			if (!File.Exists(text3))
			{
				File.WriteAllBytes(text3, array);
			}
			return text3;
		}
		catch
		{
			return null;
		}
	}

	private static byte[]? FindFolderCoverBytes(string filePath)
	{
		try
		{
			string? dir = Path.GetDirectoryName(filePath);
			if (string.IsNullOrEmpty(dir) || !Directory.Exists(dir)) return null;

			string[] candidates = { "cover.jpg", "cover.png", "folder.jpg", "folder.png", "front.jpg", "front.png", "album.jpg", "album.png", "Cover.jpg", "Folder.jpg", "Front.jpg" };
			foreach (var name in candidates)
			{
				string p = Path.Combine(dir, name);
				if (File.Exists(p))
				{
					return File.ReadAllBytes(p);
				}
			}

			string sameBaseJpg = Path.ChangeExtension(filePath, ".jpg");
			if (File.Exists(sameBaseJpg)) return File.ReadAllBytes(sameBaseJpg);
			string sameBasePng = Path.ChangeExtension(filePath, ".png");
			if (File.Exists(sameBasePng)) return File.ReadAllBytes(sameBasePng);

			// Look for any image in directory
			var files = Directory.GetFiles(dir, "*.*");
			foreach (var f in files)
			{
				string ext = Path.GetExtension(f).ToLowerInvariant();
				if (ext == ".jpg" || ext == ".png" || ext == ".jpeg" || ext == ".webp")
				{
					return File.ReadAllBytes(f);
				}
			}
		}
		catch
		{
		}
		return null;
	}

	private static string DetectImageExtension(byte[] bytes)
	{
		if (bytes.Length > 3 && bytes[0] == byte.MaxValue && bytes[1] == 216 && bytes[2] == byte.MaxValue)
		{
			return ".jpg";
		}
		if (bytes.Length > 8 && bytes[0] == 137 && bytes[1] == 80 && bytes[2] == 78 && bytes[3] == 71)
		{
			return ".png";
		}
		if (bytes.Length > 2 && bytes[0] == 66 && bytes[1] == 77)
		{
			return ".bmp";
		}
		return ".jpg";
	}

	private static string Safe(string? s)
	{
		if (!string.IsNullOrWhiteSpace(s))
		{
			return s.Trim();
		}
		return string.Empty;
	}
}
