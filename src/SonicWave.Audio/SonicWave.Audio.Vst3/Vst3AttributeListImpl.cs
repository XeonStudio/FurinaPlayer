using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace SonicWave.Audio.Vst3;

internal sealed class Vst3AttributeListImpl : IAttributeListCom
{
	private readonly Dictionary<string, object> _data = new Dictionary<string, object>(StringComparer.Ordinal);

	public int SetInt(nint id, long value)
	{
		lock (_data)
		{
			_data[ReadId(id)] = value;
		}
		return 0;
	}

	public int GetInt(nint id, out long value)
	{
		lock (_data)
		{
			if (_data.TryGetValue(ReadId(id), out object value2) && value2 is long num)
			{
				value = num;
				return 0;
			}
		}
		value = 0L;
		return 1;
	}

	public int SetFloat(nint id, double value)
	{
		lock (_data)
		{
			_data[ReadId(id)] = value;
		}
		return 0;
	}

	public int GetFloat(nint id, out double value)
	{
		lock (_data)
		{
			if (_data.TryGetValue(ReadId(id), out object value2) && value2 is double num)
			{
				value = num;
				return 0;
			}
		}
		value = 0.0;
		return 1;
	}

	public int SetString(nint id, nint str)
	{
		try
		{
			List<char> list = new List<char>();
			for (int i = 0; i < 511; i++)
			{
				char c = (char)Marshal.ReadInt16(str, i * 2);
				if (c == '\0')
				{
					break;
				}
				list.Add(c);
			}
			lock (_data)
			{
				_data[ReadId(id)] = new string(list.ToArray());
			}
		}
		catch
		{
		}
		return 0;
	}

	public int GetString(nint id, nint str, uint sizeInBytes)
	{
		try
		{
			string s;
			lock (_data)
			{
				if (!_data.TryGetValue(ReadId(id), out object value) || !(value is string text))
				{
					return 1;
				}
				s = text;
			}
			byte[] array = Encoding.Unicode.GetBytes(s);
			int num = (int)(sizeInBytes - 2);
			if (array.Length > num)
			{
				array = array[..num];
			}
			Marshal.Copy(array, 0, str, array.Length);
			Marshal.WriteInt16(str, array.Length, 0);
			return 0;
		}
		catch
		{
			return 1;
		}
	}

	public int SetBinary(nint id, nint data, uint sizeInBytes)
	{
		try
		{
			byte[] array = new byte[sizeInBytes];
			Marshal.Copy(data, array, 0, (int)sizeInBytes);
			lock (_data)
			{
				_data[ReadId(id)] = array;
			}
			return 0;
		}
		catch
		{
			return 1;
		}
	}

	public int GetBinary(nint id, out nint data, out uint sizeInBytes)
	{
		data = IntPtr.Zero;
		sizeInBytes = 0u;
		lock (_data)
		{
			if (_data.TryGetValue(ReadId(id), out object value) && value is byte[] array)
			{
				data = GCHandle.Alloc(array, GCHandleType.Pinned).AddrOfPinnedObject();
				sizeInBytes = (uint)array.Length;
			}
		}
		return (data == IntPtr.Zero) ? 1 : 0;
	}

	private static string ReadId(nint id)
	{
		List<byte> list = new List<byte>();
		for (int i = 0; i < 127; i++)
		{
			byte b = Marshal.ReadByte(id, i);
			if (b == 0)
			{
				break;
			}
			list.Add(b);
		}
		return Encoding.ASCII.GetString(list.ToArray());
	}
}
