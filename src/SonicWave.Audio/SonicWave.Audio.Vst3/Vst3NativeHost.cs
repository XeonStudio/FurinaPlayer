using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using SonicWave.Core.Models;

namespace SonicWave.Audio.Vst3;

public sealed class Vst3NativeHost
{
	private readonly object _gate = new object();

	private Vst3NativeInstance? _active;

	private readonly Dictionary<string, Vst3NativeInstance> _auxInstances = new Dictionary<string, Vst3NativeInstance>(StringComparer.OrdinalIgnoreCase);

	private readonly HashSet<string> _chainKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

	private readonly ConcurrentDictionary<string, byte[]> _stateCache = new ConcurrentDictionary<string, byte[]>(StringComparer.OrdinalIgnoreCase);

	private volatile bool _transportPlaying;

	private long _stateVersion;

	[ThreadStatic]
	private static List<(bool isSimulated, Vst3PluginState plugin, Vst3NativeInstance? instance)>? t_processScratch;

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

	public long StateVersion => Interlocked.Read(ref _stateVersion);

	public void SetTransportPlaying(bool playing)
	{
		_transportPlaying = playing;
		List<Vst3NativeInstance> targets = new List<Vst3NativeInstance>(4);
		lock (_gate)
		{
			if (_active != null)
			{
				targets.Add(_active);
			}
			foreach (Vst3NativeInstance inst in _auxInstances.Values)
			{
				targets.Add(inst);
			}
		}
		foreach (Vst3NativeInstance inst in targets)
		{
			try { inst.SetTransportPlaying(playing); } catch { }
		}
	}

	public void TriggerTransportRestart(int pauseBlocks = 100)
	{
		List<Vst3NativeInstance> targets = new List<Vst3NativeInstance>(4);
		lock (_gate)
		{
			if (_active != null)
			{
				targets.Add(_active);
			}
			foreach (Vst3NativeInstance inst in _auxInstances.Values)
			{
				targets.Add(inst);
			}
		}
		foreach (Vst3NativeInstance inst in targets)
		{
			try { inst.TriggerTransportRestart(pauseBlocks); } catch { }
		}
	}

	public event Action? Unloaded;

	public event Action<int, int>? EditorResizeRequested;

	private Vst3NativeHost()
	{
	}

	internal static string ResolveCacheKey(string pluginPath)
	{
		if (string.IsNullOrWhiteSpace(pluginPath))
		{
			return string.Empty;
		}
		string? resolved = Vst3NativeInstance.ResolveModulePath(pluginPath);
		return Vst3NativeInstance.NormalizePath(resolved ?? pluginPath);
	}

	private void OnInstanceParameterEdited()
	{
		Interlocked.Increment(ref _stateVersion);
	}

	private void OnInstanceEditorResizeRequested(int w, int h)
	{
		EditorResizeRequested?.Invoke(w, h);
	}

	private bool SaveInstanceState(Vst3NativeInstance? inst)
	{
		if (inst == null)
		{
			return false;
		}
		byte[]? exported = inst.ExportState();
		if (exported == null || exported.Length == 0)
		{
			return false;
		}
		string key = ResolveCacheKey(inst.Path);
		bool changed = true;
		if (_stateCache.TryGetValue(key, out byte[]? existing) && existing.AsSpan().SequenceEqual(exported))
		{
			changed = false;
		}
		_stateCache[key] = exported;
		if (changed)
		{
			Interlocked.Increment(ref _stateVersion);
		}
		return changed;
	}

	public byte[]? GetLiveOrCachedState(string pluginPath)
	{
		if (string.IsNullOrWhiteSpace(pluginPath))
		{
			return null;
		}
		string key = ResolveCacheKey(pluginPath);
		Vst3NativeInstance? live = null;
		lock (_gate)
		{
			if (_active != null && _active.MatchesPath(pluginPath))
			{
				live = _active;
			}
			else if (_auxInstances.TryGetValue(key, out Vst3NativeInstance? aux))
			{
				live = aux;
			}
		}
		if (live != null)
		{
			byte[]? exported = live.ExportState();
			if (exported != null && exported.Length > 0)
			{
				_stateCache[key] = exported;
				return exported.ToArray();
			}
		}
		return _stateCache.TryGetValue(key, out byte[]? cached) ? cached.ToArray() : null;
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
		if (string.IsNullOrWhiteSpace(pluginPath))
		{
			return false;
		}
		Vst3NativeInstance? active = _active;
		return active != null && active.MatchesPath(pluginPath);
	}

