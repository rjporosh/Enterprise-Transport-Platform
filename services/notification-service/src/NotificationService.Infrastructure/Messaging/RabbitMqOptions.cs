namespace NotificationService.Infrastructure.Messaging;

public sealed class RabbitMqOptions
{
    public const string SectionName = "RabbitMq";

    public string HostName { get; set; } = "localhost";
    public int Port { get; set; } = 5672;
    public string UserName { get; set; } = "guest";
    public string Password { get; set; } = "guest";
    public string Exchange { get; set; } = "notification.events";

    /// <summary>Routing-key patterns this service consumes FROM other services' exchanges (Booking, Payment, Auth) to trigger outbound notifications -- see NotificationEventConsumer. Configured here (not hardcoded) so new upstream event types can be wired without a redeploy of the binding logic itself.</summary>
    public List<UpstreamBinding> UpstreamBindings { get; set; } = new();
}

public sealed class UpstreamBinding
{
    public string Exchange { get; set; } = default!;
    public string RoutingKey { get; set; } = default!;
}
