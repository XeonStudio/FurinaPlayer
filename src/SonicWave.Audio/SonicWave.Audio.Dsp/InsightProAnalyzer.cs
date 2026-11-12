using System;
using System.Collections.Generic;

namespace SonicWave.Audio.Dsp;

public enum InsightTargetPreset
{
    StreamingMinus14,
    AppleMinus16,
    EbuR128Minus23,
    AtscA85Minus24,
    CdLoudMinus9,
    Custom
}

public readonly record struct InsightHistoryPoint(
    double TimeSeconds,
    float MomentaryLufs,
    float ShortTermLufs,
    float IntegratedLufs,
    float TargetLufs);

public sealed record InsightProSnapshot(
    int SampleRate,
    int Channels,
    double ElapsedSeconds,
    InsightTargetPreset TargetPreset,
    float TargetLufs,
    float MaxTruePeakDbTp,
    float MomentaryLufs,
    float ShortTermLufs,
    float IntegratedLufs,
    float LraLu,
    float TruePeakDbTp,
    float LeftTruePeakDbTp,
    float RightTruePeakDbTp,
    bool MomentaryExceedsTarget,
    bool ShortTermExceedsTarget,
    bool IntegratedExceedsTarget,
    bool TruePeakExceedsTarget,
    float LeftPeakDb,
    float RightPeakDb,
    float LeftRmsDb,
    float RightRmsDb,
    float LeftPeakHoldDb,
    float RightPeakHoldDb,
    bool LeftClipped,
    bool RightClipped,
    float StereoCorrelation,
    float PhaseDifferenceDeg,
    float StereoBalance,
    float[] VectorPointsL,
    float[] VectorPointsR,
    float[] SpectrumDb,
    float[] SpectrumPeakHoldDb,
    float[] SpectrumFrequenciesHz,
    InsightHistoryPoint[] HistorySamples,
    float[][] SpectrogramFrames);

/// <summary>
/// Native iZotope Insight Pro style audio metering &amp; spectral analyzer.
/// Thread-safe lock-protected DSP engine supporting both real-time playback PCM feeding
/// and offline editor selection/window analysis.
/// Implements ITU-R BS.1770-4 K-weighting + dual-gated Integrated loudness, EBU Tech 3342 LRA,
/// 4x polyphase oversampled True Peak, correlation/vectorscope, 128-band log FFT + peak-hold,
/// 240-point loudness history, and 64x64 3D spectrogram slices.
/// </summary>
public sealed class InsightProAnalyzer
{
    public const int SpectrumBandCount = 128;
    public const int SpectrogramFreqBins = 64;
    public const int SpectrogramMaxFrames = 64;
    public const int HistoryCapacity = 240;
    public const int MaxVectorPoints = 512;

    private const int FftSize = 4096;
    private const int FftHalf = FftSize / 2;
    private const float FloorLufs = -70.0f;
    private const float FloorDb = -110.0f;

    // 0.1 LU resolution histogram from -70.0 to +10.0 LUFS for O(1) memory Integrated & LRA
    private const float HistMinLufs = -70.0f;
    private const float HistMaxLufs = 10.0f;
    private const float HistStepLu = 0.1f;
    private const int HistBinCount = 801;

    // 4x oversampling polyphase FIR (4 phases x 12 taps)
    private const int TruePeakPhaseCount = 4;
    private const int TruePeakTapCount = 12;
    private static readonly float[][] TruePeakPolyphase = BuildPolyphaseSyncTable();

    private readonly object _sync = new();

    // Cached K-weighting filter coefficients for active sample rate
    private int _sampleRate = 48000;
    private int _channels = 2;
    private double _k1b0, _k1b1, _k1b2, _k1a1, _k1a2;
    private double _k2b0, _k2b1, _k2b2, _k2a1, _k2a2;

    // Per-channel K-weighting filter state (supports up to 8 channels)
    private const int MaxSupportedChannels = 8;
    private readonly double[] _k1x1 = new double[MaxSupportedChannels];
    private readonly double[] _k1x2 = new double[MaxSupportedChannels];
    private readonly double[] _k1y1 = new double[MaxSupportedChannels];
    private readonly double[] _k1y2 = new double[MaxSupportedChannels];
    private readonly double[] _k2x1 = new double[MaxSupportedChannels];
    private readonly double[] _k2x2 = new double[MaxSupportedChannels];
    private readonly double[] _k2y1 = new double[MaxSupportedChannels];
    private readonly double[] _k2y2 = new double[MaxSupportedChannels];

    // 4x True Peak delay lines for L and R (and mono/multi)
    private readonly float[] _tpHistL = new float[TruePeakTapCount];
    private readonly float[] _tpHistR = new float[TruePeakTapCount];
    private int _tpHistIdx;

    // Loudness sub-block accumulator (100ms hop, 400ms Momentary = 4 hops, 3.0s ShortTerm = 30 hops)
    private int _hopFrames = 4800;
    private double _currentSubBlockEnergySum;
    private int _currentSubBlockFrames;
    private readonly double[] _subBlockRing = new double[30];
    private int _subBlockWriteIdx;
    private int _subBlockFilledCount;

    // Dual-gate Integrated & LRA histograms
    private readonly long[] _intHistCounts = new long[HistBinCount];
    private readonly double[] _intHistPowerSum = new double[HistBinCount];
    private readonly long[] _lraHistCounts = new long[HistBinCount];
    private readonly double[] _lraHistPowerSum = new double[HistBinCount];

    // Cached loudness metrics
    private float _momentaryLufs = FloorLufs;
    private float _shortTermLufs = FloorLufs;
    private float _integratedLufs = FloorLufs;
    private float _lraLu;
    private float _truePeakDbTp = FloorDb;
    private float _leftTruePeakDbTp = FloorDb;
    private float _rightTruePeakDbTp = FloorDb;

