using Game.Application;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Game.Infrastructure;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddGameInfrastructure(
        this IServiceCollection services,
        string connectionString)
    {
        services.AddDbContext<RoyaleDbContext>(options => options.UseSqlite(connectionString));
        services.AddScoped<IGuestSessionStore, EfGuestSessionStore>();
        services.AddScoped<ISoloBattleStore, EfSoloBattleStore>();
        services.AddScoped<IFriendStore, EfFriendStore>();
        return services;
    }

    public static async Task EnsureGameDatabaseAsync(this IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<RoyaleDbContext>();
        await dbContext.Database.EnsureCreatedAsync();
        await dbContext.Database.ExecuteSqlRawAsync("""
            CREATE TABLE IF NOT EXISTS "FRIEND_CODES" (
                "PLAYER_ID" TEXT NOT NULL CONSTRAINT "PK_FRIEND_CODES" PRIMARY KEY,
                "FRIEND_CODE" TEXT NOT NULL,
                "CREATED_AT" TEXT NOT NULL
            );

            CREATE UNIQUE INDEX IF NOT EXISTS "IX_FRIEND_CODES_FRIEND_CODE"
            ON "FRIEND_CODES" ("FRIEND_CODE");

            CREATE TABLE IF NOT EXISTS "FRIENDSHIPS" (
                "FRIENDSHIP_ID" TEXT NOT NULL CONSTRAINT "PK_FRIENDSHIPS" PRIMARY KEY,
                "REQUESTER_PLAYER_ID" TEXT NOT NULL,
                "ADDRESSEE_PLAYER_ID" TEXT NOT NULL,
                "LOWER_PLAYER_ID" TEXT NOT NULL,
                "HIGHER_PLAYER_ID" TEXT NOT NULL,
                "STATUS" TEXT NOT NULL,
                "CREATED_AT" TEXT NOT NULL,
                "UPDATED_AT" TEXT NOT NULL
            );

            CREATE UNIQUE INDEX IF NOT EXISTS "IX_FRIENDSHIPS_LOWER_PLAYER_ID_HIGHER_PLAYER_ID"
            ON "FRIENDSHIPS" ("LOWER_PLAYER_ID", "HIGHER_PLAYER_ID");

            CREATE INDEX IF NOT EXISTS "IX_FRIENDSHIPS_REQUESTER_PLAYER_ID"
            ON "FRIENDSHIPS" ("REQUESTER_PLAYER_ID");

            CREATE INDEX IF NOT EXISTS "IX_FRIENDSHIPS_ADDRESSEE_PLAYER_ID"
            ON "FRIENDSHIPS" ("ADDRESSEE_PLAYER_ID");
            """);
    }
}
