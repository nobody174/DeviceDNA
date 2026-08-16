//
// DeviceDNA
// Author:  nobody174 (nobodylearn174@gmail.com)
// Repo:    https://github.com/nobody174/DeviceDNA
// Patreon: https://www.patreon.com/c/Nobody174
// License: All rights reserved (c) 2026 nobody174
// "It's never too late to give up!"
//

using System.Text.Json;
using Microsoft.Data.Sqlite;
using DeviceDNA.Model;

namespace DeviceDNA.Application;

// Local-only SQLite persistence for scan snapshots (REQUIREMENTS.md section 10: no cloud, no telemetry).
// Two tables: "scans" (one row per completed scan, metadata + overall status) and "dna_snapshots"
// (one row per DNA within that scan, with a JSON blob of the full Basic/Advanced/StatusReasons for that
// DNA — pragmatic given the DNA model's per-type Basic/Advanced shape, rather than hand-rolling a fully
// normalized column per field per DNA type, per the Phase 4 task brief).
//
// Every real scan the app performs is saved here (called from DeviceScanService), giving the History
// page (REQUIREMENTS.md section 7 item 5) and Changes view (item 4) something to read from.
public class ScanHistoryRepository
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = false };

    private readonly string _connectionString;

    public ScanHistoryRepository() : this(DefaultDatabasePath())
    {
    }

    public ScanHistoryRepository(string databasePath)
    {
        var directory = Path.GetDirectoryName(databasePath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        _connectionString = new SqliteConnectionStringBuilder { DataSource = databasePath }.ToString();
        EnsureSchema();
    }

    // %LOCALAPPDATA%\DeviceDNA\devicedna.db — local app-data location per the Phase 4 task brief.
    public static string DefaultDatabasePath() =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "DeviceDNA", "devicedna.db");

    private void EnsureSchema()
    {
        using var connection = OpenConnection();
        using var command = connection.CreateCommand();
        command.CommandText =
            """
            CREATE TABLE IF NOT EXISTS scans (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                timestamp_utc TEXT NOT NULL,
                hostname TEXT NOT NULL,
                os_summary TEXT NOT NULL,
                form_factor TEXT NOT NULL,
                overall_status TEXT NOT NULL
            );

            CREATE TABLE IF NOT EXISTS dna_snapshots (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                scan_id INTEGER NOT NULL REFERENCES scans(id) ON DELETE CASCADE,
                dna_type TEXT NOT NULL,
                name TEXT NOT NULL,
                manufacturer TEXT NOT NULL,
                summary TEXT NOT NULL,
                status TEXT NOT NULL,
                data_json TEXT NOT NULL
            );

            CREATE INDEX IF NOT EXISTS idx_dna_snapshots_scan_id ON dna_snapshots(scan_id);
            """;
        command.ExecuteNonQuery();
    }

    private SqliteConnection OpenConnection()
    {
        var connection = new SqliteConnection(_connectionString);
        connection.Open();
        using var pragma = connection.CreateCommand();
        pragma.CommandText = "PRAGMA foreign_keys = ON;";
        pragma.ExecuteNonQuery();
        return connection;
    }

    // Persists a completed Device scan. Returns the new scan's database id.
    public long SaveScan(Device device)
    {
        var overallStatus = device.Dnas.Count == 0
            ? HealthStatus.Yellow
            : device.Dnas.Max(d => d.Status);

        using var connection = OpenConnection();
        using var transaction = connection.BeginTransaction();

        long scanId;
        using (var insertScan = connection.CreateCommand())
        {
            insertScan.Transaction = transaction;
            insertScan.CommandText =
                """
                INSERT INTO scans (timestamp_utc, hostname, os_summary, form_factor, overall_status)
                VALUES ($timestamp, $hostname, $osSummary, $formFactor, $overallStatus);
                SELECT last_insert_rowid();
                """;
            insertScan.Parameters.AddWithValue("$timestamp", DateTime.UtcNow.ToString("O"));
            insertScan.Parameters.AddWithValue("$hostname", device.Hostname);
            insertScan.Parameters.AddWithValue("$osSummary", device.OsSummary);
            insertScan.Parameters.AddWithValue("$formFactor", device.FormFactor);
            insertScan.Parameters.AddWithValue("$overallStatus", overallStatus.ToString());
            scanId = (long)insertScan.ExecuteScalar()!;
        }

        foreach (var dna in device.Dnas)
        {
            using var insertDna = connection.CreateCommand();
            insertDna.Transaction = transaction;
            insertDna.CommandText =
                """
                INSERT INTO dna_snapshots (scan_id, dna_type, name, manufacturer, summary, status, data_json)
                VALUES ($scanId, $dnaType, $name, $manufacturer, $summary, $status, $dataJson);
                """;
            insertDna.Parameters.AddWithValue("$scanId", scanId);
            insertDna.Parameters.AddWithValue("$dnaType", dna.Type.ToString());
            insertDna.Parameters.AddWithValue("$name", dna.Name);
            insertDna.Parameters.AddWithValue("$manufacturer", dna.Manufacturer);
            insertDna.Parameters.AddWithValue("$summary", dna.Summary);
            insertDna.Parameters.AddWithValue("$status", dna.Status.ToString());
            insertDna.Parameters.AddWithValue("$dataJson", JsonSerializer.Serialize(DnaSnapshotData.FromDna(dna), JsonOptions));
            insertDna.ExecuteNonQuery();
        }

        transaction.Commit();
        return scanId;
    }

    // Lists past scans newest-first, for the History page. Optionally filters to scans that contain
    // at least one DNA of the given type (REQUIREMENTS.md section 7 item 5: "filterable by DNA").
    public IReadOnlyList<ScanSnapshotSummary> ListScans(DnaType? filterByDnaType = null)
    {
        using var connection = OpenConnection();
        using var command = connection.CreateCommand();

        command.CommandText = filterByDnaType is null
            ? """
              SELECT id, timestamp_utc, hostname, os_summary, overall_status
              FROM scans
              ORDER BY timestamp_utc DESC;
              """
            : """
              SELECT DISTINCT s.id, s.timestamp_utc, s.hostname, s.os_summary, s.overall_status
              FROM scans s
              JOIN dna_snapshots d ON d.scan_id = s.id
              WHERE d.dna_type = $dnaType
              ORDER BY s.timestamp_utc DESC;
              """;

        if (filterByDnaType is not null)
        {
            command.Parameters.AddWithValue("$dnaType", filterByDnaType.Value.ToString());
        }

        var results = new List<ScanSnapshotSummary>();
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            // A single malformed row (e.g. from a future schema change touching an old DB, or
            // manual DB tampering) should not take down the whole History list — skip that row
            // rather than let FormatException/ArgumentException from DateTime.Parse/Enum.Parse
            // propagate and crash the caller.
            try
            {
                results.Add(new ScanSnapshotSummary
                {
                    ScanId = reader.GetInt64(0),
                    Timestamp = DateTime.Parse(reader.GetString(1)).ToUniversalTime(),
                    Hostname = reader.GetString(2),
                    OsSummary = reader.GetString(3),
                    OverallStatus = Enum.Parse<HealthStatus>(reader.GetString(4)),
                });
            }
            catch (Exception ex) when (ex is FormatException or ArgumentException)
            {
                continue;
            }
        }

        return results;
    }

    // Loads a specific past scan back into a Device. Reconstructs each Dna's key fields plus the
    // full Basic/Advanced/StatusReasons/Driver payload from the stored JSON blob.
    public Device? LoadScan(long scanId)
    {
        using var connection = OpenConnection();

        string hostname, osSummary, formFactor;
        DateTime timestamp;
        using (var scanCommand = connection.CreateCommand())
        {
            scanCommand.CommandText = "SELECT timestamp_utc, hostname, os_summary, form_factor FROM scans WHERE id = $scanId;";
            scanCommand.Parameters.AddWithValue("$scanId", scanId);
            using var reader = scanCommand.ExecuteReader();
            if (!reader.Read())
            {
                return null;
            }

            try
            {
                timestamp = DateTime.Parse(reader.GetString(0)).ToUniversalTime();
            }
            catch (FormatException)
            {
                // A malformed timestamp on the scan row itself means the scan can't be meaningfully
                // reconstructed (its DNAs are all attributed to this timestamp) — treat as not found
                // rather than crash the caller.
                return null;
            }
            hostname = reader.GetString(1);
            osSummary = reader.GetString(2);
            formFactor = reader.GetString(3);
        }

        var dnas = new List<Dna>();
        using (var dnaCommand = connection.CreateCommand())
        {
            dnaCommand.CommandText = "SELECT dna_type, name, manufacturer, summary, status, data_json FROM dna_snapshots WHERE scan_id = $scanId ORDER BY id;";
            dnaCommand.Parameters.AddWithValue("$scanId", scanId);
            using var reader = dnaCommand.ExecuteReader();
            while (reader.Read())
            {
                // Same defensive skip as ListScans above: one corrupted/unparseable DNA row (bad
                // enum value, malformed JSON from a future model change against an old DB) should
                // not prevent the rest of the scan's DNAs — or the scan itself — from loading.
                try
                {
                    var dnaType = Enum.Parse<DnaType>(reader.GetString(0));
                    var name = reader.GetString(1);
                    var manufacturer = reader.GetString(2);
                    var summary = reader.GetString(3);
                    var status = Enum.Parse<HealthStatus>(reader.GetString(4));
                    var dataJson = reader.GetString(5);

                    var data = JsonSerializer.Deserialize<DnaSnapshotData>(dataJson, JsonOptions);
                    if (data is null)
                    {
                        continue;
                    }

                    dnas.Add(data.ToDna(dnaType, name, manufacturer, summary, status, timestamp));
                }
                catch (Exception ex) when (ex is FormatException or ArgumentException or JsonException)
                {
                    continue;
                }
            }
        }

        return new Device
        {
            Id = scanId.ToString(),
            Hostname = hostname,
            OsSummary = osSummary,
            FormFactor = formFactor,
            Dnas = dnas,
            ScanHistory = new List<ScanHistoryEntry> { new() { Timestamp = timestamp, SnapshotId = scanId.ToString() } },
        };
    }
}

//*Built with assistance from __Claude Code__ by Anthropic.*
