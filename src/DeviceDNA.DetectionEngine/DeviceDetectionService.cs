//
// DeviceDNA
// Author:  nobody174 (nobodylearn174@gmail.com)
// Repo:    https://github.com/nobody174/DeviceDNA
// Patreon: https://www.patreon.com/c/Nobody174
// License: All rights reserved (c) 2026 nobody174
// "It's never too late to give up!"
//

using System.Globalization;
using System.Runtime.Versioning;
using DeviceDNA.DetectionEngine.Rules;
using DeviceDNA.Model;
using DeviceDNA.Model.Dnas;
using DeviceDNA.PlatformAdapters;

namespace DeviceDNA.DetectionEngine;

// Orchestrates both Platform Adapters (WMI static inventory + LibreHardwareMonitor live sensors),
// maps their raw output onto the normalized DeviceDNA.Model types, and assembles a Device snapshot.
// This is the only layer allowed to translate raw adapter data into the Model — UI/Application never
// touch adapters directly (see CLAUDE.md architecture layering).
//
// Phase 3: Status/StatusReasons are computed by DeviceDNA.DetectionEngine.Rules.RulesEngine, a
// deterministic (non-AI) rules evaluator — see CLAUDE.md Rules Engine Implementation Notes. Fields
// that cannot be reliably sourced are left null rather than fabricated, per REQUIREMENTS.md section 10.
public class DeviceDetectionService
{
    private readonly ILibreHardwareMonitorAdapter _sensorAdapter;
    private readonly IWmiAdapter _wmiAdapter;
    private readonly IRegistryAdapter _registryAdapter;

    public DeviceDetectionService(ILibreHardwareMonitorAdapter sensorAdapter, IWmiAdapter wmiAdapter, IRegistryAdapter registryAdapter)
    {
        _sensorAdapter = sensorAdapter;
        _wmiAdapter = wmiAdapter;
        _registryAdapter = registryAdapter;
    }

    // Convenience factory wiring the concrete Windows Platform Adapters. Keeps knowledge of the
    // concrete adapter types inside the Detection Engine/Platform Adapters layers, so the
    // Application layer only needs to reference DetectionEngine, not PlatformAdapters directly.
    public static DeviceDetectionService CreateDefault()
    {
        // WmiAdapter/RegistryAdapter are Windows-only. DeviceDNA v1 is Windows-only end to end
        // (REQUIREMENTS.md section 11), so this is safe; the pragma suppresses the CA1416
        // platform-compatibility warning that would otherwise propagate up through every caller.
#pragma warning disable CA1416
        return new(new LibreHardwareMonitorAdapter(), new WmiAdapter(), new RegistryAdapter());
#pragma warning restore CA1416
    }

    public Device DetectDevice()
    {
        var sensors = _sensorAdapter.ReadAllSensors();
        var now = DateTime.UtcNow;

        var dnas = new List<Dna>();

        TryAdd(dnas, () => DetectCpu(sensors, now));
        TryAdd(dnas, () => DetectGpu(sensors, now));
        TryAdd(dnas, () => DetectMemory(sensors, now));
        dnas.AddRange(TryAddRange(() => DetectStorage(sensors, now)));
        TryAdd(dnas, () => DetectMotherboard(sensors, now));
        var motherboardVendorUrl = (dnas.FirstOrDefault(d => d.Type == DnaType.Motherboard)?.Basic as MotherboardBasic)?.VendorSupportUrl;
        dnas.AddRange(TryAddRange(() => DetectNetwork(now, motherboardVendorUrl)));
        TryAdd(dnas, () => DetectOs(now));

        var hostname = Environment.MachineName;
        var osName = dnas.FirstOrDefault(d => d.Type == DnaType.Os)?.Name ?? "Unknown OS";

        return new Device
        {
            Id = Guid.NewGuid().ToString(),
            Hostname = hostname,
            OsSummary = osName,
            FormFactor = DetectFormFactor(),
            Dnas = dnas,
        };
    }

    // Win32_SystemEnclosure.ChassisTypes is a documented WMI enum that reliably distinguishes
    // desktop/laptop/server chassis on real hardware. Previously this hardcoded "Desktop"
    // unconditionally (including on laptops) — a specific, confident-sounding guess presented as
    // fact, which is exactly the kind of fabrication REQUIREMENTS.md section 10 forbids. "Unknown"
    // is the honest fallback when the chassis type is absent/unrecognized, matching the pattern
    // already used elsewhere (e.g. Storage Manufacturer defaulting to "Unknown").
    private string DetectFormFactor()
    {
        var rows = _wmiAdapter.Query("Win32_SystemEnclosure");
        var row = rows.FirstOrDefault();
        if (row == null)
        {
            return "Unknown";
        }

        var chassisTypes = GetIntArray(row, "ChassisTypes");
        var chassisType = chassisTypes?.FirstOrDefault();
        return chassisType switch
        {
            3 or 4 or 5 or 6 or 7 or 15 or 16 => "Desktop", // Desktop, Low Profile Desktop, Pizza Box, Mini Tower, Tower, All in One, Compact PCI
            8 or 9 or 10 or 14 or 30 or 31 or 32 => "Laptop", // Portable, Laptop, Notebook, Sub Notebook, Tablet, Convertible, Detachable
            17 or 23 => "Server", // Main Server Chassis, Rack Mount Chassis
            21 => "Handheld", // Handheld
            _ => "Unknown",
        };
    }

    // A single malformed WMI property value (e.g. an unexpected type/format for one field on one
    // machine) would otherwise throw from inside a Convert.To* call in one DetectX method and take
    // down the entire scan — no other DNA would be detected either. The Platform Adapters already
    // degrade gracefully on their own failures (see LibreHardwareMonitorAdapter/WmiAdapter); this
    // mirrors that same defensive posture at the per-DNA level so one bad value doesn't cost the
    // other six DNAs.
    //
    // Catches Exception broadly rather than an enumerated list of specific types: an explicit list
    // (previously FormatException/InvalidCastException/OverflowException) silently under-covers as
    // the code evolves — confirmed by research that WmiAdapter.Query() already swallows every WMI
    // exception internally (so ManagementException was never actually reachable here), while a real,
    // if currently-latent, NullReferenceException risk exists from null-forgiving operators like
    // GetLong(...)!.Value in DetectMotherboard/DetectMemory. Every branch here handles all exception
    // types identically (skip this DNA, move on) — the textbook case for broad-catch, not a narrower
    // filter. Debug.WriteLine keeps a real bug visible during development without crashing in
    // production; this app has no logging infrastructure, so this is the lightweight equivalent.
    private static void TryAdd(List<Dna> list, Func<Dna?> factory)
    {
        try
        {
            var dna = factory();
            if (dna != null)
            {
                list.Add(dna);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"DeviceDetectionService: DNA detection failed and was skipped. {ex}");
        }
    }

