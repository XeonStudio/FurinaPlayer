using System.Runtime.InteropServices;

namespace SonicWave.Audio.Vst3;

[ComVisible(true)]
[Guid("936F033B-C6C0-47DB-BB08-82F813C1E613")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
public partial interface IMessageCom
{
	[PreserveSig]
	nint GetMessageID();

	[PreserveSig]
	void SetMessageID(nint id);

	[PreserveSig]
	nint GetAttributes();
}


