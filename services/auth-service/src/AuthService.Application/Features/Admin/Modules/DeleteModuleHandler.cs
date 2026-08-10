using AuthService.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AuthService.Application.Features.Admin.Modules;

public sealed class DeleteModuleHandler : IRequestHandler<DeleteModuleCommand>
{
    private readonly IAuthDbContext _context;
    private readonly IAuditLogger _auditLogger;
    private readonly ILogger<DeleteModuleHandler> _logger;

    public DeleteModuleHandler(IAuthDbContext context, IAuditLogger auditLogger, ILogger<DeleteModuleHandler> logger)
    {
        _context = context;
        _auditLogger = auditLogger;
        _logger = logger;
    }

    public async Task Handle(DeleteModuleCommand request, CancellationToken cancellationToken)
    {
        var module = await _context.Modules.FirstOrDefaultAsync(m => m.Id == request.ModuleId, cancellationToken);
        if (module is not null)
        {
            _context.Modules.Remove(module);
            await _context.SaveChangesAsync(cancellationToken);
            await _auditLogger.LogAsync(AuthService.Domain.Enums.AuditAction.ModuleCreated, null, null, true, null, null, $"Module {module.Name} deleted.", cancellationToken);
        }
    }
}
