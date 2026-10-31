using System;
using System.Collections.Generic;
using Microsoft.UI;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using Windows.Foundation;
using Windows.UI;

namespace FurinaPlayer.UI.Controls;

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
    EditResize
}

/// <summary>
/// Microsoft Fluent UI Colorful Multi-Layer Vector Icon Factory.
/// Renders layered geometric gradient tiles & depth plates matching the Fluent Design colorful iconography.
/// </summary>
public static class FluentIconFactory
{
    public static LinearGradientBrush Grad(byte r1, byte g1, byte b1, byte r2, byte g2, byte b2, double angle = 45)
    {
        var brush = new LinearGradientBrush
        {
            StartPoint = new Point(0, 0),
            EndPoint = angle == 90 ? new Point(0, 1) : (angle == 0 ? new Point(1, 0) : new Point(1, 1))
        };
        brush.GradientStops.Add(new GradientStop { Color = Color.FromArgb(255, r1, g1, b1), Offset = 0.0 });
        brush.GradientStops.Add(new GradientStop { Color = Color.FromArgb(255, r2, g2, b2), Offset = 1.0 });
        return brush;
    }

    public static SolidColorBrush Solid(byte r, byte g, byte b, byte a = 255)
        => new SolidColorBrush(Color.FromArgb(a, r, g, b));

    public static FrameworkElement Create(FluentIconKind kind, double size = 20)
    {
        double s = Math.Max(14, size);
        var root = new Grid
        {
            Width = s,
            Height = s,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };

        switch (kind)
        {
            case FluentIconKind.AppLogo:
                BuildAppLogo(root, s);
                break;
            case FluentIconKind.Home:
                BuildHome(root, s);
                break;
            case FluentIconKind.NowPlaying:
                BuildNowPlaying(root, s);
                break;
            case FluentIconKind.AudioEditor:
                BuildAudioEditor(root, s);
                break;
            case FluentIconKind.DspEffects:
                BuildDspEffects(root, s);
                break;
            case FluentIconKind.Settings:
                BuildSettings(root, s);
                break;
            case FluentIconKind.Play:
                BuildPlay(root, s);
                break;
            case FluentIconKind.Pause:
                BuildPause(root, s);
                break;
            case FluentIconKind.Previous:
                BuildSkip(root, s, isNext: false);
                break;
            case FluentIconKind.Next:
                BuildSkip(root, s, isNext: true);
                break;
            case FluentIconKind.Stop:
                BuildStop(root, s);
                break;
            case FluentIconKind.ModeSequential:
                BuildPlayMode(root, s, "SEQ", Grad(14, 165, 233, 37, 99, 235), Grad(56, 189, 248, 59, 130, 246));
                break;
            case FluentIconKind.ModeLoopOne:
                BuildPlayMode(root, s, "1", Grad(16, 185, 129, 5, 150, 105), Grad(52, 211, 153, 16, 185, 129));
                break;
            case FluentIconKind.ModeLoopAll:
                BuildPlayMode(root, s, "ALL", Grad(99, 102, 241, 67, 56, 202), Grad(129, 140, 248, 99, 102, 241));
                break;
            case FluentIconKind.ModeShuffle:
                BuildPlayMode(root, s, "SHF", Grad(245, 158, 11, 217, 119, 6), Grad(251, 191, 36, 245, 158, 11));
                break;
            case FluentIconKind.Volume:
                BuildVolume(root, s);
                break;
            case FluentIconKind.RecentList:
                BuildRecentList(root, s);
                break;
            case FluentIconKind.Eq:
                BuildEq(root, s);
                break;
            case FluentIconKind.FolderScan:
                BuildFolderScan(root, s);
                break;
            case FluentIconKind.FileAdd:
                BuildFileAdd(root, s);
                break;
            case FluentIconKind.AlbumGrid:
                BuildAlbumGrid(root, s);
                break;
            case FluentIconKind.Search:
                BuildSearch(root, s);
                break;
            case FluentIconKind.ClearFilter:
                BuildClearFilter(root, s);
                break;
            case FluentIconKind.Delete:
                BuildDelete(root, s);
                break;
            case FluentIconKind.LyricsImport:
                BuildLyricsImport(root, s);
                break;
            case FluentIconKind.MusicHero:
                BuildMusicHero(root, s);
                break;
            case FluentIconKind.ModeAudition:
                BuildModeBadge(root, s, "Au", Grad(6, 78, 59, 15, 118, 110), Grad(16, 185, 129, 20, 184, 166));
                break;
            case FluentIconKind.ModePowerPoint:
                BuildModeBadge(root, s, "P", Grad(154, 52, 18, 185, 28, 28), Grad(249, 115, 22, 220, 38, 38));
                break;
            case FluentIconKind.ModeClassic:
                BuildModeBadge(root, s, "♪", Grad(30, 58, 138, 67, 56, 202), Grad(56, 189, 248, 99, 102, 241));
                break;
            case FluentIconKind.Rack:
                BuildRack(root, s);
                break;
            case FluentIconKind.Spatial:
                BuildSpatial(root, s);
                break;
            case FluentIconKind.Dynamics:
                BuildDynamics(root, s);
                break;
            case FluentIconKind.HiRes:
                BuildHiRes(root, s);
                break;
            case FluentIconKind.VstPlugin:
                BuildVstPlugin(root, s);
                break;
            case FluentIconKind.FreeCanvas:
                BuildFreeCanvas(root, s);
                break;
            case FluentIconKind.RefreshReset:
                BuildRefreshReset(root, s);
                break;
            case FluentIconKind.Save:
                BuildSave(root, s);
                break;
            case FluentIconKind.Wallpaper:
                BuildWallpaper(root, s);
                break;
            case FluentIconKind.Font:
                BuildFont(root, s);
                break;
            case FluentIconKind.DeviceAudio:
                BuildDeviceAudio(root, s);
                break;
            case FluentIconKind.DiagnosticLog:
                BuildDiagnosticLog(root, s);
                break;
            case FluentIconKind.WindowMin:
                BuildWindowMin(root, s);
                break;
            case FluentIconKind.WindowMax:
                BuildWindowMax(root, s);
                break;
            case FluentIconKind.WindowRestore:
                BuildWindowRestore(root, s);
                break;
            case FluentIconKind.WindowClose:
                BuildWindowClose(root, s);
                break;
            case FluentIconKind.ChevronUp:
                BuildChevron(root, s, up: true);
                break;
            case FluentIconKind.ChevronDown:
                BuildChevron(root, s, up: false);
                break;
            case FluentIconKind.EditResize:
                BuildEditResize(root, s);
                break;
        }

        return root;
    }

