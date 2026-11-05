using System;

namespace SonicWave.Audio.Dsp;

public enum ChannelMonoMode
{
	Off = 0,
	Sum = 1,
	LeftOnly = 2,
	RightOnly = 3
}

/// <summary>
/// FurinaPlayer 专业级空间声场与声道矩阵处理器。
/// 集成 Crossfeed 双耳互馈、声场宽度控制、2x2 路由矩阵以及【声道工具（声像平衡、单侧偏音补偿、3频段左右校准、微秒延迟与相位控制）】。
/// </summary>
public class FurinaPlayerSpatialProcessor
{
	private const int DelayBufferSize = 8192;
	private double _sampleRate = 44100.0;

	// --- 1. Crossfeed ---
	private bool _crossfeedEnabled = false;
	private float _crossfeedAmount = 0.35f;
	private float _crossfeedCutoffHz = 700.0f;
	private float _crossfeedLowpassSide = 0.0f;

	// --- 2. Stereo Field ---
	private bool _stereoFieldEnabled = false;
	private float _stereoWidth = 1.0f;
	private float _centerGainDb = 0.0f;
	private float _sideGainDb = 0.0f;

	// --- 3. Channel Matrix ---
	private bool _channelMatrixEnabled = false;
	private float _leftToLeft = 1.0f;
	private float _rightToLeft = 0.0f;
	private float _leftToRight = 0.0f;
	private float _rightToRight = 1.0f;

	// --- 4. Channel Tool Core Parameters ---
	private bool _channelToolEnabled = false;
	private bool _compareBypassed = false;
	private double _balance = 0.0; // -1.0 (L 100%) to +1.0 (R 100%)
	private bool _constantPower = false;
	private double _leftGainDb = 0.0; // -12 dB to +6 dB
	private double _rightGainDb = 0.0; // -12 dB to +6 dB
	private double _trimStepDb = 0.25;

	// 3-Band Left/Right Trims (-6 dB to +3 dB)
	private double _lowLeftGainDb = 0.0;
	private double _lowRightGainDb = 0.0;
	private double _midLeftGainDb = 0.0;
	private double _midRightGainDb = 0.0;
	private double _highLeftGainDb = 0.0;
	private double _highRightGainDb = 0.0;

	// Delays in ms (0 to 10 ms)
	private double _leftDelayMs = 0.0;
	private double _rightDelayMs = 0.0;

	// Phase & Routing
	private ChannelMonoMode _monoMode = ChannelMonoMode.Off;
	private bool _swapLeftRight = false;
	private bool _invertPhaseLeft = false;
	private bool _invertPhaseRight = false;

	// Internal Filter & Delay States
	private readonly float[] _leftDelayBuffer = new float[DelayBufferSize];
	private readonly float[] _rightDelayBuffer = new float[DelayBufferSize];
	private int _leftDelayWritePos = 0;
	private int _rightDelayWritePos = 0;

	private readonly EchoBiquadFilter _filterLowL = new() { FilterType = EchoFilterType.LowShelf, FrequencyHz = 250.0, Q = 0.7071 };
	private readonly EchoBiquadFilter _filterLowR = new() { FilterType = EchoFilterType.LowShelf, FrequencyHz = 250.0, Q = 0.7071 };
	private readonly EchoBiquadFilter _filterMidL = new() { FilterType = EchoFilterType.Peaking, FrequencyHz = 1000.0, Q = 0.8 };
	private readonly EchoBiquadFilter _filterMidR = new() { FilterType = EchoFilterType.Peaking, FrequencyHz = 1000.0, Q = 0.8 };
	private readonly EchoBiquadFilter _filterHighL = new() { FilterType = EchoFilterType.HighShelf, FrequencyHz = 4000.0, Q = 0.7071 };
	private readonly EchoBiquadFilter _filterHighR = new() { FilterType = EchoFilterType.HighShelf, FrequencyHz = 4000.0, Q = 0.7071 };

	private float _currentLeftGain = 1.0f;
	private float _currentRightGain = 1.0f;
	private float _targetLeftGain = 1.0f;
	private float _targetRightGain = 1.0f;

	#region Public Properties - Crossfeed & Stereo Field & Matrix

	public bool CrossfeedEnabled { get => _crossfeedEnabled; set => _crossfeedEnabled = value; }
	public float CrossfeedAmount { get => _crossfeedAmount; set => _crossfeedAmount = Math.Clamp(value, 0.0f, 1.0f); }
	public float CrossfeedCutoffHz { get => _crossfeedCutoffHz; set => _crossfeedCutoffHz = Math.Clamp(value, 100.0f, 4000.0f); }

