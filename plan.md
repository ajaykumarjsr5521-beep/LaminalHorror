# plan.md — current work (update at the end of each day)

Branch `feature/F-13-liminal-framework` (redesign removed 2026-10-07; code = F-10 state). Status as of 2026-10-08, from docs 03/05/12. Last full test run on record: EditMode 279/279, PlayMode 167/167 (F-10). Not re-run after the revert (UNVERIFIED).

## DONE (Windows-side, tested)
- [x] P0 F-00 setup (Windows build OK) · P1 F-01 controller, F-02 input code
- [x] P2 F-03 interaction, F-04 inventory, F-05 puzzle (logic)
- [x] P3 F-06 save, F-07a-d menus/pause/settings/keypad/journal, F-08 captions + accessibility, X-04 localisation
- [x] F-13 Level_B1 greybox playable end to end (EditMode 223, PlayMode 122)
- [x] F-09a/b tension director + horror events
- [x] F-10 lighting + audio code, 31 CC0 clips in doc 09 (AC1-8)
- [x] Liminal redesign removed (owner decision 2026-10-07)

## DOING
- [ ] Verify the revert: run EditMode + PlayMode (expect 279 / 167)
- [ ] Branch cleanup: drop this branch; F-13 Level B1 lives on `feature/F-13-level-b1`; merge order F-10 then F-13

## TO DO
- [ ] Owner listening/visual check of F-10 audio + lighting; confirm CLEARED rows in doc 09
- [ ] Android Build Support install, APK check (closes F-00), phone checks for F-01/02/06/07/08/13 (touch feel, Android Back, perf, F-10 AC9)
- [ ] F-13 open items: touch controls on level HUD, Settings button in level pause menu
- [ ] F-08 open: flash-rate frame check, caption coverage of real audio
- [ ] F-05: 5-tester playtest
- [ ] F-11 Indexer (conditional on D4, owner decision)
- [ ] P5 polish/perf (art pass, bake, LOD, memory, a11y on devices)
- [ ] P6 / F-12 release engineering after G3 (monetization decision)
- [ ] Owner actions: branch protection (X-01), CI secrets (X-02), Git LFS (X-03)

## F-14 Stilt-Walker (spec in doc 05; a-e logic built, scene placement next)
- [x] a Noise + HearingModel · [~] b AI (placed in Level_B1, tested) · [~] c footsteps (rhythm done; clips, rig, captions open) · [~] d player noise (emitters done; throwing open)
- [x] e Hiding (3 cupboards, entity inspects) · f Rooms 2-10 (one room per series) · g Lives/checkpoints/death · h Heartbeat + mixing
- [ ] i Entity memory · j Scares · k Haptics · l Final escape
- Owner to confirm: Indexer replaced by this entity (D4 yes); Level_B1 = room 1; start with a-e on B1

Tests 2026-10-08: EditMode 312/312, PlayMode 184/184.
