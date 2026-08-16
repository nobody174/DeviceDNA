# CHANGELOG.md — DeviceDNA

Completed work lands here, most recent first. Items move here from `BACKLOG.md` once resolved, or are logged directly as phases from `ROADMAP.md` are completed.

## Unreleased

Nothing shipped yet beyond the features below.

---

## [Hardening Gauntlet run 2 of 2] Final independent pass, gauntlet effort now complete

Ran the full 10-step "Hardening — Pre-Release Gauntlet" a second and final time (run 2 of 2
requested), genuinely independent of run 1 — read the codebase fresh rather than re-checking only
the lines run 1 touched, and specifically attacked run 1's own fix per the methodology's step 6
("the sharpest findings come from attacking this session's own work"). No live-elevated-GUI
verification was possible (no way to click through a UAC prompt unattended); CPU/GPU temperature/
live-clock readings were not re-verified live, but no code on that path changed this run, so this
remains a documented gap, not a regression.

### Fixed — Project Reviewer (doc drift)
- **`DashboardView.xaml`'s strand-connector comment described a retired design.** The comment above
  `StrandCanvas` still said orbit-to-hub connector strands alternated gold/teal per connector — true
  of an earlier iteration, but `LayoutOrbit`'s actual code (and the "[Orbital dashboard: theming,
  vendor links, live-review fixes]" CHANGELOG entry) confirms every strand has used one consistent
  teal color since a user-feedback fix ("I think all should have the same wire color") landed in an
  earlier round. The code was already correct; only the comment was stale, actively misleading
  about current behavior. Fixed to describe the real single-color behavior.
- **REQUIREMENTS.md section 2's navigation line still described the retired command-deck tile
  grid's Basic-tier-then-Advanced-toggle flow**, even though section 8 (rewritten during the
  seven-role pass) and the actual shipped code both correctly describe the current orbital
  dashboard's no-toggle, all-fields-at-once detail view. Section 8's rewrite evidently didn't carry
  back to section 2's earlier, more general description of the same navigation model — a real
  doc-to-code drift a fresh full-file read caught that a lines-only diff of the seven-role pass's
  own changes wouldn't have. Fixed to point to section 8 as the current source of truth rather than
  independently re-describing (and risking re-drifting) the same flow twice.

### Reviewed, no changes needed
- **Security Auditor pass**, re-verified independently rather than trusting run 1's clean verdict:
  traced every outbound-capable call site by hand. `Process.Start` (vendor links) has exactly one
  call site, in `MainWindow.OpenVendorUrl`, itself only reachable from explicit click handlers.
  `WuaAdapter.SearchForDriverUpdates` has exactly one caller chain —
  `WindowsUpdateDriverChecker.CheckForDriverUpdates` → `WindowsUpdateDriverCheckService
  .CheckForDriverUpdates` → `MainViewModel.RunWindowsUpdateCheckAsync` — reachable only via the
  `CheckWindowsUpdateRequested` event, itself only raised by `CheckWindowsUpdateCommand`'s execute
  delegate, itself only invoked by a button click. No automatic/background/timer-driven trigger
  path exists anywhere. `RegistryAdapter` is read-only (`OpenSubKey` reads only, no
  `SetValue`/`DeleteValue` anywhere in the codebase). Every `ScanHistoryRepository` SQL statement
  re-confirmed parameterized (`$paramName` placeholders throughout, no string concatenation into
  `CommandText`). Came back genuinely clean a second time, independently.
- **QA/Playtester pass on three specific adversarial flows not covered by prior passes**: (1) Rescan
  fired while a "Check Windows Update" call is in flight on some tile — the in-flight check's async
  closure holds a reference to the *old* `DnaTileViewModel` instance, not any new one Rescan creates;
  when it completes it mutates that now-orphaned object, which nothing is bound to anymore (Rescan
  already replaced `Tiles` wholesale and reset `SelectedTile` to null) — harmless, no crash, no
  visible corruption. (2) Rapidly toggling History's DNA-type filter dropdown while scans load —
  `HistoryViewModel.LoadScans`/`ChangesViewModel.RunComparison` are both fully synchronous on the UI
  thread, so rapid changes just serialize normally with no race window. (3) Rapid orbit-node
  re-selection (A → B → A) — `DashboardView.AnimateSelectionChange` builds a fresh `Storyboard` per
  call and WPF's default `Begin()` handoff behavior (`SnapshotAndReplace`) cleanly interrupts/
  restarts any in-flight animation on the same property, and `OrbitCanvas.IsHitTestVisible` flips
  synchronously (not animation-completion-gated), so there's no dead-click window mid-transition
  either. All three came back clean — no new bugs found.
