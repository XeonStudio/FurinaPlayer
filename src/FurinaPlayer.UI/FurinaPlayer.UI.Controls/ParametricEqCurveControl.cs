using System;
using System.Collections.Generic;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using SkiaSharp;
using SkiaSharp.Views.Windows;
using SonicWave.Audio.Dsp;

namespace FurinaPlayer.UI.Controls;

public sealed class ParametricEqCurveControl : UserControl
{
	private readonly SKXamlCanvas _canvas;
	private readonly List<EchoBiquadFilter> _bands = new();
	private int _selectedBandIndex = 0;
	private double _preampDb = 0.0;
	private double _dbRange = 12.0; // ±12 dB
	private bool _showSpectrum = false;
	private float[]? _spectrumData;

	private int _draggingBandIndex = -1;
	private float _lastPointerX;
	private float _lastPointerY;

	public event Action<int, double, double>? BandChanged;
	public event Action<int>? BandSelected;

	public IReadOnlyList<EchoBiquadFilter> Bands => _bands;

	public int SelectedBandIndex
	{
		get => _selectedBandIndex;
		set
		{
			_selectedBandIndex = Math.Clamp(value, 0, Math.Max(0, _bands.Count - 1));
			_canvas.Invalidate();
		}
	}

	public double PreampDb
	{
		get => _preampDb;
		set
		{
			_preampDb = value;
			_canvas.Invalidate();
		}
	}

	public double DbRange
	{
		get => _dbRange;
		set
		{
			_dbRange = Math.Clamp(value, 6.0, 30.0);
			_canvas.Invalidate();
		}
	}

	public bool ShowSpectrum
	{
		get => _showSpectrum;
		set
		{
			_showSpectrum = value;
			_canvas.Invalidate();
		}
	}

	public ParametricEqCurveControl()
	{
		_canvas = new SKXamlCanvas();
		_canvas.PaintSurface += OnPaintSurface;
		Content = _canvas;

		PointerPressed += OnPointerPressed;
		PointerMoved += OnPointerMoved;
		PointerReleased += OnPointerReleased;
		PointerCaptureLost += OnPointerReleased;
	}

	public void SetBands(IEnumerable<EchoBiquadFilter> bands, int selectedIndex = 0)
	{
		_bands.Clear();
		if (bands != null)
		{
			_bands.AddRange(bands);
		}
		_selectedBandIndex = Math.Clamp(selectedIndex, 0, Math.Max(0, _bands.Count - 1));
		_canvas.Invalidate();
	}

	public void SetSpectrumData(float[]? spectrum)
	{
		_spectrumData = spectrum;
		if (_showSpectrum)
		{
			_canvas.Invalidate();
		}
	}

	private void OnPointerPressed(object sender, PointerRoutedEventArgs e)
	{
		var pt = e.GetCurrentPoint(this).Position;
		float x = (float)pt.X;
		float y = (float)pt.Y;

		float padLeft = 40f;
		float padRight = 20f;
		float padTop = 30f;
		float padBottom = 26f;
		float plotW = Math.Max(1f, (float)ActualWidth - padLeft - padRight);
		float plotH = Math.Max(1f, (float)ActualHeight - padTop - padBottom);

		double logMin = Math.Log10(20.0);
		double logMax = Math.Log10(20000.0);

		// Find closest band node
		int bestIdx = -1;
		float bestDist = 28f; // Hit radius

		for (int i = 0; i < _bands.Count; i++)
		{
			var band = _bands[i];
			float normX = (float)((Math.Log10(Math.Clamp(band.FrequencyHz, 20.0, 20000.0)) - logMin) / (logMax - logMin));
			float nodeX = padLeft + normX * plotW;

			float normY = (float)((_dbRange - band.GainDb) / (2.0 * _dbRange));
			float nodeY = padTop + normY * plotH;

			float dist = (float)Math.Sqrt((x - nodeX) * (x - nodeX) + (y - nodeY) * (y - nodeY));
			if (dist < bestDist)
			{
				bestDist = dist;
				bestIdx = i;
			}
		}

		if (bestIdx >= 0)
		{
			_draggingBandIndex = bestIdx;
			SelectedBandIndex = bestIdx;
			BandSelected?.Invoke(bestIdx);
			_lastPointerX = x;
			_lastPointerY = y;
			CapturePointer(e.Pointer);
			e.Handled = true;
		}
	}

