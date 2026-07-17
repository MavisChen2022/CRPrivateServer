# UNIT-FEATURE-BATTLE-001: Solo Sandbox Battle Unit Plan

## Status

VERIFIED

## Requirement

FEATURE-BATTLE-001

## Purpose

Define the unit-level evidence required before the solo sandbox battle loop can leave review.

## Test Cases

| Area | Case | Expected Result |
|---|---|---|
| Initial state | Start battle | Towers, timer, elixir, starter deck, and status are deterministic. |
| Deployment | Valid Training Knight | Elixir is reduced and a unit is added to the snapshot. |
| Deployment | Invalid card id | Command is rejected with `InvalidCard`. |
| Deployment | Enemy-half placement | Command is rejected with `InvalidPlacement`. |
| Deployment | Insufficient elixir | Command is rejected with `InsufficientElixir`. |
| Tick | Movement | Units advance toward the enemy tower by deterministic distance. |
| Tick | Damage | Enemy tower HP decreases when a unit reaches attack range. |
| Tick | Elixir regeneration | Elixir regenerates up to max and never exceeds max. |
| Result | Tower destroyed | Battle status becomes ended with win result. |
| Result | Timer expired | Battle status becomes ended with timeout result. |
| Result | Ended battle deploy | Additional deploy commands are rejected with `BattleAlreadyEnded`. |

## Execution

Run:

```powershell
dotnet test CRPrivateServer.sln
```

Expected projects:

- `tests/Game.Domain.Tests`
- `tests/Game.Application.Tests`

## Evidence

Implemented and verified on 2026-07-18. `dotnet test CRPrivateServer.sln` passes with
`SoloBattleEngineTests`, application tests, and battle API integration tests included.
