using System;

namespace SonicWave.Audio.Dsp;

public sealed class SpatialAudioEffect
{
	private sealed class SpatialCore
	{
		private readonly ChannelChain _l;

		private readonly ChannelChain _r;

		private double _feedback = 0.9;

		private double _damp1;

		private double _damp2;

		private double _modMs = 0.4;

		public SpatialCore(int sampleRate, int seed)
		{
			_l = new ChannelChain(sampleRate, seed, left: true);
			_r = new ChannelChain(sampleRate, seed + 101, left: false);
		}

		public void Apply(double roomSize, double damping, double modulation)
		{
			_feedback = 0.82 + roomSize * 0.18;
			_damp1 = damping * 0.4;
			_damp2 = 1.0 - _damp1;
			_modMs = 0.1 + modulation * 0.6;
			_l.Apply(_feedback, _damp1, _damp2, _modMs);
			_r.Apply(_feedback, _damp1, _damp2, _modMs);
		}

		public double ProcessL(double input)
		{
			return _l.Process(input);
		}

		public double ProcessR(double input)
		{
			return _r.Process(input);
		}

		public void Reset()
		{
			_l.Reset();
			_r.Reset();
		}
	}

	private sealed class ChannelChain
	{
		private static readonly double[] DiffuseMs = new double[4] { 5.0, 1.7, 9.8, 3.7 };

		private static readonly double[] CombMsL = new double[6] { 29.7, 37.1, 41.1, 43.7, 45.9, 47.8 };

		private static readonly double[] CombMsR = new double[6] { 30.2, 37.8, 41.9, 44.5, 46.7, 48.7 };

		private const double TailMs = 13.5;

		private const double CombGain = 0.012;

		private readonly ModDelay[] _diffusers = new ModDelay[4];

		private readonly ModDelay[] _combs = new ModDelay[6];

		private readonly double[] _combFilter = new double[6];

		private ModDelay _tail;

		private double _damp1;

		private double _damp2;

		public ChannelChain(int sampleRate, int seed, bool left)
		{
			double[] array = (left ? CombMsL : CombMsR);
			for (int i = 0; i < 4; i++)
			{
				_diffusers[i] = new ModDelay(sampleRate, DiffuseMs[i], 0.15, RateHz(seed + i, left), seed + i);
			}
			for (int j = 0; j < 6; j++)
			{
				_combs[j] = new ModDelay(sampleRate, array[j], 0.2, RateHz(seed + 10 + j, left), seed + 10 + j);
			}
			_tail = new ModDelay(sampleRate, 13.5, 0.5, RateHz(seed + 20, left), seed + 20);
		}

		private static double RateHz(int seed, bool left)
		{
			double num = 0.12 + (double)(seed % 17) / 17.0 * 0.2;
			if (!left)
			{
				return num + 0.037;
			}
			return num;
		}

		public void Apply(double feedback, double damp1, double damp2, double modMs)
		{
			_damp1 = damp1;
			_damp2 = damp2;
			ModDelay[] diffusers = _diffusers;
			for (int i = 0; i < diffusers.Length; i++)
			{
				diffusers[i].ModMs = modMs * 0.6;
			}
			diffusers = _combs;
			for (int i = 0; i < diffusers.Length; i++)
			{
				diffusers[i].ModMs = modMs;
			}
			_tail.ModMs = modMs * 1.6;
		}

		public double Process(double input)
		{
			double num = input;
			for (int i = 0; i < 4; i++)
			{
				num = _diffusers[i].Allpass(num, 0.5);
			}
			double num2 = 0.0;
			for (int j = 0; j < 6; j++)
			{
				double num3 = _combs[j].Read();
				num2 += num3;
				double num4 = num3 * _damp2 + _combFilter[j] * _damp1;
				_combFilter[j] = num4;
				_combs[j].Write((float)(num + num4 * 0.5));
			}
			num2 *= 0.012;
			return _tail.Allpass(num2, 0.35);
		}

		public void Reset()
		{
			ModDelay[] diffusers = _diffusers;
			for (int i = 0; i < diffusers.Length; i++)
			{
				diffusers[i].Reset();
			}
			diffusers = _combs;
			for (int i = 0; i < diffusers.Length; i++)
			{
				diffusers[i].Reset();
			}
			_tail.Reset();
			Array.Clear(_combFilter);
		}
	}

	private sealed class ModDelay
	{
		private readonly float[] _buf;

		private readonly int _baseDelay;

		private readonly int _maxMod;

		private readonly double _rateHz;

		private readonly double _phase0;

		private int _idx;

		private double _phase;

		public double ModMs { get; set; } = 0.2;

		public ModDelay(int sampleRate, double baseMs, double modMs, double rateHz, int seed)
		{
			_baseDelay = Math.Max(1, (int)((double)sampleRate * baseMs / 1000.0));
			_maxMod = Math.Max(1, (int)((double)sampleRate * modMs / 1000.0));
			_buf = new float[_baseDelay + _maxMod + 2];
			_rateHz = rateHz;
			_phase0 = (double)(seed % 1000) / 1000.0;
			ModMs = modMs;
		}

