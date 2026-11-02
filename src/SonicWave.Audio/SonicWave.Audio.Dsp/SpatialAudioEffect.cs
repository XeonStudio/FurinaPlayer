using System;

namespace SonicWave.Audio.Dsp;

/// <summary>
/// Professional High-Fidelity Spatial Audio Processor.
/// Combines Bauer Stereophonic-to-Binaural (bs2b) Crossfeed, Linear-Phase Mid-Side Stereo Expansion,
/// and Natural Acoustic Diffusion / Early Reflections for a transparent, immersive 3D soundstage.
/// </summary>
public sealed class SpatialAudioEffect
{
	private readonly int _sampleRate;
	private double _roomSize = 0.6;
	private double _damping = 0.45;
	private double _wet = 0.25;
	private double _dry = 1.0;
	private double _predelayMs = 25.0;
	private double _width = 0.6;
	private double _modulation = 0.25;

	// Crossfeed Filter states
	private double _crossfeedL2R;
	private double _crossfeedR2L;
	private double _crossAlpha;
	private double _crossfeedGain = 0.35;

	// Early reflections buffer
	private float[] _earlyBufL;
	private float[] _earlyBufR;
	private int _earlyIdx;
	private int _earlyLen;

	// Micro-diffusion all-pass filters
	private readonly float[] _apBuf1L;
	private readonly float[] _apBuf1R;
	private readonly float[] _apBuf2L;
	private readonly float[] _apBuf2R;
	private int _apIdx1L, _apIdx1R, _apIdx2L, _apIdx2R;

	public int SampleRate => _sampleRate;

	public double RoomSize
	{
		get => _roomSize;
		set
		{
			_roomSize = Math.Clamp(value, 0.0, 1.0);
			ApplyParameters();
		}
	}

	public double Damping
	{
		get => _damping;
		set
		{
			_damping = Math.Clamp(value, 0.0, 1.0);
			ApplyParameters();
		}
	}

	public double WetMix
	{
		get => _wet;
		set
		{
			_wet = Math.Clamp(value, 0.0, 1.0);
			_dry = Math.Sqrt(Math.Max(0.0, 1.0 - _wet * 0.7)); // Equal power crossfade
		}
	}

	public double PredelayMs
	{
		get => _predelayMs;
		set
		{
			_predelayMs = Math.Clamp(value, 0.0, 120.0);
			SetPredelayMs(_predelayMs);
		}
	}

	public double Width
	{
		get => _width;
		set
		{
			_width = Math.Clamp(value, 0.0, 1.5);
		}
	}

	public double Modulation
	{
		get => _modulation;
		set
		{
			_modulation = Math.Clamp(value, 0.0, 1.0);
		}
	}

	public static string[] Presets => new string[5] { "大厅", "板式", "房间", "教堂", "空间" };

