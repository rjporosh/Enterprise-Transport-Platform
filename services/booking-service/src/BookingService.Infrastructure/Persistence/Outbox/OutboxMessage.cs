namespace BookingService.Infrastructure.Persistence.Outbox;

/// <summary>
/// Transactional outbox row: written in the same DB transaction as the
/// aggregate change that raised the event, then relayed to RabbitMQ by
/// OutboxProcessor. Guarantees at-least-once delivery — the event can never
/// be "lost" between commit and publish because it's already durably stored.
/// </summary>
public sealed class OutboxMessage
{
    public Guid Id { get; set; }
    public string EventType { get; set; } = default!;
    public string Payload { get; set; } = default!;
    public DateTimeOffset OccurredOnUtc { get; set; }
    public DateTimeOffset? ProcessedOnUtc { get; set; }
    public string? Error { get; set; }
    public int RetryCount { get; set; }
}
