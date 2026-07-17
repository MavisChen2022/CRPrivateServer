# Traceability Matrix

| Requirement ID | BDD Feature | SDD | Unit Tests | Integration Tests | Behavior Tests | Status |
|---|---|---|---|---|---|---|
| FEATURE-SESSION-001 | `specs/features/guest-session.feature` | `specs/sdd/SDD-FEATURE-SESSION-001.md` | `specs/test-plans/UNIT-FEATURE-SESSION-001.md` (`tests/Game.Domain.Tests`, `tests/Game.Application.Tests`) | `tests/Game.Api.IntegrationTests` | `tests/e2e` | APPROVED |
| FEATURE-FRIEND-001 | Pending | Pending | Pending | Pending | Pending | DRAFT |
| FEATURE-BATTLE-001 | `specs/features/battle-sandbox.feature` | `specs/sdd/SDD-FEATURE-BATTLE-001.md` | `specs/test-plans/UNIT-FEATURE-BATTLE-001.md` (`tests/Game.Domain.Tests`, `tests/Game.Application.Tests`) | `tests/Game.Api.IntegrationTests` | `tests/e2e` | IN_REVIEW |

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
- FEATURE-BATTLE-001 entered `IN_REVIEW` on 2026-07-18 after GamePM, Dev, Asset, and QA approved starting a server-authoritative solo sandbox scope.
