# ROADMAP.md — DeviceDNA

Phases, planned or completed — the high-level shape of what's been built and what's still ahead.
Each phase links to `CHANGELOG.md` for the detailed history once complete. This file should only
ever contain phases/features, not bugs or in-progress findings — those belong in `BACKLOG.md` until
resolved, then move to `CHANGELOG.md` directly.

## Phase 0 — Product Definition ✅ Complete
Product concept, DNA data model, rules engine format, UX direction, visual identity, tech stack — all defined in planning conversation. See `REQUIREMENTS.md` and `CLAUDE.md`.

## Phase 1 — Foundation ✅ Complete
See `CHANGELOG.md`.

- Project scaffold (WPF or WinUI 3 — decide and document choice)
- Platform Adapter layer: WMI adapter (static inventory) + LibreHardwareMonitorLib adapter (sensors)
- DeviceDNA normalized data model implemented in code
- Basic tier UI for all 7 v1 DNAs (CPU, GPU, Memory, Storage, Motherboard, Network, OS)
- Command-deck dashboard layout (top bar, DNA tile grid, no sidebar)
- One-line summary generation per DNA

## Phase 2 — Advanced Tier & Depth ✅ Complete
See `CHANGELOG.md`.

- Advanced tier fields + UI toggle for all 7 DNAs
- Driver info display (version, date, source URL where confidently known)

## Phase 3 — Diagnosis ✅ Complete
See `CHANGELOG.md`.

- Rules engine implementation (deterministic, per `CLAUDE.md` rule format)
- Per-DNA health status calculation (green/yellow/red)
- Dedicated Diagnose page aggregating all findings across DNAs
- Confirmed-good status messages, not just warnings

## Phase 4 — History & Change Detection ✅ Complete
See `CHANGELOG.md`.

- SQLite schema for scan snapshots
- Scan history log, filterable by DNA
- Change detection: compare current scan to previous, generate "what changed" timeline

## Phase 5 — Export & Automation ✅ Complete (JSON export only — CLI/API remain future)
See `CHANGELOG.md`.

- JSON export of full Device model
- (Future, not v1) CLI
- (Future, not v1) Local API

## Phase 6 — Cross-Platform ✅ Scoping complete, implementation not started
See `CHANGELOG.md` and `docs/CROSS_PLATFORM.md` — design-level scoping only, confirming the
architecture supports future Linux/macOS Platform Adapters without a rewrite. No Linux/macOS code
has been written; the items below remain actual future work.

- Linux support via new Platform Adapter (e.g. lm-sensors equivalent)
- macOS support via new Platform Adapter
- Mobile companion strategy (separate scoping effort — Android/iOS have very different hardware-access restrictions)

## Phase 7 — Commercial Release ✅ Complete (v1 distribution scope)
`.msi` installer shipped (WiX, Start Menu shortcut, uninstall, app icon), license decided (All
Rights Reserved) and applied, LibreHardwareMonitorLib's MPL-2.0 obligations satisfied via
`THIRD-PARTY-NOTICES.md`. See `CHANGELOG.md`'s "[.msi installer shipped]" and "[License decided]"
entries. Distribution plan: free via GitHub + Patreon, no dedicated marketing site (user's explicit
choice) — the site/pricing/marketing bullets that used to live here were never actually wanted;
see `CHANGELOG.md`'s "[Website, docs site, marketing assets not started]" resolution.

Remaining below are genuinely future, not v1 scope:
- Deep tier fields for DNAs (if validated as worth building post-v1 usage)
- Display, Audio, USB/Peripherals DNA types
- Portable "quick check" no-install mode (possible — history-less lightweight variant)

## Phase 8 — Orbital Dashboard Redesign ✅ Complete
Command-deck tile grid replaced with an orbital DNA-helix visualization — a central "This Computer"
hub with one orbit node per DNA component, connected by curved strand lines; clicking a node
expands to a full detail view (all fields at once) with an animated DNA-helix graphic. Designed
first as a clickable HTML mockup, then built in five verified stages. See `CHANGELOG.md`'s
"[Orbital DNA dashboard shipped]" and "[Orbital dashboard: theming, vendor links, live-review
fixes]" entries.

## Deferred / Under Consideration (not committed to a phase yet)
- Sensor history graphing (trend charts over time, beyond basic scan-to-scan diff)
- Home Assistant / automation integrations (enabled by future local API)
