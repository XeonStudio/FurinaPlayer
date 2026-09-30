using System;
using System.Runtime.InteropServices.Marshalling;

internal class _003CSonicWave_Audio_Vst3_IPluginFactory2_003EF46AA65FD3CBA5CEEF6ADF9E8E0D937ED25DB02A5DC3F86CCE69572C978B85D93__InterfaceInformation : IIUnknownInterfaceType
{
	private unsafe static void** _vtable;

	public static Guid Iid { get; } = new Guid((ReadOnlySpan<byte>)new byte[16]
	{
		80, 182, 7, 0, 75, 242, 11, 76, 164, 100,
		237, 185, 240, 11, 42, 187
	});

	public unsafe static void** ManagedVirtualMethodTable
	{
		get
		{
			if (_vtable == null)
			{
				return _vtable = _003CSonicWave_Audio_Vst3_IPluginFactory2_003EF46AA65FD3CBA5CEEF6ADF9E8E0D937ED25DB02A5DC3F86CCE69572C978B85D93__InterfaceImplementation.CreateManagedVirtualFunctionTable();
			}
			return _vtable;
		}
	}
}
