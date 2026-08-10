using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PaymentService.Application.Common.Interfaces;
using PaymentService.Application.Common.Models;
using PaymentService.Domain.Entities;
using PaymentService.Domain.Enums;
using PaymentService.Domain.Exceptions;
using Microsoft.AspNetCore.Http;

namespace PaymentService.Application.Features.Payments.ProcessPayment;

public class ProcessPaymentHandler : IRequestHandler<ProcessPaymentCommand, ProcessPaymentResponse>
{
    private readonly IPaymentDbContext _context;
    private readonly IPaymentProviderFactory _providerFactory;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IEventPublisher _eventPublisher;
    private readonly IPaymentMetrics _metrics;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<ProcessPaymentHandler> _logger;

    public ProcessPaymentHandler(
        IPaymentDbContext context,
        IPaymentProviderFactory providerFactory,
        IDateTimeProvider dateTimeProvider,
        IEventPublisher eventPublisher,
        IPaymentMetrics metrics,
        IHttpContextAccessor httpContextAccessor,
        ILogger<ProcessPaymentHandler> logger)
    {
        _context = context;
        _providerFactory = providerFactory;
        _dateTimeProvider = dateTimeProvider;
        _eventPublisher = eventPublisher;
        _metrics = metrics;
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
    }

    public async Task<ProcessPaymentResponse> Handle(ProcessPaymentCommand request, CancellationToken cancellationToken)
    {
        var correlationId = _httpContextAccessor.HttpContext?.Items["CorrelationId"]?.ToString();

        _logger.LogInformation("Processing payment {PaymentId}", request.PaymentId);

        var payment = await _context.Payments
            .FirstOrDefaultAsync(p => p.Id == request.PaymentId, cancellationToken)
            ?? throw new PaymentNotFoundException(request.PaymentId);

        if (payment.IsExpired && payment.Status == PaymentStatus.Pending)
        {
            payment.Fail("Payment expired.");
            await _context.SaveChangesAsync(cancellationToken);
            return new ProcessPaymentResponse(payment.Id, payment.Status.ToString(), payment.ProviderReference);
        }

        if (payment.Status is not (PaymentStatus.Pending or PaymentStatus.Processing))
        {
            _logger.LogWarning("Payment {PaymentId} is in status {Status} and cannot be processed", request.PaymentId, payment.Status);
            return new ProcessPaymentResponse(payment.Id, payment.Status.ToString(), payment.ProviderReference);
        }

        payment.StartProcessing(request.ProviderReference);
        await _context.SaveChangesAsync(cancellationToken);

        var provider = _providerFactory.GetProvider(payment.PaymentMethod.ToString());

        var providerRequest = new PaymentProviderRequest(
            payment.ProviderPaymentId ?? payment.Id.ToString(),
            payment.Amount.Amount,
            payment.Amount.Currency,
            payment.OrderReference,
            payment.CustomerId,
            payment.PaymentMethod.ToString(),
            payment.IdempotencyKey,
            correlationId,
            string.IsNullOrEmpty(payment.Metadata) ? null : System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string>>(payment.Metadata));

        PaymentProviderResult providerResult;

        try
        {
            providerResult = await provider.ProcessAsync(providerRequest, cancellationToken);
            _metrics.RecordProviderLatency(provider.ProviderName, 0);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Provider {Provider} failed processing payment {PaymentId}", provider.ProviderName, request.PaymentId);
            payment.Fail($"Provider error: {ex.Message}");
            await _context.SaveChangesAsync(cancellationToken);
            return new ProcessPaymentResponse(payment.Id, payment.Status.ToString(), payment.ProviderReference);
        }

        if (providerResult.IsSuccess)
        {
            payment.Succeed(providerResult.ProviderTransactionId ?? providerResult.ProviderReference ?? string.Empty);
            _metrics.RecordPaymentSucceeded(payment.PaymentMethod.ToString(), payment.Amount.Amount, payment.Amount.Currency);
        }
        else if (providerResult.IsTransientFailure)
        {
            _logger.LogWarning("Provider returned transient failure for payment {PaymentId}. Leaving in Processing state.", request.PaymentId);
        }
        else
        {
            payment.Fail(providerResult.ErrorMessage ?? "Payment failed.", providerResult.ErrorCode);
            _metrics.RecordPaymentFailed(payment.PaymentMethod.ToString(), providerResult.ErrorCode);
        }

        foreach (var domainEvent in payment.DomainEvents)
        {
            await _eventPublisher.EnqueueAsync(domainEvent, cancellationToken);
        }

        payment.ClearDomainEvents();
        await _context.SaveChangesAsync(cancellationToken);

        return new ProcessPaymentResponse(payment.Id, payment.Status.ToString(), payment.ProviderReference);
    }
}
