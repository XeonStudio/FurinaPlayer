using System;
using System.CodeDom.Compiler;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using SonicWave.Audio.Vst3;

[DynamicInterfaceCastableImplementation]
internal interface _003CSonicWave_Audio_Vst3_IParamValueQueue_003EF832198D588C9107E992551106C20CF4C530DED109774B95846BB7CE73E036CED__InterfaceImplementation : IParamValueQueue
{
	[GeneratedCode("Microsoft.Interop.ComInterfaceGenerator", "8.0.14.32403")]
	[SkipLocalsInit]
	unsafe uint IParamValueQueue.GetParameterId()
	{
		var (ptr3, ptr4) = ((IUnmanagedVirtualMethodTableProvider)this).GetVirtualMethodTableInfoForKey(typeof(IParamValueQueue));
		uint result = ((delegate* unmanaged[MemberFunction]<void*, uint>)ptr4[3])(ptr3);
		GC.KeepAlive(this);
		return result;
	}

	[GeneratedCode("Microsoft.Interop.ComInterfaceGenerator", "8.0.14.32403")]
	[SkipLocalsInit]
	unsafe int IParamValueQueue.GetPointCount()
	{
		var (ptr3, ptr4) = ((IUnmanagedVirtualMethodTableProvider)this).GetVirtualMethodTableInfoForKey(typeof(IParamValueQueue));
		int result = ((delegate* unmanaged[MemberFunction]<void*, int>)ptr4[4])(ptr3);
		GC.KeepAlive(this);
		return result;
	}

	[GeneratedCode("Microsoft.Interop.ComInterfaceGenerator", "8.0.14.32403")]
	[SkipLocalsInit]
	unsafe int IParamValueQueue.GetPoint(int index, out int sampleOffset, out double value)
	{
		var (ptr3, ptr4) = ((IUnmanagedVirtualMethodTableProvider)this).GetVirtualMethodTableInfoForKey(typeof(IParamValueQueue));
		Unsafe.SkipInit<int>(out sampleOffset);
		Unsafe.SkipInit<double>(out value);
		int result;
		fixed (double* ptr5 = &value)
		{
			fixed (int* ptr6 = &sampleOffset)
			{
				result = ((delegate* unmanaged[MemberFunction]<void*, int, int*, double*, int>)ptr4[5])(ptr3, index, ptr6, ptr5);
			}
		}
		GC.KeepAlive(this);
		return result;
	}

	[GeneratedCode("Microsoft.Interop.ComInterfaceGenerator", "8.0.14.32403")]
	[SkipLocalsInit]
	unsafe int IParamValueQueue.AddPoint(int sampleOffset, double value, out int index)
	{
		var (ptr3, ptr4) = ((IUnmanagedVirtualMethodTableProvider)this).GetVirtualMethodTableInfoForKey(typeof(IParamValueQueue));
		Unsafe.SkipInit<int>(out index);
		int result;
		fixed (int* ptr5 = &index)
		{
			result = ((delegate* unmanaged[MemberFunction]<void*, int, double, int*, int>)ptr4[6])(ptr3, sampleOffset, value, ptr5);
		}
		GC.KeepAlive(this);
		return result;
	}

	[UnmanagedCallersOnly(CallConvs = new Type[] { typeof(CallConvMemberFunction) })]
	internal unsafe static uint ABI_GetParameterId(ComWrappers.ComInterfaceDispatch* __this_native)
	{
		uint num = 0u;
		try
		{
			return ComWrappers.ComInterfaceDispatch.GetInstance<IParamValueQueue>(__this_native).GetParameterId();
		}
		catch (Exception e)
		{
			return ExceptionAsHResultMarshaller<uint>.ConvertToUnmanaged(e);
		}
	}

	[UnmanagedCallersOnly(CallConvs = new Type[] { typeof(CallConvMemberFunction) })]
	internal unsafe static int ABI_GetPointCount(ComWrappers.ComInterfaceDispatch* __this_native)
	{
		int num = 0;
		try
		{
			return ComWrappers.ComInterfaceDispatch.GetInstance<IParamValueQueue>(__this_native).GetPointCount();
		}
		catch (Exception e)
		{
			return ExceptionAsHResultMarshaller<int>.ConvertToUnmanaged(e);
		}
	}

	[UnmanagedCallersOnly(CallConvs = new Type[] { typeof(CallConvMemberFunction) })]
	internal unsafe static int ABI_GetPoint(ComWrappers.ComInterfaceDispatch* __this_native, int index, int* __sampleOffset_native__param, double* __value_native__param)
	{
		ref int reference = ref *__sampleOffset_native__param;
		int sampleOffset = 0;
		ref double reference2 = ref *__value_native__param;
		double value = 0.0;
		int num = 0;
		try
		{
			num = ComWrappers.ComInterfaceDispatch.GetInstance<IParamValueQueue>(__this_native).GetPoint(index, out sampleOffset, out value);
			reference2 = value;
			reference = sampleOffset;
		}
		catch (Exception e)
		{
			num = ExceptionAsHResultMarshaller<int>.ConvertToUnmanaged(e);
		}
		return num;
	}

	[UnmanagedCallersOnly(CallConvs = new Type[] { typeof(CallConvMemberFunction) })]
	internal unsafe static int ABI_AddPoint(ComWrappers.ComInterfaceDispatch* __this_native, int sampleOffset, double value, int* __index_native__param)
	{
		ref int reference = ref *__index_native__param;
		int index = 0;
		int num = 0;
		try
		{
			num = ComWrappers.ComInterfaceDispatch.GetInstance<IParamValueQueue>(__this_native).AddPoint(sampleOffset, value, out index);
			reference = index;
		}
		catch (Exception e)
		{
			num = ExceptionAsHResultMarshaller<int>.ConvertToUnmanaged(e);
		}
		return num;
	}

	internal unsafe static void** CreateManagedVirtualFunctionTable()
	{
		void** ptr = (void**)RuntimeHelpers.AllocateTypeAssociatedMemory(typeof(IParamValueQueue), sizeof(void*) * 7);
		ComWrappers.GetIUnknownImpl(out var fpQueryInterface, out var fpAddRef, out var fpRelease);
		*ptr = (void*)fpQueryInterface;
		ptr[1] = (void*)fpAddRef;
		ptr[2] = (void*)fpRelease;
		ptr[3] = (delegate* unmanaged[MemberFunction]<ComWrappers.ComInterfaceDispatch*, uint>)(&ABI_GetParameterId);
		ptr[4] = (delegate* unmanaged[MemberFunction]<ComWrappers.ComInterfaceDispatch*, int>)(&ABI_GetPointCount);
		ptr[5] = (delegate* unmanaged[MemberFunction]<ComWrappers.ComInterfaceDispatch*, int, int*, double*, int>)(&ABI_GetPoint);
		ptr[6] = (delegate* unmanaged[MemberFunction]<ComWrappers.ComInterfaceDispatch*, int, double, int*, int>)(&ABI_AddPoint);
		return ptr;
	}
}
