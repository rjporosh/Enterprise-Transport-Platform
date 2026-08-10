using System.Reflection;
using AuthService.Application.Common.Interfaces;
using AuthService.Domain.Entities;
using AuthService.Infrastructure.Persistence.Outbox;
using Microsoft.EntityFrameworkCore;

namespace AuthService.Infrastructure.Persistence;

public sealed class AuthDbContext : DbContext, IAuthDbContext
{
    public AuthDbContext(DbContextOptions<AuthDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<UserRole> UserRoles => Set<UserRole>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();
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
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
        modelBuilder.HasDefaultSchema("auth");
        base.OnModelCreating(modelBuilder);
    }
}
