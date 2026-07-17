import { existsSync, readFileSync } from "node:fs";

const requiredFiles = [
  "src/Game.Web/package.json",
  "src/Game.Web/index.html",
  "src/Game.Web/src/main.tsx",
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
  "data-testid=\"home-ready\"",
  "data-testid=\"start-battle-button\"",
  "data-testid=\"guest-warning\"",
  "prefers-reduced-motion"
]) {
  if (!main.includes(text)) {
    fail(`main.tsx is missing "${text}"`);
  }
}

const battle = readFileSync("src/Game.Web/src/game/battlePreview.ts", "utf8");
if (!battle.includes("Phaser")) {
  fail("battlePreview.ts must import Phaser");
}

if (process.exitCode) {
  process.exit();
}

console.log("web validation passed");

