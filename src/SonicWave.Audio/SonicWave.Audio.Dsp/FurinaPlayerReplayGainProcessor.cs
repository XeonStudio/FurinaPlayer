using System;

namespace SonicWave.Audio.Dsp;

public enum ReplayGainMode
{
	Off = 0,
	Track = 1,
	Album = 2,
	Auto = 3
}

/// <summary>
/// FurinaPlayer 专业级 ITU-R BS.1770-4 / EBU R128 音量感知均衡处理器。
/// 支持单曲、专辑与智能自动对齐、无缝平滑斜坡增益切换、防削波限幅及未知响度平滑保护。
/// </summary>
public class FurinaPlayerReplayGainProcessor
{
	private double _sampleRate = 44100.0;
	private ReplayGainMode _mode = ReplayGainMode.Track;
	private float _targetLufs = -14.0f;
	private float _trackGainDb = 0.0f;
	private float _trackPeak = 1.0f;
	private float _trackLufs = -70.0f;
	private float _albumGainDb = 0.0f;
	private float _albumPeak = 1.0f;
	private float _albumLufs = -70.0f;
	private float _preampDb = 0.0f;
	private float _unknownLoudnessDb = -5.0f;
	private bool _preventClipping = true;
	private bool _isAdjacentAlbumTrack = false;

	private float _currentGainDb = 0.0f;
	private float _targetGainDb = 0.0f;
	private int _rampSamples = 441;
	private int _rampPosition = 441;

	public ReplayGainMode Mode
	{
		get => _mode;
		set { _mode = value; UpdateTargetGain(); }
	}

	public float TargetLufs
	{
		get => _targetLufs;
		set { _targetLufs = value; UpdateTargetGain(); }
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

	public float TrackLufs
	{
		get => _trackLufs;
		set { _trackLufs = value; UpdateTargetGain(); }
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

	public float AlbumLufs
	{
		get => _albumLufs;
		set { _albumLufs = value; UpdateTargetGain(); }
	}

	public float PreampDb
	{
		get => _preampDb;
		set { _preampDb = value; UpdateTargetGain(); }
	}

	public float UnknownLoudnessDb
	{
		get => _unknownLoudnessDb;
		set { _unknownLoudnessDb = value; UpdateTargetGain(); }
	}

	public bool PreventClipping
	{
		get => _preventClipping;
		set { _preventClipping = value; UpdateTargetGain(); }
	}

	public bool IsAdjacentAlbumTrack
	{
		get => _isAdjacentAlbumTrack;
		set { _isAdjacentAlbumTrack = value; UpdateTargetGain(); }
	}

	public float CurrentAppliedGainDb { get; private set; } = 0.0f;
	public ReplayGainMode EffectiveMode { get; private set; } = ReplayGainMode.Track;
	public string DataSource { get; private set; } = "无数据";
	public bool PreventedClippingActive { get; private set; } = false;

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

	public void UpdateTargetGain()
	{
		if (_mode == ReplayGainMode.Off)
		{
			_targetGainDb = 0.0f;
			CurrentAppliedGainDb = 0.0f;
			PreventedClippingActive = false;
			EffectiveMode = ReplayGainMode.Off;
			DataSource = "未开启";
			return;
		}

		ReplayGainMode effective = _mode;
		if (_mode == ReplayGainMode.Auto)
		{
			effective = (_isAdjacentAlbumTrack && (_albumLufs > -60.0f || Math.Abs(_albumGainDb) > 0.001f))
				? ReplayGainMode.Album
				: ReplayGainMode.Track;
		}
		EffectiveMode = effective;

		float baseGain = 0.0f;
		float peak = 1.0f;
		string source = "无数据";

		if (effective == ReplayGainMode.Album)
		{
			if (_albumLufs > -60.0f)
			{
				baseGain = _targetLufs - _albumLufs;
				peak = _albumPeak;
				source = "专辑增益";
			}
			else if (Math.Abs(_albumGainDb) > 0.001f)
			{
				baseGain = _albumGainDb;
				peak = _albumPeak;
				source = "专辑标签";
			}
			else if (_trackLufs > -60.0f)
			{
				baseGain = _targetLufs - _trackLufs;
				peak = _trackPeak;
				source = "测量 LUFS";
			}
			else if (Math.Abs(_trackGainDb) > 0.001f)
			{
				baseGain = _trackGainDb;
				peak = _trackPeak;
				source = "曲目标签";
			}
			else
			{
				baseGain = _unknownLoudnessDb;
				peak = _trackPeak;
				source = "未知响度保护";
			}
		}
		else
		{
			if (_trackLufs > -60.0f)
			{
				baseGain = _targetLufs - _trackLufs;
				peak = _trackPeak;
				source = "测量 LUFS";
			}
			else if (Math.Abs(_trackGainDb) > 0.001f)
			{
				baseGain = _trackGainDb;
				peak = _trackPeak;
				source = "曲目标签";
			}
			else
			{
				baseGain = _unknownLoudnessDb;
				peak = _trackPeak;
				source = "未知响度保护";
			}
		}

		// Preamp gain does not apply to unknown loudness fallback (align with standard ReplayGain rules)
		if (source != "未知响度保护")
		{
			baseGain += _preampDb;
		}

		bool clippingPrevented = false;
		if (_preventClipping && peak > 0.0001f)
		{
			float maxAllowedDb = (float)(-20.0 * Math.Log10(peak));
			if (baseGain > maxAllowedDb)
			{
				baseGain = maxAllowedDb;
				clippingPrevented = true;
			}
		}

		_targetGainDb = Math.Clamp(baseGain, -24.0f, 18.0f);
		CurrentAppliedGainDb = _targetGainDb;
		PreventedClippingActive = clippingPrevented;
		DataSource = source;
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
