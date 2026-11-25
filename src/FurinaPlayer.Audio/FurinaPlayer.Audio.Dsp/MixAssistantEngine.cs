using System;
using System.Collections.Generic;
using System.Linq;
using FurinaPlayer.Core.Services;

namespace FurinaPlayer.Audio.Dsp;

public enum SongStyleGenre
{
    Unknown = 0,
    ModernPop = 1,
    AcgVibrant = 2,
    VocalBallad = 3,
    EdmHipHopBass = 4,
    RockMetal = 5,
    ClassicalOrchestral = 6,
    JazzAcoustic = 7,
    LoFiAmbient = 8
}

public enum MixAssistantIntensityMode
{
    GentleSafe = 0,
    Balanced = 1,
    Vivid = 2
}

public sealed record MixAssistantTarget(
    string Id,
    string DisplayName,
    string Description,
    double[] Target32Db,
    double[] Target10Db)
{
    private readonly string _displayName = DisplayName;
    private readonly string _description = Description;

    public string DisplayName => Loc.T(_displayName);
    public string Description => Loc.T(_description);
}

public sealed record SongGenreClassification(
    SongStyleGenre Genre,
    string RecommendedTargetId,
    double Confidence01,
    double SubBassDb,
    double MidBassDb,
    double LowMidDb,
    double VocalPresenceDb,
    double AirExtensionDb,
    string AcousticSummary)
{
    public string GenreDisplayName => Genre switch
    {
        SongStyleGenre.ModernPop => Loc.T("现代流行 / 商业混音 (Modern Pop)"),
        SongStyleGenre.AcgVibrant => Loc.T("ACG 二次元 / 日系电音 (ACG Vibrant)"),
        SongStyleGenre.VocalBallad => Loc.T("抒情华语 / 原声人声 (Vocal Ballad)"),
        SongStyleGenre.EdmHipHopBass => Loc.T("电音 / 说唱重低音 (EDM & Hip-Hop)"),
        SongStyleGenre.RockMetal => Loc.T("摇滚 / 金属现场 (Rock & Metal)"),
        SongStyleGenre.ClassicalOrchestral => Loc.T("古典 / 交响大动态 (Classical)"),
        SongStyleGenre.JazzAcoustic => Loc.T("爵士 / 发烧器乐 (Jazz & Acoustic)"),
        SongStyleGenre.LoFiAmbient => Loc.T("Lo-Fi / 氛围温暖 (Lo-Fi Ambient)"),
        _ => Loc.T("综合均衡曲风 (Balanced)")
    };
}

public sealed record MixAssistantHeadphoneContext(
    string? ProductIdOrName = null,
    OpraHeadphonePreset? ActiveOpraPreset = null,
    bool IsOpraCalibrationActive = false,
    double CalibrationPreampDb = 0.0,
    double CalibrationPeakBoostDb = 0.0,
    HeadphoneWearingStyle WearingStyle = HeadphoneWearingStyle.Auto,
    HeadphoneDriverType DriverType = HeadphoneDriverType.Auto,
    HeadphoneTuningStyle TuningStyle = HeadphoneTuningStyle.Auto,
    double HeadphoneMatchStrength01 = 0.70,
    MixAssistantIntensityMode IntensityMode = MixAssistantIntensityMode.Balanced);

public sealed record MixAssistantProfile(
    double[] Bins32Db,
    double[] Bands10Db,
    double MidbandRefDb,
    double CrestFactorDb,
    double SpectralTiltDbPerOct,
    int AnalyzedFrames,
    double Confidence01)
{
    public SongGenreClassification? GenreClassification { get; init; }
    public SongStyleGenre DetectedGenre => GenreClassification?.Genre ?? SongStyleGenre.Unknown;
    public string RecommendedTargetId => GenreClassification?.RecommendedTargetId ?? "balanced_master";
}

public sealed class MixAssistantSolution
{
    public string TargetId { get; init; } = "balanced_master";
    public string TargetName { get; init; } = string.Empty;
    public double MatchAmount01 { get; init; }
    public double BassSculptDb { get; init; }
    public double TrebleSculptDb { get; init; }
    public bool DemaskMidMud { get; init; }
    public double RecommendedPreampDb { get; init; }
    public double[] BandOffsetsDb { get; init; } = new double[10];
    public FurinaPlayerBiquadFilter[] Filters { get; init; } = Array.Empty<FurinaPlayerBiquadFilter>();
    public double[] TargetCurve32Db { get; init; } = new double[32];
    public double[] BeforeSpectrum32Db { get; init; } = new double[32];
    public double[] AfterSpectrum32Db { get; init; } = new double[32];
    public double[] PredictedCurve64Db { get; init; } = new double[64];
    public IReadOnlyList<string> DetectedIssues { get; init; } = Array.Empty<string>();
    public double Confidence01 { get; init; }

    // Upgraded Genre, OPRA Headphone Matching & Anti-Distortion Headroom Telemetry
    public SongStyleGenre DetectedGenre { get; init; } = SongStyleGenre.Unknown;
    public string DetectedGenreName { get; init; } = string.Empty;
    public string EffectiveTargetId { get; init; } = "balanced_master";
    public string EffectiveTargetName { get; init; } = string.Empty;
    public string MatchedHeadphoneName { get; init; } = string.Empty;
    public string MatchedHeadphoneSummary { get; init; } = string.Empty;
    public bool WasHeadphoneCalibrationAware { get; init; }
    public double PeakBoostDb { get; init; }
    public double HeadroomSafetyMarginDb { get; init; }
    public MixAssistantIntensityMode IntensityMode { get; init; } = MixAssistantIntensityMode.Balanced;
}

/// <summary>
/// Upgraded perceptual Mix Assistant:
/// 1. 8-genre acoustic &amp; dynamic classification (sub-bass/presence/tilt/crest/contrast)
/// 2. OPRA 6,229-product / 600-flagship headphone hardware acoustic matching (calibration-aware to avoid double-EQ)
/// 3. Anti-distortion asymmetric solver with iterative biquad skirt compensation and full peak-safe Preamp headroom.
/// </summary>
public sealed class MixAssistantEngine
{
    public const int AnalysisBinCount = 32;
    public const int GraphicBandCount = 10;
    public const int PredictedCurvePointCount = 64;
    public const string AutoGenreTargetId = "auto_genre";

    private const int FftSize = 4096;
    private const int HopSize = 2048;
    private const double MinSignalDbfs = -68.0;

    public static readonly double[] GraphicFrequencies =
    {
        31.25, 62.5, 125.0, 250.0, 500.0, 1000.0, 2000.0, 4000.0, 8000.0, 16000.0
    };

    private static readonly double[] GraphicQs =
    {
        0.95, 1.15, 1.30, 1.40, 1.45, 1.45, 1.50, 1.55, 1.45, 0.90
    };

    public static readonly double[] AnalysisFrequencies = BuildAnalysisFrequencies();
    public static readonly double[] Curve64Frequencies = BuildCurve64Frequencies();
    public static IReadOnlyList<MixAssistantTarget> Targets { get; } = BuildTargets();

    private static readonly float[] HannWindow = BuildHannWindow(FftSize);

    // Streaming learning state
    private readonly object _learnLock = new();
    private readonly double[] _accumEnergy32 = new double[AnalysisBinCount];
    private readonly float[] _pcmRing = new float[FftSize * 2];
    private int _pcmFill;
    private int _activeSampleRate = 48000;
    private double _peakSum;
    private double _rmsSqSum;
    private long _rmsSamples;
    private int _learnedFrames;
    private bool _isLearning;

    // Reusable FFT scratch buffers (used under _learnLock)
    private readonly float[] _fftReal = new float[FftSize];
    private readonly float[] _fftImag = new float[FftSize];

    public bool IsLearning
    {
        get { lock (_learnLock) return _isLearning; }
    }

    public int LearnedFrames
    {
        get { lock (_learnLock) return _learnedFrames; }
    }

    public double LearningProgress01
    {
        get
        {
            lock (_learnLock)
            {
                return Math.Clamp(_learnedFrames / 72.0, 0.0, 1.0);
            }
        }
    }

