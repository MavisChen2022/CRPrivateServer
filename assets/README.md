# Asset Pipeline

This repository is public-safe by default. Do not commit extracted Clash Royale images,
audio, fonts, or proprietary game data here unless the repository visibility and license
permissions have both been reviewed.

## Local Imports

- Put local reference assets under `assets/imported/`.
- Keep `assets/imported/` ignored by Git.
- Frontend code should resolve imported assets first and fall back to original placeholder
  art when a local asset is missing.
- Commit only pipeline notes, schemas, manifests for original placeholders, and generated
  assets that are safe to redistribute.

## FEATURE-SESSION-001

The current battle preview is an original placeholder drawn in Phaser. It intentionally does
not bundle proprietary Clash Royale art or sound effects into the public repo.
