using System;
using System.Runtime.InteropServices;

namespace SonicWave.App;

internal static class DwmGlass
{
	private const int DWMWA_SYSTEMBACKDROP_TYPE = 38;

	private const int DWMWA_WINDOW_CORNER_PREFERENCE = 33;

	private const int DWMSBT_TRANSIENTWINDOW = 3;

	private const int DWMWCP_ROUND = 2;

	[DllImport("dwmapi.dll")]
	private static extern int DwmSetWindowAttribute(nint hwnd, int attr, ref int attrValue, int attrSize);

	public static bool ApplyTransientBackdrop(nint hwnd)
	{
		try
		{
			if (hwnd == IntPtr.Zero)
			{
				return false;
			}
			int attrValue = 3;
			if (DwmSetWindowAttribute(hwnd, 38, ref attrValue, 4) != 0)
			{
				return false;
			}
			int attrValue2 = 2;
			DwmSetWindowAttribute(hwnd, 33, ref attrValue2, 4);
			return true;
		}
		catch
		{
			return false;
		}
	}

	public static void ClearBackdrop(nint hwnd)
	{
		try
		{
			if (hwnd != IntPtr.Zero)
			{
				int attrValue = 0;
				DwmSetWindowAttribute(hwnd, 38, ref attrValue, 4);
			}
		}
		catch
		{
		}
	}
}
