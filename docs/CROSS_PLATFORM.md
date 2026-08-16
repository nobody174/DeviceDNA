# Cross-Platform Scoping (Phase 6)

Design-level scoping only — no Linux/macOS code in v1. This documents what a future Platform
Adapter would need, and confirms the existing architecture (CLAUDE.md's strict layering) actually
supports it without a rewrite.

## Why this is believed to work

`DeviceDNA.Model`, `DeviceDNA.DetectionEngine`'s `RulesEngine`, `DeviceDNA.Application`, and all of
`DeviceDNA.UI` contain zero OS-specific code today — confirmed by inspection: the only two classes
touching Windows APIs are `WmiAdapter` (`System.Management`) and `LibreHardwareMonitorAdapter`
(wraps `LibreHardwareMonitorLib`, which is itself Windows/Linux-capable but used here against
Windows sensors), both in `DeviceDNA.PlatformAdapters`, both behind the `IWmiAdapter` /
`ILibreHardwareMonitorAdapter` interfaces already consumed by `DeviceDetectionService` via
dependency injection (`DeviceDetectionService.CreateDefault()` is the only place concrete adapter
types are named, and it's explicitly marked as the seam — see its doc comment). A Linux adapter
pair implementing the same two interfaces is therefore additive, not a refactor.

## What a Linux Platform Adapter would need

Replacing `WmiAdapter` (static inventory) and `LibreHardwareMonitorAdapter` (live sensors):

- **Static inventory** (CPU model/cores, GPU name, memory modules, disk model/capacity, motherboard
  manufacturer/model/BIOS, network adapter names, OS name/version): `/proc/cpuinfo`, `/sys/class/`
  trees (`dmi/id/*` for board/BIOS, `net/*` for adapters), `lscpu`, `lsblk -J`, `dmidecode` (needs
  root), `/etc/os-release`. No single Linux equivalent of WMI exists — expect to shell out to a
  handful of standard CLI tools and parse structured (`-J`/JSON where available) output, similar
  in spirit to how `WmiAdapter` queries multiple `Win32_*` classes today.
- **Live sensors** (temps, clocks, utilization, fan speeds): `lm-sensors` (`sensors -j` gives JSON),
  `/sys/class/hwmon/hwmon*/` directly, `/proc/stat` for CPU utilization. LibreHardwareMonitorLib
  itself has partial Linux support as of recent versions — worth re-checking at implementation time
  whether it can be reused directly instead of hand-rolling hwmon parsing, which would shrink this
  adapter significantly.
- **SMART health**: `smartctl -j` (from `smartmontools`) in place of the WMI
  `MSStorageDriver_FailurePredictStatus` query Phase 2 used.
- **Elevation**: several of the above need root (`dmidecode`, some `hwmon` paths). Same shape of
  problem as Windows admin rights (Phase 1 already degrades gracefully when LibreHardwareMonitor
  can't get sensor access unelevated) — a Linux adapter should follow the same pattern: return
  partial data, never crash, never fabricate what it can't read.

## What a macOS Platform Adapter would need

- **Static inventory**: `system_profiler -json SPHardwareDataType SPDisplaysDataType
  SPStorageDataType SPNetworkDataType`, `sysctl -a` for various hardware.machdep keys.
- **Live sensors**: no public/stable API — historically requires SMC (System Management
  Controller) access via private frameworks or a helper tool (e.g. the approach used by
  `iStats`/`smcFanControl`). This is meaningfully harder than Linux and should be scoped as its own
  research spike before committing to an implementation estimate, not assumed to be a small delta
  from the Linux adapter.

## What does NOT change

Per CLAUDE.md's architecture diagram: `DeviceDNA.Model`, `RulesEngine`, `DeviceScanService`,
`ScanHistoryRepository`, `DeviceExportService`, and the entire WPF UI stay untouched. The only
platform-specific decision left open is UI framework portability — WPF is Windows-only, so a
Linux/macOS build would need either a second UI project (e.g. Avalonia, which is source-compatible
enough with WPF-style XAML/MVVM to reuse most of `DeviceDNA.UI.Presentation`'s ViewModels largely
as-is) or a different distribution shape entirely (e.g. a web UI over a local API — see
ROADMAP.md's deferred "local API" item, which would also decouple the UI from the OS entirely).
This UI question is explicitly out of scope for this phase and deferred to whenever Linux/macOS
support is actually prioritized.

## Non-goals of this document

This is not an implementation plan, effort estimate, or commitment to build Linux/macOS support.
It exists to confirm the Phase 1 architectural bet (Platform Adapters as the only OS-specific
layer) holds up under a first real look at what the alternative platforms would require, and to
leave a concrete starting point if/when this is prioritized.
