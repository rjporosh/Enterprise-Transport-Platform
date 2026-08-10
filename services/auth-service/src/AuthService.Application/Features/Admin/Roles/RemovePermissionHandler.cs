using AuthService.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AuthService.Application.Features.Admin.Roles;

public sealed class RemovePermissionHandler : IRequestHandler<RemovePermissionCommand>
{
    private readonly IAuthDbContext _context;
    private readonly IAuditLogger _auditLogger;
    private readonly ILogger<RemovePermissionHandler> _logger;

    public RemovePermissionHandler(IAuthDbContext context, IAuditLogger auditLogger, ILogger<RemovePermissionHandler> logger)
    {
        _context = context;
        _auditLogger = auditLogger;
        _logger = logger;
    }

    public async Task Handle(RemovePermissionCommand request, CancellationToken cancellationToken)
    {
        var existing = await _context.RolePermissions.FirstOrDefaultAsync(rp => rp.RoleId == request.RoleId && rp.PermissionId == request.PermissionId, cancellationToken);
        if (existing is not null)
        {
            _context.RolePermissions.Remove(existing);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}
