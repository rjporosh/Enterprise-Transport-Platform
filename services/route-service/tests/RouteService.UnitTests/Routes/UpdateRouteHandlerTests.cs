using RouteService.Application.Features.Routes.UpdateRoute;
using RouteService.Domain.Entities;
using RouteService.UnitTests.TestSupport;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace RouteService.UnitTests.Routes;

public class UpdateRouteHandlerTests : IDisposable
{
    private readonly TestRouteDbContext _context;
    private readonly FakeDateTimeProvider _clock = new();
    private readonly FakeCurrentUser _currentUser = new();

    public UpdateRouteHandlerTests()
    {
        var options = new DbContextOptionsBuilder<TestRouteDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _context = new TestRouteDbContext(options);
    }

    [Fact]
    public async Task Handle_ValidUpdate_ReturnsUpdatedRoute()
    {
        var route = Route.Create(Guid.NewGuid(), "UPD-1", "Old Name", Guid.NewGuid(), Guid.NewGuid(), RouteService.Domain.Enums.TransportMode.Bus, 100, TimeSpan.FromHours(2), _clock.UtcNow);
        _context.Routes.Add(route);
        _context.SaveChanges();

        var handler = new UpdateRouteHandler(_context, new FakeEventPublisher(), _clock, new RouteService.Infrastructure.Observability.AuditLogger(_currentUser, new FakeLogger<RouteService.Infrastructure.Observability.AuditLogger>()), _currentUser, new FakeLogger<UpdateRouteHandler>());
        var result = await handler.Handle(new UpdateRouteCommand(route.Id, "New Name", "Bus", 120, TimeSpan.FromHours(2), route.Version, "updater"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Name.Should().Be("New Name");
    }

    public void Dispose() => _context.Dispose();
}
