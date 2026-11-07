using System;
using System.Runtime.InteropServices;

namespace SonicWave.App;

internal static class DwmGlass
{
	private const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;

	private const int DWMWA_WINDOW_CORNER_PREFERENCE = 33;

	private const int DWMWA_BORDER_COLOR = 34;

	private const int DWMWA_SYSTEMBACKDROP_TYPE = 38;

	private const int DWMSBT_MAINWINDOW = 2;

	private const int DWMSBT_TRANSIENTWINDOW = 3;

	private const int DWMSBT_TABBEDWINDOW = 4;

	private const int DWMWCP_ROUND = 2;

	private const uint DWMWA_COLOR_NONE = 0xFFFFFFFE;

	[DllImport("dwmapi.dll")]
	private static extern int DwmSetWindowAttribute(nint hwnd, int attr, ref int attrValue, int attrSize);

	[DllImport("dwmapi.dll")]
	private static extern int DwmSetWindowAttribute(nint hwnd, int attr, ref uint attrValue, int attrSize);

	public static void RemoveBorderColor(nint hwnd)
	{
		try
		{
			if (hwnd != IntPtr.Zero)
			{
				uint color = DWMWA_COLOR_NONE;
				DwmSetWindowAttribute(hwnd, DWMWA_BORDER_COLOR, ref color, sizeof(uint));
				int cornerPref = DWMWCP_ROUND;
				DwmSetWindowAttribute(hwnd, DWMWA_WINDOW_CORNER_PREFERENCE, ref cornerPref, sizeof(int));
			}
		}
		catch
		{
		}
	}

	public static void SetImmersiveDarkMode(nint hwnd, bool isDark)
	{
		try
		{
			if (hwnd != IntPtr.Zero)
			{
				int dark = isDark ? 1 : 0;
				DwmSetWindowAttribute(hwnd, DWMWA_USE_IMMERSIVE_DARK_MODE, ref dark, sizeof(int));
			}
		}
		catch
		{
		}
	}

	public static bool ApplyTransientBackdrop(nint hwnd)
	{
		try
		{
			if (hwnd == IntPtr.Zero)
			{
				return false;
			}
			int attrValue = DWMSBT_TRANSIENTWINDOW;
			if (DwmSetWindowAttribute(hwnd, DWMWA_SYSTEMBACKDROP_TYPE, ref attrValue, sizeof(int)) != 0)
			{
				return false;
			}
			int attrValue2 = DWMWCP_ROUND;
			DwmSetWindowAttribute(hwnd, DWMWA_WINDOW_CORNER_PREFERENCE, ref attrValue2, sizeof(int));
			RemoveBorderColor(hwnd);
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
				DwmSetWindowAttribute(hwnd, DWMWA_SYSTEMBACKDROP_TYPE, ref attrValue, sizeof(int));
			}
		}
		catch
		{
		}
	}
}
