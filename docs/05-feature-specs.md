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

## F-01 First-person controller — TODO
- **Purpose/Experience:** Responsive, comfortable movement; weight without sluggishness.
- **Scope:** move, look, sprint (stamina), crouch (with headroom check), head-bob (optional/off-able), footstep events by surface. **Excl.:** jump, lean, climbing.
- **Deps:** F-00, F-02 input.
- **Design:** `CharacterController`-based `PlayerMotor`; `PlayerLook` with sensitivity/invert; crouch collider resize + ceiling check.
- **AC:** walk 3.0 m/s, sprint 5.0 m/s, crouch 1.5 m/s (configurable); cannot stand under low ceiling; no clipping through walls at 30 fps and 60 fps in a test corridor; no per-frame GC allocs (Profiler); camera look has no drift when input is zero.
- **Test:** PlayMode tests for speeds/crouch-ceiling; manual device feel test.

## F-02 Input (Windows + mobile) — TODO
- **Scope:** Input actions, `InputRouter`, mobile floating stick + look-drag + buttons, gamepad support (bonus), scalable/relocatable touch controls, safe-area handling. **Excl.:** full key rebinding UI (post-MVP).
- **AC:** same gameplay reachable on KB+M, gamepad, touch; touch controls sit inside safe area on notched devices; multi-touch (move + look + button) works simultaneously; no input stuck after app pause/resume; controls size adjustable 80–130%.
- **Test:** EditMode (router mapping), manual on ≥2 phones (different aspect ratios), Windows.

## F-03 Interaction system — TODO
- **Scope:** `IInteractable`, focus detection, prompt UI, doors (open/locked), pickups, readable notes, hold-to-interact optional. **Excl.:** physics grab/throw.
- **AC:** prompt appears within 2.0 m and ≤ ~1 frame after focus; locked door gives feedback and states needed item generically (no spoilers); interaction can't trigger through walls; works with all three input schemes.
- **Test:** PlayMode raycast/occlusion tests; manual.

## F-04 Inventory & journal — TODO
- **Scope:** item definitions (SO), pick up/use, notes list (re-readable), cross-reference collectibles, simple UI. **Excl.:** crafting, weight, stacking.
- **AC:** items persist through save/load; used key removed; notes re-readable any time; UI usable at 5" phone and 1080p PC; ≤3 taps to read a note.
- **Test:** EditMode unit tests (add/remove/has), save round-trip, manual UI.

## F-05 Puzzle: "Three Dates" — TODO
- **Scope:** 3 notes + 3 in-world date props → 4-digit code lock → opens Records Office exit door. Clue redundancy: if player missed a prop, the note text still points to its location. **Excl.:** hints system, timers.
- **AC:** only the correct code opens; wrong code never locks player out; puzzle solvable from in-game info alone (verified by a fresh tester without help ≥ 4 of 5 testers); state saved/restored.
- **Test:** unit test code validation; playtest sheet.

## F-06 Save / checkpoint — TODO
- **Scope:** JSON versioned save, checkpoints at lamp rooms, autosave, continue/new game, error surfacing. **Excl.:** cloud, multiple slots.
- **AC:** kill app mid-play → Continue restores last checkpoint with inventory & puzzle flags; interrupted write cannot corrupt existing save (atomic replace test); corrupt file → clear message, file preserved; New Game requires confirmation.
- **Test:** EditMode serialization tests incl. corrupt/old-version files; manual force-stop on Android.

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
