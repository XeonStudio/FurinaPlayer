using System;

namespace SonicWave.Audio.Dsp;

public enum CompressorDetectorMode
{
	Peak,
	Rms
}

/// <summary>
/// FurinaPlayer 专业立体声母带动态压缩器底层引擎。
/// 提供高精度 RMS / 峰值检测、Soft-Knee 软拐点计算、精确启动与释放时间常数、
/// 侧链高通滤波、立体声联动以及干湿并行混合（Parallel Compression）。
/// </summary>
public class FurinaPlayerCompressorProcessor
{
	public const float MinDetectorLevel = 1.0e-9f;
	public const float SilenceDb = -96.0f;

	private double _sampleRate = 44100.0;
	private bool _enabled = false;
	private float _thresholdDb = -18.0f;   // -72 to 0 dB
	private float _ratio = 4.0f;           // 1 to 40
	private float _attackMs = 10.0f;       // 0.1 to 500 ms
	private float _releaseMs = 120.0f;     // 5 to 5000 ms
	private float _kneeDb = 6.0f;          // 0 to 24 dB
	private float _makeupDb = 0.0f;        // -24 to 24 dB
	private float _mix = 1.0f;             // 0 to 1 (100% wet)

	private CompressorDetectorMode _detectorMode = CompressorDetectorMode.Peak;
	private bool _sidechainHighpassEnabled = false;
	private float _sidechainHighpassHz = 80.0f; // 20 to 400 Hz
	private bool _autoRelease = false;
	private float _rangeDb = 60.0f;        // 0 to 72 dB
	private float _stereoLink = 1.0f;      // 0 to 1 (100% linked)

	// Runtime state
	private float _smoothedGain = 1.0f;
	private float _gainReductionDb = 0.0f;

	// Telemetry meters (in dB)
	private float _inputPeakDbL = SilenceDb;
	private float _inputPeakDbR = SilenceDb;
	private float _inputRmsDbL = SilenceDb;
	private float _inputRmsDbR = SilenceDb;

	private float _outputPeakDbL = SilenceDb;
	private float _outputPeakDbR = SilenceDb;
	private float _outputRmsDbL = SilenceDb;
	private float _outputRmsDbR = SilenceDb;

	// Sidechain HPF Biquad coefficients & states (Stereo)
	private float _scB0 = 1.0f, _scB1 = 0.0f, _scB2 = 0.0f;
	private float _scA1 = 0.0f, _scA2 = 0.0f;
	private readonly float[] _scX1 = new float[2];
	private readonly float[] _scX2 = new float[2];
	private readonly float[] _scY1 = new float[2];
	private readonly float[] _scY2 = new float[2];

	// RMS detector states
	private readonly float[] _rmsState = new float[2];
	private float _rmsAlpha = 0.99f;

	public bool Enabled { get => _enabled; set => _enabled = value; }
	public float ThresholdDb { get => _thresholdDb; set => _thresholdDb = Math.Clamp(value, -72.0f, 0.0f); }
	public float Ratio { get => _ratio; set => _ratio = Math.Clamp(value, 1.0f, 40.0f); }
	public float AttackMs { get => _attackMs; set => _attackMs = Math.Clamp(value, 0.1f, 500.0f); }
	public float ReleaseMs { get => _releaseMs; set => _releaseMs = Math.Clamp(value, 5.0f, 5000.0f); }
	public float KneeDb { get => _kneeDb; set => _kneeDb = Math.Clamp(value, 0.0f, 24.0f); }
	public float MakeupDb { get => _makeupDb; set => _makeupDb = Math.Clamp(value, -24.0f, 24.0f); }
	public float Mix { get => _mix; set => _mix = Math.Clamp(value, 0.0f, 1.0f); }

	public CompressorDetectorMode DetectorMode { get => _detectorMode; set => _detectorMode = value; }
	public bool SidechainHighpassEnabled { get => _sidechainHighpassEnabled; set => _sidechainHighpassEnabled = value; }
	public float SidechainHighpassHz
	{
		get => _sidechainHighpassHz;
		set
		{
			_sidechainHighpassHz = Math.Clamp(value, 20.0f, 400.0f);
			UpdateSidechainCoefficients();
		}
	}
	public bool AutoRelease { get => _autoRelease; set => _autoRelease = value; }
	public float RangeDb { get => _rangeDb; set => _rangeDb = Math.Clamp(value, 0.0f, 72.0f); }
	public float StereoLink { get => _stereoLink; set => _stereoLink = Math.Clamp(value, 0.0f, 1.0f); }

