using System.Runtime.InteropServices;

namespace SonicWave.Audio.Vst3;

internal struct PClassInfo
{
	public unsafe fixed byte Cid[16];

	public int Cardinality;

	[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
	public string Category;

	[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 64)]
	public string Name;
}
