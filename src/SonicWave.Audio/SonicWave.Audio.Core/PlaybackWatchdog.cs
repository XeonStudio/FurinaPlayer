using System;

namespace SonicWave.Audio.Core;

internal sealed class PlaybackWatchdog
{
	private readonly TimeSpan _epsilon;

	private readonly int _maxStalledChecks;

	private TimeSpan _lastPosition;

	private int _stalledChecks;

	public PlaybackWatchdog(TimeSpan epsilon, int maxStalledChecks)
	{
		_epsilon = ((epsilon < TimeSpan.Zero) ? TimeSpan.Zero : epsilon);
		_maxStalledChecks = Math.Max(1, maxStalledChecks);
	}

	public void Reset(TimeSpan position)
	{
		_lastPosition = ((position < TimeSpan.Zero) ? TimeSpan.Zero : position);
		_stalledChecks = 0;
	}

	public bool CheckStalled(TimeSpan position)
	{
		if (position < TimeSpan.Zero)
		{
			position = TimeSpan.Zero;
		}
		if (position > _lastPosition + _epsilon)
		{
			_lastPosition = position;
			_stalledChecks = 0;
			return false;
		}
		_stalledChecks++;
		return _stalledChecks >= _maxStalledChecks;
	}
}
