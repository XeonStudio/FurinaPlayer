using System;
using System.Runtime.InteropServices.Marshalling;

internal class _003CSonicWave_Audio_Vst3_IEditController_003EF3F4D566CFB5F6C09A4975688751AA569B08FED240E0CE1222CBE48FC6955BC77__InterfaceInformation : IIUnknownInterfaceType
{
	private unsafe static void** _vtable;

	public static Guid Iid { get; } = new Guid((ReadOnlySpan<byte>)new byte[16]
	{
		227, 187, 215, 220, 66, 119, 141, 68, 168, 116,
		170, 204, 151, 156, 117, 158
	});

	public unsafe static void** ManagedVirtualMethodTable
	{
		get
		{
			if (_vtable == null)
			{
				return _vtable = _003CSonicWave_Audio_Vst3_IEditController_003EF3F4D566CFB5F6C09A4975688751AA569B08FED240E0CE1222CBE48FC6955BC77__InterfaceImplementation.CreateManagedVirtualFunctionTable();
			}
			return _vtable;
		}
	}
}
