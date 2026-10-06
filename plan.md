# plan.md — current work (update at the end of each day)

Schedule: docs/14-sprint-plan.md (Sprint 1 ends Oct 11; Sprint 2 = F-10, Oct 12-18). Branch: `feature/F-10-lighting-audio` (cut from `main`).

## Done
- F-09b merged to `main` 2026-10-06. EditMode 253/253, PlayMode 148/148 on `main` (re-run after merge).
- PlayMode hang fixed (Horror fixture left Level_B1 alive); run PlayMode under `timeout 540` and kill Unity.exe if it expires.

## Next: liminal redesign (docs/15-liminal-redesign.md, branch docs/liminal-redesign)
- [x] Audit and redesign written (DRAFT). Owner reviews it.
- [ ] Owner decisions: slice-first (Hotel Meridian + The Guest) vs more; art source for the architecture kit; keep Floor B1 as prologue/hub
- [ ] Then: write F-13 spec in doc 05, task checklist here, and only then code

## F-10 (kept below until merged)
## Done in F-10: spec and checklist (spec committed as first commit of the branch)
- [x] F-10 spec in doc 05 (scope, AC1-AC9, tests, sub-steps)
- [x] Owner decided: free CC0 audio now (may ship in closed test), commissioned later
- [x] Step 1: cue list fixed in the spec
- [x] Step 2: budgets in spec (16 voices, 60 MB on disk, 80 MB RAM)
- [x] Step 3: `CueCatalog` + EditMode caption-rule test (AC1): 4/4 pass
- [x] Step 4: `AudioBus` (EditMode 4/4) and `AudioDirector` (PlayMode 6/6): pool of 16, caption, ducking, sliders. Full suites: EditMode 261/261, PlayMode 154/154. No mixer asset (see spec)
- [x] Step 5: footsteps by surface (FloorSurface, FootstepTimer, FootstepPlayer, catalog asset, Level_B1 regenerated). EditMode 267/267, PlayMode 158/158
- [x] Step 6: music drone follows tension, silent when blocked (MusicDroneModel, MusicDrone). EditMode 271/271, PlayMode 160/160
- [x] Step 7: events play their cue via Cues.Play (AC2). EditMode 271/271, PlayMode 162/162
- [x] Step 8: Music/SFX sliders drive the buses via AudioLevels (AC6; persistence was already tested in SettingsStoreTests). EditMode 271/271, PlayMode 164/164
- [x] Step 9: 2 realtime lights (Hall, Stacks), 3 baked, lightmaps baked (about 1.8 MB, no LFS yet). Screenshot review: no seams seen. EditMode 271/271, PlayMode 167/167
- [x] Step 10: AssetRegister parser, audio-file coverage test, release gate before AAB builds (AC8). No audio files exist yet, so the register has no audio rows. EditMode 276/276, PlayMode 167/167
- [ ] Doc 05 status, doc 12 row, CHANGELOG at the end of each day; stop before merge

- [x] CC0 audio sourced: 31 files, licence files read, registered (AU-01..AU-31). Stand-ins: whisper, light buzz. EditMode 279/279, PlayMode 167/167

## Open questions for the owner
- Listen to the game and say which sounds feel wrong (whisper and light buzz are stand-ins); confirm the CLEARED rows in doc 09.
- No Android device yet: AC9 (device profiler) stays UNVERIFIED until one exists (Day 4: Android Build Support).
