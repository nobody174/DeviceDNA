//
// DeviceDNA
// Author:  nobody174 (nobodylearn174@gmail.com)
// Repo:    https://github.com/nobody174/DeviceDNA
// Patreon: https://www.patreon.com/c/Nobody174
// License: All rights reserved (c) 2026 nobody174
// "It's never too late to give up!"
//

namespace DeviceDNA.Model;

// One physical hardware component category. See REQUIREMENTS.md section 1 and CLAUDE.md's Full DNA Data Model Schema.
// Basic/Advanced hold one of the Dnas/* field types matching Type (e.g. Type=Cpu -> Basic is CpuBasic).
// Deep is reserved, always null in v1 (deferred to v1.1+, REQUIREMENTS.md section 2).
public class Dna
{
    public required string Id { get; init; }
    public required DnaType Type { get; init; }
    public required string Name { get; init; }
    public required string Manufacturer { get; init; }
    public required string Summary { get; init; }
    public required HealthStatus Status { get; init; }
    public required IReadOnlyList<StatusReason> StatusReasons { get; init; }
    public required object Basic { get; init; }
    public object? Advanced { get; init; }
    public object? Deep { get; init; } = null;
    public required DriverInfo Driver { get; init; }
    public required DateTime LastUpdated { get; init; }
}

//*Built with assistance from __Claude Code__ by Anthropic.*
