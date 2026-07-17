# QA-GATE-FEATURE-FRIEND-001: Friends Approval Gate

## Status

IN_REVIEW

## Requirement

FEATURE-FRIEND-001

## Purpose

Define the evidence required before QA approves friend code and friends list behavior.

## Scope

The gate covers friend code creation, friend add validation, persistence, UI empty/error states,
mobile layout, and regression protection for session and battle flows.

It does not approve chat, live presence, clans, friend battle invites, or protected third-party
assets.

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
| Friend code format | Code is public-safe and not a secret. |
| Stable code | Same player gets same persisted friend code. |
| Valid request | Service creates one pending request. |
| Accept request | Addressee can accept and both players see the friendship. |
| Self-add | Service rejects with `CannotAddSelf`. |
| Duplicate add | Service prevents duplicate rows. |
| Public list mapping | Response omits session and private account data. |

## API Integration Tests

| Case | Expected Result |
|---|---|
| Session required | Friends endpoints reject requests without valid guest session. |
| Get friends | Valid guest receives friend code and list. |
| Add valid code | Valid target code creates pending request and returns updated list. |
| Accept request | Addressee accepts incoming request; both lists include the friend. |
| Forbidden accept | Requester or unrelated guest cannot accept someone else's incoming request. |
| Self-add | Own code returns `CannotAddSelf`. |
| Duplicate add | Duplicate add returns stable duplicate/idempotent response. |
| Unknown code | Unknown code returns `FriendCodeNotFound`. |
| Persistence | API restart with same database preserves code and list. |

## Playwright Behavior Tests

| Scenario | Expected Result |
|---|---|
| Open Friends | Friends view opens from home and shows friend code. |
| Empty state | No friends shows an honest empty state. |
| Valid request | Adding a seeded second guest code shows outgoing pending state. |
| Accept request | Second browser context accepts and both lists show the friend row. |
| Invalid code | Error appears and input remains usable. |
| Self/duplicate | Validation messages are visible and recoverable. |
| Refresh persistence | Code and friend row remain after reload. |
| Mobile viewport | Form, add button, empty state, and rows fit without overlap. |

## Asset and Accessibility Cases

- Friends UI must use original placeholder avatars or initials only.
- No protected Clash Royale social art or sounds are required.
- Friend code can be selected or copied with keyboard-accessible controls.
- Form errors and successful additions use an `aria-live` status region.
- Mobile text does not overlap inputs, buttons, or friend rows.

## Evidence Criteria

To move from `IN_REVIEW` to `IMPLEMENTED`, docs, unit tests, API tests, and UI implementation must
exist and pass targeted commands.

To move from `IMPLEMENTED` to `VERIFIED`, `npm.cmd test` must pass end to end and GamePM, Dev,
Asset, and QA reviews must be `APPROVED`.

## Current Gate Result

IN_REVIEW. GamePM, Dev, Asset, and QA approved starting this slice on 2026-07-18. Implementation
and automated evidence are pending.
