var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSingleton<Game.Application.GuestSessionService>();

var app = builder.Build();

app.MapGet("/", () => Results.Redirect("/api/session"));

app.MapGet("/api/health", () => Results.Ok(new { status = "ok" }));

app.MapGet("/api/session", (HttpContext context, Game.Application.GuestSessionService sessions) =>
{
    var rawToken = context.Request.Cookies["royale_session"];
    var result = sessions.GetOrCreate(rawToken, DateTimeOffset.UtcNow);

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
