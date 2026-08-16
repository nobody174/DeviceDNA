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

// Root ViewModel for the command-deck dashboard. Calls the Application layer's scan service
// on construction and exposes the resulting DNA tiles plus overall device health to the UI.
public class MainViewModel : ViewModelBase
{
    private readonly DeviceScanService _scanService;
    private readonly WindowsUpdateDriverCheckService _windowsUpdateCheckService;
    private string _hostname = string.Empty;
    private string _osSummary = string.Empty;
    private HealthStatus _overallStatus = HealthStatus.Green;
    private string _statusMessage = "Scanning...";
    private Device? _device;
    private bool _isScanning;
    private bool _isDiagnoseActive;
    private bool _isHistoryActive;
    private DiagnoseViewModel? _diagnoseViewModel;
    private HistoryWindowViewModel? _historyViewModel;

    public MainViewModel() : this(new DeviceScanService(), new WindowsUpdateDriverCheckService())
    {
    }

    public MainViewModel(DeviceScanService scanService, WindowsUpdateDriverCheckService windowsUpdateCheckService)
    {
        _scanService = scanService;
        _windowsUpdateCheckService = windowsUpdateCheckService;
        Tiles = new List<DnaTileViewModel>();
        // Diagnose and History both navigate in-place within MainWindow (swapping DashboardView/
        // DiagnoseView/HistoryView content) rather than opening a separate popup Window — user
        // feedback: a second window for a page that's "really just the same data, grouped
        // differently" broke the sense of a single app. Only one of Diagnose/History is active at a
        // time (opening one closes the other) since they share the same content area.
        OpenDiagnoseCommand = new RelayCommand(_ => OpenDiagnose(), _ => _device != null);
        GoBackToDashboardCommand = new RelayCommand(_ => { IsDiagnoseActive = false; IsHistoryActive = false; });
        OpenHistoryCommand = new RelayCommand(_ => OpenHistory());
        RescanCommand = new RelayCommand(_ => _ = RunScanAsync(), _ => !IsScanning);
        ExportCommand = new RelayCommand(_ => ExportRequested?.Invoke(this, EventArgs.Empty), _ => _device != null);
        _ = RunScanAsync();
    }

    // True while the Diagnose page is showing in place of the dashboard tile grid.
    public bool IsDiagnoseActive
    {
        get => _isDiagnoseActive;
        private set => SetField(ref _isDiagnoseActive, value);
    }

    // True while the History & Changes page is showing in place of the dashboard tile grid.
    public bool IsHistoryActive
    {
        get => _isHistoryActive;
        private set => SetField(ref _isHistoryActive, value);
    }

    public DiagnoseViewModel? DiagnoseViewModel
    {
        get => _diagnoseViewModel;
        private set => SetField(ref _diagnoseViewModel, value);
    }

    public HistoryWindowViewModel? HistoryViewModel
    {
        get => _historyViewModel;
        private set => SetField(ref _historyViewModel, value);
    }

    public RelayCommand GoBackToDashboardCommand { get; }

    private void OpenDiagnose()
    {
        if (_device == null)
        {
            return;
        }

        DiagnoseViewModel = new DiagnoseViewModel(_device);
        IsHistoryActive = false;
        IsDiagnoseActive = true;
    }

    // History was previously a separate popup Window; each open constructed a fresh
    // HistoryWindowViewModel against the same underlying repository. Kept that same
    // "fresh view model per open" behavior here — it's what re-loads the scan list to reflect any
    // scans that happened since last opened, matching the old window's actual behavior when reopened.
    private void OpenHistory()
    {
        HistoryViewModel = new HistoryWindowViewModel(HistoryRepository);
        IsDiagnoseActive = false;
        IsHistoryActive = true;
    }

