# 12 — Progress Log (living document)

Updated with every merge/milestone. Newest first. Verification claims cover only what was actually run.

## Current status
- Phase: **P1 Controller** (P0 setup done except the Android build check)
- Engine: Unity 6000.6.4f1 (Windows + WebGL modules installed; **Android module missing**)
- Active branch: `feature/F-07-menus-settings`

## Changelog
| Date | Branch / merge | Change | Verified |
|---|---|---|---|
| 2026-10-04 | `chore/commit-practice` (merged) | One-commit-per-file rule, message format, commit-msg hook, commit template, PR/README/doc updates | Hook tested by hand (3 accept, 3 reject) |
| 2026-10-04 | `feature/X-04-localisation` (merged) | `Loc` string table, default English, Door/Pickup/Note/CodeLock/Save messages migrated, guard tests, ADR 0005 | 83/83 EditMode, 47/47 PlayMode pass |
| 2026-10-04 | `chore/engineering-process` (merged) | README, CHANGELOG, .editorconfig, PR/issue templates, CODEOWNERS, ADRs 0001-0004, gated CI workflow, doc 13 process + gap analysis, X-01..X-04 specs | Docs/config only; CI not yet run |
| 2026-10-04 | `feature/F-06-save` (merged) | SaveData v1, SaveSerializer (Ok/Missing/Corrupt/UnsupportedVersion), SaveStore (atomic write, quarantine), SaveGame (checkpoint, inventory, puzzles); inventory restore drops unknown ids; CodeLock.PuzzleId | 64/64 EditMode, 47/47 PlayMode pass. No device test |
| 2026-10-04 | `feature/F-05-puzzle` (merged) | CodeLockModel (no lockout, validated config), CodeLock interactable, Door.Unlock | 43/43 EditMode, 39/39 PlayMode pass |
| 2026-10-04 | `feature/F-04-inventory` (merged) | ItemDefinition/ItemDatabase (validated), InventoryModel, PlayerInventory (keys consumed, journal notes, snapshot/restore), Note to journal | 31/31 EditMode, 32/32 PlayMode pass |
| 2026-10-04 | `feature/F-03-interaction` (merged) | IInteractable, Interactor (2 m raycast, wall occlusion), Door (locked, generic message), Pickup, Note, prompt view, IKeyProvider/IItemReceiver hooks for F-04, run-tests.sh | 18/18 EditMode, 22/22 PlayMode pass. No device testing |
| 2026-10-04 | `feature/F-01-controller` (merged) | PlayerMotor (walk 3 / sprint 5 / crouch 1.5 m/s, headroom check), PlayerLook (pitch clamp), StaminaModel, Greybox test scene (editor-generated), PlayMode test assembly | 18/18 EditMode, 8/8 PlayMode pass; Windows build OK. No device or profiler testing |
| 2026-10-04 | `feature/F-02-input` (merged) | PlayerInputState, InputRouter, device (KB+M, gamepad) and touch sources, aggregator, VirtualStick, TouchLookArea, TouchButton, SafeAreaFitter, 80-130% scale clamp | Compiles; 13/13 EditMode tests pass. Not tested on hardware or touch |
| 2026-10-04 | `feature/F-00-project-setup` (merged) | Unity scaffold, asmdefs, packages (Input System 1.20.0, URP 17.6.0), Boot scene, batchmode build script | Compiles; 2/2 EditMode tests; Windows x64 build OK. Android NOT verified |
| 2026-10-03 | `docs/workflow-and-phases` (merged) | Git workflow, phase-to-branch map | n/a |
| 2026-10-03 | initial commit | Discovery report + specs 00-10 | n/a |

## In progress
- F-07 split: F-07a settings DONE; next F-07b pause controller, then F-07c screens and F-07d keypad/journal.

## In progress (older notes)
- Nothing active. F-06 remaining: level checkpoint triggers, menu wiring (F-07), Android force-stop test.

## Upcoming (in order)
1. (done) F-02 input code merged; device ACs pending
2. Android module install: F-00 Android APK check, then F-00 DONE
3. (done) F-01 controller code merged; device and profiler ACs pending
4. (done, test scene only) Greybox level; the real 6-space level comes later
5. (done) P2: F-03, F-04, F-05 merged
6. P3: (F-06 merged) F-07 Menus, F-08 Accessibility
7. P4: F-09 Tension director, F-10 Lighting/audio, F-11 Indexer (if D4)
8. P5 polish/perf, P6 release (F-12)

## Process gaps found 2026-10-04 (see doc 13)
No PR review or branch protection, no CI, no README/CHANGELOG/ADRs, no style rules, no LFS, hardcoded strings. Baseline added in X-01; CI (X-02) and LFS (X-03) need owner actions; strings (X-04) next.

## Known blockers / risks
- Owner actions pending: enable branch protection on `main` (X-01), add Unity CI secrets (X-02), install Git LFS (X-03).
- Android Build Support not installed (blocks APK/AAB and on-device touch testing).
- No physical-device testing yet; touch behaviour is unverified until then.
- Unity 6000.6 is a newer stream than first assumed; package versions were fixed via registry lookup and compile checks.
