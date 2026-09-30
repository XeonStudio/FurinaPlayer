using System;
using System.CodeDom.Compiler;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using SonicWave.Audio.Vst3;

[DynamicInterfaceCastableImplementation]
internal interface _003CSonicWave_Audio_Vst3_IPlugView_003EFD5B3221BE9CE02AB445706943EB09C4AACE82105EE16C1F25A426B74EB2F87EF__InterfaceImplementation : IPlugView
{
	[GeneratedCode("Microsoft.Interop.ComInterfaceGenerator", "8.0.14.32403")]
	[SkipLocalsInit]
	unsafe int IPlugView.IsPlatformTypeSupported(nint type)
	{
		var (ptr3, ptr4) = ((IUnmanagedVirtualMethodTableProvider)this).GetVirtualMethodTableInfoForKey(typeof(IPlugView));
		int result = ((delegate* unmanaged[MemberFunction]<void*, nint, int>)ptr4[3])(ptr3, type);
		GC.KeepAlive(this);
		return result;
	}

	[GeneratedCode("Microsoft.Interop.ComInterfaceGenerator", "8.0.14.32403")]
	[SkipLocalsInit]
	unsafe int IPlugView.Attached(nint parent, nint type)
	{
		var (ptr3, ptr4) = ((IUnmanagedVirtualMethodTableProvider)this).GetVirtualMethodTableInfoForKey(typeof(IPlugView));
		int result = ((delegate* unmanaged[MemberFunction]<void*, nint, nint, int>)ptr4[4])(ptr3, parent, type);
		GC.KeepAlive(this);
		return result;
	}

	[GeneratedCode("Microsoft.Interop.ComInterfaceGenerator", "8.0.14.32403")]
	[SkipLocalsInit]
	unsafe int IPlugView.Removed()
	{
		var (ptr3, ptr4) = ((IUnmanagedVirtualMethodTableProvider)this).GetVirtualMethodTableInfoForKey(typeof(IPlugView));
		int result = ((delegate* unmanaged[MemberFunction]<void*, int>)ptr4[5])(ptr3);
		GC.KeepAlive(this);
		return result;
	}

	[GeneratedCode("Microsoft.Interop.ComInterfaceGenerator", "8.0.14.32403")]
	[SkipLocalsInit]
	unsafe int IPlugView.OnWheel(float distance)
	{
		var (ptr3, ptr4) = ((IUnmanagedVirtualMethodTableProvider)this).GetVirtualMethodTableInfoForKey(typeof(IPlugView));
		int result = ((delegate* unmanaged[MemberFunction]<void*, float, int>)ptr4[6])(ptr3, distance);
		GC.KeepAlive(this);
		return result;
	}

	[GeneratedCode("Microsoft.Interop.ComInterfaceGenerator", "8.0.14.32403")]
	[SkipLocalsInit]
	unsafe int IPlugView.OnKeyDown(ushort key, short keyCode, short modifiers)
	{
		var (ptr3, ptr4) = ((IUnmanagedVirtualMethodTableProvider)this).GetVirtualMethodTableInfoForKey(typeof(IPlugView));
		int result = ((delegate* unmanaged[MemberFunction]<void*, ushort, short, short, int>)ptr4[7])(ptr3, key, keyCode, modifiers);
		GC.KeepAlive(this);
		return result;
	}

	[GeneratedCode("Microsoft.Interop.ComInterfaceGenerator", "8.0.14.32403")]
	[SkipLocalsInit]
	unsafe int IPlugView.OnKeyUp(ushort key, short keyCode, short modifiers)
	{
		var (ptr3, ptr4) = ((IUnmanagedVirtualMethodTableProvider)this).GetVirtualMethodTableInfoForKey(typeof(IPlugView));
		int result = ((delegate* unmanaged[MemberFunction]<void*, ushort, short, short, int>)ptr4[8])(ptr3, key, keyCode, modifiers);
		GC.KeepAlive(this);
		return result;
	}

