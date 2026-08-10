using BusService.Application.Common.Interfaces;
using BusService.Application.Common.Models;
using BusService.Domain.Entities;
using BusService.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BusService.Application.Features.Depots.RestoreDepot;

public sealed class RestoreDepotHandler : IRequestHandler<RestoreDepotCommand, DepotDto>
{
    private readonly IBusDbContext _context;

    public RestoreDepotHandler(IBusDbContext context)
    {
        _context = context;
    }

    public async Task<DepotDto> Handle(RestoreDepotCommand request, CancellationToken cancellationToken)
    {
        var depot = await _context.Depots.FirstOrDefaultAsync(d => d.Id == request.DepotId, cancellationToken);
        if (depot is null)
            throw new DepotNotFoundException(request.DepotId);

        depot.Restore(DateTimeOffset.UtcNow);
        await _context.SaveChangesAsync(cancellationToken);

        return new DepotDto(depot.Id, depot.Name, depot.City, depot.Address, depot.TenantId, depot.CompanyId, depot.OrganizationId, depot.IsDeleted);
    }
}

public sealed record RestoreDepotCommand(Guid DepotId) : IRequest<DepotDto>;
