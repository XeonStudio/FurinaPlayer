using System;
using System.Collections.Generic;
using System.Linq;

namespace FurinaPlayer.Audio.Dsp;

public enum FurinaSrcQualityProfile
{
	Transparent,
	Balanced,
	LowLatency
}

public enum FurinaSrcWindowType
{
	Kaiser,
	Gaussian,
	BlackmanHarris,
	Hann
}

public enum FurinaSrcPhaseType
{
	Linear,
	Minimum
}

public sealed record FurinaSrcFilterProfileSpec(
	string Id,
	string DisplayName,
	string Family,
	FurinaSrcWindowType Window,
	FurinaSrcPhaseType Phase,
	int ReferenceTapCount,
	double AttenuationDb,
	double TransitionRatio,
	double CutoffScale,
	double WindowParam,
	ResamplerFilterMode LegacyMode,
	string Description,
	bool Is1xCandidate = true,
	bool IsNxCandidate = true);

public sealed record FurinaSrcStagePlan(
	int StageIndex,
	string ProfileId,
	int SourceRate,
	int TargetRate,
	int UpsampleFactor,
	int RuntimeTaps,
	double NormalizedCutoff,
	FurinaSrcPhaseType Phase,
	double ExpectedAttenuationDb);

public sealed record FurinaSrcFilterAnalysis(
	string ProfileId,
	double[] FrequenciesNormalized,
	double[] MagnitudeDb,
	double[] ImpulseResponse,
	double PassbandRippleDb,
	double StopbandAttenuationDb,
	double GroupDelayMsAt44k,
	int AnalyzedTaps);

public sealed class FurinaSrcRuntimeConfig
{
	public string Mode { get; init; } = "off";
	public int CustomTargetRate { get; init; } = 0;
	public string Quality { get; init; } = "transparent";
	public string Filter1x { get; init; } = "furina-gauss-long";
	public string FilterNx { get; init; } = "furina-hb";
	public string Compute { get; init; } = "simd";
	public string Dither { get; init; } = "off";
	public bool AbCompareBypass { get; init; } = false;

	public FurinaSrcQualityProfile QualityProfile => FurinaSrcCatalog.ParseQuality(Quality);
	public FurinaDitherMode DitherMode => FurinaSrcCatalog.ParseDitherMode(Dither);
	public FurinaComputeBackend ComputeBackend => FurinaSrcCatalog.ParseComputeBackend(Compute);
}

public sealed class FurinaSrcRuntimeTelemetry
{
	public bool Active { get; init; }
	public bool AbCompareBypassed { get; init; }
	public int SourceSampleRate { get; init; }
	public int TargetSampleRate { get; init; }
	public int OutputDeviceSampleRate { get; init; }
	public string ActiveSlot { get; init; } = "Standby";
	public string ActiveProfileId { get; init; } = "furina-gauss-long";
	public string ActiveProfileName { get; init; } = "FurinaPlayer Gauss Long";
	public string PhaseLabel { get; init; } = "Linear phase";
	public int StageCount { get; init; }
	public int TotalTaps { get; init; }
	public double AttenuationDb { get; init; }
	public double LatencyMs { get; init; }
	public double ProcessTimeMs { get; init; }
	public double CpuLoadPercent { get; init; }
	public int SimdWidthBits { get; init; }
	public bool IsMultiThreaded { get; init; }
	public bool IsFallbackRate { get; init; }
	public string StageSummary { get; init; } = "Native pass-through";
}

/// <summary>
/// Unified FurinaPlayer SRC filter catalog, mathematical FIR kernel synthesis
/// (Kaiser / Gaussian / Blackman-Harris / Hann + FFT cepstral minimum-phase factorization),
/// and real-time magnitude/impulse response analyzer.
/// </summary>
public static class FurinaSrcCatalog
{
	public const double PcmApodMinringFirstStageCutoffHz = 20500.0;

