using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PaymentService.Application.Common.Interfaces;
using PaymentService.Domain.Enums;
using PaymentService.Domain.Exceptions;

namespace PaymentService.Application.Features.Payments.ConfirmPayment;

/// <summary>
/// Confirms a Processing payment — but **never on the strength of the request
/// body** (P0-5). The client-supplied transaction id is treated as a hint
/// only; the payment is driven to Succeeded solely on a server-side
/// <c>provider.GetStatusAsync</c> that returns <c>Succeeded</c>. A provider
/// that can't be polled (QR) returns <c>Unknown</c> here → this endpoint
/// 409s and settlement must come through the signed webhook or the audited
/// settle path.
/// </summary>
public class ConfirmPaymentHandler : IRequestHandler<ConfirmPaymentCommand, ConfirmPaymentResponse>
{
    private readonly IPaymentDbContext _context;
    private readonly IPaymentProviderFactory _providerFactory;
    private readonly IEventPublisher _eventPublisher;
    private readonly ICurrentUser _currentUser;
    private readonly ILogger<ConfirmPaymentHandler> _logger;

    public ConfirmPaymentHandler(
        IPaymentDbContext context,
        IPaymentProviderFactory providerFactory,
        IEventPublisher eventPublisher,
        ICurrentUser currentUser,
        ILogger<ConfirmPaymentHandler> logger)
    {
        _context = context;
        _providerFactory = providerFactory;
        _eventPublisher = eventPublisher;
        _currentUser = currentUser;
        _logger = logger;
    }

    public async Task<ConfirmPaymentResponse> Handle(ConfirmPaymentCommand request, CancellationToken cancellationToken)
    {
        var payment = await _context.Payments
            .FirstOrDefaultAsync(p => p.Id == request.PaymentId, cancellationToken)
            ?? throw new PaymentNotFoundException(request.PaymentId);

        // Ownership / tenant isolation from claims — not from any header.
        var callerId = _currentUser.CustomerId ?? _currentUser.UserId;
        var isPrivileged = _currentUser.IsInRole("Admin") || _currentUser.IsInRole("Operator");
        if (!isPrivileged && callerId is not null && payment.CustomerId != callerId)
            throw new PaymentNotFoundException(request.PaymentId);

        if (payment.Status == PaymentStatus.Succeeded)
            return new ConfirmPaymentResponse(payment.Id, payment.Status.ToString(), payment.ProviderPaymentId ?? string.Empty);

        if (payment.Status != PaymentStatus.Processing)
            throw new InvalidPaymentStateTransitionException(payment.Status.ToString(), PaymentStatus.Succeeded.ToString());

        var provider = _providerFactory.GetProvider(payment.PaymentMethod.ToString());
        var providerPaymentId = payment.ProviderPaymentId ?? payment.Id.ToString();
        var status = await provider.GetStatusAsync(providerPaymentId, cancellationToken);

        if (!status.IsSuccess)
        {
            _logger.LogWarning(
                "Confirm rejected for payment {PaymentId}: provider {Provider} reports {Status}, not Succeeded. " +
                "Client-supplied transaction id is ignored (P0-5). Root cause: the payment has not actually settled " +
                "with the provider. Possible solution: wait for the provider webhook, or (QR) use the settle path.",
                payment.Id, provider.ProviderName, status.Status);
            throw new InvalidPaymentStateTransitionException(
                $"{payment.Status} (provider: {status.Status})", PaymentStatus.Succeeded.ToString());
        }

        payment.Succeed(status.ProviderTransactionId ?? providerPaymentId, status.ProviderReference);

        foreach (var domainEvent in payment.DomainEvents)
            await _eventPublisher.EnqueueAsync(domainEvent, cancellationToken);
        payment.ClearDomainEvents();

        await _context.SaveChangesAsync(cancellationToken);

        return new ConfirmPaymentResponse(payment.Id, payment.Status.ToString(), payment.ProviderPaymentId ?? string.Empty);
    }
}
