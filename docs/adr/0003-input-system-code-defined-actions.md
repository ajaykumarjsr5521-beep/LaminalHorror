# ADR 0003 — Unity Input System with code-defined actions behind one input state

Status: Accepted (retroactive, 2026-10-04)

## Context
The game needs keyboard/mouse, gamepad and touch with identical gameplay. Version 1.11.2 of the Input System package did not compile on Unity 6000.6.4f1; 1.20.0 does.

## Decision
Use the Input System package pinned at 1.20.0. Define actions in code (`DeviceInputSource`) and expose a platform-neutral `PlayerInputState` through `InputRouter`. Touch widgets write into a `TouchInputSource`. Gameplay never reads devices directly.

## Consequences
- No input-actions asset to drift out of sync; bindings are visible in code review.
- Rebinding UI will need a custom layer (excluded from MVP).
- Package upgrades must be re-verified by compile and tests; pinned versions are recorded in `Packages/packages-lock.json`.
