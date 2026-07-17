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
builder.Services.AddSingleton<OnlineBattleEngine>();
builder.Services.AddScoped<SoloBattleService>();
builder.Services.AddScoped<FriendService>();
builder.Services.AddScoped<OnlineBattleService>();
builder.Services.AddScoped<FriendlyBattleService>();

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

app.MapGet("/api/friends", async (
    HttpContext context,
    GuestSessionService sessions,
    FriendService friends) =>
{
    var player = await ResolvePlayerAsync(context, sessions);
    if (player is null)
    {
        return SessionRequired();
    }

    try
    {
        return Results.Ok(await friends.GetSnapshotAsync(player.PlayerId, DateTimeOffset.UtcNow, context.RequestAborted));
    }
    catch (Exception)
    {
        return FriendProblem("FriendStoreUnavailable", "The friend store is temporarily unavailable.", 503);
    }
});

app.MapPost("/api/friends/requests", async (
    FriendRequestBody request,
    HttpContext context,
    GuestSessionService sessions,
    FriendService friends) =>
{
    var player = await ResolvePlayerAsync(context, sessions);
    if (player is null)
    {
        return SessionRequired();
    }

    try
    {
        return ToFriendResult(await friends.CreateRequestAsync(
            player.PlayerId,
            request.FriendCode,
            DateTimeOffset.UtcNow,
            context.RequestAborted));
    }
    catch (Exception)
    {
        return FriendProblem("FriendStoreUnavailable", "The friend store is temporarily unavailable.", 503);
    }
});

app.MapPost("/api/friends/requests/{friendshipId:guid}/accept", async (
    Guid friendshipId,
    HttpContext context,
    GuestSessionService sessions,
    FriendService friends) =>
{
    var player = await ResolvePlayerAsync(context, sessions);
    if (player is null)
    {
        return SessionRequired();
    }

    try
    {
        return ToFriendResult(await friends.AcceptAsync(
            player.PlayerId,
            friendshipId,
            DateTimeOffset.UtcNow,
            context.RequestAborted));
    }
    catch (Exception)
    {
        return FriendProblem("FriendStoreUnavailable", "The friend store is temporarily unavailable.", 503);
    }
});

app.MapPost("/api/friends/requests/{friendshipId:guid}/reject", async (
    Guid friendshipId,
    HttpContext context,
    GuestSessionService sessions,
    FriendService friends) =>
{
    var player = await ResolvePlayerAsync(context, sessions);
    if (player is null)
    {
        return SessionRequired();
    }

    try
    {
        return ToFriendResult(await friends.RejectAsync(
            player.PlayerId,
            friendshipId,
            DateTimeOffset.UtcNow,
            context.RequestAborted));
    }
    catch (Exception)
    {
        return FriendProblem("FriendStoreUnavailable", "The friend store is temporarily unavailable.", 503);
    }
});

app.MapPost("/api/online-battles/matchmaking", async (
    HttpContext context,
    GuestSessionService sessions,
    OnlineBattleService onlineBattles) =>
{
    var player = await ResolvePlayerAsync(context, sessions);
    if (player is null)
    {
        return SessionRequired();
    }

    try
    {
        return ToOnlineBattleResult(await onlineBattles.QueueAsync(
            player.PlayerId,
            player.DisplayName,
            DateTimeOffset.UtcNow,
            context.RequestAborted));
    }
    catch (Exception)
    {
        return OnlineBattleProblem("OnlineBattleStoreUnavailable", "The online battle store is temporarily unavailable.", 503);
    }
});

app.MapDelete("/api/online-battles/matchmaking", async (
    HttpContext context,
    GuestSessionService sessions,
    OnlineBattleService onlineBattles) =>
{
    var player = await ResolvePlayerAsync(context, sessions);
    if (player is null)
    {
        return SessionRequired();
    }

    try
    {
        return ToOnlineBattleResult(await onlineBattles.CancelQueueAsync(
            player.PlayerId,
            DateTimeOffset.UtcNow,
            context.RequestAborted));
    }
    catch (Exception)
    {
        return OnlineBattleProblem("OnlineBattleStoreUnavailable", "The online battle store is temporarily unavailable.", 503);
    }
});

