@FEATURE-BATTLE-001
Feature: Solo sandbox battle loop
  Guest players should be able to start and finish a short server-authoritative training battle
  without PvP, matchmaking, or protected Clash Royale content.

  @battle @start
  Scenario: Guest starts a solo sandbox battle
    Given the player has a valid guest session
    When the player chooses Start Battle
    Then the server creates a solo sandbox battle
    And the player sees the arena, tower hit points, elixir, timer, and starter deck
    And the battle copy does not promise matchmaking or a real opponent

  @battle @deploy
  Scenario: Player deploys a training unit
    Given the player is in an active solo sandbox battle
    And the player has enough elixir for Training Knight
    When the player deploys Training Knight on their own half
    Then the server accepts the deploy command
    And the player's elixir is reduced
    And a unit appears in the battle snapshot

  @battle @invalid-command
  Scenario: Invalid deploy commands are rejected
    Given the player is in an active solo sandbox battle
    When the player deploys an unknown card or deploys on the enemy half
    Then the server rejects the command with a stable validation code
    And the battle snapshot remains playable

  @battle @tick
  Scenario: Server ticks advance the battle
    Given the player has deployed a training unit
    When the server advances deterministic battle ticks
    Then the unit moves toward the enemy tower
    And the enemy tower loses hit points
    And elixir regenerates up to the maximum

  @battle @result
  Scenario: Battle produces a deterministic result
    Given the player is in an active solo sandbox battle
    When the enemy tower is destroyed or the battle timer expires
    Then the server marks the battle as ended
    And the player sees a win or timeout result
    And no additional deploy command is accepted

  @battle @refresh
  Scenario: Battle reloads after refresh
    Given the player has an active solo sandbox battle
    When the player refreshes the page
    Then the same battle snapshot is loaded from the server
    And the player can continue from the server state

  @battle @ownership
  Scenario: Another guest cannot access the battle
    Given one guest owns an active solo sandbox battle
    When another guest requests that battle snapshot
    Then the server rejects the request
    And the battle owner can still continue playing

  @battle @mobile
  Scenario: Solo battle is usable on a mobile viewport
    Given the player starts a solo sandbox battle on a mobile viewport
    When the player selects and deploys a starter card
    Then the card controls, arena, tower hit points, timer, and result area remain reachable
    And no controls overlap in a way that blocks play

  @battle @reduced-motion
  Scenario: Solo battle respects reduced motion
    Given the player prefers reduced motion
    When the player starts and advances a solo sandbox battle
    Then optional tweens, shake, and particle effects are disabled
    And the static battle state remains clear and playable

  @battle @asset-fallback
  Scenario: Missing imported assets do not block battle play
    Given no local imported arena, unit, tower, or sound assets are available
    When the player starts a solo sandbox battle
    Then original placeholder visuals and silent fallback audio are used
    And the battle can still be completed
