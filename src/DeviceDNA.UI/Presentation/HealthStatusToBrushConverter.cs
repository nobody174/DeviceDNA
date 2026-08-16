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
using System.Windows.Media;
using DeviceDNA.Model;

namespace DeviceDNA.UI.Presentation;

// Converts a HealthStatus enum value directly into the corresponding status brush resource.
// Status colors (green/yellow/red) are reserved strictly for health indicators, never decoration
// — see REQUIREMENTS.md section 9.
public class HealthStatusToBrushConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var key = value switch
        {
            HealthStatus.Green => "StatusGreenBrush",
            HealthStatus.Yellow => "StatusYellowBrush",
            HealthStatus.Red => "StatusRedBrush",
            _ => "StatusGreenBrush",
        };

        return System.Windows.Application.Current.TryFindResource(key) as Brush ?? Brushes.Gray;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}

//*Built with assistance from __Claude Code__ by Anthropic.*
