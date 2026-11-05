using System;
using System.Collections.Generic;

namespace SonicWave.Audio.Dsp;

public enum SdmOrder
{
	Order3_Safe = 3,
	Order5_Precision = 5,
	Order5_HiFi = 5,
	Order7_Reference = 7
}

public enum SdmTargetRate
{
	Dsd64 = 64,     // 2.8224 MHz (64x 44.1k)
	Dsd128 = 128,   // 5.6448 MHz (128x 44.1k)
	Dsd256 = 256,   // 11.2896 MHz (256x 44.1k)
	Dsd512 = 512    // 22.5792 MHz (512x 44.1k)
}

public enum SdmMode
{
	Off = 0,
	DsdPassthrough = 1,
	PcmToSdm = 2
}

public enum SdmQuantizerStrategy
{
	Off = 0,
	Dither = 1,
	Adaptive = 2
}

public enum SdmSoundProfile
{
	FurinaPlayerLinear = 0,
	WarmVintage = 1,
	AnalogDirect = 2
}

public enum SdmFirTier
{
	Standard = 0,
	Steep = 1,
	MinimumPhase = 2
}

public enum SdmComputeEngine
{
	CpuSdm = 0,
	CpuAvx2 = 1,
	GpuAccelerated = 2
}

/// <summary>
/// FurinaPlayer SDM / DSD 调制器与 DoP 传输引擎。
/// 提供高精度 5 阶 Δ-Σ 实时噪声整形、DoP (0x05 / 0xFA) 封装与硬件直通。
/// </summary>
public class FurinaPlayerSdmModulator
{
	private bool _enabled = false;
	private SdmMode _mode = SdmMode.Off;
	private SdmOrder _order = SdmOrder.Order5_Precision;
	private SdmTargetRate _targetRate = SdmTargetRate.Dsd128;
	private SdmQuantizerStrategy _quantizerStrategy = SdmQuantizerStrategy.Off;
	private SdmSoundProfile _soundProfile = SdmSoundProfile.FurinaPlayerLinear;
	private SdmFirTier _firTier = SdmFirTier.Standard;
	private SdmComputeEngine _computeEngine = SdmComputeEngine.CpuSdm;
	private bool _dopPassthrough = true;
	private bool _dsd512PreModulation = false;

	private double[] _feedbackCoeffs = Array.Empty<double>();
	private double[] _feedbackDenominatorCoeffs = Array.Empty<double>();

	// Integrator and error states per channel [channel, order]
	private readonly double[][] _integrators = new double[2][];
	private readonly double[][] _errors = new double[2][];
	private uint _rngState = 0x6d2b79f5u;
	private int _stabilityRecoveries = 0;
	private double _peakFeedbackState = 0.0;

	// Telemetry & stability properties
	public double PoleRadius { get; private set; } = 0.8842;
	public double PeakGain { get; private set; } = 1.55;
	public double RecommendedHeadroomDb { get; private set; } = 3.0;
	public double InputLimit { get; private set; } = 0.950;
	public double DitherAmplitude { get; private set; } = 1.0e-7;
	public int StabilityRecoveries => _stabilityRecoveries;
	public double PeakFeedbackState => _peakFeedbackState;

	public IReadOnlyList<double> FeedbackCoefficients => _feedbackCoeffs;
	public IReadOnlyList<double> FeedbackDenominatorCoefficients => _feedbackDenominatorCoeffs;

	public bool Enabled
	{
		get => _enabled;
		set => _enabled = value;
	}

	public SdmMode Mode
	{
		get => _mode;
		set
		{
			_mode = value;
			_enabled = value != SdmMode.Off;
		}
	}

	public SdmOrder Order
	{
		get => _order;
		set
		{
			_order = value;
			RecomputeCoefficients();
		}
	}

	public SdmTargetRate TargetRate
	{
		get => _targetRate;
		set => _targetRate = value;
	}

	public SdmQuantizerStrategy QuantizerStrategy
	{
		get => _quantizerStrategy;
		set => _quantizerStrategy = value;
	}

	public SdmSoundProfile SoundProfile
	{
		get => _soundProfile;
		set => _soundProfile = value;
	}

	public SdmFirTier FirTier
	{
		get => _firTier;
		set => _firTier = value;
	}

	public SdmComputeEngine ComputeEngine
	{
		get => _computeEngine;
		set => _computeEngine = value;
	}

	public bool DopPassthrough
	{
		get => _dopPassthrough;
		set => _dopPassthrough = value;
	}

	public bool Dsd512PreModulation
	{
		get => _dsd512PreModulation;
		set => _dsd512PreModulation = value;
	}

