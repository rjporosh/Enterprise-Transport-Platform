using RouteService.Application.Common.Models;
using RouteService.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace RouteService.Application.Features.Routes.GetRoutes;

public sealed class GetRoutesHandler : IRequestHandler<GetRoutesQuery, PagedResult<RouteDto>>
{
    private const int MaxPageSize = 200;

    private readonly IRouteDbContext _context;

    public GetRoutesHandler(IRouteDbContext context) => _context = context;

    public async Task<PagedResult<RouteDto>> Handle(GetRoutesQuery request, CancellationToken cancellationToken)
    {
        var page = Math.Max(1, request.Page);
        var pageSize = Math.Clamp(request.PageSize, 1, MaxPageSize);

        var query = _context.Routes
            .AsNoTracking()
            .Where(r => !r.IsDeleted)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            var term = request.SearchTerm.Trim().ToLower();
            query = query.Where(r => r.Code.ToLower().Contains(term) || r.Name.ToLower().Contains(term));
        }

        if (!string.IsNullOrWhiteSpace(request.TransportMode) && Enum.TryParse<TransportMode>(request.TransportMode, true, out var mode))
            query = query.Where(r => r.TransportMode == mode);

        if (!string.IsNullOrWhiteSpace(request.Status) && Enum.TryParse<RouteStatus>(request.Status, true, out var status))
            query = query.Where(r => r.Status == status);

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderBy(r => r.Code)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(r => new RouteDto(r.Id, r.Code, r.Name, r.OriginStopId, r.DestinationStopId, r.TransportMode.ToString(), r.DistanceKm, r.EstimatedDuration, r.Status.ToString(), r.Version, r.CreatedBy, r.UpdatedBy, r.CreatedAtUtc, r.UpdatedAtUtc))
            .ToListAsync(cancellationToken);

        return new PagedResult<RouteDto>(items, page, pageSize, totalCount);
    }
}
