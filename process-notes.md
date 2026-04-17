# Process Notes

## /onboard

- **Technical experience:** C#, full-stack background, intermediate-to-experienced level. AI coding agents appear to be new territory.
- **Learning goals:** Build something end-to-end; gain confidence directing AI as a coding collaborator.
- **Creative sensibility:** TikTok — signals preference for snappy, visual, low-friction UX with fast feedback loops.
- **Prior SDD experience:** Yes, informal planning before coding (outlines/notes). Comfortable with the concept.
- **Notable context:** Solutions architect background — strong systems thinking and tradeoff reasoning. Will likely engage well with architectural decisions in /spec.
- **Energy/engagement:** Concise and direct. Doesn't over-explain. Move at a brisk pace and match the energy.

## /scope

- **Core idea:** AI stock researcher — personalized recommendations based on investment amount, timeline, expected return, experience level
- **Origin:** Amar's personal frustration with jargon-heavy financial blogs inaccessible to common people
- **Key insight:** Two-flavor idea (technical vs. novice) was challenged and refined into one adaptive experience driven by an experience-level question
- **Differentiator:** Combines SEC filings + real-time data + political/macro news + analyst upgrades/downgrades — plus plain-English reasoning with sources
- **UX direction:** Interactive recommendation cards, clean and low-friction (TikTok sensibility)
- **What's cut:** Portfolio tracking, trading, social features, alerts, auth, separate app flavors
- **Deepening rounds:** 0 — learner chose to proceed after mandatory questions
- **Active shaping:** Learner drove direction throughout. Pushed back on two-flavor approach by proposing adaptive experience detection. Added political news and analyst reports unprompted — both strong differentiators. Concise and decisive; didn't need much prompting to move forward.

## /prd

- **Key change from scope:** Auth was explicitly cut in scope.md but Amar reinstated it — said he didn't want to remove it. Profile persistence and returning-user behavior followed naturally from that decision.
- **Surprising "what if" moments:** The stale-recommendations state (returning user signs in before daily refresh runs) wasn't in the scope at all — surfaced during PRD conversation. Amar had a clear answer immediately.
- **Strong opinions:** Daily auto-refresh (not manual) was Amar's call. Profile changes take effect next day, not immediately — consistent rule across all 4 fields including experience level.
- **Scope guard:** No creep issues. Amar's scope was already lean. Watchlist, history, and notifications went cleanly into "more time" without pushback.
- **Deepening rounds:** 0 — learner chose to proceed after mandatory questions.
- **Active shaping:** Amar drove all decisions. No passive acceptance — every answer was direct and specific. Notable specificity on slider mechanics ($10k increments, 1-year steps, 5% steps). Reinstating auth was a correction, not a suggestion from me.

## /spec

- **Stack decisions:** ASP.NET Core Web API (.NET 9) + React/Vite/TypeScript frontend. Learner's instinct — kept backend in C# comfort zone, chose React over Blazor for the richer component/animation ecosystem.
- **Deployment:** Railway (backend + PostgreSQL) + Vercel (frontend). Learner wanted a deployed URL.
- **Data sources confirmed:** Finnhub (prices, news, analyst ratings) + SEC EDGAR (filings, free/no key). Yahoo Finance dropped — Finnhub covers all needed signals in one API.
- **Potential upside:** Claude estimates it. Learner's call — cleaner than pulling a consensus price target from Finnhub.
- **Signal subtypes:** Learner immediately said to break signals into subtypes (analyst / macro / market). No hesitation — instinctive preference for structured data.
- **Learner confident:** Stack shape, deployment, auth flow, data model. All decisive, no back-and-forth.
- **Learner less certain:** Two-pass Claude call approach — this was proposed by agent; learner accepted without pushback but didn't originate it.
- **Deepening rounds:** 1 round, 5 questions. Surfaced: signal schema design, Finnhub rate limit strategy (sequential + delay), Claude prompt injection pattern (language-only adaptation), JWT in-memory + silent re-auth, midnight ET job timing.
- **Active shaping:** Learner drove all stack and data decisions. Agent drove architecture proposals (two-pass Claude call, sequential job processing, experience-level adaptation in Claude vs frontend). Learner accepted all proposals without pushback — no counter-proposals in this phase.

## /checklist

- **Build mode:** Autonomous — no verification checkpoints, no comprehension checks
- **Git cadence:** Commit after each item
- **Verification:** None — review at end
- **Total items:** 10, estimated 3–4 hours
- **Sequence rationale:** Scaffold → Data model → Auth → Onboarding → External APIs → Claude + Hangfire job → Dashboard → Profile → Deploy → Devpost. Each step unblocks the next; riskiest piece (ClaudeService + two-pass call) built at step 6 to leave time to pivot.
- **Deepening rounds:** 0 — learner answered "build all" and locked preferences in 4 words total
- **Active shaping:** None — learner delegated sequencing entirely. No pushback on order, no suggested regroupings. Most decisive handoff of the curriculum so far.
- **Submission planning:** Wow moment = personalized plain-English stock recommendations for everyday investors. Live deployment on Railway + Vercel. GitHub repo to be created as part of step 10. Screenshots planned: onboarding sliders, dashboard with collapsed cards, expanded card with reasoning/signals/sources.
