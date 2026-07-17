using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace Game.Api.IntegrationTests;

public sealed class FriendlyBattleApiTests : IDisposable
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly string _databasePath;
    private readonly WebApplicationFactory<Program> _factory;

    public FriendlyBattleApiTests()
    {
        _databasePath = Path.Combine(Path.GetTempPath(), $"cr-friendly-battle-{Guid.NewGuid():N}.db");
        _factory = CreateFactory(_databasePath);
    }

    [Fact]
    public async Task FriendlyBattleInvites_WithoutSession_ShouldRequireSession()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = false });

        var response = await client.GetAsync("/api/friendly-battles/current");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Contains("SessionRequired", await response.Content.ReadAsStringAsync(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task CreateInvite_WhenNotFriendsOrSelf_ShouldReturnStableCodes()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = false });
        var player = await CreateGuestAsync(client);
        var target = await CreateGuestAsync(client);

        var notFriends = await SendJsonWithCookieAsync(
            client,
            "/api/friendly-battles/invites",
            player.Cookie,
            new FriendlyInviteRequest(target.PlayerId));
        var self = await SendJsonWithCookieAsync(
            client,
            "/api/friendly-battles/invites",
            player.Cookie,
            new FriendlyInviteRequest(player.PlayerId));

        Assert.Equal(HttpStatusCode.Forbidden, notFriends.StatusCode);
        Assert.Contains("FriendlyBattleNotFriends", await notFriends.Content.ReadAsStringAsync(), StringComparison.Ordinal);
        Assert.Equal(HttpStatusCode.BadRequest, self.StatusCode);
        Assert.Contains("FriendlyBattleSelfInvite", await self.Content.ReadAsStringAsync(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task InviteLifecycle_WhenFriendsAccept_ShouldCreateSharedRoom()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = false });
        var challenger = await CreateGuestAsync(client);
        var recipient = await CreateGuestAsync(client);
        var spectator = await CreateGuestAsync(client);
        await MakeFriendsAsync(client, challenger.Cookie, recipient.Cookie);

        var createResponse = await SendJsonWithCookieAsync(
            client,
            "/api/friendly-battles/invites",
            challenger.Cookie,
            new FriendlyInviteRequest(recipient.PlayerId));
        createResponse.EnsureSuccessStatusCode();
        var challengerSnapshot = (await createResponse.Content.ReadFromJsonAsync<FriendlyBattleResponse>(JsonOptions))!;
        var invite = Assert.Single(challengerSnapshot.OutgoingInvites);
        var recipientSnapshot = await GetFriendlyAsync(client, recipient.Cookie);
        Assert.Single(recipientSnapshot.IncomingInvites);

        var duplicate = await SendJsonWithCookieAsync(
            client,
            "/api/friendly-battles/invites",
            challenger.Cookie,
            new FriendlyInviteRequest(recipient.PlayerId));
        Assert.Equal(HttpStatusCode.Conflict, duplicate.StatusCode);
        Assert.Contains("FriendlyBattleDuplicateInvite", await duplicate.Content.ReadAsStringAsync(), StringComparison.Ordinal);

        var acceptResponse = await SendWithCookieAsync(
            client,
            HttpMethod.Post,
            $"/api/friendly-battles/invites/{invite.InviteId}/accept",
            recipient.Cookie);
        acceptResponse.EnsureSuccessStatusCode();
        var accepted = (await acceptResponse.Content.ReadFromJsonAsync<FriendlyBattleResponse>(JsonOptions))!;
        var roomId = accepted.ActiveRoom!.RoomId!.Value;
        var challengerAfterAccept = await GetFriendlyAsync(client, challenger.Cookie);

        Assert.Equal(roomId, challengerAfterAccept.ActiveRoom!.RoomId);
        Assert.Empty(accepted.IncomingInvites);
        Assert.Equal("Active", accepted.ActiveRoom.Status);

        var forbidden = await SendWithCookieAsync(
            client,
            HttpMethod.Get,
            $"/api/online-battles/{roomId}",
            spectator.Cookie);
        Assert.Equal(HttpStatusCode.Forbidden, forbidden.StatusCode);
        Assert.Contains("OnlineBattleForbidden", await forbidden.Content.ReadAsStringAsync(), StringComparison.Ordinal);

        var challengerProfileAfter = await GetSessionAsync(client, challenger.Cookie);
        var recipientProfileAfter = await GetSessionAsync(client, recipient.Cookie);
        Assert.Equal(challenger.Trophies, challengerProfileAfter.Trophies);
        Assert.Equal(recipient.Gold, recipientProfileAfter.Gold);
    }

    [Fact]
    public async Task CancelAndReject_ShouldPreventAcceptance()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = false });
        var challenger = await CreateGuestAsync(client);
        var recipient = await CreateGuestAsync(client);
        await MakeFriendsAsync(client, challenger.Cookie, recipient.Cookie);
        var cancelledInvite = await CreateInviteAsync(client, challenger.Cookie, recipient.PlayerId);

        var cancelResponse = await SendWithCookieAsync(
            client,
            HttpMethod.Delete,
            $"/api/friendly-battles/invites/{cancelledInvite.InviteId}",
            challenger.Cookie);
        cancelResponse.EnsureSuccessStatusCode();
        var acceptCancelled = await SendWithCookieAsync(
            client,
            HttpMethod.Post,
            $"/api/friendly-battles/invites/{cancelledInvite.InviteId}/accept",
            recipient.Cookie);
        Assert.Equal(HttpStatusCode.Conflict, acceptCancelled.StatusCode);
        Assert.Contains("FriendlyBattleAlreadyResolved", await acceptCancelled.Content.ReadAsStringAsync(), StringComparison.Ordinal);

        var rejectedInvite = await CreateInviteAsync(client, challenger.Cookie, recipient.PlayerId);
        var rejectResponse = await SendWithCookieAsync(
            client,
            HttpMethod.Post,
            $"/api/friendly-battles/invites/{rejectedInvite.InviteId}/reject",
            recipient.Cookie);
        rejectResponse.EnsureSuccessStatusCode();
        var acceptRejected = await SendWithCookieAsync(
            client,
            HttpMethod.Post,
            $"/api/friendly-battles/invites/{rejectedInvite.InviteId}/accept",
            recipient.Cookie);
        Assert.Equal(HttpStatusCode.Conflict, acceptRejected.StatusCode);
    }

    [Fact]
    public async Task FriendlyBattle_AfterApiRestartWithSameDatabase_ShouldPersistInviteAndRoom()
    {
        var databasePath = Path.Combine(Path.GetTempPath(), $"cr-friendly-restart-{Guid.NewGuid():N}.db");
        string challengerCookie;
        string recipientCookie;
        Guid roomId;

        await using (var firstFactory = CreateFactory(databasePath))
        {
            var client = firstFactory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = false });
            var challenger = await CreateGuestAsync(client);
            var recipient = await CreateGuestAsync(client);
            challengerCookie = challenger.Cookie;
            recipientCookie = recipient.Cookie;
            await MakeFriendsAsync(client, challengerCookie, recipientCookie);
            var invite = await CreateInviteAsync(client, challengerCookie, recipient.PlayerId);
            var acceptResponse = await SendWithCookieAsync(
                client,
                HttpMethod.Post,
                $"/api/friendly-battles/invites/{invite.InviteId}/accept",
                recipientCookie);
            acceptResponse.EnsureSuccessStatusCode();
            var accepted = (await acceptResponse.Content.ReadFromJsonAsync<FriendlyBattleResponse>(JsonOptions))!;
            roomId = accepted.ActiveRoom!.RoomId!.Value;
        }

        await using (var secondFactory = CreateFactory(databasePath))
        {
            var client = secondFactory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = false });
            var challengerSnapshot = await GetFriendlyAsync(client, challengerCookie);
            var recipientSnapshot = await GetFriendlyAsync(client, recipientCookie);

            Assert.Equal(roomId, challengerSnapshot.ActiveRoom!.RoomId);
            Assert.Equal(roomId, recipientSnapshot.ActiveRoom!.RoomId);
        }

        DeleteDatabaseFiles(databasePath);
    }

    private static async Task<FriendlyInviteResponse> CreateInviteAsync(HttpClient client, string cookie, Guid friendPlayerId)
    {
        var response = await SendJsonWithCookieAsync(
            client,
            "/api/friendly-battles/invites",
            cookie,
            new FriendlyInviteRequest(friendPlayerId));
        response.EnsureSuccessStatusCode();
        var snapshot = (await response.Content.ReadFromJsonAsync<FriendlyBattleResponse>(JsonOptions))!;
        return Assert.Single(snapshot.OutgoingInvites);
    }

    private static async Task MakeFriendsAsync(HttpClient client, string requesterCookie, string addresseeCookie)
    {
        var addresseeFriends = await GetFriendsAsync(client, addresseeCookie);
        var requestResponse = await SendJsonWithCookieAsync(
            client,
            "/api/friends/requests",
            requesterCookie,
            new FriendRequest(addresseeFriends.FriendCode));
        requestResponse.EnsureSuccessStatusCode();
        var incoming = Assert.Single((await GetFriendsAsync(client, addresseeCookie)).IncomingRequests);
        var acceptResponse = await SendWithCookieAsync(
            client,
            HttpMethod.Post,
            $"/api/friends/requests/{incoming.FriendshipId}/accept",
            addresseeCookie);
        acceptResponse.EnsureSuccessStatusCode();
    }

    private static async Task<GuestSessionResponse> CreateGuestAsync(HttpClient client)
    {
        var response = await client.GetAsync("/api/session");
        response.EnsureSuccessStatusCode();
        var session = (await response.Content.ReadFromJsonAsync<GuestSessionResponse>(JsonOptions))!;
        return session with { Cookie = ExtractSessionCookie(response) };
    }

    private static async Task<GuestSessionResponse> GetSessionAsync(HttpClient client, string cookie)
    {
        var response = await SendWithCookieAsync(client, HttpMethod.Get, "/api/session", cookie);
        response.EnsureSuccessStatusCode();
        var session = (await response.Content.ReadFromJsonAsync<GuestSessionResponse>(JsonOptions))!;
        return session with { Cookie = cookie };
    }

    private static async Task<FriendsResponse> GetFriendsAsync(HttpClient client, string cookie)
    {
        var response = await SendWithCookieAsync(client, HttpMethod.Get, "/api/friends", cookie);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<FriendsResponse>(JsonOptions))!;
    }

    private static async Task<FriendlyBattleResponse> GetFriendlyAsync(HttpClient client, string cookie)
    {
        var response = await SendWithCookieAsync(client, HttpMethod.Get, "/api/friendly-battles/current", cookie);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<FriendlyBattleResponse>(JsonOptions))!;
    }

    private static async Task<HttpResponseMessage> SendWithCookieAsync(
        HttpClient client,
        HttpMethod method,
        string path,
        string cookie)
    {
        var request = new HttpRequestMessage(method, path);
        request.Headers.Add("Cookie", cookie);
        return await client.SendAsync(request);
    }

    private static async Task<HttpResponseMessage> SendJsonWithCookieAsync<TBody>(
        HttpClient client,
        string path,
        string cookie,
        TBody body)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, path)
        {
            Content = JsonContent.Create(body)
        };
        request.Headers.Add("Cookie", cookie);
        return await client.SendAsync(request);
    }

    private static string ExtractSessionCookie(HttpResponseMessage response)
    {
        var setCookie = response.Headers.GetValues("Set-Cookie")
            .Single(x => x.StartsWith("royale_session=", StringComparison.Ordinal));
        return setCookie.Split(';')[0];
    }

    private static WebApplicationFactory<Program> CreateFactory(string databasePath)
    {
        return new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Development");
                builder.UseSetting("ConnectionStrings:GameDatabase", $"Data Source={databasePath}");
                builder.ConfigureAppConfiguration((_, configuration) =>
                {
                    configuration.AddInMemoryCollection(new Dictionary<string, string?>
                    {
                        ["ConnectionStrings:GameDatabase"] = $"Data Source={databasePath}"
                    });
                });
            });
    }

    public void Dispose()
    {
        _factory.Dispose();
        DeleteDatabaseFiles(_databasePath);
    }

    private static void DeleteDatabaseFiles(string databasePath)
    {
        foreach (var path in new[] { databasePath, $"{databasePath}-shm", $"{databasePath}-wal" })
        {
            try
            {
                if (File.Exists(path))
                {
                    File.Delete(path);
                }
            }
            catch (IOException)
            {
                // SQLite can release test database handles just after xUnit disposal on Windows.
            }
        }
    }

    private sealed record FriendlyInviteRequest(Guid FriendPlayerId);

    private sealed record FriendRequest(string FriendCode);

    private sealed record GuestSessionResponse(
        Guid PlayerId,
        string DisplayName,
        int Trophies,
        int Gold,
        string AccountType,
        string GuestWarning,
        string Cookie = "");

    private sealed record FriendlyBattleResponse(
        List<FriendlyInviteResponse> IncomingInvites,
        List<FriendlyInviteResponse> OutgoingInvites,
        OnlineBattleStateResponse? ActiveRoom);

    private sealed record FriendlyInviteResponse(
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

    private sealed record OnlineBattleStateResponse(
        string Status,
        Guid? RoomId,
        OnlineBattleSnapshotResponse? Snapshot);

    private sealed record OnlineBattleSnapshotResponse(
        Guid RoomId,
        string Status,
        string Result);

    private sealed record FriendsResponse(
        string FriendCode,
        List<FriendSummaryResponse> Friends,
        List<FriendRequestResponse> IncomingRequests,
        List<FriendRequestResponse> OutgoingRequests);

    private sealed record FriendSummaryResponse(
        Guid PlayerId,
        string DisplayName,
        int Trophies,
        string ShortPlayerId,
        string Status);

    private sealed record FriendRequestResponse(
        Guid FriendshipId,
        Guid PlayerId,
        string DisplayName,
        string ShortPlayerId,
        string Status);
}
