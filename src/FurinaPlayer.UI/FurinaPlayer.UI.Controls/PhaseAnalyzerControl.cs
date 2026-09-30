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

	private string _accentHex = "#4CC2FF";

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
		if (num <= 0f || num2 <= 0f)
		{
			return;
		}
		SKColor sKColor = WaveformControl.ParseColor(_accentHex);
		using SKPaint paint = new SKPaint
		{
			Color = SKColors.Gray.WithAlpha(90),
			StrokeWidth = 1f
		};
		canvas.DrawLine(num / 2f, 0f, num / 2f, num2, paint);
		canvas.DrawLine(0f, num2 / 2f, num, num2 / 2f, paint);
		if (_points.Count == 0)
		{
			return;
		}
		using SKPaint paint2 = new SKPaint
		{
			Color = sKColor.WithAlpha(200),
			IsAntialias = true
		};
		float num3 = Math.Min(num, num2) / 2f - 4f;
		foreach (PhaseAnalyzer.LissajousPoint point in _points)
		{
			canvas.DrawCircle(num / 2f + (float)point.X * num3, num2 / 2f - (float)point.Y * num3, 1.4f, paint2);
		}
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
