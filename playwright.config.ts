import { defineConfig, devices } from "@playwright/test";

export default defineConfig({
  testDir: "./tests/e2e",
  timeout: 30_000,
  expect: {
    timeout: 10_000
  },
  use: {
    baseURL: "http://127.0.0.1:5173",
    trace: "retain-on-failure"
  },
  webServer: process.env.CR_SKIP_WEBSERVER ? undefined : [
    {
      command: "dotnet run --project src/Game.Api --launch-profile http",
      url: "http://127.0.0.1:5202/api/health",
      reuseExistingServer: false,
      timeout: 60_000,
      env: {
        ASPNETCORE_ENVIRONMENT: "Development",
        ConnectionStrings__GameDatabase: "Data Source=test-data/e2e-playwright.db"
      }
    },
    {
      command: "npm.cmd --prefix src/Game.Web run dev -- --host 127.0.0.1 --port 5173",
      url: "http://127.0.0.1:5173",
      reuseExistingServer: false,
      timeout: 60_000
    }
  ],
  projects: [
    {
      name: "chromium",
      use: { ...devices["Desktop Chrome"] }
    },
    {
      name: "mobile-chrome",
      use: { ...devices["Pixel 5"] }
    }
  ]
});
