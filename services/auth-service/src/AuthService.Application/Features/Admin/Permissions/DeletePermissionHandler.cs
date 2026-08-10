using AuthService.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AuthService.Application.Features.Admin.Permissions;

public sealed class DeletePermissionHandler : IRequestHandler<DeletePermissionCommand>
{
    private readonly IAuthDbContext _context;
    private readonly IAuditLogger _auditLogger;
    private readonly ILogger<DeletePermissionHandler> _logger;

    public DeletePermissionHandler(IAuthDbContext context, IAuditLogger auditLogger, ILogger<DeletePermissionHandler> logger)
    {
        _context = context;
        _auditLogger = auditLogger;
        _logger = logger;
    }

    public async Task Handle(DeletePermissionCommand request, CancellationToken cancellationToken)
    {
        var permission = await _context.Permissions.FirstOrDefaultAsync(p => p.Id == request.PermissionId, cancellationToken);
        if (permission is not null)
        {
            _context.Permissions.Remove(permission);
            await _context.SaveChangesAsync(cancellationToken);
            await _auditLogger.LogAsync(AuthService.Domain.Enums.AuditAction.PermissionCreated, null, null, true, null, null, $"Permission {permission.Name} deleted.", cancellationToken);
        }
    }
}
