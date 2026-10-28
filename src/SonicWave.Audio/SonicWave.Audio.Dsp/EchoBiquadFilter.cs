using System;

namespace SonicWave.Audio.Dsp;

public enum EchoFilterType
{
	Peaking,
	LowShelf,
	HighShelf,
	LowPass,
	HighPass,
	BandPass,
	Notch
}

public sealed class EchoBiquadFilter
{
	private double _b0 = 1.0;
	private double _b1 = 0.0;
	private double _b2 = 0.0;
	private double _a1 = 0.0;
	private double _a2 = 0.0;

	// Per-channel state: [channel, tap]
	private double[] _x1 = new double[8];
	private double[] _x2 = new double[8];
	private double[] _y1 = new double[8];
	private double[] _y2 = new double[8];

	public EchoFilterType FilterType { get; set; } = EchoFilterType.Peaking;
	public double FrequencyHz { get; set; } = 1000.0;
	public double GainDb { get; set; } = 0.0;
	public double Q { get; set; } = 0.7071;
	public bool Enabled { get; set; } = true;

	public void UpdateCoefficients(double sampleRate)
	{
		if (sampleRate <= 0.0) return;

		double freq = Math.Clamp(FrequencyHz, 10.0, sampleRate * 0.49);
		double q = Math.Max(0.01, Q);
		double gain = Math.Clamp(GainDb, -30.0, 30.0);

		double omega = 2.0 * Math.PI * freq / sampleRate;
		double sinOmega = Math.Sin(omega);
		double cosOmega = Math.Cos(omega);
		double alpha = sinOmega / (2.0 * q);
		double a = Math.Pow(10.0, gain / 40.0);

		double b0, b1, b2, a0, a1, a2;

		switch (FilterType)
		{
			case EchoFilterType.LowShelf:
			{
				double sqrtA2Alpha = 2.0 * Math.Sqrt(a) * alpha;
				b0 = a * ((a + 1.0) - (a - 1.0) * cosOmega + sqrtA2Alpha);
				b1 = 2.0 * a * ((a - 1.0) - (a + 1.0) * cosOmega);
				b2 = a * ((a + 1.0) - (a - 1.0) * cosOmega - sqrtA2Alpha);
				a0 = (a + 1.0) + (a - 1.0) * cosOmega + sqrtA2Alpha;
				a1 = -2.0 * ((a - 1.0) + (a + 1.0) * cosOmega);
				a2 = (a + 1.0) + (a - 1.0) * cosOmega - sqrtA2Alpha;
				break;
			}
			case EchoFilterType.HighShelf:
			{
				double sqrtA2Alpha = 2.0 * Math.Sqrt(a) * alpha;
				b0 = a * ((a + 1.0) + (a - 1.0) * cosOmega + sqrtA2Alpha);
				b1 = -2.0 * a * ((a - 1.0) + (a + 1.0) * cosOmega);
				b2 = a * ((a + 1.0) + (a - 1.0) * cosOmega - sqrtA2Alpha);
				a0 = (a + 1.0) - (a - 1.0) * cosOmega + sqrtA2Alpha;
				a1 = 2.0 * ((a - 1.0) - (a + 1.0) * cosOmega);
				a2 = (a + 1.0) - (a - 1.0) * cosOmega - sqrtA2Alpha;
				break;
			}
			case EchoFilterType.LowPass:
			{
				b0 = (1.0 - cosOmega) * 0.5;
				b1 = 1.0 - cosOmega;
				b2 = (1.0 - cosOmega) * 0.5;
				a0 = 1.0 + alpha;
				a1 = -2.0 * cosOmega;
				a2 = 1.0 - alpha;
				break;
			}
			case EchoFilterType.HighPass:
			{
				b0 = (1.0 + cosOmega) * 0.5;
				b1 = -(1.0 + cosOmega);
				b2 = (1.0 + cosOmega) * 0.5;
				a0 = 1.0 + alpha;
				a1 = -2.0 * cosOmega;
				a2 = 1.0 - alpha;
				break;
			}
			case EchoFilterType.BandPass:
			{
				b0 = alpha;
				b1 = 0.0;
				b2 = -alpha;
				a0 = 1.0 + alpha;
				a1 = -2.0 * cosOmega;
				a2 = 1.0 - alpha;
				break;
			}
			case EchoFilterType.Notch:
			{
				b0 = 1.0;
				b1 = -2.0 * cosOmega;
				b2 = 1.0;
				a0 = 1.0 + alpha;
				a1 = -2.0 * cosOmega;
				a2 = 1.0 - alpha;
				break;
			}
			case EchoFilterType.Peaking:
			default:
			{
				b0 = 1.0 + alpha * a;
				b1 = -2.0 * cosOmega;
				b2 = 1.0 - alpha * a;
				a0 = 1.0 + alpha / a;
				a1 = -2.0 * cosOmega;
				a2 = 1.0 - alpha / a;
				break;
			}
		}

		if (Math.Abs(a0) > 1e-12)
		{
			_b0 = b0 / a0;
			_b1 = b1 / a0;
			_b2 = b2 / a0;
			_a1 = a1 / a0;
			_a2 = a2 / a0;
		}
	}

	public float ProcessSample(float input, int channel)
	{
		if (!Enabled) return input;
		if (channel >= _x1.Length) return input;

		double x = input;
		double output = _b0 * x + _b1 * _x1[channel] + _b2 * _x2[channel] - _a1 * _y1[channel] - _a2 * _y2[channel];

		_x2[channel] = _x1[channel];
		_x1[channel] = x;
		_y2[channel] = _y1[channel];
		_y1[channel] = double.IsFinite(output) ? output : 0.0;

		return (float)_y1[channel];
	}

	public void Reset()
	{
		Array.Clear(_x1, 0, _x1.Length);
		Array.Clear(_x2, 0, _x2.Length);
		Array.Clear(_y1, 0, _y1.Length);
		Array.Clear(_y2, 0, _y2.Length);
	}
}
