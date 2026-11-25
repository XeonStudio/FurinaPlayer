using System;
using System.Collections.Generic;

namespace FurinaPlayer.Audio.Dsp;

public enum NeoverbHallFlavor
{
	Hall = 0,
	Room = 1,
	Chamber = 2
}

public enum NeoverbModMode
{
	Sine = 0,
	Random = 1
}

public sealed class NeoverbPreset
{
	public string Name { get; init; } = "Default";
	public double BlendX { get; init; } = 0.5;
	public double BlendY { get; init; } = 0.45;
	public double ReflectionsSpace { get; init; } = 31.0;
	public double PlateSpace { get; init; } = 1.10;
	public double HallSpace { get; init; } = 2.50;
	public NeoverbHallFlavor HallFlavor { get; init; } = NeoverbHallFlavor.Hall;
	public NeoverbModMode ModMode { get; init; } = NeoverbModMode.Sine;
	public double ModRateHz { get; init; } = 0.48;
	public double ModDepthPct { get; init; } = 25.0;
	public double PreDelayMs { get; init; } = 20.0;
	public double SmoothPct { get; init; } = 100.0;
	public double DryWetPct { get; init; } = 32.0;
	public double LevelDb { get; init; } = 0.0;
	public bool AutoCutEnabled { get; init; } = true;
}

/// <summary>
/// FurinaPlayer Neoverb Pro — 三引擎混响工作台处理器。
/// 复刻 iZotope Neoverb Pro 核心架构：Early Reflections + Plate + Hall 三角混合垫、
/// Pre-Delay、Smooth 瞬态柔化、Mod 调制、Pre EQ (含 Auto Cut 共振削减) 与 Reverb EQ。
/// </summary>
public sealed class FurinaNeoverbProcessor
{
	private const int NumFdnLines = 4;
	private static readonly int[] ReflTapsL = { 131, 277, 439, 613, 829, 1061, 1327, 1609 };
	private static readonly int[] ReflTapsR = { 157, 311, 479, 659, 881, 1109, 1381, 1667 };
	private static readonly int[] PlatePrimeSamples = { 1493, 1877, 2269, 2713 };
	private static readonly int[] HallPrimeSamples = { 2111, 2659, 3251, 3917 };

	private double _sampleRate = 44100.0;
	private readonly object _lock = new();

	public bool Enabled { get; set; }
	public double BlendX { get; set; } = 0.50; // 0=Plate(BL), 1=Hall(BR), 0.5&Y=0 => Reflections(Top)
	public double BlendY { get; set; } = 0.45; // 0=Top(Reflections), 1=Bottom(Plate/Hall)
	public double ReflectionsSpace { get; set; } = 31.0; // 0..100
	public double PlateSpace { get; set; } = 1.10; // 0.2..6.0 s
	public double HallSpace { get; set; } = 2.50; // 0.4..12.0 s
	public NeoverbHallFlavor HallFlavor { get; set; } = NeoverbHallFlavor.Hall;
	public NeoverbModMode ModMode { get; set; } = NeoverbModMode.Sine;
	public double ModRateHz { get; set; } = 0.48;
	public double ModDepthPct { get; set; } = 25.0;
	public double PreDelayMs { get; set; } = 20.0; // 0..250 ms
	public double SmoothPct { get; set; } = 100.0; // 0..100 %
	public double DryWetPct { get; set; } = 32.0; // 0..100 %
	public double LevelDb { get; set; } = 0.0; // -24..+6 dB
	public bool PreEqEnabled { get; set; } = true;
	public bool ReverbEqEnabled { get; set; } = true;
	public bool AutoCutEnabled { get; set; } = true;

	// 3-band Pre EQ & 3-band Reverb EQ: [Low, Mid, High]
	public double[] PreEqFreqs { get; } = { 100.0, 1500.0, 9500.0 };
	public double[] PreEqGainsDb { get; } = { 0.0, 0.0, 0.0 };
	public double[] PreEqQs { get; } = { 0.7, 0.8, 0.7 };

	public double[] ReverbEqFreqs { get; } = { 140.0, 1800.0, 8200.0 };
	public double[] ReverbEqGainsDb { get; } = { -2.0, 0.0, -1.5 };
	public double[] ReverbEqQs { get; } = { 0.7, 0.8, 0.7 };

