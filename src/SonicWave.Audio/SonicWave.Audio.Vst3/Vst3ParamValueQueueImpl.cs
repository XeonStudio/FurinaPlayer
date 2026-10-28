using System;

namespace SonicWave.Audio.Vst3;

internal sealed class Vst3ParamValueQueueImpl : IParamValueQueueCom
{
	private readonly uint _id;
	private readonly System.Collections.Generic.List<(int offset, double value)> _points = new System.Collections.Generic.List<(int, double)>();
	private readonly Func<double>? _getValue;
	private readonly object _lock = new object();

	public Vst3ParamValueQueueImpl(uint id, Func<double>? getValue = null)
	{
		_id = id;
		_getValue = getValue;
		if (_getValue != null)
		{
			_points.Add((0, _getValue()));
		}
	}

	public uint GetParameterId()
	{
		return _id;
	}

	public int GetPointCount()
	{
		lock (_lock)
		{
			if (_points.Count == 0 && _getValue != null) return 1;
			return _points.Count;
		}
	}

	public int GetPoint(int index, out int sampleOffset, out double value)
	{
		lock (_lock)
		{
			if (_points.Count == 0 && _getValue != null && index == 0)
			{
				sampleOffset = 0;
				value = _getValue();
				return 0;
			}
			if (index >= 0 && index < _points.Count)
			{
				sampleOffset = _points[index].offset;
				value = _points[index].value;
				return 0;
			}
			sampleOffset = 0;
			value = 0.0;
			return 1;
		}
	}

	public int AddPoint(int sampleOffset, double value, out int index)
	{
		lock (_lock)
		{
			_points.Add((sampleOffset, value));
			index = _points.Count - 1;
			return 0; // kResultOk
		}
	}

	public double GetLastValue()
	{
		lock (_lock)
		{
			if (_points.Count > 0) return _points[_points.Count - 1].value;
			if (_getValue != null) return _getValue();
			return 0.0;
		}
	}
}