    // Level meters
    private float _leftPeakDb = FloorDb;
    private float _rightPeakDb = FloorDb;
    private float _leftRmsDb = FloorDb;
    private float _rightRmsDb = FloorDb;
    private float _leftPeakHoldDb = FloorDb;
    private float _rightPeakHoldDb = FloorDb;
    private double _leftPeakHoldTimerSec;
    private double _rightPeakHoldTimerSec;
    private bool _leftClipped;
    private bool _rightClipped;

    // Sound field
    private float _stereoCorrelation = 1.0f;
    private float _phaseDifferenceDeg;
    private float _stereoBalance;
    private float[] _vectorL = Array.Empty<float>();
    private float[] _vectorR = Array.Empty<float>();

    // FFT & Spectrum
    private readonly float[] _monoRing = new float[FftSize];
    private int _monoRingWrite;
    private int _monoRingCount;
    private int _framesSinceLastFft;
    private int _framesSinceLastHistory;

    private readonly double[] _hannWindow = new double[FftSize];
    private readonly double[] _fftReal = new double[FftSize];
    private readonly double[] _fftImag = new double[FftSize];
    private readonly double[] _cosTable = new double[FftSize / 2];
    private readonly double[] _sinTable = new double[FftSize / 2];

    private readonly float[] _spectrumFreqs = new float[SpectrumBandCount];
    private readonly float[] _spectrumDb = new float[SpectrumBandCount];
    private readonly float[] _spectrumPeakHoldDb = new float[SpectrumBandCount];
    private readonly float[] _spectrumPeakHoldTimers = new float[SpectrumBandCount];

    // History & Spectrogram rolling buffers
    private readonly InsightHistoryPoint[] _historyRing = new InsightHistoryPoint[HistoryCapacity];
    private int _historyWriteIdx;
    private int _historyCount;

    private readonly float[][] _spectrogramRing = new float[SpectrogramMaxFrames][];
    private int _spectrogramWriteIdx;
    private int _spectrogramCount;

    private double _elapsedSeconds;

    // Target configuration
    private InsightTargetPreset _targetPreset = InsightTargetPreset.StreamingMinus14;
    private float _customTargetLufs = -14.0f;
    private float _customMaxTruePeakDbTp = -1.0f;

    public InsightProAnalyzer()
    {
        for (int i = 0; i < FftSize; i++)
        {
            _hannWindow[i] = 0.5 * (1.0 - Math.Cos(2.0 * Math.PI * i / (FftSize - 1)));
        }

        for (int i = 0; i < FftSize / 2; i++)
        {
            double angle = -2.0 * Math.PI * i / FftSize;
            _cosTable[i] = Math.Cos(angle);
            _sinTable[i] = Math.Sin(angle);
        }

        double logMin = Math.Log10(20.0);
        double logMax = Math.Log10(20000.0);
        for (int b = 0; b < SpectrumBandCount; b++)
        {
            double f0 = Math.Pow(10.0, logMin + (logMax - logMin) * b / SpectrumBandCount);
            double f1 = Math.Pow(10.0, logMin + (logMax - logMin) * (b + 1) / SpectrumBandCount);
            _spectrumFreqs[b] = (float)Math.Sqrt(f0 * f1);
            _spectrumDb[b] = FloorDb;
            _spectrumPeakHoldDb[b] = FloorDb;
        }

        for (int i = 0; i < SpectrogramMaxFrames; i++)
        {
            _spectrogramRing[i] = new float[SpectrogramFreqBins];
        }

        UpdateSampleRateCoefficients(48000);
    }

    public InsightTargetPreset TargetPreset
    {
        get { lock (_sync) return _targetPreset; }
        set { lock (_sync) _targetPreset = value; }
    }

    public float CustomTargetLufs
    {
        get { lock (_sync) return _customTargetLufs; }
        set { lock (_sync) _customTargetLufs = Math.Clamp(value, -50.0f, -1.0f); }
    }

    public float CustomMaxTruePeakDbTp
    {
        get { lock (_sync) return _customMaxTruePeakDbTp; }
        set { lock (_sync) _customMaxTruePeakDbTp = Math.Clamp(value, -12.0f, 0.0f); }
    }

    public static (float TargetLufs, float MaxTruePeakDbTp, string DisplayName) GetPresetSpec(InsightTargetPreset preset) => preset switch
    {
        InsightTargetPreset.StreamingMinus14 => (-14.0f, -1.0f, "Streaming (-14 LUFS / -1.0 dBTP)"),
        InsightTargetPreset.AppleMinus16 => (-16.0f, -1.0f, "Apple Sound Check (-16 LUFS / -1.0 dBTP)"),
        InsightTargetPreset.EbuR128Minus23 => (-23.0f, -1.0f, "EBU R128 (-23 LUFS / -1.0 dBTP)"),
        InsightTargetPreset.AtscA85Minus24 => (-24.0f, -2.0f, "ATSC A/85 (-24 LUFS / -2.0 dBTP)"),
        InsightTargetPreset.CdLoudMinus9 => (-9.0f, -0.3f, "CD / Loud (-9 LUFS / -0.3 dBTP)"),
        _ => (-14.0f, -1.0f, "Custom Target")
    };

    public void Reset()
    {
        lock (_sync)
        {
            ResetInternal(clearRealtimeSpectrum: true);
        }
    }

    public void ResetIntegrated()
    {
        lock (_sync)
        {
            ResetInternal(clearRealtimeSpectrum: false);
        }
    }

    /// <summary>
    /// Feeds interleaved PCM samples from real-time playback or streaming preview.
    /// </summary>
    public void PushSamples(ReadOnlySpan<float> interleaved, int channels, int sampleRate)
    {
        if (interleaved.IsEmpty || channels <= 0 || sampleRate <= 0)
        {
            return;
        }

        int frames = interleaved.Length / channels;
        if (frames <= 0)
        {
            return;
        }

        lock (_sync)
        {
            PushSamplesLocked(interleaved, frames, channels, sampleRate, computeFftNow: true, historyStepSec: 0.2);
        }
    }

