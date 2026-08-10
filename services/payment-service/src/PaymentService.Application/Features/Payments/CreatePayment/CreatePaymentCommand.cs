using MediatR;
using PaymentService.Domain.Enums;
using System.Text.Json.Serialization;

namespace PaymentService.Application.Features.Payments.CreatePayment;

public record CreatePaymentCommand(
    Guid TenantId,
    Guid? CompanyId,
    Guid? OrganizationId,
    Guid CustomerId,
    string OrderReference,
    PaymentMethodType PaymentMethod,
    decimal Amount,
    string Currency,
    string IdempotencyKey,
    decimal? FeeAmount,
    decimal? TaxAmount,
    string? Metadata,
    int? TtlMinutes,
    [property: JsonIgnore] CancellationToken CancellationToken = default) : IRequest<CreatePaymentResponse>;
