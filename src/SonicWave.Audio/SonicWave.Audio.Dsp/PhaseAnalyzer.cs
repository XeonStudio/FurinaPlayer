using System;
using System.Collections.Generic;
using SonicWave.Core.Helpers;

namespace SonicWave.Audio.Dsp;

public static class PhaseAnalyzer
{
	public readonly record struct LissajousPoint(double X, double Y);

	public static List<LissajousPoint> ComputeLissajous(float[] left, float[] right, int maxPoints = 2048)
	{
		int num = Math.Min(left.Length, right.Length);
		int num2 = Math.Max(1, (num + maxPoints - 1) / maxPoints);
		List<LissajousPoint> list = new List<LissajousPoint>(num / num2 + 1);
		for (int i = 0; i < num; i += num2)
		{
			list.Add(new LissajousPoint(left[i], right[i]));
		}
		return list;
	}

	public static double PhaseDifferenceDegrees(float[] left, float[] right)
	{
		return MathHelper.PhaseDifference(left, right);
	}

	public static double StereoCorrelation(float[] left, float[] right)
	{
		return MathHelper.PhaseDifference(left, right) / 90.0;
	}
}