    private static void BuildAppLogo(Grid root, double s)
    {
        root.Children.Add(new Border
        {
            Width = s * 0.78,
            Height = s * 0.78,
            CornerRadius = new CornerRadius(s * 0.22),
            HorizontalAlignment = HorizontalAlignment.Right,
            VerticalAlignment = VerticalAlignment.Bottom,
            Background = Grad(124, 58, 237, 67, 56, 202)
        });
        root.Children.Add(new Border
        {
            Width = s * 0.82,
            Height = s * 0.82,
            CornerRadius = new CornerRadius(s * 0.24),
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Top,
            Background = Grad(56, 189, 248, 37, 99, 235),
            BorderBrush = Solid(255, 255, 255, 90),
            BorderThickness = new Thickness(1)
        });
        var tri = new Polygon
        {
            Fill = Solid(255, 255, 255),
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(0, 0, s * 0.06, s * 0.06),
            Points = new PointCollection
            {
                new Point(s * 0.28, s * 0.22),
                new Point(s * 0.28, s * 0.58),
                new Point(s * 0.58, s * 0.40)
            }
        };
        root.Children.Add(tri);
    }

    private static void BuildHome(Grid root, double s)
    {
        root.Children.Add(new Border
        {
            Width = s * 0.76,
            Height = s * 0.62,
            CornerRadius = new CornerRadius(s * 0.16),
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Bottom,
            Background = Grad(37, 99, 235, 29, 78, 216)
        });
        var roof = new Polygon
        {
            Fill = Grad(103, 232, 249, 14, 165, 233),
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Top,
            Points = new PointCollection
            {
                new Point(s * 0.50, s * 0.08),
                new Point(s * 0.94, s * 0.48),
                new Point(s * 0.06, s * 0.48)
            }
        };
        root.Children.Add(roof);
        root.Children.Add(new Border
        {
            Width = s * 0.26,
            Height = s * 0.34,
            CornerRadius = new CornerRadius(s * 0.08, s * 0.08, s * 0.04, s * 0.04),
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Bottom,
            Margin = new Thickness(0, 0, 0, s * 0.04),
            Background = Grad(224, 242, 254, 125, 211, 252)
        });
    }

    private static void BuildNowPlaying(Grid root, double s)
    {
        root.Children.Add(new Border
        {
            Width = s * 0.84,
            Height = s * 0.72,
            CornerRadius = new CornerRadius(s * 0.16),
            HorizontalAlignment = HorizontalAlignment.Right,
            VerticalAlignment = VerticalAlignment.Bottom,
            Background = Grad(139, 92, 246, 79, 70, 229)
        });
        var mainPlate = new Border
        {
            Width = s * 0.86,
            Height = s * 0.76,
            CornerRadius = new CornerRadius(s * 0.18),
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Top,
            Background = Grad(56, 189, 248, 2, 132, 199)
        };
        root.Children.Add(mainPlate);
        root.Children.Add(new Border
        {
            Width = s * 0.86,
            Height = s * 0.22,
            CornerRadius = new CornerRadius(s * 0.18, s * 0.18, 0, 0),
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Top,
            Background = Grad(125, 211, 252, 56, 189, 248)
        });
        root.Children.Add(new Polygon
        {
            Fill = Solid(255, 255, 255),
            Points = new PointCollection
            {
                new Point(s * 0.33, s * 0.34),
                new Point(s * 0.33, s * 0.64),
                new Point(s * 0.60, s * 0.49)
            }
        });
    }

