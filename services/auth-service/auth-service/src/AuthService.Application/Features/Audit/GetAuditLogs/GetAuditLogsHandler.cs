using AuthService.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AuthService.Application.Features.Audit.GetAuditLogs;

public sealed class GetAuditLogsHandler : IRequestHandler<GetAuditLogsQuery, PagedResult<AuditLogDto>>
{
    private const int MaxPageSize = 200;

    private readonly IAuthDbContext _context;

    public GetAuditLogsHandler(IAuthDbContext context) => _context = context;

    public async Task<PagedResult<AuditLogDto>> Handle(GetAuditLogsQuery request, CancellationToken cancellationToken)
    {
        var page = Math.Max(1, request.Page);
        var pageSize = Math.Clamp(request.PageSize, 1, MaxPageSize);

        var query = _context.AuditLogs.AsQueryable();

        if (request.UserId.HasValue)
            query = query.Where(a => a.UserId == request.UserId.Value);

        if (!string.IsNullOrWhiteSpace(request.IpAddress))
            query = query.Where(a => a.IpAddress == request.IpAddress);

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(a => a.OccurredAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(a => new AuditLogDto(a.Id, a.Action.ToString(), a.UserId, a.Email, a.Success, a.IpAddress, a.UserAgent, a.Details, a.OccurredAtUtc))
            .ToListAsync(cancellationToken);

        return new PagedResult<AuditLogDto>(items, page, pageSize, totalCount);
    }
}
