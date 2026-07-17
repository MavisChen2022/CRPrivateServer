# SDD-FEATURE-FRIEND-001: Friend Code and Friends List

## Status

VERIFIED

## Requirement

FEATURE-FRIEND-001

## 1. Purpose

Provide a small durable friends feature where guest players can share public friend codes and persist
a friends list without implementing chat, presence, or friendly battles.

## 2. User Flow

1. A guest reaches the home screen through `FEATURE-SESSION-001`.
2. The player opens Friends.
3. The client calls `GET /api/friends`.
4. The server returns the player's friend code and current friends list.
5. The player enters another player's friend code.
6. The client calls `POST /api/friends/requests`.
7. The server validates the target code, self-add, duplicate rules, and ownership.
8. The server persists the pending request and returns the updated list.
9. The target player accepts or rejects the incoming request.

## 3. Business Rules

- Every player has one public friend code.
- Friend codes are not session tokens, token hashes, raw player ids, or reversible secrets.
- A player cannot add themselves.
- Duplicate pending/accepted friend rows are rejected or returned idempotently.
- Friends list entries show public profile data only.
- Guest players can lose friends if they lose the guest session.

## 4. System Components

- `Game.Domain`: friend code and friendship validation rules.
- `Game.Application`: friend service bound to session player identity.
- `Game.Infrastructure`: SQLite friend code and friendship persistence.
- `Game.Api`: friends endpoints guarded by `royale_session`.
- `Game.Web`: Friends view, empty state, add form, validation messages, and friend rows.

## 5. API Contract

`GET /api/friends`

Returns the current player's friend code and friends list.

`POST /api/friends/requests`

Request body:

```json
{
  "friendCode": "A1B2C3D4"
}
```

Validation errors use stable codes such as `SessionRequired`, `FriendCodeNotFound`,
`CannotAddSelf`, `DuplicateFriend`, `FriendRequestNotFound`, `FriendForbidden`, and
`FriendStoreUnavailable`.

`POST /api/friends/requests/{friendshipId}/accept`

Accepts an incoming request only for the addressee.

`POST /api/friends/requests/{friendshipId}/reject`

Rejects an incoming request only for the addressee.

## 6. SQLite Data Design

- `FRIEND_CODES`: `PLAYER_ID`, `FRIEND_CODE`, `CREATED_AT`.
- `FRIENDSHIPS`: `FRIENDSHIP_ID`, `REQUESTER_PLAYER_ID`, `ADDRESSEE_PLAYER_ID`,
  `LOWER_PLAYER_ID`, `HIGHER_PLAYER_ID`, `STATUS`, `CREATED_AT`, `UPDATED_AT`.

Unique constraints:

- `FRIEND_CODES.FRIEND_CODE` is unique.
- `FRIEND_CODES.PLAYER_ID` is unique.
- `FRIENDSHIPS (LOWER_PLAYER_ID, HIGHER_PLAYER_ID)` is unique.

## 7. Redis Cache Key Design

No Redis dependency is required for this slice. Future presence can use keys such as
`presence:{playerId}`, but the friends list remains SQLite-backed.

## 8. SignalR Contract

No SignalR contract is required for this MVP. Future friend requests, presence, and friendly battle
invites must authenticate through the server session and not a client-sent player id.

## 9. State Flow

```text
HomeReady -> OpenFriends -> FriendsLoading -> FriendsReady
FriendsReady -> SubmitValidCode -> PendingRequest -> FriendsReady
FriendsReady -> AcceptIncoming -> FriendAccepted -> FriendsReady
FriendsReady -> SubmitInvalidCode -> ValidationError -> FriendsReady
SessionMissing -> SessionRequired
StoreError -> RecoverableError
```

## 10. Error Handling

- Missing or invalid guest session: return `401 SessionRequired`.
- Unknown friend code: return `404 FriendCodeNotFound`.
- Self-add: return `400 CannotAddSelf`.
- Duplicate friend/request: return `409 DuplicateFriend` or `200` with existing row if idempotent.
- Wrong player accepts/rejects request: return `403 FriendForbidden`.
- Store unavailable: return `503 FriendStoreUnavailable`.

## 11. Security and Anti-Cheat

- Player identity is derived from `royale_session`.
- Friend code lookup never exposes session token, token hash, or internal account secrets.
- Client cannot choose source player id.
- API returns only public friend profile fields.

## 12. Transactions, Concurrency, and Idempotency

Friend code creation and friendship creation must be transactionally safe. Duplicate concurrent add
requests must not create duplicate rows and should return a stable duplicate/idempotent result.

## 13. Monitoring and Logs

- Count friend codes created.
- Count friend add successes and validation failures by code.
- Count friend store errors.
- Do not log session tokens, token hashes, or submitted raw cookies.

## 14. Unit Test Cases

- Friend code format is public-safe and not a raw player id.
- Self-add is rejected.
- Duplicate add is rejected or idempotent.
- Valid request creates one pending row.
- Accepting request creates an accepted relation visible to both players.
- Friend list maps only public profile fields.

## 15. Integration Test Cases

- `GET /api/friends` requires a valid session.
- `GET /api/friends` creates or returns stable friend code.
- `POST /api/friends/requests` creates a pending request by valid code.
- `POST /api/friends/requests/{friendshipId}/accept` accepts incoming request.
- Self, duplicate, and unknown code return stable responses.
- Refresh/API restart with same database preserves friend code and list.

## 16. Behavior Test Cases

- Friends button opens the Friends view.
- Empty state appears with no friends.
- Valid request shows outgoing pending state.
- Accepting request shows the friend row for both players.
- Invalid/self/duplicate errors are visible and recoverable.
- Refresh keeps the friend code and list.
- Mobile viewport keeps the Friends form and rows usable.

## 17. Rollback and Migration

Initial friend tables can be introduced with the current MVP schema initializer. Before production,
replace `EnsureCreatedAsync()` table creation with migrations that preserve existing session and
battle tables.

## Review Evidence

- GamePM: APPROVED, scope remained friend-code/request/list MVP only.
- Dev: APPROVED, server-side lifecycle, SQLite persistence, normalized duplicate guard, and
  ProblemDetails snapshot handling are implemented.
- Asset: APPROVED, Friends UI uses original CSS initials/placeholders, accessible status text, and
  no protected social art or audio.
- QA: APPROVED, domain/API/e2e evidence covers two-session request/accept, invalid/self/duplicate,
  forbidden API behavior, refresh/restart persistence, desktop, mobile, and reduced motion coverage.
- StudioLead: VERIFIED after full `npm.cmd test` passed on 2026-07-18, including docs/web/build,
  Domain 21, Application 9, API integration 24, and Playwright 30/30 across desktop/mobile Chromium.
