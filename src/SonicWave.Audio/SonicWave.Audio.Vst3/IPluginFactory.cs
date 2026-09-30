using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace SonicWave.Audio.Vst3;

[GeneratedComInterface]
[Guid("7A4D811C-5211-4A1F-AED9-D2EE0B43BF9F")]
internal partial interface IPluginFactory
{
	[PreserveSig]
	int GetFactoryInfo(nint info);

	[PreserveSig]
	int CountClasses();

	[PreserveSig]
	int GetClassInfo(int index, nint info);

	[PreserveSig]
	int CreateInstance(nint cid, nint iid, out nint obj);
}


