using PaymentService.Application.Common.Interfaces;

namespace PaymentService.UnitTests.TestSupport;

public class FakeDateTimeProvider : IDateTimeProvider
{
    private DateTimeOffset _fixedTime;

    public FakeDateTimeProvider(DateTimeOffset? fixedTime = null)
    {
        _fixedTime = fixedTime ?? new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
    }

    public DateTimeOffset UtcNow
    {
        get => _fixedTime;
        set => _fixedTime = value;
    }
}
