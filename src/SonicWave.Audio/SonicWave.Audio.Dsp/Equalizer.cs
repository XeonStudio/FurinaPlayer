using System;
using System.Linq;

namespace SonicWave.Audio.Dsp;

public sealed class Equalizer
{
	public sealed class BiQuadPeaking
	{
		private const double SmoothStepDb = 0.03;

		private double _targetGainDb;

		private double _gainDb;

		private double _b0;

		private double _b1;

		private double _b2;

		private double _a1;

		private double _a2;

		private double _x1;

		private double _x2;

		private double _y1;

		private double _y2;

		private double _freq;

		public int SampleRate { get; }

		public double Q { get; }

		public double GainDb
		{
			get
			{
				return _targetGainDb;
			}
			set
			{
				_targetGainDb = Math.Clamp(value, -12.0, 12.0);
			}
		}

		public double Frequency
		{
			get
			{
				return _freq;
			}
			set
			{
				_freq = value;
				Recalculate();
			}
		}

		public bool IsSmoothing => Math.Abs(_targetGainDb - _gainDb) > 0.001;

		public BiQuadPeaking(int sampleRate, double freq, double q, double gainDb)
		{
			SampleRate = sampleRate;
			_freq = freq;
			Q = q;
			_targetGainDb = Math.Clamp(gainDb, -12.0, 12.0);
			_gainDb = _targetGainDb;
			Recalculate();
		}

		private void Recalculate()
		{
			double num = Math.Pow(10.0, _gainDb / 40.0);
			double num2 = Math.PI * 2.0 * Math.Clamp(_freq, 1.0, (double)SampleRate * 0.49) / (double)SampleRate;
			double num3 = Math.Sin(num2) / (2.0 * Q);
			double num4 = Math.Cos(num2);
			double num5 = 1.0 + num3 * num;
			double num6 = -2.0 * num4;
			double num7 = 1.0 - num3 * num;
			double num8 = 1.0 + num3 / num;
			double num9 = -2.0 * num4;
			double num10 = 1.0 - num3 / num;
			_b0 = num5 / num8;
			_b1 = num6 / num8;
			_b2 = num7 / num8;
			_a1 = num9 / num8;
			_a2 = num10 / num8;
		}

		public double Process(double x)
		{
			if (Math.Abs(_targetGainDb - _gainDb) > 0.001)
			{
				_gainDb = ((_targetGainDb > _gainDb) ? Math.Min(_targetGainDb, _gainDb + 0.03) : Math.Max(_targetGainDb, _gainDb - 0.03));
				Recalculate();
			}
			double num = _b0 * x + _b1 * _x1 + _b2 * _x2 - _a1 * _y1 - _a2 * _y2;
			_x2 = _x1;
			_x1 = x;
			_y2 = _y1;
			_y1 = num;
			return num;
		}

		public void Reset()
		{
			_x1 = (_x2 = (_y1 = (_y2 = 0.0)));
		}
	}

	public static readonly double[] BandFrequencies = new double[10] { 31.0, 62.0, 125.0, 250.0, 500.0, 1000.0, 2000.0, 4000.0, 8000.0, 16000.0 };

	private readonly BiQuadPeaking[] _filters;

	private double[] _gains = new double[10];

	private float[] _monoScratch = Array.Empty<float>();

	public int SampleRate { get; }

	public double[] Gains
	{
		get
		{
			return _gains;
		}
		set
		{
			for (int i = 0; i < 10; i++)
			{
				_gains[i] = Math.Clamp(value[i], -12.0, 12.0);
				_filters[i].GainDb = _gains[i];
			}
		}
	}

	public bool IsActive => _gains.Any((double g) => Math.Abs(g) > 0.05);

	public bool IsSmoothing => _filters.Any((BiQuadPeaking f) => f.IsSmoothing);

	public Equalizer(int sampleRate = 44100)
	{
		SampleRate = sampleRate;
		_filters = new BiQuadPeaking[10];
		for (int i = 0; i < 10; i++)
		{
			_filters[i] = new BiQuadPeaking(sampleRate, BandFrequencies[i], 1.0, 0.0);
		}
	}

	public void Process(float[] samples)
	{
		if (!IsActive && !IsSmoothing)
		{
			return;
		}
		for (int i = 0; i < samples.Length; i++)
		{
			double num = samples[i];
			BiQuadPeaking[] filters = _filters;
			for (int j = 0; j < filters.Length; j++)
			{
				num = filters[j].Process(num);
			}
			samples[i] = (float)Math.Clamp(num, -1.0, 1.0);
		}
	}

	public void ProcessInterleaved(float[] samples, int channels)
	{
		if ((!IsActive && !IsSmoothing) || channels <= 0)
		{
			return;
		}
		for (int i = 0; i < channels; i++)
		{
			int num = (samples.Length + i) / channels;
			if (_monoScratch.Length != num)
			{
				_monoScratch = new float[num];
			}
			float[] monoScratch = _monoScratch;
			int num2 = i;
			int num3 = 0;
			while (num2 < samples.Length)
			{
				monoScratch[num3] = samples[num2];
				num2 += channels;
				num3++;
			}
			Process(monoScratch);
			int num4 = i;
			int num5 = 0;
			while (num4 < samples.Length)
			{
				samples[num4] = monoScratch[num5];
				num4 += channels;
				num5++;
			}
		}
	}

	public void Reset()
	{
		BiQuadPeaking[] filters = _filters;
		for (int i = 0; i < filters.Length; i++)
		{
			filters[i].Reset();
		}
	}
}