	[GeneratedCode("Microsoft.Interop.ComInterfaceGenerator", "8.0.14.32403")]
	[SkipLocalsInit]
	unsafe int IPlugView.GetSize(nint size)
	{
		var (ptr3, ptr4) = ((IUnmanagedVirtualMethodTableProvider)this).GetVirtualMethodTableInfoForKey(typeof(IPlugView));
		int result = ((delegate* unmanaged[MemberFunction]<void*, nint, int>)ptr4[9])(ptr3, size);
		GC.KeepAlive(this);
		return result;
	}

	[GeneratedCode("Microsoft.Interop.ComInterfaceGenerator", "8.0.14.32403")]
	[SkipLocalsInit]
	unsafe int IPlugView.OnSize(nint newSize)
	{
		var (ptr3, ptr4) = ((IUnmanagedVirtualMethodTableProvider)this).GetVirtualMethodTableInfoForKey(typeof(IPlugView));
		int result = ((delegate* unmanaged[MemberFunction]<void*, nint, int>)ptr4[10])(ptr3, newSize);
		GC.KeepAlive(this);
		return result;
	}

	[GeneratedCode("Microsoft.Interop.ComInterfaceGenerator", "8.0.14.32403")]
	[SkipLocalsInit]
	unsafe int IPlugView.OnFocus(byte state)
	{
		var (ptr3, ptr4) = ((IUnmanagedVirtualMethodTableProvider)this).GetVirtualMethodTableInfoForKey(typeof(IPlugView));
		int result = ((delegate* unmanaged[MemberFunction]<void*, byte, int>)ptr4[11])(ptr3, state);
		GC.KeepAlive(this);
		return result;
	}

	[GeneratedCode("Microsoft.Interop.ComInterfaceGenerator", "8.0.14.32403")]
	[SkipLocalsInit]
	unsafe int IPlugView.SetFrame(nint frame)
	{
		var (ptr3, ptr4) = ((IUnmanagedVirtualMethodTableProvider)this).GetVirtualMethodTableInfoForKey(typeof(IPlugView));
		int result = ((delegate* unmanaged[MemberFunction]<void*, nint, int>)ptr4[12])(ptr3, frame);
		GC.KeepAlive(this);
		return result;
	}

	[GeneratedCode("Microsoft.Interop.ComInterfaceGenerator", "8.0.14.32403")]
	[SkipLocalsInit]
	unsafe int IPlugView.CanResize()
	{
		var (ptr3, ptr4) = ((IUnmanagedVirtualMethodTableProvider)this).GetVirtualMethodTableInfoForKey(typeof(IPlugView));
		int result = ((delegate* unmanaged[MemberFunction]<void*, int>)ptr4[13])(ptr3);
		GC.KeepAlive(this);
		return result;
	}

	[GeneratedCode("Microsoft.Interop.ComInterfaceGenerator", "8.0.14.32403")]
	[SkipLocalsInit]
	unsafe int IPlugView.CheckSizeConstraint(nint rect)
	{
		var (ptr3, ptr4) = ((IUnmanagedVirtualMethodTableProvider)this).GetVirtualMethodTableInfoForKey(typeof(IPlugView));
		int result = ((delegate* unmanaged[MemberFunction]<void*, nint, int>)ptr4[14])(ptr3, rect);
		GC.KeepAlive(this);
		return result;
	}

	[UnmanagedCallersOnly(CallConvs = new Type[] { typeof(CallConvMemberFunction) })]
	internal unsafe static int ABI_IsPlatformTypeSupported(ComWrappers.ComInterfaceDispatch* __this_native, nint type)
	{
		int num = 0;
		try
		{
			return ComWrappers.ComInterfaceDispatch.GetInstance<IPlugView>(__this_native).IsPlatformTypeSupported(type);
		}
		catch (Exception e)
		{
			return ExceptionAsHResultMarshaller<int>.ConvertToUnmanaged(e);
		}
	}

	[UnmanagedCallersOnly(CallConvs = new Type[] { typeof(CallConvMemberFunction) })]
	internal unsafe static int ABI_Attached(ComWrappers.ComInterfaceDispatch* __this_native, nint parent, nint type)
	{
		int num = 0;
		try
		{
			return ComWrappers.ComInterfaceDispatch.GetInstance<IPlugView>(__this_native).Attached(parent, type);
		}
		catch (Exception e)
		{
			return ExceptionAsHResultMarshaller<int>.ConvertToUnmanaged(e);
		}
	}

