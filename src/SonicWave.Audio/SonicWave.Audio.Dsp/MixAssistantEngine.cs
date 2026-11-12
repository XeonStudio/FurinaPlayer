using System;
using System.Collections.Generic;

namespace SonicWave.Audio.Dsp;

public sealed record MixAssistantTarget(
    string Id,
    string DisplayName,
    string Description,
    double[] Target32Db,
    double[] Target10Db);

public sealed record MixAssistantProfile(
    double[] Bins32Db,
    double[] Bands10Db,
    double MidbandRefDb,
    double CrestFactorDb,
    double SpectralTiltDbPerOct,
    int AnalyzedFrames,
    double Confidence01);

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
    public EchoBiquadFilter[] Filters { get; init; } = Array.Empty<EchoBiquadFilter>();
    public double[] TargetCurve32Db { get; init; } = new double[32];
    public double[] BeforeSpectrum32Db { get; init; } = new double[32];
    public double[] AfterSpectrum32Db { get; init; } = new double[32];
    public double[] PredictedCurve64Db { get; init; } = new double[64];
    public IReadOnlyList<string> DetectedIssues { get; init; } = Array.Empty<string>();
    public double Confidence01 { get; init; }
}

/// <summary>
/// Native tonal-balance learner, spectral de-masking detector, and 10-band / parametric EQ solver
/// inspired by iZotope Neutron Pro BalanceAssistantLearnerElement, DeMaskingEQ, and Sculptor.
/// </summary>
public sealed class MixAssistantEngine
{
    public const int AnalysisBinCount = 32;
    public const int GraphicBandCount = 10;
    public const int PredictedCurvePointCount = 64;

    private const int FftSize = 4096;
    private const int HopSize = 2048;
    private const double MinSignalDbfs = -68.0;

