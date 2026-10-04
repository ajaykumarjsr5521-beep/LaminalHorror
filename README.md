# Nocturne Annex (working title)

Original first-person horror-adventure for Android (Google Play) and Windows. Unity 6000.6.4f1, URP, C#.

Status: **pre-alpha, logic layer under construction.** See [docs/12-progress-log.md](docs/12-progress-log.md) for what works and what is verified.

## Documentation
| Doc | Purpose |
|---|---|
| [docs/01-product-brief.md](docs/01-product-brief.md), [02-GDD.md](docs/02-GDD.md) | Creative direction, game design |
| [docs/03-mvp-and-roadmap.md](docs/03-mvp-and-roadmap.md) | MVP scope, phases |
| [docs/04-technical-architecture.md](docs/04-technical-architecture.md) | Architecture |
| [docs/05-feature-specs.md](docs/05-feature-specs.md) | Feature specs, acceptance criteria, statuses |
| [docs/06-testing-and-performance-plan.md](docs/06-testing-and-performance-plan.md) | Test and performance plan |
| [docs/13-engineering-process.md](docs/13-engineering-process.md) | How we work: review, CI, standards, DoD |
| [docs/adr/](docs/adr/) | Architecture decision records |

## Getting started
1. Install Unity Hub and Unity **6000.6.4f1** with Windows Build Support (and Android Build Support for mobile).
2. Open this folder as a project in Unity Hub.
3. Run tests: `Tools/BuildScripts/run-tests.sh` (set `UNITY` to your Editor exe if not at the default path).
4. Builds: see [Tools/BuildScripts/README.md](Tools/BuildScripts/README.md). Release signing secrets are read from environment variables and are never committed.
5. Try the movement test level: open `Assets/_Project/Scenes/Greybox.unity` and press Play (WASD, mouse, Shift, Ctrl/C, E).

## Contributing
Read [docs/13-engineering-process.md](docs/13-engineering-process.md). Short version: one feature per branch, spec first, small Conventional Commits, PR with tests passing, update docs and the changelog.

## Assets and licences
Only original or properly licensed assets. Every third-party asset is registered in [docs/09-asset-ip-register.md](docs/09-asset-ip-register.md). No licence has been chosen for the code yet (all rights reserved by default).
