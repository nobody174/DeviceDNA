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

namespace DeviceDNA.UI.Presentation;

// One label/value row shown in a DNA's Basic or Advanced tier view.
public class FieldRow
{
    public required string Label { get; init; }
    public required string Value { get; init; }

    // Shown as a tooltip when Value is exactly "Unknown" — distinguishes "the app honestly couldn't
    // determine this from available data" from "something is broken," per BACKLOG.md history. Not
    // fabrication risk: this is a static explanatory string, not a guessed value.
    public string? Tooltip => Value == "Unknown"
        ? "This couldn't be reliably determined from available data."
        : null;
}

// ViewModel wrapping a single Dna instance for display as an orbital dashboard node and its
// full-detail expanded view (BACKLOG.md "DNA-helix / orbital visual redesign").
public class DnaTileViewModel : ViewModelBase
{
    private readonly Dna _dna;
    private bool _isCheckingWindowsUpdate;
    private string? _windowsUpdateResultText;

    public DnaTileViewModel(Dna dna)
    {
        _dna = dna;
        OpenVendorUrlCommand = new RelayCommand(
            _ => OpenVendorUrlRequested?.Invoke(this, VendorUrl!),
            _ => VendorUrl != null);
        CheckWindowsUpdateCommand = new RelayCommand(
            _ => CheckWindowsUpdateRequested?.Invoke(this, EventArgs.Empty),
            _ => SupportsWindowsUpdateCheck && !IsCheckingWindowsUpdate);
        BasicFields = BuildBasicFields();
        AdvancedFields = BuildAdvancedFields();
    }

    public string Name => _dna.Name;
    public string Manufacturer => _dna.Manufacturer;
    public string Summary => _dna.Summary;
    public string TypeLabel => _dna.Type.ToString();
    public DnaType Type => _dna.Type;

    // Every DNA field, Basic then Advanced, with no tier split — the orbital detail view shows
    // everything for a component at once (design decision, 2026-08-15: no Basic/Advanced toggle
    // once a component is already given full-screen space).
    public IReadOnlyList<FieldRow> AllFields => BasicFields.Concat(AdvancedFields).ToList();

    // Connection type (+ signal strength for WiFi) surfaced prominently in the detail view's
    // header, not just buried as another field row — user feedback, 2026-08-16: "should it also
    // view what network we are connected to... if its wifi, it should show up wifi also." Null for
    // every non-Network DNA, so the header only shows this line where it's meaningful.
    public string? NetworkSubtitle => _dna.Basic is NetworkBasic net
        ? net.ConnectionType == "WiFi" && _dna.Advanced is NetworkAdvanced { SignalStrengthPct: { } signal }
            ? $"WiFi — Signal {signal}%"
            : net.ConnectionType
        : null;

    // The vendor's official product/support page for this specific hardware, when confidently
    // identifiable (GPU: DriverInfo.SourceUrl; CPU/Motherboard/Storage/Memory/Network/OS: their own
    // Basic.VendorSupportUrl — each with its own honesty-scoped construction, see
    // DeviceDetectionService.cs's BuildXVendorUrl methods and each field's doc comment for what
    // pattern is actually reliable per component type). Null for DNAs with no confident vendor page
    // pattern — the UI shows a plain (non-clickable) name in that case rather than a dead/wrong
    // link. This app never fetches this URL itself; it only opens it in the user's default browser
    // on an explicit click (REQUIREMENTS.md section 10, clarified 2026-08-15).
    public string? VendorUrl => _dna.Basic switch
    {
        MotherboardBasic mobo => mobo.VendorSupportUrl,
        CpuBasic cpu => cpu.VendorSupportUrl,
        StorageBasic storage => storage.VendorSupportUrl,
        MemoryBasic mem => mem.VendorSupportUrl,
        NetworkBasic net => net.VendorSupportUrl,
        OsBasic os => os.VendorSupportUrl,
        _ when _dna.Driver.IsApplicable => _dna.Driver.SourceUrl,
        _ => null,
    };

    public bool HasVendorUrl => VendorUrl != null;

