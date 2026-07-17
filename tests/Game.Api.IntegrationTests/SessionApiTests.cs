using System.Net.Http.Json;
using System.Text.Json;
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

    private static string ExtractSessionCookie(HttpResponseMessage response)
    {
        var setCookie = response.Headers.GetValues("Set-Cookie")
            .Single(x => x.StartsWith("royale_session=", StringComparison.Ordinal));
        return setCookie.Split(';')[0];
    }

    public void Dispose()
    {
        _factory.Dispose();
        if (File.Exists(_databasePath))
        {
            File.Delete(_databasePath);
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
