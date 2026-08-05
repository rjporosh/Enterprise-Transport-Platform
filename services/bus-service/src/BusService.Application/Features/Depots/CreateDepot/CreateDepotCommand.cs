using BusService.Application.Common.Models;
using MediatR;

namespace BusService.Application.Features.Depots.CreateDepot;

public sealed record CreateDepotCommand(string Name, string City, string? Address) : IRequest<DepotDto>;
