using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace Game.Api.IntegrationTests;

public sealed class FriendApiTests : IDisposable
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly string _databasePath;
    private readonly WebApplicationFactory<Program> _factory;

    public FriendApiTests()
    {
        _databasePath = Path.Combine(Path.GetTempPath(), $"cr-friends-{Guid.NewGuid():N}.db");
        _factory = CreateFactory(_databasePath);
    }

    [Fact]
    public async Task GetFriends_WithoutSession_ShouldRequireSession()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            HandleCookies = false
        });

        var response = await client.GetAsync("/api/friends");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Contains("SessionRequired", await response.Content.ReadAsStringAsync(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task GetFriends_WithSession_ShouldReturnStableFriendCode()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            HandleCookies = false
        });
        var cookie = await CreateGuestCookieAsync(client);

        var first = await GetFriendsAsync(client, cookie);
        var second = await GetFriendsAsync(client, cookie);

        Assert.Equal(8, first.FriendCode.Length);
        Assert.Equal(first.FriendCode, second.FriendCode);
        Assert.Empty(first.Friends);
        Assert.Empty(first.IncomingRequests);
        Assert.Empty(first.OutgoingRequests);

        var body = JsonSerializer.Serialize(first, JsonOptions);
        Assert.DoesNotContain("royale_session", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("token", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("tokenHash", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task FriendRequest_WhenAccepted_ShouldShowFriendForBothPlayers()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            HandleCookies = false
        });
        var requesterCookie = await CreateGuestCookieAsync(client);
        var addresseeCookie = await CreateGuestCookieAsync(client);
        var addresseeFriends = await GetFriendsAsync(client, addresseeCookie);

        var requestResponse = await SendJsonWithCookieAsync(
            client,
            "/api/friends/requests",
            requesterCookie,
            new FriendRequest(addresseeFriends.FriendCode));
        requestResponse.EnsureSuccessStatusCode();
        var requesterPending = await requestResponse.Content.ReadFromJsonAsync<FriendsResponse>(JsonOptions);
        var addresseePending = await GetFriendsAsync(client, addresseeCookie);
        var incoming = Assert.Single(addresseePending.IncomingRequests);

        var acceptResponse = await SendWithCookieAsync(
            client,
            HttpMethod.Post,
            $"/api/friends/requests/{incoming.FriendshipId}/accept",
            addresseeCookie);
        acceptResponse.EnsureSuccessStatusCode();
        var requesterAccepted = await GetFriendsAsync(client, requesterCookie);
        var addresseeAccepted = await GetFriendsAsync(client, addresseeCookie);

        Assert.NotNull(requesterPending);
        Assert.Single(requesterPending!.OutgoingRequests);
        Assert.Single(requesterAccepted.Friends);
        Assert.Single(addresseeAccepted.Friends);
    }

    [Fact]
    public async Task FriendRequest_WhenAddingSelf_ShouldReturnStableCode()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            HandleCookies = false
        });
        var cookie = await CreateGuestCookieAsync(client);
        var friends = await GetFriendsAsync(client, cookie);

        var response = await SendJsonWithCookieAsync(
            client,
            "/api/friends/requests",
            cookie,
            new FriendRequest(friends.FriendCode));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("CannotAddSelf", await response.Content.ReadAsStringAsync(), StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("ABCDEFGH")]
    [InlineData("ABCDEFG1")]
    public async Task FriendRequest_WhenCodeIsUnknownOrMalformed_ShouldReturnNotFound(string friendCode)
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            HandleCookies = false
        });
        var cookie = await CreateGuestCookieAsync(client);

        var response = await SendJsonWithCookieAsync(
            client,
            "/api/friends/requests",
            cookie,
            new FriendRequest(friendCode));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Contains("FriendCodeNotFound", await response.Content.ReadAsStringAsync(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task FriendRequest_WhenDuplicate_ShouldReturnConflict()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            HandleCookies = false
        });
        var requesterCookie = await CreateGuestCookieAsync(client);
        var addresseeCookie = await CreateGuestCookieAsync(client);
        var addresseeFriends = await GetFriendsAsync(client, addresseeCookie);

        var first = await SendJsonWithCookieAsync(
            client,
            "/api/friends/requests",
            requesterCookie,
            new FriendRequest(addresseeFriends.FriendCode));
        first.EnsureSuccessStatusCode();
        var second = await SendJsonWithCookieAsync(
            client,
            "/api/friends/requests",
            requesterCookie,
            new FriendRequest(addresseeFriends.FriendCode));

        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
        Assert.Contains("DuplicateFriend", await second.Content.ReadAsStringAsync(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task FriendRequest_WhenRequesterAcceptsOwnOutgoingRequest_ShouldReturnForbidden()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            HandleCookies = false
        });
        var requesterCookie = await CreateGuestCookieAsync(client);
        var addresseeCookie = await CreateGuestCookieAsync(client);
        var addresseeFriends = await GetFriendsAsync(client, addresseeCookie);

        var requestResponse = await SendJsonWithCookieAsync(
            client,
            "/api/friends/requests",
            requesterCookie,
            new FriendRequest(addresseeFriends.FriendCode));
        requestResponse.EnsureSuccessStatusCode();
        var requesterFriends = (await requestResponse.Content.ReadFromJsonAsync<FriendsResponse>(JsonOptions))!;
        var outgoing = Assert.Single(requesterFriends.OutgoingRequests);

        var response = await SendWithCookieAsync(
            client,
            HttpMethod.Post,
            $"/api/friends/requests/{outgoing.FriendshipId}/accept",
            requesterCookie);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Contains("FriendForbidden", await response.Content.ReadAsStringAsync(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task FriendRequest_WhenUnrelatedGuestAcceptsRequest_ShouldReturnForbidden()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            HandleCookies = false
        });
        var requesterCookie = await CreateGuestCookieAsync(client);
        var addresseeCookie = await CreateGuestCookieAsync(client);
        var unrelatedCookie = await CreateGuestCookieAsync(client);
        var addresseeFriends = await GetFriendsAsync(client, addresseeCookie);

        var requestResponse = await SendJsonWithCookieAsync(
            client,
            "/api/friends/requests",
            requesterCookie,
            new FriendRequest(addresseeFriends.FriendCode));
        requestResponse.EnsureSuccessStatusCode();
        var incoming = Assert.Single((await GetFriendsAsync(client, addresseeCookie)).IncomingRequests);

        var response = await SendWithCookieAsync(
            client,
            HttpMethod.Post,
            $"/api/friends/requests/{incoming.FriendshipId}/accept",
            unrelatedCookie);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Contains("FriendForbidden", await response.Content.ReadAsStringAsync(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task FriendRequest_WhenRejected_ShouldNotCreateFriendRows()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            HandleCookies = false
        });
        var requesterCookie = await CreateGuestCookieAsync(client);
        var addresseeCookie = await CreateGuestCookieAsync(client);
        var addresseeFriends = await GetFriendsAsync(client, addresseeCookie);

        var requestResponse = await SendJsonWithCookieAsync(
            client,
            "/api/friends/requests",
            requesterCookie,
            new FriendRequest(addresseeFriends.FriendCode));
        requestResponse.EnsureSuccessStatusCode();
        var incoming = Assert.Single((await GetFriendsAsync(client, addresseeCookie)).IncomingRequests);

        var rejectResponse = await SendWithCookieAsync(
            client,
            HttpMethod.Post,
            $"/api/friends/requests/{incoming.FriendshipId}/reject",
            addresseeCookie);
        rejectResponse.EnsureSuccessStatusCode();
        var requesterFriends = await GetFriendsAsync(client, requesterCookie);
        var addresseeSnapshot = await rejectResponse.Content.ReadFromJsonAsync<FriendsResponse>(JsonOptions);

        Assert.NotNull(addresseeSnapshot);
        Assert.Empty(addresseeSnapshot!.Friends);
        Assert.Empty(addresseeSnapshot.IncomingRequests);
        Assert.Empty(requesterFriends.Friends);
        Assert.Empty(requesterFriends.OutgoingRequests);
    }

    [Fact]
    public async Task Friends_AfterApiRestartWithSameDatabase_ShouldPersist()
    {
        var databasePath = Path.Combine(Path.GetTempPath(), $"cr-friends-restart-{Guid.NewGuid():N}.db");
        string requesterCookie;
        string addresseeCookie;

        await using (var firstFactory = CreateFactory(databasePath))
        {
            var firstClient = firstFactory.CreateClient(new WebApplicationFactoryClientOptions
            {
                HandleCookies = false
            });
            requesterCookie = await CreateGuestCookieAsync(firstClient);
            addresseeCookie = await CreateGuestCookieAsync(firstClient);
            var addresseeFriends = await GetFriendsAsync(firstClient, addresseeCookie);
            var requestResponse = await SendJsonWithCookieAsync(
                firstClient,
                "/api/friends/requests",
                requesterCookie,
                new FriendRequest(addresseeFriends.FriendCode));
            requestResponse.EnsureSuccessStatusCode();
            var incoming = Assert.Single((await GetFriendsAsync(firstClient, addresseeCookie)).IncomingRequests);
            var acceptResponse = await SendWithCookieAsync(
                firstClient,
                HttpMethod.Post,
                $"/api/friends/requests/{incoming.FriendshipId}/accept",
                addresseeCookie);
            acceptResponse.EnsureSuccessStatusCode();
        }

        await using (var secondFactory = CreateFactory(databasePath))
        {
            var secondClient = secondFactory.CreateClient(new WebApplicationFactoryClientOptions
            {
                HandleCookies = false
            });
            var requesterFriends = await GetFriendsAsync(secondClient, requesterCookie);
            var addresseeFriends = await GetFriendsAsync(secondClient, addresseeCookie);

            Assert.Single(requesterFriends.Friends);
            Assert.Single(addresseeFriends.Friends);
        }

        DeleteDatabaseFiles(databasePath);
    }

    private static async Task<string> CreateGuestCookieAsync(HttpClient client)
    {
        var response = await client.GetAsync("/api/session");
        response.EnsureSuccessStatusCode();
        return ExtractSessionCookie(response);
    }

    private static async Task<FriendsResponse> GetFriendsAsync(HttpClient client, string cookie)
    {
        var response = await SendWithCookieAsync(client, HttpMethod.Get, "/api/friends", cookie);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<FriendsResponse>(JsonOptions))!;
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
            TryDelete(path);
        }
    }

    private static void TryDelete(string path)
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

    private sealed record FriendRequest(string FriendCode);

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
