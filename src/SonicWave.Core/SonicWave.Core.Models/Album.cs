using System.Collections.Generic;

namespace SonicWave.Core.Models;

public class Album
{
	public long Id { get; set; }

	public string Name { get; set; } = string.Empty;

	public string Artist { get; set; } = string.Empty;

	public int Year { get; set; }

	public string? CoverPath { get; set; }

	public List<Track> Tracks { get; set; } = new List<Track>();

	public int TrackCount => Tracks.Count;

	public long TotalDurationMs => 0L;
}
