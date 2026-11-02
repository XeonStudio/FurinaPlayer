using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Markup;
using SkiaSharp;
using SkiaSharp.Views.Windows;
using SonicWave.Audio.Dsp;
using WinRT;

namespace FurinaPlayer.UI.Controls;

[WinRTRuntimeClassName("Microsoft.UI.Xaml.IUIElementOverrides")]
[WinRTExposedType(typeof(PhaseAnalyzerControlWinRTTypeDetails))]
public sealed class PhaseAnalyzerControl : UserControl, IComponentConnector
{
	private IReadOnlyList<PhaseAnalyzer.LissajousPoint> _points = Array.Empty<PhaseAnalyzer.LissajousPoint>();
	private double _phaseDifference = 0.0;
	private double _stereoCorrelation = 0.0;
	private string _accentHex = "#3ac5ec";

	[GeneratedCode("Microsoft.UI.Xaml.Markup.Compiler", " 3.0.0.2409")]
	private SKXamlCanvas Canvas;

	[GeneratedCode("Microsoft.UI.Xaml.Markup.Compiler", " 3.0.0.2409")]
	private bool _contentLoaded;

	public PhaseAnalyzerControl()
	{
		InitializeComponent();
	}

	public void SetPoints(IReadOnlyList<PhaseAnalyzer.LissajousPoint> points, string accentHex)
	{
		_points = points ?? Array.Empty<PhaseAnalyzer.LissajousPoint>();
		_accentHex = string.IsNullOrEmpty(accentHex) ? "#3ac5ec" : accentHex;
		Canvas?.Invalidate();
	}

	public void SetPhaseData(IReadOnlyList<PhaseAnalyzer.LissajousPoint> points, double phaseDiff, double correlation, string accentHex)
	{
		_points = points ?? Array.Empty<PhaseAnalyzer.LissajousPoint>();
		_phaseDifference = phaseDiff;
		_stereoCorrelation = correlation;
		_accentHex = string.IsNullOrEmpty(accentHex) ? "#3ac5ec" : accentHex;
		Canvas?.Invalidate();
	}

