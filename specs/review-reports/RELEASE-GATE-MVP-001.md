# RELEASE-GATE-MVP-001: Browser Private Royale MVP

## Status

VERIFIED

## Scope

Release readiness for the public-safe browser MVP:

- FEATURE-SESSION-001: guest session.
- FEATURE-FRIEND-001: friend code and friend request lifecycle. This single feature gate covers
  MVP item 2, friend code, and MVP item 3, friend request.
- FEATURE-BATTLE-001: server-authoritative solo battle sandbox.
- FEATURE-ONLINE-001: online matchmaking battle room.
- FEATURE-FRIENDLY-BATTLE-001: accepted-friend challenge into a private battle.

Out of scope for this MVP release:

- Proprietary Clash Royale assets or audio committed to Git.
- Ranked ladder, trophy mutation from friendly battles, purchases, clans, chat, spectators, rematch, or production anti-cheat.
- SignalR push delivery; current verified behavior uses HTTP polling and durable SQLite state.
- Redis-backed presence, matchmaking, and rate limiting.

## Agent Review

| Agent | Gate | Result |
|---|---|---|
| GamePM | MVP player journey and scope | APPROVED |
| Dev | Architecture, persistence, server authority | APPROVED |
| Asset | Public-safe assets, UI states, accessibility | APPROVED |
| QA | Automated evidence and regression risk | APPROVED |

## Verification Evidence

Full verification passed on 2026-07-18:

```powershell
npm.cmd test
```

This command includes:

- `npm run test:docs`
- `npm run test:web`
- `npm run test:web:build`
- `dotnet test CRPrivateServer.sln`
- `npm run test:e2e`

Latest passing counts:

- Domain tests: 31.
- Application tests: 22.
- API integration tests: 36.
- Playwright behavior tests: 38/38.

## Release Notes

- The MVP is playable in the browser with original placeholder visuals and optional ignored local asset overrides.
- The server remains authoritative for sessions, battles, matchmaking, friend requests, and friendly battle invites.
- SQLite persistence covers sessions, profiles, solo battles, friend codes, friendships, online rooms, commands, queue entries, and friendly battle invites.
- `assets/imported/` remains ignored and empty in Git tracking.
- The release target is GitHub `main` for `MavisChen2022/CRPrivateServer`.

## Follow-Up Backlog

These items are recommended after MVP release and are not blocking:

- Add explicit friendly invite expiry UI and push-style notifications.
- Add transaction-level guards around high-concurrency friendly invite acceptance and matchmaking.
- Add formal database migrations instead of startup `CREATE TABLE IF NOT EXISTS` bootstrapping.
- Add public-safe deck/card metadata import tooling with ignored local asset overrides.
- Add optional public-safe audio hooks and user-controlled sound settings.
