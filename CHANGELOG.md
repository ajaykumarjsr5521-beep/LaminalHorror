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
- Engineering process docs, `.editorconfig`, README, ADRs, PR/issue templates (X-01).

### Known limitations
- Nothing has been verified on an Android device; Android build not yet produced.
- No UI screens yet (menus, keypad, journal); no level content, art or audio.
