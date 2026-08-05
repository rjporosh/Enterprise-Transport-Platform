using BusService.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace BusService.Application.Common.Interfaces;

public interface IBusDbContext
{
    DbSet<Bus> Buses { get; }
    DbSet<Depot> Depots { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
