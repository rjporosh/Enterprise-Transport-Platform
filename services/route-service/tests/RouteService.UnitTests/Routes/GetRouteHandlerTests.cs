using RouteService.Application.Features.Routes.GetRoute;
using RouteService.Domain.Entities;
using RouteService.Domain.Exceptions;
using RouteService.UnitTests.TestSupport;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace RouteService.UnitTests.Routes;

public class GetRouteHandlerTests : IDisposable
{
    private readonly TestRouteDbContext _context;

    public GetRouteHandlerTests()
    {
        var options = new DbContextOptionsBuilder<TestRouteDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _context = new TestRouteDbContext(options);

        var route = Route.Create(Guid.NewGuid(), "TEST-1", "Test Route", Guid.NewGuid(), Guid.NewGuid(), RouteService.Domain.Enums.TransportMode.Bus, 100, TimeSpan.FromHours(2), DateTimeOffset.UtcNow);
        _context.Routes.Add(route);
        _context.SaveChanges();
    }

    [Fact]
    public async Task Handle_ExistingRoute_ReturnsRoute()
    {
        var handler = new GetRouteHandler(_context);
        var route = _context.Routes.First();
        var result = await handler.Handle(new GetRouteQuery(route.Id), CancellationToken.None);
        result.Code.Should().Be("TEST-1");
    }

    [Fact]
    public async Task Handle_MissingRoute_ThrowsNotFoundException()
    {
        var handler = new GetRouteHandler(_context);
        var act = async () => await handler.Handle(new GetRouteQuery(Guid.NewGuid()), CancellationToken.None);
        await act.Should().ThrowAsync<RouteNotFoundException>();
    }

    public void Dispose() => _context.Dispose();
}
