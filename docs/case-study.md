# Case study: building an AI speaking coach on free tiers

**Project:** [AI Speaking Coach](https://fluentuz.app) · [try the demo](https://fluentuz.app/#/demo) · [source](https://github.com/ElshodDev/speaking-coach)
**Stack:** ASP.NET Core 10 Minimal API, EF Core + PostgreSQL (Neon), React 19 + TypeScript (Vite, PWA), Google Gemini, Telegram Bot API
**Size:** about 31 000 lines of C# (a large share of it built-in exam content) and 27 000 lines of TypeScript; 759 backend unit tests, 170 frontend unit tests, 28 Playwright end-to-end suites

---

## The problem

In Uzbekistan, students preparing for IELTS or the national CEFR Multilevel exam can get plenty of grammar drills. What they rarely get is feedback on their *speaking*. A tutor who listens, corrects and explains costs money and time, so most learners practise speaking alone, if at all, and never find out which mistakes they keep repeating.

I wanted an app that closes that loop: you speak, write, read or listen; an AI grades the attempt against a rubric and names concrete mistakes; and every mistake becomes a flashcard that comes back just before you would forget it. The interface had to work in Uzbek, Russian and English, run well on a cheap phone and cost nothing to host.

The last requirement shaped more of the design than anything else.

## Constraints

- **Hosting:** Vercel for the frontend, a free Render instance for the API (it sleeps after 15 minutes without traffic and needs 30–60 s to wake), a free Neon Postgres database.
- **AI:** Gemini's free quota, shared by every user of the app.
- **Email:** Render's free tier blocks outbound SMTP, so verification emails go through Brevo's HTTPS API.
- **Me:** one developer, so the code had to stay simple enough to change safely.

## Decisions that paid off

### One multimodal call instead of a pipeline

The obvious design for speaking is speech-to-text first (Whisper or similar), then an LLM to grade the text. Gemini accepts audio directly, so a single call returns the transcript *and* the scores. That halved the API usage, removed a service and made latency easier to reason about. The trade-off is that I depend on one vendor for both steps; `GeminiClient` hides that behind one class, with a fallback to a lighter model when the main one answers 503 "high demand".

### Never trust the model's JSON

Early on, an attempt was saved with a score of 0 even though the answer deserved much more. Gemini had returned the field under a different name, and `System.Text.Json` did what it does by default: it filled the missing property with `0` and moved on. The wrong score went into the database and the progress chart without any error.

The fix is short: deserialize with `RespectRequiredConstructorParameters` and `RespectNullableAnnotations`, then range-check every score and reject empty explanations. A malformed reply now fails loudly, nothing is saved, and the user sees "try again" instead of a silently wrong grade. That one bug set a rule for the rest of the project: model output is untrusted input.

### Let the model write the test, let code grade it

For Reading and Listening the answer key is known, so there is no reason to ask a model whether an answer is right. Gemini generates the passage and questions; C# grades them. The key stays on the server and never reaches the browser.

The mock exams take this further. A full IELTS Reading or CEFR Listening test is expensive to generate, so each one is generated **once**, validated strictly and stored in a shared test bank. The validators check that questions are numbered correctly, gap-fill answers actually appear in the text, a map-labelling task has the right number of extra options, and so on. Each user gets a test they haven't taken yet, and the AI quota is spent only when the bank runs out.

Where the official scoring is not public (CEFR Listening and Reading are Rasch-scaled), the app maps raw scores onto the published "approximate correct answers" table and labels the result as an estimate. I would rather show an honest approximation than an invented precise number.

### Measure how reliable the grader is

"How consistent is an AI examiner?" is a fair question, so the app has a built-in stability test. It re-grades the same recording or essay five times without saving and shows the minimum, maximum and spread for each criterion. It doesn't make the model more reliable. It does make the uncertainty visible.

### Work with a server that sleeps

A sleeping server can't run timers, which is a problem for daily reminders. The Telegram bot uses a webhook, so an incoming message wakes the API. For reminders, a GitHub Actions cron job calls `/api/telegram/cron` every hour. The rule is "the user's reminder hour has passed and nothing was sent today", so a cron run that GitHub delays by twenty minutes still works.

The frontend follows the same idea: while the API is waking up, the app retries instead of logging the user out, and the demo page warns that the first request can take up to a minute.

### Ship the content, not just the AI

The first versions asked Gemini for everything: every reading passage, every listening script, every mock test. That made the app slow on the first click, dependent on a shared free quota, and empty whenever the model was busy. The app now ships its own original material: 88 graded reading and listening exercises, 44 full IELTS and CEFR Listening and Reading mock tests, 60 speaking and writing mock sets, 92 practice topics, 32 grammar lessons explained in three languages and 326 topic words. Every section lists its tests with a ✓ and the last score, so the learner picks what to do instead of getting whatever comes next. Wherever material could come from either source, the learner chooses between "📚 ready-made" and "✨ new by AI". The built-in content passes exactly the same validators as AI output, and unit tests fail the build if a passage is the wrong length for its level or a gap answer is missing from its text.

### Reuse the data before adding AI

Two of the later features cost no AI at all. **My mistakes** reads the corrections Gemini already returned for every speaking, writing, mock and conversation attempt, and a small deterministic classifier (a word-level diff plus keywords in the explanation, in three languages) sorts them into types such as articles, tenses and prepositions. It is approximate and says so, but it turns hundreds of scattered corrections into "your top three problems, and the lesson for each". **Dictation** checks typed sentences word by word in the browser. The one new AI feature, the **conversation partner**, keeps its history on the server and caps each conversation at eight turns, so a single conversation costs a known amount of quota.

### Keep the core logic pure

The spaced-repetition scheduler (SM-2), the card factory, IELTS band rounding, the CEFR 0–36 → 75 conversion, XP and badges, reminder timing and Telegram callback parsing never touch the database or HTTP. That is why 759 backend tests run in seconds and why most bugs could be reproduced as a failing unit test first.

XP is a good example. It is never stored: levels, badges and the weekly leaderboard are computed from `Activities` and `ReviewLogs`. There is no counter to drift out of sync, and when I changed the XP rules, all history was re-scored automatically.

## Things that went wrong, and what I changed

### A security audit of my own code

Once the feature list was long, I audited the whole project as if someone else had written it. That found real problems:

- **Account takeover through unverified sign-ups.** If someone registered with *your* email before you did, then re-registering overwrote the password on the unverified account. The attacker could set their own password right after you confirmed the code. Now the password is stored only when the emailed code is confirmed, because only the mailbox owner receives the code.
- **Parallel code guessing.** The "5 attempts" limit was checked in C# and then saved, so many parallel requests could all pass the check. Attempts are now reserved in the database with a single `UPDATE … WHERE Attempts < 5`.
- **Login CSRF through the Telegram Mini App.** The site signs linked Telegram users in automatically using Telegram's signed `initData`. A crafted link containing *my* `initData` could have signed a victim into *my* account in an ordinary browser. Auto-login now runs only inside a genuine Telegram client, and `initData` older than an hour is rejected.
- **Admins by username.** Telegram usernames can be changed and later claimed by someone else, so bot admins are now matched by numeric ID only.
- **The API key in a URL.** Gemini's key was sent as a query parameter, and URLs end up in logs. It now travels in a header.
- **AI abuse.** A few concurrent requests could burn through the shared quota. The API now limits AI calls per user, per IP and server-wide (two, eight and sixteen at a time), allows one test generation per user at a time and counts failed attempts too.

None of these needed a new framework. Each fix was a few lines plus a test that reproduces the attack.

### Reliability details that only show up in production

- Reminders were marked as sent only after the whole batch finished. If the cron request timed out halfway, the next run sent the same reminders again. Each account is now claimed with an atomic update *before* its message is sent.
- A reminder set for 23:00 was never sent, because of a "don't message late at night" check (`hour < 23`).
- After a new deploy, a phone with the old page open could fail to load a lazy-loaded chunk and show a white screen. The app now reloads once automatically, and if that fails too, shows a "Reload" card.
- Gemini sometimes returned YouTube transcripts with the same line repeated many times. The pipeline now collapses consecutive duplicates and rejects transcripts that are still mostly repetition.

### Scope creep, managed

The project grew from "grade my speaking" to mock IELTS and CEFR exams, teacher groups, a Telegram bot that can turn a PDF into a validated test, and a shadowing trainer with animated characters. What kept it manageable was a small set of shared building blocks: one `Activities` table with `jsonb` results for every exercise type, one card factory, one Gemini client and one validation style. A new feature usually meant a new prompt, a validator and a screen, not a new subsystem.

### Sign-in on a phone

User feedback said signing in on a phone was hard, and three separate causes turned up. Links shared in Telegram or Instagram open in the app's built-in browser, where Google refuses to show its sign-in window, so the button simply did nothing. Form fields inherited a font smaller than 16 px from their labels, so iPhones zoomed the page on every tap. And registration meant inventing a password on a phone keyboard, then typing a code.

The fix was mostly subtraction. The default is now passwordless: email, then the 6-digit code, which phones offer to fill in from the letter and which submits itself on the sixth digit. The account is created by the first confirmed code, so there is no separate "register" step. The app detects in-app browsers and replaces the Google button with an explanation, an "Open in Chrome" link on Android and a copy-link button. Inputs are at least 16 px. The security model did not change: it reuses the same hashed, rate-limited codes, and a code sign-in into an unverified account wipes any password set before it, exactly like Google sign-in.

Email codes still depend on a free mail quota of 300 letters a day, and most learners arrive from Telegram anyway. So the website got a Telegram sign-in that needs no Telegram widget and no extra domain setup: the site asks the API for a one-time deep link to the bot and shows a two-digit number; the user types it into the bot, which also offers a "This isn't me" button. The right number confirms the attempt, a wrong one cancels it, and the browser, which has been polling with a secret token, receives the session once. I first offered three number buttons, but a random tap would then succeed one time in three; typing the number, as Microsoft Authenticator switched to, makes the user actually look at the site, and the bot warns that anyone who sends you a link and tells you the number is a scammer. Telegram-created accounts get an internal address (`tg-<id>@telegram.invalid`) that is never shown and never emailed, and they can add a real email and password later from the profile.

### A second audit: from "works" to "trustworthy"

Before offering the app to the public I audited it again, this time from the point of view of a stranger on a cheap phone and of a store reviewer. The findings were less about bugs than about trust:

- **Sign-in edge cases.** Google sign-in wiped the password of an unverified account even when email verification was switched off, so the owner could lock themselves out. Sessions expired after 30 days even for daily users. A Telegram Mini App user without a linked account hit a dead end. All three are fixed, with a test for each rule, and sessions now renew themselves while in use.
- **A shared AI quota.** Gemini's free tier is one pool for everyone, and one AI conversation made up to nine calls. The client used to retry "quota exhausted" four times, which only burned more of it. Now there is a server-wide budget, a circuit breaker, per-feature caps and a clear "AI is busy, try again in N seconds" answer.
- **Cold starts.** While the free server woke up, a signed-in user briefly looked like a guest, and anything typed in that state was lost. The app now restores the signed-in state immediately, shows a "server is waking up" banner and times requests out with a translated message.
- **Content.** A script counted where the correct answer sits in multiple-choice questions: in the IELTS mocks, B was right in 68 of 112 three-option questions — a pattern a test-wise learner would exploit. Answers are now spread evenly and a unit test keeps them that way. The same review fixed seven factual or grammar mistakes in the built-in material and replaced typographic apostrophes in Uzbek text with the correct letters (ʻ and ʼ), again guarded by a test.
- **Trust.** The app now has terms of use, an accurate privacy policy (what teachers can see, that audio is not stored, that the free Gemini tier may use submissions), an IELTS/CEFR disclaimer, a feedback button on every page and link previews for Telegram. The admin panel shows how people sign up, so growth from Telegram is measurable.

## A demo that can't be abused

Recruiters and teachers rarely register just to look around, but an empty account shows nothing. The demo button creates a *separate* temporary account for each visitor, already filled with a week of history: speaking and writing feedback, reading and listening results, a finished shadowing lesson, saved words, flashcards (some due now) and a six-day streak.

The seed data is built by a pure function using the same records and card factory as the real endpoints, so every screen renders it normally, and a unit test checks the streak across time zones. Guardrails:

- the address is on the reserved `.invalid` domain, so no email can ever be sent;
- there is no password;
- AI limits are guest-level;
- Telegram linking and the leaderboard are off;
- creation is capped per IP and per day;
- an hourly job deletes demo accounts after 24 hours through the database's cascading foreign keys.

## Testing approach

- **Unit tests (xUnit, Vitest)** cover the pure logic: scheduling, scoring tables, validators, parsing, security rules and localisation. One test checks that every message key exists in all three languages with matching placeholders.
- **End-to-end tests (Playwright)** drive the real frontend against a mocked API that follows the backend's contract, on phone, tablet and laptop screens. The README screenshots and the demo GIF come from those same runs, so they always match the current UI.
- **CI** builds and tests both halves on every push and fails on known vulnerable packages.

The honest gap: the database and endpoint layer has no integration tests yet. That is the next piece of work (see below).

## What I would do next

1. **Integration tests** with `WebApplicationFactory` and a real Postgres in Testcontainers, to cover the queries, atomic updates and rate limits end to end.
2. **A custom domain** for the site and the API. It would let the session token move from `localStorage` to an `httpOnly` cookie, and let Brevo authenticate the sender so verification emails stop landing in spam.
3. **Error monitoring** (Sentry) and privacy-friendly analytics, so I learn about failures and drop-offs from data instead of from users.
4. **Keeping the API warm** with an uptime ping, to remove the 30–60 s cold start on the first visit.
5. **A paid AI tier or another provider.** The free Gemini tier may use submissions to improve Google's products, and its terms exclude services aimed at people under 18, so the terms currently say 18+. A paid tier would lift both restrictions and open the app to school students.

## What I learned

- Treat LLM output like user input: parse it strictly, validate it, and let deterministic code do the parts that have a right answer.
- Free tiers are workable if the design accepts their limits (sleeping servers, blocked ports, shared quotas) instead of fighting them.
- Auditing my own project as a stranger found more serious bugs than writing new tests for code I already believed in.
- Honesty is a feature. Labelling a band score as an estimate, or showing how much the AI grader varies, builds more trust than false precision.