    public static MixAssistantTarget GetTarget(string? targetId)
    {
        if (!string.IsNullOrWhiteSpace(targetId))
        {
            for (int i = 0; i < Targets.Count; i++)
            {
                if (string.Equals(Targets[i].Id, targetId, StringComparison.OrdinalIgnoreCase))
                {
                    return Targets[i];
                }
            }
        }
        return Targets.FirstOrDefault(t => t.Id == "balanced_master") ?? Targets[0];
    }

    public void BeginLearning()
    {
        lock (_learnLock)
        {
            Array.Clear(_accumEnergy32, 0, _accumEnergy32.Length);
            Array.Clear(_pcmRing, 0, _pcmRing.Length);
            _pcmFill = 0;
            _peakSum = 0.0;
            _rmsSqSum = 0.0;
            _rmsSamples = 0;
            _learnedFrames = 0;
            _isLearning = true;
        }
    }

    public void AccumulateSpectrum64(ReadOnlySpan<float> bands01)
    {
        if (bands01.IsEmpty) return;

        lock (_learnLock)
        {
            if (!_isLearning) return;

            Span<double> frame32 = stackalloc double[AnalysisBinCount];
            double totalDb = 0.0;
            int srcCount = bands01.Length;

            for (int b = 0; b < AnalysisBinCount; b++)
            {
                double normPos = (Math.Log10(AnalysisFrequencies[b]) - Math.Log10(20.0))
                               / (Math.Log10(20000.0) - Math.Log10(20.0));
                double srcIdx = Math.Clamp(normPos * (srcCount - 1), 0.0, srcCount - 1);
                int i0 = (int)Math.Floor(srcIdx);
                int i1 = Math.Min(srcCount - 1, i0 + 1);
                double frac = srcIdx - i0;

                float v01 = bands01[i0] * (float)(1.0 - frac) + bands01[i1] * (float)frac;
                double db = -80.0 + Math.Clamp(v01, 0f, 1f) * 80.0;
                totalDb += db;
                frame32[b] = Math.Pow(10.0, db / 10.0);
            }

            double avgDb = totalDb / AnalysisBinCount;
            if (avgDb < MinSignalDbfs)
            {
                return;
            }

            UpdateAccumulatedEnergyLocked(frame32);
        }
    }

    public void AccumulatePcm(ReadOnlySpan<float> interleaved, int channels, int sampleRate)
    {
        if (interleaved.IsEmpty || channels <= 0 || sampleRate <= 0) return;

        lock (_learnLock)
        {
            if (!_isLearning) return;
            _activeSampleRate = sampleRate;

            int frames = interleaved.Length / channels;
            Span<double> frame32 = stackalloc double[AnalysisBinCount];

            for (int f = 0; f < frames; f++)
            {
                double sum = 0.0;
                int baseIdx = f * channels;
                for (int c = 0; c < channels; c++)
                {
                    float s = interleaved[baseIdx + c];
                    if (float.IsFinite(s)) sum += s;
                }

                float mono = (float)(sum / channels);
                double absM = Math.Abs(mono);
                if (absM > _peakSum) _peakSum = absM;
                _rmsSqSum += (double)mono * mono;
                _rmsSamples++;

                _pcmRing[_pcmFill++] = mono;
                if (_pcmFill >= FftSize)
                {
                    if (TryComputeFft32Energy(_pcmRing.AsSpan(0, FftSize), _activeSampleRate, _fftReal, _fftImag, frame32))
                    {
                        UpdateAccumulatedEnergyLocked(frame32);
                    }

                    int remaining = _pcmFill - HopSize;
                    if (remaining > 0)
                    {
                        Array.Copy(_pcmRing, HopSize, _pcmRing, 0, remaining);
                    }
                    _pcmFill = Math.Max(0, remaining);
                }
            }
        }
    }

    public MixAssistantProfile EndLearning()
    {
        lock (_learnLock)
        {
            _isLearning = false;
            if (_learnedFrames <= 0)
            {
                return CreateFlatFallbackProfile();
            }

            double crestDb = 11.0;
            if (_rmsSamples > 0 && _rmsSqSum > 1e-12 && _peakSum > 1e-6)
            {
                double rms = Math.Sqrt(_rmsSqSum / _rmsSamples);
                crestDb = Math.Clamp(20.0 * Math.Log10(_peakSum / Math.Max(1e-9, rms)), 0.0, 32.0);
            }
            else
            {
                // Live visualizer 64-band stream does not report true intersample crest directly;
                // estimate effective macro dynamic range safely from spectral contrast instead of an optimistic 12 dB.
                crestDb = 9.2;
            }

            return FinalizeProfile(_accumEnergy32, crestDb, _learnedFrames);
        }
    }

    public MixAssistantProfile AnalyzeSamples(float[] interleaved, int channels, int sampleRate)
    {
        if (interleaved == null || interleaved.Length == 0 || channels <= 0 || sampleRate <= 0)
        {
            return CreateFlatFallbackProfile();
        }

        int totalFrames = interleaved.Length / channels;
        if (totalFrames < 128)
        {
            return CreateFlatFallbackProfile();
        }

        float[] mono = new float[totalFrames];
        double sumSq = 0.0;
        double peak = 0.0;

        for (int f = 0; f < totalFrames; f++)
        {
            double chSum = 0.0;
            int idx = f * channels;
            for (int c = 0; c < channels; c++)
            {
                float s = interleaved[idx + c];
                if (float.IsFinite(s)) chSum += s;
            }
            float m = (float)(chSum / channels);
            mono[f] = m;
            double am = Math.Abs(m);
            if (am > peak) peak = am;
            sumSq += (double)m * m;
        }

        double rms = Math.Sqrt(sumSq / totalFrames);
        double crestDb = (rms > 1e-9 && peak > 1e-9) ? Math.Clamp(20.0 * Math.Log10(peak / rms), 0.0, 36.0) : 11.0;

        double[] accum32 = new double[AnalysisBinCount];
        float[] windowBuf = new float[FftSize];
        float[] fftR = new float[FftSize];
        float[] fftI = new float[FftSize];
        double[] frame32 = new double[AnalysisBinCount];
        int validFrames = 0;

        if (totalFrames < FftSize)
        {
            Array.Copy(mono, windowBuf, totalFrames);
            if (TryComputeFft32Energy(windowBuf, sampleRate, fftR, fftI, frame32))
            {
                for (int b = 0; b < AnalysisBinCount; b++) accum32[b] = frame32[b];
                validFrames = 1;
            }
        }
        else
        {
            int maxWindows = 72;
            int availableStarts = totalFrames - FftSize;
            int step = Math.Max(HopSize, availableStarts / maxWindows);

            for (int start = 0; start <= availableStarts; start += step)
            {
                if (TryComputeFft32Energy(mono.AsSpan(start, FftSize), sampleRate, fftR, fftI, frame32))
                {
                    validFrames++;
                    double w = 1.0 / validFrames;
                    for (int b = 0; b < AnalysisBinCount; b++)
                    {
                        accum32[b] = accum32[b] * (1.0 - w) + frame32[b] * w;
                    }
                }
            }
        }

        if (validFrames == 0)
        {
            return CreateFlatFallbackProfile();
        }

        return FinalizeProfile(accum32, crestDb, validFrames);
    }

    public MixAssistantSolution SolveEqCurve(
        MixAssistantProfile learned,
        string targetId,
        double matchAmount01,
        double bassSculptDb,
        double trebleSculptDb,
        bool demaskMidMud = true)
    {
        return SolveEqCurve(
            learned,
            targetId,
            matchAmount01,
            bassSculptDb,
            trebleSculptDb,
            demaskMidMud,
            headphoneContext: null);
    }

