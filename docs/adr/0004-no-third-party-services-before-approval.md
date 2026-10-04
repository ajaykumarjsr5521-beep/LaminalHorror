# ADR 0004 — No analytics, ads, billing, cloud or crash-reporting SDKs before approval

Status: Accepted (2026-10-04)

## Context
Such SDKs affect privacy disclosures (Google Play Data safety), age rating, consent flows, cost and store review. The project brief requires purpose, privacy impact, cost and release requirements to be specified first.

## Decision
None of these SDKs is added until a spec and an ADR describing purpose, data collected, consent, cost and policy checks exist and the owner approves.

## Consequences
- No telemetry: quality relies on local logs, playtests and device testing until approved.
- Monetization and crash-reporting decisions are tracked in docs 07 and 08 and the decision list (doc 10).
