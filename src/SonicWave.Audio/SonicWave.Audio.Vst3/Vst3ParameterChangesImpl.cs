using System;
using System.Collections.Generic;

namespace SonicWave.Audio.Vst3;

internal sealed class Vst3ParameterChangesImpl : IParameterChangesCom
{
	private sealed class QueueEntry
	{
		public uint Id;
		public Vst3ParamValueQueueImpl Queue = null!;
		public nint QueueCcw = IntPtr.Zero;
	}

	private readonly object _lock = new object();
	private readonly List<QueueEntry> _entries = new List<QueueEntry>();

	public void Clear()
	{
		lock (_lock)
		{
			foreach (var e in _entries)
			{
				if (e.QueueCcw != IntPtr.Zero)
				{
					try { System.Runtime.InteropServices.Marshal.Release(e.QueueCcw); } catch {}
					e.QueueCcw = IntPtr.Zero;
				}
			}
			_entries.Clear();
		}
	}

	public void SetValue(uint id, double value)
	{
		lock (_lock)
		{
			foreach (QueueEntry entry in _entries)
			{
				if (entry.Id == id)
				{
					entry.Queue.AddPoint(0, value, out _);
					return;
				}
			}
			var q = new Vst3ParamValueQueueImpl(id, () => value);
			nint ccw = Vst3Com.ToNative((IParamValueQueueCom)q);
			_entries.Add(new QueueEntry
			{
				Id = id,
				Queue = q,
				QueueCcw = ccw
			});
		}
	}

	public int GetParameterCount()
	{
		lock (_lock)
		{
			return _entries.Count;
		}
	}

	public nint GetParameterData(int index)
	{
		lock (_lock)
		{
			if (index < 0 || index >= _entries.Count)
			{
				return IntPtr.Zero;
			}
			var e = _entries[index];
			if (e.QueueCcw != IntPtr.Zero)
			{
				System.Runtime.InteropServices.Marshal.AddRef(e.QueueCcw);
				return e.QueueCcw;
			}
			e.QueueCcw = Vst3Com.ToNative((IParamValueQueueCom)e.Queue);
			return e.QueueCcw;
		}
	}

	public nint AddParameterData(ref uint id, out int index)
	{
		lock (_lock)
		{
			for (int i = 0; i < _entries.Count; i++)
			{
				if (_entries[i].Id == id)
				{
					index = i;
					if (_entries[i].QueueCcw != IntPtr.Zero)
					{
						System.Runtime.InteropServices.Marshal.AddRef(_entries[i].QueueCcw);
						return _entries[i].QueueCcw;
					}
					_entries[i].QueueCcw = Vst3Com.ToNative((IParamValueQueueCom)_entries[i].Queue);
					return _entries[i].QueueCcw;
				}
			}
			var q = new Vst3ParamValueQueueImpl(id);
			nint ccw = Vst3Com.ToNative((IParamValueQueueCom)q);
			_entries.Add(new QueueEntry
			{
				Id = id,
				Queue = q,
				QueueCcw = ccw
			});
			index = _entries.Count - 1;
			System.Runtime.InteropServices.Marshal.AddRef(ccw);
			return ccw;
		}
	}

	public List<(uint id, double value)> DrainChanges()
	{
		var list = new List<(uint, double)>();
		lock (_lock)
		{
			if (_entries.Count == 0) return list;
			foreach (var e in _entries)
			{
				list.Add((e.Id, e.Queue.GetLastValue()));
			}
			Clear();
		}
		return list;
	}
}
