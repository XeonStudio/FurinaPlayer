using System;
using System.Runtime.InteropServices.Marshalling;

internal class _003CSonicWave_Audio_Vst3_IBStream_003EF4CDFA06C80BEE8195CFAE80BB1E1A21E5BB0712728CB91F895AA6955C048F1FE__InterfaceInformation : IIUnknownInterfaceType
{
	private unsafe static void** _vtable;

	public static Guid Iid { get; } = new Guid((ReadOnlySpan<byte>)new byte[16]
	{
		162, 110, 191, 195, 153, 48, 82, 71, 155, 107,
		249, 144, 30, 227, 62, 155
	});

	public unsafe static void** ManagedVirtualMethodTable
	{
		get
		{
			if (_vtable == null)
			{
				return _vtable = _003CSonicWave_Audio_Vst3_IBStream_003EF4CDFA06C80BEE8195CFAE80BB1E1A21E5BB0712728CB91F895AA6955C048F1FE__InterfaceImplementation.CreateManagedVirtualFunctionTable();
			}
			return _vtable;
		}
	}
}
