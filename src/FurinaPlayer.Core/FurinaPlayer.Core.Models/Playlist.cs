using System;
using System.Collections.Generic;

namespace FurinaPlayer.Core.Models;

public class Playlist
{
	public long Id { get; set; }

	public string Name { get; set; } = string.Empty;

	public DateTime CreatedAt { get; set; } = DateTime.Now;

	public List<long> TrackIds { get; set; } = new List<long>();
}
