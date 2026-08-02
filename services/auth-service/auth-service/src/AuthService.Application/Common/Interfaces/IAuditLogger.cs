using AuthService.Domain.Enums;

namespace AuthService.Application.Common.Interfaces;

/// <summary>
/// Writes an append-only security audit row and saves it immediately
/// (its own SaveChangesAsync, independent of the calling handler's unit of
/// work) — so a LoginFailure for an email that has no matching user still
/// gets recorded even though there is no aggregate to attach it to and
/// nothing else to save in that request.
/// </summary>
public interface IAuditLogger
{
    Task LogAsync(
        AuditAction action,
        Guid? userId,
        string? email,
        bool success,
        string? ipAddress,
        string? userAgent,
        string? details = null,
        CancellationToken cancellationToken = default);
}
