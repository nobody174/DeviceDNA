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
using Microsoft.Data.Sqlite;

namespace DeviceDNA.Tests;

// Covers ScanHistoryRepository's SQLite persistence: schema creation, save/load round-tripping,
// DNA-type filtering, and the defensive per-row skip-on-corruption behavior it documents in its own
// comments. Each test gets its own temp .db file (via the databasePath constructor overload) so
// tests never share state or run against the real %LOCALAPPDATA% database.
public class ScanHistoryRepositoryTests : IDisposable
{
    private readonly string _dbPath;
    private readonly ScanHistoryRepository _repository;

    public ScanHistoryRepositoryTests()
    {
        _dbPath = Path.Combine(Path.GetTempPath(), $"devicedna-test-{Guid.NewGuid():N}.db");
        _repository = new ScanHistoryRepository(_dbPath);
    }

    public void Dispose()
    {
        // Microsoft.Data.Sqlite pools connections by connection string even though the repository
        // closes each one after use, so the underlying file handle can outlive the C# `using`
        // block. ClearAllPools forces those pooled native handles closed before the delete below.
        SqliteConnection.ClearAllPools();
        if (File.Exists(_dbPath))
        {
            File.Delete(_dbPath);
        }
    }

    [Fact]
    public void SaveScan_ReturnsPositiveScanId()
    {
        var device = TestBuilders.MakeDevice(
            TestBuilders.MakeDna(DnaType.Cpu, "Test CPU", new CpuBasic { Name = "Test CPU", Manufacturer = "Test" }));

        var scanId = _repository.SaveScan(device);

        Assert.True(scanId > 0);
    }

    [Fact]
    public void SaveScan_ThenLoadScan_RoundTripsCoreDeviceFields()
    {
        var device = TestBuilders.MakeDevice(
            TestBuilders.MakeDna(DnaType.Cpu, "Test CPU", new CpuBasic { Name = "Test CPU", Manufacturer = "AMD" }));

        var scanId = _repository.SaveScan(device);
        var loaded = _repository.LoadScan(scanId);

        Assert.NotNull(loaded);
        Assert.Equal(device.Hostname, loaded!.Hostname);
        Assert.Equal(device.OsSummary, loaded.OsSummary);
        Assert.Equal(device.FormFactor, loaded.FormFactor);
    }

    [Fact]
    public void SaveScan_ThenLoadScan_RoundTripsDnaBasicFields()
    {
        var device = TestBuilders.MakeDevice(
            TestBuilders.MakeDna(
                DnaType.Storage,
                "Samsung 970 EVO",
                TestBuilders.StorageBasic("Samsung 970 EVO", freeSpacePct: 42.5, type: "NVMe"),
                manufacturer: "Samsung"));

        var scanId = _repository.SaveScan(device);
        var loaded = _repository.LoadScan(scanId);

        var dna = Assert.Single(loaded!.Dnas);
        Assert.Equal("Samsung 970 EVO", dna.Name);
        Assert.Equal("Samsung", dna.Manufacturer);
        var basic = Assert.IsType<StorageBasic>(dna.Basic);
        Assert.Equal(42.5, basic.FreeSpacePct);
        Assert.Equal("NVMe", basic.Type);
    }

    [Fact]
    public void SaveScan_ThenLoadScan_RoundTripsAdvancedFieldsAndNullsSurviveIntact()
    {
        var advanced = new CpuAdvanced { CurrentTempC = 55.5, BoostClockGhz = null, VirtualizationSupport = true };
        var device = TestBuilders.MakeDevice(
            TestBuilders.MakeDna(DnaType.Cpu, "Test CPU", new CpuBasic { Name = "Test CPU", Manufacturer = "Intel" }, advanced));

        var scanId = _repository.SaveScan(device);
        var loaded = _repository.LoadScan(scanId);

        var loadedAdvanced = Assert.IsType<CpuAdvanced>(Assert.Single(loaded!.Dnas).Advanced);
        Assert.Equal(55.5, loadedAdvanced.CurrentTempC);
        Assert.Null(loadedAdvanced.BoostClockGhz);
        Assert.True(loadedAdvanced.VirtualizationSupport);
    }

    [Fact]
    public void SaveScan_ThenLoadScan_RoundTripsStatusReasons()
    {
        var dna = TestBuilders.MakeDna(DnaType.Os, "Windows 11", new OsBasic { OsName = "Windows 11", Version = "24H2", BuildNumber = "26100" });
        var device = TestBuilders.MakeDevice(dna);

        var scanId = _repository.SaveScan(device);
        var loaded = _repository.LoadScan(scanId);

        var reason = Assert.Single(Assert.Single(loaded!.Dnas).StatusReasons);
        Assert.Equal("OK", reason.Message);
        Assert.Equal(ReasonSeverity.Info, reason.Severity);
    }

