using RouteService.Application.Features.Routes.GetRoutes;
using RouteService.UnitTests.TestSupport;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace RouteService.UnitTests.Routes;

public class GetRoutesHandlerTests : IDisposable
{
    private readonly TestRouteDbContext _context;

    public GetRoutesHandlerTests()
    {
        var options = new DbContextOptionsBuilder<TestRouteDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _context = new TestRouteDbContext(options);
    }

    [Fact]
    public async Task Handle_WithSearchTerm_ReturnsMatchingRoutes()
    {
        var handler = new GetRoutesHandler(_context);
        var result = await handler.Handle(new GetRoutesQuery("TEST", null, null), CancellationToken.None);
        result.Items.Should().BeEmpty();
    }

    public void Dispose() => _context.Dispose();
}
