# 06 — Testing & Performance Plan

Status: DRAFT. **No results exist yet; none are claimed.**

## Test layers
| Layer | Tool | Covers |
|---|---|---|
| EditMode unit | Unity Test Framework | Inventory, save serialisation/migration, code lock, tension director logic |
| PlayMode | UTF | Controller speeds, interaction occlusion, menu/restart loops, Indexer state machine |
| Manual device | Checklists | Touch feel, safe areas, interruptions, thermals |
| Playtest | 5 external testers | Puzzle fairness, pacing, clarity, fear level feedback |
| Build smoke | Batchmode build + install | Each milestone: launches, plays to ending, no exceptions |

## Reference devices (to be confirmed — D6)
- **Low:** Android 8–10-class phone, ~3 GB RAM, Mali-G52/Adreno 610-class GPU.
- **Mid:** 6 GB RAM, Adreno 618/Mali-G76-class.
- **PC:** integrated GPU laptop (e.g., Intel Iris Xe) at 1080p.
If you lack these, we substitute whatever you own plus emulator for functional (not perf) tests; perf claims limited to measured hardware.

## Performance targets
| Metric | Low | Mid | PC |
|---|---|---|---|
| Frame rate (average, gameplay) | ≥30 fps | ≥60 fps | ≥60 fps |
| 1% low | ≥24 fps | ≥45 fps | ≥45 fps |
| Peak RAM | ≤1.2 GB | ≤1.5 GB | ≤2 GB |
| Load time (menu→playable) | ≤15 s | ≤10 s | ≤8 s |
| Thermal | no throttle drop >20% over 20 min | same | n/a |
| Build size (AAB) | ≤ 400 MB target (≤ 200 MB base for Play size limits — verify current limits) | | |
| Battery | ≤ 15%/20 min (measured, informational) | | |

**Measurement:** Unity Profiler + Frame Timing Manager on development build; Android `adb shell dumpsys gfxinfo` / Perfetto; capture 5-min scripted route ×3, report median. Results recorded in `docs/results/` with device, build id, date.

## Stability criteria
0 crashes/ANRs in a 30-min soak ×3 per reference device; app pause/resume ×50 stable; low-memory warning handled; rotation locked to landscape.

## Controls & accessibility criteria
See F-02, F-08. Plus: usable one-handed-thumbs landscape on 6" phone; text ≥ 16 sp equivalent default.

## Regression gate
Every feature: tests green + smoke build before status → DONE.
