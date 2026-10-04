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

## F-06 Save / checkpoint — IN_PROGRESS (logic complete: 64 EditMode + 47 PlayMode tests pass. Pending: checkpoint/autosave triggers in level, Continue/New Game menu + confirmation (F-07), real force-stop test on Android)
- **Scope:** `SaveData` (versioned), `SaveSerializer` (JSON, explicit load results), `SaveStore` (file I/O, atomic replace, corrupt-file quarantine), `SaveGame` (capture/apply inventory + solved locks + checkpoint id). **Excl.:** checkpoint trigger volumes and autosave triggers (wired with the level / F-07 pause), cloud, multiple slots, New Game confirmation UI (F-07).
- **Deps:** F-04, F-05.
- **Design:** one slot at `persistentDataPath/save.json`. Write goes to `save.json.tmp`, then replaces the live file, so an interrupted write leaves the previous save intact. Load returns Ok / Missing / Corrupt / UnsupportedVersion with a human-readable message; a corrupt file is never deleted or overwritten silently (it is renamed to `save.json.corrupt-<time>` only when the player explicitly starts a new game). Write failures return an error message to the caller to show; they are never swallowed.
- **AC:** round-trip keeps checkpoint, inventory and solved puzzle flags; interrupted write cannot corrupt the existing save; corrupt file gives a clear result and stays on disk; future-version file refused, not parsed blindly; unknown inventory ids on load are dropped and logged.
- **Test:** EditMode (serializer incl. corrupt/old/new version, store incl. atomic and quarantine), PlayMode (capture/apply). Manual force-stop on Android pending device.
- **Sub-steps (one commit each):** 1 spec, 2 SaveData + serializer + tests, 3 SaveStore + tests, 4 SaveGame + tests, 5 docs.

## F-07 Menus, pause, settings — IN_PROGRESS (split into sub-features)
- **Purpose/experience:** the player can start, pause, resume, adjust comfort settings and leave without friction or lost progress.
- **Scope overall:** main menu, pause (also auto-pause on app focus loss), settings (look sensitivity, invert-Y, volumes, captions on/off, touch control scale, Story mode, quality), quit (Windows), restart, credits/licences screen, Continue/New Game with confirmation (uses F-06), keypad and journal screens (use F-05/F-04). **Excl.:** language selection, key rebinding.
- **Deps:** F-02, F-04, F-05, F-06, X-04 (all text via `Loc`).
- **Overall AC:** time scale 0 and audio ducked in pause; resume restores state exactly; restart/menu loops 20 times without errors or leaks (memory delta under 5 MB); Android Back button handled; settings persist; every screen usable at 5-inch phone and 1080p PC; all text via `Loc`.
- **Test:** EditMode (settings logic), PlayMode (pause, loop test), manual device checks.

### F-07a Settings model and persistence — DONE (EditMode 107/107, PlayMode 51/51; logic only, no device-dependent criteria. UI is F-07c)
- **Scope:** `SettingsData` (versioned, defaults, clamping), `SettingsStore` (settings.json, atomic write, separate from the save file), `SettingsApplier` (pushes values into `InputRouter`, `ControlsLayout`, audio volume, quality level). **Excl.:** UI.
- **Design:** values are clamped on load and set (sensitivity 0.2-3, volumes 0-1, touch scale 0.8-1.3, quality within available levels). A missing file gives defaults silently (first run). A corrupt or newer-version file gives defaults and an explicit warning result for the UI to show; the bad file is kept, as with saves. The atomic temp-file write is shared with `SaveStore` through one `AtomicFile` helper (no duplicated I/O code).
- **AC:** round trip keeps all values; out-of-range values clamp; corrupt file returns defaults plus warning and the file stays on disk; failed write returns an error message; `SaveStore` tests still pass after the shared-helper refactor; applier changes `InputRouter.LookSensitivity`, `InvertY` and `ControlsLayout.Scale`.
- **Test:** EditMode (data, store), PlayMode (applier).

### F-07b Pause controller — DONE for logic (PlayMode 62/62). Open: audio is paused, not ducked (decision: pause is simpler and silent menus suit horror; revisit with F-10); Android Back mapping needs a device check
- **Scope:** `PauseController` (pause/resume, time scale, audio pause/duck, auto-pause on focus loss and app pause, Android Back key toggles pause). **Excl.:** UI view.
- **AC:** pause sets `Time.timeScale` to 0 and restores the previous value on resume; double pause/resume are idempotent; focus loss pauses; resume after focus return does not auto-resume; restoring does not change input state beyond a reset.
- **Test:** PlayMode.

