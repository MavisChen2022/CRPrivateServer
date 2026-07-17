import React, { useEffect, useMemo, useState } from "react";
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

function App() {
  const [session, setSession] = useState<SessionState>({ status: "loading" });
  const reducedMotion = useMemo(
    () => window.matchMedia("(prefers-reduced-motion: reduce)").matches,
    []
  );

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
        <div id="battle-preview" className="battle-preview" data-testid="battle-preview" />
        <nav className="command-panel" aria-label="Game commands">
          <button type="button" data-testid="start-battle-button">Start Battle</button>
          <button type="button" data-testid="friends-button">Friends</button>
          <button type="button" data-testid="deck-button">Deck</button>
        </nav>
      </section>

      <p className="guest-warning" data-testid="guest-warning">
        {session.player.guestWarning}
      </p>
    </main>
  );
}

createRoot(document.getElementById("root")!).render(<App />);

