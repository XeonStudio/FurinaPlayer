using System;
using System.CodeDom.Compiler;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using SonicWave.Audio.Vst3;

[DynamicInterfaceCastableImplementation]
internal interface _003CSonicWave_Audio_Vst3_IMessage_003EF28CBC06B3883EAC974697B9C3CADD0F6782C4179E4B48042E970966BA9AFD570__InterfaceImplementation : IMessage
{
	[GeneratedCode("Microsoft.Interop.ComInterfaceGenerator", "8.0.14.32403")]
	[SkipLocalsInit]
	unsafe nint IMessage.GetMessageID()
	{
		var (ptr3, ptr4) = ((IUnmanagedVirtualMethodTableProvider)this).GetVirtualMethodTableInfoForKey(typeof(IMessage));
		nint result = ((delegate* unmanaged[MemberFunction]<void*, nint>)ptr4[3])(ptr3);
		GC.KeepAlive(this);
		return result;
	}

	[GeneratedCode("Microsoft.Interop.ComInterfaceGenerator", "8.0.14.32403")]
	[SkipLocalsInit]
	unsafe void IMessage.SetMessageID(nint id)
	{
		var (ptr3, ptr4) = ((IUnmanagedVirtualMethodTableProvider)this).GetVirtualMethodTableInfoForKey(typeof(IMessage));
		((delegate* unmanaged[MemberFunction]<void*, nint, void>)ptr4[4])(ptr3, id);
		GC.KeepAlive(this);
	}

	[GeneratedCode("Microsoft.Interop.ComInterfaceGenerator", "8.0.14.32403")]
	[SkipLocalsInit]
	unsafe nint IMessage.GetAttributes()
	{
		var (ptr3, ptr4) = ((IUnmanagedVirtualMethodTableProvider)this).GetVirtualMethodTableInfoForKey(typeof(IMessage));
		nint result = ((delegate* unmanaged[MemberFunction]<void*, nint>)ptr4[5])(ptr3);
		GC.KeepAlive(this);
		return result;
	}

	[UnmanagedCallersOnly(CallConvs = new Type[] { typeof(CallConvMemberFunction) })]
	internal unsafe static nint ABI_GetMessageID(ComWrappers.ComInterfaceDispatch* __this_native)
	{
		nint num = 0;
		try
		{
			return ComWrappers.ComInterfaceDispatch.GetInstance<IMessage>(__this_native).GetMessageID();
		}
		catch (Exception e)
		{
			return ExceptionAsDefaultMarshaller<nint>.ConvertToUnmanaged(e);
		}
	}

	[UnmanagedCallersOnly(CallConvs = new Type[] { typeof(CallConvMemberFunction) })]
	internal unsafe static void ABI_SetMessageID(ComWrappers.ComInterfaceDispatch* __this_native, nint id)
	{
		try
		{
			ComWrappers.ComInterfaceDispatch.GetInstance<IMessage>(__this_native).SetMessageID(id);
		}
		catch (Exception e)
		{
			ExceptionAsVoidMarshaller.ConvertToUnmanaged(e);
		}
	}

	[UnmanagedCallersOnly(CallConvs = new Type[] { typeof(CallConvMemberFunction) })]
	internal unsafe static nint ABI_GetAttributes(ComWrappers.ComInterfaceDispatch* __this_native)
	{
		nint num = 0;
		try
		{
			return ComWrappers.ComInterfaceDispatch.GetInstance<IMessage>(__this_native).GetAttributes();
		}
		catch (Exception e)
		{
			return ExceptionAsDefaultMarshaller<nint>.ConvertToUnmanaged(e);
		}
	}

	internal unsafe static void** CreateManagedVirtualFunctionTable()
	{
		void** ptr = (void**)RuntimeHelpers.AllocateTypeAssociatedMemory(typeof(IMessage), sizeof(void*) * 6);
		ComWrappers.GetIUnknownImpl(out var fpQueryInterface, out var fpAddRef, out var fpRelease);
		*ptr = (void*)fpQueryInterface;
		ptr[1] = (void*)fpAddRef;
		ptr[2] = (void*)fpRelease;
		ptr[3] = (delegate* unmanaged[MemberFunction]<ComWrappers.ComInterfaceDispatch*, nint>)(&ABI_GetMessageID);
		ptr[4] = (delegate* unmanaged[MemberFunction]<ComWrappers.ComInterfaceDispatch*, nint, void>)(&ABI_SetMessageID);
		ptr[5] = (delegate* unmanaged[MemberFunction]<ComWrappers.ComInterfaceDispatch*, nint>)(&ABI_GetAttributes);
		return ptr;
	}
}
