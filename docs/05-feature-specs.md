# 05 — Feature Specifications

Status legend: TODO · IN_PROGRESS · BLOCKED · DONE (DONE only after acceptance criteria are tested).
**Summary 2026-10-08:** Windows-side work is done and tested for F-00 to F-10, F-13 (Level_B1) and X-04 (last run EditMode 279, PlayMode 167, not re-run after the liminal redesign was removed). Nothing is checked on a phone. Open: Android module and device checks, F-10 listening/visual review, F-13 touch HUD and level Settings button, F-08 flash-rate check, F-11 (owner decision D4), F-12 after G3, X-01..X-03 owner actions. The F-13 liminal framework spec was removed with the redesign (2026-10-07). Checklist: plan.md.

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

### F-02b Touch controls on the level HUD — IN_PROGRESS (built and tested with simulated pointers: EditMode 227/227, PlayMode 128/128; screenshots reviewed at 3 sizes. NOT tested with real fingers)
- **Why:** F-13 left the level unplayable on a phone: the F-02 widgets exist but no HUD uses them.
- **Scope:** a generated `TouchControls` group in the level HUD: floating move stick (left half), look drag area (right half), Interact, Sprint (held) and Crouch (held) buttons; shown only on touch devices; scaled by the Touch control size setting (80-130%); inside the safe area; behind the pause/journal buttons and all modal screens. **Excl.:** relocating or rebinding controls, gyro look, haptics.
- **Deps:** F-02 widgets, F-07 settings (`ControlsLayout.Scale`), F-13 level HUD.
- **Design:** `TouchControlsVisibility.ShouldShow(isMobile, hasTouchscreen)` is a plain rule that is unit-tested; `TouchControlsScaler` applies `ControlsLayout.Scale` to the button group; the builder sets the widgets' private fields through `SerializedObject` so no widget code changes.
- **AC:**
  1. Move, look and a button can be held at the same time with different pointer ids and all three register (PlayMode, simulated pointer events).
  2. Interact button triggers the focused interactable; Sprint and Crouch are held while pressed and released on lift or disable.
  3. Controls are hidden on Windows without a touchscreen and shown on mobile or when a touchscreen exists.
  4. Changing the control size setting rescales the buttons within 80-130%.
  5. Controls do not overlap the Pause and Journal buttons or the safe-area edge in screenshots at 2400x1080, 1920x1080 and 1280x720.
  6. A modal screen (keypad, note, journal, pause) blocks touches to the controls underneath.
- **Test:** EditMode (visibility rule), PlayMode on the real level scene, screenshots. Not testable here: real finger feel, accidental touches, thumb reach (needs a phone, D13).
- **Sub-steps (one commit per file):** spec, `TouchControlsVisibility` + tests, `TouchControlsScaler`, builder, level builder wiring, scene, PlayMode tests, screenshots, docs.
- **Results (2026-10-04):** AC1-AC4 and AC6 pass in PlayMode on the real level scene using simulated pointer events (stick, look and Sprint held together; Interact is a one-frame press; Crouch released when the controls hide; size setting clamps to 80-130%; an open keypad catches the touch first). AC3 is covered by the visibility rule test and an override, not by running on a device. AC5: screenshots at 2400x1080, 1920x1080 and 1280x720 show no overlap with Pause, Journal or the caption strip.
- **Open:** real thumb reach, accidental touches, the fixed ring versus the floating stick origin, and whether Sprint should be a toggle all need a phone (D13). The Pause button is a corner button, not a TouchButton, so Android Back is still unchecked.


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

## F-13 Level "Night Shift: Floor B1" (greybox, playable end to end) — IN_PROGRESS (playable and tested on Windows-side checks: EditMode 223/223, PlayMode 122/122, Windows x64 build OK. OPEN: touch controls are not on the level HUD, nothing checked on a phone, Settings button hidden in the level pause menu, see Results)
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

- **Results (2026-10-04):**
  - AC1 menu to level: Continue/New Game route to `Level_B1` with the right launch mode (PlayMode, real MainMenu scene, scene loader observed not executed). Boot scene now opens the menu. Resume restores checkpoint, inventory, solved lock; tested by calling `Begin(resume)` on the real level scene, not by a full scene reload.
  - AC2 puzzle: scripted full run passes (notes, stamp, Records Office door, wrong then right code read from the in-world props, dock door, exit). A test also proves the code equals the three props read in order and that no note contains a digit.
  - AC3 exit gating: walking into a locked door is blocked, the opened door lets the player through, and the exit trigger does nothing before the lock is solved. Only the dock door's collision was walked; other doorways were not.
  - AC5 pause, journal, keypad, note reader: open/close tested, no stacking, input gate released.
  - AC6 checkpoints save on entry (hall, records, dock); the entrance does not save on a fresh start (it is index 0). Resuming inside the Records Office keeps its door unlocked and the used stamp gone (found while designing: it would have been a softlock).
  - AC7 guard test covers the new Level folder; the runner fails tests on error logs, so no missing-key errors occurred in the runs.
  - AC8 screenshots reviewed at 1920x1080 and 1280x720 (10 viewpoints, HUD on top). Clue text is small from across a room; legible up close.
