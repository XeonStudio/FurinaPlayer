using System.Runtime.InteropServices;

namespace SonicWave.Audio.Vst3;

[ComVisible(true)]
[Guid("367FAF01-AFA9-4693-8D4D-A2A0ED0882A3")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
public partial interface IPlugFrameCom
{
	[PreserveSig]
	int ResizeView(nint view, nint newSize);
}


