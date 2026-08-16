//
// DeviceDNA
// Author:  nobody174 (nobodylearn174@gmail.com)
// Repo:    https://github.com/nobody174/DeviceDNA
// Patreon: https://www.patreon.com/c/Nobody174
// License: All rights reserved (c) 2026 nobody174
// "It's never too late to give up!"
//

namespace DeviceDNA.Model.Dnas;

public class MotherboardBasic
{
    public required string Manufacturer { get; init; }
    public required string Model { get; init; }
    public string? BiosVersion { get; init; }
    public string? Chipset { get; init; }

    // Link to the vendor's official support/BIOS-download page for this board — the user clicks
    // through and checks themselves whether a newer BIOS exists. Never a live fetch/scrape (see
    // REQUIREMENTS.md section 10, clarified 2026-08-15). Null when no confident vendor page pattern
    // exists for this manufacturer.
    public string? VendorSupportUrl { get; init; }
}

public class MemorySupport
{
    public string? Type { get; init; }
    public double? MaxCapacityGb { get; init; }
    public int? Slots { get; init; }
}

public class PcieSlot
{
    public int Generation { get; init; }
    public int PhysicalWidth { get; init; }
    public int? NegotiatedWidth { get; init; }
    public bool InUse { get; init; }
    public string? PopulatedBy { get; init; }
}

public class MotherboardAdvanced
{
    public string? BiosDate { get; init; }
    public string? Socket { get; init; }
    public MemorySupport? MemorySupport { get; init; }
    public IReadOnlyList<PcieSlot>? PcieSlots { get; init; }
    public int? M2Slots { get; init; }
}

//*Built with assistance from __Claude Code__ by Anthropic.*
