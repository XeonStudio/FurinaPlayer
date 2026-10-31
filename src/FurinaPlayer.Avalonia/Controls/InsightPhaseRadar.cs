using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Threading;
using SonicWave.Audio.Dsp;

namespace FurinaPlayer.Avalonia.Controls;

/// <summary>
/// Professional iZotope Insight 2 style Phase Correlation Radar.
/// High-refresh Skia canvas rendering concentric dB grid and dynamic correlation dot.
/// </summary>
public class InsightPhaseRadar : Control
{
    private readonly DispatcherTimer _timer;
    private static readonly IBrush _bgBrush = new SolidColorBrush(Color.FromArgb(200, 15, 23, 42));
    private static readonly IPen _circlePen = new Pen(new SolidColorBrush(Color.FromArgb(60, 56, 189, 248)), 1);
    private static readonly IPen _axisPen = new Pen(new SolidColorBrush(Color.FromArgb(40, 255, 255, 255)), 1, DashStyle.Dash);
    private static readonly IBrush _dotBrush = new RadialGradientBrush
    {
        GradientStops =
        {
            new GradientStop(Color.FromArgb(255, 52, 211, 153), 0.0),
            new GradientStop(Color.FromArgb(180, 16, 185, 129), 0.6),
            new GradientStop(Color.FromArgb(0, 5, 150, 105), 1.0)
        }
    };

    public InsightPhaseRadar()
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
        double cy = h / 2.0;
        double maxR = Math.Min(cx, cy) - 10;
        if (maxR <= 5) return;

        // Concentric circles: 0dB, -6dB, -12dB, -20dB
        context.DrawEllipse(null, _circlePen, new Point(cx, cy), maxR, maxR);
        context.DrawEllipse(null, _circlePen, new Point(cx, cy), maxR * 0.75, maxR * 0.75);
        context.DrawEllipse(null, _circlePen, new Point(cx, cy), maxR * 0.50, maxR * 0.50);
        context.DrawEllipse(null, _circlePen, new Point(cx, cy), maxR * 0.25, maxR * 0.25);

        // Crosshairs
        context.DrawLine(_axisPen, new Point(cx, cy - maxR), new Point(cx, cy + maxR));
        context.DrawLine(_axisPen, new Point(cx - maxR, cy), new Point(cx + maxR, cy));

        // Read correlation from LockFreeAudioVisualBuffer
        var buf = LockFreeAudioVisualBuffer.Instance;
        buf.GetLoudnessInfo(out _, out _, out float truePeak, out float correlation);

        // Compute phase dot position
        double r = Math.Clamp((truePeak + 40.0) / 40.0, 0.05, 0.85) * maxR;
        // Angle based on correlation: 1.0 = top (mono), 0.0 = 45 deg, -1.0 = bottom (out of phase)
        double angleRad = (1.0 - correlation) * (Math.PI / 2.0) - (Math.PI / 2.0);
        double dotX = cx + r * Math.Cos(angleRad);
        double dotY = cy + r * Math.Sin(angleRad);

        // Draw glowing correlation dot
        context.DrawEllipse(_dotBrush, null, new Point(dotX, dotY), 7, 7);
    }
}
