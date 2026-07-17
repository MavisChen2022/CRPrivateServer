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
    And the player can open Friends and Deck placeholders

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

  @guest @expired-cookie
  Scenario: Expired session cookie is recovered safely
    Given the browser has an expired game session cookie
    When the player opens the home page
    Then the server recovers the session state safely
    And the player sees a playable home screen
    And the guest warning remains visible

  @guest @api-retry
  Scenario: Player retries after the session API is unavailable
    Given the session API is temporarily unavailable
    When the player opens the home page
    Then the player sees a recoverable home error
    When the player retries after the API is available
    Then the player sees their display name
    And the player sees the ready home screen

  @guest @home-buttons
  Scenario: Home buttons show honest placeholder behavior
    Given the player has a valid guest session
    When the player opens the home page
    Then Start Battle, Friends, and Deck are available
    When the player chooses Start Battle
    Then the player sees a battle-entry placeholder
    When the player chooses Friends
    Then the player sees a friends empty state
    When the player chooses Deck
    Then the player sees a starter-deck placeholder

  @guest @mobile
  Scenario: Guest home is usable on a mobile viewport
    Given the player has no existing game session
    When the player opens the home page on a mobile viewport
    Then the player sees their display name
    And the home buttons remain reachable without horizontal scrolling
    And the battle preview does not overlap the resource bar

  @guest @reduced-motion
  Scenario: Guest home respects reduced motion
    Given the player prefers reduced motion
    When the player opens the home page
    Then decorative motion is reduced
    And loading, retry, and home button controls remain usable

  @guest @multi-tab
  Scenario: Same browser context tabs share the guest session
    Given the player has a valid guest session in one browser tab
    When the player opens the home page in another tab in the same browser context
    Then the same player profile is loaded
    When both tabs refresh
    Then both tabs show the same player profile

  @guest @separate-context
  Scenario: Separate browser context creates a separate guest
    Given the player has a valid guest session in one browser context
    When a separate browser context opens the home page without a cookie
    Then the server creates a separate guest player
    And the two browser contexts do not share player identity
