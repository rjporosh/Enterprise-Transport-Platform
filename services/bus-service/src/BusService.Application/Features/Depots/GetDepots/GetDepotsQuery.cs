using BusService.Application.Common.Models;
using MediatR;

namespace BusService.Application.Features.Depots.GetDepots;

public sealed record GetDepotsQuery(string? City) : IRequest<IReadOnlyCollection<DepotDto>>;
