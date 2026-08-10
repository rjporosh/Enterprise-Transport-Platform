using MediatR;
using RouteService.Application.Common.Models;

namespace RouteService.Application.Features.Stops.GetStops;

public sealed record GetStopsQuery(string? City, string? SearchTerm, int Page = 1, int PageSize = 50) : IRequest<PagedResult<StopDto>>;
