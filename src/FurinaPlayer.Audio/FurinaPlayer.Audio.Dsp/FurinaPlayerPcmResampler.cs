using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;
using System.Threading.Tasks;

namespace FurinaPlayer.Audio.Dsp;

public enum ResamplerFilterMode
{
	HermiteCubic,
	PolySincHalfBand,
	PolySincExtended,
	Linear,
	SincPolyphase,
	GaussLong,
	GaussShort,
	Steep,
	MinPhase
}

public enum FurinaDitherMode
{
	Off,
	Tpdf,
	HighPass,
	Ns5,
	Ns9,
	UltraShaped
}

public enum FurinaComputeBackend
{
	CpuSimd,
	CpuMultiThread,
	GpuCuda
}

/// <summary>
/// FurinaPlayer high-precision SIMD Polyphase &amp; Multi-Stage 2x Cascade PCM SRC Engine.
/// Supports up to 384 kHz family-locked upsampling, dual 1x/Nx FIR slots (Kaiser, Gaussian,
/// Blackman-Harris, Hann, and FFT cepstral minimum-phase), AVX2/Vector&lt;float&gt; acceleration,
/// 5th/9th-order psychoacoustic noise-shaped dithering, and glitch-free A/B blind bypass.
/// </summary>
public class FurinaPlayerPcmResampler
{
	private const int ArbitraryTaps = 64;
	private const int ArbitraryPhases = 256;
	private const int MaxChannels = 8;

	private readonly object _configLock = new();

	private int _sourceSampleRate = 44100;
	private int _targetSampleRate = 44100;
	private bool _enabled;
	private bool _compareBypassed;
	private ResamplerFilterMode _mode = ResamplerFilterMode.GaussLong;
	private FurinaDitherMode _ditherMode = FurinaDitherMode.Off;
	private FurinaComputeBackend _computeBackend = FurinaComputeBackend.CpuSimd;
	private FurinaSrcQualityProfile _quality = FurinaSrcQualityProfile.Transparent;
	private string _filter1xId = "furina-gauss-long";
	private string _filterNxId = "furina-hb";

	// Cascaded 2x polyphase FIR stages (used for 2x / 4x / 8x integer upsampling)
	private TwoXStage[] _cascadeStages = Array.Empty<TwoXStage>();
	private IReadOnlyList<FurinaSrcStagePlan> _stagePlans = Array.Empty<FurinaSrcStagePlan>();

	// Arbitrary ratio SIMD polyphase kernel [phase][taps] (reversed for forward SIMD dot-product)
	private float[][] _arbitraryTable = Array.Empty<float[]>();
	private readonly float[][] _arbHistory = new float[MaxChannels][];
	private float[] _arbScratchIn = Array.Empty<float>();
	private double _phase;

	// Dither & high-order noise-shaping state per channel (16-tap history for up to NS-9)
	private readonly float[][] _noiseErr = new float[MaxChannels][];
	private readonly int[] _noiseErrPos = new int[MaxChannels];
	private uint _rngState = 0x9E3779B9u;
	private bool _firstBlockPending = true;

	// Zero-allocation interleaved conversion buffers
	private float[][] _channelInput = new float[MaxChannels][];
	private float[][] _channelOutput = new float[MaxChannels][];

	// Telemetry
	private double _lastProcessMs;
	private double _lastCpuLoadPct;

	private sealed class TwoXStage
	{
		public int StageIndex;
		public string ProfileId = "furina-hb";
		public int SubTaps;
		// Stored in time-reversed order so dot product can run forward contiguously with Vector<float>
		public float[] EvenRevTaps = Array.Empty<float>();
		public float[] OddRevTaps = Array.Empty<float>();
		public float[][] History = new float[MaxChannels][];
		public float[][] Work = new float[MaxChannels][];

		public TwoXStage(int stageIndex, string profileId, float[] evenRevTaps, float[] oddRevTaps)
		{
			StageIndex = stageIndex;
			ProfileId = profileId;
			EvenRevTaps = evenRevTaps;
			OddRevTaps = oddRevTaps;
			SubTaps = evenRevTaps.Length;
			for (int c = 0; c < MaxChannels; c++)
			{
				History[c] = new float[Math.Max(1, SubTaps - 1)];
				Work[c] = new float[4096];
			}
		}

		public void Reset()
		{
			Array.Clear(IsPrimed, 0, IsPrimed.Length);
			for (int c = 0; c < MaxChannels; c++)
			{
				Array.Clear(History[c], 0, History[c].Length);
			}
		}