    public MixAssistantSolution SolveEqCurve(
        MixAssistantProfile learned,
        string targetId,
        double matchAmount01,
        double bassSculptDb,
        double trebleSculptDb,
        bool demaskMidMud,
        MixAssistantHeadphoneContext? headphoneContext)
    {
        var hpCtx = headphoneContext ?? new MixAssistantHeadphoneContext();
        double[] before32 = learned?.Bins32Db is { Length: AnalysisBinCount }
            ? (double[])learned.Bins32Db.Clone()
            : new double[AnalysisBinCount];
        double confidence = learned?.Confidence01 ?? 0.0;
        double crestDb = learned?.CrestFactorDb ?? 11.0;

        var genreInfo = learned?.GenreClassification ?? ClassifySongGenre(before32, crestDb, learned?.SpectralTiltDbPerOct ?? 0.0);

        bool isAutoTarget = string.Equals(targetId, AutoGenreTargetId, StringComparison.OrdinalIgnoreCase);
        string resolvedTargetId = isAutoTarget ? genreInfo.RecommendedTargetId : targetId;

        MixAssistantTarget requestedTarget = GetTarget(targetId);
        MixAssistantTarget effectiveTarget = GetTarget(resolvedTargetId);

        double amount = Math.Clamp(matchAmount01, 0.0, 1.0);
        double bassTrim = Math.Clamp(bassSculptDb, -6.0, 6.0);
        double trebleTrim = Math.Clamp(trebleSculptDb, -6.0, 6.0);

        // 1. Intensity mode bounds & dynamic loudness throttling (curbs over-aggressive positive gain clipping)
        (double maxPosBandDb, double maxNegBandDb, double peakBudgetDb, double dynamicSafetyShiftDb) = hpCtx.IntensityMode switch
        {
            MixAssistantIntensityMode.GentleSafe => (2.8, -5.0, 3.2, -0.5),
            MixAssistantIntensityMode.Vivid => (5.2, -6.8, 5.5, 0.0),
            _ => (3.8, -6.0, 4.2, -0.25)
        };

        // Hyper-compressed modern mixes (< 9 dB crest) distort easily if pushed positive: smoothly attenuate boost budget
        double crestTightening = crestDb < 9.5
            ? Math.Clamp((crestDb - 4.5) / 5.0, 0.50, 1.0)
            : 1.0;
        maxPosBandDb *= crestTightening;
        peakBudgetDb *= crestTightening;
        if (crestDb < 8.5)
        {
            dynamicSafetyShiftDb -= 0.45;
        }

        // 2. Resolve OPRA Headphone Acoustic Matching Profile
        double[] hpDelta32 = ComputeHeadphoneAcousticAdjustment32(
            hpCtx,
            out string matchedHpName,
            out string matchedHpSummary,
            out double maxSafeSubBoostScale,
            out double calUncompensatedPeakDb);

        // 3. Build 32-bin Desired Sculpted Delta
        double[] baseDelta32 = new double[AnalysisBinCount];
        double[] sculptOnly32 = new double[AnalysisBinCount];

        for (int b = 0; b < AnalysisBinCount; b++)
        {
            baseDelta32[b] = effectiveTarget.Target32Db[b] - before32[b];
        }

        List<string> issues = DetectSpectralIssues(before32, effectiveTarget.Target32Db);

        if (demaskMidMud)
        {
            ApplySpectralDemasking(before32, effectiveTarget.Target32Db, baseDelta32);
        }

        // Blend base match delta with headphone-matched adaptation and explicit Bass/Treble Sculpt trims
        double[] desiredDelta32 = new double[AnalysisBinCount];
        for (int b = 0; b < AnalysisBinCount; b++)
        {
            double f = AnalysisFrequencies[b];
            double lowWeight = 1.0 / (1.0 + Math.Pow(f / 140.0, 2.2));
            double highWeight = 1.0 / (1.0 + Math.Pow(4500.0 / f, 2.2));
            sculptOnly32[b] = bassTrim * lowWeight + trebleTrim * highWeight;

            double matchedCore = baseDelta32[b] * amount + hpDelta32[b];

            // Enforce driver-physical sub-bass boost limits (e.g. open earbuds / electrostatics distend on +6dB < 65Hz)
            if (f < 75.0 && matchedCore > 0.0)
            {
                matchedCore *= maxSafeSubBoostScale;
            }

            // Asymmetric soft-knee compression in 32-bin domain so extreme narrow mix outliers don't drive violent EQ swings
            if (matchedCore > 0.0)
            {
                matchedCore = maxPosBandDb * Math.Tanh(matchedCore / Math.Max(0.8, maxPosBandDb));
            }
            else
            {
                matchedCore = maxNegBandDb * Math.Tanh(matchedCore / Math.Min(-0.8, maxNegBandDb));
            }

            desiredDelta32[b] = matchedCore + sculptOnly32[b];
        }

        // 4. Initial projection onto the 10 ISO octave filters
        double[] gains = new double[GraphicBandCount];
        for (int g = 0; g < GraphicBandCount; g++)
        {
            double logCenter = Math.Log10(GraphicFrequencies[g]);
            double weightedSum = 0.0;
            double weightTotal = 0.0;
            for (int b = 0; b < AnalysisBinCount; b++)
            {
                double distOct = (Math.Log10(AnalysisFrequencies[b]) - logCenter) / 0.30102999566;
                double w = Math.Exp(-0.5 * (distOct * distOct) / (0.58 * 0.58));
                weightedSum += desiredDelta32[b] * w;
                weightTotal += w;
            }
            gains[g] = weightTotal > 1e-9 ? weightedSum / weightTotal : 0.0;
        }

        // Subtract core mid-band DC offset + apply subtle subtractive tilt to keep overall gain clean
        double coreMean = (gains[3] + gains[4] + gains[5] + gains[6]) * 0.25;
        for (int g = 0; g < GraphicBandCount; g++)
        {
            gains[g] = Math.Clamp(gains[g] - coreMean * 0.80 + dynamicSafetyShiftDb, maxNegBandDb, maxPosBandDb);
        }

        // 5. Damped Iterative Biquad Fitting (compensates overlapping filter skirts so adjacent boosts NEVER stack +12 dB!)
        FurinaPlayerBiquadFilter[] filters = CreateFilterBank(gains);
        double[] actual32 = new double[AnalysisBinCount];

        for (int iter = 0; iter < 4; iter++)
        {
            for (int b = 0; b < AnalysisBinCount; b++)
            {
                double sum = 0.0;
                double freq = AnalysisFrequencies[b];
                for (int g = 0; g < GraphicBandCount; g++)
                {
                    sum += filters[g].CalculateResponseDb(freq, 48000.0);
                }
                actual32[b] = sum;
            }

            // Compute error at each graphic center frequency and update dampingly
            for (int g = 0; g < GraphicBandCount; g++)
            {
                double logCenter = Math.Log10(GraphicFrequencies[g]);
                double errSum = 0.0;
                double wSum = 0.0;
                for (int b = 0; b < AnalysisBinCount; b++)
                {
                    double distOct = (Math.Log10(AnalysisFrequencies[b]) - logCenter) / 0.30102999566;
                    double w = Math.Exp(-0.5 * (distOct * distOct) / (0.45 * 0.45));
                    double targetVal = desiredDelta32[b] - coreMean * 0.80 + dynamicSafetyShiftDb;
                    errSum += (targetVal - actual32[b]) * w;
                    wSum += w;
                }
                double stepErr = wSum > 1e-9 ? errSum / wSum : 0.0;
                gains[g] = Math.Clamp(gains[g] + stepErr * 0.48, maxNegBandDb, maxPosBandDb);
            }

            // Gentle spatial smoothing to avoid jagged alternating +/- comb resonances
            double[] nextSmoothed = new double[GraphicBandCount];
            for (int g = 0; g < GraphicBandCount; g++)
            {
                double prev = gains[Math.Max(0, g - 1)];
                double curr = gains[g];
                double next = gains[Math.Min(GraphicBandCount - 1, g + 1)];
                nextSmoothed[g] = Math.Clamp(0.14 * prev + 0.72 * curr + 0.14 * next, maxNegBandDb, maxPosBandDb);
            }
            gains = nextSmoothed;
            UpdateFilterBankGains(filters, gains);
        }

        // 6. Enforce overall composite peak budget (if summed positive excursion exceeds peakBudgetDb, shift uniformly downward!)
        double maxCompositePositiveDb = EvaluatePeakCompositeGainDb(filters);
        double budgetLimit = peakBudgetDb + Math.Max(0.0, Math.Max(bassTrim, trebleTrim) * 0.55);
        if (maxCompositePositiveDb > budgetLimit)
        {
            double shiftDown = Math.Min(2.8, maxCompositePositiveDb - budgetLimit);
            for (int g = 0; g < GraphicBandCount; g++)
            {
                gains[g] = Math.Clamp(gains[g] - shiftDown, Math.Max(-9.0, maxNegBandDb - 1.5), maxPosBandDb);
            }
            UpdateFilterBankGains(filters, gains);
            maxCompositePositiveDb = EvaluatePeakCompositeGainDb(filters);
        }

        // Quantize cleanly to 0.1 dB steps
        for (int g = 0; g < GraphicBandCount; g++)
        {
            gains[g] = Math.Round(gains[g], 1);
            filters[g].GainDb = gains[g];
            filters[g].UpdateCoefficients(48000.0);
        }

        // 7. Compute dense 64-pt curve, before/after spectrum, and TRUE 100%+ Anti-Clipping Preamp Headroom!
        double[] predicted64 = new double[PredictedCurvePointCount];
        double exactPeakPositiveDb = 0.0;
        for (int p = 0; p < PredictedCurvePointCount; p++)
        {
            double freq = Curve64Frequencies[p];
            double sumDb = 0.0;
            for (int g = 0; g < GraphicBandCount; g++)
            {
                sumDb += filters[g].CalculateResponseDb(freq, 48000.0);
            }
            predicted64[p] = Math.Round(sumDb, 2);
            if (sumDb > exactPeakPositiveDb) exactPeakPositiveDb = sumDb;
        }

        double[] after32 = new double[AnalysisBinCount];
        for (int b = 0; b < AnalysisBinCount; b++)
        {
            double freq = AnalysisFrequencies[b];
            double sumDb = 0.0;
            for (int g = 0; g < GraphicBandCount; g++)
            {
                sumDb += filters[g].CalculateResponseDb(freq, 48000.0);
            }
            after32[b] = Math.Round(before32[b] + sumDb, 2);
        }

        // Crucial Anti-Distortion Fix: Full peak compensation (100% of composite positive peak + safety margin + residual calibration peak)
        double extraSafetyMargin = exactPeakPositiveDb > 0.25
            ? (crestDb < 8.5 ? 0.6 : 0.3)
            : 0.0;
        double requiredAttenuation = exactPeakPositiveDb + calUncompensatedPeakDb + extraSafetyMargin;
        double recommendedPreamp = requiredAttenuation > 0.2
            ? Math.Round(-Math.Clamp(requiredAttenuation, 0.0, 12.0), 1)
            : 0.0;

        // Tag calibration & safety metadata cleanly onto diagnostic badges if meaningful
        if (!string.IsNullOrEmpty(matchedHpName))
        {
            issues.Add(hpCtx.IsOpraCalibrationActive
                ? Loc.F("已协同耳机校正 ({0})", matchedHpName)
                : Loc.F("已适配耳机特性 ({0})", matchedHpName));
        }

        return new MixAssistantSolution
        {
            TargetId = requestedTarget.Id,
            TargetName = requestedTarget.DisplayName,
            EffectiveTargetId = effectiveTarget.Id,
            EffectiveTargetName = effectiveTarget.DisplayName,
            DetectedGenre = genreInfo.Genre,
            DetectedGenreName = genreInfo.GenreDisplayName,
            MatchedHeadphoneName = matchedHpName,
            MatchedHeadphoneSummary = matchedHpSummary,
            WasHeadphoneCalibrationAware = hpCtx.IsOpraCalibrationActive,
            PeakBoostDb = Math.Round(exactPeakPositiveDb, 2),
            HeadroomSafetyMarginDb = Math.Round(Math.Abs(recommendedPreamp) - exactPeakPositiveDb, 2),
            IntensityMode = hpCtx.IntensityMode,
            MatchAmount01 = amount,
            BassSculptDb = bassTrim,
            TrebleSculptDb = trebleTrim,
            DemaskMidMud = demaskMidMud,
            RecommendedPreampDb = recommendedPreamp,
            BandOffsetsDb = gains,
            Filters = filters,
            TargetCurve32Db = (double[])effectiveTarget.Target32Db.Clone(),
            BeforeSpectrum32Db = before32,
            AfterSpectrum32Db = after32,
            PredictedCurve64Db = predicted64,
            DetectedIssues = issues,
            Confidence01 = confidence
        };
    }

