# AGENT-REVIEW-FEATURE-BATTLE-001: Solo Sandbox Battle Loop

## Status

IN_REVIEW

## Requirement

FEATURE-BATTLE-001

## Review Date

2026-07-18

## GamePM Review

APPROVE TO START. The slice is accepted only as a solo training sandbox: guest home to Start Battle,
deploy a public-safe test card, damage a tower, reach a win or timeout result, and return home or
replay. The UI must not imply PvP matchmaking, real opponents, or full Clash Royale parity.

## Dev Review

APPROVE TO START. The implementation must keep battle state server-authoritative through
Domain/Application/API layers. Phaser and React may render snapshots and submit inputs, but must not
own HP, elixir, timer, placement validation, or result calculation.

## Asset Review

APPROVE TO START. The repo may use original placeholder arena, unit, tower, card, and audio fallback
assets. Local Clash Royale materials may only be optional ignored overrides under `assets/imported/`
and must not be committed or required for tests/builds.

## QA Review

APPROVE TO START. Approval requires automated evidence for domain rules, API ownership and invalid
commands, Playwright start/deploy/tower/result interactions, mobile layout, reduced motion, and
missing imported asset fallback.

## Required Corrections

- Keep scope named solo sandbox until PvP is separately designed and tested.
- Add docs validation for every battle gate document before implementation starts.
- Add server-side domain tests before wiring the UI.
- Add API ownership and invalid command tests before approving release.
- Add Playwright battle behavior tests before moving to `VERIFIED`.

## Current Evidence

- Four-agent start review: complete.
- BDD/SDD/unit plan/QA gate: in progress.
- Code implementation: pending.
- Full gate: pending.
