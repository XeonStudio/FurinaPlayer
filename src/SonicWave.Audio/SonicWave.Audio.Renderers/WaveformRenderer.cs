using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using NAudio.Wave;

namespace SonicWave.Audio.Renderers;

public static class WaveformRenderer
{
	public struct PeakPair(float min, float max)
	{
		public float Min = min;

		public float Max = max;
	}

	public sealed class AuditionEnvelopeData
	{
		public float[] LeftMin { get; init; } = Array.Empty<float>();
		public float[] LeftMax { get; init; } = Array.Empty<float>();
		public float[] RightMin { get; init; } = Array.Empty<float>();
		public float[] RightMax { get; init; } = Array.Empty<float>();
		public float[] Peak { get; init; } = Array.Empty<float>();
		public float[] Rms { get; init; } = Array.Empty<float>();
		public int SpecCols { get; init; }
		public int SpecBins { get; init; }
		public int SampleRate { get; init; }
		public string SpecLeftBase64 { get; init; } = string.Empty;
		public string SpecRightBase64 { get; init; } = string.Empty;
	}

	private static readonly ConcurrentDictionary<string, AuditionEnvelopeData> s_envelopeCache = new(StringComparer.OrdinalIgnoreCase);

	private static void InPlaceFft(float[] rex, float[] imx, int n)
	{
		int j = 0;
		for (int i = 0; i < n; i++)
		{
			if (i < j)
			{
				(rex[j], rex[i]) = (rex[i], rex[j]);
				(imx[j], imx[i]) = (imx[i], imx[j]);
			}
			int m = n >> 1;
			while (m >= 1 && j >= m)
			{
				j -= m;
				m >>= 1;
			}
			j += m;
		}
		for (int s = 1; (1 << s) <= n; s++)
		{
			int m = 1 << s;
			int half = m >> 1;
			double theta = -2.0 * Math.PI / m;
			float wpr = (float)Math.Cos(theta);
			float wpi = (float)Math.Sin(theta);
			for (int k = 0; k < n; k += m)
			{
				float wr = 1f, wi = 0f;
				for (int jj = 0; jj < half; jj++)
				{
					int t = k + jj + half;
					int u = k + jj;
					float tr = wr * rex[t] - wi * imx[t];
					float ti = wr * imx[t] + wi * rex[t];
					rex[t] = rex[u] - tr;
					imx[t] = imx[u] - ti;
					rex[u] += tr;
					imx[u] += ti;
					float wtemp = wr;
					wr = wr * wpr - wi * wpi;
					wi = wtemp * wpi + wi * wpr;
				}
			}
		}
	}

