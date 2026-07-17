using Game.Application;
using Game.Domain;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace Game.Infrastructure;

public sealed class EfOnlineBattleStore : IOnlineBattleStore
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly RoyaleDbContext _dbContext;

    public EfOnlineBattleStore(RoyaleDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<StoredMatchmakingEntry?> FindWaitingByPlayerAsync(Guid playerId, CancellationToken cancellationToken)
    {
        var entity = await _dbContext.MatchmakingQueue
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.PlayerId == playerId && x.Status == "Queued", cancellationToken);
        return entity is null ? null : ToStored(entity);
    }

    public async Task<StoredMatchmakingEntry?> FindFirstWaitingExceptAsync(Guid playerId, CancellationToken cancellationToken)
    {
        var entities = await _dbContext.MatchmakingQueue
            .AsNoTracking()
            .Where(x => x.PlayerId != playerId && x.Status == "Queued")
            .ToListAsync(cancellationToken);
        var entity = entities.OrderBy(x => x.QueuedAt).FirstOrDefault();
        return entity is null ? null : ToStored(entity);
    }

    public async Task SaveQueueEntryAsync(StoredMatchmakingEntry entry, CancellationToken cancellationToken)
    {
        var entity = await _dbContext.MatchmakingQueue
            .SingleOrDefaultAsync(x => x.PlayerId == entry.PlayerId, cancellationToken);

        if (entity is null)
        {
            _dbContext.MatchmakingQueue.Add(new MatchmakingQueueEntity
            {
                PlayerId = entry.PlayerId,
                Status = entry.Status,
                QueuedAt = entry.QueuedAt,
                UpdatedAt = entry.UpdatedAt,
                MatchedRoomId = entry.MatchedRoomId
            });
        }
        else
        {
            entity.Status = entry.Status;
            entity.QueuedAt = entry.QueuedAt;
            entity.UpdatedAt = entry.UpdatedAt;
            entity.MatchedRoomId = entry.MatchedRoomId;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<StoredOnlineBattleRoom?> FindActiveByPlayerAsync(Guid playerId, CancellationToken cancellationToken)
    {
        var entity = await _dbContext.OnlineBattleRooms
            .AsNoTracking()
            .Where(x => x.Status == "Active" && (x.PlayerOneId == playerId || x.PlayerTwoId == playerId))
            .FirstOrDefaultAsync(cancellationToken);
        return entity is null ? null : ToStored(entity);
    }

    public async Task<StoredOnlineBattleRoom?> FindByIdAsync(Guid roomId, CancellationToken cancellationToken)
    {
        var entity = await _dbContext.OnlineBattleRooms
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.RoomId == roomId, cancellationToken);
        return entity is null ? null : ToStored(entity);
    }

    public async Task SaveRoomAsync(StoredOnlineBattleRoom room, CancellationToken cancellationToken)
    {
        var entity = await _dbContext.OnlineBattleRooms
            .SingleOrDefaultAsync(x => x.RoomId == room.RoomId, cancellationToken);

        if (entity is null)
        {
            _dbContext.OnlineBattleRooms.Add(new OnlineBattleRoomEntity
            {
                RoomId = room.RoomId,
                PlayerOneId = room.PlayerOneId,
                PlayerTwoId = room.PlayerTwoId,
                Status = room.Status,
                SnapshotJson = JsonSerializer.Serialize(room.Snapshot, JsonOptions),
                CreatedAt = room.CreatedAt,
                UpdatedAt = room.UpdatedAt,
                EndedAt = room.EndedAt
            });
        }
        else
        {
            entity.Status = room.Status;
            entity.SnapshotJson = JsonSerializer.Serialize(room.Snapshot, JsonOptions);
            entity.UpdatedAt = room.UpdatedAt;
            entity.EndedAt = room.EndedAt;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<StoredFriendProfile?> FindProfileAsync(Guid playerId, CancellationToken cancellationToken)
    {
        var profile = await _dbContext.PlayerProfiles
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.PlayerId == playerId, cancellationToken);
        return profile is null
            ? null
            : new StoredFriendProfile(profile.PlayerId, profile.DisplayName, profile.Trophies);
    }

    public async Task AppendCommandAsync(
        Guid roomId,
        Guid playerId,
        string commandType,
        string commandJson,
        int submittedAtTick,
        string? rejectedCode,
        CancellationToken cancellationToken)
    {
        _dbContext.OnlineBattleCommands.Add(new OnlineBattleCommandEntity
        {
            CommandId = Guid.NewGuid(),
            RoomId = roomId,
            PlayerId = playerId,
            CommandType = commandType,
            CommandJson = commandJson,
            SubmittedAtTick = submittedAtTick,
            CreatedAt = DateTimeOffset.UtcNow,
            RejectedCode = rejectedCode
        });

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private static StoredMatchmakingEntry ToStored(MatchmakingQueueEntity entity)
    {
        return new StoredMatchmakingEntry(
            entity.PlayerId,
            entity.Status,
            entity.QueuedAt,
            entity.UpdatedAt,
            entity.MatchedRoomId);
    }

    private static StoredOnlineBattleRoom ToStored(OnlineBattleRoomEntity entity)
    {
        var snapshot = JsonSerializer.Deserialize<OnlineBattleSnapshot>(entity.SnapshotJson, JsonOptions)
            ?? throw new InvalidOperationException("Online battle snapshot could not be read.");
        return new StoredOnlineBattleRoom(
            entity.RoomId,
            entity.PlayerOneId,
            entity.PlayerTwoId,
            entity.Status,
            snapshot,
            entity.CreatedAt,
            entity.UpdatedAt,
            entity.EndedAt);
    }
}
