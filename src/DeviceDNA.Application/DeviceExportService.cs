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

namespace DeviceDNA.Application;

// Serializes a full Device snapshot (all 7 DNAs, Basic/Advanced/StatusReasons/Driver) to JSON.
// Export is always user-initiated (REQUIREMENTS.md section 10) — this service only formats the
// data; the UI layer is responsible for triggering it via an explicit user action (e.g. a button)
// and choosing where the file is written.
public static class DeviceExportService
{
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() },
    };

    public static string ToJson(Device device) => JsonSerializer.Serialize(device, Options);
}

//*Built with assistance from __Claude Code__ by Anthropic.*
