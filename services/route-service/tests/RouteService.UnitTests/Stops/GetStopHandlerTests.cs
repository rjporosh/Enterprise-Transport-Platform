using RouteService.Application.Features.Stops.GetStop;
using RouteService.Domain.Entities;
using RouteService.Domain.Exceptions;
using RouteService.UnitTests.TestSupport;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace RouteService.UnitTests.Stops;

public class GetStopHandlerTests : IDisposable
{
    private readonly TestRouteDbContext _context;
    private readonly FakeDateTimeProvider _clock = new();

    public GetStopHandlerTests()
    {
        var options = new DbContextOptionsBuilder<TestRouteDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _context = new TestRouteDbContext(options);

        _context.Stops.Add(Stop.Create(Guid.NewGuid(), "DHK", "Dhaka", "Dhaka", "Kamalapur", 23.8103, 90.4125, _clock.UtcNow));
        _context.SaveChanges();
    }

    [Fact]
    public async Task Handle_ExistingStop_ReturnsStop()
    {
        var handler = new GetStopHandler(_context);
        var stop = _context.Stops.First();
        var result = await handler.Handle(new GetStopQuery(stop.Id), CancellationToken.None);
        result.Code.Should().Be("DHK");
    }

    [Fact]
    public async Task Handle_MissingStop_ThrowsNotFoundException()
    {
        var handler = new GetStopHandler(_context);
        var act = async () => await handler.Handle(new GetStopQuery(Guid.NewGuid()), CancellationToken.None);
        await act.Should().ThrowAsync<StopNotFoundException>();
    }

    public void Dispose() => _context.Dispose();
}
