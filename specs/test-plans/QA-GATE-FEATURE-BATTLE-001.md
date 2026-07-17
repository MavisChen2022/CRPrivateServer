# QA-GATE-FEATURE-BATTLE-001: Solo Sandbox Battle Approval Gate

## Status

VERIFIED

## Requirement

FEATURE-BATTLE-001

## Purpose

This gate defines the automated evidence required before QA can approve the solo sandbox battle loop.
It intentionally approves only a small playable training battle, not PvP or full Clash Royale parity.

## Scope

The gate covers:

- Server-authoritative battle domain tests.
- API integration tests bound to the approved guest session.
- Playwright behavior tests for playable battle interactions.
- Mobile, reduced-motion, ownership, invalid-command, and asset-fallback evidence.

The gate does not approve PvP matchmaking, ranked results, real card balance, purchases, clans, or
redistribution of protected third-party assets.

## Required Test Suites

| Suite | Required Project or Location | Required Command | Gate Blocking |
|---|---|---|---|
| xUnit domain/application tests | `tests/Game.Domain.Tests`, `tests/Game.Application.Tests` | `dotnet test CRPrivateServer.sln` | Yes |
| API integration tests | `tests/Game.Api.IntegrationTests` | `dotnet test CRPrivateServer.sln` | Yes |
| Playwright behavior tests | `tests/e2e` | `npm.cmd run test:e2e` | Yes |
| Docs and traceability validation | `scripts/validate-docs.mjs` | `npm.cmd run test:docs` | Yes |
| Web build validation | `src/Game.Web` | `npm.cmd run test:web:build` | Yes |

## xUnit Unit Tests

Required before `IN_REVIEW` can become `IMPLEMENTED`:

| Case | Expected Result |
|---|---|
| Initial battle state | Battle starts active with fixed tower HP, elixir, timer, and starter deck. |
| Valid deploy | Server consumes elixir and creates one unit. |
| Invalid deploy | Unknown card, invalid lane, enemy-half placement, and insufficient elixir are rejected. |
| Tick simulation | Units move, damage tower in range, and regenerate elixir deterministically. |
| Result resolution | Tower destruction creates a win and timer expiry creates timeout. |
| Ended battle guard | Deploy commands after result are rejected. |

## API Integration Tests

Required API tests before `VERIFIED`:

| Case | Expected Result |
|---|---|
| Session required | Battle endpoints reject requests without valid guest session. |
| Start solo battle | `POST /api/battles/solo` creates or resumes an owner-bound battle. |
| Get snapshot | Owner can fetch battle snapshot. |
| Forbidden access | Another guest cannot fetch or command the battle. |
| Valid command | Valid deploy returns changed elixir and unit snapshot. |
| Invalid command | Invalid card, placement, lane, and insufficient elixir return stable validation codes. |
| Tick endpoint | Tick advances tower HP and timer deterministically. |
| Persistence | Refresh or API restart with same database reloads the battle snapshot. |

## Playwright Behavior Tests

Required browser tests before `VERIFIED`:

| Scenario | Expected Result |
|---|---|
| Start Battle from home | Player reaches solo sandbox arena from the guest home screen. |
| Deploy unit | Player selects a starter card, deploys it, and sees elixir decrease. |
| Tower damage | Enemy tower HP becomes lower after ticks. |
| Battle result | Win or timeout result appears and deploy controls stop accepting input. |
| Refresh persistence | Page refresh reloads the same battle id and snapshot. |
| Mobile viewport | Arena and controls fit without blocking play. |
| Reduced motion | Optional motion is disabled while controls and state remain usable. |
| Asset fallback | Missing `assets/imported/` paths still produce a playable placeholder battle. |

## Asset and Accessibility Cases

QA blocks approval until:

- `assets/imported/` remains ignored and `git ls-files assets/imported` returns no protected files.
- The default arena, tower, unit, and card visuals are original placeholder assets.
- Optional imported images are content-type checked before use.
- Optional audio has a silent or generated fallback.
- Battle state has text fallback, keyboard-reachable deploy controls, and an `aria-live` result/status
  region.
- Reduced motion disables nonessential tweens, shake, and particle effects.

## Evidence Criteria

To move from `IN_REVIEW` to `IMPLEMENTED`, all of the following must be true:

- BDD, SDD, unit plan, QA gate, review report, and traceability entries exist.
- Domain/application tests for initial state, deploy, invalid commands, tick, and result pass.
- Dev and QA confirm battle logic is not hidden inside React or Phaser.

To move from `IMPLEMENTED` to `VERIFIED`, all of the following must be true:

- `dotnet test CRPrivateServer.sln` passes with battle API tests.
- `npm.cmd run test:e2e` passes with desktop and mobile battle scenarios.
- `npm.cmd test` passes end to end.
- GamePM, Dev, Asset, and QA reviews are all `APPROVED`.

## Current Gate Result

VERIFIED. `npm.cmd test` passed on 2026-07-18 after docs validation, web validation, Vite build,
`dotnet test CRPrivateServer.sln`, and Playwright desktop/mobile e2e. Evidence includes
server-authoritative domain tests, API ownership and invalid-command tests, SQLite restart
persistence, Start Battle UI, deploy/tower HP behavior, refresh persistence, reduced motion, and
mobile coverage.
