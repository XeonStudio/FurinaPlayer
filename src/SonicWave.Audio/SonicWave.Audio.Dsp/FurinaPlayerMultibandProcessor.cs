using System;

namespace SonicWave.Audio.Dsp;

public sealed class FurinaPlayerMultibandBand
{
	private float _thresholdDb = -18.0f;
	private float _ratio = 2.0f;
	private float _attackMs = 10.0f;
	private float _releaseMs = 120.0f;
	private float _kneeDb = 6.0f;
	private float _makeupDb = 0.0f;

	public bool Enabled { get; set; } = true;
	public float ThresholdDb { get => _thresholdDb; set => _thresholdDb = Math.Clamp(value, -60.0f, 0.0f); }
	public float Ratio { get => _ratio; set => _ratio = Math.Clamp(value, 1.0f, 20.0f); }
	public float AttackMs { get => _attackMs; set => _attackMs = Math.Clamp(value, 0.1f, 200.0f); }
	public float ReleaseMs { get => _releaseMs; set => _releaseMs = Math.Clamp(value, 5.0f, 2000.0f); }
	public float KneeDb { get => _kneeDb; set => _kneeDb = Math.Clamp(value, 0.0f, 18.0f); }
	public float MakeupDb { get => _makeupDb; set => _makeupDb = Math.Clamp(value, -12.0f, 12.0f); }

	public float GainReductionDb { get; internal set; } = 0.0f;
	internal float SmoothedGain { get; set; } = 1.0f;

	public void Reset()
	{
		SmoothedGain = 1.0f;
		GainReductionDb = 0.0f;
	}
}

/// <summary>
/// 4阶 Linkwitz-Riley (LR4, 24dB/oct) 高性能二阶双二阶级联滤波器单元。
/// 由两个相同的 2 阶巴特沃斯 (Q=1/√2) 级联而成，分频点增益衰减为精确 -6dB，且全频段相位平坦相加为 0dB。
/// </summary>
internal sealed class Lr4BiquadStage
{
	public double B0, B1, B2, A1, A2;
	private readonly double[] _x1 = new double[8];
	private readonly double[] _x2 = new double[8];
	private readonly double[] _y1 = new double[8];
	private readonly double[] _y2 = new double[8];

	public void SetCoefficients(double b0, double b1, double b2, double a1, double a2)
	{
		B0 = b0; B1 = b1; B2 = b2; A1 = a1; A2 = a2;
	}

	public double Process(double input, int channel)
	{
		if (channel >= _x1.Length) return input;
		double output = B0 * input + B1 * _x1[channel] + B2 * _x2[channel] - A1 * _y1[channel] - A2 * _y2[channel];
		_x2[channel] = _x1[channel];
		_x1[channel] = input;
		_y2[channel] = _y1[channel];
		_y1[channel] = double.IsFinite(output) ? output : 0.0;
		return _y1[channel];
	}

	public void Reset()
	{
		Array.Clear(_x1, 0, _x1.Length);
		Array.Clear(_x2, 0, _x2.Length);
		Array.Clear(_y1, 0, _y1.Length);
		Array.Clear(_y2, 0, _y2.Length);
	}
}

/// <summary>
/// FurinaPlayer 三频段动态压缩器处理器 (Multiband Compressor)
/// 采用 4 阶 Linkwitz-Riley (24 dB/oct) 分频网络将音频精确无失真分割为低频、中频与高频三个频段，
/// 并为各频段配置独立的高动态 Soft-Knee 压缩器与增益衰减指示。
/// </summary>
public sealed class FurinaPlayerMultibandProcessor
{
	private const float MinDetectorLevel = 1.0e-9f;

	private double _sampleRate = 44100.0;
	private bool _enabled = false;
	private float _lowCrossoverHz = 150.0f;
	private float _highCrossoverHz = 2500.0f;
	private float _mix = 1.0f;
	private float _stereoLink = 1.0f;

	private readonly object _lock = new();

	// 滤波器组：低频 LP_LR4, 全通补偿 AP_LR4; 中高分频 HP_LR4; 中频 LP_LR4; 高频 HP_LR4
	private readonly Lr4BiquadStage _lpLow1 = new();
	private readonly Lr4BiquadStage _lpLow2 = new();
	private readonly Lr4BiquadStage _apHigh1 = new();
	private readonly Lr4BiquadStage _apHigh2 = new();