	private void OnPaintSurface(object? sender, SKPaintSurfaceEventArgs e)
	{
		SKCanvas canvas = e.Surface.Canvas;
		float width = e.Info.Width;
		float height = e.Info.Height;

		// 1. Audition DAW Dark Background
		canvas.Clear(new SKColor(22, 25, 29)); // #16191d

		if (width <= 20f || height <= 20f)
		{
			return;
		}

		// Allocate space: Left/Center is Goniometer radar, Right 36px is Correlation Meter
		float meterWidth = 32f;
		float plotW = width - meterWidth;
		float plotH = height;

		float cx = plotW / 2f;
		float cy = plotH / 2f;
		float maxR = Math.Min(cx, cy) - 16f;
		if (maxR <= 5f) maxR = 5f;

		// Grid & Guide Paints
		using var polarGridPaint = new SKPaint
		{
			Color = new SKColor(38, 45, 54, 180),
			Style = SKPaintStyle.Stroke,
			StrokeWidth = 1f,
			IsAntialias = true
		};

		using var axisPaint = new SKPaint
		{
			Color = new SKColor(48, 56, 68),
			Style = SKPaintStyle.Stroke,
			StrokeWidth = 1f,
			IsAntialias = true
		};

		using var stereoAxisPaint = new SKPaint
		{
			Color = new SKColor(58, 197, 236, 120), // #3ac5ec with alpha
			Style = SKPaintStyle.Stroke,
			StrokeWidth = 1.2f,
			IsAntialias = true
		};

		using var textPaint = new SKPaint
		{
			Color = new SKColor(127, 140, 155),
			TextSize = 10f,
			IsAntialias = true
		};

		using var labelPaint = new SKPaint
		{
			Color = new SKColor(58, 197, 236),
			TextSize = 11f,
			IsAntialias = true,
			FakeBoldText = true
		};

		// 2. Draw Concentric Polar Circles (0dB, -6dB, -12dB)
		canvas.DrawCircle(cx, cy, maxR, polarGridPaint);
		canvas.DrawCircle(cx, cy, maxR * 0.5f, polarGridPaint);
		canvas.DrawCircle(cx, cy, maxR * 0.25f, polarGridPaint);

		// 3. Draw M/S Orthogonal Axes (Mid vertical, Side horizontal)
		canvas.DrawLine(cx, cy - maxR, cx, cy + maxR, axisPaint);
		canvas.DrawLine(cx - maxR, cy, cx + maxR, cy, axisPaint);

		// 4. Draw 45-degree L and R Angled Stereo Axes
		float diag = maxR * 0.7071f;
		// Left: up-left
		canvas.DrawLine(cx, cy, cx - diag, cy - diag, stereoAxisPaint);
		canvas.DrawText("L", cx - diag - 12f, cy - diag - 2f, labelPaint);
		// Right: up-right
		canvas.DrawLine(cx, cy, cx + diag, cy - diag, stereoAxisPaint);
		canvas.DrawText("R", cx + diag + 4f, cy - diag - 2f, labelPaint);

		// 5. Draw Lissajous Phosphor Sound Field Points
		if (_points.Count > 0)
		{
			SKColor cyan = WaveformControl.ParseColor(_accentHex);
			using var glowPaint = new SKPaint
			{
				Color = cyan.WithAlpha(45),
				IsAntialias = true
			};

			using var corePaint = new SKPaint
			{
				Color = new SKColor(0, 230, 118, 210), // #00e676 phosphorescent green/cyan
				IsAntialias = true
			};

			// Rotate 45 degrees into standard Sound Field / Goniometer coordinates
			// u = (L - R) / sqrt(2)  [Horizontal Side component]
			// v = (L + R) / sqrt(2)  [Vertical Mid component]
			foreach (var pt in _points)
			{
				double l = pt.X;
				double r = pt.Y;
				double u = (l - r) * 0.7071;
				double v = (l + r) * 0.7071;

				float px = (float)(cx + u * maxR);
				float py = (float)(cy - v * maxR);

				// Draw soft phosphor glow then crisp center
				canvas.DrawCircle(px, py, 2.5f, glowPaint);
				canvas.DrawCircle(px, py, 1.2f, corePaint);
			}
		}

		// 6. Right-side Vertical Correlation Meter
		float meterX = width - 20f;
		float meterTop = 16f;
		float meterBottom = height - 26f;
		float meterHeight = meterBottom - meterTop;
		float meterMidY = meterTop + meterHeight * 0.5f;

		// Track
		using var meterTrackPaint = new SKPaint
		{
			Color = new SKColor(32, 38, 46),
			Style = SKPaintStyle.Fill,
			IsAntialias = true
		};
		canvas.DrawRoundRect(meterX - 3f, meterTop, 6f, meterHeight, 3f, 3f, meterTrackPaint);

		// Ticks and labels: +1, 0, -1
		canvas.DrawText("+1", meterX + 6f, meterTop + 8f, textPaint);
		canvas.DrawText("0", meterX + 6f, meterMidY + 3.5f, textPaint);
		canvas.DrawText("-1", meterX + 6f, meterBottom, textPaint);

		// Dynamic level indicator
		float corr = Math.Clamp((float)_stereoCorrelation, -1f, 1f);
		float barHeight = Math.Abs(corr) * (meterHeight * 0.5f);
		SKColor meterColor = corr >= 0.2f
			? new SKColor(0, 230, 118) // Green: in-phase
			: (corr >= -0.2f ? new SKColor(255, 179, 0) : new SKColor(255, 82, 82)); // Red: out-of-phase

		using var meterBarPaint = new SKPaint
		{
			Color = meterColor,
			Style = SKPaintStyle.Fill,
			IsAntialias = true
		};

		if (corr > 0.01f)
		{
			canvas.DrawRoundRect(meterX - 3f, meterMidY - barHeight, 6f, barHeight, 2f, 2f, meterBarPaint);
		}
		else if (corr < -0.01f)
		{
			canvas.DrawRoundRect(meterX - 3f, meterMidY, 6f, barHeight, 2f, 2f, meterBarPaint);
		}
		else
		{
			canvas.DrawCircle(meterX, meterMidY, 3f, meterBarPaint);
		}

		// 7. HUD Text Overlay at Bottom-Right
		string infoText = $"相位差: {_phaseDifference:F1}°   相关度: {_stereoCorrelation:+0.00;-0.00;0.00}";
		using var hudTextPaint = new SKPaint
		{
			Color = new SKColor(160, 175, 192),
			TextSize = 10f,
			IsAntialias = true
		};
		float hudWidth = hudTextPaint.MeasureText(infoText);
		canvas.DrawText(infoText, plotW - hudWidth - 8f, height - 8f, hudTextPaint);
	}

	[GeneratedCode("Microsoft.UI.Xaml.Markup.Compiler", " 3.0.0.2409")]
	[DebuggerNonUserCode]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("ms-appx:///FurinaPlayer.UI/Controls/PhaseAnalyzerControl.xaml");
			Application.LoadComponent(this, resourceLocator, ComponentResourceLocation.Nested);
		}
	}

	[GeneratedCode("Microsoft.UI.Xaml.Markup.Compiler", " 3.0.0.2409")]
	[DebuggerNonUserCode]
	public void Connect(int connectionId, object target)
	{
		if (connectionId == 2)
		{
			Canvas = target.As<SKXamlCanvas>();
			Canvas.PaintSurface += OnPaintSurface;
		}
		_contentLoaded = true;
	}

	[GeneratedCode("Microsoft.UI.Xaml.Markup.Compiler", " 3.0.0.2409")]
	[DebuggerNonUserCode]
	public IComponentConnector GetBindingConnector(int connectionId, object target)
	{
		return null;
	}
}
