using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;

namespace SonicWave.Audio.Dsp;

public sealed class AutoEqPreset
{
	public string Name { get; set; } = "Imported AutoEq";
	public double PreampDb { get; set; } = 0.0;
	public List<EchoBiquadFilter> Bands { get; } = new();
}

public static class AutoEqParser
{
	// Regex pattern for Preamp: Preamp: -6.0 dB
	private static readonly Regex PreampRegex = new(@"Preamp:\s*([+-]?\d+(?:\.\d+)?)\s*dB", RegexOptions.IgnoreCase | RegexOptions.Compiled);

	// Regex pattern for Filter lines:
	// Filter 1: ON PK Fc 20.0 Hz Gain -2.5 dB Q 1.41
	private static readonly Regex FilterRegex = new(
		@"Filter\s*(?:\d+)?:\s*(ON|OFF)?\s*([A-Za-z]+)\s+Fc\s*([+-]?\d+(?:\.\d+)?)\s*Hz(?:\s+Gain\s*([+-]?\d+(?:\.\d+)?)\s*dB)?(?:\s+Q\s*([+-]?\d+(?:\.\d+)?))?",
		RegexOptions.IgnoreCase | RegexOptions.Compiled);

	public static AutoEqPreset Parse(string content, string name = "AutoEq")
	{
		var preset = new AutoEqPreset { Name = name };
		using var reader = new StringReader(content);
		string? line;

		while ((line = reader.ReadLine()) != null)
		{
			line = line.Trim();
			if (string.IsNullOrWhiteSpace(line) || line.StartsWith('#'))
			{
				continue;
			}

			// Check Preamp
			var preampMatch = PreampRegex.Match(line);
			if (preampMatch.Success && double.TryParse(preampMatch.Groups[1].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out double preampVal))
			{
				preset.PreampDb = preampVal;
				continue;
			}

			// Check Filter
			var filterMatch = FilterRegex.Match(line);
			if (filterMatch.Success)
			{
				bool enabled = !filterMatch.Groups[1].Value.Equals("OFF", StringComparison.OrdinalIgnoreCase);
				string typeStr = filterMatch.Groups[2].Value.ToUpperInvariant();
				double fc = 1000.0;
				double gain = 0.0;
				double q = 1.414;

				if (double.TryParse(filterMatch.Groups[3].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out double parsedFc))
				{
					fc = parsedFc;
				}

				if (filterMatch.Groups[4].Success && double.TryParse(filterMatch.Groups[4].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out double parsedGain))
				{
					gain = parsedGain;
				}

				if (filterMatch.Groups[5].Success && double.TryParse(filterMatch.Groups[5].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out double parsedQ))
				{
					q = parsedQ;
				}

				EchoFilterType filterType = typeStr switch
				{
					"LS" or "LOWSHELF" => EchoFilterType.LowShelf,
					"HS" or "HIGHSHELF" => EchoFilterType.HighShelf,
					"LP" or "LOWPASS" => EchoFilterType.LowPass,
					"HP" or "HIGHPASS" => EchoFilterType.HighPass,
					"BP" or "BANDPASS" => EchoFilterType.BandPass,
					"NO" or "NOTCH" => EchoFilterType.Notch,
					_ => EchoFilterType.Peaking
				};

				preset.Bands.Add(new EchoBiquadFilter
				{
					FilterType = filterType,
					FrequencyHz = fc,
					GainDb = gain,
					Q = q,
					Enabled = enabled
				});
			}
		}

		return preset;
	}

	public static void ApplyTo(AutoEqPreset preset, EchoEqProcessor processor)
	{
		processor.PreampDb = preset.PreampDb;
		processor.SetBands(preset.Bands);
	}
}
