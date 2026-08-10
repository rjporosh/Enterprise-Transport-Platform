using MediatR;
using Microsoft.Extensions.Logging;
using PaymentService.Application.Common.Interfaces;
using PaymentService.Domain.Entities;
using PaymentService.Domain.Exceptions;
using System.Text.Json.Serialization;

namespace PaymentService.Application.Features.Payments.RefundPayment;

public record RefundPaymentCommand(
    Guid PaymentId,
    decimal Amount,
    string Reason,
    string? InitiatedByUserId,
    [property: JsonIgnore] CancellationToken CancellationToken = default) : IRequest<RefundPaymentResponse>;