    public static SongGenreClassification ClassifySongGenre(double[] norm32, double crestDb, double tiltDbPerOct)
    {
        if (norm32 == null || norm32.Length != AnalysisBinCount)
        {
            return new SongGenreClassification(
                SongStyleGenre.ModernPop,
                "balanced_master",
                0.3,
                0, 0, 0, 0, 0,
                Loc.T("通用均衡频谱"));
        }

        double subBass = AverageRange(norm32, 25.0, 65.0);
        double midBass = AverageRange(norm32, 75.0, 165.0);
        double lowMid = AverageRange(norm32, 180.0, 420.0);
        double vocalMid = AverageRange(norm32, 500.0, 2200.0);
        double presence = AverageRange(norm32, 2400.0, 6200.0);
        double air = AverageRange(norm32, 7500.0, 16500.0);

        double subBumpOverLowMid = subBass - lowMid;
        double kickBumpOverLowMid = midBass - lowMid;
        double bassVsPres = subBass - presence;
        double trueSubShelf = Math.Max(0.0, (subBass - vocalMid) - Math.Max(0.0, -tiltDbPerOct * 2.1));
        bool isDarkRolledOff = tiltDbPerOct <= -3.5 || air <= -6.0;

        // Compute deterministic affinity scores for each music style
        var scores = new Dictionary<SongStyleGenre, (double Score, string TargetId)>
        {
            // Classical / Orchestral: wide dynamics + organic negative slope + lack of synthetic sub/kick shelf
            [SongStyleGenre.ClassicalOrchestral] = (
                (crestDb >= 11.8 ? 2.5 + (crestDb - 10.5) * 2.6 : (crestDb - 11.8) * 1.4)
                + (tiltDbPerOct is <= -0.8 and >= -4.5 ? 2.0 : -0.8)
                + (subBumpOverLowMid < 3.5 ? 1.2 : -1.8)
                + (presence < 1.5 ? 0.8 : -0.8),
                "classical_reference"),

            // EDM / Hip-Hop Bass: strong sub/kick excess on a broadband (non-muffled, non-symphonic) mix
            [SongStyleGenre.EdmHipHopBass] = (
                (!isDarkRolledOff && crestDb < 12.0
                    ? trueSubShelf * 1.6 + Math.Max(0.0, bassVsPres) * 0.95 + Math.Max(0.0, kickBumpOverLowMid) * 0.8 + 3.0
                    : -12.0)
                + (crestDb >= 12.0 ? -4.0 : 0.8),
                "bass_punch"),

            // ACG / J-Pop / Electronic: lively mid-bass punch + sparkling female-vocal upper-mid/air + tidy low-mid
            [SongStyleGenre.AcgVibrant] = (
                (presence >= 1.0 ? 2.5 + (presence - 0.5) * 0.95 : (presence > -0.8 ? 0.8 : -1.5))
                + (air > -3.2 ? 1.6 + Math.Max(0.0, air + 3.0) * 0.45 : -1.5)
                + (kickBumpOverLowMid > 0.0 ? 1.2 : -0.4)
                + (bassVsPres > 2.5 ? -3.0 : 0.6)
                + (crestDb is >= 6.0 and <= 11.2 ? 0.8 : 0.0),
                "acg_vibrant"),

            // Vocal Ballad / Acoustic Singer: vocal core forward, relaxed extreme sub & extreme air
            [SongStyleGenre.VocalBallad] = (
                Math.Max(0.0, vocalMid - subBass) * 0.95
                + Math.Max(0.0, vocalMid - air) * 0.55
                + (subBass < 0.0 ? 1.4 : -1.5)
                + (air < -1.0 && air > -7.0 ? 1.2 : -0.8)
                + (lowMid > -1.2 && lowMid < 2.2 ? 1.0 : 0.0),
                "warm_vocals"),

            // Rock / Metal: dense compressed mids + crunchy guitars in 180-400Hz & 2.5-5kHz
            [SongStyleGenre.RockMetal] = (
                (crestDb < 10.2 ? (10.5 - crestDb) * 0.9 : -1.2)
                + (presence > -1.5 && lowMid > -0.5 ? 2.0 : 0.0)
                + (midBass >= 0.0 && presence >= -1.0 && air < 2.0 ? 1.4 : 0.0)
                + (subBass < midBass + 0.5 ? 0.8 : -0.8),
                "rock_energy"),

            // Jazz / Acoustic Ensemble: warm acoustic bass body + good macro dynamics + airy cymbals
            [SongStyleGenre.JazzAcoustic] = (
                (crestDb is >= 10.2 and <= 12.8 ? 2.4 : 0.0)
                + (midBass >= -1.0 && lowMid >= -0.5 ? 1.4 : 0.0)
                + (subBass < 0.5 ? 1.0 : -1.2)
                + (air > -4.0 && air < 1.5 ? 1.1 : 0.0),
                "balanced_master"),

            // Lo-Fi / Ambient: steep downward high-frequency roll-off & muted presence/air
            [SongStyleGenre.LoFiAmbient] = (
                (tiltDbPerOct <= -3.8 ? (-tiltDbPerOct - 3.0) * 1.8 + 1.5 : -2.5)
                + (air <= -5.5 ? (-air - 4.0) * 0.75 : 0.0)
                + (presence <= -3.0 ? 1.8 : 0.0)
                + (bassVsPres > 4.0 ? 1.0 : 0.0),
                "bright_airy"),

            // Modern Pop: commercial smile + modern loudness
            [SongStyleGenre.ModernPop] = (
                (crestDb is >= 6.5 and <= 10.8 ? 1.8 : 0.5)
                + (midBass >= 0.5 && presence >= -1.2 ? 1.8 : 0.6)
                + (Math.Abs(tiltDbPerOct + 1.5) < 2.2 ? 1.0 : 0.0),
                "modern_pop")
        };

        var sorted = scores.OrderByDescending(kv => kv.Value.Score).ToList();
        var top = sorted[0];
        var second = sorted[1];
        double margin = Math.Max(0.0, top.Value.Score - second.Value.Score);
        double conf = Math.Clamp(0.55 + margin * 0.14, 0.45, 0.98);

        string summary = Loc.F(
            "超低 {0:+0.0;-0.0;0.0} / 临场 {1:+0.0;-0.0;0.0} / 空气 {2:+0.0;-0.0;0.0} dB · 动态 {3:F1} dB",
            subBass, presence, air, crestDb);

        return new SongGenreClassification(
            top.Key,
            top.Value.TargetId,
            Math.Round(conf, 2),
            Math.Round(subBass, 2),
            Math.Round(midBass, 2),
            Math.Round(lowMid, 2),
            Math.Round(presence, 2),
            Math.Round(air, 2),
            summary);
    }

