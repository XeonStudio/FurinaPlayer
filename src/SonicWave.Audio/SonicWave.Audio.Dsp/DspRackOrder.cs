using System;
using System.Collections.Generic;
using System.Linq;

namespace SonicWave.Audio.Dsp;

public enum DspRackModuleId
{
	Equalizer = 0,
	Convolution = 1,
	ReplayGain = 2,
	Compressor = 3,
	Crossfeed = 4,
	StereoField = 5,
	ChannelMatrix = 6,
	Headroom = 7,
	Resampler = 8,
	SdmModulator = 9
}

public sealed class DspRackOrder
{
	public static readonly DspRackModuleId[] DefaultOrder =
	{
		DspRackModuleId.ReplayGain,
		DspRackModuleId.Equalizer,
		DspRackModuleId.Convolution,
		DspRackModuleId.Compressor,
		DspRackModuleId.Crossfeed,
		DspRackModuleId.StereoField,
		DspRackModuleId.ChannelMatrix,
		DspRackModuleId.Headroom,
		DspRackModuleId.Resampler,
		DspRackModuleId.SdmModulator
	};

	private readonly List<DspRackModuleId> _order = new();
	private readonly Dictionary<DspRackModuleId, bool> _bypassed = new();
	private readonly object _lock = new();

	public IReadOnlyList<DspRackModuleId> Order
	{
		get
		{
			lock (_lock) return _order.ToList();
		}
	}

	public DspRackOrder()
	{
		ResetToDefault();
	}

	public void ResetToDefault()
	{
		lock (_lock)
		{
			_order.Clear();
			_order.AddRange(DefaultOrder);
			foreach (var mod in DefaultOrder)
			{
				_bypassed[mod] = false;
			}
		}
	}

	public void SetOrder(IEnumerable<DspRackModuleId> order)
	{
		if (order == null) return;
		lock (_lock)
		{
			var list = order.Distinct().ToList();
			// Add any missing modules at the end
			foreach (var mod in DefaultOrder)
			{
				if (!list.Contains(mod))
				{
					list.Add(mod);
				}
			}
			_order.Clear();
			_order.AddRange(list);
		}
	}

	public void SetBypassed(DspRackModuleId module, bool bypassed)
	{
		lock (_lock)
		{
			_bypassed[module] = bypassed;
		}
	}

	public bool IsBypassed(DspRackModuleId module)
	{
		lock (_lock)
		{
			return _bypassed.TryGetValue(module, out bool b) && b;
		}
	}

	public static string GetModuleName(DspRackModuleId module) => module switch
	{
		DspRackModuleId.Equalizer => "EQ 均衡器 (10段/双二阶)",
		DspRackModuleId.Convolution => "房间校正 / 耳机 FIR 卷积",
		DspRackModuleId.ReplayGain => "ReplayGain 回放增益",
		DspRackModuleId.Compressor => "动态压缩器 (Soft-Knee)",
		DspRackModuleId.Crossfeed => "Crossfeed 双耳互馈 (Bauer)",
		DspRackModuleId.StereoField => "声场扩展 / M/S 平衡",
		DspRackModuleId.ChannelMatrix => "声道矩阵 (2x2 路由)",
		DspRackModuleId.Headroom => "Headroom 余量衰减",
		DspRackModuleId.Resampler => "升频采样转换 (最高 384kHz)",
		DspRackModuleId.SdmModulator => "PCM 转 DSD 调制器 (SDM)",
		_ => module.ToString()
	};
}
