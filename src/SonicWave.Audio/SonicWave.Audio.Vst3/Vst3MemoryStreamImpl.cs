using System;
using System.IO;
using System.Runtime.InteropServices;

namespace SonicWave.Audio.Vst3;

internal sealed class Vst3MemoryStreamImpl : IBStreamCom
{
	private readonly MemoryStream _ms = new MemoryStream();

	public int Read(nint buffer, int numBytes, nint numBytesRead)
	{
		try
		{
			byte[] array = new byte[Math.Max(0, numBytes)];
			int num = _ms.Read(array, 0, array.Length);
			if (buffer != IntPtr.Zero && num > 0)
			{
				Marshal.Copy(array, 0, buffer, num);
			}
			if (numBytesRead != IntPtr.Zero)
			{
				Marshal.WriteInt32(numBytesRead, num);
			}
		}
		catch
		{
		}
		return 0;
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
				Marshal.WriteInt32(numBytesWritten, numBytes);
			}
		}
		catch
		{
		}
		return 0;
	}

	public int Seek(long pos, int mode, nint result)
	{
		try
		{
			long num = Math.Clamp(mode switch
			{
				0 => pos, 
				1 => _ms.Position + pos, 
				2 => _ms.Length + pos, 
				_ => _ms.Position, 
			}, 0L, _ms.Length);
			_ms.Position = num;
			if (result != IntPtr.Zero)
			{
				Marshal.WriteInt64(result, num);
			}
		}
		catch
		{
		}
		return 0;
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
