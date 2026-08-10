using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PaymentService.Application.Common.Interfaces;
using PaymentService.Application.Common.Models;
using PaymentService.Domain.Entities;
using PaymentService.Domain.Enums;
using PaymentService.Domain.Exceptions;
using Microsoft.AspNetCore.Http;

namespace PaymentService.Application.Features.Payments.ProcessWebhook;

public class ProcessWebhookHandler : IRequestHandler<ProcessWebhookCommand, ProcessWebhookResponse>
{
    private readonly IPaymentDbContext _context;
    private readonly IPaymentProviderFactory _providerFactory;
    private readonly IEventPublisher _eventPublisher;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<ProcessWebhookHandler> _logger;

    public ProcessWebhookHandler(
        IPaymentDbContext context,
        IPaymentProviderFactory providerFactory,
        IEventPublisher eventPublisher,
        IHttpContextAccessor httpContextAccessor,
        ILogger<ProcessWebhookHandler> logger)
    {
        _context = context;
        _providerFactory = providerFactory;
        _eventPublisher = eventPublisher;
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
    }

    public async Task<ProcessWebhookResponse> Handle(ProcessWebhookCommand request, CancellationToken cancellationToken)
    {
        var correlationId = _httpContextAccessor.HttpContext?.Items["CorrelationId"]?.ToString();
        _logger.LogInformation("Processing webhook {EventType} from provider {ProviderName}", request.EventType, request.ProviderName);

        var provider = _providerFactory.GetProvider(request.ProviderName);

        Payment? payment = null;
        string newStatus = string.Empty;

        try
        {
            var providerResult = await provider.ProcessAsync(new PaymentProviderRequest(
                request.EventId,
                0,
                string.Empty,
                string.Empty,
                Guid.Empty,
                string.Empty,
                request.EventId,
                correlationId,
                null), cancellationToken);

            if (providerResult.IsSuccess)
            {
                var providerPaymentId = ExtractProviderPaymentId(request.Payload, request.EventType);
                payment = await _context.Payments
                    .FirstOrDefaultAsync(p => p.ProviderPaymentId == providerPaymentId, cancellationToken);

                if (payment is null)
                {
                    _logger.LogWarning("Payment with provider ID {ProviderPaymentId} not found", providerPaymentId);
                    return new ProcessWebhookResponse(false, null, "NotFound", "Payment not found");
                }

                if (payment.Status != PaymentStatus.Succeeded && payment.Status != PaymentStatus.Failed)
                {
                    payment.Succeed(providerResult.ProviderTransactionId ?? providerPaymentId);
                    newStatus = payment.Status.ToString();
                }
            }
            else if (!providerResult.IsTransientFailure)
            {
                var providerPaymentId = ExtractProviderPaymentId(request.Payload, request.EventType);
                payment = await _context.Payments
                    .FirstOrDefaultAsync(p => p.ProviderPaymentId == providerPaymentId, cancellationToken);

                if (payment is not null && payment.Status is PaymentStatus.Pending or PaymentStatus.Processing)
                {
                    payment.Fail(providerResult.ErrorMessage ?? "Webhook indicated failure.", providerResult.ErrorCode);
                    newStatus = payment.Status.ToString();
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing webhook {EventId}", request.EventId);
            return new ProcessWebhookResponse(false, null, "Error", ex.Message);
        }

        if (payment is not null)
        {
            foreach (var domainEvent in payment.DomainEvents)
            {
                await _eventPublisher.EnqueueAsync(domainEvent, cancellationToken);
            }

            payment.ClearDomainEvents();
            await _context.SaveChangesAsync(cancellationToken);
        }

        return new ProcessWebhookResponse(true, payment?.Id.ToString(), newStatus, null);
    }

    private static string ExtractProviderPaymentId(string payload, string eventType)
    {
        try
        {
            using var doc = System.Text.Json.JsonDocument.Parse(payload);
            if (doc.RootElement.TryGetProperty("payment_id", out var paymentId))
                return paymentId.GetString() ?? string.Empty;
            if (doc.RootElement.TryGetProperty("id", out var id))
                return id.GetString() ?? string.Empty;
        }
        catch
        {
            // ignore parse errors
        }

        return eventType;
    }
}
