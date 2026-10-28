using System;

namespace SonicWave.Audio.Dsp;

public enum ChannelMatrixPreset
{
	Normal,
	SwapLeftRight,
	MonoSum,
	InvertPhaseLeft,
	InvertPhaseRight,
	Custom
}

public sealed class EchoSpatialProcessor
{
	private double _sampleRate = 44100.0;

	// Crossfeed
	private bool _crossfeedEnabled = false;
	private float _crossfeedAmount = 0.35f; // 0.0 - 1.0
	private float _crossfeedCutoffHz = 700.0f; // 300 - 2000 Hz
	private float _crossfeedLowpassSide = 0.0f;

	// Stereo Field
	private bool _stereoFieldEnabled = false;
	private float _stereoWidth = 1.0f; // 0.0 (mono) - 2.0 (super-wide)
	private float _centerGainDb = 0.0f;
	private float _sideGainDb = 0.0f;

	// Channel Matrix
	private bool _channelMatrixEnabled = false;
	private float _leftToLeft = 1.0f;
	private float _rightToLeft = 0.0f;
	private float _leftToRight = 0.0f;
	private float _rightToRight = 1.0f;

	// Properties
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
		Reset();
	}

	public void Reset()
	{
		_crossfeedLowpassSide = 0.0f;
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
}