	private readonly Lr4BiquadStage _hpLow1 = new();
	private readonly Lr4BiquadStage _hpLow2 = new();

	private readonly Lr4BiquadStage _lpMid1 = new();
	private readonly Lr4BiquadStage _lpMid2 = new();
	private readonly Lr4BiquadStage _hpHigh1 = new();
	private readonly Lr4BiquadStage _hpHigh2 = new();

	public FurinaPlayerMultibandBand[] Bands { get; } = new[]
	{
		new FurinaPlayerMultibandBand(), // 0: Low
		new FurinaPlayerMultibandBand(), // 1: Mid
		new FurinaPlayerMultibandBand()  // 2: High
	};

	public bool Enabled
	{
		get => _enabled;
		set => _enabled = value;
	}

	public float LowCrossoverHz
	{
		get => _lowCrossoverHz;
		set
		{
			float clamped = Math.Clamp(value, 40.0f, 500.0f);
			if (Math.Abs(_lowCrossoverHz - clamped) > 0.01f)
			{
				_lowCrossoverHz = clamped;
				UpdateFilterCoefficients();
			}
		}
	}

	public float HighCrossoverHz
	{
		get => _highCrossoverHz;
		set
		{
			float clamped = Math.Clamp(value, 800.0f, 8000.0f);
			if (Math.Abs(_highCrossoverHz - clamped) > 0.01f)
			{
				_highCrossoverHz = clamped;
				UpdateFilterCoefficients();
			}
		}
	}

	public float Mix
	{
		get => _mix;
		set => _mix = Math.Clamp(value, 0.0f, 1.0f);
	}

	public float StereoLink
	{
		get => _stereoLink;
		set => _stereoLink = Math.Clamp(value, 0.0f, 1.0f);
	}

	public float GainReductionDb => Math.Max(Bands[0].GainReductionDb, Math.Max(Bands[1].GainReductionDb, Bands[2].GainReductionDb));

	public FurinaPlayerMultibandProcessor(double sampleRate = 44100.0)
	{
		_sampleRate = Math.Max(1.0, sampleRate);
		UpdateFilterCoefficients();
	}

	public void Prepare(double sampleRate)
	{
		lock (_lock)
		{
			_sampleRate = Math.Max(1.0, sampleRate);
			UpdateFilterCoefficients();
			Reset();
		}
	}

	public void Reset()
	{
		lock (_lock)
		{
			_lpLow1.Reset();
			_lpLow2.Reset();
			_apHigh1.Reset();
			_apHigh2.Reset();
			_hpLow1.Reset();
			_hpLow2.Reset();
			_lpMid1.Reset();
			_lpMid2.Reset();
			_hpHigh1.Reset();
			_hpHigh2.Reset();

			for (int i = 0; i < Bands.Length; i++)
			{
				Bands[i].Reset();
			}
		}
	}

