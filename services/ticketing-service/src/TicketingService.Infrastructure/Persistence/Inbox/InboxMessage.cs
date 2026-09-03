namespace TicketingService.Infrastructure.Persistence.Inbox;

public sealed class InboxMessage
{
    public Guid Id { get; set; }
    public string Consumer { get; set; } = default!;
    public string RoutingKey { get; set; } = default!;
    public DateTimeOffset ReceivedAtUtc { get; set; }
    public DateTimeOffset ProcessedAtUtc { get; set; }
}