	private readonly FurinaPlayerBiquadFilter[] _preEqFilters = new FurinaPlayerBiquadFilter[3];
	private readonly FurinaPlayerBiquadFilter[] _revEqFilters = new FurinaPlayerBiquadFilter[3];
	private readonly FurinaPlayerBiquadFilter _autoCutFilter = new();
	private readonly FurinaPlayerBiquadFilter _smoothLpFilter = new();

	private float[] _preDelayBufL = new float[16384];
	private float[] _preDelayBufR = new float[16384];
	private int _preDelayPos;

	private float[] _reflBufL = new float[8192];
	private float[] _reflBufR = new float[8192];
	private int _reflPos;

	private readonly float[][] _plateBuf = new float[NumFdnLines][];
	private readonly int[] _platePos = new int[NumFdnLines];
	private readonly float[] _plateLpState = new float[NumFdnLines];

	private readonly float[][] _hallBuf = new float[NumFdnLines][];
	private readonly int[] _hallPos = new int[NumFdnLines];
	private readonly float[] _hallLpState = new float[NumFdnLines];

	private double _lfoPhase;
	private double _modRandTarget = 1.0;
	private double _modRandCurr = 1.0;
	private uint _rng = 0x5A17C391u;
	private float _envAttackState;
	private bool _eqDirty = true;

	public static IReadOnlyList<NeoverbPreset> Presets { get; } = new[]
	{
		new NeoverbPreset { Name = "Neoverb 默认平衡", BlendX = 0.50, BlendY = 0.45, ReflectionsSpace = 31, PlateSpace = 1.10, HallSpace = 2.50, ModRateHz = 0.48, ModDepthPct = 25, PreDelayMs = 20, SmoothPct = 80, DryWetPct = 30, AutoCutEnabled = true },
		new NeoverbPreset { Name = "清澈人声 Plate", BlendX = 0.25, BlendY = 0.78, ReflectionsSpace = 24, PlateSpace = 1.45, HallSpace = 1.80, ModRateHz = 0.65, ModDepthPct = 32, PreDelayMs = 28, SmoothPct = 85, DryWetPct = 26, AutoCutEnabled = true },
		new NeoverbPreset { Name = "温润录音室 Room", BlendX = 0.55, BlendY = 0.25, ReflectionsSpace = 45, PlateSpace = 0.75, HallSpace = 0.95, HallFlavor = NeoverbHallFlavor.Room, ModRateHz = 0.30, ModDepthPct = 15, PreDelayMs = 10, SmoothPct = 60, DryWetPct = 22, AutoCutEnabled = true },
		new NeoverbPreset { Name = "金色音乐厅 Hall", BlendX = 0.78, BlendY = 0.82, ReflectionsSpace = 52, PlateSpace = 1.60, HallSpace = 3.40, HallFlavor = NeoverbHallFlavor.Hall, ModRateHz = 0.42, ModDepthPct = 38, PreDelayMs = 35, SmoothPct = 90, DryWetPct = 36, AutoCutEnabled = true },
		new NeoverbPreset { Name = "枫丹水殿穹顶", BlendX = 0.66, BlendY = 0.72, ReflectionsSpace = 68, PlateSpace = 2.20, HallSpace = 5.80, HallFlavor = NeoverbHallFlavor.Hall, ModRateHz = 0.55, ModDepthPct = 48, PreDelayMs = 45, SmoothPct = 95, DryWetPct = 42, AutoCutEnabled = true },
		new NeoverbPreset { Name = "复古木质室内乐 Chamber", BlendX = 0.62, BlendY = 0.58, ReflectionsSpace = 38, PlateSpace = 1.05, HallSpace = 1.65, HallFlavor = NeoverbHallFlavor.Chamber, ModRateHz = 0.35, ModDepthPct = 22, PreDelayMs = 16, SmoothPct = 70, DryWetPct = 28, AutoCutEnabled = true },
		new NeoverbPreset { Name = "氛围云海 Ambient Pad", BlendX = 0.50, BlendY = 0.92, ReflectionsSpace = 80, PlateSpace = 3.60, HallSpace = 7.50, HallFlavor = NeoverbHallFlavor.Hall, ModRateHz = 0.75, ModDepthPct = 65, PreDelayMs = 60, SmoothPct = 100, DryWetPct = 55, AutoCutEnabled = true },
		new NeoverbPreset { Name = "近场桌面自然声场", BlendX = 0.48, BlendY = 0.18, ReflectionsSpace = 22, PlateSpace = 0.55, HallSpace = 0.70, HallFlavor = NeoverbHallFlavor.Room, ModRateHz = 0.25, ModDepthPct = 12, PreDelayMs = 6, SmoothPct = 65, DryWetPct = 18, AutoCutEnabled = true }
	};

