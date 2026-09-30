using System;
using System.Runtime.InteropServices.Marshalling;

internal class _003CSonicWave_Audio_Vst3_IAudioProcessor_003EF2B134E5FDF3DB13EDF17356DEC142B3C1C1BF2010B588AF8D92850228901A423__InterfaceInformation : IIUnknownInterfaceType
{
	private unsafe static void** _vtable;

	public static Guid Iid { get; } = new Guid((ReadOnlySpan<byte>)new byte[16]
	{
		153, 63, 4, 66, 218, 183, 60, 69, 165, 105,
		231, 157, 154, 174, 195, 61
	});

	public unsafe static void** ManagedVirtualMethodTable
	{
		get
		{
			if (_vtable == null)
			{
				return _vtable = _003CSonicWave_Audio_Vst3_IAudioProcessor_003EF2B134E5FDF3DB13EDF17356DEC142B3C1C1BF2010B588AF8D92850228901A423__InterfaceImplementation.CreateManagedVirtualFunctionTable();
			}
			return _vtable;
		}
	}
}
