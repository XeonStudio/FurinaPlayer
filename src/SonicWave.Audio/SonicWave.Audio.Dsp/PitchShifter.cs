using System;
using System.Numerics;
using MathNet.Numerics.IntegralTransforms;
using SonicWave.Core.Helpers;

namespace SonicWave.Audio.Dsp;

public sealed class PitchShifter
{
	private readonly int _fftSize;

	private readonly int _analysisHop;

	public PitchShifter(int fftSize = 2048)
	{
		_fftSize = fftSize;
		_analysisHop = fftSize / 4;
	}

	public float[] TimeStretch(float[] input, double ratio)
	{
		if (input.Length == 0 || Math.Abs(ratio - 1.0) < 1E-06)
		{
			return (float[])input.Clone();
		}
		ratio = Math.Clamp(ratio, 0.3, 3.0);
		int fftSize = _fftSize;
		int num = (int)Math.Round((double)_analysisHop * ratio);
		if (num < 1)
		{
			num = 1;
		}
		if (input.Length < fftSize)
		{
			return (float[])input.Clone();
		}
		int num2 = (input.Length - fftSize) / _analysisHop + 1;
		int num3 = num2 * num + fftSize;
		double[] array = new double[num3];
		double[] array2 = new double[num3];
		double[] array3 = BuildWindow(fftSize);
		double[] array4 = new double[fftSize / 2 + 1];
		double[] array5 = new double[fftSize / 2 + 1];
		int num4 = 0;
		int num5 = 0;
		while (num4 + fftSize <= input.Length)
		{
			Complex[] array6 = new Complex[fftSize];
			for (int i = 0; i < fftSize; i++)
			{
				array6[i] = new Complex((double)input[num4 + i] * array3[i], 0.0);
			}
			Fourier.Forward(array6, FourierOptions.Default);
			double num6 = Math.PI * 2.0 * (double)_analysisHop / (double)fftSize;
			for (int j = 0; j <= fftSize / 2; j++)
			{
				double magnitude = array6[j].Magnitude;
				double num7 = Math.Atan2(array6[j].Imaginary, array6[j].Real);
				double num8 = array4[j] + num6 * (double)j;
				double num9 = WrapPi(num7 - num8);
				array5[j] = WrapPi(array5[j] + num9 + num6 * ratio * (double)j);
				array4[j] = num7;
				array6[j] = new Complex(magnitude * Math.Cos(array5[j]), magnitude * Math.Sin(array5[j]));
			}
			for (int k = fftSize / 2 + 1; k < fftSize; k++)
			{
				array6[k] = new Complex(array6[fftSize - k].Real, 0.0 - array6[fftSize - k].Imaginary);
			}
			Fourier.Inverse(array6, FourierOptions.Default);
			for (int l = 0; l < fftSize; l++)
			{
				array[num5 + l] += array6[l].Real * array3[l];
				array2[num5 + l] += array3[l] * array3[l];
			}
			num4 += _analysisHop;
			num5 += num;
		}
		float[] array7 = new float[num2 * num];
		for (int m = 0; m < array7.Length; m++)
		{
			double num10 = array2[m];
			array7[m] = ((num10 > 1E-06) ? ((float)Math.Clamp(array[m] / num10, -1.0, 1.0)) : 0f);
		}
		return array7;
	}

	public float[] PitchShift(float[] input, double semitones)
	{
		if (input.Length == 0 || Math.Abs(semitones) < 0.01)
		{
			return (float[])input.Clone();
		}
		double value = Math.Pow(2.0, semitones / 12.0);
		value = Math.Clamp(value, 0.5, 2.0);
		float[] input2 = MathHelper.ResampleLinear(input, value);
		return TimeStretch(input2, value);
	}

	private static double WrapPi(double phase)
	{
		while (phase > Math.PI)
		{
			phase -= Math.PI * 2.0;
		}
		while (phase < -Math.PI)
		{
			phase += Math.PI * 2.0;
		}
		return phase;
	}

	private static double[] BuildWindow(int n)
	{
		double[] array = new double[n];
		for (int i = 0; i < n; i++)
		{
			array[i] = MathHelper.HannWindow(i, n);
		}
		return array;
	}
}
