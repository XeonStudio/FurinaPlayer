using System;
using System.Runtime.InteropServices.Marshalling;

internal class _003CSonicWave_Audio_Vst3_IPluginFactory_003EFC8BB705D48E54566D9FD89ED8AA8B7A4635C8F7E15408CD81DADC23C8AD256B3__InterfaceInformation : IIUnknownInterfaceType
{
	private unsafe static void** _vtable;

	public static Guid Iid { get; } = new Guid((ReadOnlySpan<byte>)new byte[16]
	{
		28, 129, 77, 122, 17, 82, 31, 74, 174, 217,
		210, 238, 11, 67, 191, 159
	});

	public unsafe static void** ManagedVirtualMethodTable
	{
		get
		{
			if (_vtable == null)
			{
				return _vtable = _003CSonicWave_Audio_Vst3_IPluginFactory_003EFC8BB705D48E54566D9FD89ED8AA8B7A4635C8F7E15408CD81DADC23C8AD256B3__InterfaceImplementation.CreateManagedVirtualFunctionTable();
			}
			return _vtable;
		}
	}
}
