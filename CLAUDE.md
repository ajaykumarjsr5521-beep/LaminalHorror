# Working rules for Claude (loaded every session, keep short)

## Start of every session
1. Read `plan.md` first. Work only on its current day; do not re-read the specs or roadmap unless the plan points there.
2. If the plan is stale or missing, rewrite it (max 30 lines) before coding, then continue.

## Spec and plan before development (every feature)
1. Write the spec in `docs/05-feature-specs.md` first: scope, exclusions, numbered acceptance criteria (AC), how each AC is tested, one-commit-per-file sub-steps. Commit it as the first commit of the branch.
2. Write the feature's task checklist in `plan.md` (small steps, in order, with the test for each).
3. Only then write code. If scope changes mid-way, update the spec and plan first.
4. Mark the spec DONE only with real test results; otherwise IN_PROGRESS (UNVERIFIED).

## Save tokens
- Search before reading: Grep/Glob, then Read with `offset`/`limit`. Never read a whole large file (scenes, .asset, logs).
- Never print generated files or logs. Use `grep -c`, `tail -n 20` or `grep -E "error CS|Failed"`.
- Batch independent tool calls in one turn. Chain shell steps into one command.
- Run the narrowest test first (`FILTER="Ns.Fixture" Tools/BuildScripts/run-tests.sh PlayMode`); the full suite only before a merge.
- Long runs go in the background; check the output once, do not poll.
- Do not re-verify what a tool already confirmed, or re-derive decisions already made. State results, not narration.
- Delegate wide searches to an Explore agent; keep only its conclusion.
- Ask the user only for decisions that are theirs; group questions in one call.
- Replies: short, results first. No recaps of what the user just saw.

## Project rules (details: docs/13-engineering-process.md section 3)
- One commit per file (a `.cs` plus its `.meta` counts as one). Subject `type(scope): summary`, 72 chars max. Commit as you go, push the branch.
- Never claim "tests pass" without running them; mark unverified work UNVERIFIED.
- Do not commit Unity auto-edits (`InputManager.asset`, TMP font asset) unless asked.
- Stop before merging to `main`; the owner reviews.
- Keep `plan.md`, `docs/12-progress-log.md` and doc 05 status current at the end of each day.

## Usage limit
- The owner wants all work stopped when the weekly usage limit reaches 50%. Claude cannot read that meter. When the owner says it is reached, or a usage warning appears, stop at once: commit what is done, update `plan.md`, and make no further tool calls.
