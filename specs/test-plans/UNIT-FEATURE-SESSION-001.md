# UNIT-FEATURE-SESSION-001: Guest Session Unit Tests

## Status

VERIFIED

## Requirement

FEATURE-SESSION-001

## Scope

These tests cover the domain rules for guest profiles and session token validation. They do
not cover HTTP, cookies, SQLite, Redis, or browser behavior; those are reserved for later gates.

## Test Cases

| Test | Expected Result |
|---|---|
| Create guest profile defaults | Guest profile has display name, zero trophies, starting gold, and guest account type. |
| Hash session token | Stored token hash is not the raw token and validates the original token. |
| Reject expired session token | Expired token cannot validate. |
| Reject tampered session token | Different raw token cannot validate against stored hash. |

## Execution

```powershell
dotnet run --project tests/Game.Domain.Tests/Game.Domain.Tests.csproj
```

## Evidence

- `dotnet build CRPrivateServer.sln`: passed
- `dotnet run --project tests/Game.Domain.Tests/Game.Domain.Tests.csproj`: passed

## Gate Result

Implementation Ready for the minimal domain session model.