	public FurinaNeoverbProcessor()
	{
		for (int i = 0; i < 3; i++)
		{
			_preEqFilters[i] = new FurinaPlayerBiquadFilter { Enabled = true };
			_revEqFilters[i] = new FurinaPlayerBiquadFilter { Enabled = true };
		}
		for (int i = 0; i < NumFdnLines; i++)
		{
			_plateBuf[i] = new float[32768];
			_hallBuf[i] = new float[65536];
		}
		Prepare(44100.0);
	}

	public void Prepare(double sampleRate)
	{
		lock (_lock)
		{
			_sampleRate = Math.Max(8000.0, sampleRate);
			int maxPre = Math.Max(2048, (int)(_sampleRate * 0.30) + 64);
			if (_preDelayBufL.Length < maxPre)
			{
				_preDelayBufL = new float[maxPre];
				_preDelayBufR = new float[maxPre];
			}
			int maxRefl = Math.Max(4096, (int)(_sampleRate * 0.15) + 256);
			if (_reflBufL.Length < maxRefl)
			{
				_reflBufL = new float[maxRefl];
				_reflBufR = new float[maxRefl];
			}
			_eqDirty = true;
			RefreshFiltersLocked();
		}
	}

	public void Reset()
	{
		lock (_lock)
		{
			Array.Clear(_preDelayBufL, 0, _preDelayBufL.Length);
			Array.Clear(_preDelayBufR, 0, _preDelayBufR.Length);
			Array.Clear(_reflBufL, 0, _reflBufL.Length);
			Array.Clear(_reflBufR, 0, _reflBufR.Length);
			for (int i = 0; i < NumFdnLines; i++)
			{
				Array.Clear(_plateBuf[i], 0, _plateBuf[i].Length);
				Array.Clear(_hallBuf[i], 0, _hallBuf[i].Length);
				_plateLpState[i] = 0f;
				_hallLpState[i] = 0f;
			}
			_preDelayPos = 0;
			_reflPos = 0;
			_envAttackState = 0f;
			for (int i = 0; i < 3; i++)
			{
				_preEqFilters[i].Reset();
				_revEqFilters[i].Reset();
			}
			_autoCutFilter.Reset();
			_smoothLpFilter.Reset();
		}
	}

	public void MarkEqDirty() => _eqDirty = true;

	public void ApplyPreset(NeoverbPreset preset)
	{
		if (preset == null) return;
		BlendX = preset.BlendX;
		BlendY = preset.BlendY;
		ReflectionsSpace = preset.ReflectionsSpace;
		PlateSpace = preset.PlateSpace;
		HallSpace = preset.HallSpace;
		HallFlavor = preset.HallFlavor;
		ModMode = preset.ModMode;
		ModRateHz = preset.ModRateHz;
		ModDepthPct = preset.ModDepthPct;
		PreDelayMs = preset.PreDelayMs;
		SmoothPct = preset.SmoothPct;
		DryWetPct = preset.DryWetPct;
		LevelDb = preset.LevelDb;
		AutoCutEnabled = preset.AutoCutEnabled;
		_eqDirty = true;
	}

