using System;
using System.Collections.Generic;
using FurinaPlayer.Audio.Dsp;
using FurinaPlayer.Core.Services;
using Microsoft.UI;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using SkiaSharp;
using SkiaSharp.Views.Windows;
using Windows.UI;

namespace FurinaPlayer.UI.Controls;

/// <summary>
/// Reusable Neoverb Pro-inspired Space & Blend Pad Workbench Control.
/// Used by both EditorPage ("空间与变速") and DspPage ("虚拟声场").
/// </summary>
public sealed class NeoverbWorkbenchControl : UserControl
{
	private FurinaNeoverbProcessor? _processor;
	private bool _suppressEvents;
	private bool _draggingPad;
	private int _activeEqTab; // 0=Pre EQ, 1=Reverb EQ

	private ToggleSwitch _enableToggle = null!;
	private ComboBox _presetCombo = null!;
	private ComboBox _hallFlavorCombo = null!;
	private SKXamlCanvas _blendPadCanvas = null!;
	private TextBlock _reflDbText = null!;
	private TextBlock _plateDbText = null!;
	private TextBlock _hallDbText = null!;

	private Slider _reflSpaceSlider = null!;
	private Slider _plateSpaceSlider = null!;
	private Slider _hallSpaceSlider = null!;
	private Slider _modRateSlider = null!;
	private Slider _modDepthSlider = null!;
	private Slider _preDelaySlider = null!;
	private Slider _smoothSlider = null!;
	private Slider _dryWetSlider = null!;
	private Slider _levelSlider = null!;

	private Button _btnPreEqTab = null!;
	private Button _btnRevEqTab = null!;
	private ToggleButton _autoCutBtn = null!;
	private SKXamlCanvas _eqCurveCanvas = null!;
	private Slider _eqLowSlider = null!;
	private Slider _eqMidSlider = null!;
	private Slider _eqHighSlider = null!;

	public event Action? ParametersChanged;

	public NeoverbWorkbenchControl()
	{
		BuildUi();
	}

	public void BindProcessor(FurinaNeoverbProcessor processor)
	{
		_processor = processor;
		SyncFromProcessor();
	}

	public void SyncFromProcessor()
	{
		if (_processor == null) return;
		_suppressEvents = true;
		try
		{
			_enableToggle.IsOn = _processor.Enabled;
			_hallFlavorCombo.SelectedIndex = (int)_processor.HallFlavor;
			_reflSpaceSlider.Value = _processor.ReflectionsSpace;
			_plateSpaceSlider.Value = _processor.PlateSpace;
			_hallSpaceSlider.Value = _processor.HallSpace;
			_modRateSlider.Value = _processor.ModRateHz;
			_modDepthSlider.Value = _processor.ModDepthPct;
			_preDelaySlider.Value = _processor.PreDelayMs;
			_smoothSlider.Value = _processor.SmoothPct;
			_dryWetSlider.Value = _processor.DryWetPct;
			_levelSlider.Value = _processor.LevelDb;
			_autoCutBtn.IsChecked = _processor.AutoCutEnabled;
			SyncEqSliders();
			UpdateWeightReadouts();
			_blendPadCanvas?.Invalidate();
			_eqCurveCanvas?.Invalidate();
		}
		finally
		{
			_suppressEvents = false;
		}
	}

	private void BuildUi()
	{
		var root = new StackPanel { Spacing = 12 };

		// 1. Top Header Bar (Neoverb title, Assistant, Presets, Enable)
		var topBar = new Grid { ColumnSpacing = 10 };
		topBar.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
		topBar.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
		topBar.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
		topBar.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

		var titleStack = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8, VerticalAlignment = VerticalAlignment.Center };
		titleStack.Children.Add(new FontIcon { Glyph = "\uE995", FontSize = 16, Foreground = new SolidColorBrush(Color.FromArgb(255, 96, 165, 250)) });
		titleStack.Children.Add(new TextBlock { Text = Loc.T("空间声场工作台"), FontSize = 15, FontWeight = FontWeights.SemiBold, VerticalAlignment = VerticalAlignment.Center });
		Grid.SetColumn(titleStack, 0);
		topBar.Children.Add(titleStack);

