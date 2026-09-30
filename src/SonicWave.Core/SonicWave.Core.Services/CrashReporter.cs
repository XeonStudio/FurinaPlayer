using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;

namespace SonicWave.Core.Services;

public static class CrashReporter
{
	[UnmanagedFunctionPointer(CallingConvention.Winapi)]
	private delegate int UnhandledExceptionFilterDelegate(nint exceptionPointers);

	private struct MiniDumpExceptionInformation
	{
		public uint ThreadId;

		public nint ExceptionPointers;

		public bool ClientPointers;
	}

	private static string? _dir;

	private static UnhandledExceptionFilterDelegate? _filter;

	private static readonly object Gate = new object();

	private static volatile bool _initialized;

	private const uint GenericWrite = 1073741824u;

	private const uint CreateAlways = 2u;

	private const uint FileAttributeNormal = 128u;

	private const uint DumpFlags = 4133u;

	public static string DirectoryPath => _dir ?? string.Empty;

	[DllImport("kernel32", SetLastError = true)]
	private static extern nint SetUnhandledExceptionFilter(UnhandledExceptionFilterDelegate filter);

	[DllImport("kernel32")]
	private static extern nint GetCurrentProcess();

	[DllImport("kernel32")]
	private static extern uint GetCurrentProcessId();

	[DllImport("kernel32")]
	private static extern uint GetCurrentThreadId();

	[DllImport("kernel32", CharSet = CharSet.Unicode, SetLastError = true)]
	private static extern nint CreateFileW(string name, uint access, uint share, nint sa, uint creation, uint flags, nint template);

	[DllImport("kernel32")]
	private static extern bool CloseHandle(nint h);

	[DllImport("dbghelp.dll", SetLastError = true)]
	private static extern bool MiniDumpWriteDump(nint process, uint pid, nint file, uint dumpType, ref MiniDumpExceptionInformation exceptionParam, nint userStream, nint callback);

	public static void Initialize()
	{
		lock (Gate)
		{
			if (_initialized)
			{
				return;
			}
			try
			{
				string text = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "FurinaPlayer");
				try
				{
					Directory.CreateDirectory(text);
				}
				catch
				{
					text = AppContext.BaseDirectory;
				}
				_dir = Path.Combine(text, "CrashReports");
				try
				{
					Directory.CreateDirectory(_dir);
					string path = Path.Combine(_dir, ".writetest");
					File.WriteAllText(path, "ok");
					File.Delete(path);
				}
				catch
				{
					_dir = Path.Combine(AppContext.BaseDirectory, "CrashReports");
					Directory.CreateDirectory(_dir);
				}
				_filter = NativeFilter;
				SetUnhandledExceptionFilter(_filter);
				_initialized = true;
			}
			catch
			{
			}
		}
	}

	public static void WriteManaged(Exception? ex, string source)
	{
		try
		{
			if (!_initialized)
			{
				Initialize();
			}
			if (!string.IsNullOrEmpty(_dir))
			{
				string path = Path.Combine(_dir, "crash_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".txt");
				string text = string.Join(Environment.NewLine, "时间: " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), "来源: " + source, "版本: " + AppVersion(), "异常: " + (ex?.GetType().FullName ?? "null"), "消息: " + ex?.Message, "堆栈: " + ex?.StackTrace, "内部: " + ex?.InnerException);
				File.WriteAllText(path, text + Environment.NewLine);
			}
		}
		catch
		{
		}
	}

	public static void LogInfo(string message)
	{
		try
		{
			if (!_initialized)
			{
				Initialize();
			}
			if (!string.IsNullOrEmpty(_dir))
			{
				File.AppendAllText(Path.Combine(_dir, "furina.log"), DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff") + " " + message + Environment.NewLine);
			}
		}
		catch
		{
		}
	}

	private static string AppVersion()
	{
		try
		{
			AssemblyInformationalVersionAttribute customAttribute = Assembly.GetEntryAssembly().GetCustomAttribute<AssemblyInformationalVersionAttribute>();
			return string.IsNullOrWhiteSpace(customAttribute?.InformationalVersion) ? "unknown" : customAttribute.InformationalVersion;
		}
		catch
		{
			return "unknown";
		}
	}

	private static int NativeFilter(nint exceptionPointers)
	{
		uint num = 0u;
		nint num2 = IntPtr.Zero;
		string text = "";
		string text2 = "";
		bool flag = false;
		try
		{
			nint ptr = Marshal.ReadIntPtr(exceptionPointers);
			num = (uint)Marshal.ReadInt32(ptr);
			num2 = Marshal.ReadIntPtr(ptr, IntPtr.Size * 2);
			if (_dir == null)
			{
				Initialize();
			}
			if (_dir != null)
			{
				text = "crash_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".dmp";
				string text3 = Path.Combine(_dir, text);
				nint num3 = CreateFileW(text3, 1073741824u, 0u, IntPtr.Zero, 2u, 128u, IntPtr.Zero);
				if (num3 != new IntPtr(-1) && num3 != IntPtr.Zero)
				{
					try
					{
						MiniDumpExceptionInformation exceptionParam = new MiniDumpExceptionInformation
						{
							ThreadId = GetCurrentThreadId(),
							ExceptionPointers = exceptionPointers,
							ClientPointers = true
						};
						flag = MiniDumpWriteDump(GetCurrentProcess(), GetCurrentProcessId(), num3, 4133u, ref exceptionParam, IntPtr.Zero, IntPtr.Zero);
						if (!flag)
						{
							text2 = "win32=" + Marshal.GetLastWin32Error();
						}
					}
					catch (Exception ex)
					{
						text2 = "exception=" + ex.GetType().Name;
					}
					finally
					{
						CloseHandle(num3);
					}
					if (!flag)
					{
						try
						{
							if (File.Exists(text3))
							{
								File.Delete(text3);
							}
						}
						catch
						{
						}
					}
				}
				else
				{
					text2 = "createfile-failed";
				}
			}
		}
		catch
		{
		}
		try
		{
			if (_dir != null)
			{
				File.AppendAllText(Path.Combine(_dir, "native_crashes.log"), DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff") + " 0x" + num.ToString("X8") + " @0x" + ((IntPtr)num2).ToInt64().ToString("X") + " minidump:" + text + " " + (flag ? "ok" : ("failed " + text2)) + Environment.NewLine);
			}
		}
		catch
		{
		}
		return 0;
	}
}
