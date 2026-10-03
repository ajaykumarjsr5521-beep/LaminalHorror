# 00 — Discovery Report

Date: 2026-10-03 · Status: DONE

## Current state
| Item | Finding |
|---|---|
| Project folder | `C:\Users\Ajay Mahto\Desktop\LaminalHorror` — **completely empty** (0 files). No git repo. |
| Engine / packages | None. No `ProjectSettings/`, `Packages/manifest.json`, or `Assets/`. |
| Implemented features | None. |
| Unity Editor / Hub | **Not found** in default locations (`C:\Program Files\Unity`, Unity Hub, `%LOCALAPPDATA%\Unity`). May be installed elsewhere. |
| Android SDK/NDK/JDK | Not found in `%LOCALAPPDATA%\Android\Sdk`. (Unity can install its own via Hub modules.) |
| Other tools | Git present. `dotnet` absent. |
| Build / test status | Nothing to build or test. |
| Conventions | None to preserve. Conventions are defined in `04-technical-architecture.md`. |

## Blockers
1. **Unity is not verifiably installed.** I can author C# and project files, but cannot compile, run tests, or build APKs/AABs until Unity (with Android Build Support + Windows) is available and I know its path. Without it, no feature can reach DONE.
2. No Android test device or emulator confirmed.
3. No Google Play developer account confirmed (needed only at release).

## Assumptions (please confirm or correct)
- A1: Unity 6 LTS (6000.0.x) with URP is acceptable. Exact version to be pinned to what you install.
- A2: Solo/small team; art is mostly low-poly, stylised, license-clean or self-made.
- A3: Premium (paid) game or free demo + paid full unlock is the preferred model (see doc 07).
- A4: Target audience is teens/adults (horror content; expect Teen/Mature-class rating — to be determined by the IARC questionnaire, not assumed).
- A5: Name "Nocturne Annex" is a **working title**, pending trademark/store-name search.

## Risks
| ID | Risk | Mitigation |
|---|---|---|
| R1 | Scope creep from "full horror game" ambition | One polished level first; hard MVP cut line (doc 03) |
| R2 | Mobile perf with dark, light-heavy scenes | Baked lighting, ≤2 realtime lights, early device profiling |
| R3 | Asset licensing | Register (doc 09); prefer self-made greybox + CC0 |
| R4 | Play policy drift (target API level, Data safety) | Verify current docs at release gate (doc 08); not asserted here |
| R5 | Horror pacing is hard to tune without playtests | Data-driven event system, in-editor debug tooling |
| R6 | Unity toolchain unavailable to me | Decision D1 |