    // CPU-Z Validator community benchmark database search for this exact CPU — see
    // CpuBasic.BenchmarkUrl's doc comment for how this URL is constructed and verified. Null for
    // every non-CPU DNA (no other component type has an equivalent confirmed pattern — see
    // BACKLOG.md's GPU benchmark-link entry for why that one specifically isn't built).
    public string? BenchmarkUrl => _dna.Basic is CpuBasic cpu ? cpu.BenchmarkUrl : null;

    public bool HasBenchmarkUrl => BenchmarkUrl != null;

    // Raised when the user clicks the DNA name and a VendorUrl is available; MainWindow's
    // code-behind handles this to launch the default browser, keeping process-launching out of the
    // ViewModel (same pattern as OpenDiagnoseRequested/OpenHistoryRequested in MainViewModel).
    public event EventHandler<string>? OpenVendorUrlRequested;

    public RelayCommand OpenVendorUrlCommand { get; }

    // Same launch-in-browser pattern as OpenVendorUrlCommand, but for BenchmarkUrl — kept as a
    // separate command/event rather than overloading OpenVendorUrlRequested, since a DNA can have
    // both a vendor link and a benchmark link simultaneously (CPU does) and the UI needs to
    // distinguish which one was clicked.
    public event EventHandler<string>? OpenBenchmarkUrlRequested;

    public RelayCommand OpenBenchmarkUrlCommand => _openBenchmarkUrlCommand ??= new RelayCommand(
        _ => OpenBenchmarkUrlRequested?.Invoke(this, BenchmarkUrl!),
        _ => BenchmarkUrl != null);

    private RelayCommand? _openBenchmarkUrlCommand;

    // Windows Update's Type='Driver' search is genuinely useful for chipset/network/audio/storage
    // driver classes, but research confirmed poor real-world coverage for GPU (NVIDIA/AMD favor
    // their own apps as the primary channel) and Motherboard BIOS (essentially never distributed via
    // Windows Update at all) — offered for those two anyway since it's still a real, free, honest
    // signal, but the result text for those two DNAs carries an explicit caveat (see
    // WindowsUpdateResultText). Not offered for DNAs with no driver concept at all (Memory, Storage,
    // OS — matches the existing DriverInfo.IsApplicable policy).
    public bool SupportsWindowsUpdateCheck => _dna.Type is
        DnaType.Cpu or DnaType.Gpu or DnaType.Network or DnaType.Motherboard;

    public bool IsCheckingWindowsUpdate
    {
        get => _isCheckingWindowsUpdate;
        set => SetField(ref _isCheckingWindowsUpdate, value);
    }

    // Null until a check has been run for this tile — the UI shows nothing in that case, never a
    // placeholder implying a check already happened.
    public string? WindowsUpdateResultText
    {
        get => _windowsUpdateResultText;
        private set => SetField(ref _windowsUpdateResultText, value);
    }

    // Raised when the user clicks "Check Windows Update"; MainWindow's code-behind runs the actual
    // (slow, live-network) check off the UI thread and calls back into
    // ApplyWindowsUpdateCheckResult/ApplyWindowsUpdateCheckFailure — keeps the live COM call out of
    // the ViewModel, same window-management/process-launching separation used elsewhere in this app.
    public event EventHandler? CheckWindowsUpdateRequested;

    public RelayCommand CheckWindowsUpdateCommand { get; }

    public void ApplyWindowsUpdateCheckResult(int updateCount)
    {
        IsCheckingWindowsUpdate = false;
        var caveat = _dna.Type is DnaType.Gpu or DnaType.Motherboard
            ? " (Windows Update often lags behind the vendor's own site for this hardware — check the link above too.)"
            : "";
        WindowsUpdateResultText = updateCount > 0
            ? $"Windows Update found {updateCount} applicable driver update(s).{caveat}"
            : $"No applicable driver updates found via Windows Update.{caveat}";
    }

    public void ApplyWindowsUpdateCheckFailure()
    {
        IsCheckingWindowsUpdate = false;
        WindowsUpdateResultText = "Could not check Windows Update (no connection, or the service is unavailable).";
    }

    public HealthStatus Status => _dna.Status;
    public string StatusBrushKey => Status switch
    {
        HealthStatus.Green => "StatusGreenBrush",
        HealthStatus.Yellow => "StatusYellowBrush",
        HealthStatus.Red => "StatusRedBrush",
        _ => "StatusGreenBrush",
    };

    public bool HasAdvancedFields => AdvancedFields.Count > 0;