    // WMI + LibreHardwareMonitor sensor reads are real, blocking I/O (confirmed: running ScanDevice()
    // synchronously on the UI thread made Rescan look like the app had frozen — user feedback). Runs
    // off the UI thread via Task.Run, same pattern as RunWindowsUpdateCheckAsync below, with
    // IsScanning driving a visible "Scanning..." state so the button gives real feedback instead of
    // silently blocking. RelayCommand's CanExecute re-queries via WPF's CommandManager.RequerySuggested
    // (same mechanism already used for CheckWindowsUpdateCommand), so no manual raise is needed here.
    public bool IsScanning
    {
        get => _isScanning;
        private set => SetField(ref _isScanning, value);
    }

    // Bubbled up from a DnaTileViewModel's own OpenVendorUrlRequested (each tile is constructed
    // fresh per scan in RunScan, so this re-raises rather than requiring MainWindow to re-subscribe
    // to every tile individually). MainWindow's code-behind handles this to launch the URL in the
    // user's default browser — this app never fetches the URL itself.
    public event EventHandler<string>? OpenVendorUrlRequested;

    // Same bubbling pattern as OpenVendorUrlRequested, for DnaTileViewModel.OpenBenchmarkUrlRequested.
    public event EventHandler<string>? OpenBenchmarkUrlRequested;

    // Raised when the user clicks the top-bar "Export" button; MainWindow's code-behind handles this
    // to show a SaveFileDialog and write the JSON — export is always an explicit user action
    // (REQUIREMENTS.md section 10), never automatic, so no file I/O happens in this ViewModel.
    public event EventHandler? ExportRequested;

    public RelayCommand OpenDiagnoseCommand { get; }

    public RelayCommand OpenHistoryCommand { get; }

    public RelayCommand ExportCommand { get; }

    // Phase 4: manual rescan, needed so a second scan (and thus scan-to-scan change detection) can be
    // generated without restarting the app. Every scan — startup or manual — is persisted to SQLite
    // history via DeviceScanService.
    public RelayCommand RescanCommand { get; }

    // Exposes the underlying scan service's history repository so MainWindow can hand it to
    // HistoryWindow without constructing a second repository against the same SQLite file.
    public Application.ScanHistoryRepository HistoryRepository => _scanService.HistoryRepository;

    // Exposes the last scanned Device so MainWindow can pass it to the Diagnose page.
    public Device? Device => _device;

    public string Hostname
    {
        get => _hostname;
        private set => SetField(ref _hostname, value);
    }

    public string OsSummary
    {
        get => _osSummary;
        private set => SetField(ref _osSummary, value);
    }

    public HealthStatus OverallStatus
    {
        get => _overallStatus;
        private set => SetField(ref _overallStatus, value);
    }

    public string OverallStatusBrushKey => OverallStatus switch
    {
        HealthStatus.Green => "StatusGreenBrush",
        HealthStatus.Yellow => "StatusYellowBrush",
        HealthStatus.Red => "StatusRedBrush",
        _ => "StatusGreenBrush",
    };

    public string StatusMessage
    {
        get => _statusMessage;
        private set => SetField(ref _statusMessage, value);
    }

    public IReadOnlyList<DnaTileViewModel> Tiles { get; private set; }

    // The live WUA COM call takes several seconds (confirmed ~7s on real hardware) — always run off
    // the UI thread via Task.Run so the window stays responsive, and only ever in direct response to
    // the user clicking "Check Windows Update" on one specific tile (REQUIREMENTS.md section 10,
    // clarified 2026-08-15: never automatic).
    private async Task RunWindowsUpdateCheckAsync(DnaTileViewModel tile)
    {
        tile.IsCheckingWindowsUpdate = true;
        // Same CommandManager.RequerySuggested gap as RunScanAsync above: CheckWindowsUpdateCommand's
        // CanExecute depends on IsCheckingWindowsUpdate, a code-driven state change WPF's automatic
        // requery doesn't pick up on its own — without this, the button wouldn't visibly disable the
        // instant it's clicked, nor re-enable the instant the check completes.
        System.Windows.Input.CommandManager.InvalidateRequerySuggested();

        var result = await Task.Run(() => _windowsUpdateCheckService.CheckForDriverUpdates());

        if (result.Succeeded)
        {
            tile.ApplyWindowsUpdateCheckResult(result.Updates.Count);
        }
        else
        {
            tile.ApplyWindowsUpdateCheckFailure();
        }

        System.Windows.Input.CommandManager.InvalidateRequerySuggested();
    }