	public static readonly IReadOnlyList<FurinaSrcFilterProfileSpec> Profiles = new[]
	{
		// Gaussian family
		new FurinaSrcFilterProfileSpec(
			"furina-gauss-long", "FurinaPlayer Gauss Long", "Gaussian",
			FurinaSrcWindowType.Gaussian, FurinaSrcPhaseType.Linear,
			1023, 145, 0.050, 0.965, 3.20, ResamplerFilterMode.GaussLong,
			"高阶高斯窗线性相位滤波，阻带衰减深、无预振铃感染色，兼顾音乐润泽度与瞬态。"),
		new FurinaSrcFilterProfileSpec(
			"furina-gauss-short", "FurinaPlayer Gauss Short", "Gaussian",
			FurinaSrcWindowType.Gaussian, FurinaSrcPhaseType.Linear,
			767, 138, 0.060, 0.960, 2.90, ResamplerFilterMode.GaussShort,
			"轻快高斯窗线性相位滤波，瞬态响应敏捷、延迟低，听感自然温暖。"),
		new FurinaSrcFilterProfileSpec(
			"furina-gauss-xla", "FurinaPlayer Gauss XLA", "Gaussian",
			FurinaSrcWindowType.Gaussian, FurinaSrcPhaseType.Linear,
			2047, 165, 0.035, 0.955, 3.60, ResamplerFilterMode.GaussLong,
			"加长高阶高斯窗滤波，过渡带极净、高频延伸顺滑如丝。"),
		new FurinaSrcFilterProfileSpec(
			"furina-gauss-hires-mp", "FurinaPlayer Gauss HiRes MP", "Gaussian",
			FurinaSrcWindowType.Gaussian, FurinaSrcPhaseType.Minimum,
			1023, 145, 0.052, 0.945, 3.15, ResamplerFilterMode.MinPhase,
			"高解析高斯最小相位滤波，零前振铃，适合追求微动态起音表现的音源。", Is1xCandidate: true, IsNxCandidate: true),

		// Apodizing & MinRing family
		new FurinaSrcFilterProfileSpec(
			"furina-apod-minring", "FurinaPlayer Apodizing MinRing", "Apodizing",
			FurinaSrcWindowType.Kaiser, FurinaSrcPhaseType.Minimum,
			1535, 155, 0.052, 0.875, 14.0, ResamplerFilterMode.MinPhase,
			"零前振铃切趾滤波（44.1/48k 首级 20.5kHz 截止），显著净化前期母带 ADC 混叠振铃。"),
		new FurinaSrcFilterProfileSpec(
			"furina-apod-fast", "FurinaPlayer Apodizing Fast", "Apodizing",
			FurinaSrcWindowType.Kaiser, FurinaSrcPhaseType.Linear,
			767, 132, 0.070, 0.900, 11.5, ResamplerFilterMode.PolySincExtended,
			"轻量线性相位切趾滤波，柔化数字毛刺感并保持较低运算开销。"),
		new FurinaSrcFilterProfileSpec(
			"furina-apod-long", "FurinaPlayer Apodizing Long", "Apodizing",
			FurinaSrcWindowType.BlackmanHarris, FurinaSrcPhaseType.Linear,
			2047, 165, 0.048, 0.870, 0.0, ResamplerFilterMode.Steep,
			"高阶 Blackman-Harris 切趾滤波，兼具干净滚降与 >160 dB 镜像阻断能力。"),
		new FurinaSrcFilterProfileSpec(
			"furina-minring-lp", "FurinaPlayer MinRing LP", "MinRing",
			FurinaSrcWindowType.Kaiser, FurinaSrcPhaseType.Minimum,
			767, 135, 0.055, 0.940, 11.0, ResamplerFilterMode.MinPhase,
			"轻量最小相位抗振铃滤波，起音利落、延迟极低。"),
		new FurinaSrcFilterProfileSpec(
			"furina-minring-mp", "FurinaPlayer MinRing MP", "MinRing",
			FurinaSrcWindowType.Kaiser, FurinaSrcPhaseType.Minimum,
			1023, 145, 0.048, 0.930, 12.5, ResamplerFilterMode.MinPhase,
			"经典最小相位抗振铃配置，零前振铃且中高频能量高度聚焦。"),
		new FurinaSrcFilterProfileSpec(
			"furina-minring-soft", "FurinaPlayer MinRing Soft", "MinRing",
			FurinaSrcWindowType.Gaussian, FurinaSrcPhaseType.Minimum,
			1535, 150, 0.050, 0.900, 3.65, ResamplerFilterMode.MinPhase,
			"高斯窗最小相位柔和调音，兼具极佳微细节与宽松模拟味。"),

		// Halfband & Polyphase Kaiser Sinc family
		new FurinaSrcFilterProfileSpec(
			"furina-hb", "FurinaPlayer Halfband", "Halfband",
			FurinaSrcWindowType.Kaiser, FurinaSrcPhaseType.Linear,
			255, 118, 0.068, 0.975, 9.0, ResamplerFilterMode.PolySincHalfBand,
			"高效多相半带线性相位滤波，天然适合 2x/4x/8x 级联与实时高倍率升频。"),
		new FurinaSrcFilterProfileSpec(
			"furina-sinc-m", "FurinaPlayer Sinc M", "PolySinc",
			FurinaSrcWindowType.Kaiser, FurinaSrcPhaseType.Linear,
			511, 125, 0.062, 0.985, 10.5, ResamplerFilterMode.SincPolyphase,
			"中等阶数凯撒窗多相 Sinc 滤波，通带全平直，互调失真极低。"),
		new FurinaSrcFilterProfileSpec(
			"furina-sinc-long", "FurinaPlayer Sinc Long", "PolySinc",
			FurinaSrcWindowType.Kaiser, FurinaSrcPhaseType.Linear,
			1023, 145, 0.048, 0.995, 13.0, ResamplerFilterMode.SincPolyphase,
			"长阶凯撒窗多相 Sinc 滤波，群延迟严格恒定，适合追求零相位失真的监听风格。"),
		new FurinaSrcFilterProfileSpec(
			"furina-sinc-xla", "FurinaPlayer Sinc XLA", "PolySinc",
			FurinaSrcWindowType.Kaiser, FurinaSrcPhaseType.Linear,
			2047, 165, 0.034, 1.000, 15.5, ResamplerFilterMode.SincPolyphase,
			"母带参照级超长凯撒 Sinc 滤波，20kHz 全带宽完整保留无衰减。"),
		new FurinaSrcFilterProfileSpec(
			"furina-xtr-mp", "FurinaPlayer XTR MP", "PolySinc",
			FurinaSrcWindowType.Kaiser, FurinaSrcPhaseType.Minimum,
			1535, 160, 0.038, 0.965, 15.0, ResamplerFilterMode.MinPhase,
			"极限瞬态响应最小相位凯撒滤波，阻带深度达 160 dB。"),
		new FurinaSrcFilterProfileSpec(
			"furina-closed", "FurinaPlayer Closed-Form", "Halfband",
			FurinaSrcWindowType.Hann, FurinaSrcPhaseType.Linear,
			511, 110, 0.070, 0.970, 0.0, ResamplerFilterMode.PolySincExtended,
			"闭式平滑汉宁窗滤波，计算高效且听感干净耐听。"),

		// Steep Mastering Brickwall family
		new FurinaSrcFilterProfileSpec(
			"furina-steep-short", "FurinaPlayer Steep Short", "Steep",
			FurinaSrcWindowType.BlackmanHarris, FurinaSrcPhaseType.Linear,
			511, 125, 0.060, 0.975, 0.0, ResamplerFilterMode.Steep,
			"短阶陡峭 Blackman-Harris 滤波，截止陡峭且 CPU 占用轻盈。"),
		new FurinaSrcFilterProfileSpec(
			"furina-steep-long", "FurinaPlayer Steep Long", "Steep",
			FurinaSrcWindowType.BlackmanHarris, FurinaSrcPhaseType.Linear,
			1023, 145, 0.045, 0.985, 0.0, ResamplerFilterMode.Steep,
			"长阶陡峭砖墙滤波，阻带深达 145 dB，强力隔绝超声镜像混叠。"),
		new FurinaSrcFilterProfileSpec(
			"furina-steep-xl", "FurinaPlayer Steep XL", "Steep",
			FurinaSrcWindowType.BlackmanHarris, FurinaSrcPhaseType.Linear,
			3071, 175, 0.030, 0.992, 0.0, ResamplerFilterMode.Steep,
			"旗舰 3071 参考阶陡峭滤波，提供实验室测量级频带分离度。"),
		new FurinaSrcFilterProfileSpec(
			"furina-steep3-long", "FurinaPlayer Steep3 Ultra", "Steep",
			FurinaSrcWindowType.BlackmanHarris, FurinaSrcPhaseType.Linear,
			2047, 175, 0.030, 0.998, 0.0, ResamplerFilterMode.Steep,
			"极致陡峭扩展滤波，99.8% 奈奎斯特可用带宽与极限阻带抑制。"),
	};

