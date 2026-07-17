# SDD-FEATURE-FRIENDLY-BATTLE-001: Friendly Battle Invite

## Status

VERIFIED

## Requirement

FEATURE-FRIENDLY-BATTLE-001

## 1. Purpose

Provide a private friend-to-friend battle invite flow that reuses the server-authoritative online
battle room after an accepted friendship pair agrees to play.

## 2. User Flow

1. A guest reaches the home screen through `FEATURE-SESSION-001`.
2. The guest opens Friends and sees accepted friend rows from `FEATURE-FRIEND-001`.
3. The challenger selects Friendly Battle on an accepted friend row.
4. The server persists a pending friendly battle invite.
5. The challenger sees an outgoing pending invite.
6. The recipient sees an incoming invite with Accept and Decline actions.
7. The challenger may cancel while the invite is pending.
8. The recipient may decline while the invite is pending.
9. If the recipient accepts, the server creates one friendly online battle room for both friends.
10. Both participants reuse the online battle snapshot, command, tick, reconnect, and result flow.
11. Refreshing clients restore pending invites or the active friendly battle room.

## 3. Business Rules

- Only accepted friends can create friendly battle invites.
- A player cannot invite themselves.
- A pair can have at most one pending friendly battle invite.
- Only the challenger can cancel a pending invite.
- Only the recipient can accept or decline a pending invite.
- Cancelled, declined, expired, or accepted invites cannot be accepted again.
- Accepting an invite creates one non-ranked room and does not use public matchmaking.
- Players already in an active battle are rejected with a stable code for this MVP.
- Friendly battle results never mutate trophies, gold, rewards, account type, or friendship status.
- Starter cards, arena, towers, units, and balance remain public-safe placeholders.

## 4. System Components

- `Game.Domain`: friendly invite state helpers plus reused online battle engine.
- `Game.Application`: friendly invite service, friendship authorization, invite lifecycle, room
  creation orchestration, and reconnect projections.
- `Game.Infrastructure`: SQLite friendly invite table plus reused online room persistence.
- `Game.Api`: friendly battle invite endpoints guarded by `royale_session`.
- `Game.Web`: Friends challenge controls, incoming/outgoing invite sections, status messages, and
  friendly battle entry into the existing Online Battle screen.

## 5. API Contract

`GET /api/friendly-battles/current`

Returns the current player's pending incoming/outgoing invites and active friendly room if present.

`POST /api/friendly-battles/invites`

Request body:

```json
{
  "friendPlayerId": "00000000-0000-0000-0000-000000000000"
}
```

Creates a pending friendly battle invite for an accepted friend.

`POST /api/friendly-battles/invites/{inviteId}/accept`

Accepts a pending invite as the recipient and creates one online battle room in friendly mode.

`POST /api/friendly-battles/invites/{inviteId}/reject`

Rejects a pending invite as the recipient.

`DELETE /api/friendly-battles/invites/{inviteId}`

Cancels a pending invite as the challenger.

Friendly battle snapshots include:

```json
{
  "incomingInvites": [],
  "outgoingInvites": [],
  "activeRoom": {
    "status": "Active",
    "roomId": "00000000-0000-0000-0000-000000000000",
    "snapshot": {}
  }
}
```

Stable problem codes include `SessionRequired`, `FriendlyBattleInviteNotFound`,
`FriendlyBattleForbidden`, `FriendlyBattleNotFriends`, `FriendlyBattleSelfInvite`,
`FriendlyBattleDuplicateInvite`, `FriendlyBattleAlreadyResolved`,
`FriendlyBattleInviteExpired`, `PlayerAlreadyInBattle`, and `FriendlyBattleStoreUnavailable`.

## 6. SQLite Data Design

`FRIENDLY_BATTLE_INVITES`

- `INVITE_ID` TEXT primary key.
- `REQUESTER_PLAYER_ID` TEXT not null.
- `ADDRESSEE_PLAYER_ID` TEXT not null.
- `LOWER_PLAYER_ID` TEXT not null.
- `HIGHER_PLAYER_ID` TEXT not null.
- `STATUS` TEXT not null.
- `ROOM_ID` TEXT null.
- `CREATED_AT` TEXT not null.
- `UPDATED_AT` TEXT not null.
- `EXPIRES_AT` TEXT not null.

Indexes:

- `REQUESTER_PLAYER_ID`
- `ADDRESSEE_PLAYER_ID`
- `ROOM_ID`
- `(LOWER_PLAYER_ID, HIGHER_PLAYER_ID, STATUS)` for duplicate pending invite checks.

Online battle room snapshots continue to live in `ONLINE_BATTLE_ROOMS`.

## 7. Redis Cache Key Design

No Redis dependency is required for this MVP. Future notification or presence work can use
`friendly-battle:invite:{inviteId}` and `friendly-battle:player:{playerId}:inbox`, with SQLite
remaining the source of durable recovery.

