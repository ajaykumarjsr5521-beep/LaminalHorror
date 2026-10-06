# plan.md — current work (update at the end of each day)

Branch: `feature/F-13-liminal-framework` (cut from `docs/liminal-redesign`, which is cut from `feature/F-10-lighting-audio`; none merged). Design: docs/15-liminal-redesign.md. Spec: doc 05 F-13.

## Decisions taken autonomously 2026-10-06 (owner to review)
- Slice-first: one level (Hotel Meridian, The Guest). Floor B1 = prologue/hub. Indexer = entity 10 (F-11 folded in). Art kit from CC0 libraries, registered in doc 09.

## F-13a checklist (plain C#, EditMode tests; one commit per file)
- [ ] Assembly NocturneAnnex.Liminal + test asmdef reference
- [ ] EntityPhaseMachine + LegendProgress (AC1-3)
- [ ] RuleSystem (AC4)
- [ ] QuietTimePolicy (AC5)
- [ ] AttentionTracker (AC6)
- [ ] MusicStateSelector (AC7)
- [ ] AnomalyDirector + RoomSnapshot (AC8)
- [ ] LevelDefinitionValidator (AC9)
- [ ] Doc 05 status, doc 12 row, CHANGELOG; stop before merge

## Waiting on the owner
- Merge order: F-10, then docs/liminal-redesign, then F-13. Listen to F-10 audio; confirm CLEARED register rows.
- AC9 (device profiling) needs an Android device.
