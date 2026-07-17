@FEATURE-ONLINE-001
Feature: Online battle room
  Two guest players should be able to match into a server-authoritative online battle room, submit
  starter-card commands, reconnect after refresh, and see compatible results without ranked or
  full-card-system promises.

  @online @queue
  Scenario: First guest waits for an opponent
    Given the player has a valid guest session
    When the player chooses Online Battle
    Then the player enters matchmaking
    And the UI shows a waiting state without promising ranked rewards

  @online @cancel
  Scenario: Waiting guest cancels matchmaking
    Given the player is waiting in matchmaking
    When the player cancels matchmaking
    Then the player returns to the online battle lobby
    And the cancelled queue entry cannot be matched later

  @online @match
  Scenario: Two guests are paired into one room
    Given two guests enter matchmaking
    When the server pairs them
    Then both guests receive the same online battle room id
    And each guest sees their own side and the opponent name

  @online @deploy
  Scenario: A participant deploys a starter card
    Given two guests are in an active online battle
    When one guest deploys a valid starter card on their own side
    Then the server consumes that player's elixir
    And both guests can observe the created unit in the shared snapshot

  @online @invalid-command
  Scenario: Invalid online battle command is rejected
    Given a guest is in an active online battle
    When the guest submits an unknown card, invalid lane, enemy-side placement, or insufficient-elixir command
    Then the server rejects the command with a stable validation code
    And the shared snapshot remains authoritative

  @online @owner-isolation
  Scenario: Non-participant cannot control a room
    Given two guests are in an active online battle
    And a third guest has a valid guest session
    When the third guest reads or commands the room
    Then the server rejects the request with OnlineBattleForbidden

  @online @refresh
  Scenario: Participant reconnects after refresh
    Given a guest is in an active online battle
    When the guest refreshes the page
    Then the same room id and latest shared snapshot are restored

  @online @result
  Scenario: Online battle reaches a result
    Given two guests are in an active online battle
    When a tower is destroyed or the timer expires
    Then both guests see compatible win, loss, draw, or timeout results

  @online @mobile
  Scenario: Online battle works on a mobile viewport
    Given two guests are matched on a mobile viewport
    Then command controls, towers, timer, elixir, opponent state, and result remain reachable
    And text and controls do not overlap

  @online @reduced-motion
  Scenario: Online battle works with reduced motion
    Given two guests are matched with reduced motion enabled
    Then the battle remains playable without animation-dependent feedback

  @online @asset-fallback
  Scenario: Missing imported assets do not block online battle
    Given no local imported arena, unit, tower, or sound assets are available
    When two guests enter an online battle
    Then original placeholder visuals and silent fallbacks keep the battle playable
