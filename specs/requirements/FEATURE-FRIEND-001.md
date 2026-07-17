# FEATURE-FRIEND-001: Friend Code and Friends List

## Status

VERIFIED

## Owner

GamePM

## Reviewers

- Dev: APPROVED
- QA: APPROVED
- Asset: APPROVED
- StudioLead Gate: VERIFIED

## Player Goal

A guest player can open Friends, see their own friend code, send a friend request by another
player's friend code, accept incoming requests, and persist a small friends list without chat,
online presence, or friendly battle promises.

## Scope

- Show a Friends view from the guest home screen.
- Generate and persist a public friend code for each guest player.
- Let a player send a friend request by friend code.
- Let the addressee accept or reject the incoming request.
- Persist pending, accepted, and rejected friendship records in SQLite.
- Show an empty state when no friends exist.
- Show friend display name, short player id, and an honest status placeholder.
- Reject invalid friend code, self-add, and duplicate add attempts with stable validation codes.

## Out of Scope

- Chat, clans, guilds, live online presence, push notifications, and friend battle invites.
- Account linking or cross-device recovery guarantees beyond the approved guest session.
- Exposing raw session tokens, token hashes, or private player identifiers as friend codes.
- Protected Clash Royale UI art, sounds, or social icons.

## Acceptance Criteria

- A guest can open Friends and see their own friend code.
- A player with no friends sees an empty state.
- Submitting a valid friend code creates a pending request visible to the target player.
- Accepting an incoming request persists the friend and shows them in both players' lists.
- Self-add is rejected with `CannotAddSelf`.
- Duplicate or reverse duplicate requests are rejected or idempotently return the existing relation
  without duplicate rows.
- Invalid or unknown friend code is rejected with `FriendCodeNotFound`.
- Refreshing the Friends view keeps the same friend code and friends list for the same guest session.
- Missing or invalid session is handled through the approved session gate.
- Mobile layout keeps code, input, add button, and friend rows reachable without overlap.

## Scenarios

- `@FEATURE-FRIEND-001 @friends @open`
- `@FEATURE-FRIEND-001 @friends @empty`
- `@FEATURE-FRIEND-001 @friends @request`
- `@FEATURE-FRIEND-001 @friends @accept`
- `@FEATURE-FRIEND-001 @friends @self-add`
- `@FEATURE-FRIEND-001 @friends @duplicate`
- `@FEATURE-FRIEND-001 @friends @invalid-code`
- `@FEATURE-FRIEND-001 @friends @refresh`
- `@FEATURE-FRIEND-001 @friends @mobile`

## Risks

- Friend codes must not be derived from session tokens or token hashes.
- Duplicate rows can appear without a database uniqueness rule or application idempotency guard.
- Friends UI can over-promise chat, presence, or friend battles before those features exist.
- Guest-only friend lists can be lost if the guest session is lost.

## Test Evidence

- Full gate: `npm.cmd test`
- Document gate: `npm.cmd run test:docs`
- API gate: `dotnet test CRPrivateServer.sln` passed on 2026-07-18 with Domain 21,
  Application 9, and API integration 24 tests passing.
- Web gate: `npm.cmd run test:web` and `npm.cmd run test:web:build` passed on 2026-07-18.
- Behavior gate: `npm.cmd run test:e2e` passed 30/30 on 2026-07-18 across desktop and mobile Chromium.

## Review Result

GamePM, Dev, Asset, and QA approved the implemented friend-code, friend-request, and friends-list
MVP on 2026-07-18 after domain/API/web/e2e evidence passed. The slice does not approve chat,
presence, clans, or friendly battle invites.
