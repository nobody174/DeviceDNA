//
// DeviceDNA
// Author:  nobody174 (nobodylearn174@gmail.com)
// Repo:    https://github.com/nobody174/DeviceDNA
// Patreon: https://www.patreon.com/c/Nobody174
// License: All rights reserved (c) 2026 nobody174
// "It's never too late to give up!"
//

using DeviceDNA.DetectionEngine;
using DeviceDNA.Model;

namespace DeviceDNA.Application;

// Thin orchestration service the UI calls to obtain a Device snapshot.
// Wires up the Detection Engine (which in turn wires the concrete Platform Adapters).
// Phase 4: every scan is now automatically persisted to the local SQLite history via
// ScanHistoryRepository, giving the History/Changes pages something to read from
// (REQUIREMENTS.md section 7 items 4-5).
public class DeviceScanService
{
    private readonly DeviceDetectionService _detectionService;
    private readonly ScanHistoryRepository _historyRepository;

    public DeviceScanService()
        : this(DeviceDetectionService.CreateDefault(), new ScanHistoryRepository())
    {
    }

    public DeviceScanService(DeviceDetectionService detectionService, ScanHistoryRepository historyRepository)
    {
        _detectionService = detectionService;
        _historyRepository = historyRepository;
    }

    public Device ScanDevice()
    {
        var device = _detectionService.DetectDevice();
        _historyRepository.SaveScan(device);
        return device;
    }

    // Exposes the repository so callers (e.g. MainViewModel) can open History/Changes without
    // constructing their own repository instance against the same SQLite file.
    public ScanHistoryRepository HistoryRepository => _historyRepository;
}

//*Built with assistance from __Claude Code__ by Anthropic.*
