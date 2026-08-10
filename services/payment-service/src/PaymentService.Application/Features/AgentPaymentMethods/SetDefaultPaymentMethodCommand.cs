using MediatR;
using System.Text.Json.Serialization;

namespace PaymentService.Application.Features.AgentPaymentMethods;

public record SetDefaultPaymentMethodCommand(
    Guid AgentId,
    Guid PaymentMethodId,
    [property: JsonIgnore] CancellationToken CancellationToken = default) : IRequest<AgentPaymentMethodDto>;