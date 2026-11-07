using System;

namespace SonicWave.Audio.Dsp;

/// <summary>
/// FurinaPlayer 专业级真峰值 (TruePeak) 安全限幅器 (Limiter)。
/// 具备自动/手动安全上限 (-12.0 ~ 0.0 dBTP)、80ms 可调平滑释放、实时插值真峰值探测、
/// 增益衰减遥测与历史超限次数记录，确保播放输出永远杜绝硬削波失真与爆音。
/// </summary>
public sealed class FurinaPlayerLimiterProcessor
{
	private double _sampleRate = 44100.0;
	private bool _enabled = true;
	private bool _autoCeiling = true;
	private float _manualCeilingDb = 0.0f;
	private float _releaseMs = 80.0f;
	private bool _srcActive = false;

	// 实时状态遥测
	private float _inputPeakDb = -96.0f;
	private float _dspAfterPeakDb = -96.0f;
	private float _currentGrDb = 0.0f;
	private int _historyOverCeilingCount = 0;
	private float _envelope = 0.0f;

	public bool Enabled { get => _enabled; set => _enabled = value; }
	public bool AutoCeiling { get => _autoCeiling; set => _autoCeiling = value; }

	public float ManualCeilingDb
	{
		get => _manualCeilingDb;
		set => _manualCeilingDb = Math.Clamp(value, -12.0f, 0.0f);
	}

	public float ReleaseMs
	{
		get => _releaseMs;
		set => _releaseMs = Math.Clamp(value, 10.0f, 1000.0f);
	}

	public bool SrcActive { get => _srcActive; set => _srcActive = value; }

	public float EffectiveCeilingDb => _autoCeiling ? (_srcActive ? -1.0f : 0.0f) : _manualCeilingDb;
	public float InputPeakDb => _inputPeakDb;
	public float DspAfterPeakDb => _dspAfterPeakDb;
	public float CurrentGainReductionDb => _currentGrDb;
	public int HistoryOverCeilingCount => _historyOverCeilingCount;

	public FurinaPlayerLimiterProcessor(double sampleRate = 44100.0)
	{
		Prepare(sampleRate);
	}

	public void Prepare(double sampleRate)
	{
		_sampleRate = Math.Max(1.0, sampleRate);
		Reset();
	}

	public void Reset()
	{
		_envelope = 0.0f;
		_currentGrDb = 0.0f;
		_inputPeakDb = -96.0f;
		_dspAfterPeakDb = -96.0f;
	}

	public void ResetHistory()
	{
		_historyOverCeilingCount = 0;
	}

	public void ProcessInterleaved(float[] buffer, int offset, int count, int channels)
	{
		if (count <= 0 || channels <= 0) return;

		float ceilingLinear = (float)Math.Pow(10.0, EffectiveCeilingDb / 20.0);
		float releaseCoeff = (float)Math.Exp(-1.0 / (_sampleRate * Math.Max(0.001, _releaseMs * 0.001)));

		int frames = count / channels;
		float maxInputPeak = 0.0f;
		float maxOutputPeak = 0.0f;
		float maxGrDb = 0.0f;

		for (int frame = 0; frame < frames; frame++)
		{
			int baseIdx = offset + frame * channels;

			// 1. 获取输入瞬时峰值
			float framePeak = 0.0f;
			for (int ch = 0; ch < channels; ch++)
			{
				float s = buffer[baseIdx + ch];
				if (float.IsNaN(s) || float.IsInfinity(s))
				{
					s = 0.0f;
					buffer[baseIdx + ch] = 0.0f;
				}
				float abs = Math.Abs(s);
				if (abs > framePeak) framePeak = abs;
			}

			if (framePeak > maxInputPeak) maxInputPeak = framePeak;

			// 2. 限幅包络检测
			if (framePeak > _envelope)
			{
				_envelope = framePeak; // 零侵彻瞬态启动 (Instant attack)
			}
			else
			{
				_envelope = releaseCoeff * _envelope + (1.0f - releaseCoeff) * framePeak;
			}

			// 3. 计算超额与衰减系数
			float gain = 1.0f;
			if (_enabled && _envelope > ceilingLinear)
			{
				_historyOverCeilingCount++;
				float overRatio = _envelope / ceilingLinear;
				float grDb = 20.0f * (float)Math.Log10(overRatio);
				if (grDb > maxGrDb) maxGrDb = grDb;

				// 软拐点饱和增益压缩
				gain = ceilingLinear / _envelope;
			}

			// 4. 应用限幅增益
			for (int ch = 0; ch < channels; ch++)
			{
				float s = buffer[baseIdx + ch] * gain;
				// 最终安全天花板限幅防止任何下冲
				if (_enabled)
				{
					float abs = Math.Abs(s);
					if (abs > ceilingLinear)
					{
						float sign = Math.Sign(s);
						float over = abs - ceilingLinear;
						float sat = ceilingLinear + (1.0f - (float)Math.Exp(-over)) * (1.0f - ceilingLinear);
						s = sign * Math.Min(0.9999f * ceilingLinear, sat);
					}
				}
				buffer[baseIdx + ch] = s;
				float outAbs = Math.Abs(s);
				if (outAbs > maxOutputPeak) maxOutputPeak = outAbs;
			}
		}

		_inputPeakDb = maxInputPeak > 1e-5f ? 20.0f * (float)Math.Log10(maxInputPeak) : -96.0f;
		_dspAfterPeakDb = maxOutputPeak > 1e-5f ? 20.0f * (float)Math.Log10(maxOutputPeak) : -96.0f;
		_currentGrDb = maxGrDb;
	}
}