### F-07c Screens (main menu, pause, settings, confirm dialog, credits) — IN_PROGRESS (models and views DONE for Windows-side checks: EditMode 133/133, PlayMode 71/71 incl. 9 tests driving the real MainMenu scene; layouts reviewed from screenshots at 1920x1080, 2400x1080, 1280x720. OPEN: real touch/phone check, Android Back, pause overlay not yet placed in a gameplay scene, corrupt-save message shows raw parser text, Story-mode and Quality rows need scrolling on small screens)
- **Scope:** (1) UI-independent models: `MainMenuModel` (Continue availability, save warnings, New Game with confirmation), `SettingsScreenModel` (working copy, live preview, save, revert, reset to defaults, dirty tracking). (2) Thin uGUI views built on them: main menu, pause menu (Resume, Restart, Settings, Main Menu, Quit on Windows), settings screen, confirm dialog, credits/licences. **Excl.:** language selection, key rebinding, final art.
- **Design:** models are plain C# and unit-tested; views only bind and forward taps, and every label comes from `Loc`. New Game asks for confirmation only when a save exists (a corrupt or unsupported save counts as existing and is preserved via the F-06 quarantine). A save that cannot be read shows its warning on the main menu and disables Continue; it never crashes or hides the problem. Settings changes preview live; Back without Save asks nothing but reverts, and Save persists through `SettingsStore`, showing the error message if the write fails. Quit exists on Windows only.
- **AC (models):** Continue enabled only when the save loads Ok; New Game with an existing save needs explicit confirmation and cancelling changes nothing; New Game with no save starts at once; failed clearing reports an error and does not start; corrupt save warning is surfaced; settings dirty tracking is accurate; revert restores originals and re-applies them; reset restores defaults; failed save keeps the screen open with an error.
- **AC (views, manual):** every screen usable at 5-inch phone and 1080p PC; touch targets at least 48 dp; no overlap with the safe area; Back button returns up one level; all text via `Loc`.
- **Test:** EditMode (models), manual device review for views.
- **Sub-steps (one commit per file):** 1 spec, 2 strings, 3 `SettingsData` clone/equality, 4 `MainMenuModel` + tests, 5 `SettingsScreenModel` + tests, 6 views and scene builder (next step), 7 docs.

### F-07d Keypad, note reader and journal screens — IN_PROGRESS (logic and prefabs DONE and tested: EditMode 158/158, PlayMode 87/87; layouts reviewed from screenshots at 3 sizes. OPEN: phone touch check, journal button/HUD placement and keypad wiring in the real level, long note titles are ellipsised in the list)
- **Purpose/experience:** typing a code, reading a note and re-reading old notes all feel immediate and never trap the player.
- **Scope:** `ModalGate` (Core) so open screens stop movement, look and interaction; `KeypadView` for `CodeLock` (digit pad, clear, enter, close; wrong code shows a non-punishing message and clears; solved closes with confirmation); `NoteReaderView` opened by `Note.Opened`; `JournalModel` + `JournalView` (list of collected notes with a reader pane on the same screen); prefabs generated by an editor builder; strings through `Loc`. **Excl.:** journal button placement in the final HUD (with level HUD), audio feedback, haptics.
- **Deps:** F-03 (Note events), F-04 (journal notes), F-05 (CodeLock), F-07c (UiKit, builder, screenshot tool), X-04.
- **Design:** open modals register with `ModalGate`; `InputRouter` drops movement, look, sprint and interact while a modal is open but keeps pause. Keypad entry display comes from a pure formatter (testable). Journal selection lives in `JournalModel` (empty state, bounds-safe selection). Views only forward taps. Prefabs are generated, not hand-edited.
- **AC:** keypad shows typed digits and remaining slots; only the correct code solves; wrong code shows a message and allows an immediate retry (no lockout); solved keypad closes and the door unlocks; reader shows title and body of the note just read; journal lists collected notes in pickup order, shows an empty-state message when none, and any note is readable within 3 taps (open journal, tap note = 2); opening any of these blocks movement and look, closing restores them; closing never leaves the gate stuck open (including when destroyed while open); all text via `Loc`; layouts reviewed at 1920x1080, 2400x1080, 1280x720; touch targets at least 48 dp.
- **Test:** EditMode (`ModalGate`, formatter, `JournalModel`), PlayMode (prefabs with real `CodeLock`/inventory, input blocking), screenshot review; phone check pending Android module.
- **Sub-steps (one commit per file):** 1 spec, 2 `ModalGate` + tests, 3 `InputRouter` integration + tests, 4 strings, 5 keypad formatter + `KeypadView`, 6 `JournalModel` + tests, 7 `NoteReaderView` + `JournalView`, 8 prefab builder + screenshot capture, 9 PlayMode tests, 10 docs.