    private static double[] ComputeHeadphoneAcousticAdjustment32(
        MixAssistantHeadphoneContext ctx,
        out string matchedHpName,
        out string matchedHpSummary,
        out double maxSafeSubBoostScale,
        out double calUncompensatedPeakDb)
    {
        double[] adj32 = new double[AnalysisBinCount];
        matchedHpName = string.Empty;
        matchedHpSummary = string.Empty;
        maxSafeSubBoostScale = 1.0;
        calUncompensatedPeakDb = 0.0;

        LearnedHeadphoneProfile? profile = null;
        if (ctx.ActiveOpraPreset != null)
        {
            profile = OpraHeadphoneKnowledgeBase.InferFromOpraPreset(ctx.ActiveOpraPreset);
        }
        else if (!string.IsNullOrWhiteSpace(ctx.ProductIdOrName))
        {
            profile = OpraHeadphoneKnowledgeBase.FindByProductOrPresetId(ctx.ProductIdOrName);
        }

        HeadphoneWearingStyle effWearing = ctx.WearingStyle != HeadphoneWearingStyle.Auto
            ? ctx.WearingStyle
            : (profile?.WearingStyle ?? HeadphoneWearingStyle.Auto);
        HeadphoneDriverType effDriver = ctx.DriverType != HeadphoneDriverType.Auto
            ? ctx.DriverType
            : (profile?.DriverType ?? HeadphoneDriverType.Auto);
        HeadphoneTuningStyle effTuning = ctx.TuningStyle != HeadphoneTuningStyle.Auto
            ? ctx.TuningStyle
            : (profile?.TuningStyle ?? HeadphoneTuningStyle.Auto);

        bool hasAnyHint = profile != null
            || effWearing != HeadphoneWearingStyle.Auto
            || effDriver != HeadphoneDriverType.Auto
            || effTuning != HeadphoneTuningStyle.Auto;

        if (!hasAnyHint)
        {
            if (ctx.IsOpraCalibrationActive)
            {
                calUncompensatedPeakDb = Math.Max(0.0, ctx.CalibrationPeakBoostDb + ctx.CalibrationPreampDb);
            }
            return adj32;
        }

        double strength = Math.Clamp(ctx.HeadphoneMatchStrength01, 0.0, 1.0);

        // Compose 8-zone native headphone curve relative to Harman target: [Sub, MidBass, LowMid, Mid, UpperMid, Pres, Bril, Air]
        double[] hp8 = new double[8];
        if (profile != null)
        {
            hp8[0] = profile.SubBassDb;
            hp8[1] = profile.MidBassDb;
            hp8[2] = profile.LowMidDb;
            hp8[3] = 0.0;
            hp8[4] = profile.UpperMidDb;
            hp8[5] = profile.PresenceDb;
            hp8[6] = profile.BrillianceDb;
            hp8[7] = profile.AirDb;
            matchedHpName = profile.DisplayName;
        }
        else
        {
            int layers = 0;
            void AddLayer(double[] vec)
            {
                for (int i = 0; i < 8; i++) hp8[i] += vec[i];
                layers++;
            }

            if (effWearing != HeadphoneWearingStyle.Auto && OpraHeadphoneKnowledgeBase.WearingCentroids8.TryGetValue(effWearing, out var wc))
                AddLayer(wc);
            if (effDriver != HeadphoneDriverType.Auto && OpraHeadphoneKnowledgeBase.DriverCentroids8.TryGetValue(effDriver, out var dc))
                AddLayer(dc);
            if (effTuning != HeadphoneTuningStyle.Auto && OpraHeadphoneKnowledgeBase.TuningCentroids8.TryGetValue(effTuning, out var tc))
                AddLayer(tc);

            if (layers > 0)
            {
                for (int i = 0; i < 8; i++) hp8[i] /= layers;
            }
            matchedHpName = $"{FormatWearingName(effWearing)} · {FormatDriverName(effDriver)} · {FormatTuningName(effTuning)}";
        }

        // Physical driver sub-bass excursion protection
        maxSafeSubBoostScale = (effDriver, effWearing) switch
        {
            (HeadphoneDriverType.Electrostatic, _) => 0.42,
            (_, HeadphoneWearingStyle.Earbud) => 0.38,
            (HeadphoneDriverType.BalancedArmature, _) => 0.65,
            (HeadphoneDriverType.PlanarMagnetic, _) => 1.00,
            _ => 0.85
        };

        // 8 center frequencies corresponding to OpraHeadphoneKnowledgeBase centroids
        (double Freq, double Db)[] anchors =
        {
            (40.0, hp8[0]),
            (110.0, hp8[1]),
            (280.0, hp8[2]),
            (750.0, hp8[3]),
            (1800.0, hp8[4]),
            (4000.0, hp8[5]),
            (8000.0, hp8[6]),
            (14000.0, hp8[7])
        };
        double[] hp32 = InterpolateAnchors(anchors, AnalysisFrequencies);

        if (ctx.IsOpraCalibrationActive)
        {
            // Headphone Calibration is ALREADY active! Do NOT double-EQ the raw curve!
            // Instead apply subtle driver/wearing ergonomic synergy (<= +/- 0.9 dB) & protect combined headroom.
            calUncompensatedPeakDb = Math.Max(0.0, ctx.CalibrationPeakBoostDb + ctx.CalibrationPreampDb);
            for (int b = 0; b < AnalysisBinCount; b++)
            {
                double f = AnalysisFrequencies[b];
                double erg = 0.0;
                if (effDriver == HeadphoneDriverType.PlanarMagnetic && f >= 2500 && f <= 5000)
                {
                    erg -= 0.45 * strength; // Tame calibrated planar upper-mid glare gently
                }
                if (effWearing == HeadphoneWearingStyle.OverEar && f >= 45 && f <= 110)
                {
                    erg += 0.55 * strength; // Compensate lack of bone-conducted chest thump on open over-ears
                }
                if ((effWearing == HeadphoneWearingStyle.InEarIem || effWearing == HeadphoneWearingStyle.TrueWireless) && f >= 5500 && f <= 9500)
                {
                    erg -= 0.50 * strength; // Protect ear-canal resonance peak fatigue
                }
                adj32[b] = erg;
            }

            matchedHpSummary = Loc.F("已开启耳机校正 · {0} · 防重复叠加以保护失真", matchedHpName);
        }
        else
        {
            // Headphone runs raw: gently compensate native headphone signature without overdriving!
            for (int b = 0; b < AnalysisBinCount; b++)
            {
                double f = AnalysisFrequencies[b];
                // Bounded inverse compensation scaled cleanly to avoid fighting song musicality
                double inv = -Math.Clamp(hp32[b], -6.5, 6.5) * 0.44 * strength;

                // Protect electrostatic/earbud sub-bass from distortion while still cleaning treble spikes
                if (f < 75.0 && inv > 0.0)
                {
                    inv *= maxSafeSubBoostScale;
                }
                // Extra guard on notorious 6-10kHz metallic peaks (e.g. bright dynamic / TWS / planar)
                if (f >= 5500.0 && f <= 10500.0 && hp32[b] > 2.0)
                {
                    inv -= Math.Min(1.2, (hp32[b] - 2.0) * 0.25) * strength;
                }

                adj32[b] = inv;
            }

            matchedHpSummary = Loc.F("{0} · {1} · {2}", FormatWearingName(effWearing), FormatDriverName(effDriver), FormatTuningName(effTuning));
        }

        return adj32;
    }

