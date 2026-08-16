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

namespace DeviceDNA.DetectionEngine.Rules;

// Deterministic, non-AI diagnosis rules engine (REQUIREMENTS.md section 5, CLAUDE.md Rules Engine
// Implementation Notes). Each DNA type gets its own evaluation method that inspects the already-built
// Basic/Advanced field objects and returns the full list of fired StatusReasons (including an
// Info-severity "confirmed good" reason when nothing bad fires — every DNA must always report at
// least one reason). Overall Status is derived as the worst severity among the fired reasons.
//
// Rules intentionally SKIP (do not fire, do not guess) whenever their required input data is
// null/unavailable, per REQUIREMENTS.md section 10's no-fabrication principle. This mirrors the
// RULE { applies_to, condition, severity, message, suggestion, confidence } format documented in
// REQUIREMENTS.md section 5.
public static class RulesEngine
{
    // Computes the worst-case HealthStatus for a set of fired reasons: Red > Yellow > Green.
    public static HealthStatus StatusFromReasons(IReadOnlyList<StatusReason> reasons)
    {
        if (reasons.Any(r => r.Severity == ReasonSeverity.Red))
        {
            return HealthStatus.Red;
        }
        if (reasons.Any(r => r.Severity == ReasonSeverity.Yellow))
        {
            return HealthStatus.Yellow;
        }
        return HealthStatus.Green;
    }

    // LibreHardwareMonitor without administrator privileges commonly fails to read certain sensors
    // (notably CPU package/GPU temperature) and reports a literal 0 rather than omitting the field,
    // because the underlying Advanced POCOs use nullable doubles populated unconditionally from
    // whatever sensor value was returned (see DeviceDetectionService). 0 °C is not a plausible
    // running temperature for a CPU/GPU package, so treat it as "no reading" here rather than
    // "an alarmingly cold but technically fine value" — this is a known Phase 1 limitation
    // (elevation required for full sensor access), not a real reading of 0.
    private const double ImplausibleZeroTempC = 0.0;

    private static bool HasPlausibleTemp(double? temp) => temp.HasValue && temp.Value != ImplausibleZeroTempC;

    private static StatusReason Info(string message) => new()
    {
        Message = message,
        Severity = ReasonSeverity.Info,
        Suggestion = null,
        Confidence = Confidence.High,
    };

    // ---------- CPU ----------

    public static List<StatusReason> EvaluateCpu(CpuAdvanced advanced)
    {
        var reasons = new List<StatusReason>();

        if (HasPlausibleTemp(advanced.CurrentTempC))
        {
            var temp = advanced.CurrentTempC!.Value;
            if (temp >= HealthThresholds.CpuTempRedC)
            {
                reasons.Add(new StatusReason
                {
                    Message = $"CPU temperature is {temp:0.#} °C, near the throttle point under sustained load.",
                    ShortReason = $"CPU is running very hot ({temp:0.#} °C).",
                    Severity = ReasonSeverity.Red,
                    Suggestion = "Check case airflow and cooler contact; consider reapplying thermal paste.",
                    Confidence = Confidence.High,
                });
            }
            else if (temp >= HealthThresholds.CpuTempYellowC)
            {
                reasons.Add(new StatusReason
                {
                    Message = $"CPU temperature is {temp:0.#} °C, hotter than typical for sustained load.",
                    ShortReason = $"CPU is running hot ({temp:0.#} °C).",
                    Severity = ReasonSeverity.Yellow,
                    Suggestion = "Check case airflow and cooler contact.",
                    Confidence = Medium(),
                });
            }
            else
            {
                reasons.Add(Info($"CPU temperature is {temp:0.#} °C, within a normal range."));
            }
        }

        if (advanced.CurrentUtilizationPct.HasValue)
        {
            var util = advanced.CurrentUtilizationPct.Value;
            if (util >= HealthThresholds.CpuUtilizationYellowPct)
            {
                reasons.Add(new StatusReason
                {
                    Message = $"CPU utilization is {util:0.#}%, sustained high load.",
                    ShortReason = "sustained high CPU load.",
                    Severity = ReasonSeverity.Yellow,
                    Suggestion = "Check Task Manager for a process consuming unexpectedly high CPU.",
                    Confidence = Confidence.Medium,
                });
            }
            else
            {
                reasons.Add(Info($"CPU utilization is {util:0.#}%, within a normal range."));
            }
        }

        if (advanced.VirtualizationSupport.HasValue)
        {
            if (advanced.VirtualizationSupport.Value)
            {
                reasons.Add(Info("Virtualization support is enabled in firmware."));
            }
            else
            {
                reasons.Add(new StatusReason
                {
                    Message = "Virtualization support is disabled in firmware.",
                    ShortReason = "virtualization is disabled.",
                    Severity = ReasonSeverity.Yellow,
                    Suggestion = "Enable virtualization (Intel VT-x / AMD-V) in BIOS/UEFI if you use virtual machines, WSL2, or Android emulation.",
                    Confidence = Confidence.High,
                });
            }
        }

        // "Running well below rated boost under load" requires a reliable rated-boost-clock source,
        // which Phase 2 left null (CpuAdvanced.BoostClockGhz is not populated) — skip rather than guess.

        if (reasons.Count == 0)
        {
            reasons.Add(Info("CPU detected successfully; no issues found."));
        }

        return reasons;
    }

