using System;
using System.CodeDom.Compiler;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using SonicWave.Audio.Vst3;

[DynamicInterfaceCastableImplementation]
internal interface _003CSonicWave_Audio_Vst3_IPluginFactory_003EFC8BB705D48E54566D9FD89ED8AA8B7A4635C8F7E15408CD81DADC23C8AD256B3__InterfaceImplementation : IPluginFactory
{
	[GeneratedCode("Microsoft.Interop.ComInterfaceGenerator", "8.0.14.32403")]
	[SkipLocalsInit]
	unsafe int IPluginFactory.GetFactoryInfo(nint info)
	{
		var (ptr3, ptr4) = ((IUnmanagedVirtualMethodTableProvider)this).GetVirtualMethodTableInfoForKey(typeof(IPluginFactory));
		int result = ((delegate* unmanaged[MemberFunction]<void*, nint, int>)ptr4[3])(ptr3, info);
		GC.KeepAlive(this);
		return result;
	}

	[GeneratedCode("Microsoft.Interop.ComInterfaceGenerator", "8.0.14.32403")]
	[SkipLocalsInit]
	unsafe int IPluginFactory.CountClasses()
	{
		var (ptr3, ptr4) = ((IUnmanagedVirtualMethodTableProvider)this).GetVirtualMethodTableInfoForKey(typeof(IPluginFactory));
		int result = ((delegate* unmanaged[MemberFunction]<void*, int>)ptr4[4])(ptr3);
		GC.KeepAlive(this);
		return result;
	}

	[GeneratedCode("Microsoft.Interop.ComInterfaceGenerator", "8.0.14.32403")]
	[SkipLocalsInit]
	unsafe int IPluginFactory.GetClassInfo(int index, nint info)
	{
		var (ptr3, ptr4) = ((IUnmanagedVirtualMethodTableProvider)this).GetVirtualMethodTableInfoForKey(typeof(IPluginFactory));
		int result = ((delegate* unmanaged[MemberFunction]<void*, int, nint, int>)ptr4[5])(ptr3, index, info);
		GC.KeepAlive(this);
		return result;
	}

	[GeneratedCode("Microsoft.Interop.ComInterfaceGenerator", "8.0.14.32403")]
	[SkipLocalsInit]
	unsafe int IPluginFactory.CreateInstance(nint cid, nint iid, out nint obj)
	{
		var (ptr3, ptr4) = ((IUnmanagedVirtualMethodTableProvider)this).GetVirtualMethodTableInfoForKey(typeof(IPluginFactory));
		Unsafe.SkipInit<nint>(out obj);
		int result;
		fixed (nint* ptr5 = &obj)
		{
			result = ((delegate* unmanaged[MemberFunction]<void*, nint, nint, nint*, int>)ptr4[6])(ptr3, cid, iid, ptr5);
		}
		GC.KeepAlive(this);
		return result;
	}

	[UnmanagedCallersOnly(CallConvs = new Type[] { typeof(CallConvMemberFunction) })]
	internal unsafe static int ABI_GetFactoryInfo(ComWrappers.ComInterfaceDispatch* __this_native, nint info)
	{
		int num = 0;
		try
		{
			return ComWrappers.ComInterfaceDispatch.GetInstance<IPluginFactory>(__this_native).GetFactoryInfo(info);
		}
		catch (Exception e)
		{
			return ExceptionAsHResultMarshaller<int>.ConvertToUnmanaged(e);
		}
	}

	[UnmanagedCallersOnly(CallConvs = new Type[] { typeof(CallConvMemberFunction) })]
	internal unsafe static int ABI_CountClasses(ComWrappers.ComInterfaceDispatch* __this_native)
	{
		int num = 0;
		try
		{
			return ComWrappers.ComInterfaceDispatch.GetInstance<IPluginFactory>(__this_native).CountClasses();
		}
		catch (Exception e)
		{
			return ExceptionAsHResultMarshaller<int>.ConvertToUnmanaged(e);
		}
	}

	[UnmanagedCallersOnly(CallConvs = new Type[] { typeof(CallConvMemberFunction) })]
	internal unsafe static int ABI_GetClassInfo(ComWrappers.ComInterfaceDispatch* __this_native, int index, nint info)
	{
		int num = 0;
		try
		{
			return ComWrappers.ComInterfaceDispatch.GetInstance<IPluginFactory>(__this_native).GetClassInfo(index, info);
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
			num = ComWrappers.ComInterfaceDispatch.GetInstance<IPluginFactory>(__this_native).CreateInstance(cid, iid, out obj);
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
		void** ptr = (void**)RuntimeHelpers.AllocateTypeAssociatedMemory(typeof(IPluginFactory), sizeof(void*) * 7);
		ComWrappers.GetIUnknownImpl(out var fpQueryInterface, out var fpAddRef, out var fpRelease);
		*ptr = (void*)fpQueryInterface;
		ptr[1] = (void*)fpAddRef;
		ptr[2] = (void*)fpRelease;
		ptr[3] = (delegate* unmanaged[MemberFunction]<ComWrappers.ComInterfaceDispatch*, nint, int>)(&ABI_GetFactoryInfo);
		ptr[4] = (delegate* unmanaged[MemberFunction]<ComWrappers.ComInterfaceDispatch*, int>)(&ABI_CountClasses);
		ptr[5] = (delegate* unmanaged[MemberFunction]<ComWrappers.ComInterfaceDispatch*, int, nint, int>)(&ABI_GetClassInfo);
		ptr[6] = (delegate* unmanaged[MemberFunction]<ComWrappers.ComInterfaceDispatch*, nint, nint, nint*, int>)(&ABI_CreateInstance);
		return ptr;
	}
}
