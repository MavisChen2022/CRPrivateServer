# CRPrivateServer

CRPrivateServer is a browser-based, server-authoritative royale battle game project.
The first version is a modular monolith: React + TypeScript and Phaser in the browser,
ASP.NET Core Web API on the server, and SQLite for durable MVP state. SignalR, Redis,
presence, and rate-limit infrastructure are post-MVP hardening targets rather than
verified release dependencies.

## Development Gates

Every feature moves through the same evidence chain:

1. BDD Requirement
2. SDD Design
3. TDD Unit Test
4. Implementation
5. Integration Test
6. BDD Behavior Test
7. Review and Release

No feature is considered complete until its requirement, design, code, tests, and review
evidence are linked in `TRACEABILITY_MATRIX.md`.

## Agent Roles

- `StudioLead`: owns gates, requirement IDs, traceability, and release readiness.
- `GamePM`: writes player-visible BDD behavior and validates gameplay.
- `Dev`: implements React, Phaser, ASP.NET Core, SignalR, SQLite, Redis, and the battle engine.
- `Asset`: defines UI states, browser-friendly assets, test IDs, and reduced-motion behavior.
- `QA`: validates edge cases, integration behavior, Playwright flows, security, and reconnects.

## Asset Policy

The local source folders are:

- `C:\Users\User\Desktop\CR\Clash-Royale-assets`
- `C:\Users\User\Desktop\CR\Clash-Royale-SFX-master`

Because the GitHub repository is currently public, this repo does not check in third-party
game assets by default. The implementation can load locally imported or replaced assets from
`assets/`, and only small original placeholders or generated metadata should be committed
unless repository visibility and licensing are explicitly confirmed.

## MVP Order

1. Guest session: open the page and play as a guest.
2. Friend code: create, reuse, expire, and redeem an eight-character code.
3. Friend request: accept, reject, persist, and refresh through the Friends view.
4. Solo battle sandbox: server-authoritative towers, elixir, one deployable unit, and results.
5. Online battle: matchmaking, battle room, command sync, reconnect, and battle result.
6. Friendly battle: invite an online friend and play without trophy changes.

The friend code and friend request MVP items are tracked together under
`FEATURE-FRIEND-001`.

## Verification

Run document checks with:

```powershell
npm.cmd run test:docs
```

Run the full MVP release gate with:

```powershell
npm.cmd test
```

Current MVP release gate: `specs/review-reports/RELEASE-GATE-MVP-001.md`.
