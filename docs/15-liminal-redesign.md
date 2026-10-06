# 15 — Liminal Horror Redesign (audit, framework, levels, implementation plan)

Status: DRAFT for owner review. Written 2026-10-06 from an audit of the code and scene as they are on `feature/F-10-lighting-audio`. No web research was done for this document; where it names techniques it relies on common design knowledge, not cited sources. Nothing in this document is built yet.

Working title stays **Nocturne Annex**. The redesign keeps the existing world seed (docs 01, 02): the Halloran-Vey Records Annex, the overnight archivist Imre Calloway, file C-0, Misfiles, Cross-references, Stamps, the Indexer.

---

## 1. Current problem analysis

### What exists (measured, not guessed)

| Aspect | Today |
|---|---|
| Space | 5 areas: 8x10, 12x16, 12x12, 12x12 and 3x16 m. About 608 m2 in total, all flat 3 m ceilings, walls 0.3 m thick, doors 1.5 x 2.2 m. |
| Sightlines | Longest clear view is 16 m, down the 3 m wide dock. Every other space is closed by walls and doors. Nothing is visible beyond the room you are in. |
| Materials | 5 flat solid-colour materials. No textures, normal maps, trims, signage, windows, or props with form. Primitive boxes only. |
| Light | One identical warm point light per room (intensity 7), flat blue-grey ambient (0.16, 0.16, 0.19), black camera background. No skybox, no fog (`m_Fog: 0`), no post-processing volume, no reflections. |
| Sound | One drone loop whose volume follows tension, footsteps, six one-shot event sounds. Placeholder-grade CC0 clips. |
| Events | Six local one-shots (flicker, door slam, prop shift, whisper, misfile, figure). The director fires one at most every 45 s once tension allows it. |
| Mystery | Notes exist only as puzzle clues for the 4-digit code. No folklore, rules or evidence about what the Annex is. |
| Entity | None. The Indexer (F-11) is conditional and unbuilt. |
| Structure | One level, linear. Room, door, room, door. |

### Why it does not feel liminal (answers to the ten questions)

1. **Not liminal:** liminality needs a space that is familiar in purpose and wrong in state. We built generic dark boxes. A box has no purpose and so cannot contradict it.
2. **Rooms too small:** the biggest space is 12x16 m under a 3 m ceiling. That is a classroom, not an institution. Nothing makes the player feel small.
3. **Generic prototype:** every effect is a local reaction to a timer (a light flickers, a door slams). That is a haunted-house script. It has no connection to place, history or rules.
4. **Darkness as a substitute:** ambient 0.16 plus one point light hides the lack of detail. Flat colours look acceptable in the dark and cheap in the light, so the scene is kept dark. Light should reveal wrongness, not hide missing content.
5. **Sound does not create dread:** sound effects are isolated events on a near-silent base. There is no room tone, no acoustic space (reverb, distance, occlusion), no source that moves incorrectly, and the music is one volume knob, not a state.
6. **Too little environmental storytelling:** props are boxes, so they carry no story. The notes serve the puzzle, not the world.
7. **No scale:** no long sightlines, no vertical space, no second floor visible, no atrium, no distance.
8. **No mystery:** the player is never given a question the building poses, only a code to find.
9. **No memorable identity:** the player cannot describe the game in a sentence other than "dark office". The hook (a building that keeps a file on you, and rearranges) is not visible in play.
10. **Not entering another reality:** the start is the same quality as the middle and end. There is no threshold, no arrival, and nothing becomes impossible.

### What is worth keeping

