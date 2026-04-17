# StockSense (working title)

## Idea
An AI-powered stock researcher that profiles the user's financial situation and experience level, then delivers plain-English stock recommendations with sourced reasoning — no jargon, no noise.

## Who It's For
Everyday investors frustrated by stock research that assumes too much knowledge. People who've tried reading financial blogs and hit a wall of terminology. Users who want to make informed investment decisions without becoming finance experts first.

## Inspiration & References
- [Arta AI](https://artafinance.com/global/insights/meet-arta-ai-private-wealth-guided-by-ai-agents) — conversational profile-building, personalized investment plans. Reference for tone and depth.
- [Prospero.ai](https://www.prospero.ai/) — clean signal cards, low-friction UX. Reference for recommendation card design.
- [Fiscal.ai](https://fiscal.ai/) — comprehensive research platform. Reference for what NOT to build (too complex).

Design energy: Clean, snappy, interactive. TikTok-style immediacy — fast feedback, low friction, visually satisfying. Cards should feel alive, not like a spreadsheet.

## Goals
- Give everyday investors reliable, personalized stock recommendations without requiring financial literacy
- Explain the *why* behind every recommendation in plain English
- Show sources so users can trust the output
- Adapt the depth of explanation to the user's experience level
- Solve Amar's original frustration: stock research that's actually accessible

## What "Done" Looks Like
A working app where a user can:
1. Enter investment amount
2. Specify timeline and expected return
3. Indicate their investing experience level (adaptive — novice vs. experienced)
4. Receive a set of interactive recommendation cards, each showing:
   - Stock name and ticker
   - Plain-English recommendation with reasoning
   - Key signals used (analyst upgrades/downgrades, macro/political context, real-time data)
   - Sources cited (SEC filings, news, analyst reports)
   - Interactive expand/collapse for more detail

## What's Explicitly Cut
- **Portfolio tracking** — this is a research tool, not a portfolio manager
- **Buy/sell execution** — no trading functionality
- **Watchlists and price alerts** — out of scope for v1
- **Social features** — no sharing, comments, or community
- **Two separate app "flavors"** — replaced by adaptive experience-level detection within one UX
- **Account creation / auth** — session-based only for the hackathon build

## Loose Implementation Notes
- Claude API as the research and reasoning engine — prompt engineering to adapt output depth based on experience level
- Data sources to integrate: real-time market data API (Yahoo Finance or similar), SEC EDGAR for filings, news API for political/macro signals, analyst ratings feed
- Frontend: interactive card-based UI — expand on click, clean typography, mobile-friendly
- User profile built through a short onboarding funnel (4 questions), not a long form
- Experience level question drives prompt behavior: novice gets simpler language and more "why", experienced gets more data density
