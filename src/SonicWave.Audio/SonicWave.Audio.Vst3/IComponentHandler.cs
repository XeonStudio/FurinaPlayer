using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace SonicWave.Audio.Vst3;

[GeneratedComInterface]
[Guid("93A0BEA3-0BD0-45DB-8E89-0B0CC1E46AC6")]
internal partial interface IComponentHandler
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


