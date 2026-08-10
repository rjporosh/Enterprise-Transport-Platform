using AuthService.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AuthService.Application.Features.Admin.Permissions;

public sealed class GetPermissionsHandler : IRequestHandler<GetPermissionsQuery, List<PermissionDto>>
{
    private readonly IAuthDbContext _context;

    public GetPermissionsHandler(IAuthDbContext context)
    {
        _context = context;
    }

    public async Task<List<PermissionDto>> Handle(GetPermissionsQuery request, CancellationToken cancellationToken)
    {
        var query = _context.Permissions.AsQueryable();
        if (!string.IsNullOrWhiteSpace(request.Module))
            query = query.Where(p => p.Module == request.Module);

        return await query.OrderBy(p => p.Module).ThenBy(p => p.Name)
            .Select(p => new PermissionDto(p.Id, p.Name, p.Description, p.Module, p.IsActive))
            .ToListAsync(cancellationToken);
    }
}