    // ---------- GPU ----------

    public static List<StatusReason> EvaluateGpu(GpuAdvanced advanced)
    {
        var reasons = new List<StatusReason>();

        if (HasPlausibleTemp(advanced.CurrentTempC))
        {
            var temp = advanced.CurrentTempC!.Value;
            if (temp >= HealthThresholds.GpuTempRedC)
            {
                reasons.Add(new StatusReason
                {
                    Message = $"GPU temperature is {temp:0.#} °C, near the throttle point under sustained load.",
                    ShortReason = $"GPU is running very hot ({temp:0.#} °C).",
                    Severity = ReasonSeverity.Red,
                    Suggestion = "Check case airflow and GPU fan curve; consider reapplying thermal paste on older cards.",
                    Confidence = Confidence.High,
                });
            }
            else if (temp >= HealthThresholds.GpuTempYellowC)
            {
                reasons.Add(new StatusReason
                {
                    Message = $"GPU temperature is {temp:0.#} °C, hotter than typical for sustained load.",
                    ShortReason = $"GPU is running hot ({temp:0.#} °C).",
                    Severity = ReasonSeverity.Yellow,
                    Suggestion = "Check case airflow and GPU fan curve.",
                    Confidence = Confidence.Medium,
                });
            }
            else
            {
                reasons.Add(Info($"GPU temperature is {temp:0.#} °C, within a normal range."));
            }
        }

        if (advanced.CurrentUtilizationPct.HasValue)
        {
            var util = advanced.CurrentUtilizationPct.Value;
            if (util >= HealthThresholds.GpuUtilizationYellowPct)
            {
                reasons.Add(new StatusReason
                {
                    Message = $"GPU utilization is {util:0.#}%, sustained high load.",
                    ShortReason = "sustained high GPU load.",
                    Severity = ReasonSeverity.Yellow,
                    Suggestion = "Expected during gaming/rendering; check for a stuck background process if idle otherwise.",
                    Confidence = Confidence.Medium,
                });
            }
            else
            {
                reasons.Add(Info($"GPU utilization is {util:0.#}%, within a normal range."));
            }
        }

        // Driver staleness deferred: no reliable "latest driver version" data source in this app
        // (would require an internet lookup / fabricated comparison) — skip per REQUIREMENTS.md
        // section 10's no-fabrication principle.

        if (reasons.Count == 0)
        {
            reasons.Add(Info("GPU detected successfully; no issues found."));
        }

        return reasons;
    }

    // ---------- Memory ----------

    public static List<StatusReason> EvaluateMemory(MemoryAdvanced advanced)
    {
        var reasons = new List<StatusReason>();

        if (advanced.RatedSpeedMts.HasValue && advanced.ActualSpeedMts.HasValue)
        {
            if (advanced.ActualSpeedMts.Value < advanced.RatedSpeedMts.Value)
            {
                reasons.Add(new StatusReason
                {
                    Message = $"Your RAM is rated for {advanced.RatedSpeedMts} MT/s but is currently running at {advanced.ActualSpeedMts} MT/s.",
                    ShortReason = $"RAM running below rated speed ({advanced.ActualSpeedMts} of {advanced.RatedSpeedMts} MT/s).",
                    Severity = ReasonSeverity.Yellow,
                    Suggestion = "Check your BIOS memory profile (XMP/EXPO) settings.",
                    Confidence = Confidence.High,
                });
            }
            else
            {
                reasons.Add(Info($"RAM is running at its full rated speed of {advanced.RatedSpeedMts} MT/s."));
            }
        }

        if (advanced.PerModule is { Count: > 1 } modules)
        {
            // Only compare modules whose size is actually known — a module with an unknown size
            // (SizeGb null, WMI capacity lookup failed for that module) must not be treated as
            // "distinct" from the others, since that would fabricate a mismatch finding from missing
            // data rather than a genuine size difference (or, symmetrically, mask a real mismatch if
            // two different-sized modules both happened to have unknown size and both got excluded).
            var modulesWithKnownSize = modules.Where(m => m.SizeGb.HasValue).ToList();
            var distinctManufacturers = modules.Select(m => m.Manufacturer ?? "Unknown").Distinct().Count();
            var distinctSizes = modulesWithKnownSize.Select(m => m.SizeGb!.Value).Distinct().Count();
            var sizeComparisonIsReliable = modulesWithKnownSize.Count == modules.Count;

            if (distinctManufacturers > 1 || (sizeComparisonIsReliable && distinctSizes > 1))
            {
                reasons.Add(new StatusReason
                {
                    Message = "Installed memory modules do not match (different manufacturer or capacity).",
                    ShortReason = "memory modules don't match.",
                    Severity = ReasonSeverity.Yellow,
                    Suggestion = "Mismatched modules can prevent dual-channel mode or stable XMP/EXPO speeds; consider using matched modules.",
                    Confidence = Confidence.Medium,
                });
            }
            else if (sizeComparisonIsReliable)
            {
                reasons.Add(Info("Installed memory modules match (same manufacturer and capacity)."));
            }
            // else: one or more modules' capacity is unknown and manufacturers otherwise match —
            // skip rather than guess at a verdict for the capacity half of this check.
        }

        if (reasons.Count == 0)
        {
            reasons.Add(Info("Memory detected successfully; no issues found."));
        }

        return reasons;
    }