    public IReadOnlyList<FieldRow> BasicFields { get; }
    public IReadOnlyList<FieldRow> AdvancedFields { get; }

    private List<FieldRow> BuildBasicFields()
    {
        var rows = new List<FieldRow>();

        switch (_dna.Basic)
        {
            case CpuBasic cpu:
                rows.Add(new FieldRow { Label = "Manufacturer", Value = cpu.Manufacturer });
                rows.Add(new FieldRow { Label = "Cores / Threads", Value = $"{cpu.Cores} / {cpu.Threads}" });
                rows.Add(new FieldRow { Label = "Base Clock", Value = cpu.BaseClockGhz.HasValue ? $"{cpu.BaseClockGhz.Value:0.##} GHz" : "Unknown" });
                break;
            case GpuBasic gpu:
                rows.Add(new FieldRow { Label = "Manufacturer", Value = gpu.Manufacturer });
                rows.Add(new FieldRow { Label = "VRAM", Value = gpu.VramAmountGb.HasValue ? $"{gpu.VramAmountGb.Value:0.#} GB" : "Unknown" });
                rows.Add(new FieldRow { Label = "Driver Version", Value = gpu.DriverVersion ?? "Unknown" });
                break;
            case MemoryBasic mem:
                rows.Add(new FieldRow { Label = "Total Capacity", Value = mem.TotalCapacityGb.HasValue ? $"{mem.TotalCapacityGb.Value:0.#} GB" : "Unknown" });
                rows.Add(new FieldRow { Label = "Type", Value = mem.Type });
                rows.Add(new FieldRow { Label = "Speed", Value = $"{mem.SpeedMts} MT/s" });
                rows.Add(new FieldRow { Label = "Slots Used", Value = $"{mem.SlotsUsed} / {(mem.SlotsTotal.HasValue ? mem.SlotsTotal.Value.ToString() : "Unknown")}" });
                break;
            case StorageBasic storage:
                rows.Add(new FieldRow { Label = "Capacity", Value = storage.CapacityGb.HasValue ? $"{storage.CapacityGb.Value:0.#} GB" : "Unknown" });
                rows.Add(new FieldRow { Label = "Type", Value = storage.Type });
                rows.Add(new FieldRow { Label = "Free Space", Value = storage.FreeSpacePct > 0 ? $"{storage.FreeSpacePct:0.#}%" : "Unknown" });
                break;
            case MotherboardBasic mobo:
                rows.Add(new FieldRow { Label = "Manufacturer", Value = mobo.Manufacturer });
                rows.Add(new FieldRow { Label = "Model", Value = mobo.Model });
                rows.Add(new FieldRow { Label = "BIOS Version", Value = mobo.BiosVersion ?? "Unknown" });
                rows.Add(new FieldRow { Label = "Chipset", Value = mobo.Chipset ?? "Unknown" });
                break;
            case NetworkBasic net:
                rows.Add(new FieldRow { Label = "Connection Type", Value = net.ConnectionType });
                rows.Add(new FieldRow { Label = "Current Speed", Value = net.CurrentSpeedMbps.HasValue ? $"{net.CurrentSpeedMbps:0} Mbps" : "Unknown" });
                break;
            case OsBasic os:
                rows.Add(new FieldRow { Label = "Version", Value = os.Version });
                rows.Add(new FieldRow { Label = "Build", Value = os.BuildNumber });
                rows.Add(new FieldRow { Label = "Install Date", Value = os.InstallDate?.ToString("yyyy-MM-dd") ?? "Unknown" });
                break;
        }

        return rows;
    }

