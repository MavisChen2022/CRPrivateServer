using Game.Domain;

namespace Game.Application;

public sealed record StoredFriendlyBattleInvite(
    Guid InviteId,
    Guid RequesterPlayerId,
    Guid AddresseePlayerId,
    Guid LowerPlayerId,
    Guid HigherPlayerId,
    string Status,
    Guid? RoomId,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    DateTimeOffset ExpiresAt);

public sealed record FriendlyBattleInviteSummary(
    Guid InviteId,
    Guid RequesterPlayerId,
    string RequesterDisplayName,
    Guid AddresseePlayerId,
    string AddresseeDisplayName,
    string Status,
    Guid? RoomId,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    DateTimeOffset ExpiresAt);

public sealed record FriendlyBattleSnapshot(
    List<FriendlyBattleInviteSummary> IncomingInvites,
    List<FriendlyBattleInviteSummary> OutgoingInvites,
    OnlineBattleState? ActiveRoom);

public sealed record FriendlyBattleServiceResult(
    bool Succeeded,
    FriendlyBattleSnapshot? Snapshot,
    string? ErrorCode,
    int StatusCode);

public interface IFriendlyBattleStore
{
    Task<StoredFriendProfile?> FindProfileAsync(Guid playerId, CancellationToken cancellationToken);

    Task<StoredFriendship?> FindRelationAsync(Guid firstPlayerId, Guid secondPlayerId, CancellationToken cancellationToken);

    Task<StoredFriendlyBattleInvite?> FindPendingByPairAsync(Guid firstPlayerId, Guid secondPlayerId, CancellationToken cancellationToken);

    Task<StoredFriendlyBattleInvite?> FindInviteAsync(Guid inviteId, CancellationToken cancellationToken);

    Task SaveInviteAsync(StoredFriendlyBattleInvite invite, CancellationToken cancellationToken);

    Task<IReadOnlyList<StoredFriendlyBattleInvite>> ListInvitesAsync(Guid playerId, CancellationToken cancellationToken);
}

public sealed class FriendlyBattleService
{
    private static readonly TimeSpan InviteLifetime = TimeSpan.FromMinutes(10);

    private readonly IFriendlyBattleStore _store;
    private readonly OnlineBattleService _onlineBattles;

    public FriendlyBattleService(IFriendlyBattleStore store, OnlineBattleService onlineBattles)
    {
        _store = store;
        _onlineBattles = onlineBattles;
    }

    public async Task<FriendlyBattleSnapshot> GetSnapshotAsync(
        Guid playerId,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var invites = await _store.ListInvitesAsync(playerId, cancellationToken);
        return await BuildSnapshotAsync(playerId, invites, now, cancellationToken);
    }

    public async Task<FriendlyBattleServiceResult> CreateInviteAsync(
        Guid requesterPlayerId,
        Guid addresseePlayerId,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        if (requesterPlayerId == addresseePlayerId)
        {
            return await ErrorWithSnapshotAsync(
                requesterPlayerId,
                now,
                "FriendlyBattleSelfInvite",
                400,
                cancellationToken);
        }

        var relation = await _store.FindRelationAsync(requesterPlayerId, addresseePlayerId, cancellationToken);
        if (relation is null || relation.Status != "Accepted")
        {
            return await ErrorWithSnapshotAsync(
                requesterPlayerId,
                now,
                "FriendlyBattleNotFriends",
                403,
                cancellationToken);
        }

        if (await _store.FindPendingByPairAsync(requesterPlayerId, addresseePlayerId, cancellationToken) is not null)
        {
            return await ErrorWithSnapshotAsync(
                requesterPlayerId,
                now,
                "FriendlyBattleDuplicateInvite",
                409,
                cancellationToken);
        }

        var pair = FriendshipPair.Create(requesterPlayerId, addresseePlayerId);
        await _store.SaveInviteAsync(new StoredFriendlyBattleInvite(
            Guid.NewGuid(),
            requesterPlayerId,
            addresseePlayerId,
            pair.LowerPlayerId,
            pair.HigherPlayerId,
            "Pending",
            null,
            now,
            now,
            now.Add(InviteLifetime)), cancellationToken);

        return Success(await GetSnapshotAsync(requesterPlayerId, now, cancellationToken));
    }

    public async Task<FriendlyBattleServiceResult> CancelAsync(
        Guid playerId,
        Guid inviteId,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var invite = await _store.FindInviteAsync(inviteId, cancellationToken);
        if (invite is null)
        {
            return await ErrorWithSnapshotAsync(playerId, now, "FriendlyBattleInviteNotFound", 404, cancellationToken);
        }

        if (invite.RequesterPlayerId != playerId)
        {
            return await ErrorWithSnapshotAsync(playerId, now, "FriendlyBattleForbidden", 403, cancellationToken);
        }

        return await ResolveAsync(playerId, invite, "Cancelled", now, cancellationToken);
    }

    public async Task<FriendlyBattleServiceResult> RejectAsync(
        Guid playerId,
        Guid inviteId,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var invite = await _store.FindInviteAsync(inviteId, cancellationToken);
        if (invite is null)
        {
            return await ErrorWithSnapshotAsync(playerId, now, "FriendlyBattleInviteNotFound", 404, cancellationToken);
        }

        if (invite.AddresseePlayerId != playerId)
        {
            return await ErrorWithSnapshotAsync(playerId, now, "FriendlyBattleForbidden", 403, cancellationToken);
        }

        return await ResolveAsync(playerId, invite, "Rejected", now, cancellationToken);
    }

