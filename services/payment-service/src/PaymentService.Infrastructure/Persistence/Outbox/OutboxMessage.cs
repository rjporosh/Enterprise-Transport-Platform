namespace PaymentService.Infrastructure.Persistence.Outbox;

public class OutboxMessage
{
    public Guid Id { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string Payload { get; set; } = string.Empty;
    public DateTimeOffset OccurredOnUtc { get; set; }
    public DateTimeOffset? ProcessedOnUtc { get; set; }
    public string? Error { get; set; }
    public int RetryCount { get; set; }
    public string? CorrelationId { get; set; }
}
