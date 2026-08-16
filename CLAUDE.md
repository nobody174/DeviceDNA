# CLAUDE.md — DeviceDNA

Read `REQUIREMENTS.md` first for product context and scope. This file is the technical working reference for implementation.

## Session start convention

"Read CLAUDE.md and REQUIREMENTS.md, check Current State, continue."

## Current State

**v1 feature-complete and hardening gauntlet closed.** Phases 1-7 complete for the parts that are actually buildable autonomously (solution builds clean, 0 errors/0 warnings; `dotnet publish` verified producing a working self-contained exe). The full pre-release hardening gauntlet has run end to end and closed clean: Project Reviewer → Security Auditor → QA/Playtester → Devil's Advocate → Project Reviewer round 2 (see CHANGELOG.md's five "Hardening Pass" entries). Security Auditor came back genuinely clean. The other passes found and fixed real bugs — crash risks, several data-fabrication issues corrected per REQUIREMENTS.md section 10 (including one, a fabricated Memory rated-speed value, that was silently suppressing real diagnosis findings, caught by Devil's Advocate attacking an earlier pass's own fix). Round 2 independently re-verified every fix landed correctly with no regressions and found no release-blocker. Foundation, Advanced tier, deterministic Rules Engine + Diagnose page, SQLite-backed scan History + Change Detection, JSON Export, cross-platform scoping doc, and publish verification are all in place. See `CHANGELOG.md` for details. After the gauntlet closed, worked the entirety of BACKLOG.md's severity-ranked list across two rounds using a Research Analyst → Implementer/Fixer pattern (see CHANGELOG.md's "[BACKLOG pass]" entries). Round 1: Storage `Type` now uses a real hardware-reported WMI signal instead of "Unknown" for the common case, a new genuinely-local "reboot pending" OS health rule shipped after research ruled out full "updates available" detection (network call required), and CPU boost-clock / Motherboard PCIe-topology rules were confirmed genuinely infeasible and closed as won't-implement. Round 2: closed 8 more items — including a real bug research caught mid-investigation (a fabricated Memory module-size value was silently feeding a live mismatched-modules rules-engine check, the same severity class as the earlier RatedSpeedMts fix), a full nullable-fields cleanup across CPU/GPU/Storage display fields, honest Motherboard-chipset and Storage-manufacturer extraction (verified live on real hardware, deliberately scoped narrower than first proposed per research on false-positive risk), and DNA summaries that now explain specifically what's wrong instead of a generic phrase. Four remaining items were product/domain decisions (not technical unknowns) and were each decided and documented rather than left ambiguous. BACKLOG.md is now down to genuinely open items only: Phase 7's installer/website/pricing/marketing (need real human decisions, not fabricated), two live-check rules deliberately deferred after real research (not "forbidden by principle" — see below), and a handful of accepted-as-is edge cases with documented reasoning for why they're not worth fixing.

**Privacy scope clarified (2026-08-15)**: REQUIREMENTS.md section 10 was rewritten after a real product conversation surfaced that "no cloud requirement" had been read too strictly — it's about user data never leaving the device, not a ban on the app ever making an outbound request. Online lookups are wanted, always user-triggered per action, never automatic on a routine scan. First feature under the new scope: GPU and Motherboard DNA names are now clickable, opening the vendor's real product/support page in the user's browser (verified live on this dev machine — NVIDIA RTX 3060 → nvidia.com/drivers, ASUS TUF GAMING B550-PRO → its real per-model BIOS support URL). A *live* "check for updates" feature (fetching a vendor's site to determine current-vs-installed freshness) was researched in depth and deliberately not built as a vendor-site scraper — real technical paths exist for NVIDIA/AMD/ASUS, but NVIDIA's carries explicit ToS friction and no free driver-database API exists as an alternative; this app's own separate project (Verdict) independently reached the identical conclusion for the same problem. See CHANGELOG.md's "[Privacy scope clarified + vendor links shipped]" entry and BACKLOG.md's two GPU/Motherboard entries for full reasoning.

