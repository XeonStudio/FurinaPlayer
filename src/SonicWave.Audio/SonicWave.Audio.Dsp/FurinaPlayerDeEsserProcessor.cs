using System;

namespace SonicWave.Audio.Dsp;

/// <summary>
/// FurinaPlayer 专业级动态去齿音 (De-Esser) 处理器。
/// 支持检测带中心频率 (2k~12kHz)、Q值/带宽调节、软拐点动态压缩比率、最大衰减限幅、
/// 超快启动(0.1~20ms)与平滑释放(5~200ms)、干湿并行混合，以及【按住试听齿音频带】独听模式。
/// </summary>
public sealed class FurinaPlayerDeEsserProcessor
{
	private double _sampleRate = 44100.0;
	private bool _enabled = false;
	private bool _auditionBand = false;

	// 找齿音
	private float _frequencyHz = 6500.0f;
	private float _q = 4.0f;

	// 衰减多少
	private float _thresholdDb = -20.0f;
	private float _ratio = 4.0f;
	private float _kneeDb = 6.0f;
	private float _maxReductionDb = 24.0f;
	private float _mix = 1.0f; // 100%

	// 快慢
	private float _attackMs = 1.0f;
	private float _releaseMs = 40.0f;

	// Biquad 滤波系数 (带通用于检测，动态峰值用于主信号压制)
	private double _b0, _b1, _b2, _a1, _a2;
	private double _bpL1, _bpL2, _bpR1, _bpR2;
	private double _bpL1_2, _bpL2_2, _bpR1_2, _bpR2_2;

	// 包络检测与衰减跟踪
	private float _envelopeL = 0.0f;
	private float _envelopeR = 0.0f;
	private float _gainReductionL = 0.0f;
	private float _gainReductionR = 0.0f;
	private float _currentGrDb = 0.0f;

	public bool Enabled { get => _enabled; set => _enabled = value; }
	public bool AuditionBand { get => _auditionBand; set => _auditionBand = value; }

	public float FrequencyHz
	{
		get => _frequencyHz;
		set
		{
			_frequencyHz = Math.Clamp(value, 2000.0f, 12000.0f);
			UpdateBandpassCoefficients();
		}
	}

	public float Q
	{
		get => _q;
		set
		{
			_q = Math.Clamp(value, 0.5f, 10.0f);
			UpdateBandpassCoefficients();
		}
	}

	public float ThresholdDb
	{
		get => _thresholdDb;
		set => _thresholdDb = Math.Clamp(value, -40.0f, 0.0f);
	}

	public float Ratio
	{
		get => _ratio;
		set => _ratio = Math.Clamp(value, 1.0f, 20.0f);
	}

	public float KneeDb
	{
		get => _kneeDb;
		set => _kneeDb = Math.Clamp(value, 0.0f, 12.0f);
	}

	public float MaxReductionDb
	{
		get => _maxReductionDb;
		set => _maxReductionDb = Math.Clamp(value, 0.0f, 36.0f);
	}

	public float Mix
	{
		get => _mix;
		set => _mix = Math.Clamp(value, 0.0f, 1.0f);
	}

	public float AttackMs
	{
		get => _attackMs;
		set => _attackMs = Math.Clamp(value, 0.1f, 20.0f);
	}

	public float ReleaseMs
	{
		get => _releaseMs;
		set => _releaseMs = Math.Clamp(value, 5.0f, 200.0f);
	}

	public float CurrentGainReductionDb => _currentGrDb;

	public FurinaPlayerDeEsserProcessor(double sampleRate = 44100.0)
	{
		Prepare(sampleRate);
	}

	public void Prepare(double sampleRate)
	{
		_sampleRate = Math.Max(1.0, sampleRate);
		Reset();
		UpdateBandpassCoefficients();
	}

	public void Reset()
	{
		_bpL1 = _bpL2 = _bpR1 = _bpR2 = _bpL1_2 = _bpL2_2 = _bpR1_2 = _bpR2_2 = 0.0;
		_envelopeL = _envelopeR = 0.0f;
		_gainReductionL = _gainReductionR = 0.0f;
		_currentGrDb = 0.0f;
	}

	private void UpdateBandpassCoefficients()
	{
		double w0 = 2.0 * Math.PI * _frequencyHz / _sampleRate;
		double sinW0 = Math.Sin(w0);
		double cosW0 = Math.Cos(w0);
		double alpha = sinW0 / (2.0 * Math.Max(0.1, _q));

		// Biquad 恒定峰值增益带通滤波器 (Peak Gain Q)
		double a0 = 1.0 + alpha;
		_b0 = alpha / a0;
		_b1 = 0.0;
		_b2 = -alpha / a0;
		_a1 = (-2.0 * cosW0) / a0;
		_a2 = (1.0 - alpha) / a0;
	}

