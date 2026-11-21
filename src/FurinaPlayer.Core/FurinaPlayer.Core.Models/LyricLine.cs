using System;
using System.Collections.Generic;

namespace FurinaPlayer.Core.Models;

public sealed record LyricWord(string Text, int StartMs, int EndMs);

public class LyricLine
{
	public TimeSpan Time { get; set; }

	public string Text { get; set; } = string.Empty;

	public string OriginalText { get; set; } = string.Empty;

	public string? Translation { get; set; }

	public List<(int CharIndex, int StartMs)>? WordTimings { get; set; }

	public List<LyricWord>? Words { get; set; }

	public string DisplayOriginal => !string.IsNullOrEmpty(OriginalText) ? OriginalText : Text;

	public override string ToString()
	{
		return $"[{Time:hh\\:mm\\:ss\\.ff}] {Text}";
	}
}
