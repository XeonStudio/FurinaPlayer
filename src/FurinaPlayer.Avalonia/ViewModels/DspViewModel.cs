using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using SonicWave.Audio;
using SonicWave.Audio.Dsp;
using SonicWave.Core.Services;

namespace FurinaPlayer.Avalonia.ViewModels;

public sealed class EqBandViewModel : ViewModelBase
{
    private readonly int _index;
    private readonly EchoEqProcessor _processor;
    private double _gainDb;

    public string FrequencyLabel { get; }

    public double GainDb
    {
        get => _gainDb;
        set
        {
            double clamped = Math.Clamp(Math.Round(value, 1), -12.0, 12.0);
            if (SetProperty(ref _gainDb, clamped))
            {
                _processor.SetBandGain(_index, clamped);
                OnPropertyChanged(nameof(GainLabel));
            }
        }
    }

    public string GainLabel => GainDb > 0 ? $"+{GainDb:0.0} dB" : $"{GainDb:0.0} dB";

    public EqBandViewModel(int index, string label, double initialGain, EchoEqProcessor processor)
    {
        _index = index;
        FrequencyLabel = label;
        _gainDb = initialGain;
        _processor = processor;
    }

    public void Reset()
    {
        GainDb = 0.0;
    }
}

public sealed class DspViewModel : ViewModelBase
{
    private readonly AudioEngine _engine;
    private readonly SettingsService? _settings;

    // EQ
    private bool _eqEnabled = true;
    private double _eqPreampDb;
    public ObservableCollection<EqBandViewModel> EqBands { get; } = new();

    // Headphone Calibration (OPRA)
    private bool _calibrationEnabled;
    private double _calibrationPreampDb;
    private OpraHeadphonePreset? _selectedHeadphone;
    public ObservableCollection<OpraHeadphonePreset> HeadphonePresets { get; } = new();
    private string _calibrationStatus = "未加载耳机校准";

    // Resampler
    private bool _resamplerEnabled;
    private int _resamplerSampleRateIndex = 2; // 192000
    private int _resamplerFilterModeIndex = 0; // PolySincHalfBand

    // Compressor
    private bool _compressorEnabled;
    private double _compressorThresholdDb = -12.0;
    private double _compressorRatio = 4.0;
    private double _compressorAttackMs = 20.0;
    private double _compressorReleaseMs = 120.0;
    private double _compressorMakeupDb = 2.0;

    // Spatial & Crossfeed
    private bool _crossfeedEnabled;
    private double _crossfeedAmount = 0.45;
    private bool _stereoFieldEnabled;
    private double _stereoWidth = 1.0;

    // Headroom & Limiter
    private double _headroomDb = -1.0;
    private bool _limiterEnabled = true;

    // Properties
    public bool EqEnabled
    {
        get => _eqEnabled;
        set
        {
            if (SetProperty(ref _eqEnabled, value))
            {
                var eq = _engine.DspChain?.Eq;
                if (eq != null) eq.Enabled = value;
            }
        }
    }

    public double EqPreampDb
    {
        get => _eqPreampDb;
        set
        {
            if (SetProperty(ref _eqPreampDb, value))
            {
                var eq = _engine.DspChain?.Eq;
                if (eq != null) eq.PreampDb = value;
                OnPropertyChanged(nameof(EqPreampLabel));
            }
        }
    }

    public string EqPreampLabel => EqPreampDb > 0 ? $"+{EqPreampDb:0.0} dB" : $"{EqPreampDb:0.0} dB";

    public bool CalibrationEnabled
    {
        get => _calibrationEnabled;
        set
        {
            if (SetProperty(ref _calibrationEnabled, value))
            {
                var eq = _engine.DspChain?.Eq;
                if (eq != null) eq.CalibrationEnabled = value;
                CalibrationStatus = value ? $"已启用: {SelectedHeadphone?.Model ?? "校准预设"}" : "校准已旁路";
            }
        }
    }

