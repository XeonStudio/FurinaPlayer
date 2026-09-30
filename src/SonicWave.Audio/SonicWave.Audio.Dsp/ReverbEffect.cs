using System;

namespace SonicWave.Audio.Dsp;

public sealed class ReverbEffect
{
	private sealed class ReverbCore
	{
		private static readonly double[] CombTunings = new double[8] { 1116.0, 1188.0, 1277.0, 1356.0, 1422.0, 1491.0, 1557.0, 1617.0 };

		private static readonly double[] AllpassTunings = new double[4] { 556.0, 441.0, 341.0, 225.0 };

		private const double FixedGain = 0.015;

		private const double CombFeedback = 0.5;

		private readonly double[][] _combBuf = new double[8][];

		private readonly double[][] _allpassBuf = new double[4][];

		private readonly int[] _combIdx = new int[8];

		private readonly int[] _allpassIdx = new int[4];

		private readonly double[] _combFilter = new double[8];

		private double _damp1;

		private double _damp2;

		private double _feedbackTarget = 0.9;

		public ReverbCore(int sampleRate)
		{
			double num = (double)sampleRate / 44100.0;
			for (int i = 0; i < 8; i++)
			{
				_combBuf[i] = new double[(int)(CombTunings[i] * num)];
			}
			for (int j = 0; j < 4; j++)
			{
				_allpassBuf[j] = new double[(int)(AllpassTunings[j] * num)];
			}
		}

		public void Apply(double roomSize, double damping)
		{
			_feedbackTarget = 0.8 + roomSize * 0.2;
			_damp1 = damping * 0.4;
			_damp2 = 1.0 - _damp1;
		}

		public double Process(double input)
		{
			double num = 0.0;
			for (int i = 0; i < 8; i++)
			{
				int num2 = _combIdx[i];
				double[] array = _combBuf[i];
				double num3 = array[num2];
				num += num3;
				double num4 = num3 * _damp2 + _damp1 * _combFilter[i];
				_combFilter[i] = num4;
				array[num2] = input + num4 * 0.5;
				_combIdx[i] = ((num2 + 1 < array.Length) ? (num2 + 1) : 0);
			}
			num *= 0.015;
			for (int j = 0; j < 4; j++)
			{
				int num5 = _allpassIdx[j];
				double[] array2 = _allpassBuf[j];
				num = array2[num5] - num;
				array2[num5] = num * 0.5 + input;
				_allpassIdx[j] = ((num5 + 1 < array2.Length) ? (num5 + 1) : 0);
			}
			return num;
		}

		public void Reset()
		{
			double[][] combBuf = _combBuf;
			for (int i = 0; i < combBuf.Length; i++)
			{
				Array.Clear(combBuf[i]);
			}
			combBuf = _allpassBuf;
			for (int i = 0; i < combBuf.Length; i++)
			{
				Array.Clear(combBuf[i]);
			}
			for (int j = 0; j < 8; j++)
			{
				_combFilter[j] = _feedbackTarget;
			}
			for (int k = 0; k < 8; k++)
			{
				_combIdx[k] = 0;
			}
			for (int l = 0; l < 4; l++)
			{
				_allpassIdx[l] = 0;
			}
		}
	}

	private sealed class Crossover
	{
		private readonly OnePole _lpA;

		private readonly OnePole _lpB;

		private readonly OnePole _hpA;

		private readonly OnePole _hpB;

		public Crossover(int sampleRate, double lowHz, double highHz)
		{
			_lpA = new OnePole(sampleRate, lowHz, low: true);
			_lpB = new OnePole(sampleRate, lowHz, low: true);
			_hpA = new OnePole(sampleRate, highHz, low: false);
			_hpB = new OnePole(sampleRate, highHz, low: false);
		}

		public (double Low, double Mid, double High) Process(double x)
		{
			double num = _lpB.Process(_lpA.Process(x));
			double num2 = _hpB.Process(_hpA.Process(x));
			return (Low: num, Mid: x - num - num2, High: num2);
		}

		public void Reset()
		{
			_lpA.Reset();
			_lpB.Reset();
			_hpA.Reset();
			_hpB.Reset();
		}
	}

