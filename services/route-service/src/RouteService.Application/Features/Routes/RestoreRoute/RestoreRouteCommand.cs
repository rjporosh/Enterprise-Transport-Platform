using MediatR;
using RouteService.Application.Common.Models;
using RouteService.Domain.Entities;
using RouteService.Domain.Exceptions;
using RouteService.Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace RouteService.Application.Features.Routes.RestoreRoute;

public sealed record RestoreRouteCommand(Guid RouteId) : IRequest<Result>;
