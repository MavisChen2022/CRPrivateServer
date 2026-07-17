namespace Game.Domain;

public static class SoloBattleRules
{
    public const int InitialTowerHp = 1000;
    public const int InitialElixir = 5;
    public const int MaxElixir = 10;
    public const int DurationTicks = 60;
    public const int EnemyTowerPosition = 120;
    public const int AttackRange = 70;
}

public sealed record BattleCardDefinition(
    string CardId,
    string Name,
    int ElixirCost,
    int SpeedPerTick,
    int DamagePerTick);

public sealed record BattleCardSnapshot(
    string CardId,
    string Name,
    int ElixirCost);

public sealed record BattleUnitSnapshot(
    string UnitId,
    string CardId,
    string Name,
    string Lane,
    int Position,
    int DamagePerTick);

public sealed record BattleSnapshot(
    Guid BattleId,
    Guid PlayerId,
    string Status,
    string Result,
    int Tick,
    int DurationTicks,
    int PlayerTowerHp,
    int CpuTowerHp,
    int Elixir,
    int MaxElixir,
    List<BattleCardSnapshot> StarterDeck,
    List<BattleUnitSnapshot> Units)
{
    public bool IsActive => Status == "Active";
}

public sealed record DeployBattleCommand(
    string CardId,
    string Lane,
    double X,
    double Y);

public sealed record BattleOperationResult(
    bool Accepted,
    BattleSnapshot Snapshot,
    string? ErrorCode);