	private Vst3NativeInstance? CreateInitializedInstance(string pluginPath, double sampleRate, int channels, out string error)
	{
		error = "";
		double targetRate = sampleRate > 0.0 ? sampleRate : 44100.0;
		int targetCh = channels > 0 ? channels : 2;
		Vst3NativeInstance? instance;
		try
		{
			instance = new Vst3NativeInstance(pluginPath);
		}
		catch (Exception ex)
		{
			error = "创建宿主实例失败：" + ex.Message;
			return null;
		}
		string? text = instance.Initialize(targetRate, targetCh);
		if (text != null)
		{
			try
			{
				instance.Dispose();
			}
			catch
			{
			}
			error = text;
			return null;
		}
		string key = ResolveCacheKey(pluginPath);
		if (_stateCache.TryGetValue(key, out byte[]? savedState) && savedState.Length > 0)
		{
			instance.ImportState(savedState);
		}
		instance.SetTransportPlaying(_transportPlaying);
		instance.ParameterEdited += OnInstanceParameterEdited;
		instance.EditorResizeRequested += OnInstanceEditorResizeRequested;
		return instance;
	}

	public bool EnsureLoaded(string pluginPath, double sampleRate, int channels, out string error)
	{
		error = "";
		if (string.IsNullOrWhiteSpace(pluginPath))
		{
			error = "插件路径为空";
			return false;
		}

		Vst3NativeInstance? existingActive = null;
		lock (_gate)
		{
			if (_active != null && _active.MatchesPath(pluginPath))
			{
				existingActive = _active;
			}
		}
		if (existingActive != null)
		{
			if (sampleRate > 0.0 && Math.Abs(existingActive.SampleRate - sampleRate) > 0.5)
			{
				existingActive.UpdateSampleRate(sampleRate);
			}
			return true;
		}

		Vst3NativeInstance? newInstance = CreateInitializedInstance(pluginPath, sampleRate, channels, out error);
		if (newInstance == null)
		{
			return false;
		}

		List<Vst3NativeInstance> toDispose = new List<Vst3NativeInstance>(2);
		Vst3NativeInstance? demotedAux = null;
		bool notifyUnloaded = false;
		string newKey = ResolveCacheKey(pluginPath);

		lock (_gate)
		{
			if (_active != null && _active.MatchesPath(pluginPath))
			{
				if (sampleRate > 0.0 && Math.Abs(_active.SampleRate - sampleRate) > 0.5)
				{
					_active.UpdateSampleRate(sampleRate);
				}
				toDispose.Add(newInstance);
				newInstance = null;
			}
			else
			{
				if (_auxInstances.Remove(newKey, out Vst3NativeInstance? existingAux))
				{
					byte[]? auxState = existingAux.ExportState();
					if (auxState != null && auxState.Length > 0)
					{
						_stateCache[newKey] = auxState;
						newInstance.ImportState(auxState);
					}
					toDispose.Add(existingAux);
				}
				if (_active != null)
				{
					Vst3NativeInstance oldActive = _active;
					notifyUnloaded = true;
					string oldKey = ResolveCacheKey(oldActive.Path);
					if (_chainKeys.Contains(oldKey) && !string.Equals(oldKey, newKey, StringComparison.OrdinalIgnoreCase))
					{
						_auxInstances[oldKey] = oldActive;
						demotedAux = oldActive;
					}
					else
					{
						toDispose.Add(oldActive);
					}
				}
				_active = newInstance;
			}
		}

		if (notifyUnloaded)
		{
			if (demotedAux != null)
			{
				try { demotedAux.CloseEditor(); } catch { }
			}
			Unloaded?.Invoke();
		}

		foreach (Vst3NativeInstance inst in toDispose)
		{
			SaveInstanceState(inst);
			try
			{
				inst.Dispose();
			}
			catch
			{
			}
		}
		return true;
	}

