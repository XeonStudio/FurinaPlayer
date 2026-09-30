using System;
using System.CodeDom.Compiler;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using SonicWave.Audio.Vst3;

[DynamicInterfaceCastableImplementation]
internal interface _003CSonicWave_Audio_Vst3_IAttributeList_003EF5D6A798056A7848A9BFE378EAE9712E71E1F80088F5D1CD098E3E87C9E53DE90__InterfaceImplementation : IAttributeList
{
	[GeneratedCode("Microsoft.Interop.ComInterfaceGenerator", "8.0.14.32403")]
	[SkipLocalsInit]
	unsafe int IAttributeList.SetInt(nint id, long value)
	{
		var (ptr3, ptr4) = ((IUnmanagedVirtualMethodTableProvider)this).GetVirtualMethodTableInfoForKey(typeof(IAttributeList));
		int result = ((delegate* unmanaged[MemberFunction]<void*, nint, long, int>)ptr4[3])(ptr3, id, value);
		GC.KeepAlive(this);
		return result;
	}

	[GeneratedCode("Microsoft.Interop.ComInterfaceGenerator", "8.0.14.32403")]
	[SkipLocalsInit]
	unsafe int IAttributeList.GetInt(nint id, out long value)
	{
		var (ptr3, ptr4) = ((IUnmanagedVirtualMethodTableProvider)this).GetVirtualMethodTableInfoForKey(typeof(IAttributeList));
		Unsafe.SkipInit<long>(out value);
		int result;
		fixed (long* ptr5 = &value)
		{
			result = ((delegate* unmanaged[MemberFunction]<void*, nint, long*, int>)ptr4[4])(ptr3, id, ptr5);
		}
		GC.KeepAlive(this);
		return result;
	}

	[GeneratedCode("Microsoft.Interop.ComInterfaceGenerator", "8.0.14.32403")]
	[SkipLocalsInit]
	unsafe int IAttributeList.SetFloat(nint id, double value)
	{
		var (ptr3, ptr4) = ((IUnmanagedVirtualMethodTableProvider)this).GetVirtualMethodTableInfoForKey(typeof(IAttributeList));
		int result = ((delegate* unmanaged[MemberFunction]<void*, nint, double, int>)ptr4[5])(ptr3, id, value);
		GC.KeepAlive(this);
		return result;
	}

	[GeneratedCode("Microsoft.Interop.ComInterfaceGenerator", "8.0.14.32403")]
	[SkipLocalsInit]
	unsafe int IAttributeList.GetFloat(nint id, out double value)
	{
		var (ptr3, ptr4) = ((IUnmanagedVirtualMethodTableProvider)this).GetVirtualMethodTableInfoForKey(typeof(IAttributeList));
		Unsafe.SkipInit<double>(out value);
		int result;
		fixed (double* ptr5 = &value)
		{
			result = ((delegate* unmanaged[MemberFunction]<void*, nint, double*, int>)ptr4[6])(ptr3, id, ptr5);
		}
		GC.KeepAlive(this);
		return result;
	}

	[GeneratedCode("Microsoft.Interop.ComInterfaceGenerator", "8.0.14.32403")]
	[SkipLocalsInit]
	unsafe int IAttributeList.SetString(nint id, nint str)
	{
		var (ptr3, ptr4) = ((IUnmanagedVirtualMethodTableProvider)this).GetVirtualMethodTableInfoForKey(typeof(IAttributeList));
		int result = ((delegate* unmanaged[MemberFunction]<void*, nint, nint, int>)ptr4[7])(ptr3, id, str);
		GC.KeepAlive(this);
		return result;
	}

	[GeneratedCode("Microsoft.Interop.ComInterfaceGenerator", "8.0.14.32403")]
	[SkipLocalsInit]
	unsafe int IAttributeList.GetString(nint id, nint str, uint sizeInBytes)
	{
		var (ptr3, ptr4) = ((IUnmanagedVirtualMethodTableProvider)this).GetVirtualMethodTableInfoForKey(typeof(IAttributeList));
		int result = ((delegate* unmanaged[MemberFunction]<void*, nint, nint, uint, int>)ptr4[8])(ptr3, id, str, sizeInBytes);
		GC.KeepAlive(this);
		return result;
	}

