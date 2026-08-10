using BusService.Application.Common.Models;
using MediatR;

namespace BusService.Application.Features.Buses.GetBuses;

public sealed record GetBusesQuery(
    Guid? OperatorId,
    Guid? DepotId,
    Guid? TenantId,
    Guid? CompanyId,
    Guid? OrganizationId,
    string? Status,
    int Page = 1,
    int PageSize = 50) : IRequest<PagedResult<BusDto>>;
