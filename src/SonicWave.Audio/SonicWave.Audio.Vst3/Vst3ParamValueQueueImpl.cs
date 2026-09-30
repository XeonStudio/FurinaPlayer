using System;

namespace SonicWave.Audio.Vst3;

internal sealed class Vst3ParamValueQueueImpl : IParamValueQueueCom
{
	private readonly uint _id;

	private readonly Func<double> _getValue;

	private readonly object _lock = new object();

	public Vst3ParamValueQueueImpl(uint id, Func<double> getValue)
	{
		_id = id;
		_getValue = getValue;
	}

	public uint GetParameterId()
	{
		Vst3NativeInstance.Db("IParamValueQueue.GetParameterId -> " + _id);
		return _id;
	}

	public int GetPointCount()
	{
		Vst3NativeInstance.Db("IParamValueQueue.GetPointCount");
		return 1;
	}

	public int GetPoint(int index, out int sampleOffset, out double value)
	{
		Vst3NativeInstance.Db("IParamValueQueue.GetPoint(" + index + ")");
		sampleOffset = 0;
		value = 0.0;
		if (index != 0)
		{
			return 1;
		}
		lock (_lock)
		{
			value = _getValue();
		}
		return 0;
	}

	public int AddPoint(int sampleOffset, double value, out int index)
	{
		Vst3NativeInstance.Db("IParamValueQueue.AddPoint(" + sampleOffset + ", " + value + ")");
		index = -1;
		return 1;
	}
}
