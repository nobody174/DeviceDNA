//
// DeviceDNA
// Author:  nobody174 (nobodylearn174@gmail.com)
// Repo:    https://github.com/nobody174/DeviceDNA
// Patreon: https://www.patreon.com/c/Nobody174
// License: All rights reserved (c) 2026 nobody174
// "It's never too late to give up!"
//

using DeviceDNA.Application;

namespace DeviceDNA.UI.Presentation;

// One change entry for display on the Changes page.
public class ChangeRowViewModel
{
    public required string DnaTypeLabel { get; init; }
    public required string DnaName { get; init; }
    public required string Description { get; init; }
}

// A selectable scan option for the Changes page's "compare" dropdowns. Wraps ScanSnapshotSummary
// with a display label so the same entry can be shown in both the "From" and "To" pickers.
public class ScanPickerOption
{
    public required long ScanId { get; init; }
    public required string Label { get; init; }
}

// ViewModel for the Changes view (REQUIREMENTS.md section 7 item 4): diffs two scans and lists a
// human-readable "what changed" timeline. Defaults to the two most recent scans (the common case),
// but exposes "From"/"To" dropdowns so the user can pick any two past scans to compare — the
// underlying SQLite history already stores every scan and supports loading any of them by id
// (ScanHistoryRepository.LoadScan), so this is purely a UI-selection feature, no storage change
// needed. Distinct from the History page (which lists scans) but reachable from it, per the task
// brief's "related but distinct features" framing.
public class ChangesViewModel : ViewModelBase
{
    private readonly ScanHistoryRepository _repository;
    private ScanPickerOption? _fromScan;
    private ScanPickerOption? _toScan;
    private IReadOnlyList<ChangeRowViewModel> _changes = new List<ChangeRowViewModel>();
    private string _summaryText = string.Empty;

    public ChangesViewModel(ScanHistoryRepository repository)
    {
        _repository = repository;

        var scans = repository.ListScans();
        ScanOptions = scans.Select(s => new ScanPickerOption
        {
            ScanId = s.ScanId,
            Label = $"{s.Timestamp.ToLocalTime():g} — {s.Hostname}",
        }).ToList();

        if (ScanOptions.Count < 2)
        {
            _summaryText = "Not enough scan history yet — run at least two scans to see what changed.";
            return;
        }

        // ListScans returns newest-first; default to comparing the two most recent, matching the
        // previous fixed behavior before arbitrary-pair selection was added.
        _toScan = ScanOptions[0];
        _fromScan = ScanOptions[1];
        RunComparison();
    }

    public IReadOnlyList<ScanPickerOption> ScanOptions { get; }

    public ScanPickerOption? FromScan
    {
        get => _fromScan;
        set
        {
            if (SetField(ref _fromScan, value))
            {
                RunComparison();
            }
        }
    }

    public ScanPickerOption? ToScan
    {
        get => _toScan;
        set
        {
            if (SetField(ref _toScan, value))
            {
                RunComparison();
            }
        }
    }

    public IReadOnlyList<ChangeRowViewModel> Changes
    {
        get => _changes;
        private set => SetField(ref _changes, value);
    }

    public string SummaryText
    {
        get => _summaryText;
        private set => SetField(ref _summaryText, value);
    }

    public string ComparisonLabel => FromScan is null || ToScan is null
        ? string.Empty
        : $"Comparing {FromScan.Label} → {ToScan.Label}";

    private void RunComparison()
    {
        OnPropertyChanged(nameof(ComparisonLabel));

        if (FromScan is null || ToScan is null)
        {
            Changes = new List<ChangeRowViewModel>();
            SummaryText = "Select two scans to compare.";
            return;
        }

        if (FromScan.ScanId == ToScan.ScanId)
        {
            Changes = new List<ChangeRowViewModel>();
            SummaryText = "Select two different scans to compare.";
            return;
        }

        var previous = _repository.LoadScan(FromScan.ScanId);
        var current = _repository.LoadScan(ToScan.ScanId);

        if (current is null || previous is null)
        {
            Changes = new List<ChangeRowViewModel>();
            SummaryText = "Could not load the selected scans for comparison.";
            return;
        }

        var detected = ScanChangeDetector.Compare(previous, current);
        Changes = detected.Select(c => new ChangeRowViewModel
        {
            DnaTypeLabel = c.DnaTypeLabel,
            DnaName = c.DnaName,
            Description = c.Description,
        }).ToList();

        SummaryText = Changes.Count == 0
            ? "No significant changes detected between these two scans."
            : $"{Changes.Count} change{(Changes.Count == 1 ? "" : "s")} detected.";
    }
}

//*Built with assistance from __Claude Code__ by Anthropic.*
