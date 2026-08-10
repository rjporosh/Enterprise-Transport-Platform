using MediatR;
using RouteService.Application.Common.Models;
using RouteService.Domain.Exceptions;

namespace RouteService.Application.Features.Stops.GetStop;

public sealed record GetStopQuery(Guid StopId) : IRequest<StopDto>;
