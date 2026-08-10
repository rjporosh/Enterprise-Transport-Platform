using MediatR;
using PaymentService.Application.Common.Models;
using System.Text.Json.Serialization;

namespace PaymentService.Application.Features.AgentPaymentMethods;

public record GetAgentPaymentMethodsQuery(
    Guid AgentId,
    bool? OnlyVerified = null,
    int Page = 1,
    int PageSize = 20,
    [property: JsonIgnore] CancellationToken CancellationToken = default) : IRequest<PagedResult<AgentPaymentMethodDto>>;