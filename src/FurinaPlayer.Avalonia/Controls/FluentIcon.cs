using System;
using System.Collections.Concurrent;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

namespace FurinaPlayer.Avalonia.Controls;

public enum FluentIconKind
{
    AppLogo,
    Home,
    NowPlaying,
    AudioEditor,
    DspEffects,
    Settings,
    Play,
    Pause,
    Previous,
    Next,
    Stop,
    ModeSequential,
    ModeLoopOne,
    ModeLoopAll,
    ModeShuffle,
    Volume,
    VolumeMute,
    RecentList,
    Eq,
    FolderScan,
    FileAdd,
    AlbumGrid,
    Search,
    ClearFilter,
    Delete,
    LyricsImport,
    MusicHero,
    ModeAudition,
    ModePowerPoint,
    ModeClassic,
    Rack,
    Spatial,
    Dynamics,
    HiRes,
    VstPlugin,
    FreeCanvas,
    RefreshReset,
    Save,
    Wallpaper,
    Font,
    DeviceAudio,
    DiagnosticLog,
    WindowMin,
    WindowMax,
    WindowRestore,
    WindowClose,
    ChevronUp,
    ChevronDown,
    EditResize,
    Headphones,
    Checkmark
}

/// <summary>
/// High-performance Microsoft Fluent UI Colorful Multi-Layer Vector Icon for Avalonia 11.
/// Direct Skia DrawingContext rendering with layered geometric gradient tiles and depth plates.
/// </summary>
public class FluentIcon : Control
{
    public static readonly StyledProperty<FluentIconKind> KindProperty =
        AvaloniaProperty.Register<FluentIcon, FluentIconKind>(nameof(Kind), FluentIconKind.AppLogo);

    public static readonly StyledProperty<double> IconSizeProperty =
        AvaloniaProperty.Register<FluentIcon, double>(nameof(IconSize), 20.0);

    public FluentIconKind Kind
    {
        get => GetValue(KindProperty);
        set => SetValue(KindProperty, value);
    }

    public double IconSize
    {
        get => GetValue(IconSizeProperty);
        set => SetValue(IconSizeProperty, value);
    }

    static FluentIcon()
    {
        AffectsRender<FluentIcon>(KindProperty, IconSizeProperty);
        AffectsMeasure<FluentIcon>(IconSizeProperty);
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        double s = IconSize;
        return new Size(s, s);
    }

    private static readonly ConcurrentDictionary<string, IBrush> _brushCache = new();
    private static readonly ConcurrentDictionary<string, Geometry> _geomCache = new();

    private static IBrush Grad(byte r1, byte g1, byte b1, byte r2, byte g2, byte b2, double angle = 45)
    {
        string key = $"grad_{r1}_{g1}_{b1}_{r2}_{g2}_{b2}_{angle}";
        return _brushCache.GetOrAdd(key, _ =>
        {
            var start = new RelativePoint(0, 0, RelativeUnit.Relative);
            var end = angle == 90 
                ? new RelativePoint(0, 1, RelativeUnit.Relative) 
                : (angle == 0 ? new RelativePoint(1, 0, RelativeUnit.Relative) : new RelativePoint(1, 1, RelativeUnit.Relative));

            return new LinearGradientBrush
            {
                StartPoint = start,
                EndPoint = end,
                GradientStops =
                {
                    new GradientStop(Color.FromArgb(255, r1, g1, b1), 0.0),
                    new GradientStop(Color.FromArgb(255, r2, g2, b2), 1.0)
                }
            };
        });
    }

    private static IBrush Solid(byte r, byte g, byte b, byte a = 255)
    {
        string key = $"solid_{r}_{g}_{b}_{a}";
        return _brushCache.GetOrAdd(key, _ => new SolidColorBrush(Color.FromArgb(a, r, g, b)));
    }

    private static Geometry Geom(string pathData)
    {
        return _geomCache.GetOrAdd(pathData, d => StreamGeometry.Parse(d));
    }