		public readonly bool[] IsPrimed = new bool[MaxChannels];

		public void EnsureWorkCapacity(int requiredFrames)
		{
			int needed = SubTaps + requiredFrames + 32;
			for (int c = 0; c < MaxChannels; c++)
			{
				if (Work[c].Length < needed)
				{
					Work[c] = new float[needed];
				}
			}
		}
	}

	// 5th-order & 9th-order Wannamaker / Lipshitz psychoacoustic E-weighted error feedback coefficients
	private static readonly float[] Ns5Coeffs = { 2.033f, -2.165f, 1.959f, -1.590f, 0.614f };
	private static readonly float[] Ns9Coeffs = { 2.412f, -3.370f, 3.937f, -4.174f, 3.353f, -2.205f, 1.281f, -0.569f, 0.0847f };
	private static readonly float[] UltraShapedCoeffs = { 2.65f, -3.85f, 4.42f, -4.35f, 3.45f, -2.18f, 1.15f, -0.45f, 0.065f };

	public bool Enabled
	{
		get => _enabled && _targetSampleRate != _sourceSampleRate;
		set => _enabled = value;
	}

	public bool CompareBypassed
	{
		get => _compareBypassed;
		set => _compareBypassed = value;
	}

	public int SourceSampleRate => _sourceSampleRate;

	public int TargetSampleRate
	{
		get => _targetSampleRate;
		set
		{
			int clamped = Math.Clamp(value, 8000, 384000);
			if (_targetSampleRate != clamped)
			{
				_targetSampleRate = clamped;
				RebuildStages(resetHistory: false);
			}
		}
	}

	public ResamplerFilterMode FilterMode
	{
		get => _mode;
		set
		{
			if (_mode != value)
			{
				_mode = value;
				_filter1xId = MapLegacyModeToProfileId(value);
				RebuildStages(resetHistory: false);
			}
		}
	}

	public string FilterProfile1xId
	{
		get => _filter1xId;
		set
		{
			string normalized = FurinaSrcCatalog.NormalizeProfileId(value, "furina-gauss-long");
			if (!string.Equals(_filter1xId, normalized, StringComparison.Ordinal))
			{
				_filter1xId = normalized;
				_mode = FurinaSrcCatalog.GetProfile(normalized).LegacyMode;
				RebuildStages(resetHistory: false);
			}
		}
	}

	public string FilterProfileNxId
	{
		get => _filterNxId;
		set
		{
			string normalized = FurinaSrcCatalog.NormalizeProfileId(value, "furina-hb");
			if (!string.Equals(_filterNxId, normalized, StringComparison.Ordinal))
			{
				_filterNxId = normalized;
				RebuildStages(resetHistory: false);
			}
		}
	}

	public FurinaSrcQualityProfile QualityProfile
	{
		get => _quality;
		set
		{
			if (_quality != value)
			{
				_quality = value;
				RebuildStages(resetHistory: false);
			}
		}
	}

	public FurinaDitherMode DitherMode
	{
		get => _ditherMode;
		set => _ditherMode = value;
	}

	public FurinaComputeBackend ComputeBackend
	{
		get => _computeBackend;
		set => _computeBackend = value;
	}

	public double LastProcessTimeMs => _lastProcessMs;
	public double LastCpuLoadPercent => _lastCpuLoadPct;
	public int ActiveStageCount => _cascadeStages.Length > 0 ? _cascadeStages.Length : (_targetSampleRate != _sourceSampleRate ? 1 : 0);

	public FurinaPlayerPcmResampler()
	{
		for (int c = 0; c < MaxChannels; c++)
		{
			_noiseErr[c] = new float[16];
			_arbHistory[c] = new float[ArbitraryTaps];
			_channelInput[c] = new float[8192];
			_channelOutput[c] = new float[65536];
		}
		RebuildStages(resetHistory: true);
	}

	public void Prepare(int sourceSampleRate, int targetSampleRate = 0)
	{
		lock (_configLock)
		{
			_sourceSampleRate = Math.Max(8000, sourceSampleRate);
			if (targetSampleRate > 0)
			{
				_targetSampleRate = Math.Clamp(targetSampleRate, 8000, 384000);
			}
			else if (_targetSampleRate <= 0)
			{
				_targetSampleRate = _sourceSampleRate;
			}
			RebuildStages(resetHistory: true);
		}
	}

