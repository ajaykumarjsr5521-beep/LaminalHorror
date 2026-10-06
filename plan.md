# plan.md — current work (update at the end of each day)

Schedule: docs/14-sprint-plan.md (Sprint 1 ends Oct 11; Sprint 2 = F-10, Oct 12-18). Branch: `feature/F-10-lighting-audio` (cut from `main`).

## Done
- F-09b merged to `main` 2026-10-06. EditMode 253/253, PlayMode 148/148 on `main` (re-run after merge).
- PlayMode hang fixed (Horror fixture left Level_B1 alive); run PlayMode under `timeout 540` and kill Unity.exe if it expires.

## Now: F-10 spec and checklist (spec committed as first commit of the branch)
- [x] F-10 spec in doc 05 (scope, AC1-AC9, tests, sub-steps)
- [x] Owner decided: free CC0 audio now (may ship in closed test), commissioned later
- [x] Step 1: cue list fixed in the spec
- [x] Step 2: budgets in spec (16 voices, 60 MB on disk, 80 MB RAM)
- [x] Step 3: `CueCatalog` + EditMode caption-rule test (AC1): 4/4 pass
- [x] Step 4: `AudioBus` (EditMode 4/4) and `AudioDirector` (PlayMode 6/6): pool of 16, caption, ducking, sliders. Full suites: EditMode 261/261, PlayMode 154/154. No mixer asset (see spec)
- [x] Step 5: footsteps by surface (FloorSurface, FootstepTimer, FootstepPlayer, catalog asset, Level_B1 regenerated). EditMode 267/267, PlayMode 158/158
- [ ] Step 6: music drone follows tension. Test: AC4
- [ ] Step 7: hook clips into the six events in `HorrorEventSpot`. Test: AC2
- [ ] Step 8: Music/SFX sliders in settings. Test: AC6
- [ ] Step 9: lighting bake, 2-realtime-light check, screenshots. Test: AC7
- [ ] Step 10: asset register rows + build check. Test: AC8
- [ ] Doc 05 status, doc 12 row, CHANGELOG at the end of each day; stop before merge

## Open questions for the owner
- No Android device yet: AC9 (device profiler) stays UNVERIFIED until one exists (Day 4: Android Build Support).