	/// <summary>
	/// Reverb Assistant 智能辅助建议：根据风格（Vocal / Acoustic / Hall / Ambient / Tight）自动计算混合权重、衰减与 EQ。
	/// </summary>
	public void ApplyReverbAssistant(string styleKey, string sizeKey = "medium", string toneKey = "clean")
	{
		switch ((styleKey ?? "vocal").Trim().ToLowerInvariant())
		{
			case "vocal":
				BlendX = 0.30; BlendY = 0.72; ReflectionsSpace = 26; PlateSpace = 1.35; HallSpace = 1.9;
				PreDelayMs = 26; SmoothPct = 85; AutoCutEnabled = true;
				ReverbEqGainsDb[0] = -3.5; ReverbEqGainsDb[1] = -0.5; ReverbEqGainsDb[2] = -1.5;
				break;
			case "acoustic":
				BlendX = 0.56; BlendY = 0.42; ReflectionsSpace = 40; PlateSpace = 0.95; HallSpace = 1.45;
				HallFlavor = NeoverbHallFlavor.Room; PreDelayMs = 14; SmoothPct = 70; AutoCutEnabled = true;
				ReverbEqGainsDb[0] = -2.0; ReverbEqGainsDb[1] = 0.0; ReverbEqGainsDb[2] = 0.5;
				break;
			case "ambient":
				BlendX = 0.52; BlendY = 0.90; ReflectionsSpace = 75; PlateSpace = 3.20; HallSpace = 6.80;
				HallFlavor = NeoverbHallFlavor.Hall; PreDelayMs = 45; SmoothPct = 100; AutoCutEnabled = true;
				ReverbEqGainsDb[0] = -4.0; ReverbEqGainsDb[1] = 0.0; ReverbEqGainsDb[2] = 1.2;
				break;
			case "tight":
				BlendX = 0.48; BlendY = 0.16; ReflectionsSpace = 20; PlateSpace = 0.50; HallSpace = 0.65;
				HallFlavor = NeoverbHallFlavor.Room; PreDelayMs = 6; SmoothPct = 55; AutoCutEnabled = true;
				ReverbEqGainsDb[0] = -2.5; ReverbEqGainsDb[1] = 0.0; ReverbEqGainsDb[2] = -2.0;
				break;
			default: // concert / hall
				BlendX = 0.74; BlendY = 0.80; ReflectionsSpace = 50; PlateSpace = 1.55; HallSpace = 3.10;
				HallFlavor = NeoverbHallFlavor.Hall; PreDelayMs = 30; SmoothPct = 88; AutoCutEnabled = true;
				ReverbEqGainsDb[0] = -3.0; ReverbEqGainsDb[1] = -0.5; ReverbEqGainsDb[2] = -1.0;
				break;
		}

		double sizeMult = (sizeKey ?? "medium").ToLowerInvariant() switch
		{
			"small" => 0.65,
			"large" => 1.45,
			_ => 1.0
		};
		PlateSpace = Math.Clamp(PlateSpace * sizeMult, 0.25, 6.0);
		HallSpace = Math.Clamp(HallSpace * sizeMult, 0.45, 12.0);
		DryWetPct = sizeKey == "large" ? 36.0 : (sizeKey == "small" ? 20.0 : 28.0);

		if (string.Equals(toneKey, "warm", StringComparison.OrdinalIgnoreCase))
		{
			ReverbEqGainsDb[0] += 1.0;
			ReverbEqGainsDb[2] -= 3.5;
		}
		else if (string.Equals(toneKey, "bright", StringComparison.OrdinalIgnoreCase))
		{
			ReverbEqGainsDb[0] -= 1.5;
			ReverbEqGainsDb[2] += 2.5;
		}

		Enabled = true;
		_eqDirty = true;
	}

	/// <summary>
	/// 计算三角混合垫三个顶点（Top: Reflections, BottomLeft: Plate, BottomRight: Hall）的归一化权重。
	/// </summary>
	public (double ReflWeight, double PlateWeight, double HallWeight) GetBlendWeights()
	{
		double y = Math.Clamp(BlendY, 0.0, 1.0);
		double x = Math.Clamp(BlendX, 0.0, 1.0);
		// Triangle vertices: T=(0.5, 0), BL=(0, 1), BR=(1, 1)
		double wRefl = Math.Clamp(1.0 - y, 0.0, 1.0);
		double bottomSpan = y;
		double wHall = Math.Clamp(bottomSpan * x, 0.0, 1.0);
		double wPlate = Math.Clamp(bottomSpan * (1.0 - x), 0.0, 1.0);
		double sum = Math.Max(1e-6, wRefl + wPlate + wHall);
		return (wRefl / sum, wPlate / sum, wHall / sum);
	}

