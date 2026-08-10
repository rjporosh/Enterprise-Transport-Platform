using RouteService.Application.Features.Routes.DeleteRoute;
using RouteService.Domain.Entities;
using RouteService.UnitTests.TestSupport;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace RouteService.UnitTests.Routes;

public class DeleteRouteHandlerTests : IDisposable
{
    private readonly TestRouteDbContext _context;
    private readonly FakeDateTimeProvider _clock = new();
    private readonly FakeCurrentUser _currentUser = new();

    public DeleteRouteHandlerTests()
    {
        var options = new DbContextOptionsBuilder<TestRouteDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _context = new TestRouteDbContext(options);
    }

    [Fact]
    public async Task Handle_SoftDeletesRoute()
    {
        var route = Route.Create(Guid.NewGuid(), "DEL-1", "Delete Me", Guid.NewGuid(), Guid.NewGuid(), RouteService.Domain.Enums.TransportMode.Bus, 100, TimeSpan.FromHours(2), _clock.UtcNow);
        _context.Routes.Add(route);
        _context.SaveChanges();

        var handler = new DeleteRouteHandler(_context, new FakeEventPublisher(), _clock, new RouteService.Infrastructure.Observability.AuditLogger(_currentUser, new FakeLogger<RouteService.Infrastructure.Observability.AuditLogger>()), _currentUser, new FakeLogger<DeleteRouteHandler>());
        var result = await handler.Handle(new DeleteRouteCommand(route.Id, route.Version), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        route.IsDeleted.Should().BeTrue();
    }

    public void Dispose() => _context.Dispose();
}
