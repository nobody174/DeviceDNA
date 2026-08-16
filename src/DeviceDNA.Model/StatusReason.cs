//
// DeviceDNA
// Author:  nobody174 (nobodylearn174@gmail.com)
// Repo:    https://github.com/nobody174/DeviceDNA
// Patreon: https://www.patreon.com/c/Nobody174
// License: All rights reserved (c) 2026 nobody174
// "It's never too late to give up!"
//

namespace DeviceDNA.Model;

public enum ReasonSeverity
{
    Info,
    Yellow,
    Red
}

public enum Confidence
{
    High,
    Medium,
    Low
}

// One explanation behind a DNA's status — a warning/problem OR a confirmed-good check.
// Every DNA must surface at least one Info-severity reason when checks pass; see CLAUDE.md Rules Engine notes.
public class StatusReason
{
    public required string Message { get; init; }
    public required ReasonSeverity Severity { get; init; }
    public string? Suggestion { get; init; }
    public required Confidence Confidence { get; init; }

    // A short, tile-summary-friendly version of Message (only set for Yellow/Red reasons) — e.g.
    // "CPU is running hot (92.3 °C)." vs. Message's longer "CPU temperature is 92.3 °C, near the
    // throttle point under sustained load." Lets the one-line DNA Summary say specifically what's
    // wrong (REQUIREMENTS.md section 3's explainability principle) without wrapping past one line.
    public string? ShortReason { get; init; }
}

//*Built with assistance from __Claude Code__ by Anthropic.*
