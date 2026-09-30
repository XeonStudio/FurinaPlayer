using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace SonicWave.Audio.Vst3;

[GeneratedComInterface]
[Guid("42043F99-B7DA-453C-A569-E79D9AAEC33D")]
internal partial interface IAudioProcessor
{
	[PreserveSig]
	int SetBusArrangements(nint inputs, int numIns, nint outputs, int numOuts);

	[PreserveSig]
	int GetBusArrangement(int direction, int index, nint arrangement);

	[PreserveSig]
	int CanProcessSampleSize(int symbolicSampleSize);

	[PreserveSig]
	uint GetLatencySamples();

	[PreserveSig]
	int SetupProcessing(ref ProcessSetup setup);

	[PreserveSig]
	int SetProcessing(byte state);

	[PreserveSig]
	int Process(ref ProcessData data);

	[PreserveSig]
	uint GetTailSamples();
}