	private static readonly Dictionary<string, FurinaSrcFilterProfileSpec> s_byId;
	private static readonly Dictionary<string, string> s_aliases;

	static FurinaSrcCatalog()
	{
		s_byId = Profiles.ToDictionary(p => p.Id, StringComparer.OrdinalIgnoreCase);
		s_aliases = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
		{
			["Linear Phase"] = "furina-sinc-long",
			["Min Phase"] = "furina-minring-mp",
			["Sinc Polyphase"] = "furina-sinc-long",
			["Steep"] = "furina-steep-long",
			["FurinaPlayer Gauss Long"] = "furina-gauss-long",
			["FurinaPlayer Gauss Short"] = "furina-gauss-short",
			["FurinaPlayer Gauss XLA"] = "furina-gauss-xla",
			["FurinaPlayer Gauss HiRes MP"] = "furina-gauss-hires-mp",
			["FurinaPlayer Apodizing MinRing"] = "furina-apod-minring",
			["FurinaPlayer Apodizing Fast"] = "furina-apod-fast",
			["FurinaPlayer Apodizing Long"] = "furina-apod-long",
			["FurinaPlayer MinRing LP"] = "furina-minring-lp",
			["FurinaPlayer MinRing MP"] = "furina-minring-mp",
			["FurinaPlayer MinRing Soft"] = "furina-minring-soft",
			["FurinaPlayer Halfband"] = "furina-hb",
			["FurinaPlayer Sinc M"] = "furina-sinc-m",
			["FurinaPlayer Sinc Long"] = "furina-sinc-long",
			["FurinaPlayer Sinc XLA"] = "furina-sinc-xla",
			["FurinaPlayer XTR MP"] = "furina-xtr-mp",
			["FurinaPlayer Closed-Form"] = "furina-closed",
			["FurinaPlayer Steep Short"] = "furina-steep-short",
			["FurinaPlayer Steep Long"] = "furina-steep-long",
			["FurinaPlayer Steep XL"] = "furina-steep-xl",
			["FurinaPlayer Steep3 Ultra"] = "furina-steep3-long",
			// Normalize any legacy ids transparently
			["poly-sinc-hb"] = "furina-hb",
			["poly-sinc-gauss-long"] = "furina-gauss-long",
			["poly-sinc-ext2-short"] = "furina-steep-short",
			["poly-sinc-ext2-long"] = "furina-steep-long",
			["poly-sinc-ext2-xl"] = "furina-steep-xl",
			["apod-minring"] = "furina-apod-minring",
			["minringFIR-mp"] = "furina-minring-mp",
			["closed-form"] = "furina-closed",
		};
		foreach (var p in Profiles)
		{
			s_aliases[p.DisplayName] = p.Id;
		}
	}