	private void RefreshFiltersLocked()
	{
		_preEqFilters[0].FilterType = FurinaPlayerFilterType.LowShelf;
		_preEqFilters[1].FilterType = FurinaPlayerFilterType.Peaking;
		_preEqFilters[2].FilterType = FurinaPlayerFilterType.HighShelf;

		_revEqFilters[0].FilterType = FurinaPlayerFilterType.LowShelf;
		_revEqFilters[1].FilterType = FurinaPlayerFilterType.Peaking;
		_revEqFilters[2].FilterType = FurinaPlayerFilterType.HighShelf;

		for (int i = 0; i < 3; i++)
		{
			_preEqFilters[i].FrequencyHz = Math.Clamp(PreEqFreqs[i], 20.0, _sampleRate * 0.45);
			_preEqFilters[i].GainDb = Math.Clamp(PreEqGainsDb[i], -24.0, 12.0);
			_preEqFilters[i].Q = Math.Clamp(PreEqQs[i], 0.25, 8.0);
			_preEqFilters[i].UpdateCoefficients(_sampleRate);

			_revEqFilters[i].FrequencyHz = Math.Clamp(ReverbEqFreqs[i], 20.0, _sampleRate * 0.45);
			_revEqFilters[i].GainDb = Math.Clamp(ReverbEqGainsDb[i], -24.0, 12.0);
			_revEqFilters[i].Q = Math.Clamp(ReverbEqQs[i], 0.25, 8.0);
			_revEqFilters[i].UpdateCoefficients(_sampleRate);
		}

		_autoCutFilter.FilterType = FurinaPlayerFilterType.Peaking;
		_autoCutFilter.FrequencyHz = 310.0;
		_autoCutFilter.GainDb = -4.2;
		_autoCutFilter.Q = 1.15;
		_autoCutFilter.Enabled = true;
		_autoCutFilter.UpdateCoefficients(_sampleRate);

		double smoothNorm = Math.Clamp(SmoothPct / 100.0, 0.0, 1.0);
		_smoothLpFilter.FilterType = FurinaPlayerFilterType.LowPass;
		_smoothLpFilter.FrequencyHz = 14000.0 - smoothNorm * 7500.0;
		_smoothLpFilter.Q = 0.707;
		_smoothLpFilter.Enabled = true;
		_smoothLpFilter.UpdateCoefficients(_sampleRate);

		_eqDirty = false;
	}

