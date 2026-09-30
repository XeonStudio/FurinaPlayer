using System;
using System.Runtime.InteropServices.Marshalling;

internal class _003CSonicWave_Audio_Vst3_IComponentHandler_003EF834C785F1F2D860C996B67BDE031DE2C6F9EE1FEFD2F791CC615010AF3EF7C70__InterfaceInformation : IIUnknownInterfaceType
{
	private unsafe static void** _vtable;

	public static Guid Iid { get; } = new Guid((ReadOnlySpan<byte>)new byte[16]
	{
		163, 190, 160, 147, 208, 11, 219, 69, 142, 137,
		11, 12, 193, 228, 106, 198
	});

	public unsafe static void** ManagedVirtualMethodTable
	{
		get
		{
			if (_vtable == null)
			{
				return _vtable = _003CSonicWave_Audio_Vst3_IComponentHandler_003EF834C785F1F2D860C996B67BDE031DE2C6F9EE1FEFD2F791CC615010AF3EF7C70__InterfaceImplementation.CreateManagedVirtualFunctionTable();
			}
			return _vtable;
		}
	}
}
