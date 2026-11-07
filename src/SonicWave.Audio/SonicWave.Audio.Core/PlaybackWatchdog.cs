using System;
using System.Diagnostics;

namespace SonicWave.Audio.Core;

/// <summary>
/// 播放卡死监控看门狗。
/// 专为本地及流媒体音频解码、WASAPI缓冲设计，杜绝因缓冲阶梯更新导致的假阳性报警。
/// 仅当音频处于 Playing 状态且连续较长时间（如 5 秒）位置绝对不发生任何更新时才判定死锁。
/// </summary>
internal sealed class PlaybackWatchdog
{
	private readonly TimeSpan _stallTimeout;
	private readonly TimeSpan _initialGracePeriod;
	private readonly TimeSpan _positionEpsilon;

	private TimeSpan _lastPosition = TimeSpan.Zero;
	private long _lastProgressTimestamp;
	private long _playbackStartTimestamp;
	private int _stalledTicks;

	public PlaybackWatchdog(TimeSpan stallTimeout, TimeSpan initialGracePeriod, TimeSpan? positionEpsilon = null)
	{
		_stallTimeout = stallTimeout <= TimeSpan.Zero ? TimeSpan.FromSeconds(5.0) : stallTimeout;
		_initialGracePeriod = initialGracePeriod <= TimeSpan.Zero ? TimeSpan.FromSeconds(3.0) : initialGracePeriod;
		_positionEpsilon = positionEpsilon ?? TimeSpan.FromMilliseconds(1.0);
		Reset(TimeSpan.Zero);
	}

	/// <summary>
	/// 兼容旧版构造函数签名，但内部使用稳健卡死监控阈值。
	/// </summary>
	public PlaybackWatchdog(TimeSpan epsilon, int maxStalledChecks)
		: this(TimeSpan.FromSeconds(5.0), TimeSpan.FromSeconds(3.0), TimeSpan.FromMilliseconds(1.0))
	{
	}

	public void Reset(TimeSpan position)
	{
		_lastPosition = (position < TimeSpan.Zero) ? TimeSpan.Zero : position;
		long now = Stopwatch.GetTimestamp();
		_lastProgressTimestamp = now;
		_playbackStartTimestamp = now;
		_stalledTicks = 0;
	}

	public bool CheckStalled(TimeSpan position)
	{
		if (position < TimeSpan.Zero)
		{
			position = TimeSpan.Zero;
		}

		long now = Stopwatch.GetTimestamp();

		// 1. 播放初始缓冲宽限期（前 3 秒）：严禁看门狗触发强行停止
		double elapsedSinceStartSec = (double)(now - _playbackStartTimestamp) / Stopwatch.Frequency;
		if (elapsedSinceStartSec < _initialGracePeriod.TotalSeconds || position < _initialGracePeriod)
		{
			_lastPosition = position;
			_lastProgressTimestamp = now;
			_stalledTicks = 0;
			return false;
		}

		// 2. 检查播放进度是否有真实前进（只要变化超过 1ms，说明解码与音频硬件均在正常吐出数据）
		double posDeltaMs = (position - _lastPosition).TotalMilliseconds;
		if (posDeltaMs >= _positionEpsilon.TotalMilliseconds)
		{
			_lastPosition = position;
			_lastProgressTimestamp = now;
			_stalledTicks = 0;
			return false;
		}

		// 如果因为外部 Seek 或循环播放重置导致位置回退（差距超过 500ms），刷新基准
		if (posDeltaMs < -500.0)
		{
			_lastPosition = position;
			_lastProgressTimestamp = now;
			_stalledTicks = 0;
			return false;
		}

		// 3. 采样未检测到推进（可能处于 100~200ms 的 WASAPI 缓冲区间隙中，或驱动死锁）
		_stalledTicks++;

		// 4. 计算自上次进度发生变化以来的绝对持续时间
		double stalledDurationSec = (double)(now - _lastProgressTimestamp) / Stopwatch.Frequency;

		// 只有在连续未前进超过设定超时（例如 5 秒），且经过多次周期采样确认不是偶发抖动，才判定为卡死
		if (stalledDurationSec >= _stallTimeout.TotalSeconds && _stalledTicks >= 10)
		{
			return true;
		}

		return false;
	}
}
