using MediatR;
using RouteService.Application.Common.Models;

namespace RouteService.Application.Features.Stops.CreateStop;

public sealed record CreateStopCommand(string Code, string Name, string City, string? Address, double Latitude, double Longitude, string? CreatedBy) : IRequest<Result<StopDto>>;
