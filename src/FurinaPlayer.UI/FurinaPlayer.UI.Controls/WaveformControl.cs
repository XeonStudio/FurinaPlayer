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
	private IReadOnlyList<WaveformRenderer.PeakPair> _leftPeaks = Array.Empty<WaveformRenderer.PeakPair>();
	private IReadOnlyList<WaveformRenderer.PeakPair> _rightPeaks = Array.Empty<WaveformRenderer.PeakPair>();
	private IReadOnlyList<float> _leftRms = Array.Empty<float>();
	private IReadOnlyList<float> _rightRms = Array.Empty<float>();

	private double _selectionStart;
	private double _selectionEnd = 1.0;
	private double _playhead;
	private string _accentHex = "#00e5ff";

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
		_leftPeaks = peaks;
		_rightPeaks = peaks;
		_leftRms = Array.Empty<float>();
		_rightRms = Array.Empty<float>();
		_selectionStart = selectionStartFraction;
		_selectionEnd = selectionEndFraction;
		_playhead = playheadFraction;
		_accentHex = accentHex;
		Canvas.Invalidate();
	}

	public void SetAuditionData(
		IReadOnlyList<WaveformRenderer.PeakPair> leftPeaks,
		IReadOnlyList<WaveformRenderer.PeakPair> rightPeaks,
		IReadOnlyList<float>? leftRms,
		IReadOnlyList<float>? rightRms,
		double selectionStartFraction,
		double selectionEndFraction,
		double playheadFraction,
		string accentHex = "#00e5ff")
	{
		_leftPeaks = leftPeaks ?? Array.Empty<WaveformRenderer.PeakPair>();
		_rightPeaks = rightPeaks ?? Array.Empty<WaveformRenderer.PeakPair>();
		_leftRms = leftRms ?? Array.Empty<float>();
		_rightRms = rightRms ?? Array.Empty<float>();
		_peaks = _leftPeaks.Count > 0 ? _leftPeaks : _rightPeaks;
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
		float w = e.Info.Width;
		float h = e.Info.Height;
		if (w <= 0f || h <= 0f)
		{
			return;
		}

		// 1. Adobe Audition DAW professional dark theme background
		canvas.Clear(new SKColor(22, 25, 29)); // #16191d

		// Check if any waveform data exists
		bool hasData = (_leftPeaks.Count > 0 || _rightPeaks.Count > 0 || _peaks.Count > 0);
		if (!hasData)
		{
			using var emptyPaint = new SKPaint
			{
				Color = new SKColor(120, 130, 145, 180),
				TextSize = 14f,
				IsAntialias = true,
				Typeface = CjkTypeface
			};
			float textW = emptyPaint.MeasureText(EmptyHint);
			canvas.DrawText(EmptyHint, (w - textW) / 2f, h / 2f + 5f, emptyPaint);
			return;
		}

		float laneH = h / 2f;

		// Colors matching Adobe Audition DAW
		SKColor peakColor = new SKColor(58, 197, 236); // #3ac5ec electric cyan
		SKColor rmsColor = new SKColor(2, 136, 209);   // #0288d1 deep solid cyan/blue
		SKColor greenBaselineColor = new SKColor(34, 139, 73, 140); // 0-axis baseline green
		SKColor gridLineColor = new SKColor(28, 110, 56, 70);       // dB graticule green
		SKColor hudTextColor = new SKColor(255, 255, 255, 120);     // HUD text
		SKColor hudChannelColor = new SKColor(255, 255, 255, 180);  // L / R badge

		using var baselinePaint = new SKPaint
		{
			Color = greenBaselineColor,
			StrokeWidth = 1f,
			IsAntialias = true
		};

		using var gridPaint = new SKPaint
		{
			Color = gridLineColor,
			StrokeWidth = 1f,
			IsAntialias = true
		};

		using var tickPaint = new SKPaint
		{
			Color = new SKColor(255, 255, 255, 60),
			StrokeWidth = 1f,
			IsAntialias = true
		};

		using var hudPaint = new SKPaint
		{
			Color = hudChannelColor,
			TextSize = 11f,
			IsAntialias = true,
			Typeface = SKTypeface.FromFamilyName("Segoe UI", SKFontStyleWeight.Bold, SKFontStyleWidth.Normal, SKFontStyleSlant.Upright)
		};

		using var hudSubPaint = new SKPaint
		{
			Color = hudTextColor,
			TextSize = 9.5f,
			IsAntialias = true,
			Typeface = CjkTypeface
		};

		using var dbTextPaint = new SKPaint
		{
			Color = new SKColor(255, 255, 255, 80),
			TextSize = 8.5f,
			IsAntialias = true,
			Typeface = SKTypeface.FromFamilyName("Consolas")
		};

		using var peakPaint = new SKPaint
		{
			Color = peakColor,
			StrokeWidth = 1f,
			IsAntialias = true
		};

		using var rmsPaint = new SKPaint
		{
			Color = rmsColor,
			StrokeWidth = 1f,
			IsAntialias = true
		};

		int[] gridDbs = new int[] { 0, -6, -12, -24 };

		// 2. Render each channel lane (Lane 0: Left, Lane 1: Right)
		for (int lane = 0; lane < 2; lane++)
		{
			float laneTop = lane * laneH;
			float laneMidY = laneTop + laneH / 2f;
			float maxAmp = laneH / 2f - 4f;

			var peaks = (lane == 0 ? _leftPeaks : _rightPeaks);
			if (peaks == null || peaks.Count == 0) peaks = _peaks;

			var rms = (lane == 0 ? _leftRms : _rightRms);

			// A. dB Graticule (Audition style dark-green grid)
			foreach (int db in gridDbs)
			{
				float ratio = (float)Math.Pow(10.0, db / 20.0);
				float yTop = laneMidY - ratio * maxAmp;
				float yBottom = laneMidY + ratio * maxAmp;

				if (db != 0)
				{
					canvas.DrawLine(0f, yTop, w, yTop, gridPaint);
					canvas.DrawLine(0f, yBottom, w, yBottom, gridPaint);
				}

				// Right side tick marks and dB text
				canvas.DrawLine(w - 6f, yTop, w, yTop, tickPaint);
				if (db != 0) canvas.DrawLine(w - 6f, yBottom, w, yBottom, tickPaint);

				string dbLabel = db == 0 ? "0" : $"{db}";
				canvas.DrawText(dbLabel, w - 24f, yTop + 3.5f, dbTextPaint);
			}

			// B. Center 0-axis baseline (Audition signature green center axis)
			canvas.DrawLine(0f, laneMidY, w, laneMidY, baselinePaint);

			// C. Waveform data (Peak + RMS dual layers)
			if (peaks != null && peaks.Count > 0)
			{
				int peakCount = peaks.Count;
				for (int x = 0; x < (int)w; x++)
				{
					int b = (int)((long)x * peakCount / (long)w);
					if (b < 0) b = 0;
					if (b >= peakCount) b = peakCount - 1;

					float topVal = Math.Clamp(peaks[b].Max, 0f, 1f);
					float botVal = Math.Clamp(Math.Abs(peaks[b].Min), 0f, 1f);

					float topAmp = topVal * maxAmp;
					float botAmp = botVal * maxAmp;

					float y0 = laneMidY - topAmp;
					float y1 = laneMidY + botAmp;
					if (y1 - y0 < 1.5f)
					{
						y0 = laneMidY - 0.75f;
						y1 = laneMidY + 0.75f;
					}

					// Outer Peak layer
					canvas.DrawLine((float)x, y0, (float)x, y1, peakPaint);

					// Inner RMS layer
					float rmsVal = (rms != null && b < rms.Count) ? Math.Clamp(rms[b], 0f, 1f) : Math.Min(topVal, botVal) * 0.55f;
					float rmsAmp = rmsVal * maxAmp;
					if (rmsAmp > 0.5f)
					{
						canvas.DrawLine((float)x, laneMidY - rmsAmp, (float)x, laneMidY + rmsAmp, rmsPaint);
					}
				}
			}

			// D. HUD channel labels
			string chLabel = lane == 0 ? "L" : "R";
			string chSub = lane == 0 ? "PK 峰值包络 · 线性振幅 (0 ~ -60 dB FS)" : "RMS 有效值包络 · 线性振幅 (0 ~ -60 dB FS)";
			canvas.DrawText(chLabel, 10f, laneTop + 16f, hudPaint);
			canvas.DrawText(chSub, 28f, laneTop + 16f, hudSubPaint);
		}

		// 3. Channel separator line at mid-height
		using var sepPaint = new SKPaint
		{
			Color = new SKColor(35, 41, 48), // #232930
			StrokeWidth = 1f,
			IsAntialias = true
		};
		canvas.DrawLine(0f, laneH, w, laneH, sepPaint);

		// 4. Selection Highlight
		if (_selectionEnd > _selectionStart)
		{
			float selX0 = (float)(_selectionStart * w);
			float selX1 = (float)(_selectionEnd * w);
			using var selFill = new SKPaint
			{
				Color = new SKColor(58, 197, 236, 40),
				Style = SKPaintStyle.Fill
			};
			canvas.DrawRect(selX0, 0f, selX1 - selX0, h, selFill);

			using var selBorder = new SKPaint
			{
				Color = new SKColor(255, 255, 255, 200),
				StrokeWidth = 1.5f,
				IsAntialias = true
			};
			canvas.DrawLine(selX0, 0f, selX0, h, selBorder);
			canvas.DrawLine(selX1, 0f, selX1, h, selBorder);
		}

		// 5. Audition Red Playhead Indicator with smooth position and downward handle
		if (_playhead >= 0.0 && _playhead <= 1.0)
		{
			float playX = (float)(_playhead * w);

			using var playheadLine = new SKPaint
			{
				Color = new SKColor(255, 76, 76), // #ff4c4c
				StrokeWidth = 2f,
				IsAntialias = true
			};
			canvas.DrawLine(playX, 0f, playX, h, playheadLine);

			using var playheadMarker = new SKPaint
			{
				Color = new SKColor(255, 76, 76),
				Style = SKPaintStyle.Fill,
				IsAntialias = true
			};

			// Top downward-pointing pointer
			using var topMarkerPath = new SKPath();
			topMarkerPath.MoveTo(playX - 6f, 0f);
			topMarkerPath.LineTo(playX + 6f, 0f);
			topMarkerPath.LineTo(playX, 9f);
			topMarkerPath.Close();
			canvas.DrawPath(topMarkerPath, playheadMarker);

			// Bottom upward-pointing anchor
			using var botMarkerPath = new SKPath();
			botMarkerPath.MoveTo(playX - 5f, h);
			botMarkerPath.LineTo(playX + 5f, h);
			botMarkerPath.LineTo(playX, h - 8f);
			botMarkerPath.Close();
			canvas.DrawPath(botMarkerPath, playheadMarker);
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
