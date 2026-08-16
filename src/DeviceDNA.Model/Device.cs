//
// DeviceDNA
// Author:  nobody174 (nobodylearn174@gmail.com)
// Repo:    https://github.com/nobody174/DeviceDNA
// Patreon: https://www.patreon.com/c/Nobody174
// License: All rights reserved (c) 2026 nobody174
// "It's never too late to give up!"
//

namespace DeviceDNA.Model;

public class ScanHistoryEntry
{
    public required DateTime Timestamp { get; init; }
    public required string SnapshotId { get; init; }
}

// The root container — the whole machine. Never itself a DNA. See REQUIREMENTS.md section 1.
public class Device
{
    public required string Id { get; init; }
    public required string Hostname { get; init; }
    public required string OsSummary { get; init; }
    public required string FormFactor { get; init; }
    public required IReadOnlyList<Dna> Dnas { get; init; }
    public IReadOnlyList<ScanHistoryEntry> ScanHistory { get; init; } = Array.Empty<ScanHistoryEntry>();
}

//*Built with assistance from __Claude Code__ by Anthropic.*
