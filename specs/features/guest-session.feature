@FEATURE-SESSION-001
Feature: Guest session entry
  New players should be able to open the web game and reach the playable home screen
  without registering an account.

  @guest @happy-path
  Scenario: New player opens the web game
    Given the player has no existing game session
    When the player opens the home page
    Then the server creates a guest player
    And the player sees their display name
    And the player can start a battle

  @guest @refresh
  Scenario: Returning guest refreshes the page
    Given the player already has a valid guest session
    When the player refreshes the home page
    Then the same player profile is loaded
    And the player's deck remains available

  @guest @invalid-cookie
  Scenario: Invalid session cookie is recovered safely
    Given the browser has an invalid game session cookie
    When the player opens the home page
    Then the server creates a new guest player
    And the player sees a playable home screen

  @guest @tamper-resistant
  Scenario: Client-side player id cannot impersonate another player
    Given the player has a valid guest session
    When the player changes client-side storage to another player id
    Then the server still returns the player from the session cookie
    And the player cannot access another profile

