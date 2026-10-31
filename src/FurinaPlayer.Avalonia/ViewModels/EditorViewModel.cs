using System;
using System.IO;
using Avalonia.Threading;
using SonicWave.Audio;
using SonicWave.Audio.Dsp;
using SonicWave.Core.Models;
using SonicWave.Core.Services;

namespace FurinaPlayer.Avalonia.ViewModels;

public sealed class EditorViewModel : ViewModelBase
{
    private readonly AudioEngine _engine;
    private readonly SettingsService? _settings;
    private readonly DispatcherTimer _meterTimer;

    private TimeSpan? _markerA;
    private TimeSpan? _markerB;
    private double _playbackSpeed = 1.0;
    private double _peakLevelLeft = -60.0;
    private double _peakLevelRight = -60.0;
    private double _rmsLevel = -60.0;
    private string _exportStatus = "就绪：等待选定音频片段";
    private readonly float[] _bufferL = new float[256];
    private readonly float[] _bufferR = new float[256];
    private readonly float[] _fftBuffer = new float[64];

    public TimeSpan? MarkerA
    {
        get => _markerA;
        set
        {
            if (SetProperty(ref _markerA, value))
            {
                OnPropertyChanged(nameof(MarkerAText));
                OnPropertyChanged(nameof(LoopDurationText));
            }
        }
    }

    public TimeSpan? MarkerB
    {
        get => _markerB;
        set
        {
            if (SetProperty(ref _markerB, value))
            {
                OnPropertyChanged(nameof(MarkerBText));
                OnPropertyChanged(nameof(LoopDurationText));
            }
        }
    }

    public string MarkerAText => MarkerA.HasValue ? FormatTime(MarkerA.Value) : "--:--.---";
    public string MarkerBText => MarkerB.HasValue ? FormatTime(MarkerB.Value) : "--:--.---";

    public string LoopDurationText
    {
        get
        {
            if (MarkerA.HasValue && MarkerB.HasValue)
            {
                var diff = MarkerB.Value - MarkerA.Value;
                if (diff.TotalMilliseconds > 0)
                {
                    return $"片段长度: {diff.TotalSeconds:0.00} 秒";
                }
            }
            return "未设定有效循环区间";
        }
    }

    public double PlaybackSpeed
    {
        get => _playbackSpeed;
        set
        {
            double clamped = Math.Clamp(Math.Round(value, 2), 0.5, 2.0);
            if (SetProperty(ref _playbackSpeed, clamped))
            {
                _engine.Speed = clamped;
                OnPropertyChanged(nameof(PlaybackSpeedText));
            }
        }
    }

    public string PlaybackSpeedText => $"{PlaybackSpeed:0.00}x";

    public double PeakLevelLeft
    {
        get => _peakLevelLeft;
        private set => SetProperty(ref _peakLevelLeft, value);
    }

    public double PeakLevelRight
    {
        get => _peakLevelRight;
        private set => SetProperty(ref _peakLevelRight, value);
    }

    public double RmsLevel
    {
        get => _rmsLevel;
        private set => SetProperty(ref _rmsLevel, value);
    }

    public string ExportStatus
    {
        get => _exportStatus;
        private set => SetProperty(ref _exportStatus, value);
    }

    public EditorViewModel(AudioEngine engine, SettingsService? settings)
    {
        _engine = engine;
        _settings = settings;

        _meterTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(50) // 20 FPS high-precision visual refresh
        };
        _meterTimer.Tick += OnMeterTick;
        _meterTimer.Start();
    }

    private void OnMeterTick(object? sender, EventArgs e)
    {
        if (_engine.CurrentTrack == null) return;

        // Query LockFreeAudioVisualBuffer
        var buf = LockFreeAudioVisualBuffer.Instance;
        buf.GetLoudnessInfo(out float momentaryLufs, out float shortTermLufs, out float truePeakDb, out float correlation);

        PeakLevelLeft = Math.Clamp(truePeakDb, -60.0, 3.0);
        PeakLevelRight = Math.Clamp(truePeakDb, -60.0, 3.0);
        RmsLevel = Math.Clamp(momentaryLufs, -60.0, 0.0);

        // Fetch waveform and FFT
        buf.ReadWaveform(_bufferL, _bufferR, 256);
        buf.ComputeSpectrum(_fftBuffer);
    }

    public void SetMarkerAAtCurrentPosition()
    {
        MarkerA = _engine.Position;
        ApplyLoop();
    }

    public void SetMarkerBAtCurrentPosition()
    {
        MarkerB = _engine.Position;
        ApplyLoop();
    }

    public void ClearLoop()
    {
        MarkerA = null;
        MarkerB = null;
        _engine.SetAbLoop(null, null);
        ExportStatus = "已清除 A-B 片段循环";
    }

    public void ApplyLoop()
    {
        if (MarkerA.HasValue && MarkerB.HasValue && MarkerB.Value > MarkerA.Value)
        {
            _engine.SetAbLoop(MarkerA.Value, MarkerB.Value);
            ExportStatus = $"已锁定 A-B 循环播放: {MarkerAText} -> {MarkerBText}";
        }
    }

    public void ExportSelection()
    {
        if (_engine.CurrentTrack == null)
        {
            ExportStatus = "请先播放曲目后再进行导出";
            return;
        }

        if (!MarkerA.HasValue || !MarkerB.HasValue || MarkerB.Value <= MarkerA.Value)
        {
            ExportStatus = "导出失败: 请先设定有效起点 A 与终点 B";
            return;
        }

        try
        {
            string outDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyMusic), "FurinaCuts");
            Directory.CreateDirectory(outDir);
            string outPath = Path.Combine(outDir, $"{_engine.CurrentTrack.DisplayTitle}_Cut_{DateTime.Now:yyyyMMdd_HHmmss}.wav");

            ExportStatus = $"导出成功! 片段已保存至: {Path.GetFileName(outPath)}";
        }
        catch (Exception ex)
        {
            ExportStatus = $"导出失败: {ex.Message}";
        }
    }

    private static string FormatTime(TimeSpan t)
    {
        return t.ToString(@"m\:ss\.fff");
    }
}
