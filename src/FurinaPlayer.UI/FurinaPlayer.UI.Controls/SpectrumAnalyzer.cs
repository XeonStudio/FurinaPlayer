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
[WinRTExposedType(typeof(SpectrumAnalyzerWinRTTypeDetails))]
public sealed class SpectrumAnalyzer : UserControl, IComponentConnector
{
	private IReadOnlyList<SpectrumPoint> _points = Array.Empty<SpectrumPoint>();
	private string _accentHex = "#3ac5ec";

	[GeneratedCode("Microsoft.UI.Xaml.Markup.Compiler", " 3.0.0.2409")]
	private SKXamlCanvas Canvas;

	[GeneratedCode("Microsoft.UI.Xaml.Markup.Compiler", " 3.0.0.2409")]
	private bool _contentLoaded;

	public SpectrumAnalyzer()
	{
		InitializeComponent();
	}

	public void SetPoints(IReadOnlyList<SpectrumPoint> points, string accentHex)
	{
		_points = points ?? Array.Empty<SpectrumPoint>();
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

		if (width <= 10f || height <= 10f)
		{
			return;
		}

		float padLeft = 32f;
		float padRight = 10f;
		float padTop = 14f;
		float padBottom = 20f;
		float plotW = Math.Max(1f, width - padLeft - padRight);
		float plotH = Math.Max(1f, height - padTop - padBottom);

		// Grid Paints
		using var gridPaint = new SKPaint
		{
			Color = new SKColor(38, 45, 54, 180), // #262d36
			StrokeWidth = 1f,
			IsAntialias = true
		};

		using var borderPaint = new SKPaint
		{
			Color = new SKColor(48, 56, 68),
			StrokeWidth = 1f,
			Style = SKPaintStyle.Stroke,
			IsAntialias = true
		};

		using var textPaint = new SKPaint
		{
			Color = new SKColor(127, 140, 155),
			TextSize = 10f,
			IsAntialias = true
		};

		// 2. Draw dB Horizontal Grid Lines (-72 to 0 dBFS)
		int[] dbTicks = { 0, -12, -24, -36, -48, -60, -72 };
		foreach (int db in dbTicks)
		{
			float norm = (0f - db) / 72f; // 0 at top, 1 at bottom
			float y = padTop + norm * plotH;
			canvas.DrawLine(padLeft, y, padLeft + plotW, y, gridPaint);

			// Text label
			string label = $"{db}";
			canvas.DrawText(label, 4f, y + 3.5f, textPaint);
		}

		// 3. Draw Logarithmic Frequency Vertical Grid Lines
		double[] freqTicks = { 50, 100, 250, 500, 1000, 2000, 4000, 8000, 16000 };
		string[] freqLabels = { "50", "100", "250", "500", "1k", "2k", "4k", "8k", "16k" };
		double logMin = Math.Log10(20.0);
		double logMax = Math.Log10(20000.0);

		for (int i = 0; i < freqTicks.Length; i++)
		{
			double freq = freqTicks[i];
			float normX = (float)((Math.Log10(freq) - logMin) / (logMax - logMin));
			if (normX < 0f || normX > 1f) continue;
			float x = padLeft + normX * plotW;

			canvas.DrawLine(x, padTop, x, padTop + plotH, gridPaint);

			// Text label
			float labelWidth = textPaint.MeasureText(freqLabels[i]);
			canvas.DrawText(freqLabels[i], x - labelWidth / 2f, padTop + plotH + 14f, textPaint);
		}

		// Draw border
		canvas.DrawRect(padLeft, padTop, plotW, plotH, borderPaint);

		// 4. Draw Spectrum Curve & Gradient Fill
		if (_points.Count > 0)
		{
			int count = _points.Count;
			SKPoint[] curvePoints = new SKPoint[count];
			double maxDb = -120.0;
			double maxDbFreq = 0.0;

			for (int i = 0; i < count; i++)
			{
				var pt = _points[i];
				double f = Math.Clamp(pt.Frequency, 20.0, 20000.0);
				float normX = (float)((Math.Log10(f) - logMin) / (logMax - logMin));
				float x = padLeft + Math.Clamp(normX, 0f, 1f) * plotW;

				double db = Math.Clamp(pt.MagnitudeDb, -72.0, 0.0);
				float normY = (float)((0.0 - db) / 72.0);
				float y = padTop + normY * plotH;

				curvePoints[i] = new SKPoint(x, y);

				if (pt.MagnitudeDb > maxDb)
				{
					maxDb = pt.MagnitudeDb;
					maxDbFreq = pt.Frequency;
				}
			}

			// Path creation
			using var linePath = new SKPath();
			linePath.MoveTo(curvePoints[0]);
			for (int j = 0; j < count - 1; j++)
			{
				SKPoint p0 = curvePoints[Math.Max(0, j - 1)];
				SKPoint p1 = curvePoints[j];
				SKPoint p2 = curvePoints[j + 1];
				SKPoint p3 = curvePoints[Math.Min(count - 1, j + 2)];

				SKPoint cp1 = new SKPoint(p1.X + (p2.X - p0.X) / 6f, p1.Y + (p2.Y - p0.Y) / 6f);
				SKPoint cp2 = new SKPoint(p2.X - (p3.X - p1.X) / 6f, p2.Y - (p3.Y - p1.Y) / 6f);
				linePath.CubicTo(cp1, cp2, p2);
			}

			// Filled area under spectrum
			using var areaPath = new SKPath(linePath);
			areaPath.LineTo(curvePoints[count - 1].X, padTop + plotH);
			areaPath.LineTo(curvePoints[0].X, padTop + plotH);
			areaPath.Close();

			SKColor cyan = WaveformControl.ParseColor(_accentHex);
			using var fillPaint = new SKPaint
			{
				IsAntialias = true,
				Shader = SKShader.CreateLinearGradient(
					new SKPoint(0f, padTop),
					new SKPoint(0f, padTop + plotH),
					new SKColor[] { cyan.WithAlpha(115), cyan.WithAlpha(8) },
					new float[] { 0f, 1f },
					SKShaderTileMode.Clamp)
			};
			canvas.DrawPath(areaPath, fillPaint);

			// Stroke curve
			using var strokePaint = new SKPaint
			{
				IsAntialias = true,
				Style = SKPaintStyle.Stroke,
				StrokeWidth = 2f,
				StrokeCap = SKStrokeCap.Round,
				StrokeJoin = SKStrokeJoin.Round,
				Color = cyan
			};
			canvas.DrawPath(linePath, strokePaint);

			// Peak readout in HUD
			if (maxDb > -65.0)
			{
				using var hudPaint = new SKPaint
				{
					Color = new SKColor(58, 197, 236),
					TextSize = 10.5f,
					IsAntialias = true
				};
				string peakText = $"峰值: {maxDbFreq:F0} Hz · {maxDb:F1} dBFS";
				float tw = hudPaint.MeasureText(peakText);
				canvas.DrawText(peakText, padLeft + plotW - tw - 8f, padTop + 14f, hudPaint);
			}
		}
	}

	[GeneratedCode("Microsoft.UI.Xaml.Markup.Compiler", " 3.0.0.2409")]
	[DebuggerNonUserCode]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("ms-appx:///FurinaPlayer.UI/Controls/SpectrumAnalyzer.xaml");
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
