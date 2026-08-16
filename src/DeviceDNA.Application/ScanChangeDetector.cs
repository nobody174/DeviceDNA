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

namespace DeviceDNA.Application;

// One entry in a "what changed" timeline between two scans (REQUIREMENTS.md section 7 item 4).
public class ScanChange
{
    public required string DnaTypeLabel { get; init; }
    public required string DnaName { get; init; }
    public required string Description { get; init; }
}

// Compares two Device snapshots and produces a human-readable diff timeline: driver updates,
// added/removed devices, and a curated set of "a user would actually care about this" field changes
// (REQUIREMENTS.md section 7 item 4). Lives in DeviceDNA.Application rather than DetectionEngine
// because it operates purely on already-detected Model data from two historical snapshots — it is
// business logic over stored results, not a new hardware-detection concern, so it belongs alongside
// DeviceScanService/ScanHistoryRepository rather than the Platform-Adapter-facing DetectionEngine.
//
// Deliberately NOT diffed: constantly-changing/noisy fields a user wouldn't consider a meaningful
// "change" between two scans minutes or hours apart — OS uptime, live CPU/GPU temperature and
// utilization percentages, per-core load, current VRAM usage. These fluctuate on every scan by
// definition and would drown out genuinely interesting changes (driver updates, capacity changes,
// devices appearing/disappearing). See BACKLOG.md for this decision recorded explicitly.
public static class ScanChangeDetector
{
    public static IReadOnlyList<ScanChange> Compare(Device previous, Device current)
    {
        var changes = new List<ScanChange>();

        // Match DNAs by Type + Name. Name is not guaranteed unique within a type (e.g. two identical
        // disk models both named "Samsung 970 EVO 1TB"), so DNAs are grouped by key rather than keyed
        // into a Dictionary directly — a duplicate key must not throw.
        var previousByKey = previous.Dnas.GroupBy(d => (d.Type, d.Name)).ToDictionary(g => g.Key, g => g.ToList());
        var currentByKey = current.Dnas.GroupBy(d => (d.Type, d.Name)).ToDictionary(g => g.Key, g => g.ToList());

        foreach (var (key, currentGroup) in currentByKey)
        {
            var previousCount = previousByKey.TryGetValue(key, out var previousGroup) ? previousGroup.Count : 0;
            for (var i = previousCount; i < currentGroup.Count; i++)
            {
                changes.Add(new ScanChange
                {
                    DnaTypeLabel = currentGroup[i].Type.ToString(),
                    DnaName = currentGroup[i].Name,
                    Description = $"New device detected: {currentGroup[i].Name}.",
                });
            }
        }

        foreach (var (key, previousGroup) in previousByKey)
        {
            var currentCount = currentByKey.TryGetValue(key, out var currentGroup) ? currentGroup.Count : 0;
            for (var i = currentCount; i < previousGroup.Count; i++)
            {
                changes.Add(new ScanChange
                {
                    DnaTypeLabel = previousGroup[i].Type.ToString(),
                    DnaName = previousGroup[i].Name,
                    Description = $"Device no longer detected: {previousGroup[i].Name}.",
                });
            }
        }

        // Field-level comparison only runs for the unambiguous 1:1 case — exactly one DNA with this
        // (Type, Name) on both sides. With no stable per-instance identity available (the persisted
        // snapshot doesn't carry a hardware serial or PNPDeviceID), positionally pairing 2+
        // same-named instances (e.g. previous[0]<->current[0]) would be a guess: WMI/LibreHardware
        // Monitor enumeration order between two separate scans is not guaranteed stable, so two
        // unrelated identically-modeled disks could get cross-matched and produce a confidently
        // wrong "changed" entry. When the count is ambiguous (either side has 2+), skip field
        // comparison for that group entirely — reporting nothing is safer than reporting something
        // false for a diagnostics tool whose whole value is trustworthy output.
        foreach (var (key, currentGroup) in currentByKey)
        {
            if (!previousByKey.TryGetValue(key, out var previousGroup))
            {
                continue;
            }

            if (previousGroup.Count == 1 && currentGroup.Count == 1)
            {
                CompareCommonFields(previousGroup[0], currentGroup[0], changes);
                CompareBasicFields(previousGroup[0], currentGroup[0], changes);
            }
        }

        return changes;
    }

