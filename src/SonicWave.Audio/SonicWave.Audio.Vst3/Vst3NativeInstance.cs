using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace SonicWave.Audio.Vst3;

public sealed class Vst3NativeInstance : IDisposable
{
	private nint _module;

	private nint _factoryRaw;

	private IPluginFactory? _factory;

	private IComponent? _component;

	private IEditController? _controller;

	private IAudioProcessor? _processor;

	private IConnectionPoint? _componentCp;

	private IConnectionPoint? _controllerCp;

	private nint _componentRaw;

	private nint _controllerRaw;

	private nint _processorRaw;

	private nint _componentCpRaw;

	private nint _controllerCpRaw;

	private nint _hostContextCcw;

	private nint _paramChangesCcw;

	private nint _factory2Raw;

	private readonly Vst3HostContextImpl _hostContext;

	private readonly Vst3ParameterChangesImpl _paramChanges = new Vst3ParameterChangesImpl();

	private int _inChannels = 2;

	private int _outChannels = 2;

	private int _numInputBusses = 1;

	private int _numOutputBusses = 1;

	private double _sampleRate;

	private int _maxBlock = 8192;

	private bool _processingEnabled;

	private bool _disposed;

	private IPlugView? _view;

	private nint _viewRaw;

	private bool _viewAttached;

	private bool _viewResizable;

	private nint _viewFrameCcw;

	private float[][] _inPlanar = Array.Empty<float[]>();

	private float[][] _outPlanar = Array.Empty<float[]>();

	private GCHandle[]? _inHandles;

	private GCHandle[]? _outHandles;

	private nint _inChannelBuffersPtr;

	private nint _outChannelBuffersPtr;

	private nint _inAudioBusBuffersPtr;

	private nint _outAudioBusBuffersPtr;

	private int _allocatedFrames;

	private int _allocatedInChannels;

	private int _allocatedOutChannels;

	private int _allocatedInBusses;

	private int _allocatedOutBusses;

	private readonly Vst3ParameterChangesImpl _outputParamChanges = new Vst3ParameterChangesImpl();
	private nint _outputParamChangesCcw = IntPtr.Zero;

	private float[]? _silentBuffer;
	private GCHandle _silentHandle;
	private nint _silentChannelBuffersPtr = IntPtr.Zero;

	private float[]? _scratchOutBuffer;
	private GCHandle _scratchOutHandle;
	private nint _scratchOutChannelBuffersPtr = IntPtr.Zero;

	public int NumInputBusses => _numInputBusses;
	public int NumOutputBusses => _numOutputBusses;
	public int ConnectionPointResult1 { get; private set; }
	public int ConnectionPointResult2 { get; private set; }
	public int LastProcessResult { get; private set; }
	public long ProcessCount { get; private set; }
	public float LastInPeak { get; private set; }
	public float LastOutPeak { get; private set; }

	private readonly object _lock = new object();

	private nint _processContextPtr = IntPtr.Zero;

	private long _continuousSampleTime;
	private int _transportResetCountdown = 0;
	private bool _transportPaused = false;

	public void SetTransportPlaying(bool playing)
	{
		lock (_lock)
		{
			_transportPaused = !playing;
			if (!playing)
			{
				_continuousSampleTime = 0;
			}
		}
	}

	public void TriggerTransportRestart(int pauseBlocks = 100)
	{
		lock (_lock)
		{
			_continuousSampleTime = 0;
			_transportResetCountdown = pauseBlocks;
		}
	}

	private static readonly List<GCHandle> _pins = new List<GCHandle>();

	public string Path { get; }

	public string DisplayName { get; }

	public int InChannels => _inChannels;

	public int OutChannels => _outChannels;

	public double SampleRate => _sampleRate;

	public bool ProcessingEnabled => _processingEnabled;

	public event Action<int, int>? EditorResizeRequested;

	public Vst3NativeInstance(string path)
	{
		Path = path;
		DisplayName = SafeName(path);
		_hostContext = new Vst3HostContextImpl(this);
	}

	private static string SafeName(string path)
	{
		try
		{
			string fileNameWithoutExtension = System.IO.Path.GetFileNameWithoutExtension(path);
			string result;
			if (!fileNameWithoutExtension.EndsWith(".vst3", StringComparison.OrdinalIgnoreCase))
			{
				result = fileNameWithoutExtension;
			}
			else
			{
				string text = fileNameWithoutExtension;
				result = text.Substring(0, text.Length - 5);
			}
			return result;
		}
		catch
		{
			return path;
		}
	}