	private sealed class OnePole
	{
		private readonly bool _low;

		private readonly double _a;

		private double _y;

		private double _x1;

		public OnePole(int sampleRate, double hz, bool low)
		{
			_low = low;
			_a = Math.Exp(Math.PI * -2.0 * Math.Clamp(hz, 1.0, (double)sampleRate * 0.49) / (double)sampleRate);
		}

		public double Process(double x)
		{
			if (_low)
			{
				_y += (1.0 - _a) * (x - _y);
				return _y;
			}
			double result = (_y = _a * (_y + x - _x1));
			_x1 = x;
			return result;
		}

		public void Reset()
		{
			_y = 0.0;
			_x1 = 0.0;
		}
	}

	private sealed class EarlyReflectionLines
	{
		private static readonly double[] TapsMs = new double[4] { 19.6, 27.3, 33.4, 39.7 };

		private static readonly double[] TapGains = new double[4] { 0.55, 0.45, 0.35, 0.25 };

		private readonly float[][] _bufs = new float[4][];

		private readonly int[] _idx = new int[4];

		private readonly int[] _lens = new int[4];

		public EarlyReflectionLines(int sampleRate)
		{
			for (int i = 0; i < 4; i++)
			{
				_lens[i] = Math.Max(1, (int)((double)sampleRate * TapsMs[i] / 1000.0));
				_bufs[i] = new float[_lens[i]];
			}
		}

		public double Process(double input)
		{
			double num = 0.0;
			for (int i = 0; i < 4; i++)
			{
				float[] array = _bufs[i];
				int num2 = _idx[i];
				num += (double)array[num2] * TapGains[i];
				array[num2] = (float)input;
				_idx[i] = ((num2 + 1 < _lens[i]) ? (num2 + 1) : 0);
			}
			return num;
		}

		public void Reset()
		{
			float[][] bufs = _bufs;
			for (int i = 0; i < bufs.Length; i++)
			{
				Array.Clear(bufs[i]);
			}
			for (int j = 0; j < 4; j++)
			{
				_idx[j] = 0;
			}
		}
	}

	private const double LowCrossHz = 260.0;

	private const double HighCrossHz = 2400.0;

	private readonly ReverbCore[] _cores = new ReverbCore[3];

	private readonly Crossover _cross;

	private readonly EarlyReflectionLines _early;

	private float[]? _predelayBuf;

	private int _predelayIdx;

	private int _predelayLen;

	private double _roomSize = 0.6;

	private double _damping = 0.4;

	private double _wet = 0.2;

	private double _dry = 1.0;

	private double _predelayMs = 25.0;

	private double _earlyMix = 0.3;

	private float[]? _monoScratch;

	public int SampleRate { get; }

	public double RoomSize
	{
		get
		{
			return _roomSize;
		}
		set
		{
			_roomSize = Math.Clamp(value, 0.0, 1.0);
			ApplyParameters();
		}
	}

	public double Damping
	{
		get
		{
			return _damping;
		}
		set
		{
			_damping = Math.Clamp(value, 0.0, 1.0);
			ApplyParameters();
		}
	}

	public double WetMix
	{
		get
		{
			return _wet;
		}
		set
		{
			_wet = Math.Clamp(value, 0.0, 1.0);
		}
	}

	public double PredelayMs
	{
		get
		{
			return _predelayMs;
		}
		set
		{
			_predelayMs = Math.Clamp(value, 0.0, 100.0);
			SetPredelayMs(_predelayMs);
		}
	}

	public double EarlyReflections
	{
		get
		{
			return _earlyMix;
		}
		set
		{
			_earlyMix = Math.Clamp(value, 0.0, 1.0);
		}
	}

	public static string[] Presets => new string[5] { "大厅", "房间", "教堂", "录音室", "浴室" };

	public ReverbEffect(int sampleRate = 44100)
	{
		SampleRate = sampleRate;
		_cross = new Crossover(sampleRate, 260.0, 2400.0);
		_early = new EarlyReflectionLines(sampleRate);
		for (int i = 0; i < 3; i++)
		{
			_cores[i] = new ReverbCore(sampleRate);
		}
		SetPredelayMs(_predelayMs);
		ApplyParameters();
	}