## 8. SignalR Contract

SignalR is optional for this MVP. If added later, clients can join `player:{playerId}:friendly` only
after session validation. Polling `GET /api/friendly-battles/current` must remain sufficient for
tests, reconnects, and reduced-motion-safe UI.

## 9. State Flow

```text
FriendsReady -> CreateInvite -> InvitePending
InvitePending -> CancelInvite -> InviteCancelled
InvitePending -> DeclineInvite -> InviteDeclined
InvitePending -> ExpireInvite -> InviteExpired
InvitePending -> AcceptInvite -> FriendlyBattleActive
FriendlyBattleActive -> SubmitCommand -> SnapshotUpdated
FriendlyBattleActive -> Refresh -> ResumeFriendlyRoom
FriendlyBattleActive -> TowerDestroyed -> FriendlyBattleEnded
FriendlyBattleActive -> TimerExpired -> FriendlyBattleEnded
NonFriend -> FriendlyBattleNotFriends
NonParticipant -> FriendlyBattleForbidden
```

## 10. Error Handling

- Missing or invalid session: return `401 SessionRequired`.
- Non-friend invite: return `403 FriendlyBattleNotFriends`.
- Self invite: return `400 FriendlyBattleSelfInvite`.
- Duplicate pending invite: return `409 FriendlyBattleDuplicateInvite`.
- Already resolved invite: return `409 FriendlyBattleAlreadyResolved`.
- Expired invite: return `409 FriendlyBattleInviteExpired`.
- Unrelated invite action: return `403 FriendlyBattleForbidden`.
- Store unavailable: return `503 FriendlyBattleStoreUnavailable`.

## 11. Security and Anti-Cheat

- Player identity is derived from `royale_session`.
- Client cannot choose requester id, recipient id ownership, room side, elixir, tower HP, tick, or
  result.
- Invite actions check accepted friendship and actor role before mutation.
- Friendly battle room commands reuse participant authorization from online battle.
- Responses omit session tokens, token hashes, raw cookies, and private account data.

## 12. Transactions, Concurrency, and Idempotency

Invite create, cancel, decline, and accept must load the latest invite state before mutation. Accept
must create at most one room and write the invite's `ROOM_ID` atomically enough for the MVP. Before
production, replace application guards with database transactions or conditional updates.

## 13. Monitoring and Logs

- Count invites created, accepted, cancelled, rejected, expired, duplicate attempts, forbidden
  attempts, rooms created, reconnects, and store errors.
- Do not log session tokens, token hashes, raw cookies, or proprietary asset paths.

## 14. Unit Test Cases

- Accepted friend can create a pending invite.
- Non-friend, pending friend request, and self invite are rejected.
- Duplicate pending invite is rejected or returned idempotently.
- Challenger can cancel; recipient cannot cancel.
- Recipient can decline; challenger cannot decline.
- Recipient can accept; challenger cannot accept.
- Cancelled, declined, expired, or accepted invite cannot be accepted.
- Accept creates one room and preserves trophies/gold.

## 15. Integration Test Cases

- Invite endpoints require a valid session.
- Accepted friend invite appears as outgoing for challenger and incoming for recipient.
- Non-friend and self invite return stable problem codes.
- Cancel, decline, expire, and duplicate flows are stable.
- Accept creates one online battle room readable by both participants.
- Non-participant cannot read or command the room.
- API restart preserves pending invite, active room, and result.
- Friendly battle result does not mutate trophies, gold, friendship status, or account type.

## 16. Behavior Test Cases

- Two browser contexts become accepted friends, create an invite, and see incoming/outgoing state.
- Recipient accepts and both contexts enter the same friendly battle room.
- Either context deploys a starter card and both observe the shared snapshot.
- Challenger cancel and recipient decline flows are visible and recoverable.
- Refresh restores pending invite or active room.
- Mobile and reduced-motion views remain usable.
- Missing imported assets do not block invite or battle play.

## 17. Rollback and Migration

Initial MVP can add `FRIENDLY_BATTLE_INVITES` through the current schema initializer. Before
production, replace `EnsureCreatedAsync()` table creation with migrations that preserve sessions,
friends, online rooms, and friendly battle invite history.

## Review Evidence

- GamePM: VERIFIED, scoped as accepted-friends-only non-ranked battle invites.
- Dev: VERIFIED, with `FriendlyBattleService` reusing online battle room creation and keeping
  invite lifecycle separate from matchmaking.
- Asset: VERIFIED, with Friends challenge controls, incoming challenge UI, placeholder-only arena
  reuse, mobile, reduced motion, and no protected art/audio.
- QA: VERIFIED after unit/API/two-context Playwright evidence for friend-only invites, lifecycle
  actions, owner isolation, reconnect, no reward mutation, mobile, and reduced motion.
