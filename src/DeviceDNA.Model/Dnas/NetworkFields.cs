//
// DeviceDNA
// Author:  nobody174 (nobodylearn174@gmail.com)
// Repo:    https://github.com/nobody174/DeviceDNA
// Patreon: https://www.patreon.com/c/Nobody174
// License: All rights reserved (c) 2026 nobody174
// "It's never too late to give up!"
//

namespace DeviceDNA.Model.Dnas;

public class NetworkBasic
{
    public required string AdapterName { get; init; }
    public required string ConnectionType { get; init; } // Wired, WiFi
    public double? CurrentSpeedMbps { get; init; }

    // For an integrated adapter (the common desktop case, e.g. "Realtek PCIe GbE Family
    // Controller"), reuses the motherboard's own vendor URL — research confirmed the chipset
    // maker's own site (e.g. Realtek's) has no stable per-model URL and its generic driver is
    // usually inferior to the motherboard vendor's customized package for that exact board.
    // Falls back to the chipset maker's general downloads page only when no motherboard match
    // exists (e.g. a discrete/USB NIC). Null when neither is available.
    public string? VendorSupportUrl { get; init; }
}

public class NetworkAdvanced
{
    public string? IpAddress { get; init; }
    public string? MacAddress { get; init; }
    public string? DriverVersion { get; init; }
    public int? SignalStrengthPct { get; init; } // WiFi only
    public double? MaxSupportedSpeedMbps { get; init; }
}

//*Built with assistance from __Claude Code__ by Anthropic.*
