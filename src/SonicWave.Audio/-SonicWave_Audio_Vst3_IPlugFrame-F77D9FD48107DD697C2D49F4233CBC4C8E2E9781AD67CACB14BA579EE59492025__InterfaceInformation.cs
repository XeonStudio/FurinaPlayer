using System;
using System.Runtime.InteropServices.Marshalling;

internal class _003CSonicWave_Audio_Vst3_IPlugFrame_003EF77D9FD48107DD697C2D49F4233CBC4C8E2E9781AD67CACB14BA579EE59492025__InterfaceInformation : IIUnknownInterfaceType
{
	private unsafe static void** _vtable;

	public static Guid Iid { get; } = new Guid((ReadOnlySpan<byte>)new byte[16]
	{
		1, 175, 127, 54, 169, 175, 147, 70, 141, 77,
		162, 160, 237, 8, 130, 163
	});

	public unsafe static void** ManagedVirtualMethodTable
	{
		get
		{
			if (_vtable == null)
			{
				return _vtable = _003CSonicWave_Audio_Vst3_IPlugFrame_003EF77D9FD48107DD697C2D49F4233CBC4C8E2E9781AD67CACB14BA579EE59492025__InterfaceImplementation.CreateManagedVirtualFunctionTable();
			}
			return _vtable;
		}
	}
}
