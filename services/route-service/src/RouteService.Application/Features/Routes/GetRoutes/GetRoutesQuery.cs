using MediatR;
using RouteService.Application.Common.Models;
using RouteService.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace RouteService.Application.Features.Routes.GetRoutes;

public sealed record GetRoutesQuery(string? SearchTerm, string? TransportMode, string? Status, int Page = 1, int PageSize = 50) : IRequest<PagedResult<RouteDto>>;