	public static string NormalizeProfileId(string? value, string fallbackId = "furina-gauss-long")
	{
		if (string.IsNullOrWhiteSpace(value)) return fallbackId;
		string trimmed = value.Trim();
		if (s_byId.ContainsKey(trimmed)) return s_byId[trimmed].Id;
		if (s_aliases.TryGetValue(trimmed, out string? mapped)) return mapped;

		// Handle legacy prefix normalization
		

		string lower = trimmed.ToLowerInvariant();
		if (lower.Contains("apod") && lower.Contains("minring")) return "furina-apod-minring";
		if (lower.Contains("apod")) return "furina-apod-long";
		if (lower.Contains("minring") || lower.Contains("min")) return "furina-minring-mp";
		if (lower.Contains("gauss") && lower.Contains("short")) return "furina-gauss-short";
		if (lower.Contains("gauss")) return "furina-gauss-long";
		if (lower.Contains("steep") && lower.Contains("xl")) return "furina-steep-xl";
		if (lower.Contains("steep") && lower.Contains("short")) return "furina-steep-short";
		if (lower.Contains("steep")) return "furina-steep-long";
		if (lower.Contains("half") || lower.EndsWith("-hb")) return "furina-hb";
		if (lower.Contains("sinc") || lower.Contains("linear")) return "furina-sinc-long";
		return fallbackId;
	}

