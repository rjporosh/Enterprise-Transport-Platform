using RouteService.Application.Features.Stops.UpdateStop;
using RouteService.Domain.Entities;
using RouteService.UnitTests.TestSupport;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace RouteService.UnitTests.Stops;

public class UpdateStopHandlerTests : IDisposable
{
    private readonly TestRouteDbContext _context;
    private readonly FakeDateTimeProvider _clock = new();
    private readonly FakeCurrentUser _currentUser = new();

    public UpdateStopHandlerTests()
    {
        var options = new DbContextOptionsBuilder<TestRouteDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _context = new TestRouteDbContext(options);
    }

    [Fact]
    public async Task Handle_ValidUpdate_ReturnsUpdatedStop()
    {
        var stop = Stop.Create(Guid.NewGuid(), "DHK", "Dhaka", "Dhaka", "Kamalapur", 23.8103, 90.4125, _clock.UtcNow);
        _context.Stops.Add(stop);
        _context.SaveChanges();

        var handler = new UpdateStopHandler(_context, _clock, new RouteService.Infrastructure.Observability.AuditLogger(_currentUser, new FakeLogger<RouteService.Infrastructure.Observability.AuditLogger>()), _currentUser, new FakeLogger<UpdateStopHandler>());
        var result = await handler.Handle(new UpdateStopCommand(stop.Id, "Dhaka City", "Dhaka", "Central", 23.8103, 90.4125, null), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Name.Should().Be("Dhaka City");
    }

    [Fact]
    public async Task Handle_MissingStop_ReturnsFailure()
    {
        var handler = new UpdateStopHandler(_context, _clock, new RouteService.Infrastructure.Observability.AuditLogger(_currentUser, new FakeLogger<RouteService.Infrastructure.Observability.AuditLogger>()), _currentUser, new FakeLogger<UpdateStopHandler>());
        var result = await handler.Handle(new UpdateStopCommand(Guid.NewGuid(), "New Name", "City", "Addr", 0, 0, null), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Errors.Any(e => e.Code == "StopNotFound").Should().BeTrue();
    }

    public void Dispose() => _context.Dispose();
}
