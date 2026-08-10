using MediatR;
using RouteService.Application.Common.Models;

namespace RouteService.Application.Features.Routes.SearchRoutes;

public sealed record SearchRoutesQuery(string Term, int Page = 1, int PageSize = 50) : IRequest<PagedResult<RouteService.Application.Common.Models.RouteDto>>;
