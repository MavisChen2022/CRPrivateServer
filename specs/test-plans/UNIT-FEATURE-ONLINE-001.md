# UNIT-FEATURE-ONLINE-001: Online Battle Unit Plan

## Status

VERIFIED

## Requirement

FEATURE-ONLINE-001

## Purpose

Define unit-level evidence for matchmaking, online room state, two-player command validation,
result calculation, and public snapshot mapping.

## Test Cases

| Area | Case | Expected Result |
|---|---|---|
| Matchmaking | First player queues | Waiting state is created once. |
| Matchmaking | Duplicate queue | Same player does not create duplicate waiting entries. |
| Matchmaking | Second player queues | One active room is created with two different participants. |
| Room | Resume active room | Active room is found by either participant. |
| Command | Valid deploy | Acting player's elixir decreases and shared unit is created. |
| Command | Invalid card or lane | Command is rejected with stable code and snapshot is unchanged. |
| Command | Enemy-side placement | Command is rejected before mutation. |
| Command | Insufficient elixir | Command is rejected with `InsufficientElixir`. |
| Security | Non-participant command | Request is rejected with `OnlineBattleForbidden`. |
| Result | Tower destroyed | Winner and loser are mapped consistently. |
| Result | Timer expires | Timeout/draw result is stable for both players. |

## Execution

Run:

```powershell
dotnet test CRPrivateServer.sln
```

## Evidence

GamePM, Dev, Asset, and QA verified this plan on 2026-07-18. `dotnet test CRPrivateServer.sln`
passed with Domain 31, Application 14, and API integration 31 tests.
