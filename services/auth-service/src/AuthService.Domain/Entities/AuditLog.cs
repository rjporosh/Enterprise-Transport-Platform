using AuthService.Domain.Enums;

namespace AuthService.Domain.Entities;

/// <summary>
/// Append-only security audit trail. Deliberately NOT an AggregateRoot —
/// it never raises domain events (an audit log entry describing an event
/// is not itself a business event) and rows are never updated, only
/// inserted. UserId is nullable because a LoginFailure for an unknown
/// email has no user to attach to, but the attempted email and IP are
/// still worth recording.
/// </summary>
public sealed class AuditLog : Common.Entity
{
    public AuditAction Action { get; private set; }
    public Guid? UserId { get; private set; }
    public string? Email { get; private set; }
    public bool Success { get; private set; }
    public string? IpAddress { get; private set; }
    public string? UserAgent { get; private set; }
    public string? Details { get; private set; }
    public DateTimeOffset OccurredAtUtc { get; private set; }

    private AuditLog() { } // EF Core

    private AuditLog(Guid id, AuditAction action, Guid? userId, string? email, bool success, string? ipAddress, string? userAgent, string? details, DateTimeOffset now)
        : base(id)
    {
        Action = action;
        UserId = userId;
        Email = email;
        Success = success;
        IpAddress = ipAddress;
        UserAgent = userAgent;
        Details = details;
        OccurredAtUtc = now;
    }

    public static AuditLog Create(AuditAction action, Guid? userId, string? email, bool success, string? ipAddress, string? userAgent, string? details, DateTimeOffset now) =>
        new(Guid.NewGuid(), action, userId, email, success, ipAddress, userAgent, details, now);
}
