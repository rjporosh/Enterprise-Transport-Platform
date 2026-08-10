using Microsoft.EntityFrameworkCore;

namespace RouteService.Application.Common.Interfaces;

public interface IRouteDbContext
{
    DbSet<RouteService.Domain.Entities.Route> Routes { get; }
    DbSet<RouteService.Domain.Entities.Stop> Stops { get; }
    DbSet<RouteService.Domain.Entities.RouteStop> RouteStops { get; }
    DbSet<RouteService.Domain.Entities.Schedule> Schedules { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
