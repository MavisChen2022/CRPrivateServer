using Game.Domain;
using System.Security.Cryptography;

namespace Game.Application;

public sealed record GuestSessionResult(PlayerProfile Player, string RawSessionToken, bool WasCreated);

public sealed record StoredGuestSession(SessionToken Token, PlayerProfile Player);

public interface IGuestSessionStore
{
    Task<StoredGuestSession?> FindByTokenHashAsync(
        string tokenHash,
        DateTimeOffset now,
        CancellationToken cancellationToken);

    Task SaveGuestSessionAsync(
        SessionToken token,
        PlayerProfile player,
        CancellationToken cancellationToken);
}

public sealed class GuestSessionService
{
    private static readonly TimeSpan SessionLifetime = TimeSpan.FromDays(30);
    private readonly IGuestSessionStore _store;

    public GuestSessionService(IGuestSessionStore store)
    {
        _store = store;
    }

    public async Task<GuestSessionResult> GetOrCreateAsync(
        string? rawSessionToken,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(rawSessionToken))
        {
            var tokenHash = SessionToken.Hash(rawSessionToken);
            var existing = await _store.FindByTokenHashAsync(tokenHash, now, cancellationToken);
            if (existing is not null && existing.Token.IsValid(rawSessionToken, now))
            {
                return new GuestSessionResult(existing.Player, rawSessionToken, WasCreated: false);
            }
        }

        var userId = Guid.NewGuid();
        var token = CreateRawToken();
        var player = PlayerProfile.CreateGuest(userId, CreateGuestDisplayName());
        var session = SessionToken.Issue(userId, token, now, SessionLifetime);
        await _store.SaveGuestSessionAsync(session, player, cancellationToken);

        return new GuestSessionResult(player, token, WasCreated: true);
    }

    public async Task<PlayerProfile?> GetExistingPlayerAsync(
        string? rawSessionToken,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(rawSessionToken))
        {
            return null;
        }

        var tokenHash = SessionToken.Hash(rawSessionToken);
        var existing = await _store.FindByTokenHashAsync(tokenHash, now, cancellationToken);
        if (existing is null || !existing.Token.IsValid(rawSessionToken, now))
        {
            return null;
        }

        return existing.Player;
    }

    private static string CreateRawToken()
    {
        return Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
    }

    private static string CreateGuestDisplayName()
    {
        return $"Player{Random.Shared.Next(100000, 999999)}";
    }
}
