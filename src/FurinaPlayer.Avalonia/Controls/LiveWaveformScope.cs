using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Threading;
using SonicWave.Audio.Dsp;

namespace FurinaPlayer.Avalonia.Controls;

/// <summary>
/// High-performance Skia live waveform oscilloscope connected to LockFreeAudioVisualBuffer.
/// </summary>
public class LiveWaveformScope : Control
{
    private readonly DispatcherTimer _renderTimer;
    private readonly float[] _bufferL = new float[256];
    private readonly float[] _bufferR = new float[256];
    private static readonly IBrush _bgBrush = new SolidColorBrush(Color.FromArgb(220, 10, 15, 25));
    private static readonly IPen _gridPen = new Pen(new SolidColorBrush(Color.FromArgb(40, 255, 255, 255)), 1);
    private static readonly IPen _centerPen = new Pen(new SolidColorBrush(Color.FromArgb(70, 56, 189, 248)), 1, DashStyle.Dash);
    private static readonly IPen _wavePenL = new Pen(new LinearGradientBrush
    {
        StartPoint = new RelativePoint(0, 0, RelativeUnit.Relative),
        EndPoint = new RelativePoint(1, 0, RelativeUnit.Relative),
        GradientStops =
        {
            new GradientStop(Color.FromArgb(255, 0, 194, 255), 0.0),
            new GradientStop(Color.FromArgb(255, 56, 189, 248), 1.0)
        }
    }, 2.0);
    private static readonly IPen _wavePenR = new Pen(new LinearGradientBrush
    {
        StartPoint = new RelativePoint(0, 0, RelativeUnit.Relative),
        EndPoint = new RelativePoint(1, 0, RelativeUnit.Relative),
        GradientStops =
        {
            new GradientStop(Color.FromArgb(220, 192, 132, 252), 0.0),
            new GradientStop(Color.FromArgb(220, 244, 114, 182), 1.0)
        }
    }, 1.6);

    public LiveWaveformScope()
    {
        _renderTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(40) // 25 FPS smooth render
        };
        _renderTimer.Tick += (s, e) => InvalidateVisual();
        _renderTimer.Start();
    }

    public override void Render(DrawingContext context)
    {
        base.Render(context);

        double w = Bounds.Width;
        double h = Bounds.Height;
        if (w <= 10 || h <= 10) return;

        // Draw background plate
        context.DrawRectangle(_bgBrush, new Pen(new SolidColorBrush(Color.FromArgb(50, 255, 255, 255)), 1), new Rect(0, 0, w, h), 8, 8);

        // Draw center baseline and grid
        double midY = h / 2.0;
        context.DrawLine(_centerPen, new Point(0, midY), new Point(w, midY));
        context.DrawLine(_gridPen, new Point(0, h * 0.25), new Point(w, h * 0.25));
        context.DrawLine(_gridPen, new Point(0, h * 0.75), new Point(w, h * 0.75));

        // Read waveform from LockFreeAudioVisualBuffer
        LockFreeAudioVisualBuffer.Instance.ReadWaveform(_bufferL, _bufferR, 256);

        // Render Left Channel Waveform
        var geomL = new StreamGeometry();
        using (var ctx = geomL.Open())
        {
            double stepX = w / 256.0;
            ctx.BeginFigure(new Point(0, midY), false);

            for (int i = 0; i < 256; i++)
            {
                float sample = _bufferL[i];
                double x = i * stepX;
                double y = midY - (sample * (h * 0.42));
                ctx.LineTo(new Point(x, Math.Clamp(y, 2, h - 2)));
            }

            ctx.EndFigure(false);
        }
        context.DrawGeometry(null, _wavePenL, geomL);

        // Render Right Channel Waveform (subtle overlay)
        var geomR = new StreamGeometry();
        using (var ctx = geomR.Open())
        {
            double stepX = w / 256.0;
            ctx.BeginFigure(new Point(0, midY), false);

            for (int i = 0; i < 256; i++)
            {
                float sample = _bufferR[i];
                double x = i * stepX;
                double y = midY - (sample * (h * 0.42));
                ctx.LineTo(new Point(x, Math.Clamp(y, 2, h - 2)));
            }

            ctx.EndFigure(false);
        }
        context.DrawGeometry(null, _wavePenR, geomR);
    }
}
