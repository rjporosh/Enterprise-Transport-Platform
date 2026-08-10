using AuthService.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AuthService.Application.Features.Admin.Roles;

public sealed class CreateRoleHandler : IRequestHandler<CreateRoleCommand, Guid>
{
    private readonly IAuthDbContext _context;
    private readonly IAuditLogger _auditLogger;
    private readonly ILogger<CreateRoleHandler> _logger;

    public CreateRoleHandler(IAuthDbContext context, IAuditLogger auditLogger, ILogger<CreateRoleHandler> logger)
    {
        _context = context;
        _auditLogger = auditLogger;
        _logger = logger;
    }

    public async Task<Guid> Handle(CreateRoleCommand request, CancellationToken cancellationToken)
    {
        var role = new AuthService.Domain.Entities.Role(Guid.NewGuid(), request.Name, request.Description);
        _context.Roles.Add(role);
        await _context.SaveChangesAsync(cancellationToken);
        await _auditLogger.LogAsync(AuditAction.RoleCreated, null, null, true, null, null, $"Role {request.Name} created.", cancellationToken);
        return role.Id;
    }
}
