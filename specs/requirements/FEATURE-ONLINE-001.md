# FEATURE-ONLINE-001: Online Battle Room

## Status

VERIFIED

## Owner

GamePM

## Reviewers

- Dev: APPROVE TO START
- QA: APPROVE TO START
- Asset: APPROVE TO START
- StudioLead Gate: APPROVED

## Player Goal

Two guest players can enter matchmaking, be paired into one server-authoritative online battle room,
submit starter-card commands, refresh back into the same room, and see a clear win, loss, or timeout
result without ranked ladder, purchases, clans, chat, or full Clash Royale parity.

## Scope

- Add an Online Battle entry point from the guest home screen.
- Queue two valid guest sessions into one battle room.
- Let a waiting player cancel matchmaking before being paired.
- Persist battle room membership, status, snapshots, commands, and results in SQLite.
- Derive player identity only from `royale_session`.
- Let each player deploy public-safe starter cards on their own side.
- Advance a shared server-owned battle snapshot.
- Show both player names, tower hit points, elixir, timer, units, and result.
- Let a player refresh or reopen the page and rejoin their active online battle.
- Reject invalid commands, non-participant room access, and missing sessions with stable codes.

## Out of Scope

- Ranked matchmaking, trophy changes, chest rewards, shops, purchases, clans, chat, and emotes.
- Full Clash Royale card data, balance, animations, fonts, sounds, or art.
- Friendly battle invites; those require a later friend-specific slice.
- Redis-backed distributed queues; the MVP can use SQLite and in-process coordination.

## Acceptance Criteria

- A guest can choose Online Battle and enter a waiting state.
- A waiting guest can cancel matchmaking and return to the lobby without being paired later.
- A second guest can queue and both guests receive the same battle room id.
- Each participant sees their own side, opponent name, both tower HP values, elixir, timer, and result area.
- A valid starter-card command from either participant consumes that player's elixir and creates a server-owned unit.
- Invalid card, invalid lane, enemy-side placement, insufficient elixir, or non-participant access returns stable validation codes.
- Refreshing during an active battle restores the same room for each participant.
- The battle ends when a tower is destroyed or the timer expires, and both players see compatible results.
- Mobile and reduced-motion modes remain usable without relying on animation.
- Missing optional imported assets never block online battle play.

## Scenarios

- `@FEATURE-ONLINE-001 @online @queue`
- `@FEATURE-ONLINE-001 @online @cancel`
- `@FEATURE-ONLINE-001 @online @match`
- `@FEATURE-ONLINE-001 @online @deploy`
- `@FEATURE-ONLINE-001 @online @invalid-command`
- `@FEATURE-ONLINE-001 @online @owner-isolation`
- `@FEATURE-ONLINE-001 @online @refresh`
- `@FEATURE-ONLINE-001 @online @result`
- `@FEATURE-ONLINE-001 @online @mobile`
- `@FEATURE-ONLINE-001 @online @reduced-motion`
- `@FEATURE-ONLINE-001 @online @asset-fallback`

## Risks

- Client-side player ids must not decide room ownership or command authority.
- Two browser contexts can race to enter matchmaking or submit commands.
- A one-process queue can lose waiting players on restart; persistence must make this visible and recoverable.
- Protected Clash Royale assets must stay out of the public repository.

## Test Evidence

- Full gate: `npm.cmd test`
- Document gate: `npm.cmd run test:docs`
- API gate: `dotnet test CRPrivateServer.sln`
- Web gate: `npm.cmd run test:web` and `npm.cmd run test:web:build`
- Behavior gate: `npm.cmd run test:e2e`
- Verified on 2026-07-18 with `npm.cmd test`: docs validation passed, web validation passed,
  Vite build passed, `dotnet test CRPrivateServer.sln` passed with Domain 31, Application 14, and
  API integration 31 tests, and Playwright passed 36/36 across desktop and mobile Chromium.

## Review Result

GamePM, Dev, Asset, and QA verified this slice on 2026-07-18 as a two-guest online battle room MVP
with matchmaking, cancel queue, shared server-authoritative commands, reconnect, result coverage,
mobile, reduced-motion, owner isolation, and public-safe asset fallback gates.
