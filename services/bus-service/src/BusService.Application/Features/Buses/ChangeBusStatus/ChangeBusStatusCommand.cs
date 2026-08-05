using BusService.Application.Common.Models;
using MediatR;

namespace BusService.Application.Features.Buses.ChangeBusStatus;

public sealed record ChangeBusStatusCommand(Guid BusId, string NewStatus) : IRequest<BusDto>;
