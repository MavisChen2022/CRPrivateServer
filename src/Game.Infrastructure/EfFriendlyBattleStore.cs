using Game.Application;
using Microsoft.EntityFrameworkCore;

namespace Game.Infrastructure;

public sealed class EfFriendlyBattleStore : IFriendlyBattleStore
{
    private readonly RoyaleDbContext _dbContext;

    public EfFriendlyBattleStore(RoyaleDbContext dbContext)
    {
        _dbContext = dbContext;
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
        return entity is null
            ? null
            : new StoredFriendship(
                entity.FriendshipId,
                entity.RequesterPlayerId,
                entity.AddresseePlayerId,
                entity.LowerPlayerId,
                entity.HigherPlayerId,
                entity.Status,
                entity.CreatedAt,
                entity.UpdatedAt);
    }

    public async Task<StoredFriendlyBattleInvite?> FindPendingByPairAsync(
        Guid firstPlayerId,
        Guid secondPlayerId,
        CancellationToken cancellationToken)
    {
        var lower = firstPlayerId.CompareTo(secondPlayerId) <= 0 ? firstPlayerId : secondPlayerId;
        var higher = firstPlayerId.CompareTo(secondPlayerId) <= 0 ? secondPlayerId : firstPlayerId;
        var entity = await _dbContext.FriendlyBattleInvites.AsNoTracking()
            .SingleOrDefaultAsync(x =>
                x.LowerPlayerId == lower &&
                x.HigherPlayerId == higher &&
                x.Status == "Pending", cancellationToken);
        return entity is null ? null : ToStored(entity);
    }

    public async Task<StoredFriendlyBattleInvite?> FindInviteAsync(Guid inviteId, CancellationToken cancellationToken)
    {
        var entity = await _dbContext.FriendlyBattleInvites.AsNoTracking()
            .SingleOrDefaultAsync(x => x.InviteId == inviteId, cancellationToken);
        return entity is null ? null : ToStored(entity);
    }

    public async Task SaveInviteAsync(StoredFriendlyBattleInvite invite, CancellationToken cancellationToken)
    {
        var entity = await _dbContext.FriendlyBattleInvites
            .SingleOrDefaultAsync(x => x.InviteId == invite.InviteId, cancellationToken);

        if (entity is null)
        {
            _dbContext.FriendlyBattleInvites.Add(new FriendlyBattleInviteEntity
            {
                InviteId = invite.InviteId,
                RequesterPlayerId = invite.RequesterPlayerId,
                AddresseePlayerId = invite.AddresseePlayerId,
                LowerPlayerId = invite.LowerPlayerId,
                HigherPlayerId = invite.HigherPlayerId,
                Status = invite.Status,
                RoomId = invite.RoomId,
                CreatedAt = invite.CreatedAt,
                UpdatedAt = invite.UpdatedAt,
                ExpiresAt = invite.ExpiresAt
            });
        }
        else
        {
            entity.Status = invite.Status;
            entity.RoomId = invite.RoomId;
            entity.UpdatedAt = invite.UpdatedAt;
            entity.ExpiresAt = invite.ExpiresAt;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<StoredFriendlyBattleInvite>> ListInvitesAsync(
        Guid playerId,
        CancellationToken cancellationToken)
    {
        var entities = await _dbContext.FriendlyBattleInvites.AsNoTracking()
            .Where(x => x.RequesterPlayerId == playerId || x.AddresseePlayerId == playerId)
            .ToListAsync(cancellationToken);
        return entities.Select(ToStored).ToList();
    }

    private static StoredFriendlyBattleInvite ToStored(FriendlyBattleInviteEntity entity)
    {
        return new StoredFriendlyBattleInvite(
            entity.InviteId,
            entity.RequesterPlayerId,
            entity.AddresseePlayerId,
            entity.LowerPlayerId,
            entity.HigherPlayerId,
            entity.Status,
            entity.RoomId,
            entity.CreatedAt,
            entity.UpdatedAt,
            entity.ExpiresAt);
    }
}
