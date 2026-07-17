# AGENT-REVIEW-FEATURE-SESSION-001

## Status

CHANGES_REQUESTED

## Requirement

FEATURE-SESSION-001

## Date

2026-07-17

## StudioLead Summary

The four-agent review was rerun after the guest-session persistence and UI placeholder work.
The code now has a stronger tested slice, but FEATURE-SESSION-001 remains blocked from feature
approval until behavior evidence and the remaining hardening tests are added.

## GamePM Review

Status: CHANGES_REQUESTED

- The player-visible guest entry promise remains valid.
- UX-BDD addendum now covers guest home placeholders, API retry, expired-cookie recovery,
  mobile, reduced motion, and multi-tab expectations.
- Traceability now records xUnit and partial API integration evidence.
- Approval remains blocked until passing behavior evidence is linked.

## Dev Review

Status: APPROVE for the persistence slice; CHANGES_REQUESTED for full feature readiness

- SQLite-backed guest session persistence is implemented in `Game.Infrastructure`.
- API startup creates the SQLite schema, and `/api/session` reuses valid cookie-backed sessions.
- Raw session tokens stay in the cookie while the database stores token hashes only.
- Cookies use `HttpOnly`, `SameSite=Lax`, and `Secure` outside Development.
- Follow-up hardening: add restart-level persistence coverage and additional store failure cases.

## Asset Review

Status: CHANGES_REQUESTED, partial UI and resolver corrections applied

- Basic loading, error, and ready states exist. Command placeholder states now cover the
  Start Battle, Friends, and Deck empty/locked interactions.
- Phaser canvas now has an accessible label and fallback description.
- Reduced motion now updates when the operating system preference changes.
- `assetResolver.ts` defines a public-safe local imported asset manifest under `assets/imported/`.
- `battlePreview.ts` attempts local arena, tower, and deploy SFX assets first, then falls back
  to original placeholder drawing when imported assets are missing.
- The asset policy is documented in `assets/README.md`: keep `assets/imported/` ignored and
  do not commit protected Clash assets to a public repository.
- Phaser is bundled into the main page and should later be lazy-loaded.

## QA Review

Status: CHANGES_REQUESTED

- `npm.cmd test` passes with docs validation, web validation, Vite build, .NET tests, and
  Playwright e2e.
- Domain, application, and API integration tests now run through xUnit.
- API integration covers no-cookie session creation, valid cookie reuse, invalid cookie replacement,
  expired cookie replacement, cookie flags, and no token exposure.
- Playwright e2e covers first visit, refresh identity persistence, invalid cookie recovery,
  command placeholders, and desktop/mobile Chromium projects.
- Missing tests include store unavailable `503 SessionStoreUnavailable`, restart persistence,
  reduced-motion behavior, API retry behavior, and full cookie expiry/Secure policy.

## Required Corrections

1. Keep traceability at `CHANGES_REQUESTED` until the remaining QA hardening evidence exists.
2. Extend Playwright behavior tests for reduced motion and API unavailable/retry behavior.
3. Add API integration tests for store-unavailable response, restart persistence, and full cookie
   expiry/Secure policy.
4. Lazy-load Phaser or split the web bundle before production release.
5. Keep protected Clash Royale image and sound assets out of Git; use `assets/imported/` only locally.

## Evidence

- GamePM subagent: CHANGES_REQUESTED; scenarios are now documented, behavior evidence still missing.
- Dev subagent: APPROVE for guest-session persistence slice.
- Asset subagent: CHANGES_REQUESTED; public-safe placeholder UI is usable, resolver/fallback support added after review.
- QA subagent: CHANGES_REQUESTED; xUnit/API coverage improved, initial Playwright coverage now passes,
  hardening tests still missing.
- StudioLead evidence: `npm.cmd test` passed on 2026-07-17, including 8 Playwright browser tests.