	[UnmanagedCallersOnly(CallConvs = new Type[] { typeof(CallConvMemberFunction) })]
	internal unsafe static int ABI_Removed(ComWrappers.ComInterfaceDispatch* __this_native)
	{
		int num = 0;
		try
		{
			return ComWrappers.ComInterfaceDispatch.GetInstance<IPlugView>(__this_native).Removed();
		}
		catch (Exception e)
		{
			return ExceptionAsHResultMarshaller<int>.ConvertToUnmanaged(e);
		}
	}

	[UnmanagedCallersOnly(CallConvs = new Type[] { typeof(CallConvMemberFunction) })]
	internal unsafe static int ABI_OnWheel(ComWrappers.ComInterfaceDispatch* __this_native, float distance)
	{
		int num = 0;
		try
		{
			return ComWrappers.ComInterfaceDispatch.GetInstance<IPlugView>(__this_native).OnWheel(distance);
		}
		catch (Exception e)
		{
			return ExceptionAsHResultMarshaller<int>.ConvertToUnmanaged(e);
		}
	}

	[UnmanagedCallersOnly(CallConvs = new Type[] { typeof(CallConvMemberFunction) })]
	internal unsafe static int ABI_OnKeyDown(ComWrappers.ComInterfaceDispatch* __this_native, ushort key, short keyCode, short modifiers)
	{
		int num = 0;
		try
		{
			return ComWrappers.ComInterfaceDispatch.GetInstance<IPlugView>(__this_native).OnKeyDown(key, keyCode, modifiers);
		}
		catch (Exception e)
		{
			return ExceptionAsHResultMarshaller<int>.ConvertToUnmanaged(e);
		}
	}

	[UnmanagedCallersOnly(CallConvs = new Type[] { typeof(CallConvMemberFunction) })]
	internal unsafe static int ABI_OnKeyUp(ComWrappers.ComInterfaceDispatch* __this_native, ushort key, short keyCode, short modifiers)
	{
		int num = 0;
		try
		{
			return ComWrappers.ComInterfaceDispatch.GetInstance<IPlugView>(__this_native).OnKeyUp(key, keyCode, modifiers);
		}
		catch (Exception e)
		{
			return ExceptionAsHResultMarshaller<int>.ConvertToUnmanaged(e);
		}
	}

	[UnmanagedCallersOnly(CallConvs = new Type[] { typeof(CallConvMemberFunction) })]
	internal unsafe static int ABI_GetSize(ComWrappers.ComInterfaceDispatch* __this_native, nint size)
	{
		int num = 0;
		try
		{
			return ComWrappers.ComInterfaceDispatch.GetInstance<IPlugView>(__this_native).GetSize(size);
		}
		catch (Exception e)
		{
			return ExceptionAsHResultMarshaller<int>.ConvertToUnmanaged(e);
		}
	}

	[UnmanagedCallersOnly(CallConvs = new Type[] { typeof(CallConvMemberFunction) })]
	internal unsafe static int ABI_OnSize(ComWrappers.ComInterfaceDispatch* __this_native, nint newSize)
	{
		int num = 0;
		try
		{
			return ComWrappers.ComInterfaceDispatch.GetInstance<IPlugView>(__this_native).OnSize(newSize);
		}
		catch (Exception e)
		{
			return ExceptionAsHResultMarshaller<int>.ConvertToUnmanaged(e);
		}
	}

	[UnmanagedCallersOnly(CallConvs = new Type[] { typeof(CallConvMemberFunction) })]
	internal unsafe static int ABI_OnFocus(ComWrappers.ComInterfaceDispatch* __this_native, byte state)
	{
		int num = 0;
		try
		{
			return ComWrappers.ComInterfaceDispatch.GetInstance<IPlugView>(__this_native).OnFocus(state);
		}
		catch (Exception e)
		{
			return ExceptionAsHResultMarshaller<int>.ConvertToUnmanaged(e);
		}
	}

