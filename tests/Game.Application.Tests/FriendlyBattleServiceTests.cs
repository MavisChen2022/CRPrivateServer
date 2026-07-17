using Game.Domain;

namespace Game.Application.Tests;

public sealed class FriendlyBattleServiceTests
{
    private static readonly DateTimeOffset Now = new(2026, 7, 18, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task CreateInviteAsync_WhenAcceptedFriends_ShouldCreateOutgoingAndIncomingInvite()
    {
        var requester = Guid.NewGuid();
        var addressee = Guid.NewGuid();
        var store = CreateStoreWithAcceptedFriends(requester, addressee);
        var service = CreateService(store);

        var result = await service.CreateInviteAsync(requester, addressee, Now, CancellationToken.None);
        var recipientSnapshot = await service.GetSnapshotAsync(addressee, Now, CancellationToken.None);

        Assert.True(result.Succeeded);
        Assert.Single(result.Snapshot!.OutgoingInvites);
        var incoming = Assert.Single(recipientSnapshot.IncomingInvites);
        Assert.Equal(requester, incoming.RequesterPlayerId);
        Assert.Equal("Pending", incoming.Status);
    }

    [Fact]
    public async Task CreateInviteAsync_WhenNotFriendsOrSelf_ShouldReject()
    {
        var requester = Guid.NewGuid();
        var addressee = Guid.NewGuid();
        var store = new FakeFriendlyBattleStore();
        store.AddProfile(requester, "Player100001");
        store.AddProfile(addressee, "Player100002");
        var service = CreateService(store);

        var notFriends = await service.CreateInviteAsync(requester, addressee, Now, CancellationToken.None);
        var self = await service.CreateInviteAsync(requester, requester, Now, CancellationToken.None);

        Assert.False(notFriends.Succeeded);
        Assert.Equal("FriendlyBattleNotFriends", notFriends.ErrorCode);
        Assert.False(self.Succeeded);
        Assert.Equal("FriendlyBattleSelfInvite", self.ErrorCode);
        Assert.Empty(store.Invites);
    }

    [Fact]
    public async Task CreateInviteAsync_WhenDuplicatePending_ShouldRejectWithoutSecondInvite()
    {
        var requester = Guid.NewGuid();
        var addressee = Guid.NewGuid();
        var store = CreateStoreWithAcceptedFriends(requester, addressee);
        var service = CreateService(store);

        var first = await service.CreateInviteAsync(requester, addressee, Now, CancellationToken.None);
        var second = await service.CreateInviteAsync(requester, addressee, Now, CancellationToken.None);

        Assert.True(first.Succeeded);
        Assert.False(second.Succeeded);
        Assert.Equal("FriendlyBattleDuplicateInvite", second.ErrorCode);
        Assert.Single(store.Invites);
    }

    [Fact]
    public async Task CancelAsync_WhenRequesterCancels_ShouldResolveInvite()
    {
        var requester = Guid.NewGuid();
        var addressee = Guid.NewGuid();
        var store = CreateStoreWithAcceptedFriends(requester, addressee);
        var service = CreateService(store);
        await service.CreateInviteAsync(requester, addressee, Now, CancellationToken.None);
        var invite = Assert.Single(store.Invites);

        var recipientCancel = await service.CancelAsync(addressee, invite.InviteId, Now, CancellationToken.None);
        var requesterCancel = await service.CancelAsync(requester, invite.InviteId, Now, CancellationToken.None);
        var acceptAfterCancel = await service.AcceptAsync(addressee, invite.InviteId, Now, CancellationToken.None);

        Assert.False(recipientCancel.Succeeded);
        Assert.Equal("FriendlyBattleForbidden", recipientCancel.ErrorCode);
        Assert.True(requesterCancel.Succeeded);
        Assert.Empty(requesterCancel.Snapshot!.OutgoingInvites);
        Assert.False(acceptAfterCancel.Succeeded);
        Assert.Equal("FriendlyBattleAlreadyResolved", acceptAfterCancel.ErrorCode);
    }

    [Fact]
    public async Task RejectAsync_WhenRecipientRejects_ShouldResolveInvite()
    {
        var requester = Guid.NewGuid();
        var addressee = Guid.NewGuid();
        var store = CreateStoreWithAcceptedFriends(requester, addressee);
        var service = CreateService(store);
        await service.CreateInviteAsync(requester, addressee, Now, CancellationToken.None);
        var invite = Assert.Single(store.Invites);

        var requesterReject = await service.RejectAsync(requester, invite.InviteId, Now, CancellationToken.None);
        var recipientReject = await service.RejectAsync(addressee, invite.InviteId, Now, CancellationToken.None);

        Assert.False(requesterReject.Succeeded);
        Assert.Equal("FriendlyBattleForbidden", requesterReject.ErrorCode);
        Assert.True(recipientReject.Succeeded);
        Assert.Empty(recipientReject.Snapshot!.IncomingInvites);
    }

    [Fact]
    public async Task AcceptAsync_WhenRecipientAccepts_ShouldCreateOnlineRoom()
    {
        var requester = Guid.NewGuid();
        var addressee = Guid.NewGuid();
        var store = CreateStoreWithAcceptedFriends(requester, addressee);
        var service = CreateService(store);
        await service.CreateInviteAsync(requester, addressee, Now, CancellationToken.None);
        var invite = Assert.Single(store.Invites);

        var requesterAccept = await service.AcceptAsync(requester, invite.InviteId, Now, CancellationToken.None);
        var recipientAccept = await service.AcceptAsync(addressee, invite.InviteId, Now, CancellationToken.None);
        var requesterSnapshot = await service.GetSnapshotAsync(requester, Now, CancellationToken.None);

        Assert.False(requesterAccept.Succeeded);
        Assert.Equal("FriendlyBattleForbidden", requesterAccept.ErrorCode);
        Assert.True(recipientAccept.Succeeded);
        Assert.NotNull(recipientAccept.Snapshot!.ActiveRoom);
        Assert.NotNull(requesterSnapshot.ActiveRoom);
        Assert.Equal(recipientAccept.Snapshot.ActiveRoom!.RoomId, requesterSnapshot.ActiveRoom!.RoomId);
        Assert.Equal(0, store.Profile(requester).Trophies);
        Assert.Equal(0, store.Profile(addressee).Trophies);
    }

    [Fact]
    public async Task AcceptAsync_WhenInviteExpired_ShouldReject()
    {
        var requester = Guid.NewGuid();
        var addressee = Guid.NewGuid();
        var store = CreateStoreWithAcceptedFriends(requester, addressee);
        var service = CreateService(store);
        await service.CreateInviteAsync(requester, addressee, Now, CancellationToken.None);
        var invite = Assert.Single(store.Invites);

        var result = await service.AcceptAsync(addressee, invite.InviteId, Now.AddMinutes(11), CancellationToken.None);

        Assert.False(result.Succeeded);
        Assert.Equal("FriendlyBattleInviteExpired", result.ErrorCode);
        Assert.Equal("Expired", Assert.Single(store.Invites).Status);
    }

    private static FriendlyBattleService CreateService(FakeFriendlyBattleStore store)
    {
        return new FriendlyBattleService(
            store,
            new OnlineBattleService(store, new OnlineBattleEngine()));
    }

    private static FakeFriendlyBattleStore CreateStoreWithAcceptedFriends(Guid first, Guid second)
    {
        var store = new FakeFriendlyBattleStore();
        store.AddProfile(first, "Player100001");
        store.AddProfile(second, "Player100002");
        var pair = FriendshipPair.Create(first, second);
        store.Friendships.Add(new StoredFriendship(
            Guid.NewGuid(),
            first,
            second,
            pair.LowerPlayerId,
            pair.HigherPlayerId,
            "Accepted",
            Now,
            Now));
        return store;
    }

    private sealed class FakeFriendlyBattleStore : IFriendlyBattleStore, IOnlineBattleStore
    {
        private readonly List<StoredFriendProfile> _profiles = new();
        private readonly List<StoredFriendlyBattleInvite> _invites = new();
        private readonly List<StoredOnlineBattleRoom> _rooms = new();

        public List<StoredFriendship> Friendships { get; } = new();

        public IReadOnlyList<StoredFriendlyBattleInvite> Invites => _invites;

        public void AddProfile(Guid playerId, string displayName)
        {
            _profiles.Add(new StoredFriendProfile(playerId, displayName, 0));
        }

        public StoredFriendProfile Profile(Guid playerId)
        {
            return _profiles.Single(x => x.PlayerId == playerId);
        }

        public Task<StoredFriendProfile?> FindProfileAsync(Guid playerId, CancellationToken cancellationToken)
        {
            return Task.FromResult(_profiles.SingleOrDefault(x => x.PlayerId == playerId));
        }

        public Task<StoredFriendship?> FindRelationAsync(Guid firstPlayerId, Guid secondPlayerId, CancellationToken cancellationToken)
        {
            var pair = FriendshipPair.Create(firstPlayerId, secondPlayerId);
            return Task.FromResult(Friendships.SingleOrDefault(x =>
                x.LowerPlayerId == pair.LowerPlayerId &&
                x.HigherPlayerId == pair.HigherPlayerId));
        }

        public Task<StoredFriendlyBattleInvite?> FindPendingByPairAsync(Guid firstPlayerId, Guid secondPlayerId, CancellationToken cancellationToken)
        {
            var pair = FriendshipPair.Create(firstPlayerId, secondPlayerId);
            return Task.FromResult(_invites.SingleOrDefault(x =>
                x.LowerPlayerId == pair.LowerPlayerId &&
                x.HigherPlayerId == pair.HigherPlayerId &&
                x.Status == "Pending"));
        }

        public Task<StoredFriendlyBattleInvite?> FindInviteAsync(Guid inviteId, CancellationToken cancellationToken)
        {
            return Task.FromResult(_invites.SingleOrDefault(x => x.InviteId == inviteId));
        }

        public Task SaveInviteAsync(StoredFriendlyBattleInvite invite, CancellationToken cancellationToken)
        {
            _invites.RemoveAll(x => x.InviteId == invite.InviteId);
            _invites.Add(invite);
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<StoredFriendlyBattleInvite>> ListInvitesAsync(Guid playerId, CancellationToken cancellationToken)
        {
            IReadOnlyList<StoredFriendlyBattleInvite> result = _invites
                .Where(x => x.RequesterPlayerId == playerId || x.AddresseePlayerId == playerId)
                .ToList();
            return Task.FromResult(result);
        }

        public Task<StoredMatchmakingEntry?> FindWaitingByPlayerAsync(Guid playerId, CancellationToken cancellationToken)
        {
            return Task.FromResult<StoredMatchmakingEntry?>(null);
        }

        public Task<StoredMatchmakingEntry?> FindFirstWaitingExceptAsync(Guid playerId, CancellationToken cancellationToken)
        {
            return Task.FromResult<StoredMatchmakingEntry?>(null);
        }

        public Task SaveQueueEntryAsync(StoredMatchmakingEntry entry, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        public Task<StoredOnlineBattleRoom?> FindActiveByPlayerAsync(Guid playerId, CancellationToken cancellationToken)
        {
            return Task.FromResult(_rooms.SingleOrDefault(x =>
                x.Status == "Active" &&
                (x.PlayerOneId == playerId || x.PlayerTwoId == playerId)));
        }

        public Task<StoredOnlineBattleRoom?> FindByIdAsync(Guid roomId, CancellationToken cancellationToken)
        {
            return Task.FromResult(_rooms.SingleOrDefault(x => x.RoomId == roomId));
        }

        public Task SaveRoomAsync(StoredOnlineBattleRoom room, CancellationToken cancellationToken)
        {
            _rooms.RemoveAll(x => x.RoomId == room.RoomId);
            _rooms.Add(room);
            return Task.CompletedTask;
        }

        public Task AppendCommandAsync(
            Guid roomId,
            Guid playerId,
            string commandType,
            string commandJson,
            int submittedAtTick,
            string? rejectedCode,
            CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}