    private List<FieldRow> BuildAdvancedFields()
    {
        var rows = new List<FieldRow>();

        switch (_dna.Advanced)
        {
            case CpuAdvanced cpu:
                AddIf(rows, "Architecture", cpu.ArchitectureGeneration);
                AddIf(rows, "Socket", cpu.Socket);
                if (cpu.CurrentTempC.HasValue) rows.Add(new FieldRow { Label = "Current Temp", Value = $"{cpu.CurrentTempC:0.#} °C" });
                if (cpu.CurrentUtilizationPct.HasValue) rows.Add(new FieldRow { Label = "Utilization", Value = $"{cpu.CurrentUtilizationPct:0.#}%" });
                if (cpu.CurrentLiveClockGhz.HasValue) rows.Add(new FieldRow { Label = "Live Clock", Value = $"{cpu.CurrentLiveClockGhz:0.##} GHz" });
                if (cpu.Cache is { } cache)
                {
                    if (cache.L2Kb.HasValue) rows.Add(new FieldRow { Label = "L2 Cache", Value = $"{cache.L2Kb} KB" });
                    if (cache.L3Kb.HasValue) rows.Add(new FieldRow { Label = "L3 Cache", Value = $"{cache.L3Kb} KB" });
                }
                if (cpu.VirtualizationSupport.HasValue) rows.Add(new FieldRow { Label = "Virtualization", Value = cpu.VirtualizationSupport.Value ? "Enabled" : "Disabled" });
                break;

            case GpuAdvanced gpu:
                if (gpu.CoreClockMhz.HasValue) rows.Add(new FieldRow { Label = "Core Clock", Value = $"{gpu.CoreClockMhz:0} MHz" });
                if (gpu.BoostClockMhz.HasValue) rows.Add(new FieldRow { Label = "Boost Clock", Value = $"{gpu.BoostClockMhz:0} MHz" });
                AddIf(rows, "Memory Type", gpu.MemoryType);
                if (gpu.MemoryClockMhz.HasValue) rows.Add(new FieldRow { Label = "Memory Clock", Value = $"{gpu.MemoryClockMhz:0} MHz" });
                if (gpu.PcieGeneration.HasValue) rows.Add(new FieldRow { Label = "PCIe Generation", Value = $"Gen {gpu.PcieGeneration}" });
                if (gpu.PcieLaneWidth.HasValue) rows.Add(new FieldRow { Label = "PCIe Lanes", Value = $"x{gpu.PcieLaneWidth}" });
                if (gpu.CurrentTempC.HasValue) rows.Add(new FieldRow { Label = "Current Temp", Value = $"{gpu.CurrentTempC:0.#} °C" });
                if (gpu.CurrentUtilizationPct.HasValue) rows.Add(new FieldRow { Label = "Utilization", Value = $"{gpu.CurrentUtilizationPct:0.#}%" });
                if (gpu.CurrentVramUsageGb.HasValue) rows.Add(new FieldRow { Label = "VRAM in Use", Value = $"{gpu.CurrentVramUsageGb:0.#} GB" });
                // Driver Date intentionally not repeated here — the generic Driver Info footer
                // below (appended to every DNA's Advanced view) already shows it once.
                if (gpu.ConnectedOutputsActive.HasValue) rows.Add(new FieldRow { Label = "Active Outputs", Value = $"{gpu.ConnectedOutputsActive}" });
                break;

            case MemoryAdvanced mem:
                if (mem.RatedSpeedMts.HasValue) rows.Add(new FieldRow { Label = "Rated Speed", Value = $"{mem.RatedSpeedMts} MT/s" });
                if (mem.ActualSpeedMts.HasValue) rows.Add(new FieldRow { Label = "Actual Speed", Value = $"{mem.ActualSpeedMts} MT/s" });
                AddIf(rows, "Channel Mode", mem.ChannelMode);
                if (mem.TimingsCl.HasValue) rows.Add(new FieldRow { Label = "CAS Latency", Value = $"CL{mem.TimingsCl}" });
                if (mem.PerModule != null)
                {
                    foreach (var (module, index) in mem.PerModule.Select((m, i) => (m, i)))
                    {
                        var sizeLabel = module.SizeGb.HasValue ? $"{module.SizeGb.Value:0.#} GB" : "Unknown size";
                        rows.Add(new FieldRow { Label = $"Module {index + 1}", Value = $"{sizeLabel} — {module.Manufacturer ?? "Unknown"} ({module.PartNumber ?? "unknown part"})" });
                    }
                }
                break;

            case StorageAdvanced storage:
                AddIf(rows, "Interface", storage.Interface);
                if (storage.RatedSpeedMbps.HasValue) rows.Add(new FieldRow { Label = "Rated Speed", Value = $"{storage.RatedSpeedMbps:0} Mbps" });
                if (storage.SmartHealthPct.HasValue) rows.Add(new FieldRow { Label = "SMART Health", Value = $"{storage.SmartHealthPct:0.#}%" });
                if (storage.CurrentTempC.HasValue) rows.Add(new FieldRow { Label = "Current Temp", Value = $"{storage.CurrentTempC:0.#} °C" });
                if (storage.Partitions != null)
                {
                    foreach (var partition in storage.Partitions)
                    {
                        var partitionCapacityLabel = partition.CapacityGb.HasValue ? $"{partition.CapacityGb.Value:0.#} GB" : "Unknown capacity";
                        rows.Add(new FieldRow { Label = $"Partition {partition.DriveLetter}", Value = $"{partitionCapacityLabel}, {partition.FreeSpacePct:0.#}% free" });
                    }
                }
                break;

            case MotherboardAdvanced mobo:
                AddIf(rows, "BIOS Date", mobo.BiosDate);
                AddIf(rows, "Socket", mobo.Socket);
                if (mobo.MemorySupport is { } memSupport)
                {
                    AddIf(rows, "Supported Memory Type", memSupport.Type);
                    if (memSupport.MaxCapacityGb.HasValue) rows.Add(new FieldRow { Label = "Max Memory", Value = $"{memSupport.MaxCapacityGb:0} GB" });
                    if (memSupport.Slots.HasValue) rows.Add(new FieldRow { Label = "Memory Slots", Value = $"{memSupport.Slots}" });
                }
                if (mobo.PcieSlots != null)
                {
                    foreach (var (slot, index) in mobo.PcieSlots.Select((s, i) => (s, i)))
                    {
                        var usage = slot.InUse ? (slot.PopulatedBy ?? "In use") : "Empty";
                        rows.Add(new FieldRow { Label = $"PCIe Slot {index + 1}", Value = $"Gen{slot.Generation} x{slot.PhysicalWidth} — {usage}" });
                    }
                }
                if (mobo.M2Slots.HasValue) rows.Add(new FieldRow { Label = "M.2 Slots", Value = $"{mobo.M2Slots}" });
                break;

            case NetworkAdvanced net:
                AddIf(rows, "IP Address", net.IpAddress);
                AddIf(rows, "MAC Address", net.MacAddress);
                // Driver Version intentionally not repeated here — the generic Driver Info footer
                // below (appended to every DNA's Advanced view) already shows it once.
                if (net.SignalStrengthPct.HasValue) rows.Add(new FieldRow { Label = "Signal Strength", Value = $"{net.SignalStrengthPct}%" });
                if (net.MaxSupportedSpeedMbps.HasValue) rows.Add(new FieldRow { Label = "Max Supported Speed", Value = $"{net.MaxSupportedSpeedMbps:0} Mbps" });
                break;

            case OsAdvanced os:
                if (os.Uptime.HasValue) rows.Add(new FieldRow { Label = "Uptime", Value = $"{os.Uptime.Value.Days}d {os.Uptime.Value.Hours}h {os.Uptime.Value.Minutes}m" });
                AddIf(rows, "Last Update", os.LastUpdateDate?.ToString("yyyy-MM-dd"));
                AddIf(rows, "Activation Status", os.ActivationStatus);
                AddIf(rows, "Architecture", os.Architecture);
                if (os.RebootPending.HasValue) rows.Add(new FieldRow { Label = "Restart Pending", Value = os.RebootPending.Value ? "Yes" : "No" });
                break;
        }

        // When a DNA has no meaningful driver concept (OS, CPU, Memory, Motherboard, Storage — see
        // DriverInfo.NotApplicable's doc comment), simply omit the Driver rows entirely rather than
        // showing an explicit "Driver: Not applicable" placeholder row, which read as clutter/an
        // unfinished feature rather than an intentional "this doesn't apply here" (user feedback).
        if (_dna.Driver.IsApplicable)
        {
            rows.Add(new FieldRow { Label = "Driver Version", Value = _dna.Driver.Version ?? "Unknown" });
            rows.Add(new FieldRow { Label = "Driver Date", Value = _dna.Driver.Date ?? "Unknown" });
            rows.Add(new FieldRow { Label = "Driver Source", Value = _dna.Driver.SourceUrl ?? "Not verified" });
        }

        return rows;
    }

    private static void AddIf(List<FieldRow> rows, string label, string? value)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            rows.Add(new FieldRow { Label = label, Value = value });
        }
    }
}

//*Built with assistance from __Claude Code__ by Anthropic.*
