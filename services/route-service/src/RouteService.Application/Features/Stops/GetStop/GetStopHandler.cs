using RouteService.Application.Common.Models;
using RouteService.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace RouteService.Application.Features.Stops.GetStop;

public sealed class GetStopHandler : IRequestHandler<GetStopQuery, StopDto>
{
    private readonly IRouteDbContext _context;

    public GetStopHandler(IRouteDbContext context) => _context = context;

    public async Task<StopDto> Handle(GetStopQuery request, CancellationToken cancellationToken)
    {
        var stop = await _context.Stops
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == request.StopId && !s.IsDeleted, cancellationToken);

        if (stop is null) throw new StopNotFoundException(request.StopId);

        return new StopDto(stop.Id, stop.Code, stop.Name, stop.City, stop.Address, stop.Latitude, stop.Longitude, stop.CreatedBy, stop.UpdatedBy, stop.CreatedAtUtc, stop.UpdatedAtUtc);
    }
}