    public static readonly double[] GraphicFrequencies =
    {
        31.25, 62.5, 125.0, 250.0, 500.0, 1000.0, 2000.0, 4000.0, 8000.0, 16000.0
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
        return Targets[0];
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
                double absMono = Math.Abs(mono);
                if (absMono > _peakSum) _peakSum = absMono;
                _rmsSqSum += (double)mono * mono;
                _rmsSamples++;

                _pcmRing[_pcmFill++] = mono;
                if (_pcmFill >= FftSize)
                {
                    if (TryComputeFft32Energy(_pcmRing.AsSpan(0, FftSize), _activeSampleRate, _fftReal, _fftImag, frame32))
                    {
                        UpdateAccumulatedEnergyLocked(frame32);
                    }

                    int remain = _pcmFill - HopSize;
                    if (remain > 0)
                    {
                        Array.Copy(_pcmRing, HopSize, _pcmRing, 0, remain);
                    }
                    _pcmFill = Math.Max(0, remain);
                }
            }
        }
    }

    public MixAssistantProfile GetCurrentSnapshot()
    {
        lock (_learnLock)
        {
            return BuildProfileFromAccumulatedLocked();
        }
    }

    public MixAssistantProfile EndLearning()
    {
        lock (_learnLock)
        {
            _isLearning = false;
            return BuildProfileFromAccumulatedLocked();
        }
    }

    public MixAssistantProfile AnalyzeSamples(float[] interleaved, int channels, int sampleRate)
    {
        if (interleaved == null || interleaved.Length == 0 || channels <= 0 || sampleRate <= 0)
        {
            return CreateFlatFallbackProfile();
        }

        int totalFrames = interleaved.Length / channels;
        if (totalFrames <= 0)
        {
            return CreateFlatFallbackProfile();
        }

        // Convert to mono and measure crest factor in a single pass
        float[] mono = new float[totalFrames];
        double peak = 0.0;
        double sumSq = 0.0;
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
        double crestDb = (rms > 1e-9 && peak > 1e-9) ? Math.Clamp(20.0 * Math.Log10(peak / rms), 0.0, 36.0) : 12.0;

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
            // Sample up to 64 evenly distributed windows across the entire buffer for fast O(1)-bounded analysis
            int maxWindows = 64;
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
        MixAssistantTarget target = GetTarget(targetId);
        double amount = Math.Clamp(matchAmount01, 0.0, 1.0);
        double bassTrim = Math.Clamp(bassSculptDb, -9.0, 9.0);
        double trebleTrim = Math.Clamp(trebleSculptDb, -9.0, 9.0);

        double[] before32 = learned?.Bins32Db is { Length: AnalysisBinCount }
            ? (double[])learned.Bins32Db.Clone()
            : new double[AnalysisBinCount];
        double confidence = learned?.Confidence01 ?? 0.0;

        // 1. Compute 32-band corrective delta (Target - Learned) + Spectral De-Masking + Sculpt
        double[] rawDelta32 = new double[AnalysisBinCount];
        for (int b = 0; b < AnalysisBinCount; b++)
        {
            rawDelta32[b] = target.Target32Db[b] - before32[b];
        }

        // Detect spectral issues from learned curve relative to target
        List<string> issues = DetectSpectralIssues(before32, target.Target32Db);

        // Apply Neutron-style DeMaskingEQ anti-mud / anti-sibilance refinement
        if (demaskMidMud)
        {
            ApplySpectralDemasking(before32, target.Target32Db, rawDelta32);
        }

        // Apply Low Shelf Energy & High Shelf Energy Sculpt trims
        for (int b = 0; b < AnalysisBinCount; b++)
        {
            double f = AnalysisFrequencies[b];
            double lowWeight = 1.0 / (1.0 + Math.Pow(f / 140.0, 2.2));
            double highWeight = 1.0 / (1.0 + Math.Pow(4500.0 / f, 2.2));
            rawDelta32[b] += bassTrim * lowWeight + trebleTrim * highWeight;
        }

        // 2. Project 32-band delta onto the 10 standard ISO octave bands via Gaussian log-frequency kernel
        double[] bandOffsets = new double[GraphicBandCount];
        for (int g = 0; g < GraphicBandCount; g++)
        {
            double logCenter = Math.Log10(GraphicFrequencies[g]);
            double weightedSum = 0.0;
            double weightTotal = 0.0;

            for (int b = 0; b < AnalysisBinCount; b++)
            {
                double distOct = (Math.Log10(AnalysisFrequencies[b]) - logCenter) / 0.30102999566;
                double w = Math.Exp(-0.5 * (distOct * distOct) / (0.65 * 0.65));
                weightedSum += rawDelta32[b] * w;
                weightTotal += w;
            }

            double delta = weightTotal > 1e-9 ? weightedSum / weightTotal : 0.0;

            // Scale correction by matchAmount01, plus add sculpt trims even at low match amounts if user explicitly adjusted sculpt
            double pureMatch = delta - (bassTrim * (1.0 / (1.0 + Math.Pow(GraphicFrequencies[g] / 140.0, 2.2)))
                                      + trebleTrim * (1.0 / (1.0 + Math.Pow(4500.0 / GraphicFrequencies[g], 2.2))));
            double sculptOnly = delta - pureMatch;

            bandOffsets[g] = pureMatch * amount + sculptOnly;
        }

        // 3. Smooth adjacent bands slightly (prevent comb-like alternating +/- spikes)
        double[] smoothed = new double[GraphicBandCount];
        for (int g = 0; g < GraphicBandCount; g++)
        {
            double prev = g > 0 ? bandOffsets[g - 1] : bandOffsets[g];
            double next = g < GraphicBandCount - 1 ? bandOffsets[g + 1] : bandOffsets[g];
            smoothed[g] = 0.16 * prev + 0.68 * bandOffsets[g] + 0.16 * next;
        }

        // 4. Zero-mean gain compensation across perceptual core bands (125 Hz .. 8 kHz)
        // so EQ correction preserves perceived loudness without overall level drift
        double coreMean = 0.0;
        int coreCount = 0;
        for (int g = 2; g <= 8; g++)
        {
            coreMean += smoothed[g];
            coreCount++;
        }
        coreMean = coreCount > 0 ? coreMean / coreCount : 0.0;

        for (int g = 0; g < GraphicBandCount; g++)
        {
            smoothed[g] = Math.Round(Math.Clamp(smoothed[g] - coreMean * 0.75 * amount, -9.0, 9.0), 1);
        }

        // 5. Build adaptive EchoBiquadFilter cascade (LowShelf, Peaking, HighShelf)
        EchoBiquadFilter[] filters = new EchoBiquadFilter[GraphicBandCount];
        for (int g = 0; g < GraphicBandCount; g++)
        {
            EchoFilterType fType = g == 0
                ? EchoFilterType.LowShelf
                : (g == GraphicBandCount - 1 ? EchoFilterType.HighShelf : EchoFilterType.Peaking);

            // Narrower Q for surgical resonant cuts (especially in mud/sibilance region), wider Q for gentle boosts
            double q = fType is EchoFilterType.LowShelf or EchoFilterType.HighShelf
                ? 0.71
                : (smoothed[g] < -2.0 ? 1.85 : (smoothed[g] > 2.5 ? 1.15 : 1.41));

            var filter = new EchoBiquadFilter
            {
                FilterType = fType,
                FrequencyHz = GraphicFrequencies[g],
                GainDb = smoothed[g],
                Q = q,
                Enabled = true
            };
            filter.UpdateCoefficients(48000.0);
            filters[g] = filter;
        }

        // 6. Compute composite response at 64 points and derive headroom-safe RecommendedPreampDb
        double maxPositiveDb = 0.0;
        double[] rawCurve64 = new double[PredictedCurvePointCount];
        for (int i = 0; i < PredictedCurvePointCount; i++)
        {
            double freq = Curve64Frequencies[i];
            double resp = 0.0;
            for (int f = 0; f < filters.Length; f++)
            {
                resp += filters[f].CalculateResponseDb(freq, 48000.0);
            }
            rawCurve64[i] = resp;
            if (resp > maxPositiveDb) maxPositiveDb = resp;
        }

        // Auto-makeup / headroom safety: keep positive peaks from clipping
        double recommendedPreamp = maxPositiveDb > 0.4
            ? Math.Round(-Math.Clamp(maxPositiveDb * 0.55, 0.0, 9.0), 1)
            : 0.0;

        for (int i = 0; i < PredictedCurvePointCount; i++)
        {
            rawCurve64[i] = Math.Round(rawCurve64[i] + recommendedPreamp, 2);
        }

        // 7. Compute predicted post-EQ 32-band tonal balance spectrum
        double[] after32 = new double[AnalysisBinCount];
        for (int b = 0; b < AnalysisBinCount; b++)
        {
            double freq = AnalysisFrequencies[b];
            double resp = 0.0;
            for (int f = 0; f < filters.Length; f++)
            {
                resp += filters[f].CalculateResponseDb(freq, 48000.0);
            }
            after32[b] = before32[b] + resp;
        }

        // Re-center after32 around midband reference just like learned profile
        double afterMidRef = ComputeMidbandReference(after32);
        for (int b = 0; b < AnalysisBinCount; b++)
        {
            after32[b] = Math.Round(after32[b] - afterMidRef, 2);
        }

        return new MixAssistantSolution
        {
            TargetId = target.Id,
            TargetName = target.DisplayName,
            MatchAmount01 = amount,
            BassSculptDb = bassTrim,
            TrebleSculptDb = trebleTrim,
            DemaskMidMud = demaskMidMud,
            RecommendedPreampDb = recommendedPreamp,
            BandOffsetsDb = smoothed,
            Filters = filters,
            TargetCurve32Db = (double[])target.Target32Db.Clone(),
            BeforeSpectrum32Db = before32,
            AfterSpectrum32Db = after32,
            PredictedCurve64Db = rawCurve64,
            DetectedIssues = issues,
            Confidence01 = confidence
        };
    }

    private void UpdateAccumulatedEnergyLocked(ReadOnlySpan<double> frame32)
    {
        _learnedFrames++;
        double alpha = _learnedFrames <= 24 ? (1.0 / _learnedFrames) : 0.04;
        for (int b = 0; b < AnalysisBinCount; b++)
        {
            _accumEnergy32[b] = _accumEnergy32[b] * (1.0 - alpha) + frame32[b] * alpha;
        }
    }

    private MixAssistantProfile BuildProfileFromAccumulatedLocked()
    {
        if (_learnedFrames <= 0)
        {
            return CreateFlatFallbackProfile();
        }

        double rms = _rmsSamples > 0 ? Math.Sqrt(_rmsSqSum / _rmsSamples) : 0.0;
        double crestDb = (rms > 1e-9 && _peakSum > 1e-9)
            ? Math.Clamp(20.0 * Math.Log10(_peakSum / rms), 0.0, 36.0)
            : 12.0;

        return FinalizeProfile(_accumEnergy32, crestDb, _learnedFrames);
    }

    private static MixAssistantProfile FinalizeProfile(double[] energy32, double crestDb, int frames)
    {
        double[] rawDb32 = new double[AnalysisBinCount];
        for (int b = 0; b < AnalysisBinCount; b++)
        {
            rawDb32[b] = 10.0 * Math.Log10(Math.Max(energy32[b], 1e-12));
        }

        double midRef = ComputeMidbandReference(rawDb32);
        double[] norm32 = new double[AnalysisBinCount];
        for (int b = 0; b < AnalysisBinCount; b++)
        {
            norm32[b] = Math.Round(Math.Clamp(rawDb32[b] - midRef, -24.0, 24.0), 2);
        }

        // Collapse 32 bins to 10 octave bands
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

        // Linear regression slope across log2(freq) to measure spectral tilt (dB/octave)
        double tilt = ComputeSpectralTiltDbPerOctave(norm32);
        double confidence = Math.Clamp(frames / 24.0, 0.15, 1.0);

        return new MixAssistantProfile(
            Bins32Db: norm32,
            Bands10Db: bands10,
            MidbandRefDb: Math.Round(midRef, 2),
            CrestFactorDb: Math.Round(crestDb, 2),
            SpectralTiltDbPerOct: Math.Round(tilt, 2),
            AnalyzedFrames: frames,
            Confidence01: Math.Round(confidence, 2));
    }

    private static double ComputeMidbandReference(double[] bins32Db)
    {
        // Midband reference: 400 Hz .. 2500 Hz (indices ~12..20)
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
        // 1. Mud / boxiness zone (180..450 Hz): if learned energy exceeds target + 2.0 dB, apply focused de-masking cut
        for (int b = 0; b < AnalysisBinCount; b++)
        {
            double f = AnalysisFrequencies[b];
            double excess = learned32[b] - target32[b];

            if (f >= 180.0 && f <= 450.0 && excess > 2.0)
            {
                delta32[b] -= Math.Min(3.5, (excess - 2.0) * 0.55);
            }
            // 2. Harshness / sibilance spike zone (2400..6500 Hz): tame narrow spectral spikes
            else if (f >= 2400.0 && f <= 6500.0 && excess > 2.2)
            {
                delta32[b] -= Math.Min(3.5, (excess - 2.2) * 0.50);
            }
            // 3. Sub-bass boom control (< 55 Hz)
            else if (f < 55.0 && excess > 3.5)
            {
                delta32[b] -= Math.Min(3.0, (excess - 3.5) * 0.45);
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

        if (subExcess > 3.2) issues.Add("低频轰鸣过量");
        else if (subExcess < -3.5) issues.Add("低频下潜不足");

        if (mudExcess > 2.5) issues.Add("250 Hz 浑浊堆积");
        else if (mudExcess < -3.0) issues.Add("中低频偏薄");

        if (nasalExcess > 2.8) issues.Add("1 kHz 鼻音突出");
        else if (nasalExcess < -2.8) issues.Add("中频人声凹陷");

        if (sibilancePeak > 3.0) issues.Add("3.2 kHz 齿音尖刺");

        if (airExcess < -3.0) issues.Add("高频延伸不足");
        else if (airExcess > 3.2) issues.Add("极高频偏亮刺耳");

        if (issues.Count == 0)
        {
            issues.Add("频谱分布整体均衡");
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
        double thirdOctFactor = Math.Pow(2.0, 1.0 / 6.0); // +/- 1/6 octave around center = 1/3 octave band

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
                // Interpolate energy for sub-bin low frequencies (25..50 Hz) and scale by fractional bandwidth
                double exactCenter = Math.Clamp(fc / binHz, 1.0, nyquistBin);
                int k0 = (int)Math.Floor(exactCenter);
                int k1 = Math.Min(nyquistBin, k0 + 1);
                double frac = exactCenter - k0;
                double p0 = (double)fftReal[k0] * fftReal[k0] + (double)fftImag[k0] * fftImag[k0];
                double p1 = (double)fftReal[k1] * fftReal[k1] + (double)fftImag[k1] * fftImag[k1];
                energy = (p0 * (1.0 - frac) + p1 * frac) * Math.Max(0.15, exactHigh - exactLow);
            }
            else
            {
                for (int k = binStart; k <= binEnd; k++)
                {
                    energy += (double)fftReal[k] * fftReal[k] + (double)fftImag[k] * fftImag[k];
                }
            }

            outEnergy32[b] = Math.Max(energy / (FftSize * (double)FftSize), 1e-15);
        }

        return true;
    }

    private static void RunRadix2Fft(float[] real, float[] imag, int n)
    {
        int j = 0;
        for (int i = 0; i < n - 1; i++)
        {
            if (i < j)
            {
                (real[i], real[j]) = (real[j], real[i]);
                (imag[i], imag[j]) = (imag[j], imag[i]);
            }
            int k = n >> 1;
            while (k <= j)
            {
                j -= k;
                k >>= 1;
            }
            j += k;
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
        // Curated 32-band tonal balance targets (relative to 400..2500 Hz midband reference, 0 dB = pink noise flat)
        return new List<MixAssistantTarget>
        {
            CreateTarget(
                id: "balanced_master",
                displayName: "平衡母带 (Balanced Master)",
                description: "自然平衡的母带级频响目标，控制超低频冗余并保持细腻高频延伸",
                anchors: new[] { (25.0, -1.5), (45.0, 0.2), (80.0, 0.5), (250.0, -0.4), (1000.0, 0.0), (3200.0, 0.2), (8000.0, -0.4), (16000.0, -1.5) }),

            CreateTarget(
                id: "warm_vocals",
                displayName: "温暖人声 (Warm Vocals)",
                description: "突出人声厚度与临场亲和力，柔化齿音与极高频刺激",
                anchors: new[] { (25.0, -2.5), (60.0, 0.5), (140.0, 1.8), (280.0, -0.8), (1200.0, 1.0), (2800.0, 1.8), (7000.0, -0.8), (16000.0, -2.0) }),

            CreateTarget(
                id: "bass_punch",
                displayName: "低频冲击 (Bass & Punch)",
                description: "强化下潜与鼓点冲击力，清理中低频浑浊感",
                anchors: new[] { (25.0, 1.5), (55.0, 3.6), (110.0, 2.2), (280.0, -1.2), (1000.0, -0.2), (3500.0, 1.0), (8000.0, 0.2), (16000.0, -0.8) }),

            CreateTarget(
                id: "drums_transient",
                displayName: "鼓组节奏 (Drums / Transient)",
                description: "强化底鼓下盘与军鼓瞬态脆度，抑制箱音堆积",
                anchors: new[] { (25.0, 0.5), (60.0, 2.8), (160.0, 1.4), (400.0, -1.8), (1000.0, -0.3), (4500.0, 2.5), (10000.0, 1.8), (16000.0, 0.4) }),

            CreateTarget(
                id: "modern_pop",
                displayName: "流行现代 (Modern Pop)",
                description: "现代流媒体微笑轮廓：紧致低频、清透中高频与空气感",
                anchors: new[] { (25.0, 0.5), (60.0, 2.4), (130.0, 0.8), (300.0, -1.0), (1000.0, 0.0), (3200.0, 1.6), (10000.0, 2.0), (16000.0, 1.0) }),

            CreateTarget(
                id: "acg_vibrant",
                displayName: "ACG 动感 / 二次元 (ACG Vibrant)",
                description: "专为日系电音与女声优化：明亮通透的华丽感与弹性低频",
                anchors: new[] { (25.0, 0.4), (65.0, 2.6), (160.0, 0.4), (300.0, -1.2), (1000.0, 0.2), (3500.0, 2.4), (10000.0, 2.8), (16000.0, 1.6) }),

            CreateTarget(
                id: "bright_airy",
                displayName: "通透空气 (Bright & Airy)",
                description: "轻盈低频配合宽广极高频空气感，提升空间解析力",
                anchors: new[] { (25.0, -2.2), (80.0, -1.0), (250.0, -0.6), (1000.0, 0.0), (3500.0, 1.6), (10000.0, 3.4), (16000.0, 3.6) }),

            CreateTarget(
                id: "late_night",
                displayName: "深夜轻听 (Late Night Fletcher-Munson)",
                description: "基于 Fletcher-Munson 等响曲线补偿低音量下的低音与细节流失",
                anchors: new[] { (25.0, 3.0), (60.0, 3.6), (140.0, 1.6), (400.0, 0.0), (1500.0, -0.8), (3200.0, -1.2), (9000.0, 2.0), (16000.0, 2.4) })
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
                    // Smoothstep interpolation for natural acoustic curves
                    double s = t * t * (3.0 - 2.0 * t);
                    result[i] = Math.Round(anchors[a].Db * (1.0 - s) + anchors[a + 1].Db * s, 2);
                    break;
                }
            }
        }
        return result;
    }
}