- **Sub-steps for F-07a (one commit per file):** 1 spec, 2 `AtomicFile` + test, 3 `SaveStore` refactor to use it, 4 `SettingsData` + tests, 5 `SettingsStore` + tests, 6 string keys, 7 `SettingsApplier` + tests, 8 docs.

## F-08 Captions & accessibility basics — IN_PROGRESS (a-e implemented and tested on Windows: EditMode 209/209, PlayMode 103/103; text-size and notice layouts reviewed from screenshots. OPEN: frame-capture flash-rate check and caption coverage of real audio cues need F-09/F-10 content; phone check)
- **Purpose/experience:** players who cannot hear, are sensitive to light or motion, or need larger text can play the whole game; no information is conveyed by audio or colour alone.
- **Scope overall:** captions for gameplay-relevant sounds/events, text size options (Small/Medium/Large), contrast-checked UI palette, Reduce Flicker (limits flashes), Reduce Camera Motion, first-launch content warning, pause anywhere (done in F-07b). **Excl.:** screen-reader support, full localisation, per-caption styling options beyond size.
- **Deps:** F-07a/c (settings and screens), X-04 (strings). Audio cues arrive with F-10, so cue-to-caption wiring for real sounds happens there; this feature delivers the system and its tests.
- **Overall AC:** with Reduce Flicker on, no light or screen effect changes state more than 3 times per second; every gameplay-critical audio cue has a caption key (checked when F-10 lands); UI text contrast at least 4.5:1 against its background (WCAG AA); text size changes apply to every screen without clipping at 1280x720; settings persist; content warning shown once before first play and re-readable from settings.
- **Test:** EditMode (state, budget, caption queue, contrast), PlayMode (views), screenshots at all text sizes, checklist review; frame-capture flash-rate check once real lighting events exist.

### F-08a Accessibility state and settings — DONE (tests: state, new settings fields, older files, applier, settings rows live preview/persist/revert)
- **Scope:** `Accessibility` static state in Core (captions on, text scale, reduce flicker, reduce motion) set by `SettingsApplier`; new `SettingsData` fields `TextSize` (0 small, 1 medium, 2 large), `ReduceFlicker`, `ReduceMotion`; settings screen rows and strings.
- **Design:** consumers read `Accessibility`, never `SettingsData`, so game systems do not depend on the settings screen. New fields are additive: files saved by earlier builds load with defaults (no version bump needed; `Version` stays 1 until a breaking change).
- **AC:** values round-trip through `settings.json`; out-of-range `TextSize` clamps; older files without the fields load with defaults; applying settings updates `Accessibility`.

### F-08b Flash budget and flicker-safe lighting math — DONE as math (measured: no window above 3 flashes under hammering; reduced waveform at most 3 midpoint crossings per second and at most 25 percent deep). Real lighting events must use it: see F-09/F-10
- **Scope:** `FlashBudget` (pure logic: at most 3 flashes in any rolling 1-second window) and `SafeFlicker` (flicker waveform that, with Reduce Flicker on, becomes a slow fade). **Excl.:** actual horror light events (F-09, F-10) which must use these.
- **AC:** budget never permits a 4th flash inside any 1-second window; waveform with Reduce Flicker on crosses its midpoint at most 3 times per second; with it off, behaviour is unchanged.

### F-08c Captions — DONE for the system (queue rules, view, prefab, off switch, pause-safe expiry). Caption keys for real sounds are added with F-10
- **Scope:** `CaptionService` (queue with priority, de-duplication, maximum visible lines, per-cue duration, honours the Captions setting) and `CaptionView` prefab at the bottom of the screen, scaled by text size. **Excl.:** speaker names, positional arrows.
- **AC:** posted cue shows for its duration then disappears; same cue posted again while visible refreshes rather than stacks; at most 3 lines visible, lower priority dropped first; nothing shows when captions are off; text readable at all three sizes.

