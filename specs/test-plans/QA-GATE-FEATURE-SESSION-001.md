# QA-GATE-FEATURE-SESSION-001: Guest Session Approval Gate

## Status

APPROVED

## Requirement

FEATURE-SESSION-001

## Purpose

This gate defines the evidence required before QA can approve guest session entry. The current
implementation has durable session behavior, API behavior, browser behavior, and security cases
covered by automated tests and final four-agent review.

## Scope

The gate covers:

- xUnit unit tests for domain and application session rules.
- API integration tests for `GET /api/session` and session cookies.
- Playwright behavior tests mapped to `specs/features/guest-session.feature`.
- Cookie, token, and client-tampering security cases.
- Evidence criteria for status transitions.

The gate does not approve battle matchmaking, deck editing, friends, account upgrades, purchases,
or protected Clash Royale asset redistribution.

## Required Test Suites

| Suite | Required Project or Location | Required Command | Gate Blocking |
|---|---|---|---|
| xUnit unit tests | `tests/Game.Domain.Tests`, `tests/Game.Application.Tests` | `dotnet test CRPrivateServer.sln` | Yes |
| API integration tests | `tests/Game.Api.IntegrationTests` with `WebApplicationFactory` | `dotnet test CRPrivateServer.sln` | Yes |
| Playwright behavior tests | `tests/e2e` | `npm.cmd run test:e2e` | Yes |
| Docs and traceability validation | `scripts/validate-docs.mjs` | `npm.cmd run test:docs` | Yes |
| Web build validation | `src/Game.Web` | `npm.cmd run test:web:build` | Yes |

## xUnit Unit Tests

Required unit tests before `IN_REVIEW`:

| Case | Expected Result |
|---|---|
| Guest profile defaults | New guest has generated display name, zero trophies, starting gold, and guest account type. |
| Token hash stores no raw token | Stored token hash cannot reveal or equal the raw token. |
| Valid token maps to existing profile | A valid non-expired token returns the existing guest profile. |
| Invalid token creates replacement guest | A malformed or mismatched token creates a new guest and invalidates the old cookie path. |
| Expired token creates replacement guest | Expired token is rejected and a new guest session is issued. |
| Duplicate creation is idempotent per request | One session creation request creates exactly one account, one profile, and one token. |
| Store failure returns typed error | Session store failures surface as `SessionStoreUnavailable`, not as partial profile data. |

Minimum implementation expectations:

- Tests use xUnit facts/theories, not a console smoke runner.
- Application logic is testable through interfaces, not direct singleton runtime state.
- SQLite-backed persistence tests may use a temporary database per test.

## API Integration Tests

Required API tests before `IN_REVIEW`:

| Case | Expected Result |
|---|---|
| No cookie | `GET /api/session` returns `200`, creates a guest, and sets `royale_session`. |
| Valid cookie reuse | Repeating `GET /api/session` with the cookie returns the same player id. |
| Invalid cookie replacement | Tampered cookie returns `200`, creates a new guest, and replaces the cookie. |
| Expired cookie replacement | Expired token returns `200`, creates a new guest, and replaces the cookie. |
| Token not exposed | Response body never includes raw token, token hash, or internal session id. |
| Cookie flags | Cookie is `HttpOnly`, `SameSite=Lax`, has a bounded expiry, and is `Secure` outside local HTTP dev. |
| Store unavailable | Repository failure returns `503` with stable code `SessionStoreUnavailable`. |
| Restart persistence | After API restart with the same SQLite database, valid cookie resolves to the same guest. |

Minimum implementation expectations:

- Tests use `WebApplicationFactory` or equivalent in-process ASP.NET Core integration testing.
- Cookie assertions inspect `Set-Cookie` headers directly.
- Persistence tests run against isolated test database files and clean up after themselves.

## Playwright Behavior Tests

Required browser tests before `IN_REVIEW`:

| Scenario | Expected Result |
|---|---|
| First visit creates guest home | Player sees loading, then ready home with display name and resources. |
| Refresh keeps guest | Browser refresh preserves the same player identity. |
| Tampered local storage ignored | Client-side player id changes do not affect server identity. |
| Invalid cookie recovery | Browser with bad cookie recovers to a new playable guest state. |
| API unavailable then retry | Error state appears; retry reaches home after API returns. |
| Reduced motion | Home and preview remain usable when `prefers-reduced-motion` is enabled. |
| Mobile viewport | Home fits at a representative mobile viewport without clipped buttons or overlapping text. |
| Battle preview nonblank | Phaser preview canvas renders nonblank content or accessible fallback. |

Minimum implementation expectations:

- Tests run headlessly in CI-compatible mode.
- Test selectors use stable `data-testid` values.
- Screenshots or traces are kept for failed behavior tests.

## Cookie and Security Cases

QA blocks approval until the following are automated or explicitly deferred by StudioLead:

- Client cannot choose or override `playerId`.
- Raw session token is never returned in JSON or logs.
- Session cookie is not readable by JavaScript.
- Malformed, expired, and mismatched tokens all recover without exposing stack traces.
- Store failures return a generic player-safe error and a stable machine-readable code.
- Logs include correlation id and route outcome, but no raw token or token hash.
- Guest session creation is transactionally all-or-nothing.

## Evidence Criteria

To move from `CHANGES_REQUESTED` to `IN_REVIEW`, all of the following must be true:

- Required xUnit unit tests exist and pass.
- Required API integration tests exist and pass except restart persistence if SQLite work is still under Dev review.
- Required Playwright tests for first visit, refresh, invalid cookie recovery, and mobile viewport exist and pass.
- `TRACEABILITY_MATRIX.md` lists each test suite with command and latest result.
- `AGENT-REVIEW-FEATURE-SESSION-001.md` links or summarizes the latest QA run.

To move from `IN_REVIEW` to `APPROVED`, all of the following must be true:

- `npm.cmd test` passes end to end.
- `dotnet test CRPrivateServer.sln` passes with real xUnit tests.
- API restart persistence passes against SQLite.
- Playwright behavior suite passes with reduced-motion and API retry cases included.
- Cookie/security cases pass or have an explicit StudioLead-approved deferment.
- GamePM, Dev, Asset, and QA reviews are all `APPROVED`.

## Current Gate Result

APPROVED. xUnit, API integration, Playwright browser coverage, restart persistence,
store-unavailable response, reduced motion, API retry, client tamper resistance, and cookie
expiry/Secure-policy evidence pass through `npm.cmd test`. GamePM, Dev, Asset, and QA final
reviews approved this gate.
