using RouteService.Application.Features.Stops.GetStops;
using RouteService.Domain.Entities;
using RouteService.UnitTests.TestSupport;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace RouteService.UnitTests.Stops;

public class GetStopsHandlerTests : IDisposable
{
    private readonly TestRouteDbContext _context;
    private readonly FakeDateTimeProvider _clock = new();

    public GetStopsHandlerTests()
    {
        var options = new DbContextOptionsBuilder<TestRouteDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _context = new TestRouteDbContext(options);

        _context.Stops.AddRange(
            Stop.Create(Guid.NewGuid(), "DHK", "Dhaka", "Dhaka", "Kamalapur", 23.8103, 90.4125, _clock.UtcNow),
            Stop.Create(Guid.NewGuid(), "CTG", "Chittagong", "Chittagong", "CTG Station", 22.3569, 91.7832, _clock.UtcNow));
        _context.SaveChanges();
    }

    [Fact]
    public async Task Handle_ReturnsPagedStops()
    {
        var handler = new GetStopsHandler(_context);
        var result = await handler.Handle(new GetStopsQuery(null, null, 1, 50), CancellationToken.None);
        result.Items.Should().HaveCount(2);
    }

    [Fact]
    public async Task Handle_FilterByCity_ReturnsMatching()
    {
        var handler = new GetStopsHandler(_context);
        var result = await handler.Handle(new GetStopsQuery("Dhaka", null, 1, 50), CancellationToken.None);
        result.Items.Should().HaveCount(1);
        result.Items.First().Code.Should().Be("DHK");
    }

    [Fact]
    public async Task Handle_SearchTerm_ReturnsMatching()
    {
        var handler = new GetStopsHandler(_context);
        var result = await handler.Handle(new GetStopsQuery(null, "CTG", 1, 50), CancellationToken.None);
        result.Items.Should().HaveCount(1);
        result.Items.First().Code.Should().Be("CTG");
    }

    public void Dispose() => _context.Dispose();
}
