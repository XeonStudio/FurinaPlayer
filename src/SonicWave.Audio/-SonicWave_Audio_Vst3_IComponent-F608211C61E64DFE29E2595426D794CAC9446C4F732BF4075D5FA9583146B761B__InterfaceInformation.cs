using System;
using System.Runtime.InteropServices.Marshalling;

internal class _003CSonicWave_Audio_Vst3_IComponent_003EF608211C61E64DFE29E2595426D794CAC9446C4F732BF4075D5FA9583146B761B__InterfaceInformation : IIUnknownInterfaceType
{
	private unsafe static void** _vtable;

	public static Guid Iid { get; } = new Guid((ReadOnlySpan<byte>)new byte[16]
	{
		49, 255, 49, 232, 213, 242, 1, 67, 146, 142,
		187, 238, 37, 105, 120, 2
	});

	public unsafe static void** ManagedVirtualMethodTable
	{
		get
		{
			if (_vtable == null)
			{
				return _vtable = _003CSonicWave_Audio_Vst3_IComponent_003EF608211C61E64DFE29E2595426D794CAC9446C4F732BF4075D5FA9583146B761B__InterfaceImplementation.CreateManagedVirtualFunctionTable();
			}
			return _vtable;
		}
	}
}