	public bool StereoFieldEnabled { get => _stereoFieldEnabled; set => _stereoFieldEnabled = value; }
	public float StereoWidth { get => _stereoWidth; set => _stereoWidth = Math.Clamp(value, 0.0f, 2.0f); }
	public float CenterGainDb { get => _centerGainDb; set => _centerGainDb = Math.Clamp(value, -24.0f, 24.0f); }
	public float SideGainDb { get => _sideGainDb; set => _sideGainDb = Math.Clamp(value, -24.0f, 24.0f); }

	public bool ChannelMatrixEnabled { get => _channelMatrixEnabled; set => _channelMatrixEnabled = value; }
	public float MatrixLeftToLeft { get => _leftToLeft; set => _leftToLeft = value; }
	public float MatrixRightToLeft { get => _rightToLeft; set => _rightToLeft = value; }
	public float MatrixLeftToRight { get => _leftToRight; set => _leftToRight = value; }
	public float MatrixRightToRight { get => _rightToRight; set => _rightToRight = value; }

	#endregion

	#region Public Properties - Channel Tool

	public bool ChannelToolEnabled
	{
		get => _channelToolEnabled;
		set
		{
			_channelToolEnabled = value;
			UpdateTargetGains();
		}
	}

	public bool CompareBypassed
	{
		get => _compareBypassed;
		set => _compareBypassed = value;
	}

	public double Balance
	{
		get => _balance;
		set
		{
			_balance = Math.Clamp(value, -1.0, 1.0);
			UpdateTargetGains();
		}
	}

	public bool ConstantPower
	{
		get => _constantPower;
		set
		{
			_constantPower = value;
			UpdateTargetGains();
		}
	}

	public double LeftGainDb
	{
		get => _leftGainDb;
		set
		{
			_leftGainDb = Math.Clamp(value, -12.0, 6.0);
			UpdateTargetGains();
		}
	}

	public double RightGainDb
	{
		get => _rightGainDb;
		set
		{
			_rightGainDb = Math.Clamp(value, -12.0, 6.0);
			UpdateTargetGains();
		}
	}

	public double TrimStepDb
	{
		get => _trimStepDb;
		set => _trimStepDb = Math.Clamp(value, 0.05, 3.0);
	}

	public double LeftDelayMs
	{
		get => _leftDelayMs;
		set => _leftDelayMs = Math.Clamp(value, 0.0, 10.0);
	}

	public double RightDelayMs
	{
		get => _rightDelayMs;
		set => _rightDelayMs = Math.Clamp(value, 0.0, 10.0);
	}

	public double LowLeftGainDb
	{
		get => _lowLeftGainDb;
		set { _lowLeftGainDb = Math.Clamp(value, -6.0, 3.0); UpdateBiquadFilters(); }
	}

	public double LowRightGainDb
	{
		get => _lowRightGainDb;
		set { _lowRightGainDb = Math.Clamp(value, -6.0, 3.0); UpdateBiquadFilters(); }
	}

	public double MidLeftGainDb
	{
		get => _midLeftGainDb;
		set { _midLeftGainDb = Math.Clamp(value, -6.0, 3.0); UpdateBiquadFilters(); }
	}

	public double MidRightGainDb
	{
		get => _midRightGainDb;
		set { _midRightGainDb = Math.Clamp(value, -6.0, 3.0); UpdateBiquadFilters(); }
	}

	public double HighLeftGainDb
	{
		get => _highLeftGainDb;
		set { _highLeftGainDb = Math.Clamp(value, -6.0, 3.0); UpdateBiquadFilters(); }
	}

	public double HighRightGainDb
	{
		get => _highRightGainDb;
		set { _highRightGainDb = Math.Clamp(value, -6.0, 3.0); UpdateBiquadFilters(); }
	}

	public ChannelMonoMode MonoMode
	{
		get => _monoMode;
		set => _monoMode = value;
	}

	public bool SwapLeftRight
	{
		get => _swapLeftRight;
		set => _swapLeftRight = value;
	}

	public bool InvertPhaseLeft
	{
		get => _invertPhaseLeft;
		set => _invertPhaseLeft = value;
	}

	public bool InvertPhaseRight
	{
		get => _invertPhaseRight;
		set => _invertPhaseRight = value;
	}

	#endregion

	#region Telemetry & Status Properties

