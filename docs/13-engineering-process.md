# 13 — Engineering Process & Standards

Status: ACTIVE (supersedes the informal workflow in doc 11 where they differ)

## 1. Gap analysis (as of 2026-10-04)
| Area | Gap found | Resolution | Tracked as |
|---|---|---|---|
| Review | Branches were merged locally by the author; no PR or review | PR-only merges, template, CODEOWNERS; owner enables branch protection (section 3) | X-01 |
| CI | Tests run only on one machine | GitHub Actions workflow written, gated until a Unity licence secret exists (section 7) | X-02 |
| Repo basics | No README, CHANGELOG, tags | Added; semantic versioning and tags (section 6) | X-01 |
| Style | No `.editorconfig` or written C# rules | Added `.editorconfig` and rules (section 4) | X-01 |
| Definitions | No Definition of Ready/Done | Section 2 | X-01 |
| Decisions | Not recorded | ADRs in `docs/adr/`; first four written retroactively | X-01 |
| Tracking | Markdown statuses only | GitHub Issues from templates, one per feature/bug, linked to doc 05 ids | X-01 |
| Binary assets | No Git LFS | LFS rules must exist before the first art/audio commit (section 8) | X-03 |
| Localisation | Player-facing strings hardcoded in code | All new UI/prompt text goes through a string table; existing strings migrated | X-04 |
| Test policy | No flakiness / timing rules | Section 5 | X-01 |
| Bugs / releases | No severity scale or release checklist | Sections 6 and 9 | X-01 |

## 2. Definitions
**Definition of Ready (a feature may start):** spec in doc 05 has purpose, scope and exclusions, dependencies, design, acceptance criteria (measurable), test plan; any decision needing owner approval is resolved; a GitHub issue exists.

**Definition of Done (a feature may be marked DONE):**
1. All acceptance criteria tested, with results recorded (device criteria on a real device).
2. Unit/PlayMode tests added for new logic; full suite green locally and in CI.
3. No new compiler warnings introduced; no `Debug.Log` left in hot paths.
4. No per-frame GC allocation in gameplay paths (profiler check for gameplay features).
5. Player-facing strings use the string table (once X-04 lands).
6. Docs updated: doc 05 status and notes, doc 12 progress log, CHANGELOG entry, ADR if a significant decision was made.
7. PR reviewed and approved; merged by squash or merge commit after CI passes.
8. Third-party assets logged in doc 09; unclear items marked REQUIRES_REVIEW and excluded.

A feature with logic done but device/UI criteria open stays **IN_PROGRESS** with the open items listed. That is the current state of F-00 to F-06.

## 3. Branching, commits, review
- `main` is protected: PR required, at least 1 approval, CI green, no force-push, linear or merge-commit history.
- Branch names: `feature/F-XX-name`, `fix/short-name`, `docs/...`, `chore/...`.
- Commits: Conventional Commits (`feat`, `fix`, `docs`, `test`, `refactor`, `chore`, `perf`), one logical change each, body states test results. Spec/AC commit first on a feature branch.
- PRs use `.github/PULL_REQUEST_TEMPLATE.md`. Authors do not approve their own PRs.
- **Owner action required:** enable branch protection and "require pull request" on `main` in GitHub settings; until then the process is followed by convention only. Changes up to this point were merged locally with merge commits, without independent review.

## 4. Code standards (C#)
- Rules are encoded in `.editorconfig`. Namespaces `NocturneAnnex.<Area>`; one assembly per area (no circular references; UI to Gameplay to Core).
- Private fields `_camelCase`, public members PascalCase, constants PascalCase.
- No silent failure: invalid config logs an error or returns an explicit result (see `SaveSerializer`, `CodeLock`).
- No `FindObjectOfType` or `GameObject.Find` in gameplay loops; no allocations in `Update`.
- Gameplay logic that can be plain C# lives in plain classes (`InventoryModel`, `CodeLockModel`) and is unit-tested; MonoBehaviours stay thin.
- No dependencies added without an ADR (privacy, size, licence, maintenance).

## 5. Testing policy
- Test pyramid: many EditMode unit tests, fewer PlayMode integration tests, device/manual tests for feel, touch and performance.
- Run with `Tools/BuildScripts/run-tests.sh`. A test failure blocks merge.
- Time-based PlayMode tests (speeds, door rotation) use tolerances and are the first suspects for flakiness. A flaky test is fixed or quarantined within one working day, never ignored.
- Every bug fix includes a regression test where feasible.
- Test results are recorded in the PR and in doc 12; performance numbers are only recorded when measured on a named device.

## 6. Versioning, releases, bugs
- Semantic versioning `MAJOR.MINOR.PATCH`; tag `vX.Y.Z` on `main`. Android `versionCode` increases monotonically per upload. Pre-1.0 milestones: `0.1.0` = P1 playable greybox, and so on.
- `CHANGELOG.md` follows Keep a Changelog.
- Bug severity: **S1** crash/data loss/progress blocker, fix before any build is shared; **S2** major feature broken, fix before milestone; **S3** minor, schedule; **S4** polish.
- Release gates are in doc 08 and doc 03; F-12 owns the release checklist.

## 7. CI
- `.github/workflows/tests.yml` runs EditMode + PlayMode on PRs using GameCI. It is **disabled** until the repo variable `UNITY_CI_ENABLED=true` and the secrets `UNITY_LICENSE`, `UNITY_EMAIL`, `UNITY_PASSWORD` exist (Personal licence activation needed, owner action). Risk: a GameCI image for Unity 6000.6.x may lag behind; verify before enabling.
- Until enabled, the local test run output is pasted in each PR.
- Later: build artifact job for Windows, Android build job (needs Android module in image), secret scanning.

## 8. Assets and repository hygiene
- Git LFS for `*.png *.jpg *.psd *.fbx *.blend *.wav *.ogg *.mp3 *.mp4 *.unitypackage` must be configured **before** the first asset commit (X-03). Owner must install Git LFS (`git lfs install`).
- Naming: `PascalCase` for Unity assets, `snake_case` for item/puzzle ids stored in data.
- Every third-party asset is logged in doc 09 at import time.
- No secrets in the repo; keystore stays outside (`NA_KEYSTORE_*` env vars).

## 9. Risk and decision logs
- Decisions: `docs/adr/NNNN-title.md` (context, decision, consequences).
- Risks and blockers: doc 12 "Known blockers / risks", reviewed at each milestone.
- Privacy-sensitive additions (analytics, ads, billing, crash reporting) need an ADR and owner approval first.
