namespace Game.Domain;

public sealed class SoloBattleEngine
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

    public BattleSnapshot Start(Guid playerId, Guid? battleId = null)
    {
        return new BattleSnapshot(
            battleId ?? Guid.NewGuid(),
            playerId,
            Status: "Active",
            Result: "None",
            Tick: 0,
            DurationTicks: SoloBattleRules.DurationTicks,
            PlayerTowerHp: SoloBattleRules.InitialTowerHp,
            CpuTowerHp: SoloBattleRules.InitialTowerHp,
            Elixir: SoloBattleRules.InitialElixir,
            MaxElixir: SoloBattleRules.MaxElixir,
            StarterDeck: Cards.Values
                .Select(card => new BattleCardSnapshot(card.CardId, card.Name, card.ElixirCost))
                .ToList(),
            Units: new List<BattleUnitSnapshot>());
    }

    public BattleOperationResult Deploy(BattleSnapshot snapshot, DeployBattleCommand command)
    {
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

        if (command.Y < 0.5 || command.Y > 0.95 || command.X < 0.05 || command.X > 0.95)
        {
            return Rejected(snapshot, "InvalidPlacement");
        }

        if (snapshot.Elixir < card.ElixirCost)
        {
            return Rejected(snapshot, "InsufficientElixir");
        }

        var position = Math.Clamp((int)Math.Round(command.Y * 1000), 500, 950);
        var units = snapshot.Units.ToList();
        units.Add(new BattleUnitSnapshot(
            Guid.NewGuid().ToString("N"),
            card.CardId,
            card.Name,
            command.Lane.ToLowerInvariant(),
            position,
            card.DamagePerTick));

        return new BattleOperationResult(
            Accepted: true,
            snapshot with
            {
                Elixir = snapshot.Elixir - card.ElixirCost,
                Units = units
            },
            ErrorCode: null);
    }

    public BattleSnapshot Advance(BattleSnapshot snapshot, int ticks)
    {
        var next = snapshot;
        for (var i = 0; i < ticks && next.IsActive; i++)
        {
            next = AdvanceOneTick(next);
        }

        return next;
    }

    private static BattleSnapshot AdvanceOneTick(BattleSnapshot snapshot)
    {
        var cpuTowerHp = snapshot.CpuTowerHp;
        var units = new List<BattleUnitSnapshot>();

        foreach (var unit in snapshot.Units)
        {
            if (unit.CardId == "training-bolt")
            {
                cpuTowerHp -= unit.DamagePerTick;
                continue;
            }

            if (unit.Position <= SoloBattleRules.EnemyTowerPosition + SoloBattleRules.AttackRange)
            {
                cpuTowerHp -= unit.DamagePerTick;
                units.Add(unit);
                continue;
            }

            units.Add(unit with
            {
                Position = Math.Max(SoloBattleRules.EnemyTowerPosition, unit.Position - GetSpeed(unit.CardId))
            });
        }

        var tick = snapshot.Tick + 1;
        var elixir = Math.Min(snapshot.MaxElixir, snapshot.Elixir + (tick % 2 == 0 ? 1 : 0));
        var status = "Active";
        var result = "None";

        if (cpuTowerHp <= 0)
        {
            cpuTowerHp = 0;
            status = "Ended";
            result = "Win";
        }
        else if (tick >= snapshot.DurationTicks)
        {
            status = "Ended";
            result = "Timeout";
        }

        return snapshot with
        {
            Status = status,
            Result = result,
            Tick = tick,
            CpuTowerHp = cpuTowerHp,
            Elixir = elixir,
            Units = units
        };
    }

    private static int GetSpeed(string cardId)
    {
        return Cards.TryGetValue(cardId, out var card) ? card.SpeedPerTick : 0;
    }

    private static BattleOperationResult Rejected(BattleSnapshot snapshot, string code)
    {
        return new BattleOperationResult(Accepted: false, snapshot, code);
    }
}

