//
// DeviceDNA
// Author:  nobody174 (nobodylearn174@gmail.com)
// Repo:    https://github.com/nobody174/DeviceDNA
// Patreon: https://www.patreon.com/c/Nobody174
// License: All rights reserved (c) 2026 nobody174
// "It's never too late to give up!"
//

using DeviceDNA.DetectionEngine.Rules;
using DeviceDNA.Model;
using DeviceDNA.Model.Dnas;

namespace DeviceDNA.Tests;

// Covers RulesEngine's deterministic pass/fail logic (CLAUDE.md: "Rules are deterministic, not
// AI-generated" — this is exactly the surface that must never regress silently). Each health rule
// gets a green/yellow/red case plus the "missing data means skip, not guess" cases, since that
// no-fabrication behavior (REQUIREMENTS.md section 10) is easy to accidentally break by adding a
// naive threshold check that doesn't first test for HasValue.
public class RulesEngineTests
{
    // ---------- StatusFromReasons ----------

    [Fact]
    public void StatusFromReasons_AllInfo_ReturnsGreen()
    {
        var reasons = new List<StatusReason>
        {
            new() { Message = "a", Severity = ReasonSeverity.Info, Confidence = Confidence.High },
        };

        Assert.Equal(HealthStatus.Green, RulesEngine.StatusFromReasons(reasons));
    }

    [Fact]
    public void StatusFromReasons_HasYellow_ReturnsYellow()
    {
        var reasons = new List<StatusReason>
        {
            new() { Message = "a", Severity = ReasonSeverity.Info, Confidence = Confidence.High },
            new() { Message = "b", Severity = ReasonSeverity.Yellow, Confidence = Confidence.Medium },
        };

        Assert.Equal(HealthStatus.Yellow, RulesEngine.StatusFromReasons(reasons));
    }

    [Fact]
    public void StatusFromReasons_HasRedAndYellow_ReturnsRed()
    {
        var reasons = new List<StatusReason>
        {
            new() { Message = "a", Severity = ReasonSeverity.Yellow, Confidence = Confidence.Medium },
            new() { Message = "b", Severity = ReasonSeverity.Red, Confidence = Confidence.High },
        };

        Assert.Equal(HealthStatus.Red, RulesEngine.StatusFromReasons(reasons));
    }

    // ---------- CPU ----------

    [Theory]
    [InlineData(70.0, ReasonSeverity.Info)]
    [InlineData(80.0, ReasonSeverity.Yellow)]
    [InlineData(89.9, ReasonSeverity.Yellow)]
    [InlineData(90.0, ReasonSeverity.Red)]
    [InlineData(95.0, ReasonSeverity.Red)]
    public void EvaluateCpu_Temperature_CrossesThresholdsCorrectly(double tempC, ReasonSeverity expectedSeverity)
    {
        var advanced = new CpuAdvanced { CurrentTempC = tempC };

        var reasons = RulesEngine.EvaluateCpu(advanced);

        Assert.Contains(reasons, r => r.Severity == expectedSeverity);
    }

    [Fact]
    public void EvaluateCpu_TempExactlyZero_TreatedAsImplausibleAndSkipped()
    {
        // LibreHardwareMonitor without admin privileges reports a literal 0 rather than omitting
        // the field — RulesEngine must not report "CPU is a perfectly healthy 0 degrees."
        var advanced = new CpuAdvanced { CurrentTempC = 0.0 };

        var reasons = RulesEngine.EvaluateCpu(advanced);

        Assert.DoesNotContain(reasons, r => r.Message.Contains("temperature"));
    }

    [Fact]
    public void EvaluateCpu_TempNull_SkipsTemperatureCheckEntirely()
    {
        var advanced = new CpuAdvanced { CurrentTempC = null };

        var reasons = RulesEngine.EvaluateCpu(advanced);

        Assert.DoesNotContain(reasons, r => r.Message.Contains("temperature"));
    }

    [Fact]
    public void EvaluateCpu_HighUtilization_FiresYellow()
    {
        var advanced = new CpuAdvanced { CurrentUtilizationPct = 95.0 };

        var reasons = RulesEngine.EvaluateCpu(advanced);

        Assert.Contains(reasons, r => r.Severity == ReasonSeverity.Yellow && r.Message.Contains("utilization"));
    }

