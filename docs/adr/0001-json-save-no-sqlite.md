# ADR 0001 — JSON file for saves; no SQLite or backend

Status: Accepted (retroactive, 2026-10-04)

## Context
The game stores a checkpoint id, a list of item ids and a few puzzle flags in one slot. The brief says to add a database or backend only with a clear requirement.

## Decision
Use a versioned JSON file in `persistentDataPath`, written atomically (temp file, verify, replace). No SQLite, no cloud, no accounts.

## Consequences
- Simple, offline, no extra dependency or privacy surface.
- Schema changes need a version bump and a migration step (`SaveSerializer` already refuses unknown versions explicitly).
- Revisit if save data grows large or needs querying, or if cloud save is approved.
