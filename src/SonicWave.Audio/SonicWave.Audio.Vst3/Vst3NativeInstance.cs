using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace SonicWave.Audio.Vst3;

internal sealed class Vst3NativeInstance : IDisposable
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

	private readonly object _lock = new object();

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
			_controller?.Initialize(_hostContextCcw);
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
			DetermineChannels();
			if (Vst3NativeHost.UseStereoArrangement)
			{
				ApplyStereoArrangement();
			}
			try
			{
				if (_componentCp != null && _controllerCp != null)
				{
					_componentCp.Connect(_controllerCpRaw);
					_controllerCp.Connect(_componentCpRaw);
					Db("connection points connected");
				}
			}
			catch (Exception ex)
			{
				Db("connect failed: " + ex.Message);
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
			EnsureBuffers(_maxBlock, _inChannels, _outChannels);
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
			uint[] array = new uint[num];
			uint[] array2 = new uint[num2];
			for (int i = 0; i < num; i++)
			{
				array[i] = 3u;
			}
			for (int j = 0; j < num2; j++)
			{
				array2[j] = 3u;
			}
			fixed (uint* inputs = array)
			{
				fixed (uint* outputs = array2)
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

			int num4 = Math.Max(1, _numInputBusses);
			int num5 = Math.Max(1, _numOutputBusses);
			try
			{
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
					OutputParameterChanges = IntPtr.Zero,
					InputEvents = IntPtr.Zero,
					OutputEvents = IntPtr.Zero,
					ProcessContext = IntPtr.Zero
				};
				int num11 = _processor.Process(ref data);
				if (num11 != 0)
				{
					return true;
				}
			}
			catch
			{
				return true;
			}

			int num12 = Math.Min(channels, _outChannels);
			for (int num13 = 0; num13 < num12; num13++)
			{
				float[] array2 = _outPlanar[num13];
				if (channels == 1)
				{
					for (int num14 = 0; num14 < num; num14++)
					{
						interleaved[num14] = array2[num14];
					}
				}
				else
				{
					for (int num15 = 0; num15 < num; num15++)
					{
						interleaved[num15 * channels + num13] = array2[num15];
					}
				}
			}
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

		int busSize = Marshal.SizeOf<AudioBusBuffers>();
		_inAudioBusBuffersPtr = Marshal.AllocHGlobal(busSize * numInBusses);
		for (int n = 0; n < numInBusses; n++)
		{
			Marshal.StructureToPtr(new AudioBusBuffers
			{
				NumChannels = inCh,
				SilenceFlags = (ulong)((n == 0) ? 0 : ((1L << inCh) - 1)),
				ChannelBuffers = _inChannelBuffersPtr
			}, _inAudioBusBuffersPtr + n * busSize, fDeleteOld: false);
		}

		_outAudioBusBuffersPtr = Marshal.AllocHGlobal(busSize * numOutBusses);
		for (int m = 0; m < numOutBusses; m++)
		{
			Marshal.StructureToPtr(new AudioBusBuffers
			{
				NumChannels = outCh,
				SilenceFlags = (ulong)((m == 0) ? 0 : ((1L << outCh) - 1)),
				ChannelBuffers = _outChannelBuffersPtr
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

	public unsafe string? OpenEditor(nint parentHwnd, out int width, out int height)
	{
		width = 640;
		height = 480;
		if (_disposed)
		{
			return "插件已释放，无法打开界面";
		}
		try
		{
			if (_controller == null)
			{
				return "该插件未提供编辑控制器（无法打开自带界面）";
			}
			nint num = _controller.CreateView(PinBytes(Vst3Const.EditorName));
			if (num == IntPtr.Zero)
			{
				return "插件不支持编辑器视图（createView 返回空）";
			}
			_viewRaw = num;
			_view = Vst3Com.ToManaged<IPlugView>(num);
			int num2 = _view.IsPlatformTypeSupported(PinBytes(Vst3Const.PlatformHwnd));
			if (num2 != 0 && num2 != 0)
			{
				return "插件界面不支持当前平台（HWND）";
			}
			ViewRect viewRect = default;
			if (_view.GetSize((nint)(&viewRect)) == 0)
			{
				width = Math.Max(64, viewRect.Right - viewRect.Left);
				height = Math.Max(48, viewRect.Bottom - viewRect.Top);
			}
			_viewFrameCcw = Vst3Com.ToNative((IPlugFrameCom)_hostContext);
			_view.SetFrame(_viewFrameCcw);
			num2 = _view.Attached(parentHwnd, PinBytes(Vst3Const.PlatformHwnd));
			if (num2 != 0 && num2 != 0)
			{
				_view.Removed();
				return "插件界面挂载失败（attached 返回 0x" + num2.ToString("X8") + "）";
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
				_view.OnFocus(0);
				_view.Removed();
				_viewAttached = false;
			}
			if (_viewFrameCcw != IntPtr.Zero)
			{
				Marshal.Release(_viewFrameCcw);
				_viewFrameCcw = IntPtr.Zero;
			}
			if (_viewRaw != IntPtr.Zero)
			{
				try
				{
					Marshal.Release(_viewRaw);
				}
				catch
				{
				}
				_viewRaw = IntPtr.Zero;
			}
			_view = null;
		}
		catch
		{
		}
	}

	public void RaiseEditorResize(nint newSize)
	{
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
		try
		{
			if (_controllerCpRaw != IntPtr.Zero)
			{
				Marshal.Release(_controllerCpRaw);
			}
		}
		catch
		{
		}
		try
		{
			if (_componentCpRaw != IntPtr.Zero)
			{
				Marshal.Release(_componentCpRaw);
			}
		}
		catch
		{
		}
		try
		{
			if (_processorRaw != IntPtr.Zero)
			{
				Marshal.Release(_processorRaw);
			}
		}
		catch
		{
		}
		try
		{
			if (_controllerRaw != IntPtr.Zero)
			{
				Marshal.Release(_controllerRaw);
			}
		}
		catch
		{
		}
		try
		{
			if (_componentRaw != IntPtr.Zero)
			{
				Marshal.Release(_componentRaw);
			}
		}
		catch
		{
		}
		try
		{
			if (_factory2Raw != IntPtr.Zero)
			{
				Marshal.Release(_factory2Raw);
			}
		}
		catch
		{
		}
		try
		{
			if (_factoryRaw != IntPtr.Zero)
			{
				Marshal.Release(_factoryRaw);
			}
		}
		catch
		{
		}
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
}