- **Devil's Advocate pass specifically attacking run 1's own fix** (the failed-Rescan state-clearing
  in `MainViewModel.RunScanAsync`'s catch block): traced whether `Tiles = new
  List<DnaTileViewModel>()` on the failure path actually reaches `DashboardView`'s `SelectedTile`
  reset the same way a successful scan's does. Confirmed: `OnViewModelPropertyChanged` only checks
  `e.PropertyName == nameof(Tiles)`, with no dependency on *why* `Tiles` changed, and the catch block
  calls `OnPropertyChanged(nameof(Tiles))` unconditionally — so a failed Rescan's empty `Tiles` reset
  fires the exact same `SelectedTile = null` path a successful one does. Also confirmed `_device =
  null` on failure correctly disables `ExportCommand`/`OpenDiagnoseCommand` (`CanExecute` predicates
  both check `_device != null`) and that the `finally` block's `InvalidateRequerySuggested()` call
  is unconditional (runs on both the success and failure paths), so the buttons reflect their real
  disabled state immediately rather than waiting on an incidental focus change. Run 1's fix holds —
  no gap found on a second, adversarial look.
- **Design Critic pass** on `DiagnoseView.xaml`/`HistoryView.xaml` against the app's one job:
  Diagnose's severity-grouped glyph/badge/message/suggestion/confidence layout and History's
  side-by-side scan-list/change-diff columns with clear From/To pickers both still read clearly for
  a non-technical user. No new issue found.
- **Release Manager pass**: `ScanHistoryRepository.EnsureSchema` is still `CREATE TABLE IF NOT
  EXISTS`-only, additive, no prior-version compatibility trap introduced this run (nothing touched
  the schema). CI/CD confirmed correctly out of scope (no pipeline exists, none was requested).

### Verified
- Full solution rebuilt clean (0 errors/0 warnings) after both doc fixes, and again as Project
  Reviewer round 2's own final check — every file changed in this run re-read fresh, both fixes
  confirmed still accurate against the real code they describe, no new issue surfaced.
- Non-elevated smoke launch (a few seconds, backgrounded, then killed) confirmed no startup crash;
  this run's only changes were XAML/Markdown comments, so no behavioral regression was possible.
- This was the final planned gauntlet run (2 of 2). The multi-role review + hardening process the
  user requested across the seven-role pass and both gauntlet runs is now complete.

---

## [Hardening Gauntlet run 1 of 2] Deeper adversarial pass on top of the seven-role review

Ran the full 10-step "Hardening — Pre-Release Gauntlet" (run 1 of 2 requested) as a deeper,
more adversarial pass on top of the seven-role review immediately above — deliberately not
re-finding the same ground, going further into the newest code (this session's own fixes) and
re-reading the whole codebase fresh rather than trusting the prior pass's summary. No
live-elevated-GUI verification was possible (no way to click through a UAC prompt unattended);
CPU/GPU temperature/live-clock readings were not re-verified live, but no code on that path
changed this run, so this remains a documented gap, not a regression.

### Fixed — Project Reviewer / QA Playtester
- **Rescan failure while Diagnose (or History) was open left stale, contradictory state.**
  `MainViewModel.RunScanAsync`'s catch block (a total scan failure, e.g. WMI service unavailable)
  correctly cleared `Tiles` and set `StatusMessage`/`OverallStatus` to reflect the failure, but
  left `_device` still pointing at the *previous* successful scan (keeping Export/Diagnose
  enabled — their `CanExecute` predicates only check `_device != null` — for data no longer
  consistent with the now-empty dashboard) and, if Diagnose was already open when the failed
  Rescan happened, left `DiagnoseViewModel` untouched: since it's only otherwise rebuilt on the
  success path (`OpenDiagnose()` or the end of a successful `RunScanAsync`), the Diagnose page
  kept silently showing the *previous* scan's findings with zero indication the new scan had
  actually failed — the same stale-state bug class the seven-role pass fixed for the *success*
  path, just not yet applied to the failure path. Fixed: the catch block now also nulls `_device`,
  clears `DiagnoseViewModel`, and resets `IsDiagnoseActive`/`IsHistoryActive` back to the
  dashboard, so a failed Rescan always leaves the UI in one honest, internally consistent state.

### Reviewed, no changes needed
- **Security Auditor pass**: confirmed every SQLite query in `ScanHistoryRepository` is fully
  parameterized (no string-built SQL anywhere), every network-reaching call (`Process.Start` for
  vendor links, the WUA COM driver-update search) is strictly behind an explicit per-click user
  action with no automatic/background trigger path, `Process.Start(url, UseShellExecute: true)`
  carries no injection risk since every URL is either a hardcoded literal or mechanically built
  from already-detected hardware strings (never user input), Export writes only to the
  user-chosen `SaveFileDialog` path with no automatic file writes elsewhere, and no
  secret/credential is ever logged (the app has no accounts/credentials to begin with). Came back
  genuinely clean — no findings.
- **Devil's Advocate pass on this session's own fixes** (plus the seven-role pass's five bugs):
  `HelixVisual`'s `CompositionTarget.Rendering` subscription correctly unsubscribes on `Unloaded`
  and the control is never re-constructed mid-session (it lives inside the detail panel, which
  stays in the visual tree), so no leak. `DashboardView`'s `DataContextChanged` subscribe/
  unsubscribe pair is symmetric and MainWindow only ever constructs one `MainViewModel`, so no
  duplicate-subscription risk either. `WindowsUpdateResultText`'s null-on-first-render case is
  handled correctly by the existing `StringToVisibilityConverter` (collapsed until a check runs).
  Resetting `SelectedTile = null` on every `Tiles` change was re-checked against a Rescan
  completing while the user is mid-navigation on Diagnose/History (not the dashboard) — confirmed
  harmless, since `SelectedTile` only affects `DashboardView`'s own internal detail-panel state,
  invisible while that view is `Collapsed`.
- **Release Manager pass**: `ScanHistoryRepository.EnsureSchema` uses `CREATE TABLE IF NOT EXISTS`
  with no versioned/migrated schema — additive-only, so there is no prior-version compatibility
  trap to guard against yet. No CI/CD to check (deliberately out of scope for this project).
- **Design Critic pass** on `DashboardView.xaml`/`DiagnoseView.xaml` against their one job (let a
  non-technical user understand PC health at a glance): no changes needed beyond what the UI
  Concept Designer pass already shipped. Diagnose's severity-grouped layout (red first, human
  language, one-line "confirmed good" entries) and the orbital dashboard's status-colored rings
  read clearly for the target audience; no new issue found.

### Verified
- Full solution rebuilt clean (0 errors/0 warnings) after the fix, and again at the end of the
  10-step sequence (Project Reviewer round 2 independently re-read every changed file rather than
  trusting this entry's own summary — no new issue surfaced on the second pass).
- Non-elevated smoke launch (a few seconds, backgrounded, then killed) confirmed no startup crash
  after the `MainViewModel` change.

---

## [Seven-role review pass + Hardening Gauntlet x2] Autonomous end-to-end quality pass

Ran a full, unattended review of the codebase using seven AI roles in dependency order (Documentation Architect → Debugger/Root-Cause Investigator → Code Style Enforcer → Data Model Auditor + UX Reduction Designer → UI Concept Designer → QA/Playtester Persona → Technical Writer — reordered from the requested random order since docs needed to establish ground truth first, bugs before style, style before structural review, structural review before UI critique, UI critique before playtesting), followed by two full runs of the "Hardening — Pre-Release Gauntlet." No live-elevated-GUI verification was possible during this pass (the working shell had no way to click through a UAC prompt unattended) — CPU/GPU temperature/live-clock readings were not re-verified live, though no code touching that path changed. Every other fix was verified via `dotnet build` (0 errors/0 warnings after every change) and, where practical, a non-elevated smoke launch.

### Fixed — Documentation Architect
- REQUIREMENTS.md section 8 (UX/Navigation) and section 10 (Privacy Principles) were still describing the retired command-deck tile grid and an inaccurate online-lookup scope; rewritten to match the current orbital dashboard and the actually-shipped vendor-link/WUA-check feature set.
- CLAUDE.md's Per-DNA Field Reference was missing `VendorSupportUrl`/`BenchmarkUrl` documentation (verified directly against the real `*Fields.cs` model files before writing) and its Platform Adapters paragraph didn't mention the newer `RegistryAdapter`.
- README.md's Features list still said vendor links were GPU/Motherboard-only; corrected to reflect all 7 DNA types.

### Fixed — Debugger / Root-Cause Investigator
- **Fabricated Memory manufacturer value**: `DeviceDetectionService.DetectMemory` coerced a null/unknown manufacturer to the literal string `"Unknown"` *before* a later honesty check (`manufacturer != null ? ... : "16 GB DDR4 Memory"`) — the check was always true, so a genuinely-unknown manufacturer displayed as `"Unknown 16 GB DDR4"` instead of the intended generic fallback. Fixed by keeping `manufacturer` properly nullable through the whole method and only introducing the `"Unknown"` literal at the final `Dna.Manufacturer` assignment, matching the honest-fallback pattern already used for Storage/Motherboard/CPU. `BuildMemoryVendorUrl` updated to accept a nullable manufacturer.
- **"Check Windows Update" button's requery gap**: `MainViewModel.RunWindowsUpdateCheckAsync` was missing the `CommandManager.InvalidateRequerySuggested()` call already applied to `RunScanAsync` for the identical reason (WPF's automatic requery doesn't fire on code-driven state changes) — the button wouldn't visibly disable/re-enable around the ~7-second live check without it. Fixed to match the established pattern.

### Fixed — Code Style Enforcer
- `App.xaml.cs` had leftover default WPF-template XML doc comments (`/// <summary>`), inconsistent with this codebase's stated plain-`//`-comment convention — converted. Everything else (file header/footer boilerplate on all 47 real source files, naming, Allman bracing, no non-English text, null-forgiving-operator safety) checked out already compliant.

### Fixed — UI Concept Designer
- **"Check Windows Update" had no button anywhere in the UI.** The feature was fully wired end-to-end — `IWuaAdapter` → `WindowsUpdateDriverChecker`/`WindowsUpdateDriverCheckService` → `MainViewModel.RunWindowsUpdateCheckAsync` → `DnaTileViewModel.CheckWindowsUpdateCommand`/`IsCheckingWindowsUpdate`/`WindowsUpdateResultText`, even a dedicated `CheckingLabelConverter` already sitting in `App.xaml`'s resources — but no `.xaml` file actually placed a `Button` bound to it. Very likely dropped during the orbital-dashboard rebuild, the same regression class as the vendor-link name that had to be re-added earlier. Added the button + result-text row to `DashboardView.xaml`'s detail view, gated by `SupportsWindowsUpdateCheck` (CPU/GPU/Network/Motherboard only).

### Fixed — QA / Playtester Persona
- **Stale detail view after Rescan**: `DashboardView.SelectedTile` is a code-behind `DependencyProperty`, not bound from `MainViewModel.Tiles` — a Rescan rebuilds `Tiles` as an entirely new list of `DnaTileViewModel` instances (see `MainViewModel.RunScanAsync`), so a detail view left open across a Rescan kept silently showing the previous scan's now-orphaned tile. Fixed by having `DashboardView` subscribe to its `DataContext`'s `PropertyChanged` and reset `SelectedTile` to null whenever `Tiles` changes, returning the user to the (now fresh) orbit view.
- **Diagnose page not refreshing after Rescan**: the top bar (Rescan/History/Diagnose/Export) stays reachable across every page, including Diagnose itself — but `MainViewModel.DiagnoseViewModel` was only ever rebuilt inside `OpenDiagnose()`, so a Rescan triggered while already looking at Diagnose left it showing the previous scan's findings with no indication anything had changed. Fixed by rebuilding `DiagnoseViewModel` from the new `Device` at the end of `RunScanAsync` whenever `IsDiagnoseActive` is true.
- History/Changes flows (empty history, single scan, same-scan comparison, missing scan-load) reviewed and already handle every edge case gracefully with clear user-facing messages — no changes needed.

### Fixed — Data Model Auditor + UX Reduction Designer
- Full Basic/Advanced schema for all 7 DNA types verified against CLAUDE.md's documented shape — no missing/extra/renamed fields. `DnaTileViewModel`'s existing `AddIf`/conditional field-row pattern already prevents any always-null field from rendering as UI clutter; no changes needed.

### Verified
- Full solution rebuilt clean (0 errors/0 warnings) after every fix in this pass, individually and at the end.
- Non-elevated smoke launch confirmed no startup crash after the UI/ViewModel changes.

---

## [Orbital dashboard: theming, vendor links, live-review fixes] Post-rebuild feedback round

After the orbital DNA-helix redesign shipped (see the entries below), the user ran the app live across several rounds and gave direct feedback — real bugs, real regressions from the rebuild, a couple of genuine new feature ideas, and one design decision (History converted to in-place navigation, matching Diagnose). This entry covers the whole round.

### Fixed — real bugs
- **Rescan button text disappeared while scanning** ("just turns white"). Root cause: WPF's default disabled-button visual is low-contrast, and `RescanCommand`'s `CanExecute` correctly disables it mid-scan — the text was still there, just unreadable. Fixed via a proper themed `Button` style (see below) with an explicit disabled-state `Foreground`.
- **Button gold frame randomly missing on fresh launch**, only reappearing after Alt-Tabbing away and back. Root cause, found in two passes: (1) an initial trigger-ordering issue in the new themed `Button` style, where `IsEnabled`/`IsMouseOver` triggers lacked an explicit "restore to default" case for every state, fixed by adding an explicit `IsEnabled=True` trigger; (2) the *real* remaining cause — WPF's `CommandManager.RequerySuggested` (what `RelayCommand.CanExecuteChanged` is wired to) only re-evaluates `CanExecute` on user-input/focus events, never automatically when a command's underlying state changes in code. `Diagnose`/`Export`/`Rescan` all gate on `_device`/`IsScanning`, both unset at construction — so their real "should be enabled" state wasn't reflected in the UI until *something* (like a window focus change) happened to trigger a requery. Fixed by calling `CommandManager.InvalidateRequerySuggested()` explicitly right after a scan completes, in `MainViewModel.RunScanAsync`.
- **History dropdowns unreadable** (white-on-near-white). No `ComboBox` styling existed anywhere in the app, so it rendered with WPF's default light theme against this app's dark window. Added a full themed `ComboBox` style (dark background, primary-text foreground, gold-on-hover popup items) to `App.xaml`.
- **Detail view's clickable vendor-link name regressed** — dropped entirely when the detail view was rebuilt from scratch for the orbital redesign. Re-added (`DetailNameText_MouseLeftButtonDown` in `DashboardView.xaml.cs`), reusing `DnaTileViewModel.OpenVendorUrlCommand`, which was already correctly wired end-to-end from the earlier grid UI and just needed a new click handler.
- **Storage: Western Digital's newer SSD line had no vendor link.** `ExtractStorageManufacturerFromModel`'s deliberately-narrow vendor-prefix list only checked `"WDC"` (WD's legacy HDD/older-SATA-SSD prefix); WD's own-branded SSD lines (WD Black/Blue/Green, e.g. the WD Black SN850's real model string `"WDS100T1X0E-00AFY0"`) use a distinct `"WDS"` prefix, confirmed via research as WD's own SSD-era naming convention, safe to add as a separate explicit entry (not collapsed into a shorter, riskier shared `"WD"` root — same collision-risk reasoning that already excludes Seagate's `"ST"`/Crucial's `"CT"`).

### Added — real feature requests, researched and verified before building
- **No global `Button`/`ToggleButton`/`ComboBox` styling existed** before this round — every button was individually inline-styled, inconsistently (Export/Rescan never had a visible border at all; History/Diagnose did). Added one themed `Button` style to `App.xaml`: gold border by default, **red border on hover** (user feedback: "I rather have it just to visual show it with a darker frame... make the button we're hovering over get a red frame instead" — replacing WPF's default light-blue/white hover highlight), explicit disabled-state styling. All buttons across the app simplified to use it.
- **History converted from a separate popup Window to in-place navigation** — same pattern as Diagnose, per direct user request ("I want that to swap windows inside the main app as well as we did with the diagnose popup"). `HistoryWindow.xaml`/`.xaml.cs` deleted; new `HistoryView.xaml`/`.xaml.cs` (a `UserControl`, DataContext is `MainViewModel`) added; `HistoryWindowViewModel` moved out of the deleted window file into `Presentation/HistoryViewModel.cs`; `MainViewModel` gained `IsHistoryActive`/`HistoryViewModel` state alongside the existing `IsDiagnoseActive`/`DiagnoseViewModel`, with only one of Diagnose/History able to be active at a time since they share the content area.
- **Explicit status dot re-added to the orbital detail view's header.** The icon border already picks up the DNA's status color, but that alone didn't read as "the status light" to the user the way the old command-deck grid's dedicated colored dot did ("the green light one each component is gone?... isn't any inside the orbit in the detailed view"). Added a small `Ellipse` next to the type-label subtitle, bound to the same `StatusBrushKey`.
- **Network detail view now shows connection type (+ WiFi signal strength) prominently in the header**, not just buried as a field row — new `DnaTileViewModel.NetworkSubtitle` computed property, null (and hidden) for every non-Network DNA.
- **New vendor links for Storage, Memory, Network, and Windows OS** — extending the existing GPU/Motherboard/CPU pattern, all researched first rather than guessed:
  - **Storage** (`StorageBasic.VendorSupportUrl`) and **Memory** (`MemoryBasic.VendorSupportUrl`): general manufacturer support pages only (Samsung, WDC/WDS, Kingston, SanDisk, Intel for storage; Corsair, G.Skill, Kingston, Crucial/Micron for memory) — research confirmed no vendor in either category exposes a URL derivable from a WMI model/part-number string, same honesty pattern already established for Gigabyte/ASRock/MSI motherboards.
  - **Network** (`NetworkBasic.VendorSupportUrl`): reuses the *motherboard's* vendor URL when the adapter looks integrated (name contains "Family Controller," WMI's real, consistent phrasing for onboard LAN chipsets) rather than linking to the chipset maker's (e.g. Realtek's) own site — research found Realtek's own portal has no stable per-model URL and its generic driver is usually inferior to the motherboard vendor's customized package for that exact board; falls back to a chipset-maker general page only when no motherboard match exists.
  - **Windows OS** (`OsBasic.VendorSupportUrl`): a real, per-version Microsoft release-health page (e.g. `status-windows-11-24h2`), built from a new registry read (`IRegistryAdapter.GetLocalMachineStringValue`, reading `DisplayVersion` under `CurrentVersion` — a genuinely stable, research-confirmed URL pattern, not previously read anywhere in the app). Falls back to the general release-health hub for Windows 10 or when `DisplayVersion` isn't available.
- **CPU-Z Validator benchmark link** (`CpuBasic.BenchmarkUrl`, shown as a new "View Benchmark ↗" button in the CPU detail view). User found and personally verified this link (valid.x86.fr, CPU-Z's own community validation database, sorted by highest recorded frequency for a given CPU model) works via CPU-Z's own UI. Its `psn` URL parameter was confirmed, by directly hex-decoding the user's own example URL, to be simple ASCII-hex of the exact CPU name string — verified to match WMI's `Win32_Processor.Name` byte-for-byte for that CPU. A genuinely new, distinct link type from `VendorSupportUrl` (a community benchmark database, not a vendor page), so `DnaTileViewModel` gained a separate `BenchmarkUrl`/`OpenBenchmarkUrlCommand` pair rather than overloading the existing vendor-link plumbing.

### Investigated and declined, honestly
- **GPU-equivalent benchmark link**: researched every major GPU benchmark site (TechPowerUp, UserBenchmark, PassMark/VideoCardBenchmark, Geekbench Browser, 3DMark) — none exposes a per-model URL derivable from a GPU name string; all require an opaque internal numeric ID obtainable only via a search step, which this app's own no-scraping rule forbids working around. UserBenchmark also carries a documented reputation for skewed scoring, an independent reason to avoid it even if the URL worked. Logged in BACKLOG.md as genuinely open, not rejected — revisit only if a site with a real name-derivable pattern is found.

### Verified
- Full solution rebuilt clean (0 errors/0 warnings) after every change in this round.
- Live-tested by the user across multiple passes: button frame/hover behavior on fresh launch (no Alt-Tab needed) and after navigation, History's in-place swap and readable dropdowns, all new vendor links (Storage/Memory/Network/OS) clickable and correct, CPU-Z Validator benchmark button, and the corrected Memory display name.

---

## [Orbital DNA dashboard shipped] Command-deck tile grid replaced with the DNA-helix/orbital redesign

Built the redesign the user proposed and deferred during the first-look UI pass (see above) — a central "This Computer" hub with one orbit node per DNA component, connected by curved strand lines, replacing the tile grid entirely. Designed first as a clickable HTML/CSS/JS mockup (published as an artifact) so the user could react to layout, icons, and the interaction model before any WPF was touched — several rounds of mockup iteration (icon redesigns, card density, strand coloring) happened there first, cheaply, before committing to the real build.

Built in five explicit stages, each verified live before moving to the next: (1) static orbit layout — hub + nodes positioned on a circle, no interaction; (2) real per-DNA-type icons (converted from the mockup's SVG to WPF `Path` geometry, since `Path.Data` has no `<rect>`/`<circle>` primitives — CPU chip-with-pins, GPU card, two-RAM-sticks, classic HDD platter-and-arm, motherboard layout, WiFi arcs, Windows logo), status-color rings, and curved node-to-hub connector "strands"; (3) click-to-expand — clicking a node swaps the orbit view for a full detail panel showing every field at once (Basic + Advanced, no tier toggle — a component with full-screen space doesn't need one hidden behind a click); (4) animating the swap itself (opacity/scale/translate `Storyboard`s, not an instant `Visibility` snap); (5) final wiring and dead-code cleanup (`DnaTileViewModel.IsExpanded`/`ToggleExpandCommand`/`ShowAdvanced`/`ToggleAdvancedCommand` removed — genuinely unused once the old tile-grid's Basic/Advanced-toggle interaction no longer existed).

### Design decisions, made explicit with the user before building
- Node size/orbit fixed for the standard 7 v1-scoped DNA types; only shrinks/grows if a future scan ever finds more than 7 (multiple GPUs, multiple disks) — an earlier draft wrongly made the orbit elliptical and shrink-capable for the normal case too, corrected per direct user pushback.
- Detail view's left column: a custom-drawn, continuously animated vertical DNA double-helix (`HelixVisual.xaml`/`.xaml.cs`) — parametric sine-curve geometry (not static path data) so the same drawing code can also animate a slow rotation via a `CompositionTarget.Rendering` phase-offset callback, the standard 2D technique for a convincing spinning-helix look. Colors reflect `Logo.png`'s real rainbow-gradient helix palette, redrawn as one continuous vertical strand rather than the logo's horizontal crossed-bowtie shape (a deliberate simplification — a literal bowtie crossing doesn't animate as a continuous spin).
- Detail view's right column: fixed 3-column field grid (`UniformGrid Columns="3"`, not width-based wrapping) per explicit user requirement, wrapping to as many rows as a DNA's field count needs.
- A real bug in the initial detail-panel build (the whole page rendered "stuck behind" the orbit view, looking messy) was root-caused, not just patched around: the detail panel `Grid` had no `Background` set at all, so it was transparent everywhere except where child controls explicitly painted, letting the still-visible orbit show through the gaps — not a failure of the animation itself, which was working correctly the entire time once the transparency was fixed.

### Verified
- Full solution rebuilt clean (0 errors/0 warnings) after every stage.
- Each of the five stages launched and confirmed live with the user before moving to the next — including the transparency-bug fix, confirmed resolved on the next live run rather than assumed fixed from code inspection alone.

---

## [First-look UI pass] Real bugs and UX fixes from the user's first live look at the app

The user ran the app live for the first time and gave direct first-look feedback. Sorted into confirmed bugs (fixed below) vs. a bigger visual-redesign idea (a DNA-helix/orbital layout) that was deliberately deferred to its own separate conversation rather than folded into this bug-fix pass.

### Fixed
- **Top-bar logo was illegible.** A 40×40 `Image` was squeezing the wide `Logo.png` wordmark (a non-square rectangle with "DeviceDNA" text baked in) into a square box, distorting it into an unreadable sliver. Fixed by switching to the square `assets/icon.ico` glyph (already a proper cropped "DD"/DNA-helix mark — see the earlier "[.msi installer shipped]" entry, which first wired this icon in for the exe/Start Menu/Apps-list) at 96px — doubled again after user feedback that the first fix was still too small. The redundant "Device"/"DNA" text labels next to the icon were then removed entirely per further feedback, once the icon was large enough to read on its own.
- **Rescan looked like the app had frozen.** `MainViewModel.RunScan()` called `DeviceScanService.ScanDevice()` (real WMI + LibreHardwareMonitor I/O) synchronously on the UI thread, with no loading indicator — clicking Rescan blocked the whole window with no feedback. Converted to `RunScanAsync()` using `Task.Run`, added an `IsScanning` property driving a "Scanning..." button label (new `RescanLabelConverter`) and disabling re-entrant clicks, same async pattern already used for the Windows Update check.
- **Tile-row layout stretched sibling tiles when one was expanded.** WPF's `WrapPanel` doesn't stretch siblings by height, but each tile `Border`'s default `VerticalAlignment="Stretch"` let it fill the row's height anyway once one tile in the same row grew taller — leaving visible empty space at the bottom of unexpanded neighbors. Fixed with `VerticalAlignment="Top"` on the tile `Border`.
- **Advanced toggle button looked "stuck".** No custom `ToggleButton` style existed anywhere, so WPF's default theme applied the OS accent-color (bright blue) checked-state highlight, clashing with the dark/gold theme and reading as broken/stuck rather than a clean toggle. Added a themed `ToggleButton` style in `App.xaml` — neutral background regardless of checked state, only the border picks up gold when checked, as a subtle indicator instead of a jarring color swap.
- **CPU had no vendor link**, unlike GPU/Motherboard. Added `CpuBasic.VendorSupportUrl` (new field, same pattern as `MotherboardBasic.VendorSupportUrl`) and `BuildCpuVendorUrl` in `DeviceDetectionService.cs` — links to AMD's/Intel's general processor product page (not a driver page like GPU's link, since AMD's driver-download page doesn't resolve per-CPU-model and Windows Update already covers CPU microcode; still a real, stable, always-correct URL, never a guessed one). Wired into `DnaTileViewModel.VendorUrl`.
- **Diagnose opened as a separate popup Window.** User feedback: a second window for what's fundamentally the same scan data, just grouped differently, broke the sense of a single app, and had no way back except closing it. Restructured to in-place navigation: extracted the dashboard tile grid into a new `DashboardView` `UserControl` and Diagnose's content into a new `DiagnoseView` `UserControl` (with its own "← Back to Dashboard" button), both swapped into `MainWindow`'s content area via `MainViewModel.IsDiagnoseActive`. The app-level top bar (logo, Rescan/History/Export) stays visible across both pages. `DiagnoseWindow.xaml`/`.xaml.cs` deleted; `OpenDiagnoseRequested` event replaced with in-ViewModel state (`IsDiagnoseActive`, `DiagnoseViewModel`, `GoBackToDashboardCommand`). History/Export remain unaffected, separate windows/dialogs, since the user didn't flag those.
- **"Driver: Not applicable" row cluttered DNAs with no driver concept** (OS, CPU, Memory, Motherboard, Storage). User: "just looks unprofessional." `DnaTileViewModel.BuildAdvancedFields` now omits the Driver rows entirely when `!Driver.IsApplicable`, instead of showing an explicit placeholder row.

### Investigated and confirmed correct, no change needed
- **Storage showing no driver info**: user asked directly whether storage disks have drivers. Research confirmed Windows' storage "driver" concept lives on the storage *controller* (a separate PnP device, often shared by multiple disks), not the individual physical disk WMI/DeviceDNA's Storage DNA represents — attributing a controller's driver info to one specific disk would be exactly the kind of fabrication REQUIREMENTS.md forbids, especially on multi-NVMe systems. `Storage.Driver = DriverInfo.NotApplicable` is correct as-is.

### Deferred to a separate conversation
- **DNA-helix/orbital visual redesign** — user's idea for a fundamentally different visualization (a "this computer" central DNA node with orbiting component nodes, expanding on click) is a real design direction worth exploring, but is a new visual paradigm, not a bug-fix-pass item. Explicitly deferred per user's own sequencing choice ("bugs first, then talk redesign").

---

## [CPU sensor fix — PawnIO bundled] Real fix for CPU temperature/live-clock always reading 0

During the first-look UI pass above, the user caught that CPU "Current Temp" and "Live Clock" always showed 0 — and, critically, refused to accept "no clean fix exists" after an initial research pass concluded that, because Speccy and CPU-Z were independently proven (via a live side-by-side screenshot) to read this data correctly on the identical machine at the same time. This is the record of the investigation that followed and its actual root cause and fix.

### Root cause (confirmed, not guessed)
LibreHardwareMonitorLib reads AMD SMU (System Management Unit) temperature/clock sensors through **PawnIO**, a separate signed kernel driver LHM depends on but does not bundle or reliably auto-install. On this dev machine, PawnIO's driver was never actually installed — confirmed directly (`Get-CimInstance Win32_SystemDriver` returned nothing for PawnIO before the fix). Without it, these specific sensors silently return a literal `0` instead of throwing or being omitted — not a hardware limitation, not a wrong register address (LHM's Zen 3/Vermeer register map is correct), just a missing driver dependency LHM's own installer-detection logic failed to catch. This exact failure mode is tracked upstream as LibreHardwareMonitor issues #1875, #1937, #2220, and resolved in community discussion #1904.

### Ruled out along the way (in order investigated)
- **LHM version upgrade** (0.9.6 → 0.9.7-pre724): tested live via a throwaway probe — the newer pre-release exhibited the identical 0-reading bug. Not a version problem.
- **ZenStates-Core** (open-source Ryzen SMU library used by ZenTimings): initially recommended by research as MIT-licensed and driver-independent — both turned out to be wrong on direct inspection of its actual source: it's GPLv3, and it also internally depends on PawnIO, so it wouldn't even have fixed the bug. Ruled out on both counts.
- **Writing a from-scratch SMU reader against WinRing0** (the older, pre-PawnIO driver): WinRing0 itself is a permissively-licensed (BSD-modified), already-signed, legitimately redistributable driver — but Microsoft Defender has been actively quarantining `WinRing0.sys` system-wide since September 2025 as a flagged "vulnerable driver," confirmed via real incidents affecting other shipping tools (Aqua Computer's Aquasuite; OCCT had to build an entirely new closed-source driver in response). Dead end, not a licensing or effort problem — it simply doesn't run on current Windows.
- **AMD's official Ryzen Master Monitoring SDK**: real, free, and its EULA does permit redistribution — but its public API surface looked aggregate-oriented (system-level peak/avg, not confirmed per-core clock or raw Tctl), and was set aside once the PawnIO root cause was found, since no fallback was needed.

### The actual fix
PawnIO's own official installer (v2.2.0, from `namazso/PawnIO.Setup` on GitHub, Authenticode-signed, verified) fixes this completely with zero DeviceDNA code changes — LHM's existing, already-correct AMD sensor code just works once the driver it depends on is actually present. Verified live, step by step: uninstalled PawnIO → confirmed CPU temp/clock regressed to 0 in DeviceDNA → reinstalled PawnIO manually → confirmed real values (48°C-range temps, real per-core clocks) returned immediately, matching Speccy/CPU-Z's readings.

**Shipped**: DeviceDNA's `.msi` installer (`installer/Product.wxs`) now bundles PawnIO's official installer (`installer/vendor/PawnIO_setup.exe`, unmodified) and silently installs it as a deferred `CustomAction` during DeviceDNA's own install (`ExeCommand="-install -silent"` — PawnIO's real silent flag, a custom hyphen-prefixed verb/modifier parser, not the WiX-Burn-style `/quiet` first assumed and confirmed wrong via a live test returning Win32 error 87; found via two independent sources after the wrong-flag failure). `Return="ignore"`: if PawnIO's install ever fails for any reason, it must not block or roll back DeviceDNA's own install — CPU temp/clock would just stay at their pre-fix state on that one machine, never the whole app failing to install.

### Licensing note
PawnIO is GPLv2-licensed, with an explicit exception in its own license text permitting "independent modules that communicate with PawnIO solely through the device IO control interface" — DeviceDNA does not vendor, modify, or statically link any PawnIO source; it only bundles and silently runs PawnIO's own official unmodified installer, and LibreHardwareMonitorLib (not DeviceDNA directly) talks to the resulting driver via IOCTL. Documented in full in `THIRD-PARTY-NOTICES.md`'s new PawnIO section. This is the same category of question that correctly ruled out ZenStates-Core (GPLv3, no equivalent exception) earlier in the investigation — the two were not treated the same, on purpose.

### Verified
- Full end-to-end test performed twice: uninstall PawnIO → run DeviceDNA's `.msi` fresh (silent, elevated) → confirm PawnIO driver comes up `Running` automatically (no manual step, no GUI interaction) → launch the installed DeviceDNA.exe → confirm real CPU temp/clock values in the UI. Confirmed working both times, including after the silent-flag fix.
- Full solution and installer rebuilt clean (0 errors/0 warnings; one pre-existing benign ICE60 warning on `e_sqlite3.dll`, unrelated).

---

## [License decided] All Rights Reserved license, per-file header/footer applied across the codebase

Partially closed BACKLOG.md item 9 (distribution/license half; the marketing/landing-page half remains open) during the numbered walkthrough. User's plan: DeviceDNA distributes free via GitHub and their Patreon, no paid strategy. User initially asked for MIT, but also said "I don't want people to alter my app" — flagged this as a direct contradiction rather than applying it silently: MIT explicitly grants the right to modify and redistribute modified copies, which is the opposite of what was described. Explained the practical difference (MIT = anyone can fork/modify/relicense; All Rights Reserved = free to download and run, but changes/suggestions route through the author, not forks) and recommended All Rights Reserved as the correct fit. User agreed, confirming the model explicitly: free to use, but changes come via email/comments to the author rather than forking.

### Added
- **`LICENSE`** (repo root) — All Rights Reserved, 2026 nobody174, spelling out the free-to-run/no-redistribution/no-modification-without-permission terms and pointing to the author's contact/repo/Patreon.
- **Per-file header/footer applied to all 45 real (non-generated) C# source files** across all five projects — the user's standard convention (author, repo, Patreon, license line, signature line) plus a footer crediting Claude Code assistance. `AssemblyInfo.cs` (auto-generated boilerplate) deliberately excluded as not the kind of file this convention applies to.

### Verified
- Full solution rebuilt clean (0 errors/0 warnings) after the header/footer insertion across all 45 files — confirms no file's leading directives (usings, namespace, pragmas) were disturbed by prepending the header comment block.

### Addendum: LibreHardwareMonitorLib MPL-2.0 skim (BACKLOG item 10, closed same session)
With the non-commercial/free distribution model now confirmed, closed the long-open "MPL-2.0 licensing skim not done" item too. MPL-2.0's real obligation only applies to modified MPL-licensed files — moot here since LibreHardwareMonitorLib is consumed unmodified as a NuGet package. The obligation that does apply regardless (making the license text available to end users of the redistributed library) is now satisfied via a new **`THIRD-PARTY-NOTICES.md`** at the repo root, also covering Microsoft.Data.Sqlite and Microsoft.Win32.Registry/System.Management (all MIT).

---

## [.msi installer shipped] Real WiX-based installer with Start Menu shortcut, uninstall, and app icon

Closed BACKLOG.md item 8 during the numbered walkthrough. Weighed WiX (`.msi`) vs. Inno Setup (`.exe`) — WiX is more "native"/professional (Windows' own install technology, integrates with Apps & Features the same way commercial software does) but has a steeper XML authoring curve; Inno Setup is much faster to author but produces a `.exe`, not an `.msi`. User explicitly chose WiX, reasoning that the extra authoring effort falls on Claude, not them, and they have the time either way — `.msi` was the more "professional-feeling" outcome they wanted.

### Added
- **`installer/DeviceDNA.Installer.wixproj` + `installer/Product.wxs`** — new WiX v6 (SDK-style `.wixproj`, `wix` dotnet tool) installer project. Hand-authored file list (not harvested) for the 9 files in the real `dotnet publish -r win-x64 -p:SelfContained=true -p:PublishSingleFile=true` output — the exe plus its 8 sidecar native DLLs (WPF interop + SQLite + Mono POSIX helper libs), all installed into the same directory (required — .NET's native-DLL probing looks alongside the launching exe, not in a subfolder). Fixed `UpgradeCode` GUID with a `MajorUpgrade` element so a future v1.1 `.msi` will cleanly replace v1.0 rather than fail or duplicate-install. Start Menu shortcut and full Apps & Features uninstall registration included.
- **App icon wired in for the first time.** Discovered `assets/icon.ico` already existed (a proper square-cropped "DD"/DNA-helix glyph, correctly distinct from the wide `Logo.png` wordmark used elsewhere in the UI) but had never been referenced anywhere in the project — the exe, Start Menu shortcut, and taskbar were all using the generic default icon. Set as `ApplicationIcon` in `DeviceDNA.UI.csproj` and as `ARPPRODUCTICON` in the installer.

### Fixed during implementation
- **WiX v7 (the version `dotnet tool install --global wix` installs by default) requires accepting a paid "Open Source Maintenance Fee" EULA to build** — a new FireGiant commercial licensing requirement, not present in v6. Rather than accept a licensing commitment on the user's behalf, this was surfaced directly; user chose to pin to WiX v6.0.1 (free/OSS, and the version the research pass had actually verified against) instead.
- **Installer initially defaulted to 32-bit** (`Program Files (x86)`), which would have been a functional mismatch against the win-x64 self-contained publish output. Root-caused via a failed real install attempt (`Error 1925`, then confirmed by checking `INSTALLFOLDER` in the msiexec log) — fixed by setting `Platform=x64` in the `.wixproj` (WiX v6 exposes platform via the project property, not a `Package` XML attribute).

### Verified
- Full solution rebuilt clean (0 errors/0 warnings) with the icon wired in.
- Real install/launch/uninstall round-trip performed on this dev machine via elevated `msiexec`: confirmed the exe, all 8 sidecar DLLs, and the Start Menu shortcut landed in `C:\Program Files\DeviceDNA\`; confirmed a correct Apps & Features entry (`DisplayName=DeviceDNA`, `DisplayVersion=1.0.0`, working `UninstallString`); launched the installed exe from Program Files and confirmed it stayed responsive; ran a full uninstall and confirmed the exe, install folder, Start Menu shortcut, and Apps & Features entry were all completely removed afterward.
- One benign build warning remains (WIX1076/ICE60: `e_sqlite3.dll` has no version resource / language attribute) — a standard, harmless MSI validation note for unmanaged native DLLs, not a functional issue.

---

## [Storage sensor correlation fixed] Disk-to-sensor temperature matching now uses physical drive index, not model-string substring

Closed BACKLOG.md item 5 during the numbered walkthrough. The user asked whether WMI's per-disk serial number could be used to disambiguate two identically-modeled disks (a known accepted-risk bug where substring-matching LibreHardwareMonitor sensor names against WMI disk models could misattribute temperature readings between two same-model drives). WMI's side already exposes a real serial (confirmed via `wmic diskdrive get model,serialnumber`), but the actual gap was on LibreHardwareMonitorLib's (LHM) side — research into LHM's current source confirmed it does not expose a serial number on its public API (a known, currently-unresolved LHM GitHub issue), but does expose something better for this purpose: its storage `Identifier` embeds the Windows physical drive index (`StorageDeviceNumber`), the same OS-level integer WMI reports as `Win32_DiskDrive.Index`.

### Added / Fixed
- **`RawSensorReading.HardwareIdentifier`** (`DeviceDNA.PlatformAdapters`) — new field carrying LHM's `IHardware.Identifier.ToString()`, populated in `LibreHardwareMonitorAdapter.CollectFromHardware`.
- **`DeviceDetectionService.DetectStorage`** now correlates each disk to its LHM sensor by parsing the trailing integer from `HardwareIdentifier` (new `ParseTrailingIndex` helper) and matching it against WMI's `Win32_DiskDrive.Index`, instead of matching on a `HardwareName`/`Model` substring. The old substring match is kept only as a fallback for the rare case the index can't be parsed.

### Verified
- Live on this dev machine (2 different-model NVMe drives, via a throwaway elevated probe project): LHM's `Identifier` values were `/nvme/0` and `/nvme/1` — notably not the `/storage/nvme/N` format initially guessed by research, confirming the value of checking directly rather than trusting the predicted format. `Get-CimInstance Win32_DiskDrive` confirmed `Index 0` = the same drive as `/nvme/0`, `Index 1` = the same drive as `/nvme/1` — exact match.
- Full solution rebuilt clean (0 errors/0 warnings); full GUI app launched and stayed responsive with the fix live.
- Could not reproduce the original bug's triggering condition directly (this dev machine has two different-model drives, not identical models), but the fix addresses the confirmed root cause (shared stable index vs. ambiguous model string) rather than the symptom.

---

## [Arbitrary scan-pair comparison shipped] "From"/"To" dropdowns added to the Changes view

Closed BACKLOG.md item 4 during the numbered walkthrough. The user proposed a `\logs` folder of per-scan flat files as a path to enabling arbitrary scan comparison; before building that, confirmed the existing SQLite-backed `ScanHistoryRepository` already persists every scan and already supports loading any past scan by id (`LoadScan`) — the missing piece was purely a picker UI in the Changes view, which had hardcoded "compare the two most recent scans." Explained the tradeoff (flat files would duplicate the existing structured store, lose SQL querying, and not add any capability the DB doesn't already have) and the user agreed to build on the existing system.

### Added
- **`ChangesViewModel`** (`DeviceDNA.UI.Presentation`) now holds the full scan list as `ScanOptions` and exposes `FromScan`/`ToScan` selectable properties, defaulting to the two most recent scans (preserving prior fixed behavior) and recomputing the diff via the existing `ScanChangeDetector.Compare` whenever either selection changes.
- **UI**: two "From"/"To" `ComboBox` dropdowns added to the History window's Changes column, above the existing comparison summary.

### Verified
- Full solution rebuilt clean (0 errors/0 warnings).
- Full GUI app launched and stayed responsive with the new bindings in place.

---

## [Windows Update driver check shipped] Opt-in "Check Windows Update" button for CPU/GPU/Network/Motherboard

Follow-up to the vendor-link feature below, prompted by the user surfacing third-party research claiming the Windows Update Agent (WUA) COM API supports a `Type='Driver'` search. Rather than accept or dismiss the claim, it was re-verified directly: a dedicated research pass confirmed the API genuinely works as described, but that its practical coverage for GPU/BIOS updates specifically (what this user cares about most) is weak — WU rarely carries the newest GPU/BIOS packages as fast as vendor sites do. Decision: ship it as a supplementary, explicitly opt-in check alongside the existing vendor link-out, not a replacement for it.

### Added
- **`IWuaAdapter`/`WuaAdapter`** (`DeviceDNA.PlatformAdapters`) — late-bound COM interop against `Microsoft.Update.Session` (`Type.GetTypeFromProgID`, no NuGet/PIA dependency needed), searches `IsInstalled=0 and Type='Driver'`. Fails closed (catches broadly, returns empty) rather than surfacing a COM error to the user.
- **`WindowsUpdateDriverChecker`** (`DeviceDNA.DetectionEngine`) — the seam class, mirroring `DeviceDetectionService.CreateDefault()`'s existing pattern, so the Application layer never references PlatformAdapters directly.
- **`WindowsUpdateDriverCheckService`** (`DeviceDNA.Application`) — wraps the checker, returns a success/failure result rather than throwing.
- **UI**: a "Check Windows Update" button appears on CPU/GPU/Network/Motherboard tiles only (`DnaTileViewModel.SupportsWindowsUpdateCheck`), disabled and showing "Checking..." while the async COM call is in flight (`Task.Run`, ~7s observed), result text shown inline afterward. GPU/Motherboard results include an explicit caveat: "(Windows Update often lags behind the vendor's own site for this hardware — check the link above too.)" Strictly user-triggered, never run automatically on scan (REQUIREMENTS.md section 10).

### Fixed during implementation
- First draft of `WindowsUpdateDriverCheckService` referenced `DeviceDNA.PlatformAdapters.WuaAdapter` directly from the Application layer — caught before it caused a build/architecture problem, since it violates CLAUDE.md's strict layering rule. Corrected by routing through the new `WindowsUpdateDriverChecker` seam in DetectionEngine instead.

### Verified
- `WuaAdapter` verified live against real Windows Update via a standalone probe project before UI wiring: completed in 7.6s, found 0 applicable driver updates (honest result on this well-maintained dev machine), no exceptions.
- Full solution rebuilt clean (0 errors/0 warnings) with the feature fully wired.
- Full GUI app launched and stayed responsive with the new bindings in place.

---

## [Privacy scope clarified + vendor links shipped] Clickable GPU/Motherboard names, REQUIREMENTS.md section 10 rewritten

A real product-direction conversation with the user clarified a misreading baked into the project since planning: REQUIREMENTS.md's "no cloud requirement" was about **user data never leaving the device** (no accounts, no telemetry, no sync) — it was never meant to forbid the app from ever making an outbound network call. This had caused several earlier phases (Phase 3's GPU driver-staleness rule, Phase 3's Motherboard BIOS-update rule) to be deferred more broadly than necessary. Re-audited every prior decision made under the old, stricter reading; only two were genuinely affected (see below) — the OS reboot-pending rule (Phase 3/BACKLOG pass) and the SQLite-backed local scan history were both already correctly scoped and needed no change.

### Decision, researched and made explicit
- **REQUIREMENTS.md section 10 rewritten** to distinguish "your data stays local" (unconditional) from "the app may make user-triggered, read-only outbound requests for public vendor information" (allowed, always opt-in per action, never automatic on a routine scan).
- **Live "check for updates" (fetching/scraping a vendor's site to determine current driver/BIOS freshness) was researched in depth and explicitly rejected for now** — not because it's technically infeasible (research found real, working paths for NVIDIA, AMD, and ASUS), but because of accumulated risk: NVIDIA's ToS literally prohibits automated site access even for a single user-triggered lookup; the only genuinely free driver-database aggregators (DevID.info, DriverGuide, TechPowerUp) are paid/licensed-only with no self-serve API; and Verdict (this user's other project, independently investigated the identical problem for its own driver-scanning feature) reached the same conclusion and ships a `NoOpDriverVersionSource` stub rather than a live scraper — strong independent confirmation this is the right call, not just this project's own caution.
- **What ships instead**: DeviceDNA already detects installed driver version/date and BIOS version/date entirely locally (no change). New: GPU and Motherboard DNA names are clickable, opening the vendor's official product/support page in the user's default browser — the user checks freshness themselves with one click, exactly the manual lookup they'd otherwise do via a search engine. The app never fetches, parses, or scrapes that page itself.

### Added
- **`DriverInfo.SourceUrl` now populated for GPU** (previously always null, despite being defined in the schema since planning) — `BuildGpuVendorUrl` in `DeviceDetectionService.cs` returns AMD's driver-download page pre-filled with a slug built from the GPU name (a real, stable, research-confirmed URL pattern), or NVIDIA/Intel's official top-level driver page (not a per-model guess, since neither vendor exposes a stable per-model URL without an undocumented API).
- **New `MotherboardBasic.VendorSupportUrl` field**, populated via `BuildMotherboardVendorUrl` — ASUS gets a real per-model BIOS-support URL (verified live: `TUF GAMING B550-PRO` → `asus.com/supportonly/TUF%20GAMING%20B550-PRO/helpdesk_bios/`), Gigabyte/ASRock/MSI get their general support page (research found no clean per-model URL pattern for these three without a search step or a board-revision suffix WMI doesn't expose).
- **UI**: the DNA name in each tile is now clickable when a vendor URL is available (underlined, gold, hand cursor) — click opens the URL via `Process.Start(UseShellExecute: true)` in `MainWindow.xaml.cs`, wired through a new `DnaTileViewModel.OpenVendorUrlCommand`/`OpenVendorUrlRequested` event bubbled through `MainViewModel`, matching the existing event-bubbling pattern already used for Diagnose/History/Export. Click is marked handled so it doesn't also trigger the tile's expand-toggle.

### Verified
- Full solution rebuilt clean (0 errors/0 warnings).
- Both real URLs verified live on this dev machine via a throwaway probe: GPU (NVIDIA RTX 3060) correctly resolved to `nvidia.com/en-us/drivers/`; Motherboard (ASUS TUF GAMING B550-PRO) correctly resolved to a real, well-formed, board-specific BIOS support URL.
- Full GUI app launched and ran cleanly after the change.

---

## [BACKLOG pass round 2] Full backlog sweep: research-verified fixes for 8 items, 4 decisions made and documented

Worked the remainder of BACKLOG.md's severity-ranked list, same Research Analyst → Implementer/Fixer pattern as the prior round: dedicated research agents investigated every item with a genuine open technical question, findings were verified against real hardware before implementation, then closed directly. Four more items were domain/product decisions (not technical unknowns) — each decided and documented rather than left ambiguous. Reviewed the remaining low-priority items (History filter scope, arbitrary scan-pair picker, curated diff field list, multi-enclosure edge case) and confirmed each is correctly already in its intended state — no change needed.

### Added / Fixed
- **Exception coverage widened.** `TryAdd`/`TryAddRange` in `DeviceDetectionService.cs` now catch `Exception` broadly (with `Debug.WriteLine` for dev-time visibility) instead of an enumerated list (`FormatException`/`InvalidCastException`/`OverflowException`) that research confirmed was already under-covering a real latent `NullReferenceException` risk, while the `ManagementException` case it was originally meant to guard against turned out to be moot (already swallowed by `WmiAdapter` itself).
- **Real bug fixed: `MemoryModule.SizeGb`'s fabricated `0` fallback was silently feeding `RulesEngine.EvaluateMemory`'s mismatched-modules check.** Research caught that this field — filed as purely cosmetic in BACKLOG.md — actually drove a live rules comparison, elevating it to the same severity class as the earlier `RatedSpeedMts` fix. `MemoryBasic.SlotsTotal` (previously silently collapsed to `SlotsUsed` when unknown) and `MemoryModule.SizeGb`/`MemoryBasic.TotalCapacityGb` are now nullable and null when genuinely unknown; `RulesEngine.EvaluateMemory` now excludes modules with unknown size from the mismatch comparison rather than treating a fabricated 0 as a real value.
- **Display-only nullable-field cleanup**: `CpuBasic.BaseClockGhz`, `GpuBasic.VramAmountGb`, `StorageBasic.CapacityGb`, and `StoragePartition.CapacityGb` are now nullable, showing "Unknown" instead of a fabricated "0 GHz"/"0 GB" when the underlying WMI property is missing.
- **Motherboard `Chipset` now extracted from the Model string** (e.g. "TUF GAMING B550-PRO" → "B550") via exact-token matching against a small, slow-growing list of AMD/Intel chipset codes — verified live on this dev machine. Research confirmed this is honest extraction of already-present data (vendor-registered codes, low collision risk), distinct in risk profile from the already-rejected CPU spec-database idea.
- **Storage `Manufacturer` now extracted for the ~5 vendors whose Model string reliably starts with an unambiguous full word** (Samsung, KINGSTON, WDC, SanDisk, INTEL) — verified live: this machine's Kingston drive correctly resolved, its WD drive (which doesn't match the "WDC " prefix) correctly stayed "Unknown" rather than a forced guess. Deliberately excludes short alphanumeric vendor codes (Seagate "ST", Crucial "CT") per research citing smartmontools' own vendor-ID database as prior art that this sub-case needs a much larger maintained table to do safely.
- **DNA summaries now explain what's actually wrong, not just that something is.** Added a nullable `ShortReason` to `StatusReason`, populated alongside every Yellow/Red rule's existing `Message`. `SummarySuffix` now surfaces the specific worst-severity fired reason's short form (e.g. "CPU is running hot (92.3 °C)." instead of "worth a closer look.") — research confirmed multiple Yellow/Red reasons can genuinely fire simultaneously per DNA, so the tie-break (worst severity, then source-evaluation order) matches `StatusFromReasons`' own logic.
- **Driver applicability policy now documented** at `DriverInfo.NotApplicable`'s definition: only GPU and Network have a genuinely separate, user-facing, independently-updatable driver concept; the other five DNAs' drivers are bundled with Windows Update/BIOS updates rather than tracked standalone.
- **Storage SMART "threshold" reframed as what it actually is.** Removed the misleadingly-named `HealthThresholds.StorageSmartHealthRedPct` constant (implied a tunable percentage; the underlying signal is binary, 0 or 100, never in between) — `RulesEngine.EvaluateStorage` now checks `SmartHealthPct.Value == 0` directly with an explanatory comment.
- **"Unknown" field values now explain themselves.** Any `FieldRow` (Basic/Advanced tile display) whose value is exactly "Unknown" now shows a tooltip: "This couldn't be reliably determined from available data." — distinguishes honest absence from a broken-looking app.

### Decided, not implemented
- **"Could not detect" DNA placeholder**: decided against building this. A placeholder would need to reliably distinguish "genuinely absent hardware" from "a transient WMI hiccup," which this app has no automated test coverage to verify, and the case has never actually been observed across all testing in this project. Silent omission remains the safer default until observed as a real problem.
- **Win32_SystemEnclosure multi-row handling**: reviewed again, deliberately left as-is — low practical impact for this app's desktop/laptop target audience, not worth the added complexity for an edge case this unlikely.
- **History filter scope, arbitrary scan-pair picker, curated diff field list**: all reviewed again and confirmed already in their intended, correct state — no change made.

### Verified
- Full solution rebuilt clean (0 errors/0 warnings) after each individual fix, not just at the end.
- Every new/changed field verified against real hardware via throwaway probes before being considered done: Motherboard Chipset correctly resolved "B550", Storage Manufacturer correctly resolved "KINGSTON" and correctly left the WD drive "Unknown" rather than forcing a match, full end-to-end detection (all 7 DNA types) confirmed clean with no exceptions after all changes combined.
- Full GUI app launched and ran cleanly after the complete round of changes.

---

## [BACKLOG pass] Research-verified fixes: Storage Type, OS reboot-pending rule

Worked the top of the severity-ranked BACKLOG list using a Research Analyst → Implementer/Fixer pattern: dedicated research agents investigated the genuinely open technical questions (rather than guessing at solutions), findings were verified empirically on this dev machine before implementation, then closed directly. Two items got real, shipped fixes; two more were confirmed genuinely infeasible (not just assumed) and closed as won't-implement with documented reasoning.

### Added
- **Storage `Type` now uses a real hardware-reported signal instead of "Unknown" for the common case.** `DetectStorage` now queries `MSFT_PhysicalDisk` (namespace `root\Microsoft\Windows\Storage`, Windows 8+ client SKUs) for its explicit `MediaType` (HDD/SSD/SCM) and `BusType` (including NVMe) enums, correlated to `Win32_DiskDrive` via `DeviceId`<->`Index` matching. This is a genuine hardware-reported field, not an inferred guess, so it doesn't reintroduce the fabrication the prior hardening-pass fix removed. Falls back to the prior confident-substring/"Unknown" logic if the namespace is unavailable or returns `Unspecified`. Verified empirically (non-elevated) on this dev machine: both real NVMe SSDs correctly returned `MediaType=4`/`BusType=17` instead of the previous "Unknown".
- **New `OsAdvanced.RebootPending` field and a real Yellow rule for it.** After research confirmed the only API for "updates available" (`IUpdateSearcher`, Windows Update Agent COM) requires a live network round-trip and was correctly ruled out (conflicts with the no-cloud-call principle), a narrower, genuinely local-only signal was identified and implemented: whether Windows has an installed update waiting on a reboot to finish applying, read from two well-documented `HKEY_LOCAL_MACHINE` registry keys (`WindowsUpdate\Auto Update\RebootRequired` and Component Based Servicing's `RebootPending`), no elevation required. Deliberately excludes `PendingFileRenameOperations` (a documented false-positive source also written by AV/cleanup tools), matching the exclusion used by established tools like the `PendingReboot` PowerShell module. `RulesEngine.EvaluateOs` surfaces this as Yellow, worded precisely as "a restart is pending" rather than implying general update currency.
- **New `IRegistryAdapter`/`RegistryAdapter` in `DeviceDNA.PlatformAdapters`** (read-only local registry access, `Microsoft.Win32.Registry` NuGet package) — the OS reboot-pending signal is registry-based, which per CLAUDE.md's layering rule must stay confined to the Platform Adapter layer, so this follows the exact same interface/concrete-implementation pattern already used for `IWmiAdapter`/`WmiAdapter`.

### Closed — will not implement (see BACKLOG.md history for full reasoning)
- **CPU/GPU boost-clock rule**: confirmed via research that neither LibreHardwareMonitorLib nor WMI expose a rated-boost-clock value (a structural gap, not an oversight — LHM has no CPU spec database, WMI predates Turbo Boost as a concept). A bundled spec database is technically possible but was rejected as an ongoing maintenance liability that risks producing *wrong* verdicts as new CPUs launch — worse than the current honest silence.
- **Motherboard full PCIe-slot-topology rule**: confirmed via research that no Windows API exposes per-slot negotiated PCIe link width. A smaller potential win (populating the already-existing `GpuAdvanced.PcieGeneration`/`PcieLaneWidth` fields via LibreHardwareMonitor's GPU-vendor-API-backed sensors) was identified and empirically checked on this machine's Nvidia GPU — LHM exposes PCIe throughput but not generation/width for this hardware/driver combination, so this specific win doesn't materialize here.

### Verified
- Full solution rebuilt clean (0 errors/0 warnings) after each change.
- Both new signals verified against real hardware via a throwaway probe before being considered done: Storage Type correctly resolved both real drives to "NVMe" (previously "Unknown"); OS RebootPending correctly read `false` with no exceptions, non-elevated.
- Full app launched and ran cleanly after all changes.

---

## [Hardening Pass — Project Reviewer round 2] Gauntlet closed

Ran the gauntlet's closing step per the workflow cheatsheet: "close the loop. If this round comes back clean, you're done." Scope was narrower than round 1 — not a fresh full sweep, but a targeted check of whether the fixes from the prior three passes (Project Reviewer round 1, QA/Playtester, Devil's Advocate) actually landed correctly with no fix-on-fix regressions, whether BACKLOG.md still accurately describes the codebase, and whether any remaining open item should actually block calling v1 release-ready.

### Result: clean, no new findings
- All fixes across the prior three passes verified as correctly landed by reading the actual current code (not just re-trusting CHANGELOG's own description) — the RatedSpeedMts and ScanChangeDetector fixes from the Devil's Advocate pass, and every file touched across all four hardening entries, match what's documented with no leftover dead code or partial fixes.
- Full solution build: 0 errors/0 warnings.
- Real-hardware end-to-end run (detection → rules engine → SQLite persistence → JSON export) completed with no exceptions.
- BACKLOG.md spot-checked against current code (3 Devil's-Advocate-era items) — accurate, no drift, nothing silently fixed without being logged.
- Gut-check verdict: no remaining open item rises to "must fix before anyone should trust this app's output." The deferred no-fabrication gaps (CPU boost-clock, GPU driver-staleness, BIOS-update-available, OS updates-pending rules) are honest absences by design, not wrong answers. Remaining display-only `?? 0` fallbacks are self-evidently wrong to a user rather than silently plausible, unlike the RatedSpeedMts case that was fixed specifically because it fed a live diagnosis comparison. Everything else in BACKLOG.md is UX polish or a distribution-mechanics gap (installer packaging), not a correctness or trust concern.

### Verdict
**The hardening gauntlet (Project Reviewer → Security Auditor → QA/Playtester → Devil's Advocate → Project Reviewer round 2) is genuinely closed.** DeviceDNA v1 is release-ready in the sense the gauntlet exists to verify: builds clean, runs correctly against real hardware, doesn't fabricate data it can't honestly source, and has no known crash/correctness bug left unaddressed or undocumented. Remaining BACKLOG.md items are real but are deliberate, reasoned deferrals — not gaps that slipped through unnoticed.

---

## [Hardening Pass — Devil's Advocate fixes] Final gauntlet step

Ran the final hardening-gauntlet step: Devil's Advocate, specifically attacking the fixes made in the two prior hardening passes rather than reviewing the app fresh (per the gauntlet's own guidance that this step's sharpest findings come from attacking the session's own prior work). Attacked 8 specific fixes; 2 held up cleanly (chassis-type mapping, disk-partition exact-match), 1 held up with a minor documented caveat (OS activation), and 5 had real problems — the most consequential fixed below, the rest deferred to BACKLOG.md with reasoning for why they're lower-severity or need a larger deliberate change than a quick patch.

### Fixed
- **Bug (feeds live diagnosis, not just cosmetic): `MemoryAdvanced.RatedSpeedMts` was fabricated as `0` when WMI's `Speed` property was missing**, and that fabricated `0` fed directly into `RulesEngine.EvaluateMemory`'s `ActualSpeedMts < RatedSpeedMts` comparison — meaning a genuinely-missing rated speed would make any real actual-speed reading look like it's running *above* spec, silently suppressing a real "XMP/EXPO not enabled" yellow finding instead of correctly skipping the rule for lack of data. This was the same `?? 0` fabrication class already fixed elsewhere in the hardening pass, just missed because that pass was scoped to two specific line numbers rather than a full sweep of the file. Now keeps the raw nullable value for the RulesEngine-facing field while `Basic.SpeedMts` (a required display field with no way to represent "unknown") keeps its display fallback separately.
- **Bug: `ScanChangeDetector`'s duplicate-key fix (from the first hardening pass) replaced a crash with a silent-correctness risk.** Positional matching (`previous[i]<->current[i]`) within a same-named group has no real identity behind it — if two identically-modeled disks exist and WMI/LibreHardwareMonitor's enumeration order isn't stable between separate scans (not contractually guaranteed), the diff engine could compare the wrong "before" against the wrong "after" and report a confidently wrong "changed" entry, or misattribute which of two same-named devices was actually removed. Field-level comparison (health status, driver version, capacity/speed changes) now only runs for the unambiguous 1:1 case — exactly one DNA with a given (Type, Name) on both sides of the comparison. When either side has 2+ same-named instances, field comparison is skipped for that group entirely; added/removed detection (which only compares counts, never assumes which specific instance changed) is unaffected and remains correct in all cases.

### Deferred (see BACKLOG.md for full detail and reasoning — 5 new items from this pass)
- Per-DNA exception isolation doesn't catch every exception type the WMI/Convert.To* call chain could realistically throw (narrowed the crash surface significantly, but coverage isn't 100%).
- `SummarySuffix`'s Yellow/Red wording is vague relative to full explainability (doesn't say *what* to look at, just that something's worth checking).
- `Win32_SystemEnclosure` multi-row case picks an arbitrary row (low practical impact for this app's desktop/laptop target audience).
- Several other required-field `?? 0` fallbacks remain for *display-only* fields (BaseClockGhz, VramAmountGb, module/disk capacity) — lower severity than the fixed RatedSpeedMts case since none feed a live rules comparison, and a "0 GB VRAM" reads as obviously wrong to a user rather than silently plausible.
- Storage Type "Unknown" fix, while more honest, reduced usefulness for the common case (most real SSDs report WMI's ambiguous "Fixed hard disk" MediaType) — a more reliable non-fabricated signal (`Win32_PhysicalMedia.SpindleSpeed`, NVMe interface detection) wasn't explored before landing on "Unknown" as the fallback.

### Verified
- Full solution rebuilt clean (0 errors/0 warnings) after each fix.
- Devil's Advocate independently re-verified (not just re-read the CHANGELOG's claims) the disk-partition exact-match fix's WMI Antecedent string format by construction, and independently re-verified the `Win32_SystemEnclosure.ChassisTypes` code mapping against the real SMBIOS Type 3 enum — both confirmed genuinely correct, not just asserted correct.
- App build+run smoke-tested clean after this round of fixes; no fix-on-fix regression found.

---

## [Hardening Pass — QA fixes] Security Auditor + QA/Playtester gauntlet steps

Ran the next two steps of the hardening gauntlet: Security Auditor (independent re-verification of SQL parameterization, WQL query construction, export path safety, and network-absence; came back genuinely clean — no critical/high findings) and QA/Playtester (roleplayed first-time/impatient/returning-daily personas through the real app on real hardware, driving the actual ViewModels/services and cross-checking against real XAML bindings since this environment can't click a live GUI).

### Fixed
- **Gap: GPU/Storage/Motherboard summary text still hardcoded "detected successfully." regardless of computed status** — the exact bug class the earlier hardening pass fixed for CPU/Memory/OS, but never extended to these three. Invisible on an all-green test machine, but a certain repro on any yellow/red GPU/Storage/Motherboard (a red status dot next to text literally saying "detected successfully"). Now uses the same `SummarySuffix(status)` helper as the other four DNA types.
- **Bug: GPU and Network Advanced-tier views showed Driver Version/Date twice** — once from the DNA-specific field block, once from the generic Driver Info footer appended to every DNA's Advanced view. Removed the duplicate DNA-specific rows; the footer is now the single source for driver info display.

### Deferred (see BACKLOG.md for full detail and reasoning)
- "Unknown" field values (Storage Type, etc.) have no in-UI explanation for why — minor UX polish.
- Storage Manufacturer / Motherboard Chipset never derived from their Model strings — missed enhancement, not fabrication; would need a curated lookup table to do safely.
- History's DNA-type filter has a narrower practical payoff than a user might expect from the label (filters the scan list, doesn't surface per-DNA detail) — matches the feature as built, may already be adequately covered by the Changes view.
- Security Auditor's two low-stakes notes (LocalAppData backup-tool caveat, exception messages could show a Windows username) — judged not worth acting on for a local single-user tool.

### Verified
- Full solution rebuilt clean (0 errors/0 warnings) after each fix.
- QA pass drove two real rescans, real SQLite history, real JSON export, and all 6 REQUIREMENTS.md section 7 features (Discover/Understand/Diagnose/Changes/History/Export) end-to-end on real hardware — no blocking bugs found; Diagnose/Changes/Export/core navigation confirmed genuinely smooth.

---

## [Hardening Pass] — Post-Phase-7 gauntlet fixes

Ran the Dev Support Folder's hardening gauntlet Project Reviewer step (three parallel sub-reviews: architecture/security, Model/PlatformAdapters/DetectionEngine quality, Application/UI quality + feature completeness) across the full codebase built across Phases 1-7. Architecture/security came back clean (no fabricated data infrastructure, no telemetry/network calls, no accounts/credentials, layering holds). Code-quality reviews found real, genuine bugs — fixed directly below rather than re-delegated, after an earlier phase's experience with parallel-agent file collisions.

### Fixed
- **Crash: `ScanChangeDetector.Compare` threw on duplicate `(Type, Name)` keys.** A machine with two identically-modeled disks or NICs (same `Dna.Type` + same `Dna.Name`) crashed the whole app when opening History, because `Dictionary`-keying assumed Name was unique within a type. Now groups by key and matches positionally within same-key groups instead of throwing.
- **Crash risk: `DnaSnapshotData.ToDna`'s `Id = "{type}:{name}"` had the same non-uniqueness problem.** Changed to a random Guid — nothing currently keys off `Dna.Id`, but a non-unique Id was a latent bug waiting for future code to assume uniqueness.
- **Crash: Export had no error handling around `File.WriteAllText`.** A full disk, locked file, or permission-denied path took down the whole app mid-export. Now caught and shown as a message box; the user's in-progress session is preserved.
- **No global unhandled-exception handler.** Added `App.DispatcherUnhandledException` as a last-resort safety net — an unexpected exception now shows an error dialog instead of silently terminating the process.
- **History/Changes had no defensive handling for malformed SQLite data.** `ScanHistoryRepository.ListScans`/`LoadScan` now skip individual rows that fail to parse (bad enum value, malformed JSON, corrupt timestamp) rather than letting one bad row crash the entire History/Changes view — a forward-compatibility concern for a locally-persisted app whose schema may evolve.
- **Bug: Diagnose page's Suggestion text was always invisible.** `DiagnoseWindow.xaml` bound `Suggestion` (a `string?`) through `BoolToVisibilityConverter`, which only matches actual `bool` values — a string is never a bool, so the pattern silently always evaluated false/Collapsed. Added a proper `StringToVisibilityConverter` (in the renamed `VisibilityConverters.cs`, previously misleadingly named `BoolToVisibilityInverseConverter.cs` despite containing no inverse converter) and rebound to it.
- **Fabrication: `Device.FormFactor` was hardcoded to `"Desktop"` for every machine, including laptops.** Now queries `Win32_SystemEnclosure.ChassisTypes` (a real, documented WMI enum) and maps to Desktop/Laptop/Server/Handheld/Unknown honestly; falls back to "Unknown" rather than a specific wrong guess when the chassis type is absent/unrecognized.
- **Fabrication: Storage `Type` guessed `"SSD"` for WMI's ambiguous `"Fixed hard disk"` MediaType**, which covers both HDDs and SSDs with no further WMI-level distinction available. Now honestly reports `"Unknown"` instead of a confident-sounding guess (SSD-string and NVMe-in-model-name detection, which are genuinely reliable signals, are unchanged).
- **Bug: CPU/Memory/OS `Summary` text always said "running normally" regardless of the computed `Status`**, so a Red/Yellow DNA could show a summary directly contradicting the status light next to it. Now uses a shared `SummarySuffix(HealthStatus)` helper so the summary text agrees with the actual computed status, matching the pattern Network's summary already used correctly.
- **Gap: OS `ActivationStatus` was fetched but never evaluated by the rules engine** — an unlicensed Windows install would have shown "no issues found." `RulesEngine.EvaluateOs` now surfaces `"Unlicensed"` as a real red finding; legitimate states (Licensed, grace periods) correctly stay confirmed-good rather than becoming a guessed verdict, preserving the original reasoning for not treating grace periods as problems.
- **Fabrication: `Value ?? 0` coerced missing CPU sensor readings (per-core load, live clock) to a literal `0`** instead of excluding them — a per-core load or live clock of exactly 0 is a plausible real value, so silently substituting 0 for "no reading" fabricated specific numbers. Now filters out readings with no `Value` before building `PerCoreLoadPct`/`CurrentLiveClockGhz`, consistent with how the app treats missing data everywhere else.
- **Robustness: unguarded `Convert.To*` calls in `DeviceDetectionService` could crash the entire scan on a single malformed WMI value from any one DNA type.** The Platform Adapters already degrade gracefully on their own failures; `DetectDevice()`'s per-DNA dispatch (`TryAdd`/new `TryAddRange`) now catches `FormatException`/`InvalidCastException`/`OverflowException` per DNA, so one bad value costs only that DNA, not the other six.
- **Bug: disk-to-partition WMI association matching used a bare substring `Contains(deviceId)`**, which on a system with 10+ physical disks could cross-match (e.g. `"PHYSICALDRIVE1"` matching `"PHYSICALDRIVE10"`'s Antecedent row), silently attributing one disk's partitions to another. Now matches against the exact quoted `DeviceID="..."` segment instead of a bare substring.
- Removed leftover `Debug.WriteLine` scaffolding in `MainViewModel.RunScan` (explicitly marked "safe to remove" since Phase 1).

### Deferred (see BACKLOG.md for full detail and reasoning)
- Driver-applicability policy inconsistency across DNA types (undocumented, not necessarily wrong).
- No DNA type surfaces an explicit "could not detect" finding when its WMI source is entirely absent.
- Storage SMART "threshold" is really a binary gate dressed as a percentage (correct behavior, misleading framing).
- Memory `SlotsTotal` silently collapses to `SlotsUsed` when the true total isn't sourceable (would need a Model-level nullable change).
- Storage manufacturer never derived from the Model string (missed enhancement, not fabrication).
- Storage-to-sensor temperature correlation uses fragile substring matching that could misattribute readings between identically-modeled drives (no equally clean fix available, unlike the disk-partition bug above).

### Verified
- Full solution rebuilt clean (0 errors/0 warnings) after every individual fix, not just at the end.
- Ran the app after all fixes on this real machine — launches, scans, and displays correctly with no regressions.

---

## [Phase 7] — Commercial Release (scoped subset — see BACKLOG.md for deferred items)

### Added
- Verified `dotnet publish` produces a working self-contained single-file Windows build: `dotnet publish src/DeviceDNA.UI/DeviceDNA.UI.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true`. Output is a single ~157MB `DeviceDNA.UI.exe` (bundles the .NET 8 runtime, so it runs on a machine with no .NET installed) plus a handful of native WPF interop DLLs (`wpfgfx_cor3.dll`, `PresentationNative_cor3.dll`, etc.) and `e_sqlite3.dll` that PublishSingleFile cannot bundle into the single exe (these must ship alongside it). Confirmed the published exe launches and runs standalone on this machine, independent of the dev-time build output. Logo confirmed embedded correctly as a WPF pack resource (compiled into the exe, not a loose file — verified via binary grep for the resource name).
- This is `dotnet publish` output, not a finished installer (.msi/.exe installer per REQUIREMENTS.md section 11) — an actual installer (e.g. via WiX, Inno Setup, or `dotnet-msi`) that bundles the exe + its sidecar native DLLs into one setup file, adds a Start Menu entry, and handles uninstall is deferred; see BACKLOG.md.

### Deferred (see BACKLOG.md Open Items for full list and reasoning)
- Actual installer packaging (WiX/Inno Setup or similar) — publish output verified working, installer wrapper not built.
- Website, documentation site, pricing model, marketing assets — these require real human business decisions (pricing strategy, brand copy, hosting choices) that are out of scope for unattended autonomous work and are not fabricated here.
- Deep tier fields, Display/Audio/USB DNA types, portable no-install mode — explicitly v1.1+/future scope per ROADMAP.md, not part of v1.

### Fixed
- N/A.

### Changed
- N/A.

---

## [Phase 6] — Cross-Platform (scoping only, no code)

### Added
- `docs/CROSS_PLATFORM.md` — design-level scoping doc confirming the Phase 1 architectural bet (Platform Adapters as the only OS-specific layer, per CLAUDE.md's strict layering) holds up: `DeviceDNA.Model`, `DeviceDetectionService`'s `RulesEngine`, `DeviceDNA.Application`, and all of `DeviceDNA.UI` contain zero OS-specific code today. Documents concrete Linux (`/proc`, `/sys`, `lm-sensors`, `smartctl`, `dmidecode`) and macOS (`system_profiler`, `sysctl`, SMC access as an open research question) data sources a future Platform Adapter pair would use, and flags WPF's Windows-only nature as a separate, deliberately out-of-scope UI-portability question (Avalonia or a local-API-backed web UI are the two live options, not decided here).

### Fixed
- N/A.

### Changed
- N/A.

---

## [Phase 5] — Export & Automation

### Added
- `DeviceExportService` (DeviceDNA.Application) — serializes a full `Device` snapshot (all DNAs, Basic/Advanced/StatusReasons/Driver, enums as strings) to indented JSON via `System.Text.Json`, using the existing polymorphic `Dna.Basic`/`Advanced` `object` typing (System.Text.Json serializes by runtime type automatically, no custom converters needed).
- "Export" button in `MainWindow.xaml`'s top bar, wired through `MainViewModel.ExportCommand`/`ExportRequested` (same event-based pattern as `OpenDiagnoseRequested`/`OpenHistoryRequested`) to a `SaveFileDialog` in `MainWindow.xaml.cs`. Export is always an explicit user action (REQUIREMENTS.md section 10) — no automatic/background export, and the user chooses the destination file.

### Fixed
- N/A.

### Changed
- N/A.

### Verified
- Ran a standalone probe against `DeviceDetectionService.CreateDefault().DetectDevice()` + `DeviceExportService.ToJson()` on this real machine — confirmed correct, complete JSON output including nested Advanced-tier objects (e.g. per-core CPU load array, cache), all 7 DNA types, and correct enum-as-string serialization. Note: the probe (a standalone console app, not the WPF `App`) doesn't set invariant culture the way `App.xaml.cs` does at startup, so its raw console output showed locale-formatted numbers (e.g. "13,4%") — this is specific to the probe harness, not a product bug; the real app already sets `CultureInfo.InvariantCulture` globally at startup (see Phase 1 CHANGELOG entry) and JSON export is unaffected regardless since `System.Text.Json` number formatting isn't culture-dependent.

---

## [Phase 4] — History & Change Detection

### Added
- `ScanHistoryRepository` (DeviceDNA.Application) — local SQLite persistence (Microsoft.Data.Sqlite, no ORM) at `%LOCALAPPDATA%\DeviceDNA\devicedna.db`, created on first run if missing. Two tables: `scans` (id, timestamp_utc, hostname, os_summary, form_factor, overall_status) and `dna_snapshots` (scan_id FK with `ON DELETE CASCADE`, dna_type, name, manufacturer, summary, status, plus a `data_json` blob holding the full Basic/Advanced/StatusReasons/Driver payload for that DNA). Exposes `SaveScan(Device)`, `ListScans(DnaType? filter = null)` (newest-first, optional DNA-type filter per REQUIREMENTS.md section 7 item 5), and `LoadScan(long scanId)` (reconstructs a full `Device`).
- `DnaSnapshotData` (DeviceDNA.Application) — the JSON-serializable payload stored in `data_json`. Basic/Advanced are captured as `JsonElement` and re-deserialized into the correct concrete per-DNA-type class (`CpuBasic`, `GpuAdvanced`, etc.) based on `DnaType` when a snapshot is loaded back, since `Dna.Basic`/`Dna.Advanced` are untyped `object` in the model.
- `DeviceScanService.ScanDevice()` now saves every real scan to `ScanHistoryRepository` automatically (constructs one by default, or accepts an injected instance) — every scan the app performs, startup or manual rescan, is now persisted, per REQUIREMENTS.md section 7 item 5 ("central log of past scans"). The service also exposes `HistoryRepository` so the UI layer can open History/Changes against the same underlying SQLite file rather than constructing a second repository instance.
- `ScanChangeDetector` (DeviceDNA.Application) — compares two `Device` snapshots and produces a human-readable "what changed" timeline (REQUIREMENTS.md section 7 item 4). Matches DNAs by `(Type, Name)`; flags DNAs present in only one scan as added/removed; compares `Status` and `Driver.Version` for every matched DNA; additionally diffs a curated set of Basic-tier fields a user would actually notice — Storage free-space %, Memory speed, GPU driver version, Network link speed, Motherboard BIOS version. Placed in `DeviceDNA.Application` rather than `DeviceDNA.DetectionEngine` (documented in a code comment) because it operates purely on already-detected Model data pulled from history, not on live hardware — business logic over stored results, not a new detection concern. Deliberately excludes constantly-changing/noisy fields (OS uptime, live CPU/GPU temp and utilization, per-core load, current VRAM usage) from the diff — these fluctuate on every scan by definition and would drown out genuinely interesting changes; documented in a code comment and confirmed by real-machine testing below.
- History & Changes UI (REQUIREMENTS.md section 7 items 4-5): `HistoryWindow.xaml`/`.xaml.cs` (DeviceDNA.UI), opened from a new "History" button in `MainWindow.xaml`'s top bar (same pattern as the Phase 3 "Diagnose" button). Two-column layout: left column is `HistoryViewModel` (scan list, newest-first, with a DNA-type filter `ComboBox` and a manual Refresh button); right column is `ChangesViewModel` (diffs the two most recent scans by default, shown as a change-entry timeline, or a "not enough history yet" / "no significant changes detected" message when appropriate). Combined into one window since both read from the same repository and are naturally viewed together, while staying separate ViewModels/sections per the task brief's "related but distinct features" framing. Reuses the existing dark charcoal + white/gold theme brushes — no new palette introduced.
- `MainViewModel` gained a `RescanCommand` (new "Rescan" button in `MainWindow.xaml`'s top bar) so a second scan can be generated on demand without restarting the app — needed to produce meaningful change-detection input during testing and going forward. Also gained `OpenHistoryCommand`/`OpenHistoryRequested` (mirroring the existing `OpenDiagnoseCommand`/`OpenDiagnoseRequested` pattern) and a `HistoryRepository` passthrough property so `MainWindow.xaml.cs` can construct `HistoryWindow` against the same SQLite-backed repository the scan service already uses.

### Fixed
- N/A.

### Changed
- N/A.

### Verified (real-machine testing, BASEMENT PC: Ryzen 7 5800X / RTX 3060 / ASUS B550-PRO / 16GB DDR4)
- Ran the app twice back-to-back via `dotnet run`. Confirmed via a throwaway in-process probe (same approach as Phase 3) against the real `%LOCALAPPDATA%\DeviceDNA\devicedna.db`: both scans persisted with distinct timestamps (`2026-08-14T22:10:20Z` and `2026-08-14T22:10:35Z`), `ListScans()` returned both newest-first, `ListScans(DnaType.Storage)` correctly filtered to the 2 scans containing a Storage DNA, and `ScanChangeDetector.Compare()` between the two scans returned **0 changes** — the correct/expected result for two scans taken 15 seconds apart on unchanged hardware, and confirms the noisy-field exclusions (uptime, live temps, utilization) are working as intended rather than producing false-positive noise on every scan.
- Confirmed via code review that `HistoryWindow.xaml`'s bindings (`History.FilterOptions`, `History.SelectedFilter`, `History.RefreshCommand`, `History.SummaryText`, `History.Scans`, `Changes.ComparisonLabel`, `Changes.SummaryText`, `Changes.Changes`) match the properties actually exposed by `HistoryViewModel`/`ChangesViewModel`, and that `MainWindow.xaml`'s new `RescanCommand`/`OpenHistoryCommand` bindings match `MainViewModel`.

---

## [Phase 3] — Diagnosis

### Added
- `DeviceDNA.DetectionEngine.Rules.RulesEngine` (new `Rules/` folder in DeviceDNA.DetectionEngine, sitting at the Detection Engine layer per CLAUDE.md's strict layering) — deterministic, non-AI evaluation of every DNA type per the `RULE { applies_to, condition, severity, message, suggestion, confidence }` format in REQUIREMENTS.md section 5. Implements: CPU temp (yellow/red) and utilization (yellow), virtualization-disabled (yellow); GPU temp (yellow/red, distinct thresholds from CPU) and utilization (yellow); Memory actual-speed-below-rated (yellow, using the exact REQUIREMENTS.md section 5 example message) and mismatched-module (yellow); Storage low-free-space (yellow) and SMART-failure-predicted (red); Motherboard PCIe-slot-under-negotiated-width (yellow, only when slot data is populated); Network connected-below-max-supported-speed (yellow, in addition to the existing disconnected/red check). Every DNA always emits at least one StatusReason, including an Info-severity "confirmed good" reason when no warning/error rules fire (hard product requirement, CLAUDE.md Rules Engine Implementation Notes). `RulesEngine.StatusFromReasons()` derives each DNA's overall `Status` as the worst severity among its fired reasons (Red > Yellow > Green).
- `HealthThresholds` static class centralizes all tunable numeric thresholds (CPU/GPU temp yellow+red, CPU/GPU utilization yellow, storage free-space yellow, storage SMART-health red) in one place per CLAUDE.md's requirement that thresholds be tunable constants, not scattered magic numbers.
- `DeviceDetectionService` now calls `RulesEngine` for every DNA instead of the Phase 1/2 placeholder `OkReason()` helper (removed); the Network DNA's existing disconnected/no-IP red check is preserved as-is and combined with the new connected-speed rule.
- Bug found and fixed during real-machine testing: CPU temperature rules initially treated a literal `0 °C` reading as a valid "normal" temperature. On this dev machine, LibreHardwareMonitor without administrator elevation returns `0` (not null) for the CPU package temp sensor — a known Phase 1 limitation. `RulesEngine` now has a `HasPlausibleTemp()` guard that treats `0 °C` as "no reading" for CPU/GPU temperature rules specifically, so the temp rule is skipped rather than firing a false "normal" confirmation. GPU temp (a genuinely-read 51 °C on this machine) was unaffected and continues to evaluate normally.
- Dedicated Diagnose page (REQUIREMENTS.md section 7, item 3): `DiagnoseWindow.xaml`/`.xaml.cs` (DeviceDNA.UI) opened as a second `Window` from a new "Diagnose" button in `MainWindow.xaml`'s top bar (per REQUIREMENTS.md section 8 — secondary pages reachable from the top bar, no sidebar). `DiagnoseViewModel` (DeviceDNA.UI.Presentation) flattens every DNA's `StatusReasons` into `DiagnoseFindingViewModel` entries, grouped by severity (Needs Attention / Worth Checking / Confirmed Good, in that order) so the most actionable findings surface first. Confirmed-good findings are visually distinct (green circle + checkmark glyph vs. yellow/red circle + warning/X glyph) per REQUIREMENTS.md section 3's "actively checking, not just quiet" principle. Reuses the existing dark charcoal + white/gold theme brushes from `App.xaml` — no new palette introduced.
- `MainViewModel` now retains the last-scanned `Device` and exposes an `OpenDiagnoseCommand` + `OpenDiagnoseRequested` event; `MainWindow.xaml.cs` handles the event to construct and show `DiagnoseWindow`, keeping window-management out of the ViewModel.

### Fixed
- See the CPU 0 °C bug above.

### Changed
- N/A.

---

## [Phase 2] — Advanced Tier & Depth

### Added
- `DeviceDetectionService` now populates Advanced-tier fields for all 7 DNAs (previously only CPU), per the exact field lists in CLAUDE.md: CPU power mode (via active Windows power plan), GPU driver date, Memory per-module detail, Storage SMART health (pass/fail, mapped to 100%/0%, via `MSStorageDriver_FailurePredictStatus`) and per-partition free space (correlated through `Win32_DiskDriveToDiskPartition`/`Win32_LogicalDiskToPartition` association classes), Motherboard socket/memory-support/BIOS date, Network driver version (via `Win32_PnPSignedDriver` correlation).
- `IWmiAdapter.Query` gained an optional `wmiNamespace` parameter (defaults to `root\cimv2`) so queries against `root\wmi`-namespaced classes (e.g. SMART failure prediction) work through the same adapter.
- Network DNA's `Driver` field is now populated from the resolved PnP driver version (previously always `NotApplicable`) when a version is found; still `NotApplicable` when not.
- `DnaTileViewModel.BuildAdvancedFields()` extended to render Advanced-tier rows for all 7 DNAs (previously CPU only), plus a driver info row (version/date/source or "Not applicable") appended to every DNA's Advanced view.

### Fixed
- N/A.

### Changed
- N/A.

---

## [Phase 1] — Foundation

### Added
- `LibreHardwareMonitorAdapter` (DeviceDNA.PlatformAdapters) — wraps a single `LibreHardwareMonitor.Hardware.Computer` instance, enables CPU/GPU/Memory/Storage/Motherboard/Network sensor groups, and walks the hardware/sub-hardware sensor tree into `RawSensorReading` values. Degrades gracefully (returns whatever it could read) if sensor access fails without elevation.
- `WmiAdapter` (DeviceDNA.PlatformAdapters) — queries WMI classes (`Win32_Processor`, `Win32_VideoController`, `Win32_PhysicalMemory`, `Win32_PhysicalMemoryArray`, `Win32_DiskDrive`, `Win32_LogicalDisk`, `Win32_BaseBoard`, `Win32_BIOS`, `Win32_NetworkAdapter`, `Win32_NetworkAdapterConfiguration`, `Win32_OperatingSystem`) via `ManagementObjectSearcher`, returning raw property bags as `RawWmiInventory`.
- `DeviceDetectionService` (DeviceDNA.DetectionEngine) — orchestrates both adapters, maps raw WMI + sensor data onto the 7 `Dna` types (CPU, GPU, Memory, Storage, Motherboard, Network, OS) per the CLAUDE.md Basic/Advanced field lists, and assembles a `Device`. Generates a one-line plain-English `Summary` per DNA. Leaves fields null rather than fabricating data where WMI/LibreHardwareMonitor cannot reliably source them (REQUIREMENTS.md section 10). Every DNA carries at least one info-severity `StatusReason` ("Detected successfully.") per the Phase 1 placeholder rules-engine behavior; Network DNA additionally flags red/disconnected when no IP is assigned. Storage and Network produce one `Dna` per physical disk / physical adapter respectively; Bluetooth PAN adapters are filtered out as noise.
- `DeviceScanService` (DeviceDNA.Application) — thin orchestration service the UI calls to get a `Device` snapshot; wires up `DeviceDetectionService.CreateDefault()`. No persistence yet (SQLite history is Phase 4).
- WPF command-deck UI (DeviceDNA.UI): dark charcoal theme with white/gold chrome accents, top bar with logo/wordmark/overall health indicator, main canvas grid of DNA tiles (neutral dark tile backgrounds, status light per tile). Clicking a tile expands it in place to show Basic-tier fields for all 7 DNA types; an "Advanced" toggle reveals Advanced-tier fields for CPU (proving the pattern per Phase 1 scope). `MainViewModel` runs a real hardware scan via `DeviceScanService` on startup and populates the tile grid — no mock data.
- Lightweight MVVM plumbing (`ViewModelBase`, `RelayCommand`) and value converters (`HealthStatusToBrushConverter`, `BoolToVisibilityConverter`) under `DeviceDNA.UI/Presentation`.
- `assets/Logo.png` copied into `DeviceDNA.UI/Assets/Logo.png` with `CopyToOutputDirectory` so the top bar can load it at runtime.

### Fixed
- Number formatting in DNA summaries/UI now uses invariant culture (`App` sets invariant culture at startup) so values like storage capacity render as "1907.7 GB" rather than locale-dependent "1907,7 GB".

### Changed
- N/A (first implementation pass).

---

<!-- Format for future entries:

## [Phase 1] — Foundation
### Added
- ...
### Fixed
- ...
### Changed
- ...

-->
