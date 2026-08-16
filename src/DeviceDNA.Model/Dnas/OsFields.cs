//
// DeviceDNA
// Author:  nobody174 (nobodylearn174@gmail.com)
// Repo:    https://github.com/nobody174/DeviceDNA
// Patreon: https://www.patreon.com/c/Nobody174
// License: All rights reserved (c) 2026 nobody174
// "It's never too late to give up!"
//

namespace DeviceDNA.Model.Dnas;

public class OsBasic
{
    public required string OsName { get; init; }
    public required string Version { get; init; }
    public required string BuildNumber { get; init; }
    public DateTime? InstallDate { get; init; }

    // Microsoft's real, official release-health page for this exact Windows 11 feature update
    // (e.g. "status-windows-11-24h2"), derived from the registry's DisplayVersion value — a
    // genuinely stable, per-version URL pattern (research-confirmed), not a guess. Null for
    // Windows 10 or when DisplayVersion isn't available; falls back to the general release-health
    // hub in that case (see BuildOsVendorUrl).
    public string? VendorSupportUrl { get; init; }
}

public class OsAdvanced
{
    public TimeSpan? Uptime { get; init; }
    public DateTime? LastUpdateDate { get; init; }
    public string? ActivationStatus { get; init; }
    public string? Architecture { get; init; }

    // True when Windows has an update installed that's waiting on a reboot to finish applying
    // (distinct from "updates are available" — that would require a live Windows Update server
    // query, which conflicts with this app's no-network-call principle; see BACKLOG.md). Null when
    // this genuinely could not be determined (e.g. registry read failed) rather than assumed false.
    public bool? RebootPending { get; init; }
}

//*Built with assistance from __Claude Code__ by Anthropic.*