### F-08d Text size and contrast — DONE (no compounding; every screen reviewed at Small and Large on 1280x720/1920x1080/2400x1080; all palette pairs at least 4.5:1 incl. highlighted button and captions over white). Found and fixed: journal list squeezed at Large text
- **Scope:** `ScalableText` component applying the text scale to TMP labels (base size remembered, so scaling is reversible and not cumulative); contrast check test over the UI palette.
- **AC:** switching sizes never compounds; every screen fits at the Large size at 1280x720 (screenshot review); all palette text/background pairs at least 4.5:1.

### F-08e Content notice screen — DONE (shown before the menu until accepted, remembered, re-readable from credits, failed save never traps the player, unreadable settings preserved). Note: wording is a first draft for owner review
- **Scope:** first-launch screen naming fear themes, flashing lights and intense sounds, with a continue button and a link to the settings; accepted flag stored in settings; re-readable from the credits/settings area. Status: TODO.

- **Sub-steps for F-08a-d (one commit per file):** spec, `Accessibility` + tests, `SettingsData` fields + tests, `SettingsApplier` + tests, `FlashBudget`/`SafeFlicker` + tests, `CaptionService` + tests, `ScalableText`, caption prefab and settings rows in the builders, contrast test, screenshots, docs.

## F-13 Level "Night Shift: Floor B1" (greybox, playable end to end) — IN_PROGRESS (spec written, nothing built yet)
- **Why now:** every merged system (controller, interaction, inventory, puzzle, save, menus, captions) is only tested in isolation. This feature wires them into one playable loop so the remaining work (F-09 events, F-10 art/audio) is built on something real. Chosen over starting F-09 first (decision recorded in the progress log).
- **Scope:** one scene `Level_B1`, generated by an editor builder (same approach as the menu scene: regenerate, do not hand-edit), using primitive greybox geometry only (no third-party art, so nothing to log in doc 09). Layout follows GDD section 4: Staff Entrance and Break Room, Main Reading Hall (hub), Stacks A, Records Office with the keypad door, Loading Dock Stair (quiet greybox until F-09/F-11) and the exit, with lamp-room checkpoints between areas. Contents: player rig, 3 notes, 3 date props, Brass Stamp pickup, key door(s), the Three Dates `CodeLock`, 2 checkpoint volumes, an exit trigger, HUD (interaction prompt, captions, pause button, journal button, keypad and note reader prefabs), pause overlay, `LevelBootstrap` wiring Continue / New Game / autosave.
- **Excl.:** scare events and tension director (F-09), final lighting, audio and art (F-10), the enemy (F-11), real note prose beyond placeholder-quality text kept in `DefaultStrings`.
- **Deps:** F-01 to F-08, MainMenu scene.
- **Design:** `LevelBootstrap` is a thin MonoBehaviour; checkpoint and exit rules live in plain C# (`CheckpointModel`, `LevelProgress`) so they are unit-tested. MainMenu Continue loads the level and applies the save; New Game clears the save and starts at the entrance. Checkpoint triggers call `SaveGame` and show a caption-style "Checkpoint" message. Reaching the exit shows a short end screen with Main Menu. All new player-facing text uses `Loc` keys. Modal screens use `ModalGate`.
- **AC:**
  1. Menu to level: New Game and Continue both load `Level_B1`; Continue restores inventory, solved lock and checkpoint position.
  2. The puzzle can be solved from clues found in-world only; a scripted PlayMode run (collect notes, read digits from the date props, enter code, open door, reach exit) passes.
  3. The exit is unreachable without solving the puzzle (test tries to walk through; no sequence-break via collision gaps in the builder output).
  4. Every interactable shows a prompt and works with the keyboard, gamepad and touch input paths already defined in F-02.
  5. Pause, journal, keypad and note reader open and close without leaving gameplay input blocked or the cursor in a wrong state.
  6. Checkpoint saves on entry; killing and reloading the scene resumes at the last checkpoint with no lost or duplicated items.
  7. No literal player-facing strings (guard test passes); no missing-key errors in the console during the scripted run.
  8. Screenshots of each room are reviewed at 1920x1080 and 1280x720; HUD pieces do not overlap each other or the safe area.
