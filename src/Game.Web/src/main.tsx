import React, { useEffect, useState } from "react";
import { createRoot } from "react-dom/client";
import {
  deploySfxForCard,
  detectImportedBattleAssets,
  resolveOptionalCardAsset,
  resolveOptionalUnitAsset
} from "./game/assetResolver";
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

type CommandKey = "battle" | "online" | "friends" | "deck";

const commandCopy: Record<Exclude<CommandKey, "friends">, { title: string; body: string }> = {
  battle: {
    title: "Solo sandbox ready",
    body: "Start Battle opens a server-run training fight with local card art and sound when imported assets are available."
  },
  online: {
    title: "Online room ready",
    body: "Online Battle pairs two guest players into a server-run battle room with imported local arena assets when available."
  },
  deck: {
    title: "Starter deck loaded",
    body: "The starter deck uses imported local card art first and falls back safely when assets are missing."
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

type FriendSummary = {
  playerId: string;
  displayName: string;
  trophies: number;
  shortPlayerId: string;
  status: "Friend";
};

type FriendRequestSummary = {
  friendshipId: string;
  playerId: string;
  displayName: string;
  shortPlayerId: string;
  status: "Pending";
};

type FriendsSnapshot = {
  friendCode: string;
  friends: FriendSummary[];
  incomingRequests: FriendRequestSummary[];
  outgoingRequests: FriendRequestSummary[];
};

type FriendsState =
  | { status: "idle" }
  | { status: "loading" }
  | { status: "ready"; snapshot: FriendsSnapshot; message?: string }
  | { status: "error"; message: string };

type OnlineParticipant = {
  playerId: string;
  displayName: string;
  side: "Blue" | "Red";
  towerHp: number;
  elixir: number;
  maxElixir: number;
};

type OnlineUnit = {
  unitId: string;
  ownerPlayerId: string;
  side: "Blue" | "Red";
  cardId: string;
  name: string;
  lane: "left" | "center" | "right";
  position: number;
  damagePerTick: number;
};

type OnlineBattleSnapshot = {
  roomId: string;
  status: "Active" | "Ended";
  result: "None" | "PlayerOneWin" | "PlayerTwoWin" | "Draw";
  tick: number;
  durationTicks: number;
  playerOne: OnlineParticipant;
  playerTwo: OnlineParticipant;
  starterDeck: BattleCard[];
  units: OnlineUnit[];
};

type OnlineBattleState = {
  status: "Idle" | "Waiting" | "Active" | "Ended";
  roomId?: string;
  snapshot?: OnlineBattleSnapshot;
};

type OnlineState =
  | { status: "idle" }
  | { status: "loading" }
  | { status: "ready"; state: OnlineBattleState; message?: string }
  | { status: "error"; message: string };

type FriendlyBattleInvite = {
  inviteId: string;
  requesterPlayerId: string;
  requesterDisplayName: string;
  addresseePlayerId: string;
  addresseeDisplayName: string;
  status: "Pending" | "Accepted" | "Rejected" | "Cancelled" | "Expired";
  roomId?: string;
  createdAt: string;
  updatedAt: string;
  expiresAt: string;
};

type FriendlyBattleSnapshot = {
  incomingInvites: FriendlyBattleInvite[];
  outgoingInvites: FriendlyBattleInvite[];
  activeRoom?: OnlineBattleState;
};

type FriendlyBattleState =
  | { status: "loading" }
  | { status: "ready"; snapshot: FriendlyBattleSnapshot; message?: string }
  | { status: "error"; message: string };

type ViewState = "home" | "battle" | "friends" | "online";

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
  const [friends, setFriends] = useState<FriendsState>({ status: "idle" });
  const [online, setOnline] = useState<OnlineState>({ status: "idle" });
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

    let disposed = false;
    fetchOnlineCurrent()
      .then((state) => {
        if (!disposed && (state.status === "Waiting" || state.status === "Active")) {
          setOnline({ status: "ready", state, message: "Online battle restored." });
          setView("online");
        }
      })
      .catch(() => {
        // Online resume is best effort; the home screen remains usable without it.
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

    import("./game/battlePreview").then(async ({ createBattlePreview }) => {
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

  const openFriends = async () => {
    setActiveCommand("friends");
    setView("friends");
    setFriends({ status: "loading" });
    try {
      setFriends({ status: "ready", snapshot: await fetchFriends() });
    } catch (error) {
      setFriends({
        status: "error",
        message: error instanceof Error ? error.message : "Friend service is unavailable."
      });
    }
  };

  const openOnline = async () => {
    setActiveCommand("online");
    setView("online");
    setOnline({ status: "loading" });
    try {
      setOnline({ status: "ready", state: await postOnlineMatchmaking() });
    } catch (error) {
      setOnline({
        status: "error",
        message: error instanceof Error ? error.message : "Online battle service is unavailable."
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

  if (view === "friends") {
    return (
      <FriendsScreen
        friends={friends}
        onBackHome={() => setView("home")}
        onReload={openFriends}
        onSnapshot={setFriends}
        onFriendlyRoom={(state) => {
          setOnline({ status: "ready", state, message: "Friendly battle accepted." });
          setView("online");
        }}
      />
    );
  }

  if (view === "online") {
    return (
      <OnlineBattleScreen
        online={online}
        playerId={session.player.playerId}
        reducedMotion={reducedMotion}
        onBackHome={() => setView("home")}
        onQueue={openOnline}
        onSnapshot={setOnline}
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
            onClick={openFriends}
          >
            Friends
          </button>
          <button
            type="button"
            data-testid="online-battle-button"
            aria-pressed={activeCommand === "online"}
            onClick={openOnline}
          >
            Online Battle
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
            <h2>{commandCopy[activeCommand === "friends" ? "battle" : activeCommand].title}</h2>
            <p>{commandCopy[activeCommand === "friends" ? "battle" : activeCommand].body}</p>
          </section>
        </nav>
      </section>

      <p className="guest-warning" data-testid="guest-warning">
        {session.player.guestWarning}
      </p>
    </main>
  );
}

function FriendsScreen({
  friends,
  onBackHome,
  onReload,
  onSnapshot,
  onFriendlyRoom
}: {
  friends: FriendsState;
  onBackHome: () => void;
  onReload: () => void;
  onSnapshot: (state: FriendsState) => void;
  onFriendlyRoom: (state: OnlineBattleState) => void;
}) {
  const [friendCode, setFriendCode] = useState("");
  const [friendly, setFriendly] = useState<FriendlyBattleState>({ status: "loading" });

  useEffect(() => {
    let disposed = false;
    fetchFriendlyBattles()
      .then((snapshot) => {
        if (!disposed) {
          setFriendly({ status: "ready", snapshot });
        }
      })
      .catch((error) => {
        if (!disposed) {
          setFriendly({
            status: "error",
            message: error instanceof Error ? error.message : "Friendly battle load failed."
          });
        }
      });

    return () => {
      disposed = true;
    };
  }, []);

  if (friends.status === "loading" || friends.status === "idle") {
    return (
      <main className="shell friends-shell" data-testid="friends-loading">
        <section className="status-panel">Loading friends...</section>
      </main>
    );
  }

  if (friends.status === "error") {
    return (
      <main className="shell friends-shell" data-testid="friends-error">
        <section className="status-panel">
          <h1>Friend connection problem</h1>
          <p>{friends.message}</p>
          <button type="button" data-testid="friends-retry-button" onClick={onReload}>
            Retry
          </button>
          <button type="button" className="secondary-button" onClick={onBackHome}>
            Back Home
          </button>
        </section>
      </main>
    );
  }

  const snapshot = friends.snapshot;

  const submitRequest = async (event: React.FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    try {
      const next = await postFriendRequest(friendCode);
      setFriendCode("");
      onSnapshot({ status: "ready", snapshot: next, message: "Friend request sent." });
    } catch (error) {
      const nextSnapshot = error instanceof FriendRequestError && error.snapshot
        ? error.snapshot
        : snapshot;
      onSnapshot({
        status: "ready",
        snapshot: nextSnapshot,
        message: error instanceof Error ? error.message : "Friend request failed."
      });
    }
  };

  const updateRequest = async (friendshipId: string, action: "accept" | "reject") => {
    try {
      const next = await postFriendAction(friendshipId, action);
      onSnapshot({
        status: "ready",
        snapshot: next,
        message: action === "accept" ? "Friend added." : "Friend request rejected."
      });
    } catch (error) {
      const nextSnapshot = error instanceof FriendRequestError && error.snapshot
        ? error.snapshot
        : snapshot;
      onSnapshot({
        status: "ready",
        snapshot: nextSnapshot,
        message: error instanceof Error ? error.message : "Friend update failed."
      });
    }
  };

  const challengeFriend = async (friendPlayerId: string) => {
    try {
      const snapshot = await postFriendlyInvite(friendPlayerId);
      setFriendly({ status: "ready", snapshot, message: "Friendly battle challenge sent." });
    } catch (error) {
      const nextSnapshot = error instanceof FriendlyBattleError && error.snapshot
        ? error.snapshot
        : friendly.status === "ready" ? friendly.snapshot : { incomingInvites: [], outgoingInvites: [] };
      setFriendly({
        status: "ready",
        snapshot: nextSnapshot,
        message: error instanceof Error ? error.message : "Friendly battle challenge failed."
      });
    }
  };

  const updateInvite = async (inviteId: string, action: "accept" | "reject" | "cancel") => {
    try {
      const snapshot = await postFriendlyAction(inviteId, action);
      if (action === "accept" && snapshot.activeRoom) {
        onFriendlyRoom(snapshot.activeRoom);
        return;
      }

      setFriendly({
        status: "ready",
        snapshot,
        message: action === "reject"
          ? "Friendly battle declined."
          : action === "cancel" ? "Friendly battle cancelled." : "Friendly battle updated."
      });
    } catch (error) {
      const nextSnapshot = error instanceof FriendlyBattleError && error.snapshot
        ? error.snapshot
        : friendly.status === "ready" ? friendly.snapshot : { incomingInvites: [], outgoingInvites: [] };
      setFriendly({
        status: "ready",
        snapshot: nextSnapshot,
        message: error instanceof Error ? error.message : "Friendly battle update failed."
      });
    }
  };

  return (
    <main className="shell friends-shell" data-testid="friends-ready">
      <header className="battle-topbar">
        <div>
          <p className="eyebrow">Social</p>
          <h1>Friends</h1>
        </div>
        <button type="button" className="secondary-button" data-testid="friends-back-home-button" onClick={onBackHome}>
          Back Home
        </button>
      </header>

      <section className="friends-layout">
        <section className="friend-code-panel" aria-label="Your friend code">
          <p className="eyebrow">Your code</p>
          <strong data-testid="friend-code">{snapshot.friendCode}</strong>
          <form className="friend-request-form" onSubmit={submitRequest}>
            <label htmlFor="friend-code-input">Add by code</label>
            <div>
              <input
                id="friend-code-input"
                data-testid="friend-code-input"
                value={friendCode}
                onChange={(event) => setFriendCode(event.target.value)}
                placeholder="ABCDEFGH"
                autoComplete="off"
              />
              <button type="submit" data-testid="send-friend-request-button">
                Send
              </button>
            </div>
          </form>
          <p className="friends-status" data-testid="friends-status" aria-live="polite">
            {friends.message ?? "Share this code with another guest player."}
          </p>
        </section>

        <section className="friends-list-panel" aria-label="Friends and requests">
          <FriendSection title="Friends" emptyText="No friends yet." emptyTestId="friends-empty-state">
            {snapshot.friends.map((friend) => (
              <FriendRow
                key={friend.playerId}
                testId="friend-row"
                displayName={friend.displayName}
                meta={`${friend.trophies} trophies / ${friend.shortPlayerId}`}
                badge={friend.status}
              >
                <button
                  type="button"
                  data-testid="challenge-friend-button"
                  onClick={() => challengeFriend(friend.playerId)}
                >
                  Challenge
                </button>
              </FriendRow>
            ))}
          </FriendSection>

          <FriendSection title="Challenges" emptyText="No friendly battle challenges.">
            {friendly.status === "loading" ? (
              <p className="friend-empty">Loading challenges...</p>
            ) : null}
            {friendly.status === "error" ? (
              <p className="friends-status" data-testid="challenge-status" aria-live="polite">{friendly.message}</p>
            ) : null}
            {friendly.status === "ready" ? (
              <>
                <p className="friends-status" data-testid="challenge-status" aria-live="polite">
                  {friendly.message ?? "Challenge an accepted friend to a non-ranked sandbox battle."}
                </p>
                {friendly.snapshot.incomingInvites.map((invite) => (
                  <ChallengeRow
                    key={invite.inviteId}
                    invite={invite}
                    testId="incoming-challenge-row"
                    label={`${invite.requesterDisplayName} challenged you`}
                  >
                    <button
                      type="button"
                      data-testid="accept-challenge-button"
                      onClick={() => updateInvite(invite.inviteId, "accept")}
                    >
                      Accept
                    </button>
                    <button
                      type="button"
                      className="secondary-button"
                      data-testid="decline-challenge-button"
                      onClick={() => updateInvite(invite.inviteId, "reject")}
                    >
                      Decline
                    </button>
                  </ChallengeRow>
                ))}
                {friendly.snapshot.outgoingInvites.map((invite) => (
                  <ChallengeRow
                    key={invite.inviteId}
                    invite={invite}
                    testId="outgoing-challenge-row"
                    label={`Waiting for ${invite.addresseeDisplayName}`}
                  >
                    <button
                      type="button"
                      className="secondary-button"
                      data-testid="cancel-challenge-button"
                      onClick={() => updateInvite(invite.inviteId, "cancel")}
                    >
                      Cancel
                    </button>
                  </ChallengeRow>
                ))}
              </>
            ) : null}
          </FriendSection>

          <FriendSection title="Incoming" emptyText="No incoming requests.">
            {snapshot.incomingRequests.map((request) => (
              <FriendRow
                key={request.friendshipId}
                testId="incoming-request-row"
                displayName={request.displayName}
                meta={request.shortPlayerId}
                badge={request.status}
              >
                <button
                  type="button"
                  data-testid="accept-friend-request-button"
                  onClick={() => updateRequest(request.friendshipId, "accept")}
                >
                  Accept
                </button>
                <button
                  type="button"
                  className="secondary-button"
                  data-testid="reject-friend-request-button"
                  onClick={() => updateRequest(request.friendshipId, "reject")}
                >
                  Reject
                </button>
              </FriendRow>
            ))}
          </FriendSection>

          <FriendSection title="Outgoing" emptyText="No outgoing requests.">
            {snapshot.outgoingRequests.map((request) => (
              <FriendRow
                key={request.friendshipId}
                testId="outgoing-request-row"
                displayName={request.displayName}
                meta={request.shortPlayerId}
                badge={request.status}
              />
            ))}
          </FriendSection>
        </section>
      </section>
    </main>
  );
}

function FriendSection({
  title,
  emptyText,
  emptyTestId,
  children
}: {
  title: string;
  emptyText: string;
  emptyTestId?: string;
  children: React.ReactNode;
}) {
  const items = React.Children.toArray(children);
  return (
    <section className="friend-section">
      <h2>{title}</h2>
      {items.length > 0 ? items : <p className="friend-empty" data-testid={emptyTestId}>{emptyText}</p>}
    </section>
  );
}

function FriendRow({
  testId,
  displayName,
  meta,
  badge,
  children
}: {
  testId: string;
  displayName: string;
  meta: string;
  badge: string;
  children?: React.ReactNode;
}) {
  return (
    <div className="friend-row" data-testid={testId}>
      <div className="friend-avatar" aria-hidden="true">
        {displayName.slice(0, 2).toUpperCase()}
      </div>
      <div>
        <strong>{displayName}</strong>
        <span>{meta}</span>
      </div>
      <small>{badge}</small>
      {children ? <div className="friend-actions">{children}</div> : null}
    </div>
  );
}

function ChallengeRow({
  invite,
  testId,
  label,
  children
}: {
  invite: FriendlyBattleInvite;
  testId: string;
  label: string;
  children: React.ReactNode;
}) {
  return (
    <div className="challenge-row" data-testid={testId}>
      <div>
        <strong>{label}</strong>
        <span>{invite.status}</span>
      </div>
      <div className="friend-actions">{children}</div>
    </div>
  );
}

function OnlineBattleScreen({
  online,
  playerId,
  reducedMotion,
  onBackHome,
  onQueue,
  onSnapshot
}: {
  online: OnlineState;
  playerId: string;
  reducedMotion: boolean;
  onBackHome: () => void;
  onQueue: () => void;
  onSnapshot: (state: OnlineState) => void;
}) {
  useEffect(() => {
    if (online.status !== "ready" || online.state.status !== "Waiting") {
      return;
    }

    const interval = window.setInterval(async () => {
      try {
        const state = await fetchOnlineCurrent();
        onSnapshot({
          status: "ready",
          state,
          message: state.status === "Waiting" ? "Waiting for another guest player." : "Match found."
        });
      } catch {
        onSnapshot({ status: "error", message: "Online matchmaking refresh failed." });
      }
    }, 900);

    return () => window.clearInterval(interval);
  }, [online, onSnapshot]);

  useEffect(() => {
    if (
      online.status !== "ready" ||
      online.state.status !== "Active" ||
      !online.state.snapshot ||
      online.state.snapshot.status !== "Active"
    ) {
      return;
    }

    const roomId = online.state.snapshot.roomId;
    const interval = window.setInterval(async () => {
      try {
        const state = await postOnlineTick(roomId, 2);
        onSnapshot({ status: "ready", state, message: "Battle active." });
      } catch {
        onSnapshot({ status: "error", message: "Online battle tick failed." });
      }
    }, reducedMotion ? 1400 : 850);

    return () => window.clearInterval(interval);
  }, [online, onSnapshot, reducedMotion]);

  if (online.status === "loading" || online.status === "idle") {
    return (
      <main className="shell online-shell" data-testid="online-loading">
        <section className="status-panel">Connecting to online matchmaking...</section>
      </main>
    );
  }

  if (online.status === "error") {
    return (
      <main className="shell online-shell" data-testid="online-error">
        <section className="status-panel">
          <h1>Online connection problem</h1>
          <p>{online.message}</p>
          <button type="button" data-testid="online-retry-button" onClick={onQueue}>
            Retry
          </button>
          <button type="button" className="secondary-button" onClick={onBackHome}>
            Back Home
          </button>
        </section>
      </main>
    );
  }

  const state = online.state;
  const cancelQueue = async () => {
    try {
      onSnapshot({
        status: "ready",
        state: await deleteOnlineMatchmaking(),
        message: "Matchmaking cancelled."
      });
    } catch (error) {
      onSnapshot({
        status: "ready",
        state,
        message: error instanceof Error ? error.message : "Cancel failed."
      });
    }
  };

  if (state.status === "Waiting" || !state.snapshot) {
    return (
      <main className="shell online-shell" data-testid="online-waiting">
        <header className="battle-topbar">
          <div>
            <p className="eyebrow">Online sandbox</p>
            <h1>Matchmaking</h1>
          </div>
          <button type="button" className="secondary-button" data-testid="online-back-home-button" onClick={onBackHome}>
            Back Home
          </button>
        </header>

        <section className="waiting-panel" aria-live="polite">
          <h2>Waiting for opponent</h2>
          <p data-testid="online-status">
            {online.message ?? (state.status === "Idle" ? "Ready to queue." : "Waiting for another guest player.")}
          </p>
          <div className="online-actions">
            {state.status === "Idle" ? (
              <button type="button" data-testid="online-queue-button" onClick={onQueue}>
                Queue Again
              </button>
            ) : (
              <button type="button" data-testid="cancel-online-button" onClick={cancelQueue}>
                Cancel
              </button>
            )}
          </div>
        </section>
      </main>
    );
  }

  const snapshot = state.snapshot;
  const self = snapshot.playerOne.playerId === playerId ? snapshot.playerOne : snapshot.playerTwo;
  const opponent = snapshot.playerOne.playerId === playerId ? snapshot.playerTwo : snapshot.playerOne;
  const deployY = self.side === "Blue" ? 0.72 : 0.28;
  const resultText = onlineResultText(snapshot, playerId);

  const deploy = async (cardId: string) => {
    try {
      playCardSfx(cardId);
      const next = await postOnlineDeploy(snapshot.roomId, {
        cardId,
        lane: "center",
        x: 0.5,
        y: deployY
      });
      onSnapshot({ status: "ready", state: next, message: "Card deployed." });
    } catch (error) {
      onSnapshot({
        status: "ready",
        state,
        message: error instanceof Error ? error.message : "Deploy rejected."
      });
    }
  };

  return (
    <main className="shell online-shell" data-testid="online-ready">
      <header className="battle-topbar">
        <div>
          <p className="eyebrow">Online sandbox</p>
          <h1>Guest Battle</h1>
        </div>
        <button type="button" className="secondary-button" data-testid="online-back-home-button" onClick={onBackHome}>
          Back Home
        </button>
      </header>

      <section className="battle-hud online-hud" aria-label="Online battle state">
        <div>
          <span>{opponent.displayName}</span>
          <strong data-testid="online-player-two-hp">{opponent.towerHp}</strong>
        </div>
        <div>
          <span>Room</span>
          <strong data-testid="online-room-id">{snapshot.roomId.slice(0, 8)}</strong>
          <span className="sr-only" data-testid="online-room-full-id">{snapshot.roomId}</span>
        </div>
        <div>
          <span>{self.displayName}</span>
          <strong data-testid="online-player-one-hp">{self.towerHp}</strong>
        </div>
      </section>

      <section className="online-layout">
        <div className="online-arena" data-testid="online-arena" role="img" aria-label="Server snapshot of the online arena">
          <div className={`tower online-tower online-tower-${opponent.side.toLowerCase()}`}>
            <TowerArt assetKey="topTower" />
            <span>{opponent.side} {opponent.towerHp}</span>
          </div>
          <div className="river" />
          <div className={`tower online-tower online-tower-${self.side.toLowerCase()}`}>
            <TowerArt assetKey="bottomTower" />
            <span>{self.side} {self.towerHp}</span>
          </div>
          {snapshot.units.map((unit) => (
            <div
              key={unit.unitId}
              className={`online-unit lane-${unit.lane} side-${unit.side.toLowerCase()}`}
              style={{ top: `${Math.max(9, Math.min(88, unit.position / 10))}%` }}
              data-testid="online-unit"
            >
              <UnitArt cardId={unit.cardId} name={unit.name} />
            </div>
          ))}
        </div>

        <aside className="online-controls">
          <section className="online-status-panel" aria-live="polite">
            <h2>{snapshot.status === "Ended" ? "Battle ended" : "Battle active"}</h2>
            <p data-testid="online-status">{online.message ?? "Deploy starter cards on your side of the river."}</p>
            <dl>
              <div>
                <dt>Your elixir</dt>
                <dd data-testid="online-player-one-elixir">{self.elixir}/{self.maxElixir}</dd>
              </div>
              <div>
                <dt>Opponent elixir</dt>
                <dd data-testid="online-player-two-elixir">{opponent.elixir}/{opponent.maxElixir}</dd>
              </div>
              <div>
                <dt>Timer</dt>
                <dd>{Math.max(0, snapshot.durationTicks - snapshot.tick)}</dd>
              </div>
            </dl>
            <strong data-testid="online-result">{resultText}</strong>
          </section>

          <section className="starter-deck" aria-label="Online starter deck">
            {snapshot.starterDeck.map((card) => (
              <button
                type="button"
                key={card.cardId}
                data-testid={`online-deploy-card-${card.cardId}`}
                disabled={snapshot.status === "Ended" || self.elixir < card.elixirCost}
                onClick={() => deploy(card.cardId)}
              >
                <CardArt card={card} />
                <small>{card.elixirCost} elixir</small>
              </button>
            ))}
          </section>
        </aside>
      </section>
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
      playCardSfx(cardId);
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
          <div className="tower cpu-tower">
            <TowerArt assetKey="topTower" />
            <span>CPU {snapshot.cpuTowerHp}</span>
          </div>
          <div className="river" />
          <div className="tower player-tower">
            <TowerArt assetKey="bottomTower" />
            <span>You {snapshot.playerTowerHp}</span>
          </div>
          {snapshot.units.map((unit) => (
            <div
              key={unit.unitId}
              className={`battle-unit lane-${unit.lane}`}
              style={{ top: `${Math.max(10, Math.min(86, unit.position / 10))}%` }}
              data-testid="battle-unit"
            >
              <UnitArt cardId={unit.cardId} name={unit.name} />
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
                <CardArt card={card} />
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

function CardArt({ card }: { card: BattleCard }) {
  const asset = resolveOptionalCardAsset(card.cardId);
  return (
    <span className="card-art">
      {asset ? (
        <img
          src={asset}
          alt=""
          aria-hidden="true"
          onError={(event) => {
            event.currentTarget.style.display = "none";
          }}
        />
      ) : null}
      <span>{card.name.replace("Training ", "")}</span>
    </span>
  );
}

function TowerArt({ assetKey }: { assetKey: "topTower" | "bottomTower" }) {
  const src = assetKey === "topTower"
    ? "/assets/imported/ui/top-tower.png"
    : "/assets/imported/ui/bottom-tower.png";
  return (
    <img
      src={src}
      alt=""
      aria-hidden="true"
      onError={(event) => {
        event.currentTarget.style.display = "none";
      }}
    />
  );
}

function UnitArt({ cardId, name }: { cardId: string; name: string }) {
  const asset = resolveOptionalUnitAsset(cardId);
  return (
    <>
      {asset ? (
        <img
          src={asset}
          alt=""
          aria-hidden="true"
          onError={(event) => {
            event.currentTarget.style.display = "none";
          }}
        />
      ) : null}
      <span>{name.replace("Training ", "")}</span>
    </>
  );
}

function playCardSfx(cardId: string) {
  const audio = new Audio(deploySfxForCard(cardId));
  audio.volume = 0.35;
  void audio.play().catch(() => {
    // Browsers can block autoplay or the optional local file can be missing.
  });
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

async function fetchFriends() {
  const response = await fetch("/api/friends", { credentials: "include" });
  if (!response.ok) {
    throw new Error(await readProblemCode(response, "Friend load failed."));
  }

  return response.json() as Promise<FriendsSnapshot>;
}

async function postFriendRequest(friendCode: string) {
  const response = await fetch("/api/friends/requests", {
    method: "POST",
    credentials: "include",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ friendCode })
  });

  if (!response.ok) {
    throw await readFriendProblem(response, "Friend request failed.");
  }

  return response.json() as Promise<FriendsSnapshot>;
}

async function postFriendAction(friendshipId: string, action: "accept" | "reject") {
  const response = await fetch(`/api/friends/requests/${friendshipId}/${action}`, {
    method: "POST",
    credentials: "include"
  });

  if (!response.ok) {
    throw await readFriendProblem(response, "Friend update failed.");
  }

  return response.json() as Promise<FriendsSnapshot>;
}

async function fetchFriendlyBattles() {
  const response = await fetch("/api/friendly-battles/current", { credentials: "include" });
  if (!response.ok) {
    throw new Error(await readProblemCode(response, "Friendly battle load failed."));
  }

  return response.json() as Promise<FriendlyBattleSnapshot>;
}

async function postFriendlyInvite(friendPlayerId: string) {
  const response = await fetch("/api/friendly-battles/invites", {
    method: "POST",
    credentials: "include",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ friendPlayerId })
  });

  if (!response.ok) {
    throw await readFriendlyProblem(response, "Friendly battle challenge failed.");
  }

  return response.json() as Promise<FriendlyBattleSnapshot>;
}

async function postFriendlyAction(inviteId: string, action: "accept" | "reject" | "cancel") {
  const method = action === "cancel" ? "DELETE" : "POST";
  const suffix = action === "cancel" ? "" : `/${action}`;
  const response = await fetch(`/api/friendly-battles/invites/${inviteId}${suffix}`, {
    method,
    credentials: "include"
  });

  if (!response.ok) {
    throw await readFriendlyProblem(response, "Friendly battle update failed.");
  }

  return response.json() as Promise<FriendlyBattleSnapshot>;
}

async function fetchOnlineCurrent() {
  const response = await fetch("/api/online-battles/current", { credentials: "include" });
  if (!response.ok) {
    throw new Error(await readProblemCode(response, "Online battle load failed."));
  }

  return response.json() as Promise<OnlineBattleState>;
}

async function postOnlineMatchmaking() {
  const response = await fetch("/api/online-battles/matchmaking", {
    method: "POST",
    credentials: "include"
  });

  if (!response.ok) {
    throw new Error(await readProblemCode(response, "Online matchmaking failed."));
  }

  return response.json() as Promise<OnlineBattleState>;
}

async function deleteOnlineMatchmaking() {
  const response = await fetch("/api/online-battles/matchmaking", {
    method: "DELETE",
    credentials: "include"
  });

  if (!response.ok) {
    throw new Error(await readProblemCode(response, "Online matchmaking cancel failed."));
  }

  return response.json() as Promise<OnlineBattleState>;
}

async function postOnlineDeploy(roomId: string, body: unknown) {
  const response = await fetch(`/api/online-battles/${roomId}/commands`, {
    method: "POST",
    credentials: "include",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(body)
  });

  if (!response.ok) {
    throw new Error(await readProblemCode(response, "Online deploy failed."));
  }

  return response.json() as Promise<OnlineBattleState>;
}

async function postOnlineTick(roomId: string, ticks: number) {
  const response = await fetch(`/api/online-battles/${roomId}/tick`, {
    method: "POST",
    credentials: "include",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ ticks })
  });

  if (!response.ok) {
    throw new Error(await readProblemCode(response, "Online tick failed."));
  }

  return response.json() as Promise<OnlineBattleState>;
}

function onlineResultText(snapshot: OnlineBattleSnapshot, playerId: string) {
  if (snapshot.status !== "Ended" || snapshot.result === "None") {
    return "In progress";
  }

  if (snapshot.result === "Draw") {
    return "Draw";
  }

  const selfIsPlayerOne = snapshot.playerOne.playerId === playerId;
  const didWin =
    (snapshot.result === "PlayerOneWin" && selfIsPlayerOne) ||
    (snapshot.result === "PlayerTwoWin" && !selfIsPlayerOne);
  return didWin ? "Victory" : "Defeat";
}

class FriendRequestError extends Error {
  readonly snapshot?: FriendsSnapshot;

  constructor(message: string, snapshot?: FriendsSnapshot) {
    super(message);
    this.name = "FriendRequestError";
    this.snapshot = snapshot;
  }
}

class FriendlyBattleError extends Error {
  readonly snapshot?: FriendlyBattleSnapshot;

  constructor(message: string, snapshot?: FriendlyBattleSnapshot) {
    super(message);
    this.name = "FriendlyBattleError";
    this.snapshot = snapshot;
  }
}

async function readFriendProblem(response: Response, fallback: string) {
  try {
    const problem = (await response.json()) as {
      code?: string;
      title?: string;
      snapshot?: FriendsSnapshot;
    };
    return new FriendRequestError(problem.code ?? problem.title ?? fallback, problem.snapshot);
  } catch {
    return new FriendRequestError(fallback);
  }
}

async function readFriendlyProblem(response: Response, fallback: string) {
  try {
    const problem = (await response.json()) as {
      code?: string;
      title?: string;
      snapshot?: FriendlyBattleSnapshot;
    };
    return new FriendlyBattleError(problem.code ?? problem.title ?? fallback, problem.snapshot);
  } catch {
    return new FriendlyBattleError(fallback);
  }
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