    [Fact]
    public void EvaluateCpu_VirtualizationDisabled_FiresYellow()
    {
        var advanced = new CpuAdvanced { VirtualizationSupport = false };

        var reasons = RulesEngine.EvaluateCpu(advanced);

        Assert.Contains(reasons, r => r.Severity == ReasonSeverity.Yellow && r.Message.Contains("Virtualization"));
    }

    [Fact]
    public void EvaluateCpu_VirtualizationEnabled_FiresInfoOnly()
    {
        var advanced = new CpuAdvanced { VirtualizationSupport = true };

        var reasons = RulesEngine.EvaluateCpu(advanced);

        Assert.DoesNotContain(reasons, r => r.Severity != ReasonSeverity.Info);
    }

    [Fact]
    public void EvaluateCpu_AllFieldsNull_StillReturnsAtLeastOneConfirmedGoodReason()
    {
        // Product requirement (CLAUDE.md Rules Engine notes): every DNA must surface at least one
        // Info-severity reason, never silence, even with zero usable data.
        var advanced = new CpuAdvanced();

        var reasons = RulesEngine.EvaluateCpu(advanced);

        Assert.NotEmpty(reasons);
        Assert.All(reasons, r => Assert.Equal(ReasonSeverity.Info, r.Severity));
    }

    // ---------- GPU ----------

    [Theory]
    [InlineData(70.0, ReasonSeverity.Info)]
    [InlineData(80.0, ReasonSeverity.Yellow)]
    [InlineData(87.0, ReasonSeverity.Red)]
    public void EvaluateGpu_Temperature_CrossesThresholdsCorrectly(double tempC, ReasonSeverity expectedSeverity)
    {
        var advanced = new GpuAdvanced { CurrentTempC = tempC };

        var reasons = RulesEngine.EvaluateGpu(advanced);

        Assert.Contains(reasons, r => r.Severity == expectedSeverity);
    }

    [Fact]
    public void EvaluateGpu_TempZero_TreatedAsImplausibleAndSkipped()
    {
        var advanced = new GpuAdvanced { CurrentTempC = 0.0 };

        var reasons = RulesEngine.EvaluateGpu(advanced);

        Assert.DoesNotContain(reasons, r => r.Message.Contains("temperature"));
    }

    [Fact]
    public void EvaluateGpu_NoData_StillReturnsConfirmedGoodReason()
    {
        var advanced = new GpuAdvanced();

        var reasons = RulesEngine.EvaluateGpu(advanced);

        Assert.NotEmpty(reasons);
        Assert.All(reasons, r => Assert.Equal(ReasonSeverity.Info, r.Severity));
    }

    // ---------- Memory ----------

    [Fact]
    public void EvaluateMemory_ActualBelowRated_FiresYellow()
    {
        var advanced = new MemoryAdvanced { RatedSpeedMts = 3600, ActualSpeedMts = 2933 };

        var reasons = RulesEngine.EvaluateMemory(advanced);

        Assert.Contains(reasons, r => r.Severity == ReasonSeverity.Yellow && r.Message.Contains("rated"));
    }

    [Fact]
    public void EvaluateMemory_ActualMeetsRated_FiresInfoOnly()
    {
        var advanced = new MemoryAdvanced { RatedSpeedMts = 3200, ActualSpeedMts = 3200 };

        var reasons = RulesEngine.EvaluateMemory(advanced);

        Assert.DoesNotContain(reasons, r => r.Severity != ReasonSeverity.Info);
    }

    [Fact]
    public void EvaluateMemory_RatedOrActualMissing_SkipsSpeedCheck()
    {
        var advanced = new MemoryAdvanced { RatedSpeedMts = 3200, ActualSpeedMts = null };

        var reasons = RulesEngine.EvaluateMemory(advanced);

        Assert.DoesNotContain(reasons, r => r.Message.Contains("rated"));
    }

