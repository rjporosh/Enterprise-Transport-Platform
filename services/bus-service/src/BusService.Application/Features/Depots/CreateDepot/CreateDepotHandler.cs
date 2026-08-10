using BusService.Application.Common.Interfaces;
using BusService.Application.Common.Models;
using BusService.Domain.Entities;
using MediatR;

namespace BusService.Application.Features.Depots.CreateDepot;

public sealed class CreateDepotHandler : IRequestHandler<CreateDepotCommand, DepotDto>
{
    private readonly IBusDbContext _context;

    public CreateDepotHandler(IBusDbContext context) => _context = context;

    public async Task<DepotDto> Handle(CreateDepotCommand request, CancellationToken cancellationToken)
    {
        var depot = Depot.Create(Guid.NewGuid(), request.Name.Trim(), request.City.Trim(), request.Address?.Trim());

        _context.Depots.Add(depot);
        await _context.SaveChangesAsync(cancellationToken);

        return new DepotDto(depot.Id, depot.Name, depot.City, depot.Address);
    }
}
