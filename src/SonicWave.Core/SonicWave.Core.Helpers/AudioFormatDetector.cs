using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using SonicWave.Core.Models;

namespace SonicWave.Core.Helpers;

public static class AudioFormatDetector
{
	private static readonly Dictionary<string, AudioFormat> ByExtension = new Dictionary<string, AudioFormat>(StringComparer.OrdinalIgnoreCase)
	{
		[".flac"] = AudioFormat.Flac,
		[".ape"] = AudioFormat.Ape,
		[".wav"] = AudioFormat.Wav,
		[".aiff"] = AudioFormat.Aiff,
		[".aif"] = AudioFormat.Aiff,
		[".wv"] = AudioFormat.WavPack,
		[".dsf"] = AudioFormat.Dsf,
		[".dff"] = AudioFormat.Dff,
		[".mp3"] = AudioFormat.Mp3,
		[".aac"] = AudioFormat.Aac,
		[".m4a"] = AudioFormat.M4a,
		[".m4b"] = AudioFormat.M4a,
		[".ogg"] = AudioFormat.Ogg,
		[".opus"] = AudioFormat.Opus,
		[".wma"] = AudioFormat.Wma,
		[".ac3"] = AudioFormat.Ac3,
		[".dts"] = AudioFormat.Dts,
		[".tta"] = AudioFormat.Tta,
		[".caf"] = AudioFormat.Caf
	};

	private static readonly Dictionary<string, AudioFormat> ByMagic = new Dictionary<string, AudioFormat>(StringComparer.OrdinalIgnoreCase)
	{
		["fLaC"] = AudioFormat.Flac,
		["RIFF"] = AudioFormat.Wav,
		["FORM"] = AudioFormat.Aiff,
		["MAC "] = AudioFormat.Ape,
		["wvpk"] = AudioFormat.WavPack,
		["DSD "] = AudioFormat.Dsf,
		["FRM8"] = AudioFormat.Dff,
		["ID3"] = AudioFormat.Mp3,
		["OggS"] = AudioFormat.Ogg,
		["caff"] = AudioFormat.Caf,
		["fLaC\0\0\0\""] = AudioFormat.Flac,
		["ftypM4A"] = AudioFormat.M4a,
		["ftypM4B"] = AudioFormat.M4a
	};

	public static IReadOnlyCollection<string> SupportedExtensions => ByExtension.Keys;

	public static bool IsSupportedExtension(string path)
	{
		return ByExtension.ContainsKey(Path.GetExtension(path));
	}

	public static AudioFormat FromExtension(string path)
	{
		string extension = Path.GetExtension(path);
		if (!ByExtension.TryGetValue(extension, out var value))
		{
			return AudioFormat.Unknown;
		}
		return value;
	}

	public static AudioFormat Detect(string filePath)
	{
		AudioFormat audioFormat = FromExtension(filePath);
		try
		{
			using FileStream fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
			Span<byte> buffer = stackalloc byte[16];
			int num = fileStream.Read(buffer);
			if (num >= 4)
			{
				string text = Encoding.ASCII.GetString(buffer.Slice(0, Math.Min(num, 8)));
				if (text.StartsWith("OggS", StringComparison.Ordinal) && num >= 28)
				{
					return (audioFormat == AudioFormat.Opus) ? AudioFormat.Opus : AudioFormat.Ogg;
				}
				foreach (KeyValuePair<string, AudioFormat> item in ByMagic)
				{
					if (text.StartsWith(item.Key, StringComparison.Ordinal))
					{
						return item.Value;
					}
				}
				if (text.StartsWith("ftyp", StringComparison.Ordinal))
				{
					return AudioFormat.M4a;
				}
				if (text.StartsWith("\u001b\0\0\0", StringComparison.Ordinal) || text.StartsWith("WMA", StringComparison.Ordinal) || text.StartsWith("ASF", StringComparison.Ordinal))
				{
					return AudioFormat.Wma;
				}
			}
		}
		catch
		{
		}
		return audioFormat;
	}

	public static bool IsDsd(AudioFormat format)
	{
		if ((uint)(format - 7) <= 1u)
		{
			return true;
		}
		return false;
	}

	public static bool IsLossless(AudioFormat format)
	{
		if ((uint)(format - 1) <= 7u || (uint)(format - 17) <= 1u)
		{
			return true;
		}
		return false;
	}

	public static DsdRate ClassifyDsdRate(int sampleRate)
	{
		if (sampleRate < 8500000)
		{
			if (sampleRate >= 2400000)
			{
				if (sampleRate < 4300000)
				{
					return DsdRate.Dsd64;
				}
				return DsdRate.Dsd128;
			}
			return DsdRate.None;
		}
		if (sampleRate < 17000000)
		{
			return DsdRate.Dsd256;
		}
		return DsdRate.Dsd512;
	}
}