    private static void BuildAudioEditor(Grid root, double s)
    {
        root.Children.Add(new Border
        {
            Width = s * 0.88,
            Height = s * 0.82,
            CornerRadius = new CornerRadius(s * 0.20),
            Background = Grad(13, 148, 136, 15, 118, 110)
        });
        root.Children.Add(new Border
        {
            Width = s * 0.82,
            Height = s * 0.76,
            CornerRadius = new CornerRadius(s * 0.18),
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Top,
            Background = Grad(45, 212, 191, 16, 185, 129)
        });
        var bars = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = s * 0.07,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };
        double[] heights = { 0.26, 0.48, 0.34, 0.54, 0.30 };
        foreach (var h in heights)
        {
            bars.Children.Add(new Border
            {
                Width = Math.Max(2, s * 0.085),
                Height = s * h,
                CornerRadius = new CornerRadius(s * 0.04),
                VerticalAlignment = VerticalAlignment.Center,
                Background = Solid(255, 255, 255)
            });
        }
        root.Children.Add(bars);
    }

    private static void BuildDspEffects(Grid root, double s)
    {
        root.Children.Add(new Border
        {
            Width = s * 0.80,
            Height = s * 0.80,
            CornerRadius = new CornerRadius(s * 0.20),
            HorizontalAlignment = HorizontalAlignment.Right,
            VerticalAlignment = VerticalAlignment.Bottom,
            Background = Grad(192, 38, 211, 126, 34, 206)
        });
        root.Children.Add(new Border
        {
            Width = s * 0.80,
            Height = s * 0.80,
            CornerRadius = new CornerRadius(s * 0.20),
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Top,
            Background = Grad(168, 85, 247, 99, 102, 241)
        });
        var canvas = new Canvas { Width = s * 0.80, Height = s * 0.80, HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Top };
        double[] xs = { 0.22, 0.40, 0.58 };
        double[] ys = { 0.46, 0.24, 0.50 };
        for (int i = 0; i < 3; i++)
        {
            var line = new Border
            {
                Width = Math.Max(1.8, s * 0.06),
                Height = s * 0.50,
                CornerRadius = new CornerRadius(2),
                Background = Solid(255, 255, 255, 190)
            };
            Canvas.SetLeft(line, s * xs[i] - s * 0.03);
            Canvas.SetTop(line, s * 0.15);
            canvas.Children.Add(line);

            var knob = new Ellipse
            {
                Width = s * 0.18,
                Height = s * 0.18,
                Fill = i == 1 ? Grad(253, 224, 71, 245, 158, 11) : Grad(103, 232, 249, 56, 189, 248)
            };
            Canvas.SetLeft(knob, s * xs[i] - s * 0.09);
            Canvas.SetTop(knob, s * ys[i]);
            canvas.Children.Add(knob);
        }
        root.Children.Add(canvas);
    }

    private static void BuildSettings(Grid root, double s)
    {
        var outer = new Grid { Width = s * 0.90, Height = s * 0.90 };
        int[] angles = { 0, 45, 90, 135 };
        foreach (var ang in angles)
        {
            outer.Children.Add(new Border
            {
                Width = s * 0.30,
                Height = s * 0.90,
                CornerRadius = new CornerRadius(s * 0.08),
                Background = Grad(148, 163, 184, 71, 85, 105),
                RenderTransformOrigin = new Point(0.5, 0.5),
                RenderTransform = new RotateTransform { Angle = ang }
            });
        }
        outer.Children.Add(new Ellipse
        {
            Width = s * 0.72,
            Height = s * 0.72,
            Fill = Grad(203, 213, 225, 100, 116, 139)
        });
        outer.Children.Add(new Ellipse
        {
            Width = s * 0.38,
            Height = s * 0.38,
            Fill = Grad(56, 189, 248, 37, 99, 235)
        });
        root.Children.Add(outer);
    }

    private static void BuildPlay(Grid root, double s)
    {
        root.Children.Add(new Ellipse
        {
            Width = s * 0.94,
            Height = s * 0.94,
            Fill = Grad(56, 189, 248, 37, 99, 235)
        });
        root.Children.Add(new Ellipse
        {
            Width = s * 0.78,
            Height = s * 0.78,
            Fill = Grad(14, 165, 233, 29, 78, 216)
        });
        root.Children.Add(new Polygon
        {
            Fill = Solid(255, 255, 255),
            Points = new PointCollection
            {
                new Point(s * 0.38, s * 0.28),
                new Point(s * 0.38, s * 0.72),
                new Point(s * 0.72, s * 0.50)
            }
        });
    }

    private static void BuildPause(Grid root, double s)
    {
        root.Children.Add(new Ellipse
        {
            Width = s * 0.94,
            Height = s * 0.94,
            Fill = Grad(56, 189, 248, 37, 99, 235)
        });
        root.Children.Add(new Ellipse
        {
            Width = s * 0.78,
            Height = s * 0.78,
            Fill = Grad(14, 165, 233, 29, 78, 216)
        });
        var sp = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = s * 0.12,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };
        sp.Children.Add(new Border
        {
            Width = s * 0.14,
            Height = s * 0.42,
            CornerRadius = new CornerRadius(s * 0.05),
            Background = Solid(255, 255, 255)
        });
        sp.Children.Add(new Border
        {
            Width = s * 0.14,
            Height = s * 0.42,
            CornerRadius = new CornerRadius(s * 0.05),
            Background = Solid(255, 255, 255)
        });
        root.Children.Add(sp);
    }

    private static void BuildSkip(Grid root, double s, bool isNext)
    {
        root.Children.Add(new Border
        {
            Width = s * 0.88,
            Height = s * 0.88,
            CornerRadius = new CornerRadius(s * 0.24),
            Background = Grad(56, 189, 248, 59, 130, 246)
        });
        var bar = new Border
        {
            Width = s * 0.11,
            Height = s * 0.44,
            CornerRadius = new CornerRadius(s * 0.04),
            Background = Grad(186, 230, 253, 255, 255, 255),
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = isNext ? new Thickness(s * 0.28, 0, 0, 0) : new Thickness(0, 0, s * 0.28, 0)
        };
        var tri = new Polygon
        {
            Fill = Solid(255, 255, 255),
            Points = isNext
                ? new PointCollection
                {
                    new Point(s * 0.28, s * 0.28),
                    new Point(s * 0.28, s * 0.72),
                    new Point(s * 0.60, s * 0.50)
                }
                : new PointCollection
                {
                    new Point(s * 0.72, s * 0.28),
                    new Point(s * 0.72, s * 0.72),
                    new Point(s * 0.40, s * 0.50)
                }
        };
        root.Children.Add(tri);
        root.Children.Add(bar);
    }

    private static void BuildStop(Grid root, double s)
    {
        root.Children.Add(new Border
        {
            Width = s * 0.86,
            Height = s * 0.86,
            CornerRadius = new CornerRadius(s * 0.24),
            Background = Grad(251, 113, 133, 225, 29, 72)
        });
        root.Children.Add(new Border
        {
            Width = s * 0.38,
            Height = s * 0.38,
            CornerRadius = new CornerRadius(s * 0.08),
            Background = Solid(255, 255, 255)
        });
    }

    private static void BuildPlayMode(Grid root, double s, string badge, Brush backBrush, Brush frontBrush)
    {
        root.Children.Add(new Border
        {
            Width = s * 0.82,
            Height = s * 0.76,
            CornerRadius = new CornerRadius(s * 0.20),
            HorizontalAlignment = HorizontalAlignment.Right,
            VerticalAlignment = VerticalAlignment.Bottom,
            Background = backBrush
        });
        var front = new Border
        {
            Width = s * 0.84,
            Height = s * 0.78,
            CornerRadius = new CornerRadius(s * 0.22),
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Top,
            Background = frontBrush
        };
        front.Child = new TextBlock
        {
            Text = badge,
            FontSize = badge.Length > 1 ? s * 0.30 : s * 0.42,
            FontWeight = FontWeights.ExtraBold,
            Foreground = Solid(255, 255, 255),
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };
        root.Children.Add(front);
    }

    private static void BuildVolume(Grid root, double s)
    {
        root.Children.Add(new Border
        {
            Width = s * 0.88,
            Height = s * 0.88,
            CornerRadius = new CornerRadius(s * 0.24),
            Background = Grad(56, 189, 248, 37, 99, 235)
        });
        root.Children.Add(new Polygon
        {
            Fill = Solid(255, 255, 255),
            Points = new PointCollection
            {
                new Point(s * 0.22, s * 0.40),
                new Point(s * 0.36, s * 0.40),
                new Point(s * 0.54, s * 0.24),
                new Point(s * 0.54, s * 0.76),
                new Point(s * 0.36, s * 0.60),
                new Point(s * 0.22, s * 0.60)
            }
        });
        root.Children.Add(new Border
        {
            Width = s * 0.10,
            Height = s * 0.34,
            CornerRadius = new CornerRadius(s * 0.05),
            HorizontalAlignment = HorizontalAlignment.Right,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(0, 0, s * 0.22, 0),
            Background = Grad(165, 243, 252, 255, 255, 255)
        });
    }

    private static void BuildRecentList(Grid root, double s)
    {
        root.Children.Add(new Border
        {
            Width = s * 0.80,
            Height = s * 0.80,
            CornerRadius = new CornerRadius(s * 0.20),
            HorizontalAlignment = HorizontalAlignment.Right,
            VerticalAlignment = VerticalAlignment.Bottom,
            Background = Grad(99, 102, 241, 67, 56, 202)
        });
        root.Children.Add(new Ellipse
        {
            Width = s * 0.82,
            Height = s * 0.82,
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Top,
            Fill = Grad(56, 189, 248, 14, 165, 233)
        });
        root.Children.Add(new Border
        {
            Width = s * 0.09,
            Height = s * 0.26,
            CornerRadius = new CornerRadius(2),
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Top,
            Margin = new Thickness(s * 0.37, s * 0.20, 0, 0),
            Background = Solid(255, 255, 255)
        });
        root.Children.Add(new Border
        {
            Width = s * 0.24,
            Height = s * 0.09,
            CornerRadius = new CornerRadius(2),
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Top,
            Margin = new Thickness(s * 0.37, s * 0.39, 0, 0),
            Background = Solid(255, 255, 255)
        });
    }

    private static void BuildEq(Grid root, double s)
    {
        BuildDspEffects(root, s);
    }

    private static void BuildFolderScan(Grid root, double s)
    {
        root.Children.Add(new Border
        {
            Width = s * 0.50,
            Height = s * 0.36,
            CornerRadius = new CornerRadius(s * 0.10, s * 0.10, 0, 0),
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Top,
            Margin = new Thickness(s * 0.06, s * 0.10, 0, 0),
            Background = Grad(217, 119, 6, 180, 83, 9)
        });
        root.Children.Add(new Border
        {
            Width = s * 0.74,
            Height = s * 0.50,
            CornerRadius = new CornerRadius(s * 0.08),
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Top,
            Margin = new Thickness(0, s * 0.18, 0, 0),
            Background = Grad(224, 242, 254, 186, 230, 253)
        });
        root.Children.Add(new Border
        {
            Width = s * 0.88,
            Height = s * 0.58,
            CornerRadius = new CornerRadius(s * 0.14),
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Bottom,
            Margin = new Thickness(s * 0.04, 0, 0, s * 0.08),
            Background = Grad(253, 224, 71, 245, 158, 11)
        });
        root.Children.Add(new Ellipse
        {
            Width = s * 0.42,
            Height = s * 0.42,
            HorizontalAlignment = HorizontalAlignment.Right,
            VerticalAlignment = VerticalAlignment.Bottom,
            Fill = Grad(56, 189, 248, 37, 99, 235)
        });
    }

    private static void BuildFileAdd(Grid root, double s)
    {
        root.Children.Add(new Border
        {
            Width = s * 0.68,
            Height = s * 0.84,
            CornerRadius = new CornerRadius(s * 0.14),
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Top,
            Margin = new Thickness(s * 0.08, s * 0.06, 0, 0),
            Background = Grad(96, 165, 250, 37, 99, 235)
        });
        root.Children.Add(new Border
        {
            Width = s * 0.24,
            Height = s * 0.24,
            CornerRadius = new CornerRadius(0, s * 0.14, 0, s * 0.08),
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Top,
            Margin = new Thickness(s * 0.52, s * 0.06, 0, 0),
            Background = Grad(186, 230, 253, 125, 211, 252)
        });
        var badge = new Grid
        {
            Width = s * 0.48,
            Height = s * 0.48,
            HorizontalAlignment = HorizontalAlignment.Right,
            VerticalAlignment = VerticalAlignment.Bottom
        };
        badge.Children.Add(new Ellipse { Fill = Grad(52, 211, 153, 5, 150, 105) });
        badge.Children.Add(new Border
        {
            Width = s * 0.24,
            Height = s * 0.07,
            CornerRadius = new CornerRadius(2),
            Background = Solid(255, 255, 255)
        });
        badge.Children.Add(new Border
        {
            Width = s * 0.07,
            Height = s * 0.24,
            CornerRadius = new CornerRadius(2),
            Background = Solid(255, 255, 255)
        });
        root.Children.Add(badge);
    }

    private static void BuildAlbumGrid(Grid root, double s)
    {
        double cell = s * 0.39;
        var c1 = new Border { Width = cell, Height = cell, CornerRadius = new CornerRadius(s * 0.10), HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Top, Margin = new Thickness(s * 0.06), Background = Grad(56, 189, 248, 14, 165, 233) };
        var c2 = new Border { Width = cell, Height = cell, CornerRadius = new CornerRadius(s * 0.10), HorizontalAlignment = HorizontalAlignment.Right, VerticalAlignment = VerticalAlignment.Top, Margin = new Thickness(s * 0.06), Background = Grad(168, 85, 247, 126, 34, 206) };
        var c3 = new Border { Width = cell, Height = cell, CornerRadius = new CornerRadius(s * 0.10), HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Bottom, Margin = new Thickness(s * 0.06), Background = Grad(52, 211, 153, 16, 185, 129) };
        var c4 = new Border { Width = cell, Height = cell, CornerRadius = new CornerRadius(s * 0.10), HorizontalAlignment = HorizontalAlignment.Right, VerticalAlignment = VerticalAlignment.Bottom, Margin = new Thickness(s * 0.06), Background = Grad(251, 191, 36, 245, 158, 11) };
        root.Children.Add(c1);
        root.Children.Add(c2);
        root.Children.Add(c3);
        root.Children.Add(c4);
    }

    private static void BuildSearch(Grid root, double s)
    {
        root.Children.Add(new Border
        {
            Width = s * 0.36,
            Height = s * 0.14,
            CornerRadius = new CornerRadius(s * 0.07),
            HorizontalAlignment = HorizontalAlignment.Right,
            VerticalAlignment = VerticalAlignment.Bottom,
            Margin = new Thickness(0, 0, s * 0.08, s * 0.12),
            Background = Grad(99, 102, 241, 67, 56, 202),
            RenderTransformOrigin = new Point(0.5, 0.5),
            RenderTransform = new RotateTransform { Angle = 45 }
        });
        root.Children.Add(new Ellipse
        {
            Width = s * 0.68,
            Height = s * 0.68,
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Top,
            Margin = new Thickness(s * 0.06),
            Fill = Grad(56, 189, 248, 37, 99, 235)
        });
        root.Children.Add(new Ellipse
        {
            Width = s * 0.38,
            Height = s * 0.38,
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Top,
            Margin = new Thickness(s * 0.21),
            Fill = Grad(224, 242, 254, 186, 230, 253)
        });
    }

    private static void BuildClearFilter(Grid root, double s)
    {
        root.Children.Add(new Border
        {
            Width = s * 0.86,
            Height = s * 0.86,
            CornerRadius = new CornerRadius(s * 0.24),
            Background = Grad(251, 146, 60, 234, 88, 12)
        });
        root.Children.Add(new Border
        {
            Width = s * 0.48,
            Height = s * 0.10,
            CornerRadius = new CornerRadius(2),
            Background = Solid(255, 255, 255),
            RenderTransformOrigin = new Point(0.5, 0.5),
            RenderTransform = new RotateTransform { Angle = 45 }
        });
        root.Children.Add(new Border
        {
            Width = s * 0.48,
            Height = s * 0.10,
            CornerRadius = new CornerRadius(2),
            Background = Solid(255, 255, 255),
            RenderTransformOrigin = new Point(0.5, 0.5),
            RenderTransform = new RotateTransform { Angle = -45 }
        });
    }

    private static void BuildDelete(Grid root, double s)
    {
        root.Children.Add(new Border
        {
            Width = s * 0.72,
            Height = s * 0.14,
            CornerRadius = new CornerRadius(s * 0.06),
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Top,
            Margin = new Thickness(0, s * 0.10, 0, 0),
            Background = Grad(253, 164, 175, 244, 63, 94)
        });
        root.Children.Add(new Border
        {
            Width = s * 0.62,
            Height = s * 0.66,
            CornerRadius = new CornerRadius(s * 0.08, s * 0.08, s * 0.16, s * 0.16),
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Bottom,
            Margin = new Thickness(0, 0, 0, s * 0.06),
            Background = Grad(244, 63, 94, 190, 18, 60)
        });
    }

    private static void BuildLyricsImport(Grid root, double s)
    {
        root.Children.Add(new Border
        {
            Width = s * 0.78,
            Height = s * 0.66,
            CornerRadius = new CornerRadius(s * 0.20),
            HorizontalAlignment = HorizontalAlignment.Right,
            VerticalAlignment = VerticalAlignment.Bottom,
            Background = Grad(99, 102, 241, 67, 56, 202)
        });
        var bubble = new Border
        {
            Width = s * 0.80,
            Height = s * 0.68,
            CornerRadius = new CornerRadius(s * 0.22),
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Top,
            Background = Grad(56, 189, 248, 14, 165, 233)
        };
        var lines = new StackPanel
        {
            Spacing = s * 0.08,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };
        lines.Children.Add(new Border { Width = s * 0.44, Height = s * 0.08, CornerRadius = new CornerRadius(2), Background = Solid(255, 255, 255) });
        lines.Children.Add(new Border { Width = s * 0.30, Height = s * 0.08, CornerRadius = new CornerRadius(2), HorizontalAlignment = HorizontalAlignment.Left, Background = Solid(255, 255, 255, 210) });
        bubble.Child = lines;
        root.Children.Add(bubble);
    }

    private static void BuildMusicHero(Grid root, double s)
    {
        root.Children.Add(new Border
        {
            Width = s * 0.82,
            Height = s * 0.82,
            CornerRadius = new CornerRadius(s * 0.24),
            HorizontalAlignment = HorizontalAlignment.Right,
            VerticalAlignment = VerticalAlignment.Bottom,
            Background = Grad(168, 85, 247, 109, 40, 217)
        });
        root.Children.Add(new Ellipse
        {
            Width = s * 0.88,
            Height = s * 0.88,
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Top,
            Fill = Grad(56, 189, 248, 37, 99, 235)
        });
        root.Children.Add(new Ellipse
        {
            Width = s * 0.56,
            Height = s * 0.56,
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Top,
            Margin = new Thickness(s * 0.16),
            Fill = Grad(30, 41, 59, 15, 23, 42)
        });
        root.Children.Add(new Ellipse
        {
            Width = s * 0.24,
            Height = s * 0.24,
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Top,
            Margin = new Thickness(s * 0.32),
            Fill = Grad(253, 224, 71, 245, 158, 11)
        });
    }

    private static void BuildModeBadge(Grid root, double s, string text, Brush back, Brush front)
    {
        root.Children.Add(new Border
        {
            Width = s * 0.78,
            Height = s * 0.86,
            CornerRadius = new CornerRadius(s * 0.18),
            HorizontalAlignment = HorizontalAlignment.Right,
            VerticalAlignment = VerticalAlignment.Center,
            Background = back
        });
        var badge = new Border
        {
            Width = s * 0.64,
            Height = s * 0.64,
            CornerRadius = new CornerRadius(s * 0.16),
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Center,
            Background = front,
            BorderBrush = Solid(255, 255, 255, 90),
            BorderThickness = new Thickness(1)
        };
        badge.Child = new TextBlock
        {
            Text = text,
            FontSize = s * 0.38,
            FontWeight = FontWeights.ExtraBold,
            Foreground = Solid(255, 255, 255),
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };
        root.Children.Add(badge);
    }

    private static void BuildRack(Grid root, double s)
    {
        var sp = new StackPanel
        {
            Spacing = s * 0.06,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };
        sp.Children.Add(new Border { Width = s * 0.86, Height = s * 0.22, CornerRadius = new CornerRadius(s * 0.07), Background = Grad(253, 224, 71, 245, 158, 11) });
        sp.Children.Add(new Border { Width = s * 0.86, Height = s * 0.22, CornerRadius = new CornerRadius(s * 0.07), Background = Grad(251, 146, 60, 234, 88, 12) });
        sp.Children.Add(new Border { Width = s * 0.86, Height = s * 0.22, CornerRadius = new CornerRadius(s * 0.07), Background = Grad(168, 85, 247, 126, 34, 206) });
        root.Children.Add(sp);
    }

    private static void BuildSpatial(Grid root, double s)
    {
        root.Children.Add(new Ellipse
        {
            Width = s * 0.92,
            Height = s * 0.92,
            Fill = Grad(37, 99, 235, 30, 58, 138)
        });
        root.Children.Add(new Ellipse
        {
            Width = s * 0.64,
            Height = s * 0.64,
            Fill = Grad(14, 165, 233, 2, 132, 199)
        });
        root.Children.Add(new Ellipse
        {
            Width = s * 0.34,
            Height = s * 0.34,
            Fill = Grad(103, 232, 249, 45, 212, 191)
        });
    }

    private static void BuildDynamics(Grid root, double s)
    {
        root.Children.Add(new Ellipse
        {
            Width = s * 0.90,
            Height = s * 0.90,
            Fill = Grad(249, 115, 22, 220, 38, 38)
        });
        root.Children.Add(new Polygon
        {
            Fill = Grad(254, 240, 138, 255, 255, 255),
            Points = new PointCollection
            {
                new Point(s * 0.56, s * 0.16),
                new Point(s * 0.28, s * 0.54),
                new Point(s * 0.48, s * 0.54),
                new Point(s * 0.42, s * 0.84),
                new Point(s * 0.72, s * 0.44),
                new Point(s * 0.52, s * 0.44)
            }
        });
    }

    private static void BuildHiRes(Grid root, double s)
    {
        root.Children.Add(new Border
        {
            Width = s * 0.72,
            Height = s * 0.72,
            CornerRadius = new CornerRadius(s * 0.16),
            Background = Grad(253, 224, 71, 217, 119, 6),
            RenderTransformOrigin = new Point(0.5, 0.5),
            RenderTransform = new RotateTransform { Angle = 45 }
        });
        var inner = new Border
        {
            Width = s * 0.56,
            Height = s * 0.40,
            CornerRadius = new CornerRadius(s * 0.10),
            Background = Grad(16, 185, 129, 5, 150, 105)
        };
        inner.Child = new TextBlock
        {
            Text = "HD",
            FontSize = s * 0.26,
            FontWeight = FontWeights.ExtraBold,
            Foreground = Solid(255, 255, 255),
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };
        root.Children.Add(inner);
    }

    private static void BuildVstPlugin(Grid root, double s)
    {
        root.Children.Add(new Border
        {
            Width = s * 0.74,
            Height = s * 0.74,
            CornerRadius = new CornerRadius(s * 0.18),
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Bottom,
            Background = Grad(139, 92, 246, 109, 40, 217)
        });
        root.Children.Add(new Border
        {
            Width = s * 0.52,
            Height = s * 0.52,
            CornerRadius = new CornerRadius(s * 0.14),
            HorizontalAlignment = HorizontalAlignment.Right,
            VerticalAlignment = VerticalAlignment.Top,
            Background = Grad(56, 189, 248, 14, 165, 233)
        });
    }

    private static void BuildFreeCanvas(Grid root, double s)
    {
        root.Children.Add(new Border
        {
            Width = s * 0.88,
            Height = s * 0.76,
            CornerRadius = new CornerRadius(s * 0.18),
            Background = Grad(56, 189, 248, 37, 99, 235)
        });
        root.Children.Add(new Border
        {
            Width = s * 0.46,
            Height = s * 0.14,
            CornerRadius = new CornerRadius(s * 0.06),
            Background = Grad(253, 224, 71, 245, 158, 11),
            RenderTransformOrigin = new Point(0.5, 0.5),
            RenderTransform = new RotateTransform { Angle = -30 }
        });
    }

    private static void BuildRefreshReset(Grid root, double s)
    {
        root.Children.Add(new Ellipse
        {
            Width = s * 0.86,
            Height = s * 0.86,
            Fill = Grad(56, 189, 248, 37, 99, 235)
        });
        root.Children.Add(new Ellipse
        {
            Width = s * 0.46,
            Height = s * 0.46,
            Fill = Grad(224, 242, 254, 186, 230, 253)
        });
        root.Children.Add(new Polygon
        {
            Fill = Solid(255, 255, 255),
            Points = new PointCollection
            {
                new Point(s * 0.52, s * 0.08),
                new Point(s * 0.80, s * 0.24),
                new Point(s * 0.52, s * 0.38)
            }
        });
    }

    private static void BuildSave(Grid root, double s)
    {
        root.Children.Add(new Border
        {
            Width = s * 0.86,
            Height = s * 0.86,
            CornerRadius = new CornerRadius(s * 0.24),
            Background = Grad(52, 211, 153, 5, 150, 105)
        });
        root.Children.Add(new Polygon
        {
            Fill = Solid(255, 255, 255),
            Points = new PointCollection
            {
                new Point(s * 0.24, s * 0.52),
                new Point(s * 0.42, s * 0.70),
                new Point(s * 0.78, s * 0.32),
                new Point(s * 0.68, s * 0.24),
                new Point(s * 0.42, s * 0.52),
                new Point(s * 0.32, s * 0.42)
            }
        });
    }

    private static void BuildWallpaper(Grid root, double s)
    {
        root.Children.Add(new Border
        {
            Width = s * 0.88,
            Height = s * 0.82,
            CornerRadius = new CornerRadius(s * 0.18),
            Background = Grad(56, 189, 248, 37, 99, 235)
        });
        root.Children.Add(new Ellipse
        {
            Width = s * 0.22,
            Height = s * 0.22,
            HorizontalAlignment = HorizontalAlignment.Right,
            VerticalAlignment = VerticalAlignment.Top,
            Margin = new Thickness(0, s * 0.18, s * 0.20, 0),
            Fill = Grad(254, 240, 138, 250, 204, 21)
        });
        root.Children.Add(new Polygon
        {
            Fill = Grad(167, 139, 250, 109, 40, 217),
            Points = new PointCollection
            {
                new Point(s * 0.10, s * 0.86),
                new Point(s * 0.44, s * 0.42),
                new Point(s * 0.88, s * 0.86)
            }
        });
    }

    private static void BuildFont(Grid root, double s)
    {
        root.Children.Add(new Border
        {
            Width = s * 0.82,
            Height = s * 0.82,
            CornerRadius = new CornerRadius(s * 0.20),
            HorizontalAlignment = HorizontalAlignment.Right,
            VerticalAlignment = VerticalAlignment.Bottom,
            Background = Grad(168, 85, 247, 109, 40, 217)
        });
        var plate = new Border
        {
            Width = s * 0.82,
            Height = s * 0.82,
            CornerRadius = new CornerRadius(s * 0.20),
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Top,
            Background = Grad(56, 189, 248, 37, 99, 235)
        };
        plate.Child = new TextBlock
        {
            Text = "Aa",
            FontSize = s * 0.40,
            FontWeight = FontWeights.ExtraBold,
            Foreground = Solid(255, 255, 255),
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };
        root.Children.Add(plate);
    }

    private static void BuildDeviceAudio(Grid root, double s)
    {
        root.Children.Add(new Border
        {
            Width = s * 0.78,
            Height = s * 0.88,
            CornerRadius = new CornerRadius(s * 0.20),
            Background = Grad(71, 85, 105, 30, 41, 59)
        });
        root.Children.Add(new Ellipse
        {
            Width = s * 0.24,
            Height = s * 0.24,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Top,
            Margin = new Thickness(0, s * 0.14, 0, 0),
            Fill = Grad(103, 232, 249, 14, 165, 233)
        });
        root.Children.Add(new Ellipse
        {
            Width = s * 0.42,
            Height = s * 0.42,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Bottom,
            Margin = new Thickness(0, 0, 0, s * 0.12),
            Fill = Grad(56, 189, 248, 37, 99, 235)
        });
    }

    private static void BuildDiagnosticLog(Grid root, double s)
    {
        root.Children.Add(new Border
        {
            Width = s * 0.88,
            Height = s * 0.78,
            CornerRadius = new CornerRadius(s * 0.18),
            Background = Grad(51, 65, 85, 15, 23, 42)
        });
        root.Children.Add(new Border
        {
            Width = s * 0.88,
            Height = s * 0.22,
            CornerRadius = new CornerRadius(s * 0.18, s * 0.18, 0, 0),
            VerticalAlignment = VerticalAlignment.Top,
            Background = Grad(100, 116, 139, 71, 85, 105)
        });
        root.Children.Add(new Border
        {
            Width = s * 0.44,
            Height = s * 0.24,
            CornerRadius = new CornerRadius(s * 0.08),
            HorizontalAlignment = HorizontalAlignment.Right,
            VerticalAlignment = VerticalAlignment.Bottom,
            Margin = new Thickness(0, 0, s * 0.06, s * 0.06),
            Background = Grad(52, 211, 153, 16, 185, 129)
        });
    }

    private static void BuildWindowMin(Grid root, double s)
    {
        root.Children.Add(new Border
        {
            Width = s * 0.72,
            Height = s * 0.18,
            CornerRadius = new CornerRadius(s * 0.09),
            Background = Grad(56, 189, 248, 37, 99, 235)
        });
    }

    private static void BuildWindowMax(Grid root, double s)
    {
        root.Children.Add(new Border
        {
            Width = s * 0.72,
            Height = s * 0.72,
            CornerRadius = new CornerRadius(s * 0.18),
            BorderBrush = Grad(56, 189, 248, 37, 99, 235),
            BorderThickness = new Thickness(2.2),
            Background = Grad(224, 242, 254, 186, 230, 253)
        });
    }

    private static void BuildWindowRestore(Grid root, double s)
    {
        root.Children.Add(new Border
        {
            Width = s * 0.58,
            Height = s * 0.58,
            CornerRadius = new CornerRadius(s * 0.14),
            HorizontalAlignment = HorizontalAlignment.Right,
            VerticalAlignment = VerticalAlignment.Top,
            Margin = new Thickness(0, s * 0.10, s * 0.10, 0),
            Background = Grad(99, 102, 241, 67, 56, 202)
        });
        root.Children.Add(new Border
        {
            Width = s * 0.58,
            Height = s * 0.58,
            CornerRadius = new CornerRadius(s * 0.14),
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Bottom,
            Margin = new Thickness(s * 0.10, 0, 0, s * 0.10),
            Background = Grad(56, 189, 248, 14, 165, 233)
        });
    }

    private static void BuildWindowClose(Grid root, double s)
    {
        root.Children.Add(new Border
        {
            Width = s * 0.76,
            Height = s * 0.76,
            CornerRadius = new CornerRadius(s * 0.22),
            Background = Grad(251, 113, 133, 225, 29, 72)
        });
        root.Children.Add(new Border
        {
            Width = s * 0.42,
            Height = s * 0.10,
            CornerRadius = new CornerRadius(2),
            Background = Solid(255, 255, 255),
            RenderTransformOrigin = new Point(0.5, 0.5),
            RenderTransform = new RotateTransform { Angle = 45 }
        });
        root.Children.Add(new Border
        {
            Width = s * 0.42,
            Height = s * 0.10,
            CornerRadius = new CornerRadius(2),
            Background = Solid(255, 255, 255),
            RenderTransformOrigin = new Point(0.5, 0.5),
            RenderTransform = new RotateTransform { Angle = -45 }
        });
    }

    private static void BuildChevron(Grid root, double s, bool up)
    {
        root.Children.Add(new Border
        {
            Width = s * 0.84,
            Height = s * 0.84,
            CornerRadius = new CornerRadius(s * 0.22),
            Background = Grad(56, 189, 248, 59, 130, 246)
        });
        root.Children.Add(new Polygon
        {
            Fill = Solid(255, 255, 255),
            Points = up
                ? new PointCollection
                {
                    new Point(s * 0.50, s * 0.30),
                    new Point(s * 0.72, s * 0.62),
                    new Point(s * 0.28, s * 0.62)
                }
                : new PointCollection
                {
                    new Point(s * 0.28, s * 0.38),
                    new Point(s * 0.72, s * 0.38),
                    new Point(s * 0.50, s * 0.70)
                }
        });
    }

    private static void BuildEditResize(Grid root, double s)
    {
        // Fluent Blue-Cyan Pen/Sliders tile (Row 5 Col 13 of reference image)
        root.Children.Add(new Border
        {
            Width = s * 0.84,
            Height = s * 0.84,
            CornerRadius = new CornerRadius(s * 0.22),
            Background = Grad(56, 189, 248, 37, 99, 235)
        });
        root.Children.Add(new Border
        {
            Width = s * 0.52,
            Height = s * 0.16,
            CornerRadius = new CornerRadius(s * 0.06),
            Background = Grad(253, 224, 71, 245, 158, 11),
            RenderTransformOrigin = new Point(0.5, 0.5),
            RenderTransform = new RotateTransform { Angle = -45 }
        });
    }

    /// <summary>
    /// Replaces a ButtonBase's content with a colorful Fluent UI icon (and optional label).
    /// </summary>
    public static void SetButtonFluentIcon(ButtonBase? btn, FluentIconKind kind, string? label = null, double iconSize = 18)
    {
        if (btn == null) return;
        var icon = Create(kind, iconSize);
        if (string.IsNullOrWhiteSpace(label))
        {
            btn.Content = icon;
        }
        else
        {
            var sp = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Spacing = 6,
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center
            };
            sp.Children.Add(icon);
            sp.Children.Add(new TextBlock
            {
                Text = label,
                FontSize = iconSize <= 16 ? 11 : 12,
                VerticalAlignment = VerticalAlignment.Center,
                FontWeight = FontWeights.Medium
            });
            btn.Content = sp;
            btn.FontSize = iconSize <= 16 ? 11 : 12;
        }
    }

    public static FluentIconKind ResolvePlayModeKind(string? modeText)
    {
        if (string.IsNullOrWhiteSpace(modeText)) return FluentIconKind.ModeSequential;
        if (modeText.Contains("单曲")) return FluentIconKind.ModeLoopOne;
        if (modeText.Contains("列表") || modeText.Contains("全部")) return FluentIconKind.ModeLoopAll;
        if (modeText.Contains("随机")) return FluentIconKind.ModeShuffle;
        return FluentIconKind.ModeSequential;
    }

    /// <summary>
    /// Recursively upgrades buttons and standalone FontIcons in a visual tree (loaded from XAML/XBF)
    /// to colorful Fluent UI icons. Does NOT touch WebView2 internals (Audition DAW & PowerPoint Theme).
    /// </summary>
    public static void UpgradeButtonsInVisualTree(DependencyObject? root)
    {
        if (root == null) return;
        int count = VisualTreeHelper.GetChildrenCount(root);
        for (int i = 0; i < count; i++)
        {
            var child = VisualTreeHelper.GetChild(root, i);
            if (child is WebView2) continue;

            if (child is ButtonBase btn && child is not HyperlinkButton)
            {
                TryUpgradeButton(btn);
            }
            else if (child is Panel panel)
            {
                TryUpgradeStandalonePanelIcons(panel);
                UpgradeButtonsInVisualTree(child);
            }
            else
            {
                UpgradeButtonsInVisualTree(child);
            }
        }
    }

    private static void TryUpgradeStandalonePanelIcons(Panel panel)
    {
        for (int i = 0; i < panel.Children.Count; i++)
        {
            if (panel.Children[i] is FontIcon fi && fi.Tag as string != "__fluent_upgraded")
            {
                FluentIconKind? kind = MapGlyphToFluentKind(fi.Glyph);
                if (kind.HasValue)
                {
                    double size = fi.FontSize > 0 ? Math.Max(18, fi.FontSize + 4) : 18;
                    var replacement = Create(kind.Value, size);
                    replacement.Margin = fi.Margin;
                    replacement.HorizontalAlignment = fi.HorizontalAlignment;
                    replacement.VerticalAlignment = fi.VerticalAlignment;
                    if (panel is Grid)
                    {
                        Grid.SetRow(replacement, Grid.GetRow(fi));
                        Grid.SetColumn(replacement, Grid.GetColumn(fi));
                        Grid.SetRowSpan(replacement, Grid.GetRowSpan(fi));
                        Grid.SetColumnSpan(replacement, Grid.GetColumnSpan(fi));
                    }
                    panel.Children[i] = replacement;
                }
            }
        }
    }

    private static void TryUpgradeButton(ButtonBase btn)
    {
        if (btn.Tag is string tagStr && tagStr == "__fluent_upgraded") return;

        string? text = ExtractButtonText(btn.Content);
        string? glyph = ExtractFontIconGlyph(btn.Content);
        string? tooltip = ToolTipService.GetToolTip(btn) as string;

        if (tooltip != null && (tooltip.Contains("播放模式") || tooltip.Contains("循环模式")))
        {
            string modeLabel = string.IsNullOrWhiteSpace(text) ? "顺序播放" : text;
            SetButtonFluentIcon(btn, ResolvePlayModeKind(modeLabel), modeLabel, 18);
            return;
        }

        FluentIconKind? matchedKind = MapButtonToFluentKind(btn.Name, text, glyph, tooltip);
        if (matchedKind.HasValue)
        {
            bool isBigTransport = string.IsNullOrWhiteSpace(text) && (btn.Width >= 44 || btn.Height >= 44 || (tooltip == "播放" || tooltip == "暂停") && btn.Parent is StackPanel sp && sp.Spacing >= 10);
            double size = isBigTransport ? 42 : (string.IsNullOrWhiteSpace(text) ? 20 : 18);
            if (isBigTransport)
            {
                btn.Padding = new Thickness(0);
                btn.Background = new SolidColorBrush(Colors.Transparent);
                btn.BorderThickness = new Thickness(0);
            }
            SetButtonFluentIcon(btn, matchedKind.Value, text, size);
            if (btn.Tag == null)
            {
                btn.Tag = "__fluent_upgraded";
            }
        }
    }

    private static string? ExtractButtonText(object? content)
    {
        if (content is string s) return s;
        if (content is TextBlock tb) return tb.Text;
        if (content is Panel panel)
        {
            foreach (var child in panel.Children)
            {
                if (child is TextBlock ctb && !string.IsNullOrWhiteSpace(ctb.Text))
                    return ctb.Text;
            }
        }
        return null;
    }

    private static string? ExtractFontIconGlyph(object? content)
    {
        if (content is FontIcon fi) return fi.Glyph;
        if (content is Panel panel)
        {
            foreach (var child in panel.Children)
            {
                if (child is FontIcon cfi) return cfi.Glyph;
            }
        }
        return null;
    }

    private static FluentIconKind? MapGlyphToFluentKind(string? glyph)
    {
        if (string.IsNullOrEmpty(glyph)) return null;
        return glyph switch
        {
            "\uE768" => FluentIconKind.Play,
            "\uE769" => FluentIconKind.Pause,
            "\uE892" => FluentIconKind.Previous,
            "\uE893" => FluentIconKind.Next,
            "\uE71A" => FluentIconKind.Stop,
            "\uE767" => FluentIconKind.Volume,
            "\uE823" or "\uE8FD" => FluentIconKind.RecentList,
            "\uE995" or "\uE9E9" => FluentIconKind.Eq,
            "\uE950" or "\uE74C" => FluentIconKind.Spatial,
            "\uE9D9" or "\uE994" or "\uE895" => FluentIconKind.Dynamics,
            "\uE774" or "\uE8B8" => FluentIconKind.HiRes,
            "\uE71D" => FluentIconKind.VstPlugin,
            "\uE8B7" or "\uE8DA" => FluentIconKind.FolderScan,
            "\uE8E5" or "\uE710" => FluentIconKind.FileAdd,
            "\uE809" or "\uF0E2" => FluentIconKind.AlbumGrid,
            "\uE721" or "\uE91B" => FluentIconKind.Search,
            "\uE711" => FluentIconKind.ClearFilter,
            "\uE74D" => FluentIconKind.Delete,
            "\uE72C" or "\uE7A7" or "\uE7A6" => FluentIconKind.RefreshReset,
            "\uE74E" => FluentIconKind.Save,
            "\uE70F" => FluentIconKind.EditResize,
            "\uE70E" => FluentIconKind.ChevronUp,
            "\uE70D" => FluentIconKind.ChevronDown,
            "\uE8D6" => FluentIconKind.AppLogo,
            "\uE80F" => FluentIconKind.Home,
            "\uE713" => FluentIconKind.Settings,
            "\uE921" => FluentIconKind.WindowMin,
            "\uE922" => FluentIconKind.WindowMax,
            "\uE923" => FluentIconKind.WindowRestore,
            "\uE8BB" => FluentIconKind.WindowClose,
            _ => null
        };
    }

    private static FluentIconKind? MapButtonToFluentKind(string? name, string? text, string? glyph, string? tooltip)
    {
        if (!string.IsNullOrEmpty(name))
        {
            switch (name)
            {
                case "MinButton": return FluentIconKind.WindowMin;
                case "MaxButton": return FluentIconKind.WindowMax;
                case "CloseButton": return FluentIconKind.WindowClose;
                case "RecentButton": return FluentIconKind.RecentList;
                case "PreviewPlayButton": return FluentIconKind.Play;
                case "LyricFontButton": return FluentIconKind.Font;
            }
        }

        var byGlyph = MapGlyphToFluentKind(glyph);
        if (byGlyph.HasValue) return byGlyph.Value;

        string combined = $"{text} {tooltip}".Trim();
        if (!string.IsNullOrWhiteSpace(combined))
        {
            if (combined.Contains("上一曲")) return FluentIconKind.Previous;
            if (combined.Contains("下一曲")) return FluentIconKind.Next;
            if (combined.Contains("暂停")) return FluentIconKind.Pause;
            if (combined.Contains("停止")) return FluentIconKind.Stop;
            if (combined.Contains("最近播放")) return FluentIconKind.RecentList;
            if (combined.Contains("调整面板大小")) return FluentIconKind.EditResize;
            if (combined.Contains("扫描") || combined.Contains("添加文件夹") || combined.Contains("添加音乐文件夹") || combined.Contains("添加目录") || combined.Contains("浏览")) return FluentIconKind.FolderScan;
            if (combined.Contains("添加文件") || combined.Contains("打开") || combined.Contains("选择文件") || combined.Contains("载入") || combined.Contains("导入音频")) return FluentIconKind.FileAdd;
            if (combined.Contains("专辑") || combined.Contains("网格") || combined.Contains("列表视图")) return FluentIconKind.AlbumGrid;
            if (combined.Contains("清除") || combined.Contains("清空")) return FluentIconKind.ClearFilter;
            if (combined.Contains("移除") || combined.Contains("卸载") || combined.Contains("删除") || combined.Contains("剪辑")) return FluentIconKind.Delete;
            if (combined.Contains("歌词")) return FluentIconKind.LyricsImport;
            if (combined.Contains("重置") || combined.Contains("刷新") || combined.Contains("恢复") || combined.Contains("撤销") || combined.Contains("重做")) return FluentIconKind.RefreshReset;
            if (combined.Contains("保存") || combined.Contains("导出") || combined.Contains("标准化")) return FluentIconKind.Save;
            if (combined.Contains("图片") || combined.Contains("壁纸") || combined.Contains("背景") || combined.Contains("封面")) return FluentIconKind.Wallpaper;
            if (combined.Contains("字体")) return FluentIconKind.Font;
            if (combined.Contains("设备") || combined.Contains("声卡")) return FluentIconKind.DeviceAudio;
            if (combined.Contains("日志") || combined.Contains("崩溃")) return FluentIconKind.DiagnosticLog;
            if (combined.Contains("插件") || combined.Contains("VST") || combined.Contains("界面")) return FluentIconKind.VstPlugin;
            if (combined.Contains("预设") || combined.Contains("AutoEq") || combined.Contains("EQ") || combined.Contains("DSP") || combined.Contains("淡入")) return FluentIconKind.Eq;
            if (combined.Contains("播放") || combined.Contains("预览")) return FluentIconKind.Play;
        }

        return null;
    }
}
