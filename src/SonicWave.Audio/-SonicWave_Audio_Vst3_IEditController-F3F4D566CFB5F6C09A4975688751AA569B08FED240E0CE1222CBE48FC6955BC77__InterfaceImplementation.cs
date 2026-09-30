using System;
using System.CodeDom.Compiler;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using SonicWave.Audio.Vst3;

[DynamicInterfaceCastableImplementation]
internal interface _003CSonicWave_Audio_Vst3_IEditController_003EF3F4D566CFB5F6C09A4975688751AA569B08FED240E0CE1222CBE48FC6955BC77__InterfaceImplementation : IEditController
{
	[GeneratedCode("Microsoft.Interop.ComInterfaceGenerator", "8.0.14.32403")]
	[SkipLocalsInit]
	unsafe int IEditController.Initialize(nint context)
	{
		var (ptr3, ptr4) = ((IUnmanagedVirtualMethodTableProvider)this).GetVirtualMethodTableInfoForKey(typeof(IEditController));
		int result = ((delegate* unmanaged[MemberFunction]<void*, nint, int>)ptr4[3])(ptr3, context);
		GC.KeepAlive(this);
		return result;
	}

	[GeneratedCode("Microsoft.Interop.ComInterfaceGenerator", "8.0.14.32403")]
	[SkipLocalsInit]
	unsafe int IEditController.Terminate()
	{
		var (ptr3, ptr4) = ((IUnmanagedVirtualMethodTableProvider)this).GetVirtualMethodTableInfoForKey(typeof(IEditController));
		int result = ((delegate* unmanaged[MemberFunction]<void*, int>)ptr4[4])(ptr3);
		GC.KeepAlive(this);
		return result;
	}

	[GeneratedCode("Microsoft.Interop.ComInterfaceGenerator", "8.0.14.32403")]
	[SkipLocalsInit]
	unsafe int IEditController.SetComponentState(nint stream)
	{
		var (ptr3, ptr4) = ((IUnmanagedVirtualMethodTableProvider)this).GetVirtualMethodTableInfoForKey(typeof(IEditController));
		int result = ((delegate* unmanaged[MemberFunction]<void*, nint, int>)ptr4[5])(ptr3, stream);
		GC.KeepAlive(this);
		return result;
	}

	[GeneratedCode("Microsoft.Interop.ComInterfaceGenerator", "8.0.14.32403")]
	[SkipLocalsInit]
	unsafe int IEditController.SetState(nint stream)
	{
		var (ptr3, ptr4) = ((IUnmanagedVirtualMethodTableProvider)this).GetVirtualMethodTableInfoForKey(typeof(IEditController));
		int result = ((delegate* unmanaged[MemberFunction]<void*, nint, int>)ptr4[6])(ptr3, stream);
		GC.KeepAlive(this);
		return result;
	}

	[GeneratedCode("Microsoft.Interop.ComInterfaceGenerator", "8.0.14.32403")]
	[SkipLocalsInit]
	unsafe int IEditController.GetState(nint stream)
	{
		var (ptr3, ptr4) = ((IUnmanagedVirtualMethodTableProvider)this).GetVirtualMethodTableInfoForKey(typeof(IEditController));
		int result = ((delegate* unmanaged[MemberFunction]<void*, nint, int>)ptr4[7])(ptr3, stream);
		GC.KeepAlive(this);
		return result;
	}

	[GeneratedCode("Microsoft.Interop.ComInterfaceGenerator", "8.0.14.32403")]
	[SkipLocalsInit]
	unsafe int IEditController.GetParameterCount()
	{
		var (ptr3, ptr4) = ((IUnmanagedVirtualMethodTableProvider)this).GetVirtualMethodTableInfoForKey(typeof(IEditController));
		int result = ((delegate* unmanaged[MemberFunction]<void*, int>)ptr4[8])(ptr3);
		GC.KeepAlive(this);
		return result;
	}

	[GeneratedCode("Microsoft.Interop.ComInterfaceGenerator", "8.0.14.32403")]
	[SkipLocalsInit]
	unsafe int IEditController.GetParameterInfo(int paramIndex, nint info)
	{
		var (ptr3, ptr4) = ((IUnmanagedVirtualMethodTableProvider)this).GetVirtualMethodTableInfoForKey(typeof(IEditController));
		int result = ((delegate* unmanaged[MemberFunction]<void*, int, nint, int>)ptr4[9])(ptr3, paramIndex, info);
		GC.KeepAlive(this);
		return result;
	}

