using System;
using System.Collections.Generic;
using NAudio.Wave;
using SonicWave.Audio.Dsp;
using SonicWave.Audio.Vst3;
using SonicWave.Core.Models;

namespace SonicWave.Audio.Output;

public sealed class DspSampleProvider : ISampleProvider
{
	private readonly ISampleProvider _source;

	private readonly Equalizer _equalizer;

	private readonly Equalizer _timbreEqualizer;

	private readonly StereoWidener _widener;

	private readonly SpatialAudioEffect _spatial;

	private readonly EchoDspChain _dspChain;

	private TimbreSettings _timbre = new TimbreSettings();

	private IReadOnlyList<Vst3PluginState> _vst3 = Array.Empty<Vst3PluginState>();

	private float[] _scratch = Array.Empty<float>();

	public WaveFormat WaveFormat { get; }

	public Equalizer Equalizer => _equalizer;

	public SpatialAudioEffect Spatial => _spatial;

	public EchoDspChain DspChain => _dspChain;

	public float OutputVolume { get; set; } = 1.0f;

	public double LatestPeakDb { get; private set; } = -60.0;

	public double LatestInputPeakDb { get; private set; } = -60.0;

	public double LatestTruePeakDb { get; private set; } = -60.0;

	public double LatestRmsDb { get; private set; } = -60.0;

	public float[] LatestBands { get; private set; } = new float[64];
	public float LatestStereoBalance { get; private set; } = 0f;
	public float LatestPhaseCorrelation { get; private set; } = 1f;
	public float[] LatestVectorPoints { get; private set; } = Array.Empty<float>();

	private const int FftSize = 2048;
	private readonly float[] _fftRing = new float[FftSize];
	private int _fftRingPos;
	private readonly float[] _fftRex = new float[FftSize];
	private readonly float[] _fftImx = new float[FftSize];
	private readonly float[] _fftWin = new float[FftSize];
	private readonly float[] _fftMag = new float[FftSize / 2 + 1];

	public DspSampleProvider(ISampleProvider source, int sampleRate, int channels)
	{
		_source = source;
		WaveFormat = NAudio.Wave.WaveFormat.CreateIeeeFloatWaveFormat(sampleRate, channels);
		_equalizer = new Equalizer(sampleRate);
		_timbreEqualizer = new Equalizer(sampleRate);
		_widener = new StereoWidener();
		_spatial = new SpatialAudioEffect(sampleRate);
		_dspChain = new EchoDspChain(sampleRate);
		for (int i = 0; i < FftSize; i++)
		{
			double a0 = 0.35875, a1 = 0.48829, a2 = 0.14128, a3 = 0.01168;
			double x = 2.0 * Math.PI * i / (FftSize - 1.0);
			_fftWin[i] = (float)(a0 - a1 * Math.Cos(x) + a2 * Math.Cos(2.0 * x) - a3 * Math.Cos(3.0 * x));
		}
	}

	public void SetEqGains(double[] gains)
	{
		if (gains.Length == 10)
		{
			_equalizer.Gains = gains;
			for (int i = 0; i < 10; i++)
			{
				_dspChain.Eq.SetBandGain(i, gains[i]);
			}
		}
	}

	public void SetTimbre(TimbreSettings timbre)
	{
		_timbre = timbre ?? new TimbreSettings();
		_timbreEqualizer.Gains = _timbre.BuildEqGains();
		_widener.Amount = _timbre.WidenerAmount;
	}

	public void SetVst3(IReadOnlyList<Vst3PluginState> plugins)
	{
		Vst3PluginState[] snapshot;
		if (plugins == null || plugins.Count == 0)
		{
			snapshot = Array.Empty<Vst3PluginState>();
		}
		else
		{
			snapshot = new Vst3PluginState[plugins.Count];
			for (int i = 0; i < plugins.Count; i++)
			{
				snapshot[i] = plugins[i];
			}
		}
		_vst3 = snapshot;
		if (WaveFormat.SampleRate > 0)
		{
			try
			{
				Vst3NativeHost.Instance.EnsureChainLoaded(snapshot, WaveFormat.SampleRate, WaveFormat.Channels, unloadPrimaryWhenEmpty: false);
			}
			catch
			{
			}
		}
	}

	public void SetSpatialAudio(bool enabled, double mix, string preset, double width, double modulation, double predelayMs)
	{
		if (enabled)
		{
			_spatial.SetPreset(string.IsNullOrEmpty(preset) ? "大厅" : preset);
			_spatial.WetMix = mix;
			_spatial.Width = width;
			_spatial.Modulation = modulation;
			_spatial.PredelayMs = predelayMs;
		}
		else
		{
			_spatial.WetMix = 0.0;
		}
	}

	public void SetReverb(bool enabled, double mix, string preset)
	{
		SpatialParameters spatialParameters = SpatialAudioEffect.PresetDefaults(string.IsNullOrEmpty(preset) ? "大厅" : preset);
		SetSpatialAudio(enabled, mix, preset, spatialParameters.Width, spatialParameters.Modulation, spatialParameters.PredelayMs);
	}

