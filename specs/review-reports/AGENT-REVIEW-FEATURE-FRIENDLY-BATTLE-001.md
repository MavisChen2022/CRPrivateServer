# AGENT-REVIEW-FEATURE-FRIENDLY-BATTLE-001: Friendly Battle Invite

## Status

APPROVED

## Requirement

FEATURE-FRIENDLY-BATTLE-001

## Review Date

2026-07-18

## GamePM Review

APPROVE TO START. The MVP is accepted-friends-only friendly battle invites: one guest challenges an
accepted friend, the friend accepts or declines, acceptance creates a private non-ranked room, and
the battle reuses the verified online battle loop. It must not promise ranked ladder, rewards, chat,
clans, rematches, spectator mode, or full Clash Royale parity.

## Dev Review

APPROVE TO START. The architecture should add a friendly battle invite service and SQLite invite
table, authorize through accepted friendship records, derive identity from `royale_session`, and
reuse `OnlineBattleEngine` plus online room persistence instead of creating another battle stack.

## Asset Review

APPROVE TO START. The UI should add Friends challenge controls, incoming/outgoing challenge states,
visible status messages, mobile/reduced-motion-safe layouts, and placeholder-only battle reuse with
no protected Clash Royale art, audio, fonts, or official UI frames committed to the public repo.

## QA Review

APPROVE TO START. The gate requires domain/application/API tests plus two-browser-context Playwright
evidence for friendship-required invites, duplicate/cancel/decline/expire/accept lifecycle, room
creation, owner isolation, reconnect, no reward mutation, mobile, reduced motion, persistence, and
response privacy.

## Required Corrections

- Add implementation, integration tests, Playwright behavior tests, and final review before moving to
  `VERIFIED`.
- Keep friendly battle room creation separate from public matchmaking.
- Do not introduce protected assets, ranked/reward claims, chat, clans, rematches, or spectator mode.

## Current Evidence

- Requirement/BDD/SDD/unit plan/QA gate: approved to start.
- Four-agent docs approval: complete.
- Code implementation: pending.
- Full gate: pending.
