using BusService.Application.Common.Models;
using MediatR;

namespace BusService.Application.Features.Buses.UpdateBusDetails;

public sealed record UpdateBusDetailsCommand(
    Guid BusId,
    string BusType,
    int TotalSeats,
    Guid DepotId,
    string? Manufacturer,
    string? Model,
    int? YearOfManufacture) : IRequest<BusDto>;
