using RouteService.Application.Features.Stops.DeleteStop;
using RouteService.Domain.Entities;
using RouteService.UnitTests.TestSupport;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace RouteService.UnitTests.Stops;

public class DeleteStopHandlerTests : IDisposable
{
    private readonly TestRouteDbContext _context;
    private readonly FakeDateTimeProvider _clock = new();
    private readonly FakeCurrentUser _currentUser = new();

    public DeleteStopHandlerTests()
    {
        var options = new DbContextOptionsBuilder<TestRouteDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _context = new TestRouteDbContext(options);
    }

    [Fact]
    public async Task Handle_SoftDeletesStop()
    {
        var stop = Stop.Create(Guid.NewGuid(), "DHK", "Dhaka", "Dhaka", "Kamalapur", 23.8103, 90.4125, _clock.UtcNow);
        _context.Stops.Add(stop);
        _context.SaveChanges();

        var handler = new DeleteStopHandler(_context, _clock, new RouteService.Infrastructure.Observability.AuditLogger(_currentUser, new FakeLogger<RouteService.Infrastructure.Observability.AuditLogger>()), _currentUser, new FakeLogger<DeleteStopHandler>());
        var result = await handler.Handle(new DeleteStopCommand(stop.Id), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        stop.IsDeleted.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_StopInUse_ReturnsFailure()
    {
        var route = Route.Create(Guid.NewGuid(), "R-1", "Route", Guid.NewGuid(), Guid.NewGuid(), RouteService.Domain.Enums.TransportMode.Bus, 100, TimeSpan.FromHours(2), _clock.UtcNow);
        var stop = Stop.Create(Guid.NewGuid(), "DHK", "Dhaka", "Dhaka", "Kamalapur", 23.8103, 90.4125, _clock.UtcNow);
        _context.Routes.Add(route);
        _context.Stops.Add(stop);
        _context.SaveChanges();

        _context.RouteStops.Add(new RouteService.Domain.Entities.RouteStop(Guid.NewGuid(), route.Id, stop.Id, 1, null, null));
        _context.SaveChanges();

        var handler = new DeleteStopHandler(_context, _clock, new RouteService.Infrastructure.Observability.AuditLogger(_currentUser, new FakeLogger<RouteService.Infrastructure.Observability.AuditLogger>()), _currentUser, new FakeLogger<DeleteStopHandler>());
        var result = await handler.Handle(new DeleteStopCommand(stop.Id), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Errors.Any(e => e.Code == "StopInUse").Should().BeTrue();
    }

    public void Dispose() => _context.Dispose();
}
