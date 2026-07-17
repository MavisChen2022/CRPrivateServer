using Game.Domain;

namespace Game.Application.Tests;

public sealed class OnlineBattleServiceTests
{
    [Fact]
    public async Task QueueAsync_WhenNoOpponent_ShouldReturnWaitingAndPreventDuplicateEntry()
    {
        var store = new FakeOnlineBattleStore();
        var player = Guid.NewGuid();
        store.AddProfile(player, "Player100001");
        var service = new OnlineBattleService(store, new OnlineBattleEngine());

        var first = await service.QueueAsync(player, "Player100001", DateTimeOffset.UtcNow, CancellationToken.None);
        var second = await service.QueueAsync(player, "Player100001", DateTimeOffset.UtcNow, CancellationToken.None);

        Assert.True(first.Succeeded);
        Assert.True(second.Succeeded);
        Assert.Equal("Waiting", second.State!.Status);
        Assert.Single(store.QueueEntries);
    }

    [Fact]
    public async Task CancelQueueAsync_WhenWaiting_ShouldReturnIdle()
    {
        var store = new FakeOnlineBattleStore();
        var player = Guid.NewGuid();
        store.AddProfile(player, "Player100001");
        var service = new OnlineBattleService(store, new OnlineBattleEngine());
        await service.QueueAsync(player, "Player100001", DateTimeOffset.UtcNow, CancellationToken.None);

        var result = await service.CancelQueueAsync(player, DateTimeOffset.UtcNow, CancellationToken.None);

        Assert.True(result.Succeeded);
        Assert.Equal("Idle", result.State!.Status);
        Assert.Empty(await store.WaitingPlayersAsync());
    }

    [Fact]
    public async Task QueueAsync_WhenSecondPlayerQueues_ShouldCreateOneActiveRoom()
    {
        var store = CreateStoreWithProfiles(out var playerOne, out var playerTwo);
        var service = new OnlineBattleService(store, new OnlineBattleEngine());
        await service.QueueAsync(playerOne, "Player100001", DateTimeOffset.UtcNow, CancellationToken.None);

        var matched = await service.QueueAsync(playerTwo, "Player100002", DateTimeOffset.UtcNow, CancellationToken.None);
        var currentOne = await service.GetCurrentAsync(playerOne, CancellationToken.None);

        Assert.True(matched.Succeeded);
        Assert.Equal("Active", matched.State!.Status);
        Assert.Equal(matched.State.RoomId, currentOne.State!.RoomId);
        Assert.Single(store.Rooms);
    }

    [Fact]
    public async Task QueueAsync_WhenQueuedOpponentAlreadyHasActiveRoom_ShouldReturnConflictWithoutMarkingMatch()
    {
        var store = CreateStoreWithProfiles(out var playerOne, out var playerTwo);
        var third = Guid.NewGuid();
        store.AddProfile(third, "Player100003");
        var service = new OnlineBattleService(store, new OnlineBattleEngine());
        await service.QueueAsync(playerOne, "Player100001", DateTimeOffset.UtcNow, CancellationToken.None);
        store.AddActiveRoom(playerOne, third);

        var matched = await service.QueueAsync(playerTwo, "Player100002", DateTimeOffset.UtcNow, CancellationToken.None);

        Assert.False(matched.Succeeded);
        Assert.Equal("PlayerAlreadyInBattle", matched.ErrorCode);
        Assert.DoesNotContain(store.QueueEntries, x => x.PlayerId == playerOne && x.Status == "Matched");
    }

    [Fact]
    public async Task SubmitDeployAsync_WhenParticipantActs_ShouldUpdateSharedSnapshot()
    {
        var store = CreateStoreWithProfiles(out var playerOne, out var playerTwo);
        var service = new OnlineBattleService(store, new OnlineBattleEngine());
        await service.QueueAsync(playerOne, "Player100001", DateTimeOffset.UtcNow, CancellationToken.None);
        var matched = await service.QueueAsync(playerTwo, "Player100002", DateTimeOffset.UtcNow, CancellationToken.None);

        var result = await service.SubmitDeployAsync(
            playerOne,
            matched.State!.RoomId!.Value,
            new DeployBattleCommand("training-knight", "center", 0.5, 0.72),
            DateTimeOffset.UtcNow,
            CancellationToken.None);

        Assert.True(result.Succeeded);
        Assert.Single(result.State!.Snapshot!.Units);
        Assert.Equal(2, result.State.Snapshot.PlayerOne.Elixir);
    }