	public static AuditionEnvelopeData? ComputeAuditionEnvelopeFromFile(string filePath, int bucketCount = 4096)
	{
		if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath) || bucketCount <= 0)
		{
			return null;
		}

		string cacheKey = $"{filePath}|{bucketCount}|spec2";
		if (s_envelopeCache.TryGetValue(cacheKey, out var cached))
		{
			return cached;
		}

		try
		{
			using WaveStream stream = CreateWaveStream(filePath);
			ISampleProvider sampleProvider = stream.ToSampleProvider();
			int channels = Math.Max(1, sampleProvider.WaveFormat.Channels);
			int sampleRate = Math.Max(8000, sampleProvider.WaveFormat.SampleRate);
			long totalFrames = (long)(stream.TotalTime.TotalSeconds * sampleRate);
			if (totalFrames <= 0)
			{
				totalFrames = Math.Max(1L, stream.Length / Math.Max(1, (sampleProvider.WaveFormat.BitsPerSample / 8) * channels));
			}

			float[] leftMin = new float[bucketCount];
			float[] leftMax = new float[bucketCount];
			float[] rightMin = new float[bucketCount];
			float[] rightMax = new float[bucketCount];
			float[] peak = new float[bucketCount];
			float[] rms = new float[bucketCount];
			double[] sumSq = new double[bucketCount];
			int[] counts = new int[bucketCount];

			// Adobe Audition STFT Spectrogram setup (Blackman-Harris window, perceptual log-frequency scale)
			const int specCols = 768;
			const int specBins = 80;
			const int fftN = 2048;
			const int fftMask = fftN - 1;
			const int halfN = fftN / 2;

			float[] win = new float[fftN];
			for (int i = 0; i < fftN; i++)
			{
				const double a0 = 0.35875, a1 = 0.48829, a2 = 0.14128, a3 = 0.01168;
				double x = 2.0 * Math.PI * i / (fftN - 1);
				win[i] = (float)(a0 - a1 * Math.Cos(x) + a2 * Math.Cos(2.0 * x) - a3 * Math.Cos(3.0 * x));
			}

			double f0 = 2600.0;
			double fMax = Math.Min(22000.0, sampleRate * 0.485);
			int[] binStart = new int[specBins];
			int[] binEnd = new int[specBins];
			float[] binCenterFrac = new float[specBins];
			for (int b = 0; b < specBins; b++)
			{
				double u0 = (double)b / specBins;
				double u1 = (double)(b + 1) / specBins;
				double uMid = (b + 0.5) / specBins;
				double hz0 = f0 * (Math.Pow(1.0 + fMax / f0, u0) - 1.0);
				double hz1 = f0 * (Math.Pow(1.0 + fMax / f0, u1) - 1.0);
				double hzMid = f0 * (Math.Pow(1.0 + fMax / f0, uMid) - 1.0);
				binCenterFrac[b] = (float)Math.Clamp(hzMid * fftN / sampleRate, 1.0, halfN - 2.0);
				int k0 = (int)Math.Floor(hz0 * fftN / sampleRate);
				int k1 = (int)Math.Ceiling(hz1 * fftN / sampleRate);
				binStart[b] = Math.Clamp(k0, 1, halfN - 1);
				binEnd[b] = Math.Clamp(Math.Max(k0 + 1, k1), 1, halfN);
			}

			float[] ringL = new float[fftN];
			float[] ringR = new float[fftN];
			float[] rex = new float[fftN];
			float[] imx = new float[fftN];
			float[] magBuf = new float[halfN];
			float[] specMaxL = new float[specCols * specBins];
			float[] specMaxR = new float[specCols * specBins];

			int totalHops = specCols * 2;
			long nextHopFrame = Math.Max((long)fftN, totalFrames / (totalHops * 2));
			int currentHop = 0;
			float normScale = 1f / (fftN * 0.179375f);
			long frameIndex = 0L;

			void ProcessHop(int colIdx)
			{
				int baseIdx = colIdx * specBins;
				int writePos = (int)(frameIndex & fftMask);
				for (int chIdx = 0; chIdx < 2; chIdx++)
				{
					float[] ring = chIdx == 0 ? ringL : ringR;
					float[] targetMax = chIdx == 0 ? specMaxL : specMaxR;
					for (int i = 0; i < fftN; i++)
					{
						float s = ring[(writePos + i) & fftMask];
						rex[i] = s * win[i];
						imx[i] = 0f;
					}
					InPlaceFft(rex, imx, fftN);
					for (int k = 1; k < halfN; k++)
					{
						magBuf[k] = MathF.Sqrt(rex[k] * rex[k] + imx[k] * imx[k]) * normScale;
					}
					for (int b = 0; b < specBins; b++)
					{
						int k0 = binStart[b];
						int k1 = binEnd[b];
						float m = 0f;
						if (k1 - k0 <= 2)
						{
							float kf = binCenterFrac[b];
							int ki = (int)kf;
							float frac = kf - ki;
							m = magBuf[ki] * (1f - frac) + magBuf[ki + 1] * frac;
							if (magBuf[k0] > m) m = magBuf[k0];
						}
						else
						{
							for (int k = k0; k < k1; k++)
							{
								if (magBuf[k] > m) m = magBuf[k];
							}
						}
						if (m > targetMax[baseIdx + b])
						{
							targetMax[baseIdx + b] = m;
						}
					}
				}
			}

			float[] buffer = new float[65536 * channels];
			int read;

			while ((read = sampleProvider.Read(buffer, 0, buffer.Length)) > 0)
			{
				for (int i = 0; i < read; i += channels)
				{
					long bLong = frameIndex * bucketCount / totalFrames;
					if (bLong >= bucketCount) bLong = bucketCount - 1;
					int b = (int)bLong;

					float l = buffer[i];
					float r = (channels > 1 && i + 1 < read) ? buffer[i + 1] : l;

					if (l < leftMin[b]) leftMin[b] = l;
					if (l > leftMax[b]) leftMax[b] = l;
					if (r < rightMin[b]) rightMin[b] = r;
					if (r > rightMax[b]) rightMax[b] = r;

					float absMax = Math.Max(Math.Abs(l), Math.Abs(r));
					if (absMax > peak[b]) peak[b] = absMax;

					sumSq[b] += 0.5 * (l * l + r * r);
					counts[b]++;

					int rIdx = (int)(frameIndex & fftMask);
					ringL[rIdx] = l;
					ringR[rIdx] = r;
					frameIndex++;

					if (currentHop < totalHops && frameIndex >= nextHopFrame)
					{
						int colIdx = Math.Clamp(currentHop / 2, 0, specCols - 1);
						ProcessHop(colIdx);
						currentHop++;
						nextHopFrame = (long)((currentHop + 0.5) * totalFrames / totalHops);
					}
				}
			}

			byte[] specLBytes = new byte[specCols * specBins];
			byte[] specRBytes = new byte[specCols * specBins];
			for (int idx = 0; idx < specCols * specBins; idx++)
			{
				double dbL = 20.0 * Math.Log10(Math.Max(1e-6, specMaxL[idx]));
				double dbR = 20.0 * Math.Log10(Math.Max(1e-6, specMaxR[idx]));
				int qL = (int)Math.Round(Math.Clamp((dbL + 84.0) / 80.0, 0.0, 1.0) * 255.0);
				int qR = (int)Math.Round(Math.Clamp((dbR + 84.0) / 80.0, 0.0, 1.0) * 255.0);
				specLBytes[idx] = (byte)Math.Clamp(qL, 0, 255);
				specRBytes[idx] = (byte)Math.Clamp(qR, 0, 255);
			}

			for (int b = 0; b < bucketCount; b++)
			{
				leftMin[b] = Math.Clamp(leftMin[b], -1f, 0f);
				leftMax[b] = Math.Clamp(leftMax[b], 0f, 1f);
				rightMin[b] = Math.Clamp(rightMin[b], -1f, 0f);
				rightMax[b] = Math.Clamp(rightMax[b], 0f, 1f);
				peak[b] = Math.Clamp(peak[b], 0f, 1f);
				rms[b] = counts[b] > 0 ? Math.Clamp((float)Math.Sqrt(sumSq[b] / counts[b]), 0f, 1f) : 0f;
			}

			var result = new AuditionEnvelopeData
			{
				LeftMin = leftMin,
				LeftMax = leftMax,
				RightMin = rightMin,
				RightMax = rightMax,
				Peak = peak,
				Rms = rms,
				SpecCols = specCols,
				SpecBins = specBins,
				SampleRate = sampleRate,
				SpecLeftBase64 = Convert.ToBase64String(specLBytes),
				SpecRightBase64 = Convert.ToBase64String(specRBytes)
			};
			s_envelopeCache[cacheKey] = result;
			return result;
		}
		catch
		{
			return null;
		}
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
		if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath) || targetWidth <= 0)
		{
			return list;
		}

		try
		{
			using WaveStream stream = CreateWaveStream(filePath);
			ISampleProvider sampleProvider = stream.ToSampleProvider();
			int channels = sampleProvider.WaveFormat.Channels;
			int sampleRate = sampleProvider.WaveFormat.SampleRate;
			long totalSampleFrames = (long)(stream.TotalTime.TotalSeconds * sampleRate);
			if (totalSampleFrames <= 0)
			{
				totalSampleFrames = Math.Max(1L, stream.Length / Math.Max(1, sampleProvider.WaveFormat.BitsPerSample / 8 * channels));
			}
			long framesPerBucket = Math.Max(1L, totalSampleFrames / Math.Max(1, targetWidth));
			float[] buffer = new float[Math.Max(4096, sampleRate * channels)];
			long processedFrames = 0L;
			int readCount;

			while ((readCount = sampleProvider.Read(buffer, 0, buffer.Length)) > 0)
			{
				for (int i = 0; i < readCount; i += channels)
				{
					long bucketIdx = processedFrames / framesPerBucket;
					float s = buffer[i];
					if (channels > 1 && i + 1 < readCount)
					{
						s = (buffer[i] + buffer[i + 1]) * 0.5f;
					}

					if (bucketIdx >= list.Count)
					{
						if (list.Count < targetWidth)
						{
							list.Add(new PeakPair(s, s));
						}
					}
					else if (bucketIdx < list.Count)
					{
						PeakPair pair = list[(int)bucketIdx];
						if (s < pair.Min) pair.Min = s;
						if (s > pair.Max) pair.Max = s;
						list[(int)bucketIdx] = pair;
					}
					processedFrames++;
				}
				progress?.Report((double)processedFrames / Math.Max(1L, totalSampleFrames));
			}
		}
		catch
		{
		}
		return list;
	}

	private static WaveStream CreateWaveStream(string filePath)
	{
		string ext = Path.GetExtension(filePath).ToLowerInvariant();
		if (ext == ".wav")
		{
			return new WaveFileReader(filePath);
		}
		if (ext == ".mp3")
		{
			try { return new Mp3FileReader(filePath); }
			catch { return new MediaFoundationReader(filePath); }
		}
		if (ext == ".aiff" || ext == ".aif")
		{
			return new AiffFileReader(filePath);
		}
		return new MediaFoundationReader(filePath);
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
