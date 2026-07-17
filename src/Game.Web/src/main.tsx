import React, { useEffect, useState } from "react";
import { createRoot } from "react-dom/client";
import { createBattlePreview } from "./game/battlePreview";
import "./styles/app.css";

type SessionState =
  | { status: "loading" }
  | { status: "ready"; player: PlayerSession }
  | { status: "error"; message: string };

type PlayerSession = {
  playerId: string;
  displayName: string;
  trophies: number;
  gold: number;
  accountType: "Guest" | "Linked";
  guestWarning: string;
};

type CommandKey = "battle" | "friends" | "deck";

const commandCopy: Record<CommandKey, { title: string; body: string }> = {
  battle: {
    title: "Battle sandbox pending",
    body: "Start Battle is visible for the session MVP, but matchmaking and solo sandbox behavior are not enabled yet."
  },
  friends: {
    title: "Friends list empty",
    body: "Friend discovery will unlock after account persistence and social APIs are verified."
  },
  deck: {
    title: "Starter deck pending",
    body: "Deck editing is reserved for the next gameplay slice after card data and asset loading are wired safely."
  }
};

function usePrefersReducedMotion() {
  const [reducedMotion, setReducedMotion] = useState(() =>
    window.matchMedia("(prefers-reduced-motion: reduce)").matches
  );

  useEffect(() => {
    const mediaQuery = window.matchMedia("(prefers-reduced-motion: reduce)");
    const handleChange = (event: MediaQueryListEvent) => {
      setReducedMotion(event.matches);
    };

    setReducedMotion(mediaQuery.matches);
    mediaQuery.addEventListener("change", handleChange);
    return () => mediaQuery.removeEventListener("change", handleChange);
  }, []);

  return reducedMotion;
}

function App() {
  const [session, setSession] = useState<SessionState>({ status: "loading" });
  const [activeCommand, setActiveCommand] = useState<CommandKey>("battle");
  const reducedMotion = usePrefersReducedMotion();

  useEffect(() => {
    let disposed = false;
    fetch("/api/session", { credentials: "include" })
      .then((response) => {
        if (!response.ok) {
          throw new Error("Session service is unavailable.");
        }
        return response.json() as Promise<PlayerSession>;
      })
      .then((player) => {
        if (!disposed) {
          setSession({ status: "ready", player });
        }
      })
      .catch((error: Error) => {
        if (!disposed) {
          setSession({ status: "error", message: error.message });
        }
      });

    return () => {
      disposed = true;
    };
  }, []);

  useEffect(() => {
    if (session.status !== "ready") {
      return;
    }

    return createBattlePreview("battle-preview", { reducedMotion });
  }, [reducedMotion, session.status]);

  if (session.status === "loading") {
    return (
      <main className="shell" data-testid="home-loading">
        <section className="status-panel">Loading arena...</section>
      </main>
    );
  }

  if (session.status === "error") {
    return (
      <main className="shell" data-testid="home-error">
        <section className="status-panel">
          <h1>Connection problem</h1>
          <p>{session.message}</p>
          <button type="button" onClick={() => window.location.reload()}>
            Retry
          </button>
        </section>
      </main>
    );
  }

  return (
    <main className="shell" data-testid="home-ready">
      <header className="topbar">
        <div>
          <p className="eyebrow">Guest account</p>
          <h1 data-testid="player-display-name">{session.player.displayName}</h1>
        </div>
        <dl className="resource-strip" aria-label="Player resources">
          <div>
            <dt>Trophies</dt>
            <dd data-testid="player-trophies">{session.player.trophies}</dd>
          </div>
          <div>
            <dt>Gold</dt>
            <dd data-testid="player-gold">{session.player.gold}</dd>
          </div>
        </dl>
      </header>

      <section className="arena-layout">
        <div
          id="battle-preview"
          className="battle-preview"
          data-testid="battle-preview"
          role="img"
          aria-label="Preview of a two lane royale arena with player and CPU towers"
          aria-describedby="battle-preview-fallback"
        >
          <p id="battle-preview-fallback" className="sr-only">
            Static arena preview. Animations are disabled when reduced motion is enabled.
          </p>
        </div>
        <nav className="command-panel" aria-label="Game commands">
          <button
            type="button"
            data-testid="start-battle-button"
            aria-pressed={activeCommand === "battle"}
            onClick={() => setActiveCommand("battle")}
          >
            Start Battle
          </button>
          <button
            type="button"
            data-testid="friends-button"
            aria-pressed={activeCommand === "friends"}
            onClick={() => setActiveCommand("friends")}
          >
            Friends
          </button>
          <button
            type="button"
            data-testid="deck-button"
            aria-pressed={activeCommand === "deck"}
            onClick={() => setActiveCommand("deck")}
          >
            Deck
          </button>
          <section
            className="command-placeholder"
            data-testid="command-placeholder"
            aria-live="polite"
          >
            <h2>{commandCopy[activeCommand].title}</h2>
            <p>{commandCopy[activeCommand].body}</p>
          </section>
        </nav>
      </section>

      <p className="guest-warning" data-testid="guest-warning">
        {session.player.guestWarning}
      </p>
    </main>
  );
}

createRoot(document.getElementById("root")!).render(<App />);
