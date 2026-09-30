using System;
using System.CodeDom.Compiler;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using SonicWave.Audio.Vst3;

[DynamicInterfaceCastableImplementation]
internal interface _003CSonicWave_Audio_Vst3_IAudioProcessor_003EF2B134E5FDF3DB13EDF17356DEC142B3C1C1BF2010B588AF8D92850228901A423__InterfaceImplementation : IAudioProcessor
{
	[GeneratedCode("Microsoft.Interop.ComInterfaceGenerator", "8.0.14.32403")]
	[SkipLocalsInit]
	unsafe int IAudioProcessor.SetBusArrangements(nint inputs, int numIns, nint outputs, int numOuts)
	{
		var (ptr3, ptr4) = ((IUnmanagedVirtualMethodTableProvider)this).GetVirtualMethodTableInfoForKey(typeof(IAudioProcessor));
		int result = ((delegate* unmanaged[MemberFunction]<void*, nint, int, nint, int, int>)ptr4[3])(ptr3, inputs, numIns, outputs, numOuts);
		GC.KeepAlive(this);
		return result;
	}

	[GeneratedCode("Microsoft.Interop.ComInterfaceGenerator", "8.0.14.32403")]
	[SkipLocalsInit]
	unsafe int IAudioProcessor.GetBusArrangement(int direction, int index, nint arrangement)
	{
		var (ptr3, ptr4) = ((IUnmanagedVirtualMethodTableProvider)this).GetVirtualMethodTableInfoForKey(typeof(IAudioProcessor));
		int result = ((delegate* unmanaged[MemberFunction]<void*, int, int, nint, int>)ptr4[4])(ptr3, direction, index, arrangement);
		GC.KeepAlive(this);
		return result;
	}

	[GeneratedCode("Microsoft.Interop.ComInterfaceGenerator", "8.0.14.32403")]
	[SkipLocalsInit]
	unsafe int IAudioProcessor.CanProcessSampleSize(int symbolicSampleSize)
	{
		var (ptr3, ptr4) = ((IUnmanagedVirtualMethodTableProvider)this).GetVirtualMethodTableInfoForKey(typeof(IAudioProcessor));
		int result = ((delegate* unmanaged[MemberFunction]<void*, int, int>)ptr4[5])(ptr3, symbolicSampleSize);
		GC.KeepAlive(this);
		return result;
	}

	[GeneratedCode("Microsoft.Interop.ComInterfaceGenerator", "8.0.14.32403")]
	[SkipLocalsInit]
	unsafe uint IAudioProcessor.GetLatencySamples()
	{
		var (ptr3, ptr4) = ((IUnmanagedVirtualMethodTableProvider)this).GetVirtualMethodTableInfoForKey(typeof(IAudioProcessor));
		uint result = ((delegate* unmanaged[MemberFunction]<void*, uint>)ptr4[6])(ptr3);
		GC.KeepAlive(this);
		return result;
	}

	[GeneratedCode("Microsoft.Interop.ComInterfaceGenerator", "8.0.14.32403")]
	[SkipLocalsInit]
	unsafe int IAudioProcessor.SetupProcessing(ref ProcessSetup setup)
	{
		var (ptr3, ptr4) = ((IUnmanagedVirtualMethodTableProvider)this).GetVirtualMethodTableInfoForKey(typeof(IAudioProcessor));
		int result;
		fixed (ProcessSetup* ptr5 = &setup)
		{
			result = ((delegate* unmanaged[MemberFunction]<void*, ProcessSetup*, int>)ptr4[7])(ptr3, ptr5);
		}
		GC.KeepAlive(this);
		return result;
	}

	[GeneratedCode("Microsoft.Interop.ComInterfaceGenerator", "8.0.14.32403")]
	[SkipLocalsInit]
	unsafe int IAudioProcessor.SetProcessing(byte state)
	{
		var (ptr3, ptr4) = ((IUnmanagedVirtualMethodTableProvider)this).GetVirtualMethodTableInfoForKey(typeof(IAudioProcessor));
		int result = ((delegate* unmanaged[MemberFunction]<void*, byte, int>)ptr4[8])(ptr3, state);
		GC.KeepAlive(this);
		return result;
	}

