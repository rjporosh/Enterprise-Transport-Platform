using AuthService.Application.Common.Interfaces;
using AuthService.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AuthService.UnitTests.TestSupport;

public sealed class TestAuthDbContext : DbContext, IAuthDbContext
{
    public TestAuthDbContext(DbContextOptions<TestAuthDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<UserRole> UserRoles => Set<UserRole>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<Permission> Permissions => Set<Permission>();
    public DbSet<AuthService.Domain.Entities.Module> Modules => Set<AuthService.Domain.Entities.Module>();
    public DbSet<Policy> Policies => Set<Policy>();
    public DbSet<Claim> Claims => Set<Claim>();
    public DbSet<OtpRecord> OtpRecords => Set<OtpRecord>();
    public DbSet<SecurityQuestion> SecurityQuestions => Set<SecurityQuestion>();
    public DbSet<SecurityAnswer> SecurityAnswers => Set<SecurityAnswer>();
    public DbSet<PasswordHistory> PasswordHistories => Set<PasswordHistory>();
    public DbSet<PasswordResetToken> PasswordResetTokens => Set<PasswordResetToken>();
    public DbSet<UserSession> UserSessions => Set<UserSession>();
    public DbSet<UserClaim> UserClaims => Set<UserClaim>();
    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();
    public DbSet<ModulePermission> ModulePermissions => Set<ModulePermission>();
    public DbSet<UserSecurityQuestion> UserSecurityQuestions => Set<UserSecurityQuestion>();

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
        modelBuilder.Entity<Permission>().HasKey(x => x.Id);
        modelBuilder.Entity<AuthService.Domain.Entities.Module>().HasKey(x => x.Id);
        modelBuilder.Entity<Policy>().HasKey(x => x.Id);
        modelBuilder.Entity<Claim>().HasKey(x => x.Id);
        modelBuilder.Entity<OtpRecord>().HasKey(x => x.Id);
        modelBuilder.Entity<SecurityQuestion>().HasKey(x => x.Id);
        modelBuilder.Entity<SecurityAnswer>().HasKey(x => x.Id);
        modelBuilder.Entity<PasswordHistory>().HasKey(x => x.Id);
        modelBuilder.Entity<PasswordResetToken>().HasKey(x => x.Id);
        modelBuilder.Entity<UserSession>().HasKey(x => x.Id);
        modelBuilder.Entity<UserClaim>().HasKey(x => new { x.UserId, x.Type, x.Value });
        modelBuilder.Entity<RolePermission>().HasKey(x => new { x.RoleId, x.PermissionId });
        modelBuilder.Entity<ModulePermission>().HasKey(x => new { x.ModuleId, x.PermissionId });
        modelBuilder.Entity<UserSecurityQuestion>().HasKey(x => new { x.UserId, x.SecurityQuestionId });
    }
}
