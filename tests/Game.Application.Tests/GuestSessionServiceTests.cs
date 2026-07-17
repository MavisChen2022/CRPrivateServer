using Game.Domain;

namespace Game.Application.Tests;

public sealed class GuestSessionServiceTests
{
    [Fact]
    public async Task GetOrCreateAsync_WhenNoToken_ShouldCreateGuest()
    {
        var store = new FakeGuestSessionStore();
        var service = new GuestSessionService(store);

        var result = await service.GetOrCreateAsync(null, DateTimeOffset.UtcNow, CancellationToken.None);

        Assert.True(result.WasCreated);
        Assert.Equal(AccountType.Guest, result.Player.AccountType);
        Assert.Single(store.SavedSessions);
        Assert.NotEqual(result.RawSessionToken, store.SavedSessions[0].Token.TokenHash);
    }

    [Fact]
    public async Task GetOrCreateAsync_WhenTokenIsValid_ShouldReturnExistingGuest()
    {
        var now = DateTimeOffset.UtcNow;
        var rawToken = "valid-token";
        var userId = Guid.NewGuid();
        var player = PlayerProfile.CreateGuest(userId, "Player123456");
        var token = SessionToken.Issue(userId, rawToken, now, TimeSpan.FromDays(30));
        var store = new FakeGuestSessionStore();
        await store.SaveGuestSessionAsync(token, player, CancellationToken.None);
        var service = new GuestSessionService(store);

        var result = await service.GetOrCreateAsync(rawToken, now, CancellationToken.None);

        Assert.False(result.WasCreated);
        Assert.Equal(player.PlayerId, result.Player.PlayerId);
        Assert.Equal(rawToken, result.RawSessionToken);
        Assert.Single(store.SavedSessions);
    }

    [Fact]
    public async Task GetOrCreateAsync_WhenTokenIsInvalid_ShouldCreateReplacementGuest()
    {
        var now = DateTimeOffset.UtcNow;
        var rawToken = "valid-token";
        var userId = Guid.NewGuid();
        var player = PlayerProfile.CreateGuest(userId, "Player123456");
        var token = SessionToken.Issue(userId, rawToken, now, TimeSpan.FromDays(30));
        var store = new FakeGuestSessionStore();
        await store.SaveGuestSessionAsync(token, player, CancellationToken.None);
        var service = new GuestSessionService(store);

        var result = await service.GetOrCreateAsync("tampered-token", now, CancellationToken.None);

        Assert.True(result.WasCreated);
        Assert.NotEqual(player.PlayerId, result.Player.PlayerId);
        Assert.Equal(2, store.SavedSessions.Count);
    }

    private sealed class FakeGuestSessionStore : IGuestSessionStore
    {
        public List<StoredGuestSession> SavedSessions { get; } = new();

        public Task<StoredGuestSession?> FindByTokenHashAsync(
            string tokenHash,
            DateTimeOffset now,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(SavedSessions.SingleOrDefault(x =>
                x.Token.TokenHash == tokenHash &&
                x.Token.ExpiresAt > now));
        }

        public Task SaveGuestSessionAsync(
            SessionToken token,
            PlayerProfile player,
            CancellationToken cancellationToken)
        {
            SavedSessions.Add(new StoredGuestSession(token, player));
            return Task.CompletedTask;
        }
    }
}
