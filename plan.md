# plan.md — current work (update at the end of each day)

Schedule: docs/14-sprint-plan.md (Sprint 1, Day 1 = 2026-10-05). Branch: `feature/F-09b-horror-events`.

## Today: Day 1 — verify F-09b
- [x] Commit F-09b code (22 commits) and docs, push
- [x] EditMode: 251/251 pass
- [x] Horror PlayMode fixture alone: 16/16 pass (38 s)
- [ ] Full PlayMode run FREEZES when the horror fixture is combined with the others (reproduced 3x, log stops after ~23 scene loads). Findings: each of the 18 fixtures passes alone (144 tests); all fixtures except Horror pass together (128/128, 44 s); Horror + InputRouterModal pass together (18/18). Bisect so far: Horror + any single other fixture passes (all 16 pairs). Hang needs 3+ fixtures. Horror + {Accessibility, CodeLock, ContentNotice, GameplayUi} passes; Horror + {LevelB1Scene, LevelTouchControls} passes; Horror + {MainMenu, PauseController, PlayerInventory, PlayerMotor, PromptView, SaveGame, SceneFlow, SettingsApplier} passes (73). **Horror + {InputRouterModal, Interactable, Interactor} HANGS.** Next: run Horror with each pair of those three, then read the log tail for the stuck test; suspect Horror TearDown destroying InputRouter (DestroyImmediate) or leaving ModalGate/time state that Interactable/Interactor tests rely on
- [ ] Fix failures in F-09b files only (one commit per file)
- [ ] Check AC6 (spacing) and AC8 (overlay not in release builds) are covered by a test or a code check
- [ ] Doc 05: F-09b result with real numbers; doc 12 row; CHANGELOG entry (one commit each), push
- [ ] Stop before merge; owner reviews

## Open questions for the owner
- Revert Unity auto-edits (`InputManager.asset`, TMP font)? Currently left uncommitted.

## Next (Day 2-3)
- Day 2: fix anything left; AC6/AC8 checks. Day 3: mark F-09b done, merge after review.
- Day 4: owner actions (branch protection, LFS, CI secrets), Android Build Support.
