# 02 — Game Design Document (MVP slice)

Status: DRAFT

## 1. Core loop
Explore → notice anomaly/clue → collect item/read note → solve small puzzle → unlock next area → tension event → reach safe pocket (lamp room/checkpoint) → continue → exit.

## 2. Session shape
MVP level "**Night Shift: Floor B1**" targets **20–30 minutes** first playthrough, completable in ≤10 min on replay. Natural break points at checkpoints (safe lamp rooms).

## 3. Player abilities
| Ability | Windows | Mobile |
|---|---|---|
| Move | WASD / left stick | Left virtual stick (floating) |
| Look | Mouse / right stick | Right-screen drag |
| Sprint | Shift (hold) | Toggle button; auto-sprint option off by default |
| Crouch | C (toggle) | Button (toggle) |
| Interact | E / gamepad South | Tap on-screen prompt button |
| Inventory | Tab | Button |
| Flashlight | F | Button |
| Pause | Esc | Button |

Stamina: sprint limited with generous regen (anti-spam, not a grind gate). Flashlight: **no battery drain** in MVP (avoids frustration design).

## 4. Level layout (MVP)
Hand-authored, 6 spaces, linear-with-loops:
1. **Staff Entrance & Break Room** — tutorial: movement, interact, pickup.
2. **Main Reading Hall** — hub; locked doors; first Misfile (a corridor that wasn't there).
3. **Stacks A (shelving maze-lite)** — find **Brass Stamp** (puzzle item #1) using a cross-reference note.
4. **Records Office** — typewriter/terminal puzzle: enter 4-digit code derived from three notes.
5. **Loading Dock Stair** — Indexer encounter (if included) / scripted pursuit-lite.
6. **Exit Door → Ending card** (reveal of C-0). Restart or main menu.

## 5. Puzzle (MVP)
"**Three Dates**": three notes in different rooms reference dated entries on wall calendars/ledgers. Their days form a 4-digit door code. Clues are visible in-world; notes are re-readable in the inventory journal. Solvable without external knowledge. Wrong code gives non-punishing feedback.

## 6. Horror event system
Data-driven `HorrorEvent` assets: trigger (volume / item pickup / timer-in-room), conditions (once, cooldown), actions (audio cue, light flicker, door slam, prop shift, entity appear). A **tension director** tracks a 0–1 value rising with exploration time in "unsafe" zones and decaying in safe pockets; it throttles events so scares are spaced (min gap configurable, default 45s). No unavoidable instant-death.

## 7. Entity: The Indexer (include only if approved — Decision D4)
- Behaviour: waypoint patrol + investigates noise (sprinting, door slams). Sees player only in its light cone/short range.
- States: Dormant, Patrol, Investigate, Chase (short, ≤12s), Retreat.
- Player counters: crouch to reduce noise, break line of sight, hide in lockers (single interaction).
- Failure: caught → fade, respawn at last checkpoint with items intact. No permadeath, no health bar needed.
- If cut: MVP uses scripted, non-lethal presence events only.

## 8. Health / save
No health system in MVP (caught = checkpoint respawn). Checkpoints at safe lamp rooms; autosave on checkpoint, item pickup, and pause. One save slot; "New Game" confirms overwrite.

## 9. Narrative delivery
Notes, ledger pages, answering-machine tapes (text + optional VO), environmental props. All text localisation-ready (string table), English only at MVP.

## 10. Progression & replay
- Collectibles: 9 optional **Cross-references** (lore). Stored in journal; completion shown in end card.
- Two endings via one late choice (stay-and-file vs. burn the file). Both ≤2 min of added content.
- Post-MVP: Shuffle Mode (randomised clue/item placement within authored constraints).

## 11. UX
Minimal HUD (interaction prompt, subtle stamina ring). Subtitles for all audio cues (captions e.g. "[Distant footsteps]"). Pause accessible at any moment.

## 12. Difficulty
Single "Standard" difficulty; "Story" toggle in settings: entity detection range halved, chase shorter. (Decision D5.)
