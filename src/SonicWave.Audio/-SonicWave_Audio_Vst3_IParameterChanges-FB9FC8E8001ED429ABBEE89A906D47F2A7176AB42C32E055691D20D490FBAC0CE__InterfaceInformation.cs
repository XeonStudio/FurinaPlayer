using System;
using System.Runtime.InteropServices.Marshalling;

internal class _003CSonicWave_Audio_Vst3_IParameterChanges_003EFB9FC8E8001ED429ABBEE89A906D47F2A7176AB42C32E055691D20D490FBAC0CE__InterfaceInformation : IIUnknownInterfaceType
{
	private unsafe static void** _vtable;

	public static Guid Iid { get; } = new Guid((ReadOnlySpan<byte>)new byte[16]
	{
		99, 150, 119, 164, 182, 11, 86, 74, 180, 67,
		132, 168, 70, 111, 235, 157
	});

	public unsafe static void** ManagedVirtualMethodTable
	{
		get
		{
			if (_vtable == null)
			{
				return _vtable = _003CSonicWave_Audio_Vst3_IParameterChanges_003EFB9FC8E8001ED429ABBEE89A906D47F2A7176AB42C32E055691D20D490FBAC0CE__InterfaceImplementation.CreateManagedVirtualFunctionTable();
			}
			return _vtable;
		}
	}
}
