//
// DeviceDNA
// Author:  nobody174 (nobodylearn174@gmail.com)
// Repo:    https://github.com/nobody174/DeviceDNA
// Patreon: https://www.patreon.com/c/Nobody174
// License: All rights reserved (c) 2026 nobody174
// "It's never too late to give up!"
//

namespace DeviceDNA.Model.Dnas;

public class CpuBasic
{
    public required string Name { get; init; }
    public required string Manufacturer { get; init; }
    public int Cores { get; init; }
    public int Threads { get; init; }

    // Null when WMI's MaxClockSpeed is unavailable — previously fabricated as 0 GHz, which would
    // display as a specific, wrong-looking value rather than an honest "Unknown".
    public double? BaseClockGhz { get; init; }

    // The vendor's general processor product page (AMD/Intel) — not per-model, since neither vendor
    // exposes a stable per-CPU-model URL the way GPU driver pages do. Null when the manufacturer
    // isn't recognized. Same pattern as MotherboardBasic.VendorSupportUrl.
    public string? VendorSupportUrl { get; init; }

    // CPU-Z Validator (valid.x86.fr) search for this exact CPU model, sorted by highest recorded
    // frequency — the user found and personally verified this link works via CPU-Z's own UI, and
    // decoded its "psn" parameter as plain ASCII-hex of the full CPU name string (confirmed by
    // reversing the user's own example URL: hex-decoding it yields exactly
    // "AMD Ryzen 7 5800X 8-Core Processor", matching WMI's Win32_Processor.Name for that CPU
    // byte-for-byte). A real community benchmark database, distinct from VendorSupportUrl.
    public string? BenchmarkUrl { get; init; }
}

public class CpuCache
{
    public int? L1Kb { get; init; }
    public int? L2Kb { get; init; }
    public int? L3Kb { get; init; }
}

public class CpuAdvanced
{
    public string? ArchitectureGeneration { get; init; }
    public string? Socket { get; init; }
    public double? BoostClockGhz { get; init; }
    public CpuCache? Cache { get; init; }
    public double? TdpWatts { get; init; }
    public double? CurrentTempC { get; init; }
    public double? CurrentUtilizationPct { get; init; }
    public double? CurrentLiveClockGhz { get; init; }
    public IReadOnlyList<double>? PerCoreLoadPct { get; init; }
    public bool? VirtualizationSupport { get; init; }
    public string? PowerMode { get; init; }
}

//*Built with assistance from __Claude Code__ by Anthropic.*
