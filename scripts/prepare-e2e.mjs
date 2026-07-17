import { existsSync, mkdirSync, rmSync } from "node:fs";

const dataDir = "test-data";
const databaseFiles = [
  "test-data/e2e-playwright.db",
  "test-data/e2e-playwright.db-shm",
  "test-data/e2e-playwright.db-wal"
];

if (!existsSync(dataDir)) {
  mkdirSync(dataDir);
}

for (const file of databaseFiles) {
  rmSync(file, { force: true });
}

console.log("e2e database prepared");
