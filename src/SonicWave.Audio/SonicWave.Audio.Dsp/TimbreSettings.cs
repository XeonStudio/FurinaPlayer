using System;

namespace SonicWave.Audio.Dsp;

public sealed class TimbreSettings
{
	public double Bass { get; set; }

	public double Midrange { get; set; }

	public double Treble { get; set; }

	public double Thickness { get; set; }

	public double Clarity { get; set; }

	public double Soundstage { get; set; }

	public bool IsActive
	{
		get
		{
			if (!(Math.Abs(Bass) > 0.01) && !(Math.Abs(Midrange) > 0.01) && !(Math.Abs(Treble) > 0.01) && !(Math.Abs(Thickness) > 0.01) && !(Math.Abs(Clarity) > 0.01))
			{
				return Math.Abs(Soundstage) > 0.01;
			}
			return true;
		}
	}

	public double WidenerAmount => Math.Clamp(Soundstage / 10.0 * 0.8, -0.8, 0.8);

	public double[] BuildEqGains()
	{
		double[] array = new double[10];
		for (int i = 0; i < 10; i++)
		{
			double num = 0.0;
			num += BandProfile(i, 0.9, 0.8, 0.6, 0.3, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0) * Bass;
			num += BandProfile(i, 0.0, 0.0, 0.0, 0.4, 0.7, 0.8, 0.7, 0.4, 0.0, 0.0) * Midrange;
			num += BandProfile(i, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.4, 0.7, 0.9) * Treble;
			num += BandProfile(i, 0.2, 0.5, 0.8, 0.6, 0.3, 0.0, -0.2, -0.4, -0.3, -0.2) * Thickness;
			num += BandProfile(i, 0.0, 0.0, 0.0, -0.2, -0.1, 0.2, 0.4, 0.7, 0.8, 0.6) * Clarity;
			array[i] = Math.Clamp(num, -12.0, 12.0);
		}
		return array;
	}

	private static double BandProfile(int index, double b0, double b1, double b2, double b3, double b4, double b5, double b6, double b7, double b8, double b9)
	{
		double[] array = new double[10] { b0, b1, b2, b3, b4, b5, b6, b7, b8, b9 };
		if (index < 0 || index >= array.Length)
		{
			return 0.0;
		}
		return array[index];
	}
}
