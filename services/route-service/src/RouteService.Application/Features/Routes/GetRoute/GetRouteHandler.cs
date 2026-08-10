using RouteService.Application.Common.Models;
using RouteService.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace RouteService.Application.Features.Routes.GetRoute;

public sealed class GetRouteHandler : IRequestHandler<GetRouteQuery, RouteDto>
{
    private readonly IRouteDbContext _context;

    public GetRouteHandler(IRouteDbContext context) => _context = context;

    public async Task<RouteDto> Handle(GetRouteQuery request, CancellationToken cancellationToken)
    {
        var route = await _context.Routes
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == request.RouteId && !r.IsDeleted, cancellationToken);

        if (route is null) throw new RouteNotFoundException(request.RouteId);

        return new RouteDto(route.Id, route.Code, route.Name, route.OriginStopId, route.DestinationStopId, route.TransportMode.ToString(), route.DistanceKm, route.EstimatedDuration, route.Status.ToString(), route.Version, route.CreatedBy, route.UpdatedBy, route.CreatedAtUtc, route.UpdatedAtUtc);
    }
}
