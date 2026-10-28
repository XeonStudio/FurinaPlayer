using System;
using System.IO;
using System.Runtime.InteropServices;

namespace SonicWave.Audio.Vst3;

[ComVisible(true)]
[ClassInterface(ClassInterfaceType.None)]
public sealed class Vst3MemoryStreamImpl : IBStreamCom
{
	private readonly MemoryStream _ms = new MemoryStream();

	public long Length => _ms.Length;

	public byte[] ToArray() => _ms.ToArray();

	public int Read(nint buffer, int numBytes, nint numBytesRead)
	{
		try
		{
			if (numBytes <= 0)
			{
				if (numBytesRead != IntPtr.Zero) Marshal.WriteInt32(numBytesRead, 0);
				return 0;
			}
			byte[] array = new byte[numBytes];
			int num = _ms.Read(array, 0, numBytes);
			if (buffer != IntPtr.Zero && num > 0)
			{
				Marshal.Copy(array, 0, buffer, num);
			}
			if (numBytesRead != IntPtr.Zero)
			{
				Marshal.WriteInt32(numBytesRead, num);
			}
			return 0;
		}
		catch
		{
			return -2147467259; // E_FAIL
		}
	}

	public int Write(nint buffer, int numBytes, nint numBytesWritten)
	{
		try
		{
			if (buffer != IntPtr.Zero && numBytes > 0)
			{
				byte[] array = new byte[numBytes];
				Marshal.Copy(buffer, array, 0, numBytes);
				_ms.Write(array, 0, numBytes);
			}
			if (numBytesWritten != IntPtr.Zero)
			{
				Marshal.WriteInt32(numBytesWritten, Math.Max(0, numBytes));
			}
			return 0;
		}
		catch
		{
			return -2147467259; // E_FAIL
		}
	}

	public int Seek(long pos, int mode, nint result)
	{
		try
		{
			long newPos = mode switch
			{
				0 => pos, // kIBSeekSet
				1 => _ms.Position + pos, // kIBSeekCur
				2 => _ms.Length + pos, // kIBSeekEnd
				_ => _ms.Position,
			};
			if (newPos < 0) newPos = 0;
			if (newPos > _ms.Length)
			{
				_ms.SetLength(newPos);
			}
			_ms.Position = newPos;
			if (result != IntPtr.Zero)
			{
				Marshal.WriteInt64(result, newPos);
			}
			return 0;
		}
		catch
		{
			return -2147467259; // E_FAIL
		}
	}

	public int Tell(nint pos)
	{
		if (pos != IntPtr.Zero)
		{
			Marshal.WriteInt64(pos, _ms.Position);
		}
		return 0;
	}

	public void Reset()
	{
		_ms.SetLength(0L);
		_ms.Position = 0L;
	}
}