	public void EnsureChainLoaded(IReadOnlyList<Vst3PluginState>? plugins, double sampleRate, int channels, bool unloadPrimaryWhenEmpty = true)
	{
		List<Vst3PluginState> enabledNative = new List<Vst3PluginState>();
		List<Vst3PluginState> activeNative = new List<Vst3PluginState>();
		if (plugins != null)
		{
			foreach (Vst3PluginState? item in plugins)
			{
				if (item != null && item.Enabled && !string.IsNullOrWhiteSpace(item.Path) && Vst3Manager.IsValidVst3Path(item.Path))
				{
					enabledNative.Add(item);
					if (!item.Bypass)
					{
						activeNative.Add(item);
					}
				}
			}
		}

		if (enabledNative.Count == 0)
		{
			List<Vst3NativeInstance> auxToDispose;
			lock (_gate)
			{
				_chainKeys.Clear();
				auxToDispose = new List<Vst3NativeInstance>(_auxInstances.Values);
				_auxInstances.Clear();
			}
			foreach (Vst3NativeInstance inst in auxToDispose)
			{
				SaveInstanceState(inst);
				try { inst.Dispose(); } catch { }
			}
			if (unloadPrimaryWhenEmpty)
			{
				Unload();
			}
			return;
		}

		HashSet<string> neededKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		foreach (Vst3PluginState item in activeNative)
		{
			neededKeys.Add(ResolveCacheKey(item.Path));
		}

		bool activeMatchesEnabled = false;
		Vst3NativeInstance? currentActive;
		lock (_gate)
		{
			_chainKeys.Clear();
			foreach (string k in neededKeys)
			{
				_chainKeys.Add(k);
			}
			currentActive = _active;
			if (currentActive != null)
			{
				foreach (Vst3PluginState item in enabledNative)
				{
					if (currentActive.MatchesPath(item.Path))
					{
						activeMatchesEnabled = true;
						break;
					}
				}
			}
		}

		if (activeNative.Count > 0 && (currentActive == null || !activeMatchesEnabled))
		{
			EnsureLoaded(activeNative[0].Path, sampleRate, channels, out _);
		}
		else if (currentActive != null && sampleRate > 0.0 && Math.Abs(currentActive.SampleRate - sampleRate) > 0.5)
		{
			currentActive.UpdateSampleRate(sampleRate);
		}

		// Ensure aux instances exist for any remaining active plugins not covered by _active
		List<string> missingPaths = new List<string>();
		lock (_gate)
		{
			string? activeKey = _active != null ? ResolveCacheKey(_active.Path) : null;
			foreach (Vst3PluginState item in activeNative)
			{
				string key = ResolveCacheKey(item.Path);
				if (activeKey != null && string.Equals(key, activeKey, StringComparison.OrdinalIgnoreCase))
				{
					continue;
				}
				if (!_auxInstances.ContainsKey(key))
				{
					missingPaths.Add(item.Path);
				}
			}
		}

		Dictionary<string, Vst3NativeInstance> createdAux = new Dictionary<string, Vst3NativeInstance>(StringComparer.OrdinalIgnoreCase);
		foreach (string path in missingPaths)
		{
			string key = ResolveCacheKey(path);
			if (createdAux.ContainsKey(key))
			{
				continue;
			}
			Vst3NativeInstance? aux = CreateInitializedInstance(path, sampleRate, channels, out _);
			if (aux != null)
			{
				createdAux[key] = aux;
			}
		}

		List<Vst3NativeInstance> staleToDispose = new List<Vst3NativeInstance>();
		List<Vst3NativeInstance> rateSyncTargets = new List<Vst3NativeInstance>();
		lock (_gate)
		{
			string? activeKey = _active != null ? ResolveCacheKey(_active.Path) : null;
			foreach (KeyValuePair<string, Vst3NativeInstance> kv in createdAux)
			{
				if ((activeKey != null && string.Equals(kv.Key, activeKey, StringComparison.OrdinalIgnoreCase)) ||
				    _auxInstances.ContainsKey(kv.Key) ||
				    !_chainKeys.Contains(kv.Key))
				{
					staleToDispose.Add(kv.Value);
				}
				else
				{
					_auxInstances[kv.Key] = kv.Value;
				}
			}

			List<string> removeKeys = new List<string>();
			foreach (KeyValuePair<string, Vst3NativeInstance> kv in _auxInstances)
			{
				if (!_chainKeys.Contains(kv.Key) || (activeKey != null && string.Equals(kv.Key, activeKey, StringComparison.OrdinalIgnoreCase)))
				{
					removeKeys.Add(kv.Key);
					staleToDispose.Add(kv.Value);
				}
				else
				{
					rateSyncTargets.Add(kv.Value);
				}
			}
			foreach (string k in removeKeys)
			{
				_auxInstances.Remove(k);
			}
		}

		if (sampleRate > 0.0)
		{
			foreach (Vst3NativeInstance inst in rateSyncTargets)
			{
				if (Math.Abs(inst.SampleRate - sampleRate) > 0.5)
				{
					inst.UpdateSampleRate(sampleRate);
				}
			}
		}

		foreach (Vst3NativeInstance inst in staleToDispose)
		{
			SaveInstanceState(inst);
			try { inst.Dispose(); } catch { }
		}
	}

