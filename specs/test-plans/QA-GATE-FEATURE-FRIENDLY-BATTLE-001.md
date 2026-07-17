# QA-GATE-FEATURE-FRIENDLY-BATTLE-001: Friendly Battle Approval Gate

## Status

VERIFIED

## Requirement

FEATURE-FRIENDLY-BATTLE-001

## Purpose

Define the evidence required before QA approves the accepted-friends-only friendly battle invite MVP.

## Scope

The gate covers friend-only invite authorization, invite create/cancel/decline/accept lifecycle,
room creation, room ownership, reconnect, persistence, no reward mutation, mobile layout,
reduced motion, and public-safe asset fallback.

It does not approve ranked ladder, trophy changes, rewards, shops, purchases, clans, chat, emotes,
spectator mode, rematches, push notifications, or protected third-party assets.

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
| Accepted friend invite | Pending invite is created. |
| Non-friend invite | Stable forbidden/not-friends code is returned. |
| Self invite | `FriendlyBattleSelfInvite` is returned. |
| Duplicate pending invite | No duplicate invite is persisted. |
| Cancel pending invite | Challenger can cancel and recipient cannot accept later. |
| Decline pending invite | Recipient can decline and challenger cannot accept later. |
| Expire pending invite | Expired invite cannot create a room. |
| Accept pending invite | One friendly online battle room is created. |
| Actor role enforcement | Non-owner actions return `FriendlyBattleForbidden`. |
| No reward mutation | Trophies, gold, account type, and friendship status remain unchanged. |

## API Integration Tests

| Case | Expected Result |
|---|---|
| Session required | Friendly battle endpoints reject missing sessions. |
| Friend-only invite | Accepted friends can create and list invites. |
| Non-friend forbidden | Non-friends and pending friends cannot create invites. |
| Duplicate invite | Stable duplicate code or idempotent existing invite is returned. |
| Cancel lifecycle | Challenger cancel removes pending acceptability. |
| Decline lifecycle | Recipient decline removes pending acceptability. |
| Expiry lifecycle | Expired invite cannot be accepted. |
| Accept creates room | Both friends receive the same active friendly room id. |
| Room commands | Valid deploy updates the shared snapshot. |
| Owner isolation | Third guest cannot read, accept, cancel, decline, or command. |
| Reconnect and restart | Pending invite, active room, and result survive API restart with same DB. |
| No rewards | Friendly battle result does not mutate trophies or gold. |
| Response privacy | No session token, token hash, raw cookie, or private account data is exposed. |

## Playwright Behavior Tests

| Scenario | Expected Result |
|---|---|
| Invite from friend row | Accepted friend row exposes Friendly Battle action. |
| Incoming/outgoing state | Challenger sees outgoing pending and recipient sees incoming pending. |
| Accept to room | Recipient accepts and both contexts enter the same friendly room. |
| Shared deploy | Deploy from one context appears in both snapshots. |
| Cancel invite | Challenger cancellation prevents later acceptance. |
| Decline invite | Recipient decline is visible and recoverable. |
| Refresh reconnect | Refresh restores pending invite or active room. |
| No rewards | Trophy and gold display remain unchanged after friendly battle. |
| Mobile viewport | Invite controls and battle state fit without overlap. |
| Reduced motion | Invite and battle remain playable without animation-dependent feedback. |

## Asset and Accessibility Cases

- Friendly Battle UI must use original placeholder arena, towers, units, and card controls by default.
- `assets/imported/` must remain ignored and `git ls-files assets/imported` must return no files.
- Missing imported images or audio must not block invite, accept, or battle play.
- Challenge received, accepted, declined, cancelled, expired, command errors, and battle results use
  visible text and `aria-live` where useful.
- Accept, Decline, Cancel, and Challenge controls must have clear accessible names.
- Mobile text and controls must not overlap.

## Evidence Criteria

To move from `IN_REVIEW` to `IMPLEMENTED`, docs, unit/API tests, web UI, and feature code must exist
and pass targeted commands.

To move from `IMPLEMENTED` to `VERIFIED`, full `npm.cmd test` must pass and GamePM, Dev, Asset, and
QA reviews must be `APPROVED`.

## Current Gate Result

VERIFIED. GamePM, Dev, Asset, and QA verified this friendly battle invite MVP on 2026-07-18 after
`npm.cmd test` passed. Evidence includes docs validation, web validation, Vite build, Domain 31,
Application 21, API integration 36, and Playwright 38/38 across desktop and mobile Chromium.
