namespace Game.Domain;

public sealed record OnlineBattleParticipantSnapshot(
    Guid PlayerId,
    string DisplayName,
    string Side,
    int TowerHp,
    int Elixir,
    int MaxElixir);

public sealed record OnlineBattleUnitSnapshot(
    string UnitId,
    Guid OwnerPlayerId,
    string Side,
    string CardId,
    string Name,
    string Lane,
    int Position,
    int DamagePerTick);

public sealed record OnlineBattleSnapshot(
    Guid RoomId,
    string Status,
    string Result,
    int Tick,
    int DurationTicks,
    OnlineBattleParticipantSnapshot PlayerOne,
    OnlineBattleParticipantSnapshot PlayerTwo,
    List<BattleCardSnapshot> StarterDeck,
    List<OnlineBattleUnitSnapshot> Units)
{
    public bool IsActive => Status == "Active";

    public bool HasParticipant(Guid playerId)
    {
        return PlayerOne.PlayerId == playerId || PlayerTwo.PlayerId == playerId;
    }

    public OnlineBattleParticipantSnapshot Participant(Guid playerId)
    {
        if (PlayerOne.PlayerId == playerId)
        {
            return PlayerOne;
        }

        if (PlayerTwo.PlayerId == playerId)
        {
            return PlayerTwo;
        }

        throw new InvalidOperationException("Player is not a participant.");
    }
}

public sealed record OnlineBattleOperationResult(
    bool Accepted,
    OnlineBattleSnapshot Snapshot,
    string? ErrorCode);

