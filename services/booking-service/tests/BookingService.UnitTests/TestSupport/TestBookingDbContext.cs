using BookingService.Application.Common.Interfaces;
using BookingService.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace BookingService.UnitTests.TestSupport;

/// <summary>
/// Minimal EF Core InMemory-backed context implementing the same
/// IBookingDbContext port the real BookingDbContext implements. Lets
/// Application-layer handlers be tested against real LINQ/EF behavior
/// (change tracking, Include, projections) without needing Postgres —
/// full provider-specific behavior (xmin concurrency, jsonb) is instead
/// covered by the Testcontainers-based integration tests.
/// </summary>
public sealed class TestBookingDbContext : DbContext, IBookingDbContext
{
    public TestBookingDbContext(DbContextOptions<TestBookingDbContext> options) : base(options) { }

    public DbSet<Trip> Trips => Set<Trip>();
    public DbSet<Booking> Bookings => Set<Booking>();
    public DbSet<Route> Routes => Set<Route>();
    public DbSet<Bus> Buses => Set<Bus>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Trip>(builder =>
        {
            builder.HasKey(x => x.Id);
            builder.OwnsOne(x => x.BasePrice);
            builder.HasMany(x => x.Seats).WithOne().HasForeignKey(s => s.TripId);
            builder.Navigation(x => x.Seats).UsePropertyAccessMode(PropertyAccessMode.Field);
            builder.Ignore(x => x.DomainEvents);
        });

        modelBuilder.Entity<TripSeat>().HasKey(x => x.Id);

        modelBuilder.Entity<Booking>(builder =>
        {
            builder.HasKey(x => x.Id);
            builder.OwnsOne(x => x.TotalAmount);
            builder.HasMany(x => x.Seats).WithOne().HasForeignKey(s => s.BookingId);
            builder.Navigation(x => x.Seats).UsePropertyAccessMode(PropertyAccessMode.Field);
            builder.Ignore(x => x.DomainEvents);
        });

        modelBuilder.Entity<BookingSeat>().HasKey(x => x.Id);
        modelBuilder.Entity<Route>().HasKey(x => x.Id);
        modelBuilder.Entity<Bus>().HasKey(x => x.Id);
    }
}