	private void UpdateFilterCoefficients()
	{
		double fs = _sampleRate;
		double fLow = Math.Clamp(_lowCrossoverHz, 20.0, fs * 0.45);
		double fHigh = Math.Clamp(_highCrossoverHz, fLow * 1.5, fs * 0.48);

		// Butterworth 2nd order LowPass coefficients at fLow
		CalcButterworthLp(fLow, fs, out double lpLowB0, out double lpLowB1, out double lpLowB2, out double lpLowA1, out double lpLowA2);
		_lpLow1.SetCoefficients(lpLowB0, lpLowB1, lpLowB2, lpLowA1, lpLowA2);
		_lpLow2.SetCoefficients(lpLowB0, lpLowB1, lpLowB2, lpLowA1, lpLowA2);

		// Butterworth 2nd order HighPass coefficients at fLow
		CalcButterworthHp(fLow, fs, out double hpLowB0, out double hpLowB1, out double hpLowB2, out double hpLowA1, out double hpLowA2);
		_hpLow1.SetCoefficients(hpLowB0, hpLowB1, hpLowB2, hpLowA1, hpLowA2);
		_hpLow2.SetCoefficients(hpLowB0, hpLowB1, hpLowB2, hpLowA1, hpLowA2);

		// Butterworth 2nd order LowPass coefficients at fHigh (Mid band)
		CalcButterworthLp(fHigh, fs, out double lpHighB0, out double lpHighB1, out double lpHighB2, out double lpHighA1, out double lpHighA2);
		_lpMid1.SetCoefficients(lpHighB0, lpHighB1, lpHighB2, lpHighA1, lpHighA2);
		_lpMid2.SetCoefficients(lpHighB0, lpHighB1, lpHighB2, lpHighA1, lpHighA2);

		// Butterworth 2nd order HighPass coefficients at fHigh (High band)
		CalcButterworthHp(fHigh, fs, out double hpHighB0, out double hpHighB1, out double hpHighB2, out double hpHighA1, out double hpHighA2);
		_hpHigh1.SetCoefficients(hpHighB0, hpHighB1, hpHighB2, hpHighA1, hpHighA2);
		_hpHigh2.SetCoefficients(hpHighB0, hpHighB1, hpHighB2, hpHighA1, hpHighA2);

		// 2nd order AllPass coefficients at fHigh (Phase align Low band with Mid+High)
		CalcButterworthAp(fHigh, fs, out double apHighB0, out double apHighB1, out double apHighB2, out double apHighA1, out double apHighA2);
		_apHigh1.SetCoefficients(apHighB0, apHighB1, apHighB2, apHighA1, apHighA2);
		_apHigh2.SetCoefficients(apHighB0, apHighB1, apHighB2, apHighA1, apHighA2);
	}

	private static void CalcButterworthLp(double f, double fs, out double b0, out double b1, out double b2, out double a1, out double a2)
	{
		double w = 2.0 * Math.PI * f / fs;
		double alpha = Math.Sin(w) / Math.Sqrt(2.0);
		double a0 = 1.0 + alpha;
		double cosW = Math.Cos(w);
		b0 = (1.0 - cosW) * 0.5 / a0;
		b1 = (1.0 - cosW) / a0;
		b2 = (1.0 - cosW) * 0.5 / a0;
		a1 = -2.0 * cosW / a0;
		a2 = (1.0 - alpha) / a0;
	}

	private static void CalcButterworthHp(double f, double fs, out double b0, out double b1, out double b2, out double a1, out double a2)
	{
		double w = 2.0 * Math.PI * f / fs;
		double alpha = Math.Sin(w) / Math.Sqrt(2.0);
		double a0 = 1.0 + alpha;
		double cosW = Math.Cos(w);
		b0 = (1.0 + cosW) * 0.5 / a0;
		b1 = -(1.0 + cosW) / a0;
		b2 = (1.0 + cosW) * 0.5 / a0;
		a1 = -2.0 * cosW / a0;
		a2 = (1.0 - alpha) / a0;
	}

	private static void CalcButterworthAp(double f, double fs, out double b0, out double b1, out double b2, out double a1, out double a2)
	{
		double w = 2.0 * Math.PI * f / fs;
		double alpha = Math.Sin(w) / Math.Sqrt(2.0);
		double a0 = 1.0 + alpha;
		double cosW = Math.Cos(w);
		b0 = (1.0 - alpha) / a0;
		b1 = -2.0 * cosW / a0;
		b2 = (1.0 + alpha) / a0;
		a1 = -2.0 * cosW / a0;
		a2 = (1.0 - alpha) / a0;
	}

	private static float SmoothingCoefficient(float ms, double sampleRate)
	{
		double samples = Math.Max(1.0, sampleRate * ms / 1000.0);
		return (float)Math.Exp(-1.0 / samples);
	}

	private static float GainToDb(float gain)
	{
		return (float)(20.0 * Math.Log10(Math.Max(gain, MinDetectorLevel)));
	}

	private static float DbToGain(float db)
	{
		return (float)Math.Pow(10.0, db / 20.0);
	}

