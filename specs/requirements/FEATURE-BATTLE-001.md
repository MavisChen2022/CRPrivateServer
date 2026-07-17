# FEATURE-BATTLE-001: Solo Sandbox Battle Loop

## Status

IN_REVIEW

## Owner

GamePM

## Reviewers

- Dev: APPROVE TO START
- QA: APPROVE TO START
- Asset: APPROVE TO START
- StudioLead Gate: IN_REVIEW

## Player Goal

A guest player can press Start Battle and play a short solo training battle where the server owns
the battle state, placement rules, elixir, tower damage, timer, and result.

## Scope

- Start a solo sandbox battle from an approved guest session.
- Use a fixed public-safe starter deck with placeholder cards such as `Training Knight` and
  `Training Archer`.
- Let the player deploy a unit on their own half of the arena when enough elixir is available.
- Advance deterministic server-side battle ticks that move units, damage the enemy tower, regenerate
  elixir, and resolve win or timeout results.
- Persist the battle snapshot so refresh reloads the current battle for the same guest.
- Render a playable arena with original placeholder visuals and optional local imported assets.

## Out of Scope

- PvP matchmaking, real-time multiplayer, clans, ranked trophies, and league rewards.
- Complete Clash Royale card data, card names, balance, animations, or audio.
- Client-authoritative damage, elixir, timer, or battle result.
- Committing protected Clash Royale art, sound effects, fonts, or extracted data.

## Acceptance Criteria

- A guest can start a solo battle from the home screen.
- The server rejects battle access when the guest session is missing or belongs to another player.
- Deploying a valid starter card consumes elixir and creates a server-owned unit.
- Deploying with insufficient elixir, an invalid card, an invalid lane, or enemy-half placement is
  rejected with a stable validation code.
- Server ticks move units, damage the enemy tower, regenerate elixir, and eventually produce a
  deterministic result.
- Refreshing the page reloads the same battle snapshot for the same guest.
- The battle UI is usable on desktop and mobile and remains usable with reduced motion enabled.
- Missing local imported assets never block battle play.

## Scenarios

- `@FEATURE-BATTLE-001 @battle @start`
- `@FEATURE-BATTLE-001 @battle @deploy`
- `@FEATURE-BATTLE-001 @battle @invalid-command`
- `@FEATURE-BATTLE-001 @battle @tick`
- `@FEATURE-BATTLE-001 @battle @result`
- `@FEATURE-BATTLE-001 @battle @refresh`
- `@FEATURE-BATTLE-001 @battle @mobile`
- `@FEATURE-BATTLE-001 @battle @reduced-motion`
- `@FEATURE-BATTLE-001 @battle @asset-fallback`

## Risks

- Letting the Phaser or React layer decide battle results would create a cheating and multiplayer
  migration risk.
- Reusing protected Clash Royale assets in the repository would violate the public-safe asset policy.
- A battle slice that over-promises PvP or full Clash balance would mislead players and tests.
- Persisting only client state would make refresh and ownership tests meaningless.

## Test Evidence

- Full gate: `npm.cmd test`
- Document gate: `npm.cmd run test:docs`
- API gate: `dotnet test CRPrivateServer.sln`
- Behavior gate: `npm.cmd run test:e2e`

## Review Result

Four-agent start review on 2026-07-18 approved beginning this slice with a strict MVP scope:
server-authoritative solo sandbox, public-safe placeholder visuals, optional ignored local assets,
and automated evidence before approval.