	// Telemetry properties
	public float GainReductionDb => _gainReductionDb;
	public float InputPeakDbL => _inputPeakDbL;
	public float InputPeakDbR => _inputPeakDbR;
	public float InputPeakDb => Math.Max(_inputPeakDbL, _inputPeakDbR);
	public float InputRmsDbL => _inputRmsDbL;
	public float InputRmsDbR => _inputRmsDbR;
	public float InputRmsDb => Math.Max(_inputRmsDbL, _inputRmsDbR);

	public float OutputPeakDbL => _outputPeakDbL;
	public float OutputPeakDbR => _outputPeakDbR;
	public float OutputPeakDb => Math.Max(_outputPeakDbL, _outputPeakDbR);
	public float OutputRmsDbL => _outputRmsDbL;
	public float OutputRmsDbR => _outputRmsDbR;
	public float OutputRmsDb => Math.Max(_outputRmsDbL, _outputRmsDbR);

	public float OutputHeadroomDb => Math.Clamp(-OutputPeakDb, 0.0f, 96.0f);
	public bool ClippingRisk => OutputPeakDb >= -0.05f;

	public FurinaPlayerCompressorProcessor()
	{
		UpdateSidechainCoefficients();
	}

	public void Prepare(double sampleRate)
	{
		_sampleRate = Math.Max(1.0, sampleRate);
		_rmsAlpha = (float)Math.Exp(-1.0 / (_sampleRate * 0.030)); // 30ms window
		UpdateSidechainCoefficients();
		Reset();
	}

	public void Reset()
	{
		_smoothedGain = 1.0f;
		_gainReductionDb = 0.0f;
		_inputPeakDbL = SilenceDb;
		_inputPeakDbR = SilenceDb;
		_inputRmsDbL = SilenceDb;
		_inputRmsDbR = SilenceDb;
		_outputPeakDbL = SilenceDb;
		_outputPeakDbR = SilenceDb;
		_outputRmsDbL = SilenceDb;
		_outputRmsDbR = SilenceDb;

		Array.Clear(_scX1, 0, _scX1.Length);
		Array.Clear(_scX2, 0, _scX2.Length);
		Array.Clear(_scY1, 0, _scY1.Length);
		Array.Clear(_scY2, 0, _scY2.Length);
		Array.Clear(_rmsState, 0, _rmsState.Length);
	}

	private void UpdateSidechainCoefficients()
	{
		if (_sampleRate <= 0.0) return;
		float f0 = Math.Clamp(_sidechainHighpassHz, 20.0f, (float)(_sampleRate * 0.45));
		float w0 = 2.0f * (float)Math.PI * f0 / (float)_sampleRate;
		float cosw0 = (float)Math.Cos(w0);
		float sinw0 = (float)Math.Sin(w0);
		float alpha = sinw0 / (2.0f * 0.7071f);

		float b0 = (1.0f + cosw0) * 0.5f;
		float b1 = -(1.0f + cosw0);
		float b2 = (1.0f + cosw0) * 0.5f;
		float a0 = 1.0f + alpha;
		float a1 = -2.0f * cosw0;
		float a2 = 1.0f - alpha;

		_scB0 = b0 / a0;
		_scB1 = b1 / a0;
		_scB2 = b2 / a0;
		_scA1 = a1 / a0;
		_scA2 = a2 / a0;
	}

	public static float SmoothingCoefficient(float ms, double sampleRate)
	{
		double samples = Math.Max(1.0, sampleRate * ms / 1000.0);
		return (float)Math.Exp(-1.0 / samples);
	}

	public static float GainToDb(float gain)
	{
		return (float)(20.0 * Math.Log10(Math.Max(gain, MinDetectorLevel)));
	}

	public static float DbToGain(float db)
	{
		return (float)Math.Pow(10.0, db / 20.0);
	}

	/// <summary>
	/// 计算给定输入电平在设定阈值、压缩比、拐点和范围下的增益衰减分贝量（正值，如 6.0 dB）。
	/// </summary>
	public static float ComputeReductionDb(float inputDb, float thresholdDb, float ratio, float kneeDb, float rangeDb = 60.0f)
	{
		float overDb = inputDb - thresholdDb;
		float slope = 1.0f - 1.0f / Math.Max(1.0f, ratio);
		float reductionDb;

		if (kneeDb <= 0.001f)
		{
			reductionDb = Math.Max(0.0f, overDb * slope);
		}
		else
		{
			float halfKnee = kneeDb * 0.5f;
			if (overDb <= -halfKnee)
			{
				reductionDb = 0.0f;
			}
			else if (overDb >= halfKnee)
			{
				reductionDb = overDb * slope;
			}
			else
			{
				float kneePos = overDb + halfKnee;
				reductionDb = slope * kneePos * kneePos / (2.0f * kneeDb);
			}
		}

		if (rangeDb > 0.0f)
		{
			reductionDb = Math.Min(reductionDb, rangeDb);
		}

		return Math.Max(0.0f, reductionDb);
	}

