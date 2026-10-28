using System;

namespace SonicWave.Audio.Dsp;

public sealed class EchoPcmResampler
{
	private int _sourceSampleRate = 44100;
	private int _targetSampleRate = 44100;
	private bool _enabled = false;

	// Per-channel 4-point history for Hermite cubic spline interpolation
	private float[][] _history = new float[2][];
	private double _phase = 0.0;

	public bool Enabled
	{
		get => _enabled && _targetSampleRate != _sourceSampleRate;
		set => _enabled = value;
	}

	public int TargetSampleRate
	{
		get => _targetSampleRate;
		set => _targetSampleRate = Math.Clamp(value, 44100, 384000);
	}

	public EchoPcmResampler()
	{
		for (int i = 0; i < _history.Length; i++)
		{
			_history[i] = new float[4];
		}
	}

	public void Prepare(int sourceSampleRate, int targetSampleRate = 0)
	{
		_sourceSampleRate = Math.Max(1, sourceSampleRate);
		if (targetSampleRate > 0)
		{
			_targetSampleRate = targetSampleRate;
		}
		Reset();
	}

	public void Reset()
	{
		_phase = 0.0;
		for (int i = 0; i < _history.Length; i++)
		{
			Array.Clear(_history[i], 0, _history[i].Length);
		}
	}

	// 4-point, 3rd-order Hermite interpolation
	private static float HermiteInterpolate(float y0, float y1, float y2, float y3, float mu)
	{
		float mu2 = mu * mu;
		float a0 = -0.5f * y0 + 1.5f * y1 - 1.5f * y2 + 0.5f * y3;
		float a1 = y0 - 2.5f * y1 + 2.0f * y2 - 0.5f * y3;
		float a2 = -0.5f * y0 + 0.5f * y2;
		float a3 = y1;

		return a0 * mu * mu2 + a1 * mu2 + a2 * mu + a3;
	}

	public float[] ResampleInterleaved(float[] input, int offset, int count, int channels, out int outputCount)
	{
		if (!Enabled || _sourceSampleRate == _targetSampleRate || count <= 0 || channels <= 0)
		{
			outputCount = count;
			return input;
		}

		int inputFrames = count / channels;
		double ratio = (double)_sourceSampleRate / _targetSampleRate;
		int estimatedOutputFrames = (int)Math.Ceiling(inputFrames / ratio) + 8;
		float[] output = new float[estimatedOutputFrames * channels];

		int outFrame = 0;
		double currentInputPos = _phase;

		while (currentInputPos < inputFrames)
		{
			int baseIdx = (int)Math.Floor(currentInputPos);
			float mu = (float)(currentInputPos - baseIdx);

			for (int ch = 0; ch < channels; ch++)
			{
				int chIdx = Math.Min(ch, _history.Length - 1);
				float p0 = baseIdx > 0 ? input[offset + (baseIdx - 1) * channels + ch] : _history[chIdx][2];
				float p1 = input[offset + baseIdx * channels + ch];
				float p2 = baseIdx + 1 < inputFrames ? input[offset + (baseIdx + 1) * channels + ch] : p1;
				float p3 = baseIdx + 2 < inputFrames ? input[offset + (baseIdx + 2) * channels + ch] : p2;

				output[outFrame * channels + ch] = HermiteInterpolate(p0, p1, p2, p3, mu);
			}

			outFrame++;
			currentInputPos += ratio;
		}

		_phase = currentInputPos - inputFrames;

		// Save trailing samples to history
		for (int ch = 0; ch < channels; ch++)
		{
			int chIdx = Math.Min(ch, _history.Length - 1);
			if (inputFrames >= 3)
			{
				_history[chIdx][0] = input[offset + (inputFrames - 3) * channels + ch];
				_history[chIdx][1] = input[offset + (inputFrames - 2) * channels + ch];
				_history[chIdx][2] = input[offset + (inputFrames - 1) * channels + ch];
			}
		}

		outputCount = outFrame * channels;
		return output;
	}
}
