# 14 — Sprint Plan (living document)

Status: DRAFT. Planning guesses, not commitments. Re-plan at every sprint retro.

**Assumptions:** 2 hours a day with Claude, every day including weekends (14 h per sprint). Day 1 = Mon 2026-10-05. Estimate is 85-130 h total; the plan uses the middle (56 days). Range: 43-65 days (Nov 16 - Dec 8).

**Dates that matter**
- Playable Windows MVP: about Day 35 (Sun Nov 8).
- Release-ready build: Day 56 (Sun Nov 29).
- Google Play closed test needs 14 days (check Google's current rules for new accounts). Uploading on Day 46 (Nov 19) means it ends about Dec 3, so **production release is about Dec 4-5**, not Nov 29.

**Risks to the dates:** no Android device yet (get one in Sprint 1); owner actions pending (branch protection, CI secrets, Git LFS); Indexer (F-11, decision D4) adds about 1 week; art/audio decisions need the owner.

## Sprint 1 — Finish F-09b, set up Android (Oct 5-11)
| Day | Date | Task |
|---|---|---|
| 1 | Mon Oct 5 | Run EditMode + PlayMode on the F-09b branch; list failures |
| 2 | Tue Oct 6 | Fix failures; check AC6 (event spacing) and AC8 (overlay not in release builds) |
| 3 | Wed Oct 7 | Mark F-09b done in doc 05 with real results; progress log, CHANGELOG; merge |
| 4 | Thu Oct 8 | Owner actions: branch protection, Git LFS, CI secrets. Install Android Build Support |
| 5 | Fri Oct 9 | F-10 spec: list audio cues + caption keys, asset sources, lighting budget |
| 6 | Sat Oct 10 | Baked-lighting setup: lightmap settings, max 2 realtime lights |
| 7 | Sun Oct 11 | Buffer; sprint retro; buy/borrow an Android phone |

## Sprint 2 — F-10 Lighting and audio (Oct 12-18)
| Day | Date | Task |
|---|---|---|
| 8 | Mon Oct 12 | Ambience beds: source or create, add to asset register (doc 09) |
| 9 | Tue Oct 13 | Footsteps by surface |
| 10 | Wed Oct 14 | Music drones and event stingers |
| 11 | Thu Oct 15 | Audio mixer, ducking, hook clips into events |
| 12 | Fri Oct 16 | Caption test: every cue has a caption key |
| 13 | Sat Oct 17 | Asset register audit: all CLEARED |
| 14 | Sun Oct 18 | Buffer; retro |

## Sprint 3 — Indexer and first Android build (Oct 19-25)
| Day | Date | Task |
|---|---|---|
| 15 | Mon Oct 19 | Decide D4 (Indexer in or out). If out, move F-11 days to art |
| 16 | Tue Oct 20 | F-11 spec and pure-logic model + tests |
| 17 | Wed Oct 21 | F-11 behaviour and safeguards |
| 18 | Thu Oct 22 | F-11 scene wiring |
| 19 | Fri Oct 23 | First Android APK build; fix build problems |
| 20 | Sat Oct 24 | Device test: touch controls, Back button, safe area |
| 21 | Sun Oct 25 | Fix device findings; retro |

## Sprint 4 — Art pass and bake (Oct 26 - Nov 1)
| Day | Date | Task |
|---|---|---|
| 22 | Mon Oct 26 | Material and texture plan; mobile budgets |
| 23 | Tue Oct 27 | Props and set dressing, areas 1-2 |
| 24 | Wed Oct 28 | Areas 3-4 |
| 25 | Thu Oct 29 | Area 5 and exit |
| 26 | Fri Oct 30 | Mark static, light probes |
| 27 | Sat Oct 31 | Bake lightmaps; check seams |
| 28 | Sun Nov 1 | Buffer; retro |

## Sprint 5 — Performance (Nov 2-8)
| Day | Date | Task |
|---|---|---|
| 29 | Mon Nov 2 | Occlusion culling |
| 30 | Tue Nov 3 | LODs where needed |
| 31 | Wed Nov 4 | Profile on device: frame time, memory, thermals |
| 32 | Thu Nov 5 | Fix hot spots |
| 33 | Fri Nov 6 | Mobile post-FX tuning (vignette, grain) |
| 34 | Sat Nov 7 | Windows build, full run-through |
| 35 | Sun Nov 8 | **Playable MVP checkpoint (G2/G3 review)**; retro |

## Sprint 6 — Accessibility, playtest, bugs (Nov 9-15)
| Day | Date | Task |
|---|---|---|
| 36 | Mon Nov 9 | Accessibility audit: captions, text size, contrast, flicker, motion |
| 37 | Tue Nov 10 | Content notice and settings check |
| 38 | Wed Nov 11 | Playtest round 1 |
| 39 | Thu Nov 12 | Fix round 1 |
| 40 | Fri Nov 13 | Playtest round 2 |
| 41 | Sat Nov 14 | Fix round 2 |
| 42 | Sun Nov 15 | Full regression (EditMode, PlayMode, builds); retro |

## Sprint 7 — Release prep (Nov 16-22)
| Day | Date | Task |
|---|---|---|
| 43 | Mon Nov 16 | Privacy policy; keystore (stored outside the repo) |
| 44 | Tue Nov 17 | Play listing text, screenshots, icon |
| 45 | Wed Nov 18 | Data safety form, content rating |
| 46 | Thu Nov 19 | **Signed AAB; upload to closed test; 14-day clock starts** |
| 47 | Fri Nov 20 | Recruit testers; check the install path |
| 48 | Sat Nov 21 | Release checklist (doc 08) first pass |
| 49 | Sun Nov 22 | Buffer; retro |

## Sprint 8 — Closed test and final fixes (Nov 23-29)
| Day | Date | Task |
|---|---|---|
| 50-53 | Nov 23-26 | Triage tester feedback and crash reports; fix |
| 54 | Fri Nov 27 | Regression pass |
| 55 | Sat Nov 28 | Release checklist all green |
| 56 | Sun Nov 29 | Final build ready (G4 review). Closed test ends about Dec 3; then apply for production |
