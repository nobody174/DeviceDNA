//
// DeviceDNA
// Author:  nobody174 (nobodylearn174@gmail.com)
// Repo:    https://github.com/nobody174/DeviceDNA
// Patreon: https://www.patreon.com/c/Nobody174
// License: All rights reserved (c) 2026 nobody174
// "It's never too late to give up!"
//

using DeviceDNA.DetectionEngine;

namespace DeviceDNA.Application;

// One result of a user-triggered "Check Windows Update for driver updates" action.
public class DriverUpdateCheckResult
{
    public required bool Succeeded { get; init; }
    public required IReadOnlyList<DriverUpdateInfo> Updates { get; init; }
}

// Thin orchestration service wrapping WindowsUpdateDriverChecker for the UI layer. Deliberately
// separate from DeviceScanService — this must NEVER run as part of a routine scan (REQUIREMENTS.md
// section 10, clarified 2026-08-15: online lookups are always opt-in per explicit user action). The
// UI calls CheckForDriverUpdates() only in direct response to a user clicking a "Check Windows
// Update" button on a specific DNA tile.
public class WindowsUpdateDriverCheckService
{
    private readonly WindowsUpdateDriverChecker _checker;

    public WindowsUpdateDriverCheckService() : this(WindowsUpdateDriverChecker.CreateDefault())
    {
    }

    public WindowsUpdateDriverCheckService(WindowsUpdateDriverChecker checker)
    {
        _checker = checker;
    }

    // Windows determines applicability to this machine itself — this app does not filter by
    // hardware ID. Real-world coverage caveat (confirmed by research, not this app's assumption):
    // GPU vendors (NVIDIA/AMD) treat Windows Update as a secondary channel and typically lag behind
    // their own driver pages; motherboard BIOS is essentially never distributed via Windows Update
    // at all. This check is genuinely useful for chipset/network/audio/storage-controller drivers,
    // less so for GPU/BIOS — the UI must say so, never imply "Windows Update said no" means
    // "you're definitely current."
    public DriverUpdateCheckResult CheckForDriverUpdates()
    {
        try
        {
            var updates = _checker.CheckForDriverUpdates();
            return new DriverUpdateCheckResult { Succeeded = true, Updates = updates };
        }
        catch (Exception)
        {
            return new DriverUpdateCheckResult { Succeeded = false, Updates = Array.Empty<DriverUpdateInfo>() };
        }
    }
}

//*Built with assistance from __Claude Code__ by Anthropic.*