	[UnmanagedCallersOnly(CallConvs = new Type[] { typeof(CallConvMemberFunction) })]
	internal unsafe static int ABI_SetFrame(ComWrappers.ComInterfaceDispatch* __this_native, nint frame)
	{
		int num = 0;
		try
		{
			return ComWrappers.ComInterfaceDispatch.GetInstance<IPlugView>(__this_native).SetFrame(frame);
		}
		catch (Exception e)
		{
			return ExceptionAsHResultMarshaller<int>.ConvertToUnmanaged(e);
		}
	}

	[UnmanagedCallersOnly(CallConvs = new Type[] { typeof(CallConvMemberFunction) })]
	internal unsafe static int ABI_CanResize(ComWrappers.ComInterfaceDispatch* __this_native)
	{
		int num = 0;
		try
		{
			return ComWrappers.ComInterfaceDispatch.GetInstance<IPlugView>(__this_native).CanResize();
		}
		catch (Exception e)
		{
			return ExceptionAsHResultMarshaller<int>.ConvertToUnmanaged(e);
		}
	}

	[UnmanagedCallersOnly(CallConvs = new Type[] { typeof(CallConvMemberFunction) })]
	internal unsafe static int ABI_CheckSizeConstraint(ComWrappers.ComInterfaceDispatch* __this_native, nint rect)
	{
		int num = 0;
		try
		{
			return ComWrappers.ComInterfaceDispatch.GetInstance<IPlugView>(__this_native).CheckSizeConstraint(rect);
		}
		catch (Exception e)
		{
			return ExceptionAsHResultMarshaller<int>.ConvertToUnmanaged(e);
		}
	}

	internal unsafe static void** CreateManagedVirtualFunctionTable()
	{
		void** ptr = (void**)RuntimeHelpers.AllocateTypeAssociatedMemory(typeof(IPlugView), sizeof(void*) * 15);
		ComWrappers.GetIUnknownImpl(out var fpQueryInterface, out var fpAddRef, out var fpRelease);
		*ptr = (void*)fpQueryInterface;
		ptr[1] = (void*)fpAddRef;
		ptr[2] = (void*)fpRelease;
		ptr[3] = (delegate* unmanaged[MemberFunction]<ComWrappers.ComInterfaceDispatch*, nint, int>)(&ABI_IsPlatformTypeSupported);
		ptr[4] = (delegate* unmanaged[MemberFunction]<ComWrappers.ComInterfaceDispatch*, nint, nint, int>)(&ABI_Attached);
		ptr[5] = (delegate* unmanaged[MemberFunction]<ComWrappers.ComInterfaceDispatch*, int>)(&ABI_Removed);
		ptr[6] = (delegate* unmanaged[MemberFunction]<ComWrappers.ComInterfaceDispatch*, float, int>)(&ABI_OnWheel);
		ptr[7] = (delegate* unmanaged[MemberFunction]<ComWrappers.ComInterfaceDispatch*, ushort, short, short, int>)(&ABI_OnKeyDown);
		ptr[8] = (delegate* unmanaged[MemberFunction]<ComWrappers.ComInterfaceDispatch*, ushort, short, short, int>)(&ABI_OnKeyUp);
		ptr[9] = (delegate* unmanaged[MemberFunction]<ComWrappers.ComInterfaceDispatch*, nint, int>)(&ABI_GetSize);
		ptr[10] = (delegate* unmanaged[MemberFunction]<ComWrappers.ComInterfaceDispatch*, nint, int>)(&ABI_OnSize);
		ptr[11] = (delegate* unmanaged[MemberFunction]<ComWrappers.ComInterfaceDispatch*, byte, int>)(&ABI_OnFocus);
		ptr[12] = (delegate* unmanaged[MemberFunction]<ComWrappers.ComInterfaceDispatch*, nint, int>)(&ABI_SetFrame);
		ptr[13] = (delegate* unmanaged[MemberFunction]<ComWrappers.ComInterfaceDispatch*, int>)(&ABI_CanResize);
		ptr[14] = (delegate* unmanaged[MemberFunction]<ComWrappers.ComInterfaceDispatch*, nint, int>)(&ABI_CheckSizeConstraint);
		return ptr;
	}
}
