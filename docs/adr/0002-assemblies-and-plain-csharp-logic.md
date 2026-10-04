# ADR 0002 — One assembly per area; testable logic in plain C# classes

Status: Accepted (retroactive, 2026-10-04)

## Context
Compile time, dependency direction and testability matter for a long-lived project and for small teams working in parallel.

## Decision
Separate assembly definitions per area (Core, Controls, Player, Interaction, Inventory, Puzzle, Save, Editor, tests). References point one way, with no cycles. Logic that does not need Unity lives in plain classes (`InventoryModel`, `CodeLockModel`, `StaminaModel`, `SaveSerializer`) and is covered by EditMode tests; MonoBehaviours stay thin.

## Consequences
- Fast, deterministic unit tests; PlayMode tests only for integration.
- Slightly more boilerplate (asmdef per area, event wiring).
- Cross-area features use small interfaces (`IKeyProvider`, `IItemReceiver`) instead of direct references.
