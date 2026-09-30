using System;
using System.Collections.Generic;
using System.Linq;
using SonicWave.Audio.Dsp;
using SonicWave.Core.Models;

namespace SonicWave.Audio;

public readonly record struct RenderSettings(double[]? EqGains, bool ReverbEnabled, double ReverbMix, string? ReverbPreset, TimbreSettings? Timbre, SpatialParameters? Spatial = null, IReadOnlyList<Vst3PluginState>? Vst3 = null)
{
	public static RenderSettings Flat { get; } = new RenderSettings(new double[10], ReverbEnabled: false, 0.2, "大厅", null);

	public bool IsActive
	{
		get
		{
			if (EqGains != null && EqGains.Any((double g) => Math.Abs(g) > 0.05))
			{
				return true;
			}
			if (ReverbEnabled)
			{
				return true;
			}
			if (Timbre != null && Timbre.IsActive)
			{
				return true;
			}
			if (Vst3 != null && Vst3.Any((Vst3PluginState p) => p?.IsActive ?? false))
			{
				return true;
			}
			return false;
		}
	}
}
