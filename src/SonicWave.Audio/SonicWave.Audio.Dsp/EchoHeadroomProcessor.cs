using System;

namespace SonicWave.Audio.Dsp;

public sealed class EchoHeadroomProcessor
{
	private const double MinDb = -12.0;
	private const double MaxDb = 0.0;

	private double _sampleRate = 44100.0;
	private double _targetHeadroomDb = 0.0;
	private double _smoothedHeadroomDb = 0.0;
	private double _stepDb = 0.0;
	private int _smoothingSamplesRemaining = 0;
	private int _smoothingDurationSamples = 220;
	private bool _enabled = true;

	public bool Enabled
	{
		get => _enabled;
		set => _enabled = value;
	}

	public double HeadroomDb
	{
		get => _targetHeadroomDb;
		set => _targetHeadroomDb = Math.Clamp(value, MinDb, MaxDb);
	}

	public void Prepare(double sampleRate, int maximumBlockSize = 512)
	{
		_sampleRate = Math.Max(1.0, sampleRate);
		_smoothingDurationSamples = Math.Max(maximumBlockSize, (int)(_sampleRate * 5.0 / 1000.0)); // 5ms
		_smoothedHeadroomDb = _targetHeadroomDb;
		_smoothingSamplesRemaining = 0;
		_stepDb = 0.0;
	}

	public void Reset()
	{
		_smoothedHeadroomDb = _targetHeadroomDb;
		_smoothingSamplesRemaining = 0;
		_stepDb = 0.0;
	}

	private static double MoveTowards(double current, double target, double step)
	{
		if (step == 0.0) return target;
		double next = current + step;
		return step > 0.0 ? Math.Min(next, target) : Math.Max(next, target);
	}

	public void ProcessInterleaved(float[] buffer, int offset, int count, int channels)
	{
		if (!_enabled || count <= 0 || channels <= 0) return;

		int frames = count / channels;
		double target = _targetHeadroomDb;

		if (Math.Abs(target) <= 0.001 && Math.Abs(_smoothedHeadroomDb) <= 0.001)
		{
			return;
		}

		if (Math.Abs(target - _smoothedHeadroomDb) > 0.001 && _smoothingSamplesRemaining <= 0)
		{
			_smoothingSamplesRemaining = Math.Max(1, _smoothingDurationSamples);
			_stepDb = (target - _smoothedHeadroomDb) / _smoothingSamplesRemaining;
		}

		for (int frame = 0; frame < frames; frame++)
		{
			if (_smoothingSamplesRemaining > 0)
			{
				_smoothedHeadroomDb = MoveTowards(_smoothedHeadroomDb, target, _stepDb);
				_smoothingSamplesRemaining--;
			}
			else
			{
				_smoothedHeadroomDb = target;
			}

			float gain = (float)Math.Pow(10.0, _smoothedHeadroomDb / 20.0);
			int frameOffset = offset + frame * channels;

			for (int ch = 0; ch < channels; ch++)
			{
				buffer[frameOffset + ch] *= gain;
			}
		}
	}
}