    [Fact]
    public void EvaluateMemory_MismatchedManufacturers_FiresYellow()
    {
        var advanced = new MemoryAdvanced
        {
            PerModule = new List<MemoryModule>
            {
                new() { SizeGb = 8, Manufacturer = "Corsair" },
                new() { SizeGb = 8, Manufacturer = "Kingston" },
            },
        };

        var reasons = RulesEngine.EvaluateMemory(advanced);

        Assert.Contains(reasons, r => r.Severity == ReasonSeverity.Yellow && r.Message.Contains("do not match"));
    }

    [Fact]
    public void EvaluateMemory_MismatchedSizes_FiresYellow()
    {
        var advanced = new MemoryAdvanced
        {
            PerModule = new List<MemoryModule>
            {
                new() { SizeGb = 8, Manufacturer = "Corsair" },
                new() { SizeGb = 16, Manufacturer = "Corsair" },
            },
        };

        var reasons = RulesEngine.EvaluateMemory(advanced);

        Assert.Contains(reasons, r => r.Severity == ReasonSeverity.Yellow && r.Message.Contains("do not match"));
    }

    [Fact]
    public void EvaluateMemory_MatchedModules_FiresInfo()
    {
        var advanced = new MemoryAdvanced
        {
            PerModule = new List<MemoryModule>
            {
                new() { SizeGb = 16, Manufacturer = "Corsair" },
                new() { SizeGb = 16, Manufacturer = "Corsair" },
            },
        };

        var reasons = RulesEngine.EvaluateMemory(advanced);

        Assert.Contains(reasons, r => r.Severity == ReasonSeverity.Info && r.Message.Contains("match"));
    }

    [Fact]
    public void EvaluateMemory_OneModuleSizeUnknown_DoesNotFabricateSizeMismatch()
    {
        // Regression test for the real bug documented in CLAUDE.md/BACKLOG.md: a null module size
        // must never be silently treated as "0 GB" and compared as if it were a genuine distinct
        // size, since that fabricates a mismatch finding (or masks a real one) from missing data.
        var advanced = new MemoryAdvanced
        {
            PerModule = new List<MemoryModule>
            {
                new() { SizeGb = 16, Manufacturer = "Corsair" },
                new() { SizeGb = null, Manufacturer = "Corsair" },
            },
        };

        var reasons = RulesEngine.EvaluateMemory(advanced);

        Assert.DoesNotContain(reasons, r => r.Message.Contains("do not match"));
    }

    [Fact]
    public void EvaluateMemory_SingleModule_SkipsMismatchCheck()
    {
        var advanced = new MemoryAdvanced
        {
            PerModule = new List<MemoryModule> { new() { SizeGb = 16, Manufacturer = "Corsair" } },
        };

        var reasons = RulesEngine.EvaluateMemory(advanced);

        Assert.DoesNotContain(reasons, r => r.Message.Contains("do not match") || r.Message.Contains("modules match"));
    }

    [Fact]
    public void EvaluateMemory_NoData_StillReturnsConfirmedGoodReason()
    {
        var advanced = new MemoryAdvanced();

        var reasons = RulesEngine.EvaluateMemory(advanced);

        Assert.NotEmpty(reasons);
        Assert.All(reasons, r => Assert.Equal(ReasonSeverity.Info, r.Severity));
    }

    // ---------- Storage ----------

    [Fact]
    public void EvaluateStorage_LowFreeSpace_FiresYellow()
    {
        var basic = TestBuilders.StorageBasic(freeSpacePct: 10.0);
        var advanced = new StorageAdvanced
        {
            Partitions = new List<StoragePartition> { new() { DriveLetter = "C", FreeSpacePct = 10.0 } },
        };

        var reasons = RulesEngine.EvaluateStorage(basic, advanced);

        Assert.Contains(reasons, r => r.Severity == ReasonSeverity.Yellow && r.Message.Contains("free space"));
    }

    [Fact]
    public void EvaluateStorage_PlentyOfFreeSpace_FiresInfo()
    {
        var basic = TestBuilders.StorageBasic(freeSpacePct: 80.0);
        var advanced = new StorageAdvanced
        {
            Partitions = new List<StoragePartition> { new() { DriveLetter = "C", FreeSpacePct = 80.0 } },
        };

        var reasons = RulesEngine.EvaluateStorage(basic, advanced);

        Assert.Contains(reasons, r => r.Severity == ReasonSeverity.Info && r.Message.Contains("free space"));
    }

