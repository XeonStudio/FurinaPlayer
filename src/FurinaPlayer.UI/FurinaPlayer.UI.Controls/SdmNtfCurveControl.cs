using System;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using SkiaSharp;
using SkiaSharp.Views.Windows;
using SonicWave.Audio.Dsp;

namespace FurinaPlayer.UI.Controls;

/// <summary>
/// 高解析度 SDM 噪声传输函数 (Noise Transfer Function, NTF) 曲线控件。
/// 绘制 20Hz 到 100kHz+ 频段内高频量化噪声上推曲线（Noise Shaping High-pass roll-up）。
/// </summary>
public sealed class SdmNtfCurveControl : UserControl
{
	private readonly SKXamlCanvas _canvas;
	private FurinaPlayerSdmModulator _modulator;
	private double _targetRateHz = 5644800.0; // DSD128 default
	private float? _hoverNormX = null;

	public event Action<double, double>? HoverPointChanged;

	public FurinaPlayerSdmModulator Modulator
	{
		get => _modulator;
		set
		{
			_modulator = value;
			_canvas.Invalidate();
		}
	}

	public double TargetRateHz
	{
		get => _targetRateHz;
		set
		{
			_targetRateHz = Math.Max(44100.0, value);
			_canvas.Invalidate();
		}
	}

	public SdmNtfCurveControl()
	{
		_modulator = new FurinaPlayerSdmModulator();
		_canvas = new SKXamlCanvas();
		_canvas.PaintSurface += OnPaintSurface;
		Content = _canvas;

		PointerMoved += OnPointerMoved;
		PointerExited += OnPointerExited;
	}

	public void InvalidateCurve()
	{
		_canvas.Invalidate();
	}

	private void OnPointerMoved(object sender, PointerRoutedEventArgs e)
	{
		var pt = e.GetCurrentPoint(this).Position;
		float padLeft = 48f;
		float padRight = 24f;
		float plotW = Math.Max(1f, (float)ActualWidth - padLeft - padRight);

		float normX = Math.Clamp(((float)pt.X - padLeft) / plotW, 0f, 1f);
		_hoverNormX = normX;

		double minFreq = 20.0;
		double maxFreq = _targetRateHz / 2.0;
		double freq = minFreq * Math.Pow(maxFreq / minFreq, normX);
		double db = _modulator.ComputeNtfDb(freq, _targetRateHz);

		HoverPointChanged?.Invoke(freq, db);
		_canvas.Invalidate();
	}

	private void OnPointerExited(object sender, PointerRoutedEventArgs e)
	{
		_hoverNormX = null;
		_canvas.Invalidate();
	}