	public void ApplyConfig(FurinaSrcRuntimeConfig config, int sourceSampleRate, int negotiatedTargetRate)
	{
		if (config == null) return;
		lock (_configLock)
		{
			bool rateChanged = _sourceSampleRate != Math.Max(8000, sourceSampleRate) ||
			                   _targetSampleRate != Math.Clamp(negotiatedTargetRate, 8000, 384000);

			_sourceSampleRate = Math.Max(8000, sourceSampleRate);
			_targetSampleRate = Math.Clamp(negotiatedTargetRate, 8000, 384000);
			_enabled = !string.Equals(config.Mode, "off", StringComparison.OrdinalIgnoreCase) && _targetSampleRate != _sourceSampleRate;
			_compareBypassed = config.AbCompareBypass;
			_quality = config.QualityProfile;
			_filter1xId = FurinaSrcCatalog.NormalizeProfileId(config.Filter1x, "furina-gauss-long");
			_filterNxId = FurinaSrcCatalog.NormalizeProfileId(config.FilterNx, "furina-hb");
			_mode = FurinaSrcCatalog.GetProfile(_filter1xId).LegacyMode;
			_ditherMode = config.DitherMode;
			_computeBackend = config.ComputeBackend;

			RebuildStages(resetHistory: rateChanged);
		}
	}

	public void Reset()
	{
		lock (_configLock)
		{
			_phase = 0.0;
			_firstBlockPending = true;
			foreach (var st in _cascadeStages) st.Reset();
			for (int c = 0; c < MaxChannels; c++)
			{
				Array.Clear(_arbHistory[c], 0, _arbHistory[c].Length);
				Array.Clear(_noiseErr[c], 0, _noiseErr[c].Length);
			}
			Array.Clear(_noiseErrPos, 0, _noiseErrPos.Length);
		}
	}

	public FurinaSrcRuntimeTelemetry GetTelemetry(int outputDeviceSampleRate = 0, bool isFallbackRate = false)
	{
		lock (_configLock)
		{
			bool isActive = Enabled;
			string slotId = _sourceSampleRate < 50000 ? _filter1xId : _filterNxId;
			var spec = FurinaSrcCatalog.GetProfile(slotId);
			int totalTaps = 0;
			foreach (var p in _stagePlans) totalTaps += p.RuntimeTaps;
			if (totalTaps == 0 && isActive) totalTaps = ArbitraryTaps;

			double latencyMs = 0.0;
			foreach (var p in _stagePlans)
			{
				double groupSamples = p.Phase == FurinaSrcPhaseType.Linear ? (p.RuntimeTaps - 1) * 0.5 : p.RuntimeTaps * 0.08;
				latencyMs += groupSamples * 1000.0 / Math.Max(8000, p.TargetRate);
			}

			string stageSummary = !isActive
				? "Native Pass-through"
				: (_compareBypassed
					? "A/B Linear Baseline Compare"
					: (_cascadeStages.Length > 0
						? string.Join(" -> ", BuildStageRateChain())
						: $"{_sourceSampleRate / 1000.0:0.#}k -> {_targetSampleRate / 1000.0:0.#}k Polyphase"));

			return new FurinaSrcRuntimeTelemetry
			{
				Active = isActive,
				AbCompareBypassed = _compareBypassed,
				SourceSampleRate = _sourceSampleRate,
				TargetSampleRate = _targetSampleRate,
				OutputDeviceSampleRate = outputDeviceSampleRate > 0 ? outputDeviceSampleRate : _targetSampleRate,
				ActiveSlot = isActive ? (_sourceSampleRate < 50000 ? "1X" : "NX") : "Standby",
				ActiveProfileId = spec.Id,
				ActiveProfileName = spec.DisplayName,
				PhaseLabel = spec.Phase == FurinaSrcPhaseType.Minimum ? "Minimum phase" : "Linear phase",
				StageCount = ActiveStageCount,
				TotalTaps = totalTaps,
				AttenuationDb = _quality switch
				{
					FurinaSrcQualityProfile.Transparent => spec.AttenuationDb,
					FurinaSrcQualityProfile.Balanced => Math.Min(spec.AttenuationDb, 128),
					_ => Math.Min(spec.AttenuationDb, 102)
				},
				LatencyMs = latencyMs,
				ProcessTimeMs = _lastProcessMs,
				CpuLoadPercent = _lastCpuLoadPct,
				SimdWidthBits = Vector.IsHardwareAccelerated ? Vector<float>.Count * 32 : 32,
				IsMultiThreaded = _computeBackend == FurinaComputeBackend.CpuMultiThread,
				IsFallbackRate = isFallbackRate,
				StageSummary = stageSummary
			};
		}
	}

