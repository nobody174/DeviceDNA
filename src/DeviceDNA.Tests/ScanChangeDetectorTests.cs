//
// DeviceDNA
// Author:  nobody174 (nobodylearn174@gmail.com)
// Repo:    https://github.com/nobody174/DeviceDNA
// Patreon: https://www.patreon.com/c/Nobody174
// License: All rights reserved (c) 2026 nobody174
// "It's never too late to give up!"
//

using DeviceDNA.Application;
using DeviceDNA.Model;
using DeviceDNA.Model.Dnas;

namespace DeviceDNA.Tests;

// Covers ScanChangeDetector.Compare — the History "Changes" view's diff logic. The riskiest part
// of this class isn't the happy path (1:1 same-named DNA before/after), it's the ambiguous cases
// it deliberately refuses to guess at (2+ same-named DNAs, per ScanChangeDetector.cs's own comment
// about not wanting a confidently-wrong "changed" entry from unstable enumeration order).
public class ScanChangeDetectorTests
{
    [Fact]
    public void Compare_NewDeviceAdded_ReportsAddition()
    {
        var previous = TestBuilders.MakeDevice();
        var current = TestBuilders.MakeDevice(
            TestBuilders.MakeDna(DnaType.Gpu, "New GPU", TestBuilders.GpuBasic("New GPU")));

        var changes = ScanChangeDetector.Compare(previous, current);

        Assert.Contains(changes, c => c.Description.Contains("New device detected"));
    }

    [Fact]
    public void Compare_DeviceRemoved_ReportsRemoval()
    {
        var previous = TestBuilders.MakeDevice(
            TestBuilders.MakeDna(DnaType.Gpu, "Old GPU", TestBuilders.GpuBasic("Old GPU")));
        var current = TestBuilders.MakeDevice();

        var changes = ScanChangeDetector.Compare(previous, current);

        Assert.Contains(changes, c => c.Description.Contains("no longer detected"));
    }

    [Fact]
    public void Compare_NoChanges_ReturnsEmpty()
    {
        var dna = TestBuilders.MakeDna(DnaType.Gpu, "Same GPU", TestBuilders.GpuBasic("Same GPU", driverVersion: "1.0"));
        var previous = TestBuilders.MakeDevice(dna);
        var current = TestBuilders.MakeDevice(dna);

        var changes = ScanChangeDetector.Compare(previous, current);

        Assert.Empty(changes);
    }

    [Fact]
    public void Compare_StatusChanged_ReportsStatusChange()
    {
        var previous = TestBuilders.MakeDevice(
            TestBuilders.MakeDna(DnaType.Cpu, "Test CPU", new CpuBasic { Name = "Test CPU", Manufacturer = "Test" }, status: HealthStatus.Green));
        var current = TestBuilders.MakeDevice(
            TestBuilders.MakeDna(DnaType.Cpu, "Test CPU", new CpuBasic { Name = "Test CPU", Manufacturer = "Test" }, status: HealthStatus.Yellow));

        var changes = ScanChangeDetector.Compare(previous, current);

        Assert.Contains(changes, c => c.Description.Contains("Health status changed: Green → Yellow"));
    }

    [Fact]
    public void Compare_DriverVersionChanged_ReportsDriverUpdate()
    {
        var driverBefore = new DriverInfo { IsApplicable = true, Version = "1.0" };
        var driverAfter = new DriverInfo { IsApplicable = true, Version = "2.0" };

        var previous = TestBuilders.MakeDevice(
            TestBuilders.MakeDna(DnaType.Gpu, "Test GPU", TestBuilders.GpuBasic("Test GPU"), driver: driverBefore));
        var current = TestBuilders.MakeDevice(
            TestBuilders.MakeDna(DnaType.Gpu, "Test GPU", TestBuilders.GpuBasic("Test GPU"), driver: driverAfter));

        var changes = ScanChangeDetector.Compare(previous, current);

        Assert.Contains(changes, c => c.Description.Contains("Driver updated: 1.0 → 2.0"));
    }

    [Fact]
    public void Compare_DriverNotApplicable_NeverReportsDriverUpdate()
    {
        var driverBefore = DriverInfo.NotApplicable;
        var driverAfter = DriverInfo.NotApplicable;

        var previous = TestBuilders.MakeDevice(
            TestBuilders.MakeDna(DnaType.Cpu, "Test CPU", new CpuBasic { Name = "Test CPU", Manufacturer = "Test" }, driver: driverBefore));
        var current = TestBuilders.MakeDevice(
            TestBuilders.MakeDna(DnaType.Cpu, "Test CPU", new CpuBasic { Name = "Test CPU", Manufacturer = "Test" }, driver: driverAfter));

        var changes = ScanChangeDetector.Compare(previous, current);

        Assert.DoesNotContain(changes, c => c.Description.Contains("Driver updated"));
    }

