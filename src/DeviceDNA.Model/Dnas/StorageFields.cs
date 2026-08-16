//
// DeviceDNA
// Author:  nobody174 (nobodylearn174@gmail.com)
// Repo:    https://github.com/nobody174/DeviceDNA
// Patreon: https://www.patreon.com/c/Nobody174
// License: All rights reserved (c) 2026 nobody174
// "It's never too late to give up!"
//

namespace DeviceDNA.Model.Dnas;

public class StorageBasic
{
    public required string Model { get; init; }

    // Null when WMI's Size is unavailable — previously fabricated as 0 GB.
    public double? CapacityGb { get; init; }
    public double FreeSpacePct { get; init; }
    public required string Type { get; init; } // SSD, HDD, NVMe

    // The manufacturer's general support/product page — never per-model, since research confirmed
    // no storage vendor (Samsung, WD, Kingston, Crucial, Seagate) exposes a URL derivable from a
    // WMI model string; their per-model pages use internal catalog IDs/shortened codes with no
    // mechanical mapping. Same honesty pattern as MotherboardBasic.VendorSupportUrl's
    // Gigabyte/ASRock/MSI fallback. Null when the manufacturer isn't confidently identified.
    public string? VendorSupportUrl { get; init; }
}

public class StoragePartition
{
    public required string DriveLetter { get; init; }
    public double? CapacityGb { get; init; }
    public double FreeSpacePct { get; init; }
}

public class StorageAdvanced
{
    public string? Interface { get; init; }
    public double? RatedSpeedMbps { get; init; }
    public double? SmartHealthPct { get; init; }
    public double? CurrentTempC { get; init; }
    public IReadOnlyList<StoragePartition>? Partitions { get; init; }
}

//*Built with assistance from __Claude Code__ by Anthropic.*