	private IEnumerable<string> BuildStageRateChain()
	{
		if (_stagePlans.Count == 0) yield break;
		yield return $"{_stagePlans[0].SourceRate / 1000.0:0.#}k";
		foreach (var p in _stagePlans) yield return $"{p.TargetRate / 1000.0:0.#}k";
	}

	private static string MapLegacyModeToProfileId(ResamplerFilterMode mode) => mode switch
	{
		ResamplerFilterMode.PolySincHalfBand => "furina-hb",
		ResamplerFilterMode.PolySincExtended => "furina-apod-fast",
		ResamplerFilterMode.SincPolyphase => "furina-sinc-long",
		ResamplerFilterMode.GaussLong => "furina-gauss-long",
		ResamplerFilterMode.GaussShort => "furina-gauss-short",
		ResamplerFilterMode.Steep => "furina-steep-long",
		ResamplerFilterMode.MinPhase => "furina-apod-minring",
		_ => "furina-gauss-long"
	};

	private void RebuildStages(bool resetHistory)
	{
		if (_sourceSampleRate <= 0 || _targetSampleRate <= 0 || _sourceSampleRate == _targetSampleRate)
		{
			_cascadeStages = Array.Empty<TwoXStage>();
			_stagePlans = Array.Empty<FurinaSrcStagePlan>();
			if (resetHistory) _phase = 0.0;
			return;
		}

		var plans = FurinaSrcCatalog.CreateStagePlans(
			_sourceSampleRate,
			_targetSampleRate,
			_filter1xId,
			_filterNxId,
			_quality);
		_stagePlans = plans;

		if (plans.Count > 0 && plans[0].UpsampleFactor == 2)
		{
			var stages = new TwoXStage[plans.Count];
			for (int i = 0; i < plans.Count; i++)
			{
				var plan = plans[i];
				var spec = FurinaSrcCatalog.GetProfile(plan.ProfileId);
				// Synthesize 2x lowpass taps with overall gain=2.0 (for zero-stuffed 1:2 expansion),
				// then decompose into even & odd polyphase branches.
				int oddTaps = plan.RuntimeTaps | 1;
				// Ensure sub-branch length is clean (pad by 1 zero at tail if needed)
				float[] full = FurinaSrcCatalog.SynthesizeLowpassTaps(spec, oddTaps, plan.NormalizedCutoff, gain: 2.0);
				int halfLen = (full.Length + 1) / 2;

				var evenBranch = new float[halfLen];
				var oddBranch = new float[halfLen];
				float sumEven = 0f;
				float sumOdd = 0f;
				for (int k = 0; k < halfLen; k++)
				{
					int eIdx = k * 2;
					int oIdx = k * 2 + 1;
					evenBranch[k] = eIdx < full.Length ? full[eIdx] : 0f;
					oddBranch[k] = oIdx < full.Length ? full[oIdx] : 0f;
					sumEven += evenBranch[k];
					sumOdd += oddBranch[k];
				}

				// Strictly normalize both polyphase branches to exact unity DC gain (prevents fs/2 carrier ripple)
				if (Math.Abs(sumEven) > 1e-6f)
				{
					float inv = 1.0f / sumEven;
					for (int k = 0; k < halfLen; k++) evenBranch[k] *= inv;
				}
				if (Math.Abs(sumOdd) > 1e-6f)
				{
					float inv = 1.0f / sumOdd;
					for (int k = 0; k < halfLen; k++) oddBranch[k] *= inv;
				}

				// Reverse each branch for forward contiguous Vector<float> SIMD convolution
				var evenRev = new float[halfLen];
				var oddRev = new float[halfLen];
				for (int k = 0; k < halfLen; k++)
				{
					evenRev[k] = evenBranch[halfLen - 1 - k];
					oddRev[k] = oddBranch[halfLen - 1 - k];
				}

				stages[i] = new TwoXStage(i, spec.Id, evenRev, oddRev);
			}
			_cascadeStages = stages;
			_arbitraryTable = Array.Empty<float[]>();
		}
		else
		{
			_cascadeStages = Array.Empty<TwoXStage>();
			var plan = plans.Count > 0 ? plans[0] : new FurinaSrcStagePlan(
				0, _filter1xId, _sourceSampleRate, _targetSampleRate, 1, ArbitraryTaps, 0.45, FurinaSrcPhaseType.Linear, 130);
			var spec = FurinaSrcCatalog.GetProfile(plan.ProfileId);
			_arbitraryTable = BuildArbitraryPolyphaseTable(spec, plan.NormalizedCutoff);
		}

		if (resetHistory)
		{
			_phase = 0.0;
			_firstBlockPending = true;
			foreach (var st in _cascadeStages) st.Reset();
			for (int c = 0; c < MaxChannels; c++)
			{
				Array.Clear(_arbHistory[c], 0, _arbHistory[c].Length);
				Array.Clear(_noiseErr[c], 0, _noiseErr[c].Length);
			}
			Array.Clear(_noiseErrPos, 0, _noiseErrPos.Length);
		}
	}

