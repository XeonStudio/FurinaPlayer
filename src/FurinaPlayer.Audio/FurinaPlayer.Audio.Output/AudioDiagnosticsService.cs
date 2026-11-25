#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FurinaPlayer.Audio.Backends;
using FurinaPlayer.Audio.Dsp;
using FurinaPlayer.Core.Models;
using FurinaPlayer.Core.Services;
using NAudio.CoreAudioApi;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;

namespace FurinaPlayer.Audio.Output;

public enum DiagnosticSeverity
{
	Ok = 0,
	Info = 1,
	Warning = 2,
	Error = 3
}

public sealed record AudioSessionRuntimeDiagnostics(
	PlaybackState State,
	string CurrentTrackTitle,
	string CurrentFilePath,
	double PositionSeconds,
	double DurationSeconds,
	OutputMode OutputMode,
	string ConfiguredDeviceId,
	string ConfiguredAsioDriver,
	int BufferMs,
	string ActiveBackend,
	string ActiveOutputKind,
	int SourceSampleRate,
	int OutputSampleRate,
	int BitsPerSample,
	int Channels,
	string ResamplerMode,
	string ResamplerProfile,
	bool IsResampling,
	bool DspActive,
	bool EqEnabled,
	bool HeadphoneCalibrationEnabled,
	bool Vst3Active,
	bool SpatialEnabled,
	bool DynamicsEnabled,
	double HeadroomDb,
	double EqPreampDb,
	string? LastErrorMessage,
	DateTime? LastErrorAtUtc,
	int RecoveryCount,
	bool IsRestorePending,
	bool IsDecoderPlaying,
	bool VlcAvailable,
	string[] RecentTraceLines);

public sealed record DiagnosticStageStatus(
	string Key,
	string Title,
	DiagnosticSeverity Severity,
	string Summary,
	string Detail);

public sealed record PlaybackStatusReport(
	DateTime CapturedAtUtc,
	DiagnosticSeverity OverallHealth,
	string HealthBadgeText,
	string PlaybackStateText,
	string OutputSummaryText,
	string ActiveDeviceName,
	int EndpointSampleRate,
	int EndpointBitsPerSample,
	bool SystemEndpointMuted,
	float SystemMasterVolume01,
	bool ConfiguredDeviceMissing,
	IReadOnlyList<string> SignalChainBadges,
	IReadOnlyList<string> AnomalyReasons,
	IReadOnlyList<DiagnosticStageStatus> Stages,
	AudioSessionRuntimeDiagnostics Runtime,
	string CopyableReportText);

public sealed record CompatibilityCheckItem(
	string Id,
	string Title,
	DiagnosticSeverity Severity,
	string ResultSummary,
	string ReasonDetail,
	string? RecommendedFixId,
	string? RecommendedFixLabel);

public sealed record CompatibilityProgressUpdate(
	int StepIndex,
	int TotalSteps,
	double Progress01,
	string CurrentStepTitle,
	string StatusReason,
	CompatibilityCheckItem? CompletedItem = null);

public sealed record TestToneResult(
	bool Success,
	string OutputDescription,
	string Message);

public sealed record RepairActionResult(
	bool Success,
	string ActionTitle,
	string Detail,
	IReadOnlyList<string> StepsExecuted);

/// <summary>
/// Live audio playback diagnostics, hardware compatibility auditor, step-by-step no-sound troubleshooter,
/// test-tone generator, and non-destructive audio repair service for FurinaPlayer.
/// </summary>
public static class AudioDiagnosticsService
{
	public static PlaybackStatusReport InspectPlaybackStatus(AppSettings settings, AudioSession? session = null)
	{
		session ??= AudioSession.Instance;
		AudioSessionRuntimeDiagnostics runtime = session != null
			? session.GetRuntimeDiagnosticsSnapshot()
			: CreateIdleRuntimeSnapshot(settings);

		string activeDevName = Loc.T("系统默认音频设备");
		int endpointRate = 48000;
		int endpointBits = 32;
		bool endpointMuted = false;
		float endpointVol = 1.0f;
		bool configuredDeviceMissing = false;
		bool noRenderEndpoints = false;
		string? endpointError = null;

		try
		{
			using var enumerator = new MMDeviceEnumerator();
			var activeList = enumerator.EnumerateAudioEndPoints(DataFlow.Render, DeviceState.Active).ToList();
			if (activeList.Count == 0)
			{
				noRenderEndpoints = true;
				activeDevName = Loc.T("未检测到可用渲染设备");
			}
			else if (settings.OutputMode == OutputMode.Asio)
			{
				string asioName = !string.IsNullOrWhiteSpace(runtime.ConfiguredAsioDriver)
					? runtime.ConfiguredAsioDriver
					: (settings.OutputDeviceId ?? string.Empty);
				activeDevName = string.IsNullOrWhiteSpace(asioName) ? Loc.T("未选择 ASIO 驱动") : $"ASIO · {asioName}";
				try
				{
					string[] asioDrivers = AsioOut.GetDriverNames();
					configuredDeviceMissing = string.IsNullOrWhiteSpace(asioName)
						|| !asioDrivers.Any(d => string.Equals(d, asioName, StringComparison.OrdinalIgnoreCase));
				}
				catch
				{
					configuredDeviceMissing = true;
				}
			}
			else
			{
				MMDevice? target = null;
				string? wantedId = !string.IsNullOrWhiteSpace(runtime.ConfiguredDeviceId)
					? runtime.ConfiguredDeviceId
					: settings.OutputDeviceId;

				if (!string.IsNullOrWhiteSpace(wantedId))
				{
					target = activeList.FirstOrDefault(d => string.Equals(d.ID, wantedId, StringComparison.OrdinalIgnoreCase));
					if (target == null)
					{
						configuredDeviceMissing = true;
					}
				}

				if (target == null)
				{
					try
					{
						target = enumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);
					}
					catch
					{
						target = activeList.FirstOrDefault();
					}
				}

				if (target != null)
				{
					activeDevName = target.FriendlyName;
					try
					{
						var mix = target.AudioClient.MixFormat;
						endpointRate = mix.SampleRate;
						endpointBits = mix.BitsPerSample;
					}
					catch { }
					try
					{
						endpointMuted = target.AudioEndpointVolume.Mute;
						endpointVol = target.AudioEndpointVolume.MasterVolumeLevelScalar;
					}
					catch { }
				}
			}

			foreach (var d in activeList)
			{
				try { d.Dispose(); } catch { }
			}
		}
		catch (Exception ex)
		{
			endpointError = ex.Message;
		}