	public void ProcessInterleaved(float[] buffer, int offset, int count, int channels)
	{
		if (!Enabled || count <= 0 || channels <= 0) return;

		lock (_lock)
		{
			if (_eqDirty) RefreshFiltersLocked();

			double wet = Math.Clamp(DryWetPct / 100.0, 0.0, 1.0);
			if (wet <= 0.0005) return;
			double dry = 1.0 - wet * 0.55;
			double levelGain = Math.Pow(10.0, Math.Clamp(LevelDb, -24.0, 6.0) / 20.0);

			var (wRefl, wPlate, wHall) = GetBlendWeights();

			double srRatio = _sampleRate / 44100.0;
			int preDelaySamples = Math.Clamp((int)(PreDelayMs * 0.001 * _sampleRate), 0, _preDelayBufL.Length - 2);

			double reflScale = 0.45 + Math.Clamp(ReflectionsSpace / 100.0, 0.0, 1.0) * 1.15;
			double plateDecay = Math.Clamp(PlateSpace, 0.2, 6.0);
			double hallDecay = Math.Clamp(HallSpace, 0.4, 12.0);
			double flavorScale = HallFlavor switch
			{
				NeoverbHallFlavor.Room => 0.62,
				NeoverbHallFlavor.Chamber => 0.80,
				_ => 1.05
			};

			double avgPlateLen = 2088.0 * srRatio;
			float plateFb = (float)Math.Clamp(Math.Pow(0.001, avgPlateLen / (plateDecay * _sampleRate)), 0.35, 0.965);
			double avgHallLen = 2984.0 * srRatio * flavorScale;
			float hallFb = (float)Math.Clamp(Math.Pow(0.001, avgHallLen / (hallDecay * _sampleRate)), 0.40, 0.978);

			double modDepth = Math.Clamp(ModDepthPct / 100.0, 0.0, 1.0) * 14.0 * srRatio;
			double modInc = 2.0 * Math.PI * Math.Clamp(ModRateHz, 0.05, 5.0) / _sampleRate;
			double smoothNorm = Math.Clamp(SmoothPct / 100.0, 0.0, 1.0);

			int preLen = _preDelayBufL.Length;
			int reflLen = _reflBufL.Length;

			for (int i = offset; i < offset + count; i += channels)
			{
				float inL = buffer[i];
				float inR = channels > 1 && i + 1 < offset + count ? buffer[i + 1] : inL;

				// 1. Pre EQ + Auto Cut on reverb send
				float sendL = inL;
				float sendR = inR;
				if (PreEqEnabled)
				{
					for (int b = 0; b < 3; b++)
					{
						sendL = _preEqFilters[b].ProcessSample(sendL, 0); sendR = _preEqFilters[b].ProcessSample(sendR, 1);
					}
				}
				if (AutoCutEnabled)
				{
					sendL = _autoCutFilter.ProcessSample(sendL, 0); sendR = _autoCutFilter.ProcessSample(sendR, 1);
				}

				// 2. Smooth transient ducking/softening on reverb input
				if (smoothNorm > 0.01)
				{
					float peak = Math.Max(Math.Abs(sendL), Math.Abs(sendR));
					_envAttackState = _envAttackState * 0.992f + peak * 0.008f;
					float crest = peak / Math.Max(0.002f, _envAttackState);
					if (crest > 2.2f)
					{
						float soft = 1.0f / (1.0f + (float)smoothNorm * 0.35f * (crest - 2.2f));
						sendL *= soft;
						sendR *= soft;
					}
					sendL = _smoothLpFilter.ProcessSample(sendL, 0); sendR = _smoothLpFilter.ProcessSample(sendR, 1);
				}

				// 3. Pre-Delay
				_preDelayBufL[_preDelayPos] = sendL;
				_preDelayBufR[_preDelayPos] = sendR;
				int readPd = _preDelayPos - preDelaySamples;
				if (readPd < 0) readPd += preLen;
				float pdL = _preDelayBufL[readPd];
				float pdR = _preDelayBufR[readPd];
				_preDelayPos = (_preDelayPos + 1) % preLen;

				// Modulation LFO
				_lfoPhase += modInc;
				if (_lfoPhase >= 2.0 * Math.PI)
				{
					_lfoPhase -= 2.0 * Math.PI;
					_rng ^= _rng << 13;
					_rng ^= _rng >> 17;
					_rng ^= _rng << 5;
					_modRandTarget = ((_rng & 0xFFFF) / 32768.0) - 1.0;
				}
				_modRandCurr = _modRandCurr * 0.9992 + _modRandTarget * 0.0008;
				double lfoVal = ModMode == NeoverbModMode.Sine ? Math.Sin(_lfoPhase) : _modRandCurr;

				// 4. Engine A: Early Reflections (multi-tap)
				_reflBufL[_reflPos] = pdL;
				_reflBufR[_reflPos] = pdR;
				float reflOutL = 0f, reflOutR = 0f;
				for (int t = 0; t < ReflTapsL.Length; t++)
				{
					int offL = (int)(ReflTapsL[t] * srRatio * reflScale);
					int offR = (int)(ReflTapsR[t] * srRatio * reflScale);
					int idxL = (_reflPos - offL) % reflLen; if (idxL < 0) idxL += reflLen;
					int idxR = (_reflPos - offR) % reflLen; if (idxR < 0) idxR += reflLen;
					float g = 0.24f / (1f + t * 0.28f);
					reflOutL += _reflBufL[idxL] * g;
					reflOutR += _reflBufR[idxR] * g;
				}
				_reflPos = (_reflPos + 1) % reflLen;

				// 5. Engine B: Plate 4x4 Hadamard FDN
				float plateTap0 = ReadFdnLine(_plateBuf[0], _platePos[0], (int)(PlatePrimeSamples[0] * srRatio));
				float plateTap1 = ReadFdnLine(_plateBuf[1], _platePos[1], (int)(PlatePrimeSamples[1] * srRatio));
				float plateTap2 = ReadFdnLine(_plateBuf[2], _platePos[2], (int)(PlatePrimeSamples[2] * srRatio + lfoVal * modDepth * 0.6));
				float plateTap3 = ReadFdnLine(_plateBuf[3], _platePos[3], (int)(PlatePrimeSamples[3] * srRatio - lfoVal * modDepth * 0.6));
				HadamardMix(pdL * 0.35f, pdR * 0.35f, plateFb, 0.28f,
					ref plateTap0, ref plateTap1, ref plateTap2, ref plateTap3,
					_plateBuf, _platePos, _plateLpState);
				float plateOutL = (plateTap0 + plateTap2) * 0.52f;
				float plateOutR = (plateTap1 + plateTap3) * 0.52f;

				// 6. Engine C: Hall / Room / Chamber 4x4 Hadamard FDN
				float hallTap0 = ReadFdnLine(_hallBuf[0], _hallPos[0], (int)(HallPrimeSamples[0] * srRatio * flavorScale + lfoVal * modDepth));
				float hallTap1 = ReadFdnLine(_hallBuf[1], _hallPos[1], (int)(HallPrimeSamples[1] * srRatio * flavorScale - lfoVal * modDepth * 0.8));
				float hallTap2 = ReadFdnLine(_hallBuf[2], _hallPos[2], (int)(HallPrimeSamples[2] * srRatio * flavorScale));
				float hallTap3 = ReadFdnLine(_hallBuf[3], _hallPos[3], (int)(HallPrimeSamples[3] * srRatio * flavorScale + lfoVal * modDepth * 0.5));
				HadamardMix(pdL * 0.35f, pdR * 0.35f, hallFb, 0.38f,
					ref hallTap0, ref hallTap1, ref hallTap2, ref hallTap3,
					_hallBuf, _hallPos, _hallLpState);
				float hallOutL = (hallTap0 - hallTap3) * 0.52f;
				float hallOutR = (hallTap1 + hallTap2) * 0.52f;

				// 7. Triangular Blend Pad mix
				float wetL = (float)(reflOutL * wRefl + plateOutL * wPlate + hallOutL * wHall);
				float wetR = (float)(reflOutR * wRefl + plateOutR * wPlate + hallOutR * wHall);

				// 8. Reverb EQ shaping on wet signal
				if (ReverbEqEnabled)
				{
					for (int b = 0; b < 3; b++)
					{
						wetL = _revEqFilters[b].ProcessSample(wetL, 0); wetR = _revEqFilters[b].ProcessSample(wetR, 1);
					}
				}

				float outL = (float)((inL * dry + wetL * wet) * levelGain);
				float outR = (float)((inR * dry + wetR * wet) * levelGain);
				buffer[i] = Math.Clamp(outL, -1.5f, 1.5f);
				if (channels > 1 && i + 1 < offset + count)
				{
					buffer[i + 1] = Math.Clamp(outR, -1.5f, 1.5f);
				}
			}
		}
	}

