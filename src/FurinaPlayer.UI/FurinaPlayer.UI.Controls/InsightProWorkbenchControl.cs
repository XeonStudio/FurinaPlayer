using System;
using System.Collections.Generic;
using Microsoft.UI;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using SkiaSharp;
using SkiaSharp.Views.Windows;
using SonicWave.Audio.Dsp;
using Windows.UI;

namespace FurinaPlayer.UI.Controls;

/// <summary>
/// Native WinUI 3 + SkiaSharp iZotope Insight Pro metering &amp; spectral workbench control.
/// Provides Loudness (ITU-R BS.1770-4 + Targets), Levels (Peak/RMS), Sound Field (Polar/Lissajous + Correlation),
/// Spectrum (128-band log FFT + Peak Hold + Hover Crosshair), 3D Isometric Spectrogram (5 camera perspectives),
/// and scrolling Loudness History.
/// </summary>
public sealed class InsightProWorkbenchControl : UserControl
{
    private enum SoundFieldMode
    {
        PolarSample,
        PolarLevel,
        Lissajous
    }

    private enum SpectrogramPerspective
    {
        FrontHigh,
        FrontLow,
        Diagonal,
        SideLow,
        SideHigh
    }

    // Insight DAW surface palette (high contrast in both Light & Dark host themes)
    private static readonly SKColor BgPanelSk = new(22, 26, 32);        // #161A20
    private static readonly SKColor GridLineSk = new(44, 52, 64, 175);  // #2C3440
    private static readonly SKColor AxisTextSk = new(148, 163, 184);    // #94A3B8
    private static readonly SKColor LabelTextSk = new(203, 213, 225);   // #CBD5E1
    private static readonly SKColor GreenAccentSk = new(63, 191, 111);  // #3FBF6F
    private static readonly SKColor AmberWarnSk = new(245, 166, 35);    // #F5A623
    private static readonly SKColor RedOverSk = new(255, 68, 97);       // #FF4461
    private static readonly SKColor ShortTermSk = new(235, 235, 235);   // #EBEBEB
    private static readonly SKColor MomentarySk = new(86, 96, 104);     // #566068

    private static readonly Color OuterDarkBg = Color.FromArgb(255, 18, 21, 26);
    private static readonly Color OuterLightFrameBg = Color.FromArgb(255, 22, 26, 32);
    private static readonly Color CardBg = Color.FromArgb(255, 22, 26, 32);
    private static readonly Color CardBorderDark = Color.FromArgb(255, 42, 49, 60);
    private static readonly Color CardBorderLightHost = Color.FromArgb(255, 64, 74, 90);
    private static readonly Color ActivePillBg = Color.FromArgb(36, 63, 191, 111);
    private static readonly Color ActivePillBorder = Color.FromArgb(255, 63, 191, 111);
    private static readonly Color InactivePillBg = Color.FromArgb(255, 28, 33, 41);
    private static readonly Color InactivePillBorder = Color.FromArgb(255, 52, 60, 74);

    // State
    private InsightProSnapshot? _snapshot;
    private bool _isHostDark = true;
    private bool _isCompactLayout;
    private bool _suppressTargetEvent;

    private bool _showLoudness = true;
    private bool _showLevels = true;
    private bool _showSoundField = true;
    private bool _showHistory = true;
    private bool _showSpectrum = true;
    private bool _showSpectrogram = true;

    private SoundFieldMode _soundFieldMode = SoundFieldMode.PolarSample;
    private SpectrogramPerspective _spectrogramPerspective = SpectrogramPerspective.Diagonal;

    // Spectrum hover state
    private float? _spectrumHoverX;
    private float? _spectrumHoverY;

    // Root & Layout Containers
    private readonly Border _outerShell;
    private readonly Grid _rootGrid;
    private readonly Grid _contentHost;

    // Wide layout containers
    private readonly Grid _wideGrid;
    private readonly ColumnDefinition _wideLeftCol;
    private readonly ColumnDefinition _wideRightCol;
    private readonly RowDefinition _wideTopRow;
    private readonly RowDefinition _wideBottomRow;
    private readonly Grid _wideLeftGrid;
    private readonly RowDefinition _leftRowLoudness;
    private readonly RowDefinition _leftRowLevels;
    private readonly RowDefinition _leftRowSoundField;
    private readonly Grid _wideRightGrid;
    private readonly RowDefinition _rightRowSpectrum;
    private readonly RowDefinition _rightRowSpectrogram;

    // Compact layout containers
    private readonly ScrollViewer _compactScroll;
    private readonly StackPanel _compactStack;

    // Toolbar Toggle Buttons
    private readonly Button _toggleLoudnessBtn;
    private readonly Button _toggleLevelsBtn;
    private readonly Button _toggleSoundFieldBtn;
    private readonly Button _toggleHistoryBtn;
    private readonly Button _toggleSpectrumBtn;
    private readonly Button _toggleSpectrogramBtn;
    private readonly Button _resetBtn;

    // Module Cards
    private readonly Border _loudnessCard;
    private readonly Border _levelsCard;
    private readonly Border _soundFieldCard;
    private readonly Border _spectrumCard;
    private readonly Border _spectrogramCard;
    private readonly Border _historyCard;
    private readonly Border _emptyFallbackBanner;

    // Skia Canvases
    private readonly SKXamlCanvas _loudnessCanvas;
    private readonly SKXamlCanvas _levelsCanvas;
    private readonly SKXamlCanvas _soundFieldCanvas;
    private readonly SKXamlCanvas _spectrumCanvas;
    private readonly SKXamlCanvas _spectrogramCanvas;
    private readonly SKXamlCanvas _historyCanvas;

    // Interactive Controls inside Cards
    private readonly ComboBox _targetsCombo;
    private readonly Dictionary<SoundFieldMode, Button> _soundFieldButtons = new();
    private readonly Dictionary<SpectrogramPerspective, Button> _perspectiveButtons = new();

    public event Action? ResetRequested;
    public event Action<InsightTargetPreset>? TargetChanged;

    public InsightProWorkbenchControl()
    {
        _outerShell = new Border
        {
            Background = new SolidColorBrush(OuterDarkBg),
            BorderBrush = new SolidColorBrush(CardBorderDark),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(10),
            Padding = new Thickness(8),
            RequestedTheme = ElementTheme.Dark
        };

        _rootGrid = new Grid { RowSpacing = 8 };
        _rootGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        _rootGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

        // 1. Top Toolbar
        var toolbarGrid = new Grid { ColumnSpacing = 8 };
        toolbarGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        toolbarGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        var pillsScroll = new ScrollViewer
        {
            HorizontalScrollBarVisibility = ScrollBarVisibility.Hidden,
            VerticalScrollBarVisibility = ScrollBarVisibility.Disabled,
            HorizontalAlignment = HorizontalAlignment.Center
        };
        var pillsPanel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 4,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };

        _toggleLoudnessBtn = CreateModuleTogglePill("\uE767", "Loudness", () => { _showLoudness = !_showLoudness; ApplyModuleVisibilityAndLayout(); });
        _toggleLevelsBtn = CreateModuleTogglePill("\uE9D9", "Levels", () => { _showLevels = !_showLevels; ApplyModuleVisibilityAndLayout(); });
        _toggleSoundFieldBtn = CreateModuleTogglePill("\uE967", "Sound Field", () => { _showSoundField = !_showSoundField; ApplyModuleVisibilityAndLayout(); });
        _toggleHistoryBtn = CreateModuleTogglePill("\uE81C", "History", () => { _showHistory = !_showHistory; ApplyModuleVisibilityAndLayout(); });
        _toggleSpectrumBtn = CreateModuleTogglePill("\uE9D2", "Spectrum", () => { _showSpectrum = !_showSpectrum; ApplyModuleVisibilityAndLayout(); });
        _toggleSpectrogramBtn = CreateModuleTogglePill("\uE81E", "Spectrogram", () => { _showSpectrogram = !_showSpectrogram; ApplyModuleVisibilityAndLayout(); });

        pillsPanel.Children.Add(_toggleLoudnessBtn);
        pillsPanel.Children.Add(_toggleLevelsBtn);
        pillsPanel.Children.Add(_toggleSoundFieldBtn);
        pillsPanel.Children.Add(_toggleHistoryBtn);
        pillsPanel.Children.Add(_toggleSpectrumBtn);
        pillsPanel.Children.Add(_toggleSpectrogramBtn);
        pillsScroll.Content = pillsPanel;
        Grid.SetColumn(pillsScroll, 0);
        toolbarGrid.Children.Add(pillsScroll);

        _resetBtn = CreateActionButton("\uE72C", "重置计量", () => ResetRequested?.Invoke());
        Grid.SetColumn(_resetBtn, 1);
        toolbarGrid.Children.Add(_resetBtn);

        Grid.SetRow(toolbarGrid, 0);
        _rootGrid.Children.Add(toolbarGrid);

        // 2. Build 6 Module Cards
        _loudnessCanvas = new SKXamlCanvas();
        _loudnessCanvas.PaintSurface += OnPaintLoudness;
        _targetsCombo = BuildTargetsComboBox();
        _loudnessCard = BuildLoudnessCard(_loudnessCanvas, _targetsCombo);

        _levelsCanvas = new SKXamlCanvas();
        _levelsCanvas.PaintSurface += OnPaintLevels;
        _levelsCard = BuildSimpleModuleCard("Levels", _levelsCanvas);

        _soundFieldCanvas = new SKXamlCanvas();
        _soundFieldCanvas.PaintSurface += OnPaintSoundField;
        _soundFieldCard = BuildSoundFieldCard(_soundFieldCanvas);

        _spectrumCanvas = new SKXamlCanvas();
        _spectrumCanvas.PaintSurface += OnPaintSpectrum;
        _spectrumCanvas.PointerMoved += OnSpectrumPointerMoved;
        _spectrumCanvas.PointerExited += OnSpectrumPointerExited;
        _spectrumCard = BuildSimpleModuleCard("Spectrum", _spectrumCanvas);

