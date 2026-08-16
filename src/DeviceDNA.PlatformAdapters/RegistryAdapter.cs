//
// DeviceDNA
// Author:  nobody174 (nobodylearn174@gmail.com)
// Repo:    https://github.com/nobody174/DeviceDNA
// Patreon: https://www.patreon.com/c/Nobody174
// License: All rights reserved (c) 2026 nobody174
// "It's never too late to give up!"
//

using Microsoft.Win32;
using System.Runtime.Versioning;

namespace DeviceDNA.PlatformAdapters;

// Concrete local-registry adapter, read-only. Windows-only, matching this adapter's placement in
// the OS-specific Platform Adapter layer.
[SupportedOSPlatform("windows")]
public class RegistryAdapter : IRegistryAdapter
{
    public bool LocalMachineKeyExists(string subKeyPath)
    {
        try
        {
            using var key = Registry.LocalMachine.OpenSubKey(subKeyPath);
            return key != null;
        }
        catch (Exception)
        {
            // Registry access may be restricted in unusual environments (e.g. locked-down
            // policy); treat as "could not determine" rather than crashing the caller.
            return false;
        }
    }

    public string? GetLocalMachineStringValue(string subKeyPath, string valueName)
    {
        try
        {
            using var key = Registry.LocalMachine.OpenSubKey(subKeyPath);
            return key?.GetValue(valueName) as string;
        }
        catch (Exception)
        {
            // Same defensive posture as LocalMachineKeyExists — a restricted/missing key is
            // "could not determine," never a crash.
            return null;
        }
    }
}

//*Built with assistance from __Claude Code__ by Anthropic.*