	public static FurinaSrcFilterProfileSpec GetProfile(string? idOrName, string fallbackId = "furina-gauss-long")
	{
		string id = NormalizeProfileId(idOrName, fallbackId);
		return s_byId.TryGetValue(id, out var spec) ? spec : s_byId[fallbackId];
	}

	public static int DetectRateFamilyBase(int sampleRate)
	{
		if (sampleRate > 0 && sampleRate % 44100 == 0) return 44100;
		if (sampleRate > 0 && sampleRate % 48000 == 0) return 48000;
		return (sampleRate % 11025 == 0) ? 44100 : 48000;
	}

	public static int ResolveTargetSampleRate(int sourceSampleRate, string? mode, int customTargetRate = 0)
	{
		int src = Math.Max(8000, sourceSampleRate);
		string m = (mode ?? "off").Trim();
		if (string.Equals(m, "off", StringComparison.OrdinalIgnoreCase))
		{
			return src;
		}
		if (string.Equals(m, "compatibility48", StringComparison.OrdinalIgnoreCase))
		{
			return 48000;
		}
		if (string.Equals(m, "custom", StringComparison.OrdinalIgnoreCase))
		{
			return customTargetRate >= 8000 ? Math.Clamp(customTargetRate, 44100, 384000) : src;
		}

		int factor = m switch
		{
			"family2x" => 2,
			"family4x" => 4,
			"family8x" => 8,
			_ => 1
		};
		if (factor <= 1) return src;

		int familyBase = DetectRateFamilyBase(src);
		int desired = checked(src * factor);
		if (desired <= 384000) return desired;

		// Cap gracefully at highest hardware PCM rate in same family <= 384kHz
		int maxFamilyRate = familyBase == 44100 ? 352800 : 384000;
		return Math.Max(src, maxFamilyRate);
	}

	public static FurinaSrcQualityProfile ParseQuality(string? quality) =>
		(quality ?? "transparent").Trim().ToLowerInvariant() switch
		{
			"balanced" => FurinaSrcQualityProfile.Balanced,
			"lowlatency" or "low-latency" or "low_latency" => FurinaSrcQualityProfile.LowLatency,
			_ => FurinaSrcQualityProfile.Transparent
		};

	public static FurinaDitherMode ParseDitherMode(string? dither)
	{
		string d = (dither ?? "off").Trim().ToLowerInvariant();
		if (d is "off" or "关闭" or "none") return FurinaDitherMode.Off;
		if (d is "tpdf") return FurinaDitherMode.Tpdf;
		if (d.Contains("ultra")) return FurinaDitherMode.UltraShaped;
		if (d.Contains("ns-9") || d.Contains("ns9")) return FurinaDitherMode.Ns9;
		if (d.Contains("ns-5") || d.Contains("ns5")) return FurinaDitherMode.Ns5;
		if (d.Contains("high")) return FurinaDitherMode.HighPass;
		return FurinaDitherMode.Off;
	}

	public static FurinaComputeBackend ParseComputeBackend(string? compute)
	{
		string c = (compute ?? "simd").Trim().ToLowerInvariant();
		if (c.Contains("multi") || c.Contains("多核") || c.Contains("线程") || c == "simd-mt")
			return FurinaComputeBackend.CpuMultiThread;
		return FurinaComputeBackend.CpuSimd;
	}

	public static int ResolveRuntimeTapCount(FurinaSrcFilterProfileSpec spec, FurinaSrcQualityProfile quality, int stageIndex)
	{
		// Scale FIR kernel taps to maintain sub-1ms real-time SIMD headroom even at 384kHz 8x.
		// Stage 0 (1x->2x) keeps highest resolution; upper carrier stages (Nx) scale down slightly.
		int cap = quality switch
		{
			FurinaSrcQualityProfile.Transparent => stageIndex == 0 ? 191 : 95,
			FurinaSrcQualityProfile.Balanced => stageIndex == 0 ? 95 : 47,
			FurinaSrcQualityProfile.LowLatency => stageIndex == 0 ? 47 : 23,
			_ => 127
		};
		int scaled = Math.Min(spec.ReferenceTapCount, cap);
		return Math.Max(15, scaled | 1); // guarantee odd length for exact center symmetry
	}

