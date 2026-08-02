namespace AuthService.Application.Features.Audit.GetAuditLogs;

public sealed record PagedResult<T>(IReadOnlyCollection<T> Items, int Page, int PageSize, int TotalCount);
