//
// DeviceDNA
// Author:  nobody174 (nobodylearn174@gmail.com)
// Repo:    https://github.com/nobody174/DeviceDNA
// Patreon: https://www.patreon.com/c/Nobody174
// License: All rights reserved (c) 2026 nobody174
// "It's never too late to give up!"
//

using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace DeviceDNA.UI;

// Draws and animates the vertical DNA helix shown on the orbital dashboard's detail-panel left
// column (see HelixVisual.xaml for the full design rationale). Geometry is parametric (computed
// from sine curves every render), not static path data, so the same drawing code both lays out the
// helix and drives its spin animation — a phase offset that increases over time via a WPF
// DoubleAnimation, the standard 2D technique for a convincing rotating-helix look.
public partial class HelixVisual : UserControl
{
    // Rainbow gradient stops matching Logo.png's helix palette (red -> orange -> yellow -> green ->
    // teal -> blue -> purple), sampled per-strand-segment by vertical position so the color band
    // reads the same way top-to-bottom as it does across the logo's horizontal version.
    private static readonly Color[] GradientColors =
    {
        Color.FromRgb(0xE0, 0x4C, 0x4C), // red
        Color.FromRgb(0xE0, 0x8C, 0x3C), // orange
        Color.FromRgb(0xE0, 0xC4, 0x3C), // yellow
        Color.FromRgb(0x6C, 0xC4, 0x5C), // green
        Color.FromRgb(0x4C, 0xB8, 0xB0), // teal
        Color.FromRgb(0x4C, 0x8C, 0xE0), // blue
        Color.FromRgb(0x9C, 0x5C, 0xD4), // purple
    };

    private const double TurnsVisible = 3.5; // how many full twists fit top-to-bottom
    private const double RungSpacingFraction = 1.0 / 14.0; // rungs per full turn's height-fraction

    private double _phase;

    public HelixVisual()
    {
        InitializeComponent();

        // A DoubleAnimation/Storyboard would need a custom DependencyProperty for code-behind to
        // read back each tick's value; simpler and just as smooth for this purpose is a per-frame
        // CompositionTarget.Rendering callback advancing the phase directly against wall-clock delta.
        CompositionTarget.Rendering += OnRendering;
        Unloaded += (_, _) => CompositionTarget.Rendering -= OnRendering;
    }

    private DateTime _lastFrameTime = DateTime.Now;

    private void OnRendering(object? sender, EventArgs e)
    {
        var now = DateTime.Now;
        var delta = (now - _lastFrameTime).TotalSeconds;
        _lastFrameTime = now;

        // Full rotation every 6 seconds — slow and ambient, matching how DNA helix animations read
        // in medical/film references (user's stated reference point), not a fast spin.
        _phase += delta * (Math.PI * 2 / 6.0);
        if (_phase > Math.PI * 2)
        {
            _phase -= Math.PI * 2;
        }

        Draw();
    }

    private void RootGrid_SizeChanged(object sender, SizeChangedEventArgs e) => Draw();

    private void Draw()
    {
        var width = RootGrid.ActualWidth;
        var height = RootGrid.ActualHeight;
        if (width <= 0 || height <= 0)
        {
            return;
        }

        HelixCanvas.Children.Clear();

        var centerX = width / 2;
        var amplitude = Math.Min(width / 2 - 8, 42);
        var totalAngle = Math.PI * 2 * TurnsVisible;

        const int samples = 220;
        var strandAPoints = new Point[samples + 1];
        var strandBPoints = new Point[samples + 1];

        for (var i = 0; i <= samples; i++)
        {
            var t = i / (double)samples;
            var y = t * height;
            var angle = t * totalAngle + _phase;
            strandAPoints[i] = new Point(centerX + amplitude * Math.Sin(angle), y);
            strandBPoints[i] = new Point(centerX + amplitude * Math.Sin(angle + Math.PI), y);
        }

        // Rungs: horizontal connector lines between the two strands, drawn only near each
        // strand-crossing point (where the ladder reads clearly), spaced by turn fraction.
        var rungCount = (int)(TurnsVisible / RungSpacingFraction);
        for (var r = 0; r <= rungCount; r++)
        {
            var t = r / (double)rungCount;
            var index = (int)(t * samples);
            if (index >= samples)
            {
                index = samples - 1;
            }

            var a = strandAPoints[index];
            var b = strandBPoints[index];
            // Only draw a rung where the strands are reasonably separated (near a crossing they'd
            // be a degenerate zero-length line) — skip near-zero-width rungs.
            if (Math.Abs(a.X - b.X) > amplitude * 0.35)
            {
                var rung = new Line
                {
                    X1 = a.X,
                    Y1 = a.Y,
                    X2 = b.X,
                    Y2 = b.Y,
                    Stroke = ColorAt(t),
                    StrokeThickness = 1.2,
                    Opacity = 0.55,
                };
                HelixCanvas.Children.Add(rung);
            }
        }

        DrawStrand(strandAPoints, samples);
        DrawStrand(strandBPoints, samples);

        // A handful of small circuit-node dots along one strand, echoing the logo's detail —
        // positioned at a subset of sample points so they ride the strand's motion each frame.
        for (var n = 1; n <= 6; n++)
        {
            var t = n / 7.0;
            var index = (int)(t * samples);
            var p = strandAPoints[index];
            var dot = new Ellipse
            {
                Width = 5,
                Height = 5,
                Fill = ColorAt(t),
                Opacity = 0.9,
            };
            Canvas.SetLeft(dot, p.X - 2.5);
            Canvas.SetTop(dot, p.Y - 2.5);
            HelixCanvas.Children.Add(dot);
        }
    }

    private void DrawStrand(Point[] points, int samples)
    {
        // Drawn as short colored segments rather than one Path with a single Brush, so the
        // gradient reads correctly along the strand's actual curved length top-to-bottom.
        for (var i = 0; i < samples; i++)
        {
            var t = i / (double)samples;
            var segment = new Line
            {
                X1 = points[i].X,
                Y1 = points[i].Y,
                X2 = points[i + 1].X,
                Y2 = points[i + 1].Y,
                Stroke = ColorAt(t),
                StrokeThickness = 2.4,
                StrokeStartLineCap = PenLineCap.Round,
                StrokeEndLineCap = PenLineCap.Round,
            };
            HelixCanvas.Children.Add(segment);
        }
    }

    private static SolidColorBrush ColorAt(double t)
    {
        t = Math.Clamp(t, 0, 1);
        var scaled = t * (GradientColors.Length - 1);
        var index = (int)scaled;
        if (index >= GradientColors.Length - 1)
        {
            return new SolidColorBrush(GradientColors[^1]);
        }

        var localT = scaled - index;
        var a = GradientColors[index];
        var b = GradientColors[index + 1];
        var blended = Color.FromRgb(
            (byte)(a.R + (b.R - a.R) * localT),
            (byte)(a.G + (b.G - a.G) * localT),
            (byte)(a.B + (b.B - a.B) * localT));
        return new SolidColorBrush(blended);
    }
}

//*Built with assistance from __Claude Code__ by Anthropic.*