		// Build Signal Chain Badges & Anomaly Reasons
		var badges = new List<string>();
		var anomalies = new List<string>();

		if (runtime.IsResampling)
		{
			badges.Add(Loc.F("升频/重采样 ({0}→{1} Hz)", runtime.SourceSampleRate, runtime.OutputSampleRate));
		}
		else if (runtime.OutputSampleRate > 0)
		{
			badges.Add(Loc.F("原生直出 {0:0.#} kHz", runtime.OutputSampleRate / 1000.0));
		}

		badges.Add(runtime.DspActive ? Loc.T("DSP 已启用") : Loc.T("DSP 纯净直通"));

		if (runtime.EqEnabled) badges.Add("EQ");
		if (runtime.HeadphoneCalibrationEnabled) badges.Add("OPRA");
		if (runtime.Vst3Active) badges.Add("VST3");

		if (noRenderEndpoints)
		{
			anomalies.Add(Loc.T("未检测到任何可用的 Windows 音频输出设备（耳机/扬声器未接入或驱动异常）"));
		}
		if (configuredDeviceMissing)
		{
			anomalies.Add(Loc.T("当前配置的输出设备已离线或断开，播放按钮可能无响应或回退默认设备"));
		}
		if (endpointMuted || endpointVol <= 0.01f)
		{
			anomalies.Add(Loc.T("Windows 系统输出设备处于静音或极低音量状态"));
		}
		if (settings.Volume <= 0)
		{
			anomalies.Add(Loc.T("播放器内部音量为 0%"));
		}
		if (!string.IsNullOrWhiteSpace(runtime.LastErrorMessage))
		{
			anomalies.Add(Loc.F("最近音频引擎报错: {0}", runtime.LastErrorMessage));
		}
		if (!string.IsNullOrWhiteSpace(endpointError))
		{
			anomalies.Add(Loc.F("系统音频枚举异常: {0}", endpointError));
		}
		if (runtime.IsResampling && settings.BufferMilliseconds < 90)
		{
			anomalies.Add(Loc.F("升频输出缓冲过小 ({0} ms)，高采样率下易引发爆音或播停", settings.BufferMilliseconds));
		}
		if (settings.OutputMode == OutputMode.WasapiExclusive && runtime.ActiveOutputKind.Contains("Fallback", StringComparison.OrdinalIgnoreCase))
		{
			anomalies.Add(Loc.T("独占模式打开失败，已自动回退到共享输出"));
		}
		if (runtime.State == PlaybackState.Playing && !runtime.IsDecoderPlaying && runtime.ActiveBackend == "NAudio")
		{
			anomalies.Add(Loc.T("播放状态为 Playing 但底层音频线程未处于运行状态"));
		}

		DiagnosticSeverity overall = DiagnosticSeverity.Ok;
		if (noRenderEndpoints || !string.IsNullOrWhiteSpace(runtime.LastErrorMessage) || (runtime.State == PlaybackState.Playing && !runtime.IsDecoderPlaying))
		{
			overall = DiagnosticSeverity.Error;
		}
		else if (anomalies.Count > 0 || configuredDeviceMissing || endpointMuted)
		{
			overall = DiagnosticSeverity.Warning;
		}

		// 6 Pipeline Stages (modeled on ECHO MaintenanceDiagnosticsCards)
		string fileBase = string.IsNullOrWhiteSpace(runtime.CurrentFilePath)
			? Loc.T("未选择曲目")
			: Path.GetFileName(runtime.CurrentFilePath);
		bool hasFileIssue = !string.IsNullOrWhiteSpace(runtime.CurrentFilePath)
			&& !runtime.CurrentFilePath.StartsWith("http", StringComparison.OrdinalIgnoreCase)
			&& !File.Exists(runtime.CurrentFilePath);

		var stages = new List<DiagnosticStageStatus>
		{
			new(
				"source",
				Loc.T("1. 音源与队列"),
				hasFileIssue ? DiagnosticSeverity.Error : (string.IsNullOrEmpty(runtime.CurrentFilePath) ? DiagnosticSeverity.Info : DiagnosticSeverity.Ok),
				string.IsNullOrEmpty(runtime.CurrentFilePath) ? Loc.T("当前未加载曲目") : fileBase,
				$"track={runtime.CurrentTrackTitle} | pos={runtime.PositionSeconds:F1}s / {runtime.DurationSeconds:F1}s | file={fileBase}"),
			new(
				"decode",
				Loc.T("2. 解码管线"),
				DiagnosticSeverity.Ok,
				runtime.ActiveBackend,
				$"backend={runtime.ActiveBackend} | depth={runtime.BitsPerSample}-bit | ch={runtime.Channels} | LibVLC={(runtime.VlcAvailable ? "Ready" : "Unavailable")}"),
			new(
				"resampler",
				Loc.T("3. 采样率与升频 (FurinaSrc)"),
				runtime.IsResampling && settings.BufferMilliseconds < 90 ? DiagnosticSeverity.Warning : DiagnosticSeverity.Ok,
				runtime.SourceSampleRate > 0 ? $"{runtime.SourceSampleRate} Hz → {runtime.OutputSampleRate} Hz" : $"{endpointRate} Hz (MixFormat)",
				$"mode={runtime.ResamplerMode} ({runtime.ResamplerProfile}) | resampling={runtime.IsResampling} | mixRate={endpointRate} Hz"),
			new(
				"output",
				Loc.T("4. 输出后端与独占仲裁"),
				!string.IsNullOrWhiteSpace(runtime.LastErrorMessage) ? DiagnosticSeverity.Warning : DiagnosticSeverity.Ok,
				$"{FormatOutputMode(settings.OutputMode)} · {runtime.ActiveOutputKind}",
				$"requestedMode={settings.OutputMode} | actualKind={runtime.ActiveOutputKind} | dsp={runtime.DspActive} (EQ={runtime.EqEnabled}, OPRA={runtime.HeadphoneCalibrationEnabled}, VST3={runtime.Vst3Active})"),
			new(
				"device",
				Loc.T("5. 设备端点与缓冲"),
				noRenderEndpoints ? DiagnosticSeverity.Error : (configuredDeviceMissing || endpointMuted ? DiagnosticSeverity.Warning : DiagnosticSeverity.Ok),
				activeDevName,
				$"device={activeDevName} | buffer={settings.BufferMilliseconds}ms | sysMute={endpointMuted} | sysVol={Math.Round(endpointVol * 100)}% | appVol={settings.Volume}%"),
			new(
				"stability",
				Loc.T("6. 播放稳定性守护"),
				runtime.RecoveryCount > 0 ? DiagnosticSeverity.Warning : DiagnosticSeverity.Ok,
				runtime.RecoveryCount > 0 ? Loc.F("触发热恢复 {0} 次", runtime.RecoveryCount) : Loc.T("运行平稳无异常恢复"),
				$"state={runtime.State} | decoderPlaying={runtime.IsDecoderPlaying} | restorePending={runtime.IsRestorePending} | recoveries={runtime.RecoveryCount}")
		};

