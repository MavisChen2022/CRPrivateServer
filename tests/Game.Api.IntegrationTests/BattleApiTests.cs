using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace Game.Api.IntegrationTests;

public sealed class BattleApiTests : IDisposable
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly string _databasePath;
    private readonly WebApplicationFactory<Program> _factory;

    public BattleApiTests()
    {
        _databasePath = Path.Combine(Path.GetTempPath(), $"cr-battle-{Guid.NewGuid():N}.db");
        _factory = CreateFactory(_databasePath);
    }

    [Fact]
    public async Task StartSoloBattle_WithoutSession_ShouldRequireSession()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            HandleCookies = false
        });

        var response = await client.PostAsync("/api/battles/solo", content: null);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Contains("SessionRequired", await response.Content.ReadAsStringAsync(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task StartSoloBattle_WithGuestSession_ShouldReturnSnapshot()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            HandleCookies = false
        });
        var cookie = await CreateGuestCookieAsync(client);

        var response = await SendWithCookieAsync(client, HttpMethod.Post, "/api/battles/solo", cookie);

        response.EnsureSuccessStatusCode();
        var battle = await response.Content.ReadFromJsonAsync<BattleSnapshotResponse>(JsonOptions);
        Assert.NotNull(battle);
        Assert.Equal("Active", battle!.Status);
        Assert.Equal(1000, battle.CpuTowerHp);
        Assert.Contains(battle.StarterDeck, card => card.CardId == "training-knight");
    }

    [Fact]
    public async Task BattleCommand_WithValidDeployAndTick_ShouldChangeElixirAndTowerHp()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            HandleCookies = false
        });
        var cookie = await CreateGuestCookieAsync(client);
        var battle = await StartBattleAsync(client, cookie);

        var deployResponse = await SendJsonWithCookieAsync(
            client,
            $"/api/battles/{battle.BattleId}/commands",
            cookie,
            new DeployRequest("training-knight", "center", 0.5, 0.72));
        deployResponse.EnsureSuccessStatusCode();
        var deployed = await deployResponse.Content.ReadFromJsonAsync<BattleSnapshotResponse>(JsonOptions);

        var tickResponse = await SendJsonWithCookieAsync(
            client,
            $"/api/battles/{battle.BattleId}/tick",
            cookie,
            new TickRequest(10));
        tickResponse.EnsureSuccessStatusCode();
        var advanced = await tickResponse.Content.ReadFromJsonAsync<BattleSnapshotResponse>(JsonOptions);

        Assert.NotNull(deployed);
        Assert.NotNull(advanced);
        Assert.True(deployed!.Elixir < battle.Elixir);
        Assert.True(advanced!.CpuTowerHp < battle.CpuTowerHp);
        Assert.True(advanced.Tick > battle.Tick);
    }

    [Fact]
    public async Task BattleCommand_WithInvalidPlacement_ShouldReturnStableValidationCode()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            HandleCookies = false
        });
        var cookie = await CreateGuestCookieAsync(client);
        var battle = await StartBattleAsync(client, cookie);

        var response = await SendJsonWithCookieAsync(
            client,
            $"/api/battles/{battle.BattleId}/commands",
            cookie,
            new DeployRequest("training-knight", "center", 0.5, 0.25));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("InvalidPlacement", await response.Content.ReadAsStringAsync(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task BattleSnapshot_WhenRequestedByAnotherGuest_ShouldReturnForbidden()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            HandleCookies = false
        });
        var ownerCookie = await CreateGuestCookieAsync(client);
        var battle = await StartBattleAsync(client, ownerCookie);
        var otherCookie = await CreateGuestCookieAsync(client);

        var response = await SendWithCookieAsync(
            client,
            HttpMethod.Get,
            $"/api/battles/{battle.BattleId}",
            otherCookie);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Contains("BattleForbidden", await response.Content.ReadAsStringAsync(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task BattleSnapshot_AfterApiRestartWithSameDatabase_ShouldPersist()
    {
        var databasePath = Path.Combine(Path.GetTempPath(), $"cr-battle-restart-{Guid.NewGuid():N}.db");
        string cookie;
        BattleSnapshotResponse advanced;

        await using (var firstFactory = CreateFactory(databasePath))
        {
            var firstClient = firstFactory.CreateClient(new WebApplicationFactoryClientOptions
            {
                HandleCookies = false
            });
            cookie = await CreateGuestCookieAsync(firstClient);
            var battle = await StartBattleAsync(firstClient, cookie);
            var deploy = await SendJsonWithCookieAsync(
                firstClient,
                $"/api/battles/{battle.BattleId}/commands",
                cookie,
                new DeployRequest("training-knight", "center", 0.5, 0.72));
            deploy.EnsureSuccessStatusCode();
            var tick = await SendJsonWithCookieAsync(
                firstClient,
                $"/api/battles/{battle.BattleId}/tick",
                cookie,
                new TickRequest(10));
            tick.EnsureSuccessStatusCode();
            advanced = (await tick.Content.ReadFromJsonAsync<BattleSnapshotResponse>(JsonOptions))!;
        }

        await using (var secondFactory = CreateFactory(databasePath))
        {
            var secondClient = secondFactory.CreateClient(new WebApplicationFactoryClientOptions
            {
                HandleCookies = false
            });
            var response = await SendWithCookieAsync(
                secondClient,
                HttpMethod.Get,
                $"/api/battles/{advanced.BattleId}",
                cookie);
            response.EnsureSuccessStatusCode();
            var reloaded = await response.Content.ReadFromJsonAsync<BattleSnapshotResponse>(JsonOptions);

            Assert.NotNull(reloaded);
            Assert.Equal(advanced.BattleId, reloaded!.BattleId);
            Assert.Equal(advanced.Tick, reloaded.Tick);
            Assert.Equal(advanced.CpuTowerHp, reloaded.CpuTowerHp);
        }

        DeleteDatabaseFiles(databasePath);
    }

    private static async Task<string> CreateGuestCookieAsync(HttpClient client)
    {
        var response = await client.GetAsync("/api/session");
        response.EnsureSuccessStatusCode();
        return ExtractSessionCookie(response);
    }

    private static async Task<BattleSnapshotResponse> StartBattleAsync(HttpClient client, string cookie)
    {
        var response = await SendWithCookieAsync(client, HttpMethod.Post, "/api/battles/solo", cookie);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<BattleSnapshotResponse>(JsonOptions))!;
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

    private sealed record DeployRequest(string CardId, string Lane, double X, double Y);

    private sealed record TickRequest(int Ticks);

    private sealed record BattleSnapshotResponse(
        Guid BattleId,
        Guid PlayerId,
        string Status,
        string Result,
        int Tick,
        int DurationTicks,
        int PlayerTowerHp,
        int CpuTowerHp,
        int Elixir,
        int MaxElixir,
        List<BattleCardResponse> StarterDeck,
        List<BattleUnitResponse> Units);

    private sealed record BattleCardResponse(string CardId, string Name, int ElixirCost);

    private sealed record BattleUnitResponse(
        string UnitId,
        string CardId,
        string Name,
        string Lane,
        int Position,
        int DamagePerTick);
}