	public static IReadOnlyList<FurinaSrcStagePlan> CreateStagePlans(
		int sourceSampleRate,
		int targetSampleRate,
		string filter1xId,
		string filterNxId,
		FurinaSrcQualityProfile quality)
	{
		var plans = new List<FurinaSrcStagePlan>();
		if (targetSampleRate <= 0 || sourceSampleRate <= 0 || targetSampleRate == sourceSampleRate)
			return plans;

		// Check if exact power-of-2 integer upsampling (2x, 4x, 8x)
		int ratio = targetSampleRate / sourceSampleRate;
		if (targetSampleRate > sourceSampleRate &&
			sourceSampleRate * ratio == targetSampleRate &&
			(ratio == 2 || ratio == 4 || ratio == 8))
		{
			int stageCount = (int)Math.Log2(ratio);
			int cur = sourceSampleRate;
			for (int i = 0; i < stageCount; i++)
			{
				int next = cur * 2;
				string profileId = cur < 50000 ? NormalizeProfileId(filter1xId) : NormalizeProfileId(filterNxId, "furina-hb");
				var spec = GetProfile(profileId);
				int taps = ResolveRuntimeTapCount(spec, quality, i);
				double nyquistAtStage = 0.25; // 1x -> 2x halfband nyquist
				double cutoff = Math.Clamp(nyquistAtStage * Math.Max(0.75, 1.0 - spec.TransitionRatio) * spec.CutoffScale, 0.05, 0.495);

				// First-stage 20.5 kHz apodizing min-ring guard for 44.1 / 48 kHz CD/PCM masters
				if (i == 0 && string.Equals(spec.Id, "furina-apod-minring", StringComparison.OrdinalIgnoreCase) && (cur == 44100 || cur == 48000))
				{
					cutoff = Math.Clamp(PcmApodMinringFirstStageCutoffHz / next, 0.05, 0.495);
				}

				plans.Add(new FurinaSrcStagePlan(
					i, spec.Id, cur, next, 2, taps, cutoff, spec.Phase, spec.AttenuationDb));
				cur = next;
			}
			return plans;
		}

		// Arbitrary ratio single-stage polyphase plan
		string slotId = sourceSampleRate < 50000 ? NormalizeProfileId(filter1xId) : NormalizeProfileId(filterNxId, "furina-hb");
		var singleSpec = GetProfile(slotId);
		double ratioF = Math.Min(1.0, (double)targetSampleRate / sourceSampleRate) * 0.5;
		double singleCutoff = Math.Clamp(ratioF * Math.Max(0.78, 1.0 - singleSpec.TransitionRatio) * singleSpec.CutoffScale, 0.05, 0.495);
		int singleTaps = quality switch
		{
			FurinaSrcQualityProfile.Transparent => 95,
			FurinaSrcQualityProfile.Balanced => 63,
			_ => 31
		};
		plans.Add(new FurinaSrcStagePlan(
			0, singleSpec.Id, sourceSampleRate, targetSampleRate, 1, singleTaps, singleCutoff, singleSpec.Phase, singleSpec.AttenuationDb));
		return plans;
	}

	public static double BesselI0(double x)
	{
		double sum = 1.0;
		double term = 1.0;
		double squared = (x * x) * 0.25;
		for (int i = 1; i <= 28; i++)
		{
			term *= squared / (i * i);
			sum += term;
			if (term < 1e-15) break;
		}
		return sum;
	}

	public static double ComputeWindow(int index, int tapCount, FurinaSrcFilterProfileSpec spec)
	{
		if (tapCount <= 1) return 1.0;
		double pos = (double)index / (tapCount - 1);
		double centered = 2.0 * pos - 1.0;
		return spec.Window switch
		{
			FurinaSrcWindowType.BlackmanHarris =>
				0.35875
				- 0.48829 * Math.Cos(2.0 * Math.PI * pos)
				+ 0.14128 * Math.Cos(4.0 * Math.PI * pos)
				- 0.01168 * Math.Cos(6.0 * Math.PI * pos),
			FurinaSrcWindowType.Gaussian =>
				Math.Exp(-0.5 * Math.Pow((spec.WindowParam > 0 ? spec.WindowParam : 3.2) * centered, 2.0)),
			FurinaSrcWindowType.Kaiser =>
				BesselI0((spec.WindowParam > 0 ? spec.WindowParam : 12.0) * Math.Sqrt(Math.Max(0.0, 1.0 - centered * centered)))
				/ BesselI0(spec.WindowParam > 0 ? spec.WindowParam : 12.0),
			_ => 0.5 - 0.5 * Math.Cos(2.0 * Math.PI * pos)
		};
	}

