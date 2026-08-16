//
// DeviceDNA
// Author:  nobody174 (nobodylearn174@gmail.com)
// Repo:    https://github.com/nobody174/DeviceDNA
// Patreon: https://www.patreon.com/c/Nobody174
// License: All rights reserved (c) 2026 nobody174
// "It's never too late to give up!"
//

using System.Text.Json;
using System.Text.Json.Serialization;
using DeviceDNA.Model;
using DeviceDNA.Model.Dnas;

namespace DeviceDNA.Application;

// JSON-serializable payload stored per DNA per scan (the "data_json" column in dna_snapshots).
// Basic/Advanced are typed as JsonElement here (rather than the Dna model's untyped `object`) so
// System.Text.Json can round-trip them without needing type-discriminator plumbing; ToDna() below
// re-deserializes each into its concrete per-DNA-type Basic/Advanced class based on DnaType.
public class DnaSnapshotData
{
    public JsonElement? Basic { get; set; }
    public JsonElement? Advanced { get; set; }
    public List<StoredStatusReason> StatusReasons { get; set; } = new();
    public StoredDriverInfo? Driver { get; set; }

    public static DnaSnapshotData FromDna(Dna dna) => new()
    {
        Basic = JsonSerializer.SerializeToElement(dna.Basic),
        Advanced = dna.Advanced is null ? null : JsonSerializer.SerializeToElement(dna.Advanced),
        StatusReasons = dna.StatusReasons.Select(r => new StoredStatusReason
        {
            Message = r.Message,
            Severity = r.Severity,
            Suggestion = r.Suggestion,
            Confidence = r.Confidence,
        }).ToList(),
        Driver = new StoredDriverInfo
        {
            IsApplicable = dna.Driver.IsApplicable,
            Version = dna.Driver.Version,
            Date = dna.Driver.Date,
            SourceUrl = dna.Driver.SourceUrl,
        },
    };

    public Dna ToDna(DnaType type, string name, string manufacturer, string summary, HealthStatus status, DateTime lastUpdated)
    {
        object basic = Basic is null ? new object() : DeserializeBasic(type, Basic.Value);
        object? advanced = Advanced is null ? null : DeserializeAdvanced(type, Advanced.Value);

        return new Dna
        {
            // A random Guid, not "{type}:{name}" — Name is not guaranteed unique within a type (e.g.
            // two identical disk models), and a collision-prone Id would be a latent bug waiting for
            // any future code to key off Dna.Id (see ScanChangeDetector's Type+Name grouping fix,
            // which had to handle exactly this non-uniqueness explicitly rather than assume it away).
            Id = Guid.NewGuid().ToString(),
            Type = type,
            Name = name,
            Manufacturer = manufacturer,
            Summary = summary,
            Status = status,
            StatusReasons = StatusReasons.Select(r => new StatusReason
            {
                Message = r.Message,
                Severity = r.Severity,
                Suggestion = r.Suggestion,
                Confidence = r.Confidence,
            }).ToList(),
            Basic = basic,
            Advanced = advanced,
            Driver = Driver is null
                ? DriverInfo.NotApplicable
                : new DriverInfo { IsApplicable = Driver.IsApplicable, Version = Driver.Version, Date = Driver.Date, SourceUrl = Driver.SourceUrl },
            LastUpdated = lastUpdated,
        };
    }

    private static object DeserializeBasic(DnaType type, JsonElement element) => type switch
    {
        DnaType.Cpu => element.Deserialize<CpuBasic>()!,
        DnaType.Gpu => element.Deserialize<GpuBasic>()!,
        DnaType.Memory => element.Deserialize<MemoryBasic>()!,
        DnaType.Storage => element.Deserialize<StorageBasic>()!,
        DnaType.Motherboard => element.Deserialize<MotherboardBasic>()!,
        DnaType.Network => element.Deserialize<NetworkBasic>()!,
        DnaType.Os => element.Deserialize<OsBasic>()!,
        _ => new object(),
    };

    private static object DeserializeAdvanced(DnaType type, JsonElement element) => type switch
    {
        DnaType.Cpu => element.Deserialize<CpuAdvanced>()!,
        DnaType.Gpu => element.Deserialize<GpuAdvanced>()!,
        DnaType.Memory => element.Deserialize<MemoryAdvanced>()!,
        DnaType.Storage => element.Deserialize<StorageAdvanced>()!,
        DnaType.Motherboard => element.Deserialize<MotherboardAdvanced>()!,
        DnaType.Network => element.Deserialize<NetworkAdvanced>()!,
        DnaType.Os => element.Deserialize<OsAdvanced>()!,
        _ => new object(),
    };
}

public class StoredStatusReason
{
    public string Message { get; set; } = string.Empty;
    public ReasonSeverity Severity { get; set; }
    public string? Suggestion { get; set; }
    public Confidence Confidence { get; set; }
}

public class StoredDriverInfo
{
    public bool IsApplicable { get; set; }
    public string? Version { get; set; }
    public string? Date { get; set; }
    public string? SourceUrl { get; set; }
}

//*Built with assistance from __Claude Code__ by Anthropic.*
