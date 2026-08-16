//
// DeviceDNA
// Author:  nobody174 (nobodylearn174@gmail.com)
// Repo:    https://github.com/nobody174/DeviceDNA
// Patreon: https://www.patreon.com/c/Nobody174
// License: All rights reserved (c) 2026 nobody174
// "It's never too late to give up!"
//

namespace DeviceDNA.PlatformAdapters;

// Local Windows registry reads only — no writes, no network. Registry access is OS-specific,
// so per CLAUDE.md's layering rule it's confined to this adapter, same as WmiAdapter for WMI.
public interface IRegistryAdapter
{
    // Returns true if the given HKEY_LOCAL_MACHINE subkey path exists (regardless of value
    // contents) — used for presence-only signals like Windows Update's RebootRequired key, where
    // the key's mere existence is the signal, not any value under it.
    bool LocalMachineKeyExists(string subKeyPath);

    // Reads a single named string value under the given HKEY_LOCAL_MACHINE subkey path — e.g.
    // "DisplayVersion" under CurrentVersion, the real per-release marketing name ("24H2") that
    // Win32_OperatingSystem.Version doesn't expose (it only reports the kernel build-number style
    // version). Returns null if the key/value doesn't exist or access fails, never a fabricated
    // fallback string.
    string? GetLocalMachineStringValue(string subKeyPath, string valueName);
}

//*Built with assistance from __Claude Code__ by Anthropic.*
