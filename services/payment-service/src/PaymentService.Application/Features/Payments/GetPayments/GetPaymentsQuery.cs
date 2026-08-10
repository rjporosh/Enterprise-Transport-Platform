using MediatR;
using PaymentService.Application.Common.Models;
using PaymentService.Domain.Enums;
using System.Text.Json.Serialization;

namespace PaymentService.Application.Features.Payments.GetPayments;

public record GetPaymentsQuery(
    Guid TenantId,
    Guid? CustomerId,
    PaymentStatus? Status,
    int Page,
    int PageSize,
    [property: JsonIgnore] CancellationToken CancellationToken = default) : IRequest<PagedResult<PaymentDto>>;