	public double LeftAppliedDb
	{
		get
		{
			double balDb = GetBalanceGainDb(true);
			return Math.Round((_leftGainDb + balDb) * 100.0) / 100.0;
		}
	}

	public double RightAppliedDb
	{
		get
		{
			double balDb = GetBalanceGainDb(false);
			return Math.Round((_rightGainDb + balDb) * 100.0) / 100.0;
		}
	}

	public double SkewDb => Math.Round((RightAppliedDb - LeftAppliedDb) * 100.0) / 100.0;

	public string SkewStatusText
	{
		get
		{
			double skew = SkewDb;
			if (Math.Abs(skew) < 0.05) return "中心稳定";
			return skew > 0 ? $"偏右 +{skew:F2} dB" : $"偏左 {Math.Abs(skew):F2} dB";
		}
	}

	public double MeterLeftPercent
	{
		get
		{
			double skew = SkewDb;
			// 50% - skew * 8, clamped to [8%, 92%]
			double pct = 50.0 - skew * 8.0;
			return Math.Clamp(pct, 8.0, 92.0);
		}
	}

	public double MeterRightPercent => 100.0 - MeterLeftPercent;

	#endregion

	public void SetMatrixPreset(ChannelMatrixPreset preset)
	{
		_channelMatrixEnabled = true;
		switch (preset)
		{
			case ChannelMatrixPreset.SwapLeftRight:
				_leftToLeft = 0.0f;
				_rightToLeft = 1.0f;
				_leftToRight = 1.0f;
				_rightToRight = 0.0f;
				break;
			case ChannelMatrixPreset.MonoSum:
				_leftToLeft = 0.5f;
				_rightToLeft = 0.5f;
				_leftToRight = 0.5f;
				_rightToRight = 0.5f;
				break;
			case ChannelMatrixPreset.InvertPhaseLeft:
				_leftToLeft = -1.0f;
				_rightToLeft = 0.0f;
				_leftToRight = 0.0f;
				_rightToRight = 1.0f;
				break;
			case ChannelMatrixPreset.InvertPhaseRight:
				_leftToLeft = 1.0f;
				_rightToLeft = 0.0f;
				_leftToRight = 0.0f;
				_rightToRight = -1.0f;
				break;
			case ChannelMatrixPreset.Normal:
			default:
				_leftToLeft = 1.0f;
				_rightToLeft = 0.0f;
				_leftToRight = 0.0f;
				_rightToRight = 1.0f;
				_channelMatrixEnabled = false;
				break;
		}
	}

	public void Prepare(double sampleRate)
	{
		_sampleRate = Math.Max(1.0, sampleRate);
		UpdateBiquadFilters();
		Reset();
	}

	public void Reset()
	{
		_crossfeedLowpassSide = 0.0f;
		Array.Clear(_leftDelayBuffer, 0, _leftDelayBuffer.Length);
		Array.Clear(_rightDelayBuffer, 0, _rightDelayBuffer.Length);
		_leftDelayWritePos = 0;
		_rightDelayWritePos = 0;
		_currentLeftGain = _targetLeftGain;
		_currentRightGain = _targetRightGain;
	}

	public void TrimSide(bool isLeft)
	{
		if (isLeft)
		{
			LeftGainDb -= _trimStepDb;
		}
		else
		{
			RightGainDb -= _trimStepDb;
		}
		ChannelToolEnabled = true;
	}

	public void ClearCompensation()
	{
		_balance = 0.0;
		_leftGainDb = 0.0;
		_rightGainDb = 0.0;
		_lowLeftGainDb = 0.0;
		_lowRightGainDb = 0.0;
		_midLeftGainDb = 0.0;
		_midRightGainDb = 0.0;
		_highLeftGainDb = 0.0;
		_highRightGainDb = 0.0;
		_leftDelayMs = 0.0;
		_rightDelayMs = 0.0;
		UpdateTargetGains();
		UpdateBiquadFilters();
	}

	public void SwapCompensationDirection()
	{
		double tmpGain = _leftGainDb;
		_leftGainDb = _rightGainDb;
		_rightGainDb = tmpGain;

		double tmpDelay = _leftDelayMs;
		_leftDelayMs = _rightDelayMs;
		_rightDelayMs = tmpDelay;

		double tmpLow = _lowLeftGainDb;
		_lowLeftGainDb = _lowRightGainDb;
		_lowRightGainDb = tmpLow;

		double tmpMid = _midLeftGainDb;
		_midLeftGainDb = _midRightGainDb;
		_midRightGainDb = tmpMid;

		double tmpHigh = _highLeftGainDb;
		_highLeftGainDb = _highRightGainDb;
		_highRightGainDb = tmpHigh;

		UpdateTargetGains();
		UpdateBiquadFilters();
	}

