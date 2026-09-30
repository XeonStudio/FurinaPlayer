using System;
using System.Runtime.InteropServices;

namespace SonicWave.Audio.Vst3;

public sealed class Vst3NativeHost
{
	private readonly object _gate = new object();

	private Vst3NativeInstance? _active;

	public static bool UseStereoArrangement = true;

	public static bool UseParameterChanges = true;

	public static bool DebugLog = false;

	public static bool HackParamPointer = false;

	public static Vst3NativeHost Instance { get; } = new Vst3NativeHost();

	public string? LoadedPath => _active?.Path;

	public bool IsLoaded => _active != null;

	public int InChannels => _active?.InChannels ?? 2;

	public int OutChannels => _active?.OutChannels ?? 2;

	public double SampleRate => _active?.SampleRate ?? 0.0;

	public bool ProcessingEnabled => _active?.ProcessingEnabled ?? false;

	public string BusDump => _active?.GetBusDump() ?? "(none)";

	public event Action? Unloaded;

	public event Action<int, int>? EditorResizeRequested;

	private Vst3NativeHost()
	{
	}

	public static string SelfTestParamCcw()
	{
		try
		{
			Vst3ParameterChangesImpl vst3ParameterChangesImpl = new Vst3ParameterChangesImpl();
			vst3ParameterChangesImpl.SetValue(1234u, 0.5);
			nint num = Vst3Com.ToNative((IParameterChangesCom)vst3ParameterChangesImpl);
			nint ptr = Marshal.ReadIntPtr(num);
			int num2 = Marshal.GetDelegateForFunctionPointer<NativeMethods.GetParamCountFn>(Marshal.ReadIntPtr(ptr, 3 * IntPtr.Size))(num);
			nint num3 = Marshal.GetDelegateForFunctionPointer<NativeMethods.GetParamDataFn>(Marshal.ReadIntPtr(ptr, 4 * IntPtr.Size))(num, 0);
			nint ptr2 = Marshal.ReadIntPtr(num3);
			uint num4 = Marshal.GetDelegateForFunctionPointer<NativeMethods.GetParamIdFn>(Marshal.ReadIntPtr(ptr2, 3 * IntPtr.Size))(num3);
			NativeMethods.GetPointFn delegateForFunctionPointer = Marshal.GetDelegateForFunctionPointer<NativeMethods.GetPointFn>(Marshal.ReadIntPtr(ptr2, 5 * IntPtr.Size));
			int sampleOffset = 0;
			double value = 0.0;
			int num5 = delegateForFunctionPointer(num3, 0, out sampleOffset, out value);
			Marshal.Release(num);
			return "count=" + num2 + " id=" + num4 + " hr=" + num5.ToString("X8") + " val=" + value;
		}
		catch (Exception ex)
		{
			return "selftest ex: " + ex;
		}
	}

	public bool IsLoadedFor(string pluginPath)
	{
		Vst3NativeInstance active = _active;
		if (active != null)
		{
			return string.Equals(active.Path, pluginPath, StringComparison.OrdinalIgnoreCase);
		}
		return false;
	}

	public bool EnsureLoaded(string pluginPath, double sampleRate, int channels, out string error)
	{
		error = "";
		if (IsLoadedFor(pluginPath))
		{
			_active?.UpdateSampleRate(sampleRate);
			return true;
		}
		Vst3NativeInstance vst3NativeInstance = null;
		try
		{
			vst3NativeInstance = new Vst3NativeInstance(pluginPath);
		}
		catch (Exception ex)
		{
			error = "创建宿主实例失败：" + ex.Message;
			return false;
		}
		string text = vst3NativeInstance.Initialize(sampleRate, channels);
		if (text != null)
		{
			try
			{
				vst3NativeInstance.Dispose();
			}
			catch
			{
			}
			error = text;
			return false;
		}
		bool flag = false;
		lock (_gate)
		{
			Vst3NativeInstance active = _active;
			if (active != null && !string.Equals(active.Path, pluginPath, StringComparison.OrdinalIgnoreCase))
			{
				_active = null;
				try
				{
					active.Dispose();
				}
				catch
				{
				}
				flag = true;
			}
			_active = vst3NativeInstance;
		}
		if (flag)
		{
			Unloaded?.Invoke();
		}
		return true;
	}

	public void Unload()
	{
		Vst3NativeInstance active;
		lock (_gate)
		{
			active = _active;
			_active = null;
		}
		if (active == null)
		{
			return;
		}
		Unloaded?.Invoke();
		try
		{
			active.Dispose();
		}
		catch
		{
		}
	}

	public bool Process(float[] interleaved, int channels)
	{
		return _active?.TryProcess(interleaved, channels) ?? false;
	}

	public bool OpenEditor(nint parentHwnd, out int width, out int height, out string error)
	{
		width = 640;
		height = 480;
		error = "";
		Vst3NativeInstance active = _active;
		if (active == null)
		{
			error = "未加载任何插件";
			return false;
		}
		error = active.OpenEditor(parentHwnd, out width, out height) ?? "";
		return error.Length == 0;
	}

	public void UpdateSampleRate(double sampleRate)
	{
		_active?.UpdateSampleRate(sampleRate);
	}

	public void EditorResized(int width, int height)
	{
		_active?.EditorResized(width, height);
	}

	public void CloseEditor()
	{
		_active?.CloseEditor();
	}
}
