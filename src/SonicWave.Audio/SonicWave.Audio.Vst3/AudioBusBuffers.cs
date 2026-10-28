using System;
using System.Runtime.InteropServices;

namespace SonicWave.Audio.Vst3;

[StructLayout(LayoutKind.Explicit, Size = 24)]
public struct AudioBusBuffers
{
	[FieldOffset(0)] public int NumChannels;
	[FieldOffset(8)] public ulong SilenceFlags;
	[FieldOffset(16)] public nint ChannelBuffers;
}
