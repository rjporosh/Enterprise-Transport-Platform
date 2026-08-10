using AuthService.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AuthService.Application.Features.Admin.Roles;

public sealed class AssignRoleToUserHandler : IRequestHandler<AssignRoleToUserCommand>
{
    private readonly IAuthDbContext _context;
    private readonly IAuditLogger _auditLogger;
    private readonly ILogger<AssignRoleToUserHandler> _logger;

    public AssignRoleToUserHandler(IAuthDbContext context, IAuditLogger auditLogger, ILogger<AssignRoleToUserHandler> logger)
    {
        _context = context;
        _auditLogger = auditLogger;
        _logger = logger;
    }

    public async Task Handle(AssignRoleToUserCommand request, CancellationToken cancellationToken)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken);
        if (user is null)
            throw new AuthService.Domain.Exceptions.UserNotFoundException(request.UserId);

        var role = await _context.Roles.FirstOrDefaultAsync(r => r.Id == request.RoleId, cancellationToken);
        if (role is null)
            throw new AuthService.Domain.Exceptions.RoleNotFoundException(request.RoleId);

        var existing = await _context.UserRoles.FirstOrDefaultAsync(ur => ur.UserId == request.UserId && ur.RoleId == request.RoleId, cancellationToken);
        if (existing is null)
        {
            user.AssignRole(request.RoleId, DateTimeOffset.UtcNow);
            await _context.SaveChangesAsync(cancellationToken);
            await _auditLogger.LogAsync(AuditAction.UserRoleChanged, user.Id, user.Email, true, null, null, $"Role {role.Name} assigned to user.", cancellationToken);
        }
    }
}