    private static List<Dna> TryAddRange(Func<List<Dna>> factory)
    {
        try
        {
            return factory();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"DeviceDetectionService: DNA detection failed and was skipped. {ex}");
            return new List<Dna>();
        }
    }

    // ---------- CPU ----------

    private Dna? DetectCpu(IReadOnlyList<RawSensorReading> sensors, DateTime now)
    {
        var rows = _wmiAdapter.Query("Win32_Processor");
        var row = rows.FirstOrDefault();
        if (row == null)
        {
            return null;
        }

        var name = GetString(row, "Name")?.Trim() ?? "Unknown CPU";
        var manufacturer = GetString(row, "Manufacturer") ?? "Unknown";
        var cores = GetInt(row, "NumberOfCores") ?? 0;
        var threads = GetInt(row, "NumberOfLogicalProcessors") ?? 0;
        var baseClockMhz = GetDouble(row, "MaxClockSpeed");

        var cpuSensors = sensors.Where(s => s.HardwareType == "Cpu").ToList();
        var tempReading = cpuSensors.FirstOrDefault(s => s.SensorType == "Temperature" &&
            (s.SensorName.Contains("Package", StringComparison.OrdinalIgnoreCase) || s.SensorName.Contains("Average", StringComparison.OrdinalIgnoreCase)))
            ?? cpuSensors.FirstOrDefault(s => s.SensorType == "Temperature");
        var loadReading = cpuSensors.FirstOrDefault(s => s.SensorType == "Load" &&
            s.SensorName.Contains("Total", StringComparison.OrdinalIgnoreCase))
            ?? cpuSensors.FirstOrDefault(s => s.SensorType == "Load");
        // Excludes readings with no Value (a real possibility per LibreHardwareMonitorAdapter's own
        // degraded-access behavior) rather than coercing a missing reading to 0 — a per-core load or
        // live clock of literally 0 is a plausible real value, so treating "no data" as "0" would
        // fabricate a specific number where the honest answer is "unknown for this entry".
        var clockReadings = cpuSensors.Where(s => s.SensorType == "Clock" && s.Value.HasValue).ToList();
        var perCoreLoad = cpuSensors
            .Where(s => s.SensorType == "Load" && s.SensorName.Contains("Core", StringComparison.OrdinalIgnoreCase) && s.Value.HasValue)
            .Select(s => (double)s.Value!.Value)
            .ToList();

        var basic = new CpuBasic
        {
            Name = name,
            Manufacturer = manufacturer,
            Cores = cores,
            Threads = threads,
            BaseClockGhz = baseClockMhz.HasValue ? Math.Round(baseClockMhz.Value / 1000.0, 2) : null,
            VendorSupportUrl = BuildCpuVendorUrl(manufacturer),
            BenchmarkUrl = BuildCpuBenchmarkUrl(name),
        };

        var virtualization = GetBool(row, "VirtualizationFirmwareEnabled");
        var powerMode = GetActivePowerPlanName();

        var advanced = new CpuAdvanced
        {
            ArchitectureGeneration = GetString(row, "Description"),
            Socket = GetString(row, "SocketDesignation"),
            BoostClockGhz = null, // Not reliably available from WMI/LHM without model-specific lookup; left null.
            Cache = new CpuCache
            {
                L1Kb = null,
                L2Kb = GetInt(row, "L2CacheSize"),
                L3Kb = GetInt(row, "L3CacheSize"),
            },
            TdpWatts = null,
            CurrentTempC = tempReading?.Value,
            CurrentUtilizationPct = loadReading?.Value,
            CurrentLiveClockGhz = clockReadings.Count > 0 ? Math.Round(clockReadings.Max(s => s.Value!.Value) / 1000.0, 2) : null,
            PerCoreLoadPct = perCoreLoad.Count > 0 ? perCoreLoad : null,
            VirtualizationSupport = virtualization,
            PowerMode = powerMode,
        };

        var reasons = RulesEngine.EvaluateCpu(advanced);
        var status = RulesEngine.StatusFromReasons(reasons);
        var summary = $"{name} — {cores} cores, {threads} threads, {SummarySuffix(status, reasons)}";

        return new Dna
        {
            Id = Guid.NewGuid().ToString(),
            Type = DnaType.Cpu,
            Name = name,
            Manufacturer = manufacturer,
            Summary = summary,
            Status = status,
            StatusReasons = reasons,
            Basic = basic,
            Advanced = advanced,
            Driver = DriverInfo.NotApplicable,
            LastUpdated = now,
        };
    }

    // ---------- GPU ----------

    private Dna? DetectGpu(IReadOnlyList<RawSensorReading> sensors, DateTime now)
    {
        var rows = _wmiAdapter.Query("Win32_VideoController");
        var row = rows.FirstOrDefault();
        if (row == null)
        {
            return null;
        }

        var name = GetString(row, "Name")?.Trim() ?? "Unknown GPU";
        var manufacturer = GetString(row, "AdapterCompatibility") ?? "Unknown";
        var vramBytes = GetLong(row, "AdapterRAM");
        var driverVersion = GetString(row, "DriverVersion");
        var driverDateRaw = GetString(row, "DriverDate");

        var gpuSensors = sensors.Where(s =>
            s.HardwareType.StartsWith("Gpu", StringComparison.OrdinalIgnoreCase)).ToList();
        var tempReading = gpuSensors.FirstOrDefault(s => s.SensorType == "Temperature");
        var loadReading = gpuSensors.FirstOrDefault(s => s.SensorType == "Load" &&
            s.SensorName.Contains("Core", StringComparison.OrdinalIgnoreCase))
            ?? gpuSensors.FirstOrDefault(s => s.SensorType == "Load");
        var coreClock = gpuSensors.FirstOrDefault(s => s.SensorType == "Clock" &&
            s.SensorName.Contains("Core", StringComparison.OrdinalIgnoreCase));
        var memoryClock = gpuSensors.FirstOrDefault(s => s.SensorType == "Clock" &&
            s.SensorName.Contains("Memory", StringComparison.OrdinalIgnoreCase));
        var vramUsed = gpuSensors.FirstOrDefault(s => s.SensorType == "Data" &&
            s.SensorName.Contains("Used", StringComparison.OrdinalIgnoreCase));

        var basic = new GpuBasic
        {
            Name = name,
            Manufacturer = manufacturer,
            VramAmountGb = vramBytes.HasValue ? Math.Round(vramBytes.Value / 1024.0 / 1024.0 / 1024.0, 1) : null,
            DriverVersion = driverVersion,
        };

        var advanced = new GpuAdvanced
        {
            CoreClockMhz = coreClock?.Value,
            BoostClockMhz = null,
            MemoryType = null,
            MemoryClockMhz = memoryClock?.Value,
            PcieGeneration = null,
            PcieLaneWidth = null,
            CurrentTempC = tempReading?.Value,
            CurrentUtilizationPct = loadReading?.Value,
            CurrentVramUsageGb = vramUsed?.Value != null ? Math.Round(vramUsed.Value.Value / 1024.0, 1) : null,
            DriverDate = ParseWmiDate(driverDateRaw),
            ConnectedOutputsActive = null,
        };

        var reasons = RulesEngine.EvaluateGpu(advanced);
        var status = RulesEngine.StatusFromReasons(reasons);
        var vramLabel = basic.VramAmountGb.HasValue ? $"{basic.VramAmountGb.Value} GB VRAM" : "unknown VRAM";
        var summary = $"{name} — {vramLabel}, {SummarySuffix(status, reasons)}";

        return new Dna
        {
            Id = Guid.NewGuid().ToString(),
            Type = DnaType.Gpu,
            Name = name,
            Manufacturer = manufacturer,
            Summary = summary,
            Status = status,
            StatusReasons = reasons,
            Basic = basic,
            Advanced = advanced,
            Driver = new DriverInfo
            {
                IsApplicable = true,
                Version = driverVersion,
                Date = advanced.DriverDate,
                SourceUrl = BuildGpuVendorUrl(manufacturer, name),
            },
            LastUpdated = now,
        };
    }

    // Links out to the GPU vendor's official driver page — the user clicks through and checks
    // themselves whether a newer driver exists (REQUIREMENTS.md section 10, clarified 2026-08-15:
    // online lookups are wanted, but this app never itself queries a vendor's site to determine
    // driver freshness — that would mean either an undocumented/ToS-adjacent live API call, or a
    // scraper that silently breaks and potentially misreports on any vendor site redesign; neither
    // is acceptable here). This method only ever constructs a URL, never fetches one.
    //
    // AMD publishes a stable, human-readable slug-based URL scheme for its driver pages (confirmed
    // via research) — buildable directly from the GPU name with simple normalization. NVIDIA and
    // Intel don't expose an equivalent stable per-model URL pattern without either an undocumented
    // lookup API or guessing, so those link to the vendor's official top-level driver
    // download/support page instead — a real, stable, always-correct URL, just not pre-filtered to
    // the exact model. This is honest: it's a confidently-correct link, never a guessed one.
    // Parses the trailing integer segment from a LibreHardwareMonitor storage Identifier string, e.g.
    // "/storage/nvme/0" -> 0, "/storage/ata/1" -> 1. Returns null if the identifier doesn't end in a
    // parseable integer (a format LHM hasn't been observed to produce for storage hardware, but
    // handled defensively rather than assumed).
    private static int? ParseTrailingIndex(string identifier)
    {
        var lastSegment = identifier.Split('/').LastOrDefault();
        return int.TryParse(lastSegment, out var index) ? index : null;
    }

    private static string? BuildGpuVendorUrl(string manufacturer, string gpuName)
    {
        if (manufacturer.Contains("AMD", StringComparison.OrdinalIgnoreCase) ||
            manufacturer.Contains("Advanced Micro Devices", StringComparison.OrdinalIgnoreCase))
        {
            // e.g. "AMD Radeon RX 9070 XT" -> "amd-radeon-rx-9070-xt"; falls back to the general
            // driver page if the constructed slug doesn't resolve (not verified here — that would
            // require a network call this method deliberately never makes).
            var slug = gpuName
                .Replace("AMD", "", StringComparison.OrdinalIgnoreCase)
                .Trim()
                .ToLowerInvariant()
                .Replace(" ", "-");
            return string.IsNullOrWhiteSpace(slug)
                ? "https://www.amd.com/en/support/download/drivers.html"
                : $"https://www.amd.com/en/support/download/drivers.html?os=Windows+11+-+64&q={Uri.EscapeDataString(slug)}";
        }

        if (manufacturer.Contains("NVIDIA", StringComparison.OrdinalIgnoreCase))
        {
            return "https://www.nvidia.com/en-us/drivers/";
        }

        if (manufacturer.Contains("Intel", StringComparison.OrdinalIgnoreCase))
        {
            return "https://www.intel.com/content/www/us/en/download-center/home.html";
        }

        return null;
    }

    // CPUs are the exception among AMD components: AMD's own driver-download page (used for GPU
    // above) is GPU/chipset-specific and doesn't resolve per-CPU-model, and Windows Update already
    // covers CPU microcode. So this links to AMD's/Intel's general processor product page instead of
    // their driver page — still a real, stable, always-correct URL for "read more about this CPU",
    // same honesty standard as the GPU/Motherboard links (never a guessed per-model URL).
    private static string? BuildCpuVendorUrl(string manufacturer)
    {
        if (manufacturer.Contains("AMD", StringComparison.OrdinalIgnoreCase) ||
            manufacturer.Contains("Advanced Micro Devices", StringComparison.OrdinalIgnoreCase))
        {
            return "https://www.amd.com/en/products/processors/desktops/ryzen.html";
        }

        if (manufacturer.Contains("Intel", StringComparison.OrdinalIgnoreCase))
        {
            return "https://www.intel.com/content/www/us/en/products/details/processors.html";
        }

        return null;
    }

    // CPU-Z Validator (valid.x86.fr) search for this exact CPU model, sorted by highest recorded
    // frequency — user-discovered and personally verified working. Its "psn" parameter is the
    // CPU's full name string, ASCII-hex encoded — confirmed by hex-decoding the user's own example
    // URL, which yielded "AMD Ryzen 7 5800X 8-Core Processor" exactly matching WMI's
    // Win32_Processor.Name for that CPU. A real, mechanical, name-derived URL, not a guess.
    private static string BuildCpuBenchmarkUrl(string cpuName)
    {
        var hex = Convert.ToHexString(System.Text.Encoding.ASCII.GetBytes(cpuName)).ToLowerInvariant();
        return $"https://valid.x86.fr/search/search.php?psn={hex}&sort=freq";
    }

    // ---------- Memory ----------

    private Dna? DetectMemory(IReadOnlyList<RawSensorReading> sensors, DateTime now)
    {
        var rows = _wmiAdapter.Query("Win32_PhysicalMemory");
        if (rows.Count == 0)
        {
            return null;
        }

        var modules = rows.Select(row =>
        {
            var capacityBytes = GetLong(row, "Capacity");
            return new MemoryModule
            {
                SizeGb = capacityBytes.HasValue ? Math.Round(capacityBytes.Value / 1024.0 / 1024.0 / 1024.0, 1) : null,
                Manufacturer = GetString(row, "Manufacturer")?.Trim(),
                PartNumber = GetString(row, "PartNumber")?.Trim(),
            };
        }).ToList();

        // Null (not a fabricated sum) if any module's size is unknown — a total that silently
        // treated an unknown module as 0 GB would understate the real capacity as if confirmed.
        var totalGb = modules.Any(m => !m.SizeGb.HasValue) ? (double?)null : modules.Sum(m => m.SizeGb!.Value);

        // Keep the raw nullable reading (ratedSpeedMts) separate from speedMtsForDisplay's fallback.
        // RulesEngine.EvaluateMemory compares ActualSpeedMts against RatedSpeedMts to detect
        // XMP/EXPO not enabled — feeding it a fabricated "0" when the real rated speed is unknown
        // would make any genuine actual-speed reading look like it's running above a fake spec,
        // silently suppressing a real yellow finding. Basic.SpeedMts is a required display field
        // with no way to represent "unknown," so it keeps a 0 fallback for that display-only case.
        var ratedSpeedMts = GetInt(rows[0], "Speed");
        var speedMtsForDisplay = ratedSpeedMts ?? 0;
        var configuredSpeed = GetInt(rows[0], "ConfiguredClockSpeed");
        var memoryTypeCode = GetInt(rows[0], "SMBIOSMemoryType");
        var memoryType = MapMemoryType(memoryTypeCode);

        var arrayRows = _wmiAdapter.Query("Win32_PhysicalMemoryArray");
        // Null (not collapsed to SlotsUsed) when the real total slot count isn't available — the
        // prior fallback silently reported "no empty slots" as if confirmed, when it was unknown.
        var slotsTotal = arrayRows.Count > 0 ? GetInt(arrayRows[0], "MemoryDevices") : null;

        var manufacturer = modules.FirstOrDefault()?.Manufacturer;

        var basic = new MemoryBasic
        {
            TotalCapacityGb = totalGb,
            Type = memoryType,
            SpeedMts = configuredSpeed ?? speedMtsForDisplay,
            SlotsUsed = modules.Count,
            SlotsTotal = slotsTotal,
            VendorSupportUrl = BuildMemoryVendorUrl(manufacturer),
        };

        var advanced = new MemoryAdvanced
        {
            RatedSpeedMts = ratedSpeedMts,
            ActualSpeedMts = configuredSpeed,
            ChannelMode = null, // Not reliably derivable without vendor-specific tooling; left null.
            TimingsCl = null,
            PerModule = modules,
        };
        var reasons = RulesEngine.EvaluateMemory(advanced);
        var status = RulesEngine.StatusFromReasons(reasons);
        var capacityLabel = totalGb.HasValue ? $"{totalGb.Value:0.#} GB" : "Unknown capacity";
        var summary = $"{capacityLabel} {memoryType} — {modules.Count} module(s) installed, {SummarySuffix(status, reasons)}";

        // Includes the manufacturer when confidently known, e.g. "Corsair 16GB DDR4" instead of a
        // fully generic "16GB DDR4 Memory" — real WMI part-number data exists per module but is
        // sometimes an internal/cryptic code rather than a readable retail name depending on the
        // vendor, so it's deliberately not used here (would risk showing an ugly string); the
        // manufacturer name alone is a safe, always-legible middle ground (user feedback, 2026-08-16).
        var memoryName = manufacturer != null
            ? $"{manufacturer} {capacityLabel} {memoryType}"
            : $"{capacityLabel} {memoryType} Memory";

        return new Dna
        {
            Id = Guid.NewGuid().ToString(),
            Type = DnaType.Memory,
            Name = memoryName,
            Manufacturer = manufacturer ?? "Unknown",
            Summary = summary,
            Status = status,
            StatusReasons = reasons,
            Basic = basic,
            Advanced = advanced,
            Driver = DriverInfo.NotApplicable,
            LastUpdated = now,
        };
    }

    // ---------- Storage (one DNA per physical disk) ----------

    private List<Dna> DetectStorage(IReadOnlyList<RawSensorReading> sensors, DateTime now)
    {
        var result = new List<Dna>();
        var disks = _wmiAdapter.Query("Win32_DiskDrive");

        // Build a rough drive-letter -> free space map via LogicalDisk, since correlating
        // physical disks to logical volumes precisely requires association queries not yet built.
        var logicalDisks = _wmiAdapter.Query("Win32_LogicalDisk");
        var storageSensors = sensors.Where(s => s.HardwareType == "Storage").ToList();
        // MSStorageDriver_FailurePredictStatus lives under root\wmi, not root\cimv2. This query
        // commonly returns empty on NVMe-only systems or when the legacy SMART WMI provider is
        // unavailable (e.g. NVMe drives typically are not exposed through this provider) — an
        // empty result here is a genuine source limitation, not a bug.
        var smartStatusRows = _wmiAdapter.Query("MSStorageDriver_FailurePredictStatus", "root\\wmi");
        var diskToPartitionRows = _wmiAdapter.Query("Win32_DiskDriveToDiskPartition");
        var logicalDiskToPartitionRows = _wmiAdapter.Query("Win32_LogicalDiskToPartition");

        // MSFT_PhysicalDisk (root\Microsoft\Windows\Storage, Windows 8+ client SKUs) reports MediaType
        // as an explicit enum (0=Unspecified, 3=HDD, 4=SSD, 5=SCM) and BusType (17=NVMe among others) —
        // a genuine hardware-reported signal, not inferred from an ambiguous string. Verified empirically
        // (non-elevated) to require no admin rights and to correctly report MediaType=4/BusType=17 for
        // real NVMe SSDs on this dev machine. Correlated to Win32_DiskDrive via DeviceId<->Index (both
        // small integers identifying the same physical disk in enumeration order). If this namespace is
        // unavailable/returns Unspecified (older Windows, or the documented-but-uncommon WMI quirk where
        // it silently returns 0 — see BACKLOG.md), falls back to the prior confident-substring/"Unknown"
        // logic rather than guessing.
        var physicalDisksByIndex = _wmiAdapter.Query("MSFT_PhysicalDisk", "root\\Microsoft\\Windows\\Storage")
            .Where(r => GetInt(r, "DeviceId").HasValue)
            .ToDictionary(r => GetInt(r, "DeviceId")!.Value);

        foreach (var disk in disks)
        {
            var model = GetString(disk, "Model")?.Trim() ?? "Unknown Disk";
            var capacityBytes = GetLong(disk, "Size");
            var capacityGb = capacityBytes.HasValue ? Math.Round(capacityBytes.Value / 1024.0 / 1024.0 / 1024.0, 1) : (double?)null;
            var interfaceType = GetString(disk, "InterfaceType");
            var mediaType = GetString(disk, "MediaType") ?? "";
            var deviceId = GetString(disk, "DeviceID");
            var pnpDeviceId = GetString(disk, "PNPDeviceID");
            var diskIndex = GetInt(disk, "Index");

            var type = "Unknown";
            if (diskIndex.HasValue && physicalDisksByIndex.TryGetValue(diskIndex.Value, out var physicalDisk))
            {
                var msftMediaType = GetInt(physicalDisk, "MediaType");
                var busType = GetInt(physicalDisk, "BusType");
                if (busType == 17) // NVMe, per MSFT_PhysicalDisk's documented BusType enum.
                {
                    type = "NVMe";
                }
                else if (msftMediaType == 4) // SSD
                {
                    type = "SSD";
                }
                else if (msftMediaType == 3) // HDD
                {
                    type = "HDD";
                }
                // msftMediaType == 0 (Unspecified) or missing: fall through to the string-based
                // fallback below rather than reporting "Unknown" immediately — a confident substring
                // match is still better than nothing when the modern signal didn't resolve.
            }

            if (type == "Unknown")
            {
                // Fallback for when MSFT_PhysicalDisk is unavailable or returned Unspecified: only
                // confident, explicit signals — "Fixed hard disk" is deliberately NOT treated as SSD
                // here, since that would reintroduce the fabrication this logic replaced.
                type = mediaType.Contains("SSD", StringComparison.OrdinalIgnoreCase)
                    ? "SSD"
                    : model.Contains("NVMe", StringComparison.OrdinalIgnoreCase)
                        ? "NVMe"
                        : "Unknown";
            }

            // Correlate disk -> LHM storage sensors via the Windows physical drive index, not a
            // model-name substring match. LibreHardwareMonitor's Identifier for storage hardware
            // (e.g. "/storage/nvme/0") embeds StorageDeviceNumber as its trailing segment — the same
            // OS-level device index WMI exposes as Win32_DiskDrive.Index. This disambiguates two
            // disks with an identical model string, which a substring match cannot (research-verified
            // against LibreHardwareMonitorLib's current source; see BACKLOG.md). Falls back to the
            // substring match only if the index can't be parsed from LHM's identifier for this entry.
            var matchingSensor = diskIndex.HasValue
                ? storageSensors.FirstOrDefault(s => ParseTrailingIndex(s.HardwareIdentifier) == diskIndex.Value)
                : null;

            matchingSensor ??= storageSensors.FirstOrDefault(s =>
                s.HardwareName.Contains(model, StringComparison.OrdinalIgnoreCase) ||
                model.Contains(s.HardwareName, StringComparison.OrdinalIgnoreCase));

            var tempReading = matchingSensor != null
                ? storageSensors.FirstOrDefault(s => s.HardwareIdentifier == matchingSensor.HardwareIdentifier && s.SensorType == "Temperature")
                : storageSensors.FirstOrDefault(s => s.SensorType == "Temperature");

            // Correlate physical disk -> partitions -> logical disks via the WMI association classes,
            // matching on the "Antecedent"/"Dependent" reference-path strings which embed DeviceID as
            // a quoted DeviceID="..." segment. Matched against that exact quoted form, not a bare
            // Contains(deviceId) — a bare substring match would let e.g. PHYSICALDRIVE1's deviceId
            // wrongly match PHYSICALDRIVE10/11/...19's Antecedent rows on any system with 10+ disks,
            // since "PHYSICALDRIVE1" is a substring of "PHYSICALDRIVE10".
            var partitions = new List<StoragePartition>();
            if (!string.IsNullOrEmpty(deviceId))
            {
                var quotedDeviceId = $"DeviceID=\"{deviceId!.Replace("\\", "\\\\")}\"";
                var myPartitionRows = diskToPartitionRows.Where(r =>
                    (GetString(r, "Antecedent") ?? "").Contains(quotedDeviceId, StringComparison.OrdinalIgnoreCase));

                foreach (var partRow in myPartitionRows)
                {
                    var partitionRef = GetString(partRow, "Dependent");
                    if (partitionRef == null)
                    {
                        continue;
                    }

                    var logicalRow = logicalDiskToPartitionRows.FirstOrDefault(r =>
                        (GetString(r, "Antecedent") ?? "").Equals(partitionRef, StringComparison.OrdinalIgnoreCase));
                    var logicalRef = logicalRow != null ? GetString(logicalRow, "Dependent") : null;
                    if (logicalRef == null)
                    {
                        continue;
                    }

                    // logicalRef looks like: \\HOST\root\cimv2:Win32_LogicalDisk.DeviceID="C:"
                    var driveLetterMatch = System.Text.RegularExpressions.Regex.Match(logicalRef, "DeviceID=\"([A-Z]:)\"");
                    if (!driveLetterMatch.Success)
                    {
                        continue;
                    }
                    var driveLetter = driveLetterMatch.Groups[1].Value;

                    var logicalDisk = logicalDisks.FirstOrDefault(l => GetString(l, "DeviceID") == driveLetter);
                    if (logicalDisk == null)
                    {
                        continue;
                    }

                    var logicalCapacity = GetLong(logicalDisk, "Size");
                    var logicalFree = GetLong(logicalDisk, "FreeSpace");
                    var logicalCapacityGb = logicalCapacity.HasValue ? Math.Round(logicalCapacity.Value / 1024.0 / 1024.0 / 1024.0, 1) : (double?)null;
                    var logicalFreePct = logicalCapacity.HasValue && logicalCapacity.Value > 0 && logicalFree.HasValue
                        ? Math.Round(logicalFree.Value * 100.0 / logicalCapacity.Value, 1)
                        : 0;

                    partitions.Add(new StoragePartition
                    {
                        DriveLetter = driveLetter,
                        CapacityGb = logicalCapacityGb,
                        FreeSpacePct = logicalFreePct,
                    });
                }
            }

            // Aggregate free-space percentage across this disk's partitions (weighted by capacity)
            // as a reasonable approximation for the disk-level Basic.FreeSpacePct field. Partitions
            // with unknown capacity are excluded from the weighting (their FreeSpacePct alone can't
            // be meaningfully weighted without a real capacity), rather than treating unknown as 0.
            var partitionsWithKnownCapacity = partitions.Where(p => p.CapacityGb.HasValue).ToList();
            var totalPartitionCapacity = partitionsWithKnownCapacity.Sum(p => p.CapacityGb!.Value);
            var freeSpacePct = partitionsWithKnownCapacity.Count > 0 && totalPartitionCapacity > 0
                ? Math.Round(partitionsWithKnownCapacity.Sum(p => p.CapacityGb!.Value * p.FreeSpacePct) / totalPartitionCapacity, 1)
                : 0;

            // SMART predicted-failure status via MSStorageDriver_FailurePredictStatus, matched by
            // InstanceName containing the disk's PNPDeviceID. Only PredictFailure=false with no
            // reported failure maps to a "healthy" percentage; the WMI class does not expose a
            // granular health percentage, so we surface a coarse 100%/0% rather than fabricating detail.
            double? smartHealthPct = null;
            if (!string.IsNullOrEmpty(pnpDeviceId))
            {
                var smartRow = smartStatusRows.FirstOrDefault(r =>
                    (GetString(r, "InstanceName") ?? "").Contains(pnpDeviceId!, StringComparison.OrdinalIgnoreCase));
                if (smartRow != null)
                {
                    var predictFailure = GetBool(smartRow, "PredictFailure");
                    smartHealthPct = predictFailure == true ? 0 : predictFailure == false ? 100 : null;
                }
            }

            var basic = new StorageBasic
            {
                Model = model,
                CapacityGb = capacityGb,
                FreeSpacePct = freeSpacePct,
                Type = type,
                VendorSupportUrl = BuildStorageVendorUrl(ExtractStorageManufacturerFromModel(model)),
            };

            var advanced = new StorageAdvanced
            {
                Interface = interfaceType,
                RatedSpeedMbps = null, // Not exposed by Win32_DiskDrive/LHM without model-specific spec lookup.
                SmartHealthPct = smartHealthPct,
                CurrentTempC = tempReading?.Value,
                Partitions = partitions.Count > 0 ? partitions : null,
            };

            var reasons = RulesEngine.EvaluateStorage(basic, advanced);
            var status = RulesEngine.StatusFromReasons(reasons);
            var capacityLabel = capacityGb.HasValue ? $"{capacityGb.Value:0.#} GB" : "Unknown capacity";
            var summary = $"{model} — {capacityLabel} {type}, {SummarySuffix(status, reasons)}";

            result.Add(new Dna
            {
                Id = Guid.NewGuid().ToString(),
                Type = DnaType.Storage,
                Name = model,
                Manufacturer = ExtractStorageManufacturerFromModel(model),
                Summary = summary,
                Status = status,
                StatusReasons = reasons,
                Basic = basic,
                Advanced = advanced,
                Driver = DriverInfo.NotApplicable,
                LastUpdated = now,
            });
        }

        return result;
    }

    // Only the handful of storage vendors whose Win32_DiskDrive.Model string reliably starts with a
    // full, unambiguous, capitalized word — not short alphanumeric model-code prefixes like
    // Seagate's "ST" or Crucial's "CT", which carry real collision risk (research: smartmontools'
    // own vendor-identification database, the mature prior art for exactly this problem, needed a
    // large maintained regex table rather than a short list specifically because of cases like
    // these — so this list deliberately stays narrow rather than attempting full vendor coverage).
    // Falls through to "Unknown" for everything else, same as before this was added.
    //
    // "WDC" and "WDS" are BOTH real, distinct Western Digital prefixes, not a typo/duplicate: WDC
    // is WD's legacy HDD/older-SATA-SSD product-line prefix (e.g. "WDC WD10EZEX-00..."), WDS is
    // WD's own-branded SSD-line prefix adopted after the SanDisk acquisition (e.g. WD Black SN850's
    // real model string "WDS100T1X0E-00AFY0", confirmed missing a vendor match on real hardware —
    // user feedback, 2026-08-16). Kept as two explicit entries rather than collapsed to a shared
    // "WD" root, which would be short/generic enough to reintroduce the same collision risk as
    // "ST"/"CT" (research-verified).
    private static readonly string[] KnownStorageVendorPrefixes = ["Samsung", "KINGSTON", "WDC", "WDS", "SanDisk", "INTEL"];

    private static string ExtractStorageManufacturerFromModel(string model)
    {
        foreach (var prefix in KnownStorageVendorPrefixes)
        {
            if (model.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                return prefix;
            }
        }
        return "Unknown";
    }

    // General manufacturer support pages only — research confirmed no storage vendor exposes a
    // per-model URL derivable from a WMI model string (Samsung/WD/Kingston/Crucial/Seagate all use
    // internal catalog IDs or shortened model codes with no mechanical mapping from the full model
    // string). Same honesty pattern as BuildMotherboardVendorUrl's Gigabyte/ASRock/MSI fallback.
    private static string? BuildStorageVendorUrl(string manufacturer) => manufacturer switch
    {
        "Samsung" => "https://semiconductor.samsung.com/consumer-storage/support/",
        "WDC" => "https://www.westerndigital.com/support",
        "WDS" => "https://www.westerndigital.com/support", // WD's own SSD-line prefix — same vendor as WDC.
        "KINGSTON" => "https://www.kingston.com/en/support",
        "SanDisk" => "https://www.westerndigital.com/support", // SanDisk is a Western Digital brand.
        "INTEL" => "https://www.intel.com/content/www/us/en/support.html",
        _ => null,
    };

    // General manufacturer support pages only — research confirmed no memory vendor exposes a
    // per-model URL derivable from a WMI part-number string, same reasoning as BuildStorageVendorUrl.
    // Matched with Contains rather than exact-equals since WMI's Win32_PhysicalMemory.Manufacturer
    // is a free-text field (e.g. "Corsair" vs "Corsair Memory, Inc.") with no fixed enumerated set
    // the way disk model prefixes are.
    private static string? BuildMemoryVendorUrl(string? manufacturer)
    {
        if (manufacturer == null)
        {
            return null;
        }

        if (manufacturer.Contains("Corsair", StringComparison.OrdinalIgnoreCase))
        {
            return "https://help.corsair.com/hc/en-us";
        }

        if (manufacturer.Contains("G.Skill", StringComparison.OrdinalIgnoreCase) ||
            manufacturer.Contains("G Skill", StringComparison.OrdinalIgnoreCase))
        {
            return "https://www.gskill.com/techsupport";
        }

        if (manufacturer.Contains("Kingston", StringComparison.OrdinalIgnoreCase))
        {
            return "https://www.kingston.com/en/support";
        }

        if (manufacturer.Contains("Crucial", StringComparison.OrdinalIgnoreCase) ||
            manufacturer.Contains("Micron", StringComparison.OrdinalIgnoreCase))
        {
            return "https://www.crucial.com/support";
        }

        return null;
    }

    // Reuses the motherboard's own vendor URL for what looks like an integrated adapter (research
    // confirmed the chipset maker's own site, e.g. Realtek's, has no stable per-model URL and its
    // generic driver is usually inferior to the motherboard vendor's customized package for that
    // exact board). "Family Controller" in the name is WMI's real, consistent phrasing for
    // integrated LAN chipsets (e.g. "Realtek PCIe GbE Family Controller") — a genuine signal, not a
    // guess. Falls back to the chipset maker's general downloads page only when no motherboard
    // match exists (a discrete/USB NIC, or the motherboard's own URL wasn't available).
    private static string? BuildNetworkVendorUrl(string adapterName, string? motherboardVendorUrl)
    {
        var looksIntegrated = adapterName.Contains("Family Controller", StringComparison.OrdinalIgnoreCase);

        if (looksIntegrated && motherboardVendorUrl != null)
        {
            return motherboardVendorUrl;
        }

        if (adapterName.Contains("Realtek", StringComparison.OrdinalIgnoreCase))
        {
            return "https://www.realtek.com/en/downloads";
        }

        if (adapterName.Contains("Intel", StringComparison.OrdinalIgnoreCase))
        {
            return "https://www.intel.com/content/www/us/en/support.html";
        }

        return motherboardVendorUrl;
    }

    // Real, stable Microsoft release-health page for this exact Windows 11 feature update, e.g.
    // "24H2" -> https://learn.microsoft.com/en-us/windows/release-health/status-windows-11-24h2 —
    // research-confirmed this URL pattern is genuinely mechanical from the registry's
    // DisplayVersion value, not a guess. Windows 10 (and any case where DisplayVersion isn't
    // available) falls back to the general release-health hub rather than a per-version URL,
    // since Windows 10's historical per-version page naming was less consistent near EOL.
    private static string? BuildOsVendorUrl(string osName, string? displayVersion)
    {
        if (osName.Contains("Windows 11", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrWhiteSpace(displayVersion))
        {
            return $"https://learn.microsoft.com/en-us/windows/release-health/status-windows-11-{displayVersion.ToLowerInvariant()}";
        }

        if (osName.Contains("Windows", StringComparison.OrdinalIgnoreCase))
        {
            return "https://learn.microsoft.com/en-us/windows/release-health/";
        }

        return null;
    }

    // ---------- Motherboard ----------

    private Dna? DetectMotherboard(IReadOnlyList<RawSensorReading> sensors, DateTime now)
    {
        var boardRows = _wmiAdapter.Query("Win32_BaseBoard");
        var biosRows = _wmiAdapter.Query("Win32_BIOS");
        var boardRow = boardRows.FirstOrDefault();
        if (boardRow == null)
        {
            return null;
        }

        var manufacturer = GetString(boardRow, "Manufacturer")?.Trim() ?? "Unknown";
        var model = GetString(boardRow, "Product")?.Trim() ?? "Unknown";

        var biosRow = biosRows.FirstOrDefault();
        var biosVersion = biosRow != null ? GetString(biosRow, "SMBIOSBIOSVersion") : null;
        var biosDateRaw = biosRow != null ? GetString(biosRow, "ReleaseDate") : null;

        var basic = new MotherboardBasic
        {
            Manufacturer = manufacturer,
            Model = model,
            BiosVersion = biosVersion,
            Chipset = ExtractChipsetFromModel(model),
            VendorSupportUrl = BuildMotherboardVendorUrl(manufacturer, model),
        };

        // Socket is not exposed on Win32_BaseBoard; Win32_Processor.SocketDesignation is the closest
        // reliable source (the CPU socket == the motherboard socket on desktop systems).
        var cpuRows = _wmiAdapter.Query("Win32_Processor");
        var socket = cpuRows.FirstOrDefault() is { } cpuRow ? GetString(cpuRow, "SocketDesignation") : null;

        var memArrayRows = _wmiAdapter.Query("Win32_PhysicalMemoryArray");
        var memArrayRow = memArrayRows.FirstOrDefault();
        var memSupport = memArrayRow != null
            ? new MemorySupport
            {
                Type = null, // Win32_PhysicalMemoryArray does not expose a supported-type field distinct from installed modules.
                MaxCapacityGb = GetLong(memArrayRow, "MaxCapacity").HasValue
                    ? Math.Round(GetLong(memArrayRow, "MaxCapacity")!.Value / 1024.0 / 1024.0, 1) // MaxCapacity is in KB per WMI docs.
                    : null,
                Slots = GetInt(memArrayRow, "MemoryDevices"),
            }
            : null;

        var advanced = new MotherboardAdvanced
        {
            BiosDate = ParseWmiDate(biosDateRaw),
            Socket = socket,
            MemorySupport = memSupport,
            PcieSlots = null, // Requires vendor/ACPI-specific data not available via WMI/LHM in Phase 1.
            M2Slots = null, // Not exposed by standard WMI classes without vendor-specific tooling.
        };

        var reasons = RulesEngine.EvaluateMotherboard(advanced);
        var status = RulesEngine.StatusFromReasons(reasons);
        var summary = $"{manufacturer} {model} — {SummarySuffix(status, reasons)}";

        return new Dna
        {
            Id = Guid.NewGuid().ToString(),
            Type = DnaType.Motherboard,
            Name = model,
            Manufacturer = manufacturer,
            Summary = summary,
            Status = status,
            StatusReasons = reasons,
            Basic = basic,
            Advanced = advanced,
            Driver = DriverInfo.NotApplicable,
            LastUpdated = now,
        };
    }

    // Known AMD/Intel chipset codes, matched as an exact token embedded in the motherboard's Model
    // string (e.g. "TUF GAMING B550-PRO" contains "B550"). This is honest extraction of a real
    // substring already present in detected data, not a guess — chipset codes are vendor-registered,
    // low collision risk, and the enumeration grows slowly (~1-2 codes per vendor per year), unlike
    // the CPU-boost-clock spec database rejected elsewhere in BACKLOG.md for its much higher
    // per-SKU-quantitative-spec maintenance burden. Matched with word/delimiter boundaries (not a
    // bare Contains) to avoid one code matching inside another (e.g. "X570" inside "X570S"). Falls
    // through to null ("Unknown" in the UI) when no known code is found — never guessed.
    private static readonly string[] KnownChipsetCodes =
    [
        // AMD AM4 / AM5
        "A320", "B350", "X370", "A520", "B450", "X470", "B550", "X570S", "X570",
        "A620", "B650E", "B650", "X670E", "X670", "B840", "B850", "X870E", "X870",
        // Intel LGA1200 / LGA1700 / LGA1851
        "H310", "B360", "H370", "Z370", "Z390", "H410", "B460", "H470", "Z490",
        "H510", "B560", "H570", "Z590", "H610", "B660", "H670", "Z690",
        "B760", "H770", "Z790", "B860", "Z890",
    ];

    private static string? ExtractChipsetFromModel(string model)
    {
        foreach (var code in KnownChipsetCodes)
        {
            var index = model.IndexOf(code, StringComparison.OrdinalIgnoreCase);
            if (index < 0)
            {
                continue;
            }

            var beforeOk = index == 0 || !char.IsLetterOrDigit(model[index - 1]);
            var afterIndex = index + code.Length;
            var afterOk = afterIndex >= model.Length || !char.IsLetterOrDigit(model[afterIndex]);
            if (beforeOk && afterOk)
            {
                return code;
            }
        }
        return null;
    }

    // Links out to the motherboard vendor's official support/BIOS page — same "link, never fetch"
    // principle as BuildGpuVendorUrl. Research found ASUS has the most reliably constructible
    // per-model URL pattern of the major vendors; MSI/Gigabyte/ASRock lack a clean model-string-only
    // pattern (Gigabyte needs a board revision WMI doesn't report; ASRock/MSI need a search step), so
    // those fall back to the vendor's general support/driver search page rather than guessing a
    // per-model URL that could easily be wrong.
    private static string? BuildMotherboardVendorUrl(string manufacturer, string model)
    {
        if (manufacturer.Contains("ASUS", StringComparison.OrdinalIgnoreCase) ||
            manufacturer.Contains("ASUSTeK", StringComparison.OrdinalIgnoreCase))
        {
            var slug = model.Trim().ToLowerInvariant().Replace(" ", "-");
            return string.IsNullOrWhiteSpace(slug)
                ? "https://www.asus.com/support/"
                : $"https://www.asus.com/supportonly/{Uri.EscapeDataString(model.Trim())}/helpdesk_bios/";
        }

        if (manufacturer.Contains("Gigabyte", StringComparison.OrdinalIgnoreCase) ||
            manufacturer.Contains("GIGA-BYTE", StringComparison.OrdinalIgnoreCase))
        {
            return "https://www.gigabyte.com/Support";
        }

        if (manufacturer.Contains("ASRock", StringComparison.OrdinalIgnoreCase))
        {
            return "https://www.asrock.com/support/index.asp";
        }

        if (manufacturer.Contains("MSI", StringComparison.OrdinalIgnoreCase) ||
            manufacturer.Contains("Micro-Star", StringComparison.OrdinalIgnoreCase))
        {
            return "https://www.msi.com/support";
        }

        return null;
    }

    // ---------- Network (one DNA per adapter) ----------

    private List<Dna> DetectNetwork(DateTime now, string? motherboardVendorUrl)
    {
        var result = new List<Dna>();
        var adapters = _wmiAdapter.Query("Win32_NetworkAdapter");
        var configs = _wmiAdapter.Query("Win32_NetworkAdapterConfiguration");

        foreach (var adapter in adapters)
        {
            var netConnectionId = GetString(adapter, "NetConnectionID");
            var physicalAdapter = GetBool(adapter, "PhysicalAdapter");
            var name = GetString(adapter, "Name")?.Trim() ?? netConnectionId ?? "Unknown Adapter";
            if (string.IsNullOrWhiteSpace(netConnectionId) || physicalAdapter != true ||
                name.Contains("Bluetooth", StringComparison.OrdinalIgnoreCase))
            {
                // Skip virtual/software adapters (VPNs, virtual switches, etc.) and Bluetooth PAN
                // adapters for Phase 1 clarity — these are not "network connections" in the sense
                // this DNA is meant to represent, and otherwise show as permanently disconnected/red.
                continue;
            }
            var mac = GetString(adapter, "MACAddress");
            var speedBps = GetLong(adapter, "Speed");
            var index = GetInt(adapter, "Index");

            var config = index.HasValue
                ? configs.FirstOrDefault(c => GetInt(c, "Index") == index.Value)
                : null;
            var ip = config != null ? GetStringArrayFirst(config, "IPAddress") : null;

            var connectionType = name.Contains("Wi-Fi", StringComparison.OrdinalIgnoreCase) ||
                                  name.Contains("Wireless", StringComparison.OrdinalIgnoreCase)
                ? "WiFi"
                : "Wired";

            var basic = new NetworkBasic
            {
                AdapterName = name,
                ConnectionType = connectionType,
                CurrentSpeedMbps = speedBps.HasValue ? Math.Round(speedBps.Value / 1_000_000.0, 0) : null,
                VendorSupportUrl = BuildNetworkVendorUrl(name, motherboardVendorUrl),
            };

            var driverVersion = FindPnpDriverVersion(name);

            var advanced = new NetworkAdvanced
            {
                IpAddress = ip,
                MacAddress = mac,
                DriverVersion = driverVersion,
                SignalStrengthPct = null, // WiFi signal strength not exposed via these WMI classes.
                MaxSupportedSpeedMbps = null,
            };

            var isDisconnected = string.IsNullOrWhiteSpace(ip);
            var reasons = isDisconnected
                ? new List<StatusReason>
                {
                    new()
                    {
                        Message = $"{name} has no IP address assigned.",
                        Severity = ReasonSeverity.Red,
                        Suggestion = "Check the network cable or Wi-Fi connection.",
                        Confidence = Confidence.High,
                    },
                }
                : RulesEngine.EvaluateNetworkConnected(basic, advanced);
            var status = RulesEngine.StatusFromReasons(reasons);

            var summary = string.IsNullOrWhiteSpace(ip)
                ? $"{name} — disconnected."
                : $"{name} — connected via {connectionType}.";

            result.Add(new Dna
            {
                Id = Guid.NewGuid().ToString(),
                Type = DnaType.Network,
                Name = name,
                Manufacturer = GetString(adapter, "Manufacturer") ?? "Unknown",
                Summary = summary,
                Status = status,
                StatusReasons = reasons,
                Basic = basic,
                Advanced = advanced,
                Driver = string.IsNullOrWhiteSpace(driverVersion)
                    ? DriverInfo.NotApplicable
                    : new DriverInfo
                    {
                        IsApplicable = true,
                        Version = driverVersion,
                        Date = null, // Win32_PnPSignedDriver exposes DriverDate; not yet correlated per-adapter.
                        SourceUrl = null, // Never fabricate a driver source URL (REQUIREMENTS.md section 10).
                    },
                LastUpdated = now,
            });
        }

        return result;
    }

    // ---------- OS ----------

    private Dna? DetectOs(DateTime now)
    {
        var rows = _wmiAdapter.Query("Win32_OperatingSystem");
        var row = rows.FirstOrDefault();
        if (row == null)
        {
            return null;
        }

        var osName = GetString(row, "Caption")?.Trim() ?? "Unknown OS";
        var version = GetString(row, "Version") ?? "Unknown";
        var buildNumber = GetString(row, "BuildNumber") ?? "Unknown";
        var installDateRaw = GetString(row, "InstallDate");
        var lastBootRaw = GetString(row, "LastBootUpTime");
        var architecture = GetString(row, "OSArchitecture");

        var installDate = ParseWmiDateTime(installDateRaw);
        var lastBoot = ParseWmiDateTime(lastBootRaw);
        var uptime = lastBoot.HasValue ? DateTime.Now - lastBoot.Value : (TimeSpan?)null;

        var displayVersion = _registryAdapter.GetLocalMachineStringValue(
            @"SOFTWARE\Microsoft\Windows NT\CurrentVersion", "DisplayVersion");

        var basic = new OsBasic
        {
            OsName = osName,
            Version = version,
            BuildNumber = buildNumber,
            InstallDate = installDate,
            VendorSupportUrl = BuildOsVendorUrl(osName, displayVersion),
        };

        var lastUpdateDate = GetLastQuickFixDate();
        var activationStatus = GetActivationStatus();
        var rebootPending = GetRebootPending();

        var advanced = new OsAdvanced
        {
            Uptime = uptime,
            LastUpdateDate = lastUpdateDate,
            ActivationStatus = activationStatus,
            Architecture = architecture,
            RebootPending = rebootPending,
        };

        var reasons = RulesEngine.EvaluateOs(advanced);
        var status = RulesEngine.StatusFromReasons(reasons);
        var summary = $"{osName} — build {buildNumber}, {SummarySuffix(status, reasons)}";

        return new Dna
        {
            Id = Guid.NewGuid().ToString(),
            Type = DnaType.Os,
            Name = osName,
            Manufacturer = "Microsoft",
            Summary = summary,
            Status = status,
            StatusReasons = reasons,
            Basic = basic,
            Advanced = advanced,
            Driver = DriverInfo.NotApplicable,
            LastUpdated = now,
        };
    }

    // ---------- Network driver lookup ----------

    // Correlates a network adapter to its signed driver via Win32_PnPSignedDriver, matched by
    // device name. This is a best-effort name match (WMI does not give a direct FK from
    // Win32_NetworkAdapter to Win32_PnPSignedDriver), so a null result is expected/normal when
    // no confident match is found rather than indicating a bug.
    private string? FindPnpDriverVersion(string adapterName)
    {
        var driverRows = _wmiAdapter.Query("Win32_PnPSignedDriver");
        var match = driverRows.FirstOrDefault(r =>
            string.Equals(GetString(r, "DeviceName")?.Trim(), adapterName, StringComparison.OrdinalIgnoreCase));
        return match != null ? GetString(match, "DriverVersion") : null;
    }

    // ---------- OS helpers ----------

    // Latest applied update via Win32_QuickFixEngineering.InstalledOn. This reflects hotfixes
    // registered through Windows Update's classic mechanism; it may not capture every modern
    // cumulative update, so absence of a value is a genuine source limitation, not a bug.
    private DateTime? GetLastQuickFixDate()
    {
        var rows = _wmiAdapter.Query("Win32_QuickFixEngineering");
        DateTime? latest = null;
        foreach (var row in rows)
        {
            var installedOnRaw = GetString(row, "InstalledOn");
            if (installedOnRaw != null && DateTime.TryParse(installedOnRaw, CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed))
            {
                if (!latest.HasValue || parsed > latest.Value)
                {
                    latest = parsed;
                }
            }
        }
        return latest;
    }

    // Activation status via SoftwareLicensingProduct (root\cimv2). LicenseStatus 1 = Licensed.
    // Only the primary Windows OS licensing entry (PartialProductKey present) is considered.
    private string? GetActivationStatus()
    {
        var rows = _wmiAdapter.Query("SoftwareLicensingProduct");
        var row = rows.FirstOrDefault(r => !string.IsNullOrEmpty(GetString(r, "PartialProductKey")));
        if (row == null)
        {
            return null;
        }

        var licenseStatus = GetInt(row, "LicenseStatus");
        return licenseStatus switch
        {
            1 => "Licensed",
            0 => "Unlicensed",
            2 => "Out-Of-Box Grace Period",
            3 => "Out-Of-Tolerance Grace Period",
            4 => "Non-Genuine Grace Period",
            5 => "Notification",
            6 => "Extended Grace Period",
            _ => null,
        };
    }

    // Reboot-pending signal via two well-documented local registry keys (no elevation, no network
    // call — see BACKLOG.md for why the full "updates available" check was ruled out: the only API
    // for that, IUpdateSearcher, requires a real Windows Update/WSUS server round-trip). Checks the
    // Windows Update-specific key and the broader Component Based Servicing key; deliberately does
    // NOT check PendingFileRenameOperations, which is a documented false-positive source (AV/cleanup
    // tools also write to it, per community tooling precedent like the PendingReboot PS module).
    private bool GetRebootPending() =>
        _registryAdapter.LocalMachineKeyExists(@"SOFTWARE\Microsoft\Windows\CurrentVersion\WindowsUpdate\Auto Update\RebootRequired") ||
        _registryAdapter.LocalMachineKeyExists(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Component Based Servicing\RebootPending");

    // ---------- CPU helpers ----------

    // Active Windows power plan name via Win32_PowerPlan (root\cimv2\power), where IsActive=true.
    private string? GetActivePowerPlanName()
    {
        var rows = _wmiAdapter.Query("Win32_PowerPlan", "root\\cimv2\\power");
        var active = rows.FirstOrDefault(r => GetBool(r, "IsActive") == true);
        return active != null ? GetString(active, "ElementName") : null;
    }

    // ---------- Shared helpers ----------

    // A status-aware summary clause. Previously CPU/Memory/OS hardcoded "running normally"
    // regardless of the computed Status, so a Red/Yellow DNA could show a summary claiming
    // everything was fine — directly contradicting the status light shown alongside it. Mirrors
    // the branching Network's summary already did correctly (connected vs. disconnected).
    //
    // For non-Green statuses, surfaces the specific fired reason's ShortReason rather than a vague
    // "worth a closer look" — REQUIREMENTS.md section 3 requires status to be explainable, and a
    // generic suffix didn't say what was actually wrong. Multiple Yellow/Red reasons can genuinely
    // fire simultaneously for one DNA (each RulesEngine check is an independent condition, not
    // mutually exclusive), so this picks the worst-severity, first-evaluated one — matching
    // StatusFromReasons' own worst-wins logic, using reasons' existing source-order as the tiebreak.
    private static string SummarySuffix(HealthStatus status, IReadOnlyList<StatusReason> reasons)
    {
        if (status == HealthStatus.Green)
        {
            return "running normally.";
        }

        var targetSeverity = status == HealthStatus.Red ? ReasonSeverity.Red : ReasonSeverity.Yellow;
        var picked = reasons.FirstOrDefault(r => r.Severity == targetSeverity && r.ShortReason != null);
        return picked?.ShortReason ?? (status == HealthStatus.Red ? "needs attention." : "worth a closer look.");
    }

    // Maps SMBIOSMemoryType codes to the DDR generation labels used in the Model (26=DDR4, 34=DDR5).
    private static string MapMemoryType(int? smbiosMemoryType) => smbiosMemoryType switch
    {
        20 => "DDR",
        21 => "DDR2",
        24 => "DDR3",
        26 => "DDR4",
        34 => "DDR5",
        _ => "Unknown",
    };

    private static string? GetString(RawWmiInventory row, string key) =>
        row.Properties.TryGetValue(key, out var value) ? value?.ToString() : null;

    private static string? GetStringArrayFirst(RawWmiInventory row, string key)
    {
        if (row.Properties.TryGetValue(key, out var value) && value is string[] arr && arr.Length > 0)
        {
            return arr[0];
        }
        return null;
    }

    // WMI uint16[] properties (e.g. Win32_SystemEnclosure.ChassisTypes) come back from
    // System.Management as ushort[] at runtime.
    private static int[]? GetIntArray(RawWmiInventory row, string key)
    {
        if (row.Properties.TryGetValue(key, out var value) && value is ushort[] arr && arr.Length > 0)
        {
            return arr.Select(v => (int)v).ToArray();
        }
        return null;
    }

    private static int? GetInt(RawWmiInventory row, string key)
    {
        if (!row.Properties.TryGetValue(key, out var value) || value == null)
        {
            return null;
        }
        return Convert.ToInt32(value, CultureInfo.InvariantCulture);
    }

    private static long? GetLong(RawWmiInventory row, string key)
    {
        if (!row.Properties.TryGetValue(key, out var value) || value == null)
        {
            return null;
        }
        return Convert.ToInt64(value, CultureInfo.InvariantCulture);
    }

    private static double? GetDouble(RawWmiInventory row, string key)
    {
        if (!row.Properties.TryGetValue(key, out var value) || value == null)
        {
            return null;
        }
        return Convert.ToDouble(value, CultureInfo.InvariantCulture);
    }

    private static bool? GetBool(RawWmiInventory row, string key)
    {
        if (!row.Properties.TryGetValue(key, out var value) || value == null)
        {
            return null;
        }
        return Convert.ToBoolean(value, CultureInfo.InvariantCulture);
    }

    // WMI dates come as strings like "20230815000000.000000+060" (DMTF datetime format).
    private static string? ParseWmiDate(string? dmtfDate)
    {
        var parsed = ParseWmiDateTime(dmtfDate);
        return parsed?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
    }

    private static DateTime? ParseWmiDateTime(string? dmtfDate)
    {
        if (string.IsNullOrWhiteSpace(dmtfDate))
        {
            return null;
        }

        // Minimal DMTF datetime parser (format: yyyyMMddHHmmss.ffffff+UUU) to avoid taking a
        // dependency on System.Management from the Detection Engine (that stays in Platform Adapters).
        try
        {
            var datePart = dmtfDate.Length >= 14 ? dmtfDate.Substring(0, 14) : null;
            if (datePart == null)
            {
                return null;
            }

            return DateTime.ParseExact(datePart, "yyyyMMddHHmmss", CultureInfo.InvariantCulture);
        }
        catch
        {
            return null;
        }
    }
}

//*Built with assistance from __Claude Code__ by Anthropic.*