	private double GetBalanceGainDb(bool isLeft)
	{
		double b = Math.Clamp(_balance, -1.0, 1.0);
		if (!_constantPower)
		{
			double lin = isLeft
				? (b > 0.0 ? 1.0 - b : 1.0)
				: (b < 0.0 ? 1.0 + b : 1.0);
			return lin <= 1e-4 ? -60.0 : 20.0 * Math.Log10(Math.Max(1e-4, lin));
		}
		else
		{
			double angle = (b + 1.0) * Math.PI * 0.25;
			double sqrt2 = Math.Sqrt(2.0);
			double lin = isLeft
				? Math.Min(1.0, Math.Cos(angle) * sqrt2)
				: Math.Min(1.0, Math.Sin(angle) * sqrt2);
			return lin <= 1e-4 ? -60.0 : 20.0 * Math.Log10(Math.Max(1e-4, lin));
		}
	}

	private void UpdateTargetGains()
	{
		if (!_channelToolEnabled)
		{
			_targetLeftGain = 1.0f;
			_targetRightGain = 1.0f;
			return;
		}

		double totalLeftDb = LeftAppliedDb;
		double totalRightDb = RightAppliedDb;

		_targetLeftLinear = (float)Math.Pow(10.0, totalLeftDb / 20.0);
		_targetRightLinear = (float)Math.Pow(10.0, totalRightDb / 20.0);
		_targetLeftGain = _targetLeftLinear;
		_targetRightGain = _targetRightLinear;
	}

	private float _targetLeftLinear = 1.0f;
	private float _targetRightLinear = 1.0f;

	private void UpdateBiquadFilters()
	{
		_filterLowL.GainDb = _lowLeftGainDb;
		_filterLowL.Enabled = Math.Abs(_lowLeftGainDb) > 0.001;
		_filterLowL.UpdateCoefficients(_sampleRate);

		_filterLowR.GainDb = _lowRightGainDb;
		_filterLowR.Enabled = Math.Abs(_lowRightGainDb) > 0.001;
		_filterLowR.UpdateCoefficients(_sampleRate);

		_filterMidL.GainDb = _midLeftGainDb;
		_filterMidL.Enabled = Math.Abs(_midLeftGainDb) > 0.001;
		_filterMidL.UpdateCoefficients(_sampleRate);

		_filterMidR.GainDb = _midRightGainDb;
		_filterMidR.Enabled = Math.Abs(_midRightGainDb) > 0.001;
		_filterMidR.UpdateCoefficients(_sampleRate);

		_filterHighL.GainDb = _highLeftGainDb;
		_filterHighL.Enabled = Math.Abs(_highLeftGainDb) > 0.001;
		_filterHighL.UpdateCoefficients(_sampleRate);

		_filterHighR.GainDb = _highRightGainDb;
		_filterHighR.Enabled = Math.Abs(_highRightGainDb) > 0.001;
		_filterHighR.UpdateCoefficients(_sampleRate);
	}

	public void ProcessCrossfeed(float[] buffer, int offset, int count, int channels)
	{
		if (!_crossfeedEnabled || count <= 0 || channels < 2) return;

		float coefficient = (float)(1.0 - Math.Exp(-2.0 * Math.PI * _crossfeedCutoffHz / _sampleRate));
		int frames = count / channels;

		for (int frame = 0; frame < frames; frame++)
		{
			int idx = offset + frame * channels;
			float l = buffer[idx];
			float r = buffer[idx + 1];

			float mid = 0.5f * (l + r);
			float side = 0.5f * (l - r);

			_crossfeedLowpassSide += coefficient * (side - _crossfeedLowpassSide);
			float processedSide = side - _crossfeedAmount * _crossfeedLowpassSide;

			buffer[idx] = mid + processedSide;
			buffer[idx + 1] = mid - processedSide;
		}
	}

	public void ProcessStereoField(float[] buffer, int offset, int count, int channels)
	{
		if (!_stereoFieldEnabled || count <= 0 || channels < 2) return;

		float midGain = (float)Math.Pow(10.0, _centerGainDb / 20.0);
		float sideGain = (float)Math.Pow(10.0, _sideGainDb / 20.0) * _stereoWidth;
		int frames = count / channels;

		for (int frame = 0; frame < frames; frame++)
		{
			int idx = offset + frame * channels;
			float l = buffer[idx];
			float r = buffer[idx + 1];

			float mid = 0.5f * (l + r) * midGain;
			float side = 0.5f * (l - r) * sideGain;

			buffer[idx] = mid + side;
			buffer[idx + 1] = mid - side;
		}
	}