	[GeneratedCode("Microsoft.Interop.ComInterfaceGenerator", "8.0.14.32403")]
	[SkipLocalsInit]
	unsafe int IAttributeList.SetBinary(nint id, nint data, uint sizeInBytes)
	{
		var (ptr3, ptr4) = ((IUnmanagedVirtualMethodTableProvider)this).GetVirtualMethodTableInfoForKey(typeof(IAttributeList));
		int result = ((delegate* unmanaged[MemberFunction]<void*, nint, nint, uint, int>)ptr4[9])(ptr3, id, data, sizeInBytes);
		GC.KeepAlive(this);
		return result;
	}

	[GeneratedCode("Microsoft.Interop.ComInterfaceGenerator", "8.0.14.32403")]
	[SkipLocalsInit]
	unsafe int IAttributeList.GetBinary(nint id, out nint data, out uint sizeInBytes)
	{
		var (ptr3, ptr4) = ((IUnmanagedVirtualMethodTableProvider)this).GetVirtualMethodTableInfoForKey(typeof(IAttributeList));
		Unsafe.SkipInit<nint>(out data);
		Unsafe.SkipInit<uint>(out sizeInBytes);
		int result;
		fixed (uint* ptr5 = &sizeInBytes)
		{
			fixed (nint* ptr6 = &data)
			{
				result = ((delegate* unmanaged[MemberFunction]<void*, nint, nint*, uint*, int>)ptr4[10])(ptr3, id, ptr6, ptr5);
			}
		}
		GC.KeepAlive(this);
		return result;
	}

	[UnmanagedCallersOnly(CallConvs = new Type[] { typeof(CallConvMemberFunction) })]
	internal unsafe static int ABI_SetInt(ComWrappers.ComInterfaceDispatch* __this_native, nint id, long value)
	{
		int num = 0;
		try
		{
			return ComWrappers.ComInterfaceDispatch.GetInstance<IAttributeList>(__this_native).SetInt(id, value);
		}
		catch (Exception e)
		{
			return ExceptionAsHResultMarshaller<int>.ConvertToUnmanaged(e);
		}
	}

	[UnmanagedCallersOnly(CallConvs = new Type[] { typeof(CallConvMemberFunction) })]
	internal unsafe static int ABI_GetInt(ComWrappers.ComInterfaceDispatch* __this_native, nint id, long* __value_native__param)
	{
		ref long reference = ref *__value_native__param;
		long value = 0L;
		int num = 0;
		try
		{
			num = ComWrappers.ComInterfaceDispatch.GetInstance<IAttributeList>(__this_native).GetInt(id, out value);
			reference = value;
		}
		catch (Exception e)
		{
			num = ExceptionAsHResultMarshaller<int>.ConvertToUnmanaged(e);
		}
		return num;
	}

	[UnmanagedCallersOnly(CallConvs = new Type[] { typeof(CallConvMemberFunction) })]
	internal unsafe static int ABI_SetFloat(ComWrappers.ComInterfaceDispatch* __this_native, nint id, double value)
	{
		int num = 0;
		try
		{
			return ComWrappers.ComInterfaceDispatch.GetInstance<IAttributeList>(__this_native).SetFloat(id, value);
		}
		catch (Exception e)
		{
			return ExceptionAsHResultMarshaller<int>.ConvertToUnmanaged(e);
		}
	}

	[UnmanagedCallersOnly(CallConvs = new Type[] { typeof(CallConvMemberFunction) })]
	internal unsafe static int ABI_GetFloat(ComWrappers.ComInterfaceDispatch* __this_native, nint id, double* __value_native__param)
	{
		ref double reference = ref *__value_native__param;
		double value = 0.0;
		int num = 0;
		try
		{
			num = ComWrappers.ComInterfaceDispatch.GetInstance<IAttributeList>(__this_native).GetFloat(id, out value);
			reference = value;
		}
		catch (Exception e)
		{
			num = ExceptionAsHResultMarshaller<int>.ConvertToUnmanaged(e);
		}
		return num;
	}

	[UnmanagedCallersOnly(CallConvs = new Type[] { typeof(CallConvMemberFunction) })]
	internal unsafe static int ABI_SetString(ComWrappers.ComInterfaceDispatch* __this_native, nint id, nint str)
	{
		int num = 0;
		try
		{
			return ComWrappers.ComInterfaceDispatch.GetInstance<IAttributeList>(__this_native).SetString(id, str);
		}
		catch (Exception e)
		{
			return ExceptionAsHResultMarshaller<int>.ConvertToUnmanaged(e);
		}
	}

