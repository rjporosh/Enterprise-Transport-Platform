using BusService.Application.Common.Interfaces;
using BusService.Application.Common.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BusService.Application.Features.Depots.GetDepots;

public sealed class GetDepotsHandler : IRequestHandler<GetDepotsQuery, IReadOnlyCollection<DepotDto>>
{
    private readonly IBusDbContext _context;
    private readonly ICurrentUser _currentUser;

    public GetDepotsHandler(IBusDbContext context, ICurrentUser currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<IReadOnlyCollection<DepotDto>> Handle(GetDepotsQuery request, CancellationToken cancellationToken)
    {
        var query = _context.Depots.AsQueryable();

        var tenantId = _currentUser.TenantId ?? request.TenantId;
        if (tenantId.HasValue)
            query = query.Where(d => d.TenantId == tenantId.Value);

        if (!string.IsNullOrWhiteSpace(request.City))
            query = query.Where(d => d.City == request.City);

        return await query
            .OrderBy(d => d.City).ThenBy(d => d.Name)
            .Select(d => new DepotDto(d.Id, d.Name, d.City, d.Address, d.TenantId, d.CompanyId, d.OrganizationId, d.IsDeleted))
            .ToListAsync(cancellationToken);
    }
}
