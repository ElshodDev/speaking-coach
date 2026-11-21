# Security Policy

FluentUZ handles accounts, learning progress and short voice recordings (sent to Google Gemini for grading and not stored). We take security reports seriously.

## Reporting a vulnerability

**Please do not open a public issue or pull request for a security problem.**

Use GitHub's private reporting instead:

1. Go to the repository's **Security** tab.
2. Click **Report a vulnerability** ([direct link](https://github.com/ElshodDev/speaking-coach/security/advisories/new)).
3. Describe the problem: what you found, where (URL or file), the steps to reproduce it, and what an attacker could do with it.

Only the maintainer can see the report. Do not include real users' personal data; a minimal proof of concept is enough.

## What to expect

This is a one-person project, so replies are best-effort. We aim to acknowledge a report within **7 days** and to fix confirmed high-severity issues as soon as possible. We will tell you when it is fixed and, if you like, credit you.

## Scope

In scope: the production site (https://fluentuz.app), its API, the Telegram bot and the code in this repository, for example authentication and session problems, access to another user's data, injection, cross-site scripting, secrets exposed in code, and ways to abuse the AI quota.

Out of scope: denial-of-service or load testing, social engineering, physical attacks, spam, findings that need a rooted or jailbroken device, missing best-practice headers with no demonstrated impact, and problems in third-party services (Vercel, Render, Neon, Google, Telegram). Please report those to the vendor.

## Please

- Test only with your own accounts. Do not access, change or delete other people's data.
- Stop once you have shown the problem exists, and give us reasonable time to fix it before telling anyone else.

There is no paid bug bounty. Reports made in good faith under these rules will not lead to legal action.

## Supported versions

Only the current `main` branch, which is what runs in production, receives security fixes.
