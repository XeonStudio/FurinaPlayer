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

	private string _accentHex = "#4CC2FF";

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
		_points = points;
		_accentHex = accentHex;
		Canvas.Invalidate();
	}

	private void OnPaintSurface(object? sender, SKPaintSurfaceEventArgs e)
	{
		SKCanvas canvas = e.Surface.Canvas;
		canvas.Clear(SKColors.Transparent);
		float num = e.Info.Width;
		float num2 = e.Info.Height;
		if (num <= 0f || num2 <= 0f || _points.Count == 0)
		{
			return;
		}
		SKColor color = WaveformControl.ParseColor(_accentHex);
		int count = _points.Count;
		SKPoint[] array = new SKPoint[count];
		float num3 = num2 - 1f;
		for (int i = 0; i < count; i++)
		{
			float num4 = Math.Clamp((float)((_points[i].MagnitudeDb + 80.0) / 80.0), 0f, 1f);
			float x = ((count == 1) ? (num / 2f) : ((float)i / (float)(count - 1) * num));
			float y = num3 - num4 * (num2 - 4f);
			array[i] = new SKPoint(x, y);
		}
		SKPath sKPath = new SKPath();
		sKPath.MoveTo(array[0]);
		for (int j = 0; j < count - 1; j++)
		{
			SKPoint sKPoint = array[Math.Max(0, j - 1)];
			SKPoint sKPoint2 = array[j];
			SKPoint point = array[j + 1];
			SKPoint sKPoint3 = array[Math.Min(count - 1, j + 2)];
			SKPoint point2 = new SKPoint(sKPoint2.X + (point.X - sKPoint.X) / 6f, sKPoint2.Y + (point.Y - sKPoint.Y) / 6f);
			SKPoint point3 = new SKPoint(point.X - (sKPoint3.X - sKPoint2.X) / 6f, point.Y - (sKPoint3.Y - sKPoint2.Y) / 6f);
			sKPath.CubicTo(point2, point3, point);
		}
		SKPath sKPath2 = new SKPath(sKPath);
		sKPath2.LineTo(array[count - 1].X, num2);
		sKPath2.LineTo(array[0].X, num2);
		sKPath2.Close();
		using SKPaint sKPaint = new SKPaint
		{
			IsAntialias = true
		};
		sKPaint.Shader = SKShader.CreateLinearGradient(new SKPoint(0f, 0f), new SKPoint(0f, num2), new SKColor[2]
		{
			color.WithAlpha(95),
			color.WithAlpha(8)
		}, new float[2] { 0f, 1f }, SKShaderTileMode.Clamp);
		canvas.DrawPath(sKPath2, sKPaint);
		using SKPaint paint = new SKPaint
		{
			IsAntialias = true,
			Style = SKPaintStyle.Stroke,
			StrokeWidth = 2f,
			StrokeCap = SKStrokeCap.Round,
			StrokeJoin = SKStrokeJoin.Round,
			Color = color
		};
		canvas.DrawPath(sKPath, paint);
		using SKPaint paint2 = new SKPaint
		{
			IsAntialias = true,
			Color = color.WithAlpha(220)
		};
		canvas.DrawCircle(array[count - 1].X, array[count - 1].Y, 3f, paint2);
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