	/// <summary>
	/// 计算传输特性曲线对应输出分贝值（含补偿增益与并行混合）。
	/// </summary>
	public static float ComputeMixedOutputDb(float inputDb, float thresholdDb, float ratio, float kneeDb, float makeupDb, float mix, float rangeDb = 60.0f)
	{
		float reductionDb = ComputeReductionDb(inputDb, thresholdDb, ratio, kneeDb, rangeDb);
		double inputGain = Math.Pow(10.0, inputDb / 20.0);
		double wetGain = inputGain * Math.Pow(10.0, (makeupDb - reductionDb) / 20.0);
		double mixedGain = inputGain * (1.0 - mix) + wetGain * mix;
		return (float)(20.0 * Math.Log10(Math.Max(1.0e-9, mixedGain)));
	}

	public void ProcessInterleaved(float[] buffer, int offset, int count, int channels)
	{
		if (count <= 0 || channels <= 0)
		{
			_gainReductionDb = 0.0f;
			return;
		}

		if (!_enabled)
		{
			_gainReductionDb = 0.0f;
			// Measure passive telemetry even when bypassed
			MeasureTelemetryPassive(buffer, offset, count, channels);
			return;
		}

		float attackCoef = SmoothingCoefficient(_attackMs, _sampleRate);
		float releaseMs = _releaseMs;
		float releaseCoef = SmoothingCoefficient(releaseMs, _sampleRate);
		float makeupGain = DbToGain(_makeupDb);
		int frames = count / channels;
		float peakReductionDb = 0.0f;

		float blockInPeakL = 0f, blockInPeakR = 0f;
		float blockInSumSqL = 0f, blockInSumSqR = 0f;
		float blockOutPeakL = 0f, blockOutPeakR = 0f;
		float blockOutSumSqL = 0f, blockOutSumSqR = 0f;

		for (int frame = 0; frame < frames; frame++)
		{
			int frameOffset = offset + frame * channels;
			float dryL = buffer[frameOffset];
			float dryR = channels > 1 ? buffer[frameOffset + 1] : dryL;

			// Telemetry sum
			blockInPeakL = Math.Max(blockInPeakL, Math.Abs(dryL));
			blockInPeakR = Math.Max(blockInPeakR, Math.Abs(dryR));
			blockInSumSqL += dryL * dryL;
			blockInSumSqR += dryR * dryR;

			// 1. Sidechain signal filtering (if enabled)
			float scL = dryL;
			float scR = dryR;
			if (_sidechainHighpassEnabled)
			{
				scL = ApplySidechainFilter(scL, 0);
				if (channels > 1) scR = ApplySidechainFilter(scR, 1);
			}

			// 2. Detector signal (Peak or RMS)
			float detL, detR;
			if (_detectorMode == CompressorDetectorMode.Rms)
			{
				_rmsState[0] = _rmsAlpha * _rmsState[0] + (1.0f - _rmsAlpha) * (scL * scL);
				detL = (float)Math.Sqrt(Math.Max(_rmsState[0], 1.0e-12f));

				if (channels > 1)
				{
					_rmsState[1] = _rmsAlpha * _rmsState[1] + (1.0f - _rmsAlpha) * (scR * scR);
					detR = (float)Math.Sqrt(Math.Max(_rmsState[1], 1.0e-12f));
				}
				else
				{
					detR = detL;
				}
			}
			else
			{
				detL = Math.Abs(scL);
				detR = channels > 1 ? Math.Abs(scR) : detL;
			}

			// 3. Stereo Linking
			float maxDet = Math.Max(detL, detR);
			float effectiveDet = detL * (1.0f - _stereoLink) + maxDet * _stereoLink;

			// 4. Characteristic curve reduction
			float inputDb = GainToDb(effectiveDet);
			float reductionDb = ComputeReductionDb(inputDb, _thresholdDb, _ratio, _kneeDb, _rangeDb);
			float targetGain = DbToGain(-reductionDb);

			// 5. Ballistics (Attack / Release)
			float effReleaseCoef = releaseCoef;
			if (_autoRelease)
			{
				// Program-dependent release: longer release on heavier gain reduction
				float autoRelMs = Math.Clamp(_releaseMs * (1.0f + 1.5f * (reductionDb / 20.0f)), 5.0f, 5000.0f);
				effReleaseCoef = SmoothingCoefficient(autoRelMs, _sampleRate);
			}

			float coef = targetGain < _smoothedGain ? attackCoef : effReleaseCoef;
			_smoothedGain = coef * _smoothedGain + (1.0f - coef) * targetGain;

			float currentReduction = -GainToDb(_smoothedGain);
			if (currentReduction > peakReductionDb) peakReductionDb = currentReduction;

			// 6. Makeup Gain & Parallel Compression Mix
			float wetGain = _smoothedGain * makeupGain;
			float wetFactor = _mix;
			float dryFactor = 1.0f - _mix;

			for (int ch = 0; ch < channels; ch++)
			{
				float dry = buffer[frameOffset + ch];
				float wet = dry * wetGain;
				float outSample = dry * dryFactor + wet * wetFactor;
				buffer[frameOffset + ch] = outSample;

				if (ch == 0)
				{
					blockOutPeakL = Math.Max(blockOutPeakL, Math.Abs(outSample));
					blockOutSumSqL += outSample * outSample;
				}
				else if (ch == 1)
				{
					blockOutPeakR = Math.Max(blockOutPeakR, Math.Abs(outSample));
					blockOutSumSqR += outSample * outSample;
				}
			}
		}

		_gainReductionDb = peakReductionDb;

		// Update telemetry facts with smooth decay
		float invFrames = 1.0f / Math.Max(1, frames);
		UpdateTelemetryDecay(ref _inputPeakDbL, GainToDb(blockInPeakL));
		UpdateTelemetryDecay(ref _inputPeakDbR, channels > 1 ? GainToDb(blockInPeakR) : _inputPeakDbL);
		UpdateTelemetryDecay(ref _inputRmsDbL, GainToDb((float)Math.Sqrt(blockInSumSqL * invFrames)));
		UpdateTelemetryDecay(ref _inputRmsDbR, channels > 1 ? GainToDb((float)Math.Sqrt(blockInSumSqR * invFrames)) : _inputRmsDbL);

		UpdateTelemetryDecay(ref _outputPeakDbL, GainToDb(blockOutPeakL));
		UpdateTelemetryDecay(ref _outputPeakDbR, channels > 1 ? GainToDb(blockOutPeakR) : _outputPeakDbL);
		UpdateTelemetryDecay(ref _outputRmsDbL, GainToDb((float)Math.Sqrt(blockOutSumSqL * invFrames)));
		UpdateTelemetryDecay(ref _outputRmsDbR, channels > 1 ? GainToDb((float)Math.Sqrt(blockOutSumSqR * invFrames)) : _outputRmsDbL);
	}

