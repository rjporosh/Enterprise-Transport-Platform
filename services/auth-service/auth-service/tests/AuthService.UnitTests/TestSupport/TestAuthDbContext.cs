using AuthService.Application.Common.Interfaces;
using AuthService.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AuthService.UnitTests.TestSupport;

/// <summary>
/// EF Core InMemory-backed context implementing the same IAuthDbContext
/// port the real AuthDbContext implements — lets handlers be tested against
/// real LINQ/EF behavior (Include, projections, uniqueness checks) without
/// needing Postgres. Provider-specific behavior is instead covered by the
/// Testcontainers-based integration tests.
/// </summary>
public sealed class TestAuthDbContext : DbContext, IAuthDbContext
{
    public TestAuthDbContext(DbContextOptions<TestAuthDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<UserRole> UserRoles => Set<UserRole>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(builder =>
        {
            builder.HasKey(x => x.Id);
            builder.HasIndex(x => x.Email).IsUnique();
            builder.HasMany(x => x.UserRoles).WithOne(ur => ur.User).HasForeignKey(ur => ur.UserId);
            builder.Ignore(x => x.DomainEvents);
            builder.Ignore(x => x.Version);
        });

        modelBuilder.Entity<Role>().HasKey(x => x.Id);

        modelBuilder.Entity<UserRole>(builder =>
        {
            builder.HasKey(x => new { x.UserId, x.RoleId });
            builder.HasOne(x => x.Role).WithMany().HasForeignKey(x => x.RoleId);
        });

        modelBuilder.Entity<RefreshToken>().HasKey(x => x.Id);
        modelBuilder.Entity<AuditLog>().HasKey(x => x.Id);
    }
}