Tension director and event picker (pure C#, tested), the event runner and spot framework (generalise it), save system with fired-event ids, captions and accessibility (flash budget, Reduce Flicker, Reduce Motion), settings, `AudioDirector`/`CueCatalog`/`AudioBus`/`MusicDrone`, footsteps by surface, the baked-lighting pipeline, `ModalGate`, journal, level builder scripts, 279 EditMode and 167 PlayMode tests. The redesign adds layers on top of these. It does not rewrite them.

---

## 2. Honest scope warning

The brief describes about 10 distinct levels, each with its own architecture, entity, rules, audio and music. The current plan budgets 56 days for one 20-30 minute level. Ten fully finished levels is roughly an order of magnitude more content, plus real art that a primitive-box pipeline cannot produce.

**Recommendation:** build the framework once (sections 6-10), prove it with **one vertical slice** (a large hotel atrium and its wings, with the entity "The Guest"), and treat levels 2-10 as a roadmap that reuses the framework with new data and new art. Do not start level 2 until the slice is playtested.

Other conflicts with the existing budgets (doc 06, doc 04): at most 2 realtime lights, 200k visible triangles, 150 draw calls, Low-tier phone at 30 fps. Large bright atria with fog, reflections and water are expensive on mobile. Section 3 gives the cheap techniques that make this possible (baked light, fake mirror rooms, probe reflections, fog colour not volumetrics, occlusion culling, additive scene streaming).

---

## 3. Environment redesign

**Principle:** each level is one large, legible, brightly lit public building whose purpose is obvious and whose state is wrong. The player must be able to see the whole building's logic from one spot.

### Rules for every level

- **Hero vista in the first 60 seconds:** one position from which the player sees at least 40 m of depth, two floors, and at least three distinct destinations (a lit sign, a stair, a door, a far light).
- **Scale ratios:** public spaces 5-9 m ceilings (atria 12+ m), corridors 3-4 m wide, door heights 2.6 m in lobbies, standard doors only in back-of-house. Contrast between grand public space and cramped service space is itself the liminal move.
- **Never room-door-room:** spaces connect through openings, balconies, stairs, escalators, windows into other spaces, atria, and long corridors with side views. Doors are rare and meaningful.
- **Layered depth:** foreground (props), middle (architecture), far (lit distant structure), vanishing (a space that ends in dark or glare, never fully visible).
- **Beauty first:** each level has one location the player wants to photograph: skylight, pool of light, reflection, rain on a glass wall, an unusually colourful sign.
- **Normality before wrongness:** the first 20% of the level is functioning and almost plausible. Escalators run, screens show times, refrigerators hum.
- **Silence is allowed:** long uneventful stretches are required (section 15).

### Technical approach on this budget

| Need | Technique |
|---|---|
| Big interiors | Modular kit (section 16), baked GI, few realtime lights (budget still at most 2, spent on flicker/hero lights), occlusion culling, LOD on props. |
| Brightly lit | Lightmap bake plus light probes for movers; emissive panels and signs instead of many lights. |
| Reflections | Baked reflection probes, glossy floors with cubemap, and **fake mirror rooms** (a mirrored copy of the corridor behind a glass plane). No planar realtime reflection. |
| Fog | URP fog or a cheap height-fog shader. Colour and density per level. No volumetrics on mobile. |
| Water | Vertex-wave or scrolling-normal shader with baked caustic texture; no simulation. |
| Streaming | Hub scene plus additive scenes per wing; loads hidden behind transitions. |
| Performance tiers | PC: full probes, SSAO, bloom. Mobile Low: no SSAO, fewer probes, shorter far plane, simpler fog. Same layout. |

---

## 4. Visual system

A `LiminalityProfile` asset per level (section 6) sets all of these. A coherent language beats "creepy filters".

- **Light:** one dominant colour temperature per level (hotel 2700 K amber, school 4000 K green-white, pool 5500 K cyan). Evenly lit spaces, low contrast, a few hot and a few failed fixtures. Wrongness is shown by light that is *correct but impossible* (daylight through a windowless corridor), not by gloom.
- **Colour:** muted, institutional, one accent per level (a signage colour). Avoid pure black; shadows stay readable.
- **Composition:** symmetry, one-point perspective, repeated modules, framing by doorways and columns. Place landmarks so that a screenshot reads without a monster.
- **Texture:** tiled institutional materials (carpet, tile, wallpaper, ceiling panels) with grime variation and decals; signage with readable, slightly wrong text.
- **Post-processing:** light only: gentle grade, mild bloom, very fine grain, vignette near zero. No heavy VHS/CRT filter. Grain and chromatic shift may rise slightly during presence, never as a constant.
- **Props:** environmental storytelling objects per section 8, with gameplay meaning.
- **Accessibility:** flicker through `SafeFlicker`/`FlashBudget`; camera effects scaled by `MotionScale`; every entity cue captioned.

---

## 5. Music and audio systems

### Audio hierarchy (priority high to low)

1. Room tone and environmental ambience (always present, per zone).
2. Architectural sound: HVAC, fluorescent hum, pipes, escalator and elevator motors, distant machinery.
3. Distant sounds: footsteps elsewhere, announcements, a door far away.
4. Music.
5. Subtle anomalies: sound before its cause, sound that moves incorrectly, sound with no source, delayed echo.
6. Entity sounds.
7. Rare jumpscare sounds (lowest priority, at most one per level).

### Music states (a state machine, not a loop)

| State | Music | Enters when |
|---|---|---|
| Exploration | Minimal, almost peaceful, sparse pad | Default in normal areas |
| Wonder | Warm, wide, tonal, a clear melodic fragment | Hero vistas and the beauty location |
| Unease | Same material, slight detuning, a missing note | First anomaly seen or phase Clue |
| Presence | Low-frequency layer fades in under the music; reverb tail lengthens | Phase Presence, entity within a radius |
| EntityNear | Layers shift in ways the player may not consciously register (pitch drop, stereo narrowing, rhythm hint) | Entity close, not necessarily visible |
| Hunting | Structured, rhythmic, threatening | Phase Hunt only |
| Aftermath | Music **stops**. Silence, room tone only. | Hunt ends |
| Return | Exploration returns, slightly changed | Longer after aftermath |

**Implementation:** layered stems per level (pad, warmth, dissonance, low, rhythm) mixed by `MusicDirector`. Transitions are crossfades and layer gains driven by a small `ExperienceState` struct (phase, tension, entity distance, safe-zone flag, blocked), never by hard cuts. The existing `MusicDroneModel` becomes one input. Music ducks under caption-worthy cues via the existing `AudioBus` duck.

**Silence as a mechanic:** `AmbienceDirector` can drop the ambience bed to bare room tone for a configured time ("the building holds its breath"), used as a signal that a rule is in effect or about to break.

---

## 6. Liminality system (reusable, per level)

`LevelDefinition` (ScriptableObject) holds:

- id, display name, **horror language** (reflection, architecture, water, distance, sound, identity, memory, time, scale, reality)
- `LiminalityProfile`: scale targets, light colour and intensity, fog, post volume, signage palette, ambience zones, music stems and state mapping
- `normalityPercent`: how much of the level plays straight before wrongness
- `wrongnessBudget`: how many anomalies per minute may exist (low, always)
- the level's `EntityDefinition` and its `LegendEntry` list
- transitions: entry and exit

Every level is data on the same runtime. A **liminality checklist test** (EditMode on the level definition asset, PlayMode on the scene) enforces: a hero vista exists, no room-door-room chain longer than 2, ceiling heights, at most 2 realtime lights, every entity cue has a caption.

---

## 7. Entity system (reusable framework for level legends)

Every entity moves through the same phases. Only data and behaviour differ per level.

```
LEGEND → RUMOR → CLUE → PHENOMENON → SIGHTING → EVIDENCE → PRESENCE → HUNT → SURVIVAL → AFTERMATH
```

**`EntityPhase` state machine** (plain C#, saved):

- advances on **evidence points**, earned when the player reads a legend entry, sees an anomaly with attention, breaks or keeps a rule, or reaches a place. Not on a timer.
- cannot skip a phase; cannot enter Hunt before at least N legend entries have been found (so the player always learns before they are hunted).
- phase floors: the player can slow or avoid escalation by not investigating.

**`EntityDefinition`** (ScriptableObject):

- name, legend text keys, rules (list of `RuleDefinition`), manifestations per phase, hunt behaviour id, safe-zone rules, failure consequence, music and ambience overrides.

**`EntityDirector`** (per level, plain C# plus a thin MonoBehaviour):

- chooses **manifestations** allowed in the current phase from a budgeted pool: distant figure, reflection, sound with no source, footprints, door or elevator moving, light change, object moved, phone ringing in the wrong room, headlights far away.
- obeys **attention rules**: some manifestations only run when the player is *not* looking (camera frustum plus a line-of-sight raycast), some only when the player *is* looking.
- never fires two manifestations within a minimum gap (this generalises the current 45 s director; Section 15 gives the new numbers).
- hands over to a **hunt behaviour** (a small interface: `Tick`, `ObservedBy`, `OnRuleBroken`, `OnPlayerHidden`) only in phase Hunt. Hunts are rule-based, not chase-the-player AI: the entity is dangerous only if a specific rule is broken or a condition (being seen, being heard, standing in light) holds.
- existing `HorrorEventSpot` evolves into `ManifestationSpot` (same scene binding, same accessibility handling, same save of one-shots).

**Rules** (`RuleDefinition`): id, text key, trigger condition (e.g. `PlayerTurnedAround`, `EnteredElevator`, `LightsAreBlue`, `ReflectionMovedFirst`), violation consequence. A `RuleSystem` evaluates them each frame. **Rule mutation:** late in a level one rule is replaced (a `RuleChanged` event). Players who memorised the old rule are punished or surprised in a fair, telegraphed way (the change is announced by an environmental cue, never silently).

---

## 8. Legend system (how the mythology is found)

`LegendEntry` (ScriptableObject): id, kind (rumor / clue / evidence / rule hint), text key (localisation-ready), place tag, phase it unlocks, whether it is optional, and a medium (note, graffiti, sign, register, announcement, CCTV monitor, voicemail, missing poster, staff memo).

- Entries live in the world as props with an interaction. Reading adds them to the journal (existing `JournalModel`), sorted by entity, with a "what people say" and "what I saw" split, so the player assembles the legend.
- Writing style is **folklore**, not instructions: "People say that if the elevator arrives three times, you don't get in." Never "Rule 1".
- The player learns rules from at least **two independent sources** (one folklore, one physical evidence) before a rule matters.
- Contradictory entries are allowed (an old employee note says the opposite), supporting the theories in section 11.
- `LegendProgress` (pure C#, saved) records found entries and feeds `EntityPhase`.

---

## 9. Anomaly system

Anomalies are authored, not random: each has a **meaning** (which entity, which rule, which theory it supports).

- `AnomalyDefinition`: id, entity, tracked objects (a state before and after), trigger (on first visit, on return, on looking away, on a phase), reversibility, evidence points, whether it is visible only when observed.
- Categories: displacement (chair, object), light state, sign text, door present/absent, dimension (corridor slightly longer, room slightly smaller), sound (before its cause, repeated), reflection, time (clock stops, announcement repeats), presence trace (footprints, drips).
- **Memory mechanic:** `RoomSnapshot` records a few tracked objects per area on exit. On re-entry an anomaly may change one. The player is never told what; the journal may note "something is different". A subtle change must be fair: the previous state was visible and unchanged for at least 30 s of dwell.
- **Anti-random rule:** the same anomaly never repeats within a level, and none triggers without a legend or rule it relates to.

---

## 10. Level structure and transitions

### Level phases (a framework, adjustable per level)

Arrival, normality, exploration, beauty, first anomaly, investigation, legend discovery, small encounter, environmental change, presence, rule discovery, escalation, hunt, survival, aftermath, discovery, transition. Implemented as ordered **beats** in `LevelDefinition` that gate which manifestations may run and which music state is allowed. Beats advance by evidence, with minimum dwell times so quiet stretches always exist.

### Impossible transitions

`ImpossibleTransition` (door, elevator, escalator, stair, tunnel): the player walks through one threshold and arrives in a different place. The exit from level N and the entry to level N+1 share a **match-cut** object (the same elevator panel, the same door handle), then the lighting and sound change over about 2 seconds. Loading is additive and hidden behind the transition. Each transition is saved as a checkpoint.

### Frame story (preserves the existing game)

The existing **Floor B1** becomes the **prologue and the hub**: the tutorial in the Annex (break room, Hall, Stacks, Records Office, dock stair). Its dock stair door opens into Level 1 instead of the end card. Level 10 returns to a **much larger Annex** for the C-0 reveal. This keeps every system, test and puzzle already built and gives the whole game one place to start and end.

---

## 11. World mystery

**Setting concept:** the Annex archives "unrecorded moments". Each level is a **case file** the building opened for Imre (C-1 to C-9), and each legend is a filing that went wrong. Entities are not nine monsters; they are *misfilings*, each a person's unrecorded moment stored in the shape of the building they were in.

**Questions the player is left with:** why are these places connected; why does each have a legend; why does the environment know things about me; who built this; are the entities different or one thing; am I moving through places or through versions of one place.

**Competing theories, each with in-world evidence:**

1. **The Annex is a memory of Imre.** The places are the ones Imre has been; objects bear small personal details (a lanyard colour, a childhood address on a form).
2. **Imre is a file.** C-0 is dated tomorrow. Records show Imre's employment history was written after the fact.
3. **The entities are one, the Indexer, in different guises.** Each legend ends with the same filing stamp; the Indexer's cardigan appears as a background detail in all levels.
4. **Imre is dead and this is the in-between** (the brief's "between reality, memory, dreams, and death"). Indirect clues only: no outdoors, no weather, time that does not pass, nobody who recognises the date.

No level resolves the question. The two endings (stay and file, burn the file) express different answers without confirming one.

---

## 12. The ten levels

Each level has one horror language, a **different mechanic**, and a legend that belongs to the place. Entities are listed in section 13.

| # | Level (case file) | Normal purpose | Why empty | Horror language | Signature mechanic | Escalation | Transition out |
|---|---|---|---|---|---|---|---|
| P | **Annex Floor B1** (prologue, exists) | Records archive | Night shift | Wrongness introduced | Existing puzzle and tutorial | Misfile corridor | Dock stair opens into the hotel |
| 1 | **Hotel Meridian** | Business hotel, atrium lobby | 3 AM check-in lull that never ends | Reflection | Read mirrors and glass: the world in a mirror is one step ahead | Occupied rooms that cannot exist | Elevator opens into a school |
| 2 | **Calloway Elementary** | School | After hours, every light on | Architectural paranoia | Corridors lengthen only while watched | Bell rings with no power | Classroom door opens onto a station |
| 3 | **Eastside Aquatic Centre** | Public pool complex | Closed for cleaning | Water | Water level and wet traces tell where it has been | Pool water changes level | Drain tunnel opens into a garage |
| 4 | **Level 6 Parking Structure** | Car park | Empty at midday | Distance | Something far away is closer each time you change floors | Engine behind you | Ramp leads to a platform |
| 5 | **Harlow Street Station** | Metro station | Last train gone | Sound | Announcements for trains that do not exist; use sound to locate | Train sound with no train | Tunnel opens into a mall |
| 6 | **Greenway Galleria** | Shopping mall | Closing time that does not come | Identity | Signs and displays show Imre's name and habits | Displays become Imre | Escalator climbs into an apartment tower |
| 7 | **Tower Block 7** | Apartment building | Everyone just left | Memory | Rooms remember changes; compare before and after | Your door number changes | Fire stair opens into a cinema |
| 8 | **Cine Royale** | Cinema complex | Between screenings | Time | Film shows the last or next minute of the player | Loops, clocks that disagree | Projector beam leads to a terminal |
| 9 | **Terminal Concourse** | Airport | Midday with no passengers | Scale | The hall grows; distance and size cannot be trusted | Gate that does not end | Jet bridge opens into the Annex |
| 10 | **Annex, Floor 0** | The whole archive | Night shift | Reality | Rules from earlier levels return changed; C-0 | Final rule change | Two endings |

Each level also defines: visual identity (one palette and one accent), sound identity (room tone, three architectural sounds, one signature sound), psychological effect (the language), major discovery (a legend that reframes a previous level), and its own escalation. Levels should differ in **rules**, not only colour.

Detailed design for levels is written per level when it is built; the slice (Level 1) gets a full spec.

---

## 13. The ten entities

All entities share the phase framework (section 7). Fields below are the data each `EntityDefinition` needs; unspecified parts stay deliberately unknown.

| # | Name | Legend (in-world folklore) | First visual sign | First audio sign | Rules | Hunt | Survive | Failure | Stays unknown |
|---|---|---|---|---|---|---|---|---|---|
| 1 | **The Guest** | "Don't check in to a room you did not book." | Reflection of someone standing in a hall that is empty | Key card beep in the next corridor | If the reflection moves first, stop; do not enter a room whose door is open | Appears in mirrors nearer each time and knows the next room | Keep still when reflection leads; break line of sight with glass | The next room is the Guest's | What it wants |
| 2 | **The Last Student** | "Never answer the classroom intercom." | Child-sized silhouette at the end of a corridor | Bell with no power | Moves only when unobserved; do not run toward it | Corridor lengthens as you approach | Walk backwards, keep it in view | Corridor loops with it behind | Who it was |
| 3 | **The Drowned Woman** | "Don't look into the pool after midnight." | Wet footprints | Drips from dry places, swimming below | Stay out of the water; follow dry floor | Water level rises along its path | Reach high ground, keep moving | Pool becomes the floor | Whether she hunts or searches |
| 4 | **The Driver** | "The car on Level 6 isn't parked there." | Headlights on a far level | Engine idle, far | It never approaches while watched; headlights off means hide | Each floor change, the car is closer | Break line of sight, stay out of light | Engine behind you | The car's owner |
| 5 | **The Announcer** | "If it announces your name, the train is for you." | Departure board shows a train that is not there | Platform announcement, one syllable late | Do not board; do not answer by name | Follows by voice, moves through speakers | Stay silent, move when the sound moves | The last train takes you | Whose voice |
| 6 | **The Regular** | "Don't buy anything the shop knows you want." | Display with a familiar jacket | Footsteps matching yours a beat behind | Do not take what is displayed for you | Displays copy Imre | Leave items, never enter the shop that matches | You become a display | Why it knows |
| 7 | **The Tenant** | "Your door number is not yours." | A door number slightly changed | A key in a lock, nearby | Count doors; do not enter the number that changed | Corridor re-numbers as you pass | Memorise numbers; leave by the unchanged one | Your door has a tenant | Whose home |
| 8 | **The Projectionist** | "Don't watch the second showing." | Film shows a hallway you were in | Projector clicks | Do not watch the replay; leave before the loop | The loop starts early | Break the loop by changing your path | Loop becomes final | What is on the reel |
| 9 | **The Gate Agent** | "Boarding is never final." | Distant figure at a gate | Page for a passenger | Do not approach gates that are lit | Concourse extends away from you | Move toward unlit signage | You are boarded | Where the flight goes |
| 10 | **The Indexer** | "The Annex files what is out of place." | Cardigan figure in the stacks | Stamp thump, regular | Do not be out of place; keep a stamp | Patrols by sound and light | Be where you belong; use the stamp | You are filed | Whether the Indexer is all the others |

Entity design constraints (all ten): rarely or never fully lit; heard or inferred more than seen; never speaks; first contact is not a chase; at least one rule that the player can verify; at least one aspect never explained; every cue captioned.

---

## 14. Player progression

Awareness grows in stages that match the questions the player asks:

1. **Where am I?** arrival, hero vista.
2. **Why is nobody here?** normality with absence.
3. **Why does this feel familiar?** period details, nostalgia.
4. **Why is the architecture repeating?** first anomaly.
5. **Why is something changing?** memory comparison.
6. **Is something watching me?** sightings.
7. **Does this place know me?** the building uses Imre's details.
8. **Was I ever outside?** no outdoors, time that does not pass.

Mechanically: each level gives the player one new **verb** (read reflections, count doors, listen for source, compare rooms) and the journal assembles a **case file** per level. Completed case files unlock a short, optional connecting note in the Annex hub that supports one of the four theories.

---

## 15. Horror escalation and pacing

Pacing is **contrast**: long quiet and beautiful, then wrong, then quiet again.

- **Quiet time is a requirement:** at least 90 s between any two manifestations in phases up to Presence, at least 3 minutes of no manifestation after Aftermath. This replaces the current 45 s gap (kept only for Hunt).
- **Jumpscares:** at most one per level, never in a phase before Presence, never when the player is blocked, and always captioned.
- **Escalation order:** environment (absence, then wrongness) → sound → distant sight → trace → presence → rule broken → hunt. Entity sightings come after the player has read at least two legend entries.
- **Anticipation beats:** the strongest moment per level happens before the entity appears (the phone that rings in the wrong room, then in another room).
- **Aftermath:** music stops, ambience thins, the player is allowed to be alone with the space.
- **Wrongness budget:** at most N anomalies visible at once, as set per level.

---

## 16. Implementation plan for the existing project

### Presentation

Keep **first person, no VHS/body-cam**. Reasons: the controller, touch controls, interaction and tests already exist; first person gives the best scale and "something behind me" fear; VHS imposes a recognisable visual identity that would compete with the project's own (clean, bright, institutional). A **diegetic CCTV monitor** can be used as a prop for evidence (sightings you can only see on screen), not as the camera.

### New systems (plain C# core, thin MonoBehaviours, EditMode tests first)

| System | Purpose | Notes |
|---|---|---|
| `LevelDefinition`, `LiminalityProfile` | Per-level data (section 6) | ScriptableObjects; a validator test |
| `EntityPhase` + `LegendProgress` | Phase machine and evidence (sections 7, 8) | Pure C#, serialised in the save |
| `EntityDefinition`, `RuleDefinition`, `LegendEntry` | Authored content | ScriptableObjects |
| `RuleSystem` | Evaluate rules, violations, mutation | Pure core plus conditions as small classes |
| `EntityDirector` | Pick manifestations by phase, attention, budget | Generalises `TensionDirector`/`EventPicker`; those stay as inputs |
| `ManifestationSpot` | Scene binding of a manifestation | Evolves from `HorrorEventSpot`; keep the accessibility code and save of one-shots |
| `AttentionService` | Is an object in view (frustum + raycast), for how long | Shared by manifestations and rules |
| `AnomalyDirector`, `RoomSnapshot` | Authored anomalies and memory (section 9) | Pure core |
| `MusicDirector`, `ExperienceState` | Layered music states (section 5) | Reuses `AudioDirector` voices; `MusicDroneModel` becomes one input |
| `AmbienceDirector` | Per-zone beds, room tone, silence beats | Zones from `LiminalityProfile` |
| `ImpossibleTransition`, `LevelLoader` | Additive scene loads, match-cut transitions, checkpoint | Builds on `SceneFlow` and `SaveGame` |
| `HuntBehaviour` interface and per-entity implementations | Rule-based hunts | One class per entity, built per level |

### Existing systems to modify

- `HorrorEventRunner` and `HorrorEventSpot`: generalise or wrap; keep events as a manifestation kind; raise default gaps (section 15).
- `SaveGame`/`SaveData`: add entity phase, found legend entries, anomaly states, current level; keep the fired one-shots.
- `JournalModel`/`JournalView`: legend entries grouped per entity; the two-column "what people say / what I saw".
- `CueCatalog`: add cue groups per level and entity; keep the caption rule test.
- `LevelBootstrap`: level definition driven; transitions; checkpoints per beat.
- `SettingsApplier`: add quality tier for the large-space features.
- Editor builders: replace `LevelB1Builder` primitive building with a **kit-based level assembly tool**; keep the prologue builder for Floor B1.

### Assets needed

- **Architecture kit:** modular walls, floors, ceilings, columns, stairs, escalators, balconies, windows, glass, atrium trims, elevator shaft pieces. Candidate sources for CC0 or permissive kits are Kenney and similar libraries; each must be verified and entered in doc 09 before use. Without real kit art the redesign cannot look beautiful; this is the largest dependency.
- **Textures:** institutional tile, carpet, wallpaper, ceiling panel, concrete, wood; trim sheets and decals; signage atlas.
- **Props:** reception desk, luggage cart, vending machine, phone, mirror frames, chairs, lockers, desks, school items, pool lane ropes, shopping carts, seats.
- **Audio:** per-level room tones, 3 architectural sounds each, music stems (pad, warmth, dissonance, low, rhythm) per level, entity sounds, announcements. The CC0 set currently in the project is placeholder-grade.
- **Shaders:** cheap water, fog, emissive sign, fake mirror, dust motes.
- **Fonts:** at least one signage font under an OFL licence.

### State machines needed

`EntityPhase` (section 7), `ExperienceState` / music state (section 5), `LevelBeat` sequence (section 10), `RuleSystem` (active, violated, mutated), per-entity `HuntBehaviour` (Idle, Stalk, Observe, Strike, Lose), `TransitionState` (Idle, Entering, Loading, Matched, Arrived).

### Data structures

`LevelDefinition`, `LiminalityProfile`, `EntityDefinition`, `RuleDefinition`, `LegendEntry`, `AnomalyDefinition`, `ManifestationDefinition`, `RoomSnapshot`, `ExperienceState`, `LegendProgress`. All localisation keys, all authored as assets, so content changes need no code.

### Reusable versus per level

- **Reusable:** all of the systems above, the music and ambience directors, anomaly system, legend system, transitions, journal, save, captions, accessibility, tests.
- **Per level:** `LevelDefinition` and its profile, the architecture kit assembly and scene, the entity definition and its `HuntBehaviour`, legend entries, anomalies, music stems and ambience zones, transitions.

### Proposed order of work (spec first, per CLAUDE.md)

1. **Slice spec (F-13) in doc 05:** one large Hotel Meridian level with The Guest, using all frameworks. This is the only level specified now.
2. **Framework in plain C# with EditMode tests:** `EntityPhase`, `LegendProgress`, `RuleSystem`, `AttentionService`, `EntityDirector`, `AnomalyDirector`, `RoomSnapshot`, `MusicDirector` state logic, `LevelDefinition` validator. No scene work yet.
3. **Greybox the hotel at real scale** (atrium, wings, vista) to judge scale and sightlines before any art.
4. **Environment art pass** once the greybox reads (kit, textures, light, fog, signage).
5. **Entity, legend, anomalies and rules content for Hotel Meridian.**
6. **Music and ambience content for the slice.**
7. **Playtest the slice** (owner). Decide whether to continue to level 2.

Existing F-10 work merges first (docs 05 and plan). F-11 (the Indexer) is reframed: it becomes entity 10 in this framework, not a standalone feature, pending your decision D4.

### Risks

- **Art dependency:** kit-based beauty needs real assets; primitives will not deliver it.
- **Mobile performance:** large bright spaces; mitigated by the section 3 techniques and a profile pass on a device (none exists yet).
- **Scope:** ten levels; mitigated by the slice-first order and data-driven levels.
- **Fairness:** rule mutation and watched/unwatched mechanics need telegraphing; each needs a playtest.
- **Accessibility:** reflections, flicker and sudden changes must stay within the flash budget and Reduce Motion rules.