	[GeneratedCode("Microsoft.Interop.ComInterfaceGenerator", "8.0.14.32403")]
	[SkipLocalsInit]
	unsafe int IEditController.GetParamStringByValue(uint id, double valueNormalized, nint str)
	{
		var (ptr3, ptr4) = ((IUnmanagedVirtualMethodTableProvider)this).GetVirtualMethodTableInfoForKey(typeof(IEditController));
		int result = ((delegate* unmanaged[MemberFunction]<void*, uint, double, nint, int>)ptr4[10])(ptr3, id, valueNormalized, str);
		GC.KeepAlive(this);
		return result;
	}

	[GeneratedCode("Microsoft.Interop.ComInterfaceGenerator", "8.0.14.32403")]
	[SkipLocalsInit]
	unsafe int IEditController.GetParamValueByString(uint id, nint str, out double valueNormalized)
	{
		var (ptr3, ptr4) = ((IUnmanagedVirtualMethodTableProvider)this).GetVirtualMethodTableInfoForKey(typeof(IEditController));
		Unsafe.SkipInit<double>(out valueNormalized);
		int result;
		fixed (double* ptr5 = &valueNormalized)
		{
			result = ((delegate* unmanaged[MemberFunction]<void*, uint, nint, double*, int>)ptr4[11])(ptr3, id, str, ptr5);
		}
		GC.KeepAlive(this);
		return result;
	}

	[GeneratedCode("Microsoft.Interop.ComInterfaceGenerator", "8.0.14.32403")]
	[SkipLocalsInit]
	unsafe double IEditController.NormalizedParamToPlain(uint id, double valueNormalized)
	{
		var (ptr3, ptr4) = ((IUnmanagedVirtualMethodTableProvider)this).GetVirtualMethodTableInfoForKey(typeof(IEditController));
		double result = ((delegate* unmanaged[MemberFunction]<void*, uint, double, double>)ptr4[12])(ptr3, id, valueNormalized);
		GC.KeepAlive(this);
		return result;
	}

	[GeneratedCode("Microsoft.Interop.ComInterfaceGenerator", "8.0.14.32403")]
	[SkipLocalsInit]
	unsafe double IEditController.PlainParamToNormalized(uint id, double plainValue)
	{
		var (ptr3, ptr4) = ((IUnmanagedVirtualMethodTableProvider)this).GetVirtualMethodTableInfoForKey(typeof(IEditController));
		double result = ((delegate* unmanaged[MemberFunction]<void*, uint, double, double>)ptr4[13])(ptr3, id, plainValue);
		GC.KeepAlive(this);
		return result;
	}

	[GeneratedCode("Microsoft.Interop.ComInterfaceGenerator", "8.0.14.32403")]
	[SkipLocalsInit]
	unsafe double IEditController.GetParamNormalized(uint id)
	{
		var (ptr3, ptr4) = ((IUnmanagedVirtualMethodTableProvider)this).GetVirtualMethodTableInfoForKey(typeof(IEditController));
		double result = ((delegate* unmanaged[MemberFunction]<void*, uint, double>)ptr4[14])(ptr3, id);
		GC.KeepAlive(this);
		return result;
	}

	[GeneratedCode("Microsoft.Interop.ComInterfaceGenerator", "8.0.14.32403")]
	[SkipLocalsInit]
	unsafe int IEditController.SetParamNormalized(uint id, double value)
	{
		var (ptr3, ptr4) = ((IUnmanagedVirtualMethodTableProvider)this).GetVirtualMethodTableInfoForKey(typeof(IEditController));
		int result = ((delegate* unmanaged[MemberFunction]<void*, uint, double, int>)ptr4[15])(ptr3, id, value);
		GC.KeepAlive(this);
		return result;
	}

