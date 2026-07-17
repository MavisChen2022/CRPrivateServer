using Game.Application;
using Game.Domain;
using Microsoft.EntityFrameworkCore;

namespace Game.Infrastructure;

public sealed class EfGuestSessionStore : IGuestSessionStore
{
    private readonly RoyaleDbContext _dbContext;

    public EfGuestSessionStore(RoyaleDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<StoredGuestSession?> FindByTokenHashAsync(
        string tokenHash,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var session = await _dbContext.SessionTokens
            .AsNoTracking()
            .Where(x => x.TokenHash == tokenHash && x.RevokedAt == null)
            .SingleOrDefaultAsync(cancellationToken);

        if (session is null)
        {
            return null;
        }

        var profile = await _dbContext.PlayerProfiles
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.UserId == session.UserId, cancellationToken);

        if (profile is null)
        {
            return null;
        }

        var account = await _dbContext.UserAccounts
            .AsNoTracking()
            .SingleAsync(x => x.UserId == session.UserId, cancellationToken);

        return new StoredGuestSession(
            new SessionToken(session.TokenId, session.UserId, session.TokenHash, session.ExpiresAt),
            new PlayerProfile(
                profile.PlayerId,
                profile.UserId,
                profile.DisplayName,
                profile.Trophies,
                profile.Gold,
                Enum.Parse<AccountType>(account.AccountType)));
    }

    public async Task SaveGuestSessionAsync(
        SessionToken token,
        PlayerProfile player,
        CancellationToken cancellationToken)
    {
        await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

        _dbContext.UserAccounts.Add(new UserAccountEntity
        {
            UserId = player.UserId,
            AccountType = player.AccountType.ToString(),
            Status = "Active",
            CreatedAt = DateTimeOffset.UtcNow
        });

        _dbContext.PlayerProfiles.Add(new PlayerProfileEntity
        {
            PlayerId = player.PlayerId,
            UserId = player.UserId,
            DisplayName = player.DisplayName,
            Trophies = player.Trophies,
            Gold = player.Gold
        });

        _dbContext.SessionTokens.Add(new SessionTokenEntity
        {
            TokenId = token.TokenId,
            UserId = token.UserId,
            TokenHash = token.TokenHash,
            CreatedAt = DateTimeOffset.UtcNow,
            ExpiresAt = token.ExpiresAt
        });

        await _dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }
}
