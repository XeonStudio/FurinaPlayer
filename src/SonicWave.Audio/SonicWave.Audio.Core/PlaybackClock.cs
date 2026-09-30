using System;

namespace SonicWave.Audio.Core;

internal sealed class PlaybackClock
{
	private static readonly TimeSpan BackwardBlipTolerance = TimeSpan.FromSeconds(2.0);

	private TimeSpan _lastPosition;

	private bool _hasBaseline;

	public TimeSpan LastPosition => _lastPosition;

	public void Reset(TimeSpan position)
	{
		_lastPosition = ((position < TimeSpan.Zero) ? TimeSpan.Zero : position);
		_hasBaseline = true;
	}

	public void Rebase(TimeSpan position)
	{
		_lastPosition = ((position < TimeSpan.Zero) ? TimeSpan.Zero : position);
		_hasBaseline = true;
	}

	public TimeSpan Guard(TimeSpan raw, bool seekPending)
	{
		if (raw < TimeSpan.Zero)
		{
			raw = TimeSpan.Zero;
		}
		if (!_hasBaseline)
		{
			_lastPosition = raw;
			_hasBaseline = true;
			return raw;
		}
		if (seekPending)
		{
			_lastPosition = raw;
			return raw;
		}
		if (raw < _lastPosition - BackwardBlipTolerance)
		{
			return _lastPosition;
		}
		_lastPosition = raw;
		return raw;
	}
}
