using System;
using System.Runtime.InteropServices;
using System.Text;

namespace SonicWave.Audio.Vst3;

internal sealed class Vst3HostContextImpl : IHostApplicationCom, IComponentHandlerCom, IPlugFrameCom
{
	private readonly Vst3NativeInstance _owner;

	private readonly byte[] _hostNameBytes = Encoding.Unicode.GetBytes("Furina Player\0");

	public Vst3HostContextImpl(Vst3NativeInstance owner)
	{
		_owner = owner;
	}

	public int GetName(nint name)
	{
		try
		{
			if (name != IntPtr.Zero)
			{
				Marshal.Copy(_hostNameBytes, 0, name, Math.Min(_hostNameBytes.Length, 256));
			}
		}
		catch
		{
		}
		return 0;
	}

	public int CreateInstance(nint cid, nint iid, out nint obj)
	{
		obj = IntPtr.Zero;
		try
		{
			byte[] array = new byte[16];
			Marshal.Copy(cid, array, 0, 16);
			if (array.AsSpan().SequenceEqual(Vst3Com.GetGuidBytes(Vst3Ids.IMessage)))
			{
				obj = Vst3Com.ToNative((IMessageCom)new Vst3MessageImpl());
				return 0;
			}
		}
		catch
		{
		}
		return -2147467262;
	}

	public int BeginEdit(uint id)
	{
		return 0;
	}

	public int PerformEdit(uint id, double valueNormalized)
	{
		_owner.QueueParameterChange(id, valueNormalized);
		return 0;
	}

	public int EndEdit(uint id)
	{
		return 0;
	}

	public int RestartComponent(int flags)
	{
		return 0;
	}

	public int ResizeView(nint view, nint newSize)
	{
		_owner.RaiseEditorResize(newSize);
		return 0;
	}
}