	private void OnPointerMoved(object sender, PointerRoutedEventArgs e)
	{
		if (_draggingBandIndex < 0 || _draggingBandIndex >= _bands.Count) return;

		var pt = e.GetCurrentPoint(this).Position;
		float x = (float)pt.X;
		float y = (float)pt.Y;

		float padLeft = 40f;
		float padRight = 20f;
		float padTop = 30f;
		float padBottom = 26f;
		float plotW = Math.Max(1f, (float)ActualWidth - padLeft - padRight);
		float plotH = Math.Max(1f, (float)ActualHeight - padTop - padBottom);

		double logMin = Math.Log10(20.0);
		double logMax = Math.Log10(20000.0);

		float normX = Math.Clamp((x - padLeft) / plotW, 0f, 1f);
		double newFreq = Math.Pow(10.0, logMin + normX * (logMax - logMin));

		float normY = Math.Clamp((y - padTop) / plotH, 0f, 1f);
		double newGain = _dbRange - normY * (2.0 * _dbRange);

		var band = _bands[_draggingBandIndex];
		band.FrequencyHz = Math.Round(newFreq, 1);
		band.GainDb = Math.Round(newGain, 1);
		band.UpdateCoefficients(48000.0);

		BandChanged?.Invoke(_draggingBandIndex, band.FrequencyHz, band.GainDb);
		_canvas.Invalidate();
		e.Handled = true;
	}

	private void OnPointerReleased(object sender, PointerRoutedEventArgs e)
	{
		if (_draggingBandIndex >= 0)
		{
			_draggingBandIndex = -1;
			ReleasePointerCapture(e.Pointer);
			e.Handled = true;
		}
	}

