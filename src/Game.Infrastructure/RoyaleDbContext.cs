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
    public DbSet<BattleSessionEntity> BattleSessions => Set<BattleSessionEntity>();
    public DbSet<BattleCommandEntity> BattleCommands => Set<BattleCommandEntity>();

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

        modelBuilder.Entity<BattleSessionEntity>(entity =>
        {
            entity.ToTable("BATTLE_SESSIONS");
            entity.HasKey(x => x.BattleId);
            entity.Property(x => x.BattleId).HasColumnName("BATTLE_ID");
            entity.Property(x => x.PlayerId).HasColumnName("PLAYER_ID").IsRequired();
            entity.Property(x => x.Status).HasColumnName("STATUS").IsRequired();
            entity.Property(x => x.SnapshotJson).HasColumnName("SNAPSHOT_JSON").IsRequired();
            entity.Property(x => x.CreatedAt).HasColumnName("CREATED_AT").IsRequired();
            entity.Property(x => x.UpdatedAt).HasColumnName("UPDATED_AT").IsRequired();
            entity.Property(x => x.EndedAt).HasColumnName("ENDED_AT");
            entity.HasIndex(x => x.PlayerId);
        });

        modelBuilder.Entity<BattleCommandEntity>(entity =>
        {
            entity.ToTable("BATTLE_COMMANDS");
            entity.HasKey(x => x.CommandId);
            entity.Property(x => x.CommandId).HasColumnName("COMMAND_ID");
            entity.Property(x => x.BattleId).HasColumnName("BATTLE_ID").IsRequired();
            entity.Property(x => x.PlayerId).HasColumnName("PLAYER_ID").IsRequired();
            entity.Property(x => x.CommandType).HasColumnName("COMMAND_TYPE").IsRequired();
            entity.Property(x => x.CommandJson).HasColumnName("COMMAND_JSON").IsRequired();
            entity.Property(x => x.SubmittedAtTick).HasColumnName("SUBMITTED_AT_TICK").IsRequired();
            entity.Property(x => x.CreatedAt).HasColumnName("CREATED_AT").IsRequired();
            entity.Property(x => x.RejectedCode).HasColumnName("REJECTED_CODE");
            entity.HasIndex(x => x.BattleId);
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

public sealed class BattleSessionEntity
{
    public Guid BattleId { get; set; }
    public Guid PlayerId { get; set; }
    public string Status { get; set; } = string.Empty;
    public string SnapshotJson { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public DateTimeOffset? EndedAt { get; set; }
}

public sealed class BattleCommandEntity
{
    public Guid CommandId { get; set; }
    public Guid BattleId { get; set; }
    public Guid PlayerId { get; set; }
    public string CommandType { get; set; } = string.Empty;
    public string CommandJson { get; set; } = string.Empty;
    public int SubmittedAtTick { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public string? RejectedCode { get; set; }
}

