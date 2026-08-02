namespace AuthService.Application.Features.Audit.GetAuditLogs;

public sealed record AuditLogDto(
    Guid Id,
    string Action,
    Guid? UserId,
    string? Email,
    bool Success,
    string? IpAddress,
    string? UserAgent,
    string? Details,
    DateTimeOffset OccurredAtUtc);
