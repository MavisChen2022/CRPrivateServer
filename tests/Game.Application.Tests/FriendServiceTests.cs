using Game.Domain;

namespace Game.Application.Tests;

public sealed class FriendServiceTests
{
    [Fact]
    public async Task GetSnapshotAsync_WhenNoCodeExists_ShouldCreateStablePublicCode()
    {
        var player = Guid.NewGuid();
        var store = new FakeFriendStore();
        store.AddProfile(player, "Player100001");
        var service = new FriendService(store);

        var first = await service.GetSnapshotAsync(player, DateTimeOffset.UtcNow, CancellationToken.None);
        var second = await service.GetSnapshotAsync(player, DateTimeOffset.UtcNow, CancellationToken.None);

        Assert.Equal(8, first.FriendCode.Length);
        Assert.Equal(first.FriendCode, second.FriendCode);
        Assert.Empty(first.Friends);
    }

    [Fact]
    public async Task CreateRequestAsync_WhenTargetIsSelf_ShouldReject()
    {
        var player = Guid.NewGuid();
        var store = new FakeFriendStore();
        store.AddProfile(player, "Player100001");
        var service = new FriendService(store);
        var snapshot = await service.GetSnapshotAsync(player, DateTimeOffset.UtcNow, CancellationToken.None);

        var result = await service.CreateRequestAsync(
            player,
            snapshot.FriendCode,
            DateTimeOffset.UtcNow,
            CancellationToken.None);

        Assert.False(result.Succeeded);
        Assert.Equal("CannotAddSelf", result.ErrorCode);
        Assert.Empty(store.Friendships);
    }

    [Fact]
    public async Task CreateRequestAsync_WhenDuplicate_ShouldRejectWithoutSecondRow()
    {
        var requester = Guid.NewGuid();
        var addressee = Guid.NewGuid();
        var store = CreateStoreWithProfiles(requester, addressee);
        var service = new FriendService(store);
        var target = await service.GetSnapshotAsync(addressee, DateTimeOffset.UtcNow, CancellationToken.None);

        var first = await service.CreateRequestAsync(requester, target.FriendCode, DateTimeOffset.UtcNow, CancellationToken.None);
        var second = await service.CreateRequestAsync(requester, target.FriendCode, DateTimeOffset.UtcNow, CancellationToken.None);

        Assert.True(first.Succeeded);
        Assert.False(second.Succeeded);
        Assert.Equal("DuplicateFriend", second.ErrorCode);
        Assert.Single(store.Friendships);
    }

    [Fact]
    public async Task AcceptAsync_WhenIncomingRequest_ShouldShowPublicFriendRowsForBothPlayers()
    {
        var requester = Guid.NewGuid();
        var addressee = Guid.NewGuid();
        var store = CreateStoreWithProfiles(requester, addressee);
        var service = new FriendService(store);
        var target = await service.GetSnapshotAsync(addressee, DateTimeOffset.UtcNow, CancellationToken.None);
        await service.CreateRequestAsync(requester, target.FriendCode, DateTimeOffset.UtcNow, CancellationToken.None);
        var incoming = Assert.Single((await service.GetSnapshotAsync(addressee, DateTimeOffset.UtcNow, CancellationToken.None)).IncomingRequests);

        var accepted = await service.AcceptAsync(addressee, incoming.FriendshipId, DateTimeOffset.UtcNow, CancellationToken.None);
        var requesterSnapshot = await service.GetSnapshotAsync(requester, DateTimeOffset.UtcNow, CancellationToken.None);

        Assert.True(accepted.Succeeded);
        var addresseeFriend = Assert.Single(accepted.Snapshot!.Friends);
        var requesterFriend = Assert.Single(requesterSnapshot.Friends);
        Assert.Equal(requester, addresseeFriend.PlayerId);
        Assert.Equal(addressee, requesterFriend.PlayerId);
        Assert.Equal("Friend", requesterFriend.Status);
        Assert.Equal(8, requesterFriend.ShortPlayerId.Length);
    }

    [Fact]
    public async Task AcceptAsync_WhenRequesterOrUnrelatedPlayerActs_ShouldReturnForbidden()
    {
        var requester = Guid.NewGuid();
        var addressee = Guid.NewGuid();
        var unrelated = Guid.NewGuid();
        var store = CreateStoreWithProfiles(requester, addressee, unrelated);
        var service = new FriendService(store);
        var target = await service.GetSnapshotAsync(addressee, DateTimeOffset.UtcNow, CancellationToken.None);
        await service.CreateRequestAsync(requester, target.FriendCode, DateTimeOffset.UtcNow, CancellationToken.None);
        var outgoing = Assert.Single((await service.GetSnapshotAsync(requester, DateTimeOffset.UtcNow, CancellationToken.None)).OutgoingRequests);

        var requesterResult = await service.AcceptAsync(requester, outgoing.FriendshipId, DateTimeOffset.UtcNow, CancellationToken.None);
        var unrelatedResult = await service.AcceptAsync(unrelated, outgoing.FriendshipId, DateTimeOffset.UtcNow, CancellationToken.None);

        Assert.False(requesterResult.Succeeded);
        Assert.False(unrelatedResult.Succeeded);
        Assert.Equal("FriendForbidden", requesterResult.ErrorCode);
        Assert.Equal("FriendForbidden", unrelatedResult.ErrorCode);
    }

