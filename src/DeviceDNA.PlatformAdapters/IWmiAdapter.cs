//
// DeviceDNA
// Author:  nobody174 (nobodylearn174@gmail.com)
// Repo:    https://github.com/nobody174/DeviceDNA
// Patreon: https://www.patreon.com/c/Nobody174
// License: All rights reserved (c) 2026 nobody174
// "It's never too late to give up!"
//

namespace DeviceDNA.PlatformAdapters;

// Static inventory only (name, manufacturer, capacity, BIOS version, etc.) — not live sensor data.
// See CLAUDE.md: WmiAdapter is separate from LibreHardwareMonitorAdapter, organized by data source.
public interface IWmiAdapter
{
    // wmiNamespace defaults to "root\cimv2" (the namespace most Win32_* classes live in) when null.
    // Some classes (e.g. Win32_PowerPlan) live under other namespaces (e.g. "root\cimv2\power").
    IReadOnlyList<RawWmiInventory> Query(string wmiClass, string? wmiNamespace = null);
}

//*Built with assistance from __Claude Code__ by Anthropic.*
