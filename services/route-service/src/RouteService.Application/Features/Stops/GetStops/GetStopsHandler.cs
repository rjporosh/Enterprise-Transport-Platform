using RouteService.Application.Common.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace RouteService.Application.Features.Stops.GetStops;

public sealed class GetStopsHandler : IRequestHandler<GetStopsQuery, PagedResult<StopDto>>
{
    private const int MaxPageSize = 200;

    private readonly IRouteDbContext _context;

    public GetStopsHandler(IRouteDbContext context) => _context = context;

    public async Task<PagedResult<StopDto>> Handle(GetStopsQuery request, CancellationToken cancellationToken)
    {
        var page = Math.Max(1, request.Page);
        var pageSize = Math.Clamp(request.PageSize, 1, MaxPageSize);

        var query = _context.Stops
            .AsNoTracking()
            .Where(s => !s.IsDeleted)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.City))
        {
            var city = request.City.Trim().ToLower();
            query = query.Where(s => s.City.ToLower().Contains(city));
        }

        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            var term = request.SearchTerm.Trim().ToLower();
            query = query.Where(s => s.Code.ToLower().Contains(term) || s.Name.ToLower().Contains(term));
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderBy(s => s.Code)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(s => new StopDto(s.Id, s.Code, s.Name, s.City, s.Address, s.Latitude, s.Longitude, s.CreatedBy, s.UpdatedBy, s.CreatedAtUtc, s.UpdatedAtUtc))
            .ToListAsync(cancellationToken);

        return new PagedResult<StopDto>(items, page, pageSize, totalCount);
    }
}
