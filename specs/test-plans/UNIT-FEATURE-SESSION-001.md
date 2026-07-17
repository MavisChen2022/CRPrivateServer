# UNIT-FEATURE-SESSION-001: Guest Session Unit Tests

## Status

CHANGES_REQUESTED

## Requirement

FEATURE-SESSION-001

## Scope

These tests now cover the domain rules for guest profiles and session token validation plus
the application guest-session orchestration around no token, valid token reuse, and invalid
token replacement. Browser behavior and remaining API hardening cases are tracked by the QA
gate and still block feature approval.

## Test Cases

| Test | Expected Result |
|---|---|
| Create guest profile defaults | Guest profile has display name, zero trophies, starting gold, and guest account type. |
| Hash session token | Stored token hash is not the raw token and validates the original token. |
| Reject expired session token | Expired token cannot validate. |
| Reject tampered session token | Different raw token cannot validate against stored hash. |
| Create guest session without token | A new player profile and raw cookie token are issued. |
| Reuse valid guest session token | Existing player profile is returned without exposing a new token. |
| Replace invalid guest session token | A new player profile and replacement token are issued. |

## Execution

```powershell
dotnet test CRPrivateServer.sln
```

## Evidence

- `dotnet test CRPrivateServer.sln`: passed as part of `npm.cmd test`
- `tests/Game.Domain.Tests`: 4 xUnit tests passed
- `tests/Game.Application.Tests`: 3 xUnit tests passed

## Gate Result

CHANGES_REQUESTED. The unit-test slice is now implemented with xUnit, but the overall
FEATURE-SESSION-001 gate remains blocked by missing Playwright behavior coverage and remaining
API hardening cases.
