using MediatR;
using System.Text.Json.Serialization;

namespace PaymentService.Application.Features.AgentPaymentMethods;

public record VerifyAgentPaymentMethodCommand(
    Guid PaymentMethodId,
    string? VerificationToken,
    [property: JsonIgnore] CancellationToken CancellationToken = default) : IRequest<AgentPaymentMethodDto>;