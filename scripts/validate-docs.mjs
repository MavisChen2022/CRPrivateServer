import { existsSync, readFileSync, readdirSync, statSync } from "node:fs";
import { join } from "node:path";

const requiredFiles = [
  "README.md",
  "TRACEABILITY_MATRIX.md",
  "specs/requirements/FEATURE-SESSION-001.md",
  "specs/features/guest-session.feature",
  "specs/sdd/SDD-FEATURE-SESSION-001.md",
  "specs/test-plans/UNIT-FEATURE-SESSION-001.md",
  "specs/test-plans/QA-GATE-FEATURE-SESSION-001.md",
  "specs/review-reports/AGENT-REVIEW-FEATURE-SESSION-001.md"
  ,
  "specs/requirements/FEATURE-BATTLE-001.md",
  "specs/features/battle-sandbox.feature",
  "specs/sdd/SDD-FEATURE-BATTLE-001.md",
  "specs/test-plans/UNIT-FEATURE-BATTLE-001.md",
  "specs/test-plans/QA-GATE-FEATURE-BATTLE-001.md",
  "specs/review-reports/AGENT-REVIEW-FEATURE-BATTLE-001.md",
  "specs/requirements/FEATURE-FRIEND-001.md",
  "specs/features/friend-code.feature",
  "specs/sdd/SDD-FEATURE-FRIEND-001.md",
  "specs/test-plans/UNIT-FEATURE-FRIEND-001.md",
  "specs/test-plans/QA-GATE-FEATURE-FRIEND-001.md",
  "specs/review-reports/AGENT-REVIEW-FEATURE-FRIEND-001.md",
  "specs/requirements/FEATURE-ONLINE-001.md",
  "specs/features/online-battle.feature",
  "specs/sdd/SDD-FEATURE-ONLINE-001.md",
  "specs/test-plans/UNIT-FEATURE-ONLINE-001.md",
  "specs/test-plans/QA-GATE-FEATURE-ONLINE-001.md",
  "specs/review-reports/AGENT-REVIEW-FEATURE-ONLINE-001.md",
  "specs/requirements/FEATURE-FRIENDLY-BATTLE-001.md",
  "specs/features/friendly-battle.feature",
  "specs/sdd/SDD-FEATURE-FRIENDLY-BATTLE-001.md",
  "specs/test-plans/UNIT-FEATURE-FRIENDLY-BATTLE-001.md",
  "specs/test-plans/QA-GATE-FEATURE-FRIENDLY-BATTLE-001.md",
  "specs/review-reports/AGENT-REVIEW-FEATURE-FRIENDLY-BATTLE-001.md"
];

const requiredReadmeSections = [
  "Development Gates",
  "Agent Roles",
  "Asset Policy",
  "MVP Order",
  "Verification"
];
const allowedStatuses = [
  "DRAFT",
  "IN_REVIEW",
  "APPROVED",
  "IMPLEMENTED",
  "VERIFIED",
  "RELEASED",
  "CHANGES_REQUESTED",
  "BLOCKED"
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
  for (const requirementId of [
    "FEATURE-SESSION-001",
    "FEATURE-FRIEND-001",
    "FEATURE-BATTLE-001",
    "FEATURE-ONLINE-001",
    "FEATURE-FRIENDLY-BATTLE-001"
  ]) {
    if (!matrix.includes(requirementId)) {
      fail(`TRACEABILITY_MATRIX.md is missing ${requirementId}`);
    }
  }
}

const requirementChecks = [
  "## Status",
  "## Owner",
  "## Reviewers",
  "## Acceptance Criteria",
  "## Scenarios",
  "## Test Evidence"
];

if (existsSync("specs/requirements/FEATURE-SESSION-001.md")) {
  const requirement = readFileSync("specs/requirements/FEATURE-SESSION-001.md", "utf8");
  for (const text of requirementChecks) {
    if (!requirement.includes(text)) {
      fail(`FEATURE-SESSION-001.md is missing "${text}"`);
    }
  }
  if (!allowedStatuses.some((status) => requirement.includes(`\n${status}\n`))) {
    fail("FEATURE-SESSION-001.md must include a valid status");
  }
}

if (existsSync("specs/features/guest-session.feature")) {
  const feature = readFileSync("specs/features/guest-session.feature", "utf8");
  for (const text of ["@FEATURE-SESSION-001", "Feature:", "Scenario:", "Then"]) {
    if (!feature.includes(text)) {
      fail(`guest-session.feature is missing "${text}"`);
    }
  }
}

const sddSections = [
  "## Status",
  "## Requirement",
  "## 1. Purpose",
  "## 2. User Flow",
  "## 3. Business Rules",
  "## 4. System Components",
  "## 5. API Contract",
  "## 6. SQLite Data Design",
  "## 7. Redis Cache Key Design",
  "## 8. SignalR Contract",
  "## 9. State Flow",
  "## 10. Error Handling",
  "## 11. Security and Anti-Cheat",
  "## 12. Transactions, Concurrency, and Idempotency",
  "## 13. Monitoring and Logs",
  "## 14. Unit Test Cases",
  "## 15. Integration Test Cases",
  "## 16. Behavior Test Cases",
  "## 17. Rollback and Migration",
  "## Review Evidence"
];

if (existsSync("specs/sdd/SDD-FEATURE-SESSION-001.md")) {
  const sdd = readFileSync("specs/sdd/SDD-FEATURE-SESSION-001.md", "utf8");
  for (const section of sddSections) {
    if (!sdd.includes(section)) {
      fail(`SDD-FEATURE-SESSION-001.md is missing "${section}"`);
    }
  }
}

