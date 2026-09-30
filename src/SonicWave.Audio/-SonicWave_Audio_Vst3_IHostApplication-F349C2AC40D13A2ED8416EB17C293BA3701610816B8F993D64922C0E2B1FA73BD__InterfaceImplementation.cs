using System;
using System.CodeDom.Compiler;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using SonicWave.Audio.Vst3;

[DynamicInterfaceCastableImplementation]
internal interface _003CSonicWave_Audio_Vst3_IHostApplication_003EF349C2AC40D13A2ED8416EB17C293BA3701610816B8F993D64922C0E2B1FA73BD__InterfaceImplementation : IHostApplication
{
	[GeneratedCode("Microsoft.Interop.ComInterfaceGenerator", "8.0.14.32403")]
	[SkipLocalsInit]
	unsafe int IHostApplication.GetName(nint name)
	{
		var (ptr3, ptr4) = ((IUnmanagedVirtualMethodTableProvider)this).GetVirtualMethodTableInfoForKey(typeof(IHostApplication));
		int result = ((delegate* unmanaged[MemberFunction]<void*, nint, int>)ptr4[3])(ptr3, name);
		GC.KeepAlive(this);
		return result;
	}

	[GeneratedCode("Microsoft.Interop.ComInterfaceGenerator", "8.0.14.32403")]
	[SkipLocalsInit]
	unsafe int IHostApplication.CreateInstance(nint cid, nint iid, out nint obj)
	{
		var (ptr3, ptr4) = ((IUnmanagedVirtualMethodTableProvider)this).GetVirtualMethodTableInfoForKey(typeof(IHostApplication));
		Unsafe.SkipInit<nint>(out obj);
		int result;
		fixed (nint* ptr5 = &obj)
		{
			result = ((delegate* unmanaged[MemberFunction]<void*, nint, nint, nint*, int>)ptr4[4])(ptr3, cid, iid, ptr5);
		}
		GC.KeepAlive(this);
		return result;
	}

	[UnmanagedCallersOnly(CallConvs = new Type[] { typeof(CallConvMemberFunction) })]
	internal unsafe static int ABI_GetName(ComWrappers.ComInterfaceDispatch* __this_native, nint name)
	{
		int num = 0;
		try
		{
			return ComWrappers.ComInterfaceDispatch.GetInstance<IHostApplication>(__this_native).GetName(name);
		}
		catch (Exception e)
		{
			return ExceptionAsHResultMarshaller<int>.ConvertToUnmanaged(e);
		}
	}

	[UnmanagedCallersOnly(CallConvs = new Type[] { typeof(CallConvMemberFunction) })]
	internal unsafe static int ABI_CreateInstance(ComWrappers.ComInterfaceDispatch* __this_native, nint cid, nint iid, nint* __obj_native__param)
	{
		ref nint reference = ref *__obj_native__param;
		nint obj = 0;
		int num = 0;
		try
		{
			num = ComWrappers.ComInterfaceDispatch.GetInstance<IHostApplication>(__this_native).CreateInstance(cid, iid, out obj);
			reference = obj;
		}
		catch (Exception e)
		{
			num = ExceptionAsHResultMarshaller<int>.ConvertToUnmanaged(e);
		}
		return num;
	}

	internal unsafe static void** CreateManagedVirtualFunctionTable()
	{
		void** ptr = (void**)RuntimeHelpers.AllocateTypeAssociatedMemory(typeof(IHostApplication), sizeof(void*) * 5);
		ComWrappers.GetIUnknownImpl(out var fpQueryInterface, out var fpAddRef, out var fpRelease);
		*ptr = (void*)fpQueryInterface;
		ptr[1] = (void*)fpAddRef;
		ptr[2] = (void*)fpRelease;
		ptr[3] = (delegate* unmanaged[MemberFunction]<ComWrappers.ComInterfaceDispatch*, nint, int>)(&ABI_GetName);
		ptr[4] = (delegate* unmanaged[MemberFunction]<ComWrappers.ComInterfaceDispatch*, nint, nint, nint*, int>)(&ABI_CreateInstance);
		return ptr;
	}
}