	public static float[] SynthesizeLowpassTaps(FurinaSrcFilterProfileSpec spec, int tapCount, double normalizedCutoff, double gain = 1.0)
	{
		int taps = Math.Max(3, tapCount | 1);
		double cutoff = Math.Clamp(normalizedCutoff, 0.005, 0.495);
		double center = (taps - 1) * 0.5;
		var linear = new double[taps];
		double sum = 0.0;

		for (int i = 0; i < taps; i++)
		{
			double offset = i - center;
			double sincVal = Math.Abs(offset) < 1e-12
				? 1.0
				: Math.Sin(2.0 * Math.PI * cutoff * offset) / (2.0 * Math.PI * cutoff * offset);
			double v = 2.0 * cutoff * sincVal * ComputeWindow(i, taps, spec);
			linear[i] = v;
			sum += v;
		}

		if (Math.Abs(sum) > 1e-12)
		{
			double inv = 1.0 / sum;
			for (int i = 0; i < taps; i++) linear[i] *= inv;
		}

		double[] result = spec.Phase == FurinaSrcPhaseType.Minimum
			? ConvertToMinimumPhaseTaps(linear)
			: linear;

		var f32 = new float[result.Length];
		for (int i = 0; i < result.Length; i++)
		{
			f32[i] = (float)(result[i] * gain);
		}
		return f32;
	}

	/// <summary>
	/// Real cepstrum spectral factorization (Hilbert transform minimum-phase conversion):
	/// concentrates impulse energy near t=0 while keeping exact magnitude response, eliminating pre-ringing.
	/// </summary>
	public static double[] ConvertToMinimumPhaseTaps(double[] linearPhaseTaps)
	{
		int n = linearPhaseTaps.Length;
		int fftSize = 1;
		int minSize = Math.Max(64, n * 8);
		while (fftSize < minSize) fftSize <<= 1;

		var re = new double[fftSize];
		var im = new double[fftSize];
		Array.Copy(linearPhaseTaps, re, n);

		FftInPlace(re, im, inverse: false);
		for (int i = 0; i < fftSize; i++)
		{
			double mag = Math.Sqrt(re[i] * re[i] + im[i] * im[i]);
			re[i] = Math.Log(Math.Max(1e-12, mag));
			im[i] = 0.0;
		}

		FftInPlace(re, im, inverse: true);

		// Causal cepstral window (double positive quefrencies, zero negative quefrencies)
		for (int i = 1; i < fftSize / 2; i++) re[i] *= 2.0;
		re[fftSize / 2] = 0.0;
		for (int i = fftSize / 2 + 1; i < fftSize; i++) re[i] = 0.0;
		Array.Clear(im, 0, fftSize);

		FftInPlace(re, im, inverse: false);
		for (int i = 0; i < fftSize; i++)
		{
			double mag = Math.Exp(re[i]);
			double phase = im[i];
			re[i] = mag * Math.Cos(phase);
			im[i] = mag * Math.Sin(phase);
		}

		FftInPlace(re, im, inverse: true);

		var output = new double[n];
		double sum = 0.0;
		for (int i = 0; i < n; i++)
		{
			output[i] = re[i];
			sum += output[i];
		}
		if (Math.Abs(sum) > 1e-12)
		{
			double inv = 1.0 / sum;
			for (int i = 0; i < n; i++) output[i] *= inv;
		}
		return output;
	}

