# VISUAL-REDESIGN-MVP-001: Royale-Like Local Asset UI

## Status

DRAFT - planning only. Do not change application code until the user approves this design.

## Why This Exists

The current UI does not look like a royale battle game. The attached screenshot shows a dark
dashboard page with an empty arena frame, plain buttons, and a text-only Deck panel. Even if local
assets are technically importable, the first viewport does not communicate arena, towers, cards,
elixir, units, or battle readiness.

This design fixes the visual direction before implementation begins.

## Product Boundary

The target is a browser-based private royale battle client with local-only imported art and SFX.
It must not claim to be official Clash Royale, and it must not commit protected assets to GitHub.

Allowed wording:

- `royale battle`
- `arena battle`
- `training battle`
- `friendly battle`
- `private server`

Disallowed wording:

- Official Clash Royale branding or ownership claims.
- Full parity claims.
- Ranked ladder, shop, clans, chat, spectators, rematch, rewards, or card upgrades unless separate
  features are approved.

## Asset Policy

Runtime URL:

```text
/assets/imported/...
```

Local ignored folder:

```text
src/Game.Web/public/assets/imported/
```

Source folders:

```text
C:\Users\User\Desktop\CR\Clash-Royale-assets
C:\Users\User\Desktop\CR\Clash-Royale-SFX-master
```

Required guardrails:

- Do not commit imported PNG, JPG, WebP, OGG, MP3, WAV, fonts, or extracted proprietary data.
- `git ls-files src/Game.Web/public/assets/imported assets/imported` must return empty.
- Missing images must fall back to original CSS/Phaser art.
- Missing audio must fall back to silence.
- Import scripts may warn about missing files but must not block app startup.

## Target Home Lobby

The first viewport should look like a game lobby, not an admin dashboard.

Desktop wireframe:

```text
+----------------------------------------------------------------------------+
| Guest player banner                              trophies 0 | gold 100      |
|----------------------------------------------------------------------------|
|                                                                            |
|  +--------------------------------------+  +-----------------------------+  |
|  | red tower                            |  |        Battle CTA           |  |
|  |                                      |  | Solo Battle / Online Battle |  |
|  |            arena preview             |  | Friends / Friendly Battle   |  |
|  |                                      |  |                             |  |
|  | bridge / river / lanes               |  | Starter Deck                |  |
|  |                                      |  | [Knight] [Archers]          |  |
|  | blue tower                           |  | [Guards] [Bolt]             |  |
|  +--------------------------------------+  +-----------------------------+  |
| Account notice                                                               |
+----------------------------------------------------------------------------+
```

Mobile wireframe:

```text
+----------------------------------+
| Player + currencies              |
| Large Battle CTA                 |
| Arena preview                    |
| Starter Deck 2x2                 |
| Friends / Online / Friendly      |
| Account notice                   |
+----------------------------------+
```

Required home elements:

- `home-player-profile`
- `home-hero-arena`
- `home-mode-solo`
- `home-mode-online`
- `home-mode-friends`
- `home-mode-friendly`
- `home-deck-preview`
- `home-asset-status` or subtle debug-only asset note

## Arena Requirements

The arena panel must never be empty.

Imported mode:

- Arena background from `scenes/arena.png`.
- Opponent tower from `ui/top-tower.png`.
- Player tower from `ui/bottom-tower.png`.
- Preview units from imported unit art.
- Add overlays so towers and UI remain readable over imported art.

Fallback mode:

- CSS/Phaser-drawn arena with:
  - red opponent side
  - blue player side
  - river
  - two bridges
  - lanes
  - top and bottom towers
  - visible unit markers

Required arena test IDs:

- `battle-arena`
- `battle-river`
- `battle-bridge-left`
- `battle-bridge-right`
- `battle-opponent-tower`
- `battle-player-tower`

## Deck Requirements

The Deck panel must not be text-only.

The starter deck should render four visible cards:

- `training-knight`
- `training-archer`
- `training-guard`
- `training-bolt`

Each card must show:

- card art or fallback art
- elixir cost badge
- card name
- disabled / insufficient elixir state when used in battle

Required deck test IDs:

- `deck-preview`
- `starter-card-training-knight`
- `starter-card-training-archer`
- `starter-card-training-guard`
- `starter-card-training-bolt`

Do not present this as a full collection, shop, upgrade, rarity, or marketplace system.

## Battle Screen Requirements

Solo, online, and friendly battles should share one visual language:

- Arena is the primary focus and is not trapped inside a dashboard-looking card.
- Hand/deck controls should look like card controls, not generic buttons.
- HP, elixir, and timer should be game HUD elements.
- Deployed units should show imported unit art when available.
- Result state should look like a battle result, not an alert box.

Required battle elements:

- `battle-elixir-bar`
- `battle-hand`
- `battle-card-training-knight`
- `battle-unit`
- `battle-result`

## Audio Requirements

MVP audio behavior:

- Deploy SFX plays only after a click/tap.
- Spell SFX plays only after a click/tap.
- Missing audio catches errors and falls back to silence.
- No battle music autoplay.
- Add a visible mute/sound toggle before enabling loops or more frequent audio.

## Mobile Requirements

- Main actions must be at least 44px tall.
- The deck remains a 2x2 grid or horizontal hand that does not overflow.
- Arena, HUD, and card hand must all be visible without incoherent overlap.
- Long player names, friend codes, room IDs, and error codes must wrap or be hidden in debug-only text.

## Reduced Motion Requirements

- Reduced motion disables arena pulses, card shakes, and Phaser tweens.
- State changes remain visible without animation.
- Battle deploy, friends, online, and friendly flows remain usable.

## QA Plan

Required before implementation commit:

```powershell
npm.cmd run test:web
npm.cmd run test:web:build
npm.cmd run test:e2e -- --grep "local imported card art"
npm.cmd test
```

Optional visual artifacts:

- Home screenshot.
- Solo battle screenshot.
- Online active room screenshot.
- Friendly battle challenge screenshot.
- Battle result screenshot.

DOM checks are required even when screenshots exist:

- Arena preview has child content and is not blank.
- Deck preview renders four visible cards.
- Imported images have `naturalWidth > 0` when local assets exist.
- Fallback cards and fallback arena are visible when imported assets are absent.
- No protected imported asset is tracked by Git.

## Blocking Conditions

- The redesign plan requires committing protected assets.
- The first viewport can still show a blank arena.
- The Deck panel remains text-only.
- Tests depend on local protected assets.
- Mobile or reduced-motion states are not covered.
- Existing gameplay flows break while changing visual layout.

## Review Notes

- GamePM: approved planning with product guardrails.
- Dev: approved planning with local-only asset and fallback guardrails.
- Asset: approved planning only; design spec required before implementation.
- QA: approved planning with visual, DOM, mobile, reduced-motion, and fallback checks.
