using MediatR;
using RouteService.Application.Common.Models;
using RouteService.Domain.Exceptions;
using RouteService.Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace RouteService.Application.Features.Stops.UpdateStop;

public sealed record UpdateStopCommand(Guid StopId, string Name, string City, string? Address, double Latitude, double Longitude, string? UpdatedBy) : IRequest<Result<StopDto>>;
