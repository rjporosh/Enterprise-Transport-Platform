using MediatR;
using Microsoft.Extensions.Logging;
using PaymentService.Application.Common.Interfaces;
using PaymentService.Domain.Entities;
using PaymentService.Domain.Exceptions;
using System.Text.Json.Serialization;

namespace PaymentService.Application.Features.Payments.FailPayment;

public record FailPaymentCommand(
    Guid PaymentId,
    string Reason,
    string? FailureCode,
    [property: JsonIgnore] CancellationToken CancellationToken = default) : IRequest<FailPaymentResponse>;
