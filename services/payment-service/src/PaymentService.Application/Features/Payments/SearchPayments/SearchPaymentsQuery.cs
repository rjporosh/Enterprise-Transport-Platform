using MediatR;
using PaymentService.Application.Common.Models;
using PaymentService.Application.Features.Payments.GetPaymentById;
using PaymentService.Domain.Enums;
using System.Text.Json.Serialization;

namespace PaymentService.Application.Features.Payments.SearchPayments;

public record SearchPaymentsQuery(
    Guid TenantId,
    Guid? CustomerId,
    string? OrderReference,
    string? ProviderReference,
    PaymentStatus? Status,
    DateTimeOffset? FromDate,
    DateTimeOffset? ToDate,
    int Page,
    int PageSize,
    [property: JsonIgnore] CancellationToken CancellationToken = default) : IRequest<PagedResult<PaymentDto>>;
