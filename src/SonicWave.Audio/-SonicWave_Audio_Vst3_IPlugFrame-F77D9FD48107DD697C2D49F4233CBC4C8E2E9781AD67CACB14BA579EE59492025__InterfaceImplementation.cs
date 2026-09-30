using System;
using System.CodeDom.Compiler;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using SonicWave.Audio.Vst3;

[DynamicInterfaceCastableImplementation]
internal interface _003CSonicWave_Audio_Vst3_IPlugFrame_003EF77D9FD48107DD697C2D49F4233CBC4C8E2E9781AD67CACB14BA579EE59492025__InterfaceImplementation : IPlugFrame
{
	[GeneratedCode("Microsoft.Interop.ComInterfaceGenerator", "8.0.14.32403")]
	[SkipLocalsInit]
	unsafe int IPlugFrame.ResizeView(nint view, nint newSize)
	{
		var (ptr3, ptr4) = ((IUnmanagedVirtualMethodTableProvider)this).GetVirtualMethodTableInfoForKey(typeof(IPlugFrame));
		int result = ((delegate* unmanaged[MemberFunction]<void*, nint, nint, int>)ptr4[3])(ptr3, view, newSize);
		GC.KeepAlive(this);
		return result;
	}

	[UnmanagedCallersOnly(CallConvs = new Type[] { typeof(CallConvMemberFunction) })]
	internal unsafe static int ABI_ResizeView(ComWrappers.ComInterfaceDispatch* __this_native, nint view, nint newSize)
	{
		int num = 0;
		try
		{
			return ComWrappers.ComInterfaceDispatch.GetInstance<IPlugFrame>(__this_native).ResizeView(view, newSize);
		}
		catch (Exception e)
		{
			return ExceptionAsHResultMarshaller<int>.ConvertToUnmanaged(e);
		}
	}

	internal unsafe static void** CreateManagedVirtualFunctionTable()
	{
		void** ptr = (void**)RuntimeHelpers.AllocateTypeAssociatedMemory(typeof(IPlugFrame), sizeof(void*) * 4);
		ComWrappers.GetIUnknownImpl(out var fpQueryInterface, out var fpAddRef, out var fpRelease);
		*ptr = (void*)fpQueryInterface;
		ptr[1] = (void*)fpAddRef;
		ptr[2] = (void*)fpRelease;
		ptr[3] = (delegate* unmanaged[MemberFunction]<ComWrappers.ComInterfaceDispatch*, nint, nint, int>)(&ABI_ResizeView);
		return ptr;
	}
}
