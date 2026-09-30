using System.Runtime.InteropServices;

namespace SonicWave.Audio.Vst3;

[ComVisible(true)]
[Guid("1E5F0AEB-CC7F-4533-A254-401138AD5EE4")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
public partial interface IAttributeListCom
{
	[PreserveSig]
	int SetInt(nint id, long value);

	[PreserveSig]
	int GetInt(nint id, out long value);

	[PreserveSig]
	int SetFloat(nint id, double value);

	[PreserveSig]
	int GetFloat(nint id, out double value);

	[PreserveSig]
	int SetString(nint id, nint str);

	[PreserveSig]
	int GetString(nint id, nint str, uint sizeInBytes);

	[PreserveSig]
	int SetBinary(nint id, nint data, uint sizeInBytes);

	[PreserveSig]
	int GetBinary(nint id, out nint data, out uint sizeInBytes);
}


