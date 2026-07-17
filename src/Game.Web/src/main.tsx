import React, { useEffect, useState } from "react";
import { createRoot } from "react-dom/client";
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
    title: "Solo sandbox ready",
    body: "Start Battle opens a server-run training fight with placeholder cards and no matchmaking promise."
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

type BattleCard = {
  cardId: string;
  name: string;
  elixirCost: number;
};

type BattleUnit = {
  unitId: string;
  cardId: string;
  name: string;
  lane: "left" | "center" | "right";
  position: number;
  damagePerTick: number;
};

type BattleSnapshot = {
  battleId: string;
  playerId: string;
  status: "Active" | "Ended";
  result: "None" | "Win" | "Timeout";
  tick: number;
  durationTicks: number;
  playerTowerHp: number;
  cpuTowerHp: number;
  elixir: number;
  maxElixir: number;
  starterDeck: BattleCard[];
  units: BattleUnit[];
};

type BattleState =
  | { status: "idle" }
  | { status: "loading" }
  | { status: "ready"; snapshot: BattleSnapshot; message?: string }
  | { status: "error"; message: string };

type ViewState = "home" | "battle";

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
  const [view, setView] = useState<ViewState>("home");
  const [battle, setBattle] = useState<BattleState>({ status: "idle" });
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

    const battleId = window.localStorage.getItem("activeBattleId");
    if (!battleId) {
      return;
    }

    let disposed = false;
    fetchBattle(battleId)
      .then((snapshot) => {
        if (!disposed && snapshot.status === "Active") {
          setBattle({ status: "ready", snapshot });
          setView("battle");
        }
      })
      .catch(() => {
        window.localStorage.removeItem("activeBattleId");
      });

    return () => {
      disposed = true;
    };
  }, [session.status]);

  useEffect(() => {
    if (session.status !== "ready") {
      return;
    }

    let disposePreview: (() => void) | undefined;
    let disposed = false;

    Promise.all([
      import("./game/battlePreview"),
      import("./game/assetResolver")
    ]).then(async ([{ createBattlePreview }, { detectImportedBattleAssets }]) => {
      const useImportedAssets = await detectImportedBattleAssets();
      if (!disposed) {
        disposePreview = createBattlePreview("battle-preview", {
          reducedMotion,
          useImportedAssets
        });
      }
    });

    return () => {
      disposed = true;
      disposePreview?.();
    };
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
          <button type="button" data-testid="retry-button" onClick={() => window.location.reload()}>
            Retry
          </button>
        </section>
      </main>
    );
  }

  const startBattle = async () => {
    setBattle({ status: "loading" });
    setView("battle");
    try {
      const snapshot = await postBattle("/api/battles/solo", undefined);
      window.localStorage.setItem("activeBattleId", snapshot.battleId);
      setBattle({ status: "ready", snapshot });
    } catch (error) {
      setBattle({
        status: "error",
        message: error instanceof Error ? error.message : "Battle service is unavailable."
      });
    }
  };

  if (view === "battle") {
    return (
      <BattleScreen
        battle={battle}
        reducedMotion={reducedMotion}
        onBackHome={() => setView("home")}
        onReplay={startBattle}
        onSnapshot={setBattle}
      />
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
            onClick={startBattle}
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

function BattleScreen({
  battle,
  reducedMotion,
  onBackHome,
  onReplay,
  onSnapshot
}: {
  battle: BattleState;
  reducedMotion: boolean;
  onBackHome: () => void;
  onReplay: () => void;
  onSnapshot: (state: BattleState) => void;
}) {
  useEffect(() => {
    if (battle.status !== "ready" || battle.snapshot.status !== "Active") {
      return;
    }

    const interval = window.setInterval(async () => {
      try {
        const snapshot = await postBattle(`/api/battles/${battle.snapshot.battleId}/tick`, { ticks: 2 });
        onSnapshot({ status: "ready", snapshot });
        if (snapshot.status === "Ended") {
          window.localStorage.removeItem("activeBattleId");
        }
      } catch {
        onSnapshot({ status: "error", message: "Battle tick failed. Retry from the arena controls." });
      }
    }, reducedMotion ? 1200 : 700);

    return () => window.clearInterval(interval);
  }, [battle, onSnapshot, reducedMotion]);

  if (battle.status === "loading" || battle.status === "idle") {
    return (
      <main className="shell battle-shell" data-testid="battle-loading">
        <section className="status-panel">Preparing solo arena...</section>
      </main>
    );
  }

  if (battle.status === "error") {
    return (
      <main className="shell battle-shell" data-testid="battle-error">
        <section className="status-panel">
          <h1>Battle connection problem</h1>
          <p>{battle.message}</p>
          <button type="button" data-testid="battle-retry-button" onClick={onReplay}>
            Retry
          </button>
          <button type="button" className="secondary-button" onClick={onBackHome}>
            Back Home
          </button>
        </section>
      </main>
    );
  }

  const snapshot = battle.snapshot;
  const deploy = async (cardId: string) => {
    try {
      const next = await postBattle(`/api/battles/${snapshot.battleId}/commands`, {
        cardId,
        lane: "center",
        x: 0.5,
        y: 0.72
      });
      onSnapshot({ status: "ready", snapshot: next });
    } catch (error) {
      onSnapshot({
        status: "ready",
        snapshot,
        message: error instanceof Error ? error.message : "Deploy rejected."
      });
    }
  };

  return (
    <main className="shell battle-shell" data-testid="battle-ready">
      <header className="battle-topbar">
        <div>
          <p className="eyebrow">Solo sandbox</p>
          <h1>Training Battle</h1>
        </div>
        <button type="button" className="secondary-button" data-testid="back-home-button" onClick={onBackHome}>
          Back Home
        </button>
      </header>

      <section className="battle-hud" aria-label="Battle state">
        <div>
          <span>Enemy Tower</span>
          <strong data-testid="enemy-tower-hp">{snapshot.cpuTowerHp}</strong>
        </div>
        <div>
          <span>Elixir</span>
          <strong data-testid="battle-elixir">{snapshot.elixir}/{snapshot.maxElixir}</strong>
        </div>
        <div>
          <span>Timer</span>
          <strong data-testid="battle-timer">{Math.max(0, snapshot.durationTicks - snapshot.tick)}</strong>
        </div>
      </section>

      <section className="battle-board-layout">
        <div
          className="solo-arena"
          data-testid="battle-arena"
          role="img"
          aria-label="Server snapshot of the solo sandbox arena"
        >
          <div className="tower cpu-tower">CPU {snapshot.cpuTowerHp}</div>
          <div className="river" />
          <div className="tower player-tower">You {snapshot.playerTowerHp}</div>
          {snapshot.units.map((unit) => (
            <div
              key={unit.unitId}
              className={`battle-unit lane-${unit.lane}`}
              style={{ top: `${Math.max(10, Math.min(86, unit.position / 10))}%` }}
              data-testid="battle-unit"
            >
              {unit.name.replace("Training ", "")}
            </div>
          ))}
        </div>

        <aside className="battle-controls">
          <div className="starter-deck" aria-label="Starter deck">
            {snapshot.starterDeck.map((card) => (
              <button
                type="button"
                key={card.cardId}
                data-testid={`deploy-card-${card.cardId}`}
                disabled={snapshot.status === "Ended" || snapshot.elixir < card.elixirCost}
                onClick={() => deploy(card.cardId)}
              >
                <span>{card.name}</span>
                <small>{card.elixirCost} elixir</small>
              </button>
            ))}
          </div>
          <div className="battle-status" data-testid="battle-status" aria-live="polite">
            <p className="sr-only" data-testid="battle-id">
              {snapshot.battleId}
            </p>
            {snapshot.status === "Ended" ? (
              <>
                <h2 data-testid="battle-result">{snapshot.result === "Win" ? "Victory" : "Timeout"}</h2>
                <button type="button" data-testid="replay-button" onClick={onReplay}>
                  Replay
                </button>
              </>
            ) : (
              <>
                <h2>Battle active</h2>
                <p>{battle.message ?? "Deploy starter cards on your side. The server advances the fight."}</p>
              </>
            )}
          </div>
        </aside>
      </section>
    </main>
  );
}

async function fetchBattle(battleId: string) {
  const response = await fetch(`/api/battles/${battleId}`, { credentials: "include" });
  if (!response.ok) {
    throw new Error(await readProblemCode(response, "Battle load failed."));
  }

  return response.json() as Promise<BattleSnapshot>;
}

async function postBattle(path: string, body: unknown) {
  const response = await fetch(path, {
    method: "POST",
    credentials: "include",
    headers: body === undefined ? undefined : { "Content-Type": "application/json" },
    body: body === undefined ? undefined : JSON.stringify(body)
  });

  if (!response.ok) {
    throw new Error(await readProblemCode(response, "Battle request failed."));
  }

  return response.json() as Promise<BattleSnapshot>;
}

async function readProblemCode(response: Response, fallback: string) {
  try {
    const problem = (await response.json()) as { code?: string; title?: string };
    return problem.code ?? problem.title ?? fallback;
  } catch {
    return fallback;
  }
}

createRoot(document.getElementById("root")!).render(<App />);
