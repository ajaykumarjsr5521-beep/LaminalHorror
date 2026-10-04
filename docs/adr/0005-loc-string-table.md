# ADR 0005 - Loc string table with code-defined English default

Status: Accepted (2026-10-04)

## Context
The GDD requires localisation-ready text, but only English ships at MVP. Hardcoded strings in gameplay code make later translation costly and error-prone.

## Decision
All player-facing text goes through `Loc.Get` / `Loc.Format` with `area.name` keys. The English table lives in code (`DefaultStrings`) and can be replaced at runtime via `Loc.SetTable`. A missing key shows `[key]` and logs an error once. A ScriptableObject table asset will be added only when a second language is approved.

## Consequences
- Tests and the guard in `StringGuardTests` keep new code honest (keys must exist; no literals in guarded areas).
- UI text from F-07 onward must use keys from the start.
- Wording tests should read text through `Loc` unless the wording itself is under test.
