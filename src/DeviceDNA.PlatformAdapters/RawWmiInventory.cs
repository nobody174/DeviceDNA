//
// DeviceDNA
// Author:  nobody174 (nobodylearn174@gmail.com)
// Repo:    https://github.com/nobody174/DeviceDNA
// Patreon: https://www.patreon.com/c/Nobody174
// License: All rights reserved (c) 2026 nobody174
// "It's never too late to give up!"
//

namespace DeviceDNA.PlatformAdapters;

// Raw, unnormalized static inventory pulled from WMI. One property bag per queried WMI class instance.
// Detection Engine maps these onto DeviceDNA.Model DNA field types.
public class RawWmiInventory
{
    public required string WmiClass { get; init; } // e.g. Win32_Processor, Win32_VideoController
    public required IReadOnlyDictionary<string, object?> Properties { get; init; }
}

//*Built with assistance from __Claude Code__ by Anthropic.*
