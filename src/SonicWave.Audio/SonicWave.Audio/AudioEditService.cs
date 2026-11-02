using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NAudio.Wave;
using SonicWave.Audio.Decoders;

namespace SonicWave.Audio;

public static class AudioEditService
{
	public static float[] Trim(float[] samples, int channels, long startSample, long endSample)
	{
		if (channels <= 0 || startSample < 0)
		{
			throw new ArgumentOutOfRangeException();
		}
		int num = samples.Length / channels;
		int num2 = (int)Math.Min(startSample, num);
		int num3 = (int)Math.Min(endSample, num);
		if (num3 <= num2)
		{
			return Array.Empty<float>();
		}
		float[] array = new float[(num3 - num2) * channels];
		Array.Copy(samples, num2 * channels, array, 0, array.Length);
		return array;
	}

	public static float[] CutSelection(float[] samples, int channels, long startSample, long endSample)
	{
		float[] array = Trim(samples, channels, 0L, startSample);
		float[] array2 = Trim(samples, channels, endSample, long.MaxValue);
		float[] array3 = new float[array.Length + array2.Length];
		array.CopyTo(array3, 0);
		array2.CopyTo(array3, array.Length);
		return array3;
	}

	public static void ApplyFade(float[] samples, int channels, double fadeInSeconds, double fadeOutSeconds, double sampleRate, string curve = "linear")
	{
		int num = (int)(fadeInSeconds * sampleRate) * channels;
		int num2 = (int)(fadeOutSeconds * sampleRate) * channels;
		if (num > 0)
		{
			for (int i = 0; i < Math.Min(num, samples.Length); i++)
			{
				double t = (double)i / (double)Math.Max(1, num / channels) / (double)channels;
				samples[i] *= (float)FadeCurve(t, curve);
			}
		}
		if (num2 > 0)
		{
			for (int j = Math.Max(0, samples.Length - num2); j < samples.Length; j++)
			{
				double t2 = (double)(samples.Length - 1 - j) / (double)Math.Max(1, num2 / channels) / (double)channels;
				samples[j] *= (float)FadeCurve(t2, curve);
			}
		}
	}

	public static void NormalizePeak(float[] samples, double targetDbfs = -1.0)
	{
		float num = 0f;
		for (int i = 0; i < samples.Length; i++)
		{
			num = Math.Max(num, Math.Abs(samples[i]));
		}
		if (!((double)num <= 1E-09))
		{
			double num2 = Math.Pow(10.0, targetDbfs / 20.0) / (double)num;
			for (int j = 0; j < samples.Length; j++)
			{
				samples[j] = (float)Math.Clamp((double)samples[j] * num2, -1.0, 1.0);
			}
		}
	}

	public static void NormalizeRms(float[] samples, double targetLufs = -16.0)
	{
		if (samples.Length == 0)
		{
			return;
		}
		double num = 0.0;
		for (int i = 0; i < samples.Length; i++)
		{
			num += (double)samples[i] * (double)samples[i];
		}
		double num2 = Math.Sqrt(num / (double)samples.Length);
		if (!(num2 <= 1E-09))
		{
			double num3 = Math.Pow(10.0, targetLufs / 20.0) / num2;
			for (int j = 0; j < samples.Length; j++)
			{
				samples[j] = (float)Math.Clamp((double)samples[j] * num3, -1.0, 1.0);
			}
		}
	}

	public static float[] Concatenate(params float[][] parts)
	{
		float[] array = new float[parts.Sum((float[] p) => p.Length)];
		int num = 0;
		foreach (float[] array2 in parts)
		{
			array2.CopyTo(array, num);
			num += array2.Length;
		}
		return array;
	}

	public static void ExportWav(string filePath, float[] samples, int sampleRate, int channels)
	{
		string directoryName = Path.GetDirectoryName(filePath);
		if (!string.IsNullOrEmpty(directoryName))
		{
			Directory.CreateDirectory(directoryName);
		}
		using WaveFileWriter waveFileWriter = new WaveFileWriter(filePath, new WaveFormat(sampleRate, 16, channels));
		byte[] array = new byte[samples.Length * 2];
		for (int i = 0; i < samples.Length; i++)
		{
			short num = (short)Math.Clamp((int)(samples[i] * 32767f), -32768, 32767);
			array[i * 2] = (byte)(num & 0xFF);
			array[i * 2 + 1] = (byte)((num >> 8) & 0xFF);
		}
		waveFileWriter.Write(array, 0, array.Length);
	}

	public static (float[] Samples, WaveFormat Format) LoadWav(string filePath)
	{
		using SafeAudioFileReader audioFileReader = new SafeAudioFileReader(filePath);
		WaveFormat waveFormat = audioFileReader.WaveFormat;
		int estimatedSamples = 0;
		if (audioFileReader.Length > 0 && waveFormat.BitsPerSample > 0)
		{
			estimatedSamples = (int)(audioFileReader.Length / (waveFormat.BitsPerSample / 8));
		}
		else if (audioFileReader.TotalTime.TotalSeconds > 0)
		{
			estimatedSamples = (int)(audioFileReader.TotalTime.TotalSeconds * waveFormat.SampleRate * waveFormat.Channels);
		}

		var samples = new List<float>(estimatedSamples > 0 ? estimatedSamples : 44100 * 2 * 60);
		float[] buffer = new float[16384];
		int read;
		while ((read = audioFileReader.Read(buffer, 0, buffer.Length)) > 0)
		{
			for (int k = 0; k < read; k++)
			{
				samples.Add(buffer[k]);
			}
		}
		return (Samples: samples.ToArray(), Format: waveFormat);
	}

	private static double FadeCurve(double t, string curve)
	{
		if (!(curve == "exponential"))
		{
			if (curve == "s-curve")
			{
				return t * t * (3.0 - 2.0 * t);
			}
			return t;
		}
		return t * t;
	}
}
