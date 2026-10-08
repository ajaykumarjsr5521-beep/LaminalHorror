# plan.md — current work (update at the end of each day)

Branch `feature/F-15-strategic-ai` (from F-13 branch, owner request 2026-10-08; spec F-15 in doc 05, steps a-i). Previous: `feature/F-13-liminal-framework` (redesign removed 2026-10-07; code = F-10 state). Status as of 2026-10-08, from docs 03/05/12. Last full test run on record: EditMode 279/279, PlayMode 167/167 (F-10). Not re-run after the revert (UNVERIFIED).

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

## NEXT, in priority order (spec: doc 05 "F-14 priority and sequencing"; game works offline, F-15 only suggests)
1. [x] F-14g code + tests done on `feature/F-14g-lives-v2` (EditMode 372/372, PlayMode 189/189); open: pause-screen lives tally, person playtest. Merge order: this branch is built on F-15, owner reviews
2. [x] F-14d throw + drop noise done (EditMode 379/379, PlayMode 194/194); open: touch Throw button on HUD, person playtest
3. [ ] F-15 integration: StrategyRunner in B1, publish GameEvents, StalkerAgent reads HideBonus/InvestigationMultiplier, HorrorSuggestionGate, live uvicorn + HTTP, real LLM key
4. [ ] F-14h heartbeat/DangerLevel · 5. [ ] F-14i EntityMemory (local wins over RAG)
6. [ ] F-14f room template + validator; M1 rooms 2-5 (room 5 key opens room 7); M2 rooms 6-10
7. [ ] F-14j scares · F-14k haptics · F-14l final escape · F-14c rig/clips/captions alongside

Tests 2026-10-08: EditMode 312/312, PlayMode 184/184.

## F-15 Strategic AI (current work; spec first commit, then in order)
- [x] a StrategyCommand/Validator/Executor (EditMode 14/14) · [x] b PlayerBehaviorTracker + GameEventBus (EditMode 7/7; emitters not wired yet)
- [x] c StrategyClient with fake transport (EditMode 13/13) · [x] d Server skeleton FastAPI+SQLite (pytest 10/10; packages OK on 3.14)
- [x] e LangGraph graph, rule planner (pytest 14 graph tests) · [x] f LangChain tools + structured LLM output (pytest, fake model only)
- [x] g RAG/Chroma memory (pytest 12 memory tests) · [x] h Director link + rare lines (server + Unity gate; not wired to HorrorEventRunner) · [x] i Debug panel, fairness, perf (code+tests)
- F-15 open (integration, next): place StrategyRunner in Level_B1; publish GameEvents from player/hide/death code; StalkerAgent reads Executor.HideBonus/InvestigationMultiplier (after F-14g branch merges, it edits StalkerAgent); HorrorEventRunner uses HorrorSuggestionGate; live uvicorn + Unity HTTP run; real LLM key test.
- F-14g work is preserved on `feature/F-14g-lives` (one wip commit, UNVERIFIED; split per file before merge).
