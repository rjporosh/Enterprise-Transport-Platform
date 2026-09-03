using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PaymentService.Application.Common.Interfaces;
using PaymentService.Domain.Enums;
using PaymentService.Domain.Exceptions;

namespace PaymentService.Application.Features.Payments.SettleQr;

public sealed class SettleQrHandler : IRequestHandler<SettleQrCommand, SettleQrResponse>
{
    private readonly IPaymentDbContext _context;
    private readonly IPaymentProviderFactory _providerFactory;
    private readonly IEventPublisher _eventPublisher;
    private readonly ILogger<SettleQrHandler> _logger;

    public SettleQrHandler(
        IPaymentDbContext context,
        IPaymentProviderFactory providerFactory,
        IEventPublisher eventPublisher,
        ILogger<SettleQrHandler> logger)
    {
        _context = context;
        _providerFactory = providerFactory;
        _eventPublisher = eventPublisher;
        _logger = logger;
    }

    public async Task<SettleQrResponse> Handle(SettleQrCommand request, CancellationToken cancellationToken)
    {
        var payment = await _context.Payments.FirstOrDefaultAsync(p => p.Id == request.PaymentId, cancellationToken)
            ?? throw new PaymentNotFoundException(request.PaymentId);

        if (payment.PaymentMethod != PaymentMethodType.Qr)
            throw new InvalidPaymentStateTransitionException(payment.PaymentMethod.ToString(), "Qr settlement");

        // Idempotent — a repeated webhook / double-click is a no-op.
        if (payment.Status == PaymentStatus.Succeeded)
            return new SettleQrResponse(payment.Id, payment.Status.ToString(), payment.ProviderPaymentId ?? request.SettlementReference);

        if (payment.Status != PaymentStatus.Processing)
            throw new InvalidPaymentStateTransitionException(payment.Status.ToString(), PaymentStatus.Succeeded.ToString());

        // Route through the provider so the same code path serves a real acquirer callback.
        var provider = _providerFactory.GetProvider("Qr");
        var result = await provider.ConfirmAsync(payment.Id.ToString(), cancellationToken);
        if (!result.IsSuccess)
            throw new InvalidPaymentStateTransitionException(payment.Status.ToString(), "Succeeded (provider rejected settlement)");

        payment.Succeed(result.ProviderTransactionId ?? request.SettlementReference, request.SettlementReference);

        foreach (var domainEvent in payment.DomainEvents)
            await _eventPublisher.EnqueueAsync(domainEvent, cancellationToken);
        payment.ClearDomainEvents();

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("QR payment {PaymentId} settled by {SettledBy} (ref {Ref}); payment.succeeded published for order {Order}",
            payment.Id, request.SettledBy, request.SettlementReference, payment.OrderReference);

        return new SettleQrResponse(payment.Id, payment.Status.ToString(), payment.ProviderPaymentId ?? request.SettlementReference);
    }
}
