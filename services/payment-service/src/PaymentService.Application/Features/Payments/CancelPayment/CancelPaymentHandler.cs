using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PaymentService.Application.Common.Interfaces;
using PaymentService.Domain.Entities;
using PaymentService.Domain.Exceptions;

namespace PaymentService.Application.Features.Payments.CancelPayment;

public class CancelPaymentHandler : IRequestHandler<CancelPaymentCommand, CancelPaymentResponse>
{
    private readonly IPaymentDbContext _context;
    private readonly IEventPublisher _eventPublisher;
    private readonly ILogger<CancelPaymentHandler> _logger;

    public CancelPaymentHandler(IPaymentDbContext context, IEventPublisher eventPublisher, ILogger<CancelPaymentHandler> logger)
    {
        _context = context;
        _eventPublisher = eventPublisher;
        _logger = logger;
    }

    public async Task<CancelPaymentResponse> Handle(CancelPaymentCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Cancelling payment {PaymentId}", request.PaymentId);

        var payment = await _context.Payments
            .FirstOrDefaultAsync(p => p.Id == request.PaymentId, cancellationToken)
            ?? throw new PaymentNotFoundException(request.PaymentId);

        payment.Cancel(request.Reason);

        foreach (var domainEvent in payment.DomainEvents)
        {
            await _eventPublisher.EnqueueAsync(domainEvent, cancellationToken);
        }

        payment.ClearDomainEvents();
        await _context.SaveChangesAsync(cancellationToken);

        return new CancelPaymentResponse(payment.Id, payment.Status.ToString());
    }
}
