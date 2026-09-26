# AI Speaking Coach

🇬🇧 English · 🇺🇿 [O'zbekcha](README.uz.md)

[![CI](https://github.com/ElshodDev/speaking-coach/actions/workflows/ci.yml/badge.svg)](https://github.com/ElshodDev/speaking-coach/actions/workflows/ci.yml)

An English-practice app for Uzbek- and Russian-speaking learners (A2–C1), with the interface in **Uzbek, Russian or English**. You **speak, write, read and listen**; an LLM (Google Gemini) grades your work against a rubric and points out concrete mistakes — and every mistake becomes a **spaced-repetition flashcard** that comes back right before you would forget it, including in a hands-free **commute mode** for earphones.

**Live demo:** https://speaking-coach-theta.vercel.app
(The backend runs on a free tier and sleeps after 15 minutes of inactivity — the first request may take 30–60 s.)

| Home | Review card | Writing feedback | Dark mode |
|---|---|---|---|
| ![Home](docs/screenshots/home-user.png) | ![Review](docs/screenshots/review.png) | ![Writing](docs/screenshots/writing-result.png) | ![Dark mode](docs/screenshots/review-dark.png) |

| Progress & levels | Vocabulary | Tap a word | Admin panel |
|---|---|---|---|
| ![Progress](docs/screenshots/progress.png) | ![Vocabulary](docs/screenshots/vocab.png) | ![Word tap](docs/screenshots/word-tap.png) | ![Admin](docs/screenshots/admin.png) |

## Features

| | |
|---|---|
| 🎙 **Speaking** | Record yourself on a topic. Gemini transcribes the audio **and** grades fluency, grammar and vocabulary in a single multimodal call. |
| ✍️ **Writing** | Write an essay; graded on an IELTS-style rubric (task achievement, coherence & cohesion, grammar, vocabulary) with exact corrections. |
| 📖 **Reading** | Gemini generates a fresh passage and 4 multiple-choice questions every time. Answers are checked by the server, not the LLM. |
| 🎧 **Listening** | Same, but the browser reads the script aloud (Web Speech API); the text is revealed only after you answer. |
| 🔁 **Spaced repetition** | Corrections and wrong answers automatically become flashcards scheduled with SM-2 (1 day → 3 days → ~1 week …). Daily goal and 🔥 streak. |
| 🚌 **Commute mode** | Hands-free review with earphones: question → thinking pause → answer. Keeps the screen awake with the Wake Lock API. |
| 📱 **Installable (PWA)** | Add to home screen; opens like a native app and loads offline. Bottom navigation on phones, dark mode follows the system. |
| 👆 **Tap a word** | In Reading passages, tap any unknown word: Gemini explains it in Uzbek *in the context of that sentence* (part of speech, meaning, English definition, example), and one tap saves it to your vocabulary. |
| 📚 **Vocabulary** | Your personal word list: search by English word or Uzbek meaning, filter by *New / Learning / Known* (derived from the review schedule), look up any word by typing it, and review only your words. Every word is also a spaced-repetition card — no separate table. |
| 🎚 **Learner level (A2–C1)** | Passage length, vocabulary and feedback adapt to the chosen level; grades stay on an absolute rubric so progress is comparable. |
| 📈 **Progress** | XP, levels and 10 badges; an 84-day activity calendar; score trends per criterion (with a table view); your hardest cards. |
| 🏆 **Weekly competition** | Opt-in leaderboard by weekly XP under a nickname — email and exercises are never shown. |
| 🎓 **Mock exams (IELTS)** | Full-format **Speaking** (Part 1 — 4 questions read aloud, Part 2 — cue card with 1 minute to prepare and up to 2 minutes to speak, Part 3 — 4 questions; ≈11 minutes) and **Writing** (60-minute timer, Academic chart or General Training letter + Task 2 essay, live word count, draft survives a reload, auto-submit when time is up). One Gemini request scores the whole test on the four official criteria; the **server** computes the overall band (equal criteria weighting, Task 2 counted twice, .25/.75 rounding) and the result is labelled as an estimate. 3 mocks per 24 hours. Format checked against ielts.org; CEFR Multilevel is listed as "coming soon" until its format is confirmed from uzbmb.uz. |
| 🛠 **Admin panel** | For the owner only (`Admin:Emails`): signups, daily/weekly/monthly active users, exercises by type, reviews — aggregates and masked emails only. A **System status** card checks the database schema against the code (pending migrations, missing tables), the Telegram webhook (URL, pending updates, last error) and which optional features are on — without ever returning a secret. |
| 📲 **Telegram bot** | [@SpeakingCoachUzBot](https://t.me/SpeakingCoachUzBot): connect from Profile with one tap, then review due cards right in the chat, send any English word to get a translation and add it to your vocabulary, check your streak, and get **one** daily reminder at the hour you choose (skipped if you've already hit today's goal). Same database as the website, in Uzbek, Russian or English. |
| 🌐 **Three languages** | The whole interface, server error messages and word translations switch between Uzbek (official Latin orthography), Russian and English. Detected from the browser, switchable in the header and in Profile. |
| 🔐 **Accounts** | Register / log in; your history and cards are private. The email is **verified with a 6-digit code** (so only real, owned addresses get accounts), and a forgotten password is reset with the same code. **Sign in with Google** is available too. Everything also works as a guest (nothing is saved). |

## Architecture

```
Browser (React + TypeScript, Vite, PWA — Vercel)
   │  Authorization: Bearer <token>
   ▼
ASP.NET Core 10 Minimal API (Docker — Render)
   │  Endpoints/  auth · speaking/writing · reading/listening · review
   │              profile · progress · leaderboard · admin · words · vocab
   │  Services/   GeminiClient (model fallback, retry, strict JSON parsing)
   │              evaluation & generation services, AuthService,
   │              ReviewScheduler (SM-2), ReviewCardFactory,
   │              ProgressCalculator (XP, levels, badges, ranking)
   │  Rate limiting per IP · /health
   ▼
Google Gemini                         PostgreSQL (Neon) via EF Core
 - speaking: audio → transcript+grade  Activities, Users, Sessions,
 - writing: text → grade               PendingExercises, ReviewCards,
 - reading/listening: generate         ReviewLogs
```

## Engineering decisions

Each of these is explained in more depth (in Uzbek) in [README.uz.md](README.uz.md) and in code comments.

- **One Gemini call for speaking.** Gemini accepts audio directly, so there is no separate speech-to-text step — a simpler architecture and one API call instead of two.
- **LLM output is never trusted blindly.** Replies are parsed with `RespectRequiredConstructorParameters` and `RespectNullableAnnotations`, then range-checked. The default `System.Text.Json` behaviour silently turned a missing `"score"` into `0` and saved it — a real bug this project hit and fixed.
- **The LLM generates, code grades.** For Reading/Listening the answer key is known, so grading is deterministic C#. The key is kept server-side (`PendingExercises`) and never sent to the browser.
- **Measuring LLM grading stability.** A built-in "stability test" re-grades the same input 5× (`?save=false`, sequential to respect rate limits) and shows min/max/spread per criterion — the honest answer to "how reliable is an AI grader?".
- **Email ownership is proven, not assumed.** A format check can't tell whether an address exists, so sign-up sends a 6-digit code (stored as a SHA-256 hash, 15-minute lifetime, 5 attempts, 60 s cooldown, 5 emails/hour, constant-time comparison). Unverified accounts can't log in, and re-registering an unverified address re-sends the code to its real owner, so nobody can squat on someone else's email. Emails go through Brevo's HTTPS API because Render's free tier blocks outbound SMTP ports (25/465/587) since September 2025. Verification switches on only when a Brevo key is configured, so deploying the code never locks anyone out.
- **Google sign-in without an auth library.** The browser gets a Google-signed ID token; the server verifies it itself (RS256 signature against Google's JWKS, `iss`, `aud` = our client ID, expiry, `email_verified`) using .NET's built-in RSA. Signing in with Google to an account whose email was never verified wipes the old password, since a stranger could have set it.
- **A Telegram bot on a free, sleeping server.** The bot uses a webhook (Telegram wakes the Render instance), verified with the `X-Telegram-Bot-Api-Secret-Token` header. A sleeping server can't run timers, so a GitHub Actions cron pings `/api/telegram/cron` hourly and the server sends whatever reminders are due — logic is "the hour has passed and none was sent today", so a delayed cron still works and nobody gets two messages. Linking uses a one-time `t.me/…?start=TOKEN` link (hashed, 15 min). No bot library: five Bot API calls over `HttpClient`, with the pure parts (callback encoding, reminder timing, menu parsing, Telegram JSON) unit-tested.
- **Daily AI quota per user.** Gemini's free quota is shared by everyone, so each account gets a daily budget (30 exercises, 100 word look-ups; guests 5/20 per IP, configurable via `Ai:*`). It is checked *before* calling Gemini and charged only *after* a successful answer, so a Gemini outage never eats a user's quota; the remaining budget is shown in the UI. Review and vocabulary never touch the AI and stay unlimited.
- **Your data, your control.** Profile → *Your data* downloads everything as JSON and deletes the account on the spot (confirmed by retyping the email); all rows cascade from the user, and speaking audio files are removed from disk.
- **Opaque session tokens instead of JWT.** 32 random bytes, stored only as a SHA-256 hash. Logging out revokes the token immediately — impossible with a plain JWT. Passwords use ASP.NET Core's `PasswordHasher` (PBKDF2).
- **Pure core logic.** `ReviewScheduler`, `ReviewCardFactory`, grading and parsing don't touch the database or HTTP, which is what makes them unit-testable.
- **Streaks use the user's local day**, not UTC — a review at 01:00 in Tashkent must not count as "yesterday".
- **Type-safe i18n without a library.** Each screen declares its texts once in Uzbek (`defineMessages(uz, { ru, en })`); the Russian and English objects must have the same shape, so a missing translation fails the TypeScript build. Plurals are plain functions (`ruPlural(n, 'слово', 'слова', 'слов')`). The server localizes its messages from the standard `Accept-Language` header; services return message keys, not text.
- **XP is derived, not stored.** Levels, badges and the weekly leaderboard are computed from `Activities` and `ReviewLogs` by a pure `ProgressCalculator`. There is no XP counter to drift out of sync, and changing the rules re-scores history automatically.
- **Privacy by default.** The leaderboard is opt-in and shows only a nickname; the admin panel shows aggregates and masked emails (`az***@mail.com`), never anyone's essays or cards.
- **Charts without a chart library.** Small hand-built SVG charts with a colour-blind-checked palette, tooltips, keyboard navigation and a table view for accessibility.
- **One `Activities` table with `jsonb` payloads** for all exercise types, so adding a new type needs no new table.
- **No UI framework.** A small design system of CSS variables (`src/styles.css`) gives consistent components and dark mode for free; routes are hash-based (`#/review`) so the phone's Back button works.

## Quality

- **Tests:** 228 backend unit tests (xUnit) — scheduler, streak across time zones, card creation, grading, strict parsing, token hashing, XP/levels/badges, leaderboard ranking, level-aware prompts, vocabulary status, localized messages (every key in all three languages with matching placeholders), email-code rules (expiry, attempts, cooldown, hourly cap), email templates Google ID-token validation (forged signature, wrong audience/issuer, expiry, `alg: none`), AI quota, the Telegram bot logic, the system-status rules and IELTS band maths (official rounding, Task 2 weighting, word count, mock limits, question-bank format) — and 49 frontend tests with Vitest (charts, word tapping, vocabulary search, plurals).
- **CI:** GitHub Actions builds and tests the backend and type-checks, tests and builds the frontend on every push.
- **Rate limiting:** 10 req/min per IP on login/register, 30 req/min on Gemini-backed endpoints; the real client IP is read from `X-Forwarded-For` behind Render's proxy (last hop only).
- **Health check:** `GET /health` reports API and database status (503 if the DB is unreachable).

## Tech stack (all on free tiers)

| Part | Technology |
|---|---|
| Frontend | React 18, TypeScript, Vite, CSS variables (no UI framework), PWA — Vercel |
| Backend | ASP.NET Core 10 Minimal API, EF Core, Docker — Render |
| Database | PostgreSQL — Neon |
| AI | Google Gemini (AI Studio key, no credit card) |
| Speech | Browser Web Speech API |
| CI | GitHub Actions |

## Run locally

Prerequisites: .NET 10 SDK, Node 22, a [Gemini API key](https://aistudio.google.com/apikey), a [Neon](https://neon.tech) Postgres database.

```bash
# backend
cd backend/SpeakingCoach.Api
dotnet user-secrets init
dotnet user-secrets set "Gemini:ApiKey" "<your key>"
dotnet user-secrets set "ConnectionStrings:Default" "Host=...;Database=...;Username=...;Password=...;SSL Mode=Require"
dotnet user-secrets set "Admin:Emails" "you@example.com"   # optional: who sees the admin panel
dotnet tool update --global dotnet-ef
dotnet ef database update
dotnet run                      # http://localhost:5000

# frontend (second terminal)
cd frontend/speaking-coach-web
npm ci
npm run dev                     # http://localhost:5173
```

Tests:

```bash
dotnet test backend/SpeakingCoach.Api.Tests
cd frontend/speaking-coach-web && npm test
```

## Deploy

- **Render** (backend): root directory `backend/SpeakingCoach.Api`, Docker; env vars `Gemini__ApiKey`, `ConnectionStrings__Default`, `FrontendOrigin` (the Vercel URL, for CORS), optionally `Admin__Emails` (comma-separated) and, to turn on email verification, `Email__BrevoApiKey` + `Email__FromAddress` (a sender verified in [Brevo](https://www.brevo.com); free plan: 300 emails/day), and `Google__ClientId` for Google sign-in (a Web OAuth client with the Vercel URL as an authorized JavaScript origin), and `Telegram__BotToken` + `Telegram__CronSecret` for the bot (plus GitHub secrets `API_URL` and `CRON_SECRET` for the reminder workflow).
- **Vercel** (frontend): root directory `frontend/speaking-coach-web`; env var `VITE_API_URL` (the Render URL).
- **Migrations** are applied with `dotnet ef database update` before pushing code that depends on them (Render does not run them).

## Known limitations

- Free tiers: Gemini rate limits; the backend sleeps when idle.
- No push reminders yet; commute mode needs the screen on (some phones stop audio when it turns off).
- The token lives in `localStorage` (cross-site cookies between `vercel.app` and `onrender.com` are increasingly blocked); an XSS bug could expose it.
- Verification emails from a free-mail sender address (e.g. Gmail) may land in Spam; authenticating your own domain in Brevo fixes that.
- Automated tests cover pure logic; database/endpoint integration tests are next.

## Roadmap

- [ ] Daily reminder via Web Push ("12 cards are waiting")
- [ ] Integration tests with `WebApplicationFactory` + Testcontainers Postgres
- [x] Progress page, XP/levels/badges, weekly leaderboard, admin panel
- [x] Learner level (A2–C1), tap-a-word and a personal vocabulary
- [x] Interface in Uzbek, Russian and English
- [x] Email verification and password reset
