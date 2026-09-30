using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace SonicWave.Audio.Vst3;

[GeneratedComInterface]
[Guid("A4779663-0BB6-4A56-B443-84A8466FEB9D")]
internal partial interface IParameterChanges
{
	[PreserveSig]
	int GetParameterCount();

	[PreserveSig]
	nint GetParameterData(int index);

	[PreserveSig]
	nint AddParameterData(ref uint id, out int index);
}


