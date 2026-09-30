using System;
using System.Collections.Generic;

namespace SonicWave.Audio.Vst3;

internal sealed class Vst3ParameterChangesImpl : IParameterChangesCom
{
	private sealed class QueueEntry
	{
		public uint Id;

		public double Value;
	}

	private readonly object _lock = new object();

	private readonly List<QueueEntry> _entries = new List<QueueEntry>();

	public void SetValue(uint id, double value)
	{
		lock (_lock)
		{
			foreach (QueueEntry entry in _entries)
			{
				if (entry.Id == id)
				{
					entry.Value = value;
					return;
				}
			}
			_entries.Add(new QueueEntry
			{
				Id = id,
				Value = value
			});
		}
	}

	public int GetParameterCount()
	{
		Vst3NativeInstance.Db("IParameterChanges.GetParameterCount -> " + _entries.Count);
		lock (_lock)
		{
			return _entries.Count;
		}
	}

	public nint GetParameterData(int index)
	{
		Vst3NativeInstance.Db("IParameterChanges.GetParameterData(" + index + ")");
		QueueEntry e;
		lock (_lock)
		{
			if (index < 0 || index >= _entries.Count)
			{
				return IntPtr.Zero;
			}
			e = _entries[index];
		}
		return Vst3Com.ToNative((IParamValueQueueCom)new Vst3ParamValueQueueImpl(e.Id, () => e.Value));
	}

	public nint AddParameterData(ref uint id, out int index)
	{
		Vst3NativeInstance.Db("IParameterChanges.AddParameterData(id=" + id + ")");
		lock (_lock)
		{
			foreach (QueueEntry en in _entries)
			{
				if (en.Id == id)
				{
					index = _entries.IndexOf(en);
					return Vst3Com.ToNative(new Vst3ParamValueQueueImpl(en.Id, () => en.Value));
				}
			}
			QueueEntry e = new QueueEntry
			{
				Id = id,
				Value = 0.0
			};
			_entries.Add(e);
			index = _entries.Count - 1;
			return Vst3Com.ToNative(new Vst3ParamValueQueueImpl(e.Id, () => e.Value));
		}
	}
}
