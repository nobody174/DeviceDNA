# DeviceDNA — Requirements

**Tagline:** Know what's inside your device. Understand what it means.
**Positioning:** As deep as you want. As simple as you need.

Reference point: inspired by Speccy (clean summary, dive deeper if needed) but goes further — Speccy shows numbers, DeviceDNA explains what they mean, tracks change over time, and is fully local/private with no telemetry.

---

## 1. Core Concept

A **DNA** = one physical hardware component category (CPU, GPU, Memory, Storage, Motherboard, Network, OS — v1 scope). Defined by component identity, never by connection method (e.g. a USB storage device is Storage DNA with `connection: USB`, not USB DNA).

The **Device** is the root container (the whole machine) — it is NOT itself a DNA. It aggregates all DNA objects and holds its own identity (hostname, OS, form factor).

Every DNA follows the same universal template/shape, even when a section is not applicable (shown as "Not applicable" rather than hidden) — this keeps the UI, JSON export, and rules engine all consistent.

---

## 2. Three-Tier Information Architecture

Every DNA presents information in three tiers, revealed progressively:

- **Basic** — what 95% of users want at a glance (name, manufacturer, key spec, status light, one-line plain-English summary)
- **Advanced** — what technically-interested users want (clocks, interfaces, live readings, configuration detail)
- **Deep** — forensic/enthusiast level (raw IDs, firmware versions, register-level detail) — **deferred to v1.1+**, not in v1 build

Navigation (superseded 2026-08-16 by the orbital DNA dashboard — see section 8 below): the original
v1 command-deck tile grid had you click into a DNA to land on its Basic tier, then reveal Advanced
via an in-page toggle. The current orbital dashboard instead shows a DNA's full Basic + Advanced
field set at once with no toggle, once you click its orbit node — a component given full-screen
detail space doesn't need Advanced hidden behind a click. See section 8 for the current shape.

---

## 3. Health Status Model

No numeric health score. Three states only:

- 🟢 Green — no known issues
- 🟡 Yellow — worth checking, not urgent
- 🔴 Red — something appears wrong

Every status must be explainable — the rules engine also surfaces "confirmed good" checks (e.g. "✅ RAM running at full rated speed"), not just silence when things are fine. This reinforces that the app is actively checking, not just staying quiet.

---

## 4. Normalized Data Model

All DNAs share this shape (see also `CLAUDE.md` for full schema):

```
DNA {
  id, type, name, manufacturer
  summary              → one-line plain-English sentence
  status                → green / yellow / red
  status_reasons        → list of {message, severity, confidence} objects, includes both warnings and confirmed-good checks
  basic { ... }
  advanced { ... }
  deep { ... }          → placeholder for v1.1+
  driver { version, date, source_url } → null/"not applicable" if none exists
  last_updated
}

Device {
  id, hostname, os_summary, form_factor
  dnas: [ DNA, ... ]
  scan_history: [ { timestamp, snapshot_id }, ... ]
}
```

The data model is the single source of truth. UI, JSON export, CLI (future), and API (future) are all consumers of this model — never the other way around.

---

## 5. Diagnosis / Rules Engine

**Rules-based, deterministic — NOT AI-guessed.** This is a hard product principle, not a suggestion. AI may assist with wording/explanations in future phases, but the underlying pass/fail logic must be deterministic rules.

Rule format:

```
RULE {
  applies_to     → DNA type
  condition       → the check being evaluated
  severity        → yellow / red / (or "info" for confirmed-good)
  message         → plain-English explanation
  suggestion      → what the user can do
  confidence      → High / Medium / Low
}
```

Example:
```
RULE {
  applies_to: "Memory"
  condition: actual_speed < rated_speed
  severity: yellow
  message: "Your RAM is rated for {rated_speed} but is currently running at {actual_speed}."
  suggestion: "Check your BIOS memory profile (XMP/EXPO) settings."
  confidence: High
}
```

---

## 6. V1 Scope (MVP)

**In scope — Basic + Advanced tiers only:**
- CPU
- GPU
- Memory
- Storage
- Motherboard
- Network
- OS

**Deferred to v1.1 / v2:**
- Display DNA, Audio DNA, USB/Peripherals DNA
- Deep tier for all DNAs
- History graphing / sensor trend charts (beyond basic scan-to-scan comparison)
- CLI, local API
- Portable (no-install) distribution mode
- Linux / macOS support

Full field lists per DNA (Basic/Advanced) are documented in `CLAUDE.md`.

---

## 7. Feature Requirements (V1)

1. **Discover** — scan and identify installed hardware across the 7 in-scope DNA types
2. **Understand** — one-line plain-English summary per DNA, shown on Overview
3. **Diagnose** — rules engine evaluates each DNA, surfaces yellow/red findings + green confirmations, with a dedicated Diagnose page aggregating all findings
4. **Changes** — compare current scan to previous scan(s), show a "what changed" timeline (driver updates, new/removed devices, capacity changes, etc.)
5. **History** — central log of past scans, filterable by DNA
6. **Export** — JSON export of the full normalized Device model

---

## 8. UX / Navigation Structure

