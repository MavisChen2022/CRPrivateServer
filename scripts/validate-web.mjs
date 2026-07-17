import { existsSync, readFileSync } from "node:fs";

const requiredFiles = [
  "src/Game.Web/package.json",
  "src/Game.Web/vite.config.ts",
  "src/Game.Web/index.html",
  "src/Game.Web/src/main.tsx",
  "src/Game.Web/src/game/assetResolver.ts",
  "src/Game.Web/src/game/battlePreview.ts",
  "src/Game.Web/src/styles/app.css"
];

function fail(message) {
  console.error(`web validation failed: ${message}`);
  process.exitCode = 1;
}

for (const file of requiredFiles) {
  if (!existsSync(file)) {
    fail(`missing ${file}`);
  }
}

const main = readFileSync("src/Game.Web/src/main.tsx", "utf8");
for (const text of [
  "react",
  "createBattlePreview",
  "detectImportedBattleAssets",
  "data-testid=\"home-ready\"",
  "data-testid=\"start-battle-button\"",
  "data-testid=\"battle-ready\"",
  "data-testid=\"enemy-tower-hp\"",
  "data-testid={`deploy-card-${card.cardId}`}",
  "data-testid=\"friends-ready\"",
  "data-testid=\"friend-code\"",
  "data-testid=\"friend-code-input\"",
  "data-testid=\"send-friend-request-button\"",
  "data-testid=\"challenge-friend-button\"",
  "data-testid=\"challenge-status\"",
  "testId=\"incoming-challenge-row\"",
  "testId=\"outgoing-challenge-row\"",
  "data-testid=\"accept-challenge-button\"",
  "data-testid=\"decline-challenge-button\"",
  "data-testid=\"cancel-challenge-button\"",
  "data-testid=\"online-battle-button\"",
  "data-testid=\"online-waiting\"",
  "data-testid=\"cancel-online-button\"",
  "data-testid=\"online-ready\"",
  "data-testid=\"online-arena\"",
  "data-testid=\"online-room-id\"",
  "data-testid=\"online-room-full-id\"",
  "data-testid={`online-deploy-card-${card.cardId}`}",
  "data-testid=\"online-player-one-hp\"",
  "data-testid=\"online-player-two-hp\"",
  "data-testid=\"online-player-one-elixir\"",
  "data-testid=\"online-player-two-elixir\"",
  "data-testid=\"online-status\"",
  "data-testid=\"online-result\"",
  "testId=\"incoming-request-row\"",
  "testId=\"outgoing-request-row\"",
  "testId=\"friend-row\"",
  "data-testid=\"accept-friend-request-button\"",
  "data-testid=\"friends-status\"",
  "data-testid=\"command-placeholder\"",
  "data-testid=\"guest-warning\"",
  "/api/battles/solo",
  "/api/battles/${battle.snapshot.battleId}/tick",
  "/api/friends",
  "/api/friends/requests",
  "/api/friendly-battles/current",
  "/api/friendly-battles/invites",
  "/api/online-battles/matchmaking",
  "/api/online-battles/current",
  "/api/online-battles/${roomId}/commands",
  "/api/online-battles/${roomId}/tick",
  "prefers-reduced-motion",
  "aria-describedby=\"battle-preview-fallback\"",
  "aria-live=\"polite\""
]) {
  if (!main.includes(text)) {
    fail(`main.tsx is missing "${text}"`);
  }
}

const battle = readFileSync("src/Game.Web/src/game/battlePreview.ts", "utf8");
if (!battle.includes("Phaser")) {
  fail("battlePreview.ts must import Phaser");
}
for (const text of [
  "resolveImportedAsset",
  "this.load.image(\"arena\"",
  "this.load.audio(deploySfxKey",
  "hasImportedBattleSet"
]) {
  if (!battle.includes(text)) {
    fail(`battlePreview.ts is missing imported asset fallback support: "${text}"`);
  }
}

const resolver = readFileSync("src/Game.Web/src/game/assetResolver.ts", "utf8");
for (const text of [
  "LOCAL_IMPORTED_ASSET_ROOT",
  "/assets/imported",
  "importedAssetManifest",
  "detectImportedBattleAssets",
  "method: \"HEAD\"",
  "scenes/arena.png",
  "sfx/deploy.mp3"
]) {
  if (!resolver.includes(text)) {
    fail(`assetResolver.ts is missing "${text}"`);
  }
}

const gitignore = readFileSync(".gitignore", "utf8");
if (!gitignore.includes("assets/imported/")) {
  fail(".gitignore must keep assets/imported/ out of Git");
}

const css = readFileSync("src/Game.Web/src/styles/app.css", "utf8");
for (const text of [
  "button:focus-visible",
  ".command-placeholder",
  ".solo-arena",
  ".battle-hud",
  ".starter-deck",
  ".online-layout",
  ".online-arena",
  ".online-status-panel",
  ".waiting-panel",
  ".friends-layout",
  ".friend-code-panel",
  ".friend-row",
  ".challenge-row",
  ".sr-only",
  "@media (prefers-reduced-motion: reduce)"
]) {
  if (!css.includes(text)) {
    fail(`app.css is missing "${text}"`);
  }
}

if (process.exitCode) {
  process.exit();
}

console.log("web validation passed");
