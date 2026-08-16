//
// DeviceDNA
// Author:  nobody174 (nobodylearn174@gmail.com)
// Repo:    https://github.com/nobody174/DeviceDNA
// Patreon: https://www.patreon.com/c/Nobody174
// License: All rights reserved (c) 2026 nobody174
// "It's never too late to give up!"
//

using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using DeviceDNA.UI.Presentation;

namespace DeviceDNA.UI;

// Orbital DNA dashboard (BACKLOG.md "DNA-helix / orbital visual redesign"): a central "This
// Computer" hub with one orbit node per DNA component, arranged on a circle. WPF has no built-in
// panel for circular layout, so node positions are computed here in code-behind and applied as
// Canvas.Left/Top on each generated container — kept out of DnaTileViewModel since pixel layout is
// a view concern, not something the ViewModel should know about.
//
// Clicking an orbit node sets SelectedTile, which the XAML binds to (via RelativeSource
// AncestorType=UserControl) to swap the orbit Canvas for a detail panel showing every field for
// that DNA (Basic/Advanced in two labeled sections, no tier toggle — design decision).
// SelectedTile/HasSelectedTile are DependencyProperty, not plain CLR properties, so the XAML
// bindings actually receive change notifications when a node is clicked. AnimateSelectionChange
// drives the transition itself (opacity/scale/translate on both panels) — both stay in the visual
// tree at all times rather than Visibility-toggled, since a Collapsed element can't animate.
public partial class DashboardView : UserControl
{
    private const double BaseNodeDiameter = 108;
    private const double MinNodeDiameter = 72;
    private const double HubDiameter = 152;

    public static readonly DependencyProperty SelectedTileProperty = DependencyProperty.Register(
        nameof(SelectedTile), typeof(DnaTileViewModel), typeof(DashboardView),
        new PropertyMetadata(null, OnSelectedTileChanged));

    public static readonly DependencyProperty HasSelectedTileProperty = DependencyProperty.Register(
        nameof(HasSelectedTile), typeof(bool), typeof(DashboardView), new PropertyMetadata(false));

    public DnaTileViewModel? SelectedTile
    {
        get => (DnaTileViewModel?)GetValue(SelectedTileProperty);
        set => SetValue(SelectedTileProperty, value);
    }

    public bool HasSelectedTile
    {
        get => (bool)GetValue(HasSelectedTileProperty);
        private set => SetValue(HasSelectedTileProperty, value);
    }

