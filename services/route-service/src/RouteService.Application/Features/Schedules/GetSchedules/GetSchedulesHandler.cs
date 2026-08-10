using RouteService.Application.Common.Models;
using RouteService.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace RouteService.Application.Features.Schedules.GetSchedules;

public sealed class GetSchedulesHandler : IRequestHandler<GetSchedulesQuery, PagedResult<ScheduleDto>>
{
    private const int MaxPageSize = 200;

    private readonly IRouteDbContext _context;

    public GetSchedulesHandler(IRouteDbContext context) => _context = context;

    public async Task<PagedResult<ScheduleDto>> Handle(GetSchedulesQuery request, CancellationToken cancellationToken)
    {
        var page = Math.Max(1, request.Page);
        var pageSize = Math.Clamp(request.PageSize, 1, MaxPageSize);

        var query = _context.Schedules
            .AsNoTracking()
            .Where(s => !s.IsDeleted)
            .AsQueryable();

        if (request.RouteId.HasValue)
            query = query.Where(s => s.RouteId == request.RouteId.Value);

        if (!string.IsNullOrWhiteSpace(request.Status) && Enum.TryParse<ScheduleStatus>(request.Status, true, out var status))
            query = query.Where(s => s.Status == status);

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderBy(s => s.DepartureTime)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(s => new ScheduleDto(s.Id, s.RouteId, s.DepartureTime, s.ArrivalTime, s.Status.ToString(), s.EffectiveFrom, s.EffectiveTo, s.Version, s.CreatedBy, s.UpdatedBy, s.CreatedAtUtc, s.UpdatedAtUtc))
            .ToListAsync(cancellationToken);

        return new PagedResult<ScheduleDto>(items, page, pageSize, totalCount);
    }
}
