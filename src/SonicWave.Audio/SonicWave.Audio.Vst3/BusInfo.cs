using System.Runtime.InteropServices;

namespace SonicWave.Audio.Vst3;

[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
internal struct BusInfo
{
	public int MediaType;

	public int Direction;

	public int ChannelCount;

	[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
	public string Name;

	public int BusType;

	public uint Flags;
}
