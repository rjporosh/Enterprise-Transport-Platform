using Microsoft.Extensions.Logging;
using RouteService.Application.Common.Interfaces;

namespace RouteService.Infrastructure.Observability;

public sealed class AuditLogger : IAuditLogger
{
    private readonly ICurrentUser _currentUser;
    private readonly ILogger<AuditLogger> _logger;

    public AuditLogger(ICurrentUser currentUser, ILogger<AuditLogger> logger)
    {
        _currentUser = currentUser;
        _logger = logger;
    }

    public Task LogAsync(string action, string entityName, Guid entityId, string? userId, object? changes, CancellationToken cancellationToken = default)
    {
        var log = new
        {
            Action = action,
            EntityName = entityName,
            EntityId = entityId,
            UserId = userId ?? _currentUser.UserId,
            Changes = changes is null ? null : System.Text.Json.JsonSerializer.Serialize(changes),
            OccurredOnUtc = DateTimeOffset.UtcNow
        };

        _logger.LogInformation("Audit: {Audit}", System.Text.Json.JsonSerializer.Serialize(log));
        return Task.CompletedTask;
    }
}
