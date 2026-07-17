# Cursor Handoff: CRPrivateServer

## Current State

- Repository: `MavisChen2022/CRPrivateServer`
- Branch: `main`
- Latest verified local state: FEATURE-SESSION-001 approved and pushed in `71c3aae`; FEATURE-BATTLE-001 docs gate is being prepared.
- Full verification command passed on 2026-07-18:

```powershell
npm.cmd test
```

This currently runs docs validation, web validation, Vite build, `dotnet test CRPrivateServer.sln`,
and Playwright e2e.

## Completed Slices

- FEATURE-SESSION-001 is approved by GamePM, Dev, Asset, and QA final review.
- Guest session API backed by SQLite through `src/Game.Infrastructure`.
- Secure raw session token generation; database stores token hash only.
- Cookie behavior: `HttpOnly`, `SameSite=Lax`, `Secure` outside Development.
- xUnit coverage:
  - `tests/Game.Domain.Tests`: domain guest/session token rules.
  - `tests/Game.Application.Tests`: create/reuse/replace guest session service behavior.
  - `tests/Game.Api.IntegrationTests`: no cookie, valid cookie reuse, tampered cookie replacement,
    expired cookie replacement, store-unavailable response, restart persistence, cookie flags,
    Production `Secure` policy, bounded expiry, and no token exposure.
- Playwright coverage:
  - `tests/e2e/guest-session.spec.ts`
  - Desktop Chromium and mobile Chromium projects.
  - First visit, refresh identity persistence, invalid cookie recovery, command placeholders,
    reduced motion, API retry, and client-side tamper resistance.
- Public-safe asset policy:
  - `assets/imported/` remains ignored.
  - `assetResolver.ts` defines local imported asset paths and verifies image content type before enabling them.
  - Phaser preview falls back to original placeholder rendering when local assets are unavailable.
- Phaser is lazy-loaded into a separate preview chunk.
- FEATURE-BATTLE-001 start review:
  - GamePM, Dev, Asset, and QA approved starting a server-authoritative solo sandbox battle loop.
  - Scope is a playable training battle, not PvP or full Clash Royale parity.
  - Public repo defaults must stay original placeholder visuals/audio with optional ignored local overrides.

## Remaining QA Gate Work

1. Keep four long-lived agent roles active for every next slice:
   - GamePM: requirement scope, player journey, traceability.
   - Dev: architecture, server-authoritative logic, persistence/contracts.
   - Asset: public-safe imported assets, fallback UI, accessibility.
   - QA: gate criteria, automated tests, evidence.
2. Start the next feature slice:
   - `FEATURE-BATTLE-001`: minimal solo sandbox battle loop with BDD, SDD, unit plan, QA gate, agent review, then domain/API/web tests.
   - `FEATURE-FRIEND-001`: placeholder to real friend list API.
   - Deck/card data import pipeline with public-safe asset handling.

## Commands

```powershell
npm.cmd install
npm.cmd exec playwright install chromium
npm.cmd test
npm.cmd run test:e2e -- --reporter=line
dotnet test CRPrivateServer.sln
```

## Status Guidance

`FEATURE-SESSION-001` is approved. Do not commit protected Clash Royale images, audio, fonts, or
extracted data into this public repo.
