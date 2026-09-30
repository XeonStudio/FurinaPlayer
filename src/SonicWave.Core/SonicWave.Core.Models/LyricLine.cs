using System;
using System.Collections.Generic;

namespace SonicWave.Core.Models;

public class LyricLine
{
	public TimeSpan Time { get; set; }

	public string Text { get; set; } = string.Empty;

	public List<(int CharIndex, int StartMs)>? WordTimings { get; set; }

	public override string ToString()
	{
		return $"[{Time:hh\\:mm\\:ss\\.ff}] {Text}";
	}
}