    /// <summary>
    /// Analyzes an offline audio selection/window `[startFrame .. startFrame + frameCount)`
    /// and returns an immutable <see cref="InsightProSnapshot"/>.
    /// </summary>
    public InsightProSnapshot AnalyzeWindow(float[] interleaved, int channels, int sampleRate, int startFrame, int frameCount)
    {
        if (interleaved == null || interleaved.Length == 0 || channels <= 0 || sampleRate <= 0)
        {
            lock (_sync)
            {
                return BuildSnapshotLocked();
            }
        }

        int totalFrames = interleaved.Length / channels;
        int safeStart = Math.Clamp(startFrame, 0, Math.Max(0, totalFrames - 1));
        int safeFrames = Math.Clamp(frameCount, 0, totalFrames - safeStart);
        if (safeFrames <= 0)
        {
            lock (_sync)
            {
                return BuildSnapshotLocked();
            }
        }

        lock (_sync)
        {
            ResetInternal(clearRealtimeSpectrum: true);
            UpdateSampleRateCoefficients(sampleRate);
            _channels = channels;

            double windowSec = (double)safeFrames / sampleRate;
            // Choose history interval so the selected window maps cleanly onto HistoryCapacity (240)
            double historyStepSec = Math.Max(0.05, windowSec / 180.0);
            int chunkFrames = Math.Max(512, Math.Min(4096, sampleRate / 20)); // ~50ms chunks
            int fftIntervalFrames = Math.Max(chunkFrames, safeFrames / SpectrogramMaxFrames);

            int processed = 0;
            int nextFftAt = 0;
            while (processed < safeFrames)
            {
                int count = Math.Min(chunkFrames, safeFrames - processed);
                int offset = (safeStart + processed) * channels;
                bool isLast = (processed + count) >= safeFrames;
                bool runFft = isLast || processed >= nextFftAt;
                if (runFft)
                {
                    nextFftAt = processed + fftIntervalFrames;
                }

                var span = interleaved.AsSpan(offset, count * channels);
                PushSamplesLocked(span, count, channels, sampleRate, computeFftNow: runFft, historyStepSec: historyStepSec);
                processed += count;
            }

            // Ensure at least one history point exists even for very short selections (<50ms)
            if (_historyCount == 0)
            {
                AppendHistoryPointLocked();
            }

            return BuildSnapshotLocked();
        }
    }

    public InsightProSnapshot GetSnapshot()
    {
        lock (_sync)
        {
            return BuildSnapshotLocked();
        }
    }

    private void ResetInternal(bool clearRealtimeSpectrum)
    {
        Array.Clear(_k1x1);
        Array.Clear(_k1x2);
        Array.Clear(_k1y1);
        Array.Clear(_k1y2);
        Array.Clear(_k2x1);
        Array.Clear(_k2x2);
        Array.Clear(_k2y1);
        Array.Clear(_k2y2);
        Array.Clear(_tpHistL);
        Array.Clear(_tpHistR);
        _tpHistIdx = 0;

        _currentSubBlockEnergySum = 0;
        _currentSubBlockFrames = 0;
        Array.Clear(_subBlockRing);
        _subBlockWriteIdx = 0;
        _subBlockFilledCount = 0;

        Array.Clear(_intHistCounts);
        Array.Clear(_intHistPowerSum);
        Array.Clear(_lraHistCounts);
        Array.Clear(_lraHistPowerSum);

        _momentaryLufs = FloorLufs;
        _shortTermLufs = FloorLufs;
        _integratedLufs = FloorLufs;
        _lraLu = 0.0f;
        _truePeakDbTp = FloorDb;
        _leftTruePeakDbTp = FloorDb;
        _rightTruePeakDbTp = FloorDb;

        _leftPeakHoldDb = FloorDb;
        _rightPeakHoldDb = FloorDb;
        _leftPeakHoldTimerSec = 0;
        _rightPeakHoldTimerSec = 0;
        _leftClipped = false;
        _rightClipped = false;

        _historyWriteIdx = 0;
        _historyCount = 0;
        _framesSinceLastHistory = 0;
        _elapsedSeconds = 0;

        if (clearRealtimeSpectrum)
        {
            Array.Clear(_monoRing);
            _monoRingWrite = 0;
            _monoRingCount = 0;
            _framesSinceLastFft = 0;

            _leftPeakDb = FloorDb;
            _rightPeakDb = FloorDb;
            _leftRmsDb = FloorDb;
            _rightRmsDb = FloorDb;
            _stereoCorrelation = 1.0f;
            _phaseDifferenceDeg = 0.0f;
            _stereoBalance = 0.0f;
            _vectorL = Array.Empty<float>();
            _vectorR = Array.Empty<float>();

            Array.Fill(_spectrumDb, FloorDb);
            Array.Fill(_spectrumPeakHoldDb, FloorDb);
            Array.Clear(_spectrumPeakHoldTimers);

            for (int i = 0; i < SpectrogramMaxFrames; i++)
            {
                Array.Clear(_spectrogramRing[i]);
            }
            _spectrogramWriteIdx = 0;
            _spectrogramCount = 0;
        }
    }

