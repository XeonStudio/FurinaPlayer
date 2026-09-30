using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace SonicWave.Audio.Vst3;

[GeneratedComInterface]
[Guid("936F033B-C6C0-47DB-BB08-82F813C1E613")]
internal partial interface IMessage
{
	[PreserveSig]
	nint GetMessageID();

	[PreserveSig]
	void SetMessageID(nint id);

	[PreserveSig]
	nint GetAttributes();
}


