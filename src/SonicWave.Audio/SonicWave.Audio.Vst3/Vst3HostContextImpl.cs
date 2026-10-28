using System;
using System.Runtime.InteropServices;
using System.Text;

namespace SonicWave.Audio.Vst3;

[ComVisible(true)]
[ClassInterface(ClassInterfaceType.None)]
public sealed class Vst3HostContextImpl : IHostApplicationCom, IComponentHandlerCom, IPlugFrameCom
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

	private static readonly byte[] s_msgGuid = Vst3Ids.IMessage.ToByteArray();
	private static readonly byte[] s_msgInline = new byte[] { 0x93, 0x6F, 0x03, 0x3B, 0xC6, 0xC0, 0x47, 0xDB, 0xBB, 0x08, 0x82, 0xF8, 0x13, 0xC1, 0xE6, 0x13 };

	private static readonly byte[] s_attrGuid = Vst3Ids.IAttributeList.ToByteArray();
	private static readonly byte[] s_attrInline = new byte[] { 0x1E, 0x5F, 0x0A, 0xEB, 0xCC, 0x7F, 0x45, 0x33, 0xA2, 0x54, 0x40, 0x11, 0x38, 0xAD, 0x5E, 0xE4 };

	private static readonly byte[] s_streamGuid = Vst3Ids.IBStream.ToByteArray();
	private static readonly byte[] s_streamInline = new byte[] { 0xC3, 0xBF, 0x6E, 0xA2, 0x30, 0x99, 0x47, 0x52, 0x9B, 0x6B, 0xF9, 0x90, 0x1E, 0xE3, 0x3E, 0x9B };

	private static bool MatchUid(byte[] bytes, byte[] guidBytes, byte[] inlineBytes)
	{
		if (bytes == null || bytes.Length < 16) return false;
		var span = bytes.AsSpan(0, 16);
		return span.SequenceEqual(guidBytes) || span.SequenceEqual(inlineBytes);
	}

	public int CreateInstance(nint cid, nint iid, out nint obj)
	{
		obj = IntPtr.Zero;
		try
		{
			byte[] cidBytes = new byte[16];
			if (cid != IntPtr.Zero) Marshal.Copy(cid, cidBytes, 0, 16);
			byte[] iidBytes = new byte[16];
			if (iid != IntPtr.Zero) Marshal.Copy(iid, iidBytes, 0, 16);

			bool isMsg = MatchUid(cidBytes, s_msgGuid, s_msgInline) || MatchUid(iidBytes, s_msgGuid, s_msgInline);
			if (isMsg)
			{
				obj = Vst3Com.ToNative((IMessageCom)new Vst3MessageImpl());
				return 0;
			}

			bool isStream = MatchUid(cidBytes, s_streamGuid, s_streamInline) || MatchUid(iidBytes, s_streamGuid, s_streamInline);
			if (isStream)
			{
				obj = Vst3Com.ToNative((IBStreamCom)new Vst3MemoryStreamImpl());
				return 0;
			}

			bool isAttr = MatchUid(cidBytes, s_attrGuid, s_attrInline) || MatchUid(iidBytes, s_attrGuid, s_attrInline);
			if (isAttr)
			{
				obj = Vst3Com.ToNative((IAttributeListCom)new Vst3AttributeListImpl());
				return 0;
			}

			Console.WriteLine($"[echo-vst3-host] HostContext CreateInstance unrecognized: cid={BitConverter.ToString(cidBytes)} iid={BitConverter.ToString(iidBytes)}");
		}
		catch (Exception ex)
		{
			Console.WriteLine("[echo-vst3-host] HostContext CreateInstance ex: " + ex.Message);
		}
		return -2147467262; // E_NOINTERFACE
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
		Console.WriteLine($"[echo-vst3-host] RestartComponent flags: 0x{flags:X8}");
		if ((flags & 1) != 0 || (flags & 4) != 0) // kReloadComponent or kParamValuesChanged
		{
			try { _owner.SyncComponentStateToController(); } catch {}
		}
		return 0;
	}

	public int ResizeView(nint view, nint newSize)
	{
		if (newSize == IntPtr.Zero) return 0;
		try
		{
			_owner.RaiseEditorResize(newSize);
		}
		catch
		{
		}
		return 0;
	}
}