		var assistBtn = new Button
		{
			Content = Loc.T("Reverb Assistant 智能辅助"),
			CornerRadius = new CornerRadius(8),
			Padding = new Thickness(10, 5, 10, 5),
			HorizontalAlignment = HorizontalAlignment.Right
		};
		var assistFlyout = new MenuFlyout();
		foreach (var (label, style, size, tone) in new[]
		{
			(Loc.T("清晰人声 (Vocal · 中等空间)"), "vocal", "medium", "clean"),
			(Loc.T("温暖原声 (Acoustic · 温暖室内)"), "acoustic", "medium", "warm"),
			(Loc.T("宏大音乐厅 (Hall · 宏伟明亮)"), "hall", "large", "bright"),
			(Loc.T("梦幻氛围 (Ambient · 广袤云海)"), "ambient", "large", "clean"),
			(Loc.T("紧凑近场 (Tight Room · 干脆)"), "tight", "small", "clean")
		})
		{
			var item = new MenuFlyoutItem { Text = label };
			item.Click += (_, _) =>
			{
				_processor?.ApplyReverbAssistant(style, size, tone);
				SyncFromProcessor();
				ParametersChanged?.Invoke();
			};
			assistFlyout.Items.Add(item);
		}
		assistBtn.Flyout = assistFlyout;
		Grid.SetColumn(assistBtn, 1);
		topBar.Children.Add(assistBtn);

		_presetCombo = new ComboBox { MinWidth = 175, CornerRadius = new CornerRadius(8), PlaceholderText = Loc.T("选择混响预设") };
		foreach (var p in FurinaNeoverbProcessor.Presets)
		{
			_presetCombo.Items.Add(new ComboBoxItem { Content = Loc.T(p.Name), Tag = p });
		}
		_presetCombo.SelectedIndex = 0;
		_presetCombo.SelectionChanged += (_, _) =>
		{
			if (_suppressEvents || _processor == null) return;
			if (_presetCombo.SelectedItem is ComboBoxItem { Tag: NeoverbPreset p })
			{
				_processor.ApplyPreset(p);
				_processor.Enabled = true;
				SyncFromProcessor();
				ParametersChanged?.Invoke();
			}
		};
		Grid.SetColumn(_presetCombo, 2);
		topBar.Children.Add(_presetCombo);

		_enableToggle = new ToggleSwitch { OnContent = Loc.T("开启"), OffContent = Loc.T("旁路"), MinWidth = 0 };
		_enableToggle.Toggled += (_, _) =>
		{
			if (_suppressEvents || _processor == null) return;
			_processor.Enabled = _enableToggle.IsOn;
			ParametersChanged?.Invoke();
		};
		Grid.SetColumn(_enableToggle, 3);
		topBar.Children.Add(_enableToggle);
		root.Children.Add(topBar);

