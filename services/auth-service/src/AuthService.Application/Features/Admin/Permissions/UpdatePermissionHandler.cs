using AuthService.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AuthService.Application.Features.Admin.Permissions;

public sealed class UpdatePermissionHandler : IRequestHandler<UpdatePermissionCommand>
{
    private readonly IAuthDbContext _context;
    private readonly IAuditLogger _auditLogger;
    private readonly ILogger<UpdatePermissionHandler> _logger;

    public UpdatePermissionHandler(IAuthDbContext context, IAuditLogger auditLogger, ILogger<UpdatePermissionHandler> logger)
    {
        _context = context;
        _auditLogger = auditLogger;
        _logger = logger;
    }

    public async Task Handle(UpdatePermissionCommand request, CancellationToken cancellationToken)
    {
        var permission = await _context.Permissions.FirstOrDefaultAsync(p => p.Id == request.PermissionId, cancellationToken);
        if (permission is null)
            throw new AuthService.Domain.Exceptions.PermissionNotFoundException(request.PermissionId);

        permission.Update(request.Description, request.Module);
        await _context.SaveChangesAsync(cancellationToken);
        await _auditLogger.LogAsync(AuthService.Domain.Enums.AuditAction.PermissionUpdated, null, null, true, null, null, $"Permission {permission.Name} updated.", cancellationToken);
    }
}
