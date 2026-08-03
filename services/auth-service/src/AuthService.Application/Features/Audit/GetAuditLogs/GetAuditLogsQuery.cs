using MediatR;

namespace AuthService.Application.Features.Audit.GetAuditLogs;

/// <summary>
/// Admin-only (see AuthEndpoints — mapped behind RequireRole("Admin")).
/// Every filter is optional so this backs both "show me this user's
/// history" and "show me everything from this IP" investigation flows.
/// </summary>
public sealed record GetAuditLogsQuery(
    Guid? UserId,
    string? IpAddress,
    int Page = 1,
    int PageSize = 50) : IRequest<PagedResult<AuditLogDto>>;
