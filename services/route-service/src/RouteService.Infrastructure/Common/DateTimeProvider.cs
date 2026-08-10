namespace RouteService.Infrastructure.Common;

public sealed class DateTimeProvider : RouteService.Application.Common.Interfaces.IDateTimeProvider
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}