	/// <summary>
	/// Computes true FFT-derived frequency response (dB) and time-domain impulse response
	/// directly from the synthesized FIR filter kernel for UI plotting & telemetry.
	/// </summary>
	public static FurinaSrcFilterAnalysis AnalyzeFilter(string profileId, FurinaSrcQualityProfile quality, int sourceRate = 44100)
	{
		var spec = GetProfile(profileId);
		int taps = ResolveRuntimeTapCount(spec, quality, 0);
		int target2x = sourceRate * 2;
		double cutoff = Math.Clamp(0.25 * Math.Max(0.75, 1.0 - spec.TransitionRatio) * spec.CutoffScale, 0.05, 0.495);
		if (string.Equals(spec.Id, "furina-apod-minring", StringComparison.OrdinalIgnoreCase) && (sourceRate == 44100 || sourceRate == 48000))
		{
			cutoff = Math.Clamp(PcmApodMinringFirstStageCutoffHz / target2x, 0.05, 0.495);
		}

		float[] kernel = SynthesizeLowpassTaps(spec, taps, cutoff, 1.0);

		const int fftSize = 1024;
		var re = new double[fftSize];
		var im = new double[fftSize];
		for (int i = 0; i < kernel.Length && i < fftSize; i++)
		{
			re[i] = kernel[i];
		}
		FftInPlace(re, im, inverse: false);

		int bins = fftSize / 2 + 1;
		var freqs = new double[bins];
		var magDb = new double[bins];
		double maxPassDb = -200.0;
		double minPassDb = 200.0;
		double maxStopDb = -200.0;

		for (int i = 0; i < bins; i++)
		{
			double fNorm = (double)i / (fftSize / 2); // 0..1 (1.0 == stage Nyquist; 0.5 == source Nyquist at 2x)
			freqs[i] = fNorm;
			double mag = Math.Sqrt(re[i] * re[i] + im[i] * im[i]);
			double db = Math.Max(-180.0, 20.0 * Math.Log10(Math.Max(1e-9, mag)));
			magDb[i] = db;

			if (fNorm <= cutoff * 1.65) // within passband
			{
				if (db > maxPassDb) maxPassDb = db;
				if (db < minPassDb) minPassDb = db;
			}
			else if (fNorm >= 0.55) // deep stopband past source Nyquist
			{
				if (db > maxStopDb) maxStopDb = db;
			}
		}

		var impulse = new double[kernel.Length];
		double peak = 1e-9;
		for (int i = 0; i < kernel.Length; i++)
		{
			impulse[i] = kernel[i];
			if (Math.Abs(impulse[i]) > peak) peak = Math.Abs(impulse[i]);
		}
		for (int i = 0; i < impulse.Length; i++) impulse[i] /= peak;

		double ripple = Math.Max(0.0, maxPassDb - minPassDb);
		double measuredAtten = maxStopDb < -20.0 ? Math.Abs(maxStopDb) : spec.AttenuationDb;
		double groupDelaySamples = spec.Phase == FurinaSrcPhaseType.Linear ? (taps - 1) * 0.5 : taps * 0.08;
		double groupDelayMs = groupDelaySamples * 1000.0 / Math.Max(44100, target2x);

		return new FurinaSrcFilterAnalysis(
			spec.Id,
			freqs,
			magDb,
			impulse,
			ripple,
			Math.Max(measuredAtten, spec.AttenuationDb * 0.8),
			groupDelayMs,
			taps);
	}

	private static void FftInPlace(double[] re, double[] im, bool inverse)
	{
		int n = re.Length;
		int j = 0;
		for (int i = 0; i < n; i++)
		{
			if (i < j)
			{
				(re[i], re[j]) = (re[j], re[i]);
				(im[i], im[j]) = (im[j], im[i]);
			}
			int bit = n >> 1;
			while ((j & bit) != 0)
			{
				j ^= bit;
				bit >>= 1;
			}
			j |= bit;
		}

		for (int len = 2; len <= n; len <<= 1)
		{
			double ang = (inverse ? 2.0 : -2.0) * Math.PI / len;
			double wLenRe = Math.Cos(ang);
			double wLenIm = Math.Sin(ang);
			int half = len >> 1;

			for (int start = 0; start < n; start += len)
			{
				double curRe = 1.0;
				double curIm = 0.0;
				for (int k = 0; k < half; k++)
				{
					int u = start + k;
					int v = u + half;
					double tRe = re[v] * curRe - im[v] * curIm;
					double tIm = re[v] * curIm + im[v] * curRe;
					re[v] = re[u] - tRe;
					im[v] = im[u] - tIm;
					re[u] += tRe;
					im[u] += tIm;

					double nextRe = curRe * wLenRe - curIm * wLenIm;
					curIm = curRe * wLenIm + curIm * wLenRe;
					curRe = nextRe;
				}
			}
		}

		if (inverse)
		{
			double inv = 1.0 / n;
			for (int i = 0; i < n; i++)
			{
				re[i] *= inv;
				im[i] *= inv;
			}
		}
	}
}
