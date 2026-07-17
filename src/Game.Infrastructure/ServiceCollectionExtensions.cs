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
        return services;
    }

    public static async Task EnsureGameDatabaseAsync(this IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<RoyaleDbContext>();
        await dbContext.Database.EnsureCreatedAsync();
    }
}
