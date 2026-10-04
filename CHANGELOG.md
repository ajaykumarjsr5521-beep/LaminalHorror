# Changelog

All notable changes are recorded here. Format: [Keep a Changelog](https://keepachangelog.com/), versioning: [SemVer](https://semver.org/).
Verification status per change lives in [docs/12-progress-log.md](docs/12-progress-log.md).

## [Unreleased]
### Added
- Project scaffold for Unity 6000.6.4f1: assemblies, Boot scene, batchmode build script, test runner script (F-00).
- Input layer: keyboard/mouse, gamepad, touch widgets, safe-area fitter, 80-130% control scale (F-02).
- First-person controller: walk, sprint with stamina, crouch with headroom check, look (F-01).
- Interaction: interactor with wall occlusion, doors, pickups, notes, prompt view (F-03).
- Inventory and journal logic with key consumption (F-04).
- "Three Dates" code lock logic with no-lockout retries (F-05).
- Versioned single-slot save with atomic writes and corrupt-file handling (F-06).
- Settings: versioned `settings.json`, clamping, safe fallback on corrupt files, applier for input/audio/quality; shared `AtomicFile` helper (F-07a).
- Pause controller: freezes time and audio, auto-pauses on focus loss, restores exact state (F-07b).
- Main menu and settings screen models: confirmed New Game, unreadable-save warning, live settings preview with save/revert (F-07c, logic only).
- Main menu, settings, credits and confirm screens plus pause overlay (generated scene, screenshot capture tool); stale-dialog bug found and fixed in review (F-07c).
- Keypad, note reader and journal screens as generated prefabs; `ModalGate` blocks gameplay input while a screen is open (F-07d).
- Accessibility: captions system and prefab, text size setting (Small/Medium/Large), reduce flicker and reduce camera motion settings, flash budget (max 3 per second) and safe flicker waveform, WCAG AA contrast tests, first-launch content notice (F-08).
- Settings files with an unreadable or newer-version content are now moved aside before the next write instead of being overwritten.
- Level "Night Shift: Floor B1" greybox (generated scene): break room, reading hall, stacks, records office, loading dock, Three Dates puzzle readable from in-world props, 4 checkpoints with save and resume, exit gating, HUD with pause and journal buttons, end card (F-13).
- Boot scene opens the main menu; menu Continue and New Game start the level (F-13).
- Commit-message hook and template; one-commit-per-file practice (X-01).
- Localisation-ready strings: `Loc` table, all current player-facing text migrated, guard tests (X-04).
- Engineering process docs, `.editorconfig`, README, ADRs, PR/issue templates (X-01).

### Fixed
- No URP pipeline asset was assigned, so URP materials rendered magenta; mobile and PC URP assets are now created and assigned per quality level.
- Boot scene was empty and would have shown a blank screen; it now opens the main menu. The Greybox test scene is disabled in Build Settings.

### Decisions
- iOS recorded as future scope; current targets are Windows and Android.

### Known limitations
- Nothing has been verified on an Android device; Android build not yet produced.
- The level has no touch controls on its HUD yet, so it cannot be played on a phone. Level art, lighting, audio, scare events and the enemy are not built (greybox only).
- Level note and item text is placeholder prose awaiting owner review.
