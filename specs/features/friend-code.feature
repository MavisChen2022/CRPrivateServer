@FEATURE-FRIEND-001
Feature: Friend code and friends list
  Guest players should be able to discover their own friend code, send requests by code, and accept
  incoming requests without chat, presence, or friendly battle promises.

  @friends @open
  Scenario: Guest opens Friends
    Given the player has a valid guest session
    When the player opens Friends
    Then the player sees their friend code
    And the player sees an add-friend input
    And the UI does not promise chat, presence, or friend battles

  @friends @empty
  Scenario: Player has no friends
    Given the player has a valid guest session
    And the player has no friends
    When the player opens Friends
    Then the player sees an empty state

  @friends @request
  Scenario: Player sends a friend request by friend code
    Given two guests have valid friend codes
    When the first player submits the second player's friend code
    Then the server creates a pending friend request
    And the first player sees the request as pending

  @friends @accept
  Scenario: Player accepts an incoming friend request
    Given one guest sent another guest a friend request
    When the second player accepts the request
    Then both players see each other in their friends lists

  @friends @self-add
  Scenario: Player cannot add self
    Given the player has a valid friend code
    When the player submits their own friend code
    Then the server rejects the request with CannotAddSelf
    And the friends list remains playable

  @friends @duplicate
  Scenario: Player cannot create duplicate friend rows
    Given the player already sent or accepted a request for another guest
    When the player submits the same friend code again
    Then the server returns the existing relation or DuplicateFriend
    And the friends data contains only one relation for that pair

  @friends @invalid-code
  Scenario: Unknown friend code is rejected
    Given the player has a valid guest session
    When the player submits an unknown friend code
    Then the server rejects the request with FriendCodeNotFound
    And the player can correct the code and retry

  @friends @refresh
  Scenario: Friends list persists after refresh
    Given the player has accepted another guest
    When the player refreshes the page
    Then the same friend code is shown
    And the existing friend remains in the friends list

  @friends @mobile
  Scenario: Friends view works on a mobile viewport
    Given the player opens Friends on a mobile viewport
    Then the friend code, input, add button, empty state, and friend rows remain reachable
    And text and controls do not overlap
