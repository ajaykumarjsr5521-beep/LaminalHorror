# 12 — Progress Log (living document)

Updated with every merge/milestone. Newest first. Verification claims cover only what was actually run.

## Current status
- Phase: **P1 Controller** (P0 setup done except the Android build check)
- Engine: Unity 6000.6.4f1 (Windows + WebGL modules installed; **Android module missing**)
- Active branch: `main` (F-02 merged; next: F-01)

## Changelog
| Date | Branch / merge | Change | Verified |
|---|---|---|---|
| 2026-10-04 | `feature/F-02-input` (merged) | PlayerInputState, InputRouter, device (KB+M, gamepad) and touch sources, aggregator, VirtualStick, TouchLookArea, TouchButton, SafeAreaFitter, 80-130% scale clamp | Compiles; 13/13 EditMode tests pass. Not tested on hardware or touch |
| 2026-10-04 | `feature/F-00-project-setup` (merged) | Unity scaffold, asmdefs, packages (Input System 1.20.0, URP 17.6.0), Boot scene, batchmode build script | Compiles; 2/2 EditMode tests; Windows x64 build OK. Android NOT verified |
| 2026-10-03 | `docs/workflow-and-phases` (merged) | Git workflow, phase-to-branch map | n/a |
| 2026-10-03 | initial commit | Discovery report + specs 00-10 | n/a |

## In progress
- Nothing active. F-02 remaining: touch UI prefab + on-device ACs (needs Android module + a phone).

## Upcoming (in order)
1. (done) F-02 input code merged; device ACs pending
2. Android module install: F-00 Android APK check, then F-00 DONE
3. F-01 First-person controller (move, look, sprint, crouch)
4. Greybox level (6 spaces) for movement/pacing tests
5. P2: F-03 Interaction, F-04 Inventory, F-05 Puzzle
6. P3: F-06 Save, F-07 Menus, F-08 Accessibility
7. P4: F-09 Tension director, F-10 Lighting/audio, F-11 Indexer (if D4)
8. P5 polish/perf, P6 release (F-12)

## Known blockers / risks
- Android Build Support not installed (blocks APK/AAB and on-device touch testing).
- No physical-device testing yet; touch behaviour is unverified until then.
- Unity 6000.6 is a newer stream than first assumed; package versions were fixed via registry lookup and compile checks.