	public void Unload()
	{
		Vst3NativeInstance? active;
		List<Vst3NativeInstance> auxToDispose;
		lock (_gate)
		{
			active = _active;
			_active = null;
			_chainKeys.Clear();
			auxToDispose = new List<Vst3NativeInstance>(_auxInstances.Values);
			_auxInstances.Clear();
		}
		foreach (Vst3NativeInstance inst in auxToDispose)
		{
			SaveInstanceState(inst);
			try { inst.Dispose(); } catch { }
		}
		if (active == null)
		{
			return;
		}
		SaveInstanceState(active);
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

	public bool ProcessPlugins(float[] interleaved, int channels, int sampleRate, IReadOnlyList<Vst3PluginState>? plugins)
	{
		if (plugins == null || plugins.Count == 0 || interleaved == null || interleaved.Length == 0 || channels <= 0)
		{
			return false;
		}

		List<(bool isSimulated, Vst3PluginState plugin, Vst3NativeInstance? instance)> scratch = t_processScratch ??= new List<(bool, Vst3PluginState, Vst3NativeInstance?)>(8);
		scratch.Clear();

		lock (_gate)
		{
			for (int i = 0; i < plugins.Count; i++)
			{
				Vst3PluginState? item = plugins[i];
				if (item == null || !item.IsActive)
				{
					continue;
				}
				if (string.IsNullOrWhiteSpace(item.Path) || !Vst3Manager.IsValidVst3Path(item.Path))
				{
					scratch.Add((true, item, null));
					continue;
				}
				Vst3NativeInstance? match = null;
				if (_active != null && _active.MatchesPath(item.Path))
				{
					match = _active;
				}
				else
				{
					string key = ResolveCacheKey(item.Path);
					_auxInstances.TryGetValue(key, out match);
				}
				if (match != null)
				{
					scratch.Add((false, item, match));
				}
			}
		}

		bool anyProcessed = false;
		for (int i = 0; i < scratch.Count; i++)
		{
			var entry = scratch[i];
			if (entry.isSimulated)
			{
				Vst3SimulatedProcessor.Process(interleaved, channels, new Vst3Parameters(entry.plugin.Drive, entry.plugin.Tone, entry.plugin.Mix));
				anyProcessed = true;
			}
			else if (entry.instance != null)
			{
				if (sampleRate > 0 && Math.Abs(entry.instance.SampleRate - sampleRate) > 0.5)
				{
					entry.instance.UpdateSampleRate(sampleRate);
				}
				if (entry.instance.TryProcess(interleaved, channels))
				{
					anyProcessed = true;
				}
			}
		}
		scratch.Clear();
		return anyProcessed;
	}

	public bool OpenEditor(nint parentHwnd, out int width, out int height, out string error)
	{
		width = 640;
		height = 480;
		error = "";
		Vst3NativeInstance? active = _active;
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
		if (sampleRate <= 0.0)
		{
			return;
		}
		List<Vst3NativeInstance> targets = new List<Vst3NativeInstance>(4);
		lock (_gate)
		{
			if (_active != null)
			{
				targets.Add(_active);
			}
			foreach (Vst3NativeInstance inst in _auxInstances.Values)
			{
				targets.Add(inst);
			}
		}
		foreach (Vst3NativeInstance inst in targets)
		{
			if (Math.Abs(inst.SampleRate - sampleRate) > 0.5)
			{
				inst.UpdateSampleRate(sampleRate);
			}
		}
	}

	public void EditorResized(int width, int height)
	{
		_active?.EditorResized(width, height);
	}

	public void CloseEditor()
	{
		Vst3NativeInstance? active = _active;
		if (active == null)
		{
			return;
		}
		active.CloseEditor();
		SaveInstanceState(active);
	}
}