    private void PushSamplesLocked(
        ReadOnlySpan<float> interleaved,
        int frames,
        int channels,
        int sampleRate,
        bool computeFftNow,
        double historyStepSec)
    {
        if (sampleRate != _sampleRate)
        {
            UpdateSampleRateCoefficients(sampleRate);
        }
        _channels = channels;

        int activeChannels = Math.Min(channels, MaxSupportedChannels);
        double chunkDurationSec = (double)frames / _sampleRate;
        _elapsedSeconds += chunkDurationSec;

        float peakL = 0f;
        float peakR = 0f;
        float tpPeakL = 0f;
        float tpPeakR = 0f;
        double sumSqL = 0.0;
        double sumSqR = 0.0;
        double dotLR = 0.0;

        int vectorStep = Math.Max(1, frames / MaxVectorPoints);
        int vectorCount = Math.Min(MaxVectorPoints, (frames + vectorStep - 1) / vectorStep);
        float[] vecL = new float[vectorCount];
        float[] vecR = new float[vectorCount];
        int vecIdx = 0;

        for (int i = 0; i < frames; i++)
        {
            int baseIdx = i * channels;
            float l = SanitizeSample(interleaved[baseIdx]);
            float r = channels > 1 ? SanitizeSample(interleaved[baseIdx + 1]) : l;

            float absL = Math.Abs(l);
            float absR = Math.Abs(r);
            if (absL > peakL) peakL = absL;
            if (absR > peakR) peakR = absR;
            if (absL >= 0.9999f) _leftClipped = true;
            if (absR >= 0.9999f) _rightClipped = true;

            sumSqL += l * l;
            sumSqR += r * r;
            dotLR += l * r;

            // 4x Oversampled True Peak estimation on L & R
            _tpHistL[_tpHistIdx] = l;
            _tpHistR[_tpHistIdx] = r;
            _tpHistIdx = (_tpHistIdx + 1) % TruePeakTapCount;

            float tpL = EvaluateTruePeak(_tpHistL, _tpHistIdx);
            float tpR = channels > 1 ? EvaluateTruePeak(_tpHistR, _tpHistIdx) : tpL;
            if (tpL > tpPeakL) tpPeakL = tpL;
            if (tpR > tpPeakR) tpPeakR = tpR;

            // Sample down into vectorscope points
            if (i % vectorStep == 0 && vecIdx < vectorCount)
            {
                vecL[vecIdx] = l;
                vecR[vecIdx] = r;
                vecIdx++;
            }

            // Mono downmix into FFT ring
            float mono = channels > 1 ? 0.5f * (l + r) : l;
            _monoRing[_monoRingWrite] = mono;
            _monoRingWrite = (_monoRingWrite + 1) & (FftSize - 1);
            if (_monoRingCount < FftSize) _monoRingCount++;

            // ITU-R BS.1770-4 K-weighting across channels
            double frameEnergy = 0.0;
            for (int ch = 0; ch < activeChannels; ch++)
            {
                // Standard BS.1770-4 channel weights: L/R/C = 1.0, LFE (ch 3 in 5.1) = 0.0, Ls/Rs = 1.4125
                double weight = 1.0;
                if (activeChannels >= 5)
                {
                    if (ch == 3) weight = 0.0;
                    else if (ch >= 4) weight = 1.4125375446227544; // 10^(1.5/10)
                }

                if (weight <= 0.0) continue;

                double x = SanitizeSample(interleaved[baseIdx + ch]);
                double s1 = _k1b0 * x + _k1b1 * _k1x1[ch] + _k1b2 * _k1x2[ch]
                            - _k1a1 * _k1y1[ch] - _k1a2 * _k1y2[ch];
                _k1x2[ch] = _k1x1[ch];
                _k1x1[ch] = x;
                _k1y2[ch] = _k1y1[ch];
                _k1y1[ch] = s1;

                double s2 = _k2b0 * s1 + _k2b1 * _k2x1[ch] + _k2b2 * _k2x2[ch]
                            - _k2a1 * _k2y1[ch] - _k2a2 * _k2y2[ch];
                _k2x2[ch] = _k2x1[ch];
                _k2x1[ch] = s1;
                _k2y2[ch] = _k2y1[ch];
                _k2y1[ch] = s2;

                frameEnergy += weight * s2 * s2;
            }

            _currentSubBlockEnergySum += frameEnergy;
            _currentSubBlockFrames++;

            if (_currentSubBlockFrames >= _hopFrames)
            {
                CommitSubBlockLocked(_currentSubBlockEnergySum / _currentSubBlockFrames);
                _currentSubBlockEnergySum = 0.0;
                _currentSubBlockFrames = 0;
            }
        }

        // Update instant L/R Peak, RMS, PeakHold
        float instPeakLDb = LinearToDb(peakL, FloorDb);
        float instPeakRDb = LinearToDb(peakR, FloorDb);
        float instRmsLDb = LinearToDb((float)Math.Sqrt(sumSqL / frames), FloorDb);
        float instRmsRDb = LinearToDb((float)Math.Sqrt(sumSqR / frames), FloorDb);

        float smoothAlpha = (float)(1.0 - Math.Exp(-chunkDurationSec / 0.08));
        _leftPeakDb = instPeakLDb >= _leftPeakDb ? instPeakLDb : _leftPeakDb + (instPeakLDb - _leftPeakDb) * smoothAlpha;
        _rightPeakDb = instPeakRDb >= _rightPeakDb ? instPeakRDb : _rightPeakDb + (instPeakRDb - _rightPeakDb) * smoothAlpha;
        _leftRmsDb = _leftRmsDb <= FloorDb + 1f ? instRmsLDb : _leftRmsDb + (instRmsLDb - _leftRmsDb) * smoothAlpha;
        _rightRmsDb = _rightRmsDb <= FloorDb + 1f ? instRmsRDb : _rightRmsDb + (instRmsRDb - _rightRmsDb) * smoothAlpha;

        UpdatePeakHold(ref _leftPeakHoldDb, ref _leftPeakHoldTimerSec, instPeakLDb, chunkDurationSec);
        UpdatePeakHold(ref _rightPeakHoldDb, ref _rightPeakHoldTimerSec, instPeakRDb, chunkDurationSec);

        // Update session max True Peak (dBTP)
        float tpLDb = LinearToDb(tpPeakL, FloorDb);
        float tpRDb = LinearToDb(tpPeakR, FloorDb);
        if (tpLDb > _leftTruePeakDbTp) _leftTruePeakDbTp = tpLDb;
        if (tpRDb > _rightTruePeakDbTp) _rightTruePeakDbTp = tpRDb;
        _truePeakDbTp = Math.Max(_leftTruePeakDbTp, _rightTruePeakDbTp);

        // Sound field metrics
        double energyDenom = Math.Sqrt(sumSqL * sumSqR);
        float instCorr = energyDenom > 1e-12 ? (float)Math.Clamp(dotLR / energyDenom, -1.0, 1.0) : 1.0f;
        _stereoCorrelation = _stereoCorrelation + (instCorr - _stereoCorrelation) * Math.Clamp(smoothAlpha * 1.4f, 0.1f, 1.0f);
        _phaseDifferenceDeg = (float)(Math.Acos(Math.Clamp(_stereoCorrelation, -1.0f, 1.0f)) * (180.0 / Math.PI));

        double rmsL = Math.Sqrt(sumSqL / frames);
        double rmsR = Math.Sqrt(sumSqR / frames);
        double balDenom = rmsL + rmsR;
        float instBalance = balDenom > 1e-9 ? (float)Math.Clamp((rmsR - rmsL) / balDenom, -1.0, 1.0) : 0.0f;
        _stereoBalance = _stereoBalance + (instBalance - _stereoBalance) * Math.Clamp(smoothAlpha * 1.2f, 0.1f, 1.0f);

        if (vecIdx == vectorCount)
        {
            _vectorL = vecL;
            _vectorR = vecR;
        }
        else
        {
            _vectorL = vecL[..vecIdx];
            _vectorR = vecR[..vecIdx];
        }

        // Update partial Momentary / ShortTerm if sub-blocks haven't completed yet (short windows)
        UpdatePartialLoudnessEstimateLocked();

        // Spectrum + Spectrogram
        _framesSinceLastFft += frames;
        int fftStepFrames = Math.Max(512, _sampleRate / 25); // ~40ms
        if (computeFftNow && _monoRingCount >= 256 && (_framesSinceLastFft >= fftStepFrames || _spectrogramCount == 0))
        {
            _framesSinceLastFft = 0;
            ComputeSpectrumAndSpectrogramLocked(chunkDurationSec);
        }
        else
        {
            DecaySpectrumPeakHoldLocked((float)chunkDurationSec);
        }

        // History sampling
        _framesSinceLastHistory += frames;
        int historyIntervalFrames = Math.Max(1, (int)Math.Round(historyStepSec * _sampleRate));
        if (_framesSinceLastHistory >= historyIntervalFrames)
        {
            _framesSinceLastHistory = 0;
            AppendHistoryPointLocked();
        }
    }

