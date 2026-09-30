using System.Runtime.InteropServices;

namespace SonicWave.Audio.Vst3;

internal static class NativeMethods
{
	[UnmanagedFunctionPointer(CallingConvention.StdCall)]
	public delegate nint GetPluginFactoryProc();

	[UnmanagedFunctionPointer(CallingConvention.StdCall)]
	public delegate int GetParamCountFn(nint self);

	[UnmanagedFunctionPointer(CallingConvention.StdCall)]
	public delegate nint GetParamDataFn(nint self, int index);

	[UnmanagedFunctionPointer(CallingConvention.StdCall)]
	public delegate uint GetParamIdFn(nint self);

	[UnmanagedFunctionPointer(CallingConvention.StdCall)]
	public delegate int GetPointFn(nint self, int index, out int sampleOffset, out double value);

	[DllImport("kernel32", CharSet = CharSet.Unicode, SetLastError = true)]
	public static extern nint LoadLibraryExW(string lpFileName, nint hFile, uint dwFlags);

	[DllImport("kernel32", CharSet = CharSet.Ansi, SetLastError = true)]
	public static extern nint GetProcAddress(nint hModule, string lpProcName);
}
