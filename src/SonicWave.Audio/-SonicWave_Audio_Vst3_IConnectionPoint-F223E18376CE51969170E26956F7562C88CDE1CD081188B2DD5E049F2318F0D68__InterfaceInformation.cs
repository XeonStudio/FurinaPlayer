using System;
using System.Runtime.InteropServices.Marshalling;

internal class _003CSonicWave_Audio_Vst3_IConnectionPoint_003EF223E18376CE51969170E26956F7562C88CDE1CD081188B2DD5E049F2318F0D68__InterfaceInformation : IIUnknownInterfaceType
{
	private unsafe static void** _vtable;

	public static Guid Iid { get; } = new Guid((ReadOnlySpan<byte>)new byte[16]
	{
		111, 21, 164, 112, 110, 110, 38, 64, 152, 145,
		72, 191, 170, 96, 216, 209
	});

	public unsafe static void** ManagedVirtualMethodTable
	{
		get
		{
			if (_vtable == null)
			{
				return _vtable = _003CSonicWave_Audio_Vst3_IConnectionPoint_003EF223E18376CE51969170E26956F7562C88CDE1CD081188B2DD5E049F2318F0D68__InterfaceImplementation.CreateManagedVirtualFunctionTable();
			}
			return _vtable;
		}
	}
}
