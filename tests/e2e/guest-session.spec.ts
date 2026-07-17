import { expect, test, type Browser } from "@playwright/test";
import { existsSync } from "node:fs";

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

test("starter deck command explains imported art fallback", async ({ page }) => {
  await page.goto("/", { waitUntil: "domcontentloaded" });

  await page.getByTestId("deck-button").click();
  await expect(page.getByTestId("command-placeholder")).toContainText("Starter deck loaded");
});

test("friends panel shows a stable friend code and empty list", async ({ page }) => {
  await page.goto("/", { waitUntil: "domcontentloaded" });

  await page.getByTestId("friends-button").click();

  await expect(page.getByTestId("friends-ready")).toBeVisible();
  await expect(page.getByTestId("friend-code")).toHaveText(/^[A-HJ-NP-Z2-9]{8}$/);
  await expect(page.getByTestId("friends-empty-state")).toContainText("No friends yet.");
  const friendCode = await page.getByTestId("friend-code").innerText();

  await page.reload({ waitUntil: "domcontentloaded" });
  await page.getByTestId("friends-button").click();

  await expect(page.getByTestId("friend-code")).toHaveText(friendCode);
});

test("friends panel rejects invalid and self friend codes", async ({ page }) => {
  await page.goto("/", { waitUntil: "domcontentloaded" });
  await page.getByTestId("friends-button").click();
  const ownCode = await page.getByTestId("friend-code").innerText();

  await page.getByTestId("friend-code-input").fill("BAD-CODE");
  await page.getByTestId("send-friend-request-button").click();
  await expect(page.getByTestId("friends-status")).toContainText("FriendCodeNotFound");

  await page.getByTestId("friend-code-input").fill(ownCode);
  await page.getByTestId("send-friend-request-button").click();
  await expect(page.getByTestId("friends-status")).toContainText("CannotAddSelf");
});

test("two guest players can request, accept, and persist a friendship", async ({ browser }) => {
  const playerA = await openFriendsPage(browser);
  const playerB = await openFriendsPage(browser);

  try {
    const codeB = await playerB.page.getByTestId("friend-code").innerText();

    await playerA.page.getByTestId("friend-code-input").fill(codeB.toLowerCase());
    await playerA.page.getByTestId("send-friend-request-button").click();
    await expect(playerA.page.getByTestId("outgoing-request-row")).toContainText(/Player\d{6}/);

    await playerA.page.getByTestId("friend-code-input").fill(codeB);
    await playerA.page.getByTestId("send-friend-request-button").click();
    await expect(playerA.page.getByTestId("friends-status")).toContainText("DuplicateFriend");

    await playerB.page.reload({ waitUntil: "domcontentloaded" });
    await playerB.page.getByTestId("friends-button").click();
    await expect(playerB.page.getByTestId("incoming-request-row")).toContainText(playerA.displayName);
    await playerB.page.getByTestId("accept-friend-request-button").click();
    await expect(playerB.page.getByTestId("friend-row")).toContainText(playerA.displayName);

    await playerA.page.reload({ waitUntil: "domcontentloaded" });
    await playerA.page.getByTestId("friends-button").click();
    await expect(playerA.page.getByTestId("friend-row")).toContainText(playerB.displayName);
  } finally {
    await playerA.context.close();
    await playerB.context.close();
  }
});

test("two friends can accept a friendly battle challenge and reconnect", async ({ browser }) => {
  const playerA = await openFriendsPage(browser);
  const playerB = await openFriendsPage(browser);

  try {
    const codeB = await playerB.page.getByTestId("friend-code").innerText();
    await playerA.page.getByTestId("friend-code-input").fill(codeB);
    await playerA.page.getByTestId("send-friend-request-button").click();
    await expect(playerA.page.getByTestId("outgoing-request-row")).toBeVisible();

    await playerB.page.reload({ waitUntil: "domcontentloaded" });
    await playerB.page.getByTestId("friends-button").click();
    await expect(playerB.page.getByTestId("incoming-request-row")).toContainText(playerA.displayName);
    await playerB.page.getByTestId("accept-friend-request-button").click();
    await expect(playerB.page.getByTestId("friend-row")).toContainText(playerA.displayName);

    await playerA.page.reload({ waitUntil: "domcontentloaded" });
    await playerA.page.getByTestId("friends-button").click();
    await expect(playerA.page.getByTestId("friend-row")).toContainText(playerB.displayName);
    await playerA.page.getByTestId("challenge-friend-button").click();
    await expect(playerA.page.getByTestId("outgoing-challenge-row")).toContainText(playerB.displayName);

    await playerB.page.reload({ waitUntil: "domcontentloaded" });
    await playerB.page.getByTestId("friends-button").click();
    await expect(playerB.page.getByTestId("incoming-challenge-row")).toContainText(playerA.displayName);
    await playerB.page.getByTestId("accept-challenge-button").click();
    await expect(playerB.page.getByTestId("online-ready")).toBeVisible();
    const roomB = await playerB.page.getByTestId("online-room-id").innerText();

    await playerA.page.reload({ waitUntil: "domcontentloaded" });
    await expect(playerA.page.getByTestId("online-ready")).toBeVisible();
    await expect(playerA.page.getByTestId("online-room-id")).toHaveText(roomB);
    await playerB.page.getByTestId("online-deploy-card-training-archer").click();
    await expect(playerB.page.getByTestId("online-unit")).toBeVisible();
  } finally {
    await playerA.context.close();
    await playerB.context.close();
  }
});

