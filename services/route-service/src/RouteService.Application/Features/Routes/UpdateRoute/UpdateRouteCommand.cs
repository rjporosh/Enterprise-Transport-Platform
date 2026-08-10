using MediatR;
using RouteService.Domain.Exceptions;
using RouteService.Application.Common.Models;
using RouteService.Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace RouteService.Application.Features.Routes.UpdateRoute;

public sealed record UpdateRouteCommand(Guid RouteId, string Name, string TransportMode, double DistanceKm, TimeSpan EstimatedDuration, uint ExpectedVersion, string? UpdatedBy) : IRequest<Result<RouteDto>>;
