# QA-GATE-FEATURE-ONLINE-001: Online Battle Approval Gate

## Status

APPROVED

## Requirement

FEATURE-ONLINE-001

## Purpose

Define the evidence required before QA approves the first online battle room MVP.

## Scope

The gate covers two-player matchmaking, room ownership, command validation, shared snapshots,
refresh/reconnect, persistence, mobile layout, reduced motion, and public-safe asset fallback.

It does not approve ranked ladder, trophy changes, rewards, purchases, clans, chat, emotes, friendly
battle invites, or protected third-party assets.

## Required Test Suites

| Suite | Required Project or Location | Required Command | Gate Blocking |
|---|---|---|---|
| xUnit domain/application tests | `tests/Game.Domain.Tests`, `tests/Game.Application.Tests` | `dotnet test CRPrivateServer.sln` | Yes |
| API integration tests | `tests/Game.Api.IntegrationTests` | `dotnet test CRPrivateServer.sln` | Yes |
| Playwright behavior tests | `tests/e2e` | `npm.cmd run test:e2e` | Yes |
| Docs and traceability validation | `scripts/validate-docs.mjs` | `npm.cmd run test:docs` | Yes |
| Web build validation | `src/Game.Web` | `npm.cmd run test:web:build` | Yes |

## xUnit Unit Tests

| Case | Expected Result |
|---|---|
| Queue first guest | Waiting state is created. |
| Duplicate queue | Same guest does not create duplicate waiting entries. |
| Match two guests | One room is created for two different participants. |
| Resume room | Either participant can resolve the active room. |
| Valid command | Server mutates the shared snapshot. |
| Invalid command | Stable validation code and no unauthorized mutation. |
| Result calculation | Both participants receive compatible winner/loser/draw results. |

## API Integration Tests

| Case | Expected Result |
|---|---|
| Session required | Matchmaking and room endpoints reject missing sessions. |
| First queue | Valid guest receives waiting state. |
| Second queue | Two guests receive the same active room id. |
| Current room | Refresh/restart restores active room for each participant. |
| Participant command | Command from either player updates shared snapshot. |
| Non-participant read/command | Third guest receives `OnlineBattleForbidden`. |
| Invalid command | Unknown card, invalid lane, enemy side, and insufficient elixir are rejected. |
| Result persistence | Ended room persists compatible results. |

## Playwright Behavior Tests

| Scenario | Expected Result |
|---|---|
| First player waits | Waiting state is visible and recoverable. |
| Two-player match | Two browser contexts enter the same room. |
| Shared deploy | Deploy from one context appears in both snapshots. |
| Refresh reconnect | Reload returns each participant to the same room. |
| Invalid command | Error is visible and controls remain usable. |
| Result | Both contexts show compatible end states. |
| Mobile viewport | Controls and battle state fit without overlap. |
| Reduced motion | Online battle remains playable without animation-dependent feedback. |

## Asset and Accessibility Cases

- Online UI must use original placeholder arena, towers, units, and card controls by default.
- `assets/imported/` must remain ignored and `git ls-files assets/imported` must return no files.
- Missing imported assets or audio must not block matchmaking or battle play.
- Waiting, reconnecting, command errors, and battle results use visible text and `aria-live` where useful.
- Mobile text and controls must not overlap.

## Evidence Criteria

To move from `IN_REVIEW` to `IMPLEMENTED`, docs, unit/API tests, web UI, and feature code must exist
and pass targeted commands.

To move from `IMPLEMENTED` to `VERIFIED`, full `npm.cmd test` must pass and GamePM, Dev, Asset, and
QA reviews must be `APPROVED`.

## Current Gate Result

APPROVED. GamePM, Dev, Asset, and QA approved starting this online battle MVP on 2026-07-18.
Implementation and automated evidence are pending.
