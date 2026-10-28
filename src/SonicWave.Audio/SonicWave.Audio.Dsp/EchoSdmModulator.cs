using System;

namespace SonicWave.Audio.Dsp;

public enum SdmOrder
{
	Order3_Safe = 3,
	Order5_HiFi = 5,
	Order7_Reference = 7
}

public enum SdmTargetRate
{
	Dsd64 = 64,   // 2.8224 MHz (64x 44.1k)
	Dsd128 = 128, // 5.6448 MHz (128x 44.1k)
	Dsd256 = 256  // 11.2896 MHz (256x 44.1k)
}

public sealed class EchoSdmModulator
{
	private bool _enabled = false;
	private SdmOrder _order = SdmOrder.Order5_HiFi;
	private SdmTargetRate _targetRate = SdmTargetRate.Dsd64;
	private double[] _feedbackCoeffs = Array.Empty<double>();

	// Integrator states per channel [channel, order]
	private double[][] _integrators = new double[2][];
	private double[][] _errors = new double[2][];
	private uint _rngState = 0x6d2b79f5u;

	public bool Enabled
	{
		get => _enabled;
		set => _enabled = value;
	}

	public SdmOrder Order
	{
		get => _order;
		set { _order = value; RecomputeCoefficients(); }
	}

	public SdmTargetRate TargetRate
	{
		get => _targetRate;
		set => _targetRate = value;
	}

	public EchoSdmModulator()
	{
		RecomputeCoefficients();
		Reset();
	}

	private static double Binomial(int n, int k)
	{
		double val = 1.0;
		for (int i = 1; i <= k; i++)
		{
			val *= (double)(n - i + 1) / i;
		}
		return val;
	}

	private void RecomputeCoefficients()
	{
		int order = (int)_order;
		double peakGain = _order switch
		{
			SdmOrder.Order3_Safe => 1.45,
			SdmOrder.Order5_HiFi => 1.55,
			_ => 1.60
		};

		double poleRadius = 2.0 / Math.Pow(peakGain, 1.0 / order) - 1.0;
		_feedbackCoeffs = new double[order];

		for (int i = 0; i < order; i++)
		{
			int tap = i + 1;
			double denom = Binomial(order, tap) * Math.Pow(-poleRadius, tap);
			_feedbackCoeffs[i] = denom - Binomial(order, tap) * Math.Pow(-1.0, tap);
		}

		for (int ch = 0; ch < 2; ch++)
		{
			_integrators[ch] = new double[order];
			_errors[ch] = new double[order];
		}
	}

	public void Reset()
	{
		for (int ch = 0; ch < 2; ch++)
		{
			if (_integrators[ch] != null) Array.Clear(_integrators[ch], 0, _integrators[ch].Length);
			if (_errors[ch] != null) Array.Clear(_errors[ch], 0, _errors[ch].Length);
		}
	}

	private double NextDither()
	{
		_rngState = _rngState * 1664525u + 1013904223u;
		return ((_rngState >> 16) / 65536.0 - 0.5) * 0.0000001;
	}

	// Modulates 1 PCM sample into a 1-bit DSD bit (-1 or +1)
	public int ModulateSample(double inputSample, int channel)
	{
		int ch = Math.Min(channel, 1);
		int order = (int)_order;

		// Input limiter to avoid integrator runaway
		double clampedInput = Math.Clamp(inputSample, -0.95, 0.95);

		// Noise shaping feedback from past quantization errors
		double loopFilter = 0.0;
		for (int i = 0; i < order; i++)
		{
			loopFilter += _feedbackCoeffs[i] * _errors[ch][i];
		}

		double accum = clampedInput + loopFilter + NextDither();

		// 1-bit quantizer
		int bit = accum >= 0.0 ? 1 : -1;
		double quantError = bit - accum;

		// Shift error history
		for (int i = order - 1; i > 0; i--)
		{
			_errors[ch][i] = _errors[ch][i - 1];
		}
		_errors[ch][0] = quantError;

		return bit;
	}

	// Encodes PCM block to DoP (DSD over PCM) format frames
	// Returns 24-bit DoP packed words with standard 0x05 / 0xFA marker
	public uint[] EncodeToDop(float[] interleavedPcm, int count, int channels)
	{
		int frames = count / channels;
		int dopFrames = frames;
		uint[] dopOutput = new uint[dopFrames * channels];
		byte dopMarker = 0x05;

		for (int f = 0; f < frames; f++)
		{
			dopMarker = (byte)(dopMarker == 0x05 ? 0xFA : 0x05);
			for (int ch = 0; ch < channels; ch++)
			{
				float pcm = interleavedPcm[f * channels + ch];
				ushort dsd16Bits = 0;
				for (int b = 0; b < 16; b++)
				{
					int bit = ModulateSample(pcm, ch);
					if (bit > 0)
					{
						dsd16Bits |= (ushort)(1 << (15 - b));
					}
				}
				// Format: [Marker:8][DSD:16]
				uint dopSample = ((uint)dopMarker << 16) | dsd16Bits;
				dopOutput[f * channels + ch] = dopSample;
			}
		}

		return dopOutput;
	}
}