    // ---------- Storage ----------

    public static List<StatusReason> EvaluateStorage(StorageBasic basic, StorageAdvanced advanced)
    {
        var reasons = new List<StatusReason>();

        // FreeSpacePct is 0 both when genuinely full and when unknown (Phase 2 default); only
        // evaluate when partitions were actually resolved, since a bare 0 with no partitions means
        // "no data" rather than "0% free" (see DeviceDetectionService: freeSpacePct defaults to 0
        // when partitions.Count == 0).
        if (advanced.Partitions is { Count: > 0 })
        {
            if (basic.FreeSpacePct < HealthThresholds.StorageFreeSpaceYellowPct)
            {
                reasons.Add(new StatusReason
                {
                    Message = $"Only {basic.FreeSpacePct:0.#}% free space remaining on {basic.Model}.",
                    ShortReason = $"low on free space ({basic.FreeSpacePct:0.#}% remaining).",
                    Severity = ReasonSeverity.Yellow,
                    Suggestion = "Free up disk space or move files to another drive.",
                    Confidence = Confidence.High,
                });
            }
            else
            {
                reasons.Add(Info($"{basic.FreeSpacePct:0.#}% free space remaining, plenty of headroom."));
            }
        }

        if (advanced.SmartHealthPct.HasValue)
        {
            // Direct equality, not a threshold comparison — see HealthThresholds.cs: this value is
            // binary (0 or 100), never in between.
            if (advanced.SmartHealthPct.Value == 0)
            {
                reasons.Add(new StatusReason
                {
                    Message = $"SMART reports a predicted failure for {basic.Model}.",
                    ShortReason = "drive failure predicted (SMART).",
                    Severity = ReasonSeverity.Red,
                    Suggestion = "Back up your data immediately and plan to replace this drive.",
                    Confidence = Confidence.High,
                });
            }
            else
            {
                reasons.Add(Info("SMART health check passed."));
            }
        }

        if (reasons.Count == 0)
        {
            reasons.Add(Info("Storage detected successfully; no issues found."));
        }

        return reasons;
    }

    // ---------- Motherboard ----------

    public static List<StatusReason> EvaluateMotherboard(MotherboardAdvanced advanced)
    {
        var reasons = new List<StatusReason>();

        // PCIe slot data (Phase 2 note: PcieSlots is typically null — not sourceable via WMI/LHM
        // without vendor/ACPI-specific tooling). Only evaluate if it was actually populated.
        if (advanced.PcieSlots is { Count: > 0 } slots)
        {
            var underNegotiated = slots.Where(s =>
                s.InUse && s.NegotiatedWidth.HasValue && s.NegotiatedWidth.Value < s.PhysicalWidth).ToList();

            if (underNegotiated.Count > 0)
            {
                foreach (var slot in underNegotiated)
                {
                    reasons.Add(new StatusReason
                    {
                        Message = $"A PCIe slot ({slot.PopulatedBy ?? "device"}) is negotiating at x{slot.NegotiatedWidth} instead of its physical x{slot.PhysicalWidth}.",
                        ShortReason = $"a PCIe slot is negotiating below its physical width.",
                        Severity = ReasonSeverity.Yellow,
                        Suggestion = "Reseat the card and check for shared-lane conflicts with other populated slots.",
                        Confidence = Confidence.Medium,
                    });
                }
            }
            else
            {
                reasons.Add(Info("PCIe slots are negotiating at their full physical width."));
            }
        }

        // BIOS update availability is explicitly out of scope: it would require fabricating a
        // "latest BIOS version" from nowhere (no reliable local data source), which violates the
        // no-fabrication principle in REQUIREMENTS.md section 10. Skipped intentionally.

        if (reasons.Count == 0)
        {
            reasons.Add(Info("Motherboard detected successfully; no issues found."));
        }

        return reasons;
    }

