using MediatR;
using System.Text.Json.Serialization;

namespace PaymentService.Application.Features.AgentPaymentMethods;

public record GetDefaultPaymentMethodQuery(
    Guid AgentId,
    [property: JsonIgnore] CancellationToken CancellationToken = default) : IRequest<AgentPaymentMethodDto?>;