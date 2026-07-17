using Game.Domain;
using System.Collections.Concurrent;

namespace Game.Application;

public sealed record GuestSessionResult(PlayerProfile Player, string RawSessionToken, bool WasCreated);

public sealed class GuestSessionService
{
    private static readonly TimeSpan SessionLifetime = TimeSpan.FromDays(30);
    private readonly ConcurrentDictionary<string, (SessionToken Token, PlayerProfile Player)> _sessions = new();

    public GuestSessionResult GetOrCreate(string? rawSessionToken, DateTimeOffset now)
    {
        if (!string.IsNullOrWhiteSpace(rawSessionToken))
        {
            var tokenHash = SessionToken.Hash(rawSessionToken);
            if (_sessions.TryGetValue(tokenHash, out var existing) &&
                existing.Token.IsValid(rawSessionToken, now))
            {
                return new GuestSessionResult(existing.Player, rawSessionToken, WasCreated: false);
            }
        }

        var userId = Guid.NewGuid();
        var token = CreateRawToken();
        var player = PlayerProfile.CreateGuest(userId, CreateGuestDisplayName());
        var session = SessionToken.Issue(userId, token, now, SessionLifetime);
        _sessions[session.TokenHash] = (session, player);
        return new GuestSessionResult(player, token, WasCreated: true);
    }

    private static string CreateRawToken()
    {
        return Convert.ToBase64String(Guid.NewGuid().ToByteArray()) +
            Convert.ToBase64String(Guid.NewGuid().ToByteArray());
    }

    private static string CreateGuestDisplayName()
    {
        return $"Player{Random.Shared.Next(100000, 999999)}";
    }
}

