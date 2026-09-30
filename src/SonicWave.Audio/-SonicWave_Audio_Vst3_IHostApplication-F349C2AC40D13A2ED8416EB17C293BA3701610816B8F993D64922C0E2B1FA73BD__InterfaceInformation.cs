using System;
using System.Runtime.InteropServices.Marshalling;

internal class _003CSonicWave_Audio_Vst3_IHostApplication_003EF349C2AC40D13A2ED8416EB17C293BA3701610816B8F993D64922C0E2B1FA73BD__InterfaceInformation : IIUnknownInterfaceType
{
	private unsafe static void** _vtable;

	public static Guid Iid { get; } = new Guid((ReadOnlySpan<byte>)new byte[16]
	{
		204, 149, 229, 88, 45, 219, 105, 73, 139, 106,
		175, 140, 54, 166, 100, 229
	});

	public unsafe static void** ManagedVirtualMethodTable
	{
		get
		{
			if (_vtable == null)
			{
				return _vtable = _003CSonicWave_Audio_Vst3_IHostApplication_003EF349C2AC40D13A2ED8416EB17C293BA3701610816B8F993D64922C0E2B1FA73BD__InterfaceImplementation.CreateManagedVirtualFunctionTable();
			}
			return _vtable;
		}
	}
}
