using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PaymentService.Application.Common.Interfaces;
using PaymentService.Application.Features.Payments;
using PaymentService.Domain.Entities;
using PaymentService.Domain.Enums;
using PaymentService.Domain.Exceptions;

namespace PaymentService.Application.Features.Payments.ConfirmPayment;

public class ConfirmPaymentHandler : IRequestHandler<ConfirmPaymentCommand, ConfirmPaymentResponse>
{
    private readonly IPaymentDbContext _context;
    private readonly IEventPublisher _eventPublisher;
    private readonly ILogger<ConfirmPaymentHandler> _logger;

    public ConfirmPaymentHandler(IPaymentDbContext context, IEventPublisher eventPublisher, ILogger<ConfirmPaymentHandler> logger)
    {
        _context = context;
        _eventPublisher = eventPublisher;
        _logger = logger;
    }

    public async Task<ConfirmPaymentResponse> Handle(ConfirmPaymentCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Confirming payment {PaymentId} with transaction {ProviderTransactionId}", request.PaymentId, request.ProviderTransactionId);

        var payment = await _context.Payments
            .FirstOrDefaultAsync(p => p.Id == request.PaymentId, cancellationToken)
            ?? throw new PaymentNotFoundException(request.PaymentId);

        if (payment.Status == PaymentStatus.Succeeded)
        {
            _logger.LogWarning("Payment {PaymentId} is already confirmed", request.PaymentId);
            return new ConfirmPaymentResponse(payment.Id, payment.Status.ToString(), payment.ProviderPaymentId ?? string.Empty);
        }

        payment.Succeed(request.ProviderTransactionId, request.ProviderReference);

        foreach (var domainEvent in payment.DomainEvents)
        {
            await _eventPublisher.EnqueueAsync(domainEvent, cancellationToken);
        }

        payment.ClearDomainEvents();
        await _context.SaveChangesAsync(cancellationToken);

        return new ConfirmPaymentResponse(payment.Id, payment.Status.ToString(), payment.ProviderPaymentId ?? string.Empty);
    }
}
