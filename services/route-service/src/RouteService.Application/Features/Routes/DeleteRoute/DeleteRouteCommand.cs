using MediatR;
using RouteService.Application.Common.Models;

namespace RouteService.Application.Features.Routes.DeleteRoute;

public sealed record DeleteRouteCommand(Guid RouteId, uint ExpectedVersion) : IRequest<Result>;
