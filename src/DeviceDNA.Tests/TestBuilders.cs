//
// DeviceDNA
// Author:  nobody174 (nobodylearn174@gmail.com)
// Repo:    https://github.com/nobody174/DeviceDNA
// Patreon: https://www.patreon.com/c/Nobody174
// License: All rights reserved (c) 2026 nobody174
// "It's never too late to give up!"
//

using DeviceDNA.Model;
using DeviceDNA.Model.Dnas;

namespace DeviceDNA.Tests;

// Small factory helpers for building minimal-but-valid Model objects in tests, since most Dna
// fields use `required` and constructing one inline in every test would bury the field(s) that
// actually matter to that test under a wall of unrelated boilerplate.
internal static class TestBuilders
{
    public static Dna MakeDna(
        DnaType type,
        string name,
        object basic,
        object? advanced = null,
        HealthStatus status = HealthStatus.Green,
        DriverInfo? driver = null,
        string manufacturer = "Test Manufacturer")
    {
        return new Dna
        {
            Id = Guid.NewGuid().ToString(),
            Type = type,
            Name = name,
            Manufacturer = manufacturer,
            Summary = "Test summary.",
            Status = status,
            StatusReasons = new List<StatusReason>
            {
                new() { Message = "OK", Severity = ReasonSeverity.Info, Confidence = Confidence.High },
            },
            Basic = basic,
            Advanced = advanced,
            Driver = driver ?? DriverInfo.NotApplicable,
            LastUpdated = DateTime.UtcNow,
        };
    }

    public static Device MakeDevice(params Dna[] dnas)
    {
        return new Device
        {
            Id = Guid.NewGuid().ToString(),
            Hostname = "TEST-HOST",
            OsSummary = "Windows 11",
            FormFactor = "Desktop",
            Dnas = dnas,
        };
    }

    public static StorageBasic StorageBasic(string model = "Test SSD", double freeSpacePct = 50.0, string type = "SSD") => new()
    {
        Model = model,
        Type = type,
        FreeSpacePct = freeSpacePct,
    };

    public static MemoryBasic MemoryBasic(int speedMts = 3200, string type = "DDR4") => new()
    {
        Type = type,
        SpeedMts = speedMts,
    };

    public static GpuBasic GpuBasic(string name = "Test GPU", string? driverVersion = null) => new()
    {
        Name = name,
        Manufacturer = "Test Manufacturer",
        DriverVersion = driverVersion,
    };

    public static NetworkBasic NetworkBasic(double? currentSpeedMbps = 1000) => new()
    {
        AdapterName = "Test Adapter",
        ConnectionType = "Wired",
        CurrentSpeedMbps = currentSpeedMbps,
    };

    public static MotherboardBasic MotherboardBasic(string? biosVersion = null) => new()
    {
        Manufacturer = "Test Manufacturer",
        Model = "Test Board",
        BiosVersion = biosVersion,
    };
}

//*Built with assistance from __Claude Code__ by Anthropic.*
