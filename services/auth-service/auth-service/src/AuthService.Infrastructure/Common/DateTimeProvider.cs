using AuthService.Application.Common.Interfaces;

namespace AuthService.Infrastructure.Common;

public sealed class DateTimeProvider : IDateTimeProvider
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}
