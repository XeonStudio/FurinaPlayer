using System.Runtime.InteropServices;

namespace SonicWave.Audio.Vst3;

[ComVisible(true)]
[Guid("93A0BEA3-0BD0-45DB-8E89-0B0CC1E46AC6")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
public partial interface IComponentHandlerCom
{
	[PreserveSig]
	int BeginEdit(uint id);

	[PreserveSig]
	int PerformEdit(uint id, double valueNormalized);

	[PreserveSig]
	int EndEdit(uint id);

	[PreserveSig]
	int RestartComponent(int flags);
}


