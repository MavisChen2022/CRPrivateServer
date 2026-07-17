namespace Game.Domain;

public sealed class OnlineBattleEngine
{
    private static readonly IReadOnlyDictionary<string, BattleCardDefinition> Cards =
        new Dictionary<string, BattleCardDefinition>(StringComparer.OrdinalIgnoreCase)
        {
            ["training-knight"] = new("training-knight", "Training Knight", 3, 70, 90),
            ["training-archer"] = new("training-archer", "Training Archer", 2, 55, 65),
            ["training-guard"] = new("training-guard", "Training Guard", 4, 45, 120),
            ["training-bolt"] = new("training-bolt", "Training Bolt", 5, 0, 180)
        };

    private static readonly HashSet<string> Lanes = new(StringComparer.OrdinalIgnoreCase)
    {
        "left",
        "center",
        "right"
    };

    public OnlineBattleSnapshot Start(
        Guid playerOneId,
        string playerOneName,
        Guid playerTwoId,
        string playerTwoName,
        Guid? roomId = null)
    {
        return new OnlineBattleSnapshot(
            roomId ?? Guid.NewGuid(),
            Status: "Active",
            Result: "None",
            Tick: 0,
            DurationTicks: SoloBattleRules.DurationTicks,
            PlayerOne: new OnlineBattleParticipantSnapshot(
                playerOneId,
                playerOneName,
                "Blue",
                SoloBattleRules.InitialTowerHp,
                SoloBattleRules.InitialElixir,
                SoloBattleRules.MaxElixir),
            PlayerTwo: new OnlineBattleParticipantSnapshot(
                playerTwoId,
                playerTwoName,
                "Red",
                SoloBattleRules.InitialTowerHp,
                SoloBattleRules.InitialElixir,
                SoloBattleRules.MaxElixir),
            StarterDeck: Cards.Values.Select(card => new BattleCardSnapshot(
                card.CardId,
                card.Name,
                card.ElixirCost)).ToList(),
            Units: new List<OnlineBattleUnitSnapshot>());
    }

    public OnlineBattleOperationResult Deploy(
        OnlineBattleSnapshot snapshot,
        Guid playerId,
        DeployBattleCommand command)
    {
        if (!snapshot.HasParticipant(playerId))
        {
            return Rejected(snapshot, "OnlineBattleForbidden");
        }

        if (!snapshot.IsActive)
        {
            return Rejected(snapshot, "BattleAlreadyEnded");
        }

        if (!Cards.TryGetValue(command.CardId, out var card))
        {
            return Rejected(snapshot, "InvalidCard");
        }

        if (!Lanes.Contains(command.Lane))
        {
            return Rejected(snapshot, "InvalidLane");
        }

        var actor = snapshot.Participant(playerId);
        if (!IsOwnSide(actor.Side, command.X, command.Y))
        {
            return Rejected(snapshot, "InvalidPlacement");
        }

        if (actor.Elixir < card.ElixirCost)
        {
            return Rejected(snapshot, "InsufficientElixir");
        }

        var units = snapshot.Units.ToList();
        var next = SpendElixir(snapshot, playerId, card.ElixirCost);
        if (card.CardId == "training-bolt")
        {
            return new OnlineBattleOperationResult(
                true,
                DamageOpponent(next, playerId, card.DamagePerTick),
                null);
        }

        units.Add(new OnlineBattleUnitSnapshot(
            Guid.NewGuid().ToString("N"),
            playerId,
            actor.Side,
            card.CardId,
            card.Name,
            command.Lane.ToLowerInvariant(),
            Math.Clamp((int)Math.Round(command.Y * 1000), 50, 950),
            card.DamagePerTick));

        return new OnlineBattleOperationResult(true, next with { Units = units }, null);
    }

    public OnlineBattleSnapshot Advance(OnlineBattleSnapshot snapshot, int ticks)
    {
        var next = snapshot;
        for (var i = 0; i < ticks && next.IsActive; i++)
        {
            next = AdvanceOneTick(next);
        }

        return next;
    }

