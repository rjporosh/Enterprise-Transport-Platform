using MediatR;
using RouteService.Application.Common.Models;
using RouteService.Domain.Exceptions;
using RouteService.Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace RouteService.Application.Features.Routes.GetRoute;

public sealed record GetRouteQuery(Guid RouteId) : IRequest<RouteDto>;