	[GeneratedCode("Microsoft.Interop.ComInterfaceGenerator", "8.0.14.32403")]
	[SkipLocalsInit]
	unsafe int IEditController.SetComponentHandler(nint handler)
	{
		var (ptr3, ptr4) = ((IUnmanagedVirtualMethodTableProvider)this).GetVirtualMethodTableInfoForKey(typeof(IEditController));
		int result = ((delegate* unmanaged[MemberFunction]<void*, nint, int>)ptr4[16])(ptr3, handler);
		GC.KeepAlive(this);
		return result;
	}

	[GeneratedCode("Microsoft.Interop.ComInterfaceGenerator", "8.0.14.32403")]
	[SkipLocalsInit]
	unsafe nint IEditController.CreateView(nint name)
	{
		var (ptr3, ptr4) = ((IUnmanagedVirtualMethodTableProvider)this).GetVirtualMethodTableInfoForKey(typeof(IEditController));
		nint result = ((delegate* unmanaged[MemberFunction]<void*, nint, nint>)ptr4[17])(ptr3, name);
		GC.KeepAlive(this);
		return result;
	}

	[UnmanagedCallersOnly(CallConvs = new Type[] { typeof(CallConvMemberFunction) })]
	internal unsafe static int ABI_Initialize(ComWrappers.ComInterfaceDispatch* __this_native, nint context)
	{
		int num = 0;
		try
		{
			return ComWrappers.ComInterfaceDispatch.GetInstance<IEditController>(__this_native).Initialize(context);
		}
		catch (Exception e)
		{
			return ExceptionAsHResultMarshaller<int>.ConvertToUnmanaged(e);
		}
	}

	[UnmanagedCallersOnly(CallConvs = new Type[] { typeof(CallConvMemberFunction) })]
	internal unsafe static int ABI_Terminate(ComWrappers.ComInterfaceDispatch* __this_native)
	{
		int num = 0;
		try
		{
			return ComWrappers.ComInterfaceDispatch.GetInstance<IEditController>(__this_native).Terminate();
		}
		catch (Exception e)
		{
			return ExceptionAsHResultMarshaller<int>.ConvertToUnmanaged(e);
		}
	}

	[UnmanagedCallersOnly(CallConvs = new Type[] { typeof(CallConvMemberFunction) })]
	internal unsafe static int ABI_SetComponentState(ComWrappers.ComInterfaceDispatch* __this_native, nint stream)
	{
		int num = 0;
		try
		{
			return ComWrappers.ComInterfaceDispatch.GetInstance<IEditController>(__this_native).SetComponentState(stream);
		}
		catch (Exception e)
		{
			return ExceptionAsHResultMarshaller<int>.ConvertToUnmanaged(e);
		}
	}

	[UnmanagedCallersOnly(CallConvs = new Type[] { typeof(CallConvMemberFunction) })]
	internal unsafe static int ABI_SetState(ComWrappers.ComInterfaceDispatch* __this_native, nint stream)
	{
		int num = 0;
		try
		{
			return ComWrappers.ComInterfaceDispatch.GetInstance<IEditController>(__this_native).SetState(stream);
		}
		catch (Exception e)
		{
			return ExceptionAsHResultMarshaller<int>.ConvertToUnmanaged(e);
		}
	}

	[UnmanagedCallersOnly(CallConvs = new Type[] { typeof(CallConvMemberFunction) })]
	internal unsafe static int ABI_GetState(ComWrappers.ComInterfaceDispatch* __this_native, nint stream)
	{
		int num = 0;
		try
		{
			return ComWrappers.ComInterfaceDispatch.GetInstance<IEditController>(__this_native).GetState(stream);
		}
		catch (Exception e)
		{
			return ExceptionAsHResultMarshaller<int>.ConvertToUnmanaged(e);
		}
	}

	[UnmanagedCallersOnly(CallConvs = new Type[] { typeof(CallConvMemberFunction) })]
	internal unsafe static int ABI_GetParameterCount(ComWrappers.ComInterfaceDispatch* __this_native)
	{
		int num = 0;
		try
		{
			return ComWrappers.ComInterfaceDispatch.GetInstance<IEditController>(__this_native).GetParameterCount();
		}
		catch (Exception e)
		{
			return ExceptionAsHResultMarshaller<int>.ConvertToUnmanaged(e);
		}
	}

