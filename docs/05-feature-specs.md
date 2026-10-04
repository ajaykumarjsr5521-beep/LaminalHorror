# 05 — Feature Specifications

Status legend: TODO · IN_PROGRESS · BLOCKED · DONE (DONE only after acceptance criteria are tested).
All features currently **TODO**; F-00 is **BLOCKED** on Unity availability (D1).

Each feature: Purpose · Player experience · Scope / Exclusions · Dependencies · Technical design · Acceptance criteria (AC) · Test plan · Status · Notes.

---
## F-00 Project setup & build pipeline — IN_PROGRESS (Unity 6000.6.4f1: compiles, 2/2 EditMode tests pass; Windows x64 build succeeds. Android build unverified: Android module not installed)
- **Purpose:** Reproducible project and builds.
- **Scope:** Unity project, URP assets (Mobile/PC), Input System, asmdefs, test assemblies, `.gitignore`, git init, editor build menu (Android APK/AAB, Windows). **Excl.:** signing keys, CI service.
- **Deps:** Unity install + Android/Windows modules.
- **Design:** doc 04.
- **AC:** (1) Project opens with 0 console errors. (2) EditMode test run passes (sample test). (3) Empty scene builds Android APK and Windows exe. (4) Keystore config read from outside repo.
- **Test:** run Unity batchmode build + tests; install APK on device/emulator.

## F-01 First-person controller — IN_PROGRESS (motor, look, stamina, crouch headroom: 18 EditMode + 8 PlayMode tests pass. Not yet done: footsteps/head-bob (moved to F-10), GC-alloc profiling, 30/60 fps clipping comparison, on-device feel test)
- **Purpose/Experience:** Responsive, comfortable movement; weight without sluggishness.
- **Scope:** move, look, sprint (stamina), crouch (with headroom check), head-bob (optional/off-able), footstep events by surface. **Excl.:** jump, lean, climbing.
- **Deps:** F-00, F-02 input.
- **Design:** `CharacterController`-based `PlayerMotor`; `PlayerLook` with sensitivity/invert; crouch collider resize + ceiling check.
- **AC:** walk 3.0 m/s, sprint 5.0 m/s, crouch 1.5 m/s (configurable); cannot stand under low ceiling; no clipping through walls at 30 fps and 60 fps in a test corridor; no per-frame GC allocs (Profiler); camera look has no drift when input is zero.
- **Test:** PlayMode tests for speeds/crouch-ceiling; manual device feel test.

## F-02 Input (Windows + mobile) — IN_PROGRESS (code + 11 EditMode tests pass; KB+M, gamepad, touch, safe-area and multi-touch ACs NOT yet tested on devices; touch UI prefab not built)
- **Scope:** Input actions, `InputRouter`, mobile floating stick + look-drag + buttons, gamepad support (bonus), scalable/relocatable touch controls, safe-area handling. **Excl.:** full key rebinding UI (post-MVP).
- **AC:** same gameplay reachable on KB+M, gamepad, touch; touch controls sit inside safe area on notched devices; multi-touch (move + look + button) works simultaneously; no input stuck after app pause/resume; controls size adjustable 80–130%.
- **Test:** EditMode (router mapping), manual on ≥2 phones (different aspect ratios), Windows.

## F-03 Interaction system — IN_PROGRESS (code complete; 22 PlayMode tests pass. Not yet tested: input-scheme parity on devices, prompt placement in a real HUD canvas, inventory integration (F-04))
- **Scope:** `IInteractable`, focus detection (`Interactor`), prompt view, `Door` (open/close/locked), `Pickup`, `Note`. **Excl.:** physics grab/throw, hold-to-interact (post-MVP).
- **Deps:** F-01, F-02. Inventory (F-04) plugs in through `IKeyProvider` / `IItemReceiver`, so F-03 does not depend on it.
- **Design:** `Interactor` raycasts from the camera (2.0 m). The first hit decides focus, so walls block interaction. A locked door asks the interactor's `IKeyProvider` for its key id. Doors show a generic "Locked" message with no spoilers. Pickups and notes raise C# events.
- **AC:** prompt appears within 2.0 m and about one frame after focus; locked door gives feedback and does not name the item; cannot interact through walls; works with all three input schemes.
- **Test:** PlayMode raycast/occlusion/door/pickup tests; manual on devices.
- **Sub-steps (one commit each):** 1 spec, 2 interfaces + Interactor, 3 Door/Pickup/Note, 4 prompt view, 5 docs.

