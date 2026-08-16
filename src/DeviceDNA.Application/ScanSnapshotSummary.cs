//
// DeviceDNA
// Author:  nobody174 (nobodylearn174@gmail.com)
// Repo:    https://github.com/nobody174/DeviceDNA
// Patreon: https://www.patreon.com/c/Nobody174
// License: All rights reserved (c) 2026 nobody174
// "It's never too late to give up!"
//

namespace DeviceDNA.Application;

// Lightweight summary row for a past scan, used by the History list without loading the full Device.
public class ScanSnapshotSummary
{
    public required long ScanId { get; init; }
    public required DateTime Timestamp { get; init; }
    public required string Hostname { get; init; }
    public required string OsSummary { get; init; }
    public required Model.HealthStatus OverallStatus { get; init; }
}

//*Built with assistance from __Claude Code__ by Anthropic.*
