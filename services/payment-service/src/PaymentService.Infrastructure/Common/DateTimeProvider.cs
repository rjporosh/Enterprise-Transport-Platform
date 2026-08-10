namespace PaymentService.Infrastructure.Common;

public class DateTimeProvider : PaymentService.Application.Common.Interfaces.IDateTimeProvider
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}
