using AuthService.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AuthService.Application.Features.Admin.Roles;

public sealed class GetRolesHandler : IRequestHandler<GetRolesQuery, List<RoleDto>>
{
    private readonly IAuthDbContext _context;

    public GetRolesHandler(IAuthDbContext context)
    {
        _context = context;
    }

    public async Task<List<RoleDto>> Handle(GetRolesQuery request, CancellationToken cancellationToken)
    {
        return await _context.Roles
            .OrderBy(r => r.Name)
            .Select(r => new RoleDto(r.Id, r.Name, r.Description, r.IsActive))
            .ToListAsync(cancellationToken);
    }
}