	public FurinaPlayerSdmModulator()
	{
		for (int ch = 0; ch < 2; ch++)
		{
			_integrators[ch] = new double[7];
			_errors[ch] = new double[7];
		}
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

	public void RecomputeCoefficients()
	{
		int order = (int)_order;
		PeakGain = _order switch
		{
			SdmOrder.Order3_Safe => 1.45,
			SdmOrder.Order5_Precision => 1.55,
			_ => 1.60
		};

		PoleRadius = 2.0 / Math.Pow(PeakGain, 1.0 / order) - 1.0;
		_feedbackCoeffs = new double[order];
		_feedbackDenominatorCoeffs = new double[order];

		for (int i = 0; i < order; i++)
		{
			int tap = i + 1;
			double denom = Binomial(order, tap) * Math.Pow(-PoleRadius, tap);
			_feedbackDenominatorCoeffs[i] = denom;
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
		_stabilityRecoveries = 0;
		_peakFeedbackState = 0.0;
		for (int ch = 0; ch < 2; ch++)
		{
			if (_integrators[ch] != null) Array.Clear(_integrators[ch], 0, _integrators[ch].Length);
			if (_errors[ch] != null) Array.Clear(_errors[ch], 0, _errors[ch].Length);
		}
	}

	private double NextDither()
	{
		if (_quantizerStrategy == SdmQuantizerStrategy.Off) return 0.0;
		_rngState = _rngState * 1664525u + 1013904223u;
		double r1 = ((_rngState >> 16) / 65536.0 - 0.5);
		_rngState = _rngState * 1664525u + 1013904223u;
		double r2 = ((_rngState >> 16) / 65536.0 - 0.5);
		// TPDF dither
		return (r1 + r2) * DitherAmplitude;
	}

	/// <summary>
	/// 计算给定频率下的噪声传输函数 (Noise Transfer Function, NTF) 增益 (dB)
	/// </summary>
	public double ComputeNtfDb(double freqHz, double sampleRate)
	{
		double f = Math.Max(1.0, sampleRate);
		double p = 2.0 * Math.PI * Math.Max(0.0, freqHz) / f;
		int order = (int)_order;
		double realDenom = 1.0;
		double imagDenom = 0.0;
		double realNumFeedback = 0.0;
		double imagNumFeedback = 0.0;

		for (int i = 0; i < order; i++)
		{
			double u = p * (i + 1);
			double cosU = Math.Cos(u);
			double sinU = -Math.Sin(u);

			double d = i < _feedbackDenominatorCoeffs.Length ? _feedbackDenominatorCoeffs[i] : 0.0;
			double c = i < _feedbackCoeffs.Length ? _feedbackCoeffs[i] : 0.0;

			realDenom += d * cosU;
			imagDenom += d * sinU;
			realNumFeedback += c * cosU;
			imagNumFeedback += c * sinU;
		}

		double denomMag = Math.Max(1e-15, Math.Sqrt(realDenom * realDenom + imagDenom * imagDenom));
		double numReal = realDenom - realNumFeedback;
		double numImag = imagDenom - imagNumFeedback;
		double numMag = Math.Sqrt(numReal * numReal + numImag * numImag);

		double mag = numMag / denomMag;
		return 20.0 * Math.Log10(Math.Max(1e-12, mag));
	}

	/// <summary>
	/// 将一个 PCM 采样调制为 1-bit DSD (-1 或 +1)
	/// </summary>
	public int ModulateSample(double inputSample, int channel)
	{
		int ch = Math.Min(channel, 1);
		int order = (int)_order;

		// 声音风格音色微调
		double x = inputSample;
		if (_soundProfile == SdmSoundProfile.WarmVintage)
		{
			// 温润微量二次谐波模拟质感
			x = x * 0.98 + 0.02 * (x * x * Math.Sign(x));
		}

		// 输入保护限幅
		double clampedInput = Math.Clamp(x, -InputLimit, InputLimit);

		// 循环滤波累加
		double loopFilter = 0.0;
		for (int i = 0; i < order; i++)
		{
			loopFilter += _feedbackCoeffs[i] * _errors[ch][i];
		}

		// 记录峰值状态反馈
		double absLoop = Math.Abs(loopFilter);
		if (absLoop > _peakFeedbackState) _peakFeedbackState = absLoop;

		// 稳定性卫兵 (Stability Guard)：若积分器发生溢出漂移则触发自恢复
		if (absLoop > 1.55)
		{
			_stabilityRecoveries++;
			Array.Clear(_errors[ch], 0, order);
			loopFilter = 0.0;
		}

		double accum = clampedInput + loopFilter + NextDither();

		// 1-bit 量化器
		int bit = accum >= 0.0 ? 1 : -1;
		double quantError = bit - accum;

		// 更新历史量化误差
		for (int i = order - 1; i > 0; i--)
		{
			_errors[ch][i] = _errors[ch][i - 1];
		}
		_errors[ch][0] = quantError;

		return bit;
	}

	/// <summary>
	/// 将交织 PCM 数据块编码为标准 DoP (DSD over PCM) 格式 (24-bit，带标准 0x05 / 0xFA 标记)
	/// </summary>
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
				// 格式：[Marker:8][DSD:16]
				uint dopSample = ((uint)dopMarker << 16) | dsd16Bits;
				dopOutput[f * channels + ch] = dopSample;
			}
		}

		return dopOutput;
	}

	/// <summary>
	/// 将原生 DSD 位流封装进 DoP 帧 (Passthrough 直通模式)
	/// </summary>
	public uint[] EncodeDsdPassthroughToDop(ushort[] interleavedDsdWords, int count, int channels)
	{
		uint[] dopOutput = new uint[count];
		byte dopMarker = 0x05;
		int frames = count / channels;

		for (int f = 0; f < frames; f++)
		{
			dopMarker = (byte)(dopMarker == 0x05 ? 0xFA : 0x05);
			for (int ch = 0; ch < channels; ch++)
			{
				ushort dsd16 = interleavedDsdWords[f * channels + ch];
				dopOutput[f * channels + ch] = ((uint)dopMarker << 16) | dsd16;
			}
		}

		return dopOutput;
	}
}