    private void CommitSubBlockLocked(double subBlockPower)
    {
        _subBlockRing[_subBlockWriteIdx] = subBlockPower;
        _subBlockWriteIdx = (_subBlockWriteIdx + 1) % _subBlockRing.Length;
        if (_subBlockFilledCount < _subBlockRing.Length)
        {
            _subBlockFilledCount++;
        }

        // Momentary (4 x 100ms = 400ms)
        int mCount = Math.Min(4, _subBlockFilledCount);
        double mSum = 0.0;
        for (int i = 0; i < mCount; i++)
        {
            int idx = (_subBlockWriteIdx - 1 - i + _subBlockRing.Length) % _subBlockRing.Length;
            mSum += _subBlockRing[idx];
        }
        double mPower = mCount > 0 ? mSum / mCount : 0.0;
        _momentaryLufs = PowerToLufs(mPower);

        if (_subBlockFilledCount >= 4)
        {
            AddToHistogram(_intHistCounts, _intHistPowerSum, mPower, _momentaryLufs);
            ComputeIntegratedLufsLocked();
        }

        // Short-Term (30 x 100ms = 3.0s)
        int stCount = Math.Min(30, _subBlockFilledCount);
        double stSum = 0.0;
        for (int i = 0; i < stCount; i++)
        {
            int idx = (_subBlockWriteIdx - 1 - i + _subBlockRing.Length) % _subBlockRing.Length;
            stSum += _subBlockRing[idx];
        }
        double stPower = stCount > 0 ? stSum / stCount : 0.0;
        _shortTermLufs = PowerToLufs(stPower);

        if (_subBlockFilledCount >= 10) // allow LRA accumulation once >= 1.0s of blocks exist
        {
            AddToHistogram(_lraHistCounts, _lraHistPowerSum, stPower, _shortTermLufs);
            ComputeLraLocked();
        }
    }

    private void UpdatePartialLoudnessEstimateLocked()
    {
        if (_subBlockFilledCount == 0 && _currentSubBlockFrames > 0)
        {
            double partialPower = _currentSubBlockEnergySum / _currentSubBlockFrames;
            float partialLufs = PowerToLufs(partialPower);
            _momentaryLufs = partialLufs;
            _shortTermLufs = partialLufs;
            if (partialLufs > FloorLufs)
            {
                _integratedLufs = partialLufs;
            }
        }
    }

    private static void AddToHistogram(long[] counts, double[] powerSums, double power, float lufs)
    {
        if (lufs <= HistMinLufs) return;
        int bin = Math.Clamp((int)Math.Floor((lufs - HistMinLufs) / HistStepLu), 0, HistBinCount - 1);
        counts[bin]++;
        powerSums[bin] += power;
    }

    private void ComputeIntegratedLufsLocked()
    {
        long totalCount = 0;
        double totalPower = 0.0;
        for (int b = 0; b < HistBinCount; b++)
        {
            totalCount += _intHistCounts[b];
            totalPower += _intHistPowerSum[b];
        }

        if (totalCount == 0 || totalPower <= 1e-12)
        {
            _integratedLufs = FloorLufs;
            return;
        }

        double ungatedLufs = -0.691 + 10.0 * Math.Log10(totalPower / totalCount);
        double relThreshold = Math.Max(-70.0, ungatedLufs - 10.0);
        int startBin = Math.Clamp((int)Math.Ceiling((relThreshold - HistMinLufs) / HistStepLu), 0, HistBinCount - 1);

        long gatedCount = 0;
        double gatedPower = 0.0;
        for (int b = startBin; b < HistBinCount; b++)
        {
            gatedCount += _intHistCounts[b];
            gatedPower += _intHistPowerSum[b];
        }

        _integratedLufs = gatedCount > 0 ? PowerToLufs(gatedPower / gatedCount) : FloorLufs;
    }

