# plan.md — current work (update at the end of each day)

Schedule: docs/14-sprint-plan.md (Sprint 1, Day 1 = 2026-10-05). Branch: `feature/F-09b-horror-events`.

## Today: Day 1 — verify F-09b
- [x] Commit F-09b code (22 commits) and docs, push
- [x] EditMode: 251/251 pass
- [x] Horror PlayMode fixture alone: 16/16 pass (38 s)
- [ ] Full PlayMode run (background, `Builds/full-playmode.out`). A previous full run froze after the horror fixture; if it freezes again, find which later fixture hangs: run fixtures one at a time with `FILTER`
- [ ] Fix failures in F-09b files only (one commit per file)
- [ ] Check AC6 (spacing) and AC8 (overlay not in release builds) are covered by a test or a code check
- [ ] Doc 05: F-09b result with real numbers; doc 12 row; CHANGELOG entry (one commit each), push
- [ ] Stop before merge; owner reviews

## Open questions for the owner
- Revert Unity auto-edits (`InputManager.asset`, TMP font)? Currently left uncommitted.

## Next (Day 2-3)
- Day 2: fix anything left; AC6/AC8 checks. Day 3: mark F-09b done, merge after review.
- Day 4: owner actions (branch protection, LFS, CI secrets), Android Build Support.