	private float ApplySidechainFilter(float x, int ch)
	{
		float y = _scB0 * x + _scB1 * _scX1[ch] + _scB2 * _scX2[ch] - _scA1 * _scY1[ch] - _scA2 * _scY2[ch];
		_scX2[ch] = _scX1[ch];
		_scX1[ch] = x;
		_scY2[ch] = _scY1[ch];
		_scY1[ch] = y;
		return y;
	}

	private void MeasureTelemetryPassive(float[] buffer, int offset, int count, int channels)
	{
		int frames = count / channels;
		float pL = 0f, pR = 0f, sqL = 0f, sqR = 0f;
		for (int f = 0; f < frames; f++)
		{
			int idx = offset + f * channels;
			float l = Math.Abs(buffer[idx]);
			float r = channels > 1 ? Math.Abs(buffer[idx + 1]) : l;
			if (l > pL) pL = l;
			if (r > pR) pR = r;
			sqL += l * l;
			sqR += r * r;
		}
		float invF = 1.0f / Math.Max(1, frames);
		float inDbL = GainToDb(pL);
		float inDbR = GainToDb(pR);
		float rmsL = GainToDb((float)Math.Sqrt(sqL * invF));
		float rmsR = GainToDb((float)Math.Sqrt(sqR * invF));

		UpdateTelemetryDecay(ref _inputPeakDbL, inDbL);
		UpdateTelemetryDecay(ref _inputPeakDbR, inDbR);
		UpdateTelemetryDecay(ref _inputRmsDbL, rmsL);
		UpdateTelemetryDecay(ref _inputRmsDbR, rmsR);

		_outputPeakDbL = _inputPeakDbL;
		_outputPeakDbR = _inputPeakDbR;
		_outputRmsDbL = _inputRmsDbL;
		_outputRmsDbR = _inputRmsDbR;
	}

	private static void UpdateTelemetryDecay(ref float currentDb, float targetDb)
	{
		if (targetDb >= currentDb)
		{
			currentDb = targetDb; // instant peak attack
		}
		else
		{
			currentDb = currentDb * 0.85f + targetDb * 0.15f; // fast visual meter release
		}
		if (currentDb < SilenceDb) currentDb = SilenceDb;
	}
}
