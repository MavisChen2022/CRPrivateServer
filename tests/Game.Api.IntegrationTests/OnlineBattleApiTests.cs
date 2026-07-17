using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace Game.Api.IntegrationTests;

public sealed class OnlineBattleApiTests : IDisposable
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly string _databasePath;
    private readonly WebApplicationFactory<Program> _factory;

    public OnlineBattleApiTests()
    {
        _databasePath = Path.Combine(Path.GetTempPath(), $"cr-online-{Guid.NewGuid():N}.db");
        _factory = CreateFactory(_databasePath);
    }

    [Fact]
    public async Task Queue_WithoutSession_ShouldRequireSession()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = false });

        var response = await client.PostAsync("/api/online-battles/matchmaking", content: null);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Contains("SessionRequired", await response.Content.ReadAsStringAsync(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task QueueAndCancel_WithGuestSession_ShouldReturnIdle()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = false });
        var cookie = await CreateGuestCookieAsync(client);

        var queued = await SendWithCookieAsync(client, HttpMethod.Post, "/api/online-battles/matchmaking", cookie);
        var cancelled = await SendWithCookieAsync(client, HttpMethod.Delete, "/api/online-battles/matchmaking", cookie);

        await EnsureSuccessWithBodyAsync(queued);
        await EnsureSuccessWithBodyAsync(cancelled);
        Assert.Equal("Waiting", (await queued.Content.ReadFromJsonAsync<OnlineStateResponse>(JsonOptions))!.Status);
        Assert.Equal("Idle", (await cancelled.Content.ReadFromJsonAsync<OnlineStateResponse>(JsonOptions))!.Status);
    }

    [Fact]
    public async Task Queue_WhenTwoGuestsJoin_ShouldCreateSharedRoom()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = false });
        var firstCookie = await CreateGuestCookieAsync(client);
        var secondCookie = await CreateGuestCookieAsync(client);

        var firstQueue = await QueueAsync(client, firstCookie);
        var secondQueue = await QueueAsync(client, secondCookie);
        var firstCurrent = await CurrentAsync(client, firstCookie);

        Assert.Equal("Waiting", firstQueue.Status);
        Assert.Equal("Active", secondQueue.Status);
        Assert.Equal(secondQueue.RoomId, firstCurrent.RoomId);
        Assert.Equal(1000, secondQueue.Snapshot!.PlayerOne.TowerHp);
        Assert.Equal(1000, secondQueue.Snapshot.PlayerTwo.TowerHp);
    }

    [Fact]
    public async Task OnlineCommand_WithValidDeployAndTick_ShouldUpdateSharedSnapshot()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = false });
        var firstCookie = await CreateGuestCookieAsync(client);
        var secondCookie = await CreateGuestCookieAsync(client);
        await QueueAsync(client, firstCookie);
        var matched = await QueueAsync(client, secondCookie);

        var deploy = await SendJsonWithCookieAsync(
            client,
            $"/api/online-battles/{matched.RoomId}/commands",
            firstCookie,
            new DeployRequest("training-knight", "center", 0.5, 0.72));
        deploy.EnsureSuccessStatusCode();
        var tick = await SendJsonWithCookieAsync(
            client,
            $"/api/online-battles/{matched.RoomId}/tick",
            secondCookie,
            new TickRequest(10));
        tick.EnsureSuccessStatusCode();
        var advanced = await tick.Content.ReadFromJsonAsync<OnlineStateResponse>(JsonOptions);

        Assert.NotNull(advanced);
        Assert.Single(advanced!.Snapshot!.Units);
        Assert.True(advanced.Snapshot.PlayerTwo.TowerHp < 1000);
    }

    [Fact]
    public async Task OnlineRoom_WhenRequestedByNonParticipant_ShouldReturnForbidden()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = false });
        var firstCookie = await CreateGuestCookieAsync(client);
        var secondCookie = await CreateGuestCookieAsync(client);
        var thirdCookie = await CreateGuestCookieAsync(client);
        await QueueAsync(client, firstCookie);
        var matched = await QueueAsync(client, secondCookie);

        var response = await SendWithCookieAsync(
            client,
            HttpMethod.Get,
            $"/api/online-battles/{matched.RoomId}",
            thirdCookie);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Contains("OnlineBattleForbidden", await response.Content.ReadAsStringAsync(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task OnlineCommand_WithInvalidPlacement_ShouldReturnStableCode()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = false });
        var firstCookie = await CreateGuestCookieAsync(client);
        var secondCookie = await CreateGuestCookieAsync(client);
        await QueueAsync(client, firstCookie);
        var matched = await QueueAsync(client, secondCookie);

        var response = await SendJsonWithCookieAsync(
            client,
            $"/api/online-battles/{matched.RoomId}/commands",
            firstCookie,
            new DeployRequest("training-knight", "center", 0.5, 0.25));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("InvalidPlacement", await response.Content.ReadAsStringAsync(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task OnlineBattle_AfterApiRestartWithSameDatabase_ShouldPersist()
    {
        var databasePath = Path.Combine(Path.GetTempPath(), $"cr-online-restart-{Guid.NewGuid():N}.db");
        string firstCookie;
        string secondCookie;
        Guid roomId;

        await using (var firstFactory = CreateFactory(databasePath))
        {
            var client = firstFactory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = false });
            firstCookie = await CreateGuestCookieAsync(client);
            secondCookie = await CreateGuestCookieAsync(client);
            await QueueAsync(client, firstCookie);
            var matched = await QueueAsync(client, secondCookie);
            roomId = matched.RoomId!.Value;
            var deploy = await SendJsonWithCookieAsync(
                client,
                $"/api/online-battles/{roomId}/commands",
                firstCookie,
                new DeployRequest("training-knight", "center", 0.5, 0.72));
            deploy.EnsureSuccessStatusCode();
        }

        await using (var secondFactory = CreateFactory(databasePath))
        {
            var client = secondFactory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = false });
            var firstCurrent = await CurrentAsync(client, firstCookie);
            var secondCurrent = await CurrentAsync(client, secondCookie);

            Assert.Equal(roomId, firstCurrent.RoomId);
            Assert.Equal(roomId, secondCurrent.RoomId);
            Assert.Single(firstCurrent.Snapshot!.Units);
        }

        DeleteDatabaseFiles(databasePath);
    }

    private static async Task<OnlineStateResponse> QueueAsync(HttpClient client, string cookie)
    {
        var response = await SendWithCookieAsync(client, HttpMethod.Post, "/api/online-battles/matchmaking", cookie);
        await EnsureSuccessWithBodyAsync(response);
        return (await response.Content.ReadFromJsonAsync<OnlineStateResponse>(JsonOptions))!;
    }

    private static async Task<OnlineStateResponse> CurrentAsync(HttpClient client, string cookie)
    {
        var response = await SendWithCookieAsync(client, HttpMethod.Get, "/api/online-battles/current", cookie);
        await EnsureSuccessWithBodyAsync(response);
        return (await response.Content.ReadFromJsonAsync<OnlineStateResponse>(JsonOptions))!;
    }

    private static async Task EnsureSuccessWithBodyAsync(HttpResponseMessage response)
    {
        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException(await response.Content.ReadAsStringAsync());
        }
    }

    private static async Task<string> CreateGuestCookieAsync(HttpClient client)
    {
        var response = await client.GetAsync("/api/session");
        response.EnsureSuccessStatusCode();
        return ExtractSessionCookie(response);
    }

    private static async Task<HttpResponseMessage> SendWithCookieAsync(HttpClient client, HttpMethod method, string path, string cookie)
    {
        var request = new HttpRequestMessage(method, path);
        request.Headers.Add("Cookie", cookie);
        return await client.SendAsync(request);
    }

    private static async Task<HttpResponseMessage> SendJsonWithCookieAsync<TBody>(HttpClient client, string path, string cookie, TBody body)
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

    private sealed record DeployRequest(string CardId, string Lane, double X, double Y);

    private sealed record TickRequest(int Ticks);

    private sealed record OnlineStateResponse(string Status, Guid? RoomId, OnlineBattleSnapshotResponse? Snapshot);

    private sealed record OnlineBattleSnapshotResponse(
        Guid RoomId,
        string Status,
        string Result,
        int Tick,
        int DurationTicks,
        OnlineParticipantResponse PlayerOne,
        OnlineParticipantResponse PlayerTwo,
        List<BattleCardResponse> StarterDeck,
        List<OnlineUnitResponse> Units);

    private sealed record OnlineParticipantResponse(
        Guid PlayerId,
        string DisplayName,
        string Side,
        int TowerHp,
        int Elixir,
        int MaxElixir);

    private sealed record BattleCardResponse(string CardId, string Name, int ElixirCost);

    private sealed record OnlineUnitResponse(
        string UnitId,
        Guid OwnerPlayerId,
        string Side,
        string CardId,
        string Name,
        string Lane,
        int Position,
        int DamagePerTick);
}
