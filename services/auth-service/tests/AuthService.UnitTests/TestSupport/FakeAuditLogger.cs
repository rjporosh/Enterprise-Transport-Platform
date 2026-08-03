using AuthService.Application.Common.Interfaces;
using AuthService.Domain.Enums;

namespace AuthService.UnitTests.TestSupport;

public sealed record RecordedAuditEntry(AuditAction Action, Guid? UserId, string? Email, bool Success, string? Details);

public sealed class FakeAuditLogger : IAuditLogger
{
    public List<RecordedAuditEntry> Entries { get; } = new();

    public Task LogAsync(AuditAction action, Guid? userId, string? email, bool success, string? ipAddress, string? userAgent, string? details = null, CancellationToken cancellationToken = default)
    {
        Entries.Add(new RecordedAuditEntry(action, userId, email, success, details));
        return Task.CompletedTask;
    }
}
