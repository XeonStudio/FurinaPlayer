using System;
using System.Collections.Generic;
using Microsoft.UI;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Media;
using SonicWave.Audio.Dsp;
using Windows.UI;

namespace FurinaPlayer.UI.Controls;

/// <summary>
/// Reusable Liquid-Glass Parametric EQ + 10-Band Graphic EQ + Neutron Pro Mix Assistant workbench.
/// Designed for both <c>DspPage</c> (live playback EQ) and <c>EditorPage</c> (track preview &amp; offline DSP apply).
/// </summary>
public sealed class ParametricEqWorkbenchControl : UserControl
{
    private const int MaxUndoDepth = 40;
    private static readonly double[] StandardSnapFrequencies =
    {
        20, 25, 31.25, 40, 50, 62.5, 80, 100, 125, 160, 200, 250, 315, 400, 500,
        630, 800, 1000, 1250, 1600, 2000, 2500, 3150, 4000, 5000, 6300, 8000, 10000, 12500, 16000, 20000
    };

    private static readonly Color[] BandColors =
    {
        Color.FromArgb(255, 14, 165, 233),  // 01 Sky
        Color.FromArgb(255, 22, 163, 74),   // 02 Green
        Color.FromArgb(255, 147, 51, 234),  // 03 Purple
        Color.FromArgb(255, 225, 29, 72),   // 04 Rose
        Color.FromArgb(255, 234, 88, 12),   // 05 Orange
        Color.FromArgb(255, 202, 138, 4),   // 06 Amber
        Color.FromArgb(255, 13, 148, 136),  // 07 Teal
        Color.FromArgb(255, 37, 99, 235),   // 08 Blue
        Color.FromArgb(255, 192, 38, 211),  // 09 Fuchsia
        Color.FromArgb(255, 220, 38, 38)    // 10 Red
    };

    private readonly List<EchoBiquadFilter> _bands = new();
    private readonly Stack<(List<EchoBiquadFilter> Bands, double PreampDb)> _undoStack = new();
    private readonly Stack<(List<EchoBiquadFilter> Bands, double PreampDb)> _redoStack = new();
    private readonly MixAssistantEngine _mixAssistant = new();

    private readonly List<(string Name, double[] Gains)> _builtinPresets = new()
    {
        ("平坦 (Flat)", new double[10]),
        ("耳机校正 - Moondrop / Parameter (哈曼目标)", new[] { 5.5, 4.8, 1.5, -0.5, 0.0, 0.5, 2.5, 4.0, 1.0, -2.0 }),
        ("耳机校正 - Sennheiser HD600 (弥散场优化)", new[] { 6.5, 4.0, 1.2, 0.0, 0.0, 0.0, 1.0, 1.5, -1.0, 2.0 }),
        ("耳机校正 - Sony WH-1000XM5 (下潜饱满)", new[] { 3.0, 2.0, -1.5, -2.5, 0.0, 1.0, 2.0, 3.0, 2.5, 1.5 }),
        ("哈曼目标曲线 Harman Target 2019", new[] { 6.0, 5.0, 2.5, 0.0, 0.0, 0.5, 3.0, 4.5, 1.0, -1.5 }),
        ("低频重低音增强 (Sub-Bass Boost +6dB)", new[] { 6.0, 5.0, 3.5, 1.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0 }),
        ("高频通透空气感 (High Air Brilliance)", new[] { 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.5, 2.0, 3.5, 5.0 }),
        ("暖声人声突出 (Warm Vocal Presence)", new[] { 0.0, 0.5, 1.5, 0.5, 0.0, 2.5, 3.0, 1.5, 0.0, 0.0 }),
        ("开阔立体声场 (Open Soundstage)", new[] { 2.0, 1.0, 0.0, -1.5, 0.0, 0.5, 2.0, 3.0, 4.0, 3.0 })
    };

    private bool _isDark;
    private bool _isUpdatingUi;
    private bool _isGraphicMode;
    private bool _isEqEnabled = true;
    private double _preampDb;
    private bool _snapToStandardFreq;
    private int _selectedScopeIndex;
    private bool _isAbOriginal;
    private bool _isLoudnessMatched;
    private bool _showApplyOfflineDspButton;
    private bool _showAnalyzeTrackButton = true;
    private string _applyOfflineDspButtonText = "✨ 永久写入当前音轨 (Apply DSP)";

    // Mix Assistant state
    private MixAssistantProfile? _currentProfile;
    private MixAssistantSolution? _currentSolution;
    private bool _isLearning;
    private string _selectedTargetId = "balanced_master";
    private double _matchAmount01 = 0.75;
    private double _bassSculptDb;
    private double _trebleSculptDb;
    private bool _demaskMidMud = true;

    // Root & Themeable Surfaces
    private Grid _rootGrid = null!;
    private Border _modePillBorder = null!;
    private Button _btnModeParametric = null!;
    private Button _btnModeGraphic = null!;
    private TextBlock _titleTb = null!;
    private ToggleSwitch _powerSwitch = null!;
    private ComboBox _presetCombo = null!;
    private Button _btnSpectrum = null!;
    private Button _btnUndo = null!;
    private Button _btnRedo = null!;
    private Button _btnResetTop = null!;
    private Button _btnZoomIn = null!;
    private TextBlock _tbScale = null!;
    private Button _btnZoomOut = null!;
    private Button _btnAutoScale = null!;

    // Curve
    private Border _curveCard = null!;
    private ParametricEqCurveControl _curveControl = null!;

    // Mode Sections
    private StackPanel _parametricSection = null!;
    private StackPanel _graphicAssistantSection = null!;

    // Parametric Mode Controls
    private Border _carouselCard = null!;
    private TextBlock _headroomStatusTb = null!;
    private TextBlock _beforeStateTb = null!;
    private TextBlock _afterStateTb = null!;
    private Button _btnAbTop = null!;
    private Button _btnLoudnessMatch = null!;
    private StackPanel _bandPillsStack = null!;

    private Border _inspectorCard = null!;
    private Border _bandIndexBadgeBorder = null!;
    private TextBlock _bandIndexBadgeTb = null!;
    private ComboBox _filterTypeCombo = null!;
    private CheckBox _bandEnableCb = null!;
    private Button _btnResetBand = null!;
    private Button _btnDeleteBand = null!;
    private Slider _freqSlider = null!;
    private TextBlock _freqValueTb = null!;
    private Slider _gainSlider = null!;
    private TextBlock _gainValueTb = null!;
    private Slider _qSlider = null!;
    private TextBlock _qValueTb = null!;
    private readonly List<Button> _qQuickButtons = new();
    private CheckBox _cbSnapFreq = null!;
    private Border _scopeBorder = null!;
    private readonly List<Button> _scopeButtons = new();
    private Slider _preampSlider = null!;
    private TextBlock _preampValueTb = null!;
    private TextBlock _compositeHintTb = null!;

    // Graphic + Mix Assistant Mode Controls
    private Border _graphicFadersCard = null!;
    private readonly Slider[] _graphicSliders = new Slider[10];
    private readonly TextBlock[] _graphicGainLabels = new TextBlock[10];
    private readonly TextBlock[] _graphicFreqLabels = new TextBlock[10];

    private Border _mixAssistantCard = null!;
    private Button _btnLearn = null!;
    private TextBlock _btnLearnTextTb = null!;
    private Button _btnAnalyzeTrack = null!;
    private ComboBox _targetCombo = null!;
    private ToggleSwitch _demaskSwitch = null!;
    private Slider _amountSlider = null!;
    private TextBlock _amountValueTb = null!;
    private Slider _bassSculptSlider = null!;
    private TextBlock _bassSculptValueTb = null!;
    private Slider _trebleSculptSlider = null!;
    private TextBlock _trebleSculptValueTb = null!;
    private ProgressBar _learnProgressBar = null!;
    private TextBlock _learnStatusTb = null!;
    private StackPanel _issuesStack = null!;
    private Button _btnApplyAssistant = null!;
    private Button _btnAssistantAb = null!;
    private Button _btnAssistantReset = null!;

    // Bottom Toolbar
    private Border _bottomToolbarCard = null!;
    private Button _btnApplyOfflineDsp = null!;
    private TextBlock _btnApplyOfflineDspTb = null!;
    private TextBox _presetNameTb = null!;
    private Button _btnSavePreset = null!;
    private Button _btnResetBottom = null!;
    private Button _btnImportAutoEq = null!;

    private readonly List<TextBlock> _primaryTextElements = new();
    private readonly List<TextBlock> _secondaryTextElements = new();
    private readonly List<TextBlock> _subHeaderTextElements = new();
    private readonly List<TextBlock> _mutedTextElements = new();
    private readonly List<Button> _glassToolButtons = new();

    public event Action<IReadOnlyList<EchoBiquadFilter>>? BandsChanged;
    public event Action<int, EchoBiquadFilter>? BandChanged;
    public event Action<double>? PreampChanged;
    public event Action<bool>? EnabledChanged;
    public event Action<bool>? MixAssistantLearnRequested;
    public event Action? MixAssistantAnalyzeAudioRequested;
    public event Action? ImportAutoEqRequested;
    public event Action? ApplyOfflineDspRequested;
    public event Action<bool>? AbCompareToggled;
    public event Action<bool>? LoudnessMatchToggled;
    public event Action<int>? ScopeChanged;
    public event Action<string, IReadOnlyList<EchoBiquadFilter>, double>? SaveUserPresetRequested;

    public bool ShowApplyOfflineDspButton
    {
        get => _showApplyOfflineDspButton;
        set
        {
            _showApplyOfflineDspButton = value;
            if (_btnApplyOfflineDsp != null)
            {
                _btnApplyOfflineDsp.Visibility = value ? Visibility.Visible : Visibility.Collapsed;
            }
        }
    }

    public string ApplyOfflineDspButtonText
    {
        get => _applyOfflineDspButtonText;
        set
        {
            _applyOfflineDspButtonText = string.IsNullOrWhiteSpace(value)
                ? "✨ 永久写入当前音轨 (Apply DSP)"
                : value;
            if (_btnApplyOfflineDspTb != null)
            {
                _btnApplyOfflineDspTb.Text = _applyOfflineDspButtonText;
            }
        }
    }

    public bool ShowAnalyzeTrackButton
    {
        get => _showAnalyzeTrackButton;
        set
        {
            _showAnalyzeTrackButton = value;
            if (_btnAnalyzeTrack != null)
            {
                _btnAnalyzeTrack.Visibility = value ? Visibility.Visible : Visibility.Collapsed;
            }
        }
    }

    public bool IsEqEnabled
    {
        get => _isEqEnabled;
        set
        {
            _isEqEnabled = value;
            _isUpdatingUi = true;
            try
            {
                if (_powerSwitch != null) _powerSwitch.IsOn = value;
            }
            finally
            {
                _isUpdatingUi = false;
            }
        }
    }

    public double PreampDb
    {
        get => _preampDb;
        set
        {
            _preampDb = Math.Clamp(value, -18.0, 18.0);
            _isUpdatingUi = true;
            try
            {
                if (_preampSlider != null) _preampSlider.Value = Math.Clamp(_preampDb, -12.0, 12.0);
                if (_preampValueTb != null) _preampValueTb.Text = $"{_preampDb:+0.0;-0.0;0.0} dB";
                if (_curveControl != null) _curveControl.PreampDb = _preampDb;
                UpdateHeadroomStatusDisplay();
            }
            finally
            {
                _isUpdatingUi = false;
            }
        }
    }

    public IReadOnlyList<EchoBiquadFilter> Bands => _bands;

    public int SelectedBandIndex
    {
        get => _curveControl?.SelectedBandIndex ?? 0;
        set => SelectBand(value);
    }

    public bool IsGraphicAssistantMode
    {
        get => _isGraphicMode;
        set => SetWorkbenchMode(value);
    }

    public MixAssistantEngine MixAssistant => _mixAssistant;
    public MixAssistantProfile? CurrentMixProfile => _currentProfile;
    public MixAssistantSolution? CurrentMixSolution => _currentSolution;

    public ParametricEqWorkbenchControl()
    {
        _isDark = LiquidGlassEngine.IsElementDarkTheme(this);
        InitializeDefaultBands();
        BuildUi();
        UpdateTheme(_isDark);
        SyncAllViewsFromBands(0);
    }