	[GeneratedCode("Microsoft.Interop.ComInterfaceGenerator", "8.0.14.32403")]
	[SkipLocalsInit]
	unsafe int IAudioProcessor.Process(ref ProcessData data)
	{
		var (ptr3, ptr4) = ((IUnmanagedVirtualMethodTableProvider)this).GetVirtualMethodTableInfoForKey(typeof(IAudioProcessor));
		int result;
		fixed (ProcessData* ptr5 = &data)
		{
			result = ((delegate* unmanaged[MemberFunction]<void*, ProcessData*, int>)ptr4[9])(ptr3, ptr5);
		}
		GC.KeepAlive(this);
		return result;
	}

	[GeneratedCode("Microsoft.Interop.ComInterfaceGenerator", "8.0.14.32403")]
	[SkipLocalsInit]
	unsafe uint IAudioProcessor.GetTailSamples()
	{
		var (ptr3, ptr4) = ((IUnmanagedVirtualMethodTableProvider)this).GetVirtualMethodTableInfoForKey(typeof(IAudioProcessor));
		uint result = ((delegate* unmanaged[MemberFunction]<void*, uint>)ptr4[10])(ptr3);
		GC.KeepAlive(this);
		return result;
	}

	[UnmanagedCallersOnly(CallConvs = new Type[] { typeof(CallConvMemberFunction) })]
	internal unsafe static int ABI_SetBusArrangements(ComWrappers.ComInterfaceDispatch* __this_native, nint inputs, int numIns, nint outputs, int numOuts)
	{
		int num = 0;
		try
		{
			return ComWrappers.ComInterfaceDispatch.GetInstance<IAudioProcessor>(__this_native).SetBusArrangements(inputs, numIns, outputs, numOuts);
		}
		catch (Exception e)
		{
			return ExceptionAsHResultMarshaller<int>.ConvertToUnmanaged(e);
		}
	}

	[UnmanagedCallersOnly(CallConvs = new Type[] { typeof(CallConvMemberFunction) })]
	internal unsafe static int ABI_GetBusArrangement(ComWrappers.ComInterfaceDispatch* __this_native, int direction, int index, nint arrangement)
	{
		int num = 0;
		try
		{
			return ComWrappers.ComInterfaceDispatch.GetInstance<IAudioProcessor>(__this_native).GetBusArrangement(direction, index, arrangement);
		}
		catch (Exception e)
		{
			return ExceptionAsHResultMarshaller<int>.ConvertToUnmanaged(e);
		}
	}

	[UnmanagedCallersOnly(CallConvs = new Type[] { typeof(CallConvMemberFunction) })]
	internal unsafe static int ABI_CanProcessSampleSize(ComWrappers.ComInterfaceDispatch* __this_native, int symbolicSampleSize)
	{
		int num = 0;
		try
		{
			return ComWrappers.ComInterfaceDispatch.GetInstance<IAudioProcessor>(__this_native).CanProcessSampleSize(symbolicSampleSize);
		}
		catch (Exception e)
		{
			return ExceptionAsHResultMarshaller<int>.ConvertToUnmanaged(e);
		}
	}

	[UnmanagedCallersOnly(CallConvs = new Type[] { typeof(CallConvMemberFunction) })]
	internal unsafe static uint ABI_GetLatencySamples(ComWrappers.ComInterfaceDispatch* __this_native)
	{
		uint num = 0u;
		try
		{
			return ComWrappers.ComInterfaceDispatch.GetInstance<IAudioProcessor>(__this_native).GetLatencySamples();
		}
		catch (Exception e)
		{
			return ExceptionAsHResultMarshaller<uint>.ConvertToUnmanaged(e);
		}
	}

	[UnmanagedCallersOnly(CallConvs = new Type[] { typeof(CallConvMemberFunction) })]
	internal unsafe static int ABI_SetupProcessing(ComWrappers.ComInterfaceDispatch* __this_native, ProcessSetup* __setup_native__param)
	{
		ref ProcessSetup reference = ref *__setup_native__param;
		ProcessSetup processSetup = default;
		int num = 0;
		try
		{
			processSetup = reference;
			num = ComWrappers.ComInterfaceDispatch.GetInstance<IAudioProcessor>(__this_native).SetupProcessing(ref processSetup);
			reference = processSetup;
		}
		catch (Exception e)
		{
			num = ExceptionAsHResultMarshaller<int>.ConvertToUnmanaged(e);
		}
		return num;
	}