	public void ProcessInterleaved(float[] buffer, int offset, int count, int channels)
	{
		if (!_enabled || count <= 0 || channels <= 0) return;

		float attackCoeff = (float)Math.Exp(-1.0 / (_sampleRate * Math.Max(0.0001, _attackMs * 0.001)));
		float releaseCoeff = (float)Math.Exp(-1.0 / (_sampleRate * Math.Max(0.001, _releaseMs * 0.001)));

		int frames = count / channels;
		float maxBlockGr = 0.0f;

		for (int frame = 0; frame < frames; frame++)
		{
			int baseIdx = offset + frame * channels;
			float inL = buffer[baseIdx];
			float inR = channels > 1 ? buffer[baseIdx + 1] : inL;

			// 1. 4阶带通滤波提取齿音频带 (双极点级联，带外陡降40dB/dec，杜绝低频串扰)
			double sL1 = _b0 * inL + _b1 * _bpL1 + _b2 * _bpL2 - _a1 * _bpL1 - _a2 * _bpL2;
			_bpL2 = _bpL1;
			_bpL1 = sL1;
			double sibilanceL = _b0 * sL1 + _b1 * _bpL1_2 + _b2 * _bpL2_2 - _a1 * _bpL1_2 - _a2 * _bpL2_2;
			_bpL2_2 = _bpL1_2;
			_bpL1_2 = sibilanceL;

			double sR1 = _b0 * inR + _b1 * _bpR1 + _b2 * _bpR2 - _a1 * _bpR1 - _a2 * _bpR2;
			_bpR2 = _bpR1;
			_bpR1 = sR1;
			double sibilanceR = _b0 * sR1 + _b1 * _bpR1_2 + _b2 * _bpR2_2 - _a1 * _bpR1_2 - _a2 * _bpR2_2;
			_bpR2_2 = _bpR1_2;
			_bpR1_2 = sibilanceR;

			// 2. 如果处于【按住试听齿音频带】独听模式，直接输出带通齿音信号
			if (_auditionBand)
			{
				buffer[baseIdx] = (float)sibilanceL;
				if (channels > 1) buffer[baseIdx + 1] = (float)sibilanceR;
				continue;
			}

			// 3. 包络检测
			float absSibL = (float)Math.Abs(sibilanceL);
			float absSibR = (float)Math.Abs(sibilanceR);

			_envelopeL = absSibL > _envelopeL
				? attackCoeff * _envelopeL + (1.0f - attackCoeff) * absSibL
				: releaseCoeff * _envelopeL + (1.0f - releaseCoeff) * absSibL;

			_envelopeR = absSibR > _envelopeR
				? attackCoeff * _envelopeR + (1.0f - attackCoeff) * absSibR
				: releaseCoeff * _envelopeR + (1.0f - releaseCoeff) * absSibR;

			// 4. 计算软拐点增益衰减 (Soft-Knee Gain Reduction)
			float grDbL = ComputeGainReduction(_envelopeL);
			float grDbR = ComputeGainReduction(_envelopeR);

			float blockGr = Math.Max(grDbL, grDbR);
			if (blockGr > maxBlockGr) maxBlockGr = blockGr;

			float linearGainL = (float)Math.Pow(10.0, -grDbL / 20.0);
			float linearGainR = (float)Math.Pow(10.0, -grDbR / 20.0);

			// 5. 动态压制齿音频带并混合干湿声
			// 压制只施加在齿音频带上，完整保留低频与极高频质感
			float dryL = inL;
			float dryR = inR;
			float wetL = (float)(inL - sibilanceL * (1.0 - linearGainL));
			float wetR = (float)(inR - sibilanceR * (1.0 - linearGainR));

			buffer[baseIdx] = (1.0f - _mix) * dryL + _mix * wetL;
			if (channels > 1)
			{
				buffer[baseIdx + 1] = (1.0f - _mix) * dryR + _mix * wetR;
			}
		}

		_currentGrDb = maxBlockGr;
	}

	private float ComputeGainReduction(float envLevelLinear)
	{
		if (envLevelLinear < 1e-6f) return 0.0f;

		float envDb = 20.0f * (float)Math.Log10(envLevelLinear);
		float overDb = envDb - _thresholdDb;
		float halfKnee = _kneeDb * 0.5f;

		float grDb = 0.0f;
		if (overDb <= -halfKnee)
		{
			grDb = 0.0f;
		}
		else if (overDb >= halfKnee)
		{
			grDb = overDb * (1.0f - 1.0f / _ratio);
		}
		else
		{
			// 软拐点二次多项式过渡
			float norm = (overDb + halfKnee) / _kneeDb;
			float softOver = norm * norm * halfKnee;
			grDb = softOver * (1.0f - 1.0f / _ratio);
		}

		return Math.Clamp(grDb, 0.0f, _maxReductionDb);
	}
}