    [Fact]
    public async Task RejectAsync_WhenIncomingRequest_ShouldRemovePendingWithoutCreatingFriend()
    {
        var requester = Guid.NewGuid();
        var addressee = Guid.NewGuid();
        var store = CreateStoreWithProfiles(requester, addressee);
        var service = new FriendService(store);
        var target = await service.GetSnapshotAsync(addressee, DateTimeOffset.UtcNow, CancellationToken.None);
        await service.CreateRequestAsync(requester, target.FriendCode, DateTimeOffset.UtcNow, CancellationToken.None);
        var incoming = Assert.Single((await service.GetSnapshotAsync(addressee, DateTimeOffset.UtcNow, CancellationToken.None)).IncomingRequests);

        var rejected = await service.RejectAsync(addressee, incoming.FriendshipId, DateTimeOffset.UtcNow, CancellationToken.None);
        var requesterSnapshot = await service.GetSnapshotAsync(requester, DateTimeOffset.UtcNow, CancellationToken.None);

        Assert.True(rejected.Succeeded);
        Assert.Empty(rejected.Snapshot!.IncomingRequests);
        Assert.Empty(rejected.Snapshot.Friends);
        Assert.Empty(requesterSnapshot.OutgoingRequests);
        Assert.Empty(requesterSnapshot.Friends);
    }

    private static FakeFriendStore CreateStoreWithProfiles(params Guid[] players)
    {
        var store = new FakeFriendStore();
        for (var index = 0; index < players.Length; index++)
        {
            store.AddProfile(players[index], $"Player10000{index + 1}");
        }

        return store;
    }

    private sealed class FakeFriendStore : IFriendStore
    {
        private readonly List<FriendCodeRecord> _codes = new();
        private readonly List<StoredFriendProfile> _profiles = new();
        private readonly List<StoredFriendship> _friendships = new();

        public IReadOnlyList<StoredFriendship> Friendships => _friendships;

        public void AddProfile(Guid playerId, string displayName)
        {
            _profiles.Add(new StoredFriendProfile(playerId, displayName, 0));
        }

        public Task<FriendCodeRecord?> FindCodeByPlayerAsync(Guid playerId, CancellationToken cancellationToken)
        {
            return Task.FromResult(_codes.SingleOrDefault(x => x.PlayerId == playerId));
        }

        public Task<FriendCodeRecord?> FindCodeAsync(string code, CancellationToken cancellationToken)
        {
            return Task.FromResult(_codes.SingleOrDefault(x => x.Code == code));
        }

        public Task SaveFriendCodeAsync(FriendCodeRecord code, CancellationToken cancellationToken)
        {
            _codes.RemoveAll(x => x.PlayerId == code.PlayerId);
            _codes.Add(code);
            return Task.CompletedTask;
        }

        public Task<StoredFriendProfile?> FindProfileAsync(Guid playerId, CancellationToken cancellationToken)
        {
            return Task.FromResult(_profiles.SingleOrDefault(x => x.PlayerId == playerId));
        }

        public Task<StoredFriendship?> FindRelationAsync(Guid firstPlayerId, Guid secondPlayerId, CancellationToken cancellationToken)
        {
            var pair = FriendshipPair.Create(firstPlayerId, secondPlayerId);
            return Task.FromResult(_friendships.SingleOrDefault(x =>
                x.LowerPlayerId == pair.LowerPlayerId &&
                x.HigherPlayerId == pair.HigherPlayerId));
        }

        public Task<StoredFriendship?> FindFriendshipAsync(Guid friendshipId, CancellationToken cancellationToken)
        {
            return Task.FromResult(_friendships.SingleOrDefault(x => x.FriendshipId == friendshipId));
        }

        public Task SaveFriendshipAsync(StoredFriendship friendship, CancellationToken cancellationToken)
        {
            _friendships.RemoveAll(x => x.FriendshipId == friendship.FriendshipId);
            _friendships.Add(friendship);
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<StoredFriendship>> ListRelationsAsync(Guid playerId, CancellationToken cancellationToken)
        {
            IReadOnlyList<StoredFriendship> result = _friendships
                .Where(x => x.RequesterPlayerId == playerId || x.AddresseePlayerId == playerId)
                .ToList();
            return Task.FromResult(result);
        }
    }
}