**Layout direction: "Orbital DNA dashboard"** (superseded the original v1 command-deck tile grid on
2026-08-16 — see `CHANGELOG.md`'s "[Orbital DNA dashboard shipped]" entry). No left sidebar. Top
status bar only (logo, overall device health indicator). Main canvas is a central "This Computer"
hub with one orbit node per DNA component (7 in v1), connected by curved strand lines. Clicking a
node swaps the overview for that DNA's full detail view (all Basic + Advanced fields at once, no
separate tier toggle — a component with full-screen space doesn't need one hidden behind a click),
with an animated DNA-helix graphic alongside it; a "Back to overview" control returns to the orbit.

- Node/background colors stay neutral/dark — do NOT use brand colors (white/gold) decoratively
- Status color (green/yellow/red) is reserved for the health indicator (node ring + detail-view
  status dot) only — never used decoratively
- Brand colors (white/gold) reserved for chrome: logo, top bar accents, primary wordmark
- Secondary pages (Diagnose, History/Changes, Export) reachable from the top bar / overview.
  Diagnose and History both navigate in-place within the main window (same content-swap pattern as
  the orbit-to-detail-view transition); Export is a one-shot file-save action, not a page.

---

## 9. Visual Identity

- **Logo:** two mirrored "D" letterforms (hollow/outline style) with a DNA double-helix crossing between them in rainbow gradient colors, small circuit-style connection nodes along the strand. Left D white, right D gold. Wordmark "DeviceDNA" below — "Device" in white, "DNA" in gold.
- **Theme:** dark-first. Deep charcoal background base.
- **Accent colors:** white + gold for branding/chrome. Green/yellow/red reserved strictly for health status — never used decoratively elsewhere.
- **Aesthetic target:** premium technical instrument (think modern developer tool / diagnostics dashboard), NOT gamer-RGB, NOT retro terminal-green hacker aesthetic.
- Logo source assets: see `/assets` folder (to be added) — current locked concept is the mirrored-D/rainbow-helix version approved in planning chat.

---

## 10. Privacy Principles (Brand Pillar)

- No accounts, no telemetry, no ads
- **Your data stays local**: scan happens locally, history stored locally (SQLite), database stays on your machine, never synced/uploaded anywhere. Export is user-initiated only.
- **Core scanning never requires internet**: hardware detection, current driver/BIOS version display, diagnosis, history, and change detection all work fully offline — no network dependency in the normal scan path.
- **Online lookups are allowed and expected, always opt-in per action** (clarified 2026-08-15, see BACKLOG.md/CHANGELOG.md history — original "no cloud requirement" wording was about scan data never leaving the device, not a ban on the app ever reaching the internet). What's actually shipped (as of 2026-08-16, see `CHANGELOG.md`'s "[Orbital dashboard: theming, vendor links, live-review fixes]" entry for the full per-DNA breakdown):
  - Every DNA name is clickable when a confident vendor/support URL exists — shipped for all 7 DNA types (CPU, GPU, Memory, Motherboard, Network, OS, Storage), each with its own honesty-scoped construction (a real per-model URL where one genuinely exists, e.g. NVIDIA/ASUS; a general manufacturer support page where no per-model pattern exists, e.g. Storage/Memory; a reused motherboard URL for integrated network adapters; a per-version Microsoft release-health page for Windows).
  - CPU additionally links to a real community benchmark database (CPU-Z Validator) — a distinct link type from the vendor-support link, both shown together in the detail view.
  - A **live** "check for updates" action (fetching a vendor's site to determine current-vs-installed driver/BIOS freshness) was researched in depth and deliberately **not built** — real technical paths exist for some vendors, but ToS friction (NVIDIA) and no free driver-database aggregator API made it not worth the risk for a hobby app. What shipped instead: the link-out above (so the user checks manually in one click) plus an opt-in Windows Update Agent (WUA) driver check on CPU/GPU/Network/Motherboard tiles, with an explicit in-UI caveat that WU often lags vendor sites for GPU/BIOS specifically. See `BACKLOG.md` for the two still-open "true live check" items and why they're deliberately deferred, not abandoned.
  - Never fabricate manufacturer/driver URLs or version claims — only surface a URL/verdict when confidently identifiable from a real source; if a check fails or a source can't be matched, say so plainly rather than guessing.

---

## 11. Technical Stack

- **Language/runtime:** C# / .NET
- **UI framework:** WPF or WinUI 3 (Claude Code to recommend final choice) — native Windows only for v1
- **Hardware/sensor data:** LibreHardwareMonitorLib (NuGet, MPL-2.0 license) — do not build custom kernel-level sensor access from scratch
- **Local storage:** SQLite (scan history, change logs, snapshots)
- **Distribution:** installer (.msi/.exe) for v1. Portable single-exe "quick check" mode is a possible future roadmap item, not v1.
- **Architecture layering (strict):**
  ```
  UI → Application Layer → DeviceDNA Model → Detection Engine → Platform Adapters → OS/Hardware
  ```
  The Platform Adapter layer is the ONLY OS-specific code. This is what allows future Linux/macOS support to be a new adapter, not a rewrite — the DNA model, rules engine, and UI logic stay OS-agnostic.

---

## 12. Explicitly Out of Scope for V1

- Mobile companion app (phones/tablets)
- Linux, macOS support
- CLI and local API (architecture should allow for it later, not build it now)
- AI-generated diagnosis text (rules engine only)
- Numeric health scores
- Deep tier fields for any DNA
- Display, Audio, USB/Peripherals DNA types

---

## Known Blockers

| Blocker | Waiting on | Reported |
|---|---|---|
| None | — | — |
