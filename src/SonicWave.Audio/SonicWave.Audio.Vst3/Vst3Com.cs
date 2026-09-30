using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace SonicWave.Audio.Vst3;

internal static class Vst3Com
{
	public static readonly StrategyBasedComWrappers Wrappers = new StrategyBasedComWrappers();

	public static nint ToNative<T>(T obj) where T : class
	{
		return Marshal.GetComInterfaceForObject(obj, typeof(T));
	}

	public static T ToManaged<T>(nint ptr) where T : class
	{
		return (T)Wrappers.GetOrCreateObjectForComInstance(ptr, CreateObjectFlags.None);
	}

	public static byte[] GetGuidBytes(Guid g)
	{
		return g.ToByteArray();
	}
}
