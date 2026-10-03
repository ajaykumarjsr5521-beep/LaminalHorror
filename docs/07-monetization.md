# 07 — Monetization Options & Recommendation

Status: DRAFT — **nothing is implemented; no SDK added. Decision D2 required before G3.**

| Model | Pros | Cons / risks | Fit |
|---|---|---|---|
| **A. Premium (one-time paid)** | No ads/data SDKs; best for immersion; simplest privacy; simplest Data safety | Lower discoverability; needs strong store page; no try-before-buy | Good |
| **B. Free demo + paid full unlock (one non-consumable IAP)** | Try before buy; honest; one clear purchase; no ads | Requires Play Billing integration (privacy/disclosure, testing); must keep demo valuable, not crippled | **Recommended** |
| C. Optional cosmetics | Respects gameplay | Little cosmetic appeal in a first-person game; art cost | Poor |
| D. Ads (banner/interstitial) | Free to play | Breaks immersion & tension; consent/privacy burden; kids-policy risk; conflicts with design pillars | Reject |
| E. Optional rewarded ads | Opt-in | Same SDK/privacy burden; reward design risk (must not sell progression) | Not for MVP |

## Recommendation
**B**: free demo (MVP level's first 2 spaces + one horror event) with a single, clearly priced "Unlock Full Game" purchase; or **A** if you prefer zero billing code initially. Either way:
- Nothing sold affects fairness, no random rewards, no timers, no energy, no nag screens, no ads.
- Demo progress carries over after purchase.
- Purchase button shows price and "one-time purchase"; restore purchases supported.
- Billing integration only after approval; requires Play Console merchant setup and tax profile (to verify).
- Pricing is your call; I suggest deciding after the playtest (G3).

Analytics: **none** in MVP. If wanted later, a privacy-first, opt-in, minimal design must be specified and approved first, paired with player-satisfaction measures (post-session survey, completion/fairness feedback) rather than time-on-app.
