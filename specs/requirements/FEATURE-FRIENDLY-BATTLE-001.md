# FEATURE-FRIENDLY-BATTLE-001: Friendly Battle Invite

## Status

IN_REVIEW

## Owner

GamePM

## Reviewers

- Dev: PENDING
- QA: PENDING
- Asset: PENDING
- StudioLead Gate: PENDING

## Player Goal

Two guest players who are already friends can invite each other into a private, non-ranked battle
room, accept or decline the challenge, reconnect to the room after refresh, and finish the match
without trophy changes, rewards, chat, clans, purchases, or ranked matchmaking promises.

## Scope

- Add a friendly battle action to accepted friends in the Friends view.
- Persist friendly battle invites in SQLite with challenger, recipient, status, timestamps, and
  optional linked online battle room id.
- Allow the challenger to cancel a pending invite.
- Allow the recipient to accept or decline a pending invite.
- Create a server-authoritative online battle room only after the recipient accepts.
- Reuse the public-safe starter deck, placeholder arena, and `OnlineBattleEngine` room snapshot.
- Derive all player identity and invite authority from `royale_session`.
- Reject invites between non-friends, self-invites, duplicate pending invites, missing sessions, and
  non-participant room access with stable problem codes.
- Show pending outgoing and incoming friendly battle challenges in the Friends view.
- Preserve active friendly battle room access after page refresh.
- Keep all trophy, gold, reward, ranked, clan, chat, emote, and purchase state unchanged.

## Out of Scope

- Ranked matchmaking, trophy changes, rewards, shops, purchases, clans, chat, and emotes.
- Spectator mode, rematch flow, tournaments, or multiple simultaneous party members.
- Push notifications outside the current browser session.
- Full Clash Royale card data, protected images, protected audio, proprietary fonts, or official UI.
- SignalR is optional for this slice; polling can satisfy the MVP gate.

## Acceptance Criteria

- A guest can see a Friendly Battle action only for accepted friends.
- A player cannot challenge themselves or a non-friend.
- A player can create one pending friendly battle invite to an accepted friend.
- The recipient can see the incoming invite and either accept or decline it.
- The challenger can cancel a pending invite before it is accepted.
- Accepting an invite creates one shared online battle room for the two friends.
- Both participants see the same room id, their own side, opponent name, tower HP, elixir, timer,
  units, and result area.
- Refreshing during an active friendly battle restores the same room for each participant.
- Rejected, cancelled, expired, or already-accepted invites cannot be accepted later.
- Non-participants cannot read or command the resulting battle room.
- Friendly battles do not mutate trophies, gold, friendship status, or account type.
- Mobile and reduced-motion modes remain usable without animation-dependent feedback.
- Missing optional imported assets never block inviting, accepting, or playing.

## Scenarios

- `@FEATURE-FRIENDLY-BATTLE-001 @friendly-battle @friend-only`
- `@FEATURE-FRIENDLY-BATTLE-001 @friendly-battle @create-invite`
- `@FEATURE-FRIENDLY-BATTLE-001 @friendly-battle @duplicate-invite`
- `@FEATURE-FRIENDLY-BATTLE-001 @friendly-battle @cancel-invite`
- `@FEATURE-FRIENDLY-BATTLE-001 @friendly-battle @decline-invite`
- `@FEATURE-FRIENDLY-BATTLE-001 @friendly-battle @accept-invite`
- `@FEATURE-FRIENDLY-BATTLE-001 @friendly-battle @room`
- `@FEATURE-FRIENDLY-BATTLE-001 @friendly-battle @owner-isolation`
- `@FEATURE-FRIENDLY-BATTLE-001 @friendly-battle @refresh`
- `@FEATURE-FRIENDLY-BATTLE-001 @friendly-battle @no-rewards`
- `@FEATURE-FRIENDLY-BATTLE-001 @friendly-battle @mobile`
- `@FEATURE-FRIENDLY-BATTLE-001 @friendly-battle @reduced-motion`
- `@FEATURE-FRIENDLY-BATTLE-001 @friendly-battle @asset-fallback`

## Risks

- Friendly battle authority must come from accepted friendship records, not client-sent ids.
- Pending invite races can create duplicate rooms if accept/cancel are not guarded.
- Reusing online battle rooms must not accidentally enter public matchmaking.
- Public repository asset policy must keep protected Clash Royale assets and sounds out of Git.

## Test Evidence

- Document gate: `npm.cmd run test:docs`
- Unit/API gate: `dotnet test CRPrivateServer.sln`
- Web gate: `npm.cmd run test:web` and `npm.cmd run test:web:build`
- Behavior gate: `npm.cmd run test:e2e`
- Full gate: `npm.cmd test`

## Review Result

Pending four-agent review. This document defines the intended friendly battle MVP before
implementation begins.
