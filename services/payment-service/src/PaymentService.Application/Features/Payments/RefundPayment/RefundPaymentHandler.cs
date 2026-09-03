using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PaymentService.Application.Common.Interfaces;
using PaymentService.Application.Common.Models;
using PaymentService.Domain.Exceptions;

namespace PaymentService.Application.Features.Payments.RefundPayment;

/// <summary>
/// Records a refund, then **actually calls the provider** (P0-7 — the old
/// handler never did). The refund and the payment status follow the provider
/// result: a rejected refund fails the <see cref="Domain.Entities.PaymentRefund"/>
/// row and leaves <c>Payment.Status</c> untouched; a confirmed refund moves the
/// payment to PartiallyRefunded / Refunded and publishes <c>payment.refunded</c>.
/// Idempotent per (payment, amount, reason) is the caller's responsibility via
/// an Idempotency-Key; amount ≤ available is enforced in the domain.
/// </summary>
public class RefundPaymentHandler : IRequestHandler<RefundPaymentCommand, RefundPaymentResponse>
{
    private readonly IPaymentDbContext _context;
    private readonly IPaymentProviderFactory _providerFactory;
    private readonly IEventPublisher _eventPublisher;
    private readonly ILogger<RefundPaymentHandler> _logger;

    public RefundPaymentHandler(
        IPaymentDbContext context,
        IPaymentProviderFactory providerFactory,
        IEventPublisher eventPublisher,
        ILogger<RefundPaymentHandler> logger)
    {
        _context = context;
        _providerFactory = providerFactory;
        _eventPublisher = eventPublisher;
        _logger = logger;
    }

    public async Task<RefundPaymentResponse> Handle(RefundPaymentCommand request, CancellationToken cancellationToken)
    {
        var payment = await _context.Payments
            .Include(p => p.Refunds)
            .FirstOrDefaultAsync(p => p.Id == request.PaymentId, cancellationToken)
            ?? throw new PaymentNotFoundException(request.PaymentId);

        var refund = payment.InitiateRefund(request.Amount, request.Reason, request.InitiatedByUserId);
        _context.Refunds.Add(refund);

        refund.MarkProcessing();

        var provider = _providerFactory.GetProvider(payment.PaymentMethod.ToString());
        PaymentProviderResult result;
        try
        {
            result = await provider.RefundAsync(new RefundProviderRequest(
                ProviderPaymentId: payment.ProviderPaymentId ?? payment.Id.ToString(),
                RefundAmount: refund.Amount,
                Currency: refund.Currency,
                RefundReason: request.Reason,
                IdempotencyKey: $"refund:{payment.Id}:{refund.Id}"), cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Provider {Provider} threw refunding payment {PaymentId}", provider.ProviderName, payment.Id);
            refund.Fail($"Provider error: {ex.Message}", "provider_exception");
            payment.ApplyRefundSettlement(refund);
            await PublishAndSaveAsync(payment, cancellationToken);
            return new RefundPaymentResponse(refund.Id, refund.Status.ToString(), refund.Amount, refund.Currency);
        }

        if (result.Status == PaymentProviderStatus.Succeeded)
        {
            refund.Succeed(result.ProviderTransactionId ?? result.ProviderReference);
        }
        else if (result.Status is PaymentProviderStatus.Processing or PaymentProviderStatus.Unknown)
        {
            // Left in Processing — a reconciliation job / provider callback resolves it.
            _logger.LogInformation("Refund {RefundId} for payment {PaymentId} is pending provider settlement ({Status}).",
                refund.Id, payment.Id, result.Status);
        }
        else
        {
            refund.Fail(result.ErrorMessage ?? "Provider rejected the refund.", result.ErrorCode);
        }

        payment.ApplyRefundSettlement(refund);
        await PublishAndSaveAsync(payment, cancellationToken);

        return new RefundPaymentResponse(refund.Id, refund.Status.ToString(), refund.Amount, refund.Currency);
    }

    private async Task PublishAndSaveAsync(Domain.Entities.Payment payment, CancellationToken ct)
    {
        foreach (var domainEvent in payment.DomainEvents)
            await _eventPublisher.EnqueueAsync(domainEvent, ct);
        payment.ClearDomainEvents();
        await _context.SaveChangesAsync(ct);
    }
}
