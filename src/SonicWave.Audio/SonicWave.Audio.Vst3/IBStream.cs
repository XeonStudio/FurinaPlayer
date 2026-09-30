using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace SonicWave.Audio.Vst3;

[GeneratedComInterface]
[Guid("C3BF6EA2-3099-4752-9B6B-F9901EE33E9B")]
internal partial interface IBStream
{
	[PreserveSig]
	int Read(nint buffer, int numBytes, nint numBytesRead);

	[PreserveSig]
	int Write(nint buffer, int numBytes, nint numBytesWritten);

	[PreserveSig]
	int Seek(long pos, int mode, nint result);

	[PreserveSig]
	int Tell(nint pos);
}


