using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;

namespace SonicWave.App;

/// <summary>
/// 仿照 ECHO 实现的 Windows 系统任务栏托盘管理器：
/// 拦截关闭事件隐藏到托盘、后台图标交互、快捷右键播放控制及气泡通知。
/// </summary>
public sealed class SystemTrayManager : IDisposable
{
	private const int WM_USER = 0x0400;
	private const int WM_TRAY = WM_USER + 1024;
	private const int WM_COMMAND = 0x0111;
	private const int WM_LBUTTONUP = 0x0202;
	private const int WM_LBUTTONDBLCLK = 0x0203;
	private const int WM_RBUTTONUP = 0x0205;

	private const int NIM_ADD = 0x00000000;
	private const int NIM_MODIFY = 0x00000001;
	private const int NIM_DELETE = 0x00000002;

	private const int NIF_MESSAGE = 0x00000001;
	private const int NIF_ICON = 0x00000002;
	private const int NIF_TIP = 0x00000004;
	private const int NIF_INFO = 0x00000010;

	private const int NIIF_INFO = 0x00000001;

	private const int TPM_LEFTALIGN = 0x0000;
	private const int TPM_RIGHTBUTTON = 0x0002;

	private const int MF_STRING = 0x00000000;
	private const int MF_SEPARATOR = 0x00000800;

	private const int IDM_SHOW = 1001;
	private const int IDM_PLAYPAUSE = 1002;
	private const int IDM_NEXT = 1003;
	private const int IDM_PREV = 1004;
	private const int IDM_EXIT = 1005;

