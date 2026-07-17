import { spawn } from "node:child_process";
import { createConnection } from "node:net";

const isWindows = process.platform === "win32";
const playwrightBin = isWindows ? "cmd.exe" : "node_modules/.bin/playwright";

const children = [];

function start(name, command, args, env = {}) {
  const child = spawn(command, args, {
    cwd: process.cwd(),
    env: { ...process.env, ...env },
    stdio: ["ignore", "pipe", "pipe"]
  });

  child.stdout.on("data", (chunk) => process.stdout.write(`[${name}] ${chunk}`));
  child.stderr.on("data", (chunk) => process.stderr.write(`[${name}] ${chunk}`));
  children.push(child);
  return child;
}

async function waitForUrl(url, timeoutMs = 60_000) {
  const startedAt = Date.now();
  while (Date.now() - startedAt < timeoutMs) {
    try {
      const response = await fetch(url);
      if (response.ok) {
        return;
      }
    } catch {
      // Keep polling until the server is ready or the timeout expires.
    }
    await new Promise((resolve) => setTimeout(resolve, 500));
  }

  throw new Error(`Timed out waiting for ${url}`);
}

async function waitForPort(host, port, timeoutMs = 60_000) {
  const startedAt = Date.now();
  while (Date.now() - startedAt < timeoutMs) {
    const connected = await new Promise((resolve) => {
      const socket = createConnection({ host, port }, () => {
        socket.end();
        resolve(true);
      });
      socket.on("error", () => resolve(false));
      socket.setTimeout(1000, () => {
        socket.destroy();
        resolve(false);
      });
    });

    if (connected) {
      return;
    }

    await new Promise((resolve) => setTimeout(resolve, 500));
  }

  throw new Error(`Timed out waiting for ${host}:${port}`);
}

function stopChildren() {
  for (const child of children.reverse()) {
    if (!child.killed) {
      child.kill();
    }
  }
}

async function run() {
  start(
    "api",
    "dotnet",
    ["run", "--project", "src/Game.Api", "--launch-profile", "http"],
    {
      ASPNETCORE_ENVIRONMENT: "Development",
      ConnectionStrings__GameDatabase: "Data Source=test-data/e2e-playwright.db"
    }
  );

  start(
    "web",
    "node",
    ["src/Game.Web/node_modules/vite/bin/vite.js", "src/Game.Web", "--host", "127.0.0.1", "--port", "5173"]
  );

  await waitForUrl("http://127.0.0.1:5202/api/health");
  await waitForPort("127.0.0.1", 5173);

  const args = isWindows
    ? ["/c", "node_modules\\.bin\\playwright.cmd", "test", ...process.argv.slice(2)]
    : ["test", ...process.argv.slice(2)];
  const testRun = spawn(playwrightBin, args, {
    cwd: process.cwd(),
    env: { ...process.env, CR_SKIP_WEBSERVER: "1" },
    stdio: "inherit"
  });

  const exitCode = await new Promise((resolve) => {
    testRun.on("exit", (code) => resolve(code ?? 1));
  });

  stopChildren();
  process.exit(exitCode);
}

process.on("exit", stopChildren);
process.on("SIGINT", () => {
  stopChildren();
  process.exit(130);
});
process.on("SIGTERM", () => {
  stopChildren();
  process.exit(143);
});

run().catch((error) => {
  console.error(error);
  stopChildren();
  process.exit(1);
});