	private static float ComputeReductionDb(float inputDb, float thresholdDb, float ratio, float kneeDb)
	{
		float overDb = inputDb - thresholdDb;
		float slope = 1.0f - 1.0f / ratio;

		if (kneeDb <= 0.001f)
		{
			return Math.Max(0.0f, overDb * slope);
		}

		float halfKnee = kneeDb * 0.5f;
		if (overDb <= -halfKnee)
		{
			return 0.0f;
		}

		if (overDb >= halfKnee)
		{
			return overDb * slope;
		}

		float kneePos = overDb + halfKnee;
		return slope * kneePos * kneePos / (2.0f * kneeDb);
	}

	public void ProcessInterleaved(float[] buffer, int offset, int count, int channels)
	{
		if (!_enabled || count <= 0 || channels <= 0)
		{
			for (int i = 0; i < Bands.Length; i++)
			{
				Bands[i].GainReductionDb = 0.0f;
			}
			return;
		}

		lock (_lock)
		{
			int frames = count / channels;
			float[] peakReductionDb = new float[3];

			// Precompute band attack & release coefficients
			float[] attackCoefs = new float[3];
			float[] releaseCoefs = new float[3];
			float[] makeupGains = new float[3];

			for (int b = 0; b < 3; b++)
			{
				var band = Bands[b];
				attackCoefs[b] = SmoothingCoefficient(band.AttackMs, _sampleRate);
				releaseCoefs[b] = SmoothingCoefficient(band.ReleaseMs, _sampleRate);
				makeupGains[b] = DbToGain(band.MakeupDb);
			}

			// Processing sample by sample
			for (int frame = 0; frame < frames; frame++)
			{
				int frameOffset = offset + frame * channels;

				// Per-band split samples for each channel
				// bandSamples[band, channel]
				double[] lowSamples = new double[channels];
				double[] midSamples = new double[channels];
				double[] highSamples = new double[channels];

				float maxLowAbs = 0f;
				float maxMidAbs = 0f;
				float maxHighAbs = 0f;

				for (int ch = 0; ch < channels; ch++)
				{
					double inSample = buffer[frameOffset + ch];

					// 1. Low pass split at fLow, then allpass aligned with fHigh
					double l1 = _lpLow1.Process(inSample, ch);
					double l2 = _lpLow2.Process(l1, ch);
					double ap1 = _apHigh1.Process(l2, ch);
					double low = _apHigh2.Process(ap1, ch);
					lowSamples[ch] = low;

					// 2. High pass split at fLow
					double hp1 = _hpLow1.Process(inSample, ch);
					double hp2 = _hpLow2.Process(hp1, ch);

					// 3. Mid band: Low pass hp2 at fHigh
					double m1 = _lpMid1.Process(hp2, ch);
					double mid = _lpMid2.Process(m1, ch);
					midSamples[ch] = mid;

					// 4. High band: High pass hp2 at fHigh
					double h1 = _hpHigh1.Process(hp2, ch);
					double high = _hpHigh2.Process(h1, ch);
					highSamples[ch] = high;

					float absLow = (float)Math.Abs(low);
					if (absLow > maxLowAbs) maxLowAbs = absLow;

					float absMid = (float)Math.Abs(mid);
					if (absMid > maxMidAbs) maxMidAbs = absMid;

					float absHigh = (float)Math.Abs(high);
					if (absHigh > maxHighAbs) maxHighAbs = absHigh;
				}

				// Dynamics envelope computation for the 3 bands
				float[] bandDetectors = new float[3] { maxLowAbs, maxMidAbs, maxHighAbs };
				float[] bandWets = new float[3];

				for (int b = 0; b < 3; b++)
				{
					var band = Bands[b];
					if (!band.Enabled)
					{
						bandWets[b] = 1.0f;
						continue;
					}

					float inDb = GainToDb(bandDetectors[b]);
					float redDb = ComputeReductionDb(inDb, band.ThresholdDb, band.Ratio, band.KneeDb);
					float targetGain = DbToGain(-redDb);

					float coef = targetGain < band.SmoothedGain ? attackCoefs[b] : releaseCoefs[b];
					band.SmoothedGain = coef * band.SmoothedGain + (1.0f - coef) * targetGain;

					float currentRedDb = -GainToDb(band.SmoothedGain);
					if (currentRedDb > peakReductionDb[b]) peakReductionDb[b] = currentRedDb;

					bandWets[b] = band.SmoothedGain * makeupGains[b];
				}

				// Recombination with master wet/dry mix
				float mix = _mix;
				float dryRatio = 1.0f - mix;

				for (int ch = 0; ch < channels; ch++)
				{
					double processed = lowSamples[ch] * bandWets[0] + midSamples[ch] * bandWets[1] + highSamples[ch] * bandWets[2];
					float dry = buffer[frameOffset + ch];
					float wet = (float)processed;
					float outSample = dry * dryRatio + wet * mix;

					buffer[frameOffset + ch] = float.IsFinite(outSample) ? outSample : 0f;
				}
			}

			for (int b = 0; b < 3; b++)
			{
				Bands[b].GainReductionDb = peakReductionDb[b];
			}
		}
	}