    private static void CompareCommonFields(Dna previousDna, Dna currentDna, List<ScanChange> changes)
    {
        if (previousDna.Status != currentDna.Status)
        {
            changes.Add(new ScanChange
            {
                DnaTypeLabel = currentDna.Type.ToString(),
                DnaName = currentDna.Name,
                Description = $"Health status changed: {previousDna.Status} → {currentDna.Status}.",
            });
        }

        var previousVersion = previousDna.Driver.IsApplicable ? previousDna.Driver.Version : null;
        var currentVersion = currentDna.Driver.IsApplicable ? currentDna.Driver.Version : null;
        if (!string.IsNullOrEmpty(previousVersion) && !string.IsNullOrEmpty(currentVersion) && previousVersion != currentVersion)
        {
            changes.Add(new ScanChange
            {
                DnaTypeLabel = currentDna.Type.ToString(),
                DnaName = currentDna.Name,
                Description = $"Driver updated: {previousVersion} → {currentVersion}.",
            });
        }
    }

    // Curated Basic-tier field comparisons per DNA type — capacity/free-space/speed style fields a
    // user would notice and care about, deliberately excluding live/noisy Advanced-tier readings.
    private static void CompareBasicFields(Dna previousDna, Dna currentDna, List<ScanChange> changes)
    {
        switch (currentDna.Type)
        {
            case DnaType.Storage:
                if (previousDna.Basic is StorageBasic prevStorage && currentDna.Basic is StorageBasic currStorage)
                {
                    var prevPct = Math.Round(prevStorage.FreeSpacePct, 0);
                    var currPct = Math.Round(currStorage.FreeSpacePct, 0);
                    if (prevPct != currPct)
                    {
                        changes.Add(new ScanChange
                        {
                            DnaTypeLabel = currentDna.Type.ToString(),
                            DnaName = currentDna.Name,
                            Description = $"Free space changed from {prevPct}% to {currPct}%.",
                        });
                    }
                }
                break;

            case DnaType.Memory:
                if (previousDna.Basic is MemoryBasic prevMemory && currentDna.Basic is MemoryBasic currMemory
                    && prevMemory.SpeedMts != currMemory.SpeedMts)
                {
                    changes.Add(new ScanChange
                    {
                        DnaTypeLabel = currentDna.Type.ToString(),
                        DnaName = currentDna.Name,
                        Description = $"Memory speed changed: {prevMemory.SpeedMts} MT/s → {currMemory.SpeedMts} MT/s.",
                    });
                }
                break;

            case DnaType.Gpu:
                if (previousDna.Basic is GpuBasic prevGpu && currentDna.Basic is GpuBasic currGpu
                    && !string.IsNullOrEmpty(prevGpu.DriverVersion) && !string.IsNullOrEmpty(currGpu.DriverVersion)
                    && prevGpu.DriverVersion != currGpu.DriverVersion)
                {
                    changes.Add(new ScanChange
                    {
                        DnaTypeLabel = currentDna.Type.ToString(),
                        DnaName = currentDna.Name,
                        Description = $"GPU driver updated: {prevGpu.DriverVersion} → {currGpu.DriverVersion}.",
                    });
                }
                break;

            case DnaType.Network:
                if (previousDna.Basic is NetworkBasic prevNet && currentDna.Basic is NetworkBasic currNet
                    && prevNet.CurrentSpeedMbps.HasValue && currNet.CurrentSpeedMbps.HasValue
                    && prevNet.CurrentSpeedMbps.Value != currNet.CurrentSpeedMbps.Value)
                {
                    changes.Add(new ScanChange
                    {
                        DnaTypeLabel = currentDna.Type.ToString(),
                        DnaName = currentDna.Name,
                        Description = $"Link speed changed: {prevNet.CurrentSpeedMbps} Mbps → {currNet.CurrentSpeedMbps} Mbps.",
                    });
                }
                break;

            case DnaType.Motherboard:
                if (previousDna.Basic is MotherboardBasic prevMobo && currentDna.Basic is MotherboardBasic currMobo
                    && !string.IsNullOrEmpty(prevMobo.BiosVersion) && !string.IsNullOrEmpty(currMobo.BiosVersion)
                    && prevMobo.BiosVersion != currMobo.BiosVersion)
                {
                    changes.Add(new ScanChange
                    {
                        DnaTypeLabel = currentDna.Type.ToString(),
                        DnaName = currentDna.Name,
                        Description = $"BIOS version changed: {prevMobo.BiosVersion} → {currMobo.BiosVersion}.",
                    });
                }
                break;
        }
    }
}

//*Built with assistance from __Claude Code__ by Anthropic.*