	private static float[][] BuildArbitraryPolyphaseTable(FurinaSrcFilterProfileSpec spec, double cutoffNorm)
	{
		var table = new float[ArbitraryPhases][];
		int center = ArbitraryTaps / 2 - 1;
		double bw = Math.Clamp(cutoffNorm * 2.0, 0.15, 0.985);

		for (int p = 0; p < ArbitraryPhases; p++)
		{
			double mu = (double)p / ArbitraryPhases;
			var kernel = new double[ArbitraryTaps];
			double sum = 0.0;
			for (int k = 0; k < ArbitraryTaps; k++)
			{
				double t = (k - center) - mu;
				double sincVal = Math.Abs(t) < 1e-9
					? 1.0
					: Math.Sin(Math.PI * bw * t) / (Math.PI * bw * t);
				double w = FurinaSrcCatalog.ComputeWindow(k, ArbitraryTaps, spec);
				kernel[k] = sincVal * w;
				sum += kernel[k];
			}

			if (Math.Abs(sum) > 1e-12)
			{
				double inv = 1.0 / sum;
				for (int k = 0; k < ArbitraryTaps; k++) kernel[k] *= inv;
			}

			var f32 = new float[ArbitraryTaps];
			for (int k = 0; k < ArbitraryTaps; k++) f32[k] = (float)kernel[k];
			table[p] = f32;
		}
		return table;
	}

	/// <summary>
	/// Allocates and returns resampled interleaved float samples (backwards compatible API for offline &amp; test callers).
	/// </summary>
	public float[] ResampleInterleaved(float[] input, int offset, int count, int channels, out int outputCount)
	{
		if (!Enabled || _sourceSampleRate == _targetSampleRate || count <= 0 || channels <= 0)
		{
			outputCount = count;
			return input;
		}

		int inputFrames = count / channels;
		if (inputFrames <= 0)
		{
			outputCount = 0;
			return Array.Empty<float>();
		}

		if (_cascadeStages.Length > 0 && !_compareBypassed && _mode != ResamplerFilterMode.Linear && _mode != ResamplerFilterMode.HermiteCubic)
		{
			int factor = 1 << _cascadeStages.Length;
			int exactOutFrames = inputFrames * factor;
			var output = new float[exactOutFrames * channels];
			int produced = ProcessExactCascade(input, offset, inputFrames, output, 0, channels);
			outputCount = produced * channels;
			return output;
		}

		double ratio = (double)_sourceSampleRate / _targetSampleRate;
		int estFrames = (int)Math.Ceiling(inputFrames / ratio) + 16;
		var arbOut = new float[estFrames * channels];
		int producedFrames = ProcessArbitraryBlock(input, offset, inputFrames, arbOut, 0, estFrames, channels);
		outputCount = producedFrames * channels;
		return arbOut;
	}

	/// <summary>
	/// Processes exact power-of-2 cascade (2x/4x/8x) directly into pre-allocated output buffer (zero GC allocations).
	/// </summary>
	public int ProcessExactCascade(float[] input, int inputOffset, int inputFrames, float[] output, int outputOffset, int channels)
	{
		lock (_configLock)
		{
			if (_cascadeStages.Length == 0 || inputFrames <= 0 || channels <= 0) return 0;

			long startTick = Stopwatch.GetTimestamp();
			int chCount = Math.Min(channels, MaxChannels);
			int factor = 1 << _cascadeStages.Length;
			int outFrames = inputFrames * factor;

			EnsureDeinterleaveBuffers(chCount, inputFrames, outFrames);

			// 1. Deinterleave input
			for (int f = 0; f < inputFrames; f++)
			{
				int baseIdx = inputOffset + f * channels;
				for (int c = 0; c < chCount; c++)
				{
					_channelInput[c][f] = input[baseIdx + c];
				}
			}

			// 2. Process all channels through 2x stage cascade (parallelizable when CPU MultiThread enabled)
			bool useMt = _computeBackend == FurinaComputeBackend.CpuMultiThread && chCount == 2 && outFrames >= 1024;
			if (useMt)
			{
				Parallel.Invoke(
					() => RunCascadeForChannel(0, inputFrames),
					() => RunCascadeForChannel(1, inputFrames));
			}
			else
			{
				for (int c = 0; c < chCount; c++)
				{
					RunCascadeForChannel(c, inputFrames);
				}
			}

			// 3. Apply Noise-Shaping / Dither & Re-interleave into destination
			var dither = _ditherMode;
			for (int f = 0; f < outFrames; f++)
			{
				int outBase = outputOffset + f * channels;
				for (int c = 0; c < chCount; c++)
				{
					output[outBase + c] = ApplyDitherSample(_channelOutput[c][f], c, dither);
				}
				for (int c = chCount; c < channels; c++)
				{
					output[outBase + c] = 0f;
				}
			}

			double elapsedMs = (Stopwatch.GetTimestamp() - startTick) * 1000.0 / Stopwatch.Frequency;
			UpdateTelemetryTiming(elapsedMs, outFrames, _targetSampleRate);
			return outFrames;
		}
	}

