//
// DeviceDNA
// Author:  nobody174 (nobodylearn174@gmail.com)
// Repo:    https://github.com/nobody174/DeviceDNA
// Patreon: https://www.patreon.com/c/Nobody174
// License: All rights reserved (c) 2026 nobody174
// "It's never too late to give up!"
//

using System.Reflection;
using System.Runtime.Versioning;

namespace DeviceDNA.PlatformAdapters;

// Concrete Windows Update Agent adapter via late-bound COM interop (Microsoft.Update.Session),
// the same official mechanism Windows' own Settings > Windows Update UI and Device Manager's
// "Update driver > Search automatically" use internally. No NuGet/PIA package needed — WUA is a
// COM object registered by Windows itself; Type.GetTypeFromProgID + reflection avoids taking a
// COM-interop-assembly dependency for a single narrow use.
[SupportedOSPlatform("windows")]
public class WuaAdapter : IWuaAdapter
{
    public IReadOnlyList<WuaDriverUpdate> SearchForDriverUpdates()
    {
        var results = new List<WuaDriverUpdate>();

        try
        {
            var sessionType = Type.GetTypeFromProgID("Microsoft.Update.Session");
            if (sessionType == null)
            {
                return results;
            }

            dynamic session = Activator.CreateInstance(sessionType)!;
            dynamic searcher = session.CreateUpdateSearcher();

            // Type='Driver' is a real, documented IUpdateSearcher criteria (learn.microsoft.com) —
            // distinct from the general "IsInstalled=0 and Type='Software'" OS-update search this
            // app deliberately does not perform automatically. This still requires a live round-trip
            // to Windows Update/WSUS; only ever invoked from an explicit user click (see
            // DeviceScanService/UI layer), never during a routine scan.
            dynamic searchResult = searcher.Search("IsInstalled=0 and Type='Driver'");
            dynamic updates = searchResult.Updates;

            int count = updates.Count;
            for (var i = 0; i < count; i++)
            {
                dynamic update = updates.Item(i);
                string title = update.Title ?? "Unknown update";
                string? description = update.Description;

                DateTime? lastDeploymentChangeTime = null;
                try
                {
                    lastDeploymentChangeTime = update.LastDeploymentChangeTime;
                }
                catch (Exception)
                {
                    // Not every update exposes this property populated; treat as unknown rather
                    // than fail the whole search over one missing field.
                }

                results.Add(new WuaDriverUpdate
                {
                    Title = title,
                    Description = description,
                    LastDeploymentChangeTime = lastDeploymentChangeTime,
                });
            }
        }
        catch (Exception)
        {
            // Real, expected failure modes: Windows Update service disabled/stopped, no network,
            // COM registration missing/corrupted, group policy blocking the search. Fail closed —
            // return no results rather than propagate a COM exception to the UI. "Could not
            // determine" and "found nothing" are treated the same by the caller either way (see
            // IWuaAdapter's doc comment).
        }

        return results;
    }
}

//*Built with assistance from __Claude Code__ by Anthropic.*