    [Fact]
    public void Compare_StorageFreeSpaceChanged_ReportsChange()
    {
        var previous = TestBuilders.MakeDevice(
            TestBuilders.MakeDna(DnaType.Storage, "Test SSD", TestBuilders.StorageBasic("Test SSD", freeSpacePct: 80.0)));
        var current = TestBuilders.MakeDevice(
            TestBuilders.MakeDna(DnaType.Storage, "Test SSD", TestBuilders.StorageBasic("Test SSD", freeSpacePct: 40.0)));

        var changes = ScanChangeDetector.Compare(previous, current);

        Assert.Contains(changes, c => c.Description.Contains("Free space changed from 80% to 40%"));
    }

    [Fact]
    public void Compare_StorageFreeSpaceRoundsToNearestPercent_SubPercentNoiseIgnored()
    {
        var previous = TestBuilders.MakeDevice(
            TestBuilders.MakeDna(DnaType.Storage, "Test SSD", TestBuilders.StorageBasic("Test SSD", freeSpacePct: 80.2)));
        var current = TestBuilders.MakeDevice(
            TestBuilders.MakeDna(DnaType.Storage, "Test SSD", TestBuilders.StorageBasic("Test SSD", freeSpacePct: 80.4)));

        var changes = ScanChangeDetector.Compare(previous, current);

        Assert.DoesNotContain(changes, c => c.Description.Contains("Free space changed"));
    }

    [Fact]
    public void Compare_MemorySpeedChanged_ReportsChange()
    {
        var previous = TestBuilders.MakeDevice(
            TestBuilders.MakeDna(DnaType.Memory, "RAM", TestBuilders.MemoryBasic(speedMts: 2933)));
        var current = TestBuilders.MakeDevice(
            TestBuilders.MakeDna(DnaType.Memory, "RAM", TestBuilders.MemoryBasic(speedMts: 3600)));

        var changes = ScanChangeDetector.Compare(previous, current);

        Assert.Contains(changes, c => c.Description.Contains("Memory speed changed: 2933 MT/s → 3600 MT/s"));
    }

    [Fact]
    public void Compare_AmbiguousDuplicateNames_SkipsFieldComparisonRatherThanGuessing()
    {
        // Two identically-named/typed disks on both sides — ScanChangeDetector must not positionally
        // pair them (enumeration order between two separate scans isn't guaranteed stable), since
        // that risks a confidently wrong "changed" entry. No field-level changes should be reported
        // for this group, even though the two sides' free-space values genuinely differ.
        var previous = TestBuilders.MakeDevice(
            TestBuilders.MakeDna(DnaType.Storage, "Same Model SSD", TestBuilders.StorageBasic("Same Model SSD", freeSpacePct: 90.0)),
            TestBuilders.MakeDna(DnaType.Storage, "Same Model SSD", TestBuilders.StorageBasic("Same Model SSD", freeSpacePct: 10.0)));
        var current = TestBuilders.MakeDevice(
            TestBuilders.MakeDna(DnaType.Storage, "Same Model SSD", TestBuilders.StorageBasic("Same Model SSD", freeSpacePct: 20.0)),
            TestBuilders.MakeDna(DnaType.Storage, "Same Model SSD", TestBuilders.StorageBasic("Same Model SSD", freeSpacePct: 80.0)));

        var changes = ScanChangeDetector.Compare(previous, current);

        Assert.DoesNotContain(changes, c => c.Description.Contains("Free space changed"));
    }

    [Fact]
    public void Compare_CountIncreasesFromOneToTwo_ReportsAdditionNotAmbiguousField()
    {
        var previous = TestBuilders.MakeDevice(
            TestBuilders.MakeDna(DnaType.Storage, "Same Model SSD", TestBuilders.StorageBasic("Same Model SSD", freeSpacePct: 50.0)));
        var current = TestBuilders.MakeDevice(
            TestBuilders.MakeDna(DnaType.Storage, "Same Model SSD", TestBuilders.StorageBasic("Same Model SSD", freeSpacePct: 50.0)),
            TestBuilders.MakeDna(DnaType.Storage, "Same Model SSD", TestBuilders.StorageBasic("Same Model SSD", freeSpacePct: 50.0)));

        var changes = ScanChangeDetector.Compare(previous, current);

        Assert.Contains(changes, c => c.Description.Contains("New device detected"));
    }
}

//*Built with assistance from __Claude Code__ by Anthropic.*