**Windows Update driver check shipped (2026-08-15)**: supplementing the vendor links above, CPU/GPU/Network/Motherboard tiles now have an opt-in "Check Windows Update" button using the Windows Update Agent (WUA) COM API (`Type='Driver'` search), verified live against real Windows Update (7.6s, 0 results found on this dev machine, no exceptions). This was built only after re-verifying a third-party claim about WUA's capabilities directly rather than accepting it at face value — confirmed real, but with weak practical coverage for GPU/BIOS updates specifically, so it ships as a supplement to the vendor link-out (with an explicit in-UI caveat), not a replacement. Strictly user-triggered, same opt-in-per-action rule as the vendor links. See CHANGELOG.md's "[Windows Update driver check shipped]" entry.

**Numbered BACKLOG walkthrough (2026-08-15) — complete, all 10 items closed**: worked BACKLOG.md's remaining open items one at a time with the user, in order. Items 1-2 (GPU/Motherboard live-check rules) resolved via the vendor-link + WUA-check work above. Item 3 (curated change-detection field list) and items 6-7 (History DNA-filter scope, multi-enclosure edge case) confirmed good-as-is by the user, no code change. Item 4: Changes view's fixed "compare two most recent scans" replaced with "From"/"To" scan-picker dropdowns — confirmed this was a UI-wiring gap only (the SQLite history already supported loading any past scan by id), not a storage change, ruling out a `.log`-file-based alternative the user proposed. Item 5: storage-to-sensor temperature correlation fixed to match on the Windows physical drive index (shared by WMI's `Win32_DiskDrive.Index` and LibreHardwareMonitor's `Identifier`) instead of a fragile model-name substring — verified live, the two APIs' index values matched exactly for the same physical disks on this dev machine. Item 8: a real `.msi` installer shipped via WiX Toolset (user's explicit choice over Inno Setup, reasoning the authoring cost falls on Claude not them) — see CHANGELOG.md's "[.msi installer shipped]" entry for the two real snags hit and fixed (WiX v7's new paid EULA requirement, an initial 32-bit/64-bit platform mismatch) and the previously-unwired `assets/icon.ico` now used everywhere. Item 9: distribution/license model decided — free via GitHub/Patreon, All Rights Reserved (not MIT, which the user had initially misnamed while actually describing ARR's terms — flagged and corrected rather than applied literally); `LICENSE` added and the user's standard per-file header/footer applied across all 45 real source files. Item 10: LibreHardwareMonitorLib's MPL-2.0 skim finally done, closed as non-blocking for this non-commercial distribution model, with `THIRD-PARTY-NOTICES.md` added to satisfy the one obligation that did apply. A dedicated landing/marketing page turned out to not actually be wanted (user: repo stays quietly public on GitHub, screenshots go to Patreon directly, no separate site) — closed as not-applicable rather than left open waiting on unwanted work. BACKLOG.md's original 10-item list is now fully closed. See CHANGELOG.md's per-item entries and BACKLOG.md (each closed item's Notes section) for full reasoning.

