using AuthService.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AuthService.Application.Features.Admin.Permissions;

public sealed class CreatePermissionHandler : IRequestHandler<CreatePermissionCommand, Guid>
{
    private readonly IAuthDbContext _context;
    private readonly IAuditLogger _auditLogger;
    private readonly ILogger<CreatePermissionHandler> _logger;

    public CreatePermissionHandler(IAuthDbContext context, IAuditLogger auditLogger, ILogger<CreatePermissionHandler> logger)
    {
        _context = context;
        _auditLogger = auditLogger;
        _logger = logger;
    }

    public async Task<Guid> Handle(CreatePermissionCommand request, CancellationToken cancellationToken)
    {
        var permission = new AuthService.Domain.Entities.Permission(Guid.NewGuid(), request.Name, request.Description, request.Module);
        _context.Permissions.Add(permission);
        await _context.SaveChangesAsync(cancellationToken);
        await _auditLogger.LogAsync(AuthService.Domain.Enums.AuditAction.PermissionCreated, null, null, true, null, null, $"Permission {request.Name} created.", cancellationToken);
        return permission.Id;
    }
}
