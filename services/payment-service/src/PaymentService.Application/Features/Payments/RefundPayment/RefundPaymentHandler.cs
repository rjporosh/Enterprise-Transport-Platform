using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PaymentService.Application.Common.Interfaces;
using PaymentService.Application.Common.Models;
using PaymentService.Domain.Entities;
using PaymentService.Domain.Exceptions;

namespace PaymentService.Application.Features.Payments.RefundPayment;

public class RefundPaymentHandler : IRequestHandler<RefundPaymentCommand, RefundPaymentResponse>
{
    private readonly IPaymentDbContext _context;
    private readonly IEventPublisher _eventPublisher;
    private readonly ILogger<RefundPaymentHandler> _logger;

    public RefundPaymentHandler(IPaymentDbContext context, IEventPublisher eventPublisher, ILogger<RefundPaymentHandler> logger)
    {
        _context = context;
        _eventPublisher = eventPublisher;
        _logger = logger;
    }

    public async Task<RefundPaymentResponse> Handle(RefundPaymentCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Initiating refund for payment {PaymentId}, amount {Amount}", request.PaymentId, request.Amount);

        var payment = await _context.Payments
            .Include(p => p.Refunds)
            .FirstOrDefaultAsync(p => p.Id == request.PaymentId, cancellationToken)
            ?? throw new PaymentNotFoundException(request.PaymentId);

        var refund = payment.InitiateRefund(request.Amount, request.Reason, request.InitiatedByUserId);

        _context.Refunds.Add(refund);

        foreach (var domainEvent in payment.DomainEvents)
        {
            await _eventPublisher.EnqueueAsync(domainEvent, cancellationToken);
        }

        payment.ClearDomainEvents();
        await _context.SaveChangesAsync(cancellationToken);

        return new RefundPaymentResponse(refund.Id, refund.Status.ToString(), refund.Amount, refund.Currency);
    }
}
