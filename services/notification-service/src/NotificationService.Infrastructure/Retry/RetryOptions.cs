namespace NotificationService.Infrastructure.Retry;

public sealed class RetryOptions
{
    public const string SectionName = "Retry";

    public int MaxAttempts { get; set; } = 3;
    public int BaseDelayMilliseconds { get; set; } = 500;
}
