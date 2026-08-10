namespace RouteService.Application.Common.Interfaces;

public interface IAuditLogger
{
    Task LogAsync(string action, string entityName, Guid entityId, string? userId, object? changes, CancellationToken cancellationToken = default);
}