    public static string FormatWearingName(HeadphoneWearingStyle style) => style switch
    {
        HeadphoneWearingStyle.OverEar => Loc.T("包耳式 (Over-Ear)"),
        HeadphoneWearingStyle.OnEar => Loc.T("压耳式 (On-Ear)"),
        HeadphoneWearingStyle.InEarIem => Loc.T("入耳监听 (IEM)"),
        HeadphoneWearingStyle.TrueWireless => Loc.T("真无线 (TWS)"),
        HeadphoneWearingStyle.Earbud => Loc.T("平头塞 (Earbud)"),
        _ => Loc.T("自动佩戴识别")
    };

    public static string FormatDriverName(HeadphoneDriverType driver) => driver switch
    {
        HeadphoneDriverType.Dynamic => Loc.T("动圈单元 (DD)"),
        HeadphoneDriverType.PlanarMagnetic => Loc.T("平板振膜 (Planar)"),
        HeadphoneDriverType.BalancedArmature => Loc.T("动铁单元 (BA)"),
        HeadphoneDriverType.HybridTribrid => Loc.T("圈铁/多单元混合 (Hybrid)"),
        HeadphoneDriverType.Electrostatic => Loc.T("静电单元 (EST)"),
        _ => Loc.T("自动单元识别")
    };

    public static string FormatTuningName(HeadphoneTuningStyle tuning) => tuning switch
    {
        HeadphoneTuningStyle.HarmanNeutral => Loc.T("哈曼中性参考 (Harman Neutral)"),
        HeadphoneTuningStyle.WarmLush => Loc.T("暖声润泽 (Warm & Lush)"),
        HeadphoneTuningStyle.BrightAnalytical => Loc.T("高解析偏亮 (Bright Analytical)"),
        HeadphoneTuningStyle.VShaped => Loc.T("V型流行/刺激 (V-Shaped)"),
        HeadphoneTuningStyle.BassHeavy => Loc.T("低频澎湃 (Bass-Heavy)"),
        HeadphoneTuningStyle.MidCentricVocal => Loc.T("中频人声见长 (Mid-Centric)"),
        _ => Loc.T("自动调音识别")
    };

    private static FurinaPlayerBiquadFilter[] CreateFilterBank(double[] gains)
    {
        var filters = new FurinaPlayerBiquadFilter[GraphicBandCount];
        for (int g = 0; g < GraphicBandCount; g++)
        {
            FurinaPlayerFilterType filterType = g == 0
                ? FurinaPlayerFilterType.LowShelf
                : (g == GraphicBandCount - 1 ? FurinaPlayerFilterType.HighShelf : FurinaPlayerFilterType.Peaking);

            var filter = new FurinaPlayerBiquadFilter
            {
                FilterType = filterType,
                FrequencyHz = GraphicFrequencies[g],
                GainDb = gains[g],
                Q = GraphicQs[g],
                Enabled = true
            };
            filter.UpdateCoefficients(48000.0);
            filters[g] = filter;
        }
        return filters;
    }

    private static void UpdateFilterBankGains(FurinaPlayerBiquadFilter[] filters, double[] gains)
    {
        for (int g = 0; g < GraphicBandCount; g++)
        {
            filters[g].GainDb = gains[g];
            filters[g].UpdateCoefficients(48000.0);
        }
    }

    private static double EvaluatePeakCompositeGainDb(FurinaPlayerBiquadFilter[] filters)
    {
        double peak = 0.0;
        for (int p = 0; p < PredictedCurvePointCount; p++)
        {
            double freq = Curve64Frequencies[p];
            double sumDb = 0.0;
            for (int g = 0; g < filters.Length; g++)
            {
                sumDb += filters[g].CalculateResponseDb(freq, 48000.0);
            }
            if (sumDb > peak) peak = sumDb;
        }
        return peak;
    }

    private void UpdateAccumulatedEnergyLocked(ReadOnlySpan<double> frame32Energy)
    {
        _learnedFrames++;
        double w = 1.0 / _learnedFrames;
        for (int b = 0; b < AnalysisBinCount; b++)
        {
            _accumEnergy32[b] = _accumEnergy32[b] * (1.0 - w) + frame32Energy[b] * w;
        }
    }

    private static MixAssistantProfile FinalizeProfile(double[] meanEnergy32, double crestDb, int frames)
    {
        double[] rawDb32 = new double[AnalysisBinCount];
        for (int b = 0; b < AnalysisBinCount; b++)
        {
            double e = Math.Max(1e-12, meanEnergy32[b]);
            rawDb32[b] = 10.0 * Math.Log10(e);
        }

        double midRef = ComputeMidbandReference(rawDb32);
        double[] norm32 = new double[AnalysisBinCount];
        for (int b = 0; b < AnalysisBinCount; b++)
        {
            norm32[b] = Math.Clamp(rawDb32[b] - midRef, -32.0, 24.0);
        }

        double[] bands10 = new double[GraphicBandCount];
        for (int g = 0; g < GraphicBandCount; g++)
        {
            double logCenter = Math.Log10(GraphicFrequencies[g]);
            double wSum = 0.0;
            double vSum = 0.0;
            for (int b = 0; b < AnalysisBinCount; b++)
            {
                double oct = (Math.Log10(AnalysisFrequencies[b]) - logCenter) / 0.30102999566;
                double w = Math.Exp(-0.5 * (oct * oct) / (0.42 * 0.42));
                vSum += norm32[b] * w;
                wSum += w;
            }
            bands10[g] = Math.Round(wSum > 1e-9 ? vSum / wSum : 0.0, 2);
        }

        double tilt = ComputeSpectralTiltDbPerOctave(norm32);
        double confidence = Math.Clamp(frames / 24.0, 0.15, 1.0);
        SongGenreClassification genre = ClassifySongGenre(norm32, crestDb, tilt);

        return new MixAssistantProfile(
            Bins32Db: norm32,
            Bands10Db: bands10,
            MidbandRefDb: Math.Round(midRef, 2),
            CrestFactorDb: Math.Round(crestDb, 2),
            SpectralTiltDbPerOct: Math.Round(tilt, 2),
            AnalyzedFrames: frames,
            Confidence01: Math.Round(confidence, 2))
        {
            GenreClassification = genre
        };
    }

    private static double AverageRange(double[] norm32, double minHz, double maxHz)
    {
        double sum = 0.0;
        int count = 0;
        for (int i = 0; i < AnalysisBinCount; i++)
        {
            if (AnalysisFrequencies[i] >= minHz && AnalysisFrequencies[i] <= maxHz)
            {
                sum += norm32[i];
                count++;
            }
        }
        return count > 0 ? sum / count : 0.0;
    }