	[UnmanagedCallersOnly(CallConvs = new Type[] { typeof(CallConvMemberFunction) })]
	internal unsafe static int ABI_SetProcessing(ComWrappers.ComInterfaceDispatch* __this_native, byte state)
	{
		int num = 0;
		try
		{
			return ComWrappers.ComInterfaceDispatch.GetInstance<IAudioProcessor>(__this_native).SetProcessing(state);
		}
		catch (Exception e)
		{
			return ExceptionAsHResultMarshaller<int>.ConvertToUnmanaged(e);
		}
	}

	[UnmanagedCallersOnly(CallConvs = new Type[] { typeof(CallConvMemberFunction) })]
	internal unsafe static int ABI_Process(ComWrappers.ComInterfaceDispatch* __this_native, ProcessData* __data_native__param)
	{
		ref ProcessData reference = ref *__data_native__param;
		ProcessData processData = default;
		int num = 0;
		try
		{
			processData = reference;
			num = ComWrappers.ComInterfaceDispatch.GetInstance<IAudioProcessor>(__this_native).Process(ref processData);
			reference = processData;
		}
		catch (Exception e)
		{
			num = ExceptionAsHResultMarshaller<int>.ConvertToUnmanaged(e);
		}
		return num;
	}

	[UnmanagedCallersOnly(CallConvs = new Type[] { typeof(CallConvMemberFunction) })]
	internal unsafe static uint ABI_GetTailSamples(ComWrappers.ComInterfaceDispatch* __this_native)
	{
		uint num = 0u;
		try
		{
			return ComWrappers.ComInterfaceDispatch.GetInstance<IAudioProcessor>(__this_native).GetTailSamples();
		}
		catch (Exception e)
		{
			return ExceptionAsHResultMarshaller<uint>.ConvertToUnmanaged(e);
		}
	}

	internal unsafe static void** CreateManagedVirtualFunctionTable()
	{
		void** ptr = (void**)RuntimeHelpers.AllocateTypeAssociatedMemory(typeof(IAudioProcessor), sizeof(void*) * 11);
		ComWrappers.GetIUnknownImpl(out var fpQueryInterface, out var fpAddRef, out var fpRelease);
		*ptr = (void*)fpQueryInterface;
		ptr[1] = (void*)fpAddRef;
		ptr[2] = (void*)fpRelease;
		ptr[3] = (delegate* unmanaged[MemberFunction]<ComWrappers.ComInterfaceDispatch*, nint, int, nint, int, int>)(&ABI_SetBusArrangements);
		ptr[4] = (delegate* unmanaged[MemberFunction]<ComWrappers.ComInterfaceDispatch*, int, int, nint, int>)(&ABI_GetBusArrangement);
		ptr[5] = (delegate* unmanaged[MemberFunction]<ComWrappers.ComInterfaceDispatch*, int, int>)(&ABI_CanProcessSampleSize);
		ptr[6] = (delegate* unmanaged[MemberFunction]<ComWrappers.ComInterfaceDispatch*, uint>)(&ABI_GetLatencySamples);
		ptr[7] = (delegate* unmanaged[MemberFunction]<ComWrappers.ComInterfaceDispatch*, ProcessSetup*, int>)(&ABI_SetupProcessing);
		ptr[8] = (delegate* unmanaged[MemberFunction]<ComWrappers.ComInterfaceDispatch*, byte, int>)(&ABI_SetProcessing);
		ptr[9] = (delegate* unmanaged[MemberFunction]<ComWrappers.ComInterfaceDispatch*, ProcessData*, int>)(&ABI_Process);
		ptr[10] = (delegate* unmanaged[MemberFunction]<ComWrappers.ComInterfaceDispatch*, uint>)(&ABI_GetTailSamples);
		return ptr;
	}
}
