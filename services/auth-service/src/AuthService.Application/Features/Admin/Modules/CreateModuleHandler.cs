using AuthService.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AuthService.Application.Features.Admin.Modules;

public sealed class CreateModuleHandler : IRequestHandler<CreateModuleCommand, Guid>
{
    private readonly IAuthDbContext _context;
    private readonly IAuditLogger _auditLogger;
    private readonly ILogger<CreateModuleHandler> _logger;

    public CreateModuleHandler(IAuthDbContext context, IAuditLogger auditLogger, ILogger<CreateModuleHandler> logger)
    {
        _context = context;
        _auditLogger = auditLogger;
        _logger = logger;
    }

    public async Task<Guid> Handle(CreateModuleCommand request, CancellationToken cancellationToken)
    {
        var module = new AuthService.Domain.Entities.Module(Guid.NewGuid(), request.Name, request.Description);
        _context.Modules.Add(module);
        await _context.SaveChangesAsync(cancellationToken);
        await _auditLogger.LogAsync(AuthService.Domain.Enums.AuditAction.ModuleCreated, null, null, true, null, null, $"Module {request.Name} created.", cancellationToken);
        return module.Id;
    }
}