app.MapGet("/api/online-battles/current", async (
    HttpContext context,
    GuestSessionService sessions,
    OnlineBattleService onlineBattles) =>
{
    var player = await ResolvePlayerAsync(context, sessions);
    if (player is null)
    {
        return SessionRequired();
    }

    try
    {
        return ToOnlineBattleResult(await onlineBattles.GetCurrentAsync(
            player.PlayerId,
            context.RequestAborted));
    }
    catch (Exception)
    {
        return OnlineBattleProblem("OnlineBattleStoreUnavailable", "The online battle store is temporarily unavailable.", 503);
    }
});

app.MapGet("/api/online-battles/{roomId:guid}", async (
    Guid roomId,
    HttpContext context,
    GuestSessionService sessions,
    OnlineBattleService onlineBattles) =>
{
    var player = await ResolvePlayerAsync(context, sessions);
    if (player is null)
    {
        return SessionRequired();
    }

    try
    {
        return ToOnlineBattleResult(await onlineBattles.GetRoomAsync(
            player.PlayerId,
            roomId,
            context.RequestAborted));
    }
    catch (Exception)
    {
        return OnlineBattleProblem("OnlineBattleStoreUnavailable", "The online battle store is temporarily unavailable.", 503);
    }
});

app.MapPost("/api/online-battles/{roomId:guid}/commands", async (
    Guid roomId,
    DeployBattleRequest request,
    HttpContext context,
    GuestSessionService sessions,
    OnlineBattleService onlineBattles) =>
{
    var player = await ResolvePlayerAsync(context, sessions);
    if (player is null)
    {
        return SessionRequired();
    }

    try
    {
        return ToOnlineBattleResult(await onlineBattles.SubmitDeployAsync(
            player.PlayerId,
            roomId,
            new DeployBattleCommand(request.CardId, request.Lane, request.X, request.Y),
            DateTimeOffset.UtcNow,
            context.RequestAborted));
    }
    catch (Exception)
    {
        return OnlineBattleProblem("OnlineBattleStoreUnavailable", "The online battle store is temporarily unavailable.", 503);
    }
});

app.MapPost("/api/online-battles/{roomId:guid}/tick", async (
    Guid roomId,
    TickBattleRequest request,
    HttpContext context,
    GuestSessionService sessions,
    OnlineBattleService onlineBattles) =>
{
    var player = await ResolvePlayerAsync(context, sessions);
    if (player is null)
    {
        return SessionRequired();
    }

    try
    {
        return ToOnlineBattleResult(await onlineBattles.AdvanceAsync(
            player.PlayerId,
            roomId,
            request.Ticks,
            DateTimeOffset.UtcNow,
            context.RequestAborted));
    }
    catch (Exception)
    {
        return OnlineBattleProblem("OnlineBattleStoreUnavailable", "The online battle store is temporarily unavailable.", 503);
    }
});

app.MapGet("/api/friendly-battles/current", async (
    HttpContext context,
    GuestSessionService sessions,
    FriendlyBattleService friendlyBattles) =>
{
    var player = await ResolvePlayerAsync(context, sessions);
    if (player is null)
    {
        return SessionRequired();
    }

    try
    {
        return Results.Ok(await friendlyBattles.GetSnapshotAsync(
            player.PlayerId,
            DateTimeOffset.UtcNow,
            context.RequestAborted));
    }
    catch (Exception)
    {
        return FriendlyBattleProblem("FriendlyBattleStoreUnavailable", "The friendly battle store is temporarily unavailable.", 503);
    }
});

app.MapPost("/api/friendly-battles/invites", async (
    FriendlyBattleInviteBody request,
    HttpContext context,
    GuestSessionService sessions,
    FriendlyBattleService friendlyBattles) =>
{
    var player = await ResolvePlayerAsync(context, sessions);
    if (player is null)
    {
        return SessionRequired();
    }

    try
    {
        return ToFriendlyBattleResult(await friendlyBattles.CreateInviteAsync(
            player.PlayerId,
            request.FriendPlayerId,
            DateTimeOffset.UtcNow,
            context.RequestAborted));
    }
    catch (Exception)
    {
        return FriendlyBattleProblem("FriendlyBattleStoreUnavailable", "The friendly battle store is temporarily unavailable.", 503);
    }
});

