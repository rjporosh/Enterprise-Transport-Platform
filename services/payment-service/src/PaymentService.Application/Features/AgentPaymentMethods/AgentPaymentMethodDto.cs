namespace PaymentService.Application.Features.AgentPaymentMethods;

public sealed record AgentPaymentMethodDto(
    Guid Id,
    Guid AgentId,
    string MethodType,
    string Provider,
    string AccountNumber,
    string? AccountName,
    bool IsDefault,
    bool IsVerified,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? UpdatedAtUtc);