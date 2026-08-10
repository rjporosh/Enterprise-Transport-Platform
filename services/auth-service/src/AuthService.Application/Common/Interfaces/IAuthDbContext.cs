using AuthService.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AuthService.Application.Common.Interfaces;

public interface IAuthDbContext
{
    DbSet<User> Users { get; }
    DbSet<Role> Roles { get; }
    DbSet<UserRole> UserRoles { get; }
    DbSet<RefreshToken> RefreshTokens { get; }
    DbSet<AuditLog> AuditLogs { get; }
    DbSet<Permission> Permissions { get; }
    DbSet<Module> Modules { get; }
    DbSet<Policy> Policies { get; }
    DbSet<Claim> Claims { get; }
    DbSet<OtpRecord> OtpRecords { get; }
    DbSet<SecurityQuestion> SecurityQuestions { get; }
    DbSet<SecurityAnswer> SecurityAnswers { get; }
    DbSet<PasswordHistory> PasswordHistories { get; }
    DbSet<PasswordResetToken> PasswordResetTokens { get; }
    DbSet<UserSession> UserSessions { get; }
    DbSet<UserClaim> UserClaims { get; }
    DbSet<RolePermission> RolePermissions { get; }
    DbSet<ModulePermission> ModulePermissions { get; }
    DbSet<UserSecurityQuestion> UserSecurityQuestions { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