	private void RunCascadeForChannel(int c, int inputFrames)
	{
		float[] curIn = _channelInput[c];
		int curFrames = inputFrames;

		for (int s = 0; s < _cascadeStages.Length; s++)
		{
			var stage = _cascadeStages[s];
			stage.EnsureWorkCapacity(curFrames);
			float[] work = stage.Work[c];
			float[] hist = stage.History[c];
			int histLen = hist.Length; // SubTaps - 1

			// Copy overlap history (or prime initial boundary with first sample to avoid hard-step Gibbs transient)
			if (histLen > 0)
			{
				if (!stage.IsPrimed[c])
				{
					stage.IsPrimed[c] = true;
					float edge = curFrames > 0 ? curIn[0] : 0f;
					Array.Fill(work, edge, 0, histLen);
				}
				else
				{
					Array.Copy(hist, 0, work, 0, histLen);
				}
			}
			Array.Copy(curIn, 0, work, histLen, curFrames);

			// Save tail into history for next block
			if (histLen > 0)
			{
				Array.Copy(work, curFrames, hist, 0, histLen);
			}

			float[] nextBuf = (s == _cascadeStages.Length - 1) ? _channelOutput[c] : EnsureStageIntermediateBuffer(c, s, curFrames * 2);
			int subTaps = stage.SubTaps;
			float[] evenTaps = stage.EvenRevTaps;
			float[] oddTaps = stage.OddRevTaps;

			for (int i = 0; i < curFrames; i++)
			{
				nextBuf[i * 2] = SimdDotProduct(work, i, evenTaps, subTaps);
				nextBuf[i * 2 + 1] = SimdDotProduct(work, i, oddTaps, subTaps);
			}

			curIn = nextBuf;
			curFrames *= 2;
		}
	}

	private readonly float[][][] _intermediateStageBufs =
	{
		new float[MaxChannels][],
		new float[MaxChannels][],
		new float[MaxChannels][]
	};

	private float[] EnsureStageIntermediateBuffer(int channel, int stageIdx, int neededFrames)
	{
		int slot = Math.Clamp(stageIdx, 0, 2);
		float[]? buf = _intermediateStageBufs[slot][channel];
		if (buf == null || buf.Length < neededFrames)
		{
			buf = new float[neededFrames + 512];
			_intermediateStageBufs[slot][channel] = buf;
		}
		return buf;
	}

	private void EnsureDeinterleaveBuffers(int chCount, int inFrames, int outFrames)
	{
		for (int c = 0; c < chCount; c++)
		{
			if (_channelInput[c].Length < inFrames) _channelInput[c] = new float[inFrames + 512];
			if (_channelOutput[c].Length < outFrames) _channelOutput[c] = new float[outFrames + 1024];
		}
	}

