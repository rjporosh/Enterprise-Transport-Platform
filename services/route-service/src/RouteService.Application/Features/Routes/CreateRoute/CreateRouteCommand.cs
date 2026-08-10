using MediatR;

namespace RouteService.Application.Features.Routes.CreateRoute;

public sealed record CreateRouteCommand(string Code, string Name, Guid OriginStopId, Guid DestinationStopId, string TransportMode, double DistanceKm, TimeSpan EstimatedDuration, string? CreatedBy) : IRequest<Result<RouteService.Application.Common.Models.RouteDto>>;
