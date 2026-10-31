using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Threading;
using SonicWave.Audio.Dsp;

namespace FurinaPlayer.Avalonia.Controls;

/// <summary>
/// Sound Field Goniometer / Polar Vector Display (Insight 2 style).
/// </summary>
public class InsightSoundField : Control
{
    private readonly DispatcherTimer _timer;
    private readonly float[] _bufferL = new float[128];
    private readonly float[] _bufferR = new float[128];

    private static readonly IBrush _bgBrush = new SolidColorBrush(Color.FromArgb(200, 15, 23, 42));
    private static readonly IPen _fanPen = new Pen(new SolidColorBrush(Color.FromArgb(40, 255, 255, 255)), 1);
    private static readonly IPen _polarPen = new Pen(new LinearGradientBrush
    {
        StartPoint = new RelativePoint(0, 0, RelativeUnit.Relative),
        EndPoint = new RelativePoint(1, 1, RelativeUnit.Relative),
        GradientStops =
        {
            new GradientStop(Color.FromArgb(220, 56, 189, 248), 0.0),
            new GradientStop(Color.FromArgb(220, 52, 211, 153), 1.0)
        }
    }, 1.5);

    public InsightSoundField()
    {
        _timer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(50)
        };
        _timer.Tick += (s, e) => InvalidateVisual();
        _timer.Start();
    }

    public override void Render(DrawingContext context)
    {
        base.Render(context);

        double w = Bounds.Width;
        double h = Bounds.Height;
        if (w <= 10 || h <= 10) return;

        context.DrawRectangle(_bgBrush, new Pen(new SolidColorBrush(Color.FromArgb(40, 255, 255, 255)), 1), new Rect(0, 0, w, h), 8, 8);

        double cx = w / 2.0;
        double cy = h * 0.85;
        double r = Math.Min(w * 0.45, h * 0.75);

        // Draw fan shape (Polar scope grid)
        context.DrawLine(_fanPen, new Point(cx, cy), new Point(cx - r * 0.707, cy - r * 0.707)); // Left 45 deg
        context.DrawLine(_fanPen, new Point(cx, cy), new Point(cx + r * 0.707, cy - r * 0.707)); // Right 45 deg
        context.DrawLine(_fanPen, new Point(cx, cy), new Point(cx, cy - r)); // Center

        // Arc grid
        var arcGeom = new StreamGeometry();
        using (var ctx = arcGeom.Open())
        {
            ctx.BeginFigure(new Point(cx - r * 0.707, cy - r * 0.707), false);
            ctx.ArcTo(new Point(cx + r * 0.707, cy - r * 0.707), new Size(r, r), 0, false, SweepDirection.Clockwise);
            ctx.EndFigure(false);
        }
        context.DrawGeometry(null, _fanPen, arcGeom);

        // Read real audio samples
        LockFreeAudioVisualBuffer.Instance.ReadWaveform(_bufferL, _bufferR, 128);

        // Draw Lissajous polar cloud
        var scopeGeom = new StreamGeometry();
        using (var ctx = scopeGeom.Open())
        {
            bool started = false;
            for (int i = 0; i < 64; i += 2)
            {
                float left = _bufferL[i];
                float right = _bufferR[i];

                // Mid/Side rotation
                float m = (left + right) * 0.707f;
                float s = (left - right) * 0.707f;

                double px = cx + s * r;
                double py = cy - m * r;

                if (!started)
                {
                    ctx.BeginFigure(new Point(px, py), false);
                    started = true;
                }
                else
                {
                    ctx.LineTo(new Point(px, py));
                }
            }

            if (started) ctx.EndFigure(false);
        }

        context.DrawGeometry(null, _polarPen, scopeGeom);
    }
}