**First-look UI pass + CPU sensor root-cause fix (2026-08-15)**: after the BACKLOG walkthrough closed, ran the app live with the user for the first time and worked through direct first-look feedback — real bugs (logo sizing, Rescan's missing loading feedback, tile-expand layout stretching, a stuck-looking Advanced toggle, a missing CPU vendor link, Diagnose's separate-popup-window navigation) fixed in one pass; a bigger DNA-helix/orbital redesign idea deliberately deferred to its own future conversation per the user's explicit sequencing choice (see BACKLOG.md). Separately, the user caught and refused to accept an initial "no clean fix exists" conclusion for CPU temperature/live-clock always reading 0 — proved via a live Speccy/CPU-Z side-by-side screenshot that the data genuinely is readable on this hardware. A multi-round research investigation (ZenStates-Core ruled out as both GPLv3 *and* not even fixing the bug; WinRing0 ruled out as Defender-blocked since Sept 2025, not a licensing/effort problem) eventually found the real root cause: LibreHardwareMonitorLib depends on a separate driver, **PawnIO**, which was simply never installed on this machine — LHM's own auto-install logic failed silently. Verified by manually installing PawnIO's official signed installer: fixed immediately, zero DeviceDNA code changes needed. Shipped: DeviceDNA's `.msi` installer now bundles and silently installs PawnIO as part of its own install (GPLv2 with an explicit IOCTL-communication exception — documented in `THIRD-PARTY-NOTICES.md`, treated with the same license scrutiny that correctly ruled out ZenStates-Core). Verified end-to-end twice: uninstall PawnIO → fresh silent `.msi` install → PawnIO comes back automatically → real CPU temp/clock confirmed in the running app. See CHANGELOG.md's "[First-look UI pass]" and "[CPU sensor fix — PawnIO bundled]" entries for full detail.

**Orbital DNA dashboard shipped, command-deck tile grid retired (2026-08-16)**: built the DNA-helix/orbital redesign deferred above — a central "This Computer" hub with one orbit node per DNA component, connected by curved strand lines; clicking a node swaps to a full detail view (all fields at once, no Basic/Advanced toggle) with an animated vertical DNA-helix graphic on the left, styled after `Logo.png`'s real rainbow-gradient palette. Designed first as a clickable HTML mockup (icons, layout, colors all iterated there before any WPF was touched), then built in five verified stages (static layout → icons/status/connectors → click-to-expand → animated transition → final wiring/dead-code cleanup). The old tile grid (`DnaTileViewModel.IsExpanded`/`ToggleExpandCommand`/`ShowAdvanced`/`ToggleAdvancedCommand`) no longer exists in the codebase. Followed by a substantial live-feedback round: History converted from a popup Window to in-place navigation (matching Diagnose); new themed `Button`/`ComboBox` styles across the whole app (red hover frame per explicit user request, fixing both a missing-frame bug and unreadable History dropdowns); new vendor links for Storage/Memory/Network/Windows-OS (each researched — general manufacturer pages where no per-model URL exists, Network reusing the motherboard's own URL for integrated adapters, OS using a real per-version Microsoft release-health page via a new registry read); a user-found, user-verified CPU-Z Validator benchmark link (its URL parameter reverse-engineered as plain ASCII-hex of the CPU name and confirmed against WMI's own `Win32_Processor.Name`); a GPU equivalent researched and honestly declined (no vendor site exposes a name-derivable URL, logged as open in BACKLOG.md); and two real bugs root-caused rather than guessed at (a WPF `CommandManager.RequerySuggested` timing gap explaining why button frames looked wrong only on fresh launch, and a missing `WDS`-prefix entry explaining why one specific Western Digital SSD had no vendor link). See CHANGELOG.md's "[Orbital DNA dashboard shipped]" and "[Orbital dashboard: theming, vendor links, live-review fixes]" entries for full detail.

**Seven-role review pass + Hardening Gauntlet x2, autonomous (2026-08-16)**: ran a full unattended
codebase review — Documentation Architect → Debugger/Root-Cause Investigator → Code Style Enforcer →
Data Model Auditor + UX Reduction Designer → UI Concept Designer → QA/Playtester Persona → Technical
Writer (dependency-ordered, not the originally-given random order) — followed by two full runs of
the Hardening — Pre-Release Gauntlet, fixing every real finding immediately rather than deferring
any of them. Real bugs found and fixed: a fabricated Memory-manufacturer value defeating its own
downstream honesty check (same bug class as the earlier RatedSpeedMts/module-size fixes); the "Check
Windows Update" feature was fully wired end-to-end in code but had no button anywhere in the current
orbital dashboard XAML (a dropped-during-rebuild regression, same class as the vendor-link name that
had to be re-added earlier); and two stale-state bugs where Rescan left the open Detail view or
Diagnose page silently showing the previous scan's data with no indication anything had changed. No
live-elevated-GUI verification was possible during this run (no way to click through a UAC prompt
unattended), so CPU/GPU temperature/live-clock readings were not re-verified live — no code on that
path changed, so this is a documented gap, not a known regression. See CHANGELOG.md's "[Seven-role
review pass + Hardening Gauntlet x2]" entry for the full per-role breakdown.