    private void ComputeLraLocked()
    {
        long totalCount = 0;
        double totalPower = 0.0;
        for (int b = 0; b < HistBinCount; b++)
        {
            totalCount += _lraHistCounts[b];
            totalPower += _lraHistPowerSum[b];
        }

        if (totalCount < 2 || totalPower <= 1e-12)
        {
            _lraLu = 0.0f;
            return;
        }

        double ungatedLufs = -0.691 + 10.0 * Math.Log10(totalPower / totalCount);
        double relThreshold = Math.Max(-70.0, ungatedLufs - 20.0);
        int startBin = Math.Clamp((int)Math.Ceiling((relThreshold - HistMinLufs) / HistStepLu), 0, HistBinCount - 1);

        long gatedCount = 0;
        for (int b = startBin; b < HistBinCount; b++)
        {
            gatedCount += _lraHistCounts[b];
        }

        if (gatedCount < 2)
        {
            _lraLu = 0.0f;
            return;
        }

        double p10Target = gatedCount * 0.10;
        double p95Target = gatedCount * 0.95;
        long running = 0;
        float p10 = HistMinLufs;
        float p95 = HistMinLufs;
        bool found10 = false;

        for (int b = startBin; b < HistBinCount; b++)
        {
            running += _lraHistCounts[b];
            float binLufs = HistMinLufs + (b + 0.5f) * HistStepLu;
            if (!found10 && running >= p10Target)
            {
                p10 = binLufs;
                found10 = true;
            }
            if (running >= p95Target)
            {
                p95 = binLufs;
                break;
            }
        }

        _lraLu = Math.Max(0.0f, p95 - p10);
    }

    private void ComputeSpectrumAndSpectrogramLocked(double elapsedStepSec)
    {
        int start = (_monoRingWrite - FftSize + FftSize) & (FftSize - 1);
        for (int i = 0; i < FftSize; i++)
        {
            float sample = _monoRing[(start + i) & (FftSize - 1)];
            _fftReal[i] = sample * _hannWindow[i];
            _fftImag[i] = 0.0;
        }

        RunRadix2Fft(_fftReal, _fftImag, _cosTable, _sinTable);

        // Convert to magnitude dBFS (Hann coherent gain = 0.5, single-sided = 2/N -> norm = N/4)
        double invNorm = 4.0 / FftSize;
        double logMin = Math.Log10(20.0);
        double logMax = Math.Log10(20000.0);
        double nyquist = _sampleRate * 0.5;
        float alpha = (float)Math.Clamp(1.0 - Math.Exp(-Math.Max(0.016, elapsedStepSec) / 0.09), 0.18, 0.85);
        float dt = (float)Math.Max(0.016, elapsedStepSec);

        for (int b = 0; b < SpectrumBandCount; b++)
        {
            double f0 = Math.Pow(10.0, logMin + (logMax - logMin) * b / SpectrumBandCount);
            double f1 = Math.Pow(10.0, logMin + (logMax - logMin) * (b + 1) / SpectrumBandCount);
            double fCenter = Math.Sqrt(f0 * f1);

            float db;
            if (fCenter >= nyquist)
            {
                db = FloorDb;
            }
            else
            {
                double exactBin0 = f0 * FftSize / _sampleRate;
                double exactBin1 = f1 * FftSize / _sampleRate;
                int bin0 = Math.Clamp((int)Math.Floor(exactBin0), 1, FftHalf - 1);
                int bin1 = Math.Clamp((int)Math.Ceiling(exactBin1), bin0, FftHalf - 1);

                double mag;
                if (bin1 - bin0 <= 1)
                {
                    // Smooth linear interpolation for sub-bin low frequencies
                    double exact = Math.Clamp(fCenter * FftSize / _sampleRate, 1.0, FftHalf - 1.001);
                    int lo = (int)exact;
                    double frac = exact - lo;
                    double mLo = Math.Sqrt(_fftReal[lo] * _fftReal[lo] + _fftImag[lo] * _fftImag[lo]) * invNorm;
                    double mHi = Math.Sqrt(_fftReal[lo + 1] * _fftReal[lo + 1] + _fftImag[lo + 1] * _fftImag[lo + 1]) * invNorm;
                    mag = mLo + (mHi - mLo) * frac;
                }
                else
                {
                    double maxMag = 0.0;
                    for (int k = bin0; k <= bin1; k++)
                    {
                        double m = Math.Sqrt(_fftReal[k] * _fftReal[k] + _fftImag[k] * _fftImag[k]) * invNorm;
                        if (m > maxMag) maxMag = m;
                    }
                    mag = maxMag;
                }

                db = mag > 1e-6 ? Math.Clamp((float)(20.0 * Math.Log10(mag)), FloorDb, 0.0f) : FloorDb;
            }

            _spectrumDb[b] = _spectrumDb[b] <= FloorDb + 0.5f ? db : _spectrumDb[b] + (db - _spectrumDb[b]) * alpha;

            if (_spectrumDb[b] >= _spectrumPeakHoldDb[b])
            {
                _spectrumPeakHoldDb[b] = _spectrumDb[b];
                _spectrumPeakHoldTimers[b] = 0.65f;
            }
            else
            {
                if (_spectrumPeakHoldTimers[b] > 0f)
                {
                    _spectrumPeakHoldTimers[b] -= dt;
                }
                else
                {
                    _spectrumPeakHoldDb[b] = Math.Max(_spectrumDb[b], _spectrumPeakHoldDb[b] - 12.0f * dt);
                }
            }
        }

        // Push 64-bin spectrogram slice normalized [0..1] over [-96 .. -6 dBFS]
        float[] slice = _spectrogramRing[_spectrogramWriteIdx];
        for (int i = 0; i < SpectrogramFreqBins; i++)
        {
            int src0 = i * 2;
            int src1 = Math.Min(SpectrumBandCount - 1, src0 + 1);
            float avgDb = 0.5f * (_spectrumDb[src0] + _spectrumDb[src1]);
            slice[i] = Math.Clamp((avgDb - (-96.0f)) / 90.0f, 0.0f, 1.0f);
        }
        _spectrogramWriteIdx = (_spectrogramWriteIdx + 1) % SpectrogramMaxFrames;
        if (_spectrogramCount < SpectrogramMaxFrames) _spectrogramCount++;
    }