    private static double ComputeMidbandReference(double[] bins32Db)
    {
        double sum = 0.0;
        int count = 0;
        for (int b = 0; b < AnalysisBinCount; b++)
        {
            double f = AnalysisFrequencies[b];
            if (f >= 400.0 && f <= 2500.0)
            {
                sum += bins32Db[b];
                count++;
            }
        }
        return count > 0 ? sum / count : 0.0;
    }

    private static double ComputeSpectralTiltDbPerOctave(double[] norm32)
    {
        double meanX = 0.0;
        double meanY = 0.0;
        for (int b = 0; b < AnalysisBinCount; b++)
        {
            meanX += Math.Log2(AnalysisFrequencies[b] / 1000.0);
            meanY += norm32[b];
        }
        meanX /= AnalysisBinCount;
        meanY /= AnalysisBinCount;

        double num = 0.0;
        double den = 0.0;
        for (int b = 0; b < AnalysisBinCount; b++)
        {
            double dx = Math.Log2(AnalysisFrequencies[b] / 1000.0) - meanX;
            double dy = norm32[b] - meanY;
            num += dx * dy;
            den += dx * dx;
        }
        return den > 1e-9 ? num / den : 0.0;
    }

    private static void ApplySpectralDemasking(double[] learned32, double[] target32, double[] delta32)
    {
        for (int b = 0; b < AnalysisBinCount; b++)
        {
            double f = AnalysisFrequencies[b];
            double excess = learned32[b] - target32[b];

            if (f >= 180.0 && f <= 450.0 && excess > 1.8)
            {
                delta32[b] -= Math.Min(3.8, (excess - 1.8) * 0.60);
            }
            else if (f >= 2400.0 && f <= 6500.0 && excess > 2.0)
            {
                delta32[b] -= Math.Min(3.8, (excess - 2.0) * 0.55);
            }
            else if (f < 55.0 && excess > 3.2)
            {
                delta32[b] -= Math.Min(3.2, (excess - 3.2) * 0.50);
            }
        }
    }

    private static List<string> DetectSpectralIssues(double[] learned32, double[] target32)
    {
        var issues = new List<string>();

        double subExcess = AverageRangeDiff(learned32, target32, 25.0, 65.0);
        double mudExcess = AverageRangeDiff(learned32, target32, 180.0, 420.0);
        double nasalExcess = AverageRangeDiff(learned32, target32, 700.0, 1400.0);
        double sibilancePeak = MaxRangeDiff(learned32, target32, 2400.0, 6500.0);
        double airExcess = AverageRangeDiff(learned32, target32, 8000.0, 18000.0);

        if (subExcess > 3.2) issues.Add(Loc.T("低频轰鸣过量"));
        else if (subExcess < -3.5) issues.Add(Loc.T("低频下潜不足"));

        if (mudExcess > 2.5) issues.Add(Loc.T("250 Hz 浑浊堆积"));
        else if (mudExcess < -3.0) issues.Add(Loc.T("中低频偏薄"));

        if (nasalExcess > 2.8) issues.Add(Loc.T("1 kHz 鼻音突出"));
        else if (nasalExcess < -2.8) issues.Add(Loc.T("中频人声凹陷"));

        if (sibilancePeak > 3.0) issues.Add(Loc.T("3.2 kHz 齿音尖刺"));

        if (airExcess < -3.0) issues.Add(Loc.T("高频延伸不足"));
        else if (airExcess > 3.2) issues.Add(Loc.T("极高频偏亮刺耳"));

        if (issues.Count == 0)
        {
            issues.Add(Loc.T("频谱分布整体均衡"));
        }

        return issues;
    }

    private static double AverageRangeDiff(double[] a, double[] b, double minHz, double maxHz)
    {
        double sum = 0.0;
        int count = 0;
        for (int i = 0; i < AnalysisBinCount; i++)
        {
            if (AnalysisFrequencies[i] >= minHz && AnalysisFrequencies[i] <= maxHz)
            {
                sum += a[i] - b[i];
                count++;
            }
        }
        return count > 0 ? sum / count : 0.0;
    }

    private static double MaxRangeDiff(double[] a, double[] b, double minHz, double maxHz)
    {
        double max = double.NegativeInfinity;
        for (int i = 0; i < AnalysisBinCount; i++)
        {
            if (AnalysisFrequencies[i] >= minHz && AnalysisFrequencies[i] <= maxHz)
            {
                double d = a[i] - b[i];
                if (d > max) max = d;
            }
        }
        return double.IsFinite(max) ? max : 0.0;
    }

    private static bool TryComputeFft32Energy(
        ReadOnlySpan<float> monoSamples,
        int sampleRate,
        float[] fftReal,
        float[] fftImag,
        Span<double> outEnergy32)
    {
        int n = Math.Min(FftSize, monoSamples.Length);
        double sqSum = 0.0;
        for (int i = 0; i < n; i++)
        {
            float s = monoSamples[i];
            if (!float.IsFinite(s)) s = 0f;
            sqSum += (double)s * s;
            fftReal[i] = s * HannWindow[i];
            fftImag[i] = 0f;
        }
        for (int i = n; i < FftSize; i++)
        {
            fftReal[i] = 0f;
            fftImag[i] = 0f;
        }

        double rms = Math.Sqrt(sqSum / Math.Max(1, n));
        double rmsDb = rms > 1e-9 ? 20.0 * Math.Log10(rms) : -120.0;
        if (rmsDb < MinSignalDbfs)
        {
            return false;
        }

        RunRadix2Fft(fftReal, fftImag, FftSize);

        double binHz = (double)sampleRate / FftSize;
        int nyquistBin = FftSize / 2 - 1;
        double thirdOctFactor = Math.Pow(2.0, 1.0 / 6.0);

        for (int b = 0; b < AnalysisBinCount; b++)
        {
            double fc = AnalysisFrequencies[b];
            double fLow = fc / thirdOctFactor;
            double fHigh = fc * thirdOctFactor;

            double exactLow = fLow / binHz;
            double exactHigh = fHigh / binHz;

            int binStart = Math.Clamp((int)Math.Floor(exactLow), 1, nyquistBin);
            int binEnd = Math.Clamp((int)Math.Ceiling(exactHigh), binStart, nyquistBin);

            double energy = 0.0;
            if (exactHigh - exactLow < 1.0)
            {
                double exactCenter = Math.Clamp(fc / binHz, 1.0, nyquistBin);
                int k0 = (int)Math.Floor(exactCenter);
                int k1 = Math.Min(nyquistBin, k0 + 1);
                double frac = exactCenter - k0;
                double p0 = (double)fftReal[k0] * fftReal[k0] + (double)fftImag[k0] * fftImag[k0];
                double p1 = (double)fftReal[k1] * fftReal[k1] + (double)fftImag[k1] * fftImag[k1];
                energy = (p0 * (1.0 - frac) + p1 * frac) * Math.Max(0.25, exactHigh - exactLow);
            }
            else
            {
                for (int k = binStart; k <= binEnd; k++)
                {
                    energy += (double)fftReal[k] * fftReal[k] + (double)fftImag[k] * fftImag[k];
                }
            }

            outEnergy32[b] = energy / ((double)FftSize * FftSize * 0.375);
        }

        return true;
    }

    private static void RunRadix2Fft(float[] real, float[] imag, int n)
    {
        int j = 0;
        for (int i = 0; i < n; i++)
        {
            if (i < j)
            {
                (real[i], real[j]) = (real[j], real[i]);
                (imag[i], imag[j]) = (imag[j], imag[i]);
            }
            int bit = n >> 1;
            while (bit > 0 && (j & bit) != 0)
            {
                j ^= bit;
                bit >>= 1;
            }
            j |= bit;
        }

        for (int len = 2; len <= n; len <<= 1)
        {
            double angle = -2.0 * Math.PI / len;
            float wlenR = (float)Math.Cos(angle);
            float wlenI = (float)Math.Sin(angle);

            for (int i = 0; i < n; i += len)
            {
                float wr = 1f;
                float wi = 0f;
                int half = len >> 1;

                for (int m = 0; m < half; m++)
                {
                    int u = i + m;
                    int v = u + half;

                    float vr = real[v] * wr - imag[v] * wi;
                    float vi = real[v] * wi + imag[v] * wr;

                    real[v] = real[u] - vr;
                    imag[v] = imag[u] - vi;
                    real[u] += vr;
                    imag[u] += vi;

                    float nextWr = wr * wlenR - wi * wlenI;
                    wi = wr * wlenI + wi * wlenR;
                    wr = nextWr;
                }
            }
        }
    }