	public unsafe string? Initialize(double sampleRate, int channels)
	{
		_sampleRate = sampleRate;
		string text = ResolveModulePath(Path);
		if (text == null)
		{
			return "无法定位 .vst3 模块文件（bundle 目录中未找到可加载 DLL）";
		}
		_module = NativeMethods.LoadLibraryExW(text, IntPtr.Zero, 8u);
		if (_module == IntPtr.Zero)
		{
			return "模块加载失败：" + text + "（错误码 " + Marshal.GetLastWin32Error() + "，请确认插件为 64 位且已安装运行库）";
		}
		nint procAddress = NativeMethods.GetProcAddress(_module, "GetPluginFactory");
		if (procAddress == IntPtr.Zero)
		{
			return "模块不是有效 VST3 插件（缺少 GetPluginFactory 导出）";
		}
		NativeMethods.GetPluginFactoryProc getPluginFactoryProc = (NativeMethods.GetPluginFactoryProc)Marshal.GetDelegateForFunctionPointer(procAddress, typeof(NativeMethods.GetPluginFactoryProc));
		nint num;
		try
		{
			num = getPluginFactoryProc();
		}
		catch
		{
			return "GetPluginFactory 调用失败";
		}
		if (num == IntPtr.Zero)
		{
			return "GetPluginFactory 返回空";
		}
		try
		{
			_factoryRaw = num;
			_factory = Vst3Com.ToManaged<IPluginFactory>(num);
			if (!FindEffectClass(_factory, out var info))
			{
				return "插件中未找到音频效果类（Audio Module Effect / Fx）";
			}
			byte[] array = new byte[16];
			Marshal.Copy((nint)info.Cid, array, 0, 16);
			byte[] guidBytes = Vst3Com.GetGuidBytes(Vst3Ids.IComponent);
			int num2 = CreateInstance(_factory, array, guidBytes, out _componentRaw);
			if (num2 != 0 || _componentRaw == IntPtr.Zero)
			{
				return "IComponent 创建失败";
			}
			_component = Vst3Com.ToManaged<IComponent>(_componentRaw);
			byte[] array2 = new byte[16];
			GCHandle gCHandle = GCHandle.Alloc(array2, GCHandleType.Pinned);
			try
			{
				num2 = _component.GetControllerClassId(gCHandle.AddrOfPinnedObject());
			}
			finally
			{
				gCHandle.Free();
			}
			if (num2 == 0 && array2.Any((byte b) => b != 0))
			{
				byte[] guidBytes2 = Vst3Com.GetGuidBytes(Vst3Ids.IEditController);
				if (CreateInstance(_factory, array2, guidBytes2, out _controllerRaw) == 0 && _controllerRaw != IntPtr.Zero)
				{
					_controller = Vst3Com.ToManaged<IEditController>(_controllerRaw);
				}
			}
			_hostContextCcw = Vst3Com.ToNative((IHostApplicationCom)_hostContext);
			num2 = _component.Initialize(_hostContextCcw);
			if (num2 != 0)
			{
				return "组件初始化失败（initialize 返回 0x" + num2.ToString("X8") + "）";
			}
			if (_controller == null)
			{
				nint ctrlPtr = QueryInterface(_componentRaw, Vst3Ids.IEditController);
				if (ctrlPtr != IntPtr.Zero)
				{
					_controllerRaw = ctrlPtr;
					_controller = Vst3Com.ToManaged<IEditController>(_controllerRaw);
				}
			}
			_controller?.Initialize(_hostContextCcw);
			if (_controller != null)
			{
				nint handlerCcw = Vst3Com.ToNative((IComponentHandlerCom)_hostContext);
				_controller.SetComponentHandler(handlerCcw);
			}
			_processorRaw = QueryInterface(_componentRaw, Vst3Ids.IAudioProcessor);
			if (_processorRaw == IntPtr.Zero)
			{
				return "组件不支持 IAudioProcessor";
			}
			_processor = Vst3Com.ToManaged<IAudioProcessor>(_processorRaw);
			_componentCpRaw = QueryInterface(_componentRaw, Vst3Ids.IConnectionPoint);
			_controllerCpRaw = ((_controllerRaw == IntPtr.Zero) ? IntPtr.Zero : QueryInterface(_controllerRaw, Vst3Ids.IConnectionPoint));
			if (_componentCpRaw != IntPtr.Zero)
			{
				_componentCp = Vst3Com.ToManaged<IConnectionPoint>(_componentCpRaw);
			}
			if (_controllerCpRaw != IntPtr.Zero)
			{
				_controllerCp = Vst3Com.ToManaged<IConnectionPoint>(_controllerCpRaw);
			}
			if (Vst3NativeHost.UseStereoArrangement)
			{
				ApplyStereoArrangement();
			}
			DetermineChannels();
			try
			{
				if (_componentCpRaw != IntPtr.Zero && _controllerCpRaw != IntPtr.Zero)
				{
					ConnectionPointResult1 = ConnectConnectionPoints(_componentCpRaw, _controllerCpRaw);
					ConnectionPointResult2 = ConnectConnectionPoints(_controllerCpRaw, _componentCpRaw);
					Console.WriteLine($"[echo-vst3-host] IConnectionPoint connected: comp->ctrl: 0x{ConnectionPointResult1:X8}, ctrl->comp: 0x{ConnectionPointResult2:X8}");
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine("[echo-vst3-host] Connection point connect failed: " + ex.Message);
			}
			ProcessSetup setup = new ProcessSetup
			{
				ProcessMode = 0,
				SymbolicSampleSize = 0,
				MaxSamplesPerBlock = _maxBlock,
				SampleRate = sampleRate
			};
			num2 = _processor.SetupProcessing(ref setup);
			if (num2 != 0)
			{
				return "setupProcessing 失败（0x" + num2.ToString("X8") + "）";
			}
			ActivateBuses();
			_component.SetActive(1);
			_processor.SetProcessing(1);
			_processingEnabled = true;
			_paramChangesCcw = Vst3Com.ToNative((IParameterChangesCom)_paramChanges);
			_outputParamChangesCcw = Vst3Com.ToNative((IParameterChangesCom)_outputParamChanges);
			if (_processContextPtr == IntPtr.Zero)
			{
				_processContextPtr = Marshal.AllocHGlobal(Marshal.SizeOf<Vst3ProcessContext>());
			}
			_continuousSampleTime = 0;
			EnsureBuffers(_maxBlock, _inChannels, _outChannels);
			SyncComponentStateToController();
			return null;
		}
		catch (Exception ex2)
		{
			return "VST3 宿主初始化异常：" + ex2.Message;
		}
	}

	private static nint QueryInterface(nint comObject, Guid iid)
	{
		Guid iid2 = iid;
		if (Marshal.QueryInterface(comObject, in iid2, out var ppv) != 0)
		{
			return IntPtr.Zero;
		}
		return ppv;
	}

	[UnmanagedFunctionPointer(CallingConvention.StdCall)]
	private delegate int ConnectFn(nint thisPtr, nint other);

	private static int ConnectConnectionPoints(nint sourceCp, nint targetCp)
	{
		if (sourceCp == IntPtr.Zero || targetCp == IntPtr.Zero) return -1;
		try
		{
			nint vTable = Marshal.ReadIntPtr(sourceCp);
			nint fnPtr = Marshal.ReadIntPtr(vTable, 3 * IntPtr.Size);
			var fn = Marshal.GetDelegateForFunctionPointer<ConnectFn>(fnPtr);
			return fn(sourceCp, targetCp);
		}
		catch (Exception ex)
		{
			Console.WriteLine("[echo-vst3-host] ConnectConnectionPoints error: " + ex.Message);
			return -2147467259;
		}
	}

	private static int CreateInstance(IPluginFactory factory, byte[] cid, byte[] iid, out nint obj)
	{
		obj = IntPtr.Zero;
		GCHandle gCHandle = GCHandle.Alloc(cid, GCHandleType.Pinned);
		GCHandle gCHandle2 = GCHandle.Alloc(iid, GCHandleType.Pinned);
		try
		{
			return factory.CreateInstance(gCHandle.AddrOfPinnedObject(), gCHandle2.AddrOfPinnedObject(), out obj);
		}
		finally
		{
			gCHandle.Free();
			gCHandle2.Free();
		}
	}

	private static nint PinBytes(byte[] bytes)
	{
		GCHandle item = GCHandle.Alloc(bytes, GCHandleType.Pinned);
		_pins.Add(item);
		return item.AddrOfPinnedObject();
	}

	internal static void FreePins()
	{
		foreach (GCHandle pin in _pins)
		{
			try
			{
				pin.Free();
			}
			catch
			{
			}
		}
		_pins.Clear();
	}

	private unsafe bool FindEffectClass(IPluginFactory factory, out PClassInfo info)
	{
		info = default;
		try
		{
			IPluginFactory2 pluginFactory = null;
			try
			{
				nint num = QueryInterface(_factoryRaw, Vst3Ids.IPluginFactory2);
				if (num != IntPtr.Zero)
				{
					pluginFactory = Vst3Com.ToManaged<IPluginFactory2>(num);
					_factory2Raw = num;
				}
			}
			catch
			{
			}
			int num2 = factory.CountClasses();
			for (int i = 0; i < num2; i++)
			{
				if (pluginFactory != null)
				{
					nint num3 = Marshal.AllocHGlobal(Marshal.SizeOf<PClassInfo2>());
					try
					{
						if (pluginFactory.GetClassInfo2(i, num3) == 0)
						{
							PClassInfo2 pClassInfo = Marshal.PtrToStructure<PClassInfo2>(num3);
							_ = pClassInfo.Category + "|" + pClassInfo.SubCategories;
							if (IsEffectCategory(pClassInfo.Category ?? "") || IsEffectCategory(pClassInfo.SubCategories ?? ""))
							{
								info.Cardinality = pClassInfo.Cardinality;
								info.Name = pClassInfo.Name;
								info.Category = pClassInfo.Category;
								byte[] array = new byte[16];
								Marshal.Copy((nint)pClassInfo.Cid, array, 0, 16);
								fixed (byte* cid = info.Cid)
								{
									Marshal.Copy(array, 0, (nint)cid, 16);
								}
								return true;
							}
						}
					}
					finally
					{
						Marshal.FreeHGlobal(num3);
					}
				}
				nint num4 = Marshal.AllocHGlobal(Marshal.SizeOf<PClassInfo>());
				try
				{
					if (factory.GetClassInfo(i, num4) == 0)
					{
						PClassInfo pClassInfo2 = Marshal.PtrToStructure<PClassInfo>(num4);
						if (IsEffectCategory(pClassInfo2.Category ?? ""))
						{
							info = pClassInfo2;
							return true;
						}
					}
				}
				finally
				{
					Marshal.FreeHGlobal(num4);
				}
			}
		}
		catch
		{
		}
		return false;
	}

	private static bool IsEffectCategory(string category)
	{
		string text = category.Trim();
		if (text.Length == 0)
		{
			return false;
		}
		if (text.Equals("Audio Module Effect", StringComparison.OrdinalIgnoreCase))
		{
			return true;
		}
		if (text.StartsWith("Fx", StringComparison.OrdinalIgnoreCase))
		{
			return true;
		}
		return false;
	}

	internal static void Db(string m)
	{
		try
		{
			if (Vst3NativeHost.DebugLog)
			{
				Console.Error.WriteLine("[vst3] " + m);
			}
		}
		catch
		{
		}
	}

	private unsafe void ApplyStereoArrangement()
	{
		try
		{
			if (_processor == null || _component == null)
			{
				return;
			}
			int num = Math.Max(1, _component.GetBusCount(0, 0));
			int num2 = Math.Max(1, _component.GetBusCount(0, 1));
			ulong[] array = new ulong[num];
			ulong[] array2 = new ulong[num2];
			for (int i = 0; i < num; i++)
			{
				array[i] = 3UL;
			}
			for (int j = 0; j < num2; j++)
			{
				array2[j] = 3UL;
			}
			fixed (ulong* inputs = array)
			{
				fixed (ulong* outputs = array2)
				{
					_processor.SetBusArrangements((nint)inputs, num, (nint)outputs, num2);
				}
			}
		}
		catch
		{
		}
	}

	public string GetBusDump()
	{
		try
		{
			if (_component == null)
			{
				return "(no component)";
			}
			StringBuilder stringBuilder = new StringBuilder();
			for (int i = 0; i < 2; i++)
			{
				int busCount = _component.GetBusCount(0, i);
				stringBuilder.Append((i == 0) ? "IN:" : "OUT:").Append(busCount);
				for (int j = 0; j < busCount; j++)
				{
					BusInfo busInfo = GetBusInfo(i, j);
					stringBuilder.Append(" [").Append(j).Append("] ch=")
						.Append(busInfo.ChannelCount)
						.Append(" type=")
						.Append(busInfo.BusType)
						.Append(" name=")
						.Append(busInfo.Name ?? "");
				}
				stringBuilder.Append('\n');
			}
			return stringBuilder.ToString();
		}
		catch (Exception ex)
		{
			return "dump err: " + ex.Message;
		}
	}

	private void DetermineChannels()
	{
		try
		{
			_numInputBusses = Math.Max(0, _component.GetBusCount(0, 0));
			_numOutputBusses = Math.Max(0, _component.GetBusCount(0, 1));
			for (int i = 0; i < _numInputBusses; i++)
			{
				BusInfo busInfo = GetBusInfo(0, i);
				if (busInfo.MediaType == 0 && busInfo.BusType == 0)
				{
					_inChannels = Math.Max(1, busInfo.ChannelCount);
					break;
				}
			}
			for (int j = 0; j < _numOutputBusses; j++)
			{
				BusInfo busInfo2 = GetBusInfo(1, j);
				if (busInfo2.MediaType == 0 && busInfo2.BusType == 0)
				{
					_outChannels = Math.Max(1, busInfo2.ChannelCount);
					break;
				}
			}
		}
		catch
		{
		}
	}

	private BusInfo GetBusInfo(int direction, int index)
	{
		nint num = Marshal.AllocHGlobal(Marshal.SizeOf<BusInfo>());
		try
		{
			_component.GetBusInfo(0, direction, index, num);
			return Marshal.PtrToStructure<BusInfo>(num);
		}
		finally
		{
			Marshal.FreeHGlobal(num);
		}
	}

	private void ActivateBuses()
	{
		try
		{
			int busCount = _component.GetBusCount(0, 0);
			for (int i = 0; i < busCount; i++)
			{
				BusInfo busInfo = GetBusInfo(0, i);
				_component.ActivateBus(0, 0, i, (busInfo.BusType == 0) ? ((byte)1) : ((byte)0));
			}
			int busCount2 = _component.GetBusCount(0, 1);
			for (int j = 0; j < busCount2; j++)
			{
				BusInfo busInfo2 = GetBusInfo(1, j);
				_component.ActivateBus(0, 1, j, (busInfo2.BusType == 0) ? ((byte)1) : ((byte)0));
			}
		}
		catch
		{
		}
	}

	public void UpdateSampleRate(double sampleRate)
	{
		if (_sampleRate == sampleRate || _disposed)
		{
			return;
		}
		_sampleRate = sampleRate;
		try
		{
			lock (_lock)
			{
				if (!_disposed && _processor != null)
				{
					ProcessSetup setup = new ProcessSetup
					{
						ProcessMode = 0,
						SymbolicSampleSize = 0,
						MaxSamplesPerBlock = _maxBlock,
						SampleRate = sampleRate
					};
					_processor.SetupProcessing(ref setup);
				}
			}
		}
		catch
		{
		}
	}

	private void FreeBuffers()
	{
		if (_inHandles != null)
		{
			for (int i = 0; i < _inHandles.Length; i++)
			{
				if (_inHandles[i].IsAllocated)
				{
					try
					{
						_inHandles[i].Free();
					}
					catch
					{
					}
				}
			}
			_inHandles = null;
		}
		if (_outHandles != null)
		{
			for (int j = 0; j < _outHandles.Length; j++)
			{
				if (_outHandles[j].IsAllocated)
				{
					try
					{
						_outHandles[j].Free();
					}
					catch
					{
					}
				}
			}
			_outHandles = null;
		}
		if (_inChannelBuffersPtr != IntPtr.Zero)
		{
			try
			{
				Marshal.FreeHGlobal(_inChannelBuffersPtr);
			}
			catch
			{
			}
			_inChannelBuffersPtr = IntPtr.Zero;
		}
		if (_outChannelBuffersPtr != IntPtr.Zero)
		{
			try
			{
				Marshal.FreeHGlobal(_outChannelBuffersPtr);
			}
			catch
			{
			}
			_outChannelBuffersPtr = IntPtr.Zero;
		}
		if (_inAudioBusBuffersPtr != IntPtr.Zero)
		{
			try
			{
				Marshal.FreeHGlobal(_inAudioBusBuffersPtr);
			}
			catch
			{
			}
			_inAudioBusBuffersPtr = IntPtr.Zero;
		}
		if (_outAudioBusBuffersPtr != IntPtr.Zero)
		{
			try
			{
				Marshal.FreeHGlobal(_outAudioBusBuffersPtr);
			}
			catch
			{
			}
			_outAudioBusBuffersPtr = IntPtr.Zero;
		}
		if (_silentHandle.IsAllocated)
		{
			try { _silentHandle.Free(); } catch {}
		}
		if (_silentChannelBuffersPtr != IntPtr.Zero)
		{
			try { Marshal.FreeHGlobal(_silentChannelBuffersPtr); } catch {}
			_silentChannelBuffersPtr = IntPtr.Zero;
		}
		if (_scratchOutHandle.IsAllocated)
		{
			try { _scratchOutHandle.Free(); } catch {}
		}
		if (_scratchOutChannelBuffersPtr != IntPtr.Zero)
		{
			try { Marshal.FreeHGlobal(_scratchOutChannelBuffersPtr); } catch {}
			_scratchOutChannelBuffersPtr = IntPtr.Zero;
		}
		_allocatedFrames = 0;
		_allocatedInChannels = 0;
		_allocatedOutChannels = 0;
		_allocatedInBusses = 0;
		_allocatedOutBusses = 0;
	}

	public bool TryProcess(float[] interleaved, int channels)
	{
		if (_disposed || !_processingEnabled || _processor == null || interleaved == null)
		{
			return false;
		}
		if (channels <= 0 || interleaved.Length == 0)
		{
			return true;
		}
		int num = interleaved.Length / channels;
		lock (_lock)
		{
			if (_disposed || _processor == null)
			{
				return false;
			}
			EnsureBuffers(num, _inChannels, _outChannels);
			int num2 = Math.Min(channels, _inChannels);
			for (int i = 0; i < _inChannels; i++)
			{
				float[] array = _inPlanar[i];
				if (i < num2)
				{
					if (channels == 1)
					{
						for (int j = 0; j < num; j++)
						{
							array[j] = interleaved[j];
						}
					}
					else
					{
						for (int k = 0; k < num; k++)
						{
							array[k] = interleaved[k * channels + i];
						}
					}
				}
				else
				{
					Array.Clear(array, 0, num);
				}
			}

			// Measure input peak level
			float pIn = 0f;
			for (int pi = 0; pi < num2; pi++)
			{
				float[] inArr = _inPlanar[pi];
				for (int pk = 0; pk < num; pk++)
				{
					float a = Math.Abs(inArr[pk]);
					if (a > pIn) pIn = a;
				}
			}
			LastInPeak = pIn;

			// Clear output planar buffers before process
			for (int oj = 0; oj < _outChannels; oj++)
			{
				Array.Clear(_outPlanar[oj], 0, num);
			}

			// Ensure main input bus SilenceFlags is 0 (has active signal)
			if (_inAudioBusBuffersPtr != IntPtr.Zero)
			{
				Marshal.WriteInt64(_inAudioBusBuffersPtr, 8, 0L);
			}

			int num4 = Math.Max(1, _numInputBusses);
			int num5 = Math.Max(1, _numOutputBusses);
			try
			{
				if (_processContextPtr != IntPtr.Zero)
				{
					bool isTransportPlaying = !_transportPaused;
					if (_transportResetCountdown > 0)
					{
						_transportResetCountdown--;
						isTransportPlaying = (_transportResetCountdown == 0 && !_transportPaused);
						if (!isTransportPlaying)
						{
							_continuousSampleTime = 0;
						}
					}

					long blockStartSamples = _continuousSampleTime;
					if (isTransportPlaying)
					{
						_continuousSampleTime += num;
					}

					double currentSr = _sampleRate > 0 ? _sampleRate : 48000.0;
					double tempo = 120.0;
					double projectTimeMusic = (blockStartSamples / currentSr) * (tempo / 60.0);
					double barPositionMusic = projectTimeMusic - (projectTimeMusic % 4.0);

					uint stateFlags = (uint)(ProcessContextStates.kSystemTimeValid |
										   ProcessContextStates.kContTimeValid |
										   ProcessContextStates.kProjectTimeMusicValid |
										   ProcessContextStates.kBarPositionValid |
										   ProcessContextStates.kTempoValid |
										   ProcessContextStates.kTimeSigValid);
					if (isTransportPlaying)
					{
						stateFlags |= (uint)ProcessContextStates.kPlaying;
					}

					var context = new Vst3ProcessContext
					{
						State = stateFlags,
						SampleRate = currentSr,
						ProjectTimeSamples = blockStartSamples,
						ContinousTimeSamples = blockStartSamples,
						SystemTime = (long)((System.Diagnostics.Stopwatch.GetTimestamp() / (double)System.Diagnostics.Stopwatch.Frequency) * 1_000_000_000.0),
						ProjectTimeMusic = projectTimeMusic,
						BarPositionMusic = barPositionMusic,
						CycleStartMusic = 0.0,
						CycleEndMusic = 4.0,
						Tempo = tempo,
						TimeSigNumerator = 4,
						TimeSigDenominator = 4,
						ChordKey = 0,
						ChordRoot = 0,
						ChordMask = 0,
						SmpteOffsetSubframes = 0,
						FrameRateFramesPerSecond = 30,
						FrameRateFlags = 0,
						SamplesToNextClock = 0
					};
					Marshal.StructureToPtr(context, _processContextPtr, fDeleteOld: false);
				}

				ProcessData data = new ProcessData
				{
					ProcessMode = 0,
					SymbolicSampleSize = 0,
					NumSamples = num,
					NumInputs = num4,
					NumOutputs = num5,
					Inputs = _inAudioBusBuffersPtr,
					Outputs = _outAudioBusBuffersPtr,
					InputParameterChanges = ((!Vst3NativeHost.UseParameterChanges) ? IntPtr.Zero : (Vst3NativeHost.HackParamPointer ? _hostContextCcw : _paramChangesCcw)),
					OutputParameterChanges = _outputParamChangesCcw,
					InputEvents = IntPtr.Zero,
					OutputEvents = IntPtr.Zero,
					ProcessContext = _processContextPtr
				};
				int num11 = _processor.Process(ref data);
				LastProcessResult = num11;
				ProcessCount++;

				if (_outputParamChanges.GetParameterCount() > 0)
				{
					var changes = _outputParamChanges.DrainChanges();
					if (_controller != null)
					{
						foreach (var (pid, val) in changes)
						{
							try { _controller.SetParamNormalized(pid, val); } catch {}
						}
					}
				}

				if (num11 != 0)
				{
					return true;
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine("[echo-vst3-host] Process exception: " + ex.Message);
				return true;
			}

			int num12 = Math.Min(channels, _outChannels);
			float pOut = 0f;
			for (int num13 = 0; num13 < num12; num13++)
			{
				float[] array2 = _outPlanar[num13];
				if (channels == 1)
				{
					for (int num14 = 0; num14 < num; num14++)
					{
						float val = array2[num14];
						interleaved[num14] = val;
						float a = Math.Abs(val);
						if (a > pOut) pOut = a;
					}
				}
				else
				{
					for (int num15 = 0; num15 < num; num15++)
					{
						float val = array2[num15];
						interleaved[num15 * channels + num13] = val;
						float a = Math.Abs(val);
						if (a > pOut) pOut = a;
					}
				}
			}
			LastOutPeak = pOut;
			return true;
		}
	}

	private void EnsureBuffers(int frames, int inCh, int outCh)
	{
		if (frames <= 0)
		{
			return;
		}
		int numInBusses = Math.Max(1, _numInputBusses);
		int numOutBusses = Math.Max(1, _numOutputBusses);
		int requiredFrames = Math.Max(frames, 8192);

		if (_allocatedFrames >= requiredFrames &&
			_allocatedInChannels == inCh &&
			_allocatedOutChannels == outCh &&
			_allocatedInBusses == numInBusses &&
			_allocatedOutBusses == numOutBusses &&
			_inAudioBusBuffersPtr != IntPtr.Zero &&
			_outAudioBusBuffersPtr != IntPtr.Zero)
		{
			return;
		}

		FreeBuffers();

		_inPlanar = new float[inCh][];
		_inHandles = new GCHandle[inCh];
		_inChannelBuffersPtr = Marshal.AllocHGlobal(IntPtr.Size * inCh);

		for (int i = 0; i < inCh; i++)
		{
			_inPlanar[i] = new float[requiredFrames];
			_inHandles[i] = GCHandle.Alloc(_inPlanar[i], GCHandleType.Pinned);
			Marshal.WriteIntPtr(_inChannelBuffersPtr, i * IntPtr.Size, _inHandles[i].AddrOfPinnedObject());
		}

		_outPlanar = new float[outCh][];
		_outHandles = new GCHandle[outCh];
		_outChannelBuffersPtr = Marshal.AllocHGlobal(IntPtr.Size * outCh);

		for (int j = 0; j < outCh; j++)
		{
			_outPlanar[j] = new float[requiredFrames];
			_outHandles[j] = GCHandle.Alloc(_outPlanar[j], GCHandleType.Pinned);
			Marshal.WriteIntPtr(_outChannelBuffersPtr, j * IntPtr.Size, _outHandles[j].AddrOfPinnedObject());
		}

		_silentBuffer = new float[requiredFrames];
		_silentHandle = GCHandle.Alloc(_silentBuffer, GCHandleType.Pinned);
		int auxInCh = Math.Max(2, inCh);
		_silentChannelBuffersPtr = Marshal.AllocHGlobal(IntPtr.Size * auxInCh);
		for (int s = 0; s < auxInCh; s++)
		{
			Marshal.WriteIntPtr(_silentChannelBuffersPtr, s * IntPtr.Size, _silentHandle.AddrOfPinnedObject());
		}

		_scratchOutBuffer = new float[requiredFrames];
		_scratchOutHandle = GCHandle.Alloc(_scratchOutBuffer, GCHandleType.Pinned);
		int auxOutCh = Math.Max(2, outCh);
		_scratchOutChannelBuffersPtr = Marshal.AllocHGlobal(IntPtr.Size * auxOutCh);
		for (int s = 0; s < auxOutCh; s++)
		{
			Marshal.WriteIntPtr(_scratchOutChannelBuffersPtr, s * IntPtr.Size, _scratchOutHandle.AddrOfPinnedObject());
		}

		int busSize = Marshal.SizeOf<AudioBusBuffers>();
		_inAudioBusBuffersPtr = Marshal.AllocHGlobal(busSize * numInBusses);
		for (int n = 0; n < numInBusses; n++)
		{
			Marshal.StructureToPtr(new AudioBusBuffers
			{
				NumChannels = (n == 0) ? inCh : 2,
				SilenceFlags = (n == 0) ? 0UL : ulong.MaxValue,
				ChannelBuffers = (n == 0) ? _inChannelBuffersPtr : _silentChannelBuffersPtr
			}, _inAudioBusBuffersPtr + n * busSize, fDeleteOld: false);
		}

		_outAudioBusBuffersPtr = Marshal.AllocHGlobal(busSize * numOutBusses);
		for (int m = 0; m < numOutBusses; m++)
		{
			Marshal.StructureToPtr(new AudioBusBuffers
			{
				NumChannels = (m == 0) ? outCh : 2,
				SilenceFlags = (m == 0) ? 0UL : ulong.MaxValue,
				ChannelBuffers = (m == 0) ? _outChannelBuffersPtr : _scratchOutChannelBuffersPtr
			}, _outAudioBusBuffersPtr + m * busSize, fDeleteOld: false);
		}

		_allocatedFrames = requiredFrames;
		_allocatedInChannels = inCh;
		_allocatedOutChannels = outCh;
		_allocatedInBusses = numInBusses;
		_allocatedOutBusses = numOutBusses;
	}

	public void QueueParameterChange(uint id, double value)
	{
		_paramChanges.SetValue(id, value);
	}

	[DllImport("user32.dll")]
	private static extern uint GetDpiForWindow(nint hWnd);

	public unsafe string? OpenEditor(nint parentHwnd, out int width, out int height)
	{
		width = 640;
		height = 480;
		if (_disposed)
		{
			return "插件已释放，无法打开界面";
		}
		if (parentHwnd == IntPtr.Zero)
		{
			return "宿主窗口句柄无效";
		}
		try
		{
			if (_view != null || _viewAttached)
			{
				CloseEditor();
			}

			if (_controller == null && _componentRaw != IntPtr.Zero)
			{
				nint ctrlPtr = QueryInterface(_componentRaw, Vst3Ids.IEditController);
				if (ctrlPtr != IntPtr.Zero)
				{
					_controllerRaw = ctrlPtr;
					_controller = Vst3Com.ToManaged<IEditController>(_controllerRaw);
					if (_hostContextCcw != IntPtr.Zero)
					{
						try { _controller.Initialize(_hostContextCcw); } catch {}
					}
					if (_hostContext != null)
					{
						try
						{
							nint handlerCcw = Vst3Com.ToNative((IComponentHandlerCom)_hostContext);
							_controller.SetComponentHandler(handlerCcw);
						}
						catch {}
					}
					try
					{
						_controllerCpRaw = QueryInterface(_controllerRaw, Vst3Ids.IConnectionPoint);
						if (_controllerCpRaw != IntPtr.Zero)
						{
							_controllerCp = Vst3Com.ToManaged<IConnectionPoint>(_controllerCpRaw);
							if (_componentCp != null)
							{
								_componentCp.Connect(_controllerCpRaw);
								_controllerCp.Connect(_componentCpRaw);
							}
						}
					}
					catch {}
				}
			}
			if (_controller == null)
			{
				return "该插件未提供编辑控制器（无法打开自带界面）";
			}

			if (_hostContextCcw == IntPtr.Zero && _hostContext != null)
			{
				try { _hostContextCcw = Vst3Com.ToNative((IHostApplicationCom)_hostContext); } catch {}
			}

			nint num = _controller.CreateView(PinBytes(Vst3Const.EditorName));
			if (num == IntPtr.Zero)
			{
				return "插件不支持编辑器视图（createView 返回空）";
			}
			_viewRaw = num;

			nint viewQi = QueryInterface(num, Vst3Ids.IPlugView);
			nint targetViewPtr = (viewQi != IntPtr.Zero) ? viewQi : num;

			try
			{
				_view = Vst3Com.ToManaged<IPlugView>(targetViewPtr);
			}
			catch (Exception exToManaged)
			{
				return "获取插件 IPlugView 失败: " + exToManaged.Message;
			}

			if (_view == null)
			{
				return "插件未返回有效 IPlugView 接口对象";
			}

			nint hwndPlatformPtr = PinBytes(Vst3Const.PlatformHwnd);
			int sup = _view.IsPlatformTypeSupported(hwndPlatformPtr);
			if (sup != 0)
			{
				return "插件界面不支持当前平台（HWND）";
			}

			ViewRect viewRect = default;
			if (_view.GetSize((nint)(&viewRect)) == 0)
			{
				width = Math.Max(64, viewRect.Right - viewRect.Left);
				height = Math.Max(48, viewRect.Bottom - viewRect.Top);
			}

			if (_hostContext != null)
			{
				try
				{
					_viewFrameCcw = Vst3Com.ToNative((IPlugFrameCom)_hostContext);
					if (_viewFrameCcw != IntPtr.Zero)
					{
						_view.SetFrame(_viewFrameCcw);
					}
				}
				catch (Exception exF)
				{
					Console.WriteLine("[echo-vst3-host] SetFrame warning: " + exF.Message);
				}
			}

			try
			{
				nint scaleQi = QueryInterface(targetViewPtr, new Guid("65ED9690-7164-4E13-8D2A-D5163FBECD9F"));
				if (scaleQi != IntPtr.Zero)
				{
					var scaleSupport = Vst3Com.ToManaged<IPlugViewContentScaleSupport>(scaleQi);
					scaleSupport?.SetContentScaleFactor(1.0f);
					Marshal.Release(scaleQi);
				}
			}
			catch {}
			sup = _view.Attached(parentHwnd, hwndPlatformPtr);
			if (sup != 0)
			{
				try { _view.Removed(); } catch {}
				return "插件界面挂载失败（attached 返回 0x" + sup.ToString("X8") + "）";
			}

			_viewAttached = true;
			_viewResizable = _view.CanResize() == 0;
			ViewRect viewRect2 = new ViewRect
			{
				Left = 0,
				Top = 0,
				Right = width,
				Bottom = height
			};
			_view.OnSize((nint)(&viewRect2));
			_view.OnFocus(1);
			return null;
		}
		catch (Exception ex)
		{
			string detail = $"{ex.GetType().Name}: {ex.Message} at {ex.TargetSite} in {ex.StackTrace}";
			Console.WriteLine("[echo-vst3-host] OpenEditor exception: " + detail);
			try { File.AppendAllText("editor_exception.log", $"{DateTime.Now}: {detail}\n"); } catch {}
			return "打开插件界面异常：" + ex.Message;
		}
	}

	public unsafe void EditorResized(int width, int height)
	{
		try
		{
			if (_view != null && _viewAttached)
			{
				ViewRect viewRect = new ViewRect
				{
					Left = 0,
					Top = 0,
					Right = width,
					Bottom = height
				};
				_view.OnSize((nint)(&viewRect));
			}
		}
		catch
		{
		}
	}

	public void CloseEditor()
	{
		if (_disposed)
		{
			return;
		}
		try
		{
			if (_view != null && _viewAttached)
			{
				try { _view.OnFocus(0); } catch {}
				try { _view.Removed(); } catch {}
				_viewAttached = false;
			}
			if (_view != null)
			{
				try { _view.SetFrame(IntPtr.Zero); } catch {}
				_view = null;
			}
			if (_viewFrameCcw != IntPtr.Zero)
			{
				try { Marshal.Release(_viewFrameCcw); } catch {}
				_viewFrameCcw = IntPtr.Zero;
			}
			if (_viewRaw != IntPtr.Zero)
			{
				try { Marshal.Release(_viewRaw); } catch {}
				_viewRaw = IntPtr.Zero;
			}
		}
		catch (Exception ex)
		{
			Console.WriteLine("[echo-vst3-host] CloseEditor warning: " + ex.Message);
		}
	}

	public void RaiseEditorResize(nint newSize)
	{
		if (newSize == IntPtr.Zero) return;
		try
		{
			ViewRect viewRect = Marshal.PtrToStructure<ViewRect>(newSize);
			int arg = Math.Max(64, viewRect.Right - viewRect.Left);
			int arg2 = Math.Max(48, viewRect.Bottom - viewRect.Top);
			EditorResizeRequested?.Invoke(arg, arg2);
		}
		catch
		{
		}
	}

	public void Dispose()
	{
		lock (_lock)
		{
			if (_disposed)
			{
				return;
			}
			_disposed = true;
			_processingEnabled = false;
			FreeBuffers();
		}
		try
		{
			CloseEditor();
		}
		catch
		{
		}
		try
		{
			if (_componentCp != null && _controllerCp != null)
			{
				_componentCp.Disconnect(_controllerCpRaw);
				_controllerCp.Disconnect(_componentCpRaw);
			}
		}
		catch
		{
		}
		try
		{
			_processor?.SetProcessing(0);
		}
		catch
		{
		}
		try
		{
			_component?.SetActive(0);
		}
		catch
		{
		}
		try
		{
			_component?.Terminate();
		}
		catch
		{
		}
		try
		{
			_controller?.Terminate();
		}
		catch
		{
		}
		if (_factory != null) { try { GC.SuppressFinalize(_factory); } catch {} }
		if (_component != null) { try { GC.SuppressFinalize(_component); } catch {} }
		if (_controller != null) { try { GC.SuppressFinalize(_controller); } catch {} }
		if (_processor != null) { try { GC.SuppressFinalize(_processor); } catch {} }
		if (_componentCp != null) { try { GC.SuppressFinalize(_componentCp); } catch {} }
		if (_controllerCp != null) { try { GC.SuppressFinalize(_controllerCp); } catch {} }
		if (_view != null) { try { GC.SuppressFinalize(_view); } catch {} }
		_factory = null;
		_component = null;
		_controller = null;
		_processor = null;
		_componentCp = null;
		_controllerCp = null;
		_view = null;
		try
		{
			if (_hostContextCcw != IntPtr.Zero)
			{
				Marshal.Release(_hostContextCcw);
			}
		}
		catch
		{
		}
		try
		{
			if (_paramChangesCcw != IntPtr.Zero)
			{
				Marshal.Release(_paramChangesCcw);
				_paramChangesCcw = IntPtr.Zero;
			}
		}
		catch
		{
		}
		try
		{
			if (_outputParamChangesCcw != IntPtr.Zero)
			{
				Marshal.Release(_outputParamChangesCcw);
				_outputParamChangesCcw = IntPtr.Zero;
			}
		}
		catch
		{
		}
		try
		{
			if (_processContextPtr != IntPtr.Zero)
			{
				Marshal.FreeHGlobal(_processContextPtr);
				_processContextPtr = IntPtr.Zero;
			}
		}
		catch
		{
		}
	}

	private static string ResolveModulePath(string pluginPath)
	{
		try
		{
			if (File.Exists(pluginPath))
			{
				return pluginPath;
			}
			if (Directory.Exists(pluginPath))
			{
				string[] array = new string[2] { "x86_64-win", "x86-win" };
				foreach (string path in array)
				{
					string path2 = System.IO.Path.Combine(pluginPath, "Contents", path);
					if (Directory.Exists(path2))
					{
						string text = Directory.EnumerateFiles(path2, "*.vst3", SearchOption.TopDirectoryOnly).Concat(Directory.EnumerateFiles(path2, "*.dll", SearchOption.TopDirectoryOnly)).FirstOrDefault();
						if (text != null)
						{
							return text;
						}
					}
				}
			}
		}
		catch
		{
		}
		return null;
	}

	public bool SyncComponentStateToController()
	{
		if (_component == null || _controller == null)
		{
			return false;
		}
		try
		{
			var stream = new Vst3MemoryStreamImpl();
			nint streamNative = Vst3Com.ToNative<IBStreamCom>(stream);
			try
			{
				int resGet = _component.GetState(streamNative);
				Console.WriteLine($"[echo-vst3-host] SyncComponentState ({DisplayName}): component.GetState returned 0x{resGet:X8}, bytes = {stream.Length}");
				if (resGet == 0 && stream.Length > 0)
				{
					stream.Seek(0, 0, IntPtr.Zero);
					int resSet = _controller.SetComponentState(streamNative);
					Console.WriteLine($"[echo-vst3-host] SyncComponentState ({DisplayName}): controller.SetComponentState returned 0x{resSet:X8}");
					return resSet == 0;
				}
			}
			finally
			{
				Marshal.Release(streamNative);
			}
		}
		catch (Exception ex)
		{
			Console.WriteLine($"[echo-vst3-host] SyncComponentState ({DisplayName}) exception: {ex.Message}");
		}
		return false;
	}
}
