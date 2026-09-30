using System;
using System.Collections.Generic;
using NAudio.Wave;

namespace SonicWave.Audio.Renderers;

public static class WaveformRenderer
{
	public struct PeakPair(float min, float max)
	{
		public float Min = min;

		public float Max = max;
	}

	public static List<PeakPair> ComputePeaks(float[] samples, int channels, int targetWidth, int startSample = 0, int sampleCount = -1)
	{
		if (samples.Length == 0 || targetWidth <= 0)
		{
			return new List<PeakPair>();
		}
		if (sampleCount < 0)
		{
			sampleCount = samples.Length - startSample;
		}
		sampleCount = Math.Min(sampleCount, samples.Length - startSample);
		if (sampleCount <= 0)
		{
			return new List<PeakPair>();
		}
		float[] array = ((channels > 1) ? DownmixToMono(samples, channels, startSample, sampleCount) : samples.AsSpan(startSample, sampleCount).ToArray());
		List<PeakPair> list = new List<PeakPair>(targetWidth);
		int num = Math.Min(targetWidth, array.Length);
		for (int i = 0; i < num; i++)
		{
			int num2 = (int)((long)i * (long)array.Length / num);
			int num3 = (int)((long)(i + 1) * (long)array.Length / num);
			if (num3 <= num2)
			{
				continue;
			}
			float num4 = float.MaxValue;
			float num5 = float.MinValue;
			for (int j = num2; j < num3; j++)
			{
				if (array[j] < num4)
				{
					num4 = array[j];
				}
				if (array[j] > num5)
				{
					num5 = array[j];
				}
			}
			list.Add(new PeakPair(num4, num5));
		}
		return list;
	}

	public static List<PeakPair> ComputePeaksFromFile(string filePath, int targetWidth, IProgress<double>? progress = null)
	{
		List<PeakPair> list = new List<PeakPair>();
		using AudioFileReader audioFileReader = new AudioFileReader(filePath);
		int channels = audioFileReader.WaveFormat.Channels;
		int sampleRate = audioFileReader.WaveFormat.SampleRate;
		long num = (long)(audioFileReader.TotalTime.TotalSeconds * (double)sampleRate);
		long num2 = Math.Max(1L, num / Math.Max(1, targetWidth));
		float[] array = new float[audioFileReader.WaveFormat.AverageBytesPerSecond];
		long num3 = 0L;
		int num4;
		while ((num4 = audioFileReader.Read(array, 0, array.Length)) > 0)
		{
			for (int i = 0; i < num4; i += channels)
			{
				long num5 = num3 / num2;
				float num6 = Math.Abs(array[i]);
				if (channels > 1 && i + 1 < num4)
				{
					num6 = Math.Max(num6, Math.Abs(array[i + 1]));
				}
				if (num5 >= list.Count)
				{
					list.Add(new PeakPair(num6, num6));
				}
				else
				{
					PeakPair value = list[(int)num5];
					if (num6 < value.Min)
					{
						value.Min = num6;
					}
					if (num6 > value.Max)
					{
						value.Max = num6;
					}
					list[(int)num5] = value;
				}
				num3++;
			}
			progress?.Report((double)num3 / (double)Math.Max(1L, num));
		}
		for (int j = 0; j < list.Count; j++)
		{
			PeakPair peakPair = list[j];
			list[j] = new PeakPair(0f - peakPair.Min, peakPair.Max);
		}
		return list;
	}

	private static float[] DownmixToMono(float[] samples, int channels, int start, int count)
	{
		int num = count / channels;
		float[] array = new float[num];
		for (int i = 0; i < num; i++)
		{
			float num2 = 0f;
			for (int j = 0; j < channels; j++)
			{
				float num3 = Math.Abs(samples[start + i * channels + j]);
				if (num3 > num2)
				{
					num2 = num3;
				}
			}
			array[i] = num2;
		}
		return array;
	}
}
