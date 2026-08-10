namespace RouteService.Infrastructure.Persistence.Entities;

public sealed class AuditLog
{
    public Guid Id { get; set; }
    public string Action { get; set; } = default!;
    public string EntityName { get; set; } = default!;
    public Guid EntityId { get; set; }
    public string? UserId { get; set; }
    public string? Changes { get; set; }
    public DateTimeOffset OccurredOnUtc { get; set; }
    public string? IpAddress { get; set; }
    public string? CorrelationId { get; set; }
}
