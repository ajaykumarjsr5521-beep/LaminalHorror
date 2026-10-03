# 09 — Third-Party Asset & IP Register

Rule: Anything not clearly original or properly licensed is `REQUIRES_REVIEW` and **excluded from release builds**. Release gate: zero `REQUIRES_REVIEW` rows in shipped content.

## Status values
CLEARED · REQUIRES_REVIEW · ORIGINAL (made by team) · REJECTED

## Register
| ID | Asset / Package | Type | Source URL | Licence | Commercial use | Attribution required (text) | Modify/redistribute in build | Used in | Status | Reviewed by/date |
|---|---|---|---|---|---|---|---|---|---|---|
| UP-01 | Unity Input System | Package | Unity Registry | Unity Companion Licence/Package terms | Yes (to verify at install) | No | — | Input | REQUIRES_REVIEW | — |
| UP-02 | Universal RP | Package | Unity Registry | Unity package terms | Yes (to verify) | No | — | Rendering | REQUIRES_REVIEW | — |
| UP-03 | TextMeshPro | Package | Unity Registry | Unity package terms | Yes (to verify) | No | — | UI | REQUIRES_REVIEW | — |

(Rows for models, textures, SFX, music, fonts added as acquired. Greybox geometry and primitives: ORIGINAL.)

## IP originality checklist
- [ ] Title, character, entity and location names searched (trademark/store/web) before public use
- [ ] No designs derived from existing horror games/films (entity silhouette, level layouts, signature props)
- [ ] No copyrighted audio/fonts; fonts must be OFL/commercial-licensed
- [ ] Generated/AI-assisted assets (if any) recorded with tool, terms, and review status
- [ ] Credits screen matches register
