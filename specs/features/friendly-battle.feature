@FEATURE-FRIENDLY-BATTLE-001
Feature: Friendly battle invite
  Accepted friends should be able to challenge each other to a private non-ranked battle room,
  accept or decline the invite, reconnect after refresh, and finish without trophies, rewards,
  chat, clans, purchases, or official card-system promises.

  @friendly-battle @friend-only
  Scenario: Friendly battle action is visible only for accepted friends
    Given two guests are accepted friends
    And a third guest is not their friend
    When the first guest opens the Friends view
    Then the accepted friend row offers a Friendly Battle action
    And the non-friend cannot be challenged

  @friendly-battle @create-invite
  Scenario: Guest creates a friendly battle invite
    Given two guests are accepted friends
    When the first guest challenges the second guest
    Then the first guest sees an outgoing pending friendly battle invite
    And the second guest sees an incoming pending friendly battle invite

  @friendly-battle @duplicate-invite
  Scenario: Duplicate friendly battle invite is rejected
    Given a friendly battle invite is already pending between two friends
    When the challenger submits the same invite again
    Then the server returns a stable DuplicateFriendlyBattleInvite code
    And no second pending invite is created

  @friendly-battle @cancel-invite
  Scenario: Challenger cancels a pending friendly battle invite
    Given a friendly battle invite is pending
    When the challenger cancels the invite
    Then both guests see that the challenge is no longer pending
    And the cancelled invite cannot create a battle room

  @friendly-battle @decline-invite
  Scenario: Recipient declines a pending friendly battle invite
    Given a friendly battle invite is pending
    When the recipient declines the invite
    Then both guests see that the challenge was declined
    And the declined invite cannot be accepted later

  @friendly-battle @accept-invite
  Scenario: Recipient accepts a pending friendly battle invite
    Given a friendly battle invite is pending
    When the recipient accepts the invite
    Then the server creates one shared friendly battle room
    And both friends can enter that room

  @friendly-battle @room
  Scenario: Friends play in the accepted battle room
    Given two friends are in an active friendly battle room
    When either friend deploys a valid starter card on their own side
    Then the server consumes that player's elixir
    And both friends observe the shared server snapshot

  @friendly-battle @owner-isolation
  Scenario: Non-participant cannot access a friendly battle room
    Given two friends are in an active friendly battle room
    And a third guest has a valid guest session
    When the third guest reads or commands the room
    Then the server rejects the request with OnlineBattleForbidden

  @friendly-battle @refresh
  Scenario: Participant reconnects to a friendly battle room
    Given a friend is in an active friendly battle room
    When that player refreshes the page
    Then the same room id and latest shared snapshot are restored

  @friendly-battle @no-rewards
  Scenario: Friendly battle does not mutate rewards
    Given two friends complete a friendly battle
    Then both guests keep the same trophies, gold, friendship status, and account type

  @friendly-battle @mobile
  Scenario: Friendly battle invite works on mobile
    Given two friends use mobile viewports
    When they create and accept a friendly battle invite
    Then invite controls, battle controls, room state, and result remain reachable
    And text and controls do not overlap

  @friendly-battle @reduced-motion
  Scenario: Friendly battle works with reduced motion
    Given two friends have reduced motion enabled
    When they create and accept a friendly battle invite
    Then the invite and battle remain usable without animation-dependent feedback

  @friendly-battle @asset-fallback
  Scenario: Missing imported assets do not block friendly battle
    Given no local imported arena, unit, tower, or sound assets are available
    When two friends play a friendly battle
    Then original placeholder visuals and silent fallbacks keep the flow playable
