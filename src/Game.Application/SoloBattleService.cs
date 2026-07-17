using Game.Domain;

namespace Game.Application;

public sealed record StoredBattleSession(
    Guid BattleId,
    Guid PlayerId,
    string Status,
    BattleSnapshot Snapshot,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    DateTimeOffset? EndedAt);

public sealed record BattleServiceResult(
    bool Succeeded,
    BattleSnapshot? Snapshot,
    string? ErrorCode,
    int StatusCode);

public interface ISoloBattleStore
{
    Task<StoredBattleSession?> FindActiveByPlayerAsync(
        Guid playerId,
        CancellationToken cancellationToken);

    Task<StoredBattleSession?> FindByIdAsync(
        Guid battleId,
        CancellationToken cancellationToken);

    Task SaveAsync(
        StoredBattleSession session,
        CancellationToken cancellationToken);

    Task AppendCommandAsync(
        Guid battleId,
        Guid playerId,
        string commandType,
        string commandJson,
        int submittedAtTick,
        string? rejectedCode,
        CancellationToken cancellationToken);
}

public sealed class SoloBattleService
{
    private readonly ISoloBattleStore _store;
    private readonly SoloBattleEngine _engine;

    public SoloBattleService(ISoloBattleStore store, SoloBattleEngine engine)
    {
        _store = store;
        _engine = engine;
    }

    public async Task<BattleServiceResult> StartOrResumeAsync(
        Guid playerId,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var active = await _store.FindActiveByPlayerAsync(playerId, cancellationToken);
        if (active is not null)
        {
            return Success(active.Snapshot);
        }

        var snapshot = _engine.Start(playerId);
        await _store.SaveAsync(new StoredBattleSession(
            snapshot.BattleId,
            playerId,
            snapshot.Status,
            snapshot,
            now,
            now,
            EndedAt: null), cancellationToken);

        return Success(snapshot);
    }

    public async Task<BattleServiceResult> GetSnapshotAsync(
        Guid playerId,
        Guid battleId,
        CancellationToken cancellationToken)
    {
        var session = await _store.FindByIdAsync(battleId, cancellationToken);
        return Authorize(playerId, session);
    }

    public async Task<BattleServiceResult> SubmitDeployAsync(
        Guid playerId,
        Guid battleId,
        DeployBattleCommand command,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var session = await _store.FindByIdAsync(battleId, cancellationToken);
        var authorized = Authorize(playerId, session);
        if (!authorized.Succeeded || authorized.Snapshot is null || session is null)
        {
            return authorized;
        }

        var operation = _engine.Deploy(authorized.Snapshot, command);
        await _store.AppendCommandAsync(
            battleId,
            playerId,
            "DeployCard",
            System.Text.Json.JsonSerializer.Serialize(command),
            authorized.Snapshot.Tick,
            operation.ErrorCode,
            cancellationToken);

        if (!operation.Accepted)
        {
            return new BattleServiceResult(false, operation.Snapshot, operation.ErrorCode, 400);
        }

        await SaveSnapshotAsync(session, operation.Snapshot, now, cancellationToken);
        return Success(operation.Snapshot);
    }

    public async Task<BattleServiceResult> AdvanceAsync(
        Guid playerId,
        Guid battleId,
        int ticks,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var session = await _store.FindByIdAsync(battleId, cancellationToken);
        var authorized = Authorize(playerId, session);
        if (!authorized.Succeeded || authorized.Snapshot is null || session is null)
        {
            return authorized;
        }

        var snapshot = _engine.Advance(authorized.Snapshot, Math.Clamp(ticks, 1, 10));
        await SaveSnapshotAsync(session, snapshot, now, cancellationToken);
        return Success(snapshot);
    }

    private async Task SaveSnapshotAsync(
        StoredBattleSession session,
        BattleSnapshot snapshot,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        await _store.SaveAsync(session with
        {
            Status = snapshot.Status,
            Snapshot = snapshot,
            UpdatedAt = now,
            EndedAt = snapshot.IsActive ? null : now
        }, cancellationToken);
    }

    private static BattleServiceResult Authorize(Guid playerId, StoredBattleSession? session)
    {
        if (session is null)
        {
            return new BattleServiceResult(false, null, "BattleNotFound", 404);
        }

        if (session.PlayerId != playerId)
        {
            return new BattleServiceResult(false, null, "BattleForbidden", 403);
        }

        return Success(session.Snapshot);
    }

    private static BattleServiceResult Success(BattleSnapshot snapshot)
    {
        return new BattleServiceResult(true, snapshot, null, 200);
    }
}

