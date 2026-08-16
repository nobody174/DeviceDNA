//
// DeviceDNA
// Author:  nobody174 (nobodylearn174@gmail.com)
// Repo:    https://github.com/nobody174/DeviceDNA
// Patreon: https://www.patreon.com/c/Nobody174
// License: All rights reserved (c) 2026 nobody174
// "It's never too late to give up!"
//

namespace DeviceDNA.PlatformAdapters;

// Raw, unnormalized reading straight from a hardware source. Adapters produce these;
// the Detection Engine (not adapters) maps them onto DeviceDNA.Model DNA field types.
public class RawSensorReading
{
    public required string HardwareName { get; init; }
    public required string HardwareType { get; init; } // Cpu, GpuNvidia, GpuAmd, GpuIntel, Memory, Storage, Motherboard, Network
    // LibreHardwareMonitor's internal Identifier string (e.g. "/storage/nvme/0" or "/storage/ata/1").
    // For Storage hardware this embeds the Windows physical drive index (StorageDeviceNumber) as the
    // trailing path segment — the same integer WMI exposes as Win32_DiskDrive.Index. Used to correlate
    // a physical disk to its LHM sensors reliably, instead of matching on model-name substrings which
    // cannot disambiguate two identically-modeled drives.
    public required string HardwareIdentifier { get; init; }
    public required string SensorName { get; init; }
    public required string SensorType { get; init; } // Temperature, Load, Clock, Power, Data, etc.
    public float? Value { get; init; }
}

//*Built with assistance from __Claude Code__ by Anthropic.*
