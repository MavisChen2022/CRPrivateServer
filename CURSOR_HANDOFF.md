# Cursor Handoff: CRPrivateServer

## Current State

- Repository: `MavisChen2022/CRPrivateServer`
- Branch: `main`
- Latest verified local state: FEATURE-SESSION-001, FEATURE-BATTLE-001, and FEATURE-FRIEND-001 are verified on `main`. The friend feature implementation is ready for the next commit and push.
- Full verification command passed on 2026-07-18:

```powershell
npm.cmd test
```

This currently runs docs validation, web validation, Vite build, `dotnet test CRPrivateServer.sln`,
and Playwright e2e. Latest friend feature evidence includes Domain 21, Application 9, API
integration 24, and Playwright 30/30 passing.

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
- FEATURE-BATTLE-001 implementation:
  - Domain `SoloBattleEngine` owns battle start, deploy validation, elixir, ticks, tower damage, and results.
  - API exposes `POST /api/battles/solo`, `GET /api/battles/{battleId}`,
    `POST /api/battles/{battleId}/commands`, and `POST /api/battles/{battleId}/tick`.
  - SQLite stores `BATTLE_SESSIONS` snapshot JSON and `BATTLE_COMMANDS` command records.
  - Web Start Battle opens a playable solo sandbox with placeholder arena, starter cards, HP, elixir,
    timer, refresh persistence, and reduced-motion-safe interaction.
  - E2E database setup now uses an absolute SQLite path so the API and cleanup scripts share one file.
- FEATURE-FRIEND-001 implementation:
  - Domain `FriendCode` and `FriendshipPair` enforce public-safe codes and normalized pair identity.
  - Application `FriendService` owns stable code creation, pending requests, accept/reject, duplicate
    guards, and public friends snapshots.
  - Infrastructure stores `FRIEND_CODES` and `FRIENDSHIPS` in SQLite with uniqueness indexes.
  - API exposes `GET /api/friends`, `POST /api/friends/requests`,
    `POST /api/friends/requests/{friendshipId}/accept`, and
    `POST /api/friends/requests/{friendshipId}/reject`.
  - Web Friends view shows friend code, add-code form, incoming/outgoing requests, accepted friends,
    CSS initials avatars, and `aria-live` status messages without protected assets or audio.
  - Playwright covers invalid/self/duplicate errors, two-browser-context request/accept, refresh
    persistence, desktop/mobile Chromium, and reduced-motion regression.

## Remaining QA Gate Work

1. Keep four long-lived agent roles active for every next slice:
   - GamePM: requirement scope, player journey, traceability.
   - Dev: architecture, server-authoritative logic, persistence/contracts.
   - Asset: public-safe imported assets, fallback UI, accessibility.
   - QA: gate criteria, automated tests, evidence.
2. Start the next feature slice:
   - `FEATURE-BATTLE-001`: verified MVP; next work is polish, richer results, migrations, and optional safe audio.
   - `FEATURE-FRIEND-001`: verified MVP; next work is copy button polish, clearer already-resolved
     request codes, migrations, and optional friend battle planning as a separate feature.
   - `FEATURE-ONLINE-001`: docs gate entered `IN_REVIEW` for two-player matchmaking, online battle
     rooms, server-authoritative commands, reconnect, result, mobile, reduced motion, and asset
     fallback. Implementation has not started yet.
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
