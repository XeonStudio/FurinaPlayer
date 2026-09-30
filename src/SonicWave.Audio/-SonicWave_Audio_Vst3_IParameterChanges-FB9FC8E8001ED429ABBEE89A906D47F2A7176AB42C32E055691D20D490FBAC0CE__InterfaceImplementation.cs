using System;
using System.CodeDom.Compiler;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using SonicWave.Audio.Vst3;

[DynamicInterfaceCastableImplementation]
internal interface _003CSonicWave_Audio_Vst3_IParameterChanges_003EFB9FC8E8001ED429ABBEE89A906D47F2A7176AB42C32E055691D20D490FBAC0CE__InterfaceImplementation : IParameterChanges
{
	[GeneratedCode("Microsoft.Interop.ComInterfaceGenerator", "8.0.14.32403")]
	[SkipLocalsInit]
	unsafe int IParameterChanges.GetParameterCount()
	{
		var (ptr3, ptr4) = ((IUnmanagedVirtualMethodTableProvider)this).GetVirtualMethodTableInfoForKey(typeof(IParameterChanges));
		int result = ((delegate* unmanaged[MemberFunction]<void*, int>)ptr4[3])(ptr3);
		GC.KeepAlive(this);
		return result;
	}

	[GeneratedCode("Microsoft.Interop.ComInterfaceGenerator", "8.0.14.32403")]
	[SkipLocalsInit]
	unsafe nint IParameterChanges.GetParameterData(int index)
	{
		var (ptr3, ptr4) = ((IUnmanagedVirtualMethodTableProvider)this).GetVirtualMethodTableInfoForKey(typeof(IParameterChanges));
		nint result = ((delegate* unmanaged[MemberFunction]<void*, int, nint>)ptr4[4])(ptr3, index);
		GC.KeepAlive(this);
		return result;
	}

	[GeneratedCode("Microsoft.Interop.ComInterfaceGenerator", "8.0.14.32403")]
	[SkipLocalsInit]
	unsafe nint IParameterChanges.AddParameterData(ref uint id, out int index)
	{
		var (ptr3, ptr4) = ((IUnmanagedVirtualMethodTableProvider)this).GetVirtualMethodTableInfoForKey(typeof(IParameterChanges));
		Unsafe.SkipInit<int>(out index);
		nint result;
		fixed (int* ptr5 = &index)
		{
			fixed (uint* ptr6 = &id)
			{
				result = ((delegate* unmanaged[MemberFunction]<void*, uint*, int*, nint>)ptr4[5])(ptr3, ptr6, ptr5);
			}
		}
		GC.KeepAlive(this);
		return result;
	}

	[UnmanagedCallersOnly(CallConvs = new Type[] { typeof(CallConvMemberFunction) })]
	internal unsafe static int ABI_GetParameterCount(ComWrappers.ComInterfaceDispatch* __this_native)
	{
		int num = 0;
		try
		{
			return ComWrappers.ComInterfaceDispatch.GetInstance<IParameterChanges>(__this_native).GetParameterCount();
		}
		catch (Exception e)
		{
			return ExceptionAsHResultMarshaller<int>.ConvertToUnmanaged(e);
		}
	}

	[UnmanagedCallersOnly(CallConvs = new Type[] { typeof(CallConvMemberFunction) })]
	internal unsafe static nint ABI_GetParameterData(ComWrappers.ComInterfaceDispatch* __this_native, int index)
	{
		nint num = 0;
		try
		{
			return ComWrappers.ComInterfaceDispatch.GetInstance<IParameterChanges>(__this_native).GetParameterData(index);
		}
		catch (Exception e)
		{
			return ExceptionAsDefaultMarshaller<nint>.ConvertToUnmanaged(e);
		}
	}

	[UnmanagedCallersOnly(CallConvs = new Type[] { typeof(CallConvMemberFunction) })]
	internal unsafe static nint ABI_AddParameterData(ComWrappers.ComInterfaceDispatch* __this_native, uint* __id_native__param, int* __index_native__param)
	{
		ref uint reference = ref *__id_native__param;
		uint num = 0u;
		ref int reference2 = ref *__index_native__param;
		int index = 0;
		nint num2 = 0;
		try
		{
			num = reference;
			num2 = ComWrappers.ComInterfaceDispatch.GetInstance<IParameterChanges>(__this_native).AddParameterData(ref num, out index);
			reference2 = index;
			reference = num;
		}
		catch (Exception e)
		{
			num2 = ExceptionAsDefaultMarshaller<nint>.ConvertToUnmanaged(e);
		}
		return num2;
	}

	internal unsafe static void** CreateManagedVirtualFunctionTable()
	{
		void** ptr = (void**)RuntimeHelpers.AllocateTypeAssociatedMemory(typeof(IParameterChanges), sizeof(void*) * 6);
		ComWrappers.GetIUnknownImpl(out var fpQueryInterface, out var fpAddRef, out var fpRelease);
		*ptr = (void*)fpQueryInterface;
		ptr[1] = (void*)fpAddRef;
		ptr[2] = (void*)fpRelease;
		ptr[3] = (delegate* unmanaged[MemberFunction]<ComWrappers.ComInterfaceDispatch*, int>)(&ABI_GetParameterCount);
		ptr[4] = (delegate* unmanaged[MemberFunction]<ComWrappers.ComInterfaceDispatch*, int, nint>)(&ABI_GetParameterData);
		ptr[5] = (delegate* unmanaged[MemberFunction]<ComWrappers.ComInterfaceDispatch*, uint*, int*, nint>)(&ABI_AddParameterData);
		return ptr;
	}
}
