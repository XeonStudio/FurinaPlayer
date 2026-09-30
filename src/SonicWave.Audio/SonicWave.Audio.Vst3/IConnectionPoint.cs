using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace SonicWave.Audio.Vst3;

[GeneratedComInterface]
[Guid("70A4156F-6E6E-4026-9891-48BFAA60D8D1")]
internal partial interface IConnectionPoint
{
	[PreserveSig]
	int Connect(nint other);

	[PreserveSig]
	int Disconnect(nint other);

	[PreserveSig]
	int Notify(nint message);
}


