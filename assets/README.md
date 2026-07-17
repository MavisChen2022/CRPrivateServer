# Asset Pipeline

This repository is public-safe by default. Do not commit extracted Clash Royale images,
audio, fonts, or proprietary game data here unless the repository visibility and license
permissions have both been reviewed.

## Local Imports

- Put local reference assets under `src/Game.Web/public/assets/imported/` by running
  `scripts/import-local-assets.ps1`.
- Keep `src/Game.Web/public/assets/imported/` and legacy `assets/imported/` ignored by Git.
- Frontend code should resolve imported assets first and fall back to original placeholder
  art or silence when a local image or sound is missing.
- Commit only pipeline notes, schemas, manifests for original placeholders, and generated
  assets that are safe to redistribute.

## FEATURE-SESSION-001

The current battle preview is an original placeholder drawn in Phaser. It intentionally does
not bundle proprietary Clash Royale art or sound effects into the public repo.