	[UnmanagedCallersOnly(CallConvs = new Type[] { typeof(CallConvMemberFunction) })]
	internal unsafe static int ABI_GetString(ComWrappers.ComInterfaceDispatch* __this_native, nint id, nint str, uint sizeInBytes)
	{
		int num = 0;
		try
		{
			return ComWrappers.ComInterfaceDispatch.GetInstance<IAttributeList>(__this_native).GetString(id, str, sizeInBytes);
		}
		catch (Exception e)
		{
			return ExceptionAsHResultMarshaller<int>.ConvertToUnmanaged(e);
		}
	}

	[UnmanagedCallersOnly(CallConvs = new Type[] { typeof(CallConvMemberFunction) })]
	internal unsafe static int ABI_SetBinary(ComWrappers.ComInterfaceDispatch* __this_native, nint id, nint data, uint sizeInBytes)
	{
		int num = 0;
		try
		{
			return ComWrappers.ComInterfaceDispatch.GetInstance<IAttributeList>(__this_native).SetBinary(id, data, sizeInBytes);
		}
		catch (Exception e)
		{
			return ExceptionAsHResultMarshaller<int>.ConvertToUnmanaged(e);
		}
	}

	[UnmanagedCallersOnly(CallConvs = new Type[] { typeof(CallConvMemberFunction) })]
	internal unsafe static int ABI_GetBinary(ComWrappers.ComInterfaceDispatch* __this_native, nint id, nint* __data_native__param, uint* __sizeInBytes_native__param)
	{
		ref nint reference = ref *__data_native__param;
		nint data = 0;
		ref uint reference2 = ref *__sizeInBytes_native__param;
		uint sizeInBytes = 0u;
		int num = 0;
		try
		{
			num = ComWrappers.ComInterfaceDispatch.GetInstance<IAttributeList>(__this_native).GetBinary(id, out data, out sizeInBytes);
			reference2 = sizeInBytes;
			reference = data;
		}
		catch (Exception e)
		{
			num = ExceptionAsHResultMarshaller<int>.ConvertToUnmanaged(e);
		}
		return num;
	}

	internal unsafe static void** CreateManagedVirtualFunctionTable()
	{
		void** ptr = (void**)RuntimeHelpers.AllocateTypeAssociatedMemory(typeof(IAttributeList), sizeof(void*) * 11);
		ComWrappers.GetIUnknownImpl(out var fpQueryInterface, out var fpAddRef, out var fpRelease);
		*ptr = (void*)fpQueryInterface;
		ptr[1] = (void*)fpAddRef;
		ptr[2] = (void*)fpRelease;
		ptr[3] = (delegate* unmanaged[MemberFunction]<ComWrappers.ComInterfaceDispatch*, nint, long, int>)(&ABI_SetInt);
		ptr[4] = (delegate* unmanaged[MemberFunction]<ComWrappers.ComInterfaceDispatch*, nint, long*, int>)(&ABI_GetInt);
		ptr[5] = (delegate* unmanaged[MemberFunction]<ComWrappers.ComInterfaceDispatch*, nint, double, int>)(&ABI_SetFloat);
		ptr[6] = (delegate* unmanaged[MemberFunction]<ComWrappers.ComInterfaceDispatch*, nint, double*, int>)(&ABI_GetFloat);
		ptr[7] = (delegate* unmanaged[MemberFunction]<ComWrappers.ComInterfaceDispatch*, nint, nint, int>)(&ABI_SetString);
		ptr[8] = (delegate* unmanaged[MemberFunction]<ComWrappers.ComInterfaceDispatch*, nint, nint, uint, int>)(&ABI_GetString);
		ptr[9] = (delegate* unmanaged[MemberFunction]<ComWrappers.ComInterfaceDispatch*, nint, nint, uint, int>)(&ABI_SetBinary);
		ptr[10] = (delegate* unmanaged[MemberFunction]<ComWrappers.ComInterfaceDispatch*, nint, nint*, uint*, int>)(&ABI_GetBinary);
		return ptr;
	}
}
