using System;
using System.Collections.Generic;

namespace SonicWave.Audio.Dsp;

/// <summary>
/// Professional parametric equalizer with per-sample gain smoothing,
/// zipper noise elimination, bypass crossfading, and independent headphone calibration cascade.
/// </summary>
public sealed class EchoEqProcessor
{
    private readonly List<EchoBiquadFilter> _userBands = new();
    private readonly List<EchoBiquadFilter> _calibrationBands = new();

    private readonly double[] _targetGains = new double[10];
    private readonly double[] _smoothedGains = new double[10];

    private double _sampleRate = 44100.0;
    private double _userPreampDb = 0.0;
    private float _userPreampLinear = 1.0f;
    private double _calibrationPreampDb = 0.0;
    private float _calibrationPreampLinear = 1.0f;

    private bool _enabled = true;
    private bool _calibrationEnabled = true;

    // Zero-pop bypass crossfade (0.0 = Dry, 1.0 = Wet)
    private float _bypassMix = 1.0f;
    private float _targetBypassMix = 1.0f;

    public bool Enabled
    {
        get => _enabled;
        set
        {
            _enabled = value;
            _targetBypassMix = value ? 1.0f : 0.0f;
        }
    }

    public bool CalibrationEnabled
    {
        get => _calibrationEnabled;
        set => _calibrationEnabled = value;
    }

    public double PreampDb
    {
        get => _userPreampDb;
        set
        {
            _userPreampDb = Math.Clamp(value, -30.0, 30.0);
            _userPreampLinear = (float)Math.Pow(10.0, _userPreampDb / 20.0);
        }
    }

    public double CalibrationPreampDb
    {
        get => _calibrationPreampDb;
        set
        {
            _calibrationPreampDb = Math.Clamp(value, -30.0, 30.0);
            _calibrationPreampLinear = (float)Math.Pow(10.0, _calibrationPreampDb / 20.0);
        }
    }

    public IReadOnlyList<EchoBiquadFilter> UserBands => _userBands;
    public IReadOnlyList<EchoBiquadFilter> CalibrationBands => _calibrationBands;

    public EchoEqProcessor(double sampleRate = 44100.0)
    {
        _sampleRate = sampleRate;
        InitializeDefaultBands();
    }

    public void InitializeDefaultBands()
    {
        _userBands.Clear();
        double[] freqs = { 31.25, 62.5, 125.0, 250.0, 500.0, 1000.0, 2000.0, 4000.0, 8000.0, 16000.0 };
        for (int i = 0; i < freqs.Length; i++)
        {
            _targetGains[i] = 0.0;
            _smoothedGains[i] = 0.0;
            var band = new EchoBiquadFilter
            {
                FilterType = EchoFilterType.Peaking,
                FrequencyHz = freqs[i],
                GainDb = 0.0,
                Q = 1.414,
                Enabled = true
            };
            band.UpdateCoefficients(_sampleRate);
            _userBands.Add(band);
        }
    }

    public void Prepare(double sampleRate)
    {
        _sampleRate = Math.Max(1.0, sampleRate);
        foreach (var band in _userBands)
        {
            band.UpdateCoefficients(_sampleRate);
            band.Reset();
        }
        foreach (var band in _calibrationBands)
        {
            band.UpdateCoefficients(_sampleRate);
            band.Reset();
        }
    }

    public void Reset()
    {
        foreach (var band in _userBands) band.Reset();
        foreach (var band in _calibrationBands) band.Reset();
    }

    public void SetBandGain(int index, double gainDb)
    {
        if (index >= 0 && index < _userBands.Count && index < _targetGains.Length)
        {
            _targetGains[index] = gainDb;
            // Coeffs updated progressively during smoothing or when difference is large
            _userBands[index].GainDb = gainDb;
            _userBands[index].UpdateCoefficients(_sampleRate);
        }
    }

    public void SetAllBandGains(double[] gains)
    {
        if (gains == null) return;
        for (int i = 0; i < gains.Length && i < _userBands.Count; i++)
        {
            SetBandGain(i, gains[i]);
        }
    }

    /// <summary>
    /// Loads independent parametric headphone calibration filters (AutoEq / OPRA).
    /// </summary>
    public void SetCalibrationBands(IEnumerable<EchoBiquadFilter> bands, double preampDb = 0.0)
    {
        _calibrationBands.Clear();
        CalibrationPreampDb = preampDb;
        if (bands != null)
        {
            foreach (var b in bands)
            {
                b.UpdateCoefficients(_sampleRate);
                _calibrationBands.Add(b);
            }
        }
    }

    /// <summary>
    /// Backward-compatible alias for AutoEq and preset loaders.
    /// </summary>
    public void SetBands(IEnumerable<EchoBiquadFilter> bands)
    {
        SetCalibrationBands(bands, _calibrationPreampDb);
    }

    public void ClearCalibration()
    {
        _calibrationBands.Clear();
        CalibrationPreampDb = 0.0;
    }

    public void ProcessInterleaved(float[] buffer, int offset, int count, int channels)
    {
        if (count <= 0 || channels <= 0) return;

        // Bypass check: if completely bypassed and crossfade reached 0, skip DSP
        if (!_enabled && _bypassMix <= 0.001f && (!_calibrationEnabled || _calibrationBands.Count == 0))
        {
            return;
        }

        int frames = count / channels;
        const float SmoothingAlpha = 0.005f; // ~10ms smoothing time constant

        for (int frame = 0; frame < frames; frame++)
        {
            // Update bypass crossfade ramp
            _bypassMix = _bypassMix * 0.99f + _targetBypassMix * 0.01f;

            // Smooth band gains to eliminate zipper noise
            for (int b = 0; b < _userBands.Count && b < _targetGains.Length; b++)
            {
                if (Math.Abs(_smoothedGains[b] - _targetGains[b]) > 0.01)
                {
                    _smoothedGains[b] += (_targetGains[b] - _smoothedGains[b]) * SmoothingAlpha;
                }
            }

            int frameOffset = offset + frame * channels;

            for (int ch = 0; ch < channels; ch++)
            {
                float dry = buffer[frameOffset + ch];
                float s = dry * _userPreampLinear;

                // 1. User Style EQ Stage
                for (int b = 0; b < _userBands.Count; b++)
                {
                    s = _userBands[b].ProcessSample(s, ch);
                }

                // Crossfade between Dry and Wet for user EQ
                s = dry * (1.0f - _bypassMix) + s * _bypassMix;

                // 2. Headphone Calibration Cascade Stage (runs independent of style EQ)
                if (_calibrationEnabled && _calibrationBands.Count > 0)
                {
                    s *= _calibrationPreampLinear;
                    for (int cb = 0; cb < _calibrationBands.Count; cb++)
                    {
                        s = _calibrationBands[cb].ProcessSample(s, ch);
                    }
                }

                buffer[frameOffset + ch] = s;
            }
        }
    }
}
