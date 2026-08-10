using BusService.Domain.Entities;

namespace BusService.Application.Common.Interfaces;

public interface IAuditLogger
{
    Task LogAsync(AuditLog log, CancellationToken cancellationToken = default);
}
