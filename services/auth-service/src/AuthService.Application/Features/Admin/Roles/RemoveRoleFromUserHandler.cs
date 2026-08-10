using AuthService.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AuthService.Application.Features.Admin.Roles;

public sealed class RemoveRoleFromUserHandler : IRequestHandler<RemoveRoleFromUserCommand>
{
    private readonly IAuthDbContext _context;
    private readonly IAuditLogger _auditLogger;
    private readonly ILogger<RemoveRoleFromUserHandler> _logger;

    public RemoveRoleFromUserHandler(IAuthDbContext context, IAuditLogger auditLogger, ILogger<RemoveRoleFromUserHandler> logger)
    {
        _context = context;
        _auditLogger = auditLogger;
        _logger = logger;
    }

    public async Task Handle(RemoveRoleFromUserCommand request, CancellationToken cancellationToken)
    {
        var existing = await _context.UserRoles.FirstOrDefaultAsync(ur => ur.UserId == request.UserId && ur.RoleId == request.RoleId, cancellationToken);
        if (existing is not null)
        {
            _context.UserRoles.Remove(existing);
            await _context.SaveChangesAsync(cancellationToken);
            await _auditLogger.LogAsync(AuditAction.UserRoleChanged, request.UserId, null, true, null, null, "Role removed from user.", cancellationToken);
        }
    }
}
