//
// DeviceDNA
// Author:  nobody174 (nobodylearn174@gmail.com)
// Repo:    https://github.com/nobody174/DeviceDNA
// Patreon: https://www.patreon.com/c/Nobody174
// License: All rights reserved (c) 2026 nobody174
// "It's never too late to give up!"
//

using DeviceDNA.PlatformAdapters;

namespace DeviceDNA.DetectionEngine;

// One driver update result surfaced to callers outside this layer — mirrors
// DeviceDNA.PlatformAdapters.WuaDriverUpdate but keeps the Application/UI layers from needing a
// reference to DeviceDNA.PlatformAdapters directly (same seam pattern as DeviceDetectionService's
// CreateDefault — see CLAUDE.md architecture layering).
public class DriverUpdateInfo
{
    public required string Title { get; init; }
    public string? Description { get; init; }
    public DateTime? LastDeploymentChangeTime { get; init; }
}

// Thin wrapper around IWuaAdapter — the seam that lets the Application layer trigger a Windows
// Update driver-update check without knowing about the concrete WuaAdapter/COM interop. Never
// called during a routine scan (REQUIREMENTS.md section 10, clarified 2026-08-15) — this class
// exists specifically to be invoked on an explicit, user-triggered action from the UI layer.
public class WindowsUpdateDriverChecker
{
    private readonly IWuaAdapter _wuaAdapter;

    public WindowsUpdateDriverChecker(IWuaAdapter wuaAdapter)
    {
        _wuaAdapter = wuaAdapter;
    }

    // Same factory-seam pattern as DeviceDetectionService.CreateDefault().
    public static WindowsUpdateDriverChecker CreateDefault()
    {
#pragma warning disable CA1416
        return new WindowsUpdateDriverChecker(new WuaAdapter());
#pragma warning restore CA1416
    }

    public IReadOnlyList<DriverUpdateInfo> CheckForDriverUpdates() =>
        _wuaAdapter.SearchForDriverUpdates()
            .Select(u => new DriverUpdateInfo
            {
                Title = u.Title,
                Description = u.Description,
                LastDeploymentChangeTime = u.LastDeploymentChangeTime,
            })
            .ToList();
}

//*Built with assistance from __Claude Code__ by Anthropic.*
