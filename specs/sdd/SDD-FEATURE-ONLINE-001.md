# SDD-FEATURE-ONLINE-001: Online Battle Room

## Status

APPROVED

## Requirement

FEATURE-ONLINE-001

## 1. Purpose

Provide the first two-player battle loop where guest players are matched into a durable
server-authoritative room, can submit starter-card commands, and can reconnect after refresh.

## 2. User Flow

1. A guest reaches the home screen through `FEATURE-SESSION-001`.
2. The player chooses Online Battle.
3. The client calls `POST /api/online-battles/matchmaking`.
4. If no opponent is waiting, the server persists a waiting queue entry.
5. A waiting player can cancel before pairing.
6. When a second guest queues, the server creates an online battle room with both participants.
7. Both clients poll or subscribe for the active room snapshot.
8. Each participant deploys starter cards through server-validated commands.
9. The server advances the shared battle snapshot and stores commands/results.
10. Refreshing clients call the resume endpoint and restore the active room.

## 3. Business Rules

- A player can have at most one active online battle room.
- A player can have at most one waiting matchmaking entry.
- A cancelled waiting entry cannot be matched later.
- Room ownership and command authority come only from the server session.
- Participants can deploy only on their own side.
- Starter cards and balance remain public-safe placeholders.
- Battle results are local MVP results and do not affect trophies or rewards.

## 4. System Components

- `Game.Domain`: two-player battle engine, room status, command validation, result calculation.
- `Game.Application`: matchmaking service, room service, reconnect and command orchestration.
- `Game.Infrastructure`: SQLite queue, room snapshot, command, and participant persistence.
- `Game.Api`: online battle endpoints guarded by `royale_session`.
- `Game.Web`: Online Battle view, waiting state, battle controls, reconnect state, and result UI.

## 5. API Contract

`POST /api/online-battles/matchmaking`

Queues the current guest or returns their active/waiting online battle state.

`DELETE /api/online-battles/matchmaking`

Cancels the current guest's waiting entry when they are not already matched.

`GET /api/online-battles/current`

Returns the current player's waiting state or active room snapshot.

`GET /api/online-battles/{roomId}`

Returns the room snapshot only for participants.

`POST /api/online-battles/{roomId}/commands`

Request body:

```json
{
  "cardId": "training-knight",
  "lane": "center",
  "x": 0.5,
  "y": 0.75
}
```

`POST /api/online-battles/{roomId}/tick`

Advances the shared room snapshot for MVP polling tests.

Stable problem codes include `SessionRequired`, `OnlineBattleNotFound`, `OnlineBattleForbidden`,
`OnlineBattleStoreUnavailable`, `MatchmakingUnavailable`, `InvalidCard`, `InvalidLane`,
`InvalidPlacement`, `InsufficientElixir`, `MatchmakingNotQueued`, and `BattleAlreadyEnded`.

## 6. SQLite Data Design

- `MATCHMAKING_QUEUE`: `PLAYER_ID`, `ENQUEUED_AT`, `STATUS`.
- `ONLINE_BATTLE_ROOMS`: `ROOM_ID`, `PLAYER_ONE_ID`, `PLAYER_TWO_ID`, `STATUS`, `SNAPSHOT_JSON`,
  `CREATED_AT`, `UPDATED_AT`, `ENDED_AT`.
- `ONLINE_BATTLE_COMMANDS`: `COMMAND_ID`, `ROOM_ID`, `PLAYER_ID`, `COMMAND_TYPE`, `COMMAND_JSON`,
  `SUBMITTED_AT_TICK`, `CREATED_AT`, `REJECTED_CODE`.

Indexes:

- `MATCHMAKING_QUEUE.PLAYER_ID` is unique for waiting entries.
- `ONLINE_BATTLE_ROOMS.PLAYER_ONE_ID` and `PLAYER_TWO_ID` are indexed.
- `ONLINE_BATTLE_COMMANDS.ROOM_ID` is indexed.

## 7. Redis Cache Key Design

