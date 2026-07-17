using Game.Domain;

namespace Game.Application;

public sealed record StoredMatchmakingEntry(
    Guid PlayerId,
    string Status,
    DateTimeOffset QueuedAt,
    DateTimeOffset UpdatedAt,
    Guid? MatchedRoomId);

public sealed record StoredOnlineBattleRoom(
    Guid RoomId,
    Guid PlayerOneId,
    Guid PlayerTwoId,
    string Status,
    OnlineBattleSnapshot Snapshot,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    DateTimeOffset? EndedAt);

public sealed record OnlineBattleState(
    string Status,
    Guid? RoomId,
    OnlineBattleSnapshot? Snapshot);

public sealed record OnlineBattleServiceResult(
    bool Succeeded,
    OnlineBattleState? State,
    string? ErrorCode,
    int StatusCode);

public interface IOnlineBattleStore
{
    Task<StoredMatchmakingEntry?> FindWaitingByPlayerAsync(Guid playerId, CancellationToken cancellationToken);

    Task<StoredMatchmakingEntry?> FindFirstWaitingExceptAsync(Guid playerId, CancellationToken cancellationToken);

    Task SaveQueueEntryAsync(StoredMatchmakingEntry entry, CancellationToken cancellationToken);

    Task<StoredOnlineBattleRoom?> FindActiveByPlayerAsync(Guid playerId, CancellationToken cancellationToken);

    Task<StoredOnlineBattleRoom?> FindByIdAsync(Guid roomId, CancellationToken cancellationToken);

    Task SaveRoomAsync(StoredOnlineBattleRoom room, CancellationToken cancellationToken);

    Task<StoredFriendProfile?> FindProfileAsync(Guid playerId, CancellationToken cancellationToken);

    Task AppendCommandAsync(
        Guid roomId,
        Guid playerId,
        string commandType,
        string commandJson,
        int submittedAtTick,
        string? rejectedCode,
        CancellationToken cancellationToken);
}

public sealed class OnlineBattleService
{
    private readonly IOnlineBattleStore _store;
    private readonly OnlineBattleEngine _engine;

    public OnlineBattleService(IOnlineBattleStore store, OnlineBattleEngine engine)
    {
        _store = store;
        _engine = engine;
    }

    public async Task<OnlineBattleServiceResult> QueueAsync(
        Guid playerId,
        string displayName,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var active = await _store.FindActiveByPlayerAsync(playerId, cancellationToken);
        if (active is not null)
        {
            return Success(StateFromRoom(active));
        }

        var existing = await _store.FindWaitingByPlayerAsync(playerId, cancellationToken);
        if (existing is not null)
        {
            return Success(new OnlineBattleState("Waiting", null, null));
        }

        var opponent = await _store.FindFirstWaitingExceptAsync(playerId, cancellationToken);
        if (opponent is null)
        {
            await _store.SaveQueueEntryAsync(new StoredMatchmakingEntry(
                playerId,
                "Queued",
                now,
                now,
                null), cancellationToken);
            return Success(new OnlineBattleState("Waiting", null, null));
        }

        var opponentProfile = await _store.FindProfileAsync(opponent.PlayerId, cancellationToken);
        if (opponentProfile is null)
        {
            await _store.SaveQueueEntryAsync(new StoredMatchmakingEntry(
                playerId,
                "Queued",
                now,
                now,
                null), cancellationToken);
            return Success(new OnlineBattleState("Waiting", null, null));
        }

        var created = await CreateRoomAsync(
            opponent.PlayerId,
            opponentProfile.DisplayName,
            playerId,
            displayName,
            now,
            cancellationToken);
        if (!created.Succeeded)
        {
            return created;
        }

        await _store.SaveQueueEntryAsync(opponent with
        {
            Status = "Matched",
            UpdatedAt = now,
            MatchedRoomId = created.State!.RoomId
        }, cancellationToken);

        return created;
    }

    public async Task<OnlineBattleServiceResult> CreateRoomAsync(
        Guid playerOneId,
        string playerOneDisplayName,
        Guid playerTwoId,
        string playerTwoDisplayName,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        if (await _store.FindActiveByPlayerAsync(playerOneId, cancellationToken) is not null ||
            await _store.FindActiveByPlayerAsync(playerTwoId, cancellationToken) is not null)
        {
            return new OnlineBattleServiceResult(false, null, "PlayerAlreadyInBattle", 409);
        }

        var snapshot = _engine.Start(
            playerOneId,
            playerOneDisplayName,
            playerTwoId,
            playerTwoDisplayName);
        await _store.SaveRoomAsync(new StoredOnlineBattleRoom(
            snapshot.RoomId,
            snapshot.PlayerOne.PlayerId,
            snapshot.PlayerTwo.PlayerId,
            snapshot.Status,
            snapshot,
            now,
            now,
            null), cancellationToken);

        return Success(new OnlineBattleState("Active", snapshot.RoomId, snapshot));
    }