- **Test:** EditMode (checkpoint/progress models), PlayMode (scripted full run, Continue restore, exit gating, modal input), editor screenshot review. Not testable here: touch feel, performance on a phone, Android Back (need device, D13).
- **Sub-steps (one commit per file):** 1 spec, 2 `CheckpointModel`/`LevelProgress` + tests, 3 strings, 4 `LevelBootstrap` + trigger components, 5 level builder, 6 HUD wiring in `GameplayUiBuilder`, 7 menu-to-level flow, 8 PlayMode full-run tests, 9 screenshots, 10 docs and progress log.

## F-09 Tension director & horror events — TODO
- **Accessibility requirement (from F-08):** every flashing or strobing light/screen effect must go through `FlashBudget` and `SafeFlicker`, and every camera shake/sway must be multiplied by `Accessibility.MotionScale`. Tests must prove no more than 3 state changes per second with Reduce Flicker on.
- **Scope:** `TensionDirector`, `HorrorEvent` SO, ≥6 event types (light flicker, door slam, prop shift, audio cue, Misfile corridor reveal, shadow figure). **Excl.:** procedural events.
- **AC:** min gap between scare events ≥ configured (default 45 s) verified by log; events fire once when flagged; events never fire during note reading/pause/menus; event data editable without code; debug overlay in dev builds.
- **Test:** EditMode simulation of director over time; manual pacing playtest.

## F-10 Lighting, audio & level art pass — TODO
- **Accessibility requirement (from F-08):** every gameplay-relevant sound has a caption key in `DefaultStrings` and posts it through `Captions.Post`; an EditMode test lists the cues and fails if one lacks a caption. Flicker uses `SafeFlicker`; camera motion uses `Accessibility.MotionScale`.
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

---

# Process and cross-cutting items (X)
Details and rationale in [13-engineering-process.md](13-engineering-process.md).

## X-01 Engineering process baseline — IN_PROGRESS
- **Scope:** README, CHANGELOG, `.editorconfig`, PR/issue templates, CODEOWNERS, ADRs 0001-0004, Definition of Ready/Done, testing policy, bug severity scale.
- **Also covers:** per-file atomic commits, commit message format and the `commit-msg` hook (`.githooks/`, `.gitmessage`).
- **AC:** files exist and are referenced from README; next feature PR uses the template; hook rejects a malformed subject (tested by hand); the history of the next feature shows one commit per file.
- **Open (owner):** enable branch protection and required PR on `main`. DONE when the first PR is reviewed under the new rules.

## X-02 Continuous integration — BLOCKED (owner action)
- **Scope:** run EditMode + PlayMode on every PR (`.github/workflows/tests.yml`, written, gated).
- **Needs:** repo variable `UNITY_CI_ENABLED=true`; secrets `UNITY_LICENSE`, `UNITY_EMAIL`, `UNITY_PASSWORD`; confirm a GameCI image supports Unity 6000.6.4f1.
- **AC:** a PR with a failing test shows a red check; a green run is linked in the PR.

## X-03 Git LFS for binary assets — TODO (before first art/audio commit)
- **Scope:** `.gitattributes` LFS rules for textures, models, audio, video; document install in README.
- **Needs:** owner installs Git LFS and runs `git lfs install`; confirm the GitHub LFS quota is sufficient.
- **AC:** a test binary commit appears as an LFS pointer; clone and checkout works on a clean machine.

## X-04 Localisation-ready strings — DONE (EditMode: 83/83 incl. key-existence and literal-string guards; PlayMode 47/47; no device-dependent criteria)
- **Scope:** `Loc` (Core) with `Get`, `Format`, `Has`, `SetTable`, `ResetToDefault`; default English table in code; migrate every existing player-facing string: Door (prompts, locked message), Pickup (prompt, refusal), Note (prompt), CodeLock (prompts), Save (all load/write messages). **Excl.:** additional languages, font fallback, RTL, translation workflow, ScriptableObject table asset (added when a second language is approved).
- **Design:** keys are `area.name` (e.g. `door.prompt.open`). A missing key returns `[key]` and logs an error once per key, never an empty string. `Format` uses `string.Format` with the table text. Tests that assert English text use `Loc.Get`, not literals, except where the wording is the thing under test.
- **AC:** every key used in code exists in the default table (EditMode test); missing key reported explicitly; `SetTable` changes displayed text (EditMode test); a guard test fails if migrated areas reintroduce literal player-facing strings; all existing tests still pass.
- **Sub-steps (one commit each):** 1 spec, 2 Loc + tests, 3 migrate Interaction/Puzzle, 4 migrate Save, 5 guard test + ADR + docs.
