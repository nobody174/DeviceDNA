//
// DeviceDNA
// Author:  nobody174 (nobodylearn174@gmail.com)
// Repo:    https://github.com/nobody174/DeviceDNA
// Patreon: https://www.patreon.com/c/Nobody174
// License: All rights reserved (c) 2026 nobody174
// "It's never too late to give up!"
//

using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using DeviceDNA.Model;

namespace DeviceDNA.UI.Presentation;

// Standard bool -> Visibility converter, with an optional "Invert" parameter for the collapsed/expanded
// tile toggle (e.g. showing the compact tile face when NOT expanded).
public class BoolToVisibilityConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var boolValue = value is bool b && b;
        var invert = string.Equals(parameter as string, "Invert", StringComparison.OrdinalIgnoreCase);
        if (invert)
        {
            boolValue = !boolValue;
        }

        return boolValue ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}

// Converts a string to Visibility: Visible when non-null/non-empty, Collapsed otherwise. Distinct
// from BoolToVisibilityConverter (which only matches actual bool values — a string bound to it is
// never a bool, so it always evaluates false/Collapsed, silently hiding content bound this way).
public class StringToVisibilityConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        !string.IsNullOrWhiteSpace(value as string) ? Visibility.Visible : Visibility.Collapsed;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}

// Shows a hand cursor when true (e.g. a DNA name that's clickable to a vendor page), the normal
// arrow otherwise — a plain visual affordance for "this text is a link."
public class BoolToCursorConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is true ? Cursors.Hand : Cursors.Arrow;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}

// Button label for the "Check Windows Update" action: "Checking..." while the live COM call is in
// flight, "Check Windows Update" otherwise.
public class CheckingLabelConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is true ? "Checking..." : "Check Windows Update";

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}

// Top-bar Rescan button label: "Scanning..." while the WMI/sensor scan is in flight, "Rescan"
// otherwise. A scan running synchronously on the UI thread previously made this button (and the
// whole window) look frozen with no feedback — user feedback; see MainViewModel.RunScanAsync.
public class RescanLabelConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is true ? "Scanning..." : "Rescan";

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}

// Maps a DnaType to its orbital-dashboard icon Geometry resource (see App.xaml's
// IconGeometry.* keys). Application.Current.Resources is used directly rather than a
// FindResource call on the bound element, since converters have no element context of their own.
public class DnaTypeToIconGeometryConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var key = value switch
        {
            DnaType.Cpu => "IconGeometry.Cpu",
            DnaType.Gpu => "IconGeometry.Gpu",
            DnaType.Memory => "IconGeometry.Memory",
            DnaType.Storage => "IconGeometry.Storage",
            DnaType.Motherboard => "IconGeometry.Motherboard",
            DnaType.Network => "IconGeometry.Network",
            DnaType.Os => "IconGeometry.Os",
            _ => null,
        };

        return key != null ? System.Windows.Application.Current.Resources[key] as Geometry : null;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}

// True only for DnaType.Os — used to switch the icon's rendering mode between stroke-based
// (Fill=Transparent, Stroke=icon color, matches the other six categories' outline style) and
// fill-based (the Windows-logo icon is solid quadrants, not an outline).
public class DnaTypeIsOsConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is DnaType.Os;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}

//*Built with assistance from __Claude Code__ by Anthropic.*
