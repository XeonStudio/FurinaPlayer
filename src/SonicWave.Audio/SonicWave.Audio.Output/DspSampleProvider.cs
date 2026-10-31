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

	private Vst3PluginState? _exclusiveVst3;

	private float[] _scratch = Array.Empty<float>();

	public WaveFormat WaveFormat { get; }

	public Equalizer Equalizer => _equalizer;

	public SpatialAudioEffect Spatial => _spatial;

	public EchoDspChain DspChain => _dspChain;

	public double LatestPeakDb { get; private set; } = -60.0;

	public double LatestRmsDb { get; private set; } = -60.0;

	public float[] LatestBands { get; private set; } = new float[64];
	public float LatestStereoBalance { get; private set; } = 0f;
	public float LatestPhaseCorrelation { get; private set; } = 1f;

	private readonly float[] _fftRing = new float[1024];
	private int _fftRingPos;
	private readonly float[] _fftRex = new float[1024];
	private readonly float[] _fftImx = new float[1024];
	private readonly float[] _fftWin = new float[1024];

	public DspSampleProvider(ISampleProvider source, int sampleRate, int channels)
	{
		_source = source;
		WaveFormat = NAudio.Wave.WaveFormat.CreateIeeeFloatWaveFormat(sampleRate, channels);
		_equalizer = new Equalizer(sampleRate);
		_timbreEqualizer = new Equalizer(sampleRate);
		_widener = new StereoWidener();
		_spatial = new SpatialAudioEffect(sampleRate);
		_dspChain = new EchoDspChain(sampleRate);
		for (int i = 0; i < 1024; i++)
		{
			double a0 = 0.35875, a1 = 0.48829, a2 = 0.14128, a3 = 0.01168;
			double x = 2.0 * Math.PI * i / 1023.0;
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
		_vst3 = plugins ?? Array.Empty<Vst3PluginState>();
		Vst3PluginState exclusiveVst = null;
		foreach (Vst3PluginState item in _vst3)
		{
			if (item != null && item.IsActive)
			{
				exclusiveVst = item;
				break;
			}
		}
		_exclusiveVst3 = exclusiveVst;
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
		_equalizer.ProcessInterleaved(scratch, WaveFormat.Channels);
		_timbreEqualizer.ProcessInterleaved(scratch, WaveFormat.Channels);
		_widener.ProcessInterleaved(scratch, WaveFormat.Channels);
		_spatial.ProcessInterleaved(scratch, WaveFormat.Channels);
		_dspChain.ProcessBlock(scratch, 0, num, WaveFormat.Channels);
		Vst3PluginState exclusiveVst = _exclusiveVst3;
		if (exclusiveVst != null && Vst3NativeHost.Instance.IsLoadedFor(exclusiveVst.Path))
		{
			Vst3NativeHost.Instance.Process(scratch, WaveFormat.Channels);
		}
		else
		{
			ApplyVst3(scratch);
		}
		// Real-time meters & FFT spectrum for mastering analyzers
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

			// Downmix to mono into FFT ring buffer
			_fftRing[_fftRingPos] = 0.5f * (l + r);
			_fftRingPos = (_fftRingPos + 1) & 1023;
		}

		int frameCount = Math.Max(1, num / channels);
		double rms = Math.Sqrt(sumSq / frameCount);
		LatestPeakDb = (maxVal > 0.00001f) ? Math.Max(-60.0, 20.0 * Math.Log10(maxVal)) : -60.0;
		LatestRmsDb = (rms > 0.00001) ? Math.Max(-60.0, 20.0 * Math.Log10(rms)) : -60.0;

		double denom = Math.Sqrt(sumSqL * sumSqR);
		float instCorr = (float)Math.Clamp(denom > 1e-9 ? (sumDotLR / denom) : 1.0, -1.0, 1.0);
		float instBal = (float)Math.Clamp((sumSqL + sumSqR) > 1e-9 ? ((sumSqR - sumSqL) / (sumSqL + sumSqR)) : 0.0, -1.0, 1.0);
		LatestPhaseCorrelation = LatestPhaseCorrelation * 0.7f + instCorr * 0.3f;
		LatestStereoBalance = LatestStereoBalance * 0.7f + instBal * 0.3f;

		// Compute true 64-band perceptual log FFT spectrum (iZotope Insight 2 style, 20Hz ~ 20kHz)
		const int fftN = 1024;
		int readPos = _fftRingPos;
		for (int i = 0; i < fftN; i++)
		{
			_fftRex[i] = _fftRing[(readPos + i) & 1023] * _fftWin[i];
			_fftImx[i] = 0f;
		}
		RunFft1024(_fftRex, _fftImx);

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
			int k0 = Math.Clamp((int)Math.Floor(f0 * fftN / sr), 1, 511);
			int k1 = Math.Clamp((int)Math.Ceiling(f1 * fftN / sr), k0 + 1, 512);

			float maxMag = 0f;
			for (int k = k0; k < k1; k++)
			{
				float mag = MathF.Sqrt(_fftRex[k] * _fftRex[k] + _fftImx[k] * _fftImx[k]) / (fftN * 0.179375f);
				if (mag > maxMag) maxMag = mag;
			}
			// Map [-66 dBFS .. 0 dBFS] to [0.0 .. 1.0]
			double db = 20.0 * Math.Log10(Math.Max(1e-5, maxMag));
			bands[b] = (float)Math.Clamp((db + 66.0) / 60.0, 0.0, 1.0);
		}
		LatestBands = bands;

		scratch.CopyTo(destination);
		return num;
	}

	private static void RunFft1024(float[] rex, float[] imx)
	{
		const int n = 1024;
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

	private void ApplyVst3(float[] buffer)
	{
		if (_vst3 == null || _vst3.Count == 0)
		{
			return;
		}
		foreach (Vst3PluginState item in _vst3)
		{
			if (item != null && item.IsActive)
			{
				Vst3SimulatedProcessor.Process(buffer, WaveFormat.Channels, new Vst3Parameters(item.Drive, item.Tone, item.Mix));
			}
		}
	}
}
