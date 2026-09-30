using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace SonicWave.Audio.Vst3;

[GeneratedComInterface]
[Guid("0007B650-F24B-4C0B-A464-EDB9F00B2ABB")]
internal partial interface IPluginFactory2
{
	[PreserveSig]
	int GetFactoryInfo(nint info);

	[PreserveSig]
	int CountClasses();

	[PreserveSig]
	int GetClassInfo(int index, nint info);

	[PreserveSig]
	int CreateInstance(nint cid, nint iid, out nint obj);

	[PreserveSig]
	int GetClassInfo2(int index, nint info);
}


