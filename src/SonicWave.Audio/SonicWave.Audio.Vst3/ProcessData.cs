using System;
using System.Runtime.InteropServices;

namespace SonicWave.Audio.Vst3;

[StructLayout(LayoutKind.Explicit, Size = 80)]
public struct ProcessData
{
	[FieldOffset(0)] public int ProcessMode;
	[FieldOffset(4)] public int SymbolicSampleSize;
	[FieldOffset(8)] public int NumSamples;
	[FieldOffset(12)] public int NumInputs;
	[FieldOffset(16)] public int NumOutputs;
	[FieldOffset(24)] public nint Inputs;
	[FieldOffset(32)] public nint Outputs;
	[FieldOffset(40)] public nint InputParameterChanges;
	[FieldOffset(48)] public nint OutputParameterChanges;
	[FieldOffset(56)] public nint InputEvents;
	[FieldOffset(64)] public nint OutputEvents;
	[FieldOffset(72)] public nint ProcessContext;
}
