using System;

namespace SonicWave.Audio;

internal sealed class VlcEndDetector
{
	private TimeSpan _lastPosition;

	private TimeSpan _lastDuration;

	private bool _seenNearEnd;

	public void Reset()
	{
		_lastPosition = TimeSpan.Zero;
		_lastDuration = TimeSpan.Zero;
		_seenNearEnd = false;
	}

	public bool Report(bool playing, TimeSpan position, TimeSpan duration)
	{
		bool seenNearEnd = _seenNearEnd;
		if (duration > TimeSpan.FromSeconds(1.0))
		{
			_lastDuration = duration;
			if (playing && position >= duration - TimeSpan.FromSeconds(6.0))
			{
				_seenNearEnd = true;
			}
		}
		bool result = false;
		if (!playing)
		{
			if (duration > TimeSpan.FromSeconds(1.0) && position >= duration - TimeSpan.FromSeconds(3.0))
			{
				result = true;
			}
			else if (seenNearEnd && position <= TimeSpan.FromSeconds(2.0))
			{
				result = true;
			}
			else if (seenNearEnd && duration <= TimeSpan.FromSeconds(1.0) && _lastDuration > TimeSpan.FromSeconds(1.0))
			{
				result = true;
			}
		}
		else if (seenNearEnd && _lastPosition >= _lastDuration - TimeSpan.FromSeconds(2.0) && position <= _lastPosition - TimeSpan.FromSeconds(3.0) && position <= TimeSpan.FromSeconds(3.0))
		{
			result = true;
		}
		_lastPosition = position;
		return result;
	}
}
