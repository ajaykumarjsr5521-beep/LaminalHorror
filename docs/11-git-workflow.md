# 11 — Git Workflow

> Commit rules (one commit per file, message format, hook) are in doc 13 section 3.
>
> Superseded in part by [13-engineering-process.md](13-engineering-process.md): merges to `main` go through reviewed pull requests with passing tests. Local merges were used up to F-06 (no PR tooling or branch protection existed yet).

- `main`: always in a reviewed, consistent state. No direct feature commits.
- Branches: `feature/F-XX-short-name`, `docs/...`, `chore/...`, `fix/...`. One feature per branch.
- Commits: small, imperative, Conventional-Commit style (`feat:`, `fix:`, `docs:`, `test:`, `chore:`). Spec/AC update goes in the first commit of the feature branch.
- Merge to `main` with `--no-ff` (PR if a remote exists) only after the feature's tests/build checks pass and its status in doc 05 is updated.
- Never commit keystores, credentials, `Library/`, build outputs (see `.gitignore`).
- Remote: `origin` = https://github.com/ajaykumarjsr5521-beep/LaminalHorror.git. Push every branch after committing.

## Phase → branch map
| Phase | Branches |
|---|---|
| P0 Setup | `feature/F-00-project-setup` |
| P1 Controller | `feature/F-02-input`, `feature/F-01-controller`, `feature/greybox-level` |
| P2 Interaction | `feature/F-03-interaction`, `feature/F-04-inventory`, `feature/F-05-puzzle` |
| P3 Systems | `feature/F-06-save`, `feature/F-07-menus`, `feature/F-08-accessibility` |
| P4 Horror | `feature/F-09-tension-director`, `feature/F-10-lighting-audio`, `feature/F-11-indexer` (if D4) |
| P5 Polish | `feature/perf-pass`, `feature/art-pass` |
| P6 Release | `feature/F-12-release` |

## Verification honesty
Until Unity is installed (D1), code can be written but not compiled or tested. Such work is marked `IN_PROGRESS (UNVERIFIED)` in doc 05 and cannot be DONE.