	/// <summary>
	/// Processes arbitrary-ratio block (e.g. 44.1k->48k or A/B linear compare or fractional downsample).
	/// Updates _phase cleanly so continuous streaming across buffers never clicks.
	/// </summary>
	public int ProcessArbitraryBlock(
		float[] input,
		int inputOffset,
		int inputFrames,
		float[] output,
		int outputOffset,
		int maxOutputFrames,
		int channels)
	{
		lock (_configLock)
		{
			if (inputFrames <= 0 || channels <= 0 || maxOutputFrames <= 0) return 0;

			long startTick = Stopwatch.GetTimestamp();
			double ratio = (double)_sourceSampleRate / _targetSampleRate;
			if (_firstBlockPending)
			{
				_firstBlockPending = false;
				for (int c = 0; c < Math.Min(channels, MaxChannels); c++)
				{
					Array.Fill(_arbHistory[c], input[inputOffset + c]);
				}
			}
			int outFrame = 0;
			double currentInputPos = _phase;
			float[][] table = _arbitraryTable;
			bool useLinearBaseline = _compareBypassed || _mode == ResamplerFilterMode.Linear || table.Length == 0;
			bool useHermite = !_compareBypassed && _mode == ResamplerFilterMode.HermiteCubic;

			while (currentInputPos < inputFrames && outFrame < maxOutputFrames)
			{
				int intPos = (int)Math.Floor(currentInputPos);
				float frac = (float)(currentInputPos - intPos);

				for (int c = 0; c < channels; c++)
				{
					int chIdx = Math.Min(c, MaxChannels - 1);
					float sample;

					if (useHermite)
					{
						float xm1 = GetSampleWithHistory(input, inputOffset, inputFrames, intPos - 1, c, channels);
						float x0 = GetSampleWithHistory(input, inputOffset, inputFrames, intPos, c, channels);
						float x1 = GetSampleWithHistory(input, inputOffset, inputFrames, intPos + 1, c, channels);
						float x2 = GetSampleWithHistory(input, inputOffset, inputFrames, intPos + 2, c, channels);
						sample = InterpolateHermite(xm1, x0, x1, x2, frac);
					}
					else if (useLinearBaseline)
					{
						float x0 = GetSampleWithHistory(input, inputOffset, inputFrames, intPos, c, channels);
						float x1 = GetSampleWithHistory(input, inputOffset, inputFrames, intPos + 1, c, channels);
						sample = x0 + frac * (x1 - x0);
					}
					else
					{
						int phaseIdx = Math.Clamp((int)(frac * ArbitraryPhases), 0, ArbitraryPhases - 1);
						float[] kernel = table[phaseIdx];
						sample = ConvolveArbitrary(input, inputOffset, inputFrames, intPos, c, channels, kernel);
					}

					output[outputOffset + outFrame * channels + c] = _compareBypassed
						? Math.Clamp(sample, -1.0f, 1.0f)
						: ApplyDitherSample(sample, chIdx, _ditherMode);
				}

				currentInputPos += ratio;
				outFrame++;
			}

			// Persist overlap tail for seamless continuity on next block
			for (int c = 0; c < Math.Min(channels, MaxChannels); c++)
			{
				float[] hist = _arbHistory[c];
				for (int k = 0; k < ArbitraryTaps; k++)
				{
					int idx = inputFrames - ArbitraryTaps + k;
					hist[k] = idx >= 0
						? input[inputOffset + idx * channels + c]
						: (idx + ArbitraryTaps >= 0 ? hist[idx + ArbitraryTaps] : 0f);
				}
			}

			_phase = currentInputPos - inputFrames;
			double elapsedMs = (Stopwatch.GetTimestamp() - startTick) * 1000.0 / Stopwatch.Frequency;
			UpdateTelemetryTiming(elapsedMs, outFrame, _targetSampleRate);
			return outFrame;
		}
	}

	private void UpdateTelemetryTiming(double elapsedMs, int outFrames, int sampleRate)
	{
		double budgetMs = outFrames * 1000.0 / Math.Max(8000, sampleRate);
		double cpuPct = budgetMs > 0.001 ? Math.Clamp((elapsedMs / budgetMs) * 100.0, 0.0, 100.0) : 0.0;
		_lastProcessMs = _lastProcessMs * 0.75 + elapsedMs * 0.25;
		_lastCpuLoadPct = _lastCpuLoadPct * 0.75 + cpuPct * 0.25;
	}

	private static float SimdDotProduct(float[] a, int aOffset, float[] b, int length)
	{
		int vecWidth = Vector<float>.Count;
		int i = 0;
		float sum = 0f;

		if (Vector.IsHardwareAccelerated && length >= vecWidth)
		{
			var vAcc = Vector<float>.Zero;
			int limit = length - (length % vecWidth);
			for (; i < limit; i += vecWidth)
			{
				var va = new Vector<float>(a, aOffset + i);
				var vb = new Vector<float>(b, i);
				vAcc += va * vb;
			}
			sum = Vector.Dot(vAcc, Vector<float>.One);
		}

		for (; i < length; i++)
		{
			sum += a[aOffset + i] * b[i];
		}
		return sum;
	}