- **Found and fixed during this feature:**
  - The project had no URP pipeline asset assigned, so URP materials rendered magenta. Added `RenderPipelineSetup` and two assigned assets (mobile for quality levels 0-2, PC for the rest).
  - The Boot scene was empty (a build would have started on a blank screen). Added `BootLoader`; Greybox is now disabled in Build Settings.
  - Doors swung into the player and shoved them aside; they now swing away and the doorway is 1.5 m.
  - Journal binding and keypad subscription made at build time are not saved with a scene; moved to runtime in `LevelHud`.
- **Open:**
  - Touch controls (virtual stick, look area, buttons) are not built into the level HUD (F-02 prefab not built), so the level cannot be played on a phone yet.
  - The pause menu Settings button is hidden in the level (no settings screen there yet).
  - Android Back, device performance and touch feel are unchecked; there is no Android build.
  - Item and note prose is placeholder text for owner review. Lighting and look are greybox only (F-10).
  - Process note: `ProgressGate.cs` was committed inside the asmdef commit after a subject-length rejection, and the data and material assets were committed as folders, not one file each.

## F-09 Tension director & horror events — IN_PROGRESS (split: a = director logic, b = events and scene wiring)
- **Accessibility requirement (from F-08):** every flashing or strobing light/screen effect must go through `FlashBudget` and `SafeFlicker`, and every camera shake/sway must be multiplied by `Accessibility.MotionScale`. Tests must prove no more than 3 state changes per second with Reduce Flicker on.
- **Scope:** `TensionDirector`, `HorrorEvent` SO, at least 6 event types (light flicker, door slam, prop shift, audio cue, Misfile corridor reveal, shadow figure). **Excl.:** procedural events.
- **AC:** min gap between scare events at least the configured value (default 45 s) verified by log; events fire once when flagged; events never fire during note reading/pause/menus; event data editable without code; debug overlay in dev builds.
- **Test:** EditMode simulation of director over time; manual pacing playtest.

### F-09a Tension director and event picker (logic only) — DONE (EditMode 247/247 incl. 24 new: 30-minute simulation keeps every gap at 45 s or more, safe zone drains and never fires, blocked time changes nothing and does not count towards the gap, one-shots and cooldowns honoured, story mode halves rise and doubles gap, bad config throws. Pure logic, no device-dependent criteria. Real events and the pacing playtest are F-09b)
- **Scope:** plain C# `TensionDirector` (0-1 tension that rises in unsafe zones and decays in safe ones, throttles events by a minimum gap) and `EventPicker` (chooses which authored event may fire now). **Excl.:** the event actions, `HorrorEvent` assets, triggers and scene wiring (F-09b), debug overlay.
- **Design:** the caller passes elapsed time, whether the player is in an unsafe zone, and whether gameplay is blocked (pause, modal screen, menu). Blocked time changes nothing: no rise, no decay, no gap progress. `TryTakeEvent` fires only when tension is at or above the threshold and the gap since the last event has passed, then lowers tension by a relief amount. Story mode halves the rise rate and doubles the gap. Settings are validated (rates and gap must be positive, threshold in 0-1) and a bad config throws. `EventPicker` takes candidates (id, once, cooldown, minimum tension) and skips fired one-shots, events on cooldown and events above current tension; among eligible ones it picks the one with the highest minimum tension, ties by list order, so results are deterministic and testable.
- **AC:**
  1. Over a simulated 30-minute run in an unsafe zone, no two events are closer than the minimum gap (default 45 s).
  2. In a safe zone tension falls to 0 and no event fires.
  3. No change at all while blocked, however long the block lasts.
  4. A one-shot event never repeats; a cooldown event waits its cooldown; an event needing more tension than present is not picked.
  5. Story mode halves the rise rate and doubles the minimum gap.
  6. Invalid settings throw; tension always stays within 0-1.
- **Test:** EditMode only (pure logic, simulated time).
- **Also added:** `EventPicker` can snapshot and restore which one-shot events already fired, for saving (hooked up in F-09b). `Reset()` clears tension and the gap for a respawn.
- **Sub-steps (one commit per file):** spec, `TensionSettings` + `TensionDirector` + tests, `EventPicker` + tests, docs.

### F-09b Horror events and scene wiring — DONE (2026-10-06: EditMode 253/253, PlayMode 148/148 in 52 s incl. 20 Horror tests on the real Level_B1; AC1 EditMode, AC2-5 and AC7 PlayMode, AC6 by the one-hour simulated-play test, AC8 by code check: overlay body is inside `#if UNITY_EDITOR || DEVELOPMENT_BUILD`. Not verified: pacing feel and audio, which need a person and F-10. A cross-fixture PlayMode hang was a test-teardown problem (scene left alive), fixed in the test file. Code review then found four bugs, all fixed with tests: new game kept fired one-shots, props and reveals not reset on restart, door speed/figure not restored when a spot is disabled mid-effect, a busy spot consumed a one-shot)
- **Scope:** six event kinds wired into Level_B1 and driven by the F-09a director: light flicker, door slam, prop shift, audio cue (caption plus a clip if one is assigned), Misfile corridor reveal (a hidden corridor section appears), shadow figure (a dark figure shows briefly then vanishes). Data in `HorrorEventAsset` (ScriptableObject); scene targets in `HorrorEventSpot`; `HorrorEventRunner` ticks the director, picks and plays events; `SafeZone` volumes (break room, records office) count as safe; camera shake component scaled by `Accessibility.MotionScale`; fired one-shot ids saved in the save file (additive field, version stays 1); debug overlay in editor and development builds only. **Excl.:** real audio clips and final art (F-10), the Indexer (F-11), procedural events.
- **Design:** `LightFlickerEffect` is plain C#: it uses `SafeFlicker` for the waveform and asks a `FlashBudget` before every lit-to-dim change, so even with Reduce Flicker off no more than 3 changes happen in any second; with it on the waveform is the slow shallow fade. The runner treats the game as blocked when `ModalGate` is open or time is frozen. Story mode is passed from settings by `LevelBootstrap`. A bad event asset (empty id, duplicate id) is reported and skipped, never silently half-working.
- **AC:**
  1. Flicker: never more than 3 lit-to-dim changes in any rolling second, at 60 fps over 10 s, with Reduce Flicker off and on; reduced mode at most 25 percent deep (EditMode simulation).
  2. All six kinds are fired through the runner in PlayMode on the real level and their effect is observed (light intensity changes then restores, door closes, caption posted, prop moved, corridor enabled, figure appears then hides).
  3. No event fires while a modal screen is open or the game is paused, however much tension has built up.
  4. Camera shake amplitude is zero when Reduce camera motion is on.
  5. A one-shot event that fired is not fired again after saving, leaving and Continuing.
  6. Spacing: with the real level wired, simulated play never produces two events closer than the configured gap.
  7. Event data can be changed by editing assets, not code; a duplicate id is reported.
  8. The debug overlay is not compiled into release builds.
