using BookingService.Application.Features.Bookings.GetBookingById;
using BookingService.Domain.Common;
using BookingService.Domain.Entities;
using BookingService.Domain.Exceptions;
using BookingService.UnitTests.TestSupport;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace BookingService.UnitTests.Bookings;

public class GetBookingByIdHandlerTests : IDisposable
{
    private readonly TestBookingDbContext _context;
    private readonly Guid _ownerId = Guid.NewGuid();
    private readonly Booking _booking;

    public GetBookingByIdHandlerTests()
    {
        var options = new DbContextOptionsBuilder<TestBookingDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _context = new TestBookingDbContext(options);

        _booking = Booking.Create(
            Guid.NewGuid(), _ownerId, "owner@example.com", "Owner", null,
            new Money(500m, "BDT"),
            new[] { ("A1", "Owner", 30, "Male") },
            DateTimeOffset.UtcNow);
        _context.Bookings.Add(_booking);
        _context.SaveChanges();
    }

    [Fact]
    public async Task Handle_WhenCallerOwnsBooking_ReturnsIt()
    {
        var dto = await new GetBookingByIdHandler(_context)
            .Handle(new GetBookingByIdQuery(_booking.Id, _ownerId, IsAdmin: false), CancellationToken.None);

        dto.BookingId.Should().Be(_booking.Id);
    }

    [Fact]
    public async Task Handle_WhenAnotherCustomerAsks_Throws404_NotAForbidden()
    {
        var act = () => new GetBookingByIdHandler(_context)
            .Handle(new GetBookingByIdQuery(_booking.Id, Guid.NewGuid(), IsAdmin: false), CancellationToken.None);

        await act.Should().ThrowAsync<BookingNotFoundException>();
    }

    [Fact]
    public async Task Handle_WhenAdminAsks_ReturnsAnyBooking()
    {
        var dto = await new GetBookingByIdHandler(_context)
            .Handle(new GetBookingByIdQuery(_booking.Id, Guid.NewGuid(), IsAdmin: true), CancellationToken.None);

        dto.BookingId.Should().Be(_booking.Id);
    }

    public void Dispose() => _context.Dispose();
}