test("reduced motion still renders a usable home and battle preview", async ({ page }) => {
  await page.emulateMedia({ reducedMotion: "reduce" });

  await page.goto("/", { waitUntil: "domcontentloaded" });

  await expect(page.getByTestId("home-ready")).toBeVisible();
  await expect(page.getByTestId("battle-preview")).toBeVisible();
  await expect(page.getByTestId("battle-preview").locator("canvas")).toBeVisible();
  await expect(page.getByTestId("start-battle-button")).toBeVisible();
});

test("reduced motion still allows friends interaction", async ({ page }) => {
  await page.emulateMedia({ reducedMotion: "reduce" });
  await page.goto("/", { waitUntil: "domcontentloaded" });

  await page.getByTestId("friends-button").click();
  await expect(page.getByTestId("friends-ready")).toBeVisible();
  await page.getByTestId("friend-code-input").fill("ABCDEFG1");
  await page.getByTestId("send-friend-request-button").click();

  await expect(page.getByTestId("friends-status")).toContainText("FriendCodeNotFound");
  await expect(page.getByTestId("friends-empty-state")).toBeVisible();
});

test("online matchmaking can be cancelled before a match", async ({ page }) => {
  await page.goto("/", { waitUntil: "domcontentloaded" });

  await page.getByTestId("online-battle-button").click();

  await expect(page.getByTestId("online-waiting")).toBeVisible();
  await expect(page.getByTestId("online-status")).toContainText("Waiting");
  await page.getByTestId("cancel-online-button").click();

  await expect(page.getByTestId("online-status")).toContainText("cancelled");
  await expect(page.getByTestId("online-queue-button")).toBeVisible();
});

test("two guest players can match, deploy, and restore an online room", async ({ browser }) => {
  const playerA = await openOnlinePage(browser);
  const playerB = await openOnlinePage(browser);
  const spectator = await browser.newContext();

  try {
    await expect(playerA.page.getByTestId("online-ready")).toBeVisible({ timeout: 10000 });
    await expect(playerB.page.getByTestId("online-ready")).toBeVisible({ timeout: 10000 });

    const roomA = await playerA.page.getByTestId("online-room-id").innerText();
    const roomB = await playerB.page.getByTestId("online-room-id").innerText();
    const fullRoomId = await playerA.page.getByTestId("online-room-full-id").innerText();
    expect(roomA).toBe(roomB);

    const spectatorPage = await spectator.newPage();
    await spectatorPage.goto("http://127.0.0.1:5173/", { waitUntil: "domcontentloaded" });
    await expect(spectatorPage.getByTestId("home-ready")).toBeVisible();
    const forbidden = await spectatorPage.request.get(`http://127.0.0.1:5173/api/online-battles/${fullRoomId}`);
    expect(forbidden.status()).toBe(403);

    await playerA.page.getByTestId("online-deploy-card-training-knight").click();
    await expect(playerA.page.getByTestId("online-unit")).toBeVisible();
    await expect.poll(async () => Number(await playerB.page.getByTestId("online-player-one-hp").innerText()), {
      timeout: 8000
    }).toBeLessThan(1000);

    await playerA.page.reload({ waitUntil: "domcontentloaded" });
    await expect(playerA.page.getByTestId("online-ready")).toBeVisible();
    await expect(playerA.page.getByTestId("online-room-id")).toHaveText(roomA);
  } finally {
    await playerA.context.close();
    await playerB.context.close();
    await spectator.close();
  }
});

test("reduced motion still allows online battle interaction", async ({ browser }) => {
  const playerA = await openOnlinePage(browser, true);
  const playerB = await openOnlinePage(browser, true);

  try {
    await expect(playerA.page.getByTestId("online-ready")).toBeVisible({ timeout: 10000 });
    await expect(playerB.page.getByTestId("online-ready")).toBeVisible({ timeout: 10000 });
    await playerB.page.getByTestId("online-deploy-card-training-archer").click();

    await expect(playerB.page.getByTestId("online-unit")).toBeVisible();
    await expect(playerB.page.getByTestId("online-status")).toContainText(/active|deployed/i);
  } finally {
    await playerA.context.close();
    await playerB.context.close();
  }
});