	private void OnPaintSurface(object? sender, SKPaintSurfaceEventArgs e)
	{
		SKCanvas canvas = e.Surface.Canvas;
		float width = e.Info.Width;
		float height = e.Info.Height;

		// Fluent clean white background
		canvas.Clear(new SKColor(255, 255, 255, 255));

		if (width <= 60f || height <= 60f) return;

		float padLeft = 48f;
		float padRight = 24f;
		float padTop = 22f;
		float padBottom = 32f;
		float plotW = Math.Max(1f, width - padLeft - padRight);
		float plotH = Math.Max(1f, height - padTop - padBottom);

		double minFreq = 20.0;
		double maxFreq = _targetRateHz / 2.0;
		double logMin = Math.Log10(minFreq);
		double logMax = Math.Log10(maxFreq);

		float XFor(double freq)
		{
			double norm = (Math.Log10(Math.Clamp(freq, minFreq, maxFreq)) - logMin) / (logMax - logMin);
			return padLeft + (float)norm * plotW;
		}

		// dB range: +10 dB down to -160 dB (total 170 dB span)
		double dbMax = 10.0;
		double dbMin = -160.0;
		double dbSpan = dbMax - dbMin;

		float YFor(double db)
		{
			double clamped = Math.Clamp(db, dbMin, dbMax);
			return padTop + (float)((dbMax - clamped) / dbSpan) * plotH;
		}

		// 1. Shaded PCM baseband region (20 Hz to 20,000 Hz)
		float pcmRightX = XFor(Math.Min(20000.0, maxFreq));
		using (var pcmFillPaint = new SKPaint
		{
			Color = new SKColor(37, 132, 94, 25), // Soft emerald green 10% opacity
			Style = SKPaintStyle.Fill
		})
		{
			canvas.DrawRect(padLeft, padTop, Math.Max(0f, pcmRightX - padLeft), plotH, pcmFillPaint);
		}

		using (var pcmTextPaint = new SKPaint
		{
			Color = new SKColor(37, 132, 94, 220),
			TextSize = 10.5f,
			IsAntialias = true,
			FakeBoldText = true
		})
		{
			canvas.DrawText("PCM baseband", padLeft + 10f, padTop + 16f, pcmTextPaint);
		}

		// 2. Horizontal decibel grid lines & labels (+10, 0, -40, -80, -120, -160 dB)
		double[] dbTicks = { 10.0, 0.0, -40.0, -80.0, -120.0, -160.0 };
		using var gridLinePaint = new SKPaint
		{
			Color = new SKColor(227, 229, 233, 200), // Slate line #E3E5E9
			StrokeWidth = 1f,
			IsAntialias = true
		};

		using var dbTextPaint = new SKPaint
		{
			Color = new SKColor(113, 117, 125, 230), // Muted text #71757D
			TextSize = 10f,
			IsAntialias = true,
			TextAlign = SKTextAlign.Right
		};

		foreach (double db in dbTicks)
		{
			float y = YFor(db);
			canvas.DrawLine(padLeft, y, width - padRight, y, gridLinePaint);
			canvas.DrawText(db > 0 ? $"+{db:0}" : $"{db:0}", padLeft - 8f, y + 3.5f, dbTextPaint);
		}

		// 3. Vertical frequency grid lines & labels (20, 1k, 20k, 100k, 1M)
		double[] freqTicks = { 20.0, 1000.0, 20000.0, 100000.0, 1000000.0 };
		string[] freqLabels = { "20", "1k", "20k", "100k", "1M" };

		using var freqTextPaint = new SKPaint
		{
			Color = new SKColor(113, 117, 125, 230),
			TextSize = 10f,
			IsAntialias = true,
			TextAlign = SKTextAlign.Center
		};

		for (int i = 0; i < freqTicks.Length; i++)
		{
			double freq = freqTicks[i];
			if (freq > maxFreq) continue;
			float x = XFor(freq);
			canvas.DrawLine(x, padTop, x, padTop + plotH, gridLinePaint);
			canvas.DrawText(freqLabels[i], x, padTop + plotH + 18f, freqTextPaint);
		}

		// 4. NTF Curve (High-pass roll-up Noise Transfer Function)
		using var curvePaint = new SKPaint
		{
			Color = new SKColor(49, 85, 217, 255), // Indigo / Blue #3155D9
			StrokeWidth = 2.2f,
			Style = SKPaintStyle.Stroke,
			IsAntialias = true,
			StrokeJoin = SKStrokeJoin.Round,
			StrokeCap = SKStrokeCap.Round
		};

		using var path = new SKPath();
		int steps = 260;
		for (int i = 0; i <= steps; i++)
		{
			double norm = (double)i / steps;
			double freq = minFreq * Math.Pow(maxFreq / minFreq, norm);
			double db = _modulator.ComputeNtfDb(freq, _targetRateHz);
			float x = padLeft + (float)norm * plotW;
			float y = YFor(db);

			if (i == 0) path.MoveTo(x, y);
			else path.LineTo(x, y);
		}
		canvas.DrawPath(path, curvePaint);

		// 5. Interactive pointer crosshair and tooltip
		if (_hoverNormX.HasValue)
		{
			float hoverX = padLeft + _hoverNormX.Value * plotW;
			double hoverFreq = minFreq * Math.Pow(maxFreq / minFreq, _hoverNormX.Value);
			double hoverDb = _modulator.ComputeNtfDb(hoverFreq, _targetRateHz);
			float hoverY = YFor(hoverDb);

			// Dashed crosshair lines
			using var crosshairPaint = new SKPaint
			{
				Color = new SKColor(49, 85, 217, 160),
				StrokeWidth = 1f,
				Style = SKPaintStyle.Stroke,
				PathEffect = SKPathEffect.CreateDash(new float[] { 3, 3 }, 0)
			};
			canvas.DrawLine(hoverX, padTop, hoverX, padTop + plotH, crosshairPaint);
			canvas.DrawLine(padLeft, hoverY, width - padRight, hoverY, crosshairPaint);

			// Point dot
			using var dotFill = new SKPaint { Color = SKColors.White, Style = SKPaintStyle.Fill, IsAntialias = true };
			using var dotStroke = new SKPaint { Color = new SKColor(49, 85, 217), StrokeWidth = 2f, Style = SKPaintStyle.Stroke, IsAntialias = true };
			canvas.DrawCircle(hoverX, hoverY, 4.5f, dotFill);
			canvas.DrawCircle(hoverX, hoverY, 4.5f, dotStroke);

			// Readout Badge
			string freqStr = hoverFreq >= 1000000.0 ? $"{hoverFreq / 1000000.0:0.##} MHz" : hoverFreq >= 1000.0 ? $"{hoverFreq / 1000.0:0.#} kHz" : $"{hoverFreq:0} Hz";
			string tagText = $"{freqStr} · {hoverDb:+0.0;-0.0;0.0} dB";

			using var badgeBg = new SKPaint { Color = new SKColor(255, 255, 255, 240), Style = SKPaintStyle.Fill, IsAntialias = true };
			using var badgeBorder = new SKPaint { Color = new SKColor(227, 229, 233), Style = SKPaintStyle.Stroke, StrokeWidth = 1f, IsAntialias = true };
			using var badgeText = new SKPaint { Color = new SKColor(49, 85, 217), TextSize = 10.5f, IsAntialias = true, FakeBoldText = true };

			float tw = badgeText.MeasureText(tagText);
			float bx = Math.Clamp(hoverX - tw / 2f - 8f, padLeft, width - padRight - tw - 16f);
			float by = hoverY > padTop + 40f ? hoverY - 30f : hoverY + 12f;

			canvas.DrawRoundRect(bx, by, tw + 16f, 22f, 6f, 6f, badgeBg);
			canvas.DrawRoundRect(bx, by, tw + 16f, 22f, 6f, 6f, badgeBorder);
			canvas.DrawText(tagText, bx + 8f, by + 15f, badgeText);
		}
	}
}
