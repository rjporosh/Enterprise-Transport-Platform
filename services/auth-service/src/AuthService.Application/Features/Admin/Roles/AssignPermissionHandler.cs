using AuthService.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AuthService.Application.Features.Admin.Roles;

public sealed class AssignPermissionHandler : IRequestHandler<AssignPermissionCommand>
{
    private readonly IAuthDbContext _context;
    private readonly IAuditLogger _auditLogger;
    private readonly ILogger<AssignPermissionHandler> _logger;

    public AssignPermissionHandler(IAuthDbContext context, IAuditLogger auditLogger, ILogger<AssignPermissionHandler> logger)
    {
        _context = context;
        _auditLogger = auditLogger;
        _logger = logger;
    }

    public async Task Handle(AssignPermissionCommand request, CancellationToken cancellationToken)
    {
        var role = await _context.Roles.FirstOrDefaultAsync(r => r.Id == request.RoleId, cancellationToken);
        if (role is null)
            throw new AuthService.Domain.Exceptions.RoleNotFoundException(request.RoleId);

        var permission = await _context.Permissions.FirstOrDefaultAsync(p => p.Id == request.PermissionId, cancellationToken);
        if (permission is null)
            throw new AuthService.Domain.Exceptions.PermissionNotFoundException(request.PermissionId);

        var existing = await _context.RolePermissions.FirstOrDefaultAsync(rp => rp.RoleId == request.RoleId && rp.PermissionId == request.PermissionId, cancellationToken);
        if (existing is null)
        {
            _context.RolePermissions.Add(new AuthService.Domain.Entities.RolePermission(request.RoleId, request.PermissionId, DateTimeOffset.UtcNow));
            await _context.SaveChangesAsync(cancellationToken);
            await _auditLogger.LogAsync(AuditAction.PermissionCreated, null, null, true, null, null, $"Permission {permission.Name} assigned to role {role.Name}.", cancellationToken);
        }
    }
}