		public float Read()
		{
			AdvancePhase();
			double num = ModMs * (double)_maxMod;
			int num2 = _baseDelay + (int)(num * (0.5 + 0.5 * Math.Sin(Math.PI * 2.0 * _phase)));
			int num3 = _buf.Length;
			int num4 = (_idx - num2 % num3 + num3 * 2) % num3;
			return _buf[num4];
		}

		public void Write(float x)
		{
			_buf[_idx] = x;
			_idx = (_idx + 1) % _buf.Length;
		}

		public float Allpass(double input, double feedback)
		{
			double num = Read();
			double num2 = 0.0 - input + num;
			Write((float)(input + num * feedback));
			return (float)num2;
		}

		private void AdvancePhase()
		{
			_phase += _rateHz / 44100.0;
			if (_phase > 1.0)
			{
				_phase--;
			}
		}

		public void Reset()
		{
			Array.Clear(_buf);
			_idx = 0;
			_phase = _phase0;
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
		private static readonly double[] TapsMs = new double[4] { 13.2, 19.6, 27.3, 33.4 };

		private static readonly double[] TapGains = new double[4] { 0.5, 0.4, 0.32, 0.24 };

		private readonly float[][] _bufsL = new float[4][];

		private readonly float[][] _bufsR = new float[4][];

		private readonly int[] _idxL = new int[4];

		private readonly int[] _idxR = new int[4];

		public EarlyReflectionLines(int sampleRate)
		{
			for (int i = 0; i < 4; i++)
			{
				int num = Math.Max(1, (int)((double)sampleRate * TapsMs[i] / 1000.0));
				_bufsL[i] = new float[num];
				_bufsR[i] = new float[num + 3];
			}
		}

		public double ProcessL(double input)
		{
			double num = 0.0;
			for (int i = 0; i < 4; i++)
			{
				float[] array = _bufsL[i];
				int num2 = _idxL[i];
				num += (double)array[num2] * TapGains[i];
				array[num2] = (float)input;
				_idxL[i] = ((num2 + 1 < array.Length) ? (num2 + 1) : 0);
			}
			return num;
		}

		public double ProcessR(double input)
		{
			double num = 0.0;
			for (int i = 0; i < 4; i++)
			{
				float[] array = _bufsR[i];
				int num2 = _idxR[i];
				num += (double)array[num2] * TapGains[i];
				array[num2] = (float)input;
				_idxR[i] = ((num2 + 1 < array.Length) ? (num2 + 1) : 0);
			}
			return num;
		}

		public void Reset()
		{
			float[][] bufsL = _bufsL;
			for (int i = 0; i < bufsL.Length; i++)
			{
				Array.Clear(bufsL[i]);
			}
			bufsL = _bufsR;
			for (int i = 0; i < bufsL.Length; i++)
			{
				Array.Clear(bufsL[i]);
			}
			Array.Clear(_idxL);
			Array.Clear(_idxR);
		}
	}

	private const double LowCrossHz = 240.0;

	private const double HighCrossHz = 2400.0;

	private readonly SpatialCore[] _cores = new SpatialCore[3];

	private readonly Crossover _cross;

	private readonly EarlyReflectionLines _early;

	private float[]? _predelayBuf;

	private int _predelayIdx;

	private int _predelayLen;

	private double _roomSize = 0.6;

	private double _damping = 0.45;

	private double _wet = 0.25;

	private double _dry = 1.0;

	private double _predelayMs = 25.0;

	private double _width = 0.6;

