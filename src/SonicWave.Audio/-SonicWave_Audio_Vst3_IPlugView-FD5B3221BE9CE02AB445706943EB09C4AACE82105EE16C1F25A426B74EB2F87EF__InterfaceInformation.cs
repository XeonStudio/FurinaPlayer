using System;
using System.Runtime.InteropServices.Marshalling;

internal class _003CSonicWave_Audio_Vst3_IPlugView_003EFD5B3221BE9CE02AB445706943EB09C4AACE82105EE16C1F25A426B74EB2F87EF__InterfaceInformation : IIUnknownInterfaceType
{
	private unsafe static void** _vtable;

	public static Guid Iid { get; } = new Guid((ReadOnlySpan<byte>)new byte[16]
	{
		7, 37, 195, 91, 96, 208, 234, 73, 166, 21,
		27, 82, 43, 117, 91, 41
	});

	public unsafe static void** ManagedVirtualMethodTable
	{
		get
		{
			if (_vtable == null)
			{
				return _vtable = _003CSonicWave_Audio_Vst3_IPlugView_003EFD5B3221BE9CE02AB445706943EB09C4AACE82105EE16C1F25A426B74EB2F87EF__InterfaceImplementation.CreateManagedVirtualFunctionTable();
			}
			return _vtable;
		}
	}
}
