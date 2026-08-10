namespace RouteService.UnitTests.TestSupport;

public sealed class FakeDateTimeProvider : RouteService.Application.Common.Interfaces.IDateTimeProvider
{
    public DateTimeOffset UtcNow { get; set; } = new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero);
}