test("start battle opens a playable solo sandbox", async ({ page }) => {
  await page.goto("/", { waitUntil: "domcontentloaded" });

  await page.getByTestId("start-battle-button").click();

  await expect(page.getByTestId("battle-ready")).toBeVisible();
  await expect(page.getByTestId("battle-arena")).toBeVisible();
  await expect(page.getByTestId("enemy-tower-hp")).toHaveText("1000");
  await expect(page.getByTestId("battle-elixir")).toContainText("5/10");
  await expect(page.getByTestId("deploy-card-training-knight")).toBeVisible();
  await expect(page.getByTestId("battle-status")).toContainText("Battle active");
});

test("local imported card art appears when available", async ({ page }) => {
  test.skip(
    !existsSync("src/Game.Web/public/assets/imported/cards/training-knight.png"),
    "Local imported Clash Royale assets are optional and ignored by Git."
  );

  await page.goto("/", { waitUntil: "domcontentloaded" });
  await page.getByTestId("start-battle-button").click();

  await expect(page.getByTestId("battle-ready")).toBeVisible();
  const cardImage = page.getByTestId("deploy-card-training-knight").locator("img");
  await expect(cardImage).toBeVisible();
  await expect.poll(() => cardImage.evaluate((image) => (image as HTMLImageElement).naturalWidth)).toBeGreaterThan(0);

  await page.getByTestId("deploy-card-training-knight").click();
  const unitImage = page.getByTestId("battle-unit").locator("img");
  await expect(unitImage).toBeVisible();
  await expect.poll(() => unitImage.evaluate((image) => (image as HTMLImageElement).naturalWidth)).toBeGreaterThan(0);
});

test("deploying a unit consumes elixir and damages the enemy tower", async ({ page }) => {
  await page.goto("/", { waitUntil: "domcontentloaded" });
  await page.getByTestId("start-battle-button").click();
  await expect(page.getByTestId("battle-ready")).toBeVisible();

  await page.getByTestId("deploy-card-training-knight").click();

  await expect(page.getByTestId("battle-elixir")).toContainText("2/10");
  await expect(page.getByTestId("battle-unit")).toBeVisible();
  await expect.poll(async () => Number(await page.getByTestId("enemy-tower-hp").innerText()), {
    timeout: 8000
  }).toBeLessThan(1000);
});

test("battle snapshot survives refresh", async ({ page }) => {
  await page.goto("/", { waitUntil: "domcontentloaded" });
  await page.getByTestId("start-battle-button").click();
  await expect(page.getByTestId("battle-ready")).toBeVisible();
  const battleId = await page.getByTestId("battle-id").innerText();

  await page.getByTestId("deploy-card-training-archer").click();
  await expect(page.getByTestId("battle-unit")).toBeVisible();
  await page.reload({ waitUntil: "domcontentloaded" });

  await expect(page.getByTestId("battle-ready")).toBeVisible();
  await expect(page.getByTestId("battle-id")).toHaveText(battleId);
  await expect(page.getByTestId("battle-unit")).toBeVisible();
});

test("reduced motion still allows battle interaction", async ({ page }) => {
  await page.emulateMedia({ reducedMotion: "reduce" });
  await page.goto("/", { waitUntil: "domcontentloaded" });

  await page.getByTestId("start-battle-button").click();
  await expect(page.getByTestId("battle-ready")).toBeVisible();
  await page.getByTestId("deploy-card-training-knight").click();

  await expect(page.getByTestId("battle-unit")).toBeVisible();
  await expect(page.getByTestId("battle-status")).toContainText("Battle active");
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

async function openFriendsPage(browser: Browser) {
  const context = await browser.newContext();
  const page = await context.newPage();
  await page.goto("http://127.0.0.1:5173/", { waitUntil: "domcontentloaded" });
  await expect(page.getByTestId("home-ready")).toBeVisible();
  const displayName = await page.getByTestId("player-display-name").innerText();
  await page.getByTestId("friends-button").click();
  await expect(page.getByTestId("friends-ready")).toBeVisible();
  return { context, page, displayName };
}

async function openOnlinePage(browser: Browser, reducedMotion = false) {
  const context = await browser.newContext();
  const page = await context.newPage();
  if (reducedMotion) {
    await page.emulateMedia({ reducedMotion: "reduce" });
  }

  await page.goto("http://127.0.0.1:5173/", { waitUntil: "domcontentloaded" });
  await expect(page.getByTestId("home-ready")).toBeVisible();
  const displayName = await page.getByTestId("player-display-name").innerText();
  await page.getByTestId("online-battle-button").click();
  await expect(page.getByTestId("online-waiting").or(page.getByTestId("online-ready"))).toBeVisible();
  return { context, page, displayName };
}
