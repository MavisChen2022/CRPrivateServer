using Microsoft.EntityFrameworkCore;

namespace Game.Infrastructure;

public sealed class RoyaleDbContext : DbContext
{
    public RoyaleDbContext(DbContextOptions<RoyaleDbContext> options)
        : base(options)
    {
    }

    public DbSet<UserAccountEntity> UserAccounts => Set<UserAccountEntity>();
    public DbSet<PlayerProfileEntity> PlayerProfiles => Set<PlayerProfileEntity>();
    public DbSet<SessionTokenEntity> SessionTokens => Set<SessionTokenEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<UserAccountEntity>(entity =>
        {
            entity.ToTable("USER_ACCOUNTS");
            entity.HasKey(x => x.UserId);
            entity.Property(x => x.UserId).HasColumnName("USER_ID");
            entity.Property(x => x.AccountType).HasColumnName("ACCOUNT_TYPE").IsRequired();
            entity.Property(x => x.Status).HasColumnName("STATUS").IsRequired();
            entity.Property(x => x.CreatedAt).HasColumnName("CREATED_AT").IsRequired();
        });

        modelBuilder.Entity<PlayerProfileEntity>(entity =>
        {
            entity.ToTable("PLAYER_PROFILES");
            entity.HasKey(x => x.PlayerId);
            entity.Property(x => x.PlayerId).HasColumnName("PLAYER_ID");
            entity.Property(x => x.UserId).HasColumnName("USER_ID").IsRequired();
            entity.Property(x => x.DisplayName).HasColumnName("DISPLAY_NAME").IsRequired();
            entity.Property(x => x.Trophies).HasColumnName("TROPHIES").IsRequired();
            entity.Property(x => x.Gold).HasColumnName("GOLD").IsRequired();
            entity.HasIndex(x => x.UserId).IsUnique();
        });

        modelBuilder.Entity<SessionTokenEntity>(entity =>
        {
            entity.ToTable("SESSION_TOKENS");
            entity.HasKey(x => x.TokenId);
            entity.Property(x => x.TokenId).HasColumnName("TOKEN_ID");
            entity.Property(x => x.UserId).HasColumnName("USER_ID").IsRequired();
            entity.Property(x => x.TokenHash).HasColumnName("TOKEN_HASH").IsRequired();
            entity.Property(x => x.CreatedAt).HasColumnName("CREATED_AT").IsRequired();
            entity.Property(x => x.ExpiresAt).HasColumnName("EXPIRES_AT").IsRequired();
            entity.Property(x => x.RevokedAt).HasColumnName("REVOKED_AT");
            entity.HasIndex(x => x.TokenHash).IsUnique();
        });
    }
}

public sealed class UserAccountEntity
{
    public Guid UserId { get; set; }
    public string AccountType { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
}

public sealed class PlayerProfileEntity
{
    public Guid PlayerId { get; set; }
    public Guid UserId { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public int Trophies { get; set; }
    public int Gold { get; set; }
}

public sealed class SessionTokenEntity
{
    public Guid TokenId { get; set; }
    public Guid UserId { get; set; }
    public string TokenHash { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset ExpiresAt { get; set; }
    public DateTimeOffset? RevokedAt { get; set; }
}

