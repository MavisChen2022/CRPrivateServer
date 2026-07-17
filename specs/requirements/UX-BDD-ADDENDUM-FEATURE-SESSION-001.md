# UX-BDD-ADDENDUM-FEATURE-SESSION-001

## Status

CHANGES_REQUESTED

## Requirement

FEATURE-SESSION-001

## Owner

GamePM

## Purpose

Define the player-visible placeholder behavior for the guest home screen so the MVP does not
over-promise systems that are not implemented yet.

## Home Button Behavior

### Start Battle

- The button must be enabled only after the guest profile is loaded.
- Clicking it must open a visible battle-entry placeholder, not silently do nothing.
- The placeholder must say that solo sandbox battle is being prepared for the next feature.
- The player must remain on the home screen and keep their guest identity.
- The placeholder must expose a stable test id: `battle-entry-placeholder`.

### Friends

- The button must be enabled only after the guest profile is loaded.
- Clicking it must open an empty-state panel, not silently do nothing.
- The empty state must explain that friend features will unlock after the private server social
  layer is added.
- The player must be able to close the panel and return to the home screen.
- The empty state must expose a stable test id: `friends-empty-state`.

### Deck

- The button must be enabled only after the guest profile is loaded.
- Clicking it must open an starter-deck placeholder showing that the guest has a default deck.
- The placeholder must not imply editing, upgrading, or card collection is available yet.
- The player must be able to close the panel and return to the home screen.
- The placeholder must expose a stable test id: `deck-placeholder`.

## API Retry Behavior

- If the session API is unavailable, the home screen must show a recoverable error state.
- The error state must expose a stable test id: `home-error`.
- The retry command must call the session API again without requiring a full browser refresh.
- If retry succeeds, the player must reach the ready home screen.
- The retry command must expose a stable test id: `session-retry-button`.

## Expired Cookie Behavior

- An expired `royale_session` cookie must be treated as recoverable, not fatal.
- The server may create a new guest profile when the expired cookie cannot be renewed.
- The UI must show the playable home screen after recovery.
- The guest warning must remain visible so the player understands browser data affects access.

## Mobile Behavior

- The home screen must be usable at 390x844 and 360x740 viewports.
- The resource bar, player identity, home buttons, warning, and battle preview must not overlap.
- Home buttons must remain reachable without horizontal scrolling.
- Placeholder panels must fit the viewport and remain dismissible.

## Reduced Motion Behavior

- The UI must respect `prefers-reduced-motion: reduce`.
- Decorative animation and Phaser tweens must be disabled or replaced with static states.
- Reduced motion must not hide controls, loading feedback, error recovery, or placeholder panels.

## Multi-Tab Expectations

- Opening a second tab in the same browser context must show the same guest profile because the
  HttpOnly cookie is shared by that browser context.
- Opening a separate browser context with no cookie must create a separate guest profile.
- If both same-context tabs refresh, both must continue to show the same guest profile.

## Acceptance Addendum

- Start Battle, Friends, and Deck have explicit visible placeholder outcomes.
- API failure can recover through retry.
- Expired cookies recover to a playable home screen.
- Mobile and reduced-motion behavior are player-visible requirements, not optional polish.
- Multi-tab behavior is defined by browser cookie context.