**Hardening Gauntlet run 1 of 2 (2026-08-16)**: ran the full 10-step gauntlet again as a deeper,
adversarial pass specifically on top of the seven-role review above, deliberately hunting for what
that pass hadn't already covered rather than re-confirming it. Found and fixed one real bug of the
same class as the seven-role pass's own stale-state fixes: a failed Rescan (not just a successful
one) could leave the Diagnose page silently showing the previous scan's findings, and left
`_device` non-null (keeping Export/Diagnose enabled) even though the dashboard had gone empty —
the earlier fix only covered the success path. Security Auditor, Release Manager, and Devil's
Advocate passes (against this session's own new code) all came back genuinely clean, no findings.
No live-elevated-GUI verification possible this run either — same documented gap, no code on that
path touched. See CHANGELOG.md's "[Hardening Gauntlet run 1 of 2]" entry for the full breakdown.

**Hardening Gauntlet run 2 of 2 — complete, multi-role review effort now fully closed
(2026-08-16)**: ran the full 10-step gauntlet a second, genuinely independent time. Findings were
narrower than run 1's (as expected for a codebase that had already been through one full round):
two doc-drift items caught by a fresh full-file read rather than a diff-only re-check (a stale
"alternating gold/teal" strand-color comment in `DashboardView.xaml` describing a since-changed
single-teal-color design, and REQUIREMENTS.md section 2 still describing the retired command-deck
tile grid's Basic-then-Advanced-toggle navigation instead of the orbital dashboard's current
no-toggle flow already correctly documented in section 8). Security Auditor, QA/Playtester (three
new adversarial scenarios: concurrent Rescan during an in-flight WUA check, rapid History
filter-dropdown toggling, rapid orbit-node re-selection), Devil's Advocate (a direct, successful
attempt to break run 1's own failed-Rescan state-clearing fix), and Design Critic passes all came
back genuinely clean on independent re-verification — no rubber-stamping, each traced the actual
call chains/code paths rather than trusting the prior verdict. See CHANGELOG.md's "[Hardening
Gauntlet run 2 of 2]" entry for full detail. This closes the multi-role review + hardening process
the user requested — no further gauntlet runs are planned unless a future milestone warrants one.

## Coding conventions

- All code and comments in English only. No Norwegian text in code or comments.
- C# / .NET 8, native Windows, **WPF** (decided at Phase 1 scaffold). Reasoning: mature unpackaged-exe distribution (no MSIX sandboxing friction against LibreHardwareMonitorLib's need for elevated sensor access), more flexible custom styling (the original command-deck tile grid, and its 2026-08-16 replacement, the orbital DNA dashboard — both needed non-standard layout/drawing WPF's templating model handles well), boring/stable ecosystem for a long-lived local tool. WinUI 3 was considered and rejected — packaging and third-party library compatibility are less mature and add unneeded overhead for a single-machine desktop tool.
- Every new top-level module should be documented with a `//` header comment explaining its purpose.
- No comments in JSON config files.

## Architecture (strict layering — do not violate)

```
UI
  ↓
Application Layer
  ↓
DeviceDNA Model      ← the normalized data model, single source of truth
  ↓
Detection Engine
  ↓
Platform Adapters    ← ONLY layer allowed to contain OS-specific code
  ↓
OS / Hardware
```

Rationale: this is what allows Linux/macOS support later to be an additional Platform Adapter, not a rewrite. Never let OS-specific logic leak above the Platform Adapter layer.

