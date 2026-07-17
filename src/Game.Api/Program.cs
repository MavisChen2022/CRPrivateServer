using Game.Application;
using Game.Domain;
using Game.Infrastructure;

var builder = WebApplication.CreateBuilder(args);
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
var connectionString = builder.Configuration.GetConnectionString("GameDatabase")
    ?? "Data Source=data/royale.db";
EnsureSqliteDirectoryExists(connectionString);

builder.Services.AddGameInfrastructure(connectionString);
builder.Services.AddScoped<GuestSessionService>();
builder.Services.AddSingleton<SoloBattleEngine>();
builder.Services.AddScoped<SoloBattleService>();

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

app.MapPost("/api/battles/solo", async (
    HttpContext context,
    GuestSessionService sessions,
    SoloBattleService battles) =>
{
    var player = await ResolvePlayerAsync(context, sessions);
    if (player is null)
    {
        return SessionRequired();
    }

    try
    {
        var result = await battles.StartOrResumeAsync(player.PlayerId, DateTimeOffset.UtcNow, context.RequestAborted);
        return Results.Ok(result.Snapshot);
    }
    catch (Exception)
    {
        return BattleProblem("BattleStoreUnavailable", "The battle store is temporarily unavailable.", 503);
    }
});

app.MapGet("/api/battles/{battleId:guid}", async (
    Guid battleId,
    HttpContext context,
    GuestSessionService sessions,
    SoloBattleService battles) =>
{
    var player = await ResolvePlayerAsync(context, sessions);
    if (player is null)
    {
        return SessionRequired();
    }

    try
    {
        return ToBattleResult(await battles.GetSnapshotAsync(player.PlayerId, battleId, context.RequestAborted));
    }
    catch (Exception)
    {
        return BattleProblem("BattleStoreUnavailable", "The battle store is temporarily unavailable.", 503);
    }
});

app.MapPost("/api/battles/{battleId:guid}/commands", async (
    Guid battleId,
    DeployBattleRequest request,
    HttpContext context,
    GuestSessionService sessions,
    SoloBattleService battles) =>
{
    var player = await ResolvePlayerAsync(context, sessions);
    if (player is null)
    {
        return SessionRequired();
    }

    try
    {
        var result = await battles.SubmitDeployAsync(
            player.PlayerId,
            battleId,
            new DeployBattleCommand(request.CardId, request.Lane, request.X, request.Y),
            DateTimeOffset.UtcNow,
            context.RequestAborted);
        return ToBattleResult(result);
    }
    catch (Exception)
    {
        return BattleProblem("BattleStoreUnavailable", "The battle store is temporarily unavailable.", 503);
    }
});

app.MapPost("/api/battles/{battleId:guid}/tick", async (
    Guid battleId,
    TickBattleRequest request,
    HttpContext context,
    GuestSessionService sessions,
    SoloBattleService battles) =>
{
    var player = await ResolvePlayerAsync(context, sessions);
    if (player is null)
    {
        return SessionRequired();
    }

    try
    {
        return ToBattleResult(await battles.AdvanceAsync(
            player.PlayerId,
            battleId,
            request.Ticks,
            DateTimeOffset.UtcNow,
            context.RequestAborted));
    }
    catch (Exception)
    {
        return BattleProblem("BattleStoreUnavailable", "The battle store is temporarily unavailable.", 503);
    }
});

app.Run();

static async Task<Game.Domain.PlayerProfile?> ResolvePlayerAsync(
    HttpContext context,
    GuestSessionService sessions)
{
    return await sessions.GetExistingPlayerAsync(
        context.Request.Cookies["royale_session"],
        DateTimeOffset.UtcNow,
        context.RequestAborted);
}

static IResult SessionRequired()
{
    return Results.Problem(
        title: "Session required",
        detail: "A valid guest session is required for battle endpoints.",
        statusCode: StatusCodes.Status401Unauthorized,
        extensions: new Dictionary<string, object?>
        {
            ["code"] = "SessionRequired"
        });
}

static IResult ToBattleResult(BattleServiceResult result)
{
    if (result.Succeeded)
    {
        return Results.Ok(result.Snapshot);
    }

    return BattleProblem(
        result.ErrorCode ?? "BattleUnexpectedError",
        "The battle request could not be completed.",
        result.StatusCode,
        result.Snapshot);
}

static IResult BattleProblem(string code, string detail, int statusCode, BattleSnapshot? snapshot = null)
{
    return Results.Problem(
        title: code,
        detail: detail,
        statusCode: statusCode,
        extensions: new Dictionary<string, object?>
        {
            ["code"] = code,
            ["snapshot"] = snapshot
        });
}

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

public sealed record DeployBattleRequest(string CardId, string Lane, double X, double Y);

public sealed record TickBattleRequest(int Ticks);
