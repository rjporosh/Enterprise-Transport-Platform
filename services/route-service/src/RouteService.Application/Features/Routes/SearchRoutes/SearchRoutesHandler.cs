using RouteService.Application.Common.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace RouteService.Application.Features.Routes.SearchRoutes;

public sealed class SearchRoutesHandler : IRequestHandler<SearchRoutesQuery, PagedResult<RouteDto>>
{
    private const int MaxPageSize = 200;

    private readonly IRouteDbContext _context;

    public SearchRoutesHandler(IRouteDbContext context) => _context = context;

    public async Task<PagedResult<RouteDto>> Handle(SearchRoutesQuery request, CancellationToken cancellationToken)
    {
        var page = Math.Max(1, request.Page);
        var pageSize = Math.Clamp(request.PageSize, 1, MaxPageSize);

        var term = request.Term.Trim().ToLower();

        var query = _context.Routes
            .AsNoTracking()
            .Where(r => !r.IsDeleted && (r.Code.ToLower().Contains(term) || r.Name.ToLower().Contains(term)));

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
