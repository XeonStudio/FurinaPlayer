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
using SonicWave.Audio.Renderers;
using WinRT;

namespace FurinaPlayer.UI.Controls;

[WinRTRuntimeClassName("Microsoft.UI.Xaml.IUIElementOverrides")]
[WinRTExposedType(typeof(WaveformControlWinRTTypeDetails))]
public sealed class WaveformControl : UserControl, IComponentConnector
{
	private IReadOnlyList<WaveformRenderer.PeakPair> _peaks = Array.Empty<WaveformRenderer.PeakPair>();

	private double _selectionStart;

	private double _selectionEnd = 1.0;

	private double _playhead;

	private string _accentHex = "#4CC2FF";

	private const string EmptyHint = "未加载音频文件，无法显示波形";

	private static readonly SKTypeface CjkTypeface = ResolveCjkTypeface();

	[GeneratedCode("Microsoft.UI.Xaml.Markup.Compiler", " 3.0.0.2409")]
	private SKXamlCanvas Canvas;

	[GeneratedCode("Microsoft.UI.Xaml.Markup.Compiler", " 3.0.0.2409")]
	private bool _contentLoaded;

	private static SKTypeface ResolveCjkTypeface()
	{
		string[] array = new string[5] { "Microsoft YaHei UI", "Microsoft YaHei", "SimSun", "PingFang SC", "Noto Sans CJK SC" };
		for (int i = 0; i < array.Length; i++)
		{
			SKTypeface sKTypeface = SKTypeface.FromFamilyName(array[i]);
			if (sKTypeface != null && sKTypeface.FamilyName.Length > 0)
			{
				return sKTypeface;
			}
		}
		return SKTypeface.Default;
	}

	public WaveformControl()
	{
		InitializeComponent();
	}

	public void SetPeaks(IReadOnlyList<WaveformRenderer.PeakPair> peaks, double selectionStartFraction, double selectionEndFraction, double playheadFraction, string accentHex)
	{
		_peaks = peaks;
		_selectionStart = selectionStartFraction;
		_selectionEnd = selectionEndFraction;
		_playhead = playheadFraction;
		_accentHex = accentHex;
		Canvas.Invalidate();
	}

	public void SetSelection(double selectionStartFraction, double selectionEndFraction)
	{
		_selectionStart = selectionStartFraction;
		_selectionEnd = selectionEndFraction;
		Canvas.Invalidate();
	}

	public void SetPlayhead(double fraction)
	{
		_playhead = fraction;
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
		SKColor sKColor = ParseColor(_accentHex);
		using SKPaint paint = new SKPaint
		{
			Color = sKColor.WithAlpha(60),
			StrokeWidth = 1f,
			IsAntialias = true
		};
		canvas.DrawLine(0f, num2 / 2f, num, num2 / 2f, paint);
		if (_selectionEnd > _selectionStart)
		{
			using SKPaint paint2 = new SKPaint
			{
				Color = sKColor.WithAlpha(40)
			};
			canvas.DrawRect((float)(_selectionStart * (double)num), 0f, (float)((_selectionEnd - _selectionStart) * (double)num), num2, paint2);
			using SKPaint paint3 = new SKPaint
			{
				Color = sKColor.WithAlpha(140),
				StrokeWidth = 1f
			};
			canvas.DrawLine((float)(_selectionStart * (double)num), 0f, (float)(_selectionStart * (double)num), num2, paint3);
			canvas.DrawLine((float)(_selectionEnd * (double)num), 0f, (float)(_selectionEnd * (double)num), num2, paint3);
		}
		if (_peaks.Count == 0)
		{
			using (SKPaint sKPaint = new SKPaint
			{
				Color = SKColors.Gray.WithAlpha(160),
				TextSize = 14f,
				IsAntialias = true,
				Typeface = CjkTypeface
			})
			{
				float num3 = sKPaint.MeasureText("未加载音频文件，无法显示波形");
				canvas.DrawText("未加载音频文件，无法显示波形", (num - num3) / 2f, num2 / 2f + 5f, sKPaint);
				return;
			}
		}
		float strokeWidth = Math.Max(1f, num / (float)_peaks.Count - 0.5f);
		using SKPaint paint4 = new SKPaint
		{
			Color = sKColor.WithAlpha(220),
			StrokeWidth = strokeWidth,
			IsAntialias = true,
			StrokeCap = SKStrokeCap.Round
		};
		for (int i = 0; i < _peaks.Count; i++)
		{
			float num4 = ((float)i + 0.5f) * num / (float)_peaks.Count;
			float y = num2 / 2f - Math.Max(0f, _peaks[i].Max) * num2 / 2f;
			float y2 = num2 / 2f - Math.Min(0f, _peaks[i].Min) * num2 / 2f;
			canvas.DrawLine(num4, y, num4, y2, paint4);
		}
		double playhead = _playhead;
		if (playhead > 0.0 && playhead < 1.0)
		{
			using (SKPaint paint5 = new SKPaint
			{
				Color = SKColors.White,
				StrokeWidth = 2f,
				IsAntialias = true
			})
			{
				canvas.DrawLine((float)(_playhead * (double)num), 0f, (float)(_playhead * (double)num), num2, paint5);
				return;
			}
		}
	}

	internal static SKColor ParseColor(string hex)
	{
		if (hex.Length >= 7 && hex[0] == '#')
		{
			byte red = Convert.ToByte(hex.Substring(1, 2), 16);
			byte green = Convert.ToByte(hex.Substring(3, 2), 16);
			byte blue = Convert.ToByte(hex.Substring(5, 2), 16);
			return new SKColor(red, green, blue);
		}
		return SKColors.SkyBlue;
	}

	[GeneratedCode("Microsoft.UI.Xaml.Markup.Compiler", " 3.0.0.2409")]
	[DebuggerNonUserCode]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("ms-appx:///FurinaPlayer.UI/Controls/WaveformControl.xaml");
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
