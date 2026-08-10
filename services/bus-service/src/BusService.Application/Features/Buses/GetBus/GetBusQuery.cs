using BusService.Application.Common.Models;
using MediatR;

namespace BusService.Application.Features.Buses.GetBus;

public sealed record GetBusQuery(Guid BusId) : IRequest<BusDto>;
