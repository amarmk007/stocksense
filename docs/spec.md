# StockSense — Technical Spec

## Stack

| Layer | Technology | Rationale |
|---|---|---|
| Frontend | React 18 + Vite + TypeScript | SPA with component ecosystem for interactive card UX |
| Styling | Tailwind CSS + Framer Motion | Utility-first styling + card expand/collapse animations |
| Backend | ASP.NET Core Web API (.NET 9) | Learner's home stack; mature auth + EF Core ecosystem |
| ORM | Entity Framework Core 9 | First-party .NET ORM with PostgreSQL provider |
| Database | PostgreSQL 16 | Relational, free tier on Railway |
| Auth | ASP.NET Core Identity + Google OAuth | Built-in .NET social auth; no third-party auth service needed |
| AI | Anthropic C# SDK (`Anthropic` v12.11.0) | Official SDK, .NET 8+ target |
| Market Data | Finnhub REST API | 60 free calls/min; covers quotes, news, analyst ratings |
| SEC Filings | SEC EDGAR REST API | Free, no API key; public government data |
| Background Jobs | Hangfire + PostgreSQL storage | Persistent job queue; daily refresh scheduler |

**Documentation links:**
- [ASP.NET Core 9 docs](https://learn.microsoft.com/en-us/aspnet/core/?view=aspnetcore-9.0)
- [Anthropic C# SDK](https://github.com/anthropics/anthropic-sdk-csharp) — [API reference](https://platform.claude.com/docs/en/api/sdks/csharp)
- [Finnhub API docs](https://finnhub.io/docs/api)
- [SEC EDGAR REST API](https://efts.sec.gov/LATEST/search-index?q=%22form+type%22&dateRange=custom)
- [Entity Framework Core](https://learn.microsoft.com/en-us/ef/core/)
- [Hangfire docs](https://docs.hangfire.io/en/latest/)
- [Google OAuth in ASP.NET Core](https://learn.microsoft.com/en-us/aspnet/core/security/authentication/social/google-logins)

---

## Runtime & Deployment

- **Target:** Deployed URL (Railway + Vercel)
- **Backend:** Railway service — ASP.NET Core API, auto-detected via Nixpacks
- **Database:** Railway PostgreSQL plugin (same project)
- **Frontend:** Vercel — React static site, CORS configured to allow Railway backend origin
- **Environment variables required:**

```
ANTHROPIC_API_KEY        # From console.anthropic.com
FINNHUB_API_KEY          # From finnhub.io
GOOGLE_CLIENT_ID         # From Google Cloud Console
GOOGLE_CLIENT_SECRET     # From Google Cloud Console
DATABASE_URL             # Injected by Railway PostgreSQL plugin
JWT_SECRET               # Random 256-bit secret
CORS_ORIGIN              # Vercel frontend URL (set after frontend deploy)
HANGFIRE_DAILY_CRON      # "0 5 * * *" = midnight ET (UTC-5)
```

---

## Architecture Overview

```
┌─────────────────────────────────────────────────┐
│                  React SPA (Vercel)              │
│  Auth.tsx  │  OnboardingFlow.tsx  │  Dashboard   │
│                 ProfilePage.tsx                  │
└────────────────────┬────────────────────────────┘
                     │ HTTPS + JWT Bearer
                     ▼
┌─────────────────────────────────────────────────┐
│           ASP.NET Core Web API (Railway)         │
│  AuthController  │  ProfileController            │
│  RecommendationsController                       │
│                                                  │
│  ┌──────────────────────────────────────────┐   │
│  │           Background Services            │   │
│  │  Hangfire scheduler → DailyRecommJob     │   │
│  │    ├─ FinnhubService (market data)       │   │
│  │    ├─ EdgarService (SEC filings)         │   │
│  │    └─ ClaudeService (AI generation)      │   │
│  └──────────────────────────────────────────┘   │
└────────────┬───────────────────────┬────────────┘
             │                       │
             ▼                       ▼
┌─────────────────┐       ┌──────────────────────┐
│  PostgreSQL DB  │       │   External APIs       │
│  (Railway)      │       │  Finnhub REST API     │
│                 │       │  SEC EDGAR REST API   │
│  Users          │       │  Anthropic Claude API │
│  UserProfiles   │       │  Google OAuth         │
│  RecommSets     │       └──────────────────────┘
│  RecommItems    │
└─────────────────┘
```

---

## Authentication

Implements `prd.md > Authentication`

### Google OAuth Flow

1. React redirects unauthenticated users to `GET /api/auth/google`
2. ASP.NET Core redirects to Google consent screen via `Microsoft.AspNetCore.Authentication.Google`
3. Google returns to `GET /api/auth/google/callback`
4. Backend upserts the user record (create on first login, find on return)
5. Backend issues a **JWT** (15-minute expiry) + sets `IsOnboarded` flag in the token claims
6. Backend redirects to the React app with the JWT as a URL fragment (`#token=...`)
7. React extracts the token, stores it **in memory only** (no localStorage — avoids XSS)

### Silent Re-Auth

On page load/refresh, React checks for a JWT in memory. If absent or expired, it redirects to `/api/auth/google` automatically. The Google OAuth flow completes silently if the user already has an active Google session. No manual re-login required.

### JWT Middleware

All API endpoints except `/api/auth/*` require a valid JWT Bearer token. The `[Authorize]` attribute is applied globally; auth endpoints are marked `[AllowAnonymous]`.

### NuGet packages
- `Microsoft.AspNetCore.Authentication.Google` (v10.0.5)
- `Microsoft.AspNetCore.Authentication.JwtBearer`
- `Microsoft.AspNetCore.Identity.EntityFrameworkCore`

---

## Onboarding

Implements `prd.md > Onboarding`

### React Flow

4-screen step sequence (one question per screen, TikTok-style transition between steps):
1. **Investment amount** — range slider, $1,000–$1,000,000, $10,000 increments
2. **Timeline** — range slider, 1–10 years, 1-year increments
3. **Expected return** — range slider, 5%–100%, 5% increments
4. **Experience level** — two-option picklist: Novice / Experienced

"Get Recommendations" CTA enabled only after all 4 inputs are set.

### API Call

On CTA tap: `POST /api/profile` with all 4 values → backend saves `UserProfile`, flips `IsOnboarded = true`, enqueues a Hangfire job immediately for this user.

### Loading State

React polls `GET /api/recommendations/status` every 3 seconds. Backend returns `{ status: "pending" | "ready" }`. When ready, React navigates to the dashboard. Loading screen shows friendly message: "Analyzing markets for you..."

### Guard

Onboarding route is protected: if `IsOnboarded = true` in JWT claims, redirect to dashboard. Onboarding is shown exactly once.

---

## Recommendation Engine

This is the core of StockSense.

### Daily Hangfire Job

Cron: `0 5 * * *` (midnight ET = 05:00 UTC)

`DailyRecommendationJob` iterates all users sequentially:

```
for each user:
  1. Fetch market data (FinnhubService)
  2. Fetch SEC filings (EdgarService)
  3. Build prompt payload
  4. Call Claude API (ClaudeService) → structured JSON
  5. Save new RecommendationSet + RecommendationItems to DB
  6. Mark previous set as stale
  delay 1000ms between users  ← avoids Finnhub rate limit
```

The same job is enqueued immediately on new user sign-up (first-time recommendations).

### FinnhubService

Calls made per user:
- `GET /quote?symbol={ticker}` — current price
- `GET /company-news?symbol={ticker}&from={yesterday}&to={today}` — recent news
- `GET /stock/recommendation?symbol={ticker}` — analyst ratings

Tickers are not hardcoded — Claude selects them based on the user's profile. The FinnhubService is called *after* Claude returns the recommended tickers in a first-pass call (see ClaudeService below).

**Base URL:** `https://finnhub.io/api/v1`  
**Auth:** `?token={FINNHUB_API_KEY}` query param

### EdgarService

After Claude selects tickers, fetch recent filings for each:
- `GET https://efts.sec.gov/LATEST/search-index?q="{ticker}"&dateRange=custom&startdt={30 days ago}&enddt={today}&forms=8-K,10-K`

Returns filing summaries. Extract title + filing URL for sources.

**No API key required.**

### ClaudeService

**Two-pass Claude call:**

**Pass 1 — Ticker Selection**
```
System: You are a stock research analyst. Based on the user's profile, 
        select 5-8 stocks to research. Return ONLY a JSON array of tickers.

User: Profile: { investmentAmount, timeline, expectedReturn, experienceLevel }
      Today's date: {date}

Response: ["AAPL", "MSFT", "NVDA", ...]
```

**Pass 2 — Full Recommendation Generation**
Uses Claude tool use to enforce structured output.

Tool schema:
```json
{
  "name": "submit_recommendations",
  "description": "Submit the final stock recommendations",
  "input_schema": {
    "type": "object",
    "properties": {
      "recommendations": {
        "type": "array",
        "items": {
          "type": "object",
          "properties": {
            "ticker":          { "type": "string" },
            "name":            { "type": "string" },
            "upside_estimate": { "type": "string", "description": "e.g. '+18%' or '-5%'" },
            "reasoning":       { "type": "string" },
            "signals": {
              "type": "object",
              "properties": {
                "analyst": { "type": "array", "items": { "type": "string" } },
                "macro":   { "type": "array", "items": { "type": "string" } },
                "market":  { "type": "array", "items": { "type": "string" } }
              }
            },
            "sources": {
              "type": "array",
              "items": {
                "type": "object",
                "properties": {
                  "title": { "type": "string" },
                  "url":   { "type": "string" }
                }
              }
            }
          },
          "required": ["ticker", "name", "upside_estimate", "reasoning", "signals", "sources"]
        }
      }
    }
  }
}
```

**Experience level prompt injection:**
```
// Novice
"Explain each signal in plain English. Avoid financial jargon. 
 Prioritize the 'why this matters to me' angle for every recommendation."

// Experienced
"Use precise financial terminology. Include data density — 
 P/E ratios, price targets, volume context. Assume full financial literacy."
```

**Model:** `claude-sonnet-4-6` (fast, cost-effective for batch jobs)

### Stale Data Handling

If a returning user hits `GET /api/recommendations` before the midnight job runs for that day, the API returns the previous set with `isStale: true`. React renders a banner: "Today's recommendations are being prepared — showing yesterday's."

---

## Dashboard

Implements `prd.md > Recommendation Dashboard`

### React Components

**`Dashboard.tsx`** — page-level component
- Fetches `GET /api/recommendations` on mount
- Handles 3 states: loading (first-time), stale (yesterday's data + banner), ready

**`RecommendationCard.tsx`** — single card
- Collapsed state: ticker, company name, upside estimate badge
- Expanded state: reasoning paragraph, signals (3 typed sections), sources list
- Expand/collapse via Framer Motion `AnimatePresence`
- Experience-level adaptation is in the text from Claude — no frontend branching required

### API Endpoint

`GET /api/recommendations`
```json
{
  "isStale": false,
  "generatedAt": "2026-04-16T05:02:00Z",
  "recommendations": [
    {
      "ticker": "NVDA",
      "name": "NVIDIA Corporation",
      "upsideEstimate": "+22%",
      "reasoning": "...",
      "signals": {
        "analyst": ["BofA raised price target to $950, maintains Buy"],
        "macro": ["AI infrastructure spending cycle continues through 2026"],
        "market": ["Volume 2.4x 30-day average; held $820 support on 3 tests"]
      },
      "sources": [
        { "title": "BofA Research Note – Apr 2026", "url": "https://..." },
        { "title": "NVIDIA 10-K Filing", "url": "https://www.sec.gov/..." }
      ]
    }
  ]
}
```

---

## User Profile

Implements `prd.md > User Profile`

### React

**`ProfilePage.tsx`** — accessible from dashboard nav
- Reads `GET /api/profile` on mount
- Same slider + picklist controls as onboarding
- `PATCH /api/profile` on save
- Confirmation message: "Changes will apply at tomorrow's refresh."
- No immediate recommendation re-generation on save.

### API Endpoints

- `GET /api/profile` — returns current UserProfile for authenticated user
- `PATCH /api/profile` — updates one or more profile fields; returns updated profile

---

## Data Model

### Users
| Column | Type | Notes |
|---|---|---|
| `Id` | `uuid` | PK |
| `Email` | `varchar(255)` | Unique |
| `GoogleId` | `varchar(255)` | From Google OAuth |
| `IsOnboarded` | `bool` | Default false |
| `CreatedAt` | `timestamptz` | |

### UserProfiles
| Column | Type | Notes |
|---|---|---|
| `Id` | `uuid` | PK |
| `UserId` | `uuid` | FK → Users.Id |
| `InvestmentAmount` | `int` | Dollars |
| `TimelineYears` | `int` | 1–10 |
| `ExpectedReturnPct` | `int` | 5–100 |
| `ExperienceLevel` | `varchar(20)` | `"Novice"` or `"Experienced"` |
| `UpdatedAt` | `timestamptz` | |

### RecommendationSets
| Column | Type | Notes |
|---|---|---|
| `Id` | `uuid` | PK |
| `UserId` | `uuid` | FK → Users.Id |
| `GeneratedAt` | `timestamptz` | |
| `IsStale` | `bool` | True once a newer set exists |

### RecommendationItems
| Column | Type | Notes |
|---|---|---|
| `Id` | `uuid` | PK |
| `SetId` | `uuid` | FK → RecommendationSets.Id |
| `Ticker` | `varchar(10)` | |
| `Name` | `varchar(255)` | |
| `UpsideEstimate` | `varchar(20)` | e.g. `"+18%"` |
| `Reasoning` | `text` | |
| `SignalsJson` | `jsonb` | `{ analyst: [], macro: [], market: [] }` |
| `SourcesJson` | `jsonb` | `[{ title, url }]` |
| `SortOrder` | `int` | Display order |

---

## File Structure

```
stocksense/
├── backend/
│   └── StockSense.API/
│       ├── Controllers/
│       │   ├── AuthController.cs          # Google OAuth + JWT issuance
│       │   ├── ProfileController.cs       # GET/PATCH /api/profile
│       │   └── RecommendationsController.cs # GET /api/recommendations + /status
│       ├── Data/
│       │   ├── AppDbContext.cs            # EF Core DbContext
│       │   └── Migrations/               # EF Core migration files
│       ├── Jobs/
│       │   └── DailyRecommendationJob.cs  # Hangfire job; orchestrates full pipeline
│       ├── Models/
│       │   ├── User.cs
│       │   ├── UserProfile.cs
│       │   ├── RecommendationSet.cs
│       │   └── RecommendationItem.cs
│       ├── Services/
│       │   ├── ClaudeService.cs           # Two-pass Claude API calls
│       │   ├── FinnhubService.cs          # Market data fetcher
│       │   └── EdgarService.cs            # SEC EDGAR filings fetcher
│       ├── DTOs/
│       │   ├── RecommendationResponse.cs  # API response shapes
│       │   └── ProfileDto.cs
│       ├── appsettings.json               # Non-secret config
│       ├── appsettings.Development.json   # Local dev overrides
│       └── Program.cs                     # DI registration, middleware, Hangfire setup
│
├── frontend/
│   ├── src/
│   │   ├── api/
│   │   │   └── client.ts                  # Axios instance with JWT interceptor
│   │   ├── components/
│   │   │   ├── RecommendationCard.tsx     # Collapsed + expanded card with animations
│   │   │   ├── OnboardingFlow.tsx         # 4-step onboarding sequence
│   │   │   ├── ProfilePage.tsx            # Edit profile sliders + picklist
│   │   │   └── StaleBanner.tsx            # "Yesterday's recommendations" notice
│   │   ├── hooks/
│   │   │   ├── useAuth.ts                 # JWT in-memory store + silent re-auth
│   │   │   └── useRecommendations.ts      # Polling hook for /status
│   │   ├── pages/
│   │   │   ├── Dashboard.tsx              # Main card grid page
│   │   │   └── Auth.tsx                   # Login / redirect to Google OAuth
│   │   ├── App.tsx                        # Route definitions + auth guard
│   │   └── main.tsx                       # Vite entry point
│   ├── index.html
│   ├── tailwind.config.ts
│   ├── vite.config.ts
│   └── package.json
│
├── docs/
│   ├── learner-profile.md
│   ├── scope.md
│   ├── prd.md
│   └── spec.md                            # This file
│
├── process-notes.md
└── README.md
```

---

## Key Technical Decisions

### 1. Two-pass Claude call instead of one
**Decision:** First call selects tickers; second call generates full recommendations with market data injected.  
**Why:** Sending market data for all possible stocks upfront would be an enormous prompt. Letting Claude pick tickers first keeps the context window manageable and reduces cost.  
**Tradeoff accepted:** Two API calls per user per day instead of one. Latency is acceptable in a background job.

### 2. Sequential user processing with 1-second delay
**Decision:** Daily job processes users one at a time with a 1-second delay between each.  
**Why:** Finnhub free tier is 60 calls/minute. Each user requires ~3 Finnhub calls. Sequential processing with a delay prevents rate limit errors entirely.  
**Tradeoff accepted:** Slower job completion for large user counts. Acceptable for a hackathon-scale app.

### 3. Experience-level adaptation in Claude's output, not in the frontend
**Decision:** The experience level prompt injection happens in `ClaudeService` — Claude writes Novice-appropriate or Experienced-appropriate text. The React card renders whatever text it receives with no branching.  
**Why:** Simpler frontend. All the intelligence stays in the AI layer where it belongs.  
**Tradeoff accepted:** If the prompt injection fails, all users get the same language level until the next daily refresh.

---

## Dependencies & External Services

| Service | Free Tier | Rate Limit | Key Required | Docs |
|---|---|---|---|---|
| Anthropic Claude API | $5 free credit | Per-model RPM limits | Yes | [docs](https://platform.claude.com/docs) |
| Finnhub | Unlimited calls | 60/min | Yes | [docs](https://finnhub.io/docs/api) |
| SEC EDGAR | Free | No stated limit | No | [docs](https://www.sec.gov/developer) |
| Google OAuth | Free | No stated limit | Yes (Client ID/Secret) | [docs](https://developers.google.com/identity/protocols/oauth2) |
| Railway (backend) | $5/month free credit | — | Account | [docs](https://docs.railway.app) |
| Railway (PostgreSQL) | Included in $5 credit | — | Account | [docs](https://docs.railway.app/databases/postgresql) |
| Vercel (frontend) | Free hobby tier | — | Account | [docs](https://vercel.com/docs) |

---

## Open Issues

1. **Ticker universe for Pass 1 Claude call:** Claude currently selects tickers without a curated list. It should be constrained to US-listed equities only to ensure Finnhub data is available. Add a system prompt note: "Only recommend US-listed stocks available on NYSE or NASDAQ."

2. **First-time user latency:** The two-pass Claude call + Finnhub fetches may take 20-30 seconds. The loading screen needs a clear progress indicator or estimated wait time to prevent users from thinking the app is broken.

3. **Hangfire dashboard exposure:** Hangfire includes a `/hangfire` admin dashboard. This must be secured (require admin role or remove entirely) before Railway deployment — leaving it open is a security risk.