	private static float ReadFdnLine(float[] line, int writePos, int delaySamples)
	{
		int len = line.Length;
		int d = Math.Clamp(delaySamples, 8, len - 2);
		int r = writePos - d;
		if (r < 0) r += len;
		return line[r];
	}

	private static void HadamardMix(
		float inL, float inR, float fb, float damp,
		ref float t0, ref float t1, ref float t2, ref float t3,
		float[][] bufs, int[] pos, float[] lp)
	{
		lp[0] = lp[0] * damp + t0 * (1f - damp);
		lp[1] = lp[1] * damp + t1 * (1f - damp);
		lp[2] = lp[2] * damp + t2 * (1f - damp);
		lp[3] = lp[3] * damp + t3 * (1f - damp);

		float x0 = lp[0], x1 = lp[1], x2 = lp[2], x3 = lp[3];
		const float inv2 = 0.5f;
		float m0 = (x0 + x1 + x2 + x3) * inv2 * fb;
		float m1 = (x0 - x1 + x2 - x3) * inv2 * fb;
		float m2 = (x0 + x1 - x2 - x3) * inv2 * fb;
		float m3 = (x0 - x1 - x2 + x3) * inv2 * fb;

		WriteLine(bufs[0], ref pos[0], m0 + inL);
		WriteLine(bufs[1], ref pos[1], m1 + inR);
		WriteLine(bufs[2], ref pos[2], m2 + inL);
		WriteLine(bufs[3], ref pos[3], m3 + inR);
	}

	private static void WriteLine(float[] line, ref int pos, float val)
	{
		line[pos] = Math.Clamp(val, -2f, 2f);
		pos = (pos + 1) % line.Length;
	}
}