No Redis dependency is required for this MVP. Future distributed matchmaking can use
`matchmaking:queue` and `online-battle:{roomId}` cache keys, with SQLite remaining the source of
durable recovery.

## 8. SignalR Contract

SignalR is optional for this MVP. If added, clients join `online-battle:{roomId}` only after the API
verifies session participation. Polling endpoints must remain sufficient for tests and reconnects.

## 9. State Flow

```text
HomeReady -> QueueOnline -> WaitingForOpponent
WaitingForOpponent -> CancelQueue -> OnlineLobby
WaitingForOpponent -> OpponentMatched -> OnlineBattleActive
OnlineBattleActive -> SubmitCommand -> SnapshotUpdated
OnlineBattleActive -> Refresh -> ResumeActiveRoom
OnlineBattleActive -> TowerDestroyed -> OnlineBattleEnded
OnlineBattleActive -> TimerExpired -> OnlineBattleEnded
SessionMissing -> SessionRequired
NonParticipant -> OnlineBattleForbidden
```

## 10. Error Handling

- Missing or invalid guest session: return `401 SessionRequired`.
- No visible room for the session: return `404 OnlineBattleNotFound`.
- Non-participant read or command: return `403 OnlineBattleForbidden`.
- Invalid deploy command: return `400` or `409` with a stable validation code.
- Store unavailable: return `503 OnlineBattleStoreUnavailable`.

## 11. Security and Anti-Cheat

- Player identity is derived from `royale_session`.
- Client cannot choose participant id, room side, elixir, tower HP, tick, or result.
- Server snapshots are authoritative and command logs record rejected command codes.
- Responses omit session tokens, token hashes, and private account data.

## 12. Transactions, Concurrency, and Idempotency

Matchmaking pair creation must be transactional enough to prevent one waiting player from being
matched into multiple active rooms. Command submission and tick advancement must load, validate,
mutate, and save one authoritative snapshot.

## 13. Monitoring and Logs

- Count queue entries, matches created, commands accepted, commands rejected by code, reconnects, and
  store errors.
- Do not log session tokens, token hashes, or raw cookies.

## 14. Unit Test Cases

- Queue state prevents duplicate waiting entries.
- Two waiting players produce one room.
- Valid deploy consumes only the acting player's elixir.
- Invalid deploy preserves the snapshot and returns stable codes.
- Room result maps winner/loser/draw consistently.
- Non-participant commands are rejected before mutation.

## 15. Integration Test Cases

- Matchmaking requires a valid session.
- First player receives waiting state.
- Second player creates an active room for both participants.
- Current-room endpoint restores active room after refresh/restart.
- Participant command updates shared snapshot.
- Non-participant access and commands are forbidden.
- Store restart preserves active room and result.

## 16. Behavior Test Cases

- First browser context waits for opponent.
- Second browser context matches into the same room.
- Both contexts see compatible snapshots.
- A deploy from either context appears for both.
- Refresh restores the room.
- Invalid command is visible and recoverable.
- Mobile and reduced-motion views remain usable.

## 17. Rollback and Migration

Initial MVP tables can be added through the current schema initializer. Before production, replace
`EnsureCreatedAsync()` table creation with migrations that preserve sessions, friends, and battles.

## Review Evidence

- GamePM: APPROVE TO START, scoped as a minimal two-guest online battle room without ranked, chat,
  clans, purchases, or full card parity.
- Dev: APPROVE TO START, recommends SQLite-backed matchmaking/rooms, polling-first snapshots,
  session-derived ownership, and stable problem codes.
- Asset: APPROVE TO START, requires public-safe placeholder arena, waiting/reconnect/error states,
  mobile, reduced motion, and no protected art/audio.
- QA: APPROVE TO START, requires domain/API/two-context Playwright evidence for queue, cancel, match,
  command sync, owner isolation, reconnect, result, mobile, reduced motion, and persistence.
- StudioLead: APPROVED after docs gate passed on 2026-07-18.