    public async Task<OnlineBattleServiceResult> CancelQueueAsync(
        Guid playerId,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var waiting = await _store.FindWaitingByPlayerAsync(playerId, cancellationToken);
        if (waiting is null)
        {
            return new OnlineBattleServiceResult(false, null, "MatchmakingNotQueued", 404);
        }

        await _store.SaveQueueEntryAsync(waiting with
        {
            Status = "Cancelled",
            UpdatedAt = now
        }, cancellationToken);
        return Success(new OnlineBattleState("Idle", null, null));
    }

    public async Task<OnlineBattleServiceResult> GetCurrentAsync(
        Guid playerId,
        CancellationToken cancellationToken)
    {
        var active = await _store.FindActiveByPlayerAsync(playerId, cancellationToken);
        if (active is not null)
        {
            return Success(StateFromRoom(active));
        }

        var waiting = await _store.FindWaitingByPlayerAsync(playerId, cancellationToken);
        return Success(waiting is null
            ? new OnlineBattleState("Idle", null, null)
            : new OnlineBattleState("Waiting", null, null));
    }

    public async Task<OnlineBattleServiceResult> GetRoomAsync(
        Guid playerId,
        Guid roomId,
        CancellationToken cancellationToken)
    {
        var room = await _store.FindByIdAsync(roomId, cancellationToken);
        return Authorize(playerId, room);
    }

    public async Task<OnlineBattleServiceResult> SubmitDeployAsync(
        Guid playerId,
        Guid roomId,
        DeployBattleCommand command,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var room = await _store.FindByIdAsync(roomId, cancellationToken);
        var authorized = Authorize(playerId, room);
        if (!authorized.Succeeded || room is null || authorized.State?.Snapshot is null)
        {
            return authorized;
        }

        var operation = _engine.Deploy(authorized.State.Snapshot, playerId, command);
        await _store.AppendCommandAsync(
            roomId,
            playerId,
            "DeployCard",
            System.Text.Json.JsonSerializer.Serialize(command),
            authorized.State.Snapshot.Tick,
            operation.ErrorCode,
            cancellationToken);

        if (!operation.Accepted)
        {
            return new OnlineBattleServiceResult(
                false,
                new OnlineBattleState(operation.Snapshot.Status, operation.Snapshot.RoomId, operation.Snapshot),
                operation.ErrorCode,
                operation.ErrorCode == "OnlineBattleForbidden" ? 403 : 400);
        }

        await SaveSnapshotAsync(room, operation.Snapshot, now, cancellationToken);
        return Success(new OnlineBattleState(operation.Snapshot.Status, operation.Snapshot.RoomId, operation.Snapshot));
    }

    public async Task<OnlineBattleServiceResult> AdvanceAsync(
        Guid playerId,
        Guid roomId,
        int ticks,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var room = await _store.FindByIdAsync(roomId, cancellationToken);
        var authorized = Authorize(playerId, room);
        if (!authorized.Succeeded || room is null || authorized.State?.Snapshot is null)
        {
            return authorized;
        }

        var snapshot = _engine.Advance(authorized.State.Snapshot, Math.Clamp(ticks, 1, 10));
        await SaveSnapshotAsync(room, snapshot, now, cancellationToken);
        return Success(new OnlineBattleState(snapshot.Status, snapshot.RoomId, snapshot));
    }

    private async Task SaveSnapshotAsync(
        StoredOnlineBattleRoom room,
        OnlineBattleSnapshot snapshot,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        await _store.SaveRoomAsync(room with
        {
            Status = snapshot.Status,
            Snapshot = snapshot,
            UpdatedAt = now,
            EndedAt = snapshot.IsActive ? null : now
        }, cancellationToken);
    }

    private static OnlineBattleServiceResult Authorize(Guid playerId, StoredOnlineBattleRoom? room)
    {
        if (room is null)
        {
            return new OnlineBattleServiceResult(false, null, "OnlineBattleNotFound", 404);
        }

        if (room.PlayerOneId != playerId && room.PlayerTwoId != playerId)
        {
            return new OnlineBattleServiceResult(false, null, "OnlineBattleForbidden", 403);
        }

        return Success(StateFromRoom(room));
    }

    private static OnlineBattleState StateFromRoom(StoredOnlineBattleRoom room)
    {
        return new OnlineBattleState(room.Snapshot.Status, room.RoomId, room.Snapshot);
    }

    private static OnlineBattleServiceResult Success(OnlineBattleState state)
    {
        return new OnlineBattleServiceResult(true, state, null, 200);
    }
}
