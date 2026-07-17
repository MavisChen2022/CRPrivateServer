import { existsSync, mkdirSync, rmSync } from "node:fs";
import { resolve } from "node:path";

const dataDir = resolve("test-data");
const databaseFiles = [
  resolve("test-data/e2e-playwright.db"),
  resolve("test-data/e2e-playwright.db-shm"),
  resolve("test-data/e2e-playwright.db-wal"),
  resolve("src/Game.Api/test-data/e2e-playwright.db"),
  resolve("src/Game.Api/test-data/e2e-playwright.db-shm"),
  resolve("src/Game.Api/test-data/e2e-playwright.db-wal")
];

if (!existsSync(dataDir)) {
  mkdirSync(dataDir);
}

for (const file of databaseFiles) {
  rmSync(file, { force: true });
}

console.log("e2e database prepared");
