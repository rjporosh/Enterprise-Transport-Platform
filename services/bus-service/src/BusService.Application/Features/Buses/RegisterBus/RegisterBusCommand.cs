using BusService.Application.Common.Models;
using MediatR;

namespace BusService.Application.Features.Buses.RegisterBus;

public sealed record RegisterBusCommand(
    Guid OperatorId,
    string PlateNumber,
    string BusType,
    int TotalSeats,
    Guid DepotId,
    string? Manufacturer,
    string? Model,
    int? YearOfManufacture) : IRequest<BusDto>;
