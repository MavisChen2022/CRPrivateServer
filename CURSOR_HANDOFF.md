# Cursor Handoff: CRPrivateServer

## Current State

- Repository: `MavisChen2022/CRPrivateServer`
- Branch: `main`
- Latest verified commit: `66ad73d Cover expired guest session cookies`
- Full verification command passed on 2026-07-17:

```powershell
npm.cmd test
```

This currently runs docs validation, web validation, Vite build, `dotnet test CRPrivateServer.sln`,
and Playwright e2e.

## Completed Slices

- Guest session API backed by SQLite through `src/Game.Infrastructure`.
- Secure raw session token generation; database stores token hash only.
- Cookie behavior: `HttpOnly`, `SameSite=Lax`, `Secure` outside Development.
- xUnit coverage:
  - `tests/Game.Domain.Tests`: domain guest/session token rules.
  - `tests/Game.Application.Tests`: create/reuse/replace guest session service behavior.
  - `tests/Game.Api.IntegrationTests`: no cookie, valid cookie reuse, tampered cookie replacement,
    expired cookie replacement, cookie flags, and no token exposure.
- Playwright coverage:
  - `tests/e2e/guest-session.spec.ts`
  - Desktop Chromium and mobile Chromium projects.
  - First visit, refresh identity persistence, invalid cookie recovery, and command placeholders.
- Public-safe asset policy:
  - `assets/imported/` remains ignored.
  - `assetResolver.ts` defines local imported asset paths.
  - Phaser preview falls back to original placeholder rendering when local assets are unavailable.

## Remaining QA Gate Work

1. Add store-unavailable API integration coverage.
   - Goal: force `IGuestSessionStore` failure and assert `503` with code `SessionStoreUnavailable`.
   - Likely files: `tests/Game.Api.IntegrationTests/SessionApiTests.cs`, `src/Game.Api/Program.cs`.

2. Add restart persistence coverage.
   - Goal: create a guest session, dispose the API host, start a new host with the same SQLite DB,
     send the original cookie, and assert the same player id returns.
   - Watch for Windows SQLite file locks; keep cleanup best-effort.

3. Add Playwright reduced-motion coverage.
   - Goal: emulate `prefers-reduced-motion: reduce`, load home, assert the preview and controls remain usable.
   - Likely file: `tests/e2e/guest-session.spec.ts`.

4. Add Playwright API unavailable then retry coverage.
   - Goal: simulate `/api/session` failure, verify error UI, click Retry or recover after route unblocking.
   - May be easiest with Playwright `page.route` before first navigation.

5. Split or lazy-load Phaser.
   - Current Vite build passes but warns the Phaser bundle is over 500 kB.
   - Likely files: `src/Game.Web/src/main.tsx`, `src/Game.Web/src/game/battlePreview.ts`.

6. Improve imported asset readiness.
   - Current resolver is public-safe and fallback-safe, but local imported assets require an explicit readiness path.
   - Add a manifest probe or documented local manifest before marking Asset agent approved.

## Commands

```powershell
npm.cmd install
npm.cmd exec playwright install chromium
npm.cmd test
npm.cmd run test:e2e -- --reporter=line
dotnet test CRPrivateServer.sln
```

## Status Guidance

Keep `FEATURE-SESSION-001` as `CHANGES_REQUESTED` until all remaining QA gate work above is
implemented and the four agents can approve the gate. Do not commit protected Clash Royale images,
audio, fonts, or extracted data into this public repo.
