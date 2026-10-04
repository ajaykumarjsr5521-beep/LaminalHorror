# 08 — Privacy, Data, Accessibility & Google Play Release

Status: DRAFT. **Play policy specifics below are NOT yet verified against current official docs.** Each item marked VERIFY must be checked against Google Play Console Help / Android developer docs at the release gate; no compliance is claimed.

## Data & privacy (planned MVP position)
- Collects: no personal data. Stores locally: save file, settings. No network permission requested (`INTERNET` not declared unless billing/other SDK requires — re-check with Play Billing).
- No analytics, ads, auth, crash-reporting SDK in MVP. (Crash reporting via Play Console vitals only.)
- Privacy policy: still needed for Play listing — VERIFY whether required when no data collected; plan to publish a short hosted page regardless. Host location is a decision (D7).
- Data safety form: expected "no data collected/shared" — VERIFY after final dependency audit (including Unity's own SDK behaviour; check Unity's current data collection docs and the engine version's disclosures).
- Delete data: uninstall removes local data; in-game "Delete save" option.

## Accessibility (design commitments)
Captions, text size, reduce flicker/flash, reduce camera motion, scalable touch controls, colour-independent UI, content warning screen at first launch (fear themes, flashing lights, intense sounds), pause anywhere. Photosensitivity: flash limits per F-08.

**Status 2026-10-04:** implemented and tested on Windows (F-08). Not yet verified: flash rate of real lighting events (none exist yet), caption coverage of real audio (none yet), anything on a phone. No claim of compliance with any accessibility standard or store policy is made; WCAG AA contrast of the UI palette is tested, the rest is a design commitment until reviewed.

## Content rating
Complete the IARC questionnaire in Play Console honestly (horror themes, fear, mild violence implied). Outcome determines audience; do not target children. VERIFY Families/target-audience policy implications: target age group selected as 13+/18+ per D3.

## Android release checklist (all VERIFY where noted)
- [ ] Target API level meets Play's current requirement for new apps — VERIFY
- [ ] 64-bit (ARM64) build included — VERIFY
- [ ] Signed AAB (Play App Signing enrolled; upload key stored offline/outside repo)
- [ ] Unique package id, versionCode/versionName scheme
- [ ] Size limits (base module/asset delivery) — VERIFY
- [ ] Store listing: title (cleared), short/full description, icon 512², feature graphic, ≥2 phone screenshots, optional trailer
- [ ] Privacy policy URL — VERIFY necessity; Data safety form completed
- [ ] Content rating (IARC), target audience & content declarations, ads declaration ("no ads")
- [ ] Closed/internal testing track run — VERIFY whether new personal developer accounts must meet a minimum closed-test requirement before production
- [ ] Pre-launch report reviewed; crashes/ANRs fixed
- [ ] Licences/credits screen includes all required attributions
- [ ] Asset register: zero REQUIRES_REVIEW assets in build

## Windows
- [ ] x64 standalone build, smoke-tested on clean machine; zipped (installer optional); optional distribution channel decision (itch.io/Steam/Store) — D8.

## Known limitations / release blockers (live list)
1. Unity toolchain not confirmed (D1).
2. No devices confirmed (D6).
3. Title clearance pending.
4. Policy items above unverified.