	public void ProcessChannelMatrix(float[] buffer, int offset, int count, int channels)
	{
		if (!_channelMatrixEnabled || count <= 0 || channels < 2) return;

		int frames = count / channels;
		for (int frame = 0; frame < frames; frame++)
		{
			int idx = offset + frame * channels;
			float lIn = buffer[idx];
			float rIn = buffer[idx + 1];

			buffer[idx] = lIn * _leftToLeft + rIn * _rightToLeft;
			buffer[idx + 1] = lIn * _leftToRight + rIn * _rightToRight;
		}
	}

	public void ProcessChannelTool(float[] buffer, int offset, int count, int channels)
	{
		if (!_channelToolEnabled || _compareBypassed || count <= 0 || channels < 2) return;

		int frames = count / channels;
		int leftDelaySamples = Math.Min(DelayBufferSize - 1, (int)(_leftDelayMs * 0.001 * _sampleRate));
		int rightDelaySamples = Math.Min(DelayBufferSize - 1, (int)(_rightDelayMs * 0.001 * _sampleRate));

		bool hasBandEq = Math.Abs(_lowLeftGainDb) > 0.001 || Math.Abs(_lowRightGainDb) > 0.001 ||
		                 Math.Abs(_midLeftGainDb) > 0.001 || Math.Abs(_midRightGainDb) > 0.001 ||
		                 Math.Abs(_highLeftGainDb) > 0.001 || Math.Abs(_highRightGainDb) > 0.001;

		float gainSmoothing = 0.02f;

		for (int frame = 0; frame < frames; frame++)
		{
			int idx = offset + frame * channels;
			float l = buffer[idx];
			float r = buffer[idx + 1];

			// 1. Mono Mode
			switch (_monoMode)
			{
				case ChannelMonoMode.Sum:
					float mono = 0.5f * (l + r);
					l = mono;
					r = mono;
					break;
				case ChannelMonoMode.LeftOnly:
					r = l;
					break;
				case ChannelMonoMode.RightOnly:
					l = r;
					break;
			}

			// 2. Invert Phase
			if (_invertPhaseLeft) l = -l;
			if (_invertPhaseRight) r = -r;

			// 3. Swap Left & Right
			if (_swapLeftRight)
			{
				float tmp = l;
				l = r;
				r = tmp;
			}

			// 4. Delay Buffer
			if (leftDelaySamples > 0)
			{
				_leftDelayBuffer[_leftDelayWritePos] = l;
				int readPos = (_leftDelayWritePos - leftDelaySamples + DelayBufferSize) % DelayBufferSize;
				l = _leftDelayBuffer[readPos];
				_leftDelayWritePos = (_leftDelayWritePos + 1) % DelayBufferSize;
			}
			if (rightDelaySamples > 0)
			{
				_rightDelayBuffer[_rightDelayWritePos] = r;
				int readPos = (_rightDelayWritePos - rightDelaySamples + DelayBufferSize) % DelayBufferSize;
				r = _rightDelayBuffer[readPos];
				_rightDelayWritePos = (_rightDelayWritePos + 1) % DelayBufferSize;
			}

			// 5. Band Compensation (3-band Shelf & Peaking)
			if (hasBandEq)
			{
				if (_filterLowL.Enabled) l = (float)_filterLowL.ProcessSample(l, 0);
				if (_filterMidL.Enabled) l = (float)_filterMidL.ProcessSample(l, 0);
				if (_filterHighL.Enabled) l = (float)_filterHighL.ProcessSample(l, 0);

				if (_filterLowR.Enabled) r = (float)_filterLowR.ProcessSample(r, 1);
				if (_filterMidR.Enabled) r = (float)_filterMidR.ProcessSample(r, 1);
				if (_filterHighR.Enabled) r = (float)_filterHighR.ProcessSample(r, 1);
			}

			// 6. Master Output Gain with smooth pop-free interpolation
			_currentLeftGain += (_targetLeftGain - _currentLeftGain) * gainSmoothing;
			_currentRightGain += (_targetRightGain - _currentRightGain) * gainSmoothing;

			buffer[idx] = l * _currentLeftGain;
			buffer[idx + 1] = r * _currentRightGain;
		}
	}
}
