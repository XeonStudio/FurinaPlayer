using System.Runtime.InteropServices;

namespace SonicWave.Audio.Vst3;

[ComVisible(true)]
[Guid("58E595CC-DB2D-4969-8B6A-AF8C36A664E5")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
public partial interface IHostApplicationCom
{
	[PreserveSig]
	int GetName(nint name);

	[PreserveSig]
	int CreateInstance(nint cid, nint iid, out nint obj);
}