- **Test:** EditMode (flicker effect), PlayMode on Level_B1 with the runner advanced by simulated time. Manual pacing playtest and how scary or fair it feels need a person and, for audio, F-10.
- **Sub-steps (one commit per file):** spec, `LightFlickerEffect` + tests, `HorrorEventAsset`, camera shake, `Door.Close`, `SafeZone`, `HorrorEventSpot`, `HorrorEventRunner`, save field and SaveGame hook, debug overlay, builder wiring, scene, PlayMode tests, docs.

## F-10 Lighting, audio & level art pass — IN_PROGRESS (2026-10-06: code and tests done, EditMode 279/279, PlayMode 167/167; AC1-AC8 pass. 31 CC0 clips (Kenney Impact Sounds, Kenney RPG Audio, OpenGameArt Ambience Pack 1) assigned to 21 cues and registered in doc 09. Stand-ins until better audio: event.whisper uses a creak, event.light_buzz a metal plate hit; ambience beds are sci-fi horror loops reused for the drone. NOT DONE: AC9 device profiling (no device), and how it sounds and looks to a person (nobody has listened yet). Not marked DONE until those are)
- **Scope:** (a) audio: ambience beds per zone, footsteps by surface, music drone that follows tension, event stingers hooked into the six F-09b events, code-driven buses (Music, Ambience, Sfx) with ducking; (b) lighting: baked lighting for Level_B1, at most 2 realtime lights, post-FX (vignette/grain) tuned for mobile; (c) performance: occlusion culling and LODs only where the profile shows a need. **Excl.:** Indexer sounds (F-11), voice, localised audio, new levels, real-device measurement until a device exists (sprint plan, Day 4).
- **Accessibility (from F-08):** every gameplay-relevant sound has a caption key in `DefaultStrings` and posts it through `Captions.Post`; flicker uses `SafeFlicker`; camera motion uses `Accessibility.MotionScale`. Ambience beds, the music drone and the player's own footsteps are not gameplay-relevant and need no caption. Volume sliders (Music, SFX) exist in settings and drive the buses.
- **Cue list (fixed 2026-10-06):** a cue is gameplay-relevant when it tells the player something they did not cause. Those need a caption key.
  | Cue id | When | Gameplay-relevant | Caption key |
  |---|---|---|---|
  | `event.light_buzz`, `event.door_slam`, `event.prop_shift`, `event.whisper`, `event.misfile`, `event.figure` | the six F-09b events | yes | same as the id (all exist) |
  | `checkpoint.chime` | checkpoint saved | yes | `level.checkpoint_saved` (exists) |
  | `door.open`, `door.close`, `door.locked` | player uses a door | no (player-caused; locked already shows `door.message.locked`) | none |
  | `pickup.take`, `note.open` | player action | no | none |
  | `footstep.<surface>` | player walks (surfaces: tile, carpet, concrete, to be confirmed against Level_B1) | no | none |
  | `amb.<zone>` | zones: hall, stacks, records, break_room, dock | no | none |
  | `music.drone` | follows tension | no | none |
  The `door_slam` clip is the same as `door.close` played louder, not a separate asset. Ambience and music are looped beds.
