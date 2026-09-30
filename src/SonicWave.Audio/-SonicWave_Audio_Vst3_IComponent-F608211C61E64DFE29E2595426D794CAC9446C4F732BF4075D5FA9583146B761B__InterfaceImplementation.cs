using System;
using System.CodeDom.Compiler;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using SonicWave.Audio.Vst3;

[DynamicInterfaceCastableImplementation]
internal interface _003CSonicWave_Audio_Vst3_IComponent_003EF608211C61E64DFE29E2595426D794CAC9446C4F732BF4075D5FA9583146B761B__InterfaceImplementation : IComponent
{
	[GeneratedCode("Microsoft.Interop.ComInterfaceGenerator", "8.0.14.32403")]
	[SkipLocalsInit]
	unsafe int IComponent.Initialize(nint context)
	{
		var (ptr3, ptr4) = ((IUnmanagedVirtualMethodTableProvider)this).GetVirtualMethodTableInfoForKey(typeof(IComponent));
		int result = ((delegate* unmanaged[MemberFunction]<void*, nint, int>)ptr4[3])(ptr3, context);
		GC.KeepAlive(this);
		return result;
	}

	[GeneratedCode("Microsoft.Interop.ComInterfaceGenerator", "8.0.14.32403")]
	[SkipLocalsInit]
	unsafe int IComponent.Terminate()
	{
		var (ptr3, ptr4) = ((IUnmanagedVirtualMethodTableProvider)this).GetVirtualMethodTableInfoForKey(typeof(IComponent));
		int result = ((delegate* unmanaged[MemberFunction]<void*, int>)ptr4[4])(ptr3);
		GC.KeepAlive(this);
		return result;
	}

	[GeneratedCode("Microsoft.Interop.ComInterfaceGenerator", "8.0.14.32403")]
	[SkipLocalsInit]
	unsafe int IComponent.GetControllerClassId(nint classId)
	{
		var (ptr3, ptr4) = ((IUnmanagedVirtualMethodTableProvider)this).GetVirtualMethodTableInfoForKey(typeof(IComponent));
		int result = ((delegate* unmanaged[MemberFunction]<void*, nint, int>)ptr4[5])(ptr3, classId);
		GC.KeepAlive(this);
		return result;
	}

	[GeneratedCode("Microsoft.Interop.ComInterfaceGenerator", "8.0.14.32403")]
	[SkipLocalsInit]
	unsafe int IComponent.SetIoMode(int mode)
	{
		var (ptr3, ptr4) = ((IUnmanagedVirtualMethodTableProvider)this).GetVirtualMethodTableInfoForKey(typeof(IComponent));
		int result = ((delegate* unmanaged[MemberFunction]<void*, int, int>)ptr4[6])(ptr3, mode);
		GC.KeepAlive(this);
		return result;
	}

	[GeneratedCode("Microsoft.Interop.ComInterfaceGenerator", "8.0.14.32403")]
	[SkipLocalsInit]
	unsafe int IComponent.GetBusCount(int mediaType, int direction)
	{
		var (ptr3, ptr4) = ((IUnmanagedVirtualMethodTableProvider)this).GetVirtualMethodTableInfoForKey(typeof(IComponent));
		int result = ((delegate* unmanaged[MemberFunction]<void*, int, int, int>)ptr4[7])(ptr3, mediaType, direction);
		GC.KeepAlive(this);
		return result;
	}

	[GeneratedCode("Microsoft.Interop.ComInterfaceGenerator", "8.0.14.32403")]
	[SkipLocalsInit]
	unsafe int IComponent.GetBusInfo(int mediaType, int direction, int index, nint bus)
	{
		var (ptr3, ptr4) = ((IUnmanagedVirtualMethodTableProvider)this).GetVirtualMethodTableInfoForKey(typeof(IComponent));
		int result = ((delegate* unmanaged[MemberFunction]<void*, int, int, int, nint, int>)ptr4[8])(ptr3, mediaType, direction, index, bus);
		GC.KeepAlive(this);
		return result;
	}

	[GeneratedCode("Microsoft.Interop.ComInterfaceGenerator", "8.0.14.32403")]
	[SkipLocalsInit]
	unsafe int IComponent.GetRoutingInfo(nint inInfo, nint outInfo)
	{
		var (ptr3, ptr4) = ((IUnmanagedVirtualMethodTableProvider)this).GetVirtualMethodTableInfoForKey(typeof(IComponent));
		int result = ((delegate* unmanaged[MemberFunction]<void*, nint, nint, int>)ptr4[9])(ptr3, inInfo, outInfo);
		GC.KeepAlive(this);
		return result;
	}

