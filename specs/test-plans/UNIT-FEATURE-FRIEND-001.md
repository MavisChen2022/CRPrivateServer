# UNIT-FEATURE-FRIEND-001: Friend Code Unit Plan

## Status

IN_REVIEW

## Requirement

FEATURE-FRIEND-001

## Purpose

Define unit-level evidence for friend code generation, validation, duplicate handling, and public
friend list mapping.

## Test Cases

| Area | Case | Expected Result |
|---|---|---|
| Friend code | Generate code | Code is stable for player and public-safe. |
| Friend code | Code privacy | Code is not raw session token, token hash, or full player id. |
| Request | Valid target | One pending request row is created. |
| Request | Self-add | Request is rejected with `CannotAddSelf`. |
| Request | Duplicate | Request is rejected or returns existing row without duplicate data. |
| Request | Wrong actor accept | Request is rejected with `FriendForbidden`. |
| Request | Addressee accept | Relation status becomes accepted and appears for both players. |
| List | Public fields | Friend row includes display name, short id/code, and placeholder status only. |
| Store failure | Unavailable store | Service returns `FriendStoreUnavailable` without partial data. |

## Execution

Run:

```powershell
dotnet test CRPrivateServer.sln
```

## Evidence

Pending implementation. GamePM, Dev, Asset, and QA approved starting this plan; tests are still
pending.