    public override void Render(DrawingContext context)
    {
        base.Render(context);

        double w = Bounds.Width > 0 ? Bounds.Width : IconSize;
        double h = Bounds.Height > 0 ? Bounds.Height : IconSize;
        double s = IconSize > 0 ? IconSize : 20.0;
        double f = s / 24.0; // Normalized scale factor based on 24x24 Fluent canvas
        double offsetX = (w - s) / 2.0;
        double offsetY = (h - s) / 2.0;

        using (context.PushTransform(Matrix.CreateTranslation(offsetX, offsetY)))
        {
            switch (Kind)
            {
                case FluentIconKind.AppLogo:
                    // Furina Archon Hydro Crystal Crown
                    context.DrawRectangle(Grad(0, 120, 215, 0, 164, 239, 45), null, new Rect(2 * f, 2 * f, 20 * f, 20 * f), 5 * f, 5 * f);
                    context.DrawRectangle(Grad(0, 180, 255, 128, 90, 213, 90), null, new Rect(5 * f, 5 * f, 14 * f, 14 * f), 3.5 * f, 3.5 * f);
                    context.DrawGeometry(Solid(255, 255, 255, 235), null, 
                        Geom($"M {12 * f} {7 * f} L {16 * f} {12 * f} L {12 * f} {17 * f} L {8 * f} {12 * f} Z"));
                    break;

                case FluentIconKind.Home:
                    // Fluent Blue Home Tile with depth plate
                    context.DrawRectangle(Grad(0, 114, 206, 0, 168, 232, 45), null, new Rect(3 * f, 5 * f, 18 * f, 16 * f), 3 * f, 3 * f);
                    context.DrawGeometry(Solid(255, 255, 255, 240), null,
                        Geom($"M {12 * f} {2 * f} L {22 * f} {10.5 * f} L {19 * f} {13 * f} L {12 * f} {7 * f} L {5 * f} {13 * f} L {2 * f} {10.5 * f} Z"));
                    context.DrawRectangle(Solid(255, 255, 255, 240), null, new Rect(9.5 * f, 13 * f, 5 * f, 8 * f), 1.5 * f, 1.5 * f);
                    break;

                case FluentIconKind.NowPlaying:
                case FluentIconKind.ModeClassic:
                    // Vibrant Purple/Cyan Fluent Media Player Card
                    context.DrawRectangle(Grad(119, 82, 254, 142, 68, 173, 45), null, new Rect(2 * f, 2 * f, 20 * f, 20 * f), 4.5 * f, 4.5 * f);
                    context.DrawEllipse(Grad(0, 210, 255, 0, 120, 215, 90), null, new Point(12 * f, 12 * f), 6.5 * f, 6.5 * f);
                    context.DrawGeometry(Solid(255, 255, 255, 245), null,
                        Geom($"M {10 * f} {8.5 * f} L {16 * f} {12 * f} L {10 * f} {15.5 * f} Z"));
                    break;

                case FluentIconKind.AudioEditor:
                case FluentIconKind.FreeCanvas:
                    // Teal/Cyan Audio Waveform & Studio Track
                    context.DrawRectangle(Grad(0, 180, 216, 3, 4, 94, 90), null, new Rect(2 * f, 3 * f, 20 * f, 18 * f), 3 * f, 3 * f);
                    context.DrawRectangle(Solid(0, 255, 204, 220), null, new Rect(5 * f, 9 * f, 2 * f, 6 * f), 1 * f, 1 * f);
                    context.DrawRectangle(Solid(255, 255, 255, 240), null, new Rect(9 * f, 6 * f, 2 * f, 12 * f), 1 * f, 1 * f);
                    context.DrawRectangle(Solid(0, 255, 204, 220), null, new Rect(13 * f, 8 * f, 2 * f, 8 * f), 1 * f, 1 * f);
                    context.DrawRectangle(Solid(255, 255, 255, 240), null, new Rect(17 * f, 10 * f, 2 * f, 4 * f), 1 * f, 1 * f);
                    break;

                case FluentIconKind.DspEffects:
                case FluentIconKind.Eq:
                case FluentIconKind.Rack:
                    // Amber/Orange Professional Studio Equalizer Sliders
                    context.DrawRectangle(Grad(255, 140, 0, 230, 81, 0, 45), null, new Rect(2 * f, 2 * f, 20 * f, 20 * f), 4 * f, 4 * f);
                    context.DrawRectangle(Solid(255, 255, 255, 90), null, new Rect(6 * f, 5 * f, 2 * f, 14 * f), 1 * f, 1 * f);
                    context.DrawRectangle(Solid(255, 255, 255, 90), null, new Rect(11 * f, 5 * f, 2 * f, 14 * f), 1 * f, 1 * f);
                    context.DrawRectangle(Solid(255, 255, 255, 90), null, new Rect(16 * f, 5 * f, 2 * f, 14 * f), 1 * f, 1 * f);
                    context.DrawRectangle(Solid(255, 255, 255, 245), null, new Rect(4.5 * f, 11 * f, 5 * f, 3 * f), 1 * f, 1 * f);
                    context.DrawRectangle(Solid(255, 255, 255, 245), null, new Rect(9.5 * f, 7 * f, 5 * f, 3 * f), 1 * f, 1 * f);
                    context.DrawRectangle(Solid(255, 255, 255, 245), null, new Rect(14.5 * f, 13 * f, 5 * f, 3 * f), 1 * f, 1 * f);
                    break;

                case FluentIconKind.Settings:
                    // Fluent Slate Modern Cogwheel Gear
                    context.DrawRectangle(Grad(70, 80, 95, 35, 45, 60, 45), null, new Rect(2 * f, 2 * f, 20 * f, 20 * f), 4 * f, 4 * f);
                    context.DrawEllipse(Grad(0, 164, 239, 0, 120, 215, 90), null, new Point(12 * f, 12 * f), 6 * f, 6 * f);
                    context.DrawEllipse(Solid(255, 255, 255, 230), null, new Point(12 * f, 12 * f), 2.5 * f, 2.5 * f);
                    break;

                case FluentIconKind.Play:
                    // Vivid Blue Circle with Play arrow
                    context.DrawEllipse(Grad(0, 120, 215, 0, 168, 232, 90), null, new Point(12 * f, 12 * f), 11 * f, 11 * f);
                    context.DrawGeometry(Solid(255, 255, 255, 255), null,
                        Geom($"M {9.5 * f} {7.5 * f} L {17 * f} {12 * f} L {9.5 * f} {16.5 * f} Z"));
                    break;

                case FluentIconKind.Pause:
                    // Vivid Blue Circle with twin Pause bars
                    context.DrawEllipse(Grad(0, 120, 215, 0, 168, 232, 90), null, new Point(12 * f, 12 * f), 11 * f, 11 * f);
                    context.DrawRectangle(Solid(255, 255, 255, 255), null, new Rect(8 * f, 7 * f, 2.5 * f, 10 * f), 1 * f, 1 * f);
                    context.DrawRectangle(Solid(255, 255, 255, 255), null, new Rect(13.5 * f, 7 * f, 2.5 * f, 10 * f), 1 * f, 1 * f);
                    break;

                case FluentIconKind.Previous:
                    // Left vertical bar + Left pointing triangle
                    context.DrawRectangle(Grad(0, 120, 215, 0, 168, 232, 45), null, new Rect(3.5 * f, 6 * f, 2.5 * f, 12 * f), 1 * f, 1 * f);
                    context.DrawGeometry(Grad(0, 120, 215, 0, 168, 232, 45), null,
                        Geom($"M {19 * f} {6 * f} L {8 * f} {12 * f} L {19 * f} {18 * f} Z"));
                    break;

                case FluentIconKind.Next:
                    // Right pointing triangle + Right vertical bar
                    context.DrawGeometry(Grad(0, 120, 215, 0, 168, 232, 45), null,
                        Geom($"M {5 * f} {6 * f} L {16 * f} {12 * f} L {5 * f} {18 * f} Z"));
                    context.DrawRectangle(Grad(0, 120, 215, 0, 168, 232, 45), null, new Rect(18 * f, 6 * f, 2.5 * f, 12 * f), 1 * f, 1 * f);
                    break;

                case FluentIconKind.Stop:
                    context.DrawEllipse(Grad(235, 87, 87, 192, 57, 43, 90), null, new Point(12 * f, 12 * f), 10 * f, 10 * f);
                    context.DrawRectangle(Solid(255, 255, 255, 255), null, new Rect(8 * f, 8 * f, 8 * f, 8 * f), 1.5 * f, 1.5 * f);
                    break;

                case FluentIconKind.Volume:
                    // Fluent Blue Speaker with wave ripples
                    context.DrawGeometry(Grad(0, 120, 215, 0, 168, 232, 45), null,
                        Geom($"M {4 * f} {9 * f} L {7.5 * f} {9 * f} L {12 * f} {5 * f} L {12 * f} {19 * f} L {7.5 * f} {15 * f} L {4 * f} {15 * f} Z"));
                    context.DrawGeometry(null, new Pen(Grad(0, 120, 215, 0, 168, 232, 45), 2 * f),
                        Geom($"M {15 * f} {9 * f} A {4 * f} {4 * f} 0 0 1 {15 * f} {15 * f}"));
                    context.DrawGeometry(null, new Pen(Grad(0, 120, 215, 0, 168, 232, 45), 2 * f),
                        Geom($"M {18 * f} {6 * f} A {8 * f} {8 * f} 0 0 1 {18 * f} {18 * f}"));
                    break;

                case FluentIconKind.VolumeMute:
                    context.DrawGeometry(Grad(180, 185, 195, 120, 125, 135, 45), null,
                        Geom($"M {4 * f} {9 * f} L {7.5 * f} {9 * f} L {12 * f} {5 * f} L {12 * f} {19 * f} L {7.5 * f} {15 * f} L {4 * f} {15 * f} Z"));
                    context.DrawLine(new Pen(Solid(235, 87, 87), 2 * f), new Point(15 * f, 9 * f), new Point(20 * f, 15 * f));
                    context.DrawLine(new Pen(Solid(235, 87, 87), 2 * f), new Point(20 * f, 9 * f), new Point(15 * f, 15 * f));
                    break;

                case FluentIconKind.ModeSequential:
                    context.DrawLine(new Pen(Grad(0, 120, 215, 0, 168, 232, 0), 2.2 * f), new Point(4 * f, 7 * f), new Point(17 * f, 7 * f));
                    context.DrawGeometry(Grad(0, 120, 215, 0, 168, 232, 0), null, Geom($"M {16 * f} {4.5 * f} L {20 * f} {7 * f} L {16 * f} {9.5 * f} Z"));
                    context.DrawLine(new Pen(Grad(0, 120, 215, 0, 168, 232, 0), 2.2 * f), new Point(4 * f, 12 * f), new Point(15 * f, 12 * f));
                    context.DrawLine(new Pen(Grad(0, 120, 215, 0, 168, 232, 0), 2.2 * f), new Point(4 * f, 17 * f), new Point(18 * f, 17 * f));
                    break;

                case FluentIconKind.ModeLoopAll:
                    context.DrawGeometry(null, new Pen(Grad(0, 180, 216, 0, 120, 215, 45), 2.2 * f),
                        Geom($"M {6 * f} {12 * f} A {6 * f} {6 * f} 0 1 1 {17 * f} {15 * f}"));
                    context.DrawGeometry(Grad(0, 180, 216, 0, 120, 215, 45), null,
                        Geom($"M {4 * f} {9 * f} L {7 * f} {13 * f} L {2 * f} {13 * f} Z"));
                    break;

                case FluentIconKind.ModeLoopOne:
                    context.DrawGeometry(null, new Pen(Grad(142, 68, 173, 119, 82, 254, 45), 2.2 * f),
                        Geom($"M {6 * f} {12 * f} A {6 * f} {6 * f} 0 1 1 {17 * f} {15 * f}"));
                    context.DrawGeometry(Grad(142, 68, 173, 119, 82, 254, 45), null,
                        Geom($"M {4 * f} {9 * f} L {7 * f} {13 * f} L {2 * f} {13 * f} Z"));
                    context.DrawRectangle(Solid(119, 82, 254), null, new Rect(11 * f, 9 * f, 2 * f, 6 * f), 1 * f, 1 * f);
                    break;

                case FluentIconKind.ModeShuffle:
                    context.DrawGeometry(null, new Pen(Grad(243, 156, 18, 230, 126, 34, 45), 2.2 * f),
                        Geom($"M {4 * f} {17 * f} C {8 * f} {17 * f}, {12 * f} {7 * f}, {16 * f} {7 * f}"));
                    context.DrawGeometry(Grad(243, 156, 18, 230, 126, 34, 45), null,
                        Geom($"M {15 * f} {4.5 * f} L {19 * f} {7 * f} L {15 * f} {9.5 * f} Z"));
                    context.DrawGeometry(null, new Pen(Grad(243, 156, 18, 230, 126, 34, 45), 2.2 * f),
                        Geom($"M {4 * f} {7 * f} C {8 * f} {7 * f}, {12 * f} {17 * f}, {16 * f} {17 * f}"));
                    context.DrawGeometry(Grad(243, 156, 18, 230, 126, 34, 45), null,
                        Geom($"M {15 * f} {14.5 * f} L {19 * f} {17 * f} L {15 * f} {19.5 * f} Z"));
                    break;

                case FluentIconKind.FolderScan:
                    context.DrawRectangle(Grad(255, 185, 0, 247, 99, 12, 45), null, new Rect(2 * f, 4 * f, 20 * f, 16 * f), 3 * f, 3 * f);
                    context.DrawRectangle(Grad(255, 216, 77, 255, 185, 0, 90), null, new Rect(2 * f, 8 * f, 20 * f, 12 * f), 2 * f, 2 * f);
                    context.DrawEllipse(null, new Pen(Solid(0, 120, 215), 2 * f), new Point(14 * f, 13 * f), 3.5 * f, 3.5 * f);
                    context.DrawLine(new Pen(Solid(0, 120, 215), 2 * f), new Point(16.5 * f, 15.5 * f), new Point(19 * f, 18 * f));
                    break;

                case FluentIconKind.FileAdd:
                    context.DrawRectangle(Grad(0, 120, 215, 0, 168, 232, 45), null, new Rect(4 * f, 2 * f, 16 * f, 20 * f), 2.5 * f, 2.5 * f);
                    context.DrawRectangle(Solid(255, 255, 255, 240), null, new Rect(6 * f, 4 * f, 12 * f, 16 * f), 1.5 * f, 1.5 * f);
                    context.DrawEllipse(Grad(16, 185, 129, 5, 150, 105, 90), null, new Point(15 * f, 15 * f), 5 * f, 5 * f);
                    context.DrawRectangle(Solid(255, 255, 255), null, new Rect(14.2 * f, 12 * f, 1.6 * f, 6 * f), 0.8 * f, 0.8 * f);
                    context.DrawRectangle(Solid(255, 255, 255), null, new Rect(12 * f, 14.2 * f, 6 * f, 1.6 * f), 0.8 * f, 0.8 * f);
                    break;

                case FluentIconKind.AlbumGrid:
                    context.DrawRectangle(Grad(0, 120, 215, 0, 168, 232, 45), null, new Rect(3 * f, 3 * f, 8 * f, 8 * f), 2 * f, 2 * f);
                    context.DrawRectangle(Grad(119, 82, 254, 142, 68, 173, 45), null, new Rect(13 * f, 3 * f, 8 * f, 8 * f), 2 * f, 2 * f);
                    context.DrawRectangle(Grad(255, 140, 0, 230, 81, 0, 45), null, new Rect(3 * f, 13 * f, 8 * f, 8 * f), 2 * f, 2 * f);
                    context.DrawRectangle(Grad(16, 185, 129, 5, 150, 105, 45), null, new Rect(13 * f, 13 * f, 8 * f, 8 * f), 2 * f, 2 * f);
                    break;

                case FluentIconKind.Search:
                    context.DrawEllipse(null, new Pen(Grad(0, 120, 215, 0, 168, 232, 45), 2.5 * f), new Point(10 * f, 10 * f), 6 * f, 6 * f);
                    context.DrawEllipse(Solid(0, 168, 232, 40), null, new Point(10 * f, 10 * f), 5 * f, 5 * f);
                    context.DrawLine(new Pen(Grad(0, 120, 215, 0, 168, 232, 45), 3 * f), new Point(14.5 * f, 14.5 * f), new Point(19.5 * f, 19.5 * f));
                    break;

                case FluentIconKind.ClearFilter:
                case FluentIconKind.Delete:
                    context.DrawEllipse(Grad(235, 87, 87, 192, 57, 43, 90), null, new Point(12 * f, 12 * f), 9 * f, 9 * f);
                    context.DrawLine(new Pen(Solid(255, 255, 255, 250), 2 * f), new Point(8 * f, 8 * f), new Point(16 * f, 16 * f));
                    context.DrawLine(new Pen(Solid(255, 255, 255, 250), 2 * f), new Point(16 * f, 8 * f), new Point(8 * f, 16 * f));
                    break;

                case FluentIconKind.MusicHero:
                    context.DrawEllipse(Grad(30, 41, 59, 15, 23, 42, 45), null, new Point(12 * f, 12 * f), 10 * f, 10 * f);
                    context.DrawEllipse(null, new Pen(Solid(255, 255, 255, 50), 1 * f), new Point(12 * f, 12 * f), 7.5 * f, 7.5 * f);
                    context.DrawEllipse(null, new Pen(Solid(255, 255, 255, 50), 1 * f), new Point(12 * f, 12 * f), 5 * f, 5 * f);
                    context.DrawEllipse(Grad(0, 210, 255, 119, 82, 254, 90), null, new Point(12 * f, 12 * f), 3 * f, 3 * f);
                    context.DrawEllipse(Solid(255, 255, 255), null, new Point(12 * f, 12 * f), 1 * f, 1 * f);
                    break;

                case FluentIconKind.HiRes:
                    context.DrawRectangle(Grad(212, 175, 55, 184, 134, 11, 45), null, new Rect(2 * f, 4 * f, 20 * f, 16 * f), 3 * f, 3 * f);
                    context.DrawRectangle(Solid(15, 23, 42), null, new Rect(3.5 * f, 5.5 * f, 17 * f, 13 * f), 2 * f, 2 * f);
                    context.DrawRectangle(Solid(212, 175, 55), null, new Rect(5.5 * f, 8 * f, 1.8 * f, 8 * f), 0.5 * f, 0.5 * f);
                    context.DrawRectangle(Solid(212, 175, 55), null, new Rect(9.5 * f, 8 * f, 1.8 * f, 8 * f), 0.5 * f, 0.5 * f);
                    context.DrawRectangle(Solid(212, 175, 55), null, new Rect(5.5 * f, 11 * f, 5.5 * f, 1.8 * f), 0.5 * f, 0.5 * f);
                    break;

                case FluentIconKind.Headphones:
                    context.DrawGeometry(null, new Pen(Grad(0, 180, 216, 79, 70, 229, 45), 2.5 * f),
                        Geom($"M {4 * f} {13 * f} A {8 * f} {8 * f} 0 0 1 {20 * f} {13 * f}"));
                    context.DrawRectangle(Grad(0, 180, 216, 79, 70, 229, 90), null, new Rect(3 * f, 12 * f, 3 * f, 7 * f), 1.5 * f, 1.5 * f);
                    context.DrawRectangle(Grad(0, 180, 216, 79, 70, 229, 90), null, new Rect(18 * f, 12 * f, 3 * f, 7 * f), 1.5 * f, 1.5 * f);
                    break;

                case FluentIconKind.Checkmark:
                    context.DrawEllipse(Grad(16, 185, 129, 5, 150, 105, 90), null, new Point(12 * f, 12 * f), 9 * f, 9 * f);
                    context.DrawLine(new Pen(Solid(255, 255, 255), 2 * f), new Point(7 * f, 12 * f), new Point(10.5 * f, 15.5 * f));
                    context.DrawLine(new Pen(Solid(255, 255, 255), 2 * f), new Point(10.5 * f, 15.5 * f), new Point(17 * f, 8.5 * f));
                    break;

                case FluentIconKind.RefreshReset:
                    // Circular refresh double arrow
                    context.DrawGeometry(null, new Pen(Grad(0, 180, 216, 0, 120, 215, 45), 2.2 * f),
                        Geom($"M {12 * f} {4 * f} A {8 * f} {8 * f} 0 1 1 {4 * f} {12 * f}"));
                    context.DrawGeometry(Grad(0, 180, 216, 0, 120, 215, 45), null,
                        Geom($"M {12 * f} {1.5 * f} L {16 * f} {4.5 * f} L {12 * f} {7.5 * f} Z"));
                    break;

                case FluentIconKind.Save:
                    context.DrawRectangle(Grad(0, 120, 215, 0, 168, 232, 45), null, new Rect(3 * f, 3 * f, 18 * f, 18 * f), 3 * f, 3 * f);
                    context.DrawRectangle(Solid(255, 255, 255, 220), null, new Rect(6 * f, 3 * f, 12 * f, 6 * f), 1 * f, 1 * f);
                    context.DrawRectangle(Solid(255, 255, 255, 220), null, new Rect(6 * f, 12 * f, 12 * f, 7 * f), 1.5 * f, 1.5 * f);
                    break;

                case FluentIconKind.Wallpaper:
                    context.DrawRectangle(Grad(142, 68, 173, 119, 82, 254, 45), null, new Rect(3 * f, 4 * f, 18 * f, 16 * f), 3 * f, 3 * f);
                    context.DrawEllipse(Solid(255, 215, 0), null, new Point(8 * f, 9 * f), 2.5 * f, 2.5 * f);
                    context.DrawGeometry(Solid(255, 255, 255, 200), null,
                        Geom($"M {4 * f} {18 * f} L {10 * f} {12 * f} L {14 * f} {15 * f} L {18 * f} {11 * f} L {20 * f} {18 * f} Z"));
                    break;

                case FluentIconKind.Font:
                    context.DrawRectangle(Grad(0, 120, 215, 0, 168, 232, 45), null, new Rect(3 * f, 3 * f, 18 * f, 18 * f), 3 * f, 3 * f);
                    context.DrawGeometry(Solid(255, 255, 255), null,
                        Geom($"M {12 * f} {5 * f} L {7 * f} {18 * f} L {9.5 * f} {18 * f} L {10.8 * f} {14.5 * f} L {13.2 * f} {14.5 * f} L {14.5 * f} {18 * f} L {17 * f} {18 * f} Z M {12 * f} {8.5 * f} L {11.2 * f} {12.5 * f} L {12.8 * f} {12.5 * f} Z"));
                    break;

                case FluentIconKind.DeviceAudio:
                    context.DrawRectangle(Grad(30, 41, 59, 15, 23, 42, 45), null, new Rect(2 * f, 5 * f, 20 * f, 14 * f), 3 * f, 3 * f);
                    context.DrawEllipse(Grad(0, 210, 255, 0, 120, 215, 90), null, new Point(7 * f, 12 * f), 3 * f, 3 * f);
                    context.DrawRectangle(Solid(16, 185, 129), null, new Rect(14 * f, 10 * f, 4 * f, 2 * f), 1 * f, 1 * f);
                    context.DrawRectangle(Solid(0, 168, 232), null, new Rect(14 * f, 13 * f, 4 * f, 2 * f), 1 * f, 1 * f);
                    break;

                case FluentIconKind.DiagnosticLog:
                    context.DrawRectangle(Grad(15, 23, 42, 30, 41, 59, 45), null, new Rect(3 * f, 4 * f, 18 * f, 16 * f), 3 * f, 3 * f);
                    context.DrawLine(new Pen(Solid(16, 185, 129), 1.8 * f), new Point(6 * f, 9 * f), new Point(9 * f, 11 * f));
                    context.DrawLine(new Pen(Solid(16, 185, 129), 1.8 * f), new Point(9 * f, 11 * f), new Point(6 * f, 13 * f));
                    context.DrawLine(new Pen(Solid(255, 255, 255), 1.8 * f), new Point(11 * f, 13 * f), new Point(15 * f, 13 * f));
                    break;

                case FluentIconKind.ModeAudition:
                    context.DrawRectangle(Grad(0, 194, 182, 0, 128, 128, 45), null, new Rect(2 * f, 2 * f, 20 * f, 20 * f), 4 * f, 4 * f);
                    context.DrawGeometry(Solid(15, 23, 42), null,
                        Geom($"M {7 * f} {17 * f} L {11 * f} {7 * f} L {13 * f} {7 * f} L {17 * f} {17 * f} L {14.5 * f} {17 * f} L {13.8 * f} {15 * f} L {10.2 * f} {15 * f} L {9.5 * f} {17 * f} Z M {12 * f} {9.5 * f} L {10.8 * f} {13 * f} L {13.2 * f} {13 * f} Z"));
                    break;

                case FluentIconKind.ModePowerPoint:
                    context.DrawRectangle(Grad(235, 87, 87, 192, 57, 43, 45), null, new Rect(2 * f, 2 * f, 20 * f, 20 * f), 4 * f, 4 * f);
                    context.DrawRectangle(Grad(255, 120, 80, 235, 87, 87, 90), null, new Rect(4 * f, 5 * f, 11 * f, 14 * f), 2 * f, 2 * f);
                    context.DrawGeometry(Solid(255, 255, 255), null,
                        Geom($"M {7 * f} {8 * f} L {11 * f} {8 * f} C {12.5 * f} {8 * f}, {13 * f} {9 * f}, {13 * f} {10.5 * f} C {13 * f} {12 * f}, {12.5 * f} {13 * f}, {11 * f} {13 * f} L {9 * f} {13 * f} L {9 * f} {16 * f} L {7 * f} {16 * f} Z M {9 * f} {10 * f} L {9 * f} {11.2 * f} L {10.8 * f} {11.2 * f} C {11.2 * f} {11.2 * f}, {11.5 * f} {11 * f}, {11.5 * f} {10.6 * f} C {11.5 * f} {10.2 * f}, {11.2 * f} {10 * f}, {10.8 * f} {10 * f} Z"));
                    break;

                case FluentIconKind.LyricsImport:
                    context.DrawRectangle(Grad(0, 120, 215, 0, 168, 232, 45), null, new Rect(4 * f, 3 * f, 16 * f, 18 * f), 2.5 * f, 2.5 * f);
                    context.DrawLine(new Pen(Solid(255, 255, 255, 230), 1.8 * f), new Point(7 * f, 8 * f), new Point(14 * f, 8 * f));
                    context.DrawLine(new Pen(Solid(255, 255, 255, 230), 1.8 * f), new Point(7 * f, 12 * f), new Point(17 * f, 12 * f));
                    context.DrawLine(new Pen(Solid(255, 255, 255, 230), 1.8 * f), new Point(7 * f, 16 * f), new Point(13 * f, 16 * f));
                    break;

                case FluentIconKind.RecentList:
                    context.DrawEllipse(null, new Pen(Grad(0, 120, 215, 0, 168, 232, 45), 2.2 * f), new Point(12 * f, 12 * f), 8 * f, 8 * f);
                    context.DrawLine(new Pen(Grad(0, 120, 215, 0, 168, 232, 45), 2 * f), new Point(12 * f, 7 * f), new Point(12 * f, 12 * f));
                    context.DrawLine(new Pen(Grad(0, 120, 215, 0, 168, 232, 45), 2 * f), new Point(12 * f, 12 * f), new Point(16 * f, 14 * f));
                    break;

                case FluentIconKind.VstPlugin:
                    context.DrawRectangle(Grad(119, 82, 254, 142, 68, 173, 45), null, new Rect(3 * f, 3 * f, 18 * f, 18 * f), 3 * f, 3 * f);
                    context.DrawEllipse(Grad(255, 255, 255, 200, 200, 220, 90), null, new Point(12 * f, 12 * f), 6 * f, 6 * f);
                    context.DrawEllipse(Solid(119, 82, 254), null, new Point(12 * f, 8 * f), 1.5 * f, 1.5 * f);
                    break;

                case FluentIconKind.ChevronUp:
                    context.DrawLine(new Pen(Solid(180, 190, 205), 2 * f), new Point(6 * f, 15 * f), new Point(12 * f, 9 * f));
                    context.DrawLine(new Pen(Solid(180, 190, 205), 2 * f), new Point(12 * f, 9 * f), new Point(18 * f, 15 * f));
                    break;

                case FluentIconKind.ChevronDown:
                    context.DrawLine(new Pen(Solid(180, 190, 205), 2 * f), new Point(6 * f, 9 * f), new Point(12 * f, 15 * f));
                    context.DrawLine(new Pen(Solid(180, 190, 205), 2 * f), new Point(12 * f, 15 * f), new Point(18 * f, 9 * f));
                    break;

                case FluentIconKind.EditResize:
                    context.DrawLine(new Pen(Solid(180, 190, 205), 2 * f), new Point(5 * f, 5 * f), new Point(19 * f, 19 * f));
                    context.DrawLine(new Pen(Solid(180, 190, 205), 2 * f), new Point(14 * f, 5 * f), new Point(19 * f, 5 * f));
                    context.DrawLine(new Pen(Solid(180, 190, 205), 2 * f), new Point(19 * f, 5 * f), new Point(19 * f, 10 * f));
                    break;

                case FluentIconKind.WindowMin:
                    context.DrawLine(new Pen(Solid(180, 190, 205), 1.6 * f), new Point(4 * f, 12 * f), new Point(20 * f, 12 * f));
                    break;

                case FluentIconKind.WindowMax:
                    context.DrawRectangle(null, new Pen(Solid(180, 190, 205), 1.6 * f), new Rect(5 * f, 5 * f, 14 * f, 14 * f), 1 * f, 1 * f);
                    break;

                case FluentIconKind.WindowRestore:
                    context.DrawRectangle(null, new Pen(Solid(180, 190, 205), 1.4 * f), new Rect(7 * f, 4 * f, 13 * f, 13 * f), 1 * f, 1 * f);
                    context.DrawRectangle(Solid(20, 25, 35), new Pen(Solid(180, 190, 205), 1.4 * f), new Rect(4 * f, 7 * f, 13 * f, 13 * f), 1 * f, 1 * f);
                    break;

                case FluentIconKind.WindowClose:
                    context.DrawLine(new Pen(Solid(180, 190, 205), 1.8 * f), new Point(6 * f, 6 * f), new Point(18 * f, 18 * f));
                    context.DrawLine(new Pen(Solid(180, 190, 205), 1.8 * f), new Point(18 * f, 6 * f), new Point(6 * f, 18 * f));
                    break;

                default:
                    context.DrawEllipse(Grad(0, 120, 215, 0, 168, 232, 45), null, new Point(12 * f, 12 * f), 9 * f, 9 * f);
                    break;
            }
        }
    }
}