    public double CalibrationPreampDb
    {
        get => _calibrationPreampDb;
        set
        {
            if (SetProperty(ref _calibrationPreampDb, value))
            {
                var eq = _engine.DspChain?.Eq;
                if (eq != null) eq.CalibrationPreampDb = value;
                OnPropertyChanged(nameof(CalibrationPreampLabel));
            }
        }
    }

    public string CalibrationPreampLabel => $"{CalibrationPreampDb:0.0} dB";

    public OpraHeadphonePreset? SelectedHeadphone
    {
        get => _selectedHeadphone;
        set
        {
            if (SetProperty(ref _selectedHeadphone, value) && value != null)
            {
                ApplyHeadphoneCalibration(value);
            }
        }
    }

    public string CalibrationStatus
    {
        get => _calibrationStatus;
        private set => SetProperty(ref _calibrationStatus, value);
    }

    public bool ResamplerEnabled
    {
        get => _resamplerEnabled;
        set
        {
            if (SetProperty(ref _resamplerEnabled, value))
            {
                var res = _engine.DspChain?.Resampler;
                if (res != null) res.Enabled = value;
            }
        }
    }

    public int ResamplerSampleRateIndex
    {
        get => _resamplerSampleRateIndex;
        set
        {
            if (SetProperty(ref _resamplerSampleRateIndex, value))
            {
                int sr = value switch
                {
                    0 => 48000,
                    1 => 96000,
                    2 => 192000,
                    3 => 384000,
                    _ => 192000
                };
                var res = _engine.DspChain?.Resampler;
                if (res != null) res.TargetSampleRate = sr;
            }
        }
    }

    public int ResamplerFilterModeIndex
    {
        get => _resamplerFilterModeIndex;
        set
        {
            if (SetProperty(ref _resamplerFilterModeIndex, value))
            {
                var res = _engine.DspChain?.Resampler;
                if (res != null) res.FilterMode = value == 0 ? ResamplerFilterMode.PolySincHalfBand : ResamplerFilterMode.HermiteCubic;
            }
        }
    }

    public bool CompressorEnabled
    {
        get => _compressorEnabled;
        set
        {
            if (SetProperty(ref _compressorEnabled, value))
            {
                SyncCompressor();
            }
        }
    }

    public double CompressorThresholdDb
    {
        get => _compressorThresholdDb;
        set
        {
            if (SetProperty(ref _compressorThresholdDb, value))
            {
                SyncCompressor();
                OnPropertyChanged(nameof(CompressorThresholdLabel));
            }
        }
    }

    public string CompressorThresholdLabel => $"{CompressorThresholdDb:0.0} dB";

    public double CompressorRatio
    {
        get => _compressorRatio;
        set
        {
            if (SetProperty(ref _compressorRatio, value))
            {
                SyncCompressor();
                OnPropertyChanged(nameof(CompressorRatioLabel));
            }
        }
    }

    public string CompressorRatioLabel => $"{CompressorRatio:0.0}:1";

    public double CompressorAttackMs
    {
        get => _compressorAttackMs;
        set
        {
            if (SetProperty(ref _compressorAttackMs, value))
            {
                SyncCompressor();
            }
        }
    }

    public double CompressorReleaseMs
    {
        get => _compressorReleaseMs;
        set
        {
            if (SetProperty(ref _compressorReleaseMs, value))
            {
                SyncCompressor();
            }
        }
    }

    public bool CrossfeedEnabled
    {
        get => _crossfeedEnabled;
        set
        {
            if (SetProperty(ref _crossfeedEnabled, value))
            {
                _engine.SetEchoCrossfeed(value, (float)_crossfeedAmount, 700f);
            }
        }
    }

    public double CrossfeedAmount
    {
        get => _crossfeedAmount;
        set
        {
            if (SetProperty(ref _crossfeedAmount, value))
            {
                _engine.SetEchoCrossfeed(_crossfeedEnabled, (float)value, 700f);
                OnPropertyChanged(nameof(CrossfeedAmountLabel));
            }
        }
    }

