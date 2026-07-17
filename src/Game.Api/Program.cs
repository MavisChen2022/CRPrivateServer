using Game.Application;
using Game.Infrastructure;

var builder = WebApplication.CreateBuilder(args);
var connectionString = builder.Configuration.GetConnectionString("GameDatabase")
    ?? "Data Source=data/royale.db";
EnsureSqliteDirectoryExists(connectionString);

builder.Services.AddGameInfrastructure(connectionString);
builder.Services.AddScoped<GuestSessionService>();

var app = builder.Build();
await app.Services.EnsureGameDatabaseAsync();

app.MapGet("/", () => Results.Redirect("/api/session"));

app.MapGet("/api/health", () => Results.Ok(new { status = "ok" }));

app.MapGet("/api/session", async (HttpContext context, GuestSessionService sessions) =>
{
    var rawToken = context.Request.Cookies["royale_session"];
    GuestSessionResult result;

    try
    {
        result = await sessions.GetOrCreateAsync(
            rawToken,
            DateTimeOffset.UtcNow,
            context.RequestAborted);
    }
    catch (Exception)
    {
        return Results.Problem(
            title: "Session store unavailable",
            detail: "The guest session store is temporarily unavailable.",
            statusCode: StatusCodes.Status503ServiceUnavailable,
            extensions: new Dictionary<string, object?>
            {
                ["code"] = "SessionStoreUnavailable"
            });
    }

    if (result.WasCreated)
    {
        context.Response.Cookies.Append("royale_session", result.RawSessionToken, new CookieOptions
        {
            HttpOnly = true,
            SameSite = SameSiteMode.Lax,
            Secure = !app.Environment.IsDevelopment(),
            Expires = DateTimeOffset.UtcNow.AddDays(30)
        });
    }

    return Results.Ok(new
    {
        playerId = result.Player.PlayerId,
        displayName = result.Player.DisplayName,
        trophies = result.Player.Trophies,
        gold = result.Player.Gold,
        accountType = result.Player.AccountType.ToString(),
        guestWarning = "Clearing browser data can lose access to this guest."
    });
});

app.Run();

static void EnsureSqliteDirectoryExists(string connectionString)
{
    const string prefix = "Data Source=";
    if (!connectionString.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
    {
        return;
    }

    var path = connectionString[prefix.Length..].Trim();
    if (string.IsNullOrWhiteSpace(Path.GetDirectoryName(path)))
    {
        return;
    }

    Directory.CreateDirectory(Path.GetDirectoryName(path)!);
}

public partial class Program
{
}
