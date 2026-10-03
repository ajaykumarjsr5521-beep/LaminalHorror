# 03 — MVP Scope & Phased Roadmap

Status: DRAFT

## MVP definition (milestone M-Playable)
A player can: launch → main menu → New Game → move/look/sprint/crouch → interact and pick up items → solve "Three Dates" → experience ≥3 horror events → reach exit → see ending card → restart or return to menu; pause/settings/quit all work without errors, on Android and Windows.

## In scope
Level "Night Shift: Floor B1", first-person controller, dual input, interaction, inventory/journal, one puzzle, tension director + event system, checkpoints/save, menu/pause/settings, captions, baked lighting, original audio pass, Android + Windows builds.
**Conditional:** The Indexer (D4).

## Out of scope
Multi-level, online, accounts, analytics, ads, IAP, localisation beyond English, controller rumble, VR, procedural generation.

## Phases
| Phase | Goal | Exit criteria | Est. (solo) |
|---|---|---|---|
| P0 Setup | Unity project, URP, repo, CI-less build scripts, folder structure, test framework | Empty scene builds to Android + Windows; EditMode tests run | 2–3 d |
| P1 Greybox & Controller | Player movement + camera, dual input, greybox of 6 spaces | F-01, F-02 DONE; 60 fps greybox on ref device | 1–2 wk |
| P2 Interaction & Puzzle | Interaction, inventory, notes, door/lock, puzzle | F-03..F-05 DONE | 1–2 wk |
| P3 Systems | Save/checkpoint, menu/pause/settings, captions | F-06..F-08 DONE | 1–2 wk |
| P4 Horror | Tension director, events, lighting/audio pass, (Indexer) | F-09, F-10 (+F-11) DONE | 2–3 wk |
| P5 Polish & Perf | Art pass, bake, LOD/occlusion, memory/perf targets, accessibility | Perf & a11y criteria met on devices | 2 wk |
| P6 Release prep | Play listing, Data safety, rating, signed AAB, closed test | Release checklist (doc 08) all green | 1–2 wk |

Estimates are planning guesses, not commitments.

## Approval gates
G0 spec approval (now) → G1 after P1 (feel check) → G2 after P3 (vertical slice) → G3 monetization decision (before P6) → G4 release.
