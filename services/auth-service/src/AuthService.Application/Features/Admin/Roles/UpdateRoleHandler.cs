using AuthService.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AuthService.Application.Features.Admin.Roles;

public sealed class UpdateRoleHandler : IRequestHandler<UpdateRoleCommand>
{
    private readonly IAuthDbContext _context;
    private readonly IAuditLogger _auditLogger;
    private readonly ILogger<UpdateRoleHandler> _logger;

    public UpdateRoleHandler(IAuthDbContext context, IAuditLogger auditLogger, ILogger<UpdateRoleHandler> logger)
    {
        _context = context;
        _auditLogger = auditLogger;
        _logger = logger;
    }

    public async Task Handle(UpdateRoleCommand request, CancellationToken cancellationToken)
    {
        var role = await _context.Roles.FirstOrDefaultAsync(r => r.Id == request.RoleId, cancellationToken);
        if (role is null)
            throw new AuthService.Domain.Exceptions.RoleNotFoundException(request.RoleId);

        var oldName = role.Name;
        role.Update(request.Description);
        await _context.SaveChangesAsync(cancellationToken);
        await _auditLogger.LogAsync(AuditAction.RoleUpdated, null, null, true, null, null, $"Role {oldName} updated.", cancellationToken);
    }
}