    private static void OnSelectedTileChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var view = (DashboardView)d;
        view.HasSelectedTile = e.NewValue != null;
        view.AnimateSelectionChange(e.NewValue != null);
    }

    // Animates the swap between the orbit overview and the detail panel — both stay in the visual
    // tree throughout (see the XAML comment on OrbitCanvas) so opacity/transform can animate freely
    // instead of an instant Visibility snap. Mirrors the approved HTML mockup's transition: the
    // orbit shrinks toward a corner and fades slightly rather than vanishing outright, and the
    // detail panel fades/slides in from the right — never a jarring hard cut either direction.
    private void AnimateSelectionChange(bool expanding)
    {
        const int durationMs = 450;
        var ease = new CubicEase { EasingMode = EasingMode.EaseInOut };

        var orbitScale = expanding ? 0.34 : 1.0;
        var orbitOpacity = expanding ? 0.35 : 1.0;
        // Shrink toward the upper-left, matching the mockup's collapsed-constellation corner
        // placement — computed as a fraction of the current size so it holds up at any window size.
        var orbitTranslateX = expanding ? -ActualWidth * 0.28 : 0;
        var orbitTranslateY = expanding ? -ActualHeight * 0.24 : 0;

        AnimateDouble(OrbitScaleTransform, ScaleTransform.ScaleXProperty, orbitScale, durationMs, ease);
        AnimateDouble(OrbitScaleTransform, ScaleTransform.ScaleYProperty, orbitScale, durationMs, ease);
        AnimateDouble(OrbitTranslateTransform, TranslateTransform.XProperty, orbitTranslateX, durationMs, ease);
        AnimateDouble(OrbitTranslateTransform, TranslateTransform.YProperty, orbitTranslateY, durationMs, ease);
        AnimateDouble(OrbitCanvas, UIElement.OpacityProperty, orbitOpacity, durationMs, ease);
        OrbitCanvas.IsHitTestVisible = !expanding;

        var detailOpacity = expanding ? 1.0 : 0.0;
        var detailTranslateX = expanding ? 0.0 : 24.0;
        AnimateDouble(DetailPanel, UIElement.OpacityProperty, detailOpacity, durationMs, ease);
        AnimateDouble(DetailTranslateTransform, TranslateTransform.XProperty, detailTranslateX, durationMs, ease);
        DetailPanel.IsHitTestVisible = expanding;
    }

    private static void AnimateDouble(DependencyObject target, DependencyProperty property, double to, int durationMs, IEasingFunction ease)
    {
        var animation = new DoubleAnimation
        {
            To = to,
            Duration = new Duration(TimeSpan.FromMilliseconds(durationMs)),
            EasingFunction = ease,
        };
        var storyboard = new Storyboard();
        Storyboard.SetTarget(animation, target);
        Storyboard.SetTargetProperty(animation, new PropertyPath(property));
        storyboard.Children.Add(animation);
        storyboard.Begin();
    }

    public DashboardView()
    {
        InitializeComponent();
        SizeChanged += (_, _) => LayoutOrbit();
        OrbitNodesControl.ItemContainerGenerator.StatusChanged += (_, _) => LayoutOrbit();
        DataContextChanged += OnDataContextChanged;
    }

    // A Rescan rebuilds MainViewModel.Tiles as an entirely new list of DnaTileViewModel instances
    // (see MainViewModel.RunScanAsync) — without this, a detail view left open across a Rescan would
    // keep showing the previous scan's now-orphaned tile with no visible indication it's stale.
    // SelectedTile lives on this View (not bound from the ViewModel), so it has to be reset here
    // explicitly in response to the ViewModel's own Tiles change notification, rather than via a
    // normal XAML binding.
    private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (e.OldValue is INotifyPropertyChanged oldVm)
        {
            oldVm.PropertyChanged -= OnViewModelPropertyChanged;
        }
        if (e.NewValue is INotifyPropertyChanged newVm)
        {
            newVm.PropertyChanged += OnViewModelPropertyChanged;
        }
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(MainViewModel.Tiles))
        {
            SelectedTile = null;
        }
    }

    // Clicking an orbit node's Border selects it for the detail view. The click originates on the
    // Border inside the DataTemplate, whose DataContext is the DnaTileViewModel for that node.
    private void OrbitNode_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (sender is FrameworkElement { DataContext: DnaTileViewModel tile })
        {
            SelectedTile = tile;
        }
    }

    // Clicking the detail view's DNA name opens its vendor page, when one is available — re-added
    // after being dropped in the orbital detail view rebuild (regression, user feedback,
    // 2026-08-16). The TextBlock's own DataContext here is MainViewModel (via RelativeSource in
    // XAML), not the DnaTileViewModel directly, so this reaches SelectedTile explicitly rather than
    // casting sender's DataContext the way the orbit node click handler above does.
    private void DetailNameText_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (SelectedTile?.HasVendorUrl == true)
        {
            SelectedTile.OpenVendorUrlCommand.Execute(null);
            e.Handled = true;
        }
    }

    private void BackToOverview_Click(object sender, RoutedEventArgs e)
    {
        SelectedTile = null;
    }

    private void LayoutOrbit()
    {
        if (ActualWidth <= 0 || ActualHeight <= 0)
        {
            return;
        }

        var centerX = ActualWidth / 2;
        var centerY = ActualHeight / 2;

        Canvas.SetLeft(HubBorder, centerX - HubDiameter / 2);
        Canvas.SetTop(HubBorder, centerY - HubDiameter / 2);

        var tiles = (IReadOnlyList<DnaTileViewModel>)OrbitNodesControl.ItemsSource;
        if (tiles == null || tiles.Count == 0)
        {
            return;
        }

        // Fixed circular orbit for the standard 7 v1-scoped DNA types (CPU, GPU, Memory, Storage,
        // Motherboard, Network, OS — every computer has these) at their original, already-approved
        // size and spacing. Only if a real scan ever finds MORE than 7 (multiple GPUs, multiple
        // disks each as their own node) does the layout adapt: node size shrinks modestly and the
        // orbit radius grows to fit the extras, rather than changing anything about the normal case
        // (user correction, 2026-08-16 — an earlier version wrongly made the orbit elliptical and
        // shrink-capable for the standard case too).
        var nodeDiameter = tiles.Count <= 7 ? BaseNodeDiameter : Math.Max(MinNodeDiameter, BaseNodeDiameter - (tiles.Count - 7) * 6);
        var baseRadius = 230.0;
        var radius = tiles.Count <= 7 ? baseRadius : Math.Min(Math.Min(centerX, centerY) - nodeDiameter / 2 - 24, baseRadius + (tiles.Count - 7) * 20);

        StrandCanvas.Children.Clear();
        // Single consistent strand color (teal), not alternating gold/teal per connector — the
        // two-tone version risked reading as a status signal to users, which it never was
        // (user feedback, 2026-08-16: "I think all should have the same wire color").
        var strandBrush = (Brush)FindResource("StrandAccentBrush");

        for (var i = 0; i < tiles.Count; i++)
        {
            // Start at the top (-90°) and go clockwise, matching the design mockup's node ordering.
            var angle = (i / (double)tiles.Count) * 2 * Math.PI - Math.PI / 2;
            var nodeCenterX = centerX + radius * Math.Cos(angle);
            var nodeCenterY = centerY + radius * Math.Sin(angle);
            var x = nodeCenterX - nodeDiameter / 2;
            var y = nodeCenterY - nodeDiameter / 2;

            if (OrbitNodesControl.ItemContainerGenerator.ContainerFromItem(tiles[i]) is ContentPresenter container)
            {
                Canvas.SetLeft(container, x);
                Canvas.SetTop(container, y);
                container.Width = nodeDiameter;
                container.Height = nodeDiameter;
            }

            var bowSign = i % 2 == 0 ? 1 : -1;
            DrawStrand(centerX, centerY, nodeCenterX, nodeCenterY, strandBrush, bowSign);
        }
    }

    // Curved connector from the hub to one orbit node — a quadratic Bezier bowed perpendicular to
    // the straight hub->node line, alternating bow direction per node so adjacent strands don't
    // overlap, echoing a DNA helix's cross-tie look without literal double-helix geometry (which
    // would fight the click-to-expand interaction — design decision from the HTML mockup pass).
    private void DrawStrand(double hubX, double hubY, double nodeX, double nodeY, Brush brush, int bowSign)
    {
        var midX = (hubX + nodeX) / 2;
        var midY = (hubY + nodeY) / 2;

        var dx = nodeX - hubX;
        var dy = nodeY - hubY;
        var length = Math.Sqrt(dx * dx + dy * dy);
        var perpX = length > 0 ? -dy / length : 0;
        var perpY = length > 0 ? dx / length : 0;
        const double bow = 18;

        var controlX = midX + perpX * bow * bowSign;
        var controlY = midY + perpY * bow * bowSign;

        var figure = new PathFigure { StartPoint = new Point(hubX, hubY) };
        figure.Segments.Add(new QuadraticBezierSegment(new Point(controlX, controlY), new Point(nodeX, nodeY), isStroked: true));
        var geometry = new PathGeometry();
        geometry.Figures.Add(figure);

        var path = new System.Windows.Shapes.Path
        {
            Data = geometry,
            Stroke = brush,
            StrokeThickness = 1.4,
            Opacity = 0.55,
        };
        StrandCanvas.Children.Add(path);
    }
}

//*Built with assistance from __Claude Code__ by Anthropic.*
