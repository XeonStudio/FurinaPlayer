using System.Runtime.InteropServices;

namespace SonicWave.Audio.Vst3;

internal struct PClassInfo2
{
	public unsafe fixed byte Cid[16];

	public int Cardinality;

	[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
	public string Category;

	[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 64)]
	public string Name;

	public uint ClassFlags;

	[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
	public string SubCategories;

	[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 64)]
	public string Vendor;

	[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 64)]
	public string Version;

	[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 64)]
	public string SdkVersion;
}