        _spectrogramCanvas = new SKXamlCanvas();
        _spectrogramCanvas.PaintSurface += OnPaintSpectrogram;
        _spectrogramCard = BuildSpectrogramCard(_spectrogramCanvas);

        _historyCanvas = new SKXamlCanvas();
        _historyCanvas.PaintSurface += OnPaintHistory;
        _historyCard = BuildSimpleModuleCard("History", _historyCanvas);

        _emptyFallbackBanner = BuildEmptyFallbackBanner();

        // 3. Build Wide Grid
        _wideGrid = new Grid { RowSpacing = 6, ColumnSpacing = 6 };
        _wideTopRow = new RowDefinition { Height = new GridLength(2.55, GridUnitType.Star) };
        _wideBottomRow = new RowDefinition { Height = new GridLength(1.0, GridUnitType.Star) };
        _wideGrid.RowDefinitions.Add(_wideTopRow);
        _wideGrid.RowDefinitions.Add(_wideBottomRow);

        var upperSplit = new Grid { ColumnSpacing = 6 };
        _wideLeftCol = new ColumnDefinition { Width = new GridLength(0.95, GridUnitType.Star) };
        _wideRightCol = new ColumnDefinition { Width = new GridLength(1.05, GridUnitType.Star) };
        upperSplit.ColumnDefinitions.Add(_wideLeftCol);
        upperSplit.ColumnDefinitions.Add(_wideRightCol);

        _wideLeftGrid = new Grid { RowSpacing = 6 };
        _leftRowLoudness = new RowDefinition { Height = new GridLength(1.08, GridUnitType.Star) };
        _leftRowLevels = new RowDefinition { Height = new GridLength(0.92, GridUnitType.Star) };
        _leftRowSoundField = new RowDefinition { Height = new GridLength(1.38, GridUnitType.Star) };
        _wideLeftGrid.RowDefinitions.Add(_leftRowLoudness);
        _wideLeftGrid.RowDefinitions.Add(_leftRowLevels);
        _wideLeftGrid.RowDefinitions.Add(_leftRowSoundField);
        Grid.SetColumn(_wideLeftGrid, 0);
        upperSplit.Children.Add(_wideLeftGrid);

        _wideRightGrid = new Grid { RowSpacing = 6 };
        _rightRowSpectrum = new RowDefinition { Height = new GridLength(1.0, GridUnitType.Star) };
        _rightRowSpectrogram = new RowDefinition { Height = new GridLength(1.08, GridUnitType.Star) };
        _wideRightGrid.RowDefinitions.Add(_rightRowSpectrum);
        _wideRightGrid.RowDefinitions.Add(_rightRowSpectrogram);
        Grid.SetColumn(_wideRightGrid, 1);
        upperSplit.Children.Add(_wideRightGrid);

        Grid.SetRow(upperSplit, 0);
        _wideGrid.Children.Add(upperSplit);

        // 4. Build Compact ScrollView
        _compactStack = new StackPanel { Spacing = 8, Padding = new Thickness(0, 0, 4, 0) };
        _compactScroll = new ScrollViewer
        {
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
            Content = _compactStack,
            Visibility = Visibility.Collapsed
        };

        _contentHost = new Grid();
        _contentHost.Children.Add(_wideGrid);
        _contentHost.Children.Add(_compactScroll);
        _contentHost.Children.Add(_emptyFallbackBanner);
        Grid.SetRow(_contentHost, 1);
        _rootGrid.Children.Add(_contentHost);

        _outerShell.Child = _rootGrid;
        Content = _outerShell;

        SizeChanged += OnWorkbenchSizeChanged;

