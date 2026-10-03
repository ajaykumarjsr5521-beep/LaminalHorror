# 04 — Technical Architecture

Status: DRAFT

## Stack
- Unity 6 LTS (exact version pinned after install; recorded in `ProjectSettings/ProjectVersion.txt`)
- URP (Mobile-tuned renderer asset + PC renderer asset), C# 9, Unity Input System package, TextMeshPro, Unity Test Framework (EditMode + PlayMode), Cinemachine **not** used (simple custom camera).
- Persistence: JSON file in `Application.persistentDataPath` (versioned schema). **No SQLite** — data is a few dozen flags; SQLite unjustified.
- No networking, analytics, ads, billing, cloud, or auth packages.

## Folder structure
```
Assets/
  _Project/
    Scripts/
      Core/          (GameState, ServiceLocator-lite, Events)
      Player/        (PlayerMotor, PlayerLook, Stamina)
      Input/         (InputRouter, MobileControls)
      Interaction/   (IInteractable, Interactor, Door, Pickup, Note)
      Inventory/     (Inventory, ItemDefinition)
      Puzzle/        (CodeLock, PuzzleState)
      Horror/        (TensionDirector, HorrorEvent, Actions)
      AI/            (Indexer — conditional)
      Save/          (SaveService, SaveData)
      UI/            (Menu, Pause, Settings, HUD, Captions)
      Audio/         (AudioService)
    Data/            (ScriptableObjects: items, events, settings defaults)
    Scenes/          (Boot, MainMenu, Level_B1)
    Art/ Audio/ Prefabs/ Materials/ Lighting/
  Tests/EditMode/  Tests/PlayMode/
docs/
Tools/BuildScripts/ (editor build menu for APK/AAB/Win)
```
Assembly definitions per top-level folder (Core, Gameplay, UI, Tests) to keep compile times low and dependencies one-directional: UI → Gameplay → Core.

## Key design decisions
- **Input:** Input System action asset with `Keyboard&Mouse`, `Gamepad`, `Touch` schemes. `InputRouter` exposes a platform-neutral struct (Move, Look, Sprint, Crouch, Interact…). Mobile on-screen controls feed the same router. Gameplay code never reads devices directly.
- **Interaction:** `IInteractable` + raycast/sphere-cast from camera; prompt text from the interactable. On mobile, prompt is a tappable button.
- **State:** a small `GameState` (scene flags, inventory, checkpoint id, settings) serialisable to JSON; events via C# events/ScriptableObject channels only where decoupling pays off.
- **Horror events:** ScriptableObject `HorrorEvent` with trigger + action list; `TensionDirector` MonoBehaviour gates firing.
- **Scenes:** Boot → MainMenu → Level_B1 (single scene, additive sub-scenes only if memory demands).
- **Save:** write to temp file then atomic replace; schema `version` field; failure surfaces a visible error (no silent fallback); corrupt save → offer "start new" without deleting the corrupt file.
- **Pooling:** SFX sources, decals/particles (small counts).
- **Settings:** sensitivity, invert-Y, volume buses, captions, controls scale/layout, graphics preset (Low/Med/High — shipped only after device validation), Story mode.

## Mobile rendering budget (targets — validated in doc 06)
≤150 draw calls (batched), ≤200k tris visible, ≤2 realtime lights (flashlight + 1 optional), baked GI lightmaps ≤2048 atlases ASTC, texture max 1024, audio Vorbis/ADPCM compressed, 30/60 fps selectable, GC alloc ≈ 0 per frame in gameplay.

## Build targets
Android: IL2CPP, ARM64, minimum API level **to be set after verifying Play's current requirement and device reach (doc 08)**, AAB for release, APK for dev. Windows: Mono or IL2CPP x64 standalone.

## Security/secrets
Keystore and passwords kept **outside the repo** (user-level env vars / local untracked file); `.gitignore` excludes `*.keystore`, `*.jks`, `keystore.properties`, `Library/`, `Temp/`, `Builds/`.
