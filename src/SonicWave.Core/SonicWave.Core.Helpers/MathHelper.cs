using System;
using System.Numerics;
using MathNet.Numerics.IntegralTransforms;

namespace SonicWave.Core.Helpers;

public static class MathHelper
{
	public static double[] ComputeMagnitudeSpectrum(float[] samples, int sampleRate, int fftSize = 4096)
	{
		int num = Math.Min(fftSize, samples.Length);
		if (num <= 0)
		{
			return Array.Empty<double>();
		}
		Complex[] array = new Complex[num];
		for (int i = 0; i < num; i++)
		{
			array[i] = new Complex((double)samples[i] * HannWindow(i, num), 0.0);
		}
		Fourier.Forward(array, FourierOptions.NoScaling);
		int num2 = num / 2;
		double[] array2 = new double[num2];
		for (int j = 0; j < num2; j++)
		{
			double num3 = array[j].Magnitude / ((double)num / 4.0);
			array2[j] = ((num3 < 1E-09) ? (-120.0) : (20.0 * Math.Log10(num3)));
		}
		return array2;
	}

	public static double BinToFrequency(int bin, int sampleRate, int fftSize)
	{
		return (double)bin * (double)sampleRate / (double)fftSize;
	}

	public static double HannWindow(int i, int n)
	{
		return 0.5 * (1.0 - Math.Cos(Math.PI * 2.0 * (double)i / (double)(n - 1)));
	}

	public static double ToDbfs(double linear)
	{
		if (!(linear <= 1E-09))
		{
			return 20.0 * Math.Log10(linear);
		}
		return -120.0;
	}

	public static double DbToLinear(double db)
	{
		return Math.Pow(10.0, db / 20.0);
	}

	public static double PeakDb(float[] samples)
	{
		float num = 0f;
		for (int i = 0; i < samples.Length; i++)
		{
			float num2 = Math.Abs(samples[i]);
			if (num2 > num)
			{
				num = num2;
			}
		}
		return ToDbfs(num);
	}

	public static double RmsDb(float[] samples)
	{
		if (samples.Length == 0)
		{
			return -120.0;
		}
		double num = 0.0;
		for (int i = 0; i < samples.Length; i++)
		{
			num += (double)samples[i] * (double)samples[i];
		}
		return ToDbfs(Math.Sqrt(num / (double)samples.Length));
	}

	public static double PhaseDifference(float[] left, float[] right)
	{
		int num = Math.Min(left.Length, right.Length);
		if (num == 0)
		{
			return 0.0;
		}
		double num2 = 0.0;
		double num3 = 0.0;
		double num4 = 0.0;
		double num5 = 0.0;
		double num6 = 0.0;
		for (int i = 0; i < num; i++)
		{
			double num7 = left[i];
			double num8 = right[i];
			num2 += num7;
			num3 += num8;
			num5 += num7 * num7;
			num6 += num8 * num8;
			num4 += num7 * num8;
		}
		double num9 = num4 - num2 * num3 / (double)num;
		double num10 = num5 - num2 * num2 / (double)num;
		double num11 = num6 - num3 * num3 / (double)num;
		if (num10 <= 1E-12 || num11 <= 1E-12)
		{
			return 0.0;
		}
		return Math.Clamp(num9 / Math.Sqrt(num10 * num11), -1.0, 1.0) * 90.0;
	}

	public static float[] ResampleLinear(float[] input, double ratio)
	{
		if (Math.Abs(ratio - 1.0) < 1E-06)
		{
			return (float[])input.Clone();
		}
		int num = (int)((double)input.Length / ratio);
		float[] array = new float[num];
		for (int i = 0; i < num; i++)
		{
			double num2 = (double)i * ratio;
			int num3 = (int)num2;
			int num4 = Math.Min(num3 + 1, input.Length - 1);
			double num5 = num2 - (double)num3;
			array[i] = (float)((double)input[num3] * (1.0 - num5) + (double)input[num4] * num5);
		}
		return array;
	}
}
