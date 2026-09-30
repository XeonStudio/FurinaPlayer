using System;
using System.CodeDom.Compiler;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using SonicWave.Audio.Vst3;

[DynamicInterfaceCastableImplementation]
internal interface _003CSonicWave_Audio_Vst3_IBStream_003EF4CDFA06C80BEE8195CFAE80BB1E1A21E5BB0712728CB91F895AA6955C048F1FE__InterfaceImplementation : IBStream
{
	[GeneratedCode("Microsoft.Interop.ComInterfaceGenerator", "8.0.14.32403")]
	[SkipLocalsInit]
	unsafe int IBStream.Read(nint buffer, int numBytes, nint numBytesRead)
	{
		var (ptr3, ptr4) = ((IUnmanagedVirtualMethodTableProvider)this).GetVirtualMethodTableInfoForKey(typeof(IBStream));
		int result = ((delegate* unmanaged[MemberFunction]<void*, nint, int, nint, int>)ptr4[3])(ptr3, buffer, numBytes, numBytesRead);
		GC.KeepAlive(this);
		return result;
	}

	[GeneratedCode("Microsoft.Interop.ComInterfaceGenerator", "8.0.14.32403")]
	[SkipLocalsInit]
	unsafe int IBStream.Write(nint buffer, int numBytes, nint numBytesWritten)
	{
		var (ptr3, ptr4) = ((IUnmanagedVirtualMethodTableProvider)this).GetVirtualMethodTableInfoForKey(typeof(IBStream));
		int result = ((delegate* unmanaged[MemberFunction]<void*, nint, int, nint, int>)ptr4[4])(ptr3, buffer, numBytes, numBytesWritten);
		GC.KeepAlive(this);
		return result;
	}

	[GeneratedCode("Microsoft.Interop.ComInterfaceGenerator", "8.0.14.32403")]
	[SkipLocalsInit]
	unsafe int IBStream.Seek(long pos, int mode, nint result)
	{
		var (ptr3, ptr4) = ((IUnmanagedVirtualMethodTableProvider)this).GetVirtualMethodTableInfoForKey(typeof(IBStream));
		int result2 = ((delegate* unmanaged[MemberFunction]<void*, long, int, nint, int>)ptr4[5])(ptr3, pos, mode, result);
		GC.KeepAlive(this);
		return result2;
	}

	[GeneratedCode("Microsoft.Interop.ComInterfaceGenerator", "8.0.14.32403")]
	[SkipLocalsInit]
	unsafe int IBStream.Tell(nint pos)
	{
		var (ptr3, ptr4) = ((IUnmanagedVirtualMethodTableProvider)this).GetVirtualMethodTableInfoForKey(typeof(IBStream));
		int result = ((delegate* unmanaged[MemberFunction]<void*, nint, int>)ptr4[6])(ptr3, pos);
		GC.KeepAlive(this);
		return result;
	}

	[UnmanagedCallersOnly(CallConvs = new Type[] { typeof(CallConvMemberFunction) })]
	internal unsafe static int ABI_Read(ComWrappers.ComInterfaceDispatch* __this_native, nint buffer, int numBytes, nint numBytesRead)
	{
		int num = 0;
		try
		{
			return ComWrappers.ComInterfaceDispatch.GetInstance<IBStream>(__this_native).Read(buffer, numBytes, numBytesRead);
		}
		catch (Exception e)
		{
			return ExceptionAsHResultMarshaller<int>.ConvertToUnmanaged(e);
		}
	}

	[UnmanagedCallersOnly(CallConvs = new Type[] { typeof(CallConvMemberFunction) })]
	internal unsafe static int ABI_Write(ComWrappers.ComInterfaceDispatch* __this_native, nint buffer, int numBytes, nint numBytesWritten)
	{
		int num = 0;
		try
		{
			return ComWrappers.ComInterfaceDispatch.GetInstance<IBStream>(__this_native).Write(buffer, numBytes, numBytesWritten);
		}
		catch (Exception e)
		{
			return ExceptionAsHResultMarshaller<int>.ConvertToUnmanaged(e);
		}
	}

	[UnmanagedCallersOnly(CallConvs = new Type[] { typeof(CallConvMemberFunction) })]
	internal unsafe static int ABI_Seek(ComWrappers.ComInterfaceDispatch* __this_native, long pos, int mode, nint result)
	{
		int num = 0;
		try
		{
			return ComWrappers.ComInterfaceDispatch.GetInstance<IBStream>(__this_native).Seek(pos, mode, result);
		}
		catch (Exception e)
		{
			return ExceptionAsHResultMarshaller<int>.ConvertToUnmanaged(e);
		}
	}

	[UnmanagedCallersOnly(CallConvs = new Type[] { typeof(CallConvMemberFunction) })]
	internal unsafe static int ABI_Tell(ComWrappers.ComInterfaceDispatch* __this_native, nint pos)
	{
		int num = 0;
		try
		{
			return ComWrappers.ComInterfaceDispatch.GetInstance<IBStream>(__this_native).Tell(pos);
		}
		catch (Exception e)
		{
			return ExceptionAsHResultMarshaller<int>.ConvertToUnmanaged(e);
		}
	}

	internal unsafe static void** CreateManagedVirtualFunctionTable()
	{
		void** ptr = (void**)RuntimeHelpers.AllocateTypeAssociatedMemory(typeof(IBStream), sizeof(void*) * 7);
		ComWrappers.GetIUnknownImpl(out var fpQueryInterface, out var fpAddRef, out var fpRelease);
		*ptr = (void*)fpQueryInterface;
		ptr[1] = (void*)fpAddRef;
		ptr[2] = (void*)fpRelease;
		ptr[3] = (delegate* unmanaged[MemberFunction]<ComWrappers.ComInterfaceDispatch*, nint, int, nint, int>)(&ABI_Read);
		ptr[4] = (delegate* unmanaged[MemberFunction]<ComWrappers.ComInterfaceDispatch*, nint, int, nint, int>)(&ABI_Write);
		ptr[5] = (delegate* unmanaged[MemberFunction]<ComWrappers.ComInterfaceDispatch*, long, int, nint, int>)(&ABI_Seek);
		ptr[6] = (delegate* unmanaged[MemberFunction]<ComWrappers.ComInterfaceDispatch*, nint, int>)(&ABI_Tell);
		return ptr;
	}
}
