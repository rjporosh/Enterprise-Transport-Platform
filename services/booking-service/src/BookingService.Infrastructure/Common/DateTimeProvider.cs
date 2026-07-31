using BookingService.Application.Common.Interfaces;

namespace BookingService.Infrastructure.Common;

public sealed class DateTimeProvider : IDateTimeProvider
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}
