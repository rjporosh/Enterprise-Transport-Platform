using BusService.Application.Common.Interfaces;
using BusService.Application.Common.Models;
using BusService.Domain.Entities;
using MediatR;

namespace BusService.Application.Features.Depots.CreateDepot;

public sealed class CreateDepotHandler : IRequestHandler<CreateDepotCommand, DepotDto>
{
    private readonly IBusDbContext _context;
    private readonly ICurrentUser _currentUser;

    public CreateDepotHandler(IBusDbContext context, ICurrentUser currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<DepotDto> Handle(CreateDepotCommand request, CancellationToken cancellationToken)
    {
        var tenantId = _currentUser.TenantId ?? request.TenantId;
        var companyId = _currentUser.CompanyId ?? request.CompanyId;
        var organizationId = _currentUser.OrganizationId ?? request.OrganizationId;

        var depot = Depot.Create(Guid.NewGuid(), request.Name.Trim(), request.City.Trim(), request.Address?.Trim(), tenantId, companyId, organizationId);

        _context.Depots.Add(depot);
        await _context.SaveChangesAsync(cancellationToken);

        return new DepotDto(depot.Id, depot.Name, depot.City, depot.Address, depot.TenantId, depot.CompanyId, depot.OrganizationId, depot.IsDeleted);
    }
}
