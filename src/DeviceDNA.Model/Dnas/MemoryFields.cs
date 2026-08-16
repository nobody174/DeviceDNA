//
// DeviceDNA
// Author:  nobody174 (nobodylearn174@gmail.com)
// Repo:    https://github.com/nobody174/DeviceDNA
// Patreon: https://www.patreon.com/c/Nobody174
// License: All rights reserved (c) 2026 nobody174
// "It's never too late to give up!"
//

namespace DeviceDNA.Model.Dnas;

public class MemoryBasic
{
    // Null when one or more modules' real capacity is unknown (see MemoryModule.SizeGb) — a sum
    // that silently treated a missing module as 0 GB would understate the real total as if it were
    // confirmed, rather than honestly reflecting that the total is not fully known.
    public double? TotalCapacityGb { get; init; }
    public required string Type { get; init; } // DDR4, DDR5
    public int SpeedMts { get; init; }
    public int SlotsUsed { get; init; }

    // Null when WMI's Win32_PhysicalMemoryArray.MemoryDevices is unavailable — previously silently
    // collapsed to SlotsUsed (reporting "no empty slots" as if confirmed, when it was actually just
    // unknown). See BACKLOG.md history.
    public int? SlotsTotal { get; init; }

    // General manufacturer support page, based on the first module's manufacturer (research
    // confirmed no memory vendor exposes a URL derivable from a WMI part-number string — Corsair/
    // G.Skill/Kingston/Crucial all require an internal catalog ID). Same honesty pattern as
    // StorageBasic.VendorSupportUrl. Null when the manufacturer isn't confidently identified.
    public string? VendorSupportUrl { get; init; }
}

public class MemoryModule
{
    // Null when WMI's Capacity property for this module is missing — previously fabricated as 0 GB,
    // which fed directly into RulesEngine.EvaluateMemory's mismatched-module-size check and could
    // produce a false "modules don't match" finding, or mask a genuine mismatch. See BACKLOG.md history.
    public double? SizeGb { get; init; }
    public string? Manufacturer { get; init; }
    public string? PartNumber { get; init; }
}

public class MemoryAdvanced
{
    public int? RatedSpeedMts { get; init; }
    public int? ActualSpeedMts { get; init; }
    public string? ChannelMode { get; init; }
    public int? TimingsCl { get; init; }
    public IReadOnlyList<MemoryModule>? PerModule { get; init; }
}

//*Built with assistance from __Claude Code__ by Anthropic.*
