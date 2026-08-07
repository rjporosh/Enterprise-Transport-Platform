using NotificationService.Application.Common.Interfaces;

namespace NotificationService.UnitTests.TestSupport;

public sealed class FakeDateTimeProvider : IDateTimeProvider
{
    public DateTimeOffset UtcNow { get; set; } = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
}
