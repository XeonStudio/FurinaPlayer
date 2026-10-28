using System;
using System.Runtime.InteropServices;

namespace SonicWave.Audio.Vst3;

[ComVisible(true)]
[ClassInterface(ClassInterfaceType.None)]
internal sealed class Vst3MessageImpl : IMessageCom, IDisposable
{
	private readonly Vst3AttributeListImpl _attributes = new Vst3AttributeListImpl();
	private nint _attributesNative = IntPtr.Zero;
	private nint _idPtr = IntPtr.Zero;

	public Vst3AttributeListImpl Attributes => _attributes;

	public Vst3MessageImpl()
	{
		_idPtr = Marshal.StringToHGlobalAnsi("");
		_attributesNative = Vst3Com.ToNative((IAttributeListCom)_attributes);
	}

	public nint GetMessageID()
	{
		return _idPtr;
	}

	public void SetMessageID(nint id)
	{
		if (_idPtr != IntPtr.Zero)
		{
			Marshal.FreeHGlobal(_idPtr);
			_idPtr = IntPtr.Zero;
		}
		if (id == IntPtr.Zero)
		{
			_idPtr = Marshal.StringToHGlobalAnsi("");
			return;
		}
		string? s = Marshal.PtrToStringAnsi(id);
		_idPtr = Marshal.StringToHGlobalAnsi(s ?? "");
	}

	public nint GetAttributes()
	{
		if (_attributesNative == IntPtr.Zero)
		{
			_attributesNative = Vst3Com.ToNative((IAttributeListCom)_attributes);
		}
		return _attributesNative;
	}

	public void Dispose()
	{
		if (_attributesNative != IntPtr.Zero)
		{
			try { Marshal.Release(_attributesNative); } catch {}
			_attributesNative = IntPtr.Zero;
		}
		if (_idPtr != IntPtr.Zero)
		{
			Marshal.FreeHGlobal(_idPtr);
			_idPtr = IntPtr.Zero;
		}
	}

	~Vst3MessageImpl()
	{
		Dispose();
	}
}