    [Fact]
    public void EvaluateStorage_NoPartitionsResolved_SkipsFreeSpaceCheckRatherThanFabricatingZero()
    {
        // FreeSpacePct defaults to 0 both when genuinely full AND when unresolved — must not be
        // evaluated when Partitions is empty, since that would report a false "low space" warning.
        var basic = TestBuilders.StorageBasic(freeSpacePct: 0.0);
        var advanced = new StorageAdvanced { Partitions = new List<StoragePartition>() };

        var reasons = RulesEngine.EvaluateStorage(basic, advanced);

        Assert.DoesNotContain(reasons, r => r.Message.Contains("free space"));
    }

    [Fact]
    public void EvaluateStorage_SmartHealthZero_FiresRed()
    {
        var basic = TestBuilders.StorageBasic();
        var advanced = new StorageAdvanced { SmartHealthPct = 0 };

        var reasons = RulesEngine.EvaluateStorage(basic, advanced);

        Assert.Contains(reasons, r => r.Severity == ReasonSeverity.Red && r.Message.Contains("failure"));
    }

    [Fact]
    public void EvaluateStorage_SmartHealthPassed_FiresInfo()
    {
        var basic = TestBuilders.StorageBasic();
        var advanced = new StorageAdvanced { SmartHealthPct = 100 };

        var reasons = RulesEngine.EvaluateStorage(basic, advanced);

        Assert.Contains(reasons, r => r.Severity == ReasonSeverity.Info && r.Message.Contains("SMART"));
    }

    [Fact]
    public void EvaluateStorage_NoData_StillReturnsConfirmedGoodReason()
    {
        var basic = TestBuilders.StorageBasic();
        var advanced = new StorageAdvanced();

        var reasons = RulesEngine.EvaluateStorage(basic, advanced);

        Assert.NotEmpty(reasons);
        Assert.All(reasons, r => Assert.Equal(ReasonSeverity.Info, r.Severity));
    }

    // ---------- Motherboard ----------

    [Fact]
    public void EvaluateMotherboard_UnderNegotiatedPcieSlot_FiresYellow()
    {
        var advanced = new MotherboardAdvanced
        {
            PcieSlots = new List<PcieSlot>
            {
                new() { Generation = 4, PhysicalWidth = 16, NegotiatedWidth = 8, InUse = true, PopulatedBy = "GPU" },
            },
        };

        var reasons = RulesEngine.EvaluateMotherboard(advanced);

        Assert.Contains(reasons, r => r.Severity == ReasonSeverity.Yellow && r.Message.Contains("negotiating"));
    }

    [Fact]
    public void EvaluateMotherboard_FullWidthNegotiation_FiresInfo()
    {
        var advanced = new MotherboardAdvanced
        {
            PcieSlots = new List<PcieSlot>
            {
                new() { Generation = 4, PhysicalWidth = 16, NegotiatedWidth = 16, InUse = true, PopulatedBy = "GPU" },
            },
        };

        var reasons = RulesEngine.EvaluateMotherboard(advanced);

        Assert.Contains(reasons, r => r.Severity == ReasonSeverity.Info && r.Message.Contains("full physical width"));
    }

    [Fact]
    public void EvaluateMotherboard_SlotNotInUse_IgnoredEvenIfWidthLooksUnderNegotiated()
    {
        var advanced = new MotherboardAdvanced
        {
            PcieSlots = new List<PcieSlot>
            {
                new() { Generation = 4, PhysicalWidth = 16, NegotiatedWidth = 1, InUse = false, PopulatedBy = null },
            },
        };

        var reasons = RulesEngine.EvaluateMotherboard(advanced);

        Assert.Contains(reasons, r => r.Severity == ReasonSeverity.Info);
        Assert.DoesNotContain(reasons, r => r.Severity == ReasonSeverity.Yellow);
    }

    [Fact]
    public void EvaluateMotherboard_NoPcieSlotData_StillReturnsConfirmedGoodReason()
    {
        var advanced = new MotherboardAdvanced();

        var reasons = RulesEngine.EvaluateMotherboard(advanced);

        Assert.NotEmpty(reasons);
        Assert.All(reasons, r => Assert.Equal(ReasonSeverity.Info, r.Severity));
    }

