namespace BookingService.Infrastructure.Persistence.Inbox;

/// <summary>
/// Inbound-event de-duplication ("inbox" pattern). Before a consumer acts on
/// a RabbitMQ message it records the message id here in the SAME transaction
/// as the state change it makes; a duplicate delivery (at-least-once) finds
/// the row already present and is acked without re-processing. Keyed by the
/// upstream event id so redelivery of the exact same event is idempotent.
/// </summary>
public sealed class InboxMessage
{
    public Guid Id { get; set; }
    public string Consumer { get; set; } = default!;
    public string RoutingKey { get; set; } = default!;
    public DateTimeOffset ReceivedAtUtc { get; set; }
    public DateTimeOffset ProcessedAtUtc { get; set; }
}
