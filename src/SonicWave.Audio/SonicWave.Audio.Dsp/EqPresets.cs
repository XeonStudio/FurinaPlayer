using System.Collections.Generic;
using System.Linq;

namespace SonicWave.Audio.Dsp;

public static class EqPresets
{
	public static readonly IReadOnlyDictionary<string, double[]> Presets = new Dictionary<string, double[]>
	{
		["平坦"] = new double[10],
		["流行"] = new double[10] { -1.5, -1.0, 0.0, 1.5, 2.0, 1.5, 0.0, -0.5, -1.0, -1.5 },
		["摇滚"] = new double[10] { 3.0, 2.0, 0.0, -1.0, 0.5, 2.0, 3.0, 3.0, 2.0, 1.5 },
		["爵士"] = new double[10] { 2.0, 1.5, 1.0, 1.0, 0.5, 0.0, -0.5, -0.5, 0.0, 0.5 },
		["古典"] = new double[10] { 3.0, 2.0, 0.5, -1.0, -1.5, -1.0, 0.0, 1.5, 2.5, 3.0 },
		["电子"] = new double[10] { 2.5, 1.5, 0.0, -1.5, -1.0, 0.5, 2.0, 3.0, 3.5, 3.0 },
		["人声"] = new double[10] { -2.0, -1.5, -0.5, 1.0, 2.5, 3.0, 2.5, 1.5, 0.0, -1.0 },
		["低音增强"] = new double[10] { 5.0, 4.0, 3.0, 2.0, 1.0, 0.0, -0.5, -1.0, -1.5, -2.0 },
		["高音增强"] = new double[10] { -2.0, -1.5, -1.0, -0.5, 0.0, 0.5, 1.5, 2.5, 3.5, 4.5 },
		["古典现场"] = new double[10] { 2.0, 1.0, 0.5, 0.0, -0.5, 0.0, 0.5, 1.0, 1.5, 2.0 }
	};

	public static string[] Names => Presets.Keys.ToArray();
}