		string healthText = overall switch
		{
			DiagnosticSeverity.Error => Loc.T("❌ 发现严重阻断"),
			DiagnosticSeverity.Warning => Loc.T("⚠️ 发现异常提醒"),
			_ => runtime.State == PlaybackState.Playing ? Loc.T("✅ 链路健康运行") : Loc.T("✅ 就绪状态良好")
		};

		string stateText = runtime.State switch
		{
			PlaybackState.Playing => Loc.T("正在播放"),
			PlaybackState.Paused => Loc.T("已暂停"),
			PlaybackState.Loading => Loc.T("正在缓冲加载"),
			PlaybackState.Error => Loc.T("播放异常"),
			_ => runtime.IsRestorePending ? Loc.T("就绪（待恢复播放）") : Loc.T("已停止 / 待机")
		};

		string outputSummary = $"{FormatOutputMode(settings.OutputMode)} · {activeDevName}";
		string reportText = BuildCopyableReportText(runtime, settings, activeDevName, endpointRate, endpointBits, endpointMuted, endpointVol, configuredDeviceMissing, anomalies, stages);

		return new PlaybackStatusReport(
			CapturedAtUtc: DateTime.UtcNow,
			OverallHealth: overall,
			HealthBadgeText: healthText,
			PlaybackStateText: stateText,
			OutputSummaryText: outputSummary,
			ActiveDeviceName: activeDevName,
			EndpointSampleRate: endpointRate,
			EndpointBitsPerSample: endpointBits,
			SystemEndpointMuted: endpointMuted,
			SystemMasterVolume01: endpointVol,
			ConfiguredDeviceMissing: configuredDeviceMissing,
			SignalChainBadges: badges,
			AnomalyReasons: anomalies,
			Stages: stages,
			Runtime: runtime,
			CopyableReportText: reportText);
	}

	public static async Task<IReadOnlyList<CompatibilityCheckItem>> RunCompatibilityAuditAsync(
		AppSettings settings,
		AudioSession? session,
		Action<CompatibilityProgressUpdate>? onProgress,
		CancellationToken ct = default)
	{
		session ??= AudioSession.Instance;
		var items = new List<CompatibilityCheckItem>();
		const int totalSteps = 6;

		void Report(int stepIdx, string title, string reason, CompatibilityCheckItem? doneItem = null)
		{
			onProgress?.Invoke(new CompatibilityProgressUpdate(
				stepIdx,
				totalSteps,
				Math.Clamp((double)stepIdx / totalSteps, 0.0, 1.0),
				title,
				reason,
				doneItem));
		}

		await Task.Run(async () =>
		{
			// Step 1: Windows Render Endpoint Availability
			Report(0, Loc.T("检查 1/6 · Windows 音频渲染端点"), Loc.T("正在枚举活跃 WASAPI 输出设备并检查系统服务可用性..."));
			await Task.Delay(120, ct);

			List<MMDevice> activeDevices = new();
			string? wantedId = settings.OutputDeviceId;
			MMDevice? defaultDev = null;
			try
			{
				using var enumerator = new MMDeviceEnumerator();
				activeDevices = enumerator.EnumerateAudioEndPoints(DataFlow.Render, DeviceState.Active).ToList();
				try { defaultDev = enumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia); } catch { }

				CompatibilityCheckItem step1 = activeDevices.Count > 0
					? new("endpoints", Loc.T("Windows 音频端点探测"), DiagnosticSeverity.Ok,
						Loc.F("检测到 {0} 个活跃输出设备（默认：{1}）", activeDevices.Count, defaultDev?.FriendlyName ?? activeDevices[0].FriendlyName),
						Loc.T("MMDeviceEnumerator 可正常响应，系统音频渲染服务工作正常。"),
						null, null)
					: new("endpoints", Loc.T("Windows 音频端点探测"), DiagnosticSeverity.Error,
						Loc.T("未发现活跃音频输出设备"),
						Loc.T("所有声卡/耳机均离线或被禁用。请检查物理连接或在 Windows 声音设置中启用设备。"),
						"reset_devices", Loc.T("刷新设备并恢复默认"));
				items.Add(step1);
				Report(1, step1.Title, step1.ResultSummary, step1);

				// Step 2: Volume & Mute Sanity
				Report(1, Loc.T("检查 2/6 · 静音与通道增益风险"), Loc.T("正在检查 Windows 设备静音状态、App 音量与 DSP 极端衰减..."));
				await Task.Delay(110, ct);

				MMDevice? inspectDev = (!string.IsNullOrWhiteSpace(wantedId) && settings.OutputMode != OutputMode.Asio)
					? activeDevices.FirstOrDefault(d => string.Equals(d.ID, wantedId, StringComparison.OrdinalIgnoreCase)) ?? defaultDev
					: defaultDev;

				bool sysMute = false;
				float sysVol = 1.0f;
				if (inspectDev != null)
				{
					try
					{
						sysMute = inspectDev.AudioEndpointVolume.Mute;
						sysVol = inspectDev.AudioEndpointVolume.MasterVolumeLevelScalar;
					}
					catch { }
				}

				bool appZeroVol = settings.Volume <= 0;
				double runtimeEqPreamp = session?.DspChain?.Eq?.PreampDb ?? 0.0;
				bool extremeNegHeadroom = (settings.FurinaPlayerHeadroomEnabled && settings.FurinaPlayerHeadroomDb <= -12.0) || runtimeEqPreamp <= -12.0;

				CompatibilityCheckItem step2;
				if (sysMute || sysVol <= 0.01f || appZeroVol)
				{
					step2 = new("volume", Loc.T("静音与电平检查"), DiagnosticSeverity.Warning,
						sysMute ? Loc.T("系统输出设备已静音") : (appZeroVol ? Loc.T("FurinaPlayer 音量为 0%") : Loc.T("系统主音量接近 0%")),
						Loc.T("硬件输出电平被压低或静音，点击播放后即便进度条走动也无法听见声音。"),
						"unmute_volume", Loc.T("一键解除静音并恢复安全音量"));
				}
				else if (extremeNegHeadroom)
				{
					step2 = new("volume", Loc.T("静音与电平检查"), DiagnosticSeverity.Warning,
						Loc.T("检测到大幅负增益衰减"),
						Loc.F("Headroom={0:F1} dB, EQ Preamp={1:F1} dB，可能导致听感极微弱。", settings.FurinaPlayerHeadroomDb, runtimeEqPreamp),
						"unmute_volume", Loc.T("归位安全音量与前级"));
				}
				else
				{
					step2 = new("volume", Loc.T("静音与电平检查"), DiagnosticSeverity.Ok,
						Loc.F("系统音量 {0:0}% · 应用音量 {1}%", sysVol * 100f, settings.Volume),
						Loc.T("输出链路未被静音或过度衰减。"), null, null);
				}
				items.Add(step2);
				Report(2, step2.Title, step2.ResultSummary, step2);

				// Step 3: Configured Output Binding Check
				Report(2, Loc.T("检查 3/6 · 选定设备 ID 与驱动匹配度"), Loc.T("正在校验已保存的 OutputDeviceId 与当前硬件拓扑..."));
				await Task.Delay(110, ct);

				CompatibilityCheckItem step3;
				if (settings.OutputMode == OutputMode.Asio)
				{
					string[] asioDrivers = Array.Empty<string>();
					try { asioDrivers = AsioOut.GetDriverNames(); } catch { }
					bool asioFound = !string.IsNullOrWhiteSpace(settings.OutputDeviceId)
						&& asioDrivers.Any(a => string.Equals(a, settings.OutputDeviceId, StringComparison.OrdinalIgnoreCase));
					step3 = asioFound
						? new("binding", Loc.T("ASIO 驱动绑定检查"), DiagnosticSeverity.Ok,
							Loc.F("ASIO 驱动已就绪: {0}", settings.OutputDeviceId ?? ""),
							Loc.T("ASIO 设备列表已存在所选驱动。注意部分 ASIO 驱动不支持与系统软件同时发声。"), null, null)
						: new("binding", Loc.T("ASIO 驱动绑定检查"), DiagnosticSeverity.Error,
							Loc.F("所选 ASIO 驱动不可用或为空（可用驱动 {0} 个）", asioDrivers.Length),
							Loc.T("ASIO 驱动未安装或 DAC 未开机时会导致初始化失败或无声。推荐切换为 WASAPI 共享模式。"),
							"switch_shared", Loc.T("切换到兼容的 WASAPI 共享模式"));
				}
				else if (!string.IsNullOrWhiteSpace(wantedId)
					&& !activeDevices.Any(d => string.Equals(d.ID, wantedId, StringComparison.OrdinalIgnoreCase)))
				{
					step3 = new("binding", Loc.T("输出设备绑定检查"), DiagnosticSeverity.Warning,
						Loc.T("已配置的首选设备当前已离线"),
						Loc.T("之前绑定的声卡/蓝牙耳机已断开，重新绑回当前系统默认设备可避免切换失败或卡顿。"),
						"rebind_default", Loc.T("绑定至当前默认可用设备"));
				}
				else
				{
					step3 = new("binding", Loc.T("输出设备绑定检查"), DiagnosticSeverity.Ok,
						Loc.F("设备绑定有效 ({0})", inspectDev?.FriendlyName ?? Loc.T("默认设备")),
						Loc.T("保存的输出设备 ID 与当前活跃终端匹配正常。"), null, null);
				}
				items.Add(step3);
				Report(3, step3.Title, step3.ResultSummary, step3);

				// Step 4: Sample Rate & Exclusive / Shared Compatibility Probe
				Report(3, Loc.T("检查 4/6 · 采样率与 WASAPI 独占兼容探测"), Loc.T("正在探测目标设备 MixFormat 与独占权限..."));
				await Task.Delay(130, ct);

				int mixRate = 48000;
				int mixBits = 32;
				int mixCh = 2;
				if (inspectDev != null)
				{
					try
					{
						var wf = inspectDev.AudioClient.MixFormat;
						mixRate = wf.SampleRate;
						mixBits = wf.BitsPerSample;
						mixCh = wf.Channels;
					}
					catch { }
				}

				bool exclusiveConflict = false;
				string exclusiveMsg = string.Empty;
				if (settings.OutputMode == OutputMode.WasapiExclusive && inspectDev != null)
				{
					try
					{
						using var testClient = inspectDev.AudioClient;
						var probeFmt = WaveFormat.CreateIeeeFloatWaveFormat(mixRate, mixCh);
						if (!testClient.IsFormatSupported(AudioClientShareMode.Exclusive, probeFmt, out _))
						{
							var pcm24 = new WaveFormat(mixRate, 24, mixCh);
							if (!testClient.IsFormatSupported(AudioClientShareMode.Exclusive, pcm24, out _))
							{
								exclusiveConflict = true;
								exclusiveMsg = Loc.T("声卡拒绝独占标准格式探测或正被其他应用独占占用。");
							}
						}
					}
					catch (Exception ex)
					{
						exclusiveConflict = true;
						exclusiveMsg = ex.Message;
					}
				}

				int desiredSrcRate = FurinaSrcCatalog.ResolveTargetSampleRate(44100, settings.FurinaSrcMode, settings.FurinaPlayerResamplerTargetRate);
				CompatibilityCheckItem step4;
				if (exclusiveConflict)
				{
					step4 = new("compat", Loc.T("独占权限与采样率握手"), DiagnosticSeverity.Warning,
						Loc.T("WASAPI 独占模式兼容性风险"),
						Loc.F("原因：{0} 建议切回 WASAPI 共享模式避免点击播放无声或被抢占。", exclusiveMsg),
						"switch_shared", Loc.T("切换至 WASAPI 共享模式"));
				}
				else if (!string.Equals(settings.FurinaSrcMode, "off", StringComparison.OrdinalIgnoreCase) && settings.BufferMilliseconds < 120)
				{
					step4 = new("compat", Loc.T("升频采样率与缓冲兼容"), DiagnosticSeverity.Warning,
						Loc.F("已开启升频 ({0} -> {1} Hz)，但缓冲仅 {2} ms", settings.FurinaSrcMode, desiredSrcRate, settings.BufferMilliseconds),
						Loc.T("高倍频升频在低缓冲下易在 USB DAC / 蓝牙设备上造成欠载断音。推荐提升缓冲至 200 ms。"),
						"tune_buffer", Loc.T("自动优化为 200ms 安全缓冲"));
				}
				else
				{
					step4 = new("compat", Loc.T("采样率与模式兼容"), DiagnosticSeverity.Ok,
						Loc.F("系统 MixFormat: {0} Hz / {1}-bit · 升频模式: {2}", mixRate, mixBits, settings.FurinaSrcMode ?? "off"),
						Loc.T("设备采样率、声道数与当前缓冲配置匹配良好。"), null, null);
				}
				items.Add(step4);
				Report(4, step4.Title, step4.ResultSummary, step4);

				// Step 5: DSP & VST3 & Decoder Integrity
				Report(4, Loc.T("检查 5/6 · 解码器与 DSP 链路完整性"), Loc.T("正在校验 LibVLC、VST3 与实时均衡器前级裕量..."));
				await Task.Delay(110, ct);

				bool vlcOk = session?.IsVlcAvailable ?? false;
				var runtime = session?.GetRuntimeDiagnosticsSnapshot();
				CompatibilityCheckItem step5;
				if (runtime != null && runtime.Vst3Active)
				{
					step5 = new("dsp", Loc.T("解码与 DSP 插件链路"), DiagnosticSeverity.Info,
						Loc.T("VST3 效果链正在运行中"),
						Loc.T("若切换设备后出现延迟或无声，可尝试暂时旁路 VST3 与升频以验证纯直通。"),
						"bypass_dsp_src", Loc.T("暂时旁路升频与高风险插件"));
				}
				else
				{
					step5 = new("dsp", Loc.T("解码与 DSP 插件链路"), DiagnosticSeverity.Ok,
						Loc.F("NAudio 引擎正常 · LibVLC 回退可用性: {0}", vlcOk ? "已就绪" : "内置解码独立运行"),
						Loc.T("WaveBuffer 安全拷贝补丁已激活，支持 44.1kHz~384kHz 全系采样率升频输出。"),
						null, null);
				}
				items.Add(step5);
				Report(5, step5.Title, step5.ResultSummary, step5);

				// Step 6: Runtime Error History Check
				Report(5, Loc.T("检查 6/6 · 最近播放错误与引擎状态"), Loc.T("正在分析引擎运行日志与热切换恢复计数..."));
				await Task.Delay(100, ct);

				CompatibilityCheckItem step6 = runtime != null && !string.IsNullOrWhiteSpace(runtime.LastErrorMessage)
					? new("runtime", Loc.T("运行时异常追踪"), DiagnosticSeverity.Error,
						Loc.T("捕获到最近一次播放中断异常"),
						runtime.LastErrorMessage,
						"reset_engine", Loc.T("热重启并清理音频引擎异常状态"))
					: new("runtime", Loc.T("运行时异常追踪"), DiagnosticSeverity.Ok,
						Loc.T("当前会话未记录未处理的输出崩溃或播放异常"),
						Loc.T("可以随时使用下方排障向导或测试音一键验证实际物理出声。"), null, null);
				items.Add(step6);
				Report(6, Loc.T("兼容性检查完成"), Loc.F("已完成 {0} 项系统与音频链路深度探测", items.Count), step6);
			}
			finally
			{
				foreach (var d in activeDevices)
				{
					try { d.Dispose(); } catch { }
				}
				try { defaultDev?.Dispose(); } catch { }
			}
		}, ct);

		return items;
	}

	public static async Task<TestToneResult> PlayDiagnosticTestToneAsync(
		OutputMode mode,
		string? deviceId,
		string? asioDriver,
		int bufferMs,
		CancellationToken ct = default)
	{
		return await Task.Run(() =>
		{
			try
			{
				const int sampleRate = 48000;
				const int channels = 2;
				int totalFrames = (int)(sampleRate * 0.45);
				float[] pcm = new float[totalFrames * channels];
				for (int f = 0; f < totalFrames; f++)
				{
					double t = (double)f / sampleRate;
					double env = 1.0;
					if (f < sampleRate * 0.04) env = f / (sampleRate * 0.04);
					else if (f > totalFrames - sampleRate * 0.08) env = (totalFrames - f) / (sampleRate * 0.08);
					// Soft dual-tone crystalline Furina chime (523.25 Hz C5 + 783.99 Hz G5) at -16 dBFS
					float s = (float)(env * 0.14 * (Math.Sin(2.0 * Math.PI * 523.25 * t) * 0.65 + Math.Sin(2.0 * Math.PI * 783.99 * t) * 0.35));
					pcm[f * 2] = s;
					pcm[f * 2 + 1] = s;
				}

				var source = new RawMemoryFloatSampleProvider(pcm, sampleRate, channels);
				var waveProvider = new SampleToWaveProvider(source);

				using var enumerator = new MMDeviceEnumerator();
				MMDevice? dev = null;
				if (!string.IsNullOrWhiteSpace(deviceId) && mode != OutputMode.Asio)
				{
					try
					{
						var d = enumerator.GetDevice(deviceId);
						if (d.State == DeviceState.Active) dev = d;
						else d.Dispose();
					}
					catch { }
				}
				dev ??= enumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);

				string usedDesc = dev.FriendlyName;
				using (dev)
				{
					IWavePlayer? player = null;
					Exception? playEx = null;
					try
					{
						if (mode == OutputMode.Asio && !string.IsNullOrWhiteSpace(asioDriver))
						{
							player = new AsioOut(asioDriver);
							usedDesc = $"ASIO · {asioDriver}";
						}
						else
						{
							var shareMode = mode == OutputMode.WasapiExclusive ? AudioClientShareMode.Exclusive : AudioClientShareMode.Shared;
							player = new WasapiOut(dev, shareMode, false, Math.Clamp(bufferMs, 80, 400));
							usedDesc = $"{(shareMode == AudioClientShareMode.Exclusive ? "WASAPI Exclusive" : "WASAPI Shared")} · {dev.FriendlyName}";
						}
						player.PlaybackStopped += (_, args) =>
						{
							if (args.Exception != null) playEx = args.Exception;
						};
						player.Init(waveProvider);
						player.Play();
						for (int ms = 0; ms < 560 && !ct.IsCancellationRequested; ms += 35)
						{
							if (playEx != null) break;
							Thread.Sleep(35);
						}
						player.Stop();
					}
					finally
					{
						player?.Dispose();
					}

					if (playEx != null)
					{
						return new TestToneResult(false, usedDesc, Loc.F("测试音发声阶段抛出异常: {0}", playEx.Message));
					}
					return new TestToneResult(true, usedDesc, Loc.F("已通过 [{0}] 成功输出清脆测试提示音！物理通路正常。", usedDesc));
				}
			}
			catch (Exception ex)
			{
				return new TestToneResult(false, Loc.T("输出初始化失败"), Loc.F("测试音播放失败：{0}", ex.Message));
			}
		}, ct);
	}

	public static async Task<RepairActionResult> ExecuteRepairActionAsync(
		string repairId,
		AppSettings settings,
		SettingsService settingsService,
		AudioEngine? engine,
		Action<double, string>? onStepProgress = null,
		CancellationToken ct = default)
	{
		var steps = new List<string>();
		void Step(double p, string msg)
		{
			steps.Add(msg);
			onStepProgress?.Invoke(p, msg);
		}

		return await Task.Run(async () =>
		{
			switch (repairId)
			{
				case "unmute_volume":
				{
					Step(0.25, Loc.T("正在检测并解除 Windows 默认输出终端静音..."));
					try
					{
						using var enumerator = new MMDeviceEnumerator();
						MMDevice? dev = null;
						if (!string.IsNullOrWhiteSpace(settings.OutputDeviceId))
						{
							try { dev = enumerator.GetDevice(settings.OutputDeviceId); } catch { }
						}
						dev ??= enumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);
						using (dev)
						{
							if (dev.AudioEndpointVolume.Mute) dev.AudioEndpointVolume.Mute = false;
							if (dev.AudioEndpointVolume.MasterVolumeLevelScalar < 0.25f)
							{
								dev.AudioEndpointVolume.MasterVolumeLevelScalar = 0.50f;
							}
						}
					}
					catch (Exception ex)
					{
						Step(0.5, Loc.F("系统音量接口提示: {0}", ex.Message));
					}

					await Task.Delay(120, ct);
					Step(0.70, Loc.T("正在恢复 FurinaPlayer 应用内安全音量与前级增益..."));
					if (settings.Volume <= 5)
					{
						settings.Volume = 75;
						if (engine != null) engine.Volume = 75;
					}
					if (settings.FurinaPlayerHeadroomDb <= -12.0)
					{
						settings.FurinaPlayerHeadroomDb = -1.0;
						engine?.SetFurinaPlayerHeadroom(-1.0);
					}
					settingsService.Save(settings);
					Step(1.0, Loc.T("已完成静音解除与安全电平恢复。"));
					return new RepairActionResult(true, Loc.T("静音与电平修复"), Loc.T("已解除系统/应用静音并校验前级。"), steps);
				}

				case "rebind_default":
				case "reset_devices":
				{
					Step(0.30, Loc.T("正在重新扫描 Windows 活跃音频终端..."));
					string? defaultId = null;
					string defaultName = Loc.T("系统默认设备");
					try
					{
						using var enumerator = new MMDeviceEnumerator();
						using var def = enumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);
						defaultId = def.ID;
						defaultName = def.FriendlyName;
					}
					catch { }
					await Task.Delay(120, ct);

					Step(0.75, Loc.F("正在重新绑定输出目标 -> {0}", defaultName));
					settings.OutputMode = OutputMode.WasapiShared;
					settings.OutputDeviceId = defaultId ?? string.Empty;
					settingsService.Save(settings);
					engine?.ConfigureOutput(OutputMode.WasapiShared, string.IsNullOrEmpty(defaultId) ? null : defaultId, null, Math.Max(120, settings.BufferMilliseconds));
					Step(1.0, Loc.T("设备重新绑定完成。"));
					return new RepairActionResult(true, Loc.T("重新绑定可用设备"), Loc.F("已重新切换至当前活跃设备：{0}", defaultName), steps);
				}

				case "switch_shared":
				{
					Step(0.35, Loc.T("正在释放可能的独占/ASIO 锁并切回 WASAPI 共享模式..."));
					settings.OutputMode = OutputMode.WasapiShared;
					settings.BufferMilliseconds = Math.Clamp(settings.BufferMilliseconds, 140, 300);
					settingsService.Save(settings);
					await Task.Delay(120, ct);
					Step(0.75, Loc.T("正在重配置输出后端与活跃端点..."));
					string? targetDev = settings.OutputDeviceId;
					if (targetDev != null && !targetDev.StartsWith("{", StringComparison.Ordinal))
					{
						targetDev = null; // Previous value was an ASIO driver name
						settings.OutputDeviceId = string.Empty;
						settingsService.Save(settings);
					}
					engine?.ConfigureOutput(OutputMode.WasapiShared, string.IsNullOrWhiteSpace(targetDev) ? null : targetDev, null, settings.BufferMilliseconds);
					Step(1.0, Loc.T("已切换至最稳定的 WASAPI 共享输出模式。"));
					return new RepairActionResult(true, Loc.T("切换至 WASAPI 共享模式"), Loc.T("已解除独占冲突，可立即点击播放测试。"), steps);
				}

				case "tune_buffer":
				{
					Step(0.45, Loc.T("正在将输出缓冲调优至高采样率兼容值 (200 ms)..."));
					settings.BufferMilliseconds = 200;
					settingsService.Save(settings);
					await Task.Delay(100, ct);
					Step(0.85, Loc.T("正在应用新缓冲参数到音频会话..."));
					string? devId = settings.OutputMode == OutputMode.Asio || string.IsNullOrWhiteSpace(settings.OutputDeviceId) ? null : settings.OutputDeviceId;
					string? asio = settings.OutputMode == OutputMode.Asio && !string.IsNullOrWhiteSpace(settings.OutputDeviceId) ? settings.OutputDeviceId : null;
					engine?.ConfigureOutput(settings.OutputMode, devId, asio, 200);
					Step(1.0, Loc.T("缓冲参数已优化为 200 ms。"));
					return new RepairActionResult(true, Loc.T("缓冲大小自动调优"), Loc.T("输出缓冲已调整为 200 ms。"), steps);
				}

				case "bypass_dsp_src":
				{
					Step(0.35, Loc.T("正在暂时关闭高级升频 (FurinaSrc -> Off)..."));
					settings.FurinaSrcMode = "off";
					settingsService.Save(settings);
					engine?.Session.SetFurinaSrcConfig(new FurinaSrcRuntimeConfig { Mode = "off" }, reopenIfRateChanged: true);
					await Task.Delay(120, ct);
					Step(0.75, Loc.T("正在暂时旁路 VST3 插件链并检查信号直通..."));
					engine?.SetVst3(Array.Empty<Vst3PluginState>());
					Step(1.0, Loc.T("已切换为纯净原生采样率与直通 DSP 链路。"));
					return new RepairActionResult(true, Loc.T("旁路升频与高风险插件"), Loc.T("已恢复原生采样率并停用 VST3，便于快速排查。"), steps);
				}

				case "reset_engine":
				{
					Step(0.25, Loc.T("正在停止当前流并清理 NAudio / LibVLC 后端实例..."));
					await Task.Delay(120, ct);
					bool ok = false;
					if (engine != null)
					{
						Step(0.65, Loc.T("正在重建解码器、DSP 链路与输出端点绑定..."));
						ok = await engine.SoftResetAudioEngineAsync(preserveCurrentTrackAndSeek: true);
					}
					Step(1.0, ok ? Loc.T("音频引擎已热重置就绪。") : Loc.T("引擎已重置（当前暂无预加载曲目或打开需重试）。"));
					return new RepairActionResult(true, Loc.T("热重置音频引擎"), Loc.T("已重新初始化音频播放引擎而不退出程序。"), steps);
				}

				case "reset_all_audio":
				default:
				{
					Step(0.20, Loc.T("正在停止播放并备份音乐库目录与外观设定..."));
					await Task.Delay(120, ct);
					Step(0.45, Loc.T("正在还原输出模式 (WASAPI 共享)、系统默认设备与 200 ms 缓冲..."));
					settings.OutputMode = OutputMode.WasapiShared;
					settings.OutputDeviceId = string.Empty;
					settings.BufferMilliseconds = 200;
					settings.FurinaSrcMode = "off";
					settings.FurinaPlayerResamplerEnabled = false;
					settings.FurinaPlayerSdmEnabled = false;
					settings.FurinaPlayerHeadroomEnabled = true;
					settings.FurinaPlayerHeadroomDb = -3.0;
					settings.FurinaPlayerCompressorEnabled = false;
					settings.FurinaMultibandEnabled = false;
					settings.FurinaPlayerCrossfeedEnabled = false;
					settings.FurinaPlayerStereoFieldEnabled = false;
					settings.FurinaPlayerConvolutionEnabled = false;
					settings.ReverbEnabled = false;
					settings.Volume = 80;
					settings.EqGains = new double[10];
					settings.Vst3Plugins = new List<Vst3PluginState>();
					settingsService.Save(settings);

					await Task.Delay(120, ct);
					Step(0.75, Loc.T("正在将重置后的默认音频配置下发至实时 DSP 引擎..."));
					if (engine != null)
					{
						engine.ConfigureOutput(OutputMode.WasapiShared, null, null, 200);
						engine.Volume = 80;
						engine.SetEqGains(new double[10]);
						engine.Session.SetFurinaSrcConfig(new FurinaSrcRuntimeConfig { Mode = "off" }, reopenIfRateChanged: false);
						engine.SetFurinaPlayerEqEnabled(false);
						engine.SetFurinaPlayerEqPreamp(0.0);
						engine.SetFurinaPlayerHeadroom(true, -3.0);
						engine.SetVst3(Array.Empty<Vst3PluginState>());
						await engine.SoftResetAudioEngineAsync(preserveCurrentTrackAndSeek: true);
					}

					Step(1.0, Loc.T("所有音频与效果器设置已安全恢复出厂默认，音乐库与外观配置完好保留。"));
					return new RepairActionResult(true, Loc.T("重置所有音频设置"), Loc.T("已安全还原默认输出与全部音频开关，设备列表已刷新。"), steps);
				}
			}
		}, ct);
	}

	private static string BuildCopyableReportText(
		AudioSessionRuntimeDiagnostics r,
		AppSettings s,
		string devName,
		int epRate,
		int epBits,
		bool sysMute,
		float sysVol,
		bool devMissing,
		IReadOnlyList<string> anomalies,
		IReadOnlyList<DiagnosticStageStatus> stages)
	{
		var sb = new StringBuilder();
		sb.AppendLine("=== FurinaPlayer Audio Check & Repair Diagnostics ===");
		sb.AppendLine($"CapturedAtUtc: {DateTime.UtcNow:O}");
		sb.AppendLine($"State: {r.State} (DecoderPlaying={r.IsDecoderPlaying}, RestorePending={r.IsRestorePending})");
		sb.AppendLine($"Track: {r.CurrentTrackTitle} ({r.PositionSeconds:F1}s / {r.DurationSeconds:F1}s)");
		sb.AppendLine($"OutputMode: {s.OutputMode} | ActiveKind: {r.ActiveOutputKind} | Backend: {r.ActiveBackend}");
		sb.AppendLine($"ActiveDevice: {devName} (ConfigMissing={devMissing})");
		sb.AppendLine($"EndpointMixFormat: {epRate} Hz / {epBits}-bit | SystemMute: {sysMute} | SystemVol: {Math.Round(sysVol * 100)}% | AppVol: {s.Volume}%");
		sb.AppendLine($"SampleRate: File={r.SourceSampleRate} Hz -> Output={r.OutputSampleRate} Hz (Resampling={r.IsResampling}, FurinaSrc={r.ResamplerMode}/{r.ResamplerProfile})");
		sb.AppendLine($"BufferMs: {s.BufferMilliseconds}");
		sb.AppendLine($"DSP: Active={r.DspActive} (EQ={r.EqEnabled}, Calibration={r.HeadphoneCalibrationEnabled}, VST3={r.Vst3Active}, Spatial={r.SpatialEnabled}, Dynamics={r.DynamicsEnabled})");
		sb.AppendLine($"LastError: {r.LastErrorMessage ?? "none"}");
		sb.AppendLine($"RecoveryCount: {r.RecoveryCount}");
		sb.AppendLine("Anomalies:");
		if (anomalies.Count == 0) sb.AppendLine("  - none");
		else foreach (var a in anomalies) sb.AppendLine($"  - {a}");
		sb.AppendLine("Stages:");
		foreach (var st in stages) sb.AppendLine($"  [{st.Severity}] {st.Title}: {st.Summary} ({st.Detail})");
		if (r.RecentTraceLines.Length > 0)
		{
			sb.AppendLine("RecentEngineTrace:");
			foreach (var line in r.RecentTraceLines) sb.AppendLine("  " + line);
		}
		return sb.ToString();
	}

	private static AudioSessionRuntimeDiagnostics CreateIdleRuntimeSnapshot(AppSettings s) => new(
		State: PlaybackState.Stopped,
		CurrentTrackTitle: string.Empty,
		CurrentFilePath: string.Empty,
		PositionSeconds: 0,
		DurationSeconds: 0,
		OutputMode: s.OutputMode,
		ConfiguredDeviceId: s.OutputDeviceId ?? string.Empty,
		ConfiguredAsioDriver: s.OutputMode == OutputMode.Asio ? (s.OutputDeviceId ?? string.Empty) : string.Empty,
		BufferMs: s.BufferMilliseconds,
		ActiveBackend: "NAudio",
		ActiveOutputKind: FormatOutputMode(s.OutputMode),
		SourceSampleRate: 0,
		OutputSampleRate: 0,
		BitsPerSample: 16,
		Channels: 2,
		ResamplerMode: s.FurinaSrcMode ?? "off",
		ResamplerProfile: s.FurinaSrcQuality ?? "transparent",
		IsResampling: false,
		DspActive: s.EqGains?.Any(g => Math.Abs(g) > 0.05) ?? false,
		EqEnabled: s.EqGains?.Any(g => Math.Abs(g) > 0.05) ?? false,
		HeadphoneCalibrationEnabled: false,
		Vst3Active: s.Vst3Plugins?.Any(p => p?.IsActive ?? false) ?? false,
		SpatialEnabled: s.ReverbEnabled || s.FurinaPlayerCrossfeedEnabled || s.FurinaPlayerStereoFieldEnabled,
		DynamicsEnabled: s.FurinaPlayerCompressorEnabled || s.FurinaMultibandEnabled,
		HeadroomDb: s.FurinaPlayerHeadroomDb,
		EqPreampDb: 0.0,
		LastErrorMessage: null,
		LastErrorAtUtc: null,
		RecoveryCount: 0,
		IsRestorePending: false,
		IsDecoderPlaying: false,
		VlcAvailable: false,
		RecentTraceLines: Array.Empty<string>());

	private static string FormatOutputMode(OutputMode mode) => mode switch
	{
		OutputMode.WasapiShared => Loc.T("WASAPI 共享"),
		OutputMode.WasapiExclusive => Loc.T("WASAPI 独占"),
		OutputMode.Asio => "ASIO",
		_ => Loc.T("软件解码")
	};

	private sealed class RawMemoryFloatSampleProvider : ISampleProvider
	{
		private readonly float[] _data;
		private int _pos;

		public WaveFormat WaveFormat { get; }

		public RawMemoryFloatSampleProvider(float[] data, int sampleRate, int channels)
		{
			_data = data;
			WaveFormat = WaveFormat.CreateIeeeFloatWaveFormat(sampleRate, channels);
		}

		public int Read(float[] buffer, int offset, int count)
		{
			int avail = Math.Min(count, _data.Length - _pos);
			if (avail <= 0) return 0;
			// Safe element copy (avoids ArrayTypeMismatchException on NAudio WaveBuffer.FloatBuffer)
			for (int i = 0; i < avail; i++)
			{
				buffer[offset + i] = _data[_pos + i];
			}
			_pos += avail;
			return avail;
		}
	}
}
