using System;
using System.Collections.Generic;

namespace SonicWave.Core.Models;

public class LyricDocument
{
	public string Title { get; set; } = string.Empty;

	public string Artist { get; set; } = string.Empty;

	public string Album { get; set; } = string.Empty;

	public string Source { get; set; } = "local";

	public List<LyricLine> Lines { get; set; } = new List<LyricLine>();

	public bool IsEmpty => Lines.Count == 0;

	public LyricLine? GetLineAt(TimeSpan position)
	{
		LyricLine result = null;
		foreach (LyricLine line in Lines)
		{
			if (line.Time <= position)
			{
				result = line;
				continue;
			}
			break;
		}
		return result;
	}

	public (int Current, int Next) GetIndicesAt(TimeSpan position)
	{
		int item = -1;
		for (int i = 0; i < Lines.Count; i++)
		{
			if (Lines[i].Time <= position)
			{
				item = i;
				continue;
			}
			return (Current: item, Next: i);
		}
		return (Current: item, Next: -1);
	}
}