	public int Read(float[] buffer, int offset, int count)
	{
		int num = _source.Read(buffer, offset, count);
		if (num <= 0)
		{
			return num;
		}
		if (_scratch.Length != num)
		{
			_scratch = new float[num];
		}
		Span<float> destination = buffer.AsSpan(offset, num);
		float[] scratch = _scratch;
		destination.CopyTo(scratch);

		// Measure input peak before DSP processing
		float inMax = 0f;
		for (int i = 0; i < num; i++)
		{
			float abs = Math.Abs(scratch[i]);
			if (abs > inMax) inMax = abs;
		}
		LatestInputPeakDb = (inMax > 0.00001f) ? Math.Max(-60.0, 20.0 * Math.Log10(inMax)) : -60.0;

		_equalizer.ProcessInterleaved(scratch, WaveFormat.Channels);
		_timbreEqualizer.ProcessInterleaved(scratch, WaveFormat.Channels);
		_widener.ProcessInterleaved(scratch, WaveFormat.Channels);
		_spatial.ProcessInterleaved(scratch, WaveFormat.Channels);
		_dspChain.ProcessBlock(scratch, 0, num, WaveFormat.Channels);
		IReadOnlyList<Vst3PluginState> vst3 = _vst3;
		if (vst3.Count > 0)
		{
			Vst3NativeHost.Instance.ProcessPlugins(scratch, WaveFormat.Channels, WaveFormat.SampleRate, vst3);
		}
		// Real-time meters & FFT spectrum for mastering analyzers (Pre-Fader true signal level)
		float maxVal = 0f;
		double sumSq = 0.0;
		double sumSqL = 0.0;
		double sumSqR = 0.0;
		double sumDotLR = 0.0;
		int channels = Math.Max(1, WaveFormat.Channels);

		for (int i = 0; i < num; i += channels)
		{
			float l = scratch[i];
			float r = (channels > 1 && i + 1 < num) ? scratch[i + 1] : l;
			float absL = Math.Abs(l);
			float absR = Math.Abs(r);
			if (absL > maxVal) maxVal = absL;
			if (absR > maxVal) maxVal = absR;
			sumSqL += l * l;
			sumSqR += r * r;
			sumDotLR += l * r;
			sumSq += 0.5 * (l * l + r * r);
		}

		// Feed FFT ring buffer with adaptive reference normalization so quiet & loud tracks both display full spectral contour
		float fftNormGain = maxVal > 0.0001f ? Math.Clamp(0.85f / Math.Max(0.18f, maxVal), 1.0f, 16.0f) : 1.0f;
		for (int i = 0; i < num; i += channels)
		{
			float l = scratch[i];
			float r = (channels > 1 && i + 1 < num) ? scratch[i + 1] : l;
			_fftRing[_fftRingPos] = 0.5f * (l + r) * fftNormGain;
			_fftRingPos = (_fftRingPos + 1) & (FftSize - 1);
		}

		int frameCount = Math.Max(1, num / channels);
		double rms = Math.Sqrt(sumSq / frameCount);
		LatestPeakDb = (maxVal > 0.00001f) ? Math.Max(-60.0, 20.0 * Math.Log10(maxVal)) : -60.0;
		LatestRmsDb = (rms > 0.00001) ? Math.Max(-60.0, 20.0 * Math.Log10(rms)) : -60.0;

		// Calculate True Peak estimation (intersample oversampling peak)
		float truePeakLinear = maxVal;
		for (int i = 1; i < num; i++)
		{
			float s0 = Math.Abs(scratch[i - 1]);
			float s1 = Math.Abs(scratch[i]);
			float estInter = 0.5f * (s0 + s1) + 0.125f * Math.Abs(s1 - s0);
			if (estInter > truePeakLinear) truePeakLinear = estInter;
		}
		LatestTruePeakDb = (truePeakLinear > 0.00001f) ? Math.Max(-60.0, 20.0 * Math.Log10(truePeakLinear)) : -60.0;

		double denom = Math.Sqrt(sumSqL * sumSqR);
		float instCorr = (float)Math.Clamp(denom > 1e-9 ? (sumDotLR / denom) : 1.0, -1.0, 1.0);
		float instBal = (float)Math.Clamp((sumSqL + sumSqR) > 1e-9 ? ((sumSqR - sumSqL) / (sumSqL + sumSqR)) : 0.0, -1.0, 1.0);
		LatestPhaseCorrelation = LatestPhaseCorrelation * 0.65f + instCorr * 0.35f;
		LatestStereoBalance = LatestStereoBalance * 0.65f + instBal * 0.35f;

		// Extract 64 local-envelope stereo sample pairs [L0, R0, L1, R1, ...] for iZotope Insight 2 Sound Field goniometer
		const int vecPairs = 64;
		float[] vec = new float[vecPairs * 2];
		float normGain = maxVal > 0.00005f ? (0.94f / maxVal) : 0.0f;
		for (int p = 0; p < vecPairs; p++)
		{
			int fStart = (int)((long)p * frameCount / vecPairs);
			int fEnd = Math.Max(fStart + 1, (int)((long)(p + 1) * frameCount / vecPairs));
			float bestL = 0f;
			float bestR = 0f;
			float bestEnergy = -1f;
			for (int f = fStart; f < fEnd && f < frameCount; f++)
			{
				int idx = f * channels;
				float curL = scratch[idx];
				float curR = (channels > 1 && idx + 1 < num) ? scratch[idx + 1] : curL;
				// Include subtle high-frequency pre-emphasis so stereo high/mid instruments are represented alongside bass
				if (f > 0)
				{
					int prevIdx = (f - 1) * channels;
					float dL = curL - scratch[prevIdx];
					float dR = curR - ((channels > 1 && prevIdx + 1 < num) ? scratch[prevIdx + 1] : scratch[prevIdx]);
					curL += dL * 1.15f;
					curR += dR * 1.15f;
				}
				float e = curL * curL + curR * curR;
				if (e > bestEnergy)
				{
					bestEnergy = e;
					bestL = curL;
					bestR = curR;
				}
			}
			vec[p * 2] = Math.Clamp(bestL * normGain, -1.0f, 1.0f);
			vec[p * 2 + 1] = Math.Clamp(bestR * normGain, -1.0f, 1.0f);
		}
		LatestVectorPoints = vec;

		// Compute true 64-band perceptual log FFT spectrum (iZotope Insight 2 style, 25Hz ~ 20kHz with 4.5 dB/oct tilt)
		int readPos = _fftRingPos;
		for (int i = 0; i < FftSize; i++)
		{
			_fftRex[i] = _fftRing[(readPos + i) & (FftSize - 1)] * _fftWin[i];
			_fftImx[i] = 0f;
		}
		RunFft(_fftRex, _fftImx, FftSize);

		int halfBins = FftSize / 2;
		float invWindowGain = 1.0f / (FftSize * 0.179375f);
		for (int k = 0; k <= halfBins; k++)
		{
			_fftMag[k] = MathF.Sqrt(_fftRex[k] * _fftRex[k] + _fftImx[k] * _fftImx[k]) * invWindowGain;
		}

		int sr = Math.Max(8000, WaveFormat.SampleRate);
		const int bandsCount = 64;
		float[] bands = new float[bandsCount];
		const double minFreq = 25.0;
		double maxFreq = Math.Min(20000.0, sr * 0.485);
		double logMin = Math.Log10(minFreq);
		double logMax = Math.Log10(maxFreq);

		for (int b = 0; b < bandsCount; b++)
		{
			double f0 = Math.Pow(10.0, logMin + (b / (double)bandsCount) * (logMax - logMin));
			double f1 = Math.Pow(10.0, logMin + ((b + 1) / (double)bandsCount) * (logMax - logMin));
			double fCenter = Math.Sqrt(f0 * f1);

			double bin0 = f0 * FftSize / sr;
			double bin1 = f1 * FftSize / sr;
			float bandMag;
			if (bin1 - bin0 < 1.5)
			{
				// Fractional bin interpolation for smooth low-frequency contour
				double binMid = Math.Clamp(fCenter * FftSize / sr, 1.0, halfBins - 1.001);
				int bi = (int)Math.Floor(binMid);
				float frac = (float)(binMid - bi);
				bandMag = _fftMag[bi] * (1.0f - frac) + _fftMag[bi + 1] * frac;
			}
			else
			{
				int k0 = Math.Clamp((int)Math.Floor(bin0), 1, halfBins - 1);
				int k1 = Math.Clamp((int)Math.Ceiling(bin1), k0 + 1, halfBins);
				float maxMag = 0f;
				double sumMagSq = 0.0;
				for (int k = k0; k < k1; k++)
				{
					float mag = _fftMag[k];
					if (mag > maxMag) maxMag = mag;
					sumMagSq += mag * mag;
				}
				float powerSum = (float)Math.Sqrt(sumMagSq);
				bandMag = maxMag * 0.65f + powerSum * 0.35f;
			}

			// iZotope Insight 2 Pink-Noise (+4.5 dB/oct) perceptual tilt + [-54 dBFS .. -2 dBFS] display range
			double rawDb = 20.0 * Math.Log10(Math.Max(1e-6, bandMag));
			double tiltDb = 4.5 * Math.Log2(Math.Max(35.0, fCenter) / 140.0);
			double displayDb = rawDb + tiltDb;
			bands[b] = (float)Math.Clamp((displayDb + 54.0) / 52.0, 0.0, 1.0);
		}
		LatestBands = bands;

		float outVol = Math.Clamp(OutputVolume, 0f, 1f);
		if (outVol < 0.999f)
		{
			for (int i = 0; i < num; i++)
			{
				destination[i] = scratch[i] * outVol;
			}
		}
		else
		{
			scratch.CopyTo(destination);
		}
		return num;
	}

	private static void RunFft(float[] rex, float[] imx, int n)
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
}