    private void DecaySpectrumPeakHoldLocked(float dt)
    {
        if (dt <= 0f) return;
        for (int b = 0; b < SpectrumBandCount; b++)
        {
            if (_spectrumPeakHoldTimers[b] > 0f)
            {
                _spectrumPeakHoldTimers[b] -= dt;
            }
            else
            {
                _spectrumPeakHoldDb[b] = Math.Max(_spectrumDb[b], _spectrumPeakHoldDb[b] - 12.0f * dt);
            }
        }
    }

    private void AppendHistoryPointLocked()
    {
        var (targetLufs, _) = GetActiveTargetsLocked();
        _historyRing[_historyWriteIdx] = new InsightHistoryPoint(
            _elapsedSeconds,
            _momentaryLufs,
            _shortTermLufs,
            _integratedLufs,
            targetLufs);
        _historyWriteIdx = (_historyWriteIdx + 1) % HistoryCapacity;
        if (_historyCount < HistoryCapacity) _historyCount++;
    }

    private (float TargetLufs, float MaxTruePeakDbTp) GetActiveTargetsLocked()
    {
        if (_targetPreset == InsightTargetPreset.Custom)
        {
            return (_customTargetLufs, _customMaxTruePeakDbTp);
        }
        var spec = GetPresetSpec(_targetPreset);
        return (spec.TargetLufs, spec.MaxTruePeakDbTp);
    }

    private InsightProSnapshot BuildSnapshotLocked()
    {
        var (targetLufs, maxTpDbTp) = GetActiveTargetsLocked();

        InsightHistoryPoint[] history = new InsightHistoryPoint[_historyCount];
        int hStart = (_historyWriteIdx - _historyCount + HistoryCapacity) % HistoryCapacity;
        for (int i = 0; i < _historyCount; i++)
        {
            history[i] = _historyRing[(hStart + i) % HistoryCapacity];
        }

        float[][] spectrogram = new float[_spectrogramCount][];
        int sStart = (_spectrogramWriteIdx - _spectrogramCount + SpectrogramMaxFrames) % SpectrogramMaxFrames;
        for (int i = 0; i < _spectrogramCount; i++)
        {
            float[] src = _spectrogramRing[(sStart + i) % SpectrogramMaxFrames];
            float[] copy = new float[src.Length];
            Array.Copy(src, copy, src.Length);
            spectrogram[i] = copy;
        }

        float[] specCopy = new float[SpectrumBandCount];
        float[] peakHoldCopy = new float[SpectrumBandCount];
        float[] freqsCopy = new float[SpectrumBandCount];
        Array.Copy(_spectrumDb, specCopy, SpectrumBandCount);
        Array.Copy(_spectrumPeakHoldDb, peakHoldCopy, SpectrumBandCount);
        Array.Copy(_spectrumFreqs, freqsCopy, SpectrumBandCount);

        return new InsightProSnapshot(
            SampleRate: _sampleRate,
            Channels: _channels,
            ElapsedSeconds: _elapsedSeconds,
            TargetPreset: _targetPreset,
            TargetLufs: targetLufs,
            MaxTruePeakDbTp: maxTpDbTp,
            MomentaryLufs: _momentaryLufs,
            ShortTermLufs: _shortTermLufs,
            IntegratedLufs: _integratedLufs,
            LraLu: _lraLu,
            TruePeakDbTp: _truePeakDbTp,
            LeftTruePeakDbTp: _leftTruePeakDbTp,
            RightTruePeakDbTp: _rightTruePeakDbTp,
            MomentaryExceedsTarget: _momentaryLufs > targetLufs + 0.5f,
            ShortTermExceedsTarget: _shortTermLufs > targetLufs + 0.5f,
            IntegratedExceedsTarget: _integratedLufs > targetLufs + 0.5f,
            TruePeakExceedsTarget: _truePeakDbTp > maxTpDbTp,
            LeftPeakDb: _leftPeakDb,
            RightPeakDb: _rightPeakDb,
            LeftRmsDb: _leftRmsDb,
            RightRmsDb: _rightRmsDb,
            LeftPeakHoldDb: _leftPeakHoldDb,
            RightPeakHoldDb: _rightPeakHoldDb,
            LeftClipped: _leftClipped,
            RightClipped: _rightClipped,
            StereoCorrelation: _stereoCorrelation,
            PhaseDifferenceDeg: _phaseDifferenceDeg,
            StereoBalance: _stereoBalance,
            VectorPointsL: _vectorL.Length == 0 ? Array.Empty<float>() : (float[])_vectorL.Clone(),
            VectorPointsR: _vectorR.Length == 0 ? Array.Empty<float>() : (float[])_vectorR.Clone(),
            SpectrumDb: specCopy,
            SpectrumPeakHoldDb: peakHoldCopy,
            SpectrumFrequenciesHz: freqsCopy,
            HistorySamples: history,
            SpectrogramFrames: spectrogram);
    }