    public string CrossfeedAmountLabel => $"{(int)(CrossfeedAmount * 100)}%";

    public bool StereoFieldEnabled
    {
        get => _stereoFieldEnabled;
        set
        {
            if (SetProperty(ref _stereoFieldEnabled, value))
            {
                _engine.SetEchoStereoField(value, (float)_stereoWidth, 0f, 0f);
            }
        }
    }

    public double StereoWidth
    {
        get => _stereoWidth;
        set
        {
            if (SetProperty(ref _stereoWidth, value))
            {
                _engine.SetEchoStereoField(_stereoFieldEnabled, (float)value, 0f, 0f);
                OnPropertyChanged(nameof(StereoWidthLabel));
            }
        }
    }

    public string StereoWidthLabel => $"{StereoWidth:0.0}x";

    public double HeadroomDb
    {
        get => _headroomDb;
        set
        {
            if (SetProperty(ref _headroomDb, value))
            {
                _engine.SetEchoHeadroom(value);
                OnPropertyChanged(nameof(HeadroomLabel));
            }
        }
    }

    public string HeadroomLabel => $"{HeadroomDb:0.0} dB";

    public bool LimiterEnabled
    {
        get => _limiterEnabled;
        set
        {
            if (SetProperty(ref _limiterEnabled, value))
            {
                if (_engine.DspChain != null)
                {
                    _engine.DspChain.SafetyLimiterEnabled = value;
                }
            }
        }
    }

    public DspViewModel(AudioEngine engine, SettingsService? settings)
    {
        _engine = engine;
        _settings = settings;

        // Initialize EQ Bands
        string[] labels = { "31Hz", "62Hz", "125Hz", "250Hz", "500Hz", "1kHz", "2kHz", "4kHz", "8kHz", "16kHz" };
        var eq = _engine.DspChain?.Eq;
        for (int i = 0; i < labels.Length; i++)
        {
            double gain = (eq != null && i < eq.UserBands.Count) ? eq.UserBands[i].GainDb : 0.0;
            EqBands.Add(new EqBandViewModel(i, labels[i], gain, eq ?? new EchoEqProcessor()));
        }

        // Initialize OPRA presets
        var presets = OpraDatabaseService.GetAllPresets();
        foreach (var p in presets)
        {
            HeadphonePresets.Add(p);
        }
    }

    public void ResetEq()
    {
        foreach (var band in EqBands)
        {
            band.Reset();
        }
        EqPreampDb = 0.0;
    }

    public void ApplyBassBoost()
    {
        ResetEq();
        if (EqBands.Count >= 3)
        {
            EqBands[0].GainDb = 5.5;
            EqBands[1].GainDb = 4.0;
            EqBands[2].GainDb = 2.0;
        }
    }

    public void ApplyVocalBoost()
    {
        ResetEq();
        if (EqBands.Count >= 7)
        {
            EqBands[4].GainDb = 2.5; // 500Hz
            EqBands[5].GainDb = 4.0; // 1kHz
            EqBands[6].GainDb = 3.0; // 2kHz
        }
    }

    public void ApplyHeadphoneCalibration(OpraHeadphonePreset preset)
    {
        var eq = _engine.DspChain?.Eq;
        if (eq == null) return;

        OpraDatabaseService.ApplyPreset(eq, preset.Id);
        CalibrationEnabled = true;
        CalibrationPreampDb = preset.PreampDb;
        CalibrationStatus = $"已加载 {preset.Brand} {preset.Model} ({preset.TargetCurve} 靶线, {preset.Filters.Count} 段)";
    }

    private void SyncCompressor()
    {
        _engine.SetEchoCompressor(_compressorEnabled, (float)_compressorThresholdDb, (float)_compressorRatio,
            (float)_compressorAttackMs, (float)_compressorReleaseMs, 3f, (float)_compressorMakeupDb, 1f);
    }
}