		// 2. Main 3-Column Section (Engines | Triangular Blend Pad | Mod & PreDelay & Dry/Wet)
		var mainGrid = new Grid { ColumnSpacing = 12 };
		mainGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(220) });
		mainGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
		mainGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(250) });

		// Left Column: 3 Engines
		var leftCol = new StackPanel { Spacing = 8 };
		_reflSpaceSlider = MakeCompactSlider(Loc.T("Reflections 早期反射大小"), 0, 100, 31, v => { if (_processor != null) _processor.ReflectionsSpace = v; });
		leftCol.Children.Add(WrapSubCard(_reflSpaceSlider));

		_plateSpaceSlider = MakeCompactSlider(Loc.T("Plate 金属板衰减 (s)"), 0.2, 6.0, 1.10, v => { if (_processor != null) _processor.PlateSpace = v; }, step: 0.05);
		leftCol.Children.Add(WrapSubCard(_plateSpaceSlider));

		var hallStack = new StackPanel { Spacing = 6 };
		_hallFlavorCombo = new ComboBox { HorizontalAlignment = HorizontalAlignment.Stretch, CornerRadius = new CornerRadius(8) };
		_hallFlavorCombo.Items.Add(new ComboBoxItem { Content = "Hall (音乐厅)" });
		_hallFlavorCombo.Items.Add(new ComboBoxItem { Content = "Room (录音室)" });
		_hallFlavorCombo.Items.Add(new ComboBoxItem { Content = "Chamber (木质室)" });
		_hallFlavorCombo.SelectedIndex = 0;
		_hallFlavorCombo.SelectionChanged += (_, _) =>
		{
			if (_suppressEvents || _processor == null) return;
			_processor.HallFlavor = (NeoverbHallFlavor)Math.Clamp(_hallFlavorCombo.SelectedIndex, 0, 2);
			ParametersChanged?.Invoke();
		};
		_hallSpaceSlider = MakeCompactSlider(Loc.T("Hall / Room 空间衰减 (s)"), 0.4, 12.0, 2.50, v => { if (_processor != null) _processor.HallSpace = v; }, step: 0.05);
		hallStack.Children.Add(_hallFlavorCombo);
		hallStack.Children.Add(_hallSpaceSlider);
		leftCol.Children.Add(WrapSubCard(hallStack));
		Grid.SetColumn(leftCol, 0);
		mainGrid.Children.Add(leftCol);

		// Center Column: Triangular Blend Pad
		var padCard = new Border
		{
			CornerRadius = new CornerRadius(12),
			Padding = new Thickness(10),
			MinHeight = 250,
			Background = new SolidColorBrush(Color.FromArgb(220, 17, 24, 39)),
			BorderThickness = new Thickness(1),
			BorderBrush = new SolidColorBrush(Color.FromArgb(60, 148, 163, 184))
		};
		var padGrid = new Grid();
		padGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
		padGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
		padGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

		_reflDbText = new TextBlock { Text = "Reflections -3.0 dB", FontSize = 11.5, FontWeight = FontWeights.SemiBold, Foreground = new SolidColorBrush(Color.FromArgb(255, 56, 189, 248)), HorizontalAlignment = HorizontalAlignment.Center };
		Grid.SetRow(_reflDbText, 0);
		padGrid.Children.Add(_reflDbText);

		_blendPadCanvas = new SKXamlCanvas { MinHeight = 195 };
		_blendPadCanvas.PaintSurface += OnBlendPadPaint;
		_blendPadCanvas.PointerPressed += OnBlendPadPointerPressed;
		_blendPadCanvas.PointerMoved += OnBlendPadPointerMoved;
		_blendPadCanvas.PointerReleased += OnBlendPadPointerReleased;
		Grid.SetRow(_blendPadCanvas, 1);
		padGrid.Children.Add(_blendPadCanvas);

		var bottomLabels = new Grid();
		bottomLabels.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
		bottomLabels.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
		_plateDbText = new TextBlock { Text = "Plate -3.0 dB", FontSize = 11.5, FontWeight = FontWeights.SemiBold, Foreground = new SolidColorBrush(Color.FromArgb(255, 217, 70, 239)), HorizontalAlignment = HorizontalAlignment.Left };
		_hallDbText = new TextBlock { Text = "Hall -3.0 dB", FontSize = 11.5, FontWeight = FontWeights.SemiBold, Foreground = new SolidColorBrush(Color.FromArgb(255, 249, 115, 22)), HorizontalAlignment = HorizontalAlignment.Right };
		Grid.SetColumn(_plateDbText, 0);
		Grid.SetColumn(_hallDbText, 1);
		bottomLabels.Children.Add(_plateDbText);
		bottomLabels.Children.Add(_hallDbText);
		Grid.SetRow(bottomLabels, 2);
		padGrid.Children.Add(bottomLabels);

		padCard.Child = padGrid;
		Grid.SetColumn(padCard, 1);
		mainGrid.Children.Add(padCard);

		// Right Column: Mod, Pre-Delay, Smooth, Dry/Wet, Level
		var rightCol = new StackPanel { Spacing = 8 };
		var modStack = new StackPanel { Spacing = 4 };
		_modRateSlider = MakeCompactSlider(Loc.T("Mod 调制速率 (Hz)"), 0.05, 5.0, 0.48, v => { if (_processor != null) _processor.ModRateHz = v; }, step: 0.05);
		_modDepthSlider = MakeCompactSlider(Loc.T("Mod 调制深度 (%)"), 0, 100, 25, v => { if (_processor != null) _processor.ModDepthPct = v; });
		modStack.Children.Add(_modRateSlider);
		modStack.Children.Add(_modDepthSlider);
		rightCol.Children.Add(WrapSubCard(modStack));

		var timeStack = new StackPanel { Spacing = 4 };
		_preDelaySlider = MakeCompactSlider(Loc.T("Pre-Delay 预延迟 (ms)"), 0, 250, 20, v => { if (_processor != null) _processor.PreDelayMs = v; });
		_smoothSlider = MakeCompactSlider(Loc.T("Smooth 瞬态柔化 (%)"), 0, 100, 80, v => { if (_processor != null) _processor.SmoothPct = v; });
		timeStack.Children.Add(_preDelaySlider);
		timeStack.Children.Add(_smoothSlider);
		rightCol.Children.Add(WrapSubCard(timeStack));

		var mixStack = new StackPanel { Spacing = 4 };
		_dryWetSlider = MakeCompactSlider(Loc.T("Dry / Wet 干湿比 (%)"), 0, 100, 32, v => { if (_processor != null) _processor.DryWetPct = v; });
		_levelSlider = MakeCompactSlider(Loc.T("Output Level 输出电平 (dB)"), -24, 6, 0, v => { if (_processor != null) _processor.LevelDb = v; }, step: 0.5);
		mixStack.Children.Add(_dryWetSlider);
		mixStack.Children.Add(_levelSlider);
		rightCol.Children.Add(WrapSubCard(mixStack));

		Grid.SetColumn(rightCol, 2);
		mainGrid.Children.Add(rightCol);
		root.Children.Add(mainGrid);

		// 3. Bottom Pre EQ / Reverb EQ + Auto Cut Bar
		var eqCard = new Border
		{
			CornerRadius = new CornerRadius(12),
			Padding = new Thickness(12, 10, 12, 10),
			Background = new SolidColorBrush(Color.FromArgb(200, 15, 23, 42)),
			BorderThickness = new Thickness(1),
			BorderBrush = new SolidColorBrush(Color.FromArgb(55, 148, 163, 184))
		};
		var eqStack = new StackPanel { Spacing = 8 };

		var eqTabs = new Grid { ColumnSpacing = 8 };
		eqTabs.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
		eqTabs.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
		eqTabs.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
		eqTabs.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

		_btnPreEqTab = new Button { Content = "Pre EQ", CornerRadius = new CornerRadius(8), Padding = new Thickness(12, 4, 12, 4) };
		_btnRevEqTab = new Button { Content = "Reverb EQ", CornerRadius = new CornerRadius(8), Padding = new Thickness(12, 4, 12, 4) };
		_btnPreEqTab.Click += (_, _) => { _activeEqTab = 0; SyncEqSliders(); _eqCurveCanvas?.Invalidate(); };
		_btnRevEqTab.Click += (_, _) => { _activeEqTab = 1; SyncEqSliders(); _eqCurveCanvas?.Invalidate(); };
		Grid.SetColumn(_btnPreEqTab, 0);
		Grid.SetColumn(_btnRevEqTab, 1);
		eqTabs.Children.Add(_btnPreEqTab);
		eqTabs.Children.Add(_btnRevEqTab);

		_autoCutBtn = new ToggleButton { Content = Loc.T("Auto Cut 自动削减共振"), IsChecked = true, CornerRadius = new CornerRadius(8), Padding = new Thickness(10, 4, 10, 4) };
		_autoCutBtn.Click += (_, _) =>
		{
			if (_processor != null)
			{
				_processor.AutoCutEnabled = _autoCutBtn.IsChecked == true;
				_eqCurveCanvas?.Invalidate();
				ParametersChanged?.Invoke();
			}
		};
		Grid.SetColumn(_autoCutBtn, 3);
		eqTabs.Children.Add(_autoCutBtn);
		eqStack.Children.Add(eqTabs);

		_eqCurveCanvas = new SKXamlCanvas { Height = 64 };
		_eqCurveCanvas.PaintSurface += OnEqCurvePaint;
		eqStack.Children.Add(_eqCurveCanvas);

		var eqSlidersGrid = new Grid { ColumnSpacing = 12 };
		eqSlidersGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
		eqSlidersGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
		eqSlidersGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

		_eqLowSlider = MakeCompactSlider(Loc.T("低频 Low (dB)"), -12, 12, -2, v => UpdateEqBand(0, v), step: 0.5);
		_eqMidSlider = MakeCompactSlider(Loc.T("中频 Mid (dB)"), -12, 12, 0, v => UpdateEqBand(1, v), step: 0.5);
		_eqHighSlider = MakeCompactSlider(Loc.T("高频 High (dB)"), -12, 12, -1.5, v => UpdateEqBand(2, v), step: 0.5);
		Grid.SetColumn(_eqLowSlider, 0);
		Grid.SetColumn(_eqMidSlider, 1);
		Grid.SetColumn(_eqHighSlider, 2);
		eqSlidersGrid.Children.Add(_eqLowSlider);
		eqSlidersGrid.Children.Add(_eqMidSlider);
		eqSlidersGrid.Children.Add(_eqHighSlider);
		eqStack.Children.Add(eqSlidersGrid);

		eqCard.Child = eqStack;
		root.Children.Add(eqCard);

		Content = root;
	}

	private void UpdateEqBand(int band, double gainDb)
	{
		if (_processor == null) return;
		var arr = _activeEqTab == 0 ? _processor.PreEqGainsDb : _processor.ReverbEqGainsDb;
		arr[band] = gainDb;
		_processor.MarkEqDirty();
		_eqCurveCanvas?.Invalidate();
		ParametersChanged?.Invoke();
	}

	private void SyncEqSliders()
	{
		if (_processor == null) return;
		bool prev = _suppressEvents;
		_suppressEvents = true;
		try
		{
			var arr = _activeEqTab == 0 ? _processor.PreEqGainsDb : _processor.ReverbEqGainsDb;
			_eqLowSlider.Value = arr[0];
			_eqMidSlider.Value = arr[1];
			_eqHighSlider.Value = arr[2];
		}
		finally
		{
			_suppressEvents = prev;
		}
	}

	private Slider MakeCompactSlider(string header, double min, double max, double val, Action<double> onChanged, double step = 1.0)
	{
		var s = new Slider
		{
			Header = header,
			Minimum = min,
			Maximum = max,
			Value = val,
			StepFrequency = step
		};
		s.ValueChanged += (_, e) =>
		{
			if (_suppressEvents) return;
			onChanged(e.NewValue);
			ParametersChanged?.Invoke();
		};
		return s;
	}

	private static Border WrapSubCard(UIElement child) => new Border
	{
		CornerRadius = new CornerRadius(10),
		Padding = new Thickness(10, 7, 10, 7),
		Background = new SolidColorBrush(Color.FromArgb(42, 255, 255, 255)),
		BorderThickness = new Thickness(1),
		BorderBrush = new SolidColorBrush(Color.FromArgb(35, 148, 163, 184)),
		Child = child
	};

	private void UpdateWeightReadouts()
	{
		if (_processor == null) return;
		var (wR, wP, wH) = _processor.GetBlendWeights();
		static string ToDbText(string name, double w)
		{
			double db = w <= 0.01 ? -36.0 : Math.Max(-36.0, 20.0 * Math.Log10(w));
			return $"{name} {db:F1} dB";
		}
		if (_reflDbText != null) _reflDbText.Text = ToDbText("Reflections", wR);
		if (_plateDbText != null) _plateDbText.Text = ToDbText("Plate", wP);
		if (_hallDbText != null) _hallDbText.Text = ToDbText("Hall", wH);
	}

	private void OnBlendPadPaint(object? sender, SKPaintSurfaceEventArgs e)
	{
		var canvas = e.Surface.Canvas;
		float w = e.Info.Width;
		float h = e.Info.Height;
		canvas.Clear(SKColors.Transparent);
		if (w <= 10 || h <= 10) return;

		SKPoint top = new(w * 0.5f, 14f);
		SKPoint bl = new(24f, h - 14f);
		SKPoint br = new(w - 24f, h - 14f);

		using var triPath = new SKPath();
		triPath.MoveTo(top);
		triPath.QuadTo(w * 0.16f, h * 0.45f, bl.X, bl.Y);
		triPath.QuadTo(w * 0.5f, h - 4f, br.X, br.Y);
		triPath.QuadTo(w * 0.84f, h * 0.45f, top.X, top.Y);
		triPath.Close();

		// Multi-lobe radial gradient matching Neoverb's cyan / magenta / orange blend pad
		using (var p = new SKPaint { IsAntialias = true })
		{
			p.Shader = SKShader.CreateRadialGradient(top, h * 0.85f, new[] { new SKColor(14, 165, 233, 170), new SKColor(14, 165, 233, 10) }, null, SKShaderTileMode.Clamp);
			canvas.DrawPath(triPath, p);
			p.Shader = SKShader.CreateRadialGradient(bl, w * 0.72f, new[] { new SKColor(217, 70, 239, 165), new SKColor(217, 70, 239, 10) }, null, SKShaderTileMode.Clamp);
			canvas.DrawPath(triPath, p);
			p.Shader = SKShader.CreateRadialGradient(br, w * 0.72f, new[] { new SKColor(249, 115, 22, 165), new SKColor(249, 115, 22, 10) }, null, SKShaderTileMode.Clamp);
			canvas.DrawPath(triPath, p);
		}

		using var borderPaint = new SKPaint { Style = SKPaintStyle.Stroke, StrokeWidth = 1.6f, IsAntialias = true, Color = new SKColor(255, 255, 255, 95) };
		canvas.DrawPath(triPath, borderPaint);

		double bx = _processor?.BlendX ?? 0.5;
		double by = _processor?.BlendY ?? 0.45;
		float px = (float)(bl.X + (br.X - bl.X) * bx);
		px = (float)(top.X * (1.0 - by) + px * by);
		float py = (float)(top.Y * (1.0 - by) + bl.Y * by);

		using var glowPaint = new SKPaint { IsAntialias = true, Color = new SKColor(255, 255, 255, 65) };
		canvas.DrawCircle(px, py, 16f, glowPaint);
		using var puckPaint = new SKPaint { IsAntialias = true, Color = new SKColor(99, 102, 241, 225) };
		canvas.DrawCircle(px, py, 9f, puckPaint);
		using var puckRing = new SKPaint { IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = 2.2f, Color = SKColors.White };
		canvas.DrawCircle(px, py, 9f, puckRing);
	}

	private void OnBlendPadPointerPressed(object sender, PointerRoutedEventArgs e)
	{
		_draggingPad = true;
		_blendPadCanvas.CapturePointer(e.Pointer);
		UpdatePadFromPointer(e.GetCurrentPoint(_blendPadCanvas).Position);
		e.Handled = true;
	}

	private void OnBlendPadPointerMoved(object sender, PointerRoutedEventArgs e)
	{
		if (!_draggingPad) return;
		UpdatePadFromPointer(e.GetCurrentPoint(_blendPadCanvas).Position);
		e.Handled = true;
	}

	private void OnBlendPadPointerReleased(object sender, PointerRoutedEventArgs e)
	{
		_draggingPad = false;
		try { _blendPadCanvas.ReleasePointerCapture(e.Pointer); } catch { }
	}

	private void UpdatePadFromPointer(Windows.Foundation.Point pt)
	{
		if (_processor == null || _blendPadCanvas.ActualWidth <= 10 || _blendPadCanvas.ActualHeight <= 10) return;
		double w = _blendPadCanvas.ActualWidth;
		double h = _blendPadCanvas.ActualHeight;
		double by = Math.Clamp((pt.Y - 14.0) / Math.Max(20.0, h - 28.0), 0.0, 1.0);
		double spanHalf = Math.Max(0.08, by * 0.5);
		double normX = Math.Clamp((pt.X - 24.0) / Math.Max(20.0, w - 48.0), 0.5 - spanHalf, 0.5 + spanHalf);
		double bx = by < 0.02 ? 0.5 : Math.Clamp((normX - (0.5 - spanHalf)) / (2.0 * spanHalf), 0.0, 1.0);

		_processor.BlendX = bx;
		_processor.BlendY = by;
		UpdateWeightReadouts();
		_blendPadCanvas.Invalidate();
		ParametersChanged?.Invoke();
	}

	private void OnEqCurvePaint(object? sender, SKPaintSurfaceEventArgs e)
	{
		var canvas = e.Surface.Canvas;
		float w = e.Info.Width;
		float h = e.Info.Height;
		canvas.Clear(SKColors.Transparent);
		if (w <= 10 || h <= 10) return;

		float midY = h * 0.5f;
		using var zeroPaint = new SKPaint { Color = new SKColor(255, 255, 255, 45), StrokeWidth = 1f };
		canvas.DrawLine(0, midY, w, midY, zeroPaint);

		var gains = _processor == null
			? new[] { 0.0, 0.0, 0.0 }
			: (_activeEqTab == 0 ? _processor.PreEqGainsDb : _processor.ReverbEqGainsDb);

		float[] xs = { w * 0.16f, w * 0.50f, w * 0.84f };
		float[] ys = new float[3];
		for (int i = 0; i < 3; i++)
		{
			ys[i] = Math.Clamp(midY - (float)(gains[i] / 12.0 * (h * 0.40f)), 6f, h - 6f);
		}

		using var curvePath = new SKPath();
		curvePath.MoveTo(0, ys[0]);
		curvePath.LineTo(xs[0], ys[0]);
		curvePath.CubicTo((xs[0] + xs[1]) * 0.5f, ys[0], (xs[0] + xs[1]) * 0.5f, ys[1], xs[1], ys[1]);
		curvePath.CubicTo((xs[1] + xs[2]) * 0.5f, ys[1], (xs[1] + xs[2]) * 0.5f, ys[2], xs[2], ys[2]);
		curvePath.LineTo(w, ys[2]);

		using var linePaint = new SKPaint { Style = SKPaintStyle.Stroke, StrokeWidth = 2.0f, IsAntialias = true, Color = new SKColor(56, 189, 248, 230) };
		canvas.DrawPath(curvePath, linePaint);

		using var nodePaint = new SKPaint { IsAntialias = true, Color = SKColors.White };
		for (int i = 0; i < 3; i++)
		{
			canvas.DrawCircle(xs[i], ys[i], 4.5f, nodePaint);
		}
	}
}
