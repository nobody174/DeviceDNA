//
// DeviceDNA
// Author:  nobody174 (nobodylearn174@gmail.com)
// Repo:    https://github.com/nobody174/DeviceDNA
// Patreon: https://www.patreon.com/c/Nobody174
// License: All rights reserved (c) 2026 nobody174
// "It's never too late to give up!"
//

using DeviceDNA.Model;

namespace DeviceDNA.UI.Presentation;

// One StatusReason flattened for display on the Diagnose page, carrying along which DNA it came from.
public class DiagnoseFindingViewModel
{
    public required string DnaTypeLabel { get; init; }
    public required string DnaName { get; init; }
    public required string Message { get; init; }
    public string? Suggestion { get; init; }
    public required string ConfidenceLabel { get; init; }
    public required ReasonSeverity Severity { get; init; }

    public bool IsConfirmedGood => Severity == ReasonSeverity.Info;

    public string SeverityLabel => Severity switch
    {
        ReasonSeverity.Info => "Confirmed good",
        ReasonSeverity.Yellow => "Worth checking",
        ReasonSeverity.Red => "Needs attention",
        _ => Severity.ToString(),
    };

    // Small glyph shown next to each finding: check for confirmed-good, warning/alert otherwise.
    public string Glyph => Severity switch
    {
        ReasonSeverity.Info => "✔", // check mark
        ReasonSeverity.Yellow => "⚠", // warning triangle
        ReasonSeverity.Red => "✖", // heavy X
        _ => "•",
    };

    public string StatusBrushKey => Severity switch
    {
        ReasonSeverity.Info => "StatusGreenBrush",
        ReasonSeverity.Yellow => "StatusYellowBrush",
        ReasonSeverity.Red => "StatusRedBrush",
        _ => "StatusGreenBrush",
    };
}

// A severity-grouped bucket of findings for the Diagnose page's grouped list.
public class DiagnoseGroupViewModel
{
    public required string GroupLabel { get; init; }
    public required IReadOnlyList<DiagnoseFindingViewModel> Findings { get; init; }
}

// ViewModel for the dedicated Diagnose page (REQUIREMENTS.md section 7, item 3): aggregates every
// StatusReason across every DNA from the current scan, grouped by severity (red first, then yellow,
// then confirmed-good) so the most actionable findings surface at the top.
public class DiagnoseViewModel : ViewModelBase
{
    public DiagnoseViewModel(Device device)
    {
        var allFindings = device.Dnas
            .SelectMany(dna => dna.StatusReasons.Select(reason => new DiagnoseFindingViewModel
            {
                DnaTypeLabel = dna.Type.ToString(),
                DnaName = dna.Name,
                Message = reason.Message,
                Suggestion = reason.Suggestion,
                ConfidenceLabel = reason.Confidence.ToString(),
                Severity = reason.Severity,
            }))
            .ToList();

        RedCount = allFindings.Count(f => f.Severity == ReasonSeverity.Red);
        YellowCount = allFindings.Count(f => f.Severity == ReasonSeverity.Yellow);
        GoodCount = allFindings.Count(f => f.Severity == ReasonSeverity.Info);

        Groups = new List<DiagnoseGroupViewModel>
        {
            new()
            {
                GroupLabel = "Needs Attention",
                Findings = allFindings.Where(f => f.Severity == ReasonSeverity.Red).ToList(),
            },
            new()
            {
                GroupLabel = "Worth Checking",
                Findings = allFindings.Where(f => f.Severity == ReasonSeverity.Yellow).ToList(),
            },
            new()
            {
                GroupLabel = "Confirmed Good",
                Findings = allFindings.Where(f => f.Severity == ReasonSeverity.Info).ToList(),
            },
        }.Where(g => g.Findings.Count > 0).ToList();
    }

    public IReadOnlyList<DiagnoseGroupViewModel> Groups { get; }

    public int RedCount { get; }
    public int YellowCount { get; }
    public int GoodCount { get; }

    public string SummaryText => $"{RedCount} needs attention, {YellowCount} worth checking, {GoodCount} confirmed good.";
}

//*Built with assistance from __Claude Code__ by Anthropic.*
