# SDD-FEATURE-BATTLE-001: Solo Sandbox Battle Loop

## Status

VERIFIED

## Requirement

FEATURE-BATTLE-001

## 1. Purpose

Create a small playable battle loop where the server is the authority for battle ownership, command
validation, simulation ticks, tower damage, elixir, timer, and results.

## 2. User Flow

1. A guest reaches the home screen through `FEATURE-SESSION-001`.
2. The player chooses Start Battle.
3. The web client calls `POST /api/battles/solo`.
4. The server creates or resumes an active solo battle for the guest.
5. The client renders the snapshot with arena, towers, unit list, timer, elixir, and starter deck.
6. The player selects a starter card and submits a deploy command.
7. The server validates the command, updates state, advances ticks, and returns a snapshot.
8. The UI keeps polling or command-triggering ticks until a win or timeout result is returned.
9. The player can replay or return home.

## 3. Business Rules

- Every battle belongs to exactly one guest player.
- The client never chooses authoritative HP, elixir, unit position, tick, timer, or result.
- Starter deck cards use public-safe names and placeholder stats.
- Deploy commands require an active battle, known card id, valid lane, own-half placement, and enough
  elixir.
- Battle duration is fixed for the MVP and ends when the enemy tower is destroyed or time expires.
- Missing optional imported assets or audio must not block the battle.

## 4. System Components

- `Game.Domain`: deterministic battle state, starter deck, placement validation, elixir, tick, and
  result calculation.
- `Game.Application`: solo battle service that binds battle commands to the session player.
- `Game.Infrastructure`: SQLite battle session persistence using a JSON snapshot column for this MVP.
- `Game.Api`: battle endpoints guarded by `royale_session`.
- `Game.Web`: React battle panel and optional Phaser rendering of the server snapshot.

## 5. API Contract

`POST /api/battles/solo`

Creates or resumes a solo sandbox battle for the current guest.

`GET /api/battles/{battleId}`

Returns the current battle snapshot only for the owner.

`POST /api/battles/{battleId}/commands`

Request body:

```json
{
  "type": "DeployCard",
  "cardId": "training-knight",
  "lane": "center",
  "x": 0.5,
  "y": 0.72
}
```

`POST /api/battles/{battleId}/tick`

Advances the deterministic simulation in the solo sandbox test surface and returns the new snapshot.

Validation errors return stable codes such as `BattleNotFound`, `BattleForbidden`,
`BattleAlreadyEnded`, `InvalidCard`, `InvalidLane`, `InvalidPlacement`, and `InsufficientElixir`.

## 6. SQLite Data Design

- `BATTLE_SESSIONS`: `BATTLE_ID`, `PLAYER_ID`, `STATUS`, `SNAPSHOT_JSON`, `CREATED_AT`,
  `UPDATED_AT`, `ENDED_AT`.
- `BATTLE_COMMANDS`: `COMMAND_ID`, `BATTLE_ID`, `PLAYER_ID`, `COMMAND_TYPE`, `COMMAND_JSON`,
  `SUBMITTED_AT_TICK`, `CREATED_AT`, `REJECTED_CODE`.

The MVP may store snapshot JSON to avoid premature table design. Later battle slices can normalize
units, events, and card definitions.

## 7. Redis Cache Key Design

No Redis dependency is required for this slice. Future real-time slices may cache hot snapshots with
keys such as `battle:{battleId}:snapshot`, but SQLite remains the source of truth.

## 8. SignalR Contract

No SignalR contract is required for the MVP. Future multiplayer or live spectator updates must derive
player identity from the server session and never from a client-sent player id.

## 9. State Flow

```text
HomeReady -> StartSoloBattle -> BattleActive
BattleActive -> ValidDeploy -> TickAdvanced -> BattleActive
BattleActive -> InvalidDeploy -> ValidationError -> BattleActive
BattleActive -> TowerDestroyed -> BattleEndedWin
BattleActive -> TimerExpired -> BattleEndedTimeout
BattleEnded -> Replay -> StartSoloBattle
```

## 10. Error Handling

- Missing or invalid guest session: return `401 SessionRequired`.
- Battle owned by another player: return `403 BattleForbidden`.
- Unknown battle id: return `404 BattleNotFound`.
- Domain validation failure: return `400` with stable code and current snapshot when safe.
- Store unavailable: return `503 BattleStoreUnavailable`.
- Unexpected errors return `500 BattleUnexpectedError` without private snapshot internals.

## 11. Security and Anti-Cheat

- Battle ownership is derived from `royale_session`.
- The server is authoritative for all simulation state.
- Commands are validated against server tick, battle status, elixir, placement, and starter deck.
- Snapshot responses omit private seed/internal randomness.
- Client-side local storage cannot select player id, battle id ownership, HP, elixir, or result.
- Protected Clash Royale assets are never committed and are never required for build or tests.

## 12. Transactions, Concurrency, and Idempotency

- Command handling reads the battle, validates ownership and status, appends the command, updates the
  snapshot, and saves in one transaction.
- Duplicate command ids can be used later for idempotency; the MVP can reject repeated commands at the
  same tick if they exceed elixir or status rules.
- Concurrent commands must not allow negative elixir or post-result deployment.

## 13. Monitoring and Logs

- Count solo battles started, resumed, completed, and abandoned.
- Count deploy commands accepted and rejected by validation code.
- Count battle store errors.
- Log correlation id, battle id, player id, route, and outcome without raw session tokens.

## 14. Unit Test Cases

- Starting a battle creates the expected initial towers, elixir, timer, and starter deck.
- Valid deploy consumes elixir and adds a unit.
- Invalid card, invalid lane, enemy-half placement, and insufficient elixir are rejected.
- Ticks move units, damage the enemy tower, and regenerate elixir.
- Destroying the enemy tower ends the battle as a win.
- Timer expiry ends the battle deterministically.
- Ended battles reject further deploy commands.

## 15. Integration Test Cases

- `POST /api/battles/solo` requires a guest session and returns a battle snapshot.
- `GET /api/battles/{battleId}` returns only the owner snapshot.
- Another guest receives `403 BattleForbidden`.
- `POST /api/battles/{battleId}/commands` accepts valid deploy and rejects invalid commands.
- `POST /api/battles/{battleId}/tick` advances server state and persists after API restart.

## 16. Behavior Test Cases

- Home Start Battle opens the solo sandbox arena.
- Deploying Training Knight changes elixir and eventually enemy tower HP.
- Refresh reloads the same battle snapshot.
- Win or timeout result appears and disables deploy commands.
- Mobile viewport keeps arena and controls reachable.
- Reduced motion keeps battle playable without optional motion effects.
- Missing imported assets still render placeholder arena, units, towers, and silent audio.

## 17. Rollback and Migration

Initial battle tables can be introduced with a development migration. Rollback requires preserving
guest session tables and dropping only battle tables in non-production databases unless an operator
approves a production data backup and rollback plan.

## Review Evidence

- GamePM: APPROVED, scoped as solo sandbox only with no PvP promise.
- Dev: APPROVED, battle rules live in Domain/Application/API with SQLite snapshot persistence.
- Asset: APPROVED, default visuals are public-safe placeholders and ignored local overrides remain optional.
- QA: APPROVED, domain, API, Playwright, mobile, reduced-motion, and ownership tests pass.
- StudioLead: VERIFIED through `npm.cmd test` on 2026-07-18.
