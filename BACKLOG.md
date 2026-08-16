# BACKLOG.md — DeviceDNA

Items found while building, bugs, postponed decisions, and fixes land here. When resolved, move
the entry to `CHANGELOG.md` with the resolution noted, and **remove it from this file entirely**
(don't leave a `[Closed]` copy here — CHANGELOG.md is the permanent history; this file is only the
live todo list of what's still open).

## Open Items

### [Open] GPU (and other component) benchmark-database link
- Found during: orbital detail-view feedback pass (2026-08-16)
- Description: User's idea, inspired by a real link they'd found for CPU (CPU-Z Validator, valid.x86.fr, showing top-15-highest-recorded-frequency results for a CPU model — now shipped, see CHANGELOG.md) — is there an equivalent for GPU (and possibly other components)?
- Status: open — CPU version shipped; GPU (and other types) researched and found not currently buildable
- Notes: Researched every major GPU benchmark site (TechPowerUp, UserBenchmark, PassMark/VideoCardBenchmark, Geekbench Browser, 3DMark) — none exposes a per-model URL derivable from a GPU name string; all require an opaque internal numeric ID obtained only via a search step, which this app's own rules forbid scraping for. UserBenchmark also has a documented reputation for skewed scoring, an independent reason to avoid it even if the URL worked. Revisit only if a specific site is found with a genuine name-derivable URL pattern (the same bar the CPU-Z Validator link had to clear) — don't build a fragile ID-lookup table or scraper to force this.

### [Deferred] GPU driver-staleness rule (live "is this outdated" check) not implemented — link-out + WUA opt-in check shipped instead
- Found during: Phase 3; re-scoped and partially resolved after a privacy-scope clarification and dedicated research pass; further supplemented by a Windows Update Agent (WUA) opt-in check
- Description: A live rule comparing the installed driver against "the current latest" was the original ask. Research (NVIDIA/AMD/Intel vendor-site feasibility, then a follow-up on free driver-database aggregators) found: NVIDIA's live-check path is real but sits against an explicit ToS clause prohibiting automated site access; AMD's is real and clean but scraping-based; no free aggregator API exists (DevID.info/DriverGuide/TechPowerUp are paid-only); Verdict (this user's other project) independently reached the same conclusion for its own driver scanner and ships a no-op stub rather than a live check. Separately, the user surfaced third-party research on WUA's `Type='Driver'` search — re-verified directly rather than accepted at face value, confirmed real but weak in practical GPU coverage.
- Status: open (a true vendor-site-accurate live check) — the link-out and the supplementary WUA opt-in check both shipped, see CHANGELOG.md's "[Privacy scope clarified + vendor links shipped]" and "[Windows Update driver check shipped]" entries
- Notes: Decided against building a vendor-site live check for now — not infeasible, but ToS-adjacent (NVIDIA) or fragile (AMD scraping) for a hobby app with no budget for licensed API access. What shipped instead: the GPU name is clickable and opens the vendor's real driver page in the user's browser; additionally, a "Check Windows Update" button runs a real (if incomplete) live check via WUA, with an explicit caveat that WU often lags the vendor's own site for GPU drivers. Revisit a true vendor-accurate check only if a legitimate low/no-cost API path emerges, or if the user decides the ToS risk is acceptable for a specific vendor.

### [Deferred] Motherboard BIOS-update-available rule (live check) not implemented — link-out + WUA opt-in check shipped instead
- Found during: Phase 3; re-scoped and partially resolved same pass as the GPU item above; further supplemented by the same WUA opt-in check
- Description: Same shape as the GPU item — a live "is a newer BIOS available" rule was the original ask.
- Status: open (a true vendor-site-accurate live check) — the link-out and the supplementary WUA opt-in check both shipped, see CHANGELOG.md's "[Privacy scope clarified + vendor links shipped]" and "[Windows Update driver check shipped]" entries
- Notes: Research found ASUS has the best path of the 4 major vendors (a real, if unofficial, JSON API — `GetPDBIOS` — already reverse-engineered by community tools like `g-helper`), no ToS red flags found for any of the 4 vendors for this specific low-frequency individual-use pattern. What shipped instead: Motherboard name is clickable, linking to a real per-model ASUS support URL (verified live) or a general support page for Gigabyte/ASRock/MSI (no clean per-model URL pattern found for those three); additionally, the same "Check Windows Update" button covers Motherboard, with the same WU-lags-vendor-site caveat. A live ASUS-only check via `GetPDBIOS` is a real, buildable future option if wanted — undocumented/unofficial, so it would need its own explicit go-ahead before building, same reasoning as the GPU live-check deferral.

---

## Open Questions Carried From Planning (not blockers, but worth revisiting during build)

- Exact health-light thresholds (e.g. "what temperature counts as yellow for CPU") were intentionally left as tunable constants rather than fixed numbers, centralized in `HealthThresholds.cs`. Real-world testing on this dev machine (BASEMENT PC: Ryzen 7 5800X, ASUS TUF Gaming B550-PRO, 16GB DDR4 @ 3600MHz) happened incidentally during Phases 3-4 and the hardening gauntlet (all thresholds fired/didn't-fire as expected on this hardware), but the thresholds themselves haven't been deliberately tuned against a second, different real machine yet — worth another data point before finalizing.
- Logo: current locked concept has one known imperfection — early AI-generated versions struggled with true mirror symmetry on the two "D" letterforms. A mathematically correct mirrored SVG version was built during planning as a fallback/reference if the chosen logo asset needs refinement later.

---

<!-- Format for future entries:

### [Bug/Fix/Deferred] Short title
- Found during: Phase X
- Description: ...
- Status: open / in progress
- Notes: ...

When an item is resolved: move a concise summary to CHANGELOG.md (most-recent-first) and DELETE
the entry here — do not mark it [Closed] and leave it in this file. This file should only ever
contain items that are still actually open.

-->