	[UnmanagedCallersOnly(CallConvs = new Type[] { typeof(CallConvMemberFunction) })]
	internal unsafe static int ABI_GetParameterInfo(ComWrappers.ComInterfaceDispatch* __this_native, int paramIndex, nint info)
	{
		int num = 0;
		try
		{
			return ComWrappers.ComInterfaceDispatch.GetInstance<IEditController>(__this_native).GetParameterInfo(paramIndex, info);
		}
		catch (Exception e)
		{
			return ExceptionAsHResultMarshaller<int>.ConvertToUnmanaged(e);
		}
	}

	[UnmanagedCallersOnly(CallConvs = new Type[] { typeof(CallConvMemberFunction) })]
	internal unsafe static int ABI_GetParamStringByValue(ComWrappers.ComInterfaceDispatch* __this_native, uint id, double valueNormalized, nint str)
	{
		int num = 0;
		try
		{
			return ComWrappers.ComInterfaceDispatch.GetInstance<IEditController>(__this_native).GetParamStringByValue(id, valueNormalized, str);
		}
		catch (Exception e)
		{
			return ExceptionAsHResultMarshaller<int>.ConvertToUnmanaged(e);
		}
	}

	[UnmanagedCallersOnly(CallConvs = new Type[] { typeof(CallConvMemberFunction) })]
	internal unsafe static int ABI_GetParamValueByString(ComWrappers.ComInterfaceDispatch* __this_native, uint id, nint str, double* __valueNormalized_native__param)
	{
		ref double reference = ref *__valueNormalized_native__param;
		double valueNormalized = 0.0;
		int num = 0;
		try
		{
			num = ComWrappers.ComInterfaceDispatch.GetInstance<IEditController>(__this_native).GetParamValueByString(id, str, out valueNormalized);
			reference = valueNormalized;
		}
		catch (Exception e)
		{
			num = ExceptionAsHResultMarshaller<int>.ConvertToUnmanaged(e);
		}
		return num;
	}

	[UnmanagedCallersOnly(CallConvs = new Type[] { typeof(CallConvMemberFunction) })]
	internal unsafe static double ABI_NormalizedParamToPlain(ComWrappers.ComInterfaceDispatch* __this_native, uint id, double valueNormalized)
	{
		double num = 0.0;
		try
		{
			return ComWrappers.ComInterfaceDispatch.GetInstance<IEditController>(__this_native).NormalizedParamToPlain(id, valueNormalized);
		}
		catch (Exception e)
		{
			return ExceptionAsNaNMarshaller<double>.ConvertToUnmanaged(e);
		}
	}

	[UnmanagedCallersOnly(CallConvs = new Type[] { typeof(CallConvMemberFunction) })]
	internal unsafe static double ABI_PlainParamToNormalized(ComWrappers.ComInterfaceDispatch* __this_native, uint id, double plainValue)
	{
		double num = 0.0;
		try
		{
			return ComWrappers.ComInterfaceDispatch.GetInstance<IEditController>(__this_native).PlainParamToNormalized(id, plainValue);
		}
		catch (Exception e)
		{
			return ExceptionAsNaNMarshaller<double>.ConvertToUnmanaged(e);
		}
	}

	[UnmanagedCallersOnly(CallConvs = new Type[] { typeof(CallConvMemberFunction) })]
	internal unsafe static double ABI_GetParamNormalized(ComWrappers.ComInterfaceDispatch* __this_native, uint id)
	{
		double num = 0.0;
		try
		{
			return ComWrappers.ComInterfaceDispatch.GetInstance<IEditController>(__this_native).GetParamNormalized(id);
		}
		catch (Exception e)
		{
			return ExceptionAsNaNMarshaller<double>.ConvertToUnmanaged(e);
		}
	}

	[UnmanagedCallersOnly(CallConvs = new Type[] { typeof(CallConvMemberFunction) })]
	internal unsafe static int ABI_SetParamNormalized(ComWrappers.ComInterfaceDispatch* __this_native, uint id, double value)
	{
		int num = 0;
		try
		{
			return ComWrappers.ComInterfaceDispatch.GetInstance<IEditController>(__this_native).SetParamNormalized(id, value);
		}
		catch (Exception e)
		{
			return ExceptionAsHResultMarshaller<int>.ConvertToUnmanaged(e);
		}
	}