    private static OnlineBattleSnapshot AdvanceOneTick(OnlineBattleSnapshot snapshot)
    {
        var playerOne = snapshot.PlayerOne;
        var playerTwo = snapshot.PlayerTwo;
        var units = new List<OnlineBattleUnitSnapshot>();

        foreach (var unit in snapshot.Units)
        {
            if (unit.Side == "Blue")
            {
                if (unit.Position <= SoloBattleRules.EnemyTowerPosition + SoloBattleRules.AttackRange)
                {
                    playerTwo = playerTwo with { TowerHp = playerTwo.TowerHp - unit.DamagePerTick };
                    units.Add(unit);
                    continue;
                }

                units.Add(unit with
                {
                    Position = Math.Max(SoloBattleRules.EnemyTowerPosition, unit.Position - GetSpeed(unit.CardId))
                });
                continue;
            }

            if (unit.Position >= 1000 - SoloBattleRules.EnemyTowerPosition - SoloBattleRules.AttackRange)
            {
                playerOne = playerOne with { TowerHp = playerOne.TowerHp - unit.DamagePerTick };
                units.Add(unit);
                continue;
            }

            units.Add(unit with
            {
                Position = Math.Min(1000 - SoloBattleRules.EnemyTowerPosition, unit.Position + GetSpeed(unit.CardId))
            });
        }

        var tick = snapshot.Tick + 1;
        if (tick % 2 == 0)
        {
            playerOne = playerOne with { Elixir = Math.Min(playerOne.MaxElixir, playerOne.Elixir + 1) };
            playerTwo = playerTwo with { Elixir = Math.Min(playerTwo.MaxElixir, playerTwo.Elixir + 1) };
        }

        return FinishIfNeeded(snapshot with
        {
            Tick = tick,
            PlayerOne = playerOne,
            PlayerTwo = playerTwo,
            Units = units
        });
    }

    private static OnlineBattleSnapshot SpendElixir(
        OnlineBattleSnapshot snapshot,
        Guid playerId,
        int elixirCost)
    {
        if (snapshot.PlayerOne.PlayerId == playerId)
        {
            return snapshot with
            {
                PlayerOne = snapshot.PlayerOne with { Elixir = snapshot.PlayerOne.Elixir - elixirCost }
            };
        }

        return snapshot with
        {
            PlayerTwo = snapshot.PlayerTwo with { Elixir = snapshot.PlayerTwo.Elixir - elixirCost }
        };
    }

    private static OnlineBattleSnapshot DamageOpponent(
        OnlineBattleSnapshot snapshot,
        Guid playerId,
        int damage)
    {
        var damaged = snapshot.PlayerOne.PlayerId == playerId
            ? snapshot with { PlayerTwo = snapshot.PlayerTwo with { TowerHp = snapshot.PlayerTwo.TowerHp - damage } }
            : snapshot with { PlayerOne = snapshot.PlayerOne with { TowerHp = snapshot.PlayerOne.TowerHp - damage } };
        return FinishIfNeeded(damaged);
    }

    private static OnlineBattleSnapshot FinishIfNeeded(OnlineBattleSnapshot snapshot)
    {
        var playerOneHp = Math.Max(0, snapshot.PlayerOne.TowerHp);
        var playerTwoHp = Math.Max(0, snapshot.PlayerTwo.TowerHp);
        var playerOne = snapshot.PlayerOne with { TowerHp = playerOneHp };
        var playerTwo = snapshot.PlayerTwo with { TowerHp = playerTwoHp };

        if (playerOneHp <= 0 && playerTwoHp <= 0)
        {
            return snapshot with { Status = "Ended", Result = "Draw", PlayerOne = playerOne, PlayerTwo = playerTwo };
        }

        if (playerTwoHp <= 0)
        {
            return snapshot with { Status = "Ended", Result = "PlayerOneWin", PlayerOne = playerOne, PlayerTwo = playerTwo };
        }

        if (playerOneHp <= 0)
        {
            return snapshot with { Status = "Ended", Result = "PlayerTwoWin", PlayerOne = playerOne, PlayerTwo = playerTwo };
        }

        if (snapshot.Tick >= snapshot.DurationTicks)
        {
            var result = playerOneHp == playerTwoHp
                ? "Draw"
                : playerOneHp > playerTwoHp ? "PlayerOneWin" : "PlayerTwoWin";
            return snapshot with { Status = "Ended", Result = result, PlayerOne = playerOne, PlayerTwo = playerTwo };
        }

        return snapshot with { PlayerOne = playerOne, PlayerTwo = playerTwo };
    }

    private static bool IsOwnSide(string side, double x, double y)
    {
        if (x < 0.05 || x > 0.95)
        {
            return false;
        }

        return side == "Blue"
            ? y >= 0.5 && y <= 0.95
            : y >= 0.05 && y <= 0.5;
    }

    private static int GetSpeed(string cardId)
    {
        return Cards.TryGetValue(cardId, out var card) ? card.SpeedPerTick : 0;
    }

    private static OnlineBattleOperationResult Rejected(OnlineBattleSnapshot snapshot, string code)
    {
        return new OnlineBattleOperationResult(false, snapshot, code);
    }
}
