using System;
using System.IO;

namespace SonicWave.Core.Models;

public class Track
{
	public long Id { get; set; }

	public string FilePath { get; set; } = string.Empty;

	public string Title { get; set; } = string.Empty;

	public string Artist { get; set; } = string.Empty;

	public string Album { get; set; } = string.Empty;

	public string AlbumArtist { get; set; } = string.Empty;

	public string Genre { get; set; } = string.Empty;

	public int Year { get; set; }

	public int TrackNumber { get; set; }

	public int DiscNumber { get; set; }

	public long DurationMilliseconds { get; set; }

	public int SampleRate { get; set; }

	public int BitDepth { get; set; }

	public long Bitrate { get; set; }

	public AudioFormat Format { get; set; }

	public long FileSize { get; set; }

	public string? CoverPath { get; set; }

	public string? EmbeddedLyrics { get; set; }

	public DateTime AddedAt { get; set; } = DateTime.Now;

	public DateTime LastPlayedAt { get; set; }

	public long PlayCount { get; set; }

	public bool IsFavorite { get; set; }

	public string DurationText
	{
		get
		{
			TimeSpan timeSpan = TimeSpan.FromMilliseconds(DurationMilliseconds);
			if (!(timeSpan.TotalHours >= 1.0))
			{
				return timeSpan.ToString("m\\:ss");
			}
			return timeSpan.ToString("h\\:mm\\:ss");
		}
	}

	public string DisplayTitle
	{
		get
		{
			if (!string.IsNullOrWhiteSpace(Title))
			{
				return Title;
			}
			return Path.GetFileNameWithoutExtension(FilePath);
		}
	}

	public override bool Equals(object? obj)
	{
		if (obj is Track track)
		{
			return string.Equals(track.FilePath, FilePath, StringComparison.OrdinalIgnoreCase);
		}
		return false;
	}

	public override int GetHashCode()
	{
		return StringComparer.OrdinalIgnoreCase.GetHashCode(FilePath);
	}
}
