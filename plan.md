# plan.md — current work (update at the end of each day)

Branch: `feature/F-13-liminal-framework` (cut from `docs/liminal-redesign`, which is cut from `feature/F-10-lighting-audio`; none merged). Design: docs/15-liminal-redesign.md. Spec: doc 05 F-13.

## Decisions taken autonomously 2026-10-06 (owner to review)
- Slice-first: one level (Hotel Meridian, The Guest). Floor B1 = prologue/hub. Indexer = entity 10 (F-11 folded in). Art kit from CC0 libraries, registered in doc 09.

## F-13a checklist (plain C#, EditMode tests; one commit per file)
- [x] Assembly NocturneAnnex.Liminal + test asmdef reference
- [x] EntityPhaseMachine + LegendProgress (AC1-3)
- [x] RuleSystem (AC4)
- [x] QuietTimePolicy (AC5)
- [x] AttentionTracker (AC6)
- [x] MusicStateSelector (AC7)
- [x] AnomalyDirector + RoomSnapshot (AC8)
- [x] LevelDefinitionValidator (AC9)
- [x] Doc 05 status, doc 12 row, CHANGELOG (EditMode 305/305). Stop before merge

## F-13b next (greybox Hotel Meridian at real scale; spec first)
- [x] F-13b done as specified: Hotel_Meridian scene (Build > Create Hotel Meridian Scene), LiminalDirector, LegendPickup, GuestManifestation; EditMode 305/305, PlayMode 175/175
- [x] 2026-10-07 owner decision: Hotel Meridian scene, builder, screenshots, tests, materials and lighting removed (UNVERIFIED: tests not re-run). Only Level_B1 remains; LiminalDirector, LegendPickup, GuestManifestation stay as framework code
- [ ] Next (needs owner feedback): save fields for phase and legend, music director with stems, Guest hunt, art pass

## Waiting on the owner
- Merge order: F-10, then docs/liminal-redesign, then F-13. Listen to F-10 audio; confirm CLEARED register rows.
- AC9 (device profiling) needs an Android device.