    public void SetBands(IEnumerable<EchoBiquadFilter> bands, double preampDb = 0.0, bool enabled = true, int selectedIndex = 0)
    {
        _bands.Clear();
        if (bands != null)
        {
            foreach (var b in bands)
            {
                if (b == null) continue;
                var clone = new EchoBiquadFilter
                {
                    FilterType = b.FilterType,
                    FrequencyHz = b.FrequencyHz,
                    GainDb = b.GainDb,
                    Q = b.Q,
                    Enabled = b.Enabled
                };
                clone.UpdateCoefficients(48000.0);
                _bands.Add(clone);
            }
        }
        if (_bands.Count == 0)
        {
            InitializeDefaultBands();
        }

        _preampDb = Math.Clamp(preampDb, -18.0, 18.0);
        _isEqEnabled = enabled;

        _isUpdatingUi = true;
        try
        {
            if (_powerSwitch != null) _powerSwitch.IsOn = enabled;
            if (_preampSlider != null) _preampSlider.Value = Math.Clamp(_preampDb, -12.0, 12.0);
            if (_preampValueTb != null) _preampValueTb.Text = $"{_preampDb:+0.0;-0.0;0.0} dB";
            if (_curveControl != null) _curveControl.PreampDb = _preampDb;
        }
        finally
        {
            _isUpdatingUi = false;
        }

        SyncAllViewsFromBands(selectedIndex);
    }

    public double[] GetGraphic10BandGains()
    {
        double[] gains = new double[10];
        for (int i = 0; i < 10 && i < _bands.Count; i++)
        {
            gains[i] = _bands[i].Enabled ? Math.Clamp(_bands[i].GainDb, -12.0, 12.0) : 0.0;
        }
        return gains;
    }

    public void UpdateSpectrum(float[]? spectrumData)
    {
        _curveControl?.SetSpectrumData(spectrumData);
        if (_isLearning && spectrumData != null && spectrumData.Length > 0)
        {
            _mixAssistant.AccumulateSpectrum64(spectrumData);
            SetMixAssistantProgress(
                isLearning: true,
                progress01: _mixAssistant.LearningProgress01,
                statusText: $"正在学习音频频谱平衡... ({_mixAssistant.LearnedFrames} 帧)");
        }
    }

    public void SetMixAssistantProgress(bool isLearning, double progress01, string? statusText = null)
    {
        _isLearning = isLearning;
        double clamped = Math.Clamp(progress01, 0.0, 1.0);

        if (_learnProgressBar != null)
        {
            _learnProgressBar.Value = clamped * 100.0;
        }
        if (_learnStatusTb != null)
        {
            _learnStatusTb.Text = !string.IsNullOrWhiteSpace(statusText)
                ? statusText
                : (isLearning ? $"正在学习频谱 ({clamped * 100:0}%)..." : "点击“学习监听”或“分析音轨”以诊断频响并生成平衡曲线");
        }
        if (_btnLearnTextTb != null)
        {
            _btnLearnTextTb.Text = isLearning ? "⏹ 完成学习" : "🎧 学习监听 (Learn)";
        }
        RefreshMixAssistantButtonVisuals();
    }

    public void SetMixAssistantProfile(MixAssistantProfile? profile, bool autoApply = true)
    {
        _currentProfile = profile;
        if (profile == null)
        {
            SetMixAssistantProgress(false, 0.0, "尚未学习音频频谱");
            SetMixAssistantIssues(null);
            return;
        }

        _currentSolution = _mixAssistant.SolveEqCurve(
            profile,
            _selectedTargetId,
            _matchAmount01,
            _bassSculptDb,
            _trebleSculptDb,
            _demaskMidMud);

        SetMixAssistantIssues(_currentSolution.DetectedIssues);
        SetMixAssistantProgress(
            isLearning: false,
            progress01: 1.0,
            statusText: $"已完成分析 · {_currentSolution.TargetName} · 推荐前级 {_currentSolution.RecommendedPreampDb:+0.0;-0.0;0.0} dB");

        if (autoApply)
        {
            ApplyMixAssistantSolution(_currentSolution);
        }
    }

    public void ApplyMixAssistantSolution(MixAssistantSolution solution)
    {
        if (solution == null) return;
        _currentSolution = solution;
        SetMixAssistantIssues(solution.DetectedIssues);

        PushUndoSnapshot();

        // Ensure we have at least 10 bands, and map the 10 solved bands cleanly while keeping any extra user bands untouched
        while (_bands.Count < 10)
        {
            _bands.Add(new EchoBiquadFilter
            {
                FilterType = EchoFilterType.Peaking,
                FrequencyHz = MixAssistantEngine.GraphicFrequencies[_bands.Count],
                GainDb = 0.0,
                Q = 1.414,
                Enabled = true
            });
        }

        for (int i = 0; i < 10; i++)
        {
            EchoBiquadFilter targetBand = _bands[i];
            if (solution.Filters != null && i < solution.Filters.Length)
            {
                EchoBiquadFilter solved = solution.Filters[i];
                targetBand.FilterType = solved.FilterType;
                targetBand.FrequencyHz = solved.FrequencyHz;
                targetBand.GainDb = solved.GainDb;
                targetBand.Q = solved.Q;
                targetBand.Enabled = solved.Enabled;
            }
            else if (solution.BandOffsetsDb != null && i < solution.BandOffsetsDb.Length)
            {
                targetBand.FrequencyHz = MixAssistantEngine.GraphicFrequencies[i];
                targetBand.GainDb = solution.BandOffsetsDb[i];
                targetBand.Enabled = true;
            }
            targetBand.UpdateCoefficients(48000.0);
        }

        PreampDb = solution.RecommendedPreampDb;
        SyncAllViewsFromBands(_curveControl?.SelectedBandIndex ?? 0);

        NotifyAllBandsChanged();
        PreampChanged?.Invoke(_preampDb);
    }

    public void SetMixAssistantIssues(IEnumerable<string>? issues)
    {
        if (_issuesStack == null) return;
        _issuesStack.Children.Clear();

        bool hasAny = false;
        if (issues != null)
        {
            foreach (string issue in issues)
            {
                if (string.IsNullOrWhiteSpace(issue)) continue;
                hasAny = true;
                bool isHealthy = issue.Contains("均衡", StringComparison.Ordinal);
                Color badgeBg = isHealthy
                    ? Color.FromArgb(_isDark ? (byte)65 : (byte)220, 22, 163, 74)
                    : Color.FromArgb(_isDark ? (byte)65 : (byte)220, 245, 158, 11);
                Color badgeFg = isHealthy
                    ? (_isDark ? Color.FromArgb(255, 134, 239, 172) : Color.FromArgb(255, 21, 128, 61))
                    : (_isDark ? Color.FromArgb(255, 253, 224, 71) : Color.FromArgb(255, 180, 83, 9));

                var tag = new Border
                {
                    Background = new SolidColorBrush(badgeBg),
                    BorderBrush = new SolidColorBrush(Color.FromArgb(140, badgeFg.R, badgeFg.G, badgeFg.B)),
                    BorderThickness = new Thickness(1),
                    CornerRadius = new CornerRadius(10),
                    Padding = new Thickness(8, 3, 8, 3),
                    Child = new TextBlock
                    {
                        Text = isHealthy ? $"✓ {issue}" : $"⚠ {issue}",
                        FontSize = 11,
                        FontWeight = FontWeights.SemiBold,
                        Foreground = new SolidColorBrush(badgeFg)
                    }
                };
                _issuesStack.Children.Add(tag);
            }
        }

        if (!hasAny)
        {
            _issuesStack.Children.Add(new TextBlock
            {
                Text = "等待频谱学习后显示声学诊断标签...",
                FontSize = 11,
                Foreground = GetMutedBrush(),
                VerticalAlignment = VerticalAlignment.Center
            });
        }
    }

    public void ResetToFlat()
    {
        PushUndoSnapshot();
        for (int i = 0; i < _bands.Count; i++)
        {
            var b = _bands[i];
            if (i < MixAssistantEngine.GraphicFrequencies.Length)
            {
                b.FrequencyHz = MixAssistantEngine.GraphicFrequencies[i];
                b.FilterType = i == 0
                    ? EchoFilterType.LowShelf
                    : (i == MixAssistantEngine.GraphicFrequencies.Length - 1 ? EchoFilterType.HighShelf : EchoFilterType.Peaking);
            }
            b.GainDb = 0.0;
            b.Q = 1.414;
            b.Enabled = true;
            b.UpdateCoefficients(48000.0);
        }

        PreampDb = 0.0;
        _isUpdatingUi = true;
        try
        {
            if (_presetCombo != null) _presetCombo.SelectedIndex = 0;
        }
        finally
        {
            _isUpdatingUi = false;
        }

        SyncAllViewsFromBands(_curveControl?.SelectedBandIndex ?? 0);
        NotifyAllBandsChanged();
        PreampChanged?.Invoke(0.0);
    }

    public void UpdateTheme(bool isDark)
    {
        _isDark = isDark;
        RequestedTheme = isDark ? ElementTheme.Dark : ElementTheme.Light;

        Brush primary = GetPrimaryBrush();
        Brush secondary = GetSecondaryBrush();
        Brush subHeader = GetSubHeaderBrush();
        Brush muted = GetMutedBrush();

        if (_rootGrid != null)
        {
            _rootGrid.Background = CreateSurfaceBrush(LiquidGlassVariant.Clear, 0.72);
        }

        ApplyCardSurface(_curveCard, 0.90);
        ApplyCardSurface(_carouselCard, 0.90);
        ApplyCardSurface(_inspectorCard, 0.92);
        ApplyCardSurface(_graphicFadersCard, 0.90);
        ApplyCardSurface(_mixAssistantCard, 0.92);
        ApplyCardSurface(_bottomToolbarCard, 0.88);

        if (_modePillBorder != null)
        {
            _modePillBorder.Background = CreateSoftSurfaceBrush();
            _modePillBorder.BorderBrush = CreateRimBrush(false);
        }
        if (_scopeBorder != null)
        {
            _scopeBorder.Background = CreateSoftSurfaceBrush();
            _scopeBorder.BorderBrush = CreateRimBrush(false);
        }

        foreach (var tb in _primaryTextElements) tb.Foreground = primary;
        foreach (var tb in _secondaryTextElements) tb.Foreground = secondary;
        foreach (var tb in _subHeaderTextElements) tb.Foreground = subHeader;
        foreach (var tb in _mutedTextElements) tb.Foreground = muted;

        foreach (var btn in _glassToolButtons)
        {
            btn.Background = CreateSurfaceBrush(LiquidGlassVariant.Clear, 0.78);
            btn.BorderBrush = CreateRimBrush(false);
            btn.Foreground = secondary;
        }

        if (_presetCombo != null)
        {
            _presetCombo.Background = CreateSurfaceBrush(LiquidGlassVariant.Clear, 0.82);
            _presetCombo.BorderBrush = CreateRimBrush(false);
            _presetCombo.Foreground = primary;
        }
        if (_filterTypeCombo != null)
        {
            _filterTypeCombo.Background = CreateSurfaceBrush(LiquidGlassVariant.Clear, 0.82);
            _filterTypeCombo.BorderBrush = CreateRimBrush(false);
            _filterTypeCombo.Foreground = primary;
        }
        if (_targetCombo != null)
        {
            _targetCombo.Background = CreateSurfaceBrush(LiquidGlassVariant.Clear, 0.82);
            _targetCombo.BorderBrush = CreateRimBrush(false);
            _targetCombo.Foreground = primary;
        }
        if (_presetNameTb != null)
        {
            _presetNameTb.Background = CreateSurfaceBrush(LiquidGlassVariant.Clear, 0.80);
            _presetNameTb.BorderBrush = CreateRimBrush(false);
            _presetNameTb.Foreground = primary;
        }

        if (_bandIndexBadgeBorder != null)
        {
            _bandIndexBadgeBorder.Background = new SolidColorBrush(_isDark
                ? Color.FromArgb(85, 99, 102, 241)
                : Color.FromArgb(255, 238, 242, 255));
            _bandIndexBadgeBorder.BorderBrush = new SolidColorBrush(_isDark
                ? Color.FromArgb(200, 129, 140, 248)
                : Color.FromArgb(255, 99, 102, 241));
        }
        if (_bandIndexBadgeTb != null)
        {
            _bandIndexBadgeTb.Foreground = new SolidColorBrush(_isDark
                ? Color.FromArgb(255, 199, 210, 254)
                : Color.FromArgb(255, 79, 70, 229));
        }

        for (int i = 0; i < _graphicFreqLabels.Length; i++)
        {
            if (_graphicFreqLabels[i] != null) _graphicFreqLabels[i].Foreground = subHeader;
            if (_graphicGainLabels[i] != null) _graphicGainLabels[i].Foreground = primary;
        }

        try
        {
            LiquidGlassEngine.UpgradeVisualTreeToLiquidGlass(this, isDark: isDark);
        }
        catch
        {
        }

        RefreshModePillVisuals();
        RefreshScopeButtonVisuals();
        RefreshBandPills();
        RefreshMixAssistantButtonVisuals();
        UpdateSpectrumButtonVisuals();
        UpdateAbButtonVisuals();
        UpdateUndoRedoButtonState();
    }

