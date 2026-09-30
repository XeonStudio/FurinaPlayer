using System;
using System.Runtime.InteropServices;
using Microsoft.UI.Dispatching;
using SonicWave.Audio.Vst3;
using WinRT.Interop;

namespace FurinaPlayer.UI.Helpers;

public sealed class Vst3EditorWindow : IDisposable
{
	private delegate nint WndProc(nint hWnd, uint msg, nint wParam, nint lParam);

	[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
	private struct WndClassEx
	{
		public uint cbSize;

		public uint style;

		public nint lpfnWndProc;

		public int cbClsExtra;

		public int cbWndExtra;

		public nint hInstance;

		public nint hIcon;

		public nint hCursor;

		public nint hbrBackground;

		public string lpszMenuName;

		public string lpszClassName;

		public nint hIconSm;
	}

	private struct RECT
	{
		public int Left;

		public int Top;

		public int Right;

		public int Bottom;
	}

	private const uint WM_CLOSE = 16u;

	private const uint WS_OVERLAPPEDWINDOW = 13565952u;

	private const int SW_SHOW = 5;

	private const uint SWP_NOZORDER = 4u;

	private static readonly WndProc s_wndProc = WndProcImpl;

	private static bool s_classRegistered;

	private static Vst3EditorWindow? s_active;

	private readonly string _pluginPath;

	private nint _hwnd;

	private bool _closed;

	private DispatcherQueue? _uiDispatcher;

	public string PluginPath => _pluginPath;

	public nint Handle => _hwnd;

	public event Action? Closed;

	[DllImport("user32", CharSet = CharSet.Unicode)]
	private static extern ushort RegisterClassExW(ref WndClassEx wc);

	[DllImport("user32", CharSet = CharSet.Unicode)]
	private static extern nint CreateWindowExW(uint exStyle, string cls, string name, uint style, int x, int y, int w, int h, nint parent, nint menu, nint inst, nint param);

	[DllImport("user32")]
	private static extern bool ShowWindow(nint hWnd, int cmd);

	[DllImport("user32")]
	private static extern bool UpdateWindow(nint hWnd);

	[DllImport("user32")]
	private static extern bool DestroyWindow(nint hWnd);

	[DllImport("user32")]
	private static extern bool GetWindowRect(nint hWnd, out RECT rect);

	[DllImport("user32")]
	private static extern bool SetWindowPos(nint hWnd, nint after, int x, int y, int w, int h, uint flags);

	[DllImport("user32")]
	private static extern bool SetForegroundWindow(nint hWnd);

	[DllImport("user32")]
	private static extern nint DefWindowProcW(nint hWnd, uint msg, nint wParam, nint lParam);

	[DllImport("kernel32", CharSet = CharSet.Unicode)]
	private static extern nint GetModuleHandleW(string? name);

	private Vst3EditorWindow(string pluginPath, nint hwnd)
	{
		_pluginPath = pluginPath;
		_hwnd = hwnd;
		try
		{
			_uiDispatcher = DispatcherQueue.GetForCurrentThread();
		}
		catch
		{
		}
	}

	public static Vst3EditorWindow? Open(string pluginPath, int sampleRate, int channels, out string error)
	{
		error = "";
		try
		{
			Vst3EditorWindow vst3EditorWindow = s_active;
			if (vst3EditorWindow != null && !vst3EditorWindow._closed)
			{
				if (string.Equals(vst3EditorWindow.PluginPath, pluginPath, StringComparison.OrdinalIgnoreCase))
				{
					vst3EditorWindow.BringToFront();
					error = "";
					return vst3EditorWindow;
				}
				vst3EditorWindow.CloseInternal();
			}
			EnsureClassRegistered();
			if (!Vst3NativeHost.Instance.EnsureLoaded(pluginPath, sampleRate, channels, out error))
			{
				if (error.Length == 0)
				{
					error = "插件加载失败（未知错误）";
				}
				return null;
			}
			nint num = CreateWindowExW(0u, "FurinaVst3EditorWnd", "VST3 插件界面 - Furina Player", 13565952u, 100, 100, 480, 360, IntPtr.Zero, IntPtr.Zero, GetModuleHandleW(null), IntPtr.Zero);
			if (num == IntPtr.Zero)
			{
				error = "创建宿主窗口失败";
				return null;
			}
			if (!Vst3NativeHost.Instance.OpenEditor(num, out var width, out var height, out error))
			{
				DestroyWindow(num);
				return null;
			}
			int num2 = width + 16;
			int num3 = height + 48;
			int x = 100;
			int y = 100;
			try
			{
				if (GetWindowRect(WindowNative.GetWindowHandle(WindowManager.CurrentWindow), out var rect))
				{
					int num4 = rect.Right - rect.Left;
					int num5 = rect.Bottom - rect.Top;
					x = rect.Left + Math.Max(0, (num4 - num2) / 2);
					y = rect.Top + Math.Max(0, (num5 - num3) / 2);
				}
			}
			catch
			{
			}
			SetWindowPos(num, IntPtr.Zero, x, y, num2, num3, 0u);
			ShowWindow(num, 5);
			UpdateWindow(num);
			Vst3EditorWindow vst3EditorWindow2 = (s_active = new Vst3EditorWindow(pluginPath, num));
			Vst3NativeHost.Instance.EditorResizeRequested += vst3EditorWindow2.OnEditorResizeRequested;
			return vst3EditorWindow2;
		}
		catch (Exception ex)
		{
			error = "打开插件界面异常：" + ex.Message;
			return null;
		}
	}

	public void BringToFront()
	{
		if (_hwnd != IntPtr.Zero)
		{
			SetForegroundWindow(_hwnd);
		}
	}

	public void Close()
	{
		CloseInternal();
	}

	public void Dispose()
	{
		CloseInternal();
	}

	private void OnEditorResizeRequested(int width, int height)
	{
		Action resize = () =>
		{
			if (_hwnd != IntPtr.Zero && !_closed)
			{
				if (GetWindowRect(_hwnd, out var rect))
				{
					SetWindowPos(_hwnd, IntPtr.Zero, rect.Left, rect.Top, width + 16, height + 48, 4u);
				}
				Vst3NativeHost.Instance.EditorResized(width, height);
			}
		};
		try
		{
			if (_uiDispatcher != null && !_uiDispatcher.HasThreadAccess)
			{
				_uiDispatcher.TryEnqueue(() =>
				{
					resize();
				});
			}
			else
			{
				resize();
			}
		}
		catch
		{
		}
	}

	private void CloseInternal()
	{
		if (_closed)
		{
			return;
		}
		_closed = true;
		Vst3NativeHost.Instance.EditorResizeRequested -= OnEditorResizeRequested;
		try
		{
			Vst3NativeHost.Instance.CloseEditor();
		}
		catch
		{
		}
		if (_hwnd != IntPtr.Zero)
		{
			DestroyWindow(_hwnd);
			_hwnd = IntPtr.Zero;
		}
		if (s_active == this)
		{
			s_active = null;
		}
		try
		{
			Closed?.Invoke();
		}
		catch
		{
		}
	}

	private static nint WndProcImpl(nint hWnd, uint msg, nint wParam, nint lParam)
	{
		if (msg == 16)
		{
			Vst3EditorWindow vst3EditorWindow = s_active;
			if (vst3EditorWindow != null && vst3EditorWindow._hwnd == hWnd)
			{
				vst3EditorWindow.CloseInternal();
			}
			return IntPtr.Zero;
		}
		return DefWindowProcW(hWnd, msg, wParam, lParam);
	}

	private static void EnsureClassRegistered()
	{
		if (!s_classRegistered)
		{
			WndClassEx wc = new WndClassEx
			{
				cbSize = (uint)Marshal.SizeOf<WndClassEx>(),
				lpfnWndProc = Marshal.GetFunctionPointerForDelegate(s_wndProc),
				hInstance = GetModuleHandleW(null),
				lpszClassName = "FurinaVst3EditorWnd"
			};
			RegisterClassExW(ref wc);
			s_classRegistered = true;
		}
	}
}
