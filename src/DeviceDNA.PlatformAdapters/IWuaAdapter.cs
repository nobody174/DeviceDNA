//
// DeviceDNA
// Author:  nobody174 (nobodylearn174@gmail.com)
// Repo:    https://github.com/nobody174/DeviceDNA
// Patreon: https://www.patreon.com/c/Nobody174
// License: All rights reserved (c) 2026 nobody174
// "It's never too late to give up!"
//

namespace DeviceDNA.PlatformAdapters;

// One driver update Windows Update knows about and considers applicable to this machine.
public class WuaDriverUpdate
{
    public required string Title { get; init; }
    public string? Description { get; init; }
    public DateTime? LastDeploymentChangeTime { get; init; }
}

// Windows Update Agent (WUA) access — a live, user-triggered, opt-in check against Microsoft's
// update servers for applicable driver updates. Never called automatically as part of a routine
// scan (REQUIREMENTS.md section 10, clarified 2026-08-15: online lookups are allowed, always
// opt-in per user action). Windows-specific COM API, so per CLAUDE.md's layering rule it's
// confined to this adapter, same pattern as IWmiAdapter/IRegistryAdapter.
public interface IWuaAdapter
{
    // Searches Windows Update for applicable, not-yet-installed driver updates (Type='Driver').
    // Windows itself determines applicability to this machine — no hardware-ID filtering is done
    // by this app. Requires a live network round-trip to Windows Update/WSUS; may take several
    // seconds. Returns an empty list (never throws to the caller) if the search fails or finds
    // nothing — "no driver updates found via Windows Update" is never distinguished from "the
    // search itself failed" at this layer, since both honestly mean the same thing to the caller:
    // no confirmed update to report.
    IReadOnlyList<WuaDriverUpdate> SearchForDriverUpdates();
}

//*Built with assistance from __Claude Code__ by Anthropic.*