    private async Task RunScanAsync()
    {
        IsScanning = true;
        StatusMessage = "Scanning...";

        try
        {
            var device = await Task.Run(() => _scanService.ScanDevice());
            _device = device;

            // WPF's CommandManager.RequerySuggested (what RelayCommand.CanExecuteChanged is wired
            // to) only re-evaluates CanExecute on user-input/focus events, not automatically when a
            // command's underlying state changes in code — confirmed as the real cause of Diagnose/
            // Export/Rescan's gold border being missing right after a fresh launch (their
            // CanExecute predicates depend on _device/IsScanning, both false/null at construction
            // time) until the user did something that happened to trigger a requery, like Alt-
            // Tabbing away and back (user feedback, 2026-08-16). Forcing it explicitly here means
            // the buttons reflect their real enabled state the instant the scan completes, not on
            // the next incidental focus change.
            System.Windows.Input.CommandManager.InvalidateRequerySuggested();

            Hostname = device.Hostname;
            OsSummary = device.OsSummary;
            Tiles = device.Dnas.Select(d =>
            {
                var tile = new DnaTileViewModel(d);
                tile.OpenVendorUrlRequested += (_, url) => OpenVendorUrlRequested?.Invoke(this, url);
                tile.OpenBenchmarkUrlRequested += (_, url) => OpenBenchmarkUrlRequested?.Invoke(this, url);
                tile.CheckWindowsUpdateRequested += async (_, _) => await RunWindowsUpdateCheckAsync(tile);
                return tile;
            }).ToList();
            OnPropertyChanged(nameof(Tiles));

            // A Rescan while the Diagnose page is already open (the top bar stays visible/clickable
            // across pages — see MainWindow.xaml) previously left DiagnoseViewModel showing the
            // previous scan's findings, since it's only otherwise rebuilt from OpenDiagnose(). Refresh
            // it in place so a Rescan's new findings actually reach the page the user is looking at.
            if (IsDiagnoseActive)
            {
                DiagnoseViewModel = new DiagnoseViewModel(device);
            }

            OverallStatus = Tiles.Count == 0
                ? HealthStatus.Yellow
                : Tiles.Max(t => t.Status);

            StatusMessage = Tiles.Count == 0
                ? "No hardware detected."
                : $"{Tiles.Count} DNA components detected.";
        }
        catch (Exception ex)
        {
            // A total scan failure (e.g. WMI service unavailable) should not crash the app —
            // show an empty dashboard with an explanatory message instead. Also clears _device and
            // any open Diagnose/History page back to the (now-empty) dashboard: previously a failed
            // Rescan left _device pointing at the last successful scan (keeping Export/Diagnose
            // enabled for data that's now inconsistent with the cleared Tiles) and, if Diagnose was
            // already open, left DiagnoseViewModel silently showing the previous scan's findings
            // with no indication the new scan had actually failed.
            _device = null;
            Tiles = new List<DnaTileViewModel>();
            OnPropertyChanged(nameof(Tiles));
            OverallStatus = HealthStatus.Red;
            StatusMessage = $"Scan failed: {ex.Message}";
            IsDiagnoseActive = false;
            IsHistoryActive = false;
            DiagnoseViewModel = null;
        }
        finally
        {
            IsScanning = false;
            System.Windows.Input.CommandManager.InvalidateRequerySuggested();
        }
    }
}

//*Built with assistance from __Claude Code__ by Anthropic.*
