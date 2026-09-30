using System;
using System.Runtime.InteropServices.Marshalling;

internal class _003CSonicWave_Audio_Vst3_IMessage_003EF28CBC06B3883EAC974697B9C3CADD0F6782C4179E4B48042E970966BA9AFD570__InterfaceInformation : IIUnknownInterfaceType
{
	private unsafe static void** _vtable;

	public static Guid Iid { get; } = new Guid((ReadOnlySpan<byte>)new byte[16]
	{
		59, 3, 111, 147, 192, 198, 219, 71, 187, 8,
		130, 248, 19, 193, 230, 19
	});

	public unsafe static void** ManagedVirtualMethodTable
	{
		get
		{
			if (_vtable == null)
			{
				return _vtable = _003CSonicWave_Audio_Vst3_IMessage_003EF28CBC06B3883EAC974697B9C3CADD0F6782C4179E4B48042E970966BA9AFD570__InterfaceImplementation.CreateManagedVirtualFunctionTable();
			}
			return _vtable;
		}
	}
}
