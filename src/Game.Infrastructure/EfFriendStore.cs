using Game.Application;
using Microsoft.EntityFrameworkCore;

namespace Game.Infrastructure;

public sealed class EfFriendStore : IFriendStore
{
    private readonly RoyaleDbContext _dbContext;

    public EfFriendStore(RoyaleDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<FriendCodeRecord?> FindCodeByPlayerAsync(Guid playerId, CancellationToken cancellationToken)
    {
        var entity = await _dbContext.FriendCodes.AsNoTracking()
            .SingleOrDefaultAsync(x => x.PlayerId == playerId, cancellationToken);
        return entity is null ? null : new FriendCodeRecord(entity.PlayerId, entity.Code, entity.CreatedAt);
    }

    public async Task<FriendCodeRecord?> FindCodeAsync(string code, CancellationToken cancellationToken)
    {
        var entity = await _dbContext.FriendCodes.AsNoTracking()
            .SingleOrDefaultAsync(x => x.Code == code, cancellationToken);
        return entity is null ? null : new FriendCodeRecord(entity.PlayerId, entity.Code, entity.CreatedAt);
    }

    public async Task SaveFriendCodeAsync(FriendCodeRecord code, CancellationToken cancellationToken)
    {
        _dbContext.FriendCodes.Add(new FriendCodeEntity
        {
            PlayerId = code.PlayerId,
            Code = code.Code,
            CreatedAt = code.CreatedAt
        });
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<StoredFriendProfile?> FindProfileAsync(Guid playerId, CancellationToken cancellationToken)
    {
        var profile = await _dbContext.PlayerProfiles.AsNoTracking()
            .SingleOrDefaultAsync(x => x.PlayerId == playerId, cancellationToken);
        return profile is null
            ? null
            : new StoredFriendProfile(profile.PlayerId, profile.DisplayName, profile.Trophies);
    }

    public async Task<StoredFriendship?> FindRelationAsync(
        Guid firstPlayerId,
        Guid secondPlayerId,
        CancellationToken cancellationToken)
    {
        var lower = firstPlayerId.CompareTo(secondPlayerId) <= 0 ? firstPlayerId : secondPlayerId;
        var higher = firstPlayerId.CompareTo(secondPlayerId) <= 0 ? secondPlayerId : firstPlayerId;
        var entity = await _dbContext.Friendships.AsNoTracking()
            .SingleOrDefaultAsync(x => x.LowerPlayerId == lower && x.HigherPlayerId == higher, cancellationToken);
        return entity is null ? null : ToStored(entity);
    }

    public async Task<StoredFriendship?> FindFriendshipAsync(Guid friendshipId, CancellationToken cancellationToken)
    {
        var entity = await _dbContext.Friendships.AsNoTracking()
            .SingleOrDefaultAsync(x => x.FriendshipId == friendshipId, cancellationToken);
        return entity is null ? null : ToStored(entity);
    }

    public async Task SaveFriendshipAsync(StoredFriendship friendship, CancellationToken cancellationToken)
    {
        var existing = await _dbContext.Friendships
            .SingleOrDefaultAsync(x => x.FriendshipId == friendship.FriendshipId, cancellationToken);

        if (existing is null)
        {
            _dbContext.Friendships.Add(new FriendshipEntity
            {
                FriendshipId = friendship.FriendshipId,
                RequesterPlayerId = friendship.RequesterPlayerId,
                AddresseePlayerId = friendship.AddresseePlayerId,
                LowerPlayerId = friendship.LowerPlayerId,
                HigherPlayerId = friendship.HigherPlayerId,
                Status = friendship.Status,
                CreatedAt = friendship.CreatedAt,
                UpdatedAt = friendship.UpdatedAt
            });
        }
        else
        {
            existing.RequesterPlayerId = friendship.RequesterPlayerId;
            existing.AddresseePlayerId = friendship.AddresseePlayerId;
            existing.LowerPlayerId = friendship.LowerPlayerId;
            existing.HigherPlayerId = friendship.HigherPlayerId;
            existing.Status = friendship.Status;
            existing.UpdatedAt = friendship.UpdatedAt;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<StoredFriendship>> ListRelationsAsync(
        Guid playerId,
        CancellationToken cancellationToken)
    {
        var entities = await _dbContext.Friendships.AsNoTracking()
            .Where(x => x.RequesterPlayerId == playerId || x.AddresseePlayerId == playerId)
            .ToListAsync(cancellationToken);
        return entities.Select(ToStored).ToList();
    }

    private static StoredFriendship ToStored(FriendshipEntity entity)
    {
        return new StoredFriendship(
            entity.FriendshipId,
            entity.RequesterPlayerId,
            entity.AddresseePlayerId,
            entity.LowerPlayerId,
            entity.HigherPlayerId,
            entity.Status,
            entity.CreatedAt,
            entity.UpdatedAt);
    }
}

