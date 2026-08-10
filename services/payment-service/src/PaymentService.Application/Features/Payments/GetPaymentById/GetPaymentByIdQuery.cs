using MediatR;
using System.Text.Json.Serialization;

namespace PaymentService.Application.Features.Payments.GetPaymentById;

public record GetPaymentByIdQuery(
    Guid PaymentId,
    [property: JsonIgnore] CancellationToken CancellationToken = default) : IRequest<PaymentDto?>;
