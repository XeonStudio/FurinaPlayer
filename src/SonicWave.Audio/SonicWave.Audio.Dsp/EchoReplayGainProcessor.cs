using System;

namespace SonicWave.Audio.Dsp;

public enum ReplayGainMode
{
	Off,
	Track,
	Album
}

public sealed class EchoReplayGainProcessor
{
	private double _sampleRate = 44100.0;
	private ReplayGainMode _mode = ReplayGainMode.Track;
	private float _trackGainDb = 0.0f;
	private float _trackPeak = 1.0f;
	private float _albumGainDb = 0.0f;
	private float _albumPeak = 1.0f;
	private float _preampDb = 0.0f;
	private bool _preventClipping = true;

	private float _currentGainDb = 0.0f;
	private float _targetGainDb = 0.0f;
	private int _rampSamples = 441;
	private int _rampPosition = 441;

	public ReplayGainMode Mode
	{
		get => _mode;
		set { _mode = value; UpdateTargetGain(); }
	}

	public float TrackGainDb
	{
		get => _trackGainDb;
		set { _trackGainDb = value; UpdateTargetGain(); }
	}

	public float TrackPeak
	{
		get => _trackPeak;
		set { _trackPeak = Math.Max(0.0f, value); UpdateTargetGain(); }
	}

	public float AlbumGainDb
	{
		get => _albumGainDb;
		set { _albumGainDb = value; UpdateTargetGain(); }
	}

	public float AlbumPeak
	{
		get => _albumPeak;
		set { _albumPeak = Math.Max(0.0f, value); UpdateTargetGain(); }
	}

	public float PreampDb
	{
		get => _preampDb;
		set { _preampDb = value; UpdateTargetGain(); }
	}

	public bool PreventClipping
	{
		get => _preventClipping;
		set { _preventClipping = value; UpdateTargetGain(); }
	}

	public void Prepare(double sampleRate)
	{
		_sampleRate = Math.Max(1.0, sampleRate);
		_rampSamples = Math.Max(1, (int)(_sampleRate * 0.01)); // 10ms
		Reset();
	}

	public void Reset()
	{
		_currentGainDb = _targetGainDb;
		_rampPosition = _rampSamples;
	}

	private void UpdateTargetGain()
	{
		if (_mode == ReplayGainMode.Off)
		{
			_targetGainDb = 0.0f;
			return;
		}

		float baseGain = (_mode == ReplayGainMode.Album ? _albumGainDb : _trackGainDb) + _preampDb;
		float peak = _mode == ReplayGainMode.Album ? _albumPeak : _trackPeak;

		if (_preventClipping && peak > 0.0001f)
		{
			float maxAllowedDb = (float)(-20.0 * Math.Log10(peak));
			baseGain = Math.Min(baseGain, maxAllowedDb);
		}

		_targetGainDb = Math.Clamp(baseGain, -24.0f, 18.0f);
		_rampPosition = 0; // Trigger smooth ramp
	}

	public void ProcessInterleaved(float[] buffer, int offset, int count, int channels)
	{
		if (count <= 0 || channels <= 0) return;
		if (_mode == ReplayGainMode.Off && Math.Abs(_currentGainDb) < 0.001f && Math.Abs(_targetGainDb) < 0.001f) return;

		int frames = count / channels;
		float startGainDb = _currentGainDb;

		for (int frame = 0; frame < frames; frame++)
		{
			if (_rampPosition < _rampSamples)
			{
				float t = (float)_rampPosition / _rampSamples;
				_currentGainDb = startGainDb + (_targetGainDb - startGainDb) * t;
				_rampPosition++;
			}
			else
			{
				_currentGainDb = _targetGainDb;
			}

			float linearGain = (float)Math.Pow(10.0, _currentGainDb / 20.0);
			int frameOffset = offset + frame * channels;

			for (int ch = 0; ch < channels; ch++)
			{
				buffer[frameOffset + ch] *= linearGain;
			}
		}
	}
}
