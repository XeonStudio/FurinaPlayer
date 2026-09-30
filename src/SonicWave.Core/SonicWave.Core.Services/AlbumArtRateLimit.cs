using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace SonicWave.Core.Services;

public static class AlbumArtRateLimit
{
	private static readonly object Gate = new object();

	private static readonly List<DateTime> Recent = new List<DateTime>();

	private static DateTime _lastUtc = DateTime.MinValue;

	public static int MinIntervalSeconds { get; set; } = 20;

	public static int MaxPerMinute { get; set; } = 2;

	public static async Task<bool> WaitForSlotAsync(TimeSpan maxWait, CancellationToken ct = default(CancellationToken))
	{
		DateTime deadline = DateTime.UtcNow + maxWait;
		while (true)
		{
			TimeSpan timeSpan = NextWait(DateTime.UtcNow);
			if (timeSpan <= TimeSpan.Zero)
			{
				lock (Gate)
				{
					timeSpan = NextWait(DateTime.UtcNow);
					if (timeSpan <= TimeSpan.Zero)
					{
						RecordOperation(DateTime.UtcNow);
						return true;
					}
				}
				continue;
			}
			TimeSpan timeSpan2 = deadline - DateTime.UtcNow;
			if (timeSpan2 <= TimeSpan.Zero)
			{
				break;
			}
			TimeSpan delay = ((timeSpan < timeSpan2) ? timeSpan : timeSpan2);
			try
			{
				await Task.Delay(delay, ct).ConfigureAwait(continueOnCapturedContext: false);
			}
			catch (OperationCanceledException)
			{
				return false;
			}
		}
		return false;
	}

	public static TimeSpan NextWait(DateTime nowUtc)
	{
		lock (Gate)
		{
			Trim(nowUtc);
			double num = 0.0;
			if (_lastUtc != DateTime.MinValue)
			{
				double totalSeconds = (nowUtc - _lastUtc).TotalSeconds;
				if (totalSeconds < (double)MinIntervalSeconds)
				{
					num = Math.Max(num, (double)MinIntervalSeconds - totalSeconds);
				}
			}
			if (Recent.Count >= MaxPerMinute)
			{
				num = Math.Max(num, (Recent[0].AddSeconds(60.0) - nowUtc).TotalSeconds);
			}
			return TimeSpan.FromSeconds(num);
		}
	}

	public static void RecordOperation(DateTime nowUtc)
	{
		lock (Gate)
		{
			Recent.Add(nowUtc);
			_lastUtc = nowUtc;
			Trim(nowUtc);
		}
	}

	public static void Reset()
	{
		lock (Gate)
		{
			Recent.Clear();
			_lastUtc = DateTime.MinValue;
		}
	}

	public static string StatusText()
	{
		lock (Gate)
		{
			TimeSpan timeSpan = NextWait(DateTime.UtcNow);
			return (timeSpan <= TimeSpan.Zero) ? "可执行" : $"限流中：还需等待 {Math.Ceiling(timeSpan.TotalSeconds)} 秒（每分钟最多 2 次，间隔至少 20 秒）";
		}
	}

	private static void Trim(DateTime nowUtc)
	{
		DateTime cutoff = nowUtc.AddSeconds(-60.0);
		Recent.RemoveAll((DateTime t) => t <= cutoff);
	}
}
