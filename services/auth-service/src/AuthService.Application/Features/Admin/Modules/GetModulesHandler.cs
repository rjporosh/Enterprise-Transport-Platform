using AuthService.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AuthService.Application.Features.Admin.Modules;

public sealed class GetModulesHandler : IRequestHandler<GetModulesQuery, List<ModuleDto>>
{
    private readonly IAuthDbContext _context;

    public GetModulesHandler(IAuthDbContext context)
    {
        _context = context;
    }

    public async Task<List<ModuleDto>> Handle(GetModulesQuery request, CancellationToken cancellationToken)
    {
        return await _context.Modules
            .OrderBy(m => m.Name)
            .Select(m => new ModuleDto(m.Id, m.Name, m.Description, m.IsActive))
            .ToListAsync(cancellationToken);
    }
}