    // ---------- Network ----------

    // The disconnected/no-IP red check is evaluated separately in DeviceDetectionService (existing
    // Phase 1 logic, kept as-is). This method only adds the additional "below max supported speed"
    // check and the confirmed-good reason for the connected case.
    public static List<StatusReason> EvaluateNetworkConnected(NetworkBasic basic, NetworkAdvanced advanced)
    {
        var reasons = new List<StatusReason>();

        if (basic.CurrentSpeedMbps.HasValue && advanced.MaxSupportedSpeedMbps.HasValue)
        {
            if (basic.CurrentSpeedMbps.Value < advanced.MaxSupportedSpeedMbps.Value)
            {
                reasons.Add(new StatusReason
                {
                    Message = $"Connected at {basic.CurrentSpeedMbps:0} Mbps, below the adapter's maximum supported speed of {advanced.MaxSupportedSpeedMbps:0} Mbps.",
                    ShortReason = $"below max link speed ({basic.CurrentSpeedMbps:0} of {advanced.MaxSupportedSpeedMbps:0} Mbps).",
                    Severity = ReasonSeverity.Yellow,
                    Suggestion = "Check cabling/switch port speed, or Wi-Fi band/channel congestion.",
                    Confidence = Confidence.Medium,
                });
            }
            else
            {
                reasons.Add(Info($"Connected at the adapter's maximum supported speed of {advanced.MaxSupportedSpeedMbps:0} Mbps."));
            }
        }

        if (reasons.Count == 0)
        {
            reasons.Add(Info("Network adapter connected successfully."));
        }

        return reasons;
    }

    // ---------- OS ----------

    public static List<StatusReason> EvaluateOs(OsAdvanced advanced)
    {
        var reasons = new List<StatusReason>();

        // LastUpdateDate: Phase 2's Win32_QuickFixEngineering-based lookup often returns null (it
        // only reflects classic hotfixes, not modern cumulative updates), so we cannot tell whether
        // updates are genuinely "pending" without fabricating that verdict — intentionally skipped,
        // see BACKLOG.md.
        //
        // ActivationStatus: "Unlicensed" is an unambiguous real problem worth surfacing (red).
        // Every other non-null value — "Licensed" and the various grace-period states — is a
        // legitimate, non-actionable state, not an issue; judging a grace period as "red" would risk
        // a false alarm the user can't act on, so those map to the confirmed-good path rather than a
        // guessed verdict. A null ActivationStatus (WMI query failed/unavailable) is likewise treated
        // as no signal, not an inferred problem.
        if (advanced.ActivationStatus == "Unlicensed")
        {
            reasons.Add(new StatusReason
            {
                Message = "Windows is not activated.",
                ShortReason = "Windows is not activated.",
                Severity = ReasonSeverity.Red,
                Suggestion = "Go to Settings > System > Activation to activate Windows.",
                Confidence = Confidence.High,
            });
        }
        else if (advanced.ActivationStatus is not null)
        {
            reasons.Add(Info($"Windows activation status: {advanced.ActivationStatus}."));
        }

        // RebootPending: a real, locally-observable signal (two well-documented registry keys —
        // see DeviceDetectionService.GetRebootPending), narrower than full "updates available"
        // (which would require a live Windows Update/WSUS network call and was ruled out — see
        // BACKLOG.md). Worded precisely as "a reboot is pending," not implied general update
        // currency, since that's specifically and only what this signal actually confirms.
        if (advanced.RebootPending == true)
        {
            reasons.Add(new StatusReason
            {
                Message = "A restart is pending to finish applying a Windows update.",
                ShortReason = "a restart is pending.",
                Severity = ReasonSeverity.Yellow,
                Suggestion = "Restart your computer to complete the update.",
                Confidence = Confidence.High,
            });
        }

        if (reasons.Count == 0)
        {
            reasons.Add(Info("OS detected successfully; no issues found."));
        }

        return reasons;
    }

    private static Confidence Medium() => Confidence.Medium;
}

//*Built with assistance from __Claude Code__ by Anthropic.*