	[UnmanagedCallersOnly(CallConvs = new Type[] { typeof(CallConvMemberFunction) })]
	internal unsafe static int ABI_SetComponentHandler(ComWrappers.ComInterfaceDispatch* __this_native, nint handler)
	{
		int num = 0;
		try
		{
			return ComWrappers.ComInterfaceDispatch.GetInstance<IEditController>(__this_native).SetComponentHandler(handler);
		}
		catch (Exception e)
		{
			return ExceptionAsHResultMarshaller<int>.ConvertToUnmanaged(e);
		}
	}

	[UnmanagedCallersOnly(CallConvs = new Type[] { typeof(CallConvMemberFunction) })]
	internal unsafe static nint ABI_CreateView(ComWrappers.ComInterfaceDispatch* __this_native, nint name)
	{
		nint num = 0;
		try
		{
			return ComWrappers.ComInterfaceDispatch.GetInstance<IEditController>(__this_native).CreateView(name);
		}
		catch (Exception e)
		{
			return ExceptionAsDefaultMarshaller<nint>.ConvertToUnmanaged(e);
		}
	}

	internal unsafe static void** CreateManagedVirtualFunctionTable()
	{
		void** ptr = (void**)RuntimeHelpers.AllocateTypeAssociatedMemory(typeof(IEditController), sizeof(void*) * 18);
		ComWrappers.GetIUnknownImpl(out var fpQueryInterface, out var fpAddRef, out var fpRelease);
		*ptr = (void*)fpQueryInterface;
		ptr[1] = (void*)fpAddRef;
		ptr[2] = (void*)fpRelease;
		ptr[3] = (delegate* unmanaged[MemberFunction]<ComWrappers.ComInterfaceDispatch*, nint, int>)(&ABI_Initialize);
		ptr[4] = (delegate* unmanaged[MemberFunction]<ComWrappers.ComInterfaceDispatch*, int>)(&ABI_Terminate);
		ptr[5] = (delegate* unmanaged[MemberFunction]<ComWrappers.ComInterfaceDispatch*, nint, int>)(&ABI_SetComponentState);
		ptr[6] = (delegate* unmanaged[MemberFunction]<ComWrappers.ComInterfaceDispatch*, nint, int>)(&ABI_SetState);
		ptr[7] = (delegate* unmanaged[MemberFunction]<ComWrappers.ComInterfaceDispatch*, nint, int>)(&ABI_GetState);
		ptr[8] = (delegate* unmanaged[MemberFunction]<ComWrappers.ComInterfaceDispatch*, int>)(&ABI_GetParameterCount);
		ptr[9] = (delegate* unmanaged[MemberFunction]<ComWrappers.ComInterfaceDispatch*, int, nint, int>)(&ABI_GetParameterInfo);
		ptr[10] = (delegate* unmanaged[MemberFunction]<ComWrappers.ComInterfaceDispatch*, uint, double, nint, int>)(&ABI_GetParamStringByValue);
		ptr[11] = (delegate* unmanaged[MemberFunction]<ComWrappers.ComInterfaceDispatch*, uint, nint, double*, int>)(&ABI_GetParamValueByString);
		ptr[12] = (delegate* unmanaged[MemberFunction]<ComWrappers.ComInterfaceDispatch*, uint, double, double>)(&ABI_NormalizedParamToPlain);
		ptr[13] = (delegate* unmanaged[MemberFunction]<ComWrappers.ComInterfaceDispatch*, uint, double, double>)(&ABI_PlainParamToNormalized);
		ptr[14] = (delegate* unmanaged[MemberFunction]<ComWrappers.ComInterfaceDispatch*, uint, double>)(&ABI_GetParamNormalized);
		ptr[15] = (delegate* unmanaged[MemberFunction]<ComWrappers.ComInterfaceDispatch*, uint, double, int>)(&ABI_SetParamNormalized);
		ptr[16] = (delegate* unmanaged[MemberFunction]<ComWrappers.ComInterfaceDispatch*, nint, int>)(&ABI_SetComponentHandler);
		ptr[17] = (delegate* unmanaged[MemberFunction]<ComWrappers.ComInterfaceDispatch*, nint, nint>)(&ABI_CreateView);
		return ptr;
	}
}