app.MapPost("/api/friendly-battles/invites/{inviteId:guid}/accept", async (
    Guid inviteId,
    HttpContext context,
    GuestSessionService sessions,
    FriendlyBattleService friendlyBattles) =>
{
    var player = await ResolvePlayerAsync(context, sessions);
    if (player is null)
    {
        return SessionRequired();
    }

    try
    {
        return ToFriendlyBattleResult(await friendlyBattles.AcceptAsync(
            player.PlayerId,
            inviteId,
            DateTimeOffset.UtcNow,
            context.RequestAborted));
    }
    catch (Exception)
    {
        return FriendlyBattleProblem("FriendlyBattleStoreUnavailable", "The friendly battle store is temporarily unavailable.", 503);
    }
});

app.MapPost("/api/friendly-battles/invites/{inviteId:guid}/reject", async (
    Guid inviteId,
    HttpContext context,
    GuestSessionService sessions,
    FriendlyBattleService friendlyBattles) =>
{
    var player = await ResolvePlayerAsync(context, sessions);
    if (player is null)
    {
        return SessionRequired();
    }

    try
    {
        return ToFriendlyBattleResult(await friendlyBattles.RejectAsync(
            player.PlayerId,
            inviteId,
            DateTimeOffset.UtcNow,
            context.RequestAborted));
    }
    catch (Exception)
    {
        return FriendlyBattleProblem("FriendlyBattleStoreUnavailable", "The friendly battle store is temporarily unavailable.", 503);
    }
});

app.MapDelete("/api/friendly-battles/invites/{inviteId:guid}", async (
    Guid inviteId,
    HttpContext context,
    GuestSessionService sessions,
    FriendlyBattleService friendlyBattles) =>
{
    var player = await ResolvePlayerAsync(context, sessions);
    if (player is null)
    {
        return SessionRequired();
    }

    try
    {
        return ToFriendlyBattleResult(await friendlyBattles.CancelAsync(
            player.PlayerId,
            inviteId,
            DateTimeOffset.UtcNow,
            context.RequestAborted));
    }
    catch (Exception)
    {
        return FriendlyBattleProblem("FriendlyBattleStoreUnavailable", "The friendly battle store is temporarily unavailable.", 503);
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

static IResult ToFriendResult(FriendServiceResult result)
{
    if (result.Succeeded)
    {
        return Results.Ok(result.Snapshot);
    }

    return FriendProblem(
        result.ErrorCode ?? "FriendUnexpectedError",
        "The friend request could not be completed.",
        result.StatusCode,
        result.Snapshot);
}

static IResult FriendProblem(string code, string detail, int statusCode, FriendsSnapshot? snapshot = null)
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

static IResult ToOnlineBattleResult(OnlineBattleServiceResult result)
{
    if (result.Succeeded)
    {
        return Results.Ok(result.State);
    }

    return OnlineBattleProblem(
        result.ErrorCode ?? "OnlineBattleUnexpectedError",
        "The online battle request could not be completed.",
        result.StatusCode,
        result.State);
}

static IResult OnlineBattleProblem(string code, string detail, int statusCode, OnlineBattleState? state = null)
{
    return Results.Problem(
        title: code,
        detail: detail,
        statusCode: statusCode,
        extensions: new Dictionary<string, object?>
        {
            ["code"] = code,
            ["state"] = state
        });
}

static IResult ToFriendlyBattleResult(FriendlyBattleServiceResult result)
{
    if (result.Succeeded)
    {
        return Results.Ok(result.Snapshot);
    }

    return FriendlyBattleProblem(
        result.ErrorCode ?? "FriendlyBattleUnexpectedError",
        "The friendly battle request could not be completed.",
        result.StatusCode,
        result.Snapshot);
}

static IResult FriendlyBattleProblem(string code, string detail, int statusCode, FriendlyBattleSnapshot? snapshot = null)
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

public sealed record FriendRequestBody(string FriendCode);

public sealed record FriendlyBattleInviteBody(Guid FriendPlayerId);
