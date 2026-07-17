using Game.Application;
using Game.Domain;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace Game.Infrastructure;

public sealed class EfSoloBattleStore : ISoloBattleStore
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly RoyaleDbContext _dbContext;

    public EfSoloBattleStore(RoyaleDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<StoredBattleSession?> FindActiveByPlayerAsync(
        Guid playerId,
        CancellationToken cancellationToken)
    {
        var entity = await _dbContext.BattleSessions
            .AsNoTracking()
            .Where(x => x.PlayerId == playerId && x.Status == "Active")
            .FirstOrDefaultAsync(cancellationToken);

        return entity is null ? null : ToStored(entity);
    }

    public async Task<StoredBattleSession?> FindByIdAsync(
        Guid battleId,
        CancellationToken cancellationToken)
    {
        var entity = await _dbContext.BattleSessions
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.BattleId == battleId, cancellationToken);

        return entity is null ? null : ToStored(entity);
    }

    public async Task SaveAsync(
        StoredBattleSession session,
        CancellationToken cancellationToken)
    {
        var existing = await _dbContext.BattleSessions
            .SingleOrDefaultAsync(x => x.BattleId == session.BattleId, cancellationToken);

        if (existing is null)
        {
            _dbContext.BattleSessions.Add(new BattleSessionEntity
            {
                BattleId = session.BattleId,
                PlayerId = session.PlayerId,
                Status = session.Status,
                SnapshotJson = JsonSerializer.Serialize(session.Snapshot, JsonOptions),
                CreatedAt = session.CreatedAt,
                UpdatedAt = session.UpdatedAt,
                EndedAt = session.EndedAt
            });
        }
        else
        {
            existing.Status = session.Status;
            existing.SnapshotJson = JsonSerializer.Serialize(session.Snapshot, JsonOptions);
            existing.UpdatedAt = session.UpdatedAt;
            existing.EndedAt = session.EndedAt;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task AppendCommandAsync(
        Guid battleId,
        Guid playerId,
        string commandType,
        string commandJson,
        int submittedAtTick,
        string? rejectedCode,
        CancellationToken cancellationToken)
    {
        _dbContext.BattleCommands.Add(new BattleCommandEntity
        {
            CommandId = Guid.NewGuid(),
            BattleId = battleId,
            PlayerId = playerId,
            CommandType = commandType,
            CommandJson = commandJson,
            SubmittedAtTick = submittedAtTick,
            CreatedAt = DateTimeOffset.UtcNow,
            RejectedCode = rejectedCode
        });

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private static StoredBattleSession ToStored(BattleSessionEntity entity)
    {
        var snapshot = JsonSerializer.Deserialize<BattleSnapshot>(entity.SnapshotJson, JsonOptions)
            ?? throw new InvalidOperationException("Battle snapshot could not be read.");

        return new StoredBattleSession(
            entity.BattleId,
            entity.PlayerId,
            entity.Status,
            snapshot,
            entity.CreatedAt,
            entity.UpdatedAt,
            entity.EndedAt);
    }
}
