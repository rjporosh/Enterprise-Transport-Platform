using AuthService.Application.Common.Interfaces;

namespace AuthService.UnitTests.TestSupport;

public sealed class FakeDateTimeProvider : IDateTimeProvider
{
    public DateTimeOffset UtcNow { get; set; } = new(2026, 8, 1, 9, 0, 0, TimeSpan.Zero);
}