	private float GetSampleWithHistory(float[] input, int offset, int inputFrames, int frameIdx, int channel, int channels)
	{
		if (frameIdx >= 0 && frameIdx < inputFrames)
		{
			return input[offset + frameIdx * channels + channel];
		}
		if (frameIdx < 0)
		{
			int histIdx = ArbitraryTaps + frameIdx;
			if (histIdx >= 0 && histIdx < ArbitraryTaps && channel < MaxChannels)
			{
				return _arbHistory[channel][histIdx];
			}
			return input[offset + channel];
		}
		return input[offset + (inputFrames - 1) * channels + channel];
	}

	private float ConvolveArbitrary(float[] input, int offset, int inputFrames, int intPos, int channel, int channels, float[] kernel)
	{
		const int center = ArbitraryTaps / 2 - 1;
		int startFrame = intPos - center;
		int endFrame = startFrame + ArbitraryTaps - 1;

		// Fast SIMD path when the entire 64-tap window falls inside current contiguous channel extraction or bounds
		if (_arbScratchIn.Length < ArbitraryTaps) _arbScratchIn = new float[ArbitraryTaps];

		if (startFrame >= 0 && endFrame < inputFrames)
		{
			for (int k = 0; k < ArbitraryTaps; k++)
			{
				_arbScratchIn[k] = input[offset + (startFrame + k) * channels + channel];
			}
			return SimdDotProduct(_arbScratchIn, 0, kernel, ArbitraryTaps);
		}

		float acc = 0f;
		for (int k = 0; k < ArbitraryTaps; k++)
		{
			acc += GetSampleWithHistory(input, offset, inputFrames, startFrame + k, channel, channels) * kernel[k];
		}
		return acc;
	}

	private static float InterpolateHermite(float xm1, float x0, float x1, float x2, float t)
	{
		float c0 = x0;
		float c1 = 0.5f * (x1 - xm1);
		float c2 = xm1 - 2.5f * x0 + 2.0f * x1 - 0.5f * x2;
		float c3 = 0.5f * (x2 - xm1) + 1.5f * (x0 - x1);
		return ((c3 * t + c2) * t + c1) * t + c0;
	}

	private float NextUniformFloat()
	{
		// Xorshift32: ultra fast, zero locks/allocations on audio thread
		uint x = _rngState;
		x ^= x << 13;
		x ^= x >> 17;
		x ^= x << 5;
		_rngState = x;
		return (x & 0xFFFFFFu) / 16777216.0f;
	}

	private float ApplyDitherSample(float val, int chIdx, FurinaDitherMode mode)
	{
		if (mode == FurinaDitherMode.Off)
		{
			return Math.Clamp(val, -1.0f, 1.0f);
		}

		const float lsb24 = 1.0f / (1 << 23);
		float tpdf = (NextUniformFloat() + NextUniformFloat() - 1.0f) * lsb24;

		if (mode == FurinaDitherMode.Tpdf)
		{
			return Math.Clamp(val + tpdf, -1.0f, 1.0f);
		}

		float[] err = _noiseErr[chIdx];
		int pos = _noiseErrPos[chIdx];

		if (mode == FurinaDitherMode.HighPass)
		{
			float shaped = tpdf - 0.5f * err[0];
			err[0] = Math.Clamp(tpdf, -4.0f * lsb24, 4.0f * lsb24);
			return Math.Clamp(val + shaped, -1.0f, 1.0f);
		}

		// High-order psychoacoustic IIR error feedback (NS-5 / NS-9 / Ultra-Shaped)
		float[] coeffs = mode switch
		{
			FurinaDitherMode.Ns5 => Ns5Coeffs,
			FurinaDitherMode.Ns9 => Ns9Coeffs,
			_ => UltraShapedCoeffs
		};

		float feedback = 0f;
		for (int k = 0; k < coeffs.Length; k++)
		{
			feedback += coeffs[k] * err[(pos - k - 1) & 15];
		}

		float ideal = val + feedback;
		// Emulate 24-bit shaped dither quantizer step while staying inside float32 headroom
		float dithered = ideal + tpdf;
		float quant = MathF.Round(dithered / lsb24) * lsb24;
		float qErr = Math.Clamp(quant - ideal, -6.0f * lsb24, 6.0f * lsb24);

		err[pos & 15] = qErr;
		_noiseErrPos[chIdx] = (pos + 1) & 15;

		return Math.Clamp(val + qErr * 0.5f + tpdf * 0.5f, -1.0f, 1.0f);
	}
}
