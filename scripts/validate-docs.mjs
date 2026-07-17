import { existsSync, readFileSync, readdirSync, statSync } from "node:fs";
import { join } from "node:path";

const requiredFiles = [
  "README.md",
  "TRACEABILITY_MATRIX.md"
];

const requiredReadmeSections = [
  "Development Gates",
  "Agent Roles",
  "Asset Policy",
  "MVP Order",
  "Verification"
];

function fail(message) {
  console.error(`docs validation failed: ${message}`);
  process.exitCode = 1;
}

for (const file of requiredFiles) {
  if (!existsSync(file)) {
    fail(`missing ${file}`);
  }
}

if (existsSync("README.md")) {
  const readme = readFileSync("README.md", "utf8");
  for (const section of requiredReadmeSections) {
    if (!readme.includes(`## ${section}`)) {
      fail(`README.md is missing section "${section}"`);
    }
  }
}

if (existsSync("TRACEABILITY_MATRIX.md")) {
  const matrix = readFileSync("TRACEABILITY_MATRIX.md", "utf8");
  for (const requirementId of ["FEATURE-SESSION-001", "FEATURE-FRIEND-001", "FEATURE-BATTLE-001"]) {
    if (!matrix.includes(requirementId)) {
      fail(`TRACEABILITY_MATRIX.md is missing ${requirementId}`);
    }
  }
}

function collectMarkdown(dir) {
  if (!existsSync(dir)) {
    return [];
  }

  const files = [];
  for (const entry of readdirSync(dir)) {
    const fullPath = join(dir, entry);
    if (statSync(fullPath).isDirectory()) {
      files.push(...collectMarkdown(fullPath));
    } else if (entry.endsWith(".md")) {
      files.push(fullPath);
    }
  }
  return files;
}

for (const file of collectMarkdown(".")) {
  const body = readFileSync(file, "utf8");
  if (!body.trimStart().startsWith("#")) {
    fail(`${file} must start with a Markdown heading`);
  }
}

if (process.exitCode) {
  process.exit();
}

console.log("docs validation passed");
