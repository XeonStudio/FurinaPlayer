using System;
using System.Collections.Generic;
using SonicWave.Core.Helpers;

namespace SonicWave.Audio.Dsp;

public sealed class FftAnalyzer
{
	private int _fftSize = 4096;

	private readonly double[] _smoothing = Array.Empty<double>();

	public int FftSize
	{
		get
		{
			return _fftSize;
		}
		set
		{
			if (value >= 256 && (value & (value - 1)) == 0)
			{
				_fftSize = value;
			}
		}
	}

	public List<SpectrumPoint> Compute(float[] samples, int sampleRate, bool logarithmic = true, int bands = 128)
	{
		double[] array = MathHelper.ComputeMagnitudeSpectrum(samples, sampleRate, _fftSize);
		if (array.Length == 0)
		{
			return new List<SpectrumPoint>();
		}
		if (!logarithmic)
		{
			return LinearScale(array, sampleRate);
		}
		return LogScale(array, sampleRate, bands);
	}

	private List<SpectrumPoint> LinearScale(double[] mags, int sampleRate)
	{
		List<SpectrumPoint> list = new List<SpectrumPoint>(mags.Length);
		for (int i = 0; i < mags.Length; i++)
		{
			double num = MathHelper.BinToFrequency(i, sampleRate, _fftSize);
			if (!(num < 20.0) && !(num > 20000.0))
			{
				list.Add(new SpectrumPoint(num, mags[i]));
			}
		}
		return list;
	}

	private List<SpectrumPoint> LogScale(double[] mags, int sampleRate, int bands)
	{
		double d = 20.0;
		double d2 = Math.Min(20000.0, (double)sampleRate / 2.0);
		double num = Math.Log10(d);
		double num2 = Math.Log10(d2);
		List<SpectrumPoint> list = new List<SpectrumPoint>(bands);
		for (int i = 0; i < bands; i++)
		{
			double num3 = Math.Pow(10.0, num + (num2 - num) * (double)i / (double)bands);
			double num4 = Math.Pow(10.0, num + (num2 - num) * (double)(i + 1) / (double)bands);
			int num5 = Math.Max(0, (int)(num3 * (double)_fftSize / (double)sampleRate));
			int num6 = Math.Min(mags.Length, (int)(num4 * (double)_fftSize / (double)sampleRate) + 1);
			if (num6 > num5)
			{
				double num7 = double.MinValue;
				for (int j = num5; j < num6; j++)
				{
					num7 = Math.Max(num7, mags[j]);
				}
				list.Add(new SpectrumPoint(Math.Sqrt(num3 * num4), num7));
			}
		}
		return list;
	}
}