    private void UpdateSampleRateCoefficients(int sampleRate)
    {
        _sampleRate = Math.Max(8000, sampleRate);
        _hopFrames = Math.Max(160, (int)Math.Round(0.100 * _sampleRate));
        double fs = _sampleRate;

        // ITU-R BS.1770-4 Stage 1: High-shelf pre-filter (+4 dB)
        const double f0_1 = 1681.974450955533;
        const double g_1 = 3.999843853973347;
        const double q_1 = 0.7071752369554196;
        double k1 = Math.Tan(Math.PI * f0_1 / fs);
        double vh = Math.Pow(10.0, g_1 / 20.0);
        double vb = Math.Pow(vh, 0.4996667741545416);
        double a0_1 = 1.0 + k1 / q_1 + k1 * k1;
        _k1b0 = (vh + vb * k1 / q_1 + k1 * k1) / a0_1;
        _k1b1 = 2.0 * (k1 * k1 - vh) / a0_1;
        _k1b2 = (vh - vb * k1 / q_1 + k1 * k1) / a0_1;
        _k1a1 = 2.0 * (k1 * k1 - 1.0) / a0_1;
        _k1a2 = (1.0 - k1 / q_1 + k1 * k1) / a0_1;

        // ITU-R BS.1770-4 Stage 2: Revised Low-frequency B-weighting (RLB) high-pass
        const double f0_2 = 38.13547087602444;
        const double q_2 = 0.5003270373238773;
        double k2 = Math.Tan(Math.PI * f0_2 / fs);
        double a0_2 = 1.0 + k2 / q_2 + k2 * k2;
        _k2b0 = 1.0 / a0_2;
        _k2b1 = -2.0 / a0_2;
        _k2b2 = 1.0 / a0_2;
        _k2a1 = 2.0 * (k2 * k2 - 1.0) / a0_2;
        _k2a2 = (1.0 - k2 / q_2 + k2 * k2) / a0_2;
    }

    private static float EvaluateTruePeak(float[] delayLine, int nextWriteIdx)
    {
        float maxAbs = 0f;
        for (int p = 0; p < TruePeakPhaseCount; p++)
        {
            float[] coeffs = TruePeakPolyphase[p];
            float sum = 0f;
            for (int t = 0; t < TruePeakTapCount; t++)
            {
                int idx = (nextWriteIdx - 1 - t + TruePeakTapCount) % TruePeakTapCount;
                sum += delayLine[idx] * coeffs[t];
            }
            float abs = Math.Abs(sum);
            if (abs > maxAbs) maxAbs = abs;
        }
        return maxAbs;
    }

    private static float[][] BuildPolyphaseSyncTable()
    {
        // 4-phase Kaiser-windowed sinc FIR for 4x oversampled True Peak per ITU-R BS.1770-4 Annex 2
        float[][] table = new float[TruePeakPhaseCount][];
        int halfTaps = TruePeakTapCount / 2;
        for (int p = 0; p < TruePeakPhaseCount; p++)
        {
            table[p] = new float[TruePeakTapCount];
            double frac = (double)p / TruePeakPhaseCount;
            double sum = 0.0;
            for (int t = 0; t < TruePeakTapCount; t++)
            {
                double x = (t - (halfTaps - 1)) - frac;
                double sinc = Math.Abs(x) < 1e-9 ? 1.0 : Math.Sin(Math.PI * x) / (Math.PI * x);
                double win = 0.54 - 0.46 * Math.Cos(2.0 * Math.PI * (t + 0.5) / TruePeakTapCount); // Hamming/Hann smooth window
                double val = sinc * win;
                table[p][t] = (float)val;
                sum += val;
            }
            if (Math.Abs(sum) > 1e-9)
            {
                for (int t = 0; t < TruePeakTapCount; t++)
                {
                    table[p][t] = (float)(table[p][t] / sum);
                }
            }
        }
        return table;
    }

    private static void RunRadix2Fft(double[] real, double[] imag, double[] cosTable, double[] sinTable)
    {
        int n = real.Length;
        int j = 0;
        for (int i = 0; i < n; i++)
        {
            if (i < j)
            {
                (real[i], real[j]) = (real[j], real[i]);
                (imag[i], imag[j]) = (imag[j], imag[i]);
            }
            int m = n >> 1;
            while (m >= 1 && j >= m)
            {
                j -= m;
                m >>= 1;
            }
            j += m;
        }

        for (int size = 2; size <= n; size <<= 1)
        {
            int halfSize = size >> 1;
            int tableStep = n / size;
            for (int i = 0; i < n; i += size)
            {
                for (int k = 0, tw = 0; k < halfSize; k++, tw += tableStep)
                {
                    double cos = cosTable[tw];
                    double sin = sinTable[tw];
                    int even = i + k;
                    int odd = even + halfSize;
                    double tReal = real[odd] * cos - imag[odd] * sin;
                    double tImag = real[odd] * sin + imag[odd] * cos;
                    real[odd] = real[even] - tReal;
                    imag[odd] = imag[even] - tImag;
                    real[even] += tReal;
                    imag[even] += tImag;
                }
            }
        }
    }

    private static void UpdatePeakHold(ref float holdDb, ref double timerSec, float currentDb, double dtSec)
    {
        if (currentDb >= holdDb)
        {
            holdDb = currentDb;
            timerSec = 1.25;
        }
        else if (timerSec > 0)
        {
            timerSec -= dtSec;
        }
        else
        {
            holdDb = Math.Max(currentDb, holdDb - (float)(10.0 * dtSec));
        }
    }

    private static float SanitizeSample(float s)
    {
        if (float.IsNaN(s) || float.IsInfinity(s)) return 0f;
        return Math.Clamp(s, -8.0f, 8.0f);
    }

    private static float LinearToDb(float linear, float floorDb)
    {
        if (linear <= 1e-6f) return floorDb;
        return Math.Clamp(20.0f * (float)Math.Log10(linear), floorDb, 12.0f);
    }

    private static float PowerToLufs(double meanSquarePower)
    {
        if (meanSquarePower <= 1e-10) return FloorLufs;
        return Math.Clamp((float)(-0.691 + 10.0 * Math.Log10(meanSquarePower)), FloorLufs, 6.0f);
    }
}