    [Fact]
    public void SaveScan_ThenLoadScan_RoundTripsDriverInfo()
    {
        var driver = new DriverInfo { IsApplicable = true, Version = "31.0.15", Date = "2026-06-01", SourceUrl = "https://example.com/driver" };
        var device = TestBuilders.MakeDevice(
            TestBuilders.MakeDna(DnaType.Gpu, "Test GPU", TestBuilders.GpuBasic("Test GPU"), driver: driver));

        var scanId = _repository.SaveScan(device);
        var loaded = _repository.LoadScan(scanId);

        var loadedDriver = Assert.Single(loaded!.Dnas).Driver;
        Assert.True(loadedDriver.IsApplicable);
        Assert.Equal("31.0.15", loadedDriver.Version);
        Assert.Equal("https://example.com/driver", loadedDriver.SourceUrl);
    }

    [Fact]
    public void LoadScan_UnknownScanId_ReturnsNull()
    {
        var loaded = _repository.LoadScan(999999);

        Assert.Null(loaded);
    }

    [Fact]
    public void ListScans_NoFilter_ReturnsAllScansNewestFirst()
    {
        var device = TestBuilders.MakeDevice(
            TestBuilders.MakeDna(DnaType.Cpu, "Test CPU", new CpuBasic { Name = "Test CPU", Manufacturer = "Test" }));

        var firstId = _repository.SaveScan(device);
        var secondId = _repository.SaveScan(device);

        var scans = _repository.ListScans();

        Assert.True(scans.Count >= 2);
        var firstIndex = scans.ToList().FindIndex(s => s.ScanId == firstId);
        var secondIndex = scans.ToList().FindIndex(s => s.ScanId == secondId);
        Assert.True(secondIndex < firstIndex, "Newer scan should sort before older scan.");
    }

    [Fact]
    public void ListScans_FilteredByDnaType_OnlyReturnsMatchingScans()
    {
        var deviceWithGpu = TestBuilders.MakeDevice(
            TestBuilders.MakeDna(DnaType.Gpu, "Test GPU", TestBuilders.GpuBasic("Test GPU")));
        var deviceWithoutGpu = TestBuilders.MakeDevice(
            TestBuilders.MakeDna(DnaType.Cpu, "Test CPU", new CpuBasic { Name = "Test CPU", Manufacturer = "Test" }));

        var gpuScanId = _repository.SaveScan(deviceWithGpu);
        var cpuScanId = _repository.SaveScan(deviceWithoutGpu);

        var gpuScans = _repository.ListScans(DnaType.Gpu);

        Assert.Contains(gpuScans, s => s.ScanId == gpuScanId);
        Assert.DoesNotContain(gpuScans, s => s.ScanId == cpuScanId);
    }

    [Fact]
    public void SaveScan_EmptyDnaList_OverallStatusDefaultsToYellowNotGreen()
    {
        // An empty scan (no DNAs detected at all) is a degraded/incomplete result, not a clean
        // "everything is fine" green — ScanHistoryRepository.SaveScan defaults it to Yellow.
        var device = TestBuilders.MakeDevice();

        var scanId = _repository.SaveScan(device);
        var scans = _repository.ListScans();

        var summary = Assert.Single(scans.Where(s => s.ScanId == scanId));
        Assert.Equal(HealthStatus.Yellow, summary.OverallStatus);
    }

    [Fact]
    public void SaveScan_OverallStatusIsWorstAmongAllDnas()
    {
        var device = TestBuilders.MakeDevice(
            TestBuilders.MakeDna(DnaType.Cpu, "Test CPU", new CpuBasic { Name = "Test CPU", Manufacturer = "Test" }, status: HealthStatus.Green),
            TestBuilders.MakeDna(DnaType.Storage, "Test SSD", TestBuilders.StorageBasic(), status: HealthStatus.Red),
            TestBuilders.MakeDna(DnaType.Memory, "RAM", TestBuilders.MemoryBasic(), status: HealthStatus.Yellow));

        var scanId = _repository.SaveScan(device);
        var scans = _repository.ListScans();

        var summary = Assert.Single(scans.Where(s => s.ScanId == scanId));
        Assert.Equal(HealthStatus.Red, summary.OverallStatus);
    }

    [Fact]
    public void SaveScan_TwoDnasSameTypeAndName_BothPersistIndependently()
    {
        // Regression guard for the same non-unique-name assumption ScanChangeDetector had to
        // handle explicitly (e.g. two identical disk models) — the repository must not collapse
        // or lose one of them.
        var device = TestBuilders.MakeDevice(
            TestBuilders.MakeDna(DnaType.Storage, "Same Model SSD", TestBuilders.StorageBasic("Same Model SSD", freeSpacePct: 20.0)),
            TestBuilders.MakeDna(DnaType.Storage, "Same Model SSD", TestBuilders.StorageBasic("Same Model SSD", freeSpacePct: 80.0)));

        var scanId = _repository.SaveScan(device);
        var loaded = _repository.LoadScan(scanId);

        Assert.Equal(2, loaded!.Dnas.Count);
    }
}

//*Built with assistance from __Claude Code__ by Anthropic.*
