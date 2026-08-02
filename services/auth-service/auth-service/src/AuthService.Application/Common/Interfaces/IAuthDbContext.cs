using AuthService.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AuthService.Application.Common.Interfaces;

/// <summary>
/// Everything a CQRS handler needs from persistence, without depending on
/// EF Core's DbContext type or any provider package directly.
/// </summary>
public interface IAuthDbContext
{
    DbSet<User> Users { get; }
    DbSet<Role> Roles { get; }
    DbSet<UserRole> UserRoles { get; }
    DbSet<RefreshToken> RefreshTokens { get; }
    DbSet<AuditLog> AuditLogs { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
