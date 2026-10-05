# plan.md — current work (update at the end of each day)

Schedule: docs/14-sprint-plan.md (Sprint 1, Day 1 = 2026-10-05). Branch: `feature/F-09b-horror-events`.

## Today: Day 1 — verify F-09b
- [x] Commit F-09b code (22 commits) and docs, push
- [x] EditMode: 251/251 pass
- [x] Horror PlayMode fixture alone: 16/16 pass (38 s)
- [x] Full PlayMode freeze FIXED: Horror fixture left the Level_B1 scene alive under the next fixture (hang right after its last test, with InputRouterModal + Interactable/Interactor). TearDown now destroys the scene roots; SetUp resets timeScale/ModalGate; [Timeout(120000)] + real-time cap on game-time loops so a hang fails instead of freezing. Full PlayMode: 144/144 in 60 s.
- [x] Code review found 4 bugs (restart kept fired one-shots; props not reset; disable mid-effect; busy spot ate a one-shot): all fixed with tests. EditMode 253/253, PlayMode 148/148.
- [x] Fix failures in F-09b files only (done above)
- [x] AC6 covered by OneHourOfUnsafePlay test; AC8 by `#if` code check
- [x] Doc 05: F-09b result with real numbers; doc 12 row; CHANGELOG entry (one commit each), push
- [ ] Stop before merge; owner reviews

## Open questions for the owner
- Revert Unity auto-edits (`InputManager.asset`, TMP font)? Currently left uncommitted.

## Next (Day 2-3)
- Day 2: fix anything left; AC6/AC8 checks. Day 3: mark F-09b done, merge after review.
- Day 4: owner actions (branch protection, LFS, CI secrets), Android Build Support.
