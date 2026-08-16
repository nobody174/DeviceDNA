//
// DeviceDNA
// Author:  nobody174 (nobodylearn174@gmail.com)
// Repo:    https://github.com/nobody174/DeviceDNA
// Patreon: https://www.patreon.com/c/Nobody174
// License: All rights reserved (c) 2026 nobody174
// "It's never too late to give up!"
//

namespace DeviceDNA.Model;

// Null Version/Date/SourceUrl combined with IsApplicable=false represents "not_applicable" per the schema in CLAUDE.md.
// Never fabricate SourceUrl — only set when confidently identifiable (REQUIREMENTS.md section 10).
//
// Driver applicability policy: only GPU and Network populate a real DriverInfo. This is a
// deliberate product decision, not an oversight — GPU and NIC drivers are the only ones with a
// genuinely separate, independently-updatable, user-facing update cadence (a user goes and
// downloads/updates them on their own, distinct from Windows Update/BIOS updates). CPU chipset,
// storage controller, and motherboard drivers are bundled with Windows Update or a BIOS/chipset
// package rather than being something a user tracks/updates as a standalone "driver version" the
// way GPU/NIC drivers are, so those DNAs use DriverInfo.NotApplicable. See BACKLOG.md history.
public class DriverInfo
{
    public bool IsApplicable { get; init; } = true;
    public string? Version { get; init; }
    public string? Date { get; init; }
    public string? SourceUrl { get; init; }

    public static DriverInfo NotApplicable => new() { IsApplicable = false };
}

//*Built with assistance from __Claude Code__ by Anthropic.*
