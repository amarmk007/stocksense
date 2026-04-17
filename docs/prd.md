# StockSense — Product Requirements

## Problem Statement

Everyday investors who want to make informed stock decisions are blocked by jargon-heavy financial content that assumes prior knowledge they don't have. They either give up on research entirely or make uninformed decisions. StockSense gives them personalized, plain-English stock recommendations with cited reasoning — no financial literacy required.

## User Stories

### Epic: Authentication

- As a new visitor, I want to create an account with social auth so that my profile and preferences are saved across sessions.
  - [ ] A sign-up screen is shown to unauthenticated users
  - [ ] User can sign up using a social auth provider (e.g. Google)
  - [ ] After sign-up, user is directed to the onboarding questionnaire
  - [ ] If sign-up fails, an error message is shown in plain language

- As a returning user, I want to sign in with social auth so that I land directly on my dashboard with fresh recommendations.
  - [ ] Sign-in screen is shown to unauthenticated users
  - [ ] After successful sign-in, user is directed to the dashboard
  - [ ] If sign-in fails, an error message is shown in plain language

### Epic: Onboarding

- As a new user, I want to complete a short profile questionnaire so that the app can generate recommendations tailored to my situation.
  - [ ] Onboarding questionnaire is shown immediately after sign-up, before the dashboard
  - [ ] Questionnaire contains exactly 4 questions: investment amount, timeline, expected return, experience level
  - [ ] A "Get Recommendations" button is shown after all 4 questions are answered
  - [ ] Tapping "Get Recommendations" triggers recommendation generation and navigates to the dashboard
  - [ ] Onboarding is only shown once — returning users skip it

- As a new user, I want to set my investment amount using a slider so that I can quickly indicate how much I'm working with.
  - [ ] Slider range: $1,000 to $1,000,000
  - [ ] Slider increments in $10,000 steps
  - [ ] Current value is displayed as the user drags the slider

- As a new user, I want to set my investment timeline using a slider so that recommendations match my time horizon.
  - [ ] Slider range: 1 year to 10 years
  - [ ] Slider increments in 1-year steps
  - [ ] Current value is displayed as the user drags the slider

- As a new user, I want to set my expected return using a slider so that recommendations target my financial goals.
  - [ ] Slider range: 5% to 100%
  - [ ] Slider increments in 5% steps
  - [ ] Current value is displayed as the user drags the slider

- As a new user, I want to select my investing experience level from a picklist so that the app can adapt the depth of its explanations.
  - [ ] Picklist contains exactly two options: Novice and Experienced
  - [ ] Selection is required before "Get Recommendations" is enabled

### Epic: Recommendation Dashboard

- As a user, I want to see a set of stock recommendation cards on my dashboard so that I can quickly scan what the AI recommends for my situation.
  - [ ] Dashboard shows a minimum of 5 recommendation cards per session
  - [ ] Number of cards varies based on AI judgment
  - [ ] Cards are generated fresh every day via automatic daily refresh
  - [ ] While today's recommendations are loading for the first time (new user), a loading screen is shown with a friendly message
  - [ ] If recommendation generation fails (API/network error), an error message is shown in plain, non-technical language

- As a returning user who signs in before today's recommendations are ready, I want to see yesterday's recommendations with a notice so that I'm not left with a blank screen.
  - [ ] Previous day's recommendations are displayed
  - [ ] A banner or message clearly states that today's recommendations are not ready yet

- As a user, I want each recommendation card to show key summary information in its collapsed state so that I can quickly evaluate a stock at a glance.
  - [ ] Collapsed card shows: stock name, ticker symbol, potential upside percentage
  - [ ] Collapsed state is the same for Novice and Experienced users
  - [ ] Cards are interactive — tapping/clicking expands them

- As a user, I want to expand a recommendation card to see the full research reasoning so that I understand why the AI is recommending this stock.
  - [ ] Expanded card shows: plain-English reasoning, key signals, cited sources
  - [ ] Key signals include: analyst upgrades/downgrades, macro/political context, real-time market data
  - [ ] Sources are cited (SEC filings, news articles, analyst reports)
  - [ ] All items within the expanded view are sorted most recent first
  - [ ] For Novice users: reasoning uses simple language with more "why" explanation
  - [ ] For Experienced users: reasoning uses higher data density and more technical signal detail
  - [ ] Expanded card can be collapsed again

### Epic: User Profile

- As a returning user, I want to update my profile settings so that my recommendations reflect my current financial situation.
  - [ ] A profile page is accessible from the dashboard
  - [ ] Profile page shows current values for all 4 onboarding inputs (investment amount, timeline, expected return, experience level)
  - [ ] User can edit any of the 4 fields using the same slider/picklist controls as onboarding
  - [ ] Saving changes does NOT immediately trigger new recommendations — changes take effect at the next daily refresh
  - [ ] Experience level change (Novice ↔ Experienced) follows the same rule — takes effect next day

## What We're Building

Everything needed for a complete, submittable hackathon project:

1. **Auth flow** — Social sign-in/sign-up. New users go to onboarding; returning users go to dashboard.
2. **Onboarding questionnaire** — 4 slider/picklist questions, "Get Recommendations" CTA.
3. **Recommendation dashboard** — Minimum 5 AI-generated cards, auto-refreshed daily. Handles loading, stale-data, and error states.
4. **Recommendation cards** — Collapsed view (name, ticker, upside) and expanded view (reasoning, signals, sources), experience-level adapted.
5. **Profile page** — Edit all 4 profile inputs; changes apply at next daily refresh.

## What We'd Add With More Time

- **Manual refresh button** — Let users request a fresh set on demand, not just once daily.
- **Watchlist** — Save specific stocks to follow between sessions.
- **Recommendation history** — View past days' recommendations to track changes over time.
- **More experience levels** — A middle tier ("Intermediate") between Novice and Experienced.
- **Notification** — Alert the user (email or push) when today's recommendations are ready.
- **Multiple social auth providers** — Expand beyond Google to Apple, GitHub, etc.

## Non-Goals

- **Portfolio tracking** — StockSense is a research tool, not a portfolio manager. Users do not track holdings here.
- **Buy/sell execution** — No trading functionality of any kind.
- **Price alerts and watchlists** — Not in v1; deferred to "more time" list.
- **Social features** — No sharing, comments, or community elements.
- **Real-time intraday updates** — Recommendations refresh once daily, not continuously throughout the trading day.

## Open Questions

- **Which social auth provider(s) to support at launch?** Google is the baseline assumption — confirm before /spec. *(Needs to be answered before /spec.)*
- **What data sources power the recommendations?** Scope doc mentioned Yahoo Finance, SEC EDGAR, a news API, and analyst ratings. These need to be confirmed available and accessible before /spec. *(Needs to be answered before /spec.)*
- **What time of day does the daily refresh run?** Affects the "not ready yet" UX — early morning before markets open, or overnight? *(Can wait until build.)*
- **How is "potential upside" calculated?** This number appears on the collapsed card — the AI needs a consistent way to derive and express it. *(Needs to be answered before /spec.)*
