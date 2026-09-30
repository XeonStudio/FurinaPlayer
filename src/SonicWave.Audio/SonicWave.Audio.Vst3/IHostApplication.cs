using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace SonicWave.Audio.Vst3;

[GeneratedComInterface]
[Guid("58E595CC-DB2D-4969-8B6A-AF8C36A664E5")]
internal partial interface IHostApplication
{
	[PreserveSig]
	int GetName(nint name);

	[PreserveSig]
	int CreateInstance(nint cid, nint iid, out nint obj);
}


