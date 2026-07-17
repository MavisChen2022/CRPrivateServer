using Game.Domain;

namespace Game.Application;

public sealed record FriendCodeRecord(Guid PlayerId, string Code, DateTimeOffset CreatedAt);

public sealed record StoredFriendProfile(Guid PlayerId, string DisplayName, int Trophies);

public sealed record StoredFriendship(
    Guid FriendshipId,
    Guid RequesterPlayerId,
    Guid AddresseePlayerId,
    Guid LowerPlayerId,
    Guid HigherPlayerId,
    string Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public sealed record FriendSummary(
    Guid PlayerId,
    string DisplayName,
    int Trophies,
    string ShortPlayerId,
    string Status);

public sealed record FriendRequestSummary(
    Guid FriendshipId,
    Guid PlayerId,
    string DisplayName,
    string ShortPlayerId,
    string Status);

public sealed record FriendsSnapshot(
    string FriendCode,
    List<FriendSummary> Friends,
    List<FriendRequestSummary> IncomingRequests,
    List<FriendRequestSummary> OutgoingRequests);

public sealed record FriendServiceResult(
    bool Succeeded,
    FriendsSnapshot? Snapshot,
    string? ErrorCode,
    int StatusCode);

public interface IFriendStore
{
    Task<FriendCodeRecord?> FindCodeByPlayerAsync(Guid playerId, CancellationToken cancellationToken);

    Task<FriendCodeRecord?> FindCodeAsync(string code, CancellationToken cancellationToken);

    Task SaveFriendCodeAsync(FriendCodeRecord code, CancellationToken cancellationToken);

    Task<StoredFriendProfile?> FindProfileAsync(Guid playerId, CancellationToken cancellationToken);

    Task<StoredFriendship?> FindRelationAsync(Guid firstPlayerId, Guid secondPlayerId, CancellationToken cancellationToken);

    Task<StoredFriendship?> FindFriendshipAsync(Guid friendshipId, CancellationToken cancellationToken);

    Task SaveFriendshipAsync(StoredFriendship friendship, CancellationToken cancellationToken);

    Task<IReadOnlyList<StoredFriendship>> ListRelationsAsync(Guid playerId, CancellationToken cancellationToken);
}

public sealed class FriendService
{
    private readonly IFriendStore _store;

    public FriendService(IFriendStore store)
    {
        _store = store;
    }

    public async Task<FriendsSnapshot> GetSnapshotAsync(
        Guid playerId,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var code = await GetOrCreateCodeAsync(playerId, now, cancellationToken);
        var relations = await _store.ListRelationsAsync(playerId, cancellationToken);
        return await BuildSnapshotAsync(playerId, code.Code, relations, cancellationToken);
    }

    public async Task<FriendServiceResult> CreateRequestAsync(
        Guid requesterPlayerId,
        string friendCode,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        if (!FriendCode.IsValid(friendCode))
        {
            return await ErrorWithSnapshotAsync(requesterPlayerId, now, "FriendCodeNotFound", 404, cancellationToken);
        }

        var targetCode = await _store.FindCodeAsync(FriendCode.Normalize(friendCode), cancellationToken);
        if (targetCode is null)
        {
            return await ErrorWithSnapshotAsync(requesterPlayerId, now, "FriendCodeNotFound", 404, cancellationToken);
        }

        if (targetCode.PlayerId == requesterPlayerId)
        {
            return await ErrorWithSnapshotAsync(requesterPlayerId, now, "CannotAddSelf", 400, cancellationToken);
        }

        var existing = await _store.FindRelationAsync(requesterPlayerId, targetCode.PlayerId, cancellationToken);
        if (existing is not null && existing.Status != "Rejected")
        {
            return await ErrorWithSnapshotAsync(requesterPlayerId, now, "DuplicateFriend", 409, cancellationToken);
        }

        var pair = FriendshipPair.Create(requesterPlayerId, targetCode.PlayerId);
        var friendship = existing is null
            ? new StoredFriendship(
                Guid.NewGuid(),
                requesterPlayerId,
                targetCode.PlayerId,
                pair.LowerPlayerId,
                pair.HigherPlayerId,
                "Pending",
                now,
                now)
            : existing with
            {
                RequesterPlayerId = requesterPlayerId,
                AddresseePlayerId = targetCode.PlayerId,
                Status = "Pending",
                UpdatedAt = now
            };

        await _store.SaveFriendshipAsync(friendship, cancellationToken);
        return FriendServiceResultSuccess(await GetSnapshotAsync(requesterPlayerId, now, cancellationToken));
    }

    public async Task<FriendServiceResult> AcceptAsync(
        Guid playerId,
        Guid friendshipId,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        return await ChangeIncomingStatusAsync(playerId, friendshipId, "Accepted", now, cancellationToken);
    }

    public async Task<FriendServiceResult> RejectAsync(
        Guid playerId,
        Guid friendshipId,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        return await ChangeIncomingStatusAsync(playerId, friendshipId, "Rejected", now, cancellationToken);
    }

    private async Task<FriendServiceResult> ChangeIncomingStatusAsync(
        Guid playerId,
        Guid friendshipId,
        string status,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var friendship = await _store.FindFriendshipAsync(friendshipId, cancellationToken);
        if (friendship is null)
        {
            return await ErrorWithSnapshotAsync(playerId, now, "FriendRequestNotFound", 404, cancellationToken);
        }

        if (friendship.AddresseePlayerId != playerId)
        {
            return await ErrorWithSnapshotAsync(playerId, now, "FriendForbidden", 403, cancellationToken);
        }

        if (friendship.Status != "Pending")
        {
            return await ErrorWithSnapshotAsync(playerId, now, "DuplicateFriend", 409, cancellationToken);
        }

        await _store.SaveFriendshipAsync(friendship with
        {
            Status = status,
            UpdatedAt = now
        }, cancellationToken);

        return FriendServiceResultSuccess(await GetSnapshotAsync(playerId, now, cancellationToken));
    }

    private async Task<FriendCodeRecord> GetOrCreateCodeAsync(
        Guid playerId,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var existing = await _store.FindCodeByPlayerAsync(playerId, cancellationToken);
        if (existing is not null)
        {
            return existing;
        }

        for (var attempt = 0; attempt < 5; attempt++)
        {
            var code = FriendCode.Generate();
            if (await _store.FindCodeAsync(code, cancellationToken) is not null)
            {
                continue;
            }

            var record = new FriendCodeRecord(playerId, code, now);
            await _store.SaveFriendCodeAsync(record, cancellationToken);
            return record;
        }

        throw new InvalidOperationException("Could not create a unique friend code.");
    }

    private async Task<FriendsSnapshot> BuildSnapshotAsync(
        Guid playerId,
        string friendCode,
        IReadOnlyList<StoredFriendship> relations,
        CancellationToken cancellationToken)
    {
        var friends = new List<FriendSummary>();
        var incoming = new List<FriendRequestSummary>();
        var outgoing = new List<FriendRequestSummary>();

        foreach (var relation in relations)
        {
            var otherPlayerId = relation.RequesterPlayerId == playerId
                ? relation.AddresseePlayerId
                : relation.RequesterPlayerId;
            var profile = await _store.FindProfileAsync(otherPlayerId, cancellationToken);
            if (profile is null)
            {
                continue;
            }

            if (relation.Status == "Accepted")
            {
                friends.Add(new FriendSummary(
                    profile.PlayerId,
                    profile.DisplayName,
                    profile.Trophies,
                    ShortId(profile.PlayerId),
                    "Friend"));
                continue;
            }

            var request = new FriendRequestSummary(
                relation.FriendshipId,
                profile.PlayerId,
                profile.DisplayName,
                ShortId(profile.PlayerId),
                relation.Status);

            if (relation.Status == "Pending" && relation.AddresseePlayerId == playerId)
            {
                incoming.Add(request);
            }
            else if (relation.Status == "Pending" && relation.RequesterPlayerId == playerId)
            {
                outgoing.Add(request);
            }
        }

        return new FriendsSnapshot(friendCode, friends, incoming, outgoing);
    }

    private async Task<FriendServiceResult> ErrorWithSnapshotAsync(
        Guid playerId,
        DateTimeOffset now,
        string code,
        int statusCode,
        CancellationToken cancellationToken)
    {
        return new FriendServiceResult(false, await GetSnapshotAsync(playerId, now, cancellationToken), code, statusCode);
    }

    private static FriendServiceResult FriendServiceResultSuccess(FriendsSnapshot snapshot)
    {
        return new FriendServiceResult(true, snapshot, null, 200);
    }

    private static string ShortId(Guid playerId)
    {
        return playerId.ToString("N")[..8].ToUpperInvariant();
    }
}

