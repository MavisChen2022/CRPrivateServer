# SDD-FEATURE-SESSION-001: Guest Session Entry

## Status

APPROVED

## Requirement

FEATURE-SESSION-001

## 1. Purpose

Provide a secure guest session flow so a browser can enter the playable home screen without
registration while the server remains the authority for identity.

## 2. User Flow

1. The browser loads the React home page.
2. The client calls `GET /api/session`.
3. The server validates the `royale_session` cookie.
4. If valid, the existing guest player is returned.
5. If missing or invalid, the server creates a guest account and player profile.
6. The server sets a new HttpOnly cookie and returns the player profile.
7. The UI renders home, friends, deck, and battle entry states.

## 3. Business Rules

- Guest display names use the format `Player######`.
- The browser never chooses the authoritative player id.
- Invalid cookies are recovered by creating a new guest.
- Guest accounts can later be linked without losing progress.
- Clearing browser data can make an unlinked guest unrecoverable.

## 4. System Components

- `Game.Api`: session endpoint and cookie issuance.
- `Game.Application`: guest session service and player creation.
- `Game.Domain`: player profile, account type, and session token rules.
- `Game.Infrastructure`: SQLite persistence and token hashing.
- `Game.Web`: React shell and Phaser mount point.

## 5. API Contract

`GET /api/session`

Success response:

```json
{
  "playerId": "uuid",
  "displayName": "Player123456",
  "trophies": 0,
  "gold": 100,
  "accountType": "Guest",
  "guestWarning": "Clearing browser data can lose access to this guest."
}
```

## 6. SQLite Data Design

- `USER_ACCOUNTS`: `USER_ID`, `ACCOUNT_TYPE`, `STATUS`, `CREATED_AT`.
- `PLAYER_PROFILES`: `PLAYER_ID`, `USER_ID`, `DISPLAY_NAME`, `TROPHIES`, `GOLD`.
- `SESSION_TOKENS`: `TOKEN_ID`, `USER_ID`, `TOKEN_HASH`, `CREATED_AT`, `EXPIRES_AT`, `REVOKED_AT`.

## 7. Redis Cache Key Design

Guest session identity is not stored only in Redis. Redis can later cache profile lookups, but
SQLite remains the source of truth.

## 8. SignalR Contract

No SignalR connection is required to create a guest session. After session creation, SignalR
hubs derive the player id from the authenticated cookie, never from a client-sent player id.

## 9. State Flow

```text
NoCookie -> CreateGuest -> SetCookie -> HomeReady
ValidCookie -> LoadGuest -> HomeReady
InvalidCookie -> CreateGuest -> SetCookie -> HomeReady
ApiError -> ErrorState -> Retry
```

## 10. Error Handling

- SQLite unavailable: return `503 SessionStoreUnavailable`.
- Token validation failure: recover as a new guest.
- Unexpected exception: return `500 SessionUnexpectedError` without token details.

## 11. Security and Anti-Cheat

- Cookie name: `royale_session`.
- Production cookie flags: `HttpOnly`, `Secure`, `SameSite=Lax`.
- Development can disable `Secure` only through environment configuration.
- Store only a hash of the session token.
- Never log raw session tokens, cookies, or token hashes.
- Ignore client-supplied player ids for identity.

## 12. Transactions, Concurrency, and Idempotency

Guest account, player profile, and session token are created in one SQLite transaction. Two
parallel first-load requests can create two guests, but each returned cookie maps to exactly one
profile and cannot impersonate another profile.

## 13. Monitoring and Logs

- Count guest sessions created.
- Count invalid cookie recoveries.
- Count session store errors.
- Log correlation ids without raw cookies.

## 14. Unit Test Cases

- Creating a guest creates account, profile, and token result.
- Valid token returns existing profile.
- Invalid token creates a new guest result.
- Token hash validation does not accept raw player ids.

## 15. Integration Test Cases

- `GET /api/session` without cookie sets `royale_session`.
- Reusing cookie returns the same player.
- Invalid cookie returns a new player.
- Response does not expose session token.

## 16. Behavior Test Cases

- New browser context sees a guest home screen.
- Refresh keeps the same display name.
- Tampering with client storage does not change server identity.
- Reduced motion keeps the home screen usable.

## 17. Rollback and Migration

Initial migration creates the three session tables. Rollback drops only empty development tables;
production rollback requires backup and explicit operator approval.

## Review Evidence

- GamePM: APPROVED, behavior matches FEATURE-SESSION-001.
- QA: APPROVED, security and test cases are present.
- Asset: APPROVED, UI states and testability are present.
- StudioLead: SDD Ready.

