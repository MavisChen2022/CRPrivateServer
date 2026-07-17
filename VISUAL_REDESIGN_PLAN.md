# Visual Redesign Plan: CRPrivateServer

## Status

DRAFT - design only. Do not implement until the user approves this plan.

Formal design spec: `specs/design/VISUAL-REDESIGN-MVP-001.md`.

## Problem Statement

The current home screen looks like an engineering demo, not a browser royale battle game.
The screenshot review found these blocking visual issues:

- The left arena panel is empty, so the first viewport has no battle identity.
- The command buttons are plain rectangles and do not feel like game UI.
- The Deck state is only explanatory text instead of visible cards.
- Imported local art and SFX are technically wired, but they do not shape the lobby experience.
- The page does not communicate towers, bridge, river, elixir, cards, units, or battle readiness at first glance.

## Design Goal

Make the first screen feel like a playable private royale battle client while staying local-only for protected assets.
The page should clearly read as a fan/private server interface, not an official game or a full Clash Royale clone.

## Non-Goals

- Do not commit `Clash-Royale-assets`, `Clash-Royale-SFX-master`, imported PNG/OGG/MP3 files, fonts, or extracted proprietary data.
- Do not add new gameplay systems while doing this visual redesign.
- Do not claim official Clash Royale branding, ownership, parity, or compatibility.
- Do not make tests depend on local protected assets.

## Target First View

Desktop layout:

```text
+----------------------------------------------------------------------------+
| Player banner                                          trophies | gold      |
|----------------------------------------------------------------------------|
|                                                                            |
|  +--------------------------------------+  +-----------------------------+  |
|  |                                      |  | Big yellow Battle button     |  |
|  |        Live arena preview            |  | Online Battle / Friends      |  |
|  |                                      |  |                             |  |
|  |      red tower                       |  | Starter Deck                |  |
|  |         bridge / river               |  | [Knight] [Archers]          |  |
|  |      blue tower                      |  | [Guards] [Zap]              |  |
|  |                                      |  |                             |  |
|  +--------------------------------------+  | Local assets status          |  |
|                                            +-----------------------------+  |
|                                                                            |
| warning / guest session note                                               |
+----------------------------------------------------------------------------+
```

Mobile layout:

```text
+----------------------------------+
| Player + currencies              |
| Big Battle button                |
| Arena preview                    |
| Starter Deck 2x2                 |
| Friends / Online buttons         |
| guest warning                    |
+----------------------------------+
```

## Required Visual Changes

### 1. Home Lobby

- Replace the plain dark page with a game lobby composition.
- The arena preview must never be empty.
- The primary action should be a dominant battle button.
- Secondary actions should be smaller but still game-styled.
- Player name, trophies, and gold should feel like HUD counters, not admin cards.

### 2. Arena Preview

Use local assets when available:

- `src/Game.Web/public/assets/imported/scenes/arena.png`
- `src/Game.Web/public/assets/imported/ui/top-tower.png`
- `src/Game.Web/public/assets/imported/ui/bottom-tower.png`
- starter unit PNGs under `units/`

Fallback when local assets are missing:

- CSS/Phaser-drawn arena with lane markings, river, bridge, red tower, blue tower, and animated unit markers.
- No blank panel is allowed.

### 3. Starter Deck

Replace the text-only deck placeholder with a real visible starter deck:

- 4 cards in a 2x2 grid.
- Each card shows image, name, and elixir cost.
- Local card art first, CSS card fallback second.
- Cards should look tappable and battle-ready.

Starter mapping:

- `training-knight` -> Knight art.
- `training-archer` -> Archers art.
- `training-guard` -> Guards art.
- `training-bolt` -> Zap art.

### 4. Battle Screens

Solo, online, and friendly battle screens should share the same visual language:

- Arena background uses local arena art first.
- Towers use local tower/character art first.
- Units use local unit art first.
- Card buttons use the same deck card component as the lobby.
- Fallback remains playable and visually complete.

### 5. Audio

- Deploy and spell sounds trigger only after a user click.
- Missing audio file should silently fallback.
- Add a visible mute/sound toggle before expanding audio usage further.
- Do not autoplay battle music by default.

## Asset Workflow

Local-only import command:

```powershell
powershell -ExecutionPolicy Bypass -File scripts/import-local-assets.ps1
```

Game startup command:

```powershell
powershell -ExecutionPolicy Bypass -File scripts/start-dev.ps1
```

Expected ignored output:

```text
src/Game.Web/public/assets/imported/
  cards/
  units/
  ui/
  scenes/
  sfx/
```

Required safety checks:

```powershell
git ls-files src/Game.Web/public/assets/imported assets/imported
git check-ignore -v src/Game.Web/public/assets/imported/cards/training-knight.png
```

`git ls-files` must return empty for imported asset folders.

## Acceptance Criteria

### Visual

- First viewport immediately shows arena, towers, and starter deck.
- The left arena area is never blank.
- Deck tab no longer displays only explanatory text.
- Desktop and mobile layouts are polished and do not overlap.
- Reduced motion still renders a usable static arena.

### Functional

- Guest session, friends, online battle, friendly battle, and solo battle remain playable.
- Local images load when present.
- Missing local images do not break the app.
- SFX plays after click when present.
- Missing SFX does not break deploy actions.

### Tests

Required before implementation commit:

```powershell
npm.cmd run test:web
npm.cmd run test:web:build
npm.cmd run test:e2e -- --grep "local imported card art"
npm.cmd test
```

Add or update tests to cover:

- Arena preview has visible child content, not only an empty frame.
- Lobby deck renders four card buttons with image/fallback art.
- Imported card/unit images have `naturalWidth > 0` when local assets exist.
- No local imported asset file is tracked by Git.

## Implementation Sequence

1. Redesign `HomeScreen` layout into lobby + arena + deck.
2. Create reusable `CardArt`, `UnitArt`, and `TowerArt` visual components.
3. Make arena fallback non-empty even when Phaser/imported assets fail.
4. Update solo/online/friendly battle screens to share the visual language.
5. Add optional sound toggle and keep audio click-triggered.
6. Run visual/e2e tests in both local-assets and fallback modes.

## Open Review Questions

- Should the main lobby prioritize `Start Battle` or `Online Battle` as the largest button?
- Should the local asset status be visible to players, or only in a small development/debug note?
- Should battle music stay off by default until a sound settings panel exists?