    private void InitializeDefaultBands()
    {
        _bands.Clear();
        double[] freqs = MixAssistantEngine.GraphicFrequencies;
        for (int i = 0; i < freqs.Length; i++)
        {
            var f = new EchoBiquadFilter
            {
                FilterType = i == 0
                    ? EchoFilterType.LowShelf
                    : (i == freqs.Length - 1 ? EchoFilterType.HighShelf : EchoFilterType.Peaking),
                FrequencyHz = freqs[i],
                GainDb = 0.0,
                Q = 1.414,
                Enabled = true
            };
            f.UpdateCoefficients(48000.0);
            _bands.Add(f);
        }
    }

    private void BuildUi()
    {
        _rootGrid = new Grid
        {
            Padding = new Thickness(14, 10, 14, 10),
            RowSpacing = 8
        };
        _rootGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        _rootGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1.0, GridUnitType.Star) });

        BuildTopBar();
        Grid.SetRow(BuildBodyScrollViewer(), 1);

        Content = _rootGrid;
    }

    private void BuildTopBar()
    {
        var topBar = new Grid { RowSpacing = 6, ColumnSpacing = 8 };
        topBar.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        topBar.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        var row0 = new Grid { ColumnSpacing = 8 };
        row0.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        row0.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.0, GridUnitType.Star) });
        row0.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        // Left: Title + Power
        var leftSp = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 10,
            VerticalAlignment = VerticalAlignment.Center
        };

        _titleTb = TrackPrimaryText(new TextBlock
        {
            Text = "参数 EQ",
            FontSize = 18,
            FontWeight = FontWeights.Bold,
            VerticalAlignment = VerticalAlignment.Center
        });
        leftSp.Children.Add(_titleTb);

        _powerSwitch = new ToggleSwitch
        {
            IsOn = _isEqEnabled,
            OnContent = "",
            OffContent = "",
            MinWidth = 0,
            VerticalAlignment = VerticalAlignment.Center
        };
        _powerSwitch.Toggled += (s, e) =>
        {
            if (_isUpdatingUi) return;
            _isEqEnabled = _powerSwitch.IsOn;
            EnabledChanged?.Invoke(_isEqEnabled);
        };
        leftSp.Children.Add(_powerSwitch);
        Grid.SetColumn(leftSp, 0);
        row0.Children.Add(leftSp);

        // Mode Toggle Pill: 参数 EQ / 图形均衡 & Mix Assistant
        _modePillBorder = new Border
        {
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(14),
            Padding = new Thickness(3),
            HorizontalAlignment = HorizontalAlignment.Right,
            VerticalAlignment = VerticalAlignment.Center
        };
        var modeSp = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 2 };
        _btnModeParametric = new Button
        {
            Content = "参数 EQ (Parametric)",
            Padding = new Thickness(10, 4, 10, 4),
            CornerRadius = new CornerRadius(11),
            FontSize = 12,
            BorderThickness = new Thickness(0)
        };
        _btnModeParametric.Click += (s, e) => SetWorkbenchMode(false);

        _btnModeGraphic = new Button
        {
            Content = "图形均衡 & Mix Assistant",
            Padding = new Thickness(10, 4, 10, 4),
            CornerRadius = new CornerRadius(11),
            FontSize = 12,
            BorderThickness = new Thickness(0)
        };
        _btnModeGraphic.Click += (s, e) => SetWorkbenchMode(true);

        modeSp.Children.Add(_btnModeParametric);
        modeSp.Children.Add(_btnModeGraphic);
        _modePillBorder.Child = modeSp;
        Grid.SetColumn(_modePillBorder, 2);
        row0.Children.Add(_modePillBorder);

        Grid.SetRow(row0, 0);
        topBar.Children.Add(row0);

        // Row 1: Preset + Spectrum + Undo/Redo/Reset + Scale Zoom
        var rightSp = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 6,
            VerticalAlignment = VerticalAlignment.Center
        };

        _presetCombo = new ComboBox
        {
            MinWidth = 175,
            MaxWidth = 230,
            CornerRadius = new CornerRadius(8),
            BorderThickness = new Thickness(1)
        };
        foreach (var preset in _builtinPresets)
        {
            _presetCombo.Items.Add(new ComboBoxItem { Content = preset.Name });
        }
        _presetCombo.SelectedIndex = 0;
        _presetCombo.SelectionChanged += OnPresetComboSelectionChanged;
        rightSp.Children.Add(_presetCombo);

        _btnSpectrum = new Button
        {
            Content = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Spacing = 5,
                Children =
                {
                    new FontIcon { Glyph = "\uE9D9", FontSize = 13 },
                    new TextBlock { Text = "频谱", FontSize = 12, VerticalAlignment = VerticalAlignment.Center }
                }
            },
            Padding = new Thickness(10, 4, 10, 4),
            CornerRadius = new CornerRadius(8),
            BorderThickness = new Thickness(1)
        };
        _btnSpectrum.Click += (s, e) =>
        {
            if (_curveControl == null) return;
            _curveControl.ShowSpectrum = !_curveControl.ShowSpectrum;
            UpdateSpectrumButtonVisuals();
        };
        rightSp.Children.Add(_btnSpectrum);

        _btnUndo = CreateGlassToolButton(new FontIcon { Glyph = "\uE7A7", FontSize = 13 }, new Thickness(8, 5, 8, 5));
        _btnUndo.Click += (s, e) => Undo();
        ToolTipService.SetToolTip(_btnUndo, "撤销 (Undo)");
        rightSp.Children.Add(_btnUndo);

        _btnRedo = CreateGlassToolButton(new FontIcon { Glyph = "\uE7A6", FontSize = 13 }, new Thickness(8, 5, 8, 5));
        _btnRedo.Click += (s, e) => Redo();
        ToolTipService.SetToolTip(_btnRedo, "重做 (Redo)");
        rightSp.Children.Add(_btnRedo);

        _btnResetTop = CreateGlassToolButton(new FontIcon { Glyph = "\uE72C", FontSize = 13 }, new Thickness(8, 5, 8, 5));
        _btnResetTop.Click += (s, e) => ResetToFlat();
        ToolTipService.SetToolTip(_btnResetTop, "重置 EQ 为平坦");
        rightSp.Children.Add(_btnResetTop);

        var scaleSp = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 3,
            VerticalAlignment = VerticalAlignment.Center
        };
        _btnZoomIn = CreateGlassToolButton("+", new Thickness(8, 3, 8, 3));
        _btnZoomIn.Click += (s, e) =>
        {
            if (_curveControl.DbRange > 6.0)
            {
                _curveControl.DbRange -= 6.0;
                _tbScale.Text = $"±{_curveControl.DbRange:F0} dB";
            }
        };
        _tbScale = TrackPrimaryText(new TextBlock
        {
            Text = "±12 dB",
            FontSize = 12,
            FontWeight = FontWeights.SemiBold,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(4, 0, 4, 0)
        });
        _btnZoomOut = CreateGlassToolButton("-", new Thickness(8, 3, 8, 3));
        _btnZoomOut.Click += (s, e) =>
        {
            if (_curveControl.DbRange < 30.0)
            {
                _curveControl.DbRange += 6.0;
                _tbScale.Text = $"±{_curveControl.DbRange:F0} dB";
            }
        };
        _btnAutoScale = CreateGlassToolButton("自动", new Thickness(8, 3, 8, 3));
        _btnAutoScale.Click += (s, e) => AutoFitCurveScale();

        scaleSp.Children.Add(_btnZoomIn);
        scaleSp.Children.Add(_tbScale);
        scaleSp.Children.Add(_btnZoomOut);
        scaleSp.Children.Add(_btnAutoScale);
        rightSp.Children.Add(scaleSp);

        Grid.SetRow(rightSp, 1);
        topBar.Children.Add(rightSp);

        Grid.SetRow(topBar, 0);
        _rootGrid.Children.Add(topBar);
    }

    private ScrollViewer BuildBodyScrollViewer()
    {
        var sv = new ScrollViewer
        {
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled
        };

        var bodyStack = new StackPanel { Spacing = 8 };

        // A. Interactive Parametric EQ Curve Control
        _curveControl = new ParametricEqCurveControl
        {
            Height = 200,
            HorizontalAlignment = HorizontalAlignment.Stretch
        };
        _curveControl.BandSelected += idx => SelectBand(idx);
        _curveControl.BandChanged += OnCurveNodeMoved;

        _curveCard = new Border
        {
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(12),
            Padding = new Thickness(2),
            Child = _curveControl
        };
        bodyStack.Children.Add(_curveCard);

        // B. Parametric Mode Section (Band Pills Carousel + Selected Band Inspector)
        _parametricSection = new StackPanel { Spacing = 8 };
        _parametricSection.Children.Add(BuildBandCarouselCard());
        _parametricSection.Children.Add(BuildBandInspectorCard());
        bodyStack.Children.Add(_parametricSection);

        // C. Graphic EQ & Mix Assistant Section
        _graphicAssistantSection = new StackPanel
        {
            Spacing = 8,
            Visibility = Visibility.Collapsed
        };
        _graphicAssistantSection.Children.Add(BuildMixAssistantCard());
        _graphicAssistantSection.Children.Add(BuildGraphicFadersCard());
        bodyStack.Children.Add(_graphicAssistantSection);

        // D. Bottom Action Toolbar
        bodyStack.Children.Add(BuildBottomToolbar());

        sv.Content = bodyStack;
        _rootGrid.Children.Add(sv);
        return sv;
    }

    private Border BuildBandCarouselCard()
    {
        _carouselCard = new Border
        {
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(12),
            Padding = new Thickness(16, 12, 16, 12)
        };

        var grid = new Grid { RowSpacing = 8 };
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        var statusRow = new Grid();
        statusRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.0, GridUnitType.Star) });
        statusRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        var statusLeft = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 12, VerticalAlignment = VerticalAlignment.Center };
        _headroomStatusTb = new TextBlock
        {
            Text = "● 待确认 余量 -0.0 dB",
            FontSize = 12,
            FontWeight = FontWeights.SemiBold,
            Foreground = new SolidColorBrush(Color.FromArgb(255, 22, 163, 74))
        };
        _beforeStateTb = TrackMutedText(new TextBlock { Text = "处理前 0.0 dB", FontSize = 12 });
        _afterStateTb = TrackMutedText(new TextBlock { Text = "处理后 0.0 dB", FontSize = 12 });
        statusLeft.Children.Add(_headroomStatusTb);
        statusLeft.Children.Add(_beforeStateTb);
        statusLeft.Children.Add(_afterStateTb);
        Grid.SetColumn(statusLeft, 0);
        statusRow.Children.Add(statusLeft);

        var compareRight = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
        _btnAbTop = CreateGlassToolButton("⇄ A/B 对比", new Thickness(10, 4, 10, 4));
        _btnAbTop.Click += (s, e) => ToggleAbCompare();
        _btnLoudnessMatch = CreateGlassToolButton("实时等响对比", new Thickness(10, 4, 10, 4));
        _btnLoudnessMatch.Click += (s, e) =>
        {
            _isLoudnessMatched = !_isLoudnessMatched;
            LoudnessMatchToggled?.Invoke(_isLoudnessMatched);
            UpdateAbButtonVisuals();
        };
        compareRight.Children.Add(_btnAbTop);
        compareRight.Children.Add(_btnLoudnessMatch);
        Grid.SetColumn(compareRight, 1);
        statusRow.Children.Add(compareRight);

        Grid.SetRow(statusRow, 0);
        grid.Children.Add(statusRow);

        var pillsSv = new ScrollViewer
        {
            HorizontalScrollBarVisibility = ScrollBarVisibility.Hidden,
            VerticalScrollBarVisibility = ScrollBarVisibility.Disabled
        };
        _bandPillsStack = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
        pillsSv.Content = _bandPillsStack;
        Grid.SetRow(pillsSv, 1);
        grid.Children.Add(pillsSv);

        _carouselCard.Child = grid;
        return _carouselCard;
    }

    private Border BuildBandInspectorCard()
    {
        _inspectorCard = new Border
        {
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(12),
            Padding = new Thickness(18, 14, 18, 14)
        };

        var stack = new StackPanel { Spacing = 12 };

        // Row 1: Badge + FilterType + Enable + Reset + Zero
        var row1 = new Grid { ColumnSpacing = 12 };
        row1.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        row1.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        row1.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.0, GridUnitType.Star) });
        row1.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        _bandIndexBadgeTb = new TextBlock
        {
            Text = "01",
            FontSize = 13,
            FontWeight = FontWeights.Bold,
            VerticalAlignment = VerticalAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Center
        };
        _bandIndexBadgeBorder = new Border
        {
            BorderThickness = new Thickness(1.2),
            CornerRadius = new CornerRadius(14),
            Width = 32,
            Height = 32,
            Child = _bandIndexBadgeTb
        };
        Grid.SetColumn(_bandIndexBadgeBorder, 0);
        row1.Children.Add(_bandIndexBadgeBorder);

        _filterTypeCombo = new ComboBox
        {
            MinWidth = 156,
            CornerRadius = new CornerRadius(8),
            BorderThickness = new Thickness(1)
        };
        string[] typeNames =
        {
            "峰值 (Peaking)",
            "低架 (Low Shelf)",
            "高架 (High Shelf)",
            "低通 (Low Pass)",
            "高通 (High Pass)",
            "带通 (Band Pass)",
            "陷波 (Notch)"
        };
        foreach (string t in typeNames) _filterTypeCombo.Items.Add(new ComboBoxItem { Content = t });
        _filterTypeCombo.SelectedIndex = 0;
        _filterTypeCombo.SelectionChanged += OnInspectorFilterTypeChanged;
        Grid.SetColumn(_filterTypeCombo, 1);
        row1.Children.Add(_filterTypeCombo);

        var row1Right = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 10, VerticalAlignment = VerticalAlignment.Center };
        _bandEnableCb = new CheckBox
        {
            Content = "启用此段",
            IsChecked = true,
            FontSize = 13,
            FontWeight = FontWeights.Medium
        };
        _bandEnableCb.Checked += (s, e) => OnInspectorBandEnabledChanged(true);
        _bandEnableCb.Unchecked += (s, e) => OnInspectorBandEnabledChanged(false);
        row1Right.Children.Add(_bandEnableCb);

        _btnResetBand = CreateGlassToolButton(
            new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Spacing = 4,
                Children = { new FontIcon { Glyph = "\uE72C", FontSize = 12 }, new TextBlock { Text = "重置此段", FontSize = 12 } }
            },
            new Thickness(10, 5, 10, 5));
        _btnResetBand.Click += (s, e) => ResetCurrentBand();
        row1Right.Children.Add(_btnResetBand);

        _btnDeleteBand = new Button
        {
            Content = new FontIcon { Glyph = "\uE74D", FontSize = 13 },
            Padding = new Thickness(8, 5, 8, 5),
            CornerRadius = new CornerRadius(6),
            Background = new SolidColorBrush(Color.FromArgb(40, 239, 68, 68)),
            BorderBrush = new SolidColorBrush(Color.FromArgb(160, 239, 68, 68)),
            BorderThickness = new Thickness(1),
            Foreground = new SolidColorBrush(Color.FromArgb(255, 220, 38, 38))
        };
        ToolTipService.SetToolTip(_btnDeleteBand, "增益归零（或移除新增频段）");
        _btnDeleteBand.Click += (s, e) => ZeroOrDeleteCurrentBand();
        row1Right.Children.Add(_btnDeleteBand);

        Grid.SetColumn(row1Right, 3);
        row1.Children.Add(row1Right);
        stack.Children.Add(row1);

        // Row 2: Freq / Gain / Q Sliders
        var slidersGrid = new Grid { ColumnSpacing = 22 };
        slidersGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.0, GridUnitType.Star) });
        slidersGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.0, GridUnitType.Star) });
        slidersGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.0, GridUnitType.Star) });

        // Frequency
        var freqCol = new StackPanel { Spacing = 4 };
        var freqHead = new Grid();
        freqHead.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.0, GridUnitType.Star) });
        freqHead.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        freqHead.Children.Add(TrackSubHeaderText(new TextBlock { Text = "频率 Frequency", FontSize = 12, FontWeight = FontWeights.Medium }));
        _freqValueTb = new TextBlock
        {
            Text = "1000 Hz",
            FontSize = 14,
            FontWeight = FontWeights.Bold,
            Foreground = new SolidColorBrush(Color.FromArgb(255, 2, 132, 199))
        };
        Grid.SetColumn(_freqValueTb, 1);
        freqHead.Children.Add(_freqValueTb);
        freqCol.Children.Add(freqHead);
        _freqSlider = new Slider { Minimum = 0, Maximum = 1000, StepFrequency = 1, Value = 500 };
        _freqSlider.ValueChanged += OnInspectorFreqSliderChanged;
        freqCol.Children.Add(_freqSlider);
        Grid.SetColumn(freqCol, 0);
        slidersGrid.Children.Add(freqCol);

        // Gain
        var gainCol = new StackPanel { Spacing = 4 };
        var gainHead = new Grid();
        gainHead.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.0, GridUnitType.Star) });
        gainHead.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        gainHead.Children.Add(TrackSubHeaderText(new TextBlock { Text = "增益 Gain", FontSize = 12, FontWeight = FontWeights.Medium }));
        _gainValueTb = new TextBlock
        {
            Text = "0.0 dB",
            FontSize = 14,
            FontWeight = FontWeights.Bold,
            Foreground = new SolidColorBrush(Color.FromArgb(255, 234, 88, 12))
        };
        Grid.SetColumn(_gainValueTb, 1);
        gainHead.Children.Add(_gainValueTb);
        gainCol.Children.Add(gainHead);
        _gainSlider = new Slider { Minimum = -18.0, Maximum = 18.0, StepFrequency = 0.1, Value = 0.0 };
        _gainSlider.ValueChanged += OnInspectorGainSliderChanged;
        gainCol.Children.Add(_gainSlider);
        Grid.SetColumn(gainCol, 1);
        slidersGrid.Children.Add(gainCol);

        // Q
        var qCol = new StackPanel { Spacing = 4 };
        var qHead = new Grid();
        qHead.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.0, GridUnitType.Star) });
        qHead.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        qHead.Children.Add(TrackSubHeaderText(new TextBlock { Text = "品质因数 Q", FontSize = 12, FontWeight = FontWeights.Medium }));
        _qValueTb = new TextBlock
        {
            Text = "1.41",
            FontSize = 14,
            FontWeight = FontWeights.Bold,
            Foreground = new SolidColorBrush(Color.FromArgb(255, 124, 58, 237))
        };
        Grid.SetColumn(_qValueTb, 1);
        qHead.Children.Add(_qValueTb);
        qCol.Children.Add(qHead);
        _qSlider = new Slider { Minimum = 0.1, Maximum = 10.0, StepFrequency = 0.05, Value = 1.41 };
        _qSlider.ValueChanged += OnInspectorQSliderChanged;
        qCol.Children.Add(_qSlider);

        var qTagsSp = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 6, HorizontalAlignment = HorizontalAlignment.Right };
        void AddQTag(string label, double qVal)
        {
            var b = CreateGlassToolButton(label, new Thickness(8, 2, 8, 2));
            b.FontSize = 10;
            b.CornerRadius = new CornerRadius(4);
            b.Click += (s, e) => { _qSlider.Value = qVal; };
            _qQuickButtons.Add(b);
            qTagsSp.Children.Add(b);
        }
        AddQTag("宽 (0.5)", 0.5);
        AddQTag("标准 (1.4)", 1.41);
        AddQTag("窄 (3.0)", 3.0);
        qCol.Children.Add(qTagsSp);

        Grid.SetColumn(qCol, 2);
        slidersGrid.Children.Add(qCol);
        stack.Children.Add(slidersGrid);

        // Row 3: Snap, Scope, Preamp
        var row3 = new Grid { ColumnSpacing = 16 };
        row3.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        row3.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.0, GridUnitType.Star) });

        var scopeSp = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 10, VerticalAlignment = VerticalAlignment.Center };
        _cbSnapFreq = new CheckBox
        {
            Content = "吸附到标准频点",
            FontSize = 12,
            IsChecked = false
        };
        _cbSnapFreq.Checked += (s, e) => _snapToStandardFreq = true;
        _cbSnapFreq.Unchecked += (s, e) => _snapToStandardFreq = false;
        scopeSp.Children.Add(_cbSnapFreq);

        _scopeBorder = new Border
        {
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(8),
            Padding = new Thickness(6, 3, 6, 3)
        };
        var scopeInner = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 4 };
        scopeInner.Children.Add(TrackSubHeaderText(new TextBlock
        {
            Text = "作用域:",
            FontSize = 11,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(0, 0, 4, 0)
        }));
        string[] scopeLabels = { "立体声", "左", "右", "中 (M)", "侧 (S)" };
        for (int i = 0; i < scopeLabels.Length; i++)
        {
            int idx = i;
            var btn = new Button
            {
                Content = scopeLabels[i],
                Padding = new Thickness(8, 2, 8, 2),
                FontSize = 11,
                CornerRadius = new CornerRadius(4),
                BorderThickness = new Thickness(1)
            };
            btn.Click += (s, e) =>
            {
                _selectedScopeIndex = idx;
                RefreshScopeButtonVisuals();
                ScopeChanged?.Invoke(idx);
            };
            _scopeButtons.Add(btn);
            scopeInner.Children.Add(btn);
        }
        _scopeBorder.Child = scopeInner;
        scopeSp.Children.Add(_scopeBorder);
        Grid.SetColumn(scopeSp, 0);
        row3.Children.Add(scopeSp);

        var preampSp = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 8,
            HorizontalAlignment = HorizontalAlignment.Right,
            VerticalAlignment = VerticalAlignment.Center
        };
        preampSp.Children.Add(TrackSubHeaderText(new TextBlock
        {
            Text = "EQ 前级增益:",
            FontSize = 12,
            FontWeight = FontWeights.Medium,
            VerticalAlignment = VerticalAlignment.Center
        }));
        _preampSlider = new Slider
        {
            Minimum = -12.0,
            Maximum = 12.0,
            StepFrequency = 0.5,
            Value = _preampDb,
            Width = 135,
            VerticalAlignment = VerticalAlignment.Center
        };
        _preampSlider.ValueChanged += OnPreampSliderChanged;
        preampSp.Children.Add(_preampSlider);

        _preampValueTb = TrackPrimaryText(new TextBlock
        {
            Text = "0.0 dB",
            FontSize = 12,
            FontWeight = FontWeights.Bold,
            Width = 54,
            VerticalAlignment = VerticalAlignment.Center
        });
        preampSp.Children.Add(_preampValueTb);

        _compositeHintTb = TrackMutedText(new TextBlock
        {
            Text = "合成峰值 0.0 dB",
            FontSize = 11,
            VerticalAlignment = VerticalAlignment.Center
        });
        preampSp.Children.Add(_compositeHintTb);

        Grid.SetColumn(preampSp, 1);
        row3.Children.Add(preampSp);
        stack.Children.Add(row3);

        _inspectorCard.Child = stack;
        return _inspectorCard;
    }

    private Border BuildGraphicFadersCard()
    {
        _graphicFadersCard = new Border
        {
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(12),
            Padding = new Thickness(16, 14, 16, 14)
        };

        var rootStack = new StackPanel { Spacing = 10 };

        var headerGrid = new Grid();
        headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.0, GridUnitType.Star) });
        headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        var titleSp = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 10, VerticalAlignment = VerticalAlignment.Center };
        titleSp.Children.Add(TrackPrimaryText(new TextBlock
        {
            Text = "10 段图形均衡推子 (Graphic EQ)",
            FontSize = 14,
            FontWeight = FontWeights.Bold,
            VerticalAlignment = VerticalAlignment.Center
        }));
        titleSp.Children.Add(TrackMutedText(new TextBlock
        {
            Text = "与上方参数 EQ 曲线双向实时同步",
            FontSize = 11,
            VerticalAlignment = VerticalAlignment.Center
        }));
        Grid.SetColumn(titleSp, 0);
        headerGrid.Children.Add(titleSp);

        var btnZeroFaders = CreateGlassToolButton("推子归零", new Thickness(10, 4, 10, 4));
        btnZeroFaders.Click += (s, e) => ResetToFlat();
        Grid.SetColumn(btnZeroFaders, 1);
        headerGrid.Children.Add(btnZeroFaders);

        rootStack.Children.Add(headerGrid);

        var fadersGrid = new Grid { ColumnSpacing = 6 };
        for (int i = 0; i < 10; i++)
        {
            fadersGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.0, GridUnitType.Star) });
            int bandIndex = i;

            var colStack = new StackPanel
            {
                Spacing = 4,
                HorizontalAlignment = HorizontalAlignment.Center
            };

            var gainTb = TrackPrimaryText(new TextBlock
            {
                Text = "0.0",
                FontSize = 11,
                FontWeight = FontWeights.SemiBold,
                HorizontalAlignment = HorizontalAlignment.Center
            });
            _graphicGainLabels[i] = gainTb;

            var slider = new Slider
            {
                Orientation = Orientation.Vertical,
                Minimum = -12.0,
                Maximum = 12.0,
                StepFrequency = 0.1,
                SmallChange = 0.5,
                LargeChange = 2.0,
                Height = 96,
                HorizontalAlignment = HorizontalAlignment.Center
            };
            slider.ValueChanged += (s, e) => OnGraphicFaderChanged(bandIndex, e.NewValue);
            _graphicSliders[i] = slider;

            var dot = new Border
            {
                Width = 8,
                Height = 8,
                CornerRadius = new CornerRadius(4),
                Background = new SolidColorBrush(BandColors[i % BandColors.Length]),
                HorizontalAlignment = HorizontalAlignment.Center
            };

            double hz = MixAssistantEngine.GraphicFrequencies[i];
            string freqStr = hz >= 1000.0 ? $"{hz / 1000.0:0.#}k" : (hz < 100.0 ? $"{hz:0.#}" : $"{hz:0}");
            var freqTb = TrackSubHeaderText(new TextBlock
            {
                Text = freqStr,
                FontSize = 11,
                FontWeight = FontWeights.Medium,
                HorizontalAlignment = HorizontalAlignment.Center
            });
            _graphicFreqLabels[i] = freqTb;

            colStack.Children.Add(gainTb);
            colStack.Children.Add(slider);
            colStack.Children.Add(dot);
            colStack.Children.Add(freqTb);

            Grid.SetColumn(colStack, i);
            fadersGrid.Children.Add(colStack);
        }

        rootStack.Children.Add(fadersGrid);
        _graphicFadersCard.Child = rootStack;
        return _graphicFadersCard;
    }

    private Border BuildMixAssistantCard()
    {
        _mixAssistantCard = new Border
        {
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(12),
            Padding = new Thickness(18, 14, 18, 14)
        };

        var stack = new StackPanel { Spacing = 12 };

        // Header Row
        var headerGrid = new Grid { ColumnSpacing = 12 };
        headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.0, GridUnitType.Star) });
        headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        var headerLeft = new StackPanel { Spacing = 2 };
        headerLeft.Children.Add(TrackPrimaryText(new TextBlock
        {
            Text = "🎛️ Mix Assistant 智能混音平衡助手",
            FontSize = 15,
            FontWeight = FontWeights.Bold
        }));
        headerLeft.Children.Add(TrackMutedText(new TextBlock
        {
            Text = "源自 Neutron Pro Tonal Balance 感知学习 · De-Masking 防掩蔽 · Sculptor 音色塑形",
            FontSize = 11
        }));
        Grid.SetColumn(headerLeft, 0);
        headerGrid.Children.Add(headerLeft);

        var statusRight = new StackPanel
        {
            Spacing = 4,
            Width = 260,
            VerticalAlignment = VerticalAlignment.Center
        };
        _learnStatusTb = TrackSubHeaderText(new TextBlock
        {
            Text = "点击“学习监听”或“分析音轨”诊断频谱平衡",
            FontSize = 11,
            TextTrimming = TextTrimming.CharacterEllipsis
        });
        _learnProgressBar = new ProgressBar
        {
            Minimum = 0,
            Maximum = 100,
            Value = 0,
            Height = 6
        };
        statusRight.Children.Add(_learnStatusTb);
        statusRight.Children.Add(_learnProgressBar);
        Grid.SetColumn(statusRight, 1);
        headerGrid.Children.Add(statusRight);

        stack.Children.Add(headerGrid);

        // Row 1: Learn / Analyze / Target / De-Mask
        var controlsRow = new Grid { ColumnSpacing = 12 };
        controlsRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        controlsRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        controlsRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.0, GridUnitType.Star) });
        controlsRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        _btnLearnTextTb = new TextBlock
        {
            Text = "🎧 学习监听 (Learn)",
            FontSize = 12,
            FontWeight = FontWeights.SemiBold,
            VerticalAlignment = VerticalAlignment.Center
        };
        _btnLearn = new Button
        {
            Content = _btnLearnTextTb,
            Padding = new Thickness(14, 6, 14, 6),
            CornerRadius = new CornerRadius(8),
            BorderThickness = new Thickness(1)
        };
        _btnLearn.Click += OnLearnButtonClick;
        Grid.SetColumn(_btnLearn, 0);
        controlsRow.Children.Add(_btnLearn);

        _btnAnalyzeTrack = CreateGlassToolButton("🔍 分析音轨 (Analyze)", new Thickness(12, 6, 12, 6));
        _btnAnalyzeTrack.Click += (s, e) => MixAssistantAnalyzeAudioRequested?.Invoke();
        _btnAnalyzeTrack.Visibility = _showAnalyzeTrackButton ? Visibility.Visible : Visibility.Collapsed;
        Grid.SetColumn(_btnAnalyzeTrack, 1);
        controlsRow.Children.Add(_btnAnalyzeTrack);

        var targetSp = new Grid
        {
            ColumnSpacing = 6,
            VerticalAlignment = VerticalAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Stretch
        };
        targetSp.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        targetSp.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.0, GridUnitType.Star) });
        var targetLabelTb = TrackSubHeaderText(new TextBlock
        {
            Text = "目标风格:",
            FontSize = 12,
            FontWeight = FontWeights.Medium,
            VerticalAlignment = VerticalAlignment.Center
        });
        Grid.SetColumn(targetLabelTb, 0);
        targetSp.Children.Add(targetLabelTb);

        _targetCombo = new ComboBox
        {
            MinWidth = 140,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            CornerRadius = new CornerRadius(8),
            BorderThickness = new Thickness(1)
        };
        foreach (var t in MixAssistantEngine.Targets)
        {
            _targetCombo.Items.Add(new ComboBoxItem { Content = t.DisplayName, Tag = t.Id });
        }
        _targetCombo.SelectedIndex = 0;
        _targetCombo.SelectionChanged += (s, e) =>
        {
            if (_isUpdatingUi) return;
            if (_targetCombo.SelectedItem is ComboBoxItem item && item.Tag is string id)
            {
                _selectedTargetId = id;
                ResolveCurrentProfileLive();
            }
        };
        Grid.SetColumn(_targetCombo, 1);
        targetSp.Children.Add(_targetCombo);
        Grid.SetColumn(targetSp, 2);
        controlsRow.Children.Add(targetSp);

        var demaskSp = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 6,
            VerticalAlignment = VerticalAlignment.Center
        };
        demaskSp.Children.Add(TrackSubHeaderText(new TextBlock
        {
            Text = "自动防掩蔽 (De-Mask)",
            FontSize = 12,
            VerticalAlignment = VerticalAlignment.Center
        }));
        _demaskSwitch = new ToggleSwitch
        {
            IsOn = _demaskMidMud,
            OnContent = "开",
            OffContent = "关",
            MinWidth = 0
        };
        _demaskSwitch.Toggled += (s, e) =>
        {
            if (_isUpdatingUi) return;
            _demaskMidMud = _demaskSwitch.IsOn;
            ResolveCurrentProfileLive();
        };
        demaskSp.Children.Add(_demaskSwitch);
        Grid.SetColumn(demaskSp, 3);
        controlsRow.Children.Add(demaskSp);

        stack.Children.Add(controlsRow);

        // Row 2: Amount, Low Sculpt, High Sculpt
        var sculptGrid = new Grid { ColumnSpacing = 20 };
        sculptGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.0, GridUnitType.Star) });
        sculptGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.0, GridUnitType.Star) });
        sculptGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.0, GridUnitType.Star) });

        // Amount 0-100%
        var amountCol = new StackPanel { Spacing = 2 };
        var amountHead = new Grid();
        amountHead.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.0, GridUnitType.Star) });
        amountHead.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        amountHead.Children.Add(TrackSubHeaderText(new TextBlock { Text = "匹配强度 Amount", FontSize = 12 }));
        _amountValueTb = new TextBlock
        {
            Text = "75%",
            FontSize = 13,
            FontWeight = FontWeights.Bold,
            Foreground = new SolidColorBrush(Color.FromArgb(255, 79, 70, 229))
        };
        Grid.SetColumn(_amountValueTb, 1);
        amountHead.Children.Add(_amountValueTb);
        amountCol.Children.Add(amountHead);
        _amountSlider = new Slider { Minimum = 0, Maximum = 100, StepFrequency = 1, Value = 75 };
        _amountSlider.ValueChanged += (s, e) =>
        {
            _matchAmount01 = Math.Clamp(e.NewValue / 100.0, 0.0, 1.0);
            _amountValueTb.Text = $"{e.NewValue:0}%";
            if (!_isUpdatingUi) ResolveCurrentProfileLive();
        };
        amountCol.Children.Add(_amountSlider);
        Grid.SetColumn(amountCol, 0);
        sculptGrid.Children.Add(amountCol);

        // Bass Sculpt
        var bassCol = new StackPanel { Spacing = 2 };
        var bassHead = new Grid();
        bassHead.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.0, GridUnitType.Star) });
        bassHead.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        bassHead.Children.Add(TrackSubHeaderText(new TextBlock { Text = "低频 Sculpt (Low Shelf)", FontSize = 12 }));
        _bassSculptValueTb = new TextBlock
        {
            Text = "0.0 dB",
            FontSize = 13,
            FontWeight = FontWeights.Bold,
            Foreground = new SolidColorBrush(Color.FromArgb(255, 14, 165, 233))
        };
        Grid.SetColumn(_bassSculptValueTb, 1);
        bassHead.Children.Add(_bassSculptValueTb);
        bassCol.Children.Add(bassHead);
        _bassSculptSlider = new Slider { Minimum = -6.0, Maximum = 6.0, StepFrequency = 0.5, Value = 0.0 };
        _bassSculptSlider.ValueChanged += (s, e) =>
        {
            _bassSculptDb = e.NewValue;
            _bassSculptValueTb.Text = $"{e.NewValue:+0.0;-0.0;0.0} dB";
            if (!_isUpdatingUi) ResolveCurrentProfileLive();
        };
        bassCol.Children.Add(_bassSculptSlider);
        Grid.SetColumn(bassCol, 1);
        sculptGrid.Children.Add(bassCol);

        // Treble Sculpt
        var trebleCol = new StackPanel { Spacing = 2 };
        var trebleHead = new Grid();
        trebleHead.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.0, GridUnitType.Star) });
        trebleHead.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        trebleHead.Children.Add(TrackSubHeaderText(new TextBlock { Text = "高频 Sculpt (High Shelf)", FontSize = 12 }));
        _trebleSculptValueTb = new TextBlock
        {
            Text = "0.0 dB",
            FontSize = 13,
            FontWeight = FontWeights.Bold,
            Foreground = new SolidColorBrush(Color.FromArgb(255, 168, 85, 247))
        };
        Grid.SetColumn(_trebleSculptValueTb, 1);
        trebleHead.Children.Add(_trebleSculptValueTb);
        trebleCol.Children.Add(trebleHead);
        _trebleSculptSlider = new Slider { Minimum = -6.0, Maximum = 6.0, StepFrequency = 0.5, Value = 0.0 };
        _trebleSculptSlider.ValueChanged += (s, e) =>
        {
            _trebleSculptDb = e.NewValue;
            _trebleSculptValueTb.Text = $"{e.NewValue:+0.0;-0.0;0.0} dB";
            if (!_isUpdatingUi) ResolveCurrentProfileLive();
        };
        trebleCol.Children.Add(_trebleSculptSlider);
        Grid.SetColumn(trebleCol, 2);
        sculptGrid.Children.Add(trebleCol);

        stack.Children.Add(sculptGrid);

        // Row 3: Spectral Issues + Apply / A/B / Reset
        var footerGrid = new Grid { ColumnSpacing = 12 };
        footerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.0, GridUnitType.Star) });
        footerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        var issuesSv = new ScrollViewer
        {
            HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
            VerticalScrollBarVisibility = ScrollBarVisibility.Disabled,
            VerticalAlignment = VerticalAlignment.Center
        };
        _issuesStack = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 6, VerticalAlignment = VerticalAlignment.Center };
        issuesSv.Content = _issuesStack;
        Grid.SetColumn(issuesSv, 0);
        footerGrid.Children.Add(issuesSv);

        var actionsSp = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8, VerticalAlignment = VerticalAlignment.Center };
        _btnApplyAssistant = new Button
        {
            Content = "✨ 应用助手方案 Apply",
            Padding = new Thickness(14, 6, 14, 6),
            CornerRadius = new CornerRadius(8),
            Background = new SolidColorBrush(Color.FromArgb(255, 79, 70, 229)),
            Foreground = new SolidColorBrush(Colors.White),
            FontWeight = FontWeights.SemiBold,
            FontSize = 12
        };
        _btnApplyAssistant.Click += (s, e) => OnApplyAssistantClick();
        actionsSp.Children.Add(_btnApplyAssistant);

        _btnAssistantAb = CreateGlassToolButton("⇄ A/B 对比", new Thickness(12, 6, 12, 6));
        _btnAssistantAb.Click += (s, e) => ToggleAbCompare();
        actionsSp.Children.Add(_btnAssistantAb);

        _btnAssistantReset = CreateGlassToolButton("🔄 重置", new Thickness(10, 6, 10, 6));
        _btnAssistantReset.Click += (s, e) => ResetMixAssistantAndEq();
        actionsSp.Children.Add(_btnAssistantReset);

        Grid.SetColumn(actionsSp, 1);
        footerGrid.Children.Add(actionsSp);
        stack.Children.Add(footerGrid);

        SetMixAssistantIssues(null);
        _mixAssistantCard.Child = stack;
        return _mixAssistantCard;
    }

    private Border BuildBottomToolbar()
    {
        _bottomToolbarCard = new Border
        {
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(12),
            Padding = new Thickness(16, 10, 16, 10)
        };

        var bar = new Grid { ColumnSpacing = 12 };
        bar.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.0, GridUnitType.Star) });
        bar.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        var leftSp = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 10, VerticalAlignment = VerticalAlignment.Center };

        _btnApplyOfflineDspTb = new TextBlock
        {
            Text = _applyOfflineDspButtonText,
            FontSize = 12,
            FontWeight = FontWeights.SemiBold,
            VerticalAlignment = VerticalAlignment.Center
        };
        _btnApplyOfflineDsp = new Button
        {
            Content = _btnApplyOfflineDspTb,
            Background = new SolidColorBrush(Color.FromArgb(255, 79, 70, 229)),
            Foreground = new SolidColorBrush(Colors.White),
            Padding = new Thickness(14, 6, 14, 6),
            CornerRadius = new CornerRadius(8),
            Visibility = _showApplyOfflineDspButton ? Visibility.Visible : Visibility.Collapsed
        };
        _btnApplyOfflineDsp.Click += (s, e) => ApplyOfflineDspRequested?.Invoke();
        leftSp.Children.Add(_btnApplyOfflineDsp);

        _presetNameTb = new TextBox
        {
            PlaceholderText = "保存为用户预设名称...",
            Width = 200,
            CornerRadius = new CornerRadius(8),
            BorderThickness = new Thickness(1)
        };
        leftSp.Children.Add(_presetNameTb);

        _btnSavePreset = CreateGlassToolButton("保存到预设", new Thickness(12, 6, 12, 6));
        _btnSavePreset.Click += OnSavePresetClick;
        leftSp.Children.Add(_btnSavePreset);

        Grid.SetColumn(leftSp, 0);
        bar.Children.Add(leftSp);

        var rightSp = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8, VerticalAlignment = VerticalAlignment.Center };
        _btnResetBottom = CreateGlassToolButton("🔄 重置曲线", new Thickness(10, 6, 10, 6));
        _btnResetBottom.Click += (s, e) => ResetToFlat();
        rightSp.Children.Add(_btnResetBottom);

        _btnImportAutoEq = CreateGlassToolButton("📁 导入预设 / APO", new Thickness(10, 6, 10, 6));
        _btnImportAutoEq.Click += (s, e) => ImportAutoEqRequested?.Invoke();
        rightSp.Children.Add(_btnImportAutoEq);

        Grid.SetColumn(rightSp, 1);
        bar.Children.Add(rightSp);

        _bottomToolbarCard.Child = bar;
        return _bottomToolbarCard;
    }

    private void SetWorkbenchMode(bool graphicAssistant)
    {
        _isGraphicMode = graphicAssistant;
        if (_curveControl != null)
        {
            _curveControl.Height = graphicAssistant ? 150 : 200;
        }
        if (_parametricSection != null)
        {
            _parametricSection.Visibility = graphicAssistant ? Visibility.Collapsed : Visibility.Visible;
        }
        if (_graphicAssistantSection != null)
        {
            _graphicAssistantSection.Visibility = graphicAssistant ? Visibility.Visible : Visibility.Collapsed;
        }
        if (graphicAssistant)
        {
            SyncGraphicFadersFromBands();
        }
        else
        {
            UpdateSelectedBandInspector(_curveControl?.SelectedBandIndex ?? 0);
            RefreshBandPills();
        }
        RefreshModePillVisuals();
    }

    private void RefreshBandPills()
    {
        if (_bandPillsStack == null) return;
        _bandPillsStack.Children.Clear();

        int activeIdx = _curveControl?.SelectedBandIndex ?? 0;
        Brush primary = GetPrimaryBrush();
        Brush secondary = GetSecondaryBrush();

        for (int i = 0; i < _bands.Count; i++)
        {
            int bandIndex = i;
            var band = _bands[i];
            bool isSelected = bandIndex == activeIdx;
            Color color = BandColors[bandIndex % BandColors.Length];

            var pill = new Button
            {
                Padding = new Thickness(8, 4, 10, 4),
                CornerRadius = new CornerRadius(16),
                Background = isSelected
                    ? new SolidColorBrush(Color.FromArgb(48, color.R, color.G, color.B))
                    : CreateSurfaceBrush(LiquidGlassVariant.Clear, 0.78),
                BorderBrush = isSelected
                    ? new SolidColorBrush(color)
                    : CreateRimBrush(false),
                BorderThickness = new Thickness(isSelected ? 1.5 : 1),
                Opacity = band.Enabled ? 1.0 : 0.55
            };

            var pillSp = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Spacing = 6,
                VerticalAlignment = VerticalAlignment.Center
            };
            pillSp.Children.Add(new Border
            {
                Background = new SolidColorBrush(color),
                CornerRadius = new CornerRadius(10),
                Width = 20,
                Height = 20,
                Child = new TextBlock
                {
                    Text = $"{bandIndex + 1:D2}",
                    FontSize = 10,
                    FontWeight = FontWeights.Bold,
                    Foreground = new SolidColorBrush(Colors.White),
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                }
            });

            string freqText = band.FrequencyHz >= 1000.0 ? $"{band.FrequencyHz / 1000.0:F1}k" : $"{band.FrequencyHz:F0}";
            string gainText = $"{band.GainDb:+0.0;-0.0;0.0}";
            pillSp.Children.Add(new TextBlock
            {
                Text = $"{freqText} Hz  {gainText} dB",
                FontSize = 11,
                FontWeight = isSelected ? FontWeights.Bold : FontWeights.Normal,
                Foreground = isSelected ? primary : secondary,
                VerticalAlignment = VerticalAlignment.Center
            });

            pill.Content = pillSp;
            pill.Click += (s, e) => SelectBand(bandIndex);
            _bandPillsStack.Children.Add(pill);
        }

        var btnAddBand = CreateGlassToolButton(
            new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Spacing = 4,
                Children = { new FontIcon { Glyph = "\uE710", FontSize = 11 }, new TextBlock { Text = "添加频段", FontSize = 11 } }
            },
            new Thickness(8, 4, 10, 4));
        btnAddBand.CornerRadius = new CornerRadius(16);
        btnAddBand.Click += (s, e) => AddNewBand();
        _bandPillsStack.Children.Add(btnAddBand);
    }

    private void SelectBand(int idx)
    {
        if (_bands.Count == 0) return;
        int clamped = Math.Clamp(idx, 0, _bands.Count - 1);
        if (_curveControl != null)
        {
            _curveControl.SelectedBandIndex = clamped;
        }
        UpdateSelectedBandInspector(clamped);
        RefreshBandPills();
    }

    private void SyncAllViewsFromBands(int selectedIndex)
    {
        int clamped = _bands.Count > 0 ? Math.Clamp(selectedIndex, 0, _bands.Count - 1) : 0;
        _curveControl?.SetBands(_bands, clamped);
        UpdateSelectedBandInspector(clamped);
        RefreshBandPills();
        SyncGraphicFadersFromBands();
        UpdateHeadroomStatusDisplay();
    }

    private void SyncGraphicFadersFromBands()
    {
        _isUpdatingUi = true;
        try
        {
            for (int i = 0; i < 10; i++)
            {
                double g = i < _bands.Count ? Math.Clamp(_bands[i].GainDb, -12.0, 12.0) : 0.0;
                if (_graphicSliders[i] != null) _graphicSliders[i].Value = g;
                if (_graphicGainLabels[i] != null) _graphicGainLabels[i].Text = $"{g:+0.0;-0.0;0.0}";
            }
        }
        finally
        {
            _isUpdatingUi = false;
        }
    }

    private void UpdateSelectedBandInspector(int idx)
    {
        if (idx < 0 || idx >= _bands.Count) return;
        var band = _bands[idx];

        _isUpdatingUi = true;
        try
        {
            if (_bandIndexBadgeTb != null) _bandIndexBadgeTb.Text = $"{idx + 1:D2}";
            if (_bandEnableCb != null) _bandEnableCb.IsChecked = band.Enabled;
            if (_filterTypeCombo != null) _filterTypeCombo.SelectedIndex = Math.Clamp((int)band.FilterType, 0, 6);

            if (_freqSlider != null)
            {
                double logMin = Math.Log10(20.0);
                double logMax = Math.Log10(20000.0);
                double logCur = Math.Log10(Math.Clamp(band.FrequencyHz, 20.0, 20000.0));
                _freqSlider.Value = ((logCur - logMin) / (logMax - logMin)) * 1000.0;
            }
            if (_freqValueTb != null)
            {
                _freqValueTb.Text = band.FrequencyHz >= 1000.0
                    ? $"{band.FrequencyHz / 1000.0:F2} kHz"
                    : $"{band.FrequencyHz:F0} Hz";
            }
            if (_gainSlider != null) _gainSlider.Value = Math.Clamp(band.GainDb, -18.0, 18.0);
            if (_gainValueTb != null) _gainValueTb.Text = $"{band.GainDb:+0.0;-0.0;0.0} dB";
            if (_qSlider != null) _qSlider.Value = Math.Clamp(band.Q, 0.1, 10.0);
            if (_qValueTb != null) _qValueTb.Text = $"{band.Q:F2}";
        }
        finally
        {
            _isUpdatingUi = false;
        }
    }

    private void OnCurveNodeMoved(int idx, double freq, double gain)
    {
        if (_isUpdatingUi || idx < 0 || idx >= _bands.Count) return;

        double finalFreq = _snapToStandardFreq ? SnapToNearestFrequency(freq) : freq;
        double finalGain = Math.Round(Math.Clamp(gain, -18.0, 18.0), 1);

        var band = _bands[idx];
        band.FrequencyHz = finalFreq;
        band.GainDb = finalGain;
        band.UpdateCoefficients(48000.0);

        if (_snapToStandardFreq)
        {
            _curveControl?.SetBands(_bands, idx);
        }

        UpdateSelectedBandInspector(idx);
        SyncGraphicFadersFromBands();
        RefreshBandPills();
        UpdateHeadroomStatusDisplay();
        NotifySingleBandChanged(idx, band);
    }

    private void OnInspectorFreqSliderChanged(object sender, RangeBaseValueChangedEventArgs e)
    {
        if (_isUpdatingUi || _curveControl == null) return;
        int idx = _curveControl.SelectedBandIndex;
        if (idx < 0 || idx >= _bands.Count) return;

        double logMin = Math.Log10(20.0);
        double logMax = Math.Log10(20000.0);
        double freq = Math.Pow(10.0, logMin + (e.NewValue / 1000.0) * (logMax - logMin));
        freq = Math.Clamp(freq, 20.0, 20000.0);
        if (_snapToStandardFreq) freq = SnapToNearestFrequency(freq);

        var band = _bands[idx];
        band.FrequencyHz = Math.Round(freq, 1);
        band.UpdateCoefficients(48000.0);

        if (_freqValueTb != null)
        {
            _freqValueTb.Text = band.FrequencyHz >= 1000.0
                ? $"{band.FrequencyHz / 1000.0:F2} kHz"
                : $"{band.FrequencyHz:F0} Hz";
        }
        _curveControl.SetBands(_bands, idx);
        RefreshBandPills();
        UpdateHeadroomStatusDisplay();
        NotifySingleBandChanged(idx, band);
    }

    private void OnInspectorGainSliderChanged(object sender, RangeBaseValueChangedEventArgs e)
    {
        if (_isUpdatingUi || _curveControl == null) return;
        int idx = _curveControl.SelectedBandIndex;
        if (idx < 0 || idx >= _bands.Count) return;

        var band = _bands[idx];
        band.GainDb = Math.Round(e.NewValue, 1);
        band.UpdateCoefficients(48000.0);

        if (_gainValueTb != null) _gainValueTb.Text = $"{band.GainDb:+0.0;-0.0;0.0} dB";
        _curveControl.SetBands(_bands, idx);
        SyncGraphicFadersFromBands();
        RefreshBandPills();
        UpdateHeadroomStatusDisplay();
        NotifySingleBandChanged(idx, band);
    }

    private void OnInspectorQSliderChanged(object sender, RangeBaseValueChangedEventArgs e)
    {
        if (_isUpdatingUi || _curveControl == null) return;
        int idx = _curveControl.SelectedBandIndex;
        if (idx < 0 || idx >= _bands.Count) return;

        var band = _bands[idx];
        band.Q = Math.Round(e.NewValue, 2);
        band.UpdateCoefficients(48000.0);

        if (_qValueTb != null) _qValueTb.Text = $"{band.Q:F2}";
        _curveControl.SetBands(_bands, idx);
        UpdateHeadroomStatusDisplay();
        NotifySingleBandChanged(idx, band);
    }

    private void OnInspectorFilterTypeChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isUpdatingUi || _curveControl == null || _filterTypeCombo == null) return;
        int idx = _curveControl.SelectedBandIndex;
        if (idx < 0 || idx >= _bands.Count) return;

        int sel = _filterTypeCombo.SelectedIndex;
        if (sel >= 0 && sel <= 6)
        {
            PushUndoSnapshot();
            var band = _bands[idx];
            band.FilterType = (EchoFilterType)sel;
            band.UpdateCoefficients(48000.0);
            _curveControl.SetBands(_bands, idx);
            UpdateHeadroomStatusDisplay();
            NotifySingleBandChanged(idx, band);
        }
    }

    private void OnInspectorBandEnabledChanged(bool enabled)
    {
        if (_isUpdatingUi || _curveControl == null) return;
        int idx = _curveControl.SelectedBandIndex;
        if (idx < 0 || idx >= _bands.Count) return;

        PushUndoSnapshot();
        var band = _bands[idx];
        band.Enabled = enabled;
        band.UpdateCoefficients(48000.0);
        _curveControl.SetBands(_bands, idx);
        RefreshBandPills();
        UpdateHeadroomStatusDisplay();
        NotifySingleBandChanged(idx, band);
    }

    private void OnPreampSliderChanged(object sender, RangeBaseValueChangedEventArgs e)
    {
        if (_isUpdatingUi) return;
        _preampDb = Math.Round(e.NewValue, 1);
        if (_preampValueTb != null) _preampValueTb.Text = $"{_preampDb:+0.0;-0.0;0.0} dB";
        if (_curveControl != null) _curveControl.PreampDb = _preampDb;
        UpdateHeadroomStatusDisplay();
        PreampChanged?.Invoke(_preampDb);
    }

    private void OnGraphicFaderChanged(int bandIndex, double newValue)
    {
        if (_isUpdatingUi || bandIndex < 0 || bandIndex >= _bands.Count) return;

        double rounded = Math.Round(newValue, 1);
        if (_graphicGainLabels[bandIndex] != null)
        {
            _graphicGainLabels[bandIndex].Text = $"{rounded:+0.0;-0.0;0.0}";
        }

        var band = _bands[bandIndex];
        band.GainDb = rounded;
        band.Enabled = true;
        band.UpdateCoefficients(48000.0);

        _curveControl?.SetBands(_bands, bandIndex);
        UpdateSelectedBandInspector(bandIndex);
        RefreshBandPills();
        UpdateHeadroomStatusDisplay();
        NotifySingleBandChanged(bandIndex, band);
    }

    private void ResetCurrentBand()
    {
        if (_curveControl == null) return;
        int idx = _curveControl.SelectedBandIndex;
        if (idx < 0 || idx >= _bands.Count) return;

        PushUndoSnapshot();
        var band = _bands[idx];
        if (idx < MixAssistantEngine.GraphicFrequencies.Length)
        {
            band.FrequencyHz = MixAssistantEngine.GraphicFrequencies[idx];
        }
        band.GainDb = 0.0;
        band.Q = 1.414;
        band.Enabled = true;
        band.UpdateCoefficients(48000.0);

        SyncAllViewsFromBands(idx);
        NotifySingleBandChanged(idx, band);
    }

    private void ZeroOrDeleteCurrentBand()
    {
        if (_curveControl == null) return;
        int idx = _curveControl.SelectedBandIndex;
        if (idx < 0 || idx >= _bands.Count) return;

        PushUndoSnapshot();
        if (_bands.Count > 10 && idx >= 10)
        {
            _bands.RemoveAt(idx);
            int nextIdx = Math.Clamp(idx - 1, 0, _bands.Count - 1);
            SyncAllViewsFromBands(nextIdx);
            NotifyAllBandsChanged();
            return;
        }

        var band = _bands[idx];
        band.GainDb = 0.0;
        band.UpdateCoefficients(48000.0);
        SyncAllViewsFromBands(idx);
        NotifySingleBandChanged(idx, band);
    }

    private void AddNewBand()
    {
        PushUndoSnapshot();
        double nextFreq = 1000.0;
        if (_bands.Count > 0)
        {
            var last = _bands[^1];
            nextFreq = Math.Clamp(last.FrequencyHz * 0.72, 20.0, 20000.0);
        }

        var newBand = new EchoBiquadFilter
        {
            FilterType = EchoFilterType.Peaking,
            FrequencyHz = Math.Round(nextFreq, 1),
            GainDb = 0.0,
            Q = 1.414,
            Enabled = true
        };
        newBand.UpdateCoefficients(48000.0);
        _bands.Add(newBand);

        int newIdx = _bands.Count - 1;
        SyncAllViewsFromBands(newIdx);
        NotifyAllBandsChanged();
    }

    private void OnPresetComboSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isUpdatingUi || _presetCombo == null) return;
        int idx = _presetCombo.SelectedIndex;
        if (idx < 0 || idx >= _builtinPresets.Count) return;

        PushUndoSnapshot();
        double[] gains = _builtinPresets[idx].Gains;
        for (int i = 0; i < 10 && i < _bands.Count; i++)
        {
            _bands[i].FrequencyHz = MixAssistantEngine.GraphicFrequencies[i];
            _bands[i].FilterType = i == 0
                ? EchoFilterType.LowShelf
                : (i == 9 ? EchoFilterType.HighShelf : EchoFilterType.Peaking);
            _bands[i].GainDb = i < gains.Length ? gains[i] : 0.0;
            _bands[i].Q = i == 0 || i == 9 ? 0.71 : 1.414;
            _bands[i].Enabled = true;
            _bands[i].UpdateCoefficients(48000.0);
        }

        SyncAllViewsFromBands(_curveControl?.SelectedBandIndex ?? 0);
        NotifyAllBandsChanged();
    }

    private void OnSavePresetClick(object sender, RoutedEventArgs e)
    {
        string name = _presetNameTb?.Text?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(name))
        {
            name = $"自定义预设 {_builtinPresets.Count + 1}";
        }

        double[] gains = GetGraphic10BandGains();
        _builtinPresets.Add((name, gains));
        _presetCombo.Items.Add(new ComboBoxItem { Content = name });
        _isUpdatingUi = true;
        try
        {
            _presetCombo.SelectedIndex = _builtinPresets.Count - 1;
            if (_presetNameTb != null) _presetNameTb.Text = string.Empty;
        }
        finally
        {
            _isUpdatingUi = false;
        }

        SaveUserPresetRequested?.Invoke(name, _bands, _preampDb);
    }

    private void OnLearnButtonClick(object sender, RoutedEventArgs e)
    {
        if (!_isLearning)
        {
            _mixAssistant.BeginLearning();
            SetMixAssistantProgress(true, 0.0, "正在监听音频流，请播放音乐...");
            MixAssistantLearnRequested?.Invoke(true);
        }
        else
        {
            var profile = _mixAssistant.EndLearning();
            SetMixAssistantProfile(profile, autoApply: true);
            MixAssistantLearnRequested?.Invoke(false);
        }
    }

    private void ResolveCurrentProfileLive()
    {
        if (_currentProfile == null)
        {
            // Synthesize a neutral profile if user drags target/sculpt before learning so preview still responds
            var neutral = new MixAssistantProfile(
                Bins32Db: new double[MixAssistantEngine.AnalysisBinCount],
                Bands10Db: new double[MixAssistantEngine.GraphicBandCount],
                MidbandRefDb: -24.0,
                CrestFactorDb: 12.0,
                SpectralTiltDbPerOct: 0.0,
                AnalyzedFrames: 0,
                Confidence01: 0.5);
            _currentSolution = _mixAssistant.SolveEqCurve(
                neutral,
                _selectedTargetId,
                _matchAmount01,
                _bassSculptDb,
                _trebleSculptDb,
                _demaskMidMud);
            return;
        }

        _currentSolution = _mixAssistant.SolveEqCurve(
            _currentProfile,
            _selectedTargetId,
            _matchAmount01,
            _bassSculptDb,
            _trebleSculptDb,
            _demaskMidMud);
        ApplyMixAssistantSolution(_currentSolution);
    }

    private void OnApplyAssistantClick()
    {
        if (_currentSolution == null)
        {
            ResolveCurrentProfileLive();
        }
        if (_currentSolution != null)
        {
            ApplyMixAssistantSolution(_currentSolution);
        }
    }

    private void ResetMixAssistantAndEq()
    {
        _isUpdatingUi = true;
        try
        {
            _selectedTargetId = "balanced_master";
            _matchAmount01 = 0.75;
            _bassSculptDb = 0.0;
            _trebleSculptDb = 0.0;
            _demaskMidMud = true;
            if (_targetCombo != null) _targetCombo.SelectedIndex = 0;
            if (_amountSlider != null) _amountSlider.Value = 75;
            if (_amountValueTb != null) _amountValueTb.Text = "75%";
            if (_bassSculptSlider != null) _bassSculptSlider.Value = 0.0;
            if (_bassSculptValueTb != null) _bassSculptValueTb.Text = "0.0 dB";
            if (_trebleSculptSlider != null) _trebleSculptSlider.Value = 0.0;
            if (_trebleSculptValueTb != null) _trebleSculptValueTb.Text = "0.0 dB";
            if (_demaskSwitch != null) _demaskSwitch.IsOn = true;
        }
        finally
        {
            _isUpdatingUi = false;
        }
        ResetToFlat();
    }

    private void ToggleAbCompare()
    {
        _isAbOriginal = !_isAbOriginal;
        UpdateAbButtonVisuals();
        AbCompareToggled?.Invoke(_isAbOriginal);
        EnabledChanged?.Invoke(_isAbOriginal ? false : _isEqEnabled);
    }

    private void AutoFitCurveScale()
    {
        double maxAbs = Math.Abs(_preampDb);
        foreach (var b in _bands)
        {
            if (b.Enabled)
            {
                maxAbs = Math.Max(maxAbs, Math.Abs(b.GainDb + _preampDb));
            }
        }
        double nextRange = maxAbs <= 5.5 ? 6.0 : (maxAbs <= 11.5 ? 12.0 : (maxAbs <= 17.5 ? 18.0 : (maxAbs <= 23.5 ? 24.0 : 30.0)));
        _curveControl.DbRange = nextRange;
        _tbScale.Text = $"±{nextRange:F0} dB";
    }

    private void UpdateHeadroomStatusDisplay()
    {
        double peakDb = double.NegativeInfinity;
        for (int i = 0; i < MixAssistantEngine.Curve64Frequencies.Length; i++)
        {
            double f = MixAssistantEngine.Curve64Frequencies[i];
            double total = _preampDb;
            for (int b = 0; b < _bands.Count; b++)
            {
                if (_bands[b].Enabled)
                {
                    total += _bands[b].CalculateResponseDb(f, 48000.0);
                }
            }
            if (total > peakDb) peakDb = total;
        }

        if (!double.IsFinite(peakDb)) peakDb = 0.0;
        double margin = -peakDb;

        if (_headroomStatusTb != null)
        {
            bool safe = margin >= -0.2;
            _headroomStatusTb.Text = safe
                ? $"● 安全余量 {margin:+0.0;-0.0;0.0} dB"
                : $"▲ 峰值超限 {peakDb:+0.0;-0.0;0.0} dB (建议降低前级)";
            _headroomStatusTb.Foreground = new SolidColorBrush(safe
                ? Color.FromArgb(255, 22, 163, 74)
                : Color.FromArgb(255, 234, 88, 12));
        }

        if (_compositeHintTb != null)
        {
            _compositeHintTb.Text = $"合成峰值 {peakDb:+0.0;-0.0;0.0} dB";
        }
        if (_beforeStateTb != null)
        {
            _beforeStateTb.Text = $"前级 {_preampDb:+0.0;-0.0;0.0} dB";
        }
        if (_afterStateTb != null)
        {
            _afterStateTb.Text = _isAbOriginal ? "当前对比: A (原始直通)" : "当前对比: B (EQ 处理后)";
        }
    }

    private void PushUndoSnapshot()
    {
        var snap = CloneBandsList(_bands);
        _undoStack.Push((snap, _preampDb));
        while (_undoStack.Count > MaxUndoDepth)
        {
            // Keep stack bounded
            var items = _undoStack.ToArray();
            _undoStack.Clear();
            for (int i = MaxUndoDepth - 1; i >= 0; i--)
            {
                _undoStack.Push(items[i]);
            }
            break;
        }
        _redoStack.Clear();
        UpdateUndoRedoButtonState();
    }

    private void Undo()
    {
        if (_undoStack.Count == 0) return;
        _redoStack.Push((CloneBandsList(_bands), _preampDb));
        var prev = _undoStack.Pop();
        SetBands(prev.Bands, prev.PreampDb, _isEqEnabled, _curveControl?.SelectedBandIndex ?? 0);
        UpdateUndoRedoButtonState();
        NotifyAllBandsChanged();
        PreampChanged?.Invoke(_preampDb);
    }

    private void Redo()
    {
        if (_redoStack.Count == 0) return;
        _undoStack.Push((CloneBandsList(_bands), _preampDb));
        var next = _redoStack.Pop();
        SetBands(next.Bands, next.PreampDb, _isEqEnabled, _curveControl?.SelectedBandIndex ?? 0);
        UpdateUndoRedoButtonState();
        NotifyAllBandsChanged();
        PreampChanged?.Invoke(_preampDb);
    }

    private void UpdateUndoRedoButtonState()
    {
        if (_btnUndo != null) _btnUndo.IsEnabled = _undoStack.Count > 0;
        if (_btnRedo != null) _btnRedo.IsEnabled = _redoStack.Count > 0;
    }

    private void NotifySingleBandChanged(int idx, EchoBiquadFilter band)
    {
        BandChanged?.Invoke(idx, band);
        BandsChanged?.Invoke(_bands);
    }

    private void NotifyAllBandsChanged()
    {
        for (int i = 0; i < _bands.Count; i++)
        {
            BandChanged?.Invoke(i, _bands[i]);
        }
        BandsChanged?.Invoke(_bands);
    }

    private static List<EchoBiquadFilter> CloneBandsList(IReadOnlyList<EchoBiquadFilter> source)
    {
        var list = new List<EchoBiquadFilter>(source.Count);
        for (int i = 0; i < source.Count; i++)
        {
            var s = source[i];
            var copy = new EchoBiquadFilter
            {
                FilterType = s.FilterType,
                FrequencyHz = s.FrequencyHz,
                GainDb = s.GainDb,
                Q = s.Q,
                Enabled = s.Enabled
            };
            copy.UpdateCoefficients(48000.0);
            list.Add(copy);
        }
        return list;
    }

    private static double SnapToNearestFrequency(double freq)
    {
        double logF = Math.Log10(Math.Clamp(freq, 20.0, 20000.0));
        double bestFreq = freq;
        double bestDist = double.MaxValue;
        for (int i = 0; i < StandardSnapFrequencies.Length; i++)
        {
            double d = Math.Abs(logF - Math.Log10(StandardSnapFrequencies[i]));
            if (d < bestDist)
            {
                bestDist = d;
                bestFreq = StandardSnapFrequencies[i];
            }
        }
        return bestFreq;
    }

    private void RefreshModePillVisuals()
    {
        if (_btnModeParametric == null || _btnModeGraphic == null) return;
        Brush activeBg = new SolidColorBrush(_isDark
            ? Color.FromArgb(225, 79, 70, 229)
            : Color.FromArgb(255, 255, 255, 255));
        Brush activeFg = _isDark
            ? new SolidColorBrush(Colors.White)
            : new SolidColorBrush(Color.FromArgb(255, 15, 23, 42));
        Brush inactiveBg = new SolidColorBrush(Colors.Transparent);
        Brush inactiveFg = GetSecondaryBrush();

        _btnModeParametric.Background = !_isGraphicMode ? activeBg : inactiveBg;
        _btnModeParametric.Foreground = !_isGraphicMode ? activeFg : inactiveFg;
        _btnModeParametric.FontWeight = !_isGraphicMode ? FontWeights.SemiBold : FontWeights.Medium;

        _btnModeGraphic.Background = _isGraphicMode ? activeBg : inactiveBg;
        _btnModeGraphic.Foreground = _isGraphicMode ? activeFg : inactiveFg;
        _btnModeGraphic.FontWeight = _isGraphicMode ? FontWeights.SemiBold : FontWeights.Medium;
    }

    private void RefreshScopeButtonVisuals()
    {
        Brush secondary = GetSecondaryBrush();
        for (int i = 0; i < _scopeButtons.Count; i++)
        {
            bool sel = i == _selectedScopeIndex;
            var btn = _scopeButtons[i];
            btn.Background = sel
                ? new SolidColorBrush(Color.FromArgb(255, 79, 70, 229))
                : CreateSurfaceBrush(LiquidGlassVariant.Clear, 0.78);
            btn.BorderBrush = sel
                ? new SolidColorBrush(Color.FromArgb(255, 99, 102, 241))
                : CreateRimBrush(false);
            btn.Foreground = sel ? new SolidColorBrush(Colors.White) : secondary;
            btn.FontWeight = sel ? FontWeights.SemiBold : FontWeights.Normal;
        }
    }

    private void RefreshMixAssistantButtonVisuals()
    {
        if (_btnLearn == null || _btnLearnTextTb == null) return;
        if (_isLearning)
        {
            _btnLearn.Background = new SolidColorBrush(Color.FromArgb(255, 225, 29, 72));
            _btnLearn.BorderBrush = new SolidColorBrush(Color.FromArgb(255, 244, 63, 94));
            _btnLearnTextTb.Foreground = new SolidColorBrush(Colors.White);
        }
        else
        {
            _btnLearn.Background = new SolidColorBrush(_isDark
                ? Color.FromArgb(80, 14, 165, 233)
                : Color.FromArgb(230, 224, 242, 254));
            _btnLearn.BorderBrush = new SolidColorBrush(Color.FromArgb(200, 14, 165, 233));
            _btnLearnTextTb.Foreground = new SolidColorBrush(_isDark
                ? Color.FromArgb(255, 186, 230, 253)
                : Color.FromArgb(255, 3, 105, 161));
        }
    }

    private void UpdateSpectrumButtonVisuals()
    {
        if (_btnSpectrum == null || _curveControl == null) return;
        bool active = _curveControl.ShowSpectrum;
        _btnSpectrum.Background = active
            ? new SolidColorBrush(_isDark ? Color.FromArgb(80, 79, 70, 229) : Color.FromArgb(235, 238, 242, 255))
            : CreateSurfaceBrush(LiquidGlassVariant.Clear, 0.78);
        _btnSpectrum.BorderBrush = active
            ? new SolidColorBrush(Color.FromArgb(255, 99, 102, 241))
            : CreateRimBrush(false);
        _btnSpectrum.Foreground = active
            ? new SolidColorBrush(_isDark ? Color.FromArgb(255, 199, 210, 254) : Color.FromArgb(255, 79, 70, 229))
            : GetSecondaryBrush();
    }

    private void UpdateAbButtonVisuals()
    {
        void StyleAbBtn(Button? btn)
        {
            if (btn == null) return;
            btn.Background = _isAbOriginal
                ? new SolidColorBrush(_isDark ? Color.FromArgb(85, 245, 158, 11) : Color.FromArgb(235, 254, 243, 199))
                : CreateSurfaceBrush(LiquidGlassVariant.Clear, 0.78);
            btn.BorderBrush = _isAbOriginal
                ? new SolidColorBrush(Color.FromArgb(255, 245, 158, 11))
                : CreateRimBrush(false);
            btn.Foreground = _isAbOriginal
                ? new SolidColorBrush(_isDark ? Color.FromArgb(255, 253, 230, 138) : Color.FromArgb(255, 180, 83, 9))
                : GetSecondaryBrush();
            btn.Content = _isAbOriginal ? "⇄ A: 原始 (点击切回 B)" : "⇄ A/B 对比";
        }
        StyleAbBtn(_btnAbTop);
        StyleAbBtn(_btnAssistantAb);

        if (_btnLoudnessMatch != null)
        {
            _btnLoudnessMatch.Background = _isLoudnessMatched
                ? new SolidColorBrush(_isDark ? Color.FromArgb(80, 79, 70, 229) : Color.FromArgb(235, 238, 242, 255))
                : CreateSurfaceBrush(LiquidGlassVariant.Clear, 0.78);
            _btnLoudnessMatch.BorderBrush = _isLoudnessMatched
                ? new SolidColorBrush(Color.FromArgb(255, 99, 102, 241))
                : CreateRimBrush(false);
            _btnLoudnessMatch.Foreground = _isLoudnessMatched
                ? new SolidColorBrush(_isDark ? Color.FromArgb(255, 199, 210, 254) : Color.FromArgb(255, 79, 70, 229))
                : GetSecondaryBrush();
        }
        UpdateHeadroomStatusDisplay();
    }

    private void ApplyCardSurface(Border? card, double opacityScale)
    {
        if (card == null) return;
        card.Background = CreateSurfaceBrush(LiquidGlassVariant.Regular, opacityScale);
        card.BorderBrush = CreateRimBrush(false);
    }

    private Brush CreateSurfaceBrush(LiquidGlassVariant variant, double opacityScale) =>
        LiquidGlassEngine.CreateSurfaceBrush(variant, _isDark, opacityScale);

    private Brush CreateRimBrush(bool isSelected) =>
        LiquidGlassEngine.CreateSpecularRimBrush(LiquidGlassVariant.Regular, _isDark, isHovered: false, isSelected: isSelected);

    private Brush CreateSoftSurfaceBrush() =>
        _isDark
            ? CreateSurfaceBrush(LiquidGlassVariant.Clear, 0.40)
            : new SolidColorBrush(Color.FromArgb(200, 241, 245, 249));

    private Brush GetPrimaryBrush() => new SolidColorBrush(_isDark
        ? Colors.White
        : Color.FromArgb(255, 15, 23, 42));

    private Brush GetSecondaryBrush() => new SolidColorBrush(_isDark
        ? Color.FromArgb(255, 241, 245, 249)
        : Color.FromArgb(255, 51, 65, 85));

    private Brush GetSubHeaderBrush() => new SolidColorBrush(_isDark
        ? Color.FromArgb(255, 203, 213, 225)
        : Color.FromArgb(255, 71, 85, 105));

    private Brush GetMutedBrush() => new SolidColorBrush(_isDark
        ? Color.FromArgb(255, 148, 163, 184)
        : Color.FromArgb(255, 100, 116, 139));

    private TextBlock TrackPrimaryText(TextBlock tb)
    {
        _primaryTextElements.Add(tb);
        return tb;
    }

    private TextBlock TrackSecondaryText(TextBlock tb)
    {
        _secondaryTextElements.Add(tb);
        return tb;
    }

    private TextBlock TrackSubHeaderText(TextBlock tb)
    {
        _subHeaderTextElements.Add(tb);
        return tb;
    }

    private TextBlock TrackMutedText(TextBlock tb)
    {
        _mutedTextElements.Add(tb);
        return tb;
    }

    private Button CreateGlassToolButton(object content, Thickness padding)
    {
        var btn = new Button
        {
            Content = content,
            Padding = padding,
            CornerRadius = new CornerRadius(8),
            BorderThickness = new Thickness(1)
        };
        _glassToolButtons.Add(btn);
        return btn;
    }
}
