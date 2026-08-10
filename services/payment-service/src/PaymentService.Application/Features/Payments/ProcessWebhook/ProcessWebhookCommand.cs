using MediatR;
using Microsoft.Extensions.Logging;
using PaymentService.Application.Common.Interfaces;
using PaymentService.Domain.Entities;
using PaymentService.Domain.Exceptions;
using System.Text.Json.Serialization;

namespace PaymentService.Application.Features.Payments.ProcessWebhook;

public record ProcessWebhookCommand(
    string ProviderName,
    string EventType,
    string EventId,
    string Payload,
    string? Signature,
    DateTimeOffset? Timestamp,
    [property: JsonIgnore] CancellationToken CancellationToken = default) : IRequest<ProcessWebhookResponse>;
