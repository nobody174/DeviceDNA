//
// DeviceDNA
// Author:  nobody174 (nobodylearn174@gmail.com)
// Repo:    https://github.com/nobody174/DeviceDNA
// Patreon: https://www.patreon.com/c/Nobody174
// License: All rights reserved (c) 2026 nobody174
// "It's never too late to give up!"
//

using System.Management;
using System.Runtime.Versioning;

namespace DeviceDNA.PlatformAdapters;

// Concrete adapter for static hardware inventory via WMI (Win32_Processor, Win32_VideoController,
// Win32_PhysicalMemory, Win32_DiskDrive, Win32_BaseBoard, Win32_BIOS, Win32_NetworkAdapter,
// Win32_OperatingSystem, Win32_ComputerSystem, etc.). Returns raw property bags — the Detection
// Engine is responsible for mapping these onto DeviceDNA.Model DNA field types.
// WMI is Windows-only, matching this adapter's placement in the OS-specific Platform Adapter layer.
[SupportedOSPlatform("windows")]
public class WmiAdapter : IWmiAdapter
{
    public IReadOnlyList<RawWmiInventory> Query(string wmiClass, string? wmiNamespace = null)
    {
        var results = new List<RawWmiInventory>();
        var scope = wmiNamespace ?? "root\\cimv2";

        try
        {
            using var searcher = new ManagementObjectSearcher(scope, $"SELECT * FROM {wmiClass}");
            using var collection = searcher.Get();

            foreach (ManagementBaseObject obj in collection)
            {
                using (obj)
                {
                    var properties = new Dictionary<string, object?>();
                    foreach (var property in obj.Properties)
                    {
                        properties[property.Name] = property.Value;
                    }

                    results.Add(new RawWmiInventory
                    {
                        WmiClass = wmiClass,
                        Properties = properties,
                    });
                }
            }
        }
        catch (Exception)
        {
            // WMI class may not exist on this system, or access may be restricted.
            // Return whatever was collected (possibly empty) rather than crashing the caller.
        }

        return results;
    }
}

//*Built with assistance from __Claude Code__ by Anthropic.*
