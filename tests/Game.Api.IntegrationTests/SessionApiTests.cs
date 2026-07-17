using Game.Domain;
using Microsoft.Data.Sqlite;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace Game.Api.IntegrationTests;

public sealed class SessionApiTests : IDisposable
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly string _databasePath;
    private readonly WebApplicationFactory<Program> _factory;

    public SessionApiTests()
    {
        _databasePath = Path.Combine(Path.GetTempPath(), $"cr-session-{Guid.NewGuid():N}.db");
        _factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseSetting("ConnectionStrings:GameDatabase", $"Data Source={_databasePath}");
                builder.ConfigureAppConfiguration((_, configuration) =>
                {
                    configuration.AddInMemoryCollection(new Dictionary<string, string?>
                    {
                        ["ConnectionStrings:GameDatabase"] = $"Data Source={_databasePath}"
                    });
                });
            });
    }

    [Fact]
    public async Task GetSession_WhenNoCookie_ShouldCreateGuestAndSetCookie()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            HandleCookies = false
        });

        var response = await client.GetAsync("/api/session");

        response.EnsureSuccessStatusCode();
        Assert.Contains(response.Headers.GetValues("Set-Cookie"), x =>
            x.Contains("royale_session=", StringComparison.Ordinal) &&
            x.Contains("httponly", StringComparison.OrdinalIgnoreCase) &&
            x.Contains("samesite=lax", StringComparison.OrdinalIgnoreCase));

        var body = await response.Content.ReadFromJsonAsync<SessionResponse>(JsonOptions);
        Assert.NotNull(body);
        Assert.Equal("Guest", body!.AccountType);
        Assert.StartsWith("Player", body.DisplayName, StringComparison.Ordinal);
        Assert.DoesNotContain("token", await response.Content.ReadAsStringAsync(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GetSession_WhenCookieIsReused_ShouldReturnSamePlayer()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            HandleCookies = false
        });

        var firstResponse = await client.GetAsync("/api/session");
        var cookie = ExtractSessionCookie(firstResponse);
        var secondRequest = new HttpRequestMessage(HttpMethod.Get, "/api/session");
        secondRequest.Headers.Add("Cookie", cookie);
        var secondResponse = await client.SendAsync(secondRequest);
        secondResponse.EnsureSuccessStatusCode();

        var first = await firstResponse.Content.ReadFromJsonAsync<SessionResponse>(JsonOptions);
        var second = await secondResponse.Content.ReadFromJsonAsync<SessionResponse>(JsonOptions);

        Assert.NotNull(first);
        Assert.NotNull(second);
        Assert.Equal(first!.PlayerId, second!.PlayerId);
    }

    [Fact]
    public async Task GetSession_WhenCookieIsTampered_ShouldCreateReplacementGuest()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            HandleCookies = false
        });

        var first = await client.GetFromJsonAsync<SessionResponse>("/api/session", JsonOptions);
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/session");
        request.Headers.Add("Cookie", "royale_session=tampered-token");
        var secondResponse = await client.SendAsync(request);
        secondResponse.EnsureSuccessStatusCode();
        var second = await secondResponse.Content.ReadFromJsonAsync<SessionResponse>(JsonOptions);

        Assert.NotNull(first);
        Assert.NotNull(second);
        Assert.NotEqual(first!.PlayerId, second!.PlayerId);
    }

    [Fact]
    public async Task GetSession_WhenCookieIsExpired_ShouldCreateReplacementGuest()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            HandleCookies = false
        });
        var health = await client.GetAsync("/api/health");
        health.EnsureSuccessStatusCode();
        const string expiredRawToken = "expired-token";
        var expiredPlayerId = Guid.NewGuid();
        await SeedExpiredGuestSessionAsync(expiredRawToken, expiredPlayerId);

        var request = new HttpRequestMessage(HttpMethod.Get, "/api/session");
        request.Headers.Add("Cookie", $"royale_session={expiredRawToken}");
        var response = await client.SendAsync(request);

        response.EnsureSuccessStatusCode();
        Assert.Contains(response.Headers.GetValues("Set-Cookie"), x =>
            x.StartsWith("royale_session=", StringComparison.Ordinal) &&
            !x.StartsWith($"royale_session={expiredRawToken}", StringComparison.Ordinal));
        var body = await response.Content.ReadFromJsonAsync<SessionResponse>(JsonOptions);
        Assert.NotNull(body);
        Assert.NotEqual(expiredPlayerId, body!.PlayerId);
    }

    private static string ExtractSessionCookie(HttpResponseMessage response)
    {
        var setCookie = response.Headers.GetValues("Set-Cookie")
            .Single(x => x.StartsWith("royale_session=", StringComparison.Ordinal));
        return setCookie.Split(';')[0];
    }

    private async Task SeedExpiredGuestSessionAsync(string rawToken, Guid playerId)
    {
        var userId = Guid.NewGuid();
        await using var connection = new SqliteConnection($"Data Source={_databasePath}");
        await connection.OpenAsync();

        await using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO USER_ACCOUNTS (USER_ID, ACCOUNT_TYPE, CREATED_AT, STATUS)
            VALUES ($userId, 'Guest', $createdAt, 'Active');

            INSERT INTO PLAYER_PROFILES (PLAYER_ID, USER_ID, DISPLAY_NAME, TROPHIES, GOLD)
            VALUES ($playerId, $userId, 'Player000001', 0, 100);

            INSERT INTO SESSION_TOKENS (TOKEN_ID, USER_ID, TOKEN_HASH, CREATED_AT, EXPIRES_AT, REVOKED_AT)
            VALUES ($tokenId, $userId, $tokenHash, $createdAt, $expiresAt, NULL);
            """;
        command.Parameters.AddWithValue("$userId", userId);
        command.Parameters.AddWithValue("$playerId", playerId);
        command.Parameters.AddWithValue("$tokenId", Guid.NewGuid());
        command.Parameters.AddWithValue("$tokenHash", SessionToken.Hash(rawToken));
        command.Parameters.AddWithValue("$createdAt", DateTimeOffset.UtcNow.AddDays(-31));
        command.Parameters.AddWithValue("$expiresAt", DateTimeOffset.UtcNow.AddDays(-1));
        await command.ExecuteNonQueryAsync();
    }

    public void Dispose()
    {
        _factory.Dispose();
        foreach (var path in new[] { _databasePath, $"{_databasePath}-shm", $"{_databasePath}-wal" })
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

    private sealed record SessionResponse(
        Guid PlayerId,
        string DisplayName,
        int Trophies,
        int Gold,
        string AccountType,
        string GuestWarning);
}