	[GeneratedCode("Microsoft.Interop.ComInterfaceGenerator", "8.0.14.32403")]
	[SkipLocalsInit]
	unsafe int IComponent.ActivateBus(int mediaType, int direction, int index, byte state)
	{
		var (ptr3, ptr4) = ((IUnmanagedVirtualMethodTableProvider)this).GetVirtualMethodTableInfoForKey(typeof(IComponent));
		int result = ((delegate* unmanaged[MemberFunction]<void*, int, int, int, byte, int>)ptr4[10])(ptr3, mediaType, direction, index, state);
		GC.KeepAlive(this);
		return result;
	}

	[GeneratedCode("Microsoft.Interop.ComInterfaceGenerator", "8.0.14.32403")]
	[SkipLocalsInit]
	unsafe int IComponent.SetActive(byte state)
	{
		var (ptr3, ptr4) = ((IUnmanagedVirtualMethodTableProvider)this).GetVirtualMethodTableInfoForKey(typeof(IComponent));
		int result = ((delegate* unmanaged[MemberFunction]<void*, byte, int>)ptr4[11])(ptr3, state);
		GC.KeepAlive(this);
		return result;
	}

	[GeneratedCode("Microsoft.Interop.ComInterfaceGenerator", "8.0.14.32403")]
	[SkipLocalsInit]
	unsafe int IComponent.SetState(nint stream)
	{
		var (ptr3, ptr4) = ((IUnmanagedVirtualMethodTableProvider)this).GetVirtualMethodTableInfoForKey(typeof(IComponent));
		int result = ((delegate* unmanaged[MemberFunction]<void*, nint, int>)ptr4[12])(ptr3, stream);
		GC.KeepAlive(this);
		return result;
	}

	[GeneratedCode("Microsoft.Interop.ComInterfaceGenerator", "8.0.14.32403")]
	[SkipLocalsInit]
	unsafe int IComponent.GetState(nint stream)
	{
		var (ptr3, ptr4) = ((IUnmanagedVirtualMethodTableProvider)this).GetVirtualMethodTableInfoForKey(typeof(IComponent));
		int result = ((delegate* unmanaged[MemberFunction]<void*, nint, int>)ptr4[13])(ptr3, stream);
		GC.KeepAlive(this);
		return result;
	}

	[UnmanagedCallersOnly(CallConvs = new Type[] { typeof(CallConvMemberFunction) })]
	internal unsafe static int ABI_Initialize(ComWrappers.ComInterfaceDispatch* __this_native, nint context)
	{
		int num = 0;
		try
		{
			return ComWrappers.ComInterfaceDispatch.GetInstance<IComponent>(__this_native).Initialize(context);
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
			return ComWrappers.ComInterfaceDispatch.GetInstance<IComponent>(__this_native).Terminate();
		}
		catch (Exception e)
		{
			return ExceptionAsHResultMarshaller<int>.ConvertToUnmanaged(e);
		}
	}

	[UnmanagedCallersOnly(CallConvs = new Type[] { typeof(CallConvMemberFunction) })]
	internal unsafe static int ABI_GetControllerClassId(ComWrappers.ComInterfaceDispatch* __this_native, nint classId)
	{
		int num = 0;
		try
		{
			return ComWrappers.ComInterfaceDispatch.GetInstance<IComponent>(__this_native).GetControllerClassId(classId);
		}
		catch (Exception e)
		{
			return ExceptionAsHResultMarshaller<int>.ConvertToUnmanaged(e);
		}
	}

	[UnmanagedCallersOnly(CallConvs = new Type[] { typeof(CallConvMemberFunction) })]
	internal unsafe static int ABI_SetIoMode(ComWrappers.ComInterfaceDispatch* __this_native, int mode)
	{
		int num = 0;
		try
		{
			return ComWrappers.ComInterfaceDispatch.GetInstance<IComponent>(__this_native).SetIoMode(mode);
		}
		catch (Exception e)
		{
			return ExceptionAsHResultMarshaller<int>.ConvertToUnmanaged(e);
		}
	}

	[UnmanagedCallersOnly(CallConvs = new Type[] { typeof(CallConvMemberFunction) })]
	internal unsafe static int ABI_GetBusCount(ComWrappers.ComInterfaceDispatch* __this_native, int mediaType, int direction)
	{
		int num = 0;
		try
		{
			return ComWrappers.ComInterfaceDispatch.GetInstance<IComponent>(__this_native).GetBusCount(mediaType, direction);
		}
		catch (Exception e)
		{
			return ExceptionAsHResultMarshaller<int>.ConvertToUnmanaged(e);
		}
	}

