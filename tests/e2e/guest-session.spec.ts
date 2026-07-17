import { expect, test } from "@playwright/test";

test.beforeEach(async ({ page }) => {
  page.on("console", (message) => {
    if (message.type() === "error") {
      console.error(`browser console error: ${message.text()}`);
    }
  });
  page.on("pageerror", (error) => {
    console.error(`browser page error: ${error.message}`);
  });
});

test("first visit creates a guest session and renders the home screen", async ({ page }) => {
  await page.goto("/", { waitUntil: "domcontentloaded" });

  await expect(page.getByTestId("home-ready")).toBeVisible();
  await expect(page.getByTestId("player-display-name")).toHaveText(/Player\d{6}/);
  await expect(page.getByTestId("player-trophies")).toHaveText("0");
  await expect(page.getByTestId("player-gold")).toHaveText("100");
  await expect(page.getByTestId("guest-warning")).toContainText("Clearing browser data");

  const cookies = await page.context().cookies();
  const sessionCookie = cookies.find((cookie) => cookie.name === "royale_session");
  expect(sessionCookie).toBeTruthy();
  expect(sessionCookie?.httpOnly).toBe(true);
  expect(sessionCookie?.sameSite).toBe("Lax");
});

test("refresh keeps the same guest identity", async ({ page }) => {
  await page.goto("/", { waitUntil: "domcontentloaded" });
  const playerName = await page.getByTestId("player-display-name").innerText();

  await page.reload({ waitUntil: "domcontentloaded" });

  await expect(page.getByTestId("home-ready")).toBeVisible();
  await expect(page.getByTestId("player-display-name")).toHaveText(playerName);
});

test("invalid cookie recovers by issuing a replacement guest session", async ({ page, context }) => {
  await context.addCookies([
    {
      name: "royale_session",
      value: "tampered-token",
      domain: "127.0.0.1",
      path: "/",
      httpOnly: true,
      sameSite: "Lax"
    }
  ]);

  await page.goto("/", { waitUntil: "domcontentloaded" });

  await expect(page.getByTestId("home-ready")).toBeVisible();
  await expect(page.getByTestId("player-display-name")).toHaveText(/Player\d{6}/);

  const cookies = await context.cookies();
  const sessionCookie = cookies.find((cookie) => cookie.name === "royale_session");
  expect(sessionCookie?.value).not.toBe("tampered-token");
});

test("command placeholders are interactive without over-promising gameplay", async ({ page }) => {
  await page.goto("/", { waitUntil: "domcontentloaded" });

  await page.getByTestId("friends-button").click();
  await expect(page.getByTestId("command-placeholder")).toContainText("Friends list empty");

  await page.getByTestId("deck-button").click();
  await expect(page.getByTestId("command-placeholder")).toContainText("Starter deck pending");

  await page.getByTestId("start-battle-button").click();
  await expect(page.getByTestId("command-placeholder")).toContainText("Battle sandbox pending");
});

test("reduced motion still renders a usable home and battle preview", async ({ page }) => {
  await page.emulateMedia({ reducedMotion: "reduce" });

  await page.goto("/", { waitUntil: "domcontentloaded" });

  await expect(page.getByTestId("home-ready")).toBeVisible();
  await expect(page.getByTestId("battle-preview")).toBeVisible();
  await expect(page.getByTestId("battle-preview").locator("canvas")).toBeVisible();
  await expect(page.getByTestId("start-battle-button")).toBeVisible();
});

test("session API failure shows an error and retry can recover", async ({ page }) => {
  let shouldFail = true;
  await page.route("**/api/session", async (route) => {
    if (shouldFail) {
      await route.fulfill({
        status: 503,
        contentType: "application/json",
        body: JSON.stringify({ title: "Session store unavailable" })
      });
      return;
    }

    await route.continue();
  });

  await page.goto("/", { waitUntil: "domcontentloaded" });
  await expect(page.getByTestId("home-error")).toBeVisible();
  await expect(page.getByTestId("home-error")).toContainText("Session service is unavailable.");

  shouldFail = false;
  await page.getByTestId("retry-button").click();

  await expect(page.getByTestId("home-ready")).toBeVisible();
  await expect(page.getByTestId("player-display-name")).toHaveText(/Player\d{6}/);
});

test("client-side player id tampering does not change server identity", async ({ page }) => {
  await page.goto("/", { waitUntil: "domcontentloaded" });
  const playerName = await page.getByTestId("player-display-name").innerText();

  await page.evaluate(() => {
    window.localStorage.setItem("playerId", "00000000-0000-0000-0000-000000000000");
    window.localStorage.setItem("displayName", "InjectedPlayer");
  });
  await page.reload({ waitUntil: "domcontentloaded" });

  await expect(page.getByTestId("home-ready")).toBeVisible();
  await expect(page.getByTestId("player-display-name")).toHaveText(playerName);
});
