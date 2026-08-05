using BusService.Application.Common.Interfaces;

namespace BusService.Infrastructure.Common;

public sealed class DateTimeProvider : IDateTimeProvider
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}