Within Platform Adapters, organize by **data source**, not by DNA type — e.g. one `LibreHardwareMonitorAdapter` that feeds sensor readings into multiple DNAs (CPU, GPU, Storage, Motherboard all pull from one `Computer.Open()` call), a separate `WmiAdapter` for static inventory fields, and a separate `RegistryAdapter` (`IRegistryAdapter`) for local Windows Registry reads (OS reboot-pending signal, OS `DisplayVersion` for the release-health link) — added in the BACKLOG pass and orbital-redesign work respectively, same seam pattern as the other two. Avoid each DNA independently wrapping the same underlying library.

## Full DNA Data Model Schema

```
DNA {
  id: string (unique)
  type: enum ["CPU", "GPU", "Memory", "Storage", "Motherboard", "Network", "OS"]  // v1 scope
  name: string
  manufacturer: string
  summary: string                    // one-line plain-English sentence
  status: enum ["green", "yellow", "red"]
  status_reasons: [
    { message: string, severity: enum["info","yellow","red"], suggestion: string|null, confidence: enum["High","Medium","Low"] }
  ]
  basic: { ... }                     // per-DNA fields, see below
  advanced: { ... }                  // per-DNA fields, see below
  deep: { ... }                      // placeholder object, empty in v1
  driver: { version: string|null, date: string|null, source_url: string|null } | "not_applicable"
  last_updated: timestamp
}

Device {
  id: string
  hostname: string
  os_summary: string
  form_factor: string
  dnas: [DNA]
  scan_history: [ { timestamp, snapshot_id } ]
}
```

## Per-DNA Field Reference (V1 — Basic + Advanced only, Deep deferred)

Every DNA type's Basic tier also carries a `vendor_support_url` (nullable — see REQUIREMENTS.md
section 10 and CHANGELOG.md's "[Orbital dashboard: theming, vendor links, live-review fixes]"
entry): a real, honesty-scoped link to the manufacturer's product/support page, opened in the
user's browser on click, never fetched by the app itself. Shipped for all 7 v1 DNA types as of
2026-08-16. Not listed per-DNA below to avoid repeating it seven times — treat it as always present
alongside each type's other Basic fields.

### CPU
- Basic: name, manufacturer, cores, threads, base_clock, status, summary, vendor_support_url
- Advanced: architecture/generation, socket, boost_clock, cache (L1/L2/L3), tdp, current_temp, current_utilization_pct, current_live_clock, per_core_load, virtualization_support (bool), power_mode
- Also (Basic-tier, CPU-only): `benchmark_url` — a link to the CPU-Z Validator community benchmark database for this exact CPU model, distinct from `vendor_support_url` (a vendor page, not a community database). No equivalent exists for other DNA types (researched for GPU, no site exposes a name-derivable URL — see BACKLOG.md).
- Health rules: red = sustained high temp near throttle point or reported errors; yellow = hotter than typical sustained, high utilization sustained, virtualization disabled, running well below rated boost under load

### GPU
- Basic: name, manufacturer, vram_amount, driver_version, status, summary, vendor_support_url
- Advanced: core_clock, boost_clock, memory_type, memory_clock, pcie_generation, pcie_lane_width, current_temp, current_utilization_pct, current_vram_usage, driver_date, connected_outputs_active
- Health rules: (define during rules-engine implementation — mirror CPU pattern: temp, utilization, driver staleness)

### Memory
- Basic: total_capacity, type (DDR4/DDR5), speed_mts, slots_used, slots_total, status, summary, vendor_support_url
- Advanced: rated_speed, actual_speed, channel_mode, timings_cl, per_module [{size, manufacturer, part_number}]
- Health rules: yellow = actual_speed < rated_speed (XMP/EXPO not enabled), yellow = mismatched modules; red = errors/instability detected