	private double _modulation = 0.25;

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
			_predelayMs = Math.Clamp(value, 0.0, 120.0);
			SetPredelayMs(_predelayMs);
		}
	}

	public double Width
	{
		get
		{
			return _width;
		}
		set
		{
			_width = Math.Clamp(value, 0.0, 1.0);
		}
	}

	public double Modulation
	{
		get
		{
			return _modulation;
		}
		set
		{
			_modulation = Math.Clamp(value, 0.0, 1.0);
		}
	}

	public static string[] Presets => new string[5] { "大厅", "板式", "房间", "教堂", "空间" };

	public SpatialAudioEffect(int sampleRate = 44100)
	{
		SampleRate = sampleRate;
		_cross = new Crossover(sampleRate, 240.0, 2400.0);
		_early = new EarlyReflectionLines(sampleRate);
		for (int i = 0; i < 3; i++)
		{
			_cores[i] = new SpatialCore(sampleRate, i * 7 + 1);
		}
		SetPredelayMs(_predelayMs);
		ApplyParameters();
	}

	public void SetPreset(string name)
	{
		switch (name)
		{
		case "大厅":
			RoomSize = 0.8;
			Damping = 0.35;
			WetMix = 0.32;
			PredelayMs = 30.0;
			Width = 0.7;
			Modulation = 0.25;
			break;
		case "板式":
			RoomSize = 0.5;
			Damping = 0.5;
			WetMix = 0.28;
			PredelayMs = 12.0;
			Width = 0.5;
			Modulation = 0.4;
			break;
		case "房间":
			RoomSize = 0.45;
			Damping = 0.6;
			WetMix = 0.2;
			PredelayMs = 20.0;
			Width = 0.4;
			Modulation = 0.15;
			break;
		case "教堂":
			RoomSize = 0.95;
			Damping = 0.2;
			WetMix = 0.4;
			PredelayMs = 45.0;
			Width = 0.85;
			Modulation = 0.2;
			break;
		case "空间":
			RoomSize = 0.25;
			Damping = 0.7;
			WetMix = 0.12;
			PredelayMs = 10.0;
			Width = 0.9;
			Modulation = 0.1;
			break;
		default:
			RoomSize = 0.6;
			Damping = 0.45;
			WetMix = 0.25;
			PredelayMs = 25.0;
			Width = 0.6;
			Modulation = 0.25;
			break;
		}
	}

	public void SetParameters(double roomSize, double damping, double wetMix, double predelayMs, double width, double modulation)
	{
		RoomSize = roomSize;
		Damping = damping;
		WetMix = wetMix;
		PredelayMs = predelayMs;
		Width = width;
		Modulation = modulation;
	}

	public static SpatialParameters PresetDefaults(string preset)
	{
		return preset switch
		{
			"板式" => new SpatialParameters(0.5, 0.4, 12.0), 
			"房间" => new SpatialParameters(0.4, 0.15, 20.0), 
			"教堂" => new SpatialParameters(0.85, 0.2, 45.0), 
			"空间" => new SpatialParameters(0.9, 0.1, 10.0), 
			_ => new SpatialParameters(0.7, 0.25, 30.0), 
		};
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
		_cores[0].Apply(Math.Min(1.0, _roomSize * 1.18), Math.Max(0.05, _damping * 0.55), _modulation);
		_cores[1].Apply(_roomSize, _damping, _modulation);
		_cores[2].Apply(Math.Max(0.1, _roomSize * 0.72), Math.Min(0.95, _damping * 1.3 + 0.1), _modulation);
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
			for (int i = 0; i < num; i++)
			{
				double num2 = samples[i];
				double num3 = ReadPredelay(num2);
				(double Low, double Mid, double High) tuple = _cross.Process(num3);
				double item = tuple.Low;
				double item2 = tuple.Mid;
				double item3 = tuple.High;
				double num4 = _cores[0].ProcessL(item) + _cores[1].ProcessL(item2) + _cores[2].ProcessL(item3);
				double num5 = _cores[0].ProcessR(item) + _cores[1].ProcessR(item2) + _cores[2].ProcessR(item3);
				double num6 = num4 + _early.ProcessL(num3) * 0.3;
				num5 += _early.ProcessR(num3) * 0.3;
				double num7 = (num6 + num5) * 0.5;
				samples[i] = SoftLimit(num2 * _dry + num7 * _wet);
			}
		}
	}

	public void ProcessInterleaved(float[] samples, int channels)
	{
		if (channels <= 0 || _wet <= 0.001)
		{
			return;
		}
		if (channels == 1)
		{
			Process(samples, samples.Length);
			return;
		}
		int num = samples.Length / channels;
		for (int i = 0; i < num; i++)
		{
			int num2 = i * channels;
			double num3 = samples[num2];
			double num4 = samples[num2 + 1];
			double num5 = ReadPredelay(num3);
			double num6 = ReadPredelay(num4);
			(double Low, double Mid, double High) tuple = _cross.Process(num5);
			double item = tuple.Low;
			double item2 = tuple.Mid;
			double item3 = tuple.High;
			(double Low, double Mid, double High) tuple2 = _cross.Process(num6);
			double item4 = tuple2.Low;
			double item5 = tuple2.Mid;
			double item6 = tuple2.High;
			double num7 = _cores[0].ProcessL(item) + _cores[1].ProcessL(item2) + _cores[2].ProcessL(item3);
			double num8 = _cores[0].ProcessR(item4) + _cores[1].ProcessR(item5) + _cores[2].ProcessR(item6);
			double num9 = num7 + _early.ProcessL(num5) * 0.3;
			num8 += _early.ProcessR(num6) * 0.3;
			double num10 = (num9 + num8) * 0.5;
			double num11 = (num9 - num8) * 0.5 * _width;
			double num12 = num10 + num11;
			double num13 = num10 - num11;
			samples[num2] = SoftLimit(num3 * _dry + num12 * _wet);
			samples[num2 + 1] = SoftLimit(num4 * _dry + num13 * _wet);
		}
	}

	private double ReadPredelay(double input)
	{
		float[]? predelayBuf = _predelayBuf;
		double result = predelayBuf[_predelayIdx];
		predelayBuf[_predelayIdx] = (float)input;
		_predelayIdx = ((_predelayIdx + 1 < _predelayLen) ? (_predelayIdx + 1) : 0);
		return result;
	}

	public void Reset()
	{
		SpatialCore[] cores = _cores;
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