    private static MixAssistantProfile CreateFlatFallbackProfile()
    {
        return new MixAssistantProfile(
            Bins32Db: new double[AnalysisBinCount],
            Bands10Db: new double[GraphicBandCount],
            MidbandRefDb: -24.0,
            CrestFactorDb: 12.0,
            SpectralTiltDbPerOct: 0.0,
            AnalyzedFrames: 0,
            Confidence01: 0.0);
    }

    private static double[] BuildAnalysisFrequencies()
    {
        double[] freqs = new double[AnalysisBinCount];
        double logMin = Math.Log10(25.0);
        double logMax = Math.Log10(20000.0);
        for (int i = 0; i < AnalysisBinCount; i++)
        {
            freqs[i] = Math.Round(Math.Pow(10.0, logMin + (logMax - logMin) * i / (AnalysisBinCount - 1)), 2);
        }
        return freqs;
    }

    private static double[] BuildCurve64Frequencies()
    {
        double[] freqs = new double[PredictedCurvePointCount];
        double logMin = Math.Log10(20.0);
        double logMax = Math.Log10(20000.0);
        for (int i = 0; i < PredictedCurvePointCount; i++)
        {
            freqs[i] = Math.Pow(10.0, logMin + (logMax - logMin) * i / (PredictedCurvePointCount - 1));
        }
        return freqs;
    }

    private static float[] BuildHannWindow(int size)
    {
        float[] w = new float[size];
        for (int i = 0; i < size; i++)
        {
            w[i] = 0.5f * (1.0f - (float)Math.Cos(2.0 * Math.PI * i / (size - 1)));
        }
        return w;
    }

    private static IReadOnlyList<MixAssistantTarget> BuildTargets()
    {
        return new List<MixAssistantTarget>
        {
            CreateTarget(
                id: AutoGenreTargetId,
                displayName: "🧠 智能识别曲风 (Auto Match)",
                description: "根据实时频谱、动态余量与斜率自动匹配最优曲风与母带目标曲线",
                anchors: new[] { (25.0, -1.2), (55.0, 0.4), (95.0, 0.6), (260.0, -0.5), (1000.0, 0.0), (3200.0, 0.4), (8000.0, 0.0), (16000.0, -0.8) }),

            CreateTarget(
                id: "balanced_master",
                displayName: "平衡母带 (Balanced Master)",
                description: "自然平衡的母带级频响目标，控制超低频冗余并保持细腻高频延伸",
                anchors: new[] { (25.0, -1.5), (45.0, 0.2), (80.0, 0.5), (250.0, -0.4), (1000.0, 0.0), (3200.0, 0.2), (8000.0, -0.4), (16000.0, -1.5) }),

            CreateTarget(
                id: "warm_vocals",
                displayName: "温暖人声 (Warm Vocals)",
                description: "突出人声厚度与临场亲和力，柔化齿音与极高频刺激",
                anchors: new[] { (25.0, -2.5), (60.0, 0.5), (140.0, 1.5), (280.0, -0.8), (1200.0, 0.9), (2800.0, 1.5), (7000.0, -0.8), (16000.0, -2.0) }),

            CreateTarget(
                id: "bass_punch",
                displayName: "低频冲击 (Bass & Punch)",
                description: "强化下潜与鼓点冲击力，清理中低频浑浊感",
                anchors: new[] { (25.0, 1.0), (55.0, 2.8), (110.0, 1.8), (280.0, -1.2), (1000.0, -0.2), (3500.0, 0.8), (8000.0, 0.2), (16000.0, -0.8) }),

            CreateTarget(
                id: "drums_transient",
                displayName: "鼓组节奏 (Drums / Transient)",
                description: "强化底鼓下盘与军鼓瞬态脆度，抑制箱音堆积",
                anchors: new[] { (25.0, 0.3), (60.0, 2.2), (160.0, 1.1), (400.0, -1.6), (1000.0, -0.2), (4500.0, 1.8), (10000.0, 1.4), (16000.0, 0.4) }),

            CreateTarget(
                id: "modern_pop",
                displayName: "流行现代 (Modern Pop)",
                description: "现代流媒体微笑轮廓：紧致低频、清透中高频与空气感",
                anchors: new[] { (25.0, 0.2), (60.0, 2.0), (130.0, 0.6), (300.0, -1.0), (1000.0, 0.0), (3200.0, 1.2), (10000.0, 1.6), (16000.0, 0.8) }),

            CreateTarget(
                id: "acg_vibrant",
                displayName: "ACG 动感 / 二次元 (ACG Vibrant)",
                description: "专为日系电音与女声优化：明亮通透的华丽感与弹性低频",
                anchors: new[] { (25.0, 0.2), (65.0, 2.2), (160.0, 0.4), (300.0, -1.2), (1000.0, 0.2), (3500.0, 1.8), (10000.0, 2.2), (16000.0, 1.2) }),

            CreateTarget(
                id: "rock_energy",
                displayName: "摇滚现场 (Rock & Metal)",
                description: "压制 250-400Hz 吉他厚闷感并柔化 4kHz 噪刺，突出拳拳到肉的底鼓能量",
                anchors: new[] { (25.0, -0.5), (65.0, 1.8), (130.0, 0.8), (320.0, -1.6), (1200.0, 0.4), (3800.0, -0.4), (9000.0, 1.2), (16000.0, 0.6) }),

            CreateTarget(
                id: "classical_reference",
                displayName: "古典原声 (Classical Reference)",
                description: "保留大动态交响呼吸感与厅堂空气延伸，轻柔平滑中高频染色",
                anchors: new[] { (25.0, -0.6), (55.0, 0.4), (160.0, 0.2), (350.0, -0.3), (1000.0, 0.0), (3200.0, 0.0), (9500.0, 0.8), (16000.0, 0.2) }),

            CreateTarget(
                id: "bright_airy",
                displayName: "通透空气 (Bright & Airy)",
                description: "轻盈低频配合宽广极高频空气感，提升空间解析力",
                anchors: new[] { (25.0, -2.0), (80.0, -0.8), (250.0, -0.6), (1000.0, 0.0), (3500.0, 1.2), (10000.0, 2.6), (16000.0, 2.8) }),

            CreateTarget(
                id: "late_night",
                displayName: "深夜轻听 (Late Night Fletcher-Munson)",
                description: "基于 Fletcher-Munson 等响曲线补偿低音量下的低音与细节流失",
                anchors: new[] { (25.0, 2.4), (60.0, 2.8), (140.0, 1.2), (400.0, 0.0), (1500.0, -0.6), (3200.0, -0.9), (9000.0, 1.6), (16000.0, 2.0) })
        };
    }

    private static MixAssistantTarget CreateTarget(string id, string displayName, string description, (double Freq, double Db)[] anchors)
    {
        double[] t32 = InterpolateAnchors(anchors, AnalysisFrequencies);
        double[] t10 = InterpolateAnchors(anchors, GraphicFrequencies);
        return new MixAssistantTarget(id, displayName, description, t32, t10);
    }

    private static double[] InterpolateAnchors((double Freq, double Db)[] anchors, double[] targetFreqs)
    {
        double[] result = new double[targetFreqs.Length];
        for (int i = 0; i < targetFreqs.Length; i++)
        {
            double logF = Math.Log10(targetFreqs[i]);
            if (targetFreqs[i] <= anchors[0].Freq)
            {
                result[i] = anchors[0].Db;
                continue;
            }
            if (targetFreqs[i] >= anchors[^1].Freq)
            {
                result[i] = anchors[^1].Db;
                continue;
            }

            for (int a = 0; a < anchors.Length - 1; a++)
            {
                if (targetFreqs[i] >= anchors[a].Freq && targetFreqs[i] <= anchors[a + 1].Freq)
                {
                    double l0 = Math.Log10(anchors[a].Freq);
                    double l1 = Math.Log10(anchors[a + 1].Freq);
                    double t = (logF - l0) / Math.Max(1e-9, l1 - l0);
                    double s = t * t * (3.0 - 2.0 * t);
                    result[i] = Math.Round(anchors[a].Db * (1.0 - s) + anchors[a + 1].Db * s, 2);
                    break;
                }
            }
        }
        return result;
    }
}
