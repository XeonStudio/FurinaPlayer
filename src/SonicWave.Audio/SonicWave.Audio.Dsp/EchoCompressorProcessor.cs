using System;

namespace SonicWave.Audio.Dsp;

public sealed class EchoCompressorProcessor
{
	private const float MinDetectorLevel = 1.0e-9f;

	private double _sampleRate = 44100.0;
	private bool _enabled = false;
	private float _thresholdDb = -18.0f; // -72 to 0
	private float _ratio = 4.0f; // 1 to 40
	private float _attackMs = 10.0f; // 0.1 to 500
	private float _releaseMs = 120.0f; // 5 to 5000
	private float _kneeDb = 6.0f; // 0 to 24
	private float _makeupDb = 0.0f; // -24 to 24
	private float _mix = 1.0f; // 0 to 1

	private float _smoothedGain = 1.0f;
	private float _gainReductionDb = 0.0f;

	public bool Enabled { get => _enabled; set => _enabled = value; }
	public float ThresholdDb { get => _thresholdDb; set => _thresholdDb = Math.Clamp(value, -72.0f, 0.0f); }
	public float Ratio { get => _ratio; set => _ratio = Math.Clamp(value, 1.0f, 40.0f); }
	public float AttackMs { get => _attackMs; set => _attackMs = Math.Clamp(value, 0.1f, 500.0f); }
	public float ReleaseMs { get => _releaseMs; set => _releaseMs = Math.Clamp(value, 5.0f, 5000.0f); }
	public float KneeDb { get => _kneeDb; set => _kneeDb = Math.Clamp(value, 0.0f, 24.0f); }
	public float MakeupDb { get => _makeupDb; set => _makeupDb = Math.Clamp(value, -24.0f, 24.0f); }
	public float Mix { get => _mix; set => _mix = Math.Clamp(value, 0.0f, 1.0f); }

	public float GainReductionDb => _gainReductionDb;

	public void Prepare(double sampleRate)
	{
		_sampleRate = Math.Max(1.0, sampleRate);
		Reset();
	}

	public void Reset()
	{
		_smoothedGain = 1.0f;
		_gainReductionDb = 0.0f;
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
			_gainReductionDb = 0.0f;
			return;
		}

		float attackCoef = SmoothingCoefficient(_attackMs, _sampleRate);
		float releaseCoef = SmoothingCoefficient(_releaseMs, _sampleRate);
		float makeupGain = DbToGain(_makeupDb);
		int frames = count / channels;
		float peakReductionDb = 0.0f;

		for (int frame = 0; frame < frames; frame++)
		{
			int frameOffset = offset + frame * channels;
			float detector = 0.0f;

			for (int ch = 0; ch < channels; ch++)
			{
				float abs = Math.Abs(buffer[frameOffset + ch]);
				if (abs > detector) detector = abs;
			}

			float inputDb = GainToDb(detector);
			float reductionDb = ComputeReductionDb(inputDb, _thresholdDb, _ratio, _kneeDb);
			float targetGain = DbToGain(-reductionDb);

			float coef = targetGain < _smoothedGain ? attackCoef : releaseCoef;
			_smoothedGain = coef * _smoothedGain + (1.0f - coef) * targetGain;

			peakReductionDb = Math.Max(peakReductionDb, -GainToDb(_smoothedGain));
			float wetGain = _smoothedGain * makeupGain;

			for (int ch = 0; ch < channels; ch++)
			{
				float dry = buffer[frameOffset + ch];
				buffer[frameOffset + ch] = dry * ((1.0f - _mix) + _mix * wetGain);
			}
		}

		_gainReductionDb = peakReductionDb;
	}
}
