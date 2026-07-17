# AGENT-REVIEW-FEATURE-SESSION-001

## Status

CHANGES_REQUESTED

## Requirement

FEATURE-SESSION-001

## Date

2026-07-17

## StudioLead Summary

The four-agent review was run after the initial skeleton was committed. All four specialist
agents rejected release readiness. The current code builds and has a useful first skeleton, but
the repository previously overstated approval status. This report corrects the gate state and
records the next required work.

## GamePM Review

Status: CHANGES_REQUESTED

- The player-visible guest entry promise is valid.
- Start Battle, Friends, and Deck buttons currently do nothing and over-promise the MVP.
- Missing scenarios include API retry, expired cookie recovery, mobile, reduced motion, and
  multi-tab expectations.
- Requested next documents: behavior test plan, home-screen UX states, battle-entry placeholder,
  deck empty state, and friends empty state.

## Dev Review

Status: CHANGES_REQUESTED

- Current implementation uses an in-memory singleton instead of the SDD's SQLite infrastructure.
- No `Game.Infrastructure`, EF Core model, migration, repository, or transaction exists.
- The raw token generator and hash strategy are acceptable only as temporary skeleton code.
- Requested next code tasks: add persistence, repository interfaces, transaction handling,
  real xUnit tests, API integration tests, and Playwright behavior coverage.

## Asset Review

Status: CHANGES_REQUESTED

- Basic loading, error, and ready states exist, but empty states are missing.
- Phaser canvas needs accessible labeling and fallback text.
- Reduced motion is only sampled at initial render.
- Phaser is bundled into the main page and should later be lazy-loaded.
- The asset policy is directionally correct: keep `assets/imported/` ignored and do not commit
  protected Clash assets to a public repository.

## QA Review

Status: CHANGES_REQUESTED

- `npm.cmd test` passes, but it does not prove integration or behavior readiness.
- The current test project is a console smoke runner, not xUnit.
- Missing tests include no-cookie session creation, valid cookie reuse, invalid cookie replacement,
  cookie flags, no token exposure, tampered player id, Playwright Gherkin flows, and persistence
  across restart once SQLite is implemented.

## Required Corrections

1. Keep traceability at `CHANGES_REQUESTED` until xUnit, API integration, and Playwright evidence exist.
2. Add `Game.Infrastructure` with SQLite-backed guest session persistence.
3. Replace in-memory session storage or explicitly revise the SDD if in-memory is only a temporary spike.
4. Add real xUnit tests for domain and application service behavior.
5. Add API integration tests with `WebApplicationFactory`.
6. Add Playwright behavior tests for `guest-session.feature`.
7. Add UI placeholder behavior and accessible canvas fallback.

## Evidence

- GamePM subagent: CHANGES_REQUESTED.
- Dev subagent: CHANGES_REQUESTED.
- Asset subagent: CHANGES_REQUESTED.
- QA subagent: CHANGES_REQUESTED.
- StudioLead action: statuses corrected and review report added.

