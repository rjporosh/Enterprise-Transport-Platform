using MediatR;
using PaymentService.Domain.Enums;
using System.Text.Json.Serialization;

namespace PaymentService.Application.Features.AgentPaymentMethods;

public record AddAgentPaymentMethodCommand(
    Guid AgentId,
    PaymentMethodType MethodType,
    string Provider,
    string AccountNumber,
    string? AccountName,
    string? Metadata,
    [property: JsonIgnore] CancellationToken CancellationToken = default) : IRequest<AgentPaymentMethodDto>;