	public void SetPreset(string name)
	{
		switch (name)
		{
		case "房间":
			RoomSize = 0.5;
			Damping = 0.5;
			WetMix = 0.25;
			EarlyReflections = 0.25;
			break;
		case "大厅":
			RoomSize = 0.8;
			Damping = 0.3;
			WetMix = 0.35;
			EarlyReflections = 0.35;
			break;
		case "教堂":
			RoomSize = 0.95;
			Damping = 0.15;
			WetMix = 0.4;
			EarlyReflections = 0.4;
			break;
		case "录音室":
			RoomSize = 0.3;
			Damping = 0.7;
			WetMix = 0.15;
			EarlyReflections = 0.2;
			break;
		case "浴室":
			RoomSize = 0.65;
			Damping = 0.25;
			WetMix = 0.3;
			EarlyReflections = 0.3;
			break;
		default:
			RoomSize = 0.6;
			Damping = 0.4;
			WetMix = 0.2;
			EarlyReflections = 0.3;
			break;
		}
	}

	private void SetPredelayMs(double ms)
	{
		int num = Math.Max(1, (int)((double)SampleRate * ms / 1000.0));
		if (_predelayBuf == null || _predelayBuf.Length != num)
		{
			_predelayBuf = new float[num];
			_predelayIdx = 0;
		}
		_predelayLen = num;
	}

	private void ApplyParameters()
	{
		_cores[0].Apply(Math.Min(1.0, _roomSize * 1.2), Math.Max(0.05, _damping * 0.6));
		_cores[1].Apply(_roomSize, _damping);
		_cores[2].Apply(Math.Max(0.1, _roomSize * 0.75), Math.Min(0.95, _damping * 1.25 + 0.12));
	}

	public void Process(float[] samples)
	{
		Process(samples, samples.Length);
	}

	public void Process(float[] samples, int count)
	{
		if (!(_wet <= 0.001))
		{
			int num = Math.Min(count, samples.Length);
			float[] predelayBuf = _predelayBuf;
			int predelayLen = _predelayLen;
			for (int i = 0; i < num; i++)
			{
				double num2 = samples[i];
				double num3 = predelayBuf[_predelayIdx];
				predelayBuf[_predelayIdx] = (float)num2;
				_predelayIdx = ((_predelayIdx + 1 < predelayLen) ? (_predelayIdx + 1) : 0);
				(double Low, double Mid, double High) tuple = _cross.Process(num3);
				double item = tuple.Low;
				double item2 = tuple.Mid;
				double item3 = tuple.High;
				double num4 = _cores[0].Process(item) + _cores[1].Process(item2) + _cores[2].Process(item3);
				num4 += _early.Process(num3) * _earlyMix;
				double x = num2 * _dry + num4 * _wet;
				samples[i] = SoftLimit(x);
			}
		}
	}

	public void ProcessInterleaved(float[] samples, int channels)
	{
		if (channels <= 0 || _wet <= 0.001)
		{
			return;
		}
		int num = (samples.Length + channels - 1) / channels;
		if (_monoScratch == null || _monoScratch.Length < num)
		{
			_monoScratch = new float[num];
		}
		for (int i = 0; i < channels; i++)
		{
			int count = 0;
			for (int j = i; j < samples.Length; j += channels)
			{
				_monoScratch[count++] = samples[j];
			}
			Process(_monoScratch, count);
			int num2 = i;
			int num3 = 0;
			while (num2 < samples.Length)
			{
				samples[num2] = _monoScratch[num3];
				num2 += channels;
				num3++;
			}
		}
	}

	public void Reset()
	{
		ReverbCore[] cores = _cores;
		for (int i = 0; i < cores.Length; i++)
		{
			cores[i].Reset();
		}
		_cross.Reset();
		_early.Reset();
		if (_predelayBuf != null)
		{
			Array.Clear(_predelayBuf);
		}
		_predelayIdx = 0;
	}

	private static float SoftLimit(double x)
	{
		if (x > 1.0)
		{
			x = 1.0 + (x - 1.0) / 3.0;
		}
		else if (x < -1.0)
		{
			x = -1.0 + (x + 1.0) / 3.0;
		}
		return (float)x;
	}
}
