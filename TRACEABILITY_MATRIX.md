# Traceability Matrix

| Requirement ID | BDD Feature | SDD | Unit Tests | Integration Tests | Behavior Tests | Status |
|---|---|---|---|---|---|---|
| FEATURE-SESSION-001 | `specs/features/guest-session.feature` | `specs/sdd/SDD-FEATURE-SESSION-001.md` | `specs/test-plans/UNIT-FEATURE-SESSION-001.md` (`tests/Game.Domain.Tests`, `tests/Game.Application.Tests`) | `tests/Game.Api.IntegrationTests` partial | `tests/e2e` partial | CHANGES_REQUESTED |
| FEATURE-FRIEND-001 | Pending | Pending | Pending | Pending | Pending | DRAFT |
| FEATURE-BATTLE-001 | Pending | Pending | Pending | Pending | Pending | DRAFT |

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

- `npm.cmd test`: passed on 2026-07-17 after docs validation, web validation, Vite build, `dotnet test CRPrivateServer.sln`, and Playwright e2e.
- FEATURE-SESSION-001 unit coverage now includes xUnit domain and application tests.
- FEATURE-SESSION-001 API integration coverage now includes no-cookie creation, valid-cookie reuse, tampered-cookie replacement, and expired-cookie replacement.
- FEATURE-SESSION-001 Playwright coverage now includes first visit, refresh, invalid cookie recovery, command placeholders, and desktop/mobile Chromium projects.
- FEATURE-SESSION-001 remains `CHANGES_REQUESTED` until remaining API failure/restart, reduced-motion, and retry cases pass.
