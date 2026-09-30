using System.Runtime.InteropServices;

namespace SonicWave.Audio.Vst3;

[ComVisible(true)]
[Guid("A4779663-0BB6-4A56-B443-84A8466FEB9D")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
public partial interface IParameterChangesCom
{
	[PreserveSig]
	int GetParameterCount();

	[PreserveSig]
	nint GetParameterData(int index);

	[PreserveSig]
	nint AddParameterData(ref uint id, out int index);
}


