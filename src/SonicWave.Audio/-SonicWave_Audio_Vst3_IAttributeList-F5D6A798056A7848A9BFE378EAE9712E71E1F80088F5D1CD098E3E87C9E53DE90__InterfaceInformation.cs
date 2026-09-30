using System;
using System.Runtime.InteropServices.Marshalling;

internal class _003CSonicWave_Audio_Vst3_IAttributeList_003EF5D6A798056A7848A9BFE378EAE9712E71E1F80088F5D1CD098E3E87C9E53DE90__InterfaceInformation : IIUnknownInterfaceType
{
	private unsafe static void** _vtable;

	public static Guid Iid { get; } = new Guid((ReadOnlySpan<byte>)new byte[16]
	{
		235, 10, 95, 30, 127, 204, 51, 69, 162, 84,
		64, 17, 56, 173, 94, 228
	});

	public unsafe static void** ManagedVirtualMethodTable
	{
		get
		{
			if (_vtable == null)
			{
				return _vtable = _003CSonicWave_Audio_Vst3_IAttributeList_003EF5D6A798056A7848A9BFE378EAE9712E71E1F80088F5D1CD098E3E87C9E53DE90__InterfaceImplementation.CreateManagedVirtualFunctionTable();
			}
			return _vtable;
		}
	}
}
