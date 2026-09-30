using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace SonicWave.Audio.Vst3;

internal sealed class Vst3MessageImpl : IMessageCom
{
	private readonly Vst3AttributeListImpl _attributes = new Vst3AttributeListImpl();

	private byte[] _id = Encoding.ASCII.GetBytes("changed\0");

	public Vst3AttributeListImpl Attributes => _attributes;

	public nint GetMessageID()
	{
		lock (_attributes)
		{
			GCHandle gCHandle = GCHandle.Alloc(_id, GCHandleType.Pinned);
			nint result = gCHandle.AddrOfPinnedObject();
			gCHandle.Free();
			return result;
		}
	}

	public void SetMessageID(nint id)
	{
		lock (_attributes)
		{
			if (id == IntPtr.Zero)
			{
				_id = Encoding.ASCII.GetBytes("changed\0");
				return;
			}
			List<byte> list = new List<byte>();
			for (int i = 0; i < 255; i++)
			{
				byte b = Marshal.ReadByte(id, i);
				if (b == 0)
				{
					break;
				}
				list.Add(b);
			}
			list.Add(0);
			_id = list.ToArray();
		}
	}

	public nint GetAttributes()
	{
		lock (_attributes)
		{
			return Vst3Com.ToNative((IAttributeListCom)_attributes);
		}
	}
}