        // Initial placement into wide layout
        PlaceCardsInWideGrid();
        UpdateTogglePillVisuals();
        UpdateSoundFieldButtons();
        UpdatePerspectiveButtons();
        ApplyModuleVisibilityAndLayout();
    }

    public void UpdateSnapshot(InsightProSnapshot snapshot)
    {
        if (snapshot == null) return;
        _snapshot = snapshot;

        // Sync ComboBox selection without firing TargetChanged loop
        int targetIdx = PresetToComboIndex(snapshot.TargetPreset);
        if (_targetsCombo.SelectedIndex != targetIdx)
        {
            _suppressTargetEvent = true;
            _targetsCombo.SelectedIndex = targetIdx;
            _suppressTargetEvent = false;
        }

        InvalidateAllCanvases();
    }

    public void UpdateTheme(bool isDark)
    {
        _isHostDark = isDark;
        // Maintain Insight Pro dark precision DAW surface for crisp meter readability in both host themes,
        // while adapting outer shell rim/border to integrate cleanly with Light or Dark host pages.
        _outerShell.Background = new SolidColorBrush(isDark ? OuterDarkBg : OuterLightFrameBg);
        _outerShell.BorderBrush = new SolidColorBrush(isDark ? CardBorderDark : CardBorderLightHost);

        var cardBorderBrush = new SolidColorBrush(isDark ? CardBorderDark : Color.FromArgb(255, 56, 65, 80));
        _loudnessCard.BorderBrush = cardBorderBrush;
        _levelsCard.BorderBrush = cardBorderBrush;
        _soundFieldCard.BorderBrush = cardBorderBrush;
        _spectrumCard.BorderBrush = cardBorderBrush;
        _spectrogramCard.BorderBrush = cardBorderBrush;
        _historyCard.BorderBrush = cardBorderBrush;

        InvalidateAllCanvases();
    }

    private void InvalidateAllCanvases()
    {
        if (_showLoudness) _loudnessCanvas.Invalidate();
        if (_showLevels) _levelsCanvas.Invalidate();
        if (_showSoundField) _soundFieldCanvas.Invalidate();
        if (_showSpectrum) _spectrumCanvas.Invalidate();
        if (_showSpectrogram) _spectrogramCanvas.Invalidate();
        if (_showHistory) _historyCanvas.Invalidate();
    }

    private void OnWorkbenchSizeChanged(object sender, SizeChangedEventArgs e)
    {
        bool shouldCompact = e.NewSize.Width > 1 && e.NewSize.Width < 460.0;
        if (shouldCompact != _isCompactLayout)
        {
            _isCompactLayout = shouldCompact;
            SwitchLayoutContainer();
        }
    }

    private void SwitchLayoutContainer()
    {
        DetachAllCards();
        if (_isCompactLayout)
        {
            _wideGrid.Visibility = Visibility.Collapsed;
            _compactScroll.Visibility = Visibility.Visible;
            PlaceCardsInCompactStack();
        }
        else
        {
            _compactScroll.Visibility = Visibility.Collapsed;
            _wideGrid.Visibility = Visibility.Visible;
            PlaceCardsInWideGrid();
        }
        ApplyModuleVisibilityAndLayout();
    }

    private void DetachAllCards()
    {
        Border[] cards = { _loudnessCard, _levelsCard, _soundFieldCard, _spectrumCard, _spectrogramCard, _historyCard };
        foreach (var card in cards)
        {
            if (card.Parent is Panel panel)
            {
                panel.Children.Remove(card);
            }
        }
    }

    private void PlaceCardsInWideGrid()
    {
        // Reset explicit compact heights so Grid stars take over smoothly
        _loudnessCard.Height = double.NaN;
        _levelsCard.Height = double.NaN;
        _soundFieldCard.Height = double.NaN;
        _spectrumCard.Height = double.NaN;
        _spectrogramCard.Height = double.NaN;
        _historyCard.Height = double.NaN;

        _loudnessCard.MinHeight = 96;
        _levelsCard.MinHeight = 88;
        _soundFieldCard.MinHeight = 120;
        _spectrumCard.MinHeight = 135;
        _spectrogramCard.MinHeight = 145;
        _historyCard.MinHeight = 88;

        Grid.SetRow(_loudnessCard, 0);
        _wideLeftGrid.Children.Add(_loudnessCard);

        Grid.SetRow(_levelsCard, 1);
        _wideLeftGrid.Children.Add(_levelsCard);

        Grid.SetRow(_soundFieldCard, 2);
        _wideLeftGrid.Children.Add(_soundFieldCard);

        Grid.SetRow(_spectrumCard, 0);
        _wideRightGrid.Children.Add(_spectrumCard);

        Grid.SetRow(_spectrogramCard, 1);
        _wideRightGrid.Children.Add(_spectrogramCard);

        Grid.SetRow(_historyCard, 1);
        _wideGrid.Children.Add(_historyCard);
    }

    private void PlaceCardsInCompactStack()
    {
        _loudnessCard.Height = 165;
        _levelsCard.Height = 165;
        _soundFieldCard.Height = 235;
        _spectrumCard.Height = 225;
        _spectrogramCard.Height = 250;
        _historyCard.Height = 175;

        _compactStack.Children.Add(_loudnessCard);
        _compactStack.Children.Add(_levelsCard);
        _compactStack.Children.Add(_soundFieldCard);
        _compactStack.Children.Add(_spectrumCard);
        _compactStack.Children.Add(_spectrogramCard);
        _compactStack.Children.Add(_historyCard);
    }

    private void ApplyModuleVisibilityAndLayout()
    {
        _loudnessCard.Visibility = _showLoudness ? Visibility.Visible : Visibility.Collapsed;
        _levelsCard.Visibility = _showLevels ? Visibility.Visible : Visibility.Collapsed;
        _soundFieldCard.Visibility = _showSoundField ? Visibility.Visible : Visibility.Collapsed;
        _spectrumCard.Visibility = _showSpectrum ? Visibility.Visible : Visibility.Collapsed;
        _spectrogramCard.Visibility = _showSpectrogram ? Visibility.Visible : Visibility.Collapsed;
        _historyCard.Visibility = _showHistory ? Visibility.Visible : Visibility.Collapsed;

        bool anyVisible = _showLoudness || _showLevels || _showSoundField || _showSpectrum || _showSpectrogram || _showHistory;
        _emptyFallbackBanner.Visibility = anyVisible ? Visibility.Collapsed : Visibility.Visible;

        if (!_isCompactLayout)
        {
            _leftRowLoudness.Height = _showLoudness ? new GridLength(1.08, GridUnitType.Star) : new GridLength(0);
            _leftRowLevels.Height = _showLevels ? new GridLength(0.92, GridUnitType.Star) : new GridLength(0);
            _leftRowSoundField.Height = _showSoundField ? new GridLength(1.38, GridUnitType.Star) : new GridLength(0);

            _rightRowSpectrum.Height = _showSpectrum ? new GridLength(1.0, GridUnitType.Star) : new GridLength(0);
            _rightRowSpectrogram.Height = _showSpectrogram ? new GridLength(1.08, GridUnitType.Star) : new GridLength(0);

            bool anyLeft = _showLoudness || _showLevels || _showSoundField;
            bool anyRight = _showSpectrum || _showSpectrogram;
            bool anyTop = anyLeft || anyRight;

            _wideLeftCol.Width = anyLeft ? new GridLength(0.95, GridUnitType.Star) : new GridLength(0);
            _wideRightCol.Width = anyRight ? new GridLength(1.05, GridUnitType.Star) : new GridLength(0);
            _wideLeftGrid.Visibility = anyLeft ? Visibility.Visible : Visibility.Collapsed;
            _wideRightGrid.Visibility = anyRight ? Visibility.Visible : Visibility.Collapsed;

            _wideTopRow.Height = anyTop ? new GridLength(2.55, GridUnitType.Star) : new GridLength(0);
            _wideBottomRow.Height = _showHistory ? new GridLength(1.0, GridUnitType.Star) : new GridLength(0);
        }

        UpdateTogglePillVisuals();
        InvalidateAllCanvases();
    }

    private void UpdateTogglePillVisuals()
    {
        StyleTogglePill(_toggleLoudnessBtn, _showLoudness);
        StyleTogglePill(_toggleLevelsBtn, _showLevels);
        StyleTogglePill(_toggleSoundFieldBtn, _showSoundField);
        StyleTogglePill(_toggleHistoryBtn, _showHistory);
        StyleTogglePill(_toggleSpectrumBtn, _showSpectrum);
        StyleTogglePill(_toggleSpectrogramBtn, _showSpectrogram);
    }

    private static void StyleTogglePill(Button btn, bool active)
    {
        btn.Background = new SolidColorBrush(active ? ActivePillBg : InactivePillBg);
        btn.BorderBrush = new SolidColorBrush(active ? ActivePillBorder : InactivePillBorder);
        if (btn.Content is StackPanel sp)
        {
            var fg = new SolidColorBrush(active ? Color.FromArgb(255, 110, 231, 160) : Color.FromArgb(255, 148, 163, 184));
            foreach (var child in sp.Children)
            {
                if (child is FontIcon icon) icon.Foreground = fg;
                else if (child is TextBlock tb) tb.Foreground = fg;
            }
        }
    }

    private static Button CreateModuleTogglePill(string glyph, string label, Action onClick)
    {
        var sp = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 4,
            VerticalAlignment = VerticalAlignment.Center
        };
        sp.Children.Add(new FontIcon { Glyph = glyph, FontSize = 11 });
        sp.Children.Add(new TextBlock { Text = label, FontSize = 11, FontWeight = FontWeights.SemiBold, VerticalAlignment = VerticalAlignment.Center });

        var btn = new Button
        {
            Content = sp,
            Padding = new Thickness(8, 3, 8, 3),
            CornerRadius = new CornerRadius(5),
            BorderThickness = new Thickness(1),
            MinHeight = 26
        };
        btn.Click += (_, _) => onClick();
        return btn;
    }

    private static Button CreateActionButton(string glyph, string label, Action onClick)
    {
        var sp = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 5 };
        var fg = new SolidColorBrush(Color.FromArgb(255, 226, 232, 240));
        sp.Children.Add(new FontIcon { Glyph = glyph, FontSize = 12, Foreground = fg });
        sp.Children.Add(new TextBlock { Text = label, FontSize = 11.5, FontWeight = FontWeights.SemiBold, Foreground = fg, VerticalAlignment = VerticalAlignment.Center });

        var btn = new Button
        {
            Content = sp,
            Background = new SolidColorBrush(Color.FromArgb(255, 32, 38, 48)),
            BorderBrush = new SolidColorBrush(Color.FromArgb(255, 62, 72, 88)),
            BorderThickness = new Thickness(1),
            Padding = new Thickness(10, 5, 10, 5),
            CornerRadius = new CornerRadius(5),
            MinHeight = 28
        };
        btn.Click += (_, _) => onClick();
        return btn;
    }

    private Border CreateCardShell(string title, FrameworkElement body, FrameworkElement? footer = null, FrameworkElement? headerRight = null)
    {
        var grid = new Grid();
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        if (footer != null)
        {
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        }

        var header = new Grid
        {
            Padding = new Thickness(10, 5, 10, 3),
            Background = new SolidColorBrush(Color.FromArgb(255, 25, 30, 37))
        };
        header.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        header.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        header.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        var titleTb = new TextBlock
        {
            Text = title,
            FontSize = 11.5,
            FontWeight = FontWeights.SemiBold,
            Foreground = new SolidColorBrush(Color.FromArgb(255, 203, 213, 225)),
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };
        Grid.SetColumn(titleTb, 1);
        header.Children.Add(titleTb);

        if (headerRight != null)
        {
            Grid.SetColumn(headerRight, 2);
            header.Children.Add(headerRight);
        }

        Grid.SetRow(header, 0);
        grid.Children.Add(header);

        Grid.SetRow(body, 1);
        grid.Children.Add(body);

        if (footer != null)
        {
            Grid.SetRow(footer, 2);
            grid.Children.Add(footer);
        }

        return new Border
        {
            Background = new SolidColorBrush(CardBg),
            BorderBrush = new SolidColorBrush(CardBorderDark),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(5),
            Child = grid
        };
    }

    private Border BuildSimpleModuleCard(string title, SKXamlCanvas canvas)
    {
        return CreateCardShell(title, canvas);
    }

    private ComboBox BuildTargetsComboBox()
    {
        var combo = new ComboBox
        {
            MinWidth = 185,
            MinHeight = 24,
            FontSize = 10.5,
            Padding = new Thickness(8, 2, 8, 2),
            HorizontalAlignment = HorizontalAlignment.Center,
            Background = new SolidColorBrush(Color.FromArgb(255, 29, 34, 42)),
            BorderBrush = new SolidColorBrush(Color.FromArgb(255, 58, 68, 84)),
            Foreground = new SolidColorBrush(Color.FromArgb(255, 226, 232, 240)),
            RequestedTheme = ElementTheme.Dark
        };

        combo.Items.Add(new ComboBoxItem { Content = "Targets: Streaming (-14 LUFS)", Tag = InsightTargetPreset.StreamingMinus14 });
        combo.Items.Add(new ComboBoxItem { Content = "Targets: Apple (-16 LUFS)", Tag = InsightTargetPreset.AppleMinus16 });
        combo.Items.Add(new ComboBoxItem { Content = "Targets: EBU R128 (-23 LUFS)", Tag = InsightTargetPreset.EbuR128Minus23 });
        combo.Items.Add(new ComboBoxItem { Content = "Targets: ATSC A/85 (-24 LUFS)", Tag = InsightTargetPreset.AtscA85Minus24 });
        combo.Items.Add(new ComboBoxItem { Content = "Targets: CD / Loud (-9 LUFS)", Tag = InsightTargetPreset.CdLoudMinus9 });
        combo.Items.Add(new ComboBoxItem { Content = "Targets: Custom", Tag = InsightTargetPreset.Custom });
        combo.SelectedIndex = 0;

        combo.SelectionChanged += (_, _) =>
        {
            if (_suppressTargetEvent) return;
            if (combo.SelectedItem is ComboBoxItem item && item.Tag is InsightTargetPreset preset)
            {
                TargetChanged?.Invoke(preset);
            }
        };

        return combo;
    }

    private static int PresetToComboIndex(InsightTargetPreset preset) => preset switch
    {
        InsightTargetPreset.StreamingMinus14 => 0,
        InsightTargetPreset.AppleMinus16 => 1,
        InsightTargetPreset.EbuR128Minus23 => 2,
        InsightTargetPreset.AtscA85Minus24 => 3,
        InsightTargetPreset.CdLoudMinus9 => 4,
        _ => 5
    };

    private Border BuildLoudnessCard(SKXamlCanvas canvas, ComboBox targetsCombo)
    {
        var footer = new Border
        {
            Padding = new Thickness(6, 2, 6, 3),
            BorderBrush = new SolidColorBrush(Color.FromArgb(255, 36, 43, 53)),
            BorderThickness = new Thickness(0, 1, 0, 0),
            Child = targetsCombo
        };
        return CreateCardShell("Loudness", canvas, footer);
    }

    private Border BuildSoundFieldCard(SKXamlCanvas canvas)
    {
        var pillContainer = new Border
        {
            Background = new SolidColorBrush(Color.FromArgb(255, 18, 21, 26)),
            BorderBrush = new SolidColorBrush(Color.FromArgb(255, 48, 56, 68)),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(4),
            HorizontalAlignment = HorizontalAlignment.Center,
            Margin = new Thickness(8, 2, 8, 6),
            Padding = new Thickness(2)
        };

        var sp = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 2 };
        sp.Children.Add(CreateSoundFieldSegment("Polar Sample", SoundFieldMode.PolarSample));
        sp.Children.Add(CreateSoundFieldSegment("Polar Level", SoundFieldMode.PolarLevel));
        sp.Children.Add(CreateSoundFieldSegment("Lissajous", SoundFieldMode.Lissajous));
        pillContainer.Child = sp;

        return CreateCardShell("Sound Field", canvas, pillContainer);
    }

    private Button CreateSoundFieldSegment(string text, SoundFieldMode mode)
    {
        var btn = new Button
        {
            Content = text,
            FontSize = 10.5,
            Padding = new Thickness(12, 3, 12, 3),
            CornerRadius = new CornerRadius(3),
            BorderThickness = new Thickness(1),
            MinHeight = 22
        };
        btn.Click += (_, _) =>
        {
            _soundFieldMode = mode;
            UpdateSoundFieldButtons();
            _soundFieldCanvas.Invalidate();
        };
        _soundFieldButtons[mode] = btn;
        return btn;
    }

    private void UpdateSoundFieldButtons()
    {
        foreach (var (mode, btn) in _soundFieldButtons)
        {
            bool active = mode == _soundFieldMode;
            btn.Background = new SolidColorBrush(active ? ActivePillBg : Colors.Transparent);
            btn.BorderBrush = new SolidColorBrush(active ? ActivePillBorder : Colors.Transparent);
            btn.Foreground = new SolidColorBrush(active ? Color.FromArgb(255, 110, 231, 160) : Color.FromArgb(255, 160, 174, 192));
        }
    }

    private Border BuildSpectrogramCard(SKXamlCanvas canvas)
    {
        var bar = new Border
        {
            Background = new SolidColorBrush(Color.FromArgb(255, 18, 21, 26)),
            BorderBrush = new SolidColorBrush(Color.FromArgb(255, 48, 56, 68)),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(4),
            HorizontalAlignment = HorizontalAlignment.Center,
            Margin = new Thickness(6, 2, 6, 6),
            Padding = new Thickness(2)
        };

        var sp = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 2 };
        sp.Children.Add(CreatePerspectiveButton("Front High", SpectrogramPerspective.FrontHigh));
        sp.Children.Add(CreatePerspectiveButton("Front Low", SpectrogramPerspective.FrontLow));
        sp.Children.Add(CreatePerspectiveButton("Diagonal", SpectrogramPerspective.Diagonal));
        sp.Children.Add(CreatePerspectiveButton("Side Low", SpectrogramPerspective.SideLow));
        sp.Children.Add(CreatePerspectiveButton("Side High", SpectrogramPerspective.SideHigh));
        bar.Child = sp;

        return CreateCardShell("Spectrogram", canvas, bar);
    }

    private Button CreatePerspectiveButton(string text, SpectrogramPerspective perspective)
    {
        var btn = new Button
        {
            Content = text,
            FontSize = 10,
            Padding = new Thickness(8, 3, 8, 3),
            CornerRadius = new CornerRadius(3),
            BorderThickness = new Thickness(1),
            MinHeight = 22
        };
        btn.Click += (_, _) =>
        {
            _spectrogramPerspective = perspective;
            UpdatePerspectiveButtons();
            _spectrogramCanvas.Invalidate();
        };
        _perspectiveButtons[perspective] = btn;
        return btn;
    }

    private void UpdatePerspectiveButtons()
    {
        foreach (var (persp, btn) in _perspectiveButtons)
        {
            bool active = persp == _spectrogramPerspective;
            btn.Background = new SolidColorBrush(active ? Color.FromArgb(44, 245, 166, 35) : Colors.Transparent);
            btn.BorderBrush = new SolidColorBrush(active ? Color.FromArgb(255, 245, 166, 35) : Colors.Transparent);
            btn.Foreground = new SolidColorBrush(active ? Color.FromArgb(255, 253, 211, 128) : Color.FromArgb(255, 160, 174, 192));
        }
    }

    private Border BuildEmptyFallbackBanner()
    {
        var sp = new StackPanel
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            Spacing = 10
        };
        sp.Children.Add(new TextBlock
        {
            Text = "所有分析模块已隐藏",
            FontSize = 14,
            FontWeight = FontWeights.SemiBold,
            Foreground = new SolidColorBrush(Color.FromArgb(255, 203, 213, 225)),
            HorizontalAlignment = HorizontalAlignment.Center
        });
        var restoreBtn = CreateActionButton("\uE777", "恢复全部仪表", () =>
        {
            _showLoudness = _showLevels = _showSoundField = _showHistory = _showSpectrum = _showSpectrogram = true;
            ApplyModuleVisibilityAndLayout();
        });
        restoreBtn.HorizontalAlignment = HorizontalAlignment.Center;
        sp.Children.Add(restoreBtn);

        return new Border
        {
            Background = new SolidColorBrush(CardBg),
            BorderBrush = new SolidColorBrush(CardBorderDark),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(6),
            Visibility = Visibility.Collapsed,
            Child = sp
        };
    }

    // =========================================================================
    // 1. LOUDNESS PAINT
    // =========================================================================
    private void OnPaintLoudness(object? sender, SKPaintSurfaceEventArgs e)
    {
        var canvas = e.Surface.Canvas;
        float w = e.Info.Width;
        float h = e.Info.Height;
        canvas.Clear(BgPanelSk);
        if (w <= 30f || h <= 30f) return;

        var snap = _snapshot;
        float st = snap?.ShortTermLufs ?? -70f;
        float integ = snap?.IntegratedLufs ?? -70f;
        float mom = snap?.MomentaryLufs ?? -70f;
        float lra = snap?.LraLu ?? 0f;
        float tp = snap?.TruePeakDbTp ?? -110f;
        float target = snap?.TargetLufs ?? -14f;
        float maxTp = snap?.MaxTruePeakDbTp ?? -1f;

        float scale = Math.Clamp(Math.Min(w / 340f, h / 74f), 0.36f, 1.85f);
        bool ultraNarrow = w < 210f;

        using var captionPaint = new SKPaint
        {
            Color = AxisTextSk,
            TextSize = Math.Clamp(11.5f * scale, 6.0f, 20f),
            IsAntialias = true,
            TextAlign = SKTextAlign.Center,
            Typeface = WaveformControl.CjkTypeface
        };
        using var unitPaint = new SKPaint
        {
            Color = new SKColor(115, 128, 148),
            TextSize = Math.Clamp(10.5f * scale, 5.5f, 17f),
            IsAntialias = true,
            Typeface = WaveformControl.CjkTypeface
        };

        float labelY = Math.Max(captionPaint.TextSize + 3f, h * 0.21f);
        float topRowY = Math.Max(labelY + 25f * scale, h * 0.59f);
        float col1X = w * 0.18f;
        float col2X = w * 0.50f;
        float col3X = w * 0.82f;

        // Labels above big readouts (abbreviate only in ultra-narrow strips to prevent collision)
        canvas.DrawText(ultraNarrow ? "ST" : "Short Term", col1X, labelY, captionPaint);
        canvas.DrawText(ultraNarrow ? "Int" : "Integrated", col2X, labelY, captionPaint);
        canvas.DrawText(ultraNarrow ? "Mom" : "Momentary", col3X, labelY, captionPaint);

        string lufsUnit = ultraNarrow ? "LU" : "LUFS";
        DrawLoudnessValue(canvas, col1X, topRowY, st, target, 23f * scale, lufsUnit, unitPaint);
        DrawLoudnessValue(canvas, col2X, topRowY + 1f, integ, target, 28f * scale, lufsUnit, unitPaint);
        DrawLoudnessValue(canvas, col3X, topRowY, mom, target, 23f * scale, lufsUnit, unitPaint);

        // Sub-row: LRA & True Peak
        float subY = Math.Min(h - 6f, Math.Max(topRowY + 18f * scale, h * 0.90f));
        using var subLabelPaint = new SKPaint
        {
            Color = AxisTextSk,
            TextSize = Math.Clamp(11.5f * scale, 6.0f, 20f),
            IsAntialias = true,
            TextAlign = SKTextAlign.Right,
            Typeface = WaveformControl.CjkTypeface
        };
        using var subValuePaint = new SKPaint
        {
            Color = ShortTermSk,
            TextSize = Math.Clamp(14.5f * scale, 7.5f, 25f),
            IsAntialias = true,
            FakeBoldText = true,
            TextAlign = SKTextAlign.Left,
            Typeface = WaveformControl.CjkTypeface
        };

        float lraAnchorX = w * 0.30f;
        canvas.DrawText("LRA", lraAnchorX - 4f, subY - 1f, subLabelPaint);
        string lraText = lra.ToString("0.0");
        canvas.DrawText(lraText, lraAnchorX, subY, subValuePaint);
        float lraValW = subValuePaint.MeasureText(lraText);
        canvas.DrawText("LU", lraAnchorX + lraValW + 3f, subY - 1f, unitPaint);

        float tpAnchorX = w * 0.68f;
        bool tpOver = tp > maxTp;
        subValuePaint.Color = tpOver ? RedOverSk : (tp <= -109.5f ? AxisTextSk : ShortTermSk);
        canvas.DrawText(ultraNarrow ? "TP" : "True Peak", tpAnchorX - 4f, subY - 1f, subLabelPaint);
        string tpText = tp <= -109.5f ? "-∞" : tp.ToString("0.0");
        canvas.DrawText(tpText, tpAnchorX, subY, subValuePaint);
        float tpValW = subValuePaint.MeasureText(tpText);
        canvas.DrawText("dBTP", tpAnchorX + tpValW + 3f, subY - 1f, unitPaint);
    }

    private static void DrawLoudnessValue(SKCanvas canvas, float cx, float cy, float lufs, float targetLufs, float fontSize, string unit, SKPaint unitPaint)
    {
        SKColor color;
        string text;
        if (lufs <= -69.5f)
        {
            color = AxisTextSk;
            text = "-∞";
        }
        else
        {
            text = lufs.ToString("0.0");
            if (lufs > targetLufs + 0.5f) color = RedOverSk;
            else if (lufs >= targetLufs - 1.5f) color = AmberWarnSk;
            else color = GreenAccentSk;
        }

        using var valPaint = new SKPaint
        {
            Color = color,
            TextSize = fontSize,
            IsAntialias = true,
            FakeBoldText = true,
            TextAlign = SKTextAlign.Center,
            Typeface = WaveformControl.CjkTypeface
        };
        float numW = valPaint.MeasureText(text);
        float unitW = unitPaint.MeasureText(unit);
        float unitShift = Math.Clamp(unitW * 0.5f + 2f, 6f, 18f);
        canvas.DrawText(text, cx - unitShift, cy, valPaint);
        canvas.DrawText(unit, cx - unitShift + numW * 0.5f + 4f, cy - 1f, unitPaint);
    }

    // =========================================================================
    // 2. LEVELS PAINT
    // =========================================================================
    private void OnPaintLevels(object? sender, SKPaintSurfaceEventArgs e)
    {
        var canvas = e.Surface.Canvas;
        float w = e.Info.Width;
        float h = e.Info.Height;
        canvas.Clear(BgPanelSk);
        if (w <= 40f || h <= 40f) return;

        var snap = _snapshot;
        float lPeak = snap?.LeftPeakDb ?? -110f;
        float rPeak = snap?.RightPeakDb ?? -110f;
        float lRms = snap?.LeftRmsDb ?? -110f;
        float rRms = snap?.RightRmsDb ?? -110f;
        float lHold = snap?.LeftPeakHoldDb ?? -110f;
        float rHold = snap?.RightPeakHoldDb ?? -110f;
        bool lClip = snap?.LeftClipped ?? false;
        bool rClip = snap?.RightClipped ?? false;

        float topPad = 32f;
        float botPad = 20f;
        float meterH = Math.Max(20f, h - topPad - botPad);
        float barW = Math.Clamp(w * 0.22f, 36f, 96f);
        float lBarX = w * 0.31f - barW * 0.5f;
        float rBarX = w * 0.69f - barW * 0.5f;

        using var headerLabelPaint = new SKPaint
        {
            Color = AxisTextSk,
            TextSize = 9.5f,
            IsAntialias = true,
            TextAlign = SKTextAlign.Right,
            Typeface = WaveformControl.CjkTypeface
        };
        using var numPaint = new SKPaint
        {
            Color = ShortTermSk,
            TextSize = 10.5f,
            IsAntialias = true,
            TextAlign = SKTextAlign.Center,
            Typeface = WaveformControl.CjkTypeface
        };

        canvas.DrawText("Peak", Math.Max(34f, lBarX - 10f), 13f, headerLabelPaint);
        canvas.DrawText("RMS", Math.Max(34f, lBarX - 10f), 25f, headerLabelPaint);

        DrawMeterReadout(canvas, lBarX + barW * 0.5f, 13f, lPeak, lClip, numPaint);
        DrawMeterReadout(canvas, lBarX + barW * 0.5f, 25f, lRms, false, numPaint);
        DrawMeterReadout(canvas, rBarX + barW * 0.5f, 13f, rPeak, rClip, numPaint);
        DrawMeterReadout(canvas, rBarX + barW * 0.5f, 25f, rRms, false, numPaint);

        // Scale mapping: 0 dB at top, -60 dB near bottom (non-linear DAW look)
        float DbToY(float db)
        {
            float clamped = Math.Clamp(db, -60f, 0f);
            float norm = (0f - clamped) / 60f;
            return topPad + norm * meterH;
        }

        // Draw L & R Meter Blocks
        DrawChannelLevelBar(canvas, lBarX, topPad, barW, meterH, lPeak, lRms, lHold, lClip, DbToY);
        DrawChannelLevelBar(canvas, rBarX, topPad, barW, meterH, rPeak, rRms, rHold, rClip, DbToY);

        // Scale ticks on outer left & right
        int[] ticks = { 0, -10, -20, -30, -40, -50, -60 };
        using var tickPaint = new SKPaint { Color = new SKColor(18, 21, 26, 170), StrokeWidth = 1f };
        using var tickTextL = new SKPaint { Color = AxisTextSk, TextSize = 9f, IsAntialias = true, TextAlign = SKTextAlign.Right };
        using var tickTextR = new SKPaint { Color = AxisTextSk, TextSize = 9f, IsAntialias = true, TextAlign = SKTextAlign.Left };

        foreach (int db in ticks)
        {
            float y = DbToY(db);
            canvas.DrawLine(lBarX, y, lBarX + barW, y, tickPaint);
            canvas.DrawLine(rBarX, y, rBarX + barW, y, tickPaint);
            string lbl = db == -60 ? "-∞" : db.ToString();
            canvas.DrawText(lbl, lBarX - 5f, y + 3f, tickTextL);
            canvas.DrawText(lbl, rBarX + barW + 5f, y + 3f, tickTextR);
        }

        using var chLblPaint = new SKPaint { Color = LabelTextSk, TextSize = 10.5f, IsAntialias = true, TextAlign = SKTextAlign.Center };
        canvas.DrawText("L", lBarX + barW * 0.5f, topPad + meterH + 14f, chLblPaint);
        canvas.DrawText("R", rBarX + barW * 0.5f, topPad + meterH + 14f, chLblPaint);
    }

    private static void DrawMeterReadout(SKCanvas canvas, float cx, float y, float db, bool clip, SKPaint paint)
    {
        paint.Color = clip ? RedOverSk : (db <= -69.5f ? AxisTextSk : ShortTermSk);
        string text = db <= -69.5f ? "-∞" : db.ToString("0.0");
        canvas.DrawText(text, cx, y, paint);
    }

    private static void DrawChannelLevelBar(
        SKCanvas canvas,
        float x,
        float top,
        float w,
        float h,
        float peakDb,
        float rmsDb,
        float holdDb,
        bool clipped,
        Func<float, float> dbToY)
    {
        using var trackPaint = new SKPaint { Color = new SKColor(38, 46, 56), Style = SKPaintStyle.Fill };
        canvas.DrawRect(x, top, w, h, trackPaint);

        // Peak bar (dimmer steel-blue/slate)
        float peakY = dbToY(peakDb);
        float botY = top + h;
        if (peakY < botY - 0.5f)
        {
            SKColor peakCol = peakDb > -0.5f ? RedOverSk.WithAlpha(200)
                : (peakDb > -6.0f ? AmberWarnSk.WithAlpha(185) : new SKColor(118, 134, 152));
            using var pPaint = new SKPaint { Color = peakCol, Style = SKPaintStyle.Fill };
            canvas.DrawRect(x, peakY, w, botY - peakY, pPaint);
        }

        // RMS bar (bright crisp light-silver / emerald tint)
        float rmsY = dbToY(rmsDb);
        if (rmsY < botY - 0.5f)
        {
            using var rmsPaint = new SKPaint { Color = new SKColor(178, 189, 202), Style = SKPaintStyle.Fill };
            canvas.DrawRect(x, rmsY, w, botY - rmsY, rmsPaint);
        }

        // Peak Hold cap line
        if (holdDb > -59.5f)
        {
            float holdY = dbToY(holdDb);
            using var holdPaint = new SKPaint
            {
                Color = clipped ? RedOverSk : new SKColor(236, 242, 248),
                StrokeWidth = 2f
            };
            canvas.DrawLine(x, holdY, x + w, holdY, holdPaint);
        }

        // Clip indicator cap at top
        if (clipped)
        {
            using var clipPaint = new SKPaint { Color = RedOverSk, Style = SKPaintStyle.Fill };
            canvas.DrawRect(x, top, w, 4f, clipPaint);
        }
    }

    // =========================================================================
    // 3. SOUND FIELD PAINT (Polar Sample / Polar Level / Lissajous + Correlation)
    // =========================================================================
    private void OnPaintSoundField(object? sender, SKPaintSurfaceEventArgs e)
    {
        var canvas = e.Surface.Canvas;
        float w = e.Info.Width;
        float h = e.Info.Height;
        canvas.Clear(BgPanelSk);
        if (w <= 40f || h <= 40f) return;

        var snap = _snapshot;
        float corr = snap?.StereoCorrelation ?? 1.0f;
        float phaseDeg = snap?.PhaseDifferenceDeg ?? 0.0f;
        float[] ptsL = snap?.VectorPointsL ?? Array.Empty<float>();
        float[] ptsR = snap?.VectorPointsR ?? Array.Empty<float>();

        float corrWidth = 36f;
        float plotW = w - corrWidth - 8f;
        float plotH = h;

        using var ringPaint = new SKPaint
        {
            Color = new SKColor(58, 69, 84, 190),
            Style = SKPaintStyle.Stroke,
            StrokeWidth = 1f,
            IsAntialias = true
        };
        using var axisPaint = new SKPaint
        {
            Color = new SKColor(72, 85, 102, 200),
            Style = SKPaintStyle.Stroke,
            StrokeWidth = 1f,
            IsAntialias = true
        };
        using var labelPaint = new SKPaint
        {
            Color = AxisTextSk,
            TextSize = 9.5f,
            IsAntialias = true
        };

        if (_soundFieldMode == SoundFieldMode.Lissajous)
        {
            float cx = plotW * 0.5f;
            float cy = plotH * 0.5f;
            float r = Math.Max(16f, Math.Min(cx, cy) - 18f);

            canvas.DrawCircle(cx, cy, r, ringPaint);
            canvas.DrawCircle(cx, cy, r * 0.5f, ringPaint);
            canvas.DrawLine(cx, cy - r, cx, cy + r, axisPaint);
            canvas.DrawLine(cx - r, cy, cx + r, cy, axisPaint);

            float d = r * 0.7071f;
            canvas.DrawLine(cx - d, cy + d, cx + d, cy - d, ringPaint);
            canvas.DrawLine(cx + d, cy + d, cx - d, cy - d, ringPaint);
            canvas.DrawText("L", cx - d - 12f, cy - d, labelPaint);
            canvas.DrawText("R", cx + d + 5f, cy - d, labelPaint);

            DrawVectorSamples(canvas, ptsL, ptsR, cx, cy, r, semicircle: false);
        }
        else
        {
            // Semicircle Radar (Polar Sample & Polar Level)
            float cx = plotW * 0.5f;
            float cy = plotH - 18f;
            float r = Math.Max(20f, Math.Min(plotW * 0.46f, plotH - 30f));

            var outerRect = new SKRect(cx - r, cy - r, cx + r, cy + r);
            var midRect = new SKRect(cx - r * 0.5f, cy - r * 0.5f, cx + r * 0.5f, cy + r * 0.5f);
            canvas.DrawArc(outerRect, 180f, 180f, false, ringPaint);
            canvas.DrawArc(midRect, 180f, 180f, false, ringPaint);
            canvas.DrawLine(cx - r, cy, cx + r, cy, axisPaint);

            // Center vertical M axis + 45-degree L and R spokes
            canvas.DrawLine(cx, cy, cx, cy - r, axisPaint);
            float d = r * 0.7071f;
            canvas.DrawLine(cx, cy, cx - d, cy - d, ringPaint);
            canvas.DrawLine(cx, cy, cx + d, cy - d, ringPaint);

            canvas.DrawText("L", cx - r - 10f, cy + 2f, labelPaint);
            canvas.DrawText("R", cx + r + 4f, cy + 2f, labelPaint);

            if (_soundFieldMode == SoundFieldMode.PolarSample)
            {
                DrawVectorSamples(canvas, ptsL, ptsR, cx, cy, r, semicircle: true);
            }
            else
            {
                DrawPolarLevelEnvelope(canvas, ptsL, ptsR, cx, cy, r);
            }
        }

        // Right-side vertical Correlation meter (+1 / 0 / -1)
        float meterX = w - 26f;
        float meterTop = 14f;
        float meterBottom = h - 20f;
        float meterH = Math.Max(20f, meterBottom - meterTop);
        float midY = meterTop + meterH * 0.5f;

        using var trackPaint = new SKPaint { Color = new SKColor(38, 46, 56), Style = SKPaintStyle.Fill };
        canvas.DrawRoundRect(meterX - 2.5f, meterTop, 5f, meterH, 2.5f, 2.5f, trackPaint);

        canvas.DrawText("+1", meterX + 6f, meterTop + 7f, labelPaint);
        canvas.DrawText("0", meterX + 6f, midY + 3f, labelPaint);
        canvas.DrawText("-1", meterX + 6f, meterBottom, labelPaint);

        float clampedCorr = Math.Clamp(corr, -1f, 1f);
        float indY = midY - clampedCorr * (meterH * 0.5f);
        SKColor corrCol = clampedCorr >= 0.2f ? GreenAccentSk : (clampedCorr >= -0.2f ? AmberWarnSk : RedOverSk);

        using var corrBarPaint = new SKPaint { Color = corrCol.WithAlpha(190), Style = SKPaintStyle.Fill, IsAntialias = true };
        if (clampedCorr >= 0f)
            canvas.DrawRoundRect(meterX - 2.5f, indY, 5f, midY - indY, 2f, 2f, corrBarPaint);
        else
            canvas.DrawRoundRect(meterX - 2.5f, midY, 5f, indY - midY, 2f, 2f, corrBarPaint);

        using var corrDotPaint = new SKPaint { Color = corrCol, Style = SKPaintStyle.Fill, IsAntialias = true };
        canvas.DrawCircle(meterX, indY, 3.2f, corrDotPaint);

        // Compact numeric Phase / Correlation overlay bottom-left
        using var hudPaint = new SKPaint
        {
            Color = new SKColor(148, 163, 184, 210),
            TextSize = 9.5f,
            IsAntialias = true,
            Typeface = WaveformControl.CjkTypeface
        };
        canvas.DrawText($"Corr {corr:+0.00;-0.00;0.00}  ·  {phaseDeg:0}°", 8f, h - 6f, hudPaint);
    }

    private static void DrawVectorSamples(SKCanvas canvas, float[] ptsL, float[] ptsR, float cx, float cy, float r, bool semicircle)
    {
        if (ptsL.Length == 0 || ptsR.Length == 0) return;

        using var glowPaint = new SKPaint { Color = new SKColor(220, 235, 248, 42), IsAntialias = true };
        using var corePaint = new SKPaint { Color = new SKColor(235, 245, 255, 185), IsAntialias = true };

        int count = Math.Min(ptsL.Length, ptsR.Length);
        for (int i = 0; i < count; i++)
        {
            float l = ptsL[i];
            float rVal = ptsR[i];
            float u = (rVal - l) * 0.7071f; // Side (X: Left=-, Right=+)
            float v = (l + rVal) * 0.7071f; // Mid  (Y)

            if (semicircle)
            {
                // Fold into upper semicircle while preserving stereo width angle
                v = Math.Abs(v);
                float mag = MathF.Sqrt(u * u + v * v);
                if (mag > 1.0f)
                {
                    u /= mag;
                    v /= mag;
                }
            }
            else
            {
                u = Math.Clamp(u, -1f, 1f);
                v = Math.Clamp(v, -1f, 1f);
            }

            float px = cx + u * r;
            float py = cy - v * r;
            canvas.DrawCircle(px, py, 2.1f, glowPaint);
            canvas.DrawCircle(px, py, 1.0f, corePaint);
        }
    }

    private static void DrawPolarLevelEnvelope(SKCanvas canvas, float[] ptsL, float[] ptsR, float cx, float cy, float r)
    {
        if (ptsL.Length == 0 || ptsR.Length == 0) return;

        const int bins = 36;
        float[] energy = new float[bins];
        int count = Math.Min(ptsL.Length, ptsR.Length);

        for (int i = 0; i < count; i++)
        {
            float l = ptsL[i];
            float rVal = ptsR[i];
            float u = (rVal - l) * 0.7071f;
            float v = Math.Abs((l + rVal) * 0.7071f);
            float mag = MathF.Sqrt(u * u + v * v);
            if (mag < 1e-4f) continue;

            // Angle from -90 deg (left) to +90 deg (right)
            double angle = Math.Atan2(u, Math.Max(1e-5f, v)); // -PI/2 .. +PI/2
            int bin = Math.Clamp((int)((angle + Math.PI * 0.5) / Math.PI * (bins - 1)), 0, bins - 1);
            if (mag > energy[bin]) energy[bin] = mag;
        }

        using var path = new SKPath();
        path.MoveTo(cx, cy);
        for (int b = 0; b < bins; b++)
        {
            double angle = -Math.PI * 0.5 + Math.PI * b / (bins - 1);
            float rad = Math.Clamp(energy[b], 0.04f, 1.0f) * r;
            float x = cx + (float)(Math.Sin(angle) * rad);
            float y = cy - (float)(Math.Cos(angle) * rad);
            path.LineTo(x, y);
        }
        path.Close();

        using var fillPaint = new SKPaint
        {
            Color = GreenAccentSk.WithAlpha(85),
            Style = SKPaintStyle.Fill,
            IsAntialias = true
        };
        using var strokePaint = new SKPaint
        {
            Color = GreenAccentSk,
            Style = SKPaintStyle.Stroke,
            StrokeWidth = 1.6f,
            IsAntialias = true
        };
        canvas.DrawPath(path, fillPaint);
        canvas.DrawPath(path, strokePaint);
    }

    // =========================================================================
    // 4. SPECTRUM PAINT (Log 20..20k Hz, -110..0 dB, Peak Hold, Hover Tooltip)
    // =========================================================================
    private void OnSpectrumPointerMoved(object sender, PointerRoutedEventArgs e)
    {
        var pt = e.GetCurrentPoint(_spectrumCanvas).Position;
        _spectrumHoverX = (float)pt.X;
        _spectrumHoverY = (float)pt.Y;
        _spectrumCanvas.Invalidate();
    }

    private void OnSpectrumPointerExited(object sender, PointerRoutedEventArgs e)
    {
        _spectrumHoverX = null;
        _spectrumHoverY = null;
        _spectrumCanvas.Invalidate();
    }

    private void OnPaintSpectrum(object? sender, SKPaintSurfaceEventArgs e)
    {
        var canvas = e.Surface.Canvas;
        float w = e.Info.Width;
        float h = e.Info.Height;
        canvas.Clear(BgPanelSk);
        if (w <= 40f || h <= 40f) return;

        float padLeft = 10f;
        float padRight = 32f;
        float padTop = 10f;
        float padBottom = 20f;
        float plotW = Math.Max(1f, w - padLeft - padRight);
        float plotH = Math.Max(1f, h - padTop - padBottom);

        double logMin = Math.Log10(20.0);
        double logMax = Math.Log10(20000.0);

        float FreqToX(double freq) => padLeft + (float)((Math.Log10(Math.Clamp(freq, 20.0, 20000.0)) - logMin) / (logMax - logMin)) * plotW;
        float DbToY(float db) => padTop + (0f - Math.Clamp(db, -110f, 0f)) / 110f * plotH;

        using var gridPaint = new SKPaint { Color = GridLineSk, StrokeWidth = 1f };
        using var axisTextPaint = new SKPaint { Color = AxisTextSk, TextSize = 9.5f, IsAntialias = true };

        // Horizontal dB grid lines (-10 .. -110) with right-side labels
        for (int db = -10; db >= -110; db -= 10)
        {
            float y = DbToY(db);
            canvas.DrawLine(padLeft, y, padLeft + plotW, y, gridPaint);
            canvas.DrawText(db.ToString(), padLeft + plotW + 5f, y + 3f, axisTextPaint);
        }

        // Vertical logarithmic frequency ticks
        double[] fTicks = { 40, 100, 200, 400, 600, 1000, 2000, 4000, 6000, 10000 };
        string[] fLabels = { "40", "100", "200", "400", "600", "1k", "2k", "4k", "6k", "10k" };
        axisTextPaint.TextAlign = SKTextAlign.Center;
        for (int i = 0; i < fTicks.Length; i++)
        {
            float x = FreqToX(fTicks[i]);
            canvas.DrawLine(x, padTop, x, padTop + plotH, gridPaint);
            canvas.DrawText(fLabels[i], x, padTop + plotH + 13f, axisTextPaint);
        }

        var snap = _snapshot;
        if (snap != null && snap.SpectrumDb.Length > 1 && snap.SpectrumFrequenciesHz.Length == snap.SpectrumDb.Length)
        {
            int n = snap.SpectrumDb.Length;
            SKPoint[] pts = new SKPoint[n];
            SKPoint[] holdPts = new SKPoint[n];
            for (int i = 0; i < n; i++)
            {
                float x = FreqToX(snap.SpectrumFrequenciesHz[i]);
                pts[i] = new SKPoint(x, DbToY(snap.SpectrumDb[i]));
                holdPts[i] = new SKPoint(x, DbToY(snap.SpectrumPeakHoldDb[i]));
            }

            using var linePath = BuildSmoothPath(pts);
            using var areaPath = new SKPath(linePath);
            areaPath.LineTo(pts[n - 1].X, padTop + plotH);
            areaPath.LineTo(pts[0].X, padTop + plotH);
            areaPath.Close();

            using var fillPaint = new SKPaint
            {
                IsAntialias = true,
                Shader = SKShader.CreateLinearGradient(
                    new SKPoint(0, padTop),
                    new SKPoint(0, padTop + plotH),
                    new[] { new SKColor(210, 220, 232, 80), new SKColor(100, 116, 139, 12) },
                    new[] { 0f, 1f },
                    SKShaderTileMode.Clamp)
            };
            canvas.DrawPath(areaPath, fillPaint);

            // Peak hold dashed trace
            using var holdPath = BuildSmoothPath(holdPts);
            using var holdPaint = new SKPaint
            {
                Color = GreenAccentSk.WithAlpha(140),
                Style = SKPaintStyle.Stroke,
                StrokeWidth = 1.1f,
                IsAntialias = true,
                PathEffect = SKPathEffect.CreateDash(new[] { 4f, 3f }, 0)
            };
            canvas.DrawPath(holdPath, holdPaint);

            // Main spectrum trace
            using var strokePaint = new SKPaint
            {
                Color = new SKColor(224, 232, 240, 235),
                Style = SKPaintStyle.Stroke,
                StrokeWidth = 1.5f,
                IsAntialias = true
            };
            canvas.DrawPath(linePath, strokePaint);

            // Hover crosshair & frequency/dB tooltip
            if (_spectrumHoverX.HasValue && _spectrumHoverY.HasValue &&
                _spectrumHoverX.Value >= padLeft && _spectrumHoverX.Value <= padLeft + plotW &&
                _spectrumHoverY.Value >= padTop && _spectrumHoverY.Value <= padTop + plotH)
            {
                float hx = _spectrumHoverX.Value;
                float normX = Math.Clamp((hx - padLeft) / plotW, 0f, 1f);
                double hoverFreq = Math.Pow(10.0, logMin + normX * (logMax - logMin));
                int bandIdx = Math.Clamp((int)Math.Round(normX * (n - 1)), 0, n - 1);
                float hoverDb = snap.SpectrumDb[bandIdx];
                float holdDb = snap.SpectrumPeakHoldDb[bandIdx];
                float hy = DbToY(hoverDb);

                using var crossPaint = new SKPaint
                {
                    Color = GreenAccentSk.WithAlpha(180),
                    StrokeWidth = 1f,
                    PathEffect = SKPathEffect.CreateDash(new[] { 3f, 3f }, 0)
                };
                canvas.DrawLine(hx, padTop, hx, padTop + plotH, crossPaint);
                canvas.DrawLine(padLeft, hy, padLeft + plotW, hy, crossPaint);

                using var dotPaint = new SKPaint { Color = GreenAccentSk, IsAntialias = true };
                canvas.DrawCircle(hx, hy, 3.5f, dotPaint);

                string freqStr = hoverFreq >= 1000.0 ? $"{hoverFreq / 1000.0:0.00} kHz" : $"{hoverFreq:0} Hz";
                string tip = $"{freqStr}  ·  {hoverDb:0.0} dBFS  (Peak {holdDb:0.0})";

                using var tipTextPaint = new SKPaint
                {
                    Color = new SKColor(240, 248, 255),
                    TextSize = 10.5f,
                    IsAntialias = true,
                    Typeface = WaveformControl.CjkTypeface
                };
                float tw = tipTextPaint.MeasureText(tip);
                float boxW = tw + 14f;
                float boxH = 20f;
                float boxX = Math.Clamp(hx + 10f, padLeft + 4f, padLeft + plotW - boxW - 4f);
                float boxY = Math.Clamp(hy - 26f, padTop + 4f, padTop + plotH - boxH - 4f);

                using var tipBg = new SKPaint { Color = new SKColor(15, 19, 25, 235), Style = SKPaintStyle.Fill, IsAntialias = true };
                using var tipBorder = new SKPaint { Color = GreenAccentSk.WithAlpha(180), Style = SKPaintStyle.Stroke, StrokeWidth = 1f, IsAntialias = true };
                canvas.DrawRoundRect(boxX, boxY, boxW, boxH, 4f, 4f, tipBg);
                canvas.DrawRoundRect(boxX, boxY, boxW, boxH, 4f, 4f, tipBorder);
                canvas.DrawText(tip, boxX + 7f, boxY + 13.5f, tipTextPaint);
            }
        }
    }

    private static SKPath BuildSmoothPath(SKPoint[] pts)
    {
        var path = new SKPath();
        if (pts.Length == 0) return path;
        path.MoveTo(pts[0]);
        for (int i = 0; i < pts.Length - 1; i++)
        {
            SKPoint p0 = pts[Math.Max(0, i - 1)];
            SKPoint p1 = pts[i];
            SKPoint p2 = pts[i + 1];
            SKPoint p3 = pts[Math.Min(pts.Length - 1, i + 2)];
            SKPoint cp1 = new(p1.X + (p2.X - p0.X) / 6f, p1.Y + (p2.Y - p0.Y) / 6f);
            SKPoint cp2 = new(p2.X - (p3.X - p1.X) / 6f, p2.Y - (p3.Y - p1.Y) / 6f);
            path.CubicTo(cp1, cp2, p2);
        }
        return path;
    }

    // =========================================================================
    // 5. 3D ISOMETRIC SPECTROGRAM PAINT (5 Camera Angles + Magma/Inferno Palette)
    // =========================================================================
    private void OnPaintSpectrogram(object? sender, SKPaintSurfaceEventArgs e)
    {
        var canvas = e.Surface.Canvas;
        float w = e.Info.Width;
        float h = e.Info.Height;
        canvas.Clear(BgPanelSk);
        if (w <= 50f || h <= 50f) return;

        // Perspective angles (yawDeg, pitchDeg)
        (float yawDeg, float pitchDeg) = _spectrogramPerspective switch
        {
            SpectrogramPerspective.FrontHigh => (8f, 54f),
            SpectrogramPerspective.FrontLow => (6f, 22f),
            SpectrogramPerspective.SideLow => (-72f, 22f),
            SpectrogramPerspective.SideHigh => (-68f, 52f),
            _ => (-38f, 34f) // Diagonal (Insight Pro default)
        };

        double yaw = yawDeg * Math.PI / 180.0;
        double pitch = pitchDeg * Math.PI / 180.0;
        double cosY = Math.Cos(yaw);
        double sinY = Math.Sin(yaw);
        double cosP = Math.Cos(pitch);
        double sinP = Math.Sin(pitch);

        float scale = Math.Min(w * 0.34f, h * 0.34f);
        float cx = w * 0.49f;
        float cy = h * 0.54f;
        float heightScale = 0.48f;

        // World coordinates: x = freq [-1..1], z = time [-1..1] (+1 newest front, -1 oldest back), y = magnitude [0..heightScale]
        (SKPoint Screen, float Depth) Project(float wx, float wy, float wz)
        {
            double rx = wx * cosY - wz * sinY;
            double rz = wx * sinY + wz * cosY;
            double ry = wy * cosP - rz * sinP;
            double depth = wy * sinP + rz * cosP;
            return (new SKPoint(cx + (float)rx * scale, cy - (float)ry * scale), (float)depth);
        }

        // Draw 3D base bounding box & frequency/time axis labels
        var b00 = Project(-1f, 0f, -1f).Screen;
        var b10 = Project(1f, 0f, -1f).Screen;
        var b11 = Project(1f, 0f, 1f).Screen;
        var b01 = Project(-1f, 0f, 1f).Screen;

        using var baseGridPaint = new SKPaint { Color = new SKColor(52, 62, 76, 170), StrokeWidth = 1f, Style = SKPaintStyle.Stroke, IsAntialias = true };
        canvas.DrawLine(b00, b10, baseGridPaint);
        canvas.DrawLine(b10, b11, baseGridPaint);
        canvas.DrawLine(b11, b01, baseGridPaint);
        canvas.DrawLine(b01, b00, baseGridPaint);

        using var axisLabelPaint = new SKPaint { Color = AxisTextSk, TextSize = 9f, IsAntialias = true };

        // Frequency axis labels along right/front edge (wx from -1 to +1, wz = +1)
        double[] fTicks = { 25, 100, 500, 1000, 2000, 5000, 10000, 20000 };
        string[] fLabels = { "25", "100", "500", "1k", "2k", "5k", "10k", "20000" };
        double lMin = Math.Log10(25.0);
        double lMax = Math.Log10(20000.0);
        for (int i = 0; i < fTicks.Length; i++)
        {
            float nx = (float)((Math.Log10(fTicks[i]) - lMin) / (lMax - lMin));
            float wx = -1f + nx * 2f;
            var pEdge = Project(wx, 0f, 1.04f).Screen;
            canvas.DrawText(fLabels[i], pEdge.X + 2f, pEdge.Y + 4f, axisLabelPaint);
        }

        // Time axis ticks along left side (wx = -1.06, wz from +1 to -1)
        string[] tLabels = { "0", "5", "10", "15" };
        for (int i = 0; i < tLabels.Length; i++)
        {
            float wz = 1f - (i / 3f) * 2f;
            var pTime = Project(-1.08f, 0f, wz).Screen;
            canvas.DrawText(tLabels[i], pTime.X - 10f, pTime.Y + 3f, axisLabelPaint);
        }

        var snap = _snapshot;
        float[][]? frames = snap?.SpectrogramFrames;
        if (frames == null || frames.Length < 2)
        {
            // Subtle floor crosslines when idle
            for (int g = 1; g < 4; g++)
            {
                float t = -1f + g * 0.5f;
                canvas.DrawLine(Project(-1f, 0f, t).Screen, Project(1f, 0f, t).Screen, baseGridPaint);
                canvas.DrawLine(Project(t, 0f, -1f).Screen, Project(t, 0f, 1f).Screen, baseGridPaint);
            }
            return;
        }

        // Downsample grid slightly for buttery 60fps Skia rendering (max 40 slices x 48 freq bins)
        int srcSlices = frames.Length;
        int tSteps = Math.Min(40, srcSlices);
        int fSteps = 48;

        // Determine Painter's back-to-front traversal direction based on camera yaw
        bool drawTimeForward = Project(0f, 0f, -1f).Depth <= Project(0f, 0f, 1f).Depth;
        bool drawFreqForward = Project(-1f, 0f, 0f).Depth <= Project(1f, 0f, 0f).Depth;

        // Pre-project grid vertices
        var gridPts = new SKPoint[tSteps, fSteps];
        var gridVals = new float[tSteps, fSteps];
        for (int ti = 0; ti < tSteps; ti++)
        {
            int srcT = Math.Clamp((int)Math.Round((double)ti / (tSteps - 1) * (srcSlices - 1)), 0, srcSlices - 1);
            float[] slice = frames[srcT];
            float wz = -1f + 2f * ti / (tSteps - 1); // -1 oldest -> +1 newest

            for (int fi = 0; fi < fSteps; fi++)
            {
                int srcF = Math.Clamp((int)Math.Round((double)fi / (fSteps - 1) * (slice.Length - 1)), 0, slice.Length - 1);
                float val = Math.Clamp(slice[srcF], 0f, 1f);
                float wx = -1f + 2f * fi / (fSteps - 1);
                gridVals[ti, fi] = val;
                gridPts[ti, fi] = Project(wx, val * heightScale, wz).Screen;
            }
        }

        using var quadPaint = new SKPaint { Style = SKPaintStyle.Fill, IsAntialias = true };
        using var ridgePaint = new SKPaint { Style = SKPaintStyle.Stroke, StrokeWidth = 0.8f, IsAntialias = true };
        using var quadPath = new SKPath();

        for (int tLoop = 0; tLoop < tSteps - 1; tLoop++)
        {
            int ti = drawTimeForward ? tLoop : (tSteps - 2 - tLoop);
            for (int fLoop = 0; fLoop < fSteps - 1; fLoop++)
            {
                int fi = drawFreqForward ? fLoop : (fSteps - 2 - fLoop);

                float avgVal = 0.25f * (gridVals[ti, fi] + gridVals[ti + 1, fi] + gridVals[ti + 1, fi + 1] + gridVals[ti, fi + 1]);
                SKColor col = EvaluateMagmaColor(avgVal);

                quadPath.Reset();
                quadPath.MoveTo(gridPts[ti, fi]);
                quadPath.LineTo(gridPts[ti, fi + 1]);
                quadPath.LineTo(gridPts[ti + 1, fi + 1]);
                quadPath.LineTo(gridPts[ti + 1, fi]);
                quadPath.Close();

                quadPaint.Color = col;
                canvas.DrawPath(quadPath, quadPaint);

                if (avgVal > 0.14f)
                {
                    ridgePaint.Color = col.WithAlpha(160);
                    canvas.DrawLine(gridPts[ti + 1, fi], gridPts[ti + 1, fi + 1], ridgePaint);
                }
            }
        }
    }

    private static SKColor EvaluateMagmaColor(float t)
    {
        t = Math.Clamp(t, 0f, 1f);
        // 5-stop Magma/Inferno palette: Deep Indigo -> Purple -> Crimson -> Orange -> Bright Gold
        (float Pos, byte R, byte G, byte B)[] stops =
        {
            (0.00f, 18, 12, 42),
            (0.28f, 92, 18, 110),
            (0.56f, 187, 55, 84),
            (0.80f, 249, 140, 10),
            (1.00f, 252, 244, 166)
        };

        for (int i = 0; i < stops.Length - 1; i++)
        {
            if (t <= stops[i + 1].Pos)
            {
                float span = stops[i + 1].Pos - stops[i].Pos;
                float u = span > 1e-5f ? (t - stops[i].Pos) / span : 0f;
                byte r = (byte)(stops[i].R + (stops[i + 1].R - stops[i].R) * u);
                byte g = (byte)(stops[i].G + (stops[i + 1].G - stops[i].G) * u);
                byte b = (byte)(stops[i].B + (stops[i + 1].B - stops[i].B) * u);
                return new SKColor(r, g, b);
            }
        }
        return new SKColor(252, 244, 166);
    }

    // =========================================================================
    // 6. HISTORY PAINT (Integrated, ShortTerm, Momentary, Target Dashed Line)
    // =========================================================================
    private void OnPaintHistory(object? sender, SKPaintSurfaceEventArgs e)
    {
        var canvas = e.Surface.Canvas;
        float w = e.Info.Width;
        float h = e.Info.Height;
        canvas.Clear(BgPanelSk);
        if (w <= 40f || h <= 30f) return;

        float padLeft = 10f;
        float padRight = 32f;
        float padTop = 10f;
        float padBottom = 18f;
        float plotW = Math.Max(1f, w - padLeft - padRight);
        float plotH = Math.Max(1f, h - padTop - padBottom);

        float LufsToY(float lufs)
        {
            float clamped = Math.Clamp(lufs, -60f, 0f);
            return padTop + (0f - clamped) / 60f * plotH;
        }

        using var gridPaint = new SKPaint { Color = GridLineSk, StrokeWidth = 1f };
        using var axisPaint = new SKPaint { Color = AxisTextSk, TextSize = 9f, IsAntialias = true };

        for (int lufs = 0; lufs >= -60; lufs -= 10)
        {
            float y = LufsToY(lufs);
            canvas.DrawLine(padLeft, y, padLeft + plotW, y, gridPaint);
            canvas.DrawText(lufs.ToString(), padLeft + plotW + 5f, y + 3f, axisPaint);
        }

        var snap = _snapshot;
        float targetLufs = snap?.TargetLufs ?? -14f;
        var samples = snap?.HistorySamples ?? Array.Empty<InsightHistoryPoint>();

        // Time grid ticks
        double maxT = samples.Length >= 2 ? Math.Max(5.0, samples[^1].TimeSeconds) : 20.0;
        double minT = samples.Length >= 2 ? samples[0].TimeSeconds : 0.0;
        double spanT = Math.Max(1.0, maxT - minT);

        axisPaint.TextAlign = SKTextAlign.Center;
        int timeDivs = Math.Clamp((int)(plotW / 65f), 4, 24);
        for (int i = 0; i <= timeDivs; i++)
        {
            float x = padLeft + (float)i / timeDivs * plotW;
            canvas.DrawLine(x, padTop, x, padTop + plotH, gridPaint);
            double sec = minT + spanT * i / timeDivs;
            canvas.DrawText($"{sec:0.0}s", x, padTop + plotH + 12f, axisPaint);
        }

        // Target horizontal dashed red line
        float targetY = LufsToY(targetLufs);
        using var targetLinePaint = new SKPaint
        {
            Color = RedOverSk.WithAlpha(210),
            StrokeWidth = 1.3f,
            Style = SKPaintStyle.Stroke,
            PathEffect = SKPathEffect.CreateDash(new[] { 5f, 4f }, 0)
        };
        canvas.DrawLine(padLeft, targetY, padLeft + plotW, targetY, targetLinePaint);

        if (samples.Length >= 2)
        {
            using var momPath = new SKPath();
            using var stPath = new SKPath();
            using var intPath = new SKPath();

            for (int i = 0; i < samples.Length; i++)
            {
                var p = samples[i];
                float x = padLeft + (float)((p.TimeSeconds - minT) / spanT) * plotW;
                float yMom = LufsToY(p.MomentaryLufs);
                float ySt = LufsToY(p.ShortTermLufs);
                float yInt = LufsToY(p.IntegratedLufs);

                if (i == 0)
                {
                    momPath.MoveTo(x, yMom);
                    stPath.MoveTo(x, ySt);
                    intPath.MoveTo(x, yInt);
                }
                else
                {
                    momPath.LineTo(x, yMom);
                    stPath.LineTo(x, ySt);
                    intPath.LineTo(x, yInt);
                }
            }

            // Momentary trace (dim gray)
            using var momPaint = new SKPaint
            {
                Color = MomentarySk.WithAlpha(215),
                Style = SKPaintStyle.Stroke,
                StrokeWidth = 1.2f,
                IsAntialias = true
            };
            canvas.DrawPath(momPath, momPaint);

            // Short Term trace (bright off-white)
            using var stPaint = new SKPaint
            {
                Color = ShortTermSk.WithAlpha(210),
                Style = SKPaintStyle.Stroke,
                StrokeWidth = 1.5f,
                IsAntialias = true
            };
            canvas.DrawPath(stPath, stPaint);

            // Integrated trace (green, or red if overflowing target)
            bool integOver = snap != null && snap.IntegratedExceedsTarget;
            using var intPaint = new SKPaint
            {
                Color = integOver ? RedOverSk : GreenAccentSk,
                Style = SKPaintStyle.Stroke,
                StrokeWidth = 2.0f,
                IsAntialias = true
            };
            canvas.DrawPath(intPath, intPaint);
        }

        // Compact legend at top-left
        DrawHistoryLegend(canvas, padLeft + 8f, padTop + 12f, targetLufs);
    }

    private static void DrawHistoryLegend(SKCanvas canvas, float x, float y, float targetLufs)
    {
        (string Label, SKColor Color)[] items =
        {
            ("Integrated", GreenAccentSk),
            ("Short Term", ShortTermSk),
            ("Momentary", new SKColor(135, 148, 164)),
            ($"Target ({targetLufs:0} LUFS)", RedOverSk)
        };

        using var textPaint = new SKPaint
        {
            Color = LabelTextSk,
            TextSize = 9.5f,
            IsAntialias = true,
            Typeface = WaveformControl.CjkTypeface
        };
        using var dotPaint = new SKPaint { IsAntialias = true };

        float curX = x;
        foreach (var item in items)
        {
            dotPaint.Color = item.Color;
            canvas.DrawCircle(curX + 3f, y - 3f, 3f, dotPaint);
            canvas.DrawText(item.Label, curX + 9f, y, textPaint);
            curX += textPaint.MeasureText(item.Label) + 22f;
        }
    }
}