## F-04 Inventory & journal — IN_PROGRESS (logic complete: 31 EditMode + 32 PlayMode tests pass. Pending: inventory/journal UI (F-07), save persistence (F-06), real item assets, on-device check)
- **Scope:** `ItemDefinition` (ScriptableObject) and `ItemDatabase`; `InventoryModel` (plain C#, capacity, add/remove/has); `PlayerInventory` component implementing `IKeyProvider` + `IItemReceiver`; used keys are consumed; collected notes form a re-readable journal list. **Excl.:** crafting, weight, stacking; inventory/journal UI (moved to F-07 menus); save/load (hooks `Snapshot`/`Restore` here, persistence in F-06).
- **Deps:** F-03 (interfaces).
- **Design:** state is a list of item ids, so save is a string list. Unknown item ids are refused and logged, never silently added. `Door` consumes its key on unlock via `IKeyProvider.ConsumeKey`.
- **AC:** add/remove/has behave correctly incl. capacity and duplicates; used key removed; notes stay readable after pickup; snapshot/restore round-trips; unknown ids refused with an error log. (UI usability and persistence ACs are covered by F-07 and F-06.)
- **Test:** EditMode unit tests (model, database), PlayMode (door+inventory integration).
- **Sub-steps (one commit each):** 1 spec, 2 item definitions, 3 InventoryModel + tests, 4 PlayerInventory + tests, 5 key consumption in Door, 6 Note to journal, 7 docs.

## F-05 Puzzle: "Three Dates" — IN_PROGRESS (logic complete: 43 EditMode + 39 PlayMode tests pass. Pending: keypad UI (F-07), notes/props content in level, persistence (F-06), 5-tester playtest)
- **Scope (this feature):** `CodeLockModel` (digit entry, validation, no lockout), `CodeLock` interactable that unlocks a target `Door` and raises `Solved`, state snapshot/restore. **Excl.:** keypad UI (F-07), actual note/prop content and placement (level build), hint system, timers.
- **Scope (level content, later):** 3 notes + 3 in-world date props give the digits of a 4-digit code that opens the Records Office exit. Clue redundancy: each note text also points to the location of its date prop.
- **Deps:** F-03 (Door, Interactor), F-04 (notes in journal).
- **Design:** model is plain C#, unit-tested. Wrong code clears the entry and gives feedback, with no attempt limit or lockout. The code is configured per lock and validated at startup (length and digits only); a bad config is logged as an error, not silently accepted. `Door.Unlock()` added so a solved lock can open a door without a key.
- **AC:** only the correct code solves it; wrong code never locks the player out and can be retried at once; solved lock stays solved and unlocks its door; snapshot/restore keeps solved state; invalid config reported. Fresh-tester solvability (4 of 5 without help) is checked at playtest once level content exists.
- **Test:** EditMode (model), PlayMode (lock + door); playtest sheet later.
- **Sub-steps (one commit each):** 1 spec, 2 model + tests, 3 Door.Unlock + test, 4 CodeLock + tests, 5 docs.

## F-06 Save / checkpoint — IN_PROGRESS
- **Scope:** `SaveData` (versioned), `SaveSerializer` (JSON, explicit load results), `SaveStore` (file I/O, atomic replace, corrupt-file quarantine), `SaveGame` (capture/apply inventory + solved locks + checkpoint id). **Excl.:** checkpoint trigger volumes and autosave triggers (wired with the level / F-07 pause), cloud, multiple slots, New Game confirmation UI (F-07).
- **Deps:** F-04, F-05.
- **Design:** one slot at `persistentDataPath/save.json`. Write goes to `save.json.tmp`, then replaces the live file, so an interrupted write leaves the previous save intact. Load returns Ok / Missing / Corrupt / UnsupportedVersion with a human-readable message; a corrupt file is never deleted or overwritten silently (it is renamed to `save.json.corrupt-<time>` only when the player explicitly starts a new game). Write failures return an error message to the caller to show; they are never swallowed.
- **AC:** round-trip keeps checkpoint, inventory and solved puzzle flags; interrupted write cannot corrupt the existing save; corrupt file gives a clear result and stays on disk; future-version file refused, not parsed blindly; unknown inventory ids on load are dropped and logged.
- **Test:** EditMode (serializer incl. corrupt/old/new version, store incl. atomic and quarantine), PlayMode (capture/apply). Manual force-stop on Android pending device.
- **Sub-steps (one commit each):** 1 spec, 2 SaveData + serializer + tests, 3 SaveStore + tests, 4 SaveGame + tests, 5 docs.

## F-07 Menus, pause, settings — TODO
- **Scope:** main menu, pause (also auto-pause on app focus loss), settings (sensitivity, volume, captions, touch scale, Story mode, quality), quit (Windows), restart, credits/licences screen. **Excl.:** language selection.
- **AC:** time scale 0 and audio ducked in pause; resume restores state exactly; restart/menu loops 20× without errors or leaks (memory delta < 5 MB); Android Back button handled; settings persist.
- **Test:** PlayMode loop test, manual.

## F-08 Captions & accessibility basics — TODO
- **Scope:** captions for key sounds/events, text size options (S/M/L), colour-safe UI, reduce flicker/flash option (disables strobe-like lighting), reduce camera motion option, no information conveyed by audio only. **Excl.:** screen-reader support, full localisation.
- **AC:** no flashing >3 per second in any event when Reduce Flicker on; all gameplay-critical audio cues have captions; contrast ≥ 4.5:1 for text.
- **Test:** checklist + manual review, flash-rate check by frame capture.

## F-09 Tension director & horror events — TODO
- **Scope:** `TensionDirector`, `HorrorEvent` SO, ≥6 event types (light flicker, door slam, prop shift, audio cue, Misfile corridor reveal, shadow figure). **Excl.:** procedural events.
- **AC:** min gap between scare events ≥ configured (default 45 s) verified by log; events fire once when flagged; events never fire during note reading/pause/menus; event data editable without code; debug overlay in dev builds.
- **Test:** EditMode simulation of director over time; manual pacing playtest.

## F-10 Lighting, audio & level art pass — TODO
- **Scope:** baked lighting, ≤2 realtime lights, ambience beds, footsteps, music drones, occlusion culling, LODs where needed, volume/post-FX (vignette/grain tuned for mobile).
- **AC:** meets perf budgets (doc 06) on reference devices; no light-leak seams in lightmaps on review; audio mix ducking works; all assets in register (doc 09) with status CLEARED.
- **Test:** profiler capture on devices; asset register audit.

## F-11 The Indexer (conditional on D4) — TODO
- **Scope:** NavMesh patrol, hearing/vision, chase ≤12 s, hide spots, checkpoint-respawn. **Excl.:** multiple enemy types.
- **AC:** never unavoidable (a valid escape exists for every encounter, verified on authored route); detection respects crouch/noise; Story mode halves detection; CPU ≤1 ms/frame on ref device; no softlocks on respawn.
- **Test:** PlayMode state-machine tests; 10 scripted run-throughs; manual.

## F-12 Release engineering — TODO (after G3)
- **Scope:** signed AAB, versioning, Play listing assets, Data safety form, content rating, closed test track, Windows zip/installer. See doc 08.
- **AC:** all doc-08 checklist items checked with evidence; AAB installs via Play internal testing on ≥2 devices; no crashes in 30-min soak.
