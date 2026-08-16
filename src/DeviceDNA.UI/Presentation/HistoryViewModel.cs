//
// DeviceDNA
// Author:  nobody174 (nobodylearn174@gmail.com)
// Repo:    https://github.com/nobody174/DeviceDNA
// Patreon: https://www.patreon.com/c/Nobody174
// License: All rights reserved (c) 2026 nobody174
// "It's never too late to give up!"
//

using DeviceDNA.Application;
using DeviceDNA.Model;

namespace DeviceDNA.UI.Presentation;

// One row in the History list.
public class ScanHistoryRowViewModel
{
    public required long ScanId { get; init; }
    public required DateTime Timestamp { get; init; }
    public required string Hostname { get; init; }
    public required HealthStatus OverallStatus { get; init; }

    public string TimestampLabel => Timestamp.ToLocalTime().ToString("g");
    public string StatusBrushKey => OverallStatus switch
    {
        HealthStatus.Green => "StatusGreenBrush",
        HealthStatus.Yellow => "StatusYellowBrush",
        HealthStatus.Red => "StatusRedBrush",
        _ => "StatusGreenBrush",
    };
}

// A selectable "filter by DNA" option for the History page's dropdown.
public class DnaTypeFilterOption
{
    public required string Label { get; init; }
    public DnaType? Type { get; init; }
}

// ViewModel for the History page (REQUIREMENTS.md section 7 item 5): lists past scans, newest first,
// filterable by DNA type. Selecting the two most recent scans by default feeds the Changes view.
public class HistoryViewModel : ViewModelBase
{
    private readonly ScanHistoryRepository _repository;
    private DnaTypeFilterOption _selectedFilter;

    public HistoryViewModel(ScanHistoryRepository repository)
    {
        _repository = repository;

        FilterOptions = new List<DnaTypeFilterOption>
        {
            new() { Label = "All DNA types", Type = null },
        }.Concat(Enum.GetValues<DnaType>().Select(t => new DnaTypeFilterOption { Label = t.ToString(), Type = t }))
         .ToList();

        _selectedFilter = FilterOptions[0];

        Scans = new List<ScanHistoryRowViewModel>();
        RefreshCommand = new RelayCommand(_ => LoadScans());
        LoadScans();
    }

    public IReadOnlyList<DnaTypeFilterOption> FilterOptions { get; }

    public DnaTypeFilterOption SelectedFilter
    {
        get => _selectedFilter;
        set
        {
            if (SetField(ref _selectedFilter, value))
            {
                LoadScans();
            }
        }
    }

    public IReadOnlyList<ScanHistoryRowViewModel> Scans { get; private set; }

    public RelayCommand RefreshCommand { get; }

    public string SummaryText => Scans.Count == 0
        ? "No scans recorded yet."
        : $"{Scans.Count} scan{(Scans.Count == 1 ? "" : "s")} recorded.";

    private void LoadScans()
    {
        var summaries = _repository.ListScans(_selectedFilter.Type);
        Scans = summaries.Select(s => new ScanHistoryRowViewModel
        {
            ScanId = s.ScanId,
            Timestamp = s.Timestamp,
            Hostname = s.Hostname,
            OverallStatus = s.OverallStatus,
        }).ToList();

        OnPropertyChanged(nameof(Scans));
        OnPropertyChanged(nameof(SummaryText));
    }
}

// Root ViewModel composing the History list and Changes diff sections — formerly HistoryWindow's
// own DataContext when History was a separate popup Window; now constructed by MainViewModel and
// exposed as MainViewModel.HistoryViewModel for HistoryView's in-place bindings.
public class HistoryWindowViewModel
{
    public HistoryWindowViewModel(ScanHistoryRepository repository)
    {
        History = new HistoryViewModel(repository);
        Changes = new ChangesViewModel(repository);
    }

    public HistoryViewModel History { get; }
    public ChangesViewModel Changes { get; }
}

//*Built with assistance from __Claude Code__ by Anthropic.*