if (existsSync("specs/test-plans/UNIT-FEATURE-SESSION-001.md")) {
  const plan = readFileSync("specs/test-plans/UNIT-FEATURE-SESSION-001.md", "utf8");
  for (const text of ["## Status", "## Test Cases", "## Execution", "## Evidence"]) {
    if (!plan.includes(text)) {
      fail(`UNIT-FEATURE-SESSION-001.md is missing "${text}"`);
    }
  }
  if (!allowedStatuses.some((status) => plan.includes(`\n${status}\n`))) {
    fail("UNIT-FEATURE-SESSION-001.md must include a valid status");
  }
}

if (existsSync("specs/test-plans/QA-GATE-FEATURE-SESSION-001.md")) {
  const plan = readFileSync("specs/test-plans/QA-GATE-FEATURE-SESSION-001.md", "utf8");
  for (const text of [
    "## Status",
    "## Required Test Suites",
    "## xUnit Unit Tests",
    "## API Integration Tests",
    "## Playwright Behavior Tests",
    "## Cookie and Security Cases",
    "## Evidence Criteria",
    "## Current Gate Result"
  ]) {
    if (!plan.includes(text)) {
      fail(`QA-GATE-FEATURE-SESSION-001.md is missing "${text}"`);
    }
  }
  if (!allowedStatuses.some((status) => plan.includes(`\n${status}\n`))) {
    fail("QA-GATE-FEATURE-SESSION-001.md must include a valid status");
  }
}

if (existsSync("specs/review-reports/AGENT-REVIEW-FEATURE-SESSION-001.md")) {
  const report = readFileSync("specs/review-reports/AGENT-REVIEW-FEATURE-SESSION-001.md", "utf8");
  for (const text of ["## GamePM Review", "## Dev Review", "## Asset Review", "## QA Review", "## Required Corrections"]) {
    if (!report.includes(text)) {
      fail(`AGENT-REVIEW-FEATURE-SESSION-001.md is missing "${text}"`);
    }
  }
}

const featureDocuments = {
  "FEATURE-BATTLE-001": "battle-sandbox.feature",
  "FEATURE-FRIEND-001": "friend-code.feature",
  "FEATURE-ONLINE-001": "online-battle.feature",
  "FEATURE-FRIENDLY-BATTLE-001": "friendly-battle.feature"
};

for (const [featureId, featureFile] of Object.entries(featureDocuments)) {
  const requirementPath = `specs/requirements/${featureId}.md`;
  const featurePath = `specs/features/${featureFile}`;
  const sddPath = `specs/sdd/SDD-${featureId}.md`;
  const unitPath = `specs/test-plans/UNIT-${featureId}.md`;
  const qaPath = `specs/test-plans/QA-GATE-${featureId}.md`;
  const reviewPath = `specs/review-reports/AGENT-REVIEW-${featureId}.md`;

  if (existsSync(requirementPath)) {
    const requirement = readFileSync(requirementPath, "utf8");
    for (const text of requirementChecks) {
      if (!requirement.includes(text)) {
        fail(`${requirementPath} is missing "${text}"`);
      }
    }
    if (!allowedStatuses.some((status) => requirement.includes(`\n${status}\n`))) {
      fail(`${requirementPath} must include a valid status`);
    }
  }

  if (existsSync(featurePath)) {
    const feature = readFileSync(featurePath, "utf8");
    for (const text of [`@${featureId}`, "Feature:", "Scenario:", "Then"]) {
      if (!feature.includes(text)) {
        fail(`${featurePath} is missing "${text}"`);
      }
    }
  }

  if (existsSync(sddPath)) {
    const sdd = readFileSync(sddPath, "utf8");
    for (const section of sddSections) {
      if (!sdd.includes(section)) {
        fail(`${sddPath} is missing "${section}"`);
      }
    }
  }

  if (existsSync(unitPath)) {
    const plan = readFileSync(unitPath, "utf8");
    for (const text of ["## Status", "## Test Cases", "## Execution", "## Evidence"]) {
      if (!plan.includes(text)) {
        fail(`${unitPath} is missing "${text}"`);
      }
    }
    if (!allowedStatuses.some((status) => plan.includes(`\n${status}\n`))) {
      fail(`${unitPath} must include a valid status`);
    }
  }

  if (existsSync(qaPath)) {
    const plan = readFileSync(qaPath, "utf8");
    for (const text of [
      "## Status",
      "## Required Test Suites",
      "## xUnit Unit Tests",
      "## API Integration Tests",
      "## Playwright Behavior Tests",
      "## Evidence Criteria",
      "## Current Gate Result"
    ]) {
      if (!plan.includes(text)) {
        fail(`${qaPath} is missing "${text}"`);
      }
    }
    if (!allowedStatuses.some((status) => plan.includes(`\n${status}\n`))) {
      fail(`${qaPath} must include a valid status`);
    }
  }

  if (existsSync(reviewPath)) {
    const report = readFileSync(reviewPath, "utf8");
    for (const text of ["## GamePM Review", "## Dev Review", "## Asset Review", "## QA Review", "## Required Corrections"]) {
      if (!report.includes(text)) {
        fail(`${reviewPath} is missing "${text}"`);
      }
    }
  }
}

function collectMarkdown(dir) {
  if (!existsSync(dir)) {
    return [];
  }

  const ignoredDirectories = new Set(["node_modules", "bin", "obj", "dist"]);
  const files = [];
  for (const entry of readdirSync(dir)) {
    const fullPath = join(dir, entry);
    if (statSync(fullPath).isDirectory()) {
      if (ignoredDirectories.has(entry)) {
        continue;
      }
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