	public void ApplyPreset(string presetId)
	{
		lock (_lock)
		{
			_enabled = true;
			switch (presetId?.ToLowerInvariant())
			{
				case "gentle":
				case "轻柔均衡":
					// Low
					Bands[0].Enabled = true;
					Bands[0].ThresholdDb = -24.0f;
					Bands[0].Ratio = 1.6f;
					Bands[0].AttackMs = 25.0f;
					Bands[0].ReleaseMs = 180.0f;
					// Mid
					Bands[1].Enabled = true;
					Bands[1].ThresholdDb = -22.0f;
					Bands[1].Ratio = 1.6f;
					Bands[1].AttackMs = 15.0f;
					Bands[1].ReleaseMs = 130.0f;
					// High
					Bands[2].Enabled = true;
					Bands[2].ThresholdDb = -28.0f;
					Bands[2].Ratio = 1.6f;
					Bands[2].AttackMs = 5.0f;
					Bands[2].ReleaseMs = 90.0f;
					break;

				case "bass":
				case "低频收紧":
					// Low
					Bands[0].Enabled = true;
					Bands[0].ThresholdDb = -30.0f;
					Bands[0].Ratio = 4.0f;
					Bands[0].AttackMs = 12.0f;
					Bands[0].ReleaseMs = 140.0f;
					// Mid
					Bands[1].Enabled = true;
					Bands[1].ThresholdDb = -18.0f;
					Bands[1].Ratio = 1.0f;
					Bands[1].AttackMs = 10.0f;
					Bands[1].ReleaseMs = 120.0f;
					// High
					Bands[2].Enabled = true;
					Bands[2].ThresholdDb = -18.0f;
					Bands[2].Ratio = 1.0f;
					Bands[2].AttackMs = 10.0f;
					Bands[2].ReleaseMs = 120.0f;
					break;

				case "strong":
				case "明显压缩":
					// Low
					Bands[0].Enabled = true;
					Bands[0].ThresholdDb = -32.0f;
					Bands[0].Ratio = 4.0f;
					Bands[0].AttackMs = 10.0f;
					Bands[0].ReleaseMs = 160.0f;
					// Mid
					Bands[1].Enabled = true;
					Bands[1].ThresholdDb = -28.0f;
					Bands[1].Ratio = 4.0f;
					Bands[1].AttackMs = 5.0f;
					Bands[1].ReleaseMs = 100.0f;
					// High
					Bands[2].Enabled = true;
					Bands[2].ThresholdDb = -34.0f;
					Bands[2].Ratio = 4.0f;
					Bands[2].AttackMs = 2.0f;
					Bands[2].ReleaseMs = 70.0f;
					break;

				case "default":
				case "恢复默认":
				default:
					_enabled = false;
					_lowCrossoverHz = 150.0f;
					_highCrossoverHz = 2500.0f;
					_mix = 1.0f;
					_stereoLink = 1.0f;
					for (int i = 0; i < 3; i++)
					{
						Bands[i].Enabled = true;
						Bands[i].ThresholdDb = -18.0f;
						Bands[i].Ratio = 2.0f;
						Bands[i].AttackMs = 10.0f;
						Bands[i].ReleaseMs = 120.0f;
						Bands[i].KneeDb = 6.0f;
						Bands[i].MakeupDb = 0.0f;
						Bands[i].GainReductionDb = 0.0f;
						Bands[i].SmoothedGain = 1.0f;
					}
					UpdateFilterCoefficients();
					break;
			}
		}
	}
}
