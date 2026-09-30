using System;
using System.Runtime.InteropServices.Marshalling;

internal class _003CSonicWave_Audio_Vst3_IParamValueQueue_003EF832198D588C9107E992551106C20CF4C530DED109774B95846BB7CE73E036CED__InterfaceInformation : IIUnknownInterfaceType
{
	private unsafe static void** _vtable;

	public static Guid Iid { get; } = new Guid((ReadOnlySpan<byte>)new byte[16]
	{
		24, 58, 38, 1, 7, 237, 111, 79, 152, 201,
		211, 86, 70, 134, 249, 186
	});

	public unsafe static void** ManagedVirtualMethodTable
	{
		get
		{
			if (_vtable == null)
			{
				return _vtable = _003CSonicWave_Audio_Vst3_IParamValueQueue_003EF832198D588C9107E992551106C20CF4C530DED109774B95846BB7CE73E036CED__InterfaceImplementation.CreateManagedVirtualFunctionTable();
			}
			return _vtable;
		}
	}
}
