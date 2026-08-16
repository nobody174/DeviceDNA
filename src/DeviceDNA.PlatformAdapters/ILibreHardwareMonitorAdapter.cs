//
// DeviceDNA
// Author:  nobody174 (nobodylearn174@gmail.com)
// Repo:    https://github.com/nobody174/DeviceDNA
// Patreon: https://www.patreon.com/c/Nobody174
// License: All rights reserved (c) 2026 nobody174
// "It's never too late to give up!"
//

namespace DeviceDNA.PlatformAdapters;

// One Computer.Open() call feeds sensor readings into multiple DNAs (CPU, GPU, Storage, Motherboard) —
// see CLAUDE.md's "organize by data source, not by DNA type" rule. Detection Engine filters by HardwareType.
public interface ILibreHardwareMonitorAdapter
{
    IReadOnlyList<RawSensorReading> ReadAllSensors();
}

//*Built with assistance from __Claude Code__ by Anthropic.*