    // ---------- Network ----------

    [Fact]
    public void EvaluateNetworkConnected_BelowMaxSpeed_FiresYellow()
    {
        var basic = TestBuilders.NetworkBasic(currentSpeedMbps: 100);
        var advanced = new NetworkAdvanced { MaxSupportedSpeedMbps = 1000 };

        var reasons = RulesEngine.EvaluateNetworkConnected(basic, advanced);

        Assert.Contains(reasons, r => r.Severity == ReasonSeverity.Yellow && r.Message.Contains("below"));
    }

    [Fact]
    public void EvaluateNetworkConnected_AtMaxSpeed_FiresInfo()
    {
        var basic = TestBuilders.NetworkBasic(currentSpeedMbps: 1000);
        var advanced = new NetworkAdvanced { MaxSupportedSpeedMbps = 1000 };

        var reasons = RulesEngine.EvaluateNetworkConnected(basic, advanced);

        Assert.Contains(reasons, r => r.Severity == ReasonSeverity.Info && r.Message.Contains("maximum"));
    }

    [Fact]
    public void EvaluateNetworkConnected_NoData_StillReturnsConfirmedGoodReason()
    {
        var basic = TestBuilders.NetworkBasic(currentSpeedMbps: null);
        var advanced = new NetworkAdvanced();

        var reasons = RulesEngine.EvaluateNetworkConnected(basic, advanced);

        Assert.NotEmpty(reasons);
        Assert.All(reasons, r => Assert.Equal(ReasonSeverity.Info, r.Severity));
    }

    // ---------- OS ----------

    [Fact]
    public void EvaluateOs_Unlicensed_FiresRed()
    {
        var advanced = new OsAdvanced { ActivationStatus = "Unlicensed" };

        var reasons = RulesEngine.EvaluateOs(advanced);

        Assert.Contains(reasons, r => r.Severity == ReasonSeverity.Red && r.Message.Contains("not activated"));
    }

    [Fact]
    public void EvaluateOs_Licensed_FiresInfoNotRed()
    {
        var advanced = new OsAdvanced { ActivationStatus = "Licensed" };

        var reasons = RulesEngine.EvaluateOs(advanced);

        Assert.Contains(reasons, r => r.Severity == ReasonSeverity.Info);
        Assert.DoesNotContain(reasons, r => r.Severity == ReasonSeverity.Red);
    }

    [Fact]
    public void EvaluateOs_GracePeriodState_TreatedAsInfoNotRed()
    {
        // A non-"Unlicensed", non-null activation state (e.g. a grace period) must not be judged
        // red — the app can't distinguish a benign transient grace state from a real problem, so
        // guessing "red" would be a false alarm the user can't act on.
        var advanced = new OsAdvanced { ActivationStatus = "Notification" };

        var reasons = RulesEngine.EvaluateOs(advanced);

        Assert.DoesNotContain(reasons, r => r.Severity == ReasonSeverity.Red);
    }

    [Fact]
    public void EvaluateOs_RebootPendingTrue_FiresYellow()
    {
        var advanced = new OsAdvanced { RebootPending = true };

        var reasons = RulesEngine.EvaluateOs(advanced);

        Assert.Contains(reasons, r => r.Severity == ReasonSeverity.Yellow && r.Message.Contains("restart"));
    }

    [Fact]
    public void EvaluateOs_RebootPendingNull_DoesNotFireRebootWarning()
    {
        // Null means "could not be determined" and must not be treated as false (no warning) or
        // true (a false warning) — it must simply not fire.
        var advanced = new OsAdvanced { RebootPending = null };

        var reasons = RulesEngine.EvaluateOs(advanced);

        Assert.DoesNotContain(reasons, r => r.Message.Contains("restart"));
    }

    [Fact]
    public void EvaluateOs_NoData_StillReturnsConfirmedGoodReason()
    {
        var advanced = new OsAdvanced();

        var reasons = RulesEngine.EvaluateOs(advanced);

        Assert.NotEmpty(reasons);
        Assert.All(reasons, r => Assert.Equal(ReasonSeverity.Info, r.Severity));
    }
}

//*Built with assistance from __Claude Code__ by Anthropic.*
