using MediatR;
using Microsoft.Extensions.Logging;
using PaymentService.Application.Common.Interfaces;
using PaymentService.Domain.Entities;
using PaymentService.Domain.Exceptions;
using System.Text.Json.Serialization;

namespace PaymentService.Application.Features.Payments.CancelPayment;

public record CancelPaymentCommand(
    Guid PaymentId,
    string? Reason,
    [property: JsonIgnore] CancellationToken CancellationToken = default) : IRequest<CancelPaymentResponse>;
