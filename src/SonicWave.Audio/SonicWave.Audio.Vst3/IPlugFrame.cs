using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace SonicWave.Audio.Vst3;

[GeneratedComInterface]
[Guid("367FAF01-AFA9-4693-8D4D-A2A0ED0882A3")]
internal partial interface IPlugFrame
{
	[PreserveSig]
	int ResizeView(nint view, nint newSize);
}