- **Design:** a `CueCatalog` ScriptableObject maps a cue id to clips, bus (Music, Ambience, Sfx), caption key (empty only for non-gameplay cues) and volume range. `AudioBus` (plain C#) holds the Music, Ambience and Sfx slider gains and a duck factor; `AudioDirector` plays cues by id (3D or 2D) from a pool of 16 voices and applies bus gain to every playing voice. Changed 2026-10-06: no `AudioMixer` asset, because a mixer cannot be authored reliably from code and would be untestable here; the bus is the single place a mixer could be wired in later. A footstep component reads the surface under the player (physic material or tag) and picks a clip set. The music drone volume follows `TensionDirector.Tension`. Ducking lowers music and ambience while a stinger or caption-worthy cue plays. The horror assembly cannot reference Audio (Audio references Horror for the music drone), so events call `Cues.Play` (a small interface in Core that `AudioDirector` registers with); by convention an event's caption key is also its cue id, and the spot still posts the caption itself (posting the same key twice only refreshes it). A missing clip logs one warning (not an error: clips arrive later than code, and errors would fail every scene test meanwhile) and the cue still posts its caption (same rule as F-09b's audio cue).
- **Budgets:** at most 2 realtime lights, rest baked; at most 16 simultaneous voices; audio memory counts against peak RAM in doc 06 (Low <= 1.2 GB). Set 2026-10-06, to be revised from measurements: all audio files together at most 60 MB compressed (Vorbis) of the 200 MB base size; at most 80 MB of audio in RAM (short SFX decompressed on load, ambience and music streamed); no single clip over 3 MB except streamed beds.
- **AC:**
  1. Every cue in the catalog with a gameplay-relevant flag has a non-empty caption key that exists in `DefaultStrings` (EditMode; the test fails if one lacks it).
  2. Each of the six F-09b events plays its clip through the director when fired in PlayMode (clip played, correct bus) and still posts its caption with the clip missing.
  3. Footsteps: stepping on each authored surface selects that surface's clip set; an unknown surface falls back to default (PlayMode).
  4. Music drone volume rises with tension and falls with it, within the configured range; silent when blocked or paused (EditMode on the mapping, PlayMode for pause).
  5. Ducking: while a stinger plays, the Music and Ambience bus gain is lowered by the configured amount and restored after (EditMode on `AudioBus`; PlayMode that the director ducks while a gameplay cue plays and applies the gain to playing voices).
  6. Music and SFX sliders change the bus gains and persist with settings (PlayMode).
  7. Level_B1 has at most 2 realtime lights and baked lightmaps (editor check test), and no light-leak seams on a screenshot review at 5 listed viewpoints. Done 2026-10-06: the two lights a horror event flickers (Light_Hall, Light_Stacks) stay realtime, the other three are baked (CPU lightmapper, 3 lightmaps, about 1.8 MB, committed before Git LFS exists: owner action Day 4); screenshots of hall, stacks, records keypad, dock door and calendar lights at 1920x1080 show no seams, but there are no pre-bake shots to compare the overall look against.
  8. Asset register (doc 09): every audio file and every third-party asset used has a row with status CLEARED or ORIGINAL; a build check fails on any REQUIRES_REVIEW row used in a shipped scene.
  9. Performance (doc 06 targets): profiler capture on the reference devices. **Not verifiable until a device exists;** until then only an Editor capture is recorded and marked as such.
- **Test:** EditMode (caption rule, tension mapping, register audit), PlayMode on Level_B1 (events, footsteps, ducking, sliders), manual screenshot review, device profiler capture later. How scary it sounds needs a person.
- **Decisions (owner, 2026-10-06):** audio comes from free CC0 sources now (each file gets a CLEARED register row with source URL); commissioned audio replaces it in a later scope. Free CC0 audio may ship in the closed test.
- **Sub-steps (one commit per file):** spec (this commit), cue list and budgets in the spec, `CueCatalog`, `AudioBus`, `AudioDirector`, footstep surfaces, music drone mapping, event clip hookup in `HorrorEventSpot`, settings sliders, lighting bake and light-count check, asset register rows, tests, docs.

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

## F-14 The Stilt-Walker: hearing entity, memory and 10-room expansion — TODO (spec written 2026-10-08, no code; same game, extends F-09, F-10, F-11, F-13)
- **Source:** owner prompt 2026-10-08 (hearing-based hunter with a wooden leg, 5-10 rooms, 2 lives, entity memory, dynamic heartbeat/audio, haptics, final escape). Not a separate game: it builds on Level_B1, `Horror`, `Audio`, `Save`, `Settings`, `Player`.
- **Decisions taken (2026-10-08, owner to review):** (1) The Indexer of F-11 is replaced by this entity; D4 becomes "yes". (2) Level_B1 stays as Room 1 and the tutorial; rooms 2-10 are added as scenes or zones by editor builders, greybox first. (3) "2 lives" counts per run; death restores the last checkpoint and keeps puzzle progress; the entity memory persists in the save. (4) No gore; Story mode halves detection ranges (existing rule). (5) Audio stays CC0 from doc 09; each new clip gets a register row. (6) Rooms ship one per sub-feature step, in order.
- **Exclusions:** iOS, multiplayer, real microphone input, procedural level generation, device profiling (needs an Android device).
- **Fairness rules (all sub-features):** the entity never knows the player position without a sound event, never teleports, never ignores walls, never knows which hide spot is used; memory only changes probabilities (patrol, investigation, search, suspicion), never speed beyond the per-room table. Every death is explainable from a logged `DeathCause`.

### F-14 priority and sequencing (revised 2026-10-08, owner request: F-15 RAG/LangChain/LangGraph runs in parallel)
- **Status today (verified by search, not by a new test run):** one room only (Level_B1). a done; b, c, d, e partly done; f-l not built. F-14g exists as one UNVERIFIED wip commit on `feature/F-14g-lives`.
- **Rule 1, offline first:** the game must be fully playable without the F-15 server. F-14 owns the deterministic truth (`EntityMemory`, `DangerLevel`, room and door state). F-15 only suggests: it may bias probabilities through `StrategyExecutor`, never replaces F-14 logic, and a missing server changes nothing.
- **Rule 2, boundary for memory:** F-14i `EntityMemory` is local and saved with the game. F-15g RAG stores richer summaries on the server and may add a rare caption line. If they disagree, F-14i wins.
- **Rule 3, shared hooks first:** F-15 integration is blocked by F-14g (death events, `StalkerAgent` edits) and F-14d emitters (noise events). Build those before more rooms.
- **Build order (each step: EditMode, then PlayMode, one file per commit):**
  1. **F-14g** lives, checkpoints, death: split the wip commit per file, test (unblocks F-15 `GameEvent` death/hide emitters).
  2. **F-14d** finish scene emitters (footsteps, doors, drops, throws): unblocks hearing and F-15 behaviour tracking.
  3. **F-15 integration**: place `StrategyRunner`, publish events, `StalkerAgent` reads `HideBonus` and `InvestigationMultiplier`, `HorrorEventRunner` uses `HorrorSuggestionGate`, live uvicorn + HTTP run, real LLM key test.
  4. **F-14h** heartbeat and `DangerLevel` (also feeds F-15 director context).
  5. **F-14i** `EntityMemory` (needs deaths from step 1; sets the F-15 boundary above).
  6. **F-14f** room template and `RoomDefinition` validator (exactly one progression door, no softlock), then rooms in two milestones: **M1 rooms 2-5** (includes the room 5 key for room 7), **M2 rooms 6-10**. One room per step; backtracking changes (moved chair, dead light, new sound) are part of room 5 onward.
  7. **F-14j** scares, **F-14k** haptics, **F-14l** final escape (`CONGRATULATIONS! You made it out.` plus stats).
  8. **F-14c** open art items (rig, clips, captions) run alongside as audio and art become available.
- **Gaps from the owner prompt now explicit in the spec:** per-room escalation (rooms 1-10 table lives in F-14f), several doors per room with a varying correct door, noisy floors, throwing as a distraction (F-14d), entity sighting then vanishing (F-14j), 2 lives per run (F-14g), completion stats (F-14l).
- **Not claimed:** none of this is verified by a person playing. Rooms beyond B1, lives, heartbeat, memory, scares, haptics and ending remain TODO.

### F-14a Noise model and hearing (plain C#, EditMode) — DONE (2026-10-08: HearingModelTests 7/7, AC1-6 pass; SoundMemory folded into HearingModel as MarkHandled)
- **Scope:** `NoiseEvent {position, loudness, time, kind}`, `NoiseTable` (crouch 2 m, walk 6, run 14, door slam 20, dropped object 12, thrown object impact 16, machinery/alarm 30; Story x0.5), `HearingModel` (score = loudness / distance, decays with age, reduced by walls), `SoundMemory` (best candidate location).
- **AC:** 1. Run is heard farther than walk; crouch is quietest. 2. A sound outside its radius is never heard. 3. Each wall between halves the effective radius. 4. Older events decay and expire. 5. The strongest recent event wins. 6. Story mode halves radii.
- **Test:** EditMode, deterministic.

### F-14b Entity AI states (plain C# machine + NavMesh agent) — IN_PROGRESS (2026-10-08: brain 10/10 EditMode; StalkerAgent PlayMode 5/5 on an arena and 5/5 on the real Level_B1 (placed, on NavMesh, patrols, hears noise); full PlayMode 177/177. Open: AC3 hide-spot search (F-14e), AC6 escape routes (F-14f), how it feels to play: UNVERIFIED by a person)
- **Scope:** states Patrol, Listen, Investigate, Chase, Search, Cooldown, plus Watch (stand and look without attacking). Transitions only through `HearingModel`, or line of sight under 6 m in light. Chase speed 1.15x player walk, 0.9x player run; chase ends after 20 s without sound or sight.
- **AC:** 1. Hears a sound, stops, turns, walks to it, searches 15 s, returns to patrol. 2. Cannot see through walls or closed doors. 3. Search inspects nearby hide spots with a probability per spot. 4. Watch and ignore outcomes occur and never lead to a kill without a new trigger. 5. No state skips Listen. 6. A valid escape route exists per room (script).
- **Test:** EditMode machine tests; PlayMode on the real level with a scripted noisy and a scripted quiet player.

### F-14c Wooden-leg body, animation and footsteps — IN_PROGRESS (2026-10-08: rhythm logic done, WoodenLegRhythmTests 5/5, AC1-3 logic pass; open: rig and animation, audio clips and cue registration, silence in Listen/Watch, captions)
- **Scope:** 2.3-2.7 m greybox rig (placeholder parts until art), limp cycle, alternating `normal` and `wood` footsteps with 4 variants each, distance mixing (far: quiet and filtered; near: loud with low-frequency), AI-driven silence, breathing layer under 5 m.
- **AC:** 1. Rhythm alternates two different clips. 2. Volume and filter change across 3 distance bands. 3. No variant repeats twice in a row. 4. Silence happens only in Listen or Watch. 5. Every cue has a caption.

### F-14d Player noise generation — IN_PROGRESS (2026-10-08: footsteps, doors, drops and thrown bottles emit in Level_B1; ImpactNoiseRuleTests + ThrowInputTests 7/7, BottleThrowTests 5/5; full run EditMode 379/379, PlayMode 194/194. AC1-7 pass in tests. Open: touch Throw button not placed on the HUD, throw feel and bottle placement UNVERIFIED by a person, interaction noise for puzzles comes with F-14f)
- **Scope:** player emits `NoiseEvent`s from walk, run, crouch, doors, drops, interactions, noisy floors and thrown objects.
- **AC:** 1. Each action emits the right kind and loudness. 2. A thrown bottle makes an impact noise at the landing point. 3. A slammed door is louder than a gently closed one.
- **Already done:** footsteps (`PlayerNoiseEmitter`) and doors (`DoorNoise`) emit in Level_B1 (StalkerBuilder).
- **Remaining scope (2026-10-08):** a throwable `Bottle` (pick up with interact, hold one, throw with a new Throw action: G, gamepad West, touch button) and drop/impact noise from any `NoisyProp`. `ImpactNoiseRule` (plain C#) maps impact speed to `Drop` (12 m) or `ThrownImpact` (16 m) and ignores taps under 1.5 m/s. One impact noise per 0.5 s per prop. Exclusions: noisy floor zones beyond the existing surface scale, interaction noise for puzzles (comes with F-14f), throwing arc preview, touch button art.
- **AC (added):** 4. Impact speed under 1.5 m/s makes no noise; a fall makes Drop, a throw makes ThrownImpact. 5. A thrown bottle landing in a room makes the stalker leave Patrol and walk to the landing point (PlayMode on Level_B1). 6. Throw with nothing held does nothing and makes no noise. 7. The input state carries ThrowPressed for one frame on keyboard, gamepad and touch aggregation.
- **Sub-steps (one commit per file):** 1 spec, 2 `ImpactNoiseRule` + tests, 3 `PlayerInputState.ThrowPressed` + keyboard/gamepad binding + aggregator + tests, 4 `NoisyProp` + `Bottle`, 5 `PlayerThrower`, 6 builder places 3 bottles and the thrower, 7 PlayMode tests, 8 docs.

### F-14e Hiding system — IN_PROGRESS (2026-10-08: HideSpot, 3 cupboards in Level_B1 (safety .8, .3, .55), entity inspects spots while searching. HideInspectionTests 6/6 + HideSpotTests 7/7; full run EditMode 312/312, PlayMode 184/184; AC1-4 pass. Open: how it feels to a person (UNVERIFIED), other furniture types, touch exit button on phone)
- **Scope:** `HideSpot` (cupboard, under bed or table, curtain, closet; safety 0-1; noise on enter and exit). Entering during Chase only out of the entity's line of sight. Entity inspection chance from safety, entry noise and memory.
- **AC:** 1. Spots differ in safety. 2. A noisy entry raises inspection chance. 3. Inspected and found means death; inspected and not found means it leaves. 4. The player can always exit.

### F-14f Rooms, doors, keys and puzzles (rooms 2-10) — TODO
- **Scope:** per-room `RoomDefinition` data: 4-6 doors (one progression door; others locked, fake, blocked, key), clues, hide spots, noise props, difficulty table. Rooms: 1 intro (B1), 2 first threat, 3 psychological, 4 multi-key, 5 backtracking, 6 combination and switch puzzles, 7 major threat (all doors locked until the key from room 5), 8 interconnected, 9 final trials, 10 final escape. Carry-over items, fuse/power, combination lock, hidden switch; room changes on return (moved chair, dead light, new sound).
- **AC:** 1. Each room validates: exactly one progression door, no softlock (script). 2. The room 5 key opens room 7. 3. Difficulty values rise monotonically. 4. Return changes happen once and are saved.
- **Note:** one commit series and one PlayMode run per room; art stays greybox.

### F-14g Lives, checkpoints and death sequence — IN_PROGRESS (2026-10-08: RunLives, DeathSequence, DeathController in Level_B1; RunLivesTests + DeathSequenceTests 13/13, DeathFlowTests 5/5; full run EditMode 372/372, PlayMode 189/189. AC1-4 pass in tests. Open: pause-screen lives tally UI, how the sequence feels to a person (UNVERIFIED), room 6 case needs F-14f)
- **Scope:** 2 lives per run; checkpoints after rooms 1, 3, 5, 7; death sequence (about 4 s); `DeathMemory` saved; lives shown without a HUD bar (pause screen tally).
- **AC:** 1. Death in room 6 restarts at the last checkpoint with solved puzzles kept. 2. A second death ends the run. 3. The sequence lasts at most 5 s. 4. Save and load keep lives and memory.

### F-14h Dynamic heartbeat, breathing and mixing — TODO
- **Scope:** `DangerLevel` 0-4 from entity distance and state; gains per level in a data table; hide-mode mix; screen pulse, vignette and shake limited by the existing reduce-motion and flicker settings.
- **AC:** 1. Gains follow the table at each level. 2. Reduce motion removes shake and pulse. 3. Hide mode lowers ambience to the table value. 4. Transitions smooth within 1 s.

### F-14i Entity memory and adaptive AI — TODO
- **Scope:** `EntityMemory` (death rooms and spots, preferred hide spots, routes, run and throw habits), levels 0-5 from recorded events, effects limited to probabilities, rare captioned lines ("Again?", "I remember.", "You again."; at most one per 10 minutes, optional in settings).
- **AC:** 1. Snapshot and restore are exact. 2. Levels rise only through recorded events. 3. A hide spot used at a past death is inspected more often but not always. 4. Memory never changes speed or reveals the player position. 5. Lines respect the rate limit.

### F-14j Scares and false presence — TODO
- **Scope:** extends `HorrorEventSpot`: toy box set (8 boxes, 1 key, 1 spring toy, 1 footstep beat), mannequin, light off, self-opening door, whisper behind, fake wooden knocks, one-second corridor sighting. Rate limited by the tension director; about 40% harmless, 20% entity-linked.
- **AC:** 1. Scare gap stays at 45 s or more (existing test). 2. False footsteps never match the real rhythm. 3. The box set always leaves a reachable key.

### F-14k Haptics — TODO
- **Scope:** gamepad rumble and mobile vibration on footsteps, heartbeat, hit and scares; settings toggle and strength.
- **AC:** 1. A pulse follows the footstep event. 2. The disabled setting sends nothing. Feel is UNTESTABLE here (needs hardware).

### F-14l Final escape and ending — TODO
- **Scope:** silence beat, chase to exit, door slam, "YOU ESCAPED" screen with time, deaths, lives, rooms and secrets; closing shot of two eyes in an upper window.
- **AC:** 1. Winnable by a valid scripted route. 2. Stats match the run. 3. No input stays blocked after the end.
- **Order:** a, b, c, d, e, f (room by room), g, h, i, j, k, l. Each: EditMode first, PlayMode second, one file per commit.

## F-15 Strategic AI layer (Python server: LangGraph + LangChain + RAG + LLM) — IN_PROGRESS (UNVERIFIED as a whole; a-i built and unit-tested; open: scene wiring, emitters, StalkerAgent and HorrorEventRunner use, real LLM and HTTP run, play feel)
**Principle:** Unity's real-time AI (F-14a-e: HearingModel, EntityBrain, StalkerAgent, HideSpot) owns every frame. The strategic layer only answers "what should the entity try?" via a validated, time-limited `StrategyCommand` that biases probabilities and utility weights. The game must run identically without the server. The existing `TensionDirector` and `HorrorEventRunner` (F-09) stay the Horror Director; F-15 only feeds them. F-15 absorbs F-14i (entity memory, adaptive AI, rare lines).
- **Exclusions:** the LLM never sets transforms, targets, speed, or positions; never sees exact hide spot; no RAG for deterministic state (doors, keys); no shipping of API keys; no cloud dependency required to play; no new art or audio.
- **Layout:** Unity `Assets/_Project/Scripts/Strategy/` (StrategyCommand, StrategyValidator, StrategyExecutor, PlayerBehaviorTracker, GameEventBus, StrategyClient, StrategyDebugPanel). Python `Server/` (main.py, api/, agents/, tools/, memory/, models/, tests/).
- **Strategies (closed enum):** NONE, INCREASE_HIDING_PRESSURE, INCREASE_INVESTIGATION, CHANGE_PATROL_PREFERENCE, CHANGE_SEARCH_PRIORITY, RELAX_PRESSURE. Limits: duration 10-180 s, intensity 0-1, priority 0-1; at most one active; effects capped (spot inspect chance never 0 or 1, max +0.5).
- **Memory split:** SQLite = exact state (deaths, spot/route stats, behavior stats). Chroma (local, persistent, no server) = semantic summaries with importance, room, kind, created_at, expiry.
- **LLM cadence:** only on triggers (new room, death, behavior shift over threshold, encounter ended, strategy expired) with 60 s cooldown and cache by profile hash; timeout 2 s; failure keeps the current strategy, else the deterministic rule planner.

### Sub-steps (one commit per file, EditMode/pytest first)
- **F-15a Command contract (Unity) — DONE 2026-10-08 (StrategyExecutorTests 14/14 EditMode, filtered run):** StrategyCommand, StrategyValidator, StrategyExecutor (expiry, apply to inspect chance and investigation weight). AC: 1 invalid enum, out-of-range, or expired command rejected. 2 valid command raises inspect chance but never to 0 or 1. 3 expiry restores baseline. 4 executor never touches transform or speed. Test: EditMode.
- **F-15b PlayerBehaviorTracker + GameEventBus (Unity) — DONE 2026-10-08 (PlayerBehaviorTrackerTests 7/7 EditMode; not yet wired to gameplay emitters, so UNVERIFIED in a real session):** counts run/walk/crouch ratio, noise, hide usage per spot type, routes, deaths, chase duration, risk tolerance. AC: 1 ratios sum to 1. 2 profile snapshot is deterministic for a scripted session. 3 profile contains no positions or hide-spot ids. Test: EditMode.
- **F-15c StrategyClient (Unity) — DONE 2026-10-08 (StrategyClientTests 13/13 EditMode, fake transport; real HTTP transport comes with F-15d):** async HTTP, request ids, 2 s timeout, 1 retry, offline = no-op, debug state. AC: 1 timeout leaves the active strategy unchanged. 2 bad JSON rejected. 3 never called from Update. Test: EditMode with fake transport.
- **F-15d Server skeleton (Python) — DONE 2026-10-08 (pytest 10/10; Python 3.14.7, fastapi 0.142, pydantic 2.13, langgraph 1.2, langchain-core 1.6, chromadb 1.5 all install; no real HTTP transport in Unity yet):** FastAPI, Pydantic models, SQLite store, `/health`, `/events`, `/profile`, `/strategy`. AC: 1 schema rejects unknown strategy. 2 events persist. 3 request ids logged. Test: pytest.
- **F-15e LangGraph graph — DONE 2026-10-08 (pytest 24/24 total, 14 graph tests; rules only, no LLM; InMemorySaver per session, restart loses cooldown and history):** OBSERVE, ANALYZE, RETRIEVE_MEMORY, PLAN, SELECT_STRATEGY, MONITOR, ADAPT, with cooldown and failure edges; deterministic rule planner as default node. AC: 1 each transition covered. 2 cooldown blocks a second call. 3 failure falls back to rules. Test: pytest with no LLM.
- **F-15f LangChain tools + LLM — DONE 2026-10-08 with a FAKE model only (pytest 34/34 total, 10 in test_llm; no real provider call made, prompt quality UNVERIFIED; enable with STRATEGY_LLM="provider:model"):** tool functions (behavior, deaths, hiding stats, room stats, available events), `with_structured_output` on the command schema, provider behind env var (default off = rules). AC: 1 invalid LLM output rejected, fallback used. 2 tools return only the allowed fields. Test: pytest with a fake chat model.
- **F-15g RAG memory — DONE 2026-10-08 (pytest 48/48 total, 12 in test_memory; retrieval uses a hashed bag-of-words embedder, not a semantic model, so relevance on real data is UNVERIFIED; plug a real embedder via memory/embeddings.py):** event to memory, importance score, embedding (local), Chroma, metadata filters, top-K 3, summary and expiry. AC: 1 retrieval returns the relevant memory. 2 expired memory excluded. 3 vector store failure still returns a strategy. Test: pytest.
- **F-15h Director link + rare lines — DONE 2026-10-08 (pytest 71/71 total, 23 in test_horror; HorrorSuggestionGateTests 7/7 EditMode; the gate is NOT yet connected to HorrorEventRunner or its event assets, and pacing feel is UNVERIFIED):** HorrorDirectorClient sends tension inputs, receives event suggestion validated against the F-09 catalogue; contextual lines rate-limited (1 per 10 min). AC: 1 unknown event ignored. 2 rate limit holds. 3 "do nothing" is a valid result. Test: EditMode + pytest.
- **F-15i Debug panel + fairness/perf tests — DONE (code and tests) 2026-10-08 (EditMode 359/359, PlayMode 184/184, pytest 75/75; shared JSON contract fixtures; StrategyRunner exists but is not placed in a scene; AC3 'server killed' covered by offline and failure unit tests, not a live kill):** panel shows real-time state, strategy, remaining time, confidence, reason, LLM online/offline. AC: 1 no strategy value exceeds caps. 2 game loop cost with client enabled unchanged within noise (PlayMode). 3 full flow runs with server killed. Test: EditMode, PlayMode, scripted kill.
- **F-15j Level_B1 integration — IN_PROGRESS (2026-10-08: built; StrategyIntegrationTests 5/5; live run against uvicorn + real OpenAI key reached ServerState.Online in 1.5 s, 1 request, command NONE for a fresh profile; full run EditMode 379/379, PlayMode 199 passed + 1 skipped (live test skips when the server is down). Open: HorrorEventRunner link (F-15k), PatrolBias/SearchBias unused, whether the LLM changes play is UNVERIFIED by a person):** wires F-15 into the playable level; the game stays fully playable with no server (Online defaults to false).
  - **Scope:** `IStrategyModifiers` (Entity, HideBonus and InvestigationMultiplier only); `StalkerAgent.Modifiers` feeds `HideSpot.Inspect(memoryBonus)` and an `EntityBrain.SearchScale`; `StrategyBridge` (Strategy assembly) publishes `GameEvent`s from the NoiseHub bus (Noise), StalkerAgent (Death when caught or found hiding), brain state changes (ChaseEnded with seconds), hide spot occupancy (Hide, tag = spot type, value 1 if not found), player motor once per second (Move) and level start (RoomEntered `B1`); triggers `NEW_ROOM`, `DEATH`, `ENCOUNTER_ENDED`; builder places `StrategyRunner` + bridge; Online and URL read from PlayerPrefs `strategy.online` / `strategy.url`.
  - **Exclusions:** `HorrorEventRunner` use of `HorrorSuggestionGate` (next, F-15k), PatrolBias and SearchBias consumption, a Settings toggle for Online, any position or specific hide spot in events (categories only), shipping a key.
  - **AC:** 1. Level_B1 contains one `StrategyRunner` and bridge, Online false by default, and plays with no server. 2. A noise on the hub, a catch, a chase ending and a hide each produce one matching `GameEvent` with no position data. 3. A fake transport command with HideBonus raises the inspection chance of a spot, and an InvestigationMultiplier changes the search time, both within the validator limits; with no command both stay at baseline. 4. A server timeout or error changes nothing (executor stays at baseline). 5. With a live uvicorn server and a key, a NEW_ROOM trigger gets a validated response in under 3 s (manual run, logged; skipped when the server is down).
  - **Test:** EditMode (modifier maths, search scale), PlayMode on Level_B1 with fake transport, one live PlayMode test that skips when `127.0.0.1:8765` is unreachable. Sub-steps (one commit per file): 1 spec, 2 `IStrategyModifiers`, 3 `EntityBrain.SearchScale` + test, 4 `StalkerAgent.Modifiers`, 5 Strategy asmdef refs, 6 `StrategyBridge`, 7 builder, 8 PlayMode tests, 9 live test, 10 docs.
- **Status rule:** DONE only with real test output; Python tests need Python 3.11+ (check availability first). LLM quality and "feels like it is learning" are UNVERIFIED until a person plays.