	public SpatialAudioEffect(int sampleRate = 44100)
	{
		_sampleRate = (sampleRate > 0) ? sampleRate : 44100;

		// Allocate early reflection & all-pass diffusion buffers
		_earlyLen = Math.Max(128, (int)(_sampleRate * 0.15)); // 150ms max delay
		_earlyBufL = new float[_earlyLen];
		_earlyBufR = new float[_earlyLen];

		// Prime sample delays for smooth, metallic-free diffusion
		_apBuf1L = new float[Math.Max(16, (int)(_sampleRate * 0.0047))]; // 4.7ms
		_apBuf1R = new float[Math.Max(16, (int)(_sampleRate * 0.0051))]; // 5.1ms
		_apBuf2L = new float[Math.Max(16, (int)(_sampleRate * 0.0089))]; // 8.9ms
		_apBuf2R = new float[Math.Max(16, (int)(_sampleRate * 0.0097))]; // 9.7ms

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
				WetMix = 0.30;
				PredelayMs = 28.0;
				Width = 0.85;
				Modulation = 0.25;
				break;
			case "板式":
				RoomSize = 0.5;
				Damping = 0.5;
				WetMix = 0.22;
				PredelayMs = 12.0;
				Width = 0.65;
				Modulation = 0.35;
				break;
			case "房间":
				RoomSize = 0.4;
				Damping = 0.6;
				WetMix = 0.18;
				PredelayMs = 16.0;
				Width = 0.50;
				Modulation = 0.15;
				break;
			case "教堂":
				RoomSize = 0.95;
				Damping = 0.2;
				WetMix = 0.38;
				PredelayMs = 45.0;
				Width = 1.0;
				Modulation = 0.2;
				break;
			case "空间":
			default:
				RoomSize = 0.6;
				Damping = 0.45;
				WetMix = 0.25;
				PredelayMs = 20.0;
				Width = 0.75;
				Modulation = 0.20;
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
			"板式" => new SpatialParameters(0.65, 0.35, 12.0),
			"房间" => new SpatialParameters(0.50, 0.15, 16.0),
			"教堂" => new SpatialParameters(1.00, 0.20, 45.0),
			"空间" => new SpatialParameters(0.75, 0.20, 20.0),
			_ => new SpatialParameters(0.85, 0.25, 28.0),
		};
	}

	private void SetPredelayMs(double ms)
	{
		_predelayMs = Math.Clamp(ms, 0.0, 120.0);
	}

	private void ApplyParameters()
	{
		// Bauer crossfeed filter cutoff around 700Hz
		double crossfeedCutoffHz = 650.0 + (1.0 - _damping) * 200.0;
		_crossAlpha = 1.0 - Math.Exp(-2.0 * Math.PI * crossfeedCutoffHz / _sampleRate);
		_crossfeedGain = 0.25 + _roomSize * 0.25;
	}

	public void Process(float[] samples)
	{
		Process(samples, samples.Length);
	}

	public void Process(float[] samples, int count)
	{
		if (_wet <= 0.001 || samples == null) return;
		int num = Math.Min(count, samples.Length);
		for (int i = 0; i < num; i++)
		{
			samples[i] = SoftLimit(samples[i]);
		}
	}

	public void ProcessInterleaved(float[] samples, int channels)
	{
		if (channels <= 0 || _wet <= 0.001 || samples == null || samples.Length < channels * 2)
		{
			return;
		}

		if (channels == 1)
		{
			Process(samples, samples.Length);
			return;
		}

		int frames = samples.Length / channels;
		double widthMult = 1.0 + _width * 0.9; // 1.0 (natural) to ~2.35 (wide)
		double wetGain = _wet;
		double dryGain = _dry;

		int predelaySamples = Math.Max(1, (int)(_sampleRate * _predelayMs / 1000.0));
		predelaySamples = Math.Min(predelaySamples, _earlyLen - 64);

		for (int f = 0; f < frames; f++)
		{
			int idxL = f * channels;
			int idxR = idxL + 1;

			double inL = samples[idxL];
			double inR = samples[idxR];

			// 1. Mid-Side Linear Decomposition
			double mid = 0.5 * (inL + inR);
			double side = 0.5 * (inL - inR);

			// Apply smooth stereo field width expansion
			double wideSide = side * widthMult;
			double wideL = mid + wideSide;
			double wideR = mid - wideSide;

			// 2. Headphone Bauer Crossfeed (eliminates in-the-head isolation, natural outward soundstage)
			_crossfeedL2R += _crossAlpha * (wideL - _crossfeedL2R);
			_crossfeedR2L += _crossAlpha * (wideR - _crossfeedR2L);

			double crossL = wideL + _crossfeedGain * (_crossfeedR2L - _crossfeedL2R);
			double crossR = wideR + _crossfeedGain * (_crossfeedL2R - _crossfeedR2L);

			// 3. Early reflection & Micro-diffusion line
			int writeIdx = _earlyIdx;
			_earlyBufL[writeIdx] = (float)crossL;
			_earlyBufR[writeIdx] = (float)crossR;

			int readIdx = (writeIdx - predelaySamples + _earlyLen) % _earlyLen;
			double delayedL = _earlyBufL[readIdx];
			double delayedR = _earlyBufR[readIdx];
			_earlyIdx = (writeIdx + 1) % _earlyLen;

			// Micro-diffuser stage 1 (all-pass)
			double diff1L = AllPass(delayedL, _apBuf1L, ref _apIdx1L, 0.45);
			double diff1R = AllPass(delayedR, _apBuf1R, ref _apIdx1R, 0.45);

			// Micro-diffuser stage 2 (all-pass)
			double diff2L = AllPass(diff1L, _apBuf2L, ref _apIdx2L, 0.35);
			double diff2R = AllPass(diff1R, _apBuf2R, ref _apIdx2R, 0.35);

			// 4. Spatial Synthesis & Blend
			double wetL = crossL * 0.65 + diff2L * 0.35;
			double wetR = crossR * 0.65 + diff2R * 0.35;

			double outL = inL * dryGain + wetL * wetGain;
			double outR = inR * dryGain + wetR * wetGain;

			samples[idxL] = SoftLimit(outL);
			samples[idxR] = SoftLimit(outR);
		}
	}

	private static double AllPass(double input, float[] buf, ref int idx, double feedback)
	{
		int len = buf.Length;
		double bufOut = buf[idx];
		double output = -input + bufOut;
		buf[idx] = (float)(input + bufOut * feedback);
		idx = (idx + 1) % len;
		return output;
	}

	public void Reset()
	{
		_crossfeedL2R = 0.0;
		_crossfeedR2L = 0.0;
		Array.Clear(_earlyBufL);
		Array.Clear(_earlyBufR);
		Array.Clear(_apBuf1L);
		Array.Clear(_apBuf1R);
		Array.Clear(_apBuf2L);
		Array.Clear(_apBuf2R);
		_earlyIdx = 0;
		_apIdx1L = 0;
		_apIdx1R = 0;
		_apIdx2L = 0;
		_apIdx2R = 0;
	}

	private static float SoftLimit(double x)
	{
		if (x > 1.0)
		{
			x = 1.0 + (x - 1.0) / (1.0 + (x - 1.0) * 0.5);
		}
		else if (x < -1.0)
		{
			x = -1.0 + (x + 1.0) / (1.0 - (x + 1.0) * 0.5);
		}
		return (float)Math.Clamp(x, -1.05, 1.05);
	}
}
