using MediatR;
using RouteService.Application.Common.Models;

namespace RouteService.Application.Features.Stops.DeleteStop;

public sealed record DeleteStopCommand(Guid StopId) : IRequest<Result>;
