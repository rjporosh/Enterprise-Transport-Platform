using AuthService.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AuthService.Application.Features.Admin.Modules;

public sealed class UpdateModuleHandler : IRequestHandler<UpdateModuleCommand>
{
    private readonly IAuthDbContext _context;
    private readonly IAuditLogger _auditLogger;
    private readonly ILogger<UpdateModuleHandler> _logger;

    public UpdateModuleHandler(IAuthDbContext context, IAuditLogger auditLogger, ILogger<UpdateModuleHandler> logger)
    {
        _context = context;
        _auditLogger = auditLogger;
        _logger = logger;
    }

    public async Task Handle(UpdateModuleCommand request, CancellationToken cancellationToken)
    {
        var module = await _context.Modules.FirstOrDefaultAsync(m => m.Id == request.ModuleId, cancellationToken);
        if (module is null)
            throw new AuthService.Domain.Exceptions.ModuleNotFoundException(request.ModuleId);

        module.Update(request.Description);
        await _context.SaveChangesAsync(cancellationToken);
        await _auditLogger.LogAsync(AuthService.Domain.Enums.AuditAction.ModuleUpdated, null, null, true, null, null, $"Module {module.Name} updated.", cancellationToken);
    }
}