	[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
	private struct NOTIFYICONDATA
	{
		public int cbSize;
		public IntPtr hWnd;
		public int uID;
		public int uFlags;
		public int uCallbackMessage;
		public IntPtr hIcon;
		[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
		public string szTip;
		public int dwState;
		public int dwStateMask;
		[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
		public string szInfo;
		public int uTimeoutOrVersion;
		[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 64)]
		public string szInfoTitle;
		public int dwInfoFlags;
		public Guid guidItem;
		public IntPtr hBalloonIcon;
	}

	[StructLayout(LayoutKind.Sequential)]
	private struct POINT
	{
		public int X;
		public int Y;
	}

	[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
	private struct WNDCLASSEX
	{
		public int cbSize;
		public int style;
		public IntPtr lpfnWndProc;
		public int cbClsExtra;
		public int cbWndExtra;
		public IntPtr hInstance;
		public IntPtr hIcon;
		public IntPtr hCursor;
		public IntPtr hbrBackground;
		public string? lpszMenuName;
		public string lpszClassName;
		public IntPtr hIconSm;
	}

	private delegate IntPtr WndProcDelegate(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

	[DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
	private static extern ushort RegisterClassEx(ref WNDCLASSEX lpwcx);

	[DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
	private static extern IntPtr CreateWindowEx(int dwExStyle, string lpClassName, string lpWindowName, int dwStyle, int x, int y, int nWidth, int nHeight, IntPtr hWndParent, IntPtr hMenu, IntPtr hInstance, IntPtr lpParam);

	[DllImport("user32.dll")]
	private static extern bool DestroyWindow(IntPtr hWnd);

	[DllImport("user32.dll")]
	private static extern IntPtr DefWindowProc(IntPtr hWnd, uint uMsg, IntPtr wParam, IntPtr lParam);

	[DllImport("user32.dll")]
	private static extern bool GetCursorPos(out POINT lpPoint);

	[DllImport("user32.dll")]
	private static extern bool SetForegroundWindow(IntPtr hWnd);

	[DllImport("user32.dll")]
	private static extern IntPtr CreatePopupMenu();

	[DllImport("user32.dll", CharSet = CharSet.Auto)]
	private static extern bool AppendMenu(IntPtr hMenu, int uFlags, IntPtr uIDNewItem, string lpNewItem);

	[DllImport("user32.dll")]
	private static extern bool DestroyMenu(IntPtr hMenu);

	[DllImport("user32.dll")]
	private static extern bool TrackPopupMenu(IntPtr hMenu, uint uFlags, int x, int y, int nReserved, IntPtr hWnd, IntPtr prcRect);

	[DllImport("shell32.dll", CharSet = CharSet.Auto)]
	private static extern bool Shell_NotifyIcon(int dwMessage, ref NOTIFYICONDATA lpData);

	[DllImport("user32.dll", CharSet = CharSet.Auto)]
	private static extern IntPtr LoadImage(IntPtr hInst, string lpszName, uint uType, int cxDesired, int cyDesired, uint fuLoad);

	[DllImport("user32.dll")]
	private static extern bool DestroyIcon(IntPtr hIcon);

	[DllImport("kernel32.dll", CharSet = CharSet.Auto)]
	private static extern IntPtr GetModuleHandle(string? lpModuleName);

	private IntPtr _msgHwnd;
	private IntPtr _hIcon;
	private WndProcDelegate? _wndProc;
	private bool _isCreated = false;

	public event Action? RestoreRequested;
	public event Action? PlayPauseRequested;
	public event Action? NextRequested;
	public event Action? PrevRequested;
	public event Action? ExitRequested;

	public void Initialize()
	{
		if (_isCreated) return;

		try
		{
			string className = "FurinaPlayer_TrayClass_" + Guid.NewGuid().ToString("N");
			_wndProc = WndProc;

			WNDCLASSEX wc = new WNDCLASSEX
			{
				cbSize = Marshal.SizeOf<WNDCLASSEX>(),
				lpfnWndProc = Marshal.GetFunctionPointerForDelegate(_wndProc),
				hInstance = GetModuleHandle(null),
				lpszClassName = className
			};

			RegisterClassEx(ref wc);

			_msgHwnd = CreateWindowEx(0, className, "FurinaPlayerTrayMessageWindow", 0, 0, 0, 0, 0, new IntPtr(-3) /* HWND_MESSAGE */, IntPtr.Zero, wc.hInstance, IntPtr.Zero);

			// 加载 app.ico 图标
			string icoPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "app.ico");
			if (File.Exists(icoPath))
			{
				_hIcon = LoadImage(IntPtr.Zero, icoPath, 1 /* IMAGE_ICON */, 16, 16, 0x00000010 /* LR_LOADFROMFILE */);
			}

			NOTIFYICONDATA nid = new NOTIFYICONDATA
			{
				cbSize = Marshal.SizeOf<NOTIFYICONDATA>(),
				hWnd = _msgHwnd,
				uID = 1,
				uFlags = NIF_MESSAGE | NIF_ICON | NIF_TIP,
				uCallbackMessage = WM_TRAY,
				hIcon = _hIcon,
				szTip = "FurinaPlayer - 高保真音乐播放器"
			};

			Shell_NotifyIcon(NIM_ADD, ref nid);
			_isCreated = true;
			Debug.WriteLine("[SystemTrayManager] Tray icon initialized successfully.");
		}
		catch (Exception ex)
		{
			Debug.WriteLine($"[SystemTrayManager] Failed to init tray: {ex.Message}");
		}
	}

	public void ShowNotification(string title, string message)
	{
		if (!_isCreated || _msgHwnd == IntPtr.Zero) return;
		try
		{
			NOTIFYICONDATA nid = new NOTIFYICONDATA
			{
				cbSize = Marshal.SizeOf<NOTIFYICONDATA>(),
				hWnd = _msgHwnd,
				uID = 1,
				uFlags = NIF_INFO,
				szInfoTitle = title,
				szInfo = message,
				dwInfoFlags = NIIF_INFO
			};
			Shell_NotifyIcon(NIM_MODIFY, ref nid);
		}
		catch { }
	}

	private IntPtr WndProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam)
	{
		if (msg == WM_TRAY)
		{
			int evt = lParam.ToInt32();
			if (evt == WM_LBUTTONUP || evt == WM_LBUTTONDBLCLK)
			{
				RestoreRequested?.Invoke();
				return IntPtr.Zero;
			}
			else if (evt == WM_RBUTTONUP)
			{
				ShowContextMenu();
				return IntPtr.Zero;
			}
		}
		else if (msg == WM_COMMAND)
		{
			int cmd = wParam.ToInt32() & 0xFFFF;
			switch (cmd)
			{
				case IDM_SHOW:
					RestoreRequested?.Invoke();
					break;
				case IDM_PLAYPAUSE:
					PlayPauseRequested?.Invoke();
					break;
				case IDM_NEXT:
					NextRequested?.Invoke();
					break;
				case IDM_PREV:
					PrevRequested?.Invoke();
					break;
				case IDM_EXIT:
					ExitRequested?.Invoke();
					break;
			}
			return IntPtr.Zero;
		}

		return DefWindowProc(hWnd, msg, wParam, lParam);
	}

	private void ShowContextMenu()
	{
		IntPtr hMenu = CreatePopupMenu();
		if (hMenu == IntPtr.Zero) return;

		try
		{
			AppendMenu(hMenu, MF_STRING, new IntPtr(IDM_SHOW), "显示主窗口");
			AppendMenu(hMenu, MF_SEPARATOR, IntPtr.Zero, string.Empty);
			AppendMenu(hMenu, MF_STRING, new IntPtr(IDM_PLAYPAUSE), "播放 / 暂停");
			AppendMenu(hMenu, MF_STRING, new IntPtr(IDM_PREV), "上一首");
			AppendMenu(hMenu, MF_STRING, new IntPtr(IDM_NEXT), "下一首");
			AppendMenu(hMenu, MF_SEPARATOR, IntPtr.Zero, string.Empty);
			AppendMenu(hMenu, MF_STRING, new IntPtr(IDM_EXIT), "退出 FurinaPlayer");

			GetCursorPos(out POINT pt);
			SetForegroundWindow(_msgHwnd);
			TrackPopupMenu(hMenu, TPM_LEFTALIGN | TPM_RIGHTBUTTON, pt.X, pt.Y, 0, _msgHwnd, IntPtr.Zero);
		}
		finally
		{
			DestroyMenu(hMenu);
		}
	}

	public void Dispose()
	{
		if (_isCreated)
		{
			try
			{
				NOTIFYICONDATA nid = new NOTIFYICONDATA
				{
					cbSize = Marshal.SizeOf<NOTIFYICONDATA>(),
					hWnd = _msgHwnd,
					uID = 1
				};
				Shell_NotifyIcon(NIM_DELETE, ref nid);
				if (_msgHwnd != IntPtr.Zero) DestroyWindow(_msgHwnd);
				if (_hIcon != IntPtr.Zero) DestroyIcon(_hIcon);
			}
			catch { }
			_isCreated = false;
		}
	}
}