	[UnmanagedCallersOnly(CallConvs = new Type[] { typeof(CallConvMemberFunction) })]
	internal unsafe static int ABI_GetBusInfo(ComWrappers.ComInterfaceDispatch* __this_native, int mediaType, int direction, int index, nint bus)
	{
		int num = 0;
		try
		{
			return ComWrappers.ComInterfaceDispatch.GetInstance<IComponent>(__this_native).GetBusInfo(mediaType, direction, index, bus);
		}
		catch (Exception e)
		{
			return ExceptionAsHResultMarshaller<int>.ConvertToUnmanaged(e);
		}
	}

	[UnmanagedCallersOnly(CallConvs = new Type[] { typeof(CallConvMemberFunction) })]
	internal unsafe static int ABI_GetRoutingInfo(ComWrappers.ComInterfaceDispatch* __this_native, nint inInfo, nint outInfo)
	{
		int num = 0;
		try
		{
			return ComWrappers.ComInterfaceDispatch.GetInstance<IComponent>(__this_native).GetRoutingInfo(inInfo, outInfo);
		}
		catch (Exception e)
		{
			return ExceptionAsHResultMarshaller<int>.ConvertToUnmanaged(e);
		}
	}

	[UnmanagedCallersOnly(CallConvs = new Type[] { typeof(CallConvMemberFunction) })]
	internal unsafe static int ABI_ActivateBus(ComWrappers.ComInterfaceDispatch* __this_native, int mediaType, int direction, int index, byte state)
	{
		int num = 0;
		try
		{
			return ComWrappers.ComInterfaceDispatch.GetInstance<IComponent>(__this_native).ActivateBus(mediaType, direction, index, state);
		}
		catch (Exception e)
		{
			return ExceptionAsHResultMarshaller<int>.ConvertToUnmanaged(e);
		}
	}

	[UnmanagedCallersOnly(CallConvs = new Type[] { typeof(CallConvMemberFunction) })]
	internal unsafe static int ABI_SetActive(ComWrappers.ComInterfaceDispatch* __this_native, byte state)
	{
		int num = 0;
		try
		{
			return ComWrappers.ComInterfaceDispatch.GetInstance<IComponent>(__this_native).SetActive(state);
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
			return ComWrappers.ComInterfaceDispatch.GetInstance<IComponent>(__this_native).SetState(stream);
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
			return ComWrappers.ComInterfaceDispatch.GetInstance<IComponent>(__this_native).GetState(stream);
		}
		catch (Exception e)
		{
			return ExceptionAsHResultMarshaller<int>.ConvertToUnmanaged(e);
		}
	}

	internal unsafe static void** CreateManagedVirtualFunctionTable()
	{
		void** ptr = (void**)RuntimeHelpers.AllocateTypeAssociatedMemory(typeof(IComponent), sizeof(void*) * 14);
		ComWrappers.GetIUnknownImpl(out var fpQueryInterface, out var fpAddRef, out var fpRelease);
		*ptr = (void*)fpQueryInterface;
		ptr[1] = (void*)fpAddRef;
		ptr[2] = (void*)fpRelease;
		ptr[3] = (delegate* unmanaged[MemberFunction]<ComWrappers.ComInterfaceDispatch*, nint, int>)(&ABI_Initialize);
		ptr[4] = (delegate* unmanaged[MemberFunction]<ComWrappers.ComInterfaceDispatch*, int>)(&ABI_Terminate);
		ptr[5] = (delegate* unmanaged[MemberFunction]<ComWrappers.ComInterfaceDispatch*, nint, int>)(&ABI_GetControllerClassId);
		ptr[6] = (delegate* unmanaged[MemberFunction]<ComWrappers.ComInterfaceDispatch*, int, int>)(&ABI_SetIoMode);
		ptr[7] = (delegate* unmanaged[MemberFunction]<ComWrappers.ComInterfaceDispatch*, int, int, int>)(&ABI_GetBusCount);
		ptr[8] = (delegate* unmanaged[MemberFunction]<ComWrappers.ComInterfaceDispatch*, int, int, int, nint, int>)(&ABI_GetBusInfo);
		ptr[9] = (delegate* unmanaged[MemberFunction]<ComWrappers.ComInterfaceDispatch*, nint, nint, int>)(&ABI_GetRoutingInfo);
		ptr[10] = (delegate* unmanaged[MemberFunction]<ComWrappers.ComInterfaceDispatch*, int, int, int, byte, int>)(&ABI_ActivateBus);
		ptr[11] = (delegate* unmanaged[MemberFunction]<ComWrappers.ComInterfaceDispatch*, byte, int>)(&ABI_SetActive);
		ptr[12] = (delegate* unmanaged[MemberFunction]<ComWrappers.ComInterfaceDispatch*, nint, int>)(&ABI_SetState);
		ptr[13] = (delegate* unmanaged[MemberFunction]<ComWrappers.ComInterfaceDispatch*, nint, int>)(&ABI_GetState);
		return ptr;
	}
}
