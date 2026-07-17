# FEATURE-SESSION-001: Guest Session Entry

## Status

APPROVED

## Owner

GamePM

## Reviewers

- Dev: APPROVED
- QA: APPROVED
- Asset: APPROVED
- StudioLead Gate: BDD Ready

## Player Goal

A new player can open the web game and reach the playable home screen without creating an
account, while a returning guest keeps the same browser session after refresh.

## Scope

- Create a guest player when no valid session exists.
- Store identity in a server-issued `royale_session` HttpOnly cookie.
- Show player name, trophy count, deck access, friends access, and a start battle command.
- Warn guest players that clearing browser data can lose access.
- Recover safely when a cookie is missing, expired, or invalid.

## Out of Scope

- Email, Google, or Discord account linking.
- Ranked matchmaking.
- Full card progression.
- Payment or store features.

## Acceptance Criteria

- A new browser context opens the home page and receives a guest profile.
- Refreshing the page keeps the same guest profile.
- A tampered client-side player id cannot impersonate another player.
- An invalid cookie creates a new guest instead of failing the page.
- The UI includes loading, error, empty, and reduced-motion safe states.

## Scenarios

- `@FEATURE-SESSION-001 @guest @happy-path`
- `@FEATURE-SESSION-001 @guest @refresh`
- `@FEATURE-SESSION-001 @guest @invalid-cookie`
- `@FEATURE-SESSION-001 @guest @tamper-resistant`

## Risks

- Guest-only players can lose progress if their browser data is removed.
- Secure cookies require HTTPS in production and a local development exception.
- Public repository visibility means third-party game assets must not be committed by default.

## Test Evidence

- Document gate: `npm.cmd run test:docs`

