# Traceability Matrix

| Requirement ID | BDD Feature | SDD | Unit Tests | Integration Tests | Behavior Tests | Status |
|---|---|---|---|---|---|---|
| FEATURE-SESSION-001 | `specs/features/guest-session.feature` | `specs/sdd/SDD-FEATURE-SESSION-001.md` | `specs/test-plans/UNIT-FEATURE-SESSION-001.md` (`tests/Game.Domain.Tests`, `tests/Game.Application.Tests`) | `tests/Game.Api.IntegrationTests` | `tests/e2e` | APPROVED |
| FEATURE-FRIEND-001 | `specs/features/friend-code.feature` | `specs/sdd/SDD-FEATURE-FRIEND-001.md` | `specs/test-plans/UNIT-FEATURE-FRIEND-001.md` (`tests/Game.Domain.Tests`, `tests/Game.Application.Tests`) | `tests/Game.Api.IntegrationTests` | `tests/e2e` | VERIFIED |
| FEATURE-BATTLE-001 | `specs/features/battle-sandbox.feature` | `specs/sdd/SDD-FEATURE-BATTLE-001.md` | `specs/test-plans/UNIT-FEATURE-BATTLE-001.md` (`tests/Game.Domain.Tests`, `tests/Game.Application.Tests`) | `tests/Game.Api.IntegrationTests` | `tests/e2e` | VERIFIED |
| FEATURE-ONLINE-001 | `specs/features/online-battle.feature` | `specs/sdd/SDD-FEATURE-ONLINE-001.md` | `specs/test-plans/UNIT-FEATURE-ONLINE-001.md` (`tests/Game.Domain.Tests`, `tests/Game.Application.Tests`) | `tests/Game.Api.IntegrationTests` | `tests/e2e` | APPROVED |

## Status Rules

- `DRAFT`: initial document exists but has not entered review.
- `IN_REVIEW`: required reviewers are checking the document.
- `APPROVED`: required reviewers accepted the gate.
- `IMPLEMENTED`: code and unit tests are complete.
- `VERIFIED`: integration and behavior evidence passed.
- `RELEASED`: all release gates passed.
- `CHANGES_REQUESTED`: a reviewer found a blocking issue.
- `BLOCKED`: an external dependency prevents progress.

## Latest Evidence

- `npm.cmd test`: passed on 2026-07-18 after docs validation, web validation, Vite build, `dotnet test CRPrivateServer.sln`, and Playwright e2e.
- FEATURE-SESSION-001 unit coverage now includes xUnit domain and application tests.
- FEATURE-SESSION-001 API integration coverage now includes no-cookie creation, valid-cookie reuse, tampered-cookie replacement, expired-cookie replacement, store-unavailable `503`, restart persistence, bounded expiry, and Production `Secure` cookie policy.
- FEATURE-SESSION-001 Playwright coverage now includes first visit, refresh, invalid cookie recovery, command placeholders, reduced motion, API retry, client-side tamper resistance, and desktop/mobile Chromium projects.
- FEATURE-SESSION-001 is `APPROVED` after final GamePM, Dev, Asset, and QA review.
- FEATURE-BATTLE-001 is `VERIFIED` after `npm.cmd test` passed on 2026-07-18.
- FEATURE-BATTLE-001 unit/API coverage includes deterministic battle start, valid deploy, invalid card/lane/placement, insufficient elixir, tick movement/damage/elixir regen, tower win, timeout, session-required API, owner-only access, invalid command code, and SQLite restart persistence.
- FEATURE-BATTLE-001 Playwright coverage includes Start Battle, solo arena rendering, deploy/elixir change, enemy tower HP damage, refresh persistence, reduced motion, and desktop/mobile Chromium projects.
- FEATURE-FRIEND-001 is `VERIFIED` after domain/API/web/e2e targeted gates passed on 2026-07-18.
- FEATURE-FRIEND-001 full `npm.cmd test` passed on 2026-07-18 with Domain 21, Application 9, API integration 24, and Playwright 30/30.
- FEATURE-FRIEND-001 coverage includes public-safe friend code generation, stable friend code retrieval, pending request creation, accept/reject lifecycle, duplicate/self/unknown/invalid/forbidden errors, response privacy, SQLite restart persistence, Friends UI empty state, aria-live validation messages, two-browser-context request/accept, refresh persistence, desktop/mobile Chromium, and reduced-motion regression coverage.
- FEATURE-ONLINE-001 docs gate is `APPROVED` on 2026-07-18 after `npm.cmd run test:docs` passed and four agents approved the scoped MVP: queue, cancel, match, server-authoritative commands, reconnect, result, mobile, reduced motion, persistence, and asset fallback.
