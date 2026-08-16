//
// DeviceDNA
// Author:  nobody174 (nobodylearn174@gmail.com)
// Repo:    https://github.com/nobody174/DeviceDNA
// Patreon: https://www.patreon.com/c/Nobody174
// License: All rights reserved (c) 2026 nobody174
// "It's never too late to give up!"
//

namespace DeviceDNA.Model.Dnas;

public class GpuBasic
{
    public required string Name { get; init; }
    public required string Manufacturer { get; init; }
    // Null when WMI's AdapterRAM is unavailable — previously fabricated as 0 GB.
    public double? VramAmountGb { get; init; }
    public string? DriverVersion { get; init; }
}

public class GpuAdvanced
{
    public double? CoreClockMhz { get; init; }
    public double? BoostClockMhz { get; init; }
    public string? MemoryType { get; init; }
    public double? MemoryClockMhz { get; init; }
    public int? PcieGeneration { get; init; }
    public int? PcieLaneWidth { get; init; }
    public double? CurrentTempC { get; init; }
    public double? CurrentUtilizationPct { get; init; }
    public double? CurrentVramUsageGb { get; init; }
    public string? DriverDate { get; init; }
    public int? ConnectedOutputsActive { get; init; }
}

//*Built with assistance from __Claude Code__ by Anthropic.*
