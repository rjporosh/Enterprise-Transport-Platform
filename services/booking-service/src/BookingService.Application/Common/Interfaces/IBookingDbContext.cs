using BookingService.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace BookingService.Application.Common.Interfaces;

/// <summary>
/// Narrow abstraction over the EF Core DbContext so Application handlers can
/// be unit-tested against an InMemory/Sqlite provider without referencing
/// Infrastructure. Only exposes what handlers actually need.
/// </summary>
public interface IBookingDbContext
{
    DbSet<Trip> Trips { get; }
    DbSet<Booking> Bookings { get; }
    DbSet<Route> Routes { get; }
    DbSet<Bus> Buses { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