    public async Task<FriendlyBattleServiceResult> AcceptAsync(
        Guid playerId,
        Guid inviteId,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var invite = await _store.FindInviteAsync(inviteId, cancellationToken);
        if (invite is null)
        {
            return await ErrorWithSnapshotAsync(playerId, now, "FriendlyBattleInviteNotFound", 404, cancellationToken);
        }

        if (invite.AddresseePlayerId != playerId)
        {
            return await ErrorWithSnapshotAsync(playerId, now, "FriendlyBattleForbidden", 403, cancellationToken);
        }

        if (invite.Status != "Pending")
        {
            return await ErrorWithSnapshotAsync(playerId, now, "FriendlyBattleAlreadyResolved", 409, cancellationToken);
        }

        if (invite.ExpiresAt <= now)
        {
            await _store.SaveInviteAsync(invite with { Status = "Expired", UpdatedAt = now }, cancellationToken);
            return await ErrorWithSnapshotAsync(playerId, now, "FriendlyBattleInviteExpired", 409, cancellationToken);
        }

        var requester = await _store.FindProfileAsync(invite.RequesterPlayerId, cancellationToken);
        var addressee = await _store.FindProfileAsync(invite.AddresseePlayerId, cancellationToken);
        if (requester is null || addressee is null)
        {
            return await ErrorWithSnapshotAsync(playerId, now, "FriendlyBattleInviteNotFound", 404, cancellationToken);
        }

        var room = await _onlineBattles.CreateRoomAsync(
            invite.RequesterPlayerId,
            requester.DisplayName,
            invite.AddresseePlayerId,
            addressee.DisplayName,
            now,
            cancellationToken);
        if (!room.Succeeded || room.State?.RoomId is null)
        {
            return await ErrorWithSnapshotAsync(
                playerId,
                now,
                room.ErrorCode ?? "FriendlyBattleRoomUnavailable",
                room.StatusCode,
                cancellationToken);
        }

        await _store.SaveInviteAsync(invite with
        {
            Status = "Accepted",
            RoomId = room.State.RoomId,
            UpdatedAt = now
        }, cancellationToken);

        return Success(await GetSnapshotAsync(playerId, now, cancellationToken));
    }

    private async Task<FriendlyBattleServiceResult> ResolveAsync(
        Guid playerId,
        StoredFriendlyBattleInvite invite,
        string status,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        if (invite.Status != "Pending")
        {
            return await ErrorWithSnapshotAsync(playerId, now, "FriendlyBattleAlreadyResolved", 409, cancellationToken);
        }

        await _store.SaveInviteAsync(invite with { Status = status, UpdatedAt = now }, cancellationToken);
        return Success(await GetSnapshotAsync(playerId, now, cancellationToken));
    }

    private async Task<FriendlyBattleSnapshot> BuildSnapshotAsync(
        Guid playerId,
        IReadOnlyList<StoredFriendlyBattleInvite> invites,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var incoming = new List<FriendlyBattleInviteSummary>();
        var outgoing = new List<FriendlyBattleInviteSummary>();
        OnlineBattleState? activeRoom = null;

        foreach (var invite in invites)
        {
            var status = invite.Status == "Pending" && invite.ExpiresAt <= now ? "Expired" : invite.Status;
            var requester = await _store.FindProfileAsync(invite.RequesterPlayerId, cancellationToken);
            var addressee = await _store.FindProfileAsync(invite.AddresseePlayerId, cancellationToken);
            if (requester is null || addressee is null)
            {
                continue;
            }

            var summary = new FriendlyBattleInviteSummary(
                invite.InviteId,
                invite.RequesterPlayerId,
                requester.DisplayName,
                invite.AddresseePlayerId,
                addressee.DisplayName,
                status,
                invite.RoomId,
                invite.CreatedAt,
                invite.UpdatedAt,
                invite.ExpiresAt);

            if (status == "Accepted" && invite.RoomId is not null)
            {
                var room = await _onlineBattles.GetRoomAsync(playerId, invite.RoomId.Value, cancellationToken);
                if (room.Succeeded)
                {
                    activeRoom = room.State;
                }
            }

            if (invite.AddresseePlayerId == playerId && status == "Pending")
            {
                incoming.Add(summary);
            }
            else if (invite.RequesterPlayerId == playerId && status == "Pending")
            {
                outgoing.Add(summary);
            }
        }

        return new FriendlyBattleSnapshot(incoming, outgoing, activeRoom);
    }

    private async Task<FriendlyBattleServiceResult> ErrorWithSnapshotAsync(
        Guid playerId,
        DateTimeOffset now,
        string code,
        int statusCode,
        CancellationToken cancellationToken)
    {
        return new FriendlyBattleServiceResult(
            false,
            await GetSnapshotAsync(playerId, now, cancellationToken),
            code,
            statusCode);
    }

    private static FriendlyBattleServiceResult Success(FriendlyBattleSnapshot snapshot)
    {
        return new FriendlyBattleServiceResult(true, snapshot, null, 200);
    }
}