### Storage
- Basic: model, capacity, free_space_pct, type (SSD/HDD/NVMe), status, summary, vendor_support_url
- Advanced: interface, rated_speed, smart_health_pct, current_temp, partitions
- Health rules: yellow = capacity >80-85% full, yellow = SMART warning attribute; red = SMART failure predicted, red = high temp

### Motherboard
- Basic: manufacturer, model, bios_version, chipset, status, summary, vendor_support_url
- Advanced: bios_date, socket, memory_support {type, max, slots}, pcie_slots [{generation, physical_width, negotiated_width, in_use, populated_by}], m2_slots
- Health rules: yellow = BIOS update available, yellow = PCIe slot negotiating below physical width unexpectedly; red = not typical for this component (mostly informational)

### Network (one entry per adapter)
- Basic: adapter_name, connection_type (Wired/WiFi), current_speed, status, summary, vendor_support_url
- Advanced: ip_address, mac_address, driver_version, signal_strength (WiFi only), max_supported_speed
- Health rules: yellow = connected but below max supported speed, yellow = weak WiFi signal; red = disconnected/no IP
- `vendor_support_url` note: reuses the Motherboard's own vendor URL for integrated adapters (research-confirmed a chipset maker's own page, e.g. Realtek's, has no stable per-model pattern and its generic driver is usually inferior to the motherboard vendor's customized package) — falls back to a chipset-maker general page only when no motherboard match exists.

### OS
- Basic: os_name, version, build_number, install_date, status, summary, vendor_support_url
- Advanced: uptime, last_update_date, activation_status, architecture (64-bit)
- Health rules: yellow = updates pending; red = activation issue / critical update missing
- `vendor_support_url` note: a real, per-version Microsoft release-health page (e.g. `status-windows-11-24h2`), built from the registry's `DisplayVersion` value — not derivable from `Win32_OperatingSystem.Version` alone.

## Rules Engine Implementation Notes

- Rules are deterministic, not AI-generated. Any future AI involvement is limited to *wording* of explanations, never the pass/fail logic itself.
- Every DNA should surface at least one "confirmed good" (info-severity) status_reason when relevant checks pass, not just silence — this is a product requirement, not optional polish.
- Thresholds (e.g. "what counts as high temp") should be tunable constants, not hardcoded magic numbers scattered through code — centralize them so they can be adjusted after real-world testing on the dev's own machines.

## Third-party dependencies

- **LibreHardwareMonitorLib** — NuGet package, MPL-2.0 license. Used unmodified as a consumed library (not forked) — this keeps DeviceDNA's own source closed if desired. Licensing skim done (2026-08-15): MPL-2.0's file-modification obligation doesn't trigger since the library is unmodified; the license-availability obligation is satisfied via `THIRD-PARTY-NOTICES.md`. Revisit only if the distribution model ever changes to commercial/paid.

## Dev Support Folder
Path: D:\Claude AI Projects\projects\Personal Dev Support Folder\
Relevant sections for this project: 02-build-phase (recurring build-time roles), 03-checkpoints (periodic health checks), 04-hardening (pre-release gauntlet), 06-reference-library (patterns), 08-meta-ai-roles (role selection)
Autonomy: follow this folder's own autonomy default (see its README) for any in-folder work.
Write-back: if something reusable comes up while working here (a checklist, a fix that generalizes, a pattern that'd repeat elsewhere), write it into the support folder as it happens — don't wait to be asked. See the support folder README's "Build philosophy" section for what counts as reusable.
Roles: auto-select the right role(s) from the task itself, using 08-meta-ai-roles/ai-role-cheatsheet.md and ai-role-workflow-cheatsheet.md — don't wait to be told which hat to wear. State which hat you're wearing if it's not obvious, but don't ask permission to wear it. Multiple hats can apply to one task; wear them in sequence or name the tension explicitly rather than silently picking one and hiding a conflict.

## Known Blockers

| Blocker | Waiting on | Reported |
|---|---|---|
| None | — | — |