    [Fact]
    public async Task SubmitDeployAsync_WhenNonParticipantActs_ShouldReturnForbidden()
    {
        var store = CreateStoreWithProfiles(out var playerOne, out var playerTwo);
        var unrelated = Guid.NewGuid();
        store.AddProfile(unrelated, "Player100003");
        var service = new OnlineBattleService(store, new OnlineBattleEngine());
        await service.QueueAsync(playerOne, "Player100001", DateTimeOffset.UtcNow, CancellationToken.None);
        var matched = await service.QueueAsync(playerTwo, "Player100002", DateTimeOffset.UtcNow, CancellationToken.None);

        var result = await service.SubmitDeployAsync(
            unrelated,
            matched.State!.RoomId!.Value,
            new DeployBattleCommand("training-knight", "center", 0.5, 0.72),
            DateTimeOffset.UtcNow,
            CancellationToken.None);

        Assert.False(result.Succeeded);
        Assert.Equal("OnlineBattleForbidden", result.ErrorCode);
    }

    private static FakeOnlineBattleStore CreateStoreWithProfiles(out Guid playerOne, out Guid playerTwo)
    {
        var store = new FakeOnlineBattleStore();
        playerOne = Guid.NewGuid();
        playerTwo = Guid.NewGuid();
        store.AddProfile(playerOne, "Player100001");
        store.AddProfile(playerTwo, "Player100002");
        return store;
    }

    private sealed class FakeOnlineBattleStore : IOnlineBattleStore
    {
        private readonly List<StoredMatchmakingEntry> _queue = new();
        private readonly List<StoredFriendProfile> _profiles = new();
        private readonly List<StoredOnlineBattleRoom> _rooms = new();

        public IReadOnlyList<StoredMatchmakingEntry> QueueEntries => _queue;
        public IReadOnlyList<StoredOnlineBattleRoom> Rooms => _rooms;

        public void AddProfile(Guid playerId, string displayName)
        {
            _profiles.Add(new StoredFriendProfile(playerId, displayName, 0));
        }

        public void AddActiveRoom(Guid playerOneId, Guid playerTwoId)
        {
            var snapshot = new OnlineBattleEngine().Start(playerOneId, "Player100001", playerTwoId, "Player100003");
            _rooms.Add(new StoredOnlineBattleRoom(
                snapshot.RoomId,
                snapshot.PlayerOne.PlayerId,
                snapshot.PlayerTwo.PlayerId,
                snapshot.Status,
                snapshot,
                DateTimeOffset.UtcNow,
                DateTimeOffset.UtcNow,
                null));
        }

        public Task<IReadOnlyList<Guid>> WaitingPlayersAsync()
        {
            IReadOnlyList<Guid> players = _queue.Where(x => x.Status == "Queued").Select(x => x.PlayerId).ToList();
            return Task.FromResult(players);
        }

        public Task<StoredMatchmakingEntry?> FindWaitingByPlayerAsync(Guid playerId, CancellationToken cancellationToken)
        {
            return Task.FromResult(_queue.SingleOrDefault(x => x.PlayerId == playerId && x.Status == "Queued"));
        }

        public Task<StoredMatchmakingEntry?> FindFirstWaitingExceptAsync(Guid playerId, CancellationToken cancellationToken)
        {
            return Task.FromResult(_queue.Where(x => x.PlayerId != playerId && x.Status == "Queued").OrderBy(x => x.QueuedAt).FirstOrDefault());
        }

        public Task SaveQueueEntryAsync(StoredMatchmakingEntry entry, CancellationToken cancellationToken)
        {
            _queue.RemoveAll(x => x.PlayerId == entry.PlayerId);
            _queue.Add(entry);
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

        public Task<StoredFriendProfile?> FindProfileAsync(Guid playerId, CancellationToken cancellationToken)
        {
            return Task.FromResult(_profiles.SingleOrDefault(x => x.PlayerId == playerId));
        }

        public Task AppendCommandAsync(Guid roomId, Guid playerId, string commandType, string commandJson, int submittedAtTick, string? rejectedCode, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}
