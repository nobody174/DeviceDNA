//
// DeviceDNA
// Author:  nobody174 (nobodylearn174@gmail.com)
// Repo:    https://github.com/nobody174/DeviceDNA
// Patreon: https://www.patreon.com/c/Nobody174
// License: All rights reserved (c) 2026 nobody174
// "It's never too late to give up!"
//

namespace DeviceDNA.DetectionEngine.Rules;

// Centralized, tunable numeric thresholds used by the rules engine. CLAUDE.md's Rules Engine
// Implementation Notes require thresholds to be named constants in one place, not scattered
// magic numbers, so they can be adjusted after real-world testing on real machines.
public static class HealthThresholds
{
    // CPU temperature (Celsius). CPUs run cooler than GPUs under typical sustained load.
    public const double CpuTempYellowC = 80.0;
    public const double CpuTempRedC = 90.0;

    // CPU sustained utilization (%).
    public const double CpuUtilizationYellowPct = 90.0;

    // GPU temperature (Celsius). GPUs are commonly designed to run hotter than CPUs.
    public const double GpuTempYellowC = 80.0;
    public const double GpuTempRedC = 87.0;

    // GPU sustained utilization (%). Mirrors the CPU pattern.
    public const double GpuUtilizationYellowPct = 90.0;

    // Storage: free space percentage below which capacity pressure becomes a yellow warning.
    // Basic.FreeSpacePct is percent FREE, so "low" free space triggers the warning.
    public const double StorageFreeSpaceYellowPct = 15.0;

    // Storage: SmartHealthPct is coarse pass/fail (100 = passed, 0 = predicted failure), not a
    // graduated percentage — WMI's SMART signal is binary (see DeviceDetectionService). This is
    // intentionally NOT a tunable threshold constant (unlike the others in this file): any value
    // between 1 and 99 would behave identically, since the underlying data only ever produces 0 or
    // 100. RulesEngine checks this via direct equality (SmartHealthPct == 0), not a comparison
    // against a constant here, to avoid implying a precision the data doesn't have.
}

//*Built with assistance from __Claude Code__ by Anthropic.*