	private void OnPaintSurface(object? sender, SKPaintSurfaceEventArgs e)
	{
		SKCanvas canvas = e.Surface.Canvas;
		float width = e.Info.Width;
		float height = e.Info.Height;

		// Audition / ECHO light mode white acrylic translucent background
		canvas.Clear(new SKColor(255, 255, 255, 225));

		if (width <= 50f || height <= 50f) return;

		float padLeft = 40f;
		float padRight = 20f;
		float padTop = 30f;
		float padBottom = 26f;
		float plotW = Math.Max(1f, width - padLeft - padRight);
		float plotH = Math.Max(1f, height - padTop - padBottom);

		double logMin = Math.Log10(20.0);
		double logMax = Math.Log10(20000.0);

		// 1. Shaded Band Regions at the top (light mode gentle pastel bands)
		var regions = new (double Start, double End, string Name, SKColor Bg)[]
		{
			(20, 250, "低频", new SKColor(241, 245, 249, 160)),
			(250, 500, "中低频", new SKColor(238, 242, 255, 160)),
			(500, 2000, "中频", new SKColor(240, 253, 244, 160)),
			(2000, 6000, "临场感", new SKColor(254, 249, 195, 140)),
			(6000, 20000, "明亮", new SKColor(243, 244, 246, 160))
		};

		using var regionTextPaint = new SKPaint
		{
			Color = new SKColor(100, 116, 139), // Slate 500
			TextSize = 10.5f,
			IsAntialias = true,
			FakeBoldText = true
		};

		foreach (var r in regions)
		{
			float x1 = padLeft + (float)((Math.Log10(r.Start) - logMin) / (logMax - logMin)) * plotW;
			float x2 = padLeft + (float)((Math.Log10(r.End) - logMin) / (logMax - logMin)) * plotW;
			using var rPaint = new SKPaint { Color = r.Bg, Style = SKPaintStyle.Fill };
			canvas.DrawRect(x1, padTop, x2 - x1, plotH, rPaint);

			float tw = regionTextPaint.MeasureText(r.Name);
			float center = (x1 + x2) / 2f - tw / 2f;
			canvas.DrawText(r.Name, center, padTop - 10f, regionTextPaint);
		}

		// 2. Frequency Vertical Grid Lines
		double[] freqTicks = { 20, 31.5, 50, 80, 125, 200, 315, 500, 800, 1300, 2000, 3200, 5000, 8000, 12500, 20000 };
		string[] freqLabels = { "20", "31.5", "50", "80", "125", "200", "315", "500", "800", "1.3k", "2k", "3.2k", "5k", "8k", "12.5k", "20k" };

		using var gridLinePaint = new SKPaint
		{
			Color = new SKColor(226, 232, 240, 220), // Slate 200 (#E2E8F0)
			StrokeWidth = 1f,
			IsAntialias = true
		};

		using var freqTextPaint = new SKPaint
		{
			Color = new SKColor(71, 85, 105), // Slate 600
			TextSize = 10f,
			IsAntialias = true
		};

		for (int i = 0; i < freqTicks.Length; i++)
		{
			float normX = (float)((Math.Log10(freqTicks[i]) - logMin) / (logMax - logMin));
			if (normX < 0f || normX > 1f) continue;
			float x = padLeft + normX * plotW;
			canvas.DrawLine(x, padTop, x, padTop + plotH, gridLinePaint);

			float tw = freqTextPaint.MeasureText(freqLabels[i]);
			canvas.DrawText(freqLabels[i], x - tw / 2f, padTop + plotH + 16f, freqTextPaint);
		}

		// 3. Decibel Horizontal Grid Lines
		double[] dbTicks = { _dbRange, _dbRange * 0.5, 0.0, -_dbRange * 0.5, -_dbRange };
		using var zeroLinePaint = new SKPaint
		{
			Color = new SKColor(99, 102, 241, 200), // Blue-violet 0 dB reference line (#6366F1)
			StrokeWidth = 1.4f,
			IsAntialias = true
		};

		using var dbTextPaint = new SKPaint
		{
			Color = new SKColor(71, 85, 105), // Slate 600
			TextSize = 10f,
			IsAntialias = true
		};

		foreach (double db in dbTicks)
		{
			float normY = (float)((_dbRange - db) / (2.0 * _dbRange));
			float y = padTop + normY * plotH;

			if (Math.Abs(db) < 0.01)
			{
				canvas.DrawLine(padLeft, y, padLeft + plotW, y, zeroLinePaint);
			}
			else
			{
				canvas.DrawLine(padLeft, y, padLeft + plotW, y, gridLinePaint);
			}

			string label = db > 0 ? $"+{db:0} dB" : $"{db:0} dB";
			canvas.DrawText(label, 4f, y + 3.5f, dbTextPaint);
		}

		// 4. Draw Composite EQ Frequency Response Curve
		int pointsCount = 300;
		SKPoint[] curvePoints = new SKPoint[pointsCount];

		for (int i = 0; i < pointsCount; i++)
		{
			double norm = (double)i / (pointsCount - 1);
			double freq = Math.Pow(10.0, logMin + norm * (logMax - logMin));
			double totalDb = _preampDb;

			foreach (var band in _bands)
			{
				if (band != null && band.Enabled)
				{
					totalDb += band.CalculateResponseDb(freq, 48000.0);
				}
			}

			float x = padLeft + (float)norm * plotW;
			float normY = (float)((_dbRange - totalDb) / (2.0 * _dbRange));
			float y = padTop + Math.Clamp(normY, -0.1f, 1.1f) * plotH;
			curvePoints[i] = new SKPoint(x, y);
		}

		using var curvePath = new SKPath();
		curvePath.MoveTo(curvePoints[0]);
		for (int i = 1; i < pointsCount; i++)
		{
			curvePath.LineTo(curvePoints[i]);
		}

		// Fill area under curve
		using var fillPath = new SKPath(curvePath);
		float zeroY = padTop + 0.5f * plotH;
		fillPath.LineTo(curvePoints[pointsCount - 1].X, zeroY);
		fillPath.LineTo(curvePoints[0].X, zeroY);
		fillPath.Close();

		using var areaPaint = new SKPaint
		{
			IsAntialias = true,
			Color = new SKColor(99, 102, 241, 35), // Indigo gentle fill
			Style = SKPaintStyle.Fill
		};
		canvas.DrawPath(fillPath, areaPaint);

		using var curveStrokePaint = new SKPaint
		{
			IsAntialias = true,
			Style = SKPaintStyle.Stroke,
			StrokeWidth = 2.6f,
			StrokeCap = SKStrokeCap.Round,
			StrokeJoin = SKStrokeJoin.Round,
			Color = new SKColor(79, 70, 229) // Indigo 600 (#4F46E5) high contrast on light bg
		};
		canvas.DrawPath(curvePath, curveStrokePaint);

		// 5. Draw Numbered Band Badges
		SKColor[] bandColors = {
			new SKColor(171, 71, 188), // Purple
			new SKColor(66, 165, 245),  // Blue
			new SKColor(38, 166, 154),  // Teal
			new SKColor(102, 187, 106), // Green
			new SKColor(255, 167, 38),  // Orange
			new SKColor(239, 83, 80),   // Red
			new SKColor(126, 87, 194),  // Deep Purple
			new SKColor(41, 182, 246),  // Light Blue
			new SKColor(141, 110, 99),  // Brown
			new SKColor(120, 144, 156)  // Blue Grey
		};

		using var badgeTextPaint = new SKPaint
		{
			Color = SKColors.White,
			TextSize = 10f,
			IsAntialias = true,
			FakeBoldText = true
		};

		for (int i = 0; i < _bands.Count; i++)
		{
			var band = _bands[i];
			float normX = (float)((Math.Log10(Math.Clamp(band.FrequencyHz, 20.0, 20000.0)) - logMin) / (logMax - logMin));
			float nodeX = padLeft + Math.Clamp(normX, 0f, 1f) * plotW;

			float normY = (float)((_dbRange - band.GainDb) / (2.0 * _dbRange));
			float nodeY = padTop + Math.Clamp(normY, 0f, 1f) * plotH;

			bool isSelected = i == _selectedBandIndex;
			SKColor color = bandColors[i % bandColors.Length];

			if (isSelected)
			{
				// Draw dashed guidelines to axes
				using var dashPaint = new SKPaint
				{
					Color = color.WithAlpha(160),
					StrokeWidth = 1f,
					PathEffect = SKPathEffect.CreateDash(new float[] { 4f, 4f }, 0f),
					IsAntialias = true
				};
				canvas.DrawLine(nodeX, padTop, nodeX, padTop + plotH, dashPaint);
				canvas.DrawLine(padLeft, nodeY, padLeft + plotW, nodeY, dashPaint);

				// Outer selection halo
				using var haloPaint = new SKPaint
				{
					Color = color.WithAlpha(60),
					IsAntialias = true,
					Style = SKPaintStyle.Fill
				};
				canvas.DrawCircle(nodeX, nodeY, 18f, haloPaint);
			}

			// Badge shadow for depth on white background
			using var shadowPaint = new SKPaint
			{
				Color = new SKColor(15, 23, 42, 40),
				IsAntialias = true,
				Style = SKPaintStyle.Fill
			};
			canvas.DrawCircle(nodeX, nodeY + 1f, isSelected ? 12f : 10f, shadowPaint);

			// Badge circle
			using var nodeFill = new SKPaint
			{
				Color = isSelected ? color : color.WithAlpha(220),
				IsAntialias = true,
				Style = SKPaintStyle.Fill
			};
			canvas.DrawCircle(nodeX, nodeY, isSelected ? 11f : 9f, nodeFill);

			using var nodeBorder = new SKPaint
			{
				Color = SKColors.White,
				StrokeWidth = isSelected ? 2.2f : 1.4f,
				IsAntialias = true,
				Style = SKPaintStyle.Stroke
			};
			canvas.DrawCircle(nodeX, nodeY, isSelected ? 11f : 9f, nodeBorder);

			// Number text
			string numStr = $"{i + 1}";
			float tw = badgeTextPaint.MeasureText(numStr);
			canvas.DrawText(numStr, nodeX - tw / 2f, nodeY + 3.5f, badgeTextPaint);
		}
	}
}
